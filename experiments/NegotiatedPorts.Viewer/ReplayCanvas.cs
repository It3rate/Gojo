using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace NegotiatedPorts.Viewer;

internal sealed class ReplayCanvas(Replay replay) : Control
{
    private int frame;
    public int Frame
    {
        get => frame;
        set
        {
            var next = Math.Clamp(value, 0, replay.Frames.Length - 1);
            if (frame == next) return;
            frame = next;
            InvalidateVisual();
        }
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.Custom(new DrawOperation(new Rect(Bounds.Size), replay, frame));
    }
}

internal sealed class DrawOperation(Rect bounds, Replay replay, int frame) : ICustomDrawOperation
{
    public Rect Bounds { get; } = bounds;
    public bool HitTest(Point point) => Bounds.Contains(point);
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { }
    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature) return;
        using var lease = feature.Lease();
        ReplayRenderer.Draw(lease.SkCanvas, (float)Bounds.Width, (float)Bounds.Height, replay, frame);
    }
}
