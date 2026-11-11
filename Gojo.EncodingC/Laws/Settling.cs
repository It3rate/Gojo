namespace Gojo.EncodingC;

/// <summary>A few small laws for what is known, as <see cref="Flows"/> has for
/// what moves. Each is only a table. With them a machine observes, forgets and
/// acts on what it has worked out as wirings the engine runs, with no host
/// code in the loop.
/// <para>A value that is not settled is held as one plane per value, set where
/// the value is ruled out (<see cref="Supports"/>). To a relation those planes
/// are an open role. To a transition law they are plain bits, read as one
/// code: bit v set means value v is ruled out.</para></summary>
public static class Settling
{
    private static readonly Dictionary<Shape, RelationLaw> Sames = [];
    private static readonly Dictionary<int, TransitionLaw> Forgets = [];
    private static readonly Dictionary<Shape, TransitionLaw> Enacts = [];

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

    /// <summary>Opens a value again: clears every ruled-out bit. One role, the
    /// planes of a value with <paramref name="values"/> values read as plain
    /// bits. Wired to run every step, it is the passing of time: what was
    /// worked out about a moment does not hold for the next.</summary>
    public static TransitionLaw Forget(int values)
    {
        lock (Forgets)
        {
            if (Forgets.TryGetValue(values, out var cached)) return cached;
            return Forgets[values] = TransitionLaw.FromFunction([Shape.Binary(1 << values)], _ => [0], new Accounting(true));
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
                    var left = Enumerable.Range(0, shape.Size).Where(v => ((state[0] >> v) & 1) == 0).ToArray();
                    return left.Length == 1 ? [state[0], left[0]] : state;
                },
                new Accounting(true, new Unchanged(0)));
        }
    }
}
