using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class ExtentResolutionRungTests
{
    [Theory]
    [InlineData(1, 4)] [InlineData(4, 1)]
    public void Opposed_responses_after_an_accepted_step_permit_trying_a_finer_rung(byte earlier, byte current)
    {
        var coarse = CoupledExtentMachine.Create(3); var rung = ExtentResolutionRung.Create(coarse);
        var a = coarse.Publication(earlier); var step = coarse.Step(a); var b = coarse.Publication(current);
        var native = rung.Reversal.Evaluate(a, b, step.Surface, false);
        var compiled = rung.Reversal.Evaluate(a, b, step.Surface);
        Assert.True(native.Suggested); Assert.True(compiled.Suggested); Assert.True(native.Resolved);
        Assert.Equal(0, rung.Fine.Snapshot().Value); Assert.False(rung.Active);
        var before = coarse.Snapshot(); var result = rung.Activate(native.Witness);
        Assert.True(result.Activated); Assert.True(rung.Active);
        Assert.Equal(before.Value * 3, rung.Fine.Snapshot().Value);
        Assert.Equal(before.Value, coarse.Snapshot().Value); Assert.Equal(before.Tension, coarse.Snapshot().Tension);
        Assert.True(rung.Fine.StepUnit.UnitShape.Equals(UnitShape.SubdivisionOf(coarse.StepUnit.UnitShape, 3)));
        Assert.NotSame(coarse.ExtentPort.Right, rung.Fine.ExtentPort.Right);
    }

    [Theory]
    [InlineData(0)] [InlineData(3)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public void Missing_or_ambiguous_comparisons_cannot_choose_refinement(byte support)
    {
        var coarse = CoupledExtentMachine.Create(2); var rung = ExtentResolutionRung.Create(coarse);
        var first = coarse.Publication(4); var step = coarse.Step(first);
        var read = rung.Reversal.Evaluate(first, coarse.Publication(support), step.Surface);
        Assert.False(read.Resolved); Assert.False(read.Suggested);
        Assert.False(rung.Activate(read.Witness).Activated); Assert.Equal(0, rung.Fine.Snapshot().Value);
    }

    [Theory]
    [InlineData(2)] [InlineData(4)]
    public void Equal_or_unchanged_responses_resolve_without_requesting_refinement(byte support)
    {
        var coarse = CoupledExtentMachine.Create(2); var rung = ExtentResolutionRung.Create(coarse);
        var first = coarse.Publication(4); var step = coarse.Step(first);
        var read = rung.Reversal.Evaluate(first, coarse.Publication(support), step.Surface);
        Assert.True(read.Resolved); Assert.False(read.Suggested);
        Assert.False(rung.Activate(read.Witness).Activated);
    }

    [Fact]
    public void A_finer_read_preserves_coarse_value_and_exact_remainder()
    {
        var coarse = CoupledExtentMachine.Create(1); var rung = ExtentResolutionRung.Create(coarse);
        var read = Permit(rung); Assert.True(rung.Activate(read.Witness).Activated);
        rung.Fine.Step(rung.Fine.Publication(4));
        Assert.Equal(7, rung.Fine.Snapshot().Value); Assert.Equal(2, coarse.Snapshot().Value);
        var projection = rung.ReadCoarse(); Assert.True(projection.TryReadProjection(out var whole, out var residue));
        Assert.Equal(2, whole.Ticks);
        Assert.True(ProjectionResidue.TryRead(residue, out var basis, out var numerator, out var denominator));
        Assert.Same(rung.Fine.ExtentPort.Right, basis); Assert.Equal(1, numerator); Assert.Equal(1, denominator);
        Assert.Equal(0, coarse.Snapshot().Tension); Assert.Equal(0, rung.Fine.Snapshot().Tension);
    }

    [Fact]
    public void Capacity_refuses_an_incomplete_copy_and_retains_the_projected_demand()
    {
        var coarse = CoupledExtentMachine.Create(1); var rung = ExtentResolutionRung.Create(coarse, fineCapacity: 5);
        var read = Permit(rung); var activation = rung.Activate(read.Witness);
        Assert.False(activation.Activated); Assert.True(activation.Blocked); Assert.Equal(6, activation.ProjectedFine);
        Assert.Equal(0, rung.Fine.Snapshot().Value); Assert.Equal(0, rung.Fine.Snapshot().Tension);
        Assert.Equal(2, coarse.Snapshot().Value); Assert.False(rung.Active);
    }

    [Fact]
    public void Stale_debt_and_nonempty_target_do_not_get_erased_by_activation()
    {
        var coarse = CoupledExtentMachine.Create(1, 2); var rung = ExtentResolutionRung.Create(coarse);
        var read = Permit(rung);
        ((Carrier)coarse.ExtentPort.Right).Add(1);
        Assert.False(rung.Activate(read.Witness).Activated);
        Assert.Equal(1, coarse.Snapshot().Tension); Assert.Equal(0, rung.Fine.Snapshot().Value);
        var other = ExtentResolutionRung.Create(CoupledExtentMachine.Create(1)); var valid = Permit(other);
        other.Fine.Step(other.Fine.Publication(4));
        Assert.False(other.Activate(valid.Witness).Activated); Assert.Equal(1, other.Fine.Snapshot().Value);
    }

    [Fact]
    public void Replayed_or_superseded_evidence_cannot_reset_a_fine_machine()
    {
        var rung = ExtentResolutionRung.Create(CoupledExtentMachine.Create(1)); var read = Permit(rung);
        Assert.True(rung.Activate(read.Witness).Activated);
        rung.Fine.Step(rung.Fine.Publication(4));
        var again = rung.Activate(read.Witness);
        Assert.False(again.Activated); Assert.True(again.AlreadyActive); Assert.Equal(7, rung.Fine.Snapshot().Value);
        var stale = ExtentResolutionRung.Create(CoupledExtentMachine.Create(1)); var previous = Permit(stale);
        stale.Coarse.Step(stale.Coarse.Publication(2));
        Assert.False(stale.Activate(previous.Witness).Activated);
    }

    [Fact]
    public void No_accepted_step_no_refinement_even_if_two_signs_differ()
    {
        var full = CoupledExtentMachine.Create(2, 2); var blocked = ExtentResolutionRung.Create(full);
        var first = full.Publication(4); var step = full.Step(first);
        Assert.False(blocked.Reversal.Evaluate(first, full.Publication(1), step.Surface).Suggested);
        var idle = CoupledExtentMachine.Create(2); var noPulse = ExtentResolutionRung.Create(idle);
        var prior = idle.Publication(4); var inactive = idle.Step(prior, pulse: false);
        Assert.False(noPulse.Reversal.Evaluate(prior, idle.Publication(1), inactive.Surface).Suggested);
        var foreign = CoupledExtentMachine.Create(2); var foreignPacket = foreign.Publication(4); var foreignStep = foreign.Step(foreignPacket);
        Assert.False(noPulse.Reversal.Evaluate(foreignPacket, foreign.Publication(1), foreignStep.Surface).Suggested);
    }

    [Fact]
    public void Duplicate_event_and_external_extent_change_leave_the_premise_open()
    {
        var coarse = CoupledExtentMachine.Create(2); var rung = ExtentResolutionRung.Create(coarse);
        var occurrence = new Placeholder(); var first = coarse.Publication(4, occurrence); var step = coarse.Step(first);
        Assert.False(rung.Reversal.Evaluate(first, coarse.Publication(1, occurrence), step.Surface).Suggested);
        ((Carrier)coarse.ExtentPort.Right).Add(1);
        Assert.False(rung.Reversal.Evaluate(first, coarse.Publication(1), step.Surface).Suggested);
    }

    [Fact]
    public void An_old_event_or_rewritten_earlier_support_does_not_establish_a_new_reversal()
    {
        var coarse = CoupledExtentMachine.Create(2); var rung = ExtentResolutionRung.Create(coarse);
        var oldEvent = new Placeholder(); coarse.Step(coarse.Publication(2, oldEvent));
        var earlierEvent = new Placeholder(); var first = coarse.Publication(4, earlierEvent); var step = coarse.Step(first);
        Assert.False(rung.Reversal.Evaluate(first, coarse.Publication(1, oldEvent), step.Surface).Suggested);
        var rewritten = coarse.Publication(1, earlierEvent);
        Assert.False(rung.Reversal.Evaluate(rewritten, coarse.Publication(4), step.Surface).Suggested);
    }

    [Fact]
    public void A_clock_move_opposed_to_the_comparison_is_not_evidence_of_overshooting_that_comparison()
    {
        var coarse = CoupledExtentMachine.Create(2, policy: "clock"); var rung = ExtentResolutionRung.Create(coarse);
        var first = coarse.Publication(1); var step = coarse.Step(first);
        Assert.Equal(1, step.AcceptedDelta);
        Assert.False(rung.Reversal.Evaluate(first, coarse.Publication(4), step.Surface).Suggested);
    }

    [Fact]
    public void Saved_candidate_activates_after_restore_and_saved_active_rung_preserves_fine_residue()
    {
        var rung = ExtentResolutionRung.Create(CoupledExtentMachine.Create(1)); var read = Permit(rung);
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(rung.ArchiveSurface(), read.Witness));
        var copy = ExtentResolutionRung.Restore((Relation)roots[0]);
        Assert.True(copy.Activate((Relation)roots[1]).Activated);
        copy.Fine.Step(copy.Fine.Publication(4));
        Assert.False(rung.Active); Assert.Equal(0, rung.Fine.Snapshot().Value);
        var again = ExtentResolutionRung.Restore((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(copy.ArchiveSurface()))[0]);
        Assert.True(again.Active); Assert.Equal(7, again.Fine.Snapshot().Value); Assert.Equal(2, again.Coarse.Snapshot().Value);
        Assert.True(again.ReadCoarse().TryReadProjection(out var whole, out var residue));
        Assert.Equal(2, whole.Ticks); Assert.True(ProjectionResidue.TryRead(residue, out _, out var n, out _)); Assert.Equal(1, n);
    }

    private static ResponseReversalRead.Read Permit(ExtentResolutionRung rung)
    {
        var a = rung.Coarse.Publication(4); var step = rung.Coarse.Step(a); var b = rung.Coarse.Publication(1);
        var read = rung.Reversal.Evaluate(a, b, step.Surface); Assert.True(read.Suggested); return read;
    }
}
