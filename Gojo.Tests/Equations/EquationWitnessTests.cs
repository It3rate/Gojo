using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

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

    [Fact]
    public void EquationMapSampleCanBeReadAsCommonWitness()
    {
        var input = new Carrier();
        var output = new Carrier();
        var equation = Equation.Affine(input, 0, 10, output, 0, 100);
        var samples = new[] { new CarrierLocation(input, 3) };

        EquationInspection.Map(new Carrier(), equation, samples, output, out var mappedSamples);

        Assert.True(EquationInspection.TryReadPredictionSampleWitness(
            mappedSamples[0],
            equation,
            new Carrier(),
            out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var selected,
            out var readSurface,
            out var result,
            out var residueOrTension,
            out var provenance));
        Assert.Equal(samples[0], Assert.IsType<CarrierLocation>(selected));
        Assert.Equal(equation, readSurface);
        Assert.Equal(new CarrierLocation(output, 30), Assert.IsType<CarrierLocation>(result));
        Assert.Equal(new CarrierLocation(input, 0), Assert.IsType<CarrierLocation>(residueOrTension));
        Assert.True(Equation.TryReadPrediction(
            Assert.IsType<Relation>(provenance),
            out var provenancePrediction,
            out _));
        Assert.Equal(result, provenancePrediction);
    }
}
