using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Encoding.Structures;

namespace QualitativeExecution.Tests;

public sealed class EvidenceSelectionAuditTests
{
    private static readonly Lazy<EvidenceSelectionStudy> SmallStudy = new(() =>
        EvidenceSelectionStudy.Build(new(8, 4, 731, 4, 0)));

    [Fact]
    public void Concealed_source_choice_cannot_change_the_policy_before_a_public_reply()
    {
        var policy = SmallStudy.Value.Policy;
        var a = EvidenceSelectionStudy.CreateScenario("contiguous-block", 3, 0);
        var b = EvidenceSelectionStudy.CreateScenario("contiguous-block", 3, 3);
        Assert.Empty(a.Receiver.Publications); Assert.Empty(b.Receiver.Publications);
        Assert.Equal(a.Receiver.Ports.Count, b.Receiver.Ports.Count);
        for (var index = 0; index < a.Receiver.Ports.Count; index++)
        {
            var x = policy.Features(a.Receiver, a.Receiver.Ports[index]);
            var y = policy.Features(b.Receiver, b.Receiver.Ports[index]);
            Assert.Equal(x.Values, y.Values);
            Assert.Equal(policy.Predict(x), policy.Predict(y));
        }
        Assert.Equal(Index(a.Receiver.Ports, policy.Select(a.Receiver)),
            Index(b.Receiver.Ports, policy.Select(b.Receiver)));
    }

