using System.Numerics;
using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected closed-bound reads over local samples, calibrated contributions and
/// shared slots. All premises and results are ordinary relations. These reads
/// neither settle carriers nor infer probability or private source ancestry.
/// </summary>
public static partial class BoundedQuantity
{
    /// <summary>Connects an unresolved slot or exact sample to its support and evidence.</summary>
    public static Relation Sample(Carrier root, IElement sample, Relation support, IElement evidence) =>
        Relation.OrderedGroup(root, sample, support, evidence);

    /// <summary>Connects an exact offset, ordered sample-to-calibration contributions and provenance.</summary>
    public static Relation Affine(Carrier root, IElement offset, Relation contributions, IElement provenance) =>
        Relation.OrderedGroup(root, offset, contributions, provenance);

    /// <summary>Reads exactly the three affine fields; no recursive discovery or evaluation.</summary>
    public static bool TryReadAffine(Relation expression, out IElement offset,
        out Relation contributions, out IElement provenance)
    {
        if (expression.TryReadOrderedGroup(out var fields) && fields.Length == 3 &&
            TryScalar(fields[0], out _, out _) && fields[1] is Relation terms &&
            terms.TryReadOrderedGroup(out _))
        {
            offset = fields[0]; contributions = terms; provenance = fields[2];
            return true;
        }
        offset = provenance = null!; contributions = null!;
        return false;
    }

    /// <summary>
    /// Reads exact lower/upper endpoints and their minimal outward whole-tick
    /// enclosure from a successful observation. Open or malformed results fail.
    /// </summary>
    public static bool TryReadBounds(Relation witness, out Carrier basis, out IElement lower,
        out IElement upper, out Relation enclosingTicks)
    {
        if (witness.TryReadOrderedGroup(out var fields) && fields.Length == 5 &&
            fields[2] is Relation result && result.TryReadOrderedGroup(out var values) && values.Length == 3 &&
            TryScalar(values[0], out var lowerBasis, out var lo) &&
            TryScalar(values[1], out var upperBasis, out var hi) && lowerBasis.SameIdentityAs(upperBasis) &&
            lo <= hi && values[2] is Relation enclosure &&
            TrySpan(enclosure, out var frameBasis, out var start, out var end) &&
            lowerBasis.SameIdentityAs(frameBasis) && start == lo.Floor() && end == hi.Ceiling())
        {
            basis = lowerBasis; lower = values[0]; upper = values[1]; enclosingTicks = enclosure;
            return true;
        }
        basis = null!; lower = upper = null!; enclosingTicks = null!;
        return false;
    }

    private static bool TryBounds(Relation witness, out Carrier basis, out Scalar lower, out Scalar upper)
    {
        if (TryReadBounds(witness, out basis, out var lo, out var hi, out _) &&
            TryScalar(lo, out _, out lower) && TryScalar(hi, out _, out upper)) return true;
        lower = upper = Scalar.Zero;
        return false;
    }

    private static Relation Observation(IElement source, IElement readSurface, Carrier basis,
        Scalar lower, Scalar upper, IElement provenance)
    {
        var floor = lower.Floor();
        var ceiling = upper.Ceiling();
        var result = Relation.OrderedGroup(new Carrier(), lower.Encode(basis), upper.Encode(basis),
            Relation.Segment(basis, checked((long)floor), checked((long)ceiling)));
        var rounding = Relation.OrderedGroup(new Carrier(),
            (lower - new Scalar(floor)).Encode(basis), (new Scalar(ceiling) - upper).Encode(basis));
        return ReadWitness.Create(new Carrier(), source, readSurface, result, rounding, provenance);
    }

    private static bool Unresolved(IElement request, out Relation witness)
    {
        witness = ReadWitness.Create(new Carrier(), request, request,
            new Placeholder(), request, new Placeholder());
        return false;
    }

    private static bool TryScalar(IElement element, out Carrier basis, out Scalar value)
    {
        if (ProjectionResidue.TryRead(element, out basis, out var numerator, out var denominator) &&
            !basis.IsUnknown && !basis.IsInverse)
        {
            value = new Scalar(numerator, denominator);
            return true;
        }
        basis = null!; value = Scalar.Zero;
        return false;
    }

    private static bool TryPoint(IElement element, out CarrierLocation point)
    {
        if (element is Relation { Left: CarrierLocation location, Right: Carrier carrier } &&
            !carrier.IsUnknown && !carrier.IsInverse && location.Carrier.SameIdentityAs(carrier))
        {
            point = location;
            return true;
        }
        point = default;
        return false;
    }

    private static bool TrySpan(IElement element, out Carrier basis, out long start, out long end)
    {
        if (element is Relation span && TryPoint(span.Left, out var left) &&
            TryPoint(span.Right, out var right) && left.Carrier.SameIdentityAs(right.Carrier))
        {
            basis = left.Carrier; start = left.Ticks; end = right.Ticks;
            return true;
        }
        basis = null!; start = end = 0;
        return false;
    }

    private static bool TryCalibration(Relation calibration, out Carrier source, out Carrier target,
        out Scalar slope, out Scalar offset)
    {
        if (TrySpan(calibration.Left, out source, out var a, out var b) && a != b &&
            TrySpan(calibration.Right, out target, out var c, out var d))
        {
            slope = new Scalar((BigInteger)d - c, (BigInteger)b - a);
            offset = new Scalar(c) - slope * new Scalar(a);
            return true;
        }
        source = target = null!; slope = offset = Scalar.Zero;
        return false;
    }
}
