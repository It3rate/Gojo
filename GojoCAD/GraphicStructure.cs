using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

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

    /// <summary>Creates a circular arc as a span on a circular carrier from an origin point.</summary>
    public static Relation CircularArc(Relation origin, TickRef start, TickRef end)
    {
        var circle = CircularCarrierAt(origin);
        return Segment(circle, start, end);
    }

    /// <summary>Creates transition evidence from an incoming carrier span to an outgoing carrier span.</summary>
    public static Relation CarrierCurveTransition(Relation incoming, Relation outgoing) =>
        new(incoming, outgoing);

    /// <summary>Creates clothoid evidence over a carrier transition plus length/grade span.</summary>
    public static Relation CarrierClothoidTransition(Relation transition, Relation length) =>
        new(transition, length);

    /// <summary>Creates involute evidence from a circular arc and an unwound/tangent length span.</summary>
    public static Relation CarrierInvoluteCurve(Relation baseArc, Relation unwoundLength) =>
        new(baseArc, unwoundLength);

    /// <summary>Creates a coordinate tuple point in a shared frame/group basis.</summary>
    public static Relation FramePoint(Carrier frame, params CarrierLocation[] coordinates) =>
        Relation.OrderedGroup(frame, (coordinates ?? []).Cast<IElement>().ToArray());

    /// <summary>Creates a segment between two coordinate tuple points.</summary>
    public static Relation FrameSegment(Relation start, Relation end) =>
        new(start, end);

    /// <summary>Creates an arc from a shared-frame center and endpoint span.</summary>
    public static Relation FrameArc(Relation center, Relation start, Relation end) =>
        new(center, FrameSegment(start, end));

    /// <summary>Creates a full circle from a shared-frame center and one radius endpoint.</summary>
    public static Relation FrameCircle(Relation center, Relation radiusPoint) =>
        new(center, FrameSegment(center, radiusPoint));

    /// <summary>Creates an arc section whose through point selects the intended sweep.</summary>
    public static Relation FrameArcSection(Relation center, Relation start, Relation through, Relation end) =>
        new(FrameArc(center, start, end), through);

    /// <summary>Creates a quadratic curve from a shared-frame chord and control point.</summary>
    public static Relation FrameQuadraticCurve(Relation start, Relation control, Relation end) =>
        new(FrameSegment(start, end), control);

    /// <summary>Creates a cubic curve from a shared-frame chord and control span.</summary>
    public static Relation FrameCubicCurve(Relation start, Relation firstControl, Relation secondControl, Relation end) =>
        new(FrameSegment(start, end), FrameSegment(firstControl, secondControl));

    /// <summary>Creates transition evidence from an incoming frame span to an outgoing frame span.</summary>
    public static Relation FrameCurveTransition(Relation incoming, Relation outgoing) =>
        new(incoming, outgoing);

    /// <summary>Creates clothoid transition evidence over a frame transition plus length/grade span.</summary>
    public static Relation FrameClothoidTransition(Relation transition, Relation length) =>
        new(transition, length);

    /// <summary>Creates involute evidence from a base frame arc and an unwound/tangent length span.</summary>
    public static Relation FrameInvoluteCurve(Relation baseArc, Relation unwoundLength) =>
        new(baseArc, unwoundLength);

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

    /// <summary>Creates positive area participation from a closed boundary surface.</summary>
    public static Relation AreaFill(params IElement[] boundaryElements) =>
        FillParticipation(inverse: false, boundaryElements);

    /// <summary>Creates inverse area participation from a closed boundary surface.</summary>
    public static Relation AntiAreaFill(params IElement[] boundaryElements) =>
        FillParticipation(inverse: true, boundaryElements);

    /// <summary>
    /// Creates the abstract repeat surface for a relative angle/length path
    /// step. The concrete geometry may be expanded separately; this surface
    /// preserves the template, controller, transition ports, and feedback.
    /// </summary>
    public static Relation RelativeAngleStepRepeat(int count)
    {
        var start = new Placeholder();
        var incoming = new Placeholder();
        var angle = new Placeholder();
        var length = new Placeholder();
        var end = new Placeholder();
        var outgoing = new Placeholder();
        var inputPorts = RepeatSurface.Ports(new Carrier(), start, incoming, angle, length);
        var outputPorts = RepeatSurface.Ports(new Carrier(), end, outgoing);
        var selected = Relation.OrderedGroup(
            new Carrier(),
            start,
            incoming,
            angle,
            length,
            end,
            outgoing);
        var template = RepeatSurface.Template(new Carrier(), inputPorts, selected, outputPorts);
        var transition = RepeatSurface.Frame(
            new Carrier(),
            StructuralPattern.Bind(end, new Relation(new Relation(start, angle), length)),
            StructuralPattern.Bind(outgoing, new Relation(incoming, angle)));
        var feedback = RepeatSurface.Frame(
            new Carrier(),
            RepeatSurface.Feed(end, start),
            RepeatSurface.Feed(outgoing, incoming));

        return RepeatSurface.Create(
            new Carrier(),
            template,
            RepeatSurface.Count(RepeatSurface.CreateAxis(new Carrier()), Math.Max(0, count)),
            transition,
            feedback,
            Point(Carrier.Unknown, 0));
    }

    private static Relation FillParticipation(bool inverse, IReadOnlyList<IElement> boundaryElements)
    {
        var boundary = ChainBoundary(PreserveBoundaryEndpointPins(boundaryElements));
        var fill = inverse
            ? boundary.CreateInverseFillBasis()
            : boundary.CreateFillBasis();
        return Point(fill, Site(1));
    }

    private static IReadOnlyList<IElement> PreserveBoundaryEndpointPins(IReadOnlyList<IElement> elements)
    {
        var preserved = elements.ToList();
        var endpoints = elements
            .SelectMany(ReadBoundaryEndpoints)
            .ToArray();
        for (var leftIndex = 0; leftIndex < endpoints.Length; leftIndex++)
        for (var rightIndex = leftIndex + 1; rightIndex < endpoints.Length; rightIndex++)
        {
            var left = endpoints[leftIndex];
            var right = endpoints[rightIndex];
            if (ReferenceEquals(left.Segment, right.Segment) ||
                !EndpointLocationsMeet(left.Location, right.Location))
            {
                continue;
            }

            preserved.Add(Pin(left.Point, right.Point));
        }

        return preserved;
    }

    private static IEnumerable<BoundaryEndpoint> ReadBoundaryEndpoints(IElement element)
    {
        if (element is Relation segment &&
            TryReadPoint(segment.Left, out var start) &&
            TryReadPoint(segment.Right, out var end))
        {
            yield return new BoundaryEndpoint(segment, (Relation)segment.Left, start);
            yield return new BoundaryEndpoint(segment, (Relation)segment.Right, end);
        }
    }

    private static bool TryReadPoint(IElement element, out CarrierLocation location)
    {
        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier carrier &&
            pointLocation.Carrier.SameIdentityAs(carrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static bool EndpointLocationsMeet(CarrierLocation left, CarrierLocation right) =>
        left.SameTickRefAs(right) ||
        left.Carrier.SameIdentityAs(right.Carrier) && left.Ticks == right.Ticks;

    private static Relation ChainBoundary(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return new Relation(Carrier.Unknown, Carrier.Unknown);
        }

        IElement structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private sealed record BoundaryEndpoint(Relation Segment, Relation Point, CarrierLocation Location);
}
