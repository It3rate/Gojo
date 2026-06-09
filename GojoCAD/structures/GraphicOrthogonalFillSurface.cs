using Gojo.Core;

namespace GojoCAD.Structures;

/// <summary>
/// Reads shared-frame line, arc, circle, and rectangle carriers as local
/// orthogonal fill bands. The generated output is ordinary Gojo area
/// structure; no area meaning is stored outside those emitted fills.
/// </summary>
public static class GraphicOrthogonalFillSurface
{
    public static Relation OrthogonalFillFrame(
        Carrier root,
        IElement offsetSideWitness,
        long distanceTicks,
        IElement? outwardOnlyCircleSource = null)
    {
        var participants = OrthogonalFillParticipants(offsetSideWitness, distanceTicks, outwardOnlyCircleSource);
        return GraphicAreaParticipationSurface.AreaFrame(root ?? new Carrier(), participants);
    }

    public static IElement[] OrthogonalFillParticipants(
        IElement offsetSideWitness,
        long distanceTicks,
        IElement? outwardOnlyCircleSource = null)
    {
        var participants = new List<IElement>();
        foreach (var source in OffsetWitnessSources(offsetSideWitness).DistinctBy(SourceId))
        {
            participants.AddRange(OrthogonalFillParticipantsFromSource(source, distanceTicks));
        }

        return [.. participants];
    }

    private static IEnumerable<IElement> OffsetWitnessSources(IElement source)
    {
        if (source is Relation relation &&
            GraphicOffsetSurface.TryReadBoundarySideWitness(
                relation,
                out var witnessSource,
                out _,
                out _,
                out _))
        {
            yield return witnessSource;
            yield break;
        }

        if (source is Relation group &&
            group.Right is Carrier holder &&
            group.TryReadOrderedElementsOn(holder, out var elements))
        {
            foreach (var element in elements)
            foreach (var nestedWitnessSource in OffsetWitnessSources(element))
            {
                yield return nestedWitnessSource;
            }
        }
    }

    private static IElement[] OrthogonalFillParticipantsFromSource(
        IElement source,
        long distanceTicks)
    {
        var distance = Math.Abs(distanceTicks);
        var participants = new List<IElement>();
        var settledPathSegmentIds = new HashSet<ulong>();
        foreach (var path in OpenSharedFrameSegmentPaths(source))
        {
            if (TryOpenSegmentPathBandFill(path.Template, path.Points, distance, out var pathBandFill))
            {
                participants.Add(pathBandFill);
                foreach (var segmentId in path.SegmentSourceIds)
                {
                    settledPathSegmentIds.Add(segmentId);
                }
            }
        }

        foreach (var (template, startX, startY, endX, endY) in SettledOpenSharedFrameLineSegments(source, settledPathSegmentIds))
        {
            if (TryLineBandFill(template, startX, startY, endX, endY, distance, out var bandFill))
            {
                participants.Add(bandFill);
            }
        }

        foreach (var arc in OpenSharedFrameArcs(source))
        {
            if (!TryReadPointTicks(arc.Center, out var centerX, out var centerY) ||
                !TryReadPointTicks(arc.Start, out var startX, out var startY) ||
                !TryReadPointTicks(arc.End, out var endX, out var endY))
            {
                continue;
            }

            if (TryArcBandFill(arc.Center, centerX, centerY, startX, startY, endX, endY, distance, out var arcBandFill))
            {
                participants.Add(arcBandFill);
            }
        }

        foreach (var boundary in GraphicReadGraph.From(source)
            .ReadsOf<ClosedSharedFrameBoundaryRead>()
            .DistinctBy(boundary => SourceId(boundary.Source)))
        {
            if (TryReadCircleBoundary(boundary, out var center, out var centerX, out var centerY, out var radius))
            {
                participants.AddRange(CircleOrthogonalFrameFills(
                    center,
                    centerX,
                    centerY,
                    radius,
                    distance));
                continue;
            }

            if (!TryReadAxisAlignedRectangleBoundary(boundary, out var template, out var minX, out var maxX, out var minY, out var maxY))
            {
                if (TryReadClosedSegmentLoopBoundary(boundary, out template, out var points))
                {
                    participants.AddRange(ClosedSegmentLoopOrthogonalFrameFills(template, points, distance));
                }

                continue;
            }

            participants.AddRange(RectangleOrthogonalFrameFills(template, minX, maxX, minY, maxY, distance));
        }

        return [.. participants];
    }

