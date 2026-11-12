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

    /// <summary>One bit, as the surface shows it now.</summary>
    public abstract bool Read(int plane, int index);

    internal abstract bool ThroughPort { get; }

    internal abstract bool IsConstant(int plane);

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
