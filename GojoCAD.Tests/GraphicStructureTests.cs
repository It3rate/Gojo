using Gojo.Core;

namespace GojoCAD.Tests;

public sealed class GraphicStructureTests
{
    [Fact]
    public void PointPreservesSharedGraphicSite()
    {
        var axis = new Carrier();
        var site = GraphicStructure.Site(12);

        var point = GraphicStructure.Point(axis, site);

        Assert.True(GraphicStructure.TryReadPoint(point, out var location));
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

        Assert.True(GraphicStructure.TryReadRadialFlow(radialFlow, out var source));
        Assert.Equal(point, source);
    }

    [Fact]
    public void SegmentUsesTwoPositiveValuesOnOneCarrier()
    {
        var axis = new Carrier();
        var segment = GraphicStructure.Segment(axis, 2, 8);

        Assert.True(GraphicStructure.TryReadSegment(segment, out var start, out var end));
        Assert.Same(axis, start.Carrier);
        Assert.Same(axis, end.Carrier);
        Assert.False(start.Carrier.IsInverse);
        Assert.Equal(2, start.Ticks);
        Assert.Equal(8, end.Ticks);
    }

    [Fact]
    public void DirectedSegmentUsesInverseStartAndPositiveEnd()
    {
        var axis = new Carrier();

        var directed = GraphicStructure.DirectedSegment(axis, inverseStart: 2, positiveEnd: 4);

        Assert.True(GraphicStructure.TryReadDirectedSegment(directed, out var inverseStart, out var positiveEnd));
        Assert.True(inverseStart.Carrier.SameIdentityAs(axis.Inverse()));
        Assert.True(positiveEnd.Carrier.SameIdentityAs(axis));
        Assert.Equal(2, inverseStart.Ticks);
        Assert.Equal(4, positiveEnd.Ticks);
        Assert.True(inverseStart.Carrier.UnitShape.AlgebraicallyCompatibleWith(positiveEnd.Carrier.UnitShape));
    }
}
