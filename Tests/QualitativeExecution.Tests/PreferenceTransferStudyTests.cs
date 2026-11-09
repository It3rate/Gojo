using System.Text.Json;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class PreferenceTransferStudyTests
{
    [Theory]
    [InlineData("inset", "new-start")] [InlineData("chevron", "new-start")]
    [InlineData("inset", "changed-goal")] [InlineData("chevron", "changed-goal")]
    [InlineData("inset", "cross-family")] [InlineData("chevron", "cross-family")]
    public void A_useful_prior_can_help_a_new_context_without_identifying_its_motion(string family, string scenario)
    {
        var source = PreferenceTransferStudy.Train(family);
        var hp = source.Height.Phase; var wp = source.Width.Phase;
        var hd = source.Height.DiscardedCount; var wd = source.Width.DiscardedCount;
        var history = source.Height.RecentPasses; var widthHistory = source.Width.RecentPasses;
        var cold = PreferenceTransferStudy.Execute(source, scenario, "cold");
        var reused = PreferenceTransferStudy.Execute(source, scenario, "reuse");
        Assert.False(cold.TransferAccepted); Assert.True(reused.TransferAccepted);
        Assert.True(reused.Target.JointSettled); Assert.True(cold.Target.JointSettled);
        Assert.True(reused.FirstSettledPass < cold.FirstSettledPass);
        Assert.Equal(cold.Target.Frames[0].Height, reused.Target.Frames[0].Height);
        Assert.Equal(cold.Target.Frames[0].Width, reused.Target.Frames[0].Width);
        Assert.All(reused.Target.Frames, f =>
        {
            Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates);
            Assert.False(f.TrialInformative); Assert.False(f.HeightTrial); Assert.False(f.WidthTrial);
        });
        Assert.All(reused.Target.PassResults, p => Assert.InRange(p.RetainedEvaluations, 0, 4));
        Assert.Equal(hp, source.Height.Phase); Assert.Equal(wp, source.Width.Phase);
        Assert.Equal(hd, source.Height.DiscardedCount); Assert.Equal(wd, source.Width.DiscardedCount);
        Assert.Equal(history, source.Height.RecentPasses); Assert.Equal(widthHistory, source.Width.RecentPasses);
        Assert.Null(source.Height.PendingChoice); Assert.Null(source.Width.PendingChoice);
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void Reordered_options_transfer_by_correspondence_not_by_source_index(string family)
    {
        var run = PreferenceTransferStudy.Execute(PreferenceTransferStudy.Train(family), "reordered-options", "reuse");
        Assert.True(run.TransferAccepted); Assert.Equal(1, run.HeightSourceIndex); Assert.Equal(1, run.WidthSourceIndex);
        Assert.Equal(0, run.HeightTargetIndex); Assert.Equal(0, run.WidthTargetIndex);
        Assert.True(run.Target.JointSettled); Assert.Equal(1, run.FirstSettledPass);
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void A_reversed_publication_can_make_the_old_analogy_bad_then_passes_recover_without_reset(string family)
    {
        var source = PreferenceTransferStudy.Train(family);
        var revised = PreferenceTransferStudy.Execute(source, "reversed-response", "reuse");
        var fixedPrior = PreferenceTransferStudy.Execute(source, "reversed-response", "fixed");
        var corrected = PreferenceTransferStudy.Execute(source, "corrected-reversal", "reuse");
        Assert.Equal(0, revised.HeightTargetIndex); Assert.Equal(1, corrected.HeightTargetIndex);
        Assert.False(fixedPrior.Target.JointSettled); Assert.True(revised.Target.JointSettled);
        Assert.True(corrected.Target.JointSettled); Assert.True(corrected.FirstSettledPass < revised.FirstSettledPass);
        var before = revised.Target.Frames[0]; var first = revised.Target.Frames[1];
        Assert.Equal(before.Height + 1, first.Height); Assert.True(first.LandmarkGap < before.LandmarkGap);
        var ending = revised.Target.Frames.Single(f => f.Pass == 1 && f.Step == PartialDecisionStudy.BeatsPerPass);
        var next = revised.Target.Frames.Single(f => f.Pass == 2 && f.Step == 1);
        Assert.Equal(12, ending.Height); Assert.Equal(1, ending.HeightTension);
        Assert.Equal(-1, next.HeightRequest); Assert.Equal(ending.Height, next.Height); Assert.Equal(0, next.HeightTension);
        Assert.All(revised.Target.Frames, f => { Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates); });
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void Capacity_failure_is_not_erased_by_a_useful_transferred_preference(string family)
    {
        var run = PreferenceTransferStudy.Execute(PreferenceTransferStudy.Train(family), "capacity", "reuse");
        Assert.True(run.TransferAccepted); Assert.False(run.Target.JointSettled); Assert.Null(run.FirstSettledPass);
        Assert.All(run.Target.Frames, f => Assert.InRange(f.Height, 0, 1));
        Assert.Contains(run.Target.Frames, f => f.HeightTension > 0);
        Assert.NotEqual(run.Target.Frames[^1].Goal, run.Target.Frames[^1].LandmarkGap);
    }

    [Fact]
    public void Withheld_first_pass_preserves_the_prior_without_diagnostic_leakage()
    {
        var run = PreferenceTransferStudy.Execute(PreferenceTransferStudy.Train(), "withheld-first", "reuse");
        var start = run.Target.Frames[0];
        Assert.All(run.Target.Frames.Where(f => f.Pass == 1), f =>
        {
            Assert.False(f.Available); Assert.Null(f.HeightRequest); Assert.Null(f.WidthRequest);
            Assert.Equal(start.Height, f.Height); Assert.Equal(start.Width, f.Width);
            Assert.Equal(start.HeightTension, f.HeightTension); Assert.Equal(start.WidthTension, f.WidthTension);
        });
        Assert.Equal(0, run.Target.PassResults[0].RetainedEvaluations);
        Assert.Equal(1, run.HeightTargetIndex); Assert.Equal(1, run.Target.PassResults[1].HeightChoice);
        Assert.True(run.Target.JointSettled);
    }

    [Theory]
    [InlineData("cross-family")] [InlineData("reordered-options")]
    [InlineData("reversed-response")] [InlineData("capacity")]
    public void Native_and_compiled_laws_keep_transfer_and_entire_target_passes_equivalent(string scenario)
    {
        var native = PreferenceTransferStudy.Execute(PreferenceTransferStudy.Train(compiled: false), scenario, "reuse", compiled: false);
        var compact = PreferenceTransferStudy.Execute(PreferenceTransferStudy.Train(), scenario, "reuse");
        Assert.Equal(native.TransferAccepted, compact.TransferAccepted);
        Assert.Equal(native.HeightTargetIndex, compact.HeightTargetIndex);
        Assert.Equal(JsonSerializer.Serialize(native.Target.Frames), JsonSerializer.Serialize(compact.Target.Frames));
        Assert.Equal(JsonSerializer.Serialize(native.Target.PassResults), JsonSerializer.Serialize(compact.Target.PassResults));
    }
}
