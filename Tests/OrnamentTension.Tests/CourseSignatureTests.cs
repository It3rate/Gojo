using Gojo.Experiments.OrnamentFlowPilot;

namespace Gojo.Tests.OrnamentTension;

public class CourseSignatureTests
{
    [Fact]
    public void ShrinkingReachesAndTurningHandAreIndependentCyclicReads()
    {
        var segments = FretSymmetrySource.Glide();
        var read = CourseSignature.Read(segments);
        Assert.Equal(8, read.LengthPeriod);
        Assert.Equal(3, read.LongestContraction);
        var contractions = read.ReachRuns.Where(run => run.Trend == "shrinking").ToArray();
        Assert.Equal(2, contractions.Length);
        Assert.All(contractions, run => Assert.Equal(new long[] { 7, 3, 2, 1 }, run.Reaches));
        Assert.Contains(contractions, run => run.Hand == 'L');
        Assert.Contains(contractions, run => run.Hand == 'R');
        var shifted = CourseSignature.Read([.. segments[2..], .. segments[..2]]);
        Assert.Equal(read.LongestContraction, shifted.LongestContraction);
        var changed = CourseSignature.Read(FretSymmetrySource.Direct(2));
        Assert.Equal(read.TurnWord, changed.TurnWord);
        Assert.Equal(16, changed.LengthPeriod);
    }

    [Fact]
    public void GlideSwapsWholeWhiteChannelsWhilePureHorizontalFlipFails()
    {
        var segments = FretSymmetrySource.Glide();
        var gaps = GapSignature.Read(segments);
        var symmetries = FretSymmetry.Read(segments).Band;
        var glide = Assert.Single(symmetries, s => s.Kind == "glide reflection");
        var map = SignatureStudy.MapGaps(gaps, glide);
        Assert.True(map.CarriesRegions);
        foreach (var region in gaps.Regions)
            Assert.NotEqual(region.Id, map.TargetRegions[region.Id]);
        var unshifted = SignatureStudy.MapGaps(gaps, glide with { OffsetX = 0 });
        Assert.False(unshifted.CarriesRegions);
        Assert.All(unshifted.AreaOverlaps, row => Assert.Equal(16m, row.Sum()));
    }
}
