using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace Gojo.Tests.Equations;

public sealed class EquationSequenceTests
{
    [Fact]
    public void SequenceFeedsOneAffineReadIntoTheNext()
    {
        var input = new Carrier();
        var normalized = new Carrier();
        var output = new Carrier();
        var first = Equation.Affine(input, 0, 10, normalized, 0, 100);
        var second = Equation.Affine(normalized, 0, 100, output, 0, 5);
        var sequenceRoot = new Carrier();
        var sequence = EquationSequence.Create(sequenceRoot, first, second);

        var prediction = EquationSequence.Predict(sequence, new CarrierLocation(input, 6), output, out var stepReads);

        Assert.True(EquationSequence.TryReadSteps(sequence, out var steps));
        Assert.Equal(new[] { first, second }, steps);
        Assert.Equal(2, stepReads.Length);
        Assert.True(Equation.TryReadPrediction(stepReads[0], out var middle, out _));
        Assert.Equal(new CarrierLocation(normalized, 60), middle);
        Assert.True(Equation.TryReadPrediction(prediction, out var final, out var residue));
        Assert.Equal(new CarrierLocation(output, 3), final);
        Assert.IsType<Relation>(residue);
    }

    [Fact]
    public void SequenceCanFeedProductOutputIntoAffineRead()
    {
        var width = new Carrier();
        var height = new Carrier();
        var area = new Carrier();
        var score = new Carrier();
        var product = Equation.Product(area, Equation.UnitPort(width), Equation.UnitPort(height));
        var areaToScore = Equation.Affine(area, 0, 100, score, 0, 10);
        var sequence = EquationSequence.Create(new Carrier(), product, areaToScore);

        var prediction = EquationSequence.Predict(
            sequence,
            new Relation(new CarrierLocation(width, 8), new CarrierLocation(height, 5)),
            score,
            out var stepReads);

        Assert.Equal(2, stepReads.Length);
        Assert.True(Equation.TryReadPrediction(stepReads[0], out var productRead, out _));
        Assert.Equal(new CarrierLocation(area, 40), productRead);
        Assert.True(Equation.TryReadPrediction(prediction, out var final, out _));
        Assert.Equal(new CarrierLocation(score, 4), final);
    }

    [Fact]
    public void EmptySequencePreservesInputAsUnresolvedResidue()
    {
        var input = new Carrier();
        var output = new Carrier();
        var sequence = EquationSequence.Create(new Carrier());
        var supplied = new CarrierLocation(input, 7);

        var prediction = EquationSequence.Predict(sequence, supplied, output, out var stepReads);

        Assert.Empty(stepReads);
        Assert.True(Equation.TryReadPrediction(prediction, out var final, out var residue));
        Assert.Equal(new CarrierLocation(output, 0), final);
        Assert.Equal(supplied, residue);
    }

    [Fact]
    public void SequencePredictionCanBeReadAsRepeatSurface()
    {
        var input = new Carrier();
        var normalized = new Carrier();
        var output = new Carrier();
        var repeatAxis = new Carrier();
        var first = Equation.Affine(input, 0, 10, normalized, 0, 100);
        var second = Equation.Affine(normalized, 0, 100, output, 0, 5);
        var sequence = EquationSequence.Create(new Carrier(), first, second);
        var supplied = new CarrierLocation(input, 6);

        var repeat = EquationSequence.PredictRepeat(
            sequence,
            supplied,
            output,
            repeatAxis,
            out var prediction,
            out var stepReads);

        Assert.True(RepeatSurface.TryRead(
            repeat,
            out var selected,
            out var controller,
            out var transition,
            out var feedback,
            out var residue,
            out var timeline));
        Assert.Equal(sequence, selected);
        Assert.True(RepeatSurface.TryReadSpan(controller, out var start, out var end));
        Assert.Equal(new CarrierLocation(repeatAxis, 0), start);
        Assert.Equal(new CarrierLocation(repeatAxis, 2), end);
        Assert.Equal(new IElement[] { first, second }, OrderedElements(Assert.IsType<Relation>(transition)));
        Assert.True(Equation.TryReadPrediction(stepReads[0], out var middle, out _));
        Assert.True(Equation.TryReadPrediction(prediction, out var final, out var predictionResidue));
        Assert.Equal(new IElement[] { middle, final }, OrderedElements(Assert.IsType<Relation>(feedback)));
        Assert.Equal(predictionResidue, residue);

        var timelineRelation = Assert.IsType<Relation>(timeline);
        Assert.True(TimeStepSequence.TryRead(timelineRelation, out var origin, out var initial, out var timedSteps));
        Assert.Equal(new CarrierLocation(repeatAxis, 0), origin);
        Assert.Equal(supplied, initial);
        Assert.Equal(2, timedSteps.Length);
        Assert.True(TimeStepSequence.TryReadStep(
            timedSteps[0],
            out _,
            out var firstStart,
            out var firstEnd,
            out var firstBefore,
            out var firstTransition,
            out var firstAfter,
            out _));
        Assert.Equal(new CarrierLocation(repeatAxis, 0), firstStart);
        Assert.Equal(new CarrierLocation(repeatAxis, 1), firstEnd);
        Assert.Equal(supplied, firstBefore);
        Assert.Equal(first, firstTransition);
        Assert.Equal(middle, firstAfter);

        Assert.True(TimeStepSequence.TryReadStep(
            timedSteps[1],
            out _,
            out var secondStart,
            out var secondEnd,
            out var secondBefore,
            out var secondTransition,
            out var secondAfter,
            out _));
        Assert.Equal(new CarrierLocation(repeatAxis, 1), secondStart);
        Assert.Equal(new CarrierLocation(repeatAxis, 2), secondEnd);
        Assert.Equal(middle, secondBefore);
        Assert.Equal(second, secondTransition);
        Assert.Equal(final, secondAfter);
    }

    private static IElement[] OrderedElements(Relation structure)
    {
        var holder = Assert.IsType<Carrier>(structure.Right);
        Assert.True(structure.TryReadOrderedElementsOn(holder, out var elements));
        return elements;
    }
}
