using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using P = Gojo.Experiments.QualitativeFrieze.ProvisionalEquivalence;
using E = Gojo.Experiments.QualitativeFrieze.RevisableRuleGeneration;

namespace QualitativeExecution.Tests;

public sealed class RevisableRuleGenerationTests
{
    [Fact]
    public void An_ordinary_prior_chooses_shared_proposal_structure_without_manufacturing_source_support()
    {
        foreach (var scenario in RuleTransferCases.Scenarios())
        {
            var fixture = new Fixture(scenario); var engine = new E();
            var stage = fixture.Run(engine, engine.Ordinary);
            Assert.Same(fixture.SharedOne, stage.Chosen!.Hypothesis); Assert.Equal("Yes", stage.Answer);
            Assert.Equal(2, stage.Proposals.Count(proposal => proposal.Frontier));
            Assert.Single(Entries(stage.Chosen.Hypothesis.Equalities));
            Assert.Empty(Entries(stage.Chosen.SourceAssessment.Support)); Assert.NotEmpty(Entries(stage.Chosen.SourceAssessment.InScopeOpen));
            Assert.Equal("Yes", stage.Chosen.Sharing.Answer); Assert.NotEmpty(Entries(stage.Execution!.Accepted));
            Assert.True(stage.OutputConformance!.IsEligible); Assert.NotEmpty(Entries(stage.OutputConformance.SampleSupport));
            Assert.DoesNotContain(stage.Execution.RoleValues, OrnamentReconstruction.Participants(stage.SourceRead.Surface));
            Assert.DoesNotContain(stage.OutputConformance.Surface, OrnamentReconstruction.Participants(stage.SourceRead.Surface));
            Assert.All(OrnamentReconstruction.Participants(stage.Execution.Surface), item => Assert.True(item is Relation or Placeholder));
            Assert.DoesNotContain(fixture.Clock, OrnamentReconstruction.Participants(stage.Execution.Surface));
            Assert.Contains(fixture.Clock, OrnamentReconstruction.Participants(stage.Step));
        }
    }

    [Fact]
    public void Parameter_independence_can_change_the_selected_explanation_without_changing_the_drawing()
    {
        foreach (var scenario in RuleTransferCases.Scenarios())
        {
            var fixture = new Fixture(scenario); var engine = new E();
            var shared = fixture.Run(engine, engine.Ordinary); var independent = fixture.Run(engine, engine.Independent);
            Assert.Same(fixture.SharedOne, shared.Chosen!.Hypothesis); Assert.Same(fixture.IndependentOne, independent.Chosen!.Hypothesis);
            Assert.Equal("Yes", shared.Chosen.Sharing.Answer); Assert.Equal("No", independent.Chosen.Sharing.Answer);
            Assert.Single(Entries(shared.Chosen.Sharing.DistinctParticipants)); Assert.Equal(2, Entries(independent.Chosen.Sharing.DistinctParticipants).Length);
            Assert.Equal("Yes", independent.Answer); Assert.Empty(Entries(independent.Chosen.Hypothesis.Equalities));
            Assert.Equal("Yes", new OrnamentReconstructionGraphRead(shared.Execution!.Sample!.Observation,
                independent.Execution!.Sample!.Observation, 100000).Answer);
            Assert.Equal(Values(shared.Execution.RoleValues), Values(independent.Execution.RoleValues));
            Assert.Empty(Entries(shared.Chosen.SourceAssessment.Support)); Assert.Empty(Entries(independent.Chosen.SourceAssessment.Support));
        }
    }

