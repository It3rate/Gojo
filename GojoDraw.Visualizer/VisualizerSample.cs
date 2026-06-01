using Gojo.Core;
using GojoCAD;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerSample(
    IElement Structure,
    int ProjectionSeed,
    IReadOnlyDictionary<ulong, int> ElementIndexes)
{
    public Carrier Drawing { get; init; } = new();

    public IReadOnlyList<IElement> Elements { get; init; } = [];

    public VisualizerSample Resample() => this with { ProjectionSeed = Random.Shared.Next() };

    public int? SourceIndexOf(IElement? source) =>
        source is Relation relation && ElementIndexes.TryGetValue(relation.Id, out var index)
            ? index
            : null;

    public static IReadOnlyList<VisualizerLegendPage> CreateLegendPages() =>
    [
        new VisualizerLegendPage("Primitives",
        [
            LegendEntry("point", CreatePointLegend(), 101),
            LegendEntry("radial", CreateRadialLegend(), 102),
            LegendEntry("line seed", CreateLineSeedLegend(), 106),
            LegendEntry("circumference", CreateCircumferenceLegend(), 109),
            LegendEntry("radius", CreateRadiusAttachmentLegend(), 108),
            LegendEntry("span", CreateSpanLegend(), 103),
            LegendEntry("directed span", CreateDirectedSpanLegend(), 105),
            LegendEntry("partition", CreateSegmentPartitionLegend(), 104),
            LegendEntry("sheet seed", CreateSheetSeedLegend(), 107)
        ]),
        new VisualizerLegendPage("Combinations",
        [
            LegendEntry("coordinate path", CreateSharedFramePolylineLegend(), 111),
            LegendEntry("relative path", CreateCircularAngleLegend(), 112),
            LegendEntry("direction length", CreateCircularAngleLegend(), 121),
            LegendEntry("parallel lines", CreateParallelLinesLegend(), 114),
            LegendEntry("circle offset", CreateCircleOffsetLegend(), 115),
            LegendEntry("directed path", CreateDirectedPathLegend(), 116),
            LegendEntry("area", CreateAreaLegend(), 117),
            LegendEntry("anti-area", CreateAntiAreaLegend(), 118),
            LegendEntry("branch", CreateBranchLegend(), 119),
            LegendEntry("merge", CreateMergeLegend(), 120)
        ])
    ];

    public static IReadOnlyList<VisualizerLegendEntry> CreateLegend() =>
        CreateLegendPages().SelectMany(page => page.Entries).ToArray();

    public static VisualizerSample Create()
    {
        var drawing = new Carrier();
        var hubSite = GraphicStructure.Site(0);
        var nearSite = GraphicStructure.Site(3);
        var midSite = GraphicStructure.Site(6);
        var radiusSite = GraphicStructure.Site(8);
        var highSite = GraphicStructure.Site(9);
        var sheetEndSite = GraphicStructure.Site(10);
        var farSite = GraphicStructure.Site(42);
        var outerSite = GraphicStructure.Site(16);
        var tailSite = GraphicStructure.Site(25);

        var contourA = GraphicStructure.Site(8);
        var contourB = GraphicStructure.Site(12);
        var contourC = GraphicStructure.Site(16);
        var contourD = GraphicStructure.Site(20);
        var contourE = GraphicStructure.Site(24);
        var contourF = GraphicStructure.Site(28);

        var axisOrigin = GraphicStructure.Site(0);
        var arrowBack = GraphicStructure.Site(3);
        var arrowForward = GraphicStructure.Site(7);
        var arrowFar = GraphicStructure.Site(11);
        var rimArrowBack = GraphicStructure.Site(2);
        var rimArrowMid = GraphicStructure.Site(4);
        var rimArrowForward = GraphicStructure.Site(6);

        var hub = GraphicStructure.Point(drawing, hubSite);
        var farPoint = GraphicStructure.Point(drawing, farSite);
        var mainSegment = GraphicStructure.Segment(drawing, hubSite, farSite);
        var extensionSegment = GraphicStructure.Segment(drawing, farSite, tailSite);
        var diagonalSegment = GraphicStructure.Segment(drawing, nearSite, outerSite);
        var sheetSource = GraphicStructure.Segment(drawing, nearSite, sheetEndSite);
        var zeroSpan = GraphicStructure.Segment(drawing, midSite, midSite);

        var circumference = GraphicStructure.CircularCarrierAt(hub);
        var firstRadiusAttachment = GraphicStructure.Pin(
            GraphicStructure.Point(drawing, radiusSite),
            GraphicStructure.Point(circumference, contourA));
        var secondRadiusAttachment = GraphicStructure.Pin(
            GraphicStructure.Point(drawing, midSite),
            GraphicStructure.Point(circumference, midSite));
        var angleRadialAttachment = GraphicStructure.Pin(
            GraphicStructure.Point(drawing, hubSite),
            GraphicStructure.Point(circumference, contourB));

        var arrowAxis = GraphicStructure.LinearCarrierAt(drawing, hubSite);
        var arrowOriginAttachment = GraphicStructure.Pin(
            GraphicStructure.Point(drawing, hubSite),
            GraphicStructure.Point(arrowAxis, axisOrigin));

        var rimAxis = GraphicStructure.LinearCarrierAt(drawing, radiusSite);
        var rimAxisOriginAttachment = GraphicStructure.Pin(
            GraphicStructure.Point(drawing, radiusSite),
            GraphicStructure.Point(rimAxis, axisOrigin));
        var arrowAxisArcAttachment = GraphicStructure.Pin(
            GraphicStructure.Point(arrowAxis, arrowFar),
            GraphicStructure.Point(circumference, contourB));

        var frame = new Carrier();
        var frameX = new Carrier();
        var frameY = new Carrier();
        var framePoints = new[]
        {
            GraphicStructure.FramePoint(frame, new CarrierLocation(frameX, 0), new CarrierLocation(frameY, -24)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(frameX, 8), new CarrierLocation(frameY, -18)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(frameX, 16), new CarrierLocation(frameY, -29)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(frameX, 24), new CarrierLocation(frameY, -20)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(frameX, 32), new CarrierLocation(frameY, -27))
        };
        var framePolyline = Enumerable.Range(0, 4)
            .Select(index => GraphicStructure.FrameSegment(framePoints[index], framePoints[index + 1]))
            .ToArray();
        var frameAnchor = GraphicStructure.Pin(framePoints[0], GraphicStructure.Point(drawing, outerSite));

        var angleLineStart = GraphicStructure.Site(0);
        var angleLineAEnd = GraphicStructure.Site(9);
        var angleLineARadius = GraphicStructure.Site(15);
        var angleLineBEnd = GraphicStructure.Site(8);
        var angleLineBRadius = GraphicStructure.Site(13);
        var angleLineCEnd = GraphicStructure.Site(7);
        var angleCircleAnchor = GraphicStructure.Site(0);
        var angleCirclePeriod = GraphicStructure.Site(24);
        var angleCircleAPartition = GraphicStructure.Site(5);
        var angleCircleBPartition = GraphicStructure.Site(-4);
        var angleCircleCPartition = GraphicStructure.Site(6);

        var anglePathStart = GraphicStructure.Point(drawing, tailSite);
        var angleLineA = GraphicStructure.LinearCarrierAt(anglePathStart);
        var angleCircleA = GraphicStructure.CircularCarrierAt(anglePathStart);
        var angleSegmentA = GraphicStructure.Segment(angleLineA, angleLineStart, angleLineAEnd);
        var angleLineAStartPoint = GraphicStructure.Point(angleLineA, angleLineStart);
        var angleLineAEndPoint = GraphicStructure.Point(angleLineA, angleLineAEnd);
        var angleStartAttachment = GraphicStructure.Pin(
            anglePathStart,
            angleLineAStartPoint);
        var angleRadiusA = GraphicStructure.Pin(
            GraphicStructure.Point(drawing, 35),
            GraphicStructure.Point(angleCircleA, angleCircleAnchor));
        var anglePartitionA = GraphicStructure.Point(angleCircleA, angleCircleAPartition);
        var anglePeriodA = GraphicStructure.Point(angleCircleA, angleCirclePeriod);
        var angleWitnessA = GraphicStructure.Pin(
            angleLineAEndPoint,
            anglePartitionA);

        var angleLineB = GraphicStructure.LinearCarrierAt(angleLineAEndPoint);
        var angleCircleB = GraphicStructure.CircularCarrierAt(angleLineAEndPoint);
        var angleSegmentB = GraphicStructure.Segment(angleLineB, angleLineStart, angleLineBEnd);
        var angleLineBStartPoint = GraphicStructure.Point(angleLineB, angleLineStart);
        var angleLineBEndPoint = GraphicStructure.Point(angleLineB, angleLineBEnd);
        var angleJoinAB = GraphicStructure.Pin(
            angleLineAEndPoint,
            angleLineBStartPoint);
        var angleRadiusB = GraphicStructure.Pin(
            GraphicStructure.Point(angleLineA, angleLineARadius),
            GraphicStructure.Point(angleCircleB, angleCircleAnchor));
        var anglePartitionB = GraphicStructure.Point(angleCircleB, angleCircleBPartition);
        var anglePeriodB = GraphicStructure.Point(angleCircleB, angleCirclePeriod);
        var angleWitnessB = GraphicStructure.Pin(
            angleLineBEndPoint,
            anglePartitionB);

        var angleLineC = GraphicStructure.LinearCarrierAt(angleLineBEndPoint);
        var angleCircleC = GraphicStructure.CircularCarrierAt(angleLineBEndPoint);
        var angleSegmentC = GraphicStructure.Segment(angleLineC, angleLineStart, angleLineCEnd);
        var angleLineCStartPoint = GraphicStructure.Point(angleLineC, angleLineStart);
        var angleLineCEndPoint = GraphicStructure.Point(angleLineC, angleLineCEnd);
        var angleJoinBC = GraphicStructure.Pin(
            angleLineBEndPoint,
            angleLineCStartPoint);
        var angleRadiusC = GraphicStructure.Pin(
            GraphicStructure.Point(angleLineB, angleLineBRadius),
            GraphicStructure.Point(angleCircleC, angleCircleAnchor));
        var anglePartitionC = GraphicStructure.Point(angleCircleC, angleCircleCPartition);
        var anglePeriodC = GraphicStructure.Point(angleCircleC, angleCirclePeriod);
        var angleWitnessC = GraphicStructure.Pin(
            angleLineCEndPoint,
            anglePartitionC);

        var elements = new IElement[]
        {
            GraphicStructure.RadialFlow(hub),
            GraphicStructure.RadialFlow(farPoint),
            mainSegment,
            GraphicStructure.Point(drawing, nearSite),
            GraphicStructure.Point(drawing, midSite),
            GraphicStructure.Point(drawing, highSite),
            extensionSegment,
            GraphicStructure.Point(drawing, outerSite),
            diagonalSegment,
            GraphicStructure.Point(drawing, sheetEndSite),
            zeroSpan,
            Relation.OrthogonalOutlet(zeroSpan),
            Relation.OrthogonalOutlet(sheetSource),
            firstRadiusAttachment,
            secondRadiusAttachment,
            angleRadialAttachment,
            GraphicStructure.Segment(circumference, contourA, contourC),
            GraphicStructure.Point(circumference, contourB),
            GraphicStructure.Segment(circumference, contourC, contourE),
            GraphicStructure.Point(circumference, contourD),
            GraphicStructure.Point(circumference, contourF),
            arrowOriginAttachment,
            GraphicStructure.DirectedSegment(arrowAxis, arrowBack, arrowForward),
            GraphicStructure.Segment(arrowAxis, axisOrigin, arrowFar),
            GraphicStructure.Point(arrowAxis, arrowForward),
            rimAxisOriginAttachment,
            GraphicStructure.DirectedSegment(rimAxis, rimArrowBack, rimArrowForward),
            GraphicStructure.Segment(rimAxis, axisOrigin, rimArrowForward),
            GraphicStructure.Point(rimAxis, rimArrowMid),
            arrowAxisArcAttachment,
            frameAnchor,
            framePolyline[0],
            framePolyline[1],
            framePolyline[2],
            framePolyline[3],
            angleStartAttachment,
            angleSegmentA,
            angleRadiusA,
            anglePartitionA,
            anglePeriodA,
            angleWitnessA,
            angleJoinAB,
            angleSegmentB,
            angleRadiusB,
            anglePartitionB,
            anglePeriodB,
            angleWitnessB,
            angleJoinBC,
            angleSegmentC,
            angleRadiusC,
            anglePartitionC,
            anglePeriodC,
            angleWitnessC
        };

        var structure = Chain(elements);

        return new VisualizerSample(structure, Random.Shared.Next(), IndexElements(elements))
        {
            Drawing = drawing,
            Elements = elements
        };
    }

    private static VisualizerLegendEntry LegendEntry(string label, IElement structure, int seed) =>
        new(label, structure, seed);

    public static IElement CreateLinearSpanLegend()
    {
        var carrier = new Carrier();
        return GraphicStructure.Segment(carrier, 0, 8);
    }

    public static IElement CreateCircularSpanLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = new Carrier(UnitShape.CircularFrom(origin));
        var span = GraphicStructure.Segment(circle, 8, 18);
        var partition = GraphicStructure.Point(circle, 13);
        return Chain([span, partition]);
    }

    public static IElement CreateLinearDirectedSpanLegend()
    {
        var carrier = new Carrier();
        return GraphicStructure.DirectedSegment(carrier, 2, 8);
    }

    public static IElement CreateCircularDirectedSpanLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = new Carrier(UnitShape.CircularFrom(origin));
        return GraphicStructure.DirectedSegment(circle, 3, 10);
    }

    private static IElement CreatePointLegend()
    {
        var carrier = new Carrier();
        return GraphicStructure.Point(carrier, 0);
    }

    private static IElement CreateRadialLegend()
    {
        var carrier = new Carrier();
        var point = GraphicStructure.Point(carrier, 0);
        return GraphicStructure.RadialFlow(point);
    }

    private static IElement CreateSpanLegend()
    {
        return Chain([CreateLinearSpanLegend(), CreateCircularSpanLegend()]);
    }

    private static IElement CreateSegmentPartitionLegend()
    {
        var carrier = new Carrier();
        var segment = GraphicStructure.Segment(carrier, 0, 8);
        var partitionA = GraphicStructure.Point(carrier, 2);
        var partitionB = GraphicStructure.Point(carrier, 4);
        var partitionC = GraphicStructure.Point(carrier, 6);
        return Chain([segment, partitionA, partitionB, partitionC]);
    }

    private static IElement CreateDirectedSpanLegend()
    {
        return Chain([CreateLinearDirectedSpanLegend(), CreateCircularDirectedSpanLegend()]);
    }

    private static IElement CreateLineSeedLegend()
    {
        var carrier = new Carrier();
        var zeroSpan = GraphicStructure.Segment(carrier, 4, 4);
        return Relation.OrthogonalOutlet(zeroSpan);
    }

    private static IElement CreateSheetSeedLegend()
    {
        var carrier = new Carrier();
        var segment = GraphicStructure.Segment(carrier, 0, 8);
        return Relation.OrthogonalOutlet(segment);
    }

    private static IElement CreateRadiusAttachmentLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = new Carrier(UnitShape.CircularFrom(origin));
        return new Relation(
            GraphicStructure.Point(carrier, 7),
            GraphicStructure.Point(circle, 7));
    }

    private static IElement CreateCircumferenceLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = new Carrier(UnitShape.CircularFrom(origin));
        return GraphicStructure.Point(circle, 8);
    }

    private static IElement CreateSharedFramePolylineLegend()
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var points = new[]
        {
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 0), new CarrierLocation(y, 0)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 4), new CarrierLocation(y, 3)),
            GraphicStructure.FramePoint(frame, new CarrierLocation(x, 8), new CarrierLocation(y, -1))
        };
        return Chain([
            GraphicStructure.FrameSegment(points[0], points[1]),
            GraphicStructure.FrameSegment(points[1], points[2])
        ]);
    }

    private static IElement CreateRepeatedStepPathLegend()
    {
        var templateBasis = new Carrier();
        var template = new Relation(
            GraphicStructure.Segment(templateBasis, 0, 4),
            GraphicStructure.Point(templateBasis, 2));
        var sites = new[]
        {
            GraphicStructure.Site(0),
            GraphicStructure.Site(4),
            GraphicStructure.Site(8),
            GraphicStructure.Site(12)
        };
        return Chain(Enumerable.Range(0, 3)
            .Select(index =>
            {
                var carrier = new Carrier(UnitShape.DescribedAtom(template));
                return (IElement)GraphicStructure.Segment(carrier, sites[index], sites[index + 1]);
            })
            .ToArray());
    }

    private static IElement CreateCircularAngleLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = new Carrier(UnitShape.CircularFrom(origin));
        var radialBasis = new Relation(
            GraphicStructure.Point(carrier, 6),
            GraphicStructure.Point(circle, 8));
        var arc = GraphicStructure.Segment(circle, 8, 13);
        var centerToArc = new Relation(
            GraphicStructure.Point(carrier, 0),
            GraphicStructure.Point(circle, 13));
        var ray = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(carrier, 0, 0)));
        var rayToArc = new Relation(
            GraphicStructure.Point(ray, 6),
            GraphicStructure.Point(circle, 13));
        var raySegment = GraphicStructure.Segment(ray, 0, 6);

        return Chain([radialBasis, arc, centerToArc, rayToArc, raySegment]);
    }

    private static IElement CreateParallelLinesLegend()
    {
        var carrier = new Carrier();
        var first = GraphicStructure.Segment(carrier, 0, 8);
        var secondCarrier = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(carrier, 3, 3)));
        var alignment = GraphicStructure.Pin(
            GraphicStructure.Point(carrier, 8),
            GraphicStructure.Point(secondCarrier, 8));
        var second = GraphicStructure.Segment(secondCarrier, 0, 8);
        return Chain([first, alignment, second]);
    }

    private static IElement CreateCircleOffsetLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var inner = new Carrier(UnitShape.CircularFrom(origin));
        var outer = new Carrier(UnitShape.CircularFrom(origin));
        return Chain([
            GraphicStructure.Point(inner, 8),
            GraphicStructure.Point(outer, 12),
            GraphicStructure.Segment(inner, 4, 12),
            GraphicStructure.Segment(outer, 4, 12)
        ]);
    }

    private static IElement CreateDirectedPathLegend()
    {
        var carrier = new Carrier();
        return Chain([
            GraphicStructure.DirectedSegment(carrier, 0, 5),
            GraphicStructure.DirectedSegment(carrier, 5, 10)
        ]);
    }

    private static IElement CreateAreaLegend()
    {
        var carrier = new Carrier();
        return Chain([
            GraphicStructure.Segment(carrier, 0, 6),
            GraphicStructure.Segment(carrier, 6, 10),
            GraphicStructure.Segment(carrier, 10, 0)
        ]);
    }

    private static IElement CreateAntiAreaLegend() => CreateAreaLegend();

    private static IElement CreateBranchLegend()
    {
        var carrier = new Carrier();
        return Chain([
            GraphicStructure.Segment(carrier, 0, 6),
            GraphicStructure.Segment(carrier, 6, 10),
            GraphicStructure.Segment(carrier, 6, 2)
        ]);
    }

    private static IElement CreateMergeLegend() => CreateBranchLegend();

    private static IElement Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return Carrier.Unknown;
        }

        var structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure;
    }

    private static IReadOnlyDictionary<ulong, int> IndexElements(IReadOnlyList<IElement> elements)
    {
        var indexes = new Dictionary<ulong, int>();
        for (var index = 0; index < elements.Count; index++)
        {
            if (elements[index] is Relation relation)
            {
                indexes[relation.Id] = index;
            }
        }

        return indexes;
    }
}

internal sealed record VisualizerLegendEntry(
    string Label,
    IElement Structure,
    int ProjectionSeed);

internal sealed record VisualizerLegendDisplay(
    string Label,
    IElement Structure,
    int ProjectionSeed);

internal sealed record VisualizerLegendPage(
    string Label,
    IReadOnlyList<VisualizerLegendEntry> Entries);
