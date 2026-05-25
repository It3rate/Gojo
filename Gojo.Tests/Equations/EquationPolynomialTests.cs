using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Tests.Equations;

public sealed class EquationPolynomialTests
{
    [Fact]
    public void PolynomialPredictsConstantLinearQuadraticAndCubicTerms()
    {
        var x = new Carrier();
        var output = new Carrier();
        var polynomial = EquationPolynomial.Create(new Carrier(), x, output, 2, 3, 4, 5);

        var prediction = Equation.Predict(polynomial, new CarrierLocation(x, 2), output);

        Assert.True(Equation.TryReadPrediction(prediction, out var predicted, out var residue));
        Assert.Equal(new CarrierLocation(output, 64), predicted);
        Assert.IsType<Relation>(residue);
    }

    [Fact]
    public void PolynomialTermFanoutGeneratesUnitLinearQuadraticAndCubicSurfaces()
    {
        var x = new Carrier();
        var output = new Carrier();
        var polynomial = EquationPolynomial.Create(new Carrier(), x, output, 0, 0, 0, 0);

        Assert.True(EquationPolynomial.TryReadTermFanout(polynomial, out var termFanout));
        Assert.True(EquationFanout.TryReadBranches(termFanout, out var branches));
        Assert.Equal(4, branches.Length);
        for (var degree = 0; degree < branches.Length; degree++)
        {
            Assert.True(EquationPower.TryReadPowerSurface(
                branches[degree],
                out _,
                out var controller,
                out _,
                out _,
                out _,
                out _,
                out var readInput,
                out _));
            Assert.Equal(x, readInput);
            Assert.True(EquationPower.TryReadLiteralExponent(controller, out var exponent));
            Assert.Equal(degree, exponent);
        }

        var termPrediction = EquationFanout.Predict(termFanout, new CarrierLocation(x, 3), out var branchReads);

        Assert.Equal(4, branchReads.Length);
        Assert.True(EquationFanout.TryReadPrediction(termPrediction, out var terms, out var residues));
        Assert.Equal(new[] { 1L, 3L, 9L, 27L }, terms.Cast<CarrierLocation>().Select(term => term.Ticks).ToArray());
        Assert.Equal(4, residues.Length);
    }

    [Fact]
    public void PolynomialMapRecordsPredictionsForSampleStream()
    {
        var x = new Carrier();
        var output = new Carrier();
        var polynomial = EquationPolynomial.Create(new Carrier(), x, output, 1, 2, 1);
        var inputs = new[]
        {
            new CarrierLocation(x, 0),
            new CarrierLocation(x, 1),
            new CarrierLocation(x, 2)
        };

        var map = EquationInspection.Map(new Carrier(), polynomial, inputs, output, out var samples);

        Assert.True(EquationInspection.TryReadMapSamples(map, out var readSamples));
        Assert.Equal(samples, readSamples);
        var predictions = samples
            .Select(sample =>
            {
                Assert.True(EquationInspection.TryReadPredictionSampleElement(
                    sample,
                    out _,
                    out _,
                    out var predicted,
                    out var residue));
                Assert.IsType<Relation>(residue);
                return predicted.Ticks;
            })
            .ToArray();
        Assert.Equal(new[] { 1L, 4L, 9L }, predictions);
    }

    [Fact]
    public void PolynomialPatternKeepsMissingCoefficientInspectable()
    {
        var x = new Carrier();
        var output = new Carrier();
        var pattern = EquationPolynomial.Pattern(
            new Carrier(),
            x,
            output,
            degree: 3,
            out _,
            out var coefficientSlots);
        var bindings = new[]
        {
            EquationPolynomial.BindCoefficient(coefficientSlots[0], 2),
            EquationPolynomial.BindCoefficient(coefficientSlots[1], 3),
            EquationPolynomial.BindCoefficient(coefficientSlots[3], 5)
        };

        var complete = EquationPolynomial.TryPredict(
            pattern,
            new CarrierLocation(x, 2),
            output,
            bindings,
            out var prediction,
            out var unboundSlots,
            out var stepReads);

        Assert.False(complete);
        Assert.Equal([coefficientSlots[2]], unboundSlots);
        Assert.Equal(2, stepReads.Length);
        Assert.True(EquationFanout.TryReadPrediction(stepReads[0], out var terms, out _));
        Assert.Equal(new[] { 1L, 2L, 4L, 8L }, terms.Cast<CarrierLocation>().Select(term => term.Ticks).ToArray());
        Assert.True(Equation.TryReadPrediction(prediction, out var predicted, out var residue));
        Assert.Equal(new CarrierLocation(output, 0), predicted);
        Assert.IsType<Relation>(residue);
    }
}
