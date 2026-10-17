using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class UnitAlgebraConformanceTests
{
    [Fact]
    public void NestedProductReadExposesOnlyItsThreeOrderedFactors()
    {
        var u = new Carrier().UnitShape;
        var v = new Carrier().UnitShape;
        var w = new Carrier().UnitShape;
        var nested = UnitShape.Product(UnitShape.Product(u, v), w);
        Assert.True(nested.TryReadOrthogonalProductOperands(out var factors));
        Assert.Equal(3, factors.Length);
        Assert.True(factors[0].CompatibleWith(u));
        Assert.True(factors[1].CompatibleWith(v));
        Assert.True(factors[2].CompatibleWith(w));
    }

    [Fact]
    public void ProductReadKeepsUnreducedAdditiveParticipationAsOneFactor()
    {
        var u = new Carrier().UnitShape;
        var v = new Carrier().UnitShape;
        var mixed = UnitShape.Additive(u, v);
        var product = UnitShape.Product(mixed, u);
        Assert.True(product.TryReadOrthogonalProductOperands(out var factors));
        Assert.Equal(2, factors.Length);
        Assert.True(factors[0].CompatibleWith(mixed));
        Assert.True(factors[1].CompatibleWith(u));
        Assert.Equal(1, product.ProductParticipationOf(u));
    }

    [Fact]
    public void ProductReadKeepsOpposedFactorWithoutInventingAMarkerFactor()
    {
        var u = new Carrier().UnitShape;
        var opposed = UnitShape.OpposedFaceOf(u);
        var product = UnitShape.Product(opposed, u);
        Assert.True(product.TryReadOrthogonalProductOperands(out var factors));
        Assert.Equal(2, factors.Length);
        Assert.True(factors[0].TryReadOpposedFace(out var forward));
        Assert.True(forward.CompatibleWith(u));
        Assert.True(factors[1].CompatibleWith(u));
        Assert.Equal(2, product.ProductParticipationOf(u));
    }
}
