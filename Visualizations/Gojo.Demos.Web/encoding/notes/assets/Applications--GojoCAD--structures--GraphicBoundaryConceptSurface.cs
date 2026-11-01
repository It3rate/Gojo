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
        var seenTips = new HashSet<string>();
        var arcSpanIds = graph.ReadsOf<SharedFrameArcRead>()
            .Select(arc => SourceId(arc.Span.Source))
            .ToHashSet();
        var circleRadiusSpanIds = graph.ReadsOf<SharedFrameCircleRead>()
            .Select(circle => SourceId(circle.RadiusSpan.Source))
            .ToHashSet();
        var closedBoundarySegmentIds = graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
            .SelectMany(boundary => boundary.Segments)
            .Select(segment => SourceId(segment.Source))
            .ToHashSet();

        bool IsSupportOrClosedSegment(IElement segmentSource)
        {
            var id = SourceId(segmentSource);
            return arcSpanIds.Contains(id) ||
                circleRadiusSpanIds.Contains(id) ||
                closedBoundarySegmentIds.Contains(id);
        }

        void AddCandidate(SharedFramePointRead framePoint, IElement terminatingEdge)
        {
            var key = $"{SourceId(framePoint.Source)}:{SourceId(terminatingEdge)}";
            if (!seenTips.Add(key))
            {
                return;
            }

            candidates.Add(GraphicSelectionSurface.Frame(
                new Carrier(),
                GraphicConceptShape.LineTipEvidenceSurface(new Carrier()),
                CreateWitness(framePoint, witnessRadiusTicks),
                framePoint.Source,
                terminatingEdge));
        }

        foreach (var pathGraph in graph.ReadsOf<PathGraphRead>())
            foreach (var node in pathGraph.Nodes.Where(node => node.Valence == 1))
            {
                var endpoint = node.Endpoints[0];
                if (endpoint.SharedFramePoint is not { } framePoint ||
                    IsSupportOrClosedSegment(endpoint.Segment.Source))
                {
                    continue;
                }

                AddCandidate(framePoint, endpoint.Segment.Source);
            }

        foreach (var arc in graph.ReadsOf<SharedFrameArcRead>())
        {
            if (closedBoundarySegmentIds.Contains(SourceId(arc.Span.Source)))
            {
                continue;
            }

            AddCandidate(arc.Start, arc.Span.Source);
            AddCandidate(arc.End, arc.Span.Source);
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

    /// <summary>
    /// Creates rounded boundary outlines by applying a selected cusp pattern to
    /// closed shared-frame boundaries. Selected cusp sites are trimmed on their
    /// incoming and outgoing source sections, then replaced with circular arc
    /// sections. Unsupported selected sites remain as residue.
    /// </summary>
    public static Relation RoundedBoundaryFromCuspSelection(
        Carrier root,
        IElement selection,
        IElement outline,
        long radiusTicks)
    {
        var outlineSource = outline ?? GraphicConceptShape.TensionSlot();
        var graph = GraphicReadGraph.From(outlineSource);
        var selectedCusps = ReadSelectedCusps(selection).ToArray();
        var sectionBySpanSource = graph.ReadsOf<SharedFrameArcSectionRead>()
            .GroupBy(section => SourceId(section.BaseArc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var roundedOutlines = new List<IElement>();
        var residue = new List<IElement>();
        foreach (var boundary in graph
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => SourceId(boundary.Source)))
        {
            if (TryCreateRoundedBoundary(
                    boundary,
                    selectedCusps,
                    sectionBySpanSource,
                    Math.Abs(radiusTicks),
                    out var rounded,
                    residue))
            {
                roundedOutlines.Add(rounded);
            }
            else
            {
                roundedOutlines.Add(BoundaryStructure(boundary));
            }
        }

        if (roundedOutlines.Count == 0)
        {
            residue.Add(outline ?? GraphicConceptShape.TensionSlot());
        }

        var residueOrTension = residue.Count == 0
            ? GraphicConceptShape.TensionSlot()
            : GraphicSelectionSurface.Frame(new Carrier(), [.. residue.DistinctBy(SourceId)]);
        return GraphicSelectionSurface.Frame(
            RootOrNew(root),
            selection ?? GraphicConceptShape.TensionSlot(),
            GraphicConceptShape.RoundedCuspJoinEvidenceSurface(new Carrier()),
            Relation.Segment(new Carrier(), 0, Math.Abs(radiusTicks)),
            GraphicSelectionSurface.Frame(new Carrier(), [.. roundedOutlines]),
            residueOrTension);
    }

    /// <summary>Reads rounded-cusp action evidence and its generated outline frame.</summary>
    public static bool TryReadRoundedBoundaryFromCuspSelection(
        Relation action,
        out IElement selection,
        out IElement roundedCuspEvidence,
        out IElement radiusEvidence,
        out IElement outlineFrame,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(action, out var elements) &&
            elements.Length == 5 &&
            elements[1] is Relation roundedRelation &&
            GraphicConceptShape.TryReadRoundedCuspJoinEvidence(roundedRelation, out _, out _, out _, out _))
        {
            selection = elements[0];
            roundedCuspEvidence = elements[1];
            radiusEvidence = elements[2];
            outlineFrame = elements[3];
            residueOrTension = elements[4];
            return true;
        }

        selection = default!;
        roundedCuspEvidence = default!;
        radiusEvidence = default!;
        outlineFrame = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Creates a small shared-frame circle around an existing shared-frame point read.</summary>
    public static IElement CircleAtSharedFramePoint(SharedFramePointRead point, long radiusTicks) =>
        CreateWitness(point, radiusTicks);

    private sealed record SelectedCuspRead(
        IElement Source,
        ulong PointId,
        ulong FirstSegmentId,
        ulong SecondSegmentId)
    {
        public bool Matches(IReadOnlyList<SharedFrameSegmentEndpointRead> site)
        {
            if (site.Count != 2 ||
                SourceId(site[0].Point.Source) != PointId &&
                SourceId(site[1].Point.Source) != PointId)
            {
                return false;
            }

            var first = SourceId(site[0].Segment.Source);
            var second = SourceId(site[1].Segment.Source);
            return first == FirstSegmentId && second == SecondSegmentId ||
                first == SecondSegmentId && second == FirstSegmentId;
        }
    }

    private sealed record OrientedBoundaryEdge(
        SharedFrameSegmentRead Segment,
        SharedFrameSegmentEndpointRead StartEndpoint,
        SharedFrameSegmentEndpointRead EndEndpoint);

    private readonly record struct FramePointTicks(SharedFramePointRead Template, long X, long Y);

    private readonly record struct PointTicks(double X, double Y);

    private sealed record EdgeGeometry(
        SharedFramePointRead Template,
        bool IsArc,
        PointTicks Start,
        PointTicks End,
        PointTicks Center,
        double Radius,
        double Sweep)
    {
        public double LengthFromEndToStart() =>
            IsArc
                ? Radius * Math.Abs(Sweep)
                : Distance(End, Start);

        public double LengthFromStartToEnd() =>
            IsArc
                ? Radius * Math.Abs(Sweep)
                : Distance(Start, End);

        public bool TryDirectionAwayFromEnd(out double unitX, out double unitY) =>
            TryUnit(End, RawPointFromEnd(Math.Min(1d, LengthFromEndToStart() * 0.5d)), out unitX, out unitY);

        public bool TryDirectionAwayFromStart(out double unitX, out double unitY) =>
            TryUnit(Start, RawPointFromStart(Math.Min(1d, LengthFromStartToEnd() * 0.5d)), out unitX, out unitY);

        public FramePointTicks PointFromEnd(double distance) =>
            ToFramePoint(RawPointFromEnd(distance));

        public FramePointTicks PointFromStart(double distance) =>
            ToFramePoint(RawPointFromStart(distance));

        public PointTicks RawPointFromEnd(double distance) =>
            IsArc
                ? ArcPointFromEnd(distance)
                : LinePoint(End, Start, distance);

        public PointTicks RawPointFromStart(double distance) =>
            IsArc
                ? ArcPointFromStart(distance)
                : LinePoint(Start, End, distance);

        public FramePointTicks ToFramePoint(PointTicks point) =>
            new(Template, Rounded(point.X), Rounded(point.Y));

        private PointTicks LinePoint(PointTicks from, PointTicks to, double distance)
        {
            if (!TryUnit(from, to, out var unitX, out var unitY))
            {
                return from;
            }

            return new PointTicks(from.X + unitX * distance, from.Y + unitY * distance);
        }

        private PointTicks ArcPointFromStart(double distance)
        {
            var startAngle = Angle(Center, Start);
            var maxDistance = Radius * Math.Abs(Sweep);
            var clampedDistance = Math.Clamp(distance, 0d, maxDistance);
            var angleOffset = Radius <= 0d
                ? 0d
                : Math.Sign(Sweep) * clampedDistance / Radius;
            var angle = startAngle + angleOffset;
            return new PointTicks(
                Center.X + Math.Cos(angle) * Radius,
                Center.Y + Math.Sin(angle) * Radius);
        }

        private PointTicks ArcPointFromEnd(double distance)
        {
            var endAngle = Angle(Center, End);
            var maxDistance = Radius * Math.Abs(Sweep);
            var clampedDistance = Math.Clamp(distance, 0d, maxDistance);
            var angleOffset = Radius <= 0d
                ? 0d
                : Math.Sign(Sweep) * clampedDistance / Radius;
            var angle = endAngle - angleOffset;
            return new PointTicks(
                Center.X + Math.Cos(angle) * Radius,
                Center.Y + Math.Sin(angle) * Radius);
        }
    }

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

    private static IEnumerable<SelectedCuspRead> ReadSelectedCusps(IElement selection)
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
                appliedElements.Length < 5 ||
                !TryReadFirstSharedFramePoint(appliedElements[2], out var point) ||
                !TryReadFirstSharedFrameSegment(appliedElements[3], out var first) ||
                !TryReadFirstSharedFrameSegment(appliedElements[4], out var second))
            {
                continue;
            }

            yield return new SelectedCuspRead(
                matchElement,
                SourceId(point.Source),
                SourceId(first.Source),
                SourceId(second.Source));
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

        return GraphicStructure.FrameCircle(
            P(x.Ticks, y.Ticks),
            P(x.Ticks + radiusTicks, y.Ticks));
    }

    private static bool TryCreateRoundedBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        IReadOnlyList<SelectedCuspRead> selectedCusps,
        IReadOnlyDictionary<ulong, SharedFrameArcSectionRead> sectionBySpanSource,
        long radiusTicks,
        out Relation rounded,
        ICollection<IElement> residue)
    {
        if (radiusTicks <= 0 ||
            !TryReadOrientedBoundaryEdges(boundary, out var edges))
        {
            rounded = null!;
            return false;
        }

        var arcBySpanSource = (boundary.Arcs ?? [])
            .GroupBy(arc => SourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var startTrimByEdge = new Dictionary<int, FramePointTicks>();
        var endTrimByEdge = new Dictionary<int, FramePointTicks>();
        var roundArcAfterEdge = new Dictionary<int, IElement>();
        var touched = false;

        for (var index = 0; index < edges.Count; index++)
        {
            var edge = edges[index];
            var nextIndex = (index + 1) % edges.Count;
            var next = edges[nextIndex];
            var site = FindSite(boundary.Sites, edge.EndEndpoint);
            var selected = site is null
                ? null
                : selectedCusps.FirstOrDefault(cusp => cusp.Matches(site));
            if (selected is null)
            {
                continue;
            }

            if (!TryCreateRoundedJoin(
                    edge,
                    next,
                    arcBySpanSource,
                    sectionBySpanSource,
                    radiusTicks,
                    out var incomingTrim,
                    out var outgoingTrim,
                    out var arc))
            {
                residue.Add(selected.Source);
                continue;
            }

            endTrimByEdge[index] = incomingTrim;
            startTrimByEdge[nextIndex] = outgoingTrim;
            roundArcAfterEdge[index] = arc;
            touched = true;
        }

        if (!touched)
        {
            rounded = null!;
            return false;
        }

        var elements = new List<IElement>();
        for (var index = 0; index < edges.Count; index++)
        {
            var edge = edges[index];
            var segmentId = SourceId(edge.Segment.Source);
            var start = startTrimByEdge.TryGetValue(index, out var startTrim)
                ? CreateFramePoint(startTrim)
                : edge.StartEndpoint.Point.Source;
            var end = endTrimByEdge.TryGetValue(index, out var endTrim)
                ? CreateFramePoint(endTrim)
                : edge.EndEndpoint.Point.Source;
            if (!SameFramePoint(start, end))
            {
                elements.Add(arcBySpanSource.TryGetValue(segmentId, out var sourceArc)
                    ? CreateRetainedArcSection(sourceArc, sectionBySpanSource, (Relation)start, (Relation)end)
                    : GraphicStructure.FrameSegment((Relation)start, (Relation)end));
            }

            if (roundArcAfterEdge.TryGetValue(index, out var arc))
            {
                elements.Add(arc);
            }
        }

        rounded = Chain([.. elements]);
        return true;
    }

    private static bool TryCreateLineLineRound(
        OrientedBoundaryEdge incoming,
        OrientedBoundaryEdge outgoing,
        long radiusTicks,
        out FramePointTicks incomingTrim,
        out FramePointTicks outgoingTrim,
        out IElement arc)
    {
        incomingTrim = default;
        outgoingTrim = default;
        arc = default!;
        if (!TryReadPointTicks(incoming.EndEndpoint.Point, out var cuspX, out var cuspY) ||
            !TryReadPointTicks(incoming.StartEndpoint.Point, out var incomingX, out var incomingY) ||
            !TryReadPointTicks(outgoing.EndEndpoint.Point, out var outgoingX, out var outgoingY))
        {
            return false;
        }

        var incomingVectorX = incomingX - cuspX;
        var incomingVectorY = incomingY - cuspY;
        var outgoingVectorX = outgoingX - cuspX;
        var outgoingVectorY = outgoingY - cuspY;
        var incomingLength = Math.Sqrt(incomingVectorX * incomingVectorX + incomingVectorY * incomingVectorY);
        var outgoingLength = Math.Sqrt(outgoingVectorX * outgoingVectorX + outgoingVectorY * outgoingVectorY);
        if (incomingLength <= 0 ||
            outgoingLength <= 0)
        {
            return false;
        }

        var uX = incomingVectorX / incomingLength;
        var uY = incomingVectorY / incomingLength;
        var vX = outgoingVectorX / outgoingLength;
        var vY = outgoingVectorY / outgoingLength;
        var dot = Math.Clamp(uX * vX + uY * vY, -0.999, 0.999);
        var theta = Math.Acos(dot);
        var halfTheta = theta / 2.0;
        var tangentDistance = radiusTicks / Math.Tan(halfTheta);
        var maxTangentDistance = Math.Min(incomingLength, outgoingLength) * 0.45;
        tangentDistance = Math.Min(tangentDistance, maxTangentDistance);
        var effectiveRadius = tangentDistance * Math.Tan(halfTheta);
        var bisectorX = uX + vX;
        var bisectorY = uY + vY;
        var bisectorLength = Math.Sqrt(bisectorX * bisectorX + bisectorY * bisectorY);
        if (tangentDistance <= 0 ||
            effectiveRadius <= 0 ||
            bisectorLength <= 0)
        {
            return false;
        }

        bisectorX /= bisectorLength;
        bisectorY /= bisectorLength;
        var centerDistance = effectiveRadius / Math.Sin(halfTheta);
        var center = new FramePointTicks(
            incoming.EndEndpoint.Point,
            Rounded(cuspX + bisectorX * centerDistance),
            Rounded(cuspY + bisectorY * centerDistance));
        incomingTrim = new FramePointTicks(
            incoming.EndEndpoint.Point,
            Rounded(cuspX + uX * tangentDistance),
            Rounded(cuspY + uY * tangentDistance));
        outgoingTrim = new FramePointTicks(
            outgoing.StartEndpoint.Point,
            Rounded(cuspX + vX * tangentDistance),
            Rounded(cuspY + vY * tangentDistance));
        if (SameFramePoint(incomingTrim, incoming.EndEndpoint.Point) ||
            SameFramePoint(outgoingTrim, outgoing.StartEndpoint.Point))
        {
            return false;
        }

        arc = CreateArcSection(center, incomingTrim, outgoingTrim);
        return true;
    }

    private readonly record struct LineArcRoundCandidate(
        FramePointTicks Center,
        FramePointTicks LineTrim,
        FramePointTicks ArcTrim,
        double Score);

    private static bool TryCreateLineArcRound(
        EdgeGeometry incomingGeometry,
        EdgeGeometry outgoingGeometry,
        long radiusTicks,
        out FramePointTicks incomingTrim,
        out FramePointTicks outgoingTrim,
        out IElement arc)
    {
        incomingTrim = default;
        outgoingTrim = default;
        arc = default!;
        if (incomingGeometry.IsArc == outgoingGeometry.IsArc)
        {
            return false;
        }

        var lineGeometry = incomingGeometry.IsArc ? outgoingGeometry : incomingGeometry;
        var arcGeometry = incomingGeometry.IsArc ? incomingGeometry : outgoingGeometry;
        var lineFromStart = incomingGeometry.IsArc;
        var arcFromStart = !incomingGeometry.IsArc;
        var lineLength = lineFromStart
            ? lineGeometry.LengthFromStartToEnd()
            : lineGeometry.LengthFromEndToStart();
        var arcLength = arcFromStart
            ? arcGeometry.LengthFromStartToEnd()
            : arcGeometry.LengthFromEndToStart();
        if (lineLength <= 0d ||
            arcLength <= 0d ||
            radiusTicks <= 0 ||
            !TryDirectionAwayFromCusp(lineGeometry, lineFromStart, out var lineAwayX, out var lineAwayY))
        {
            return false;
        }

        var cusp = lineFromStart ? lineGeometry.Start : lineGeometry.End;
        if (!TryApproximateRoundedCenter(
                incomingGeometry,
                outgoingGeometry,
                radiusTicks,
                out var approximateCenter))
        {
            approximateCenter = cusp;
        }

        LineArcRoundCandidate? best = null;
        var maxLineDistance = lineLength * 0.85d;
        var maxArcDistance = arcLength * 0.85d;
        for (var step = 0; step < 12; step++)
        {
            var radius = radiusTicks * Math.Pow(0.88d, step);
            if (radius < 1d)
            {
                break;
            }

            foreach (var normalSign in new[] { -1d, 1d })
                foreach (var tangentSide in new[] { -1d, 1d })
                {
                    if (!TryLineArcRoundCandidate(
                            lineGeometry,
                            arcGeometry,
                            lineFromStart,
                            arcFromStart,
                            cusp,
                            lineAwayX,
                            lineAwayY,
                            normalSign,
                            tangentSide,
                            radius,
                            maxLineDistance,
                            maxArcDistance,
                            approximateCenter,
                            out var candidate))
                    {
                        continue;
                    }

                    if (best is null || candidate.Score < best.Value.Score)
                    {
                        best = candidate;
                    }
                }

            if (best is not null)
            {
                break;
            }
        }

        if (best is null)
        {
            return false;
        }

        if (incomingGeometry.IsArc)
        {
            incomingTrim = best.Value.ArcTrim;
            outgoingTrim = best.Value.LineTrim;
        }
        else
        {
            incomingTrim = best.Value.LineTrim;
            outgoingTrim = best.Value.ArcTrim;
        }

        if (SameFramePoint(incomingTrim, incomingGeometry.End) ||
            SameFramePoint(outgoingTrim, outgoingGeometry.Start))
        {
            return false;
        }

        arc = CreateArcSection(best.Value.Center, incomingTrim, outgoingTrim);
        return true;
    }

    private static bool TryLineArcRoundCandidate(
        EdgeGeometry lineGeometry,
        EdgeGeometry arcGeometry,
        bool lineFromStart,
        bool arcFromStart,
        PointTicks cusp,
        double lineAwayX,
        double lineAwayY,
        double normalSign,
        double tangentSide,
        double radius,
        double maxLineDistance,
        double maxArcDistance,
        PointTicks approximateCenter,
        out LineArcRoundCandidate candidate)
    {
        candidate = default;
        var lineNormalX = -lineAwayY * normalSign;
        var lineNormalY = lineAwayX * normalSign;
        var centerToCuspX = cusp.X - arcGeometry.Center.X;
        var centerToCuspY = cusp.Y - arcGeometry.Center.Y;
        var shiftedX = centerToCuspX + lineNormalX * radius;
        var shiftedY = centerToCuspY + lineNormalY * radius;
        var targetDistance = arcGeometry.Radius + tangentSide * radius;
        if (targetDistance <= 0d)
        {
            return false;
        }

        var b = 2d * (shiftedX * lineAwayX + shiftedY * lineAwayY);
        var c = shiftedX * shiftedX + shiftedY * shiftedY - targetDistance * targetDistance;
        var discriminant = b * b - 4d * c;
        if (discriminant < 0d)
        {
            return false;
        }

        LineArcRoundCandidate? best = null;
        var root = Math.Sqrt(discriminant);
        foreach (var lineDistance in new[] { (-b - root) * 0.5d, (-b + root) * 0.5d })
        {
            if (lineDistance <= 0.75d ||
                lineDistance > maxLineDistance)
            {
                continue;
            }

            var rawLineTrim = new PointTicks(
                cusp.X + lineAwayX * lineDistance,
                cusp.Y + lineAwayY * lineDistance);
            var rawCenter = new PointTicks(
                rawLineTrim.X + lineNormalX * radius,
                rawLineTrim.Y + lineNormalY * radius);
            if (!TryUnit(arcGeometry.Center, rawCenter, out var radialX, out var radialY))
            {
                continue;
            }

            var rawArcTrim = new PointTicks(
                arcGeometry.Center.X + radialX * arcGeometry.Radius,
                arcGeometry.Center.Y + radialY * arcGeometry.Radius);
            if (!TryArcDistanceFromCusp(
                    arcGeometry,
                    arcFromStart,
                    rawArcTrim,
                    out var arcDistance) ||
                arcDistance <= 0.75d ||
                arcDistance > maxArcDistance)
            {
                continue;
            }

            var center = lineGeometry.ToFramePoint(rawCenter);
            var lineTrim = lineGeometry.ToFramePoint(rawLineTrim);
            var arcTrim = arcGeometry.ToFramePoint(rawArcTrim);
            if (SameFramePoint(lineTrim, lineFromStart ? lineGeometry.Start : lineGeometry.End) ||
                SameFramePoint(arcTrim, arcFromStart ? arcGeometry.Start : arcGeometry.End))
            {
                continue;
            }

            var score =
                Distance(rawCenter, approximateCenter) * 8d +
                Math.Abs(lineDistance - arcDistance) * 0.4d +
                lineDistance * 0.05d +
                arcDistance * 0.05d;
            var rootCandidate = new LineArcRoundCandidate(center, lineTrim, arcTrim, score);
            if (best is null || rootCandidate.Score < best.Value.Score)
            {
                best = rootCandidate;
            }
        }

        if (best is null)
        {
            return false;
        }

        candidate = best.Value;
        return true;
    }

    private static bool TryDirectionAwayFromCusp(
        EdgeGeometry geometry,
        bool fromStart,
        out double unitX,
        out double unitY) =>
        fromStart
            ? geometry.TryDirectionAwayFromStart(out unitX, out unitY)
            : geometry.TryDirectionAwayFromEnd(out unitX, out unitY);

    private static bool TryArcDistanceFromCusp(
        EdgeGeometry arcGeometry,
        bool fromStart,
        PointTicks point,
        out double distance)
    {
        distance = 0d;
        if (!arcGeometry.IsArc ||
            arcGeometry.Radius <= 0d ||
            arcGeometry.Sweep == 0d)
        {
            return false;
        }

        var originAngle = fromStart
            ? Angle(arcGeometry.Center, arcGeometry.Start)
            : Angle(arcGeometry.Center, arcGeometry.End);
        var pointAngle = Angle(arcGeometry.Center, point);
        var direction = fromStart
            ? Math.Sign(arcGeometry.Sweep)
            : -Math.Sign(arcGeometry.Sweep);
        var travelSweep = direction >= 0d
            ? PositiveSweep(originAngle, pointAngle)
            : -NegativeSweep(originAngle, pointAngle);
        if (travelSweep > Math.Abs(arcGeometry.Sweep) + 0.000001d)
        {
            return false;
        }

        distance = travelSweep * arcGeometry.Radius;
        return true;
    }

    private static bool TryApproximateRoundedCenter(
        EdgeGeometry incomingGeometry,
        EdgeGeometry outgoingGeometry,
        long radiusTicks,
        out PointTicks center)
    {
        center = default;
        var cusp = incomingGeometry.End;
        var incomingLength = incomingGeometry.LengthFromEndToStart();
        var outgoingLength = outgoingGeometry.LengthFromStartToEnd();
        if (incomingLength <= 0d ||
            outgoingLength <= 0d ||
            !incomingGeometry.TryDirectionAwayFromEnd(out var incomingAwayX, out var incomingAwayY) ||
            !outgoingGeometry.TryDirectionAwayFromStart(out var outgoingAwayX, out var outgoingAwayY))
        {
            return false;
        }

        var dot = Math.Clamp(
            incomingAwayX * outgoingAwayX + incomingAwayY * outgoingAwayY,
            -0.999,
            0.999);
        var theta = Math.Acos(dot);
        var halfTheta = theta / 2.0;
        var tangentDistance = Math.Min(radiusTicks, Math.Min(incomingLength, outgoingLength) * 0.72d);
        var effectiveRadius = tangentDistance * Math.Tan(halfTheta);
        var bisectorX = incomingAwayX + outgoingAwayX;
        var bisectorY = incomingAwayY + outgoingAwayY;
        var bisectorLength = Math.Sqrt(bisectorX * bisectorX + bisectorY * bisectorY);
        if (tangentDistance <= 0d ||
            effectiveRadius <= 0d ||
            bisectorLength <= 0d)
        {
            return false;
        }

        bisectorX /= bisectorLength;
        bisectorY /= bisectorLength;
        var centerDistance = effectiveRadius / Math.Sin(halfTheta);
        center = new PointTicks(
            cusp.X + bisectorX * centerDistance,
            cusp.Y + bisectorY * centerDistance);
        return true;
    }

    private static bool TryCreateRoundedJoin(
        OrientedBoundaryEdge incoming,
        OrientedBoundaryEdge outgoing,
        IReadOnlyDictionary<ulong, SharedFrameArcRead> arcBySpanSource,
        IReadOnlyDictionary<ulong, SharedFrameArcSectionRead> sectionBySpanSource,
        long radiusTicks,
        out FramePointTicks incomingTrim,
        out FramePointTicks outgoingTrim,
        out IElement arc)
    {
        incomingTrim = default;
        outgoingTrim = default;
        arc = default!;
        if (!TryReadEdgeGeometry(incoming, arcBySpanSource, sectionBySpanSource, out var incomingGeometry) ||
            !TryReadEdgeGeometry(outgoing, arcBySpanSource, sectionBySpanSource, out var outgoingGeometry) ||
            !SamePoint(incomingGeometry.End, outgoingGeometry.Start))
        {
            return false;
        }

        if (!incomingGeometry.IsArc &&
            !outgoingGeometry.IsArc)
        {
            return TryCreateLineLineRound(incoming, outgoing, radiusTicks, out incomingTrim, out outgoingTrim, out arc);
        }

        if (TryCreateLineArcRound(
                incomingGeometry,
                outgoingGeometry,
                radiusTicks,
                out incomingTrim,
                out outgoingTrim,
                out arc))
        {
            return true;
        }

        var cusp = incomingGeometry.End;
        var incomingLength = incomingGeometry.LengthFromEndToStart();
        var outgoingLength = outgoingGeometry.LengthFromStartToEnd();
        if (incomingLength <= 0d ||
            outgoingLength <= 0d ||
            !incomingGeometry.TryDirectionAwayFromEnd(out var incomingAwayX, out var incomingAwayY) ||
            !outgoingGeometry.TryDirectionAwayFromStart(out var outgoingAwayX, out var outgoingAwayY))
        {
            return false;
        }

        var dot = Math.Clamp(
            incomingAwayX * outgoingAwayX + incomingAwayY * outgoingAwayY,
            -0.999,
            0.999);
        var theta = Math.Acos(dot);
        var halfTheta = theta / 2.0;
        var requestedRadius = radiusTicks;
        var maxTangentDistance = Math.Min(incomingLength, outgoingLength) * 0.72;
        var tangentDistance = Math.Min(requestedRadius, maxTangentDistance);
        var effectiveRadius = tangentDistance * Math.Tan(halfTheta);
        var bisectorX = incomingAwayX + outgoingAwayX;
        var bisectorY = incomingAwayY + outgoingAwayY;
        var bisectorLength = Math.Sqrt(bisectorX * bisectorX + bisectorY * bisectorY);
        if (tangentDistance <= 0d ||
            effectiveRadius <= 0d ||
            bisectorLength <= 0d)
        {
            return false;
        }

        bisectorX /= bisectorLength;
        bisectorY /= bisectorLength;
        var centerDistance = effectiveRadius / Math.Sin(halfTheta);
        incomingTrim = incomingGeometry.PointFromEnd(tangentDistance);
        outgoingTrim = outgoingGeometry.PointFromStart(tangentDistance);
        var center = new FramePointTicks(
            incoming.EndEndpoint.Point,
            Rounded(cusp.X + bisectorX * centerDistance),
            Rounded(cusp.Y + bisectorY * centerDistance));
        if (SameFramePoint(incomingTrim, incoming.EndEndpoint.Point) ||
            SameFramePoint(outgoingTrim, outgoing.StartEndpoint.Point))
        {
            return false;
        }

        arc = CreateArcSection(center, incomingTrim, outgoingTrim);
        return true;
    }

    private static IElement CreateRetainedArcSection(
        SharedFrameArcRead sourceArc,
        IReadOnlyDictionary<ulong, SharedFrameArcSectionRead> sectionBySpanSource,
        Relation start,
        Relation end)
    {
        var center = (Relation)sourceArc.Center.Source;
        if (sectionBySpanSource.TryGetValue(SourceId(sourceArc.Span.Source), out var section) &&
            !SameFramePoint(section.Through.Source, start) &&
            !SameFramePoint(section.Through.Source, end))
        {
            return GraphicStructure.FrameArcSection(center, start, (Relation)section.Through.Source, end);
        }

        return CreateArcSection(center, start, end);
    }

    private static IElement CreateArcSection(FramePointTicks center, FramePointTicks start, FramePointTicks end) =>
        GraphicStructure.FrameArcSection(
            CreateFramePoint(center),
            CreateFramePoint(start),
            CreateFramePoint(ArcMidpoint(start.Template, ToPoint(center), ToPoint(start), ToPoint(end))),
            CreateFramePoint(end));

    private static IElement CreateArcSection(Relation center, Relation start, Relation end)
    {
        if (!TryReadFirstSharedFramePoint(center, out var centerPoint) ||
            !TryReadFirstSharedFramePoint(start, out var startPoint) ||
            !TryReadFirstSharedFramePoint(end, out var endPoint) ||
            !TryReadPointTicks(centerPoint, out var centerX, out var centerY) ||
            !TryReadPointTicks(startPoint, out var startX, out var startY) ||
            !TryReadPointTicks(endPoint, out var endX, out var endY))
        {
            return GraphicStructure.FrameArc(center, start, end);
        }

        var through = CreateFramePoint(ArcMidpoint(
            startPoint,
            new PointTicks(centerX, centerY),
            new PointTicks(startX, startY),
            new PointTicks(endX, endY)));
        if (SameFramePoint(through, start) ||
            SameFramePoint(through, end) ||
            SameFramePoint(through, center))
        {
            return GraphicStructure.FrameArc(center, start, end);
        }

        return GraphicStructure.FrameArcSection(center, start, through, end);
    }

    private static FramePointTicks ArcMidpoint(
        SharedFramePointRead template,
        PointTicks center,
        PointTicks start,
        PointTicks end)
    {
        var startAngle = Angle(center, start);
        var sweep = ShortestSweep(startAngle, Angle(center, end));
        var radius = (Distance(center, start) + Distance(center, end)) * 0.5d;
        var midpointAngle = startAngle + sweep * 0.5d;
        return new FramePointTicks(
            template,
            Rounded(center.X + Math.Cos(midpointAngle) * radius),
            Rounded(center.Y + Math.Sin(midpointAngle) * radius));
    }

    private static PointTicks ToPoint(FramePointTicks point) =>
        new(point.X, point.Y);

    private static bool TryReadEdgeGeometry(
        OrientedBoundaryEdge edge,
        IReadOnlyDictionary<ulong, SharedFrameArcRead> arcBySpanSource,
        IReadOnlyDictionary<ulong, SharedFrameArcSectionRead> sectionBySpanSource,
        out EdgeGeometry geometry)
    {
        geometry = default!;
        if (!TryReadPointTicks(edge.StartEndpoint.Point, out var startX, out var startY) ||
            !TryReadPointTicks(edge.EndEndpoint.Point, out var endX, out var endY))
        {
            return false;
        }

        var start = new PointTicks(startX, startY);
        var end = new PointTicks(endX, endY);
        var segmentId = SourceId(edge.Segment.Source);
        if (arcBySpanSource.TryGetValue(segmentId, out var arc) &&
            TryReadPointTicks(arc.Center, out var centerX, out var centerY))
        {
            var center = new PointTicks(centerX, centerY);
            var radius = (Distance(center, start) + Distance(center, end)) * 0.5d;
            if (radius <= 0d)
            {
                return false;
            }

            var sweep = sectionBySpanSource.TryGetValue(segmentId, out var section) &&
                TryReadPointTicks(section.Through, out var throughX, out var throughY)
                    ? SweepThrough(
                        Angle(center, start),
                        Angle(center, new PointTicks(throughX, throughY)),
                        Angle(center, end))
                    : ShortestSweep(Angle(center, start), Angle(center, end));
            geometry = new EdgeGeometry(
                edge.StartEndpoint.Point,
                IsArc: true,
                start,
                end,
                center,
                radius,
                sweep);
            return true;
        }

        geometry = new EdgeGeometry(
            edge.StartEndpoint.Point,
            IsArc: false,
            start,
            end,
            default,
            Radius: 0d,
            Sweep: 0d);
        return Distance(start, end) > 0d;
    }

    private static bool TryReadOrientedBoundaryEdges(
        ClosedSharedFrameBoundaryRead boundary,
        out IReadOnlyList<OrientedBoundaryEdge> edges)
    {
        if (boundary.Segments.Count == 0)
        {
            edges = [];
            return false;
        }

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

    private static IElement BoundaryStructure(ClosedSharedFrameBoundaryRead boundary)
    {
        if (boundary.Circles is { Count: > 0 })
        {
            return Chain([.. boundary.Circles.Select(circle => circle.Source)]);
        }

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

    private static IReadOnlyList<SharedFrameSegmentEndpointRead>? FindSite(
        IEnumerable<IReadOnlyList<SharedFrameSegmentEndpointRead>> sites,
        SharedFrameSegmentEndpointRead endpoint)
    {
        var signature = EndpointSignature(endpoint);
        return sites.FirstOrDefault(site => site.Any(candidate => EndpointSignature(candidate) == signature));
    }

    private static SharedFrameSegmentEndpointRead OtherEndpoint(SharedFrameSegmentEndpointRead endpoint) =>
        endpoint.IsStart
            ? new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.End, IsStart: false)
            : new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.Start, IsStart: true);

    private static bool TryReadFirstSharedFramePoint(IElement source, out SharedFramePointRead point)
    {
        point = GraphicReadGraph.From(source).ReadsOf<SharedFramePointRead>().FirstOrDefault()!;
        return point is not null;
    }

    private static bool TryReadFirstSharedFrameSegment(IElement source, out SharedFrameSegmentRead segment)
    {
        segment = GraphicReadGraph.From(source).ReadsOf<SharedFrameSegmentRead>().FirstOrDefault()!;
        return segment is not null;
    }

    private static bool TryReadPointTicks(SharedFramePointRead point, out long x, out long y)
    {
        if (point.Coordinates.Count >= 2)
        {
            x = point.Coordinates[0].Ticks;
            y = point.Coordinates[1].Ticks;
            return true;
        }

        x = 0;
        y = 0;
        return false;
    }

    private static Relation CreateFramePoint(FramePointTicks point)
    {
        var coordinates = point.Template.Coordinates
            .Select((coordinate, index) => new CarrierLocation(
                coordinate.Carrier,
                index == 0 ? point.X : index == 1 ? point.Y : coordinate.Ticks))
            .ToArray();
        return GraphicStructure.FramePoint(point.Template.Holder, coordinates);
    }

    private static bool SameFramePoint(IElement left, IElement right) =>
        GraphicReadGraph.From(left).ReadsOf<SharedFramePointRead>().FirstOrDefault() is { } leftPoint &&
        GraphicReadGraph.From(right).ReadsOf<SharedFramePointRead>().FirstOrDefault() is { } rightPoint &&
        SameFramePoint(leftPoint, rightPoint);

    private static bool SameFramePoint(FramePointTicks left, SharedFramePointRead right) =>
        right.Coordinates.Count >= 2 &&
        left.X == right.Coordinates[0].Ticks &&
        left.Y == right.Coordinates[1].Ticks;

    private static bool SameFramePoint(FramePointTicks left, PointTicks right) =>
        left.X == Rounded(right.X) &&
        left.Y == Rounded(right.Y);

    private static bool SamePoint(PointTicks left, PointTicks right) =>
        Distance(left, right) < 0.01d;

    private static double Distance(PointTicks left, PointTicks right)
    {
        var deltaX = left.X - right.X;
        var deltaY = left.Y - right.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static bool TryUnit(PointTicks from, PointTicks to, out double unitX, out double unitY)
    {
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (length <= 0d)
        {
            unitX = 0d;
            unitY = 0d;
            return false;
        }

        unitX = deltaX / length;
        unitY = deltaY / length;
        return true;
    }

    private static bool SameFramePoint(SharedFramePointRead left, SharedFramePointRead right) =>
        left.Coordinates.Count >= 2 &&
        right.Coordinates.Count >= 2 &&
        left.Holder.SameIdentityAs(right.Holder) &&
        left.Coordinates[0].Carrier.SameIdentityAs(right.Coordinates[0].Carrier) &&
        left.Coordinates[1].Carrier.SameIdentityAs(right.Coordinates[1].Carrier) &&
        left.Coordinates[0].Ticks == right.Coordinates[0].Ticks &&
        left.Coordinates[1].Ticks == right.Coordinates[1].Ticks;

    private static string EndpointSignature(SharedFrameSegmentEndpointRead endpoint) =>
        $"{SourceId(endpoint.Segment.Source)}:{endpoint.IsStart}";

    private static long Rounded(double value) =>
        (long)Math.Round(value, MidpointRounding.AwayFromZero);

    private static double Angle(PointTicks center, PointTicks point) =>
        Math.Atan2(point.Y - center.Y, point.X - center.X);

    private static double ShortestSweep(double startAngle, double endAngle)
    {
        var sweep = endAngle - startAngle;
        while (sweep <= -Math.PI)
        {
            sweep += Math.PI * 2d;
        }

        while (sweep > Math.PI)
        {
            sweep -= Math.PI * 2d;
        }

        return sweep;
    }

    private static double SweepThrough(double startAngle, double throughAngle, double endAngle)
    {
        var positiveThrough = PositiveSweep(startAngle, throughAngle);
        var positiveEnd = PositiveSweep(startAngle, endAngle);
        if (positiveThrough <= positiveEnd + 0.000001d)
        {
            return positiveEnd;
        }

        var negativeThrough = NegativeSweep(startAngle, throughAngle);
        var negativeEnd = NegativeSweep(startAngle, endAngle);
        if (negativeThrough >= negativeEnd - 0.000001d)
        {
            return negativeEnd;
        }

        return ShortestSweep(startAngle, endAngle);
    }

    private static double PositiveSweep(double startAngle, double endAngle)
    {
        var sweep = endAngle - startAngle;
        while (sweep < 0d)
        {
            sweep += Math.PI * 2d;
        }

        return sweep;
    }

    private static double NegativeSweep(double startAngle, double endAngle)
    {
        var sweep = endAngle - startAngle;
        while (sweep > 0d)
        {
            sweep -= Math.PI * 2d;
        }

        return sweep;
    }

    private static ulong SourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

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
