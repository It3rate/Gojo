using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentDefaultsPolicyTests
{
    [Fact]
    public void The_same_negative_assessment_excludes_only_when_it_is_a_required_claim()
    {
        var rule = Rule(); var fact = Fact(rule, "No"); var observation = Observation(fact);
        var required = new D.Receipt(new(required: [rule]), observation);
        var strong = new D.Receipt(new(strongDefaults: [rule]), observation);
        var preference = new D.Receipt(new(preferences: [rule]), observation);
        var unused = new D.Receipt(new(unused: [rule]), observation);
        Assert.Equal("No", required.Answer); Assert.False(required.Eligible);
        Assert.Equal("Yes", strong.Answer); Assert.True(strong.Eligible); Assert.Equal((1, 1), strong.StrongRange);
        Assert.Equal("Yes", preference.Answer); Assert.Equal((1, 1), preference.PreferenceRange);
        Assert.Equal("Yes", unused.Answer); Assert.Equal((0, 0), unused.StrongRange);
        Assert.Equal("Unused", Assert.Single(unused.Reads).Answer);
        Assert.Contains(fact.Surface, R.Participants(unused.Surface));
        var sharedPolicy = new D.Policy(required: [rule]);
        var rejected = new D.Receipt(sharedPolicy, observation);
        var accepted = new D.Receipt(sharedPolicy, Observation(Fact(rule, "Yes")));
        Assert.Null(D.Receipt.ComparePreference(rejected, accepted));
    }

    [Fact]
    public void Explicit_exceptions_waive_only_the_selected_default_without_changing_evidence()
    {
        var required = Rule(); var strong = Rule(); var preference = Rule();
        var policy = new D.Policy([required], [strong], [preference]);
        var observation = Observation(Fact(required, "Yes"), Fact(strong, "No"), Fact(preference, "No"));
        var before = new D.Receipt(policy, observation); var directive = new D.Directive(policy, [strong]);
        var after = new D.Receipt(policy, observation, directive);
        Assert.Equal((1, 1), before.StrongRange); Assert.Equal((0, 0), after.StrongRange);
        Assert.Equal((1, 1), after.PreferenceRange); Assert.Equal("Waived", after.Reads[1].Answer);
        Assert.Equal("No", after.Reads[1].Fact!.Answer);
        Assert.Equal("Blocked", new D.Receipt(policy, observation, new(policy, [required])).Answer);
        var lookalike = new D.Rule("lookalike", strong.Predicate);
        Assert.Equal("Blocked", new D.Receipt(policy, observation, new(policy, [lookalike])).Answer);
        Assert.Equal("Blocked", new D.Receipt(policy, observation, new(policy, [strong, strong])).Answer);
    }

    [Fact]
    public void Negative_claims_are_native_polarity_and_do_not_invert_the_source_fact()
    {
        var positive = Rule(); var negative = new D.Rule("absence expected", positive.Predicate, false);
        var fact = Fact(negative, "No"); var receipt = new D.Receipt(new(required: [negative]), Observation(fact));
        Assert.Equal("Yes", receipt.Answer); Assert.Equal("Yes", Assert.Single(receipt.Reads).Answer);
        Assert.Equal("No", receipt.Reads[0].Fact!.Answer);
        Assert.Contains(fact.Surface, R.Participants(receipt.Supported));
        Assert.False(new D.Rule(negative.Surface).Expected);
    }

    [Fact]
    public void Missing_evidence_remains_possible_and_cannot_outrank_a_checked_clean_default()
    {
        var hard = Rule(); var weak = Rule(); var policy = new D.Policy([hard], [weak]);
        var unknownHard = new D.Receipt(policy, Observation(Fact(weak, "Yes")));
        Assert.Equal("Open", unknownHard.Answer); Assert.True(unknownHard.Eligible);
        var unknownDefault = new D.Receipt(policy, Observation(Fact(hard, "Yes")));
        var checkedClean = new D.Receipt(policy, Observation(Fact(hard, "Yes"), Fact(weak, "Yes")));
        Assert.Equal("Yes", unknownDefault.Answer); Assert.Equal((0, 1), unknownDefault.StrongRange);
        Assert.Null(D.Receipt.ComparePreference(unknownDefault, checkedClean));
        Assert.Null(D.Receipt.ComparePreference(checkedClean, unknownDefault));
        Assert.Equal(0, D.Receipt.ComparePreference(checkedClean, checkedClean));
    }

    [Fact]
    public void Opposed_assessments_keep_both_proofs_and_leave_the_claim_open()
    {
        var rule = Rule(); var yes = Fact(rule, "Yes"); var no = Fact(rule, "No");
        var receipt = new D.Receipt(new(required: [rule]), Observation(yes, no));
        Assert.Equal("Open", receipt.Answer); Assert.Empty(FiniteLaw.Entries(receipt.Blocked));
        var read = Assert.Single(receipt.Reads); Assert.True(read.Fact!.Conflicted);
        Assert.NotEmpty(FiniteLaw.Entries(read.Fact.Supported)); Assert.NotEmpty(FiniteLaw.Entries(read.Fact.Refuted));
        Assert.Contains(((Relation)FiniteLaw.Entries(yes.Supported)[0]), R.Participants(read.Surface));
        Assert.Contains(((Relation)FiniteLaw.Entries(no.Refuted)[0]), R.Participants(read.Surface));
    }

    [Fact]
    public void Defaults_have_ordered_strength_and_partial_uncertainty_ranges()
    {
        var strong = Enumerable.Range(0, 3).Select(_ => Rule()).ToArray();
        var preferences = Enumerable.Range(0, 5).Select(_ => Rule()).ToArray();
        var policy = new D.Policy(strongDefaults: strong, preferences: preferences);
        var allStrongGood = new D.Receipt(policy, Observation(strong.Select(rule => Fact(rule, "Yes"))
            .Concat(preferences.Select(rule => Fact(rule, "No"))).ToArray()));
        var oneStrongBad = new D.Receipt(policy, Observation(strong.Select((rule, index) => Fact(rule, index == 0 ? "No" : "Yes"))
            .Concat(preferences.Select(rule => Fact(rule, "Yes"))).ToArray()));
        Assert.Equal(-1, D.Receipt.ComparePreference(allStrongGood, oneStrongBad));
        var oneUnknown = new D.Receipt(policy, Observation(strong.Take(2).Select(rule => Fact(rule, "Yes")).ToArray()));
        var threeViolations = new D.Receipt(policy, Observation(strong.Select(rule => Fact(rule, "No")).ToArray()));
        Assert.Equal((0, 1), oneUnknown.StrongRange);
        Assert.Equal(-1, D.Receipt.ComparePreference(oneUnknown, threeViolations));
    }

    [Fact]
    public void Unused_and_waived_predicates_do_not_turn_an_unavailable_service_into_a_rejection()
    {
        var unused = Rule(); var weak = Rule(); var policy = new D.Policy(strongDefaults: [weak], unused: [unused]);
        var receipt = new D.Receipt(policy, Observation(Fact(unused, "Blocked"), Fact(weak, "Blocked")), new(policy, [weak]));
        Assert.Equal("Yes", receipt.Answer); Assert.Equal((0, 0), receipt.StrongRange);
        Assert.Equal("Waived", receipt.Reads[0].Answer); Assert.Equal("Unused", receipt.Reads[1].Answer);
        var active = new D.Receipt(policy, Observation(Fact(unused, "Blocked"), Fact(weak, "Blocked")));
        Assert.Equal("Yes", active.Answer); Assert.Equal((0, 1), active.StrongRange);
    }

    [Fact]
    public void Lookalike_predicates_do_not_supply_an_unrequested_actual_service()
    {
        var rule = Rule(); var sameShape = Rule();
        var receipt = new D.Receipt(new(required: [rule]), Observation(Fact(sameShape, "Yes")));
        Assert.Equal("Open", receipt.Answer); Assert.Null(Assert.Single(receipt.Reads).Fact);
        var duplicateAuthority = new D.Policy(required: [rule], strongDefaults: [new D.Rule("other label", rule.Predicate, false)]);
        Assert.False(duplicateAuthority.IsValid);
    }

    [Fact]
    public void An_uncommitted_polarity_is_open_and_does_not_become_an_accidental_negative_claim()
    {
        var predicate = new Relation(new Placeholder(), new Placeholder());
        var partial = new D.Rule(new Relation(predicate,
            new Relation(StructuralConstraint.Open(), StructuralConstraint.Closed())));
        var receipt = new D.Receipt(new(required: [partial]), Observation(D.Evidence.FromAnswer(predicate, "Yes",
            new Relation(predicate, new Placeholder()))));
        Assert.Empty(FiniteLaw.Entries(partial.Blocked)); Assert.NotEmpty(FiniteLaw.Entries(partial.Open));
        Assert.Equal("Open", receipt.Answer); Assert.Equal("Open", Assert.Single(receipt.Reads).Answer);
    }

    [Fact]
    public void Policy_observation_and_override_survive_independent_transport_without_a_side_registry()
    {
        var hard = Rule(); var weak = Rule(); var policy = new D.Policy([hard], [weak]);
        var observation = Observation(Fact(hard, "Yes"), Fact(weak, "No")); var directive = new D.Directive(policy, [weak]);
        var root = Restore(new(policy.Surface, new Relation(observation.Surface, directive.Surface)));
        var importedPolicy = new D.Policy((Relation)root.Left);
        var importedObservation = new D.Observation((Relation)((Relation)root.Right).Left);
        var importedDirective = new D.Directive((Relation)((Relation)root.Right).Right);
        var receipt = new D.Receipt(importedPolicy, importedObservation, importedDirective);
        Assert.Equal("Yes", receipt.Answer); Assert.Equal("Waived", receipt.Reads[1].Answer);
        Assert.NotSame(policy.Surface, importedPolicy.Surface);
        Assert.Same(importedPolicy.Required[0].Predicate, importedObservation.Facts[0].Predicate);
        Assert.Same(importedPolicy.StrongDefaults[0].Surface, Assert.Single(importedDirective.Waived));
        Assert.DoesNotContain(observation.Surface, R.Participants(importedPolicy.Surface));
    }

    [Fact]
    public void Native_arrangement_assessment_can_supply_the_rule_without_family_dispatch()
    {
        var sample = OrnamentReconstructionCases.Specs.First(spec => spec.Family == "SQ-PATH").Create();
        var question = OrnamentArrangementRead.Crystallize("connected cycle", sample.Observation, out _);
        var rule = new D.Rule("whole branch arrangement", question.Surface);
        var receipt = new OrnamentArrangementRead.Receipt(question, sample.Replay().Observation);
        var native = new D.Receipt(new(required: [rule]), new(sample.Observation.Surface,
            [D.Evidence.FromAnswer(question.Surface, receipt.Answer, receipt.Surface)]));
        Assert.Equal("Yes", native.Answer); Assert.Contains(receipt.Surface, R.Participants(native.Surface));
        Assert.Same(question.Surface, Assert.Single(native.Reads).Rule.Predicate);
    }

    private static D.Rule Rule() => new("display only", new Relation(new Placeholder(), new Placeholder()));
    private static D.Evidence Fact(D.Rule rule, string answer) => D.Evidence.FromAnswer(rule.Predicate, answer,
        new Relation(rule.Predicate, new Placeholder()));
    private static D.Observation Observation(params D.Evidence[] facts) => new(new Relation(new Placeholder(), new Placeholder()), facts);
    private static Relation Restore(Relation surface)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("defeasible-read", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var imported));
        return Assert.IsType<Relation>(imported.Root);
    }
}
