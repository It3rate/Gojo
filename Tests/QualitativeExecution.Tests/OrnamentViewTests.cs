using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class OrnamentViewTests
{
    [Fact]
    public void OverlappingGroupsRemainDistinctAndDoNotBecomeATransitivePartition()
    {
        var f = new Quilt(4, 1);
        var a = OrnamentView.Group(new Placeholder(), f.Faces[0], f.Faces[1]);
        var b = OrnamentView.Group(new Placeholder(), f.Faces[1], f.Faces[2]);
        var c = OrnamentView.Group(new Placeholder(), f.Faces[2], f.Faces[3]);
        var view = new OrnamentView(f.Source, S(a, b, c), new Placeholder());
        Assert.True(view.IsComplete); Assert.Equal(3, E(view.Groups).Length);
        Assert.Equal(2, E(view.Overlaps).Length); Assert.Single(E(view.Adjacency));
        Assert.Equal(4, E(view.SelectedFaces).Length); Assert.Empty(E(view.Complement));
        foreach (var group in Groups(view))
        {
            Assert.Equal(2, E(group.Faces).Length); Assert.Single(E(group.Internal));
            Assert.Equal(7, E(group.Strokes).Length); Assert.Equal(6, E(group.Boundary).Length);
        }
        var adjacency = (Relation)E(view.Adjacency)[0];
        Assert.True(view.TryReadAdjacency(adjacency, out var first, out var third, out var seamRows));
        Assert.Same(a, first.Definition); Assert.Same(c, third.Definition); Assert.Single(E(seamRows));
        AssertNoNumbers(view.Evaluate());
    }

    [Fact]
    public void InternalBoundaryCutAndComplementKeepActualSourceIncidence()
    {
        var f = new Quilt(3, 1); var group = OrnamentView.Group(new Placeholder(), f.Faces[0], f.Faces[1]);
        var view = new OrnamentView(f.Source, S(group), new Placeholder()); var parts = Assert.Single(Groups(view));
        Assert.Single(E(parts.Internal)); Assert.Equal(6, E(parts.Boundary).Length);
        Assert.Single(E(parts.Cut)); Assert.Single(E(parts.Complement)); Assert.Same(f.Faces[2], E(parts.Complement)[0]);
        Assert.Single(E(view.Complement));
        foreach (var seam in E(parts.Internal).Concat(E(parts.Boundary)).Cast<Relation>())
        {
            Assert.Contains(seam.Left, E(f.Source.Graph.Strokes));
            Assert.Equal(2, E((Relation)seam.Right).Length);
            foreach (var incidence in E((Relation)seam.Right).Cast<Relation>())
            {
                Assert.True(f.Source.TryReadFace((Relation)incidence.Left, out var boundary, out _));
                Assert.Contains(incidence.Right, E(boundary)); Assert.Same(seam.Left, ((Relation)incidence.Right).Left);
            }
        }
    }

    [Fact]
    public void ASecondQuestionCanShareAllMembersWithoutChangingTheSource()
    {
        var f = new Quilt(2, 2); var sourceRead = f.Source.Evaluate(); var strokes = f.Source.Graph.Strokes;
        var definition = OrnamentView.Group(new Placeholder(), f.Faces[0], f.Faces[1]);
        var a = new OrnamentView(f.Source, S(definition), new Placeholder());
        var b = new OrnamentView(f.Source, S(definition), new Placeholder());
        Assert.True(a.IsComplete); Assert.True(b.IsComplete); Assert.NotSame(a.Surface, b.Surface);
        Assert.NotSame(E(a.Groups)[0], E(b.Groups)[0]);
        Assert.Same(sourceRead, f.Source.Evaluate()); Assert.Same(strokes, f.Source.Graph.Strokes);
        Assert.Equal(12, E(strokes).Length); Assert.Equal(4, E(f.Source.Faces).Length);
        var correspondence = new OrnamentViewOverlap(a, b, new Placeholder());
        var match = Assert.Single(Correspondences(correspondence));
        Assert.Equal(2, E(match.SharedFaces).Length); Assert.Empty(E(match.LeftOnlyFaces)); Assert.Empty(E(match.RightOnlyFaces));
        Assert.Same(a.Context, a.Surface.Right); Assert.Same(b.Context, b.Surface.Right);
        AssertNoNumbers(correspondence.Evaluate());
    }

    [Fact]
    public void DifferentCoversGiveManyToManyCorrespondenceAndExclusiveParts()
    {
        var f = new Quilt(4, 1);
        var a = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[0], f.Faces[1]),
            OrnamentView.Group(new Placeholder(), f.Faces[2], f.Faces[3])), new Placeholder());
        var b = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[1], f.Faces[2])), new Placeholder());
        var comparison = new OrnamentViewOverlap(a, b, new Placeholder());
        Assert.True(comparison.IsComplete);
        var matches = Correspondences(comparison); Assert.Equal(2, matches.Length);
        foreach (var match in matches)
        {
            Assert.Single(E(match.SharedFaces)); Assert.Single(E(match.LeftOnlyFaces)); Assert.Single(E(match.RightOnlyFaces));
            Assert.Equal(4, E(match.SharedStrokes).Length); Assert.Equal(3, E(match.LeftOnlyStrokes).Length);
            Assert.Equal(3, E(match.RightOnlyStrokes).Length);
            Assert.Contains(match.Left.Read, E(a.Groups)); Assert.Contains(match.Right.Read, E(b.Groups));
        }
        Assert.Same(f.Faces[1], E(matches[0].SharedFaces)[0]); Assert.Same(f.Faces[2], E(matches[1].SharedFaces)[0]);
    }

    [Fact]
    public void SharedBoundaryAndSharedSiteDoNotInventSharedChambers()
    {
        var f = new Quilt(2, 2);
        OrnamentView Single(int index) => new(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[index])), new Placeholder());
        var sharedEdge = Assert.Single(Correspondences(new OrnamentViewOverlap(Single(0), Single(1), new Placeholder())));
        Assert.Empty(E(sharedEdge.SharedFaces)); Assert.Single(E(sharedEdge.SharedStrokes)); Assert.Equal(2, E(sharedEdge.SharedSites).Length);
        var sharedSite = Assert.Single(Correspondences(new OrnamentViewOverlap(Single(0), Single(3), new Placeholder())));
        Assert.Empty(E(sharedSite.SharedFaces)); Assert.Empty(E(sharedSite.SharedStrokes)); Assert.Single(E(sharedSite.SharedSites));
        var twoDiagonal = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[0]),
            OrnamentView.Group(new Placeholder(), f.Faces[3])), new Placeholder());
        Assert.Empty(E(twoDiagonal.Adjacency)); Assert.Empty(E(twoDiagonal.Overlaps));
    }

    [Fact]
    public void SeparateSitesAndEdgesRemainUnrelatedDespiteSameSource()
    {
        var f = new Quilt(3, 1);
        var left = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[0])), new Placeholder());
        var right = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[2])), new Placeholder());
        var comparison = new OrnamentViewOverlap(left, right, new Placeholder());
        Assert.Empty(E(comparison.Correspondences)); Assert.Single(E(comparison.Unrelated));
        Assert.True(comparison.IsComplete);
    }

    [Fact]
    public void ForeignCopiedExteriorAndDuplicateFacesCannotCreateAGroup()
    {
        var f = new Quilt(2, 1); var other = new Quilt(2, 1);
        var copied = new Relation(f.Faces[0].Left, f.Faces[0].Right);
        foreach (var selection in new[] { S(other.Faces[0]), S(copied), S(E(f.Source.Exterior)), S(f.Faces[0], f.Faces[0]), S() })
        {
            var view = new OrnamentView(f.Source, S(new Relation(new Placeholder(), selection)), new Placeholder());
            Assert.False(view.IsComplete); Assert.Empty(E(view.Groups)); Assert.NotEmpty(E(view.Blocked));
            Assert.Equal(2, E(view.Complement).Length);
        }
    }

    [Fact]
    public void OpenMembershipDoesNotPublishAnExactPrefixGroup()
    {
        var f = new Quilt(2, 1);
        var definition = new Relation(new Placeholder(), StructuralConstraint.Open(f.Faces[0]));
        var view = new OrnamentView(f.Source, S(definition), new Placeholder());
        Assert.False(view.IsComplete); Assert.NotEmpty(E(view.Unresolved)); Assert.Empty(E(view.Groups));
        Assert.Empty(E(view.SelectedFaces)); Assert.Equal(2, E(view.Complement).Length);
    }

    [Fact]
    public void OpenDefinitionScopeAndDuplicatedIdentityDoNotChooseAFirstMeaning()
    {
        var f = new Quilt(2, 1); var identity = new Placeholder();
        var a = OrnamentView.Group(identity, f.Faces[0]); var b = OrnamentView.Group(identity, f.Faces[1]);
        var duplicate = new OrnamentView(f.Source, S(a, b), new Placeholder());
        Assert.False(duplicate.IsComplete); Assert.NotEmpty(E(duplicate.Blocked)); Assert.Empty(E(duplicate.Groups));
        var open = new OrnamentView(f.Source, StructuralConstraint.Open(a), new Placeholder());
        Assert.False(open.IsComplete); Assert.NotEmpty(E(open.Unresolved)); Assert.Empty(E(open.Groups));
    }

    [Fact]
    public void AClosedEmptyViewHasAnExplicitCompleteComplement()
    {
        var f = new Quilt(2, 1); var view = new OrnamentView(f.Source, S(), new Placeholder());
        Assert.True(view.IsComplete); Assert.Empty(E(view.Groups)); Assert.Empty(E(view.SelectedFaces)); Assert.Equal(2, E(view.Complement).Length);
    }

    [Fact]
    public void ANewEmbeddingIsNotTheSameCapturedSourceEvenWhenItsGraphIsReused()
    {
        var f = new Quilt(2, 1); var revised = new ScaffoldQuestionScaffold(f.Source.Graph,
            f.Source.Boundaries, f.Source.ExteriorBoundaries, new Placeholder());
        var left = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[0])), new Placeholder());
        var right = new OrnamentView(revised, S(OrnamentView.Group(new Placeholder(), (Relation)E(revised.Faces)[0])), new Placeholder());
        var comparison = new OrnamentViewOverlap(left, right, new Placeholder());
        Assert.False(comparison.IsComplete); Assert.Single(E(comparison.Blocked)); Assert.Empty(E(comparison.Correspondences));
    }

    [Fact]
    public void CopiedGroupAdjacencyAndCorrespondenceRowsCannotForgeActualReadEvidence()
    {
        var f = new Quilt(2, 1);
        var view = new OrnamentView(f.Source, S(OrnamentView.Group(new Placeholder(), f.Faces[0]),
            OrnamentView.Group(new Placeholder(), f.Faces[1])), new Placeholder());
        var group = (Relation)E(view.Groups)[0]; Assert.False(view.TryReadGroup(new(group.Left, group.Right), out _));
        var adjacent = (Relation)E(view.Adjacency)[0];
        Assert.False(view.TryReadAdjacency(new(adjacent.Left, adjacent.Right), out _, out _, out _));
        var comparison = new OrnamentViewOverlap(view, view, new Placeholder()); var pair = (Relation)E(comparison.Correspondences)[0];
        Assert.False(comparison.TryReadCorrespondence(new(pair.Left, pair.Right), out _));
    }

    private static OrnamentViewGroup[] Groups(OrnamentView view) => E(view.Groups).Cast<Relation>().Select(group =>
    { Assert.True(view.TryReadGroup(group, out var parts)); return parts; }).ToArray();
    private static OrnamentViewCorrespondence[] Correspondences(OrnamentViewOverlap view) => E(view.Correspondences).Cast<Relation>().Select(group =>
    { Assert.True(view.TryReadCorrespondence(group, out var parts)); return parts; }).ToArray();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; Assert.False(element is Carrier or CarrierLocation);
            if (element is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
    }

    private sealed class Quilt
    {
        internal ScaffoldQuestionScaffold Source { get; }
        internal Relation[] Faces => E(Source.Faces).Cast<Relation>().ToArray();
        internal Quilt(int columns, int rows)
        {
            var frame = new QualitativeMotionFrame(); var reach = new Placeholder();
            var sites = new IElement[columns + 1, rows + 1];
            for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++) sites[x, y] = new Placeholder();
            var edges = new Dictionary<(int X, int Y, bool Vertical), Relation>();
            Relation Edge(int x, int y, bool vertical)
            {
                var edge = QualitativeStrokeGraph.Stroke(new Placeholder(), sites[x, y],
                    sites[x + (vertical ? 0 : 1), y + (vertical ? 1 : 0)],
                    S(QualitativeMotionFrame.Move(vertical ? frame.Up : frame.Right, reach)));
                edges[(x, y, vertical)] = edge; return edge;
            }
            for (var y = 0; y <= rows; y++) for (var x = 0; x < columns; x++) Edge(x, y, false);
            for (var y = 0; y < rows; y++) for (var x = 0; x <= columns; x++) Edge(x, y, true);
            Relation O(int x, int y, bool vertical, bool forward)
            {
                var edge = edges[(x, y, vertical)]; var a = sites[x, y]; var b = sites[x + (vertical ? 0 : 1), y + (vertical ? 1 : 0)];
                return ScaffoldQuestionScaffold.Oriented(edge, forward ? a : b, forward ? b : a);
            }
            var boundaries = new List<IElement>();
            for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++)
                boundaries.Add(ScaffoldQuestionScaffold.Boundary(O(x, y, false, true), O(x + 1, y, true, true),
                    O(x, y + 1, false, false), O(x, y, true, false)));
            var outside = new List<Relation>();
            for (var x = columns - 1; x >= 0; x--) outside.Add(O(x, 0, false, false));
            for (var y = 0; y < rows; y++) outside.Add(O(0, y, true, true));
            for (var x = 0; x < columns; x++) outside.Add(O(x, rows, false, true));
            for (var y = rows - 1; y >= 0; y--) outside.Add(O(columns, y, true, false));
            var graph = new QualitativeStrokeGraph(frame, S(sites.Cast<IElement>().ToArray()), S(edges.Values.Cast<IElement>().ToArray()));
            Source = new(graph, S(boundaries.ToArray()), S(ScaffoldQuestionScaffold.Boundary(outside.ToArray())), new Placeholder());
            Assert.True(Source.IsComplete);
        }
    }
}
