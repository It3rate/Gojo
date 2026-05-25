using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Equations;

/// <summary>
/// Inspection helpers for applying patterned equations to binding-frame streams.
/// Each sample records supplied bindings, prediction residue, and unresolved
/// slots as visible Gojo structure.
/// </summary>
public static class EquationPatternInspection
{
    /// <summary>
    /// Applies one patterned equation to an ordered stream of binding frames.
    /// Frames may be partial; unresolved slots are recorded per sample.
    /// </summary>
    public static Relation Map(
        Carrier root,
        Relation equationPattern,
        IElement inputPattern,
        IReadOnlyList<IReadOnlyList<Relation>> frames,
        Carrier output,
        out Relation[] samples)
    {
        var holder = RootOrNew(root);
        var index = new Carrier();
        var count = CountOf(frames);
        samples = new Relation[count];

        for (var sampleIndex = 0; sampleIndex < count; sampleIndex++)
        {
            var frame = frames[sampleIndex] ?? [];
            EquationPattern.TryPredict(
                equationPattern,
                inputPattern,
                output,
                frame,
                out var prediction,
                out var unboundSlots);
            samples[sampleIndex] = PatternSample(index, sampleIndex, frame, prediction, unboundSlots);
        }

        return Relation.OrderedGroup(
            holder,
            [
                Relation.Segment(index, 0, count),
                equationPattern ?? Relation.Point(Carrier.Unknown, 0),
                inputPattern ?? Relation.Point(Carrier.Unknown, 0),
                .. samples
            ]);
    }

    /// <summary>Reads patterned samples from a structure returned by Map.</summary>
    public static bool TryReadMapSamples(Relation map, out Relation[] samples)
    {
        if (!TryReadRootElements(map, out var elements) || elements.Length < 3)
        {
            samples = [];
            return false;
        }

        samples = [.. elements.Skip(3).OfType<Relation>()];
        return true;
    }

    /// <summary>Reads one patterned prediction sample.</summary>
    public static bool TryReadPatternSample(
        Relation sample,
        out CarrierLocation index,
        out Relation[] bindings,
        out CarrierLocation predicted,
        out IElement residue,
        out IElement[] unboundSlots)
    {
        if (TryReadRootElements(sample, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation indexPoint &&
            indexPoint.Left is CarrierLocation indexLocation &&
            elements[1] is Relation bindingGroup &&
            elements[2] is CarrierLocation predictedLocation &&
            elements[4] is Relation unboundGroup)
        {
            index = indexLocation;
            bindings = ReadRelationGroup(bindingGroup);
            predicted = predictedLocation;
            residue = elements[3];
            unboundSlots = ReadElementGroup(unboundGroup);
            return true;
        }

        index = default;
        bindings = [];
        predicted = default;
        residue = default!;
        unboundSlots = [];
        return false;
    }

    private static Relation PatternSample(
        Carrier index,
        int sampleIndex,
        IReadOnlyList<Relation> bindings,
        Relation prediction,
        IReadOnlyList<IElement> unboundSlots)
    {
        var predicted = prediction.Left is CarrierLocation location
            ? location
            : new CarrierLocation(Carrier.Unknown, 0);
        var residue = prediction.Left is CarrierLocation
            ? prediction.Right
            : prediction;

        return Relation.OrderedGroup(
            new Carrier(),
            Relation.Point(index, sampleIndex),
            BindingGroup(bindings),
            predicted,
            residue,
            ElementGroup(unboundSlots));
    }

    private static Relation BindingGroup(IReadOnlyList<Relation> bindings) =>
        ElementGroup(StructuralPattern.ReadSlotBindings(bindings));

    private static Relation ElementGroup(IReadOnlyList<IElement> elements) =>
        Relation.OrderedGroup(new Carrier(), [.. elements]);

    private static Relation[] ReadRelationGroup(Relation group) =>
        TryReadRootElements(group, out var elements)
            ? [.. elements.OfType<Relation>()]
            : [];

    private static IElement[] ReadElementGroup(Relation group) =>
        TryReadRootElements(group, out var elements)
            ? elements
            : [];

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static int CountOf<T>(IReadOnlyList<T> values) =>
        values?.Count ?? 0;

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}
