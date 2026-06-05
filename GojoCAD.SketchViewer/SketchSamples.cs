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

        (
            Relation LowerLeft,
            Relation LowerRight,
            Relation UpperRight,
            Relation UpperLeft,
            long Left,
            long Right,
            long Bottom,
            long Top) RectFromCenter(long centerX, long centerY, long width, long height)
        {
            var halfWidth = width / 2;
            var halfHeight = height / 2;
            var left = centerX - halfWidth;
            var right = centerX + halfWidth;
            var bottom = centerY - halfHeight;
            var top = centerY + halfHeight;
            return (
                P(left, bottom),
                P(right, bottom),
                P(right, top),
                P(left, top),
                left,
                right,
                bottom,
                top);
        }

        Relation RoundedRectOutline(long centerX, long centerY, long width, long height, long radius)
        {
            var rect = RectFromCenter(centerX, centerY, width, height);
            var control = Math.Max(1, radius / 2);
            return Chain(
                GraphicStructure.FrameSegment(P(rect.Left + radius, rect.Top), P(rect.Right - radius, rect.Top)),
                GraphicStructure.FrameCubicCurve(
                    P(rect.Right - radius, rect.Top),
                    P(rect.Right - control, rect.Top),
                    P(rect.Right, rect.Top - control),
                    P(rect.Right, rect.Top - radius)),
                GraphicStructure.FrameSegment(P(rect.Right, rect.Top - radius), P(rect.Right, rect.Bottom + radius)),
                GraphicStructure.FrameCubicCurve(
                    P(rect.Right, rect.Bottom + radius),
                    P(rect.Right, rect.Bottom + control),
                    P(rect.Right - control, rect.Bottom),
                    P(rect.Right - radius, rect.Bottom)),
                GraphicStructure.FrameSegment(P(rect.Right - radius, rect.Bottom), P(rect.Left + radius, rect.Bottom)),
                GraphicStructure.FrameCubicCurve(
                    P(rect.Left + radius, rect.Bottom),
                    P(rect.Left + control, rect.Bottom),
                    P(rect.Left, rect.Bottom + control),
                    P(rect.Left, rect.Bottom + radius)),
                GraphicStructure.FrameSegment(P(rect.Left, rect.Bottom + radius), P(rect.Left, rect.Top - radius)),
                GraphicStructure.FrameCubicCurve(
                    P(rect.Left, rect.Top - radius),
                    P(rect.Left, rect.Top - control),
                    P(rect.Left + control, rect.Top),
                    P(rect.Left + radius, rect.Top)));
        }

        (Relation Point, long X, long Y) RelativeSite(
            long originX,
            long originY,
            long radiusX,
            long radiusY,
            long xTicks,
            long yTicks,
            long scale = 100)
        {
            return Site(
                originX + radiusX * xTicks / scale,
                originY + radiusY * yTicks / scale);
        }

        Relation ProfileFill(
            long centerX,
            long centerY,
            long radiusX,
            long radiusY,
            params (long X, long Y)[] profile)
        {
            var points = new Relation[profile.Length];
            for (var index = 0; index < profile.Length; index++)
            {
                var site = RelativeSite(centerX, centerY, radiusX, radiusY, profile[index].X, profile[index].Y);
                points[index] = site.Point;
            }

            return AreaPolygon(points);
        }

        Relation ProfileSurface(params (long X, long Y)[] profile)
        {
            var profileFrame = new Carrier();
            var profileX = new Carrier();
            var profileY = new Carrier();
            var points = new IElement[profile.Length];
            for (var index = 0; index < profile.Length; index++)
            {
                points[index] = GraphicStructure.FramePoint(
                    profileFrame,
                    new CarrierLocation(profileX, profile[index].X),
                    new CarrierLocation(profileY, profile[index].Y));
            }

            return GraphicParameterSurface.Frame(new Carrier(), points);
        }

        (Relation Point, long X, long Y) BowedControl(
            (Relation Point, long X, long Y) start,
            (Relation Point, long X, long Y) end,
            long alongTicks,
            long normalTicks,
            long scale = 100)
        {
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            return Site(
                start.X + dx * alongTicks / scale - dy * normalTicks / scale,
                start.Y + dy * alongTicks / scale + dx * normalTicks / scale);
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

        Relation Cloudlet(long anchorX, long anchorY, long width, long height)
        {
            Relation C(long xTicks, long yTicks) =>
                RelativeSite(anchorX, anchorY, width, height, xTicks, yTicks).Point;

            var left = C(0, 0);
            var leftPeak = C(19, 48);
            var leftValley = C(35, 20);
            var middlePeak = C(56, 64);
            var middleValley = C(74, 24);
            var rightPeak = C(91, 44);
            var right = C(100, -4);
            var lowerRight = C(78, -36);
            var lowerLeft = C(30, -32);
            return Chain(
                GraphicStructure.FrameCubicCurve(left, C(0, 28), C(13, 48), leftPeak),
                GraphicStructure.FrameCubicCurve(leftPeak, C(24, 48), C(33, 36), leftValley),
                GraphicStructure.FrameCubicCurve(leftValley, C(39, 48), C(48, 64), middlePeak),
                GraphicStructure.FrameCubicCurve(middlePeak, C(63, 64), C(70, 44), middleValley),
                GraphicStructure.FrameCubicCurve(middleValley, C(80, 44), C(83, 44), rightPeak),
                GraphicStructure.FrameCubicCurve(rightPeak, C(96, 44), C(100, 20), right),
                GraphicStructure.FrameCubicCurve(right, C(100, -24), C(89, -36), lowerRight),
                GraphicStructure.FrameCubicCurve(lowerRight, C(67, -36), C(41, -32), lowerLeft),
                GraphicStructure.FrameCubicCurve(lowerLeft, C(11, -32), C(0, -28), left));
        }

        var leafBlobProfile = new (long X, long Y)[]
        {
            (-100, 0),
            (-83, 50),
            (-50, 100),
            (0, 110),
            (50, 100),
            (100, 33),
            (100, -33),
            (50, -100),
            (0, -105),
            (-67, -67)
        };

        Relation FilledLeafBlob(long centerX, long centerY, long radiusX, long radiusY)
        {
            return ProfileFill(
                centerX,
                centerY,
                radiusX,
                radiusY,
                leafBlobProfile);
        }

        Relation SoftTreeWoodFill(params (long X, long Y)[] points)
        {
            var relations = new Relation[points.Length];
            for (var index = 0; index < points.Length; index++)
            {
                relations[index] = P(points[index].X, points[index].Y);
            }

            return AreaPolygon(relations);
        }

        Relation ClosedCubicOutline(params (Relation Start, Relation ControlA, Relation ControlB, Relation End)[] curves) =>
            Chain(curves
                .Select(curve => GraphicStructure.FrameCubicCurve(
                    curve.Start,
                    curve.ControlA,
                    curve.ControlB,
                    curve.End))
                .Cast<IElement>()
                .ToArray());

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
        const long doorWidthTicks = 28;
        const long doorCoarseWidthTicks = 20;
        const long doorHeightTicks = 22;
        const long doorCoarseHeightTicks = 15;
        var doorWidth = Relation.Point(new Carrier(), doorWidthTicks);
        var doorCoarseWidth = Relation.Point(new Carrier(), doorCoarseWidthTicks);
        var doorHeight = Relation.Point(new Carrier(), doorHeightTicks);
        var doorCoarseHeight = Relation.Point(new Carrier(), doorCoarseHeightTicks);
        var doorEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(wallXBasis, doorCenterXTicks, 100, doorCoarseXPoint, doorCenter),
            ParameterValue(wallYBasis, doorCenterYTicks, 100, doorCoarseYPoint, doorCenter),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 47, 100, doorCoarseWidth, doorWidth),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 58, 100, doorCoarseHeight, doorHeight));
        var doorRect = RectFromCenter(doorCenterSite.X, doorCenterSite.Y, doorWidthTicks, doorHeightTicks);
        var doorLeftBottom = doorRect.LowerLeft;
        var doorRightBottom = doorRect.LowerRight;
        var doorRightTop = doorRect.UpperRight;
        var doorLeftTop = doorRect.UpperLeft;
        var doorRectangle = RectOutline(doorRect.LowerLeft, doorRect.LowerRight, doorRect.UpperRight, doorRect.UpperLeft);
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
            GraphicStructure.FrameCubicCurve(
                doorLeftTop,
                P(doorRect.Left, doorRect.Top + doorWidthTicks * 4 / 7),
                P(doorRect.Right, doorRect.Top + doorWidthTicks * 4 / 7),
                doorRightTop),
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
        const long windowCenterXTicks = 81;
        const long windowCenterYTicks = 68;
        var windowCenterSite = PointFromAxes(houseBottomBasis, windowCenterXTicks, houseLeftBasis, windowCenterYTicks);
        var windowCoarseXSite = PointFromAxes(houseBottomBasis, CoarseTicks(windowCenterXTicks), houseLeftBasis, windowCenterYTicks);
        var windowCoarseYSite = PointFromAxes(houseBottomBasis, windowCenterXTicks, houseLeftBasis, CoarseTicks(windowCenterYTicks));
        var windowCenter = windowCenterSite.Point;
        var windowCoarseXPoint = windowCoarseXSite.Point;
        var windowCoarseYPoint = windowCoarseYSite.Point;
        const long windowWidthTicks = 20;
        const long windowCoarseWidthTicks = 16;
        const long windowHeightTicks = 20;
        const long windowCoarseHeightTicks = 16;
        const long windowCornerRadiusTicks = 4;
        var windowWidth = Relation.Point(new Carrier(), windowWidthTicks);
        var windowCoarseWidth = Relation.Point(new Carrier(), windowCoarseWidthTicks);
        var windowHeight = Relation.Point(new Carrier(), windowHeightTicks);
        var windowCoarseHeight = Relation.Point(new Carrier(), windowCoarseHeightTicks);
        var windowCornerRadius = Relation.Point(new Carrier(), windowCornerRadiusTicks);
        var windowCoarseCornerRadius = Relation.Point(new Carrier(), 0);
        var windowEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(wallXBasis, windowCenterXTicks, 100, windowCoarseXPoint, windowCenter),
            ParameterValue(wallYBasis, windowCenterYTicks, 100, windowCoarseYPoint, windowCenter),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 35, 100, windowCoarseWidth, windowWidth),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 53, 100, windowCoarseHeight, windowHeight),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), 32, 100, windowCoarseCornerRadius, windowCornerRadius));
        var windowRect = RectFromCenter(windowCenterSite.X, windowCenterSite.Y, windowWidthTicks, windowHeightTicks);
        var windowLowerLeft = windowRect.LowerLeft;
        var windowLowerRight = windowRect.LowerRight;
        var windowUpperRight = windowRect.UpperRight;
        var windowUpperLeft = windowRect.UpperLeft;
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
            GraphicStructure.FrameSegment(P(windowRect.Left, windowCenterSite.Y), windowCenter),
            GraphicStructure.FrameSegment(windowCenter, P(windowRect.Right, windowCenterSite.Y)),
            GraphicStructure.FrameSegment(P(windowCenterSite.X, windowRect.Top), windowCenter),
            GraphicStructure.FrameSegment(windowCenter, P(windowCenterSite.X, windowRect.Bottom)));
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
        var roundedWindow = RoundedRectOutline(
            windowCenterSite.X,
            windowCenterSite.Y,
            windowWidthTicks,
            windowHeightTicks,
            windowCornerRadiusTicks);
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
        const long chimneyWidthTicks = 12;
        const long chimneyCoarseWidthTicks = 8;
        const long chimneyHeightTicks = 19;
        const long chimneyCoarseHeightTicks = 12;
        const long chimneyCapRiseTicks = 7;
        var chimneyWidth = Relation.Point(new Carrier(), chimneyWidthTicks);
        var chimneyCoarseWidth = Relation.Point(new Carrier(), chimneyCoarseWidthTicks);
        var chimneyHeight = Relation.Point(new Carrier(), chimneyHeightTicks);
        var chimneyCoarseHeight = Relation.Point(new Carrier(), chimneyCoarseHeightTicks);
        var chimneyCapRise = Relation.Point(new Carrier(), chimneyCapRiseTicks);
        var chimneyNoCapRise = Relation.Point(new Carrier(), 0);
        var chimneyCapWidthTicks = chimneyWidthTicks + 8;
        var chimneyCapHalfWidthTicks = chimneyCapWidthTicks / 2;
        var chimneyWithCap = Chain(
            chimneyBody,
            GraphicStructure.FrameSegment(
                P(chimneyCenterOnRoof.X - chimneyCapHalfWidthTicks, chimneyTopY),
                P(chimneyCenterOnRoof.X + chimneyCapHalfWidthTicks, chimneyTopY)),
            GraphicStructure.FrameSegment(
                P(chimneyCenterOnRoof.X + chimneyCapHalfWidthTicks, chimneyTopY),
                P(chimneyCenterOnRoof.X, chimneyTopY + chimneyCapRiseTicks)),
            GraphicStructure.FrameSegment(
                P(chimneyCenterOnRoof.X, chimneyTopY + chimneyCapRiseTicks),
                P(chimneyCenterOnRoof.X - chimneyCapHalfWidthTicks, chimneyTopY)));
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
        var doorKnob = FilledCircle(
            doorCenterSite.X + doorWidthTicks / 4,
            doorCenterSite.Y + doorHeightTicks / 5,
            Math.Max(2, doorWidthTicks / 14));

        const long cloudAnchorXTicks = 10;
        const long cloudAnchorYTicks = 80;
        const long cloudWidthTicks = 54;
        const long cloudHeightTicks = 25;
        var cloudXBasis = BasisSpan(52, 48, 112, 48);
        var cloudYBasis = BasisSpan(52, 8, 52, 58);
        var cloudAnchorSite = PointFromAxes(cloudXBasis, cloudAnchorXTicks, cloudYBasis, cloudAnchorYTicks);
        var cloudAnchorCoarseXSite = PointFromAxes(cloudXBasis, CoarseTicks(cloudAnchorXTicks), cloudYBasis, cloudAnchorYTicks);
        var cloudAnchorCoarseYSite = PointFromAxes(cloudXBasis, cloudAnchorXTicks, cloudYBasis, CoarseTicks(cloudAnchorYTicks));
        var cloudWidth = Relation.Point(new Carrier(), cloudWidthTicks);
        var cloudCoarseWidth = Relation.Point(new Carrier(), CoarseTicks(cloudWidthTicks));
        var cloudHeight = Relation.Point(new Carrier(), cloudHeightTicks);
        var cloudCoarseHeight = Relation.Point(new Carrier(), CoarseTicks(cloudHeightTicks));
        var cloudOval = Oval(
            cloudAnchorSite.X + cloudWidthTicks / 2,
            cloudAnchorSite.Y,
            cloudWidthTicks * 9 / 20,
            cloudHeightTicks * 11 / 25);
        var cloudOutline = Cloudlet(cloudAnchorSite.X, cloudAnchorSite.Y, cloudWidthTicks, cloudHeightTicks);
        var cloudAnchorSlot = new Placeholder();
        var cloudWidthSlot = new Placeholder();
        var cloudHeightSlot = new Placeholder();
        var cloudTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            cloudAnchorSlot,
            cloudWidthSlot,
            cloudHeightSlot);
        var cloudEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            ParameterValue(cloudXBasis.Span, cloudAnchorXTicks, 100, cloudAnchorCoarseXSite.Point, cloudAnchorSite.Point),
            ParameterValue(cloudYBasis.Span, cloudAnchorYTicks, 100, cloudAnchorCoarseYSite.Point, cloudAnchorSite.Point),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), cloudWidthTicks, 100, cloudCoarseWidth, cloudWidth),
            ParameterValue(Relation.Segment(new Carrier(), 0, 1), cloudHeightTicks, 100, cloudCoarseHeight, cloudHeight));
        var cloudOvalInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            cloudTemplate,
            cloudOval,
            cloudEvidence,
            FocusSurface.Bind(cloudAnchorSlot, cloudAnchorSite.Point),
            FocusSurface.Bind(cloudWidthSlot, cloudWidth),
            FocusSurface.Bind(cloudHeightSlot, cloudHeight));
        var cloudOutlineInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            cloudTemplate,
            cloudOutline,
            cloudEvidence,
            FocusSurface.Bind(cloudAnchorSlot, cloudAnchorSite.Point),
            FocusSurface.Bind(cloudWidthSlot, cloudWidth),
            FocusSurface.Bind(cloudHeightSlot, cloudHeight));

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
        var treeLeftControlA = BowedControl(treeJunctionSite, treeLeftTipSite, 25, -15);
        var treeLeftControlB = BowedControl(treeJunctionSite, treeLeftTipSite, 70, -10);
        var treeRightControlA = BowedControl(treeJunctionSite, treeRightTipSite, 35, 5);
        var treeRightControlB = BowedControl(treeJunctionSite, treeRightTipSite, 70, 10);
        var treeLeftCurve = GraphicStructure.FrameCubicCurve(
            treeJunction,
            treeLeftControlA.Point,
            treeLeftControlB.Point,
            treeLeftTip);
        var treeRightCurve = GraphicStructure.FrameCubicCurve(
            treeJunction,
            treeRightControlA.Point,
            treeRightControlB.Point,
            treeRightTip);
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
        var woodBaseLeft = P(-113, -30);
        var woodBaseRight = P(-103, -30);
        var woodTrunkRight = P(-103, 2);
        var woodRightLower = P(-82, 22);
        var woodRightOuter = P(-82, 27);
        var woodRightUpper = P(-88, 28);
        var woodRightCrotch = P(-106, 12);
        var woodCenterCrotch = P(-110, 12);
        var woodLeftUpper = P(-130, 28);
        var woodLeftOuter = P(-136, 25);
        var woodLeftLower = P(-132, 20);
        var woodTrunkLeft = P(-113, 5);
        var treeWoodMergedFill = SoftTreeWoodFill(
            (-113, -30),
            (-103, -30),
            (-103, -18),
            (-103, -6),
            (-102, 2),
            (-97, 8),
            (-91, 14),
            (-86, 19),
            (-82, 22),
            (-82, 26),
            (-86, 28),
            (-93, 25),
            (-100, 20),
            (-106, 12),
            (-110, 12),
            (-116, 16),
            (-123, 24),
            (-130, 28),
            (-135, 26),
            (-136, 24),
            (-132, 20),
            (-126, 16),
            (-120, 11),
            (-113, 5),
            (-113, -8),
            (-113, -20));
        var treeWoodMergedOutline = ClosedCubicOutline(
            (woodBaseLeft, P(-110, -30), P(-106, -30), woodBaseRight),
            (woodBaseRight, P(-103, -20), P(-103, -6), woodTrunkRight),
            (woodTrunkRight, P(-98, 7), P(-89, 14), woodRightLower),
            (woodRightLower, P(-80, 24), P(-82, 28), woodRightOuter),
            (woodRightOuter, P(-84, 29), P(-86, 29), woodRightUpper),
            (woodRightUpper, P(-94, 27), P(-101, 17), woodRightCrotch),
            (woodRightCrotch, P(-107, 12), P(-109, 12), woodCenterCrotch),
            (woodCenterCrotch, P(-116, 15), P(-123, 25), woodLeftUpper),
            (woodLeftUpper, P(-133, 30), P(-137, 27), woodLeftOuter),
            (woodLeftOuter, P(-138, 23), P(-135, 20), woodLeftLower),
            (woodLeftLower, P(-126, 16), P(-118, 9), woodTrunkLeft),
            (woodTrunkLeft, P(-113, -5), P(-113, -20), woodBaseLeft));
        var treeWoodMerged = Chain(treeWoodMergedFill, treeWoodMergedOutline);

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
        var leafProfileSlot = new Placeholder();
        var leafProfile = ProfileSurface(leafBlobProfile);
        var leafTemplate = GraphicParameterSurface.Frame(
            new Carrier(),
            leafCenterSlot,
            leafRadiusXSlot,
            leafRadiusYSlot,
            leafProfileSlot);
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
                FocusSurface.Bind(leafRadiusYSlot, coarseRadiusYValue),
                FocusSurface.Bind(leafProfileSlot, leafProfile));
            var blobInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
                new Carrier(),
                leafTemplate,
                blob,
                parameterEvidence,
                FocusSurface.Bind(leafCenterSlot, center),
                FocusSurface.Bind(leafRadiusXSlot, radiusXValue),
                FocusSurface.Bind(leafRadiusYSlot, radiusYValue),
                FocusSurface.Bind(leafProfileSlot, leafProfile));
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
        var cloudSeedStep = RefinedRung(treeSeedState, cloudOval, cloudOval, treeSeedState, cloudOval, cloudSeedState, cloudOvalInstance);
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
        var cloudRefineStep = RefinedRung(treeRegionsState, cloudOval, cloudOval, Chain(horizon, houseWithOverhang, treeLeafRegions, archedDoor, roundedWindowPlus), cloudOutline, cloudRefinedState, cloudOutlineInstance);
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
