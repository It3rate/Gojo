using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private readonly List<GojoCadSkiaHitArea> hitAreas = [];
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

        context.Custom(new VisualizerDrawOperation(new Rect(Bounds.Size), Sample, hitAreas, hover, hoverPoint));
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
        var next = hitAreas
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
}

internal sealed class VisualizerDrawOperation : ICustomDrawOperation
{
    private static readonly GojoCadSkiaAdapter Adapter = new();
    private readonly VisualizerSample sample;
    private readonly List<GojoCadSkiaHitArea> hitAreas;
    private readonly GojoCadSkiaHitArea? hover;
    private readonly SKPoint hoverPoint;

    public VisualizerDrawOperation(
        Rect bounds,
        VisualizerSample sample,
        List<GojoCadSkiaHitArea> hitAreas,
        GojoCadSkiaHitArea? hover,
        SKPoint hoverPoint)
    {
        Bounds = bounds;
        this.sample = sample;
        this.hitAreas = hitAreas;
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
        DrawSample(canvas);
        DrawHover(canvas);
        canvas.Restore();
    }

    private void DrawSample(SKCanvas canvas)
    {
        using var paints = new GojoCadSkiaPaints();
        hitAreas.Clear();
        Adapter.DrawElement(canvas, sample.Structure, sample.Projection, paints, hitAreas);
    }

    private void DrawHover(SKCanvas canvas)
    {
        if (hover is null)
        {
            return;
        }

        using var paints = new GojoCadSkiaPaints();
        var text = hover.Text;
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
}
