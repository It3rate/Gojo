using Gojo.Core;
using GojoCAD;

namespace Gojo.GearShapeLadder;

public static class GearShapeGeometry
{
    public const int StageCount = 6;
    public const int SingleCircleStage = 0;
    public const int TwoCircleStage = 1;
    public const int CoupledCircleStage = 2;
    public const int TriangleToothStage = 3;
    public const int FlatToothStage = 4;
    public const int InvoluteToothStage = 5;
    public const int TicksPerTooth = 8;
    public const int CoordinateResolution = 4;
    public const double Module = 8d * CoordinateResolution;
    public const double PressureAngleRadians = 20d * Math.PI / 180d;
    public const double BottomLandPitchFraction = 0.18d;
    public const double GearCenterY = -58d * CoordinateResolution;
    public const double DevelopedBaselineY = 205d * CoordinateResolution;
    public const double DevelopedLeft = -270d * CoordinateResolution;
    public const double DevelopedRight = 270d * CoordinateResolution;

    public static double PitchRadius(int teeth) => Module * teeth * 0.5d;

    public static int ShapeDetailForStage(int stage) =>
        Math.Clamp(stage - CoupledCircleStage, 0, 3);

    /// <summary>
    /// Rotation, expressed in perimeter ticks, that puts a driven tooth space
    /// at the left-hand pitch contact. Odd tooth counts already present a
    /// space at PI; even counts need half a tooth of phase.
    /// </summary>
    public static int DrivenMeshPhaseTicks(int teeth) =>
        teeth % 2 == 0 ? TicksPerTooth / 2 : 0;

    public static Relation PitchCircle(FrameBasis basis, double centerX, double centerY, double radius) =>
        GraphicStructure.FrameCircle(
            basis.Point(centerX, centerY),
            basis.Point(centerX + radius, centerY));

    public static Relation BoreCircle(FrameBasis basis, double centerX, double centerY, double pitchRadius) =>
        PitchCircle(
            basis,
            centerX,
            centerY,
            Math.Clamp(
                pitchRadius * 0.19d,
                12d * CoordinateResolution,
                24d * CoordinateResolution));

    public static Relation PhaseMarker(FrameBasis basis, double centerX, double centerY, double pitchRadius) =>
        GraphicStructure.FrameSegment(
            basis.Point(centerX, centerY),
            basis.Point(centerX + pitchRadius * 0.72d, centerY));

    public static Relation GearContour(
        FrameBasis basis,
        int teeth,
        double centerX,
        double centerY,
        int stage,
        Relation pitchCircle)
    {
        if (stage <= 0)
        {
            return pitchCircle;
        }

        var pitchRadius = PitchRadius(teeth);
        var tooth = ToothProfile(teeth, pitchRadius, stage);
        var points = new List<ModelPoint>(tooth.Count * teeth);
        var pitchAngle = Math.PI * 2d / teeth;
        for (var toothIndex = 0; toothIndex < teeth; toothIndex++)
        {
            var centerAngle = toothIndex * pitchAngle;
            foreach (var sample in tooth)
            {
                var angle = centerAngle + sample.Angle;
                AddDistinct(points, new ModelPoint(
                    centerX + Math.Cos(angle) * sample.Radius,
                    centerY + Math.Sin(angle) * sample.Radius));
            }
        }

        return ClosedPolyline(basis, points);
    }

    public static Relation DevelopedProfile(
        FrameBasis basis,
        int driverTeeth,
        int shapeDetail)
    {
        var pitchRadius = PitchRadius(driverTeeth);
        var tooth = ToothProfile(driverTeeth, pitchRadius, shapeDetail);
        var halfPitch = Math.PI / driverTeeth;
        var linearPitch = Math.PI * Module;
        var startTooth = (int)Math.Floor(DevelopedLeft / linearPitch) - 1;
        var endTooth = (int)Math.Ceiling(DevelopedRight / linearPitch) + 1;
        var rootRadius = tooth.Min(sample => sample.Radius);
        var points = new List<ModelPoint>();

        for (var repeat = startTooth; repeat <= endTooth; repeat++)
        {
            foreach (var sample in tooth)
            {
                var x = repeat * linearPitch + (sample.Angle + halfPitch) * pitchRadius;
                var y = DevelopedBaselineY - (sample.Radius - rootRadius);
                AddDistinct(points, new ModelPoint(x, y));
            }
        }

        var clipped = points
            .Where(point => point.X >= DevelopedLeft - linearPitch && point.X <= DevelopedRight + linearPitch)
            .ToArray();
        return OpenPolyline(basis, clipped);
    }

