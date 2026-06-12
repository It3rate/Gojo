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
    private bool draggingPhysicalPoint;

    public event EventHandler<BlocksDocument?>? DocumentChanged;

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
            if (ShouldAnimate())
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
            var shouldRestart = document is null ||
                value is null ||
                document.PageIndex != value.PageIndex;
            document = value;
            if (shouldRestart)
            {
                animationClock.Restart();
            }

            InvalidateVisual();
            DocumentChanged?.Invoke(this, document);
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
        if (hit.Kind == BlocksHitKind.DragPhysicalPoint)
        {
            Document = Document.StartPhysicalPointInteraction(PhysicalPointTFromPosition(point));
            draggingPhysicalPoint = true;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        Document = hit.Kind switch
        {
            BlocksHitKind.PreviousPage => Document.PreviousPage(),
            BlocksHitKind.NextPage => Document.NextPage(),
            BlocksHitKind.PageDot => Document.SelectPage(hit.PageIndex),
            BlocksHitKind.ToggleNotes => Document.ToggleNotes(),
            BlocksHitKind.ToggleSidePanel => Document.ToggleSidePanel(),
            BlocksHitKind.ToggleAudio => Document.ToggleAudio(),
            BlocksHitKind.ToggleGlossary => Document.ToggleGlossary(),
            BlocksHitKind.ToggleTheme => Document.ToggleTheme(),
            BlocksHitKind.ResetPresentation => Document.ResetCurrentPage(),
            _ => Document
        };
        if (hit.Kind != BlocksHitKind.None)
        {
            animationClock.Restart();
        }
        e.Handled = hit.Kind != BlocksHitKind.None;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!draggingPhysicalPoint || Document is null)
        {
            return;
        }

        Document = Document.DragPhysicalPoint(PhysicalPointTFromPosition(e.GetPosition(this)));
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!draggingPhysicalPoint)
        {
            return;
        }

        draggingPhysicalPoint = false;
        e.Pointer.Capture(null);
        e.Handled = true;
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
            GojoDrawKeyCommand.ToggleGlossary => Document.ToggleGlossary(),
            GojoDrawKeyCommand.ToggleTheme => Document.ToggleTheme(),
            GojoDrawKeyCommand.Close => Document.HideNotes(),
            GojoDrawKeyCommand.Reset => Document.ResetCurrentPage(),
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
            Key.G => GojoDrawKey.G,
            Key.M => GojoDrawKey.M,
            Key.N => GojoDrawKey.N,
            Key.R => GojoDrawKey.R,
            Key.T => GojoDrawKey.T,
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

    private bool ShouldAnimate()
    {
        if (Document is not { } current ||
            current.PhysicalPointInteractive ||
            current.CurrentPage.TutorialScene is not { } scene)
        {
            return false;
        }

        return scene.Loops || animationClock.Elapsed <= scene.Duration;
    }

    private double PhysicalPointTFromPosition(Point point)
    {
        if (Document is null)
        {
            return 0.5d;
        }

        var size = new SKSize((float)Bounds.Width, (float)Bounds.Height);
        var pageArea = BlocksLayout.PageArea(size, Document);
        var structureArea = BlocksLayout.StructureArea(pageArea);
        var (start, end) = BlocksLayout.PhysicalLine(structureArea);
        var width = Math.Max(1f, end.X - start.X);
        return Math.Clamp(((float)point.X - start.X) / width, 0f, 1f);
    }
}

internal static class BlocksLayout
{
    public const float TopRailHeight = 58;
    public const float BottomRailHeight = 66;
    public const float SidePanelWidth = 196;
    public const float PageMargin = 28;

    public static float ContentLeft(bool sidePanelVisible) =>
        sidePanelVisible ? SidePanelWidth + PageMargin : PageMargin;

    public static SKRect PageArea(SKSize size, BlocksDocument document)
    {
        var notesReserve = document.NotesVisible ? Math.Min(190, Math.Max(120, size.Height * 0.24f)) : 0;
        var left = ContentLeft(document.SidePanelVisible);
        return new SKRect(left, 82, Math.Max(left + 1, size.Width - PageMargin), Math.Max(83, size.Height - 88 - notesReserve));
    }

