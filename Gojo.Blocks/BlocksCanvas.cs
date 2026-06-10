using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using GojoDraw;
using SkiaSharp;

namespace Gojo.Blocks;

internal sealed class BlocksCanvas : Control
{
    private BlocksDocument? document;
    private BlocksHitArea[] hitAreas = [];

    public BlocksCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public BlocksDocument? Document
    {
        get => document;
        set
        {
            document = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Document is not { } current)
        {
            return;
        }

        context.Custom(new BlocksDrawOperation(
            new Rect(Bounds.Size),
            current,
            PublishHitAreas));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateVisual();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Document is null ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var point = e.GetPosition(this);
        var hit = hitAreas.FirstOrDefault(area => area.Bounds.Contains((float)point.X, (float)point.Y));
        Document = hit.Kind switch
        {
            BlocksHitKind.PreviousPage => Document.PreviousPage(),
            BlocksHitKind.NextPage => Document.NextPage(),
            BlocksHitKind.PageDot => Document.SelectPage(hit.PageIndex),
            _ => Document
        };
        e.Handled = hit.Kind != BlocksHitKind.None;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Document is null)
        {
            return;
        }

        if (e.Key == Key.Left)
        {
            Document = Document.PreviousPage();
            e.Handled = true;
        }
        else if (e.Key == Key.Right)
        {
            Document = Document.NextPage();
            e.Handled = true;
        }
    }

    private void PublishHitAreas(IReadOnlyList<BlocksHitArea> next)
    {
        hitAreas = [.. next];
    }
}

internal sealed class BlocksDrawOperation : ICustomDrawOperation
{
    private static readonly GojoCadSkiaSceneRenderer GojoRenderer = new();
    private static readonly SKColor Background = new(248, 247, 243);
    private static readonly SKColor RailFill = new(236, 235, 229);
    private static readonly SKColor Ink = new(47, 55, 65);
    private static readonly SKColor Muted = new(105, 112, 120);
    private static readonly SKColor Accent = new(66, 122, 153);
    private static readonly SKColor SoftLine = new(197, 200, 196);
    private readonly BlocksDocument document;
    private readonly Action<IReadOnlyList<BlocksHitArea>> publishHitAreas;

    public BlocksDrawOperation(Rect bounds, BlocksDocument document, Action<IReadOnlyList<BlocksHitArea>> publishHitAreas)
    {
        Bounds = bounds;
        this.document = document;
        this.publishHitAreas = publishHitAreas;
    }

    public Rect Bounds { get; }

    public void Dispose()
    {
    }

