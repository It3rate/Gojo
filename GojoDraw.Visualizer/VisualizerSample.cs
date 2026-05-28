using Gojo.Core;
using GojoCAD;

namespace GojoDraw.Visualizer;

internal sealed record VisualizerSample(
    IElement Structure,
    int ProjectionSeed)
{
    public static VisualizerSample Create()
    {
        var drawing = new Carrier();
        var hubSite = GraphicStructure.Site(0);
        var radiusPartitionSite = GraphicStructure.Site(5);
        var rimSite = GraphicStructure.Site(10);
        var orbitSite = GraphicStructure.Site(20);
        var lowerOrbitSite = GraphicStructure.Site(30);
        var arrowStartSite = GraphicStructure.Site(12);
        var arrowEndSite = GraphicStructure.Site(18);

        var hub = GraphicStructure.Point(drawing, hubSite);
        var radialFlow = GraphicStructure.RadialFlow(hub);
        var radiusSegment = GraphicStructure.Segment(drawing, hubSite, rimSite);
        var radiusPartition = GraphicStructure.Point(drawing, radiusPartitionSite);

        var circumference = new Carrier(UnitShape.CircularFrom(hub));
        var rimMarker = GraphicStructure.Point(circumference, rimSite);
        var circumferenceChord = GraphicStructure.Segment(circumference, rimSite, orbitSite);
        var lowerOrbitPartition = GraphicStructure.Point(circumference, lowerOrbitSite);
        var rimAttachment = new Relation(GraphicStructure.Point(drawing, rimSite), rimMarker);

        var arrowAxisSeed = GraphicStructure.Segment(drawing, hubSite, hubSite);
        var arrowAxis = new Carrier(UnitShape.LinearFrom(arrowAxisSeed));
        var arrowOriginAttachment = new Relation(GraphicStructure.Point(drawing, hubSite), GraphicStructure.Point(arrowAxis, hubSite));
        var directedSegment = GraphicStructure.DirectedSegment(arrowAxis, arrowStartSite, arrowEndSite);

        var contourAttachment = new Relation(
            radialFlow,
            new Relation(
                rimAttachment,
                new Relation(
                    circumferenceChord,
                    new Relation(lowerOrbitPartition, new Relation(arrowOriginAttachment, directedSegment)))));

        var structure = new Relation(
            hub,
            new Relation(radiusSegment, new Relation(radiusPartition, contourAttachment)));

        return new VisualizerSample(structure, Random.Shared.Next());
    }
}
