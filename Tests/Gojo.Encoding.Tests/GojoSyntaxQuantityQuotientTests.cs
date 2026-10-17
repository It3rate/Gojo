using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxQuantityQuotientTests
{
    [Theory]
    [InlineData(2, 3, 1)]
    [InlineData(3, 4, 0)]
    [InlineData(4, -3, 1)]
    public void AuthoredDivisionMatchesTheKernelAndRetainsItsExactRemainder(int index, long expected, long remainder)
    {
        var roots = Parse();
        var request = Assert.IsType<Relation>(roots[index]);
        Assert.True(QuantityQuotient.TryObserve(request, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out var selected, out _, out var result, out var residue, out _));
        Assert.Same(request, selected);
        Assert.Equal(new CarrierLocation(Carriers(roots)[2], expected), result);
        Assert.Equal(new CarrierLocation(Carriers(roots)[0], remainder), residue);
        Assert.All(Carriers(roots), carrier => Assert.Equal(0UL, carrier.Value));
    }

    [Fact]
    public void UnknownAndZeroDenominatorsStayOpenAndBindingDoesNotRewriteTheOriginal()
    {
        var roots = Parse();
        for (var i = 5; i <= 6; i++)
        {
            Assert.False(QuantityQuotient.TryObserve(Assert.IsType<Relation>(roots[i]), out var witness));
            Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
            Assert.IsType<Placeholder>(result);
            Assert.IsType<Relation>(residue);
        }
        var bound = Assert.IsType<Relation>(StructuralPattern.Apply(roots[6], Assert.IsType<Relation>(roots[7])));
        Assert.True(QuantityQuotient.TryObserve(bound, out _));
        Assert.False(StructuralPattern.IsComplete(roots[6]));
        Assert.True(QuantityQuotient.TryRead(bound, out _, out var d, out _, out _));
        Assert.Equal(2, Assert.IsType<CarrierLocation>(d).Ticks);
    }

    [Fact]
    public void AuthoredConsumptionKeepsItsNativeHundredKilometreBasis()
    {
        var roots = Parse();
        var carriers = Carriers(roots);
        Assert.True(QuantityQuotient.TryObserve(Assert.IsType<Relation>(roots[8]), out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
        Assert.Equal(new CarrierLocation(carriers[8], 2), result);
        Assert.Equal(new CarrierLocation(carriers[5], 1), residue);
        Assert.True(carriers[8].UnitShape.TryReadQuotient(out _, out var d, out _));
        Assert.Equal(carriers[7].UnitShape, Assert.Single(d));
        Assert.False(QuantityQuotient.TryObserve(Assert.IsType<Relation>(roots[9]), out _));
    }

    [Fact]
    public void ReciprocalAndDimensionlessUnitsRemainDistinctAndPartialUnitCanBeBound()
    {
        var roots = Parse();
        var carriers = Carriers(roots);
        Assert.True(carriers[3].UnitShape.TryReadQuotient(out var n, out var d, out _));
        Assert.Empty(n);
        Assert.Equal(carriers[1].UnitShape, Assert.Single(d));
        Assert.True(carriers[4].UnitShape.TryReadQuotient(out var dn, out var dd, out _));
        Assert.Empty(dn);
        Assert.Empty(dd);
        Assert.False(carriers[4].UnitShape.CompatibleWith(Carrier.Unknown.UnitShape));
        Assert.False(UnitQuotient.TryReadUnit(Assert.IsType<Relation>(roots[10]), out _));
        var bound = Assert.IsType<Relation>(StructuralPattern.Apply(roots[10], Assert.IsType<Relation>(roots[11])));
        Assert.True(UnitQuotient.TryReadUnit(bound, out var unit));
        Assert.Equal(carriers[3].UnitShape, unit);
        Assert.True(Assert.IsType<Relation>(roots[12]).TryReadOrderedGroup(out var views));
        Assert.Equal(-1, Assert.IsType<CarrierLocation>(views[0]).Ticks);
        Assert.True(Assert.IsType<Carrier>(views[1]).UnitShape.TryReadOpposedFace(out _));
        Assert.Equal(Relation.Segment(carriers[1], 1, 0), views[2]);
        Assert.False(carriers[3].UnitShape.CompatibleWith(Assert.IsType<Carrier>(views[1]).UnitShape));
        Assert.True(Assert.IsType<Carrier>(roots[15]).UnitShape.TryReadQuotient(out _, out var squared, out _));
        Assert.Equal(2, squared.Length);
        Assert.All(squared, factor => Assert.Equal(carriers[1].UnitShape, factor));
        Assert.True(Assert.IsType<Carrier>(roots[16]).UnitShape.TryReadOrthogonalProductOperands(out var factors));
        Assert.Equal(new[] { carriers[3].UnitShape, carriers[0].UnitShape }, factors);
    }

    [Fact]
    public void CoarserReadPreservesBothResolutionResidueAndDivisionResidue()
    {
        var roots = Parse();
        var carriers = Carriers(roots);
        Assert.True(QuantityQuotient.TryObserve(Assert.IsType<Relation>(roots[13]), out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var sourceResidue, out _));
        var fine = Assert.IsType<CarrierLocation>(result);
        Assert.Equal(118, fine.Ticks);
        Assert.True(Assert.IsType<Relation>(roots[14]).TryProject(fine, carriers[9], out var coarseRead));
        Assert.True(coarseRead.TryReadProjection(out var coarse, out var resolutionResidue));
        Assert.Equal(11, coarse.Ticks);
        var fineRemainder = Assert.IsType<CarrierLocation>(resolutionResidue);
        var originalRemainder = Assert.IsType<CarrierLocation>(sourceResidue);
        Assert.Same(carriers[2], fineRemainder.Carrier);
        Assert.Same(carriers[0], originalRemainder.Carrier);
        Assert.Equal(237, coarse.Ticks * 10 * 2 + fineRemainder.Ticks * 2 + originalRemainder.Ticks);
    }

    [Theory]
    [InlineData("group(ports(?factor), ports(), $u)")]
    [InlineData("group(ports(point($u, 0)), ports(), $u)")]
    [InlineData("group(ports(point($u, 1)), ports(), ?standard)")]
    [InlineData("group(ports(), ports(), $u, $u)")]
    public void PrematureOrMalformedUnitCaptureProducesALocatedError(string expression)
    {
        var error = Assert.Throws<GojoSyntaxException>(() => GojoSyntax.Parse(
            $"gojo 1; let $u = carrier();\nroot carrier(quotient_unit({expression}));"));
        Assert.Equal(2, error.Line);
        Assert.True(error.Column > 1);
    }

    private static IReadOnlyList<IElement> Parse() => GojoSyntax.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Syntax", "quantity-quotients.gojo"))).Roots;

    private static Carrier[] Carriers(IReadOnlyList<IElement> roots)
    {
        Assert.True(Assert.IsType<Relation>(roots[0]).TryReadOrderedGroup(out var carriers));
        return carriers.Cast<Carrier>().ToArray();
    }
}
