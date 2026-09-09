using Gojo.Core;
using static Gojo.Structures.PlanarMetric;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

public static partial class SegmentOffset
{
    /// <summary>
    /// Reads group(offset request, S(scale,0,1)). Prepares a known metric-normal
    /// direction, its open squared-scale equation and two existing affine
    /// interpolation requests sharing that scale. Success means this partial
    /// construction is established, not that its endpoints are concrete.
    /// </summary>
    public static bool TryPrepare(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || f[0] is not Relation offset || !Context(offset, out var context) ||
            !Span(f[1], out var scaleAxis, out var start, out var end) || start != 0 || end != 1)
            return Unresolved(request, out witness);
        try
        {
            var metric = context.Metric;
            var scale = new Placeholder();
            // A square equation has two signs. Keep the selected nonnegative
            // branch in the actual interpolation support, not only in code.
            var support = Relation.Segment(scaleAxis, 0, checked((long)ScaleUpper(context)));
            Fields(context.Fields[1], 2, out var metricFields);
            var grade = Group(scale, f[1], support, metricFields[0]);
            var first = new Placeholder(); var second = new Placeholder();
            var normal = Vector(context.Normal, metric);
            var equation = Group(f[1], new Relation(scale, scale), new Relation(
                Dot(context.Normal, context.Normal, metric).Encode(metric.Square),
                (context.Distance * context.Distance).Encode(metric.Square)));
            var recipes = Group(
                new Relation(Group(context.Edge.Left, Vector(Add(context.A, context.Normal), metric), grade), first),
                new Relation(Group(context.Edge.Right, Vector(Add(context.B, context.Normal), metric), grade), second));
            var result = Group(normal, equation, recipes, new Relation(first, second));
            witness = Observe(request, captured, result, [],
                Dot(Subtract(context.B, context.A), context.Normal, metric).Encode(metric.Square));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
    }

