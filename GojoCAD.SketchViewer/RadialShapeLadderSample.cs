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

        Relation MarkerCircle(long centerX, long centerY) =>
            Circle(centerX, centerY, 5);

        Relation CenterHoleCuspMarkers(long signX, long signY)
        {
            var circleSide = (long)Math.Round(Math.Sqrt(centerInner * centerInner - offset * offset));
            return Chain(
                MarkerCircle(signX * offset, signY * offset),
                MarkerCircle(signX * offset, signY * circleSide),
                MarkerCircle(signX * circleSide, signY * offset));
        }

        Relation InteriorCuspMarkers() =>
            Chain(
                CenterHoleCuspMarkers(1, 1),
                CenterHoleCuspMarkers(-1, 1),
                CenterHoleCuspMarkers(-1, -1),
                CenterHoleCuspMarkers(1, -1));

        (long X, long Y) PointOnCenterCircle(double degrees)
        {
            var radians = Math.PI * degrees / 180d;
            return (
                (long)Math.Round(Math.Cos(radians) * centerInner),
                (long)Math.Round(Math.Sin(radians) * centerInner));
        }

        double AngleDegrees(long pointX, long pointY) =>
            Math.Atan2(pointY, pointX) * 180d / Math.PI;

        double ShortestSweep(double startDegrees, double endDegrees)
        {
            var sweep = endDegrees - startDegrees;
            while (sweep > 180d)
            {
                sweep -= 360d;
            }

            while (sweep < -180d)
            {
                sweep += 360d;
            }

            return sweep;
        }

        Relation RoundedCenterHoleOutline(long signX, long signY)
        {
            const long cornerRadius = 7;
            const double circleTrimDegrees = 8d;
            var circleSide = (long)Math.Round(Math.Sqrt(centerInner * centerInner - offset * offset));
            var verticalStart = (X: signX * offset, Y: signY * (offset + cornerRadius));
            var verticalEnd = (X: signX * offset, Y: signY * (circleSide - cornerRadius));
            var horizontalStart = (X: signX * (circleSide - cornerRadius), Y: signY * offset);
            var horizontalEnd = (X: signX * (offset + cornerRadius), Y: signY * offset);
            var startAngle = AngleDegrees(signX * offset, signY * circleSide);
            var endAngle = AngleDegrees(signX * circleSide, signY * offset);
            var sweepSign = Math.Sign(ShortestSweep(startAngle, endAngle));
            var arcStart = PointOnCenterCircle(startAngle + sweepSign * circleTrimDegrees);
            var arcEnd = PointOnCenterCircle(endAngle - sweepSign * circleTrimDegrees);

            return Chain(
                Segment(verticalStart.X, verticalStart.Y, verticalEnd.X, verticalEnd.Y),
                Arc(
                    signX * (offset + cornerRadius),
                    signY * (circleSide - cornerRadius),
                    verticalEnd.X,
                    verticalEnd.Y,
                    arcStart.X,
                    arcStart.Y),
                Arc(0, 0, arcStart.X, arcStart.Y, arcEnd.X, arcEnd.Y),
                Arc(
                    signX * (circleSide - cornerRadius),
                    signY * (offset + cornerRadius),
                    arcEnd.X,
                    arcEnd.Y,
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
        var cuspMarkers = InteriorCuspMarkers();
        var roundedUnionInteriorHoles = RoundedUnionInteriorHoleOutlines();

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
        var offsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            GraphicOffsetSurface.RequestWithDistance(new Carrier(), endpointState, Distance(offset)),
            offsetBoundaries);
        var fillEvidence = GraphicParameterSurface.Frame(new Carrier(), offsetEvidence, areaFrame);
        var fillUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), areaFrame, unionInteriorHoles);
        var cuspSelection = GraphicParameterSurface.Frame(new Carrier(), fillUnion, cuspMarkers);
        var roundedUnion = GraphicBooleanSurface.AreaUnion(new Carrier(), roundedAreaFrame, roundedUnionInteriorHoles);

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
            cuspMarkers,
            unionInteriorHoles,
            fillUnion,
            cuspMarkers,
            cuspSelection,
            GraphicParameterSurface.Frame(new Carrier(), fillUnion, cuspMarkers, cuspSelection));
        var roundedStep = RefinedRung(
            cuspSelection,
            cuspMarkers,
            fillUnion,
            UnknownPoint(),
            roundedUnion,
            roundedUnion,
            GraphicParameterSurface.Frame(new Carrier(), cuspSelection, cuspMarkers, roundedUnion));

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
        };
        var applications = new[]
        {
            endpointStep.Application,
            offsetStep.Application,
            fillStep.Application,
            mergeStep.Application,
            outlineStep.Application,
            cuspStep.Application,
            roundedStep.Application
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
                "Interior cusp selection",
                "Rounded interior corners"
            ]);
    }
}
