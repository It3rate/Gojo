namespace Gojo.EncodingC;

/// <summary>The two-faced gap between two unary counts, have and want, at each
/// position: a <see cref="Deficit"/> (want more than have) and an
/// <see cref="Excess"/> (have more than want), each itself a unary count, read
/// live as a surface laws can read by reference
/// (docs/GojoTension-2026-11.md, §3.4).
/// <para><b>How it is read.</b> Bit k of the deficit is set when want exceeds
/// have by more than k. For unary counts that holds exactly when, for some j,
/// have's bit j is clear and want's bit j + k is set: a shifted AND, ORed over
/// j. The excess is the same with have and want swapped. Both come out as
/// unary counts, never both nonzero, so the gap carries both how far and which
/// way. A response reads one operand: the deficit's lowest bit, "short at
/// all", opens the way in, and the excess's lowest bit opens the way out.</para>
/// <para><b>Never stored.</b> Like <see cref="Gap"/>, it exists while both
/// sides do and is never written. A count whose bits are not a count is not
/// repaired: what reads from it may not be a count either, and a law reading
/// it finds it unread.</para>
/// <para><b>Where the two sides may be.</b> Have and want are read at the same
/// position. Both belong to one local structure, so a law reading the gap
/// reads nothing it could not bind: a store and a workspace over it, two
/// fields of one store, or a store and what it reads through a port. When
/// either side is a workspace, the gap is a supposition, and a relation
/// reading it narrows only what is supposed.</para></summary>
public sealed class Difference : Surface
{
    private readonly Surface have;
    private readonly Surface want;
    private readonly Field haveCount;
    private readonly Field wantCount;
    private readonly int capacity;

    public Difference(Surface have, Field haveCount, Surface want, Field wantCount)
    {
        ArgumentNullException.ThrowIfNull(have);
        ArgumentNullException.ThrowIfNull(haveCount);
        ArgumentNullException.ThrowIfNull(want);
        ArgumentNullException.ThrowIfNull(wantCount);
        if (!haveCount.Shape.IsUnary || !wantCount.Shape.IsUnary) throw new ArgumentException("A two-faced gap compares unary counts.");
        if (have.Home is { } a && want.Home is { } b && !ReferenceEquals(a, b))
            throw new ArgumentException("Have and want belong to one local structure: two fields of one store, a store and a workspace over it, or a store and a port.");
        if (have.Count != want.Count) throw new ArgumentException("Have and want are read at the same positions.");
        this.have = have;
        this.want = want;
        this.haveCount = haveCount;
        this.wantCount = wantCount;
        capacity = haveCount.Planes.Count > wantCount.Planes.Count ? haveCount.Planes.Count : wantCount.Planes.Count;
        Deficit = new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity)]);
        Excess = new Field(Shape.Unary(capacity), [.. Enumerable.Range(capacity, capacity)]);
    }

    /// <summary>How far have is below want, as a unary count on planes 0 to
    /// capacity − 1.</summary>
    public Field Deficit { get; }

    /// <summary>How far have is above want, as a unary count on the next planes.</summary>
    public Field Excess { get; }

    public override int Count => have.Count;

    public override int PlaneCount => 2 * capacity;

    public override bool IsLive(int index) => have.IsLive(index) && want.IsLive(index);

    public override bool Read(int plane, int index)
    {
        if ((uint)plane >= (uint)PlaneCount) throw new ArgumentOutOfRangeException(nameof(plane));
        var (low, high, lowCount, highCount, k) = plane < capacity
            ? (have, want, haveCount, wantCount, plane)
            : (want, have, wantCount, haveCount, plane - capacity);
        for (var j = 0; j + k < highCount.Planes.Count; j++)
            if (!Bit(low, lowCount, j, index) && high.Read(highCount.Planes[j + k], index)) return true;
        return false;
    }

    private static bool Bit(Surface surface, Field count, int k, int index) => k < count.Planes.Count && surface.Read(count.Planes[k], index);

    internal override bool ThroughPort => false;

    internal override Store? Home => have.Home ?? want.Home;

    internal override Surface Underlying =>
        have.Underlying is Workspace ? have.Underlying : want.Underlying is Workspace ? want.Underlying : this;

    internal override bool ReadOnly => true;

    internal override bool IsConstant(int plane) => false;

    internal override void Set(int plane, int index, bool value) =>
        throw new InvalidOperationException("A two-faced gap is a read of have and want. Change one of them.");

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys)
    {
        have.ReadKeys(index, keys);
        want.ReadKeys(index, keys);
    }

    internal override (Surface, int) WriteKey(int index) => (this, index);
}
