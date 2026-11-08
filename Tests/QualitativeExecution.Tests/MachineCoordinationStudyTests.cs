using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class MachineCoordinationStudyTests
{
    [Fact]
    public void A_pending_gate_bounds_repeated_debt_but_does_not_certify_the_goal()
    {
        var unguarded = MachineCoordinationStudy.Execute("pending", "capacity", "repeat");
        var guarded = MachineCoordinationStudy.Execute("pending", "capacity", "pending-gate");
        Assert.True(unguarded.FinalTension > 1); Assert.Equal(1, guarded.FinalTension);
        Assert.False(guarded.GoalReached); Assert.True(guarded.Suppressed > 0);
        Assert.Contains(guarded.Frames, f => f.OutputMask == 2 && f.Value != f.Goal && f.Tension != 0);
        var released = MachineCoordinationStudy.Execute("pending", "release", "pending-gate");
        Assert.True(released.GoalReached);
        Assert.Contains(released.Frames, f => f.Request == -1 && f.Accepted == 0);
    }

    [Fact]
    public void Delayed_acknowledgement_converges_with_a_fixed_source_but_loss_stays_visible()
    {
        for (var seed = 0; seed < 3; seed++)
        {
            var run = MachineCoordinationStudy.Execute("channel", "fixed", "acknowledgement", seed);
            Assert.True(run.GoalReached); Assert.Equal(0, run.FinalTension);
            Assert.Contains(run.Frames, frame => frame.Waiting && !frame.Consumed && frame.Request is null);
            Assert.Contains(run.Frames, frame => frame.Acknowledged);
        }
        var lost = MachineCoordinationStudy.Execute("channel", "lost-ack", "acknowledgement");
        Assert.True(lost.Waiting); Assert.False(lost.GoalReached);
        Assert.Equal(1, lost.Requests); Assert.DoesNotContain(lost.Frames, f => f.Acknowledged);
    }

    [Fact]
    public void Acknowledgement_of_a_suppressed_response_preserves_physical_debt()
    {
        var run = MachineCoordinationStudy.Execute("channel", "capacity", "acknowledgement-gate");
        Assert.Equal(1, run.FinalTension); Assert.False(run.GoalReached);
        Assert.Contains(run.Frames, f => f.Suppressed && f.Consumed && f.Request == 0 && f.Tension == 1);
        Assert.Contains(run.Frames, f => f.Acknowledged && f.Tension == 1);
    }

    [Fact]
    public void Priority_selects_a_goal_and_agreement_preserves_a_conflict()
    {
        var first = MachineCoordinationStudy.Execute("arbitration", "opposed", "prefer-first");
        var second = MachineCoordinationStudy.Execute("arbitration", "opposed", "prefer-second");
        var agreement = MachineCoordinationStudy.Execute("arbitration", "opposed", "agreement");
        Assert.True(first.GoalReached); Assert.False(first.OtherGoalReached);
        Assert.False(second.GoalReached); Assert.True(second.OtherGoalReached);
        Assert.True(first.Conflicts > 0); Assert.True(second.Conflicts > 0);
        // From outside both goals, compatible steps can reach one goal before
        // its hold conflicts with the other source's remaining request.
        Assert.False(agreement.GoalReached && agreement.OtherGoalReached == true);
        Assert.Contains(agreement.Frames, f => f.Conflict && f.Request is null && f.OutputMask == 0);
    }

    [Fact]
    public void A_satisfied_local_gap_does_not_establish_whole_outline_closure()
    {
        var run = MachineCoordinationStudy.Execute("gap", "reachable", "gap-feedback", family: "chevron");
        Assert.True(run.GoalReached); Assert.Equal(6, run.Frames[^1].Gap);
        Assert.False(run.Frames[^1].EndCoincident);
    }

    [Fact]
    public void An_unrepresentable_inset_gap_keeps_its_resolution_limit_visible()
    {
        var reachable = MachineCoordinationStudy.Execute("gap", "reachable", "gap-feedback", family: "inset");
        var unavailable = MachineCoordinationStudy.Execute("gap", "off-grid", "gap-feedback", family: "inset");
        Assert.True(reachable.GoalReached); Assert.Equal(9, reachable.Frames[^1].Gap);
        Assert.False(unavailable.GoalReached);
        Assert.Equal(new long?[] { 9, 12 }, unavailable.Frames.TakeLast(8).Select(f => f.Gap).Distinct().Order().ToArray());
        Assert.DoesNotContain(unavailable.Frames, f => f.Gap == 10);
    }

    [Fact]
    public void Native_drawing_history_connects_actual_public_ports_to_their_captured_samples()
    {
        var run = MachineCoordinationStudy.Execute("pending", "release", "pending-gate", captureNative: true);
        Assert.NotNull(run.NativeSurface);
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var todo = new Stack<IElement>(); todo.Push(run.NativeSurface);
        var count = 0;
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item) || item is not Relation relation) continue;
            todo.Push(relation.Left); todo.Push(relation.Right);
            if (relation is { Left: Relation { Left: CarrierLocation live, Right: Carrier owner }, Right: CarrierLocation sample } &&
                ReferenceEquals(live.Carrier, owner) && live.SameTickRefAs(owner.ValueSlot) &&
                ReferenceEquals(sample.Carrier, owner) && !sample.SameTickRefAs(live)) count++;
        }
        Assert.True(count >= 4 * run.Frames.Length);
    }

    [Fact]
    public void A_composed_inflight_request_restores_with_the_same_gate_and_selector_contract()
    {
        var machine = CoupledExtentMachine.Create(3, 3); machine.Step(machine.Publication(4));
        var gate = PendingAdjustmentGate.Create(machine);
        var selector = ExtentResponseArbiter.Create(machine, "first");
        var channel = AcknowledgedComparisonChannel.Create(machine);
        var selected = selector.Evaluate(machine.Publication(4), machine.Publication(1));
        Assert.True(channel.Offer(selected.Packet).Accepted); Assert.True(channel.Dispatch().Accepted);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(machine.ArchiveSurface(), gate.Surface, selector.Surface, channel.ArchiveSurface()));
        var copy = CoupledExtentMachine.Restore((Relation)roots[0]);
        var copiedGate = PendingAdjustmentGate.FromSurface((Relation)roots[1]);
        var copiedSelector = ExtentResponseArbiter.FromSurface((Relation)roots[2]);
        var copiedChannel = AcknowledgedComparisonChannel.Restore((Relation)roots[3]);
        var packet = copiedChannel.Snapshot().Packet!;
        var hold = copy.Step(copiedGate.Evaluate(packet).Packet);
        Assert.Equal(0, hold.RequestedDelta); Assert.Equal(1, hold.After.Tension);
        var receipt = copiedChannel.Receipt(copy, hold);
        Assert.True(receipt.Accepted); Assert.True(copiedChannel.Acknowledge(receipt.Receipt!).Accepted);
        Assert.False(copy.Step(copiedGate.Evaluate(packet).Packet).ConsumedEvent);
        var next = copiedSelector.Evaluate(copy.Publication(1), copy.Publication(4));
        copy.Step(copiedGate.Evaluate(next.Packet).Packet);
        Assert.Equal(0, copy.Snapshot().Tension); Assert.Equal(3, copy.Snapshot().Value);
        Assert.Equal(1, machine.Snapshot().Tension);
    }
}
