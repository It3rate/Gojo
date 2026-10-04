using System.Globalization;
using SkiaSharp;

namespace ConnectionAssembly.Viewer;

// All arithmetic here chooses pixels and labels from already recorded observations.
// In particular, this renderer never recognizes input or constructs a connection.
internal static class ReplayRenderer
{
    private static readonly SKColor Background = SKColor.Parse("#F2F4F0");
    private static readonly SKColor Ink = SKColor.Parse("#172B35");
    private static readonly SKColor Muted = SKColor.Parse("#647780");
    private static readonly SKColor Border = SKColor.Parse("#DFE6E4");
    private static readonly SKColor Teal = SKColor.Parse("#137D71");
    private static readonly SKColor Cyan = SKColor.Parse("#338DAF");
    private static readonly SKColor Amber = SKColor.Parse("#BD6C35");
    private static readonly SKColor Purple = SKColor.Parse("#8061B0");
    private static readonly SKTypeface Regular = SKTypeface.FromFamilyName("Segoe UI");
    private static readonly SKTypeface Bold = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold);
    private const float DesignWidth = 1480;
    private const float DesignHeight = 1000;

    public static void Draw(SKCanvas canvas, float width, float height, Replay replay, int index)
    {
        canvas.Clear(Background);
        if (width <= 0 || height <= 0) return;
        var scale = Math.Min(width / DesignWidth, height / DesignHeight);
        canvas.Save();
        canvas.Translate((width - DesignWidth * scale) / 2, (height - DesignHeight * scale) / 2);
        canvas.Scale(scale);
        var frame = replay.Frames[index];
        Text(canvas, replay.Title, 28, 43, 27, Ink, true, 1060);
        Text(canvas, "CONNECTION ASSEMBLY / RECORDED OBSERVATIONS", 29, 69, 12, Muted);
        Text(canvas, $"STEP {frame.Step}", 1452, 39, 21, Ink, true, align: SKTextAlign.Right);
        Text(canvas, $"Frame {index + 1} of {replay.Frames.Length}", 1452, 64, 14, Muted, align: SKTextAlign.Right);

        DrawSignal(canvas, replay, index);
        DrawNetwork(canvas, replay, frame);
        DrawLedger(canvas, frame);

        Text(canvas, "Reading this view", 29, 939, 15, Ink, true);
        Text(canvas, "Ports and receipts describe this arrival. Gates and support are after feedback, available for the next arrival.", 180, 939, 14, Muted);
        Text(canvas, "Recognition and help are local accounting, not a forecast. Phase names belong to the observer, not the learner.", 29, 966, 14, Muted);
        var graph = frame.NativeRelations is not null || frame.NativeCarriers is not null
            ? $"Retained: {frame.NativeRelations?.ToString(CultureInfo.InvariantCulture) ?? "—"} relations · {frame.NativeCarriers?.ToString(CultureInfo.InvariantCulture) ?? "—"} carriers"
            : "Retained size not sampled at this step";
        Text(canvas, graph, 1452, 989, 12, Muted, align: SKTextAlign.Right);
        canvas.Restore();
    }

    private static void DrawSignal(SKCanvas canvas, Replay replay, int index)
    {
        Card(canvas, new SKRect(28, 91, 1452, 275));
        var current = replay.Frames[index];
        Text(canvas, "INPUT", 48, 119, 12, Muted, true);
        Text(canvas, current.Phase, 108, 120, 17, Ink, true, 700);
        var inputLabel = current.Value is not null ? $"Observed value {Number(current.Value.Value)}" : "Unknown input";
        if (!current.Complete) inputLabel += " · native read open";
        Text(canvas, inputLabel,
            1431, 120, 14, current.Complete ? Teal : Amber, align: SKTextAlign.Right);

        var first = Math.Max(0, index - 179);
        var visible = replay.Frames.Skip(first).Take(index - first + 1).ToArray();
        var values = visible.Where(item => item.Value is not null).Select(item => item.Value!.Value).ToArray();
        var minimum = values.Length == 0 ? 0 : values.Min();
        var maximum = values.Length == 0 ? 1 : values.Max();
        if (minimum == maximum) { minimum -= 1; maximum += 1; }
        var left = 106f;
        var right = 1426f;
        var top = 144f;
        var bottom = 235f;
        float X(int position) => left + (right - left) * (position - first) / Math.Max(1, index - first);
        float Y(double value) => bottom - (float)((value - minimum) / (maximum - minimum)) * (bottom - top);
        Line(canvas, left, top, right, top, Border, 1);
        Line(canvas, left, bottom, right, bottom, Border, 1);
        Text(canvas, Number(maximum), left - 13, top + 4, 12, Muted, align: SKTextAlign.Right);
        Text(canvas, Number(minimum), left - 13, bottom + 4, 12, Muted, align: SKTextAlign.Right);
        using var path = new SKPath();
        var connected = false;
        for (var i = first; i <= index; i++)
        {
            var observation = replay.Frames[i];
            if (observation.Value is null)
            {
                connected = false;
                Line(canvas, X(i), top, X(i), bottom, Amber.WithAlpha(100), 2, [2, 4]);
                continue;
            }
            var x = X(i);
            var y = Y(observation.Value.Value);
            if (!connected || i > first && observation.Step != replay.Frames[i - 1].Step + 1) path.MoveTo(x, y);
            else path.LineTo(x, y);
            connected = true;
            if (i == index) Circle(canvas, x, y, 4, observation.Complete ? Teal : Amber);
        }
        using (var paint = Stroke(Teal, 2)) canvas.DrawPath(path, paint);
        Text(canvas, $"Step {replay.Frames[first].Step}", left, 259, 12, Muted);
        Text(canvas, "At most 180 past arrivals · gaps remain gaps · visible range rescales", 765, 259, 12, Muted, align: SKTextAlign.Center);
        Text(canvas, $"Step {current.Step}", right, 259, 12, Muted, align: SKTextAlign.Right);
    }

    private static void DrawNetwork(SKCanvas canvas, Replay replay, ReplayFrame frame)
    {
        Card(canvas, new SKRect(28, 295, 1008, 912));
        Text(canvas, "LOCAL READERS + DIRECTED HELP", 48, 325, 13, Ink, true);
        Text(canvas, "Six supplied readers share one input; their successes are not independent evidence.", 48, 349, 13, Muted);
        var boxes = Enumerable.Range(0, 6).Select(NodeBox).ToArray();
        foreach (var link in frame.Links.Where(Visible)) DrawEdge(canvas, boxes, link);
        for (var i = 0; i < 6; i++) DrawNode(canvas, boxes[i], replay.NodeNames[i], i, frame.Nodes[i], frame.Complete);

        Legend(canvas, 50, 836, Teal, "Useful receipt now", null);
        Legend(canvas, 281, 836, Cyan, "Other receipt now", null);
        Legend(canvas, 518, 836, Purple, "New probe: next arrival", [6, 5]);
        Legend(canvas, 794, 836, Muted.WithAlpha(130), "Withdrawn", [2, 5]);
        Text(canvas, "Thin lines: open gates; support controls line weight. Unused open links remain available while support fades.", 49, 870, 12, Muted, maxWidth: 935);
        Text(canvas, "An open read may have cleared ports: zero is no pulse, not an absence proof. Closed idle slots are hidden.", 49, 892, 12, Muted, maxWidth: 935);
    }

    private static SKRect NodeBox(int index)
    {
        var left = 74f + index % 3 * 310;
        var top = index < 3 ? 404f : 652f;
        return new SKRect(left, top, left + 226, top + 148);
    }

    private static void DrawNode(SKCanvas canvas, SKRect box, string name, int index, ReplayNode node, bool complete)
    {
        var border = !complete ? Amber : node.Raw > 0 ? Teal : node.Pressure > 0 ? Amber : Border;
        Rounded(canvas, box, 11, SKColors.White);
        Rounded(canvas, box, 11, border, 1.5f);
        Circle(canvas, box.Left + 22, box.Top + 24, 12, Ink);
        Text(canvas, (index + 1).ToString(CultureInfo.InvariantCulture), box.Left + 22, box.Top + 29, 12, SKColors.White, true, align: SKTextAlign.Center);
        Text(canvas, name, box.Left + 43, box.Top + 29, 16, Ink, true, 172);
        static string Read(int value) => value.ToString(CultureInfo.InvariantCulture);
        Text(canvas, "raw", box.Left + 16, box.Top + 61, 12, Muted);
        Text(canvas, Read(node.Raw), box.Left + 59, box.Top + 61, 18, node.Raw > 0 && complete ? Teal : Ink, true);
        Text(canvas, "request", box.Left + 109, box.Top + 61, 12, Muted);
        Text(canvas, Read(node.Request), box.Right - 18, box.Top + 61, 18, Ink, true, align: SKTextAlign.Right);
        Text(canvas, "help", box.Left + 16, box.Top + 88, 12, Muted);
        Text(canvas, Read(node.Help), box.Left + 59, box.Top + 88, 18, node.Help > 0 && complete ? Teal : Ink, true);
        Text(canvas, "pressure", box.Left + 109, box.Top + 88, 12, Muted);
        Text(canvas, Read(node.Pressure), box.Right - 18, box.Top + 88, 18, node.Pressure > 0 && complete ? Amber : Ink, true, align: SKTextAlign.Right);
        Line(canvas, box.Left + 15, box.Top + 103, box.Right - 15, box.Top + 103, Border, 1);
        Text(canvas, $"engagement {node.Engagement}", box.Left + 16, box.Top + 124, 12, Muted);
        Text(canvas, !complete ? "open read" : node.Accounted ? "accounted" : "unaccounted", box.Right - 16, box.Top + 124, 11,
            !complete || !node.Accounted ? Amber : Teal, align: SKTextAlign.Right);
        Rounded(canvas, new SKRect(box.Left + 16, box.Top + 133, box.Right - 16, box.Top + 137), 2, Border);
        var filled = (box.Width - 32) * Math.Clamp(node.Engagement / 48f, 0, 1);
        if (filled > 0) Rounded(canvas, new SKRect(box.Left + 16, box.Top + 133, box.Left + 16 + filled, box.Top + 137), 2, Muted.WithAlpha(130));
        Circle(canvas, box.Right, box.Top + 58, 5, complete && node.Raw > 0 ? Teal : SKColors.White, border);
        Circle(canvas, box.Left, box.Top + 82, 5, complete && node.Help > 0 ? Teal : SKColors.White, border);
    }

    private static void DrawEdge(SKCanvas canvas, SKRect[] boxes, ReplayLink link)
    {
        var source = boxes[link.From];
        var target = boxes[link.To];
        var start = new SKPoint(source.Right + 6, source.Top + 58);
        var end = new SKPoint(target.Left - 8, target.Top + 82);
        var sameRow = link.From / 3 == link.To / 3;
        var bend = sameRow ? (link.From < link.To ? -94f : 92f) : (link.From < link.To ? 35f : -35f);
        var distance = Math.Abs(link.From % 3 - link.To % 3);
        if (sameRow) bend *= 1 + distance * .17f;
        var control1 = new SKPoint(start.X + 66, start.Y + bend);
        var control2 = new SKPoint(end.X - 66, end.Y + bend);
        using var path = new SKPath();
        path.MoveTo(start);
        path.CubicTo(control1, control2, end);
        var tone = link.Proposed ? Purple : link.Released ? Muted.WithAlpha(110) : Muted.WithAlpha((byte)(95 + Math.Clamp(link.Support, 0, 48) * 2));
        var lineWidth = 1.1f + Math.Clamp(link.Support, 0, 48) / 48f * 2;
        float[]? dash = link.Proposed ? [6, 5] : link.Released ? [2, 5] : null;
        using (var paint = Stroke(tone, lineWidth, dash)) canvas.DrawPath(path, paint);
        if (link.Receipt > 0)
        {
            tone = link.Useful ? Teal : Cyan;
            using var receipt = Stroke(tone.WithAlpha(220), 3);
            canvas.DrawPath(path, receipt);
            var mid = new SKPoint((start.X + control1.X * 3 + control2.X * 3 + end.X) / 8,
                (start.Y + control1.Y * 3 + control2.Y * 3 + end.Y) / 8);
            Circle(canvas, mid.X, mid.Y, 4, tone, SKColors.White);
        }
        using var arrow = new SKPath();
        arrow.MoveTo(end.X, end.Y);
        arrow.LineTo(end.X - 8, end.Y - 5);
        arrow.LineTo(end.X - 8, end.Y + 5);
        arrow.Close();
        using var fill = Fill(tone);
        canvas.DrawPath(arrow, fill);
    }

    private static void DrawLedger(SKCanvas canvas, ReplayFrame frame)
    {
        Card(canvas, new SKRect(1028, 295, 1452, 912));
        Text(canvas, "CONNECTION READS", 1048, 326, 13, Ink, true);
        Text(canvas, "After feedback", 1220, 353, 11, Muted, align: SKTextAlign.Center);
        Text(canvas, "This arrival", 1384, 353, 11, Muted, align: SKTextAlign.Center);
        Text(canvas, "From → to", 1048, 376, 12, Muted);
        Text(canvas, "support", 1200, 376, 12, Muted, align: SKTextAlign.Right);
        Text(canvas, "gate", 1263, 376, 12, Muted, align: SKTextAlign.Right);
        Text(canvas, "receipt", 1334, 376, 12, Muted, align: SKTextAlign.Right);
        Text(canvas, "note", 1381, 376, 12, Muted);
        Line(canvas, 1048, 388, 1432, 388, Border, 1);
        var links = frame.Links.Where(Visible).OrderBy(link => link.From).ThenBy(link => link.To).ToArray();
        for (var i = 0; i < links.Length; i++)
        {
            var link = links[i];
            var y = 410f + i * 15.7f;
            var tone = link.Useful ? Teal : link.Proposed ? Purple : link.Released ? Amber : Ink;
            if (i % 2 == 0) Rect(canvas, new SKRect(1044, y - 12, 1435, y + 3), Background.WithAlpha(180));
            Text(canvas, $"{link.From + 1} → {link.To + 1}", 1054, y, 12, tone, link.Useful);
            Text(canvas, link.Support.ToString(CultureInfo.InvariantCulture), 1200, y, 12, tone, align: SKTextAlign.Right);
            Text(canvas, link.Gate.ToString(CultureInfo.InvariantCulture), 1263, y, 12, tone, align: SKTextAlign.Right);
            Text(canvas, link.Receipt.ToString(CultureInfo.InvariantCulture), 1334, y, 12, tone, align: SKTextAlign.Right);
            Text(canvas, link.Proposed ? "probe" : link.Released ? "closed" : link.Useful ? "useful" : link.Receipt > 0 ? "other" : "", 1363, y, 11, tone);
        }
        if (links.Length == 0)
        {
            Text(canvas, "No available or recently active links.", 1048, 418, 14, Muted);
            Text(canvas, "The recorded closed slots remain in the graph.", 1048, 444, 12, Muted, maxWidth: 385);
        }
        Text(canvas, "Support is supplied participation, not confidence.", 1048, 893, 12, Muted, maxWidth: 385);
    }

    private static bool Visible(ReplayLink link) => link.Gate != 0 || link.Receipt != 0 || link.Proposed || link.Released;
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void Legend(SKCanvas canvas, float x, float y, SKColor tone, string text, float[]? dash)
    {
        Line(canvas, x, y - 4, x + 26, y - 4, tone, 2, dash);
        Text(canvas, text, x + 34, y, 12, Muted);
    }

    private static void Card(SKCanvas canvas, SKRect box)
    {
        Rounded(canvas, box, 13, SKColors.White);
        Rounded(canvas, box, 13, Border, 1);
    }

    private static void Text(SKCanvas canvas, string value, float x, float y, float size, SKColor color, bool bold = false,
        float maxWidth = float.MaxValue, SKTextAlign align = SKTextAlign.Left)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = color, TextSize = size, Typeface = bold ? Bold : Regular, TextAlign = align };
        if (paint.MeasureText(value) > maxWidth)
        {
            while (value.Length > 1 && paint.MeasureText(value + "…") > maxWidth) value = value[..^1];
            value += "…";
        }
        canvas.DrawText(value, x, y, paint);
    }

    private static SKPaint Fill(SKColor color) => new() { IsAntialias = true, Color = color, Style = SKPaintStyle.Fill };
    private static SKPaint Stroke(SKColor color, float width, float[]? dash = null) => new()
    {
        IsAntialias = true, Color = color, StrokeWidth = width, Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round, PathEffect = dash is null ? null : SKPathEffect.CreateDash(dash, 0)
    };
    private static void Rounded(SKCanvas canvas, SKRect box, float radius, SKColor color, float stroke = 0)
    {
        using var paint = stroke > 0 ? Stroke(color, stroke) : Fill(color);
        canvas.DrawRoundRect(box, radius, radius, paint);
    }
    private static void Rect(SKCanvas canvas, SKRect box, SKColor color)
    {
        using var paint = Fill(color);
        canvas.DrawRect(box, paint);
    }
    private static void Circle(SKCanvas canvas, float x, float y, float radius, SKColor color, SKColor? outline = null)
    {
        using var paint = Fill(color);
        canvas.DrawCircle(x, y, radius, paint);
        if (outline is not null)
        {
            using var border = Stroke(outline.Value, 1.2f);
            canvas.DrawCircle(x, y, radius, border);
        }
    }
    private static void Line(SKCanvas canvas, float x1, float y1, float x2, float y2, SKColor color, float width, float[]? dash = null)
    {
        using var paint = Stroke(color, width, dash);
        canvas.DrawLine(x1, y1, x2, y2, paint);
    }
}
