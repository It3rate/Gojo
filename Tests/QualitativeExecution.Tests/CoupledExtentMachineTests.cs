using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class CoupledExtentMachineTests
{
    [Fact]
    public void Feedback_steps_toward_a_published_goal_without_receiving_its_magnitude()
    {
        var machine = CoupledExtentMachine.Create(1, 8);
        var target = 5L;
        for (var i = 0; i < 8; i++)
        {
            var before = machine.Snapshot();
            byte mask = before.Value < target ? (byte)4 : before.Value > target ? (byte)1 : (byte)2;
            var publication = machine.Publication(mask);
            Assert.DoesNotContain(Reachable(publication), element => ReferenceEquals(element, machine.ExtentPort));
            var step = machine.Step(publication);
            Assert.True(step.Resolved); Assert.True(step.ConsumedEvent);
            Assert.Equal(step.RequestedDelta, step.AcceptedDelta + step.PendingDelta + step.UnroutedDelta);
            Assert.Equal(before.Value + (before.Value < target ? 1 : 0), step.After.Value);
        }
        Assert.Equal(5, machine.Snapshot().Value); Assert.Equal(0, machine.Snapshot().Tension);
        Assert.Equal(8, machine.History.Count);
    }

    [Fact]
    public void Missing_and_ambiguous_comparisons_preserve_distinct_native_support_and_do_not_act()
    {
        var machine = CoupledExtentMachine.Create(3);
        var missing = machine.Step(machine.Publication(0));
        var uncertain = machine.Step(machine.Publication(7));
        Assert.False(missing.Resolved); Assert.False(uncertain.Resolved);
        Assert.Null(missing.RequestedDelta); Assert.Null(uncertain.RequestedDelta);
        Assert.Equal(3, machine.Snapshot().Value); Assert.Equal(2, machine.Snapshot().Beat);
        Assert.True(FiniteLaw.TryReadResult(missing.LawRead, out var a, out var pending, out _));
        Assert.True(StructuralConstraint.TryReadScope(a, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail); Assert.NotEmpty(FiniteLaw.Entries(pending));
        Assert.True(FiniteLaw.TryReadResult(uncertain.LawRead, out var b, out _, out _));
        Assert.Equal(3, FiniteLaw.Entries(b).Length);
        var exact = machine.Step(machine.Publication(2));
        Assert.True(exact.Resolved); Assert.Equal(0, exact.RequestedDelta); Assert.False(exact.Blocked);
    }

    [Fact]
    public void Full_capacity_counts_every_arrival_and_opposed_requests_pay_existing_tension_first()
    {
        var machine = CoupledExtentMachine.Create(2, 2);
        var first = machine.Step(machine.Publication(4));
        var second = machine.Step(machine.Publication(4));
        Assert.Equal(0, first.AcceptedDelta); Assert.Equal(1, first.PendingDelta);
        Assert.Equal(2, second.After.Tension); Assert.True(second.Blocked);
        var release = machine.Step(machine.Publication(1));
        Assert.Equal(2, release.After.Value); Assert.Equal(1, release.After.Tension);
        Assert.Equal(0, release.AcceptedDelta); Assert.Equal(-1, release.PendingDelta);
        machine.Step(machine.Publication(1));
        var draw = machine.Step(machine.Publication(1));
        Assert.Equal(1, draw.After.Value); Assert.Equal(0, draw.After.Tension);
        Assert.All(new[] { first, second, release, draw }, step =>
            Assert.Equal(step.RequestedDelta, step.AcceptedDelta + step.PendingDelta + step.UnroutedDelta));
    }

    [Fact]
    public void A_missing_outlet_is_unrouted_request_not_a_secret_negative_assignment()
    {
        var machine = CoupledExtentMachine.Create(3, allowDecrease: false);
        var read = machine.Step(machine.Publication(1));
        Assert.True(read.Resolved); Assert.True(read.Blocked);
        Assert.Equal(-1, read.RequestedDelta); Assert.Equal(-1, read.UnroutedDelta);
        Assert.Equal(0, read.IntroducedDelta); Assert.Equal(0, read.PendingDelta);
        Assert.Equal(3, read.After.Value); Assert.Equal(0, read.After.Tension);
    }

    [Fact]
    public void A_negative_request_at_zero_retains_unmet_draw_without_changing_orientation()
    {
        var machine = CoupledExtentMachine.Create();
        var draw = machine.Step(machine.Publication(1));
        Assert.Equal(0, draw.After.Value); Assert.Equal(-1, draw.After.Tension);
        var refill = machine.Step(machine.Publication(4));
        Assert.Equal(0, refill.After.Value); Assert.Equal(0, refill.After.Tension);
        Assert.Equal(1, refill.PendingDelta);
    }

    [Fact]
    public void Replayed_or_foreign_publication_cannot_reexecute_a_signed_step()
    {
        var machine = CoupledExtentMachine.Create(2);
        var publication = machine.Publication(4);
        machine.Step(publication);
        var replay = machine.Step(publication);
        Assert.False(replay.ConsumedEvent); Assert.False(replay.Resolved);
        Assert.Equal(3, replay.After.Value); Assert.Equal(1, replay.After.Beat);
        var stranger = CoupledExtentMachine.Create(2);
        var foreign = machine.Step(stranger.Publication(1));
        Assert.False(foreign.ConsumedEvent); Assert.Equal(3, foreign.After.Value);
    }

    [Fact]
    public void A_paused_clock_leaves_the_same_publication_available_for_one_later_arrival()
    {
        var machine = CoupledExtentMachine.Create(2);
        var publication = machine.Publication(4);
        var waiting = machine.Step(publication, pulse: false);
        Assert.False(waiting.ConsumedEvent); Assert.Equal(0, waiting.After.Beat);
        Assert.Equal(1, waiting.UnroutedDelta); Assert.Equal(2, waiting.After.Value);
        var later = machine.Step(publication);
        Assert.True(later.ConsumedEvent); Assert.Equal(3, later.After.Value);
        Assert.Equal(0, later.UnroutedDelta);
    }

    [Theory]
    [InlineData("feedback")]
    [InlineData("hold")]
    [InlineData("clock")]
    public void Compiled_law_agrees_with_native_reads_and_whole_runs_for_every_finite_support(string policy)
    {
        var native = CoupledExtentMachine.Create(3, policy: policy);
        var compiled = CoupledExtentMachine.Create(3, policy: policy);
        Assert.True(native.CompactLawAgrees());
        foreach (byte mask in new byte[] { 0, 7, 4, 4, 1, 2, 3, 6, 5, 1, 1, 2 })
        {
            var a = native.Step(native.Publication(mask));
            var b = compiled.Step(compiled.Publication(mask), compiled: true);
            Assert.Equal(a.RequestedDelta, b.RequestedDelta);
            Assert.Equal((a.After.Value, a.After.Tension, a.After.Beat, a.After.Phase, a.Resolved),
                (b.After.Value, b.After.Tension, b.After.Beat, b.After.Phase, b.Resolved));
        }
    }

    [Fact]
    public void Same_initial_ink_does_not_make_hold_clock_and_feedback_the_same_machine()
    {
        var hold = CoupledExtentMachine.Create(2, policy: "hold");
        var clock = CoupledExtentMachine.Create(2, policy: "clock");
        var feedback = CoupledExtentMachine.Create(2);
        for (var i = 0; i < 4; i++)
        {
            hold.Step(hold.Publication(4)); clock.Step(clock.Publication(4)); feedback.Step(feedback.Publication(4));
        }
        Assert.Equal(2, hold.Snapshot().Value); Assert.Equal(2, clock.Snapshot().Value);
        Assert.Equal(6, feedback.Snapshot().Value);
    }

    [Fact]
    public void Native_transport_preserves_pending_tension_law_aliases_and_future_behavior()
    {
        var original = CoupledExtentMachine.Create(2, 2);
        original.Step(original.Publication(4)); original.Step(original.Publication(4));
        original.Step(original.Publication(0));
        var restored = CoupledExtentMachine.Decode(original.Encode());
        Assert.NotSame(original.Owner, restored.Owner); Assert.NotSame(original.Surface, restored.Surface);
        Assert.Equal(original.Snapshot().Value, restored.Snapshot().Value);
        Assert.Equal(2, restored.Snapshot().Tension); Assert.Equal(3, restored.History.Count);
        Assert.True(restored.CompactLawAgrees());
        foreach (byte mask in new byte[] { 1, 7, 1, 1, 2, 4 })
        {
            var a = original.Step(original.Publication(mask));
            var b = restored.Step(restored.Publication(mask), compiled: true);
            Assert.Equal((a.After.Value, a.After.Tension, a.After.Beat, a.After.Phase),
                (b.After.Value, b.After.Tension, b.After.Beat, b.After.Phase));
        }
        Assert.Equal(original.History.Count, restored.History.Count);
    }

    [Fact]
    public void Two_owners_sharing_a_unit_still_have_independent_state_and_only_coarse_read_connections()
    {
        var first = CoupledExtentMachine.Create(5);
        var second = CoupledExtentMachine.Create(1, unit: first.StepUnit.UnitShape);
        var firstBefore = first.Snapshot();
        for (var i = 0; i < 6; i++)
        {
            var reference = first.Snapshot().Value; var current = second.Snapshot().Value;
            var read = second.Publication(current < reference ? (byte)4 : current > reference ? (byte)1 : (byte)2);
            Assert.DoesNotContain(Reachable(read), item => ReferenceEquals(item, first.ExtentPort));
            second.Step(read);
        }
        Assert.Equal(5, second.Snapshot().Value); Assert.Equal(firstBefore.Value, first.Snapshot().Value);
        Assert.Equal(0, first.Snapshot().Beat);
    }

    [Fact]
    public void One_composed_archive_preserves_port_aliases_and_shared_unit_without_sharing_owner_state()
    {
        var a = CoupledExtentMachine.Create(5);
        var b = CoupledExtentMachine.Create(1, unit: a.StepUnit.UnitShape);
        var c = CoupledExtentMachine.Create(3, unit: a.StepUnit.UnitShape);
        var d = CoupledExtentMachine.Create(2, 2, unit: a.StepUnit.UnitShape);
        d.Step(d.Publication(4));
        var link = new Relation(a.ExtentPort, b.ExtentPort);
        var other = new Relation(c.ExtentPort, d.ExtentPort);
        var bytes = GojoNumericFormat.Encode(a.ArchiveSurface(), b.ArchiveSurface(), c.ArchiveSurface(), d.ArchiveSurface(), link, other);
        var roots = GojoNumericFormat.Decode(bytes);
        var restored = roots.Take(4).Cast<Relation>().Select(CoupledExtentMachine.Restore).ToArray();
        Assert.All(restored, machine => Assert.Same(restored[0].StepUnit.UnitShape, machine.StepUnit.UnitShape));
        var restoredLink = Assert.IsType<Relation>(roots[4]);
        Assert.Same(restored[0].ExtentPort, restoredLink.Left);
        Assert.Same(restored[1].ExtentPort, restoredLink.Right);
        Assert.NotSame(a.ExtentPort, restoredLink.Left);
        Assert.Equal(1, restored[3].Snapshot().Tension);
        restored[1].Step(restored[1].Publication(4));
        Assert.Equal(2, restored[1].Snapshot().Value);
        Assert.Equal(1, b.Snapshot().Value);
        Assert.Equal(5, restored[0].Snapshot().Value);
        Assert.Equal(3, restored[2].Snapshot().Value);
    }

    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var next))
        {
            if (!seen.Add(next)) continue; yield return next;
            if (next is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
