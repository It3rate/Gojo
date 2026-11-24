namespace Gojo.EncodingC;

/// <summary>A few small laws for moving things. Each is only a table and its
/// accounting; nothing else knows these laws by name.</summary>
public static class Flows
{
    private static readonly Dictionary<(int, int, int, int), TransitionLaw> Converts = [];
    private static readonly Dictionary<(int, int), TransitionLaw> GatedMoves = [];
    private static readonly Dictionary<(int, int, int, int, int), TransitionLaw> Fulfills = [];

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

    /// <summary>Fills an outstanding order: takes <paramref name="take"/> units
    /// from a source, clears one unit of order, and gives
    /// <paramref name="give"/> to a target, all at once, or nothing when the
    /// source is short, nothing is on order or the target has no room. Roles:
    /// source, order, target. Both the material and the order are accounted:
    /// give · source + take · target and give · order + target are preserved,
    /// so a unit never arrives without the order it answers, and an order is
    /// only closed by what arrives.
    /// <para>A delivery to a customer is this law from the shelf, against a
    /// backorder, into a pickup counter; a supplier's delivery is it from the
    /// supplier's stock, against an order on the books, into the dock. A draw
    /// that only empties the value leaves the unit nowhere; this one keeps it.</para></summary>
    public static TransitionLaw Fulfill(int sourceCapacity, int orderCapacity, int targetCapacity, int take = 1, int give = 1)
    {
        if (take < 1 || give < 1) throw new ArgumentOutOfRangeException(nameof(take), "Take and give at least one unit.");
        lock (Fulfills)
        {
            var key = (sourceCapacity, orderCapacity, targetCapacity, take, give);
            if (Fulfills.TryGetValue(key, out var cached)) return cached;
            return Fulfills[key] = TransitionLaw.FromFunction(
                [Shape.Unary(sourceCapacity), Shape.Unary(orderCapacity), Shape.Unary(targetCapacity)],
                s => s[0] >= take && s[1] >= 1 && s[2] + give <= targetCapacity ? [s[0] - take, s[1] - 1, s[2] + give] : s,
                new Accounting(true, new PreservedSum([0, 2], [give, take]), new PreservedSum([1, 2], [give, 1])));
        }
    }

    /// <summary>Whose turn it is between two routes that would contend for one
    /// count: 0 for the first, 1 for the second. Roles: first has work, second
    /// has work (read), turn (written). With work on both sides the turn
    /// alternates; with work on one side it goes there; with none it holds, so
    /// a structure taking turns still settles. A route waits on the turn bit
    /// itself or on its flipped view.</summary>
    public static TransitionLaw Turn { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit],
        s => [s[0], s[1], s[0] == 1 && s[1] == 1 ? 1 - s[2] : s[0] == 1 ? 0 : s[1] == 1 ? 1 : s[2]],
        new Accounting(true, new Unchanged(0), new Unchanged(1)));

    /// <summary>The same law, waiting on <paramref name="gates"/> gate bits
    /// appended after its own roles: it acts only while every gate is 1, and
    /// holds otherwise. Gates are read, never written, and the law's own
    /// promises are kept, so a conserving move stays a conserving move.
    /// <para>Conditions compose this way: a reorder that runs only while the
    /// shop is short <em>and</em> this supplier is still possible is a move
    /// with two gates. A gated move with one gate was enough while every
    /// fixture had one condition.</para></summary>
    public static TransitionLaw When(TransitionLaw law, int gates = 1)
    {
        ArgumentNullException.ThrowIfNull(law);
        if (gates < 1) throw new ArgumentOutOfRangeException(nameof(gates), "Wait on at least one gate.");
        if (!law.IsFunctional) throw new ArgumentException("A gated law runs on concrete state, so the law must admit one next state for each state.", nameof(law));
        var own = law.Roles.Count;
        var roles = law.Roles.Concat(Enumerable.Repeat(Shape.Bit, gates)).ToArray();
        var invariants = law.Accounting.Invariants.Concat(Enumerable.Range(own, gates).Select(r => (Invariant)new Unchanged(r))).ToArray();
        return TransitionLaw.FromFunction(roles, state =>
        {
            var inner = state[..own];
            if (state[own..].Any(gate => gate == 0)) return state;
            var afters = law.Afters(law.TupleIndex(inner));
            return afters.Count == 0 ? null : [.. law.Tuple(afters[0]), .. state[own..]];
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
