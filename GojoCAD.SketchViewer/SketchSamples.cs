using Gojo.Core;
using GojoCAD.Structures;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static class SketchSamples
{
    public static IReadOnlyList<SketchScene> CreateInitialScenes()
    {
        var seed = Random.Shared.Next();
        return
        [
            CreateSharedFrameContinuousLine(seed),
            CreateCarrierJoinedLine(seed + 17),
            CreateDiscoveredCompositeLine(seed + 31),
            CreateResurfacedCurve(seed + 47)
        ];
    }

    private static SketchScene CreateSharedFrameContinuousLine(int seed)
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var points = new[]
        {
            Point(frame, x, y, -46, -16),
            Point(frame, x, y, -26, 10),
            Point(frame, x, y, -4, 2),
            Point(frame, x, y, 18, 26),
            Point(frame, x, y, 46, 6)
        };
        var segments = new IElement[]
        {
            GraphicStructure.FrameSegment(points[0], points[1]),
            GraphicStructure.FrameSegment(points[1], points[2]),
            GraphicStructure.FrameSegment(points[2], points[3]),
            GraphicStructure.FrameSegment(points[3], points[4])
        };

        return new SketchScene(
            "Shared Frame Continuous Line",
            "Four connected frame segments share endpoint point structure.",
            Chain(segments),
            seed,
            new SKRect(-58, -34, 58, 34));
    }

    private static SketchScene CreateCarrierJoinedLine(int seed)
    {
        var axis = new Carrier();
        var a = GraphicStructure.Site(0);
        var b = GraphicStructure.Site(16);
        var c = GraphicStructure.Site(28);
        var d = GraphicStructure.Site(45);
        var first = GraphicStructure.Segment(axis, a, b);
        var second = GraphicStructure.Segment(axis, b, c);
        var third = GraphicStructure.Segment(axis, c, d);
        var branchCarrier = GraphicStructure.LinearCarrierAt(axis, c);
        var branch = GraphicStructure.Segment(branchCarrier, 0, 17);
        var branchWitness = GraphicStructure.Pin(
            GraphicStructure.Point(axis, c),
            GraphicStructure.Point(branchCarrier, 0));

        return new SketchScene(
            "Carrier Chain With Branch Seed",
            "Continuous carrier spans share TickRef endpoints; a linear branch is pinned at one join.",
            Chain(first, second, third, branchWitness, branch),
            seed,
            new SKRect(-4, -28, 52, 20));
    }

    private static SketchScene CreateDiscoveredCompositeLine(int seed)
    {
        var start = GraphicConceptShape.Port(new Carrier());
        var middle = GraphicConceptShape.Port(new Carrier());
        var end = GraphicConceptShape.Port(new Carrier());
        var firstConcept = GraphicConceptShape.Section(new Carrier(), start, GraphicConceptShape.TensionSlot(), middle);
        var secondConcept = GraphicConceptShape.Section(new Carrier(), middle, GraphicConceptShape.TensionSlot(), end);
        var discoveredSource = GraphicConceptShape.Frame(new Carrier(), firstConcept, secondConcept);
        GraphicConceptShape.TryDiscoverCompositeShape(discoveredSource, out var discovered);

        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var p0 = Point(frame, x, y, -36, 0);
        var p1 = Point(frame, x, y, 0, 0);
        var p2 = Point(frame, x, y, 38, 0);
        var visual = Chain(
            GraphicStructure.FrameSegment(p0, p1),
            GraphicStructure.FrameSegment(p1, p2),
            discovered);

        return new SketchScene(
            "Discovered Two-Port Composite",
            "Raw section concepts discover a composite while concrete frame spans provide the sketch pixels.",
            visual,
            seed,
            new SKRect(-45, -18, 45, 18));
    }

    private static SketchScene CreateResurfacedCurve(int seed)
    {
        var start = GraphicConceptShape.Port(new Carrier());
        var middle = GraphicConceptShape.Port(new Carrier());
        var end = GraphicConceptShape.Port(new Carrier());
        var first = GraphicConceptShape.Section(new Carrier(), start, GraphicConceptShape.TensionSlot(), middle);
        var second = GraphicConceptShape.Section(new Carrier(), middle, GraphicConceptShape.TensionSlot(), end);
        var joined = GraphicConceptShape.Join(new Carrier(), first, second);
        var refined = GraphicConceptShape.RefineCompositeSection(
            new Carrier(),
            joined,
            GraphicConceptShape.BezierEvidenceSurface(new Carrier()));

        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var p0 = Point(frame, x, y, -44, -16);
        var p1 = Point(frame, x, y, -6, 4);
        var p2 = Point(frame, x, y, 42, -10);
        var control = Point(frame, x, y, 10, 34);
        var lower = Chain(
            GraphicStructure.FrameSegment(p0, p1),
            GraphicStructure.FrameSegment(p1, p2));
        var curve = GraphicStructure.FrameQuadraticCurve(p0, control, p2);

        return new SketchScene(
            "Composite Resurfaced As Curve",
            "Two lower straight spans remain, while a higher rung can expose one curved section.",
            Chain(lower, curve, refined),
            seed,
            new SKRect(-56, -28, 56, 42));
    }

    private static Relation Point(Carrier frame, Carrier x, Carrier y, long xTicks, long yTicks) =>
        GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            return new Relation(Carrier.Unknown, Carrier.Unknown);
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }
}