    /// <summary>
    /// Reads group(prepared witness, fine whole -> actual prepared scale whole).
    /// Returns group(shared bounded fine sample, affine endpoint expressions,
    /// endpoint bound witnesses). One open sample drives both endpoints and
    /// both coordinates; boxes are marginal reads, not independent choices.
    /// The nonnegative root is enclosed by exact square comparisons at the
    /// selected subdivision. No midpoint or approximate exact endpoint is chosen.
    /// </summary>
    public static bool TryObserveAtResolution(Relation request, out Relation witness)
    {
        var captured = Capture(request);
        if (!Fields(captured, 2, out var f) || !Prepared(f[0], out var prepared) ||
            f[1] is not Relation map || !ReferenceEquals(map.Right, prepared.Whole) ||
            !Span(map.Left, out var fine, out var start, out var parts) || start != 0 || parts <= 0 ||
            fine.SameIdentityAs(prepared.ScaleAxis) ||
            !Subdivision(fine, prepared.ScaleAxis, parts))
            return Unresolved(request, out witness);
        var context = prepared.Context; var metric = context.Metric;
        var scaledSquare = context.Distance * context.Distance * new Scalar(parts) * new Scalar(parts) /
            Dot(context.Normal, context.Normal, metric);
        var lower = scaledSquare.SquareRootFloor();
        var upper = new Scalar(lower * lower) == scaledSquare ? lower : lower + 1;
        try
        {
            var lo = checked((long)lower); var hi = checked((long)upper);
            IElement sampleValue = lo == hi ? new CarrierLocation(fine, lo) : new Placeholder();
            var sample = BoundedQuantity.Sample(new Carrier(), sampleValue, Relation.Segment(fine, lo, hi),
                Group(f[0], new Relation(map, prepared.ScaleSlot)));
            var maps = context.Normal.Select((v, i) =>
            {
                var coefficient = v / new Scalar(parts);
                return new Relation(Relation.Segment(fine, 0, checked((long)coefficient.Denominator)),
                    Relation.Segment(metric.Axes[i], 0, checked((long)coefficient.Numerator)));
            }).ToArray();
            var expressions = new IElement[2]; var bounds = new IElement[2];
            for (var endpoint = 0; endpoint < 2; endpoint++)
            {
                var coordinates = endpoint == 0 ? context.A : context.B;
                var lanes = new IElement[2]; var reads = new IElement[2];
                for (var i = 0; i < 2; i++)
                {
                    var affine = BoundedQuantity.Affine(new Carrier(), coordinates[i].Encode(metric.Axes[i]),
                        Group(new Relation(sample, maps[i])), prepared.Recipes[endpoint]);
                    if (!BoundedQuantity.TryObserve(affine, out var read)) return Unresolved(request, out witness, f[0]);
                    lanes[i] = affine; reads[i] = read;
                }
                expressions[endpoint] = Relation.OrderedGroup(metric.Holder, lanes);
                bounds[endpoint] = Relation.OrderedGroup(metric.Holder, reads);
            }
            var result = Group(sample, new Relation(expressions[0], expressions[1]), new Relation(bounds[0], bounds[1]));
            witness = Observe(request, captured, result, [], Group(f[0],
                new Relation(sample, new Relation(map, prepared.ScaleSlot)), prepared.Equation,
                Group(new Relation(((Relation)prepared.Recipes[0]).Right, expressions[0]),
                    new Relation(((Relation)prepared.Recipes[1]).Right, expressions[1]))));
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness, f[0]); }
    }

    // The retained preparation is an exposed graph. Validate its connections
    // and calculations before consuming it; a five-field witness is no seal
    // of correctness, and its asserted normal or endpoint probes can be false.
    private static bool Prepared(IElement element, out PreparedRead prepared)
    {
        prepared = null!;
        if (!Fields(element, 5, out var w) || !Fields(w[1], 2, out var source) ||
            source[0] is not Relation offset || !Context(offset, out var context) ||
            !Span(source[1], out var axis, out var a, out var b) || a != 0 || b != 1 ||
            !Fields(w[2], 4, out var result) || !SameCoordinates(result[0], context.Normal, context.Metric) ||
            !Fields(result[1], 3, out var equation) || !ReferenceEquals(equation[0], source[1]) ||
            equation[1] is not Relation { Left: Placeholder { IsInverse: false } slot } product ||
            !ReferenceEquals(product.Right, slot) || equation[2] is not Relation constraint ||
            !Number(constraint.Left, out var normalAxis, out var normalSquared) ||
            !normalAxis.SameIdentityAs(context.Metric.Square) || normalSquared != Dot(context.Normal, context.Normal, context.Metric) ||
            !Number(constraint.Right, out var targetAxis, out var distanceSquared) ||
            !targetAxis.SameIdentityAs(context.Metric.Square) || distanceSquared != context.Distance * context.Distance ||
            !Fields(result[2], 2, out var recipes) || recipes[0] is not Relation first || recipes[1] is not Relation second ||
            !Fields(first.Left, 3, out var firstInputs) || !Fields(second.Left, 3, out var secondInputs) ||
            !ReferenceEquals(firstInputs[0], context.Edge.Left) || !ReferenceEquals(secondInputs[0], context.Edge.Right) ||
            !SameCoordinates(firstInputs[1], Add(context.A, context.Normal), context.Metric) ||
            !SameCoordinates(secondInputs[1], Add(context.B, context.Normal), context.Metric) ||
            !ReferenceEquals(firstInputs[2], secondInputs[2]) || !Fields(firstInputs[2], 4, out var grade) ||
            !ReferenceEquals(grade[0], slot) || !ReferenceEquals(grade[1], source[1]) ||
            !Span(grade[2], out var supportAxis, out var lo, out var hi) || !supportAxis.SameIdentityAs(axis) ||
            lo != 0 || new System.Numerics.BigInteger(hi) != ScaleUpper(context) ||
            !Fields(context.Fields[1], 2, out var metric) || !ReferenceEquals(grade[3], metric[0]) ||
            first.Right is not Placeholder { IsInverse: false } firstSlot || second.Right is not Placeholder { IsInverse: false } secondSlot ||
            new HashSet<Placeholder> { slot, firstSlot, secondSlot }.Count != 3 ||
            result[3] is not Relation output || !ReferenceEquals(output.Left, firstSlot) || !ReferenceEquals(output.Right, secondSlot) ||
            !GroupFields(w[3], out var remainder) || remainder.Length != 0) return false;
        prepared = new PreparedRead(context, source[1], axis, slot, result[1], recipes); return true;
    }

    private static bool SameCoordinates(IElement point, Scalar[] expected, MetricRead metric) =>
        Complete(point, metric, out var values) && values.SequenceEqual(expected);

    private static System.Numerics.BigInteger ScaleUpper(OffsetRead context)
    {
        var square = context.Distance * context.Distance / Dot(context.Normal, context.Normal, context.Metric);
        var lower = square.SquareRootFloor();
        return new Scalar(lower * lower) == square ? lower : lower + 1;
    }

    private static bool Subdivision(Carrier fine, Carrier source, long parts)
    {
        try { return fine.UnitShape.CompatibleWith(UnitShape.SubdivisionOf(source.UnitShape, parts)); }
        catch (OverflowException) { return false; }
    }

    private static bool Context(Relation request, out OffsetRead context)
    {
        context = null!;
        if (!Fields(request, 4, out var f) || f[0] is not Relation edge || !Metric(f[1], out var metric) ||
            !Complete(edge.Left, metric, out var a) || !Complete(edge.Right, metric, out var b) ||
            f[2] is not Relation { Left: Relation outlet } selection ||
            !UnitShape.TryReadOrthogonalSource(outlet, out _) || !ReferenceEquals(outlet.Left, edge) ||
            !Distance(selection.Right, metric, out _, out var distance) ||
            f[3] is not Relation side || !ReferenceEquals(side.Left, edge.Left) ||
            !Complete(side.Right, metric, out var sidePoint) ||
            !NormalDirection(Subtract(b, a), Subtract(sidePoint, a), metric, out var normal)) return false;
        context = new OffsetRead(f, edge, metric, a, b, distance, normal); return true;
    }

    // These are transient reads only. The requests, equations, recipes and
    // correlations above are the durable ordinary Gojo representation.
    private sealed record OffsetRead(IElement[] Fields, Relation Edge, MetricRead Metric,
        Scalar[] A, Scalar[] B, Scalar Distance, Scalar[] Normal);
    private sealed record PreparedRead(OffsetRead Context, IElement Whole, Carrier ScaleAxis,
        Placeholder ScaleSlot, IElement Equation, IElement[] Recipes);
}
