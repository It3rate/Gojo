namespace Gojo.EncodingC;

/// <summary>A comparison of two amounts in a common basis. A wiring can use
/// the result as evidence for a separate response law.</summary>
public static class TensionGap
{
    /// <summary>The exact goal difference with roles have, want, deficit,
    /// excess. All four are unary counts up to <paramref name="capacity"/>.
    /// Each admitted tuple has at most one positive difference. The relation
    /// compares samples or narrows supports. The table size is bounded by
    /// <see cref="Law.MaxBits"/>.</summary>
    public static RelationLaw Between(int capacity)
    {
        var count = Shape.Unary(capacity);
        return RelationLaw.FromPredicate([count, count, count, count], tuple =>
        {
            var (deficit, excess) = Difference(tuple[0], tuple[1]);
            return tuple[2] == deficit && tuple[3] == excess;
        });
    }

    /// <summary>Tracks the same goal difference as <see cref="Between"/> over
    /// concrete samples. Reads have and want, then updates the deficit and
    /// excess fields. The samples stay unchanged, and a result already equal
    /// to their difference comes to rest without reopening supports. All roles
    /// are unary counts and the transition table is bounded by <see cref="Law.MaxBits"/>.</summary>
    public static TransitionLaw Track(int capacity)
    {
        var count = Shape.Unary(capacity);
        return TransitionLaw.FromFunction([count, count, count, count], state =>
        {
            var (deficit, excess) = Difference(state[0], state[1]);
            return [state[0], state[1], deficit, excess];
        }, new Accounting(true, new Unchanged(0), new Unchanged(1)));
    }

    private static (int Deficit, int Excess) Difference(int have, int want) =>
        want >= have ? (want - have, 0) : (0, have - want);
}
