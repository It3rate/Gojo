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
}
