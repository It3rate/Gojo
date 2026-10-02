using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

// Shared exact reads for the selected flat positive-definite planar metric.
// This is derived arithmetic; the basis and pairings remain ordinary structure.
internal static class PlanarMetric
{
    internal static bool Complete(IElement point, MetricRead metric, out Scalar[] coordinates)
    {
        coordinates = [];
        if (!Coordinates(point, metric, out var values) || values.Any(v => v is null)) return false;
        coordinates = values.Select(v => v!.Value).ToArray(); return true;
    }

    internal static Scalar[] Add(Scalar[] a, Scalar[] b) => [a[0] + b[0], a[1] + b[1]];
    internal static Scalar Cross(Scalar[] a, Scalar[] b) => a[0] * b[1] - a[1] * b[0];

    // A relative guide selects one of the two metric-normal directions. Its
    // tangent component has no effect. This does not assign a screen direction.
    internal static bool Normal(Scalar[] direction, Scalar[] guide, Scalar distance,
        MetricRead metric, out Scalar[] normal)
    {
        normal = [];
        if (distance < Scalar.Zero || !NormalDirection(direction, guide, metric, out var candidate)) return false;
        if (!(distance * distance / Dot(candidate, candidate, metric)).TrySquareRoot(out var scale)) return false;
        normal = Scale(candidate, scale); return true;
    }

    // Keep this readable before normalization: a metric-normal direction may
    // be exact even when its requested length needs an irrational scale.
    internal static bool NormalDirection(Scalar[] direction, Scalar[] guide, MetricRead metric, out Scalar[] normal)
    {
        normal = [];
        if (Dot(direction, direction, metric) <= Scalar.Zero) return false;
        Scalar[] candidate = [-(metric.Values[1] * direction[0] + metric.Values[2] * direction[1]),
            metric.Values[0] * direction[0] + metric.Values[1] * direction[1]];
        var side = Dot(candidate, guide, metric);
        if (side.IsZero) return false;
        normal = side < Scalar.Zero ? Scale(candidate, new Scalar(-1)) : candidate; return true;
    }

    // The chosen native reach is mapped explicitly to a nonnegative magnitude
    // in the metric's common linear unit. It is not an implicit unit conversion.
    internal static bool Distance(IElement element, MetricRead metric, out Relation reach, out Scalar distance)
    {
        reach = null!; distance = Scalar.Zero;
        if (element is not Relation { Left: Relation native } calibration ||
            !Span(native, out _, out var a, out var b) || a != 0 || b <= 0 ||
            !Span(calibration.Right, out var length, out var c, out var d) || c != 0 || d < 0 ||
            !length.UnitShape.CompatibleWith(metric.Axes[0].UnitShape)) return false;
        reach = native; distance = new Scalar(d); return true;
    }

    internal static bool Metric(IElement element, out MetricRead metric)
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

    internal static bool Coordinates(IElement point, MetricRead metric, out Scalar?[] values)
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

    internal static Scalar Dot(Scalar[] a, Scalar[] b, MetricRead metric) =>
        metric.Values[0] * a[0] * b[0] + metric.Values[1] * (a[0] * b[1] + a[1] * b[0]) + metric.Values[2] * a[1] * b[1];
    internal static Scalar[] Subtract(Scalar[] a, Scalar[] b) => [a[0] - b[0], a[1] - b[1]];
    internal static Scalar[] Scale(Scalar[] value, Scalar factor) => [value[0] * factor, value[1] * factor];
    internal static Relation Vector(Scalar[] value, MetricRead metric) =>
        Relation.OrderedGroup(metric.Holder, value[0].Encode(metric.Axes[0]), value[1].Encode(metric.Axes[1]));
    internal sealed record MetricRead(Carrier Holder, Carrier[] Axes, Carrier Square, Scalar[] Values);

    internal static bool Number(IElement element, out Carrier axis, out Scalar value)
    {
        if (Point(element, out var point)) element = point;
        if (ProjectionResidue.TryRead(element, out axis, out var n, out var d) && Concrete(axis))
        { value = new Scalar(n, d); return true; }
        value = Scalar.Zero; axis = null!; return false;
    }
    internal static bool Point(IElement element, out CarrierLocation location)
    {
        if (element is Relation { Left: CarrierLocation p, Right: Carrier c } && p.Carrier.SameIdentityAs(c) && Concrete(c))
        { location = p; return true; }
        location = default; return false;
    }
    internal static bool Span(IElement element, out Carrier axis, out long a, out long b)
    {
        if (element is Relation span && Point(span.Left, out var left) && Point(span.Right, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        { axis = left.Carrier; a = left.Ticks; b = right.Ticks; return true; }
        axis = null!; a = b = 0; return false;
    }
    internal static bool Concrete(Carrier carrier) => !carrier.IsUnknown && !carrier.IsInverse;
    internal static bool GroupFields(IElement element, out IElement[] fields)
    {
        if (element is Relation relation && relation.TryReadOrderedGroup(out fields)) return true;
        fields = []; return false;
    }
    internal static bool Fields(IElement element, int count, out IElement[] fields) =>
        GroupFields(element, out fields) && fields.Length == count;
}
