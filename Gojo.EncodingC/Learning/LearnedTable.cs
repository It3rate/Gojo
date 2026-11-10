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
/// learns its own law this way has decided something from outcomes.
/// <para>"Unsupported" is a rule of thumb, not proof that a pair cannot happen.
/// A crystallized law keeps those exclusions apart (<see cref="Crystallized.Unsupported"/>),
/// and the table keeps counting, so a later witness can admit the pair again.</para>
/// <para>History is kept small: counts, and the last few ticks that witnessed
/// each pair. Counting is the first step; a table this size is meant to give
/// way to structure that predicts, once a simple rule fits.</para></summary>
public sealed class LearnedTable
{
    private readonly Shape[] roles;
    private readonly int[] pairCounts;
    private readonly int[] beforeCounts;
    private readonly List<long>?[] witnesses;

    public LearnedTable(IReadOnlyList<Shape> roles, int admitThreshold = 1, int unsupportedAfter = 4, int samples = 4)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (admitThreshold < 1) throw new ArgumentOutOfRangeException(nameof(admitThreshold));
        if (unsupportedAfter < 1) throw new ArgumentOutOfRangeException(nameof(unsupportedAfter));
        if (samples < 0) throw new ArgumentOutOfRangeException(nameof(samples));
        Samples = samples;
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

    /// <summary>How many of the latest witnessing ticks each pair keeps.</summary>
    public int Samples { get; }

    public long Observations { get; private set; }

    public void Witness(int before, int after, long tick)
    {
        if ((uint)before >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        if ((uint)after >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(after));
        var bit = before + StateCount * after;
        pairCounts[bit]++;
        beforeCounts[before]++;
        if (Samples > 0)
        {
            var kept = witnesses[bit] ??= [];
            kept.Add(tick);
            if (kept.Count > Samples) kept.RemoveAt(0);
        }
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

    /// <summary>The latest ticks that witnessed a pair, at most <see cref="Samples"/>.</summary>
    public IReadOnlyList<long> WitnessesOf(int before, int after) => witnesses[before + StateCount * after]?.AsReadOnly() ?? (IReadOnlyList<long>)[];

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
    /// breaks the accounting is not adopted; the pairs that break it are returned.
    /// <see cref="Crystallized.Unsupported"/> marks, over the table's bits, the
    /// pairs left out only because they were not seen.</summary>
    public Crystallized Crystallize(Accounting accounting)
    {
        ArgumentNullException.ThrowIfNull(accounting);
        var unknown = UnknownCount;
        var unsupported = new Plane(StateCount * StateCount);
        for (var bit = 0; bit < unsupported.Length; bit++)
            if (Read(bit % StateCount, bit / StateCount) == LearnedRead.Unsupported) unsupported[bit] = true;
        if (unknown > 0) return new Crystallized(null, unknown, [], unsupported);
        var pairs = Enumerable.Range(0, StateCount * StateCount)
            .Where(bit => Read(bit % StateCount, bit / StateCount) == LearnedRead.Admitted)
            .Select(bit => (bit % StateCount, bit / StateCount));
        var law = TransitionLaw.TryFromPairs(roles, pairs, accounting, out var violations);
        return new Crystallized(law, 0, violations, unsupported);
    }
}

/// <summary>What crystallizing a learned table gave: the law, or null with the
/// number of unknown pairs or the pairs that break the accounting.
/// <see cref="Unsupported"/> has one bit per table bit (before + N·after) for
/// each pair the law leaves out only because it was not seen.</summary>
public sealed record Crystallized(TransitionLaw? Law, int Unknown, IReadOnlyList<Violation> Violations, Plane Unsupported);
