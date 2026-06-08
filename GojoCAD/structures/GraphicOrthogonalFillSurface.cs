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
        return GraphicBooleanSurface.AreaFrame(root ?? new Carrier(), participants);
    }

    public static IElement[] OrthogonalFillParticipants(
        IElement offsetSideWitness,
        long distanceTicks,
        IElement? outwardOnlyCircleSource = null)
    {
        var outwardOnlyCircleKeys = (outwardOnlyCircleSource is null
                ? []
                : CircleReads(outwardOnlyCircleSource)
                    .Select(circle => $"{circle.CenterX}:{circle.CenterY}:{circle.Radius}"))
            .ToHashSet();
        var participants = new List<IElement>();
        foreach (var source in OffsetWitnessSources(offsetSideWitness).DistinctBy(SourceId))
        {
            participants.AddRange(OrthogonalFillParticipantsFromSource(source, distanceTicks, outwardOnlyCircleKeys));
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
        long distanceTicks,
        IReadOnlySet<string> outwardOnlyCircleKeys)
    {
        var distance = Math.Abs(distanceTicks);
        var participants = new List<IElement>();
        foreach (var (template, startX, startY, endX, endY) in SettledOpenSharedFrameLineSegments(source))
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
                var outwardOnly = outwardOnlyCircleKeys.Contains($"{centerX}:{centerY}:{radius}");
                participants.AddRange(CircleOrthogonalBandFills(
                    center,
                    centerX,
                    centerY,
                    radius,
                    distance,
                    includeInner: !outwardOnly));
                continue;
            }

            if (!TryReadAxisAlignedRectangleBoundary(boundary, out var template, out var minX, out var maxX, out var minY, out var maxY))
            {
                continue;
            }

            participants.AddRange(RectangleOrthogonalBandFills(template, minX, maxX, minY, maxY, distance));
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

    private static IEnumerable<(SharedFramePointRead Template, long StartX, long StartY, long EndX, long EndY)> SettledOpenSharedFrameLineSegments(IElement source)
    {
        var axisSegments = new List<(SharedFrameSegmentRead Template, bool Horizontal, long Axis, long Min, long Max)>();
        var freeSegments = new List<SharedFrameSegmentRead>();
        foreach (var segment in OpenSharedFrameLineSegments(source))
        {
            if (!TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
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

    private static IElement[] CircleOrthogonalBandFills(
        SharedFramePointRead template,
        long centerX,
        long centerY,
        long radius,
        long distanceTicks,
        bool includeInner)
    {
        if (radius <= 0)
        {
            return [DiskFill(template, centerX, centerY, distanceTicks)];
        }

        var points = new (long X, long Y)[]
        {
            (centerX + radius, centerY),
            (centerX, centerY + radius),
            (centerX - radius, centerY),
            (centerX, centerY - radius)
        };
        var fills = new List<IElement>();
        for (var index = 0; index < points.Length; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Length];
            if (TryArcBandFillWithRadii(
                    template,
                    centerX,
                    centerY,
                    start.X,
                    start.Y,
                    end.X,
                    end.Y,
                    innerRadius: radius,
                    outerRadius: radius + distanceTicks,
                    out var outerFill))
            {
                fills.Add(outerFill);
            }

            if (includeInner &&
                radius > distanceTicks &&
                TryArcBandFillWithRadii(
                    template,
                    centerX,
                    centerY,
                    start.X,
                    start.Y,
                    end.X,
                    end.Y,
                    innerRadius: radius - distanceTicks,
                    outerRadius: radius,
                    out var innerFill))
            {
                fills.Add(innerFill);
            }
        }

        return [.. fills];
    }

    private static IElement[] RectangleOrthogonalBandFills(
        SharedFramePointRead template,
        long minX,
        long maxX,
        long minY,
        long maxY,
        long distanceTicks)
    {
        var fills = new List<IElement>
        {
            PolygonFill(
                template,
                (minX - distanceTicks, minY - distanceTicks),
                (maxX + distanceTicks, minY - distanceTicks),
                (maxX, minY),
                (minX, minY)),
            PolygonFill(
                template,
                (maxX + distanceTicks, minY - distanceTicks),
                (maxX + distanceTicks, maxY + distanceTicks),
                (maxX, maxY),
                (maxX, minY)),
            PolygonFill(
                template,
                (maxX + distanceTicks, maxY + distanceTicks),
                (minX - distanceTicks, maxY + distanceTicks),
                (minX, maxY),
                (maxX, maxY)),
            PolygonFill(
                template,
                (minX - distanceTicks, maxY + distanceTicks),
                (minX - distanceTicks, minY - distanceTicks),
                (minX, minY),
                (minX, maxY))
        };

        if (maxX - minX > distanceTicks * 2 &&
            maxY - minY > distanceTicks * 2)
        {
            fills.AddRange([
                PolygonFill(
                    template,
                    (minX, minY),
                    (maxX, minY),
                    (maxX - distanceTicks, minY + distanceTicks),
                    (minX + distanceTicks, minY + distanceTicks)),
                PolygonFill(
                    template,
                    (maxX, minY),
                    (maxX, maxY),
                    (maxX - distanceTicks, maxY - distanceTicks),
                    (maxX - distanceTicks, minY + distanceTicks)),
                PolygonFill(
                    template,
                    (maxX, maxY),
                    (minX, maxY),
                    (minX + distanceTicks, maxY - distanceTicks),
                    (maxX - distanceTicks, maxY - distanceTicks)),
                PolygonFill(
                    template,
                    (minX, maxY),
                    (minX, minY),
                    (minX + distanceTicks, minY + distanceTicks),
                    (minX + distanceTicks, maxY - distanceTicks))
            ]);
        }

        return [.. fills];
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

    private static Relation Circle(SharedFramePointRead template, long centerX, long centerY, long radius) =>
        Chain(
            Arc(template, centerX, centerY, centerX + radius, centerY, centerX, centerY + radius),
            Arc(template, centerX, centerY, centerX, centerY + radius, centerX - radius, centerY),
            Arc(template, centerX, centerY, centerX - radius, centerY, centerX, centerY - radius),
            Arc(template, centerX, centerY, centerX, centerY - radius, centerX + radius, centerY));

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
}
