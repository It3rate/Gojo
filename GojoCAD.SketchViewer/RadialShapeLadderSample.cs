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
        const long centerInner = centerRadius - offset;
        const long endpointRadius = 18;

        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation P(long xTicks, long yTicks) =>
            Point(frame, x, y, xTicks, yTicks);

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

        Relation CircleAt(long originX, long originY, long centerX, long centerY, long radius) =>
            Chain(
                ArcAt(originX, originY, centerX, centerY, centerX + radius, centerY, centerX, centerY + radius),
                ArcAt(originX, originY, centerX, centerY, centerX, centerY + radius, centerX - radius, centerY),
                ArcAt(originX, originY, centerX, centerY, centerX - radius, centerY, centerX, centerY - radius),
                ArcAt(originX, originY, centerX, centerY, centerX, centerY - radius, centerX + radius, centerY));

        Relation Circle(long centerX, long centerY, long radius) =>
            CircleAt(0, 0, centerX, centerY, radius);

        Relation RectangleOutline(long minX, long maxX, long minY, long maxY) =>
            Chain(
                Segment(minX, minY, maxX, minY),
                Segment(maxX, minY, maxX, maxY),
                Segment(maxX, maxY, minX, maxY),
                Segment(minX, maxY, minX, minY));

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

        IElement[] DerivedOffsetBoundaryElements(IElement source, long distanceTicks)
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

            return [.. elements];
        }

        IElement[] DerivedFillParticipants(IElement offsetStructure)
        {
            var participants = new List<IElement>();
            var lineGroups = OpenSharedFrameLineSegments(offsetStructure)
                .Select(segment =>
                {
                    TryReadSegmentTicks(segment, out var startX, out var startY, out var endX, out var endY);
                    var horizontal = Math.Abs(endX - startX) >= Math.Abs(endY - startY);
                    var key = horizontal
                        ? $"h:{Math.Min(startX, endX)}:{Math.Max(startX, endX)}"
                        : $"v:{Math.Min(startY, endY)}:{Math.Max(startY, endY)}";
                    return (key, startX, startY, endX, endY);
                })
                .GroupBy(line => line.key);
            foreach (var group in lineGroups)
            {
                var lines = group
                    .OrderBy(line =>
                    {
                        var horizontal = Math.Abs(line.endX - line.startX) >= Math.Abs(line.endY - line.startY);
                        return horizontal
                            ? line.startY + line.endY
                            : line.startX + line.endX;
                    })
                    .ToArray();
                if (lines.Length < 2)
                {
                    continue;
                }

                for (var index = 0; index + 1 < lines.Length; index += 2)
                {
                    participants.Add(PolygonFill(
                        (lines[index].startX, lines[index].startY),
                        (lines[index].endX, lines[index].endY),
                        (lines[index + 1].endX, lines[index + 1].endY),
                        (lines[index + 1].startX, lines[index + 1].startY)));
                }
            }

            var circleGroups = GraphicReadGraph.From(offsetStructure)
                .ReadsOf<ClosedSharedFrameBoundaryRead>()
                .Select(boundary =>
                {
                    var read = TryReadCircleBoundary(boundary, out var centerX, out var centerY, out var radius);
                    return (read, centerX, centerY, radius);
                })
                .Where(circle => circle.read)
                .DistinctBy(circle => $"{circle.centerX}:{circle.centerY}:{circle.radius}")
                .GroupBy(circle => $"{circle.centerX}:{circle.centerY}");
            foreach (var group in circleGroups)
            {
                var radii = group
                    .Select(circle => circle.radius)
                    .Distinct()
                    .OrderDescending()
                    .ToArray();
                if (radii.Length < 2)
                {
                    continue;
                }

                var first = group.First();
                participants.Add(RingFill(first.centerX, first.centerY, radii[0], radii[1]));
            }

            var rectangleGroups = GraphicReadGraph.From(offsetStructure)
                .ReadsOf<ClosedSharedFrameBoundaryRead>()
                .Select(boundary =>
                {
                    var read = TryReadAxisAlignedRectangleBoundary(
                        boundary,
                        out var minX,
                        out var maxX,
                        out var minY,
                        out var maxY);
                    return (read, minX, maxX, minY, maxY);
                })
                .Where(rectangle => rectangle.read)
                .DistinctBy(rectangle => $"{rectangle.minX}:{rectangle.maxX}:{rectangle.minY}:{rectangle.maxY}")
                .GroupBy(rectangle => $"{rectangle.minX + rectangle.maxX}:{rectangle.minY + rectangle.maxY}");
            foreach (var group in rectangleGroups)
            {
                var rectangles = group
                    .OrderByDescending(rectangle => (rectangle.maxX - rectangle.minX) * (rectangle.maxY - rectangle.minY))
                    .ToArray();
                if (rectangles.Length < 2)
                {
                    continue;
                }

                var outer = rectangles[0];
                var inner = rectangles[1];
                participants.Add(Chain(
                    GraphicStructure.AreaFill(RectangleOutline(outer.minX, outer.maxX, outer.minY, outer.maxY)),
                    GraphicStructure.AntiAreaFill(RectangleOutline(inner.minX, inner.maxX, inner.minY, inner.maxY))));
            }

            return [.. participants];
        }

        Relation PolygonFill(params (long X, long Y)[] points)
        {
            if (points.Length < 3)
            {
                return UnknownPoint();
            }

            var edges = new IElement[points.Length];
            for (var index = 0; index < points.Length; index++)
            {
                var start = points[index];
                var end = points[(index + 1) % points.Length];
                edges[index] = Segment(start.X, start.Y, end.X, end.Y);
            }

            return GraphicStructure.AreaFill(edges);
        }

        Relation DiskFill(long centerX, long centerY, long radius) =>
            GraphicStructure.AreaFill(Circle(centerX, centerY, radius));

        Relation AntiDiskFill(long centerX, long centerY, long radius) =>
            GraphicStructure.AntiAreaFill(Circle(centerX, centerY, radius));

        Relation RingFill(long centerX, long centerY, long outerRadius, long innerRadius) =>
            Chain(DiskFill(centerX, centerY, outerRadius), AntiDiskFill(centerX, centerY, innerRadius));

        Relation CenterHoleOutline(long innerRadius, long halfWidth, long signX, long signY)
        {
            var corner = P(signX * halfWidth, signY * halfWidth);
            var circleSide = (long)Math.Round(Math.Sqrt(innerRadius * innerRadius - halfWidth * halfWidth));
            return Chain(
                Segment(signX * halfWidth, signY * halfWidth, signX * halfWidth, signY * circleSide),
                Arc(0, 0, signX * halfWidth, signY * circleSide, signX * circleSide, signY * halfWidth),
                GraphicStructure.FrameSegment(P(signX * circleSide, signY * halfWidth), corner));
        }

        Relation CenterHoleAntiFill(long signX, long signY) =>
            GraphicStructure.AntiAreaFill(CenterHoleOutline(centerInner, offset, signX, signY));

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

        Relation RoundedCenterHoleOutline(long innerRadius, long halfWidth, long signX, long signY)
        {
            const long cornerRadius = 7;
            var filletCenterOffset = halfWidth + cornerRadius;
            var filletCenterRadius = innerRadius - cornerRadius;
            var straightTangent = (long)Math.Round(Math.Sqrt(
                filletCenterRadius * filletCenterRadius -
                filletCenterOffset * filletCenterOffset));
            var circleScale = (double)innerRadius / filletCenterRadius;
            var circleNearLine = (long)Math.Round(filletCenterOffset * circleScale);
            var circleNearArc = (long)Math.Round(straightTangent * circleScale);
            var verticalStart = (X: signX * halfWidth, Y: signY * (halfWidth + cornerRadius));
            var verticalEnd = (X: signX * halfWidth, Y: signY * straightTangent);
            var verticalArcEnd = (X: signX * circleNearLine, Y: signY * circleNearArc);
            var horizontalArcStart = (X: signX * circleNearArc, Y: signY * circleNearLine);
            var horizontalStart = (X: signX * straightTangent, Y: signY * halfWidth);
            var horizontalEnd = (X: signX * (halfWidth + cornerRadius), Y: signY * halfWidth);

            return Chain(
                Segment(verticalStart.X, verticalStart.Y, verticalEnd.X, verticalEnd.Y),
                Arc(
                    signX * filletCenterOffset,
                    signY * straightTangent,
                    verticalEnd.X,
                    verticalEnd.Y,
                    verticalArcEnd.X,
                    verticalArcEnd.Y),
                Arc(0, 0, verticalArcEnd.X, verticalArcEnd.Y, horizontalArcStart.X, horizontalArcStart.Y),
                Arc(
                    signX * straightTangent,
                    signY * filletCenterOffset,
                    horizontalArcStart.X,
                    horizontalArcStart.Y,
                    horizontalStart.X,
                    horizontalStart.Y),
                Segment(horizontalStart.X, horizontalStart.Y, horizontalEnd.X, horizontalEnd.Y),
                Arc(
                    signX * (offset + cornerRadius),
                    signY * (offset + cornerRadius),
                    horizontalEnd.X,
                    horizontalEnd.Y,
                    verticalStart.X,
                    verticalStart.Y));
        }

        Relation RoundedCenterHoleAntiFill(long signX, long signY) =>
            GraphicStructure.AntiAreaFill(RoundedCenterHoleOutline(centerInner, offset, signX, signY));

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
            const long cornerRadius = 6;
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

        Relation DerivedUnionInteriorHoleOutlines(IElement sourceAreaFrame, bool rounded)
        {
            var graph = GraphicReadGraph.From(sourceAreaFrame);
            var endpointHoles = new List<(long CenterX, long CenterY, long Radius)>();
            long? centerInnerRadius = null;
            foreach (var area in graph.ReadsOf<SharedFrameAreaRead>().Where(area => area.IsAntiArea))
            {
                if (!TryReadCircleBoundary(area.Boundary, out var holeCenterX, out var holeCenterY, out var holeRadius))
                {
                    continue;
                }

                if (holeCenterX == 0 && holeCenterY == 0)
                {
                    centerInnerRadius = holeRadius;
                }
                else
                {
                    endpointHoles.Add((holeCenterX, holeCenterY, holeRadius));
                }
            }

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
                .DefaultIfEmpty(offset)
                .Min();

            var innerRadius = centerInnerRadius ?? centerInner;
            var outlines = endpointHoles
                .Distinct()
                .Select(hole => (IElement)Circle(hole.CenterX, hole.CenterY, hole.Radius))
                .ToList();
            outlines.Add(rounded
                ? RoundedCenterHoleOutline(innerRadius, halfWidths, 1, 1)
                : CenterHoleOutline(innerRadius, halfWidths, 1, 1));
            outlines.Add(rounded
                ? RoundedCenterHoleOutline(innerRadius, halfWidths, -1, 1)
                : CenterHoleOutline(innerRadius, halfWidths, -1, 1));
            outlines.Add(rounded
                ? RoundedCenterHoleOutline(innerRadius, halfWidths, -1, -1)
                : CenterHoleOutline(innerRadius, halfWidths, -1, -1));
            outlines.Add(rounded
                ? RoundedCenterHoleOutline(innerRadius, halfWidths, 1, -1)
                : CenterHoleOutline(innerRadius, halfWidths, 1, -1));

            return Group([.. outlines]);
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
        var endpointState = Group(centerPlus, endpointCircles);
        var offsetBoundaries = Group(DerivedOffsetBoundaryElements(endpointState, offset));
        var fillParticipants = DerivedFillParticipants(offsetBoundaries);
        var centerHoleCutNE = CenterHoleAntiFill(1, 1);
        var centerHoleCutNW = CenterHoleAntiFill(-1, 1);
        var centerHoleCutSW = CenterHoleAntiFill(-1, -1);
        var centerHoleCutSE = CenterHoleAntiFill(1, -1);
        var roundedCenterHoleCutNE = RoundedCenterHoleAntiFill(1, 1);
        var roundedCenterHoleCutNW = RoundedCenterHoleAntiFill(-1, 1);
        var roundedCenterHoleCutSW = RoundedCenterHoleAntiFill(-1, -1);
        var roundedCenterHoleCutSE = RoundedCenterHoleAntiFill(1, -1);
        var separateFills = Group(fillParticipants);

        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. fillParticipants,
            centerHoleCutNE,
            centerHoleCutNW,
            centerHoleCutSW,
            centerHoleCutSE]);
        var roundedAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [.. fillParticipants,
            roundedCenterHoleCutNE,
            roundedCenterHoleCutNW,
            roundedCenterHoleCutSW,
            roundedCenterHoleCutSE]);
        var unionInteriorHoles = DerivedUnionInteriorHoleOutlines(areaFrame, rounded: false);
        var roundedUnionInteriorHoles = DerivedUnionInteriorHoleOutlines(roundedAreaFrame, rounded: true);
        if (!TryReadRadialProfile(
                areaFrame,
                out var profileArmReach,
                out var profileHalfWidth,
                out var profileCenterOuter,
                out var profileEndpointOuter))
        {
            throw new InvalidOperationException("Radial shape ladder could not derive the exterior outline profile from its area frame.");
        }

        if (!TryReadRadialProfile(
                roundedAreaFrame,
                out var roundedProfileArmReach,
                out var roundedProfileHalfWidth,
                out var roundedProfileCenterOuter,
                out var roundedProfileEndpointOuter))
        {
            throw new InvalidOperationException("Radial shape ladder could not derive the rounded exterior outline profile from its area frame.");
        }

        var exteriorSharpOutline = ExteriorSharpOutline(
            profileArmReach,
            profileHalfWidth,
            profileCenterOuter,
            profileEndpointOuter);
        var exteriorRoundedOutline = ExteriorRoundedOutline(
            roundedProfileArmReach,
            roundedProfileHalfWidth,
            roundedProfileCenterOuter,
            roundedProfileEndpointOuter);
        var exteriorRoundedOuterFill = GraphicStructure.AreaFill(exteriorRoundedOutline);
        var exteriorRoundedAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            [exteriorRoundedOuterFill, .. AntiAreaFillsFromOutlines(roundedUnionInteriorHoles)]);
        var offsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), endpointState, Distance(offset)),
            offsetBoundaries);
        var fillEvidence = GraphicParameterSurface.Frame(new Carrier(), offsetEvidence, areaFrame);
        var fillUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, unionInteriorHoles);
        var fillUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), fillUnion, unionInteriorHoles);
        var cuspPattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var interiorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), unionInteriorHoles);
        var exteriorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), exteriorSharpOutline);
        var cuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            fillUnion,
            GraphicSelectionSurface.Frame(new Carrier(), fillUnion, interiorCuspDomain),
            cuspPattern);
        var roundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), roundedAreaFrame, roundedUnionInteriorHoles);
        var roundedUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), roundedUnion, roundedUnionInteriorHoles);
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
        const long hRoundedCornerRadius = 4;
        var hEndpointHoleRadius = Math.Max(4, endpointRadius - offset);
        var hVariant = Math.Clamp(comparisonVariant, 0, 2);
        Relation HEndpointHole(long xTicks, long yTicks) =>
            Circle(xTicks, yTicks, hEndpointHoleRadius);

        Relation hCenterStrokes;
        Relation hShape;
        Relation hExteriorSharpOutline;
        Relation hUnionInteriorHoles;
        var hAdditionalPositiveFillOutlines = new List<IElement>();
        string hVariantName;
        if (hVariant == 1)
        {
            hVariantName = "curved-arm square";
            var hCenterSquare = RectangleOutline(
                hOriginX - 32,
                hOriginX + 32,
                -48,
                48);
            hCenterStrokes = Group(
                ArcAt(hOriginX, 0, 0, 112, -78, 108, 78, 108),
                ArcAt(hOriginX, 0, 0, -112, -108, -92, 108, -92));
            hShape = Group(hCenterStrokes, hCenterSquare);
            hExteriorSharpOutline = Chain(
                ArcAt(hOriginX, 0, 0, 112, -98, 104, 98, 104),
                SegmentAt(hOriginX, 0, 98, 104, 88, -94),
                ArcAt(hOriginX, 0, 0, -112, 88, -94, -88, -94),
                SegmentAt(hOriginX, 0, -88, -94, -98, 104));
            hUnionInteriorHoles = Group(
                RectangleOutline(hOriginX - 24, hOriginX + 24, -32, 32),
                HEndpointHole(hOriginX - 78, 108),
                HEndpointHole(hOriginX + 78, 108),
                HEndpointHole(hOriginX - 108, -92),
                HEndpointHole(hOriginX + 108, -92));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX - 78, 108, endpointRadius + offset));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX + 78, 108, endpointRadius + offset));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX - 108, -92, endpointRadius + offset));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX + 108, -92, endpointRadius + offset));
        }
        else if (hVariant == 2)
        {
            hVariantName = "Y/flat-bottom";
            var hYJoint = PAt(hOriginX, 0, 0, 32);
            var hBottomJoint = PAt(hOriginX, 0, 0, -82);
            hCenterStrokes = Group(
                SegmentFrom(PAt(hOriginX, 0, -78, 100), hYJoint),
                SegmentFrom(PAt(hOriginX, 0, 78, 100), hYJoint),
                SegmentFrom(hYJoint, hBottomJoint),
                SegmentFrom(hBottomJoint, PAt(hOriginX, 0, -95, -82)),
                SegmentFrom(hBottomJoint, PAt(hOriginX, 0, 95, -82)));
            var hCenterCircle = CircleAt(hOriginX, 0, 0, 22, 48);
            hShape = Group(hCenterStrokes, hCenterCircle);
            hExteriorSharpOutline = Chain(
                SegmentAt(hOriginX, 0, -112, -102, 112, -102),
                SegmentAt(hOriginX, 0, 112, -102, 112, -66),
                SegmentAt(hOriginX, 0, 112, -66, 18, -66),
                SegmentAt(hOriginX, 0, 18, -66, 18, 16),
                SegmentAt(hOriginX, 0, 18, 16, 88, 86),
                SegmentAt(hOriginX, 0, 88, 86, 66, 108),
                SegmentAt(hOriginX, 0, 66, 108, 0, 42),
                SegmentAt(hOriginX, 0, 0, 42, -66, 108),
                SegmentAt(hOriginX, 0, -66, 108, -88, 86),
                SegmentAt(hOriginX, 0, -88, 86, -18, 16),
                SegmentAt(hOriginX, 0, -18, 16, -18, -66),
                SegmentAt(hOriginX, 0, -18, -66, -112, -66),
                SegmentAt(hOriginX, 0, -112, -66, -112, -102));
            hUnionInteriorHoles = Group(
                CircleAt(hOriginX, 0, 0, 22, 32),
                HEndpointHole(hOriginX - 78, 100),
                HEndpointHole(hOriginX + 78, 100),
                HEndpointHole(hOriginX - 95, -82),
                HEndpointHole(hOriginX + 95, -82));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX, 22, 48));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX - 78, 100, endpointRadius + offset));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX + 78, 100, endpointRadius + offset));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX - 95, -82, endpointRadius + offset));
            hAdditionalPositiveFillOutlines.Add(Circle(hOriginX + 95, -82, endpointRadius + offset));
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
        var hEndpointState = Group(hShape, hEndpointCircles);
        var hOffsetBoundaries = Group(DerivedOffsetBoundaryElements(hEndpointState, offset));
        IElement[] HFillParticipants(IElement exteriorOutline, IElement antiOutlines) =>
        [
            GraphicStructure.AreaFill(exteriorOutline),
            .. hAdditionalPositiveFillOutlines.Select(outline => GraphicStructure.AreaFill(outline)),
            .. AntiAreaFillsFromOutlines(antiOutlines)
        ];

        var hFillParticipants = HFillParticipants(hExteriorSharpOutline, hUnionInteriorHoles);
        var hSeparateFills = Group(hFillParticipants);
        var hAreaFrame = GraphicBooleanSurface.AreaFrame(new Carrier(), hFillParticipants);
        var hMergedAreaFrame = hAreaFrame;
        var hOffsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), hEndpointState, Distance(offset)),
            hOffsetBoundaries);
        var hFillEvidence = GraphicParameterSurface.Frame(new Carrier(), hOffsetEvidence, hMergedAreaFrame);
        var hFillUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            hMergedAreaFrame,
            Group(hExteriorSharpOutline, hUnionInteriorHoles));
        var hFillUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), hFillUnion, hUnionInteriorHoles);
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
        var hRoundedAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            HFillParticipants(hExteriorSharpOutline, hRoundedUnionInteriorHoles));
        var hRoundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), hRoundedAreaFrame, hRoundedUnionInteriorHoles);
        var hRoundedUnionOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), hRoundedUnion, hRoundedUnionInteriorHoles);
        var hExteriorSharpOutlineEvidence = GraphicOutlineSurface.FromArea(new Carrier(), hRoundedUnion, hExteriorSharpOutline);
        var hExteriorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), hExteriorSharpOutline);
        var hExteriorCuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            hRoundedUnion,
            GraphicSelectionSurface.Frame(new Carrier(), hRoundedUnion, hExteriorCuspDomain),
            cuspPattern);
        var hExteriorRoundedCuspAction = GraphicBoundaryConceptSurface.RoundedBoundaryFromCuspSelection(
            new Carrier(),
            hExteriorCuspSelection,
            hExteriorSharpOutline,
            hRoundedCornerRadius);
        var hExteriorRoundedOutline = GraphicBoundaryConceptSurface.TryReadRoundedBoundaryFromCuspSelection(
            hExteriorRoundedCuspAction,
            out _,
            out _,
            out _,
            out var generatedHExteriorRoundedOutline,
            out _)
            ? generatedHExteriorRoundedOutline
            : hExteriorSharpOutline;
        var hExteriorRoundedAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            HFillParticipants(hExteriorRoundedOutline, hRoundedUnionInteriorHoles));
        var hExteriorRoundedUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            hExteriorRoundedAreaFrame,
            Group(hExteriorRoundedOutline, hRoundedUnionInteriorHoles));
        var hFinalColoredUnion = GraphicBooleanSurface.AreaUnion(
            new Carrier(),
            hExteriorRoundedAreaFrame,
            Group(hExteriorRoundedOutline, hRoundedUnionInteriorHoles));

        var baseResult = Group(centerPlus, hShape);
        var endpointResult = Group(endpointState, hEndpointState);
        var offsetResult = Group(offsetBoundaries, hOffsetBoundaries);
        var separateFillResult = Group(separateFills, hSeparateFills);
        var mergeResult = Group(fillUnion, hFillUnion);
        var outlineResult = Group(fillUnionOutlineEvidence, hFillUnionOutlineEvidence);
        var cuspResult = Group(cuspSelection, hCuspSelection);
        var roundedResult = Group(roundedUnion, hRoundedUnion);
        var exteriorCuspResult = Group(exteriorCuspSelection, hExteriorCuspSelection);
        var exteriorRoundedResult = Group(exteriorRoundedUnion, hExteriorRoundedUnion);
        var finalColoredResult = Group(finalColoredUnion, hFinalColoredUnion);

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
            Group(offsetBoundaries, hOffsetBoundaries),
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
                hRoundedCuspAction));
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
            new SKRect(-165, -165, 405, 165),
            [
                $"Plus/circle + {hVariantName} seeds",
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
