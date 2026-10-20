using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class StructuralEvidenceStudyTests
{
    private static readonly Lazy<StructuralEvidenceStudy> Study = new(StructuralEvidenceStudy.Build);

    [Fact]
    public void Four_policies_share_the_same_cases_and_keep_the_unanswerable_recipe_open()
    {
        var study = Study.Value;
        Assert.Equal(27, study.Episodes.Count);
        Assert.Equal(4, study.Summaries.Count);
        foreach (var summary in study.Summaries)
        {
            Assert.Equal(6, summary.Episodes); Assert.Equal(5, summary.Resolved);
            Assert.Equal(1, summary.Open); Assert.Equal(0, summary.Conflicted);
        }
        foreach (var episode in study.Episodes.Where(episode => episode.Control is null))
        {
            if (episode.Case.ExpectedAmbiguity)
            { Assert.True(episode.CorrectAmbiguity); Assert.False(episode.CorrectAtVocabulary); }
            else Assert.True(episode.CorrectAtVocabulary);
        }
    }

    [Fact]
    public void The_transferred_weights_are_exactly_the_previous_span_training_recipe()
    {
        var reference = EvidenceSelectionStudy.Build(new(48, 1, 731, 4, 0)).Policy;
        Assert.Equal(reference.TrainingRows, Study.Value.Policy.TrainingRows);
        Assert.Equal(reference.Coefficients, Study.Value.Policy.Coefficients);
        foreach (var episode in Study.Value.Episodes.Where(episode => episode.Control is null && episode.Policy == "transferred"))
            Assert.All(episode.Steps, step =>
                Assert.Equal(StructuralEvidenceInquiry.Predict(step.Features, Study.Value.Policy), step.Prediction));
    }

    [Fact]
    public void Broader_questions_are_available_to_every_policy_but_do_not_distinguish_candidates()
    {
        foreach (var example in Study.Value.Cases.Cases.Where(example => !example.ExpectedAmbiguity))
        {
            var episodes = Study.Value.Episodes.Where(episode => episode.Control is null && ReferenceEquals(episode.Case, example)).ToArray();
            Assert.Equal(4, episodes.Length);
            foreach (var episode in episodes)
            {
                Assert.Equal(example.Queries.Count + 1, episode.Receiver.Queries.Count);
                var fresh = new StructuralEvidenceInquiry.Receiver(episode.Receiver.Candidates, episode.Receiver.Queries,
                    episode.Receiver.RelevantQueries);
                var broader = StructuralEvidenceInquiry.Features(fresh, fresh.Queries[0], Study.Value.Policy.Basis);
                Assert.Equal(0, broader.Values[1]); Assert.Equal(0, broader.Values[2]);
            }
            Assert.Same(episodes[0].Receiver.Queries[0], episodes[0].Steps[0].Query);
        }
    }

    [Fact]
    public void Unsearched_conflicted_and_unavailable_controls_remain_distinct()
    {
        var controls = Study.Value.Episodes.Where(episode => episode.Control is not null).ToArray();
        var unsearched = Assert.Single(controls, episode => episode.Control == "unsearched");
        Assert.False(unsearched.CorrectAtVocabulary); Assert.False(unsearched.HasConflict);
        Assert.True(unsearched.Final.Eligible.Count > 1);
        Assert.All(unsearched.Steps, step => Assert.NotEmpty(E(step.Response.Open)));

        var conflict = Assert.Single(controls, episode => episode.Control == "conflict");
        Assert.False(conflict.CorrectAtVocabulary); Assert.True(conflict.HasConflict);
        Assert.Empty(conflict.Final.Eligible); Assert.Equal(2, conflict.Receiver.Responses.Count);
        Assert.Same(conflict.Steps[0].Response.Revision, conflict.Steps[1].Response.Revision);
        Assert.NotEmpty(E(conflict.Steps[0].Response.Supported));
        Assert.NotEmpty(E(conflict.Steps[1].Response.Refuted));

        var incapable = Assert.Single(controls, episode => episode.Control == "incapable");
        Assert.False(incapable.CorrectAtVocabulary); Assert.True(incapable.HasBlocked);
        Assert.False(incapable.HasConflict); Assert.Empty(incapable.Steps);
        Assert.Empty(incapable.Final.Eligible);
    }

    [Fact]
    public void Supplier_audit_and_independent_evaluations_do_not_become_receiver_inputs()
    {
        foreach (var episode in Study.Value.Episodes)
        {
            var publicRead = Reachable(episode.Receiver.Surface).ToArray();
            Assert.DoesNotContain(publicRead, element => ReferenceEquals(element, episode.SupplierAudit));
            Assert.DoesNotContain(publicRead, element => ReferenceEquals(element, episode.Source.Observation.Surface));
            Assert.All(episode.Evaluations, evaluation =>
                Assert.DoesNotContain(publicRead, element => ReferenceEquals(element, evaluation.Surface)));
            Assert.Equal(episode.Receiver.Candidates.Count, episode.Evaluations.Count);
        }
    }

    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue;
            yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
