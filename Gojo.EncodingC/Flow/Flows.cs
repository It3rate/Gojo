namespace Gojo.EncodingC;

/// <summary>A few small laws for moving things. Each is only a table and its
/// accounting; nothing else knows these laws by name.</summary>
public static class Flows
{
    private static readonly Dictionary<(int, int, int, int), TransitionLaw> Converts = [];
    private static readonly Dictionary<(int, int), TransitionLaw> GatedMoves = [];

    /// <summary>The smallest move: source S, gate C, target T, one bit each;
    /// state <c>S + 2C + 4T</c>. A whole unit hands off when S is set, the gate
    /// is open and T is clear. The table is the eight bytes
    /// <c>01 02 04 00 10 20 48 80</c>.</summary>
    public static TransitionLaw Passage { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit],
        state => state is [1, 1, 0] ? [0, 1, 1] : state,
        new Accounting(true, new PreservedSum([0, 2]), new Unchanged(1), new NeverIncreases(0), new NeverDecreases(2)));

    /// <summary>Flips one bit every step. The simplest driven structure: an oscillator.</summary>
    public static TransitionLaw Toggle { get; } = TransitionLaw.FromFunction(
        [Shape.Bit],
        state => [1 - state[0]],
        new Accounting(true));

    /// <summary>One unit from a unary source to a unary target, one to one and
    /// uncalibrated, when the source has one and the target has room. Roles:
    /// source, target.</summary>
    public static TransitionLaw Move(int sourceCapacity, int targetCapacity) => Convert(1, 1, sourceCapacity, targetCapacity);

    /// <summary>Moves one unit from source to target and clears one outstanding
    /// unit only when the target has room. Roles: source, outstanding, target.
    /// Preserves source plus target and outstanding plus target.</summary>
    public static TransitionLaw Fulfill(int sourceCapacity, int demandCapacity, int targetCapacity) =>
        TransitionLaw.FromFunction(
            [Shape.Unary(sourceCapacity), Shape.Unary(demandCapacity), Shape.Unary(targetCapacity)],
            state => state[0] > 0 && state[1] > 0 && state[2] < targetCapacity
                ? [state[0] - 1, state[1] - 1, state[2] + 1]
                : state,
            new Accounting(true, new PreservedSum([0, 2]), new PreservedSum([1, 2])));

    /// <summary>Fulfils one pending case and records its receipt when the
    /// target has room for all its units and the tally has room for one case.
    /// Roles: source cases, pending cases, target units, received cases.</summary>
    public static TransitionLaw FulfillBatch(int sourceCapacity, int demandCapacity, int targetCapacity, int unitsPerBatch, int receiptCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitsPerBatch);
        return TransitionLaw.FromFunction(
            [Shape.Unary(sourceCapacity), Shape.Unary(demandCapacity), Shape.Unary(targetCapacity), Shape.Unary(receiptCapacity)],
            state => state[0] > 0 && state[1] > 0 && state[2] <= targetCapacity - unitsPerBatch && state[3] < receiptCapacity
                ? [state[0] - 1, state[1] - 1, state[2] + unitsPerBatch, state[3] + 1]
                : state,
            new Accounting(true,
                new PreservedSum([0, 2], [unitsPerBatch, 1]),
                new PreservedSum([1, 3]),
                new PreservedSum([0, 3])));
    }

    /// <summary>Takes <paramref name="take"/> units from the source and gives
    /// <paramref name="give"/> to the target, or nothing when the source has too
    /// few or the target too little room. The accounting preserves
    /// give·source + take·target, so the rate is stated in the law. This is a
    /// calibration, chosen by whoever wires it; a plain move is the 1:1 case.
    /// Roles: source, target.</summary>
    public static TransitionLaw Convert(int take, int give, int sourceCapacity, int targetCapacity)
    {
        if (take < 1 || give < 1) throw new ArgumentOutOfRangeException(nameof(take), "Take and give at least one unit.");
        lock (Converts)
        {
            var key = (take, give, sourceCapacity, targetCapacity);
            if (Converts.TryGetValue(key, out var cached)) return cached;
            var law = TransitionLaw.FromFunction(
                [Shape.Unary(sourceCapacity), Shape.Unary(targetCapacity)],
                state => state[0] >= take && state[1] + give <= targetCapacity ? [state[0] - take, state[1] + give] : state,
                new Accounting(true, new PreservedSum([0, 1], [give, take]), new NeverIncreases(0), new NeverDecreases(1)));
            Converts[key] = law;
            return law;
        }
    }

    /// <summary>Appends read-only gate bits to a functional law. The law runs
    /// while every gate is set and otherwise holds, keeping its accounting.</summary>
    public static TransitionLaw When(TransitionLaw law, int gates = 1)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(gates);
        var bitLimit = Law.MaxBits;
        for (var gate = 0; gate < gates; gate++)
        {
            // One gate doubles the state count and quadruples the table.
            bitLimit /= 4;
            if (law.BitCount > bitLimit) throw new ArgumentOutOfRangeException(nameof(gates), "The gated law table would exceed the size limit.");
        }
        if (!law.IsFunctional) throw new ArgumentException("A gated law needs exactly one next state for each state.", nameof(law));

        var own = law.Roles.Count;
        var roles = law.Roles.Concat(Enumerable.Repeat(Shape.Bit, gates)).ToArray();
        var invariants = law.Accounting.Invariants.Concat(Enumerable.Range(own, gates).Select(role => new Unchanged(role))).ToArray();
        return TransitionLaw.FromFunction(roles, state =>
        {
            var gateBits = state.AsSpan(own);
            if (gateBits.Contains(0)) return state;
            var after = law.Afters(law.TupleIndex(state.AsSpan(0, own)))[0];
            return [.. law.Tuple(after), .. gateBits];
        }, new Accounting(true, invariants));
    }

    /// <summary>A move that happens only while a gate bit is set. Roles: source,
    /// gate, target. Branches are built from these: the gates decide which
    /// target a unit goes to.</summary>
    public static TransitionLaw GatedMove(int sourceCapacity, int targetCapacity)
    {
        lock (GatedMoves)
        {
            if (GatedMoves.TryGetValue((sourceCapacity, targetCapacity), out var cached)) return cached;
            var law = TransitionLaw.FromFunction(
                [Shape.Unary(sourceCapacity), Shape.Bit, Shape.Unary(targetCapacity)],
                state => state[0] > 0 && state[1] == 1 && state[2] < targetCapacity ? [state[0] - 1, 1, state[2] + 1] : state,
                new Accounting(true, new PreservedSum([0, 2]), new Unchanged(1), new NeverIncreases(0), new NeverDecreases(2)));
            GatedMoves[(sourceCapacity, targetCapacity)] = law;
            return law;
        }
    }
}
