using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped outline derivation evidence. The surface keeps outline and
/// area separate while connecting the requested source, derived outline,
/// settlement evidence, and unresolved residue.
/// </summary>
public static class GraphicOutlineSurface
{
    /// <summary>Creates an ordered outline/evidence frame.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>Creates an ordered frame of discovered outline boundaries.</summary>
    public static Relation OutlineFrame(Carrier root, params IElement[] outlines) =>
        Frame(root, outlines ?? []);

    /// <summary>
    /// Creates an area-to-outline derivation:
    /// area/union source, outline result, settlement evidence, residue/tension.
    /// </summary>
    public static Relation FromArea(
        Carrier root,
        IElement areaOrUnion,
        IElement outline) =>
        FromArea(
            root,
            areaOrUnion,
            outline,
            GraphicBooleanSurface.BoundarySettlement(new Carrier()),
            TensionSlot());

    /// <summary>Creates an area-to-outline derivation with explicit evidence.</summary>
    public static Relation FromArea(
        Carrier root,
        IElement areaOrUnion,
        IElement outline,
        IElement settlementEvidence,
        IElement residueOrTension) =>
        Frame(
            root,
            areaOrUnion ?? TensionSlot(),
            outline ?? TensionSlot(),
            settlementEvidence ?? GraphicBooleanSurface.BoundarySettlement(new Carrier()),
            residueOrTension ?? TensionSlot());

