using Gojo.Core;
using GojoCAD;
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

        var graph = GraphicReadGraph.From(element);
        return DrawReadGraph(canvas, graph, projection, paints, hitAreas);
    }

    private static int DrawReadGraph(
        SKCanvas canvas,
        GraphicReadGraph graph,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var drawn = 0;
        var pointRoles = new HashSet<LocationKey>();
        var angleControlledPathSegments = ReadAngleControlledPathSegmentSources(graph);

        foreach (var radial in graph.ReadsOf<RadialSeedRead>())
        {
            DrawRadialFlow(canvas, radial, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var sheet in graph.ReadsOf<SheetSeedRead>())
        {
            DrawSheetSeed(canvas, sheet, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var line in graph.ReadsOf<LineSeedRead>())
        {
            DrawLineSeed(canvas, line, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var attachment in graph.ReadsOf<RadiusAttachmentRead>())
        {
            DrawRadiusAttachment(canvas, attachment, projection, paints, hitAreas);
            drawn++;
        }

        drawn += DrawCircumferences(canvas, graph, projection, paints, hitAreas, pointRoles);

        foreach (var span in graph.ReadsOf<CircularSpanRead>())
        {
            DrawCircumferenceSpan(canvas, span, projection, paints, hitAreas, pointRoles);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<SharedFrameSegmentRead>())
        {
            DrawSharedFrameSegment(canvas, segment, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<PositiveSegmentRead>()
            .Where(segment => !IsCircularCarrier(segment.Start.Location.Carrier)))
        {
            var partitions = graph.ReadsOf<SegmentPartitionRead>()
                .Where(partition => ReferenceEquals(partition.Segment, segment))
                .Select(partition => partition.Point);
            DrawPositiveSegment(
                canvas,
                segment,
                partitions,
                IsAngleControlledPathSegment(segment, angleControlledPathSegments),
                projection,
                paints,
                hitAreas,
                pointRoles);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<InverseSegmentRead>())
        {
            DrawInverseSegment(canvas, segment, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<DirectedSegmentRead>())
        {
            DrawDirectedSegment(canvas, segment, projection, paints, hitAreas);
            drawn++;
        }

        foreach (var segment in graph.ReadsOf<PositiveSegmentRead>()
            .Where(segment => IsAngleControlledPathSegment(segment, angleControlledPathSegments)))
        {
            DrawAngleControlledSegmentOverlay(canvas, segment, projection, paints);
            drawn++;
        }

        foreach (var point in graph.ReadsOf<PointRead>().Where(point => !point.Location.Carrier.IsUnknown))
        {
            var key = LocationKey.Of(point);
            if (pointRoles.Contains(key))
            {
                continue;
            }

            if (IsCircularCarrier(point.Location.Carrier))
            {
                var circle = ReadCircleProjection(point.Location, projection);
                DrawCircumferenceTick(canvas, circle.Center, point.Location, projection, paints, hitAreas, "circumference tick", priority: 5, point.Source);
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
        GraphicReadGraph graph,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var drawn = 0;
        var circularLocations = graph.ReadsOf<CircularMarkerRead>()
            .Select(marker => new LocatedSource(marker.Point.Location, marker.Source))
            .Concat(graph.ReadsOf<CircularSpanRead>().SelectMany(span => new[]
            {
                new LocatedSource(span.Segment.Start.Location, span.Source),
                new LocatedSource(span.Segment.End.Location, span.Source)
            }))
            .Where(entry => IsCircularCarrier(entry.Location.Carrier))
            .GroupBy(entry => entry.Location.Carrier.Id);

        foreach (var group in circularLocations)
        {
            var marker = group.First();
            var circle = ReadCircleProjection(marker.Location, projection);
            var paint = projection.UsesSampledCarrierShape(marker.Location.Carrier)
                ? paints.CircumferenceTension
                : paints.Circumference;

            canvas.DrawCircle(circle.Center, circle.Radius, paint);
            AddHit(hitAreas, CircleBounds(circle.Center, circle.Radius), "circumference", priority: 1, marker.Source);
            drawn++;

            var origin = ReadCircularOrigin(marker.Location.Carrier);
            DrawBasisRect(canvas, origin, projection, paints, hitAreas, "circumference basis origin", priority: 6, marker.Source);
            pointRoles.Add(LocationKey.Of(origin));

            var observedTicks = group.GroupBy(entry => LocationKey.Of(entry.Location))
                .Select(group => group.First())
                .ToArray();
            DrawCircumferenceTickLadder(
                canvas,
                circle.Center,
                marker.Location.Carrier,
                observedTicks.Select(entry => entry.Location),
                projection,
                paints);

            foreach (var entry in observedTicks)
            {
                DrawCircumferenceTick(canvas, circle.Center, entry.Location, projection, paints, hitAreas, "circumference tick", priority: 5, entry.Source);
                pointRoles.Add(LocationKey.Of(entry.Location));
                drawn++;
            }
        }

        return drawn;
    }

    private static void DrawCircumferenceSpan(
        SKCanvas canvas,
        CircularSpanRead span,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var start = span.Segment.Start.Location;
        var end = span.Segment.End.Location;
        var circle = ReadCircleProjection(start, projection);
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);

        var bounds = CircleBounds(circle.Center, circle.Radius);
        var startAngle = AngleDegrees(circle.Center, startPoint);
        var endAngle = AngleDegrees(circle.Center, endPoint);
        var sweep = ShortestSweep(startAngle, endAngle);
        var paint = projection.UsesSampledCarrierShape(start.Carrier) ||
            projection.UsesSampledProjection(start) ||
            projection.UsesSampledProjection(end)
                ? paints.CircumferenceSpanTension
                : paints.CircumferenceSpan;
        canvas.DrawArc(bounds, startAngle, sweep, false, paint);
        AddHit(hitAreas, bounds, "circumference span", priority: 3, span.Source);

        DrawCircumferenceTick(canvas, circle.Center, start, projection, paints, hitAreas, "circumference tick", priority: 5, span.Source);
        DrawCircumferenceTick(canvas, circle.Center, end, projection, paints, hitAreas, "circumference tick", priority: 5, span.Source);
        pointRoles.Add(LocationKey.Of(start));
        pointRoles.Add(LocationKey.Of(end));
    }

    private static void DrawDot(
        SKCanvas canvas,
        PointRead point,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var location = point.Location;
        var center = projection.Project(location);
        canvas.DrawCircle(center, paints.DotRadius, paints.Dot);
        AddHit(hitAreas, CenteredRect(center, paints.DotRadius * 2.4f), $"point {location.Carrier.Id}:{location.Ticks}", priority: 6, point.Source);
    }

    private static void DrawRadialFlow(
        SKCanvas canvas,
        RadialSeedRead radial,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var center = projection.Project(radial.Point.Location);
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
        AddHit(hitAreas, CircleBounds(center, paints.RadialRadius), "radial flow", priority: 0, radial.Source);
    }

    private static void DrawPositiveSegment(
        SKCanvas canvas,
        PositiveSegmentRead segment,
        IEnumerable<PointRead> points,
        bool isAngleControlledPathSegment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        HashSet<LocationKey> pointRoles)
    {
        var start = segment.Start.Location;
        var end = segment.End.Location;
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);
        var isSampled = projection.UsesSampledProjection(start) || projection.UsesSampledProjection(end);
        var paint = isAngleControlledPathSegment
            ? (isSampled ? paints.AngleControlledSegmentTension : paints.AngleControlledSegment)
            : (isSampled ? paints.SegmentTension : paints.Segment);

        canvas.DrawLine(startPoint, endPoint, paint);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 10f), "segment", priority: 2, segment.Source);

        foreach (var point in points)
        {
            DrawSegmentTick(canvas, point.Location, startPoint, endPoint, projection, paints, hitAreas, "segment tick", priority: 5, point.Source);
            pointRoles.Add(LocationKey.Of(point));
        }
    }

    private static void DrawLineSeed(
        SKCanvas canvas,
        LineSeedRead seed,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var location = seed.Span.Left.Location;
        var center = projection.Project(location);
        var before = projection.Project(new CarrierLocation(location.Carrier, location.Ticks - 1));
        var after = projection.Project(new CarrierLocation(location.Carrier, location.Ticks + 1));
        var normal = PerpendicularUnit(before, after);
        var half = paints.LineSeedLength * 0.5f;
        var start = new SKPoint(center.X - normal.X * half, center.Y - normal.Y * half);
        var end = new SKPoint(center.X + normal.X * half, center.Y + normal.Y * half);
        var paint = projection.UsesSampledProjection(location)
            ? paints.LineSeedTension
            : paints.LineSeed;

        canvas.DrawLine(start, end, paint);
        AddHit(hitAreas, LineBounds(start, end, 8f), "line seed", priority: 2, seed.Source);
    }

    private static void DrawAngleControlledSegmentOverlay(
        SKCanvas canvas,
        PositiveSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        var start = segment.Start.Location;
        var end = segment.End.Location;
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);
        var paint = projection.UsesSampledProjection(start) || projection.UsesSampledProjection(end)
            ? paints.AngleControlledSegmentTension
            : paints.AngleControlledSegment;
        canvas.DrawLine(startPoint, endPoint, paint);
    }

    private static HashSet<ulong> ReadAngleControlledPathSegmentSources(GraphicReadGraph graph)
    {
        var circularWitnesses = graph.ReadsOf<LinearBasisWitnessRead>()
            .Where(witness => witness.WitnessPoint.CircularMarker is not null)
            .ToArray();
        return [.. graph.ReadsOf<OpenSegmentPathRead>()
            .SelectMany(path => path.Segments)
            .Where(segment => circularWitnesses.Any(witness =>
                witness.LinearCarrier.SameIdentityAs(segment.Start.Location.Carrier)))
            .Select(segment => segment.Source)
            .OfType<Relation>()
            .Select(relation => relation.Id)];
    }

    private static bool IsAngleControlledPathSegment(
        PositiveSegmentRead segment,
        IReadOnlySet<ulong> angleControlledPathSegments) =>
        segment.Source is Relation relation &&
        angleControlledPathSegments.Contains(relation.Id);

    private static void DrawSheetSeed(
        SKCanvas canvas,
        SheetSeedRead seed,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.Project(seed.Span.Start.Location);
        var end = projection.Project(seed.Span.End.Location);
        var normal = PerpendicularUnit(start, end);
        var half = paints.SheetSeedWidth * 0.5f;
        using var path = new SKPath();
        path.MoveTo(start.X + normal.X * half, start.Y + normal.Y * half);
        path.LineTo(end.X + normal.X * half, end.Y + normal.Y * half);
        path.LineTo(end.X - normal.X * half, end.Y - normal.Y * half);
        path.LineTo(start.X - normal.X * half, start.Y - normal.Y * half);
        path.Close();
        var edgePaint = projection.UsesSampledProjection(seed.Span.Start.Location) ||
            projection.UsesSampledProjection(seed.Span.End.Location)
                ? paints.SheetSeedEdgeTension
                : paints.SheetSeedEdge;

        canvas.DrawPath(path, paints.SheetSeed);
        canvas.DrawPath(path, edgePaint);
        AddHit(hitAreas, path.Bounds, "sheet seed", priority: 1, seed.Source);
    }

    private static void DrawRadiusAttachment(
        SKCanvas canvas,
        RadiusAttachmentRead attachment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.Project(attachment.BasePoint.Location);
        var end = projection.Project(attachment.Marker.Point.Location);
        var paint = projection.UsesSampledProjection(attachment.BasePoint.Location) ||
            projection.UsesSampledProjection(attachment.Marker.Point.Location)
                ? paints.RadiusAttachmentTension
                : paints.RadiusAttachment;
        canvas.DrawLine(start, end, paint);
        AddHit(hitAreas, LineBounds(start, end, 7f), "radius attachment", priority: 2, attachment.Source);
    }

    private static void DrawInverseSegment(
        SKCanvas canvas,
        InverseSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = segment.Start.Location;
        var end = segment.End.Location;
        var startPoint = projection.Project(start);
        var endPoint = projection.Project(end);
        var paint = projection.UsesSampledProjection(start) || projection.UsesSampledProjection(end)
            ? paints.SegmentTension
            : paints.Segment;

        canvas.DrawLine(startPoint, endPoint, paint);
        DrawEndpointBar(canvas, startPoint, endPoint, paints);
        DrawEndpointBar(canvas, endPoint, startPoint, paints);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 10f), "inverse segment", priority: 2, segment.Source);
    }

    private static void DrawDirectedSegment(
        SKCanvas canvas,
        DirectedSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var startPoint = projection.Project(segment.InverseStart.Location);
        var endPoint = projection.Project(segment.PositiveEnd.Location);
        var paint = projection.UsesSampledProjection(segment.InverseStart.Location) ||
            projection.UsesSampledProjection(segment.PositiveEnd.Location)
                ? paints.DirectedSegmentTension
                : paints.DirectedSegment;

        canvas.DrawLine(startPoint, endPoint, paint);
        DrawArrowTip(canvas, startPoint, endPoint, paints, paint);
        AddHit(hitAreas, LineBounds(startPoint, endPoint, 12f), "directed segment", priority: 4, segment.Source);
    }

    private static void DrawSharedFrameSegment(
        SKCanvas canvas,
        SharedFrameSegmentRead segment,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas)
    {
        var start = projection.ProjectFrame(segment.Start);
        var end = projection.ProjectFrame(segment.End);

        canvas.DrawLine(start, end, paints.SharedFrameSegment);
        canvas.DrawCircle(start, paints.DotRadius * 0.62f, paints.SharedFramePoint);
        canvas.DrawCircle(end, paints.DotRadius * 0.62f, paints.SharedFramePoint);
        AddHit(hitAreas, LineBounds(start, end, 9f), "shared-frame segment", priority: 3, segment.Source);
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
        int priority,
        IElement? source)
    {
        var point = projection.Project(location);
        var perpendicular = PerpendicularUnit(startPoint, endPoint);
        DrawTick(canvas, point, perpendicular, paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority, source);
    }

    private static void DrawCircumferenceTick(
        SKCanvas canvas,
        SKPoint center,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority,
        IElement? source)
    {
        var point = projection.Project(location);
        DrawTick(canvas, point, Unit(center, point), paints);
        var bounds = CenteredRect(point, paints.PartitionTickLength);
        AddHit(hitAreas, bounds, text, priority, source);
    }

    private static void DrawCircumferenceTickLadder(
        SKCanvas canvas,
        SKPoint center,
        Carrier carrier,
        IEnumerable<CarrierLocation> observedLocations,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        var period = ReadObservedCircumferencePeriod(observedLocations);
        if (period <= 0)
        {
            return;
        }

        var tickStep = Math.Max(1, (int)Math.Ceiling(period / 16d));
        for (var tick = 0; tick < period; tick += tickStep)
        {
            DrawCircumferenceTickMark(
                canvas,
                center,
                new CarrierLocation(carrier, tick),
                projection,
                paints);
        }
    }

    private static long ReadObservedCircumferencePeriod(IEnumerable<CarrierLocation> observedLocations)
    {
        var deltas = observedLocations
            .Select(location => Math.Abs(location.Ticks))
            .Where(delta => delta > 0)
            .Distinct()
            .Order()
            .ToArray();
        if (deltas.Length < 2)
        {
            return 0;
        }

        var candidate = deltas[^1];
        var previous = deltas[^2];
        return candidate >= previous * 2
            ? candidate
            : 0;
    }

    private static void DrawCircumferenceTickMark(
        SKCanvas canvas,
        SKPoint center,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints)
    {
        var point = projection.Project(location);
        DrawTick(canvas, point, Unit(center, point), paints);
    }

    private static void DrawBasisRect(
        SKCanvas canvas,
        CarrierLocation location,
        IGojoCadSkiaProjection projection,
        GojoCadSkiaPaints paints,
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        string text,
        int priority,
        IElement? source)
    {
        var point = projection.Project(location);
        var bounds = CenteredRect(point, paints.BasisSize);
        canvas.DrawRect(bounds, paints.Basis);
        AddHit(hitAreas, bounds, text, priority, source);
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
        GojoCadSkiaPaints paints,
        SKPaint paint)
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

        canvas.DrawLine(end, left, paint);
        canvas.DrawLine(end, right, paint);
    }

    private static void AddHit(
        ICollection<GojoCadSkiaHitArea>? hitAreas,
        SKRect bounds,
        string text,
        int priority,
        IElement? source = null)
    {
        hitAreas?.Add(new GojoCadSkiaHitArea(bounds, text, priority, source));
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

    private readonly record struct LocatedSource(CarrierLocation Location, IElement Source);

    private readonly record struct CircleProjection(SKPoint Center, float Radius);

    private readonly record struct LocationKey(ulong CarrierId, ulong TickRefId, long Ticks)
    {
        public static LocationKey Of(CarrierLocation location) =>
            new(location.Carrier.Id, location.Tick?.Id ?? 0, location.Ticks);

        public static LocationKey Of(PointRead point) => Of(point.Location);
    }
}
