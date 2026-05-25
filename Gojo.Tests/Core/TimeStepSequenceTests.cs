using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class TimeStepSequenceTests
{
    [Fact]
    public void StepReadsTimeSpanAndTransitionEvidence()
    {
        var time = new Carrier();
        var value = new Carrier();
        var before = Relation.Point(value, 2);
        var transition = Relation.Segment(value, 2, 5);
        var after = Relation.Point(value, 5);
        var residue = Relation.Point(value, 0);

        var step = TimeStepSequence.Step(
            new Carrier(),
            time,
            startTicks: 7,
            endTicks: 8,
            before,
            transition,
            after,
            residue);

        Assert.True(TimeStepSequence.TryReadStep(
            step,
            out var span,
            out var start,
            out var end,
            out var readBefore,
            out var readTransition,
            out var readAfter,
            out var readResidue));
        Assert.Equal(Relation.Segment(time, 7, 8), span);
        Assert.Equal(new CarrierLocation(time, 7), start);
        Assert.Equal(new CarrierLocation(time, 8), end);
        Assert.Equal(before, readBefore);
        Assert.Equal(transition, readTransition);
        Assert.Equal(after, readAfter);
        Assert.Equal(residue, readResidue);
    }

    [Fact]
    public void SequenceReadsClockOriginInitialStateAndOrderedSteps()
    {
        var time = new Carrier();
        var value = new Carrier();
        var initial = Relation.Point(value, 0);
        var first = TimeStepSequence.Step(
            new Carrier(),
            time,
            0,
            1,
            initial,
            Relation.Segment(value, 0, 4),
            Relation.Point(value, 4),
            Relation.Point(value, 0));
        var second = TimeStepSequence.Step(
            new Carrier(),
            time,
            1,
            2,
            Relation.Point(value, 4),
            Relation.Segment(value, 4, 6),
            Relation.Point(value, 6),
            Relation.Point(value, 0));

        var sequence = TimeStepSequence.Create(new Carrier(), time, initial, first, second);

        Assert.True(TimeStepSequence.TryRead(sequence, out var origin, out var readInitial, out var steps));
        Assert.Equal(new CarrierLocation(time, 0), origin);
        Assert.Equal(initial, readInitial);
        Assert.Equal(new[] { first, second }, steps);
    }

    [Fact]
    public void SeparateClocksCanShareTheSameTimeUnitShape()
    {
        var timeBasis = new Carrier();
        var firstClock = TimeStepSequence.CreateClock(timeBasis);
        var secondClock = TimeStepSequence.CreateClock(timeBasis);

        Assert.False(firstClock.SameIdentityAs(secondClock));
        Assert.True(firstClock.UnitShape.CompatibleWith(secondClock.UnitShape));

        var firstStep = TimeStepSequence.Step(
            new Carrier(),
            firstClock,
            0,
            1,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondStep = TimeStepSequence.Step(
            new Carrier(),
            secondClock,
            0,
            1,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(TimeStepSequence.TryReadStep(firstStep, out _, out var firstStart, out _, out _, out _, out _, out _));
        Assert.True(TimeStepSequence.TryReadStep(secondStep, out _, out var secondStart, out _, out _, out _, out _, out _));
        Assert.True(firstStart.Carrier.UnitShape.CompatibleWith(secondStart.Carrier.UnitShape));
        Assert.False(firstStart.Carrier.SameIdentityAs(secondStart.Carrier));
    }
}
