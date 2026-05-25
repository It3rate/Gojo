using Gojo.Core;

namespace Gojo.Reads;

/// <summary>
/// Observational reads for fused carrier-face values: one positive face and
/// one opposed face of the same carrier, in either relation order.
/// </summary>
public static class FusedCarrierFaceReads
{
    /// <summary>Reads a fused positive/opposed carrier-face pair in either order.</summary>
    public static bool TryReadFusedCarrierFaces(
        this IElement fused,
        out CarrierLocation inverseFace,
        out CarrierLocation positiveFace) =>
        Relation.TryReadFusedCarrierFaces(fused, out inverseFace, out positiveFace);

    /// <summary>
    /// Multiplies two fused carrier-face values by distributing their positive
    /// and opposed participation into four visible product terms.
    /// </summary>
    public static Relation FusedCarrierFaceProduct(
        IElement left,
        IElement right,
        Carrier productCarrier,
        out Relation[] terms)
    {
        var carrier = productCarrier ?? Carrier.Unknown;
        if (!left.TryReadFusedCarrierFaces(out var leftInverse, out var leftPositive) ||
            !right.TryReadFusedCarrierFaces(out var rightInverse, out var rightPositive))
        {
            terms = [];
            return Relation.OrderedGroup(new Carrier());
        }

        terms =
        [
            ProductTerm(carrier, leftInverse, rightInverse),
            ProductTerm(carrier, leftInverse, rightPositive),
            ProductTerm(carrier, leftPositive, rightInverse),
            ProductTerm(carrier, leftPositive, rightPositive)
        ];
        return Relation.OrderedGroup(new Carrier(), terms);
    }

    /// <summary>
    /// Folds locations on a carrier and its opposed face into one visible
    /// location. This is a scalar-face read, not a full structured product read.
    /// </summary>
    public static CarrierLocation FoldCarrierFaces(Carrier carrier, params CarrierLocation[] locations)
    {
        var positive = carrier ?? Carrier.Unknown;
        var inverse = positive.IsUnknown ? Carrier.Unknown : positive.Inverse();
        var net = 0L;
        foreach (var location in locations ?? [])
        {
            if (location.Carrier.SameIdentityAs(positive))
            {
                net = checked(net + location.Ticks);
            }
            else if (location.Carrier.SameIdentityAs(inverse))
            {
                net = checked(net - location.Ticks);
            }
        }

        return net >= 0
            ? new CarrierLocation(positive, net)
            : new CarrierLocation(inverse, checked(-net));
    }

    private static Relation ProductTerm(
        Carrier productCarrier,
        CarrierLocation left,
        CarrierLocation right)
    {
        var targetCarrier = TargetFace(productCarrier, left.Carrier.IsInverse || right.Carrier.IsInverse);
        return new Relation(
            new CarrierLocation(targetCarrier, checked(left.Ticks * right.Ticks)),
            new Relation(left, right));
    }

    private static Carrier TargetFace(Carrier carrier, bool inverse) =>
        inverse && !carrier.IsUnknown ? carrier.Inverse() : carrier;

}

/// <summary>
/// Compatibility aliases for the explicit directed endpoint segment shape.
/// A directed segment is one fused carrier-face value, but fused carrier-face
/// math is not limited to literal segment semantics.
/// </summary>
public static class DirectedSegmentReads
{
    /// <summary>Reads the opposed start and positive endpoint of a directed segment.</summary>
    public static bool TryReadDirectedSegment(
        this Relation segment,
        out CarrierLocation inverseStart,
        out CarrierLocation positiveEnd) =>
        FusedCarrierFaceReads.TryReadFusedCarrierFaces(segment, out inverseStart, out positiveEnd);

    /// <summary>Compatibility wrapper for fused carrier-face multiplication.</summary>
    public static Relation DirectedSegmentProduct(
        Relation left,
        Relation right,
        Carrier productCarrier,
        out Relation[] terms) =>
        FusedCarrierFaceReads.FusedCarrierFaceProduct(left, right, productCarrier, out terms);
}
