using Gojo.Core;
using GojoCAD;
using SkiaSharp;
using System.Runtime.CompilerServices;

namespace Gojo.GearShapeLadder;

public static class GearShapeRenderer
{
    private static readonly ConditionalWeakTable<GearShapeLadderModel, RenderProjection> ProjectionCache = new();
    private static readonly SKColor Background = new(248, 247, 243);
    private static readonly SKColor Ink = new(54, 62, 70);
    private static readonly SKColor MutedInk = new(111, 119, 126);
    private static readonly SKColor DriverFill = new(112, 139, 164, 178);
    private static readonly SKColor DrivenFill = new(181, 157, 112, 165);
    private static readonly SKColor ProfileFill = new(112, 139, 164, 42);
    private static readonly string[] StageNames =
    [
        "One sized circle",
        "Two sized circles",
        "Coupled pitch circles",
        "Triangle teeth",
        "Flat-tipped teeth",
        "Sampled involute vector teeth"
    ];

    public static void Draw(SKCanvas canvas, SKRect bounds, GearShapeLadderModel model, int selectedStage)
    {
        var stage = Math.Clamp(selectedStage, 0, model.MaximumStage);
        var structure = ProjectionCache.GetValue(model, CreateProjection).Stages[stage];
        var motion = structure.HasMesh ? model.ReadMotion() : default;
        var driverAngle = structure.HasMesh ? TurnsToRadians(motion.DriverTicks, model.DriverTeeth) : 0d;
        var drivenAngle = structure.HasMesh ? TurnsToRadians(motion.DrivenTicks, model.DrivenTeeth) : 0d;
        var shapeDetail = GearShapeGeometry.ShapeDetailForStage(stage);

        canvas.Save();
        canvas.ClipRect(bounds);
        canvas.Clear(Background);

        using var title = TextPaint(22f, Ink, fakeBold: true);
        using var subtitle = TextPaint(13f, MutedInk);
        canvas.DrawText("Gear Shape Ladder", bounds.Left + 34f, bounds.Top + 38f, title);
        canvas.DrawText(
            $"Rung {stage + 1}/{GearShapeGeometry.StageCount} · {StageNames[stage]} · {StageSubtitle(stage)}",
            bounds.Left + 34f,
            bounds.Top + 62f,
            subtitle);

        var viewport = new SKRect(bounds.Left + 34f, bounds.Top + 82f, bounds.Right - 34f, bounds.Bottom - 26f);
        var transform = ModelTransform.Fit(viewport);
        if (structure.HasMesh)
        {
            DrawPitchContact(canvas, transform);
        }

        var driverLabel = stage < GearShapeGeometry.TriangleToothStage
            ? stage == GearShapeGeometry.SingleCircleStage
                ? $"size {model.DriverTeeth} circle"
                : $"size {model.DriverTeeth} left circle"
            : $"{model.DriverTeeth}T driver";
        DrawGear(
            canvas,
            structure.Driver,
            transform,
            shapeDetail,
            driverAngle,
            DriverFill,
            driverLabel,
            structure.HasMesh);

        if (structure.Driven is { } driven)
        {
            var drivenLabel = stage < GearShapeGeometry.TriangleToothStage
                ? $"size {model.DrivenTeeth} right circle"
                : $"{model.DrivenTeeth}T driven";
            DrawGear(
                canvas,
                driven,
                transform,
                shapeDetail,
                drivenAngle,
                DrivenFill,
                drivenLabel,
                structure.HasMesh);
        }

        if (structure.DevelopedProfile is { } developed)
        {
            DrawDevelopedProfile(canvas, developed, transform, model.DriverTeeth, shapeDetail);
        }

        canvas.Restore();
    }

    private static string StageSubtitle(int stage) => stage switch
    {
        GearShapeGeometry.SingleCircleStage => "choose its size",
        GearShapeGeometry.TwoCircleStage => "choose the second size",
        GearShapeGeometry.CoupledCircleStage => "choose the turning rate",
        _ => "both views read the same Gojo refinement"
    };

