using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding.Tests;

/// <summary>
/// Tiny sentence-like grammar samples built as local definition structure. The
/// helper does not parse language; it creates inspectable token, lexical,
/// modifier, low-resolution position, interpretation, and trace surfaces that
/// can be read back without making a global grammar classifier or preposition
/// concept tree.
/// </summary>
internal static class RedTraceNearPinGrammarFixture
{
    public static Relation CreateLedger(Carrier root)
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

        var tokenRed = Add("token-red", Address("Token", Atom("red"), Atom("position-0")));
        var tokenTrace = Add("token-trace", Address("Token", Atom("trace"), Atom("position-1")));
        var tokenNear = Add("token-near", Address("Token", Atom("near"), Atom("position-2")));
        var tokenPin = Add("token-pin", Address("Token", Atom("pin"), Atom("position-3")));
        var tokenSequence = Add(
            "token-sequence",
            Address("Tokens", Atom("token-red"), Atom("token-trace"), Atom("token-near"), Atom("token-pin")),
            [tokenRed, tokenTrace, tokenNear, tokenPin]);

        var redQuality = Add(
            "red-quality",
            Address("Quality", Atom("red")),
            residueOrTension: Text("exact red basis remains open"));
        var traceObject = Add("trace-object", Address("Object", Atom("trace"), Atom("cad-route")));
        var pinObject = Add("pin-object", Address("Object", Atom("pin"), Atom("cad-site")));

        var clearanceGround = new Carrier();
        var lowResolutionGround = new Carrier(UnitShape.ResolutionOf(clearanceGround.UnitShape, 10));
        var lowPositionBody = Relation.Point(lowResolutionGround, 1);
        var lowPosition = Add(
            "trace-pin-low-resolution-position",
            lowPositionBody,
            [tokenTrace, tokenPin],
            Text("low-resolution position on the pin ground; exact distance remains missing"));
        var tracePinPositionRead = Add(
            "trace-pin-position-read",
            ReadWitness.Create(
                new Carrier(),
                tokenTrace,
                tokenPin,
                lowPositionBody,
                Text("exact trace-to-pin position remains unresolved"),
                tokenNear),
            [tokenTrace, tokenNear, tokenPin, lowPosition],
            Text("near is read from low-resolution position on the pin ground, not from a stored concept tree"));

        var redCandidate = Add(
            "red-lexical-candidate",
            Address("Lex", Atom("token-red"), Atom("red-quality")),
            [tokenRed, redQuality],
            Text("red may be color, warning, or net-class evidence until context settles it"));
        var traceCandidate = Add(
            "trace-lexical-candidate",
            Address("Lex", Atom("token-trace"), Atom("trace-object")),
            [tokenTrace, traceObject]);
        var nearCandidate = Add(
            "near-lexical-candidate",
            Address("Lex", Atom("token-near"), Atom("trace-pin-position-read")),
            [tokenNear, tracePinPositionRead],
            Text("near token points at a missing low-resolution position read"));
        var pinCandidate = Add(
            "pin-lexical-candidate",
            Address("Lex", Atom("token-pin"), Atom("pin-object")),
            [tokenPin, pinObject]);

        var redTraceModifier = Add(
            "red-trace-modifier",
            Address("Modify", Atom("red-lexical-candidate"), Atom("trace-lexical-candidate")),
            [redCandidate, traceCandidate],
            Text("red modifies trace while exact color basis remains unresolved"));
        var traceNearPinPhrase = Add(
            "trace-near-pin-phrase",
            Address(
                "PositionPhrase",
                Atom("trace-lexical-candidate"),
                Atom("near-lexical-candidate"),
                Atom("pin-lexical-candidate")),
            [traceCandidate, nearCandidate, pinCandidate, tracePinPositionRead],
            Text("near can attach to trace before modifier settlement"));
        var redTraceNearPinPhrase = Add(
            "red-trace-near-pin-phrase",
            Address(
                "PositionPhrase",
                Atom("red-trace-modifier"),
                Atom("near-lexical-candidate"),
                Atom("pin-lexical-candidate")),
            [redTraceModifier, nearCandidate, pinCandidate, tracePinPositionRead],
            Text("near can attach after red-trace phrase settlement"));
        var selectedRead = Add(
            "red-trace-near-pin-selected-read",
            Address("SelectedRead", Atom("red-trace-near-pin-phrase")),
            [redTraceNearPinPhrase],
            Text("selected coarse read keeps exact red and nearness basis unresolved"));
        var interpretation = Add(
            "red-trace-near-pin-interpretation",
            Address(
                "InterpretationBundle",
                Atom("token-sequence"),
                Atom("red-trace-near-pin-selected-read"),
                Atom("trace-near-pin-phrase")),
            [tokenSequence, selectedRead, redTraceNearPinPhrase, traceNearPinPhrase],
            Text("alternate attachment remains as residue"));
        Add(
            "red-trace-near-pin-grammar-trace",
            Address(
                "GrammarTrace",
                Atom("token-sequence"),
                Atom("red-lexical-candidate"),
                Atom("trace-lexical-candidate"),
                Atom("near-lexical-candidate"),
                Atom("pin-lexical-candidate"),
                Atom("red-trace-modifier"),
                Atom("trace-near-pin-phrase"),
                Atom("red-trace-near-pin-phrase"),
                Atom("red-trace-near-pin-selected-read")),
            [
                tokenSequence,
                redCandidate,
                traceCandidate,
                nearCandidate,
                pinCandidate,
                redTraceModifier,
                traceNearPinPhrase,
                redTraceNearPinPhrase,
                selectedRead,
                interpretation
            ],
            Text("original order and unselected read remain available"));

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
