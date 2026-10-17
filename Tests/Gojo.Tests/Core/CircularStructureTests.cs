using Gojo.Core;

namespace Gojo.Tests.Core;

public sealed class CircularStructureTests
{
    [Fact]
    public void CarrierUnitShapeRevealsCircularOriginWithoutExternalClassifier()
    {
        var origin = new Carrier();
        var perimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(origin, 5)));
        var radius = new Carrier(UnitShape.LinearFrom(Relation.Segment(origin, 5, 5)));

        Assert.True(perimeter.TryReadCircularOrigin(out var perimeterOrigin));
        Assert.True(radius.TryReadLinearOrigin(out var radiusOrigin));

        Assert.Same(origin, perimeterOrigin.Carrier);
        Assert.Same(origin, radiusOrigin.Carrier);
        Assert.Equal(5, perimeterOrigin.Ticks);
        Assert.Equal(5, radiusOrigin.Ticks);
    }

    [Fact]
    public void RadialEndpointToPerimeterMarkerIsOrdinaryConnectedStructure()
    {
        var origin = new Carrier();
        var perimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(origin, 0)));
        var radius = new Carrier(UnitShape.LinearFrom(Relation.Segment(origin, 0, 0)));
        var attachment = new Relation(Relation.Point(radius, 12), Relation.Point(perimeter, 3));

        Assert.True(perimeter.TryReadCircularOrigin(out var perimeterOrigin));
        Assert.True(radius.TryReadLinearOrigin(out var radiusOrigin));
        Assert.Same(origin, perimeterOrigin.Carrier);
        Assert.Same(origin, radiusOrigin.Carrier);
        Assert.Contains(perimeter, attachment.Carriers());
        Assert.Contains(radius, attachment.Carriers());
        Assert.Equal(12, ((Relation)attachment.Left).LeftLocation.Ticks);
        Assert.Equal(3, ((Relation)attachment.Right).LeftLocation.Ticks);
    }

    [Fact]
    public void LinearAndCircularOriginsAreDistinctWhenSourceStructureDiffers()
    {
        var origin = new Carrier();
        var perimeter = new Carrier(UnitShape.CircularFrom(Relation.Point(origin, 0)));
        var ray = new Carrier(UnitShape.LinearFrom(Relation.Segment(origin, 0, 0)));

        Assert.True(perimeter.TryReadCircularOrigin(out _));
        Assert.False(perimeter.TryReadLinearOrigin(out _));
        Assert.True(ray.TryReadLinearOrigin(out _));
        Assert.False(ray.TryReadCircularOrigin(out _));
    }
}
