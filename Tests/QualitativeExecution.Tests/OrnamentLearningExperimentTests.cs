using Gojo.Experiments.QualitativeFrieze.Learning;
using E = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningExperiment;

namespace QualitativeExecution.Tests;

public sealed class OrnamentLearningExperimentTests
{
    [Fact]
    public void Evaluation_keeps_missing_targets_conflicts_abstentions_and_novel_targets_in_the_denominator()
    {
        var absent = Row(0, "a", boundaries: 0, admissible: 0);
        var conflicting = Row(1, "b", boundaries: 1, admissible: 0) with
        { SeenExact = true, SeenShape = true, Conflicting = true };
        var recalled = Row(2, "b", boundaries: 1, admissible: 2) with
        { SeenShape = true, ShapeTop1 = true, ShapeTop3 = true, FrequencyShapeTop1 = true,
            GeneratedClosed = true, GeneratedOpenClaims = 3, TeacherShapeAdmissible = true };
        var novelAlternative = Row(3, "c", boundaries: 1, admissible: 1) with { GeneratedClosed = true };
        var summary = E.Summarize("test", [absent, conflicting, recalled, novelAlternative]);

        Assert.Equal(4, summary.Rows);
        Assert.Equal(3, summary.Groups);
        Assert.Equal(3, summary.WithTeacherBoundary);
        Assert.Equal(2, summary.Abstentions);
        Assert.Equal(1, summary.Conflicting);
        Assert.Equal(1, summary.ConflictAbstentions);
        Assert.Equal(1, summary.UnseenShapeRows);
        Assert.Equal(1, summary.ShapeTop1);
        Assert.Equal(0, summary.ExactTop1);
        Assert.Equal(3, summary.MeaningsProposed);
        Assert.Equal(2, summary.GeneratedClosed);
        Assert.Equal(1, summary.GeneratedWithOpenClaims);
        Assert.Equal(1, summary.CleanFamiliarRows);
        Assert.Equal(1, summary.CleanFamiliarAdmitted);
        Assert.Equal(1, summary.CleanFamiliarTop1);
    }

    [Fact]
    public void A_constructed_sibling_group_cannot_be_used_in_two_partitions()
    {
        var training = new OrnamentLearningCorpus.Descriptor(0, "shared-parent", "train", 0, 0, 0);
        var transformedSibling = new OrnamentLearningCorpus.Descriptor(1, "shared-parent", "test", 0, 0, 37);
        Assert.Throws<InvalidOperationException>(() => E.EnsurePartitionIsolation([training, transformedSibling]));
    }

    private static E.Row Row(int index, string group, int boundaries, int admissible) => new(
        Index: index, Group: group, Split: "test", Family: 0, Edit: 0,
        TeacherBoundaries: boundaries, SeenExact: false, SeenShape: false, Conflicting: false,
        Streams: 2, Admissible: admissible, FirstStreamAdmissible: admissible,
        ExactTop1: false, ShapeTop1: false, ShapeTop3: false, FirstStreamShapeTop1: false,
        UniformShapeTop1: false, FrequencyShapeTop1: false, RandomShapeTop1: false,
        GeneratedClosed: false, GeneratedOpenClaims: 0, Milliseconds: 1);
}
