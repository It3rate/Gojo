using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using R = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace QualitativeExecution.Tests;

public sealed class StructuralEvidenceRulesTests
{
    [Fact]
    public void A_goal_derives_from_actual_conjunctive_leaf_witnesses_without_asking_unrelated_rules()
    {
        var a = Predicate(); var b = Predicate(); var goal = Predicate(); var unrelated = Predicate();
        var yesA = Fact(a, "Yes"); var yesB = Fact(b, "Yes");
        var program = new R.Program([new("chosen candidate", goal, [a, b]), new("unrelated", unrelated, [Predicate()])]);
        var source = new R.Publication(Predicate(), [yesA, yesB]);
        var read = R.Read(program, source, goal);
        Assert.Equal("Yes", read.Answer); Assert.Equal(new[] { goal, a, b }, E(read.Visited));
        Assert.DoesNotContain(unrelated, E(read.Visited)); Assert.Empty(E(read.Missing));
        Assert.Contains(yesA.Surface, Participants(read.Surface)); Assert.Contains(yesB.Surface, Participants(read.Surface));
        Assert.Single(E(read.Supported)); Assert.Equal(3, E(read.Consumed).Length);
    }

    [Fact]
    public void A_false_premise_refutes_only_this_candidate_and_preserves_later_unasked_goals()
    {
        var a = Predicate(); var b = Predicate(); var goal = Predicate();
        var read = R.Read(new([new("ordered conjunction", goal, [a, b])]), new(Predicate(), [Fact(a, "No")]), goal);
        Assert.Equal("No", read.Answer); Assert.Equal(new[] { goal, a }, E(read.Visited));
        Assert.Equal(new[] { b }, E(read.Unasked)); Assert.Empty(E(read.Missing)); Assert.Single(E(read.Refuted));
    }

    [Fact]
    public void A_closed_publication_does_not_supply_negative_facts_for_missing_predicates()
    {
        var leaf = Predicate(); var goal = Predicate(); var source = new R.Publication(Predicate(), []);
        var read = R.Read(new([new("needs a fact", goal, [leaf])]), source, goal);
        Assert.Equal("Open", read.Answer); Assert.Equal(new[] { leaf }, E(read.Missing)); Assert.Empty(E(read.Refuted));
        Assert.Equal("Open", R.Read(new([]), source, Predicate()).Answer);
    }

    [Fact]
    public void An_open_assessment_retains_its_actual_leaf_obligation()
    {
        var leaf = Predicate(); var goal = Predicate(); var fact = Fact(leaf, "Open");
        var read = R.Read(new([new("needs resolved evidence", goal, [leaf])]), new(Predicate(), [fact]), goal);
        Assert.Equal("Open", read.Answer); Assert.Equal(new[] { leaf }, E(read.Missing));
        Assert.NotEmpty(E(read.Open)); Assert.Contains(fact.Surface, Participants(read.Surface));
    }

    [Theory]
    [InlineData("Yes")]
    [InlineData("No")]
    public void An_open_assessment_cannot_be_erased_by_one_concurrent_known_assessment(string known)
    {
        var leaf = Predicate();
        var read = R.Read(new([]), new(Predicate(), [Fact(leaf, known), Fact(leaf, "Open")]), leaf);
        Assert.Equal("Open", read.Answer); Assert.Equal(new[] { leaf }, E(read.Missing));
    }

    [Fact]
    public void A_blocked_leaf_keeps_the_passage_reason_and_skips_later_obligations()
    {
        var leaf = Predicate(); var later = Predicate(); var goal = Predicate(); var fact = Fact(leaf, "Blocked");
        var read = R.Read(new([new("ordered requirements", goal, [leaf, later])]), new(Predicate(), [fact]), goal);
        Assert.Equal("Blocked", read.Answer); Assert.Equal(new[] { later }, E(read.Unasked));
        Assert.Contains(fact.Surface, Participants(read.Blocked));
    }

    [Fact]
    public void Opposed_evidence_is_conflict_and_does_not_visit_later_premises()
    {
        var leaf = Predicate(); var later = Predicate(); var goal = Predicate();
        var yes = Fact(leaf, "Yes"); var no = Fact(leaf, "No");
        var read = R.Read(new([new("conflicted conjunction", goal, [leaf, later])]), new(Predicate(), [yes, no]), goal);
        Assert.Equal("Conflict", read.Answer); Assert.NotEmpty(E(read.Conflicts)); Assert.Equal(new[] { later }, E(read.Unasked));
        Assert.Contains(yes.Surface, Participants(read.Surface)); Assert.Contains(no.Surface, Participants(read.Surface));
    }

