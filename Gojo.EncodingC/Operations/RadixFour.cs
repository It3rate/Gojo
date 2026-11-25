namespace Gojo.EncodingC;

/// <summary>Two positions related by a four-to-one grouping in one selected
/// count basis. The law binds the positions that share that calibration.</summary>
internal static class RadixFour
{
    private const int Radix = 4;

    internal static Shape Digit { get; } = Shape.Binary(Radix);
    internal static Shape Amount { get; } = Shape.Binary(Radix * Radix);

    /// <summary>Roles are coarse digit, fine digit, and amount in fine units.
    /// An unobserved digit can stay open while the amount narrows.</summary>
    internal static RelationLaw ReadAmount { get; } = RelationLaw.FromPredicate(
        [Digit, Digit, Amount], state => Radix * state[0] + state[1] == state[2]);

    /// <summary>Admits one fine unit from pressure. Roles are pressure, coarse
    /// digit, and fine digit. A full pair holds pressure for a later step.</summary>
    internal static TransitionLaw Admit(int pressureCapacity) => TransitionLaw.FromFunction(
        [Shape.Unary(pressureCapacity), Digit, Digit],
        state =>
        {
            var (pressure, coarse, fine) = (state[0], state[1], state[2]);
            if (pressure == 0 || (coarse == Radix - 1 && fine == Radix - 1))
                return state;

            return fine == Radix - 1
                ? [pressure - 1, coarse + 1, 0]
                : [pressure - 1, coarse, fine + 1];
        },
        new Accounting(true, new PreservedSum([0, 1, 2], [1, Radix, 1]), new NeverIncreases(0)));
}
