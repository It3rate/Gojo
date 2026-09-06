namespace Gojo.Core;

public sealed partial class UnitShape
{
    /// <summary>
    /// Freezes an explicitly selected quotient into ordered unit surfaces and
    /// standard provenance. Construction is internal to the checked expression
    /// read; it performs no cancellation, conversion, or numeric division.
    /// </summary>
    internal static UnitShape Quotient(
        IReadOnlyList<UnitShape> numerator, IReadOnlyList<UnitShape> denominator, IElement standard)
    {
        var descriptors = new List<IElement>();
        IElement[] Surfaces(IReadOnlyList<UnitShape> units) => [.. units.Select(unit =>
            RebindDescriptorSlots(unit.surface, unit.descriptors, descriptors))];
        var n = Surfaces(numerator);
        var d = Surfaces(denominator);
        var standardSlot = DescriptorSlot(FindOrAddDescriptor(descriptors, standard));
        return new UnitShape(Relation.OrderedGroup(Carrier.Unknown,
            Relation.OrderedGroup(Carrier.Unknown, n),
            Relation.OrderedGroup(Carrier.Unknown, d), standardSlot), descriptors);
    }

    /// <summary>
    /// Reads a quotient tick promise without reducing or reordering its factors.
    /// Empty numerator means reciprocal participation; both empty means an
    /// explicitly dimensionless tick under this standard, not a wildcard unit.
    /// </summary>
    public bool TryReadQuotient(out UnitShape[] numerator, out UnitShape[] denominator, out IElement standard)
    {
        if (TryReadOpposedFaceCore(surface, out var inner))
            return ShapeFromSurface(inner).TryReadQuotient(out numerator, out denominator, out standard);

        if (TryReadQuotientSurface(surface, out var n, out var d, out var slot) &&
            TryReadDescriptor(slot, out standard))
        {
            numerator = [.. n.Select(ShapeFromSurface)];
            denominator = [.. d.Select(ShapeFromSurface)];
            return true;
        }

        numerator = [];
        denominator = [];
        standard = null!;
        return false;
    }

    private static bool TryReadQuotientSurface(
        IElement element, out IElement[] numerator, out IElement[] denominator, out int standardSlot)
    {
        if (element is Relation { Right: Carrier holder } group && holder.IsUnknown &&
            group.TryReadOrderedGroup(out var fields) && fields.Length == 3 &&
            fields[0] is Relation n && n.TryReadOrderedGroup(out numerator) &&
            fields[1] is Relation d && d.TryReadOrderedGroup(out denominator) &&
            TryReadDescriptorSlot(fields[2], out standardSlot))
            return true;

        numerator = [];
        denominator = [];
        standardSlot = 0;
        return false;
    }
}
