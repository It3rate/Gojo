using SkiaSharp;

namespace Gojo.Experiments.PrimitiveRuntime.Viewer;

internal static class PrimitiveRenderer
{
    private static readonly SKColor Ink = SKColor.Parse("#21323D");
    private static readonly SKColor Muted = SKColor.Parse("#65747E");
    private static readonly SKColor LineColor = SKColor.Parse("#DEE5E7");
    private static readonly SKColor Teal = SKColor.Parse("#087F83");
    private static readonly SKColor Amber = SKColor.Parse("#B27723");
    private static readonly SKTypeface Regular = SKTypeface.FromFamilyName("Segoe UI");
    private static readonly SKTypeface Bold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
    private static readonly SKTypeface Mono = SKTypeface.FromFamilyName("Cascadia Mono");

    public static void Draw(SKCanvas canvas, float width, float height, InspectionView view)
    {
        canvas.Clear(SKColor.Parse("#F2F5F4"));
        canvas.Save();
        var scale = Math.Min(width / 1560f, height / 940f);
        canvas.Translate((width - 1560 * scale) / 2, (height - 940 * scale) / 2);
        canvas.Scale(scale);

        Text(canvas, "GOJO / PRIMITIVE LAB", 42, 43, 15, Teal, true);
        Text(canvas, "A connection, before numbers", 42, 88, 34, Ink, true);
        Text(canvas, "Connected sites and authored transitions, executed directly on compact state.", 43, 119, 17, Muted);
        Pill(canvas, 1290, 44, 225, view.Calibrated ? "CALIBRATED OBSERVER" : "PRE-NUMERIC", view.Calibrated ? Teal : Ink);

        Panel(canvas, new SKRect(40, 150, 1010, 650));
        Panel(canvas, new SKRect(1030, 150, 1520, 650));
        Panel(canvas, new SKRect(40, 670, 800, 900));
        Panel(canvas, new SKRect(820, 670, 1520, 900));

        Text(canvas, "LOCAL CONNECTION", 65, 184, 13, Muted, true);
        Text(canvas, view.Reversed ? "Observer follows the reversed order" : "Observer follows the exposed order", 65, 212, 19, Ink, true);
        Text(canvas, view.Calibrated ? "Landmark labels show native calibration ticks; the numeric read uses the observer frame." : "Schematic spacing is unmeasured. The landmarks have order, not distance.", 65, 238, 15, Muted);
        DrawConnection(canvas, view);

        Text(canvas, "CURRENT SUPPORT", 1055, 184, 13, Muted, true);
        Text(canvas, view.Open ? view.Mask == 0 ? "Open, no known alternatives" : "Known alternatives + open remainder" : view.Mask == 0 ? "Empty closed support" : view.Choices.Length == 1 ? "One supported state" : "Supported alternatives", 1055, 216, 22, view.Open || view.Mask == 0 ? Amber : Ink, true);
        Text(canvas, view.Phase, 1055, 246, 15, Muted);
        DrawBits(canvas, view);
        var stateY = 363f;
        foreach (var choice in view.Choices.Take(4))
        {
            Dot(canvas, 1064, stateY - 5, 5, Teal);
            Text(canvas, choice.Label, 1080, stateY, 18, Ink);
            stateY += 29;
        }
        if (view.Choices.Length > 4) Text(canvas, $"+ {view.Choices.Length - 4} more alternatives", 1080, stateY, 16, Muted);
        if (view.Choices.Length == 0) Text(canvas, view.Open ? "No known state has been supplied." : "No state fits the supplied support.", 1055, stateY, 17, Muted);
        if (view.Open) Text(canvas, "The open remainder is retained separately.", 1055, 505, 15, Amber);
        Text(canvas, "OBSERVER NUMERIC READ", 1055, 546, 13, Muted, true);
        var metricY = 576f;
        foreach (var line in view.MetricLines.Take(3))
        { Text(canvas, line, 1055, metricY, 15, Muted); metricY += 23; }

        Text(canvas, "LATEST TRANSITION", 65, 703, 13, Muted, true);
        Text(canvas, view.Receipt, 65, 737, 23, Ink, true);
        var receiptY = 772f;
        foreach (var line in view.ReceiptLines.Take(4))
        { Text(canvas, line, 65, receiptY, 16, Muted); receiptY += 27; }

        Text(canvas, "PHYSICAL ENCODING", 845, 703, 13, Muted, true);
        var storageY = 739f;
        foreach (var line in view.StorageLines.Take(5))
        { Text(canvas, line, 845, storageY, 16, Muted); storageY += 28; }
        Text(canvas, "Inspection is separate from execution. Full history is optional.", 43, 928, 14, Muted);
        Text(canvas, $"Step {view.Step}", 1390, 928, 16, Ink, true);
        canvas.Restore();
    }

