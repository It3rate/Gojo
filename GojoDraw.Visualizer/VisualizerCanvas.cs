using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using GojoDraw;
using SkiaSharp;
using System.Threading;

namespace GojoDraw.Visualizer;

internal sealed class VisualizerCanvas : Control
{
    private VisualizerSample? sample;
    private GojoCadSkiaHitArea[] hitAreas = [];
    private GojoCadSkiaHitArea? hover;
    private SKPoint hoverPoint;

    public VisualizerCanvas()
    {
        ClipToBounds = true;
    }

    public VisualizerSample? Sample
    {
        get => sample;
        set
        {
            sample = value;
            hover = null;
            Volatile.Write(ref hitAreas, []);
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Sample is null)
        {
            return;
        }

        context.Custom(new VisualizerDrawOperation(
            new Rect(Bounds.Size),
            Sample,
            PublishHitAreas,
            hover,
            hoverPoint));
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
        var snapshot = Volatile.Read(ref hitAreas);
        var next = snapshot
            .Where(area => area.Contains(hoverPoint))
            .OrderByDescending(area => area.Priority)
            .FirstOrDefault();
        if (!Equals(next, hover))
        {
            hover = next;
            InvalidateVisual();
        }
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

    private void PublishHitAreas(GojoCadSkiaHitArea[] nextHitAreas)
    {
        Volatile.Write(ref hitAreas, nextHitAreas);
    }
}

internal sealed class VisualizerDrawOperation : ICustomDrawOperation
{
    private const float LegendWidth = 152f;
    private const float LegendGap = 10f;
    private const float LegendTopPadding = 34f;
    private const float LegendRowMinHeight = 27f;

    private static readonly GojoCadSkiaAdapter Adapter = new();
    private static readonly IReadOnlyList<VisualizerLegendEntry> LegendEntries = VisualizerSample.CreateLegend();
    private readonly VisualizerSample sample;
    private readonly Action<GojoCadSkiaHitArea[]> publishHitAreas;
    private readonly GojoCadSkiaHitArea? hover;
    private readonly SKPoint hoverPoint;

    public VisualizerDrawOperation(
        Rect bounds,
        VisualizerSample sample,
        Action<GojoCadSkiaHitArea[]> publishHitAreas,
        GojoCadSkiaHitArea? hover,
        SKPoint hoverPoint)
    {
        Bounds = bounds;
        this.sample = sample;
        this.publishHitAreas = publishHitAreas;
        this.hover = hover;
        this.hoverPoint = hoverPoint;
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
            sample.Structure,
            viewport,
            sample.ProjectionSeed);
        var hitAreas = new List<GojoCadSkiaHitArea>();
        Adapter.DrawElement(canvas, sample.Structure, projection, paints, hitAreas);
        publishHitAreas(hitAreas.ToArray());
    }

    private void DrawLegend(SKCanvas canvas)
    {
        var bounds = ReadLegendBounds();
        using var background = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(255, 254, 250, 230),
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
            Color = new SKColor(39, 45, 54, 210),
            TextSize = 12f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Style = SKPaintStyle.Fill
        };
        using var label = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(50, 56, 66, 210),
            TextSize = 11f,
            Typeface = SKTypeface.FromFamilyName("Segoe UI"),
            Style = SKPaintStyle.Fill
        };

        canvas.DrawRect(bounds, background);
        canvas.DrawLine(bounds.Right, bounds.Top, bounds.Right, bounds.Bottom, divider);
        canvas.DrawText("Legend", bounds.Left + 12f, bounds.Top + 21f, title);

        var rowHeight = MathF.Max(
            LegendRowMinHeight,
            (bounds.Height - LegendTopPadding - 8f) / Math.Max(1, LegendEntries.Count));
        using var paints = CreateLegendPaints();

        for (var index = 0; index < LegendEntries.Count; index++)
        {
            var entry = LegendEntries[index];
            var rowTop = bounds.Top + LegendTopPadding + index * rowHeight;
            var rowCenter = rowTop + rowHeight * 0.5f;
            var glyphBounds = new SKRect(
                bounds.Left + 10f,
                rowTop + 4f,
                bounds.Left + 58f,
                rowTop + rowHeight - 4f);

            canvas.Save();
            canvas.ClipRect(glyphBounds);
            var projection = new DerivedSkiaProjection(entry.Structure, glyphBounds, entry.ProjectionSeed);
            Adapter.DrawElement(canvas, entry.Structure, projection, paints);
            canvas.Restore();

            canvas.DrawText(entry.Label, bounds.Left + 68f, rowCenter + 4f, label);
        }
    }

    private SKRect ReadLegendBounds()
    {
        var width = MathF.Min(LegendWidth, MathF.Max(124f, (float)Bounds.Width * 0.23f));
        return new SKRect(0, 0, width, (float)Bounds.Height);
    }

    private SKRect ReadSampleViewport()
    {
        var legend = ReadLegendBounds();
        var left = MathF.Min(legend.Right + LegendGap, MathF.Max(0f, (float)Bounds.Width - 80f));
        return new SKRect(left, 0, (float)Bounds.Width, (float)Bounds.Height);
    }

    private static GojoCadSkiaPaints CreateLegendPaints()
    {
        var paints = new GojoCadSkiaPaints
        {
            DotRadius = 3.4f,
            RadialRadius = 14f,
            EndpointBarLength = 7f,
            PartitionTickLength = 7f,
            BasisSize = 5f,
            ArrowLength = 7f,
            ArrowWidth = 3f,
            LineSeedLength = 24f,
            SheetSeedWidth = 10f
        };

        paints.Segment.StrokeWidth = 1.6f;
        paints.SegmentTension.StrokeWidth = 1.5f;
        paints.AngleControlledSegment.StrokeWidth = 1.8f;
        paints.AngleControlledSegmentTension.StrokeWidth = 1.7f;
        paints.DirectedSegment.StrokeWidth = 1.8f;
        paints.DirectedSegmentTension.StrokeWidth = 1.7f;
        paints.Circumference.StrokeWidth = 0.9f;
        paints.CircumferenceTension.StrokeWidth = 0.9f;
        paints.CircumferenceSpan.StrokeWidth = 1.2f;
        paints.CircumferenceSpanTension.StrokeWidth = 1.1f;
        paints.EndpointBar.StrokeWidth = 1f;
        paints.Partition.StrokeWidth = 1f;
        paints.Basis.StrokeWidth = 1f;
        paints.LineSeed.StrokeWidth = 1f;
        paints.LineSeedTension.StrokeWidth = 1f;
        paints.SheetSeedEdge.StrokeWidth = 0.9f;
        paints.SheetSeedEdgeTension.StrokeWidth = 0.9f;
        paints.RadiusAttachment.StrokeWidth = 1f;
        paints.RadiusAttachmentTension.StrokeWidth = 1f;

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

    private string ReadHoverText(GojoCadSkiaHitArea area)
    {
        var index = sample.SourceIndexOf(area.Source);
        return index is null
            ? area.Text
            : $"{area.Text} element[{index.Value}]";
    }
}