    [Fact]
    public void Display_names_do_not_change_an_actual_advertised_role_read()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("reflected-pair", 2, 1);
        var original = scenario.Receiver; var policy = SmallStudy.Value.Policy;
        var ports = original.Ports.Select(port => new EvidenceSelectionStudy.EvidencePort(
            "A completely different visible label", port.Roles)).ToArray();
        var renamed = new EvidenceSelectionStudy.Receiver(original.Engine,
            original.Forms.Select(form => form with { Name = "Unrelated label" }).ToArray(),
            original.Context, ports);
        for (var index = 0; index < ports.Length; index++)
            Assert.Equal(policy.Features(original, original.Ports[index]).Values,
                policy.Features(renamed, ports[index]).Values);
        Assert.Equal(Index(original.Ports, policy.Select(original)), Index(ports, policy.Select(renamed)));
    }

    [Fact]
    public void A_port_copy_with_the_same_role_scope_cannot_acquire_a_reply()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("single-exception", 0, 0);
        var original = scenario.Receiver.Ports[0];
        var copy = new EvidenceSelectionStudy.EvidencePort(original.Name, original.Roles);
        var response = scenario.Request(copy);
        var before = scenario.Receiver.Selection;
        Assert.NotEmpty(E(response.Blocked)); Assert.Empty(response.Publications);
        Assert.Same(before, scenario.Receiver.Receive(response));
        Assert.Empty(scenario.Receiver.Publications);
        Assert.NotEmpty(E(scenario.Receiver.Rejected));
    }

    [Fact]
    public void Another_receiver_cannot_import_a_reply_by_sharing_the_advertised_ports()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("single-exception", 0, 0);
        var original = scenario.Receiver;
        var other = new EvidenceSelectionStudy.Receiver(original.Engine, original.Forms,
            original.Context, original.Ports);
        var response = scenario.Request(original.Ports[0]);
        var before = other.Selection;
        Assert.Same(before, other.Receive(response));
        Assert.Empty(other.Publications); Assert.NotEmpty(E(other.Rejected));
    }

    [Fact]
    public void Actual_response_publications_are_connected_to_the_native_witness_and_cannot_be_replayed()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("single-exception", 0, 0);
        var receiver = scenario.Receiver; var port = receiver.Ports[0];
        var actual = scenario.Request(port);
        Assert.All(actual.Publications, publication =>
            Assert.Contains(Reachable(actual.Surface), element => ReferenceEquals(element, publication.Surface)));
        var admitted = receiver.Receive(actual); var count = receiver.Publications.Count;
        Assert.Equal(actual.Publications.Count, count);
        Assert.Same(admitted, receiver.Receive(actual));
        Assert.Equal(count, receiver.Publications.Count); Assert.NotEmpty(E(receiver.Rejected));
    }

    [Fact]
    public void Open_replies_do_not_turn_the_preferred_completion_into_supported_source_evidence()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("contiguous-block", 1, 2, "open");
        var episode = EvidenceSelectionStudy.Run(scenario, "learned", SmallStudy.Value.Policy,
            4, new Random(22), "open");
        Assert.False(episode.Metric.Identified);
        Assert.True(episode.Final.Eligible.Count > 1);
        Assert.All(episode.Final.Candidates, item =>
        {
            Assert.Empty(E(item.Support)); Assert.Empty(E(item.SampleSupport));
            Assert.NotEmpty(E(item.Open));
        });
    }

    [Fact]
    public void Incompatible_reports_remain_visible_after_all_actual_ports_are_read()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("reflected-pair", 2, 1, "conflict");
        foreach (var port in scenario.Receiver.Ports)
            scenario.Receiver.Receive(scenario.Request(port));
        Assert.Null(scenario.Receiver.Selection.Chosen);
        Assert.Empty(scenario.Receiver.Selection.Eligible);
        Assert.All(scenario.Receiver.Selection.Candidates, item =>
        {
            Assert.NotEmpty(E(item.InScopeConflicts)); Assert.True(item.HasConflict);
        });
        Assert.True(scenario.Receiver.Publications.Select(pub => pub.Source)
            .Distinct(ReferenceEqualityComparer.Instance).Count() > 1);
    }

    [Fact]
    public void Native_training_rows_reconstruct_the_model_without_the_training_helper()
    {
        var policy = SmallStudy.Value.Policy;
        var reconstructed = EvidenceSelectionStudy.LearnedPolicy.FromSurface(policy.Surface);
        Assert.Equal(policy.Coefficients, reconstructed.Coefficients);
        Assert.Equal(policy.TrainingRows, reconstructed.TrainingRows);
        var scenario = EvidenceSelectionStudy.CreateScenario("contiguous-block", 4, 2);
        Assert.Same(policy.Select(scenario.Receiver), reconstructed.Select(scenario.Receiver));
        foreach (var port in scenario.Receiver.Ports)
            Assert.Equal(policy.Predict(policy.Features(scenario.Receiver, port)),
                reconstructed.Predict(reconstructed.Features(scenario.Receiver, port)));
    }

    [Fact]
    public void Persisted_native_policy_reconstructs_the_same_coefficients_with_fresh_graph_identity()
    {
        var policy = SmallStudy.Value.Policy;
        var json = GojoStructureCompactJson.Serialize(
            GojoStructureCompactJson.FromElement("evidence-policy-audit", policy.Surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var world));
        var surface = Assert.IsType<Relation>(world.Root);
        Assert.NotSame(policy.Surface, surface);
        var restored = EvidenceSelectionStudy.LearnedPolicy.FromSurface(surface);
        Assert.Equal(policy.Coefficients, restored.Coefficients);
        var scenario = EvidenceSelectionStudy.CreateScenario("reflected-pair", 1, 2);
        Assert.Same(policy.Select(scenario.Receiver), restored.Select(scenario.Receiver));
    }

    [Fact]
    public void Full_source_evaluation_remains_outside_receiver_and_feature_surfaces()
    {
        var scenario = EvidenceSelectionStudy.CreateScenario("contiguous-block", 1, 2);
        var receiver = scenario.Receiver; var policy = SmallStudy.Value.Policy;
        var before = policy.Features(receiver, receiver.Ports[0]);
        var evaluation = scenario.Evaluate();
        var premise = (Relation)evaluation.Candidates[0].Surface.Left;
        var sourceAndContext = (Relation)premise.Right;
        var sourcePublications = E((Relation)sourceAndContext.Left);
        Assert.NotEmpty(sourcePublications);
        var feature = policy.Features(receiver, receiver.Ports[0]);
        Assert.Equal(before.Values, feature.Values);
        Assert.Empty(receiver.Publications);
        foreach (var publication in sourcePublications)
        {
            Assert.DoesNotContain(Reachable(receiver.Surface), element => ReferenceEquals(element, publication));
            Assert.DoesNotContain(Reachable(feature.Surface), element => ReferenceEquals(element, publication));
        }
    }

    [Fact]
    public void Identification_requires_the_independent_full_source_read()
    {
        var policy = SmallStudy.Value.Policy;
        foreach (var family in new[] { "contiguous-block", "reflected-pair" })
        for (var truth = 0; truth < 4; truth++)
        {
            var scenario = EvidenceSelectionStudy.CreateScenario(family, truth, truth);
            var episode = EvidenceSelectionStudy.Run(scenario, "learned", policy, 4, new Random(31));
            if (!episode.Metric.Identified) continue;
            var candidate = Assert.Single(episode.Final.Eligible);
            Assert.Contains(episode.Evaluation.Eligible,
                item => ReferenceEquals(item.Hypothesis, candidate.Hypothesis));
        }
        var incapable = EvidenceSelectionStudy.Run(
            EvidenceSelectionStudy.CreateScenario("reflected-pair", 0, 2, "incapable"),
            "learned", policy, 4, new Random(31));
        Assert.False(incapable.Metric.Identified); Assert.Empty(incapable.Final.Eligible);
        Assert.NotEmpty(incapable.Final.Candidates);
        Assert.All(incapable.Final.Candidates, item => Assert.NotEmpty(E(item.Blocked)));
    }

    [Fact]
    public void A_unique_completion_from_an_incomplete_library_is_not_successful_identification()
    {
        var policy = SmallStudy.Value.Policy; EvidenceSelectionStudy.Episode? misleading = null;
        foreach (var family in new[] { "contiguous-block", "reflected-pair" })
        for (var truth = 0; truth < 4 && misleading is null; truth++)
        {
            var scenario = EvidenceSelectionStudy.CreateScenario(family, 1, truth, "unavailable");
            var episode = EvidenceSelectionStudy.Run(scenario, "learned", policy, 4, new Random(32));
            Assert.False(episode.Metric.Identified);
            Assert.Empty(episode.Evaluation.Eligible);
            if (episode.Metric.UniqueCompatible) misleading = episode;
        }
        Assert.NotNull(misleading);
        Assert.Single(misleading.Final.Eligible);
        Assert.False(misleading.Metric.Identified);
        Assert.Contains(misleading.Final.Candidates, item => E(item.Blocked).Length > 0);
    }

    private static int Index(IReadOnlyList<EvidenceSelectionStudy.EvidencePort> ports,
        EvidenceSelectionStudy.EvidencePort? selected)
    {
        for (var index = 0; index < ports.Count; index++)
            if (ReferenceEquals(ports[index], selected)) return index;
        return -1;
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
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
