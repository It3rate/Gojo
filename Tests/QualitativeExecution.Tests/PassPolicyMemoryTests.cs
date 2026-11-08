using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class PassPolicyMemoryTests
{
    [Theory]
    [InlineData(6, 3, true, false, false)]
    [InlineData(3, 6, false, false, true)]
    [InlineData(5, 5, false, false, true)]
    [InlineData(0, 0, false, true, false)]
    [InlineData(-6, -2, true, false, false)]
    [InlineData(-2, 2, false, false, true)]
    [InlineData(-2, 0, true, true, false)]
    public void Whole_pass_magnitude_and_goal_reads_drive_the_native_preference_law(
        long before, long after, bool improved, bool achieved, bool advance)
    {
        var memory = Memory(); var choice = memory.Choose(Proposal(memory));
        var result = Complete(memory, before, after, choice.Receipt);
        Assert.True(result.Recorded); Assert.True(result.Complete);
        Assert.Equal(improved, result.Improved); Assert.Equal(achieved, result.Achieved);
        Assert.Equal(advance, result.Advanced); Assert.Equal(advance ? 1 : 0, memory.Phase);
        Assert.Null(memory.PendingChoice); Assert.Same(result.Witness, Assert.Single(memory.RecentPasses));
        Assert.Equal(0UL, memory.DiscrepancyBasis.Value); Assert.Equal(0, memory.DiscrepancyBasis.Tension);
        Assert.Contains(Reachable(result.Witness), item => ReferenceEquals(item, choice.Receipt));
    }

    [Fact]
    public void Selection_is_a_provisional_operation_and_does_not_narrow_the_original_proposal()
    {
        var memory = Memory(); var proposal = Proposal(memory); var first = memory.Choose(proposal);
        Assert.True(first.Selected); Assert.Same(memory.Options[0], first.Candidate);
        Assert.Equal(0, first.PhaseBefore); Assert.Equal(0, first.PhaseAfter); Assert.Equal(0, memory.Phase);
        Assert.True(FiniteChoice.TryReadSelected(first.Receipt!, out var command));
        QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out _, out var all, out _);
        QualitativePublication.TryRead(command, out _, out _, out _, out _, out _, out var selected, out _);
        Assert.Equal(3, FiniteLaw.Entries(all).Length); Assert.Single(FiniteLaw.Entries(selected));
        Assert.Same(first.Receipt, memory.Choose(proposal).Receipt);
    }

    [Fact]
    public void The_cyclic_scan_skips_unavailable_options_and_retains_its_membership_walk()
    {
        var memory = Memory(); var choice = memory.Choose(Proposal(memory, S(memory.Options[2])));
        Assert.True(choice.Selected); Assert.Equal(2, choice.SelectedIndex); Assert.Equal(0, memory.Phase);
        var keep = Complete(memory, 7, 6, choice.Receipt);
        Assert.False(keep.Advanced); Assert.Equal(2, memory.Phase);
        var last = memory.Choose(Proposal(memory)); Assert.Same(memory.Options[2], last.Candidate);
        var rotate = Complete(memory, 6, 7, last.Receipt);
        Assert.True(rotate.Advanced); Assert.Equal(0, memory.Phase);
    }

    [Fact]
    public void Open_empty_or_foreign_offers_do_not_become_default_choices()
    {
        var memory = Memory();
        Assert.False(memory.Choose(Proposal(memory, StructuralConstraint.Open(memory.Options[0]))).Selected);
        Assert.False(memory.Choose(Proposal(memory, S())).Selected);
        var foreign = new Relation(new Placeholder(), new Placeholder());
        Assert.False(memory.Choose(Proposal(memory, S(foreign))).Selected);
        var foreignDomain = QualitativePublication.Create(new Placeholder(), new Placeholder(), new Placeholder(),
            new Carrier(), S(memory.Options[0], foreign), S(memory.Options[0]), S());
        Assert.False(memory.Choose(foreignDomain).Selected);
        Assert.Null(memory.PendingChoice); Assert.Equal(0, memory.Phase);
    }

    [Fact]
    public void Unreadable_passes_are_retained_without_creating_a_reward_or_cursor_change()
    {
        var memory = Memory(); var choice = memory.Choose(Proposal(memory));
        var result = memory.CompletePass(memory.Context, StructuralConstraint.Open(), Sample(memory, 0), S(), choice.Receipt);
        Assert.True(result.Recorded); Assert.False(result.Complete); Assert.False(result.Achieved);
        Assert.False(result.Advanced); Assert.Equal(0, memory.Phase); Assert.Null(memory.PendingChoice);
        Assert.Single(memory.RecentPasses);
        Assert.False(Complete(memory, 9, 1).Recorded); // A pass needs a new selected route.
    }

    [Fact]
    public void Live_or_foreign_basis_discrepancies_remain_unreadable()
    {
        var memory = Memory(); memory.Choose(Proposal(memory));
        Assert.False(memory.CompletePass(memory.Context, memory.DiscrepancyBasis.ValueSlot, Sample(memory, 0), S()).Complete);
        memory.Choose(Proposal(memory));
        Assert.False(memory.CompletePass(memory.Context, new CarrierLocation(new Carrier(), 8), Sample(memory, 1), S()).Complete);
        Assert.Equal(0, memory.Phase);
    }

    [Fact]
    public void Other_context_or_other_selection_cannot_redirect_this_preference()
    {
        var memory = Memory(); var choice = memory.Choose(Proposal(memory));
        var stranger = Memory(); var strangerChoice = stranger.Choose(Proposal(stranger));
        var otherContext = memory.CompletePass(new Placeholder(), Sample(memory, 1), Sample(memory, 8), S(), choice.Receipt);
        var otherChoice = memory.CompletePass(memory.Context, Sample(memory, 1), Sample(memory, 8), S(), strangerChoice.Receipt);
        Assert.False(otherContext.Recorded); Assert.False(otherChoice.Recorded);
        Assert.Equal(0, memory.Phase); Assert.Same(choice.Receipt, memory.PendingChoice); Assert.Empty(memory.RecentPasses);
    }

    [Fact]
    public void A_captured_pass_does_not_drift_when_caller_owned_samples_change()
    {
        var memory = Memory(); var choice = memory.Choose(Proposal(memory));
        var before = Sample(memory, -8); var after = Sample(memory, -3);
        var result = memory.CompletePass(memory.Context, before, after, S(), choice.Receipt);
        before.Tick.Set(1); after.Tick.Set(9);
        var body = FiniteLaw.Entries((Relation)result.Witness.Right);
        var capture = Assert.IsType<Relation>(body[2]);
        Assert.Equal(-8, Assert.IsType<CarrierLocation>(capture.Left).Ticks);
        Assert.Equal(-3, Assert.IsType<CarrierLocation>(capture.Right).Ticks);
        Assert.True(result.Improved);
    }

    [Fact]
    public void Only_two_passes_remain_and_duplicate_exclusion_has_that_explicit_limit()
    {
        var memory = Memory(); var events = new[] { new Placeholder(), new Placeholder(), new Placeholder() };
        var recorded = new List<Relation>();
        foreach (var occurrence in events)
        {
            memory.Choose(Proposal(memory));
            recorded.Add(memory.CompletePass(memory.Context, Sample(memory, 5), Sample(memory, 5), S(), eventIdentity: occurrence).Witness);
        }
        Assert.Equal(2, memory.RecentPasses.Length); Assert.Equal(1, memory.DiscardedCount);
        Assert.DoesNotContain(Reachable(memory.ArchiveSurface()), item => ReferenceEquals(item, recorded[0]));
        Assert.Equal(1, Assert.IsType<CarrierLocation>(memory.LossSurface.Left).Ticks);
        var pending = memory.Choose(Proposal(memory));
        var replay = memory.CompletePass(memory.Context, Sample(memory, 5), Sample(memory, 5), S(), eventIdentity: events[2]);
        Assert.False(replay.Recorded); Assert.Same(pending.Receipt, memory.PendingChoice);
        var forgottenReplay = memory.CompletePass(memory.Context, Sample(memory, 5), Sample(memory, 5), S(), eventIdentity: events[0]);
        Assert.True(forgottenReplay.Recorded); Assert.Equal(2, memory.DiscardedCount);
    }

    [Fact]
    public void Native_transport_preserves_pending_choice_ring_position_and_small_history()
    {
        var memory = Memory(); memory.Choose(Proposal(memory)); Complete(memory, 4, 5);
        var choice = memory.Choose(Proposal(memory)); Assert.Equal(1, choice.SelectedIndex);
        var restored = PassPolicyMemory.Decode(memory.Encode());
        Assert.Equal(1, restored.Phase); Assert.Single(restored.RecentPasses); Assert.NotNull(restored.PendingChoice);
        Assert.NotSame(memory.Context, restored.Context); Assert.NotSame(memory.Options[1], restored.Options[1]);
        var next = Complete(restored, 5, 7);
        Assert.True(next.Advanced); Assert.Equal(2, restored.Phase); Assert.Equal(2, restored.RecentPasses.Length);
        Assert.Equal(1, memory.Phase); Assert.NotNull(memory.PendingChoice);
    }

    [Fact]
    public void A_forged_sampling_walk_cannot_skip_an_available_earlier_option()
    {
        var memory = Memory(); var original = memory.Choose(Proposal(memory));
        FiniteChoice.TryReadSelection(original.Receipt!, out var proposal, out _, out _, out var evidence, out _, out _, out _, out _);
        var cause = Assert.IsType<Relation>(evidence);
        var captured = FiniteLaw.Entries(Assert.IsType<Relation>(cause.Right))[0];
        QualitativePublication.TryRead(proposal, out _, out _, out _, out _, out _, out var support, out _);
        // Each proof is a real membership read, but the first option was
        // available. The cyclic first-available policy cannot justify skipping it.
        var fakeWalk = S(StructuralConstraint.Match(memory.Options[0], S(), support),
            StructuralConstraint.Match(memory.Options[1], S(), support));
        var fakeCause = new Relation(memory.Surface, S(captured, fakeWalk));
        var fakeReceipt = FiniteChoice.Select(proposal, memory.Identity, memory.Options[1], fakeCause);
        Assert.True(FiniteChoice.TryReadSelected(fakeReceipt, out _)); // Legal action, wrong policy history.
        var archive = S(memory.Surface, S(fakeReceipt), S());
        Assert.Throws<ArgumentException>(() => PassPolicyMemory.Restore(archive));
        Assert.Same(original.Receipt, memory.PendingChoice); Assert.Equal(0, memory.Phase);
    }

    [Fact]
    public void The_native_and_compiled_pass_laws_agree_through_keeps_wraps_and_missing_reads()
    {
        var native = Memory(); var compiled = Memory();
        foreach (var (before, after) in new (long?, long?)[] { (8, 5), (5, 5), (5, 7), (7, 8), (8, 0), (null, 0), (0, 0) })
        {
            native.Choose(Proposal(native)); compiled.Choose(Proposal(compiled));
            var a = native.CompletePass(native.Context, before.HasValue ? Sample(native, before.Value) : StructuralConstraint.Open(),
                after.HasValue ? Sample(native, after.Value) : StructuralConstraint.Open(), S(), compiled: false);
            var b = compiled.CompletePass(compiled.Context, before.HasValue ? Sample(compiled, before.Value) : StructuralConstraint.Open(),
                after.HasValue ? Sample(compiled, after.Value) : StructuralConstraint.Open(), S(), compiled: true);
            Assert.Equal((a.Recorded, a.Complete, a.Improved, a.Achieved, a.Advanced, a.PhaseAfter),
                (b.Recorded, b.Complete, b.Improved, b.Achieved, b.Advanced, b.PhaseAfter));
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void The_fixed_comparison_uses_native_keep_rows_even_when_a_pass_stalls_or_worsens(bool compiled)
    {
        var memory = PassPolicyMemory.CreateFixed(new Placeholder(), new Carrier(),
            new Relation(new Placeholder(), new Placeholder()), new Relation(new Placeholder(), new Placeholder()));
        foreach (var end in new long[] { 5, 9 })
        {
            memory.Choose(Proposal(memory));
            var result = memory.CompletePass(memory.Context, Sample(memory, 5), Sample(memory, end), S(), compiled: compiled);
            Assert.True(result.Complete); Assert.False(result.Advanced); Assert.False(result.Improved);
            Assert.Equal(0, memory.Phase);
        }
        var restored = PassPolicyMemory.Decode(memory.Encode());
        restored.Choose(Proposal(restored));
        Assert.False(restored.CompletePass(restored.Context, Sample(restored, 1), Sample(restored, 9), S(), compiled: compiled).Advanced);
    }

    private static PassPolicyMemory Memory() => PassPolicyMemory.Create(new Placeholder(), new Carrier(),
        new Relation(new Placeholder(), new Placeholder()), new Relation(new Placeholder(), new Placeholder()), new Relation(new Placeholder(), new Placeholder()));
    private static CarrierLocation Sample(PassPolicyMemory memory, long value) => new(memory.DiscrepancyBasis, value);
    private static Relation Proposal(PassPolicyMemory memory, Relation? support = null) => QualitativePublication.Create(
        new Placeholder(), new Placeholder(), new Placeholder(), new Carrier(), memory.OptionDomain, support ?? S(memory.Options.Cast<IElement>().ToArray()), S());
    private static PassPolicyMemory.PassRead Complete(PassPolicyMemory memory, long before, long after, Relation? choice = null) =>
        memory.CompletePass(memory.Context, Sample(memory, before), Sample(memory, after), S(), choice);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var todo = new Stack<IElement>(); todo.Push(root);
        while (todo.TryPop(out var item))
        {
            if (!seen.Add(item)) continue; yield return item;
            if (item is Relation relation) { todo.Push(relation.Left); todo.Push(relation.Right); }
        }
    }
}
