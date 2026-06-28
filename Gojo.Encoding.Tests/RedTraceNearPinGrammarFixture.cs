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
    public readonly record struct RedTraceNearPinGrammarInput(
        string RedToken,
        string TraceToken,
        string PositionToken,
        string PinToken,
        long LowResolutionTicks,
        long SourceTicksPerReadTick)
    {
        public static RedTraceNearPinGrammarInput Default =>
            new("red", "trace", "near", "pin", 1, 10);
    }

    public static Relation CreateLedger(Carrier root) =>
        WriteLedger(root, RedTraceNearPinGrammarInput.Default);

    public static Relation WriteLedger(Carrier root, RedTraceNearPinGrammarInput input)
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

        var tokenRed = Add("token-red", Address("Token", Atom(input.RedToken), Atom("position-0")));
        var tokenTrace = Add("token-trace", Address("Token", Atom(input.TraceToken), Atom("position-1")));
        var tokenNear = Add("token-near", Address("Token", Atom(input.PositionToken), Atom("position-2")));
        var tokenPin = Add("token-pin", Address("Token", Atom(input.PinToken), Atom("position-3")));
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
        var lowResolutionGround = new Carrier(UnitShape.ResolutionOf(clearanceGround.UnitShape, input.SourceTicksPerReadTick));
        var lowPositionBody = Relation.Point(lowResolutionGround, input.LowResolutionTicks);
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

    public static Relation CreateCadContextMergedLedger(Carrier root) =>
        CreateCadContextMergedLedger(root, CreateLedger(new Carrier()));

    public static Relation CreateCadContextMergedLedger(Carrier root, Relation baseLedger)
    {
        if (!ConceptDefinitionSurface.TryReadLedger(baseLedger, out var baseDefinitions) ||
            !ConceptDefinitionDiscoverySurface.TryReadLedger(baseLedger, out var graph))
        {
            return ConceptDefinitionSurface.Ledger(root, []);
        }

        var definitions = baseDefinitions.ToList();

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

        Relation Need(string name) =>
            graph.TryFindByName(name, out var node)
                ? node.Definition
                : ConceptDefinitionSurface.Create(
                    new Carrier(),
                    $"missing-{name}",
                    Address("Missing", Atom(name)),
                    residueOrTension: Text("context merge could not find required base evidence"));

        IElement NeedBody(string name) =>
            graph.TryFindByName(name, out var node)
                ? node.Read.Body
                : Need(name);

        var tokenRed = Need("token-red");
        var tokenNear = Need("token-near");
        var redQuality = Need("red-quality");
        var traceObject = Need("trace-object");
        var pinObject = Need("pin-object");
        var lowPosition = Need("trace-pin-low-resolution-position");
        var lowPositionBody = NeedBody("trace-pin-low-resolution-position");
        var tracePinPositionRead = Need("trace-pin-position-read");
        var traceNearPinPhrase = Need("trace-near-pin-phrase");
        var selectedRead = Need("red-trace-near-pin-selected-read");
        var interpretation = Need("red-trace-near-pin-interpretation");

        var cadLayerContext = Add(
            "cad-layer-context",
            Address("ContextFrame", Atom("cad-board"), Atom("copper-layer")),
            [redQuality],
            Text("context narrows red toward copper layer color evidence"));
        var clearanceContext = Add(
            "board-clearance-context",
            Address("ContextFrame", Atom("board-clearance"), Atom("trace-pin-gap")),
            [traceObject, pinObject],
            Text("context narrows the position read toward board clearance"));
        var copperLayerColorBasis = Add(
            "copper-layer-color-basis",
            Address("Basis", Atom("copper-layer-color"), Atom("cad-layer-context")),
            [cadLayerContext, redQuality],
            Text("color basis supplied by CAD layer context"));
        var redCopperLayerRead = Add(
            "red-copper-layer-read",
            ReadWitness.Create(
                new Carrier(),
                redQuality,
                copperLayerColorBasis,
                Address("SettledQuality", Atom("red-quality"), Atom("copper-layer-color")),
                Text("red basis settled to copper layer color at this CAD resolution"),
                tokenRed),
            [tokenRed, redQuality, copperLayerColorBasis],
            Text("red residue is lowered by CAD layer context"));
        var clearanceBasis = Add(
            "trace-pin-clearance-basis",
            Address("Basis", Atom("board-clearance"), Atom("pin-ground")),
            [traceObject, pinObject, clearanceContext],
            Text("clearance basis supplied by board context"));
        var tracePinClearanceRead = Add(
            "trace-pin-clearance-read",
            ReadWitness.Create(
                new Carrier(),
                tracePinPositionRead,
                clearanceBasis,
                lowPositionBody,
                Text("clearance basis supplied; exact metric distance is still below this read resolution"),
                tokenNear),
            [tracePinPositionRead, tokenNear, clearanceBasis, lowPosition],
            Text("position residue is lowered to a board-clearance read"));
        var contextSettlement = Add(
            "red-trace-near-pin-context-settlement",
            Address(
                "ContextSettlement",
                Atom("red-trace-near-pin-selected-read"),
                Atom("red-copper-layer-read"),
                Atom("trace-pin-clearance-read")),
            [selectedRead, redCopperLayerRead, tracePinClearanceRead, traceNearPinPhrase],
            Text("context supports selected read while alternate attachment remains trace residue"));
        Add(
            "red-trace-near-pin-merged-interpretation",
            Address(
                "MergedInterpretation",
                Atom("red-trace-near-pin-interpretation"),
                Atom("cad-layer-context"),
                Atom("board-clearance-context"),
                Atom("red-trace-near-pin-context-settlement")),
            [interpretation, cadLayerContext, clearanceContext, contextSettlement, traceNearPinPhrase],
            Text("merge lowers red and clearance residue without deleting alternate attachment"));

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    public static Relation CreatePerspectiveDistanceLedger(Carrier root)
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

        var boardGround = new Carrier();
        var relativeGround = new Carrier(UnitShape.ResolutionOf(boardGround.UnitShape, 10));
        var element = Add("same-element", Address("Object", Atom("component"), Atom("board-site")));
        var boardPosition = Add(
            "same-element-board-position",
            Relation.Point(boardGround, 50),
            [element],
            Text("same underlying board position"));
        var localPerspective = Add(
            "local-read-perspective",
            Address("Perspective", Atom("reader-at-48"), Atom("board-ground")),
            [boardPosition],
            Text("read origin is at board tick 48"));
        var remotePerspective = Add(
            "remote-read-perspective",
            Address("Perspective", Atom("reader-at-0"), Atom("board-ground")),
            [boardPosition],
            Text("read origin is at board tick 0"));
        var localRelativePosition = Add(
            "same-element-local-relative-position",
            Relation.Point(relativeGround, 1),
            [element, boardPosition, localPerspective],
            Text("relative position under local perspective"));
        var remoteRelativePosition = Add(
            "same-element-remote-relative-position",
            Relation.Point(relativeGround, 8),
            [element, boardPosition, remotePerspective],
            Text("relative position under remote perspective"));

        Add(
            "same-element-local-perspective-read",
            ReadWitness.Create(
                new Carrier(),
                element,
                localPerspective,
                Relation.Point(relativeGround, 1),
                Text("distance qualifier is produced by the read, not stored on the element"),
                localPerspective),
            [element, boardPosition, localPerspective, localRelativePosition]);
        Add(
            "same-element-remote-perspective-read",
            ReadWitness.Create(
                new Carrier(),
                element,
                remotePerspective,
                Relation.Point(relativeGround, 8),
                Text("distance qualifier is produced by the read, not stored on the element"),
                remotePerspective),
            [element, boardPosition, remotePerspective, remoteRelativePosition]);

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
