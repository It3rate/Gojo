using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;

namespace QualitativeExecution.Tests;

public sealed class StructuralEvidenceInquiryTests
{
    [Fact]
    public void Native_topology_questions_identify_five_examples_and_preserve_unadvertised_recipe_ambiguity()
    {
        foreach (var example in StructuralEvidenceCases.Build().Cases)
        {
            var receiver = example.CreateReceiver(); var supplier = example.CreateSupplier();
            foreach (var query in example.Queries) receiver.Receive(supplier.Request(receiver, query));
            Assert.Empty(E(receiver.Selection.Conflicts));
            Assert.Equal(example.ExpectedAmbiguity ? 2 : 1, receiver.Selection.Eligible.Count);
            Assert.All(receiver.Selection.Eligible, candidate => Assert.Empty(E(candidate.Refuted)));
            Assert.All(receiver.Responses, response =>
                Assert.Equal(1, E(response.Supported).Length + E(response.Refuted).Length));
            if (example.ExpectedAmbiguity) Assert.Null(receiver.Selection.Chosen);
        }
    }

    [Fact]
    public void Opposing_reports_of_the_same_event_stay_visible_and_do_not_force_a_candidate()
    {
        var example = StructuralEvidenceCases.Build().Cases[0];
        var receiver = example.CreateReceiver(); var query = example.Queries[0];
        var eventIdentity = new Placeholder();
        var positive = new StructuralEvidenceInquiry.Supplier(example.Source.Observation, example.Queries, revision: eventIdentity);
        var negative = new StructuralEvidenceInquiry.Supplier(example.Candidates[0].Observation, example.Queries, revision: eventIdentity);
        receiver.Receive(positive.Request(receiver, query));
        Assert.Single(receiver.Selection.Eligible);
        receiver.Receive(negative.Request(receiver, query));
        Assert.Equal(2, receiver.Responses.Count);
        Assert.Single(E(receiver.Selection.Conflicts));
        Assert.Empty(receiver.Selection.Eligible);
        Assert.All(receiver.Selection.Candidates, candidate => Assert.NotEmpty(E(candidate.Blocked)));
    }

    [Fact]
    public void A_candidate_with_unfinished_search_is_not_refuted_by_a_positive_supplier_answer()
    {
        var example = StructuralEvidenceCases.Build().Cases[0]; var query = example.Queries[0];
        var candidate = new StructuralEvidenceInquiry.Candidate("unsearched", example.Source.Observation,
            critic: new(example.Source.Observation, proposalCapacity: 0));
        var receiver = new StructuralEvidenceInquiry.Receiver([candidate], [query]);
        receiver.Receive(example.CreateSupplier().Request(receiver, query));
        var retained = Assert.Single(receiver.Selection.Eligible);
        Assert.NotEmpty(E(retained.Open)); Assert.Empty(E(retained.Refuted));
    }

    [Fact]
    public void A_lookalike_query_is_not_an_advertised_port()
    {
        var example = StructuralEvidenceCases.Build().Cases[0]; var receiver = example.CreateReceiver();
        var counterfeit = new StructuralEvidenceInquiry.Query("same description", example.Queries[0].Pattern);
        receiver.Receive(example.CreateSupplier().Request(receiver, counterfeit));
        Assert.Empty(receiver.Responses); Assert.NotEmpty(E(receiver.Rejected));
        Assert.Equal(example.Candidates.Count, receiver.Selection.Eligible.Count);
    }

    [Fact]
    public void A_reply_cannot_claim_both_presence_and_absence_under_one_request()
    {
        var example = StructuralEvidenceCases.Build().Cases[0]; var receiver = example.CreateReceiver();
        var original = example.CreateSupplier().Request(receiver, example.Queries[0]);
        var contradictoryPayload = new StructuralEvidenceInquiry.Response(original.Query, original.Request,
            original.Source, original.Revision, S(original.Query.Surface), S(original.Query.Surface), S(), original.Proof);
        receiver.Receive(contradictoryPayload);
        Assert.Empty(receiver.Responses); Assert.NotEmpty(E(receiver.Rejected));
    }

    [Fact]
    public void An_absent_receiver_capability_stays_blocked_even_if_its_pattern_is_supported()
    {
        var example = StructuralEvidenceCases.Build().Cases[0]; var query = example.Queries[0];
        var candidate = new StructuralEvidenceInquiry.Candidate("unavailable construction", example.Source.Observation, available: false);
        var receiver = new StructuralEvidenceInquiry.Receiver([candidate], [query]);
        receiver.Receive(example.CreateSupplier().Request(receiver, query));
        Assert.Empty(receiver.Selection.Eligible);
        var read = Assert.Single(receiver.Selection.Candidates);
        Assert.NotEmpty(E(read.Supported)); Assert.NotEmpty(E(read.Blocked));
    }

    [Fact]
    public void Transferred_policy_ranks_native_queries_without_reading_the_concealed_source()
    {
        var learned = EvidenceSelectionStudy.Build(new(TrainingEpisodes: 4, EvaluationEpisodes: 2, RetainedPerPolicy: 0)).Policy;
        var examples = StructuralEvidenceCases.Build().Cases;
        foreach (var example in examples)
        {
            var receiver = example.CreateReceiver(); var supplier = example.CreateSupplier();
            var before = learned.Coefficients.ToArray();
            foreach (var query in example.Queries)
            {
                var read = StructuralEvidenceInquiry.Features(receiver, query, learned.Basis);
                Assert.Equal(5, read.Values.Length);
                Assert.Equal(.5, read.Values[3]);
            }
            while (receiver.Selection.Eligible.Count > 1 && StructuralEvidenceInquiry.Available(receiver).Length > 0)
            {
                var next = StructuralEvidenceInquiry.Select(receiver, learned)!;
                receiver.Receive(supplier.Request(receiver, next));
            }
            Assert.Equal(before, learned.Coefficients);
            Assert.Equal(example.ExpectedAmbiguity ? 2 : 1, receiver.Selection.Eligible.Count);
        }
    }

    [Fact]
    public void Bidirectional_evaluation_checks_more_than_the_requested_question()
    {
        var examples = StructuralEvidenceCases.Build().Cases;
        foreach (var example in examples)
        {
            var receiver = example.CreateReceiver(); var supplier = example.CreateSupplier();
            foreach (var query in example.Queries) receiver.Receive(supplier.Request(receiver, query));
            foreach (var retained in receiver.Selection.Eligible)
            {
                var comparison = StructuralEvidenceInquiry.CompareObservations(example.Source.Induction, retained.Candidate.Critic);
                Assert.True(comparison.IsCompatibleAtVocabulary);
                Assert.NotEmpty(E(comparison.Supported));
            }
        }
        var triangleAgainstTee = StructuralEvidenceInquiry.CompareObservations(examples[0].Source.Induction,
            examples[2].Candidates[0].Induction);
        Assert.False(triangleAgainstTee.IsCompatibleAtVocabulary);
        Assert.NotEmpty(E(triangleAgainstTee.Refuted));
    }

    [Fact]
    public void An_unfinished_candidate_inventory_cannot_certify_bidirectional_vocabulary_agreement()
    {
        var example = StructuralEvidenceCases.Build().Cases[0];
        var unfinished = new QualitativeMotifInduction(example.Source.Observation, proposalCapacity: 0);
        var comparison = StructuralEvidenceInquiry.CompareObservations(example.Source.Induction, unfinished);
        Assert.False(comparison.IsCompatibleAtVocabulary);
        Assert.NotEmpty(E(comparison.Open));
        Assert.Empty(E(comparison.Refuted));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
