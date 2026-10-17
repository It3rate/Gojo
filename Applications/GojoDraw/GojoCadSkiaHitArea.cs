using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>Render-boundary hover region produced from a drawn primitive.</summary>
public sealed record GojoCadSkiaHitArea(
    SKRect Bounds,
    string Text,
    int Priority = 0,
    IElement? Source = null,
    GojoDrawHitBehavior Behavior = GojoDrawHitBehavior.Hover,
    string? InteractionId = null)
{
    public bool Contains(SKPoint point) => Bounds.Contains(point);
}

public enum GojoDrawHitBehavior
{
    Hover,
    Press,
    Drag,
    Toggle,
    Slider,
    Navigation
}