    public static SKRect StructureArea(SKRect pageArea) =>
        new(pageArea.Left + 34, pageArea.Top + 30, pageArea.Right - 34, pageArea.Bottom - 30);

    public static (SKPoint Start, SKPoint End) PhysicalLine(SKRect structureArea)
    {
        var y = structureArea.MidY;
        return (
            new SKPoint(structureArea.Left + structureArea.Width * 0.16f, y),
            new SKPoint(structureArea.Right - structureArea.Width * 0.16f, y));
    }
}

internal sealed class BlocksDrawOperation : ICustomDrawOperation
{
    private static readonly GojoCadSkiaSceneRenderer GojoRenderer = new();
    private readonly BlocksDocument document;
    private readonly BlocksTheme theme;
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
        theme = document.CurrentTheme;
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
        canvas.Clear(theme.Background);
        canvas.ClipRect(new SKRect(0, 0, size.Width, size.Height));
        DrawShell(canvas, size, hitAreas);
        DrawPage(canvas, size, hitAreas);
        DrawBottomNavigation(canvas, size, hitAreas);
        canvas.Restore();

        publishHitAreas(hitAreas);
    }

    private void DrawShell(SKCanvas canvas, SKSize size, List<BlocksHitArea> hitAreas)
    {
        var topRail = new SKRect(0, 0, size.Width, BlocksLayout.TopRailHeight);
        var bottomRail = new SKRect(0, Math.Max(0, size.Height - BlocksLayout.BottomRailHeight), size.Width, size.Height);

        using var railPaint = new SKPaint { Color = theme.RailFill, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var divider = new SKPaint { Color = theme.SoftLine, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        canvas.DrawRect(topRail, railPaint);
        canvas.DrawRect(bottomRail, railPaint);
        canvas.DrawLine(0, topRail.Bottom, size.Width, topRail.Bottom, divider);
        canvas.DrawLine(0, bottomRail.Top, size.Width, bottomRail.Top, divider);

        var sideButton = new SKRect(18, 13, 46, 41);
        DrawIconButton(canvas, sideButton, document.SidePanelVisible, DrawPanelIcon);
        hitAreas.Add(new BlocksHitArea(sideButton, BlocksHitKind.ToggleSidePanel, -1));

        var glossaryButton = new SKRect(56, 13, 84, 41);
        DrawIconButton(canvas, glossaryButton, document.GlossaryVisible, DrawGlossaryIcon);
        hitAreas.Add(new BlocksHitArea(glossaryButton, BlocksHitKind.ToggleGlossary, -1));

        var audioButton = new SKRect(size.Width - 52, 13, size.Width - 24, 41);
        DrawIconButton(canvas, audioButton, document.AudioEnabled, (c, r, p) => DrawAudioIcon(c, r, p, document.AudioEnabled));
        hitAreas.Add(new BlocksHitArea(audioButton, BlocksHitKind.ToggleAudio, -1));

        var notesButton = new SKRect(size.Width - 88, 13, size.Width - 60, 41);
        DrawIconButton(canvas, notesButton, document.NotesVisible, DrawNotesIcon);
        hitAreas.Add(new BlocksHitArea(notesButton, BlocksHitKind.ToggleNotes, -1));

        var resetButton = new SKRect(size.Width - 124, 13, size.Width - 96, 41);
        DrawIconButton(canvas, resetButton, false, DrawResetIcon);
        hitAreas.Add(new BlocksHitArea(resetButton, BlocksHitKind.ResetPresentation, -1));

        var themeButton = new SKRect(size.Width - 160, 13, size.Width - 132, 41);
        DrawIconButton(canvas, themeButton, false, DrawThemeIcon);
        hitAreas.Add(new BlocksHitArea(themeButton, BlocksHitKind.ToggleTheme, -1));

        if (document.SidePanelVisible)
        {
            DrawSidePanel(canvas, size);
        }

        var page = document.CurrentPage;
        using var title = TextPaint(18, theme.Ink, bold: true);
        using var subtitle = TextPaint(12.5f, theme.Muted);
        var textX = ContentLeft() + 2;
        canvas.DrawText(page.Title, textX, 33, title);
        var mode = document.GlossaryVisible ? "Glossary" : "Tutorial";
        canvas.DrawText($"{mode} {document.PageIndex + 1}/{document.Pages.Count}  {page.Subtitle}  -  {theme.Label}", textX, 51, subtitle);
    }

    private void DrawSidePanel(SKCanvas canvas, SKSize size)
    {
        var panel = new SKRect(0, BlocksLayout.TopRailHeight, BlocksLayout.SidePanelWidth, Math.Max(BlocksLayout.TopRailHeight, size.Height - BlocksLayout.BottomRailHeight));
        using var fill = new SKPaint { Color = theme.PanelFill, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var divider = new SKPaint { Color = theme.SoftLine, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        using var label = TextPaint(11.5f, theme.Muted, bold: true);
        using var text = TextPaint(12.5f, theme.Ink);

        canvas.DrawRect(panel, fill);
        canvas.DrawLine(panel.Right, panel.Top, panel.Right, panel.Bottom, divider);
        canvas.DrawText("Concepts", panel.Left + 20, panel.Top + 32, label);

        var y = panel.Top + 64;
        foreach (var concept in document.CurrentPage.Concepts)
        {
            using var marker = new SKPaint { Color = theme.Accent, Style = SKPaintStyle.Fill, IsAntialias = true };
            canvas.DrawCircle(panel.Left + 23, y - 4, 2.8f, marker);
            DrawClippedText(canvas, concept, panel.Left + 34, y, panel.Width - 50, text);
            y += 24;
        }

        using var hint = TextPaint(11.5f, theme.Muted);
        var hintY = panel.Bottom - 82;
        canvas.DrawText("Keys", panel.Left + 20, hintY, label);
        canvas.DrawText("Left / Right: pages", panel.Left + 20, hintY + 22, hint);
        canvas.DrawText("G: glossary   T: theme", panel.Left + 20, hintY + 42, hint);
    }

    private void DrawPage(SKCanvas canvas, SKSize size, List<BlocksHitArea> hitAreas)
    {
        var page = document.CurrentPage;
        var pageArea = BlocksLayout.PageArea(size, document);

        using var pageFill = new SKPaint { Color = theme.PageFill, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var pageStroke = new SKPaint { Color = theme.PageStroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1.2f, IsAntialias = true };
        canvas.DrawRoundRect(pageArea, 8, 8, pageFill);
        canvas.DrawRoundRect(pageArea, 8, 8, pageStroke);

        var structureArea = BlocksLayout.StructureArea(pageArea);
        if (page.IsGlossary)
        {
            DrawGlossaryPage(canvas, page, structureArea);
        }
        else if (page.Id == BlocksDocument.PhysicalStructurePageId)
        {
            DrawPhysicalStructurePage(canvas, structureArea, hitAreas);
        }
        else if (document.CurrentStructure is { } structure)
        {
            using var paints = theme.CadTheme.CreatePaints();
            paints.AreaBoundary.Color = WithAlpha(theme.Ink, 226);
            paints.AreaBoundary.StrokeWidth = 2.2f;
            paints.SharedFramePoint.Color = WithAlpha(theme.Ink, 180);
            GojoRenderer.Draw(canvas, structure, structureArea, page.ProjectionSeed, page.ModelBounds, paints);
            DrawVisualCues(canvas, page.TutorialScene, structureArea);
        }
        else
        {
            DrawEmptyPageMark(canvas, pageArea);
        }
    }

    private void DrawPhysicalStructurePage(SKCanvas canvas, SKRect structureArea, List<BlocksHitArea> hitAreas)
    {
        var pointT = document.PhysicalPointInteractive
            ? document.PhysicalPointT
            : AnimatedPhysicalPointT();
        var (start, end) = BlocksLayout.PhysicalLine(structureArea);
        var point = Lerp(start, end, (float)pointT);
        var loop = document.PhysicalPointInteractive ? 1f : (float)animationFrame.LoopProgress;

        using var railShadow = new SKPaint
        {
            Color = WithAlpha(theme.SoftLine, 130),
            StrokeWidth = 10,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var rail = new SKPaint
        {
            Color = WithAlpha(theme.Ink, 226),
            StrokeWidth = 3,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        using var endpoint = new SKPaint { Color = WithAlpha(theme.Ink, 190), Style = SKPaintStyle.Fill, IsAntialias = true };
        canvas.DrawLine(start, end, railShadow);
        canvas.DrawLine(start, end, rail);
        canvas.DrawCircle(start, 5, endpoint);
        canvas.DrawCircle(end, 5, endpoint);

        DrawRadiatingFlow(canvas, point, loop, theme);

        using var pointFill = new SKPaint
        {
            Color = document.PhysicalPointInteractive ? theme.Accent : theme.CadTheme.Flow,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var pointStroke = new SKPaint
        {
            Color = theme.PageFill,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            IsAntialias = true
        };
        canvas.DrawCircle(point, 11, pointFill);
        canvas.DrawCircle(point, 11, pointStroke);

        using var tick = new SKPaint
        {
            Color = WithAlpha(theme.Accent, 120),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };
        canvas.DrawLine(point.X, point.Y - 34, point.X, point.Y + 34, tick);

        hitAreas.Add(new BlocksHitArea(new SKRect(point.X - 22, point.Y - 22, point.X + 22, point.Y + 22), BlocksHitKind.DragPhysicalPoint, -1));
    }

    private double AnimatedPhysicalPointT() =>
        0.5d + Math.Sin(animationFrame.LoopProgress * Math.PI * 2d) * 0.34d;

    private void DrawGlossaryPage(SKCanvas canvas, BlocksPage page, SKRect area)
    {
        var items = page.GlossaryItems ?? [];
        if (items.Count == 0)
        {
            DrawEmptyPageMark(canvas, area);
            return;
        }

        var columns = area.Width >= 760 ? 4 : area.Width >= 540 ? 3 : 2;
        var rows = (int)Math.Ceiling(items.Count / (double)columns);
        var gap = 14f;
        var cellWidth = (area.Width - gap * (columns - 1)) / columns;
        var cellHeight = (area.Height - gap * (rows - 1)) / rows;
        using var cardFill = new SKPaint { Color = WithAlpha(theme.RailFill, 170), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var cardStroke = new SKPaint { Color = theme.PageStroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
        using var title = TextPaint(12.5f, theme.Ink, bold: true);

        for (var index = 0; index < items.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var left = area.Left + column * (cellWidth + gap);
            var top = area.Top + row * (cellHeight + gap);
            var cell = new SKRect(left, top, left + cellWidth, top + cellHeight);
            canvas.DrawRoundRect(cell, 8, 8, cardFill);
            canvas.DrawRoundRect(cell, 8, 8, cardStroke);

            var item = items[index];
            DrawClippedText(canvas, item.Title, cell.Left + 12, cell.Top + 23, cell.Width - 24, title);
            var diagram = new SKRect(cell.Left + 12, cell.Top + 34, cell.Right - 12, cell.Bottom - 10);
            using var paints = theme.CadTheme.CreatePaints();
            GojoRenderer.Draw(canvas, item.Structure, diagram, item.ProjectionSeed, item.ModelBounds, paints);
        }
    }

    private static void DrawRadiatingFlow(SKCanvas canvas, SKPoint point, float loop, BlocksTheme theme)
    {
        for (var index = 0; index < 3; index++)
        {
            var phase = (loop + index / 3f) % 1f;
            var radius = 20f + phase * 48f;
            var alpha = (byte)(92 * (1f - phase));
            using var ring = new SKPaint
            {
                Color = WithAlpha(theme.CadTheme.Flow, alpha),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2f,
                IsAntialias = true
            };
            canvas.DrawCircle(point, radius, ring);
        }

        using var spoke = new SKPaint
        {
            Color = WithAlpha(theme.CadTheme.Flow, 96),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.4f,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };
        for (var index = 0; index < 8; index++)
        {
            var angle = index * MathF.PI * 0.25f;
            var inner = 18f;
            var outer = 34f + 8f * MathF.Sin(loop * MathF.PI * 2f + index);
            canvas.DrawLine(
                point.X + MathF.Cos(angle) * inner,
                point.Y + MathF.Sin(angle) * inner,
                point.X + MathF.Cos(angle) * outer,
                point.Y + MathF.Sin(angle) * outer,
                spoke);
        }
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
            Color = WithAlpha(theme.Muted, 90),
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
        var available = Math.Max(120f, size.Width - ContentLeft() - 160f);
        var dotSpacing = document.Pages.Count <= 1
            ? 0f
            : Math.Clamp(available / (document.Pages.Count - 1), 8f, 18f);
        var dotStart = centerX - (document.Pages.Count - 1) * dotSpacing * 0.5f;

        using var dotPaint = new SKPaint { Color = theme.Muted, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var activeDotPaint = new SKPaint { Color = theme.Accent, Style = SKPaintStyle.Fill, IsAntialias = true };
        using var arrowPaint = new SKPaint
        {
            Color = theme.Ink,
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
            var radius = index == document.PageIndex ? 4.9f : 3.2f;
            canvas.DrawCircle(x, y, radius, index == document.PageIndex ? activeDotPaint : dotPaint);
            hitAreas.Add(new BlocksHitArea(new SKRect(x - 9, y - 9, x + 9, y + 9), BlocksHitKind.PageDot, index));
        }
    }

    private float ContentLeft() =>
        BlocksLayout.ContentLeft(document.SidePanelVisible);

    private void DrawIconButton(SKCanvas canvas, SKRect bounds, bool active, Action<SKCanvas, SKRect, SKPaint> drawIcon)
    {
        using var fill = new SKPaint
        {
            Color = active ? theme.PageFill : theme.RailFill,
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var stroke = new SKPaint
        {
            Color = active ? theme.Accent : theme.Muted,
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

    private static void DrawGlossaryIcon(SKCanvas canvas, SKRect bounds, SKPaint paint)
    {
        var left = bounds.Left + 7;
        var top = bounds.Top + 7;
        var size = 5.5f;
        var gap = 4.2f;
        for (var row = 0; row < 2; row++)
        for (var column = 0; column < 3; column++)
        {
            var cell = new SKRect(
                left + column * (size + gap),
                top + row * (size + gap),
                left + column * (size + gap) + size,
                top + row * (size + gap) + size);
            canvas.DrawRect(cell, paint);
        }
    }

    private static void DrawThemeIcon(SKCanvas canvas, SKRect bounds, SKPaint paint)
    {
        var center = new SKPoint(bounds.MidX, bounds.MidY);
        var radius = 8.5f;
        canvas.DrawCircle(center, radius, paint);
        canvas.DrawLine(center.X, center.Y - radius, center.X, center.Y + radius, paint);
        canvas.DrawArc(new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius), -90, 180, false, paint);
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

    private static void DrawResetIcon(SKCanvas canvas, SKRect bounds, SKPaint paint)
    {
        var arcBounds = new SKRect(bounds.Left + 7, bounds.Top + 7, bounds.Right - 7, bounds.Bottom - 7);
        canvas.DrawArc(arcBounds, 35, 285, false, paint);
        using var path = new SKPath();
        path.MoveTo(bounds.Left + 10, bounds.Top + 9);
        path.LineTo(bounds.Left + 10, bounds.Top + 16);
        path.LineTo(bounds.Left + 17, bounds.Top + 13);
        canvas.DrawPath(path, paint);
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

    private static SKColor WithAlpha(SKColor color, byte alpha) =>
        new(color.Red, color.Green, color.Blue, alpha);

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
    ToggleAudio,
    ToggleGlossary,
    ToggleTheme,
    ResetPresentation,
    DragPhysicalPoint
}
