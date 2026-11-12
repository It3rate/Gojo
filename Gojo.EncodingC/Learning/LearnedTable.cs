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
/// <para><b>The counts are a store.</b> Everything the table knows is in
/// <see cref="Counts"/>: one position per pair, then one per before-state,
/// then one for rows it could not read, each holding a small unary count. So
/// what was learned is saved with a machine, a law can read how sure the
/// table is, and nothing here is kept beside the encoding.</para>
/// <para><b>Small on purpose.</b> A count stops at the largest threshold: "often
/// enough" is all it has to say. No history of when things happened is kept.
/// A law that fits predicts its own history, so only what it gets wrong is
/// worth storing, and counting is the first step toward such a law, not a
/// record to grow.</para>
/// <para>"Unsupported" is a rule of thumb, not proof that a pair cannot happen.
/// A crystallized law marks those exclusions (<see cref="Crystallized.Unsupported"/>),
/// the counts they came from stay in the store, and the table keeps
/// counting, so a later witness can admit the pair again.</para></summary>
public sealed class LearnedTable
{
    private readonly Shape[] roles;
    private readonly Field count;

    public LearnedTable(IReadOnlyList<Shape> roles, int admitThreshold = 1, int unsupportedAfter = 4)
        : this(roles, null, admitThreshold, unsupportedAfter)
    {
    }

    /// <summary>A table over counts that already exist, as a saved machine has them.</summary>
    public LearnedTable(IReadOnlyList<Shape> roles, Store? counts, int admitThreshold = 1, int unsupportedAfter = 4)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (admitThreshold is < 1 or > 63) throw new ArgumentOutOfRangeException(nameof(admitThreshold));
        if (unsupportedAfter is < 1 or > 63) throw new ArgumentOutOfRangeException(nameof(unsupportedAfter));
        this.roles = roles.ToArray();
        Roles = Array.AsReadOnly(this.roles);
        StateCount = roles.Aggregate(1, (p, r) => checked(p * r.Size));
        if ((long)StateCount * StateCount > Law.MaxBits) throw new ArgumentException("Table too large.", nameof(roles));
        AdmitThreshold = admitThreshold;
        UnsupportedAfter = unsupportedAfter;
        var capacity = admitThreshold > unsupportedAfter ? admitThreshold : unsupportedAfter;
        var positions = StateCount * StateCount + StateCount + 1;
        if (counts is not null && (counts.PlaneCount != capacity || counts.Count != positions))
            throw new ArgumentException($"These counts are not this table's: it needs {capacity} planes over {positions} positions.", nameof(counts));
        Counts = counts ?? new Store(capacity, positions);
        count = new Field(Shape.Unary(capacity), Enumerable.Range(0, capacity).ToArray());
    }

    public IReadOnlyList<Shape> Roles { get; }

    public int StateCount { get; }

    public int AdmitThreshold { get; }

    public int UnsupportedAfter { get; }

    /// <summary>The counts: position <c>before + N·after</c> for each pair, then
    /// <c>N² + before</c> for each before-state, then one position for rows that
    /// could not be read. Each is a unary count that stops at the larger
    /// threshold.</summary>
    public Store Counts { get; }

    /// <summary>The pairs seen often enough to admit, one bit per pair at
    /// <c>before + N·after</c>. It is one plane of <see cref="Counts"/>, the one
    /// that says "at least the threshold": what was learned is already a table
    /// in the planes, before anything turns it into a law.</summary>
    public Plane Admitted => Plane.Of(StateCount * StateCount, Counts.Read(AdmitThreshold - 1).Indexes().Where(bit => bit < StateCount * StateCount));

    /// <summary>Rows the table was shown and could not learn from: their bits
    /// were not codes, or the row did not run. A learner should know how much
    /// it did not see.</summary>
    public int Unread => Held(StateCount * StateCount + StateCount);

    /// <summary>How many times a pair was witnessed, up to where counting stops.</summary>
    public int Count(int before, int after) => Held(Pair(before, after));

    /// <summary>How many times a before-state was witnessed, whatever followed.
    /// With <see cref="Count"/> this is the evidence behind a read: how sure
    /// the table can be, not only what it says.</summary>
    public int Seen(int before)
    {
        if ((uint)before >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        return Held(StateCount * StateCount + before);
    }

    public void Witness(int before, int after) =>
        Unary.Add(Counts, count, Plane.Of(Counts.Count, Pair(before, after), StateCount * StateCount + before));

    /// <summary>Records every row a wiring ran, where both states were codes.
    /// The rest are counted in <see cref="Unread"/>.</summary>
    public void Witness(WiringStep step, Plane rows)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(rows);
        foreach (var row in rows.Indexes())
            if (step.Before[row] >= 0 && step.After[row] >= 0) Witness(step.Before[row], step.After[row]);
            else Unary.Add(Counts, count, Plane.Of(Counts.Count, Counts.Count - 1));
    }

    public LearnedRead Read(int before, int after)
    {
        var seen = Count(before, after);
        if (seen >= AdmitThreshold) return LearnedRead.Admitted;
        return seen == 0 && Seen(before) >= UnsupportedAfter ? LearnedRead.Unsupported : LearnedRead.Unknown;
    }

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

    private int Pair(int before, int after)
    {
        if ((uint)before >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(before));
        if ((uint)after >= (uint)StateCount) throw new ArgumentOutOfRangeException(nameof(after));
        return before + StateCount * after;
    }

    private int Held(int position) => count.Read(Counts, position) ?? 0;
}

/// <summary>What crystallizing a learned table gave: the law, or null with the
/// number of unknown pairs or the pairs that break the accounting.
/// <see cref="Unsupported"/> has one bit per table bit (before + N·after) for
/// each pair the law leaves out only because it was not seen.</summary>
public sealed record Crystallized(TransitionLaw? Law, int Unknown, IReadOnlyList<Violation> Violations, Plane Unsupported);
