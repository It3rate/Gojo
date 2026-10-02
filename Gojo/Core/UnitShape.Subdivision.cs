namespace Gojo.Core;

public sealed partial class UnitShape
{
    /// <summary>Defines a finer tick: parts returned ticks correspond to one source tick.</summary>
    public static UnitShape SubdivisionOf(UnitShape source, long parts)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parts);
        if (parts == 1) return source;
        if (source.TryReadOpposedFace(out var forward))
            return OpposedFaceOf(SubdivisionOf(forward, parts));
        if (source.TryReadSubdivision(out var inner, out var previous))
            return SubdivisionOf(inner, checked(previous * parts));
        if (source.TryReadResolution(out var coarseSource, out var coarse) && coarse % parts == 0)
            return ResolutionOf(coarseSource, coarse / parts);
        return new UnitShape(new Relation(Segment(Carrier.Unknown, 0, parts), SnapshotOf(source.surface)), source.descriptors);
    }

    /// <summary>Reads a finer tick's parent unit and positive subdivision.</summary>
    public bool TryReadSubdivision(out UnitShape source, out long parts)
    {
        if (TryReadSubdivisionSurface(surface, out var inner, out parts))
        {
            source = ShapeFromSurface(inner);
            return true;
        }
        source = null!;
        return false;
    }

    private static bool TryReadSubdivisionSurface(IElement element, out IElement source, out long parts)
    {
        if (element is Relation { Left: Relation scale } relation &&
            TryReadSegment(scale, out var start, out var end) && start.Carrier.IsUnknown &&
            start.Ticks == 0 && end.Ticks > 1)
        {
            source = relation.Right;
            parts = end.Ticks;
            return true;
        }
        source = null!;
        parts = 0;
        return false;
    }
}