    public static Relation DevelopedBaseline(FrameBasis basis) =>
        GraphicStructure.FrameSegment(
            basis.Point(DevelopedLeft, DevelopedBaselineY),
            basis.Point(DevelopedRight, DevelopedBaselineY));

    public static int SegmentCount(Relation polyline)
    {
        return polyline.Right is Carrier holder &&
            polyline.TryReadOrderedElementsOn(holder, out var elements)
            ? elements.OfType<Relation>().Count()
            : 0;
    }

    private static IReadOnlyList<PolarPoint> ToothProfile(int teeth, double pitchRadius, int stage)
    {
        var halfPitch = Math.PI / teeth;
        var outerRadius = pitchRadius + Module;
        if (stage <= 0)
        {
            return
            [
                new PolarPoint(-halfPitch, pitchRadius),
                new PolarPoint(halfPitch, pitchRadius)
            ];
        }

        if (stage == 1)
        {
            var rootRadius = pitchRadius - Module;
            return
            [
                new PolarPoint(-halfPitch, rootRadius),
                new PolarPoint(0d, outerRadius),
                new PolarPoint(halfPitch, rootRadius)
            ];
        }

        if (stage == 2)
        {
            var rootRadius = pitchRadius - Module;
            var topHalfAngle = halfPitch * 0.27d;
            return
            [
                new PolarPoint(-halfPitch, rootRadius),
                new PolarPoint(-topHalfAngle, outerRadius),
                new PolarPoint(topHalfAngle, outerRadius),
                new PolarPoint(halfPitch, rootRadius)
            ];
        }

        return InvoluteToothProfile(teeth, pitchRadius, outerRadius);
    }

    private static IReadOnlyList<PolarPoint> InvoluteToothProfile(
        int teeth,
        double pitchRadius,
        double outerRadius)
    {
        const int samplesPerFlank = 12;
        const int samplesPerFillet = 7;
        const int samplesPerHalfBottomLand = 5;
        var halfPitch = Math.PI / teeth;
        var rootRadius = Math.Max(Module, pitchRadius - Module * 1.25d);
        var baseRadius = pitchRadius * Math.Cos(PressureAngleRadians);
        var pitchParameter = InvoluteParameter(baseRadius, pitchRadius);
        var outerParameter = InvoluteParameter(baseRadius, outerRadius);
        var pitchPolarAngle = InvolutePolarAngle(pitchParameter);
        var halfToothAtPitch = Math.PI / (2d * teeth);
        var leftBaseAngle = -halfToothAtPitch - pitchPolarAngle;
        var leftRootJoinAngle = -halfPitch + halfPitch * BottomLandPitchFraction;
        var leftHalf = new List<PolarPoint>(
            samplesPerHalfBottomLand + samplesPerFillet + samplesPerFlank + 2);

        // The involute is only defined above the base circle. A generated gear
        // therefore has a distinct bottom land and root fillet below it. Keep
        // both as explicit vector participation instead of joining the two
        // involute flanks at a single, pointed root vertex.
        for (var index = 0; index <= samplesPerHalfBottomLand; index++)
        {
            var amount = index / (double)samplesPerHalfBottomLand;
            leftHalf.Add(new PolarPoint(
                Lerp(-halfPitch, leftRootJoinAngle, amount),
                rootRadius));
        }

        var angleSpan = leftBaseAngle - leftRootJoinAngle;
        var radiusSpan = baseRadius - rootRadius;
        for (var index = 1; index <= samplesPerFillet; index++)
        {
            var amount = index / (double)samplesPerFillet;
            leftHalf.Add(new PolarPoint(
                CubicBezier(
                    leftRootJoinAngle,
                    leftRootJoinAngle + angleSpan * 0.55d,
                    leftBaseAngle,
                    leftBaseAngle,
                    amount),
                CubicBezier(
                    rootRadius,
                    rootRadius,
                    baseRadius - radiusSpan * 0.45d,
                    baseRadius,
                    amount)));
        }

        for (var index = 1; index <= samplesPerFlank; index++)
        {
            var t = outerParameter * index / samplesPerFlank;
            leftHalf.Add(new PolarPoint(
                leftBaseAngle + InvolutePolarAngle(t),
                baseRadius * Math.Sqrt(1d + t * t)));
        }

        var points = new List<PolarPoint>(leftHalf.Count * 2);
        points.AddRange(leftHalf);
        for (var index = leftHalf.Count - 1; index >= 0; index--)
        {
            var sample = leftHalf[index];
            points.Add(new PolarPoint(-sample.Angle, sample.Radius));
        }

        return points;
    }

