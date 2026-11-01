using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class QuotientCompositionTests
{
    [Fact]
    public void NestedDenominatorsInvertRolesAndRetainEveryStandard()
    {
        var a = new Carrier(); var b = new Carrier(); var c = new Carrier(); var d = new Carrier();
        var abStandard = new Relation(a, b);
        var cdStandard = new Relation(c, d);
        var ab = new Carrier(QuotientUnitConformanceTests.Read([a], [b], abStandard));
        var cd = new Carrier(QuotientUnitConformanceTests.Read([c], [d], cdStandard));
        var outerStandard = new Relation(ab, cd);
        var original = Expression([ab], [cd], outerStandard);
        Assert.True(UnitQuotient.TryExpand(original, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out var selected, out var decompositions, out var result, out _, out var standard));
        Assert.Same(original, selected);
        Assert.Same(outerStandard, standard);
        Assert.True(UnitQuotient.TryReadUnit(Assert.IsType<Relation>(result), out var expanded));
        Assert.True(expanded.TryReadQuotient(out var n, out var den, out _));
        Assert.Equal(new[] { a.UnitShape, d.UnitShape }, n);
        Assert.Equal(new[] { b.UnitShape, c.UnitShape }, den);
        Assert.True(Assert.IsType<Relation>(decompositions).TryReadOrderedGroup(out var parts));
        Assert.Equal(2, parts.Length);
        for (var i = 0; i < 2; i++)
        {
            Assert.True(UnitQuotient.TryRead(Assert.IsType<Relation>(Assert.IsType<Relation>(parts[i]).Right), out _, out _, out var retained));
            Assert.Equal(i == 0 ? abStandard : cdStandard, retained);
        }
    }

    [Fact]
    public void ProductOfSpeedAndTimeCanBeReadAsDistanceOnlyWithExplicitEquivalence()
    {
        var mile = new Carrier(); var hour = new Carrier();
        var standard = new Relation(mile, hour);
        var speed = new Carrier(QuotientUnitConformanceTests.Read([mile], [hour], standard));
        var product = new Carrier(UnitShape.Product(speed.UnitShape, hour.UnitShape));
        Assert.False(product.UnitShape.CompatibleWith(mile.UnitShape));
        Assert.True(UnitQuotient.TryProjectEquivalent(new CarrierLocation(product, 18), mile, standard, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out var normalization));
        Assert.Equal(new CarrierLocation(mile, 18), result);
        Assert.Equal(new CarrierLocation(product, 0), residue);
        Assert.True(ReadWitness.TryRead(Assert.IsType<Relation>(normalization), out _, out var stages, out _, out _, out _));
        Assert.True(Assert.IsType<Relation>(stages).TryReadOrderedGroup(out var records));
        Assert.Equal(2, records.Length);
        Assert.Equal(0UL, mile.Value);
    }

    [Fact]
    public void NestedNumericDivisionUsesReducedOutputAndKeepsSourceRemainder()
    {
        var mile = new Carrier(); var hour = new Carrier();
        var standard = new Relation(mile, hour);
        var speed = new Carrier(QuotientUnitConformanceTests.Read([mile], [hour], standard));
        var inverseTime = new Carrier(QuotientUnitConformanceTests.Read([], [hour], standard));
        var request = QuantityQuotient.Create(new Carrier(), new CarrierLocation(speed, 7),
            new CarrierLocation(inverseTime, 2), mile, standard);
        Assert.False(QuantityQuotient.TryObserve(request, out _));
        Assert.True(QuantityQuotient.TryObserveReduced(request, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out var residue, out _));
        Assert.Equal(new CarrierLocation(mile, 3), result);
        Assert.Equal(new CarrierLocation(speed, 1), residue);
        var zero = QuantityQuotient.Create(new Carrier(), new CarrierLocation(speed, 7),
            new CarrierLocation(inverseTime, 0), mile, standard);
        Assert.False(QuantityQuotient.TryObserveReduced(zero, out var failed));
        Assert.True(ReadWitness.TryRead(failed, out _, out _, out var open, out _, out _));
        Assert.IsType<Placeholder>(open);
    }

    [Fact]
    public void ExpansionDoesNotStripCalibrationOpposedOrAdditiveParticipation()
    {
        var a = new Carrier(); var b = new Carrier();
        var standard = new Relation(a, b);
        var ratio = QuotientUnitConformanceTests.Read([a], [b], standard);
        foreach (var unit in new[] { UnitShape.ResolutionOf(ratio, 10), UnitShape.OpposedFaceOf(ratio), UnitShape.Additive(a.UnitShape, b.UnitShape) })
        {
            var c = new Carrier(unit);
            var expression = Expression([c], [], standard);
            Assert.True(UnitQuotient.TryNormalize(expression, out var witness));
            Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _));
            Assert.True(UnitQuotient.TryReadNumeratorUnit(Assert.IsType<Relation>(result), out var retained));
            Assert.Equal(unit, retained);
        }
        var partial = UnitQuotient.Create(new Carrier(), Relation.OrderedGroup(new Carrier(), new Placeholder()),
            Relation.OrderedGroup(new Carrier(), Relation.Point(b, 1)), standard);
        Assert.True(UnitQuotient.TryNormalize(partial, out var partialWitness));
        Assert.True(ReadWitness.TryRead(partialWitness, out _, out _, out var partialResult, out _, out _));
        Assert.False(UnitQuotient.TryReadUnit(Assert.IsType<Relation>(partialResult), out _));
    }

    private static Relation Expression(Carrier[] n, Carrier[] d, IElement standard) =>
        UnitQuotient.Create(new Carrier(), Relation.OrderedGroup(new Carrier(), [.. n.Select(c => Relation.Point(c, 1))]),
            Relation.OrderedGroup(new Carrier(), [.. d.Select(c => Relation.Point(c, 1))]), standard);
}
