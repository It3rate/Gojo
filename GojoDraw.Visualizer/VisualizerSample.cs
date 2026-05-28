using Gojo.Core;
using GojoCAD;
using GojoDraw;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerSample(
    Relation Dot,
    Relation RadialDot,
    Relation RadialFlow,
    Relation Segment,
    SiteSkiaProjection Projection)
{
    public static VisualizerSample Create()
    {
        var drawing = new Carrier();
        var dotSite = GraphicStructure.Site(0);
        var radialSite = GraphicStructure.Site(1);
        var segmentStart = GraphicStructure.Site(2);
        var segmentEnd = GraphicStructure.Site(3);

        var dot = GraphicStructure.Point(drawing, dotSite);
        var radialDot = GraphicStructure.Point(drawing, radialSite);
        var radialFlow = GraphicStructure.RadialFlow(radialDot);
        var segment = GraphicStructure.Segment(drawing, segmentStart, segmentEnd);

        var projection = new SiteSkiaProjection()
            .Map(dotSite, 150, 245)
            .Map(radialSite, 400, 245)
            .Map(segmentStart, 560, 320)
            .Map(segmentEnd, 710, 170);

        return new VisualizerSample(dot, radialDot, radialFlow, segment, projection);
    }
}
