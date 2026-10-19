using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class LatticeLadderObservationTests
{
    [Fact]
    public void StandaloneGraphReadsGridAndTriangleCandidatesWithoutConstructionHistory()
    {
        var square = Grid(false); var grid = LatticeLadderObservation.Read(square.Graph, square.Bindings);
        Assert.True(grid.IsComplete);
        var basis = Assert.Single(grid.Summaries);
        Assert.Equal(2, basis.Channels); Assert.Equal(4, basis.Commutations);
        Assert.Equal(0, basis.Closures); Assert.Equal(0, basis.Residue); Assert.Equal(0, basis.Unsettled);
        AssertBlind(grid, square);

        var triangle = Grid(true); var refined = LatticeLadderObservation.Read(triangle.Graph, triangle.Bindings);
        Assert.True(refined.IsComplete);
        Assert.Contains(refined.Summaries, candidate => candidate.Channels == 2 && candidate.Commutations == 4 &&
            candidate.Residue == 4 && candidate.Closures == 0 && candidate.UnexpectedClosures == 8);
        var full = Assert.Single(refined.Summaries, candidate => candidate.Channels == 3);
        Assert.Equal(8, full.Closures); Assert.Equal(0, full.Residue); Assert.Equal(0, full.UnexpectedClosures);
        AssertBlind(refined, triangle);
    }

    [Fact]
    public void IncidenceSupportRetainsEveryParallelAndReversedOccurrenceBesideItsCoarserRead()
    {
        var f = Grid(false, reverseCopies: true);
        var graph = f.Graph;
        var raw = LatticeLadderObservation.Read(graph, f.Bindings);
        Assert.Equal(24, raw.SourceStrokeCount); Assert.Equal(24, raw.ObservedStrokeCount);
        Assert.Contains(raw.Summaries, candidate => candidate.Competing > 0);

        var support = LatticeLadderObservation.ReadIncidenceSupport(graph, f.Bindings);
        Assert.True(support.IsComplete); Assert.Equal(24, support.SourceStrokeCount); Assert.Equal(12, support.ObservedStrokeCount);
        Assert.Equal(24, E(support.Publisher.Observation.Segments).Length);
        Assert.Equal(24, E(support.SourceToSupport).Length); Assert.Equal(24, E(support.SegmentCorrespondence).Length);
        Assert.Equal(12, E(support.SourceToSupport).Cast<Relation>().Select(row => row.Right).Distinct(ReferenceEqualityComparer.Instance).Count());
        var candidate = Assert.Single(support.Summaries);
        Assert.Equal(4, candidate.Commutations); Assert.Equal(0, candidate.Competing); Assert.Equal(0, candidate.Residue);
        Assert.All(E(support.SourceToSupport).Cast<Relation>(), row =>
        {
            Assert.Contains(E(f.Graph.Strokes), stroke => ReferenceEquals(stroke, row.Left));
            Assert.Contains(E(support.Observation.Segments), segment => ReferenceEquals(segment, row.Right));
        });
        Assert.Contains(E(support.SegmentCorrespondence).Cast<Relation>(), row =>
        {
            var target = (Relation)row.Right; var frames = (Relation)target.Right;
            var beforeEnds = (Relation)((Relation)frames.Left).Left;
            var afterEnds = (Relation)((Relation)frames.Right).Left;
            return ReferenceEquals(beforeEnds.Left, afterEnds.Right) && ReferenceEquals(beforeEnds.Right, afterEnds.Left);
        });
        Assert.Same(graph.Surface, support.Source.Left);
        AssertBlind(support, f);
    }

    [Fact]
    public void IncidenceSupportNeverCoalescesCoordinateEqualButUnjoinedEndpoints()
    {
        var f = new Fixture();
        var a = f.Site(0, 0); var b = f.Site(1, 0); var c = f.Site(0, 0); var d = f.Site(1, 0);
        f.Edge(a, b); f.Edge(d, c);
        var support = LatticeLadderObservation.ReadIncidenceSupport(f.Graph, f.Bindings);
        Assert.True(support.IsComplete); Assert.Equal(4, E(support.Observation.Sites).Length);
        Assert.Equal(2, support.SourceStrokeCount); Assert.Equal(2, support.ObservedStrokeCount);
        Assert.Equal(2, E(support.SourceToSupport).Cast<Relation>().Select(row => row.Right).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Empty(support.Candidates); Assert.Empty(E(support.Observation.Continuations));
    }

    [Fact]
    public void DeclaredCrossingSurvivesSupportWithoutBecomingASharedSite()
    {
        var f = new Fixture(); var a = f.Site(0, 0); var b = f.Site(2, 2); var c = f.Site(0, 2); var d = f.Site(2, 0);
        var first = f.Edge(a, b); var second = f.Edge(c, d); f.Edge(b, a);
        var support = LatticeLadderObservation.ReadIncidenceSupport(f.Graph, f.Bindings,
            declaredCrossings: S(new Relation(first, second)));
        Assert.True(support.IsComplete); Assert.Equal(2, support.ObservedStrokeCount); Assert.Equal(4, E(support.Observation.Sites).Length);
        var crossing = Assert.IsType<Relation>(Assert.Single(E(support.Observation.Crossings)));
        Assert.True(support.Observation.TryReadCrossing(crossing, out _, out _));
        Assert.Empty(E(support.Observation.Continuations)); Assert.Empty(E(support.Induction.TriangleCircuits));
        Assert.All(support.Summaries, candidate => Assert.Equal(0, candidate.Commutations));
    }

    [Fact]
    public void ExhaustedProposalBudgetKeepsUnsearchedCandidatesVisible()
    {
        var f = Grid(true); var read = LatticeLadderObservation.ReadIncidenceSupport(f.Graph, f.Bindings, capacity: 0);
        Assert.False(read.IsComplete); Assert.Empty(read.Candidates); Assert.NotEmpty(E(read.Unsearched));
        Assert.Equal(8, E(read.Induction.TriangleCircuits).Length);
        Assert.Empty(E(read.Blocked)); Assert.Empty(E(read.Unresolved));
    }

    [Fact]
    public void OpenStrokeInventoryKeepsKnownCommutationsWithoutClaimingACompleteGraph()
    {
        var f = Grid(false); var graph = f.Graph;
        var open = new QualitativeStrokeGraph(f.Frame, graph.Sites, StructuralConstraint.Open(E(graph.Strokes)));
        var read = LatticeLadderObservation.ReadIncidenceSupport(open, f.Bindings);
        Assert.False(read.IsComplete); Assert.NotEmpty(E(read.Unresolved));
        Assert.False(read.Observation.IsComplete);
        Assert.Contains(read.Summaries, candidate => candidate.Commutations == 4 && candidate.Unsettled == 0);
        Assert.Equal(12, read.SourceStrokeCount); Assert.Equal(12, read.ObservedStrokeCount);
    }

    [Theory]
    [InlineData("L00")]
    [InlineData("L05")]
    public void CrystallizedSplitSidedGridsRecoverThroughWitnessedStraightStrands(string id)
    {
        var source = new LatticeLadderRecipes(2, 2).Cases.Single(item => item.Id == id);
        var standalone = source.Ladder.Crystallize("standalone observation").Standalone;
        Assert.False(standalone.TryReadAncestor(source.Unit.Surface, out _, out _));
        var recipe = LatticeLadderAuthor.Rebuild(source.Recipe, standalone.Current.Program, 2, 2);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        var read = LatticeLadderObservation.ReadStrandSupport(run, field);
        Assert.True(read.IsComplete);
        Assert.True(read.SourceStrokeCount > read.IncidenceStrokeCount);
        Assert.True(read.IncidenceStrokeCount > read.ObservedStrokeCount);
        Assert.NotEmpty(E(read.CarriedSites));
        Assert.Equal(read.IncidenceStrokeCount, E(read.StrandMap).Length);
        Assert.Equal(read.SourceStrokeCount, E(read.SourceToSupport).Length);
        Assert.Contains(read.Summaries, candidate => candidate.Channels == 2 && candidate.Commutations == 4 &&
            candidate.Residue == 0 && candidate.Competing == 0 && candidate.Unsettled == 0);
        Assert.DoesNotContain(Reachable(read.Observation.Surface), item => ReferenceEquals(item, source.RungSurface));
        foreach (var row in E(read.StrandCorrespondence).Cast<Relation>())
        {
            var uses = E((Relation)row.Left).Cast<Relation>().ToArray();
            Assert.NotEmpty(uses);
            for (var i = 1; i < uses.Length; i++)
                Assert.Same(((Relation)uses[i - 1].Right).Right, ((Relation)uses[i].Right).Left);
            Assert.True(read.Observation.TryReadSegment((Relation)row.Right, out _, out var start, out var end, out _));
            Assert.Same(start, ((Relation)uses[0].Right).Left);
            Assert.Same(end, ((Relation)uses[^1].Right).Right);
        }
    }

    [Fact]
    public void StrandReadKeepsCornersTJunctionsAndUnprovedSameQuadrantBends()
    {
        var f = new Fixture();
        var a = f.Site(0, 0); var b = f.Site(1, 0); var c = f.Site(2, 0); var d = f.Site(1, 1);
        f.Edge(a, b); f.Edge(b, c); f.Edge(b, d); // T: retain the branch landmark.
        var e = f.Site(4, 0); var g = f.Site(5, 0); var h = f.Site(5, 1);
        f.Edge(e, g); f.Edge(g, h); // Corner: no opposite rays.
        var i = f.Site(8, 0); var j = f.Site(9, 1); var k = f.Site(11, 2);
        f.Edge(i, j); f.Edge(j, k); // Both NE, but the native rays are not proportional.
        var read = LatticeLadderObservation.ReadStrandSupport(f.Graph, f.Bindings);
        Assert.True(read.IsComplete); Assert.Empty(E(read.CarriedSites)); Assert.Equal(7, read.ObservedStrokeCount);
        Assert.Equal(10, E(read.Observation.Sites).Length);
    }

    [Theory]
    [InlineData("B15", 2, false)]
    [InlineData("B16", 3, true)]
    public void ExecutedNativeFieldsRecoverIncidenceBasisThroughTheirAcceptedSeams(string id, int channels, bool triangles)
    {
        var recipe = new BourgoinPatternLibrary().Recipes.Single(recipe => recipe.Id == id);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        var read = LatticeLadderObservation.ReadIncidenceSupport(run, field);
        Assert.True(field.IsComplete); Assert.True(read.IsComplete);
        Assert.NotEmpty(E(field.Accepted)); Assert.True(read.SourceStrokeCount > read.ObservedStrokeCount);
        Assert.Equal(read.SourceStrokeCount, E(read.SourceToSupport).Length);
        Assert.Contains(read.Summaries, candidate => candidate.Channels == channels && candidate.Commutations > 0 &&
            candidate.Competing == 0 && candidate.Unsettled == 0 && candidate.Residue == 0 && (candidate.Closures > 0) == triangles);
        Assert.DoesNotContain(Reachable(read.Observation.Surface), item => ReferenceEquals(item, run.Surface));
        Assert.DoesNotContain(Reachable(read.Observation.Surface), item => ReferenceEquals(item, field.Surface));
    }

    private static Fixture Grid(bool diagonals, bool reverseCopies = false)
    {
        var f = new Fixture(); var sites = new IElement[3, 3];
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) sites[x, y] = f.Site(x, y);
        void Edge(IElement a, IElement b) { f.Edge(a, b); if (reverseCopies) f.Edge(b, a); }
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++)
        {
            if (x < 2) Edge(sites[x, y], sites[x + 1, y]);
            if (y < 2) Edge(sites[x, y], sites[x, y + 1]);
            if (diagonals && x < 2 && y < 2) Edge(sites[x, y], sites[x + 1, y + 1]);
        }
        return f;
    }

    private static void AssertBlind(LatticeLadderObservation read, Fixture source)
    {
        var observed = Reachable(read.Observation.Surface).ToArray();
        Assert.DoesNotContain(observed, item => ReferenceEquals(item, source.Reach));
        Assert.DoesNotContain(observed, item => ReferenceEquals(item, source.Frame.Surface));
        foreach (var site in source.Sites) Assert.DoesNotContain(observed, item => ReferenceEquals(item, site));
        foreach (var stroke in source.Strokes) Assert.DoesNotContain(observed, item => ReferenceEquals(item, stroke));
    }

    private static IEnumerable<IElement> Reachable(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(source);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }

    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public IElement Reach { get; } = new Placeholder();
        public List<IElement> Sites { get; } = [];
        public List<IElement> Strokes { get; } = [];
        private readonly Dictionary<IElement, (int X, int Y)> coordinates = new(ReferenceEqualityComparer.Instance);
        public QualitativeStrokeGraph Graph => new(Frame, S(Sites.ToArray()), S(Strokes.ToArray()));
        public Relation Bindings => S(Sites.Select(site => (IElement)new Relation(site, Word(coordinates[site]))).ToArray());
        public IElement Site(int x, int y)
        { var site = new Placeholder(); Sites.Add(site); coordinates.Add(site, (x, y)); return site; }
        public Relation Edge(IElement from, IElement to)
        {
            var a = coordinates[from]; var b = coordinates[to];
            var edge = QualitativeStrokeGraph.Stroke(new Placeholder(), from, to, Word((b.X - a.X, b.Y - a.Y)));
            Strokes.Add(edge); return edge;
        }
        private Relation Word((int X, int Y) point) => S(Enumerable.Repeat(point.X < 0 ? Frame.Left : Frame.Right, Math.Abs(point.X))
            .Concat(Enumerable.Repeat(point.Y < 0 ? Frame.Down : Frame.Up, Math.Abs(point.Y)))
            .Select(direction => (IElement)QualitativeMotionFrame.Move(direction, Reach)).ToArray());
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
