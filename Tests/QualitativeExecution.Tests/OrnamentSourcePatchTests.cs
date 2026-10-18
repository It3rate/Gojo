using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentSourcePatchTests
{
    [Theory]
    [InlineData(false, 1, 1, 2, 5, 4)]
    [InlineData(false, 3, 2, 12, 23, 12)]
    [InlineData(true, 1, 1, 4, 8, 5)]
    [InlineData(true, 3, 2, 24, 41, 18)]
    public void ExecutedUniqueEdgesSupplyACompleteNativeCellularPatch(bool fan, int columns, int rows,
        int faces, int edges, int sites)
    {
        var source = fan ? OrnamentSourcePatch.SquareFans(columns, rows) : OrnamentSourcePatch.TriangleGrid(columns, rows);
        Assert.True(source.Scaffold.IsComplete); Assert.True(source.Join.TryReadPassed(source.Join.Evaluate()));
        Assert.Equal(faces, source.FaceRecords.Length); Assert.Equal(edges, source.Units.Length);
        Assert.Equal(edges, E(source.Scaffold.Graph.Strokes).Length); Assert.Equal(sites, E(source.Scaffold.Graph.Sites).Length);
        Assert.Equal(sites, E(source.Bindings).Length); Assert.Single(E(source.Scaffold.Exterior));
        Assert.Empty(E(source.Join.Blocked)); Assert.Empty(E(source.Join.Unresolved));
        foreach (var face in source.FaceRecords)
        {
            Assert.True(source.Scaffold.TryReadFace(face.Face, out var boundary, out var vertices));
            Assert.Same(face.Boundary, boundary); Assert.Equal(3, E(boundary).Length); Assert.Equal(3, E(vertices).Length);
            foreach (var coordinate in face.LogicalVertices)
                Assert.Contains(E(vertices), site => ReferenceEquals(site, source.CoordinateSites[coordinate]));
        }
        foreach (var unit in source.Units)
        {
            Assert.True(unit.PlacementExecution.IsAtBoundary); Assert.True(unit.Current.Execution.IsAtBoundary);
            Assert.Single(E(unit.Current.Execution.CompletionEvents));
            Assert.Single(E(unit.Current.Graph.Strokes)); Assert.Equal(2, E(unit.Current.Ports).Length);
            Assert.Empty(E(unit.Pending)); Assert.Empty(E(unit.Unresolved));
        }
        AssertNoNativeNumbers(source.Surface);
    }

    [Fact]
    public void SharedTriangleSeamIsOneActualStrokeReadInOppositeDirections()
    {
        var source = OrnamentSourcePatch.TriangleGrid(1, 1);
        var a = E(source.FaceRecords[0].Boundary).Cast<Relation>().ToArray();
        var b = E(source.FaceRecords[1].Boundary).Cast<Relation>().ToArray();
        var common = a.Join(b, part => part.Left, part => part.Left, (left, right) => (left, right), ReferenceEqualityComparer.Instance).Single();
        var first = Assert.IsType<Relation>(common.left.Right); var second = Assert.IsType<Relation>(common.right.Right);
        Assert.Same(first.Left, second.Right); Assert.Same(first.Right, second.Left);
        Assert.Equal(2, source.FaceRecords.Length); // Reading a seam never merges the two source chambers.
    }

    [Fact]
    public void FanCentreIsAnActualSharedSiteAndVertexStarsOverlapCellGroups()
    {
        var source = OrnamentSourcePatch.SquareFans(2, 2);
        var cell = source.FaceRecords.Where(face => face.CellX == 0 && face.CellY == 0).ToArray();
        var centre = source.CoordinateSites[(3, 3)];
        Assert.Equal(4, cell.Length);
        Assert.All(cell, face =>
        {
            Assert.True(source.Scaffold.TryReadFace(face.Face, out _, out var vertices));
            Assert.Contains(E(vertices), site => ReferenceEquals(site, centre));
        });
        var star = source.FaceRecords.Where(face => face.LogicalVertices.Contains((6, 6))).ToArray();
        Assert.Equal(8, star.Length); Assert.Equal(2, star.Count(face => cell.Any(actual => ReferenceEquals(actual, face))));
        var sourceFaceIdentities = E(source.Scaffold.Faces);
        Assert.All(star, face => Assert.Contains(sourceFaceIdentities, actual => ReferenceEquals(actual, face.Face)));
    }

    [Fact]
    public void TheTwoFramesStayUncalibratedEvenWhenRepeatedWordsHaveSharedEndpoints()
    {
        var source = OrnamentSourcePatch.TriangleGrid(2, 2);
        foreach (var coordinate in source.CoordinateSites)
        {
            var binding = E(source.SiteWords).Cast<Relation>().Single(row => ReferenceEquals(row.Left, coordinate.Value));
            var inverse = Inverse(source, source.Word(coordinate.Key.X, coordinate.Key.Y));
            Assert.True(source.Frame.TryReadResult(source.Frame.Reduce(Assert.IsType<Relation>(binding.Right), inverse),
                out var remaining, out _, out var pending));
            Assert.Empty(E(remaining)); Assert.Empty(E(pending));
        }
        AssertNoNativeNumbers(source.Surface);
    }

    private static Relation Inverse(OrnamentSourcePatch source, Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
        (IElement)Gojo.Experiments.QualitativeExecution.QualitativeMotionFrame.Move(
            E(source.Frame.Opposites).Cast<Relation>().Single(row => ReferenceEquals(row.Left, move.Left)).Right, move.Right)).ToArray());
    private static void AssertNoNativeNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item);
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