    [Fact]
    public void Direct_conclusion_evidence_cannot_hide_a_conflicting_construction_definition()
    {
        var leaf = Predicate(); var goal = Predicate();
        var read = R.Read(new([new("candidate constraint", goal, [leaf])]),
            new(Predicate(), [Fact(goal, "Yes"), Fact(leaf, "No")]), goal);
        Assert.Equal("Conflict", read.Answer); Assert.NotEmpty(E(read.Supported)); Assert.NotEmpty(E(read.Refuted));
    }

    [Fact]
    public void Query_budget_cuts_are_separate_from_missing_source_facts()
    {
        var leaf = Predicate(); var goal = Predicate(); var program = new R.Program([new("needs lookup", goal, [leaf])]);
        var source = new R.Publication(Predicate(), [Fact(leaf, "Yes")]);
        var zero = R.Read(program, source, goal, 0);
        Assert.Equal("Open", zero.Answer); Assert.Empty(E(zero.Visited)); Assert.Empty(E(zero.Missing));
        Assert.Equal(new[] { goal }, E(zero.BudgetCuts)); Assert.Equal(new[] { goal }, E(zero.Unasked));
        var one = R.Read(program, source, goal, 1);
        Assert.Equal("Open", one.Answer); Assert.Equal(new[] { goal }, E(one.Visited)); Assert.Empty(E(one.Missing));
        Assert.Equal(new[] { leaf }, E(one.BudgetCuts)); Assert.Equal("Yes", R.Read(program, source, goal, 2).Answer);
    }

    [Fact]
    public void Reused_subgoals_retain_one_consultation_without_becoming_independent_evidence()
    {
        var leaf = Predicate(); var left = Predicate(); var right = Predicate(); var goal = Predicate();
        var program = new R.Program([new("root", goal, [left, right]), new("left", left, [leaf]), new("right", right, [leaf])]);
        var read = R.Read(program, new(Predicate(), [Fact(leaf, "Yes")]), goal, 4);
        Assert.Equal("Yes", read.Answer); Assert.Equal(4, E(read.Visited).Length);
        Assert.Single(E(read.Visited), item => ReferenceEquals(item, leaf)); Assert.Empty(E(read.BudgetCuts));
    }

    [Fact]
    public void Duplicate_conclusions_cycles_and_open_rule_scopes_remain_blocked_contracts()
    {
        var a = Predicate(); var b = Predicate();
        var duplicate = new R.Program([new("one", a, []), new("two", a, [])]);
        var cycle = new R.Program([new("first", a, [b]), new("second", b, [a])]);
        var open = new R.Program(S(new Relation(StructuralConstraint.Open(b), a)));
        foreach (var invalid in new[] { duplicate, cycle, open })
        {
            Assert.False(invalid.IsValid); var read = R.Read(invalid, new(Predicate(), []), a);
            Assert.Equal("Blocked", read.Answer); Assert.Empty(E(read.Visited)); Assert.NotEmpty(E(read.Blocked));
        }
    }

    [Fact]
    public void Lookalike_predicates_do_not_supply_the_requested_native_alias()
    {
        var leaf = Predicate(); var copy = new Relation(leaf.Left, leaf.Right); var goal = Predicate();
        var read = R.Read(new([new("identity matters", goal, [leaf])]), new(Predicate(), [Fact(copy, "Yes")]), goal);
        Assert.Equal("Open", read.Answer); Assert.Equal(new[] { leaf }, E(read.Missing)); Assert.Empty(E(read.Supported));
    }

    [Fact]
    public void One_native_world_restores_rule_leaf_and_goal_aliases_without_a_side_registry()
    {
        var leaf = Predicate(); var goal = Predicate(); var program = new R.Program([new("portable definition", goal, [leaf])]);
        var source = new R.Publication(Predicate(), [Fact(leaf, "Yes")]); var world = new Relation(program.Surface, new Relation(source.Surface, goal));
        var restored = Restore(world); var body = Assert.IsType<Relation>(restored.Right);
        Assert.True(R.Program.TryRead(Assert.IsType<Relation>(restored.Left), out var actualProgram));
        Assert.True(R.Publication.TryRead(Assert.IsType<Relation>(body.Left), out var actualSource));
        var actualGoal = Assert.IsType<Relation>(body.Right); var read = R.Read(actualProgram, actualSource, actualGoal);
        Assert.Equal("Yes", read.Answer); Assert.NotSame(goal, actualGoal);
        Assert.Same(Assert.Single(actualProgram.Rules).Premises[0], Assert.Single(actualSource.Evidence).Predicate);
    }

