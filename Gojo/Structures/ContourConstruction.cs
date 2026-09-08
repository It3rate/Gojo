using System.Numerics;
using Gojo.Core;
using Scalar = Gojo.Core.ExactScalar;

namespace Gojo.Structures;

/// <summary>
/// Selected intrinsic reads for a point-derived constant-offset contour, its
/// radial reference, closure and equal subdivision. No coordinate grid, angle
/// constant or trigonometric sample participates in the construction.
/// </summary>
public static class ContourConstruction
{
    /// <summary>
    /// Reads O(point) -> (common reach -> contour carrier). An open reach is a
    /// valid uncalibrated level-path pattern and remains in the remainder.
    /// This selected planar contract does not turn every outlet into a circle.
    /// </summary>
    public static bool TryReadOffset(Relation offset, out Relation witness)
    {
        if (!Offset(offset, out _, out _, out _)) return Unresolved(offset, out witness);
        var captured = Capture(offset);
        Offset(captured, out _, out var reach, out _);
        witness = Observe(offset, captured, captured, reach is Placeholder ? [reach] : [], captured);
        return true;
    }

    /// <summary>
    /// Reads group(offset, radial attachment, whole span, closing relation).
    /// A radius supplies the shared reach and an actual starting site. The
    /// positive 0..1 whole is one unwrapped traversal, not a length in radius units.
    /// </summary>
    public static bool TryReadCircle(Relation circle, out Relation witness)
    {
        if (!Circle(circle, out _)) return Unresolved(circle, out witness);
        var captured = Capture(circle);
        witness = Observe(circle, captured, captured, [], captured);
        return true;
    }

    /// <summary>
    /// Reads group(circle, subdivided whole -> circle whole). The fine carrier
    /// must retain the corresponding subdivision unit promise as well as this
    /// explicit connection to the actual whole. A marker count alone is insufficient.
    /// </summary>
    public static bool TryReadPartition(Relation partition, out Relation witness)
    {
        if (!Partition(partition, out _, out _, out _)) return Unresolved(partition, out witness);
        var captured = Capture(partition);
        witness = Observe(partition, captured, captured, [], captured);
        return true;
    }

    /// <summary>
    /// Reads group(partition, selected subdivision point). Returns a native
    /// marker, an equal-reach radial attachment, wrapped whole participation
    /// and signed completed traversals. The return site is shared at the seam.
    /// Numeric sites are captured; creating this read does not move material.
    /// </summary>
    public static bool TryLocate(Relation request, out Relation witness)
    {
        if (!Fields(request, 2, out var live) || !Partition(live[0], out _, out var liveAxis, out _) ||
            !Point(live[1], out var selected) || !selected.Carrier.SameIdentityAs(liveAxis))
            return Unresolved(request, out witness);
        var captured = Capture(request);
        Fields(captured, 2, out var fields);
        Partition(fields[0], out var circle, out var axis, out var parts);
        Point(fields[1], out var point);
        var cycles = BigInteger.DivRem(new BigInteger(point.Ticks), parts, out var remainder);
        if (remainder.Sign < 0) { remainder += parts; cycles--; }
        IElement marker;
        Relation radial;
        if (remainder.IsZero)
        {
            marker = circle.Start;
            radial = circle.Radial;
        }
        else
        {
            marker = point.Ticks == (long)remainder ? fields[1] : Relation.Point(axis, (long)remainder);
            Span(circle.Radius, out var originalRay, out _, out var reach);
            var ray = new Carrier(originalRay.UnitShape);
            var start = Relation.Point(ray, 0);
            var end = Relation.Point(ray, reach);
            radial = Group(new Relation(circle.Source, start), new Relation(start, end), new Relation(end, marker));
        }
        Fields(radial, 3, out var radialFields);
        var reachCorrespondence = new Relation(circle.Radius, radialFields[1]);
        // Whole participation is exact, and is deliberately not arc length.
        var result = Group(marker, radial, new Scalar(remainder, parts).Encode(circle.Contour),
            new Scalar(cycles).Encode(circle.Contour));
        witness = Observe(request, fields[0], result, [], Group(captured, reachCorrespondence));
        return true;
    }

