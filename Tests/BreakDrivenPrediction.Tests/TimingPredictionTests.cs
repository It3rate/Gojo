using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction.Tests;

public sealed class TimingPredictionTests
{
    [Fact]
    public void FirstBatchedObservationsTeachOnlyAFutureIssueAtTheirAvailability()
    {
        var (_, root) = Create();
        var assessment = Observe(ref root, Packet(root, 0, 12, 5, 10));

        Assert.False(T.Settled(assessment));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(12, T.Available(root).Ticks);
        Assert.Equal(new long[] { 5, 10 }, EventTimes(root));
        AssertIssue(root, issuedAt: 12, predictedAt: 15);
        Assert.Equal(3, L.Rungs(Issue(root)).Length);
    }

    [Fact]
    public void LinkedRungsShareEvidenceButCreditTheObservedEventOnlyOnce()
    {
        var (_, root) = Seeded();
        var pending = Assert.IsType<Relation>(T.Pending(root));
        var issue = Issue(root);
        IElement? common = null;
        foreach (var row in L.Rungs(issue))
        {
            var structural = Assert.IsType<Relation>(C.Parts(row)[3]);
            Assert.True(StructuralLadderSurface.TryReadRung(structural, out var source,
                out _, out _, out _, out _, out _, out _, out _));
            if (common is null) common = source;
            else Assert.Same(common, source);
        }

        var assessment = Observe(ref root, Packet(root, 12, 24, 15, 20));
        Assert.Same(pending, C.Parts(assessment)[0]);
        Assert.True(T.Settled(assessment));
        Assert.True(T.Successful(assessment));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.Equal(new long[] { 15, 20 }, EventTimes(root));
        AssertIssue(root, issuedAt: 24, predictedAt: 25);

        Observe(ref root, Packet(root, 24, 36, 25, 30, 35));
        Assert.Equal(2UL, T.Support(root).Value);
        Assert.Equal(new long[] { 30, 35 }, EventTimes(root));
        AssertIssue(root, issuedAt: 36, predictedAt: 40);
    }

    [Fact]
    public void FirstFutureOccurrenceSettlesTheIssueBeforeLaterBatchEventsCanFitIt()
    {
        var (_, root) = Seeded(demandedWidth: 1);
        var assessment = Observe(ref root, Packet(root, 12, 24, 14, 15));

        Assert.True(T.Settled(assessment));
        Assert.False(T.Successful(assessment));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.Equal(new long[] { 14, 15 }, EventTimes(root));
        AssertIssue(root, issuedAt: 24, predictedAt: 25);
    }

    [Theory]
    [InlineData(3, 1, true)]
    [InlineData(1, 3, false)]
    public void DemandIsFrozenForThePendingIssue(long issuedDemand, long laterDemand, bool succeeds)
    {
        var (_, root) = Seeded(issuedDemand);
        var frozen = Assert.IsType<CarrierLocation>(C.Parts(Assert.IsType<Relation>(T.Pending(root)))[1]);
        T.Demand(root).Set(laterDemand);
        Assert.Equal(issuedDemand, frozen.Ticks);

        var assessment = Observe(ref root, Packet(root, 12, 24, 16, 21));
        Assert.True(T.Settled(assessment));
        Assert.Equal(succeeds, T.Successful(assessment));
        Assert.Equal(succeeds ? 1UL : 0UL, T.Support(root).Value);
        var nextDemand = Assert.IsType<CarrierLocation>(C.Parts(Assert.IsType<Relation>(T.Pending(root)))[1]);
        Assert.Equal(laterDemand, nextDemand.Ticks);
        Assert.Equal(issuedDemand, frozen.Ticks);
    }

    [Fact]
    public void UnrepresentedDemandCanRecoverWithoutCreditingItsUnresolvedForecast()
    {
        var (_, root) = Create(demandedWidth: 2);
        var unresolved = Observe(ref root, Packet(root, 0, 12, 5, 10));
        Assert.False(T.Settled(unresolved));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(new long[] { 5, 10 }, EventTimes(root));

        T.Demand(root).Set(3);
        var recovered = Observe(ref root, Packet(root, 12, 24, 15, 20));
        Assert.False(T.Settled(recovered));
        Assert.Equal(0UL, T.Support(root).Value);
        AssertIssue(root, issuedAt: 24, predictedAt: 25);

        var prospective = Observe(ref root, Packet(root, 24, 36, 25, 30, 35));
        Assert.True(T.Settled(prospective));
        Assert.True(T.Successful(prospective));
        Assert.Equal(1UL, T.Support(root).Value);
    }

