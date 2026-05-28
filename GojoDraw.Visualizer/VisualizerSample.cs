using Gojo.Core;
using GojoCAD;
using GojoDraw;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerSample(
    IElement Structure,
    SiteSkiaProjection Projection)
{
    public static VisualizerSample Create()
    {
        var drawing = new Carrier();
        var hubSite = GraphicStructure.Site(0);
        var rimSite = GraphicStructure.Site(1);
        var orbitSite = GraphicStructure.Site(2);
        var arrowEndSite = GraphicStructure.Site(3);

        var hub = GraphicStructure.Point(drawing, hubSite);
        var radialFlow = GraphicStructure.RadialFlow(hub);
        var radiusSegment = GraphicStructure.Segment(drawing, hubSite, rimSite);
        var directedSegment = GraphicStructure.DirectedSegment(drawing, hubSite, arrowEndSite);

        var circumference = new Carrier(UnitShape.CircularFrom(hub));
        var rimMarker = GraphicStructure.Point(circumference, rimSite);
        var circumferenceChord = GraphicStructure.Segment(circumference, rimSite, orbitSite);
        var rimAttachment = new Relation(GraphicStructure.Point(drawing, rimSite), rimMarker);
        var contourAttachment = new Relation(radialFlow, new Relation(rimAttachment, circumferenceChord));

        var structure = new Relation(
            hub,
            new Relation(radiusSegment, new Relation(directedSegment, contourAttachment)));

        var projection = new SiteSkiaProjection()
            .Map(hubSite, 320, 250)
            .Map(rimSite, 500, 250)
            .Map(orbitSite, 450, 120)
            .Map(arrowEndSite, 215, 145);

        return new VisualizerSample(structure, projection);
    }
}
