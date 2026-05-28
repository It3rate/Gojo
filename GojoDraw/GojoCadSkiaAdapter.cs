using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Minimal Skia adapter that traverses GojoCAD relation structure and renders
/// low-level physical forms it recognizes.
/// </summary>
public sealed class GojoCadSkiaAdapter
{
    public int DrawElement(
        SKCanvas canvas,
        IElement element,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(paints);

        var evidence = new RenderEvidence();
        Collect(element, evidence);
        return DrawEvidence(canvas, evidence, projection, paints, hitAreas);
    }

    private static void Collect(IElement element, RenderEvidence evidence)
    {
        if (element is not Relation relation)
        {
            return;
        }

        if (RecognizeRadialFlow(relation, out var radialPoint))
        {
            evidence.RadialFlows.Add(radialPoint);
            Collect(radialPoint, evidence);
            return;
        }

        if (RecognizeDirectedSegment(relation, out var inverseStart, out var positiveEnd))
        {
            evidence.DirectedSegments.Add(new DirectedSegmentEvidence(inverseStart, positiveEnd));
            return;
        }

        if (RecognizePositiveSegment(relation, out var start, out var end))
        {
            evidence.PositiveSegments.Add(new SegmentEvidence(start, end));
            return;
        }

        if (RecognizePoint(relation, out var point))
        {
            evidence.Points.Add(point);
            return;
        }

        Collect(relation.Left, evidence);
        Collect(relation.Right, evidence);
    }

