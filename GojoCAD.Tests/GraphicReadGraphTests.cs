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
        Assert.Equal([0, 1], graph.Bands.Select(band => band.Order).ToArray());
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

    [Fact]
    public void ReadsLinearBasisWitnessFromEndpointAttachmentToContourPoint()
    {
        var baseAxis = new Carrier();
        var origin = GraphicStructure.Point(baseAxis, 0);
        var contour = new Carrier(UnitShape.CircularFrom(origin));
        var linear = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(baseAxis, 0, 0)));
        var endpointAttachment = new Relation(
            GraphicStructure.Point(linear, 11),
            GraphicStructure.Point(contour, 12));

        var graph = GraphicReadGraph.From(endpointAttachment);

        var witness = Assert.Single(graph.ReadsOf<LinearBasisWitnessRead>());
        Assert.True(witness.LinearCarrier.SameIdentityAs(linear));
        Assert.Equal(0, witness.LinearOriginTicks);
        Assert.Equal(11, witness.LinearLocation.Ticks);
        Assert.True(witness.SourceOrigin.Carrier.SameIdentityAs(baseAxis));
        Assert.True(witness.WitnessPoint.Location.Carrier.SameIdentityAs(contour));
        Assert.Single(graph.ReadsOf<PointAttachmentRead>());
        Assert.Empty(graph.ReadsOf<RadiusAttachmentRead>());
        Assert.Equal([0, 1, 2], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void ReadsSharedSitesSegmentConnectionsAndJunctions()
    {
        var junction = GraphicStructure.Site(0);
        var axisA = new Carrier();
        var axisB = new Carrier();
        var axisC = new Carrier();
        var first = GraphicStructure.Segment(axisA, GraphicStructure.Site(-8), junction);
        var second = GraphicStructure.Segment(axisB, junction, GraphicStructure.Site(8));
        var third = GraphicStructure.Segment(axisC, junction, GraphicStructure.Site(13));
        var structure = Chain(first, second, third);

        var graph = GraphicReadGraph.From(structure);

        var shared = Assert.Single(graph.ReadsOf<SharedSiteRead>(), site => site.Points.Count == 3);
        Assert.All(shared.Points, point => Assert.True(point.Location.SameTickRefAs(shared.Points[0].Location)));
        Assert.Equal(3, graph.ReadsOf<SegmentConnectionRead>().Count());
        Assert.Empty(graph.ReadsOf<SegmentJoinRead>());
        var junctionRead = Assert.Single(graph.ReadsOf<SegmentJunctionRead>());
        Assert.Equal(3, junctionRead.Endpoints.Count);
        Assert.Equal([0, 1, 2], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void ReadsSegmentJoinOnlyAtTwoEndpointSite()
    {
        var middle = GraphicStructure.Site(0);
        var first = GraphicStructure.Segment(new Carrier(), GraphicStructure.Site(-8), middle);
        var second = GraphicStructure.Segment(new Carrier(), middle, GraphicStructure.Site(8));
        var structure = new Relation(first, second);

        var graph = GraphicReadGraph.From(structure);

        var join = Assert.Single(graph.ReadsOf<SegmentJoinRead>());
        Assert.Equal(2, join.Site.Points.Count);
        Assert.Equal(middle.Id, join.First.Point.Location.Tick?.Id);
        Assert.Equal(middle.Id, join.Second.Point.Location.Tick?.Id);
        Assert.Empty(graph.ReadsOf<SegmentJunctionRead>());
        Assert.Empty(graph.ReadsOf<BoundaryJoinRead>());
        Assert.Equal([0, 1, 2], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void AffectedReadsFromSegmentSourceIncludesCompositeConnectionReads()
    {
        var junction = GraphicStructure.Site(0);
        var axisA = new Carrier();
        var axisB = new Carrier();
        var first = GraphicStructure.Segment(axisA, GraphicStructure.Site(-8), junction);
        var second = GraphicStructure.Segment(axisB, junction, GraphicStructure.Site(8));
        var structure = new Relation(first, second);
        var graph = GraphicReadGraph.From(structure);

        var affected = graph.AffectedReadsFrom(second, radius: 2);

        Assert.Contains(affected.OfType<SharedSiteRead>(), site => site.Points.Count == 2);
        Assert.Contains(affected.OfType<SegmentConnectionRead>(), connection =>
            ReferenceEquals(connection.Left.Segment.Source, second) ||
            ReferenceEquals(connection.Right.Segment.Source, second));
        Assert.Contains(affected.OfType<SegmentJoinRead>(), join =>
            ReferenceEquals(join.First.Segment.Source, second) ||
            ReferenceEquals(join.Second.Segment.Source, second));
    }

    [Fact]
    public void ReadsLinearBasisAlignmentBetweenLinearCarriers()
    {
        var baseAxis = new Carrier();
        var first = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(baseAxis, 0, 0)));
        var second = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(baseAxis, 4, 4)));
        var aligned = new Relation(
            GraphicStructure.Point(first, 5),
            GraphicStructure.Point(second, 10));

        var graph = GraphicReadGraph.From(aligned);

        Assert.Equal(2, graph.ReadsOf<LinearBasisWitnessRead>().Count());
        var alignment = Assert.Single(graph.ReadsOf<LinearBasisAlignmentRead>());
        Assert.True(alignment.Left.Carrier.SameIdentityAs(first));
        Assert.True(alignment.Right.Carrier.SameIdentityAs(second));
        Assert.Equal(5, alignment.Left.DeltaTicks);
        Assert.Equal(10, alignment.Right.DeltaTicks);
        Assert.True(alignment.HasSameFlow);
        Assert.False(alignment.HasOpposedFlow);
    }

    [Fact]
    public void ReadsOpposedLinearBasisAlignmentFromInverseFace()
    {
        var baseAxis = new Carrier();
        var first = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(baseAxis, 0, 0)));
        var second = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(baseAxis, 4, 4)));
        var opposed = new Relation(
            GraphicStructure.Point(first, 5),
            GraphicStructure.Point(second.Inverse(), 10));

        var graph = GraphicReadGraph.From(opposed);

        var alignment = Assert.Single(graph.ReadsOf<LinearBasisAlignmentRead>());
        Assert.True(alignment.Left.Carrier.SameIdentityAs(first));
        Assert.True(alignment.Right.Carrier.SameIdentityAs(second));
        Assert.Equal(5, alignment.Left.DeltaTicks);
        Assert.Equal(-10, alignment.Right.DeltaTicks);
        Assert.False(alignment.HasSameFlow);
        Assert.True(alignment.HasOpposedFlow);
    }

    [Fact]
    public void ReadsClosedSegmentBoundaryFromEndpointCycle()
    {
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(10);
        var thirdSite = GraphicStructure.Site(20);
        var first = GraphicStructure.Segment(new Carrier(), firstSite, secondSite);
        var second = GraphicStructure.Segment(new Carrier(), secondSite, thirdSite);
        var third = GraphicStructure.Segment(new Carrier(), thirdSite, firstSite);
        var structure = Chain(first, second, third);

        var graph = GraphicReadGraph.From(structure);

        var boundary = Assert.Single(graph.ReadsOf<ClosedSegmentBoundaryRead>());
        Assert.Equal(3, boundary.Segments.Count);
        Assert.Equal(3, boundary.Sites.Count);
        Assert.Equal(3, graph.ReadsOf<SegmentJoinRead>().Count());
        Assert.Equal(3, graph.ReadsOf<BoundaryJoinRead>().Count());
        Assert.Empty(graph.ReadsOf<SegmentJunctionRead>());
        Assert.Equal([0, 1, 2, 4], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void NavigationFromSharedSiteFindsAffectedLocalReads()
    {
        var axis = new Carrier();
        var partitionSite = GraphicStructure.Site(5);
        var segment = GraphicStructure.Segment(axis, 2, 8);
        var partition = GraphicStructure.Point(axis, partitionSite);
        var structure = new Relation(segment, partition);
        var graph = GraphicReadGraph.From(structure);

        var affectedReads = graph.AffectedReadsFrom(partitionSite, radius: 2);

        Assert.Contains(affectedReads.OfType<PointRead>(), read => read.Location.Ticks == 5);
        Assert.Contains(affectedReads.OfType<PositiveSegmentRead>(), read =>
            read.Start.Location.Ticks == 2 &&
            read.End.Location.Ticks == 8);
        Assert.Single(affectedReads.OfType<SegmentPartitionRead>());
    }

    [Fact]
    public void RefreshFromSharedSiteRebuildsChangedReadsFromStructure()
    {
        var axis = new Carrier();
        var partitionSite = GraphicStructure.Site(5);
        var segment = GraphicStructure.Segment(axis, 2, 8);
        var partition = GraphicStructure.Point(axis, partitionSite);
        var structure = new Relation(segment, partition);
        var graph = GraphicReadGraph.From(structure);

        partitionSite.Set(12);
        var refresh = graph.RefreshFrom(partitionSite, radius: 2);

        Assert.Single(refresh.PreviousReads.OfType<SegmentPartitionRead>());
        Assert.Empty(refresh.CurrentGraph.ReadsOf<SegmentPartitionRead>());
        Assert.Contains(refresh.CurrentReads.OfType<PointRead>(), read => read.Location.Ticks == 12);
    }

    [Fact]
    public void RefreshFromSharedSiteKeepsNewPartitionWhenMovedInsideSpan()
    {
        var axis = new Carrier();
        var partitionSite = GraphicStructure.Site(12);
        var segment = GraphicStructure.Segment(axis, 2, 18);
        var partition = GraphicStructure.Point(axis, partitionSite);
        var structure = new Relation(segment, partition);
        var graph = GraphicReadGraph.From(structure);

        partitionSite.Set(7);
        var refresh = graph.RefreshFrom(partitionSite, radius: 2);

        var currentPartition = Assert.Single(refresh.CurrentGraph.ReadsOf<SegmentPartitionRead>());
        Assert.Equal(7, currentPartition.Point.Location.Ticks);
        Assert.Contains(refresh.CurrentReads.OfType<SegmentPartitionRead>(), read => read.Point.Location.Ticks == 7);
    }

    private static IElement Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            return Carrier.Unknown;
        }

        var structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure;
    }
}
