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

    /// <summary>Joins two graphic relations as ordinary inspectable structure.</summary>
    public static Relation Pin(Relation first, Relation second) =>
        new(first, second);

    /// <summary>Creates a linear carrier whose origin is a single shared site.</summary>
    public static Carrier LinearCarrierAt(Carrier carrier, TickRef site) =>
        LinearCarrierAt(Point(carrier, site));

    /// <summary>Creates a linear carrier whose origin is a point relation.</summary>
    public static Carrier LinearCarrierAt(Relation point) =>
        new(UnitShape.LinearAt(point));

    /// <summary>Creates a circular carrier whose origin is a single shared site.</summary>
    public static Carrier CircularCarrierAt(Carrier carrier, TickRef site) =>
        new(UnitShape.CircularFrom(Point(carrier, site)));

    /// <summary>Creates a circular carrier whose origin is a point relation.</summary>
    public static Carrier CircularCarrierAt(Relation point) =>
        new(UnitShape.CircularFrom(point));

    /// <summary>Creates a coordinate tuple point in a shared frame/group basis.</summary>
    public static Relation FramePoint(Carrier frame, params CarrierLocation[] coordinates) =>
        Relation.OrderedGroup(frame, (coordinates ?? []).Cast<IElement>().ToArray());

    /// <summary>Creates a segment between two coordinate tuple points.</summary>
    public static Relation FrameSegment(Relation start, Relation end) =>
        new(start, end);

    /// <summary>
    /// Creates a directed segment whose start is on the aligned inverse face
    /// and whose end is on the positive face: start*i + end.
    /// </summary>
    public static Relation DirectedSegment(Carrier carrier, TickRef inverseStart, TickRef positiveEnd)
    {
        var forward = carrier ?? Carrier.Unknown;
        var inverse = forward.IsUnknown ? Carrier.Unknown : forward.Inverse();
        return new(Point(inverse, inverseStart), Point(forward, positiveEnd));
    }

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
