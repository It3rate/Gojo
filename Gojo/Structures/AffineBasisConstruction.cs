using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// A selected affine product construction from an ordered native segment and
/// a line selected through its orthogonal outlet, with connected origins.
/// This declares independent coordinate roles; it does not discover physical
/// independence, impose a metric, or select a drawing orientation.
/// </summary>
public static class AffineBasisConstruction
{
    /// <summary>
    /// Reads group(first span, O(first span) -> second span, first start -> second start).
    /// Captures that foundation, creates one coordinate tick per whole native span,
    /// and returns group(captured foundation, coordinate basis, native calibrations).
    /// Missing legs remain valid open construction patterns without a complete basis.
    /// </summary>
    public static bool TryCrystallize(Relation construction, out Relation witness)
    {
        if (!Foundation(construction, out _, out _)) return Unresolved(construction, out witness);
        var captured = (Relation)new ExposedStructureSnapshot().Copy(construction);
        Foundation(captured, out var first, out var second);
        var u = new Carrier(UnitShape.DescribedAtom(first));
        var v = new Carrier(UnitShape.DescribedAtom(second));
        var uWhole = Relation.Segment(u, 0, 1);
        var vWhole = Relation.Segment(v, 0, 1);
        var coordinates = Relation.OrderedGroup(new Carrier(), uWhole, vWhole);
        var calibrations = Group(new Relation(first, uWhole), new Relation(second, vWhole));
        var result = Group(captured, coordinates, calibrations);
        witness = Observe(construction, captured, result, [], calibrations);
        return true;
    }

    /// <summary>
    /// Reads group(crystallized basis, ordered native coordinate observations).
    /// A native coordinate is a location, exact fraction, or open slot; a whole
    /// point may be open. Captures numeric observations before projection.
    /// No target point, carrier value or tension is mutated.
    /// </summary>
    public static bool TryLocate(Relation request, out Relation witness)
    {
        if (!Fields(request, 2, out var fields) ||
            !TryRead(fields[0], out _, out _)) return Unresolved(request, out witness);
        var captured = (Relation)new ExposedStructureSnapshot().Copy(request);
        Fields(captured, 2, out fields);
        TryRead(fields[0], out var coordinates, out var calibrations);
        var complete = AffineFrame.TryProject(Group(fields[1], calibrations, coordinates), out var projection);
        projection.TryReadOrderedGroup(out var read);
        witness = Observe(request, fields[0], read[2], FieldsOf(read[3]), Group(captured, projection));
        return complete;
    }

    /// <summary>
    /// Validates the complete captured construction and its exact calibration
    /// connections. This surface can be used directly as an AffineFrame basis.
    /// A descriptor alone or a pair of unrelated carriers is insufficient.
    /// </summary>
    internal static bool TryRead(IElement element, out Relation coordinates, out Relation calibrations)
    {
        coordinates = calibrations = null!;
        if (!Fields(element, 3, out var fields) || !Foundation(fields[0], out var first, out var second) ||
            fields[1] is not Relation { Right: Carrier holder } basis || !Concrete(holder) ||
            !Fields(basis, 2, out var units) || !Fields(fields[2], 2, out var maps)) return false;
        var legs = new[] { first, second };
        var axes = new Carrier[2];
        for (var i = 0; i < 2; i++)
        {
            if (!Span(units[i], out axes[i], out var start, out var end) || start.Ticks != 0 || end.Ticks != 1 ||
                !axes[i].UnitShape.Equals(UnitShape.DescribedAtom(legs[i])) ||
                maps[i] is not Relation map || !ReferenceEquals(map.Left, legs[i]) ||
                !ReferenceEquals(map.Right, units[i])) return false;
        }
        if (axes[0].SameIdentityAs(axes[1])) return false;
        coordinates = basis; calibrations = (Relation)fields[2]; return true;
    }

    private static bool Foundation(IElement element, out Relation first, out Relation second)
    {
        first = second = null!;
        if (!Fields(element, 3, out var f) || f[0] is not Relation a ||
            !Span(a, out var firstAxis, out var aStart, out var aEnd) || aStart.Ticks == aEnd.Ticks ||
            f[1] is not Relation { Left: Relation outlet, Right: Relation b } ||
            !UnitShape.TryReadOrthogonalSource(outlet, out _) || !ReferenceEquals(outlet.Left, a) ||
            !Span(b, out var secondAxis, out var bStart, out var bEnd) || bStart.Ticks == bEnd.Ticks ||
            firstAxis.SameIdentityAs(secondAxis) ||
            !secondAxis.TryReadLinearOrigin(out var origin) ||
            !origin.Carrier.SameIdentityAs(aStart.Carrier) || origin.Ticks != aStart.Ticks ||
            bStart.Ticks != 0 ||
            f[2] is not Relation attachment || !SameSite(attachment.Left, a.Left) ||
            !SameSite(attachment.Right, b.Left)) return false;
        first = a; second = b; return true;
    }

    private static bool Span(IElement element, out Carrier axis, out CarrierLocation a, out CarrierLocation b)
    {
        if (element is Relation span && Point(span.Left, out a) && Point(span.Right, out b) &&
            a.Carrier.SameIdentityAs(b.Carrier)) { axis = a.Carrier; return true; }
        axis = null!; a = b = default; return false;
    }
    private static bool Point(IElement element, out CarrierLocation point)
    {
        if (element is Relation { Left: CarrierLocation p, Right: Carrier c } &&
            Concrete(c) && c.SameIdentityAs(p.Carrier)) { point = p; return true; }
        point = default; return false;
    }
    private static bool SameSite(IElement a, IElement b) => Point(a, out var x) && Point(b, out var y) &&
        x.Carrier.SameIdentityAs(y.Carrier) && x.SameTickRefAs(y);
    private static bool Concrete(Carrier c) => !c.IsUnknown && !c.IsInverse;
    private static bool Fields(IElement element, int count, out IElement[] fields)
    {
        if (element is Relation relation && relation.TryReadOrderedGroup(out fields) && fields.Length == count) return true;
        fields = []; return false;
    }
    private static IElement[] FieldsOf(IElement element) =>
        element is Relation relation && relation.TryReadOrderedGroup(out var fields) ? fields : [element];
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static Relation Observe(IElement source, IElement surface, IElement result, IElement[] remainder, IElement provenance) =>
        ReadWitness.Create(new Carrier(), source, surface, result, Group(remainder), provenance);
    private static bool Unresolved(Relation request, out Relation witness)
    { witness = Observe(request, request, new Placeholder(), [request], request); return false; }
}
