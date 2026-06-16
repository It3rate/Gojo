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

    public static SKRect GlossaryArea(SKRect pageArea) =>
        new(pageArea.Left + 12, pageArea.Top + 22, pageArea.Right - 14, pageArea.Bottom - 24);

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
        canvas.DrawText($"{mode} {document.PageIndex + 1}/{document.Pages.Count}  {page.Subtitle}", textX, 51, subtitle);
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

        if (page.IsGlossary)
        {
            DrawGlossaryPage(canvas, page, BlocksLayout.GlossaryArea(pageArea), hitAreas);
        }
        else if (page.Id == BlocksDocument.PhysicalStructurePageId)
        {
            var structureArea = BlocksLayout.StructureArea(pageArea);
            DrawPhysicalStructurePage(canvas, structureArea, hitAreas);
        }
        else if (document.CurrentStructure is { } structure)
        {
            var structureArea = BlocksLayout.StructureArea(pageArea);
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
        if (page.GlossarySheet == BlocksGlossarySheetKind.ConnectedPaths)
        {
            DrawConnectedPathsGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.PathReadBases)
        {
            DrawPathReadBasesGlossary(canvas, page, area, hitAreas);
            return;
        }

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

    private void DrawConnectedPathsGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("connected-paths-separate-sections", "Separate sections"),
            new GlossarySpecimenDescriptor("connected-paths-shared-point", "Shared point"),
            new GlossarySpecimenDescriptor("connected-paths-bonded-point", "Bonded point"),
            new GlossarySpecimenDescriptor("connected-paths-orthogonal-endpoint", "Orthogonal endpoint"),
            new GlossarySpecimenDescriptor("connected-paths-higher-bridge", "Higher bridge"),
            new GlossarySpecimenDescriptor("connected-paths-closure-tension", "Closure tension")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawConnectedPathSpecimen);
    }

    private void DrawPathReadBasesGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("path-basis-xy", "XY basis"),
            new GlossarySpecimenDescriptor("path-basis-circular", "Circular basis"),
            new GlossarySpecimenDescriptor("path-basis-orthogonal", "Orthogonal basis"),
            new GlossarySpecimenDescriptor("path-basis-mixed", "Mixed basis"),
            new GlossarySpecimenDescriptor("path-basis-composite-read", "Composite read"),
            new GlossarySpecimenDescriptor("path-basis-residue-error", "Residue / error")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawPathBasisSpecimen);
    }

    private void DrawSpecimenGrid(
        SKCanvas canvas,
        BlocksPage page,
        SKRect area,
        List<BlocksHitArea> hitAreas,
        IReadOnlyList<GlossarySpecimenDescriptor> specimens,
        Action<SKCanvas, int, SKRect> drawSpecimen)
    {
        var columns = area.Width >= 760 ? 3 : 2;
        var rows = (int)Math.Ceiling(specimens.Count / (double)columns);
        var gap = 13f;
        var cellWidth = (area.Width - gap * (columns - 1)) / columns;
        var cellHeight = (area.Height - gap * (rows - 1)) / rows;
        using var cardFill = new SKPaint { Color = WithAlpha(theme.RailFill, 138), Style = SKPaintStyle.Fill, IsAntialias = true };
        using var cardStroke = new SKPaint { Color = theme.PageStroke, Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = true };
        using var title = TextPaint(12.2f, theme.Ink, bold: true);

        for (var index = 0; index < specimens.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var left = area.Left + column * (cellWidth + gap);
            var top = area.Top + row * (cellHeight + gap);
            var cell = new SKRect(left, top, left + cellWidth, top + cellHeight);
            canvas.DrawRoundRect(cell, 7, 7, cardFill);
            canvas.DrawRoundRect(cell, 7, 7, cardStroke);

            var specimen = specimens[index];
            DrawClippedText(canvas, specimen.Title, cell.Left + 11, cell.Top + 21, cell.Width - 22, title);
            var diagram = new SKRect(cell.Left + 11, cell.Top + 31, cell.Right - 11, cell.Bottom - 9);
            drawSpecimen(canvas, index, diagram);
            AddGlossaryTooltipHit(hitAreas, Inset(cell, 7), specimen.Id, specimen.Title, page);
        }
    }

    private void DrawGlossaryLadder(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var sheet = page.GlossarySheet;
        var columns = GlossaryColumns(sheet);
        var columnKeys = GlossaryColumnKeys(sheet);
        var rows = GlossaryRows(sheet);
        var rowTitles = GlossaryRowTitles(sheet);
        var rowKeys = GlossaryRowKeys(sheet);
        var labelWidth = Math.Clamp(area.Width * 0.07f, 48f, 60f);
        var headerHeight = 46f;
        var gap = 10f;
        var rowWeights = GlossaryRowWeights(sheet, rows.Length);
        var availableHeight = area.Height - headerHeight - gap * (rows.Length - 1);
        var totalWeight = rowWeights.Sum();
        var rowHeights = rowWeights
            .Select(weight => availableHeight * weight / totalWeight)
            .ToArray();
        var cellWidth = (area.Width - labelWidth - gap * (columns.Length - 1)) / columns.Length;

        using var headerPaint = TextPaint(11.8f, theme.Ink, bold: true);
        using var rowPaint = TextPaint(12.5f, theme.Ink);
        using var gridPaint = new SKPaint { Color = WithAlpha(theme.SoftLine, 95), Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };

        for (var column = 0; column < columns.Length; column++)
        {
            var x = area.Left + labelWidth + column * (cellWidth + gap);
            DrawCenteredMultilineText(canvas, columns[column], new SKRect(x, area.Top + 4, x + cellWidth, area.Top + headerHeight), headerPaint, 13f);
        }

        var y = area.Top + headerHeight;
        for (var row = 0; row < rows.Length; row++)
        {
            var cellHeight = rowHeights[row];
            DrawRightAlignedMultilineText(canvas, rows[row], new SKRect(area.Left, y, area.Left + labelWidth - 5, y + cellHeight), rowPaint, 15f);
            for (var column = 0; column < columns.Length; column++)
            {
                var x = area.Left + labelWidth + column * (cellWidth + gap);
                var cell = new SKRect(x, y, x + cellWidth, y + cellHeight);
                var title = $"{TitleCase(rowTitles[row])} {TitleCase(columns[column])}";
                var id = $"{GlossarySheetKey(sheet)}-{rowKeys[row]}-{columnKeys[column]}";
                canvas.DrawRoundRect(cell, 6, 6, gridPaint);
                DrawGlossaryLadderCell(canvas, sheet, row, column, cell);
                AddGlossaryTooltipHit(hitAreas, Inset(cell, 8), id, title, page);
            }

            y += cellHeight + gap;
        }
    }

    private static float[] GlossaryRowWeights(BlocksGlossarySheetKind sheet, int rowCount)
    {
        if (sheet == BlocksGlossarySheetKind.BooleanParticipation &&
            rowCount == 3)
        {
            return [0.72f, 1.14f, 1.14f];
        }

        return Enumerable.Repeat(1f, rowCount).ToArray();
    }

    private static string[] GlossaryColumns(BlocksGlossarySheetKind sheet) =>
        sheet switch
        {
            BlocksGlossarySheetKind.SourceToSupport => ["site", "flow", "boundary", "partitioned", "orientation", "direction"],
            BlocksGlossarySheetKind.DirectionToArea => ["directed boundary", "boundary support", "directed support", "area", "directed area", "bounded area"],
            BlocksGlossarySheetKind.Curves => ["corner / cusp", "arc blend", "Bezier", "clothoid", "involute", "plotted path"],
            BlocksGlossarySheetKind.Branches => ["branch", "merge", "orthogonal joint", "other"],
            BlocksGlossarySheetKind.BooleanParticipation => ["participants", "touch / cross", "overlap", "union", "intersection", "difference"],
            _ => []
        };

    private static string[] GlossaryColumnKeys(BlocksGlossarySheetKind sheet) =>
        sheet switch
        {
            BlocksGlossarySheetKind.SourceToSupport => ["site", "flow", "boundary", "partitioned", "orientation", "direction"],
            BlocksGlossarySheetKind.DirectionToArea => ["directed-boundary", "boundary-support", "directed-support", "area", "directed-area", "bounded-area"],
            BlocksGlossarySheetKind.Curves => ["corner", "arc-blend", "bezier", "clothoid", "involute", "plotted"],
            BlocksGlossarySheetKind.Branches => ["branch", "merge", "orthogonal-joint", "other"],
            BlocksGlossarySheetKind.BooleanParticipation => ["participants", "touch", "overlap", "union", "intersection", "difference"],
            _ => []
        };

    private static string[] GlossaryRows(BlocksGlossarySheetKind sheet) =>
        sheet switch
        {
            BlocksGlossarySheetKind.Curves => ["line\nto line", "line\nto arc", "arc\nto arc"],
            BlocksGlossarySheetKind.Branches => ["tip", "middle", "structure"],
            BlocksGlossarySheetKind.BooleanParticipation => ["point", "segment", "area"],
            _ => ["radial\npositive", "radial\ninverse", "linear"]
        };

    private static string[] GlossaryRowTitles(BlocksGlossarySheetKind sheet) =>
        sheet switch
        {
            BlocksGlossarySheetKind.Curves => ["line to line", "line to arc", "arc to arc"],
            BlocksGlossarySheetKind.Branches => ["tip", "middle", "structure"],
            BlocksGlossarySheetKind.BooleanParticipation => ["point", "segment", "area"],
            _ => ["radial positive", "radial inverse", "linear"]
        };

    private static string[] GlossaryRowKeys(BlocksGlossarySheetKind sheet) =>
        sheet switch
        {
            BlocksGlossarySheetKind.Curves => ["line-line", "line-arc", "arc-arc"],
            BlocksGlossarySheetKind.Branches => ["tip", "middle", "structure"],
            BlocksGlossarySheetKind.BooleanParticipation => ["point", "segment", "area"],
            _ => ["radial-positive", "radial-inverse", "linear"]
        };

    private static void AddGlossaryTooltipHit(
        List<BlocksHitArea> hitAreas,
        SKRect bounds,
        string id,
        string title,
        BlocksPage page)
    {
        var body = TooltipBody(page.GlossaryDescriptionFor(id), page.GlossaryStructureFor(id));
        if (string.IsNullOrWhiteSpace(body))
        {
            return;
        }

        hitAreas.Add(new BlocksHitArea(bounds, BlocksHitKind.GlossaryTooltip, -1, title, body));
    }

    private static string TooltipBody(string? description, BlocksGlossaryStructure? structure)
    {
        var sections = new List<string>();
        if (!string.IsNullOrWhiteSpace(description))
        {
            sections.Add(description.Trim());
        }

        if (structure is not null)
        {
            var structureText = $"Gojo construction (P=point, S=segment)\n{structure.Expression}";
            if (!string.IsNullOrWhiteSpace(structure.EncodingNote))
            {
                structureText += $"\n\nEncoding note\n{structure.EncodingNote.Trim()}";
            }

            sections.Add(structureText);
        }

        return string.Join("\n\n", sections);
    }

    private static string GlossarySheetKey(BlocksGlossarySheetKind sheet) =>
        sheet switch
        {
            BlocksGlossarySheetKind.SourceToSupport => "source-support",
            BlocksGlossarySheetKind.DirectionToArea => "direction-area",
            BlocksGlossarySheetKind.Curves => "curves",
            BlocksGlossarySheetKind.Branches => "branches",
            BlocksGlossarySheetKind.BooleanParticipation => "boolean-participation",
            _ => "glossary"
        };

    private static string TitleCase(string value) =>
        string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    private void DrawGlossaryLadderCell(
        SKCanvas canvas,
        BlocksGlossarySheetKind sheet,
        int row,
        int column,
        SKRect cell)
    {
        var inner = Inset(cell, 10);
        if (sheet == BlocksGlossarySheetKind.SourceToSupport)
        {
            var ladderRow = (GlossaryLadderRow)row;
            switch (column)
            {
                case 0:
                    DrawLadderSite(canvas, ladderRow, inner);
                    break;
                case 1:
                    DrawLadderFlow(canvas, ladderRow, inner);
                    break;
                case 2:
                    DrawLadderBoundary(canvas, ladderRow, inner);
                    break;
                case 3:
                    DrawLadderPartitioned(canvas, ladderRow, inner);
                    break;
                case 4:
                    DrawLadderOrientation(canvas, ladderRow, inner);
                    break;
                case 5:
                    DrawLadderDirection(canvas, ladderRow, inner);
                    break;
            }
            return;
        }

        if (sheet == BlocksGlossarySheetKind.Curves)
        {
            DrawCurveJoinCell(canvas, row, column, inner);
            return;
        }

        if (sheet == BlocksGlossarySheetKind.Branches)
        {
            DrawBranchJoinCell(canvas, row, column, inner);
            return;
        }

        if (sheet == BlocksGlossarySheetKind.BooleanParticipation)
        {
            DrawBooleanParticipationCell(canvas, row, column, inner);
            return;
        }

        var supportRow = (GlossaryLadderRow)row;
        switch (column)
        {
            case 0:
                DrawLadderDirectedBoundary(canvas, supportRow, inner);
                break;
            case 1:
                DrawLadderBoundedSupport(canvas, supportRow, inner);
                break;
            case 2:
                DrawLadderDirectedSupport(canvas, supportRow, inner);
                break;
            case 3:
                DrawLadderArea(canvas, supportRow, inner);
                break;
            case 4:
                DrawLadderAreaFace(canvas, supportRow, inner);
                break;
            case 5:
                DrawLadderBoundedArea(canvas, supportRow, inner);
                break;
        }
    }

    private static void DrawConnectedPathSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawSeparatePathSections(canvas, bounds);
                break;
            case 1:
                DrawSharedPointPath(canvas, bounds);
                break;
            case 2:
                DrawBondedPointPath(canvas, bounds);
                break;
            case 3:
                DrawOrthogonalEndpointPath(canvas, bounds);
                break;
            case 4:
                DrawHigherBridgePath(canvas, bounds);
                break;
            case 5:
                DrawClosureTensionPath(canvas, bounds);
                break;
        }
    }

    private void DrawPathBasisSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawXyBasisPath(canvas, bounds);
                break;
            case 1:
                DrawCircularBasisPath(canvas, bounds);
                break;
            case 2:
                DrawOrthogonalBasisPath(canvas, bounds);
                break;
            case 3:
                DrawMixedBasisPath(canvas, bounds);
                break;
            case 4:
                DrawCompositePathRead(canvas, bounds);
                break;
            case 5:
                DrawPathResidueError(canvas, bounds);
                break;
        }
    }

    private static void DrawSeparatePathSections(SKCanvas canvas, SKRect bounds)
    {
        var leftY = bounds.MidY + bounds.Height * 0.08f;
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 12, leftY), new SKPoint(bounds.MidX - 10, leftY - 12));
        using var path = new SKPath();
        path.MoveTo(bounds.MidX + 18, bounds.MidY + 18);
        path.QuadTo(bounds.Right - 26, bounds.Top + 4, bounds.Right - 10, bounds.MidY - 12);
        DrawBridgePath(canvas, path, BoundaryColor, 1.8f, glow: false);
        DrawPoint(canvas, new SKPoint(bounds.MidX - 10, leftY - 12), SiteColor, 4.2f);
        DrawPoint(canvas, new SKPoint(bounds.MidX + 18, bounds.MidY + 18), SiteColor, 4.2f);
    }

    private static void DrawSharedPointPath(SKCanvas canvas, SKRect bounds)
    {
        var joint = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 12, bounds.Bottom - 18), joint);
        using var path = new SKPath();
        path.MoveTo(joint);
        path.QuadTo(bounds.MidX + bounds.Width * 0.18f, bounds.Top + 2, bounds.Right - 12, bounds.Top + bounds.Height * 0.34f);
        DrawBridgePath(canvas, path, BoundaryColor, 1.9f, glow: false);
        DrawPoint(canvas, joint, EndpointColor, 5.4f);
    }

    private static void DrawBondedPointPath(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.MidX - 8, bounds.MidY + 2);
        var b = new SKPoint(bounds.MidX + 9, bounds.MidY - 4);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 12, bounds.Bottom - 15), a);
        using var path = new SKPath();
        path.MoveTo(b);
        path.CubicTo(bounds.MidX + 28, bounds.Top + 4, bounds.Right - 26, bounds.Top + 4, bounds.Right - 10, bounds.MidY - 8);
        DrawBridgePath(canvas, path, BoundaryColor, 1.8f, glow: false);
        DrawBond(canvas, a, b);
        DrawPoint(canvas, a, EndpointColor, 4.4f);
        DrawPoint(canvas, b, EndpointColor, 4.4f);
    }

    private static void DrawOrthogonalEndpointPath(SKCanvas canvas, SKRect bounds)
    {
        var center = new SKPoint(bounds.Left + bounds.Width * 0.42f, bounds.Bottom - bounds.Height * 0.18f);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.35f;
        var start = PointOnCircle(center, radius, -165f);
        var end = PointOnCircle(center, radius, -32f);
        using var arc = new SKPath();
        arc.MoveTo(start);
        arc.QuadTo(bounds.Left + bounds.Width * 0.34f, bounds.Top + 5, end.X, end.Y);
        DrawBridgePath(canvas, arc, BoundaryColor, 1.9f, glow: false);
        var branchEnd = new SKPoint(end.X + 22, end.Y - 32);
        DrawVectorArrow(canvas, end, branchEnd, PositiveStroke, 2.1f);
        DrawPoint(canvas, end, EndpointColor, 5f);
    }

    private static void DrawHigherBridgePath(SKCanvas canvas, SKRect bounds)
    {
        var a0 = new SKPoint(bounds.Left + 12, bounds.MidY + 15);
        var a1 = new SKPoint(bounds.Left + bounds.Width * 0.34f, bounds.MidY + 4);
        var b0 = new SKPoint(bounds.Right - bounds.Width * 0.34f, bounds.MidY - 10);
        var b1 = new SKPoint(bounds.Right - 12, bounds.MidY - 20);
        DrawCarrierLine(canvas, a0, a1);
        DrawCarrierLine(canvas, b0, b1);
        using var bridge = new SKPath();
        bridge.MoveTo(a1);
        bridge.CubicTo(bounds.MidX - 12, bounds.Top + 8, bounds.MidX + 12, bounds.Bottom - 8, b0.X, b0.Y);
        DrawBridgePath(canvas, bridge, PositiveStroke, 2.4f, glow: true);
        DrawPoint(canvas, a1, EndpointColor, 4.6f);
        DrawPoint(canvas, b0, EndpointColor, 4.6f);
    }

    private static void DrawClosureTensionPath(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.36f;
        var points = new[]
        {
            PointOnCircle(center, radius, -90f),
            PointOnCircle(center, radius, -18f),
            PointOnCircle(center, radius, 54f),
            PointOnCircle(center, radius, 126f),
            PointOnCircle(center, radius, 198f)
        };
        var miss = new SKPoint(points[0].X - 12, points[0].Y + 7);
        for (var index = 0; index < points.Length - 1; index++)
        {
            DrawCarrierLine(canvas, points[index], points[index + 1]);
        }
        DrawCarrierLine(canvas, points[^1], miss);
        DrawDashedLine(canvas, miss, points[0], InverseStroke, 1.4f);
        DrawTensionSpot(canvas, new SKPoint((miss.X + points[0].X) * 0.5f, (miss.Y + points[0].Y) * 0.5f), 15f);
        DrawPoint(canvas, points[0], EndpointColor, 4.2f);
        DrawPoint(canvas, miss, InverseStroke, 4.2f);
    }

    private static void DrawXyBasisPath(SKCanvas canvas, SKRect bounds)
    {
        DrawTinyAxes(canvas, bounds);
        var a = new SKPoint(bounds.Left + bounds.Width * 0.24f, bounds.Bottom - bounds.Height * 0.28f);
        var b = new SKPoint(bounds.Right - bounds.Width * 0.20f, bounds.Top + bounds.Height * 0.30f);
        DrawVectorArrow(canvas, a, b, PositiveStroke, 2.2f);
        DrawPoint(canvas, a, SiteColor, 4.8f);
        DrawPoint(canvas, b, EndpointColor, 4.8f);
    }

    private static void DrawCircularBasisPath(SKCanvas canvas, SKRect bounds)
    {
        var origin = new SKPoint(bounds.Left + bounds.Width * 0.30f, bounds.Bottom - bounds.Height * 0.28f);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.23f;
        DrawProtractor(canvas, origin, radius, -32f);
        var end = PointOnCircle(origin, bounds.Width * 0.48f, -32f);
        DrawVectorArrow(canvas, origin, end, PositiveStroke, 2.2f);
        DrawPoint(canvas, origin, SiteColor, 4.8f);
        DrawPoint(canvas, end, EndpointColor, 4.6f);
    }

    private static void DrawOrthogonalBasisPath(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY + bounds.Height * 0.10f;
        var start = new SKPoint(bounds.Left + 13, y);
        var end = new SKPoint(bounds.Right - 14, y);
        DrawCarrierLine(canvas, start, end);
        var joint = new SKPoint(bounds.Left + bounds.Width * 0.45f, y);
        DrawVectorArrow(canvas, joint, new SKPoint(joint.X, bounds.Top + 8), PositiveStroke, 2.3f);
        DrawTensionSpot(canvas, new SKPoint(joint.X, joint.Y - 18), 9f, PositiveColor);
        DrawPoint(canvas, joint, EndpointColor, 4.8f);
    }

    private static void DrawMixedBasisPath(SKCanvas canvas, SKRect bounds)
    {
        DrawTinyAxes(canvas, bounds);
        var origin = new SKPoint(bounds.Left + bounds.Width * 0.35f, bounds.Bottom - bounds.Height * 0.30f);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.20f;
        DrawProtractor(canvas, origin, radius, -48f);
        var end = PointOnCircle(origin, bounds.Width * 0.44f, -48f);
        DrawVectorArrow(canvas, origin, end, PositiveStroke, 2.2f);
        DrawDashedLine(canvas, new SKPoint(end.X, bounds.Bottom - 10), end, EvidenceColor, 1.1f);
        DrawPoint(canvas, origin, SiteColor, 4.8f);
        DrawPoint(canvas, end, EndpointColor, 4.8f);
    }

    private static void DrawCompositePathRead(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 12, bounds.Bottom - 18);
        var b = new SKPoint(bounds.Left + bounds.Width * 0.35f, bounds.MidY + 5);
        var c = new SKPoint(bounds.Left + bounds.Width * 0.58f, bounds.MidY - 12);
        var d = new SKPoint(bounds.Right - 12, bounds.Top + bounds.Height * 0.34f);
        DrawCarrierLine(canvas, a, b);
        using var arc = new SKPath();
        arc.MoveTo(b);
        arc.QuadTo(bounds.MidX, bounds.Top + 2, c.X, c.Y);
        DrawBridgePath(canvas, arc, BoundaryColor, 1.7f, glow: false);
        using var curve = new SKPath();
        curve.MoveTo(c);
        curve.CubicTo(bounds.MidX + 24, bounds.Bottom - 6, bounds.Right - 34, bounds.Top + 2, d.X, d.Y);
        DrawBridgePath(canvas, curve, BoundaryColor, 1.7f, glow: false);
        using var read = new SKPath();
        read.MoveTo(a);
        read.LineTo(b);
        read.QuadTo(bounds.MidX, bounds.Top + 2, c.X, c.Y);
        read.CubicTo(bounds.MidX + 24, bounds.Bottom - 6, bounds.Right - 34, bounds.Top + 2, d.X, d.Y);
        DrawBridgePath(canvas, read, PositiveStroke, 2.4f, glow: true);
        DrawPathPartitions(canvas, read, 8);
    }

    private void DrawPathResidueError(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.31f;
        var points = new[]
        {
            PointOnCircle(center, radius, -110f),
            PointOnCircle(center, radius, -12f),
            PointOnCircle(center, radius, 82f),
            PointOnCircle(center, radius, 176f)
        };
        DrawProtractor(canvas, points[0], radius * 0.62f, -22f);
        DrawCarrierLine(canvas, points[0], points[1]);
        DrawCarrierLine(canvas, points[1], points[2]);
        DrawCarrierLine(canvas, points[2], points[3]);
        var target = new SKPoint(points[0].X + 4, points[0].Y - 2);
        DrawDashedLine(canvas, points[3], target, InverseStroke, 1.5f);
        DrawTensionSpot(canvas, new SKPoint((points[3].X + target.X) * 0.5f, (points[3].Y + target.Y) * 0.5f), 16f);
        DrawPoint(canvas, target, EndpointColor, 4.4f);
        DrawPoint(canvas, points[3], InverseStroke, 4.4f);
    }

    private static void DrawBond(SKCanvas canvas, SKPoint a, SKPoint b)
    {
        DrawDashedLine(canvas, a, b, EndpointColor, 1.4f);
        using var ring = StrokePaint(EndpointColor, 1.2f, 175);
        canvas.DrawCircle(new SKPoint((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f), 7f, ring);
    }

    private static void DrawDashedLine(SKCanvas canvas, SKPoint start, SKPoint end, SKColor color, float width)
    {
        using var paint = StrokePaint(color, width, 175);
        using var dash = SKPathEffect.CreateDash([4f, 3.2f], 0);
        paint.PathEffect = dash;
        canvas.DrawLine(start, end, paint);
    }

    private static void DrawTensionSpot(SKCanvas canvas, SKPoint point, float radius, SKColor? color = null)
    {
        using var glow = FillPaint(color ?? InverseColor, 70);
        canvas.DrawCircle(point, radius, glow);
    }

    private static void DrawTinyAxes(SKCanvas canvas, SKRect bounds)
    {
        var origin = new SKPoint(bounds.Left + bounds.Width * 0.23f, bounds.Bottom - bounds.Height * 0.24f);
        using var axis = StrokePaint(TickColor, 1f, 115);
        canvas.DrawLine(origin.X, bounds.Top + 10, origin.X, bounds.Bottom - 8, axis);
        canvas.DrawLine(bounds.Left + 8, origin.Y, bounds.Right - 8, origin.Y, axis);
        for (var index = 1; index < 4; index++)
        {
            var x = origin.X + index * bounds.Width * 0.17f;
            canvas.DrawLine(x, origin.Y - 4, x, origin.Y + 4, axis);
            var y = origin.Y - index * bounds.Height * 0.16f;
            canvas.DrawLine(origin.X - 4, y, origin.X + 4, y, axis);
        }
    }

    private static void DrawProtractor(SKCanvas canvas, SKPoint center, float radius, float rotation)
    {
        using var stroke = StrokePaint(TickColor, 1.1f, 126);
        canvas.DrawCircle(center, radius, stroke);
        for (var index = 0; index < 12; index++)
        {
            var angle = rotation + index * 30f;
            var inner = PointOnCircle(center, radius - 4, angle);
            var outer = PointOnCircle(center, radius + 2, angle);
            DrawLine(canvas, inner, outer, TickColor, index % 3 == 0 ? 1.25f : 0.9f, 126);
        }
        var direction = PointOnCircle(center, radius + 8, rotation);
        DrawVectorArrow(canvas, center, direction, TickColor, 1f, 132);
    }

    private static void DrawPathPartitions(SKCanvas canvas, SKPath path, int count)
    {
        using var measure = new SKPathMeasure(path, false);
        var length = measure.Length;
        if (length <= 0.001f)
        {
            return;
        }

        using var paint = StrokePaint(BoundaryColor, 1.75f, 215);
        for (var index = 1; index <= count; index++)
        {
            var distance = length * index / (count + 1);
            if (!measure.GetPositionAndTangent(distance, out var point, out var tangent))
            {
                continue;
            }

            var normal = Normalize(new SKPoint(-tangent.Y, tangent.X));
            var half = index % 2 == 0 ? 6.5f : 5.5f;
            canvas.DrawLine(
                point.X - normal.X * half,
                point.Y - normal.Y * half,
                point.X + normal.X * half,
                point.Y + normal.Y * half,
                paint);
        }
    }

    private static void DrawBranchJoinCell(SKCanvas canvas, int row, int column, SKRect bounds)
    {
        switch ((row, column))
        {
            case (0, 0):
                DrawTipBranch(canvas, bounds);
                break;
            case (0, 1):
                DrawTipMerge(canvas, bounds);
                break;
            case (0, 2):
                DrawButtAndCorner(canvas, bounds);
                break;
            case (0, 3):
                DrawArcTipJoin(canvas, bounds);
                break;
            case (1, 0):
                DrawMiddleBranch(canvas, bounds);
                break;
            case (1, 1):
                DrawMiddleMerge(canvas, bounds);
                break;
            case (1, 2):
                DrawOrthogonalArcMiddle(canvas, bounds);
                break;
            case (1, 3):
                DrawCrossingNoJoin(canvas, bounds);
                break;
            case (2, 0):
                DrawMultiBranch(canvas, bounds);
                break;
            case (2, 1):
                DrawBranchMergeNetwork(canvas, bounds);
                break;
            case (2, 2):
                DrawEnclosureArea(canvas, bounds);
                break;
            case (2, 3):
                DrawPartialUArea(canvas, bounds);
                break;
        }
    }

    private void DrawBooleanParticipationCell(SKCanvas canvas, int row, int column, SKRect bounds)
    {
        switch (row)
        {
            case 0:
                DrawBooleanPointCell(canvas, column, bounds);
                break;
            case 1:
                DrawBooleanSegmentCell(canvas, column, bounds);
                break;
            case 2:
                DrawBooleanAreaCell(canvas, column, bounds);
                break;
        }
    }

    private static void DrawBooleanPointCell(SKCanvas canvas, int column, SKRect bounds)
    {
        var center = Center(bounds);
        var left = new SKPoint(bounds.Left + bounds.Width * 0.34f, center.Y);
        var right = new SKPoint(bounds.Right - bounds.Width * 0.34f, center.Y);
        var shared = center;
        DrawBooleanScopeLine(canvas, bounds);

        switch (column)
        {
            case 0:
                DrawPoint(canvas, left, PositiveStroke, 5.4f);
                DrawPoint(canvas, right, InverseStroke, 5.4f);
                break;
            case 1:
                DrawCoincidentPoints(canvas, shared, sharedSupport: false);
                DrawLine(canvas, new SKPoint(shared.X - 10, shared.Y - 12), new SKPoint(shared.X + 10, shared.Y + 12), TickColor, 1.2f, 155);
                break;
            case 2:
                DrawCoincidentPoints(canvas, shared, sharedSupport: true);
                break;
            case 3:
                DrawPoint(canvas, left, PositiveStroke, 5.4f);
                DrawPoint(canvas, right, InverseStroke, 5.4f);
                DrawLine(canvas, left, right, BoundaryColor, 1.1f, 115);
                break;
            case 4:
                DrawCoincidentPoints(canvas, shared, sharedSupport: true);
                DrawPoint(canvas, shared, EndpointColor, 3.1f);
                break;
            case 5:
                DrawPoint(canvas, left, PositiveStroke, 5.6f);
                DrawExcludedPoint(canvas, right);
                break;
        }
    }

    private static void DrawBooleanSegmentCell(SKCanvas canvas, int column, SKRect bounds)
    {
        var lanes = DrawBooleanSegmentLanes(canvas, bounds);
        var separatedA = (Start: 0.14f, End: 0.38f);
        var separatedB = (Start: 0.60f, End: 0.86f);
        var touchA = (Start: 0.14f, End: 0.50f);
        var touchB = (Start: 0.50f, End: 0.86f);
        var a = (Start: 0.16f, End: 0.66f);
        var b = (Start: 0.42f, End: 0.86f);
        var overlapStart = Math.Max(a.Start, b.Start);
        var overlapEnd = Math.Min(a.End, b.End);

        switch (column)
        {
            case 0:
                DrawVerticalSupport(canvas, lanes.A, lanes.Top, lanes.Bottom, separatedA.Start, separatedA.End, PositiveStroke, 7.6f);
                DrawVerticalSupport(canvas, lanes.B, lanes.Top, lanes.Bottom, separatedB.Start, separatedB.End, InverseStroke, 7.6f);
                break;
            case 1:
                DrawVerticalSupport(canvas, lanes.A, lanes.Top, lanes.Bottom, touchA.Start, touchA.End, PositiveStroke, 7.4f);
                DrawVerticalSupport(canvas, lanes.B, lanes.Top, lanes.Bottom, touchB.Start, touchB.End, InverseStroke, 7.4f);
                DrawPoint(canvas, new SKPoint(lanes.R, YOnLane(lanes.Top, lanes.Bottom, 0.50f)), EndpointColor, 4.5f);
                break;
            case 2:
                DrawVerticalSupport(canvas, lanes.A, lanes.Top, lanes.Bottom, a.Start, a.End, PositiveStroke, 7.4f);
                DrawVerticalSupport(canvas, lanes.B, lanes.Top, lanes.Bottom, b.Start, b.End, InverseStroke, 7.4f);
                DrawVerticalSupport(canvas, lanes.R, lanes.Top, lanes.Bottom, overlapStart, overlapEnd, EndpointColor, 7.8f, alpha: 210);
                break;
            case 3:
                DrawVerticalSupport(canvas, lanes.A, lanes.Top, lanes.Bottom, a.Start, a.End, PositiveStroke, 5.4f, alpha: 150);
                DrawVerticalSupport(canvas, lanes.B, lanes.Top, lanes.Bottom, b.Start, b.End, InverseStroke, 5.4f, alpha: 150);
                DrawVerticalSupport(canvas, lanes.R, lanes.Top, lanes.Bottom, a.Start, b.End, PositiveStroke, 8.4f, alpha: 225);
                DrawVerticalSupport(canvas, lanes.R, lanes.Top, lanes.Bottom, overlapStart, overlapEnd, EndpointColor, 4.1f, alpha: 230);
                break;
            case 4:
                DrawVerticalSupport(canvas, lanes.A, lanes.Top, lanes.Bottom, a.Start, a.End, PositiveStroke, 5.8f, alpha: 145);
                DrawVerticalSupport(canvas, lanes.B, lanes.Top, lanes.Bottom, b.Start, b.End, InverseStroke, 5.8f, alpha: 145);
                DrawVerticalSupport(canvas, lanes.R, lanes.Top, lanes.Bottom, overlapStart, overlapEnd, EndpointColor, 8.8f, alpha: 230);
                break;
            case 5:
                DrawVerticalSupport(canvas, lanes.A, lanes.Top, lanes.Bottom, a.Start, a.End, PositiveStroke, 5.8f, alpha: 160);
                DrawVerticalSupport(canvas, lanes.B, lanes.Top, lanes.Bottom, b.Start, b.End, InverseStroke, 5.8f, alpha: 125);
                DrawVerticalSupport(canvas, lanes.R, lanes.Top, lanes.Bottom, a.Start, overlapStart, PositiveStroke, 8.4f, alpha: 230);
                DrawResidueMark(canvas, new SKPoint(lanes.B + 6, YOnLane(lanes.Top, lanes.Bottom, (overlapStart + overlapEnd) * 0.5f)));
                break;
        }
    }

    private void DrawBooleanAreaCell(SKCanvas canvas, int column, SKRect bounds)
    {
        var a = new SKRect(bounds.Left + bounds.Width * 0.16f, bounds.Top + bounds.Height * 0.27f, bounds.Left + bounds.Width * 0.58f, bounds.Bottom - bounds.Height * 0.20f);
        var b = new SKRect(bounds.Right - bounds.Width * 0.58f, bounds.Top + bounds.Height * 0.20f, bounds.Right - bounds.Width * 0.16f, bounds.Bottom - bounds.Height * 0.27f);
        var shared = SKRect.Intersect(a, b);

        switch (column)
        {
            case 0:
                DrawAreaParticipant(canvas, a, PositiveColor, PositiveStroke);
                DrawAreaParticipant(canvas, OffsetRect(b, bounds.Width * 0.15f, 0), InverseColor, InverseStroke);
                break;
            case 1:
                DrawAreaParticipant(canvas, a, PositiveColor, PositiveStroke);
                DrawAreaParticipant(canvas, OffsetRect(a, a.Width, 0), InverseColor, InverseStroke);
                DrawPoint(canvas, new SKPoint(a.Right, a.MidY), EndpointColor, 3.8f);
                break;
            case 2:
                DrawAreaParticipant(canvas, a, PositiveColor, PositiveStroke);
                DrawAreaParticipant(canvas, b, InverseColor, InverseStroke);
                DrawRectFill(canvas, shared, EndpointColor, 92);
                break;
            case 3:
                DrawAreaParticipant(canvas, a, PositiveColor, PositiveStroke);
                DrawAreaParticipant(canvas, b, PositiveColor, PositiveStroke);
                DrawAreaUnionOutline(canvas, a, b);
                break;
            case 4:
                DrawRectFill(canvas, shared, EndpointColor, 190);
                DrawRectStroke(canvas, shared, EndpointColor, 1.8f);
                break;
            case 5:
                DrawAreaParticipant(canvas, a, PositiveColor, PositiveStroke);
                DrawRectFill(canvas, shared, theme.Background, 235);
                DrawRectStroke(canvas, shared, InverseStroke, 1.6f);
                DrawResidueMark(canvas, new SKPoint(b.Right - 4, b.Top + 4));
                break;
        }
    }

    private static void DrawBooleanScopeLine(SKCanvas canvas, SKRect bounds)
    {
        DrawLine(
            canvas,
            new SKPoint(bounds.Left + bounds.Width * 0.18f, bounds.MidY),
            new SKPoint(bounds.Right - bounds.Width * 0.18f, bounds.MidY),
            BoundaryColor,
            1.1f,
            90);
    }

    private static void DrawCoincidentPoints(SKCanvas canvas, SKPoint point, bool sharedSupport)
    {
        using var positive = FillPaint(PositiveColor, sharedSupport ? (byte)116 : (byte)74);
        using var inverse = StrokePaint(InverseStroke, 2.2f, sharedSupport ? (byte)235 : (byte)175);
        canvas.DrawCircle(point, sharedSupport ? 8.4f : 7.2f, positive);
        canvas.DrawCircle(point, 7.2f, inverse);
        DrawPoint(canvas, point, SiteColor, 3.2f);
    }

    private static void DrawExcludedPoint(SKCanvas canvas, SKPoint point)
    {
        using var stroke = StrokePaint(InverseStroke, 1.8f, 150);
        canvas.DrawCircle(point, 5.8f, stroke);
        DrawLine(canvas, new SKPoint(point.X - 7, point.Y + 7), new SKPoint(point.X + 7, point.Y - 7), InverseStroke, 1.7f, 170);
    }

    private static (float A, float B, float R, float Top, float Bottom) DrawBooleanSegmentLanes(SKCanvas canvas, SKRect bounds)
    {
        var top = bounds.Top + 21f;
        var bottom = bounds.Bottom - 10f;
        var a = bounds.Left + bounds.Width * 0.24f;
        var b = bounds.Left + bounds.Width * 0.50f;
        var r = bounds.Left + bounds.Width * 0.76f;
        DrawVerticalCarrier(canvas, a, top, bottom);
        DrawVerticalCarrier(canvas, b, top, bottom);
        DrawVerticalCarrier(canvas, r, top, bottom);
        DrawLaneMarker(canvas, "A", new SKPoint(a, bounds.Top + 10f), PositiveStroke);
        DrawLaneMarker(canvas, "B", new SKPoint(b, bounds.Top + 10f), InverseStroke);
        DrawLaneMarker(canvas, "R", new SKPoint(r, bounds.Top + 10f), EndpointColor);
        return (a, b, r, top, bottom);
    }

    private static void DrawVerticalCarrier(SKCanvas canvas, float x, float top, float bottom)
    {
        DrawLine(canvas, new SKPoint(x, top), new SKPoint(x, bottom), BoundaryColor, 1.1f, 125);
        for (var index = 1; index < 5; index++)
        {
            var y = top + (bottom - top) * index / 5f;
            DrawLine(canvas, new SKPoint(x - 5, y), new SKPoint(x + 5, y), TickColor, 0.9f, 120);
        }
    }

    private static void DrawLaneMarker(SKCanvas canvas, string label, SKPoint point, SKColor color)
    {
        DrawPoint(canvas, point, color, 4.1f);
        using var paint = TextPaint(8.5f, color, bold: true);
        var width = paint.MeasureText(label);
        canvas.DrawText(label, point.X - width * 0.5f, point.Y - 7.5f, paint);
    }

    private static void DrawVerticalSupport(
        SKCanvas canvas,
        float x,
        float top,
        float bottom,
        float start,
        float end,
        SKColor color,
        float width,
        byte alpha = 235)
    {
        DrawLine(
            canvas,
            new SKPoint(x, YOnLane(top, bottom, start)),
            new SKPoint(x, YOnLane(top, bottom, end)),
            color,
            width,
            alpha);
    }

    private static float YOnLane(float top, float bottom, float fraction) =>
        top + (bottom - top) * Math.Clamp(fraction, 0f, 1f);

    private static void DrawAreaParticipant(SKCanvas canvas, SKRect rect, SKColor fill, SKColor stroke)
    {
        DrawRectFill(canvas, rect, fill, 125);
        DrawRectStroke(canvas, rect, stroke, 1.4f);
    }

    private static void DrawAreaUnionOutline(SKCanvas canvas, SKRect a, SKRect b)
    {
        var outline = new SKRect(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
        DrawRectStroke(canvas, outline, PositiveStroke, 1.9f);
    }

    private static void DrawRectFill(SKCanvas canvas, SKRect rect, SKColor color, byte alpha)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var fill = FillPaint(color, alpha);
        canvas.DrawRect(rect, fill);
    }

    private static void DrawRectStroke(SKCanvas canvas, SKRect rect, SKColor color, float width)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var stroke = StrokePaint(color, width);
        canvas.DrawRect(rect, stroke);
    }

    private static SKRect OffsetRect(SKRect rect, float dx, float dy) =>
        new(rect.Left + dx, rect.Top + dy, rect.Right + dx, rect.Bottom + dy);

    private static void DrawResidueMark(SKCanvas canvas, SKPoint point)
    {
        using var stroke = StrokePaint(InverseStroke, 1.5f, 165);
        canvas.DrawLine(point.X - 5, point.Y - 5, point.X + 5, point.Y + 5, stroke);
        canvas.DrawLine(point.X + 5, point.Y - 5, point.X - 5, point.Y + 5, stroke);
    }

    private static void DrawTipBranch(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        var left = new SKPoint(bounds.Left + 8, hub.Y);
        var up = new SKPoint(bounds.Right - 10, bounds.Top + bounds.Height * 0.28f);
        var down = new SKPoint(bounds.Right - 10, bounds.Bottom - bounds.Height * 0.28f);
        DrawCarrierLine(canvas, left, hub);
        DrawVectorArrow(canvas, hub, up, PositiveStroke, 2.1f);
        DrawVectorArrow(canvas, hub, down, PositiveStroke, 2.1f);
        DrawOrthogonalForce(canvas, new SKPoint(hub.X, bounds.Bottom - 8), hub);
        DrawPoint(canvas, hub, EndpointColor, 4.8f);
    }

    private static void DrawTipMerge(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        var leftUp = new SKPoint(bounds.Left + 10, bounds.Top + bounds.Height * 0.28f);
        var leftDown = new SKPoint(bounds.Left + 10, bounds.Bottom - bounds.Height * 0.28f);
        var right = new SKPoint(bounds.Right - 8, hub.Y);
        DrawVectorArrow(canvas, leftUp, hub, PositiveStroke, 2f);
        DrawVectorArrow(canvas, leftDown, hub, PositiveStroke, 2f);
        DrawVectorArrow(canvas, hub, right, PositiveStroke, 2.2f);
        DrawOrthogonalForce(canvas, new SKPoint(hub.X, bounds.Top + 8), hub);
        DrawPoint(canvas, hub, EndpointColor, 4.8f);
    }

    private static void DrawButtAndCorner(SKCanvas canvas, SKRect bounds)
    {
        var leftCell = new SKRect(bounds.Left, bounds.Top, bounds.MidX - 3, bounds.Bottom);
        var rightCell = new SKRect(bounds.MidX + 3, bounds.Top, bounds.Right, bounds.Bottom);
        var butt = Center(leftCell);
        DrawCarrierLine(canvas, new SKPoint(leftCell.Left + 4, butt.Y), new SKPoint(leftCell.Right - 4, butt.Y));
        DrawVectorArrow(canvas, new SKPoint(butt.X, leftCell.Top + 8), butt, InverseStroke, 1.8f);
        DrawPoint(canvas, butt, EndpointColor, 4.2f);

        var corner = new SKPoint(rightCell.Left + rightCell.Width * 0.42f, rightCell.Bottom - rightCell.Height * 0.30f);
        DrawCarrierLine(canvas, new SKPoint(rightCell.Left + 4, corner.Y), corner);
        DrawVectorArrow(canvas, corner, new SKPoint(rightCell.Right - 5, rightCell.Top + rightCell.Height * 0.25f), PositiveStroke, 1.9f);
        DrawPoint(canvas, corner, EndpointColor, 4.2f);
    }

    private static void DrawArcTipJoin(SKCanvas canvas, SKRect bounds)
    {
        var hub = new SKPoint(bounds.Left + bounds.Width * 0.34f, bounds.MidY + bounds.Height * 0.08f);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), hub);
        using var path = new SKPath();
        path.MoveTo(hub);
        path.QuadTo(bounds.MidX + 5, bounds.Top + 8, bounds.Right - 8, bounds.Top + bounds.Height * 0.35f);
        DrawBridgePath(canvas, path, PositiveStroke, 2.2f, glow: true);
        DrawOrthogonalForce(canvas, new SKPoint(hub.X, bounds.Bottom - 9), hub);
        DrawPoint(canvas, hub, EndpointColor, 4.6f);
    }

    private static void DrawMiddleBranch(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), new SKPoint(bounds.Right - 8, hub.Y));
        DrawVectorArrow(canvas, hub, new SKPoint(hub.X, bounds.Top + 8), PositiveStroke, 2.2f);
        DrawPoint(canvas, hub, EndpointColor, 4.8f);
        DrawOrthogonalForce(canvas, new SKPoint(hub.X, bounds.Bottom - 9), hub);
    }

    private static void DrawMiddleMerge(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), new SKPoint(bounds.Right - 8, hub.Y));
        DrawVectorArrow(canvas, new SKPoint(hub.X, bounds.Top + 8), hub, InverseStroke, 1.9f);
        DrawVectorArrow(canvas, new SKPoint(hub.X, bounds.Bottom - 8), hub, InverseStroke, 1.9f);
        DrawVectorArrow(canvas, hub, new SKPoint(bounds.Right - 8, hub.Y), PositiveStroke, 1.8f);
        DrawPoint(canvas, hub, EndpointColor, 4.8f);
    }

    private static void DrawOrthogonalArcMiddle(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), new SKPoint(bounds.Right - 8, hub.Y));
        using var arc = new SKPath();
        arc.MoveTo(new SKPoint(bounds.Left + bounds.Width * 0.33f, bounds.Top + 8));
        arc.QuadTo(new SKPoint(bounds.MidX, bounds.Top + bounds.Height * 0.24f), hub);
        DrawBridgePath(canvas, arc, PositiveStroke, 2.1f, glow: true);
        DrawPoint(canvas, hub, EndpointColor, 4.6f);
        DrawLine(canvas, new SKPoint(hub.X, hub.Y - 10), new SKPoint(hub.X, hub.Y + 10), TickColor, 1f, 150);
    }

    private static void DrawCrossingNoJoin(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), new SKPoint(bounds.Right - 8, hub.Y));
        DrawLine(canvas, new SKPoint(hub.X, bounds.Top + 8), new SKPoint(hub.X, hub.Y - 8), InverseStroke, 1.8f, 190);
        DrawLine(canvas, new SKPoint(hub.X, hub.Y + 8), new SKPoint(hub.X, bounds.Bottom - 8), InverseStroke, 1.8f, 190);
        using var gap = FillPaint(new SKColor(255, 255, 255), 210);
        canvas.DrawCircle(hub, 6.5f, gap);
        DrawLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), new SKPoint(bounds.Right - 8, hub.Y), BoundaryColor, 1.7f, 180);
    }

    private static void DrawMultiBranch(SKCanvas canvas, SKRect bounds)
    {
        var hub = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 8, hub.Y), hub);
        for (var index = 0; index < 4; index++)
        {
            var angle = -50f + index * 33f;
            var end = PointOnCircle(hub, Math.Min(bounds.Width, bounds.Height) * 0.40f, angle);
            DrawVectorArrow(canvas, hub, end, PositiveStroke, 1.8f);
        }
        DrawPoint(canvas, hub, EndpointColor, 5f);
    }

    private static void DrawBranchMergeNetwork(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 8, bounds.MidY);
        var b = new SKPoint(bounds.Left + bounds.Width * 0.36f, bounds.MidY);
        var c = new SKPoint(bounds.Left + bounds.Width * 0.66f, bounds.MidY);
        var d = new SKPoint(bounds.Right - 8, bounds.MidY);
        DrawVectorArrow(canvas, a, b, PositiveStroke, 1.6f);
        DrawVectorArrow(canvas, b, new SKPoint(c.X, bounds.Top + 10), PositiveStroke, 1.4f);
        DrawVectorArrow(canvas, b, new SKPoint(c.X, bounds.Bottom - 10), PositiveStroke, 1.4f);
        DrawVectorArrow(canvas, new SKPoint(c.X, bounds.Top + 10), d, InverseStroke, 1.4f);
        DrawVectorArrow(canvas, new SKPoint(c.X, bounds.Bottom - 10), d, InverseStroke, 1.4f);
        DrawPoint(canvas, b, EndpointColor, 4.5f);
        DrawPoint(canvas, d, EndpointColor, 4.5f);
    }

    private static void DrawEnclosureArea(SKCanvas canvas, SKRect bounds)
    {
        var rect = Inset(bounds, 12);
        var start = new SKPoint(rect.Left, rect.Top + 4);
        using var area = new SKPath();
        area.MoveTo(start);
        area.LineTo(rect.Left, rect.Bottom - 4);
        area.LineTo(rect.Right - 11, rect.Bottom - 6);
        area.QuadTo(rect.Right + 7, rect.MidY, rect.Right - 11, rect.Top + 6);
        area.Close();

        using var fill = FillPaint(PositiveColor, 84);
        using var stroke = StrokePaint(PositiveStroke, 1.9f);
        canvas.DrawPath(area, fill);
        canvas.DrawPath(area, stroke);
        DrawPoint(canvas, start, EndpointColor, 3.8f);
        DrawUnrelatedBranchLine(canvas, bounds);
    }

    private static void DrawPartialUArea(SKCanvas canvas, SKRect bounds)
    {
        var rect = Inset(bounds, 13);
        var openY = rect.Top + rect.Height * 0.34f;
        using var area = FillPaint(PositiveColor, 62);
        using var shape = new SKPath();
        shape.MoveTo(rect.Left, rect.Top + 4);
        shape.LineTo(rect.Left, rect.Bottom);
        shape.LineTo(rect.Right - 10, rect.Bottom);
        shape.QuadTo(rect.Right + 8, rect.MidY + 4, rect.Right - 8, openY);
        shape.LineTo(rect.Left, rect.Top + 4);
        canvas.DrawPath(shape, area);

        using var settled = new SKPath();
        settled.MoveTo(rect.Left, rect.Top + 4);
        settled.LineTo(rect.Left, rect.Bottom);
        settled.LineTo(rect.Right - 10, rect.Bottom);
        settled.QuadTo(rect.Right + 8, rect.MidY + 4, rect.Right - 8, openY);
        using var stroke = StrokePaint(PositiveStroke, 2f);
        canvas.DrawPath(settled, stroke);

        using var dash = StrokePaint(TickColor, 1.2f, 160);
        using var pathEffect = SKPathEffect.CreateDash([4f, 4f], 0);
        dash.PathEffect = pathEffect;
        using var unsettled = new SKPath();
        unsettled.MoveTo(rect.Right - 8, openY);
        unsettled.QuadTo(rect.Right + 1, rect.Top + 5, rect.Right - 12, rect.Top + 4);
        unsettled.LineTo(rect.Left, rect.Top + 4);
        canvas.DrawPath(unsettled, dash);
        DrawUnrelatedBranchLine(canvas, bounds);
    }

    private static void DrawUnrelatedBranchLine(SKCanvas canvas, SKRect bounds)
    {
        var start = new SKPoint(bounds.Left + 8, bounds.Top + bounds.Height * 0.26f);
        var end = new SKPoint(bounds.Right - 8, bounds.Top + bounds.Height * 0.14f);
        DrawVectorArrow(canvas, start, end, BoundaryColor, 1.25f, 150);
    }

    private static void DrawOrthogonalForce(SKCanvas canvas, SKPoint start, SKPoint end)
    {
        using var soft = StrokePaint(PositiveColor, 8f, 35);
        canvas.DrawLine(start, end, soft);
        DrawVectorArrow(canvas, start, end, PositiveStroke, 1.15f, 170);
    }

    private static void DrawCurveJoinCell(SKCanvas canvas, int row, int column, SKRect bounds)
    {
        var geometry = CurveJoinGeometry.For(row, bounds);
        if (column == 5)
        {
            DrawPlottedCurveContrast(canvas, bounds, geometry);
            return;
        }

        DrawCurveCarriers(canvas, geometry);
        switch (column)
        {
            case 0:
                DrawCornerJoin(canvas, geometry);
                break;
            case 1:
                DrawArcBlendJoin(canvas, geometry);
                break;
            case 2:
                DrawBezierJoin(canvas, geometry);
                break;
            case 3:
                DrawClothoidJoin(canvas, geometry);
                break;
            case 4:
                DrawInvoluteJoin(canvas, geometry);
                break;
        }
    }

    private static void DrawCurveCarriers(SKCanvas canvas, CurveJoinGeometry geometry)
    {
        if (geometry.FirstArc is { } firstArc)
        {
            DrawCarrierArc(canvas, firstArc);
        }
        else
        {
            DrawCarrierLine(canvas, geometry.FirstStart, geometry.Start);
        }

        if (geometry.SecondArc is { } secondArc)
        {
            DrawCarrierArc(canvas, secondArc);
        }
        else
        {
            DrawCarrierLine(canvas, geometry.End, geometry.SecondEnd);
        }

        DrawPoint(canvas, geometry.Start, SiteColor, 4.2f);
        DrawPoint(canvas, geometry.End, SiteColor, 4.2f);
        DrawVectorArrow(canvas, geometry.Start, OffsetPoint(geometry.Start, geometry.StartTangent, 14f), BoundaryColor, 1.05f, 150);
        DrawVectorArrow(canvas, OffsetPoint(geometry.End, geometry.EndTangent, -14f), geometry.End, BoundaryColor, 1.05f, 150);
    }

    private static void DrawCarrierLine(SKCanvas canvas, SKPoint start, SKPoint end)
    {
        DrawLine(canvas, start, end, BoundaryColor, 1.7f, 180);
    }

    private static void DrawCarrierArc(SKCanvas canvas, CurveArc arc)
    {
        var oval = new SKRect(
            arc.Center.X - arc.Radius,
            arc.Center.Y - arc.Radius,
            arc.Center.X + arc.Radius,
            arc.Center.Y + arc.Radius);
        using var paint = StrokePaint(BoundaryColor, 1.7f, 180);
        canvas.DrawArc(oval, arc.StartAngle, arc.Sweep, false, paint);
    }

    private static void DrawCornerJoin(SKCanvas canvas, CurveJoinGeometry geometry)
    {
        var corner = new SKPoint(
            (geometry.Start.X + geometry.End.X) * 0.5f,
            (geometry.Start.Y + geometry.End.Y) * 0.5f);
        using var path = new SKPath();
        path.MoveTo(geometry.Start);
        path.LineTo(corner);
        path.LineTo(geometry.End);
        DrawBridgePath(canvas, path, InverseStroke, 2.1f, glow: false);
        DrawPoint(canvas, corner, EndpointColor, 4.6f);
    }

    private static void DrawArcBlendJoin(SKCanvas canvas, CurveJoinGeometry geometry)
    {
        using var path = new SKPath();
        var control = OffsetPoint(
            OffsetPoint(geometry.Start, geometry.StartTangent, geometry.ControlScale * 0.45f),
            geometry.EndTangent,
            geometry.ControlScale * 0.28f);
        path.MoveTo(geometry.Start);
        path.QuadTo(control, geometry.End);
        DrawBridgePath(canvas, path, PositiveStroke, 2.2f, glow: true);
    }

    private static void DrawBezierJoin(SKCanvas canvas, CurveJoinGeometry geometry)
    {
        var c1 = OffsetPoint(geometry.Start, geometry.StartTangent, geometry.ControlScale);
        var c2 = OffsetPoint(geometry.End, geometry.EndTangent, -geometry.ControlScale);
        DrawLine(canvas, geometry.Start, c1, TickColor, 1f, 135);
        DrawLine(canvas, geometry.End, c2, TickColor, 1f, 135);
        DrawPoint(canvas, c1, TickColor, 2.4f);
        DrawPoint(canvas, c2, TickColor, 2.4f);

        using var path = new SKPath();
        path.MoveTo(geometry.Start);
        path.CubicTo(c1, c2, geometry.End);
        DrawBridgePath(canvas, path, PositiveStroke, 2.3f, glow: true);
    }

    private static void DrawClothoidJoin(SKCanvas canvas, CurveJoinGeometry geometry)
    {
        var c1 = OffsetPoint(geometry.Start, geometry.StartTangent, geometry.ControlScale * 0.35f);
        var c2 = OffsetPoint(geometry.End, geometry.EndTangent, -geometry.ControlScale * 1.2f);
        using var path = new SKPath();
        path.MoveTo(geometry.Start);
        path.CubicTo(c1, c2, geometry.End);
        DrawBridgePath(canvas, path, new SKColor(49, 132, 153), 2.3f, glow: true);

        for (var index = 1; index <= 3; index++)
        {
            var t = index / 4f;
            var point = CubicPoint(geometry.Start, c1, c2, geometry.End, t);
            var tangent = Normalize(CubicTangent(geometry.Start, c1, c2, geometry.End, t));
            var normal = new SKPoint(-tangent.Y, tangent.X);
            var length = 4f + index * 2.2f;
            DrawLine(canvas, OffsetPoint(point, normal, -length), OffsetPoint(point, normal, length), TickColor, 1f, 145);
        }
    }

    private static void DrawInvoluteJoin(SKCanvas canvas, CurveJoinGeometry geometry)
    {
        var baseCenter = new SKPoint(
            (geometry.Start.X + geometry.End.X) * 0.5f,
            (geometry.Start.Y + geometry.End.Y) * 0.5f + 7f);
        var baseRadius = Math.Min(18f, Math.Max(11f, geometry.ControlScale * 0.30f));
        DrawCircleBoundary(canvas, baseCenter, baseRadius, 1f, WithAlpha(BoundaryColor, 150));

        using var path = new SKPath();
        path.MoveTo(geometry.Start);
        var turns = 5;
        for (var index = 1; index <= turns; index++)
        {
            var t = index / (float)turns;
            var angle = -145f + t * 230f;
            var unwind = baseRadius + t * geometry.ControlScale * 0.60f;
            var spiral = PointOnCircle(baseCenter, unwind, angle);
            var blend = Lerp(spiral, Lerp(geometry.Start, geometry.End, t), 0.48f);
            path.LineTo(blend);
        }
        path.LineTo(geometry.End);
        DrawBridgePath(canvas, path, PositiveStroke, 2.2f, glow: true);
        DrawLine(canvas, baseCenter, geometry.Start, TickColor, 1f, 115);
    }

    private static void DrawPlottedCurveContrast(SKCanvas canvas, SKRect bounds, CurveJoinGeometry geometry)
    {
        var plot = Inset(bounds, 9);
        using var axis = StrokePaint(TickColor, 1f, 135);
        canvas.DrawLine(plot.Left, plot.Bottom - 10, plot.Right, plot.Bottom - 10, axis);
        canvas.DrawLine(plot.Left + 8, plot.Top, plot.Left + 8, plot.Bottom, axis);

        var c1 = OffsetPoint(geometry.Start, geometry.StartTangent, geometry.ControlScale);
        var c2 = OffsetPoint(geometry.End, geometry.EndTangent, -geometry.ControlScale);
        using var path = new SKPath();
        path.MoveTo(geometry.Start);
        path.CubicTo(c1, c2, geometry.End);
        DrawBridgePath(canvas, path, new SKColor(66, 122, 153), 1.8f, glow: false);
        for (var index = 0; index <= 6; index++)
        {
            var point = CubicPoint(geometry.Start, c1, c2, geometry.End, index / 6f);
            DrawPoint(canvas, point, BoundaryMarkerColor, 2f);
        }
    }

    private static void DrawBridgePath(SKCanvas canvas, SKPath path, SKColor color, float width, bool glow)
    {
        if (glow)
        {
            using var soft = StrokePaint(color, width + 6f, 42);
            canvas.DrawPath(path, soft);
        }

        using var paint = StrokePaint(color, width);
        canvas.DrawPath(path, paint);
    }

    private static SKPoint CubicPoint(SKPoint a, SKPoint b, SKPoint c, SKPoint d, float t)
    {
        var u = 1f - t;
        var aw = u * u * u;
        var bw = 3f * u * u * t;
        var cw = 3f * u * t * t;
        var dw = t * t * t;
        return new SKPoint(
            a.X * aw + b.X * bw + c.X * cw + d.X * dw,
            a.Y * aw + b.Y * bw + c.Y * cw + d.Y * dw);
    }

    private static SKPoint CubicTangent(SKPoint a, SKPoint b, SKPoint c, SKPoint d, float t)
    {
        var u = 1f - t;
        var ab = 3f * u * u;
        var bc = 6f * u * t;
        var cd = 3f * t * t;
        return new SKPoint(
            (b.X - a.X) * ab + (c.X - b.X) * bc + (d.X - c.X) * cd,
            (b.Y - a.Y) * ab + (c.Y - b.Y) * bc + (d.Y - c.Y) * cd);
    }

    private static SKPoint OffsetPoint(SKPoint point, SKPoint direction, float distance) =>
        new(point.X + direction.X * distance, point.Y + direction.Y * distance);

    private static SKPoint Normalize(SKPoint point)
    {
        var length = MathF.Sqrt(point.X * point.X + point.Y * point.Y);
        return length <= 0.001f ? new SKPoint(1, 0) : new SKPoint(point.X / length, point.Y / length);
    }

    private readonly record struct CurveArc(SKPoint Center, float Radius, float StartAngle, float Sweep);

    private readonly record struct CurveJoinGeometry(
        SKPoint FirstStart,
        SKPoint Start,
        SKPoint End,
        SKPoint SecondEnd,
        SKPoint StartTangent,
        SKPoint EndTangent,
        float ControlScale,
        CurveArc? FirstArc,
        CurveArc? SecondArc)
    {
        public static CurveJoinGeometry For(int row, SKRect bounds) =>
            row switch
            {
                1 => LineToArc(bounds),
                2 => ArcToArc(bounds),
                _ => LineToLine(bounds)
            };

        private static CurveJoinGeometry LineToLine(SKRect bounds)
        {
            var y = bounds.MidY + bounds.Height * 0.10f;
            var start = new SKPoint(bounds.Left + bounds.Width * 0.34f, y);
            var end = new SKPoint(bounds.Right - bounds.Width * 0.32f, bounds.MidY - bounds.Height * 0.18f);
            return new CurveJoinGeometry(
                new SKPoint(bounds.Left + 8, y),
                start,
                end,
                new SKPoint(bounds.Right - 8, bounds.MidY - bounds.Height * 0.30f),
                Normalize(new SKPoint(1f, -0.06f)),
                Normalize(new SKPoint(1f, -0.18f)),
                bounds.Width * 0.22f,
                null,
                null);
        }

        private static CurveJoinGeometry LineToArc(SKRect bounds)
        {
            var start = new SKPoint(bounds.Left + bounds.Width * 0.32f, bounds.MidY + bounds.Height * 0.15f);
            var center = new SKPoint(bounds.Right - bounds.Width * 0.25f, bounds.MidY + bounds.Height * 0.03f);
            var radius = Math.Min(bounds.Width, bounds.Height) * 0.26f;
            var arcStartAngle = 190f;
            var end = PointOnCircle(center, radius, arcStartAngle);
            return new CurveJoinGeometry(
                new SKPoint(bounds.Left + 8, start.Y),
                start,
                end,
                PointOnCircle(center, radius, 95f),
                new SKPoint(1f, 0f),
                TangentForAngle(arcStartAngle, clockwise: false),
                bounds.Width * 0.24f,
                null,
                new CurveArc(center, radius, arcStartAngle, -95f));
        }

        private static CurveJoinGeometry ArcToArc(SKRect bounds)
        {
            var radius = Math.Min(bounds.Width, bounds.Height) * 0.24f;
            var leftCenter = new SKPoint(bounds.Left + bounds.Width * 0.30f, bounds.MidY + bounds.Height * 0.06f);
            var rightCenter = new SKPoint(bounds.Right - bounds.Width * 0.28f, bounds.MidY - bounds.Height * 0.04f);
            var startAngle = -20f;
            var endAngle = 200f;
            var start = PointOnCircle(leftCenter, radius, startAngle);
            var end = PointOnCircle(rightCenter, radius, endAngle);
            return new CurveJoinGeometry(
                PointOnCircle(leftCenter, radius, -105f),
                start,
                end,
                PointOnCircle(rightCenter, radius, 105f),
                TangentForAngle(startAngle, clockwise: true),
                TangentForAngle(endAngle, clockwise: false),
                bounds.Width * 0.25f,
                new CurveArc(leftCenter, radius, -105f, 85f),
                new CurveArc(rightCenter, radius, endAngle, -95f));
        }

        private static SKPoint TangentForAngle(float degrees, bool clockwise)
        {
            var radians = degrees * MathF.PI / 180f;
            var radial = new SKPoint(MathF.Cos(radians), MathF.Sin(radians));
            return clockwise
                ? new SKPoint(radial.Y, -radial.X)
                : new SKPoint(-radial.Y, radial.X);
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

    private void DrawLadderOrientation(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawLinearOrientation(canvas, bounds);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.34f;
        DrawCircleBoundary(canvas, center, radius, width: 1.9f, color: inverse ? WithAlpha(InverseStroke, 120) : WithAlpha(PositiveStroke, 145));
        DrawPoint(canvas, center, SiteColor, 8);
        DrawRadiusEvidence(canvas, center, new SKPoint(center.X + radius * 0.86f, center.Y));
        DrawRadialTicks(canvas, center, radius);

        var arcRadius = radius * 1.12f;
        var start = inverse ? -145f : -35f;
        var sweep = inverse ? 105f : -105f;
        var color = inverse ? InverseStroke : PositiveStroke;
        using var paint = StrokePaint(color, 4.2f);
        paint.StrokeCap = SKStrokeCap.Butt;
        var oval = new SKRect(center.X - arcRadius, center.Y - arcRadius, center.X + arcRadius, center.Y + arcRadius);
        canvas.DrawArc(oval, start, sweep, false, paint);
        var arrowStart = PointOnCircle(center, arcRadius, start + sweep * 0.84f);
        var arrowEnd = PointOnCircle(center, arcRadius, start + sweep);
        using var arrow = StrokePaint(color, 3.2f);
        DrawArrowHead(canvas, arrowStart, arrowEnd, arrow);
    }

    private void DrawLadderDirection(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawLinearDirection(canvas, bounds);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        DrawRadialSignedDirection(canvas, bounds, inverse);
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

    private void DrawLadderBoundedArea(SKCanvas canvas, GlossaryLadderRow row, SKRect bounds)
    {
        if (row == GlossaryLadderRow.Linear)
        {
            DrawBoundedLinearArea(canvas, bounds);
            return;
        }

        var inverse = row == GlossaryLadderRow.RadialInverse;
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.33f;
        using var fill = FillPaint(inverse ? InverseColor : PositiveColor, 185);
        canvas.DrawCircle(center, radius, fill);
        DrawCircleBoundary(canvas, center, radius, width: 1.8f, color: inverse ? InverseStroke : PositiveStroke);
        DrawRadialTicks(canvas, center, radius);
        var radiusEnd = new SKPoint(center.X + radius, center.Y);
        DrawPoint(canvas, center, SiteColor, 7.5f);
        DrawRadiusEvidence(canvas, center, radiusEnd);
        DrawPoint(canvas, radiusEnd, BoundaryMarkerColor, 3.2f);
        DrawSpinArrow(canvas, center, radius * 0.78f, inverse);
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

    private static void DrawCenteredMultilineText(SKCanvas canvas, string text, SKRect bounds, SKPaint paint, float lineHeight)
    {
        var lines = LabelLines(text);
        var firstBaseline = bounds.MidY - (lines.Count - 1) * lineHeight * 0.5f + paint.TextSize * 0.34f;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var width = paint.MeasureText(line);
            canvas.DrawText(line, bounds.MidX - width * 0.5f, firstBaseline + index * lineHeight, paint);
        }
    }

    private static void DrawRightAlignedMultilineText(SKCanvas canvas, string text, SKRect bounds, SKPaint paint, float lineHeight)
    {
        var lines = LabelLines(text);
        var firstBaseline = bounds.MidY - (lines.Count - 1) * lineHeight * 0.5f + paint.TextSize * 0.34f;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var width = paint.MeasureText(line);
            canvas.DrawText(line, bounds.Right - width, firstBaseline + index * lineHeight, paint);
        }
    }

    private static IReadOnlyList<string> LabelLines(string text) =>
        text.Contains('\n')
            ? text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            : text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

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

    private static void DrawArcArrow(SKCanvas canvas, SKPoint center, float radius, float startAngle, float endAngle, SKColor color, float width)
    {
        var sweep = endAngle - startAngle;
        using var paint = StrokePaint(color, width);
        var oval = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
        canvas.DrawArc(oval, startAngle, sweep, false, paint);
        var arrowStep = Math.Sign(sweep) * Math.Min(18f, Math.Abs(sweep) * 0.35f);
        var arrowStart = PointOnCircle(center, radius, endAngle - arrowStep);
        var arrowEnd = PointOnCircle(center, radius, endAngle);
        DrawArrowHead(canvas, arrowStart, arrowEnd, paint);
    }

    private static void DrawSign(SKCanvas canvas, string text, SKPoint point, SKColor color, float size)
    {
        using var paint = TextPaint(size, color, bold: true);
        var width = paint.MeasureText(text);
        canvas.DrawText(text, point.X - width * 0.5f, point.Y + size * 0.34f, paint);
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
        var halfSpan = Math.Clamp(bounds.Width * 0.13f, 13f, 17f);
        var start = new SKPoint(center.X - halfSpan, center.Y);
        var end = new SKPoint(center.X + halfSpan, center.Y);
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

    private static void DrawCircleBoundary(SKCanvas canvas, SKPoint center, float radius, float width = 1.25f, SKColor? color = null)
    {
        using var stroke = StrokePaint(color ?? BoundaryColor, width);
        canvas.DrawCircle(center, radius, stroke);
    }

    private static void DrawRadialTicks(SKCanvas canvas, SKPoint center, float radius)
    {
        for (var index = 0; index < 8; index++)
        {
            var angle = index * 45f;
            var a = PointOnCircle(center, radius - 4, angle);
            var b = PointOnCircle(center, radius + 5, angle);
            DrawLine(canvas, a, b, TickColor, 1.1f);
        }
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

    private static void DrawLinearOrientation(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var left = bounds.Left + bounds.Width * 0.16f;
        var right = bounds.Right - bounds.Width * 0.16f;
        var zero = new SKPoint((left + right) * 0.5f, y);
        DrawLine(canvas, new SKPoint(left, y), new SKPoint(right, y), BoundaryColor, 1.4f, 165);
        DrawPoint(canvas, zero, SiteColor, 7.5f);

        DrawVectorArrow(
            canvas,
            new SKPoint(zero.X - 4, y - 14),
            new SKPoint(right, y - 14),
            PositiveStroke,
            2.4f);
        DrawVectorArrow(
            canvas,
            new SKPoint(zero.X + 4, y + 14),
            new SKPoint(left, y + 14),
            InverseStroke,
            2.4f);
        DrawSign(canvas, "+", new SKPoint(right + 8, y - 11), PositiveStroke, 16f);
        DrawSign(canvas, "+", new SKPoint(left - 18, y + 17), InverseStroke, 16f);
    }

    private static void DrawLinearDirection(SKCanvas canvas, SKRect bounds)
    {
        var left = bounds.Left + bounds.Width * 0.14f;
        var right = bounds.Right - bounds.Width * 0.14f;
        var topY = bounds.MidY - bounds.Height * 0.18f;
        var bottomY = bounds.MidY + bounds.Height * 0.18f;
        var zeroX = (left + right) * 0.5f;
        DrawLinearDirectionGuide(canvas, left, right, topY);
        DrawLinearDirectionGuide(canvas, left, right, bottomY);

        DrawVectorArrow(
            canvas,
            new SKPoint(left + 10, topY),
            new SKPoint(right - 2, topY),
            PositiveStroke,
            2.2f);
        DrawOriginRing(canvas, new SKPoint(left + 8, topY), PositiveStroke);
        DrawPoint(canvas, new SKPoint(zeroX, topY), BoundaryColor, 4.8f);
        DrawSign(canvas, "-", new SKPoint(left + 7, topY - 20), PositiveStroke, 18f);
        DrawSign(canvas, "+", new SKPoint(right - 1, topY - 20), PositiveStroke, 18f);

        DrawVectorArrow(
            canvas,
            new SKPoint(right - 10, bottomY),
            new SKPoint(left + 2, bottomY),
            InverseStroke,
            2.2f);
        DrawOriginRing(canvas, new SKPoint(right - 8, bottomY), InverseStroke);
        DrawPoint(canvas, new SKPoint(zeroX, bottomY), BoundaryColor, 4.8f);
        DrawSign(canvas, "+", new SKPoint(left + 6, bottomY + 21), InverseStroke, 18f);
        DrawSign(canvas, "-", new SKPoint(right - 4, bottomY + 21), InverseStroke, 18f);
    }

    private static void DrawLinearDirectionGuide(SKCanvas canvas, float left, float right, float y)
    {
        DrawLine(canvas, new SKPoint(left, y), new SKPoint(right, y), BoundaryColor, 1.1f, 165);
        DrawLine(canvas, new SKPoint(left, y), new SKPoint(left, y - 10), BoundaryColor, 1.1f, 150);
        DrawLine(canvas, new SKPoint(right, y), new SKPoint(right, y - 10), BoundaryColor, 1.1f, 150);
        for (var index = 1; index < 5; index++)
        {
            var x = left + (right - left) * index / 5f;
            DrawLine(canvas, new SKPoint(x, y - 8), new SKPoint(x, y + 8), TickColor, 1.1f);
        }
    }

    private static void DrawOriginRing(SKCanvas canvas, SKPoint center, SKColor color)
    {
        using var fill = FillPaint(new SKColor(255, 190, 190), 210);
        using var stroke = StrokePaint(color, 2.8f);
        canvas.DrawCircle(center, 5.8f, fill);
        canvas.DrawCircle(center, 5.8f, stroke);
    }

    private static void DrawRadialSignedDirection(SKCanvas canvas, SKRect bounds, bool inverse)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.34f;
        DrawCircleBoundary(canvas, center, radius, width: 1.7f);
        DrawRadialTicks(canvas, center, radius);
        DrawPoint(canvas, center, SiteColor, 8);
        var zero = new SKPoint(center.X + radius, center.Y);
        DrawRadiusEvidence(canvas, center, zero);
        DrawPoint(canvas, zero, BoundaryColor, 3.3f);

        var directionAngle = 42f;
        var positiveEndAngle = inverse ? directionAngle : -directionAngle;
        var negativeStartAngle = inverse ? -directionAngle : directionAngle;
        var positiveLabel = PointOnCircle(center, radius * 1.23f, positiveEndAngle);
        var negativeLabel = PointOnCircle(center, radius * 1.23f, negativeStartAngle);

        DrawArcArrow(
            canvas,
            center,
            radius * 1.02f,
            0f,
            positiveEndAngle,
            PositiveStroke,
            2.2f);
        DrawArcArrow(
            canvas,
            center,
            radius * 1.02f,
            negativeStartAngle,
            0f,
            InverseStroke,
            2.2f);
        DrawSign(canvas, "+", positiveLabel, PositiveStroke, 18f);
        DrawSign(canvas, "-", negativeLabel, InverseStroke, 18f);
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

    private static void DrawBoundedLinearArea(SKCanvas canvas, SKRect bounds)
    {
        var width = bounds.Width * 0.58f;
        var height = bounds.Height * 0.58f;
        var rect = new SKRect(
            bounds.MidX - width * 0.5f,
            bounds.MidY - height * 0.5f,
            bounds.MidX + width * 0.5f,
            bounds.MidY + height * 0.5f);
        var top = new SKRect(rect.Left, rect.Top, rect.Right, rect.MidY);
        var bottom = new SKRect(rect.Left, rect.MidY, rect.Right, rect.Bottom);
        using var positive = FillPaint(PositiveColor, 205);
        using var inverse = FillPaint(InverseColor, 165);
        canvas.DrawRect(top, positive);
        canvas.DrawRect(bottom, inverse);
        DrawLine(canvas, new SKPoint(rect.Left, rect.MidY), new SKPoint(rect.Right, rect.MidY), BoundaryColor, 1.6f);
        for (var index = 1; index < 5; index++)
        {
            var x = rect.Left + rect.Width * index / 5f;
            DrawLine(canvas, new SKPoint(x, rect.MidY - 7), new SKPoint(x, rect.MidY + 7), TickColor, 1f);
        }
        DrawLine(canvas, new SKPoint(rect.Left, rect.Top), new SKPoint(rect.Right, rect.Top), BoundaryColor, 1f, 175);
        DrawLine(canvas, new SKPoint(rect.Right, rect.Top), new SKPoint(rect.Right, rect.Bottom), BoundaryColor, 1f, 175);
        DrawLine(canvas, new SKPoint(rect.Right, rect.Bottom), new SKPoint(rect.Left, rect.Bottom), BoundaryColor, 1f, 175);
        DrawLine(canvas, new SKPoint(rect.Left, rect.Bottom), new SKPoint(rect.Left, rect.Top), BoundaryColor, 1f, 175);
        DrawVectorArrow(
            canvas,
            new SKPoint(rect.Left + 8, rect.MidY - 12),
            new SKPoint(rect.Right - 8, rect.MidY - 12),
            PositiveStroke,
            1.45f);
        DrawVectorArrow(
            canvas,
            new SKPoint(rect.Right - 8, rect.MidY + 12),
            new SKPoint(rect.Left + 8, rect.MidY + 12),
            InverseStroke,
            1.45f);
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
        var panelWidth = Math.Min(430f, Math.Max(220f, size.Width - 48f));
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
        var paragraphs = text.Replace("\r\n", "\n").Split('\n');
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add(string.Empty);
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
        }

        return lines;
    }
}

internal readonly record struct BlocksHoverTip(SKPoint Anchor, string Title, string Body);

internal readonly record struct GlossarySpecimenDescriptor(string Id, string Title);

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
