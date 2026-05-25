using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Tests.Equations;

public sealed class EquationFanoutTests
{
    [Fact]
    public void FanoutReadsSameInputThroughMultipleBranches()
    {
        var input = new Carrier();
        var percent = new Carrier();
        var inverse = new Carrier();
        var percentBranch = Equation.Affine(input, 0, 10, percent, 0, 100);
        var inverseBranch = Equation.Affine(input, 0, 10, inverse, 100, 0);
        var fanout = EquationFanout.Create(new Carrier(), percentBranch, inverseBranch);

        var prediction = EquationFanout.Predict(fanout, new CarrierLocation(input, 3), out var branchReads);

        Assert.True(EquationFanout.TryReadBranches(fanout, out var branches));
        Assert.Equal(new[] { percentBranch, inverseBranch }, branches);
        Assert.Equal(2, branchReads.Length);
        Assert.True(EquationFanout.TryReadPrediction(prediction, out var outputs, out var residues));
        Assert.Equal(new IElement[]
        {
            new CarrierLocation(percent, 30),
            new CarrierLocation(inverse, 70)
        }, outputs);
        Assert.Equal(2, residues.Length);
    }

    [Fact]
    public void FanoutOutputCanFeedMergeSurface()
    {
        var input = new Carrier();
        var scaled = new Carrier();
        var doubled = new Carrier();
        var score = new Carrier();
        var scaleBranch = Equation.Affine(input, 0, 10, scaled, 0, 100);
        var doubleBranch = Equation.Affine(input, 0, 1, doubled, 0, 2);
        var fanout = EquationFanout.Create(new Carrier(), scaleBranch, doubleBranch);
        var merge = Equation.Sum(score, Equation.UnitPort(scaled), Equation.UnitPort(doubled));

        var branchPrediction = EquationFanout.Predict(fanout, new CarrierLocation(input, 4), out _);
        var merged = Equation.Predict(merge, branchPrediction.Left, score);

        Assert.True(Equation.TryReadPrediction(merged, out var predicted, out _));
        Assert.Equal(new CarrierLocation(score, 48), predicted);
    }

    [Fact]
    public void SequenceCanFanOutThenMerge()
    {
        var input = new Carrier();
        var scaled = new Carrier();
        var doubled = new Carrier();
        var score = new Carrier();
        var fanout = EquationFanout.Create(
            new Carrier(),
            Equation.Affine(input, 0, 10, scaled, 0, 100),
            Equation.Affine(input, 0, 1, doubled, 0, 2));
        var merge = Equation.Sum(score, Equation.UnitPort(scaled), Equation.UnitPort(doubled));
        var sequence = EquationSequence.Create(new Carrier(), fanout, merge);

        var prediction = EquationSequence.Predict(sequence, new CarrierLocation(input, 4), score, out var stepReads);

        Assert.Equal(2, stepReads.Length);
        Assert.True(EquationFanout.TryReadPrediction(stepReads[0], out var branchOutputs, out _));
        Assert.Equal(2, branchOutputs.Length);
        Assert.True(Equation.TryReadPrediction(prediction, out var predicted, out _));
        Assert.Equal(new CarrierLocation(score, 48), predicted);
    }
}
