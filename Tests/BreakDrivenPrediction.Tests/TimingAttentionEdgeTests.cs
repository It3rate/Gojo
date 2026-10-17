using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction.Tests;

public sealed class TimingAttentionEdgeTests
{
    [Fact]
    public void NonnestedFinerProbeCannotExtendTheDemandedAbsenceDeadline()
    {
        var root = Create(demandedWidth: 13, probeEvery: 1, widths: [13, 11]);
        var issued = Observe(ref root, Packet(root, 0, 22, 15, 20));
        Assert.Equal(2, T.IssuedRows(issued).Length);
        AssertPrediction(root, issuedAt: 22, predictedAt: 25);
        var selected = Assert.IsType<Relation>(T.SelectedRung(T.Pending(root)));
        var diagnostic = Assert.Single(T.DiagnosticRows(T.Pending(root)));
        Assert.Equal((13L, 26L), Cell(selected));
        Assert.Equal((22L, 33L), Cell(diagnostic));

        var expired = Observe(ref root, Packet(root, 22, 26));
        Assert.True(T.Settled(expired));
        Assert.False(T.Successful(expired));
        Assert.True(T.IsDormant(root));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.Empty(T.IssuedRows(expired));
        Assert.IsType<Placeholder>(T.Pending(root));
    }

    [Fact]
    public void OneWakeOccurrenceCannotTurnTheSilentGapIntoANewCadence()
    {
        var root = Seeded();
        var phase = T.Phase(root).Ticks;
        var slept = Observe(ref root, Packet(root, 12, 24));
        Assert.True(T.Settled(slept));
        Assert.True(T.IsDormant(root));
        Assert.Equal(new long[] { 5, 10 }, KnownTimes(root));

        var still = Observe(ref root, Packet(root, 24, 36));
        Assert.True(T.IsDormant(root));
        Assert.Empty(T.IssuedRows(still));
        Assert.Equal(new long[] { 5, 10 }, KnownTimes(root));
        Assert.Equal(phase, T.Phase(root).Ticks);

        var first = Observe(ref root, Packet(root, 36, 48, 45));
        Assert.False(T.IsDormant(root));
        Assert.False(T.Settled(first));
        Assert.Empty(T.IssuedRows(first));
        Assert.IsType<Placeholder>(T.Pending(root));
        Assert.Equal(new long[] { 45 }, KnownTimes(root));
        Assert.Equal(phase, T.Phase(root).Ticks);
        Assert.Equal(0UL, T.Support(root).Value);

        var second = Observe(ref root, Packet(root, 48, 60, 50));
        Assert.False(T.Settled(second));
        Assert.Equal(new long[] { 45, 50 }, KnownTimes(root));
        AssertPrediction(root, issuedAt: 60, predictedAt: 65);
        Assert.Equal(0UL, T.Support(root).Value);
        // The absence-triggered finer probe was deferred until an actual new
        // forecast became possible, rather than spent while the learner slept.
        Assert.Single(T.DiagnosticRows(T.Pending(root)));
    }

    [Fact]
    public void UnknownCoverageNeitherEstablishesAbsenceNorWakesADormantPredictor()
    {
        var active = Seeded();
        var phase = T.Phase(active).Ticks;
        var unknown = Observe(ref active, UnknownPacket(active, 12, 24));
        Assert.False(T.Settled(unknown));
        Assert.False(T.IsDormant(active));
        Assert.Equal(0UL, T.Support(active).Value);
        Assert.Empty(KnownTimes(active));
        Assert.Equal(phase, T.Phase(active).Ticks);

        var noPromise = Observe(ref active, Packet(active, 24, 36));
        Assert.False(T.Settled(noPromise));
        Assert.False(T.IsDormant(active));
        Assert.Empty(T.IssuedRows(noPromise));

        var dormant = Seeded();
        Observe(ref dormant, Packet(dormant, 12, 24));
        Assert.True(T.IsDormant(dormant));
        var dormantPhase = T.Phase(dormant).Ticks;
        Observe(ref dormant, UnknownPacket(dormant, 24, 36));
        Assert.True(T.IsDormant(dormant));
        Assert.Empty(KnownTimes(dormant));

        var partial = C.Parts(Packet(dormant, 36, 48, 45));
        partial[5] = C.G(new Placeholder());
        var unusableWake = Observe(ref dormant, C.G(partial));
        Assert.False(T.Settled(unusableWake));
        Assert.True(T.IsDormant(dormant));
        Assert.Empty(T.IssuedRows(unusableWake));
        Assert.Empty(KnownTimes(dormant));
        Assert.Equal(dormantPhase, T.Phase(dormant).Ticks);
        Assert.Equal(0UL, T.Support(dormant).Value);

        var actualWake = Observe(ref dormant, Packet(dormant, 48, 60, 55));
        Assert.False(T.IsDormant(dormant));
        Assert.Empty(T.IssuedRows(actualWake));
        Assert.Equal(new long[] { 55 }, KnownTimes(dormant));
    }

