using Gojo.Core;
using Gojo.Reads;

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

    /// <summary>Grounds an independent site on a carrier as a Gojo point relation.</summary>
    public static Relation Point(Carrier carrier, long ticks) =>
        Relation.Point(carrier, ticks);

    /// <summary>Creates an ordinary positive-face segment between two shared sites.</summary>
    public static Relation Segment(Carrier carrier, TickRef start, TickRef end) =>
        new(Point(carrier, start), Point(carrier, end));

    /// <summary>Creates an ordinary positive-face segment between independent sites.</summary>
    public static Relation Segment(Carrier carrier, long start, long end) =>
        Relation.Segment(carrier, start, end);

    /// <summary>
    /// Creates a directed segment whose start is on the aligned inverse face
    /// and whose end is on the positive face: start*i + end.
    /// </summary>
    public static Relation DirectedSegment(Carrier carrier, long inverseStart, long positiveEnd) =>
        Relation.DirectedSegment(carrier, inverseStart, positiveEnd);

    /// <summary>Creates radial flow potential from a point without measuring radius.</summary>
    public static Relation RadialFlow(Relation point) =>
        Relation.OrthogonalOutlet(point);

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

    /// <summary>Reads a positive-face segment between two point relations.</summary>
    public static bool TryReadSegment(
        Relation segment,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (segment.Left is Relation startPoint &&
            segment.Right is Relation endPoint &&
            TryReadPoint(startPoint, out start) &&
            TryReadPoint(endPoint, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier) &&
            !start.Carrier.IsInverse &&
            !end.Carrier.IsInverse)
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    /// <summary>Reads a directed segment as inverse-face start and positive-face end.</summary>
    public static bool TryReadDirectedSegment(
        Relation directedSegment,
        out CarrierLocation inverseStart,
        out CarrierLocation positiveEnd) =>
        directedSegment.TryReadDirectedSegment(out inverseStart, out positiveEnd);

    /// <summary>Reads radial flow as an orthogonal outlet from a point.</summary>
    public static bool TryReadRadialFlow(Relation radialFlow, out Relation point)
    {
        if (radialFlow.Left is Relation sourcePoint &&
            TryReadPoint(sourcePoint, out _) &&
            radialFlow.Right is Relation outlet &&
            TryReadSegment(outlet, out var start, out var end) &&
            start.Carrier.IsUnknown &&
            end.Carrier.IsUnknown &&
            start.Ticks == 0 &&
            end.Ticks == 0)
        {
            point = sourcePoint;
            return true;
        }

        point = null!;
        return false;
    }
}
