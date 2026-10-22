using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Gojo.Patterns;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;
using Weighting = Gojo.Experiments.QualitativeFrieze.Composition.ShapeEvidenceWeighting;

namespace QualitativeExecution.Tests;

public sealed class ShapeEvidenceWeightingTests
{
    private static readonly Lazy<IReadOnlyList<RelationalShapeCorpus.Case>> Corpus = new(RelationalShapeCorpus.Cases);

    [Fact]
    public void Neutral_channels_preserve_uniform_pool_weights_and_the_original_tie_break()
    {
        var fixture = Freeze("R006", 8);
        var result = Weighting.Read(fixture.Pool, Weighting.Plans.Single(plan => plan.Id == "uniform-flat"));
        Assert.NotEmpty(result.Steps);
        Assert.Same(fixture.Pool.Baseline, result.Selected);
        Assert.Equal(fixture.Pool.Alternatives.Count, result.Tied.Count);
        var expected = new Stats.Ratio(1, fixture.Pool.Alternatives.Count);
        Assert.All(result.Prior.Rows, row => Assert.Equal(expected, row.Probability));
        Assert.All(result.Posterior.Rows, row => Assert.Equal(expected, row.Probability));
        Assert.All(result.Steps, step => Assert.All(step.Likelihoods.Values, value => Assert.Equal(new Stats.Ratio(1, 2), value)));
    }

    [Fact]
    public void Every_estimated_law_reads_the_same_frozen_models_and_observation_sources()
    {
        var fixture = Freeze("R006", 8);
        var results = Weighting.Plans.Select(plan => Weighting.Read(fixture.Pool, plan)).ToArray();
        foreach (var result in results)
        {
            Assert.Same(fixture.Pool.Alternatives, result.Alternatives);
            Assert.Same(fixture.Pool.Baseline, result.Baseline);
            Assert.Equal(results[0].Steps.Select(step => step.Family.Id), result.Steps.Select(step => step.Family.Id));
            Assert.Equal(fixture.Pool.Alternatives.Select(item => item.Id), result.Alternatives.Select(item => item.Id));
            Assert.True(result.Posterior.Complete);
        }
    }

    [Fact]
    public void Repeated_routes_and_uniform_scale_variants_gain_no_additional_model_mass()
    {
        var fixture = Freeze("R006", 8);
        Assert.True(fixture.Pool.Alternatives.Count < fixture.Trials.Count);
        Assert.Contains(fixture.Pool.Alternatives, item => item.Routes.Count > 1);
        var repeated = fixture.Trials.Concat(new[] { fixture.Trials[0], fixture.Trials[0] }).ToArray();
        var rereadPool = Weighting.Freeze(fixture.Packet.Message, fixture.Search, repeated);
        Assert.Equal(fixture.Pool.Alternatives.Count, rereadPool.Alternatives.Count);
        var flat = Weighting.Plans.Single(plan => plan.Id == "uniform-flat");
        var original = Weighting.Read(fixture.Pool, flat); var replay = Weighting.Read(rereadPool, flat);
        Assert.Equal(original.Posterior.Rows.Select(row => row.Probability), replay.Posterior.Rows.Select(row => row.Probability));
        Assert.Equal(original.Selected.Id, replay.Selected.Id);
    }

    [Fact]
    public void Shape_keys_ignore_common_unmeasured_scale_but_preserve_aspect_ratio()
    {
        var source = Corpus.Value.Single(item => item.Id == "R006");
        var packet = RelationalShapeExperiment.Publish(JointShapeDescription.Analyze(source.Source));
        var message = packet.Message;
        var inference = RelationalSpatialEvidence.Infer(message, packet.Spatial, S(), 0);
        Assert.True(inference.Complete); var layout = inference.Layout!;
        JointShapeReconstruction.Candidate Sample(int x, int y) => JointShapeReconstruction.Propose(layout, 0,
            construction: JointShapeReconstruction.Construction(layout,
                RelationalShapeSearch.Placement(layout, [x], [y]), S(), S(), inference.Witness));
        var square = Sample(1, 1); var largerSquare = Sample(3, 3); var rectangle = Sample(3, 1);
        Assert.NotEqual(RelationalShapeSearch.Signature(message, square), RelationalShapeSearch.Signature(message, largerSquare));
        Assert.Equal(Weighting.SampleKey(message, square), Weighting.SampleKey(message, largerSquare));
        Assert.NotEqual(Weighting.SampleKey(message, square), Weighting.SampleKey(message, rectangle));
    }

