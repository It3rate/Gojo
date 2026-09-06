using Gojo.Core;

namespace Gojo.Structures;

public static partial class BoundedQuantity
{
    /// <summary>
    /// Observes ports(left affine, right affine) -> S(output,0,1) in its exact
    /// ordered product unit. Bounds enclose all admitted shared-source states;
    /// an identical-function square also retains its nonnegative range.
    /// </summary>
    public static bool TryMultiply(Relation request, out Relation witness)
    {
        if (request.Left is not Relation operands || !operands.TryReadOrderedGroup(out var inputs) ||
            inputs.Length != 2 || inputs[0] is not Relation a || inputs[1] is not Relation b ||
            !TrySpan(request.Right, out var output, out var start, out var end) || start != 0 || end != 1 ||
            !TryAnalyze(a, out var left) || !TryAnalyze(b, out var right) || !TryJoint(left, right, out var joint) ||
            !output.UnitShape.Equals(UnitShape.OrthogonalProduct(left.Basis.UnitShape, right.Basis.UnitShape)))
            return Unresolved(request, out witness);

        Bounds(left, joint, out var loA, out var hiA);
        Bounds(right, joint, out var loB, out var hiB);
        var corners = new[] { loA * loB, loA * hiB, hiA * loB, hiA * hiB };
        var lower = corners.Min(); var upper = corners.Max();
        if (SameFunction(left, right))
        {
            lower = loA <= Scalar.Zero && hiA >= Scalar.Zero
                ? Scalar.Zero : Scalar.Min(loA * loA, hiA * hiA);
            upper = Scalar.Max(loA * loA, hiA * hiA);
        }
        witness = Observation(request, request, output, lower, upper,
            Relation.OrderedGroup(new Carrier(), Normalize(left, a), Normalize(right, b)));
        return true;
    }

    /// <summary>
    /// Observes group(numerator affine, denominator affine, output, standard).
    /// A denominator enclosure containing zero remains open, including X/X.
    /// Results use the native quotient unit; no implicit dimensional reduction.
    /// </summary>
    public static bool TryDivide(Relation request, out Relation witness)
    {
        if (!request.TryReadOrderedGroup(out var fields) || fields.Length != 4 ||
            fields[0] is not Relation a || fields[1] is not Relation b ||
            fields[2] is not Carrier output || output.IsUnknown || output.IsInverse ||
            !TryAnalyze(a, out var left) || !TryAnalyze(b, out var right) || !TryJoint(left, right, out var joint))
            return Unresolved(request, out witness);

        var units = UnitQuotient.Create(new Carrier(),
            Relation.OrderedGroup(new Carrier(), Relation.Point(left.Basis, 1)),
            Relation.OrderedGroup(new Carrier(), Relation.Point(right.Basis, 1)), fields[3]);
        if (!UnitQuotient.TryReadUnit(units, out var unit) || !output.UnitShape.Equals(unit))
            return Unresolved(request, out witness);

        Bounds(left, joint, out var loA, out var hiA);
        Bounds(right, joint, out var loB, out var hiB);
        if (loB <= Scalar.Zero && hiB >= Scalar.Zero) return Unresolved(request, out witness);

        var corners = new[] { loA / loB, loA / hiB, hiA / loB, hiA / hiB };
        var lower = SameFunction(left, right) ? Scalar.One : corners.Min();
        var upper = SameFunction(left, right) ? Scalar.One : corners.Max();
        witness = Observation(request, new Relation(request, units), output, lower, upper,
            Relation.OrderedGroup(new Carrier(), Normalize(left, a), Normalize(right, b), units));
        return true;
    }

    /// <summary>
    /// Observes group(quantity observation, permitted span, specification evidence)
    /// as the nonnegative distance to that span. Zero maximum violation supports
    /// conformance; positive minimum excludes it; overlap remains unsettled.
    /// True means the distance read succeeded, not that the quantity conforms.
    /// </summary>
    public static bool TryObserveConformance(Relation request, out Relation witness)
    {
        if (!request.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
            fields[0] is not Relation observation ||
            !TryBounds(observation, out var basis, out var lower, out var upper) ||
            !TrySpan(fields[1], out var allowedBasis, out var a, out var b) ||
            !basis.SameIdentityAs(allowedBasis)) return Unresolved(request, out witness);

        var allowedLower = new Scalar(Math.Min(a, b));
        var allowedUpper = new Scalar(Math.Max(a, b));
        var minimum = Scalar.Max(Scalar.Zero, Scalar.Max(allowedLower - upper, lower - allowedUpper));
        var maximum = Scalar.Max(Scalar.Zero, Scalar.Max(allowedLower - lower, upper - allowedUpper));
        witness = Observation(request, new Relation(observation, fields[1]), basis, minimum, maximum, fields[2]);
        return true;
    }
}
