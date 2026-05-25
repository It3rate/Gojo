using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Tests.Equations;

public sealed class EquationWitnessTests
{
    [Fact]
    public void EquationPredictionCanBeReadAsCommonWitness()
    {
        var input = new Carrier();
        var output = new Carrier();
        var equation = Equation.Affine(input, 0, 10, output, 0, 100);
        var sample = new CarrierLocation(input, 3);

        var witness = Equation.PredictWitness(new Carrier(), equation, sample, output);

        Assert.True(Equation.TryReadPredictionWitness(
            witness,
            out var selectedInput,
            out var readEquation,
            out var predicted,
            out var residue,
            out var provenance));
        Assert.Equal(sample, Assert.IsType<CarrierLocation>(selectedInput));
        Assert.Equal(equation, readEquation);
        Assert.Equal(new CarrierLocation(output, 30), predicted);
        Assert.Equal(new CarrierLocation(input, 0), Assert.IsType<CarrierLocation>(residue));
        Assert.True(Equation.TryReadPrediction(
            Assert.IsType<Relation>(provenance),
            out var provenancePrediction,
            out _));
        Assert.Equal(predicted, provenancePrediction);
    }
}
