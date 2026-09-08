using System.Numerics;
using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected planar rotation reads grounded in a marked contour, shared radial
/// attachments and explicit metric pairings. No angle or rotation opcode is
/// stored. Traversal, geometric validation and point application stay separate.
/// </summary>
public static class ContourRotation
{
    /// <summary>
    /// Reads group(surface, start marker, signed step span). The surface is
    /// group(origin-to-centre, circular carrier, metric, radial bindings, cycle).
    /// Counts ordered edges; it does not claim equal angular spacing. Captures
    /// exposed numeric sites without traversing private carrier attachments.
    /// </summary>
    public static bool TryTraverse(Relation request, out Relation witness)
    {
        if (!Walk(request, out _, out _, out _, out _)) return Unresolved(request, out witness);
        var captured = (Relation)Capture(request);
        Walk(captured, out var surface, out var start, out var finish, out var laps);
        Fields(captured, 3, out var fields);
        Span(fields[2], out var stepAxis, out _, out _);
        try
        {
            var result = Group(start, finish, fields[2], new Scalar(laps).Encode(stepAxis));
            witness = Observe(request, surface, result, [], captured);
            return true;
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
    }

    /// <summary>
    /// Validates a captured traversal's planar metric and all radial landmarks.
    /// Success establishes a constant-reach contour with simple cyclic order,
    /// and returns group(metric, first radial relation, final radial relation).
    /// Missing geometry keeps the successful weaker traversal as evidence.
    /// </summary>
    public static bool TryTurn(Relation traversal, out Relation witness)
    {
        if (!Traversal(traversal, out var surface, out var start, out var finish) ||
            !Geometry(surface, out var geometry)) return Unresolved(traversal, out witness);
        Fields(surface, 5, out var fields);
        GroupFields(fields[3], out var bindings);
        var first = bindings.Cast<Relation>().Single(binding => SiteEqual(binding.Left, start));
        var last = bindings.Cast<Relation>().Single(binding => SiteEqual(binding.Left, finish));
        try
        {
            var result = Group(fields[2], first.Right, last.Right);
            // The squared reach is a derived metric read, never an angle.
            witness = Observe(traversal, surface, result, [], geometry.Reach.Encode(geometry.Square));
            return true;
        }
        catch (OverflowException) { return Unresolved(traversal, out witness); }
    }

    /// <summary>
    /// Reads group(traversal witness, exposed ordered points). Produces new
    /// point structure, source/result correspondences and reflection evidence.
    /// It does not mutate the input, inject material tension or rotate hidden
    /// internals. Repeated point references remain repeated references.
    /// </summary>
    public static bool TryApply(Relation request, out Relation witness)
    {
        if (!Fields(request, 2, out var fields) || fields[0] is not Relation traversal ||
            !TryTurn(traversal, out var turn) ||
            !Fields(turn, 5, out var turnFields) || !Fields(turnFields[2], 3, out var turnResult) ||
            !Metric(turnResult[0], out var metric) ||
            turnResult[1] is not Relation first || turnResult[2] is not Relation last ||
            !Coordinates(first.Left, metric, out var centre) || centre.Any(v => v is null) ||
            !Coordinates(first.Right, metric, out var firstPoint) || firstPoint.Any(v => v is null) ||
            !Coordinates(last.Right, metric, out var lastPoint) || lastPoint.Any(v => v is null) ||
            fields[1] is not Relation points || !GroupFields(points, out var items) || items.Length == 0)
            return Unresolved(request, out witness);

        var origin = centre.Select(v => v!.Value).ToArray();
        var a = Subtract(firstPoint.Select(v => v!.Value).ToArray(), origin);
        var b = Subtract(lastPoint.Select(v => v!.Value).ToArray(), origin);
        var n = Subtract(a, b);
        var reach = Dot(a, a, metric);
        var normalReach = Dot(n, n, metric);
        Scalar[] ReflectAlong(Scalar[] value) => Subtract(Scale(a, new Scalar(2) * Dot(a, value, metric) / reach), value);
        Scalar[] ReflectNormal(Scalar[] value) => Subtract(value, Scale(n, new Scalar(2) * Dot(n, value, metric) / normalReach));
        Scalar[] Turn(Scalar[] value) => normalReach.IsZero ? value : ReflectNormal(ReflectAlong(value));
        var columnX = Turn([Scalar.One, Scalar.Zero]);
        var columnY = Turn([Scalar.Zero, Scalar.One]);
        var result = new List<IElement>();
        var clauses = new List<IElement>();
        var reads = new List<IElement>();
        var remainder = new List<IElement>();
        var memo = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var captures = new CaptureState();
        try
        {
            foreach (var item in items)
            {
                if (memo.TryGetValue(item, out var previous)) { result.Add(previous); continue; }
                if (!Coordinates(item, metric, out var coordinates)) return Unresolved(request, out witness);
                var output = new IElement[2];
                for (var i = 0; i < 2; i++)
                {
                    var knownX = coordinates[0] is not null || columnX[i].IsZero;
                    var knownY = coordinates[1] is not null || columnY[i].IsZero;
                    output[i] = knownX && knownY
                        ? (origin[i] + columnX[i] * ((coordinates[0] ?? origin[0]) - origin[0]) +
                            columnY[i] * ((coordinates[1] ?? origin[1]) - origin[1])).Encode(metric.Axes[i])
                        : new Placeholder();
                }
                var mapped = Relation.OrderedGroup(metric.Holder, output);
                var captured = captures.Copy(item);
                IElement intermediate = new Placeholder();
                if (coordinates.All(v => v is not null))
                {
                    var relative = Subtract(coordinates.Select(v => v!.Value).ToArray(), origin);
                    intermediate = Vector(normalReach.IsZero ? relative : ReflectAlong(relative), metric);
                }
                var read = Observe(captured, turnFields[2], mapped,
                    output.Where(e => e is Placeholder), intermediate);
                if (output.Any(e => e is Placeholder)) remainder.Add(read);
                result.Add(mapped); memo.Add(item, mapped);
                clauses.Add(new Relation(item, mapped)); reads.Add(read);
            }
        }
        catch (OverflowException) { return Unresolved(request, out witness); }
        witness = Observe(request, turn, Relation.OrderedGroup((Carrier)points.Right, [.. result]),
            remainder, Group(Group([.. clauses]), Group([.. reads])));
        return remainder.Count == 0;
    }

    private static bool Walk(IElement request, out Relation surface, out IElement start,
        out IElement finish, out BigInteger laps)
    {
        surface = null!; start = finish = null!; laps = 0;
        if (!Fields(request, 3, out var fields) || fields[0] is not Relation candidate ||
            !Topology(candidate, out var markers) || !Span(fields[2], out _, out var a, out var b)) return false;
        var index = Array.FindIndex(markers, marker => SiteEqual(marker, fields[1]));
        if (index < 0) return false;
        var absolute = new BigInteger(index) + b - a;
        var q = BigInteger.DivRem(absolute, markers.Length, out var r);
        if (r.Sign < 0) { r += markers.Length; q--; }
        surface = candidate; start = markers[index]; finish = markers[(int)r]; laps = q;
        return true;
    }

    private static bool Traversal(IElement element, out Relation surface, out IElement start, out IElement finish)
    {
        surface = null!; start = finish = null!;
        return Fields(element, 5, out var witness) && Walk(witness[4], out surface, out start, out finish, out var laps) &&
            ReferenceEquals(witness[1], surface) && Fields(witness[2], 4, out var result) &&
            SiteEqual(result[0], start) && SiteEqual(result[1], finish) &&
            Fields(witness[4], 3, out var request) && ReferenceEquals(result[2], request[2]) &&
            Span(result[2], out var stepAxis, out _, out _) &&
            Number(result[3], out var countAxis, out var count) && countAxis.SameIdentityAs(stepAxis) &&
            count == new Scalar(laps) && GroupFields(witness[3], out var remainder) && remainder.Length == 0;
    }

    private static bool Topology(IElement element, out IElement[] markers)
    {
        markers = [];
        if (!Fields(element, 5, out var fields) || fields[0] is not Relation anchor ||
            fields[1] is not Carrier perimeter || !Concrete(perimeter) ||
            !perimeter.TryReadCircularOrigin(out var source) || !Point(anchor.Left, out var origin) ||
            !origin.Carrier.SameIdentityAs(source.Carrier) || origin.Ticks != source.Ticks ||
            !GroupFields(fields[3], out var bindings) || bindings.Length < 3 ||
            !GroupFields(fields[4], out var edges) || edges.Length != bindings.Length) return false;
        var sites = new List<IElement>();
        var coordinates = new HashSet<long>();
        foreach (var elementBinding in bindings)
        {
            if (elementBinding is not Relation binding || !Point(binding.Left, out var marker) ||
                !marker.Carrier.SameIdentityAs(perimeter) || !coordinates.Add(marker.Ticks) ||
                binding.Right is not Relation radial || !ReferenceEquals(radial.Left, anchor.Right)) return false;
            sites.Add(binding.Left);
        }
        for (var i = 0; i < sites.Count; i++)
            if (edges[i] is not Relation edge || !SiteEqual(edge.Left, sites[i]) ||
                !SiteEqual(edge.Right, sites[(i + 1) % sites.Count])) return false;
        markers = [.. sites]; return true;
    }

    private static bool Geometry(IElement surface, out GeometryRead geometry)
    {
        geometry = null!;
        if (!Topology(surface, out _) || !Fields(surface, 5, out var fields) ||
            !Metric(fields[2], out var metric) || fields[0] is not Relation anchor ||
            !Coordinates(anchor.Right, metric, out var c) || c.Any(v => v is null) ||
            !GroupFields(fields[3], out var bindings)) return false;
        var centre = c.Select(v => v!.Value).ToArray();
        var radii = new List<Scalar[]>();
        foreach (var binding in bindings.Cast<Relation>())
        {
            var radial = (Relation)binding.Right;
            if (!Coordinates(radial.Right, metric, out var p) || p.Any(v => v is null)) return false;
            radii.Add(Subtract(p.Select(v => v!.Value).ToArray(), centre));
        }
        var reach = Dot(radii[0], radii[0], metric);
        if (reach <= Scalar.Zero || radii.Any(r => Dot(r, r, metric) != reach)) return false;
        // Strict convex cyclic order rejects repeated poses, self-crossing stars
        // and scrambled landmarks. Either authored traversal direction is legal.
        var orientation = 0;
        for (var i = 0; i < radii.Count; i++)
        {
            var edge = Subtract(radii[(i + 1) % radii.Count], radii[i]);
            for (var j = 0; j < radii.Count; j++)
            {
                if (j == i || j == (i + 1) % radii.Count) continue;
                var other = Subtract(radii[j], radii[i]);
                var cross = edge[0] * other[1] - edge[1] * other[0];
                var sign = cross.Numerator.Sign;
                if (sign == 0 || orientation != 0 && sign != orientation) return false;
                orientation = sign;
            }
        }
        geometry = new GeometryRead(reach, metric.Square); return true;
    }

    private static bool Metric(IElement element, out MetricRead metric)
    {
        metric = null!;
        if (!Fields(element, 2, out var fields) || fields[0] is not Relation basis ||
            basis.Right is not Carrier holder || !Concrete(holder) ||
            !Fields(basis, 2, out var units) || !Span(units[0], out var x, out var x0, out var x1) ||
            !Span(units[1], out var y, out var y0, out var y1) ||
            x0 != 0 || x1 != 1 || y0 != 0 || y1 != 1 || x.SameIdentityAs(y) ||
            !x.UnitShape.CompatibleWith(y.UnitShape) || !Fields(fields[1], 3, out var pairings)) return false;
        var values = new Scalar[3];
        Carrier square = null!;
        for (var i = 0; i < 3; i++)
        {
            if (pairings[i] is not Relation pairing || pairing.Left is not Relation pair ||
                !ReferenceEquals(pair.Left, units[i == 2 ? 1 : 0]) ||
                !ReferenceEquals(pair.Right, units[i == 0 ? 0 : 1]) ||
                !Number(pairing.Right, out var valueAxis, out values[i]) ||
                !valueAxis.UnitShape.CompatibleWith(UnitShape.Product(x.UnitShape, x.UnitShape))) return false;
            if (i == 0) square = valueAxis;
            else if (!square.SameIdentityAs(valueAxis)) return false;
        }
        if (values[0] <= Scalar.Zero || values[0] * values[2] - values[1] * values[1] <= Scalar.Zero) return false;
        metric = new MetricRead(holder, [x, y], square, values); return true;
    }

    private static bool Coordinates(IElement point, MetricRead metric, out Scalar?[] values)
    {
        values = new Scalar?[2];
        if (point is Placeholder { IsInverse: false }) return true;
        if (point is not Relation relation || !ReferenceEquals(relation.Right, metric.Holder) ||
            !Fields(point, 2, out var coordinates)) return false;
        for (var i = 0; i < 2; i++)
        {
            if (coordinates[i] is Placeholder { IsInverse: false }) continue;
            if (!Number(coordinates[i], out var axis, out var value) || !axis.SameIdentityAs(metric.Axes[i])) return false;
            values[i] = value;
        }
        return true;
    }

    private static Scalar Dot(Scalar[] a, Scalar[] b, MetricRead metric) =>
        metric.Values[0] * a[0] * b[0] + metric.Values[1] * (a[0] * b[1] + a[1] * b[0]) + metric.Values[2] * a[1] * b[1];
    private static Scalar[] Subtract(Scalar[] a, Scalar[] b) => [a[0] - b[0], a[1] - b[1]];
    private static Scalar[] Scale(Scalar[] value, Scalar factor) => [value[0] * factor, value[1] * factor];
    private static Relation Vector(Scalar[] value, MetricRead metric) =>
        Relation.OrderedGroup(metric.Holder, value[0].Encode(metric.Axes[0]), value[1].Encode(metric.Axes[1]));
    private sealed record MetricRead(Carrier Holder, Carrier[] Axes, Carrier Square, Scalar[] Values);
    private sealed record GeometryRead(Scalar Reach, Carrier Square);

    private static bool Number(IElement element, out Carrier axis, out Scalar value)
    {
        if (Point(element, out var point)) element = point;
        if (ProjectionResidue.TryRead(element, out axis, out var n, out var d) && Concrete(axis))
        { value = new Scalar(n, d); return true; }
        value = Scalar.Zero; axis = null!; return false;
    }
    private static bool Point(IElement element, out CarrierLocation location)
    {
        if (element is Relation { Left: CarrierLocation p, Right: Carrier c } && p.Carrier.SameIdentityAs(c) && Concrete(c))
        { location = p; return true; }
        location = default; return false;
    }
    private static bool Span(IElement element, out Carrier axis, out long a, out long b)
    {
        if (element is Relation span && Point(span.Left, out var left) && Point(span.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        { axis = left.Carrier; a = left.Ticks; b = right.Ticks; return true; }
        axis = null!; a = b = 0; return false;
    }
    private static bool SiteEqual(IElement a, IElement b) =>
        Point(a, out var x) && Point(b, out var y) && x.Carrier.SameIdentityAs(y.Carrier) && x.SameTickRefAs(y);
    private static bool Concrete(Carrier carrier) => !carrier.IsUnknown && !carrier.IsInverse;
    private static bool GroupFields(IElement element, out IElement[] fields)
    {
        if (element is Relation relation && relation.TryReadOrderedGroup(out fields)) return true;
        fields = []; return false;
    }
    private static bool Fields(IElement element, int count, out IElement[] fields) =>
        GroupFields(element, out fields) && fields.Length == count;
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static Relation Observe(IElement source, IElement surface, IElement result, IEnumerable<IElement> remainder, IElement provenance) =>
        ReadWitness.Create(new Carrier(), source, surface, result, Group([.. remainder]), provenance);
    private static bool Unresolved(Relation request, out Relation witness)
    { witness = Observe(request, request, new Placeholder(), [request], request); return false; }
    private static IElement Capture(IElement element) => new CaptureState().Copy(element);
    private sealed class CaptureState
    {
        private readonly Dictionary<Relation, IElement> relations = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<TickRef, TickRef> ticks = new(ReferenceEqualityComparer.Instance);
        internal IElement Copy(IElement element)
        {
            if (element is CarrierLocation location)
            {
                if (!ticks.TryGetValue(location.Tick, out var tick)) ticks.Add(location.Tick, tick = new TickRef(location.Ticks));
                return new CarrierLocation(location.Carrier, tick);
            }
            if (element is not Relation relation) return element;
            if (relations.TryGetValue(relation, out var previous)) return previous;
            var result = new Relation(Copy(relation.Left), Copy(relation.Right));
            relations.Add(relation, result); return result;
        }
    }
}
