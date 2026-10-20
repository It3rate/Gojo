using Gojo.Core;
using Gojo.Experiments.QualitativeFrieze;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace QualitativeExecution.Tests;

public sealed class OrnamentReconstructionVocabularyTests
{
    [Fact]
    public void Vocabulary_contains_distinct_connected_questions_across_all_five_services()
    {
        var entries = OrnamentReconstructionVocabulary.All;
        Assert.InRange(entries.Count, 55, 64); Assert.Equal(entries.Count, entries.Select(entry => entry.Id).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, entries.Count).Select(index => $"Q{index:000}"), entries.Select(entry => entry.Id));
        Assert.All(entries, entry => Assert.True(entry.Query.IsValid));
        Assert.Equal(24, entries.Count(entry => entry.Channel == "visible motif"));
        Assert.Equal(20, entries.Count(entry => entry.Channel == "mechanical path"));
        Assert.Equal(10, entries.Count(entry => entry.Channel == "joint occurrence"));
        Assert.Equal(3, entries.Count(entry => entry.Channel == "crossing"));
        Assert.Equal(3, entries.Count(entry => entry.Channel == "continuation"));
        Assert.DoesNotContain(entries.SelectMany(entry => R.Participants(entry.Query.Surface)), item => item is Carrier or CarrierLocation);
    }

    [Fact]
    public void Long_whole_path_questions_retain_their_recipe_without_an_induction_length_workaround()
    {
        var wave = Sample("WAVE"); var input = Input(wave);
        Assert.True(R.TryDescribeGroup(input, (Relation)Gojo.Experiments.QualitativeExecution.FiniteLaw.Entries(wave.MechanicalGroups).Single(), out var pattern, out _));
        var question = OrnamentReconstructionVocabulary.All.Single(entry => entry.Channel == "mechanical path" &&
            entry.Query.Pattern is not null && EdgeCount(entry.Query.Pattern) == 7).Query;
        var bank = new R.Bank([input], [question], new(MaxWalkEdges: 0, MaxCycleEdges: 0, ProposalCapacity: 0));
        Assert.Equal("Yes", bank.Read(input, question).Answer); Assert.Equal(0, bank.Inductions);
    }

    [Fact]
    public void Vocabulary_separates_public_outline_from_whole_mechanical_path()
    {
        var one = Sample("SQ-PATH"); var four = Sample("SQ-SIDES"); var a = Input(one); var b = Input(four);
        Assert.True(R.TryDescribeGroup(a, (Relation)Gojo.Experiments.QualitativeExecution.FiniteLaw.Entries(one.MechanicalGroups).Single(), out var pattern, out _));
        var choices = OrnamentReconstructionVocabulary.All.Where(entry => entry.Channel == "mechanical path" && entry.Query.Pattern is not null &&
            EdgeCount(entry.Query.Pattern) == 4).Select(entry => entry.Query).ToArray();
        var visible = R.Query.Motif("Same contour through a public read", pattern); var bank = new R.Bank([a, b], [.. choices, visible]);
        var whole = choices.Single(question => bank.Read(a, question).Answer == "Yes");
        Assert.Equal("Yes", bank.Read(a, whole).Answer); Assert.Equal("No", bank.Read(b, whole).Answer);
        Assert.Equal("Yes", bank.Read(a, visible).Answer); Assert.Equal("Yes", bank.Read(b, visible).Answer);
    }

    private static OrnamentReconstructionCases.Sample Sample(string family) => OrnamentReconstructionCases.Specs.Single(spec =>
        spec.Family == family && spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1).Create();
    private static R.Input Input(OrnamentReconstructionCases.Sample sample) => new(sample.Spec.Name, sample.Observation, sample.MechanicalGroups);
    private static int EdgeCount(Relation pattern)
    {
        var definition = (Relation)pattern.Right; var sitesAndEdges = (Relation)((Relation)definition.Left).Right;
        return Gojo.Experiments.QualitativeExecution.FiniteLaw.Entries((Relation)sitesAndEdges.Right).Length;
    }
}
