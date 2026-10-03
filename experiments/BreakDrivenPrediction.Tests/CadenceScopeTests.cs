using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using Q = BreakDrivenPrediction.CadenceScope;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction.Tests;

public sealed class CadenceScopeTests
{
    [Fact]
    public void NativeCandidateWindowAndRootViewsShareTheirAuthoritativeSites()
    {
        var root = Create();
        AssertAliases(root);
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        AssertAliases(root);
        var scope = T.Scope(root)!;
        Assert.True(Q.TryInterval(scope, out var period));
        Assert.Equal(5, period.Ticks);
        Assert.True(T.HasCurrentCadence(root));
        Assert.True(T.TryExpected(root, out var expected));
        Assert.Equal(15, expected.Ticks);
        Assert.True(TimeStepSequence.TryReadSpan((Relation)T.ContinuationWindow(root), out var lower, out var upper));
        Assert.Equal(13, lower.Ticks);
        Assert.Equal(17, upper.Ticks);
        Assert.Same(expected.Carrier, lower.Carrier);
        Assert.Same(expected.Carrier, upper.Carrier);
        Assert.Empty(Q.EventBreaks(scope));
        Assert.Empty(Q.AbsenceBreaks(scope));
    }

    [Fact]
    public void ExactZeroSlackIsSupportedAndNegativeSlackStaysUnresolved()
    {
        var source = S.Create(0, new Carrier(), 1);
        var invalid = T.CreateScoped(S.Port(source), slackTicks: -1);
        Assert.Null(T.Scope(invalid));
        Assert.False(T.IsAttentive(invalid));
        var root = T.CreateScoped(S.Port(source), slackTicks: 0);
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        Assert.True(TimeStepSequence.TryReadSpan((Relation)T.ContinuationWindow(root), out var lower, out var upper));
        Assert.Equal(15, lower.Ticks);
        Assert.Equal(15, upper.Ticks);
        Observe(ref root, Packet(root, 10, 15, 15));
        Assert.True(T.HasCurrentCadence(root));
        Assert.Empty(Q.EventBreaks(T.Scope(root)!));
        Assert.True(T.TryExpected(root, out var next));
        Assert.Equal(20, next.Ticks);
    }

    [Fact]
    public void FixedSlackAcceptsSmallIntervalVariationWithoutLearningATolerance()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        var prior = 10L;
        foreach (var arrival in new long[] { 16, 20, 25, 31, 35, 40 })
        {
            Observe(ref root, Packet(root, prior, arrival, arrival));
            Assert.True(T.HasCurrentCadence(root));
            Assert.Empty(Q.EventBreaks(T.Scope(root)!));
            Assert.Empty(Q.AbsenceBreaks(T.Scope(root)!));
            Assert.True(Q.TryInterval(T.Scope(root)!, out var period));
            Assert.Equal(arrival - prior, period.Ticks);
            Assert.True(T.TryExpected(root, out var expected));
            Assert.Equal(arrival + period.Ticks, expected.Ticks);
            Assert.Equal(2, C.Length((Relation)C.Parts(T.Scope(root)!)[1]).Ticks);
            AssertAliases(root);
            prior = arrival;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CheckpointPreservesQualifiedOrOpenCandidateAndSharedSitesIndependently(bool open)
    {
        var root = Create(demand: 0);
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        if (open) Observe(ref root, Packet(root, 10, 17));
        var nextTime = open ? 20L : 15L;
        var packet = Packet(root, T.Available(root).Ticks, nextTime, nextTime);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root, packet));
        var other = Assert.IsType<Relation>(restored[0]);
        var otherPacket = Assert.IsType<Relation>(restored[1]);
        AssertAliases(other);
        Assert.Same(T.Port(other), C.Parts(otherPacket)[0]);
        Assert.NotSame(T.Port(root), T.Port(other));
        Assert.NotSame(T.Support(root), T.Support(other));
        Assert.Equal(!open, T.HasCurrentCadence(other));
        Assert.True(Q.TryInterval(T.Scope(other)!, out var oldInterval));
        Assert.Equal(5, oldInterval.Ticks);
        var untouched = GojoNumericFormat.Encode(other, otherPacket);

