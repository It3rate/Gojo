using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionDialogue;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionDialogueTests
{
    [Fact]
    public void Query_choice_is_blind_to_supplier_and_adaptive_results_preserve_the_inferred_focus()
    {
        var vocabulary = OrnamentReconstructionVocabulary.All.Select(entry => entry.Query).ToArray();
        var specs = new[] { "SQ-PATH", "SQ-SIDES", "X-JOIN", "X-CROSS", "ZIGZAG", "MEANDER" }.Select(Spec).ToArray();
        var inputs = specs.Select(spec => Input(spec.Create())).ToArray();
        var bank = new R.Bank(inputs, vocabulary); var context = new R.Context(inputs, vocabulary);
        var projection = new OrnamentReconstructionProjection(bank, context, vocabulary);
        var focus = new R.Focus(vocabulary, vocabulary.Where(query => query.Kind is "visible motif" or "mechanical path" or "crossing").ToArray());
        var policy = new D.Policy(projection, focus); var results = new List<D.Result>();
        foreach (var spec in specs)
        {
            var source = Input(spec.Create()); var sourceBank = new R.Bank([source], vocabulary);
            var result = policy.Run(new(sourceBank, source), exhaustivePrune: true); results.Add(result);
            Assert.True(result.Uniform); Assert.NotEqual(0, result.Admitted);
            Assert.True(result.FinalMessage.NativeCost <= result.OriginalMessage.NativeCost);
            Assert.Equal(projection.Possible(result.FinalMessage.Claims), result.Possible);
            Assert.Equal(projection.Admitted(result.FinalMessage.Claims), result.Admitted);
            foreach (var index in projection.Indices(result.Possible))
                foreach (var query in vocabulary.Where(query => FiniteLaw.Entries(focus.Selected).Any(actual => ReferenceEquals(actual, query.Surface))))
                    Assert.Equal(sourceBank.Read(source, query).Answer, projection.ReadAnswer(index, query));
        }
        Assert.All(results, result => Assert.NotEmpty(result.Steps));
        Assert.All(results, result => Assert.Same(results[0].Steps[0].Query.Surface, result.Steps[0].Query.Surface));
    }

    [Fact]
    public void Supplier_observation_and_certifying_proofs_are_outside_all_receiver_roots()
    {
        var (projection, query, source, sourceBank) = BinaryCase(); var supplier = new D.Supplier(sourceBank, source);
        var result = new D.Policy(projection, [0]).Run(supplier);
        Assert.True(result.Uniform); Assert.NotEmpty(result.Steps);
        foreach (var root in new IElement[] { result.Surface, result.FinalMessage.Surface, supplier.Surface, result.Steps[0].Reply.Surface })
        {
            var reachable = R.Participants(root);
            Assert.DoesNotContain(source.Surface, reachable); Assert.DoesNotContain(source.Observation.Surface, reachable);
            Assert.DoesNotContain(sourceBank.Surface, reachable); Assert.DoesNotContain(supplier.AuditSurface, reachable);
            Assert.DoesNotContain(result.Steps[0].Reply.AuditSurface, reachable);
        }
        Assert.Contains(source.Surface, R.Participants(result.AuditSurface));
        Assert.Same(query.Surface, result.Steps[0].Request.Surface.Right);
        Assert.Same(result.Steps[0].Request.Surface, result.Steps[0].Reply.Surface.Left);
    }

    [Fact]
    public void An_opaque_open_reply_keeps_both_candidates_without_inventing_a_negative_claim()
    {
        var (projection, _, source, _) = BinaryCase();
        var sourceBank = new R.Bank([source], projection.Queries, new(ProposalCapacity: 0, RetainedCapacity: 0));
        var result = new D.Policy(projection, [0]).Run(new(sourceBank, source));
        Assert.False(result.Uniform); var step = Assert.Single(result.Steps);
        Assert.Equal("Open", step.Reply.Answer); Assert.Equal(step.BeforePossible, step.AfterPossible);
        Assert.Empty(result.FinalMessage.Claims); Assert.Equal(projection.Universe, result.Possible);
        Assert.Equal("no informative advertised query", result.StopReason);
    }

    [Fact]
    public void Request_query_and_opaque_event_aliases_are_authenticated()
    {
        var (projection, query, source, sourceBank) = BinaryCase();
        var first = new D.Supplier(sourceBank, source); var second = new D.Supplier(sourceBank, source);
        var request = new D.Request(StructuralConstraint.Closed(), first.Surface, query);
        var response = first.Reply(request); Assert.True(response.IsValid);
        var wrongEvent = second.Reply(request); Assert.False(wrongEvent.IsValid); Assert.Equal("Blocked", wrongEvent.Answer);
        var anotherQuery = R.Query.Motif("lookalike, different actual query", new(source.Observation.Frame,
            ((Relation)query.Pattern!).Right as Relation ?? throw new InvalidOperationException()));
        var malformedRequest = new D.Request(query, first.Surface, new Relation(new Relation(StructuralConstraint.Closed(), first.Surface), anotherQuery.Surface));
        Assert.False(malformedRequest.IsValid); Assert.Equal("Blocked", first.Reply(malformedRequest).Answer);
        var rebound = new D.Response(new D.Request(StructuralConstraint.Closed(), second.Surface, query), response.Surface);
        Assert.False(rebound.IsValid);
    }

    private static (OrnamentReconstructionProjection Projection, R.Query Query, R.Input Source, R.Bank SourceBank) BinaryCase()
    {
        var square = Input(Spec("SQ-PATH").Create()); var cross = Input(Spec("X-CROSS").Create());
        Assert.True(R.TryDescribeGroup(square, (Relation)FiniteLaw.Entries(square.MechanicalGroups)[0], out var shape, out _));
        var query = R.Query.Motif("closed axial contour", shape); var inputs = new[] { square, cross };
        var bank = new R.Bank(inputs, [query]); var projection = new OrnamentReconstructionProjection(bank, new(inputs, [query]), [query]);
        var source = Input(Spec("SQ-PATH").Create()); return (projection, query, source, new([source], [query]));
    }
    private static OrnamentReconstructionCases.Spec Spec(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1);
    private static R.Input Input(OrnamentReconstructionCases.Sample sample) => new(sample.Spec.Name, sample.Observation, sample.MechanicalGroups);
}
