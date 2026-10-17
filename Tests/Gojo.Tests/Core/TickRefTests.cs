using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class TickRefTests
{
    [Fact]
    public void AnonymousCarrierLocationsAreEqualWithoutSharingTickReference()
    {
        var carrier = new Carrier();
        var first = new CarrierLocation(carrier, 10);
        var second = new CarrierLocation(carrier, 10);

        Assert.Equal(first, second);
        Assert.False(first.SameTickRefAs(second));

        first.Tick.Set(12);

        Assert.Equal(new CarrierLocation(carrier, 12), first);
        Assert.Equal(new CarrierLocation(carrier, 10), second);
    }

    [Fact]
    public void CarrierLocationsCanShareTickReferenceAcrossCarriers()
    {
        var carrier = new Carrier();
        var inverse = carrier.Inverse();
        var tick = new TickRef(10);
        var positive = new CarrierLocation(carrier, tick);
        var inverted = new CarrierLocation(inverse, tick);

        Assert.NotEqual(positive, inverted);
        Assert.True(positive.SameTickRefAs(inverted));

        tick.Set(12);

        Assert.Equal(new CarrierLocation(carrier, 12), positive);
        Assert.Equal(new CarrierLocation(inverse, 12), inverted);
    }

    [Fact]
    public void SharedTickReferenceMovesMirroredUnitSurfaces()
    {
        var carrier = new Carrier();
        var inverse = carrier.Inverse();
        var origin = new TickRef(0);
        var extent = new TickRef(10);
        var unit = Segment(carrier, origin, extent);
        var inverseUnit = Segment(inverse, origin, extent);

        extent.Set(12);

        Assert.Equal(new CarrierLocation(carrier, 0), PointLocation((Relation)unit.Left));
        Assert.Equal(new CarrierLocation(carrier, 12), PointLocation((Relation)unit.Right));
        Assert.Equal(new CarrierLocation(inverse, 0), PointLocation((Relation)inverseUnit.Left));
        Assert.Equal(new CarrierLocation(inverse, 12), PointLocation((Relation)inverseUnit.Right));
    }

    [Fact]
    public void PerturbingSharedTickReferenceMovesEachCarrierFace()
    {
        var carrier = new Carrier();
        var inverse = carrier.Inverse();
        var extent = new TickRef(10);
        var point = Point(carrier, extent);
        var inversePoint = Point(inverse, extent);

        point.Perturb(new CarrierLocation(carrier, 2));

        Assert.Equal(new CarrierLocation(carrier, 12), point.Left);
        Assert.Equal(new CarrierLocation(inverse, 12), inversePoint.Left);
    }

    [Fact]
    public void ProjectionReadsSharedAndAnonymousLocationsByTickValue()
    {
        var source = new Carrier();
        var target = new Carrier();
        var ratio = new Relation(Point(source, 2), Point(target, 3));
        var shared = new CarrierLocation(source, new TickRef(6));
        var anonymous = new CarrierLocation(source, 6);

        Assert.False(shared.SameTickRefAs(anonymous));
        Assert.Equal(
            ratio.Project(anonymous, target),
            ratio.Project(shared, target));
    }

    [Fact]
    public void DualCarrierLocationNegatesValueWithoutSharingTickReference()
    {
        var carrier = new Carrier();
        var tick = new TickRef(5);
        var location = new CarrierLocation(carrier, tick);
        var dual = location.Dual();

        Assert.Equal(new CarrierLocation(carrier, -5), dual);
        Assert.False(location.SameTickRefAs(dual));

        tick.Set(7);

        Assert.Equal(new CarrierLocation(carrier, 7), location);
        Assert.Equal(new CarrierLocation(carrier, -5), dual);
    }

    private static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);

    private static Relation Point(Carrier carrier, TickRef tick) =>
        new(new CarrierLocation(carrier, tick), carrier);

    private static Relation Segment(Carrier carrier, TickRef left, TickRef right) =>
        new(Point(carrier, left), Point(carrier, right));

    private static CarrierLocation PointLocation(Relation point) => (CarrierLocation)point.Left;
}
