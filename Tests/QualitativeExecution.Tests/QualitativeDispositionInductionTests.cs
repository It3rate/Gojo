using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeDispositionInductionTests
{
    [Fact]
    public void TwoObservedChannelsDiscoverActualCommutingGridCircuitsWithoutFaces()
    {
        var f = Grid(3, 2); var induction = new QualitativeDispositionInduction(f.Observation());
        Assert.True(induction.IsComplete); Assert.Equal(2, E(induction.DirectionFamilies).Length);
        var candidate = Assert.Single(Candidates(induction));
        Assert.Equal(2, E(candidate.Basis).Length); Assert.Equal(17, E(candidate.SelectedSegments).Length);
        Assert.Equal(6, E(candidate.Commutations).Length); Assert.Empty(E(candidate.Residue));
        Assert.Empty(E(candidate.Closures)); Assert.Empty(E(candidate.Unsettled));
        foreach (var item in E(candidate.Commutations))
        {
            var proof = Assert.IsType<Relation>(item); var paths = Assert.IsType<Relation>(proof.Right);
            var first = E((Relation)paths.Left).Cast<Relation>().ToArray(); var second = E((Relation)paths.Right).Cast<Relation>().ToArray();
            Assert.Equal(2, first.Length); Assert.Equal(2, second.Length);
            Assert.Same(End(first[1]), End(second[1]));
            Assert.Same(proof.Left, Start(first[0])); Assert.Same(proof.Left, Start(second[0]));
        }
        AssertNoNumbers(induction.Surface);
    }

    [Fact]
    public void SlantedGridHasTheSameLocalCommutationLawWithoutRightAngleClaim()
    {
        var f = Grid(4, 3, shear: true); var induction = new QualitativeDispositionInduction(f.Observation());
        var candidate = Assert.Single(Candidates(induction));
        Assert.Equal(12, E(candidate.Commutations).Length); Assert.Empty(E(candidate.Residue));
        Assert.Equal(2, E(candidate.Basis).Length); Assert.Empty(E(induction.TriangleCircuits));
        Assert.Contains(E(induction.DirectionFamilies).Cast<Relation>(), family => E((Relation)family.Left).Length == 2);
    }

    [Fact]
    public void TriangularFieldKeepsBothCoarseGridAndThreeDirectionExplanation()
    {
        var f = Grid(3, 2, diagonals: true); var induction = new QualitativeDispositionInduction(f.Observation());
        var candidates = Candidates(induction);
        Assert.Equal(3, E(induction.DirectionFamilies).Length); Assert.Equal(12, E(induction.TriangleCircuits).Length);
        var coarse = candidates.First(candidate => E(candidate.Basis).Length == 2 &&
            E(candidate.Commutations).Length == 6 && E(candidate.Residue).Length == 6);
        Assert.Equal(12, E(coarse.UnexpectedClosures).Length); Assert.Empty(E(coarse.Closures));
        var refined = Assert.Single(candidates, candidate => E(candidate.Basis).Length == 3);
        Assert.Empty(E(refined.Residue)); Assert.Equal(12, E(refined.Closures).Length);
        Assert.Empty(E(refined.UnexpectedClosures)); Assert.Contains(E(induction.Candidates), row => ReferenceEquals(row, coarse.Read));
        foreach (var row in E(coarse.Residue)) Assert.Contains(f.Edges, edge => ReferenceEquals(row, edge));
    }

    [Fact]
    public void AlternatingRowTransportInducesPhaseChannelAndComposedDisposition()
    {
        var f = Grid(3, 4, staggered: true); var induction = new QualitativeDispositionInduction(f.Observation());
        Assert.Equal(3, E(induction.DirectionFamilies).Length); Assert.Empty(E(induction.TriangleCircuits));
        var rhythms = Rhythms(induction);
        Assert.Contains(rhythms, rhythm => E(rhythm.Routes).Length == 4 &&
            E(rhythm.SelectedSegments).Length == 16 && E(rhythm.Routes).Cast<Relation>().All(route => QualitativeDispositionInduction.RouteUses(route).Length == 4));
        var composed = Candidates(induction).Where(candidate => E(candidate.Basis).Length == 2 &&
            E(candidate.Residue).Length == 0 && E(candidate.Commutations).Length == 12).ToArray();
        Assert.NotEmpty(composed);
        Assert.All(composed, candidate => Assert.Contains(E(candidate.Basis), basis =>
            E(induction.Rhythms).Any(rhythm => ReferenceEquals(rhythm, basis))));
        Assert.All(composed, candidate => Assert.Empty(E(candidate.Closures)));
    }

    [Fact]
    public void AlternatingHeadingRhythmIsInducedFromActualRouteNotSuppliedRecipe()
    {
        var f = new Fixture(); var sites = Enumerable.Range(0, 9).Select(_ => f.Site()).ToArray();
        for (var i = 0; i < 8; i++) f.Edge(sites[i], sites[i + 1], f.Frame.Right, i % 2 == 0 ? f.Frame.Up : f.Frame.Down);
        var induction = new QualitativeDispositionInduction(f.Observation());
        var full = Rhythms(induction).Where(rhythm => E(rhythm.SelectedSegments).Length == 8).ToArray();
        Assert.Equal(2, full.Length); // The two actual traversal orientations survive.
        foreach (var rhythm in full)
        {
            var route = Assert.IsType<Relation>(Assert.Single(E(rhythm.Routes)));
            var uses = QualitativeDispositionInduction.RouteUses(route); Assert.Equal(8, uses.Length);
            Assert.Equal(7, E((Relation)route.Right).Length); Assert.Empty(E(rhythm.Residue));
            var phaseCycle = Assert.IsType<Relation>(rhythm.Phases);
            Assert.Equal(2, E((Relation)phaseCycle.Left).Length);
            Assert.Equal(8, E((Relation)phaseCycle.Right).Length);
            for (var i = 0; i + 1 < uses.Length; i++) Assert.Same(End(uses[i]), Start(uses[i + 1]));
        }
    }

    [Fact]
    public void CrossingObservationDoesNotCreateAJoinOrAnUnobservedRoute()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site(); var d = f.Site();
        var first = f.Edge(a, b, f.Frame.Right, f.Frame.Up); var second = f.Edge(c, d, f.Frame.Right, f.Frame.Down);
        var crossing = new Relation(first, second);
        var observation = new QualitativeRecognitionObservation(f.Frame, S(f.Sites.ToArray()), S(f.Edges.ToArray()), crossings: S(crossing));
        var induction = new QualitativeDispositionInduction(observation);
        Assert.Empty(E(induction.Rhythms)); Assert.Empty(E(induction.TriangleCircuits));
        Assert.All(Candidates(induction), candidate => Assert.Empty(E(candidate.Commutations)));
        Assert.Same(observation.Surface, ((Relation)induction.Definition.Left).Left);
    }

    [Fact]
    public void KnownSubsetSurvivesAnOpenHeadingAndUnknownSegmentStaysResidue()
    {
        var f = Grid(2, 2); var a = f.Site(); var b = f.Site();
        var unknown = new Relation(new Placeholder(), new Relation(new Relation(a, b), StructuralConstraint.Open())); f.Edges.Add(unknown);
        var induction = new QualitativeDispositionInduction(f.Observation());
        Assert.False(induction.IsComplete); Assert.NotEmpty(E(induction.Unresolved));
        var candidate = Assert.Single(Candidates(induction));
        Assert.Equal(4, E(candidate.Commutations).Length);
        Assert.Same(unknown, Assert.Single(E(candidate.Residue)));
    }

    [Fact]
    public void TwoPathsWithDifferentNativeEndpointsExposeUnsettledCommutation()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site(); var d = f.Site(); var e = f.Site();
        f.Edge(a, b, f.Frame.Right); f.Edge(a, c, f.Frame.Up);
        f.Edge(b, d, f.Frame.Up); f.Edge(c, e, f.Frame.Right);
        var induction = new QualitativeDispositionInduction(f.Observation()); var candidate = Assert.Single(Candidates(induction));
        Assert.Empty(E(candidate.Commutations)); var unsettled = Assert.IsType<Relation>(Assert.Single(E(candidate.Unsettled)));
        Assert.Same(d, ((Relation)unsettled.Right).Left); Assert.Same(e, ((Relation)unsettled.Right).Right);
        Assert.True(induction.IsComplete); // A completely read mismatch is not unknown search.
    }

    [Fact]
    public void BranchingChannelsKeepAlternativesInsteadOfSelectingAConvenientSquare()
    {
        var f = Grid(2, 2); var a = f.Sites[0]; var fork = f.Site(); f.Edge(a, fork, f.Frame.Right);
        var induction = new QualitativeDispositionInduction(f.Observation()); var candidate = Assert.Single(Candidates(induction));
        Assert.NotEmpty(E(candidate.Competing)); Assert.Equal(3, E(candidate.Commutations).Length);
        Assert.Empty(E(candidate.Residue));
    }

    [Fact]
    public void BudgetExhaustionRetainsProposalsAsUnsearchedRatherThanFalse()
    {
        var f = Grid(3, 2, diagonals: true); var induction = new QualitativeDispositionInduction(f.Observation(), capacity: 0);
        Assert.False(induction.IsComplete); Assert.Empty(E(induction.Spent)); Assert.Empty(E(induction.Candidates));
        Assert.NotEmpty(E(induction.Unsearched)); Assert.Equal(12, E(induction.TriangleCircuits).Length);
        foreach (var row in E(induction.Unsearched)) Assert.IsType<Placeholder>(((Relation)row).Right);
    }

    [Fact]
    public void ReaderRequiresTheActualCandidateAndRetainsTheReadSource()
    {
        var f = Grid(1, 1); var induction = new QualitativeDispositionInduction(f.Observation());
        var actual = Assert.IsType<Relation>(Assert.Single(E(induction.Candidates)));
        Assert.False(induction.TryReadCandidate(new Relation(actual.Left, actual.Right), out _));
        Assert.True(induction.TryReadCandidate(actual, out var read)); Assert.Same(actual, read.Read);
        Assert.Same(induction.Observation.Surface, ((Relation)induction.Definition.Left).Left);
    }

    [Fact]
    public void DuplicateSegmentIdentityCannotInflateSupportAndKnownSubsetSurvives()
    {
        var f = Grid(2, 2); var a = f.Site(); var b = f.Site(); var c = f.Site();
        var first = f.Edge(a, b, f.Frame.Right); var copiedIdentity = new Relation(first.Left,
            new Relation(new Relation(b, c), S(f.Frame.Right))); f.Edges.Add(copiedIdentity);
        var induction = new QualitativeDispositionInduction(f.Observation()); var candidate = Assert.Single(Candidates(induction));
        Assert.False(induction.IsComplete); Assert.NotEmpty(E(induction.Unresolved)); Assert.NotEmpty(E(induction.Blocked));
        Assert.Equal(4, E(candidate.Commutations).Length); Assert.Equal(12, E(candidate.SelectedSegments).Length);
        Assert.Contains(E(candidate.Residue), row => ReferenceEquals(first, row));
        Assert.Contains(E(candidate.Residue), row => ReferenceEquals(copiedIdentity, row));
    }

    [Fact]
    public void AliasedDuplicateSitesAndMalformedCrossingsRemainVisibleWithoutErasingKnownGrid()
    {
        var f = Grid(2, 2); var alias = (Placeholder)f.Site(); f.Sites.Add(alias.Dual().Dual());
        var other = f.Site(); var excluded = f.Edge(alias, other, f.Frame.Right);
        var badCrossing = new Relation(f.Edges[0], f.Edges[1]); // Adjacent rows share an actual endpoint.
        var observation = new QualitativeRecognitionObservation(f.Frame, S(f.Sites.ToArray()), S(f.Edges.ToArray()), crossings: S(badCrossing));
        Assert.False(observation.IsComplete);
        var induction = new QualitativeDispositionInduction(observation); var candidate = Assert.Single(Candidates(induction));
        Assert.False(induction.IsComplete); Assert.Contains(E(induction.Unresolved), row => ReferenceEquals(row, badCrossing));
        Assert.Equal(4, E(candidate.Commutations).Length); Assert.Same(excluded, Assert.Single(E(candidate.Residue)));
    }

    private static QualitativeDispositionCandidate[] Candidates(QualitativeDispositionInduction induction) =>
        E(induction.Candidates).Cast<Relation>().Select(row => { Assert.True(induction.TryReadCandidate(row, out var result)); return result; }).ToArray();
    private static QualitativeDispositionRhythm[] Rhythms(QualitativeDispositionInduction induction) =>
        E(induction.Rhythms).Cast<Relation>().Select(row => { Assert.True(induction.TryReadRhythm(row, out var result)); return result; }).ToArray();
    private static IElement Start(Relation use) => ((Relation)((Relation)use.Right).Left).Left;
    private static IElement End(Relation use) => ((Relation)((Relation)use.Right).Left).Right;
    private static Fixture Grid(int columns, int rows, bool shear = false, bool diagonals = false, bool staggered = false)
    {
        var f = new Fixture(); var sites = new IElement[columns + 1, rows + 1];
        for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++) sites[x, y] = f.Site();
        for (var y = 0; y <= rows; y++) for (var x = 0; x < columns; x++) f.Edge(sites[x, y], sites[x + 1, y], f.Frame.Right);
        for (var y = 0; y < rows; y++) for (var x = 0; x <= columns; x++)
        {
            var heading = shear ? new IElement[] { f.Frame.Right, f.Frame.Up } : staggered ?
                [y % 2 == 0 ? f.Frame.Right : f.Frame.Left, f.Frame.Up] : [f.Frame.Up];
            f.Edge(sites[x, y], sites[x, y + 1], heading);
        }
        if (diagonals) for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++)
            f.Edge(sites[x, y], sites[x + 1, y + 1], f.Frame.Right, f.Frame.Up);
        return f;
    }
    private sealed class Fixture
    {
        public QualitativeMotionFrame Frame { get; } = new();
        public List<IElement> Sites { get; } = [];
        public List<IElement> Edges { get; } = [];
        public IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        public Relation Edge(IElement from, IElement to, params IElement[] headings)
        { var edge = new Relation(new Placeholder(), new Relation(new Relation(from, to), S(headings))); Edges.Add(edge); return edge; }
        public QualitativeRecognitionObservation Observation() => new(Frame, S(Sites.ToArray()), S(Edges.ToArray()));
    }
    private static void AssertNoNumbers(IElement root)
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
