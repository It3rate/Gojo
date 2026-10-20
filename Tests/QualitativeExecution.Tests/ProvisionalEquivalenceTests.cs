using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class ProvisionalEquivalenceTests
{
    [Fact]
    public void Matching_retains_actual_ordered_occurrence_pairs_and_one_sided_participation()
    {
        var f = new Fixture(); var a = S(f.U, f.U); var b = S(f.U);
        var read = f.Engine.Compare(a, b);
        Assert.True(read.IsUnequal); Assert.False(read.IsEqual);
        var pair = Assert.IsType<Relation>(Assert.Single(E(read.Pairs)));
        Assert.Same(a.Right, pair.Left); Assert.Same(b.Right, pair.Right);
        var remainder = Assert.Single(E(read.LeftRemainder));
        Assert.Same(((Relation)a.Right).Right, remainder);
        Assert.Empty(E(read.RightRemainder)); Assert.Empty(E(read.Open));
    }

    [Fact]
    public void Open_prefix_and_independent_unmeasured_reaches_do_not_establish_inequality()
    {
        var f = new Fixture();
        foreach (var comparison in new[] { f.Engine.Compare(StructuralConstraint.Open(f.U), S(f.U)), f.Engine.Compare(S(f.U), S(f.V)) })
        {
            Assert.False(comparison.IsEqual); Assert.False(comparison.IsUnequal);
            Assert.NotEmpty(E(comparison.Open));
        }
        Assert.True(f.Engine.Compare(S(f.U, f.V), S(f.U, f.V)).IsEqual);
    }

    [Fact]
    public void Repeated_parameter_is_an_assumption_between_distinct_actual_roles()
    {
        var f = new Fixture(); var publication = f.Publish(S(new Relation(f.X, S(f.U)), new Relation(f.Y, StructuralConstraint.Open())));
        var before = f.Engine.Assess(f.Hypotheses, [publication], f.Context());
        Assert.Same(f.Shared, before.Chosen); Assert.NotSame(f.X, f.Y);
        var assessment = before.Candidates[0];
        Assert.Empty(E(assessment.Support)); Assert.Single(E(assessment.Open));
        Assert.Same(f.XY, Assert.Single(E(assessment.Retained)));
        var dependency = Assert.IsType<Relation>(Assert.Single(E(assessment.Dependencies)));
        var bindings = Assert.IsType<Relation>(dependency.Left);
        Assert.Same(E(f.Shared.Bindings)[0], bindings.Left); Assert.Same(E(f.Shared.Bindings)[1], bindings.Right);
        Assert.Same(f.XY, dependency.Right);
        Assert.DoesNotContain(E(assessment.Support), row => ReferenceEquals(row, E(assessment.SampleSupport).FirstOrDefault()));
    }

    [Fact]
    public void Later_distinguishing_evidence_revises_selection_without_rewriting_earlier_read()
    {
        var f = new Fixture();
        var initial = f.Engine.Assess(f.Hypotheses, [f.Publish(S(new Relation(f.X, S(f.U)), new Relation(f.Y, StructuralConstraint.Open())))], f.Context());
        var captured = initial.Surface; var oldOpen = initial.Candidates[0].Open;
        var refined = f.Engine.Assess(f.Hypotheses, [f.Publish(S(new Relation(f.X, S(f.U, f.U)), new Relation(f.Y, S(f.U))))], f.Context());
        Assert.Same(f.Shared, initial.Chosen); Assert.Same(f.Separate, refined.Chosen);
        Assert.Same(captured, initial.Surface); Assert.Same(oldOpen, initial.Candidates[0].Open);
        Assert.Empty(E(initial.Candidates[0].Refuted));
        Assert.Single(E(refined.Candidates[0].Refuted)); Assert.Single(E(refined.Candidates[0].Withdrawn));
        Assert.NotEmpty(E(refined.Candidates[0].InScopeSampleRefuted));
    }

    [Fact]
    public void Concurrent_supplier_conflict_remains_visible_under_explicit_working_source_choices()
    {
        var f = new Fixture(); var state = new Placeholder(); var a = new Placeholder(); var b = new Placeholder();
        var pubs = new[] {
            new ProvisionalEquivalence.Publication(a, state, S(new Relation(f.X, S(f.U)), new Relation(f.Y, S(f.U)))),
            new ProvisionalEquivalence.Publication(b, state, S(new Relation(f.X, S(f.U, f.U)), new Relation(f.Y, S(f.U)))) };
        var contested = f.Engine.Assess(f.Hypotheses, pubs, f.Context());
        Assert.Null(contested.Chosen); Assert.Empty(contested.Eligible);
        Assert.All(contested.Candidates, item => Assert.NotEmpty(E(item.InScopeConflicts)));
        var onA = f.Engine.Assess(f.Hypotheses, pubs, f.Context(S(a)));
        var onB = f.Engine.Assess(f.Hypotheses, pubs, f.Context(S(b)));
        Assert.Same(f.Shared, onA.Chosen); Assert.Same(f.Separate, onB.Chosen);
        Assert.All(onA.Candidates.Concat(onB.Candidates), item => Assert.True(item.HasConflict));
        Assert.All(onA.Candidates.Concat(onB.Candidates), item => Assert.Empty(E(item.InScopeConflicts)));
        Assert.Single(E(onA.Candidates[0].Support)); Assert.Single(E(onA.Candidates[0].Refuted));
        Assert.NotEmpty(E(onA.Candidates[1].SampleRefuted));
    }

    [Fact]
    public void Different_observation_events_are_not_reported_as_simultaneous_supplier_conflict()
    {
        var f = new Fixture(); var source = new Placeholder();
        var pubs = new[] {
            new ProvisionalEquivalence.Publication(source, new Placeholder(), S(new Relation(f.X, S(f.U)), new Relation(f.Y, S(f.U)))),
            new ProvisionalEquivalence.Publication(source, new Placeholder(), S(new Relation(f.X, S(f.U, f.U)), new Relation(f.Y, S(f.U)))) };
        var selection = f.Engine.Assess(f.Hypotheses, pubs, f.Context());
        Assert.All(selection.Candidates, item => Assert.Empty(E(item.Conflicts)));
        // Both remain active premises because the caller supplied both. Event
        // distinction does not secretly choose a latest observation.
        Assert.Null(selection.Chosen);
    }

    [Fact]
    public void Withdrawal_exposes_only_the_affected_explicit_assumption_dependencies()
    {
        var f = new Fixture(); var z = new Placeholder(); var domain = S(f.X, f.Y, z); var yz = new Relation(f.Y, z);
        var hypothesis = new ProvisionalEquivalence.Hypothesis(domain, f.Shared.Program,
            S(new Relation(f.X, f.P), new Relation(f.Y, f.P), new Relation(z, f.P)), S(new Relation(f.P, S(f.U))), S(f.XY, yz));
        var engine = new ProvisionalEquivalence(domain, S(f.U));
        var assessment = Assert.Single(engine.Assess([hypothesis], [f.Publish(S(new Relation(f.X, S(f.U)), new Relation(f.Y, S(f.U)), new Relation(z, S(f.U, f.U))))],
            new ProvisionalEquivalence.Context(S(hypothesis.Program), S(f.XY), S(hypothesis.Surface))).Candidates);
        Assert.Same(f.XY, Assert.Single(E(assessment.Retained)));
        var withdrawal = Assert.IsType<Relation>(Assert.Single(E(assessment.Withdrawn)));
        Assert.Same(yz, withdrawal.Left);
        Assert.Equal(2, E(assessment.Dependencies).Length);
        Assert.Same(f.XY, ((Relation)E(assessment.Dependencies)[0]).Right);
    }

    [Fact]
    public void A_copied_role_or_program_does_not_own_the_original_surface()
    {
        var f = new Fixture(); var copiedRole = new Placeholder();
        var forged = new ProvisionalEquivalence.Hypothesis(f.Domain, f.Shared.Program,
            S(new Relation(copiedRole, f.P), new Relation(f.Y, f.P)), f.Shared.ParameterValues, S(new Relation(copiedRole, f.Y)));
        var assessment = Assert.Single(f.Engine.Assess([forged], [],
            new ProvisionalEquivalence.Context(S(f.Shared.Program), S(), S(forged.Surface))).Candidates);
        Assert.False(assessment.IsEligible); Assert.NotEmpty(E(assessment.Blocked));
        var copiedProgram = new Relation(f.Shared.Program.Left, f.Shared.Program.Right);
        var blocked = f.Engine.Assess([f.Shared], [], new ProvisionalEquivalence.Context(S(copiedProgram), S(), S(f.Shared.Surface)));
        Assert.Null(blocked.Chosen); Assert.NotEmpty(E(blocked.Candidates[0].Blocked));
    }

    [Fact]
    public void Missing_capability_does_not_rewrite_source_evidence_or_invent_a_finer_realization()
    {
        var f = new Fixture(); var publication = f.Publish(S(new Relation(f.X, S(f.U, f.U)), new Relation(f.Y, S(f.U))));
        var selection = f.Engine.Assess(f.Hypotheses, [publication], new ProvisionalEquivalence.Context(S(f.Shared.Program), S(f.XY), S(f.Shared.Surface, f.Separate.Surface)));
        Assert.Null(selection.Chosen); Assert.Empty(selection.Eligible);
        Assert.Single(E(selection.Candidates[0].Refuted)); Assert.NotEmpty(E(selection.Candidates[1].Blocked));
        Assert.Same(publication.Values, publication.Surface.Right);
    }

    [Fact]
    public void Duplicate_parameter_samples_and_duplicate_supplier_roles_are_blocked()
    {
        var f = new Fixture();
        var malformed = new ProvisionalEquivalence.Hypothesis(f.Domain, f.Shared.Program, f.Shared.Bindings,
            S(new Relation(f.P, S(f.U)), new Relation(f.P, S(f.U, f.U))), f.Shared.Equalities);
        var selection = f.Engine.Assess([malformed], [], new ProvisionalEquivalence.Context(S(malformed.Program), S(), S(malformed.Surface)));
        Assert.Null(selection.Chosen); Assert.NotEmpty(E(selection.Candidates[0].Blocked));
        var duplicated = f.Publish(S(new Relation(f.X, S(f.U)), new Relation(f.X, S(f.U, f.U)), new Relation(f.Y, S(f.U))));
        var rejected = f.Engine.Assess(f.Hypotheses, [duplicated], f.Context());
        Assert.Null(rejected.Chosen); Assert.All(rejected.Candidates, item => Assert.NotEmpty(E(item.Blocked)));
    }

    [Fact]
    public void Foreign_publication_roles_and_open_publication_complements_are_distinct_reads()
    {
        var f = new Fixture(); var impostor = f.Publish(S(new Relation(new Placeholder(), S(f.U)), new Relation(f.Y, S(f.U))));
        var rejected = f.Engine.Assess(f.Hypotheses, [impostor], f.Context());
        Assert.Null(rejected.Chosen); Assert.All(rejected.Candidates, item => Assert.NotEmpty(E(item.Blocked)));
        var partial = f.Publish(StructuralConstraint.Open(new Relation(f.X, S(f.U)), new Relation(f.Y, S(f.U))));
        var read = f.Engine.Assess(f.Hypotheses, [partial], f.Context());
        Assert.Same(f.Shared, read.Chosen); Assert.Single(E(read.Candidates[0].Support));
        Assert.Empty(E(read.Candidates[0].Blocked));
        // The published prefix supports the selected two-role question; it
        // does not certify any unqueried entries in the open complement.
        Assert.Same(partial.Values, partial.Surface.Right);
    }

    [Fact]
    public void Native_preference_selects_only_actual_pareto_candidates_and_keeps_ties()
    {
        var f = new Fixture();
        var twin = new ProvisionalEquivalence.Hypothesis(f.Domain, f.Shared.Program, f.Shared.Bindings, f.Shared.ParameterValues, f.Shared.Equalities);
        var selection = f.Engine.Assess([f.Shared, twin], [], new ProvisionalEquivalence.Context(S(f.Shared.Program), S(f.XY), S(twin.Surface, f.Shared.Surface)));
        Assert.Equal(2, selection.Pareto.Count); Assert.Same(twin, selection.Chosen);
        var copied = new Relation(twin.Surface.Left, twin.Surface.Right);
        Assert.Null(f.Engine.Assess([twin], [], new ProvisionalEquivalence.Context(S(twin.Program), S(), S(copied))).Chosen);
    }

    [Fact]
    public void Shared_parameter_answers_reverse_and_transitive_query_scope_without_merging_roles()
    {
        var f = new Fixture(); var z = new Placeholder(); var domain = S(f.X, f.Y, z);
        var hypothesis = new ProvisionalEquivalence.Hypothesis(domain, f.Shared.Program,
            S(new Relation(f.X, f.P), new Relation(f.Y, f.P), new Relation(z, f.P)), S(new Relation(f.P, S(f.U))), S(f.XY, new Relation(f.Y, z)));
        var engine = new ProvisionalEquivalence(domain, S(f.U));
        var assessment = Assert.Single(engine.Assess([hypothesis], [], new ProvisionalEquivalence.Context(S(hypothesis.Program), S(new Relation(f.Y, f.X), new Relation(f.X, z)), S(hypothesis.Surface))).Candidates);
        Assert.Equal(0, assessment.QueryOpenCount); Assert.Equal(1, assessment.ParameterCount);
        Assert.Equal(3, E(hypothesis.Bindings).Length); Assert.Equal(2, E(assessment.Dependencies).Length);
    }

    private sealed class Fixture
    {
        public Placeholder U { get; } = new(); public Placeholder V { get; } = new();
        public Placeholder X { get; } = new(); public Placeholder Y { get; } = new();
        public Placeholder P { get; } = new(); public Placeholder Q { get; } = new();
        public Relation Domain { get; }
        public Relation XY { get; }
        public ProvisionalEquivalence Engine { get; }
        public ProvisionalEquivalence.Hypothesis Shared { get; }
        public ProvisionalEquivalence.Hypothesis Separate { get; }
        public ProvisionalEquivalence.Hypothesis[] Hypotheses => [Shared, Separate];
        public Fixture()
        {
            Domain = S(X, Y); XY = new(X, Y); Engine = new(Domain, S(U, V));
            Shared = new(Domain, S(new Relation(X, Y)), S(new Relation(X, P), new Relation(Y, P)), S(new Relation(P, S(U))), S(XY));
            Separate = new(Domain, S(new Relation(X, Y)), S(new Relation(X, P), new Relation(Y, Q)), S(new Relation(P, S(U, U)), new Relation(Q, S(U))), S());
        }
        public ProvisionalEquivalence.Publication Publish(Relation values) => new(new Placeholder(), new Placeholder(), values);
        public ProvisionalEquivalence.Context Context(Relation? sources = null) => new(S(Shared.Program, Separate.Program), S(XY), S(Shared.Surface, Separate.Surface), sources);
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
