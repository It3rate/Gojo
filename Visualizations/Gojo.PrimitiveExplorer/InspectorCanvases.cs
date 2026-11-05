using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Gojo.PrimitiveExplorer;

internal static class Palette
{
#if BROWSER
    public const string UiFont = "avares://Avalonia.Fonts.Inter/Assets#Inter";
    public const string CodeFont = UiFont;
#else
    public const string UiFont = "Segoe UI";
    public const string CodeFont = "Cascadia Mono,Consolas";
#endif
    public static readonly IBrush Paper = Brush("#F7F5EE"), Ink = Brush("#213B3A"), Muted = Brush("#677C78"),
        Teal = Brush("#167C6C"), Orange = Brush("#C77336"), Dark = Brush("#152D2D"), Pale = Brush("#DCEDE6"), White = Brush("#FFFFFF"), Line = Brush("#CCD8D0");
    public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
    public static FormattedText Text(string text, double size, IBrush ink, bool mono = false, bool bold = false) =>
        new(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(mono ? CodeFont : UiFont, FontStyle.Normal, bold ? FontWeight.SemiBold : FontWeight.Normal), size, ink);
    public static void DrawText(DrawingContext c, string text, double x, double y, double size, IBrush ink, bool mono = false, bool bold = false) =>
        c.DrawText(Text(text, size, ink, mono, bold), new(x, y));
    public static double Wrap(DrawingContext? c, string text, double x, double y, double width, double size, IBrush ink)
    {
        var line = ""; var yy = y;
        foreach (var word in text.Split(' '))
        {
            var candidate = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && Text(candidate, size, ink).Width > width)
            { if (c is not null) DrawText(c, line, x, yy, size, ink); yy += size * 1.45; line = word; }
            else line = candidate;
        }
        if (line.Length > 0) { if (c is not null) DrawText(c, line, x, yy, size, ink); yy += size * 1.45; }
        return yy;
    }
}

internal sealed class FocusLink
{
    public string? Hover, Pinned;
    public string? Key => Hover ?? Pinned;
    public string? Detail;
    public event Action? Changed;
    public void Point(string? key, string? detail = null) { Hover = key; Detail = detail; Changed?.Invoke(); }
    public void Pin(string? key) { Pinned = key; Changed?.Invoke(); }
    public void Clear() { Hover = Pinned = Detail = null; Changed?.Invoke(); }
}

internal sealed class EncodingCanvas : Control
{
    private ExplorerPage page;
    private readonly FocusLink focus;
    private readonly List<(Rect Rect, string Key, string Detail)> hits = [];
    private readonly Dictionary<string, double> keyOffsets = [];
    public double? OffsetOf(string key) => keyOffsets.TryGetValue(key, out var y) ? y : null;
    public EncodingCanvas(ExplorerPage page, FocusLink focus)
    {
        this.page = page; this.focus = focus; MinHeight = 300; Focusable = true;
        focus.Changed += InvalidateVisual;
        PointerMoved += (_, e) => { var at = e.GetPosition(this); var hit = hits.LastOrDefault(h => h.Rect.Contains(at)); focus.Point(hit.Key, hit.Detail); };
        PointerExited += (_, _) => focus.Point(null);
        PointerPressed += (_, e) => { var hit = hits.LastOrDefault(h => h.Rect.Contains(e.GetPosition(this))); focus.Pin(hit.Key); e.Handled = true; Focus(); };
    }
    public void SetPage(ExplorerPage next) { page = next; InvalidateMeasure(); InvalidateVisual(); }
    protected override Size MeasureOverride(Size availableSize) => new(double.IsInfinity(availableSize.Width) ? 370 : availableSize.Width, Layout(null, double.IsInfinity(availableSize.Width) ? 370 : availableSize.Width));
    public override void Render(DrawingContext context) { base.Render(context); Layout(context, Bounds.Width); }
    private double Layout(DrawingContext? c, double width)
    {
        if (c is not null) { hits.Clear(); keyOffsets.Clear(); c.DrawRectangle(Palette.Dark, null, new(0, 0, width, Bounds.Height)); }
        var y = 20d; var left = 18d; var textWidth = Math.Max(150, width - 36);
        if (c is not null) Palette.DrawText(c, "LOW BIT FIRST  →    ·    GREY = PADDING", left, y, 10, Palette.Brush("#86AAA1"), true);
        y += 34;
        foreach (var block in page.Blocks)
        {
            var top = y;
            y = Palette.Wrap(c, block.Title, left, y, textWidth, 14, Palette.White) + 7;
            y = Palette.Wrap(c, block.Note, left, y, textWidth, 11.5, Palette.Brush("#A8C2B9")) + 10;
            if (c is not null) { hits.Add((new(left, top, textWidth, y - top), block.Key, block.Note)); keyOffsets.TryAdd(block.Key, top); }
            if (block.Bytes.Length == 0)
            {
                if (c is not null) Palette.DrawText(c, "No rows yet", left, y, 12, Palette.Brush("#86AAA1"), true);
                y += 25;
            }
            // Two bytes per line. Byte labels are conventional high-bit-first
            // hexadecimal; the individual bit cells follow physical offsets.
            const double cell = 14;
            for (var by = 0; by < block.Bytes.Length; by += 2)
            {
                for (var j = 0; j < 2 && by + j < block.Bytes.Length; j++)
                {
                    var b = by + j; var xx = left + j * 159;
                    if (c is not null) Palette.DrawText(c, block.Bytes[b].ToString("X2"), xx, y + 2, 11, Palette.Brush("#87B8A5"), true);
                    for (var bit = 0; bit < 8; bit++)
                    {
                        var index = b * 8 + bit; var mark = block.Marks.FirstOrDefault(m => index >= m.Offset && index < m.Offset + m.Width);
                        var valid = index < block.UsedBits;
                        var selected = valid && focus.Key is not null && (mark?.Keys.Contains(focus.Key) == true || block.Key == focus.Key);
                        var rect = new Rect(xx + 26 + bit * cell, y, cell - 1, 20);
                        if (c is not null)
                        {
                            if (valid && mark is not null) foreach (var key in mark.Keys) keyOffsets.TryAdd(key, y);
                            c.DrawRectangle(selected ? Palette.Orange : valid ? Palette.Brush("#234540") : Palette.Brush("#1C3534"), null, rect, 2, 2);
                            Palette.DrawText(c, ((block.Bytes[b] >> bit) & 1).ToString(), rect.X + 3, rect.Y + 2, 11,
                                valid ? Palette.White : Palette.Brush("#546C65"), true);
                            if (valid) hits.Add((rect, mark?.Keys.LastOrDefault(k => k != block.Key) ?? block.Key,
                                $"{block.Title} · byte {b}, bit {bit} (offset {index}) = {((block.Bytes[b] >> bit) & 1)}. {mark?.Label}"));
                        }
                    }
                }
                y += 25;
            }
            y += 18;
            if (c is not null) c.DrawLine(new Pen(Palette.Brush("#31504B"), 1), new(left, y), new(width - left, y));
            y += 22;
        }
        return y + 10;
    }
}

