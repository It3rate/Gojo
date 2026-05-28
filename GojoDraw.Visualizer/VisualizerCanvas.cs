using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;
using GojoDraw;
using SkiaSharp;

namespace GojoDraw.Visualizer;

internal sealed class VisualizerCanvas : Control
{
    private VisualizerSample? sample;

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

        context.Custom(new VisualizerDrawOperation(new Rect(Bounds.Size), Sample));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InvalidateVisual();
    }
}

internal sealed class VisualizerDrawOperation : ICustomDrawOperation
{
    private static readonly GojoCadSkiaAdapter Adapter = new();
    private readonly VisualizerSample sample;

    public VisualizerDrawOperation(Rect bounds, VisualizerSample sample)
    {
        Bounds = bounds;
        this.sample = sample;
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
        DrawSample(canvas);
        canvas.Restore();
    }

    private void DrawSample(SKCanvas canvas)
    {
        using var dotPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(31, 45, 61),
            Style = SKPaintStyle.Fill
        };
        using var radialDotPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(162, 64, 46),
            Style = SKPaintStyle.Fill
        };
        using var segmentPaint = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(42, 91, 143),
            StrokeWidth = 5f,
            StrokeCap = SKStrokeCap.Round,
            Style = SKPaintStyle.Stroke
        };

        Adapter.DrawDot(canvas, sample.Dot, sample.Projection, 10f, dotPaint);
        Adapter.DrawRadialFlow(
            canvas,
            sample.RadialFlow,
            sample.Projection,
            58f,
            new SKColor(162, 64, 46, 110),
            new SKColor(162, 64, 46, 0));
        Adapter.DrawDot(canvas, sample.RadialDot, sample.Projection, 9f, radialDotPaint);
        Adapter.DrawSegment(canvas, sample.Segment, sample.Projection, segmentPaint);
    }
}
