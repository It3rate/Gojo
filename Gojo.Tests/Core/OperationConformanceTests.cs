using Gojo.Core;
using Gojo.Equations;
using Gojo.Reads;

namespace Gojo.Tests.Core;

public sealed class OperationConformanceTests
{
    [Fact]
    public void SignedSumsProductsAndDistributivityMatchIntegerArithmetic()
    {
        var x = new Carrier();
        var y = new Carrier(x.UnitShape);
        var result = new Carrier();
        var sum = Equation.Sum(result, Equation.UnitPort(x), Equation.UnitPort(y));
        var product = Equation.Product(result, Equation.UnitPort(x), Equation.UnitPort(y));
        for (var a = -6; a <= 6; a++)
        for (var b = -6; b <= 6; b++)
        {
            Assert.Equal(a + b, Read(sum, a, b));
            Assert.Equal(a * b, Read(product, a, b));
            for (var c = -3; c <= 3; c++)
            {
                Assert.Equal(Read(product, a, Read(sum, b, c)),
                    Read(sum, Read(product, a, b), Read(product, a, c)));
            }
        }
        Assert.Equal(0UL, result.Value);
        Assert.Equal(0, result.Tension);

        long Read(Relation operation, long a, long b)
        {
            Assert.True(operation.TryReadOperationFrom(new Relation(new CarrierLocation(x, a), new CarrierLocation(y, b)), result, out var read));
            Assert.Equal(new Relation(new CarrierLocation(x, 0), new CarrierLocation(y, 0)), read.Right);
            return Assert.IsType<CarrierLocation>(read.Left).Ticks;
        }
    }

    [Theory]
    [InlineData(long.MaxValue, 1, -1, long.MaxValue, false)]
    [InlineData(long.MinValue, -1, 1, long.MinValue, false)]
    [InlineData(long.MaxValue, 2, 0, 0, true)]
    [InlineData(long.MinValue, -1, -1, long.MinValue, true)]
    public void OnlyTheStoredOperationResultNeedsToFitTheTickRange(long a, long b, long c, long expected, bool multiply)
    {
        var x = new Carrier();
        var y = new Carrier();
        var z = new Carrier();
        var result = new Carrier();
        var ports = new[] { Equation.UnitPort(x), Equation.UnitPort(y), Equation.UnitPort(z) };
        var operation = multiply ? Equation.Product(result, ports) : Equation.Sum(result, ports);
        var input = new Relation(new Relation(new CarrierLocation(x, a), new CarrierLocation(y, b)), new CarrierLocation(z, c));
        Assert.True(operation.TryReadOperationFrom(input, result, out var read));
        Assert.Equal(expected, Assert.IsType<CarrierLocation>(read.Left).Ticks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnrepresentableFinalSumOrProductStillThrows(bool multiply)
    {
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var operation = multiply
            ? Equation.Product(result, Equation.UnitPort(x), Equation.UnitPort(y))
            : Equation.Sum(result, Equation.UnitPort(x), Equation.UnitPort(y));
        var input = new Relation(new CarrierLocation(x, long.MaxValue), new CarrierLocation(y, 2));
        Assert.Throws<OverflowException>(() => operation.TryReadOperationFrom(input, result, out _));
        Assert.Equal(0UL, result.Value);
        Assert.Equal(0, result.Tension);
    }

    [Fact]
    public void WideOutputBasisChecksTheFinalCoordinateAfterItsOffset()
    {
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var operation = new Relation(new Relation(Equation.UnitPort(x), Equation.UnitPort(y)),
            Relation.Segment(result, long.MinValue, long.MaxValue));
        var input = new Relation(new CarrierLocation(x, 1), new CarrierLocation(y, 1));
        Assert.True(operation.TryReadOperationFrom(input, result, out var read));
        Assert.Equal(long.MaxValue, Assert.IsType<CarrierLocation>(read.Left).Ticks);
    }

    [Fact]
    public void SumKeepsFractionalResiduePerOperandInsteadOfInventingOutputPrecision()
    {
        var x = new Carrier();
        var y = new Carrier();
        var result = new Carrier();
        var sum = Equation.Sum(result, Equation.WeightedPort(x, 0, 3, 0, 10), Equation.UnitPort(y));
        Assert.True(sum.TryReadOperationFrom(new Relation(new CarrierLocation(x, 1), new CarrierLocation(y, 4)), result, out var read));
        Assert.Equal(7, Assert.IsType<CarrierLocation>(read.Left).Ticks);
        var residues = Assert.IsType<Relation>(read.Right);
        Assert.True(ProjectionResidue.TryRead(residues.Left, out var source, out var n, out var d));
        Assert.Same(x, source);
        Assert.Equal(1, n);
        Assert.Equal(10, d);
        Assert.Equal(new CarrierLocation(y, 0), residues.Right);
    }

    [Fact]
    public void ExpandedOpposedFaceProductCanBeFoldedThroughItsRetainedSources()
    {
        var x = new Carrier();
        var y = new Carrier(x.UnitShape);
        var area = new Carrier(UnitShape.Product(x.UnitShape, y.UnitShape));
        var product = Equation.Product(area, Equation.UnitPort(x), Equation.UnitPort(y));
        var input = new Relation(
            new Relation(Relation.Point(x, 3), Relation.Point(x.Inverse(), 2)),
            new Relation(Relation.Point(y, 5), Relation.Point(y.Inverse(), 4)));
        Assert.True(product.TryReadOperationFrom(input, area, out var expansion));
        var group = Assert.IsType<Relation>(expansion.Left);
        Assert.True(group.TryReadOrderedElementsOn(Assert.IsType<Carrier>(group.Right), out var terms));
        Assert.Equal(4, terms.Length);
        // The preserved term face is participation before the inverse-pair fold.
        // Re-read each source pair through the concrete product law before summing.
        var folded = terms.Select(term =>
        {
            Assert.True(product.TryReadOperationFrom(Assert.IsType<Relation>(term).Right, area, out var read));
            return Assert.IsType<CarrierLocation>(read.Left);
        }).ToArray();
        var sum = Equation.Sum(area, folded.Select(_ => Equation.UnitPort(area)).ToArray());
        IElement values = folded[0];
        foreach (var value in folded.Skip(1)) values = new Relation(values, value);
        Assert.True(sum.TryReadOperationFrom(values, area, out var combined));
        Assert.True(Assert.IsType<Relation>(combined.Left).TryReadFusedCarrierFaces(out var opposed, out var forward));
        Assert.Equal(3 * 5 - 2 * 4, forward.Ticks);
        Assert.Equal(3 * 4 + 2 * 5, opposed.Ticks);
        Assert.Same(area, forward.Carrier);
        Assert.Same(area.Inverse(), opposed.Carrier);
        Assert.Equal(2, area.UnitShape.ProductParticipationOf(x.UnitShape));
    }
}
