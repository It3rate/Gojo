using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace QualitativeExecution.Tests;

public sealed class RetainedMatchTests
{
    [Fact]
    public void A_legacy_whole_footprint_witness_also_retains_its_full_site_correspondence()
    {
        var example = RetainedMatchCases.Build().Cases[0];
        var question = new C.Question("whole-footprint read", example.InitialQuestion.Roles);
        var initial = new C.Assessment(example.Source.Induction, question);
        var witness = Assert.Single(initial.Witnesses);
        var memory = RetainedMatch.Capture(initial, witness, S(question.Roles.Select(role => (IElement)role.Slot).ToArray()));
        Assert.True(memory.IsValid);
        Assert.Equal(question.Roles.Count, E(memory.Pins).Length);
        foreach (var pin in E(memory.Pins).Cast<Relation>())
        {
            var row = Assert.IsType<Relation>(pin.Right);
            var scope = Assert.IsType<Relation>(Assert.IsType<Relation>(row.Right).Right);
            Assert.NotEmpty(E(scope));
            Assert.All(E(scope), item => Assert.IsType<Relation>(item));
        }
        Assert.Equal("Yes", new C.Assessment(example.Source.Induction, question, retained: memory).Answer);
    }

    [Fact]
    public void A_witness_from_another_assessment_is_not_accepted_as_the_selected_certificate()
    {
        var example = RetainedMatchCases.Build().Cases[0];
        var first = new C.Assessment(example.Source.Induction, example.InitialQuestion);
        var other = new C.Assessment(example.Source.Induction, example.InitialQuestion);
        var invalid = RetainedMatch.Capture(first, Assert.Single(other.Witnesses), example.RetainedRoles);
        Assert.False(invalid.IsValid);
        Assert.NotEmpty(E(invalid.Blocked));
        Assert.Equal("Blocked", new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: invalid).Answer);
    }

    [Fact]
    public void A_followup_cannot_silently_drop_a_retained_role()
    {
        var example = RetainedMatchCases.Build().Cases[0];
        var initial = new C.Assessment(example.Source.Induction, example.InitialQuestion);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), example.RetainedRoles);
        var remaining = example.FollowupQuestion.Roles.Where(role => !E(example.RetainedRoles)
            .Any(slot => ReferenceEquals(slot, role.Slot))).ToArray();
        var question = new C.Question("dropped role", remaining);
        var assessment = new C.Assessment(example.Source.Induction, question, retained: memory);
        Assert.Equal("Blocked", assessment.Answer);
        Assert.Empty(E(assessment.Refuted));
        Assert.Empty(E(assessment.Stale));
    }

    [Fact]
    public void Retained_identity_does_not_remove_a_finite_followup_assignment_budget()
    {
        var example = RetainedMatchCases.Build().Cases[1];
        var initial = new C.Assessment(example.Source.Induction, example.InitialQuestion);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), example.RetainedRoles);
        var cut = new C.Assessment(example.Source.Induction, example.FollowupQuestion, assignmentCapacity: 0, retained: memory);
        Assert.Equal("Open", cut.Answer);
        Assert.Empty(E(cut.Refuted));
        Assert.Empty(cut.Witnesses);
        var mappedCut = new C.Assessment(example.Source.Induction, example.FollowupQuestion, correspondenceCapacity: 0, retained: memory);
        Assert.Equal("Open", mappedCut.Answer);
        Assert.Empty(E(mappedCut.Refuted));
    }

    [Fact]
    public void A_later_successful_read_can_explicitly_capture_the_same_selected_path_again()
    {
        var example = RetainedMatchCases.Build().Cases[1];
        var initial = new C.Assessment(example.Source.Induction, example.InitialQuestion);
        var memory = RetainedMatch.Capture(initial, Assert.Single(initial.Witnesses), example.RetainedRoles);
        var second = new C.Assessment(example.Source.Induction, example.FollowupQuestion, retained: memory);
        Assert.Equal("Yes", second.Answer);
        var laterMemory = RetainedMatch.Capture(second, Assert.Single(second.Witnesses), example.RetainedRoles);
        Assert.True(laterMemory.IsValid);
        var third = new C.Assessment(example.Source.Induction, example.InitialQuestion, retained: laterMemory);
        Assert.Equal("Yes", third.Answer);
        Assert.Same(example.Source.Observation.Surface, laterMemory.Observation);
        Assert.Same(second.Surface, laterMemory.SourceAssessment);
    }

    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
