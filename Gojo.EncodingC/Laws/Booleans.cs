namespace Gojo.EncodingC;

/// <summary>Boolean operations as tables: every two-input Boolean rule is one
/// byte read in every direction, the bit rung of arithmetic (AND = carry/product,
/// OR = clamped sum, XOR = wrapped sum), plus four-region occupancy partitioning
/// and Fredkin conservative/reversible routing.
/// <para><b>Encoding matters.</b> These laws act on single-bit presence roles.
/// Planewise bitwise acts on unary thermometer counts give min (AND), max (OR),
/// and span (XOR), not multiplication.</para></summary>
public static class Booleans
{
    private static readonly RelationLaw[] Gates = new RelationLaw[16];
    private static readonly TransitionLaw[] Routers = new TransitionLaw[16];
    private static readonly Dictionary<int, RelationLaw> Parities = [];

    /// <summary>Any of the 16 two-input Boolean relations over (a, b, out), one
    /// byte each, read in every direction. Bit <c>a + 2·b</c> of
    /// <paramref name="truthMask"/> is the output for (a, b).</summary>
    public static RelationLaw Gate(int truthMask)
    {
        if ((uint)truthMask >= 16) throw new ArgumentOutOfRangeException(nameof(truthMask), "Use a four-bit truth mask 0..15.");
        return Gates[truthMask] ??= RelationLaw.FromPredicate(
            [Shape.Bit, Shape.Bit, Shape.Bit],
            t => t[2] == ((truthMask >> (t[0] + 2 * t[1])) & 1));
    }

    public static RelationLaw False => Gate(0b0000);
    public static RelationLaw Nor => Gate(0b0001);
    public static RelationLaw Diff => Gate(0b0010);
    public static RelationLaw NotB => Gate(0b0011);
    public static RelationLaw ReverseDiff => Gate(0b0100);
    public static RelationLaw NotA => Gate(0b0101);
    public static RelationLaw Xor => Gate(0b0110);
    public static RelationLaw Nand => Gate(0b0111);
    public static RelationLaw And => Gate(0b1000);
    public static RelationLaw Xnor => Gate(0b1001);
    public static RelationLaw ProjectA => Gate(0b1010);
    public static RelationLaw ReverseImplies => Gate(0b1011);
    public static RelationLaw ProjectB => Gate(0b1100);
    public static RelationLaw Implies => Gate(0b1101);
    public static RelationLaw Or => Gate(0b1110);
    public static RelationLaw True => Gate(0b1111);

    /// <summary>Single-bit negation as a bidirectional relation over (a, notA).
    /// Prefer a live <see cref="Flipped"/> view when no open narrowing is needed.</summary>
    public static RelationLaw Not { get; } = RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], t => t[0] != t[1]);

    /// <summary>One-bit half adder over (a, b, sum, carry): sum is wrapped
    /// addition (XOR), carry is product/co-presence (AND). Two bytes.</summary>
    public static RelationLaw HalfAdder { get; } = RelationLaw.FromPredicate(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        t => t[2] == ((t[0] + t[1]) & 1) && t[3] == ((t[0] + t[1]) >> 1));

    /// <summary>One-bit full adder over (a, b, cin, sum, cout): four bytes,
    /// bidirectional across all five roles.</summary>
    public static RelationLaw FullAdder { get; } = RelationLaw.FromPredicate(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        t => t[3] == ((t[0] + t[1] + t[2]) & 1) && t[4] == ((t[0] + t[1] + t[2]) >> 1));

    /// <summary>Partitions two presences (a, b) into their four mutually
    /// exclusive occupancy regions: (a, b, neither, aOnly, bOnly, both).</summary>
    public static RelationLaw FourRegions { get; } = RelationLaw.FromPredicate(
        [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
        t =>
        {
            var q = t[0] + 2 * t[1];
            return t[2] == (q == 0 ? 1 : 0)
                && t[3] == (q == 1 ? 1 : 0)
                && t[4] == (q == 2 ? 1 : 0)
                && t[5] == (q == 3 ? 1 : 0);
        });

    /// <summary>Conserving selection over the four occupancy regions of (a, b):
    /// routes a unit of participation from <c>in</c> to <c>selected</c> when
    /// <paramref name="truthMask"/> admits (a, b), or to <c>residue</c> when it
    /// does not. Roles: a, b (Unchanged), in, selected, residue (PreservedSum).</summary>
    public static TransitionLaw ConservingSelect(int truthMask)
    {
        if ((uint)truthMask >= 16) throw new ArgumentOutOfRangeException(nameof(truthMask), "Use a four-bit truth mask 0..15.");
        return Routers[truthMask] ??= TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
            s =>
            {
                if (s[2] == 0) return s;
                var admit = ((truthMask >> (s[0] + 2 * s[1])) & 1) == 1;
                if (admit && s[3] == 0) return [s[0], s[1], 0, 1, s[4]];
                if (!admit && s[4] == 0) return [s[0], s[1], 0, s[3], 1];
                return s;
            },
            new Accounting(true, new Unchanged(0), new Unchanged(1), new PreservedSum([2, 3, 4])));
    }

    /// <summary>Fredkin controlled-swap gate over (control, a, b): swaps a and b
    /// when control is 1. Both conservative (PreservedSum on a, b) and
    /// reversible (exact bijection, self-inverse).</summary>
    public static TransitionLaw Fredkin { get; } = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit],
        s => s[0] == 1 ? [1, s[2], s[1]] : s,
        new Accounting(functional: true, reversible: true, new Unchanged(0), new PreservedSum([1, 2])));

    /// <summary>Parity of a unary count (e.g. from <see cref="Counts.Tally"/>
    /// over 3+ Boolean inputs). Roles: count, odd (1 when count is odd).</summary>
    public static RelationLaw OddParity(int maxCount)
    {
        if (maxCount < 1) throw new ArgumentOutOfRangeException(nameof(maxCount));
        lock (Parities)
        {
            if (Parities.TryGetValue(maxCount, out var cached)) return cached;
            return Parities[maxCount] = RelationLaw.FromPredicate(
                [Shape.Unary(maxCount), Shape.Bit],
                t => t[1] == (t[0] & 1));
        }
    }
}
