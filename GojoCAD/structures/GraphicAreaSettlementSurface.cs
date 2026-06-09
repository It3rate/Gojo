using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Local area-boundary settlement for shared-frame segment and circular-arc
/// participants. The numeric work here is a read: the result is emitted as
/// ordinary Gojo area and outline structure.
/// </summary>
public static class GraphicAreaSettlementSurface
{
    private const double Epsilon = 0.01;
    private const double SampleDistance = 0.55;
    private const double EndpointSettlementDistance = 1.5;
    private const double MixedLoopMinimumArea = 256d;

    /// <summary>
    /// Settles an area frame into positive and anti area loops by splitting
    /// segment/arc boundaries at intersections and keeping only fragments that
    /// separate filled from unfilled space.
    /// </summary>
    public static Relation SettledAreaFrame(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicBooleanSurface.AreaFrame(RootOrNew(root), [.. settlement.ResolvedAreaFills]);
    }

    /// <summary>
    /// Settles an area frame but emits only positive area loops. Unfilled loops
    /// remain available through settled outline reads.
    /// </summary>
    public static Relation SettledPositiveAreaFrame(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicBooleanSurface.AreaFrame(RootOrNew(root), [.. settlement.PositiveAreaFills]);
    }

    /// <summary>
    /// Settles an area frame into a positive area union whose outline witness
    /// carries both exterior and unfilled interior loops.
    /// </summary>
    public static Relation SettledPositiveAreaUnion(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        var settledAreaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), [.. settlement.AreaFills]);
        var outlineFrame = GraphicOutlineSurface.OutlineFrame(new Carrier(), [.. settlement.Outlines]);
        return GraphicBooleanSurface.AreaUnion(RootOrNew(root), settledAreaFrame, outlineFrame);
    }

    /// <summary>Reads the settled boundary loops from an area frame.</summary>
    public static Relation SettledBoundaryOutlinesFromAreas(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicOutlineSurface.OutlineFrame(RootOrNew(root), [.. settlement.Outlines]);
    }

    /// <summary>Reads only the settled positive boundary loops from an area frame.</summary>
    public static Relation SettledPositiveBoundaryOutlinesFromAreas(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicOutlineSurface.OutlineFrame(RootOrNew(root), [.. settlement.PositiveOutlines]);
    }

    /// <summary>Reads only the settled unfilled boundary loops from an area frame.</summary>
    public static Relation SettledAntiBoundaryOutlinesFromAreas(Carrier root, IElement areaFrame)
    {
        var settlement = CreateSettlement(areaFrame);
        return GraphicOutlineSurface.OutlineFrame(RootOrNew(root), [.. settlement.AntiOutlines]);
    }

    private static SettlementRead CreateSettlement(IElement areaFrame)
    {
        if (areaFrame is not Relation frame ||
            !GraphicBooleanSurface.TryReadAreaFrame(frame, out var participantElements) ||
            participantElements.Length == 0)
        {
            return EmptySettlement();
        }

        var participants = participantElements
            .Select(ReadParticipant)
            .Where(participant => participant.Positive.Count != 0 || participant.Negative.Count != 0)
            .ToArray();
        if (participants.Length == 0)
        {
            return EmptySettlement();
        }

        var edges = participants
            .SelectMany(participant => participant.Positive.Concat(participant.Negative))
            .SelectMany(boundary => boundary.Edges)
            .ToArray();
        if (edges.Length == 0)
        {
            return EmptySettlement();
        }

        var splitParameters = edges.ToDictionary(edge => edge, _ => new List<double> { 0d, 1d });
        for (var leftIndex = 0; leftIndex < edges.Length; leftIndex++)
        for (var rightIndex = leftIndex + 1; rightIndex < edges.Length; rightIndex++)
        {
            foreach (var intersection in Intersections(edges[leftIndex], edges[rightIndex]))
            {
                if (edges[leftIndex].TryParameterOf(intersection, out var leftT))
                {
                    splitParameters[edges[leftIndex]].Add(leftT);
                }

                if (edges[rightIndex].TryParameterOf(intersection, out var rightT))
                {
                    splitParameters[edges[rightIndex]].Add(rightT);
                }
            }
        }

        var fragments = new List<FragmentRead>();
        foreach (var edge in edges)
        {
            var parameters = splitParameters[edge]
                .Where(t => t >= -Epsilon && t <= 1d + Epsilon)
                .Select(t => Math.Clamp(t, 0d, 1d))
                .DistinctBy(t => Math.Round(t, 6))
                .Order()
                .ToArray();
            for (var index = 0; index + 1 < parameters.Length; index++)
            {
                var startT = parameters[index];
                var endT = parameters[index + 1];
                if (endT - startT < Epsilon)
                {
                    continue;
                }

                var fragment = edge.Fragment(startT, endT);
                var leftInside = Contains(participants, fragment.LeftSample());
                var rightInside = Contains(participants, fragment.RightSample());
                if (leftInside == rightInside)
                {
                    continue;
                }

                fragments.Add(fragment.WithSides(leftInside, rightInside));
            }
        }

        var settledFragments = SettleFragmentEndpoints(fragments);
        var frontierFragments = settledFragments
            .DistinctBy(FragmentKey)
            .ToArray();
        var loops = BuildOneSidedBoundaryLoops(frontierFragments);
        var outlines = new List<IElement>();
        var positiveOutlines = new List<IElement>();
        var antiOutlines = new List<IElement>();
        var fills = new List<IElement>();
        var positiveFills = new List<IElement>();
        var antiFills = new List<IElement>();
        foreach (var loop in loops)
        {
            var signedArea = SignedArea(loop);
            if (Math.Abs(signedArea) < Epsilon)
            {
                continue;
            }

            var isFilledLeft = loop.All(fragment => fragment.LeftInside);
            var isUnfilledLeft = loop.All(fragment => !fragment.LeftInside);
            var isConsistentSide = isFilledLeft || isUnfilledLeft;
            var isPositive = Contains(participants, InteriorSample(loop, signedArea));
            if (!isConsistentSide &&
                (!isPositive || Math.Abs(signedArea) < MixedLoopMinimumArea))
            {
                continue;
            }

            var orientedLoop = OrientLoop(loop, isPositive, signedArea);
            var elements = orientedLoop.Select(fragment => fragment.ToElement()).ToArray();
            if (elements.Length < 3)
            {
                continue;
            }

            var outline = Chain(elements);
            outlines.Add(outline);
            if (isPositive)
            {
                positiveOutlines.Add(outline);
                var fill = GraphicStructure.AreaFill(outline);
                fills.Add(fill);
                positiveFills.Add(fill);
            }
            else
            {
                antiOutlines.Add(outline);
                var fill = GraphicStructure.AntiAreaFill(outline);
                fills.Add(fill);
                antiFills.Add(fill);
            }
        }

        var resolvedAreaFills = fills.ToArray();
        var resolvedPositiveAreaFills = positiveFills.ToArray();
        var resolvedAntiAreaFills = antiFills.ToArray();

        var emittedPositiveBounds = fills
            .Where(HasPositiveAreaRead)
            .SelectMany(ReadAreaBounds)
            .ToArray();
        var emittedAntiBounds = fills
            .Where(HasAntiAreaRead)
            .SelectMany(ReadAreaBounds)
            .ToArray();

        static bool IsUnresolved(IElement source, IReadOnlyList<BoundsRead> emittedBounds) =>
            !TryReadFirstAreaBounds(source, out var sourceBounds) ||
            !emittedBounds.Any(bounds => bounds.Overlaps(sourceBounds));

        // A partially emitted anti loop is still residue; touching the source
        // does not settle the unfilled shape that remains.
        static bool IsUncovered(IElement source, IReadOnlyList<BoundsRead> emittedBounds) =>
            !TryReadFirstAreaBounds(source, out var sourceBounds) ||
            !emittedBounds.Any(bounds => bounds.Contains(sourceBounds));

        var residue = new List<IElement>();
        var positiveResidueFills = new List<IElement>();
        var antiResidueFills = new List<IElement>();
        foreach (var participant in participants)
        {
            var positiveResidue = participant.PositiveSources
                .Where(source => IsUnresolved(source, emittedPositiveBounds))
                .DistinctBy(SourceId)
                .ToArray();
            var antiResidue = participant.NegativeSources
                .Where(source => IsUncovered(source, emittedAntiBounds))
                .DistinctBy(SourceId)
                .ToArray();
            if (antiResidue.Length > 0 &&
                participant.Positive.Count > 0)
            {
                residue.Add(participant.Source);
                positiveResidueFills.AddRange(participant.PositiveSources);
                continue;
            }

            residue.AddRange(positiveResidue);
            residue.AddRange(antiResidue);
            positiveResidueFills.AddRange(positiveResidue);
            antiResidueFills.AddRange(antiResidue);
        }

        var distinctResidue = residue
            .DistinctBy(SourceId)
            .ToArray();
        if (distinctResidue.Length > 0)
        {
            fills.InsertRange(0, distinctResidue);
        }

        var distinctPositiveResidue = positiveResidueFills
            .DistinctBy(SourceId)
            .ToArray();
        if (distinctPositiveResidue.Length > 0)
        {
            positiveFills.InsertRange(0, distinctPositiveResidue);
        }

        var distinctAntiResidue = antiResidueFills
            .DistinctBy(SourceId)
            .ToArray();
        if (distinctAntiResidue.Length > 0)
        {
            antiFills.InsertRange(0, distinctAntiResidue);
            foreach (var outline in distinctAntiResidue
                .SelectMany(AntiAreaBoundaryOutlines)
                .DistinctBy(SourceId))
            {
                if (!ContainsSource(outlines, outline))
                {
                    outlines.Add(outline);
                }

                if (!ContainsSource(antiOutlines, outline))
                {
                    antiOutlines.Add(outline);
                }
            }
        }

        return new SettlementRead(
            [.. fills],
            [.. positiveFills],
            [.. antiFills],
            [.. outlines],
            [.. positiveOutlines],
            [.. antiOutlines],
            resolvedAreaFills,
            resolvedPositiveAreaFills,
            resolvedAntiAreaFills);
    }

    private static SettlementRead EmptySettlement() =>
        new([], [], [], [], [], [], [], [], []);

    private static ParticipantRead ReadParticipant(IElement participant)
    {
        var graph = GraphicReadGraph.From(participant);
        var positive = new List<BoundaryRead>();
        var negative = new List<BoundaryRead>();
        var positiveSources = new List<IElement>();
        var negativeSources = new List<IElement>();
        foreach (var area in graph.ReadsOf<SharedFrameAreaRead>()
            .DistinctBy(area => SourceId(area.Source)))
        {
            if (!TryReadBoundary(area.Boundary, area.IsAntiArea, out var boundary))
            {
                continue;
            }

            if (area.IsAntiArea)
            {
                negative.Add(boundary);
                negativeSources.Add(area.Source);
            }
            else
            {
                positive.Add(boundary);
                positiveSources.Add(area.Source);
            }
        }

        return new ParticipantRead(participant, [.. positive], [.. negative], [.. positiveSources], [.. negativeSources]);
    }

    private static bool HasPositiveAreaRead(IElement element) =>
        GraphicReadGraph.From(element)
            .ReadsOf<SharedFrameAreaRead>()
            .Any(area => !area.IsAntiArea);

    private static bool HasAntiAreaRead(IElement element) =>
        GraphicReadGraph.From(element)
            .ReadsOf<SharedFrameAreaRead>()
            .Any(area => area.IsAntiArea);

    private static IEnumerable<IElement> AntiAreaBoundaryOutlines(IElement element)
    {
        foreach (var area in GraphicReadGraph.From(element)
            .ReadsOf<SharedFrameAreaRead>()
            .Where(area => area.IsAntiArea)
            .DistinctBy(area => SourceId(area.Source)))
        {
            yield return ReadableBoundaryOutline(area);
        }
    }

    private static IElement ReadableBoundaryOutline(SharedFrameAreaRead area)
    {
        if (GraphicReadGraph.From(area.Source).ReadsOf<ClosedSharedFrameBoundaryRead>().Any())
        {
            return area.Source;
        }

        if (GraphicReadGraph.From(area.Boundary.Source).ReadsOf<ClosedSharedFrameBoundaryRead>().Any())
        {
            return area.Boundary.Source;
        }

        return BoundaryStructure(area.Boundary);
    }

    private static bool ContainsSource(IReadOnlyList<IElement> elements, IElement source) =>
        elements.Any(element => SourceId(element) == SourceId(source));

    private static IEnumerable<BoundsRead> ReadAreaBounds(IElement element)
    {
        foreach (var area in GraphicReadGraph.From(element).ReadsOf<SharedFrameAreaRead>())
        {
            if (TryReadBoundaryBounds(area.Boundary, out var bounds))
            {
                yield return bounds;
            }
        }
    }

    private static bool TryReadFirstAreaBounds(IElement element, out BoundsRead bounds)
    {
        foreach (var area in GraphicReadGraph.From(element).ReadsOf<SharedFrameAreaRead>())
        {
            if (TryReadBoundaryBounds(area.Boundary, out bounds))
            {
                return true;
            }
        }

        bounds = default;
        return false;
    }

    private static bool TryReadBoundaryBounds(ClosedSharedFrameBoundaryRead boundary, out BoundsRead bounds)
    {
        var points = TryReadBoundary(boundary, isAnti: false, out var read)
            ? read.Edges.SelectMany(edge => edge.Flatten()).ToArray()
            : boundary.Segments
                .SelectMany(segment => new[] { segment.Start, segment.End })
                .Where(point => point.Coordinates.Count >= 2)
                .Select(point => new Point2(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks))
                .ToArray();
        if (points.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = new BoundsRead(
            points.Min(point => point.X),
            points.Max(point => point.X),
            points.Min(point => point.Y),
            points.Max(point => point.Y));
        return true;
    }

    private static bool TryReadBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        bool isAnti,
        out BoundaryRead read)
    {
        read = default!;
        var oriented = TryReadOrientedBoundaryEdges(boundary, out var walked)
            ? walked
            : TryReadCoordinateBoundaryEdges(boundary, out var coordinateWalked)
                ? coordinateWalked
            : boundary.Segments
                .Select(segment => new OrientedBoundaryEdge(
                    segment,
                    new SharedFrameSegmentEndpointRead(segment, segment.Start, IsStart: true),
                    new SharedFrameSegmentEndpointRead(segment, segment.End, IsStart: false)))
                .ToArray();

        var arcBySpanSource = (boundary.Arcs ?? [])
            .GroupBy(arc => SourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var edges = new List<BoundaryEdge>();
        foreach (var edge in oriented)
        {
            if (arcBySpanSource.TryGetValue(SourceId(edge.Segment.Source), out var arc))
            {
                if (!TryCreateArcEdge(edge, arc, out var arcEdge))
                {
                    continue;
                }

                edges.Add(arcEdge);
            }
            else
            {
                if (!TryCreateLineEdge(edge, out var lineEdge))
                {
                    continue;
                }

                edges.Add(lineEdge);
            }
        }

        if (edges.Count < 3)
        {
            return false;
        }

        read = new BoundaryRead(isAnti, [.. edges]);
        return true;
    }

    private static IElement BoundaryStructure(ClosedSharedFrameBoundaryRead boundary)
    {
        var arcBySpanSource = (boundary.Arcs ?? [])
            .GroupBy(arc => SourceId(arc.Span.Source))
            .ToDictionary(group => group.Key, group => group.First());
        var elements = TryReadOrientedBoundaryEdges(boundary, out var oriented)
            ? oriented
                .Select(edge => arcBySpanSource.TryGetValue(SourceId(edge.Segment.Source), out var arc)
                    ? arc.Source
                    : edge.Segment.Source)
                .ToArray()
            : boundary.Segments
                .Select(segment => arcBySpanSource.TryGetValue(SourceId(segment.Source), out var arc)
                    ? arc.Source
                    : segment.Source)
                .ToArray();
        return Chain(elements);
    }

    private static bool TryCreateLineEdge(OrientedBoundaryEdge source, out BoundaryEdge edge)
    {
        edge = default!;
        if (!TryReadPoint(source.StartEndpoint.Point, out var start) ||
            !TryReadPoint(source.EndEndpoint.Point, out var end) ||
            NearlySame(start.Point, end.Point))
        {
            return false;
        }

        edge = BoundaryEdge.Line(start.Template, start.Point, end.Point);
        return true;
    }

    private static bool TryCreateArcEdge(
        OrientedBoundaryEdge source,
        SharedFrameArcRead arc,
        out BoundaryEdge edge)
    {
        edge = default!;
        if (!TryReadPoint(source.StartEndpoint.Point, out var start) ||
            !TryReadPoint(source.EndEndpoint.Point, out var end) ||
            !TryReadPoint(arc.Center, out var center) ||
            NearlySame(start.Point, end.Point))
        {
            return false;
        }

        edge = BoundaryEdge.Arc(start.Template, center.Point, start.Point, end.Point);
        return true;
    }

    private static IEnumerable<Point2> Intersections(BoundaryEdge left, BoundaryEdge right)
    {
        if (!left.IsArc && !right.IsArc)
        {
            return LineLineIntersections(left, right);
        }

        if (!left.IsArc && right.IsArc)
        {
            return LineArcIntersections(left, right);
        }

        if (left.IsArc && !right.IsArc)
        {
            return LineArcIntersections(right, left);
        }

        return ArcArcIntersections(left, right);
    }

    private static IEnumerable<Point2> LineLineIntersections(BoundaryEdge left, BoundaryEdge right)
    {
        var p = left.Start;
        var r = left.End - left.Start;
        var q = right.Start;
        var s = right.End - right.Start;
        var cross = Cross(r, s);
        if (Math.Abs(cross) < Epsilon)
        {
            if (Math.Abs(Cross(q - p, r)) >= Epsilon)
            {
                yield break;
            }

            var lengthSquared = Dot(r, r);
            if (lengthSquared < Epsilon)
            {
                yield break;
            }

            var t0 = Dot(q - p, r) / lengthSquared;
            var t1 = Dot(q + s - p, r) / lengthSquared;
            var overlapStart = Math.Max(0d, Math.Min(t0, t1));
            var overlapEnd = Math.Min(1d, Math.Max(t0, t1));
            if (overlapEnd < overlapStart - Epsilon)
            {
                yield break;
            }

            if (WithinUnit(overlapStart))
            {
                yield return left.PointAt(Math.Clamp(overlapStart, 0d, 1d));
            }

            if (WithinUnit(overlapEnd) &&
                Math.Abs(overlapEnd - overlapStart) > Epsilon)
            {
                yield return left.PointAt(Math.Clamp(overlapEnd, 0d, 1d));
            }

            yield break;
        }

        var t = Cross(q - p, s) / cross;
        var u = Cross(q - p, r) / cross;
        if (WithinUnit(t) && WithinUnit(u))
        {
            yield return p + r * t;
        }
    }

    private static IEnumerable<Point2> LineArcIntersections(BoundaryEdge line, BoundaryEdge arc)
    {
        var d = line.End - line.Start;
        var f = line.Start - arc.Center;
        var a = Dot(d, d);
        var b = 2d * Dot(f, d);
        var c = Dot(f, f) - arc.Radius * arc.Radius;
        var discriminant = b * b - 4d * a * c;
        if (discriminant < -Epsilon ||
            Math.Abs(a) < Epsilon)
        {
            yield break;
        }

        var root = Math.Sqrt(Math.Max(0d, discriminant));
        foreach (var t in new[] { (-b - root) / (2d * a), (-b + root) / (2d * a) })
        {
            var point = line.Start + d * t;
            if (WithinUnit(t) &&
                arc.TryParameterOf(point, out _))
            {
                yield return point;
            }
        }
    }

    private static IEnumerable<Point2> ArcArcIntersections(BoundaryEdge left, BoundaryEdge right)
    {
        var delta = right.Center - left.Center;
        var distance = Length(delta);
        if (distance < Epsilon &&
            Math.Abs(left.Radius - right.Radius) < Epsilon)
        {
            foreach (var point in new[] { left.Start, left.End, right.Start, right.End })
            {
                if (left.TryParameterOf(point, out _) &&
                    right.TryParameterOf(point, out _))
                {
                    yield return point;
                }
            }

            yield break;
        }

        if (distance < Epsilon ||
            distance > left.Radius + right.Radius + Epsilon ||
            distance < Math.Abs(left.Radius - right.Radius) - Epsilon)
        {
            yield break;
        }

        var a = (left.Radius * left.Radius - right.Radius * right.Radius + distance * distance) / (2d * distance);
        var hSquared = left.Radius * left.Radius - a * a;
        if (hSquared < -Epsilon)
        {
            yield break;
        }

        var h = Math.Sqrt(Math.Max(0d, hSquared));
        var direction = delta / distance;
        var basePoint = left.Center + direction * a;
        var normal = new Point2(-direction.Y, direction.X);
        foreach (var point in new[] { basePoint + normal * h, basePoint - normal * h })
        {
            if (left.TryParameterOf(point, out _) &&
                right.TryParameterOf(point, out _))
            {
                yield return point;
            }
        }
    }

    private static IReadOnlyList<IReadOnlyList<FragmentRead>> BuildOneSidedBoundaryLoops(IReadOnlyList<FragmentRead> fragments)
    {
        // Each fragment is already a local read of area on one orthogonal side
        // and no-fill on the other; orient it so the filled side is walkable.
        var directed = fragments
            .Where(fragment => fragment.LeftInside != fragment.RightInside)
            .Select((fragment, index) => new BoundaryFragmentRead(
                index,
                fragment.LeftInside ? fragment : fragment.Reversed()))
            .ToArray();
        var outgoingByStart = directed
            .GroupBy(edge => PointKey(edge.Fragment.Start))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(edge => DirectionAngle(edge.Fragment.TangentAtStart()))
                    .ToArray());
        var used = new HashSet<int>();
        var loops = new List<IReadOnlyList<FragmentRead>>();
        foreach (var seed in directed)
        {
            if (used.Contains(seed.FragmentIndex))
            {
                continue;
            }

            var loop = new List<BoundaryFragmentRead>();
            var loopIndices = new HashSet<int>();
            var current = seed;
            var startKey = PointKey(seed.Fragment.Start);
            var isClosed = false;
            var guard = 0;
            while (guard++ < directed.Length + 2)
            {
                if (used.Contains(current.FragmentIndex) ||
                    !loopIndices.Add(current.FragmentIndex))
                {
                    break;
                }

                loop.Add(current);
                var endKey = PointKey(current.Fragment.End);
                if (endKey == startKey)
                {
                    isClosed = true;
                    break;
                }

                if (!TryChooseNextBoundaryEdge(current, outgoingByStart, used, loopIndices, out var next))
                {
                    break;
                }

                current = next;
            }

            foreach (var edge in loop)
            {
                used.Add(edge.FragmentIndex);
            }

            if (isClosed && loop.Count >= 3)
            {
                loops.Add(loop.Select(edge => edge.Fragment).ToArray());
            }
        }

        return loops;
    }

    private static IReadOnlyList<FragmentRead> SettleFragmentEndpoints(IReadOnlyList<FragmentRead> fragments)
    {
        // Intersection reads from different carriers can differ by sub-tick
        // residue. Settle nearby endpoints into one shared reference point.
        var settledPoints = new List<Point2>();

        Point2 Settle(Point2 point)
        {
            foreach (var settled in settledPoints)
            {
                if (Length(settled - point) <= EndpointSettlementDistance)
                {
                    return settled;
                }
            }

            settledPoints.Add(point);
            return point;
        }

        return [.. fragments.Select(fragment => fragment.WithEndpoints(
            Settle(fragment.Start),
            Settle(fragment.End)))];
    }

    private static bool TryChooseNextBoundaryEdge(
        BoundaryFragmentRead current,
        IReadOnlyDictionary<string, BoundaryFragmentRead[]> outgoingByStart,
        IReadOnlySet<int> used,
        IReadOnlySet<int> loopIndices,
        out BoundaryFragmentRead next)
    {
        if (!outgoingByStart.TryGetValue(PointKey(current.Fragment.End), out var outgoing))
        {
            next = default;
            return false;
        }

        var candidates = outgoing
            .Where(candidate => !used.Contains(candidate.FragmentIndex) && !loopIndices.Contains(candidate.FragmentIndex))
            .ToArray();
        if (candidates.Length == 0)
        {
            next = default;
            return false;
        }

        var incomingAngle = DirectionAngle(current.Fragment.TangentAtEnd());
        next = candidates
            .OrderBy(candidate => AbsoluteTurn(incomingAngle, DirectionAngle(candidate.Fragment.TangentAtStart())))
            .First();
        return true;
    }

    private static Point2 InteriorSample(IReadOnlyList<FragmentRead> loop, double signedArea)
    {
        return signedArea >= 0d
            ? loop[0].LeftSample()
            : loop[0].RightSample();
    }

    private static IReadOnlyList<FragmentRead> OrientLoop(
        IReadOnlyList<FragmentRead> loop,
        bool isPositive,
        double signedArea)
    {
        var shouldReverse = isPositive
            ? signedArea > 0d
            : signedArea < 0d;
        return shouldReverse
            ? [.. loop.Reverse().Select(fragment => fragment.Reversed())]
            : loop;
    }

    private static double SignedArea(IReadOnlyList<FragmentRead> loop)
    {
        var points = new List<Point2>();
        foreach (var fragment in loop)
        {
            points.AddRange(fragment.Flatten());
        }

        if (points.Count < 3)
        {
            return 0d;
        }

        var area = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            area += current.X * next.Y - next.X * current.Y;
        }

        return area * 0.5d;
    }

    private static bool Contains(IReadOnlyList<ParticipantRead> participants, Point2 point)
    {
        var positive = false;
        var globalCut = false;
        foreach (var participant in participants)
        {
            if (participant.Positive.Count == 0)
            {
                if (participant.Negative.Any(boundary => boundary.Contains(point)))
                {
                    globalCut = true;
                }

                continue;
            }

            if (participant.Positive.Any(boundary => boundary.Contains(point)) &&
                !participant.Negative.Any(boundary => boundary.Contains(point)))
            {
                positive = true;
            }
        }

        return positive && !globalCut;
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

    private static bool TryReadCoordinateBoundaryEdges(
        ClosedSharedFrameBoundaryRead boundary,
        out IReadOnlyList<OrientedBoundaryEdge> edges)
    {
        edges = [];
        if (boundary.Segments.Count == 0 ||
            !TryPointKey(boundary.Segments[0].Start, out var startKey) ||
            !TryPointKey(boundary.Segments[0].End, out var currentKey))
        {
            return false;
        }

        var unused = boundary.Segments.Skip(1).ToList();
        var ordered = new List<OrientedBoundaryEdge>
        {
            new(
                boundary.Segments[0],
                new SharedFrameSegmentEndpointRead(boundary.Segments[0], boundary.Segments[0].Start, IsStart: true),
                new SharedFrameSegmentEndpointRead(boundary.Segments[0], boundary.Segments[0].End, IsStart: false))
        };

        while (unused.Count > 0)
        {
            var nextIndex = unused.FindIndex(segment =>
                TryPointKey(segment.Start, out var segmentStartKey) && segmentStartKey == currentKey ||
                TryPointKey(segment.End, out var segmentEndKey) && segmentEndKey == currentKey);
            if (nextIndex < 0)
            {
                return false;
            }

            var next = unused[nextIndex];
            unused.RemoveAt(nextIndex);
            if (TryPointKey(next.Start, out var nextStartKey) &&
                nextStartKey == currentKey &&
                TryPointKey(next.End, out var nextEndKey))
            {
                ordered.Add(new OrientedBoundaryEdge(
                    next,
                    new SharedFrameSegmentEndpointRead(next, next.Start, IsStart: true),
                    new SharedFrameSegmentEndpointRead(next, next.End, IsStart: false)));
                currentKey = nextEndKey;
                continue;
            }

            if (TryPointKey(next.Start, out var reverseEndKey))
            {
                ordered.Add(new OrientedBoundaryEdge(
                    next,
                    new SharedFrameSegmentEndpointRead(next, next.End, IsStart: false),
                    new SharedFrameSegmentEndpointRead(next, next.Start, IsStart: true)));
                currentKey = reverseEndKey;
            }
        }

        if (currentKey != startKey)
        {
            return false;
        }

        edges = ordered;
        return true;
    }

    private static bool TryPointKey(SharedFramePointRead point, out string key)
    {
        if (point.Coordinates.Count >= 2)
        {
            key = $"{point.Coordinates[0].Ticks}:{point.Coordinates[1].Ticks}";
            return true;
        }

        key = "";
        return false;
    }

    private static bool TryReadPoint(SharedFramePointRead point, out FramePointTicks ticks)
    {
        if (point.Coordinates.Count >= 2)
        {
            ticks = new FramePointTicks(
                point,
                new Point2(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks));
            return true;
        }

        ticks = default;
        return false;
    }

    private static Relation CreateFramePoint(SharedFramePointRead template, Point2 point)
    {
        var roundedX = Rounded(point.X);
        var roundedY = Rounded(point.Y);
        var coordinates = template.Coordinates
            .Select((coordinate, index) => new CarrierLocation(
                coordinate.Carrier,
                index == 0 ? roundedX : index == 1 ? roundedY : coordinate.Ticks))
            .ToArray();
        return GraphicStructure.FramePoint(template.Holder, coordinates);
    }

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            var slot = new Placeholder();
            return new Relation(slot, slot.Dual());
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }

    private static SharedFrameSegmentEndpointRead OtherEndpoint(SharedFrameSegmentEndpointRead endpoint) =>
        endpoint.IsStart
            ? new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.End, IsStart: false)
            : new SharedFrameSegmentEndpointRead(endpoint.Segment, endpoint.Segment.Start, IsStart: true);

    private static string EndpointSignature(SharedFrameSegmentEndpointRead endpoint) =>
        $"{SourceId(endpoint.Segment.Source)}:{(endpoint.IsStart ? "s" : "e")}";

    private static string PointKey(Point2 point) =>
        $"{Rounded(point.X)}:{Rounded(point.Y)}";

    private static string FragmentKey(FragmentRead fragment)
    {
        var key = $"{(fragment.IsArc ? "a" : "l")}:{PointKey(fragment.Start)}:{PointKey(fragment.End)}";
        return fragment.IsArc
            ? $"{key}:{PointKey(fragment.Center)}:{Rounded(fragment.Radius)}:{Rounded(fragment.Sweep * 1000000d)}"
            : key;
    }

    private static long Rounded(double value) =>
        (long)Math.Round(value, MidpointRounding.AwayFromZero);

    private static bool NearlySame(Point2 left, Point2 right) =>
        Length(left - right) < Epsilon;

    private static bool WithinUnit(double value) =>
        value >= -Epsilon && value <= 1d + Epsilon;

    private static double Cross(Point2 left, Point2 right) =>
        left.X * right.Y - left.Y * right.X;

    private static double Dot(Point2 left, Point2 right) =>
        left.X * right.X + left.Y * right.Y;

    private static double Length(Point2 point) =>
        Math.Sqrt(point.X * point.X + point.Y * point.Y);

    private static Point2 Unit(Point2 point)
    {
        var length = Length(point);
        return length < Epsilon
            ? new Point2(0d, 0d)
            : point / length;
    }

    private static double Angle(Point2 center, Point2 point) =>
        Math.Atan2(point.Y - center.Y, point.X - center.X);

    private static double DirectionAngle(Point2 direction) =>
        Math.Atan2(direction.Y, direction.X);

    private static double AbsoluteTurn(double fromAngle, double toAngle)
    {
        var turn = toAngle - fromAngle;
        while (turn <= -Math.PI)
        {
            turn += Math.PI * 2d;
        }

        while (turn > Math.PI)
        {
            turn -= Math.PI * 2d;
        }

        return Math.Abs(turn);
    }

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

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();

    private sealed record SettlementRead(
        IReadOnlyList<IElement> AreaFills,
        IReadOnlyList<IElement> PositiveAreaFills,
        IReadOnlyList<IElement> AntiAreaFills,
        IReadOnlyList<IElement> Outlines,
        IReadOnlyList<IElement> PositiveOutlines,
        IReadOnlyList<IElement> AntiOutlines,
        IReadOnlyList<IElement> ResolvedAreaFills,
        IReadOnlyList<IElement> ResolvedPositiveAreaFills,
        IReadOnlyList<IElement> ResolvedAntiAreaFills);

    private sealed record ParticipantRead(
        IElement Source,
        IReadOnlyList<BoundaryRead> Positive,
        IReadOnlyList<BoundaryRead> Negative,
        IReadOnlyList<IElement> PositiveSources,
        IReadOnlyList<IElement> NegativeSources);

    private sealed record BoundaryRead(bool IsAnti, IReadOnlyList<BoundaryEdge> Edges)
    {
        public bool Contains(Point2 point)
        {
            if (Edges.Count < 3)
            {
                return false;
            }

            var inside = false;
            foreach (var edge in Edges)
            {
                var flattened = edge.Flatten().ToArray();
                for (var index = 0; index + 1 < flattened.Length; index++)
                {
                    var current = flattened[index];
                    var next = flattened[index + 1];
                    if ((current.Y > point.Y) != (next.Y > point.Y) &&
                        point.X < (next.X - current.X) * (point.Y - current.Y) / (next.Y - current.Y + double.Epsilon) + current.X)
                    {
                        inside = !inside;
                    }
                }
            }

            return inside;
        }
    }

    private sealed record BoundaryEdge(
        SharedFramePointRead Template,
        bool IsArc,
        Point2 Start,
        Point2 End,
        Point2 Center,
        double Radius,
        double StartAngle,
        double Sweep)
    {
        public static BoundaryEdge Line(SharedFramePointRead template, Point2 start, Point2 end) =>
            new(template, IsArc: false, start, end, default, 0d, 0d, 0d);

        public static BoundaryEdge Arc(SharedFramePointRead template, Point2 center, Point2 start, Point2 end)
        {
            var startAngle = Angle(center, start);
            return new(
                template,
                IsArc: true,
                start,
                end,
                center,
                (Length(start - center) + Length(end - center)) * 0.5d,
                startAngle,
                ShortestSweep(startAngle, Angle(center, end)));
        }

        public bool TryParameterOf(Point2 point, out double t)
        {
            if (!IsArc)
            {
                var delta = End - Start;
                var lengthSquared = Dot(delta, delta);
                if (lengthSquared < Epsilon)
                {
                    t = 0d;
                    return false;
                }

                t = Dot(point - Start, delta) / lengthSquared;
                var projected = Start + delta * t;
                return WithinUnit(t) && Length(projected - point) <= 0.75d;
            }

            if (Math.Abs(Radius) < Epsilon ||
                Math.Abs(Length(point - Center) - Radius) > 0.85d)
            {
                t = 0d;
                return false;
            }

            var angle = Angle(Center, point);
            var deltaAngle = ShortestSweep(StartAngle, angle);
            t = Math.Abs(Sweep) < Epsilon ? 0d : deltaAngle / Sweep;
            return WithinUnit(t);
        }

        public FragmentRead Fragment(double startT, double endT) =>
            new(
                Template,
                IsArc,
                PointAt(startT),
                PointAt(endT),
                Center,
                Radius,
                StartAngle + Sweep * startT,
                Sweep * (endT - startT),
                LeftInside: false,
                RightInside: false);

        public Point2 PointAt(double t)
        {
            if (!IsArc)
            {
                return Start + (End - Start) * t;
            }

            var angle = StartAngle + Sweep * t;
            return new Point2(
                Center.X + Math.Cos(angle) * Radius,
                Center.Y + Math.Sin(angle) * Radius);
        }

        public IEnumerable<Point2> Flatten()
        {
            if (!IsArc)
            {
                yield return Start;
                yield return End;
                yield break;
            }

            var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(Sweep) / (Math.PI / 16d)));
            for (var index = 0; index <= steps; index++)
            {
                yield return PointAt(index / (double)steps);
            }
        }

    }

    private sealed record FragmentRead(
        SharedFramePointRead Template,
        bool IsArc,
        Point2 Start,
        Point2 End,
        Point2 Center,
        double Radius,
        double StartAngle,
        double Sweep,
        bool LeftInside,
        bool RightInside)
    {
        public FragmentRead WithSides(bool leftInside, bool rightInside) =>
            this with { LeftInside = leftInside, RightInside = rightInside };

        public FragmentRead WithEndpoints(Point2 start, Point2 end)
        {
            if (!IsArc)
            {
                return this with { Start = start, End = end };
            }

            var startAngle = Angle(Center, start);
            var sweep = ShortestSweep(startAngle, Angle(Center, end));
            if (Math.Abs(sweep) >= Epsilon &&
                Math.Sign(sweep) != Math.Sign(Sweep))
            {
                sweep += sweep > 0d ? -Math.PI * 2d : Math.PI * 2d;
            }

            return this with
            {
                Start = start,
                End = end,
                StartAngle = startAngle,
                Sweep = sweep
            };
        }

        public FragmentRead Reversed() =>
            new(Template, IsArc, End, Start, Center, Radius, StartAngle + Sweep, -Sweep, RightInside, LeftInside);

        public Point2 MidPoint() =>
            IsArc
                ? PointAt(0.5d)
                : Start + (End - Start) * 0.5d;

        public Point2 LeftSample()
        {
            var normal = LeftNormal();
            return MidPoint() + normal * SampleDistance;
        }

        public Point2 RightSample()
        {
            var normal = LeftNormal();
            return MidPoint() - normal * SampleDistance;
        }

        public IElement ToElement()
        {
            var start = CreateFramePoint(Template, Start);
            var end = CreateFramePoint(Template, End);
            if (!IsArc)
            {
                return GraphicStructure.FrameSegment(start, end);
            }

            return GraphicStructure.FrameArc(CreateFramePoint(Template, Center), start, end);
        }

        public IEnumerable<Point2> Flatten()
        {
            if (!IsArc)
            {
                yield return Start;
                yield return End;
                yield break;
            }

            var steps = Math.Max(2, (int)Math.Ceiling(Math.Abs(Sweep) / (Math.PI / 16d)));
            for (var index = 0; index <= steps; index++)
            {
                yield return PointAt(index / (double)steps);
            }
        }

        public Point2 TangentAtStart()
        {
            if (!IsArc)
            {
                return End - Start;
            }

            var sign = Math.Sign(Sweep == 0d ? 1d : Sweep);
            return new Point2(-Math.Sin(StartAngle) * sign, Math.Cos(StartAngle) * sign);
        }

        public Point2 TangentAtEnd()
        {
            if (!IsArc)
            {
                return End - Start;
            }

            var sign = Math.Sign(Sweep == 0d ? 1d : Sweep);
            var endAngle = StartAngle + Sweep;
            return new Point2(-Math.Sin(endAngle) * sign, Math.Cos(endAngle) * sign);
        }

        private Point2 PointAt(double t)
        {
            if (!IsArc)
            {
                return Start + (End - Start) * t;
            }

            var angle = StartAngle + Sweep * t;
            return new Point2(
                Center.X + Math.Cos(angle) * Radius,
                Center.Y + Math.Sin(angle) * Radius);
        }

        private Point2 LeftNormal()
        {
            var tangent = IsArc
                ? new Point2(
                    -Math.Sin(StartAngle + Sweep * 0.5d) * Math.Sign(Sweep == 0d ? 1d : Sweep),
                    Math.Cos(StartAngle + Sweep * 0.5d) * Math.Sign(Sweep == 0d ? 1d : Sweep))
                : End - Start;
            var length = Length(tangent);
            if (length < Epsilon)
            {
                return new Point2(0d, 0d);
            }

            tangent /= length;
            return new Point2(-tangent.Y, tangent.X);
        }
    }

    private readonly record struct FramePointTicks(SharedFramePointRead Template, Point2 Point);

    private readonly record struct OrientedBoundaryEdge(
        SharedFrameSegmentRead Segment,
        SharedFrameSegmentEndpointRead StartEndpoint,
        SharedFrameSegmentEndpointRead EndEndpoint);

    private readonly record struct BoundsRead(double MinX, double MaxX, double MinY, double MaxY)
    {
        public bool Contains(BoundsRead other) =>
            MinX <= other.MinX &&
            MaxX >= other.MaxX &&
            MinY <= other.MinY &&
            MaxY >= other.MaxY;

        public bool Overlaps(BoundsRead other) =>
            MinX <= other.MaxX &&
            MaxX >= other.MinX &&
            MinY <= other.MaxY &&
            MaxY >= other.MinY;
    }

    private readonly record struct BoundaryFragmentRead(
        int FragmentIndex,
        FragmentRead Fragment);

    private readonly record struct Point2(double X, double Y)
    {
        public static Point2 operator +(Point2 left, Point2 right) =>
            new(left.X + right.X, left.Y + right.Y);

        public static Point2 operator -(Point2 left, Point2 right) =>
            new(left.X - right.X, left.Y - right.Y);

        public static Point2 operator *(Point2 point, double scale) =>
            new(point.X * scale, point.Y * scale);

        public static Point2 operator /(Point2 point, double scale) =>
            new(point.X / scale, point.Y / scale);
    }

    private static ulong SourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;
}
