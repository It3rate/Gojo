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
        var radiusPartitionSite = GraphicStructure.Site(5);
        var rimSite = GraphicStructure.Site(10);
        var orbitSite = GraphicStructure.Site(20);
        var lowerOrbitSite = GraphicStructure.Site(30);
        var arrowEndSite = GraphicStructure.Site(40);

        var hub = GraphicStructure.Point(drawing, hubSite);
        var radialFlow = GraphicStructure.RadialFlow(hub);
        var radiusSegment = GraphicStructure.Segment(drawing, hubSite, rimSite);
        var radiusPartition = GraphicStructure.Point(drawing, radiusPartitionSite);
        var directedSegment = GraphicStructure.DirectedSegment(drawing, hubSite, arrowEndSite);

        var circumference = new Carrier(UnitShape.CircularFrom(hub));
        var rimMarker = GraphicStructure.Point(circumference, rimSite);
        var circumferenceChord = GraphicStructure.Segment(circumference, rimSite, orbitSite);
        var lowerOrbitPartition = GraphicStructure.Point(circumference, lowerOrbitSite);
        var rimAttachment = new Relation(GraphicStructure.Point(drawing, rimSite), rimMarker);
        var contourAttachment = new Relation(
            radialFlow,
            new Relation(rimAttachment, new Relation(circumferenceChord, lowerOrbitPartition)));

        var structure = new Relation(
            hub,
            new Relation(radiusSegment, new Relation(radiusPartition, new Relation(directedSegment, contourAttachment))));

        var projection = new SiteSkiaProjection()
            .Map(hubSite, 320, 250)
            .Map(drawing, 0, 320, 250)
            .Map(radiusPartitionSite, 380, 250)
            .Map(rimSite, 440, 250)
            .Map(orbitSite, 380, 146)
            .Map(lowerOrbitSite, 380, 354)
            .Map(arrowEndSite, 225, 155);

        return new VisualizerSample(structure, projection);
    }
}