    [Fact]
    public void Later_unequal_source_values_withdraw_a_shared_assumption_and_execute_the_split_candidate()
    {
        foreach (var scenario in RuleTransferCases.Scenarios())
        {
            var fixture = new Fixture(scenario); var engine = new E();
            var initial = fixture.Run(engine, engine.Ordinary);
            var partial = new P.Publication(fixture.Source, new Placeholder(), S(new Relation(scenario.HorizontalRole, fixture.Two)));
            var intermediate = fixture.Run(engine, engine.Ordinary, [partial], previous: initial, ordinal: 1);
            Assert.Same(fixture.SharedTwo, intermediate.Chosen!.Hypothesis); Assert.Equal("Yes", intermediate.Answer);
            Assert.Empty(Entries(intermediate.Chosen.SourceAssessment.Support)); Assert.NotEmpty(Entries(intermediate.Chosen.SourceAssessment.InScopeOpen));
            var later = new P.Publication(fixture.Source, new Placeholder(), S(new Relation(scenario.HorizontalRole, fixture.Two),
                new Relation(scenario.VerticalRole, fixture.One)));
            var revised = fixture.Run(engine, engine.Ordinary, [partial, later], previous: intermediate, ordinal: 2);
            Assert.Equal("Yes", revised.Answer); Assert.Same(fixture.IndependentTwoOne, revised.Chosen!.Hypothesis);
            Assert.Single(Entries(revised.Withdrawn)); Assert.Empty(Entries(revised.Carried));
            Assert.Single(Entries(intermediate.Chosen.Hypothesis.Equalities));
            var defeated = revised.Proposals.Single(proposal => ReferenceEquals(proposal.Hypothesis, fixture.SharedTwo));
            Assert.False(defeated.Eligible); Assert.NotEmpty(Entries(defeated.SourceAssessment.Withdrawn));
            Assert.NotEmpty(Entries(defeated.SourceAssessment.InScopeRefuted));
            Assert.Equal(new[] { 2, 1 }, Values(revised.Execution!.RoleValues));
            Assert.DoesNotContain(revised.Execution.RoleValues, OrnamentReconstruction.Participants(revised.SourceRead.Surface));
            Assert.Contains(later.Surface, OrnamentReconstruction.Participants(revised.Step));
        }
    }

    [Fact]
    public void Same_revision_literal_conflicts_cannot_be_overridden_by_a_parameter_sharing_prior()
    {
        foreach (var scenario in RuleTransferCases.Scenarios())
        {
            var fixture = new Fixture(scenario); var engine = new E(); var revision = new Placeholder();
            var secondSource = new Placeholder();
            var first = new P.Publication(fixture.Source, revision, S(new Relation(scenario.HorizontalRole, fixture.Two), new Relation(scenario.VerticalRole, fixture.Two)));
            var opposed = new P.Publication(secondSource, revision, S(new Relation(scenario.HorizontalRole, fixture.Two), new Relation(scenario.VerticalRole, fixture.One)));
            var context = fixture.Context(S(fixture.Source, secondSource));
            var stage = fixture.Run(engine, engine.Ordinary, [first, opposed], context: context);
            Assert.Equal("Open", stage.Answer); Assert.Null(stage.Chosen); Assert.Null(stage.Execution); Assert.Null(stage.OutputConformance);
            Assert.Empty(stage.SourceRead.Eligible); Assert.All(stage.Proposals, proposal => Assert.False(proposal.Eligible));
            Assert.All(stage.SourceRead.Candidates, assessment => Assert.NotEmpty(Entries(assessment.InScopeConflicts)));
            Assert.Contains(first.Surface, OrnamentReconstruction.Participants(stage.Surface));
            Assert.Contains(opposed.Surface, OrnamentReconstruction.Participants(stage.Surface));
        }
    }

    [Fact]
    public void Capability_and_actual_preference_aliases_are_checked_before_execution()
    {
        var scenario = RuleTransferCases.Scenarios()[0]; var fixture = new Fixture(scenario); var engine = new E();
        var incapable = new P.Context(S(), S(new Relation(scenario.HorizontalRole, scenario.VerticalRole)), S(), S(fixture.Source));
        var blocked = fixture.Run(engine, engine.Ordinary, context: incapable);
        Assert.Null(blocked.Chosen); Assert.Null(blocked.Execution); Assert.Equal("Open", blocked.Answer);
        Assert.All(blocked.SourceRead.Candidates, candidate => Assert.NotEmpty(Entries(candidate.Blocked)));
        var sameLookingHypothesis = scenario.Template.Hypothesis(fixture.SharedOne.Bindings, fixture.SharedOne.ParameterValues);
        Assert.Throws<ArgumentException>(() => fixture.Run(engine, engine.Ordinary, preference: S(sameLookingHypothesis.Surface)));
        var anotherEngine = new E();
        Assert.Throws<ArgumentException>(() => fixture.Run(engine, anotherEngine.Ordinary));
        var foreignQuestion = new OrnamentParticipationCount.Question(new Placeholder(), S(), S(new Placeholder()));
        Assert.Throws<ArgumentException>(() => fixture.Run(engine, new D.Policy(strongDefaults: [new D.Rule("lookalike role", foreignQuestion.Surface)])));
    }

    [Fact]
    public void Without_a_prior_and_explicit_choice_all_eligible_alternatives_remain_available()
    {
        foreach (var scenario in RuleTransferCases.Scenarios())
        {
            var fixture = new Fixture(scenario); var engine = new E();
            var open = fixture.Run(engine, engine.Unselected, preference: S());
            Assert.Equal("Open", open.Answer); Assert.Null(open.Chosen); Assert.Null(open.Execution);
            Assert.All(open.Proposals, proposal => Assert.True(proposal.Frontier));
            Assert.Equal(fixture.Candidates.Length, open.SourceRead.Eligible.Count);
            Assert.All(open.Proposals, proposal => Assert.Equal("Unused", Assert.Single(proposal.DefaultRead.Reads).Answer));
            var ordinary = fixture.Run(engine, engine.Ordinary, preference: S());
            Assert.Null(ordinary.Chosen); Assert.Equal(2, ordinary.Proposals.Count(proposal => proposal.Frontier));
        }
    }

