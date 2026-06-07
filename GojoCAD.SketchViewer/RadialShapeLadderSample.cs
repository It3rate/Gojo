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
        const long centerOuter = centerRadius + offset;
        const long centerInner = centerRadius - offset;
        const long endpointRadius = 18;
        const long endpointOuter = endpointRadius + offset;
        const long endpointInner = endpointRadius - offset;

        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation P(long xTicks, long yTicks) =>
            Point(frame, x, y, xTicks, yTicks);

        Relation Distance(long ticks) =>
            Relation.Segment(new Carrier(), 0, Math.Abs(ticks));

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

        Relation RectOutline(long left, long bottom, long right, long top) =>
            Chain(
                Segment(left, bottom, right, bottom),
                Segment(right, bottom, right, top),
                Segment(right, top, left, top),
                Segment(left, top, left, bottom));

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

        Relation OffsetBoundaryPairs() =>
            Chain(
                Segment(-armReach, offset, armReach, offset),
                Segment(-armReach, -offset, armReach, -offset),
                Segment(-offset, -armReach, -offset, armReach),
                Segment(offset, -armReach, offset, armReach),
                Circle(0, 0, centerOuter),
                Circle(0, 0, centerInner),
                Circle(armReach, 0, endpointOuter),
                Circle(armReach, 0, endpointInner),
                Circle(-armReach, 0, endpointOuter),
                Circle(-armReach, 0, endpointInner),
                Circle(0, armReach, endpointOuter),
                Circle(0, armReach, endpointInner),
                Circle(0, -armReach, endpointOuter),
                Circle(0, -armReach, endpointInner));

        Relation CompositeOutline() =>
            Chain(
                RectOutline(-armReach, -offset, armReach, offset),
                RectOutline(-offset, -armReach, offset, armReach),
                Circle(0, 0, centerOuter),
                Circle(0, 0, centerInner),
                Circle(armReach, 0, endpointOuter),
                Circle(armReach, 0, endpointInner),
                Circle(-armReach, 0, endpointOuter),
                Circle(-armReach, 0, endpointInner),
                Circle(0, armReach, endpointOuter),
                Circle(0, armReach, endpointInner),
                Circle(0, -armReach, endpointOuter),
                Circle(0, -armReach, endpointInner));

        Relation CenterHoleOutline(long signX, long signY)
        {
            var corner = P(signX * offset, signY * offset);
            var circleSide = (long)Math.Round(Math.Sqrt(centerInner * centerInner - offset * offset));
            return Chain(
                Segment(signX * offset, signY * offset, signX * offset, signY * circleSide),
                Arc(0, 0, signX * offset, signY * circleSide, signX * circleSide, signY * offset),
                GraphicStructure.FrameSegment(P(signX * circleSide, signY * offset), corner));
        }

        Relation CenterHoleAntiFill(long signX, long signY) =>
            GraphicStructure.AntiAreaFill(CenterHoleOutline(signX, signY));

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

        Relation RoundedCenterHoleOutline(long signX, long signY)
        {
            const long cornerRadius = 7;
            var filletCenterOffset = offset + cornerRadius;
            var filletCenterRadius = centerInner - cornerRadius;
            var straightTangent = (long)Math.Round(Math.Sqrt(
                filletCenterRadius * filletCenterRadius -
                filletCenterOffset * filletCenterOffset));
            var circleScale = (double)centerInner / filletCenterRadius;
            var circleNearLine = (long)Math.Round(filletCenterOffset * circleScale);
            var circleNearArc = (long)Math.Round(straightTangent * circleScale);
            var verticalStart = (X: signX * offset, Y: signY * (offset + cornerRadius));
            var verticalEnd = (X: signX * offset, Y: signY * straightTangent);
            var verticalArcEnd = (X: signX * circleNearLine, Y: signY * circleNearArc);
            var horizontalArcStart = (X: signX * circleNearArc, Y: signY * circleNearLine);
            var horizontalStart = (X: signX * straightTangent, Y: signY * offset);
            var horizontalEnd = (X: signX * (offset + cornerRadius), Y: signY * offset);

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
            GraphicStructure.AntiAreaFill(RoundedCenterHoleOutline(signX, signY));

        Relation ExteriorSharpOutline()
        {
            var centerTopLeft = VerticalCircleCusp(0, 0, centerOuter, -offset, 1);
            var centerTopRight = VerticalCircleCusp(0, 0, centerOuter, offset, 1);
            var centerRightTop = HorizontalCircleCusp(0, 0, centerOuter, offset, 1);
            var centerRightBottom = HorizontalCircleCusp(0, 0, centerOuter, -offset, 1);
            var centerBottomRight = VerticalCircleCusp(0, 0, centerOuter, offset, -1);
            var centerBottomLeft = VerticalCircleCusp(0, 0, centerOuter, -offset, -1);
            var centerLeftBottom = HorizontalCircleCusp(0, 0, centerOuter, -offset, -1);
            var centerLeftTop = HorizontalCircleCusp(0, 0, centerOuter, offset, -1);
            var topLeft = VerticalCircleCusp(0, armReach, endpointOuter, -offset, -1);
            var topRight = VerticalCircleCusp(0, armReach, endpointOuter, offset, -1);
            var rightTop = HorizontalCircleCusp(armReach, 0, endpointOuter, offset, -1);
            var rightBottom = HorizontalCircleCusp(armReach, 0, endpointOuter, -offset, -1);
            var bottomRight = VerticalCircleCusp(0, -armReach, endpointOuter, offset, 1);
            var bottomLeft = VerticalCircleCusp(0, -armReach, endpointOuter, -offset, 1);
            var leftBottom = HorizontalCircleCusp(-armReach, 0, endpointOuter, -offset, 1);
            var leftTop = HorizontalCircleCusp(-armReach, 0, endpointOuter, offset, 1);

            return Chain(
                ArcBetween((0, armReach), topLeft, CircleSite(0, armReach, -endpointOuter, 0)),
                Arc(0, armReach, -endpointOuter, armReach, 0, armReach + endpointOuter),
                Arc(0, armReach, 0, armReach + endpointOuter, endpointOuter, armReach),
                ArcBetween((0, armReach), CircleSite(0, armReach, endpointOuter, 0), topRight),
                SegmentBetween(topRight, centerTopRight),
                ArcBetween((0, 0), centerTopRight, centerRightTop),
                SegmentBetween(centerRightTop, rightTop),
                ArcBetween((armReach, 0), rightTop, CircleSite(armReach, 0, endpointOuter, 0)),
                ArcBetween((armReach, 0), CircleSite(armReach, 0, endpointOuter, 0), rightBottom),
                SegmentBetween(rightBottom, centerRightBottom),
                ArcBetween((0, 0), centerRightBottom, centerBottomRight),
                SegmentBetween(centerBottomRight, bottomRight),
                ArcBetween((0, -armReach), bottomRight, CircleSite(0, -armReach, endpointOuter, 0)),
                Arc(0, -armReach, endpointOuter, -armReach, 0, -armReach - endpointOuter),
                Arc(0, -armReach, 0, -armReach - endpointOuter, -endpointOuter, -armReach),
                ArcBetween((0, -armReach), CircleSite(0, -armReach, -endpointOuter, 0), bottomLeft),
                SegmentBetween(bottomLeft, centerBottomLeft),
                ArcBetween((0, 0), centerBottomLeft, centerLeftBottom),
                SegmentBetween(centerLeftBottom, leftBottom),
                ArcBetween((-armReach, 0), leftBottom, CircleSite(-armReach, 0, -endpointOuter, 0)),
                ArcBetween((-armReach, 0), CircleSite(-armReach, 0, -endpointOuter, 0), leftTop),
                SegmentBetween(leftTop, centerLeftTop),
                ArcBetween((0, 0), centerLeftTop, centerTopLeft),
                SegmentBetween(centerTopLeft, topLeft));
        }

        Relation ExteriorRoundedOutline()
        {
            const long cornerRadius = 6;
            var centerTopLeft = VerticalExternalFillet(0, 0, centerOuter, -offset, 1, cornerRadius);
            var centerTopRight = VerticalExternalFillet(0, 0, centerOuter, offset, 1, cornerRadius);
            var centerRightTop = HorizontalExternalFillet(0, 0, centerOuter, offset, 1, cornerRadius);
            var centerRightBottom = HorizontalExternalFillet(0, 0, centerOuter, -offset, 1, cornerRadius);
            var centerBottomRight = VerticalExternalFillet(0, 0, centerOuter, offset, -1, cornerRadius);
            var centerBottomLeft = VerticalExternalFillet(0, 0, centerOuter, -offset, -1, cornerRadius);
            var centerLeftBottom = HorizontalExternalFillet(0, 0, centerOuter, -offset, -1, cornerRadius);
            var centerLeftTop = HorizontalExternalFillet(0, 0, centerOuter, offset, -1, cornerRadius);
            var topLeft = VerticalExternalFillet(0, armReach, endpointOuter, -offset, -1, cornerRadius);
            var topRight = VerticalExternalFillet(0, armReach, endpointOuter, offset, -1, cornerRadius);
            var rightTop = HorizontalExternalFillet(armReach, 0, endpointOuter, offset, -1, cornerRadius);
            var rightBottom = HorizontalExternalFillet(armReach, 0, endpointOuter, -offset, -1, cornerRadius);
            var bottomRight = VerticalExternalFillet(0, -armReach, endpointOuter, offset, 1, cornerRadius);
            var bottomLeft = VerticalExternalFillet(0, -armReach, endpointOuter, -offset, 1, cornerRadius);
            var leftBottom = HorizontalExternalFillet(-armReach, 0, endpointOuter, -offset, 1, cornerRadius);
            var leftTop = HorizontalExternalFillet(-armReach, 0, endpointOuter, offset, 1, cornerRadius);

            return Chain(
                ArcBetween((0, armReach), topLeft.CircleTangent, CircleSite(0, armReach, -endpointOuter, 0)),
                Arc(0, armReach, -endpointOuter, armReach, 0, armReach + endpointOuter),
                Arc(0, armReach, 0, armReach + endpointOuter, endpointOuter, armReach),
                ArcBetween((0, armReach), CircleSite(0, armReach, endpointOuter, 0), topRight.CircleTangent),
                ArcBetween(topRight.Center, topRight.CircleTangent, topRight.LineTangent),
                SegmentBetween(topRight.LineTangent, centerTopRight.LineTangent),
                ArcBetween(centerTopRight.Center, centerTopRight.LineTangent, centerTopRight.CircleTangent),
                ArcBetween((0, 0), centerTopRight.CircleTangent, centerRightTop.CircleTangent),
                ArcBetween(centerRightTop.Center, centerRightTop.CircleTangent, centerRightTop.LineTangent),
                SegmentBetween(centerRightTop.LineTangent, rightTop.LineTangent),
                ArcBetween(rightTop.Center, rightTop.LineTangent, rightTop.CircleTangent),
                ArcBetween((armReach, 0), rightTop.CircleTangent, CircleSite(armReach, 0, endpointOuter, 0)),
                ArcBetween((armReach, 0), CircleSite(armReach, 0, endpointOuter, 0), rightBottom.CircleTangent),
                ArcBetween(rightBottom.Center, rightBottom.CircleTangent, rightBottom.LineTangent),
                SegmentBetween(rightBottom.LineTangent, centerRightBottom.LineTangent),
                ArcBetween(centerRightBottom.Center, centerRightBottom.LineTangent, centerRightBottom.CircleTangent),
                ArcBetween((0, 0), centerRightBottom.CircleTangent, centerBottomRight.CircleTangent),
                ArcBetween(centerBottomRight.Center, centerBottomRight.CircleTangent, centerBottomRight.LineTangent),
                SegmentBetween(centerBottomRight.LineTangent, bottomRight.LineTangent),
                ArcBetween(bottomRight.Center, bottomRight.LineTangent, bottomRight.CircleTangent),
                ArcBetween((0, -armReach), bottomRight.CircleTangent, CircleSite(0, -armReach, endpointOuter, 0)),
                Arc(0, -armReach, endpointOuter, -armReach, 0, -armReach - endpointOuter),
                Arc(0, -armReach, 0, -armReach - endpointOuter, -endpointOuter, -armReach),
                ArcBetween((0, -armReach), CircleSite(0, -armReach, -endpointOuter, 0), bottomLeft.CircleTangent),
                ArcBetween(bottomLeft.Center, bottomLeft.CircleTangent, bottomLeft.LineTangent),
                SegmentBetween(bottomLeft.LineTangent, centerBottomLeft.LineTangent),
                ArcBetween(centerBottomLeft.Center, centerBottomLeft.LineTangent, centerBottomLeft.CircleTangent),
                ArcBetween((0, 0), centerBottomLeft.CircleTangent, centerLeftBottom.CircleTangent),
                ArcBetween(centerLeftBottom.Center, centerLeftBottom.CircleTangent, centerLeftBottom.LineTangent),
                SegmentBetween(centerLeftBottom.LineTangent, leftBottom.LineTangent),
                ArcBetween(leftBottom.Center, leftBottom.LineTangent, leftBottom.CircleTangent),
                ArcBetween((-armReach, 0), leftBottom.CircleTangent, CircleSite(-armReach, 0, -endpointOuter, 0)),
                ArcBetween((-armReach, 0), CircleSite(-armReach, 0, -endpointOuter, 0), leftTop.CircleTangent),
                ArcBetween(leftTop.Center, leftTop.CircleTangent, leftTop.LineTangent),
                SegmentBetween(leftTop.LineTangent, centerLeftTop.LineTangent),
                ArcBetween(centerLeftTop.Center, centerLeftTop.LineTangent, centerLeftTop.CircleTangent),
                ArcBetween((0, 0), centerLeftTop.CircleTangent, centerTopLeft.CircleTangent),
                ArcBetween(centerTopLeft.Center, centerTopLeft.CircleTangent, centerTopLeft.LineTangent),
                SegmentBetween(centerTopLeft.LineTangent, topLeft.LineTangent),
                ArcBetween(topLeft.Center, topLeft.LineTangent, topLeft.CircleTangent));
        }

        Relation UnionInteriorHoleOutlines() =>
            Chain(
                Circle(armReach, 0, endpointInner),
                Circle(-armReach, 0, endpointInner),
                Circle(0, armReach, endpointInner),
                Circle(0, -armReach, endpointInner),
                CenterHoleOutline(1, 1),
                CenterHoleOutline(-1, 1),
                CenterHoleOutline(-1, -1),
                CenterHoleOutline(1, -1));

        Relation RoundedUnionInteriorHoleOutlines() =>
            Chain(
                Circle(armReach, 0, endpointInner),
                Circle(-armReach, 0, endpointInner),
                Circle(0, armReach, endpointInner),
                Circle(0, -armReach, endpointInner),
                RoundedCenterHoleOutline(1, 1),
                RoundedCenterHoleOutline(-1, 1),
                RoundedCenterHoleOutline(-1, -1),
                RoundedCenterHoleOutline(1, -1));

        var centerPlus = Chain(
            Segment(-armReach, 0, armReach, 0),
            Segment(0, -armReach, 0, armReach),
            Circle(0, 0, centerRadius));
        var endpointCircles = Chain(
            Circle(armReach, 0, endpointRadius),
            Circle(-armReach, 0, endpointRadius),
            Circle(0, armReach, endpointRadius),
            Circle(0, -armReach, endpointRadius));
        var endpointState = Chain(centerPlus, endpointCircles);
        var offsetBoundaries = OffsetBoundaryPairs();

        var centerOuterFill = DiskFill(0, 0, centerOuter);
        var centerInnerClear = AntiDiskFill(0, 0, centerInner);
        var centerRingFill = Chain(centerOuterFill, centerInnerClear);
        var barEnd = armReach - endpointInner - 2;
        var horizontalFill = PolygonFill((-barEnd, -offset), (barEnd, -offset), (barEnd, offset), (-barEnd, offset));
        var verticalFill = PolygonFill((-offset, -barEnd), (offset, -barEnd), (offset, barEnd), (-offset, barEnd));
        var rightEndpointFill = RingFill(armReach, 0, endpointOuter, endpointInner);
        var leftEndpointFill = RingFill(-armReach, 0, endpointOuter, endpointInner);
        var topEndpointFill = RingFill(0, armReach, endpointOuter, endpointInner);
        var bottomEndpointFill = RingFill(0, -armReach, endpointOuter, endpointInner);
        var centerHoleCutNE = CenterHoleAntiFill(1, 1);
        var centerHoleCutNW = CenterHoleAntiFill(-1, 1);
        var centerHoleCutSW = CenterHoleAntiFill(-1, -1);
        var centerHoleCutSE = CenterHoleAntiFill(1, -1);
        var roundedCenterHoleCutNE = RoundedCenterHoleAntiFill(1, 1);
        var roundedCenterHoleCutNW = RoundedCenterHoleAntiFill(-1, 1);
        var roundedCenterHoleCutSW = RoundedCenterHoleAntiFill(-1, -1);
        var roundedCenterHoleCutSE = RoundedCenterHoleAntiFill(1, -1);
        var separateFills = Chain(
            centerRingFill,
            horizontalFill,
            verticalFill,
            rightEndpointFill,
            leftEndpointFill,
            topEndpointFill,
            bottomEndpointFill);
        var compositeOutline = CompositeOutline();
        var unionInteriorHoles = UnionInteriorHoleOutlines();
        var roundedUnionInteriorHoles = RoundedUnionInteriorHoleOutlines();
        var exteriorSharpOutline = ExteriorSharpOutline();
        var exteriorRoundedOutline = ExteriorRoundedOutline();
        var exteriorRoundedOuterFill = GraphicStructure.AreaFill(exteriorRoundedOutline);

        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            centerRingFill,
            horizontalFill,
            verticalFill,
            rightEndpointFill,
            leftEndpointFill,
            topEndpointFill,
            bottomEndpointFill,
            centerHoleCutNE,
            centerHoleCutNW,
            centerHoleCutSW,
            centerHoleCutSE);
        var roundedAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            centerRingFill,
            horizontalFill,
            verticalFill,
            rightEndpointFill,
            leftEndpointFill,
            topEndpointFill,
            bottomEndpointFill,
            roundedCenterHoleCutNE,
            roundedCenterHoleCutNW,
            roundedCenterHoleCutSW,
            roundedCenterHoleCutSE);
        var exteriorRoundedAreaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            exteriorRoundedOuterFill,
            AntiDiskFill(armReach, 0, endpointInner),
            AntiDiskFill(-armReach, 0, endpointInner),
            AntiDiskFill(0, armReach, endpointInner),
            AntiDiskFill(0, -armReach, endpointInner),
            roundedCenterHoleCutNE,
            roundedCenterHoleCutNW,
            roundedCenterHoleCutSW,
            roundedCenterHoleCutSE);
        var offsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), endpointState, Distance(offset)),
            offsetBoundaries);
        var fillEvidence = GraphicParameterSurface.Frame(new Carrier(), offsetEvidence, areaFrame);
        var fillUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, unionInteriorHoles);
        var cuspPattern = GraphicBoundaryConceptSurface.CuspWitnessPattern(new Carrier(), new Placeholder());
        var interiorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), unionInteriorHoles);
        var exteriorCuspDomain = GraphicBoundaryConceptSurface.CuspCandidateDomain(new Carrier(), exteriorSharpOutline);
        var cuspSelection = GraphicSelectionSurface.Search(
            new Carrier(),
            fillUnion,
            GraphicSelectionSurface.Frame(new Carrier(), fillUnion, interiorCuspDomain),
            cuspPattern);
        var roundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), roundedAreaFrame, roundedUnionInteriorHoles);
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
            endpointCircles,
            UnknownPoint(),
            centerPlus,
            endpointCircles,
            endpointState,
            GraphicParameterSurface.Frame(new Carrier(), centerPlus, endpointCircles));
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
            fillUnion);
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
            GraphicParameterSurface.Frame(new Carrier(), cuspSelection, roundedUnion));
        var exteriorCuspStep = RefinedRung(
            roundedUnion,
            cuspPattern,
            exteriorCuspDomain,
            roundedUnion,
            exteriorCuspSelection,
            exteriorCuspSelection,
            GraphicParameterSurface.Frame(new Carrier(), roundedUnion, exteriorCuspDomain, exteriorCuspSelection));
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
