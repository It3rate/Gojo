using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxTwoSpaceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TwoLocalResponsesMeetUsingOnlyPublishedObservationsAcrossDifferentBases(int restores)
    {
        var w = new World(restores);
        var a = w.A; var b = w.B;
        var am = a.Publish(1); var bm = b.Publish(101);
        Assert.NotSame(a.Output, b.Output); Assert.NotSame(a.Clock, b.Clock);
        Assert.NotSame(F(a.Report)[3], F(b.Report)[3]);
        Assert.DoesNotContain(a.Gain, am.Carriers());
        Assert.DoesNotContain(b.Gain, bm.Carriers());
        Assert.DoesNotContain(Assert.IsType<CarrierLocation>(F(a.Report)[2]).Carrier, am.Carriers());
        Number(F(F(am)[0])[2], a.Output, 5);
        Number(F(F(bm)[0])[2], b.Output, 18);
        var ar = a.Receive(bm, 2); var br = b.Receive(am, 102);
        Number(F(F(ar)[2])[2], a.Output, 9);
        Number(F(F(br)[2])[2], b.Output, 10);
        Assert.Same(F(a.Report)[3], F(F(ar)[2])[3]);
        Assert.Same(F(a.Report)[4], F(F(ar)[2])[4]);
        Assert.Equal(50UL, a.Gain.Value); Assert.Equal(180UL, b.Gain.Value);
        var ap = a.Plan(ar); var bp = b.Plan(br);
        a.Respond(ap, ar); b.Respond(bp, br);
        a.Expect(7); b.Expect(14);
        Assert.Equal(0UL, a.Output.Value); Assert.Equal(0UL, b.Output.Value);
        Number(F(F(am)[0])[2], a.Output, 5); Number(F(F(bm)[0])[2], b.Output, 18);

        // New publications retain immediate exposed references, not private ancestry.
        var nextA = a.Publish(3); var nextB = b.Publish(103);
        var ancestor = R(Assert.Single(F(F(nextA)[1])));
        Assert.Same(b.P[3], ancestor.Left);
        Assert.Same(Assert.IsType<CarrierLocation>(F(F(bm)[0])[1]).Tick,
            Assert.IsType<CarrierLocation>(ancestor.Right).Tick);
        Assert.DoesNotContain(b.Gain, nextA.Carriers());
        Assert.IsType<Placeholder>(F(nextA)[2]);
        ar = a.Receive(nextB, 4); br = b.Receive(nextA, 104);
        var agreedA = a.Comparison(ar); var agreedB = b.Comparison(br);
        Number(F(agreedA)[2], a.Output, 0); Number(F(agreedB)[2], b.Output, 0);
        // Agreement at seven is still challenged by each local reference at two.
        Number(F(a.AnchorComparison())[2], a.Output, -5);
        Number(F(b.AnchorComparison())[2], b.Output, -10);
    }

    [Fact]
    public void FullResponsesSwapThePreviousViewsUntilEachLocalBudgetIsExhausted()
    {
        var w = new World();
        for (var i = 0; i < 6; i++)
        {
            var am = w.A.Publish(1 + i); var bm = w.B.Publish(101 + i);
            var ar = w.A.Receive(bm, 20 + i); var br = w.B.Receive(am, 120 + i);
            var ap = w.A.Plan(ar, full: true); var bp = w.B.Plan(br, full: true);
            w.A.Respond(ap, ar); w.B.Respond(bp, br);
            w.A.Expect(i % 2 == 0 ? 9 : 5); w.B.Expect(i % 2 == 0 ? 10 : 18);
        }
        var nextA = w.A.Publish(7); var nextB = w.B.Publish(107);
        var receiptA = w.A.Receive(nextB, 30); var receiptB = w.B.Receive(nextA, 130);
        foreach (var (space, receipt) in new[] { (w.A, receiptA), (w.B, receiptB) })
        {
            var plan = space.Plan(receipt, full: true);
            var request = space.Response(plan, receipt);
            Assert.False(LocalCalibration.TryApplyReceived(request, out var blocked, out var history, out var inbox));
            Assert.Same(space.History, history); Assert.Same(space.Inbox, inbox);
            Assert.Same(request, F(blocked)[3]);
            Assert.Equal(6, F(history).Length);
        }
        w.A.Expect(5); w.B.Expect(18);
    }

    [Fact]
    public void SeparateLocalEvidenceChangesAnAgreedViewWithoutAnExternalDispatcherChoosingTheAnswer()
    {
        var w = new World();
        Exchange(w, 1, 101);
        w.A.Expect(7); w.B.Expect(14);
        w.A.Anchor(); w.B.Anchor();
        w.A.Expect(2); w.B.Expect(4);
        Exchange(w, 2, 102);
        w.A.Expect(2); w.B.Expect(4);
        Assert.Equal(3, F(w.A.History).Length); Assert.Equal(3, F(w.B.History).Length);
        Number(F(w.A.AnchorComparison())[2], w.A.Output, 0);
        Number(F(w.B.AnchorComparison())[2], w.B.Output, 0);
    }

    [Fact]
    public void IncompatibleLocalEvidenceRepeatedlyDisturbsPeerAgreement()
    {
        var w = new World();
        w.Roots[1] = Replace(w.Roots[1], 2, Replace(w.B.P[2], 2, new CarrierLocation(w.B.Output, 16)));
        for (var i = 0; i < 3; i++)
        {
            w.A.Anchor(); w.B.Anchor();
            w.A.Expect(2); w.B.Expect(16);
            Exchange(w, i + 1, i + 101);
            w.A.Expect(5); w.B.Expect(10);
            Number(F(w.A.AnchorComparison())[2], w.A.Output, -3);
            Number(F(w.B.AnchorComparison())[2], w.B.Output, 6);
        }
        Assert.Equal(6, F(w.A.History).Length); Assert.Equal(6, F(w.B.History).Length);
        Assert.False(LocalCalibration.TryApplyResponse(w.A.LocalResponse(w.A.AnchorPlan()), out _, out var retained));
        Assert.Same(w.A.History, retained);
    }

    [Fact]
    public void AReceiverCanUseADelayedPublicationWithoutReadingTheSendersCurrentPrivateState()
    {
        var w = new World();
        var am = w.A.Publish(1); var oldB = w.B.Publish(101);
        var br = w.B.Receive(am, 102);
        w.B.Respond(w.B.Plan(br), br);
        w.B.Expect(14);
        var newB = w.B.Publish(103); // Still in transit; A has not received it.
        var delayed = w.A.Receive(oldB, 2000);
        Number(F(F(delayed)[2])[2], w.A.Output, 9);
        w.A.Respond(w.A.Plan(delayed), delayed);
        w.A.Expect(7);
        var current = w.A.Receive(newB, 2001);
        Number(F(F(current)[2])[2], w.A.Output, 7);
        Assert.False(LocalCalibration.TryReceive(w.A.Delivery(oldB, 2002), out var stale, out var inbox));
        Assert.Same(w.A.Inbox, inbox); Assert.IsType<Placeholder>(F(stale)[2]);
        w.A.Expect(7);
    }

    [Fact]
    public void NewerReceiptInvalidatesAPlanEvenWhenItsNumericValueIsUnchanged()
    {
        var w = new World();
        var first = w.A.Receive(w.B.Publish(101), 1);
        var oldPlan = w.A.Plan(first);
        var second = w.A.Receive(w.B.Publish(102), 2);
        Number(F(F(first)[2])[2], w.A.Output, 9); Number(F(F(second)[2])[2], w.A.Output, 9);
        Assert.False(LocalCalibration.TryApplyReceived(w.A.Response(oldPlan, first), out _, out var history, out var inbox));
        Assert.Same(w.A.History, history); Assert.Same(w.A.Inbox, inbox);
        Assert.False(LocalCalibration.TryApplyReceived(w.A.Response(oldPlan, second), out _, out _, out _));
        w.A.Respond(w.A.Plan(second), second);
        w.A.Expect(7);
    }

    [Fact]
    public void PendingPublicationReplacesCurrentReadWithoutFallingBackToOlderConcreteEvidence()
    {
        var w = new World();
        var first = w.A.Receive(w.B.Publish(101), 1);
        var oldPlan = w.A.Plan(first);
        var pending = w.B.Pending(102);
        var receipt = w.A.Receive(pending, 2);
        Assert.IsType<Placeholder>(F(F(receipt)[2])[2]);
        Assert.Same(pending, F(receipt)[3]);
        Assert.False(LocalCalibration.TryCompare(w.A.Comparing(receipt), out _));
        Assert.False(LocalCalibration.TryApplyReceived(w.A.Response(oldPlan, first), out _, out _, out _));
        w.A.Expect(5);
        Number(F(F(first)[2])[2], w.A.Output, 9);
        var ready = w.A.Receive(w.B.Publish(103), 3);
        w.A.Respond(w.A.Plan(ready), ready);
        w.A.Expect(7);
    }

    [Fact]
    public void DuplicateDeliveryAndRepeatedConsumptionDoNotCauseExtraParameterChanges()
    {
        var w = new World(); var message = w.B.Publish(101);
        var receipt = w.A.Receive(message, 1);
        Assert.False(LocalCalibration.TryReceive(w.A.Delivery(message, 2), out _, out var sameInbox));
        Assert.Same(w.A.Inbox, sameInbox);
        var equalSequence = w.B.Publish(101);
        Assert.NotSame(F(F(message)[0])[1], F(F(equalSequence)[0])[1]);
        Assert.False(LocalCalibration.TryReceive(w.A.Delivery(equalSequence, 3), out _, out _));
        w.A.Respond(w.A.Plan(receipt), receipt);
        w.A.Expect(7);
        // Replanning with the new local state cannot consume this receipt again.
        var repeated = w.A.Plan(receipt);
        Assert.False(LocalCalibration.TryApplyReceived(w.A.Response(repeated, receipt), out _, out _, out _));
        Assert.Single(F(w.A.History));
        w.A.Expect(7);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void RestoredInFlightMessageAndPartiallyAppliedCyclePreserveBothLocalVersions(int restores)
    {
        var original = new World();
        var am = original.A.Publish(1); var bm = original.B.Publish(101);
        var ar = original.A.Receive(bm, 2);
        original.A.Respond(original.A.Plan(ar), ar); // B has not received A's original publication yet.
        var packet = new IElement[] { G(original.Roots), original.A.State, original.A.History, original.A.Inbox,
            original.B.State, original.B.History, original.B.Inbox, am, bm };
        for (var i = 0; i < restores; i++) packet = GojoNumericFormat.Decode(GojoNumericFormat.Encode(packet));
        var w = new World(F(packet[0]), packet[1..7]);
        w.A.Expect(7); w.B.Expect(18);
        var br = w.B.Receive(R(packet[7]), 102);
        Number(F(F(br)[2])[2], w.B.Output, 10); // Historical A=5, not A's current 7.
        w.B.Respond(w.B.Plan(br), br);
        w.B.Expect(14);
        Assert.False(LocalCalibration.TryReceive(w.A.Delivery(R(packet[8]), 3), out _, out _));
        var next = w.A.Publish(3);
        Assert.Same(w.B.P[3], R(Assert.Single(F(F(next)[1]))).Left);
        Assert.Single(F(w.A.History)); Assert.Single(F(w.B.History));
    }

    [Fact]
    public void PortSubjectContextClockAndScalarBasisAreExplicitAdmissionConditions()
    {
        var w = new World(); var message = w.B.Publish(101); var observation = F(message)[0];
        var foreign = new Carrier(w.B.Output.UnitShape);
        foreach (var invalid in new[]
        {
            Replace(message, 0, Replace(observation, 0, G(Relation.Segment(w.B.Output, 0, 1), w.B.Clock))),
            Replace(message, 0, Replace(observation, 1, new CarrierLocation(w.A.Clock, 101))),
            Replace(message, 0, Replace(observation, 2, new CarrierLocation(foreign, 18))),
            Replace(message, 0, Replace(observation, 3, new Placeholder())),
            Replace(message, 0, Replace(observation, 4, new Placeholder()))
        })
        {
            var request = w.A.Delivery(invalid, 1);
            Assert.False(LocalCalibration.TryReceive(request, out var refused, out var inbox));
            Assert.Same(w.A.Inbox, inbox); Assert.Same(request, F(refused)[3]);
        }
        Assert.False(LocalCalibration.TryReceive(w.Bind(1, (6,message),(7,w.A.Inbox),
            (8,new CarrierLocation(w.B.Clock,1))), out _, out _));
        w.A.Expect(5); w.B.Expect(18);
    }

    [Fact]
    public void MessageRestoredAloneNeedsExplicitReconnectionRatherThanMatchingNumbers()
    {
        var w = new World();
        var message = R(GojoNumericFormat.Decode(GojoNumericFormat.Encode(w.B.Publish(101)))[0]);
        Assert.False(LocalCalibration.TryReceive(w.A.Delivery(message, 1), out _, out _));
        var observation = F(F(message)[0]);
        var remoteBasis = Assert.IsType<CarrierLocation>(observation[2]).Carrier;
        var link = G(observation[0], w.A.P[4], new Relation(observation[3], F(w.A.Report)[3]),
            new Relation(observation[4], F(w.A.Report)[4]),
            new Relation(Relation.Segment(remoteBasis, 0, 2), Relation.Segment(w.A.Output, 0, 1)));
        w.A.Inbox = G(link, new Placeholder(), new Placeholder());
        var receipt = w.A.Receive(message, 2);
        w.A.Respond(w.A.Plan(receipt), receipt);
        w.A.Expect(7);
    }

    [Fact]
    public void UnknownAntecedentsStayUnknownAndRepeatedProvenanceDoesNotBecomeIndependentEvidence()
    {
        var w = new World();
        var published = w.B.Publish(101, new Placeholder());
        Assert.IsType<Placeholder>(F(published)[1]);
        var first = w.A.Receive(published, 1);
        Assert.IsType<Placeholder>(F(F(first)[4])[1]);
        w.A.Respond(w.A.Plan(first), first);
        var returned = w.A.Publish(2);
        var origin = R(Assert.Single(F(F(returned)[1])));
        Assert.Same(w.B.P[3], origin.Left);
        var br = w.B.Receive(returned, 102);
        Assert.Same(origin, Assert.Single(F(F(F(br)[4])[1])));
        Assert.IsType<Placeholder>(F(F(br)[4])[2]);
        Assert.DoesNotContain(w.A.Gain, returned.Carriers());
    }

    [Fact]
    public void AuthoredRepeatKeepsPerSpaceControllersAndSharedRequestTemplates()
    {
        var w = new World();
        for (var i = 0; i < 2; i++)
        {
            Assert.True(RepeatSurface.TryRead(R(w.Roots[6+i]), out var focus, out var controller,
                out var transition, out var feedback, out _, out var timeline));
            var space = i == 0 ? w.A : w.B;
            Assert.Same(space.P[5], focus);
            Assert.Same(space.StepAxis, Assert.IsType<CarrierLocation>(R(R(controller).Left).Left).Carrier);
            Assert.NotSame(space.Clock, space.StepAxis);
            var actions = F(transition);
            var requestOrder = new[] { 5, 0, 1, 6, 2, 3 };
            Assert.Equal(requestOrder.Length, actions.Length);
            for (var j = 0; j < requestOrder.Length; j++)
            {
                Assert.True(TransformSurface.TryRead(R(actions[j]), out _, out var operation, out _, out _, out _));
                Assert.Same(F(w.Roots[4])[requestOrder[j]], operation);
            }
            Assert.Equal(3, F(feedback).Length);
            Assert.True(TimeStepSequence.TryRead(R(timeline), out _, out _, out var steps));
            Assert.Empty(steps);
        }
    }

    [Fact]
    public void ALocalRevisionInvalidatesTheOldPlanAndZeroResponseStillConsumesItsReceipt()
    {
        var w = new World();
        var receipt = w.A.Receive(w.B.Publish(101), 1);
        var oldPlan = w.A.Plan(receipt);
        w.A.Anchor();
        Assert.False(LocalCalibration.TryApplyReceived(w.A.Response(oldPlan, receipt), out _, out _, out _));
        w.A.Expect(2);
        w.B.Anchor();
        var sameValue = w.A.Receive(w.B.Publish(102), 2);
        w.A.Respond(w.A.Plan(sameValue), sameValue);
        w.A.Expect(2);
        Assert.False(LocalCalibration.TryApplyReceived(w.A.Response(w.A.Plan(sameValue), sameValue), out _, out _, out _));
        Assert.Equal(2, F(w.A.History).Length);
    }

    [Fact]
    public void ExhaustingOneSpaceDoesNotSpendTheOtherSpacesBudget()
    {
        var w = new World();
        w.Roots[0] = Replace(w.Roots[0], 11, Relation.Segment(w.A.StepAxis, 0, 1));
        Exchange(w, 1, 101);
        Assert.False(LocalCalibration.TryApplyResponse(w.A.LocalResponse(w.A.AnchorPlan()), out _, out _));
        w.B.Anchor();
        w.A.Expect(7); w.B.Expect(4);
        var receipt = w.B.Receive(w.A.Publish(2), 103);
        w.B.Respond(w.B.Plan(receipt), receipt);
        w.B.Expect(9);
        Assert.Single(F(w.A.History)); Assert.Equal(3, F(w.B.History).Length);
    }

    [Fact]
    public void AdmissionRetainsExactFractionalConversionAndRefusesOverflowBeforeReplacingTheInbox()
    {
        var w = new World(); var message = w.B.Publish(101);
        var link = Replace(F(w.A.Inbox)[0], 4,
            new Relation(Relation.Segment(w.B.Output, 0, 7), Relation.Segment(w.A.Output, 0, 1)));
        w.A.Inbox = G(link, new Placeholder(), new Placeholder());
        var receipt = w.A.Receive(message, 1);
        Number(F(F(receipt)[2])[2], w.A.Output, 18, 7);
        w.A.Respond(w.A.Plan(receipt), receipt);
        w.A.Expect(19, 5); // -85/7 actuator ticks requested, -12 applied; remainder retained.

        var overflowLink = Replace(link, 4,
            new Relation(Relation.Segment(w.B.Output, 0, 1), Relation.Segment(w.A.Output, 0, 2)));
        var freshInbox = G(overflowLink, new Placeholder(), new Placeholder());
        var large = Replace(message, 0, Replace(F(message)[0], 2, new CarrierLocation(w.B.Output, long.MaxValue)));
        var request = w.Bind(1, (6,large),(7,freshInbox),(8,new CarrierLocation(w.A.Clock,2)));
        Assert.False(LocalCalibration.TryReceive(request, out var refused, out var retained));
        Assert.Same(freshInbox, retained); Assert.Same(request, F(refused)[3]);
        w.A.Expect(19, 5);
    }

    [Fact]
    public void AReceiptCannotSubstituteAnUnrelatedScalarOrArrivalEventForTheDeliveredRead()
    {
        foreach (var changedField in new[] { 1, 2 })
        {
            var w = new World();
            var receipt = w.A.Receive(w.B.Publish(101), 1);
            var corrupted = Replace(receipt, 2, Replace(F(receipt)[2], changedField,
                new CarrierLocation(changedField == 1 ? w.A.Clock : w.A.Output, 999)));
            w.A.Inbox = Replace(w.A.Inbox, 1, corrupted);
            var request = w.A.Response(w.A.Plan(corrupted), corrupted);
            Assert.False(LocalCalibration.TryApplyReceived(request, out _, out var history, out var inbox));
            Assert.Same(w.A.History, history); Assert.Same(w.A.Inbox, inbox);
            w.A.Expect(5);
        }
    }

    [Theory]
    [InlineData("half")]
    [InlineData("full")]
    [InlineData("delay")]
    [InlineData("anchors")]
    public void IllustratedStagesMatchActualActuatorsAndRetainedPublications(string scenario)
    {
        var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Gallery", "two-space-feedback.html"));
        var data = System.Text.RegularExpressions.Regex.Match(html,
            "<script type=\"application/json\" id=\"traces\">(.*?)</script>",
            System.Text.RegularExpressions.RegexOptions.Singleline).Groups[1].Value;
        using var document = System.Text.Json.JsonDocument.Parse(data);
        var w = new World();
        Relation? am = null, bm = null, laterB = null;
        var actual = new List<long?[]>();
        long? Published(Relation? message)
        {
            if (message is null) return null;
            Assert.True(ProjectionResidue.TryRead(F(F(message)[0])[2], out _, out var n, out var d));
            Assert.Equal(BigInteger.One, d); return checked((long)n);
        }
        void Capture() => actual.Add([(long)w.A.Gain.Value, (long)w.B.Gain.Value,
            Published(am), Published(bm), Published(laterB)]);
        void Publish(long a, long b) { am = w.A.Publish(a); bm = w.B.Publish(b); }

        if (scenario == "half")
        {
            Capture(); Publish(1,101); Capture();
            var ar = w.A.Receive(bm!,2); var br = w.B.Receive(am!,102);
            var ap = w.A.Plan(ar); var bp = w.B.Plan(br); Capture();
            w.A.Respond(ap,ar); Capture(); w.B.Respond(bp,br); Capture();
            Publish(3,103); Capture();
            ar = w.A.Receive(bm!,4); br = w.B.Receive(am!,104);
            Number(F(w.A.Comparison(ar))[2],w.A.Output,0);
            Number(F(w.B.Comparison(br))[2],w.B.Output,0); Capture();
        }
        else if (scenario == "full")
        {
            Publish(1,101); Capture();
            for (var i = 0; i < 2; i++)
            {
                var ar = w.A.Receive(bm!,20+i); var br = w.B.Receive(am!,120+i);
                var ap = w.A.Plan(ar,true); var bp = w.B.Plan(br,true);
                w.A.Respond(ap,ar); Capture(); w.B.Respond(bp,br); Capture();
                if (i == 0) { Publish(2,102); Capture(); }
            }
        }
        else if (scenario == "delay")
        {
            Publish(1,101); Capture();
            var br = w.B.Receive(am!,102); w.B.Respond(w.B.Plan(br),br); Capture();
            laterB = w.B.Publish(103); Capture();
            var ar = w.A.Receive(bm!,2000); w.A.Respond(w.A.Plan(ar),ar); Capture();
            var oldB = bm!;
            ar = w.A.Receive(laterB,2001); bm = laterB; laterB = null;
            Number(F(w.A.Comparison(ar))[2],w.A.Output,0); Capture();
            Assert.False(LocalCalibration.TryReceive(w.A.Delivery(oldB,2002),out _,out _)); Capture();
        }
        else
        {
            Exchange(w,1,101); Publish(3,103); Capture();
            w.A.Anchor(); Capture(); w.B.Anchor(); Capture();
            Publish(4,104); Capture();
        }
        var expected = document.RootElement.GetProperty(scenario).EnumerateArray().Select(row =>
            new[] { "a", "b", "ma", "mb", "laterB" }.Select(name =>
                row.GetProperty(name).ValueKind == System.Text.Json.JsonValueKind.Null
                    ? (long?)null : row.GetProperty(name).GetInt64()).ToArray()).ToArray();
        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++) Assert.Equal(expected[i], actual[i]);
    }

    [Fact]
    public void IndependentApplicationTimelinesRetainTheirReceivedEvidenceAfterRestoration()
    {
        var w = new World(); Exchange(w,1,101); w.A.Anchor(); w.B.Anchor();
        Relation Timeline(Space space)
        {
            var applications = F(space.History);
            var steps = applications.Select((application,i) =>
            {
                var fields = F(application); var result = F(fields[2]);
                return TimeStepSequence.Step(new Carrier(),space.StepAxis,i,i+1,
                    result[0],application,result[2],fields[3]);
            }).ToArray();
            return TimeStepSequence.Create(new Carrier(),space.StepAxis,F(F(applications[0])[2])[0],steps);
        }
        var packet = GojoNumericFormat.Decode(GojoNumericFormat.Encode(Timeline(w.A),Timeline(w.B)));
        var clocks = new List<Carrier>();
        foreach (var timeline in packet)
        {
            Assert.True(TimeStepSequence.TryRead(R(timeline),out var origin,out var initial,out var steps));
            clocks.Add(origin.Carrier); Assert.Equal(2,steps.Length);
            Assert.True(TimeStepSequence.TryReadStep(steps[0],out _,out _,out _,out var before,
                out var application,out var after,out _));
            Assert.Same(initial,before);
            var receipt = F(F(application)[4])[1];
            var link = F(F(receipt)[4])[3];
            Assert.Same(F(link)[1],F(F(receipt)[2])[0]);
            Assert.True(TimeStepSequence.TryReadStep(steps[1],out _,out _,out _,out var nextBefore,
                out _,out _,out _));
            Assert.Same(after,nextBefore);
        }
        Assert.NotSame(clocks[0],clocks[1]);
    }

    private static void Exchange(World w, long aVersion, long bVersion)
    {
        var am = w.A.Publish(aVersion); var bm = w.B.Publish(bVersion);
        var ar = w.A.Receive(bm, aVersion + 20); var br = w.B.Receive(am, bVersion + 20);
        var ap = w.A.Plan(ar); var bp = w.B.Plan(br);
        w.A.Respond(ap, ar); w.B.Respond(bp, br);
    }

    // Harness views over local roots only. Their durable state is ordinary graph
    // structure. Neither participant's methods access the other Space instance.
    private sealed class World
    {
        public IElement[] Roots { get; }
        public Space A { get; }
        public Space B { get; }
        public World(int restores = 0)
        {
            Roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
                "Fixtures", "Syntax", "two-space-feedback.gojo"))).Roots.ToArray();
            for (var i = 0; i < restores; i++) Roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(Roots));
            A = new Space(this, 0); B = new Space(this, 1);
        }
        public World(IElement[] roots, IElement[] state)
        {
            Roots = roots;
            A = new Space(this, 0, state[..3]); B = new Space(this, 1, state[3..]);
        }
        public Relation Bind(int request, params (int Slot, IElement Value)[] values) =>
            R(StructuralPattern.Refine(F(Roots[4])[request],
                values.Select(v => new Relation(F(Roots[5])[v.Slot], v.Value)).ToArray()));
    }

    private sealed class Space
    {
        private readonly World world;
        private readonly int index;
        public IElement[] P => F(world.Roots[index]);
        public Relation Report => R(P[1]);
        public Carrier Gain => Assert.IsType<Carrier>(F(P[0])[2]);
        public Carrier Output => Assert.IsType<CarrierLocation>(F(P[2])[2]).Carrier;
        public Carrier Clock => Assert.IsType<CarrierLocation>(F(Report)[1]).Carrier;
        public Carrier StepAxis => Assert.IsType<CarrierLocation>(R(R(P[11]).Left).Left).Carrier;
        public Relation State { get; private set; }
        public Relation History { get; private set; }
        public Relation Inbox { get; set; }
        public Space(World world, int index, IElement[]? retained = null)
        {
            this.world = world; this.index = index;
            if (retained is not null) { State = R(retained[0]); History = R(retained[1]); Inbox = R(retained[2]); return; }
            Gain.Add((ulong)Assert.IsType<CarrierLocation>(R(P[10]).Left).Ticks);
            Assert.True(LocalCalibration.TryCapture(R(P[0]), out var state));
            State = state; History = R(P[12]); Inbox = R(world.Roots[index + 2]);
        }
        public Relation Predict()
        {
            Assert.True(LocalCalibration.TryPredict(world.Bind(5,(20,Report),(21,F(State)[2])), out var prediction));
            return prediction;
        }
        public void Expect(long n, long d = 1) => Number(F(Predict())[2], Output, n, d);
        public Relation Publish(long version, IElement? antecedents = null)
        {
            if (antecedents is null)
            {
                antecedents = G();
                if (F(Inbox)[2] is Relation consumed)
                {
                    var o = F(F(F(consumed)[0])[0]);
                    antecedents = G(new Relation(o[0], o[1]));
                }
            }
            var request = world.Bind(0, (0,State),(1,Predict()),(2,P[3]),
                (3,new CarrierLocation(Clock,version)),(4,antecedents),(5,new Placeholder()));
            Assert.True(LocalCalibration.TryPublish(request, out var publication)); return R(F(publication)[2]);
        }
        public Relation Pending(long version)
        {
            var slots = F(world.Roots[5]);
            return R(StructuralPattern.Refine(world.Roots[8], new Relation(slots[2],P[3]),
                new Relation(slots[3],new CarrierLocation(Clock,version)), new Relation(slots[18],F(Report)[3]),
                new Relation(slots[19],F(Report)[4]), new Relation(slots[4],G())));
        }
        public Relation Delivery(Relation message, long arrival) =>
            world.Bind(1, (6,message),(7,Inbox),(8,new CarrierLocation(Clock,arrival)));
        public Relation Receive(Relation message, long arrival)
        {
            Assert.True(LocalCalibration.TryReceive(Delivery(message, arrival), out var receipt, out var inbox));
            Inbox = inbox; return receipt;
        }
        public Relation Comparing(Relation receipt) =>
            world.Bind(6,(1,Predict()),(22,F(receipt)[2]),(23,P[9]),(20,Report));
        public Relation Comparison(Relation receipt)
        {
            Assert.True(LocalCalibration.TryCompare(Comparing(receipt), out var comparison)); return comparison;
        }
        private Relation PlanComparison(Relation comparison, bool full)
        {
            Assert.True(LocalCalibration.TryPlanResponse(world.Bind(2,(0,State),(9,comparison),
                (10,P[full ? 7 : 6]),(11,P[8]),(12,P[5])), out var plan)); return plan;
        }
        public Relation Plan(Relation receipt, bool full = false) => PlanComparison(Comparison(receipt), full);
        public Relation Response(Relation plan, Relation receipt) =>
            world.Bind(3,(13,plan),(0,State),(14,History),(15,P[11]),(16,receipt),(7,Inbox));
        public void Respond(Relation plan, Relation receipt)
        {
            Assert.True(LocalCalibration.TryApplyReceived(Response(plan, receipt), out var applied,
                out var history, out var inbox));
            State = R(F(F(applied)[2])[2]); History = history; Inbox = inbox;
        }
        public Relation AnchorComparison()
        {
            Assert.True(LocalCalibration.TryCompare(world.Bind(6,(1,Predict()),(22,P[2]),(23,P[9]),(20,Report)), out var comparison));
            return comparison;
        }
        public Relation AnchorPlan() => PlanComparison(AnchorComparison(), true);
        public Relation LocalResponse(Relation plan) => world.Bind(4,(13,plan),(0,State),(14,History),(15,P[11]));
        public void Anchor()
        {
            Assert.True(LocalCalibration.TryApplyResponse(LocalResponse(AnchorPlan()), out var applied, out var history));
            State = R(F(F(applied)[2])[2]); History = history;
        }
    }

    private static Relation R(IElement e) => Assert.IsType<Relation>(e);
    private static Relation G(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] F(IElement e) { Assert.True(R(e).TryReadOrderedGroup(out var fields)); return fields; }
    private static Relation Replace(IElement e, int index, IElement replacement)
    { var fields = F(e); fields[index] = replacement; return G(fields); }
    private static void Number(IElement e, Carrier basis, long n, long d = 1)
    {
        Assert.True(ProjectionResidue.TryRead(e, out var actual, out var numerator, out var denominator));
        Assert.Same(basis, actual); Assert.Equal((BigInteger)n * denominator, (BigInteger)numerator * d);
    }
}
