using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeFrieze.Learning;

namespace QualitativeExecution.Tests;

public sealed class OrnamentLearningCorpusTests
{
    [Fact]
    public void Recipe_edit_groups_do_not_leak_transform_size_or_seed_siblings_across_partitions()
    {
        var first = OrnamentLearningCorpus.Describe(4096, 1729);
        var other = OrnamentLearningCorpus.Describe(4096, 9);
        Assert.Equal(OrnamentLearningCorpus.GroupCount, first.Select(row => row.Group).Distinct().Count());
        Assert.All(first.GroupBy(row => row.Group), group => Assert.Single(group.Select(row => row.Split).Distinct()));
        Assert.All(first.Zip(other), pair => Assert.Equal(pair.First.Split, pair.Second.Split));
        Assert.Equal(new[] { "test", "train", "validation" }, first.Select(row => row.Split).Distinct().Order().ToArray());
        Assert.All(first.Take(256), row => Assert.Equal(row, OrnamentLearningCorpus.Describe(256)[row.Index]));
    }

    [Fact]
    public void A_sample_contains_an_executable_native_motif_and_a_repeated_disposition()
    {
        var sample = OrnamentLearningCorpus.Materialize(Case(0, 0, 1296));
        var replay = sample.Program.Generate(sample.Bindings, sample.Window);
        Assert.True(replay.IsComplete);
        Assert.Equal(4, replay.Units.Count);
        Assert.Equal(4, E(sample.Target.Graph.Strokes).Length);
        Assert.Equal(16, E(sample.Pattern.Graph.Strokes).Length);
        var reads = new NativeInteriorFeatures(new(sample.Target)).Reads;
        Assert.Equal("Supported", Assert.Single(reads).Find("Square").Status);
        Assert.False(OrnamentReconstruction.Participants(sample.RecipePacket).Any(participant => participant is Carrier or CarrierLocation));
    }

    [Fact]
    public void The_stricter_protocol_holds_all_edits_and_transforms_of_a_base_construction_together()
    {
        var rows = OrnamentLearningCorpus.Describe(2048, splitByFamily: true);
        Assert.Equal(OrnamentLearningCorpus.FamilyCount, rows.Select(row => row.Group).Distinct().Count());
        Assert.All(rows.GroupBy(row => row.Family), group => Assert.Single(group.Select(row => row.Split).Distinct()));
        Assert.Equal(new[] { "test", "train", "validation" }, rows.Select(row => row.Split).Distinct().Order().ToArray());
        Assert.Equal(8, rows.Where(row => row.Split == "train").Select(row => row.Family).Distinct().Count());
        Assert.Equal(2, rows.Where(row => row.Split == "validation").Select(row => row.Family).Distinct().Count());
        Assert.Equal(2, rows.Where(row => row.Split == "test").Select(row => row.Family).Distinct().Count());
    }

    [Fact]
    public void Public_stream_roots_preserve_shared_sites_without_revealing_the_teacher_or_its_program()
    {
        var sample = OrnamentLearningCorpus.Materialize(Case(0, 0, 2));
        Assert.InRange(sample.Streams.Count, 2, 3);
        var all = sample.Streams.SelectMany(stream => E(stream.Source.Graph.Strokes)).Distinct(ReferenceEqualityComparer.Instance).ToArray();
        Assert.True(all.Length < E(sample.Target.Graph.Strokes).Length);
        foreach (var stream in sample.Streams)
        {
            var participants = OrnamentReconstruction.Participants(stream.Surface);
            Assert.False(participants.Any(item => ReferenceEquals(item, sample.Program.Surface)));
            Assert.False(participants.Any(item => ReferenceEquals(item, sample.Target.Graph.Surface)));
            Assert.False(participants.Any(item => ReferenceEquals(item, sample.Construction)));
            Assert.All(E(stream.Source.Graph.Sites), site => Assert.True(E(sample.Target.Graph.Sites).Any(item => ReferenceEquals(item, site))));
            Assert.Empty(E(stream.Source.Provenance));
        }
    }

    [Fact]
    public void Native_notch_refinement_changes_the_shape_and_retains_its_parent_construction()
    {
        var plain = OrnamentLearningCorpus.Materialize(Case(0, 0, 0));
        var notched = OrnamentLearningCorpus.Materialize(Case(0, 2, 0));
        Assert.Equal(4, E(plain.Target.Graph.Strokes).Length);
        Assert.Equal(8, E(notched.Target.Graph.Strokes).Length);
        var read = Assert.Single(new NativeInteriorFeatures(new(notched.Target)).Reads);
        Assert.Equal("Supported", read.Find("Contains notch").Status);
        Assert.Equal("Refuted", read.Find("Rectangle").Status);
        Assert.True(OrnamentReconstruction.Participants(notched.Surface).Any(item => ReferenceEquals(item, notched.Program.Surface)));
    }

    [Fact]
    public void A_missing_passage_remains_an_open_source_and_only_a_separate_hypothesis_closes_it()
    {
        var sample = OrnamentLearningCorpus.Materialize(Case(0, 1, 0));
        var source = new NativeInteriorTopology(sample.Target);
        Assert.Empty(source.Boundaries);
        Assert.Equal(3, E(sample.Target.Graph.Strokes).Length);
        var completion = new NativeInteriorCompletion(source);
        Assert.NotEmpty(completion.Proposals);
        Assert.All(completion.Proposals.SelectMany(proposal => proposal.Reads), read => Assert.Equal("Possible", read.Find("Closed").Status));
        Assert.Equal(3, E(sample.Target.Graph.Strokes).Length);
    }

    [Fact]
    public void Conflicting_landmark_replies_are_native_distinct_bindings_to_the_same_actual_site()
    {
        var sample = OrnamentLearningCorpus.Materialize(Case(0, 0, 0));
        var stream = Assert.Single(sample.Streams, stream => stream.Name == "competing landmark");
        var bindings = E(stream.Source.Bindings).Cast<Relation>().ToArray();
        Assert.Equal(2, bindings.Length);
        Assert.Same(bindings[0].Left, bindings[1].Left);
        Assert.NotSame(bindings[0].Right, bindings[1].Right);
        Assert.Empty(E(stream.Source.Graph.Strokes));
        Assert.Empty(E(stream.Source.Provenance));
        Assert.False(OrnamentReconstruction.Participants(stream.Surface).Any(item => ReferenceEquals(item, sample.Target.Graph.Surface)));
    }

    [Fact]
    public void Every_recipe_edit_group_executes_and_keeps_its_pattern_within_the_declared_passage_cap()
    {
        foreach (var descriptor in OrnamentLearningCorpus.Describe(OrnamentLearningCorpus.GroupCount, 1296))
        {
            var sample = OrnamentLearningCorpus.Materialize(descriptor, includePattern: false);
            Assert.InRange(E(sample.Target.Graph.Strokes).Length, 1, 64);
            Assert.InRange(E(sample.Pattern.Graph.Strokes).Length, 1, 64);
            foreach (var stream in sample.Streams)
                Assert.False(OrnamentReconstruction.Participants(stream.Surface).Any(participant => participant is Carrier or CarrierLocation));
        }
    }

    private static OrnamentLearningCorpus.Descriptor Case(int family, int edit, int variation) =>
        new(0, $"recipe-{family:00}-edit-{edit:00}", "train", family, edit, variation);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
}
