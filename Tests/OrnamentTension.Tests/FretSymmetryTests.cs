using Gojo.Experiments.OrnamentFlowPilot;
using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Tests.OrnamentTension;

public class FretSymmetryTests
{
    [Fact]
    public void TwoEncodingsReproduceTheSelectedCourseButBindDifferentReaches()
    {
        var direct = FretSymmetrySource.Direct();
        Assert.Equal(direct, FretSymmetrySource.Glide());
        Assert.Equal(new PathSite[] { new(7, 0), new(0, -3), new(-2, 0), new(0, 1), new(1, 0), new(0, 1), new(-2, 0), new(0, -3) }, direct[..8]);
        var independent = FretSymmetry.Read(FretSymmetrySource.Direct(2));
        var coupled = FretSymmetry.Read(FretSymmetrySource.Glide(2));
        Assert.Equal(9L, independent.Pitch);
        Assert.Equal(10L, coupled.Pitch);
        Assert.DoesNotContain(independent.Band, m => m.Kind == "glide reflection");
        Assert.Contains(coupled.Band, m => m.Kind == "glide reflection" && m.OffsetX == 5 && !m.Reversed);
    }

    [Fact]
    public void GlobalInkSymmetriesAndLocalDirectionalLikenessesRemainDistinct()
    {
        var bay = FretSymmetrySource.Glide();
        var survey = FretSymmetry.Read(bay);
        Assert.Equal(4096L, survey.CandidatesChecked);
        Assert.Equal(4, survey.Band.Length);
        Assert.Contains(new BandSymmetry("vertical-mirror", 7, 0, true, "reflection"), survey.Band);
        Assert.Contains(new BandSymmetry("horizontal-mirror", 4, -4, false, "glide reflection"), survey.Band);
        Assert.Contains(new BandSymmetry("half-turn", 3, -4, true, "rotation"), survey.Band);
        var local = Assert.Single(survey.Matches, m => m.Source == 5 && m.Target == 12 && !m.Reversed && m.Frame == "rising-diagonal");
        Assert.Equal(8, local.DirectionRun);
        Assert.Equal(1, local.SimilarRun);
        Assert.Equal(64, survey.Matches.Count(m => m.EqualRun == 16));
        Assert.DoesNotContain(survey.Matches, m => m.EqualRun is >= 3 and < 16);
        Assert.DoesNotContain(survey.Matches, m => m.SimilarRun >= 3 && m.EqualRun < 3);
        var clipped = survey.Matches.Where(m => m.Cut.ScaleNumerator != m.Cut.ScaleDenominator).ToArray();
        Assert.Equal(288, clipped.Length);
        Assert.All(clipped, m => Assert.Equal(3, m.Cut.Run));
        var diagonal = Assert.Single(clipped, m => m.Source == 13 && m.Target == 6 && !m.Reversed && m.Frame == "rising-diagonal");
        Assert.Equal(new FretCut(3, 2, 3, 3, 9), diagonal.Cut);
        Assert.True(SymmetryStudy.ReadCap(bay, survey).CutsBothArms);
    }

    [Fact]
    public void UniformScalingRequiresAllLengthsToShareTheFirstRatio()
    {
        PathSite[] word = [new(1, 0), new(0, 1), new(1, 0), new(0, -1),
            new(2, 0), new(0, 2), new(2, 0), new(0, -2)];
        var match = Assert.Single(FretSymmetry.Read(word).Matches,
            m => m.Source == 0 && m.Target == 4 && m.Frame == "identity" && !m.Reversed);
        Assert.Equal((2L, 1L, 4, 8), (match.ScaleNumerator, match.ScaleDenominator, match.SimilarRun, match.DirectionRun));
    }
}