    private static double Lerp(double from, double to, double amount) =>
        from + (to - from) * amount;

    private static double CubicBezier(double start, double control1, double control2, double end, double amount)
    {
        var inverse = 1d - amount;
        return inverse * inverse * inverse * start +
            3d * inverse * inverse * amount * control1 +
            3d * inverse * amount * amount * control2 +
            amount * amount * amount * end;
    }

    private static double InvoluteParameter(double baseRadius, double radius) =>
        Math.Sqrt(Math.Max(0d, radius * radius / (baseRadius * baseRadius) - 1d));

    private static double InvolutePolarAngle(double parameter) =>
        parameter - Math.Atan(parameter);

    private static Relation ClosedPolyline(FrameBasis basis, IReadOnlyList<ModelPoint> input)
    {
        var points = DistinctRounded(input);
        if (points.Count < 3)
        {
            return OpenPolyline(basis, points);
        }

        var sites = points.Select(point => basis.Point(point.X, point.Y)).ToArray();
        var segments = new IElement[sites.Length];
        for (var index = 0; index < sites.Length; index++)
        {
            segments[index] = GraphicStructure.FrameSegment(sites[index], sites[(index + 1) % sites.Length]);
        }

        return Relation.OrderedGroup(new Carrier(), segments);
    }

    private static Relation OpenPolyline(FrameBasis basis, IReadOnlyList<ModelPoint> input)
    {
        var points = DistinctRounded(input);
        if (points.Count < 2)
        {
            return Relation.OrderedGroup(new Carrier());
        }

        var sites = points.Select(point => basis.Point(point.X, point.Y)).ToArray();
        var segments = new IElement[sites.Length - 1];
        for (var index = 0; index + 1 < sites.Length; index++)
        {
            segments[index] = GraphicStructure.FrameSegment(sites[index], sites[index + 1]);
        }

        return Relation.OrderedGroup(new Carrier(), segments);
    }

    private static IReadOnlyList<ModelPoint> DistinctRounded(IReadOnlyList<ModelPoint> input)
    {
        var points = new List<ModelPoint>(input.Count);
        foreach (var point in input)
        {
            AddDistinct(points, new ModelPoint(Math.Round(point.X), Math.Round(point.Y)));
        }

        if (points.Count > 1 && points[0] == points[^1])
        {
            points.RemoveAt(points.Count - 1);
        }

        return points;
    }

    private static void AddDistinct(List<ModelPoint> points, ModelPoint point)
    {
        if (points.Count == 0 ||
            Math.Round(points[^1].X) != Math.Round(point.X) ||
            Math.Round(points[^1].Y) != Math.Round(point.Y))
        {
            points.Add(point);
        }
    }

    private sealed record PolarPoint(double Angle, double Radius);

    private readonly record struct ModelPoint(double X, double Y);

    public sealed class FrameBasis
    {
        private readonly Carrier frame = new();
        private readonly Carrier x = new();
        private readonly Carrier y = new();

        public Relation Point(double px, double py) =>
            GraphicStructure.FramePoint(
                frame,
                new CarrierLocation(x, checked((long)Math.Round(px))),
                new CarrierLocation(y, checked((long)Math.Round(py))));
    }
}
