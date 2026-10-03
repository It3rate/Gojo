using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;
using G = BreakDrivenPrediction.CadenceScope;
using L = BreakDrivenPrediction.TimingLadder;
using S = BreakDrivenPrediction.TimingSource;
using T = BreakDrivenPrediction.TimingPrediction;

namespace BreakDrivenPrediction.Tests;

public sealed class CadenceScopeEdgeTests
{
    [Theory]
    [InlineData(13, 16, 14, 18)]
    [InlineData(17, 24, 22, 26)]
    public void BothClosedBoundsAcceptTheEventBeforeCheckingTheRemainingEmptyTail(
        long observedAt, long expectedAt, long lower, long upper)
    {
        var root = Seeded();
        Assert.Equal((13L, 17L), Window(root));

        Observe(ref root, Packet(root, 12, observedAt, observedAt));

        Assert.True(T.HasCurrentCadence(root));
        Assert.Equal(new long[] { 10, observedAt }, KnownTimes(root));
        AssertExpected(root, expectedAt);
        Assert.Equal((lower, upper), Window(root));
        Assert.Empty(G.EventBreaks(Scope(root)));
        Assert.Empty(G.AbsenceBreaks(Scope(root)));
        Assert.Empty(G.UnknownOpens(Scope(root)));
    }