    private static IEnumerable<SharedFrameSegmentRead> OpenSharedFrameLineSegments(IElement source)
    {
        var graph = GraphicReadGraph.From(source);
        var arcSpanIds = graph.ReadsOf<SharedFrameArcRead>()
            .Select(arc => SourceId(arc.Span.Source))
            .ToHashSet();
        var closedBoundarySegmentIds = graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
            .SelectMany(boundary => boundary.Segments)
            .Select(segment => SourceId(segment.Source))
            .ToHashSet();
        var seen = new HashSet<ulong>();
        foreach (var pathGraph in graph.ReadsOf<PathGraphRead>())
        foreach (var edge in pathGraph.Edges)
        {
            if (edge.Segment is not SharedFrameSegmentRead segment ||
                !edge.IsSharedFrame ||
                arcSpanIds.Contains(SourceId(segment.Source)) ||
                closedBoundarySegmentIds.Contains(SourceId(segment.Source)) ||
                !seen.Add(SourceId(segment.Source)))
            {
                continue;
            }

            yield return segment;
        }
    }

    private static IEnumerable<SharedFrameArcRead> OpenSharedFrameArcs(IElement source)
    {
        var graph = GraphicReadGraph.From(source);
        var closedBoundarySegmentIds = graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
            .SelectMany(boundary => boundary.Segments)
            .Select(segment => SourceId(segment.Source))
            .ToHashSet();
        var seen = new HashSet<ulong>();
        foreach (var arc in graph.ReadsOf<SharedFrameArcRead>())
        {
            if (closedBoundarySegmentIds.Contains(SourceId(arc.Span.Source)) ||
                !seen.Add(SourceId(arc.Source)))
            {
                continue;
            }

            yield return arc;
        }
    }

