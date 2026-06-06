using Gojo.Core;
using GojoCAD.GCode;
using Gojo.Structures;
using GojoCAD.Structures;
using GojoDraw;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal static class SketchSamples
{
    public static IReadOnlyList<SketchScene> CreateInitialScenes()
    {
        var seed = Random.Shared.Next();
        return
        [
            CreateOffsetStudy(seed),
            CreateGCodeTrace(seed),
            CreateScratchPinsGCodeTrace(seed),
            CreateComplexHouseSketch(seed)
        ];
    }

    private static SketchScene CreateScratchPinsGCodeTrace(int seed)
    {
        var structure = GCodeParser.Parse(
            ReadGCodeSample("abcd.nc"),
            new GCodeParseOptions(
                CoordinateTicksPerUnit: 10000,
                ArcTicksPerTurn: 1440,
                PenDownZ: 0d));

        return new SketchScene(
            "ABCD ",
            "Sample gcode",
            structure,
            seed,
            ModelBounds: new SKRect(-500f, -205000f, 240000f, -25000f),
            RungLabels: ["Imported GCode"]);
    }

    private static SketchScene CreateGCodeTrace(int seed)
    {
        const string program = """
            (GojoCAD G-code import sample: square and arc corners)
            (Units: millimeters. Coordinates are absolute.)
            G21
            G90
            F900

            (Stroke 1: outer path with square left corners and rounded right corners)
            T1 M6
            G0 Z5
            G0 X0 Y0
            G1 Z-1
            G1 X70 Y0
            G3 X82 Y12 I0 J12
            G1 X82 Y48
            G3 X70 Y60 I-12 J0
            G1 X0 Y60
            G1 X0 Y0
            G0 Z5

            (Stroke 2: inner square with hard line-line corners)
            T2 M6
            G0 X20 Y18
            G1 Z-1
            G1 X50 Y18
            G1 X50 Y42
            G1 X20 Y42
            G1 X20 Y18
            G0 Z5

            M30
            """;
        var structure = GCodeParser.Parse(
            program,
            new GCodeParseOptions(
                CoordinateTicksPerUnit: 10,
                ArcTicksPerTurn: 1440,
                PenDownZ: 0d));

        return new SketchScene(
            "G-code Import",
            "Parsed G0/G1/G2/G3 motion as GojoCAD basis, flow, radial, and circumference structure.",
            structure,
            seed,
            RungLabels: ["Imported program"]);
    }

    private static string ReadGCodeSample(string fileName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "G code", "samples", fileName),
            Path.Combine(Environment.CurrentDirectory, "GojoCAD", "G code", "samples", fileName),
            Path.Combine(Environment.CurrentDirectory, "G code", "samples", fileName)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }
        }

        throw new FileNotFoundException($"Could not find G-code sample '{fileName}'.", fileName);
    }

    private static SketchScene CreateOffsetStudy(int seed)
    {
        var random = new Random(seed);
        var frame = new Carrier();
        var x = new Carrier();
        var y = new Carrier();
        var samples = new List<GojoCadOffsetSample>();
        var requests = new List<IElement>();

        float Pick(float min, float max) =>
            min + random.NextSingle() * (max - min);

        long Tick(float value) =>
            (long)MathF.Round(value * 10f);

        Relation FP(SKPoint point) =>
            Point(frame, x, y, Tick(point.X), Tick(point.Y));

        Relation Distance(float distance) =>
            Relation.Segment(new Carrier(), 0, Tick(MathF.Abs(distance)));

        Relation Segment(SKPoint start, SKPoint end) =>
            GraphicStructure.FrameSegment(FP(start), FP(end));

        Relation ArcSource(SKPoint center, SKPoint start, SKPoint end) =>
            GraphicStructure.FrameArc(FP(center), FP(start), FP(end));

        void AddNormalOffset(
            string label,
            IReadOnlyList<SKPoint> original,
            Relation source,
            float offsetDistance,
            bool closed = false,
            SKPoint? interiorWitness = null,
            IReadOnlyList<SKPoint>? closure = null)
        {
            IReadOnlyList<SKPoint> interior;
            IReadOnlyList<SKPoint> exterior;
            if (interiorWitness is { } witness)
            {
                interior = GojoCadOffsetGeometry.OffsetTowardWitness(
                    original,
                    witness,
                    offsetDistance,
                    closed,
                    out exterior);
            }
            else
            {
                interior = GojoCadOffsetGeometry.OffsetNormalField(original, offsetDistance, closed);
                exterior = GojoCadOffsetGeometry.OffsetNormalField(original, -offsetDistance, closed);
            }

            samples.Add(new GojoCadOffsetSample(label, original, interior, exterior, closed, closure));
            requests.Add(GraphicOffsetSurface.RequestWithDistance(new Carrier(), source, Distance(offsetDistance)));
        }

        void AddRadialOffset(
            string label,
            IReadOnlyList<SKPoint> original,
            IReadOnlyList<SKPoint> interior,
            IReadOnlyList<SKPoint> exterior,
            Relation source,
            float offsetDistance,
            bool closed,
            IReadOnlyList<SKPoint>? closure = null)
        {
            samples.Add(new GojoCadOffsetSample(label, original, interior, exterior, closed, closure));
            requests.Add(GraphicOffsetSurface.RequestWithDistance(new Carrier(), source, Distance(offsetDistance)));
        }

        var lineAngle = Pick(-35f, 35f) * MathF.PI / 180f;
        var lineStart = new SKPoint(-46f, -2f);
        var lineEnd = new SKPoint(42f, -2f + MathF.Sin(lineAngle) * 36f);
        AddNormalOffset("line + positive flow", [lineStart, lineEnd], Segment(lineStart, lineEnd), Pick(7f, 10f));

        var inverseStart = new SKPoint(42f, 6f);
        var inverseEnd = new SKPoint(-42f, Pick(-20f, 20f));
        AddNormalOffset("line + inverse flow", [inverseStart, inverseEnd], Segment(inverseStart, inverseEnd), Pick(7f, 10f));

        var circleCenter = new SKPoint(0f, 0f);
        var circleRadius = Pick(28f, 36f);
        var circleOffset = Pick(5f, 7f);
        var circle = CirclePoints(circleCenter, circleRadius, 64);
        var innerCircle = CirclePoints(circleCenter, circleRadius - circleOffset, 64);
        var outerCircle = CirclePoints(circleCenter, circleRadius + circleOffset, 64);
        var circleRight = PointOnCircle(circleCenter, circleRadius, 0f);
        var circleTop = PointOnCircle(circleCenter, circleRadius, 90f);
        var circleLeft = PointOnCircle(circleCenter, circleRadius, 180f);
        var circleBottom = PointOnCircle(circleCenter, circleRadius, 270f);
        var circleSource = Chain(
            ArcSource(circleCenter, circleRight, circleTop),
            ArcSource(circleCenter, circleTop, circleLeft),
            ArcSource(circleCenter, circleLeft, circleBottom),
            ArcSource(circleCenter, circleBottom, circleRight));
        AddRadialOffset("circle full boundary", circle, innerCircle, outerCircle, circleSource, circleOffset, closed: true);

        var arcCenter = new SKPoint(0f, 0f);
        var arcRadius = Pick(31f, 40f);
        var arcStart = Pick(20f, 85f);
        var arcSweep = Pick(150f, 230f);
        var arcOffset = Pick(5f, 7f);
        var arc = ArcPoints(arcCenter, arcRadius, arcStart, arcSweep, 38);
        var arcInterior = ArcPoints(arcCenter, arcRadius - arcOffset, arcStart, arcSweep, 38);
        var arcExterior = ArcPoints(arcCenter, arcRadius + arcOffset, arcStart, arcSweep, 38);
        AddRadialOffset(
            "arc partial boundary",
            arc,
            arcInterior,
            arcExterior,
            ArcSource(arcCenter, arc[0], arc[^1]),
            arcOffset,
            closed: false);

        var quadStart = new SKPoint(-48f, -24f);
        var quadControl = new SKPoint(Pick(-10f, 12f), Pick(26f, 48f));
        var quadEnd = new SKPoint(48f, Pick(-22f, 4f));
        var quad = QuadraticPoints(quadStart, quadControl, quadEnd, 44);
        AddNormalOffset(
            "quadratic Bezier",
            quad,
            GraphicStructure.FrameQuadraticCurve(FP(quadStart), FP(quadControl), FP(quadEnd)),
            Pick(5f, 8f));

        var cubicStart = new SKPoint(-50f, -20f);
        var cubicFirst = new SKPoint(Pick(-40f, -18f), Pick(30f, 50f));
        var cubicSecond = new SKPoint(Pick(18f, 42f), Pick(-48f, -24f));
        var cubicEnd = new SKPoint(50f, Pick(10f, 28f));
        var cubic = CubicPoints(cubicStart, cubicFirst, cubicSecond, cubicEnd, 52);
        AddNormalOffset(
            "cubic Bezier",
            cubic,
            GraphicStructure.FrameCubicCurve(FP(cubicStart), FP(cubicFirst), FP(cubicSecond), FP(cubicEnd)),
            Pick(5f, 8f));

        var clothoidStart = new SKPoint(-58f, Pick(-28f, -12f));
        var clothoidEnd = new SKPoint(58f, Pick(16f, 34f));
        var startAngle = Pick(-8f, 28f) * MathF.PI / 180f;
        var endAngle = Pick(132f, 184f) * MathF.PI / 180f;
        var startTangent = new SKPoint(MathF.Cos(startAngle), MathF.Sin(startAngle));
        var endTangent = new SKPoint(MathF.Cos(endAngle), MathF.Sin(endAngle));
        var clothoid = CubicFromTangents(clothoidStart, clothoidEnd, startTangent, endTangent, 32f, 52);
        var clothoidIncoming = Segment(Add(clothoidStart, Scale(startTangent, -26f)), clothoidStart);
        var clothoidOutgoing = Segment(clothoidEnd, Add(clothoidEnd, Scale(endTangent, 26f)));
        var clothoidSource = GraphicStructure.FrameClothoidTransition(
            GraphicStructure.FrameCurveTransition(clothoidIncoming, clothoidOutgoing),
            Segment(clothoidStart, Add(clothoidStart, Scale(startTangent, 32f))));
        AddNormalOffset("clothoid line-line", clothoid, clothoidSource, Pick(5f, 8f));

        var arcLineCenter = new SKPoint(-32f, -8f);
        var arcLineRadius = Pick(22f, 30f);
        var arcLineStartAngle = Pick(115f, 150f);
        var arcLineEndAngle = Pick(30f, 55f);
        var arcLineStart = PointOnCircle(arcLineCenter, arcLineRadius, arcLineStartAngle);
        var arcLineEntry = PointOnCircle(arcLineCenter, arcLineRadius, arcLineEndAngle);
        var arcLineTangent = TangentOnCircle(arcLineCenter, arcLineEntry, clockwise: true);
        var lineTarget = new SKPoint(52f, Pick(16f, 34f));
        var lineTangent = Unit(arcLineEntry, lineTarget);
        var arcLineClothoid = CubicFromTangents(arcLineEntry, lineTarget, arcLineTangent, lineTangent, 28f, 48);
        var arcLineSource = GraphicStructure.FrameClothoidTransition(
            GraphicStructure.FrameCurveTransition(
                ArcSource(arcLineCenter, arcLineStart, arcLineEntry),
                Segment(lineTarget, Add(lineTarget, Scale(lineTangent, 24f)))),
            Segment(arcLineEntry, Add(arcLineEntry, Scale(arcLineTangent, 28f))));
        AddNormalOffset("clothoid arc-line", arcLineClothoid, arcLineSource, Pick(5f, 7f));

        var involuteCenter = new SKPoint(0f, 0f);
        var involuteRadius = Pick(17f, 23f);
        var involuteStartAngle = Pick(-30f, 35f) * MathF.PI / 180f;
        var involuteTheta = Pick(2.2f, 3.15f);
        var involute = InvolutePoints(involuteCenter, involuteRadius, involuteStartAngle, involuteTheta, 64);
        var involuteBaseStart = PointOnCircleRadians(involuteCenter, involuteRadius, involuteStartAngle);
        var involuteBaseEnd = PointOnCircleRadians(involuteCenter, involuteRadius, involuteStartAngle + involuteTheta);
        var involuteSource = GraphicStructure.FrameInvoluteCurve(
            ArcSource(involuteCenter, involuteBaseStart, involuteBaseEnd),
            Segment(involuteBaseStart, involute[^1]));
        AddNormalOffset("involute unwind", involute, involuteSource, Pick(4f, 6f));

        var cCenter = new SKPoint(0f, 0f);
        var cRadius = Pick(28f, 36f);
        var cStart = Pick(48f, 70f);
        var cSweep = Pick(225f, 260f);
        var cShape = ArcPoints(cCenter, cRadius, cStart, cSweep, 52);
        var cClosure = CubicPoints(
            cShape[^1],
            Add(cShape[^1], new SKPoint(20f, 8f)),
            Add(cShape[0], new SKPoint(20f, -8f)),
            cShape[0],
            18);
        AddNormalOffset(
            "open C with closure tension",
            cShape,
            ArcSource(cCenter, cShape[0], cShape[^1]),
            Pick(5f, 7f),
            closed: false,
            interiorWitness: cCenter,
            closure: cClosure);

        var study = new GojoCadOffsetStudy(samples);
        return new SketchScene(
            "Offset Curve Study",
            "Source curves with structural offset requests; red is interior, green is exterior.",
            GraphicConceptShape.Frame(new Carrier(), [.. requests]),
            seed,
            RungLabels: ["Offset samples"],
            OffsetStudy: study);
    }

    private static IReadOnlyList<SKPoint> CirclePoints(SKPoint center, float radius, int steps)
    {
        var count = Math.Max(8, steps);
        var points = new SKPoint[count];
        for (var index = 0; index < count; index++)
        {
            points[index] = PointOnCircle(center, radius, index * 360f / count);
        }

        return points;
    }

    private static IReadOnlyList<SKPoint> ArcPoints(SKPoint center, float radius, float startDegrees, float sweepDegrees, int steps)
    {
        var count = Math.Max(2, steps);
        var points = new SKPoint[count + 1];
        for (var index = 0; index <= count; index++)
        {
            var u = index / (float)count;
            points[index] = PointOnCircle(center, radius, startDegrees + sweepDegrees * u);
        }

        return points;
    }

    private static IReadOnlyList<SKPoint> QuadraticPoints(SKPoint start, SKPoint control, SKPoint end, int steps)
    {
        var count = Math.Max(2, steps);
        var points = new SKPoint[count + 1];
        for (var index = 0; index <= count; index++)
        {
            var u = index / (float)count;
            var oneMinus = 1f - u;
            points[index] = new SKPoint(
                oneMinus * oneMinus * start.X + 2f * oneMinus * u * control.X + u * u * end.X,
                oneMinus * oneMinus * start.Y + 2f * oneMinus * u * control.Y + u * u * end.Y);
        }

        return points;
    }

    private static IReadOnlyList<SKPoint> CubicPoints(SKPoint start, SKPoint first, SKPoint second, SKPoint end, int steps)
    {
        var count = Math.Max(2, steps);
        var points = new SKPoint[count + 1];
        for (var index = 0; index <= count; index++)
        {
            var u = index / (float)count;
            var oneMinus = 1f - u;
            points[index] = new SKPoint(
                oneMinus * oneMinus * oneMinus * start.X +
                3f * oneMinus * oneMinus * u * first.X +
                3f * oneMinus * u * u * second.X +
                u * u * u * end.X,
                oneMinus * oneMinus * oneMinus * start.Y +
                3f * oneMinus * oneMinus * u * first.Y +
                3f * oneMinus * u * u * second.Y +
                u * u * u * end.Y);
        }

        return points;
    }

    private static IReadOnlyList<SKPoint> CubicFromTangents(
        SKPoint start,
        SKPoint end,
        SKPoint startTangent,
        SKPoint endTangent,
        float handle,
        int steps)
    {
        var first = Add(start, Scale(Unit(startTangent), handle));
        var second = Add(end, Scale(Unit(endTangent), -handle));
        return CubicPoints(start, first, second, end, steps);
    }

    private static IReadOnlyList<SKPoint> InvolutePoints(SKPoint center, float radius, float startRadians, float unwindRadians, int steps)
    {
        var count = Math.Max(3, steps);
        var points = new SKPoint[count + 1];
        for (var index = 0; index <= count; index++)
        {
            var theta = unwindRadians * index / count;
            var angle = startRadians + theta;
            points[index] = new SKPoint(
                center.X + radius * (MathF.Cos(angle) + theta * MathF.Sin(angle)),
                center.Y + radius * (MathF.Sin(angle) - theta * MathF.Cos(angle)));
        }

        return points;
    }

    private static SKPoint PointOnCircle(SKPoint center, float radius, float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        return PointOnCircleRadians(center, radius, radians);
    }

    private static SKPoint PointOnCircleRadians(SKPoint center, float radius, float radians) =>
        new(
            center.X + MathF.Cos(radians) * radius,
            center.Y + MathF.Sin(radians) * radius);

    private static SKPoint TangentOnCircle(SKPoint center, SKPoint point, bool clockwise)
    {
        var radial = Unit(new SKPoint(point.X - center.X, point.Y - center.Y));
        return clockwise
            ? new SKPoint(radial.Y, -radial.X)
            : new SKPoint(-radial.Y, radial.X);
    }

    private static SKPoint Add(SKPoint point, SKPoint vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    private static SKPoint Scale(SKPoint vector, float scale) =>
        new(vector.X * scale, vector.Y * scale);

    private static SKPoint Unit(SKPoint vector)
    {
        var length = MathF.Sqrt(vector.X * vector.X + vector.Y * vector.Y);
        return length <= float.Epsilon
            ? new SKPoint(1f, 0f)
            : new SKPoint(vector.X / length, vector.Y / length);
    }

    private static SKPoint Unit(SKPoint start, SKPoint end) =>
        Unit(new SKPoint(end.X - start.X, end.Y - start.Y));

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
        var treeStemLine = GraphicStructure.FrameSegment(treeRoot, treeJunction);
        var treeBall = Circle(treeBallCenter.X, treeBallCenter.Y, 17);
        var treeLineBall = Chain(treeTrunkLine, treeBall);
        var treeYCenterline = Chain(
            treeStemLine,
            GraphicStructure.FrameSegment(treeJunction, treeLeftTip),
            GraphicStructure.FrameSegment(treeJunction, treeRightTip));
        var treeBranches = Chain(
            treeYCenterline,
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
        var treeCurvedWoodCenterline = Chain(treeStemLine, treeLeftCurve, treeRightCurve);
        var treeCurvedBranches = Chain(treeCurvedWoodCenterline, treeBall);
        var woodBaseLeft = P(-113, -30);
        var woodBaseRight = P(-103, -30);
        var woodTrunkRight = P(-101, 3);
        var woodRightLower = P(-82, 22);
        var woodRightUpper = P(-88, 28);
        var woodRightCrotch = P(-106, 12);
        var woodCenterCrotch = P(-110, 12);
        var woodLeftUpper = P(-130, 28);
        var woodLeftLower = P(-132, 20);
        var woodTrunkLeft = P(-113, 4);
        var treeWoodDraftOutline = ClosedCubicOutline(
            (woodBaseLeft, P(-110, -30), P(-106, -30), woodBaseRight),
            (woodBaseRight, P(-103, -18), P(-101, -5), woodTrunkRight),
            (woodTrunkRight, P(-101, 8), P(-90, 15), woodRightLower),
            (woodRightLower, P(-78, 24), P(-82, 30), woodRightUpper),
            (woodRightUpper, P(-95, 26), P(-101, 18), woodRightCrotch),
            (woodRightCrotch, P(-108, 11), P(-109, 11), woodCenterCrotch),
            (woodCenterCrotch, P(-111, 11), P(-123, 25), woodLeftUpper),
            (woodLeftUpper, P(-138, 28), P(-138, 21), woodLeftLower),
            (woodLeftLower, P(-126, 16), P(-113, 9), woodTrunkLeft),
            (woodTrunkLeft, P(-113, -5), P(-113, -20), woodBaseLeft));
        var treeWoodDraft = Chain(treeWoodDraftOutline, treeBall);
        var treeWoodSettled = Chain(treeWoodDraftOutline, treeBall);
        var treeWoodOnly = treeWoodDraftOutline;

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
        var treeOffsetEvidence = GraphicParameterSurface.Frame(
            new Carrier(),
            treeTopologyEvidence,
            GraphicOffsetSurface.RequestWithDistance(
                new Carrier(),
                treeCurvedWoodCenterline,
                Relation.Segment(new Carrier(), 0, 5)));
        var treeWoodDraftInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            treeTopologyTemplate,
            treeWoodDraft,
            treeOffsetEvidence,
            FocusSurface.Bind(treeRootSlot, treeRoot),
            FocusSurface.Bind(treeJunctionSlot, treeJunction),
            FocusSurface.Bind(treeLeftTipSlot, treeLeftTip),
            FocusSurface.Bind(treeRightTipSlot, treeRightTip),
            FocusSurface.Bind(treeCrownCenterSlot, treeBallCenter.Point));
        var treeWoodSettledInstance = GraphicParameterSurface.TemplateInstanceFromBindings(
            new Carrier(),
            treeTopologyTemplate,
            treeWoodSettled,
            treeOffsetEvidence,
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
            treeWoodOnly,
            leftLeaf.ApproxCircle,
            crownLeaf.ApproxCircle,
            rightLeaf.ApproxCircle);
        var treeFluffy = Chain(
            treeWoodOnly,
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
        var treeTrunkState = Chain(horizon, houseWithOverhang, treeWoodDraft, cloudOval, doorRectangle, windowRectangle);
        var doorArchState = Chain(horizon, houseWithOverhang, treeWoodDraft, cloudOval, archedDoor, windowRectangle);
        var treeBranchThickState = Chain(horizon, houseWithOverhang, treeWoodSettled, cloudOval, archedDoor, windowRectangle);
        var windowPlusState = Chain(horizon, houseWithOverhang, treeWoodOnly, cloudOval, archedDoor, windowPlus);
        var windowRefinedState = Chain(horizon, houseWithOverhang, treeWoodOnly, cloudOval, archedDoor, roundedWindowPlus);
        var treeRegionsState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOval, archedDoor, roundedWindowPlus);
        var cloudRefinedState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOutline, archedDoor, roundedWindowPlus);
        var chimneyBodyState = Chain(horizon, houseWithOverhang, treeLeafRegions, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody);
        var treeFluffyState = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyBody);
        var chimneyCapState = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyWithCap);
        var finalSketch = Chain(horizon, houseWithOverhang, treeFluffy, cloudOutline, archedDoor, roundedWindowPlus, chimneyWithCap, doorKnob);

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
        var treeTrunkStep = RefinedRung(windowRectState, treeCurvedWoodCenterline, treeCurvedBranches, Chain(horizon, houseWithOverhang, cloudOval, doorRectangle, windowRectangle), treeWoodDraft, treeTrunkState, treeWoodDraftInstance);
        var doorArchStep = RefinedRung(treeTrunkState, doorRectangle, doorRectangle, Chain(horizon, houseWithOverhang, cloudOval, treeWoodDraft, windowRectangle), archedDoor, doorArchState, archedDoorInstance);
        var treeBranchThickStep = RefinedRung(doorArchState, treeWoodDraft, treeWoodDraft, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, windowRectangle), treeWoodSettled, treeBranchThickState, treeWoodSettledInstance);
        var windowPlusStep = RefinedRung(treeBranchThickState, windowRectangle, windowRectangle, Chain(horizon, houseWithOverhang, cloudOval, treeWoodOnly, archedDoor), windowPlus, windowPlusState, windowPlusInstance);
        var windowRefineStep = RefinedRung(windowPlusState, windowRectangle, windowPlus, Chain(horizon, houseWithOverhang, cloudOval, treeWoodOnly, archedDoor), roundedWindowPlus, windowRefinedState, roundedWindowInstance);
        var treeRegionStep = RefinedRung(windowRefinedState, treeBall, treeWoodOnly, Chain(horizon, houseWithOverhang, cloudOval, archedDoor, roundedWindowPlus), treeLeafRegions, treeRegionsState, leafRegionEvidence);
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
            "Tree exterior outline",
            "Door arch",
            "Tree outline settles",
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