    public bool HitTest(Point p) => Bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature leaseFeature)
        {
            return;
        }

        using var lease = leaseFeature.Lease();
        var canvas = lease.SkCanvas;
        var size = new SKSize((float)Bounds.Width, (float)Bounds.Height);
        var hitAreas = new List<BlocksHitArea>();

        canvas.Save();
        canvas.Clear(Background);
        canvas.ClipRect(new SKRect(0, 0, size.Width, size.Height));
        DrawShell(canvas, size);
        DrawPage(canvas, size);
        DrawBottomNavigation(canvas, size, hitAreas);
        canvas.Restore();

        publishHitAreas(hitAreas);
    }

    private void DrawShell(SKCanvas canvas, SKSize size)
    {
        var leftRail = new SKRect(0, 0, 218, size.Height);
        var bottomRail = new SKRect(218, Math.Max(0, size.Height - 72), size.Width, size.Height);

        using var railPaint = new SKPaint { Color = RailFill, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var divider = new SKPaint { Color = SoftLine, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        canvas.DrawRect(leftRail, railPaint);
        canvas.DrawLine(leftRail.Right, 0, leftRail.Right, size.Height, divider);
        canvas.DrawLine(leftRail.Right, bottomRail.Top, size.Width, bottomRail.Top, divider);

        DrawLeftRail(canvas, leftRail);
    }

    private void DrawLeftRail(SKCanvas canvas, SKRect rail)
    {
        var page = document.CurrentPage;
        using var title = TextPaint(22, Ink, bold: true);
        using var subtitle = TextPaint(12.5f, Muted);
        using var label = TextPaint(11.5f, Muted, bold: true);
        using var buttonFill = new SKPaint { Color = new SKColor(248, 247, 243), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var stroke = new SKPaint { Color = new SKColor(184, 188, 184), Style = SKPaintStyle.Stroke, StrokeWidth = 1.1f, IsAntialias = true };

        canvas.DrawText("Gojo Blocks", rail.Left + 24, rail.Top + 40, title);
        canvas.DrawText($"{document.PageIndex + 1}/{document.Pages.Count}", rail.Left + 24, rail.Top + 64, subtitle);

        DrawWrappedText(canvas, page.Title, rail.Left + 24, rail.Top + 104, 164, title, 27);
        DrawWrappedText(canvas, page.Subtitle, rail.Left + 24, rail.Top + 150, 158, subtitle, 18);

        var y = rail.Top + 250;
        canvas.DrawText("Controls", rail.Left + 24, y, label);
        for (var index = 0; index < 3; index++)
        {
            var button = new SKRect(rail.Left + 24, y + 18 + index * 42, rail.Right - 24, y + 48 + index * 42);
            canvas.DrawRoundRect(button, 6, 6, buttonFill);
            canvas.DrawRoundRect(button, 6, 6, stroke);
        }

        canvas.DrawText("Sliders", rail.Left + 24, y + 172, label);
        for (var index = 0; index < 2; index++)
        {
            var cy = y + 198 + index * 36;
            canvas.DrawLine(rail.Left + 28, cy, rail.Right - 34, cy, stroke);
            using var knob = new SKPaint { Color = Accent, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawCircle(rail.Left + 74 + index * 42, cy, 5.5f, knob);
        }
    }

    private void DrawPage(SKCanvas canvas, SKSize size)
    {
        var page = document.CurrentPage;
        var pageArea = new SKRect(250, 42, Math.Max(251, size.Width - 44), Math.Max(43, size.Height - 104));

        using var pageFill = new SKPaint { Color = new SKColor(255, 255, 252), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var pageStroke = new SKPaint { Color = new SKColor(215, 217, 210), Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f, IsAntialias = true };
        canvas.DrawRoundRect(pageArea, 8, 8, pageFill);
        canvas.DrawRoundRect(pageArea, 8, 8, pageStroke);

        using var title = TextPaint(25, Ink, bold: true);
        using var body = TextPaint(13.5f, Muted);
        canvas.DrawText(page.Title, pageArea.Left + 28, pageArea.Top + 42, title);
        DrawWrappedText(canvas, page.Body, pageArea.Left + 28, pageArea.Top + 72, pageArea.Width - 56, body, 19);

        if (page.Structure is { } structure)
        {
            using var paints = new GojoCadSkiaPaints();
            paints.AreaBoundary.Color = new SKColor(64, 76, 88, 226);
            paints.AreaBoundary.StrokeWidth = 2.2f;
            paints.SharedFramePoint.Color = new SKColor(64, 76, 88, 180);
            var structureArea = new SKRect(
                pageArea.Left + 40,
                pageArea.Top + 130,
                pageArea.Right - 40,
                pageArea.Bottom - 38);
            GojoRenderer.Draw(canvas, structure, structureArea, page.ProjectionSeed, page.ModelBounds, paints);
        }
        else
        {
            DrawEmptyPageMark(canvas, pageArea);
        }
    }

    private void DrawEmptyPageMark(SKCanvas canvas, SKRect pageArea)
    {
        using var stroke = new SKPaint
        {
            Color = new SKColor(130, 145, 150, 90),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true
        };
        var center = new SKPoint(pageArea.MidX, pageArea.MidY + 18);
        canvas.DrawCircle(center, 46, stroke);
        canvas.DrawLine(center.X - 58, center.Y, center.X + 58, center.Y, stroke);
        canvas.DrawLine(center.X, center.Y - 58, center.X, center.Y + 58, stroke);
    }

    private void DrawBottomNavigation(SKCanvas canvas, SKSize size, List<BlocksHitArea> hitAreas)
    {
        var centerX = (218 + size.Width) * 0.5f;
        var y = size.Height - 36;
        var dotSpacing = 20f;
        var dotStart = centerX - (document.Pages.Count - 1) * dotSpacing * 0.5f;

        using var dotPaint = new SKPaint { Color = new SKColor(147, 153, 154), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var activeDotPaint = new SKPaint { Color = Accent, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var arrowPaint = new SKPaint
        {
            Color = Ink,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.4f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        var previous = new SKRect(dotStart - 56, y - 18, dotStart - 24, y + 18);
        var next = new SKRect(dotStart + (document.Pages.Count - 1) * dotSpacing + 24, y - 18, dotStart + (document.Pages.Count - 1) * dotSpacing + 56, y + 18);
        DrawArrow(canvas, previous, left: true, arrowPaint);
        DrawArrow(canvas, next, left: false, arrowPaint);
        hitAreas.Add(new BlocksHitArea(previous, BlocksHitKind.PreviousPage, -1));
        hitAreas.Add(new BlocksHitArea(next, BlocksHitKind.NextPage, -1));

        for (var index = 0; index < document.Pages.Count; index++)
        {
            var x = dotStart + index * dotSpacing;
            var radius = index == document.PageIndex ? 5.4f : 3.8f;
            canvas.DrawCircle(x, y, radius, index == document.PageIndex ? activeDotPaint : dotPaint);
            hitAreas.Add(new BlocksHitArea(new SKRect(x - 9, y - 9, x + 9, y + 9), BlocksHitKind.PageDot, index));
        }
    }

    private static void DrawArrow(SKCanvas canvas, SKRect bounds, bool left, SKPaint paint)
    {
        var midY = bounds.MidY;
        var pointX = left ? bounds.Left + 9 : bounds.Right - 9;
        var tailX = left ? bounds.Right - 8 : bounds.Left + 8;
        using var path = new SKPath();
        path.MoveTo(tailX, midY - 11);
        path.LineTo(pointX, midY);
        path.LineTo(tailX, midY + 11);
        canvas.DrawPath(path, paint);
    }

    private static SKPaint TextPaint(float size, SKColor color, bool bold = false) =>
        new()
        {
            Color = color,
            TextSize = size,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", bold ? SKFontStyle.Bold : SKFontStyle.Normal),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

    private static void DrawWrappedText(SKCanvas canvas, string text, float x, float y, float width, SKPaint paint, float lineHeight)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";
            if (paint.MeasureText(candidate) <= width || string.IsNullOrEmpty(line))
            {
                line = candidate;
                continue;
            }

            canvas.DrawText(line, x, y, paint);
            y += lineHeight;
            line = word;
        }

        if (!string.IsNullOrEmpty(line))
        {
            canvas.DrawText(line, x, y, paint);
        }
    }
}

internal readonly record struct BlocksHitArea(SKRect Bounds, BlocksHitKind Kind, int PageIndex);

internal enum BlocksHitKind
{
    None,
    PreviousPage,
    NextPage,
    PageDot
}
