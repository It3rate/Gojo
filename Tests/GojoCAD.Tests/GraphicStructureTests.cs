using Gojo.Core;

namespace GojoCAD.Tests;

public sealed class GraphicStructureTests
{
    [Fact]
    public void PointIsGroundedCarrierLocationRelation()
    {
        var axis = new Carrier();
        var site = GraphicStructure.Site(12);

        var point = GraphicStructure.Point(axis, site);

        var location = Assert.IsType<CarrierLocation>(point.Left);
        Assert.Same(axis, point.Right);
        Assert.Same(axis, location.Carrier);
        Assert.True(location.SameTickRefAs(new CarrierLocation(axis, site)));
        Assert.Equal(12, location.Ticks);
    }

    [Fact]
    public void RadialFlowIsOrthogonalOutletFromPoint()
    {
        var axis = new Carrier();
        var point = GraphicStructure.Point(axis, 4);

        var radialFlow = GraphicStructure.RadialFlow(point);

        Assert.Equal(point, radialFlow.Left);
        var outlet = Assert.IsType<Relation>(radialFlow.Right);
        var startPoint = Assert.IsType<Relation>(outlet.Left);
        var endPoint = Assert.IsType<Relation>(outlet.Right);
        var start = Assert.IsType<CarrierLocation>(startPoint.Left);
        var end = Assert.IsType<CarrierLocation>(endPoint.Left);
        Assert.True(start.Carrier.IsUnknown);
        Assert.True(end.Carrier.IsUnknown);
        Assert.Equal(0, start.Ticks);
        Assert.Equal(0, end.Ticks);
    }

    [Fact]
    public void SegmentUsesTwoPositiveValuesOnOneCarrier()
    {
        var axis = new Carrier();
        var segment = GraphicStructure.Segment(axis, 2, 8);

        var startPoint = Assert.IsType<Relation>(segment.Left);
        var endPoint = Assert.IsType<Relation>(segment.Right);
        var start = Assert.IsType<CarrierLocation>(startPoint.Left);
        var end = Assert.IsType<CarrierLocation>(endPoint.Left);
        Assert.Same(axis, start.Carrier);
        Assert.Same(axis, end.Carrier);
        Assert.False(start.Carrier.IsInverse);
        Assert.Equal(2, start.Ticks);
        Assert.Equal(8, end.Ticks);
    }

    [Fact]
    public void PinIsOrdinaryRelation()
    {
        var first = new Carrier();
        var second = new Carrier();
        var firstPoint = GraphicStructure.Point(first, 2);
        var secondPoint = GraphicStructure.Point(second, 4);

        var pin = GraphicStructure.Pin(firstPoint, secondPoint);

        Assert.Same(firstPoint, pin.Left);
        Assert.Same(secondPoint, pin.Right);
    }

    [Fact]
    public void LinearCarrierAtUsesOneSharedSiteAsOrigin()
    {
        var axis = new Carrier();
        var site = GraphicStructure.Site(7);

        var linear = GraphicStructure.LinearCarrierAt(axis, site);

        Assert.True(linear.TryReadLinearOrigin(out var origin));
        Assert.True(origin.Carrier.SameIdentityAs(axis));
        Assert.Equal(7, origin.Ticks);
    }

    [Fact]
    public void LinearCarrierAtCanUsePointRelationAsOrigin()
    {
        var axis = new Carrier();
        var point = GraphicStructure.Point(axis, GraphicStructure.Site(7));

        var linear = GraphicStructure.LinearCarrierAt(point);

        Assert.True(linear.TryReadLinearOrigin(out var origin));
        Assert.True(origin.Carrier.SameIdentityAs(axis));
        Assert.Equal(7, origin.Ticks);
    }

    [Fact]
    public void CircularCarrierAtUsesOneSharedSiteAsOrigin()
    {
        var axis = new Carrier();
        var site = GraphicStructure.Site(7);

        var circular = GraphicStructure.CircularCarrierAt(axis, site);

        Assert.True(circular.TryReadCircularOrigin(out var origin));
        Assert.True(origin.Carrier.SameIdentityAs(axis));
        Assert.Equal(7, origin.Ticks);
    }

    [Fact]
    public void DirectedSegmentUsesInverseStartAndPositiveEnd()
    {
        var axis = new Carrier();

        var directed = GraphicStructure.DirectedSegment(axis, inverseStart: 2, positiveEnd: 4);

        var startPoint = Assert.IsType<Relation>(directed.Left);
        var endPoint = Assert.IsType<Relation>(directed.Right);
        var inverseStart = Assert.IsType<CarrierLocation>(startPoint.Left);
        var positiveEnd = Assert.IsType<CarrierLocation>(endPoint.Left);
        Assert.True(inverseStart.Carrier.SameIdentityAs(axis.Inverse()));
        Assert.True(positiveEnd.Carrier.SameIdentityAs(axis));
        Assert.Equal(2, inverseStart.Ticks);
        Assert.Equal(4, positiveEnd.Ticks);
        Assert.True(inverseStart.Carrier.UnitShape.AlgebraicallyCompatibleWith(positiveEnd.Carrier.UnitShape));
    }

    [Fact]
    public void DirectedSegmentCanUseSharedGraphicSites()
    {
        var axis = new Carrier();
        var startSite = GraphicStructure.Site(2);
        var endSite = GraphicStructure.Site(4);

        var directed = GraphicStructure.DirectedSegment(axis, startSite, endSite);

        var startPoint = Assert.IsType<Relation>(directed.Left);
        var endPoint = Assert.IsType<Relation>(directed.Right);
        var inverseStart = Assert.IsType<CarrierLocation>(startPoint.Left);
        var positiveEnd = Assert.IsType<CarrierLocation>(endPoint.Left);
        Assert.True(inverseStart.Carrier.SameIdentityAs(axis.Inverse()));
        Assert.True(positiveEnd.Carrier.SameIdentityAs(axis));
        Assert.True(inverseStart.SameTickRefAs(new CarrierLocation(axis.Inverse(), startSite)));
        Assert.True(positiveEnd.SameTickRefAs(new CarrierLocation(axis, endSite)));
    }
}
