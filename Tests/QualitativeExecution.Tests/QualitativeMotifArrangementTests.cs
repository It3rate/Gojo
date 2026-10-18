using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeMotifArrangementTests
{
    [Fact]
    public void SharedActualBoundariesLiftIntoFreshOrdinallyOrientedCellLinks()
    {
        var f = new Fixture(); var a = f.Arrangement();
        Assert.True(a.IsComplete); Assert.Equal(4, E(a.Chambers).Length);
        Assert.Equal(2, E(a.Horizontal).Length); Assert.Equal(2, E(a.Vertical).Length);
        Assert.Equal(2, E(a.HorizontalPaths).Length); Assert.Equal(2, E(a.VerticalPaths).Length);
        Assert.All(E(a.HorizontalPaths).Concat(E(a.VerticalPaths)), path => Assert.Equal(2, E((Relation)path).Length));
        foreach (var binding in E(a.NodeBindings).Cast<Relation>())
        {
            Assert.IsType<Placeholder>(binding.Right); Assert.True(a.TryNodeForOccurrence(binding.Left, out var node)); Assert.Same(binding.Right, node);
            Assert.Contains(Reach(a.Support(node)), item => ReferenceEquals(item, binding.Left));
        }
        foreach (var link in E(a.Horizontal).Concat(E(a.Vertical)).Cast<Relation>())
        {
            var ends = Assert.IsType<Relation>(link.Right);
            Assert.Contains(ends.Left, E(a.Chambers)); Assert.Contains(ends.Right, E(a.Chambers)); Assert.NotSame(ends.Left, ends.Right);
        }
    }

    [Fact]
    public void PublicInterfaceAndReturnedFactsCannotTraverseThePrivateFineObservation()
    {
        var f = new Fixture(); var a = f.Arrangement(); var replies = E(a.Chambers).Select(a.ReadCell).ToArray();
        var exposed = Reach(a.PublicSurface);
        foreach (var hidden in E(f.Observation.Sites).Concat(E(f.Observation.Segments)).Append(f.Observation.Surface).Append(f.Motifs.Surface).Append(f.Placement.Surface))
            Assert.DoesNotContain(exposed, item => ReferenceEquals(hidden, item));
        Assert.DoesNotContain(exposed, item => item is Carrier or CarrierLocation);
        Assert.All(replies, reply => Assert.Contains(a.Question, Reach(reply.Evidence)));
        Assert.Contains(Reach(a.AuditSurface), item => ReferenceEquals(item, f.Placement.Surface));
        Assert.Contains(Reach(a.AuditSurface), item => ReferenceEquals(item, f.Motifs.Surface));
    }

    [Fact]
    public void ContentQueriesAreLazyAndCachingRetainsTheActualEarlierReply()
    {
        var f = new Fixture(); var a = f.Arrangement();
        Assert.Equal(0, a.ActualQueryCount); Assert.Equal(0, a.RequestCount);
        var node = E(a.Chambers)[0]; var first = a.ReadCell(node);
        Assert.Equal(1, a.ActualQueryCount); Assert.Equal(1, a.RequestCount); Assert.Equal(0, a.CacheHits);
        var second = a.ReadCell(node.Dual().Dual());
        Assert.Same(first, second); Assert.Equal(1, a.ActualQueryCount); Assert.Equal(2, a.RequestCount); Assert.Equal(1, a.CacheHits);
        Assert.Contains(Reach(a.PublicSurface), item => ReferenceEquals(item, first.Surface));
    }

    [Fact]
    public void ACellPublishesActualReusablePatternDefinitionsIncludingAJunctionStar()
    {
        var f = new Fixture(); var a = f.Arrangement(); var replies = E(a.Chambers).Select(a.ReadCell).ToArray();
        Assert.All(replies, reply => { Assert.True(reply.IsComplete); Assert.Single(E(reply.Symbols)); });
        var prototypes = replies.SelectMany(reply => E(reply.Symbols)).ToArray(); Assert.Equal(4, prototypes.Distinct(ReferenceEqualityComparer.Instance).Count());
        foreach (var prototype in prototypes) Assert.Contains(f.SymbolFamilies, family => ReferenceEquals(family.Definition, prototype));
        Assert.Contains(f.SymbolFamilies, family => family.Kind == "junction" && family.SegmentCount == 8 &&
            prototypes.Any(prototype => ReferenceEquals(prototype, family.Definition)));
    }

    [Fact]
    public void CompleteFiniteCompetingReadingsRemainClosedAlternativesWithoutChoosingAWinner()
    {
        var f = new Fixture(competing: true); var a = f.Arrangement();
        var replies = E(a.Chambers).Select(a.ReadCell).ToArray(); var ambiguous = Assert.Single(replies, reply => E(reply.Competing).Length != 0);
        Assert.True(ambiguous.IsComplete); Assert.True(Closed(ambiguous.Symbols)); Assert.Equal(2, E(ambiguous.Symbols).Length);
        Assert.Empty(E(ambiguous.Unresolved)); Assert.Single(E(ambiguous.Competing));
        Assert.Same(ambiguous, a.ReadCell(ambiguous.Cell));
    }

    [Fact]
    public void AnExplicitlyEmptyPrototypeQuestionReturnsKnownEmptyContent()
    {
        var f = new Fixture(); var a = new QualitativeMotifArrangement(f.Motifs, f.Placement, f.ChamberSelection, S());
        var reply = a.ReadCell(E(a.Chambers)[0]);
        Assert.True(reply.IsComplete); Assert.True(Closed(reply.Symbols)); Assert.Empty(E(reply.Symbols)); Assert.Empty(E(reply.Unresolved));
    }

    [Fact]
    public void MissingOneInteriorEnvelopeLeavesOnlyItsDependentCellContentsOpen()
    {
        var f = new Fixture(); var missingEdge = f.UnitEdges[0];
        Assert.True(f.Publisher.TryMapSegment(missingEdge.Left, out var observed));
        var partial = new QualitativeRecognitionPlacement(f.Observation, f.Placement.HorizontalBuckets, f.Placement.VerticalBuckets,
            S(E(f.Placement.Envelopes).Where(item => item is Relation row && !ReferenceEquals(row.Left, observed)).ToArray()));
        var a = f.Arrangement(partial); Assert.Equal(4, E(a.Chambers).Length);
        var replies = E(a.Chambers).Select(a.ReadCell).ToArray();
        Assert.Single(replies, reply => !reply.IsComplete); Assert.Equal(3, replies.Count(reply => reply.IsComplete));
        Assert.Single(replies, reply => !Closed(reply.Symbols));
    }

    [Fact]
    public void MissingBoundaryEnvelopesDoNotInventAcceptedChambers()
    {
        var f = new Fixture(); var endpointOnly = new QualitativeRecognitionPlacement(f.Observation,
            f.Placement.HorizontalBuckets, f.Placement.VerticalBuckets);
        var a = f.Arrangement(endpointOnly);
        Assert.False(a.IsComplete); Assert.Empty(E(a.Chambers)); Assert.False(Closed(a.Chambers));
        Assert.NotEmpty(E(a.Unresolved)); Assert.Equal(0, a.ActualQueryCount);
    }

    [Fact]
    public void MissingOrdinalObservationDoesNotCreateLinksFromRenderedCoincidence()
    {
        var f = new Fixture(); var partial = new QualitativeRecognitionPlacement(f.Observation,
            f.Placement.HorizontalBuckets, StructuralConstraint.Open(), f.Placement.Envelopes);
        var a = f.Arrangement(partial);
        Assert.Empty(E(a.Chambers)); Assert.Empty(E(a.Horizontal)); Assert.Empty(E(a.Vertical)); Assert.False(a.IsComplete);
        Assert.False(Closed(a.CellPorts)); Assert.False(Closed(a.Horizontal));
    }

    [Fact]
    public void CoincidentSeamsWithIndependentActualSegmentsDoNotBecomeAdjacency()
    {
        var source = new Source();
        source.Cycle([(0, 0), (10, 0), (10, 10), (0, 10)]);
        source.Cycle([(10, 0), (20, 0), (20, 10), (10, 10)]);
        var (publisher, placement, motifs) = source.Publish();
        var a = new QualitativeMotifArrangement(motifs, placement);
        Assert.True(a.IsComplete); Assert.Equal(2, E(a.Chambers).Length); Assert.Empty(E(a.Horizontal)); Assert.Empty(E(a.Vertical));
        Assert.Equal(8, E(publisher.Observation.Sites).Length);
    }

    [Fact]
    public void ARecognizedDiamondIsAnotherViewRatherThanAGlobalRectangleFailure()
    {
        var f = new Fixture(); var a = new QualitativeMotifArrangement(f.Motifs, f.Placement, symbolFamilySelection: f.SymbolSelection);
        Assert.True(a.IsComplete); Assert.Empty(E(a.Blocked)); Assert.Empty(E(a.Unresolved)); Assert.NotEmpty(E(a.OtherViews));
        // Four grid circuits and one isolated inner square are valid rectangle
        // views at different resolutions; the diamond is retained separately.
        Assert.Equal(5, E(a.Chambers).Length);
    }

    [Fact]
    public void AForeignFineOccurrenceCannotBeSmuggledBackThroughARejectedPublicRequest()
    {
        var f = new Fixture(); var a = f.Arrangement(); var foreign = f.Motifs.Families[0].Occurrences[0].Read;
        var reply = a.ReadCell(foreign);
        Assert.False(reply.IsComplete); Assert.False(Closed(reply.Symbols)); Assert.Equal(0, a.ActualQueryCount);
        var exposed = Reach(a.PublicSurface);
        Assert.DoesNotContain(exposed, item => ReferenceEquals(item, foreign)); Assert.DoesNotContain(exposed, item => ReferenceEquals(item, f.Observation.Surface));
        Assert.Contains(Reach(a.AuditSurface), item => ReferenceEquals(item, foreign));
    }

    [Fact]
    public void EveryPrefilteredCandidateRetainsAnActualNoninteriorEndpointComparison()
    {
        var f = new Fixture(); var a = f.Arrangement(); var cell = E(a.Chambers)[0]; a.ReadCell(cell);
        Assert.True(a.TryReadQueryAudit(cell, out var inventory, out var checks, out var exclusions, out var unknown, out var unsearched));
        Assert.NotEmpty(E(exclusions)); Assert.Empty(E(unknown)); Assert.Empty(E(unsearched));
        Assert.Equal(E(inventory).Length, E(checks).Length + E(exclusions).Length);
        foreach (var candidate in E(inventory).Cast<Relation>())
            Assert.Single(E(checks).Concat(E(exclusions)), row => row is Relation { Left: Relation key } &&
                ReferenceEquals(key.Left, candidate.Left) && ReferenceEquals(key.Right, candidate.Right));
        foreach (var item in E(exclusions).Cast<Relation>())
        {
            var key = Assert.IsType<Relation>(item.Left); var body = Assert.IsType<Relation>(item.Right);
            var witness = Assert.IsType<Relation>(body.Right); var comparison = Assert.IsType<Relation>(witness.Right);
            Assert.True(f.Placement.TryReadComparison(comparison, out var before, out var equal, out var after, out var pending));
            Assert.Empty(E(pending)); Assert.Empty(E(after)); Assert.Equal(1, E(before).Length + E(equal).Length);
            var actualOccurrence = Assert.Single(f.SymbolFamilies.SelectMany(family => family.Occurrences), occurrence => ReferenceEquals(occurrence.Read, key.Right));
            Assert.Contains(E(actualOccurrence.Sites), site => ReferenceEquals(site, witness.Left));
        }
    }

    [Fact]
    public void ALocallyUnresolvedInteriorCandidateIsCheckedAndCannotBeLostToThePrefilter()
    {
        var f = new Fixture(); Assert.True(f.Publisher.TryMapSegment(f.UnitEdges[0].Left, out var observed));
        var partial = new QualitativeRecognitionPlacement(f.Observation, f.Placement.HorizontalBuckets, f.Placement.VerticalBuckets,
            S(E(f.Placement.Envelopes).Where(item => item is Relation row && !ReferenceEquals(row.Left, observed)).ToArray()));
        var candidate = Assert.Single(f.SymbolFamilies.SelectMany(family => family.Occurrences), occurrence =>
            E(occurrence.Segments).Any(row => ReferenceEquals(row, observed)));
        var a = f.Arrangement(partial); var reply = Assert.Single(E(a.Chambers).Select(a.ReadCell), result => !result.IsComplete);
        Assert.True(a.TryReadQueryAudit(reply.Cell, out var inventory, out var checks, out var exclusions, out var unknown, out _));
        Assert.Equal(E(inventory).Length, E(checks).Length + E(exclusions).Length); Assert.NotEmpty(E(unknown));
        Assert.Contains(E(checks), row => row is Relation { Left: Relation key } && ReferenceEquals(key.Right, candidate.Read));
        Assert.DoesNotContain(E(exclusions), row => row is Relation { Left: Relation key } && ReferenceEquals(key.Right, candidate.Read));
    }

    private sealed class Fixture
    {
        public QualitativeRecognitionPublisher Publisher { get; }
        public QualitativeRecognitionObservation Observation => Publisher.Observation;
        public QualitativeRecognitionPlacement Placement { get; }
        public QualitativeMotifInduction Motifs { get; }
        public Relation ChamberSelection { get; }
        public Relation SymbolSelection { get; }
        public QualitativeMotifFamily[] SymbolFamilies { get; }
        public Relation[] UnitEdges { get; }
        public Fixture(bool competing = false)
        {
            var source = new Source(); source.Grid(); var units = new List<Relation>();
            units.AddRange(source.Cycle([(5, 7), (7, 5), (5, 3), (3, 5)]));
            units.AddRange(source.Cycle(competing ? [(4, 4), (6, 4), (5, 6)] : [(14, 4), (16, 4), (15, 6)]));
            units.AddRange(source.Cycle([(3, 13), (7, 13), (7, 17), (3, 17)]));
            var centre = source.Site(15, 15);
            foreach (var (x, y) in new[] { (12, 15), (18, 15), (15, 12), (15, 18), (13, 13), (13, 17), (17, 13), (17, 17) })
                units.Add(source.Edge(centre, source.Site(x, y)));
            UnitEdges = units.ToArray(); (Publisher, Placement, Motifs) = source.Publish();
            var rectangles = Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4)
                .SelectMany(family => family.Occurrences).Where(occurrence => E(occurrence.Segments).Cast<Relation>()
                    .All(row => { Observation.TryReadSegment(row, out _, out _, out _, out var heading); return E(heading).Length == 1; })).ToArray();
            ChamberSelection = S(rectangles.Where(candidate => rectangles.Any(other => !ReferenceEquals(candidate.Read, other.Read) &&
                E(candidate.Segments).Any(edge => E(other.Segments).Any(second => ReferenceEquals(edge, second)))))
                .Select(candidate => (IElement)candidate.Read).ToArray());
            SymbolFamilies = Motifs.Families.Where(family => family.Kind == "cycle" || family.Kind == "junction" && family.SegmentCount == 8).ToArray();
            SymbolSelection = S(SymbolFamilies.Select(family => (IElement)family.Read).ToArray());
        }
        public QualitativeMotifArrangement Arrangement(QualitativeRecognitionPlacement? placement = null) => new(Motifs, placement ?? Placement, ChamberSelection, SymbolSelection);
    }
    private sealed class Source
    {
        private readonly QualitativeMotionFrame frame = new(); private readonly IElement reach = new Placeholder();
        private readonly List<IElement> sites = []; private readonly List<Relation> edges = []; private readonly List<IElement> bindings = [];
        public IElement Site(int x, int y)
        {
            var site = new Placeholder(); var moves = new List<IElement>(); sites.Add(site);
            for (var i = 0; i < Math.Abs(x); i++) moves.Add(QualitativeMotionFrame.Move(x < 0 ? frame.Left : frame.Right, reach));
            for (var i = 0; i < Math.Abs(y); i++) moves.Add(QualitativeMotionFrame.Move(y < 0 ? frame.Down : frame.Up, reach));
            bindings.Add(new Relation(site, S(moves.ToArray()))); return site;
        }
        public Relation Edge(IElement from, IElement to) { var edge = new Relation(new Placeholder(), new Relation(from, to)); edges.Add(edge); return edge; }
        public Relation[] Cycle((int X, int Y)[] points)
        { var roles = points.Select(point => Site(point.X, point.Y)).ToArray(); return roles.Select((role, index) => Edge(role, roles[(index + 1) % roles.Length])).ToArray(); }
        public void Grid()
        {
            var grid = new IElement[3, 3]; for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++) grid[x, y] = Site(x * 10, y * 10);
            for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++)
            { if (x < 2) Edge(grid[x, y], grid[x + 1, y]); if (y < 2) Edge(grid[x, y], grid[x, y + 1]); }
        }
        public (QualitativeRecognitionPublisher Publisher, QualitativeRecognitionPlacement Placement, QualitativeMotifInduction Motifs) Publish()
        {
            var publisher = new QualitativeRecognitionPublisher(frame, new Relation(S(sites.ToArray()), new Relation(S(edges.ToArray()), S())), S(bindings.ToArray()));
            var envelopes = S(edges.Select(edge => { var ends = (Relation)edge.Right; return (IElement)QualitativeRecognitionPlacementPublisher.Envelope(frame, edge.Left, ends.Left, ends.Right); }).ToArray());
            var placement = new QualitativeRecognitionPlacementPublisher(frame, S(bindings.ToArray()), publisher, envelopes).Placement;
            var motifs = new QualitativeMotifInduction(publisher.Observation, maxCycleEdges: 4, maxWalkEdges: 0, proposalCapacity: 20000, retainedCapacity: 4000);
            return (publisher, placement, motifs);
        }
    }
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static HashSet<IElement> Reach(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(source);
        while (work.TryPop(out var item)) if (seen.Add(item) && item is Relation row) { work.Push(row.Left); work.Push(row.Right); }
        return seen;
    }
}
