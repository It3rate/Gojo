using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Boundary-local concept candidate surfaces. These helpers read existing
/// outline/boundary structure and emit ordinary Gojo candidate frames that can
/// be searched by <see cref="GraphicSelectionSurface"/>.
/// </summary>
public static class GraphicBoundaryConceptSurface
{
    /// <summary>
    /// Creates a search pattern for a boundary cusp plus a visual/site witness.
    /// </summary>
    public static Relation CuspWitnessPattern(Carrier root, IElement witnessSlot) =>
        GraphicSelectionSurface.Frame(
            RootOrNew(root),
            GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier()),
            witnessSlot ?? new Placeholder(),
            new Placeholder(),
            new Placeholder(),
            new Placeholder());

    /// <summary>
    /// Walks closed shared-frame boundaries and emits cusp candidate frames:
    /// cusp evidence, witness, site point, incoming edge, outgoing edge.
    /// Arc-to-arc continuation on the same center remains smooth evidence and
    /// is not emitted as a cusp candidate.
    /// </summary>
    public static Relation CuspCandidateDomain(Carrier root, IElement outline, long witnessRadiusTicks = 5)
    {
        var graph = GraphicReadGraph.From(outline ?? GraphicConceptShape.TensionSlot());
        var candidates = new List<IElement>();
        foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>())
        foreach (var site in boundary.Sites)
        {
            if (site.Count != 2 ||
                IsSmoothArcContinuation(site, boundary.Arcs ?? []))
            {
                continue;
            }

            var witness = CreateWitness(site[0].Point, witnessRadiusTicks);
            candidates.Add(GraphicSelectionSurface.Frame(
                new Carrier(),
                GraphicConceptShape.CuspJoinEvidenceSurface(new Carrier()),
                witness,
                site[0].Point.Source,
                site[0].Segment.Source,
                site[1].Segment.Source));
        }

        return GraphicSelectionSurface.Frame(RootOrNew(root), [.. candidates]);
    }

    private static IElement CreateWitness(SharedFramePointRead point, long radiusTicks)
    {
        if (point.Coordinates.Count < 2 ||
            radiusTicks <= 0)
        {
            return point.Source;
        }

        var x = point.Coordinates[0];
        var y = point.Coordinates[1];
        Relation P(long xTicks, long yTicks) =>
            GraphicStructure.FramePoint(
                point.Holder,
                new CarrierLocation(x.Carrier, xTicks),
                new CarrierLocation(y.Carrier, yTicks));

        Relation Arc(long startX, long startY, long endX, long endY) =>
            GraphicStructure.FrameArc(
                P(x.Ticks, y.Ticks),
                P(startX, startY),
                P(endX, endY));

        return Chain(
            Arc(x.Ticks + radiusTicks, y.Ticks, x.Ticks, y.Ticks + radiusTicks),
            Arc(x.Ticks, y.Ticks + radiusTicks, x.Ticks - radiusTicks, y.Ticks),
            Arc(x.Ticks - radiusTicks, y.Ticks, x.Ticks, y.Ticks - radiusTicks),
            Arc(x.Ticks, y.Ticks - radiusTicks, x.Ticks + radiusTicks, y.Ticks));
    }

    private static bool IsSmoothArcContinuation(
        IReadOnlyList<SharedFrameSegmentEndpointRead> site,
        IReadOnlyList<SharedFrameArcRead> arcs)
    {
        if (site.Count != 2)
        {
            return false;
        }

        var first = FindArcForSpan(site[0].Segment, arcs);
        var second = FindArcForSpan(site[1].Segment, arcs);
        return first is not null &&
            second is not null &&
            (ReferenceEquals(first.Center.Source, second.Center.Source) ||
                Equals(first.Center.Source, second.Center.Source));
    }

    private static SharedFrameArcRead? FindArcForSpan(
        SharedFrameSegmentRead segment,
        IReadOnlyList<SharedFrameArcRead> arcs)
    {
        foreach (var arc in arcs)
        {
            if (ReferenceEquals(arc.Span, segment) ||
                ReferenceEquals(arc.Span.Source, segment.Source) ||
                Equals(arc.Span.Source, segment.Source))
            {
                return arc;
            }
        }

        return null;
    }

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            return GraphicConceptShape.TensionSlot();
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();
}
