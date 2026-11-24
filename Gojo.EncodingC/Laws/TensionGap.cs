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
            var difference = tuple[1] - tuple[0];
            return difference >= 0
                ? tuple[2] == difference && tuple[3] == 0
                : tuple[2] == 0 && tuple[3] == -difference;
        });
    }
}
