using System.Text.Json;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;

namespace QualitativeExecution.Tests;

public sealed class PartialDecisionStudyTests
{
    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void Shared_response_can_settle_without_identifying_one_equivalent_word(string family)
    {
        var unique = PartialDecisionStudy.Execute("equivalent", "unique", family);
        var shared = PartialDecisionStudy.Execute("equivalent", "shared", family);
        Assert.False(unique.JointSettled); Assert.True(shared.JointSettled);
        var calibration = unique.Frames.Last(f => f.Beat == 0);
        Assert.All(unique.Frames.Where(f => f.Beat > 0), f =>
        {
            Assert.Equal(calibration.Height, f.Height); Assert.Equal(calibration.Width, f.Width);
            Assert.Null(f.HeightRequest); Assert.Null(f.WidthRequest);
        });
        Assert.All(shared.Frames, f =>
        {
            Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates);
            Assert.False(f.HeightTrial); Assert.False(f.WidthTrial);
        });
        Assert.Contains(shared.Frames, f => f.HeightAction == "shared" && f.HeightRequest is 1 or -1);
        Assert.All(shared.PassResults, p => Assert.Equal(0, p.ProvisionalActs));
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void Complete_pass_preference_revision_has_utility_without_eliminating_motion_candidates(string family)
    {
        var fixedChoice = PartialDecisionStudy.Execute("opposed", "fixed", family);
        var revised = PartialDecisionStudy.Execute("opposed", "passes", family);
        Assert.False(fixedChoice.JointSettled); Assert.True(revised.JointSettled);
        Assert.Equal(PartialDecisionStudy.Passes, revised.PassResults.Length);
        Assert.Equal(1, revised.PassResults[0].HeightChoice); Assert.Equal(1, revised.PassResults[0].WidthChoice);
        Assert.All(fixedChoice.PassResults, p => { Assert.Equal(0, p.HeightChoice); Assert.Equal(0, p.WidthChoice); });
        Assert.All(revised.Frames, f => { Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates); Assert.False(f.TrialInformative); });
        Assert.All(revised.PassResults, p => Assert.Equal(0, p.Trials));
        Assert.True(revised.PassResults[0].ProvisionalActs > 0);
        Assert.True(revised.PassResults[1].GapAfter == revised.PassResults[1].Goal);
        Assert.Equal(0, revised.PassResults[1].EndAfter);
        Assert.All(revised.PassResults, p => Assert.InRange(p.RetainedEvaluations, 0, 4));
        Assert.Equal(8, revised.PassResults[^1].DroppedEvaluations);
    }

    [Fact]
    public void Revising_the_route_carries_real_state_and_repays_prior_tension_before_moving()
    {
        var run = PartialDecisionStudy.Execute("opposed", "passes");
        var end = run.Frames.Single(f => f.Pass == 1 && f.Step == PartialDecisionStudy.BeatsPerPass);
        var next = run.Frames.Single(f => f.Pass == 2 && f.Step == 1);
        Assert.Equal(-1, end.HeightTension); Assert.Equal(0, end.Height);
        Assert.Equal(1, next.HeightRequest); Assert.Equal(end.Height, next.Height); Assert.Equal(0, next.HeightTension);
        Assert.Equal(end.Width + next.WidthRequest, next.Width);
        Assert.True(run.JointSettled);
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void A_negative_trial_can_identify_motion_at_upper_capacity_without_claiming_goal_success(string family)
    {
        var run = PartialDecisionStudy.Execute("capacity", "adaptive", family);
        var trial = Assert.Single(run.Frames.Where(f => f.HeightTrial));
        Assert.Equal(-1, trial.HeightRequest); Assert.True(trial.TrialInformative);
        Assert.Equal(1, trial.HeightCandidates); Assert.Equal(0, trial.Height);
        Assert.All(run.Frames, f => Assert.InRange(f.Height, 0, 1));
        Assert.False(run.JointSettled); Assert.False(run.PrimarySettled);
        Assert.Equal(1, run.Frames[^1].HeightTension); Assert.Equal(1, run.Frames[^1].HeightCandidates);
        Assert.NotEqual(run.Frames[^1].Goal, run.Frames[^1].Gap);
    }

    [Fact]
    public void Waiting_for_a_grant_is_distinct_from_a_trial_and_a_repaid_debt_is_not_motion_evidence()
    {
        var run = PartialDecisionStudy.Execute("delayed", "adaptive");
        Assert.Contains(run.Frames, f => f.TrialWaiting && !f.HeightTrial && !f.WidthTrial && f.HeightAction == "provisional");
        var trials = run.Frames.Where(f => f.HeightTrial).ToArray();
        Assert.Equal(2, trials.Length); Assert.False(trials[0].TrialInformative); Assert.True(trials[1].TrialInformative);
        Assert.Equal(2, trials[0].HeightCandidates); Assert.Equal(1, trials[1].HeightCandidates);
        var before = run.Frames.Single(f => f.Beat == trials[0].Beat - 1);
        Assert.Equal(before.Height, trials[0].Height); Assert.NotEqual(before.HeightTension, trials[0].HeightTension);
        Assert.True(run.JointSettled);
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void Refused_quiet_access_does_not_become_an_isolated_trial_but_provisional_passes_can_continue(string family)
    {
        var run = PartialDecisionStudy.Execute("quiet-refused", "adaptive", family);
        Assert.Contains(run.Frames, f => f.TrialWaiting);
        Assert.All(run.Frames, f =>
        {
            Assert.False(f.HeightTrial); Assert.False(f.WidthTrial); Assert.False(f.TrialInformative);
            Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates);
        });
        Assert.All(run.PassResults, p => Assert.Equal(0, p.Trials));
        Assert.Contains(run.PassResults, p => p.ProvisionalActs > 0); Assert.True(run.JointSettled);
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void Confounded_trials_preserve_both_explanations_even_if_later_passes_reach_the_goal(string family)
    {
        var run = PartialDecisionStudy.Execute("confounded", "adaptive", family);
        Assert.Contains(run.Frames, f => f.HeightTrial || f.WidthTrial);
        Assert.All(run.Frames, f =>
        {
            Assert.False(f.TrialInformative); Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates);
        });
        Assert.Equal(4, run.PassResults.Sum(p => p.Trials)); Assert.Equal(0, run.PassResults.Sum(p => p.InformativeTrials));
        Assert.True(run.JointSettled); Assert.Contains(run.PassResults, p => p.ProvisionalActs > 0);
    }

    [Theory]
    [InlineData("passes")] [InlineData("adaptive")]
    public void Withheld_pass_observations_do_not_leak_through_diagnostic_geometry(string mode)
    {
        var run = PartialDecisionStudy.Execute("withheld", mode);
        var before = run.Frames.Single(f => f.Pass == 1 && f.Step == PartialDecisionStudy.BeatsPerPass);
        var withheld = run.Frames.Where(f => f.Pass == 2).ToArray();
        Assert.Equal(PartialDecisionStudy.BeatsPerPass, withheld.Length);
        Assert.All(withheld, f =>
        {
            Assert.False(f.Available); Assert.False(f.HeightTrial); Assert.False(f.WidthTrial);
            Assert.Null(f.HeightRequest); Assert.Null(f.WidthRequest);
            Assert.Equal(before.Height, f.Height); Assert.Equal(before.Width, f.Width);
            Assert.Equal(before.HeightTension, f.HeightTension); Assert.Equal(before.WidthTension, f.WidthTension);
            Assert.NotNull(f.LandmarkGap); // Viewer diagnostics exist but cannot drive action.
        });
        Assert.Equal(0, run.PassResults[1].ProvisionalActs); Assert.Equal(0, run.PassResults[1].Trials);
        Assert.Equal(-1, run.PassResults[1].HeightChoice); Assert.Equal(-1, run.PassResults[1].WidthChoice);
    }

    [Fact]
    public void A_changed_goal_starts_an_independent_policy_context_without_resetting_the_ongoing_world()
    {
        var run = PartialDecisionStudy.Execute("goal-change", "passes", captureNative: true);
        var oldPass = run.PassResults[2]; var next = run.PassResults[3];
        Assert.NotEqual(oldPass.Goal, next.Goal); Assert.Equal(oldPass.GapAfter, next.GapBefore);
        Assert.True(next.RetainedEvaluations < oldPass.RetainedEvaluations);
        Assert.Equal(0, next.DroppedEvaluations); Assert.True(run.JointSettled);
        Assert.All(run.Frames, f => { Assert.Equal(2, f.HeightCandidates); Assert.Equal(2, f.WidthCandidates); });
        var live = Assert.IsType<Relation>(FiniteLaw.Entries(run.NativeSurface!)[0]);
        var retired = Assert.IsType<Relation>(FiniteLaw.Entries(live)[5]);
        Assert.Equal(2, FiniteLaw.Entries(retired).Length); // Separate original height/width contexts survive.
        foreach (var item in FiniteLaw.Entries(retired))
        {
            var transition = Assert.IsType<Relation>(item);
            var memory = PassPolicyMemory.Restore(Assert.IsType<Relation>(transition.Left));
            Assert.Equal(2, memory.RecentPasses.Length);
        }
    }

    [Theory]
    [InlineData("inset")] [InlineData("chevron")]
    public void A_landmark_response_can_help_restore_a_section_without_relabeling_the_missing_gap(string family)
    {
        var run = PartialDecisionStudy.Execute("lost-section", "passes", family);
        // The inset section also consumes height, so zero width alone does
        // not collapse it. It loses the section later in the poor first pass.
        if (family == "chevron") Assert.Null(run.Frames[0].Gap);
        else Assert.Equal(6, run.Frames[0].Gap);
        Assert.Contains(run.Frames, f => f.Gap is null && f.LandmarkGap is not null);
        Assert.Contains(run.Frames, f => f.Gap is null && f.WidthRequest == 1);
        Assert.True(run.JointSettled); Assert.NotNull(run.Frames[^1].Gap);
    }

    [Theory]
    [InlineData("equivalent", "shared")]
    [InlineData("opposed", "passes")]
    [InlineData("capacity", "adaptive")]
    [InlineData("delayed", "adaptive")]
    public void Native_and_compiled_laws_preserve_complete_passes_and_actions(string scenario, string mode)
    {
        var native = PartialDecisionStudy.Execute(scenario, mode, compiled: false);
        var compiled = PartialDecisionStudy.Execute(scenario, mode, compiled: true);
        Assert.Equal(JsonSerializer.Serialize(native.Frames), JsonSerializer.Serialize(compiled.Frames));
        Assert.Equal(JsonSerializer.Serialize(native.PassResults), JsonSerializer.Serialize(compiled.PassResults));
    }
}