    [Fact]
    public void Compactness_changes_prior_shares_without_adding_observation_events()
    {
        var fixture = Freeze("R022", 8);
        Assert.True(fixture.Pool.Alternatives.Select(item => Weighting.CompactCost(item.Trial)).Distinct().Count() > 1);
        var equal = Weighting.Read(fixture.Pool, new("equal-test", "Equal", 8, 8, 8, 16));
        var compact = Weighting.Read(fixture.Pool, new("compact-test", "Compact", 8, 8, 8, 16, true));
        Assert.False(equal.Prior.Rows.Select(row => row.Probability).SequenceEqual(compact.Prior.Rows.Select(row => row.Probability)));
        Assert.Equal(equal.Prior.Rows.Select(row => row.Probability), equal.Posterior.Rows.Select(row => row.Probability));
        Assert.Equal(compact.Prior.Rows.Select(row => row.Probability), compact.Posterior.Rows.Select(row => row.Probability));
        Assert.Empty(equal.Prior.Events); Assert.Empty(compact.Prior.Events);
        Assert.Equal(equal.Steps.Count, compact.Steps.Count);
        Assert.Equal(equal.Steps.Select(step => step.Family.Port), compact.Steps.Select(step => step.Family.Port));
        Assert.Equal(equal.Posterior.Events.Length, compact.Posterior.Events.Length);
    }

    [Fact]
    public void Native_weighted_state_rereads_after_independent_transport()
    {
        var result = Weighting.Read(Freeze("R006", 6).Pool, Weighting.Plans[0]);
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("weighted-test", result.State));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var decoded));
        var restored = Stats.Read(Assert.IsType<Relation>(decoded.Root));
        Assert.True(restored.Complete);
        Assert.Equal(result.Posterior.Rows.Select(row => row.Probability), restored.Rows.Select(row => row.Probability));
        Assert.Equal(result.Posterior.EvidenceProbability, restored.EvidenceProbability);
        Assert.Equal(result.Posterior.Events.Length, restored.Events.Length);
        Assert.NotSame(result.Posterior.Rows[0].Model, restored.Rows[0].Model);
    }

    [Fact]
    public void Ranking_receives_anonymous_native_packets_without_teacher_ancestry()
    {
        var fixture = Freeze("R006", 6);
        var result = Weighting.Read(fixture.Pool, Weighting.Plans[0]);
        var teacher = Corpus.Value.Single(item => item.Id == "R006");
        var privateParts = new[] { teacher.Source.Graph.Surface, teacher.Source.Bindings, teacher.Source.Provenance,
            teacher.Control.Surface, fixture.Analysis.Audit, fixture.Analysis.SourceMap };
        var reachable = OrnamentReconstruction.Participants(result.Surface).ToHashSet(ReferenceEqualityComparer.Instance);
        Assert.False(privateParts.Any(reachable.Contains));
        Assert.False(reachable.Any(item => item is Carrier or CarrierLocation));
        Assert.All(result.Alternatives, item => {
            Assert.Equal("sample", item.Trial.Candidate.Source.Id);
            Assert.DoesNotContain(item.Trial.Candidate.Source.Name, new[] { teacher.Name, teacher.Control.Family });
        });
    }

    [Fact]
    public void Closure_goals_crossing_and_overlap_defaults_are_not_observation_families()
    {
        var fixture = Freeze("R076", 6); var pool = fixture.Pool;
        Assert.True(pool.Search.Focus.Context.PreserveObservedGap);
        Assert.DoesNotContain(pool.Families, family => ReferenceEquals(family.Port, fixture.Packet.Message.ClosureDefault));
        Assert.Equal(new[] { "joints", "spatial", "equivalence", "symmetry", "interiors", "coarse" }, pool.Families.Select(family => family.Id));
        foreach (var item in pool.Alternatives)
        {
            var omitted = item.Trial.Core.Claims.Where(claim => claim.Kind is "closed" or "crossings" or "overlaps" or "assumptions")
                .Select(claim => (IElement)claim.Witness).ToHashSet(ReferenceEqualityComparer.Instance);
            Assert.NotEmpty(omitted);
            foreach (var group in pool.Assessments[item.Id])
            {
                var lanes = (Relation)group.Surface.Right;
                var checks = E((Relation)lanes.Left).Concat(E((Relation)((Relation)lanes.Right).Left))
                    .Concat(E((Relation)((Relation)lanes.Right).Right));
                Assert.False(checks.Any(omitted.Contains));
            }
        }
    }

    private sealed record Fixture(JointShapeDescription.Analysis Analysis, RelationalShapeExperiment.Packet Packet,
        RelationalShapeSearch.Result Search, IReadOnlyList<RelationalShapeSearch.Trial> Trials, Weighting.Pool Pool);
    private static Fixture Freeze(string sourceId, int budget)
    {
        var source = Corpus.Value.Single(item => item.Id == sourceId);
        var analysis = JointShapeDescription.Analyze(source.Source);
        var packet = RelationalShapeExperiment.Publish(analysis); var m = packet.Message;
        var focus = new RelationalShapeSearch.Focus("full-test", "Full exposed evidence", packet.Spatial,
            packet.Equivalence, StructuralEvidenceStreams.SelectClosure(m, packet.Streams));
        var trials = new List<RelationalShapeSearch.Trial>();
        var search = RelationalShapeSearch.Run(m, focus, budget, observeTrial: trials.Add);
        Assert.Equal(search.Evaluated, trials.Count);
        return new(analysis, packet, search, trials, Weighting.Freeze(m, search, trials));
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope)
    { Assert.True(StructuralConstraint.TryReadScope(scope, out var rows, out _, out _)); return rows; }
}