    /// <summary>Reads an area-to-outline derivation surface.</summary>
    public static bool TryReadFromArea(
        Relation outlineDerivation,
        out IElement areaOrUnion,
        out IElement outline,
        out IElement settlementEvidence,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(outlineDerivation, out var elements) &&
            elements.Length == 4)
        {
            areaOrUnion = elements[0];
            outline = elements[1];
            settlementEvidence = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        areaOrUnion = default!;
        outline = default!;
        settlementEvidence = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>
    /// Locally reads boundary outlines carried by positive and anti areas in
    /// an area frame. This is not boolean settlement; it preserves the boundary
    /// evidence that is already present in the area participants.
    /// </summary>
    public static Relation BoundaryOutlinesFromAreas(Carrier root, IElement areaFrame) =>
        BoundaryOutlinesFromAreas(root, areaFrame, includePositive: true, includeAnti: true);

    /// <summary>Locally reads boundary outlines carried by positive areas only.</summary>
    public static Relation PositiveBoundaryOutlinesFromAreas(Carrier root, IElement areaFrame) =>
        BoundaryOutlinesFromAreas(root, areaFrame, includePositive: true, includeAnti: false);

    /// <summary>Locally reads boundary outlines carried by anti areas only.</summary>
    public static Relation AntiBoundaryOutlinesFromAreas(Carrier root, IElement areaFrame) =>
        BoundaryOutlinesFromAreas(root, areaFrame, includePositive: false, includeAnti: true);

    private static Relation BoundaryOutlinesFromAreas(
        Carrier root,
        IElement areaFrame,
        bool includePositive,
        bool includeAnti)
    {
        var outlines = new List<IElement>();
        var graph = GraphicReadGraph.From(areaFrame ?? TensionSlot());
        foreach (var area in graph.ReadsOf<SharedFrameAreaRead>())
        {
            if (area.IsAntiArea && !includeAnti ||
                !area.IsAntiArea && !includePositive)
            {
                continue;
            }

            var outline = TryReadableSharedBoundarySource(area.Source, out var areaSource)
                ? areaSource
                : TryReadableSharedBoundarySource(area.Boundary.Source, out var boundarySource)
                    ? boundarySource
                    : BoundaryStructure(area.Boundary);
            if (!AlreadyContainsSource(outlines, outline))
            {
                outlines.Add(outline);
            }
        }

        foreach (var area in graph.ReadsOf<AreaRead>())
        {
            if (area.IsAntiArea && !includeAnti ||
                !area.IsAntiArea && !includePositive)
            {
                continue;
            }

            var outline = BoundaryStructure(area.Boundary);
            if (!AlreadyContainsSource(outlines, outline))
            {
                outlines.Add(outline);
            }
        }

        return OutlineFrame(RootOrNew(root), [.. outlines]);
    }

    private static IElement BoundaryStructure(ClosedSharedFrameBoundaryRead boundary)
    {
        var arcBySpanSource = (boundary.Arcs ?? [])
            .GroupBy(arc => SourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var elements = TryReadOrientedBoundaryEdges(boundary, out var oriented)
            ? oriented
                .Select(edge => OrientedBoundaryElement(edge, arcBySpanSource))
                .ToArray()
            : boundary.Segments
                .Select(segment => arcBySpanSource.TryGetValue(SourceId(segment.Source), out var arc)
                    ? arc.Source
                    : segment.Source)
                .ToArray();
        return Chain(elements);
    }

    private static IElement BoundaryStructure(ClosedSegmentBoundaryRead boundary) =>
        Chain([.. boundary.Segments.Select(segment => segment.Source)]);

    private static bool TryReadableSharedBoundarySource(IElement source, out IElement readable)
    {
        if (GraphicReadGraph.From(source).ReadsOf<ClosedSharedFrameBoundaryRead>().Any())
        {
            readable = source;
            return true;
        }

        readable = default!;
        return false;
    }

    private static bool AlreadyContainsSource(IReadOnlyList<IElement> outlines, IElement source)
    {
        foreach (var outline in outlines)
        {
            if (ReferenceEquals(outline, source) || Equals(outline, source))
            {
                return true;
            }
        }

        return false;
    }

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            return TensionSlot();
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
    }

    private static bool TryReadFrameElements(Relation frame, out IElement[] elements)
    {
        if (frame?.Right is Carrier holder &&
            frame.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();

    private static IElement OrientedBoundaryElement(
        OrientedBoundaryEdge edge,
        IReadOnlyDictionary<ulong, SharedFrameArcRead> arcBySpanSource)
    {
        var start = (Relation)edge.StartEndpoint.Point.Source;
        var end = (Relation)edge.EndEndpoint.Point.Source;
        return arcBySpanSource.TryGetValue(SourceId(edge.Segment.Source), out var arc)
            ? GraphicStructure.FrameArc((Relation)arc.Center.Source, start, end)
            : GraphicStructure.FrameSegment(start, end);
    }

    private static bool TryReadOrientedBoundaryEdges(
        ClosedSharedFrameBoundaryRead boundary,
        out IReadOnlyList<OrientedBoundaryEdge> edges)
    {
        var siteByEndpoint = boundary.Sites
            .SelectMany(site => site.Select(endpoint => (endpoint, site)))
            .ToDictionary(pair => EndpointSignature(pair.endpoint), pair => pair.site);
        var first = boundary.Segments[0];
        var incoming = new SharedFrameSegmentEndpointRead(first, first.Start, IsStart: true);
        var visited = new HashSet<ulong>();
        var ordered = new List<OrientedBoundaryEdge>();
        while (visited.Add(SourceId(incoming.Segment.Source)))
        {
            var outgoing = OtherEndpoint(incoming);
            ordered.Add(new OrientedBoundaryEdge(incoming.Segment, incoming, outgoing));
            if (!siteByEndpoint.TryGetValue(EndpointSignature(outgoing), out var site))
            {
                break;
            }

            var next = site.FirstOrDefault(endpoint =>
                SourceId(endpoint.Segment.Source) != SourceId(incoming.Segment.Source) &&
                !visited.Contains(SourceId(endpoint.Segment.Source)));
            if (next is null)
            {
                break;
            }

            incoming = next;
        }

        edges = ordered;
        return ordered.Count == boundary.Segments.Count;
    }

    private static SharedFrameSegmentEndpointRead OtherEndpoint(SharedFrameSegmentEndpointRead endpoint) =>
        endpoint.IsStart
            ? new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.End, IsStart: false)
            : new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.Start, IsStart: true);

    private static string EndpointSignature(SharedFrameSegmentEndpointRead endpoint) =>
        $"{SourceId(endpoint.Segment.Source)}:{(endpoint.IsStart ? "s" : "e")}";

    private static ulong SourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private readonly record struct OrientedBoundaryEdge(
        SharedFrameSegmentRead Segment,
        SharedFrameSegmentEndpointRead StartEndpoint,
        SharedFrameSegmentEndpointRead EndEndpoint);
}
