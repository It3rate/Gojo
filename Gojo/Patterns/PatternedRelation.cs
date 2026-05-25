using Gojo.Core;

namespace Gojo.Patterns;

/// <summary>
/// Adapter helpers for relation patterns that contain placeholders. Each
/// helper instantiates the pattern into ordinary Gojo relation structure and
/// then delegates to the existing relation read/projection APIs.
/// </summary>
public static class PatternedRelation
{
    /// <summary>Reads unresolved slots from a relation pattern against a binding frame.</summary>
    public static IElement[] ReadUnboundSlots(Relation pattern, IReadOnlyList<Relation> bindings) =>
        StructuralPattern.ReadUnboundSlots(pattern, bindings);

    /// <summary>Reads unresolved slots from a relation pattern against a binding frame.</summary>
    public static IElement[] ReadUnboundSlots(Relation pattern, params Relation[] bindings) =>
        StructuralPattern.ReadUnboundSlots(pattern, bindings);

    /// <summary>Applies bindings and returns the instantiated relation when the root remains a relation.</summary>
    public static bool TryInstantiate(
        Relation pattern,
        IReadOnlyList<Relation> bindings,
        out Relation relation)
    {
        if (StructuralPattern.Apply(pattern, bindings) is Relation instantiated)
        {
            relation = instantiated;
            return true;
        }

        relation = null!;
        return false;
    }

    /// <summary>Applies bindings and reports unbound slots without treating them as invalid structure.</summary>
    public static bool TryInstantiateComplete(
        Relation pattern,
        IReadOnlyList<Relation> bindings,
        out Relation relation,
        out IElement[] unboundSlots)
    {
        var complete = StructuralPattern.TryApplyComplete(pattern, bindings, out var applied, out unboundSlots);
        if (applied is Relation instantiated)
        {
            relation = instantiated;
            return complete;
        }

        relation = null!;
        return false;
    }

    /// <summary>Applies bindings and reports unbound slots without treating them as invalid structure.</summary>
    public static bool TryInstantiateComplete(
        Relation pattern,
        out Relation relation,
        out IElement[] unboundSlots,
        params Relation[] bindings) =>
        TryInstantiateComplete(pattern, bindings, out relation, out unboundSlots);

    /// <summary>Applies bindings and returns the instantiated relation when the root remains a relation.</summary>
    public static bool TryInstantiate(
        Relation pattern,
        out Relation relation,
        params Relation[] bindings) =>
        TryInstantiate(pattern, bindings, out relation);

    /// <summary>Projects a location through a placeholder-bound relation pattern.</summary>
    public static bool TryProject(
        Relation portPattern,
        CarrierLocation location,
        Carrier reader,
        IReadOnlyList<Relation> bindings,
        out Relation projected)
    {
        if (TryInstantiate(portPattern, bindings, out var port))
        {
            return port.TryProject(location, reader, out projected);
        }

        projected = null!;
        return false;
    }

    /// <summary>Projects a location through a placeholder-bound relation pattern.</summary>
    public static bool TryProject(
        Relation portPattern,
        CarrierLocation location,
        Carrier reader,
        out Relation projected,
        params Relation[] bindings) =>
        TryProject(portPattern, location, reader, bindings, out projected);

    /// <summary>Reads an operation after binding placeholders in both operation and input surfaces.</summary>
    public static bool TryReadOperationFrom(
        Relation operationPattern,
        IElement inputPattern,
        Carrier reader,
        IReadOnlyList<Relation> bindings,
        out Relation read)
    {
        if (!TryInstantiate(operationPattern, bindings, out var operation))
        {
            read = null!;
            return false;
        }

        var input = StructuralPattern.Apply(inputPattern, bindings);
        return operation.TryReadOperationFrom(input, reader, out read);
    }

    /// <summary>Reads an operation after binding placeholders in both operation and input surfaces.</summary>
    public static bool TryReadOperationFrom(
        Relation operationPattern,
        IElement inputPattern,
        Carrier reader,
        out Relation read,
        params Relation[] bindings) =>
        TryReadOperationFrom(operationPattern, inputPattern, reader, bindings, out read);

    /// <summary>Crystallizes the unit shape exposed by a placeholder-bound relation pattern.</summary>
    public static bool TryCrystallize(
        Relation relationPattern,
        IReadOnlyList<Relation> bindings,
        out Carrier carrier)
    {
        if (TryInstantiate(relationPattern, bindings, out var relation))
        {
            return relation.TryCrystallize(out carrier);
        }

        carrier = null!;
        return false;
    }

    /// <summary>Crystallizes the unit shape exposed by a placeholder-bound relation pattern.</summary>
    public static bool TryCrystallize(
        Relation relationPattern,
        out Carrier carrier,
        params Relation[] bindings) =>
        TryCrystallize(relationPattern, bindings, out carrier);

    /// <summary>Crystallizes and projects a placeholder-bound operation value into the crystal.</summary>
    public static bool TryCrystallize(
        Relation operationPattern,
        IElement inputPattern,
        IReadOnlyList<Relation> bindings,
        out Carrier carrier,
        out Relation read)
    {
        if (!TryInstantiate(operationPattern, bindings, out var operation))
        {
            carrier = null!;
            read = null!;
            return false;
        }

        var input = StructuralPattern.Apply(inputPattern, bindings);
        return operation.TryCrystallize(input, out carrier, out read);
    }

    /// <summary>Crystallizes and projects a placeholder-bound operation value into the crystal.</summary>
    public static bool TryCrystallize(
        Relation operationPattern,
        IElement inputPattern,
        out Carrier carrier,
        out Relation read,
        params Relation[] bindings) =>
        TryCrystallize(operationPattern, inputPattern, bindings, out carrier, out read);
}
