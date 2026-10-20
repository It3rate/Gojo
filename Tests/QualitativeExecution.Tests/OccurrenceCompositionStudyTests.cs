using Gojo.Experiments.QualitativeFrieze;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.QualitativeExecution.Tests;

public sealed class OccurrenceCompositionStudyTests
{
    private static readonly Lazy<OccurrenceCompositionStudy> Study = new(OccurrenceCompositionStudy.Build);
    private static readonly Lazy<OccurrenceCompositionStudy> Landmarks = new(OccurrenceCompositionStudy.BuildLandmarks);

    [Fact]
    public void Stronger_role_questions_refine_the_same_candidate_library()
    {
        var episode = Study.Value.Episodes.Single(item => item.Case.Id == "C01");
        Assert.Equal(new[] { 3, 3, 2, 1 }, episode.Stages.Select(stage => stage.Compatible.Count));
        Assert.All(episode.EarlierVocabulary, comparison => Assert.True(comparison.IsCompatibleAtVocabulary));
        Assert.Equal(new[] { "Yes", "Yes", "No" }, episode.Reads.Select(read => read.Source.Answer));
        Assert.Equal(1, Assert.Single(episode.Stages[^1].Compatible).Index);
    }

    [Fact]
    public void Known_negative_answers_agree_without_becoming_positive_shape_claims()
    {
        var episode = Study.Value.Episodes.Single(item => item.Case.Id == "C01");
        var last = episode.Reads[^1]; var candidate = episode.Stages[^1].Candidates[1];
        Assert.Equal("No", last.Source.Answer); Assert.Equal("No", last.Candidates[1].Answer);
        Assert.NotEmpty(FiniteLaw.Entries(last.Source.Refuted));
        Assert.Empty(FiniteLaw.Entries(last.Source.Supported));
        Assert.Equal(3, FiniteLaw.Entries(candidate.Agreement).Length);
        Assert.True(candidate.IsSettled);
    }

    [Fact]
    public void Four_distinctions_settle_and_unpublished_spacing_keeps_both_alternatives()
    {
        var study = Study.Value;
        Assert.Equal(new[] { 1, 1, 1, 1, 2 }, study.Episodes.Select(item => item.Stages[^1].Compatible.Count));
        Assert.All(study.Episodes, episode => Assert.All(episode.Stages[^1].Compatible,
            candidate => Assert.True(candidate.IsSettled)));
        Assert.True(study.Episodes[^1].Case.ExpectedAmbiguity);
    }

    [Fact]
    public void Different_exhausted_search_stages_remain_open()
    {
        Assert.Equal(2, Study.Value.Controls.Count);
        Assert.All(Study.Value.Controls, control => {
            Assert.Equal("Open", control.Read.Answer);
            Assert.Empty(FiniteLaw.Entries(control.Read.Supported));
            Assert.Empty(FiniteLaw.Entries(control.Read.Refuted));
        });
    }

    [Fact]
    public void Bounded_joint_search_caps_assignments_without_false_absence()
    {
        var scaling = Study.Value.Scaling;
        Assert.Equal(20, scaling.Count);
        Assert.All(scaling, row => Assert.InRange(row.Attempts, 0, row.AssignmentCapacity));
        var large = scaling.Where(row => row.Copies == 16 && row.Question == "Two loops sharing a site").ToArray();
        Assert.Equal("Open", large.Single(row => row.AssignmentCapacity == 8).Answer);
        Assert.Equal("No", large.Single(row => row.AssignmentCapacity == 4096).Answer);
        Assert.All(scaling.Where(row => row.Copies >= 2 && row.Question == "Two site-disjoint loops"),
            row => Assert.Equal("Yes", row.Answer));
    }

    [Fact]
    public void Landmark_questions_refine_existing_contact_without_replacing_parts_or_observation()
    {
        var study = Landmarks.Value;
        Assert.Equal(new[] { 1, 1, 1, 2 }, study.Episodes.Select(episode => episode.Stages[^1].Compatible.Count));
        Assert.Equal(new[] { 3, 2, 1 }, study.Episodes[0].Stages.Select(stage => stage.Compatible.Count));
        foreach (var episode in study.Episodes)
        {
            var coarse = episode.Reads[0]; var fine = episode.Reads[1];
            Assert.Equal("Yes", coarse.Source.Answer); Assert.Equal("Yes", fine.Source.Answer);
            Assert.Same(coarse.Source.Output.Observation, fine.Source.Output.Observation);
            foreach (var role in coarse.Question.Roles)
                Assert.Contains(fine.Question.Roles, candidate => ReferenceEquals(candidate.Slot, role.Slot));
            Assert.NotEmpty(fine.Source.Witnesses);
            Assert.NotEmpty(FiniteLaw.Entries(fine.Source.Witnesses[0].SelectedSites));
        }
    }

    [Fact]
    public void Landmark_controls_separate_inventory_assignment_correspondence_and_open_selection()
    {
        var controls = Landmarks.Value.Controls;
        Assert.Equal(4, controls.Count);
        Assert.All(controls, control => {
            Assert.Equal("Open", control.Read.Answer);
            Assert.Empty(FiniteLaw.Entries(control.Read.Refuted));
            Assert.Empty(FiniteLaw.Entries(control.Read.Blocked));
        });
        Assert.Empty(FiniteLaw.Entries(controls[2].Read.CorrespondenceBudget));
        Assert.NotEmpty(FiniteLaw.Entries(controls[3].Read.Question.Unresolved));
    }

    [Fact]
    public void Landmark_correspondences_have_a_separate_accounted_budget()
    {
        foreach (var episode in Landmarks.Value.Episodes)
        {
            var coarse = episode.Reads[0].Source; var fine = episode.Reads[1].Source;
            Assert.Empty(FiniteLaw.Entries(coarse.CorrespondenceBudget));
            Assert.NotEmpty(FiniteLaw.Entries(fine.CorrespondenceConsumed));
            Assert.Equal(FiniteLaw.Entries(fine.CorrespondenceBudget).Length,
                FiniteLaw.Entries(fine.CorrespondenceConsumed).Length + FiniteLaw.Entries(fine.CorrespondenceRemaining).Length);
        }
        Assert.Empty(Landmarks.Value.Scaling);
    }
}
