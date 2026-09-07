using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected affine reads of exposed ordered coordinates. The request carries
/// its frames, support and basis; no graphic classifier or carrier settlement
/// participates. Results are exact rational coordinates or connected open slots.
/// </summary>
public static partial class AffineFrame
{
    /// <summary>
    /// Reads ports(first, second, group(sample, parameter frame, allowed support,
    /// coordinate basis)) -> output. The basis is group_on(holder, S(axis,0,1),...).
    /// Uses u=(sample-a)/(b-a), then (1-u)first+u second. A wider support explicitly
    /// permits extrapolation; reversed parameter frames remain ordered frames.
    /// False may return a useful partial witness; inspect its result and remainder.
    /// </summary>
    public static bool TryInterpolate(Relation request, out Relation witness)
    {
        if (!Fields(request.Left, 3, out var inputs) || !Fields(inputs[2], 4, out var grade) ||
            !TryBasis(grade[3], out var holder, out var axes) ||
            !TrySpan(grade[1], out var parameter, out var a, out var b) || a == b ||
            !TrySpan(grade[2], out var support, out var lo, out var hi) || lo > hi ||
            !parameter.SameIdentityAs(support) ||
            !TryScalar(grade[0], out var sampleBasis, out var sample) ||
            !parameter.SameIdentityAs(sampleBasis) || sample < lo || sample > hi ||
            !TryCoordinates(inputs[0], axes, out var first) ||
            !TryCoordinates(inputs[1], axes, out var second))
            return Unresolved(request, out witness);

        var u = (sample - a) / (b - a);
        if (inputs[0] is Placeholder && ReferenceEquals(inputs[0], inputs[1])) second = first;
        var result = new IElement[axes.Length];
        var remainder = new List<IElement>();
        try
        {
            for (var i = 0; i < axes.Length; i++)
            {
                var knownA = TryScalar(first[i], out _, out var x);
                var knownB = TryScalar(second[i], out _, out var y);
                if (knownA && knownB)
                    result[i] = ((Scalar.One - u) * x + u * y).Encode(axes[i]);
                else if (knownA && u.IsZero) result[i] = x.Encode(axes[i]);
                else if (knownB && u == Scalar.One) result[i] = y.Encode(axes[i]);
                else
                {
                    result[i] = ReferenceEquals(first[i], second[i]) ? first[i] : new Placeholder();
                    remainder.Add(new Relation(Group(first[i], second[i], inputs[2]), result[i]));
                }
            }
        }
        catch (OverflowException) { return Unresolved(request, out witness); }

        var point = Relation.OrderedGroup(holder, result);
        witness = Observe(request, inputs[2], point, remainder, new Relation(request.Right, point));
        return remainder.Count == 0;
    }

