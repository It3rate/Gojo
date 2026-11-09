namespace Gojo.EncodingB;

/// <summary>How a learned table reads one (before, after) pair.</summary>
public enum LearnedRead
{
    /// <summary>Witnessed at least the admission threshold.</summary>
    Admitted,

    /// <summary>Its before-state was seen often enough, and never went here.</summary>
    Unsupported,

    /// <summary>Neither: not yet observed enough to say. Prediction through it is open.</summary>
    Unknown,
}

/// <summary>A prediction from a learned table: the admitted after-states, and
/// whether any after-state is still unknown (an open result, not a guess).</summary>
public sealed record LearnedPrediction(IReadOnlyList<int> Admitted, bool Open);

/// <summary>Learning rung L3: a transition table acquired from witnessed
/// transitions. Two tables over the same tuple index, A (admitted) and U
/// (unsupported); unknown is <c>~A &amp; ~U</c>. Every admitted pair cites the ticks
/// that witnessed it. Laws thereby get the same non-signal structure as values.</summary>
public sealed class LearnedTransitionTable
{
    private readonly int[] pairCounts;
    private readonly int[] beforeCounts;
    private readonly List<long>[] witnesses;

    public LearnedTransitionTable(string name, IReadOnlyList<LawRole> roles, int admitThreshold = 1, int unsupportedAfter = 4)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(roles);
        if (admitThreshold < 1) throw new ArgumentOutOfRangeException(nameof(admitThreshold));
        if (unsupportedAfter < 1) throw new ArgumentOutOfRangeException(nameof(unsupportedAfter));
        Name = name;
        Roles = roles.ToArray();
        StateCount = roles.Aggregate(1, (p, r) => checked(p * r.Domain.Size));
        if ((long)StateCount * StateCount > LawTable.MaxBits) throw new ArgumentException("Table too large.", nameof(roles));
        AdmitThreshold = admitThreshold;
        UnsupportedAfter = unsupportedAfter;
        pairCounts = new int[StateCount * StateCount];
        beforeCounts = new int[StateCount];
        witnesses = new List<long>[StateCount * StateCount];
    }

    public string Name { get; }
    public IReadOnlyList<LawRole> Roles { get; }
    public int StateCount { get; }
    public int AdmitThreshold { get; }
    public int UnsupportedAfter { get; }
    public long Observations { get; private set; }

    public void Witness(int before, int after, long tick)
    {
        if (before < 0 || before >= StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        if (after < 0 || after >= StateCount) throw new ArgumentOutOfRangeException(nameof(after));
        var bit = before + StateCount * after;
        pairCounts[bit]++;
        beforeCounts[before]++;
        (witnesses[bit] ??= []).Add(tick);
        Observations++;
    }

    /// <summary>Records every row transition of a law application.</summary>
    public void Witness(TransitionWitness applied, long tick)
    {
        ArgumentNullException.ThrowIfNull(applied);
        foreach (var row in applied.Transitions) Witness(row.Before, row.After, tick);
    }

    public LearnedRead Read(int before, int after)
    {
        var bit = before + StateCount * after;
        if (pairCounts[bit] >= AdmitThreshold) return LearnedRead.Admitted;
        return pairCounts[bit] == 0 && beforeCounts[before] >= UnsupportedAfter ? LearnedRead.Unsupported : LearnedRead.Unknown;
    }

    public IReadOnlyList<long> WitnessesOf(int before, int after) => witnesses[before + StateCount * after] ?? [];

    public LearnedPrediction Predict(int before)
    {
        var admitted = Enumerable.Range(0, StateCount).Where(a => Read(before, a) == LearnedRead.Admitted).ToArray();
        var open = Enumerable.Range(0, StateCount).Any(a => Read(before, a) == LearnedRead.Unknown);
        return new LearnedPrediction(admitted, open);
    }

    /// <summary>Pairs still unknown. Zero means every pair is decided.</summary>
    public int UnknownCount => Enumerable.Range(0, StateCount * StateCount)
        .Count(bit => Read(bit % StateCount, bit / StateCount) == LearnedRead.Unknown);

    /// <summary>Crystallizes the admitted table into a law once nothing is unknown.
    /// The law is checked against the given signature like any authored law; a
    /// table that breaks it is refused with the reason, not adopted.</summary>
    public (TransitionLaw? Law, string Read) Crystallize(AccountingSignature signature, ConfluenceClass confluence)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (UnknownCount > 0) return (null, $"{UnknownCount} pairs are still unknown; the table stays open");
        var pairs = Enumerable.Range(0, StateCount * StateCount)
            .Where(bit => Read(bit % StateCount, bit / StateCount) == LearnedRead.Admitted)
            .Select(bit => (bit % StateCount, bit / StateCount));
        try
        {
            var law = TransitionLaw.FromPairs(Name, Roles, pairs, signature, confluence,
                $"learned from {Observations} witnessed transitions (threshold {AdmitThreshold}, unsupported after {UnsupportedAfter})");
            return (law, "crystallized");
        }
        catch (ArgumentException refused)
        {
            return (null, refused.Message);
        }
    }
}
