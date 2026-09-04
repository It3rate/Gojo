using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Gojo.GearShapeLadder;

internal sealed class GearShapeCanvas : Control
{
    private GearShapeLadderModel? model;
    private int stage;

    public GearShapeCanvas() => ClipToBounds = true;

    public GearShapeLadderModel? Model
    {
        get => model;
        set
        {
            model = value;
            InvalidateVisual();
        }
    }

    public int Stage
    {
        get => stage;
        set
        {
            stage = Math.Clamp(value, 0, GearShapeGeometry.StageCount - 1);
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (model is not null)
        {
            context.Custom(new GearShapeDrawOperation(new Rect(Bounds.Size), model, stage));
        }
    }
}

internal sealed class GearShapeDrawOperation(Rect bounds, GearShapeLadderModel model, int stage) : ICustomDrawOperation
{
    public Rect Bounds { get; } = bounds;

    public void Dispose() { }

    public bool HitTest(Point p) => Bounds.Contains(p);

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public void Render(ImmediateDrawingContext context)
    {
        if (context.TryGetFeature(typeof(ISkiaSharpApiLeaseFeature)) is not ISkiaSharpApiLeaseFeature feature)
        {
            return;
        }

        using var lease = feature.Lease();
        GearShapeRenderer.Draw(
            lease.SkCanvas,
            new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height),
            model,
            stage);
    }
}