    /// <summary>
    /// Reads group(first, second, coordinate basis) as the exact displacement
    /// second-first. Zero displacement is a coordinate read, not site identity.
    /// Shared coordinate slots cancel; separately open coordinates stay open.
    /// </summary>
    public static bool TryDifference(Relation request, out Relation witness)
    {
        if (!Fields(request, 3, out var fields) ||
            !TryBasis(fields[2], out var holder, out var axes) ||
            !TryCoordinates(fields[0], axes, out var first) ||
            !TryCoordinates(fields[1], axes, out var second))
            return Unresolved(request, out witness);

        if (fields[0] is Placeholder && ReferenceEquals(fields[0], fields[1])) second = first;
        var result = new IElement[axes.Length];
        var remainder = new List<IElement>();
        try
        {
            for (var i = 0; i < axes.Length; i++)
            {
                if (TryScalar(first[i], out _, out var a) && TryScalar(second[i], out _, out var b))
                    result[i] = (b - a).Encode(axes[i]);
                else if (ReferenceEquals(first[i], second[i])) result[i] = Scalar.Zero.Encode(axes[i]);
                else
                {
                    result[i] = new Placeholder();
                    remainder.Add(new Relation(Group(first[i], second[i], Relation.Point(axes[i], 1)), result[i]));
                }
            }
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
        witness = Observe(request, fields[2], Relation.OrderedGroup(holder, result), remainder, request);
        return remainder.Count == 0;
    }

    /// <summary>
    /// Reads group(point, ordered coordinate calibrations, target basis).
    /// Each calibration is S(source,a,b) -> S(target,c,d). Explicit calibrations
    /// may change units. There is no implicit axis permutation or face normalization.
    /// </summary>
    public static bool TryProject(Relation request, out Relation witness) =>
        TryProject(request, null, out witness);

    private static bool TryProject(Relation request, Dictionary<object, IElement>[]? shared,
        out Relation witness)
    {
        if (!Fields(request, 3, out var fields) ||
            !TryBasis(fields[2], out var holder, out var targets) ||
            !Fields(fields[1], targets.Length, out var maps)) return Unresolved(request, out witness);

        var sources = new Carrier[targets.Length];
        var slopes = new Scalar[targets.Length];
        var offsets = new Scalar[targets.Length];
        for (var i = 0; i < targets.Length; i++)
        {
            if (maps[i] is not Relation map ||
                !TrySpan(map.Left, out sources[i], out var a, out var b) || a == b ||
                !TrySpan(map.Right, out var target, out var c, out var d) ||
                !targets[i].SameIdentityAs(target)) return Unresolved(request, out witness);
            slopes[i] = (d - c) / (b - a);
            offsets[i] = c - slopes[i] * a;
        }
        if (sources.Distinct(ReferenceEqualityComparer.Instance).Count() != sources.Length ||
            !TryCoordinates(fields[0], sources, out var coordinates)) return Unresolved(request, out witness);

        var result = new IElement[targets.Length];
        var remainder = new List<IElement>();
        try
        {
            for (var i = 0; i < targets.Length; i++)
            {
                var identity = CoordinateIdentity(coordinates[i]);
                if (shared is not null && shared[i].TryGetValue(identity, out var previous)) result[i] = previous;
                else
                {
                    if (TryScalar(coordinates[i], out _, out var x))
                        result[i] = (offsets[i] + slopes[i] * x).Encode(targets[i]);
                    else if (slopes[i].IsZero) result[i] = offsets[i].Encode(targets[i]);
                    else result[i] = new Placeholder();
                    shared?[i].Add(identity, result[i]);
                }
                // A constant target cannot account for its incoming coordinate.
                // Keep that source participation even though the result is known.
                if (slopes[i].IsZero) remainder.Add(new Relation(coordinates[i], maps[i]));
                else if (result[i] is Placeholder)
                    remainder.Add(new Relation(new Relation(coordinates[i], maps[i]), result[i]));
            }
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
        witness = Observe(request, Group(fields[1], fields[2]), Relation.OrderedGroup(holder, result), remainder, request);
        return result.All(coordinate => coordinate is not Placeholder);
    }

    private static object CoordinateIdentity(IElement coordinate) => coordinate switch
    {
        CarrierLocation location => location.Tick,
        Relation { Left: CarrierLocation location, Right: Carrier } => location.Tick,
        _ => coordinate
    };

    private static bool TryCoordinates(IElement element, Carrier[] axes, out IElement[] coordinates)
    {
        // Missing points have distinct open coordinate roles, linked back through
        // the observation's source. A whole point slot is not x=y=... evidence.
        if (element is Placeholder)
        {
            coordinates = axes.Select(_ => (IElement)new Placeholder()).ToArray();
            return true;
        }
        if (!Fields(element, axes.Length, out coordinates)) return false;
        for (var i = 0; i < axes.Length; i++)
            if (coordinates[i] is not Placeholder &&
                (!TryScalar(coordinates[i], out var basis, out _) || !axes[i].SameIdentityAs(basis))) return false;
        return true;
    }

    private static bool TryBasis(IElement element, out Carrier holder, out Carrier[] axes)
    {
        if (element is Relation { Right: Carrier candidate } basis && Concrete(candidate) &&
            basis.TryReadOrderedGroup(out var spans) && spans.Length > 0)
        {
            axes = new Carrier[spans.Length];
            for (var i = 0; i < spans.Length; i++)
                if (!TrySpan(spans[i], out axes[i], out var a, out var b) || !a.IsZero || b != Scalar.One)
                { holder = null!; axes = []; return false; }
            if (axes.Distinct(ReferenceEqualityComparer.Instance).Count() == axes.Length)
            { holder = candidate; return true; }
        }
        holder = null!; axes = []; return false;
    }

    private static bool TryScalar(IElement element, out Carrier basis, out Scalar scalar)
    {
        if (element is Relation { Left: CarrierLocation point, Right: Carrier carrier } &&
            point.Carrier.SameIdentityAs(carrier)) element = point;
        if (ProjectionResidue.TryRead(element, out basis, out var n, out var d) && Concrete(basis))
        { scalar = new Scalar(n, d); return true; }
        basis = null!; scalar = Scalar.Zero; return false;
    }

    private static bool TrySpan(IElement element, out Carrier basis, out Scalar a, out Scalar b)
    {
        if (element is Relation span && TryPoint(span.Left, out var first) &&
            TryPoint(span.Right, out var second) && first.Carrier.SameIdentityAs(second.Carrier))
        { basis = first.Carrier; a = new Scalar(first.Ticks); b = new Scalar(second.Ticks); return true; }
        basis = null!; a = b = Scalar.Zero; return false;
    }

    private static bool TryPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation { Left: CarrierLocation point, Right: Carrier carrier } &&
            Concrete(carrier) && point.Carrier.SameIdentityAs(carrier))
        { location = point; return true; }
        location = default; return false;
    }

    private static bool Concrete(Carrier carrier) => !carrier.IsUnknown && !carrier.IsInverse;

    private static bool Fields(IElement element, int count, out IElement[] fields)
    {
        if (element is Relation relation && relation.TryReadOrderedGroup(out fields) && fields.Length == count) return true;
        fields = []; return false;
    }

    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);

    private static Relation Observe(IElement source, IElement surface, IElement result,
        IEnumerable<IElement> remainder, IElement provenance) =>
        ReadWitness.Create(new Carrier(), source, surface, result, Group([.. remainder]), provenance);

    private static bool Unresolved(Relation source, out Relation witness)
    {
        witness = Observe(source, source, new Placeholder(), [source], source);
        return false;
    }
}
