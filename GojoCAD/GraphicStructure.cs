using Gojo.Core;

namespace GojoCAD;

/// <summary>
/// Construction and local-read helpers for Gojo-native graphic format
/// structure. The helpers return ordinary carriers and relations; they do not
/// own graphic meaning or render anything.
/// </summary>
public static class GraphicStructure
{
    /// <summary>Creates a coordinate reference that can physically join sites.</summary>
    public static TickRef Site(long ticks = 0) => new(ticks);

    /// <summary>Grounds a shared site on a carrier as a Gojo point relation.</summary>
    public static Relation Point(Carrier carrier, TickRef site) =>
        new(new CarrierLocation(carrier, site), carrier);

    /// <summary>Creates a directed graphic span between two shared sites.</summary>
    public static Relation DirectedSpan(Carrier carrier, TickRef start, TickRef end) =>
        new(Point(carrier, start), Point(carrier, end));

    /// <summary>Creates a directed graphic span with independent endpoint sites.</summary>
    public static Relation DirectedSpan(Carrier carrier, long start, long end) =>
        Relation.Segment(carrier, start, end);

    /// <summary>Gathers ordered spans into one polyline surface.</summary>
    public static Relation Polyline(Carrier holder, params Relation[] spans) =>
        Relation.OrderedGroup(holder, spans);

    /// <summary>
    /// Gathers a junction point and connected spans into a branch surface. The
    /// actual branch meaning still comes from shared sites on the spans.
    /// </summary>
    public static Relation Branch(Carrier holder, Relation junction, params Relation[] spans)
    {
        var participants = new IElement[spans.Length + 1];
        participants[0] = junction;
        Array.Copy(spans, 0, participants, 1, spans.Length);
        return Relation.OrderedGroup(holder, participants);
    }

    /// <summary>
    /// Records an unsolved curve transition as selected span plus tangent and
    /// parameter evidence. Sampling points is intentionally left to later reads.
    /// </summary>
    public static Relation CurveTransition(
        Carrier holder,
        Relation selectedSpan,
        Relation startTangent,
        Relation endTangent,
        Relation parameterSurface,
        IElement? residue = null) =>
        Relation.OrderedGroup(
            holder,
            selectedSpan,
            startTangent,
            endTangent,
            parameterSurface,
            residue ?? Carrier.Unknown);

    /// <summary>
    /// Creates a clothoid-like transition surface by relating path length to
    /// curvature change. This defines the format evidence without calculating
    /// the curve.
    /// </summary>
    public static Relation ClothoidLikeTransition(
        Carrier holder,
        Relation selectedSpan,
        Carrier length,
        Carrier curvature,
        long lengthTicks,
        long curvatureTicks)
    {
        var lengthSpan = Relation.Segment(length, 0, lengthTicks);
        var curvatureSpan = Relation.Segment(curvature, 0, curvatureTicks);
        var lengthToCurvature = new Relation(lengthSpan, curvatureSpan);
        return Relation.OrderedGroup(holder, selectedSpan, lengthToCurvature);
    }

    /// <summary>Reads a Gojo point relation into its grounded location.</summary>
    public static bool TryReadPoint(Relation point, out CarrierLocation location)
    {
        if (point.Left is CarrierLocation candidate &&
            point.Right is Carrier carrier &&
            candidate.Carrier.SameIdentityAs(carrier))
        {
            location = candidate;
            return true;
        }

        location = default;
        return false;
    }

    /// <summary>Reads a relation as a directed span between two point relations.</summary>
    public static bool TryReadDirectedSpan(
        Relation span,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (span.Left is Relation startPoint &&
            span.Right is Relation endPoint &&
            TryReadPoint(startPoint, out start) &&
            TryReadPoint(endPoint, out end))
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    /// <summary>Reads ordered directed spans from a polyline surface.</summary>
    public static bool TryReadPolyline(
        Relation polyline,
        Carrier holder,
        out Relation[] spans)
    {
        if (!polyline.TryReadOrderedElementsOn(holder, out var elements) ||
            elements.Length == 0)
        {
            spans = [];
            return false;
        }

        var result = new List<Relation>();
        foreach (var element in elements)
        {
            if (element is not Relation span ||
                !TryReadDirectedSpan(span, out _, out _))
            {
                spans = [];
                return false;
            }

            result.Add(span);
        }

        spans = [.. result];
        return true;
    }

    /// <summary>Reads whether the last endpoint returns to the first endpoint.</summary>
    public static bool TryReadClosedPolyline(
        Relation polyline,
        Carrier holder,
        out Relation[] spans)
    {
        if (!TryReadPolyline(polyline, holder, out spans))
        {
            return false;
        }

        TryReadDirectedSpan(spans[0], out var firstStart, out _);
        TryReadDirectedSpan(spans[^1], out _, out var lastEnd);
        return ShareGroundedSite(firstStart, lastEnd);
    }

    /// <summary>Reads a branch surface whose spans all touch the junction site.</summary>
    public static bool TryReadBranch(
        Relation branch,
        Carrier holder,
        out CarrierLocation junction,
        out Relation[] spans)
    {
        if (!branch.TryReadOrderedElementsOn(holder, out var elements) ||
            elements.Length < 2 ||
            elements[0] is not Relation junctionPoint ||
            !TryReadPoint(junctionPoint, out junction))
        {
            junction = default;
            spans = [];
            return false;
        }

        var result = new List<Relation>();
        foreach (var element in elements.Skip(1))
        {
            if (element is not Relation span ||
                !TryReadDirectedSpan(span, out var start, out var end) ||
                (!ShareSite(junction, start) && !ShareSite(junction, end)))
            {
                spans = [];
                return false;
            }

            result.Add(span);
        }

        spans = [.. result];
        return true;
    }

    /// <summary>Reads whether two grounded locations share one physical site ref.</summary>
    public static bool ShareSite(CarrierLocation left, CarrierLocation right) =>
        left.SameTickRefAs(right);

    /// <summary>
    /// Reads whether two grounded locations share both site ref and carrier
    /// face. This is useful for closed paths on one contour carrier.
    /// </summary>
    public static bool ShareGroundedSite(CarrierLocation left, CarrierLocation right) =>
        left.Carrier.SameIdentityAs(right.Carrier) && left.SameTickRefAs(right);
}
