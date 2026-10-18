using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentDerivedScaffoldTests
{
    [Fact]
    public void SharedSeamsProduceFreshRepresentativeAdjacencyWithoutIdentifyingSourceSites()
    {
        var source = Grid(2, 1); var before = source.Evaluate();
        var result = new OrnamentDerivedScaffold(source, new Placeholder());
        Assert.True(result.IsComplete); Assert.Equal(2, E(result.Sites).Length); Assert.Single(E(result.Edges)); Assert.Empty(E(result.Faces));
        Assert.Equal(2, E(result.Representatives).Length); Assert.Single(E(result.Seams));
        Assert.NotEmpty(E(result.Boundary)); Assert.Same(before, source.Evaluate());
        foreach (var row in E(result.Representatives).Cast<Relation>())
        {
            Assert.True(result.TryReadRepresentative(row, out var sourceFace, out var representative));
            Assert.Contains(sourceFace, E(source.Faces)); Assert.Contains(representative, E(result.Sites));
            Assert.DoesNotContain(representative, E(source.Graph.Sites)); Assert.IsType<Placeholder>(representative);
        }
        var edge = (Relation)Assert.Single(E(result.Edges));
        Assert.True(result.TryReadEdge(edge, out var sourceSeam, out var a, out var b, out var evidence));
        Assert.Contains(sourceSeam, E(source.Graph.Strokes)); Assert.NotSame(a, b);
        Assert.Equal(2, E(evidence).Length);
        foreach (var pair in E(evidence).Cast<Relation>())
        {
            Assert.True(source.TryReadFace((Relation)pair.Left, out var boundary, out _));
            Assert.Contains(pair.Right, E(boundary));
            Assert.Same(sourceSeam, ((Relation)pair.Right).Left);
        }
        AssertNoNumbers(result.Surface);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void InteriorVertexStarsProduceActualCyclicCellsAtDifferentArities(int sides)
    {
        var (source, center) = Fan(sides); var result = new OrnamentDerivedScaffold(source, new Placeholder());
        Assert.True(source.IsComplete); Assert.True(result.IsComplete);
        Assert.Equal(sides, E(result.Sites).Length); Assert.Equal(sides, E(result.Edges).Length);
        var cell = (Relation)Assert.Single(E(result.Faces));
        Assert.True(result.TryReadCell(cell, out var origin, out var boundary, out var corners));
        Assert.Same(center, origin); Assert.Equal(sides, E(boundary).Length); Assert.Equal(sides, E(corners).Length);
        Assert.Same(cell, ((Relation)Assert.Single(E(result.CellSources))).Right);
        var darts = E(boundary).Cast<Relation>().ToArray();
        for (var i = 0; i < darts.Length; i++)
        {
            Assert.Contains(darts[i].Left, E(result.Edges));
            Assert.Same(((Relation)darts[i].Right).Right, ((Relation)darts[(i + 1) % darts.Length].Right).Left);
        }
        AssertNoNumbers(result.Surface);
    }

    [Fact]
    public void DerivedTopologySuppliesSeveralFurtherLevelsWhileBoundaryTruncationStaysVisible()
    {
        var source = Grid(4, 4);
        var first = new OrnamentDerivedScaffold(source, new Placeholder());
        var second = new OrnamentDerivedScaffold(first, new Placeholder());
        var third = new OrnamentDerivedScaffold(second, new Placeholder());
        var fourth = new OrnamentDerivedScaffold(third, new Placeholder());
        foreach (var result in new[] { first, second, third, fourth })
        { Assert.True(result.IsComplete); Assert.NotEmpty(E(result.Boundary)); AssertNoNumbers(result.Surface); }
        Assert.Equal((16, 24, 9), Size(first)); Assert.Equal((9, 12, 4), Size(second));
        Assert.Equal((4, 4, 1), Size(third)); Assert.Equal((1, 0, 0), Size(fourth));
        Assert.Same(first.Topology, second.SourceTopology); Assert.Same(first.Surface, second.Source);
        Assert.Same(second.Topology, third.SourceTopology); Assert.Same(second.Surface, third.Source);
        foreach (var pair in E(second.Representatives).Cast<Relation>()) Assert.Contains(pair.Left, E(first.Faces));
        foreach (var pair in E(third.Representatives).Cast<Relation>()) Assert.Contains(pair.Left, E(second.Faces));
        Assert.Equal(16, E(source.Faces).Length); Assert.Equal(25, E(source.Graph.Sites).Length);
    }

    [Fact]
    public void MissingExteriorPremiseCannotBeRepairedByInventingRepresentativeGeometry()
    {
        var valid = Grid(2, 2);
        var source = new ScaffoldQuestionScaffold(valid.Graph, valid.Boundaries, S(), new Placeholder());
        var result = new OrnamentDerivedScaffold(source, new Placeholder());
        Assert.False(result.IsComplete); Assert.NotEmpty(E(result.Unresolved));
        Assert.Empty(E(result.Sites)); Assert.Empty(E(result.Edges)); Assert.Empty(E(result.Faces));
        var next = new OrnamentDerivedScaffold(result, new Placeholder());
        Assert.False(next.IsComplete); Assert.NotEmpty(E(next.Unresolved)); Assert.Empty(E(next.Sites));
    }

    [Fact]
    public void ReadersRejectCopiedClaimsAndTheSourceOuterFaceIsNeverAnInteriorRepresentative()
    {
        var result = new OrnamentDerivedScaffold(Grid(2, 2), new Placeholder());
        var representative = (Relation)E(result.Representatives)[0]; var edge = (Relation)E(result.Edges)[0];
        var cell = (Relation)E(result.Faces)[0];
        Assert.False(result.TryReadRepresentative(new Relation(representative.Left, representative.Right), out _, out _));
        Assert.False(result.TryReadEdge(new Relation(edge.Left, edge.Right), out _, out _, out _, out _));
        Assert.False(result.TryReadCell(new Relation(cell.Left, cell.Right), out _, out _, out _));
        Assert.Equal(4, E(result.Representatives).Length); Assert.Equal(4, E(result.Sites).Length);
    }

    [Fact]
    public void WholeSourceAndPriorBoundaryRemainConnectedRatherThanPromisingExactReconstructionFromAdjacency()
    {
        var source = Grid(3, 2); var first = new OrnamentDerivedScaffold(source, new Placeholder());
        var second = new OrnamentDerivedScaffold(first, new Placeholder());
        Assert.Contains(source.Evaluate(), Reach(first.Surface));
        Assert.Contains(source.Graph.Surface, Reach(first.Surface));
        Assert.Contains(first.Surface, Reach(second.Surface)); Assert.Contains(first.Boundary, Reach(second.Retained));
        Assert.Contains(first.Topology, Reach(second.Surface));
    }

    private static (int, int, int) Size(OrnamentDerivedScaffold read) => (E(read.Sites).Length, E(read.Edges).Length, E(read.Faces).Length);

    private static ScaffoldQuestionScaffold Grid(int columns, int rows)
    {
        var f = new QualitativeMotionFrame(); var reach = new Placeholder();
        var sites = new IElement[rows + 1, columns + 1];
        for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++) sites[y, x] = new Placeholder();
        var horizontal = new Relation[rows + 1, columns]; var vertical = new Relation[rows, columns + 1];
        var allEdges = new List<IElement>();
        for (var y = 0; y <= rows; y++) for (var x = 0; x < columns; x++)
        { horizontal[y, x] = QualitativeStrokeGraph.Stroke(new Placeholder(), sites[y, x], sites[y, x + 1], S(QualitativeMotionFrame.Move(f.Right, reach))); allEdges.Add(horizontal[y, x]); }
        for (var y = 0; y < rows; y++) for (var x = 0; x <= columns; x++)
        { vertical[y, x] = QualitativeStrokeGraph.Stroke(new Placeholder(), sites[y, x], sites[y + 1, x], S(QualitativeMotionFrame.Move(f.Up, reach))); allEdges.Add(vertical[y, x]); }
        var faces = new List<IElement>();
        for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++)
            faces.Add(S(O(horizontal[y, x], sites[y, x], sites[y, x + 1]), O(vertical[y, x + 1], sites[y, x + 1], sites[y + 1, x + 1]),
                O(horizontal[y + 1, x], sites[y + 1, x + 1], sites[y + 1, x]), O(vertical[y, x], sites[y + 1, x], sites[y, x])));
        var outer = new List<IElement>();
        for (var y = 0; y < rows; y++) outer.Add(O(vertical[y, 0], sites[y, 0], sites[y + 1, 0]));
        for (var x = 0; x < columns; x++) outer.Add(O(horizontal[rows, x], sites[rows, x], sites[rows, x + 1]));
        for (var y = rows - 1; y >= 0; y--) outer.Add(O(vertical[y, columns], sites[y + 1, columns], sites[y, columns]));
        for (var x = columns - 1; x >= 0; x--) outer.Add(O(horizontal[0, x], sites[0, x + 1], sites[0, x]));
        var graph = new QualitativeStrokeGraph(f, S(sites.Cast<IElement>().ToArray()), S(allEdges.ToArray()));
        var result = new ScaffoldQuestionScaffold(graph, S(faces.ToArray()), S(S(outer.ToArray())), new Placeholder());
        Assert.True(result.IsComplete); return result;
    }

    private static (ScaffoldQuestionScaffold, IElement) Fan(int sides)
    {
        var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
        var points = sides == 3 ? new[] { (0, 0), (4, 0), (0, 4) } : new[] { (0, 0), (4, 0), (4, 4), (0, 4) };
        var centerPoint = sides == 3 ? (1, 1) : (2, 2); var center = new Placeholder();
        var vertices = points.Select(_ => (IElement)new Placeholder()).ToArray(); var perimeter = new Relation[sides]; var spokes = new Relation[sides];
        Relation Word((int X, int Y) a, (int X, int Y) b)
        {
            var terms = new List<IElement>();
            for (var i = 0; i < Math.Abs(b.X - a.X); i++) terms.Add(QualitativeMotionFrame.Move(b.X > a.X ? frame.Right : frame.Left, reach));
            for (var i = 0; i < Math.Abs(b.Y - a.Y); i++) terms.Add(QualitativeMotionFrame.Move(b.Y > a.Y ? frame.Up : frame.Down, reach));
            return S(terms.ToArray());
        }
        for (var i = 0; i < sides; i++)
        {
            perimeter[i] = QualitativeStrokeGraph.Stroke(new Placeholder(), vertices[i], vertices[(i + 1) % sides], Word(points[i], points[(i + 1) % sides]));
            spokes[i] = QualitativeStrokeGraph.Stroke(new Placeholder(), center, vertices[i], Word(centerPoint, points[i]));
        }
        var faces = Enumerable.Range(0, sides).Select(i => (IElement)S(O(spokes[i], center, vertices[i]),
            O(perimeter[i], vertices[i], vertices[(i + 1) % sides]), O(spokes[(i + 1) % sides], vertices[(i + 1) % sides], center))).ToArray();
        var outside = Enumerable.Range(0, sides).Reverse().Select(i => (IElement)O(perimeter[i], vertices[(i + 1) % sides], vertices[i])).ToArray();
        return (new ScaffoldQuestionScaffold(new QualitativeStrokeGraph(frame, S(vertices.Append(center).ToArray()), S(perimeter.Concat(spokes).Cast<IElement>().ToArray())),
            S(faces), S(S(outside)), new Placeholder()), center);
    }

    private static Relation O(Relation edge, IElement from, IElement to) => ScaffoldQuestionScaffold.Oriented(edge, from, to);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            if (item is Relation relation) { work.Push(relation.Left); work.Push(relation.Right); }
        }
        return seen;
    }
    private static void AssertNoNumbers(IElement source) => Assert.DoesNotContain(Reach(source), item => item is Carrier or CarrierLocation);
}
