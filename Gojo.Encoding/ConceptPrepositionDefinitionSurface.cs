using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// First preposition samples built as open role-bearing comparison structure.
/// The useful concepts here are focus reads over one parameterized scale:
/// near, far, halfway, and arrived are siblings, not isolated predicates.
/// </summary>
public static class ConceptPrepositionDefinitionSurface
{
    public static Relation CreatePrepositionLedger(Carrier root)
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

        var figureSlot = Add(
            "figure-slot",
            ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("figure")),
            residueOrTension: Text("figure role remains open until a phrase or context binds it"));
        var groundSlot = Add(
            "ground-slot",
            ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("ground")),
            residueOrTension: Text("ground role remains open until a phrase or context binds it"));
        var basisSlot = Add(
            "basis-slot",
            ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("basis")),
            residueOrTension: Text("metric or comparison basis is contextual"));
        var resolutionSlot = Add(
            "resolution-slot",
            ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("resolution")),
            residueOrTension: Text("qualification resolution is contextual"));
        var roleFrame = Add(
            "preposition-role-frame",
            Address(
                "RoleFrame",
                Atom("figure-slot"),
                Atom("ground-slot"),
                Atom("basis-slot"),
                Atom("resolution-slot")),
            [figureSlot, groundSlot, basisSlot, resolutionSlot]);

        var comparisonTemplate = Add(
            "comparison-template",
            ResolutionFacetStructureSurface.Comparison(
                new Carrier(),
                figureSlot,
                groundSlot,
                resolutionSlot,
                Text("basis and resolution remain open until binding")),
            [figureSlot, groundSlot, basisSlot, resolutionSlot],
            Text("basis and resolution remain open until binding"));
        var comparisonScale = Add(
            "comparison-scale",
            Address("Scale", Atom("comparison-template"), Atom("salient-landmarks")),
            [comparisonTemplate],
            Text("scale landmarks are salient regions, not necessarily equal partitions"));

        var beginningZone = Add("beginning-zone", Address("Zone", Atom("comparison-scale"), Atom("source-site")), [comparisonScale]);
        var farZone = Add("far-zone", Address("Zone", Atom("comparison-scale"), Atom("high-separation")), [comparisonScale]);
        var halfwayZone = Add("halfway-zone", Address("Zone", Atom("comparison-scale"), Atom("middle-salience")), [comparisonScale]);
        var nearZone = Add("near-zone", Address("Zone", Atom("comparison-scale"), Atom("low-separation")), [comparisonScale]);
        var arrivedZone = Add("arrived-zone", Address("Zone", Atom("comparison-scale"), Atom("goal-settlement")), [comparisonScale]);

        Add("beginning-focus", Address("Focus", Atom("comparison-scale"), Atom("beginning-zone")), [comparisonScale, beginningZone]);
        var farFocus = Add("far-focus", Address("Focus", Atom("comparison-scale"), Atom("far-zone")), [comparisonScale, farZone]);
        var halfwayFocus = Add("halfway-focus", Address("Focus", Atom("comparison-scale"), Atom("halfway-zone")), [comparisonScale, halfwayZone]);
        var nearFocus = Add("near-focus", Address("Focus", Atom("comparison-scale"), Atom("near-zone")), [comparisonScale, nearZone]);
        var arrivedFocus = Add("arrived-focus", Address("Focus", Atom("comparison-scale"), Atom("arrived-zone")), [comparisonScale, arrivedZone]);

        var near = Add(
            "near",
            ConceptDefinitionSurface.Reference(new Carrier(), nearFocus, roleFrame),
            [nearFocus, roleFrame],
            Text("near is a crystallized focus read, not the whole comparison scale"));
        Add(
            "far",
            ConceptDefinitionSurface.Reference(new Carrier(), farFocus, roleFrame),
            [farFocus, roleFrame]);
        Add(
            "halfway",
            ConceptDefinitionSurface.Reference(new Carrier(), halfwayFocus, roleFrame),
            [halfwayFocus, roleFrame]);
        Add(
            "arrived",
            ConceptDefinitionSurface.Reference(new Carrier(), arrivedFocus, roleFrame),
            [arrivedFocus, roleFrame]);

        var store = Add("store", Atom("store"));
        var house = Add("my-house", Atom("my-house"));
        var ordinaryBasis = Add("ordinary-distance-basis", Address("Basis", Atom("physical-or-travel-distance")));
        var ordinaryResolution = Add("ordinary-distance-resolution", Address("Res", Atom("ordinary-language-distance")));
        var storeHouseBinding = Add(
            "store-house-near-binding",
            Address(
                "BindRoles",
                Atom("preposition-role-frame"),
                Atom("store"),
                Atom("my-house"),
                Atom("ordinary-distance-basis"),
                Atom("ordinary-distance-resolution")),
            [roleFrame, store, house, ordinaryBasis, ordinaryResolution]);
        var storeHouseComparison = Add(
            "store-house-comparison",
            ResolutionFacetStructureSurface.Comparison(
                new Carrier(),
                store,
                house,
                ordinaryResolution,
                Text("place context supplies the rough distance basis; exact distance remains absent")),
            [store, house, ordinaryBasis, ordinaryResolution],
            Text("place context supplies the rough distance basis; exact distance remains absent"));
        Add(
            "store-house-near-read",
            Address("Apply", Atom("near"), Atom("store-house-near-binding"), Atom("store-house-comparison")),
            [near, storeHouseBinding, storeHouseComparison],
            Text("store and house bind the open roles while the distance value remains low resolution"));

        var trace = Add("trace", Atom("trace"));
        var pad = Add("pad", Atom("pad"));
        var boardBasis = Add("board-clearance-basis", Address("Basis", Atom("pcb-clearance-distance")));
        var boardResolution = Add("board-clearance-resolution", Address("Res", Atom("manufacturing-clearance")));
        var tracePadBinding = Add(
            "trace-pad-near-binding",
            Address(
                "BindRoles",
                Atom("preposition-role-frame"),
                Atom("trace"),
                Atom("pad"),
                Atom("board-clearance-basis"),
                Atom("board-clearance-resolution")),
            [roleFrame, trace, pad, boardBasis, boardResolution]);
        var tracePadComparison = Add(
            "trace-pad-comparison",
            ResolutionFacetStructureSurface.Comparison(
                new Carrier(),
                trace,
                pad,
                boardResolution,
                Text("near can be descriptive or a clearance-rule warning in this context")),
            [trace, pad, boardBasis, boardResolution],
            Text("near can be descriptive or a clearance-rule warning in this context"));
        Add(
            "trace-pad-near-read",
            Address("Apply", Atom("near"), Atom("trace-pad-near-binding"), Atom("trace-pad-comparison")),
            [near, tracePadBinding, tracePadComparison],
            Text("same focus shape, but the CAD basis changes the meaning of near"));

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
