using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class PendingAdjustmentGateTests
{
    [Fact]
    public void Repeated_pressure_holds_after_one_blocked_request_without_erasing_it()
    {
        var machine = CoupledExtentMachine.Create(3, 3);
        var gate = PendingAdjustmentGate.Create(machine);
        for (var i = 0; i < 12; i++) machine.Step(gate.Evaluate(machine.Publication(4)).Packet);
        Assert.Equal(3, machine.Snapshot().Value);
        Assert.Equal(1, machine.Snapshot().Tension);
        Assert.Equal(12, machine.Snapshot().Beat);
    }

    [Fact]
    public void Opposed_response_accounts_for_old_debt_before_visible_shrinkage()
    {
        var machine = CoupledExtentMachine.Create(3, 3);
        var gate = PendingAdjustmentGate.Create(machine);
        machine.Step(gate.Evaluate(machine.Publication(4)).Packet);
        var opposite = machine.Step(gate.Evaluate(machine.Publication(1)).Packet);
        Assert.Equal(-1, opposite.IntroducedDelta); Assert.Equal(0, opposite.AcceptedDelta);
        Assert.Equal(-1, opposite.PendingDelta); Assert.Equal(0, opposite.After.Tension);
        Assert.Equal(2, machine.Step(gate.Evaluate(machine.Publication(1)).Packet).After.Value);
    }

    [Fact]
    public void Negative_debt_is_guarded_symmetrically()
    {
        var machine = CoupledExtentMachine.Create(0, 3); var gate = PendingAdjustmentGate.Create(machine);
        for (var i = 0; i < 6; i++) machine.Step(gate.Evaluate(machine.Publication(1)).Packet);
        Assert.Equal(-1, machine.Snapshot().Tension);
        machine.Step(gate.Evaluate(machine.Publication(4)).Packet);
        Assert.Equal(0, machine.Snapshot().Tension); Assert.Equal(0, machine.Snapshot().Value);
    }

    [Fact]
    public void Missing_pending_read_and_missing_comparison_do_not_become_measured_zero()
    {
        var machine = CoupledExtentMachine.Create(2); var gate = PendingAdjustmentGate.Create(machine);
        var pendingMissing = gate.Evaluate(machine.Publication(4), pendingAvailable: false);
        Assert.False(pendingMissing.Resolved); Assert.Null(pendingMissing.Pending);
        Assert.Null(machine.Step(pendingMissing.Packet).RequestedDelta);
        Assert.False(gate.Evaluate(machine.Publication(0)).Resolved);
        var equal = gate.Evaluate(machine.Publication(2));
        Assert.True(equal.Resolved); Assert.Equal(2, equal.OutputMask);
    }

    [Fact]
    public void Foreign_contract_stays_open_and_output_cannot_reach_private_pending_port()
    {
        var machine = CoupledExtentMachine.Create(2); var gate = PendingAdjustmentGate.Create(machine);
        Assert.False(gate.Evaluate(CoupledExtentMachine.Create(2).Publication(4)).Resolved);
        var output = gate.Evaluate(machine.Publication(4));
        var fields = FiniteLaw.Entries(gate.Surface);
        Assert.DoesNotContain(Reachable(output.Packet), item => ReferenceEquals(item, fields[1]));
        Assert.Contains(Reachable(output.Witness), item => ReferenceEquals(item, fields[1]));
    }

    [Fact]
    public void A_known_answer_with_foreign_input_residue_cannot_become_an_actionable_singleton()
    {
        var machine = CoupledExtentMachine.Create(2); var gate = PendingAdjustmentGate.Create(machine);
        var support = StructuralConstraint.Closed(FiniteLaw.Entries(machine.ComparisonDomain)[2], new Placeholder());
        var packet = QualitativePublication.Create(machine.Sensor, machine.Owner, new Placeholder(),
            machine.ComparisonBasis, machine.ComparisonDomain, support);
        var read = gate.Evaluate(packet);
        Assert.False(read.Resolved); Assert.Equal(0, read.OutputMask);
        Assert.Null(machine.Step(read.Packet).RequestedDelta);
        Assert.Equal(2, machine.Snapshot().Value);
    }

    [Fact]
    public void Suppressed_event_is_not_a_new_request_when_replayed()
    {
        var machine = CoupledExtentMachine.Create(3, 3); var gate = PendingAdjustmentGate.Create(machine);
        machine.Step(machine.Publication(4));
        var packet = machine.Publication(4); machine.Step(gate.Evaluate(packet).Packet);
        machine.Step(machine.Publication(1));
        var replay = machine.Step(gate.Evaluate(packet).Packet);
        Assert.False(replay.ConsumedEvent); Assert.Equal(0, replay.After.Tension);
    }

    [Fact]
    public void Composed_archive_preserves_live_pending_port_and_native_compiled_reads()
    {
        var machine = CoupledExtentMachine.Create(3, 3); var gate = PendingAdjustmentGate.Create(machine);
        machine.Step(machine.Publication(4));
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(machine.ArchiveSurface(), gate.Surface));
        var copy = CoupledExtentMachine.Restore((Relation)restored[0]);
        var copiedGate = PendingAdjustmentGate.FromSurface((Relation)restored[1]);
        for (byte mask = 0; mask < 8; mask++)
        {
            var packet = copy.Publication(mask);
            var native = copiedGate.Evaluate(packet, compiled: false); var compact = copiedGate.Evaluate(packet);
            Assert.Equal(native.OutputMask, compact.OutputMask); Assert.Equal(native.Resolved, compact.Resolved);
        }
        copy.Step(copiedGate.Evaluate(copy.Publication(1)).Packet);
        Assert.Equal(0, copiedGate.Evaluate(copy.Publication(4)).Pending);
        Assert.Equal(1, gate.Evaluate(machine.Publication(4)).Pending);
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Stack<IElement>(); queue.Push(root);
        while (queue.TryPop(out var next)) { if (!seen.Add(next)) continue; yield return next;
            if (next is Relation relation) { queue.Push(relation.Left); queue.Push(relation.Right); } }
    }
}
