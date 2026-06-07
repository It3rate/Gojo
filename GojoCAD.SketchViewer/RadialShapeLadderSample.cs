using System.Linq;
using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static partial class SketchSamples
{
    private static SketchScene CreateRadialShapeLadder(int seed)
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

        Relation Distance(long ticks) =>
            Relation.Segment(new Carrier(), 0, Math.Abs(ticks));

        Relation Group(params IElement[] elements) =>
            Relation.OrderedGroup(new Carrier(), elements ?? []);

        Relation Segment(long startX, long startY, long endX, long endY) =>
            GraphicStructure.FrameSegment(P(startX, startY), P(endX, endY));

        Relation Arc(long centerX, long centerY, long startX, long startY, long endX, long endY) =>
            GraphicStructure.FrameArc(P(centerX, centerY), P(startX, startY), P(endX, endY));

        Relation Circle(long centerX, long centerY, long radius) =>
            Chain(
                Arc(centerX, centerY, centerX + radius, centerY, centerX, centerY + radius),
                Arc(centerX, centerY, centerX, centerY + radius, centerX - radius, centerY),
                Arc(centerX, centerY, centerX - radius, centerY, centerX, centerY - radius),
                Arc(centerX, centerY, centerX, centerY - radius, centerX + radius, centerY));

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

        IEnumerable<SharedFrameSegmentRead> OpenSharedFrameLineSegments(IElement source)
        {
            var graph = GraphicReadGraph.From(source);
            var arcSpanIds = graph.ReadsOf<SharedFrameArcRead>()
                .Select(arc => SourceId(arc.Span.Source))
                .ToHashSet();
            var seen = new HashSet<ulong>();

            foreach (var pathGraph in graph.ReadsOf<PathGraphRead>())
            foreach (var edge in pathGraph.Edges)
            {
                if (edge.Segment is not SharedFrameSegmentRead segment ||
                    !edge.IsSharedFrame ||
                    edge.Start.Valence != 1 ||
                    edge.End.Valence != 1 ||
                    arcSpanIds.Contains(SourceId(segment.Source)) ||
                    !seen.Add(SourceId(segment.Source)))
                {
                    continue;
                }

                yield return segment;
            }
        }

        IElement[] DerivedOffsetBoundaryElements(IElement source, long distanceTicks)
        {
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
                if (length <= 0)
                {
                    continue;
                }

                var normalX = (long)Math.Round(-deltaY / length * distanceTicks);
                var normalY = (long)Math.Round(deltaX / length * distanceTicks);
                elements.Add(Segment(startX + normalX, startY + normalY, endX + normalX, endY + normalY));
                elements.Add(Segment(startX - normalX, startY - normalY, endX - normalX, endY - normalY));
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
                var lines = group.ToArray();
                if (lines.Length != 2)
                {
                    continue;
                }

                participants.Add(PolygonFill(
                    (lines[0].startX, lines[0].startY),
                    (lines[0].endX, lines[0].endY),
                    (lines[1].endX, lines[1].endY),
                    (lines[1].startX, lines[1].startY)));
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

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), centerPlus);
        var endpointStep = RefinedRung(
            centerPlus,
            lineTipPattern,
            lineTipSelection,
            centerPlus,
            endpointCircles,
            endpointState,
            endpointCircleAction);
        var offsetStep = RefinedRung(
            endpointState,
            endpointState,
            endpointState,
            UnknownPoint(),
            offsetBoundaries,
            offsetBoundaries,
            offsetEvidence);
        var fillStep = RefinedRung(
            offsetBoundaries,
            offsetBoundaries,
            offsetBoundaries,
            UnknownPoint(),
            separateFills,
            separateFills,
            fillEvidence);
        var mergeStep = RefinedRung(
            separateFills,
            separateFills,
            separateFills,
            UnknownPoint(),
            fillUnion,
            fillUnion,
            fillUnion);
        var outlineStep = RefinedRung(
            fillUnion,
            fillUnion,
            fillUnion,
            UnknownPoint(),
            fillUnion,
            fillUnion,
            fillUnionOutlineEvidence);
        var cuspStep = RefinedRung(
            fillUnion,
            cuspPattern,
            interiorCuspDomain,
            fillUnion,
            cuspSelection,
            cuspSelection,
            GraphicParameterSurface.Frame(new Carrier(), fillUnion, interiorCuspDomain, cuspSelection));
        var roundedStep = RefinedRung(
            cuspSelection,
            cuspPattern,
            cuspSelection,
            UnknownPoint(),
            roundedUnion,
            roundedUnion,
            GraphicParameterSurface.Frame(new Carrier(), cuspSelection, roundedUnion, roundedUnionOutlineEvidence));
        var exteriorCuspStep = RefinedRung(
            roundedUnion,
            cuspPattern,
            exteriorCuspDomain,
            roundedUnion,
            exteriorCuspSelection,
            exteriorCuspSelection,
            GraphicParameterSurface.Frame(new Carrier(), roundedUnion, exteriorSharpOutlineEvidence, exteriorCuspDomain, exteriorCuspSelection));
        var exteriorRoundedStep = RefinedRung(
            exteriorCuspSelection,
            cuspPattern,
            exteriorCuspSelection,
            UnknownPoint(),
            exteriorRoundedUnion,
            exteriorRoundedUnion,
            GraphicParameterSurface.Frame(new Carrier(), exteriorCuspSelection, exteriorRoundedUnion));
        var finalColoredStep = RefinedRung(
            exteriorRoundedUnion,
            exteriorRoundedUnion,
            exteriorRoundedUnion,
            UnknownPoint(),
            finalColoredUnion,
            finalColoredUnion,
            GraphicParameterSurface.Frame(new Carrier(), exteriorRoundedUnion, finalColoredUnion));

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
            "Centerline plus and circle evolve through endpoint rings, offset fills, union evidence, cusp selections, and rounded interior/exterior corners.",
            ladder,
            seed,
            new SKRect(-165, -165, 165, 165),
            [
                "Plus + center circle",
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
