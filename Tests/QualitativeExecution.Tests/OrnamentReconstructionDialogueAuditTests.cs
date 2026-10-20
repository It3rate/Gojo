using System.Numerics;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionDialogue;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionDialogueAuditTests
{
    [Fact]
    public void The_first_question_depends_on_public_candidate_predictions_and_not_the_concealed_source()
    {
        var fixture = new Fixture(); var policy = fixture.Policy();
        var firstSource = fixture.Supplier("SQ-PATH"); var secondSource = fixture.Supplier("X-CROSS");
        var decision = Assert.IsType<D.Decision>(policy.Choose(fixture.Projection.Universe, []));
        Assert.Same(fixture.Visible.Surface, decision.Query.Surface);
        Assert.Equal(0, firstSource.Bank.EvaluatedReads); Assert.Equal(0, secondSource.Bank.EvaluatedReads);

        var first = policy.Run(firstSource.Supplier); var second = policy.Run(secondSource.Supplier);
        Assert.NotEmpty(first.Steps); Assert.NotEmpty(second.Steps);
        Assert.Same(decision.Query.Surface, first.Steps[0].Query.Surface);
        Assert.Same(decision.Query.Surface, second.Steps[0].Query.Surface);
        Assert.Equal("Yes", first.Steps[0].Reply.Answer); Assert.Equal("No", second.Steps[0].Reply.Answer);
        var publicDecision = OrnamentReconstructionPacket.Participants(decision.Surface);
        Assert.DoesNotContain(firstSource.Sample.Observation.Surface, publicDecision);
        Assert.DoesNotContain(secondSource.Sample.Observation.Surface, publicDecision);
    }

    [Fact]
    public void Replies_and_transcripts_keep_private_source_observations_and_certifying_reads_in_the_audit_root_only()
    {
        var fixture = new Fixture(); var source = fixture.Supplier("SQ-PATH"); var result = fixture.Policy().Run(source.Supplier);
        Assert.True(result.Uniform); Assert.NotEmpty(result.Steps);
        foreach (var root in new IElement[] { source.Supplier.Surface, result.Surface, result.FinalMessage.Surface,
                     result.OriginalMessage.Surface }.Concat(result.Steps.Select(step => (IElement)step.Reply.Surface)))
        {
            var publicRead = OrnamentReconstructionPacket.Participants(root);
            foreach (var hidden in new IElement[] { source.Sample.Surface, source.Sample.Program.Surface,
                source.Sample.Execution.Surface, source.Sample.Observation.Surface, source.Sample.Publisher.Surface,
                source.Sample.MechanicalEvidence, source.Bank.Surface, source.Input.Surface }) Assert.DoesNotContain(hidden, publicRead);
            Assert.DoesNotContain(publicRead, item => item is Carrier or CarrierLocation);
        }
        Assert.Contains(source.Sample.Observation.Surface, OrnamentReconstructionPacket.Participants(result.AuditSurface));
        Assert.Contains(source.Sample.Observation.Surface, OrnamentReconstructionPacket.Participants(source.Supplier.AuditSurface));
        foreach (var step in result.Steps)
        {
            Assert.DoesNotContain(step.Reply.AuditSurface, OrnamentReconstructionPacket.Participants(step.Reply.Surface));
            var body = Assert.IsType<Relation>(step.Reply.Surface.Right); var actualEvent = Assert.IsType<Relation>(body.Left);
            Assert.Same(source.Supplier.Source, actualEvent.Left); Assert.Same(source.Supplier.Revision, actualEvent.Right);
        }
    }

    [Fact]
    public void A_reply_for_another_request_query_or_supplier_revision_cannot_be_imported_as_the_same_observation()
    {
        var fixture = new Fixture(); var first = fixture.Supplier("SQ-PATH"); var second = fixture.Supplier("SQ-PATH");
        var request = new D.Request(S(), first.Supplier.Surface, fixture.Visible); var reply = first.Supplier.Reply(request);
        Assert.True(request.IsValid); Assert.True(reply.IsValid); Assert.Equal("Yes", reply.Answer);
        var differentRequest = new D.Request(S(), first.Supplier.Surface, fixture.Visible);
        Assert.False(new D.Response(differentRequest, reply.Surface).IsValid);
        var wrongQuery = new D.Request(fixture.Whole, first.Supplier.Surface, request.Surface);
        Assert.False(wrongQuery.IsValid); Assert.Equal("Blocked", first.Supplier.Reply(wrongQuery).Answer);
        Assert.Equal("Blocked", second.Supplier.Reply(request).Answer);

        var body = Assert.IsType<Relation>(reply.Surface.Right);
        var wrongRevision = new Relation(reply.Surface.Left,
            new Relation(new Relation(first.Supplier.Source, second.Supplier.Revision), body.Right));
        var stale = new D.Response(request, wrongRevision);
        Assert.False(stale.IsValid); Assert.Equal("Blocked", stale.Answer);
        Assert.Equal("Yes", reply.Answer);
    }

    [Fact]
    public void Closed_repertoire_consensus_can_be_wrong_about_an_absent_source_and_only_an_external_read_exposes_that()
    {
        var fixture = new Fixture(["SQ-SIDES", "X-CROSS"]); var source = fixture.Supplier("SQ-PATH");
        var result = fixture.Policy().Run(source.Supplier);
        Assert.True(result.Uniform); Assert.Empty(result.Steps); Assert.Empty(result.FinalMessage.Claims);
        Assert.Equal("known focus consensus", result.StopReason); Assert.Equal(0, source.Bank.EvaluatedReads);
        Assert.All(fixture.Projection.Indices(result.Possible), index => Assert.Equal("No", fixture.Projection.ReadAnswer(index, fixture.Whole)));
        Assert.Equal("Yes", source.Bank.Read(source.Input, fixture.Whole).Answer);
        Assert.DoesNotContain(source.Sample.Observation.Surface, OrnamentReconstructionPacket.Participants(result.Surface));
    }

    [Fact]
    public void Missing_mechanical_capability_does_not_create_a_mechanical_inference_from_an_unresolved_profile()
    {
        var fixture = new Fixture(["SQ-PATH", "SQ-SIDES"], mechanicalCapability: false);
        var source = fixture.Supplier("SQ-PATH"); var result = fixture.Policy().Run(source.Supplier);
        Assert.False(result.Uniform); Assert.Equal(fixture.Projection.Universe, result.Possible);
        Assert.Empty(result.Steps); Assert.Empty(result.FinalMessage.Claims);
        Assert.Equal("no informative advertised query", result.StopReason);
        Assert.Equal(0, source.Bank.EvaluatedReads);
        Assert.All(fixture.Projection.Indices(result.Possible), index => Assert.Equal("Open", fixture.Projection.ReadAnswer(index, fixture.Whole)));
    }

    [Fact]
    public void An_opaque_open_reply_preserves_alternatives_and_is_not_encoded_as_a_negative_claim()
    {
        var fixture = new Fixture(); var source = fixture.Supplier("SQ-PATH", hiddenMechanicalGroups: true);
        var result = fixture.Policy().Run(source.Supplier);
        var open = Assert.Single(result.Steps, step => step.Reply.Answer == "Open");
        Assert.True(open.Reply.IsValid); Assert.Equal(open.BeforePossible, open.AfterPossible);
        Assert.Equal(open.BeforeAdmitted, open.AfterAdmitted); Assert.Empty(E(open.Reply.No));
        Assert.False(Closed(open.Reply.Open));
        Assert.DoesNotContain(result.OriginalMessage.Claims, claim => ReferenceEquals(claim.Query.Surface, fixture.Whole.Surface));
        Assert.False(result.Uniform); Assert.Equal(2, fixture.Projection.Indices(result.Possible).Count);
        Assert.Contains(result.Steps, step => ReferenceEquals(step.Query.Surface, fixture.Whole.Surface));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Pruning_withdraws_redundant_hints_and_preserves_the_publicly_inferred_profile_without_source_goal_access(bool exhaustive)
    {
        var fixture = new Fixture(); var source = fixture.Supplier("SQ-PATH"); var policy = fixture.Policy();
        var result = policy.Run(source.Supplier, exhaustivePrune: exhaustive);
        Assert.True(result.Uniform); Assert.Equal(2, result.Steps.Count); Assert.Equal(2, result.OriginalMessage.HintCount);
        Assert.Single(result.PruneSteps); Assert.Single(result.FinalMessage.Claims);
        Assert.Same(fixture.Whole.Surface, result.FinalMessage.Claims[0].Query.Surface);
        Assert.Equal(fixture.Projection.Possible(result.FinalMessage.Claims), result.Possible);
        Assert.Equal(fixture.Projection.Admitted(result.FinalMessage.Claims), result.Admitted);
        Assert.True(policy.IsUniform(result.Possible)); Assert.NotEqual(BigInteger.Zero, result.Admitted);
        Assert.True(result.FinalMessage.NativeCost < result.OriginalMessage.NativeCost);
        foreach (var step in result.PruneSteps)
        {
            Assert.True(policy.IsUniform(step.Possible)); Assert.NotEqual(BigInteger.Zero, step.Admitted);
            Assert.Same(fixture.Visible.Surface, step.Removed.Query.Surface);
        }
        var publicEvidence = OrnamentReconstructionPacket.Participants(result.Surface);
        Assert.Contains(result.OriginalMessage.Surface, publicEvidence);
        Assert.DoesNotContain(source.Input.Surface, publicEvidence);
    }

    [Fact]
    public void Request_budget_exhaustion_preserves_the_partial_message_and_unsettled_focus()
    {
        var fixture = new Fixture(); var source = fixture.Supplier("SQ-PATH");
        var result = fixture.Policy().Run(source.Supplier, maxRequests: 1);
        Assert.False(result.Uniform); Assert.Equal("request budget", result.StopReason);
        Assert.Single(result.Steps); Assert.Single(result.FinalMessage.Claims);
        Assert.Equal(2, fixture.Projection.Indices(result.Possible).Count);
        Assert.Empty(result.PruneSteps); Assert.Equal(1, source.Bank.EvaluatedReads);
    }

    [Fact]
    public void Invalid_or_unfinished_focus_keeps_contract_evidence_distinct_from_an_empty_hypothesis_set()
    {
        var fixture = new Fixture(); var source = fixture.Supplier("SQ-PATH");
        var invalid = new R.Focus(new(S(fixture.Whole.Surface), new Relation(S(fixture.Visible.Surface), S(fixture.Whole.Surface))));
        var unfinished = new R.Focus(new(S(fixture.Whole.Surface),
            new Relation(StructuralConstraint.Open(fixture.Whole.Surface), S())));
        var blocked = new D.Policy(fixture.Projection, invalid).Run(source.Supplier);
        var open = new D.Policy(fixture.Projection, unfinished).Run(source.Supplier);
        Assert.Equal("blocked dialogue contract", blocked.StopReason); Assert.NotEmpty(E(blocked.Blocked));
        Assert.Equal("open dialogue contract", open.StopReason); Assert.Empty(E(open.Blocked)); Assert.NotEmpty(E(open.Open));
        Assert.Empty(blocked.Steps); Assert.Empty(open.Steps); Assert.False(blocked.Uniform); Assert.False(open.Uniform);
        Assert.Equal(0, source.Bank.EvaluatedReads);
        var emptyButValid = new D.Policy(fixture.Projection, [1], BigInteger.Zero).Run(source.Supplier);
        Assert.Equal("empty possible set", emptyButValid.StopReason);
        Assert.Empty(E(emptyButValid.Blocked)); Assert.Empty(E(emptyButValid.Open));
    }

    [Fact]
    public void Cached_query_decisions_preserve_the_public_decision_and_keep_distinct_asked_scopes_separate()
    {
        var fixture = new Fixture(); var policy = fixture.Policy();
        var first = Assert.IsType<D.Decision>(policy.Choose(fixture.Projection.Universe, []));
        var cached = Assert.IsType<D.Decision>(policy.Choose(fixture.Projection.Universe, []));
        Assert.Same(first, cached); Assert.Equal(1, policy.DecisionEvaluations); Assert.Equal(1, policy.CachedDecisions);
        var independentlyComputed = Assert.IsType<D.Decision>(fixture.Policy().Choose(fixture.Projection.Universe, []));
        Assert.Same(first.Query.Surface, independentlyComputed.Query.Surface);
        Assert.Equal(first.IfYes, independentlyComputed.IfYes); Assert.Equal(first.IfNo, independentlyComputed.IfNo);
        Assert.Equal(first.Gain, independentlyComputed.Gain); Assert.Equal(first.NativeQueryCost, independentlyComputed.NativeQueryCost);
        var afterAsked = Assert.IsType<D.Decision>(policy.Choose(fixture.Projection.Universe, [fixture.Visible]));
        Assert.Same(fixture.Whole.Surface, afterAsked.Query.Surface); Assert.Equal(2, policy.DecisionEvaluations);
        var renamed = new R.Query("same actual query", fixture.Visible.Surface);
        Assert.Same(afterAsked, policy.Choose(fixture.Projection.Universe, [renamed]));
    }

    private sealed class Fixture
    {
        internal R.Query Visible { get; }
        internal R.Query Whole { get; }
        internal R.Query[] Queries { get; }
        internal R.Input[] Inputs { get; }
        internal OrnamentReconstructionProjection Projection { get; }
        internal Fixture(string[]? families = null, bool mechanicalCapability = true)
        {
            var exemplar = Sample("SQ-PATH");
            var example = new R.Input("source-free exemplar", exemplar.Observation, exemplar.MechanicalGroups);
            Assert.True(R.TryDescribeGroup(example, Assert.IsType<Relation>(E(example.MechanicalGroups)[0]), out var pattern, out _));
            Visible = R.Query.Motif("one visible square", pattern); Whole = R.Query.WholePath("one whole square path", pattern);
            Queries = [Visible, Whole];
            Inputs = (families ?? ["SQ-PATH", "SQ-SIDES", "X-CROSS", "X-UNRELATED"]).Select(family =>
            {
                var sample = Sample(family); return new R.Input("receiver " + family, sample.Observation, sample.MechanicalGroups);
            }).ToArray();
            var bank = new R.Bank(Inputs, Queries); var context = new R.Context(Inputs, mechanicalCapability ? Queries : [Visible]);
            Projection = new(bank, context, Queries);
        }
        internal D.Policy Policy() => new(Projection, [1]);
        internal (D.Supplier Supplier, R.Bank Bank, R.Input Input, OrnamentReconstructionCases.Sample Sample) Supplier(
            string family, bool hiddenMechanicalGroups = false)
        {
            var sample = Sample(family); var input = new R.Input("concealed sender", sample.Observation,
                hiddenMechanicalGroups ? StructuralConstraint.Open() : sample.MechanicalGroups);
            var bank = new R.Bank([input], Queries); return (new(bank, input), bank, input, sample);
        }
    }
    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
