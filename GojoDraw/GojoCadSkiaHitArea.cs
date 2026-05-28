using SkiaSharp;

namespace GojoDraw;

/// <summary>Render-boundary hover region produced from a drawn primitive.</summary>
public sealed record GojoCadSkiaHitArea(SKRect Bounds, string Text, int Priority = 0)
{
    public bool Contains(SKPoint point) => Bounds.Contains(point);
}
