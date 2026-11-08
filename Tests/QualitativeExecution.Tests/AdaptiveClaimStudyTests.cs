using System.Text.Json;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class AdaptiveClaimStudyTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void Collapsed_landmark_goal_does_not_become_a_successful_section_gap(int seed)
    {
        var primary = AdaptiveClaimStudy.Execute("alternative", "collapsed", "primary", seed);
        var fallback = AdaptiveClaimStudy.Execute("alternative", "collapsed", "alternative", seed);
        Assert.False(primary.PrimarySettled); Assert.False(primary.AlternativeSettled);
        Assert.False(fallback.PrimarySettled); Assert.True(fallback.AlternativeSettled); Assert.False(fallback.JointSettled);
        Assert.All(fallback.Frames, f => { Assert.Equal(0, f.Width); Assert.Null(f.Gap); });
        Assert.Contains(fallback.Frames, f => f.UsedAlternative && f.HeightRequest is 1 or -1);
        Assert.Equal(fallback.Frames[^1].Goal, fallback.Frames[^1].LandmarkGap);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void An_alternative_can_recover_the_primary_observation_when_overlap_returns(int seed)
    {
        var primary = AdaptiveClaimStudy.Execute("alternative", "disjoint", "primary", seed);
        var fallback = AdaptiveClaimStudy.Execute("alternative", "disjoint", "alternative", seed);
        Assert.Null(primary.Frames[0].Gap); Assert.Null(primary.Frames[^1].Gap);
        Assert.False(primary.PrimarySettled); Assert.True(fallback.PrimarySettled);
        Assert.Contains(fallback.Frames, f => f.UsedAlternative && f.HeightRequest == -1);
        Assert.Contains(fallback.Frames, f => f.Gap.HasValue && !f.UsedAlternative && f.Beat > 0);
        Assert.False(fallback.Frames[^1].UsedAlternative);
    }

    [Fact]
    public void Withholding_the_public_observation_blocks_alternative_actuation()
    {
        var run = AdaptiveClaimStudy.Execute("alternative", "missing", "alternative");
        var initial = run.Frames[0];
        Assert.All(run.Frames.Where(f => f.Beat is >= 1 and <= 8), f =>
        {
            Assert.Null(f.HeightRequest); Assert.Equal(0, f.HeightMask);
            Assert.Equal(initial.Height, f.Height); Assert.False(f.UsedAlternative);
            Assert.NotNull(f.LandmarkGap);
        });
        Assert.True(run.AlternativeSettled); Assert.False(run.PrimarySettled);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void A_finer_rung_reaches_a_gap_between_coarse_steps_without_erasing_residue(int seed)
    {
        var coarse = AdaptiveClaimStudy.Execute("refinement", "between-steps", "coarse", seed);
        var refined = AdaptiveClaimStudy.Execute("refinement", "between-steps", "refine", seed);
        Assert.False(coarse.PrimarySettled); Assert.DoesNotContain(coarse.Frames, f => f.Gap == 30);
        Assert.True(refined.PrimarySettled); Assert.True(refined.JointSettled);
        var final = refined.Frames[^1]; Assert.Equal(30, final.Gap);
        Assert.True(final.HeightFine); Assert.True(final.WidthFine);
        Assert.Equal(1, final.HeightScale); Assert.Equal(1, final.WidthScale);
        Assert.Equal(final.Height, final.HeightCoarse * 3 + final.HeightResidue);
        Assert.Equal(final.Width, final.WidthCoarse * 3 + final.WidthResidue);
        Assert.NotEqual(0, final.HeightResidue); Assert.NotEqual(0, final.WidthResidue);
        Assert.All(refined.Frames.Where(f => f.HeightFine), f => Assert.Equal(final.HeightRetained, f.HeightRetained));
        Assert.All(refined.Frames.Where(f => f.WidthFine), f => Assert.Equal(final.WidthRetained, f.WidthRetained));
    }

    [Fact]
    public void Refinement_activation_preserves_the_existing_physical_extent()
    {
        var run = AdaptiveClaimStudy.Execute("refinement", "between-steps", "refine");
        Assert.Contains(run.Frames, f => f.HeightRefined); Assert.Contains(run.Frames, f => f.WidthRefined);
        for (var i = 1; i < run.Frames.Length; i++)
        {
            var a = run.Frames[i - 1]; var b = run.Frames[i];
            if (b.HeightRefined)
            {
                Assert.Equal(a.Height * a.HeightScale, b.Height * b.HeightScale);
                Assert.Null(b.HeightRequest); Assert.Equal(a.Height, b.HeightRetained);
                // Width may respond on this beat, but cannot move these heights.
                Assert.Equal(a.Lower.Select(p => p[1]), b.Lower.Select(p => p[1]));
            }
            if (b.WidthRefined)
            {
                Assert.Equal(a.Width * a.WidthScale, b.Width * b.WidthScale);
                Assert.Null(b.WidthRequest); Assert.Equal(a.Width, b.WidthRetained);
            }
        }
    }

    [Fact]
    public void Simultaneous_rung_activation_keeps_the_same_geometric_picture()
    {
        var run = AdaptiveClaimStudy.Execute("combined", "collapsed", "combined");
        var index = Array.FindIndex(run.Frames, f => f.HeightRefined && f.WidthRefined);
        Assert.True(index > 0);
        var before = run.Frames[index - 1]; var after = run.Frames[index];
        Assert.Equal(before.Upper.SelectMany(p => p), after.Upper.SelectMany(p => p));
        Assert.Equal(before.Lower.SelectMany(p => p), after.Lower.SelectMany(p => p));
        Assert.Null(after.HeightRequest); Assert.Null(after.WidthRequest);
        Assert.Equal("unresolved", after.HeightMotion); Assert.Equal("unresolved", after.WidthMotion);
    }

    [Fact]
    public void Fine_capacity_refusal_does_not_copy_into_an_unavailable_receiver()
    {
        var run = AdaptiveClaimStudy.Execute("refinement", "fine-capacity", "refine");
        Assert.Contains(run.Frames, f => f.RefinementBlocked);
        Assert.All(run.Frames, f => { Assert.False(f.HeightFine); Assert.Equal(3, f.HeightScale); });
        Assert.False(run.PrimarySettled);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void Isolated_trial_evidence_selects_a_motion_word_before_feedback_uses_it(int seed)
    {
        var run = AdaptiveClaimStudy.Execute("motion", "ordinary", "observed", seed);
        Assert.True(run.JointSettled);
        Assert.True(run.Frames[0].HeightCandidates > 1); Assert.True(run.Frames[0].WidthCandidates > 1);
        var h = Assert.Single(run.Frames.Where(f => f.HeightTrial));
        var w = Assert.Single(run.Frames.Where(f => f.WidthTrial));
        Assert.Equal(1, h.HeightCandidates); Assert.Equal(1, w.WidthCandidates);
        Assert.Equal("(0, -9) reaches / tick", h.HeightMotion);
        Assert.Equal("(3, 0) reaches / tick", w.WidthMotion);
        Assert.Equal(1, run.Frames[^1].HeightCandidates); Assert.Equal(1, run.Frames[^1].WidthCandidates);
        Assert.All(run.Frames.Where(f => f.Beat > 0 && !f.HeightTrial && f.HeightCandidates > 1), f => Assert.Null(f.HeightRequest));
        Assert.All(run.Frames.Where(f => f.Beat > 0 && !f.WidthTrial && f.WidthCandidates > 1), f => Assert.Null(f.WidthRequest));
    }

    [Theory]
    [InlineData("confounded")] [InlineData("blocked")] [InlineData("ambiguous")]
    public void Insufficient_evidence_never_substitutes_the_authored_motion(string scenario)
    {
        var run = AdaptiveClaimStudy.Execute("motion", scenario, "observed");
        var final = run.Frames[^1];
        Assert.True(final.HeightCandidates > 1); Assert.Equal("unresolved", final.HeightMotion);
        Assert.Null(final.HeightRequest); Assert.Equal(0, final.HeightMask);
        Assert.False(run.JointSettled);
        if (scenario == "ambiguous")
        {
            Assert.Equal(2, final.HeightCandidates); Assert.Equal(2, final.WidthCandidates);
        }
        if (scenario == "blocked")
        {
            var trial = Assert.Single(run.Frames.Where(f => f.HeightTrial));
            Assert.Equal(run.Frames[0].Height, trial.Height); Assert.Equal(1, trial.HeightTension);
        }
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public void Combined_refinement_relearns_fine_motion_instead_of_inheriting_coarse_ticks(int seed)
    {
        var run = AdaptiveClaimStudy.Execute("combined", "between-steps", "combined", seed);
        Assert.True(run.JointSettled);
        var activation = Assert.Single(run.Frames.Where(f => f.HeightRefined));
        Assert.True(activation.HeightCandidates > 1); Assert.Equal("unresolved", activation.HeightMotion);
        Assert.Null(activation.HeightRequest);
        var fineTrial = Assert.Single(run.Frames.Where(f => f.HeightFine && f.HeightTrial));
        Assert.True(fineTrial.Beat > activation.Beat); Assert.Equal(1, fineTrial.HeightCandidates);
        Assert.Equal("(0, -3) reaches / tick", fineTrial.HeightMotion);
        Assert.Contains(run.Frames, f => !f.HeightFine && f.HeightTrial && f.HeightMotion == "(0, -9) reaches / tick");
        Assert.Equal(30, run.Frames[^1].Gap);
    }

    [Theory]
    [InlineData("alternative", "collapsed", "alternative")]
    [InlineData("refinement", "between-steps", "refine")]
    [InlineData("motion", "ordinary", "observed")]
    [InlineData("combined", "between-steps", "combined")]
    public void Native_and_compiled_execution_preserve_the_same_trace(string stage, string scenario, string mode)
    {
        var native = AdaptiveClaimStudy.Execute(stage, scenario, mode, compiled: false);
        var compiled = AdaptiveClaimStudy.Execute(stage, scenario, mode, compiled: true);
        Assert.Equal(JsonSerializer.Serialize(native.Frames), JsonSerializer.Serialize(compiled.Frames));
    }
}
