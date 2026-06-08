using System.Linq;
using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static partial class SketchSamples
{
    private static SketchScene CreateRadialShapeLadder(int seed, int comparisonVariant)
    {
        const long armReach = 122;
        const long offset = 10;
        const long centerRadius = 71;
        const long endpointRadius = 18;
        const long endpointHoleRadius = endpointRadius - offset;
        const long roundedCornerRadius = 12;

        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation PAt(long originX, long originY, long xTicks, long yTicks) =>
            Point(frame, x, y, originX + xTicks, originY + yTicks);

        Relation Distance(long ticks) =>
            Relation.Segment(new Carrier(), 0, Math.Abs(ticks));

        Relation Group(params IElement[] elements) =>
            Relation.OrderedGroup(new Carrier(), elements ?? []);

        Relation SegmentFrom(Relation start, Relation end) =>
            GraphicStructure.FrameSegment(start, end);

        Relation SegmentAt(long originX, long originY, long startX, long startY, long endX, long endY) =>
            SegmentFrom(PAt(originX, originY, startX, startY), PAt(originX, originY, endX, endY));

        Relation Segment(long startX, long startY, long endX, long endY) =>
            SegmentAt(0, 0, startX, startY, endX, endY);

        Relation ArcAt(long originX, long originY, long centerX, long centerY, long startX, long startY, long endX, long endY) =>
            GraphicStructure.FrameArc(
                PAt(originX, originY, centerX, centerY),
                PAt(originX, originY, startX, startY),
                PAt(originX, originY, endX, endY));

        Relation Arc(long centerX, long centerY, long startX, long startY, long endX, long endY) =>
            ArcAt(0, 0, centerX, centerY, startX, startY, endX, endY);

        Relation CircleBoundaryAt(long originX, long originY, long centerX, long centerY, long radius) =>
            Chain(
                ArcAt(originX, originY, centerX, centerY, centerX + radius, centerY, centerX, centerY + radius),
                ArcAt(originX, originY, centerX, centerY, centerX, centerY + radius, centerX - radius, centerY),
                ArcAt(originX, originY, centerX, centerY, centerX - radius, centerY, centerX, centerY - radius),
                ArcAt(originX, originY, centerX, centerY, centerX, centerY - radius, centerX + radius, centerY));

        Relation Circle(long centerX, long centerY, long radius) =>
            CircleBoundaryAt(0, 0, centerX, centerY, radius);

        Relation EndpointHoleOutlines(IElement circleSource) =>
            Group([.. CircleReads(circleSource)
                .Select(circle => Circle(circle.CenterX, circle.CenterY, endpointHoleRadius))]);

        Relation ArcBandAt(long originX, long originY, long centerX, long centerY, long startX, long startY, long endX, long endY, long distanceTicks)
        {
            var distance = Math.Abs(distanceTicks);
            var startRadius = Math.Sqrt(
                Math.Pow(startX - centerX, 2) +
                Math.Pow(startY - centerY, 2));
            var endRadius = Math.Sqrt(
                Math.Pow(endX - centerX, 2) +
                Math.Pow(endY - centerY, 2));
            var radius = Math.Min(startRadius, endRadius);
            if (radius <= distance)
            {
                return Chain(ArcAt(originX, originY, centerX, centerY, startX, startY, endX, endY));
            }

            (long X, long Y) OffsetPoint(long pointX, long pointY, double offsetRadius)
            {
                var dx = pointX - centerX;
                var dy = pointY - centerY;
                var length = Math.Sqrt(dx * dx + dy * dy);
                return length <= 0
                    ? (pointX, pointY)
                    : (
                        centerX + (long)Math.Round(dx / length * offsetRadius),
                        centerY + (long)Math.Round(dy / length * offsetRadius));
            }

            var outerStart = OffsetPoint(startX, startY, radius + distance);
            var outerEnd = OffsetPoint(endX, endY, radius + distance);
            var innerStart = OffsetPoint(startX, startY, radius - distance);
            var innerEnd = OffsetPoint(endX, endY, radius - distance);
            return Chain(
                ArcAt(originX, originY, centerX, centerY, outerStart.X, outerStart.Y, outerEnd.X, outerEnd.Y),
                SegmentAt(originX, originY, outerEnd.X, outerEnd.Y, innerEnd.X, innerEnd.Y),
                ArcAt(originX, originY, centerX, centerY, innerEnd.X, innerEnd.Y, innerStart.X, innerStart.Y),
                SegmentAt(originX, originY, innerStart.X, innerStart.Y, outerStart.X, outerStart.Y));
        }

        Relation RectangleOutline(long minX, long maxX, long minY, long maxY) =>
            Chain(
                Segment(minX, minY, maxX, minY),
                Segment(maxX, minY, maxX, maxY),
                Segment(maxX, maxY, minX, maxY),
                Segment(minX, maxY, minX, minY));

        Relation PolygonBoundary(params (long X, long Y)[] points)
        {
            if (points.Length < 3)
            {
                return Chain();
            }

            var edges = new IElement[points.Length];
            for (var index = 0; index < points.Length; index++)
            {
                var start = points[index];
                var end = points[(index + 1) % points.Length];
                edges[index] = Segment(start.X, start.Y, end.X, end.Y);
            }

            return Chain(edges);
        }

        Relation DiamondOutlineAt(long originX, long originY, long halfWidth, long halfHeight) =>
            Chain(
                SegmentAt(originX, originY, 0, halfHeight, halfWidth, 0),
                SegmentAt(originX, originY, halfWidth, 0, 0, -halfHeight),
                SegmentAt(originX, originY, 0, -halfHeight, -halfWidth, 0),
                SegmentAt(originX, originY, -halfWidth, 0, 0, halfHeight));

        ulong SourceId(IElement source) =>
            source is Relation relation ? relation.Id : 0UL;

        bool TryReadPointTicks(SharedFramePointRead point, out long pointX, out long pointY)
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

        bool TryReadSegmentTicks(
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

        bool TryReadCircleBoundary(
            ClosedSharedFrameBoundaryRead boundary,
            out long centerX,
            out long centerY,
            out long radius)
        {
            var arcs = boundary.Arcs ?? [];
            if (arcs.Count < 4 ||
                !TryReadPointTicks(arcs[0].Center, out centerX, out centerY) ||
                !TryReadPointTicks(arcs[0].Start, out var startX, out var startY))
            {
                centerX = 0;
                centerY = 0;
                radius = 0;
                return false;
            }

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

        bool TryReadAxisAlignedRectangleBoundary(
            ClosedSharedFrameBoundaryRead boundary,
            out long minX,
            out long maxX,
            out long minY,
            out long maxY)
        {
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

            return corners.SetEquals([
                $"{minX}:{minY}",
                $"{maxX}:{minY}",
                $"{maxX}:{maxY}",
                $"{minX}:{maxY}"
            ]);
        }

        bool TryReadClosedSegmentLoopBoundary(
            ClosedSharedFrameBoundaryRead boundary,
            out (long X, long Y)[] points)
        {
            points = [];
            if (boundary.Arcs is { Count: > 0 } ||
                boundary.Segments.Count < 3)
            {
                return false;
            }

            var segments = new List<(long StartX, long StartY, long EndX, long EndY)>();
            foreach (var segment in boundary.Segments)
            {
                if (!TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
                {
                    return false;
                }

                segments.Add((startX, startY, endX, endY));
            }

            var used = new bool[segments.Count];
            var ordered = new List<(long X, long Y)> { (segments[0].StartX, segments[0].StartY) };
            var current = (X: segments[0].EndX, Y: segments[0].EndY);
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

            points = [.. ordered];
            return true;
        }

        IEnumerable<SharedFrameSegmentRead> OpenSharedFrameLineSegments(IElement source)
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

        IEnumerable<SharedFrameArcRead> OpenSharedFrameArcs(IElement source)
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

        IEnumerable<(long StartX, long StartY, long EndX, long EndY)> SettledOpenSharedFrameLineSegmentTicks(IElement source)
        {
            var axisSegments = new List<(bool Horizontal, long Axis, long Min, long Max)>();
            var freeSegments = new List<(long StartX, long StartY, long EndX, long EndY)>();
            foreach (var segment in OpenSharedFrameLineSegments(source))
            {
                if (!TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
                {
                    continue;
                }

                if (startY == endY)
                {
                    axisSegments.Add((Horizontal: true, startY, Math.Min(startX, endX), Math.Max(startX, endX)));
                    continue;
                }

                if (startX == endX)
                {
                    axisSegments.Add((Horizontal: false, startX, Math.Min(startY, endY), Math.Max(startY, endY)));
                    continue;
                }

                freeSegments.Add((startX, startY, endX, endY));
            }

            foreach (var group in axisSegments.GroupBy(segment => $"{segment.Horizontal}:{segment.Axis}"))
            {
                var ordered = group.OrderBy(segment => segment.Min).ToArray();
                if (ordered.Length == 0)
                {
                    continue;
                }

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
                        ? (settledMin, axis, settledMax, axis)
                        : (axis, settledMin, axis, settledMax);
                    settledMin = segment.Min;
                    settledMax = segment.Max;
                }

                yield return horizontal
                    ? (settledMin, axis, settledMax, axis)
                    : (axis, settledMin, axis, settledMax);
            }

            foreach (var segment in freeSegments)
            {
                yield return segment;
            }
        }

        (long CenterX, long CenterY, long Radius)[] CircleReads(IElement source) =>
            [.. GraphicReadGraph.From(source)
                .ReadsOf<ClosedSharedFrameBoundaryRead>()
                .Select(boundary =>
                {
                    var read = TryReadCircleBoundary(boundary, out var centerX, out var centerY, out var radius);
                    return (read, centerX, centerY, radius);
                })
                .Where(circle => circle.read)
                .Select(circle => (circle.centerX, circle.centerY, circle.radius))
                .Distinct()];

        IElement TrimOpenTipsByEndpointCircles(IElement source, IElement endpointCircles)
        {
            var caps = CircleReads(endpointCircles);
            if (caps.Length == 0)
            {
                return source;
            }

            (long CenterX, long CenterY, long Radius)? CapAt(long pointX, long pointY)
            {
                foreach (var cap in caps)
                {
                    if (Math.Abs(cap.CenterX - pointX) <= 1 &&
                        Math.Abs(cap.CenterY - pointY) <= 1)
                    {
                        return cap;
                    }
                }

                return null;
            }

            var elements = new List<IElement>();
            foreach (var segment in OpenSharedFrameLineSegments(source))
            {
                if (!TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
                {
                    continue;
                }

                var deltaX = endX - startX;
                var deltaY = endY - startY;
                var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (length <= 0d)
                {
                    continue;
                }

                var startCap = CapAt(startX, startY);
                var endCap = CapAt(endX, endY);
                var startTrim = startCap?.Radius ?? 0L;
                var endTrim = endCap?.Radius ?? 0L;
                if (startTrim + endTrim >= length - 1d)
                {
                    continue;
                }

                var unitX = deltaX / length;
                var unitY = deltaY / length;
                var trimmedStartX = startX + (long)Math.Round(unitX * startTrim);
                var trimmedStartY = startY + (long)Math.Round(unitY * startTrim);
                var trimmedEndX = endX - (long)Math.Round(unitX * endTrim);
                var trimmedEndY = endY - (long)Math.Round(unitY * endTrim);
                elements.Add(Segment(trimmedStartX, trimmedStartY, trimmedEndX, trimmedEndY));
            }

            foreach (var arc in OpenSharedFrameArcs(source))
            {
                if (!TryReadPointTicks(arc.Center, out var centerX, out var centerY) ||
                    !TryReadPointTicks(arc.Start, out var startX, out var startY) ||
                    !TryReadPointTicks(arc.End, out var endX, out var endY))
                {
                    continue;
                }

                var startRadius = Math.Sqrt(
                    Math.Pow(startX - centerX, 2) +
                    Math.Pow(startY - centerY, 2));
                var endRadius = Math.Sqrt(
                    Math.Pow(endX - centerX, 2) +
                    Math.Pow(endY - centerY, 2));
                var radius = Math.Min(startRadius, endRadius);
                if (radius <= 0d)
                {
                    continue;
                }

                var startCap = CapAt(startX, startY);
                var endCap = CapAt(endX, endY);
                var startTrim = (startCap?.Radius ?? 0L) / radius;
                var endTrim = (endCap?.Radius ?? 0L) / radius;
                var startAngle = Math.Atan2(startY - centerY, startX - centerX);
                var sweep = ShortestSweep(startAngle, Math.Atan2(endY - centerY, endX - centerX));
                if (Math.Abs(sweep) <= startTrim + endTrim + 0.001d)
                {
                    continue;
                }

                var direction = Math.Sign(sweep == 0d ? 1d : sweep);
                var trimmedStartAngle = startAngle + direction * startTrim;
                var trimmedEndAngle = startAngle + sweep - direction * endTrim;
                (long X, long Y) PointAt(double angle) => (
                    centerX + (long)Math.Round(Math.Cos(angle) * radius),
                    centerY + (long)Math.Round(Math.Sin(angle) * radius));
                var trimmedStart = PointAt(trimmedStartAngle);
                var trimmedEnd = PointAt(trimmedEndAngle);
                elements.Add(Arc(centerX, centerY, trimmedStart.X, trimmedStart.Y, trimmedEnd.X, trimmedEnd.Y));
            }

            return Group([.. elements]);
        }

        IElement[] DerivedOffsetBoundaryElements(IElement source, long distanceTicks, IElement? outwardOnlyCircleSource = null)
        {
            var elements = new List<IElement>();
            foreach (var (startX, startY, endX, endY) in SettledOpenSharedFrameLineSegmentTicks(source))
            {
                var deltaX = endX - startX;
                var deltaY = endY - startY;
                var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (length <= 0)
                {
                    continue;
                }

                var segmentDistanceTicks = Math.Abs(distanceTicks);
                var normalX = (long)Math.Round(-deltaY / length * segmentDistanceTicks);
                var normalY = (long)Math.Round(deltaX / length * segmentDistanceTicks);
                elements.Add(Segment(startX + normalX, startY + normalY, endX + normalX, endY + normalY));
                elements.Add(Segment(startX - normalX, startY - normalY, endX - normalX, endY - normalY));
            }

            foreach (var arc in OpenSharedFrameArcs(source))
            {
                if (!TryReadPointTicks(arc.Center, out var centerX, out var centerY) ||
                    !TryReadPointTicks(arc.Start, out var startX, out var startY) ||
                    !TryReadPointTicks(arc.End, out var endX, out var endY))
                {
                    continue;
                }

                var startRadius = Math.Sqrt(
                    Math.Pow(startX - centerX, 2) +
                    Math.Pow(startY - centerY, 2));
                var endRadius = Math.Sqrt(
                    Math.Pow(endX - centerX, 2) +
                    Math.Pow(endY - centerY, 2));
                var radius = Math.Min(startRadius, endRadius);
                if (radius <= 0)
                {
                    continue;
                }

                (long X, long Y) OffsetArcPoint(long pointX, long pointY, double offsetRadius)
                {
                    var dx = pointX - centerX;
                    var dy = pointY - centerY;
                    var length = Math.Sqrt(dx * dx + dy * dy);
                    return length <= 0
                        ? (pointX, pointY)
                        : (
                            centerX + (long)Math.Round(dx / length * offsetRadius),
                            centerY + (long)Math.Round(dy / length * offsetRadius));
                }

                var outerStart = OffsetArcPoint(startX, startY, radius + distanceTicks);
                var outerEnd = OffsetArcPoint(endX, endY, radius + distanceTicks);
                elements.Add(Arc(centerX, centerY, outerStart.X, outerStart.Y, outerEnd.X, outerEnd.Y));
                if (radius > distanceTicks)
                {
                    var innerStart = OffsetArcPoint(startX, startY, radius - distanceTicks);
                    var innerEnd = OffsetArcPoint(endX, endY, radius - distanceTicks);
                    elements.Add(Arc(centerX, centerY, innerStart.X, innerStart.Y, innerEnd.X, innerEnd.Y));
                }
            }

            var graph = GraphicReadGraph.From(source);
            var seenCircles = new HashSet<string>();
            foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>())
            {
                if (!TryReadCircleBoundary(boundary, out var centerX, out var centerY, out var radius) ||
                    !seenCircles.Add($"{centerX}:{centerY}:{radius}"))
                {
                    continue;
                }

                elements.Add(Circle(centerX, centerY, radius + distanceTicks));
                if (radius > distanceTicks)
                {
                    elements.Add(Circle(centerX, centerY, radius - distanceTicks));
                }
            }

            var seenRectangles = new HashSet<string>();
            foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>())
            {
                if (!TryReadAxisAlignedRectangleBoundary(boundary, out var minX, out var maxX, out var minY, out var maxY) ||
                    !seenRectangles.Add($"{minX}:{maxX}:{minY}:{maxY}"))
                {
                    continue;
                }

                elements.Add(RectangleOutline(
                    minX - distanceTicks,
                    maxX + distanceTicks,
                    minY - distanceTicks,
                    maxY + distanceTicks));
                if (maxX - minX > distanceTicks * 2 &&
                    maxY - minY > distanceTicks * 2)
                {
                    elements.Add(RectangleOutline(
                        minX + distanceTicks,
                        maxX - distanceTicks,
                        minY + distanceTicks,
                        maxY - distanceTicks));
                }
            }

            var seenLoops = new HashSet<string>();
            foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>())
            {
                if (TryReadCircleBoundary(boundary, out _, out _, out _) ||
                    TryReadAxisAlignedRectangleBoundary(boundary, out _, out _, out _, out _) ||
                    !TryReadClosedSegmentLoopBoundary(boundary, out var points))
                {
                    continue;
                }

                var key = string.Join("|", points.Select(point => $"{point.X}:{point.Y}"));
                if (!seenLoops.Add(key))
                {
                    continue;
                }

                if (TryOffsetPolygon(points, Math.Abs(distanceTicks), outward: true, out var outer))
                {
                    elements.Add(PolygonBoundary(outer));
                }

                if (TryOffsetPolygon(points, Math.Abs(distanceTicks), outward: false, out var inner))
                {
                    elements.Add(PolygonBoundary(inner));
                }
            }

            return [.. elements];
        }

        static double ShortestSweep(double startAngle, double endAngle)
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

        static bool SamePoint((long X, long Y) left, (long X, long Y) right) =>
            left.X == right.X && left.Y == right.Y;

        static double SignedArea(IReadOnlyList<(long X, long Y)> points)
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

        static bool TryOffsetPolygon(
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
            var lines = new (double X, double Y, double Dx, double Dy)[points.Count];
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
                lines[index] = (
                    start.X + normalX * distanceTicks * side,
                    start.Y + normalY * distanceTicks * side,
                    dx,
                    dy);
            }

            var output = new List<(long X, long Y)>();
            for (var index = 0; index < lines.Length; index++)
            {
                var previous = lines[(index + lines.Length - 1) % lines.Length];
                var current = lines[index];
                var cross = previous.Dx * current.Dy - previous.Dy * current.Dx;
                if (Math.Abs(cross) < 0.0001d)
                {
                    return false;
                }

                var deltaX = current.X - previous.X;
                var deltaY = current.Y - previous.Y;
                var t = (deltaX * current.Dy - deltaY * current.Dx) / cross;
                output.Add((
                    (long)Math.Round(previous.X + previous.Dx * t),
                    (long)Math.Round(previous.Y + previous.Dy * t)));
            }

            if (Math.Abs(SignedArea(output)) < 1d)
            {
                return false;
            }

            offsetPoints = [.. output];
            return true;
        }

        Relation OffsetSideWitness(
            IElement source,
            IElement offsetBoundary,
            long distanceTicks,
            IElement? outwardOnlyCircleSource = null)
        {
            const double markerOffset = 5d;
            const double markerLength = 13d;
            var positiveMarkers = new List<IElement>();
            var inverseMarkers = new List<IElement>();
            var outwardOnlyCircleKeys = (outwardOnlyCircleSource is null
                    ? []
                    : CircleReads(outwardOnlyCircleSource)
                        .Select(circle => $"{circle.CenterX}:{circle.CenterY}:{circle.Radius}"))
                .ToHashSet();

            void AddLineMarker(List<IElement> markers, long startX, long startY, long endX, long endY, int sideSign)
            {
                var deltaX = endX - startX;
                var deltaY = endY - startY;
                var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (length <= 0d)
                {
                    return;
                }

                var tangentX = deltaX / length;
                var tangentY = deltaY / length;
                var normalX = -tangentY;
                var normalY = tangentX;
                var halfLength = Math.Min(markerLength * 0.5d, Math.Max(4d, length * 0.18d));
                var centerX = (startX + endX) * 0.5d + normalX * sideSign * markerOffset;
                var centerY = (startY + endY) * 0.5d + normalY * sideSign * markerOffset;
                markers.Add(Segment(
                    (long)Math.Round(centerX - tangentX * halfLength),
                    (long)Math.Round(centerY - tangentY * halfLength),
                    (long)Math.Round(centerX + tangentX * halfLength),
                    (long)Math.Round(centerY + tangentY * halfLength)));
            }

            void AddArcMarker(List<IElement> markers, long centerX, long centerY, long startX, long startY, long endX, long endY, int sideSign)
            {
                var radius = Math.Min(
                    Math.Sqrt(Math.Pow(startX - centerX, 2) + Math.Pow(startY - centerY, 2)),
                    Math.Sqrt(Math.Pow(endX - centerX, 2) + Math.Pow(endY - centerY, 2)));
                if (radius <= 0d)
                {
                    return;
                }

                var startAngle = Math.Atan2(startY - centerY, startX - centerX);
                var sweep = ShortestSweep(startAngle, Math.Atan2(endY - centerY, endX - centerX));
                if (Math.Abs(sweep) <= 0.0001d)
                {
                    return;
                }

                var direction = Math.Sign(sweep);
                var midAngle = startAngle + sweep * 0.5d;
                var tangentX = -Math.Sin(midAngle) * direction;
                var tangentY = Math.Cos(midAngle) * direction;
                var normalX = -tangentY;
                var normalY = tangentX;
                var arcX = centerX + Math.Cos(midAngle) * radius;
                var arcY = centerY + Math.Sin(midAngle) * radius;
                var centerMarkerX = arcX + normalX * sideSign * markerOffset;
                var centerMarkerY = arcY + normalY * sideSign * markerOffset;
                var halfLength = Math.Min(markerLength * 0.5d, Math.Max(4d, Math.Abs(sweep) * radius * 0.18d));
                markers.Add(Segment(
                    (long)Math.Round(centerMarkerX - tangentX * halfLength),
                    (long)Math.Round(centerMarkerY - tangentY * halfLength),
                    (long)Math.Round(centerMarkerX + tangentX * halfLength),
                    (long)Math.Round(centerMarkerY + tangentY * halfLength)));
            }

            void AddLineSideMarkers(long startX, long startY, long endX, long endY, bool positive, bool inverse)
            {
                if (positive)
                {
                    AddLineMarker(positiveMarkers, startX, startY, endX, endY, sideSign: 1);
                }

                if (inverse)
                {
                    AddLineMarker(inverseMarkers, startX, startY, endX, endY, sideSign: -1);
                }
            }

            void AddArcSideMarkers(long centerX, long centerY, long startX, long startY, long endX, long endY, bool positive, bool inverse)
            {
                if (positive)
                {
                    AddArcMarker(positiveMarkers, centerX, centerY, startX, startY, endX, endY, sideSign: 1);
                }

                if (inverse)
                {
                    AddArcMarker(inverseMarkers, centerX, centerY, startX, startY, endX, endY, sideSign: -1);
                }
            }

            void AddClosedBoundaryMarkers(ClosedSharedFrameBoundaryRead boundary, bool positive, bool inverse)
            {
                var arcSpanIds = (boundary.Arcs ?? [])
                    .Select(arc => SourceId(arc.Span.Source))
                    .ToHashSet();
                foreach (var segment in boundary.Segments)
                {
                    if (arcSpanIds.Contains(SourceId(segment.Source)) ||
                        !TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY))
                    {
                        continue;
                    }

                    AddLineSideMarkers(startX, startY, endX, endY, positive, inverse);
                }

                foreach (var arc in boundary.Arcs ?? [])
                {
                    if (!TryReadPointTicks(arc.Center, out var centerX, out var centerY) ||
                        !TryReadPointTicks(arc.Start, out var startX, out var startY) ||
                        !TryReadPointTicks(arc.End, out var endX, out var endY))
                    {
                        continue;
                    }

                    AddArcSideMarkers(centerX, centerY, startX, startY, endX, endY, positive, inverse);
                }
            }

            void AddElementBoundaryMarkers(IElement boundaryElement, bool positive, bool inverse)
            {
                foreach (var boundary in GraphicReadGraph.From(boundaryElement)
                    .ReadsOf<ClosedSharedFrameBoundaryRead>()
                    .DistinctBy(boundary => SourceId(boundary.Source)))
                {
                    AddClosedBoundaryMarkers(boundary, positive, inverse);
                }
            }

            foreach (var (startX, startY, endX, endY) in SettledOpenSharedFrameLineSegmentTicks(source))
            {
                AddLineSideMarkers(startX, startY, endX, endY, positive: true, inverse: true);

                var deltaX = endX - startX;
                var deltaY = endY - startY;
                var length = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (length <= 0d)
                {
                    continue;
                }

                var distance = Math.Abs(distanceTicks);
                var normalX = (long)Math.Round(-deltaY / length * distance);
                var normalY = (long)Math.Round(deltaX / length * distance);
                AddLineSideMarkers(
                    startX + normalX,
                    startY + normalY,
                    endX + normalX,
                    endY + normalY,
                    positive: false,
                    inverse: true);
                AddLineSideMarkers(
                    startX - normalX,
                    startY - normalY,
                    endX - normalX,
                    endY - normalY,
                    positive: true,
                    inverse: false);
            }

            foreach (var arc in OpenSharedFrameArcs(source))
            {
                if (!TryReadPointTicks(arc.Center, out var centerX, out var centerY) ||
                    !TryReadPointTicks(arc.Start, out var startX, out var startY) ||
                    !TryReadPointTicks(arc.End, out var endX, out var endY))
                {
                    continue;
                }

                AddArcSideMarkers(centerX, centerY, startX, startY, endX, endY, positive: true, inverse: true);
                var startRadius = Math.Sqrt(
                    Math.Pow(startX - centerX, 2) +
                    Math.Pow(startY - centerY, 2));
                var endRadius = Math.Sqrt(
                    Math.Pow(endX - centerX, 2) +
                    Math.Pow(endY - centerY, 2));
                var radius = Math.Min(startRadius, endRadius);
                if (radius <= 0d)
                {
                    continue;
                }

                (long X, long Y) OffsetArcPoint(long pointX, long pointY, double offsetRadius)
                {
                    var dx = pointX - centerX;
                    var dy = pointY - centerY;
                    var length = Math.Sqrt(dx * dx + dy * dy);
                    return length <= 0
                        ? (pointX, pointY)
                        : (
                            centerX + (long)Math.Round(dx / length * offsetRadius),
                            centerY + (long)Math.Round(dy / length * offsetRadius));
                }

                void AddGeneratedArcMarkers((long X, long Y) generatedStart, (long X, long Y) generatedEnd)
                {
                    var startAngle = Math.Atan2(startY - centerY, startX - centerX);
                    var sweep = ShortestSweep(startAngle, Math.Atan2(endY - centerY, endX - centerX));
                    if (Math.Abs(sweep) <= 0.0001d)
                    {
                        return;
                    }

                    var direction = Math.Sign(sweep);
                    var midAngle = startAngle + sweep * 0.5d;
                    var tangentX = -Math.Sin(midAngle) * direction;
                    var tangentY = Math.Cos(midAngle) * direction;
                    var leftNormalX = -tangentY;
                    var leftNormalY = tangentX;
                    var sourceMidRadius = radius;
                    var generatedRadius = Math.Min(
                        Math.Sqrt(Math.Pow(generatedStart.X - centerX, 2) + Math.Pow(generatedStart.Y - centerY, 2)),
                        Math.Sqrt(Math.Pow(generatedEnd.X - centerX, 2) + Math.Pow(generatedEnd.Y - centerY, 2)));
                    var sourceMidX = centerX + Math.Cos(midAngle) * sourceMidRadius;
                    var sourceMidY = centerY + Math.Sin(midAngle) * sourceMidRadius;
                    var generatedMidX = centerX + Math.Cos(midAngle) * generatedRadius;
                    var generatedMidY = centerY + Math.Sin(midAngle) * generatedRadius;
                    var sideDot = (generatedMidX - sourceMidX) * leftNormalX +
                        (generatedMidY - sourceMidY) * leftNormalY;
                    AddArcSideMarkers(
                        centerX,
                        centerY,
                        generatedStart.X,
                        generatedStart.Y,
                        generatedEnd.X,
                        generatedEnd.Y,
                        positive: sideDot < 0d,
                        inverse: sideDot >= 0d);
                }

                var outerStart = OffsetArcPoint(startX, startY, radius + distanceTicks);
                var outerEnd = OffsetArcPoint(endX, endY, radius + distanceTicks);
                AddGeneratedArcMarkers(outerStart, outerEnd);
                if (radius > distanceTicks)
                {
                    var innerStart = OffsetArcPoint(startX, startY, radius - distanceTicks);
                    var innerEnd = OffsetArcPoint(endX, endY, radius - distanceTicks);
                    AddGeneratedArcMarkers(innerStart, innerEnd);
                }
            }

            var graph = GraphicReadGraph.From(source);
            foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
                .DistinctBy(boundary => SourceId(boundary.Source)))
            {
                var outwardOnly = TryReadCircleBoundary(boundary, out var centerX, out var centerY, out var radius) &&
                    outwardOnlyCircleKeys.Contains($"{centerX}:{centerY}:{radius}");
                AddClosedBoundaryMarkers(boundary, positive: !outwardOnly, inverse: true);
            }

            foreach (var boundary in graph.ReadsOf<ClosedSharedFrameBoundaryRead>()
                .DistinctBy(boundary => SourceId(boundary.Source)))
            {
                if (TryReadCircleBoundary(boundary, out var centerX, out var centerY, out var radius))
                {
                    if (outwardOnlyCircleKeys.Contains($"{centerX}:{centerY}:{radius}"))
                    {
                        AddElementBoundaryMarkers(Circle(centerX, centerY, radius + distanceTicks), positive: true, inverse: false);
                        if (radius > distanceTicks)
                        {
                            AddElementBoundaryMarkers(Circle(centerX, centerY, radius - distanceTicks), positive: false, inverse: true);
                        }

                        continue;
                    }

                    AddElementBoundaryMarkers(Circle(centerX, centerY, radius + distanceTicks), positive: true, inverse: false);
                    if (radius > distanceTicks)
                    {
                        AddElementBoundaryMarkers(Circle(centerX, centerY, radius - distanceTicks), positive: false, inverse: true);
                    }

                    continue;
                }

                if (!TryReadAxisAlignedRectangleBoundary(boundary, out var minX, out var maxX, out var minY, out var maxY))
                {
                    if (TryReadClosedSegmentLoopBoundary(boundary, out var points))
                    {
                        if (TryOffsetPolygon(points, Math.Abs(distanceTicks), outward: true, out var outer))
                        {
                            AddElementBoundaryMarkers(PolygonBoundary(outer), positive: true, inverse: false);
                        }

                        if (TryOffsetPolygon(points, Math.Abs(distanceTicks), outward: false, out var inner))
                        {
                            AddElementBoundaryMarkers(PolygonBoundary(inner), positive: false, inverse: true);
                        }
                    }

                    continue;
                }

                AddElementBoundaryMarkers(
                    RectangleOutline(
                        minX - distanceTicks,
                        maxX + distanceTicks,
                        minY - distanceTicks,
                        maxY + distanceTicks),
                    positive: true,
                    inverse: false);
                if (maxX - minX > distanceTicks * 2 &&
                    maxY - minY > distanceTicks * 2)
                {
                    AddElementBoundaryMarkers(
                        RectangleOutline(
                            minX + distanceTicks,
                            maxX - distanceTicks,
                            minY + distanceTicks,
                            maxY - distanceTicks),
                        positive: false,
                        inverse: true);
                }
            }

            return GraphicOffsetSurface.BoundarySideWitness(
                new Carrier(),
                source,
                offsetBoundary,
                Group([.. positiveMarkers]),
                Group([.. inverseMarkers]));
        }

        IElement[] DerivedOrthogonalInteriorFills(
            IElement offsetSideWitness,
            long distanceTicks,
            IElement? outwardOnlyCircleSource = null) =>
            GraphicOrthogonalFillSurface.OrthogonalFillParticipants(
                offsetSideWitness,
                distanceTicks,
                outwardOnlyCircleSource);

        IElement[] PositiveAreaFillsFromAreaFrame(IElement sourceAreaFrame) =>
            [.. GraphicReadGraph.From(sourceAreaFrame)
                .ReadsOf<SharedFrameAreaRead>()
                .Where(area => !area.IsAntiArea)
                .DistinctBy(area => SourceId(area.Source))
                .Select(area => area.Source)];

        (long X, long Y) HorizontalCircleCusp(long centerX, long centerY, long radius, long lineY, long sideX)
        {
            var lineOffset = lineY - centerY;
            return (
                centerX + sideX * (long)Math.Round(Math.Sqrt(radius * radius - lineOffset * lineOffset)),
                lineY);
        }

        (long X, long Y) VerticalCircleCusp(long centerX, long centerY, long radius, long lineX, long sideY)
        {
            var lineOffset = lineX - centerX;
            return (
                lineX,
                centerY + sideY * (long)Math.Round(Math.Sqrt(radius * radius - lineOffset * lineOffset)));
        }

        Relation SegmentBetween((long X, long Y) start, (long X, long Y) end) =>
            Segment(start.X, start.Y, end.X, end.Y);

        Relation ArcBetween((long X, long Y) center, (long X, long Y) start, (long X, long Y) end) =>
            Arc(center.X, center.Y, start.X, start.Y, end.X, end.Y);

        (long X, long Y) CircleSite(long centerX, long centerY, long offsetX, long offsetY) =>
            (centerX + offsetX, centerY + offsetY);

        ((long X, long Y) Center, (long X, long Y) LineTangent, (long X, long Y) CircleTangent)
            HorizontalExternalFillet(long centerX, long centerY, long radius, long lineY, long sideX, long filletRadius)
        {
            var outwardY = Math.Sign(lineY - centerY);
            var filletCenterY = lineY + outwardY * filletRadius;
            var filletDistance = radius + filletRadius;
            var deltaY = filletCenterY - centerY;
            var filletCenterX = centerX + sideX * (long)Math.Round(Math.Sqrt(
                filletDistance * filletDistance -
                deltaY * deltaY));
            var scale = (double)radius / filletDistance;
            var circleTangent = (
                X: centerX + (long)Math.Round((filletCenterX - centerX) * scale),
                Y: centerY + (long)Math.Round((filletCenterY - centerY) * scale));
            return ((filletCenterX, filletCenterY), (filletCenterX, lineY), circleTangent);
        }

        ((long X, long Y) Center, (long X, long Y) LineTangent, (long X, long Y) CircleTangent)
            VerticalExternalFillet(long centerX, long centerY, long radius, long lineX, long sideY, long filletRadius)
        {
            var outwardX = Math.Sign(lineX - centerX);
            var filletCenterX = lineX + outwardX * filletRadius;
            var filletDistance = radius + filletRadius;
            var deltaX = filletCenterX - centerX;
            var filletCenterY = centerY + sideY * (long)Math.Round(Math.Sqrt(
                filletDistance * filletDistance -
                deltaX * deltaX));
            var scale = (double)radius / filletDistance;
            var circleTangent = (
                X: centerX + (long)Math.Round((filletCenterX - centerX) * scale),
                Y: centerY + (long)Math.Round((filletCenterY - centerY) * scale));
            return ((filletCenterX, filletCenterY), (lineX, filletCenterY), circleTangent);
        }

        bool TryReadRadialProfile(
            IElement sourceAreaFrame,
            out long profileArmReach,
            out long profileHalfWidth,
            out long profileCenterOuter,
            out long profileEndpointOuter)
        {
            var graph = GraphicReadGraph.From(sourceAreaFrame);
            var positiveCircles = graph.ReadsOf<SharedFrameAreaRead>()
                .Where(area => !area.IsAntiArea)
                .Select(area =>
                {
                    var read = TryReadCircleBoundary(area.Boundary, out var centerX, out var centerY, out var radius);
                    return (read, centerX, centerY, radius);
                })
                .Where(circle => circle.read)
                .DistinctBy(circle => $"{circle.centerX}:{circle.centerY}:{circle.radius}")
                .ToArray();
            var centerCircle = positiveCircles
                .Where(circle => circle.centerX == 0 && circle.centerY == 0)
                .OrderByDescending(circle => circle.radius)
                .FirstOrDefault();
            var endpointCircle = positiveCircles
                .Where(circle => circle.centerX != 0 || circle.centerY != 0)
                .OrderByDescending(circle => Math.Max(Math.Abs(circle.centerX), Math.Abs(circle.centerY)))
                .ThenByDescending(circle => circle.radius)
                .FirstOrDefault();
            var halfWidths = graph.ReadsOf<SharedFrameAreaRead>()
                .Where(area => !area.IsAntiArea && area.Boundary.Arcs is not { Count: > 0 })
                .Select(area =>
                {
                    if (!TryReadBoundaryBounds(area.Boundary, out var minX, out var maxX, out var minY, out var maxY))
                    {
                        return 0L;
                    }

                    return Math.Min(maxX - minX, maxY - minY) / 2;
                })
                .Where(width => width > 0)
                .Order()
                .ToArray();

            profileArmReach = Math.Max(Math.Abs(endpointCircle.centerX), Math.Abs(endpointCircle.centerY));
            profileHalfWidth = halfWidths.FirstOrDefault();
            profileCenterOuter = centerCircle.radius;
            profileEndpointOuter = endpointCircle.radius;
            return profileArmReach > 0 &&
                profileHalfWidth > 0 &&
                profileCenterOuter > 0 &&
                profileEndpointOuter > 0;
        }

        bool TryReadSeedOffsetProfile(
            IElement sourceCenterCircle,
            IElement sourceEndpointCircles,
            long distanceTicks,
            out long profileArmReach,
            out long profileHalfWidth,
            out long profileCenterOuter,
            out long profileEndpointOuter)
        {
            var center = CircleReads(sourceCenterCircle)
                .OrderByDescending(circle => circle.Radius)
                .FirstOrDefault();
            var endpoint = CircleReads(sourceEndpointCircles)
                .OrderByDescending(circle => Math.Max(Math.Abs(circle.CenterX), Math.Abs(circle.CenterY)))
                .ThenByDescending(circle => circle.Radius)
                .FirstOrDefault();
            profileArmReach = Math.Max(Math.Abs(endpoint.CenterX), Math.Abs(endpoint.CenterY));
            profileHalfWidth = Math.Abs(distanceTicks);
            profileCenterOuter = center.Radius + Math.Abs(distanceTicks);
            profileEndpointOuter = endpoint.Radius + Math.Abs(distanceTicks);
            return profileArmReach > 0 &&
                profileHalfWidth > 0 &&
                profileCenterOuter > 0 &&
                profileEndpointOuter > 0;
        }

        Relation ExteriorSharpOutline(
            long profileArmReach,
            long profileHalfWidth,
            long profileCenterOuter,
            long profileEndpointOuter)
        {
            var centerTopLeft = VerticalCircleCusp(0, 0, profileCenterOuter, -profileHalfWidth, 1);
            var centerTopRight = VerticalCircleCusp(0, 0, profileCenterOuter, profileHalfWidth, 1);
            var centerRightTop = HorizontalCircleCusp(0, 0, profileCenterOuter, profileHalfWidth, 1);
            var centerRightBottom = HorizontalCircleCusp(0, 0, profileCenterOuter, -profileHalfWidth, 1);
            var centerBottomRight = VerticalCircleCusp(0, 0, profileCenterOuter, profileHalfWidth, -1);
            var centerBottomLeft = VerticalCircleCusp(0, 0, profileCenterOuter, -profileHalfWidth, -1);
            var centerLeftBottom = HorizontalCircleCusp(0, 0, profileCenterOuter, -profileHalfWidth, -1);
            var centerLeftTop = HorizontalCircleCusp(0, 0, profileCenterOuter, profileHalfWidth, -1);
            var topLeft = VerticalCircleCusp(0, profileArmReach, profileEndpointOuter, -profileHalfWidth, -1);
            var topRight = VerticalCircleCusp(0, profileArmReach, profileEndpointOuter, profileHalfWidth, -1);
            var rightTop = HorizontalCircleCusp(profileArmReach, 0, profileEndpointOuter, profileHalfWidth, -1);
            var rightBottom = HorizontalCircleCusp(profileArmReach, 0, profileEndpointOuter, -profileHalfWidth, -1);
            var bottomRight = VerticalCircleCusp(0, -profileArmReach, profileEndpointOuter, profileHalfWidth, 1);
            var bottomLeft = VerticalCircleCusp(0, -profileArmReach, profileEndpointOuter, -profileHalfWidth, 1);
            var leftBottom = HorizontalCircleCusp(-profileArmReach, 0, profileEndpointOuter, -profileHalfWidth, 1);
            var leftTop = HorizontalCircleCusp(-profileArmReach, 0, profileEndpointOuter, profileHalfWidth, 1);

            return Chain(
                ArcBetween((0, profileArmReach), topLeft, CircleSite(0, profileArmReach, -profileEndpointOuter, 0)),
                Arc(0, profileArmReach, -profileEndpointOuter, profileArmReach, 0, profileArmReach + profileEndpointOuter),
                Arc(0, profileArmReach, 0, profileArmReach + profileEndpointOuter, profileEndpointOuter, profileArmReach),
                ArcBetween((0, profileArmReach), CircleSite(0, profileArmReach, profileEndpointOuter, 0), topRight),
                SegmentBetween(topRight, centerTopRight),
                ArcBetween((0, 0), centerTopRight, centerRightTop),
                SegmentBetween(centerRightTop, rightTop),
                ArcBetween((profileArmReach, 0), rightTop, CircleSite(profileArmReach, 0, profileEndpointOuter, 0)),
                ArcBetween((profileArmReach, 0), CircleSite(profileArmReach, 0, profileEndpointOuter, 0), rightBottom),
                SegmentBetween(rightBottom, centerRightBottom),
                ArcBetween((0, 0), centerRightBottom, centerBottomRight),
                SegmentBetween(centerBottomRight, bottomRight),
                ArcBetween((0, -profileArmReach), bottomRight, CircleSite(0, -profileArmReach, profileEndpointOuter, 0)),
                Arc(0, -profileArmReach, profileEndpointOuter, -profileArmReach, 0, -profileArmReach - profileEndpointOuter),
                Arc(0, -profileArmReach, 0, -profileArmReach - profileEndpointOuter, -profileEndpointOuter, -profileArmReach),
                ArcBetween((0, -profileArmReach), CircleSite(0, -profileArmReach, -profileEndpointOuter, 0), bottomLeft),
                SegmentBetween(bottomLeft, centerBottomLeft),
                ArcBetween((0, 0), centerBottomLeft, centerLeftBottom),
                SegmentBetween(centerLeftBottom, leftBottom),
                ArcBetween((-profileArmReach, 0), leftBottom, CircleSite(-profileArmReach, 0, -profileEndpointOuter, 0)),
                ArcBetween((-profileArmReach, 0), CircleSite(-profileArmReach, 0, -profileEndpointOuter, 0), leftTop),
                SegmentBetween(leftTop, centerLeftTop),
                ArcBetween((0, 0), centerLeftTop, centerTopLeft),
                SegmentBetween(centerTopLeft, topLeft));
        }

        Relation ExteriorRoundedOutline(
            long profileArmReach,
            long profileHalfWidth,
            long profileCenterOuter,
            long profileEndpointOuter)
        {
            const long cornerRadius = 14;
            var centerTopLeft = VerticalExternalFillet(0, 0, profileCenterOuter, -profileHalfWidth, 1, cornerRadius);
            var centerTopRight = VerticalExternalFillet(0, 0, profileCenterOuter, profileHalfWidth, 1, cornerRadius);
            var centerRightTop = HorizontalExternalFillet(0, 0, profileCenterOuter, profileHalfWidth, 1, cornerRadius);
            var centerRightBottom = HorizontalExternalFillet(0, 0, profileCenterOuter, -profileHalfWidth, 1, cornerRadius);
            var centerBottomRight = VerticalExternalFillet(0, 0, profileCenterOuter, profileHalfWidth, -1, cornerRadius);
            var centerBottomLeft = VerticalExternalFillet(0, 0, profileCenterOuter, -profileHalfWidth, -1, cornerRadius);
            var centerLeftBottom = HorizontalExternalFillet(0, 0, profileCenterOuter, -profileHalfWidth, -1, cornerRadius);
            var centerLeftTop = HorizontalExternalFillet(0, 0, profileCenterOuter, profileHalfWidth, -1, cornerRadius);
            var topLeft = VerticalExternalFillet(0, profileArmReach, profileEndpointOuter, -profileHalfWidth, -1, cornerRadius);
            var topRight = VerticalExternalFillet(0, profileArmReach, profileEndpointOuter, profileHalfWidth, -1, cornerRadius);
            var rightTop = HorizontalExternalFillet(profileArmReach, 0, profileEndpointOuter, profileHalfWidth, -1, cornerRadius);
            var rightBottom = HorizontalExternalFillet(profileArmReach, 0, profileEndpointOuter, -profileHalfWidth, -1, cornerRadius);
            var bottomRight = VerticalExternalFillet(0, -profileArmReach, profileEndpointOuter, profileHalfWidth, 1, cornerRadius);
            var bottomLeft = VerticalExternalFillet(0, -profileArmReach, profileEndpointOuter, -profileHalfWidth, 1, cornerRadius);
            var leftBottom = HorizontalExternalFillet(-profileArmReach, 0, profileEndpointOuter, -profileHalfWidth, 1, cornerRadius);
            var leftTop = HorizontalExternalFillet(-profileArmReach, 0, profileEndpointOuter, profileHalfWidth, 1, cornerRadius);

            return Chain(
                ArcBetween((0, profileArmReach), topLeft.CircleTangent, CircleSite(0, profileArmReach, -profileEndpointOuter, 0)),
                Arc(0, profileArmReach, -profileEndpointOuter, profileArmReach, 0, profileArmReach + profileEndpointOuter),
                Arc(0, profileArmReach, 0, profileArmReach + profileEndpointOuter, profileEndpointOuter, profileArmReach),
                ArcBetween((0, profileArmReach), CircleSite(0, profileArmReach, profileEndpointOuter, 0), topRight.CircleTangent),
                ArcBetween(topRight.Center, topRight.CircleTangent, topRight.LineTangent),
                SegmentBetween(topRight.LineTangent, centerTopRight.LineTangent),
                ArcBetween(centerTopRight.Center, centerTopRight.LineTangent, centerTopRight.CircleTangent),
                ArcBetween((0, 0), centerTopRight.CircleTangent, centerRightTop.CircleTangent),
                ArcBetween(centerRightTop.Center, centerRightTop.CircleTangent, centerRightTop.LineTangent),
                SegmentBetween(centerRightTop.LineTangent, rightTop.LineTangent),
                ArcBetween(rightTop.Center, rightTop.LineTangent, rightTop.CircleTangent),
                ArcBetween((profileArmReach, 0), rightTop.CircleTangent, CircleSite(profileArmReach, 0, profileEndpointOuter, 0)),
                ArcBetween((profileArmReach, 0), CircleSite(profileArmReach, 0, profileEndpointOuter, 0), rightBottom.CircleTangent),
                ArcBetween(rightBottom.Center, rightBottom.CircleTangent, rightBottom.LineTangent),
                SegmentBetween(rightBottom.LineTangent, centerRightBottom.LineTangent),
                ArcBetween(centerRightBottom.Center, centerRightBottom.LineTangent, centerRightBottom.CircleTangent),
                ArcBetween((0, 0), centerRightBottom.CircleTangent, centerBottomRight.CircleTangent),
                ArcBetween(centerBottomRight.Center, centerBottomRight.CircleTangent, centerBottomRight.LineTangent),
                SegmentBetween(centerBottomRight.LineTangent, bottomRight.LineTangent),
                ArcBetween(bottomRight.Center, bottomRight.LineTangent, bottomRight.CircleTangent),
                ArcBetween((0, -profileArmReach), bottomRight.CircleTangent, CircleSite(0, -profileArmReach, profileEndpointOuter, 0)),
                Arc(0, -profileArmReach, profileEndpointOuter, -profileArmReach, 0, -profileArmReach - profileEndpointOuter),
                Arc(0, -profileArmReach, 0, -profileArmReach - profileEndpointOuter, -profileEndpointOuter, -profileArmReach),
                ArcBetween((0, -profileArmReach), CircleSite(0, -profileArmReach, -profileEndpointOuter, 0), bottomLeft.CircleTangent),
                ArcBetween(bottomLeft.Center, bottomLeft.CircleTangent, bottomLeft.LineTangent),
                SegmentBetween(bottomLeft.LineTangent, centerBottomLeft.LineTangent),
                ArcBetween(centerBottomLeft.Center, centerBottomLeft.LineTangent, centerBottomLeft.CircleTangent),
                ArcBetween((0, 0), centerBottomLeft.CircleTangent, centerLeftBottom.CircleTangent),
                ArcBetween(centerLeftBottom.Center, centerLeftBottom.CircleTangent, centerLeftBottom.LineTangent),
                SegmentBetween(centerLeftBottom.LineTangent, leftBottom.LineTangent),
                ArcBetween(leftBottom.Center, leftBottom.LineTangent, leftBottom.CircleTangent),
                ArcBetween((-profileArmReach, 0), leftBottom.CircleTangent, CircleSite(-profileArmReach, 0, -profileEndpointOuter, 0)),
                ArcBetween((-profileArmReach, 0), CircleSite(-profileArmReach, 0, -profileEndpointOuter, 0), leftTop.CircleTangent),
                ArcBetween(leftTop.Center, leftTop.CircleTangent, leftTop.LineTangent),
                SegmentBetween(leftTop.LineTangent, centerLeftTop.LineTangent),
                ArcBetween(centerLeftTop.Center, centerLeftTop.LineTangent, centerLeftTop.CircleTangent),
                ArcBetween((0, 0), centerLeftTop.CircleTangent, centerTopLeft.CircleTangent),
                ArcBetween(centerTopLeft.Center, centerTopLeft.CircleTangent, centerTopLeft.LineTangent),
                SegmentBetween(centerTopLeft.LineTangent, topLeft.LineTangent),
                ArcBetween(topLeft.Center, topLeft.LineTangent, topLeft.CircleTangent));
        }

        bool TryReadBoundaryBounds(
            ClosedSharedFrameBoundaryRead boundary,
            out long minX,
            out long maxX,
            out long minY,
            out long maxY)
        {
            var points = boundary.Segments
                .SelectMany(segment => new[] { segment.Start, segment.End })
                .Select(point =>
                {
                    TryReadPointTicks(point, out var pointX, out var pointY);
                    return (pointX, pointY);
                })
                .ToArray();
            if (points.Length == 0)
            {
                minX = 0;
                maxX = 0;
                minY = 0;
                maxY = 0;
                return false;
            }

            minX = points.Min(point => point.pointX);
            maxX = points.Max(point => point.pointX);
            minY = points.Min(point => point.pointY);
            maxY = points.Max(point => point.pointY);
            return true;
        }

        Relation HExteriorSharpOutline(
            long originX,
            long reachTicks,
            long columnX,
            long columnHalfWidth,
            long crossbarHalfWidth,
            long centerHalfX,
            long centerHalfY,
            long capRadius)
        {
            var leftColumnX = originX - columnX;
            var rightColumnX = originX + columnX;
            var leftColumnInnerX = leftColumnX + columnHalfWidth;
            var rightColumnInnerX = rightColumnX - columnHalfWidth;
            var centerLeftX = originX - centerHalfX;
            var centerRightX = originX + centerHalfX;
            var centerTopY = centerHalfY;
            var centerBottomY = -centerHalfY;
            var tangentOffsetY = (long)Math.Round(Math.Sqrt(capRadius * capRadius - columnHalfWidth * columnHalfWidth));

            (long X, long Y) TopCapLeft(long centerX) => (centerX - columnHalfWidth, reachTicks - tangentOffsetY);
            (long X, long Y) TopCapRight(long centerX) => (centerX + columnHalfWidth, reachTicks - tangentOffsetY);
            (long X, long Y) BottomCapLeft(long centerX) => (centerX - columnHalfWidth, -reachTicks + tangentOffsetY);
            (long X, long Y) BottomCapRight(long centerX) => (centerX + columnHalfWidth, -reachTicks + tangentOffsetY);

            IElement[] TopCapArcs(long centerX) =>
            [
                Arc(centerX, reachTicks, centerX - columnHalfWidth, reachTicks - tangentOffsetY, centerX - capRadius, reachTicks),
                Arc(centerX, reachTicks, centerX - capRadius, reachTicks, centerX, reachTicks + capRadius),
                Arc(centerX, reachTicks, centerX, reachTicks + capRadius, centerX + capRadius, reachTicks),
                Arc(centerX, reachTicks, centerX + capRadius, reachTicks, centerX + columnHalfWidth, reachTicks - tangentOffsetY)
            ];

            IElement[] BottomCapArcs(long centerX) =>
            [
                Arc(centerX, -reachTicks, centerX + columnHalfWidth, -reachTicks + tangentOffsetY, centerX + capRadius, -reachTicks),
                Arc(centerX, -reachTicks, centerX + capRadius, -reachTicks, centerX, -reachTicks - capRadius),
                Arc(centerX, -reachTicks, centerX, -reachTicks - capRadius, centerX - capRadius, -reachTicks),
                Arc(centerX, -reachTicks, centerX - capRadius, -reachTicks, centerX - columnHalfWidth, -reachTicks + tangentOffsetY)
            ];

            return Chain(
                TopCapArcs(leftColumnX)
                    .Append(SegmentBetween(TopCapRight(leftColumnX), (leftColumnInnerX, crossbarHalfWidth)))
                    .Append(Segment(leftColumnInnerX, crossbarHalfWidth, centerLeftX, crossbarHalfWidth))
                    .Append(Segment(centerLeftX, crossbarHalfWidth, centerLeftX, centerTopY))
                    .Append(Segment(centerLeftX, centerTopY, centerRightX, centerTopY))
                    .Append(Segment(centerRightX, centerTopY, centerRightX, crossbarHalfWidth))
                    .Append(Segment(centerRightX, crossbarHalfWidth, rightColumnInnerX, crossbarHalfWidth))
                    .Append(SegmentBetween((rightColumnInnerX, crossbarHalfWidth), TopCapLeft(rightColumnX)))
                    .Concat(TopCapArcs(rightColumnX))
                    .Append(SegmentBetween(TopCapRight(rightColumnX), BottomCapRight(rightColumnX)))
                    .Concat(BottomCapArcs(rightColumnX))
                    .Append(SegmentBetween(BottomCapLeft(rightColumnX), (rightColumnInnerX, -crossbarHalfWidth)))
                    .Append(Segment(rightColumnInnerX, -crossbarHalfWidth, centerRightX, -crossbarHalfWidth))
                    .Append(Segment(centerRightX, -crossbarHalfWidth, centerRightX, centerBottomY))
                    .Append(Segment(centerRightX, centerBottomY, centerLeftX, centerBottomY))
                    .Append(Segment(centerLeftX, centerBottomY, centerLeftX, -crossbarHalfWidth))
                    .Append(Segment(centerLeftX, -crossbarHalfWidth, leftColumnInnerX, -crossbarHalfWidth))
                    .Append(SegmentBetween((leftColumnInnerX, -crossbarHalfWidth), BottomCapRight(leftColumnX)))
                    .Concat(BottomCapArcs(leftColumnX))
                    .Append(SegmentBetween(BottomCapLeft(leftColumnX), TopCapLeft(leftColumnX)))
                    .ToArray());
        }

        IElement[] AntiAreaFillsFromOutlines(IElement outlines)
        {
            if (outlines is Relation outlineFrame &&
                outlineFrame.Right is Carrier holder &&
                outlineFrame.TryReadOrderedElementsOn(holder, out var outlineElements))
            {
                return [.. outlineElements.Select(outline => GraphicStructure.AntiAreaFill(outline))];
            }

            return [GraphicStructure.AntiAreaFill(outlines)];
        }

        IElement[] AreaFillsFromOutlines(IElement outlines)
        {
            if (outlines is Relation outlineFrame &&
                outlineFrame.Right is Carrier holder &&
                outlineFrame.TryReadOrderedElementsOn(holder, out var outlineElements))
            {
                return [.. outlineElements.Select(outline => GraphicStructure.AreaFill(outline))];
            }

            return [GraphicStructure.AreaFill(outlines)];
        }

        bool HasClosedBoundaryReads(IElement source) =>
            GraphicReadGraph.From(source).ReadsOf<ClosedSharedFrameBoundaryRead>().Any();

        var centerPlusStrokes = Group(
            Segment(-armReach, 0, armReach, 0),
            Segment(0, -armReach, 0, armReach));
        var centerCircle = Circle(0, 0, centerRadius);
        var centerPlus = Group(centerPlusStrokes, centerCircle);
        var lineTipPattern = GraphicBoundaryConceptSurface.LineTipWitnessPattern(new Carrier(), new Placeholder());
        var lineTipDomain = GraphicBoundaryConceptSurface.LineTipCandidateDomain(new Carrier(), centerPlusStrokes);
        var lineTipSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            centerPlusStrokes,
            GraphicSelectionSurface.Frame(new Carrier(), centerPlusStrokes, lineTipDomain),
            lineTipPattern);
        var endpointCircleAction = GraphicBoundaryConceptSurface.CircleCapsFromLineTipSelection(
            new Carrier(),
            lineTipSelection,
            endpointRadius);
        var endpointCircles = GraphicBoundaryConceptSurface.TryReadCircleCapsFromLineTipSelection(
            endpointCircleAction,
            out _,
            out _,
            out _,
            out var generatedEndpointCircles,
            out _)
            ? generatedEndpointCircles
            : UnknownPoint();
        var trimmedCenterPlusStrokes = TrimOpenTipsByEndpointCircles(centerPlusStrokes, endpointCircles);
        var endpointState = Group(trimmedCenterPlusStrokes, centerCircle, endpointCircles);
        var offsetBoundaries = Group(DerivedOffsetBoundaryElements(endpointState, offset, endpointCircles));
        var offsetSideWitness = OffsetSideWitness(endpointState, offsetBoundaries, offset, endpointCircles);
        var fillParticipants = DerivedOrthogonalInteriorFills(offsetSideWitness, offset, endpointCircles);
        var endpointHoleOutlines = EndpointHoleOutlines(endpointCircles);
        var separateFills = GraphicBooleanSurface.AreaFrame(new Carrier(), fillParticipants);
        var areaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), fillParticipants);
        var mergedAreaFrame = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(new Carrier(), areaFrame);
        var unionInteriorHoles = Group(
            GraphicAreaSettlementSurface.SettledAntiBoundaryOutlinesFromAreas(new Carrier(), areaFrame),
            endpointHoleOutlines);
        var fillUnion = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), areaFrame);
        GraphicBooleanSurface.TryReadAreaUnion(fillUnion, out _, out var fillOutlineWitness, out _, out _);
        if (!TryReadRadialProfile(
                areaFrame,
                out var profileArmReach,
                out var profileHalfWidth,
                out var profileCenterOuter,
                out var profileEndpointOuter) &&
            !TryReadSeedOffsetProfile(
                centerCircle,
                endpointCircles,
                offset,
                out profileArmReach,
                out profileHalfWidth,
                out profileCenterOuter,
                out profileEndpointOuter))
        {
            throw new InvalidOperationException("Radial shape ladder could not derive the exterior outline profile from its area frame.");
        }

        var exteriorSharpOutline = ExteriorSharpOutline(
            profileArmReach,
            profileHalfWidth,
            profileCenterOuter,
            profileEndpointOuter);
        var offsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), endpointState, Distance(offset)),
            offsetSideWitness);
        var fillEvidence = GraphicParameterSurface.Frame(new Carrier(), offsetEvidence, areaFrame);
        var fillUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), fillUnion, fillOutlineWitness);
        var cuspPattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var interiorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), unionInteriorHoles);
        var exteriorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), exteriorSharpOutline);
        var cuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            fillUnion,
            GraphicSelectionSurface.Frame(new Carrier(), fillUnion, interiorCuspDomain),
            cuspPattern);
        var roundedCuspAction = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            cuspSelection,
            unionInteriorHoles,
            roundedCornerRadius);
        var roundedUnionInteriorHoles = GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            roundedCuspAction,
            out _,
            out _,
            out _,
            out var generatedRoundedInteriorHoles,
            out _)
            ? generatedRoundedInteriorHoles
            : unionInteriorHoles;
        roundedUnionInteriorHoles = Group(roundedUnionInteriorHoles, endpointHoleOutlines);
        IElement[] positiveFillParticipants = PositiveAreaFillsFromAreaFrame(mergedAreaFrame);
        var roundedSourceAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. positiveFillParticipants, .. AntiAreaFillsFromOutlines(roundedUnionInteriorHoles)]);
        var roundedAreaFrame = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(new Carrier(), roundedSourceAreaFrame);
        var roundedUnionOutlineWitness = Group(
            GraphicOutlineSurface.PositiveBoundaryOutlinesFromAreas(new Carrier(), roundedAreaFrame),
            roundedUnionInteriorHoles);
        var roundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), roundedAreaFrame, roundedUnionOutlineWitness);
        var roundedUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), roundedUnion, roundedUnionOutlineWitness);
        var exteriorRoundedOutline = ExteriorRoundedOutline(
            profileArmReach,
            profileHalfWidth,
            profileCenterOuter,
            profileEndpointOuter);
        var exteriorRoundedOuterFill = GraphicStructure.AreaFill(exteriorRoundedOutline);
        var exteriorRoundedSourceAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [exteriorRoundedOuterFill, .. AntiAreaFillsFromOutlines(roundedUnionInteriorHoles)]);
        var exteriorRoundedAreaFrame = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(
            new Carrier(),
            exteriorRoundedSourceAreaFrame);
        var exteriorSharpOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), roundedUnion, exteriorSharpOutline);
        var exteriorCuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            roundedUnion,
            GraphicSelectionSurface.Frame(new Carrier(), roundedUnion, exteriorCuspDomain),
            cuspPattern);
        var exteriorRoundedUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            exteriorRoundedAreaFrame,
            Chain(exteriorRoundedOutline, roundedUnionInteriorHoles));
        var finalColoredUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            exteriorRoundedAreaFrame,
            Chain(exteriorRoundedOutline, roundedUnionInteriorHoles));

        const long hOriginX = 265;
        const long hReach = 108;
        const long hColumnX = 72;
        const long hSquareHalfX = 34;
        const long hSquareHalfY = 58;
        const long hCrossbarHalfWidth = offset;
        const long hRoundedCornerRadius = 12;
        var hEndpointHoleRadius = endpointHoleRadius;
        var hVariant = Math.Clamp(comparisonVariant, 0, 2);
        Relation HEndpointHole(long xTicks, long yTicks) =>
            Circle(xTicks, yTicks, hEndpointHoleRadius);

        Relation hCenterStrokes;
        IElement hBodyShape;
        Relation hShape;
        Relation hExteriorSharpOutline;
        Relation hUnionInteriorHoles;
        string hVariantName;
        if (hVariant == 2)
        {
            hVariantName = "crescent + bent-arms";
            const long crescentOriginX = -70;
            const long bentOriginX = hOriginX;
            var crescentCenterCircle = CircleBoundaryAt(crescentOriginX, 0, 0, 0, 78);
            var crescentCenterStrokes = Group(
                ArcAt(crescentOriginX, 0, -170, 0, -95, 108, -95, -108),
                ArcAt(crescentOriginX, 0, 170, 0, 95, -108, 95, 108));
            var crescentShape = Group(crescentCenterStrokes, crescentCenterCircle);
            var crescentExterior = Group(
                ArcBandAt(crescentOriginX, 0, -170, 0, -95, 108, -95, -108, offset),
                ArcBandAt(crescentOriginX, 0, 170, 0, 95, -108, 95, 108, offset),
                crescentCenterCircle,
                Circle(crescentOriginX - 95, 108, endpointRadius + offset),
                Circle(crescentOriginX - 95, -108, endpointRadius + offset),
                Circle(crescentOriginX + 95, -108, endpointRadius + offset),
                Circle(crescentOriginX + 95, 108, endpointRadius + offset));
            var crescentHoles = Group(
                CircleBoundaryAt(crescentOriginX, 0, 0, 0, 32),
                HEndpointHole(crescentOriginX - 95, 108),
                HEndpointHole(crescentOriginX - 95, -108),
                HEndpointHole(crescentOriginX + 95, -108),
                HEndpointHole(crescentOriginX + 95, 108));

            var bentLeftTop = PAt(bentOriginX, 0, -92, 122);
            var bentLeftKnee = PAt(bentOriginX, 0, -18, -06);
            var bentLeftLower = PAt(bentOriginX, 0, -132, -88);
            var bentRightTop = PAt(bentOriginX, 0, 92, 122);
            var bentRightKnee = PAt(bentOriginX, 0, 18, -06);
            var bentRightLower = PAt(bentOriginX, 0, 132, -88);
            var bentCenterStrokes = Group(
                SegmentFrom(bentLeftTop, bentLeftKnee),
                SegmentFrom(bentLeftKnee, bentLeftLower),
                SegmentFrom(bentRightTop, bentRightKnee),
                SegmentFrom(bentRightKnee, bentRightLower));
            var bentCenterDiamond = DiamondOutlineAt(bentOriginX, 0, 76, 88);
            var bentShape = Group(bentCenterStrokes, bentCenterDiamond);
            var bentExterior = Group(
                bentCenterDiamond,
                Circle(bentOriginX - 92, 122, endpointRadius + offset),
                Circle(bentOriginX - 132, -88, endpointRadius + offset),
                Circle(bentOriginX + 92, 122, endpointRadius + offset),
                Circle(bentOriginX + 132, -88, endpointRadius + offset));
            var bentHoles = Group(
                DiamondOutlineAt(bentOriginX, 0, 56, 68),
                HEndpointHole(bentOriginX - 92, 122),
                HEndpointHole(bentOriginX - 132, -88),
                HEndpointHole(bentOriginX + 92, 122),
                HEndpointHole(bentOriginX + 132, -88));

            hCenterStrokes = Group(crescentCenterStrokes, bentCenterStrokes);
            hBodyShape = Group(crescentCenterCircle, bentCenterDiamond);
            hShape = Group(crescentShape, bentShape);
            hExteriorSharpOutline = Group(crescentExterior, bentExterior);
            hUnionInteriorHoles = Group(crescentHoles, bentHoles);
        }
        else if (hVariant == 1)
        {
            hVariantName = "bug + YT";
            const long curvedOriginX = -70;
            const long curvedBodyShiftY = 24;
            const long yOriginX = hOriginX;
            const long yCircleCenterY = 46;
            var curvedCenterSquare = RectangleOutline(
                curvedOriginX - 32,
                curvedOriginX + 32,
                -68 + curvedBodyShiftY,
                48 + curvedBodyShiftY);
            var curvedCenterStrokes = Group(
                ArcAt(curvedOriginX, 0, 0, 112, -78, 108, 78, 108),
                ArcAt(curvedOriginX, 0, 0, -112, -108, -92, 108, -92));
            var curvedShape = Group(curvedCenterStrokes, curvedCenterSquare);
            var curvedExterior = Group(
                ArcBandAt(curvedOriginX, 0, 0, 112, -78, 108, 78, 108, offset),
                ArcBandAt(curvedOriginX, 0, 0, -112, -108, -92, 108, -92, offset),
                curvedCenterSquare,
                Circle(curvedOriginX - 78, 108, endpointRadius + offset),
                Circle(curvedOriginX + 78, 108, endpointRadius + offset),
                Circle(curvedOriginX - 108, -92, endpointRadius + offset),
                Circle(curvedOriginX + 108, -92, endpointRadius + offset));
            var curvedHoles = Group(
                RectangleOutline(curvedOriginX - 24, curvedOriginX + 24, -52 + curvedBodyShiftY, 32 + curvedBodyShiftY),
                HEndpointHole(curvedOriginX - 78, 108),
                HEndpointHole(curvedOriginX + 78, 108),
                HEndpointHole(curvedOriginX - 108, -92),
                HEndpointHole(curvedOriginX + 108, -92));

            var yJoint = PAt(yOriginX, 0, 0, 38);
            var yBottomJoint = PAt(yOriginX, 0, 0, -82);
            var yCenterStrokes = Group(
                SegmentFrom(PAt(yOriginX, 0, -92, 116), yJoint),
                SegmentFrom(PAt(yOriginX, 0, 92, 116), yJoint),
                SegmentFrom(yJoint, yBottomJoint),
                SegmentFrom(yBottomJoint, PAt(yOriginX, 0, -110, -82)),
                SegmentFrom(yBottomJoint, PAt(yOriginX, 0, 110, -82)));
            var yCenterCircle = CircleBoundaryAt(yOriginX, 0, 0, yCircleCenterY, 58);
            var yShape = Group(yCenterStrokes, yCenterCircle);
            var yExterior = Group(
                Chain(
                    SegmentAt(yOriginX, 0, -130, -102, 130, -102),
                    SegmentAt(yOriginX, 0, 130, -102, 130, -62),
                    SegmentAt(yOriginX, 0, 130, -62, 18, -62),
                    SegmentAt(yOriginX, 0, 18, -62, 18, 20),
                    SegmentAt(yOriginX, 0, 18, 20, 104, 104),
                    SegmentAt(yOriginX, 0, 104, 104, 78, 130),
                    SegmentAt(yOriginX, 0, 78, 130, 0, 52),
                    SegmentAt(yOriginX, 0, 0, 52, -78, 130),
                    SegmentAt(yOriginX, 0, -78, 130, -104, 104),
                    SegmentAt(yOriginX, 0, -104, 104, -18, 20),
                    SegmentAt(yOriginX, 0, -18, 20, -18, -62),
                    SegmentAt(yOriginX, 0, -18, -62, -130, -62),
                    SegmentAt(yOriginX, 0, -130, -62, -130, -102)),
                yCenterCircle,
                Circle(yOriginX - 92, 116, endpointRadius + offset),
                Circle(yOriginX + 92, 116, endpointRadius + offset),
                Circle(yOriginX - 110, -82, endpointRadius + offset),
                Circle(yOriginX + 110, -82, endpointRadius + offset));
            var yHoles = Group(
                CircleBoundaryAt(yOriginX, 0, 0, yCircleCenterY, 32),
                HEndpointHole(yOriginX - 92, 116),
                HEndpointHole(yOriginX + 92, 116),
                HEndpointHole(yOriginX - 110, -82),
                HEndpointHole(yOriginX + 110, -82));

            hCenterStrokes = Group(curvedCenterStrokes, yCenterStrokes);
            hBodyShape = Group(curvedCenterSquare, yCenterCircle);
            hShape = Group(curvedShape, yShape);
            hExteriorSharpOutline = Group(curvedExterior, yExterior);
            hUnionInteriorHoles = Group(curvedHoles, yHoles);
        }
        else
        {
            hVariantName = "H/square";
            var hLeftBottom = PAt(hOriginX, 0, -hColumnX, -hReach);
            var hLeftMid = PAt(hOriginX, 0, -hColumnX, 0);
            var hLeftTop = PAt(hOriginX, 0, -hColumnX, hReach);
            var hRightBottom = PAt(hOriginX, 0, hColumnX, -hReach);
            var hRightMid = PAt(hOriginX, 0, hColumnX, 0);
            var hRightTop = PAt(hOriginX, 0, hColumnX, hReach);
            hCenterStrokes = Group(
                SegmentFrom(hLeftBottom, hLeftMid),
                SegmentFrom(hLeftMid, hLeftTop),
                SegmentFrom(hRightBottom, hRightMid),
                SegmentFrom(hRightMid, hRightTop),
                SegmentFrom(hLeftMid, hRightMid));
            var hCenterSquare = RectangleOutline(
                hOriginX - hSquareHalfX,
                hOriginX + hSquareHalfX,
                -hSquareHalfY,
                hSquareHalfY);
            hBodyShape = hCenterSquare;
            hShape = Group(hCenterStrokes, hCenterSquare);
            hExteriorSharpOutline = HExteriorSharpOutline(
                hOriginX,
                hReach,
                hColumnX,
                offset,
                hCrossbarHalfWidth,
                hSquareHalfX + offset,
                hSquareHalfY + offset,
                endpointRadius + offset);
            hUnionInteriorHoles = Group(
                RectangleOutline(
                    hOriginX - hSquareHalfX + offset,
                    hOriginX + hSquareHalfX - offset,
                    hCrossbarHalfWidth,
                    hSquareHalfY - offset),
                RectangleOutline(
                    hOriginX - hSquareHalfX + offset,
                    hOriginX + hSquareHalfX - offset,
                    -hSquareHalfY + offset,
                    -hCrossbarHalfWidth),
                HEndpointHole(hOriginX - hColumnX, -hReach),
                HEndpointHole(hOriginX - hColumnX, hReach),
                HEndpointHole(hOriginX + hColumnX, -hReach),
                HEndpointHole(hOriginX + hColumnX, hReach));
        }
        var hLineTipDomain = GraphicBoundaryConceptSurface.LineTipCandidateDomain(new Carrier(), hCenterStrokes);
        var hLineTipSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            hCenterStrokes,
            GraphicSelectionSurface.Frame(new Carrier(), hCenterStrokes, hLineTipDomain),
            lineTipPattern);
        var hEndpointCircleAction = GraphicBoundaryConceptSurface.CircleCapsFromLineTipSelection(
            new Carrier(),
            hLineTipSelection,
            endpointRadius);
        var hEndpointCircles = GraphicBoundaryConceptSurface.TryReadCircleCapsFromLineTipSelection(
            hEndpointCircleAction,
            out _,
            out _,
            out _,
            out var generatedHEndpointCircles,
            out _)
            ? generatedHEndpointCircles
            : UnknownPoint();
        var hEndpointHoleOutlines = EndpointHoleOutlines(hEndpointCircles);
        var hTrimmedCenterStrokes = TrimOpenTipsByEndpointCircles(hCenterStrokes, hEndpointCircles);
        var hEndpointState = Group(hTrimmedCenterStrokes, hBodyShape, hEndpointCircles);
        var hOffsetBoundaries = Group(DerivedOffsetBoundaryElements(hEndpointState, offset, hEndpointCircles));
        var hOffsetSideWitness = OffsetSideWitness(hEndpointState, hOffsetBoundaries, offset, hEndpointCircles);
        var hFillParticipants = DerivedOrthogonalInteriorFills(hOffsetSideWitness, offset, hEndpointCircles);
        var hSeparateFills = GraphicBooleanSurface.AreaFrame(new Carrier(), hFillParticipants);
        var hAreaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), hFillParticipants);
        var hSettledAreaFrame = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(new Carrier(), hAreaFrame);
        var hMergedAreaFrame = hSettledAreaFrame;
        hUnionInteriorHoles = Group(
            GraphicAreaSettlementSurface.SettledAntiBoundaryOutlinesFromAreas(new Carrier(), hAreaFrame),
            hEndpointHoleOutlines);
        var hFillUnion = GraphicAreaSettlementSurface.SettledPositiveAreaUnion(new Carrier(), hAreaFrame);
        GraphicBooleanSurface.TryReadAreaUnion(hFillUnion, out _, out var hFillOutlineWitness, out _, out _);
        var hOffsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), hEndpointState, Distance(offset)),
            hOffsetSideWitness);
        var hFillEvidence = GraphicParameterSurface.Frame(new Carrier(), hOffsetEvidence, hMergedAreaFrame);
        var hFillUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), hFillUnion, hFillOutlineWitness);
        var hInteriorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), hUnionInteriorHoles);
        var hCuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            hFillUnion,
            GraphicSelectionSurface.Frame(new Carrier(), hFillUnion, hInteriorCuspDomain),
            cuspPattern);
        var hRoundedCuspAction = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            hCuspSelection,
            hUnionInteriorHoles,
            hRoundedCornerRadius);
        var hRoundedUnionInteriorHoles = GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            hRoundedCuspAction,
            out _,
            out _,
            out _,
            out var generatedHRoundedInteriorHoles,
            out _)
            ? generatedHRoundedInteriorHoles
            : hUnionInteriorHoles;
        hRoundedUnionInteriorHoles = Group(hRoundedUnionInteriorHoles, hEndpointHoleOutlines);
        IElement[] hPositiveFillParticipants = PositiveAreaFillsFromAreaFrame(hMergedAreaFrame);
        var hRoundedSourceAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. hPositiveFillParticipants, .. AntiAreaFillsFromOutlines(hRoundedUnionInteriorHoles)]);
        var hRoundedAreaFrame = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(new Carrier(), hRoundedSourceAreaFrame);
        var hRoundedUnionOutlineWitness = Group(
            GraphicOutlineSurface.PositiveBoundaryOutlinesFromAreas(new Carrier(), hRoundedAreaFrame),
            hRoundedUnionInteriorHoles);
        var hRoundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), hRoundedAreaFrame, hRoundedUnionOutlineWitness);
        var hRoundedUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), hRoundedUnion, hRoundedUnionOutlineWitness);
        var hExteriorSharpOutlineFromFill = GraphicOutlineSurface.PositiveBoundaryOutlinesFromAreas(new Carrier(), hRoundedAreaFrame);
        var hExteriorReadableSourceOutline = HasClosedBoundaryReads(hExteriorSharpOutlineFromFill)
            ? hExteriorSharpOutlineFromFill
            : hExteriorSharpOutline;
        var hExteriorSharpOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), hRoundedUnion, hExteriorSharpOutlineFromFill);
        var hExteriorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), hExteriorReadableSourceOutline);
        var hExteriorCuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            hRoundedUnion,
            GraphicSelectionSurface.Frame(new Carrier(), hRoundedUnion, hExteriorCuspDomain),
            cuspPattern);
        var hExteriorRoundedCuspAction = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            hExteriorCuspSelection,
            hExteriorSharpOutlineFromFill,
            hRoundedCornerRadius);
        var hExteriorRoundedOutline = GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            hExteriorRoundedCuspAction,
            out _,
            out _,
            out _,
            out var generatedHExteriorRoundedOutline,
            out _)
            && HasClosedBoundaryReads(generatedHExteriorRoundedOutline)
            ? generatedHExteriorRoundedOutline
            : hExteriorReadableSourceOutline;
        var hExteriorRoundedSourceAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. AreaFillsFromOutlines(hExteriorRoundedOutline), .. AntiAreaFillsFromOutlines(hRoundedUnionInteriorHoles)]);
        var hExteriorRoundedAreaFrame = GraphicAreaSettlementSurface.SettledPositiveAreaFrame(new Carrier(), hExteriorRoundedSourceAreaFrame);
        var hExteriorRoundedUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            hExteriorRoundedAreaFrame,
            Group(hExteriorRoundedOutline, hRoundedUnionInteriorHoles));
        var hFinalColoredUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            hExteriorRoundedAreaFrame,
            Group(hExteriorRoundedOutline, hRoundedUnionInteriorHoles));

        Relation Lanes(IElement originalPairLane, IElement comparisonPairLane) =>
            hVariant == 0
                ? Group(originalPairLane, comparisonPairLane)
                : Group(comparisonPairLane);

        var baseResult = Lanes(centerPlus, hShape);
        var endpointResult = Lanes(endpointState, hEndpointState);
        var offsetResult = Lanes(offsetSideWitness, hOffsetSideWitness);
        var separateFillResult = Lanes(separateFills, hSeparateFills);
        var mergeResult = Lanes(fillUnion, hFillUnion);
        var outlineResult = Lanes(fillUnionOutlineEvidence, hFillUnionOutlineEvidence);
        var cuspResult = Lanes(cuspSelection, hCuspSelection);
        var roundedResult = Lanes(roundedUnion, hRoundedUnion);
        var exteriorCuspResult = Lanes(exteriorCuspSelection, hExteriorCuspSelection);
        var exteriorRoundedResult = Lanes(exteriorRoundedUnion, hExteriorRoundedUnion);
        var finalColoredResult = Lanes(finalColoredUnion, hFinalColoredUnion);

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), baseResult);
        var endpointStep = RefinedRung(
            baseResult,
            lineTipPattern,
            Group(lineTipSelection, hLineTipSelection),
            baseResult,
            Group(endpointCircles, hEndpointCircles),
            endpointResult,
            Group(endpointCircleAction, hEndpointCircleAction));
        var offsetStep = RefinedRung(
            endpointResult,
            endpointResult,
            endpointResult,
            UnknownPoint(),
            Group(offsetSideWitness, hOffsetSideWitness),
            offsetResult,
            Group(offsetEvidence, hOffsetEvidence));
        var fillStep = RefinedRung(
            offsetResult,
            offsetResult,
            offsetResult,
            UnknownPoint(),
            Group(separateFills, hSeparateFills),
            separateFillResult,
            Group(fillEvidence, hFillEvidence));
        var mergeStep = RefinedRung(
            separateFillResult,
            separateFillResult,
            separateFillResult,
            UnknownPoint(),
            mergeResult,
            mergeResult,
            mergeResult);
        var outlineStep = RefinedRung(
            mergeResult,
            mergeResult,
            mergeResult,
            UnknownPoint(),
            mergeResult,
            mergeResult,
            Group(fillUnionOutlineEvidence, hFillUnionOutlineEvidence));
        var cuspStep = RefinedRung(
            mergeResult,
            cuspPattern,
            Group(interiorCuspDomain, hInteriorCuspDomain),
            mergeResult,
            cuspResult,
            cuspResult,
            GraphicParameterSurface.Frame(new Carrier(), mergeResult, Group(interiorCuspDomain, hInteriorCuspDomain), cuspResult));
        var roundedStep = RefinedRung(
            cuspResult,
            cuspPattern,
            cuspResult,
            UnknownPoint(),
            roundedResult,
            roundedResult,
            GraphicParameterSurface.Frame(
                new Carrier(),
                cuspResult,
                roundedResult,
                Group(roundedUnionOutlineEvidence, hRoundedUnionOutlineEvidence),
                Group(roundedCuspAction, hRoundedCuspAction)));
        var exteriorCuspStep = RefinedRung(
            roundedResult,
            cuspPattern,
            Group(exteriorCuspDomain, hExteriorCuspDomain),
            roundedResult,
            exteriorCuspResult,
            exteriorCuspResult,
            GraphicParameterSurface.Frame(
                new Carrier(),
                roundedResult,
                Group(exteriorSharpOutlineEvidence, hExteriorSharpOutlineEvidence),
                Group(exteriorCuspDomain, hExteriorCuspDomain),
                exteriorCuspResult));
        var exteriorRoundedStep = RefinedRung(
            exteriorCuspResult,
            cuspPattern,
            exteriorCuspResult,
            UnknownPoint(),
            exteriorRoundedResult,
            exteriorRoundedResult,
            GraphicParameterSurface.Frame(new Carrier(), exteriorCuspResult, exteriorRoundedResult, hExteriorRoundedCuspAction));
        var finalColoredStep = RefinedRung(
            exteriorRoundedResult,
            exteriorRoundedResult,
            exteriorRoundedResult,
            UnknownPoint(),
            finalColoredResult,
            finalColoredResult,
            GraphicParameterSurface.Frame(new Carrier(), exteriorRoundedResult, finalColoredResult));

        var rungs = new[]
        {
            baseRung,
            endpointStep.Rung,
            offsetStep.Rung,
            fillStep.Rung,
            mergeStep.Rung,
            outlineStep.Rung,
            cuspStep.Rung,
            roundedStep.Rung,
            exteriorCuspStep.Rung,
            exteriorRoundedStep.Rung,
            finalColoredStep.Rung,
        };
        var applications = new[]
        {
            endpointStep.Application,
            offsetStep.Application,
            fillStep.Application,
            mergeStep.Application,
            outlineStep.Application,
            cuspStep.Application,
            roundedStep.Application,
            exteriorCuspStep.Application,
            exteriorRoundedStep.Application,
            finalColoredStep.Application
        };
        var ladder = TimelineLadder(rungs, applications);

        return new SketchScene(
            "Radial Shape Ladder",
            $"Two centerline seeds evolve through endpoint rings, offset fills, union evidence, cusp selections, and rounded corner actions. Comparison seed: {hVariantName}.",
            ladder,
            seed,
            new SKRect(-245, -165, 430, 165),
            [
                $"{hVariantName} seeds",
                "Endpoint circles",
                "Offset boundary pairs",
                "Separate offset fills",
                "Merged fill color",
                "Composite outline",
                "Interior cusp selection",
                "Rounded interior corners",
                "Exterior cusp selection",
                "Rounded exterior corners",
                "Final colored area"
            ]);
    }
}