    [Fact]
    public void UnknownFineOccurrenceCoverageCannotGradeOrPreserveAnInventedCadence()
    {
        var (_, root) = Seeded();
        Observe(ref root, Packet(root, 12, 24, 15, 20));
        Assert.Equal(1UL, T.Support(root).Value);

        var assessment = Observe(ref root, UnknownPacket(root, 24, 36));
        Assert.False(T.Settled(assessment));
        Assert.False(T.Successful(assessment));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.All(T.Events(root), item => Assert.IsType<Placeholder>(item));
        Assert.IsType<Placeholder>(T.Pending(root));

        // Later fine observations can establish a new local cadence. They do
        // not retroactively turn the missing interval into a successful issue.
        var resumed = Observe(ref root, Packet(root, 36, 48, 40, 45));
        Assert.False(T.Settled(resumed));
        Assert.Equal(1UL, T.Support(root).Value);
        AssertIssue(root, issuedAt: 48, predictedAt: 50);
    }

    [Fact]
    public void SparseEndpointValuesNeverSupplyFineEventTimestamps()
    {
        var (_, root) = Create(stride: 12);
        for (var end = 12L; end <= 48; end += 12)
        {
            var packet = C.Parts(UnknownPacket(root, end - 12, end));
            var valueAxis = Assert.IsType<Carrier>(C.Parts(T.Port(root))[1]);
            packet[4] = new CarrierLocation(valueAxis, end % 24 == 0 ? -10 : 10);
            var assessment = Observe(ref root, C.G(packet));
            Assert.False(T.Settled(assessment));
            Assert.Equal(0UL, T.Support(root).Value);
            Assert.All(T.Events(root), item => Assert.IsType<Placeholder>(item));
            Assert.IsType<Placeholder>(T.Pending(root));
        }
    }

    [Fact]
    public void CompleteEmptyCoverageWaitsForTheLargestIssuedCellBeforeExpiring()
    {
        var (_, root) = Seeded();
        Observe(ref root, Packet(root, 12, 24, 15, 20));
        Assert.Equal(1UL, T.Support(root).Value);
        var pending = T.Pending(root);

        var early = Observe(ref root, Packet(root, 24, 30));
        Assert.False(T.Settled(early));
        Assert.Same(pending, T.Pending(root));
        Assert.Equal(1UL, T.Support(root).Value);

        var expired = Observe(ref root, Packet(root, 30, 36));
        Assert.True(T.Settled(expired));
        Assert.False(T.Successful(expired));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
    }

    [Fact]
    public void EmptyOccurrenceCollectionWithOpenCoverageResidueCannotCertifyAbsence()
    {
        var (_, root) = Seeded();
        Observe(ref root, Packet(root, 12, 24, 15, 20));
        var packet = C.Parts(Packet(root, 24, 36));
        packet[5] = C.G(new Placeholder());

        var assessment = Observe(ref root, C.G(packet));
        Assert.False(T.Settled(assessment));
        Assert.False(T.Successful(assessment));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.All(T.Events(root), item => Assert.IsType<Placeholder>(item));
        Assert.IsType<Placeholder>(T.Pending(root));
    }

    [Fact]
    public void RepeatedPacketCannotConsumeOrCreditTheSameIssueTwice()
    {
        var (_, root) = Seeded();
        var packet = Packet(root, 12, 24, 15, 20);
        Observe(ref root, packet);
        AssertRejectedWithoutMutation(root, packet);
        Assert.Equal(1UL, T.Support(root).Value);
    }

