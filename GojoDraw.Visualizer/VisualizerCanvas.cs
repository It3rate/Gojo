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
        using var paints = new GojoCadSkiaPaints();
        Adapter.DrawElement(canvas, sample.Structure, sample.Projection, paints);
    }
}