        Observe(ref root, packet);
        Assert.Equal(untouched, GojoNumericFormat.Encode(other, otherPacket));
        AssertRejected(other, packet);
        Observe(ref other, otherPacket);
        Assert.Equal(EventTimes(root), EventTimes(other));
        Assert.Equal(T.HasCurrentCadence(root), T.HasCurrentCadence(other));
        Assert.Equal(T.Phase(root).Ticks, T.Phase(other).Ticks);
        Assert.Equal(0UL, T.Support(other).Value);
        AssertAliases(root);
        AssertAliases(other);
        if (open)
        {
            Assert.Single(EventTimes(other));
            Assert.False(T.HasCurrentCadence(other));
            Assert.True(Q.TryInterval(T.Scope(other)!, out var remembered));
            Assert.Equal(5, remembered.Ticks);
        }
    }

    [Fact]
    public void ReadsWithoutNewObservationsCannotAgeOrInvalidateTheCandidate()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        var before = GojoNumericFormat.Encode(root);
        var candidate = T.RetainedCadence(root);
        var window = T.ContinuationWindow(root);
        for (var i = 0; i < 4; i++)
        {
            Assert.True(T.HasCurrentCadence(root));
            Assert.True(T.TryExpected(root, out _));
            Assert.True(Q.TryInterval(T.Scope(root)!, out _));
            Q.EventBreaks(T.Scope(root)!); Q.AbsenceBreaks(T.Scope(root)!); Q.UnknownOpens(T.Scope(root)!);
            Assert.Same(candidate, T.RetainedCadence(root));
            Assert.Same(window, T.ContinuationWindow(root));
        }
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForeignEvidenceAndStaleRootsCannotPartiallyMoveCadence(bool foreignOccurrence)
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        var stale = root;
        var foreign = new Carrier(T.Available(root).Carrier.UnitShape);
        var malformed = C.Parts(Packet(root, 10, 15, 15));
        if (foreignOccurrence) malformed[3] = C.G(Relation.Segment(foreign, 15, 15));
        else malformed[2] = new CarrierLocation(foreign, 15);
        AssertRejected(root, C.G(malformed));
        var packet = Packet(root, 10, 15, 15);
        Observe(ref root, packet);
        var current = GojoNumericFormat.Encode(root);
        AssertRejected(stale, packet);
        AssertRejected(stale, Packet(stale, 10, 20, 15, 20));
        Assert.Equal(current, GojoNumericFormat.Encode(root));
        AssertAliases(root);
    }

    [Fact]
    public void UnrepresentableWindowDoesNotPartiallyAdmitItsOtherwiseValidReport()
    {
        var root = Create();
        AssertRejected(root, Packet(root, 0, long.MaxValue - 1, long.MaxValue - 2, long.MaxValue - 1));
        Assert.Equal(0, T.Available(root).Ticks);
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(0, T.Phase(root).Ticks);
        Assert.IsType<Placeholder>(T.RetainedCadence(root));
    }

    [Fact]
    public void ScopeAndItsLatestBatchEvidenceRemainBoundedAcrossRepeatedIssues()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 10, 5, 10));
        var sizes = new List<long>();
        for (var issue = 2; issue <= 28; issue++)
        {
            Assert.True(T.TryExpected(root, out var expected));
            Observe(ref root, Packet(root, T.Available(root).Ticks, expected.Ticks, expected.Ticks));
            AssertAliases(root);
            Assert.Equal(2, T.Events(root).Length);
            Assert.True(T.HasCurrentCadence(root));
            if (issue is 12 or 20 or 28) sizes.Add(GraphSize(root));
        }
        Assert.All(sizes.Skip(1), size => Assert.InRange(size, 1, sizes[0] + 128));
    }

    private static Relation Create(long demand = 3) => T.CreateScoped(S.Port(S.Create(0, new Carrier(), 1)), demand);
    private static Relation Packet(Relation root, long start, long end, params long[] events)
    {
        var port = T.Port(root);
        var p = C.Parts(port);
        var clock = (Carrier)p[0];
        return C.G(port, Relation.Segment(clock, start, end), new CarrierLocation(clock, end),
            C.G(events.Select(at => (IElement)Relation.Segment(clock, at, at)).ToArray()),
            new CarrierLocation((Carrier)p[1], 0), C.G());
    }
    private static Relation Observe(ref Relation root, Relation packet)
    {
        Assert.True(T.TryObserve(root, packet, out var next, out var assessment));
        root = next;
        return assessment;
    }
    private static long[] EventTimes(Relation root) => T.Events(root).OfType<CarrierLocation>().Select(at => at.Ticks).ToArray();
    private static void AssertAliases(Relation root)
    {
        var r = C.Parts(root);
        var s = C.Parts(T.Scope(root)!);
        Assert.Same(r[2], s[2]);
        Assert.True(((CarrierLocation)r[4]).SameTickRefAs((CarrierLocation)s[5]));
        Assert.Same(T.Port(root), s[0]);
    }
    private static void AssertRejected(Relation root, Relation packet)
    {
        var before = GojoNumericFormat.Encode(root);
        Assert.False(T.TryObserve(root, packet, out var rejected, out _));
        Assert.Same(root, rejected);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }
    private static long GraphSize(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return (long)graph.Carriers.Length + graph.Relations.Length + graph.TickRefs.Length +
            graph.CarrierLocations.Length + graph.UnitShapes.Length + graph.Placeholders.Length;
    }
}
