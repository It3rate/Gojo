using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;

namespace QualitativeExecution.Tests;

public sealed class EvidenceSelectionTests
{
    private static readonly Lazy<EvidenceSelectionStudy> Study = new(() =>
        EvidenceSelectionStudy.Build(new(TrainingEpisodes: 24, EvaluationEpisodes: 16)));

    [Fact]
    public void Training_uses_different_source_construction_families_before_variants_are_generated()
    {
        var metrics = Study.Value.Metrics;
        Assert.Equal(new[] { "period-four", "single-exception" }, metrics.Where(row => row.Split == "training")
            .Select(row => row.Family).Distinct().Order().ToArray());
        Assert.Equal(new[] { "contiguous-block", "reflected-pair" }, metrics.Where(row => row.Split == "held-out")
            .Select(row => row.Family).Distinct().Order().ToArray());
        Assert.Equal(4, Study.Value.Summaries.Count);
        Assert.All(Study.Value.Summaries, row => Assert.Equal(16, row.Episodes));
        Assert.True(Study.Value.Policy.TrainingRows > 0);
    }

    [Fact]
    public void Learned_policy_and_simple_disagreement_rule_acquire_useful_evidence_on_held_out_layouts()
    {
        var summaries = Study.Value.Summaries.ToDictionary(row => row.Policy);
        Assert.True(summaries["learned"].Identified > summaries["fixed"].Identified);
        Assert.True(summaries["disagreement"].Identified > summaries["fixed"].Identified);
        Assert.True(summaries["learned"].MeanRequests < summaries["fixed"].MeanRequests);
        Assert.True(summaries["learned"].Identified >= 12,
            string.Join("; ", summaries.Values.Select(row => $"{row.Policy}: {row.Identified}/{row.Episodes}, requests {row.MeanRequests:F2}")));
    }

    [Fact]
    public void Learned_coefficients_and_ranking_are_reconstructible_from_native_training_witnesses()
    {
        var policy = Study.Value.Policy;
        var restored = EvidenceSelectionStudy.LearnedPolicy.FromSurface(policy.Surface);
        Assert.Equal(policy.TrainingRows, restored.TrainingRows);
        Assert.Equal(policy.Coefficients, restored.Coefficients);
        var scenario = EvidenceSelectionStudy.CreateScenario("contiguous-block", 0, 2);
        Assert.Same(policy.Select(scenario.Receiver), restored.Select(scenario.Receiver));
        Assert.DoesNotContain(Reachable(policy.Surface), element => element is Carrier or CarrierLocation);
    }

    [Fact]
    public void Acquisition_witnesses_retain_before_actual_advertised_port_reply_and_after()
    {
        var episode = Study.Value.Episodes.First(row => row.Policy == "learned" && row.Metric.Split == "held-out");
        Assert.NotEmpty(episode.Steps);
        foreach (var step in episode.Steps)
        {
            Assert.Contains(episode.Receiver.Ports, actual => ReferenceEquals(actual, step.Port));
            Assert.Contains(Reachable(step.Surface), element => ReferenceEquals(element, step.Before.Surface));
            Assert.Contains(Reachable(step.Surface), element => ReferenceEquals(element, step.After.Surface));
            Assert.Contains(Reachable(step.Features), element => ReferenceEquals(element, step.Port.Surface));
            Assert.All(step.Publications, pub => Assert.All(E(pub.Values).Cast<Relation>(), row =>
                Assert.Contains(E(step.Port.Roles), role => ReferenceEquals(role, row.Left))));
            Assert.True(step.After.Eligible.Count <= step.Before.Eligible.Count);
        }
        Assert.NotSame(episode.Final.Surface, episode.Evaluation.Surface);
        Assert.True(episode.Metric.Identified);
        Assert.Contains(episode.Evaluation.Eligible, row => ReferenceEquals(row.Hypothesis, episode.Final.Chosen));
    }

    [Fact]
    public void Unreported_word_values_remain_open_and_cannot_become_success_by_stopping()
    {
        var episode = Study.Value.Episodes.Single(row => row.Metric.Split == "open");
        Assert.Equal(4, episode.Final.Eligible.Count);
        Assert.False(episode.Metric.Identified); Assert.True(episode.Metric.Exhausted);
        Assert.All(episode.Final.Candidates, candidate => Assert.NotEmpty(E(candidate.SampleOpen)));
        Assert.All(episode.Steps, step => Assert.All(step.Publications, pub =>
            Assert.All(E(pub.Values).Cast<Relation>(), row => Assert.False(Closed((Relation)row.Right)))));
    }

    [Fact]
    public void Same_event_supplier_conflict_and_missing_capability_stay_visible()
    {
        var conflict = Study.Value.Episodes.Single(row => row.Metric.Split == "conflict");
        Assert.False(conflict.Metric.Identified); Assert.Null(conflict.Final.Chosen);
        Assert.True(conflict.Metric.ConflictCount > 0);
        Assert.All(conflict.Final.Candidates, candidate => Assert.NotEmpty(E(candidate.InScopeConflicts)));
        var incapable = Study.Value.Episodes.Single(row => row.Metric.Split == "incapable");
        Assert.Empty(incapable.Steps); Assert.False(incapable.Metric.Identified);
        Assert.Empty(incapable.Final.Eligible); Assert.Equal(4, incapable.Metric.BlockedCount);
        Assert.All(incapable.Final.Candidates, candidate => Assert.NotEmpty(E(candidate.Blocked)));
    }

    [Fact]
    public void Chosen_program_can_execute_and_its_exposed_actual_strokes_realize_the_accepted_span_samples()
    {
        var episode = Study.Value.Episodes.First(row => row.Policy == "learned" && row.Metric.Identified);
        var form = episode.Forms.Single(form => ReferenceEquals(form.Hypothesis, episode.Final.Chosen));
        var execution = new UnitArrangementNode("Audited learned selection", [form.Program], form.Program, S());
        Assert.True(execution.Current.Execution.IsAtBoundary);
        var roleStrokes = S(E(form.RoleSelectors).Cast<Relation>().Select(selector =>
        {
            var port = E(execution.Current.Ports).Cast<Relation>().Single(actual => ReferenceEquals(actual.Left, selector.Left));
            var cursor = (Relation)((Relation)port.Right).Right; var boundary = (Relation)cursor.Right;
            var receipt = (Relation)boundary.Right;
            Assert.True(execution.Drawing.TryReadAccepted(receipt, out _, out var added));
            var sourceStroke = E(added).Single();
            var target = E(execution.Current.Join.StrokeMap).Cast<Relation>()
                .Single(row => ReferenceEquals(row.Left, sourceStroke)).Right;
            return (IElement)new Relation(selector.Left, target);
        }).ToArray());
        EquivalenceRevisionStudy.ReadSpans(execution.Current.Join.Graph, roleStrokes, out var values);
        Assert.Equal(episode.Roles.Length, E(values).Length);
        var actual = new ProvisionalEquivalence.Publication(new Placeholder(), new Placeholder(), values);
        var verified = episode.Receiver.Engine.Assess([form.Hypothesis], [actual], episode.Context);
        Assert.Single(verified.Eligible);
        Assert.Empty(E(verified.Candidates.Single().SampleRefuted));
        Assert.NotEmpty(E(verified.Candidates.Single().SampleSupport));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (!seen.Add(element)) continue; yield return element;
            if (element is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}