    [Fact]
    public void EmptyCoverageOpensScopeWithNoDemandAndPositiveSupportBeforeThePromiseExpires()
    {
        var root = Seeded();
        Assert.True(T.Successful(Observe(ref root, Packet(root, 12, 24, 15, 20))));
        Assert.True(T.Successful(Observe(ref root, Packet(root, 24, 26, 25))));
        Assert.Equal(2UL, T.Support(root).Value);
        AssertExpected(root, 30);
        Assert.Equal((28L, 32L), Window(root));
        var pending = T.Pending(root);
        AssertPrediction(root, issuedAt: 26, predictedAt: 30);
        T.Demand(root).Set(0);

        var guardExpired = Observe(ref root, Packet(root, 26, 32));

        Assert.False(T.Settled(guardExpired));
        Assert.Same(pending, T.Pending(root));
        Assert.Equal(2UL, T.Support(root).Value);
        Assert.False(T.IsDormant(root));
        Assert.False(T.HasCurrentCadence(root));
        Assert.Empty(KnownTimes(root));
        Assert.Single(G.AbsenceBreaks(Scope(root)));
        Assert.Empty(G.EventBreaks(Scope(root)));
        Assert.Empty(G.UnknownOpens(Scope(root)));
        Assert.Equal(new long[] { 20, 25 }, CandidateTimes(root));

        var promiseExpired = Observe(ref root, Packet(root, 32, 36));
        Assert.True(T.Settled(promiseExpired));
        Assert.False(T.Successful(promiseExpired));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.False(T.IsDormant(root));
        Assert.False(T.HasCurrentCadence(root));
        Assert.Empty(T.IssuedRows(promiseExpired));
        Assert.Empty(G.AbsenceBreaks(Scope(root)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownCoverageRetainsTheCandidateWithoutAnAbsenceClaimOrSupportPenalty(bool partial)
    {
        var root = Seeded();
        Assert.True(T.Successful(Observe(ref root, Packet(root, 12, 24, 15, 20))));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.Equal(new long[] { 15, 20 }, CandidateTimes(root));
        var packet = C.Parts(Packet(root, 24, 36, 25, 30, 35));
        if (partial) packet[5] = C.G(new Placeholder());
        else packet[3] = new Placeholder();

        var unavailable = Observe(ref root, C.G(packet));

        Assert.False(T.Settled(unavailable));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.False(T.IsDormant(root));
        Assert.False(T.HasCurrentCadence(root));
        Assert.False(T.TryExpected(root, out _));
        Assert.Empty(KnownTimes(root));
        Assert.Equal(new long[] { 15, 20 }, CandidateTimes(root));
        Assert.True(G.TryInterval(Scope(root), out var remembered));
        Assert.Equal(5, remembered.Ticks);
        Assert.Single(G.UnknownOpens(Scope(root)));
        Assert.Empty(G.AbsenceBreaks(Scope(root)));
        Assert.Empty(G.EventBreaks(Scope(root)));
        Assert.IsType<Placeholder>(T.Pending(root));

        var firstFresh = Observe(ref root, Packet(root, 36, 40, 40));
        Assert.False(T.Settled(firstFresh));
        Assert.False(T.HasCurrentCadence(root));
        Assert.Equal(new long[] { 40 }, KnownTimes(root));
        Assert.Equal(new long[] { 15, 20 }, CandidateTimes(root));
        Assert.Empty(T.IssuedRows(firstFresh));

        var rebound = Observe(ref root, Packet(root, 40, 44, 44));
        Assert.False(T.Settled(rebound));
        Assert.Equal(1UL, T.Support(root).Value);
        AssertExpected(root, 48);
        Assert.Equal(new long[] { 40, 44 }, CandidateTimes(root));
        AssertPrediction(root, issuedAt: 44, predictedAt: 48);
    }

    [Fact]
    public void ACompleteLateEventIsCheckedAgainstTheOldWindowBeforeBecomingTheFirstFreshGround()
    {
        var root = Seeded(demandedWidth: 0);

        var late = Observe(ref root, Packet(root, 12, 18, 18));

        Assert.False(T.Settled(late));
        Assert.Equal(0UL, T.Support(root).Value);
        Assert.False(T.HasCurrentCadence(root));
        Assert.Equal(new long[] { 18 }, KnownTimes(root));
        Assert.Equal(new long[] { 5, 10 }, CandidateTimes(root));
        Assert.Single(G.EventBreaks(Scope(root)));
        Assert.Empty(G.UnknownOpens(Scope(root)));
        Assert.Empty(T.IssuedRows(late));
    }

    [Fact]
    public void SplittingACompleteBatchPreservesItsQualifiedCadenceWithoutGivingRetrospectiveCredit()
    {
        var together = Seeded(demandedWidth: 0);
        var split = Seeded(demandedWidth: 0);

        var singleReport = Observe(ref together, Packet(together, 12, 26, 18, 22));
        var firstReport = Observe(ref split, Packet(split, 12, 18, 18));
        var secondReport = Observe(ref split, Packet(split, 18, 26, 22));

        Assert.Single(G.EventBreaks(Scope(together)));
        Assert.Empty(G.EventBreaks(Scope(split)));
        Assert.Equal(new long[] { 18, 22 }, KnownTimes(together));
        Assert.Equal(KnownTimes(together), KnownTimes(split));
        Assert.Equal(CandidateTimes(together), CandidateTimes(split));
        Assert.True(T.HasCurrentCadence(together));
        Assert.True(T.HasCurrentCadence(split));
        AssertExpected(together, 26);
        AssertExpected(split, 26);
        Assert.Equal((24L, 28L), Window(together));
        Assert.Equal(Window(together), Window(split));
        Assert.Equal(T.Available(together).Ticks, T.Available(split).Ticks);
        Assert.Equal(0UL, T.Support(together).Value);
        Assert.Equal(0UL, T.Support(split).Value);
        Assert.False(T.Settled(singleReport));
        Assert.False(T.Settled(firstReport));
        Assert.False(T.Settled(secondReport));
        Assert.Empty(T.IssuedRows(singleReport));
        Assert.Empty(T.IssuedRows(firstReport));
        Assert.Empty(T.IssuedRows(secondReport));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    public void AStillOpenWindowWithANonfutureCenterCannotBeSkippedToAnotherCycle(long available)
    {
        var root = Create();

        var observed = Observe(ref root, Packet(root, 0, available, 5, 10));

        Assert.True(T.HasCurrentCadence(root));
        AssertExpected(root, 15);
        Assert.Equal((13L, 17L), Window(root));
        Assert.Empty(T.IssuedRows(observed));
        Assert.IsType<Placeholder>(T.Pending(root));
        Assert.Equal(0, T.Phase(root).Ticks);
        Assert.Equal(0UL, T.Support(root).Value);

        Observe(ref root, Packet(root, available, 17));
        Assert.False(T.HasCurrentCadence(root));
        Assert.Single(G.AbsenceBreaks(Scope(root)));
        Assert.Empty(KnownTimes(root));
        Assert.Equal(new long[] { 5, 10 }, CandidateTimes(root));
    }

    [Fact]
    public void ALongPeriodRemainsApplicableAcrossSeveralCompleteEmptyReports()
    {
        var root = Create();
        Observe(ref root, Packet(root, 0, 24, 1, 21));
        AssertExpected(root, 41);
        Assert.Equal((39L, 43L), Window(root));
        AssertPrediction(root, issuedAt: 24, predictedAt: 41);
        var pending = T.Pending(root);

        foreach (var (start, end) in new[] { (24L, 30L), (30L, 36L), (36L, 40L) })
        {
            var empty = Observe(ref root, Packet(root, start, end));
            Assert.False(T.Settled(empty));
            Assert.True(T.HasCurrentCadence(root));
            Assert.Same(pending, T.Pending(root));
            AssertExpected(root, 41);
            Assert.Equal(new long[] { 1, 21 }, KnownTimes(root));
            Assert.Empty(G.AbsenceBreaks(Scope(root)));
            Assert.Equal(0UL, T.Support(root).Value);
        }

        var eventReport = Observe(ref root, Packet(root, 40, 42, 41));
        Assert.True(T.Successful(eventReport));
        Assert.Equal(1UL, T.Support(root).Value);
        AssertExpected(root, 61);
        Assert.Equal(new long[] { 21, 41 }, KnownTimes(root));
        AssertPrediction(root, issuedAt: 42, predictedAt: 61);
    }

    [Fact]
    public void ACoarseFrozenPromiseCanSucceedWhileTheSameEventBreaksCadenceApplicability()
    {
        var root = Seeded(demandedWidth: 12);
        AssertPrediction(root, issuedAt: 12, predictedAt: 15);
        var selected = Assert.IsType<Relation>(T.SelectedRung(T.Pending(root)));
        Assert.Equal((12L, 24L), Span(Assert.IsType<Relation>(C.Parts(selected)[1])));
        Assert.Equal((13L, 17L), Window(root));

        var observed = Observe(ref root, Packet(root, 12, 20, 20));

        Assert.True(T.Settled(observed));
        Assert.True(T.Successful(observed));
        Assert.Equal(1UL, T.Support(root).Value);
        Assert.Equal(0, T.Support(root).Tension);
        Assert.False(T.IsDormant(root));
        Assert.False(T.HasCurrentCadence(root));
        Assert.Single(G.EventBreaks(Scope(root)));
        Assert.Equal(new long[] { 20 }, KnownTimes(root));
        Assert.Equal(new long[] { 5, 10 }, CandidateTimes(root));
        Assert.Empty(T.IssuedRows(observed));
        Assert.IsType<Placeholder>(T.Pending(root));
    }

    private static Relation Create(long demandedWidth = 3)
    {
        var source = S.Create(0, new Carrier(), sampleStride: 1);
        return T.CreateScoped(S.Port(source), demandedWidth, probeEvery: 4, slackTicks: 2);
    }

    private static Relation Seeded(long demandedWidth = 3)
    {
        var root = Create(demandedWidth);
        Observe(ref root, Packet(root, 0, 12, 5, 10));
        return root;
    }

    private static Relation Scope(Relation root) => Assert.IsType<Relation>(T.Scope(root));

    private static long[] KnownTimes(Relation root) => T.Events(root)
        .OfType<CarrierLocation>().Select(value => value.Ticks).ToArray();

    private static long[] CandidateTimes(Relation root)
    {
        var candidate = Assert.IsType<Relation>(T.RetainedCadence(root));
        var grounds = Assert.IsType<Relation>(C.Parts(candidate)[0]);
        return C.Parts(grounds).Select(point => Assert.IsType<CarrierLocation>(point).Ticks).ToArray();
    }

    private static (long Start, long End) Window(Relation root) =>
        Span(Assert.IsType<Relation>(T.ContinuationWindow(root)));

    private static (long Start, long End) Span(Relation span)
    {
        Assert.True(TimeStepSequence.TryReadSpan(span, out var start, out var end));
        return (start.Ticks, end.Ticks);
    }

    private static void AssertExpected(Relation root, long expected)
    {
        Assert.True(T.TryExpected(root, out var actual));
        Assert.Equal(expected, actual.Ticks);
        Assert.Same(T.Available(root).Carrier, actual.Carrier);
    }

    private static void AssertPrediction(Relation root, long issuedAt, long predictedAt)
    {
        var issue = Assert.IsType<Relation>(C.Parts(Assert.IsType<Relation>(T.Pending(root)))[0]);
        Assert.NotEmpty(L.Rungs(issue));
        var fields = C.Parts(issue);
        Assert.Equal(issuedAt, Assert.IsType<CarrierLocation>(fields[0]).Ticks);
        Assert.Equal(predictedAt, Assert.IsType<CarrierLocation>(fields[1]).Ticks);
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

    private static Relation Observe(ref Relation root, Relation packet)
    {
        Assert.True(T.TryObserve(root, packet, out var next, out var assessment));
        root = next;
        return assessment;
    }
}
