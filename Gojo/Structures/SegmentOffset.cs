using Gojo.Core;
using static Gojo.Structures.PlanarMetric;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected straight-segment constructions in a supplied flat metric chart.
/// Requests retain the source, outlet, reach calibration and relative side.
/// These are geometric observations, not material movement or a CAD dispatcher.
/// </summary>
public static class SegmentOffset
{
    /// <summary>
    /// Reads group(edge, metric, O(edge) -> (native reach -> length span),
    /// edge start -> side point). Calculates one normal displacement and applies
    /// it to both endpoints. The result is the ordered offset edge. Provenance
    /// contains endpoint correspondence, displacement, dot product and squared
    /// distance. Irrational exact coordinates stay unresolved without rounding.
    /// </summary>
    public static bool TryConstruct(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 4, out var f) || f[0] is not Relation edge || !Metric(f[1], out var metric) ||
            !Complete(edge.Left, metric, out var a) || !Complete(edge.Right, metric, out var b) ||
            f[2] is not Relation { Left: Relation outlet } selection ||
            !UnitShape.TryReadOrthogonalSource(outlet, out _) || !ReferenceEquals(outlet.Left, edge) ||
            !Distance(selection.Right, metric, out _, out var distance) ||
            f[3] is not Relation side || !ReferenceEquals(side.Left, edge.Left) ||
            !Complete(side.Right, metric, out var sidePoint)) return Unresolved(request, out witness);
        var direction = Subtract(b, a);
        if (!Normal(direction, Subtract(sidePoint, a), distance, metric, out var displacement))
            return Unresolved(request, out witness);
        try
        {
            IElement first = distance.IsZero ? edge.Left : Vector(Add(a, displacement), metric);
            IElement last = distance.IsZero ? edge.Right : Vector(Add(b, displacement), metric);
            var result = new Relation(first, last);
            var correspondence = Group(new Relation(edge.Left, first), new Relation(edge.Right, last));
            witness = Observe(request, captured, result, [], Group(correspondence, Vector(displacement, metric),
                Dot(direction, displacement, metric).Encode(metric.Square),
                Dot(displacement, displacement, metric).Encode(metric.Square)));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
    }

    /// <summary>
    /// Reads group(first offset request, second offset request,
    /// group(first parameter frame -> allowed support, second frame -> support)).
    /// Source edges must share the actual join point, metric and reach calibration.
    /// Intersects their calculated straight supports, then trims/extends to a
    /// shared miter site only within both explicit supports. This is one local
    /// corner policy, not a round join, cap, region inset or intersection solver.
    /// </summary>
    public static bool TryJoin(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 3, out var f) || f[0] is not Relation firstRequest || f[1] is not Relation secondRequest ||
            !Fields(firstRequest, 4, out var first) || !Fields(secondRequest, 4, out var second) ||
            first[0] is not Relation firstSource || second[0] is not Relation secondSource ||
            !ReferenceEquals(firstSource.Right, secondSource.Left) || !ReferenceEquals(first[1], second[1]) ||
            first[2] is not Relation firstSelection || second[2] is not Relation secondSelection ||
            !ReferenceEquals(firstSelection.Right, secondSelection.Right) || !Fields(f[2], 2, out var supports) ||
            !Metric(first[1], out var metric)) return Unresolved(request, out witness);

        // These are constructed from the requests, not accepted from asserted or
        // potentially stale result fields in a supplied witness.
        var firstComplete = TryConstruct(firstRequest, out var firstRead);
        var secondComplete = TryConstruct(secondRequest, out var secondRead);
        var evidence = Group(firstRead, secondRead);
        if (!firstComplete || !secondComplete) return Unresolved(request, out witness, evidence);
        Fields(firstRead, 5, out var w1); Fields(secondRead, 5, out var w2);
        var e1 = (Relation)w1[2]; var e2 = (Relation)w2[2];
        Complete(e1.Left, metric, out var a); Complete(e1.Right, metric, out var b);
        Complete(e2.Left, metric, out var c); Complete(e2.Right, metric, out var d);
        var u = Subtract(b, a); var v = Subtract(d, c);
        Fields(w1[4], 4, out var p1); Fields(w2[4], 4, out var p2);
        Complete(p1[1], metric, out var displacement1); Complete(p2[1], metric, out var displacement2);
        // A connected path offset must choose the same relative side along its
        // ordered edges. Reflections of the chart reverse both signs together.
        if (Cross(u, displacement1).Numerator.Sign != Cross(v, displacement2).Numerator.Sign)
            return Unresolved(request, out witness, evidence);
        var determinant = Cross(u, v);
        if (determinant.IsZero) return Unresolved(request, out witness, evidence);
        var delta = Subtract(c, a);
        var t = Cross(delta, v) / determinant;
        var s = Cross(delta, u) / determinant;
        try
        {
            var joint = Vector(Add(a, Scale(u, t)), metric);
            var firstAllowed = Parameter(supports[0], t, out var firstParameter);
            var secondAllowed = Parameter(supports[1], s, out var secondParameter);
            var candidate = Group(joint, Group(firstParameter, secondParameter));
            if (!firstAllowed || !secondAllowed)
                return Unresolved(request, out witness, Group(evidence, candidate));
            var joinedFirst = new Relation(e1.Left, joint);
            var joinedSecond = new Relation(joint, e2.Right);
            witness = Observe(request, captured, Group(joinedFirst, joinedSecond), [],
                Group(evidence, candidate, Group(new Relation(e1, joinedFirst), new Relation(e2, joinedSecond))));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness, evidence); }
    }

    private static bool Parameter(IElement element, Scalar fraction, out IElement sample)
    {
        sample = new Placeholder();
        if (element is not Relation support || !Span(support.Left, out var axis, out var a, out var b) || a == b ||
            !Span(support.Right, out var allowed, out var lo, out var hi) || lo > hi || !axis.SameIdentityAs(allowed)) return false;
        var value = new Scalar(a) + fraction * new Scalar(new System.Numerics.BigInteger(b) - a);
        sample = value.Encode(axis);
        return value >= new Scalar(lo) && value <= new Scalar(hi);
    }

    private static Relation Capture(Relation r) => (Relation)new ExposedStructureSnapshot().Copy(r);
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static Relation Observe(IElement source, IElement surface, IElement result, IElement[] remainder, IElement provenance) =>
        ReadWitness.Create(new Carrier(), source, surface, result, Group(remainder), provenance);
    private static bool Unresolved(Relation request, out Relation witness, IElement? evidence = null)
    { witness = Observe(request, request, new Placeholder(), [request], evidence ?? request); return false; }
}
