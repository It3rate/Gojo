using Gojo.Core;
using GojoCAD;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerSample(
    IElement Structure,
    int ProjectionSeed)
{
    public VisualizerSample Resample() => this with { ProjectionSeed = Random.Shared.Next() };

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
            GraphicStructure.Point(rimAxis, rimArrowMid)
        };

        var structure = Chain(elements);

        return new VisualizerSample(structure, Random.Shared.Next());
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
}
