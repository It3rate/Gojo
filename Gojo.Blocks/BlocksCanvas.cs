using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gojo.Core;
using GojoCAD;
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
        DrawToCanvas(canvas, size);
    }

    public static IReadOnlyList<BlocksHitArea> DrawToCanvas(
        SKCanvas canvas,
        SKSize size,
        BlocksDocument document,
        GojoDrawAnimationFrame animationFrame,
        BlocksHoverTip? hoverTip = null)
    {
        IReadOnlyList<BlocksHitArea> hitAreas = [];
        var operation = new BlocksDrawOperation(
            new Rect(0, 0, size.Width, size.Height),
            document,
            animationFrame,
            hoverTip,
            next => hitAreas = [.. next]);
        operation.DrawToCanvas(canvas, size);
        return hitAreas;
    }

    private void DrawToCanvas(SKCanvas canvas, SKSize size)
    {
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
        if (page.GlossarySheet == BlocksGlossarySheetKind.ResolutionTension)
        {
            DrawResolutionTensionGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.SiteFlowBoundary)
        {
            DrawSiteFlowBoundaryGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.ActionsRepeat)
        {
            DrawActionsRepeatGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.UnitsTicks)
        {
            DrawUnitsTicksGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.BasisFrames)
        {
            DrawBasisFramesGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.GradesDimensions)
        {
            DrawGradesDimensionsGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.FaceNormalSupport)
        {
            DrawFaceNormalSupportGlossary(canvas, page, area, hitAreas);
            return;
        }

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

        if (page.GlossarySheet == BlocksGlossarySheetKind.DerivedPathStructures)
        {
            DrawDerivedPathStructuresGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.BooleanOffsetReads)
        {
            DrawBooleanOffsetReadsGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.CrystallizationProvenance)
        {
            DrawCrystallizationProvenanceGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.DroneClipOffsetLadder)
        {
            DrawDroneClipOffsetLadderGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.RadialCrescentLadder)
        {
            DrawRadialCrescentLadderGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.DiamondCuspRounding)
        {
            DrawDiamondCuspRoundingGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.ConstraintsSettlement)
        {
            DrawConstraintsSettlementGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.TopologyParticipation)
        {
            DrawTopologyParticipationGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.RepetitionTimeline)
        {
            DrawRepetitionTimelineGlossary(canvas, page, area, hitAreas);
            return;
        }

        if (page.GlossarySheet == BlocksGlossarySheetKind.PcbDataAttachment)
        {
            DrawPcbDataAttachmentGlossary(canvas, page, area, hitAreas);
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

        var columns = page.Id == "glossary-radial-crescent-outline-experiment"
            ? Math.Min(3, Math.Max(1, items.Count))
            : area.Width >= 760 ? 4 : area.Width >= 540 ? 3 : 2;
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
            if (page.Id == "glossary-radial-crescent-outline-experiment")
            {
                paints.DrawSharedFramePointMarkers = false;
            }

            GojoRenderer.Draw(canvas, item.Structure, diagram, item.ProjectionSeed, item.ModelBounds, paints);
            AddGlossaryTooltipHit(hitAreas, Inset(diagram, 2), item.Id, item.Title, page);
        }
    }

    private void DrawResolutionTensionGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("resolution-tension-resolution-read", "Resolution read"),
            new GlossarySpecimenDescriptor("resolution-tension-unspecified-site", "Unspecified site"),
            new GlossarySpecimenDescriptor("resolution-tension-closure-tension", "Closure tension"),
            new GlossarySpecimenDescriptor("resolution-tension-failed-read", "Failed read"),
            new GlossarySpecimenDescriptor("resolution-tension-residue-witness", "Residue witness"),
            new GlossarySpecimenDescriptor("resolution-tension-settled-read", "Settled read")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawResolutionTensionSpecimen);
    }

    private void DrawSiteFlowBoundaryGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("site-flow-boundary-site", "Site"),
            new GlossarySpecimenDescriptor("site-flow-boundary-radial-flow", "Radial flow"),
            new GlossarySpecimenDescriptor("site-flow-boundary-linear-potential", "Linear potential"),
            new GlossarySpecimenDescriptor("site-flow-boundary-measured-boundary", "Measured boundary"),
            new GlossarySpecimenDescriptor("site-flow-boundary-open-exterior", "Open exterior"),
            new GlossarySpecimenDescriptor("site-flow-boundary-boundary-support", "Boundary support")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawSiteFlowBoundarySpecimen);
    }

    private void DrawActionsRepeatGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("actions-repeat-addition", "Addition"),
            new GlossarySpecimenDescriptor("actions-repeat-stretch", "Stretch (multiply)"),
            new GlossarySpecimenDescriptor("actions-repeat-repeat", "Action repeat"),
            new GlossarySpecimenDescriptor("actions-repeat-multiply", "Additive Repeat (multiply)"),
            new GlossarySpecimenDescriptor("actions-repeat-stretch-repeat", "Stretch Repeat (exponents)"),
            new GlossarySpecimenDescriptor("actions-repeat-compare", "Compare (divide)")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawActionsRepeatSpecimen);
    }

    private void DrawUnitsTicksGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("units-ticks-internal-partition", "Internal partition"),
            new GlossarySpecimenDescriptor("units-ticks-unit-comparison", "Unit comparison"),
            new GlossarySpecimenDescriptor("units-ticks-tick-reference", "Tick reference"),
            new GlossarySpecimenDescriptor("units-ticks-segment-ruler", "Segment ruler"),
            new GlossarySpecimenDescriptor("units-ticks-circular-ruler", "Circular ruler"),
            new GlossarySpecimenDescriptor("units-ticks-unit-shape", "Unit shape")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawUnitsTicksSpecimen);
    }

    private void DrawBasisFramesGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("basis-frames-xy-frame", "XY frame"),
            new GlossarySpecimenDescriptor("basis-frames-local-carrier", "Local carrier"),
            new GlossarySpecimenDescriptor("basis-frames-circular-basis", "Circular basis"),
            new GlossarySpecimenDescriptor("basis-frames-orthogonal-basis", "Orthogonal basis"),
            new GlossarySpecimenDescriptor("basis-frames-rotated-frame", "Rotated frame"),
            new GlossarySpecimenDescriptor("basis-frames-read-surface", "Read surface")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawBasisFramesSpecimen);
    }

    private void DrawGradesDimensionsGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("grades-dimensions-site", "0D site"),
            new GlossarySpecimenDescriptor("grades-dimensions-span", "1D span"),
            new GlossarySpecimenDescriptor("grades-dimensions-area", "2D area"),
            new GlossarySpecimenDescriptor("grades-dimensions-volume-hint", "3D volume hint"),
            new GlossarySpecimenDescriptor("grades-dimensions-segment-normal", "Segment normal"),
            new GlossarySpecimenDescriptor("grades-dimensions-point-pair-normal", "Point-pair normal")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawGradesDimensionsSpecimen);
    }

    private void DrawFaceNormalSupportGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("face-normal-point-radial", "Point normal"),
            new GlossarySpecimenDescriptor("face-normal-segment-sides", "Segment sides"),
            new GlossarySpecimenDescriptor("face-normal-arc-normal", "Arc normal"),
            new GlossarySpecimenDescriptor("face-normal-area-face", "Area face"),
            new GlossarySpecimenDescriptor("face-normal-inverse-face", "Inverse face"),
            new GlossarySpecimenDescriptor("face-normal-face-relation", "Face relation")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawFaceNormalSupportSpecimen);
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

    private void DrawDerivedPathStructuresGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("derived-path-open-offset", "Open offset"),
            new GlossarySpecimenDescriptor("derived-path-curved-offset", "Curved offset"),
            new GlossarySpecimenDescriptor("derived-path-inset-outset", "Inset / outset"),
            new GlossarySpecimenDescriptor("derived-path-branch-merge-fill", "Branch fill"),
            new GlossarySpecimenDescriptor("derived-path-outline-fill", "Outline fill"),
            new GlossarySpecimenDescriptor("derived-path-offset-residue", "Offset residue")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawDerivedPathSpecimen);
    }

    private void DrawBooleanOffsetReadsGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("boolean-offset-source-carriers", "Source carriers"),
            new GlossarySpecimenDescriptor("boolean-offset-offset-reads", "Offset reads"),
            new GlossarySpecimenDescriptor("boolean-offset-split-intersections", "Split intersections"),
            new GlossarySpecimenDescriptor("boolean-offset-union-hole", "Union + hole"),
            new GlossarySpecimenDescriptor("boolean-offset-intersection-read", "Intersection read"),
            new GlossarySpecimenDescriptor("boolean-offset-rounded-ladder", "Rounded ladder")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawBooleanOffsetSpecimen);
    }

    private void DrawCrystallizationProvenanceGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("crystallization-provenance-source-carriers", "Source carriers"),
            new GlossarySpecimenDescriptor("crystallization-provenance-read-path", "Read path"),
            new GlossarySpecimenDescriptor("crystallization-provenance-crystallized-element", "Crystallized element"),
            new GlossarySpecimenDescriptor("crystallization-provenance-provenance-links", "Provenance links"),
            new GlossarySpecimenDescriptor("crystallization-provenance-independent-read", "Independent read"),
            new GlossarySpecimenDescriptor("crystallization-provenance-residue-retained", "Residue retained")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawCrystallizationProvenanceSpecimen);
    }

    private void DrawDroneClipOffsetLadderGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("drone-clip-source", "Source"),
            new GlossarySpecimenDescriptor("drone-clip-path-read", "Path read"),
            new GlossarySpecimenDescriptor("drone-clip-offset-reads", "Offset reads"),
            new GlossarySpecimenDescriptor("drone-clip-intersections", "Intersections"),
            new GlossarySpecimenDescriptor("drone-clip-split-sections", "Split sections"),
            new GlossarySpecimenDescriptor("drone-clip-outside-round", "Outside round"),
            new GlossarySpecimenDescriptor("drone-clip-inside-round", "Inside round"),
            new GlossarySpecimenDescriptor("drone-clip-merged-loops", "Merged loops"),
            new GlossarySpecimenDescriptor("drone-clip-negative-spaces", "Negative spaces"),
            new GlossarySpecimenDescriptor("drone-clip-settled-read", "Settled read")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawDroneClipSpecimen);
    }

    private void DrawRadialCrescentLadderGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("radial-crescent-source-seeds", "Seed arms / body"),
            new GlossarySpecimenDescriptor("radial-crescent-endpoint-caps", "Motor caps"),
            new GlossarySpecimenDescriptor("radial-crescent-trimmed-carriers", "Trimmed arms"),
            new GlossarySpecimenDescriptor("radial-crescent-offset-boundaries", "Offset boundaries"),
            new GlossarySpecimenDescriptor("radial-crescent-fill-participants", "Fill participants"),
            new GlossarySpecimenDescriptor("radial-crescent-merged-areas", "Merged areas")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawRadialCrescentSpecimen);
    }

    private void DrawRadialCrescentRoundingGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("radial-crescent-rounding-interior-cusps", "Interior cusp selection"),
            new GlossarySpecimenDescriptor("radial-crescent-rounding-interior-rounded", "Rounded interior outlines"),
            new GlossarySpecimenDescriptor("radial-crescent-rounding-exterior-cusps", "Exterior cusp selection"),
            new GlossarySpecimenDescriptor("radial-crescent-rounding-final-rounded", "Final rounded outline")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawRadialCrescentRoundingSpecimen, columnsOverride: 2);
    }

    private void DrawDiamondCuspRoundingGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("diamond-cusp-rounding-source", "Source shape"),
            new GlossarySpecimenDescriptor("diamond-cusp-rounding-outer-selection", "Select outer cusps"),
            new GlossarySpecimenDescriptor("diamond-cusp-rounding-outer-rounded", "Round outer"),
            new GlossarySpecimenDescriptor("diamond-cusp-rounding-inner-selection", "Select inner cusps"),
            new GlossarySpecimenDescriptor("diamond-cusp-rounding-inner-rounded", "Round inner"),
            new GlossarySpecimenDescriptor("diamond-cusp-rounding-result", "Final cutout")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawDiamondCuspRoundingSpecimen, columnsOverride: 3);
    }

    private void DrawConstraintsSettlementGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("constraints-settlement-equal-length", "Equal length"),
            new GlossarySpecimenDescriptor("constraints-settlement-tangency", "Tangency"),
            new GlossarySpecimenDescriptor("constraints-settlement-radius", "Radius"),
            new GlossarySpecimenDescriptor("constraints-settlement-closure", "Closure"),
            new GlossarySpecimenDescriptor("constraints-settlement-settled", "Settled read"),
            new GlossarySpecimenDescriptor("constraints-settlement-residue", "Residue")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawConstraintsSettlementSpecimen);
    }

    private void DrawTopologyParticipationGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("topology-participation-crossing", "Crossing"),
            new GlossarySpecimenDescriptor("topology-participation-over-under", "Over / under"),
            new GlossarySpecimenDescriptor("topology-participation-containment", "Containment"),
            new GlossarySpecimenDescriptor("topology-participation-adjacency", "Adjacency"),
            new GlossarySpecimenDescriptor("topology-participation-near-far", "Near / far"),
            new GlossarySpecimenDescriptor("topology-participation-bonded-distinct", "Bonded distinct")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawTopologyParticipationSpecimen);
    }

    private void DrawRepetitionTimelineGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("repetition-timeline-controller", "Controller"),
            new GlossarySpecimenDescriptor("repetition-timeline-transition", "Transition"),
            new GlossarySpecimenDescriptor("repetition-timeline-feedback", "Feedback"),
            new GlossarySpecimenDescriptor("repetition-timeline-timeline", "Timeline"),
            new GlossarySpecimenDescriptor("repetition-timeline-accumulated-residue", "Accumulated residue"),
            new GlossarySpecimenDescriptor("repetition-timeline-stable-pattern", "Stable pattern")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawRepetitionTimelineSpecimen);
    }

    private void DrawPcbDataAttachmentGlossary(SKCanvas canvas, BlocksPage page, SKRect area, List<BlocksHitArea> hitAreas)
    {
        var specimens = new[]
        {
            new GlossarySpecimenDescriptor("pcb-data-schematic-intent", "Schematic intent"),
            new GlossarySpecimenDescriptor("pcb-data-layout-carriers", "Layout carriers"),
            new GlossarySpecimenDescriptor("pcb-data-data-attachment", "Data attachment"),
            new GlossarySpecimenDescriptor("pcb-data-render-read", "Render read"),
            new GlossarySpecimenDescriptor("pcb-data-rule-check", "Rule check"),
            new GlossarySpecimenDescriptor("pcb-data-physical-read", "Physical board")
        };
        DrawSpecimenGrid(canvas, page, area, hitAreas, specimens, DrawPcbDataAttachmentSpecimen, columnsOverride: 3);
    }

    private void DrawSpecimenGrid(
        SKCanvas canvas,
        BlocksPage page,
        SKRect area,
        List<BlocksHitArea> hitAreas,
        IReadOnlyList<GlossarySpecimenDescriptor> specimens,
        Action<SKCanvas, int, SKRect> drawSpecimen,
        int? columnsOverride = null)
    {
        var columns = columnsOverride ?? (area.Width >= 760 ? 3 : 2);
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
        var isCurveSheet = sheet == BlocksGlossarySheetKind.Curves;
        var labelWidth = isCurveSheet ? Math.Clamp(area.Width * 0.052f, 38f, 48f) : Math.Clamp(area.Width * 0.07f, 48f, 60f);
        var headerHeight = isCurveSheet ? 36f : 46f;
        var gap = isCurveSheet ? 7f : 10f;
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
            BlocksGlossarySheetKind.BooleanOffsetReads => "boolean-offset",
            BlocksGlossarySheetKind.DroneClipOffsetLadder => "drone-clip",
            BlocksGlossarySheetKind.RadialCrescentLadder => "radial-crescent",
            BlocksGlossarySheetKind.DiamondCuspRounding => "diamond-cusp-rounding",
            BlocksGlossarySheetKind.PcbDataAttachment => "pcb-data",
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
        var inner = Inset(cell, sheet == BlocksGlossarySheetKind.Curves ? 4f : 10f);
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

    private static void DrawResolutionTensionSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawResolutionRead(canvas, bounds);
                break;
            case 1:
                DrawUnspecifiedSite(canvas, bounds);
                break;
            case 2:
                DrawClosureTensionPath(canvas, bounds);
                break;
            case 3:
                DrawFailedRead(canvas, bounds);
                break;
            case 4:
                DrawResidueWitness(canvas, bounds);
                break;
            case 5:
                DrawSettledRead(canvas, bounds);
                break;
        }
    }

    private void DrawSiteFlowBoundarySpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawPoint(canvas, Center(bounds), SiteColor, 8.5f);
                break;
            case 1:
                DrawLadderFlow(canvas, GlossaryLadderRow.RadialPositive, bounds);
                break;
            case 2:
                DrawLinearFlow(canvas, bounds);
                break;
            case 3:
                DrawLadderBoundary(canvas, GlossaryLadderRow.RadialPositive, bounds);
                break;
            case 4:
                DrawLinearBoundary(canvas, bounds, ticks: false);
                break;
            case 5:
                DrawLadderBoundedSupport(canvas, GlossaryLadderRow.RadialPositive, bounds);
                break;
        }
    }

    private static void DrawActionsRepeatSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawAdditionAction(canvas, bounds);
                break;
            case 1:
                DrawStretchAction(canvas, bounds);
                break;
            case 2:
                DrawRepeatAction(canvas, bounds);
                break;
            case 3:
                DrawMultiplyAction(canvas, bounds);
                break;
            case 4:
                DrawStretchRepeatAction(canvas, bounds);
                break;
            case 5:
                DrawCompareAction(canvas, bounds);
                break;
        }
    }

    private static void DrawUnitsTicksSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawLinearBoundary(canvas, bounds, ticks: true);
                break;
            case 1:
                DrawUnitComparison(canvas, bounds);
                break;
            case 2:
                DrawTickReference(canvas, bounds);
                break;
            case 3:
                DrawSegmentRuler(canvas, bounds);
                break;
            case 4:
                DrawCircularRuler(canvas, bounds);
                break;
            case 5:
                DrawUnitShape(canvas, bounds);
                break;
        }
    }

    private void DrawBasisFramesSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawXyBasisPath(canvas, bounds);
                break;
            case 1:
                DrawLocalCarrierBasis(canvas, bounds);
                break;
            case 2:
                DrawCircularBasisPath(canvas, bounds);
                break;
            case 3:
                DrawOrthogonalBasisPath(canvas, bounds);
                break;
            case 4:
                DrawRotatedFrameBasis(canvas, bounds);
                break;
            case 5:
                DrawReadSurfaceBasis(canvas, bounds);
                break;
        }
    }

    private void DrawGradesDimensionsSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawPoint(canvas, Center(bounds), SiteColor, 8.5f);
                break;
            case 1:
                DrawLinearBoundary(canvas, bounds, ticks: false);
                break;
            case 2:
                DrawLadderArea(canvas, GlossaryLadderRow.RadialPositive, bounds);
                break;
            case 3:
                DrawVolumeHint(canvas, bounds);
                break;
            case 4:
                DrawSegmentNormalGrade(canvas, bounds);
                break;
            case 5:
                DrawPointPairNormalGrade(canvas, bounds);
                break;
        }
    }

    private void DrawFaceNormalSupportSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawLadderFlow(canvas, GlossaryLadderRow.RadialPositive, bounds);
                break;
            case 1:
                DrawLinearSupportBand(canvas, bounds);
                break;
            case 2:
                DrawArcNormalSupport(canvas, bounds);
                break;
            case 3:
                DrawLadderAreaFace(canvas, GlossaryLadderRow.RadialPositive, bounds);
                break;
            case 4:
                DrawLadderAreaFace(canvas, GlossaryLadderRow.RadialInverse, bounds);
                break;
            case 5:
                DrawFaceRelation(canvas, bounds);
                break;
        }
    }

    private static void DrawCrystallizationProvenanceSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawCompositeSourceCarriers(canvas, bounds);
                break;
            case 1:
                DrawCompositePathRead(canvas, bounds);
                break;
            case 2:
                DrawCrystallizedElement(canvas, bounds);
                break;
            case 3:
                DrawProvenanceLinks(canvas, bounds);
                break;
            case 4:
                DrawIndependentRead(canvas, bounds);
                break;
            case 5:
                DrawResidueWitness(canvas, bounds);
                break;
        }
    }

    private static void DrawConstraintsSettlementSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawEqualLengthConstraint(canvas, bounds);
                break;
            case 1:
                DrawTangencyConstraint(canvas, bounds);
                break;
            case 2:
                DrawRadiusConstraint(canvas, bounds);
                break;
            case 3:
                DrawClosureTensionPath(canvas, bounds);
                break;
            case 4:
                DrawSettledRead(canvas, bounds);
                break;
            case 5:
                DrawActionResidue(canvas, bounds);
                break;
        }
    }

    private static void DrawTopologyParticipationSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawTopologyCrossingNoJoin(canvas, bounds);
                break;
            case 1:
                DrawOverUnder(canvas, bounds);
                break;
            case 2:
                DrawContainment(canvas, bounds);
                break;
            case 3:
                DrawAdjacency(canvas, bounds);
                break;
            case 4:
                DrawNearFar(canvas, bounds);
                break;
            case 5:
                DrawBondedPointPath(canvas, bounds);
                break;
        }
    }

    private static void DrawRepetitionTimelineSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawControllerSelection(canvas, bounds);
                break;
            case 1:
                DrawRepeatedTransition(canvas, bounds);
                break;
            case 2:
                DrawFeedbackLoop(canvas, bounds);
                break;
            case 3:
                DrawTimeline(canvas, bounds);
                break;
            case 4:
                DrawAccumulatedResidue(canvas, bounds);
                break;
            case 5:
                DrawStablePattern(canvas, bounds);
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

    private void DrawDerivedPathSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawOpenOffsetRead(canvas, bounds);
                break;
            case 1:
                DrawCurvedOffsetRead(canvas, bounds);
                break;
            case 2:
                DrawInsetOutsetLayers(canvas, bounds);
                break;
            case 3:
                DrawBranchMergeFillRead(canvas, bounds);
                break;
            case 4:
                DrawOutlineFillRead(canvas, bounds);
                break;
            case 5:
                DrawOffsetResidueRead(canvas, bounds);
                break;
        }
    }

    private void DrawBooleanOffsetSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawBooleanSourceCarriers(canvas, bounds);
                break;
            case 1:
                DrawBooleanOffsetReads(canvas, bounds);
                break;
            case 2:
                DrawBooleanSplitIntersections(canvas, bounds);
                break;
            case 3:
                DrawBooleanUnionHole(canvas, bounds);
                break;
            case 4:
                DrawBooleanIntersectionRead(canvas, bounds);
                break;
            case 5:
                DrawBooleanRoundedLadder(canvas, bounds);
                break;
        }
    }

    private void DrawDroneClipSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawDroneClipSource(canvas, bounds);
                break;
            case 1:
                DrawDroneClipPathRead(canvas, bounds);
                break;
            case 2:
                DrawDroneClipOffsetReads(canvas, bounds);
                break;
            case 3:
                DrawDroneClipIntersections(canvas, bounds);
                break;
            case 4:
                DrawDroneClipSplitSections(canvas, bounds);
                break;
            case 5:
                DrawDroneClipOutsideRound(canvas, bounds);
                break;
            case 6:
                DrawDroneClipInsideRound(canvas, bounds);
                break;
            case 7:
                DrawDroneClipMergedLoops(canvas, bounds);
                break;
            case 8:
                DrawDroneClipNegativeSpaces(canvas, bounds);
                break;
            case 9:
                DrawDroneClipSettledRead(canvas, bounds);
                break;
        }
    }

    private void DrawRadialCrescentSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawRadialCrescentSourceSeeds(canvas, bounds);
                break;
            case 1:
                DrawRadialCrescentEndpointCaps(canvas, bounds);
                break;
            case 2:
                DrawRadialCrescentTrimmedCarriers(canvas, bounds);
                break;
            case 3:
                DrawRadialCrescentOffsetBoundaries(canvas, bounds);
                break;
            case 4:
                DrawRadialCrescentFillParticipants(canvas, bounds);
                break;
            case 5:
                DrawRadialCrescentMergedAreas(canvas, bounds);
                break;
            case 6:
                DrawRadialCrescentOutlineRead(canvas, bounds);
                break;
            case 7:
                DrawRadialCrescentInteriorRound(canvas, bounds);
                break;
            case 8:
                DrawRadialCrescentExteriorRound(canvas, bounds);
                break;
            case 9:
                DrawRadialCrescentSettledArea(canvas, bounds);
                break;
        }
    }

    private void DrawRadialCrescentRoundingSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawRadialCrescentInteriorCuspSelection(canvas, bounds);
                break;
            case 1:
                DrawRadialCrescentRoundedInteriorOutlines(canvas, bounds);
                break;
            case 2:
                DrawRadialCrescentExteriorCuspSelection(canvas, bounds);
                break;
            case 3:
                DrawRadialCrescentFinalRoundedOutline(canvas, bounds);
                break;
        }
    }

    private void DrawDiamondCuspRoundingSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        switch (index)
        {
            case 0:
                DrawDiamondCuspSource(canvas, bounds);
                break;
            case 1:
                DrawDiamondOuterSelection(canvas, bounds);
                break;
            case 2:
                DrawDiamondOuterRounded(canvas, bounds);
                break;
            case 3:
                DrawDiamondInnerSelection(canvas, bounds);
                break;
            case 4:
                DrawDiamondInnerRounded(canvas, bounds);
                break;
            case 5:
                DrawDiamondCuspResult(canvas, bounds);
                break;
        }
    }

    private void DrawPcbDataAttachmentSpecimen(SKCanvas canvas, int index, SKRect bounds)
    {
        var stage = PcbDataGlossaryGeometry.CreateStage();
        switch (index)
        {
            case 0:
                DrawPcbSchematicIntent(canvas, bounds, stage);
                break;
            case 1:
                DrawPcbLayoutCarriers(canvas, bounds, stage);
                break;
            case 2:
                DrawPcbDataAttachments(canvas, bounds, stage);
                break;
            case 3:
                DrawPcbRenderRead(canvas, bounds, stage);
                break;
            case 4:
                DrawPcbRuleCheck(canvas, bounds, stage);
                break;
            case 5:
                DrawPcbPhysicalRead(canvas, bounds, stage);
                break;
        }
    }

    private static void DrawPcbSchematicIntent(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        _ = stage.Schematic;
        var j1 = new SKRect(bounds.Left + bounds.Width * 0.07f, bounds.Top + bounds.Height * 0.27f, bounds.Left + bounds.Width * 0.25f, bounds.Bottom - bounds.Height * 0.20f);
        var u1 = new SKRect(bounds.Left + bounds.Width * 0.42f, bounds.Top + bounds.Height * 0.21f, bounds.Left + bounds.Width * 0.62f, bounds.Bottom - bounds.Height * 0.18f);
        var led = new SKRect(bounds.Left + bounds.Width * 0.78f, bounds.Top + bounds.Height * 0.30f, bounds.Right - bounds.Width * 0.07f, bounds.Bottom - bounds.Height * 0.27f);

        DrawPcbSchematicBlock(canvas, j1, "J1", PcbConnectorColor);
        DrawPcbSchematicBlock(canvas, u1, "U1", PcbChipColor);
        DrawPcbSchematicBlock(canvas, led, "D1", PcbLedColor);

        var jPins = new[]
        {
            new SKPoint(j1.Right, j1.Top + j1.Height * 0.22f),
            new SKPoint(j1.Right, j1.MidY),
            new SKPoint(j1.Right, j1.Bottom - j1.Height * 0.22f)
        };
        var uLeft = new[]
        {
            new SKPoint(u1.Left, u1.Top + u1.Height * 0.22f),
            new SKPoint(u1.Left, u1.MidY),
            new SKPoint(u1.Left, u1.Bottom - u1.Height * 0.22f)
        };
        var uRight = new[]
        {
            new SKPoint(u1.Right, u1.Top + u1.Height * 0.25f),
            new SKPoint(u1.Right, u1.Bottom - u1.Height * 0.25f)
        };
        var ledPins = new[]
        {
            new SKPoint(led.Left, led.Top + led.Height * 0.30f),
            new SKPoint(led.Left, led.Bottom - led.Height * 0.30f)
        };

        DrawPcbNetWire(canvas, jPins[0], uLeft[0], PcbPowerColor, "VCC");
        DrawPcbNetWire(canvas, jPins[1], uLeft[1], PcbSignalColor, "SIG");
        DrawPcbNetWire(canvas, jPins[2], uLeft[2], PcbGroundColor, "GND");
        DrawPcbNetWire(canvas, uRight[0], ledPins[0], PcbLedColor, "LED");
        DrawPcbNetWire(canvas, uRight[1], ledPins[1], PcbGroundColor, "GND");
        DrawPcbSmallCaption(canvas, "nets before geometry", new SKPoint(bounds.MidX, bounds.Bottom - 9), themeColor: PcbTextColor);
    }

    private static void DrawPcbLayoutCarriers(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        DrawPcbBoard(canvas, bounds, physical: false, alpha: 32);
        DrawPcbFootprints(canvas, bounds, physical: false);
        DrawPcbPads(canvas, bounds, stage, filled: false, alpha: 210);
        foreach (var trace in stage.Traces)
        {
            DrawPcbTrace(canvas, bounds, trace, PcbCarrierColor, 1.45f, 178, centerSites: true);
        }

        DrawPcbRouteTicks(canvas, bounds, stage.Traces[1], 4, PcbMarkerColor);
        DrawPcbSmallCaption(canvas, "frame carriers + shared pad sites", new SKPoint(bounds.MidX, bounds.Bottom - 9), PcbTextColor);
    }

    private static void DrawPcbDataAttachments(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        DrawPcbBoard(canvas, bounds, physical: false, alpha: 24);
        DrawPcbFootprints(canvas, bounds, physical: false);
        DrawPcbPads(canvas, bounds, stage, filled: false, alpha: 165);
        foreach (var trace in stage.Traces)
        {
            DrawPcbTrace(canvas, bounds, trace, PcbCarrierColor, 1.15f, 96, centerSites: false);
        }

        DrawPcbTraceBadge(canvas, bounds, stage.Traces[0], new PcbDataGlossaryGeometry.ModelPoint(-58, 58));
        DrawPcbTraceBadge(canvas, bounds, stage.Traces[1], new PcbDataGlossaryGeometry.ModelPoint(-58, 18));
        DrawPcbTraceBadge(canvas, bounds, stage.Traces[2], new PcbDataGlossaryGeometry.ModelPoint(-58, -60));
        DrawPcbTraceBadge(canvas, bounds, stage.Traces[3], new PcbDataGlossaryGeometry.ModelPoint(62, 44));
        DrawPcbTraceBadge(canvas, bounds, stage.Traces[4], new PcbDataGlossaryGeometry.ModelPoint(62, -54));
    }

    private static void DrawPcbRenderRead(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        DrawPcbBoard(canvas, bounds, physical: true, alpha: 210);
        DrawPcbBottomLayerHint(canvas, bounds, stage);
        foreach (var trace in stage.Traces)
        {
            DrawPcbCopperTrace(canvas, bounds, trace, trace.Layer == "BOT" ? (byte)172 : (byte)230);
        }

        DrawPcbPads(canvas, bounds, stage, filled: true, alpha: 242);
        DrawPcbFootprints(canvas, bounds, physical: true);
        DrawPcbLayerLegend(canvas, bounds);
    }

    private static void DrawPcbRuleCheck(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        DrawPcbBoard(canvas, bounds, physical: false, alpha: 28);
        foreach (var trace in stage.Traces)
        {
            DrawPcbCopperTrace(canvas, bounds, trace, 92);
        }

        DrawPcbPads(canvas, bounds, stage, filled: true, alpha: 165);
        DrawPcbCheckBadge(canvas, new SKPoint(bounds.Left + 18, bounds.Top + 18), "continuity", true);
        DrawPcbCheckBadge(canvas, new SKPoint(bounds.Left + 18, bounds.Top + 42), "width basis", true);
        DrawPcbCheckBadge(canvas, new SKPoint(bounds.Left + 18, bounds.Top + 66), "clearance", false);

        var tension = PcbPoint(bounds, -37, -16);
        using var glow = FillPaint(PcbWarningColor, 46);
        canvas.DrawCircle(tension, 18, glow);
        using var ring = StrokePaint(PcbWarningColor, 2.2f, 232);
        canvas.DrawCircle(tension, 11, ring);
        DrawVectorArrow(canvas, new SKPoint(tension.X + 22, tension.Y + 18), tension, PcbWarningColor, 1.35f, 220);
        DrawPcbSmallCaption(canvas, "unresolved proximity stays inspectable", new SKPoint(bounds.MidX + 8, bounds.Bottom - 9), PcbTextColor);
    }

    private static void DrawPcbPhysicalRead(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        DrawPcbBoard(canvas, bounds, physical: true, alpha: 242);
        DrawPcbMountHoles(canvas, bounds);
        DrawPcbBottomLayerHint(canvas, bounds, stage);
        foreach (var trace in stage.Traces)
        {
            DrawPcbCopperTrace(canvas, bounds, trace, trace.Layer == "BOT" ? (byte)150 : (byte)224);
        }

        DrawPcbPads(canvas, bounds, stage, filled: true, alpha: 248);
        DrawPcbFootprints(canvas, bounds, physical: true);
        DrawPcbSilkscreen(canvas, bounds);
    }

    private static void DrawPcbSchematicBlock(SKCanvas canvas, SKRect rect, string label, SKColor color)
    {
        using var fill = FillPaint(color, 42);
        using var stroke = StrokePaint(color, 1.45f, 218);
        canvas.DrawRoundRect(rect, 5, 5, fill);
        canvas.DrawRoundRect(rect, 5, 5, stroke);
        DrawPcbLabelPill(canvas, Center(rect), label, 9.5f, PcbTextColor);
    }

    private static void DrawPcbNetWire(SKCanvas canvas, SKPoint start, SKPoint end, SKColor color, string label)
    {
        var midX = (start.X + end.X) * 0.5f;
        using var paint = StrokePaint(color, 2.0f, 214);
        using var path = new SKPath();
        path.MoveTo(start);
        path.LineTo(midX, start.Y);
        path.LineTo(midX, end.Y);
        path.LineTo(end);
        canvas.DrawPath(path, paint);
        DrawPoint(canvas, start, color, 3.1f);
        DrawPoint(canvas, end, color, 3.1f);
        using var text = TextPaint(7.6f, color, bold: true);
        var labelWidth = text.MeasureText(label);
        canvas.DrawText(label, midX - labelWidth * 0.5f, Math.Min(start.Y, end.Y) - 3f, text);
    }

    private static void DrawPcbBoard(SKCanvas canvas, SKRect bounds, bool physical, byte alpha)
    {
        var board = PcbRect(bounds, -110, 68, 110, -68);
        using var fill = FillPaint(physical ? PcbBoardColor : PcbBoardGhostColor, alpha);
        using var stroke = StrokePaint(physical ? PcbBoardEdgeColor : PcbCarrierColor, physical ? 1.8f : 1.15f, physical ? (byte)226 : (byte)130);
        canvas.DrawRoundRect(board, 10, 10, fill);
        canvas.DrawRoundRect(board, 10, 10, stroke);

        if (!physical)
        {
            using var dot = FillPaint(PcbCarrierColor, 38);
            for (var x = -80; x <= 80; x += 40)
            for (var y = -40; y <= 40; y += 40)
            {
                canvas.DrawCircle(PcbPoint(bounds, x, y), 1.3f, dot);
            }
        }
    }

    private static void DrawPcbFootprints(SKCanvas canvas, SKRect bounds, bool physical)
    {
        var chip = PcbRect(bounds, -22, 32, 42, -32);
        if (physical)
        {
            using var body = FillPaint(PcbChipColor, 238);
            using var bodyStroke = StrokePaint(new SKColor(17, 25, 28), 1.2f, 235);
            canvas.DrawRoundRect(chip, 4, 4, body);
            canvas.DrawRoundRect(chip, 4, 4, bodyStroke);
            using var text = TextPaint(8.6f, new SKColor(226, 232, 226), bold: true);
            DrawCenteredText(canvas, "U1", chip, text);
        }
        else
        {
            using var stroke = StrokePaint(PcbFootprintColor, 1.15f, 152);
            canvas.DrawRoundRect(chip, 4, 4, stroke);
        }

        var connector = PcbRect(bounds, -102, 54, -82, -54);
        using (var stroke = StrokePaint(physical ? PcbSilkColor : PcbFootprintColor, 1.1f, physical ? (byte)190 : (byte)125))
        {
            canvas.DrawRoundRect(connector, 3, 3, stroke);
        }

        if (physical)
        {
            using var ledStroke = StrokePaint(PcbSilkColor, 1.15f, 205);
            using var ledFill = FillPaint(PcbLedColor, 48);
            var ledRect = PcbRect(bounds, 62, 37, 94, -37);
            canvas.DrawRoundRect(ledRect, 8, 8, ledFill);
            canvas.DrawRoundRect(ledRect, 8, 8, ledStroke);
        }
    }

    private static void DrawPcbPads(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage, bool filled, byte alpha)
    {
        foreach (var pad in stage.Pads)
        {
            var center = PcbPoint(bounds, pad.Center);
            var radius = PcbRadius(bounds, pad.Circle);
            var color = PcbNetColor(pad.Net);
            if (filled)
            {
                using var copper = FillPaint(PcbCopperPadColor, alpha);
                using var ring = StrokePaint(color, 1.25f, 228);
                using var drill = FillPaint(PcbDrillColor, 230);
                canvas.DrawCircle(center, radius, copper);
                canvas.DrawCircle(center, radius, ring);
                canvas.DrawCircle(center, Math.Max(2.5f, radius * 0.42f), drill);
            }
            else
            {
                using var ring = StrokePaint(color, 1.25f, alpha);
                canvas.DrawCircle(center, radius, ring);
                DrawPoint(canvas, center, PcbCarrierColor, 2.4f);
            }
        }
    }

    private static void DrawPcbTrace(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.TraceRead trace, SKColor color, float width, byte alpha, bool centerSites)
    {
        using var path = PcbPathFromSections(bounds, trace.Sections);
        using var paint = StrokePaint(color, width, alpha);
        canvas.DrawPath(path, paint);
        if (!centerSites)
        {
            return;
        }

        foreach (var point in PcbTraceSites(bounds, trace))
        {
            DrawPoint(canvas, point, color, 2.4f);
        }
    }

    private static void DrawPcbCopperTrace(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.TraceRead trace, byte alpha)
    {
        using var path = PcbPathFromSections(bounds, trace.Sections);
        var width = Math.Max(2.6f, trace.WidthTicks * PcbScale(bounds) * 0.85f);
        var color = PcbNetColor(trace.Net);
        if (trace.Layer == "BOT")
        {
            using var underlay = StrokePaint(PcbBottomShadowColor, width + 2.4f, (byte)Math.Min(120, (int)alpha));
            using var dash = SKPathEffect.CreateDash([5f, 3.2f], 0);
            underlay.PathEffect = dash;
            canvas.DrawPath(path, underlay);
        }

        using var paint = StrokePaint(color, width, alpha);
        canvas.DrawPath(path, paint);
    }

    private static void DrawPcbBottomLayerHint(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.StageRead stage)
    {
        foreach (var trace in stage.Traces.Where(trace => trace.Layer == "BOT"))
        {
            using var path = PcbPathFromSections(bounds, trace.Sections);
            using var shadow = StrokePaint(PcbBottomShadowColor, Math.Max(2.6f, trace.WidthTicks * PcbScale(bounds) * 1.05f), 86);
            using var dash = SKPathEffect.CreateDash([6f, 4f], 1.4f);
            shadow.PathEffect = dash;
            canvas.DrawPath(path, shadow);
        }
    }

    private static void DrawPcbTraceBadge(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.TraceRead trace, PcbDataGlossaryGeometry.ModelPoint modelPoint)
    {
        var anchor = PcbPoint(bounds, modelPoint);
        var target = PcbTraceSites(bounds, trace).Skip(1).FirstOrDefault();
        DrawDashedLine(canvas, anchor, target, PcbMarkerColor, 0.95f);
        var lines = new[] { $"net {trace.Net}", trace.Layer, $"w{trace.WidthTicks} c{trace.CostTicks}" };
        DrawPcbBadge(canvas, anchor, lines, PcbNetColor(trace.Net));
    }

    private static void DrawPcbBadge(SKCanvas canvas, SKPoint center, IReadOnlyList<string> lines, SKColor color)
    {
        using var text = TextPaint(7.3f, PcbTextColor, bold: true);
        var width = Math.Max(38f, lines.Max(line => text.MeasureText(line)) + 10f);
        var height = lines.Count * 8.2f + 7f;
        var rect = new SKRect(center.X - width * 0.5f, center.Y - height * 0.5f, center.X + width * 0.5f, center.Y + height * 0.5f);
        using var fill = FillPaint(new SKColor(248, 250, 247), 238);
        using var stroke = StrokePaint(color, 1.1f, 220);
        canvas.DrawRoundRect(rect, 4.5f, 4.5f, fill);
        canvas.DrawRoundRect(rect, 4.5f, 4.5f, stroke);
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            canvas.DrawText(line, rect.Left + 5f, rect.Top + 9.8f + index * 8.2f, text);
        }
    }

    private static void DrawPcbCheckBadge(SKCanvas canvas, SKPoint origin, string label, bool pass)
    {
        using var text = TextPaint(7.4f, PcbTextColor, bold: true);
        var width = text.MeasureText(label) + 24;
        var rect = new SKRect(origin.X, origin.Y, origin.X + width, origin.Y + 16);
        using var fill = FillPaint(new SKColor(249, 250, 247), 232);
        using var stroke = StrokePaint(pass ? PcbPassColor : PcbWarningColor, 1.1f, 230);
        canvas.DrawRoundRect(rect, 4, 4, fill);
        canvas.DrawRoundRect(rect, 4, 4, stroke);
        using var mark = TextPaint(8.3f, pass ? PcbPassColor : PcbWarningColor, bold: true);
        canvas.DrawText(pass ? "ok" : "!", rect.Left + 4, rect.Top + 10.7f, mark);
        canvas.DrawText(label, rect.Left + 18, rect.Top + 10.6f, text);
    }

    private static void DrawPcbLayerLegend(SKCanvas canvas, SKRect bounds)
    {
        DrawPcbSmallCaption(canvas, "TOP copper", new SKPoint(bounds.Right - 38, bounds.Top + 17), PcbPowerColor);
        DrawPcbSmallCaption(canvas, "BOT copper", new SKPoint(bounds.Right - 38, bounds.Top + 31), PcbBottomShadowColor);
    }

    private static void DrawPcbRouteTicks(SKCanvas canvas, SKRect bounds, PcbDataGlossaryGeometry.TraceRead trace, int count, SKColor color)
    {
        using var path = PcbPathFromSections(bounds, trace.Sections);
        using var measure = new SKPathMeasure(path, false);
        using var tick = StrokePaint(color, 1.1f, 135);
        for (var index = 1; index <= count; index++)
        {
            if (!measure.GetPositionAndTangent(measure.Length * index / (count + 1), out var position, out var tangent))
            {
                continue;
            }

            var normal = Normalize(new SKPoint(-tangent.Y, tangent.X));
            canvas.DrawLine(
                position.X - normal.X * 5,
                position.Y - normal.Y * 5,
                position.X + normal.X * 5,
                position.Y + normal.Y * 5,
                tick);
        }
    }

    private static void DrawPcbMountHoles(SKCanvas canvas, SKRect bounds)
    {
        using var ring = StrokePaint(PcbSilkColor, 0.9f, 160);
        using var drill = FillPaint(PcbDrillColor, 240);
        foreach (var (x, y) in new[] { (-98L, 58L), (98L, 58L), (-98L, -58L), (98L, -58L) })
        {
            var p = PcbPoint(bounds, x, y);
            canvas.DrawCircle(p, 4.8f, drill);
            canvas.DrawCircle(p, 8.2f, ring);
        }
    }

    private static void DrawPcbSilkscreen(SKCanvas canvas, SKRect bounds)
    {
        using var text = TextPaint(7.4f, PcbSilkColor, bold: true);
        canvas.DrawText("GOJO-01", PcbPoint(bounds, -70, 59).X, PcbPoint(bounds, -70, 59).Y, text);
        canvas.DrawText("J1", PcbPoint(bounds, -105, -61).X, PcbPoint(bounds, -105, -61).Y, text);
        canvas.DrawText("D1", PcbPoint(bounds, 61, 44).X, PcbPoint(bounds, 61, 44).Y, text);
    }

    private static void DrawPcbSmallCaption(SKCanvas canvas, string text, SKPoint center, SKColor themeColor)
    {
        DrawPcbLabelPill(canvas, center, text, 7.2f, themeColor);
    }

    private static void DrawPcbLabelPill(SKCanvas canvas, SKPoint center, string text, float size, SKColor textColor)
    {
        using var paint = TextPaint(size, textColor, bold: true);
        var width = paint.MeasureText(text);
        var rect = new SKRect(
            center.X - width * 0.5f - 4.2f,
            center.Y - size * 0.72f - 2.2f,
            center.X + width * 0.5f + 4.2f,
            center.Y + size * 0.28f + 2.2f);
        using var fill = FillPaint(new SKColor(248, 250, 247), 218);
        using var stroke = StrokePaint(textColor, 0.55f, 92);
        canvas.DrawRoundRect(rect, 3.5f, 3.5f, fill);
        canvas.DrawRoundRect(rect, 3.5f, 3.5f, stroke);
        canvas.DrawText(text, center.X - width * 0.5f, center.Y, paint);
    }

    private static IEnumerable<SKPoint> PcbTraceSites(SKRect bounds, PcbDataGlossaryGeometry.TraceRead trace)
    {
        foreach (var section in trace.Sections)
        {
            var graph = GraphicReadGraph.From(section);
            var curve = graph.ReadsOf<SharedFrameQuadraticCurveRead>().FirstOrDefault();
            if (curve is not null)
            {
                yield return PcbPoint(bounds, curve.Start);
                yield return PcbPoint(bounds, curve.Control);
                yield return PcbPoint(bounds, curve.End);
                continue;
            }

            var segment = graph.ReadsOf<SharedFrameSegmentRead>().FirstOrDefault();
            if (segment is not null)
            {
                yield return PcbPoint(bounds, segment.Start);
                yield return PcbPoint(bounds, segment.End);
            }
        }
    }

    private static SKPath PcbPathFromSections(SKRect bounds, IReadOnlyList<IElement> sections)
    {
        var path = new SKPath();
        var hasStarted = false;
        foreach (var section in sections)
        {
            var graph = GraphicReadGraph.From(section);
            var curve = graph.ReadsOf<SharedFrameQuadraticCurveRead>().FirstOrDefault();
            if (curve is not null)
            {
                MoveOrLineTo(path, PcbPoint(bounds, curve.Start), ref hasStarted);
                path.QuadTo(PcbPoint(bounds, curve.Control), PcbPoint(bounds, curve.End));
                continue;
            }

            var segment = graph.ReadsOf<SharedFrameSegmentRead>().FirstOrDefault();
            if (segment is not null)
            {
                MoveOrLineTo(path, PcbPoint(bounds, segment.Start), ref hasStarted);
                path.LineTo(PcbPoint(bounds, segment.End));
            }
        }

        return path;
    }

    private static SKPoint PcbPoint(SKRect bounds, Relation point)
    {
        var framePoint = GraphicReadGraph.From(point)
            .ReadsOf<SharedFramePointRead>()
            .FirstOrDefault();
        return framePoint is null ? PcbPoint(bounds, 0, 0) : PcbPoint(bounds, framePoint);
    }

    private static SKPoint PcbPoint(SKRect bounds, SharedFramePointRead point) =>
        PcbPoint(bounds, PcbModelPoint(point));

    private static SKPoint PcbPoint(SKRect bounds, PcbDataGlossaryGeometry.ModelPoint point) =>
        PcbPoint(bounds, point.X, point.Y);

    private static SKPoint PcbPoint(SKRect bounds, long x, long y)
    {
        var scale = PcbScale(bounds);
        return new SKPoint(bounds.MidX + x * scale, bounds.MidY - y * scale);
    }

    private static SKRect PcbRect(SKRect bounds, long left, long top, long right, long bottom)
    {
        var a = PcbPoint(bounds, left, top);
        var b = PcbPoint(bounds, right, bottom);
        return new SKRect(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }

    private static float PcbScale(SKRect bounds) =>
        Math.Min(bounds.Width / 240f, bounds.Height / 162f) * 0.94f;

    private static float PcbRadius(SKRect bounds, Relation circle)
    {
        var read = GraphicReadGraph.From(circle)
            .ReadsOf<SharedFrameCircleRead>()
            .FirstOrDefault();
        if (read is null)
        {
            return PcbScale(bounds) * 6f;
        }

        var center = PcbModelPoint(read.Center);
        var radius = PcbModelPoint(read.RadiusPoint);
        return MathF.Sqrt((radius.X - center.X) * (radius.X - center.X) + (radius.Y - center.Y) * (radius.Y - center.Y)) * PcbScale(bounds);
    }

    private static PcbDataGlossaryGeometry.ModelPoint PcbModelPoint(SharedFramePointRead point)
    {
        var x = point.Coordinates.Count > 0 ? point.Coordinates[0].Ticks : 0;
        var y = point.Coordinates.Count > 1 ? point.Coordinates[1].Ticks : 0;
        return new PcbDataGlossaryGeometry.ModelPoint(x, y);
    }

    private static SKColor PcbNetColor(string net) =>
        net switch
        {
            "VCC" => PcbPowerColor,
            "SIG" => PcbSignalColor,
            "GND" => PcbGroundColor,
            "LED" => PcbLedColor,
            _ => PcbCarrierColor
        };

    private static SKColor PcbBoardColor => new(30, 112, 84);

    private static SKColor PcbBoardGhostColor => new(38, 116, 96);

    private static SKColor PcbBoardEdgeColor => new(13, 70, 54);

    private static SKColor PcbCopperPadColor => new(229, 184, 89);

    private static SKColor PcbDrillColor => new(239, 244, 240);

    private static SKColor PcbPowerColor => new(222, 91, 54);

    private static SKColor PcbSignalColor => new(25, 154, 168);

    private static SKColor PcbGroundColor => new(91, 120, 214);

    private static SKColor PcbLedColor => new(215, 141, 42);

    private static SKColor PcbBottomShadowColor => new(98, 88, 180);

    private static SKColor PcbCarrierColor => new(99, 108, 109);

    private static SKColor PcbFootprintColor => new(109, 132, 126);

    private static SKColor PcbChipColor => new(38, 46, 47);

    private static SKColor PcbConnectorColor => new(98, 121, 128);

    private static SKColor PcbMarkerColor => new(126, 134, 142);

    private static SKColor PcbWarningColor => new(214, 61, 50);

    private static SKColor PcbPassColor => new(27, 142, 92);

    private static SKColor PcbSilkColor => new(226, 238, 224);

    private static SKColor PcbTextColor => new(39, 49, 49);

    private static void DrawResolutionRead(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var start = new SKPoint(bounds.Left + 16, y);
        var end = new SKPoint(bounds.Right - 16, y);
        DrawCarrierLine(canvas, start, end);
        for (var index = 0; index <= 12; index++)
        {
            var x = start.X + (end.X - start.X) * index / 12f;
            var height = index % 3 == 0 ? 13f : 7f;
            DrawLine(canvas, new SKPoint(x, y - height), new SKPoint(x, y + height), TickColor, 1f);
        }

        using var coarse = StrokePaint(PositiveStroke, 3.8f, 210);
        canvas.DrawLine(start.X, y - 20, end.X, y - 20, coarse);
        for (var index = 1; index < 4; index++)
        {
            var x = start.X + (end.X - start.X) * index / 4f;
            DrawLine(canvas, new SKPoint(x, y - 29), new SKPoint(x, y - 11), PositiveStroke, 1.5f, 210);
        }
    }

    private static void DrawUnspecifiedSite(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        DrawPoint(canvas, center, SiteColor, 7.5f);
        using var ring = StrokePaint(EvidenceColor, 1.5f, 110);
        canvas.DrawCircle(center, Math.Min(bounds.Width, bounds.Height) * 0.25f, ring);
        DrawDashedLine(canvas, new SKPoint(bounds.Left + 18, bounds.MidY), new SKPoint(bounds.Right - 18, bounds.MidY), EvidenceColor, 1.1f);
        DrawTensionSpot(canvas, center, Math.Min(bounds.Width, bounds.Height) * 0.34f, EvidenceColor);
    }

    private static void DrawFailedRead(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 16, bounds.Bottom - 18);
        var b = new SKPoint(bounds.MidX - 10, bounds.Top + 20);
        var c = new SKPoint(bounds.Right - 18, bounds.Bottom - 18);
        DrawCarrierLine(canvas, a, b);
        DrawCarrierLine(canvas, b, c);
        var requested = new SKPoint(bounds.MidX + 8, bounds.MidY + 16);
        DrawDashedLine(canvas, a, requested, EvidenceColor, 1.2f);
        DrawDashedLine(canvas, requested, c, EvidenceColor, 1.2f);
        DrawTensionSpot(canvas, requested, 16f);
        DrawPoint(canvas, requested, InverseStroke, 4.5f);
    }

    private static void DrawResidueWitness(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var start = new SKPoint(bounds.Left + 14, y + 8);
        var split = new SKPoint(bounds.MidX + 8, y - 10);
        var end = new SKPoint(bounds.Right - 18, y - 3);
        DrawVectorArrow(canvas, start, split, PositiveStroke, 2.2f);
        DrawCarrierLine(canvas, split, end);
        DrawDashedLine(canvas, split, new SKPoint(bounds.Right - 14, bounds.Bottom - 16), InverseStroke, 1.4f);
        DrawTensionSpot(canvas, new SKPoint(bounds.Right - 32, bounds.Bottom - 18), 13f);
        DrawPoint(canvas, split, EndpointColor, 4.8f);
    }

    private static void DrawSettledRead(SKCanvas canvas, SKRect bounds)
    {
        using var path = new SKPath();
        path.MoveTo(bounds.Left + 16, bounds.Bottom - 20);
        path.CubicTo(bounds.MidX - 10, bounds.Top + 8, bounds.MidX + 26, bounds.Bottom - 2, bounds.Right - 16, bounds.Top + 24);
        DrawBridgePath(canvas, path, PositiveStroke, 2.6f, glow: true);
        DrawPathPartitions(canvas, path, 6);
        DrawPoint(canvas, new SKPoint(bounds.Left + 16, bounds.Bottom - 20), SiteColor, 4.8f);
        DrawPoint(canvas, new SKPoint(bounds.Right - 16, bounds.Top + 24), EndpointColor, 4.8f);
    }

    private static void DrawAdditionAction(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var a = new SKPoint(bounds.Left + 14, y);
        var b = new SKPoint(bounds.MidX - 7, y);
        var c = new SKPoint(bounds.Right - 16, y);
        DrawVectorArrow(canvas, a, b, PositiveStroke, 2.2f);
        DrawVectorArrow(canvas, b, c, PositiveStroke, 2.2f);
        DrawPoint(canvas, a, SiteColor, 4.7f);
        DrawPoint(canvas, b, EndpointColor, 4.7f);
        DrawPoint(canvas, c, EndpointColor, 4.7f);
    }

    private static void DrawStretchAction(SKCanvas canvas, SKRect bounds)
    {
        var startX = bounds.Left + bounds.Width * 0.16f;
        var sourceLength = bounds.Width * 0.22f;
        var stretchedLength = sourceLength * 3f;
        var sourceY = bounds.MidY - bounds.Height * 0.12f;
        var stretchedY = bounds.MidY + bounds.Height * 0.14f;
        var sourceStart = new SKPoint(startX, sourceY);
        var sourceEnd = new SKPoint(startX + sourceLength, sourceY);
        var stretchedStart = new SKPoint(startX, stretchedY);
        var stretchedEnd = new SKPoint(startX + stretchedLength, stretchedY);

        DrawLine(canvas, sourceStart, sourceEnd, BoundaryColor, 2f, 170);
        DrawPoint(canvas, sourceStart, SiteColor, 4.2f);
        DrawPoint(canvas, sourceEnd, EndpointColor, 4.2f);

        DrawLine(canvas, stretchedStart, stretchedEnd, PositiveStroke, 3f, 215);
        for (var index = 0; index <= 3; index++)
        {
            var x = startX + sourceLength * index;
            DrawLine(canvas, new SKPoint(x, stretchedY - 8), new SKPoint(x, stretchedY + 8), TickColor, 1.1f, 150);
            DrawPoint(canvas, new SKPoint(x, stretchedY), index == 0 ? SiteColor : EndpointColor, index == 0 ? 3.8f : 3.4f);
        }

        DrawDashedLine(canvas, sourceStart, stretchedStart, EvidenceColor, 0.9f);
        DrawDashedLine(canvas, sourceEnd, new SKPoint(startX + sourceLength, stretchedY), EvidenceColor, 0.9f);
        DrawVectorArrow(
            canvas,
            new SKPoint(startX + sourceLength, sourceY - 11),
            new SKPoint(startX + stretchedLength, sourceY - 11),
            PositiveStroke,
            1.35f,
            150);
    }

    private static void DrawRepeatAction(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY + 8;
        var start = bounds.Left + 18;
        var step = (bounds.Width - 36) / 5f;
        for (var index = 0; index < 5; index++)
        {
            var a = new SKPoint(start + step * index, y);
            var b = new SKPoint(start + step * (index + 0.78f), y);
            DrawVectorArrow(canvas, a, b, index % 2 == 0 ? PositiveStroke : BoundaryColor, 1.7f, 220);
        }
        for (var index = 0; index <= 5; index++)
        {
            DrawPoint(canvas, new SKPoint(start + step * index, y), SiteColor, 3.8f);
        }
    }

    private static void DrawMultiplyAction(SKCanvas canvas, SKRect bounds)
    {
        var unit = Math.Min(bounds.Width, bounds.Height) * 0.17f;
        for (var row = 0; row < 3; row++)
        for (var column = 0; column < 4; column++)
        {
            var x = bounds.MidX + (column - 1.5f) * unit * 1.18f;
            var y = bounds.MidY + (row - 1f) * unit * 0.95f;
            using var fill = FillPaint(PositiveColor, (byte)(row == 0 ? 95 : 52));
            using var stroke = StrokePaint(PositiveStroke, 1f, 150);
            var rect = new SKRect(x - unit * 0.38f, y - unit * 0.28f, x + unit * 0.38f, y + unit * 0.28f);
            canvas.DrawRoundRect(rect, 3, 3, fill);
            canvas.DrawRoundRect(rect, 3, 3, stroke);
        }
    }

    private static void DrawStretchRepeatAction(SKCanvas canvas, SKRect bounds)
    {
        var left = bounds.Left + bounds.Width * 0.18f;
        var top = bounds.Top + bounds.Height * 0.24f;
        var rowGap = bounds.Height * 0.105f;
        var baseLength = bounds.Width * 0.14f;
        const float growthRate = 1.38f;

        using var axis = StrokePaint(TickColor, 1f, 120);
        canvas.DrawLine(left, top - 9, left, top + rowGap * 5f + 9, axis);

        for (var index = 0; index < 6; index++)
        {
            var y = top + rowGap * index;
            var length = baseLength * MathF.Pow(growthRate, index);
            var start = new SKPoint(left, y);
            var end = new SKPoint(left + length, y);
            var alpha = (byte)(112 + index * 20);

            DrawVectorArrow(canvas, start, end, PositiveStroke, 1.35f + index * 0.15f, alpha);
            DrawPoint(canvas, start, SiteColor, 2.7f);
            DrawPoint(canvas, end, EndpointColor, 2.9f);

            if (index > 0)
            {
                var previousLength = baseLength * MathF.Pow(growthRate, index - 1);
                using var relation = StrokePaint(EvidenceColor, 0.8f, 82);
                canvas.DrawLine(left + previousLength, y - rowGap, end.X, end.Y, relation);
            }
        }
    }

    private static void DrawCompareAction(SKCanvas canvas, SKRect bounds)
    {
        var top = bounds.MidY - bounds.Height * 0.18f;
        var bottom = bounds.MidY + bounds.Height * 0.18f;
        DrawLine(canvas, new SKPoint(bounds.Left + 18, top), new SKPoint(bounds.Right - 18, top), PositiveStroke, 3f, 210);
        var unit = (bounds.Width - 36) / 5f;
        for (var index = 0; index < 5; index++)
        {
            var x0 = bounds.Left + 18 + unit * index;
            DrawLine(canvas, new SKPoint(x0, bottom), new SKPoint(x0 + unit * 0.78f, bottom), BoundaryColor, 2f, 180);
            DrawLine(canvas, new SKPoint(x0, bottom - 6), new SKPoint(x0, bottom + 6), TickColor, 1f);
        }
        DrawLine(canvas, new SKPoint(bounds.Left + 18 + unit * 5, bottom - 6), new SKPoint(bounds.Left + 18 + unit * 5, bottom + 6), TickColor, 1f);
    }

    private static void DrawActionResidue(SKCanvas canvas, SKRect bounds)
    {
        DrawRepeatAction(canvas, bounds);
        var residue = new SKPoint(bounds.Right - bounds.Width * 0.22f, bounds.MidY - 18);
        DrawDashedLine(canvas, new SKPoint(bounds.Right - bounds.Width * 0.33f, bounds.MidY + 8), residue, InverseStroke, 1.3f);
        DrawTensionSpot(canvas, residue, 12f);
        DrawPoint(canvas, residue, InverseStroke, 4.2f);
    }

    private static void DrawUnitComparison(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        DrawLine(canvas, new SKPoint(bounds.Left + 16, y - 16), new SKPoint(bounds.Right - 16, y - 16), PositiveStroke, 3f, 210);
        var unit = (bounds.Width - 32) / 4.5f;
        for (var index = 0; index < 4; index++)
        {
            var x = bounds.Left + 16 + unit * index;
            DrawLine(canvas, new SKPoint(x, y + 12), new SKPoint(x + unit * 0.82f, y + 12), BoundaryColor, 2f, 175);
            DrawPoint(canvas, new SKPoint(x, y + 12), SiteColor, 3.2f);
        }
        DrawTensionSpot(canvas, new SKPoint(bounds.Right - 23, y + 12), 8f, EvidenceColor);
    }

    private static void DrawTickReference(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var left = bounds.Left + 18;
        var right = bounds.Right - 18;
        DrawLine(canvas, new SKPoint(left, y), new SKPoint(right, y), BoundaryColor, 1.8f);
        for (var index = 0; index <= 8; index++)
        {
            var x = left + (right - left) * index / 8f;
            DrawLine(canvas, new SKPoint(x, y - 7), new SKPoint(x, y + 7), TickColor, 1f);
        }
        var tick = new SKPoint(left + (right - left) * 5f / 8f, y);
        DrawPoint(canvas, tick, EndpointColor, 5f);
        DrawDashedLine(canvas, tick, new SKPoint(tick.X, bounds.Top + 12), EvidenceColor, 1.2f);
    }

    private static void DrawSegmentRuler(SKCanvas canvas, SKRect bounds)
    {
        DrawLinearBoundary(canvas, new SKRect(bounds.Left + 2, bounds.Top + 8, bounds.Right - 2, bounds.Bottom - 8), ticks: true);
        DrawVectorArrow(canvas, new SKPoint(bounds.Left + 22, bounds.Bottom - 12), new SKPoint(bounds.Right - 22, bounds.Bottom - 12), PositiveStroke, 1.6f);
    }

    private static void DrawCircularRuler(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.32f;
        DrawProtractor(canvas, center, radius, 0);
        DrawPoint(canvas, center, SiteColor, 5.8f);
        DrawArcArrow(canvas, center, radius + 4, -8, 82, PositiveStroke, 2f);
    }

    private static void DrawUnitShape(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var spacing = Math.Min(bounds.Width, bounds.Height) * 0.30f;
        var yTop = center.Y - spacing * 0.45f;
        var yBottom = center.Y + spacing * 0.50f;

        var segmentLeft = new SKPoint(center.X - spacing * 1.35f, yTop);
        var segmentRight = new SKPoint(center.X - spacing * 0.25f, yTop);
        DrawLine(canvas, segmentLeft, segmentRight, PositiveStroke, 2.3f, 210);
        for (var index = 0; index <= 3; index++)
        {
            var x = Lerp(segmentLeft, segmentRight, index / 3f).X;
            DrawLine(canvas, new SKPoint(x, yTop - 6), new SKPoint(x, yTop + 6), TickColor, 1f, 150);
        }

        var circleCenter = new SKPoint(center.X + spacing * 0.78f, yTop);
        var radius = spacing * 0.40f;
        DrawCircleBoundary(canvas, circleCenter, radius, 1.6f, PositiveStroke);
        DrawPoint(canvas, circleCenter, SiteColor, 3.6f);
        for (var index = 0; index < 6; index++)
        {
            var tick = PointOnCircle(circleCenter, radius, index * 60f);
            var outer = PointOnCircle(circleCenter, radius + 4, index * 60f);
            DrawLine(canvas, tick, outer, TickColor, 0.9f, 150);
        }

        using var fill = FillPaint(PositiveColor, 58);
        using var stroke = StrokePaint(PositiveStroke, 1.5f, 205);
        var tile = new SKRect(
            center.X - spacing * 1.14f,
            yBottom - spacing * 0.32f,
            center.X - spacing * 0.30f,
            yBottom + spacing * 0.32f);
        canvas.DrawRoundRect(tile, 3, 3, fill);
        canvas.DrawRoundRect(tile, 3, 3, stroke);
        DrawLine(canvas, new SKPoint(tile.MidX, tile.Top), new SKPoint(tile.MidX, tile.Bottom), PositiveStroke, 0.9f, 130);

        using var composite = new SKPath();
        composite.MoveTo(center.X + spacing * 0.20f, yBottom + spacing * 0.22f);
        composite.CubicTo(
            center.X + spacing * 0.38f,
            yBottom - spacing * 0.48f,
            center.X + spacing * 1.16f,
            yBottom - spacing * 0.45f,
            center.X + spacing * 1.30f,
            yBottom + spacing * 0.20f);
        DrawBridgePath(canvas, composite, PositiveStroke, 1.9f, glow: true);
        DrawPoint(canvas, new SKPoint(center.X + spacing * 0.20f, yBottom + spacing * 0.22f), SiteColor, 3.2f);
        DrawPoint(canvas, new SKPoint(center.X + spacing * 1.30f, yBottom + spacing * 0.20f), SiteColor, 3.2f);
    }

    private static void DrawLocalCarrierBasis(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 16, bounds.Bottom - 20);
        var b = new SKPoint(bounds.Right - 18, bounds.Top + 24);
        DrawVectorArrow(canvas, a, b, BoundaryColor, 2f, 190);
        var mid = new SKPoint((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
        var normal = Normalize(new SKPoint(-(b.Y - a.Y), b.X - a.X));
        DrawVectorArrow(canvas, mid, new SKPoint(mid.X + normal.X * 28, mid.Y + normal.Y * 28), PositiveStroke, 1.8f);
        DrawPoint(canvas, mid, EndpointColor, 4.5f);
    }

    private static void DrawRotatedFrameBasis(SKCanvas canvas, SKRect bounds)
    {
        var origin = Center(bounds);
        var xEnd = PointOnCircle(origin, bounds.Width * 0.33f, -28);
        var yEnd = PointOnCircle(origin, bounds.Height * 0.28f, -118);
        DrawVectorArrow(canvas, origin, xEnd, PositiveStroke, 1.8f);
        DrawVectorArrow(canvas, origin, yEnd, EvidenceColor, 1.8f);
        DrawDashedLine(canvas, new SKPoint(bounds.Left + 14, bounds.MidY), new SKPoint(bounds.Right - 14, bounds.MidY), TickColor, 1f);
        DrawDashedLine(canvas, new SKPoint(bounds.MidX, bounds.Top + 10), new SKPoint(bounds.MidX, bounds.Bottom - 10), TickColor, 1f);
        DrawPoint(canvas, origin, SiteColor, 5.5f);
    }

    private static void DrawReadSurfaceBasis(SKCanvas canvas, SKRect bounds)
    {
        var surface = new SKRect(bounds.Left + 18, bounds.Top + 20, bounds.Right - 18, bounds.Bottom - 18);
        using var fill = FillPaint(new SKColor(218, 231, 232), 120);
        using var stroke = StrokePaint(EvidenceColor, 1.2f, 145);
        canvas.DrawRoundRect(surface, 6, 6, fill);
        canvas.DrawRoundRect(surface, 6, 6, stroke);
        DrawVectorArrow(canvas, new SKPoint(surface.Left + 12, surface.Bottom - 16), new SKPoint(surface.Right - 12, surface.Top + 16), PositiveStroke, 2.2f);
        DrawPoint(canvas, new SKPoint(surface.Left + 12, surface.Bottom - 16), SiteColor, 4.4f);
        DrawPoint(canvas, new SKPoint(surface.Right - 12, surface.Top + 16), EndpointColor, 4.4f);
    }

    private static void DrawVolumeHint(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var w = bounds.Width * 0.30f;
        var h = bounds.Height * 0.24f;
        var depth = new SKPoint(w * 0.32f, -h * 0.45f);
        var front = new SKRect(center.X - w, center.Y - h * 0.4f, center.X + w, center.Y + h * 0.8f);
        using var fill = FillPaint(PositiveColor, 70);
        using var stroke = StrokePaint(PositiveStroke, 1.4f, 185);
        canvas.DrawRect(front, fill);
        canvas.DrawRect(front, stroke);
        canvas.DrawLine(front.Left, front.Top, front.Left + depth.X, front.Top + depth.Y, stroke);
        canvas.DrawLine(front.Right, front.Top, front.Right + depth.X, front.Top + depth.Y, stroke);
        canvas.DrawLine(front.Right, front.Bottom, front.Right + depth.X, front.Bottom + depth.Y, stroke);
        canvas.DrawLine(front.Left + depth.X, front.Top + depth.Y, front.Right + depth.X, front.Top + depth.Y, stroke);
        canvas.DrawLine(front.Right + depth.X, front.Top + depth.Y, front.Right + depth.X, front.Bottom + depth.Y, stroke);
    }

    private static void DrawSegmentNormalGrade(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var a = new SKPoint(bounds.Left + 18, y);
        var b = new SKPoint(bounds.Right - 18, y);
        DrawCarrierLine(canvas, a, b);
        var mid = Center(bounds);
        DrawVectorArrow(canvas, mid, new SKPoint(mid.X, bounds.Top + 13), PositiveStroke, 2f);
        DrawVectorArrow(canvas, mid, new SKPoint(mid.X, bounds.Bottom - 13), InverseStroke, 2f);
        DrawPoint(canvas, mid, EndpointColor, 4.8f);
    }

    private static void DrawPointPairNormalGrade(SKCanvas canvas, SKRect bounds)
    {
        var left = new SKPoint(bounds.MidX - bounds.Width * 0.14f, bounds.MidY);
        var right = new SKPoint(bounds.MidX + bounds.Width * 0.14f, bounds.MidY);
        DrawLine(canvas, left, right, BoundaryColor, 1.8f, 160);
        using var cylinder = StrokePaint(PositiveStroke, 1.5f, 150);
        var rect = new SKRect(left.X, bounds.Top + 20, right.X, bounds.Bottom - 20);
        canvas.DrawOval(rect, cylinder);
        DrawPoint(canvas, left, SiteColor, 5.2f);
        DrawPoint(canvas, right, SiteColor, 5.2f);
        DrawDashedLine(canvas, new SKPoint(bounds.MidX, bounds.Top + 12), new SKPoint(bounds.MidX, bounds.Bottom - 12), EvidenceColor, 1.1f);
    }

    private static void DrawArcNormalSupport(SKCanvas canvas, SKRect bounds)
    {
        var center = new SKPoint(bounds.MidX, bounds.Bottom - bounds.Height * 0.10f);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.48f;
        using var arc = StrokePaint(BoundaryColor, 1.8f, 190);
        var oval = new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius);
        canvas.DrawArc(oval, 205, 130, false, arc);
        for (var angle = 220f; angle <= 320f; angle += 25f)
        {
            var p = PointOnCircle(center, radius, angle);
            var outP = PointOnCircle(center, radius + 18, angle);
            DrawVectorArrow(canvas, p, outP, PositiveStroke, 1.4f, 210);
        }
    }

    private static void DrawFaceRelation(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        var left = bounds.Left + 24;
        var right = bounds.Right - 24;
        using var positive = FillPaint(PositiveColor, 112);
        using var inverse = FillPaint(InverseColor, 112);
        canvas.DrawRect(new SKRect(left, y - 30, right, y), positive);
        canvas.DrawRect(new SKRect(left, y, right, y + 30), inverse);
        DrawCarrierLine(canvas, new SKPoint(left, y), new SKPoint(right, y));
        DrawVectorArrow(canvas, new SKPoint(bounds.MidX, y), new SKPoint(bounds.MidX, y - 25), PositiveStroke, 1.8f);
        DrawVectorArrow(canvas, new SKPoint(bounds.MidX, y), new SKPoint(bounds.MidX, y + 25), InverseStroke, 1.8f);
    }

    private static void DrawCompositeSourceCarriers(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 15, bounds.Bottom - 18);
        var b = new SKPoint(bounds.MidX - 8, bounds.MidY + 8);
        var c = new SKPoint(bounds.MidX + 20, bounds.MidY - 12);
        var d = new SKPoint(bounds.Right - 16, bounds.Top + 26);
        DrawCarrierLine(canvas, a, b);
        using var arc = new SKPath();
        arc.MoveTo(b);
        arc.QuadTo(bounds.MidX + 4, bounds.Top + 7, c.X, c.Y);
        DrawBridgePath(canvas, arc, BoundaryColor, 1.7f, glow: false);
        DrawCarrierLine(canvas, c, d);
        DrawPoint(canvas, b, EndpointColor, 4.2f);
        DrawPoint(canvas, c, EndpointColor, 4.2f);
    }

    private static void DrawCrystallizedElement(SKCanvas canvas, SKRect bounds)
    {
        using var path = new SKPath();
        path.MoveTo(bounds.Left + 16, bounds.Bottom - 18);
        path.LineTo(bounds.MidX - 8, bounds.MidY + 8);
        path.QuadTo(bounds.MidX + 4, bounds.Top + 7, bounds.MidX + 20, bounds.MidY - 12);
        path.LineTo(bounds.Right - 16, bounds.Top + 26);
        DrawBridgePath(canvas, path, PositiveStroke, 4f, glow: true);
        DrawPathPartitions(canvas, path, 5);
    }

    private static void DrawProvenanceLinks(SKCanvas canvas, SKRect bounds)
    {
        DrawCompositeSourceCarriers(canvas, bounds);
        using var path = new SKPath();
        path.MoveTo(bounds.Left + 16, bounds.Bottom - 18);
        path.CubicTo(bounds.MidX - 10, bounds.Top + 8, bounds.MidX + 22, bounds.Bottom - 8, bounds.Right - 16, bounds.Top + 26);
        DrawBridgePath(canvas, path, PositiveStroke, 2.4f, glow: true);
        DrawDashedLine(canvas, new SKPoint(bounds.MidX - 8, bounds.MidY + 8), new SKPoint(bounds.MidX, bounds.MidY - 10), EvidenceColor, 1.1f);
        DrawDashedLine(canvas, new SKPoint(bounds.MidX + 20, bounds.MidY - 12), new SKPoint(bounds.MidX, bounds.MidY - 10), EvidenceColor, 1.1f);
    }

    private static void DrawIndependentRead(SKCanvas canvas, SKRect bounds)
    {
        DrawCrystallizedElement(canvas, bounds);
        DrawArcArrow(canvas, Center(bounds), Math.Min(bounds.Width, bounds.Height) * 0.30f, 210, 318, EvidenceColor, 1.4f);
    }

    private static void DrawEqualLengthConstraint(SKCanvas canvas, SKRect bounds)
    {
        var y1 = bounds.MidY - 16;
        var y2 = bounds.MidY + 16;
        DrawLine(canvas, new SKPoint(bounds.Left + 18, y1), new SKPoint(bounds.Right - 28, y1), PositiveStroke, 2.3f);
        DrawLine(canvas, new SKPoint(bounds.Left + 28, y2), new SKPoint(bounds.Right - 18, y2), BoundaryColor, 2.3f);
        DrawDashedLine(canvas, new SKPoint(bounds.Right - 28, y1), new SKPoint(bounds.Right - 18, y2), EvidenceColor, 1.1f);
        DrawPoint(canvas, new SKPoint(bounds.Right - 28, y1), EndpointColor, 4.2f);
        DrawPoint(canvas, new SKPoint(bounds.Right - 18, y2), EndpointColor, 4.2f);
    }

    private static void DrawTangencyConstraint(SKCanvas canvas, SKRect bounds)
    {
        var join = Center(bounds);
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 16, join.Y + 18), join);
        using var arc = new SKPath();
        arc.MoveTo(join);
        arc.QuadTo(bounds.MidX + 22, bounds.Top + 8, bounds.Right - 18, bounds.MidY - 10);
        DrawBridgePath(canvas, arc, PositiveStroke, 2.4f, glow: true);
        DrawVectorArrow(canvas, join, new SKPoint(join.X + 30, join.Y - 30), EvidenceColor, 1.5f);
        DrawPoint(canvas, join, EndpointColor, 5f);
    }

    private static void DrawRadiusConstraint(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.29f;
        DrawCircleBoundary(canvas, center, radius, 1.7f, PositiveStroke);
        var edge = PointOnCircle(center, radius, -18);
        DrawRadiusEvidence(canvas, center, edge);
        DrawPoint(canvas, center, SiteColor, 5.2f);
        DrawPoint(canvas, edge, EndpointColor, 4.5f);
    }

    private static void DrawTopologyCrossingNoJoin(SKCanvas canvas, SKRect bounds)
    {
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 18, bounds.Bottom - 18), new SKPoint(bounds.Right - 18, bounds.Top + 18));
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 18, bounds.Top + 18), new SKPoint(bounds.Right - 18, bounds.Bottom - 18));
        using var gap = FillPaint(new SKColor(250, 250, 246), 235);
        canvas.DrawCircle(Center(bounds), 8f, gap);
        DrawPoint(canvas, Center(bounds), TickColor, 3.5f);
    }

    private static void DrawOverUnder(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        DrawCarrierLine(canvas, new SKPoint(bounds.Left + 16, y), new SKPoint(bounds.Right - 16, y));
        using var bridge = new SKPath();
        bridge.MoveTo(bounds.MidX - 34, bounds.Bottom - 18);
        bridge.CubicTo(bounds.MidX - 18, bounds.Top + 4, bounds.MidX + 18, bounds.Top + 4, bounds.MidX + 34, bounds.Bottom - 18);
        DrawBridgePath(canvas, bridge, PositiveStroke, 2.3f, glow: false);
    }

    private static void DrawContainment(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        using var fill = FillPaint(PositiveColor, 58);
        using var stroke = StrokePaint(PositiveStroke, 1.5f, 190);
        canvas.DrawCircle(center, Math.Min(bounds.Width, bounds.Height) * 0.34f, fill);
        canvas.DrawCircle(center, Math.Min(bounds.Width, bounds.Height) * 0.34f, stroke);
        DrawPoint(canvas, new SKPoint(center.X + 10, center.Y - 4), EndpointColor, 5f);
    }

    private static void DrawAdjacency(SKCanvas canvas, SKRect bounds)
    {
        var mid = bounds.MidX;
        var top = bounds.MidY - 22;
        var bottom = bounds.MidY + 22;
        using var a = FillPaint(PositiveColor, 94);
        using var b = FillPaint(InverseColor, 80);
        canvas.DrawRect(new SKRect(bounds.Left + 18, top, mid, bottom), a);
        canvas.DrawRect(new SKRect(mid, top, bounds.Right - 18, bottom), b);
        DrawLine(canvas, new SKPoint(mid, top), new SKPoint(mid, bottom), BoundaryColor, 2f);
    }

    private static void DrawNearFar(SKCanvas canvas, SKRect bounds)
    {
        var nearA = new SKPoint(bounds.Left + 32, bounds.MidY - 8);
        var nearB = new SKPoint(bounds.Left + 58, bounds.MidY + 8);
        var far = new SKPoint(bounds.Right - 26, bounds.MidY - 12);
        DrawPoint(canvas, nearA, SiteColor, 5.4f);
        DrawPoint(canvas, nearB, EndpointColor, 5.4f);
        DrawPoint(canvas, far, BoundaryColor, 5.4f);
        DrawDashedLine(canvas, nearA, nearB, PositiveStroke, 1.3f);
        DrawDashedLine(canvas, nearB, far, TickColor, 1.1f);
    }

    private static void DrawControllerSelection(SKCanvas canvas, SKRect bounds)
    {
        var source = new SKRect(bounds.Left + 22, bounds.MidY - 12, bounds.Right - 22, bounds.MidY + 12);
        using var fill = FillPaint(PositiveColor, 58);
        using var stroke = StrokePaint(PositiveStroke, 1.5f, 190);
        canvas.DrawRoundRect(source, 5, 5, fill);
        canvas.DrawRoundRect(source, 5, 5, stroke);
        DrawPoint(canvas, new SKPoint(source.Left + source.Width * 0.35f, source.MidY), EndpointColor, 5f);
        DrawPoint(canvas, new SKPoint(source.Left + source.Width * 0.62f, source.MidY), EndpointColor, 5f);
    }

    private static void DrawRepeatedTransition(SKCanvas canvas, SKRect bounds)
    {
        var start = new SKPoint(bounds.Left + 18, bounds.Bottom - 18);
        var step = (bounds.Width - 36) / 4f;
        for (var index = 0; index < 4; index++)
        {
            var a = new SKPoint(start.X + step * index, start.Y - index * 10);
            var b = new SKPoint(start.X + step * (index + 1), start.Y - (index + 1) * 10);
            DrawVectorArrow(canvas, a, b, PositiveStroke, 1.7f, (byte)(230 - index * 24));
        }
    }

    private static void DrawFeedbackLoop(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.28f;
        DrawArcArrow(canvas, center, radius, -40, 245, PositiveStroke, 2.2f);
        DrawVectorArrow(canvas, PointOnCircle(center, radius, 245), new SKPoint(center.X - radius - 14, center.Y + radius * 0.15f), EvidenceColor, 1.4f);
        DrawTensionSpot(canvas, center, 10f, EvidenceColor);
    }

    private static void DrawTimeline(SKCanvas canvas, SKRect bounds)
    {
        var y = bounds.MidY;
        DrawLine(canvas, new SKPoint(bounds.Left + 16, y), new SKPoint(bounds.Right - 16, y), BoundaryColor, 1.5f);
        for (var index = 0; index < 6; index++)
        {
            var x = bounds.Left + 20 + (bounds.Width - 40) * index / 5f;
            DrawPoint(canvas, new SKPoint(x, y), index < 4 ? PositiveStroke : EvidenceColor, 4.2f);
            if (index < 5)
            {
                DrawVectorArrow(canvas, new SKPoint(x + 6, y), new SKPoint(x + (bounds.Width - 40) / 5f - 6, y), TickColor, 1f, 150);
            }
        }
    }

    private static void DrawAccumulatedResidue(SKCanvas canvas, SKRect bounds)
    {
        DrawTimeline(canvas, bounds);
        for (var index = 0; index < 4; index++)
        {
            var x = bounds.Left + bounds.Width * (0.32f + index * 0.11f);
            var y = bounds.Bottom - 16 - index * 4;
            DrawTensionSpot(canvas, new SKPoint(x, y), 6f + index * 1.5f);
        }
    }

    private static void DrawStablePattern(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var radius = Math.Min(bounds.Width, bounds.Height) * 0.25f;
        for (var index = 0; index < 6; index++)
        {
            var a = PointOnCircle(center, radius, index * 60);
            var b = PointOnCircle(center, radius, (index + 1) * 60);
            DrawCarrierLine(canvas, a, b);
            DrawPoint(canvas, a, PositiveStroke, 3.8f);
        }
        DrawPoint(canvas, center, SiteColor, 4.6f);
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

    private static void DrawOpenOffsetRead(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 16, bounds.Bottom - 20);
        var b = new SKPoint(bounds.Left + bounds.Width * 0.42f, bounds.MidY + 6);
        var c = new SKPoint(bounds.Right - 18, bounds.Top + bounds.Height * 0.34f);
        using var source = PolyPath(a, b, c);
        using var offset = OffsetPolyline(18f, a, b, c);
        DrawBridgePath(canvas, source, BoundaryColor, 1.8f, glow: false);
        DrawBridgePath(canvas, offset, PositiveStroke, 2.3f, glow: true);
        DrawPolylineOffsetWitnesses(canvas, 18f, a, b, c);
    }

    private static void DrawCurvedOffsetRead(SKCanvas canvas, SKRect bounds)
    {
        var p0 = new SKPoint(bounds.Left + 14, bounds.Bottom - 20);
        var p1 = new SKPoint(bounds.Left + bounds.Width * 0.34f, bounds.Top + 6);
        var p2 = new SKPoint(bounds.Left + bounds.Width * 0.55f, bounds.Bottom - 10);
        var p3 = new SKPoint(bounds.Right - 16, bounds.Top + bounds.Height * 0.35f);
        using var source = CubicPath(p0, p1, p2, p3);
        using var offset = OffsetCubicPath(18f, p0, p1, p2, p3);
        DrawBridgePath(canvas, source, BoundaryColor, 1.7f, glow: false);
        DrawBridgePath(canvas, offset, PositiveStroke, 2.25f, glow: true);
        DrawCubicOffsetWitnesses(canvas, p0, p1, p2, p3, 18f, 4);
        DrawPathPartitions(canvas, offset, 5);
    }

    private static void DrawInsetOutsetLayers(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var width = bounds.Width * 0.48f;
        var height = bounds.Height * 0.42f;
        using var source = RoundedRectPath(new SKRect(center.X - width * 0.5f, center.Y - height * 0.5f, center.X + width * 0.5f, center.Y + height * 0.5f), 10f);
        using var outset1 = RoundedRectPath(new SKRect(center.X - width * 0.62f, center.Y - height * 0.64f, center.X + width * 0.62f, center.Y + height * 0.64f), 13f);
        using var outset2 = RoundedRectPath(new SKRect(center.X - width * 0.76f, center.Y - height * 0.80f, center.X + width * 0.76f, center.Y + height * 0.80f), 16f);
        using var inset1 = RoundedRectPath(new SKRect(center.X - width * 0.36f, center.Y - height * 0.34f, center.X + width * 0.36f, center.Y + height * 0.34f), 7f);
        using var inset2 = RoundedRectPath(new SKRect(center.X - width * 0.20f, center.Y - height * 0.17f, center.X + width * 0.20f, center.Y + height * 0.17f), 4f);
        DrawBridgePath(canvas, outset2, PositiveStroke, 1.7f, glow: false);
        DrawBridgePath(canvas, outset1, PositiveStroke, 2.1f, glow: false);
        DrawBridgePath(canvas, source, BoundaryColor, 2.3f, glow: false);
        DrawBridgePath(canvas, inset1, InverseStroke, 2.1f, glow: false);
        DrawBridgePath(canvas, inset2, InverseStroke, 1.7f, glow: false);
    }

    private static void DrawBranchMergeFillRead(SKCanvas canvas, SKRect bounds)
    {
        var left = new SKPoint(bounds.Left + 16, bounds.MidY);
        var split = new SKPoint(bounds.Left + bounds.Width * 0.30f, bounds.MidY);
        var merge = new SKPoint(bounds.Right - bounds.Width * 0.30f, bounds.MidY);
        var right = new SKPoint(bounds.Right - 16, bounds.MidY);
        var upper = new SKPoint(bounds.MidX, bounds.Top + bounds.Height * 0.24f);
        var lower = new SKPoint(bounds.MidX, bounds.Bottom - bounds.Height * 0.24f);

        using var fillPath = new SKPath();
        fillPath.MoveTo(split);
        fillPath.QuadTo(upper.X, upper.Y, merge.X, merge.Y);
        fillPath.QuadTo(lower.X, lower.Y, split.X, split.Y);
        fillPath.Close();
        using var fill = FillPaint(PositiveColor, 88);
        canvas.DrawPath(fillPath, fill);

        DrawCarrierLine(canvas, left, split);
        DrawCarrierLine(canvas, merge, right);
        using var top = new SKPath();
        top.MoveTo(split);
        top.QuadTo(upper.X, upper.Y, merge.X, merge.Y);
        using var bottom = new SKPath();
        bottom.MoveTo(split);
        bottom.QuadTo(lower.X, lower.Y, merge.X, merge.Y);
        DrawBridgePath(canvas, top, PositiveStroke, 2.2f, glow: true);
        DrawBridgePath(canvas, bottom, PositiveStroke, 2.2f, glow: true);
        DrawPoint(canvas, split, EndpointColor, 4.8f);
        DrawPoint(canvas, merge, EndpointColor, 4.8f);
    }

    private static void DrawOutlineFillRead(SKCanvas canvas, SKRect bounds)
    {
        var rect = new SKRect(bounds.Left + bounds.Width * 0.22f, bounds.Top + bounds.Height * 0.18f, bounds.Right - bounds.Width * 0.20f, bounds.Bottom - bounds.Height * 0.18f);
        using var outline = RoundedRectPath(rect, 14f);
        using var fill = FillPaint(PositiveColor, 95);
        canvas.DrawPath(outline, fill);
        DrawBridgePath(canvas, outline, BoundaryColor, 2f, glow: false);
        DrawSpinArrow(canvas, Center(rect), Math.Min(rect.Width, rect.Height) * 0.34f, inverse: false);
    }

    private static void DrawOffsetResidueRead(SKCanvas canvas, SKRect bounds)
    {
        var a = new SKPoint(bounds.Left + 18, bounds.Bottom - 18);
        var b = new SKPoint(bounds.Left + bounds.Width * 0.44f, bounds.MidY - 2);
        var c = new SKPoint(bounds.Right - 18, bounds.Top + bounds.Height * 0.32f);
        using var source = PolyPath(a, b, c);
        using var clean = PolyPath(
            new SKPoint(a.X + 4, a.Y - 17),
            new SKPoint(b.X + 5, b.Y - 18),
            new SKPoint(c.X + 4, c.Y - 17));
        using var miss = PolyPath(
            new SKPoint(a.X + 5, a.Y - 17),
            new SKPoint(b.X + 14, b.Y - 10),
            new SKPoint(c.X + 4, c.Y - 17));
        DrawBridgePath(canvas, source, BoundaryColor, 1.7f, glow: false);
        DrawBridgePath(canvas, clean, PositiveStroke, 1.6f, glow: false);
        DrawBridgePath(canvas, miss, InverseStroke, 2f, glow: true);
        DrawTensionSpot(canvas, new SKPoint(b.X + 10, b.Y - 13), 13f);
    }

    private static void DrawBooleanSourceCarriers(SKCanvas canvas, SKRect bounds)
    {
        var rect = BooleanFrameRect(bounds, 0.64f, 0.48f);
        var gap = Math.Min(rect.Width, rect.Height) * 0.12f;
        var segments = BooleanFrameSegments(rect, gap);
        foreach (var (start, end) in segments)
        {
            DrawCarrierLine(canvas, start, end);
            DrawPoint(canvas, start, SiteColor, 3.4f);
            DrawPoint(canvas, end, SiteColor, 3.4f);
        }
    }

    private static void DrawBooleanOffsetReads(SKCanvas canvas, SKRect bounds)
    {
        var rect = BooleanFrameRect(bounds, 0.62f, 0.46f);
        var gap = Math.Min(rect.Width, rect.Height) * 0.10f;
        foreach (var (start, end) in BooleanFrameSegments(rect, gap))
        {
            DrawOffsetCarrierBand(canvas, start, end, 15f);
            DrawCarrierLine(canvas, start, end);
        }
    }

    private static void DrawBooleanSplitIntersections(SKCanvas canvas, SKRect bounds)
    {
        var outer = BooleanFrameRect(bounds, 0.68f, 0.52f);
        var inner = BooleanFrameRect(bounds, 0.36f, 0.22f);
        using var outerStroke = StrokePaint(PositiveStroke, 2f, 220);
        using var innerStroke = StrokePaint(InverseStroke, 1.8f, 200);
        canvas.DrawRect(outer, outerStroke);
        canvas.DrawRect(inner, innerStroke);
        DrawRectPartitions(canvas, outer, 2, PositiveStroke);
        DrawRectPartitions(canvas, inner, 1, InverseStroke);
        foreach (var point in RectCorners(outer).Concat(RectCorners(inner)))
        {
            DrawPoint(canvas, point, EndpointColor, 4.3f);
            DrawTensionSpot(canvas, point, 8f, EndpointColor);
        }
    }

    private static void DrawBooleanUnionHole(SKCanvas canvas, SKRect bounds)
    {
        var outer = BooleanFrameRect(bounds, 0.68f, 0.52f);
        var inner = BooleanFrameRect(bounds, 0.36f, 0.22f);
        using var ring = RectRingPath(outer, inner);
        using var fill = FillPaint(PositiveColor, 104);
        canvas.DrawPath(ring, fill);
        DrawRectOutline(canvas, outer, PositiveStroke, 2.1f);
        DrawRectOutline(canvas, inner, InverseStroke, 1.9f);
        DrawArcArrow(canvas, new SKPoint(outer.MidX, outer.Top + 1), outer.Width * 0.18f, 190f, 305f, PositiveStroke, 1.25f);
        DrawArcArrow(canvas, new SKPoint(inner.MidX, inner.Top + inner.Height * 0.24f), inner.Width * 0.15f, 202f, 64f, InverseStroke, 1.15f);
    }

    private static void DrawBooleanIntersectionRead(SKCanvas canvas, SKRect bounds)
    {
        var center = Center(bounds);
        var horizontal = new SKRect(bounds.Left + bounds.Width * 0.15f, center.Y - bounds.Height * 0.12f, bounds.Right - bounds.Width * 0.15f, center.Y + bounds.Height * 0.12f);
        var vertical = new SKRect(center.X - bounds.Width * 0.12f, bounds.Top + bounds.Height * 0.16f, center.X + bounds.Width * 0.12f, bounds.Bottom - bounds.Height * 0.16f);
        var overlap = SKRect.Intersect(horizontal, vertical);
        using var input = FillPaint(PositiveColor, 56);
        canvas.DrawRect(horizontal, input);
        canvas.DrawRect(vertical, input);
        DrawRectOutline(canvas, horizontal, BoundaryColor, 1.3f, 170);
        DrawRectOutline(canvas, vertical, BoundaryColor, 1.3f, 170);
        using var shared = FillPaint(PositiveColor, 150);
        canvas.DrawRect(overlap, shared);
        DrawRectOutline(canvas, overlap, PositiveStroke, 2.2f);
        DrawCarrierLine(canvas, new SKPoint(horizontal.Left, horizontal.MidY), new SKPoint(horizontal.Right, horizontal.MidY));
        DrawCarrierLine(canvas, new SKPoint(vertical.MidX, vertical.Top), new SKPoint(vertical.MidX, vertical.Bottom));
        foreach (var point in RectCorners(overlap))
        {
            DrawPoint(canvas, point, EndpointColor, 3.8f);
        }
    }

    private static void DrawBooleanRoundedLadder(SKCanvas canvas, SKRect bounds)
    {
        var gap = Math.Max(8f, bounds.Width * 0.035f);
        var stageWidth = (bounds.Width - gap * 2f) / 3f;
        var stages = new[]
        {
            new SKRect(bounds.Left, bounds.Top + 4, bounds.Left + stageWidth, bounds.Bottom - 4),
            new SKRect(bounds.Left + stageWidth + gap, bounds.Top + 4, bounds.Left + stageWidth * 2f + gap, bounds.Bottom - 4),
            new SKRect(bounds.Left + stageWidth * 2f + gap * 2f, bounds.Top + 4, bounds.Right, bounds.Bottom - 4)
        };

        var source = BooleanFrameRect(stages[0], 0.70f, 0.48f);
        DrawRectOutline(canvas, source, BoundaryColor, 1.8f, 190);
        foreach (var point in RectCorners(source))
        {
            DrawPoint(canvas, point, EndpointColor, 3.1f);
        }

        var rounded = BooleanFrameRect(stages[1], 0.72f, 0.50f);
        using (var roundedPath = RoundedRectPath(rounded, Math.Min(rounded.Width, rounded.Height) * 0.18f))
        {
            DrawBridgePath(canvas, roundedPath, PositiveStroke, 2.2f, glow: true);
        }
        DrawCornerReplacementMarks(canvas, rounded);

        var outer = BooleanFrameRect(stages[2], 0.76f, 0.56f);
        var inner = BooleanFrameRect(stages[2], 0.36f, 0.22f);
        using (var ring = RoundedRingPath(outer, inner, 9f, 5f))
        using (var fill = FillPaint(PositiveColor, 110))
        {
            canvas.DrawPath(ring, fill);
        }
        DrawRoundRectOutline(canvas, outer, 9f, PositiveStroke, 1.9f);
        DrawRoundRectOutline(canvas, inner, 5f, InverseStroke, 1.6f);

        DrawVectorArrow(canvas, new SKPoint(stages[0].Right + 2, stages[0].MidY), new SKPoint(stages[1].Left - 2, stages[1].MidY), BoundaryColor, 1.1f, 160);
        DrawVectorArrow(canvas, new SKPoint(stages[1].Right + 2, stages[1].MidY), new SKPoint(stages[2].Left - 2, stages[2].MidY), BoundaryColor, 1.1f, 160);
    }

    private static SKRect BooleanFrameRect(SKRect bounds, float widthFactor, float heightFactor)
    {
        var width = bounds.Width * widthFactor;
        var height = bounds.Height * heightFactor;
        return new SKRect(
            bounds.MidX - width * 0.5f,
            bounds.MidY - height * 0.5f,
            bounds.MidX + width * 0.5f,
            bounds.MidY + height * 0.5f);
    }

    private static IReadOnlyList<(SKPoint Start, SKPoint End)> BooleanFrameSegments(SKRect rect, float gap) =>
    [
        (new SKPoint(rect.Left + gap, rect.Top), new SKPoint(rect.Right - gap, rect.Top)),
        (new SKPoint(rect.Right, rect.Top + gap), new SKPoint(rect.Right, rect.Bottom - gap)),
        (new SKPoint(rect.Right - gap, rect.Bottom), new SKPoint(rect.Left + gap, rect.Bottom)),
        (new SKPoint(rect.Left, rect.Bottom - gap), new SKPoint(rect.Left, rect.Top + gap))
    ];

    private static void DrawOffsetCarrierBand(SKCanvas canvas, SKPoint start, SKPoint end, float width)
    {
        using var soft = StrokePaint(PositiveColor, width + 6f, 45);
        canvas.DrawLine(start, end, soft);
        using var band = StrokePaint(PositiveColor, width, 62);
        canvas.DrawLine(start, end, band);
        using var edge = StrokePaint(PositiveStroke, 1.45f, 190);
        var direction = Normalize(new SKPoint(end.X - start.X, end.Y - start.Y));
        var normal = new SKPoint(-direction.Y, direction.X);
        var half = width * 0.5f;
        canvas.DrawLine(
            start.X + normal.X * half,
            start.Y + normal.Y * half,
            end.X + normal.X * half,
            end.Y + normal.Y * half,
            edge);
        canvas.DrawLine(
            start.X - normal.X * half,
            start.Y - normal.Y * half,
            end.X - normal.X * half,
            end.Y - normal.Y * half,
            edge);
    }

    private static void DrawRectPartitions(SKCanvas canvas, SKRect rect, int perSide, SKColor color)
    {
        using var paint = StrokePaint(color, 1.15f, 150);
        for (var side = 0; side < 4; side++)
        for (var index = 1; index <= perSide; index++)
        {
            var t = index / (float)(perSide + 1);
            var point = side switch
            {
                0 => new SKPoint(rect.Left + rect.Width * t, rect.Top),
                1 => new SKPoint(rect.Right, rect.Top + rect.Height * t),
                2 => new SKPoint(rect.Right - rect.Width * t, rect.Bottom),
                _ => new SKPoint(rect.Left, rect.Bottom - rect.Height * t)
            };
            var vertical = side is 0 or 2;
            canvas.DrawLine(
                point.X + (vertical ? 0 : -4f),
                point.Y + (vertical ? -4f : 0),
                point.X + (vertical ? 0 : 4f),
                point.Y + (vertical ? 4f : 0),
                paint);
        }
    }

    private static SKPoint[] RectCorners(SKRect rect) =>
    [
        new(rect.Left, rect.Top),
        new(rect.Right, rect.Top),
        new(rect.Right, rect.Bottom),
        new(rect.Left, rect.Bottom)
    ];

    private static SKPath RectRingPath(SKRect outer, SKRect inner)
    {
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.AddRect(outer);
        path.AddRect(inner);
        return path;
    }

    private static SKPath RoundedRingPath(SKRect outer, SKRect inner, float outerRadius, float innerRadius)
    {
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.AddRoundRect(outer, outerRadius, outerRadius);
        path.AddRoundRect(inner, innerRadius, innerRadius);
        return path;
    }

    private static void DrawRectOutline(SKCanvas canvas, SKRect rect, SKColor color, float width, byte alpha = 255)
    {
        using var stroke = StrokePaint(color, width, alpha);
        canvas.DrawRect(rect, stroke);
    }

    private static void DrawRoundRectOutline(SKCanvas canvas, SKRect rect, float radius, SKColor color, float width, byte alpha = 255)
    {
        using var stroke = StrokePaint(color, width, alpha);
        canvas.DrawRoundRect(rect, radius, radius, stroke);
    }

    private static void DrawCornerReplacementMarks(SKCanvas canvas, SKRect rect)
    {
        var radius = Math.Min(rect.Width, rect.Height) * 0.18f;
        var corners = new[]
        {
            new SKPoint(rect.Right - radius, rect.Top + radius),
            new SKPoint(rect.Right - radius, rect.Bottom - radius),
            new SKPoint(rect.Left + radius, rect.Bottom - radius),
            new SKPoint(rect.Left + radius, rect.Top + radius)
        };
        foreach (var corner in corners)
        {
            DrawPoint(canvas, corner, EndpointColor, 2.9f);
        }
    }

    private static void DrawDroneClipSource(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DroneClipOuterPath(bounds, rounded: false);
        DrawBridgePath(canvas, outer, BoundaryColor, 1.5f, glow: false);
        DrawDroneClipSlot(canvas, bounds, BoundaryColor, 1.2f, 160, rounded: false);
        DrawDroneClipHoles(canvas, bounds, BoundaryColor, 1.1f, 150);
        foreach (var point in DroneClipRawPoints(bounds).Where((_, index) => index % 2 == 0))
        {
            DrawPoint(canvas, point, SiteColor, 2.6f);
        }
    }

    private static void DrawDroneClipPathRead(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DroneClipOuterPath(bounds, rounded: false);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.05f, glow: true);
        DrawPathPartitions(canvas, outer, 10);
        DrawDroneClipSlot(canvas, bounds, InverseStroke, 1.55f, 190, rounded: false);
        DrawDroneClipHoles(canvas, bounds, InverseStroke, 1.35f, 180);
    }

    private static void DrawDroneClipOffsetReads(SKCanvas canvas, SKRect bounds)
    {
        using var source = DroneClipOuterPath(bounds, rounded: false);
        DrawBridgePath(canvas, source, BoundaryColor, 1f, glow: false);
        using var outer = DroneClipOuterPath(bounds, rounded: true, inflate: 4.2f);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.3f, glow: true);
        using var slotFill = FillPaint(InverseColor, 42);
        var slot = DroneClipSlotRect(bounds, inflate: 3.4f);
        canvas.DrawRoundRect(slot, 5f, 5f, slotFill);
        DrawRoundRectOutline(canvas, slot, 5f, InverseStroke, 1.5f, 175);
        DrawDroneClipHoles(canvas, bounds, InverseStroke, 1.4f, 170, inflate: 1.6f);
    }

    private static void DrawDroneClipIntersections(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DroneClipOuterPath(bounds, rounded: false, inflate: 3.6f);
        DrawBridgePath(canvas, outer, PositiveStroke, 1.8f, glow: false);
        DrawDroneClipSlot(canvas, bounds, InverseStroke, 1.5f, 170, rounded: false, inflate: 2.8f);
        foreach (var point in DroneClipIntersectionPoints(bounds))
        {
            DrawTensionSpot(canvas, point, 6.2f, EndpointColor);
            DrawPoint(canvas, point, EndpointColor, 3.2f);
        }
    }

    private static void DrawDroneClipSplitSections(SKCanvas canvas, SKRect bounds)
    {
        var points = DroneClipRawPoints(bounds);
        for (var index = 0; index < points.Length; index++)
        {
            var start = points[index];
            var end = points[(index + 1) % points.Length];
            var color = index is 2 or 3 or 10 or 11 ? BoundaryColor : PositiveStroke;
            var alpha = color == BoundaryColor ? (byte)120 : (byte)220;
            DrawLine(canvas, start, end, color, color == BoundaryColor ? 1.1f : 2.1f, alpha);
        }
        var slot = DroneClipSlotRect(bounds);
        DrawRectOutline(canvas, slot, InverseStroke, 1.7f, 190);
        DrawRectPartitions(canvas, slot, 1, InverseStroke);
        foreach (var point in DroneClipIntersectionPoints(bounds))
        {
            DrawPoint(canvas, point, EndpointColor, 2.9f);
        }
    }

    private static void DrawDroneClipOutsideRound(SKCanvas canvas, SKRect bounds)
    {
        using var source = DroneClipOuterPath(bounds, rounded: false);
        DrawBridgePath(canvas, source, BoundaryColor, 1.05f, glow: false);
        using var rounded = DroneClipOuterPath(bounds, rounded: true);
        DrawBridgePath(canvas, rounded, PositiveStroke, 2.35f, glow: true);
        DrawDroneOutsideRoundMarks(canvas, bounds);
    }

    private static void DrawDroneClipInsideRound(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DroneClipOuterPath(bounds, rounded: true);
        DrawBridgePath(canvas, outer, PositiveStroke, 1.5f, glow: false);
        DrawDroneClipSlot(canvas, bounds, BoundaryColor, 1f, 120, rounded: false);
        DrawDroneClipSlot(canvas, bounds, InverseStroke, 1.8f, 220, rounded: true, inflate: 2.6f);
        DrawDroneClipHoles(canvas, bounds, InverseStroke, 1.6f, 200, inflate: 1.1f);
    }

    private static void DrawDroneClipMergedLoops(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DroneClipOuterPath(bounds, rounded: true);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.1f, glow: true);
        DrawDroneClipSlot(canvas, bounds, InverseStroke, 1.7f, 220, rounded: true, inflate: 2.4f);
        DrawDroneClipHoles(canvas, bounds, InverseStroke, 1.45f, 210, inflate: 1.1f);
        DrawArcArrow(canvas, DroneClipPoint(bounds, 0, -27), DroneClipScale(bounds) * 15f, 195f, 325f, PositiveStroke, 1.1f);
        DrawArcArrow(canvas, DroneClipPoint(bounds, 0, -2.5f), DroneClipScale(bounds) * 9f, 185f, 45f, InverseStroke, 1.05f);
    }

    private static void DrawDroneClipNegativeSpaces(SKCanvas canvas, SKRect bounds)
    {
        using var path = DroneClipFillPath(bounds);
        using var fill = FillPaint(PositiveColor, 118);
        canvas.DrawPath(path, fill);
        using var outer = DroneClipOuterPath(bounds, rounded: true);
        DrawBridgePath(canvas, outer, PositiveStroke, 1.9f, glow: false);
        DrawDroneClipSlot(canvas, bounds, InverseStroke, 1.55f, 230, rounded: true, inflate: 2.4f);
        DrawDroneClipHoles(canvas, bounds, InverseStroke, 1.25f, 210, inflate: 1.1f);
    }

    private static void DrawDroneClipSettledRead(SKCanvas canvas, SKRect bounds)
    {
        using var path = DroneClipFillPath(bounds);
        using var fill = FillPaint(PositiveColor, 132);
        canvas.DrawPath(path, fill);
        using var outer = DroneClipOuterPath(bounds, rounded: true);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.05f, glow: false);
        DrawPathPartitions(canvas, outer, 12);
        DrawDroneClipSlot(canvas, bounds, InverseStroke, 1.55f, 235, rounded: true, inflate: 2.4f);
        DrawDroneClipHoles(canvas, bounds, InverseStroke, 1.3f, 220, inflate: 1.1f);
        DrawDashedLine(canvas, DroneClipPoint(bounds, -52, -12), DroneClipPoint(bounds, -44, -18), EvidenceColor, 0.9f);
        DrawDashedLine(canvas, DroneClipPoint(bounds, 52, 12), DroneClipPoint(bounds, 44, 18), EvidenceColor, 0.9f);
    }

    private static SKPath DroneClipOuterPath(SKRect bounds, bool rounded, float inflate = 0)
    {
        var path = new SKPath();
        if (!rounded)
        {
            var points = DroneClipRawPoints(bounds, inflate);
            path.MoveTo(points[0]);
            for (var index = 1; index < points.Length; index++)
            {
                path.LineTo(points[index]);
            }
            path.Close();
            return path;
        }

        path.MoveTo(DroneClipPoint(bounds, -44 - inflate, -14 - inflate * 0.25f));
        path.LineTo(DroneClipPoint(bounds, -37 - inflate * 0.6f, -20 - inflate));
        path.LineTo(DroneClipPoint(bounds, -13 - inflate * 0.25f, -20 - inflate));
        path.QuadTo(DroneClipPoint(bounds, -9, -26 - inflate), DroneClipPoint(bounds, -4, -25 - inflate));
        path.LineTo(DroneClipPoint(bounds, 4, -25 - inflate));
        path.QuadTo(DroneClipPoint(bounds, 9, -26 - inflate), DroneClipPoint(bounds, 13 + inflate * 0.25f, -20 - inflate));
        path.LineTo(DroneClipPoint(bounds, 37 + inflate * 0.6f, -20 - inflate));
        path.QuadTo(DroneClipPoint(bounds, 52 + inflate, -20 - inflate * 0.35f), DroneClipPoint(bounds, 52 + inflate, -12));
        path.LineTo(DroneClipPoint(bounds, 52 + inflate, 12));
        path.QuadTo(DroneClipPoint(bounds, 52 + inflate, 20 + inflate * 0.35f), DroneClipPoint(bounds, 37 + inflate * 0.6f, 20 + inflate));
        path.LineTo(DroneClipPoint(bounds, 13 + inflate * 0.25f, 20 + inflate));
        path.QuadTo(DroneClipPoint(bounds, 9, 26 + inflate), DroneClipPoint(bounds, 4, 25 + inflate));
        path.LineTo(DroneClipPoint(bounds, -4, 25 + inflate));
        path.QuadTo(DroneClipPoint(bounds, -9, 26 + inflate), DroneClipPoint(bounds, -13 - inflate * 0.25f, 20 + inflate));
        path.LineTo(DroneClipPoint(bounds, -37 - inflate * 0.6f, 20 + inflate));
        path.QuadTo(DroneClipPoint(bounds, -52 - inflate, 20 + inflate * 0.35f), DroneClipPoint(bounds, -52 - inflate, 12));
        path.LineTo(DroneClipPoint(bounds, -52 - inflate, -12));
        path.QuadTo(DroneClipPoint(bounds, -52 - inflate, -20 - inflate * 0.35f), DroneClipPoint(bounds, -44 - inflate, -14 - inflate * 0.25f));
        path.Close();
        return path;
    }

    private static SKPath DroneClipFillPath(SKRect bounds)
    {
        var path = DroneClipOuterPath(bounds, rounded: true);
        path.FillType = SKPathFillType.EvenOdd;
        var slot = DroneClipSlotRect(bounds, inflate: 2.4f);
        path.AddRoundRect(slot, 5f, 5f);
        foreach (var (center, radius) in DroneClipHoleCircles(bounds, inflate: 1.1f))
        {
            path.AddCircle(center.X, center.Y, radius);
        }
        return path;
    }

    private static SKPoint[] DroneClipRawPoints(SKRect bounds, float inflate = 0) =>
    [
        DroneClipPoint(bounds, -52 - inflate, -12),
        DroneClipPoint(bounds, -39 - inflate * 0.5f, -20 - inflate),
        DroneClipPoint(bounds, -12, -20 - inflate),
        DroneClipPoint(bounds, -7, -26 - inflate),
        DroneClipPoint(bounds, 7, -26 - inflate),
        DroneClipPoint(bounds, 12, -20 - inflate),
        DroneClipPoint(bounds, 39 + inflate * 0.5f, -20 - inflate),
        DroneClipPoint(bounds, 52 + inflate, -12),
        DroneClipPoint(bounds, 52 + inflate, 12),
        DroneClipPoint(bounds, 39 + inflate * 0.5f, 20 + inflate),
        DroneClipPoint(bounds, 12, 20 + inflate),
        DroneClipPoint(bounds, 7, 26 + inflate),
        DroneClipPoint(bounds, -7, 26 + inflate),
        DroneClipPoint(bounds, -12, 20 + inflate),
        DroneClipPoint(bounds, -39 - inflate * 0.5f, 20 + inflate),
        DroneClipPoint(bounds, -52 - inflate, 12)
    ];

    private static IEnumerable<SKPoint> DroneClipIntersectionPoints(SKRect bounds)
    {
        foreach (var index in new[] { 0, 3, 4, 7, 8, 11, 12, 15 })
        {
            yield return DroneClipRawPoints(bounds, 3.6f)[index];
        }

        var slot = DroneClipSlotRect(bounds, inflate: 2.8f);
        foreach (var point in RectCorners(slot))
        {
            yield return point;
        }
    }

    private static void DrawDroneClipSlot(SKCanvas canvas, SKRect bounds, SKColor color, float width, byte alpha, bool rounded, float inflate = 0)
    {
        var slot = DroneClipSlotRect(bounds, inflate);
        if (rounded)
        {
            DrawRoundRectOutline(canvas, slot, 5f, color, width, alpha);
        }
        else
        {
            DrawRectOutline(canvas, slot, color, width, alpha);
        }
    }

    private static SKRect DroneClipSlotRect(SKRect bounds, float inflate = 0)
    {
        var topLeft = DroneClipPoint(bounds, -26 - inflate, -7 - inflate);
        var bottomRight = DroneClipPoint(bounds, 26 + inflate, 7 + inflate);
        return new SKRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    private static void DrawDroneClipHoles(SKCanvas canvas, SKRect bounds, SKColor color, float width, byte alpha, float inflate = 0)
    {
        using var stroke = StrokePaint(color, width, alpha);
        foreach (var (center, radius) in DroneClipHoleCircles(bounds, inflate))
        {
            canvas.DrawCircle(center, radius, stroke);
        }
    }

    private static IReadOnlyList<(SKPoint Center, float Radius)> DroneClipHoleCircles(SKRect bounds, float inflate = 0)
    {
        var scale = DroneClipScale(bounds);
        return
        [
            (DroneClipPoint(bounds, -37, 0), scale * (3.4f + inflate)),
            (DroneClipPoint(bounds, 37, 0), scale * (3.4f + inflate))
        ];
    }

    private static void DrawDroneOutsideRoundMarks(SKCanvas canvas, SKRect bounds)
    {
        foreach (var point in DroneClipIntersectionPoints(bounds).Take(8))
        {
            DrawPoint(canvas, point, EndpointColor, 2.8f);
        }
    }

    private static SKPoint DroneClipPoint(SKRect bounds, float x, float y)
    {
        var scale = DroneClipScale(bounds);
        return new SKPoint(bounds.MidX + x * scale, bounds.MidY + y * scale);
    }

    private static float DroneClipScale(SKRect bounds) =>
        Math.Min(bounds.Width / 124f, bounds.Height / 72f);

    private static void DrawRadialCrescentSourceSeeds(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentBodies(canvas, bounds, BoundaryColor, 1.1f, 145);
        DrawRadialCrescentCenterStrokes(canvas, bounds, BoundaryColor, 1.8f, 220);
        foreach (var point in RadialCrescentSourceSites(bounds))
        {
            DrawPoint(canvas, point, SiteColor, 2.6f);
        }
    }

    private static void DrawRadialCrescentEndpointCaps(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentBodies(canvas, bounds, BoundaryColor, 0.9f, 95);
        DrawRadialCrescentCenterStrokes(canvas, bounds, BoundaryColor, 1.2f, 115);
        DrawRadialCrescentEndpointCircles(canvas, bounds, PositiveStroke, 1.75f, 210, outer: true);
        DrawRadialCrescentEndpointCircles(canvas, bounds, InverseStroke, 1.15f, 175, outer: false);
        foreach (var point in RadialCrescentEndpointCenters(bounds))
        {
            DrawPoint(canvas, point, EndpointColor, 2.8f);
        }
    }

    private static void DrawRadialCrescentTrimmedCarriers(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentBodies(canvas, bounds, BoundaryColor, 0.9f, 88);
        DrawRadialCrescentBodyHollows(canvas, bounds, BoundaryColor, 0.85f, 76);
        DrawRadialCrescentEndpointCircles(canvas, bounds, BoundaryColor, 0.9f, 95, outer: true);
        DrawRadialCrescentCenterStrokes(canvas, bounds, BoundaryColor, 1f, 85);
        DrawRadialCrescentCenterStrokes(canvas, bounds, PositiveStroke, 2.35f, 230, trimmed: true, glow: true);
        foreach (var point in RadialCrescentEndpointCenters(bounds))
        {
            DrawPoint(canvas, point, EndpointColor, 2.5f);
        }
    }

    private static void DrawRadialCrescentOffsetBoundaries(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentBodies(canvas, bounds, BoundaryColor, 0.85f, 75);
        DrawRadialCrescentCenterStrokes(canvas, bounds, BoundaryColor, 0.95f, 80, trimmed: true);
        DrawRadialCrescentArcOffsets(canvas, bounds);
        DrawRadialCrescentBentOffsets(canvas, bounds);
        DrawRadialCrescentBodyOffsets(canvas, bounds);
        DrawRadialCrescentEndpointCircles(canvas, bounds, PositiveStroke, 1.35f, 190, outer: true);
        DrawRadialCrescentEndpointCircles(canvas, bounds, InverseStroke, 1.05f, 165, outer: false);
        DrawRadialCrescentOffsetWitnesses(canvas, bounds);
    }

    private static void DrawRadialCrescentFillParticipants(SKCanvas canvas, SKRect bounds)
    {
        using var inverse = FillPaint(InverseColor, 52);
        DrawRadialCrescentPositiveParticipants(canvas, bounds, 68);
        DrawRadialCrescentAreaOutlines(canvas, bounds, roundedExterior: false, PositiveStroke, 1.25f, 170);
        using var holes = RadialCrescentInteriorVoidsPath(bounds, rounded: false);
        canvas.DrawPath(holes, inverse);
        DrawRadialCrescentInteriorVoids(canvas, bounds, InverseStroke, 1.15f, 190, rounded: false);
    }

    private void DrawRadialCrescentMergedAreas(SKCanvas canvas, SKRect bounds)
    {
        using var positive = RadialCrescentPositiveOuterUnionPath(bounds);
        using var openings = RadialCrescentMergedOpeningPath(bounds);
        using var merged = PathOp(positive, openings, SKPathOp.Difference);
        using (var fill = FillPaint(PositiveColor, 118))
        {
            canvas.DrawPath(merged, fill);
        }

        using var exteriorRead = StrokePaint(theme.Ink, 1.45f, 225);
        canvas.DrawPath(positive, exteriorRead);
        using var interiorRead = StrokePaint(InverseStroke, 1.45f, 225);
        canvas.DrawPath(openings, interiorRead);
    }

    private static void DrawDiamondCuspSource(SKCanvas canvas, SKRect bounds)
    {
        using var outline = DiamondCuspSourcePath(bounds);
        using var inner = DiamondInnerSourcePath(bounds);
        DrawBridgePath(canvas, outline, BoundaryColor, 2.15f, glow: false);
        DrawBridgePath(canvas, inner, BoundaryColor, 1.75f, glow: false);
        DrawDiamondSharedPoints(canvas, bounds, SiteColor, 4.3f, 210);
        DrawDiamondAngleBasis(canvas, bounds, alpha: 80);
    }

    private static void DrawDiamondOuterSelection(SKCanvas canvas, SKRect bounds)
    {
        using var outline = DiamondCuspSourcePath(bounds);
        using var inner = DiamondInnerSourcePath(bounds);
        DrawBridgePath(canvas, outline, BoundaryColor, 1.9f, glow: false);
        DrawBridgePath(canvas, inner, BoundaryColor, 1.65f, glow: false);
        DrawDiamondAngleBasis(canvas, bounds, alpha: 105);
        foreach (var point in DiamondCuspPoints(bounds))
        {
            DrawDiamondSelectionPoint(canvas, point);
        }
    }

    private static void DrawDiamondOuterRounded(SKCanvas canvas, SKRect bounds)
    {
        using var source = DiamondCuspSourcePath(bounds);
        using var rounded = DiamondCuspRoundedPath(bounds);
        using var inner = DiamondInnerSourcePath(bounds);
        using var sourceStroke = StrokePaint(BoundaryColor, 1.3f, 112);
        canvas.DrawPath(source, sourceStroke);
        DrawBridgePath(canvas, rounded, PositiveStroke, 2.35f, glow: true);
        DrawBridgePath(canvas, inner, BoundaryColor, 1.6f, glow: false);
        foreach (var corner in DiamondCuspRoundedCornerPaths(bounds))
        {
            using (corner)
            {
                DrawBridgePath(canvas, corner, EndpointColor, 2.6f, glow: false);
            }
        }
    }

    private static void DrawDiamondInnerSelection(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DiamondCuspRoundedPath(bounds);
        using var inner = DiamondInnerSourcePath(bounds);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.05f, glow: false);
        DrawBridgePath(canvas, inner, BoundaryColor, 1.8f, glow: false);
        foreach (var point in DiamondInnerCuspPoints(bounds))
        {
            DrawDiamondSelectionPoint(canvas, point);
        }
    }

    private static void DrawDiamondInnerRounded(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DiamondCuspRoundedPath(bounds);
        using var innerSource = DiamondInnerSourcePath(bounds);
        using var innerRounded = DiamondInnerRoundedPath(bounds);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.05f, glow: false);
        using var sourceStroke = StrokePaint(BoundaryColor, 1.25f, 100);
        canvas.DrawPath(innerSource, sourceStroke);
        DrawBridgePath(canvas, innerRounded, InverseStroke, 2.25f, glow: false);
        foreach (var corner in DiamondInnerRoundedCornerPaths(bounds))
        {
            using (corner)
            {
                DrawBridgePath(canvas, corner, EndpointColor, 2.5f, glow: false);
            }
        }
    }

    private static void DrawDiamondCuspResult(SKCanvas canvas, SKRect bounds)
    {
        using var outer = DiamondCuspRoundedPath(bounds);
        using var inner = DiamondInnerRoundedPath(bounds);
        using var rounded = PathOp(outer, inner, SKPathOp.Difference);
        using var fill = FillPaint(PositiveColor, 82);
        canvas.DrawPath(rounded, fill);
        DrawBridgePath(canvas, outer, PositiveStroke, 2.1f, glow: false);
        DrawBridgePath(canvas, inner, InverseStroke, 1.95f, glow: false);
    }

    private void DrawRadialCrescentInteriorCuspSelection(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentOutlineReadSet(canvas, bounds, roundedInterior: false, roundedExterior: false, fill: false);
        DrawRadialCrescentInteriorCuspMarks(canvas, bounds);
    }

    private void DrawRadialCrescentRoundedInteriorOutlines(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentOutlineReadSet(canvas, bounds, roundedInterior: true, roundedExterior: false, fill: false);
    }

    private void DrawRadialCrescentExteriorCuspSelection(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentOutlineReadSet(canvas, bounds, roundedInterior: true, roundedExterior: false, fill: false);
        DrawRadialCrescentExteriorCuspMarks(canvas, bounds);
    }

    private void DrawRadialCrescentFinalRoundedOutline(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentOutlineReadSet(canvas, bounds, roundedInterior: true, roundedExterior: true, fill: true);
    }

    private void DrawRadialCrescentOutlineReadSet(SKCanvas canvas, SKRect bounds, bool roundedInterior, bool roundedExterior, bool fill)
    {
        using var positive = roundedExterior
            ? RadialCrescentRoundedPositiveOuterUnionPath(bounds)
            : RadialCrescentPositiveOuterUnionPath(bounds);
        using var openings = roundedInterior
            ? RadialCrescentRoundedMergedOpeningPath(bounds)
            : RadialCrescentMergedOpeningPath(bounds);

        if (fill)
        {
            using var merged = PathOp(positive, openings, SKPathOp.Difference);
            using var fillPaint = FillPaint(PositiveColor, 118);
            canvas.DrawPath(merged, fillPaint);
        }

        using var exteriorRead = StrokePaint(theme.Ink, fill ? 1.55f : 1.9f, 225);
        using var exteriorCorner = roundedExterior
            ? SKPathEffect.CreateCorner(Math.Max(4f, RadialCrescentScale(bounds) * 16f))
            : null;
        exteriorRead.PathEffect = exteriorCorner;
        canvas.DrawPath(positive, exteriorRead);

        using var interiorRead = StrokePaint(InverseStroke, fill ? 1.45f : 1.85f, 230);
        using var interiorCorner = roundedInterior
            ? SKPathEffect.CreateCorner(Math.Max(4f, RadialCrescentScale(bounds) * 14f))
            : null;
        interiorRead.PathEffect = interiorCorner;
        canvas.DrawPath(openings, interiorRead);
    }

    private static void DrawRadialCrescentOutlineRead(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentAreaOutlines(canvas, bounds, roundedExterior: false, PositiveStroke, 2.05f, 230);
        DrawRadialCrescentInteriorVoids(canvas, bounds, InverseStroke, 1.5f, 220, rounded: false);
        DrawRadialCrescentOutlinePartitions(canvas, bounds);
    }

    private static void DrawRadialCrescentInteriorRound(SKCanvas canvas, SKRect bounds)
    {
        using var area = RadialCrescentSettledAreaPath(bounds, roundedExterior: false, roundedInterior: true);
        using var fill = FillPaint(PositiveColor, 76);
        canvas.DrawPath(area, fill);
        DrawRadialCrescentAreaOutlines(canvas, bounds, roundedExterior: false, BoundaryColor, 1.1f, 120);
        DrawRadialCrescentInteriorVoids(canvas, bounds, InverseStroke, 1.8f, 235, rounded: true);
        foreach (var point in RadialCrescentInteriorCuspSites(bounds))
        {
            DrawTensionSpot(canvas, point, 5.2f, EndpointColor);
            DrawPoint(canvas, point, EndpointColor, 2.5f);
        }
    }

    private static void DrawRadialCrescentExteriorRound(SKCanvas canvas, SKRect bounds)
    {
        DrawRadialCrescentAreaOutlines(canvas, bounds, roundedExterior: false, BoundaryColor, 0.9f, 95);
        DrawRadialCrescentAreaOutlines(canvas, bounds, roundedExterior: true, PositiveStroke, 2.1f, 235);
        DrawRadialCrescentInteriorVoids(canvas, bounds, InverseStroke, 1.25f, 195, rounded: true);
        foreach (var point in RadialCrescentExteriorCuspSites(bounds))
        {
            DrawPoint(canvas, point, EndpointColor, 2.4f);
        }
    }

    private static void DrawRadialCrescentSettledArea(SKCanvas canvas, SKRect bounds)
    {
        using var area = RadialCrescentSettledAreaPath(bounds, roundedExterior: true, roundedInterior: true);
        using var fill = FillPaint(PositiveColor, 124);
        canvas.DrawPath(area, fill);
        DrawRadialCrescentAreaOutlines(canvas, bounds, roundedExterior: true, PositiveStroke, 1.85f, 230);
        DrawRadialCrescentInteriorVoids(canvas, bounds, InverseStroke, 1.35f, 230, rounded: true);
        DrawRadialCrescentOutlinePartitions(canvas, bounds);
        DrawDashedLine(canvas, RadialCrescentPoint(bounds, -182, 88), RadialCrescentPoint(bounds, -166, 104), EvidenceColor, 0.85f);
        DrawDashedLine(canvas, RadialCrescentPoint(bounds, 338, 108), RadialCrescentPoint(bounds, 354, 121), EvidenceColor, 0.85f);
    }

    private static void DrawRadialCrescentBodies(SKCanvas canvas, SKRect bounds, SKColor color, float width, byte alpha)
    {
        DrawModelCircle(canvas, bounds, -70, 0, 78, color, width, alpha);
        using var diamond = RadialCrescentDiamondPath(bounds, 265, 0, 76, 88, rounded: false);
        DrawBridgePath(canvas, diamond, color, width, glow: false);
    }

    private static void DrawRadialCrescentBodyHollows(SKCanvas canvas, SKRect bounds, SKColor color, float width, byte alpha)
    {
        DrawModelCircle(canvas, bounds, -70, 0, 68, color, width, alpha);
        using var diamond = RadialCrescentDiamondPath(bounds, 265, 0, 56, 68, rounded: false);
        DrawBridgePath(canvas, diamond, color, width, glow: false);
    }

    private static void DrawRadialCrescentCenterStrokes(
        SKCanvas canvas,
        SKRect bounds,
        SKColor color,
        float width,
        byte alpha = 255,
        bool trimmed = false,
        bool glow = false)
    {
        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var path = RadialCrescentArcPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, trimDegrees: trimmed ? 8f : 0f);
            DrawBridgePath(canvas, path, color, width, glow);
        }

        foreach (var polyline in RadialCrescentBentPolylines())
        {
            for (var index = 0; index + 1 < polyline.Length; index++)
            {
                var trimStart = trimmed && index == 0 ? 18f : 0f;
                var trimEnd = trimmed && index == polyline.Length - 2 ? 18f : 0f;
                DrawModelSegment(canvas, bounds, polyline[index], polyline[index + 1], color, width, alpha, trimStart, trimEnd);
            }
        }
    }

    private static void DrawRadialCrescentEndpointCircles(SKCanvas canvas, SKRect bounds, SKColor color, float width, byte alpha, bool outer)
    {
        var radius = outer ? 28f : 8f;
        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            DrawModelCircle(canvas, bounds, x, y, radius, color, width, alpha);
        }
    }

    private static void DrawRadialCrescentPositiveParticipants(SKCanvas canvas, SKRect bounds, byte alpha)
    {
        using var fill = FillPaint(PositiveColor, alpha);
        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var band = RadialCrescentArcBandPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, 10f);
            canvas.DrawPath(band, fill);
        }

        foreach (var polyline in RadialCrescentBentPolylines())
        {
            using var band = RadialCrescentPolylineBandPath(bounds, 10f, polyline);
            canvas.DrawPath(band, fill);
        }

        using (var circleBody = RadialCrescentCircleRingPath(bounds, -70, 0, 88, 68))
        using (var diamondBody = RadialCrescentDiamondRingPath(bounds, 265, 0, 86, 98, 56, 68, rounded: false))
        {
            canvas.DrawPath(circleBody, fill);
            canvas.DrawPath(diamondBody, fill);
        }

        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            using var motor = RadialCrescentCircleRingPath(bounds, x, y, 28, 8);
            canvas.DrawPath(motor, fill);
        }
    }

    private static SKPath RadialCrescentPositiveOuterUnionPath(SKRect bounds)
    {
        var composite = new SKPath();
        var hasPath = false;
        void Add(SKPath path)
        {
            if (!hasPath)
            {
                composite.AddPath(path);
                hasPath = true;
                return;
            }

            using var result = PathOp(composite, path, SKPathOp.Union);
            composite.Reset();
            composite.AddPath(result);
        }

        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var band = RadialCrescentArcBandPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, 10f);
            Add(band);
        }

        foreach (var polyline in RadialCrescentBentPolylines())
        {
            using var band = RadialCrescentPolylineBandPath(bounds, 10f, polyline);
            Add(band);
        }

        using (var circleBody = RadialCrescentModelCirclePath(bounds, -70, 0, 88))
        using (var diamondBody = RadialCrescentDiamondPath(bounds, 265, 0, 86, 98, rounded: false))
        {
            Add(circleBody);
            Add(diamondBody);
        }

        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            using var motor = RadialCrescentModelCirclePath(bounds, x, y, 28);
            Add(motor);
        }

        return composite;
    }

    private static SKPath RadialCrescentRoundedPositiveOuterUnionPath(SKRect bounds)
    {
        var composite = new SKPath();
        var hasPath = false;
        void Add(SKPath path)
        {
            if (!hasPath)
            {
                composite.AddPath(path);
                hasPath = true;
                return;
            }

            using var result = PathOp(composite, path, SKPathOp.Union);
            composite.Reset();
            composite.AddPath(result);
        }

        using (var crescent = RadialCrescentCrescentAreaPath(bounds, roundedExterior: true))
        using (var bent = RadialCrescentBentAreaPath(bounds, roundedExterior: true))
        {
            Add(crescent);
            Add(bent);
        }

        return composite;
    }

    private static SKPath RadialCrescentArmSupportUnionPath(SKRect bounds)
    {
        var composite = new SKPath();
        var hasPath = false;
        void Add(SKPath path)
        {
            if (!hasPath)
            {
                composite.AddPath(path);
                hasPath = true;
                return;
            }

            using var result = PathOp(composite, path, SKPathOp.Union);
            composite.Reset();
            composite.AddPath(result);
        }

        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var band = RadialCrescentArcBandPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, 10f);
            Add(band);
        }

        foreach (var polyline in RadialCrescentBentPolylines())
        {
            using var band = RadialCrescentPolylineBandPath(bounds, 10f, polyline);
            Add(band);
        }

        return composite;
    }

    private static SKPath RadialCrescentMergedOpeningPath(SKRect bounds)
    {
        using var arms = RadialCrescentArmSupportUnionPath(bounds);
        using var circleBodyOpening = RadialCrescentModelCirclePath(bounds, -70, 0, 68);
        using var diamondBodyOpening = RadialCrescentDiamondPath(bounds, 265, 0, 56, 68, rounded: false);
        using var circleVisibleOpening = PathOp(circleBodyOpening, arms, SKPathOp.Difference);
        using var diamondVisibleOpening = PathOp(diamondBodyOpening, arms, SKPathOp.Difference);
        var openings = PathOp(circleVisibleOpening, diamondVisibleOpening, SKPathOp.Union);
        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            using var motorOpening = RadialCrescentModelCirclePath(bounds, x, y, 8);
            using var result = PathOp(openings, motorOpening, SKPathOp.Union);
            openings.Reset();
            openings.AddPath(result);
        }

        return openings;
    }

    private static SKPath RadialCrescentRoundedMergedOpeningPath(SKRect bounds)
    {
        using var arms = RadialCrescentArmSupportUnionPath(bounds);
        using var circleBodyOpening = RadialCrescentModelCirclePath(bounds, -70, 0, 68);
        using var diamondBodyOpening = RadialCrescentDiamondPath(bounds, 265, 0, 56, 68, rounded: true);
        using var circleVisibleOpening = PathOp(circleBodyOpening, arms, SKPathOp.Difference);
        using var diamondVisibleOpening = PathOp(diamondBodyOpening, arms, SKPathOp.Difference);
        var openings = PathOp(circleVisibleOpening, diamondVisibleOpening, SKPathOp.Union);
        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            using var motorOpening = RadialCrescentModelCirclePath(bounds, x, y, 8);
            using var result = PathOp(openings, motorOpening, SKPathOp.Union);
            openings.Reset();
            openings.AddPath(result);
        }

        return openings;
    }

    private static void DrawRadialCrescentInteriorCuspMarks(SKCanvas canvas, SKRect bounds)
    {
        foreach (var point in RadialCrescentInteriorRoundingSites(bounds))
        {
            DrawSelectedCusp(canvas, point);
        }
    }

    private static void DrawRadialCrescentExteriorCuspMarks(SKCanvas canvas, SKRect bounds)
    {
        foreach (var point in RadialCrescentExteriorRoundingSites(bounds))
        {
            DrawSelectedCusp(canvas, point);
        }
    }

    private static void DrawSelectedCusp(SKCanvas canvas, SKPoint point)
    {
        var selectionColor = new SKColor(245, 22, 18);
        using var glow = FillPaint(selectionColor, 44);
        canvas.DrawCircle(point, 8.8f, glow);
        using var dot = FillPaint(selectionColor, 250);
        canvas.DrawCircle(point, 6.2f, dot);
    }

    private static IEnumerable<SKPoint> RadialCrescentInteriorRoundingSites(SKRect bounds) =>
        RadialCrescentGlossaryGeometry.InteriorCuspPointGroups()
            .SelectMany(group => group)
            .Select(point => RadialCrescentPoint(bounds, point.X, point.Y));

    private static IEnumerable<SKPoint> RadialCrescentExteriorRoundingSites(SKRect bounds) =>
    [
        RadialCrescentPoint(bounds, -165, 108),
        RadialCrescentPoint(bounds, -165, -108),
        RadialCrescentPoint(bounds, 25, -108),
        RadialCrescentPoint(bounds, 25, 108),
        RadialCrescentPoint(bounds, 173, 122),
        RadialCrescentPoint(bounds, 133, -88),
        RadialCrescentPoint(bounds, 357, 122),
        RadialCrescentPoint(bounds, 397, -88),
        RadialCrescentPoint(bounds, -70, 88),
        RadialCrescentPoint(bounds, -70, -88),
        RadialCrescentPoint(bounds, 265, 98),
        RadialCrescentPoint(bounds, 351, 0),
        RadialCrescentPoint(bounds, 265, -98),
        RadialCrescentPoint(bounds, 179, 0)
    ];

    private static void DrawRadialCrescentArcOffsets(SKCanvas canvas, SKRect bounds)
    {
        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var positive = RadialCrescentArcPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, radiusAdjust: 10f, trimDegrees: 3f);
            using var inverse = RadialCrescentArcPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, radiusAdjust: -10f, trimDegrees: 3f);
            DrawBridgePath(canvas, positive, PositiveStroke, 1.85f, glow: true);
            DrawBridgePath(canvas, inverse, InverseStroke, 1.35f, glow: false);
        }
    }

    private static void DrawRadialCrescentBentOffsets(SKCanvas canvas, SKRect bounds)
    {
        foreach (var polyline in RadialCrescentBentPolylines())
        {
            var positive = RadialCrescentOffsetModelPolyline(10f, polyline);
            var inverse = RadialCrescentOffsetModelPolyline(-10f, polyline);
            DrawModelPolyline(canvas, bounds, positive, PositiveStroke, 1.85f, 225, glow: true);
            DrawModelPolyline(canvas, bounds, inverse, InverseStroke, 1.35f, 195);
        }
    }

    private static void DrawRadialCrescentBodyOffsets(SKCanvas canvas, SKRect bounds)
    {
        DrawModelCircle(canvas, bounds, -70, 0, 88, PositiveStroke, 1.35f, 185);
        DrawModelCircle(canvas, bounds, -70, 0, 68, InverseStroke, 1.1f, 150);
        using var outerDiamond = RadialCrescentDiamondPath(bounds, 265, 0, 86, 98, rounded: false);
        using var innerDiamond = RadialCrescentDiamondPath(bounds, 265, 0, 56, 68, rounded: false);
        DrawBridgePath(canvas, outerDiamond, PositiveStroke, 1.35f, glow: false);
        DrawBridgePath(canvas, innerDiamond, InverseStroke, 1.1f, glow: false);
    }

    private static void DrawRadialCrescentOffsetWitnesses(SKCanvas canvas, SKRect bounds)
    {
        foreach (var (source, offset) in new[]
        {
            (RadialCrescentPoint(bounds, -165, 0), RadialCrescentPoint(bounds, -155, 0)),
            (RadialCrescentPoint(bounds, 25, 0), RadialCrescentPoint(bounds, 15, 0)),
            (RadialCrescentPoint(bounds, 210, 58), RadialCrescentPoint(bounds, 202, 64)),
            (RadialCrescentPoint(bounds, 320, 58), RadialCrescentPoint(bounds, 328, 64))
        })
        {
            DrawOffsetWitness(canvas, source, offset);
        }
    }

    private static void DrawRadialCrescentAreaOutlines(SKCanvas canvas, SKRect bounds, bool roundedExterior, SKColor color, float width, byte alpha)
    {
        if (roundedExterior)
        {
            using var crescent = RadialCrescentCrescentAreaPath(bounds, roundedExterior: true);
            using var bent = RadialCrescentBentAreaPath(bounds, roundedExterior: true);
            DrawBridgePath(canvas, crescent, color, width, glow: false);
            DrawBridgePath(canvas, bent, color, width, glow: false);
            return;
        }

        DrawModelCircle(canvas, bounds, -70, 0, 88, color, width, alpha);
        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var band = RadialCrescentArcBandPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, 10f);
            DrawBridgePath(canvas, band, color, width, glow: false);
        }

        foreach (var polyline in RadialCrescentBentPolylines())
        {
            using var band = RadialCrescentPolylineBandPath(bounds, 10f, polyline);
            DrawBridgePath(canvas, band, color, width, glow: false);
        }

        using var diamond = RadialCrescentDiamondPath(bounds, 265, 0, 86, 98, rounded: false);
        DrawBridgePath(canvas, diamond, color, width, glow: false);
        DrawRadialCrescentEndpointCircles(canvas, bounds, color, width, alpha, outer: true);
    }

    private static void DrawRadialCrescentInteriorVoids(SKCanvas canvas, SKRect bounds, SKColor color, float width, byte alpha, bool rounded)
    {
        DrawModelCircle(canvas, bounds, -70, 0, 68, color, width, alpha);
        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            DrawModelCircle(canvas, bounds, x, y, 8, color, width, alpha);
        }

        using var diamond = RadialCrescentDiamondPath(bounds, 265, 0, 56, 68, rounded);
        DrawBridgePath(canvas, diamond, color, width, glow: false);
    }

    private static void DrawRadialCrescentOutlinePartitions(SKCanvas canvas, SKRect bounds)
    {
        using var measurePath = RadialCrescentArcPath(bounds, -240, 0, -165, 108, -165, -108, radiusAdjust: 10f);
        DrawPathPartitions(canvas, measurePath, 4);
        using var rightPath = RadialCrescentArcPath(bounds, 100, 0, 25, -108, 25, 108, radiusAdjust: 10f);
        DrawPathPartitions(canvas, rightPath, 4);

        foreach (var polyline in RadialCrescentBentPolylines())
        {
            using var bandCenter = ModelPolylinePath(bounds, polyline);
            DrawPathPartitions(canvas, bandCenter, 3);
        }
    }

    private static SKPath RadialCrescentSettledAreaPath(SKRect bounds, bool roundedExterior, bool roundedInterior)
    {
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        using (var crescent = RadialCrescentCrescentAreaPath(bounds, roundedExterior))
        using (var bent = RadialCrescentBentAreaPath(bounds, roundedExterior))
        using (var holes = RadialCrescentInteriorVoidsPath(bounds, roundedInterior))
        {
            path.AddPath(crescent);
            path.AddPath(bent);
            path.AddPath(holes);
        }

        return path;
    }

    private static SKPath RadialCrescentCrescentAreaPath(SKRect bounds, bool roundedExterior)
    {
        var path = new SKPath();
        path.AddCircle(RadialCrescentPoint(bounds, -70, 0).X, RadialCrescentPoint(bounds, -70, 0).Y, RadialCrescentScale(bounds) * 88f);
        foreach (var arc in RadialCrescentArcSeeds())
        {
            using var band = RadialCrescentArcBandPath(bounds, arc.Cx, arc.Cy, arc.Sx, arc.Sy, arc.Ex, arc.Ey, roundedExterior ? 13f : 10f);
            path.AddPath(band);
        }

        foreach (var (x, y) in RadialCrescentEndpointModelCenters().Take(4))
        {
            path.AddCircle(RadialCrescentPoint(bounds, x, y).X, RadialCrescentPoint(bounds, x, y).Y, RadialCrescentScale(bounds) * 28f);
        }

        return path;
    }

    private static SKPath RadialCrescentBentAreaPath(SKRect bounds, bool roundedExterior)
    {
        var path = new SKPath();
        foreach (var polyline in RadialCrescentBentPolylines())
        {
            using var band = RadialCrescentPolylineBandPath(bounds, roundedExterior ? 13f : 10f, polyline);
            path.AddPath(band);
        }

        using (var diamond = RadialCrescentDiamondPath(bounds, 265, 0, roundedExterior ? 89 : 86, roundedExterior ? 101 : 98, roundedExterior))
        {
            path.AddPath(diamond);
        }

        foreach (var (x, y) in RadialCrescentEndpointModelCenters().Skip(4))
        {
            path.AddCircle(RadialCrescentPoint(bounds, x, y).X, RadialCrescentPoint(bounds, x, y).Y, RadialCrescentScale(bounds) * 28f);
        }

        return path;
    }

    private static SKPath RadialCrescentInteriorVoidsPath(SKRect bounds, bool rounded)
    {
        var path = new SKPath();
        path.AddCircle(RadialCrescentPoint(bounds, -70, 0).X, RadialCrescentPoint(bounds, -70, 0).Y, RadialCrescentScale(bounds) * 68f);
        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            path.AddCircle(RadialCrescentPoint(bounds, x, y).X, RadialCrescentPoint(bounds, x, y).Y, RadialCrescentScale(bounds) * 8f);
        }

        using (var diamond = RadialCrescentDiamondPath(bounds, 265, 0, 56, 68, rounded))
        {
            path.AddPath(diamond);
        }

        return path;
    }

    private static SKPath RadialCrescentCircleRingPath(SKRect bounds, float x, float y, float outerRadius, float innerRadius)
    {
        var center = RadialCrescentPoint(bounds, x, y);
        var scale = RadialCrescentScale(bounds);
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        path.AddCircle(center.X, center.Y, outerRadius * scale);
        path.AddCircle(center.X, center.Y, innerRadius * scale);
        return path;
    }

    private static SKPath RadialCrescentModelCirclePath(SKRect bounds, float x, float y, float radius)
    {
        var center = RadialCrescentPoint(bounds, x, y);
        var path = new SKPath();
        path.AddCircle(center.X, center.Y, radius * RadialCrescentScale(bounds));
        return path;
    }

    private static SKPath RadialCrescentDiamondRingPath(
        SKRect bounds,
        float centerX,
        float centerY,
        float outerHalfWidth,
        float outerHalfHeight,
        float innerHalfWidth,
        float innerHalfHeight,
        bool rounded)
    {
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        using var outer = RadialCrescentDiamondPath(bounds, centerX, centerY, outerHalfWidth, outerHalfHeight, rounded);
        using var inner = RadialCrescentDiamondPath(bounds, centerX, centerY, innerHalfWidth, innerHalfHeight, rounded);
        path.AddPath(outer);
        path.AddPath(inner);
        return path;
    }

    private static SKPath PathOp(SKPath left, SKPath right, SKPathOp operation)
    {
        var result = new SKPath();
        if (left.Op(right, operation, result))
        {
            return result;
        }

        result.Dispose();
        var fallback = new SKPath();
        fallback.AddPath(left);
        if (operation == SKPathOp.Union)
        {
            fallback.AddPath(right);
        }

        return fallback;
    }

    private static SKPath RadialCrescentArcBandPath(
        SKRect bounds,
        float cx,
        float cy,
        float sx,
        float sy,
        float ex,
        float ey,
        float distance)
    {
        var outer = RadialCrescentArcModelPoints(cx, cy, sx, sy, ex, ey, distance, 0, 24);
        var inner = RadialCrescentArcModelPoints(cx, cy, sx, sy, ex, ey, -distance, 0, 24).Reverse().ToArray();
        var path = new SKPath();
        var first = RadialCrescentPoint(bounds, outer[0].X, outer[0].Y);
        path.MoveTo(first);
        foreach (var point in outer.Skip(1).Concat(inner))
        {
            path.LineTo(RadialCrescentPoint(bounds, point.X, point.Y));
        }

        path.Close();
        return path;
    }

    private static SKPath RadialCrescentPolylineBandPath(SKRect bounds, float distance, IReadOnlyList<(float X, float Y)> points)
    {
        var positive = RadialCrescentOffsetModelPolyline(distance, points);
        var inverse = RadialCrescentOffsetModelPolyline(-distance, points).Reverse().ToArray();
        var path = new SKPath();
        path.MoveTo(RadialCrescentPoint(bounds, positive[0].X, positive[0].Y));
        foreach (var point in positive.Skip(1).Concat(inverse))
        {
            path.LineTo(RadialCrescentPoint(bounds, point.X, point.Y));
        }

        path.Close();
        return path;
    }

    private static SKPath RadialCrescentArcPath(
        SKRect bounds,
        float cx,
        float cy,
        float sx,
        float sy,
        float ex,
        float ey,
        float radiusAdjust = 0,
        float trimDegrees = 0)
    {
        var points = RadialCrescentArcModelPoints(cx, cy, sx, sy, ex, ey, radiusAdjust, trimDegrees, 28);
        var path = new SKPath();
        path.MoveTo(RadialCrescentPoint(bounds, points[0].X, points[0].Y));
        foreach (var point in points.Skip(1))
        {
            path.LineTo(RadialCrescentPoint(bounds, point.X, point.Y));
        }

        return path;
    }

    private static (float X, float Y)[] RadialCrescentArcModelPoints(
        float cx,
        float cy,
        float sx,
        float sy,
        float ex,
        float ey,
        float radiusAdjust,
        float trimDegrees,
        int samples)
    {
        var startAngle = MathF.Atan2(sy - cy, sx - cx) * 180f / MathF.PI;
        var endAngle = MathF.Atan2(ey - cy, ex - cx) * 180f / MathF.PI;
        var sweep = ShortestSweepDegrees(startAngle, endAngle);
        if (trimDegrees > 0 &&
            MathF.Abs(sweep) > trimDegrees * 2f)
        {
            var direction = MathF.Sign(sweep);
            startAngle += direction * trimDegrees;
            sweep -= direction * trimDegrees * 2f;
        }

        var radius = MathF.Sqrt((sx - cx) * (sx - cx) + (sy - cy) * (sy - cy)) + radiusAdjust;
        var result = new (float X, float Y)[samples + 1];
        for (var index = 0; index <= samples; index++)
        {
            var angle = (startAngle + sweep * index / samples) * MathF.PI / 180f;
            result[index] = (cx + MathF.Cos(angle) * radius, cy + MathF.Sin(angle) * radius);
        }

        return result;
    }

    private static SKPath RadialCrescentDiamondPath(SKRect bounds, float centerX, float centerY, float halfWidth, float halfHeight, bool rounded)
    {
        var top = RadialCrescentPoint(bounds, centerX, centerY + halfHeight);
        var right = RadialCrescentPoint(bounds, centerX + halfWidth, centerY);
        var bottom = RadialCrescentPoint(bounds, centerX, centerY - halfHeight);
        var left = RadialCrescentPoint(bounds, centerX - halfWidth, centerY);
        var path = new SKPath();
        path.MoveTo(top);
        if (!rounded)
        {
            path.LineTo(right);
            path.LineTo(bottom);
            path.LineTo(left);
            path.Close();
            return path;
        }

        var scale = RadialCrescentScale(bounds);
        var pull = 10f * scale;
        path.QuadTo(new SKPoint(right.X - pull, right.Y - pull), right);
        path.QuadTo(new SKPoint(right.X - pull, right.Y + pull), bottom);
        path.QuadTo(new SKPoint(left.X + pull, left.Y + pull), left);
        path.QuadTo(new SKPoint(left.X + pull, left.Y - pull), top);
        path.Close();
        return path;
    }

    private static SKPath ModelPolylinePath(SKRect bounds, IReadOnlyList<(float X, float Y)> points)
    {
        var path = new SKPath();
        path.MoveTo(RadialCrescentPoint(bounds, points[0].X, points[0].Y));
        foreach (var point in points.Skip(1))
        {
            path.LineTo(RadialCrescentPoint(bounds, point.X, point.Y));
        }

        return path;
    }

    private static void DrawModelPolyline(SKCanvas canvas, SKRect bounds, IReadOnlyList<(float X, float Y)> points, SKColor color, float width, byte alpha, bool glow = false)
    {
        using var path = ModelPolylinePath(bounds, points);
        DrawBridgePath(canvas, path, color, width, glow);
    }

    private static void DrawModelSegment(
        SKCanvas canvas,
        SKRect bounds,
        (float X, float Y) start,
        (float X, float Y) end,
        SKColor color,
        float width,
        byte alpha,
        float trimStart = 0,
        float trimEnd = 0)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length <= 0.001f)
        {
            return;
        }

        var sx = start.X + dx / length * trimStart;
        var sy = start.Y + dy / length * trimStart;
        var ex = end.X - dx / length * trimEnd;
        var ey = end.Y - dy / length * trimEnd;
        DrawLine(canvas, RadialCrescentPoint(bounds, sx, sy), RadialCrescentPoint(bounds, ex, ey), color, width, alpha);
    }

    private static void DrawModelCircle(SKCanvas canvas, SKRect bounds, float x, float y, float radius, SKColor color, float width, byte alpha)
    {
        using var stroke = StrokePaint(color, width, alpha);
        canvas.DrawCircle(RadialCrescentPoint(bounds, x, y), radius * RadialCrescentScale(bounds), stroke);
    }

    private static (float X, float Y)[] RadialCrescentOffsetModelPolyline(float distance, IReadOnlyList<(float X, float Y)> points)
    {
        var result = new (float X, float Y)[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            if (index == 0)
            {
                var normal = SegmentNormal(points[0], points[1], distance);
                result[index] = (points[index].X + normal.X, points[index].Y + normal.Y);
                continue;
            }

            if (index == points.Count - 1)
            {
                var normal = SegmentNormal(points[index - 1], points[index], distance);
                result[index] = (points[index].X + normal.X, points[index].Y + normal.Y);
                continue;
            }

            var previousNormal = SegmentNormal(points[index - 1], points[index], distance);
            var nextNormal = SegmentNormal(points[index], points[index + 1], distance);
            var previousA = (points[index - 1].X + previousNormal.X, points[index - 1].Y + previousNormal.Y);
            var previousB = (points[index].X + previousNormal.X, points[index].Y + previousNormal.Y);
            var nextA = (points[index].X + nextNormal.X, points[index].Y + nextNormal.Y);
            var nextB = (points[index + 1].X + nextNormal.X, points[index + 1].Y + nextNormal.Y);
            result[index] = TryModelLineIntersection(previousA, previousB, nextA, nextB, out var intersection)
                ? intersection
                : (points[index].X + (previousNormal.X + nextNormal.X) * 0.5f, points[index].Y + (previousNormal.Y + nextNormal.Y) * 0.5f);
        }

        return result;
    }

    private static (float X, float Y) SegmentNormal((float X, float Y) start, (float X, float Y) end, float distance)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        return length <= 0.001f
            ? (0, 0)
            : (-dy / length * distance, dx / length * distance);
    }

    private static bool TryModelLineIntersection(
        (float X, float Y) a0,
        (float X, float Y) a1,
        (float X, float Y) b0,
        (float X, float Y) b1,
        out (float X, float Y) intersection)
    {
        var ax = a1.X - a0.X;
        var ay = a1.Y - a0.Y;
        var bx = b1.X - b0.X;
        var by = b1.Y - b0.Y;
        var denominator = ax * by - ay * bx;
        if (MathF.Abs(denominator) <= 0.001f)
        {
            intersection = default;
            return false;
        }

        var cx = b0.X - a0.X;
        var cy = b0.Y - a0.Y;
        var t = (cx * by - cy * bx) / denominator;
        intersection = (a0.X + ax * t, a0.Y + ay * t);
        return true;
    }

    private static float ShortestSweepDegrees(float start, float end)
    {
        var sweep = end - start;
        while (sweep <= -180f)
        {
            sweep += 360f;
        }

        while (sweep > 180f)
        {
            sweep -= 360f;
        }

        return sweep;
    }

    private static IEnumerable<SKPoint> RadialCrescentSourceSites(SKRect bounds)
    {
        foreach (var (x, y) in RadialCrescentEndpointModelCenters())
        {
            yield return RadialCrescentPoint(bounds, x, y);
        }

        foreach (var (x, y) in new[] { (247f, -6f), (283f, -6f), (-70f, 0f), (265f, 0f) })
        {
            yield return RadialCrescentPoint(bounds, x, y);
        }
    }

    private static IEnumerable<SKPoint> RadialCrescentEndpointCenters(SKRect bounds) =>
        RadialCrescentEndpointModelCenters().Select(point => RadialCrescentPoint(bounds, point.X, point.Y));

    private static IReadOnlyList<(float X, float Y)> RadialCrescentEndpointModelCenters() =>
    [
        (-165, 108),
        (-165, -108),
        (25, -108),
        (25, 108),
        (173, 122),
        (133, -88),
        (357, 122),
        (397, -88)
    ];

    private static IReadOnlyList<(float Cx, float Cy, float Sx, float Sy, float Ex, float Ey)> RadialCrescentArcSeeds() =>
    [
        (-240, 0, -165, 108, -165, -108),
        (100, 0, 25, -108, 25, 108)
    ];

    private static IReadOnlyList<(float X, float Y)[]> RadialCrescentBentPolylines() =>
    [
        [(173, 122), (247, -6), (133, -88)],
        [(357, 122), (283, -6), (397, -88)]
    ];

    private static IEnumerable<SKPoint> RadialCrescentInteriorCuspSites(SKRect bounds) =>
    [
        RadialCrescentPoint(bounds, 265, 68),
        RadialCrescentPoint(bounds, 321, 0),
        RadialCrescentPoint(bounds, 265, -68),
        RadialCrescentPoint(bounds, 209, 0)
    ];

    private static IEnumerable<SKPoint> RadialCrescentExteriorCuspSites(SKRect bounds) =>
    [
        RadialCrescentPoint(bounds, -165, 108),
        RadialCrescentPoint(bounds, -165, -108),
        RadialCrescentPoint(bounds, 25, -108),
        RadialCrescentPoint(bounds, 25, 108),
        RadialCrescentPoint(bounds, 173, 122),
        RadialCrescentPoint(bounds, 133, -88),
        RadialCrescentPoint(bounds, 357, 122),
        RadialCrescentPoint(bounds, 397, -88)
    ];

    private static SKPath DiamondCuspSourcePath(SKRect bounds)
        => DiamondCuspPathFromSections(bounds, DiamondCuspGlossaryGeometry.CreateSource().Sections);

    private static SKPath DiamondCuspRoundedPath(SKRect bounds)
        => DiamondCuspPathFromSections(bounds, DiamondCuspGlossaryGeometry.CreateRounded().Sections);

    private static SKPath DiamondInnerSourcePath(SKRect bounds)
        => DiamondCuspPathFromSections(bounds, DiamondCuspGlossaryGeometry.CreateInnerSource().Sections);

    private static SKPath DiamondInnerRoundedPath(SKRect bounds)
        => DiamondCuspPathFromSections(bounds, DiamondCuspGlossaryGeometry.CreateInnerRounded().Sections);

    private static IEnumerable<SKPath> DiamondCuspRoundedCornerPaths(SKRect bounds)
        => DiamondRoundedCornerPaths(bounds, DiamondCuspGlossaryGeometry.CreateRounded().Sections);

    private static IEnumerable<SKPath> DiamondInnerRoundedCornerPaths(SKRect bounds)
        => DiamondRoundedCornerPaths(bounds, DiamondCuspGlossaryGeometry.CreateInnerRounded().Sections);

    private static IEnumerable<SKPath> DiamondRoundedCornerPaths(SKRect bounds, IReadOnlyList<IElement> sections)
    {
        foreach (var section in sections)
        {
            var curve = GraphicReadGraph.From(section)
                .ReadsOf<SharedFrameQuadraticCurveRead>()
                .FirstOrDefault();
            if (curve is null)
            {
                continue;
            }

            var path = new SKPath();
            path.MoveTo(DiamondPoint(bounds, curve.Start));
            path.QuadTo(DiamondPoint(bounds, curve.Control), DiamondPoint(bounds, curve.End));
            yield return path;
        }
    }

    private static SKPath DiamondCuspPathFromSections(SKRect bounds, IReadOnlyList<IElement> sections)
    {
        var path = new SKPath();
        var hasStarted = false;
        foreach (var section in sections)
        {
            AppendDiamondCuspSection(path, bounds, section, ref hasStarted);
        }

        if (hasStarted)
        {
            path.Close();
        }

        return path;
    }

    private static void AppendDiamondCuspSection(SKPath path, SKRect bounds, IElement section, ref bool hasStarted)
    {
        var graph = GraphicReadGraph.From(section);
        var curve = graph.ReadsOf<SharedFrameQuadraticCurveRead>().FirstOrDefault();
        if (curve is not null)
        {
            MoveOrLineTo(path, DiamondPoint(bounds, curve.Start), ref hasStarted);
            path.QuadTo(DiamondPoint(bounds, curve.Control), DiamondPoint(bounds, curve.End));
            return;
        }

        var arc = graph.ReadsOf<SharedFrameArcRead>().FirstOrDefault();
        if (arc is not null)
        {
            MoveOrLineTo(path, DiamondPoint(bounds, arc.Start), ref hasStarted);
            AddDiamondArc(path, bounds, arc);
            return;
        }

        var segment = graph.ReadsOf<SharedFrameSegmentRead>().FirstOrDefault();
        if (segment is not null)
        {
            MoveOrLineTo(path, DiamondPoint(bounds, segment.Start), ref hasStarted);
            path.LineTo(DiamondPoint(bounds, segment.End));
        }
    }

    private static void MoveOrLineTo(SKPath path, SKPoint point, ref bool hasStarted)
    {
        if (!hasStarted)
        {
            path.MoveTo(point);
            hasStarted = true;
            return;
        }

        path.LineTo(point);
    }

    private static void DrawDiamondSharedPoints(SKCanvas canvas, SKRect bounds, SKColor color, float radius, byte alpha)
    {
        using var fill = FillPaint(color, alpha);
        foreach (var point in DiamondCuspPoints(bounds))
        {
            canvas.DrawCircle(point, radius, fill);
        }
    }

    private static void DrawDiamondAngleBasis(SKCanvas canvas, SKRect bounds, byte alpha)
    {
        using var stroke = StrokePaint(BoundaryMarkerColor, 0.9f, alpha);
        foreach (var point in DiamondCuspPoints(bounds))
        {
            canvas.DrawCircle(point, 12.5f, stroke);
            DrawLine(canvas, point, new SKPoint(point.X + 11.8f, point.Y), BoundaryMarkerColor, 0.65f, alpha);
            DrawLine(canvas, point, new SKPoint(point.X, point.Y - 11.8f), BoundaryMarkerColor, 0.65f, alpha);
        }
    }

    private static void DrawDiamondSelectionPoint(SKCanvas canvas, SKPoint point)
    {
        using var glow = FillPaint(EndpointColor, 54);
        canvas.DrawCircle(point, 8.5f, glow);
        using var fill = FillPaint(EndpointColor, 245);
        canvas.DrawCircle(point, 5.5f, fill);
    }

    private static IEnumerable<SKPoint> DiamondCuspPoints(SKRect bounds) =>
        DiamondCuspGlossaryGeometry.CreateSource().Cusps.Select(point => DiamondPoint(bounds, point));

    private static IEnumerable<SKPoint> DiamondInnerCuspPoints(SKRect bounds) =>
        DiamondCuspGlossaryGeometry.CreateInnerSource().Cusps.Select(point => DiamondPoint(bounds, point));

    private static SKPoint DiamondPoint(SKRect bounds, DiamondCuspGlossaryGeometry.ModelPoint point) =>
        DiamondPoint(bounds, point.X, point.Y);

    private static SKPoint DiamondPoint(SKRect bounds, Relation point)
    {
        var framePoint = GraphicReadGraph.From(point)
            .ReadsOf<SharedFramePointRead>()
            .FirstOrDefault();
        return framePoint is null
            ? DiamondPoint(bounds, 0, 0)
            : DiamondPoint(bounds, framePoint);
    }

    private static SKPoint DiamondPoint(SKRect bounds, SharedFramePointRead point) =>
        DiamondPoint(bounds, DiamondModelPoint(point));

    private static SKPoint DiamondPoint(SKRect bounds, long x, long y)
    {
        var scale = DiamondScale(bounds);
        return new SKPoint(bounds.MidX + x * scale, bounds.MidY - y * scale);
    }

    private static float DiamondScale(SKRect bounds) =>
        Math.Min(bounds.Width / 210f, bounds.Height / 210f) * 0.9f;

    private static DiamondCuspGlossaryGeometry.ModelPoint DiamondModelPoint(SharedFramePointRead point)
    {
        var x = point.Coordinates.Count > 0 ? point.Coordinates[0].Ticks : 0;
        var y = point.Coordinates.Count > 1 ? point.Coordinates[1].Ticks : 0;
        return new DiamondCuspGlossaryGeometry.ModelPoint(x, y);
    }

    private static void AddDiamondArc(
        SKPath path,
        SKRect bounds,
        SharedFrameArcRead arc)
    {
        var center = DiamondModelPoint(arc.Center);
        var start = DiamondModelPoint(arc.Start);
        var end = DiamondModelPoint(arc.End);
        var scale = DiamondScale(bounds);
        var centerPoint = DiamondPoint(bounds, center);
        var radius = MathF.Sqrt(
            (start.X - center.X) * (start.X - center.X) +
            (start.Y - center.Y) * (start.Y - center.Y)) * scale;
        var startAngle = MathF.Atan2(start.Y - center.Y, start.X - center.X);
        var endAngle = MathF.Atan2(end.Y - center.Y, end.X - center.X);
        var sweep = ShortestSweepRadians(startAngle, endAngle);
        var screenStartDegrees = -startAngle * 180f / MathF.PI;
        var screenSweepDegrees = -sweep * 180f / MathF.PI;
        var oval = new SKRect(
            centerPoint.X - radius,
            centerPoint.Y - radius,
            centerPoint.X + radius,
            centerPoint.Y + radius);
        path.ArcTo(oval, screenStartDegrees, screenSweepDegrees, forceMoveTo: false);
    }

    private static float ShortestSweepRadians(float start, float end)
    {
        var sweep = end - start;
        while (sweep <= -MathF.PI)
        {
            sweep += MathF.PI * 2f;
        }

        while (sweep > MathF.PI)
        {
            sweep -= MathF.PI * 2f;
        }

        return sweep;
    }

    private static SKPoint RadialCrescentPoint(SKRect bounds, float x, float y)
    {
        var scale = RadialCrescentScale(bounds);
        return new SKPoint(bounds.MidX + (x - 80f) * scale, bounds.MidY - y * scale);
    }

    private static float RadialCrescentScale(SKRect bounds) =>
        Math.Min(bounds.Width / 720f, bounds.Height / 330f) * 0.96f;

    private static SKPath PolyPath(params SKPoint[] points)
    {
        var path = new SKPath();
        if (points.Length == 0)
        {
            return path;
        }

        path.MoveTo(points[0]);
        for (var index = 1; index < points.Length; index++)
        {
            path.LineTo(points[index]);
        }

        return path;
    }

    private static SKPath CubicPath(SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3)
    {
        var path = new SKPath();
        path.MoveTo(p0);
        path.CubicTo(p1.X, p1.Y, p2.X, p2.Y, p3.X, p3.Y);
        return path;
    }

    private static SKPath OffsetPolyline(float distance, params SKPoint[] points)
    {
        var path = new SKPath();
        if (points.Length == 0)
        {
            return path;
        }

        var offsetPoints = new SKPoint[points.Length];
        if (points.Length == 1)
        {
            offsetPoints[0] = points[0];
        }
        else
        {
            offsetPoints[0] = OffsetLinePoint(points[0], points[1], distance);
            offsetPoints[^1] = OffsetLinePoint(points[^2], points[^1], distance, endPoint: true);
            for (var index = 1; index < points.Length - 1; index++)
            {
                var prevA = OffsetLinePoint(points[index - 1], points[index], distance);
                var prevB = OffsetLinePoint(points[index - 1], points[index], distance, endPoint: true);
                var nextA = OffsetLinePoint(points[index], points[index + 1], distance);
                var nextB = OffsetLinePoint(points[index], points[index + 1], distance, endPoint: true);
                offsetPoints[index] = TryLineIntersection(prevA, prevB, nextA, nextB, out var intersection)
                    ? intersection
                    : OffsetLinePoint(points[index - 1], points[index + 1], distance);
            }
        }

        return PolyPath(offsetPoints);
    }

    private static SKPath OffsetCubicPath(float distance, SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3)
    {
        const int Samples = 28;
        var offsetPoints = new SKPoint[Samples + 1];
        for (var index = 0; index <= Samples; index++)
        {
            var t = index / (float)Samples;
            var point = CubicPoint(p0, p1, p2, p3, t);
            var tangent = Normalize(CubicTangent(p0, p1, p2, p3, t));
            var normal = new SKPoint(-tangent.Y, tangent.X);
            offsetPoints[index] = new SKPoint(point.X + normal.X * distance, point.Y + normal.Y * distance);
        }

        return SmoothPathThrough(offsetPoints);
    }

    private static SKPath SmoothPathThrough(IReadOnlyList<SKPoint> points)
    {
        var path = new SKPath();
        if (points.Count == 0)
        {
            return path;
        }

        path.MoveTo(points[0]);
        for (var index = 1; index < points.Count; index++)
        {
            path.LineTo(points[index]);
        }

        return path;
    }

    private static SKPoint OffsetLinePoint(SKPoint start, SKPoint end, float distance, bool endPoint = false)
    {
        var direction = Normalize(new SKPoint(end.X - start.X, end.Y - start.Y));
        var normal = new SKPoint(-direction.Y, direction.X);
        var point = endPoint ? end : start;
        return new SKPoint(point.X + normal.X * distance, point.Y + normal.Y * distance);
    }

    private static bool TryLineIntersection(SKPoint a, SKPoint b, SKPoint c, SKPoint d, out SKPoint intersection)
    {
        var abx = b.X - a.X;
        var aby = b.Y - a.Y;
        var cdx = d.X - c.X;
        var cdy = d.Y - c.Y;
        var denominator = abx * cdy - aby * cdx;
        if (MathF.Abs(denominator) <= 0.001f)
        {
            intersection = default;
            return false;
        }

        var acx = c.X - a.X;
        var acy = c.Y - a.Y;
        var t = (acx * cdy - acy * cdx) / denominator;
        intersection = new SKPoint(a.X + abx * t, a.Y + aby * t);
        return true;
    }

    private static void DrawOffsetWitnesses(SKCanvas canvas, SKPath source, SKPath offset, int count)
    {
        using var sourceMeasure = new SKPathMeasure(source, false);
        using var offsetMeasure = new SKPathMeasure(offset, false);
        if (sourceMeasure.Length <= 0.001f ||
            offsetMeasure.Length <= 0.001f)
        {
            return;
        }

        for (var index = 1; index <= count; index++)
        {
            var fraction = index / (float)(count + 1);
            if (sourceMeasure.GetPositionAndTangent(sourceMeasure.Length * fraction, out var sourcePoint, out _) &&
                offsetMeasure.GetPositionAndTangent(offsetMeasure.Length * fraction, out var offsetPoint, out _))
            {
                DrawOffsetWitness(canvas, sourcePoint, offsetPoint);
            }
        }
    }

    private static void DrawPolylineOffsetWitnesses(SKCanvas canvas, float distance, params SKPoint[] points)
    {
        for (var index = 0; index < points.Length - 1; index++)
        {
            var start = points[index];
            var end = points[index + 1];
            var fraction = index == 0 ? 0.58f : 0.42f;
            var sourcePoint = new SKPoint(
                start.X + (end.X - start.X) * fraction,
                start.Y + (end.Y - start.Y) * fraction);
            var direction = Normalize(new SKPoint(end.X - start.X, end.Y - start.Y));
            var normal = new SKPoint(-direction.Y, direction.X);
            var offsetPoint = new SKPoint(
                sourcePoint.X + normal.X * distance,
                sourcePoint.Y + normal.Y * distance);
            DrawOffsetWitness(canvas, sourcePoint, offsetPoint);
        }
    }

    private static void DrawCubicOffsetWitnesses(SKCanvas canvas, SKPoint p0, SKPoint p1, SKPoint p2, SKPoint p3, float distance, int count)
    {
        for (var index = 1; index <= count; index++)
        {
            var t = index / (float)(count + 1);
            var sourcePoint = CubicPoint(p0, p1, p2, p3, t);
            var tangent = Normalize(CubicTangent(p0, p1, p2, p3, t));
            var normal = new SKPoint(-tangent.Y, tangent.X);
            var offsetPoint = new SKPoint(sourcePoint.X + normal.X * distance, sourcePoint.Y + normal.Y * distance);
            DrawOffsetWitness(canvas, sourcePoint, offsetPoint);
        }
    }

    private static SKPath RoundedRectPath(SKRect rect, float radius)
    {
        var path = new SKPath();
        path.AddRoundRect(rect, radius, radius);
        return path;
    }

    private static void DrawOffsetWitness(SKCanvas canvas, SKPoint source, SKPoint derived)
    {
        DrawDashedLine(canvas, source, derived, EvidenceColor, 1f);
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
        var steps = 24;
        for (var index = 1; index <= steps; index++)
        {
            var t = index / (float)steps;
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
