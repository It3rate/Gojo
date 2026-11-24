namespace Gojo.EncodingC;

/// <summary>What a law's role can be bound to: a store's positions, the
/// lanes of a port another local structure exposes, or a workspace over a store. A
/// role on a store works inside a local structure. A role on a port works
/// across a boundary, and a law may write there only by moving what it takes
/// (see <see cref="Wiring"/>). A role on a workspace works on a live view of a
/// store and copies a bit only when it writes it.
/// <para>Every read and write a law makes goes through its surface, so the same
/// law runs unchanged on any of them.</para></summary>
public abstract class Surface
{
    private protected Surface()
    {
    }

    /// <summary>Positions of a store or workspace, or lanes of a port.</summary>
    public abstract int Count { get; }

    public abstract int PlaneCount { get; }

    public abstract bool IsLive(int index);

    /// <summary>Whether this index and another surface's index reach one
    /// position of one store, following ports and sockets. This is identity:
    /// two positions holding equal bits are still two positions, and a lane
    /// the owner rebinds reaches somewhere else. A lane that reaches nowhere is
    /// the same as nothing but itself.</summary>
    public bool SamePosition(int index, Surface other, int otherIndex)
    {
        ArgumentNullException.ThrowIfNull(other);
        if ((uint)index >= (uint)Count || (uint)otherIndex >= (uint)other.Count) return false;
        if (PlaneCount == 0 || other.PlaneCount == 0) return ReferenceEquals(this, other) && index == otherIndex;
        var (home, at, _) = Bit(index, 0);
        var (otherHome, otherAt, _) = other.Bit(otherIndex, 0);
        return ReferenceEquals(home, otherHome) && at == otherAt;
    }

    /// <summary>One bit, as the surface shows it now.</summary>
    public abstract bool Read(int plane, int index);

    internal abstract bool ThroughPort { get; }

    /// <summary>The store whose own positions this surface reaches directly,
    /// or null for a port or socket, which reaches another local structure's
    /// store only through lanes it exposes. A local structure binds directly
    /// only surfaces whose home is one of its own stores.</summary>
    internal virtual Store? Home => null;

    /// <summary>What this surface shows: itself, or for a view, what the view
    /// shows. Whether a surface is supposed, gated or material is decided
    /// here, so a view never changes what its source may do.</summary>
    internal virtual Surface Underlying => this;

    /// <summary>Whether something other than <paramref name="taker"/> already
    /// takes through this surface. Only a port has one taker.</summary>
    internal virtual bool TakenByOther(Wiring taker) => false;

    /// <summary>Records that <paramref name="taker"/> writes through this
    /// surface, as a move.</summary>
    internal virtual void Take(Wiring taker)
    {
    }

    internal abstract bool IsConstant(int plane);

    /// <summary>The bit a plane holds at a position nothing has written yet:
    /// 0, absence, unless the plane declares that new positions start at 1, as
    /// what is still possible does before anything is known.</summary>
    internal virtual bool Filled(int plane) => false;

    /// <summary>A surface that is a read of other things, such as the gap
    /// between a workspace and its source. No law may write it.</summary>
    internal virtual bool ReadOnly => false;

    /// <summary>Writes one bit as a law does.</summary>
    internal abstract void Set(int plane, int index, bool value);

    /// <summary>Which bit this is once ports are followed. Two roles that reach
    /// one bit share one position, whatever surfaces they came through.</summary>
    internal abstract (Surface Home, int Index, int Plane) Bit(int index, int plane);

    /// <summary>For scheduling: every place whose writes a read of this index
    /// can see.</summary>
    internal abstract void ReadKeys(int index, List<(Surface, int)> keys);

    /// <summary>For scheduling: the place a write to this index changes.</summary>
    internal abstract (Surface, int) WriteKey(int index);

    /// <summary>One row of a law changed these indexes in one step, as a move
    /// does. A store holds what is and keeps nothing more; a workspace records
    /// them, so they are committed or reverted only together.</summary>
    internal virtual void ChangedTogether(IReadOnlyList<int> indexes)
    {
    }
}
