using Gojo.Core;
using GojoCAD;

namespace GojoCAD.Tests;

public sealed class GraphicReadGraphTests
{
    [Fact]
    public void ReadsPointFromGroundedCarrierLocation()
    {
        var axis = new Carrier();
        var point = GraphicStructure.Point(axis, 7);

        var graph = GraphicReadGraph.From(point);

        var read = Assert.Single(graph.ReadsOf<PointRead>());
        Assert.Equal(point, read.Source);
        Assert.True(read.Location.Carrier.SameIdentityAs(axis));
        Assert.Equal(7, read.Location.Ticks);
    }

    [Fact]
    public void ReadsPositiveSegmentAndInteriorPartition()
    {
        var axis = new Carrier();
        var segment = GraphicStructure.Segment(axis, 2, 8);
        var partition = GraphicStructure.Point(axis, 5);
        var structure = new Relation(segment, partition);

        var graph = GraphicReadGraph.From(structure);

        var span = Assert.Single(graph.ReadsOf<PositiveSegmentRead>());
        Assert.Equal(2, span.Start.Location.Ticks);
        Assert.Equal(8, span.End.Location.Ticks);
        var tick = Assert.Single(graph.ReadsOf<SegmentPartitionRead>());
        Assert.Equal(5, tick.Point.Location.Ticks);
        Assert.Equal(span, tick.Segment);
    }

    [Fact]
    public void ReadsDirectedSegmentFromInverseAndPositiveFaces()
    {
        var axis = new Carrier();
        var directed = GraphicStructure.DirectedSegment(axis, inverseStart: 2, positiveEnd: 4);

        var graph = GraphicReadGraph.From(directed);

        var read = Assert.Single(graph.ReadsOf<DirectedSegmentRead>());
        Assert.True(read.InverseStart.Location.Carrier.SameIdentityAs(axis.Inverse()));
        Assert.True(read.PositiveEnd.Location.Carrier.SameIdentityAs(axis));
        Assert.False(read.IsSourceOrderReversed);
    }

    [Fact]
    public void ReadsLineSeedFromOrthogonalZeroSpan()
    {
        var axis = new Carrier();
        var zeroSpan = GraphicStructure.Segment(axis, 3, 3);
        var lineSeed = Relation.OrthogonalOutlet(zeroSpan);

        var graph = GraphicReadGraph.From(lineSeed);

        Assert.Single(graph.ReadsOf<ZeroSpanRead>(), span => span.Left.Location.Carrier.SameIdentityAs(axis));
        Assert.Single(graph.ReadsOf<OrthogonalOutletRead>());
        var line = Assert.Single(graph.ReadsOf<LineSeedRead>());
        Assert.Equal(3, line.Span.Left.Location.Ticks);
        Assert.Equal(3, line.Span.Right.Location.Ticks);
    }

    [Fact]
    public void ReadsRadialSeedFromPointOrthogonal()
    {
        var axis = new Carrier();
        var point = GraphicStructure.Point(axis, 4);
        var radial = GraphicStructure.RadialFlow(point);

        var graph = GraphicReadGraph.From(radial);

        var read = Assert.Single(graph.ReadsOf<RadialSeedRead>());
        Assert.True(read.Point.Location.Carrier.SameIdentityAs(axis));
        Assert.Equal(4, read.Point.Location.Ticks);
    }

    [Fact]
    public void ReadsCircularMarkersSpansAndContourPartitions()
    {
        var baseAxis = new Carrier();
        var origin = GraphicStructure.Point(baseAxis, 0);
        var contour = new Carrier(UnitShape.CircularFrom(origin));
        var span = GraphicStructure.Segment(contour, 10, 30);
        var partition = GraphicStructure.Point(contour, 20);
        var structure = new Relation(span, partition);

        var graph = GraphicReadGraph.From(structure);

        Assert.Equal(3, graph.ReadsOf<CircularMarkerRead>().Count());
        var contourSpan = Assert.Single(graph.ReadsOf<CircularSpanRead>());
        Assert.Equal(10, contourSpan.Segment.Start.Location.Ticks);
        Assert.Equal(30, contourSpan.Segment.End.Location.Ticks);
        var contourPartition = Assert.Single(graph.ReadsOf<ContourPartitionRead>());
        Assert.Equal(20, contourPartition.Marker.Point.Location.Ticks);
    }

    [Fact]
    public void ReadsRadiusAttachmentBetweenBasePointAndCircularMarker()
    {
        var baseAxis = new Carrier();
        var origin = GraphicStructure.Point(baseAxis, 0);
        var contour = new Carrier(UnitShape.CircularFrom(origin));
        var radiusAttachment = new Relation(
            GraphicStructure.Point(baseAxis, 12),
            GraphicStructure.Point(contour, 12));

        var graph = GraphicReadGraph.From(radiusAttachment);

        var attachment = Assert.Single(graph.ReadsOf<RadiusAttachmentRead>());
        Assert.True(attachment.BasePoint.Location.Carrier.SameIdentityAs(baseAxis));
        Assert.True(attachment.Marker.Point.Location.Carrier.SameIdentityAs(contour));
        Assert.Equal(12, attachment.BasePoint.Location.Ticks);
        Assert.Equal(12, attachment.Marker.Point.Location.Ticks);
    }
}
