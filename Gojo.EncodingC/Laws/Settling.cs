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

    /// <summary>Holds a possibility bit while all <paramref name="reasons"/>
    /// hold (truth maintenance, Doyle 1979): writes 1 when every reason bit is
    /// 1 and 0 otherwise. Unlike monotonic narrowing, a justified choice
    /// reopens automatically when its reasons return, writes only when changed
    /// so <see cref="Engine.Settle"/> goes quiet, and has one writer so it
    /// never self-contends under <see cref="Engine.StepTogether"/>. Roles:
    /// reason bits (read), then the support plane as a plain bit (written).</summary>
    public static TransitionLaw Justify(int reasons)
    {
        if (reasons < 1) throw new ArgumentOutOfRangeException(nameof(reasons), "A justification needs at least one reason.");
        lock (Justifies)
        {
            if (Justifies.TryGetValue(reasons, out var cached)) return cached;
            return Justifies[reasons] = TransitionLaw.FromFunction(
                [.. Enumerable.Repeat(Shape.Bit, reasons + 1)],
                state => [.. state[..reasons], state[..reasons].All(bit => bit == 1) ? 1 : 0],
                new Accounting(true, [.. Enumerable.Range(0, reasons).Select(r => (Invariant)new Unchanged(r))]));
        }
    }

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