    [Fact]
    public void StaleRootCannotReplayCreditOrAdmitALaterReport()
    {
        var (_, root) = Seeded();
        var stale = root;
        var packet = Packet(root, 12, 24, 15, 20);
        Observe(ref root, packet);
        Assert.Equal(1UL, T.Support(root).Value);
        var currentBefore = GojoNumericFormat.Encode(root);

        AssertRejectedWithoutMutation(stale, packet);
        AssertRejectedWithoutMutation(stale, Packet(root, 24, 36, 25, 30, 35));
        // A newer availability stamp with coverage matching the old root must
        // also fail its shared admission cursor, not revive that old version.
        AssertRejectedWithoutMutation(stale, Packet(stale, 12, 36, 15, 20, 25, 30, 35));
        Assert.Equal(currentBefore, GojoNumericFormat.Encode(root));
        Assert.Equal(1UL, T.Support(root).Value);

        var admitted = Observe(ref root, Packet(root, 24, 36, 25, 30, 35));
        Assert.True(T.Successful(admitted));
        Assert.Equal(2UL, T.Support(root).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EqualNumbersOnForeignPublicClocksCannotEnterThePredictor(int field)
    {
        var (_, root) = Seeded();
        var packet = C.Parts(Packet(root, 12, 24, 15, 20));
        var clock = T.Available(root).Carrier;
        var foreign = new Carrier(clock.UnitShape);
        if (field == 0)
        {
            var port = C.Parts(T.Port(root));
            port[0] = foreign;
            port[2] = Relation.Segment(foreign, 0, 1);
            packet[0] = C.G(port);
        }
        if (field == 1) packet[1] = Relation.Segment(foreign, 12, 24);
        if (field == 2) packet[2] = new CarrierLocation(foreign, 24);
        if (field == 3) packet[3] = C.G(Relation.Segment(foreign, 15, 15));
        AssertRejectedWithoutMutation(root, C.G(packet));
    }

    [Fact]
    public void AnOccurrenceBeyondTheReportsAvailabilityCannotTeachOrEarnCredit()
    {
        var (_, root) = Seeded();
        AssertRejectedWithoutMutation(root, Packet(root, 12, 24, 25));
    }

    [Fact]
    public void ReadHelpersDoNotMutateTheWorldOrItsPendingNumbers()
    {
        var (_, root) = Seeded();
        var before = GojoNumericFormat.Encode(root);
        var issue = Issue(root);
        var frozenIssued = Assert.IsType<CarrierLocation>(C.Parts(issue)[0]);
        var frozenPredicted = Assert.IsType<CarrierLocation>(C.Parts(issue)[1]);
        for (var i = 0; i < 3; i++)
        {
            T.Port(root); T.Ladder(root); T.Support(root); T.Demand(root);
            T.Pending(root); T.Events(root); T.Available(root);
            var read = L.Assess(issue, new CarrierLocation(frozenIssued.Carrier, 16));
            var rows = C.Parts(Assert.IsType<Relation>(C.Parts(read)[2]));
            foreach (var row in rows.Cast<Relation>())
            {
                L.Matches(row);
                Assert.True(L.TryDifference(row, out var difference));
                Assert.Equal(1, difference.Ticks);
            }
        }
        Assert.Equal(before, GojoNumericFormat.Encode(root));
        Observe(ref root, Packet(root, 12, 24, 16, 21));
        Assert.Equal(12, frozenIssued.Ticks);
        Assert.Equal(15, frozenPredicted.Ticks);
    }

    [Fact]
    public void SupportSaturatesWithoutBankingTensionAndOneMissDrawsOneTick()
    {
        var (_, root) = Seeded(demandedWidth: 1);
        for (var end = 24L; end <= 144; end += 12)
            Observe(ref root, Packet(root, end - 12, end, MultiplesOfFive(end - 12, end)));
        Assert.Equal(8UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);

        var due = Assert.IsType<CarrierLocation>(C.Parts(Issue(root))[1]);
        var miss = Observe(ref root, Packet(root, 144, 156, due.Ticks + 1));
        Assert.True(T.Settled(miss));
        Assert.False(T.Successful(miss));
        Assert.Equal(7UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
    }

    [Fact]
    public void CheckpointPreservesPublicPortAndPendingAliasesInAnIndependentWorld()
    {
        var (source, root) = Seeded();
        var packet = Packet(root, 12, 24, 15, 20);
        var encoded = GojoNumericFormat.Encode(source, root, T.Pending(root), packet);
        var restored = GojoNumericFormat.Decode(encoded);
        var otherSource = Assert.IsType<Relation>(restored[0]);
        var other = Assert.IsType<Relation>(restored[1]);
        var otherPacket = Assert.IsType<Relation>(restored[3]);
        Assert.Same(S.Port(otherSource), T.Port(other));
        Assert.Same(restored[2], T.Pending(other));
        Assert.Same(T.Port(other), C.Parts(otherPacket)[0]);
        Assert.NotSame(T.Port(root), T.Port(other));
        Assert.NotSame(T.Support(root), T.Support(other));
        AssertRejectedWithoutMutation(other, packet);

        var untouched = GojoNumericFormat.Encode(otherSource, other, otherPacket);
        var first = Observe(ref root, packet);
        Assert.Equal(untouched, GojoNumericFormat.Encode(otherSource, other, otherPacket));
        var second = Observe(ref other, otherPacket);
        Assert.Equal(T.Successful(first), T.Successful(second));
        Assert.Equal(T.Support(root).Value, T.Support(other).Value);
        AssertIssue(other, issuedAt: 24, predictedAt: 25);
    }

    [Fact]
    public void RetainedEventTraceAndReachableEvidenceStayBounded()
    {
        var (_, root) = Seeded();
        var sizes = new List<long>();
        for (var report = 2; report <= 32; report++)
        {
            var end = report * 12L;
            Observe(ref root, Packet(root, end - 12, end, MultiplesOfFive(end - 12, end)));
            Assert.Equal(2, T.Events(root).Length);
            if (report is 8 or 16 or 32) sizes.Add(GraphSize(root));
        }
        Assert.All(sizes.Skip(1), size => Assert.InRange(size, 1L, sizes[0] + 128));
    }

    [Fact]
    public void BatchedFineReversalsSurviveNetCancellationWithoutExposingPrivateSensors()
    {
        var clock = new Carrier();
        var fine = S.Create(0, clock, sampleStride: 1);
        var sparse = S.Create(0, clock, sampleStride: 12);
        long[] levels = [1, 2, 1, 0, 1, 2, 1, 0, 1, 2, 1, 0];
        for (var i = 0; i < levels.Length; i++)
        {
            Assert.True(S.TryObserve(fine, new CarrierLocation(clock, i + 1),
                new CarrierLocation(S.Sensor(fine), levels[i]), out var next));
            fine = next;
        }
        Assert.True(S.TryObserve(sparse, new CarrierLocation(clock, 12),
            new CarrierLocation(S.Sensor(sparse), 0), out var nextSparse));
        sparse = nextSparse;
        Assert.True(S.Publish(fine, new CarrierLocation(clock, 12), out var publishedFine, out var finePacket));
        Assert.True(S.Publish(sparse, new CarrierLocation(clock, 12), out var publishedSparse, out var sparsePacket));
        var fineFields = C.Parts(finePacket);
        var sparseFields = C.Parts(sparsePacket);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(fineFields[4]).Ticks);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(sparseFields[4]).Ticks);
        Assert.Equal(new long[] { 3, 5, 7, 9, 11 }, SpanTimes(C.Parts(Assert.IsType<Relation>(fineFields[3]))));
        Assert.IsType<Placeholder>(sparseFields[3]);
        AssertPublicOnly(publishedFine, finePacket);
        AssertPublicOnly(publishedSparse, sparsePacket);
    }

    // These helpers author observations and inspect native results. The test
    // schedules and expected values are never passed to the predictor as a model.
    private static (Relation Source, Relation Root) Create(long demandedWidth = 3, long stride = 1)
    {
        var source = S.Create(0, new Carrier(), stride);
        return (source, T.Create(S.Port(source), demandedWidth));
    }

    private static (Relation Source, Relation Root) Seeded(long demandedWidth = 3)
    {
        var (source, root) = Create(demandedWidth);
        Observe(ref root, Packet(root, 0, 12, 5, 10));
        return (source, root);
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
        Assert.Equal(6, C.Parts(assessment).Length);
        return assessment;
    }

    private static Relation Issue(Relation root) =>
        Assert.IsType<Relation>(C.Parts(Assert.IsType<Relation>(T.Pending(root)))[0]);

    private static void AssertIssue(Relation root, long issuedAt, long predictedAt)
    {
        var fields = C.Parts(Issue(root));
        var issued = Assert.IsType<CarrierLocation>(fields[0]);
        var predicted = Assert.IsType<CarrierLocation>(fields[1]);
        Assert.Equal(issuedAt, issued.Ticks);
        Assert.Equal(predictedAt, predicted.Ticks);
        Assert.Same(issued.Carrier, predicted.Carrier);
        Assert.True(predicted.Ticks > issued.Ticks);
    }

    private static long[] EventTimes(Relation root) => T.Events(root)
        .Select(item => Assert.IsType<CarrierLocation>(item).Ticks).ToArray();

    private static long[] SpanTimes(IEnumerable<IElement> events) => events.Select(item =>
    {
        Assert.True(TimeStepSequence.TryReadSpan(Assert.IsType<Relation>(item), out var start, out var end));
        Assert.Equal(start.Ticks, end.Ticks);
        return start.Ticks;
    }).ToArray();

    private static long[] MultiplesOfFive(long start, long end) =>
        Enumerable.Range(checked((int)start + 1), checked((int)(end - start)))
            .Where(t => t % 5 == 0).Select(t => (long)t).ToArray();

    private static void AssertRejectedWithoutMutation(Relation root, Relation packet)
    {
        var before = GojoNumericFormat.Encode(root);
        Assert.False(T.TryObserve(root, packet, out var rejected, out _));
        Assert.Same(root, rejected);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    private static void AssertPublicOnly(Relation source, Relation packet)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(packet);
        Assert.DoesNotContain(graph.Carriers, row => row.Id == S.Sensor(source).Id);
        Assert.DoesNotContain(graph.Carriers, row => row.Id == S.Clock(source).Id);
        Assert.DoesNotContain(graph.TickRefs, row => row.Id == S.Sensor(source).ValueSlot.Tick.Id);
        Assert.DoesNotContain(graph.TickRefs, row => row.Id == S.Sensor(source).LiveTensionSlot.Tick.Id);
    }

    private static long GraphSize(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return (long)graph.Carriers.Length + graph.Relations.Length + graph.TickRefs.Length +
            graph.CarrierLocations.Length + graph.UnitShapes.Length + graph.Placeholders.Length;
    }
}
