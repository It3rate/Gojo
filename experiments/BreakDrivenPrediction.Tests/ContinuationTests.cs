using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;

namespace BreakDrivenPrediction.Tests;

public sealed class ContinuationTests
{
    [Fact]
    public void TenDirectionalSuccessesRetainSelectionAndDoNotAppendAnEvent()
    {
        var root = C.Create(0);
        var selected = C.Selected(root);
        var binding = C.Parts(root)[3];
        var eventBefore = C.Parts(root)[9];
        for (var i = 0; i < 10; i++)
        {
            var assessment = Advance(ref root, 1, out _);
            Assert.True(C.Pass(Claim(assessment, 5)));
            Assert.Same(selected, C.Selected(root));
            Assert.Same(binding, C.Parts(root)[3]);
            Assert.Same(eventBefore, C.Parts(root)[9]);
        }
        Assert.Empty(C.Proposals(root));
        Assert.Empty(C.Connections(root));
        Assert.Equal(10UL, C.Clock(root).Value);
    }

    [Fact]
    public void BreakChangesSelectionWithoutRewritingItsIssuedForecast()
    {
        var root = C.Create(10);
        Advance(ref root, 1, out _);
        var previous = C.Selected(root);
        var issue = C.Issue(root);
        var baseline = Assert.IsType<CarrierLocation>(C.Parts(issue)[5]);
        var exported = Assert.IsType<CarrierLocation>(C.Parts(issue)[6]);
        var assessment = Observe(ref root, issue, -3);
        Assert.NotSame(previous, C.Selected(root));
        Assert.Equal(-3, C.Step(C.Selected(root)).Ticks);
        Assert.Equal(1, baseline.Ticks);
        Assert.Equal(1, exported.Ticks);
        Assert.False(C.Pass(Claim(assessment, 5)));
        Assert.False(C.Pass(Claim(assessment, 6)));
        Assert.True(TimeStepSequence.TryReadStep(Assert.IsType<Relation>(C.Parts(root)[9]),
            out _, out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void FirstRunIsCensoredAndOnlyAnObservedOnsetSuppliesDuration()
    {
        var root = C.Create(0);
        for (var i = 0; i < 3; i++) Advance(ref root, 2, out _);
        Advance(ref root, -2, out _);
        Assert.Empty(C.Proposals(root));
        for (var i = 0; i < 3; i++) Advance(ref root, -2, out _);
        Advance(ref root, 2, out _);
        var edge = Assert.Single(C.Proposals(root));
        Assert.Equal(4, Duration(edge));
        Assert.Equal(0UL, Credit(edge).Value);
        Assert.Empty(C.Connections(root));
    }

    [Fact]
    public void IncompatibleForeignAndStaleIssuesLeaveTheWorldUnchanged()
    {
        var root = C.Create(0);
        var issue = C.Issue(root);
        RejectWithoutMutation(root, issue, new CarrierLocation(new Carrier(), 1));
        RejectWithoutMutation(root, C.Issue(C.Create(0)), new CarrierLocation(C.Sensor(root), 1));
        Observe(ref root, issue, 1);
        RejectWithoutMutation(root, issue, new CarrierLocation(C.Sensor(root), 2));
    }

    [Fact]
    public void ExplicitPressureRetargetsStableScaleAndIsConsumedOnlyOnce()
    {
        var root = C.Create(0);
        Advance(ref root, 1, out _);
        var selected = C.Selected(root);
        C.Pressure(root).Add(3);
        Assert.Equal(3, C.Pressure(root).Tension);
        var assessment = Advance(ref root, 4, out var issue);
        Assert.True(C.Pass(Claim(assessment, 5)));
        Assert.False(C.Pass(Claim(assessment, 7)));
        Assert.Same(selected, C.Selected(root));
        Assert.Equal(4, C.Step(selected).Ticks);
        Assert.Equal(1, Assert.IsType<CarrierLocation>(C.Parts(issue)[6]).Ticks);
        Assert.Equal(3, Assert.IsType<CarrierLocation>(C.Parts(assessment)[11]).Ticks);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(C.Parts(assessment)[12]).Ticks);
        Assert.Equal(0, C.Pressure(root).Tension);
        Advance(ref root, 6, out _);
        Assert.Equal(4, C.Step(selected).Ticks);
        Assert.Equal(0, C.Pressure(root).Tension);
    }

    [Fact]
    public void RepeatedIssuanceDoesNotAdmitInputOrConsumePressure()
    {
        var root = C.Create(-3);
        C.Pressure(root).Add(2);
        var before = GojoNumericFormat.Encode(root);
        var first = C.Issue(root);
        var second = C.Issue(root);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
        Assert.Equal(Assert.IsType<CarrierLocation>(C.Parts(first)[6]).Ticks,
            Assert.IsType<CarrierLocation>(C.Parts(second)[6]).Ticks);
        Assert.Equal(0UL, C.Clock(root).Value);
        Assert.Equal(2, C.Pressure(root).Tension);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TimingCreditRequiresAProspectiveCounterfactualOpportunity(bool regular)
    {
        var root = C.Create(0);
        int[] lengths = regular ? [3, 5, 3, 5, 3, 5, 3, 5, 3, 5, 3, 5] :
            [2, 5, 3, 1, 6, 4, 2, 7, 1, 3, 5, 2];
        var opportunities = 0;
        for (var run = 0; run < lengths.Length; run++)
        for (var i = 0; i < lengths[run]; i++)
        {
            var issue = C.Issue(root);
            var f = C.Parts(issue);
            var tested = f[7] as Relation;
            var priorCredit = tested is null ? 0UL : Credit(tested).Value;
            var priorDuration = tested is null ? 0 : Duration(tested);
            var assessment = Observe(ref root, issue, run % 2 == 0 ? 2 : -2);
            if (tested is null) continue;
            opportunities++;
            var shadow = Claim(assessment, 8);
            var baseline = Claim(assessment, 5);
            var current = C.Proposals(root).Single(edge => SameRoute(edge, tested));
            var expected = priorCredit;
            if (C.Pass(shadow) && !C.Pass(baseline)) expected = Math.Min(4UL, priorCredit + 1);
            if (!C.Pass(shadow) && C.Pass(baseline)) expected = priorCredit == 0 ? 0 : priorCredit - 1;
            if (Duration(current) != priorDuration) expected = 0;
            Assert.Equal(expected, Credit(current).Value);
            if (priorCredit < 2)
                Assert.Equal(Assert.IsType<CarrierLocation>(f[5]).Ticks,
                    Assert.IsType<CarrierLocation>(f[6]).Ticks);
        }
        // This only establishes that the fixture exercised future opportunities;
        // it does not require the irregular input to become predictable.
        Assert.True(opportunities > 0);
        Assert.All(C.Connections(root), link => Assert.True(Credit(Assert.IsType<Relation>(link.Right)).Value >= 2));
    }

    [Fact]
    public void ChangedDurationCannotInheritSupportForTheOldDuration()
    {
        var root = C.Create(0);
        for (var i = 0; i < 80; i++) Advance(ref root, RegularDelta(i), out _);
        for (var i = 0; i < 6; i++) Advance(ref root, 2, out _);
        var active = C.Selected(root);
        var previous = C.Proposals(root).Single(edge => ReferenceEquals(Route(edge).Left, active));
        Assert.Equal(3, Duration(previous));
        Assert.True(Credit(previous).Value >= 2);
        Advance(ref root, -2, out _);
        var replacement = C.Proposals(root).Single(edge => ReferenceEquals(Route(edge).Left, active));
        Assert.Equal(6, Duration(replacement));
        Assert.NotSame(Credit(previous), Credit(replacement));
        Assert.Equal(0UL, Credit(replacement).Value);
        Assert.DoesNotContain(C.Connections(root), link => ReferenceEquals(link.Right, replacement));
    }

    [Fact]
    public void RepeatedSaturationDoesNotBankTensionAgainstTheNextDrawAfterCheckpoint()
    {
        var root = C.Create(0);
        for (var i = 0; i < 80; i++) Advance(ref root, RegularDelta(i), out _);
        for (var i = 0; i < 3; i++) Advance(ref root, 2, out _);
        var issue = C.Issue(root);
        var tested = Assert.IsType<Relation>(C.Parts(issue)[7]);
        Assert.Equal(4UL, Credit(tested).Value);
        Assert.Equal(0, Credit(tested).Tension);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root, issue));
        var clone = Assert.IsType<Relation>(restored[0]);
        var cloneIssue = Assert.IsType<Relation>(restored[1]);
        var clonedTested = Assert.IsType<Relation>(C.Parts(cloneIssue)[7]);
        var assessment = Observe(ref root, issue, 2);
        Observe(ref clone, cloneIssue, 2);
        Assert.True(C.Pass(Claim(assessment, 5)));
        Assert.False(C.Pass(Claim(assessment, 8)));
        Assert.Equal(3UL, Credit(tested).Value);
        Assert.Equal(3UL, Credit(clonedTested).Value);
        Assert.Equal(0, Credit(tested).Tension);
        Assert.Equal(0, Credit(clonedTested).Tension);
        AssertEquivalent(root, clone);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(17)]
    [InlineData(80)]
    public void CheckpointRestoresPendingForecastAndContinuesIndependently(int prefix)
    {
        var root = C.Create(0);
        for (var i = 0; i < prefix; i++) Advance(ref root, RegularDelta(i), out _);
        var issue = C.Issue(root);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root, issue));
        var clone = Assert.IsType<Relation>(restored[0]);
        var cloneIssue = Assert.IsType<Relation>(restored[1]);
        Assert.NotSame(C.Sensor(root), C.Sensor(clone));
        Assert.NotSame(C.Clock(root), C.Clock(clone));
        Assert.False(C.Sensor(root).ValueSlot.SameTickRefAs(C.Sensor(clone).ValueSlot));
        Assert.Same(C.Parts(clone)[3], C.Parts(cloneIssue)[4]);
        AssertEquivalent(root, clone);
        for (var i = prefix; i < prefix + 24; i++)
        {
            var beforeClone = GojoNumericFormat.Encode(clone, cloneIssue);
            var a = Observe(ref root, issue, RegularDelta(i));
            Assert.Equal(beforeClone, GojoNumericFormat.Encode(clone, cloneIssue));
            var b = Observe(ref clone, cloneIssue, RegularDelta(i));
            Assert.Equal(C.Pass(Claim(a, 6)), C.Pass(Claim(b, 6)));
            AssertEquivalent(root, clone);
            issue = C.Issue(root);
            cloneIssue = C.Issue(clone);
            Assert.Equal(Assert.IsType<CarrierLocation>(C.Parts(issue)[6]).Ticks,
                Assert.IsType<CarrierLocation>(C.Parts(cloneIssue)[6]).Ticks);
        }
    }

    [Fact]
    public void ReachableWorldDoesNotRetainAnEverGrowingObservationHistory()
    {
        var root = C.Create(0);
        var sizes = new List<long>();
        for (var i = 0; i < 256; i++)
        {
            Advance(ref root, RegularDelta(i), out _);
            Assert.InRange(C.Proposals(root).Length, 0, 2);
            Assert.InRange(C.Connections(root).Length, 0, 2);
            if (i + 1 is 64 or 128 or 256) sizes.Add(GraphSize(root));
        }
        Assert.All(sizes.Skip(1), count => Assert.InRange(count, 1L, sizes[0] + 64));
    }

    [Fact]
    public void ZeroObservationDoesNotInventAFlatPrimitiveOrCompleteTheNextRun()
    {
        var root = C.Create(0);
        Advance(ref root, -1, out _);
        var selected = C.Selected(root);
        var assessment = Advance(ref root, 0, out _);
        Assert.Same(selected, C.Selected(root));
        Assert.False(C.Pass(Claim(assessment, 5)));
        Assert.IsType<Placeholder>(C.Parts(root)[5]);
        Assert.Empty(C.Proposals(root));
        Advance(ref root, 1, out _);
        Assert.Empty(C.Proposals(root));
    }

    private static Relation Advance(ref Relation root, long delta, out Relation issue)
    {
        issue = C.Issue(root);
        return Observe(ref root, issue, delta);
    }

    // Test inputs and expected counts are measurement apparatus, not learner state.
    private static Relation Observe(ref Relation root, Relation issue, long delta)
    {
        var observation = new CarrierLocation(C.Sensor(root), C.Level(C.Sensor(root)).Ticks + delta);
        Assert.True(C.TryObserve(root, issue, observation, out var next, out var assessment));
        root = next;
        Assert.Equal(13, C.Parts(assessment).Length);
        return assessment;
    }

    private static void RejectWithoutMutation(Relation root, Relation issue, CarrierLocation input)
    {
        var before = GojoNumericFormat.Encode(root);
        Assert.False(C.TryObserve(root, issue, input, out var next, out var assessment));
        Assert.Same(root, next);
        Assert.False(assessment.Left is CarrierLocation);
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    private static void AssertEquivalent(Relation a, Relation b)
    {
        Assert.Equal(C.Level(C.Sensor(a)).Ticks, C.Level(C.Sensor(b)).Ticks);
        Assert.Equal(C.Clock(a).Value, C.Clock(b).Value);
        Assert.Equal(C.Pressure(a).Tension, C.Pressure(b).Tension);
        Assert.Equal(C.Step(C.Selected(a)).Ticks, C.Step(C.Selected(b)).Ticks);
        var ea = C.Proposals(a); var eb = C.Proposals(b);
        Assert.Equal(ea.Length, eb.Length);
        Assert.Equal(C.Connections(a).Length, C.Connections(b).Length);
        for (var i = 0; i < ea.Length; i++)
        {
            Assert.Equal(C.Step(Assert.IsType<Relation>(Route(ea[i]).Left)).Ticks,
                C.Step(Assert.IsType<Relation>(Route(eb[i]).Left)).Ticks);
            Assert.Equal(Duration(ea[i]), Duration(eb[i]));
            Assert.Equal(Credit(ea[i]).Value, Credit(eb[i]).Value);
            Assert.Equal(Credit(ea[i]).Tension, Credit(eb[i]).Tension);
        }
    }

    private static Relation Claim(Relation assessment, int index) => Assert.IsType<Relation>(C.Parts(assessment)[index]);
    private static Relation Route(Relation edge) => Assert.IsType<Relation>(C.Parts(edge)[0]);
    private static Carrier Credit(Relation edge) => Assert.IsType<Carrier>(C.Parts(edge)[2]);
    private static long Duration(Relation edge) => C.Length(Assert.IsType<Relation>(C.Parts(edge)[1])).Ticks;
    private static bool SameRoute(Relation a, Relation b) =>
        ReferenceEquals(Route(a).Left, Route(b).Left) && ReferenceEquals(Route(a).Right, Route(b).Right);
    private static long RegularDelta(int index) => index % 8 < 3 ? 2 : -2;
    private static long GraphSize(Relation root)
    {
        var graph = GojoEncodedSnapshot.CaptureGraph(root);
        return (long)graph.Carriers.Length + graph.Relations.Length + graph.TickRefs.Length +
            graph.CarrierLocations.Length + graph.UnitShapes.Length + graph.Placeholders.Length;
    }
}
