using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class RepeatSurfaceTests
{
    [Fact]
    public void RepeatSurfaceReadsSelectedControllerTransitionFeedbackResidueAndTimeline()
    {
        var axisBasis = new Carrier();
        var axis = RepeatSurface.CreateAxis(axisBasis);
        var selected = Relation.Segment(new Carrier(), 0, 1);
        var controller = RepeatSurface.Count(axis, 3);
        var transition = Relation.Segment(new Carrier(), 1, 2);
        var feedback = Relation.Point(new Carrier(), 0);
        var residue = Relation.Point(new Carrier(), 0);
        var timeline = TimeStepSequence.Create(
            new Carrier(),
            axis,
            selected,
            TimeStepSequence.Step(
                new Carrier(),
                axis,
                0,
                1,
                selected,
                transition,
                feedback,
                residue));

        var repeat = RepeatSurface.Create(
            new Carrier(),
            selected,
            controller,
            transition,
            feedback,
            residue,
            timeline);

        Assert.True(RepeatSurface.TryRead(
            repeat,
            out var readSelected,
            out var readController,
            out var readTransition,
            out var readFeedback,
            out var readResidue,
            out var readTimeline));
        Assert.Equal(selected, readSelected);
        Assert.Equal(controller, readController);
        Assert.Equal(transition, readTransition);
        Assert.Equal(feedback, readFeedback);
        Assert.Equal(residue, readResidue);
        Assert.Equal(timeline, readTimeline);
        Assert.True(RepeatSurface.TryReadSpan(readController, out var start, out var end));
        Assert.Equal(new CarrierLocation(axis, 0), start);
        Assert.Equal(new CarrierLocation(axis, 3), end);
        Assert.True(RepeatSurface.TryReadTimeline(repeat, out var timelineRead));
        Assert.Equal(timeline, timelineRead);
    }

    [Fact]
    public void RepeatAxisCanShareUnitShapeWithoutSharingIdentity()
    {
        var basis = new Carrier();
        var first = RepeatSurface.CreateAxis(basis);
        var second = RepeatSurface.CreateAxis(basis);

        Assert.False(first.SameIdentityAs(second));
        Assert.True(first.UnitShape.CompatibleWith(second.UnitShape));
    }

    [Fact]
    public void ControllerCanBeEquationShapedInsteadOfLiteralCount()
    {
        var repeatAxis = new Carrier();
        var count = new Carrier();
        var selected = Relation.Point(new Carrier(), 1);
        var controller = new Relation(
            Relation.Point(repeatAxis, 0),
            Relation.Point(count, 0));

        var repeat = RepeatSurface.Create(
            new Carrier(),
            selected,
            controller,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(RepeatSurface.TryRead(
            repeat,
            out _,
            out var readController,
            out _,
            out _,
            out _,
            out _));
        Assert.Equal(controller, readController);
        Assert.False(RepeatSurface.TryReadSpan(readController, out _, out _));
    }
}
