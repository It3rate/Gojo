namespace Gojo.Core;

/// <summary>
/// Primitive ordered Gojo structure. A relation can be a point, segment, port,
/// operation surface, route, or nested structure depending on its elements;
/// behavior is derived from that relation shape rather than from operation
/// tags.
/// </summary>
public sealed partial class Relation : IElement, IEquatable<Relation>
{
    private static ulong idCounter = 1;

    /// <summary>Creates an ordered relation from left element to right element.</summary>
    public Relation(IElement left, IElement right)
    {
        Id = NextId();
        Left = left;
        Right = right;
    }

    /// <summary>Creates a point by grounding a carrier location on its carrier.</summary>
    public static Relation Point(Carrier carrier, long ticks) =>
        new(new CarrierLocation(carrier, ticks), carrier);

    /// <summary>Creates a directed segment from two carrier-grounded points.</summary>
    public static Relation Segment(Carrier carrier, long start, long end) =>
        new(Point(carrier, start), Point(carrier, end));

    /// <summary>
    /// Creates a directed endpoint segment whose start is opposed-flow
    /// participation and whose end is positive-flow participation.
    /// </summary>
    public static Relation DirectedSegment(Carrier carrier, long inverseStart, long positiveEnd)
    {
        var forward = carrier ?? Carrier.Unknown;
        var inverse = forward.IsUnknown ? Carrier.Unknown : forward.Inverse();
        return new(Point(inverse, inverseStart), Point(forward, positiveEnd));
    }

    /// <summary>Creates the uncalibrated orthogonal outlet surface for a source.</summary>
    public static Relation OrthogonalOutlet(IElement source) =>
        new(source ?? throw new ArgumentNullException(nameof(source)), Segment(Carrier.Unknown, 0, 0));

    /// <summary>Creates a live read from an input structure into a concrete output port.</summary>
    public static Relation LiveOutput(IElement input, Relation outputPort) =>
        new(input, outputPort);

    /// <summary>Creates additive participation from a holder origin to a participant.</summary>
    public static Relation AdditiveAttachment(Carrier holder, IElement participant) =>
        new(Point(holder, 0), participant);

    /// <summary>Creates an ordered additive group anchored by a holder carrier.</summary>
    public static Relation AdditiveGroup(Carrier holder, params IElement[] participants) =>
        new(ChainAdditiveAttachments(holder, participants), holder);

    /// <summary>Creates an ordered group anchored by a holder carrier.</summary>
    public static Relation OrderedGroup(Carrier holder, params IElement[] elements) =>
        AdditiveGroup(holder, elements);

    /// <summary>Creates a fill basis carrier without measuring this structure.</summary>
    public Carrier CreateFillBasis() => CreateFillBasis(inverse: false);

    /// <summary>Creates an inverse fill basis carrier without measuring this structure.</summary>
    public Carrier CreateInverseFillBasis() => CreateFillBasis(inverse: true);

    public ulong Id { get; }

    public IElement Left { get; private set; }

    public IElement Right { get; private set; }

}
