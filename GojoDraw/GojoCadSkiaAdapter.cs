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

        if (TryReadRadialFlow(relation, out var radialPoint))
        {
            evidence.RadialFlows.Add(radialPoint);
            Collect(radialPoint, evidence);
            return;
        }

        if (TryReadDirectedSegment(relation, out var inverseStart, out var positiveEnd))
        {
            evidence.DirectedSegments.Add(new DirectedSegmentEvidence(inverseStart, positiveEnd));
            return;
        }

        if (TryReadPositiveSegment(relation, out var start, out var end))
        {
            evidence.PositiveSegments.Add(new SegmentEvidence(start, end));
            return;
        }

        if (TryReadPoint(relation, out var point))
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
            if (DrawRadialFlow(canvas, radialPoint, projection, paints, hitAreas))
            {
                drawn++;
            }
        }

        drawn += DrawCircumferences(canvas, evidence, projection, paints, hitAreas, pointRoles);

        foreach (var segment in evidence.PositiveSegments)
        {
            if (segment.Start.Carrier.TryReadCircularOrigin(out _))
            {
                if (DrawCircumferenceSpan(canvas, segment, projection, paints, hitAreas, pointRoles))
                {
                    drawn++;
                }
            }
            else if (DrawPositiveSegment(canvas, segment, evidence.Points, projection, paints, hitAreas, pointRoles))
            {
                drawn++;
            }
        }

        foreach (var segment in evidence.DirectedSegments)
        {
            if (DrawDirectedSegment(canvas, segment, projection, paints, hitAreas))
            {
                drawn++;
            }
        }

        foreach (var point in evidence.Points)
        {
            var key = LocationKey.Of(point);
            if (pointRoles.Contains(key))
            {
                continue;
            }

            if (point.Carrier.TryReadCircularOrigin(out _))
            {
                if (TryReadCircle(point, projection, out var center, out _) &&
                    DrawCircumferenceTick(canvas, center, point, projection, paints, hitAreas, "circumference tick", priority: 5))
                {
                    pointRoles.Add(key);
                    drawn++;
                }
            }
            else if (DrawDot(canvas, point, projection, paints, hitAreas))
            {
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
            .Where(location => location.Carrier.TryReadCircularOrigin(out _))
            .GroupBy(location => location.Carrier.Id);

        foreach (var group in circularLocations)
        {
            var marker = group.First();
            if (!TryReadCircle(marker, projection, out var center, out var radius))
            {
                continue;
            }

            canvas.DrawCircle(center, radius, paints.Circumference);
            AddHit(hitAreas, CircleBounds(center, radius), "circumference", priority: 1);
            drawn++;

            if (marker.Carrier.TryReadCircularOrigin(out var origin))
            {
                DrawBasisRect(canvas, origin, projection, paints, hitAreas, "circumference basis origin", priority: 6);
                pointRoles.Add(LocationKey.Of(origin));
            }

            foreach (var location in group)
            {
                if (DrawCircumferenceTick(canvas, center, location, projection, paints, hitAreas, "circumference tick", priority: 5))
                {
                    pointRoles.Add(LocationKey.Of(location));
                    drawn++;
                }
            }
        }

        return drawn;
    }

    private static bool DrawCircumferenceSpan(
        SKCanvas canvas,
        SegmentEvidence segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        if (!TryReadCircle(segment.Start, projection, out var center, out var radius) ||
            !projection.TryProject(segment.Start, out var startPoint) ||
            !projection.TryProject(segment.End, out var endPoint))
        {
            return false;
        }

        var bounds = CircleBounds(center, radius);
        var startAngle = AngleDegrees(center, startPoint);
        var endAngle = AngleDegrees(center, endPoint);
        var sweep = ShortestSweep(startAngle, endAngle);
        canvas.DrawArc(bounds, startAngle, sweep, false, paints.CircumferenceSpan);
        AddHit(hitAreas, bounds, "circumference span", priority: 3);

        DrawCircumferenceTick(canvas, center, segment.Start, projection, paints, hitAreas, "circumference tick", priority: 5);
        DrawCircumferenceTick(canvas, center, segment.End, projection, paints, hitAreas, "circumference tick", priority: 5);
        pointRoles.Add(LocationKey.Of(segment.Start));
        pointRoles.Add(LocationKey.Of(segment.End));
        return true;
    }

    private static bool DrawDot(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        if (!projection.TryProject(location, out var center))
        {
            return false;
        }

        canvas.DrawCircle(center, paints.DotRadius, paints.Dot);
        AddHit(hitAreas, CenteredRect(center, paints.DotRadius * 2.4f), $"point {location.Carrier.Id}:{location.Ticks}", priority: 6);
        return true;
    }

    private static bool DrawRadialFlow(
        SKCanvas canvas,
        Relation point,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        if (!TryReadPoint(point, out var location) ||
            !projection.TryProject(location, out var center))
        {
            return false;
        }

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
        return true;
    }

    private static bool DrawPositiveSegment(
        SKCanvas canvas,
        SegmentEvidence segment,
        IReadOnlyList<CarrierLocation> points,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        if (!projection.TryProject(segment.Start, out var startPoint) ||
            !projection.TryProject(segment.End, out var endPoint))
        {
            return false;
        }

        canvas.DrawLine(startPoint, endPoint, paints.Segment);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 10f), "segment", priority: 2);

        foreach (var point in points.Where(point => IsInteriorPointOnSegment(point, segment)))
        {
            if (DrawSegmentTick(canvas, point, startPoint, endPoint, projection, paints, hitAreas, "segment tick", priority: 5))
            {
                pointRoles.Add(LocationKey.Of(point));
            }
        }

        return true;
    }

    private static bool DrawDirectedSegment(
        SKCanvas canvas,
        DirectedSegmentEvidence segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        if (!projection.TryProject(segment.InverseStart, out var startPoint) ||
            !projection.TryProject(segment.PositiveEnd, out var endPoint))
        {
            return false;
        }

        canvas.DrawLine(startPoint, endPoint, paints.DirectedSegment);
        DrawArrowTip(canvas, startPoint, endPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 12f), "directed segment", priority: 4);
        return true;
    }

    private static bool DrawSegmentTick(
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
        if (!projection.TryProject(location, out var point))
        {
            return false;
        }

        var perpendicular = PerpendicularUnit(startPoint, endPoint);
        DrawTick(canvas, point, perpendicular, paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority);
        return true;
    }

    private static bool DrawCircumferenceTick(
        SKCanvas canvas,
        SKPoint center,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority)
    {
        if (!projection.TryProject(location, out var point))
        {
            return false;
        }

        DrawTick(canvas, point, Unit(center, point), paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority);
        return true;
    }

    private static bool DrawBasisRect(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority)
    {
        if (!projection.TryProject(location, out var point))
        {
            return false;
        }

        var bounds = CenteredRect(point, paints.BasisSize);
        canvas.DrawRect(bounds, paints.Basis);
        AddHit(hitAreas, bounds, text, priority);
        return true;
    }

    private static bool TryReadPoint(Relation relation, out CarrierLocation location)
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

    private static bool TryReadPositiveSegment(
        Relation relation,
        out CarrierLocation start,
        out CarrierLocation end)
    {
        if (relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            TryReadPoint(startPoint, out start) &&
            TryReadPoint(endPoint, out end) &&
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

    private static bool TryReadDirectedSegment(
        Relation relation,
        out CarrierLocation inverseStart,
        out CarrierLocation positiveEnd)
    {
        if (relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            TryReadPointOnAnyCarrier(startPoint, out inverseStart) &&
            TryReadPointOnAnyCarrier(endPoint, out positiveEnd) &&
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

    private static bool TryReadRadialFlow(Relation relation, out Relation point)
    {
        if (relation.Left is Relation sourcePoint &&
            TryReadPoint(sourcePoint, out _) &&
            relation.Right is Relation outlet &&
            TryReadUnknownZeroSegment(outlet))
        {
            point = sourcePoint;
            return true;
        }

        point = null!;
        return false;
    }

    private static bool TryReadUnknownZeroSegment(Relation relation)
    {
        return
            relation.Left is Relation startPoint &&
            relation.Right is Relation endPoint &&
            TryReadPointOnAnyCarrier(startPoint, out var start) &&
            TryReadPointOnAnyCarrier(endPoint, out var end) &&
            start.Carrier.IsUnknown &&
            end.Carrier.IsUnknown &&
            start.Ticks == 0 &&
            end.Ticks == 0;
    }

    private static bool TryReadPointOnAnyCarrier(Relation relation, out CarrierLocation location)
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

    private static bool TryReadCircle(
        CarrierLocation marker,
        IGojoCadSkiaProjection projection,
        out SKPoint center,
        out float radius)
    {
        if (marker.Carrier.TryReadCircularOrigin(out var origin) &&
            projection.TryProject(origin, out center) &&
            projection.TryProject(marker, out var markerPoint))
        {
            radius = Distance(center, markerPoint);
            return radius > 0.1f;
        }

        center = default;
        radius = 0;
        return false;
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

    private sealed class RenderEvidence
    {
        public List<CarrierLocation> Points { get; } = [];

        public List<SegmentEvidence> PositiveSegments { get; } = [];

        public List<DirectedSegmentEvidence> DirectedSegments { get; } = [];

        public List<Relation> RadialFlows { get; } = [];
    }

    private readonly record struct SegmentEvidence(CarrierLocation Start, CarrierLocation End);

    private readonly record struct DirectedSegmentEvidence(CarrierLocation InverseStart, CarrierLocation PositiveEnd);

    private readonly record struct LocationKey(ulong CarrierId, ulong TickRefId, long Ticks)
    {
        public static LocationKey Of(CarrierLocation location) =>
            new(location.Carrier.Id, location.Tick?.Id ?? 0, location.Ticks);
    }
}
