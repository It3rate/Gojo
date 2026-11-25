namespace Gojo.EncodingC;

/// <summary>A few small laws for what is known, as <see cref="Flows"/> has for
/// what moves. Each is only a table. With them a machine observes, forgets and
/// acts on what it has worked out as wirings the engine runs, with no host
/// code in the loop.
/// <para>A value that is not settled is held as one plane per value, set where
/// the value is still possible (<see cref="Supports"/>). To a relation those
/// planes are an open role. To a transition law they are plain bits, read as
/// one code: bit v set means value v is still possible.</para></summary>
public static class Settling
{
    private static readonly Dictionary<Shape, RelationLaw> Sames = [];
    private static readonly Dictionary<int, TransitionLaw> Forgets = [];
    private static readonly Dictionary<Shape, TransitionLaw> Enacts = [];
    private static readonly Dictionary<int, TransitionLaw> Justifies = [];

    /// <summary>Two roles hold the same value. Between a sample and an open
    /// role this is observation: the open role is narrowed to what the sample
    /// reads. Between two open roles it is one thing known in two places.</summary>
    public static RelationLaw Same(Shape shape)
    {
        lock (Sames)
        {
            if (Sames.TryGetValue(shape, out var cached)) return cached;
            return Sames[shape] = RelationLaw.FromPredicate([shape, shape], t => t[0] == t[1]);
        }
    }

    /// <summary>A clock: a count that adds one every step it runs, up to its
    /// capacity. One role, the count.</summary>
    public static TransitionLaw Clock(int capacity) => TransitionLaw.FromFunction(
        [Shape.Unary(capacity)], s => [s[0] < capacity ? s[0] + 1 : s[0]], new Accounting(true, new NeverDecreases(0)));

