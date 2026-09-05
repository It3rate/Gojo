using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Tiny boundary/area definition samples built as Gojo relation structure.
/// The methods are scaffolding for inspection: the meaning is carried by the
/// returned definition ledger, not by these method names.
/// </summary>
public static class BoundaryAreaDefinitionSurface
{
    public static Relation CreateBandVariantLedger(Carrier root)
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

        var carrier = new Carrier();
        var spanBody = Relation.Segment(carrier, 0, 1);
        var orthogonalOutletBody = Relation.OrthogonalOutlet(spanBody);

        var span = Add("span", spanBody);
        var sourceBoundary = Add(
            "source-boundary",
            Address("Bd0", Atom("span")),
            [span]);
        var orthogonalOutlet = Add(
            "orthogonal-outlet",
            orthogonalOutletBody,
            [span]);

        var positiveExtent = Add("positive-extent", Atom("+d"));
        var positiveOffset = Add(
            "positive-offset-read",
            Address("Off", Atom("span"), Atom("orthogonal-outlet"), Atom("positive-extent")),
            [span, orthogonalOutlet, positiveExtent]);
        var positiveTerminalBoundary = Add(
            "positive-terminal-boundary",
            Address("Bd1", Atom("positive-offset-read")),
            [positiveOffset]);
        var positiveBoundaryPair = Add(
            "positive-boundary-pair",
            Relation.OrderedGroup(new Carrier(), sourceBoundary, positiveTerminalBoundary),
            [sourceBoundary, positiveTerminalBoundary]);
        var sourcePositiveLane = Add(
            "source-positive-lane",
            Address("Dir", Atom("source-boundary"), Atom("+f")),
            [sourceBoundary]);
        var terminalPositiveLane = Add(
            "terminal-positive-lane",
            Address("Dir", Atom("positive-terminal-boundary"), Atom("+f")),
            [positiveTerminalBoundary]);
        var positiveLaneEvidence = Add(
            "positive-lane-evidence",
            Relation.OrderedGroup(new Carrier(), sourcePositiveLane, terminalPositiveLane),
            [sourcePositiveLane, terminalPositiveLane]);
        var positiveArea = Add(
            "positive-area",
            Address("Area+", Atom("positive-boundary-pair"), Atom("positive-lane-evidence")),
            [positiveBoundaryPair, positiveLaneEvidence]);
        Add(
            "area-support",
            ConceptDefinitionSurface.Reference(new Carrier(), positiveArea, positiveBoundaryPair),
            [positiveArea, positiveBoundaryPair]);

        var inverseExtent = Add("inverse-extent", Atom("-d"));
        var inverseOffset = Add(
            "inverse-offset-read",
            Address("Off", Atom("span"), Atom("orthogonal-outlet"), Atom("inverse-extent")),
            [span, orthogonalOutlet, inverseExtent]);
        var inverseTerminalBoundary = Add(
            "inverse-terminal-boundary",
            Address("Bd1", Atom("inverse-offset-read")),
            [inverseOffset]);
        var inverseBoundaryPair = Add(
            "inverse-boundary-pair",
            Relation.OrderedGroup(new Carrier(), sourceBoundary, inverseTerminalBoundary),
            [sourceBoundary, inverseTerminalBoundary]);
        var sourceInverseLane = Add(
            "source-inverse-lane",
            Address("Dir", Atom("source-boundary"), Atom("+i")),
            [sourceBoundary]);
        var terminalInverseLane = Add(
            "terminal-inverse-lane",
            Address("Dir", Atom("inverse-terminal-boundary"), Atom("+i")),
            [inverseTerminalBoundary]);
        var inverseLaneEvidence = Add(
            "inverse-lane-evidence",
            Relation.OrderedGroup(new Carrier(), sourceInverseLane, terminalInverseLane),
            [sourceInverseLane, terminalInverseLane]);
        var antiArea = Add(
            "anti-area",
            Address("Area-", Atom("inverse-boundary-pair"), Atom("inverse-lane-evidence")),
            [inverseBoundaryPair, inverseLaneEvidence]);
        Add(
            "anti-area-support",
            ConceptDefinitionSurface.Reference(new Carrier(), antiArea, inverseBoundaryPair),
            [antiArea, inverseBoundaryPair]);

        var terminalNegativeInverseLane = Add(
            "terminal-negative-inverse-lane",
            Address("Dir", Atom("positive-terminal-boundary"), Atom("-i")),
            [positiveTerminalBoundary]);
        var mixedLaneEvidence = Add(
            "mixed-lane-evidence",
            Relation.OrderedGroup(new Carrier(), sourcePositiveLane, terminalNegativeInverseLane),
            [sourcePositiveLane, terminalNegativeInverseLane]);
        var unsettledArea = Add(
            "unsettled-area-read",
            Address("Area?", Atom("positive-boundary-pair"), Atom("mixed-lane-evidence")),
            [positiveBoundaryPair, mixedLaneEvidence],
            Text("mixed lane read does not settle as area"));
        Add(
            "mixed-lane-residue",
            Address("Residue", Atom("unsettled-area-read")),
            [unsettledArea],
            Text("mixed lane leaves directional residue, not geometric diagonal area"));

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