    [Fact]
    public void Restricting_source_scope_keeps_other_conflicts_as_history_without_using_them_as_current_refutation()
    {
        var scenario = RuleTransferCases.Scenarios()[0]; var fixture = new Fixture(scenario); var engine = new E();
        var revision = new Placeholder(); var outsideSource = new Placeholder();
        var current = new P.Publication(fixture.Source, revision, S(new Relation(scenario.HorizontalRole, fixture.Two), new Relation(scenario.VerticalRole, fixture.Two)));
        var outside = new P.Publication(outsideSource, revision, S(new Relation(scenario.HorizontalRole, fixture.Two), new Relation(scenario.VerticalRole, fixture.One)));
        var stage = fixture.Run(engine, engine.Ordinary, [current, outside]);
        Assert.Equal("Yes", stage.Answer); Assert.Same(fixture.SharedTwo, stage.Chosen!.Hypothesis);
        Assert.True(stage.Chosen.SourceAssessment.HasConflict); Assert.Empty(Entries(stage.Chosen.SourceAssessment.InScopeConflicts));
        Assert.Contains(outside.Surface, OrnamentReconstruction.Participants(stage.SourceRead.Surface));
    }

    [Fact]
    public void Unavailable_output_basis_does_not_turn_eligible_unresolved_conformance_into_confirmation()
    {
        var scenario = RuleTransferCases.Scenarios()[0]; var fixture = new Fixture(scenario); var engine = new E();
        var unsupportedBasis = S(new Placeholder());
        var stage = engine.Evaluate(scenario.Template, unsupportedBasis, fixture.Candidates, [], fixture.Context(),
            engine.Ordinary, S(fixture.Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()), fixture.Clock);
        Assert.NotNull(stage.Chosen); Assert.Equal("Yes", stage.Execution!.Answer); Assert.NotNull(stage.Execution.Sample);
        Assert.True(stage.OutputConformance!.IsEligible); Assert.NotEmpty(Entries(stage.OutputConformance.SampleOpen));
        Assert.Empty(Entries(stage.OutputConformance.SampleSupport)); Assert.Equal("Open", stage.Answer);
        Assert.Empty(Entries(stage.Chosen.SourceAssessment.Support));
    }

    [Fact]
    public void A_native_prior_template_source_and_proposal_world_can_rebind_and_reexecute_independently()
    {
        var scenario = RuleTransferCases.Scenarios()[0]; var fixture = new Fixture(scenario); var engine = new E();
        var publication = new P.Publication(fixture.Source, new Placeholder(), S(new Relation(scenario.HorizontalRole, fixture.Two),
            new Relation(scenario.VerticalRole, fixture.One)));
        var context = fixture.Context(); var preference = S(fixture.Candidates.Select(candidate => (IElement)candidate.Surface).ToArray());
        var before = fixture.Run(engine, engine.Ordinary, [publication]);
        var world = Restore(S(engine.Surface, scenario.Template.Surface, S(scenario.Reach),
            S(fixture.Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()), S(publication.Surface), context.Surface, preference));
        var fields = Entries(world);
        Assert.True(E.TryRead((Relation)fields[0], out var importedEngine, out var priorWitness));
        Assert.True(RevisablePatternTemplate.TryRead((Relation)fields[1], out var template, out var templateWitness));
        var rawCandidates = Entries((Relation)fields[3]).Cast<Relation>().ToArray();
        var candidates = rawCandidates.Select(raw => ReadHypothesis(raw, template)).ToArray();
        var candidateMap = rawCandidates.Select((raw, i) => new Relation(raw, candidates[i].Surface)).ToArray();
        var requested = Entries((Relation)fields[6]);
        var reboundPreference = S(requested.Select(raw => (IElement)candidates[Array.FindIndex(rawCandidates,
            candidate => ReferenceEquals(candidate, raw))].Surface).ToArray());
        var publications = Entries((Relation)fields[4]).Cast<Relation>().Select(raw =>
        {
            var header = (Relation)raw.Left; return new P.Publication(header.Left, header.Right, (Relation)raw.Right);
        }).ToArray();
        var rawContext = (Relation)fields[5]; var contextBody = (Relation)rawContext.Right; var choice = (Relation)contextBody.Right;
        var importedContext = new P.Context((Relation)rawContext.Left, (Relation)contextBody.Left, (Relation)choice.Left, (Relation)choice.Right);
        var after = importedEngine.Evaluate(template, (Relation)fields[2], candidates, publications, importedContext,
            importedEngine.Ordinary, reboundPreference, new Carrier());
        var rebinding = new Relation(new Relation(((Relation)fields[1]).Left is Relation templateBody ? templateBody.Right : fields[1], template.Parameters),
            new Relation(S(candidateMap.Cast<IElement>().ToArray()), new Relation(fields[6], reboundPreference)));
        var witness = new Relation(world, new Relation(priorWitness, new Relation(templateWitness, new Relation(rebinding, after.Surface))));
        Assert.Equal("Yes", before.Answer); Assert.Equal("Yes", after.Answer);
        Assert.Same(importedEngine.SharedParameterQuestion.Surface, importedEngine.Ordinary.Rules[0].Predicate);
        Assert.Same(candidates[3], after.Chosen!.Hypothesis); Assert.Equal(new[] { 2, 1 }, Values(after.Execution!.RoleValues));
        Assert.Equal("Yes", new OrnamentReconstructionGraphRead(before.Execution!.Sample!.Observation,
            after.Execution.Sample!.Observation, 100000).Answer);
        Assert.Contains(template.Parameters, OrnamentReconstruction.Participants(witness));
        Assert.DoesNotContain(engine.Surface, OrnamentReconstruction.Participants(after.Surface));
        Assert.DoesNotContain(scenario.Template.Surface, OrnamentReconstruction.Participants(after.Surface));
        Assert.DoesNotContain(publication.Source, OrnamentReconstruction.Participants(after.SourceRead.Surface));
    }

