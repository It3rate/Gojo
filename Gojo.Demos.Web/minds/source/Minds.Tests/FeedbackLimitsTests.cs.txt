using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;
using Minds.FeedbackLimits;
using Minds.TwoSpace;

namespace Minds.Tests;

public sealed class FeedbackLimitsTests
{
    public static IEnumerable<object[]> Conditions => StudyProtocol.Cases.Select(c => new object[] { c.Id });

    [Theory]
    [MemberData(nameof(Conditions))]
    public void EverySelectedEventMatchesTheIndependentlyComputedContract(string id)
    {
        var c = Case(id); var tape = StudyProtocol.Tape(c);
        var actual = GojoStudy.Run(c, tape); var reference = ConventionalReference.Run(c, tape);
        Assert.Empty(StudyComparison.Compare(actual.Trace, reference));
        Assert.True(TimeStepSequence.TryRead(actual.Timeline, out _, out _, out var steps));
        Assert.Equal(tape.Count, steps.Length);
        Assert.Equal(c.Rounds + 1, actual.Trace.Rounds.Count);
    }

    [Fact]
    public void NativeUnitChangesPreservePhysicalTrajectoryWhenTickResolutionIsPreserved()
    {
        var baseline = Run(Case("quarter"));
        foreach (var id in new[] { "unit-one", "unit-four" })
        {
            var c = Case(id) with { ResponseDenominator = 4 };
            var scaled = Run(c);
            for (var i = 0; i < baseline.Rounds.Count; i++)
                foreach (var field in GojoStudy.MeasureNames)
                    Assert.Equal(baseline.Rounds[i].Values[field], scaled.Rounds[i].Values[field]);
        }
    }

    [Fact]
    public void StaleDeliveryIsRetainedWithoutReplacingTheCurrentReceipt()
    {
        var run = Run(Case("late"));
        var stale = Assert.Single(run.Events, e => e.Action == "Deliver" && e.Accepted == false);
        Assert.Contains("Old publication", stale.Note);
        Assert.Equal("5", stale.Values["a.latestSequence"]);
        Assert.Equal("3", stale.Values["a.consumedSequence"]);
        var next = run.Events[stale.Index + 1];
        Assert.True(next.Accepted); Assert.Equal("5", next.Values["a.consumedSequence"]);
    }

    [Fact]
    public void MissingDeliveryAndSelectedInactionRemainDifferentRecords()
    {
        var silence = Run(Case("disconnected")).Rounds[^1].Values;
        var inaction = Run(Case("no-response")).Rounds[^1].Values;
        Assert.Equal("none", silence["a.received"]); Assert.Equal("0", silence["admissions"]);
        Assert.Equal("9", inaction["a.received"]); Assert.Equal("24", inaction["admissions"]);
        Assert.Equal("0", silence["applicationCalls"]); Assert.Equal("0", inaction["applicationCalls"]);
        Assert.Equal("5", silence["a"]); Assert.Equal("5", inaction["a"]);
    }

    [Fact]
    public void ControllerExhaustionDoesNotEraseRequestsOrPretendTheyWereApplied()
    {
        var run = Run(Case("budget"));
        var refused = run.Events.Where(e => e.Action == "Respond" && e.Accepted == false).ToArray();
        Assert.Equal(20, refused.Length);
        var last = run.Rounds[^1].Values;
        Assert.Equal("24", last["applicationCalls"]); Assert.Equal("2", last["a.applications"]);
        Assert.Equal("0", last["a.remaining"]); Assert.Equal("5/2", last["a.requested"]);
        Assert.Equal("1/2", last["a.fraction"]); Assert.Equal("0", last["a.tension"]);
        Assert.Equal("13/2", last["a"]); Assert.Equal("15/2", last["b"]);
    }

    [Fact]
    public void NewReferenceObservationDoesNotMutateActuatorOrPreviousEvidence()
    {
        var roots = GojoSyntax.Parse(StudyProtocol.Fixture(Case("half"))).Roots;
        var a = new Space(roots[0], roots[2], roots[4], roots[5]);
        var b = new Space(roots[1], roots[3], roots[4], roots[5]);
        var observer = new PairObserver(a.Output); var before = observer.Observe(a, b);
        var state = a.State; var history = a.History; var original = a.Reference;
        a.ObserveReference(4, 1000);
        var after = observer.Observe(a, b);
        Assert.Same(state, a.State); Assert.Same(history, a.History); Assert.Equal(50UL, a.Actuator.Value);
        Assert.NotSame(original, a.Reference);
        Number(Fields(original)[2], 2); Number(Fields(a.Reference)[2], 4);
        Number(PairObserver.Scalar((Relation)Fields(before)[8]), -3);
        Number(PairObserver.Scalar((Relation)Fields(after)[8]), -1);
    }

    [Fact]
    public void DifferenceCheckerCannotReportAChangedRecordedValueAsAMatch()
    {
        var run = ConventionalReference.Run(Case("half"), StudyProtocol.Tape(Case("half")));
        var changed = run.Events.ToArray();
        var values = new Dictionary<string, string>(changed[1].Values) { ["a"] = "123" };
        changed[1] = changed[1] with { Values = values };
        var difference = Assert.Single(StudyComparison.Compare(new StudyTrace(changed, run.Rounds), run));
        Assert.Equal("a", difference.Field); Assert.Equal(1, difference.Index);
    }

    private static StudyCase Case(string id) => StudyProtocol.Cases.Single(c => c.Id == id);
    private static StudyTrace Run(StudyCase c) => GojoStudy.Run(c, StudyProtocol.Tape(c)).Trace;
    private static IElement[] Fields(IElement e)
    { Assert.True(Assert.IsType<Relation>(e).TryReadOrderedGroup(out var fields)); return fields; }
    private static void Number(IElement e, long expected)
    { Assert.True(ProjectionResidue.TryRead(e, out _, out var n, out var d)); Assert.Equal(expected * d, n); }
}
