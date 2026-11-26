namespace Gojo.EncodingC;

public abstract partial class Surface
{
    /// <summary>Whether two surfaces expose disjoint sets of live positions, each
    /// position appearing once. Returns null when an exposed position has no
    /// readable live source. Rebinding a port changes a subsequent read.
    /// The bits and their values play no part in this read.</summary>
    public bool? DisjointPositions(Surface other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!HasLivePositions() || !other.HasLivePositions()) return null;

        return HasUniquePositions() && other.HasUniquePositions() && !SharesPosition(other);
    }

    private bool HasLivePositions() =>
        this is not Socket { Plugged: null } &&
        (Count == 0 || PlaneCount != 0) &&
        Enumerable.Range(0, Count).All(IsLive);

    private bool HasUniquePositions()
    {
        for (var i = 0; i < Count; i++)
            for (var j = 0; j < i; j++)
                if (SamePosition(i, this, j)) return false;
        return true;
    }

    private bool SharesPosition(Surface other)
    {
        for (var i = 0; i < Count; i++)
            for (var j = 0; j < other.Count; j++)
                if (SamePosition(i, other, j)) return true;
        return false;
    }
}
