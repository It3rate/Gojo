using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Render-boundary DTO for checking offset behavior. The GojoCAD source and
/// offset request remain relation structure; these points are Cartesian samples.
/// </summary>
public sealed record GojoCadOffsetStudy(IReadOnlyList<GojoCadOffsetSample> Samples);

/// <summary>One sampled offset example: source, interior side, exterior side.</summary>
public sealed record GojoCadOffsetSample(
    string Label,
    IReadOnlyList<SKPoint> Original,
    IReadOnlyList<SKPoint> Interior,
    IReadOnlyList<SKPoint> Exterior,
    bool Closed = false,
    IReadOnlyList<SKPoint>? Closure = null);

/// <summary>Small Cartesian helpers for offset visualization at the Skia boundary.</summary>
public static class GojoCadOffsetGeometry
{
    public static IReadOnlyList<SKPoint> OffsetNormalField(
        IReadOnlyList<SKPoint> points,
        float distance,
        bool closed = false)
    {
        if (points.Count == 0)
        {
            return [];
        }

        if (points.Count == 1)
        {
            return [new SKPoint(points[0].X, points[0].Y + distance)];
        }

        var offset = new SKPoint[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            var previous = index == 0
                ? closed ? points[^1] : points[0]
                : points[index - 1];
            var next = index == points.Count - 1
                ? closed ? points[0] : points[^1]
                : points[index + 1];
            var tangent = Unit(previous, next);
            var normal = new SKPoint(-tangent.Y, tangent.X);
            offset[index] = Add(points[index], Scale(normal, distance));
        }

        return offset;
    }

    public static IReadOnlyList<SKPoint> OffsetTowardWitness(
        IReadOnlyList<SKPoint> points,
        SKPoint witness,
        float distance,
        bool closed,
        out IReadOnlyList<SKPoint> opposite)
    {
        var positive = OffsetNormalField(points, distance, closed);
        var negative = OffsetNormalField(points, -distance, closed);
        var positiveDistance = AverageDistanceSquared(positive, witness);
        var negativeDistance = AverageDistanceSquared(negative, witness);
        if (positiveDistance <= negativeDistance)
        {
            opposite = negative;
            return positive;
        }

        opposite = positive;
        return negative;
    }

    public static SKRect BoundsOf(params IReadOnlyList<SKPoint>[] pointSets)
    {
        var any = false;
        var left = 0f;
        var top = 0f;
        var right = 0f;
        var bottom = 0f;
        foreach (var points in pointSets)
        {
            foreach (var point in points)
            {
                if (!any)
                {
                    left = right = point.X;
                    top = bottom = point.Y;
                    any = true;
                    continue;
                }

                left = MathF.Min(left, point.X);
                top = MathF.Min(top, point.Y);
                right = MathF.Max(right, point.X);
                bottom = MathF.Max(bottom, point.Y);
            }
        }

        if (!any)
        {
            return new SKRect(-1, -1, 1, 1);
        }

        if (MathF.Abs(right - left) < 0.001f)
        {
            left -= 1f;
            right += 1f;
        }

        if (MathF.Abs(bottom - top) < 0.001f)
        {
            top -= 1f;
            bottom += 1f;
        }

        return new SKRect(left, top, right, bottom);
    }

    private static float AverageDistanceSquared(IReadOnlyList<SKPoint> points, SKPoint witness)
    {
        if (points.Count == 0)
        {
            return float.PositiveInfinity;
        }

        var sum = 0f;
        foreach (var point in points)
        {
            var dx = point.X - witness.X;
            var dy = point.Y - witness.Y;
            sum += dx * dx + dy * dy;
        }

        return sum / points.Count;
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

    private static SKPoint Add(SKPoint point, SKPoint vector) =>
        new(point.X + vector.X, point.Y + vector.Y);

    private static SKPoint Scale(SKPoint vector, float scale) =>
        new(vector.X * scale, vector.Y * scale);
}

/// <summary>Draws offset samples as a grid using fixed diagnostic colors.</summary>
public sealed class GojoCadSkiaOffsetStudyRenderer
{
    private static readonly SKColor SourceColor = new(36, 91, 148, 255);
    private static readonly SKColor InteriorColor = new(194, 47, 45, 245);
    private static readonly SKColor ExteriorColor = new(46, 130, 73, 245);
    private static readonly SKColor ClosureColor = new(82, 88, 96, 105);
    private static readonly SKColor LabelColor = new(31, 37, 45, 230);
    private static readonly SKColor BorderColor = new(82, 88, 96, 42);

    public int Draw(SKCanvas canvas, GojoCadOffsetStudy study, SKRect viewport)
    {
        if (study.Samples.Count == 0)
        {
            return 0;
        }

        var columns = study.Samples.Count <= 4 ? 2 : 3;
        var rows = (int)Math.Ceiling(study.Samples.Count / (double)columns);
        var cellWidth = viewport.Width / columns;
        var cellHeight = viewport.Height / rows;
        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Color = LabelColor,
            TextSize = 12.5f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };
        using var borderPaint = Stroke(BorderColor, 1f);
        using var sourcePaint = Stroke(SourceColor, 3.2f);
        using var interiorPaint = Stroke(InteriorColor, 2.25f);
        using var exteriorPaint = Stroke(ExteriorColor, 2.25f);
        using var closurePaint = Stroke(ClosureColor, 1.4f);
        closurePaint.PathEffect = SKPathEffect.CreateDash([5f, 5f], 0f);

