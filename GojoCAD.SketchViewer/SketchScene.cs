using Gojo.Core;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal sealed record SketchScene(
    string Label,
    string Caption,
    IElement Structure,
    int ProjectionSeed,
    SKRect? ModelBounds = null);
