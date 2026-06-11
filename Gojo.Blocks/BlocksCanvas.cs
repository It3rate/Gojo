using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GojoDraw;
using SkiaSharp;
using System.Diagnostics;

namespace Gojo.Blocks;

internal sealed class BlocksCanvas : Control
{
    private readonly Stopwatch animationClock = Stopwatch.StartNew();
    private readonly DispatcherTimer animationTimer;
    private BlocksDocument? document;
    private BlocksHitArea[] hitAreas = [];

    public BlocksCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
        animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1000.0 / 60)
        };
        animationTimer.Tick += (_, _) =>
        {
            if (Document?.CurrentPage.HasAnimation == true)
            {
                InvalidateVisual();
            }
        };
    }

    public BlocksDocument? Document
    {
        get => document;
        set
        {
            document = value;
            animationClock.Restart();
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
            GojoDrawAnimationClock.FrameFor(animationClock.Elapsed, current.CurrentPage.TutorialScene),
            PublishHitAreas));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        animationTimer.Start();
        InvalidateVisual();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        animationTimer.Stop();
        base.OnDetachedFromVisualTree(e);
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
            BlocksHitKind.ToggleNotes => Document.ToggleNotes(),
            _ => Document
        };
        if (hit.Kind != BlocksHitKind.None)
        {
            animationClock.Restart();
        }
        e.Handled = hit.Kind != BlocksHitKind.None;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (Document is null)
        {
            return;
        }

        var command = GojoDrawKeyboard.ReadCommand(ToGojoDrawKeyInput(e));
        var next = command switch
        {
            GojoDrawKeyCommand.Previous => Document.PreviousPage(),
            GojoDrawKeyCommand.Next => Document.NextPage(),
            GojoDrawKeyCommand.First => Document.FirstPage(),
            GojoDrawKeyCommand.Last => Document.LastPage(),
            GojoDrawKeyCommand.ToggleNotes => Document.ToggleNotes(),
            GojoDrawKeyCommand.Close => Document.HideNotes(),
            GojoDrawKeyCommand.Reset => Document,
            _ => Document
        };

        if (!ReferenceEquals(next, Document) || command != GojoDrawKeyCommand.None)
        {
            var pageChanged = next.PageIndex != Document.PageIndex;
            Document = next;
            if (pageChanged || command == GojoDrawKeyCommand.Reset)
            {
                animationClock.Restart();
            }
            e.Handled = true;
        }
    }

    private static GojoDrawKeyInput ToGojoDrawKeyInput(KeyEventArgs e) =>
        new(ToGojoDrawKey(e.Key), ToGojoDrawModifiers(e.KeyModifiers));

    private static GojoDrawKey ToGojoDrawKey(Key key) =>
        key switch
        {
            Key.Left => GojoDrawKey.Left,
            Key.Right => GojoDrawKey.Right,
            Key.Up => GojoDrawKey.Up,
            Key.Down => GojoDrawKey.Down,
            Key.PageUp => GojoDrawKey.PageUp,
            Key.PageDown => GojoDrawKey.PageDown,
            Key.Home => GojoDrawKey.Home,
            Key.End => GojoDrawKey.End,
            Key.Space => GojoDrawKey.Space,
            Key.Enter => GojoDrawKey.Enter,
            Key.Escape => GojoDrawKey.Escape,
            Key.N => GojoDrawKey.N,
            Key.R => GojoDrawKey.R,
            _ => GojoDrawKey.Unknown
        };

    private static GojoDrawKeyModifiers ToGojoDrawModifiers(KeyModifiers modifiers)
    {
        var result = GojoDrawKeyModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= GojoDrawKeyModifiers.Shift;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= GojoDrawKeyModifiers.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= GojoDrawKeyModifiers.Alt;
        }

        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            result |= GojoDrawKeyModifiers.Meta;
        }

        return result;
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
    private static readonly SKColor Ink = new(47, 55, 65);
    private static readonly SKColor Muted = new(105, 112, 120);
    private static readonly SKColor Accent = new(66, 122, 153);
    private static readonly SKColor SoftLine = new(197, 200, 196);
    private readonly BlocksDocument document;
    private readonly GojoDrawAnimationFrame animationFrame;
    private readonly Action<IReadOnlyList<BlocksHitArea>> publishHitAreas;

    public BlocksDrawOperation(
        Rect bounds,
        BlocksDocument document,
        GojoDrawAnimationFrame animationFrame,
        Action<IReadOnlyList<BlocksHitArea>> publishHitAreas)
    {
        Bounds = bounds;
        this.document = document;
        this.animationFrame = animationFrame;
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
        DrawNotes(canvas, size, hitAreas);
        canvas.Restore();

        publishHitAreas(hitAreas);
    }

    private void DrawShell(SKCanvas canvas, SKSize size)
    {
        var topRail = new SKRect(0, 0, size.Width, 58);
        var bottomRail = new SKRect(0, Math.Max(0, size.Height - 66), size.Width, size.Height);

        using var railPaint = new SKPaint { Color = new SKColor(236, 235, 229), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var divider = new SKPaint { Color = SoftLine, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        canvas.DrawRect(topRail, railPaint);
        canvas.DrawRect(bottomRail, railPaint);
        canvas.DrawLine(0, topRail.Bottom, size.Width, topRail.Bottom, divider);
        canvas.DrawLine(0, bottomRail.Top, size.Width, bottomRail.Top, divider);

        var page = document.CurrentPage;
        using var title = TextPaint(18, Ink, bold: true);
        using var subtitle = TextPaint(12.5f, Muted);
        canvas.DrawText(page.Title, 28, 33, title);
        canvas.DrawText($"{document.PageIndex + 1}/{document.Pages.Count}  {page.Subtitle}", 28, 51, subtitle);
    }

    private void DrawPage(SKCanvas canvas, SKSize size)
    {
        var page = document.CurrentPage;
        var notesReserve = document.NotesVisible ? Math.Min(190, Math.Max(120, size.Height * 0.24f)) : 0;
        var pageArea = new SKRect(28, 82, Math.Max(29, size.Width - 28), Math.Max(83, size.Height - 88 - notesReserve));

        using var pageFill = new SKPaint { Color = new SKColor(255, 255, 252), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var pageStroke = new SKPaint { Color = new SKColor(215, 217, 210), Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f, IsAntialias = true };
        canvas.DrawRoundRect(pageArea, 8, 8, pageFill);
        canvas.DrawRoundRect(pageArea, 8, 8, pageStroke);

        if (page.Structure is { } structure)
        {
            using var paints = GojoCadSkiaTheme.Tutorial.CreatePaints();
            paints.AreaBoundary.Color = new SKColor(64, 76, 88, 226);
            paints.AreaBoundary.StrokeWidth = 2.2f;
            paints.SharedFramePoint.Color = new SKColor(64, 76, 88, 180);
            var structureArea = new SKRect(
                pageArea.Left + 34,
                pageArea.Top + 30,
                pageArea.Right - 34,
                pageArea.Bottom - 30);
            GojoRenderer.Draw(canvas, structure, structureArea, page.ProjectionSeed, page.ModelBounds, paints);
            DrawTutorialAnimationHint(canvas, structureArea);
        }
        else
        {
            DrawEmptyPageMark(canvas, pageArea);
        }
    }

    private void DrawTutorialAnimationHint(SKCanvas canvas, SKRect structureArea)
    {
        if (document.CurrentPage.TutorialScene?.HasAnimation != true)
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(66, 122, 153, 150),
            StrokeWidth = 2,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        var y = structureArea.Bottom + 14;
        var x0 = structureArea.Left;
        var x1 = structureArea.Right;
        canvas.DrawLine(x0, y, x1, y, paint);
        canvas.DrawCircle(x0 + (x1 - x0) * (float)animationFrame.LoopProgress, y, 4.5f, paint);
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
        var centerX = size.Width * 0.5f;
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

        var notes = new SKRect(size.Width - 112, y - 18, size.Width - 28, y + 18);
        using var noteText = TextPaint(12.5f, document.NotesVisible ? Accent : Muted, bold: true);
        using var noteStroke = new SKPaint
        {
            Color = document.NotesVisible ? Accent : new SKColor(172, 176, 174),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.1f,
            IsAntialias = true
        };
        canvas.DrawRoundRect(notes, 6, 6, noteStroke);
        canvas.DrawText("Notes", notes.Left + 20, notes.MidY + 4.5f, noteText);
        hitAreas.Add(new BlocksHitArea(notes, BlocksHitKind.ToggleNotes, -1));
    }

    private void DrawNotes(SKCanvas canvas, SKSize size, List<BlocksHitArea> hitAreas)
    {
        if (!document.NotesVisible)
        {
            return;
        }

        var height = Math.Min(190, Math.Max(120, size.Height * 0.24f));
        var drawer = new SKRect(28, size.Height - 80 - height, size.Width - 28, size.Height - 88);
        using var fill = new SKPaint { Color = new SKColor(255, 255, 252, 246), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var stroke = new SKPaint { Color = new SKColor(191, 195, 190), Style = SKPaintStyle.Stroke, StrokeWidth = 1.1f, IsAntialias = true };
        using var title = TextPaint(11.5f, Muted, bold: true);
        using var body = TextPaint(13.5f, Ink);

        canvas.DrawRoundRect(drawer, 8, 8, fill);
        canvas.DrawRoundRect(drawer, 8, 8, stroke);
        canvas.DrawText("Narration notes", drawer.Left + 18, drawer.Top + 24, title);
        DrawWrappedText(canvas, document.CurrentPage.Body, drawer.Left + 18, drawer.Top + 50, drawer.Width - 36, body, 19);
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
    PageDot,
    ToggleNotes
}
