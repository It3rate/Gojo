using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class RepeatSurfaceTests
{
    [Fact]
    public void RepeatTemplateReadsInputPortsSelectedStructureAndOutputPorts()
    {
        var current = new Placeholder();
        var increment = new Placeholder();
        var next = new Placeholder();
        var inputs = RepeatSurface.Ports(new Carrier(), current, increment);
        var selected = new Relation(new Relation(current, increment), next);
        var outputs = RepeatSurface.Ports(new Carrier(), next);

        var template = RepeatSurface.Template(new Carrier(), inputs, selected, outputs);

        Assert.True(RepeatSurface.TryReadTemplate(
            template,
            out var readInputs,
            out var readSelected,
            out var readOutputs));
        Assert.Equal(selected, readSelected);
        Assert.True(RepeatSurface.TryReadPorts(readInputs, out var inputPorts));
        Assert.Equal([current, increment], inputPorts);
        Assert.True(RepeatSurface.TryReadPorts(readOutputs, out var outputPorts));
        Assert.Equal([next], outputPorts);
    }

    [Fact]
    public void FeedbackCanFeedOutputPortIntoNextInputFrame()
    {
        var current = new Placeholder();
        var increment = new Placeholder();
        var next = new Placeholder();
        var zero = Relation.Point(new Carrier(), 0);
        var three = Relation.Point(new Carrier(), 3);
        var initial = new[]
        {
            StructuralPattern.Bind(current, zero),
            StructuralPattern.Bind(increment, three)
        };
        var transition = new[]
        {
            StructuralPattern.Bind(current, current),
            StructuralPattern.Bind(increment, increment),
            StructuralPattern.Bind(next, new Relation(current, increment))
        };
        var feedback = new[]
        {
            RepeatSurface.Feed(next, current)
        };
        var data = new[]
        {
            StructuralPattern.Bind(increment, increment)
        };

        var firstInstance = RepeatSurface.ApplyTransitionFrame(initial, transition);
        Assert.True(StructuralPattern.TryReadBoundSlot(next, firstInstance, out var firstNext));
        Assert.Equal(new Relation(zero, three), firstNext);

        var secondInput = RepeatSurface.ApplyFeedbackFrame(firstInstance, feedback, data);
        Assert.True(StructuralPattern.TryReadBoundSlot(current, secondInput, out var secondCurrent));
        Assert.Equal(firstNext, secondCurrent);
        Assert.True(StructuralPattern.TryReadBoundSlot(increment, secondInput, out var secondIncrement));
        Assert.Equal(three, secondIncrement);

        var secondInstance = RepeatSurface.ApplyTransitionFrame(secondInput, transition);
        Assert.True(StructuralPattern.TryReadBoundSlot(next, secondInstance, out var secondNext));
        Assert.Equal(new Relation(new Relation(zero, three), three), secondNext);
    }

    [Fact]
    public void RepeatSurfaceCanCarryPortTemplateAndFeedbackAsOrdinaryFields()
    {
        var current = new Placeholder();
        var increment = new Placeholder();
        var next = new Placeholder();
        var axis = RepeatSurface.CreateAxis(new Carrier());
        var inputs = RepeatSurface.Ports(new Carrier(), current, increment);
        var selected = new Relation(new Relation(current, increment), next);
        var outputs = RepeatSurface.Ports(new Carrier(), next);
        var template = RepeatSurface.Template(new Carrier(), inputs, selected, outputs);
        var transition = RepeatSurface.Frame(
            new Carrier(),
            StructuralPattern.Bind(next, new Relation(current, increment)));
        var feedback = RepeatSurface.Frame(new Carrier(), RepeatSurface.Feed(next, current));

        var repeat = RepeatSurface.Create(
            new Carrier(),
            template,
            RepeatSurface.Count(axis, 3),
            transition,
            feedback,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(RepeatSurface.TryRead(
            repeat,
            out var readSelected,
            out var readController,
            out var readTransition,
            out var readFeedback,
            out _,
            out _));
        Assert.Equal(template, readSelected);
        Assert.True(RepeatSurface.TryReadSpan(readController, out _, out var end));
        Assert.Equal(3, end.Ticks);
        Assert.True(RepeatSurface.TryReadFrame(readTransition, out var transitionBindings));
        Assert.Single(transitionBindings);
        Assert.True(RepeatSurface.TryReadFrame(readFeedback, out var feedbackBindings));
        var feed = Assert.Single(feedbackBindings);
        Assert.True(RepeatSurface.TryReadFeed(feed, out var outputPort, out var nextInputPort));
        Assert.Equal(next, outputPort);
        Assert.Equal(current, nextInputPort);
    }

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
