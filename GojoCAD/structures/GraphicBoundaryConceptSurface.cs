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
    /// Creates a search pattern for an open line/path terminal plus a
    /// visual/site witness.
    /// </summary>
    public static Relation LineTipWitnessPattern(Carrier root, IElement witnessSlot) =>
        GraphicSelectionSurface.Frame(
            RootOrNew(root),
            GraphicConceptShape.LineTipEvidenceSurface(new Carrier()),
            witnessSlot ?? new Placeholder(),
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

    /// <summary>
    /// Walks path graph topology and emits terminal/tip candidate frames:
    /// tip evidence, witness, terminal point, terminating edge.
    /// </summary>
    public static Relation LineTipCandidateDomain(Carrier root, IElement source, long witnessRadiusTicks = 5)
    {
        var graph = GraphicReadGraph.From(source ?? GraphicConceptShape.TensionSlot());
        var candidates = new List<IElement>();
        foreach (var pathGraph in graph.ReadsOf<PathGraphRead>())
        foreach (var node in pathGraph.Nodes.Where(node => node.Valence == 1))
        {
            var endpoint = node.Endpoints[0];
            if (endpoint.SharedFramePoint is not { } framePoint)
            {
                continue;
            }

            candidates.Add(GraphicSelectionSurface.Frame(
                new Carrier(),
                GraphicConceptShape.LineTipEvidenceSurface(new Carrier()),
                CreateWitness(framePoint, witnessRadiusTicks),
                framePoint.Source,
                endpoint.Segment.Source));
        }

        return GraphicSelectionSurface.Frame(RootOrNew(root), [.. candidates]);
    }

    /// <summary>
    /// Creates circular cap outlines at the terminal points carried by a
    /// terminal/tip selection.
    /// </summary>
    public static Relation CircleCapsFromLineTipSelection(Carrier root, IElement selection, long radiusTicks)
    {
        var circles = new List<IElement>();
        foreach (var point in ReadSelectedSharedFramePoints(selection))
        {
            circles.Add(CircleAtSharedFramePoint(point, radiusTicks));
        }

        var residue = circles.Count == 0
            ? GraphicSelectionSurface.Frame(new Carrier(), selection ?? GraphicConceptShape.TensionSlot(), GraphicConceptShape.TensionSlot())
            : GraphicConceptShape.TensionSlot();
        return GraphicSelectionSurface.Frame(
            RootOrNew(root),
            selection ?? GraphicConceptShape.TensionSlot(),
            GraphicConceptShape.CircularTipCapEvidenceSurface(new Carrier()),
            Relation.Segment(new Carrier(), 0, Math.Abs(radiusTicks)),
            GraphicSelectionSurface.Frame(new Carrier(), [.. circles]),
            residue);
    }

    /// <summary>Reads circular cap action evidence and its generated circle outlines.</summary>
    public static bool TryReadCircleCapsFromLineTipSelection(
        Relation action,
        out IElement selection,
        out IElement capEvidence,
        out IElement radiusEvidence,
        out IElement circleFrame,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(action, out var elements) &&
            elements.Length == 5 &&
            elements[1] is Relation capRelation &&
            GraphicConceptShape.TryReadCircularTipCapEvidence(capRelation, out _, out _, out _, out _))
        {
            selection = elements[0];
            capEvidence = capRelation;
            radiusEvidence = elements[2];
            circleFrame = elements[3];
            residueOrTension = elements[4];
            return true;
        }

        selection = default!;
        capEvidence = default!;
        radiusEvidence = default!;
        circleFrame = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Creates a small shared-frame circle around an existing shared-frame point read.</summary>
    public static IElement CircleAtSharedFramePoint(SharedFramePointRead point, long radiusTicks) =>
        CreateWitness(point, radiusTicks);

    private static IEnumerable<SharedFramePointRead> ReadSelectedSharedFramePoints(IElement selection)
    {
        if (selection is not Relation selectionRelation ||
            !GraphicSelectionSurface.TryReadSelection(
                selectionRelation,
                out _,
                out _,
                out _,
                out var matches,
                out _) ||
            matches is not Relation matchFrame ||
            !GraphicSelectionSurface.TryReadMatchFrame(matchFrame, out var matchElements))
        {
            yield break;
        }

        foreach (var matchElement in matchElements)
        {
            if (matchElement is not Relation match ||
                !GraphicSelectionSurface.TryReadMatch(
                    match,
                    out _,
                    out _,
                    out _,
                    out var evidence,
                    out _) ||
                !TryReadAppliedPatternElements(evidence, out var appliedElements) ||
                appliedElements.Length < 3)
            {
                continue;
            }

            var pointGraph = GraphicReadGraph.From(appliedElements[2]);
            foreach (var point in pointGraph.ReadsOf<SharedFramePointRead>().Take(1))
            {
                yield return point;
            }
        }
    }

    private static bool TryReadAppliedPatternElements(IElement evidence, out IElement[] elements)
    {
        if (evidence is Relation patternEvidence &&
            GraphicSelectionSurface.TryReadPatternEvidence(
                patternEvidence,
                out _,
                out var appliedPattern,
                out _,
                out _) &&
            appliedPattern is Relation appliedRelation &&
            TryReadFrameElements(appliedRelation, out elements))
        {
            return true;
        }

        elements = [];
        return false;
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