    private static void DrawConnection(SKCanvas canvas, InspectionView view)
    {
        const float start = 120, end = 925, landmarkY = 300, movingY = 380;
        float X(int index)
        {
            var fraction = view.Calibrated && view.Coordinates is { Length: > 1 } coordinates && coordinates[^1] != coordinates[0]
                ? (coordinates[index] - coordinates[0]) / (coordinates[^1] - coordinates[0])
                : (double)index / Math.Max(1, view.Landmarks.Length - 1);
            if (view.Reversed) fraction = 1 - fraction;
            return start + (end - start) * (float)fraction;
        }

        Stroke(canvas, new SKPoint(start, landmarkY), new SKPoint(end, landmarkY), LineColor, 2);
        for (var i = 0; i < view.Landmarks.Length; i++)
        {
            var x = X(i);
            Stroke(canvas, new SKPoint(x, landmarkY + 6), new SKPoint(x, 515), LineColor, 1, true);
            Dot(canvas, x, landmarkY, 5, Muted);
            Text(canvas, view.Landmarks[i], x - 7, landmarkY - 18, 17, Ink, true);
            if (view.Calibrated && view.Coordinates is { } ticks)
                Text(canvas, $"{ticks[i]:0.###}", x - 8, landmarkY + 29, 13, Muted);
        }

        var originX = X(0);
        var terminalX = X(view.Landmarks.Length - 1);
        const float terminalY = 462;
        // Each alternative is an observation of the same site, not a duplicate object.
        foreach (var choice in view.Choices)
        {
            var movingX = X(choice.Landmark);
            var alternative = view.Choices.Length > 1 || view.Open;
            Arrow(canvas, originX, movingY, movingX, movingY, Teal, alternative);
            Arrow(canvas, movingX, movingY, terminalX, terminalY, Teal, alternative);
            Dot(canvas, movingX, movingY, 14, Teal.WithAlpha(alternative ? (byte)100 : (byte)255));
            Dot(canvas, movingX, movingY, 5, SKColors.White);
            var forwardInView = choice.Forward != view.Reversed;
            Text(canvas, forwardInView ? "→" : "←", movingX - 10, movingY - 25, 25, Teal, true);
        }
        Dot(canvas, originX, movingY, 9, Ink);
        Dot(canvas, terminalX, terminalY, 9, Ink);
        Text(canvas, Site(view, 0, "anchor"), Math.Clamp(originX - 35, 72, 870), movingY + 31, 13, Ink);
        Text(canvas, Site(view, 2, "terminal"), Math.Clamp(terminalX - 40, 72, 855), terminalY + 30, 13, Ink);
        if (view.Choices.Length == 0)
        {
            using var dashed = Paint(Teal.WithAlpha(130), 2, false);
            dashed.PathEffect = SKPathEffect.CreateDash([7, 6], 0);
            canvas.DrawRoundRect(new SKRect(360, 353, 680, 412), 12, 12, dashed);
            Text(canvas, view.Open ? "moving site remains open" : "no supported moving-site binding", 380, 388, 17, Muted);
        }

        var independentX = X(1);
        Arrow(canvas, originX, 535, independentX, 535, Amber, false);
        Dot(canvas, originX, 535, 7, Ink);
        using (var ring = Paint(Amber, 3, false)) canvas.DrawCircle(independentX, 535, 11, ring);
        Text(canvas, Site(view, 3, "independent site"), Math.Clamp(independentX - 35, 72, 800), 565, 13, Amber);
        Text(canvas, "Two connections share one moving site; the amber site keeps its own identity.", 65, 604, 15, Muted);
        Text(canvas, Site(view, 1, "moving site") + " is the common endpoint.", 65, 628, 15, Teal);
    }

