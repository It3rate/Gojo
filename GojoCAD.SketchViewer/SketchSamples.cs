using Gojo.Core;
using Gojo.Structures;
using GojoCAD.Structures;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static class SketchSamples
{
    public static IReadOnlyList<SketchScene> CreateInitialScenes()
    {
        var seed = Random.Shared.Next();
        return
        [
            CreateComplexHouseSketch(seed)
        ];
    }

    private static SketchScene CreateComplexHouseSketch(int seed)
    {
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();

        Relation P(long xTicks, long yTicks) =>
            Point(frame, x, y, xTicks, yTicks);

        long Lerp(long start, long end, long ticks, long scale = 100) =>
            start + ((end - start) * ticks / scale);

        var parameterScales = new long[] { 1, 8, 40 };

        long CoarseTicks(long refinedTicks) =>
            GraphicParameterSurface.CoarseTicks(refinedTicks, parameterScales);

        Relation ParameterValue(
            IElement basis,
            long refinedTicks,
            long scaleTicks,
            IElement coarseResult,
            IElement refinedResult) =>
            GraphicParameterSurface.NormalizedValue(
                new Carrier(),
                basis,
                refinedTicks,
                scaleTicks,
                parameterScales,
                coarseResult,
                refinedResult,
                UnknownPoint());

        (Relation Point, long X, long Y) Site(long xTicks, long yTicks) =>
            (P(xTicks, yTicks), xTicks, yTicks);

        (
            Relation Start,
            Relation End,
            Relation Span,
            long StartX,
            long StartY,
            long EndX,
            long EndY) BasisSpan(long startX, long startY, long endX, long endY)
        {
            var start = Site(startX, startY);
            var end = Site(endX, endY);
            return (
                start.Point,
                end.Point,
                GraphicStructure.FrameSegment(start.Point, end.Point),
                start.X,
                start.Y,
                end.X,
                end.Y);
        }

        (Relation Point, long X, long Y) PointOnSpan(
            (
                Relation Start,
                Relation End,
                Relation Span,
                long StartX,
                long StartY,
                long EndX,
                long EndY) span,
            long ticks,
            long scale = 100)
        {
            var xTicks = Lerp(span.StartX, span.EndX, ticks, scale);
            var yTicks = Lerp(span.StartY, span.EndY, ticks, scale);
            return Site(xTicks, yTicks);
        }

        (Relation Point, long X, long Y) PointFromAxes(
            (
                Relation Start,
                Relation End,
                Relation Span,
                long StartX,
                long StartY,
                long EndX,
                long EndY) xBasis,
            long xTicks,
            (
                Relation Start,
                Relation End,
                Relation Span,
                long StartX,
                long StartY,
                long EndX,
                long EndY) yBasis,
            long yTicks,
            long scale = 100)
        {
            var xPoint = PointOnSpan(xBasis, xTicks, scale);
            var yPoint = PointOnSpan(yBasis, yTicks, scale);
            return Site(xPoint.X, yPoint.Y);
        }

        Relation Oval(long centerX, long centerY, long radiusX, long radiusY)
        {
            var left = P(centerX - radiusX, centerY);
            var top = P(centerX, centerY + radiusY);
            var right = P(centerX + radiusX, centerY);
            var bottom = P(centerX, centerY - radiusY);
            var controlX = Math.Max(2, radiusX / 2);
            var controlY = Math.Max(2, radiusY / 2);
            return Chain(
                GraphicStructure.FrameCubicCurve(left, P(centerX - radiusX, centerY + controlY), P(centerX - controlX, centerY + radiusY), top),
                GraphicStructure.FrameCubicCurve(top, P(centerX + controlX, centerY + radiusY), P(centerX + radiusX, centerY + controlY), right),
                GraphicStructure.FrameCubicCurve(right, P(centerX + radiusX, centerY - controlY), P(centerX + controlX, centerY - radiusY), bottom),
                GraphicStructure.FrameCubicCurve(bottom, P(centerX - controlX, centerY - radiusY), P(centerX - radiusX, centerY - controlY), left));
        }

        Relation Circle(long centerX, long centerY, long radius)
        {
            var center = P(centerX, centerY);
            var right = P(centerX + radius, centerY);
            var top = P(centerX, centerY + radius);
            var left = P(centerX - radius, centerY);
            var bottom = P(centerX, centerY - radius);
            return Chain(
                GraphicStructure.FrameArc(center, right, top),
                GraphicStructure.FrameArc(center, top, left),
                GraphicStructure.FrameArc(center, left, bottom),
                GraphicStructure.FrameArc(center, bottom, right));
        }

        Relation AreaPolygon(params Relation[] points)
        {
            if (points.Length < 3)
            {
                return UnknownPoint();
            }

            var edges = new IElement[points.Length];
            for (var index = 0; index < points.Length; index++)
            {
                edges[index] = GraphicStructure.FrameSegment(points[index], points[(index + 1) % points.Length]);
            }

            return GraphicStructure.AreaFill(edges);
        }

        Relation FilledCircle(long centerX, long centerY, long radius)
        {
            return Chain(
                AreaPolygon(
                    P(centerX + radius, centerY),
                    P(centerX + radius * 7 / 10, centerY + radius * 7 / 10),
                    P(centerX, centerY + radius),
                    P(centerX - radius * 7 / 10, centerY + radius * 7 / 10),
                    P(centerX - radius, centerY),
                    P(centerX - radius * 7 / 10, centerY - radius * 7 / 10),
                    P(centerX, centerY - radius),
                    P(centerX + radius * 7 / 10, centerY - radius * 7 / 10)),
                Circle(centerX, centerY, radius));
        }

        Relation Cloudlet(long anchorX, long anchorY)
        {
            var left = P(anchorX, anchorY);
            var leftPeak = P(anchorX + 10, anchorY + 12);
            var leftValley = P(anchorX + 19, anchorY + 5);
            var middlePeak = P(anchorX + 30, anchorY + 16);
            var middleValley = P(anchorX + 40, anchorY + 6);
            var rightPeak = P(anchorX + 49, anchorY + 11);
            var right = P(anchorX + 54, anchorY - 1);
            var lowerRight = P(anchorX + 42, anchorY - 9);
            var lowerLeft = P(anchorX + 16, anchorY - 8);
            return Chain(
                GraphicStructure.FrameCubicCurve(left, P(anchorX, anchorY + 7), P(anchorX + 7, anchorY + 12), leftPeak),
                GraphicStructure.FrameCubicCurve(leftPeak, P(anchorX + 13, anchorY + 12), P(anchorX + 18, anchorY + 9), leftValley),
                GraphicStructure.FrameCubicCurve(leftValley, P(anchorX + 21, anchorY + 12), P(anchorX + 26, anchorY + 16), middlePeak),
                GraphicStructure.FrameCubicCurve(middlePeak, P(anchorX + 34, anchorY + 16), P(anchorX + 38, anchorY + 11), middleValley),
                GraphicStructure.FrameCubicCurve(middleValley, P(anchorX + 43, anchorY + 11), P(anchorX + 45, anchorY + 11), rightPeak),
                GraphicStructure.FrameCubicCurve(rightPeak, P(anchorX + 52, anchorY + 11), P(anchorX + 54, anchorY + 5), right),
                GraphicStructure.FrameCubicCurve(right, P(anchorX + 54, anchorY - 6), P(anchorX + 48, anchorY - 9), lowerRight),
                GraphicStructure.FrameCubicCurve(lowerRight, P(anchorX + 36, anchorY - 9), P(anchorX + 22, anchorY - 8), lowerLeft),
                GraphicStructure.FrameCubicCurve(lowerLeft, P(anchorX + 6, anchorY - 8), P(anchorX, anchorY - 7), left));
        }

        Relation FilledLeafBlob(long centerX, long centerY, long radiusX, long radiusY)
        {
            return AreaPolygon(
                P(centerX - radiusX, centerY),
                P(centerX - radiusX * 5 / 6, centerY + radiusY / 2),
                P(centerX - radiusX / 2, centerY + radiusY),
                P(centerX, centerY + radiusY + 2),
                P(centerX + radiusX / 2, centerY + radiusY),
                P(centerX + radiusX, centerY + radiusY / 3),
                P(centerX + radiusX, centerY - radiusY / 3),
                P(centerX + radiusX / 2, centerY - radiusY),
                P(centerX, centerY - radiusY - 1),
                P(centerX - radiusX * 2 / 3, centerY - radiusY * 2 / 3));
        }

        Relation RectOutline(Relation lowerLeftPoint, Relation lowerRightPoint, Relation upperRightPoint, Relation upperLeftPoint) =>
            Chain(
                GraphicStructure.FrameSegment(lowerLeftPoint, lowerRightPoint),
                GraphicStructure.FrameSegment(lowerRightPoint, upperRightPoint),
                GraphicStructure.FrameSegment(upperRightPoint, upperLeftPoint),
                GraphicStructure.FrameSegment(upperLeftPoint, lowerLeftPoint));

        var horizonBasis = BasisSpan(-148, -30, 134, -30);
        var horizon = horizonBasis.Span;
        var houseBottomBasis = BasisSpan(-52, -30, 52, -30);
        var houseLeftBasis = BasisSpan(-52, -30, -52, 8);
        var houseRightBasis = BasisSpan(52, -30, 52, 8);
        var lowerLeft = houseBottomBasis.Start;
        var lowerRight = houseBottomBasis.End;
        var upperLeft = houseLeftBasis.End;
        var upperRight = houseRightBasis.End;
        var roofApexSite = PointOnSpan(BasisSpan(-52, 8, 52, 8), 50);
        var roofApex = P(roofApexSite.X, roofApexSite.Y + 32);
        const long roofExtensionTicks = 13;
        const long roofLiftTicks = 8;
        var roofOverhangLeft = P(-52 - roofExtensionTicks, 8);
        var roofOverhangApex = P(roofApexSite.X, roofApexSite.Y + 32 + roofLiftTicks);
        var roofOverhangRight = P(52 + roofExtensionTicks, 8);
        var roofRightOverhangBasis = BasisSpan(
            roofApexSite.X,
            roofApexSite.Y + 32 + roofLiftTicks,
            52 + roofExtensionTicks,
            8);
        var houseBoxTop = GraphicStructure.FrameSegment(upperRight, upperLeft);
        var houseBox = Chain(
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            houseBoxTop,
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));
        var houseRoof = Chain(
            GraphicStructure.FrameSegment(lowerLeft, lowerRight),
            GraphicStructure.FrameSegment(lowerRight, upperRight),
            GraphicStructure.FrameSegment(upperRight, roofApex),
            GraphicStructure.FrameSegment(roofApex, upperLeft),
            GraphicStructure.FrameSegment(upperLeft, lowerLeft));
        var roofOverhang = Chain(
            GraphicStructure.FrameSegment(roofOverhangLeft, roofOverhangApex),
            GraphicStructure.FrameSegment(roofOverhangApex, roofOverhangRight));
        var houseWithOverhang = Chain(houseRoof, roofOverhang);
        var roofExtensionSlot = new Placeholder();
        var roofLiftSlot = new Placeholder();
        var roofOverhangTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            roofExtensionSlot,
            roofLiftSlot);
        var roofOverhangEvidence = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            roofOverhangTemplate,
            roofOverhang,
            GraphicParameterSurface.Frame(
                new Carrier(),
                ParameterValue(houseBoxTop, roofExtensionTicks, 100, houseBoxTop, roofOverhang),
                ParameterValue(
                    BasisSpan(roofApexSite.X, roofApexSite.Y + 32, roofApexSite.X, roofApexSite.Y + 82).Span,
                    roofLiftTicks,
                    100,
                    houseRoof,
                    roofOverhang)),
            FocusSurface.Bind(roofExtensionSlot, Relation.Point(new Carrier(), roofExtensionTicks)),
            FocusSurface.Bind(roofLiftSlot, Relation.Point(new Carrier(), roofLiftTicks)));

        var wallXBasis = houseBottomBasis.Span;
        var wallYBasis = houseLeftBasis.Span;
        var doorTemplateCenterSlot = new Placeholder();
        var doorTemplateWidthSlot = new Placeholder();
        var doorTemplateHeightSlot = new Placeholder();
        var doorTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            doorTemplateCenterSlot,
            doorTemplateWidthSlot,
            doorTemplateHeightSlot);
        const long doorCenterXTicks = 50;
        const long doorCenterYTicks = 29;
        var doorCenterSite = PointFromAxes(houseBottomBasis, doorCenterXTicks, houseLeftBasis, doorCenterYTicks);
        var doorCoarseXSite = PointFromAxes(houseBottomBasis, CoarseTicks(doorCenterXTicks), houseLeftBasis, doorCenterYTicks);
        var doorCoarseYSite = PointFromAxes(houseBottomBasis, doorCenterXTicks, houseLeftBasis, CoarseTicks(doorCenterYTicks));
        var doorCenter = doorCenterSite.Point;
        var doorCoarseXPoint = doorCoarseXSite.Point;
        var doorCoarseYPoint = doorCoarseYSite.Point;
        var doorWidth = Relation.Point(new Carrier(), 28);
        var doorCoarseWidth = Relation.Point(new Carrier(), 20);
        var doorHeight = Relation.Point(new Carrier(), 22);
        var doorCoarseHeight = Relation.Point(new Carrier(), 15);
        var doorEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(wallXBasis, doorCenterXTicks, 100, doorCoarseXPoint, doorCenter),
            ParameterValue(wallYBasis, doorCenterYTicks, 100, doorCoarseYPoint, doorCenter),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 47, 100, doorCoarseWidth, doorWidth),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 58, 100, doorCoarseHeight, doorHeight));
        var doorLeftBottom = P(-14, -30);
        var doorRightBottom = P(14, -30);
        var doorRightTop = P(14, -8);
        var doorLeftTop = P(-14, -8);
        var doorRectangle = RectOutline(doorLeftBottom, doorRightBottom, doorRightTop, doorLeftTop);
        var doorRectangleInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            doorTemplate,
            doorRectangle,
            doorEvidence,
            FocusSurface.Bind(doorTemplateCenterSlot, doorCenter),
            FocusSurface.Bind(doorTemplateWidthSlot, doorWidth),
            FocusSurface.Bind(doorTemplateHeightSlot, doorHeight));
        var archedDoor = Chain(
            GraphicStructure.FrameSegment(doorLeftBottom, doorLeftTop),
            GraphicStructure.FrameCubicCurve(doorLeftTop, P(-14, 8), P(14, 8), doorRightTop),
            GraphicStructure.FrameSegment(doorRightTop, doorRightBottom),
            GraphicStructure.FrameSegment(doorRightBottom, doorLeftBottom));
        var archedDoorInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            doorTemplate,
            archedDoor,
            doorEvidence,
            FocusSurface.Bind(doorTemplateCenterSlot, doorCenter),
            FocusSurface.Bind(doorTemplateWidthSlot, doorWidth),
            FocusSurface.Bind(doorTemplateHeightSlot, doorHeight));

        var windowTemplateCenterSlot = new Placeholder();
        var windowTemplateWidthSlot = new Placeholder();
        var windowTemplateHeightSlot = new Placeholder();
        var windowTemplateCornerSlot = new Placeholder();
        var windowTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            windowTemplateCenterSlot,
            windowTemplateWidthSlot,
            windowTemplateHeightSlot,
            windowTemplateCornerSlot);
        var windowLowerLeft = P(22, -14);
        var windowLowerRight = P(42, -14);
        var windowUpperRight = P(42, 6);
        var windowUpperLeft = P(22, 6);
        const long windowCenterXTicks = 81;
        const long windowCenterYTicks = 68;
        var windowCenterSite = PointFromAxes(houseBottomBasis, windowCenterXTicks, houseLeftBasis, windowCenterYTicks);
        var windowCoarseXSite = PointFromAxes(houseBottomBasis, CoarseTicks(windowCenterXTicks), houseLeftBasis, windowCenterYTicks);
        var windowCoarseYSite = PointFromAxes(houseBottomBasis, windowCenterXTicks, houseLeftBasis, CoarseTicks(windowCenterYTicks));
        var windowCenter = windowCenterSite.Point;
        var windowCoarseXPoint = windowCoarseXSite.Point;
        var windowCoarseYPoint = windowCoarseYSite.Point;
        var windowWidth = Relation.Point(new Carrier(), 20);
        var windowCoarseWidth = Relation.Point(new Carrier(), 16);
        var windowHeight = Relation.Point(new Carrier(), 20);
        var windowCoarseHeight = Relation.Point(new Carrier(), 16);
        var windowCornerRadius = Relation.Point(new Carrier(), 4);
        var windowCoarseCornerRadius = Relation.Point(new Carrier(), 0);
        var windowEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(wallXBasis, windowCenterXTicks, 100, windowCoarseXPoint, windowCenter),
            ParameterValue(wallYBasis, windowCenterYTicks, 100, windowCoarseYPoint, windowCenter),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 35, 100, windowCoarseWidth, windowWidth),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 53, 100, windowCoarseHeight, windowHeight),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 32, 100, windowCoarseCornerRadius, windowCornerRadius));
        var windowRectangle = RectOutline(windowLowerLeft, windowLowerRight, windowUpperRight, windowUpperLeft);
        var windowRectangleInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            windowTemplate,
            windowRectangle,
            windowEvidence,
            FocusSurface.Bind(windowTemplateCenterSlot, windowCenter),
            FocusSurface.Bind(windowTemplateWidthSlot, windowWidth),
            FocusSurface.Bind(windowTemplateHeightSlot, windowHeight),
            FocusSurface.Bind(windowTemplateCornerSlot, windowCoarseCornerRadius));
        var windowCross = Chain(
            GraphicStructure.FrameSegment(P(22, -4), windowCenter),
            GraphicStructure.FrameSegment(windowCenter, P(42, -4)),
            GraphicStructure.FrameSegment(P(32, 6), windowCenter),
            GraphicStructure.FrameSegment(windowCenter, P(32, -14)));
        var windowPlus = Chain(windowRectangle, windowCross);
        var windowPlusInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            windowTemplate,
            windowPlus,
            windowEvidence,
            FocusSurface.Bind(windowTemplateCenterSlot, windowCenter),
            FocusSurface.Bind(windowTemplateWidthSlot, windowWidth),
            FocusSurface.Bind(windowTemplateHeightSlot, windowHeight),
            FocusSurface.Bind(windowTemplateCornerSlot, windowCoarseCornerRadius));
        var roundedWindow = Chain(
            GraphicStructure.FrameSegment(P(26, 6), P(38, 6)),
            GraphicStructure.FrameCubicCurve(P(38, 6), P(40, 6), P(42, 4), P(42, 2)),
            GraphicStructure.FrameSegment(P(42, 2), P(42, -10)),
            GraphicStructure.FrameCubicCurve(P(42, -10), P(42, -12), P(40, -14), P(38, -14)),
            GraphicStructure.FrameSegment(P(38, -14), P(26, -14)),
            GraphicStructure.FrameCubicCurve(P(26, -14), P(24, -14), P(22, -12), P(22, -10)),
            GraphicStructure.FrameSegment(P(22, -10), P(22, 2)),
            GraphicStructure.FrameCubicCurve(P(22, 2), P(22, 4), P(24, 6), P(26, 6)));
        var roundedWindowPlus = Chain(roundedWindow, windowCross);
        var roundedWindowInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            windowTemplate,
            roundedWindowPlus,
            windowEvidence,
            FocusSurface.Bind(windowTemplateCenterSlot, windowCenter),
            FocusSurface.Bind(windowTemplateWidthSlot, windowWidth),
            FocusSurface.Bind(windowTemplateHeightSlot, windowHeight),
            FocusSurface.Bind(windowTemplateCornerSlot, windowCornerRadius));

        const long chimneyBaseLeftTicks = 22;
        const long chimneyBaseRightTicks = 40;
        const long chimneyTopY = 51;
        var chimneyBaseLeftSite = PointOnSpan(roofRightOverhangBasis, chimneyBaseLeftTicks);
        var chimneyBaseRightSite = PointOnSpan(roofRightOverhangBasis, chimneyBaseRightTicks);
        var chimneyBaseLeftCoarseSite = PointOnSpan(roofRightOverhangBasis, CoarseTicks(chimneyBaseLeftTicks));
        var chimneyBaseRightCoarseSite = PointOnSpan(roofRightOverhangBasis, CoarseTicks(chimneyBaseRightTicks));
        var chimneyLowerLeft = chimneyBaseLeftSite.Point;
        var chimneyLowerRight = chimneyBaseRightSite.Point;
        var chimneyUpperRight = P(chimneyBaseRightSite.X, chimneyTopY);
        var chimneyUpperLeft = P(chimneyBaseLeftSite.X, chimneyTopY);
        var chimneyBody = Chain(
            GraphicStructure.FrameSegment(chimneyLowerLeft, chimneyLowerRight),
            GraphicStructure.FrameSegment(chimneyLowerRight, chimneyUpperRight),
            GraphicStructure.FrameSegment(chimneyUpperRight, chimneyUpperLeft),
            GraphicStructure.FrameSegment(chimneyUpperLeft, chimneyLowerLeft));
        var chimneyWithCap = Chain(
            chimneyBody,
            GraphicStructure.FrameSegment(P(10, 51), P(30, 51)),
            GraphicStructure.FrameSegment(P(30, 51), P(20, 58)),
            GraphicStructure.FrameSegment(P(20, 58), P(10, 51)));
        var chimneyCenterSlot = new Placeholder();
        var chimneyWidthSlot = new Placeholder();
        var chimneyHeightSlot = new Placeholder();
        var chimneyCapRiseSlot = new Placeholder();
        var chimneyTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            chimneyCenterSlot,
            chimneyWidthSlot,
            chimneyHeightSlot,
            chimneyCapRiseSlot);
        const long chimneyCenterTicks = 31;
        var chimneyCenterOnRoof = PointOnSpan(roofRightOverhangBasis, chimneyCenterTicks);
        var chimneyCoarseCenterOnRoof = PointOnSpan(roofRightOverhangBasis, CoarseTicks(chimneyCenterTicks));
        var chimneyCenter = P(chimneyCenterOnRoof.X, 42);
        var chimneyCoarseCenter = P(chimneyCoarseCenterOnRoof.X, chimneyCoarseCenterOnRoof.Y);
        var chimneyWidth = Relation.Point(new Carrier(), 12);
        var chimneyCoarseWidth = Relation.Point(new Carrier(), 8);
        var chimneyHeight = Relation.Point(new Carrier(), 19);
        var chimneyCoarseHeight = Relation.Point(new Carrier(), 12);
        var chimneyCapRise = Relation.Point(new Carrier(), 7);
        var chimneyNoCapRise = Relation.Point(new Carrier(), 0);
        var chimneyEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(roofRightOverhangBasis.Span, chimneyCenterTicks, 100, chimneyCoarseCenter, chimneyCenter),
            ParameterValue(roofRightOverhangBasis.Span, chimneyBaseLeftTicks, 100, chimneyBaseLeftCoarseSite.Point, chimneyLowerLeft),
            ParameterValue(roofRightOverhangBasis.Span, chimneyBaseRightTicks, 100, chimneyBaseRightCoarseSite.Point, chimneyLowerRight),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 36, 100, chimneyCoarseWidth, chimneyWidth),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 48, 100, chimneyCoarseHeight, chimneyHeight),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 28, 100, chimneyNoCapRise, chimneyCapRise));
        var chimneyBodyInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            chimneyTemplate,
            chimneyBody,
            chimneyEvidence,
            FocusSurface.Bind(chimneyCenterSlot, chimneyCenter),
            FocusSurface.Bind(chimneyWidthSlot, chimneyWidth),
            FocusSurface.Bind(chimneyHeightSlot, chimneyHeight),
            FocusSurface.Bind(chimneyCapRiseSlot, chimneyNoCapRise));
        var chimneyCapInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            chimneyTemplate,
            chimneyWithCap,
            chimneyEvidence,
            FocusSurface.Bind(chimneyCenterSlot, chimneyCenter),
            FocusSurface.Bind(chimneyWidthSlot, chimneyWidth),
            FocusSurface.Bind(chimneyHeightSlot, chimneyHeight),
            FocusSurface.Bind(chimneyCapRiseSlot, chimneyCapRise));
        var doorKnob = FilledCircle(7, -15, 2);

        var cloudOval = Oval(84, 48, 24, 11);
        var cloudOutline = Cloudlet(58, 48);

        const long treeRootTicks = 14;
        const long treeJunctionTicks = 89;
        const long treeLeftTipXTicks = 24;
        const long treeLeftTipYTicks = 14;
        const long treeRightTipXTicks = 81;
        const long treeRightTipYTicks = 17;
        const long treeBallCenterXTicks = 53;
        const long treeBallCenterYTicks = 20;
        var treeRootSite = PointOnSpan(horizonBasis, treeRootTicks);
        var treeTrunkBasis = BasisSpan(treeRootSite.X, treeRootSite.Y, treeRootSite.X, treeRootSite.Y + 38);
        var treeRoot = treeRootSite.Point;
        var treeTop = treeTrunkBasis.End;
        var treeJunctionSite = PointOnSpan(treeTrunkBasis, treeJunctionTicks);
        var treeJunction = treeJunctionSite.Point;
        var treeCrownXBasis = BasisSpan(-145, 16, -75, 16);
        var treeCrownYBasis = BasisSpan(-145, 16, -145, 58);
        var treeLeftTipSite = PointFromAxes(treeCrownXBasis, treeLeftTipXTicks, treeCrownYBasis, treeLeftTipYTicks);
        var treeRightTipSite = PointFromAxes(treeCrownXBasis, treeRightTipXTicks, treeCrownYBasis, treeRightTipYTicks);
        var treeBallCenter = PointFromAxes(treeCrownXBasis, treeBallCenterXTicks, treeCrownYBasis, treeBallCenterYTicks);
        var treeLeftTip = treeLeftTipSite.Point;
        var treeRightTip = treeRightTipSite.Point;
        var treeTrunkLine = treeTrunkBasis.Span;
        var treeBall = Circle(treeBallCenter.X, treeBallCenter.Y, 17);
        var treeLineBall = Chain(treeTrunkLine, treeBall);
        var treeBranches = Chain(
            treeTrunkLine,
            GraphicStructure.FrameSegment(treeJunction, treeLeftTip),
            GraphicStructure.FrameSegment(treeJunction, treeRightTip),
            treeBall);
        var treeLeftCurve = GraphicStructure.FrameCubicCurve(treeJunction, P(-112, 11), P(-121, 18), treeLeftTip);
        var treeRightCurve = GraphicStructure.FrameCubicCurve(treeJunction, P(-103, 12), P(-95, 20), treeRightTip);
        var treeCurvedBranches = Chain(treeTrunkLine, treeLeftCurve, treeRightCurve, treeBall);
        var treeTrunkThick = Chain(
            GraphicStructure.FrameSegment(P(-113, -30), P(-103, -30)),
            GraphicStructure.FrameSegment(P(-103, -30), P(-103, 8)),
            GraphicStructure.FrameSegment(P(-103, 8), P(-113, 8)),
            GraphicStructure.FrameSegment(P(-113, 8), P(-113, -30)));
        var treeWithThickTrunk = Chain(
            treeTrunkThick,
            treeLeftCurve,
            treeRightCurve,
            treeBall);
        var leftBranchThick = AreaPolygon(
            P(-111, 2),
            P(-121, 11),
            P(-132, 20),
            P(-135, 24),
            P(-130, 28),
            P(-119, 19),
            P(-106, 8),
            P(-105, 4));
        var rightBranchThick = AreaPolygon(
            P(-111, 6),
            P(-100, 16),
            P(-90, 26),
            P(-84, 26),
            P(-85, 20),
            P(-96, 13),
            P(-106, 2),
            P(-111, 3));
        var treeWoodThick = Chain(treeTrunkThick, leftBranchThick, rightBranchThick, treeBall);
        var treeWoodMerged = AreaPolygon(
            P(-113, -30),
            P(-103, -30),
            P(-103, 2),
            P(-98, 7),
            P(-90, 14),
            P(-82, 22),
            P(-82, 27),
            P(-88, 28),
            P(-98, 20),
            P(-106, 12),
            P(-110, 12),
            P(-119, 20),
            P(-130, 28),
            P(-136, 25),
            P(-132, 20),
            P(-121, 11),
            P(-113, 5));

        var treeRootSlot = new Placeholder();
        var treeJunctionSlot = new Placeholder();
        var treeLeftTipSlot = new Placeholder();
        var treeRightTipSlot = new Placeholder();
        var treeCrownCenterSlot = new Placeholder();
        var treeTopologyTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            treeRootSlot,
            treeJunctionSlot,
            treeLeftTipSlot,
            treeRightTipSlot,
            treeCrownCenterSlot);
        var treeRootCoarseSite = PointOnSpan(horizonBasis, CoarseTicks(treeRootTicks));
        var treeJunctionCoarseSite = PointOnSpan(treeTrunkBasis, CoarseTicks(treeJunctionTicks));
        var treeLeftTipCoarseXSite = PointFromAxes(
            treeCrownXBasis,
            CoarseTicks(treeLeftTipXTicks),
            treeCrownYBasis,
            treeLeftTipYTicks);
        var treeLeftTipCoarseYSite = PointFromAxes(
            treeCrownXBasis,
            treeLeftTipXTicks,
            treeCrownYBasis,
            CoarseTicks(treeLeftTipYTicks));
        var treeRightTipCoarseXSite = PointFromAxes(
            treeCrownXBasis,
            CoarseTicks(treeRightTipXTicks),
            treeCrownYBasis,
            treeRightTipYTicks);
        var treeRightTipCoarseYSite = PointFromAxes(
            treeCrownXBasis,
            treeRightTipXTicks,
            treeCrownYBasis,
            CoarseTicks(treeRightTipYTicks));
        var treeBallCenterCoarseXSite = PointFromAxes(
            treeCrownXBasis,
            CoarseTicks(treeBallCenterXTicks),
            treeCrownYBasis,
            treeBallCenterYTicks);
        var treeBallCenterCoarseYSite = PointFromAxes(
            treeCrownXBasis,
            treeBallCenterXTicks,
            treeCrownYBasis,
            CoarseTicks(treeBallCenterYTicks));
        var treeTopologyEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(horizonBasis.Span, treeRootTicks, 100, treeRootCoarseSite.Point, treeRoot),
            ParameterValue(treeTrunkBasis.Span, treeJunctionTicks, 100, treeJunctionCoarseSite.Point, treeJunction),
            ParameterValue(treeCrownXBasis.Span, treeLeftTipXTicks, 100, treeLeftTipCoarseXSite.Point, treeLeftTip),
            ParameterValue(treeCrownYBasis.Span, treeLeftTipYTicks, 100, treeLeftTipCoarseYSite.Point, treeLeftTip),
            ParameterValue(treeCrownXBasis.Span, treeRightTipXTicks, 100, treeRightTipCoarseXSite.Point, treeRightTip),
            ParameterValue(treeCrownYBasis.Span, treeRightTipYTicks, 100, treeRightTipCoarseYSite.Point, treeRightTip),
            ParameterValue(treeCrownXBasis.Span, treeBallCenterXTicks, 100, treeBallCenterCoarseXSite.Point, treeBallCenter.Point),
            ParameterValue(treeCrownYBasis.Span, treeBallCenterYTicks, 100, treeBallCenterCoarseYSite.Point, treeBallCenter.Point));
        var treeBranchesInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            treeTopologyTemplate,
            treeBranches,
            treeTopologyEvidence,
            FocusSurface.Bind(treeRootSlot, treeRoot),
            FocusSurface.Bind(treeJunctionSlot, treeJunction),
            FocusSurface.Bind(treeLeftTipSlot, treeLeftTip),
            FocusSurface.Bind(treeRightTipSlot, treeRightTip),
            FocusSurface.Bind(treeCrownCenterSlot, treeBallCenter.Point));
        var treeCurvedBranchesInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            treeTopologyTemplate,
            treeCurvedBranches,
            treeTopologyEvidence,
            FocusSurface.Bind(treeRootSlot, treeRoot),
            FocusSurface.Bind(treeJunctionSlot, treeJunction),
            FocusSurface.Bind(treeLeftTipSlot, treeLeftTip),
            FocusSurface.Bind(treeRightTipSlot, treeRightTip),
            FocusSurface.Bind(treeCrownCenterSlot, treeBallCenter.Point));
        var treeWoodThickInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            treeTopologyTemplate,
            treeWoodThick,
            treeTopologyEvidence,
            FocusSurface.Bind(treeRootSlot, treeRoot),
            FocusSurface.Bind(treeJunctionSlot, treeJunction),
            FocusSurface.Bind(treeLeftTipSlot, treeLeftTip),
            FocusSurface.Bind(treeRightTipSlot, treeRightTip),
            FocusSurface.Bind(treeCrownCenterSlot, treeBallCenter.Point));
        var treeWoodMergedInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            treeTopologyTemplate,
            treeWoodMerged,
            treeTopologyEvidence,
            FocusSurface.Bind(treeRootSlot, treeRoot),
            FocusSurface.Bind(treeJunctionSlot, treeJunction),
            FocusSurface.Bind(treeLeftTipSlot, treeLeftTip),
            FocusSurface.Bind(treeRightTipSlot, treeRightTip),
            FocusSurface.Bind(treeCrownCenterSlot, treeBallCenter.Point));

        var leafCenterSlot = new Placeholder();
        var leafRadiusXSlot = new Placeholder();
        var leafRadiusYSlot = new Placeholder();
        var leafTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            leafCenterSlot,
            leafRadiusXSlot,
            leafRadiusYSlot);
        var leafCenterXBasis = GraphicStructure.FrameSegment(P(-145, 16), P(-75, 16));
        var leafCenterYBasis = GraphicStructure.FrameSegment(P(-145, 16), P(-145, 58));
        var leafRadiusXBasis = Relation.Segment(new Carrier(), 0, 1);
        var leafRadiusYBasis = Relation.Segment(new Carrier(), 0, 1);

        (Relation ApproxCircle, Relation Blob, Relation CircleInstance, Relation BlobInstance) LeafInstance(
            long centerXTicks,
            long centerYTicks,
            long radiusXTicks,
            long radiusYTicks)
        {
            const long scale = 100;
            var coarseCenterXTicks = CoarseTicks(centerXTicks);
            var coarseCenterYTicks = CoarseTicks(centerYTicks);
            var coarseRadiusXTicks = CoarseTicks(radiusXTicks);
            var coarseRadiusYTicks = CoarseTicks(radiusYTicks);
            var coarseCenterX = Lerp(-145, -75, coarseCenterXTicks, scale);
            var coarseCenterY = Lerp(16, 58, coarseCenterYTicks, scale);
            var centerX = Lerp(-145, -75, centerXTicks, scale);
            var centerY = Lerp(16, 58, centerYTicks, scale);
            var coarseRadiusX = Lerp(10, 24, coarseRadiusXTicks, scale);
            var coarseRadiusY = Lerp(8, 22, coarseRadiusYTicks, scale);
            var radiusX = Lerp(10, 24, radiusXTicks, scale);
            var radiusY = Lerp(8, 22, radiusYTicks, scale);
            var coarseCenter = P(coarseCenterX, coarseCenterY);
            var center = P(centerX, centerY);
            var coarseRadiusXValue = Relation.Point(new Carrier(), coarseRadiusX);
            var radiusXValue = Relation.Point(new Carrier(), radiusX);
            var coarseRadiusYValue = Relation.Point(new Carrier(), coarseRadiusY);
            var radiusYValue = Relation.Point(new Carrier(), radiusY);
            var parameterEvidence = GraphicParameterSurface.Frame(
                new Carrier(),
                ParameterValue(leafCenterXBasis, centerXTicks, scale, coarseCenter, center),
                ParameterValue(leafCenterYBasis, centerYTicks, scale, coarseCenter, center),
                ParameterValue(leafRadiusXBasis, radiusXTicks, scale, coarseRadiusXValue, radiusXValue),
                ParameterValue(leafRadiusYBasis, radiusYTicks, scale, coarseRadiusYValue, radiusYValue));
            var approximateRadius = Math.Max(6, (coarseRadiusX + coarseRadiusY) / 2);
            var approximateCircle = Circle(coarseCenterX, coarseCenterY, approximateRadius);
            var blob = FilledLeafBlob(centerX, centerY, radiusX, radiusY);
            var circleInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
                new Carrier(),
                leafTemplate,
                approximateCircle,
                parameterEvidence,
                FocusSurface.Bind(leafCenterSlot, coarseCenter),
                FocusSurface.Bind(leafRadiusXSlot, coarseRadiusXValue),
                FocusSurface.Bind(leafRadiusYSlot, coarseRadiusYValue));
            var blobInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
                new Carrier(),
                leafTemplate,
                blob,
                parameterEvidence,
                FocusSurface.Bind(leafCenterSlot, center),
                FocusSurface.Bind(leafRadiusXSlot, radiusXValue),
                FocusSurface.Bind(leafRadiusYSlot, radiusYValue));
            return (approximateCircle, blob, circleInstance, blobInstance);
        }

        var leftLeaf = LeafInstance(21, 31, 50, 50);
        var crownLeaf = LeafInstance(53, 60, 64, 64);
        var rightLeaf = LeafInstance(83, 36, 50, 50);
        var leafRegionEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            leftLeaf.CircleInstance,
            crownLeaf.CircleInstance,
            rightLeaf.CircleInstance);
        var leafBlobEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            leftLeaf.BlobInstance,
            crownLeaf.BlobInstance,
            rightLeaf.BlobInstance);
        var treeLeafRegions = Chain(
            treeWoodMerged,
            leftLeaf.ApproxCircle,
            crownLeaf.ApproxCircle,
            rightLeaf.ApproxCircle);
        var treeFluffy = Chain(
            treeWoodMerged,
            leftLeaf.Blob,
            crownLeaf.Blob,
            rightLeaf.Blob);

        var horizonState = horizon;
        var houseBoxState = Chain(horizon, houseBox);
        var roofState = Chain(horizon, houseRoof);
        var treeSeedState = Chain(horizon, houseRoof, treeLineBall);
        var cloudSeedState = Chain(horizon, houseRoof, treeLineBall, cloudOval);
        var roofOverhangState = Chain(horizon, houseWithOverhang, treeLineBall, cloudOval);
        var treeSplitState = Chain(horizon, houseWithOverhang, treeBranches, cloudOval);
        var doorRectState = Chain(horizon, houseWithOverhang, treeBranches, cloudOval, doorRectangle);
        var treeCurvedState = Chain(horizon, houseWithOverhang, treeCurvedBranches, cloudOval, doorRectangle);
        var windowRectState = Chain(horizon, houseWithOverhang, treeCurvedBranches, cloudOval, doorRectangle, windowRectangle);
        var treeTrunkState = Chain(horizon, houseWithOverhang, treeWithThickTrunk, cloudOval, doorRectangle, windowRectangle);
        var doorArchState = Chain(horizon, houseWithOverhang, treeWithThickTrunk, cloudOval, archedDoor, windowRectangle);
        var treeBranchThickState = Chain(horizon, houseWithOverhang, treeWoodThick, cloudOval, archedDoor, windowRectangle);
        var treeWoodMergedState = Chain(horizon, houseWithOverhang, treeWoodMerged, cloudOval, archedDoor, windowRectangle);
        var windowPlusState = Chain(horizon, houseWithOverhang, treeWoodMerged, cloudOval, archedDoor, windowPlus);
        var windowRefinedState = Chain(horizon, houseWithOverhang, treeWoodMerged, cloudOval, archedDoor, roundedWindowPlus);
        var treeRegionsState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOval, archedDoor, roundedWindowPlus);
        var cloudRefinedState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOutline, archedDoor, roundedWindowPlus);
        var chimneyBodyState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody);
        var treeFluffyState = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody);
        var chimneyCapState = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyWithCap);
        var finalSketch = Chain(chimneyCapState, doorKnob);

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), horizonState);
        var houseBoxStep = RefinedRung(horizonState, houseBox, horizon, horizonState, houseBox, houseBoxState);
        var roofStep = RefinedRung(houseBoxState, houseBoxTop, houseBox, horizon, houseRoof, roofState);
        var treeSeedStep = RefinedRung(roofState, treeRoot, treeRoot, roofState, treeLineBall, treeSeedState);
        var cloudSeedStep = RefinedRung(treeSeedState, cloudOval, cloudOval, treeSeedState, cloudOval, cloudSeedState);
        var roofOverhangStep = RefinedRung(cloudSeedState, roofOverhang, roofOverhang, cloudSeedState, roofOverhang, roofOverhangState, roofOverhangEvidence);
        var treeSplitStep = RefinedRung(roofOverhangState, treeJunction, treeLineBall, Chain(horizon, houseWithOverhang, cloudOval), treeBranches, treeSplitState, treeBranchesInstance);
        var doorRectStep = RefinedRung(treeSplitState, doorLeftBottom, doorLeftBottom, treeSplitState, doorRectangle, doorRectState, doorRectangleInstance);
        var treeCurveStep = RefinedRung(doorRectState, treeJunction, treeBranches, Chain(horizon, houseWithOverhang, cloudOval, doorRectangle), treeCurvedBranches, treeCurvedState, treeCurvedBranchesInstance);
        var windowRectStep = RefinedRung(treeCurvedState, windowLowerLeft, windowLowerLeft, treeCurvedState, windowRectangle, windowRectState, windowRectangleInstance);
        var treeTrunkStep = RefinedRung(windowRectState, treeTrunkLine, treeCurvedBranches, Chain(horizon, houseWithOverhang, cloudOval, doorRectangle, windowRectangle), treeWithThickTrunk, treeTrunkState);
        var doorArchStep = RefinedRung(treeTrunkState, doorRectangle, doorRectangle, Chain(horizon, houseWithOverhang, cloudOval, treeWithThickTrunk, windowRectangle), archedDoor, doorArchState, archedDoorInstance);
        var treeBranchThickStep = RefinedRung(doorArchState, treeJunction, treeWithThickTrunk, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, windowRectangle), treeWoodThick, treeBranchThickState, treeWoodThickInstance);
        var treeWoodMergeStep = RefinedRung(treeBranchThickState, treeWoodThick, treeWoodThick, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, windowRectangle), treeWoodMerged, treeWoodMergedState, treeWoodMergedInstance);
        var windowPlusStep = RefinedRung(treeWoodMergedState, windowRectangle, windowRectangle, Chain(horizon, houseWithOverhang, cloudOval, treeWoodMerged, archedDoor), windowPlus, windowPlusState, windowPlusInstance);
        var windowRefineStep = RefinedRung(windowPlusState, windowRectangle, windowPlus, Chain(horizon, houseWithOverhang, cloudOval, treeWoodMerged, archedDoor), roundedWindowPlus, windowRefinedState, roundedWindowInstance);
        var treeRegionStep = RefinedRung(windowRefinedState, treeBall, treeWoodMerged, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, roundedWindowPlus), treeLeafRegions, treeRegionsState, leafRegionEvidence);
        var cloudRefineStep = RefinedRung(treeRegionsState, cloudOval, cloudOval, Chain(horizon, houseWithOverhang, treeLeafRegions, archedDoor, roundedWindowPlus), cloudOutline, cloudRefinedState);
        var chimneyBodyStep = RefinedRung(cloudRefinedState, roofOverhang, roofOverhang, cloudRefinedState, chimneyBody, chimneyBodyState, chimneyBodyInstance);
        var treeFluffyStep = RefinedRung(chimneyBodyState, treeLeafRegions, treeLeafRegions, Chain(horizon, houseWithOverhang, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody), treeFluffy, treeFluffyState, leafBlobEvidence);
        var chimneyCapStep = RefinedRung(treeFluffyState, chimneyBody, chimneyBody, Chain(horizon, houseWithOverhang, cloudOutline, treeFluffy, archedDoor, roundedWindowPlus), chimneyWithCap, chimneyCapState, chimneyCapInstance);
        var doorKnobStep = RefinedRung(chimneyCapState, doorKnob, doorKnob, chimneyCapState, doorKnob, finalSketch);
        var sketchRungs = new[]
        {
            baseRung,
            houseBoxStep.Rung,
            roofStep.Rung,
            treeSeedStep.Rung,
            cloudSeedStep.Rung,
            roofOverhangStep.Rung,
            treeSplitStep.Rung,
            doorRectStep.Rung,
            treeCurveStep.Rung,
            windowRectStep.Rung,
            treeTrunkStep.Rung,
            doorArchStep.Rung,
            treeBranchThickStep.Rung,
            treeWoodMergeStep.Rung,
            windowPlusStep.Rung,
            windowRefineStep.Rung,
            treeRegionStep.Rung,
            cloudRefineStep.Rung,
            chimneyBodyStep.Rung,
            treeFluffyStep.Rung,
            chimneyCapStep.Rung,
            doorKnobStep.Rung
        };
        var sketchApplications = new[]
        {
            houseBoxStep.Application,
            roofStep.Application,
            treeSeedStep.Application,
            cloudSeedStep.Application,
            roofOverhangStep.Application,
            treeSplitStep.Application,
            doorRectStep.Application,
            treeCurveStep.Application,
            windowRectStep.Application,
            treeTrunkStep.Application,
            doorArchStep.Application,
            treeBranchThickStep.Application,
            treeWoodMergeStep.Application,
            windowPlusStep.Application,
            windowRefineStep.Application,
            treeRegionStep.Application,
            cloudRefineStep.Application,
            chimneyBodyStep.Application,
            treeFluffyStep.Application,
            chimneyCapStep.Application,
            doorKnobStep.Application
        };
        var ladder = TimelineLadder(sketchRungs, sketchApplications);
        var rungLabels = new[]
        {
            "Horizon",
            "House box",
            "Peak roof",
            "Tree line + circular crown",
            "Cloud oval",
            "Roof overhang",
            "Tree branch split",
            "Door rectangle",
            "Tree split becomes curves",
            "Window rectangle",
            "Tree trunk thickens",
            "Door arch",
            "Tree branches thicken",
            "Tree wood merges",
            "Window plus",
            "Window rounded rectangle",
            "Tree leaf circles",
            "Puffy cloud",
            "Chimney body",
            "Tree fluffs out",
            "Pointy chimney cap",
            "Door knob"
        };

        return new SketchScene(
            "House Drawing Ladder",
            "Ordered rough-in ladder from box and oval to the finished sketch.",
            ladder,
            seed,
            new SKRect(-156, -84, 138, 64),
            rungLabels);
    }

    private static (Relation Rung, Relation Focus, Relation Application, Relation Transform) RefinedRung(
        IElement source,
        IElement focusBasis,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement result,
        IElement? parameterEvidence = null)
    {
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(participation, replacement));
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            focusBasis,
            participation,
            complement,
            replacement,
            correspondence,
            UnknownPoint(),
            UnknownPoint());

        var selectedPort = new Placeholder();
        var carryPort = new Placeholder();
        var replacementPort = new Placeholder();
        var resultPort = new Placeholder();
        var selectedCarryReplacementPorts = TransformSurface.Ports(new Carrier(), selectedPort, carryPort, replacementPort);
        var operation = TransformSurface.Fold(selectedCarryReplacementPorts, resultPort);
        var operationProvenance = TransformSurface.Frame(
            new Carrier(),
            TransformSurface.Remove(selectedPort),
            TransformSurface.Carry(carryPort, carryPort),
            TransformSurface.Map(selectedPort, replacementPort),
            operation,
            parameterEvidence ?? UnknownPoint());
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), selectedPort, carryPort, replacementPort),
            operation,
            TransformSurface.Ports(new Carrier(), resultPort),
            UnknownPoint(),
            operationProvenance);
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(selectedPort, participation),
            FocusSurface.Bind(carryPort, complement),
            FocusSurface.Bind(replacementPort, replacement),
            FocusSurface.Bind(resultPort, result),
            FocusSurface.Bind(participation, replacement));

        if (!TransformSurface.TryCreateApplication(
                new Carrier(),
                transform,
                focus,
                bindings,
                result,
                UnknownPoint(),
                parameterEvidence ?? UnknownPoint(),
                out var application))
        {
            throw new InvalidOperationException("Generated sketch refinement did not create a transform application.");
        }

        if (!StructuralLadderSurface.TryCreateRungFromTransformApplication(
                new Carrier(),
                application,
                out var rung))
        {
            throw new InvalidOperationException("Generated sketch transform application did not create a ladder rung.");
        }

        return (rung, focus, application, transform);
    }

    private static Relation TimelineLadder(IReadOnlyList<Relation> rungs, IReadOnlyList<Relation> applications)
    {
        if (rungs.Count == 0)
        {
            return StructuralLadderSurface.BaseRung(new Carrier(), UnknownPoint());
        }

        var clock = TimeStepSequence.CreateClock(new Carrier());
        var stepCount = Math.Min(Math.Max(0, rungs.Count - 1), applications.Count);
        var steps = new Relation[stepCount];
        for (var index = 0; index < stepCount; index++)
        {
            steps[index] = StructuralLadderSurface.TransformStep(
                new Carrier(),
                clock,
                index,
                index + 1,
                rungs[index],
                applications[index],
                rungs[index + 1],
                UnknownPoint());
        }

        return StructuralLadderSurface.Create(
            new Carrier(),
            rungs[0],
            StructuralLadderSurface.RungFrame(new Carrier(), [.. rungs]),
            StructuralLadderSurface.Timeline(new Carrier(), clock, rungs[0], steps),
            UnknownPoint(),
            UnknownPoint());
    }

    private static Relation Point(Carrier frame, Carrier x, Carrier y, long xTicks, long yTicks) =>
        GraphicStructure.FramePoint(frame, new CarrierLocation(x, xTicks), new CarrierLocation(y, yTicks));

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Relation Chain(params IElement[] elements)
    {
        if (elements.Length == 0)
        {
            return new Relation(Carrier.Unknown, Carrier.Unknown);
        }

        IElement structure = elements[^1];
        for (var index = elements.Length - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure as Relation ?? new Relation(structure, structure);
    }
}