    private static int DrawEvidence(
        SKCanvas canvas,
        RenderEvidence evidence,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var drawn = 0;
        var pointRoles = new HashSet<LocationKey>();

        foreach (var radialPoint in evidence.RadialFlows)
        {
            DrawRadialFlow(canvas, radialPoint, projection, paints, hitAreas);
            drawn++;
        }

        drawn += DrawCircumferences(canvas, evidence, projection, paints, hitAreas, pointRoles);

        foreach (var segment in evidence.PositiveSegments)
        {
            if (IsCircularCarrier(segment.Start.Carrier))
            {
                DrawCircumferenceSpan(canvas, segment, projection, paints, hitAreas, pointRoles);
                drawn++;
            }
            else
            {
                DrawPositiveSegment(canvas, segment, evidence.Points, projection, paints, hitAreas, pointRoles);
                drawn++;
            }
        }

        foreach (var segment in evidence.DirectedSegments)
        {
            DrawDirectedSegment(canvas, segment, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var point in evidence.Points)
        {
            var key = LocationKey.Of(point);
            if (pointRoles.Contains(key))
            {
                continue;
            }

            if (IsCircularCarrier(point.Carrier))
            {
                var circle = ReadCircleProjection(point, projection);
                DrawCircumferenceTick(canvas, circle.Center, point, projection, paints, hitAreas, "circumference tick", priority: 5);
                pointRoles.Add(key);
                drawn++;
            }
            else
            {
                DrawDot(canvas, point, projection, paints, hitAreas);
                drawn++;
            }
        }

        return drawn;
    }

    private static int DrawCircumferences(
        SKCanvas canvas,
        RenderEvidence evidence,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var drawn = 0;
        var circularLocations = evidence.Points
            .Concat(evidence.PositiveSegments.SelectMany(segment => new[] { segment.Start, segment.End }))
            .Where(location => IsCircularCarrier(location.Carrier))
            .GroupBy(location => location.Carrier.Id);

        foreach (var group in circularLocations)
        {
            var marker = group.First();
            var circle = ReadCircleProjection(marker, projection);

            canvas.DrawCircle(circle.Center, circle.Radius, paints.Circumference);
            AddHit(hitAreas, CircleBounds(circle.Center, circle.Radius), "circumference", priority: 1);
            drawn++;

            var origin = ReadCircularOrigin(marker.Carrier);
            DrawBasisRect(canvas, origin, projection, paints, hitAreas, "circumference basis origin", priority: 6);
            pointRoles.Add(LocationKey.Of(origin));

            foreach (var location in group)
            {
                DrawCircumferenceTick(canvas, circle.Center, location, projection, paints, hitAreas, "circumference tick", priority: 5);
                pointRoles.Add(LocationKey.Of(location));
                drawn++;
            }
        }

        return drawn;
    }

    private static void DrawCircumferenceSpan(
        SKCanvas canvas,
        SegmentEvidence segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var circle = ReadCircleProjection(segment.Start, projection);
        var startPoint = projection.Project(segment.Start);
        var endPoint = projection.Project(segment.End);

        var bounds = CircleBounds(circle.Center, circle.Radius);
        var startAngle = AngleDegrees(circle.Center, startPoint);
        var endAngle = AngleDegrees(circle.Center, endPoint);
        var sweep = ShortestSweep(startAngle, endAngle);
        canvas.DrawArc(bounds, startAngle, sweep, false, paints.CircumferenceSpan);
        AddHit(hitAreas, bounds, "circumference span", priority: 3);

        DrawCircumferenceTick(canvas, circle.Center, segment.Start, projection, paints, hitAreas, "circumference tick", priority: 5);
        DrawCircumferenceTick(canvas, circle.Center, segment.End, projection, paints, hitAreas, "circumference tick", priority: 5);
        pointRoles.Add(LocationKey.Of(segment.Start));
        pointRoles.Add(LocationKey.Of(segment.End));
    }

    private static void DrawDot(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var center = projection.Project(location);
        canvas.DrawCircle(center, paints.DotRadius, paints.Dot);
        AddHit(hitAreas, CenteredRect(center, paints.DotRadius * 2.4f), $"point {location.Carrier.Id}:{location.Ticks}", priority: 6);
    }

    private static void DrawRadialFlow(
        SKCanvas canvas,
        Relation point,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        RecognizePoint(point, out var location);
        var center = projection.Project(location);
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                center,
                paints.RadialRadius,
                [paints.RadialInner, paints.RadialOuter],
                [0f, 1f],
                SKShaderTileMode.Clamp),
            Style = SKPaintStyle.Fill
        };
        canvas.DrawCircle(center, paints.RadialRadius, paint);
        AddHit(hitAreas, CircleBounds(center, paints.RadialRadius), "radial flow", priority: 0);
    }

    private static void DrawPositiveSegment(
        SKCanvas canvas,
        SegmentEvidence segment,
        IReadOnlyList<CarrierLocation> points,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var startPoint = projection.Project(segment.Start);
        var endPoint = projection.Project(segment.End);

        canvas.DrawLine(startPoint, endPoint, paints.Segment);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 10f), "segment", priority: 2);

        foreach (var point in points.Where(point => IsInteriorPointOnSegment(point, segment)))
        {
            DrawSegmentTick(canvas, point, startPoint, endPoint, projection, paints, hitAreas, "segment tick", priority: 5);
            pointRoles.Add(LocationKey.Of(point));
        }
    }

    private static void DrawDirectedSegment(
        SKCanvas canvas,
        DirectedSegmentEvidence segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var startPoint = projection.Project(segment.InverseStart);
        var endPoint = projection.Project(segment.PositiveEnd);

        canvas.DrawLine(startPoint, endPoint, paints.DirectedSegment);
        DrawArrowTip(canvas, startPoint, endPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 12f), "directed segment", priority: 4);
    }

    private static void DrawSegmentTick(
        SKCanvas canvas,
        CarrierLocation location,
        SKPoint startPoint,
        SKPoint endPoint,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority)
    {
        var point = projection.Project(location);
        var perpendicular = PerpendicularUnit(startPoint, endPoint);
        DrawTick(canvas, point, perpendicular, paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority);
    }

    private static void DrawCircumferenceTick(
        SKCanvas canvas,
        SKPoint center,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority)
    {
        var point = projection.Project(location);
        DrawTick(canvas, point, Unit(center, point), paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority);
    }

    private static void DrawBasisRect(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority)
    {
        var point = projection.Project(location);
        var bounds = CenteredRect(point, paints.BasisSize);
        canvas.DrawRect(bounds, paints.Basis);
        AddHit(hitAreas, bounds, text, priority);
    }

    private static bool RecognizePoint(Relation relation, out CarrierLocation location)
    {
        if (relation.Left is CarrierLocation candidate &&
            relation.Right is Carrier carrier &&
            candidate.Carrier.SameIdentityAs(carrier) &&
            !candidate.Carrier.IsUnknown)
        {
            location = candidate;
            return true;
        }

        location = default;
        return false;
    }

    private static bool RecognizePositiveSegment(
        Relation relation,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            RecognizePoint(startPoint, out start) &&
            RecognizePoint(endPoint, out end) &&
            start.Carrier.SameIdentityAs(end.Carrier) &&
            !start.Carrier.IsInverse &&
            !end.Carrier.IsInverse)
        {
            return true;
        }

        start = default;
        end = default;
        return false;
    }

    private static bool RecognizeDirectedSegment(
        Relation relation,
        out CarrierLocation inverseStart,
        out CarrierLocation positiveEnd)
    {
        if (relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            RecognizePointOnAnyCarrier(startPoint, out inverseStart) &&
            RecognizePointOnAnyCarrier(endPoint, out positiveEnd) &&
            inverseStart.Carrier.IsInverse &&
            !positiveEnd.Carrier.IsInverse &&
            inverseStart.Carrier.Inverse().SameIdentityAs(positiveEnd.Carrier))
        {
            return true;
        }

        inverseStart = default;
        positiveEnd = default;
        return false;
    }

    private static bool RecognizeRadialFlow(Relation relation, out Relation point)
    {
        if (relation.Left is Relation sourcePoint &&
            RecognizePoint(sourcePoint, out _) &&
            relation.Right is Relation outlet &&
            IsUnknownZeroSegment(outlet))
        {
            point = sourcePoint;
            return true;
        }

        point = null!;
        return false;
    }

    private static bool IsUnknownZeroSegment(Relation relation)
    {
        return
            relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            RecognizePointOnAnyCarrier(startPoint, out var start) &&
            RecognizePointOnAnyCarrier(endPoint, out var end) &&
            start.Carrier.IsUnknown &&
            end.Carrier.IsUnknown &&
            start.Ticks == 0 &&
            end.Ticks == 0;
    }

    private static bool RecognizePointOnAnyCarrier(Relation relation, out CarrierLocation location)
    {
        if (relation.Left is CarrierLocation candidate &&
            relation.Right is Carrier carrier &&
            candidate.Carrier.SameIdentityAs(carrier))
        {
            location = candidate;
            return true;
        }

        location = default;
        return false;
    }

    private static CircleProjection ReadCircleProjection(
        CarrierLocation marker,
        IGojoCadSkiaProjection projection)
    {
        var origin = ReadCircularOrigin(marker.Carrier);
        var center = projection.Project(origin);
        var markerPoint = projection.Project(marker);
        var radius = MathF.Max(0.1f, Distance(center, markerPoint));
        return new CircleProjection(center, radius);
    }

    private static bool IsInteriorPointOnSegment(CarrierLocation point, SegmentEvidence segment)
    {
        if (!point.Carrier.SameIdentityAs(segment.Start.Carrier))
        {
            return false;
        }

        var min = Math.Min(segment.Start.Ticks, segment.End.Ticks);
        var max = Math.Max(segment.Start.Ticks, segment.End.Ticks);
        return point.Ticks > min && point.Ticks < max;
    }

    private static void DrawEndpointBar(
        SKCanvas canvas,
        SKPoint point,
        SKPoint toward,
        GojoCadSkiaPaints paints)
    {
        var perpendicular = PerpendicularUnit(point, toward);
        var half = paints.EndpointBarLength * 0.5f;
        canvas.DrawLine(
            new SKPoint(point.X - perpendicular.X * half, point.Y - perpendicular.Y * half),
            new SKPoint(point.X + perpendicular.X * half, point.Y + perpendicular.Y * half),
            paints.EndpointBar);
    }

    private static void DrawTick(
        SKCanvas canvas,
        SKPoint center,
        SKPoint direction,
        GojoCadSkiaPaints paints)
    {
        var half = paints.PartitionTickLength * 0.5f;
        canvas.DrawLine(
            new SKPoint(center.X - direction.X * half, center.Y - direction.Y * half),
            new SKPoint(center.X + direction.X * half, center.Y + direction.Y * half),
            paints.Partition);
    }

    private static void DrawArrowTip(
        SKCanvas canvas,
        SKPoint start,
        SKPoint end,
        GojoCadSkiaPaints paints)
    {
        var direction = Unit(start, end);
        var perpendicular = new SKPoint(-direction.Y, direction.X);
        var back = new SKPoint(
            end.X - direction.X * paints.ArrowLength,
            end.Y - direction.Y * paints.ArrowLength);
        var left = new SKPoint(
            back.X + perpendicular.X * paints.ArrowWidth,
            back.Y + perpendicular.Y * paints.ArrowWidth);
        var right = new SKPoint(
            back.X - perpendicular.X * paints.ArrowWidth,
            back.Y - perpendicular.Y * paints.ArrowWidth);

        canvas.DrawLine(end, left, paints.DirectedSegment);
        canvas.DrawLine(end, right, paints.DirectedSegment);
    }

    private static void AddHit(
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        SKRect bounds,
        string text,
        int priority)
    {
        hitAreas?.Add(new GojoCadSkiaHitArea(bounds, text, priority));
    }

    private static SKRect CenteredRect(SKPoint center, float size)
    {
        var half = size * 0.5f;
        return new SKRect(center.X - half, center.Y - half, center.X + half, center.Y + half);
    }

    private static SKRect CircleBounds(SKPoint center, float radius) =>
        new(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

    private static SKRect LineBounds(SKPoint start, SKPoint end, float padding) => new(
        MathF.Min(start.X, end.X) - padding,
        MathF.Min(start.Y, end.Y) - padding,
        MathF.Max(start.X, end.X) + padding,
        MathF.Max(start.Y, end.Y) + padding);

    private static float AngleDegrees(SKPoint center, SKPoint point) =>
        MathF.Atan2(point.Y - center.Y, point.X - center.X) * 180f / MathF.PI;

    private static float ShortestSweep(float start, float end)
    {
        var sweep = end - start;
        while (sweep > 180f)
        {
            sweep -= 360f;
        }

        while (sweep < -180f)
        {
            sweep += 360f;
        }

        return sweep;
    }

    private static SKPoint PerpendicularUnit(SKPoint start, SKPoint end)
    {
        var unit = Unit(start, end);
        return new SKPoint(-unit.Y, unit.X);
    }

    private static SKPoint Unit(SKPoint start, SKPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        return length <= float.Epsilon
            ? new SKPoint(1, 0)
            : new SKPoint(dx / length, dy / length);
    }

    private static float Distance(SKPoint left, SKPoint right)
    {
        var dx = right.X - left.X;
        var dy = right.Y - left.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static bool IsCircularCarrier(Carrier carrier) => carrier.TryReadCircularOrigin(out _);

    private static CarrierLocation ReadCircularOrigin(Carrier carrier) =>
        carrier.TryReadCircularOrigin(out var origin)
            ? origin
            : new CarrierLocation(Carrier.Unknown, 0);

    private sealed class RenderEvidence
    {
        public List<CarrierLocation> Points { get; } = [];

        public List<SegmentEvidence> PositiveSegments { get; } = [];

        public List<DirectedSegmentEvidence> DirectedSegments { get; } = [];

        public List<Relation> RadialFlows { get; } = [];
    }

    private readonly record struct SegmentEvidence(CarrierLocation Start, CarrierLocation End);

    private readonly record struct DirectedSegmentEvidence(CarrierLocation InverseStart, CarrierLocation PositiveEnd);

    private readonly record struct CircleProjection(SKPoint Center, float Radius);

    private readonly record struct LocationKey(ulong CarrierId, ulong TickRefId, long Ticks)
    {
        public static LocationKey Of(CarrierLocation location) =>
            new(location.Carrier.Id, location.Tick?.Id ?? 0, location.Ticks);
    }
}
