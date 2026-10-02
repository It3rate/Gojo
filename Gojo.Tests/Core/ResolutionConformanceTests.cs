using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Tests.Core;

public sealed class ResolutionConformanceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public void ResolutionRequiresAPositiveTickRatio(long ratio)
    {
        var unit = new Carrier().UnitShape;
        Assert.Throws<ArgumentOutOfRangeException>(() => UnitShape.ResolutionOf(unit, ratio));
        Assert.Same(unit, UnitShape.ResolutionOf(unit, 1));
    }

    [Fact]
    public void NestedResolutionMustNotSaturateIntoADifferentUnitPromise()
    {
        var unit = new Carrier().UnitShape;
        var wide = UnitShape.ResolutionOf(unit, long.MaxValue);
        Assert.Throws<OverflowException>(() => UnitShape.ResolutionOf(wide, 2));
        Assert.True(wide.TryReadResolution(out var original, out var ratio));
        Assert.True(original.CompatibleWith(unit));
        Assert.Equal(long.MaxValue, ratio);
    }

    [Fact]
    public void AdditionBeforeAndAfterCoarseningCanDifferWhileBothAccountForTheSameInput()
    {
        var x = new Carrier();
        var y = new Carrier(x.UnitShape);
        var fine = new Carrier(x.UnitShape);
        var coarse = new Carrier(UnitShape.ResolutionOf(fine.UnitShape, 10));
        var input = new Relation(new CarrierLocation(x, 17), new CarrierLocation(y, 28));
        var fineSum = Equation.Sum(fine, Equation.UnitPort(x), Equation.UnitPort(y));
        var coarseSum = Equation.Sum(coarse, Equation.WeightedPort(x, 0, 10, 0, 1), Equation.WeightedPort(y, 0, 10, 0, 1));
        Assert.True(fineSum.TryReadOperationFrom(input, fine, out var fineRead));
        var fineValue = Assert.IsType<CarrierLocation>(fineRead.Left);
        var readBasis = new Relation(Relation.Point(fine, 10), Relation.Point(coarse, 1));
        Assert.True(readBasis.Project(fineValue, coarse).TryGetCarrierLocations(out var after, out var afterResidue));
        Assert.True(coarseSum.TryReadOperationFrom(input, coarse, out var beforeRead));
        var before = Assert.IsType<CarrierLocation>(beforeRead.Left);
        Assert.True(Assert.IsType<Relation>(beforeRead.Right).TryGetCarrierLocations(out var rx, out var ry));
        Assert.Equal(4, after.Ticks);
        Assert.Equal(3, before.Ticks);
        Assert.Equal(45, 10 * after.Ticks + afterResidue.Ticks);
        Assert.Equal(45, 10 * before.Ticks + rx.Ticks + ry.Ticks);
        Assert.Same(x, rx.Carrier);
        Assert.Same(y, ry.Carrier);
    }

    [Fact]
    public void DirectCoarseReadAgreesWithStagedReadWhenEveryRemainderIsKept()
    {
        var fine = new Carrier();
        var tens = new Carrier(UnitShape.ResolutionOf(fine.UnitShape, 10));
        var hundreds = new Carrier(UnitShape.ResolutionOf(tens.UnitShape, 10));
        var first = new Relation(Relation.Point(fine, 10), Relation.Point(tens, 1));
        var second = new Relation(Relation.Point(tens, 10), Relation.Point(hundreds, 1));
        var direct = new Relation(Relation.Point(fine, 100), Relation.Point(hundreds, 1));
        for (var x = -235; x <= 235; x++)
        {
            Assert.True(first.Project(new CarrierLocation(fine, x), tens).TryGetCarrierLocations(out var t, out var r1));
            Assert.True(second.Project(t, hundreds).TryGetCarrierLocations(out var h, out var r2));
            Assert.True(direct.Project(new CarrierLocation(fine, x), hundreds).TryGetCarrierLocations(out var hd, out var rd));
            Assert.Equal(hd, h);
            Assert.Equal(rd.Ticks, r1.Ticks + 10 * r2.Ticks);
            Assert.Equal(x, 100 * h.Ticks + rd.Ticks);
        }
    }
}
