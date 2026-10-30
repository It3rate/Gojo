using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace Gojo.Experiments.PrimitiveRuntime.Viewer;

internal sealed class PrimitiveCanvas(InspectionView initial) : Control
{
    public InspectionView View { get; set; } = initial;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.Custom(new PrimitiveDrawOperation(new Rect(Bounds.Size), View));
    }
}

internal sealed class PrimitiveDrawOperation(Rect bounds, InspectionView view) : ICustomDrawOperation
{
    public Rect Bounds { get; } = bounds;
    public bool HitTest(Point point) => Bounds.Contains(point);
    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
    public void Dispose() { }

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature) return;
        using var lease = feature.Lease();
        PrimitiveRenderer.Draw(lease.SkCanvas, (float)Bounds.Width, (float)Bounds.Height, view);
    }
}