    private static string Site(InspectionView view, int index, string fallback) => view.SiteLabels.Length > index ? view.SiteLabels[index] : fallback;

    private static void DrawBits(SKCanvas canvas, InspectionView view)
    {
        var count = Math.Min(view.DomainSize, 16);
        var cellWidth = Math.Min(42, 420f / Math.Max(1, count));
        for (var i = 0; i < count; i++)
        {
            var active = (view.Mask & (1UL << i)) != 0;
            var x = 1055 + cellWidth * i;
            using var paint = Paint(active ? Teal : SKColor.Parse("#E8EDEE"));
            canvas.DrawRoundRect(new SKRect(x, 271, x + cellWidth - 5, 308), 5, 5, paint);
            Text(canvas, active ? "1" : "0", x + (cellWidth - 17) / 2, 296, 18, active ? SKColors.White : Muted, mono: true);
        }
        Text(canvas, "Bits address whole states in the shared basis.", 1055, 332, 14, Muted);
    }

    private static void Panel(SKCanvas canvas, SKRect rect)
    {
        using var paint = Paint(SKColors.White);
        canvas.DrawRoundRect(rect, 15, 15, paint);
        using var outline = Paint(LineColor, 1, false);
        canvas.DrawRoundRect(rect, 15, 15, outline);
    }

    private static void Pill(SKCanvas canvas, float x, float y, float width, string text, SKColor color)
    {
        using var paint = Paint(color.WithAlpha(15));
        canvas.DrawRoundRect(new SKRect(x, y - 20, x + width, y + 15), 17, 17, paint);
        Text(canvas, text, x + 17, y + 3, 12, color, true);
    }

    private static void Arrow(SKCanvas canvas, float fromX, float fromY, float toX, float toY, SKColor color, bool dashed)
    {
        var a = new SKPoint(fromX, fromY); var b = new SKPoint(toX, toY);
        Stroke(canvas, a, b, color.WithAlpha(dashed ? (byte)110 : (byte)220), 3, dashed);
        var dx = toX - fromX; var dy = toY - fromY; var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 30) return;
        var ux = dx / length; var uy = dy / length;
        var tip = new SKPoint(toX - ux * 17, toY - uy * 17);
        Stroke(canvas, tip, new SKPoint(tip.X - ux * 10 - uy * 5, tip.Y - uy * 10 + ux * 5), color, 2);
        Stroke(canvas, tip, new SKPoint(tip.X - ux * 10 + uy * 5, tip.Y - uy * 10 - ux * 5), color, 2);
    }

    private static void Stroke(SKCanvas canvas, SKPoint from, SKPoint to, SKColor color, float width, bool dashed = false)
    {
        using var paint = Paint(color, width, false);
        if (dashed) paint.PathEffect = SKPathEffect.CreateDash([6, 6], 0);
        canvas.DrawLine(from, to, paint);
    }

    private static void Dot(SKCanvas canvas, float x, float y, float radius, SKColor color)
    {
        using var paint = Paint(color); canvas.DrawCircle(x, y, radius, paint);
    }

    private static void Text(SKCanvas canvas, string text, float x, float y, float size, SKColor color, bool bold = false, bool mono = false)
    {
        using var paint = Paint(color);
        paint.Typeface = mono ? Mono : bold ? Bold : Regular;
        paint.TextSize = size;
        canvas.DrawText(text, x, y, paint);
    }

    private static SKPaint Paint(SKColor color, float width = 1, bool fill = true) => new()
    {
        Color = color, IsAntialias = true, StrokeWidth = width,
        Style = fill ? SKPaintStyle.Fill : SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round
    };
}
