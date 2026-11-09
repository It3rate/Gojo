namespace Gojo.EncodingB;

/// <summary>A map between a fine and a coarse rung. The abstraction α sends each
/// fine value to a coarse one; the concretization γ gives the set of fine values
/// a coarse value admits. Together they form a Galois connection. γ cannot
/// recover detail α discarded; it only bounds it.</summary>
public sealed class RungMap
{
    private readonly int[] abstraction;

    public RungMap(string name, Domain fine, Domain coarse, Func<int, int> alpha)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(fine);
        ArgumentNullException.ThrowIfNull(coarse);
        ArgumentNullException.ThrowIfNull(alpha);
        Name = name;
        Fine = fine;
        Coarse = coarse;
        abstraction = Enumerable.Range(0, fine.Size).Select(alpha).ToArray();
        if (abstraction.Any(c => c < 0 || c >= coarse.Size))
            throw new ArgumentException("α must map every fine value into the coarse domain.", nameof(alpha));
    }

    public string Name { get; }
    public Domain Fine { get; }
    public Domain Coarse { get; }

    public int Abstract(int fine) => abstraction[fine];

    /// <summary>α over a support: every coarse value some admitted fine value reads as.</summary>
    public ulong Abstract(ulong fineSupport) =>
        Bits.Indexes(fineSupport).Aggregate(0UL, (coarse, fine) => coarse | Bits.Of(abstraction[fine]));

    /// <summary>γ: every fine value compatible with a coarse value.</summary>
    public ulong Concretize(int coarse) =>
        Enumerable.Range(0, Fine.Size).Where(f => abstraction[f] == coarse).Aggregate(0UL, (m, f) => m | Bits.Of(f));

    public ulong Concretize(ulong coarseSupport) =>
        Bits.Indexes(coarseSupport).Aggregate(0UL, (fine, coarse) => fine | Concretize(coarse));

    /// <summary>Soundness: a finer read never contradicts a valid coarser one.
    /// True when every admitted fine value abstracts into the coarse support.</summary>
    public bool Consistent(ulong fineSupport, ulong coarseSupport) => (Abstract(fineSupport) & ~coarseSupport) == 0;

    /// <summary>Refines a coarse read with fine evidence: the fine values both admit.</summary>
    public ulong Refine(ulong coarseSupport, ulong fineEvidence) => Concretize(coarseSupport) & fineEvidence;
}

/// <summary>Rung constructions and pooling.</summary>
public static class Rungs
{
    /// <summary>R1 → R0: a thermometer magnitude 0..capacity read as detectable or not.
    /// α(t) = t ≠ 0; γ(detectable) = {1 … capacity}.</summary>
    public static RungMap Presence(int capacity)
    {
        if (capacity is < 1 or > 63) throw new ArgumentOutOfRangeException(nameof(capacity));
        var fine = new Domain("magnitude", Enumerable.Range(0, capacity + 1).Select(i => i.ToString()).ToArray());
        var coarse = Domain.Binary("presence", "not detectable", "detectable");
        return new RungMap($"presence of magnitude 0..{capacity}", fine, coarse, t => t == 0 ? 0 : 1);
    }

    /// <summary>Coarse presence as an OR-pool: coarse bit g is set when any child
    /// site in group g is set. The reduction a mipmap performs.</summary>
    public static ulong OrPool(ulong children, IReadOnlyList<ulong> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        if (groups.Count > Bits.Width) throw new ArgumentException("At most 64 groups.", nameof(groups));
        ulong coarse = 0;
        for (var g = 0; g < groups.Count; g++)
            if ((children & groups[g]) != 0) coarse |= Bits.Of(g);
        return coarse;
    }

    /// <summary>A coarse count as a sum-pool with carry residue: the total in
    /// whole coarse units, and what remains below one unit. Exact: nothing lost.</summary>
    public static CoarseRead SumPool(IEnumerable<int> childCounts, int unit)
    {
        ArgumentNullException.ThrowIfNull(childCounts);
        var total = childCounts.Sum();
        return CoarseRead.Exact(total, unit);
    }
}

/// <summary>A value read in coarse units. With its residue it is exact. Without
/// it, the lower detail lies outside the read's boundary: the read admits a range
/// and promises no reconstruction. "Hundreds = 3" is not "300".</summary>
public sealed record CoarseRead(int Coarse, int Unit, int? Residue)
{
    public static CoarseRead Exact(int value, int unit)
    {
        if (unit < 1) throw new ArgumentOutOfRangeException(nameof(unit));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        return new CoarseRead(value / unit, unit, value % unit);
    }

    /// <summary>A coarse witness with no lower detail supplied.</summary>
    public static CoarseRead Witness(int coarse, int unit) => new(coarse, unit, null);

    public bool IsExact => Residue is not null;

    /// <summary>The smallest and largest fine values this read admits.</summary>
    public (int Low, int High) Range =>
        Residue is int residue ? (Coarse * Unit + residue, Coarse * Unit + residue) : (Coarse * Unit, Coarse * Unit + Unit - 1);

    /// <summary>The fine value, when the residue participated.</summary>
    public int? Value => Residue is int residue ? Coarse * Unit + residue : null;
}
