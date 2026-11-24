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
    private readonly (Surface Surface, Field Count)[] have;
    private readonly (Surface Surface, Field Count)[] want;
    private readonly int capacity;

    public Difference(Surface have, Field haveCount, Surface want, Field wantCount)
        : this([(have, haveCount)], [(want, wantCount)])
    {
    }

    /// <summary>The gap between two sums of unary counts at each position: what
    /// is held across several places against what is wanted across several.
    /// A shop's stock on the shelf and at the dock, against its target and the
    /// orders still owed, is one comparison, not four.</summary>
    public Difference(IReadOnlyList<(Surface Surface, Field Count)> have, IReadOnlyList<(Surface Surface, Field Count)> want)
    {
        ArgumentNullException.ThrowIfNull(have);
        ArgumentNullException.ThrowIfNull(want);
        var terms = have.Concat(want).ToArray();
        if (have.Count == 0 || want.Count == 0) throw new ArgumentException("Compare at least one count with at least one.");
        if (terms.Any(t => t.Surface is null || t.Count is null || !t.Count.Shape.IsUnary)) throw new ArgumentException("A two-faced gap compares unary counts.");
        if (terms.Select(t => t.Surface.Home).OfType<Store>().Distinct().Skip(1).Any())
            throw new ArgumentException("Have and want belong to one local structure: fields of one store, a store and a workspace over it, or a store and a port.");
        if (terms.Any(t => t.Surface.Count != terms[0].Surface.Count)) throw new ArgumentException("Have and want are read at the same positions.");
        this.have = [.. have];
        this.want = [.. want];
        int Total(IEnumerable<(Surface, Field Count)> side) => side.Sum(t => t.Count.Planes.Count);
        capacity = Total(have) > Total(want) ? Total(have) : Total(want);
        Deficit = new Field(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity)]);
        Excess = new Field(Shape.Unary(capacity), [.. Enumerable.Range(capacity, capacity)]);
    }

    /// <summary>How far have is below want, as a unary count on planes 0 to
    /// capacity − 1.</summary>
    public Field Deficit { get; }

    /// <summary>How far have is above want, as a unary count on the next planes.</summary>
    public Field Excess { get; }

    public override int Count => have[0].Surface.Count;

    public override int PlaneCount => 2 * capacity;

    public override bool IsLive(int index) => have.Concat(want).All(t => t.Surface.IsLive(index));

    /// <summary>Bit k of the deficit: want exceeds have by more than k, which
    /// holds when, for some m, want is at least m and have is not at least
    /// m − k. Whether a sum of unary counts is at least m is itself read from
    /// their bits: the first count is at least i and the rest at least m − i,
    /// for some i. With one count each side this is the shifted AND.</summary>
    public override bool Read(int plane, int index)
    {
        if ((uint)plane >= (uint)PlaneCount) throw new ArgumentOutOfRangeException(nameof(plane));
        var (low, high, k) = plane < capacity ? (have, want, plane) : (want, have, plane - capacity);
        var top = high.Sum(t => t.Count.Planes.Count);
        for (var m = 1; m <= top; m++)
            if (AtLeast(high, 0, m, index) && !AtLeast(low, 0, m - k, index)) return true;
        return false;
    }

    private static bool AtLeast((Surface Surface, Field Count)[] terms, int from, int total, int index)
    {
        if (total <= 0) return true;
        if (from == terms.Length) return false;
        var (surface, count) = terms[from];
        for (var i = 0; i <= count.Planes.Count; i++)
        {
            if (i > 0 && !surface.Read(count.Planes[i - 1], index)) return false;
            if (AtLeast(terms, from + 1, total - i, index)) return true;
        }
        return false;
    }

    internal override bool ThroughPort => false;

    internal override Store? Home => have.Concat(want).Select(t => t.Surface.Home).OfType<Store>().FirstOrDefault();

    internal override Surface Underlying =>
        have.Concat(want).Select(t => t.Surface.Underlying).OfType<Workspace>().FirstOrDefault() ?? (Surface)this;

    internal override bool ReadOnly => true;

    internal override bool IsConstant(int plane) => false;

    internal override void Set(int plane, int index, bool value) =>
        throw new InvalidOperationException("A two-faced gap is a read of have and want. Change one of them.");

    internal override (Surface Home, int Index, int Plane) Bit(int index, int plane) => (this, index, plane);

    internal override void ReadKeys(int index, List<(Surface, int)> keys)
    {
        foreach (var (surface, _) in have.Concat(want)) surface.ReadKeys(index, keys);
    }

    internal override (Surface, int) WriteKey(int index) => (this, index);
}
