using Gojo.Core;

namespace Gojo.Encoding.Structures;

/// <summary>
/// Tiny resolution definition samples built as Gojo relation structure. The
/// returned ledger is an inspection surface for resolution facets: partition,
/// unit comparison, observer read, omission, approximation, ambiguity, ladder
/// refinement, and runtime resolution.
/// </summary>
public static class ResolutionDefinitionSurface
{
    public static Relation CreateFacetLedger(Carrier root)
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

        var spanBody = Relation.Segment(new Carrier(), 0, 10);
        var span = Add("span", spanBody);

        var resolutionCarrier = new Carrier();
        var runtimeClock = new Carrier();

        var partitionCount = Add("partition-count", Atom("5"));
        var partitionResolution = Add(
            "partition-resolution",
            ResolutionFacetStructureSurface.Partition(
                new Carrier(),
                span,
                partitionCount,
                ResolutionFacetStructureSurface.PartitionSites(spanBody, 5)),
            [span, partitionCount]);
        Add(
            "partition-site",
            Address("Site", Atom("partition-resolution"), Atom("tick")),
            [partitionResolution],
            Text("partition sites are readable but not all listed at this resolution"));

        var unitSpan = Add("unit-span", Relation.Segment(new Carrier(), 0, 1));
        var unitComparison = Add(
            "unit-comparison",
            ResolutionFacetStructureSurface.Comparison(
                new Carrier(),
                span,
                unitSpan,
                residueOrTension: Text("comparison residue preserves calibration mismatch")),
            [span, unitSpan],
            Text("comparison residue preserves calibration mismatch"));
        Add(
            "unit-resolution",
            ResolutionFacetStructureSurface.UnitResolution(new Carrier(), unitComparison),
            [unitComparison]);

        var observerBasis = Add(
            "observer-basis",
            ResolutionFacetStructureSurface.ObserverBasis(new Carrier(), span),
            [span]);
        var coarseRung = Add(
            "coarse-rung",
            ResolutionFacetStructureSurface.ResolutionRung(new Carrier(), resolutionCarrier, "rough", 1));
        var observerRead = Add(
            "observer-resolution",
            ResolutionFacetStructureSurface.ObserverResolution(
                new Carrier(),
                span,
                observerBasis,
                coarseRung,
                Text("lower detail remains open at selected observer resolution")),
            [span, observerBasis, coarseRung],
            Text("lower detail remains open at selected observer resolution"));
        Add(
            "coarse-read-result",
            ConceptDefinitionSurface.Reference(new Carrier(), observerRead, coarseRung),
            [observerRead, coarseRung]);

        Add(
            "omitted-input-slot",
            ResolutionFacetStructureSurface.Slot(new Carrier(), Atom("input")),
            residueOrTension: Text("input role is intentionally open, not invalid"));

        var target = Add(
            "target",
            ResolutionFacetStructureSurface.Target(
                new Carrier(),
                Text("sharper target is aspirational or unavailable")),
            residueOrTension: Text("sharper target is aspirational or unavailable"));
        var approximateComparison = Add(
            "approximate-comparison",
            ResolutionFacetStructureSurface.Comparison(
                new Carrier(),
                span,
                target,
                coarseRung,
                Text("compatible at selected resolution with delta unresolved")),
            [span, target, coarseRung],
            Text("compatible at selected resolution with delta unresolved"));
        Add(
            "approximation-resolution",
            ResolutionFacetStructureSurface.Approximation(
                new Carrier(),
                approximateComparison,
                coarseRung,
                Text("near enough at this rung while sharper difference remains residue")),
            [approximateComparison, coarseRung],
            Text("near enough at this rung while sharper difference remains residue"));

        var ambiguousBasis = Add(
            "ambiguous-basis",
            ResolutionFacetStructureSurface.AmbiguousBasis(
                new Carrier(),
                span,
                Text("multiple read bases remain possible")),
            [span],
            Text("multiple read bases remain possible"));
        Add(
            "ambiguous-resolution",
            ResolutionFacetStructureSurface.ObserverResolution(
                new Carrier(),
                span,
                ambiguousBasis,
                EncodedTextSurface.UnknownPoint(),
                Text("read surface is unresolved rather than absent")),
            [span, ambiguousBasis],
            Text("read surface is unresolved rather than absent"));

        var fineRung = Add(
            "fine-rung",
            ResolutionFacetStructureSurface.ResolutionRung(new Carrier(), resolutionCarrier, "fine", 10));
        var roughRung = Add(
            "rough-rung",
            ResolutionFacetStructureSurface.Rung(new Carrier(), span, coarseRung),
            [span, coarseRung]);
        var sharperRung = Add(
            "fine-rung-read",
            ResolutionFacetStructureSurface.Rung(new Carrier(), span, fineRung),
            [span, fineRung]);
        var correspondence = Add(
            "rung-correspondence",
            ResolutionFacetStructureSurface.Correspondence(new Carrier(), roughRung, sharperRung),
            [roughRung, sharperRung]);
        Add(
            "ladder-resolution",
            ResolutionFacetStructureSurface.LadderResolution(
                new Carrier(),
                roughRung,
                sharperRung,
                correspondence,
                Text("fine rung refines the coarse rung without deleting coarse evidence")),
            [roughRung, sharperRung, correspondence],
            Text("fine rung refines the coarse rung without deleting coarse evidence"));

        var runtimeInput = Add(
            "runtime-input",
            ResolutionFacetStructureSurface.RuntimeInput(
                new Carrier(),
                Text("runtime input is variable at this read")),
            residueOrTension: Text("runtime input is variable at this read"));
        var runtimeStep = Add(
            "runtime-step",
            ResolutionFacetStructureSurface.RuntimeStep(new Carrier(), runtimeClock, "0", 0));
        Add(
            "runtime-resolution",
            ResolutionFacetStructureSurface.RuntimeResolution(
                new Carrier(),
                runtimeClock,
                span,
                runtimeInput,
                runtimeStep,
                coarseRung,
                Text("runtime residue belongs to this run, not permanently to the source")),
            [span, runtimeInput, runtimeStep, coarseRung],
            Text("runtime residue belongs to this run, not permanently to the source"));

        return ConceptDefinitionSurface.Ledger(root, definitions);
    }

    private static Relation Address(string lexical, params IElement[] parts) =>
        ConceptAddressSurface.Compound(new Carrier(), lexical, parts);

    private static Relation Atom(string name) =>
        ConceptAddressSurface.Atom(new Carrier(), name);

    private static Relation Text(string text) =>
        EncodedTextSurface.Create(new Carrier(), text);
}
