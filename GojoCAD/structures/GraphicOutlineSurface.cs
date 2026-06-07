using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped outline derivation evidence. The surface keeps outline and
/// area separate while connecting the requested source, derived outline,
/// boundary settlement evidence, and unresolved residue.
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
    /// area/union source, outline result, boundary settlement evidence,
    /// residue/tension.
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
        IElement boundarySettlement,
        IElement residueOrTension) =>
        Frame(
            root,
            areaOrUnion ?? TensionSlot(),
            outline ?? TensionSlot(),
            boundarySettlement ?? GraphicBooleanSurface.BoundarySettlement(new Carrier()),
            residueOrTension ?? TensionSlot());

    /// <summary>Reads an area-to-outline derivation surface.</summary>
    public static bool TryReadFromArea(
        Relation outlineDerivation,
        out IElement areaOrUnion,
        out IElement outline,
        out IElement boundarySettlement,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(outlineDerivation, out var elements) &&
            elements.Length == 4)
        {
            areaOrUnion = elements[0];
            outline = elements[1];
            boundarySettlement = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        areaOrUnion = default!;
        outline = default!;
        boundarySettlement = default!;
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

            var outline = BoundaryStructure(area.Boundary);
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
        var elements = new List<IElement>();
        foreach (var segment in boundary.Segments)
        {
            if (!IsArcSpan(segment, boundary.Arcs ?? []))
            {
                elements.Add(segment.Source);
            }
        }

        foreach (var arc in boundary.Arcs ?? [])
        {
            elements.Add(arc.Source);
        }

        return Chain([.. elements]);
    }

    private static IElement BoundaryStructure(ClosedSegmentBoundaryRead boundary) =>
        Chain([.. boundary.Segments.Select(segment => segment.Source)]);

    private static bool IsArcSpan(SharedFrameSegmentRead segment, IReadOnlyList<SharedFrameArcRead> arcs)
    {
        foreach (var arc in arcs)
        {
            if (ReferenceEquals(arc.Span, segment) ||
                ReferenceEquals(arc.Span.Source, segment.Source) ||
                Equals(arc.Span.Source, segment.Source))
            {
                return true;
            }
        }

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
}
