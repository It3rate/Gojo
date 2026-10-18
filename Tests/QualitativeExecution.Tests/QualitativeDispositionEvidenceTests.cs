using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeDispositionEvidenceTests
{
    [Fact]
    public void MatchingDirectionCountsWithoutConnectionsCannotEstablishFlowOrCommutations()
    {
        var f = new Fixture();
        for (var i = 0; i < 4; i++) f.Edge(f.Site(), f.Site(), i < 2 ? [f.Frame.Right] : [f.Frame.Up]);
        var evidence = new QualitativeDispositionEvidence(new(f.Observation())); var candidate = Assert.Single(evidence.Candidates);
        Assert.True(evidence.IsComplete); Assert.Equal(4, E(candidate.Admitted).Length);
        Assert.Empty(E(candidate.RouteSupport)); Assert.Empty(E(candidate.CircuitSupport)); Assert.Empty(E(candidate.Supported));
        Assert.Equal(4, E(candidate.AdmittedOnly).Length); Assert.Equal(4, E(candidate.Singletons).Length);
    }

    [Fact]
    public void ActualGridHasSeparateRunAndFourSiteCircuitWitnesses()
    {
        var f = Grid(3, 2); var induction = new QualitativeDispositionInduction(f.Observation());
        var evidence = new QualitativeDispositionEvidence(induction); var candidate = Assert.Single(evidence.Candidates);
        Assert.True(evidence.IsComplete); Assert.Equal(17, E(candidate.Supported).Length); Assert.Empty(E(candidate.AdmittedOnly));
        Assert.Equal(6, E(candidate.CircuitProofs).Length); Assert.Equal(17, E(candidate.CircuitSupport).Length);
        foreach (var item in E(candidate.CircuitProofs))
        {
            Assert.True(evidence.TryReadCircuitProof(candidate.Candidate.Read, (Relation)item, out var actual, out var segments, out var sites));
            Assert.Contains(E(candidate.Candidate.Commutations), source => ReferenceEquals(source, actual));
            Assert.Equal(4, E(segments).Length); Assert.Equal(4, E(sites).Length);
            Assert.Equal(4, E(segments).Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.Equal(4, E(sites).Distinct(ReferenceEqualityComparer.Instance).Count());
        }
        AssertNoNumbers(evidence.Surface);
    }

    [Fact]
    public void EqualEndpointWithCollapsedIntermediateRolesIsNotAFourSiteCommutation()
    {
        var f = new Fixture(); var a = f.Site(); var b = f.Site(); var c = f.Site();
        f.Edge(a, b, f.Frame.Right); f.Edge(a, b, f.Frame.Up);
        f.Edge(b, c, f.Frame.Right); f.Edge(b, c, f.Frame.Up);
        var induction = new QualitativeDispositionInduction(f.Observation());
        var candidate = Assert.Single(new QualitativeDispositionEvidence(induction).Candidates);
        Assert.Empty(E(candidate.Candidate.Commutations)); Assert.Empty(E(candidate.CircuitProofs));
        Assert.NotEmpty(E(candidate.Unsettled));
    }

    [Fact]
    public void PeriodicAdmissionDoesNotExplainATruncatedAlternatingFragment()
    {
        var f = new Fixture(); var main = Zigzag(f, 8); var shortRun = Zigzag(f, 3); Horizontal(f, 2);
        var induction = new QualitativeDispositionInduction(f.Observation()); var evidence = new QualitativeDispositionEvidence(induction);
        var candidate = Composed(evidence, induction, f.Frame.Right, f.Frame.Up, f.Frame.Down);
        Assert.Equal(13, E(candidate.Admitted).Length); Assert.Equal(10, E(candidate.RouteSupport).Length);
        Assert.Equal(3, E(candidate.AdmittedOnly).Length); Assert.Empty(E(candidate.CircuitSupport));
        Assert.All(shortRun, edge => Assert.Contains(E(candidate.AdmittedOnly), item => ReferenceEquals(item, edge)));
        Assert.All(main, edge => Assert.Contains(E(candidate.Supported), item => ReferenceEquals(item, edge)));
    }

    [Fact]
    public void PhaseForkRetainsBranchEvidenceAndDoesNotInventAFullBranchRoute()
    {
        var f = new Fixture(); var mainSites = Enumerable.Range(0, 9).Select(_ => f.Site()).ToArray();
        for (var i = 0; i < 8; i++) f.Edge(mainSites[i], mainSites[i + 1], f.Frame.Right, i % 2 == 0 ? f.Frame.Up : f.Frame.Down);
        var branch = new List<Relation>(); var start = mainSites[4];
        for (var i = 0; i < 3; i++) { var end = f.Site(); branch.Add(f.Edge(start, end, f.Frame.Right, i % 2 == 0 ? f.Frame.Up : f.Frame.Down)); start = end; }
        Horizontal(f, 2);
        var induction = new QualitativeDispositionInduction(f.Observation()); var evidence = new QualitativeDispositionEvidence(induction);
        var candidate = Composed(evidence, induction, f.Frame.Right, f.Frame.Up, f.Frame.Down);
        Assert.NotEmpty(E(candidate.Competing)); Assert.All(branch, edge => Assert.Contains(E(candidate.AdmittedOnly), actual => ReferenceEquals(edge, actual)));
        var phaseChannels = E(candidate.ChannelEvidence).Cast<Relation>().Where(row => E(induction.Rhythms).Any(rhythm => ReferenceEquals(rhythm, row.Left))).ToArray();
        Assert.Single(phaseChannels);
        Assert.NotEmpty(E(candidate.RouteProofs)); Assert.Empty(E(candidate.CircuitProofs));
    }

    [Fact]
    public void ValidOffsetRowsHaveFullQualifiedCoverageWithoutClaimingTriangleCells()
    {
        var f = Grid(3, 4, staggered: true); var induction = new QualitativeDispositionInduction(f.Observation());
        var evidence = new QualitativeDispositionEvidence(induction);
        var candidate = Assert.Single(evidence.Candidates.Where(candidate => E(candidate.Candidate.Basis).Length == 2 &&
            E(candidate.CircuitProofs).Length == 12 && E(candidate.Outside).Length == 0), candidate =>
                E(candidate.Candidate.Basis).Any(channel => induction.TryReadRhythm((Relation)channel, out var rhythm) &&
                    E(rhythm.Headings).Cast<Relation>().All(heading => E(heading).Any(direction => ReferenceEquals(direction, f.Frame.Up)))));
        Assert.Equal(31, E(candidate.Supported).Length); Assert.Empty(E(candidate.AdmittedOnly)); Assert.Empty(E(candidate.TriangleProofs));
        Assert.Empty(E(candidate.Blocked)); Assert.True(evidence.IsComplete);
    }

    [Fact]
    public void CoverageUnionAccountsForEveryObservedSegmentExactlyOnceAtItsSelectedGrade()
    {
        var f = Grid(2, 2); f.Edge(f.Site(), f.Site(), f.Frame.Right, f.Frame.Up);
        var evidence = new QualitativeDispositionEvidence(new(f.Observation()));
        foreach (var candidate in evidence.Candidates)
        {
            var all = E(candidate.Supported).Concat(E(candidate.AdmittedOnly)).Concat(E(candidate.Outside)).ToArray();
            Assert.Equal(f.Edges.Count, all.Length); Assert.Equal(all.Length, all.Distinct(ReferenceEqualityComparer.Instance).Count());
            Assert.All(all, edge => Assert.Contains(f.Edges, actual => ReferenceEquals(actual, edge)));
            var expected = E(candidate.RouteSupport).Concat(E(candidate.CircuitSupport)).Concat(E(candidate.TriangleSupport))
                .Distinct(ReferenceEqualityComparer.Instance).ToArray();
            Assert.Equal(expected.Length, E(candidate.Supported).Length);
        }
    }

    [Fact]
    public void ActualTriangleProofIsPositiveCycleEvidenceAndCrossBasisTrianglesRemainSeparate()
    {
        var f = Grid(2, 2, diagonals: true); var evidence = new QualitativeDispositionEvidence(new(f.Observation()));
        var rich = Assert.Single(evidence.Candidates, candidate => E(candidate.Candidate.Basis).Length == 3);
        Assert.Equal(8, E(rich.TriangleProofs).Length);
        foreach (var row in E(rich.TriangleProofs).Cast<Relation>())
        {
            Assert.True(evidence.TryReadTriangleProof(rich.Candidate.Read, row, out _, out var segments)); Assert.Equal(3, E(segments).Length);
        }
        var coarse = evidence.Candidates.First(candidate => E(candidate.Candidate.UnexpectedClosures).Length != 0 && E(candidate.Candidate.Closures).Length == 0);
        Assert.Empty(E(coarse.TriangleProofs)); Assert.Empty(E(coarse.Blocked));
    }

    [Fact]
    public void ExactReadersRejectCopiedProofsAndForeignCandidateReferences()
    {
        var f = Grid(2, 2); var evidence = new QualitativeDispositionEvidence(new(f.Observation())); var candidate = Assert.Single(evidence.Candidates);
        var routeProof = Assert.IsType<Relation>(E(candidate.RouteProofs)[0]);
        Assert.True(evidence.TryReadRouteProof(candidate.Candidate.Read, routeProof, out _, out _, out _));
        Assert.False(evidence.TryReadRouteProof(candidate.Candidate.Read, new Relation(routeProof.Left, routeProof.Right), out _, out _, out _));
        var circuitProof = Assert.IsType<Relation>(E(candidate.CircuitProofs)[0]);
        Assert.False(evidence.TryReadCircuitProof(candidate.Candidate.Read, new Relation(circuitProof.Left, circuitProof.Right), out _, out _, out _));
        Assert.False(evidence.TryReadCandidate(new Relation(candidate.Candidate.Read.Left, candidate.Candidate.Read.Right), out _));
    }

    [Fact]
    public void UnknownHeadingKeepsKnownCircuitsAndSearchQualificationVisible()
    {
        var f = Grid(2, 2); var unknown = new Relation(new Placeholder(), new Relation(new Relation(f.Site(), f.Site()), StructuralConstraint.Open()));
        f.Edges.Add(unknown); var evidence = new QualitativeDispositionEvidence(new(f.Observation())); var candidate = Assert.Single(evidence.Candidates);
        Assert.False(evidence.IsComplete); Assert.NotEmpty(E(evidence.Unresolved));
        Assert.Equal(4, E(candidate.CircuitProofs).Length); Assert.Same(unknown, Assert.Single(E(candidate.Outside)));
    }

    private static QualitativeDispositionCandidateEvidence Composed(QualitativeDispositionEvidence evidence,
        QualitativeDispositionInduction induction, IElement horizontal, IElement up, IElement down) => evidence.Candidates.First(candidate =>
        E(candidate.Candidate.Basis).Length == 2 && E(candidate.Outside).Length == 0 && E(candidate.Candidate.Basis).Any(channel =>
            channel is Relation actual && induction.TryReadRhythm(actual, out var rhythm) && E(rhythm.Headings).Cast<Relation>().All(heading =>
                E(heading).Any(direction => ReferenceEquals(direction, horizontal))) && E(rhythm.Headings).Cast<Relation>().Any(heading =>
                E(heading).Any(direction => ReferenceEquals(direction, up))) && E(rhythm.Headings).Cast<Relation>().Any(heading =>
                E(heading).Any(direction => ReferenceEquals(direction, down)))));
    private static Relation[] Zigzag(Fixture f, int length)
    {
        var result = new List<Relation>(); var start = f.Site();
        for (var i = 0; i < length; i++) { var end = f.Site(); result.Add(f.Edge(start, end, f.Frame.Right, i % 2 == 0 ? f.Frame.Up : f.Frame.Down)); start = end; }
        return result.ToArray();
    }
    private static void Horizontal(Fixture f, int length)
    { var start = f.Site(); for (var i = 0; i < length; i++) { var end = f.Site(); f.Edge(start, end, f.Frame.Right); start = end; } }
    private static Fixture Grid(int columns, int rows, bool staggered = false, bool diagonals = false)
    {
        var f = new Fixture(); var sites = new IElement[columns + 1, rows + 1];
        for (var y = 0; y <= rows; y++) for (var x = 0; x <= columns; x++) sites[x, y] = f.Site();
        for (var y = 0; y <= rows; y++) for (var x = 0; x < columns; x++) f.Edge(sites[x, y], sites[x + 1, y], f.Frame.Right);
        for (var y = 0; y < rows; y++) for (var x = 0; x <= columns; x++)
            f.Edge(sites[x, y], sites[x, y + 1], staggered ? [y % 2 == 0 ? f.Frame.Right : f.Frame.Left, f.Frame.Up] : [f.Frame.Up]);
        if (diagonals) for (var y = 0; y < rows; y++) for (var x = 0; x < columns; x++) f.Edge(sites[x, y], sites[x + 1, y + 1], f.Frame.Right, f.Frame.Up);
        return f;
    }
    private sealed class Fixture
    {
        internal QualitativeMotionFrame Frame { get; } = new(); internal List<IElement> Sites { get; } = []; internal List<IElement> Edges { get; } = [];
        internal IElement Site() { var site = new Placeholder(); Sites.Add(site); return site; }
        internal Relation Edge(IElement from, IElement to, params IElement[] headings)
        { var edge = new Relation(new Placeholder(), new Relation(new Relation(from, to), StructuralConstraint.Closed(headings))); Edges.Add(edge); return edge; }
        internal QualitativeRecognitionObservation Observation() => new(Frame, StructuralConstraint.Closed(Sites.ToArray()), StructuralConstraint.Closed(Edges.ToArray()));
    }
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item)) { if (!seen.Add(item)) continue; Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item);
            if (item is Relation row) { todo.Push(row.Left); todo.Push(row.Right); } }
    }
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
