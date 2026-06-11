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
            BlocksHitKind.ToggleSidePanel => Document.ToggleSidePanel(),
            BlocksHitKind.ToggleAudio => Document.ToggleAudio(),
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
            GojoDrawKeyCommand.TogglePanel => Document.ToggleSidePanel(),
            GojoDrawKeyCommand.ToggleAudio => Document.ToggleAudio(),
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
            Key.B => GojoDrawKey.B,
            Key.M => GojoDrawKey.M,
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
    private const float TopRailHeight = 58;
    private const float BottomRailHeight = 66;
    private const float SidePanelWidth = 196;
    private const float PageMargin = 28;
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
        DrawShell(canvas, size, hitAreas);
        DrawPage(canvas, size);
        DrawBottomNavigation(canvas, size, hitAreas);
        DrawNotes(canvas, size, hitAreas);
        canvas.Restore();

        publishHitAreas(hitAreas);
    }

    private void DrawShell(SKCanvas canvas, SKSize size, List<BlocksHitArea> hitAreas)
    {
        var topRail = new SKRect(0, 0, size.Width, TopRailHeight);
        var bottomRail = new SKRect(0, Math.Max(0, size.Height - BottomRailHeight), size.Width, size.Height);

        using var railPaint = new SKPaint { Color = new SKColor(236, 235, 229), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var divider = new SKPaint { Color = SoftLine, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        canvas.DrawRect(topRail, railPaint);
        canvas.DrawRect(bottomRail, railPaint);
        canvas.DrawLine(0, topRail.Bottom, size.Width, topRail.Bottom, divider);
        canvas.DrawLine(0, bottomRail.Top, size.Width, bottomRail.Top, divider);

        var sideButton = new SKRect(18, 13, 46, 41);
        DrawIconButton(canvas, sideButton, document.SidePanelVisible, DrawPanelIcon);
        hitAreas.Add(new BlocksHitArea(sideButton, BlocksHitKind.ToggleSidePanel, -1));

        var audioButton = new SKRect(size.Width - 52, 13, size.Width - 24, 41);
        DrawIconButton(canvas, audioButton, document.AudioEnabled, (c, r, p) => DrawAudioIcon(c, r, p, document.AudioEnabled));
        hitAreas.Add(new BlocksHitArea(audioButton, BlocksHitKind.ToggleAudio, -1));

        var notesButton = new SKRect(size.Width - 88, 13, size.Width - 60, 41);
        DrawIconButton(canvas, notesButton, document.NotesVisible, DrawNotesIcon);
        hitAreas.Add(new BlocksHitArea(notesButton, BlocksHitKind.ToggleNotes, -1));

        if (document.SidePanelVisible)
        {
            DrawSidePanel(canvas, size);
        }

        var page = document.CurrentPage;
        using var title = TextPaint(18, Ink, bold: true);
        using var subtitle = TextPaint(12.5f, Muted);
        var textX = ContentLeft() + 2;
        canvas.DrawText(page.Title, textX, 33, title);
        canvas.DrawText($"{document.PageIndex + 1}/{document.Pages.Count}  {page.Subtitle}", textX, 51, subtitle);
    }

    private void DrawSidePanel(SKCanvas canvas, SKSize size)
    {
        var panel = new SKRect(0, TopRailHeight, SidePanelWidth, Math.Max(TopRailHeight, size.Height - BottomRailHeight));
        using var fill = new SKPaint { Color = new SKColor(241, 240, 234), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var divider = new SKPaint { Color = SoftLine, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        using var label = TextPaint(11.5f, Muted, bold: true);
        using var text = TextPaint(12.5f, Ink);
        using var active = TextPaint(12.5f, Accent, bold: true);

        canvas.DrawRect(panel, fill);
        canvas.DrawLine(panel.Right, panel.Top, panel.Right, panel.Bottom, divider);
        canvas.DrawText("Tutorial", panel.Left + 20, panel.Top + 32, label);

        var y = panel.Top + 64;
        for (var index = 0; index < document.Pages.Count; index++)
        {
            var pageText = $"{index + 1}. {document.Pages[index].Title}";
            DrawClippedText(canvas, pageText, panel.Left + 20, y, panel.Width - 34, index == document.PageIndex ? active : text);
            y += 26;
        }

        using var hint = TextPaint(11.5f, Muted);
        var hintY = panel.Bottom - 82;
        canvas.DrawText("Keys", panel.Left + 20, hintY, label);
        canvas.DrawText("Left / Right: pages", panel.Left + 20, hintY + 22, hint);
        canvas.DrawText("N: notes   B: panel   M: audio", panel.Left + 20, hintY + 42, hint);
    }

    private void DrawPage(SKCanvas canvas, SKSize size)
    {
        var page = document.CurrentPage;
        var notesReserve = document.NotesVisible ? Math.Min(190, Math.Max(120, size.Height * 0.24f)) : 0;
        var pageArea = new SKRect(ContentLeft(), 82, Math.Max(ContentLeft() + 1, size.Width - PageMargin), Math.Max(83, size.Height - 88 - notesReserve));

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
            DrawVisualCues(canvas, page.TutorialScene, structureArea);
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

    private void DrawVisualCues(SKCanvas canvas, BlocksTutorialScene? scene, SKRect structureArea)
    {
        if (scene is null)
        {
            return;
        }

        var loopElapsed = TimeSpan.FromSeconds(animationFrame.LoopSeconds);
        foreach (var cue in scene.VisualCues)
        {
            if (cue.Track is null)
            {
                continue;
            }

            var progress = GojoDrawAnimationClock.ProgressFor(cue.Track, loopElapsed);
            if (progress <= 0 || progress >= 1 && !scene.Loops)
            {
                continue;
            }

            var start = cue.Track.Start.In(structureArea);
            var end = cue.Track.End.In(structureArea);
            var point = Lerp(start, end, (float)progress);
            switch (cue.Kind)
            {
                case GojoDrawVisualCueKind.Pulse:
                    DrawPulse(canvas, point, cue.Strength, (float)progress);
                    break;
                case GojoDrawVisualCueKind.TensionGlow:
                    DrawTensionGlow(canvas, point, cue.Strength, (float)progress);
                    break;
                case GojoDrawVisualCueKind.FlowArrow:
                    DrawFlowArrow(canvas, start, end, (float)progress);
                    break;
                case GojoDrawVisualCueKind.ResidueChip:
                    DrawResidueChip(canvas, point, cue.Strength);
                    break;
            }
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
        var centerX = (ContentLeft() + size.Width) * 0.5f;
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

    private void DrawNotes(SKCanvas canvas, SKSize size, List<BlocksHitArea> hitAreas)
    {
        if (!document.NotesVisible)
        {
            return;
        }

        var height = Math.Min(190, Math.Max(120, size.Height * 0.24f));
        var drawer = new SKRect(ContentLeft(), size.Height - 80 - height, size.Width - PageMargin, size.Height - 88);
        using var fill = new SKPaint { Color = new SKColor(255, 255, 252, 246), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var stroke = new SKPaint { Color = new SKColor(191, 195, 190), Style = SKPaintStyle.Stroke, StrokeWidth = 1.1f, IsAntialias = true };
        using var title = TextPaint(11.5f, Muted, bold: true);
        using var body = TextPaint(13.5f, Ink);

        canvas.DrawRoundRect(drawer, 8, 8, fill);
        canvas.DrawRoundRect(drawer, 8, 8, stroke);
        canvas.DrawText("Narration notes", drawer.Left + 18, drawer.Top + 24, title);
        DrawWrappedText(canvas, document.CurrentPage.Body, drawer.Left + 18, drawer.Top + 50, drawer.Width - 36, body, 19);
    }

    private float ContentLeft() =>
        document.SidePanelVisible ? SidePanelWidth + PageMargin : PageMargin;

    private static void DrawIconButton(SKCanvas canvas, SKRect bounds, bool active, Action<SKCanvas, SKRect, SKPaint> drawIcon)
    {
        using var fill = new SKPaint
        {
            Color = active ? new SKColor(255, 255, 252) : new SKColor(236, 235, 229),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var stroke = new SKPaint
        {
            Color = active ? Accent : new SKColor(174, 178, 176),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.15f,
            IsAntialias = true
        };

        canvas.DrawRoundRect(bounds, 6, 6, fill);
        canvas.DrawRoundRect(bounds, 6, 6, stroke);
        drawIcon(canvas, bounds, stroke);
    }

    private static void DrawPanelIcon(SKCanvas canvas, SKRect bounds, SKPaint paint)
    {
        var inset = 7f;
        var rect = new SKRect(bounds.Left + inset, bounds.Top + 7, bounds.Right - inset, bounds.Bottom - 7);
        canvas.DrawRect(rect, paint);
        canvas.DrawLine(rect.Left + rect.Width * 0.38f, rect.Top, rect.Left + rect.Width * 0.38f, rect.Bottom, paint);
    }

    private static void DrawNotesIcon(SKCanvas canvas, SKRect bounds, SKPaint paint)
    {
        var rect = new SKRect(bounds.Left + 8, bounds.Top + 6.5f, bounds.Right - 8, bounds.Bottom - 6.5f);
        canvas.DrawRect(rect, paint);
        canvas.DrawLine(rect.Left + 4, rect.Top + 7, rect.Right - 4, rect.Top + 7, paint);
        canvas.DrawLine(rect.Left + 4, rect.Top + 12, rect.Right - 5, rect.Top + 12, paint);
    }

    private static void DrawAudioIcon(SKCanvas canvas, SKRect bounds, SKPaint paint, bool enabled)
    {
        var midY = bounds.MidY;
        using var path = new SKPath();
        path.MoveTo(bounds.Left + 8, midY - 5);
        path.LineTo(bounds.Left + 13, midY - 5);
        path.LineTo(bounds.Left + 19, midY - 10);
        path.LineTo(bounds.Left + 19, midY + 10);
        path.LineTo(bounds.Left + 13, midY + 5);
        path.LineTo(bounds.Left + 8, midY + 5);
        path.Close();
        canvas.DrawPath(path, paint);
        if (enabled)
        {
            canvas.DrawArc(new SKRect(bounds.Left + 17, midY - 10, bounds.Right - 6, midY + 10), -35, 70, false, paint);
        }
        else
        {
            canvas.DrawLine(bounds.Right - 10, midY - 8, bounds.Right - 4, midY + 8, paint);
            canvas.DrawLine(bounds.Right - 4, midY - 8, bounds.Right - 10, midY + 8, paint);
        }
    }

    private static void DrawPulse(SKCanvas canvas, SKPoint point, float strength, float progress)
    {
        using var fill = new SKPaint { Color = new SKColor(66, 122, 153, 170), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var stroke = new SKPaint { Color = new SKColor(66, 122, 153, 90), Style = SKPaintStyle.Stroke, StrokeWidth = 1.4f, IsAntialias = true };
        canvas.DrawCircle(point, 5.5f * strength, fill);
        canvas.DrawCircle(point, 12f + progress * 10f, stroke);
    }

    private static void DrawTensionGlow(SKCanvas canvas, SKPoint point, float strength, float progress)
    {
        using var fill = new SKPaint { Color = new SKColor(195, 63, 61, (byte)(72 + progress * 80)), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawCircle(point, 18f * strength + progress * 8f, fill);
    }

    private static void DrawFlowArrow(SKCanvas canvas, SKPoint start, SKPoint end, float progress)
    {
        using var paint = new SKPaint { Color = new SKColor(49, 132, 104, 180), Style = SKPaintStyle.Stroke, StrokeWidth = 2f, StrokeCap = SKStrokeCap.Round, IsAntialias = true };
        var tip = Lerp(start, end, progress);
        canvas.DrawLine(start, tip, paint);
        DrawArrowHead(canvas, start, tip, paint);
    }

    private static void DrawResidueChip(SKCanvas canvas, SKPoint point, float strength)
    {
        using var fill = new SKPaint { Color = new SKColor(183, 111, 57, 185), Style = SKPaintStyle.Fill, IsAntialias = true };
        var r = 6f * strength;
        using var path = new SKPath();
        path.MoveTo(point.X, point.Y - r);
        path.LineTo(point.X + r, point.Y);
        path.LineTo(point.X, point.Y + r);
        path.LineTo(point.X - r, point.Y);
        path.Close();
        canvas.DrawPath(path, fill);
    }

    private static void DrawArrowHead(SKCanvas canvas, SKPoint start, SKPoint end, SKPaint paint)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001f)
        {
            return;
        }

        var ux = dx / length;
        var uy = dy / length;
        var px = -uy;
        var py = ux;
        var size = 7f;
        canvas.DrawLine(end.X, end.Y, end.X - ux * size + px * size * 0.45f, end.Y - uy * size + py * size * 0.45f, paint);
        canvas.DrawLine(end.X, end.Y, end.X - ux * size - px * size * 0.45f, end.Y - uy * size - py * size * 0.45f, paint);
    }

    private static SKPoint Lerp(SKPoint start, SKPoint end, float progress) =>
        new(start.X + (end.X - start.X) * progress, start.Y + (end.Y - start.Y) * progress);

    private static void DrawClippedText(SKCanvas canvas, string text, float x, float y, float width, SKPaint paint)
    {
        var clipped = text;
        while (clipped.Length > 1 && paint.MeasureText(clipped) > width)
        {
            clipped = clipped[..^1];
        }

        if (clipped.Length < text.Length && clipped.Length > 1)
        {
            clipped = clipped[..^1] + ".";
        }

        canvas.DrawText(clipped, x, y, paint);
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
    ToggleNotes,
    ToggleSidePanel,
    ToggleAudio
}
