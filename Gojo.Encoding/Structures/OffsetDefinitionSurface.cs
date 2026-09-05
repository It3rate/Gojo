using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Tiny offset definition samples built as Gojo relation structure. Offset
/// meaning is carried by the returned ledger: source support, outlet or normal
/// evidence, facing, orientation, extent, resolution, terminal boundary, and
/// any residue that prevents the read from settling cleanly.
/// </summary>
public static class OffsetDefinitionSurface
{
    public static Relation CreateOffsetVariantLedger(Carrier root)
    {
        var definitions = new List<Relation>();

        Relation Add(
            string name,
            IElement body,
            IReadOnlyList<IElement>? dependencies = null,
            IElement? residueOrTension = null)
        {
            var definition = ConceptDefinitionSurface.Create(
                new Carrier(),
                name,
                body,
                dependencies,
                residueOrTension: residueOrTension);
            definitions.Add(definition);
            return definition;
        }

        var segmentCarrier = new Carrier();
        var segmentBody = Relation.Segment(segmentCarrier, 0, 1);
        var segmentOutletBody = Relation.OrthogonalOutlet(segmentBody);
        var segment = Add("segment-source", segmentBody);
        var segmentBoundary = Add("segment-source-boundary", Address("Bd0", Atom("segment-source")), [segment]);
        var segmentOutlet = Add("segment-orthogonal-outlet", segmentOutletBody, [segment]);
        var segmentResolution = Add("segment-offset-resolution", Address("Res", Atom("normal-distance")));

        var leftFacing = Add("segment-left-facing", Address("Face", Atom("segment-source"), Atom("left")), [segment]);
        var rightFacing = Add("segment-right-facing", Address("Face", Atom("segment-source"), Atom("right")), [segment]);
        var leftDistance = Add("left-distance", Atom("+d"));
        var rightDistance = Add("right-distance", Atom("-d"));

        var leftOffset = Add(
            "segment-left-offset",
            Address(
                "Off",
                Atom("segment-source"),
                Atom("segment-orthogonal-outlet"),
                Atom("segment-left-facing"),
                Atom("left-distance"),
                Atom("segment-offset-resolution")),
            [segment, segmentOutlet, leftFacing, leftDistance, segmentResolution]);
        var rightOffset = Add(
            "segment-right-offset",
            Address(
                "Off",
                Atom("segment-source"),
                Atom("segment-orthogonal-outlet"),
                Atom("segment-right-facing"),
                Atom("right-distance"),
                Atom("segment-offset-resolution")),
            [segment, segmentOutlet, rightFacing, rightDistance, segmentResolution]);
        var leftTerminal = Add("segment-left-terminal-boundary", Address("Bd1", Atom("segment-left-offset")), [leftOffset]);
        var rightTerminal = Add("segment-right-terminal-boundary", Address("Bd1", Atom("segment-right-offset")), [rightOffset]);
        Add("segment-left-band", Address("Band", Atom("segment-source-boundary"), Atom("segment-left-terminal-boundary")), [segmentBoundary, leftTerminal]);
        Add("segment-right-band", Address("Band", Atom("segment-source-boundary"), Atom("segment-right-terminal-boundary")), [segmentBoundary, rightTerminal]);

        var openOffset = Add(
            "open-segment-offset",
            Address("OffOpen", Atom("segment-left-offset")),
            [leftOffset],
            Text("cap and join policy not present"));
        Add(
            "offset-cap-residue",
            Address("Residue", Atom("open-segment-offset")),
            [openOffset],
            Text("open offset has no closure boundary yet"));

        var circleCarrier = new Carrier();
        var circleOrigin = Add("circle-center", Relation.Point(circleCarrier, 0));
        var circleRadius = Add("circle-radius", Atom("+r"));
        var circleSource = Add(
            "circle-source",
            Address("Circle", Atom("circle-center"), Atom("circle-radius")),
            [circleOrigin, circleRadius]);
        var circumference = Add(
            "circumference-boundary",
            Address("BdSelf", Atom("circle-source")),
            [circleSource]);
        var circleNormal = Add("circle-normal-outlet", Address("O", Atom("circumference-boundary")), [circumference]);
        var clockwise = Add("clockwise-orientation", Address("Orient", Atom("circumference-boundary"), Atom("cw")), [circumference]);
        var counterClockwise = Add("counterclockwise-orientation", Address("Orient", Atom("circumference-boundary"), Atom("ccw")), [circumference]);
        var exteriorFacing = Add("circle-exterior-facing", Address("Face", Atom("circumference-boundary"), Atom("exterior")), [circumference]);
        var interiorFacing = Add("circle-interior-facing", Address("Face", Atom("circumference-boundary"), Atom("interior")), [circumference]);
        var outwardDistance = Add("outward-distance", Atom("+d"));
        var inwardDistance = Add("inward-distance", Atom("-d"));
        var circleResolution = Add("circle-offset-resolution", Address("Res", Atom("radial-distance")));

        var exteriorCircleOffset = Add(
            "circle-exterior-offset",
            Address(
                "Off",
                Atom("circumference-boundary"),
                Atom("circle-normal-outlet"),
                Atom("circle-exterior-facing"),
                Atom("outward-distance"),
                Atom("circle-offset-resolution")),
            [circumference, circleNormal, exteriorFacing, outwardDistance, circleResolution]);
        var exteriorOffsetBoundary = Add(
            "circle-exterior-offset-boundary",
            Address("BdSelf", Atom("circle-exterior-offset")),
            [exteriorCircleOffset, clockwise]);
        Add(
            "circle-exterior-ring-band",
            Address("Band", Atom("circumference-boundary"), Atom("circle-exterior-offset-boundary")),
            [circumference, exteriorOffsetBoundary]);

        var interiorCircleOffset = Add(
            "circle-interior-offset",
            Address(
                "Off",
                Atom("circumference-boundary"),
                Atom("circle-normal-outlet"),
                Atom("circle-interior-facing"),
                Atom("inward-distance"),
                Atom("circle-offset-resolution")),
            [circumference, circleNormal, interiorFacing, inwardDistance, circleResolution]);
        var interiorOffsetBoundary = Add(
            "circle-interior-offset-boundary",
            Address("BdSelf", Atom("circle-interior-offset")),
            [interiorCircleOffset, counterClockwise]);
        Add(
            "circle-interior-ring-band",
            Address("Band", Atom("circle-interior-offset-boundary"), Atom("circumference-boundary")),
            [interiorOffsetBoundary, circumference]);

        var closedSegmentLoop = Add("closed-segment-loop", Address("Loop", Atom("segment-source")), [segment]);
        var loopOffset = Add(
            "closed-loop-offset",
            Address(
                "OffClosed",
                Atom("closed-segment-loop"),
                Atom("segment-orthogonal-outlet"),
                Atom("segment-left-facing"),
                Atom("left-distance"),
                Atom("segment-offset-resolution")),
            [closedSegmentLoop, segmentOutlet, leftFacing, leftDistance, segmentResolution]);
        Add("closed-loop-offset-boundary", Address("BdSelf", Atom("closed-loop-offset")), [loopOffset]);

        var mismatch = Add(
            "offset-mismatch",
            Address("Off?", Atom("segment-source"), Atom("circle-normal-outlet"), Atom("left-distance")),
            [segment, circleNormal, leftDistance],
            Text("source and normal evidence do not share local support"));
        Add(
            "offset-mismatch-residue",
            Address("Residue", Atom("offset-mismatch")),
            [mismatch],
            Text("offset read remains unresolved until source and outlet are locally compatible"));

        Add(
            "crystallized-offset-read",
            ConceptDefinitionSurface.Reference(new Carrier(), exteriorCircleOffset, circumference),
            [exteriorCircleOffset, circumference]);

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