    private static void DrawPitchContact(SKCanvas canvas, ModelTransform transform)
    {
        using var guide = Stroke(new SKColor(118, 124, 130, 100), 1.2f);
        guide.PathEffect = SKPathEffect.CreateDash([4f, 5f], 0);
        var contact = transform.Map(new WorldPoint(0, GearShapeGeometry.GearCenterY));
        canvas.DrawLine(contact.X, contact.Y - 27f, contact.X, contact.Y + 27f, guide);
        using var dot = Fill(new SKColor(61, 75, 87, 185));
        canvas.DrawCircle(contact, 3.2f, dot);
    }

    private static void DrawGear(
        SKCanvas canvas,
        GearRenderRead gear,
        ModelTransform transform,
        int shapeDetail,
        double angle,
        SKColor fillColor,
        string displayLabel,
        bool showMechanicalEvidence)
    {
        var pitch = gear.PitchCircle;
        var effectiveAngle = angle + gear.MeshPhaseRadians;
        using var fill = Fill(fillColor);
        using var outline = Stroke(Ink, 2.1f);
        using var pitchPaint = Stroke(new SKColor(67, 78, 89, 118), 1.15f);
        pitchPaint.PathEffect = SKPathEffect.CreateDash([5f, 5f], 0);

        if (shapeDetail == 0)
        {
            var center = transform.Map(pitch.Center);
            canvas.DrawCircle(center, (float)(pitch.Radius * transform.Scale), fill);
            canvas.DrawCircle(center, (float)(pitch.Radius * transform.Scale), outline);
        }
        else
        {
            using var path = Path(gear.Contour, transform, pitch.Center, effectiveAngle, closed: true);
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, outline);
        }

        var pitchCenter = transform.Map(pitch.Center);
        if (shapeDetail > 0)
        {
            canvas.DrawCircle(pitchCenter, (float)(pitch.Radius * transform.Scale), pitchPaint);
        }

        if (showMechanicalEvidence && gear.PhaseMarker is { } phaseMarker)
        {
            using var spoke = Stroke(new SKColor(Ink.Red, Ink.Green, Ink.Blue, 172), 2f);
            using var markerPath = Path(phaseMarker, transform, pitch.Center, effectiveAngle, closed: false);
            canvas.DrawPath(markerPath, spoke);
        }

        if (showMechanicalEvidence && gear.BoreCircle is { } bore)
        {
            var boreCenter = transform.Map(bore.Center);
            using var hole = Fill(Background);
            canvas.DrawCircle(boreCenter, (float)(bore.Radius * transform.Scale), hole);
            canvas.DrawCircle(boreCenter, (float)(bore.Radius * transform.Scale), outline);
        }

