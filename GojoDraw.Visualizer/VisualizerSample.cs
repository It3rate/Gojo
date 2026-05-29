using Gojo.Core;
using GojoCAD;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerSample(
    IElement Structure,
    int ProjectionSeed,
    IReadOnlyDictionary<ulong, int> ElementIndexes)
{
    public VisualizerSample Resample() => this with { ProjectionSeed = Random.Shared.Next() };

    public int? SourceIndexOf(IElement? source) =>
        source is Relation relation && ElementIndexes.TryGetValue(relation.Id, out var index)
            ? index
            : null;

    public static IReadOnlyList<VisualizerLegendEntry> CreateLegend() =>
    [
        LegendEntry("point", CreatePointLegend(), 101),
        LegendEntry("radial", CreateRadialLegend(), 102),
        LegendEntry("segment", CreateSegmentLegend(), 103),
        LegendEntry("partition", CreateSegmentPartitionLegend(), 104),
        LegendEntry("directed", CreateDirectedLegend(), 105),
        LegendEntry("line seed", CreateLineSeedLegend(), 106),
        LegendEntry("sheet seed", CreateSheetSeedLegend(), 107),
        LegendEntry("radius", CreateRadiusAttachmentLegend(), 108),
        LegendEntry("circle", CreateCircumferenceLegend(), 109),
        LegendEntry("arc/span", CreateCircumferenceSpanLegend(), 110)
    ];

    public static VisualizerSample Create()
    {
        var drawing = new Carrier();
        var hubSite = GraphicStructure.Site(0);
        var nearSite = GraphicStructure.Site(3);
        var midSite = GraphicStructure.Site(6);
        var radiusSite = GraphicStructure.Site(8);
        var highSite = GraphicStructure.Site(9);
        var sheetEndSite = GraphicStructure.Site(10);
        var farSite = GraphicStructure.Site(12);
        var outerSite = GraphicStructure.Site(16);
        var tailSite = GraphicStructure.Site(20);

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

        var circumference = new Carrier(UnitShape.CircularFrom(hub));
        var firstRadiusAttachment = new Relation(
            GraphicStructure.Point(drawing, radiusSite),
            GraphicStructure.Point(circumference, contourA));
        var secondRadiusAttachment = new Relation(
            GraphicStructure.Point(drawing, midSite),
            GraphicStructure.Point(circumference, midSite));

        var arrowAxis = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(drawing, hubSite, hubSite)));
        var arrowOriginAttachment = new Relation(
            GraphicStructure.Point(drawing, hubSite),
            GraphicStructure.Point(arrowAxis, axisOrigin));

        var rimAxis = new Carrier(UnitShape.LinearFrom(GraphicStructure.Segment(drawing, radiusSite, radiusSite)));
        var rimAxisOriginAttachment = new Relation(
            GraphicStructure.Point(drawing, radiusSite),
            GraphicStructure.Point(rimAxis, axisOrigin));
        var arrowAxisArcAttachment = new Relation(
            GraphicStructure.Point(arrowAxis, arrowFar),
            GraphicStructure.Point(circumference, contourB));

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
            arrowAxisArcAttachment
        };

        var structure = Chain(elements);

        return new VisualizerSample(structure, Random.Shared.Next(), IndexElements(elements));
    }

    private static VisualizerLegendEntry LegendEntry(string label, IElement structure, int seed) =>
        new(label, structure, seed);

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

    private static IElement CreateSegmentLegend()
    {
        var carrier = new Carrier();
        return GraphicStructure.Segment(carrier, 0, 8);
    }

    private static IElement CreateSegmentPartitionLegend()
    {
        var carrier = new Carrier();
        var segment = GraphicStructure.Segment(carrier, 0, 8);
        var partition = GraphicStructure.Point(carrier, 4);
        return Chain([segment, partition]);
    }

    private static IElement CreateDirectedLegend()
    {
        var carrier = new Carrier();
        return GraphicStructure.DirectedSegment(carrier, 2, 8);
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

    private static IElement CreateCircumferenceSpanLegend()
    {
        var carrier = new Carrier();
        var origin = GraphicStructure.Point(carrier, 0);
        var circle = new Carrier(UnitShape.CircularFrom(origin));
        var span = GraphicStructure.Segment(circle, 8, 18);
        var partition = GraphicStructure.Point(circle, 13);
        return Chain([span, partition]);
    }

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
