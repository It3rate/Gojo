using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class PreferenceTransferTests
{
    [Fact]
    public void Default_advanced_untried_pending_and_unresolved_policies_do_not_export_learned_preferences()
    {
        var source = Memory();
        Assert.False(source.ExportPreference().Available);
        source.Choose(Proposal(source));
        Assert.False(source.ExportPreference().Available);
        Complete(source, 2, 5);
        Assert.Equal(1, source.Phase);
        Assert.False(source.ExportPreference().Available);
        source.Choose(Proposal(source));
        source.CompletePass(source.Context, StructuralConstraint.Open(), Sample(source, 0), S());
        Assert.False(source.ExportPreference().Available);
        source.Choose(Proposal(source)); Complete(source, 5, 1);
        Assert.True(source.ExportPreference().Available);
        source.Choose(Proposal(source));
        Assert.False(source.ExportPreference().Available);
    }

    [Theory]
    [InlineData(5, 2)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    public void Improvement_or_achieved_goal_supports_a_local_preference(long before, long after)
    {
        var source = Memory(); source.Choose(Proposal(source)); Complete(source, before, after);
        var exported = source.ExportPreference();
        Assert.True(exported.Available); Assert.Same(source.Options[0], exported.Option);
        Assert.Same(source.RecentPasses[0], exported.Evidence);
        var fields = FiniteLaw.Entries(exported.Surface);
        Assert.Same(source.Context, fields[1]); Assert.Same(source.OptionDomain, fields[2]);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(fields[3]).Ticks);
    }

    [Fact]
    public void Option_correspondence_survives_reordering_without_copying_a_cursor_or_target_evidence()
    {
        var source = Trained(); var exported = source.ExportPreference(); var target = Memory();
        var sourceBefore = source.RecentPasses;
        var mapping = target.PreferenceMapping(exported.Surface,
            S(new Relation(source.Options[0], target.Options[1]), new Relation(exported.Option!, target.Options[0])), S());
        var imported = target.ImportPreference(exported.Surface, mapping);
        Assert.True(imported.Accepted); Assert.Equal(1, source.Phase); Assert.Equal(0, target.Phase);
        Assert.Same(target.Options[0], imported.TargetOption);
        Assert.Empty(target.RecentPasses); Assert.Null(target.PendingChoice); Assert.Equal(0, target.DiscardedCount);
        Assert.Equal(0UL, target.DiscrepancyBasis.Value); Assert.Equal(0, target.DiscrepancyBasis.Tension);
        Assert.Equal(sourceBefore, source.RecentPasses); Assert.Equal(0, source.DiscardedCount); Assert.Null(source.PendingChoice);
        Assert.False(target.ExportPreference().Available); // Imported provenance is not local training.
        Assert.Same(target.Options[0], target.Choose(Proposal(target)).Candidate);
        Assert.Contains(Reachable(target.ImportedPreference!), item => ReferenceEquals(item, exported.Evidence));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void A_bad_analogy_is_a_revisable_prior_and_full_pass_feedback_can_replace_it(bool compiled)
    {
        var source = Trained(); var exported = source.ExportPreference(); var target = Memory();
        var wrong = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[0])), S());
        Assert.True(target.ImportPreference(exported.Surface, wrong).Accepted);
        target.Choose(Proposal(target));
        var badPass = Complete(target, 2, 8, compiled);
        Assert.True(badPass.Advanced); Assert.Equal(1, target.Phase);
        Assert.False(target.ExportPreference().Available);
        target.Choose(Proposal(target));
        var recovery = Complete(target, 8, 0, compiled);
        Assert.True(recovery.Achieved); Assert.Equal(1, target.Phase);
        Assert.Equal(2, target.RecentPasses.Length); Assert.NotNull(target.ImportedPreference);
        Assert.True(target.ExportPreference().Available); Assert.Equal(1, source.Phase);
    }

    [Fact]
    public void Missing_ambiguous_open_or_foreign_correspondence_leaves_target_untouched()
    {
        var source = Trained(); var exported = source.ExportPreference(); var target = Memory();
        var rows = new[]
        {
            S(),
            S(new Relation(source.Options[0], target.Options[1])),
            S(new Relation(exported.Option!, target.Options[0]), new Relation(exported.Option!, target.Options[1])),
            StructuralConstraint.Open(new Relation(exported.Option!, target.Options[1])),
            S(new Relation(new Relation(new Placeholder(), new Placeholder()), target.Options[1])),
            S(new Relation(exported.Option!, new Relation(new Placeholder(), new Placeholder())))
        };
        foreach (var row in rows)
        {
            Assert.False(target.ImportPreference(exported.Surface, target.PreferenceMapping(exported.Surface, row, S())).Accepted);
            Assert.Equal(0, target.Phase); Assert.Null(target.ImportedPreference); Assert.Empty(target.RecentPasses);
        }
    }

    [Fact]
    public void Mapping_binds_the_exact_export_and_exact_target_context()
    {
        var source = Trained(); var exported = source.ExportPreference(); var anotherExport = source.ExportPreference();
        var target = Memory(); var other = Memory();
        var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[1])), S());
        Assert.False(target.ImportPreference(anotherExport.Surface, mapping).Accepted);
        Assert.False(other.ImportPreference(exported.Surface, mapping).Accepted);
        var body = FiniteLaw.Entries((Relation)mapping.Right); body[0] = new Placeholder();
        Assert.False(target.ImportPreference(exported.Surface, new Relation(mapping.Left, S(body))).Accepted);
        Assert.Null(target.ImportedPreference);
    }

    [Fact]
    public void Import_is_once_only_even_when_the_imported_position_is_zero()
    {
        var exported = Trained().ExportPreference(); var target = Memory();
        var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[0])), S());
        Assert.True(target.ImportPreference(exported.Surface, mapping).Accepted);
        Assert.False(target.ImportPreference(exported.Surface, mapping).Accepted);
        Assert.Equal(0, target.Phase); Assert.Empty(target.RecentPasses);
    }

    [Fact]
    public void An_imported_preference_does_not_force_an_unavailable_target_option()
    {
        var exported = Trained().ExportPreference(); var target = Memory();
        var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[1])), S());
        Assert.True(target.ImportPreference(exported.Surface, mapping).Accepted);
        var available = QualitativePublication.Create(new Placeholder(), new Placeholder(), new Placeholder(),
            new Carrier(), target.OptionDomain, S(target.Options[0]), S());
        var choice = target.Choose(available);
        Assert.True(choice.Selected); Assert.Same(target.Options[0], choice.Candidate);
        Assert.Equal(1, target.Phase); // Reading availability did not rewrite the prior.
        Complete(target, 4, 0);
        Assert.Equal(0, target.Phase); // Local utility now supports the available alternative.
    }

    [Fact]
    public void Pending_or_previously_evaluated_target_cannot_be_reset_by_an_import()
    {
        var exported = Trained().ExportPreference(); var target = Memory();
        var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[1])), S());
        target.Choose(Proposal(target));
        Assert.False(target.ImportPreference(exported.Surface, mapping).Accepted);
        Complete(target, 5, 4);
        Assert.False(target.ImportPreference(exported.Surface, mapping).Accepted);
        Assert.Equal(0, target.Phase); Assert.Single(target.RecentPasses); Assert.Null(target.ImportedPreference);
    }

    [Fact]
    public void Historical_export_keeps_its_source_context_even_if_source_later_revises_its_preference()
    {
        var source = Trained(); var exported = source.ExportPreference();
        source.Choose(Proposal(source)); Complete(source, 1, 5);
        Assert.Equal(0, source.Phase); Assert.Equal(2, source.PhaseTension);
        var target = Memory(); var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[1])), S());
        Assert.True(target.ImportPreference(exported.Surface, mapping).Accepted);
        Assert.Equal(1, target.Phase); Assert.Equal(0, source.Phase);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_archive_retains_import_and_continues_local_learning_without_reimport(bool compiled)
    {
        var exported = Trained().ExportPreference(); var target = Memory();
        var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[1])), S());
        Assert.True(target.ImportPreference(exported.Surface, mapping).Accepted);
        var restored = PassPolicyMemory.Decode(target.Encode());
        Assert.Equal(1, restored.Phase); Assert.Empty(restored.RecentPasses); Assert.NotNull(restored.ImportedPreference);
        var origin = (Relation)restored.ImportedPreference!.Left;
        Assert.False(restored.ImportPreference((Relation)origin.Left, (Relation)origin.Right).Accepted);
        restored.Choose(Proposal(restored)); Complete(restored, 1, 9, compiled);
        Assert.Equal(0, restored.Phase); Assert.NotNull(restored.ImportedPreference);
        var twice = PassPolicyMemory.Decode(restored.Encode());
        Assert.Equal(0, twice.Phase); Assert.Equal(2, twice.PhaseTension);
        Assert.Single(twice.RecentPasses); Assert.NotNull(twice.ImportedPreference);
    }

    [Fact]
    public void A_forged_useful_outcome_cannot_replace_the_actual_retained_discrepancies()
    {
        var source = Trained(); var exported = source.ExportPreference();
        var fields = FiniteLaw.Entries(exported.Surface);
        var witness = (Relation)fields[5]; var payload = FiniteLaw.Entries((Relation)witness.Right);
        payload[2] = new Relation(Sample(source, 1), Sample(source, 9));
        fields[5] = new Relation(witness.Left, S(payload));
        var forged = S(fields); var target = Memory();
        var mapping = target.PreferenceMapping(forged, S(new Relation(exported.Option!, target.Options[1])), S());
        Assert.False(target.ImportPreference(forged, mapping).Accepted);
        Assert.Null(target.ImportedPreference);
    }

    [Fact]
    public void Restore_rejects_an_import_witness_with_a_substituted_context_or_settlement()
    {
        var exported = Trained().ExportPreference(); var target = Memory();
        var mapping = target.PreferenceMapping(exported.Surface, S(new Relation(exported.Option!, target.Options[1])), S());
        Assert.True(target.ImportPreference(exported.Surface, mapping).Accepted);
        var archive = FiniteLaw.Entries(target.ArchiveSurface());
        var witness = target.ImportedPreference!;
        var body = FiniteLaw.Entries((Relation)witness.Right); body[1] = new Placeholder();
        archive[3] = S(new Relation(witness.Left, S(body)));
        Assert.Throws<ArgumentException>(() => PassPolicyMemory.Restore(S(archive)));
        body = FiniteLaw.Entries((Relation)witness.Right);
        var transition = FiniteLaw.Entries((Relation)body[4]);
        var settled = (CarrierLocation)transition[3];
        transition[3] = new CarrierLocation(settled.Carrier, 0); body[4] = S(transition);
        archive[3] = S(new Relation(witness.Left, S(body)));
        Assert.Throws<ArgumentException>(() => PassPolicyMemory.Restore(S(archive)));
    }

    private static PassPolicyMemory Memory() => PassPolicyMemory.Create(new Placeholder(), new Carrier(),
        new Relation(new Placeholder(), new Placeholder()), new Relation(new Placeholder(), new Placeholder()));
    private static PassPolicyMemory Trained()
    {
        var source = Memory(); source.Choose(Proposal(source)); Complete(source, 2, 5);
        source.Choose(Proposal(source)); Complete(source, 5, 0); return source;
    }
    private static CarrierLocation Sample(PassPolicyMemory memory, long value) => new(memory.DiscrepancyBasis, value);
    private static Relation Proposal(PassPolicyMemory memory) => QualitativePublication.Create(
        new Placeholder(), new Placeholder(), new Placeholder(), new Carrier(), memory.OptionDomain, memory.OptionDomain, S());
    private static PassPolicyMemory.PassRead Complete(PassPolicyMemory memory, long before, long after, bool compiled = true) =>
        memory.CompletePass(memory.Context, Sample(memory, before), Sample(memory, after), S(), compiled: compiled);
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