        using var gearLabel = TextPaint(12.5f, Ink, fakeBold: true);
        using var gearRead = TextPaint(11.5f, MutedInk);
        var labelY = pitchCenter.Y + (float)((
            pitch.Radius +
            GearShapeGeometry.Module +
            25d * GearShapeGeometry.CoordinateResolution) * transform.Scale);
        canvas.DrawText(displayLabel, pitchCenter.X, labelY, gearLabel);
        canvas.DrawText("circular perimeter carrier", pitchCenter.X, labelY + 17f, gearRead);
    }

    private static void DrawDevelopedProfile(
        SKCanvas canvas,
        DevelopedRenderRead profile,
        ModelTransform transform,
        int driverTeeth,
        int stage)
    {
        var baseline = profile.Baseline;
        var developed = profile.Profile;
        using var baselinePaint = Stroke(new SKColor(111, 117, 123, 135), 1.2f);
        using var profilePaint = Stroke(Ink, 2.15f);
        using var shade = Fill(ProfileFill);
        using var baselinePath = Path(baseline, transform, null, 0, closed: false);
        canvas.DrawPath(baselinePath, baselinePaint);

        if (developed.Count > 1 && stage > 0)
        {
            var filledPoints = developed.ToList();
            filledPoints.Add(new WorldPoint(developed[^1].X, GearShapeGeometry.DevelopedBaselineY));
            filledPoints.Add(new WorldPoint(developed[0].X, GearShapeGeometry.DevelopedBaselineY));
            using var fillPath = Path(filledPoints, transform, null, 0, closed: true);
            canvas.DrawPath(fillPath, shade);
        }

        using var profilePath = Path(developed, transform, null, 0, closed: false);
        canvas.DrawPath(profilePath, profilePaint);

        var linearPitch = Math.PI * GearShapeGeometry.Module;
        using var repeatPaint = Stroke(new SKColor(111, 117, 123, 70), 1f);
        repeatPaint.PathEffect = SKPathEffect.CreateDash([2f, 4f], 0);
        for (var repeat = Math.Ceiling(GearShapeGeometry.DevelopedLeft / linearPitch);
             repeat <= Math.Floor(GearShapeGeometry.DevelopedRight / linearPitch);
             repeat++)
        {
            var x = repeat * linearPitch;
            var from = transform.Map(new WorldPoint(
                x,
                GearShapeGeometry.DevelopedBaselineY + 7d * GearShapeGeometry.CoordinateResolution));
            var to = transform.Map(new WorldPoint(
                x,
                GearShapeGeometry.DevelopedBaselineY - 23d * GearShapeGeometry.CoordinateResolution));
            canvas.DrawLine(from, to, repeatPaint);
        }

        using var heading = TextPaint(13f, Ink, fakeBold: true);
        using var note = TextPaint(11.5f, MutedInk);
        var left = transform.Map(new WorldPoint(GearShapeGeometry.DevelopedLeft, GearShapeGeometry.DevelopedBaselineY));
        canvas.DrawText(
            stage == 0 ? "Developed perimeter profile" : "Developed tooth profile",
            left.X,
            left.Y + 35f,
            heading);
        canvas.DrawText(
            stage == 0
                ? "straight pitch line"
                : $"one-direction repeat · same {driverTeeth}T driver profile",
            left.X,
            left.Y + 52f,
            note);
    }

    private static double TurnsToRadians(long ticks, int teeth) =>
        ticks * Math.PI * 2d / (teeth * GearShapeGeometry.TicksPerTooth);

    private static RenderProjection CreateProjection(GearShapeLadderModel model)
    {
        var stages = new StageRenderRead[model.Rungs.Count];
        for (var index = 0; index < stages.Length; index++)
        {
            var stage = model.ReadStage(index);
            stages[index] = new StageRenderRead(
                ReadCircularShape(stage.Driver),
                stage.Driven is null ? null : ReadCircularShape(stage.Driven),
                stage.DevelopedProfile is null
                    ? null
                    : new DevelopedRenderRead(
                        ReadPolyline(stage.DevelopedProfile.Profile),
                        ReadPolyline(stage.DevelopedProfile.Baseline)),
                stage.Mesh is not null);
        }

        return new RenderProjection(stages);
    }

    private static GearRenderRead ReadCircularShape(CircularShapeRead gear)
    {
        var meshPhaseRadians = 0d;
        if (gear.PhaseMarker is not null)
        {
            if (!GearShapeLadderSurface.TryReadCalibration(gear.Calibration, out var calibration))
            {
                throw new InvalidOperationException("Expected a readable gear calibration relation.");
            }

            meshPhaseRadians = calibration.MeshPhaseTicks * Math.PI * 2d /
                (calibration.ToothCount * calibration.TicksPerTooth);
        }

        return new(
            ReadCircle(gear.PitchCircle),
            ReadPolyline(gear.Contour),
            gear.BoreCircle is null ? null : ReadCircle(gear.BoreCircle),
            gear.PhaseMarker is null ? null : ReadPolyline(gear.PhaseMarker),
            meshPhaseRadians);
    }

    private static CircleGeometry ReadCircle(Relation relation)
    {
        var read = GraphicReadGraph.From(relation)
            .ReadsOf<SharedFrameCircleRead>()
            .FirstOrDefault(candidate => ReferenceEquals(candidate.Source, relation))
            ?? throw new InvalidOperationException("Expected a shared-frame circle relation.");
        var center = ReadPoint(read.Center);
        var edge = ReadPoint(read.RadiusPoint);
        return new CircleGeometry(center, Math.Sqrt(Math.Pow(edge.X - center.X, 2) + Math.Pow(edge.Y - center.Y, 2)));
    }

    private static IReadOnlyList<WorldPoint> ReadPolyline(Relation relation)
    {
        var points = new List<WorldPoint>();
        if (relation.Right is Carrier holder && relation.TryReadOrderedElementsOn(holder, out var elements))
        {
            foreach (var segmentRelation in elements.OfType<Relation>())
            {
                AppendSegment(points, segmentRelation);
            }
        }
        else
        {
            AppendSegment(points, relation);
        }

        return points;
    }

    private static void AppendSegment(List<WorldPoint> points, Relation relation)
    {
        var segment = GraphicReadGraph.From(relation)
            .ReadsOf<SharedFrameSegmentRead>()
            .FirstOrDefault(candidate => ReferenceEquals(candidate.Source, relation));
        if (segment is null)
        {
            return;
        }

        var start = ReadPoint(segment.Start);
        var end = ReadPoint(segment.End);
        if (points.Count == 0 || points[^1] != start)
        {
            points.Add(start);
        }

        points.Add(end);
    }

    private static WorldPoint ReadPoint(SharedFramePointRead point)
    {
        if (point.Coordinates.Count < 2)
        {
            throw new InvalidOperationException("Gear geometry requires a two-coordinate frame point.");
        }

        return new WorldPoint(point.Coordinates[0].Ticks, point.Coordinates[1].Ticks);
    }

    private static SKPath Path(
        IReadOnlyList<WorldPoint> points,
        ModelTransform transform,
        WorldPoint? rotationCenter,
        double radians,
        bool closed)
    {
        var path = new SKPath();
        for (var index = 0; index < points.Count; index++)
        {
            var world = rotationCenter is { } center ? Rotate(points[index], center, radians) : points[index];
            var point = transform.Map(world);
            if (index == 0)
            {
                path.MoveTo(point);
            }
            else
            {
                path.LineTo(point);
            }
        }

        if (closed)
        {
            path.Close();
        }

        return path;
    }

    private static WorldPoint Rotate(WorldPoint point, WorldPoint center, double radians)
    {
        var x = point.X - center.X;
        var y = point.Y - center.Y;
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new WorldPoint(
            center.X + x * cosine - y * sine,
            center.Y + x * sine + y * cosine);
    }

    private static SKPaint Fill(SKColor color) => new()
    {
        IsAntialias = true,
        Color = color,
        Style = SKPaintStyle.Fill
    };

    private static SKPaint Stroke(SKColor color, float width) => new()
    {
        IsAntialias = true,
        Color = color,
        StrokeWidth = width,
        StrokeJoin = SKStrokeJoin.Round,
        StrokeCap = SKStrokeCap.Round,
        Style = SKPaintStyle.Stroke
    };

    private static SKPaint TextPaint(float size, SKColor color, bool fakeBold = false) => new()
    {
        IsAntialias = true,
        Color = color,
        TextSize = size,
        FakeBoldText = fakeBold,
        TextAlign = SKTextAlign.Left
    };

    private readonly record struct CircleGeometry(WorldPoint Center, double Radius);

    private readonly record struct WorldPoint(double X, double Y);

    private sealed record GearRenderRead(
        CircleGeometry PitchCircle,
        IReadOnlyList<WorldPoint> Contour,
        CircleGeometry? BoreCircle,
        IReadOnlyList<WorldPoint>? PhaseMarker,
        double MeshPhaseRadians);

    private sealed record DevelopedRenderRead(
        IReadOnlyList<WorldPoint> Profile,
        IReadOnlyList<WorldPoint> Baseline);

    private sealed record StageRenderRead(
        GearRenderRead Driver,
        GearRenderRead? Driven,
        DevelopedRenderRead? DevelopedProfile,
        bool HasMesh);

    private sealed record RenderProjection(IReadOnlyList<StageRenderRead> Stages);

    private readonly record struct ModelTransform(double Scale, double OffsetX, double OffsetY)
    {
        private const double Left = -340d * GearShapeGeometry.CoordinateResolution;
        private const double Top = -235d * GearShapeGeometry.CoordinateResolution;
        private const double Right = 340d * GearShapeGeometry.CoordinateResolution;
        private const double Bottom = 265d * GearShapeGeometry.CoordinateResolution;

        public static ModelTransform Fit(SKRect viewport)
        {
            var scale = Math.Min(viewport.Width / (Right - Left), viewport.Height / (Bottom - Top));
            var width = (Right - Left) * scale;
            var height = (Bottom - Top) * scale;
            var offsetX = viewport.MidX - width * 0.5 - Left * scale;
            var offsetY = viewport.MidY - height * 0.5 - Top * scale;
            return new ModelTransform(scale, offsetX, offsetY);
        }

        public SKPoint Map(WorldPoint point) =>
            new((float)(OffsetX + point.X * Scale), (float)(OffsetY + point.Y * Scale));
    }
}
