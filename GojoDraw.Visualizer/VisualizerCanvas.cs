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

            canvas.DrawText(
                pages[index].Label,
                tab.Left + 8f,
                tab.Top + 15.5f,
                active ? title : enabledLabel);
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
        using var paint = new SKPaint
        {
            IsAntialias = basePaint.IsAntialias,
            Color = basePaint.Color,
            TextSize = basePaint.TextSize,
            Typeface = basePaint.Typeface,
            Style = basePaint.Style
        };
        var x = cell.Left + 51f;
        var maxWidth = MathF.Max(16f, cell.Right - x - 4f);
        while (paint.TextSize > 8f && paint.MeasureText(label) > maxWidth)
        {
            paint.TextSize -= 0.5f;
        }

        canvas.DrawText(label, x, cell.Top + cell.Height * 0.5f + 4f, paint);
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