        for (var index = 0; index < study.Samples.Count; index++)
        {
            var row = index / columns;
            var column = index % columns;
            var cell = new SKRect(
                viewport.Left + column * cellWidth + 10f,
                viewport.Top + row * cellHeight + 8f,
                viewport.Left + (column + 1) * cellWidth - 10f,
                viewport.Top + (row + 1) * cellHeight - 10f);
            canvas.DrawRoundRect(cell, 7f, 7f, borderPaint);
            canvas.DrawText(study.Samples[index].Label, cell.Left + 10f, cell.Top + 20f, labelPaint);
            DrawSample(canvas, study.Samples[index], new SKRect(cell.Left + 14f, cell.Top + 32f, cell.Right - 14f, cell.Bottom - 14f), sourcePaint, interiorPaint, exteriorPaint, closurePaint);
        }

        DrawLegend(canvas, new SKPoint(viewport.Left, viewport.Top - 22f), sourcePaint, interiorPaint, exteriorPaint, labelPaint);
        return study.Samples.Count;
    }

    private static void DrawSample(
        SKCanvas canvas,
        GojoCadOffsetSample sample,
        SKRect cell,
        SKPaint sourcePaint,
        SKPaint interiorPaint,
        SKPaint exteriorPaint,
        SKPaint closurePaint)
    {
        var bounds = GojoCadOffsetGeometry.BoundsOf(
            sample.Original,
            sample.Interior,
            sample.Exterior,
            sample.Closure ?? []);
        var scale = MathF.Min(cell.Width / bounds.Width, cell.Height / bounds.Height) * 0.82f;
        var mappedOriginal = MapPoints(sample.Original, bounds, cell, scale);
        var mappedInterior = MapPoints(sample.Interior, bounds, cell, scale);
        var mappedExterior = MapPoints(sample.Exterior, bounds, cell, scale);
        var mappedClosure = MapPoints(sample.Closure ?? [], bounds, cell, scale);

        DrawPath(canvas, mappedClosure, closed: false, closurePaint);
        DrawPath(canvas, mappedExterior, sample.Closed, exteriorPaint);
        DrawPath(canvas, mappedInterior, sample.Closed, interiorPaint);
        DrawPath(canvas, mappedOriginal, sample.Closed, sourcePaint);
        DrawDirectionArrow(canvas, mappedOriginal, sample.Closed, sourcePaint);
    }

    private static IReadOnlyList<SKPoint> MapPoints(IReadOnlyList<SKPoint> points, SKRect bounds, SKRect cell, float scale) =>
        points
            .Select(point => new SKPoint(
                cell.MidX + (point.X - bounds.MidX) * scale,
                cell.MidY - (point.Y - bounds.MidY) * scale))
            .ToArray();

    private static void DrawPath(SKCanvas canvas, IReadOnlyList<SKPoint> points, bool closed, SKPaint paint)
    {
        if (points.Count == 0)
        {
            return;
        }

        using var path = new SKPath();
        path.MoveTo(points[0]);
        for (var index = 1; index < points.Count; index++)
        {
            path.LineTo(points[index]);
        }

        if (closed)
        {
            path.Close();
        }

        canvas.DrawPath(path, paint);
    }

    private static void DrawDirectionArrow(SKCanvas canvas, IReadOnlyList<SKPoint> points, bool closed, SKPaint paint)
    {
        if (points.Count < 2)
        {
            return;
        }

        var endIndex = closed ? Math.Max(1, points.Count * 3 / 4) : points.Count - 1;
        var startIndex = Math.Max(0, endIndex - 1);
        var start = points[startIndex];
        var end = points[endIndex];
        var unit = Unit(start, end);
        var normal = new SKPoint(-unit.Y, unit.X);
        var left = new SKPoint(end.X - unit.X * 8f + normal.X * 4f, end.Y - unit.Y * 8f + normal.Y * 4f);
        var right = new SKPoint(end.X - unit.X * 8f - normal.X * 4f, end.Y - unit.Y * 8f - normal.Y * 4f);
        canvas.DrawLine(end, left, paint);
        canvas.DrawLine(end, right, paint);
    }

    private static void DrawLegend(
        SKCanvas canvas,
        SKPoint origin,
        SKPaint sourcePaint,
        SKPaint interiorPaint,
        SKPaint exteriorPaint,
        SKPaint labelPaint)
    {
        var x = origin.X;
        DrawLegendItem(canvas, x, origin.Y, sourcePaint, labelPaint, "source");
        DrawLegendItem(canvas, x + 88f, origin.Y, interiorPaint, labelPaint, "interior");
        DrawLegendItem(canvas, x + 182f, origin.Y, exteriorPaint, labelPaint, "exterior");
    }

    private static void DrawLegendItem(SKCanvas canvas, float x, float y, SKPaint linePaint, SKPaint labelPaint, string label)
    {
        canvas.DrawLine(x, y - 4f, x + 28f, y - 4f, linePaint);
        canvas.DrawText(label, x + 35f, y, labelPaint);
    }

    private static SKPaint Stroke(SKColor color, float width) => new()
    {
        IsAntialias = true,
        Color = color,
        StrokeWidth = width,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        Style = SKPaintStyle.Stroke
    };

    private static SKPoint Unit(SKPoint start, SKPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        return length <= float.Epsilon
            ? new SKPoint(1, 0)
            : new SKPoint(dx / length, dy / length);
    }
}
