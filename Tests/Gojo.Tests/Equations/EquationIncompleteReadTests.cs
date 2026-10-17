using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Tests.Equations;

public sealed class EquationIncompleteReadTests
{
    [Fact]
    public void MissingOperandNeverBecomesNumericZeroThroughPredictionOrInspection()
    {
        var axis = new Carrier();
        var equation = Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis));
        var input = new Placeholder();
        Assert.False(Equation.TryPredict(equation, input, axis, out var attempted));
        Assert.False(Equation.TryReadPrediction(attempted, out _, out _));
        Assert.False(Equation.TryReadPrediction(Equation.Predict(equation, input, axis), out _, out _));
        var residue = Assert.IsType<Relation>(attempted.Right);
        Assert.Same(equation, residue.Left); Assert.Same(input, residue.Right);
        var restored = (Relation)GojoEncodedSnapshot.CaptureArchive(attempted).RestoreRoots()[0];
        Assert.False(Equation.TryReadPrediction(restored, out _, out _));
    }

    [Fact]
    public void FailedProjectionRetainsSurfaceInputAndTargetWithoutZeroAnswer()
    {
        var source = new Carrier(); var target = new Carrier(); var unrelated = new Carrier();
        var equation = Equation.Affine(source, 0, 12, target, 0, 120);
        var input = new CarrierLocation(unrelated, 4);
        Assert.False(equation.TryProject(input, target, out var projection));
        Assert.False(Equation.TryReadPrediction(projection, out _, out _));
        Assert.False(Equation.TryReadPrediction(equation.Project(input, target), out _, out _));
        Assert.False(Equation.TryPredict(equation, input, target, out var prediction));
        Assert.False(Equation.TryReadPrediction(prediction, out _, out _));
        Assert.Same(target, Assert.IsType<Relation>(prediction.Left).Right);
    }

    [Fact]
    public void FailureSurvivesEquationCompositionWhileMeasuredZeroRemainsReadable()
    {
        var axis = new Carrier();
        var sum = Equation.Sum(axis, Equation.UnitPort(axis), Equation.UnitPort(axis));
        var sequence = EquationSequence.Create(new Carrier(), sum, Equation.Affine(axis, 0, 1, axis, 0, 10));
        Assert.False(Equation.TryPredict(sequence, new Placeholder(), axis, out var failed));
        Assert.False(Equation.TryReadPrediction(failed, out _, out _));
        var complete = new Relation(new CarrierLocation(axis, 2), new CarrierLocation(axis, -2));
        Assert.True(Equation.TryPredict(sequence, complete, axis, out var known));
        Assert.True(Equation.TryReadPrediction(known, out var value, out _));
        Assert.Equal(0, value.Ticks);
    }
}
