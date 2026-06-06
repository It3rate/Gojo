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

        Relation RoundedArm(long centerX, long centerY)
        {
            if (Math.Abs(centerX) > 0)
            {
                var sign = Math.Sign(centerX);
                var shoulderX = sign * centerOuter;
                var neckX = sign * armReach;
                return Chain(
                    Arc(0, 0, shoulderX, offset, sign * (centerOuter - 4), sign * 24),
                    Segment(shoulderX, offset, neckX, offset),
                    Arc(neckX, 0, neckX, offset, centerX + sign * endpointOuter, centerY),
                    Arc(neckX, 0, centerX + sign * endpointOuter, centerY, neckX, -offset),
                    Segment(neckX, -offset, shoulderX, -offset),
                    Arc(0, 0, sign * (centerOuter - 4), -sign * 24, shoulderX, -offset),
                    Circle(centerX, centerY, endpointInner));
            }

            var verticalSign = Math.Sign(centerY);
            var shoulderY = verticalSign * centerOuter;
            var neckY = verticalSign * armReach;
            return Chain(
                Arc(0, 0, -offset, shoulderY, -verticalSign * 24, verticalSign * (centerOuter - 4)),
                Segment(-offset, shoulderY, -offset, neckY),
                Arc(0, neckY, -offset, neckY, centerX, centerY + verticalSign * endpointOuter),
                Arc(0, neckY, centerX, centerY + verticalSign * endpointOuter, offset, neckY),
                Segment(offset, neckY, offset, shoulderY),
                Arc(0, 0, verticalSign * 24, verticalSign * (centerOuter - 4), offset, shoulderY),
                Circle(centerX, centerY, endpointInner));
        }

        Relation RoundedQuadrantWindow(long signX, long signY)
        {
            var outerX = signX * 61;
            var outerY = signY * 61;
            var innerX = signX * 30;
            var innerY = signY * 30;
            var barX = signX * 15;
            var barY = signY * 15;

            return Chain(
                Arc(0, 0, outerX, signY * 13, signX * 13, outerY),
                Segment(signX * 13, outerY, barX, innerY),
                Arc(innerX, innerY, barX, innerY, innerX, barY),
                Segment(innerX, barY, outerX, signY * 13));
        }

        Relation RoundedCompositeOutline() =>
            Chain(
                Circle(0, 0, centerOuter),
                Circle(0, 0, centerInner),
                RoundedArm(armReach, 0),
                RoundedArm(-armReach, 0),
                RoundedArm(0, armReach),
                RoundedArm(0, -armReach),
                RoundedQuadrantWindow(1, 1),
                RoundedQuadrantWindow(-1, 1),
                RoundedQuadrantWindow(-1, -1),
                RoundedQuadrantWindow(1, -1));

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
        var barEnd = armReach - endpointInner;
        var horizontalFill = PolygonFill((-barEnd, -offset), (barEnd, -offset), (barEnd, offset), (-barEnd, offset));
        var verticalFill = PolygonFill((-offset, -barEnd), (offset, -barEnd), (offset, barEnd), (-offset, barEnd));
        var rightEndpointFill = RingFill(armReach, 0, endpointOuter, endpointInner);
        var leftEndpointFill = RingFill(-armReach, 0, endpointOuter, endpointInner);
        var topEndpointFill = RingFill(0, armReach, endpointOuter, endpointInner);
        var bottomEndpointFill = RingFill(0, -armReach, endpointOuter, endpointInner);
        var separateFills = Chain(
            centerRingFill,
            horizontalFill,
            verticalFill,
            rightEndpointFill,
            leftEndpointFill,
            topEndpointFill,
            bottomEndpointFill);
        var compositeOutline = CompositeOutline();
        var roundedOffset = RoundedCompositeOutline();
        var ornaments = Chain(
            Circle(0, 42, 6),
            Circle(42, 0, 6),
            Circle(0, -42, 6),
            Circle(-42, 0, 6));
        var finalShape = Chain(roundedOffset, ornaments);

        var areaFrame = GraphicBooleanSurface.AreaFrame(
            new Carrier(),
            centerRingFill,
            horizontalFill,
            verticalFill,
            rightEndpointFill,
            leftEndpointFill,
            topEndpointFill,
            bottomEndpointFill);
        var offsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), endpointState, Distance(offset)),
            offsetBoundaries);
        var fillEvidence = GraphicParameterSurface.Frame(new Carrier(), offsetEvidence, areaFrame);
        var fillUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, compositeOutline);
        var roundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, roundedOffset);
        var finalUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, Chain(roundedOffset, ornaments));

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
        var roundedStep = RefinedRung(
            fillUnion,
            fillUnion,
            fillUnion,
            UnknownPoint(),
            roundedOffset,
            roundedOffset,
            roundedUnion);
        var ornamentStep = RefinedRung(
            roundedOffset,
            ornaments,
            UnknownPoint(),
            roundedOffset,
            ornaments,
            finalUnion,
            GraphicParameterSurface.Frame(new Carrier(), roundedOffset, ornaments, finalShape, finalUnion));

        var rungs = new[]
        {
            baseRung,
            endpointStep.Rung,
            offsetStep.Rung,
            fillStep.Rung,
            mergeStep.Rung,
            outlineStep.Rung,
            roundedStep.Rung,
            ornamentStep.Rung
        };
        var applications = new[]
        {
            endpointStep.Application,
            offsetStep.Application,
            fillStep.Application,
            mergeStep.Application,
            outlineStep.Application,
            roundedStep.Application,
            ornamentStep.Application
        };
        var ladder = TimelineLadder(rungs, applications);

        return new SketchScene(
            "Radial Shape Ladder",
            "Centerline plus and circle evolve through endpoint rings, offset fills, union evidence, a composite outline, smoothing, and small ornaments.",
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
                "Curved corners",
                "Tiny circle ornaments"
            ]);
    }
}