    private static IEnumerable<OpenSegmentPathRead> OpenSharedFrameSegmentPaths(IElement source)
    {
        var graph = GraphicReadGraph.From(source);
        var arcSpanIds = graph.ReadsOf<SharedFrameArcRead>()
            .Select(arc => SourceId(arc.Span.Source))
            .ToHashSet();
        var closedBoundarySegmentIds = graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
            .SelectMany(boundary => boundary.Segments)
            .Select(segment => SourceId(segment.Source))
            .ToHashSet();

        foreach (var pathGraph in graph.ReadsOf<PathGraphRead>())
        {
            var eligibleEdges = pathGraph.Edges
                .Where(edge =>
                    edge.IsSharedFrame &&
                    edge.Segment is SharedFrameSegmentRead segment &&
                    !arcSpanIds.Contains(SourceId(segment.Source)) &&
                    !closedBoundarySegmentIds.Contains(SourceId(segment.Source)))
                .ToArray();
            if (eligibleEdges.Length < 2)
            {
                continue;
            }

            var edgeById = eligibleEdges
                .GroupBy(edge => SourceId(edge.Source))
                .ToDictionary(group => group.Key, group => group.First());
            var nodeEdges = new Dictionary<string, List<ulong>>();
            foreach (var edge in eligibleEdges)
            {
                var edgeId = SourceId(edge.Source);
                AddNodeEdge(PathNodeKey(edge.Start), edgeId);
                AddNodeEdge(PathNodeKey(edge.End), edgeId);
            }

            var unused = edgeById.Keys.ToHashSet();
            while (unused.Count > 0)
            {
                var seedId = unused.First();
                var component = new HashSet<ulong>();
                var queue = new Queue<ulong>();
                queue.Enqueue(seedId);
                unused.Remove(seedId);
                while (queue.Count > 0)
                {
                    var currentId = queue.Dequeue();
                    component.Add(currentId);
                    var currentEdge = edgeById[currentId];
                    foreach (var nodeKey in new[] { PathNodeKey(currentEdge.Start), PathNodeKey(currentEdge.End) })
                    foreach (var nextId in nodeEdges[nodeKey])
                    {
                        if (unused.Remove(nextId))
                        {
                            queue.Enqueue(nextId);
                        }
                    }
                }

                if (component.Count < 2)
                {
                    continue;
                }

                var componentNodeEdges = nodeEdges
                    .Where(pair => pair.Value.Any(component.Contains))
                    .ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value.Where(component.Contains).ToArray());
                if (componentNodeEdges.Any(pair => pair.Value.Length > 2))
                {
                    continue;
                }

                var terminals = componentNodeEdges
                    .Where(pair => pair.Value.Length == 1)
                    .Select(pair => pair.Key)
                    .ToArray();
                if (terminals.Length != 2)
                {
                    continue;
                }

                if (TryWalkOpenSegmentPath(
                    terminals[0],
                    component,
                    edgeById,
                    componentNodeEdges,
                    out var template,
                    out var points,
                    out var segmentSourceIds))
                {
                    yield return new(template, points, segmentSourceIds);
                }
            }

            void AddNodeEdge(string nodeKey, ulong edgeId)
            {
                if (!nodeEdges.TryGetValue(nodeKey, out var edges))
                {
                    edges = [];
                    nodeEdges[nodeKey] = edges;
                }

                edges.Add(edgeId);
            }
        }
    }

    private static bool TryWalkOpenSegmentPath(
        string startNodeKey,
        IReadOnlySet<ulong> component,
        IReadOnlyDictionary<ulong, PathGraphEdgeRead> edgeById,
        IReadOnlyDictionary<string, ulong[]> nodeEdges,
        out SharedFramePointRead template,
        out (long X, long Y)[] points,
        out ulong[] segmentSourceIds)
    {
        template = default!;
        points = [];
        segmentSourceIds = [];
        var used = new HashSet<ulong>();
        var ordered = new List<(long X, long Y)>();
        var segmentIds = new List<ulong>();
        var currentNodeKey = startNodeKey;
        while (used.Count < component.Count)
        {
            if (!nodeEdges.TryGetValue(currentNodeKey, out var candidates))
            {
                return false;
            }

            var edgeId = candidates.FirstOrDefault(id => component.Contains(id) && !used.Contains(id));
            if (edgeId == 0UL ||
                !edgeById.TryGetValue(edgeId, out var edge) ||
                edge.Segment is not SharedFrameSegmentRead segment ||
                !TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
            {
                return false;
            }

            var segmentStart = (startX, startY);
            var segmentEnd = (endX, endY);
            var startKey = PointKey(segmentStart);
            var endKey = PointKey(segmentEnd);
            if (ordered.Count == 0)
            {
                template = segment.Start;
                if (currentNodeKey == startKey)
                {
                    ordered.Add(segmentStart);
                    ordered.Add(segmentEnd);
                    currentNodeKey = endKey;
                }
                else if (currentNodeKey == endKey)
                {
                    ordered.Add(segmentEnd);
                    ordered.Add(segmentStart);
                    currentNodeKey = startKey;
                }
                else
                {
                    return false;
                }
            }
            else if (currentNodeKey == startKey)
            {
                ordered.Add(segmentEnd);
                currentNodeKey = endKey;
            }
            else if (currentNodeKey == endKey)
            {
                ordered.Add(segmentStart);
                currentNodeKey = startKey;
            }
            else
            {
                return false;
            }

            used.Add(edgeId);
            segmentIds.Add(SourceId(segment.Source));
        }

        if (ordered.Count < 3 ||
            ordered.Zip(ordered.Skip(1)).Any(pair => SamePoint(pair.First, pair.Second)))
        {
            return false;
        }

        points = [.. ordered];
        segmentSourceIds = [.. segmentIds];
        return true;
    }

    private static IEnumerable<(SharedFramePointRead Template, long StartX, long StartY, long EndX, long EndY)> SettledOpenSharedFrameLineSegments(
        IElement source,
        IReadOnlySet<ulong> excludedSegmentSourceIds)
    {
        var axisSegments = new List<(SharedFrameSegmentRead Template, bool Horizontal, long Axis, long Min, long Max)>();
        var freeSegments = new List<SharedFrameSegmentRead>();
        foreach (var segment in OpenSharedFrameLineSegments(source))
        {
            if (excludedSegmentSourceIds.Contains(SourceId(segment.Source)) ||
                !TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
            {
                continue;
            }

            if (startY == endY)
            {
                axisSegments.Add((segment, Horizontal: true, startY, Math.Min(startX, endX), Math.Max(startX, endX)));
                continue;
            }

            if (startX == endX)
            {
                axisSegments.Add((segment, Horizontal: false, startX, Math.Min(startY, endY), Math.Max(startY, endY)));
                continue;
            }

            freeSegments.Add(segment);
        }

        foreach (var group in axisSegments.GroupBy(segment => $"{segment.Horizontal}:{segment.Axis}"))
        {
            var ordered = group.OrderBy(segment => segment.Min).ToArray();
            if (ordered.Length == 0)
            {
                continue;
            }

            var template = ordered[0].Template;
            var horizontal = ordered[0].Horizontal;
            var axis = ordered[0].Axis;
            var settledMin = ordered[0].Min;
            var settledMax = ordered[0].Max;
            foreach (var segment in ordered.Skip(1))
            {
                if (segment.Min <= settledMax)
                {
                    settledMax = Math.Max(settledMax, segment.Max);
                    continue;
                }

                yield return horizontal
                    ? (template.Start, settledMin, axis, settledMax, axis)
                    : (template.Start, axis, settledMin, axis, settledMax);
                settledMin = segment.Min;
                settledMax = segment.Max;
            }

            yield return horizontal
                ? (template.Start, settledMin, axis, settledMax, axis)
                : (template.Start, axis, settledMin, axis, settledMax);
        }

        foreach (var segment in freeSegments)
        {
            if (TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
            {
                yield return (segment.Start, startX, startY, endX, endY);
            }
        }
    }

    private static bool TryOpenSegmentPathBandFill(
        SharedFramePointRead template,
        IReadOnlyList<(long X, long Y)> points,
        long distanceTicks,
        out IElement fill)
    {
        fill = Carrier.Unknown;
        if (points.Count < 2 ||
            distanceTicks <= 0)
        {
            return false;
        }

        var left = new (double X, double Y)[points.Count];
        var right = new (double X, double Y)[points.Count];
        var lines = new OffsetPathLine[points.Count - 1];
        for (var index = 0; index + 1 < points.Count; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0d)
            {
                return false;
            }

            var normalX = -dy / length * distanceTicks;
            var normalY = dx / length * distanceTicks;
            lines[index] = new(
                start.X,
                start.Y,
                dx,
                dy,
                normalX,
                normalY);
        }

        left[0] = (points[0].X + lines[0].NormalX, points[0].Y + lines[0].NormalY);
        right[0] = (points[0].X - lines[0].NormalX, points[0].Y - lines[0].NormalY);
        left[^1] = (points[^1].X + lines[^1].NormalX, points[^1].Y + lines[^1].NormalY);
        right[^1] = (points[^1].X - lines[^1].NormalX, points[^1].Y - lines[^1].NormalY);
        for (var index = 1; index + 1 < points.Count; index++)
        {
            var previous = lines[index - 1];
            var current = lines[index];
            left[index] = TryIntersect(
                    previous.LeftOffset(),
                    current.LeftOffset(),
                    out var leftIntersection)
                ? leftIntersection
                : (points[index].X + (previous.NormalX + current.NormalX) * 0.5d,
                    points[index].Y + (previous.NormalY + current.NormalY) * 0.5d);
            right[index] = TryIntersect(
                    previous.RightOffset(),
                    current.RightOffset(),
                    out var rightIntersection)
                ? rightIntersection
                : (points[index].X - (previous.NormalX + current.NormalX) * 0.5d,
                    points[index].Y - (previous.NormalY + current.NormalY) * 0.5d);
        }

        var polygon = left
            .Concat(right.Reverse())
            .Select(point => ((long)Math.Round(point.X), (long)Math.Round(point.Y)))
            .ToArray();
        if (polygon.Length < 4 ||
            Math.Abs(SignedArea(polygon)) < 1d)
        {
            return false;
        }

        fill = PolygonFill(template, polygon);
        return true;
    }

    private static bool TryLineBandFill(
        SharedFramePointRead template,
        long startX,
        long startY,
        long endX,
        long endY,
        long distanceTicks,
        out IElement fill)
    {
        var deltaX = endX - startX;
        var deltaY = endY - startY;
        var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (length <= 0d)
        {
            fill = Carrier.Unknown;
            return false;
        }

        var normalX = (long)Math.Round(-deltaY / length * distanceTicks);
        var normalY = (long)Math.Round(deltaX / length * distanceTicks);
        fill = PolygonFill(
            template,
            (startX + normalX, startY + normalY),
            (endX + normalX, endY + normalY),
            (endX - normalX, endY - normalY),
            (startX - normalX, startY - normalY));
        return true;
    }

    private static bool TryArcBandFill(
        SharedFramePointRead template,
        long centerX,
        long centerY,
        long startX,
        long startY,
        long endX,
        long endY,
        long distanceTicks,
        out IElement fill)
    {
        var startRadius = Math.Sqrt(
            Math.Pow(startX - centerX, 2) +
            Math.Pow(startY - centerY, 2));
        var endRadius = Math.Sqrt(
            Math.Pow(endX - centerX, 2) +
            Math.Pow(endY - centerY, 2));
        var radius = Math.Min(startRadius, endRadius);
        if (radius <= distanceTicks ||
            radius <= 0d)
        {
            fill = Carrier.Unknown;
            return false;
        }

        return TryArcBandFillWithRadii(
            template,
            centerX,
            centerY,
            startX,
            startY,
            endX,
            endY,
            innerRadius: radius - distanceTicks,
            outerRadius: radius + distanceTicks,
            out fill);
    }

    private static IElement[] CircleOrthogonalFrameFills(
        SharedFramePointRead template,
        long centerX,
        long centerY,
        long radius,
        long distanceTicks)
    {
        var outerRadius = radius + distanceTicks;
        if (outerRadius <= 0)
        {
            return [DiskFill(template, centerX, centerY, distanceTicks)];
        }

        var outer = DiskFill(template, centerX, centerY, outerRadius);
        var innerRadius = radius - distanceTicks;
        if (innerRadius <= 0)
        {
            return [outer];
        }

        return [Chain(outer, AntiDiskFill(template, centerX, centerY, innerRadius))];
    }

    private static IElement[] RectangleOrthogonalFrameFills(
        SharedFramePointRead template,
        long minX,
        long maxX,
        long minY,
        long maxY,
        long distanceTicks)
    {
        var outer = GraphicStructure.AreaFill(RectangleBoundary(
            template,
            minX - distanceTicks,
            maxX + distanceTicks,
            minY - distanceTicks,
            maxY + distanceTicks));
        if (maxX - minX <= distanceTicks * 2 ||
            maxY - minY <= distanceTicks * 2)
        {
            return [outer];
        }

        var inner = GraphicStructure.AntiAreaFill(RectangleBoundary(
            template,
            minX + distanceTicks,
            maxX - distanceTicks,
            minY + distanceTicks,
            maxY - distanceTicks));
        return [Chain(outer, inner)];
    }

    private static IElement[] ClosedSegmentLoopOrthogonalFrameFills(
        SharedFramePointRead template,
        IReadOnlyList<(long X, long Y)> points,
        long distanceTicks)
    {
        if (points.Count < 3 ||
            !TryOffsetPolygon(points, distanceTicks, outward: true, out var outer))
        {
            return [];
        }

        var outerFill = GraphicStructure.AreaFill(PolygonBoundary(template, outer));
        return TryOffsetPolygon(points, distanceTicks, outward: false, out var inner)
            ? [Chain(outerFill, GraphicStructure.AntiAreaFill(PolygonBoundary(template, inner)))]
            : [outerFill];
    }

    private static bool TryArcBandFillWithRadii(
        SharedFramePointRead template,
        long centerX,
        long centerY,
        long startX,
        long startY,
        long endX,
        long endY,
        double innerRadius,
        double outerRadius,
        out IElement fill)
    {
        if (innerRadius < 0d ||
            outerRadius <= innerRadius)
        {
            fill = Carrier.Unknown;
            return false;
        }

        (long X, long Y) OffsetArcPoint(long pointX, long pointY, double offsetRadius)
        {
            var dx = pointX - centerX;
            var dy = pointY - centerY;
            var length = Math.Sqrt(dx * dx + dy * dy);
            return length <= 0d
                ? (pointX, pointY)
                : (
                    centerX + (long)Math.Round(dx / length * offsetRadius),
                    centerY + (long)Math.Round(dy / length * offsetRadius));
        }

        var outerStart = OffsetArcPoint(startX, startY, outerRadius);
        var outerEnd = OffsetArcPoint(endX, endY, outerRadius);
        var innerStart = OffsetArcPoint(startX, startY, innerRadius);
        var innerEnd = OffsetArcPoint(endX, endY, innerRadius);
        fill = GraphicStructure.AreaFill(
            Arc(template, centerX, centerY, outerStart.X, outerStart.Y, outerEnd.X, outerEnd.Y),
            Segment(template, outerEnd.X, outerEnd.Y, innerEnd.X, innerEnd.Y),
            Arc(template, centerX, centerY, innerEnd.X, innerEnd.Y, innerStart.X, innerStart.Y),
            Segment(template, innerStart.X, innerStart.Y, outerStart.X, outerStart.Y));
        return true;
    }

    private static Relation PolygonFill(SharedFramePointRead template, params (long X, long Y)[] points)
    {
        if (points.Length < 3)
        {
            return Relation.OrderedGroup(new Carrier(), []);
        }

        var edges = new IElement[points.Length];
        for (var index = 0; index < points.Length; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Length];
            edges[index] = Segment(template, start.X, start.Y, end.X, end.Y);
        }

        return GraphicStructure.AreaFill(edges);
    }

    private static Relation DiskFill(SharedFramePointRead template, long centerX, long centerY, long radius) =>
        GraphicStructure.AreaFill(Circle(template, centerX, centerY, radius));

    private static Relation AntiDiskFill(SharedFramePointRead template, long centerX, long centerY, long radius) =>
        GraphicStructure.AntiAreaFill(Circle(template, centerX, centerY, radius));

    private static Relation Circle(SharedFramePointRead template, long centerX, long centerY, long radius) =>
        Chain(
            Arc(template, centerX, centerY, centerX + radius, centerY, centerX, centerY + radius),
            Arc(template, centerX, centerY, centerX, centerY + radius, centerX - radius, centerY),
            Arc(template, centerX, centerY, centerX - radius, centerY, centerX, centerY - radius),
            Arc(template, centerX, centerY, centerX, centerY - radius, centerX + radius, centerY));

    private static Relation RectangleBoundary(
        SharedFramePointRead template,
        long minX,
        long maxX,
        long minY,
        long maxY) =>
        Chain(
            Segment(template, minX, minY, maxX, minY),
            Segment(template, maxX, minY, maxX, maxY),
            Segment(template, maxX, maxY, minX, maxY),
            Segment(template, minX, maxY, minX, minY));

    private static Relation PolygonBoundary(SharedFramePointRead template, IReadOnlyList<(long X, long Y)> points)
    {
        if (points.Count < 3)
        {
            return Chain();
        }

        var edges = new IElement[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Count];
            edges[index] = Segment(template, start.X, start.Y, end.X, end.Y);
        }

        return Chain(edges);
    }

    private static Relation Arc(
        SharedFramePointRead template,
        long centerX,
        long centerY,
        long startX,
        long startY,
        long endX,
        long endY) =>
        GraphicStructure.FrameArc(
            Point(template, centerX, centerY),
            Point(template, startX, startY),
            Point(template, endX, endY));

    private static Relation Segment(
        SharedFramePointRead template,
        long startX,
        long startY,
        long endX,
        long endY) =>
        GraphicStructure.FrameSegment(
            Point(template, startX, startY),
            Point(template, endX, endY));

    private static Relation Point(SharedFramePointRead template, long xTicks, long yTicks)
    {
        if (template.Coordinates.Count < 2)
        {
            return Relation.OrderedGroup(new Carrier(), []);
        }

        return GraphicStructure.FramePoint(
            template.Holder,
            new CarrierLocation(template.Coordinates[0].Carrier, xTicks),
            new CarrierLocation(template.Coordinates[1].Carrier, yTicks));
    }

    private static Relation Chain(params IElement[] elements) =>
        Relation.OrderedGroup(new Carrier(), elements ?? []);

    private static IEnumerable<(long CenterX, long CenterY, long Radius)> CircleReads(IElement source)
    {
        foreach (var boundary in GraphicReadGraph.From(source).ReadsOf<ClosedSharedFrameBoundaryRead>())
        {
            if (TryReadCircleBoundary(boundary, out _, out var centerX, out var centerY, out var radius))
            {
                yield return (centerX, centerY, radius);
            }
        }
    }

    private static bool TryReadCircleBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        out SharedFramePointRead center,
        out long centerX,
        out long centerY,
        out long radius)
    {
        var arcs = boundary.Arcs ?? [];
        if (arcs.Count < 4 ||
            !TryReadPointTicks(arcs[0].Center, out centerX, out centerY) ||
            !TryReadPointTicks(arcs[0].Start, out var startX, out var startY))
        {
            center = default!;
            centerX = 0;
            centerY = 0;
            radius = 0;
            return false;
        }

        center = arcs[0].Center;
        foreach (var arc in arcs)
        {
            if (!TryReadPointTicks(arc.Center, out var arcCenterX, out var arcCenterY) ||
                arcCenterX != centerX ||
                arcCenterY != centerY)
            {
                radius = 0;
                return false;
            }
        }

        radius = (long)Math.Round(Math.Sqrt(
            Math.Pow(startX - centerX, 2) +
            Math.Pow(startY - centerY, 2)));
        return radius > 0;
    }

    private static bool TryReadAxisAlignedRectangleBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        out SharedFramePointRead template,
        out long minX,
        out long maxX,
        out long minY,
        out long maxY)
    {
        template = default!;
        minX = 0;
        maxX = 0;
        minY = 0;
        maxY = 0;
        if (boundary.Arcs is { Count: > 0 } ||
            boundary.Segments.Count != 4 ||
            !TryReadBoundaryBounds(boundary, out minX, out maxX, out minY, out maxY) ||
            minX == maxX ||
            minY == maxY)
        {
            return false;
        }

        var corners = new HashSet<string>();
        foreach (var segment in boundary.Segments)
        {
            if (!TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY) ||
                (startX != endX && startY != endY))
            {
                return false;
            }

            corners.Add($"{startX}:{startY}");
            corners.Add($"{endX}:{endY}");
        }

        if (corners.Count != 4)
        {
            return false;
        }

        template = boundary.Segments[0].Start;
        return true;
    }

    private static bool TryReadClosedSegmentLoopBoundary(
        ClosedSharedFrameBoundaryRead boundary,
        out SharedFramePointRead template,
        out (long X, long Y)[] points)
    {
        template = default!;
        points = [];
        if (boundary.Arcs is { Count: > 0 } ||
            boundary.Segments.Count < 3)
        {
            return false;
        }

        var segments = new List<SegmentTicks>();
        foreach (var segment in boundary.Segments)
        {
            if (!TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
            {
                return false;
            }

            segments.Add(new(segment.Start, startX, startY, endX, endY));
        }

        var used = new bool[segments.Count];
        var ordered = new List<(long X, long Y)> { (segments[0].StartX, segments[0].StartY) };
        var current = (segments[0].EndX, segments[0].EndY);
        used[0] = true;
        for (var count = 1; count < segments.Count; count++)
        {
            ordered.Add(current);
            var found = false;
            for (var index = 1; index < segments.Count; index++)
            {
                if (used[index])
                {
                    continue;
                }

                var candidate = segments[index];
                if (SamePoint(current, (candidate.StartX, candidate.StartY)))
                {
                    current = (candidate.EndX, candidate.EndY);
                    used[index] = true;
                    found = true;
                    break;
                }

                if (SamePoint(current, (candidate.EndX, candidate.EndY)))
                {
                    current = (candidate.StartX, candidate.StartY);
                    used[index] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        if (!SamePoint(current, ordered[0]) ||
            Math.Abs(SignedArea(ordered)) < 1d)
        {
            return false;
        }

        template = segments[0].Template;
        points = [.. ordered];
        return true;
    }

    private static bool TryOffsetPolygon(
        IReadOnlyList<(long X, long Y)> points,
        long distanceTicks,
        bool outward,
        out (long X, long Y)[] offsetPoints)
    {
        offsetPoints = [];
        if (points.Count < 3 ||
            distanceTicks <= 0)
        {
            return false;
        }

        var centroid = (
            X: points.Average(point => point.X),
            Y: points.Average(point => point.Y));
        var lines = new OffsetLine[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Count];
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length <= 0d)
            {
                return false;
            }

            var normalX = -dy / length;
            var normalY = dx / length;
            var midX = (start.X + end.X) * 0.5d;
            var midY = (start.Y + end.Y) * 0.5d;
            if (normalX * (centroid.X - midX) + normalY * (centroid.Y - midY) > 0d)
            {
                normalX = -normalX;
                normalY = -normalY;
            }

            var side = outward ? 1d : -1d;
            lines[index] = new(
                X: start.X + normalX * distanceTicks * side,
                Y: start.Y + normalY * distanceTicks * side,
                Dx: dx,
                Dy: dy);
        }

        var output = new List<(long X, long Y)>();
        for (var index = 0; index < lines.Length; index++)
        {
            var previous = lines[(index + lines.Length - 1) % lines.Length];
            var current = lines[index];
            if (!TryIntersect(previous, current, out var intersection))
            {
                return false;
            }

            output.Add((
                (long)Math.Round(intersection.X),
                (long)Math.Round(intersection.Y)));
        }

        if (Math.Abs(SignedArea(output)) < 1d)
        {
            return false;
        }

        offsetPoints = [.. output];
        return true;
    }

    private static bool TryReadBoundaryBounds(
        ClosedSharedFrameBoundaryRead boundary,
        out long minX,
        out long maxX,
        out long minY,
        out long maxY)
    {
        var points = boundary.Segments
            .SelectMany(segment => new[] { segment.Start, segment.End })
            .ToArray();
        if (points.Length == 0)
        {
            minX = 0;
            maxX = 0;
            minY = 0;
            maxY = 0;
            return false;
        }

        var read = new List<(long X, long Y)>();
        foreach (var point in points)
        {
            if (TryReadPointTicks(point, out var x, out var y))
            {
                read.Add((x, y));
            }
        }

        if (read.Count == 0)
        {
            minX = 0;
            maxX = 0;
            minY = 0;
            maxY = 0;
            return false;
        }

        minX = read.Min(point => point.X);
        maxX = read.Max(point => point.X);
        minY = read.Min(point => point.Y);
        maxY = read.Max(point => point.Y);
        return true;
    }

    private static bool TryReadPointTicks(SharedFramePointRead point, out long pointX, out long pointY)
    {
        if (point.Coordinates.Count >= 2)
        {
            pointX = point.Coordinates[0].Ticks;
            pointY = point.Coordinates[1].Ticks;
            return true;
        }

        pointX = 0;
        pointY = 0;
        return false;
    }

    private static bool TryReadSegmentTicks(
        SharedFrameSegmentRead segment,
        out long startX,
        out long startY,
        out long endX,
        out long endY)
    {
        if (TryReadPointTicks(segment.Start, out startX, out startY) &&
            TryReadPointTicks(segment.End, out endX, out endY))
        {
            return true;
        }

        startX = 0;
        startY = 0;
        endX = 0;
        endY = 0;
        return false;
    }

    private static ulong SourceId(IElement source) =>
        source is Relation relation ? relation.Id : 0UL;

    private static bool SamePoint((long X, long Y) left, (long X, long Y) right) =>
        left.X == right.X && left.Y == right.Y;

    private static string PointKey((long X, long Y) point) =>
        $"{point.X}:{point.Y}";

    private static string PathNodeKey(PathGraphNodeRead node)
    {
        foreach (var endpoint in node.Endpoints)
        {
            if (endpoint.SharedFramePoint is { } shared &&
                TryReadPointTicks(shared, out var x, out var y))
            {
                return $"{x}:{y}";
            }
        }

        return SourceId(node.Source).ToString();
    }

    private static double SignedArea(IReadOnlyList<(long X, long Y)> points)
    {
        var area = 0d;
        for (var index = 0; index < points.Count; index++)
        {
            var current = points[index];
            var next = points[(index + 1) % points.Count];
            area += current.X * next.Y - next.X * current.Y;
        }

        return area * 0.5d;
    }

    private static bool TryIntersect(OffsetLine left, OffsetLine right, out (double X, double Y) intersection)
    {
        var cross = left.Dx * right.Dy - left.Dy * right.Dx;
        if (Math.Abs(cross) < 0.0001d)
        {
            intersection = default;
            return false;
        }

        var deltaX = right.X - left.X;
        var deltaY = right.Y - left.Y;
        var t = (deltaX * right.Dy - deltaY * right.Dx) / cross;
        intersection = (left.X + left.Dx * t, left.Y + left.Dy * t);
        return true;
    }

    private readonly record struct SegmentTicks(
        SharedFramePointRead Template,
        long StartX,
        long StartY,
        long EndX,
        long EndY);

    private readonly record struct OffsetLine(double X, double Y, double Dx, double Dy);

    private sealed record OpenSegmentPathRead(
        SharedFramePointRead Template,
        IReadOnlyList<(long X, long Y)> Points,
        IReadOnlyList<ulong> SegmentSourceIds);

    private readonly record struct OffsetPathLine(
        double X,
        double Y,
        double Dx,
        double Dy,
        double NormalX,
        double NormalY)
    {
        public OffsetLine LeftOffset() => new(X + NormalX, Y + NormalY, Dx, Dy);

        public OffsetLine RightOffset() => new(X - NormalX, Y - NormalY, Dx, Dy);
    }
}
