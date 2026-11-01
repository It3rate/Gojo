using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class QuotientUnitConformanceTests
{
    [Fact]
    public void ReciprocalTickRetainsDenominatorRepetitionAndStandard()
    {
        var time = new Carrier();
        var standard = Relation.Point(time, 1);
        var unit = Read([], [time, time], standard);
        Assert.True(unit.TryReadQuotient(out var n, out var d, out var retained));
        Assert.Empty(n);
        Assert.Equal(2, d.Length);
        Assert.All(d, factor => Assert.Equal(time.UnitShape, factor));
        Assert.Equal(standard, retained);
        Assert.False(unit.CompatibleWith(time.UnitShape));
        Assert.False(unit.CompatibleWith(time.Inverse().UnitShape));
        Assert.False(unit.TryReadOpposedFace(out _));
        Assert.False(unit.TryReadOrthogonalProductOperands(out _));
        Assert.False(unit.TryReadAdditiveOperands(out _));
        Assert.Equal(unit, Read([], [time, time], standard));
    }

    [Fact]
    public void DimensionlessPromiseIsExplicitAndDoesNotCancelAnUnreducedUnit()
    {
        var u = new Carrier();
        var standard = Relation.Point(u, 1);
        var dimensionless = Read([], [], standard);
        Assert.True(dimensionless.TryReadQuotient(out var n, out var d, out _));
        Assert.Empty(n);
        Assert.Empty(d);
        Assert.False(dimensionless.CompatibleWith(Carrier.Unknown.UnitShape));
        Assert.NotEqual(Read([u], [u], standard), dimensionless);
        var expression = Expression([u], [u], standard);
        Assert.True(UnitQuotient.TryCancel(expression, out var witness));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _));
        Assert.True(UnitQuotient.TryReadUnit(Assert.IsType<Relation>(result), out var reduced));
        Assert.Equal(dimensionless, reduced);
    }

    [Fact]
    public void OrderCalibrationAndProvenanceSurviveCrystallization()
    {
        var u = new Carrier(UnitShape.DescribedAtom(new Carrier()));
        var v = new Carrier(UnitShape.DescribedAtom(new Carrier()));
        var hundredV = new Carrier(UnitShape.ResolutionOf(v.UnitShape, 100));
        var standard = new Relation(Relation.Point(u, 1), Relation.Point(v, 100));
        var unit = Read([u, v], [hundredV], standard);
        Assert.True(unit.TryReadQuotient(out var n, out var d, out var retained));
        Assert.Equal(new[] { u.UnitShape, v.UnitShape }, n);
        Assert.Equal(hundredV.UnitShape, Assert.Single(d));
        Assert.Equal(standard, retained);
        Assert.False(unit.CompatibleWith(Read([v, u], [hundredV], standard)));
        Assert.False(unit.CompatibleWith(Read([u, v], [v], standard)));
        var otherStandard = new Relation(Relation.Point(u, 1), Relation.Point(v, 1));
        Assert.True(unit.AlgebraicallyCompatibleWith(Read([u, v], [hundredV], otherStandard)));
        Assert.False(unit.CompatibleWith(Read([u, v], [hundredV], otherStandard)));
    }

    [Fact]
    public void NativeUnitCapturesStandardCoordinatesAndExposesOnlySnapshots()
    {
        var u = new Carrier();
        var v = new Carrier();
        var tick = new TickRef(100);
        var standard = new Relation(new CarrierLocation(v, tick), v);
        var unit = Read([u], [v], standard);
        var same = Read([u], [v], standard);
        var hash = unit.GetHashCode();
        tick.Set(50);
        Assert.Equal(same, unit);
        Assert.Equal(hash, unit.GetHashCode());
        Assert.NotEqual(Read([u], [v], standard), unit);
        Assert.True(unit.TryReadQuotient(out _, out _, out var first));
        var exposed = Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(first).Left);
        Assert.Equal(100, exposed.Ticks);
        exposed.Tick.Set(3);
        Assert.True(unit.TryReadQuotient(out _, out _, out var second));
        Assert.Equal(100, Assert.IsType<CarrierLocation>(Assert.IsType<Relation>(second).Left).Ticks);
    }

    [Fact]
    public void CompositeReadersKeepQuotientAndOpposedFactorsWhole()
    {
        var u = new Carrier();
        var v = new Carrier();
        var quotient = Read([u], [v], new Relation(u, v));
        var outer = Read([new Carrier(quotient)], [v], u);
        Assert.True(outer.TryReadQuotient(out var n, out var d, out _));
        Assert.Equal(quotient, Assert.Single(n));
        Assert.Equal(v.UnitShape, Assert.Single(d));
        Assert.True(UnitShape.Product(quotient, u.UnitShape).TryReadOrthogonalProductOperands(out var product));
        Assert.Equal(new[] { quotient, u.UnitShape }, product);
        Assert.True(UnitShape.Additive(quotient, u.Inverse().UnitShape).TryReadAdditiveOperands(out var sum));
        Assert.Equal(new[] { quotient, u.Inverse().UnitShape }, sum);
        var coarse = UnitShape.ResolutionOf(quotient, 10);
        Assert.True(coarse.TryReadResolution(out var fine, out var scale));
        Assert.Equal(10, scale);
        Assert.Equal(quotient, fine);
        Assert.True(UnitShape.OpposedFaceOf(quotient).TryReadQuotient(out _, out var opposedD, out _));
        Assert.Equal(v.UnitShape, Assert.Single(opposedD));
    }

    [Fact]
    public void IncompleteExpressionsDoNotInventAStableTickPromise()
    {
        var u = new Carrier();
        var empty = Relation.OrderedGroup(new Carrier());
        foreach (var factor in new IElement[] { new Placeholder(), Relation.Point(Carrier.Unknown, 1), Relation.Point(u, 0) })
        {
            var expression = UnitQuotient.Create(new Carrier(), empty,
                Relation.OrderedGroup(new Carrier(), factor), u);
            Assert.False(UnitQuotient.TryReadUnit(expression, out _));
        }
        Assert.False(UnitQuotient.TryReadUnit(Expression([u], [], new Placeholder()), out _));
    }

    internal static UnitShape Read(Carrier[] numerator, Carrier[] denominator, IElement standard)
    {
        Assert.True(UnitQuotient.TryReadUnit(Expression(numerator, denominator, standard), out var unit));
        return unit;
    }

    private static Relation Expression(Carrier[] numerator, Carrier[] denominator, IElement standard) =>
        UnitQuotient.Create(new Carrier(),
            Relation.OrderedGroup(new Carrier(), [.. numerator.Select(c => Relation.Point(c, 1))]),
            Relation.OrderedGroup(new Carrier(), [.. denominator.Select(c => Relation.Point(c, 1))]), standard);
}