    /// <summary>Has ever been: a presence that accumulates along a chain of
    /// moments. Roles: before, now, after; after is before or now.</summary>
    public static RelationLaw Ever { get; } = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit, Shape.Bit], t => t[2] == (t[0] | t[1]));

    /// <summary>Holds a possibility while its reasons hold. Roles: the reasons,
    /// one bit each (read), then one support plane of a value as a plain bit
    /// (written). The bit becomes 1 when every reason is 1 and 0 otherwise.
    /// <para><b>Justified, not accumulated.</b> Narrowing only rules out, which is
    /// right for what was learned about the past: which supplier delivered a
    /// batch stays known. A possibility about now, such as "supplier A can be
    /// ordered from", holds for a reason that can stop holding, and narrowing
    /// would keep it ruled out after its reason returns. Reopening it every
    /// step and narrowing it again would write the same bits both ways each
    /// step: taken together they contend, and in order they never go quiet.
    /// So a justified bit has one writer, this law, computing it from its
    /// reasons each step. It reopens when its reasons return, writes only when
    /// it changes, and never contends with itself. This is the
    /// truth-maintenance idea (Doyle, 1979): a belief is held while its
    /// justification is.</para>
    /// <para>Declare it before what it gates, so a gate never reads a
    /// possibility one step stale. A reason that must be absent is read
    /// through a flipped view; all reasons for one bit belong to its one law.</para></summary>
    public static TransitionLaw Justify(int reasons)
    {
        if (reasons is < 1 or > 10) throw new ArgumentOutOfRangeException(nameof(reasons), "A justification has one to ten reasons, so its table stays within Law.MaxBits.");
        lock (Justifies)
        {
            if (Justifies.TryGetValue(reasons, out var cached)) return cached;
            return Justifies[reasons] = TransitionLaw.FromFunction(
                [.. Enumerable.Repeat(Shape.Bit, reasons + 1)],
                state => [.. state[..reasons], state[..reasons].All(bit => bit == 1) ? 1 : 0],
                new Accounting(true, [.. Enumerable.Range(0, reasons).Select(r => (Invariant)new Unchanged(r))]));
        }
    }

    /// <summary>The gap between two open values, as a relation: roles left,
    /// right and disagrees, where disagrees is 1 exactly when left and right
    /// differ. Bound to open supports it narrows every way. Disjoint open
    /// values settle disagrees to 1 without settling either; overlapping ones
    /// leave it open. Holding disagrees to 0 narrows both to what they share,
    /// and holding it to 1 against a settled right rules that value out of
    /// left. This is the gap for values that are not counts, such as which car
    /// a word refers to (from tension_neon).</summary>
    public static RelationLaw ValueGap(Shape shape)
    {
        if ((long)shape.Size * shape.Size * 2 > Law.MaxBits) throw new ArgumentOutOfRangeException(nameof(shape), "The gap table would exceed Law.MaxBits.");
        lock (ValueGaps)
        {
            if (ValueGaps.TryGetValue(shape, out var cached)) return cached;
            return ValueGaps[shape] = RelationLaw.FromPredicate([shape, shape, Shape.Bit], t => t[2] == (t[0] != t[1] ? 1 : 0));
        }
    }

    private static readonly Dictionary<Shape, RelationLaw> ValueGaps = [];

    /// <summary>Two values are the same once something has been heard. Roles:
    /// heard (a bit, read), a sample (read), the value (narrowed while heard is
    /// 1, left alone while it is 0). A listener reading a speaker's words
    /// through a port narrows only after the words have arrived; before, the
    /// speaker's planes may hold anything, and reading them would narrow by
    /// what was never said.</summary>
    public static RelationLaw SameWhen(Shape shape)
    {
        lock (SameWhens)
        {
            if (SameWhens.TryGetValue(shape, out var cached)) return cached;
            return SameWhens[shape] = RelationLaw.FromPredicate([Shape.Bit, shape, shape], t => t[0] == 0 || t[1] == t[2]);
        }
    }

    private static readonly Dictionary<Shape, RelationLaw> SameWhens = [];

    /// <summary>A latch: while a trigger bit is 1, sets a bit to
    /// <paramref name="to"/>, and otherwise leaves it. Roles: trigger (read),
    /// bit (written). Latching a row's life bit to 1 recruits a spare row, and
    /// to 0 retires one: a structural change made by structure, the slow
    /// clock's step (Ashby's ultrastability), when the trigger is a threshold
    /// read of a streak.</summary>
    public static TransitionLaw Latch(bool to) => to ? LatchOn : LatchOff;

    private static readonly TransitionLaw LatchOn = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit],
        s => [s[0], s[0] == 1 ? 1 : s[1]], new Accounting(true, new Unchanged(0), new NeverDecreases(1)));

    private static readonly TransitionLaw LatchOff = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit],
        s => [s[0], s[0] == 1 ? 0 : s[1]], new Accounting(true, new Unchanged(0), new NeverIncreases(1)));

    /// <summary>Opens a value again: sets every value possible. One role, the
    /// planes of a value with <paramref name="values"/> values read as plain
    /// bits. Wired to run every step, it is the passing of time: what was
    /// worked out about a moment does not hold for the next.</summary>
    public static TransitionLaw Forget(int values)
    {
        lock (Forgets)
        {
            if (Forgets.TryGetValue(values, out var cached)) return cached;
            return Forgets[values] = TransitionLaw.FromFunction([Shape.Binary(1 << values)], _ => [(1 << values) - 1], new Accounting(true));
        }
    }

    /// <summary>Writes a settled value into a field's bits, turning what was
    /// worked out into what is. Roles: the value's planes as plain bits (read),
    /// then the field (written). While the value is open or contradicted the
    /// field keeps its bits.</summary>
    public static TransitionLaw Enact(Shape shape)
    {
        lock (Enacts)
        {
            if (Enacts.TryGetValue(shape, out var cached)) return cached;
            return Enacts[shape] = TransitionLaw.FromFunction(
                [Shape.Binary(1 << shape.Size), shape],
                state =>
                {
                    var left = Enumerable.Range(0, shape.Size).Where(v => ((state[0] >> v) & 1) != 0).ToArray();
                    return left.Length == 1 ? [state[0], left[0]] : state;
                },
                new Accounting(true, new Unchanged(0)));
        }
    }
}
