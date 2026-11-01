using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected landmark/calibration experiments over ordinary relation surfaces.
/// A conditional prediction does not establish the report's actual quantity,
/// its spread, source reliability, or which premise should change.
/// See GojoLandmarkCalibrationFixture.md for the exact ordered contracts.
/// </summary>
public static partial class LocalCalibration
{
    /// <summary>
    /// Reads group(observation, candidate). Observation has source, event, sample,
    /// subject and context; candidate has source, context, map, floor, expectation
    /// and evidence. False can retain a calculated but inadmissible prediction.
    /// </summary>
    public static bool TryPredict(Relation request, out Relation witness)
    {
        if (!Fields(request, 2, out var r) || !Observation(r[0], out var o) ||
            !Fields(r[1], 6, out var c) || !Same(o[0], c[0]) || !Same(o[4], c[1]) ||
            !TryScalar(o[2], out var input, out var x) ||
            !Map(c[2], out var source, out var a, out var b, out var target, out var d, out var e) ||
            !input.SameIdentityAs(source) ||
            !TryScalar(c[3], out var floorBasis, out var floor) || !target.SameIdentityAs(floorBasis))
            return Unresolved(request, out witness);

        try
        {
            var y = d + (x - a) * (e - d) / (b - a);
            var result = y.Encode(target);
            var remainder = new List<IElement>();
            if (d == e) remainder.Add(new Relation(o[2], c[2]));
            if (y < floor) remainder.Add(new Relation(result, c[3]));
            witness = ReadWitness.Create(new Carrier(), r[0], r[1], result, Group([.. remainder]),
                Group(new Relation(o[3], result), c[4], c[5]));
            return y >= floor;
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
    }

    /// <summary>
    /// Reads group(prediction, feedback observation, feedback map, correspondence).
    /// The correspondence connects the actual two observation surfaces, and their
    /// subject/context identities must agree. Result is mapped feedback - prediction.
    /// No observation is promoted to an authoritative truth or automatic delta.
    /// </summary>
    public static bool TryCompare(Relation request, out Relation witness)
    {
        if (!Fields(request, 4, out var r) || !Prediction(r[0], out var p, out var basis, out var predicted) ||
            !Observation(p[0], out var report) || !Observation(r[1], out var feedback) ||
            !Same(report[3], feedback[3]) || !Same(report[4], feedback[4]) ||
            r[3] is not Relation correspondence || !Same(correspondence.Left, p[0]) ||
            !Same(correspondence.Right, r[1]) ||
            !TryScalar(feedback[2], out var feedbackBasis, out var sample) ||
            !Map(r[2], out var source, out var a, out var b, out var target, out var c, out var d) ||
            !source.SameIdentityAs(feedbackBasis) || !target.SameIdentityAs(basis) || c == d)
            return Unresolved(request, out witness);

        try
        {
            var expected = (c + (sample - a) * (d - c) / (b - a)).Encode(basis);
            TryScalar(expected, out _, out var value);
            witness = ReadWitness.Create(new Carrier(), r[0], request, (value - predicted).Encode(basis),
                p[3], Group(expected, r[1], r[3]));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
    }

    // Check the selected arithmetic, not the truth of an external observation.
    private static bool Prediction(IElement element, out IElement[] fields, out Carrier basis, out Scalar value)
    {
        if (Fields(element, 5, out fields) && TryScalar(fields[2], out basis, out value) &&
            TryPredict(Group(fields[0], fields[1]), out var recalculated) &&
            Fields(recalculated, 5, out var checkedFields) && ScalarEqual(fields[2], checkedFields[2])) return true;
        basis = null!; value = Scalar.Zero;
        return false;
    }

    private static bool Comparison(IElement element, out IElement[] fields, out Carrier basis, out Scalar residual)
    {
        if (Fields(element, 5, out fields) && fields[1] is Relation request &&
            Fields(request, 4, out var inputs) && Same(fields[0], inputs[0]) &&
            TryScalar(fields[2], out basis, out residual) && TryCompare(request, out var checkedWitness) &&
            Fields(checkedWitness, 5, out var c) && ScalarEqual(fields[2], c[2])) return true;
        basis = null!; residual = Scalar.Zero;
        return false;
    }

    private static bool Observation(IElement element, out IElement[] fields) =>
        Fields(element, 5, out fields) && Site(fields[1], out _) &&
        fields[3] is Placeholder { IsInverse: false };

    private static bool Map(IElement element, out Carrier source, out Scalar a, out Scalar b,
        out Carrier target, out Scalar c, out Scalar d)
    {
        if (element is Relation r && Range(r.Left, out source, out a, out b) && a != b &&
            Range(r.Right, out target, out c, out d)) return true;
        source = target = null!; a = b = c = d = Scalar.Zero;
        return false;
    }

    private static bool Range(IElement element, out Carrier basis, out Scalar start, out Scalar end)
    {
        if (element is Relation r && TryScalar(r.Left, out basis, out start) &&
            TryScalar(r.Right, out var other, out end) && basis.SameIdentityAs(other)) return true;
        basis = null!; start = end = Scalar.Zero;
        return false;
    }

    private static bool TryScalar(IElement element, out Carrier basis, out Scalar value)
    {
        if (Site(element, out var site)) element = site;
        if (ProjectionResidue.TryRead(element, out basis, out var n, out var d) &&
            !basis.IsUnknown && !basis.IsInverse)
        {
            value = new Scalar(n, d);
            return true;
        }
        basis = null!; value = Scalar.Zero;
        return false;
    }

    private static bool Site(IElement element, out CarrierLocation location)
    {
        if (element is CarrierLocation l && !l.Carrier.IsUnknown && !l.Carrier.IsInverse)
        { location = l; return true; }
        if (element is Relation { Left: CarrierLocation p, Right: Carrier c } &&
            !c.IsUnknown && !c.IsInverse && p.Carrier.SameIdentityAs(c))
        { location = p; return true; }
        location = default; return false;
    }

    private static bool ScalarEqual(IElement first, IElement second) =>
        TryScalar(first, out var a, out var x) && TryScalar(second, out var b, out var y) &&
        a.SameIdentityAs(b) && x == y;

    private static bool Same(IElement first, IElement second) =>
        ReferenceEquals(first, second) ||
        first is Placeholder p && second is Placeholder q && p.Equals(q) ||
        first is CarrierLocation a && second is CarrierLocation b &&
        a.Carrier.SameIdentityAs(b.Carrier) && ReferenceEquals(a.Tick, b.Tick);

    private static bool Fields(IElement element, int count, out IElement[] fields)
    {
        if (element is Relation r && r.TryReadOrderedGroup(out fields) && fields.Length == count) return true;
        fields = []; return false;
    }

    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);

    private static bool Unresolved(IElement request, out Relation witness)
    {
        witness = ReadWitness.Create(new Carrier(), request, request, new Placeholder(), request, new Placeholder());
        return false;
    }
}
