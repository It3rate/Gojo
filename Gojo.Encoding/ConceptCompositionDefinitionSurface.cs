using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Tiny composition samples for nouns that become useful after several earlier
/// surfaces participate together. These are not exhaustive permutations; they
/// are common crystallizations such as margin, ring, annulus, and clearance.
/// </summary>
public static class ConceptCompositionDefinitionSurface
{
    public static Relation CreateNounCompositionLedger(Carrier root)
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
        var segmentOutlet = Add("segment-normal-outlet", segmentOutletBody, [segment]);
        var segmentFacing = Add("segment-margin-facing", Address("Face", Atom("segment-source"), Atom("left")), [segment]);
        var marginDistance = Add("margin-distance", Atom("+d"));
        var marginResolution = Add("margin-resolution", Address("Res", Atom("normal-distance")));
        var marginOffset = Add(
            "margin-offset",
            Address(
                "Off",
                Atom("segment-source"),
                Atom("segment-normal-outlet"),
                Atom("segment-margin-facing"),
                Atom("margin-distance"),
                Atom("margin-resolution")),
            [segment, segmentOutlet, segmentFacing, marginDistance, marginResolution]);
        var marginBoundary = Add("margin-terminal-boundary", Address("Bd1", Atom("margin-offset")), [marginOffset]);
        var marginBand = Add(
            "margin-band",
            Address("Band", Atom("segment-source-boundary"), Atom("margin-terminal-boundary")),
            [segmentBoundary, marginBoundary]);
        var marginSourceLane = Add("margin-source-lane", Address("Dir", Atom("segment-source-boundary"), Atom("+f")), [segmentBoundary]);
        var marginTerminalLane = Add("margin-terminal-lane", Address("Dir", Atom("margin-terminal-boundary"), Atom("+f")), [marginBoundary]);
        var marginLaneEvidence = Add(
            "margin-lane-evidence",
            Relation.OrderedGroup(new Carrier(), marginSourceLane, marginTerminalLane),
            [marginSourceLane, marginTerminalLane]);
        var marginArea = Add(
            "margin-area",
            Address("Area+", Atom("margin-band"), Atom("margin-lane-evidence")),
            [marginBand, marginLaneEvidence]);
        Add(
            "margin",
            ConceptDefinitionSurface.Reference(new Carrier(), marginArea, marginBand),
            [marginArea, marginBand]);

        var circleCarrier = new Carrier();
        var circleCenter = Add("circle-center", Relation.Point(circleCarrier, 0));
        var circleRadius = Add("circle-radius", Atom("+r"));
        var circle = Add("circle-source", Address("Circle", Atom("circle-center"), Atom("circle-radius")), [circleCenter, circleRadius]);
        var circumference = Add("circumference-boundary", Address("BdSelf", Atom("circle-source")), [circle]);
        var circleNormal = Add("circle-normal-outlet", Address("O", Atom("circumference-boundary")), [circumference]);
        var exteriorFacing = Add("circle-exterior-facing", Address("Face", Atom("circumference-boundary"), Atom("exterior")), [circumference]);
        var interiorFacing = Add("circle-interior-facing", Address("Face", Atom("circumference-boundary"), Atom("interior")), [circumference]);
        var clockwise = Add("clockwise-orientation", Address("Orient", Atom("circumference-boundary"), Atom("cw")), [circumference]);
        var ringDistance = Add("ring-distance", Atom("+d"));
        var ringResolution = Add("ring-resolution", Address("Res", Atom("radial-distance")));
        var ringOffset = Add(
            "ring-offset",
            Address(
                "Off",
                Atom("circumference-boundary"),
                Atom("circle-normal-outlet"),
                Atom("circle-exterior-facing"),
                Atom("ring-distance"),
                Atom("ring-resolution")),
            [circumference, circleNormal, exteriorFacing, ringDistance, ringResolution]);
        var outerRingBoundary = Add(
            "outer-ring-boundary",
            Address("BdSelf", Atom("ring-offset")),
            [ringOffset, clockwise]);
        var ringBand = Add(
            "ring-band",
            Address("Band", Atom("circumference-boundary"), Atom("outer-ring-boundary")),
            [circumference, outerRingBoundary]);
        var ringInnerLane = Add("ring-inner-lane", Address("Dir", Atom("circumference-boundary"), Atom("+f")), [circumference]);
        var ringOuterLane = Add("ring-outer-lane", Address("Dir", Atom("outer-ring-boundary"), Atom("+f")), [outerRingBoundary]);
        var ringLaneEvidence = Add(
            "ring-lane-evidence",
            Relation.OrderedGroup(new Carrier(), ringInnerLane, ringOuterLane),
            [ringInnerLane, ringOuterLane]);
        var ringArea = Add("ring-area", Address("Area+", Atom("ring-band"), Atom("ring-lane-evidence")), [ringBand, ringLaneEvidence]);
        var holeLane = Add("circle-hole-lane", Address("Dir", Atom("circumference-boundary"), Atom("+i")), [circumference]);
        var holeEvidence = Add("circle-hole-evidence", Relation.OrderedGroup(new Carrier(), holeLane), [holeLane]);
        var circleHole = Add(
            "circle-hole",
            Address("Area-", Atom("circumference-boundary"), Atom("circle-hole-evidence")),
            [circumference, holeEvidence]);
        var annulus = Add(
            "annulus-support",
            Address("Support", Atom("ring-area"), Atom("circle-hole")),
            [ringArea, circleHole],
            Text("inner boundary participates as a hole, not an eraser"));
        Add("ring", ConceptDefinitionSurface.Reference(new Carrier(), annulus, ringBand), [annulus, ringBand]);

        var clearanceRule = Add(
            "clearance-rule",
            Address(
                "Clearance",
                Atom("segment-source"),
                Atom("margin-terminal-boundary"),
                Atom("margin-distance"),
                Atom("margin-resolution")),
            [segment, marginBoundary, marginDistance, marginResolution],
            Text("clearance is a constraint read over an offset boundary"));
        var keepout = Add(
            "clearance-keepout",
            Address("Area-", Atom("margin-band"), Atom("clearance-rule")),
            [marginBand, clearanceRule],
            Text("keepout anti-area is a rule surface, not a visual deletion"));
        Add("clearance", ConceptDefinitionSurface.Reference(new Carrier(), clearanceRule, keepout), [clearanceRule, keepout]);

        _ = interiorFacing;
        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
