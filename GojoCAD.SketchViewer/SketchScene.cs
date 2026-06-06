using Gojo.Core;
using Gojo.Structures;
using GojoDraw;
using SkiaSharp;

namespace GojoCAD.SketchViewer;

internal sealed record SketchScene(
    string Label,
    string Caption,
    IElement Structure,
    int ProjectionSeed,
    SKRect? ModelBounds = null,
    IReadOnlyList<string>? RungLabels = null,
    GojoCadOffsetStudy? OffsetStudy = null)
{
    public IReadOnlyList<Relation> ReadRungs()
    {
        if (OffsetStudy is not null)
        {
            return [];
        }

        if (Structure is Relation relation &&
            StructuralLadderSurface.TryReadRungPath(relation, out var path))
        {
            return path;
        }

        return Structure is Relation fallback ? [fallback] : [];
    }

    public int RungCount => Math.Max(1, ReadRungs().Count);

    public int LastRungIndex => RungCount - 1;

    public string ReadRungLabel(int index)
    {
        var bounded = Math.Clamp(index, 0, LastRungIndex);
        if (RungLabels is { Count: > 0 } labels &&
            bounded < labels.Count)
        {
            return labels[bounded];
        }

        return $"Rung {bounded + 1}";
    }
}
