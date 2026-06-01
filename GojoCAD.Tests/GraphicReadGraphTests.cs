using Gojo.Core;
using Gojo.Structures;
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
        var pathJunction = Assert.Single(graph.ReadsOf<PathJunctionRead>());
        Assert.Equal(3, pathJunction.Endpoints.Count);
        Assert.Empty(graph.ReadsOf<BranchRead>());
        Assert.Empty(graph.ReadsOf<MergeRead>());
        Assert.Equal([0, 1, 2, 4], graph.Bands.Select(band => band.Order).ToArray());
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
        var flow = Assert.Single(graph.ReadsOf<SegmentJoinFlowRead>());
        Assert.True(flow.IsCarrierTransition);
        Assert.False(flow.SharesCarrier);
        var compatibility = Assert.Single(graph.ReadsOf<SegmentJoinUnitCompatibilityRead>());
        Assert.True(compatibility.HasDifferentAlgebra);
        Assert.False(compatibility.AlgebraicallyCompatible);
        Assert.Empty(graph.ReadsOf<SegmentJunctionRead>());
        Assert.Empty(graph.ReadsOf<BoundaryJoinRead>());
        var pathGraph = Assert.Single(graph.ReadsOf<PathGraphRead>());
        Assert.Equal(2, pathGraph.Edges.Count);
        Assert.Equal(2, pathGraph.TerminalNodeCount);
        Assert.Equal([0, 1, 2, 4], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void ReadsSegmentJoinFlowOnSameCarrier()
    {
        var axis = new Carrier();
        var middle = GraphicStructure.Site(0);
        var first = GraphicStructure.Segment(axis, GraphicStructure.Site(-8), middle);
        var second = GraphicStructure.Segment(axis, middle, GraphicStructure.Site(8));
        var structure = new Relation(first, second);

        var graph = GraphicReadGraph.From(structure);

        var flow = Assert.Single(graph.ReadsOf<SegmentJoinFlowRead>());
        Assert.True(flow.SharesCarrier);
        Assert.True(flow.UsesSameFace);
        Assert.False(flow.UsesOpposedFaces);
        Assert.False(flow.IsCarrierTransition);
        Assert.Equal(-8, flow.First.AwayTicks);
        Assert.Equal(8, flow.Second.AwayTicks);
        Assert.True(flow.HasOpposedLocalFlow);
        Assert.False(flow.HasSameLocalFlow);
        var compatibility = Assert.Single(graph.ReadsOf<SegmentJoinUnitCompatibilityRead>());
        Assert.True(compatibility.SharesUnitShapeIdentity);
        Assert.True(compatibility.CompatibleWithDescriptors);
        Assert.False(compatibility.HasDifferentDescriptors);
    }

    [Fact]
    public void ReadsSharedTemplateProvenanceAcrossDifferentCarrierInstances()
    {
        var descriptor = GraphicStructure.Point(new Carrier(), 1);
        var firstCarrier = new Carrier(UnitShape.DescribedAtom(descriptor));
        var secondCarrier = new Carrier(UnitShape.DescribedAtom(descriptor));
        var middle = GraphicStructure.Site(0);
        var first = GraphicStructure.Segment(firstCarrier, GraphicStructure.Site(-8), middle);
        var second = GraphicStructure.Segment(secondCarrier, middle, GraphicStructure.Site(8));
        var structure = new Relation(first, second);

        var graph = GraphicReadGraph.From(structure);

        Assert.Equal(2, graph.ReadsOf<CarrierUnitRead>().Count());
        var compatibility = Assert.Single(graph.ReadsOf<SegmentJoinUnitCompatibilityRead>());
        Assert.False(compatibility.SharesUnitShapeIdentity);
        Assert.True(compatibility.AlgebraicallyCompatible);
        Assert.True(compatibility.CompatibleWithDescriptors);
        Assert.True(compatibility.HasSharedTemplateProvenance);
        Assert.False(compatibility.HasDifferentDescriptors);
        Assert.Equal(1, compatibility.First.DescriptorCount);
        Assert.Equal(1, compatibility.Second.DescriptorCount);
    }

    [Fact]
    public void ReadsDifferentUnitProvenanceAcrossCompatibleAlgebra()
    {
        var lightDescriptor = GraphicStructure.Point(new Carrier(), 1);
        var heatDescriptor = GraphicStructure.Point(new Carrier(), 1);
        var lightCarrier = new Carrier(UnitShape.DescribedAtom(lightDescriptor));
        var heatCarrier = new Carrier(UnitShape.DescribedAtom(heatDescriptor));
        var middle = GraphicStructure.Site(0);
        var light = GraphicStructure.Segment(lightCarrier, GraphicStructure.Site(-8), middle);
        var heat = GraphicStructure.Segment(heatCarrier, middle, GraphicStructure.Site(8));
        var structure = new Relation(light, heat);

        var graph = GraphicReadGraph.From(structure);

        var compatibility = Assert.Single(graph.ReadsOf<SegmentJoinUnitCompatibilityRead>());
        Assert.True(compatibility.AlgebraicallyCompatible);
        Assert.False(compatibility.CompatibleWithDescriptors);
        Assert.True(compatibility.HasDifferentDescriptors);
        Assert.False(compatibility.HasDifferentAlgebra);
    }

    [Fact]
    public void ReadsOpenSegmentPathFromSharedCarrierSegments()
    {
        var axis = new Carrier();
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(8);
        var thirdSite = GraphicStructure.Site(16);
        var fourthSite = GraphicStructure.Site(24);
        var fifthSite = GraphicStructure.Site(32);
        var first = GraphicStructure.Segment(axis, firstSite, secondSite);
        var second = GraphicStructure.Segment(axis, secondSite, thirdSite);
        var third = GraphicStructure.Segment(axis, thirdSite, fourthSite);
        var fourth = GraphicStructure.Segment(axis, fourthSite, fifthSite);
        var structure = Chain(first, second, third, fourth);

        var graph = GraphicReadGraph.From(structure);

        var path = Assert.Single(graph.ReadsOf<OpenSegmentPathRead>());
        Assert.Equal([first, second, third, fourth], path.Segments.Select(segment => segment.Source).ToArray());
        Assert.Equal(4, path.Segments.Count);
        Assert.Equal(3, path.Joins.Count);
        Assert.True(path.HasSingleCarrier);
        Assert.False(path.HasCarrierTransitions);
        Assert.True(path.HasCompatibleUnitProvenance);
        Assert.False(path.HasSharedTemplateProvenance);
    }

    [Fact]
    public void ReadsOpenSegmentPathFromRepeatedStepTemplateInstances()
    {
        var templateBasis = new Carrier();
        var arcRadiusTemplate = new Relation(
            GraphicStructure.Segment(templateBasis, 0, 8),
            GraphicStructure.Point(templateBasis, 3));
        var sites = new[]
        {
            GraphicStructure.Site(0),
            GraphicStructure.Site(8),
            GraphicStructure.Site(16),
            GraphicStructure.Site(24),
            GraphicStructure.Site(32)
        };
        var segments = Enumerable.Range(0, 4)
            .Select(index =>
            {
                var carrier = new Carrier(UnitShape.DescribedAtom(arcRadiusTemplate));
                return GraphicStructure.Segment(carrier, sites[index], sites[index + 1]);
            })
            .ToArray();
        var structure = Chain(segments);

        var graph = GraphicReadGraph.From(structure);

        var path = Assert.Single(graph.ReadsOf<OpenSegmentPathRead>());
        Assert.Equal(4, path.Segments.Count);
        Assert.Equal(3, path.Joins.Count);
        Assert.False(path.HasSingleCarrier);
        Assert.True(path.HasCarrierTransitions);
        Assert.True(path.HasCompatibleUnitProvenance);
        Assert.True(path.HasSharedTemplateProvenance);
        Assert.All(path.UnitCompatibilities, compatibility =>
        {
            Assert.True(compatibility.HasSharedTemplateProvenance);
            Assert.True(compatibility.CompatibleWithDescriptors);
        });
    }

    [Fact]
    public void ReadsRelativeAngleStepRepeatSurface()
    {
        var repeat = GraphicStructure.RelativeAngleStepRepeat(count: 3);

        var graph = GraphicReadGraph.From(repeat);

        var template = Assert.Single(graph.ReadsOf<GraphicRepeatTemplateRead>());
        Assert.Equal(4, template.InputPortList.Count);
        Assert.Equal(2, template.OutputPortList.Count);
        var repeatRead = Assert.Single(graph.ReadsOf<GraphicRepeatRead>());
        Assert.Equal(template.Source, repeatRead.Template.Source);
        Assert.True(RepeatSurface.TryReadSpan(repeatRead.Controller, out var start, out var end));
        Assert.Equal(0, start.Ticks);
        Assert.Equal(3, end.Ticks);
        Assert.Equal(2, repeatRead.TransitionBindings.Count);
        Assert.Equal(2, repeatRead.FeedbackBindings.Count);
        Assert.Equal([0, 1], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void ReadsOpenSegmentPathFromInverseSegments()
    {
        var axis = new Carrier();
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(8);
        var thirdSite = GraphicStructure.Site(16);
        var fourthSite = GraphicStructure.Site(24);
        var first = GraphicStructure.Segment(axis.Inverse(), firstSite, secondSite);
        var second = GraphicStructure.Segment(axis.Inverse(), secondSite, thirdSite);
        var third = GraphicStructure.Segment(axis.Inverse(), thirdSite, fourthSite);
        var structure = Chain(first, second, third);

        var graph = GraphicReadGraph.From(structure);

        Assert.Equal(3, graph.ReadsOf<InverseSegmentRead>().Count());
        var path = Assert.Single(graph.ReadsOf<OpenSegmentPathRead>());
        Assert.True(path.IsAllInverse);
        Assert.False(path.IsMixedSegmentForm);
        Assert.Equal([first, second, third], path.Segments.Select(segment => segment.Source).ToArray());
    }

    [Fact]
    public void ReadsOpenSegmentPathFromDirectedSegments()
    {
        var axis = new Carrier();
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(8);
        var thirdSite = GraphicStructure.Site(16);
        var fourthSite = GraphicStructure.Site(24);
        var first = GraphicStructure.DirectedSegment(axis, firstSite, secondSite);
        var second = GraphicStructure.DirectedSegment(axis, secondSite, thirdSite);
        var third = GraphicStructure.DirectedSegment(axis, thirdSite, fourthSite);
        var structure = Chain(first, second, third);

        var graph = GraphicReadGraph.From(structure);

        Assert.Equal(3, graph.ReadsOf<DirectedSegmentRead>().Count());
        var path = Assert.Single(graph.ReadsOf<OpenSegmentPathRead>());
        Assert.True(path.IsAllDirected);
        Assert.False(path.IsMixedSegmentForm);
        Assert.Equal([first, second, third], path.Segments.Select(segment => segment.Source).ToArray());
    }

    [Fact]
    public void ReadsDirectedBranchAtPathJunction()
    {
        var axis = new Carrier();
        var junction = GraphicStructure.Site(0);
        var incoming = GraphicStructure.DirectedSegment(axis, GraphicStructure.Site(-9), junction);
        var firstOutgoing = GraphicStructure.DirectedSegment(axis, junction, GraphicStructure.Site(8));
        var secondOutgoing = GraphicStructure.DirectedSegment(axis, junction, GraphicStructure.Site(15));
        var structure = Chain(incoming, firstOutgoing, secondOutgoing);

        var graph = GraphicReadGraph.From(structure);

        var junctionRead = Assert.Single(graph.ReadsOf<PathJunctionRead>());
        Assert.Equal(3, junctionRead.Endpoints.Count);
        var branch = Assert.Single(graph.ReadsOf<BranchRead>());
        Assert.Same(junctionRead, branch.Junction);
        Assert.Single(branch.Incoming);
        Assert.Equal(2, branch.Outgoing.Count);
        Assert.Empty(graph.ReadsOf<MergeRead>());
        Assert.Empty(graph.ReadsOf<OpenSegmentPathRead>());
    }

    [Fact]
    public void ReadsDirectedMergeAtPathJunction()
    {
        var axis = new Carrier();
        var junction = GraphicStructure.Site(0);
        var firstIncoming = GraphicStructure.DirectedSegment(axis, GraphicStructure.Site(-9), junction);
        var secondIncoming = GraphicStructure.DirectedSegment(axis, GraphicStructure.Site(-15), junction);
        var outgoing = GraphicStructure.DirectedSegment(axis, junction, GraphicStructure.Site(8));
        var structure = Chain(firstIncoming, secondIncoming, outgoing);

        var graph = GraphicReadGraph.From(structure);

        var junctionRead = Assert.Single(graph.ReadsOf<PathJunctionRead>());
        Assert.Equal(3, junctionRead.Endpoints.Count);
        var merge = Assert.Single(graph.ReadsOf<MergeRead>());
        Assert.Same(junctionRead, merge.Junction);
        Assert.Equal(2, merge.Incoming.Count);
        Assert.Single(merge.Outgoing);
        Assert.Empty(graph.ReadsOf<BranchRead>());
        Assert.Empty(graph.ReadsOf<OpenSegmentPathRead>());
    }

    [Fact]
    public void KeepsMixedPathJunctionWithoutBranchOrMergeName()
    {
        var axis = new Carrier();
        var junction = GraphicStructure.Site(0);
        var incoming = GraphicStructure.DirectedSegment(axis, GraphicStructure.Site(-9), junction);
        var outgoing = GraphicStructure.DirectedSegment(axis, junction, GraphicStructure.Site(8));
        var plain = GraphicStructure.Segment(axis, junction, GraphicStructure.Site(15));
        var structure = Chain(incoming, outgoing, plain);

        var graph = GraphicReadGraph.From(structure);

        Assert.Single(graph.ReadsOf<PathJunctionRead>());
        Assert.Empty(graph.ReadsOf<BranchRead>());
        Assert.Empty(graph.ReadsOf<MergeRead>());
    }

    [Fact]
    public void ReadsMixedConnectedPathWithoutCallingItUniformPolyline()
    {
        var axis = new Carrier();
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(8);
        var thirdSite = GraphicStructure.Site(16);
        var fourthSite = GraphicStructure.Site(24);
        var positive = GraphicStructure.Segment(axis, firstSite, secondSite);
        var directed = GraphicStructure.DirectedSegment(axis, secondSite, thirdSite);
        var inverse = GraphicStructure.Segment(axis.Inverse(), thirdSite, fourthSite);
        var structure = Chain(positive, directed, inverse);

        var graph = GraphicReadGraph.From(structure);

        var path = Assert.Single(graph.ReadsOf<OpenSegmentPathRead>());
        Assert.True(path.IsMixedSegmentForm);
        Assert.False(path.IsAllPositive);
        Assert.False(path.IsAllDirected);
        Assert.False(path.IsAllInverse);
        Assert.Equal([positive, directed, inverse], path.Segments.Select(segment => segment.Source).ToArray());
    }

    [Fact]
    public void ReadsConnectedPathThroughBoundEndpointRelations()
    {
        var axisA = new Carrier();
        var axisB = new Carrier();
        var axisC = new Carrier();
        var first = GraphicStructure.Segment(axisA, 0, 8);
        var second = GraphicStructure.Segment(axisB, 10, 18);
        var third = GraphicStructure.Segment(axisC, 20, 28);
        var firstJoin = new Relation(
            GraphicStructure.Point(axisA, 8),
            GraphicStructure.Point(axisB, 10));
        var secondJoin = new Relation(
            GraphicStructure.Point(axisB, 18),
            GraphicStructure.Point(axisC, 20));
        var structure = Chain(first, firstJoin, second, secondJoin, third);

        var graph = GraphicReadGraph.From(structure);

        var path = Assert.Single(graph.ReadsOf<OpenSegmentPathRead>());
        Assert.True(path.IsAllPositive);
        Assert.Equal([first, second, third], path.Segments.Select(segment => segment.Source).ToArray());
    }

    [Fact]
    public void ReadsSharedFramePolylineFromCoordinatePointSegments()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var points = new[]
        {
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 0)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, 4)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 16), new CarrierLocation(y, -3)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 24), new CarrierLocation(y, 5)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 32), new CarrierLocation(y, 1))
        };
        var segments = Enumerable.Range(0, 4)
            .Select(index => GraphicStructure.FrameSegment(points[index], points[index + 1]))
            .ToArray();
        var structure = Chain(segments);

        var graph = GraphicReadGraph.From(structure);

        Assert.Equal(4, graph.ReadsOf<SharedFrameSegmentRead>().Count());
        var path = Assert.Single(graph.ReadsOf<SharedFramePolylineRead>());
        Assert.Equal(2, path.CoordinateCount);
        Assert.Equal(4, path.Segments.Count);
        Assert.Equal(3, path.Sites.Count);
        Assert.Contains(path.Segments.SelectMany(segment => new[] { segment.Start, segment.End }), point =>
            point.Coordinates[1].Ticks != 0);
        Assert.Equal([segments[0], segments[1], segments[2], segments[3]], path.Segments.Select(segment => segment.Source).ToArray());
    }

    [Fact]
    public void ReadsSharedFrameArcFromCoordinatePointTriple()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var center = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 5), new CarrierLocation(y, 6));
        var start = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 5), new CarrierLocation(y, 11));
        var end = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 10), new CarrierLocation(y, 6));
        var arcElement = GraphicStructure.FrameArc(center, start, end);

        var graph = GraphicReadGraph.From(arcElement);

        var arc = Assert.Single(graph.ReadsOf<SharedFrameArcRead>());
        Assert.Equal(center, arc.Center.Source);
        Assert.Equal(start, arc.Start.Source);
        Assert.Equal(end, arc.End.Source);
        Assert.Equal(arc.Span, Assert.Single(graph.ReadsOf<SharedFrameSegmentRead>()));
    }

    [Fact]
    public void ReadsSharedFrameBezierCurvesFromCoordinateControls()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var start = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 0));
        var control = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 4), new CarrierLocation(y, 8));
        var firstControl = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 3), new CarrierLocation(y, 10));
        var secondControl = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, 10));
        var end = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 10), new CarrierLocation(y, 0));
        var quadratic = GraphicStructure.FrameQuadraticCurve(start, control, end);
        var cubic = GraphicStructure.FrameCubicCurve(start, firstControl, secondControl, end);
        var structure = Chain(quadratic, cubic);

        var graph = GraphicReadGraph.From(structure);

        var quadraticRead = Assert.Single(graph.ReadsOf<SharedFrameQuadraticCurveRead>());
        Assert.Equal(control, quadraticRead.Control.Source);
        var cubicRead = Assert.Single(graph.ReadsOf<SharedFrameCubicCurveRead>());
        Assert.Equal(firstControl, cubicRead.FirstControl.Source);
        Assert.Equal(secondControl, cubicRead.SecondControl.Source);
    }

    [Fact]
    public void ReadsSharedFrameTransitionClothoidAndInvoluteControls()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var lowerLeft = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 0));
        var corner = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, 0));
        var upperRight = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, 8));
        var lengthEnd = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 4), new CarrierLocation(y, 4));
        var incoming = GraphicStructure.FrameSegment(lowerLeft, corner);
        var outgoing = GraphicStructure.FrameSegment(corner, upperRight);
        var transition = GraphicStructure.FrameCurveTransition(incoming, outgoing);
        var length = GraphicStructure.FrameSegment(corner, lengthEnd);
        var clothoid = GraphicStructure.FrameClothoidTransition(transition, length);
        var center = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 4), new CarrierLocation(y, 4));
        var arc = GraphicStructure.FrameArc(center, lowerLeft, corner);
        var involute = GraphicStructure.FrameInvoluteCurve(arc, length);
        var structure = Chain(clothoid, involute);

        var graph = GraphicReadGraph.From(structure);

        var transitionRead = Assert.Single(graph.ReadsOf<SharedFrameCurveTransitionRead>());
        Assert.Equal(corner, transitionRead.Junction.Source);
        var clothoidRead = Assert.Single(graph.ReadsOf<SharedFrameClothoidTransitionRead>());
        Assert.Equal(length, clothoidRead.Length.Source);
        var involuteRead = Assert.Single(graph.ReadsOf<SharedFrameInvoluteCircleRead>());
        Assert.Equal(length, involuteRead.UnwoundLength.Source);
    }

    [Fact]
    public void ReadsCarrierNativeTransitionAndClothoidControls()
    {
        var baseCarrier = new Carrier();
        var origin = GraphicStructure.Point(baseCarrier, 0);
        var junction = GraphicStructure.Site(0);
        var incomingStart = GraphicStructure.Site(-8);
        var outgoingEnd = GraphicStructure.Site(8);
        var incomingCarrier = GraphicStructure.LinearCarrierAt(origin);
        var outgoingCarrier = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(incomingCarrier, junction, junction)));
        var incoming = GraphicStructure.Segment(incomingCarrier, incomingStart, junction);
        var outgoing = GraphicStructure.Segment(outgoingCarrier, junction, outgoingEnd);
        var transition = GraphicStructure.CarrierCurveTransition(incoming, outgoing);
        var length = GraphicStructure.Segment(incomingCarrier, junction, GraphicStructure.Site(12));
        var clothoid = GraphicStructure.CarrierClothoidTransition(transition, length);

        var graph = GraphicReadGraph.From(clothoid);

        var transitionRead = Assert.Single(graph.ReadsOf<CarrierCurveTransitionRead>());
        Assert.True(transitionRead.IsLineToLine);
        Assert.True(transitionRead.IsJoined);
        Assert.NotNull(transitionRead.Junction);
        Assert.Equal(junction.Id, transitionRead.Junction.Location.Tick?.Id);
        var clothoidRead = Assert.Single(graph.ReadsOf<CarrierClothoidTransitionRead>());
        Assert.Same(transitionRead, clothoidRead.Transition);
        Assert.Equal(length, clothoidRead.Length.Source);
    }

    [Fact]
    public void ReadsCarrierNativeArcToLineClothoidControl()
    {
        var baseCarrier = new Carrier();
        var origin = GraphicStructure.Point(baseCarrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        var arcStart = GraphicStructure.Site(0);
        var arcEnd = GraphicStructure.Site(6);
        var lineEnd = GraphicStructure.Site(14);
        var arc = GraphicStructure.Segment(circle, arcStart, arcEnd);
        var tangent = GraphicStructure.LinearCarrierAt(GraphicStructure.Point(circle, arcEnd));
        var line = GraphicStructure.Segment(tangent, arcEnd, lineEnd);
        var transition = GraphicStructure.CarrierCurveTransition(arc, line);
        var length = GraphicStructure.Segment(tangent, arcEnd, GraphicStructure.Site(18));
        var clothoid = GraphicStructure.CarrierClothoidTransition(transition, length);

        var graph = GraphicReadGraph.From(clothoid);

        var transitionRead = Assert.Single(graph.ReadsOf<CarrierCurveTransitionRead>());
        Assert.True(transitionRead.IsArcToLine);
        Assert.True(transitionRead.IsJoined);
        Assert.NotNull(transitionRead.Junction);
        Assert.Equal(arcEnd.Id, transitionRead.Junction.Location.Tick?.Id);
        Assert.Single(graph.ReadsOf<CarrierClothoidTransitionRead>());
    }

    [Fact]
    public void ReadsCarrierNativeBridgedParallelLineClothoidControl()
    {
        var firstCarrier = new Carrier();
        var firstStart = GraphicStructure.Site(0);
        var firstEnd = GraphicStructure.Site(8);
        var secondCarrier = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(firstCarrier, 4, 4)));
        var secondStart = GraphicStructure.Site(0);
        var secondEnd = GraphicStructure.Site(8);
        var incoming = GraphicStructure.Segment(firstCarrier, firstStart, firstEnd);
        var outgoing = GraphicStructure.Segment(secondCarrier, secondStart, secondEnd);
        var transition = GraphicStructure.CarrierCurveTransition(incoming, outgoing);
        var length = GraphicStructure.Segment(firstCarrier, firstEnd, GraphicStructure.Site(16));
        var clothoid = GraphicStructure.CarrierClothoidTransition(transition, length);

        var graph = GraphicReadGraph.From(clothoid);

        var transitionRead = Assert.Single(graph.ReadsOf<CarrierCurveTransitionRead>());
        Assert.True(transitionRead.IsLineToLine);
        Assert.True(transitionRead.IsBridged);
        Assert.Null(transitionRead.Junction);
        Assert.Equal(firstEnd.Id, transitionRead.Entry.Location.Tick?.Id);
        Assert.Equal(secondStart.Id, transitionRead.Exit.Location.Tick?.Id);
        Assert.Single(graph.ReadsOf<CarrierClothoidTransitionRead>());
    }

    [Fact]
    public void ReadsCarrierNativeInvoluteFromCircularArcAndTangentLength()
    {
        var baseCarrier = new Carrier();
        var origin = GraphicStructure.Point(baseCarrier, 0);
        var circle = GraphicStructure.CircularCarrierAt(origin);
        var arcStart = GraphicStructure.Site(0);
        var arcEnd = GraphicStructure.Site(6);
        var arc = GraphicStructure.Segment(circle, arcStart, arcEnd);
        var tangent = GraphicStructure.LinearCarrierAt(GraphicStructure.Point(circle, arcEnd));
        var length = GraphicStructure.Segment(tangent, GraphicStructure.Site(0), GraphicStructure.Site(9));
        var involute = GraphicStructure.CarrierInvoluteCurve(arc, length);

        var graph = GraphicReadGraph.From(involute);

        var circular = Assert.Single(graph.ReadsOf<CircularSpanRead>());
        Assert.Equal(arc, circular.Source);
        var involuteRead = Assert.Single(graph.ReadsOf<CarrierInvoluteCircleRead>());
        Assert.Same(circular, involuteRead.BaseArc);
        Assert.Equal(length, involuteRead.UnwoundLength.Source);
    }

    [Fact]
    public void ReadsPathGraphFromSharedFrameLetterT()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var topLeft = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 12));
        var topCenter = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 5), new CarrierLocation(y, 12));
        var topRight = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 10), new CarrierLocation(y, 12));
        var bottomCenter = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 5), new CarrierLocation(y, 0));
        var leftTop = GraphicStructure.FrameSegment(topLeft, topCenter);
        var rightTop = GraphicStructure.FrameSegment(topCenter, topRight);
        var stem = GraphicStructure.FrameSegment(topCenter, bottomCenter);
        var structure = Chain(leftTop, rightTop, stem);

        var graph = GraphicReadGraph.From(structure);

        var pathGraph = Assert.Single(graph.ReadsOf<PathGraphRead>());
        Assert.Equal(3, pathGraph.Edges.Count);
        Assert.Equal(4, pathGraph.Nodes.Count);
        Assert.Equal(3, pathGraph.TerminalNodeCount);
        Assert.Equal(1, pathGraph.BranchNodeCount);
        Assert.False(pathGraph.HasClosedTopology);
        var branch = Assert.Single(pathGraph.Nodes, node => node.Valence == 3);
        Assert.All(branch.Endpoints, endpoint => Assert.NotNull(endpoint.SharedFramePoint));
    }

    [Fact]
    public void ReadsClosedPathGraphFromSharedFrameLetterO()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var bottomLeft = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 0));
        var bottomRight = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, 0));
        var topRight = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, 10));
        var topLeft = GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 10));
        var bottom = GraphicStructure.FrameSegment(bottomLeft, bottomRight);
        var right = GraphicStructure.FrameSegment(bottomRight, topRight);
        var top = GraphicStructure.FrameSegment(topRight, topLeft);
        var left = GraphicStructure.FrameSegment(topLeft, bottomLeft);
        var structure = Chain(bottom, right, top, left);

        var graph = GraphicReadGraph.From(structure);

        var pathGraph = Assert.Single(graph.ReadsOf<PathGraphRead>());
        Assert.Equal(4, pathGraph.Edges.Count);
        Assert.Equal(4, pathGraph.Nodes.Count);
        Assert.Equal(0, pathGraph.TerminalNodeCount);
        Assert.Equal(0, pathGraph.BranchNodeCount);
        Assert.True(pathGraph.HasClosedTopology);
        Assert.All(pathGraph.Nodes, node => Assert.Equal(2, node.Valence));
    }

    [Fact]
    public void ReadsSharedFramePointAttachmentToOrdinaryPoint()
    {
        var drawing = new Carrier();
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var framePoint = GraphicStructure.FramePoint(
            frame,
            new CarrierLocation(x, 8),
            new CarrierLocation(y, 3));
        var drawingPoint = GraphicStructure.Point(drawing, 12);
        var structure = new Relation(framePoint, drawingPoint);

        var graph = GraphicReadGraph.From(structure);

        var attachment = Assert.Single(graph.ReadsOf<SharedFramePointAttachmentRead>());
        Assert.True(attachment.FramePoint.Holder.SameIdentityAs(frame));
        Assert.True(attachment.Point.Location.Carrier.SameIdentityAs(drawing));
        Assert.Equal(12, attachment.Point.Location.Ticks);
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
        Assert.Contains(affected.OfType<SegmentJoinFlowRead>(), flow =>
            ReferenceEquals(flow.Join.First.Segment.Source, second) ||
            ReferenceEquals(flow.Join.Second.Segment.Source, second));
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
        Assert.Equal(3, graph.ReadsOf<BoundaryJoinFlowRead>().Count());
        Assert.Equal(3, graph.ReadsOf<BoundaryJoinUnitCompatibilityRead>().Count());
        Assert.All(graph.ReadsOf<BoundaryJoinFlowRead>(), flow => Assert.True(flow.IsCarrierTransition));
        Assert.All(graph.ReadsOf<BoundaryJoinUnitCompatibilityRead>(), compatibility =>
            Assert.True(compatibility.HasDifferentAlgebra));
        Assert.Empty(graph.ReadsOf<AreaRead>());
        Assert.Empty(graph.ReadsOf<SegmentJunctionRead>());
        Assert.Equal([0, 1, 2, 4], graph.Bands.Select(band => band.Order).ToArray());
    }

    [Fact]
    public void ReadsClosedSegmentBoundaryThroughEndpointAttachments()
    {
        var bottom = new Carrier();
        var left = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(bottom, 0, 0)));
        var top = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(left, 4, 4)));
        var right = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(bottom, 10, 10)));
        var bottomSegment = GraphicStructure.Segment(bottom, 0, 10);
        var leftSegment = GraphicStructure.Segment(left, 0, 4);
        var topSegment = GraphicStructure.Segment(top, 0, 10);
        var rightSegment = GraphicStructure.Segment(right, 0, 4);
        var structure = Chain(
            bottomSegment,
            leftSegment,
            topSegment,
            rightSegment,
            GraphicStructure.Pin(GraphicStructure.Point(bottom, 0), GraphicStructure.Point(left, 0)),
            GraphicStructure.Pin(GraphicStructure.Point(left, 4), GraphicStructure.Point(top, 0)),
            GraphicStructure.Pin(GraphicStructure.Point(top, 10), GraphicStructure.Point(right, 4)),
            GraphicStructure.Pin(GraphicStructure.Point(bottom, 10), GraphicStructure.Point(right, 0)));

        var graph = GraphicReadGraph.From(structure);

        var boundary = Assert.Single(graph.ReadsOf<ClosedSegmentBoundaryRead>());
        Assert.Equal(4, boundary.Segments.Count);
        Assert.Equal(4, boundary.Sites.Count);
        Assert.Equal(4, graph.ReadsOf<BoundaryJoinRead>().Count());
    }

    [Fact]
    public void ReadsAreaFromBoundaryFillParticipation()
    {
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(10);
        var thirdSite = GraphicStructure.Site(20);
        var first = GraphicStructure.Segment(new Carrier(), firstSite, secondSite);
        var second = GraphicStructure.Segment(new Carrier(), secondSite, thirdSite);
        var third = GraphicStructure.Segment(new Carrier(), thirdSite, firstSite);
        var fill = GraphicStructure.AreaFill(first, second, third);
        var structure = Chain(first, second, third, fill);

        var graph = GraphicReadGraph.From(structure);

        var boundary = Assert.Single(graph.ReadsOf<ClosedSegmentBoundaryRead>());
        var participation = Assert.Single(graph.ReadsOf<AreaParticipationRead>());
        var area = Assert.Single(graph.ReadsOf<AreaRead>());
        Assert.Same(boundary, area.Boundary);
        Assert.Same(participation, area.Participation);
        Assert.False(area.IsAntiArea);
        Assert.Equal(3, participation.Boundary.Segments.Count);
    }

    [Fact]
    public void ReadsAntiAreaFromInverseBoundaryFillParticipation()
    {
        var firstSite = GraphicStructure.Site(0);
        var secondSite = GraphicStructure.Site(10);
        var thirdSite = GraphicStructure.Site(20);
        var first = GraphicStructure.Segment(new Carrier(), firstSite, secondSite);
        var second = GraphicStructure.Segment(new Carrier(), secondSite, thirdSite);
        var third = GraphicStructure.Segment(new Carrier(), thirdSite, firstSite);
        var fill = GraphicStructure.AntiAreaFill(first, second, third);
        var structure = Chain(first, second, third, fill);

        var graph = GraphicReadGraph.From(structure);

        var area = Assert.Single(graph.ReadsOf<AreaRead>());
        Assert.True(area.IsAntiArea);
    }

    [Fact]
    public void ReadsAdjacentAreasThroughLocalFillParticipation()
    {
        var bottom = new Carrier();
        var leftLower = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(bottom, 0, 0)));
        var rightLower = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(bottom, 10, 10)));
        var middle = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(leftLower, 4, 4)));
        var leftUpper = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(middle, 0, 0)));
        var rightUpper = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(middle, 10, 10)));
        var top = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(leftUpper, 4, 4)));

        var bottomSegment = GraphicStructure.Segment(bottom, 0, 10);
        var leftLowerSegment = GraphicStructure.Segment(leftLower, 0, 4);
        var rightLowerSegment = GraphicStructure.Segment(rightLower, 0, 4);
        var middleSegment = GraphicStructure.Segment(middle, 0, 10);
        var leftUpperSegment = GraphicStructure.Segment(leftUpper, 0, 4);
        var rightUpperSegment = GraphicStructure.Segment(rightUpper, 0, 4);
        var topSegment = GraphicStructure.Segment(top, 0, 10);

        var lowerBottomLeft = GraphicStructure.Pin(GraphicStructure.Point(bottom, 0), GraphicStructure.Point(leftLower, 0));
        var lowerTopLeft = GraphicStructure.Pin(GraphicStructure.Point(leftLower, 4), GraphicStructure.Point(middle, 0));
        var lowerTopRight = GraphicStructure.Pin(GraphicStructure.Point(middle, 10), GraphicStructure.Point(rightLower, 4));
        var lowerBottomRight = GraphicStructure.Pin(GraphicStructure.Point(bottom, 10), GraphicStructure.Point(rightLower, 0));
        var upperBottomLeft = GraphicStructure.Pin(GraphicStructure.Point(middle, 0), GraphicStructure.Point(leftUpper, 0));
        var upperTopLeft = GraphicStructure.Pin(GraphicStructure.Point(leftUpper, 4), GraphicStructure.Point(top, 0));
        var upperTopRight = GraphicStructure.Pin(GraphicStructure.Point(top, 10), GraphicStructure.Point(rightUpper, 4));
        var upperBottomRight = GraphicStructure.Pin(GraphicStructure.Point(middle, 10), GraphicStructure.Point(rightUpper, 0));

        var lowerFill = GraphicStructure.AreaFill(
            bottomSegment,
            leftLowerSegment,
            middleSegment,
            rightLowerSegment,
            lowerBottomLeft,
            lowerTopLeft,
            lowerTopRight,
            lowerBottomRight);
        var upperFill = GraphicStructure.AreaFill(
            middleSegment,
            leftUpperSegment,
            topSegment,
            rightUpperSegment,
            upperBottomLeft,
            upperTopLeft,
            upperTopRight,
            upperBottomRight);
        var structure = Chain(
            bottomSegment,
            leftLowerSegment,
            rightLowerSegment,
            middleSegment,
            leftUpperSegment,
            rightUpperSegment,
            topSegment,
            lowerBottomLeft,
            lowerTopLeft,
            lowerTopRight,
            lowerBottomRight,
            upperBottomLeft,
            upperTopLeft,
            upperTopRight,
            upperBottomRight,
            lowerFill,
            upperFill);

        var graph = GraphicReadGraph.From(structure);

        Assert.Equal(2, graph.ReadsOf<AreaRead>().Count());
        Assert.Equal(2, graph.ReadsOf<AreaParticipationRead>().Count());
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