    private sealed class Fixture
    {
        public RuleTransferCases.GenerationCase Scenario { get; }
        public Carrier Clock { get; } = new();
        public IElement Source { get; } = new Placeholder();
        public Relation One { get; }
        public Relation Two { get; }
        public P.Hypothesis SharedOne { get; }
        public P.Hypothesis SharedTwo { get; }
        public P.Hypothesis IndependentOne { get; }
        public P.Hypothesis IndependentTwoOne { get; }
        public P.Hypothesis[] Candidates { get; }
        public Fixture(RuleTransferCases.GenerationCase scenario)
        {
            Scenario = scenario; One = S(scenario.Reach); Two = S(scenario.Reach, scenario.Reach);
            SharedOne = Candidate(true, One, One); IndependentOne = Candidate(false, One, One);
            SharedTwo = Candidate(true, Two, Two); IndependentTwoOne = Candidate(false, Two, One);
            Candidates = [SharedOne, IndependentOne, SharedTwo, IndependentTwoOne];
        }
        private P.Hypothesis Candidate(bool shared, Relation horizontal, Relation vertical)
        {
            var h = new Placeholder(); IElement v = shared ? h : new Placeholder();
            return Scenario.Template.Hypothesis(S(new Relation(Scenario.HorizontalRole, h), new Relation(Scenario.VerticalRole, v)),
                shared ? S(new Relation(h, horizontal)) : S(new Relation(h, horizontal), new Relation(v, vertical)));
        }
        public P.Context Context(Relation? working = null) => new(S(Scenario.Template.Surface),
            S(new Relation(Scenario.HorizontalRole, Scenario.VerticalRole)), S(), working ?? S(Source));
        public E.Stage Run(E engine, D.Policy policy, IReadOnlyList<P.Publication>? publications = null,
            P.Context? context = null, Relation? preference = null, E.Stage? previous = null, int ordinal = 0) =>
            engine.Evaluate(Scenario.Template, S(Scenario.Reach), Candidates, publications ?? [], context ?? Context(), policy,
                preference ?? S(Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()), Clock, ordinal, previous);
    }
    private static int[] Values(Relation roles) => Entries(roles).Cast<Relation>().Select(row => Entries((Relation)row.Right).Length).ToArray();
    private static P.Hypothesis ReadHypothesis(Relation raw, RevisablePatternTemplate template)
    {
        var program = (Relation)raw.Right; var bindings = (Relation)program.Right; var values = (Relation)bindings.Right; var equalities = (Relation)values.Right;
        return new(template.Parameters, template.Surface, (Relation)bindings.Left, (Relation)values.Left, (Relation)equalities.Left,
            new Relation(equalities.Right, template.Provenance));
    }
    private static Relation Restore(Relation source)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("native prior and proposal world", source));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var imported));
        return Assert.IsType<Relation>(imported.Root);
    }
    private static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
