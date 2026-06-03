using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using Gojo.Core;
using GojoDraw;
using SkiaSharp;
using System.Threading;

namespace GojoDraw.Visualizer;

internal sealed class VisualizerCanvas : Control
{
    internal static readonly IReadOnlyList<VisualizerLegendPage> LegendPages = VisualizerSample.CreateLegendPages();

    private VisualizerDocument? document;
    private GojoCadSkiaHitArea[] structureHitAreas = [];
    private VisualizerLegendHitArea[] legendHitAreas = [];
    private VisualizerHover? hover;
    private SKPoint hoverPoint;
    private int activeLegendPageIndex;

    public VisualizerCanvas()
    {
        ClipToBounds = true;
    }

    public event EventHandler? DocumentChanged;

    public VisualizerDocument? Document
    {
        get => document;
        set
        {
            document = value;
            hover = null;
            Volatile.Write(ref structureHitAreas, []);
            Volatile.Write(ref legendHitAreas, []);
            InvalidateVisual();
            DocumentChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Document is null)
        {
            return;
        }

        context.Custom(new VisualizerDrawOperation(
            new Rect(Bounds.Size),
            Document,
            PublishStructureHitAreas,
            PublishLegendHitAreas,
            hover,
            hoverPoint,
            activeLegendPageIndex));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var position = e.GetPosition(this);
        hoverPoint = new SKPoint((float)position.X, (float)position.Y);
        var next = ReadHitAt(hoverPoint);
        if (!Equals(next, hover))
        {
            hover = next;
            InvalidateVisual();
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Document is null ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var position = e.GetPosition(this);
        var hit = ReadHitAt(new SKPoint((float)position.X, (float)position.Y));
        if (hit?.LegendIndex is int legendIndex)
        {
            var entries = ReadActiveLegendEntries();
            if (legendIndex < 0 || legendIndex >= entries.Count)
            {
                return;
            }

            var entry = entries[legendIndex];
            if (Document.CanApply(entry))
            {
                Document = Document.Apply(entry);
            }

            e.Handled = true;
            return;
        }

        if (hit?.LegendPageIndex is int legendPageIndex)
        {
            activeLegendPageIndex = Math.Clamp(legendPageIndex, 0, Math.Max(0, LegendPages.Count - 1));
            hover = null;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        Document = hit?.Source is null
            ? Document with { SelectedIndex = null }
            : Document.SelectSource(hit.Source);
        e.Handled = true;
    }

    private IReadOnlyList<VisualizerLegendEntry> ReadActiveLegendEntries()
    {
        var pageIndex = Math.Clamp(activeLegendPageIndex, 0, Math.Max(0, LegendPages.Count - 1));
        return LegendPages.Count == 0
            ? []
            : LegendPages[pageIndex].Entries;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (hover is not null)
        {
            hover = null;
            InvalidateVisual();
        }
    }

    private VisualizerHover? ReadHitAt(SKPoint point)
    {
        var legend = Volatile.Read(ref legendHitAreas)
            .Where(area => area.Contains(point))
            .OrderByDescending(area => area.Priority)
            .FirstOrDefault();
        if (legend is not null)
        {
            return new VisualizerHover(
                legend.Bounds,
                legend.Text,
                legend.Priority,
                Source: null,
                LegendIndex: legend.EntryIndex,
                LegendPageIndex: legend.PageIndex);
        }

        var structure = Volatile.Read(ref structureHitAreas)
            .Where(area => area.Contains(point))
            .OrderByDescending(area => area.Priority)
            .FirstOrDefault();
        return structure is null
            ? null
            : new VisualizerHover(
                structure.Bounds,
                structure.Text,
                structure.Priority,
                structure.Source,
                LegendIndex: null,
                LegendPageIndex: null);
    }

    private void PublishStructureHitAreas(GojoCadSkiaHitArea[] nextHitAreas)
    {
        Volatile.Write(ref structureHitAreas, nextHitAreas);
    }

    private void PublishLegendHitAreas(VisualizerLegendHitArea[] nextHitAreas)
    {
        Volatile.Write(ref legendHitAreas, nextHitAreas);
    }
}

internal sealed class VisualizerDrawOperation : ICustomDrawOperation
{
    private const float LegendWidth = 286f;
    private const float LegendGap = 10f;
    private const float LegendTopPadding = 62f;
    private const float LegendCellMinHeight = 46f;
    private const int LegendColumns = 2;

    private static readonly GojoCadSkiaAdapter Adapter = new();
    private readonly VisualizerDocument document;
    private readonly Action<GojoCadSkiaHitArea[]> publishStructureHitAreas;
    private readonly Action<VisualizerLegendHitArea[]> publishLegendHitAreas;
    private readonly VisualizerHover? hover;
    private readonly SKPoint hoverPoint;
    private readonly int activeLegendPageIndex;

    public VisualizerDrawOperation(
        Rect bounds,
        VisualizerDocument document,
        Action<GojoCadSkiaHitArea[]> publishStructureHitAreas,
        Action<VisualizerLegendHitArea[]> publishLegendHitAreas,
        VisualizerHover? hover,
        SKPoint hoverPoint,
        int activeLegendPageIndex)
    {
        Bounds = bounds;
        this.document = document;
        this.publishStructureHitAreas = publishStructureHitAreas;
        this.publishLegendHitAreas = publishLegendHitAreas;
        this.hover = hover;
        this.hoverPoint = hoverPoint;
        this.activeLegendPageIndex = activeLegendPageIndex;
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
        canvas.Save();
        canvas.Clear(new SKColor(249, 248, 244));
        canvas.ClipRect(new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height));
        DrawLegend(canvas);
        DrawSample(canvas);
        DrawHover(canvas);
        canvas.Restore();
    }

    private void DrawSample(SKCanvas canvas)
    {
        if (document.DemoPage is { } demoPage)
        {
            DrawDemoPage(canvas, demoPage);
            return;
        }

        using var paints = new GojoCadSkiaPaints();
        var viewport = ReadSampleViewport();
        var projection = new DerivedSkiaProjection(
            document.Structure,
            viewport,
            document.ProjectionSeed,
            ReadStableModelBounds());
        var hitAreas = new List<GojoCadSkiaHitArea>();
        Adapter.DrawElement(canvas, document.Structure, projection, paints, hitAreas);
        DrawSelection(canvas, hitAreas);
        publishStructureHitAreas(hitAreas.ToArray());
    }

    private void DrawDemoPage(SKCanvas canvas, VisualizerDemoPage demoPage)
    {
        var viewport = ReadSampleViewport();
        var hitAreas = new List<GojoCadSkiaHitArea>();
        using var paints = CreateDemoPaints();
        using var panelFill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 254, 250, 210),
            Style = SKPaintStyle.Fill
        };
        using var panelStroke = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(84, 91, 102, 72),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke
        };
        using var title = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(31, 37, 45, 225),
            TextSize = 13f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };
        using var caption = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(57, 64, 74, 195),
            TextSize = 10.5f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            Style = SKPaintStyle.Fill
        };

        DrawTextFitted(
            canvas,
            demoPage.Label,
            viewport.Left + 18f,
            viewport.Top + 27f,
            viewport.Width - 36f,
            title,
            10f);

        var content = new SKRect(
            viewport.Left + 16f,
            viewport.Top + 42f,
            viewport.Right - 16f,
            viewport.Bottom - 16f);
        var gap = 16f;
        var panelWidth = (content.Width - gap) * 0.5f;
        var panelHeight = (content.Height - gap) * 0.5f;

        for (var index = 0; index < demoPage.Panels.Count; index++)
        {
            var panel = demoPage.Panels[index];
            var column = index % 2;
            var row = index / 2;
            var bounds = new SKRect(
                content.Left + column * (panelWidth + gap),
                content.Top + row * (panelHeight + gap),
                content.Left + column * (panelWidth + gap) + panelWidth,
                content.Top + row * (panelHeight + gap) + panelHeight);

            canvas.DrawRoundRect(bounds, 6f, 6f, panelFill);
            canvas.DrawRoundRect(bounds, 6f, 6f, panelStroke);
            DrawDemoPanel(canvas, panel, bounds, paints, title, caption, hitAreas);
        }

        publishStructureHitAreas(hitAreas.ToArray());
    }

    private static void DrawDemoPanel(
        SKCanvas canvas,
        VisualizerDemoPanel panel,
        SKRect bounds,
        GojoCadSkiaPaints paints,
        SKPaint title,
        SKPaint caption,
        ICollection<GojoCadSkiaHitArea> hitAreas)
    {
        var titleBaseline = bounds.Top + 21f;
        DrawTextFitted(
            canvas,
            panel.Title,
            bounds.Left + 12f,
            titleBaseline,
            bounds.Width - 24f,
            title,
            9f);

        var drawingBounds = new SKRect(
            bounds.Left + 12f,
            bounds.Top + 31f,
            bounds.Right - 12f,
            bounds.Bottom - 46f);
        canvas.Save();
        canvas.ClipRect(drawingBounds);
        var panelStructure = Chain(panel.Elements
            .Select(element => element.Element)
            .ToArray());
        var projection = new DerivedSkiaProjection(
            panelStructure,
            drawingBounds,
            panel.ProjectionSeed,
            panel.ModelBounds);
        foreach (var element in panel.Elements)
        {
            using var elementPaints = element.Tint.HasValue
                ? CreateDemoPaints(element.Tint.Value)
                : null;
            Adapter.DrawElement(canvas, element.Element, projection, elementPaints ?? paints, hitAreas);
        }

        DrawDemoLabels(canvas, panel, drawingBounds);
        canvas.Restore();

        if (!string.IsNullOrWhiteSpace(panel.ResidueText))
        {
            DrawResidueBadge(canvas, panel.ResidueText, bounds);
        }

        DrawWrappedText(
            canvas,
            panel.Caption,
            new SKRect(bounds.Left + 12f, bounds.Bottom - 38f, bounds.Right - 12f, bounds.Bottom - 8f),
            caption,
            lineHeight: 13f,
            maxLines: 2);
        hitAreas.Add(new GojoCadSkiaHitArea(
            bounds,
            panel.Title,
            Priority: 0,
            Source: panel.Evidence));
    }

    private static GojoCadSkiaPaints CreateDemoPaints(SKColor? tint = null)
    {
        var paints = new GojoCadSkiaPaints
        {
            DotRadius = 4.3f,
            RadialRadius = 20f,
            EndpointBarLength = 7f,
            PartitionTickLength = 7f,
            BasisSize = 5f,
            ArrowLength = 7f,
            ArrowWidth = 3.5f,
            LineSeedLength = 26f,
            SheetSeedWidth = 10f
        };

        paints.SharedFrameSegment.StrokeWidth = 2.2f;
        paints.SharedFrameCurve.StrokeWidth = 2.8f;
        paints.SharedFrameCurveTension.StrokeWidth = 2.1f;
        paints.SharedFramePoint.Color = new SKColor(88, 74, 142, 185);
        if (tint.HasValue)
        {
            ApplyDemoTint(paints, tint.Value);
        }

        return paints;
    }

    private static void ApplyDemoTint(GojoCadSkiaPaints paints, SKColor tint)
    {
        var stroke = tint;
        var tension = WithAlpha(tint, Math.Min(tint.Alpha, (byte)170));
        var point = WithAlpha(tint, Math.Min(tint.Alpha, (byte)190));

        paints.Segment.Color = stroke;
        paints.SegmentTension.Color = tension;
        paints.AngleControlledSegment.Color = stroke;
        paints.AngleControlledSegmentTension.Color = tension;
        paints.SharedFrameSegment.Color = stroke;
        paints.SharedFrameCurve.Color = stroke;
        paints.SharedFrameCurveTension.Color = tension;
        paints.SharedFramePoint.Color = point;
        paints.DirectedSegment.Color = stroke;
        paints.DirectedSegmentTension.Color = tension;
        paints.CircumferenceSpan.Color = tension;
        paints.CircumferenceSpanTension.Color = tension;
        paints.AreaBoundary.Color = tension;
        paints.BranchNode.Color = point;
        paints.MergeNode.Color = point;
        paints.PathGraphBranchNode.Color = point;
        paints.PathGraphTerminalNode.Color = point;
    }

    private static SKColor WithAlpha(SKColor color, byte alpha) =>
        new(color.Red, color.Green, color.Blue, alpha);

    private static void DrawDemoLabels(SKCanvas canvas, VisualizerDemoPanel panel, SKRect drawingBounds)
    {
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            TextSize = 12f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };
        using var labelFill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 254, 250, 220),
            Style = SKPaintStyle.Fill
        };
        using var labelStroke = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(82, 88, 96, 95),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke
        };

        foreach (var label in panel.Labels)
        {
            textPaint.Color = label.Color;
            var point = ProjectDemoModel(label.X, label.Y, panel.ModelBounds, drawingBounds);
            var width = textPaint.MeasureText(label.Text);
            var box = new SKRect(
                point.X - 5f,
                point.Y - 17f,
                point.X + width + 6f,
                point.Y + 4f);
            canvas.DrawRoundRect(box, 4f, 4f, labelFill);
            canvas.DrawRoundRect(box, 4f, 4f, labelStroke);
            canvas.DrawText(label.Text, point.X, point.Y, textPaint);
        }
    }

    private static SKPoint ProjectDemoModel(float x, float y, SKRect modelBounds, SKRect viewport)
    {
        var margin = MathF.Min(36f, MathF.Min(viewport.Width, viewport.Height) * 0.12f);
        var innerWidth = Math.Max(1f, viewport.Width - margin * 2f);
        var innerHeight = Math.Max(1f, viewport.Height - margin * 2f);
        var scale = MathF.Min(innerWidth / modelBounds.Width, innerHeight / modelBounds.Height);
        var modelCenter = new SKPoint(
            modelBounds.Left + modelBounds.Width * 0.5f,
            modelBounds.Top + modelBounds.Height * 0.5f);
        var viewportCenter = new SKPoint(
            viewport.Left + viewport.Width * 0.5f,
            viewport.Top + viewport.Height * 0.5f);
        var offset = new SKPoint(
            viewportCenter.X - modelCenter.X * scale,
            viewportCenter.Y - modelCenter.Y * scale);
        return new SKPoint(offset.X + x * scale, offset.Y - y * scale);
    }

    private static void DrawResidueBadge(SKCanvas canvas, string text, SKRect bounds)
    {
        using var fill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(199, 45, 47, 26),
            Style = SKPaintStyle.Fill
        };
        using var stroke = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(199, 45, 47, 130),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke
        };
        using var textPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(143, 41, 43, 225),
            TextSize = 10f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };

        var textWidth = textPaint.MeasureText(text);
        var badge = new SKRect(
            bounds.Right - textWidth - 26f,
            bounds.Top + 8f,
            bounds.Right - 10f,
            bounds.Top + 29f);
        canvas.DrawRoundRect(badge, 4f, 4f, fill);
        canvas.DrawRoundRect(badge, 4f, 4f, stroke);
        canvas.DrawText(text, badge.Left + 8f, badge.Top + 14.5f, textPaint);
    }

    private void DrawSelection(SKCanvas canvas, IReadOnlyList<GojoCadSkiaHitArea> hitAreas)
    {
        if (document.SelectedIndex is null)
        {
            return;
        }

        using var fill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(230, 145, 46, 34),
            Style = SKPaintStyle.Fill
        };
        using var stroke = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(207, 112, 38, 220),
            StrokeWidth = 1.6f,
            Style = SKPaintStyle.Stroke
        };

        foreach (var area in hitAreas.Where(area => document.SourceIndexOf(area.Source) == document.SelectedIndex))
        {
            var bounds = Expanded(area.Bounds, 4f);
            canvas.DrawRoundRect(bounds, 4f, 4f, fill);
            canvas.DrawRoundRect(bounds, 4f, 4f, stroke);
        }
    }

    private void DrawLegend(SKCanvas canvas)
    {
        var bounds = ReadLegendBounds();
        var legendHits = new List<VisualizerLegendHitArea>();
        using var background = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 254, 250, 238),
            Style = SKPaintStyle.Fill
        };
        using var divider = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(88, 94, 102, 72),
            StrokeWidth = 1f,
            Style = SKPaintStyle.Stroke
        };
        using var title = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(39, 45, 54, 220),
            TextSize = 12f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };
        using var enabledLabel = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(50, 56, 66, 220),
            TextSize = 10.5f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            Style = SKPaintStyle.Fill
        };
        using var disabledLabel = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(103, 108, 117, 120),
            TextSize = 10.5f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            Style = SKPaintStyle.Fill
        };
        using var hoverFill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(34, 101, 145, 22),
            Style = SKPaintStyle.Fill
        };
        using var disabledWash = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 254, 250, 158),
            Style = SKPaintStyle.Fill
        };
        using var tabFill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(62, 80, 96, 16),
            Style = SKPaintStyle.Fill
        };
        using var activeTabFill = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(34, 101, 145, 34),
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRect(bounds, background);
        canvas.DrawLine(bounds.Right, bounds.Top, bounds.Right, bounds.Bottom, divider);
        canvas.DrawText("Legend", bounds.Left + 12f, bounds.Top + 21f, title);

        var pages = VisualizerCanvas.LegendPages;
        var pageIndex = Math.Clamp(activeLegendPageIndex, 0, Math.Max(0, pages.Count - 1));
        if (pages.Count == 0)
        {
            publishLegendHitAreas([]);
            return;
        }

        var tabGap = 6f;
        var tabWidth = (bounds.Width - 24f - tabGap * (pages.Count - 1)) / pages.Count;
        for (var index = 0; index < pages.Count; index++)
        {
            var tab = new SKRect(
                bounds.Left + 12f + index * (tabWidth + tabGap),
                bounds.Top + 31f,
                bounds.Left + 12f + index * (tabWidth + tabGap) + tabWidth,
                bounds.Top + 54f);
            var active = index == pageIndex;
            var hovered = hover?.LegendPageIndex == index;
            canvas.DrawRoundRect(tab, 4f, 4f, active ? activeTabFill : tabFill);
            if (hovered && !active)
            {
                canvas.DrawRoundRect(tab, 4f, 4f, hoverFill);
            }

            DrawTextFitted(
                canvas,
                pages[index].Label,
                tab.Left + 6f,
                tab.Top + 15.5f,
                tab.Width - 12f,
                active ? title : enabledLabel,
                7.5f);
            legendHits.Add(new VisualizerLegendHitArea(
                tab,
                EntryIndex: null,
                PageIndex: index,
                Text: $"show {pages[index].Label}",
                IsEnabled: true,
                Priority: 30));
        }

        var entries = pages[pageIndex].Entries;
        var rows = Math.Max(1, (int)Math.Ceiling(entries.Count / (double)LegendColumns));
        var cellGap = 8f;
        var cellWidth = (bounds.Width - 24f - cellGap) / LegendColumns;
        var rowHeight = MathF.Max(
            LegendCellMinHeight,
            (bounds.Height - LegendTopPadding - 10f) / rows);
        using var paints = CreateLegendPaints();

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var display = document.DisplayFor(entry);
            var column = index % LegendColumns;
            var row = index / LegendColumns;
            var cell = new SKRect(
                bounds.Left + 10f + column * (cellWidth + cellGap),
                bounds.Top + LegendTopPadding + row * rowHeight,
                bounds.Left + 10f + column * (cellWidth + cellGap) + cellWidth,
                bounds.Top + LegendTopPadding + row * rowHeight + rowHeight - 4f);
            var glyphBounds = new SKRect(
                cell.Left + 5f,
                cell.Top + 5f,
                cell.Left + 45f,
                cell.Bottom - 5f);
            var enabled = document.CanApply(entry);
            var isHovered = hover?.LegendIndex == index;

            if (isHovered)
            {
                canvas.DrawRoundRect(cell, 4f, 4f, hoverFill);
            }

            canvas.Save();
            canvas.ClipRect(glyphBounds);
            var projection = new DerivedSkiaProjection(display.Structure, glyphBounds, display.ProjectionSeed);
            Adapter.DrawElement(canvas, display.Structure, projection, paints);
            canvas.Restore();

            if (!enabled)
            {
                canvas.DrawRoundRect(cell, 4f, 4f, disabledWash);
            }

            DrawLegendLabel(
                canvas,
                display.Label,
                cell,
                enabled ? enabledLabel : disabledLabel);

            var text = enabled
                ? $"add {display.Label}"
                : $"{display.Label}: {document.DisabledReason(entry)}";
            legendHits.Add(new VisualizerLegendHitArea(
                cell,
                EntryIndex: index,
                PageIndex: null,
                Text: text,
                IsEnabled: enabled,
                Priority: 20));
        }

        publishLegendHitAreas(legendHits.ToArray());
    }

    private SKRect ReadLegendBounds()
    {
        var width = MathF.Min(LegendWidth, MathF.Max(184f, (float)Bounds.Width * 0.31f));
        return new SKRect(0, 0, width, (float)Bounds.Height);
    }

    private SKRect ReadSampleViewport()
    {
        var legend = ReadLegendBounds();
        var left = MathF.Min(legend.Right + LegendGap, MathF.Max(0f, (float)Bounds.Width - 80f));
        return new SKRect(left, 0, (float)Bounds.Width, (float)Bounds.Height);
    }

    private static SKRect ReadStableModelBounds() =>
        new(-48f, -48f, 88f, 52f);

    private static void DrawLegendLabel(SKCanvas canvas, string label, SKRect cell, SKPaint basePaint)
    {
        DrawTextFitted(
            canvas,
            label,
            cell.Left + 51f,
            cell.Top + cell.Height * 0.5f + 4f,
            MathF.Max(16f, cell.Right - cell.Left - 55f),
            basePaint,
            8f);
    }

    private static void DrawTextFitted(
        SKCanvas canvas,
        string text,
        float x,
        float baseline,
        float maxWidth,
        SKPaint basePaint,
        float minTextSize)
    {
        using var paint = new SKPaint
        {
            IsAntialias = basePaint.IsAntialias,
            Color = basePaint.Color,
            TextSize = basePaint.TextSize,
            Typeface = basePaint.Typeface,
            Style = basePaint.Style
        };
        while (paint.TextSize > minTextSize && paint.MeasureText(text) > maxWidth)
        {
            paint.TextSize -= 0.5f;
        }

        canvas.DrawText(text, x, baseline, paint);
    }

    private static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        SKRect bounds,
        SKPaint basePaint,
        float lineHeight,
        int maxLines)
    {
        if (string.IsNullOrWhiteSpace(text) || maxLines <= 0)
        {
            return;
        }

        using var paint = new SKPaint
        {
            IsAntialias = basePaint.IsAntialias,
            Color = basePaint.Color,
            TextSize = basePaint.TextSize,
            Typeface = basePaint.Typeface,
            Style = basePaint.Style
        };
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in words)
        {
            var candidate = string.IsNullOrEmpty(current) ? word : $"{current} {word}";
            if (paint.MeasureText(candidate) <= bounds.Width || string.IsNullOrEmpty(current))
            {
                current = candidate;
                continue;
            }

            lines.Add(current);
            current = word;
            if (lines.Count == maxLines)
            {
                break;
            }
        }

        if (lines.Count < maxLines && !string.IsNullOrEmpty(current))
        {
            lines.Add(current);
        }

        for (var index = 0; index < lines.Count && index < maxLines; index++)
        {
            var line = lines[index];
            if (index == maxLines - 1 && index < words.Length - 1 && paint.MeasureText(line) > bounds.Width)
            {
                while (line.Length > 1 && paint.MeasureText($"{line}...") > bounds.Width)
                {
                    line = line[..^1];
                }

                line = $"{line}...";
            }

            canvas.DrawText(line, bounds.Left, bounds.Top + 10.5f + index * lineHeight, paint);
        }
    }

    private static GojoCadSkiaPaints CreateLegendPaints()
    {
        var paints = new GojoCadSkiaPaints
        {
            DotRadius = 3.2f,
            RadialRadius = 12f,
            EndpointBarLength = 6f,
            PartitionTickLength = 6f,
            BasisSize = 5f,
            ArrowLength = 6f,
            ArrowWidth = 3f,
            LineSeedLength = 22f,
            SheetSeedWidth = 9f
        };

        paints.Segment.StrokeWidth = 1.5f;
        paints.SegmentTension.StrokeWidth = 1.4f;
        paints.AngleControlledSegment.StrokeWidth = 1.8f;
        paints.AngleControlledSegmentTension.StrokeWidth = 1.6f;
        paints.SharedFrameCurve.StrokeWidth = 1.7f;
        paints.SharedFrameCurveTension.StrokeWidth = 1.5f;
        paints.DirectedSegment.StrokeWidth = 1.7f;
        paints.DirectedSegmentTension.StrokeWidth = 1.6f;
        paints.Circumference.StrokeWidth = 0.9f;
        paints.CircumferenceTension.StrokeWidth = 0.9f;
        paints.CircumferenceSpan.StrokeWidth = 1.1f;
        paints.CircumferenceSpanTension.StrokeWidth = 1f;
        paints.EndpointBar.StrokeWidth = 1f;
        paints.Partition.StrokeWidth = 1f;
        paints.Basis.StrokeWidth = 1f;
        paints.LineSeed.StrokeWidth = 1f;
        paints.LineSeedTension.StrokeWidth = 1f;
        paints.SheetSeedEdge.StrokeWidth = 0.9f;
        paints.SheetSeedEdgeTension.StrokeWidth = 0.9f;
        paints.RadiusAttachment.StrokeWidth = 1f;
        paints.RadiusAttachmentTension.StrokeWidth = 1f;
        paints.AreaBoundary.StrokeWidth = 0.9f;

        return paints;
    }

    private void DrawHover(SKCanvas canvas)
    {
        if (hover is null)
        {
            return;
        }

        using var paints = new GojoCadSkiaPaints();
        var text = ReadHoverText(hover);
        paints.HoverText.TextSize = 13f;
        var width = paints.HoverText.MeasureText(text);
        var x = MathF.Min(hoverPoint.X + 14f, (float)Bounds.Width - width - 30f);
        var y = MathF.Min(hoverPoint.Y + 24f, (float)Bounds.Height - 34f);
        x = MathF.Max(8f, x);
        y = MathF.Max(24f, y);
        var box = new SKRect(x - 8f, y - 20f, x + width + 8f, y + 8f);

        canvas.DrawRoundRect(box, 4f, 4f, paints.HoverBackground);
        canvas.DrawRoundRect(box, 4f, 4f, paints.HoverBorder);
        canvas.DrawText(text, x, y, paints.HoverText);
    }

    private string ReadHoverText(VisualizerHover area)
    {
        if (area.LegendIndex is not null)
        {
            return area.Text;
        }

        var index = document.SourceIndexOf(area.Source);
        return index is null
            ? area.Text
            : $"{area.Text} element[{index.Value}]";
    }

    private static IElement Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return Carrier.Unknown;
        }

        var structure = elements[^1];
        for (var index = elements.Count - 2; index >= 0; index--)
        {
            structure = new Relation(elements[index], structure);
        }

        return structure;
    }

    private static SKRect Expanded(SKRect rect, float amount) =>
        new(rect.Left - amount, rect.Top - amount, rect.Right + amount, rect.Bottom + amount);
}

internal sealed record VisualizerLegendHitArea(
    SKRect Bounds,
    int? EntryIndex,
    int? PageIndex,
    string Text,
    bool IsEnabled,
    int Priority)
{
    public bool Contains(SKPoint point) => Bounds.Contains(point);
}

internal sealed record VisualizerHover(
    SKRect Bounds,
    string Text,
    int Priority,
    IElement? Source,
    int? LegendIndex,
    int? LegendPageIndex);