    [Fact]
    public void A_derived_fact_requires_an_explicit_downstream_publication_and_survives_transport()
    {
        var leaf = Predicate(); var firstGoal = Predicate(); var downstreamGoal = Predicate();
        var source = new R.Publication(Predicate(), [Fact(leaf, "Yes")]);
        var first = R.Read(new([new("first construction", firstGoal, [leaf])]), source, firstGoal);
        var independent = new R.Program([new("second construction", downstreamGoal, [firstGoal])]);
        Assert.Equal("Open", R.Read(independent, source, downstreamGoal).Answer);
        var fact = first.ToEvidence(); Assert.Equal("Yes", fact.Answer); Assert.Same(firstGoal, fact.Predicate);
        Assert.Contains(first.Surface, Participants(fact.Surface));
        var explicitPublication = new R.Publication(new(source.Source, first.Surface), [fact]);
        var imported = Restore(new(independent.Surface, new Relation(explicitPublication.Surface, downstreamGoal)));
        var body = Assert.IsType<Relation>(imported.Right);
        var read = R.Read(new(Assert.IsType<Relation>(imported.Left)), new(Assert.IsType<Relation>(body.Left)), Assert.IsType<Relation>(body.Right));
        Assert.Equal("Yes", read.Answer);
        var incompatibleGoal = new Relation(firstGoal.Left, firstGoal.Right);
        Assert.Equal("Open", R.Read(new([new("foreign predicate", downstreamGoal, [incompatibleGoal])]), explicitPublication, downstreamGoal).Answer);
    }

    [Fact]
    public void Downstream_evidence_preserves_actual_opposed_root_accounts()
    {
        var goal = Predicate(); var first = R.Read(new([]), new(Predicate(), [Fact(goal, "Yes"), Fact(goal, "No")]), goal);
        Assert.Equal("Conflict", first.Answer); var fact = first.ToEvidence();
        Assert.NotEmpty(E(fact.Supported)); Assert.NotEmpty(E(fact.Refuted)); Assert.Equal("Open", fact.Answer);
        Assert.Contains(first.Surface, Participants(fact.Surface));
        Assert.Equal("Conflict", R.Read(new([]), new(Predicate(), [fact]), goal).Answer);
    }

    [Fact]
    public void One_combined_conflicting_publication_keeps_both_actual_root_polarities()
    {
        var goal = Predicate(); var combined = new D.Evidence(goal, S(Predicate()), S(Predicate()), S(), S());
        Assert.Equal("Open", combined.Answer);
        var read = R.Read(new([]), new(Predicate(), [combined]), goal);
        Assert.Equal("Conflict", read.Answer); var fact = read.ToEvidence();
        Assert.NotEmpty(E(fact.Supported)); Assert.NotEmpty(E(fact.Refuted));
        Assert.Contains(combined.Surface, Participants(fact.Surface));
    }

    [Fact]
    public void An_inherited_child_conflict_is_not_two_complete_proofs_of_the_root_conjunction()
    {
        var leaf = Predicate(); var later = Predicate(); var goal = Predicate();
        var read = R.Read(new([new("needs two premises", goal, [leaf, later])]),
            new(Predicate(), [Fact(leaf, "Yes"), Fact(leaf, "No")]), goal);
        Assert.Equal("Conflict", read.Answer); Assert.Equal(new[] { later }, E(read.Unasked));
        Assert.Empty(E(read.Supported)); Assert.Empty(E(read.Refuted));
        var fact = read.ToEvidence(); Assert.Equal("Open", fact.Answer);
        Assert.Empty(E(fact.Supported)); Assert.Empty(E(fact.Refuted)); Assert.Contains(read.Conflicts, Participants(fact.Surface));
        Assert.Equal("Open", R.Read(new([]), new(Predicate(), [fact]), goal).Answer);
    }

    [Fact]
    public void An_explicit_empty_premise_rule_is_a_candidate_tautology_and_not_a_global_default()
    {
        var goal = Predicate(); var program = new R.Program([new("explicit candidate", goal, [])]);
        Assert.True(program.IsValid); var source = new R.Publication(Predicate(), []);
        Assert.Equal("Yes", R.Read(program, source, goal).Answer);
        Assert.Equal("Open", R.Read(program, source, Predicate()).Answer);
    }

    private static Relation Predicate() => new(new Placeholder(), new Placeholder());
    private static D.Evidence Fact(Relation predicate, string answer) => D.Evidence.FromAnswer(predicate, answer, new(predicate, new Placeholder()));
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static HashSet<IElement> Participants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        { if (!seen.Add(item) || item is not Relation row) continue; pending.Push(row.Left); pending.Push(row.Right); }
        return seen;
    }
    private static Relation Restore(Relation surface)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("inference-world", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
}