    [Fact]
    public void DeferredFailureProbeAndIssuePhaseSurviveAnIndependentCheckpoint()
    {
        var root = Seeded();
        var phase = T.Phase(root).Ticks;
        T.Demand(root).Set(0);
        var missed = Observe(ref root, Packet(root, 12, 24, 14, 19));
        Assert.True(T.Settled(missed));
        Assert.False(T.Successful(missed));
        Assert.False(T.IsDormant(root));
        Assert.Empty(T.IssuedRows(missed));
        Assert.Equal(phase, T.Phase(root).Ticks);

        // A missing interval clears cadence but does not consume the queued
        // probe; no eligible issue has yet used that failure response.
        Observe(ref root, UnknownPacket(root, 24, 36));
        var packet = Packet(root, 36, 48, 40, 45);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root, packet));
        var other = Assert.IsType<Relation>(restored[0]);
        var otherPacket = Assert.IsType<Relation>(restored[1]);
        Assert.True(T.IsAttentive(other));
        Assert.Same(T.Port(other), C.Parts(otherPacket)[0]);
        Assert.NotSame(T.Port(root), T.Port(other));
        Assert.NotSame(T.Support(root), T.Support(other));
        Assert.NotSame(T.Phase(root).Carrier, T.Phase(other).Carrier);
        Assert.Equal(phase, T.Phase(other).Ticks);
        var otherBefore = GojoNumericFormat.Encode(other, otherPacket);

        T.Demand(root).Set(3);
        var resumed = Observe(ref root, packet);
        Assert.Equal(otherBefore, GojoNumericFormat.Encode(other, otherPacket));
        Assert.False(T.Settled(resumed));
        Assert.Equal(2, T.IssuedRows(resumed).Length);
        Assert.Single(T.DiagnosticRows(T.Pending(root)));
        AssertPrediction(root, issuedAt: 48, predictedAt: 50);

        T.Demand(other).Set(3);
        AssertRejectedWithoutMutation(other, packet);
        var replayed = Observe(ref other, otherPacket);
        Assert.Equal(T.IssuedRows(resumed).Length, T.IssuedRows(replayed).Length);
        Assert.Single(T.DiagnosticRows(T.Pending(other)));
        Assert.Equal(T.Phase(root).Ticks, T.Phase(other).Ticks);
        Assert.Equal(0UL, T.Support(other).Value);
        AssertPrediction(other, issuedAt: 48, predictedAt: 50);
    }

    [Fact]
    public void StaleRootsCannotWakeOrAdvanceTheSharedIssuePhase()
    {
        var root = Seeded();
        var staleActive = root;
        var packet = Packet(root, 12, 24);
        Observe(ref root, packet);
        Assert.True(T.IsDormant(root));
        var dormantBefore = GojoNumericFormat.Encode(root);
        AssertRejectedWithoutMutation(staleActive, packet);
        AssertRejectedWithoutMutation(staleActive, Packet(staleActive, 12, 36, 15, 20, 25, 30, 35));
        Assert.Equal(dormantBefore, GojoNumericFormat.Encode(root));

        var staleDormant = root;
        var phase = T.Phase(root).Ticks;
        Observe(ref root, Packet(root, 24, 36, 30));
        Assert.False(T.IsDormant(root));
        Assert.Equal(new long[] { 30 }, KnownTimes(root));
        Assert.IsType<Placeholder>(T.Pending(root));
        Assert.Equal(phase, T.Phase(root).Ticks);

        var wokeBefore = GojoNumericFormat.Encode(root);
        AssertRejectedWithoutMutation(staleDormant, Packet(staleDormant, 24, 48, 30, 35, 40, 45));
        Assert.Equal(wokeBefore, GojoNumericFormat.Encode(root));
        Observe(ref root, Packet(root, 36, 48, 40));
        AssertPrediction(root, issuedAt: 48, predictedAt: 50);
        Assert.Equal(0UL, T.Support(root).Value);
    }

    private static Relation Create(long demandedWidth = 3, long probeEvery = 4, params long[] widths)
    {
        var source = S.Create(0, new Carrier(), sampleStride: 1);
        return T.CreateAttentive(S.Port(source), demandedWidth, probeEvery, widths);
    }

    private static Relation Seeded()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 12, 5, 10));
        return root;
    }

    private static Relation Packet(Relation root, long start, long end, params long[] occurrences)
    {
        var port = T.Port(root);
        var fields = C.Parts(port);
        var clock = Assert.IsType<Carrier>(fields[0]);
        var value = Assert.IsType<Carrier>(fields[1]);
        return C.G(port, Relation.Segment(clock, start, end), new CarrierLocation(clock, end),
            C.G(occurrences.Select(t => (IElement)Relation.Segment(clock, t, t)).ToArray()),
            new CarrierLocation(value, 0), C.G());
    }

    private static Relation UnknownPacket(Relation root, long start, long end)
    {
        var fields = C.Parts(Packet(root, start, end));
        fields[3] = new Placeholder();
        return C.G(fields);
    }

    private static Relation Observe(ref Relation root, Relation packet)
    {
        Assert.True(T.TryObserve(root, packet, out var next, out var assessment));
        root = next;
        return assessment;
    }

    private static (long Start, long End) Cell(Relation row)
    {
        Assert.True(TimeStepSequence.TryReadSpan(Assert.IsType<Relation>(C.Parts(row)[1]), out var start, out var end));
        return (start.Ticks, end.Ticks);
    }

    private static long[] KnownTimes(Relation root) => T.Events(root)
        .OfType<CarrierLocation>().Select(value => value.Ticks).ToArray();

    private static void AssertPrediction(Relation root, long issuedAt, long predictedAt)
    {
        var issue = Assert.IsType<Relation>(C.Parts(Assert.IsType<Relation>(T.Pending(root)))[0]);
        Assert.NotEmpty(L.Rungs(issue));
        var fields = C.Parts(issue);
        Assert.Equal(issuedAt, Assert.IsType<CarrierLocation>(fields[0]).Ticks);
        Assert.Equal(predictedAt, Assert.IsType<CarrierLocation>(fields[1]).Ticks);
    }

    private static void AssertRejectedWithoutMutation(Relation root, Relation packet)
    {
        var before = GojoNumericFormat.Encode(root);
        Assert.False(T.TryObserve(root, packet, out var rejected, out _));
        Assert.Same(root, rejected);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }
}