internal sealed class StructureCanvas : Control
{
    private readonly Dictionary<DrawingItem, Geometry> curves = [];
    private static readonly Pen CurveHit = new(Palette.Ink, 14);
    private ExplorerPage page;
    private readonly FocusLink focus;
    private double scale = 1, dx, dy;
    private readonly Cursor hand = new(StandardCursorType.Hand);
    public event Action<Choice, int>? ChoicePicked;
    public StructureCanvas(ExplorerPage page, FocusLink focus)
    {
        this.page = page; this.focus = focus; MinHeight = 280; Focusable = true;
        focus.Changed += InvalidateVisual;
        PointerMoved += (_, e) =>
        {
            var hit = Hit(e.GetPosition(this));
            Cursor = hit?.Selects is null ? null : hand;
            focus.Point(hit?.Key, hit?.Detail);
        };
        PointerExited += (_, _) => focus.Point(null);
        PointerPressed += (_, e) =>
        {
            var hit = Hit(e.GetPosition(this));
            if (hit?.Selects is not null) return;
            focus.Pin(hit?.Key); e.Handled = true; Focus();
        };
        Tapped += (_, e) =>
        {
            if (Hit(e.GetPosition(this))?.Selects is not { } pick) return;
            ChoicePicked?.Invoke(pick.Choice, pick.Index);
            e.Handled = true;
        };
    }
    public void SetPage(ExplorerPage next)
    {
        page = next;
        curves.Clear();
        foreach (var item in page.Drawing.Where(d => d.PathData != null))
            curves[item] = Geometry.Parse(item.PathData!);
        InvalidateVisual();
    }
    internal DrawingItem? Hit(Point screen)
    {
        var pt = new Point((screen.X - dx) / scale, (screen.Y - dy) / scale);
        foreach (var tile in page.Drawing.Where(d => d.Selects.HasValue))
            if (TileBounds(tile).Contains(pt)) return tile;
        foreach (var item in page.Drawing.Where(d => d.Kind == "node").Reverse())
            if (new Rect(item.X - NodeWidth(item) / 2, item.Y - 23, NodeWidth(item), 46).Contains(pt)) return item;
        // Interior landmarks lie on top of strokes. Prefer the small marker
        // near its centre so the same site can light up in every attached run.
        foreach (var item in page.Drawing.Where(d => d.Kind == "circle" && d.X2 <= 8).Reverse())
            if (Math.Pow(pt.X - item.X, 2) + Math.Pow(pt.Y - item.Y, 2) <= Math.Pow(item.X2 + 3, 2)) return item;
        foreach (var (item, path) in curves.Reverse())
            if (path.StrokeContains(CurveHit, pt)) return item;
        foreach (var item in page.Drawing.Where(d => d.Kind is "edge" or "stroke").Reverse())
        {
            var vx = item.X2 - item.X; var vy = item.Y2 - item.Y;
            var t = Math.Clamp(((pt.X - item.X) * vx + (pt.Y - item.Y) * vy) / Math.Max(1, vx * vx + vy * vy), 0, 1);
            if (Math.Pow(pt.X - item.X - t * vx, 2) + Math.Pow(pt.Y - item.Y - t * vy, 2) < 100) return item;
        }
        foreach (var item in page.Drawing.Where(d => d.Kind == "circle"))
            if (Math.Abs(Math.Sqrt(Math.Pow(pt.X - item.X, 2) + Math.Pow(pt.Y - item.Y, 2)) - item.X2) < 12) return item;
        return null;
    }
    private static Rect TileBounds(DrawingItem tile) => new(tile.X - tile.X2 / 2, tile.Y - tile.Y2 / 2, tile.X2, tile.Y2);
    private static double NodeWidth(DrawingItem item) => Math.Clamp(Palette.Text(item.Label, 12, Palette.Ink).Width + 26, 58, 192);
    public override void Render(DrawingContext c)
    {
        base.Render(c); c.DrawRectangle(Palette.Paper, null, new(0, 0, Bounds.Width, Bounds.Height));
        scale = Math.Min(Bounds.Width / 720, Bounds.Height / 480); dx = (Bounds.Width - 720 * scale) / 2; dy = (Bounds.Height - 480 * scale) / 2;
        using var transform = c.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(dx, dy));
        for (var x = 20; x < 720; x += 24) for (var y = 20; y < 480; y += 24)
            c.DrawEllipse(Palette.Brush("#DAE2D9"), null, new Point(x, y), .9, .9);
        foreach (var item in page.Drawing.OrderBy(d => d.Kind == "node" ? 1 : 0))
        {
            var selected = focus.Key is not null && ((focus.Key == item.Key && item.Key.Length > 0) || item.RelatedKeys?.Contains(focus.Key) == true);
            var ink = selected ? Palette.Orange : item.Color is not null ? Palette.Brush(item.Color) : item.Active ? Palette.Teal : Palette.Muted;
            var pen = new Pen(ink, selected ? 3.5 : item.LineWidth ?? (item.Active ? 2.5 : 1.7), item.Uncertain ? DashStyle.Dash : null);
            if (item.Kind == "tile")
            {
                var rect = TileBounds(item);
                c.DrawRectangle(item.Active ? Palette.Pale : Palette.White,
                    new Pen(selected ? Palette.Orange : item.Active ? Palette.Teal : Palette.Line, item.Active ? 2 : 1), rect, 6, 6);
                var label = Palette.Text(item.Label, 10, Palette.Ink, bold: item.Active);
                c.DrawText(label, new(item.X - label.Width / 2, rect.Bottom - label.Height - 2));
            }
            if (item.Kind == "circle") c.DrawEllipse(null, pen, new(item.X, item.Y), item.X2, item.X2);
            if (item.PathData != null) c.DrawGeometry(null, pen, curves[item]);
            if (item.Kind == "stroke") c.DrawLine(pen, new(item.X, item.Y), new(item.X2, item.Y2));
            if (item.Kind == "edge")
            {
                c.DrawLine(pen, new(item.X, item.Y), new(item.X2, item.Y2));
                var angle = Math.Atan2(item.Y2 - item.Y, item.X2 - item.X);
                c.DrawLine(pen, new(item.X2, item.Y2), new(item.X2 - 10 * Math.Cos(angle - .4), item.Y2 - 10 * Math.Sin(angle - .4)));
                c.DrawLine(pen, new(item.X2, item.Y2), new(item.X2 - 10 * Math.Cos(angle + .4), item.Y2 - 10 * Math.Sin(angle + .4)));
                if (item.Label.Length > 0)
                {
                    var t = Palette.Text(item.Label, 11, ink); var x = (item.X + item.X2 - t.Width) / 2; var y = (item.Y + item.Y2) / 2 - 24;
                    c.DrawRectangle(Palette.Paper, null, new(x - 4, y - 2, t.Width + 8, 20), 4, 4); c.DrawText(t, new(x, y));
                }
            }
            if (item.Kind == "node")
            {
                var w = NodeWidth(item); var r = new Rect(item.X - w / 2, item.Y - 23, w, 46);
                c.DrawRectangle(selected ? Palette.Brush("#F9E4CF") : item.Active ? Palette.Pale : Palette.White, pen, r, 12, 12);
                var t = Palette.Text(item.Label, 12, selected ? Palette.Orange : Palette.Ink, bold: true);
                c.DrawText(t, new(item.X - t.Width / 2, item.Y - t.Height / 2));
                if (item.Active) c.DrawEllipse(Palette.Teal, null, new(r.Right - 6, r.Top + 6), 4, 4);
            }
            if (item.Kind == "label")
            {
                var t = Palette.Text(item.Label, 11, Palette.Muted); c.DrawText(t, new(item.X - t.Width / 2, item.Y));
            }
        }
    }
}
