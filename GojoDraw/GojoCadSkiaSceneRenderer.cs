using Gojo.Core;
using SkiaSharp;

namespace GojoDraw;

/// <summary>
/// Small render-boundary helper for drawing one GojoCAD structure into a Skia
/// viewport. Meaning still comes from the Gojo structure and GojoCAD reads;
/// this only wires projection, paints, and adapter traversal together.
/// </summary>
public sealed class GojoCadSkiaSceneRenderer
{
    private readonly GojoCadSkiaAdapter adapter = new();

    public int Draw(
        SKCanvas canvas,
        IElement structure,
        SKRect viewport,
        int projectionSeed,
        SKRect? fixedModelBounds = null,
        GojoCadSkiaPaints? paints = null,
        ICollection<GojoCadSkiaHitArea>? hitAreas = null)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        ArgumentNullException.ThrowIfNull(structure);

        using var ownedPaints = paints is null ? new GojoCadSkiaPaints() : null;
        var projection = new DerivedSkiaProjection(
            structure,
            viewport,
            projectionSeed,
            fixedModelBounds);

        return adapter.DrawElement(
            canvas,
            structure,
            projection,
            paints ?? ownedPaints!,
            hitAreas);
    }
}