    private static bool Offset(IElement element, out Relation source, out IElement reach, out Carrier contour)
    {
        source = null!; reach = null!; contour = null!;
        if (element is not Relation { Left: Relation outlet, Right: Relation level } ||
            outlet.Left is not Relation origin || !Point(origin, out var p) ||
            !UnknownSpan(outlet.Right) || level.Right is not Carrier c || !Concrete(c) ||
            !c.TryReadCircularOrigin(out var native) || !SameOrigin(p, native)) return false;
        if (level.Left is not Placeholder { IsInverse: false } &&
            (!Span(level.Left, out var ray, out var a, out var b) || a != 0 || b <= 0 ||
             !ray.TryReadLinearOrigin(out var radialOrigin) || !SameOrigin(p, radialOrigin))) return false;
        source = origin; reach = level.Left; contour = c; return true;
    }

    private static bool Circle(IElement element, out CircleRead circle)
    {
        circle = null!;
        if (!Fields(element, 4, out var fields) || !Offset(fields[0], out var source, out var reach, out var contour) ||
            reach is not Relation radius || !Span(radius, out _, out _, out _) ||
            !Fields(fields[1], 3, out var radial) || !ReferenceEquals(radial[1], radius) ||
            radial[0] is not Relation root || !SameSite(root.Left, source) || !SameSite(root.Right, radius.Left) ||
            radial[2] is not Relation attachment || !SameSite(attachment.Left, radius.Right) ||
            fields[2] is not Relation whole || !Span(whole, out var wholeAxis, out var start, out var end) ||
            !wholeAxis.SameIdentityAs(contour) || start != 0 || end != 1 || !SameSite(attachment.Right, whole.Left) ||
            fields[3] is not Relation close || !SameSite(close.Left, whole.Right) || !SameSite(close.Right, whole.Left))
            return false;
        circle = new CircleRead(source, contour, radius, (Relation)fields[1], whole.Left, whole);
        return true;
    }

    private static bool Partition(IElement element, out CircleRead circle, out Carrier axis, out long parts)
    {
        circle = null!; axis = null!; parts = 0;
        if (!Fields(element, 2, out var fields) || !Circle(fields[0], out circle) ||
            fields[1] is not Relation map || !ReferenceEquals(map.Right, circle.Whole) ||
            !Span(map.Left, out axis, out var start, out parts) || start != 0 || parts <= 0 ||
            axis.SameIdentityAs(circle.Contour) ||
            !axis.UnitShape.CompatibleWith(UnitShape.SubdivisionOf(circle.Contour.UnitShape, parts))) return false;
        return true;
    }

    private sealed record CircleRead(Relation Source, Carrier Contour, Relation Radius, Relation Radial, IElement Start, Relation Whole);
    private static bool SameOrigin(CarrierLocation a, CarrierLocation b) => a.Carrier.SameIdentityAs(b.Carrier) && a.Ticks == b.Ticks;
    private static bool SameSite(IElement a, IElement b) => Point(a, out var x) && Point(b, out var y) &&
        x.Carrier.SameIdentityAs(y.Carrier) && x.SameTickRefAs(y);
    private static bool Concrete(Carrier c) => !c.IsUnknown && !c.IsInverse;
    private static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is Relation { Left: CarrierLocation location, Right: Carrier c } &&
            Concrete(c) && c.SameIdentityAs(location.Carrier)) { point = location; return true; }
        point = default; return false;
    }
    private static bool Span(IElement element, out Carrier axis, out long start, out long end)
    {
        if (element is Relation r && Point(r.Left, out var a) && Point(r.Right, out var b) && a.Carrier.SameIdentityAs(b.Carrier))
        { axis = a.Carrier; start = a.Ticks; end = b.Ticks; return true; }
        axis = null!; start = end = 0; return false;
    }
    private static bool UnknownSpan(IElement element) =>
        element is Relation { Left: Relation { Left: CarrierLocation a, Right: Carrier ca },
            Right: Relation { Left: CarrierLocation b, Right: Carrier cb } } &&
        a.Carrier.IsUnknown && b.Carrier.IsUnknown && ca.IsUnknown && cb.IsUnknown && a.Ticks == 0 && b.Ticks == 0;
    private static bool Fields(IElement element, int count, out IElement[] fields)
    {
        if (element is Relation r && r.TryReadOrderedGroup(out fields) && fields.Length == count) return true;
        fields = []; return false;
    }
    private static Relation Capture(Relation r) => (Relation)new ExposedStructureSnapshot().Copy(r);
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static Relation Observe(IElement source, IElement surface, IElement result, IElement[] remainder, IElement provenance) =>
        ReadWitness.Create(new Carrier(), source, surface, result, Group(remainder), provenance);
    private static bool Unresolved(Relation request, out Relation witness)
    { witness = Observe(request, request, new Placeholder(), [request], request); return false; }
}
