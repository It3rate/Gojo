using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class PolynomialExactnessTests
{
    [Fact]
    public void DistinctExponentVectorsCannotCancelThroughAPackedKey()
    {
        var count = Shape.Unary(2);
        var equation = new Polynomial([count, count], (1, [1, 0]), (-1, [0, 64]));

        Assert.Equal(2, equation.Terms.Count);
        Assert.True(equation.Relation.Admits(1, 1));
        Assert.False(equation.Relation.Admits(1, 0));
    }

    [Fact]
    public void ExactAdmissibilityDoesNotWrapWhileValueChecksItsFinalRange()
    {
        var count = Shape.Unary(2);
        var power = new Polynomial([count], (1, [64]));
        Assert.Equal(1, power.Relation.AdmittedCount);
        Assert.True(power.Relation.Admits(0));
        Assert.False(power.Relation.Admits(2));
        Assert.False(power.Holds([2]));
        Assert.Throws<OverflowException>(() => power.Value([2]));

        var difference = new Polynomial([count, count], (1, [64, 0]), (-1, [0, 64]));
        Assert.True(difference.Relation.Admits(2, 2));
        Assert.Equal(0, difference.Value([2, 2]));
    }

    [Fact]
    public void CoefficientsReduceBeforeTheirCanonicalRangeIsChecked()
    {
        Shape[] bits = [Shape.Bit, Shape.Bit];
        var merged = new Polynomial(bits, (int.MaxValue, [1, 0]), (1, [1, 0]), (int.MinValue, [0, 1]));
        Assert.True(merged.Same(Polynomial.Balance(bits, 1, -1)));

        var minimum = new Polynomial(bits, (1, [1, 0]), (int.MinValue, [0, 1]));
        Assert.Equal(int.MinValue, minimum.Terms[1].Weight);
        Assert.Equal(43, minimum.Bits);
        Assert.Equal("a − 2147483648b = 0", new Lens().Describe(minimum));
        Assert.Throws<OverflowException>(() => new Polynomial(bits, (int.MinValue, [1, 0]), (1, [0, 1])));
    }

    [Fact]
    public void RecognitionCannotPredictOrFitAWrappedHighPower()
    {
        Shape[] roles = [Shape.Unary(2), Shape.Bit];
        var equation = new Polynomial(roles, (1, [64, 0]), (-1, [0, 1]));
        Assert.Equal(1, Recognition.Predict(equation, 1, [1, -1]));
        Assert.Null(Recognition.Predict(equation, 1, [2, -1]));
        Assert.Empty(Recognition.Fits(roles, 1, new int[][] { [1, 1], [2, 0] }, maxTerms: 1, maxDegree: 64, maxWeight: 1));
    }

    [Fact]
    public void BindingANonlinearEquationNeedsNoRepresentableTotalDegree()
    {
        var bit = Shape.Bit;
        var equation = new Polynomial([bit, bit, bit], (1, [int.MaxValue, int.MaxValue, 0]), (-1, [0, 0, 1]));
        var bound = Operations.Bind(equation, [bit, bit, bit], [0, 1, -1], [-1, -1, 2]);

        Assert.True(bound.Accounting.Functional);
        Assert.False(bound.Accounting.Conserves(2));
        Assert.Equal([1, 1, 1], bound.Tuple(Assert.Single(bound.Afters(bound.TupleIndex([1, 1, 0])))));
    }
}
