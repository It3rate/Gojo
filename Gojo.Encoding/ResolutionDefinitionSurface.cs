using Gojo.Core;

namespace Gojo.Encoding;

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

        var partitionCount = Add("partition-count", Atom("5"));
        var partitionResolution = Add(
            "partition-resolution",
            Address("Part", Atom("span"), Atom("partition-count")),
            [span, partitionCount]);
        Add(
            "partition-site",
            Address("Site", Atom("partition-resolution"), Atom("tick")),
            [partitionResolution],
            Text("partition sites are readable but not all listed at this resolution"));

        var unitSpan = Add("unit-span", Relation.Segment(new Carrier(), 0, 1));
        var unitComparison = Add(
            "unit-comparison",
            Address("Comp", Atom("span"), Atom("unit-span")),
            [span, unitSpan],
            Text("comparison residue preserves calibration mismatch"));
        Add(
            "unit-resolution",
            Address("U", Atom("unit-comparison")),
            [unitComparison]);

        var observerBasis = Add(
            "observer-basis",
            Address("B", Atom("span")),
            [span]);
        var coarseRung = Add(
            "coarse-rung",
            Address("Res", Atom("rough")));
        var observerRead = Add(
            "observer-resolution",
            Address("Read", Atom("span"), Atom("observer-basis"), Atom("coarse-rung")),
            [span, observerBasis, coarseRung],
            Text("lower detail remains open at selected observer resolution"));
        Add(
            "coarse-read-result",
            ConceptDefinitionSurface.Reference(new Carrier(), observerRead, coarseRung),
            [observerRead, coarseRung]);

        Add(
            "omitted-input-slot",
            Address("Slot", Atom("input"), Atom("?")),
            residueOrTension: Text("input role is intentionally open, not invalid"));

        var target = Add(
            "target",
            Address("Target", Atom("?")),
            residueOrTension: Text("sharper target is aspirational or unavailable"));
        var approximateComparison = Add(
            "approximate-comparison",
            Address("Comp", Atom("span"), Atom("target"), Atom("coarse-rung")),
            [span, target, coarseRung],
            Text("compatible at selected resolution with delta unresolved"));
        Add(
            "approximation-resolution",
            Address("Approx", Atom("approximate-comparison"), Atom("coarse-rung")),
            [approximateComparison, coarseRung],
            Text("near enough at this rung while sharper difference remains residue"));

        var ambiguousBasis = Add(
            "ambiguous-basis",
            Address("B?", Atom("span")),
            [span],
            Text("multiple read bases remain possible"));
        Add(
            "ambiguous-resolution",
            Address("Read", Atom("span"), Atom("ambiguous-basis")),
            [span, ambiguousBasis],
            Text("read surface is unresolved rather than absent"));

        var fineRung = Add(
            "fine-rung",
            Address("Res", Atom("fine")));
        var roughRung = Add(
            "rough-rung",
            Address("Rung", Atom("span"), Atom("coarse-rung")),
            [span, coarseRung]);
        var sharperRung = Add(
            "fine-rung-read",
            Address("Rung", Atom("span"), Atom("fine-rung")),
            [span, fineRung]);
        var correspondence = Add(
            "rung-correspondence",
            Address("Corr", Atom("rough-rung"), Atom("fine-rung-read")),
            [roughRung, sharperRung]);
        Add(
            "ladder-resolution",
            Address("Refine", Atom("rough-rung"), Atom("fine-rung-read"), Atom("rung-correspondence")),
            [roughRung, sharperRung, correspondence],
            Text("fine rung refines the coarse rung without deleting coarse evidence"));

        var runtimeInput = Add(
            "runtime-input",
            Address("Input", Atom("?")),
            residueOrTension: Text("runtime input is variable at this read"));
        var runtimeStep = Add("runtime-step", Address("t", Atom("0")));
        Add(
            "runtime-resolution",
            Address("Run", Atom("span"), Atom("runtime-input"), Atom("runtime-step"), Atom("coarse-rung")),
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
