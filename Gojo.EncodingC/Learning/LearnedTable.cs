namespace Gojo.EncodingC;

/// <summary>How a learned table reads one (before, after) pair.</summary>
public enum LearnedRead
{
    /// <summary>Witnessed at least the admission threshold.</summary>
    Admitted,

    /// <summary>Its before-state was seen often enough and never went here.</summary>
    Unsupported,

    /// <summary>Not seen enough to say. Prediction through it stays open.</summary>
    Unknown,
}

/// <summary>A transition table learned from what actually happened. Each
/// witnessed (before, after) pair is counted; a pair seen often enough is
/// admitted, a pair whose before-state was seen often enough without it is
/// unsupported, and everything else stays unknown rather than guessed. Once
/// nothing is unknown the table can crystallize into a law, checked against
/// the accounting it is asked to keep like any authored law. A structure that
/// learns its own law this way has decided something from outcomes.</summary>
public sealed class LearnedTable
{
    private readonly Shape[] roles;
    private readonly int[] pairCounts;
    private readonly int[] beforeCounts;
    private readonly List<long>?[] witnesses;

    public LearnedTable(IReadOnlyList<Shape> roles, int admitThreshold = 1, int unsupportedAfter = 4)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (admitThreshold < 1) throw new ArgumentOutOfRangeException(nameof(admitThreshold));
        if (unsupportedAfter < 1) throw new ArgumentOutOfRangeException(nameof(unsupportedAfter));
        this.roles = roles.ToArray();
        StateCount = roles.Aggregate(1, (p, r) => checked(p * r.Size));
        if ((long)StateCount * StateCount > Law.MaxBits) throw new ArgumentException("Table too large.", nameof(roles));
        AdmitThreshold = admitThreshold;
        UnsupportedAfter = unsupportedAfter;
        pairCounts = new int[StateCount * StateCount];
        beforeCounts = new int[StateCount];
        witnesses = new List<long>?[StateCount * StateCount];
    }

    public IReadOnlyList<Shape> Roles => roles;

    public int StateCount { get; }

    public int AdmitThreshold { get; }

    public int UnsupportedAfter { get; }

    public long Observations { get; private set; }

    public void Witness(int before, int after, long tick)
    {
        if ((uint)before >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        if ((uint)after >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(after));
        var bit = before + StateCount * after;
        pairCounts[bit]++;
        beforeCounts[before]++;
        (witnesses[bit] ??= []).Add(tick);
        Observations++;
    }

    /// <summary>Records every row a wiring ran, where both states were codes.</summary>
    public void Witness(WiringStep step, Plane rows, long tick)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows.Indexes())
            if (step.Before[row] >= 0 && step.After[row] >= 0) Witness(step.Before[row], step.After[row], tick);
    }

    public LearnedRead Read(int before, int after)
    {
        var bit = before + StateCount * after;
        if (pairCounts[bit] >= AdmitThreshold) return LearnedRead.Admitted;
        return pairCounts[bit] == 0 && beforeCounts[before] >= UnsupportedAfter ? LearnedRead.Unsupported : LearnedRead.Unknown;
    }

    /// <summary>The ticks that witnessed a pair.</summary>
    public IReadOnlyList<long> WitnessesOf(int before, int after) => witnesses[before + StateCount * after] ?? [];

    /// <summary>Admitted after-states, and whether any after-state is still unknown.</summary>
    public (IReadOnlyList<int> Admitted, bool Open) Predict(int before)
    {
        var admitted = Enumerable.Range(0, StateCount).Where(a => Read(before, a) == LearnedRead.Admitted).ToArray();
        var open = Enumerable.Range(0, StateCount).Any(a => Read(before, a) == LearnedRead.Unknown);
        return (admitted, open);
    }

    public int UnknownCount => Enumerable.Range(0, StateCount * StateCount)
        .Count(bit => Read(bit % StateCount, bit / StateCount) == LearnedRead.Unknown);

    /// <summary>The admitted table as a law, once nothing is unknown. A table that
    /// breaks the accounting is not adopted; the pairs that break it are returned.</summary>
    public (TransitionLaw? Law, int Unknown, IReadOnlyList<Violation> Violations) Crystallize(Accounting accounting)
    {
        ArgumentNullException.ThrowIfNull(accounting);
        var unknown = UnknownCount;
        if (unknown > 0) return (null, unknown, []);
        var pairs = Enumerable.Range(0, StateCount * StateCount)
            .Where(bit => Read(bit % StateCount, bit / StateCount) == LearnedRead.Admitted)
            .Select(bit => (bit % StateCount, bit / StateCount));
        var law = TransitionLaw.TryFromPairs(roles, pairs, accounting, out var violations);
        return (law, 0, violations);
    }
}
