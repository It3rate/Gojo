using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

namespace QualitativeExecution.Tests;

public sealed class RetainedMatchStudyTests
{
    private static readonly Lazy<RetainedMatchStudy> Study = new(RetainedMatchStudy.Build);

    [Fact]
    public void Later_questions_preserve_the_chosen_reference_instead_of_switching_to_a_successful_distractor()
    {
        var episodes = Study.Value.Episodes;
        Assert.All(episodes, episode => Assert.Equal("Yes", episode.Fresh.Answer));
        Assert.Equal(new[] { "No", "Yes", "No", "Yes", "Yes" }, episodes.Select(episode => episode.Followup.Answer));
        Assert.Equal(new[] { 1, 1, 2, 1, 1 }, episodes.Select(episode => FiniteLaw.Entries(episode.Memory.Pins).Length));
        Assert.Equal(new[] { 2, 2, 5, 2, 2 }, episodes.Select(episode => FiniteLaw.Entries(episode.SelectedSegments).Length));
    }

    [Fact]
    public void A_negative_followup_does_not_withdraw_the_valid_match_or_mutate_its_observation()
    {
        foreach (var episode in Study.Value.Episodes)
        {
            Assert.Equal("Yes", episode.Initial.Answer); Assert.Equal("Yes", episode.Recheck.Answer);
            Assert.True(episode.Memory.IsValid);
            Assert.Same(episode.Memory, episode.Followup.Retained);
            Assert.Same(episode.Memory, episode.Recheck.Retained);
            Assert.Same(episode.Initial.Output.Observation, episode.Followup.Output.Observation);
            Assert.Same(episode.Initial.Output.Observation, episode.Recheck.Output.Observation);
            Assert.All(FiniteLaw.Entries(episode.SelectedSegments), edge =>
                Assert.Contains(FiniteLaw.Entries(episode.Case.Source.Observation.Segments), actual => ReferenceEquals(actual, edge)));
        }
    }

    [Fact]
    public void Unfinished_changed_event_and_unadmitted_role_controls_stay_distinct()
    {
        var controls = Study.Value.Controls;
        Assert.Equal(new[] { "No", "Open", "Open", "Open", "Blocked" }, controls.Select(control => control.Read.Answer));
        Assert.NotEmpty(FiniteLaw.Entries(controls[1].Read.Stale));
        Assert.Empty(FiniteLaw.Entries(controls[1].Read.Refuted));
        Assert.Empty(FiniteLaw.Entries(controls[2].Read.Stale));
        Assert.Empty(FiniteLaw.Entries(controls[2].Read.Refuted));
        Assert.Empty(FiniteLaw.Entries(controls[3].Read.Consumed));
        Assert.NotEmpty(FiniteLaw.Entries(controls[4].Read.Blocked));
    }
}
