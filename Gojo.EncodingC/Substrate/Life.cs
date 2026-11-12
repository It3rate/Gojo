namespace Gojo.EncodingC;

/// <summary>Whether each position of a store is in use, as a surface with
/// one plane. It lets existence be state like everything else: a law reads it,
/// recruits a spare position by setting its bit, and retires a position in
/// use by clearing it.
/// <para>That is how a structure grows and prunes itself. Growth is recruiting
/// room that was already there, as a body recruits cells it has: a dormant
/// row, wired but not in use, starts to run when a law recruits it. Pruning
/// is retiring what is no longer wanted, such as a predictor whose trust ran
/// out. A retired position is out of reach here, so nothing brings it back,
/// and a reference to it never comes to mean something else.</para></summary>
public sealed class Life : Surface
{
    private readonly Store store;

    internal Life(Store store) => this.store = store;

    /// <summary>The store whose positions these are.</summary>
    internal Store Of => store;

    public override int Count => store.Count;

    public override int PlaneCount => 1;

    /// <summary>Spare and in-use positions can be reached here. Retired ones cannot.</summary>
    public override bool IsLive(int index) => (uint)index < (uint)Count && !store.IsRetired(index);

    public override bool Read(int plane, int index)
    {
        if (plane != 0) throw new ArgumentOutOfRangeException(nameof(plane));
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        return store.IsLive(index);
    }

    internal override bool ThroughPort => false;

    internal override bool IsConstant(int plane) => false;

    internal override void Set(int plane, int index, bool value)
    {
        if (plane != 0) throw new ArgumentOutOfRangeException(nameof(plane));
        if (value == store.IsLive(index)) return;
        if (value) store.Recruit(index);
        else store.Retire(index);
    }

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    // Whether a position is in use decides whether rows that touch it run, so
    // it is scheduled as that position.
    internal override void ReadKeys(int index, List<(Surface, int)> keys) => keys.Add((store, index));

    internal override (Surface, int) WriteKey(int index) => (store, index);
}
