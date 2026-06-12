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
    private static readonly TimeSpan HoverDelay = TimeSpan.FromMilliseconds(420);
    private readonly Stopwatch animationClock = Stopwatch.StartNew();
    private readonly DispatcherTimer animationTimer;
    private readonly DispatcherTimer hoverTimer;
    private BlocksDocument? document;
    private BlocksHitArea[] hitAreas = [];
    private BlocksHitArea? hoverCandidate;
    private bool hoverReady;
    private Point hoverPoint;
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
        hoverTimer = new DispatcherTimer
        {
            Interval = HoverDelay
        };
        hoverTimer.Tick += (_, _) =>
        {
            hoverTimer.Stop();
            if (hoverCandidate is not null)
            {
                hoverReady = true;
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
                ClearHover();
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
            CurrentHoverTip(),
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
        hoverTimer.Stop();
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
        if (hit.Kind != BlocksHitKind.None &&
            hit.Kind != BlocksHitKind.GlossaryTooltip)
        {
            animationClock.Restart();
        }
        e.Handled = hit.Kind != BlocksHitKind.None &&
            hit.Kind != BlocksHitKind.GlossaryTooltip;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetPosition(this);
        if (!draggingPhysicalPoint || Document is null)
        {
            UpdateHover(point);
            return;
        }

        ClearHover();
        Document = Document.DragPhysicalPoint(PhysicalPointTFromPosition(point));
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

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ClearHover();
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

    private void UpdateHover(Point point)
    {
        hoverPoint = point;
        var hit = hitAreas.FirstOrDefault(area =>
            area.Kind == BlocksHitKind.GlossaryTooltip &&
            area.Bounds.Contains((float)point.X, (float)point.Y));
        if (hit.Kind != BlocksHitKind.GlossaryTooltip ||
            string.IsNullOrWhiteSpace(hit.TooltipBody))
        {
            ClearHover();
            return;
        }

        if (hoverCandidate is { } current &&
            current.TooltipTitle == hit.TooltipTitle &&
            current.TooltipBody == hit.TooltipBody)
        {
            if (hoverReady)
            {
                InvalidateVisual();
            }
            return;
        }

        hoverCandidate = hit;
        hoverReady = false;
        hoverTimer.Stop();
        hoverTimer.Start();
    }

    private void ClearHover()
    {
        if (hoverCandidate is null && !hoverReady)
        {
            return;
        }

        hoverCandidate = null;
        hoverReady = false;
        hoverTimer.Stop();
        InvalidateVisual();
    }

    private BlocksHoverTip? CurrentHoverTip() =>
        hoverReady &&
        hoverCandidate is { TooltipTitle: { } title, TooltipBody: { } body } &&
        !string.IsNullOrWhiteSpace(body)
            ? new BlocksHoverTip(new SKPoint((float)hoverPoint.X, (float)hoverPoint.Y), title, body)
            : null;

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
    private static readonly SKColor SiteColor = new(95, 99, 100);
    private static readonly SKColor PositiveColor = new(53, 199, 117);
    private static readonly SKColor PositiveStroke = new(4, 136, 104);
    private static readonly SKColor InverseColor = new(255, 96, 101);
    private static readonly SKColor InverseStroke = new(174, 58, 47);
    private static readonly SKColor BoundaryColor = new(144, 149, 149);
    private static readonly SKColor BoundaryMarkerColor = new(92, 101, 207);
    private static readonly SKColor EvidenceColor = new(80, 91, 224);
    private static readonly SKColor EndpointColor = new(200, 63, 21);
    private static readonly SKColor TickColor = new(175, 180, 180);
    private readonly BlocksDocument document;
    private readonly BlocksTheme theme;
    private readonly GojoDrawAnimationFrame animationFrame;
    private readonly BlocksHoverTip? hoverTip;
    private readonly Action<IReadOnlyList<BlocksHitArea>> publishHitAreas;

    private enum GlossaryLadderRow
    {
        RadialPositive,
        RadialInverse,
        Linear
    }

    public BlocksDrawOperation(
        Rect bounds,
        BlocksDocument document,
        GojoDrawAnimationFrame animationFrame,
        BlocksHoverTip? hoverTip,
        Action<IReadOnlyList<BlocksHitArea>> publishHitAreas)
    {
        Bounds = bounds;
        this.document = document;
        theme = document.CurrentTheme;
        this.animationFrame = animationFrame;
        this.hoverTip = hoverTip;
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
        DrawHoverTip(canvas, size);
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
            DrawGlossaryPage(canvas, page, structureArea, hitAreas);
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

    private void DrawGlossaryPage(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        if (page.GlossarySheet != BlocksGlossarySheetKind.None)
        {
            DrawGlossaryLadder(canvas, page, area, hitAreas);
            return;
        }

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
            AddGlossaryTooltipHit(hitAreas, Inset(diagram, 2), item.Id, item.Title, page);
        }
    }

    private void DrawGlossaryLadder(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var sheet = page.GlossarySheet;
        string[] columns = sheet == BlocksGlossarySheetKind.SourceToSupport
            ? ["site", "flow", "boundary", "bounded support", "partitioned"]
            : ["direction", "directed boundary", "directed support", "area", "area face / spin"];
        string[] columnKeys = sheet == BlocksGlossarySheetKind.SourceToSupport
            ? ["site", "flow", "boundary", "bounded-support", "partitioned"]
            : ["direction", "directed-boundary", "directed-support", "area", "area-face-spin"];
        string[] rows = ["radial positive", "radial inverse", "linear"];
        string[] rowKeys = ["radial-positive", "radial-inverse", "linear"];
        var labelWidth = Math.Clamp(area.Width * 0.16f, 98f, 138f);
        var headerHeight = 38f;
        var gap = 12f;
        var cellWidth = (area.Width - labelWidth - gap * (columns.Length - 1)) / columns.Length;
        var cellHeight = (area.Height - headerHeight - gap * (rows.Length - 1)) / rows.Length;

        using var headerPaint = TextPaint(12.5f, theme.Ink, bold: true);
        using var rowPaint = TextPaint(12.5f, theme.Ink);
        using var gridPaint = new SKPaint { Color = WithAlpha(theme.SoftLine, 95), Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };

        for (var column = 0; column < columns.Length; column++)
        {
            var x = area.Left + labelWidth + column * (cellWidth + gap);
            DrawCenteredText(canvas, columns[column], new SKRect(x, area.Top + 4, x + cellWidth, area.Top + headerHeight), headerPaint);
        }

        for (var row = 0; row < rows.Length; row++)
        {
            var y = area.Top + headerHeight + row * (cellHeight + gap);
            DrawRightAlignedText(canvas, rows[row], new SKRect(area.Left, y, area.Left + labelWidth - 16, y + cellHeight), rowPaint);
            for (var column = 0; column < columns.Length; column++)
            {
                var x = area.Left + labelWidth + column * (cellWidth + gap);
                var cell = new SKRect(x, y, x + cellWidth, y + cellHeight);
                canvas.DrawRoundRect(cell, 6, 6, gridPaint);
                DrawGlossaryLadderCell(canvas, sheet, (GlossaryLadderRow)row, column, cell);
                var title = $"{TitleCase(rows[row])} {TitleCase(columns[column])}";
                var id = $"{GlossarySheetKey(sheet)}-{rowKeys[row]}-{columnKeys[column]}";
                AddGlossaryTooltipHit(hitAreas, Inset(cell, 8), id, title, page);
            }
        }
    }

    private static void AddGlossaryTooltipHit(
        List<BlocksHitArea> hitAreas,
        SKRect bounds,
        string id,
        string title,
        BlocksPage page)
    {
        var body = page.GlossaryDescriptionFor(id);
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        hitAreas.Add(new BlocksHitArea(bounds, BlocksHitKind.GlossaryTooltip, -1, title, body));
    }

    private static string GlossarySheetKey(BlocksGlossarySheetKind sheet) =>
        sheet == BlocksGlossarySheetKind.SourceToSupport
            ? "source-support"
            : "direction-area";

    private static string TitleCase(string value) =>
        string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    private void DrawGlossaryLadderCell(
        SKCanvas canvas,
        BlocksGlossarySheetKind sheet,
        GlossaryLadderRow row,
        int column,
        SKRect cell)
    {
        var inner = Inset(cell, 10);
        if (sheet == BlocksGlossarySheetKind.SourceToSupport)
        {
            switch (column)
            {
                case 0:
                    DrawLadderSite(canvas, row, inner);
                    break;
                case 1:
                    DrawLadderFlow(canvas, row, inner);
                    break;
                case 2:
                    DrawLadderBoundary(canvas, row, inner);
                    break;
                case 3:
                    DrawLadderBoundedSupport(canvas, row, inner);
                    break;
                case 4:
                    DrawLadderPartitioned(canvas, row, inner);
                    break;
            }
            return;
        }

        switch (column)
        {
            case 0:
                DrawLadderDirection(canvas, row, inner);
                break;
            case 1:
                DrawLadderDirectedBoundary(canvas, row, inner);
                break;
            case 2:
                DrawLadderDirectedSupport(canvas, row, inner);
                break;
            case 3:
                DrawLadderArea(canvas, row, inner);
                break;
            case 4:
                DrawLadderAreaFace(canvas, row, inner);
                break;
        }
    }

    private void DrawLadderSite(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        var center = Center(bounds);
        if (row == GlossaryLadderRow.Linear)
        {
            DrawPoint(canvas, new SKPoint(center.X - 6, center.Y), SiteColor, 9);
            DrawPoint(canvas, new SKPoint(center.X + 6, center.Y), SiteColor, 9);
            return;
        }

        DrawPoint(canvas, center, SiteColor, 10);
    }

    private void DrawLadderFlow(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        var center = Center(bounds);
        if (row == GlossaryLadderRow.Linear)
        {
            DrawLinearFlow(canvas, bounds);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        DrawRadialGlow(canvas, center, inverse ? InverseColor : PositiveColor, Math.Min(bounds.Width, bounds.Height) * 0.34f);
        DrawPoint(canvas, center, SiteColor, 10);
        for (var index = 0; index < 6; index++)
        {
            var angle = -90f + index * 60f;
            var outer = PointOnCircle(center, Math.Min(bounds.Width, bounds.Height) * 0.38f, angle);
            var inner = PointOnCircle(center, Math.Min(bounds.Width, bounds.Height) * 0.15f, angle);
            DrawVectorArrow(canvas, inverse ? outer : inner, inverse ? inner : outer, inverse ? InverseStroke : PositiveStroke, 1.3f);
        }
    }

    private void DrawLadderBoundary(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawLinearBoundary(canvas, bounds, ticks: false);
            return;
        }

        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.33f;
        DrawCircleBoundary(canvas, center, radius);
        DrawPoint(canvas, center, SiteColor, 8);
        DrawRadiusEvidence(canvas, center, new SKPoint(center.X + radius, center.Y));
        DrawPoint(canvas, new SKPoint(center.X + radius, center.Y), BoundaryMarkerColor, 3.8f);
    }

    private void DrawLadderBoundedSupport(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawLinearSupportBand(canvas, bounds);
            return;
        }

        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.35f;
        using var fill = FillPaint(row == GlossaryLadderRow.RadialPositive ? PositiveColor : InverseColor, 215);
        canvas.DrawCircle(center, radius, fill);
        DrawCircleBoundary(canvas, center, radius);
        DrawPoint(canvas, center, SiteColor, 8);
        DrawRadiusEvidence(canvas, center, new SKPoint(center.X + radius * 0.86f, center.Y));
    }

    private void DrawLadderPartitioned(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawLinearBoundary(canvas, bounds, ticks: true);
            return;
        }

        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.35f;
        DrawCircleBoundary(canvas, center, radius, width: 2.1f);
        DrawPoint(canvas, center, SiteColor, 8);
        for (var index = 0; index < 12; index++)
        {
            var angle = index * 30f;
            var a = PointOnCircle(center, radius - 4, angle);
            var b = PointOnCircle(center, radius + 5, angle);
            DrawLine(canvas, a, b, TickColor, 1.2f);
        }
        DrawRadiusEvidence(canvas, center, new SKPoint(center.X + radius * 0.86f, center.Y));
    }

    private void DrawLadderDirection(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            var y = bounds.MidY;
            var start = new SKPoint(bounds.Left + bounds.Width * 0.20f, y);
            var end = new SKPoint(bounds.Right - bounds.Width * 0.20f, y);
            DrawLine(canvas, start, end, BoundaryColor, 1.5f);
            DrawVectorArrow(canvas, start, end, PositiveStroke, 1.7f);
            DrawPoint(canvas, start, SiteColor, 5);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        DrawRadialDirection(canvas, bounds, inverse, fullArc: false);
    }

    private void DrawLadderDirectedBoundary(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            var y = bounds.MidY;
            var start = new SKPoint(bounds.Left + bounds.Width * 0.22f, y);
            var end = new SKPoint(bounds.Right - bounds.Width * 0.18f, y);
            DrawLine(canvas, start, end, BoundaryColor, 1.3f);
            DrawVectorArrow(canvas, start, end, PositiveStroke, 2f);
            DrawPoint(canvas, start, EndpointColor, 6);
            DrawPoint(canvas, end, WithAlpha(theme.Muted, 120), 4);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        DrawRadialDirectedArc(canvas, bounds, inverse, fillSector: false);
    }

    private void DrawLadderDirectedSupport(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawDirectedLinearSupport(canvas, bounds);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        DrawRadialDirectedArc(canvas, bounds, inverse, fillSector: true);
    }

    private void DrawLadderArea(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawFilledRectArea(canvas, bounds, PositiveColor, upper: true);
            DrawFilledRectArea(canvas, bounds, InverseColor, upper: false);
            return;
        }

        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.34f;
        using var fill = FillPaint(row == GlossaryLadderRow.RadialPositive ? PositiveColor : InverseColor, 235);
        canvas.DrawCircle(center, radius, fill);
    }

    private void DrawLadderAreaFace(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawFilledRectArea(canvas, bounds, PositiveColor, upper: true);
            DrawVectorArrow(
                canvas,
                new SKPoint(bounds.Left + bounds.Width * 0.28f, bounds.MidY - 16),
                new SKPoint(bounds.Right - bounds.Width * 0.25f, bounds.MidY - 16),
                PositiveStroke,
                1.6f);
            DrawFilledRectArea(canvas, bounds, InverseColor, upper: false);
            DrawVectorArrow(
                canvas,
                new SKPoint(bounds.Right - bounds.Width * 0.25f, bounds.MidY + 16),
                new SKPoint(bounds.Left + bounds.Width * 0.28f, bounds.MidY + 16),
                InverseStroke,
                1.6f);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.33f;
        using var fill = FillPaint(inverse ? InverseColor : PositiveColor, 205);
        canvas.DrawCircle(center, radius, fill);
        DrawCircleBoundary(canvas, center, radius, width: 1.4f);
        DrawSpinArrow(canvas, center, radius * 0.80f, inverse);
    }

    private static SKPoint Center(SKRect bounds) =>
        new(bounds.MidX, bounds.MidY);

    private static SKRect Inset(SKRect bounds, float amount) =>
        new(bounds.Left + amount, bounds.Top + amount, bounds.Right - amount, bounds.Bottom - amount);

    private static SKPaint FillPaint(SKColor color, byte alpha = 255) =>
        new()
        {
            Color = WithAlpha(color, alpha),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

    private static SKPaint StrokePaint(SKColor color, float width, byte alpha = 255) =>
        new()
        {
            Color = WithAlpha(color, alpha),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = width,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

    private static void DrawCenteredText(SKCanvas canvas, string text, SKRect bounds, SKPaint paint)
    {
        var width = paint.MeasureText(text);
        canvas.DrawText(text, bounds.MidX - width * 0.5f, bounds.MidY + paint.TextSize * 0.34f, paint);
    }

    private static void DrawRightAlignedText(SKCanvas canvas, string text, SKRect bounds, SKPaint paint)
    {
        var width = paint.MeasureText(text);
        canvas.DrawText(text, bounds.Right - width, bounds.MidY + paint.TextSize * 0.34f, paint);
    }

    private static void DrawPoint(SKCanvas canvas, SKPoint point, SKColor color, float radius)
    {
        using var fill = FillPaint(color);
        canvas.DrawCircle(point, radius, fill);
    }

    private static void DrawLine(SKCanvas canvas, SKPoint start, SKPoint end, SKColor color, float width, byte alpha = 255)
    {
        using var paint = StrokePaint(color, width, alpha);
        canvas.DrawLine(start, end, paint);
    }

    private static void DrawVectorArrow(SKCanvas canvas, SKPoint start, SKPoint end, SKColor color, float width, byte alpha = 255)
    {
        using var paint = StrokePaint(color, width, alpha);
        canvas.DrawLine(start, end, paint);
        DrawArrowHead(canvas, start, end, paint);
    }

    private static SKPoint PointOnCircle(SKPoint center, float radius, float degrees)
    {
        var radians = degrees * MathF.PI / 180f;
        return new SKPoint(
            center.X + MathF.Cos(radians) * radius,
            center.Y + MathF.Sin(radians) * radius);
    }

    private static void DrawRadialGlow(SKCanvas canvas, SKPoint center, SKColor color, float radius)
    {
        using var outer = FillPaint(color, 38);
        using var middle = FillPaint(color, 58);
        using var inner = FillPaint(color, 78);
        canvas.DrawCircle(center, radius, outer);
        canvas.DrawCircle(center, radius * 0.70f, middle);
        canvas.DrawCircle(center, radius * 0.42f, inner);
    }

    private static void DrawLinearFlow(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var halfHeight = bounds.Height * 0.33f;
        var start = new SKPoint(bounds.Left + bounds.Width * 0.28f, center.Y);
        var end = new SKPoint(bounds.Right - bounds.Width * 0.28f, center.Y);
        using var positiveGlow = StrokePaint(PositiveColor, 13, 48);
        using var inverseGlow = StrokePaint(InverseColor, 13, 48);
        canvas.DrawLine(center.X, center.Y - 2, center.X, center.Y - halfHeight, positiveGlow);
        canvas.DrawLine(center.X, center.Y + 2, center.X, center.Y + halfHeight, inverseGlow);
        DrawLine(canvas, start, end, BoundaryColor, 1.4f, 160);
        DrawVectorArrow(
            canvas,
            new SKPoint(center.X, center.Y - 2),
            new SKPoint(center.X, center.Y - halfHeight),
            PositiveStroke,
            1.35f);
        DrawVectorArrow(
            canvas,
            new SKPoint(center.X, center.Y + 2),
            new SKPoint(center.X, center.Y + halfHeight),
            InverseStroke,
            1.35f);
        DrawPoint(canvas, start, SiteColor, 8.5f);
        DrawPoint(canvas, end, SiteColor, 8.5f);
    }

    private static void DrawLinearBoundary(SKCanvas canvas, SKRect bounds, bool ticks)
    {
        var y = bounds.MidY;
        var start = new SKPoint(bounds.Left + bounds.Width * 0.16f, y);
        var end = new SKPoint(bounds.Right - bounds.Width * 0.16f, y);
        DrawLine(canvas, start, end, BoundaryColor, 2f);
        DrawPoint(canvas, start, SiteColor, 8);
        DrawPoint(canvas, end, BoundaryMarkerColor, 4.3f);

        if (!ticks)
        {
            DrawRadiusEvidence(canvas, start, end);
            return;
        }

        for (var index = 1; index < 9; index++)
        {
            var x = start.X + (end.X - start.X) * index / 9f;
            DrawLine(canvas, new SKPoint(x, y - 7), new SKPoint(x, y + 7), TickColor, 1f);
        }
        DrawRadiusEvidence(canvas, start, end);
    }

    private static void DrawCircleBoundary(SKCanvas canvas, SKPoint center, float radius, float width = 1.25f)
    {
        using var stroke = StrokePaint(BoundaryColor, width);
        canvas.DrawCircle(center, radius, stroke);
    }

    private static void DrawRadiusEvidence(SKCanvas canvas, SKPoint start, SKPoint end)
    {
        using var paint = StrokePaint(EvidenceColor, 1.3f, 190);
        using var dash = SKPathEffect.CreateDash([4f, 3f], 0);
        paint.PathEffect = dash;
        canvas.DrawLine(start, end, paint);
    }

    private static void DrawLinearSupportBand(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var left = bounds.Left + bounds.Width * 0.18f;
        var right = bounds.Right - bounds.Width * 0.18f;
        var upper = new SKRect(left, y - 25, right, y);
        var lower = new SKRect(left, y, right, y + 25);
        using var positive = FillPaint(PositiveColor, 208);
        using var inverse = FillPaint(InverseColor, 105);
        canvas.DrawRect(upper, positive);
        canvas.DrawRect(lower, inverse);
        DrawLine(canvas, new SKPoint(left, y), new SKPoint(right, y), BoundaryColor, 2f);
        DrawPoint(canvas, new SKPoint(left, y), SiteColor, 8);
        DrawPoint(canvas, new SKPoint(right, y), SiteColor, 8);
    }

    private static void DrawRadialDirection(SKCanvas canvas, SKRect bounds, bool inverse, bool fullArc)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.34f;
        DrawCircleBoundary(canvas, center, radius);
        DrawPoint(canvas, center, SiteColor, 8);
        DrawRadiusEvidence(canvas, center, new SKPoint(center.X + radius * 0.86f, center.Y));

        var sweepStart = -38f;
        var sweepEnd = 34f;
        var color = inverse ? InverseStroke : PositiveStroke;
        if (fullArc)
        {
            DrawRadialDirectedArc(canvas, bounds, inverse, fillSector: false);
            return;
        }

        var start = PointOnCircle(center, radius * 0.95f, inverse ? sweepEnd : sweepStart);
        var end = PointOnCircle(center, radius * 0.95f, inverse ? sweepStart : sweepEnd);
        DrawVectorArrow(canvas, start, end, color, 1.4f);
        DrawVectorArrow(
            canvas,
            center,
            PointOnCircle(center, radius * 0.78f, inverse ? 180 : 0),
            color,
            1.2f,
            165);
    }

    private static void DrawRadialDirectedArc(SKCanvas canvas, SKRect bounds, bool inverse, bool fillSector)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.34f;
        var startAngle = inverse ? 145f : -35f;
        var sweep = inverse ? -112f : 112f;
        var color = inverse ? InverseStroke : PositiveStroke;
        var fillColor = inverse ? InverseColor : PositiveColor;
        var oval = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);

        if (fillSector)
        {
            using var sector = new SKPath();
            sector.MoveTo(center);
            sector.LineTo(PointOnCircle(center, radius, startAngle));
            sector.ArcTo(oval, startAngle, sweep, false);
            sector.Close();
            using var fill = FillPaint(fillColor, 154);
            canvas.DrawPath(sector, fill);
        }

        DrawCircleBoundary(canvas, center, radius, 1.1f);
        DrawPoint(canvas, center, SiteColor, 8);
        DrawRadiusEvidence(canvas, center, new SKPoint(center.X + radius * 0.86f, center.Y));

        using var arc = StrokePaint(color, 2f);
        canvas.DrawArc(oval, startAngle, sweep, false, arc);
        var arrowStart = PointOnCircle(center, radius, startAngle + sweep * 0.84f);
        var arrowEnd = PointOnCircle(center, radius, startAngle + sweep);
        DrawArrowHead(canvas, arrowStart, arrowEnd, arc);
        DrawPoint(canvas, PointOnCircle(center, radius, startAngle), EndpointColor, 4.6f);
    }

    private static void DrawDirectedLinearSupport(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var left = bounds.Left + bounds.Width * 0.16f;
        var right = bounds.Right - bounds.Width * 0.16f;
        var top = new SKRect(left, y - 28, right, y - 4);
        var bottom = new SKRect(left, y + 4, right, y + 28);
        using var positive = FillPaint(PositiveColor, 200);
        using var inverse = FillPaint(InverseColor, 180);
        canvas.DrawRect(top, positive);
        canvas.DrawRect(bottom, inverse);
        DrawLine(canvas, new SKPoint(left, y - 4), new SKPoint(right, y - 4), BoundaryColor, 1.5f);
        DrawLine(canvas, new SKPoint(left, y + 4), new SKPoint(right, y + 4), BoundaryColor, 1.5f);
        DrawVectorArrow(
            canvas,
            new SKPoint(left + 6, y - 16),
            new SKPoint(right - 5, y - 16),
            PositiveStroke,
            1.5f);
        DrawVectorArrow(
            canvas,
            new SKPoint(right - 5, y + 16),
            new SKPoint(left + 6, y + 16),
            InverseStroke,
            1.5f);
        DrawPoint(canvas, new SKPoint(left, y), EndpointColor, 5.4f);
        DrawPoint(canvas, new SKPoint(right, y), EndpointColor, 5.4f);
    }

    private static void DrawFilledRectArea(SKCanvas canvas, SKRect bounds, SKColor color, bool upper)
    {
        var width = bounds.Width * 0.56f;
        var height = bounds.Height * 0.34f;
        var centerY = upper ? bounds.MidY - height * 0.58f : bounds.MidY + height * 0.58f;
        var rect = new SKRect(
            bounds.MidX - width * 0.5f,
            centerY - height * 0.5f,
            bounds.MidX + width * 0.5f,
            centerY + height * 0.5f);
        using var fill = FillPaint(color, 220);
        canvas.DrawRect(rect, fill);
    }

    private static void DrawSpinArrow(SKCanvas canvas, SKPoint center, float radius, bool inverse)
    {
        var startAngle = inverse ? 150f : -35f;
        var sweep = inverse ? -250f : 250f;
        var color = inverse ? InverseStroke : PositiveStroke;
        var oval = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
        using var paint = StrokePaint(color, 1.8f);
        canvas.DrawArc(oval, startAngle, sweep, false, paint);
        var arrowStart = PointOnCircle(center, radius, startAngle + sweep * 0.90f);
        var arrowEnd = PointOnCircle(center, radius, startAngle + sweep);
        DrawArrowHead(canvas, arrowStart, arrowEnd, paint);
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

    private void DrawHoverTip(SKCanvas canvas, SKSize size)
    {
        if (hoverTip is not { } tip)
        {
            return;
        }

        using var titlePaint = TextPaint(12.5f, theme.Ink, bold: true);
        using var bodyPaint = TextPaint(12f, theme.Ink);
        var panelWidth = Math.Min(340f, Math.Max(190f, size.Width - 48f));
        var textWidth = panelWidth - 26f;
        var lines = WrapTextLines(tip.Body, bodyPaint, textWidth);
        var lineHeight = 16.2f;
        var panelHeight = 44f + lines.Count * lineHeight;
        var x = tip.Anchor.X + 18f;
        var y = tip.Anchor.Y + 18f;
        if (x + panelWidth > size.Width - 18f)
        {
            x = tip.Anchor.X - panelWidth - 18f;
        }

        if (y + panelHeight > size.Height - 18f)
        {
            y = tip.Anchor.Y - panelHeight - 18f;
        }

        x = Math.Clamp(x, 14f, Math.Max(14f, size.Width - panelWidth - 14f));
        y = Math.Clamp(y, 14f, Math.Max(14f, size.Height - panelHeight - 14f));
        var panel = new SKRect(x, y, x + panelWidth, y + panelHeight);
        using var shadow = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 34),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var fill = new SKPaint
        {
            Color = WithAlpha(theme.PageFill, 247),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };
        using var stroke = new SKPaint
        {
            Color = WithAlpha(theme.PageStroke, 230),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };

        canvas.DrawRoundRect(panel.Left + 2, panel.Top + 3, panel.Width, panel.Height, 8, 8, shadow);
        canvas.DrawRoundRect(panel, 8, 8, fill);
        canvas.DrawRoundRect(panel, 8, 8, stroke);
        DrawClippedText(canvas, tip.Title, panel.Left + 13, panel.Top + 22, textWidth, titlePaint);
        var textY = panel.Top + 43;
        foreach (var line in lines)
        {
            canvas.DrawText(line, panel.Left + 13, textY, bodyPaint);
            textY += lineHeight;
        }
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
        var size = 4.6f;
        var gap = 3.4f;
        var width = size * 3 + gap * 2;
        var height = size * 2 + gap;
        var left = bounds.MidX - width * 0.5f;
        var top = bounds.MidY - height * 0.5f;
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

    private static IReadOnlyList<string> WrapTextLines(string text, SKPaint paint, float width)
    {
        var lines = new List<string>();
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

            lines.Add(line);
            line = word;
        }

        if (!string.IsNullOrEmpty(line))
        {
            lines.Add(line);
        }

        return lines;
    }
}

internal readonly record struct BlocksHoverTip(SKPoint Anchor, string Title, string Body);

internal readonly record struct BlocksHitArea(
    SKRect Bounds,
    BlocksHitKind Kind,
    int PageIndex,
    string? TooltipTitle = null,
    string? TooltipBody = null);

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
    GlossaryTooltip,
    DragPhysicalPoint
}
