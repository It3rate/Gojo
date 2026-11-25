namespace Gojo.EncodingC;

/// <summary>Multi-rung numeric laws and structural ladder builder: semantic-radix
/// digit addition, digit multiplication, conserving carry-up / refinement-down,
/// and exact bidirectional numeric gaps between open values (fulfilling
/// GojoTension-2026-11.md §8).
/// <para><b>Radix is semantic.</b> Any radix B ≥ 2 is supported (base 4, 8, 10,
/// etc.); power-of-two radices are a crystallization target for native word
/// packing, not a restriction on structure.</para></summary>
public static class Rungs
{
    private static readonly Dictionary<(int, bool), RelationLaw> DigitAdds = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> DigitAddSteps = [];
    private static readonly Dictionary<(int, bool), RelationLaw> DigitMults = [];
    private static readonly Dictionary<(int, bool), RelationLaw> OpenGaps = [];
    private static readonly Dictionary<(int, int, bool), RelationLaw> Squares = [];

    /// <summary>The shape of one digit in <paramref name="radix"/>: unary
    /// thermometer count 0..B−1 (connects directly to conserving moves and
    /// room views) or packed binary 0..B−1.</summary>
    public static Shape DigitShape(int radix, bool unary = true)
    {
        if (radix < 2) throw new ArgumentOutOfRangeException(nameof(radix), "A radix has at least two digits.");
        return unary ? Shape.Unary(radix - 1) : Shape.Binary(radix);
    }

    /// <summary>Bidirectional one-rung ripple adder over (a, b, cin, sum, cout)
    /// where a + b + cin = sum + radix · cout. Runs forward (add), backward
    /// (subtract), and across open supports.</summary>
    public static RelationLaw DigitAdd(int radix, bool unary = true)
    {
        var digit = DigitShape(radix, unary);
        lock (DigitAdds)
        {
            if (DigitAdds.TryGetValue((radix, unary), out var cached)) return cached;
            return DigitAdds[(radix, unary)] = RelationLaw.FromPredicate(
                [digit, digit, Shape.Bit, digit, Shape.Bit],
                t => t[0] + t[1] + t[2] == t[3] + radix * t[4]);
        }
    }

    /// <summary>Justified (live functional) one-rung ripple adder over
    /// (a, b, cin [Unchanged], sum, cout [written]): recomputes from current
    /// inputs each step and settles quietly when inputs hold.</summary>
    public static TransitionLaw DigitAddStep(int radix, bool unary = true)
    {
        var digit = DigitShape(radix, unary);
        lock (DigitAddSteps)
        {
            if (DigitAddSteps.TryGetValue((radix, unary), out var cached)) return cached;
            return DigitAddSteps[(radix, unary)] = TransitionLaw.FromFunction(
                [digit, digit, Shape.Bit, digit, Shape.Bit],
                s =>
                {
                    var total = s[0] + s[1] + s[2];
                    return [s[0], s[1], s[2], total % radix, total / radix];
                },
                new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2)));
        }
    }

    /// <summary>Bidirectional one-rung digit multiplier over (a, b, low, high)
    /// where a · b = low + radix · high. Building block of a place-value
    /// lattice: radix 4 is 256 bits (32 bytes), radix 10 is 10,000 bits.</summary>
    public static RelationLaw DigitMultiply(int radix, bool unary = true)
    {
        var digit = DigitShape(radix, unary);
        lock (DigitMults)
        {
            if (DigitMults.TryGetValue((radix, unary), out var cached)) return cached;
            return DigitMults[(radix, unary)] = RelationLaw.FromPredicate(
                [digit, digit, digit, digit],
                t => t[0] * t[1] == t[2] + radix * t[3]);
        }
    }

    /// <summary>1D bidirectional square table over (n, low, high) where
    /// n² = low + radix · high (for quarter-square factoring comparisons).</summary>
    public static RelationLaw SquareDigits(int maxN, int radix, bool unary = false)
    {
        if (maxN < 1) throw new ArgumentOutOfRangeException(nameof(maxN));
        if (radix < 2 || maxN * maxN >= radix * radix)
            throw new ArgumentOutOfRangeException(nameof(radix), "Two radix digits must hold maxN².");
        var input = unary ? Shape.Unary(maxN) : Shape.Binary(maxN + 1);
        var digit = DigitShape(radix, unary);
        lock (Squares)
        {
            if (Squares.TryGetValue((maxN, radix, unary), out var cached)) return cached;
            return Squares[(maxN, radix, unary)] = RelationLaw.FromPredicate(
                [input, digit, digit],
                t => t[0] * t[0] == t[1] + radix * t[2]);
        }
    }

    /// <summary>Conserving carry upward between unary rungs: takes
    /// <paramref name="radix"/> fine units and gives 1 coarse unit, preserving
    /// 1·fine + radix·coarse.</summary>
    public static TransitionLaw CarryUp(int radix, int fineCapacity, int coarseCapacity) =>
        Flows.Convert(radix, 1, fineCapacity, coarseCapacity);

    /// <summary>Conserving refinement / borrow downward between unary rungs:
    /// takes 1 coarse unit and gives <paramref name="radix"/> fine units,
    /// preserving radix·coarse + 1·fine.</summary>
    public static TransitionLaw RefineDown(int radix, int coarseCapacity, int fineCapacity) =>
        Flows.Convert(1, radix, coarseCapacity, fineCapacity);

    /// <summary>Exact bidirectional two-faced numeric gap between two open
    /// values (fulfilling GojoTension-2026-11.md §8). Roles: have, want,
    /// deficit, excess, where have + deficit = want + excess and at least one
    /// of deficit, excess is zero. Narrows exact possible distances for open
    /// supports, settles agreement when both faces are held at 0, and solves
    /// backward from (have, deficit) to want.</summary>
    public static RelationLaw OpenGap(int capacity, bool unary = true)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        var count = unary ? Shape.Unary(capacity) : Shape.Binary(capacity + 1);
        lock (OpenGaps)
        {
            if (OpenGaps.TryGetValue((capacity, unary), out var cached)) return cached;
            return OpenGaps[(capacity, unary)] = RelationLaw.FromPredicate(
                [count, count, count, count],
                t => t[0] + t[2] == t[1] + t[3] && (t[2] == 0 || t[3] == 0));
        }
    }
}

/// <summary>A multi-rung place-value number laid out as ordinary fields in a
/// store (rung 0 is finest, rung k−1 coarsest). Like <see cref="Tension"/>,
/// this class holds no hidden state: everything it builds is ordinary planes
/// and wirings that save, fork and compose like any other structure.</summary>
public sealed class Ladder
{
    public Ladder(Store store, int radix, int rungCount, bool unary = true, int extraHeadroom = 0)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (radix < 2) throw new ArgumentOutOfRangeException(nameof(radix));
        if (rungCount < 1) throw new ArgumentOutOfRangeException(nameof(rungCount));
        if (extraHeadroom < 0) throw new ArgumentOutOfRangeException(nameof(extraHeadroom));
        Store = store;
        Radix = radix;
        Unary = unary;
        var fields = new Field[rungCount];
        for (var i = 0; i < rungCount; i++)
        {
            var shape = unary ? Shape.Unary(radix - 1 + extraHeadroom) : Shape.Binary(radix + extraHeadroom);
            fields[i] = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
        }
        Digits = Array.AsReadOnly(fields);
    }

    public Store Store { get; }
    public int Radix { get; }
    public bool Unary { get; }
    public IReadOnlyList<Field> Digits { get; }

    /// <summary>Reads the place-value integer Σ digitᵢ · Radixⁱ at
    /// <paramref name="position"/>, or null if any rung holds a non-code.</summary>
    public long? Read(Surface surface, int position)
    {
        long total = 0, weight = 1;
        for (var i = 0; i < Digits.Count; i++)
        {
            var d = Digits[i].Read(surface, position);
            if (d is null) return null;
            total += d.Value * weight;
            weight *= Radix;
        }
        return total;
    }

    /// <summary>Writes a nonnegative place-value integer into this ladder's
    /// rungs in <see cref="Store"/> (adapter convenience for setup/tests).</summary>
    public void Write(int position, long value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        for (var i = 0; i < Digits.Count; i++)
        {
            Digits[i].Write(Store, position, (int)(value % Radix));
            value /= Radix;
        }
        if (value != 0) throw new ArgumentOutOfRangeException(nameof(value), "Value exceeds ladder capacity.");
    }

    /// <summary>Allocates open <see cref="Supports"/> for every rung so
    /// relational laws can narrow multi-rung open values in every direction.</summary>
    public static Supports[] Open(Store store, int radix, int rungCount, bool unary = true)
    {
        var shape = Rungs.DigitShape(radix, unary);
        var result = new Supports[rungCount];
        for (var i = 0; i < rungCount; i++)
            result[i] = Supports.On(store, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]));
        return result;
    }

    /// <summary>Restricts open multi-rung supports to one exact nonnegative value.</summary>
    public static void RestrictExact(Supports[] rungs, int radix, int position, long value)
    {
        for (var i = 0; i < rungs.Length; i++)
        {
            rungs[i].Restrict(position, 1UL << (int)(value % radix));
            value /= radix;
        }
        if (value != 0) throw new ArgumentOutOfRangeException(nameof(value), "Value exceeds open ladder rungs.");
    }

    /// <summary>Reads the exact settled value of open multi-rung supports, or
    /// null if any rung is still open or contradicted.</summary>
    public static long? ReadSettled(Supports[] rungs, int radix, int position)
    {
        long total = 0, weight = 1;
        for (var i = 0; i < rungs.Length; i++)
        {
            var mask = rungs[i].Values(position);
            if (mask == 0 || (mask & (mask - 1)) != 0) return null;
            var digit = System.Numerics.BitOperations.TrailingZeroCount(mask);
            total += digit * weight;
            weight *= radix;
        }
        return total;
    }

    /// <summary>Wires a bidirectional ripple-carry addition network over open
    /// multi-rung supports: a + b + cin = sum + Radix^k · cout.</summary>
    public static IReadOnlyList<Wiring> WireRelationalAdd(
        Store store,
        int radix,
        Supports[] a,
        Supports[] b,
        Supports[] sum,
        Supports cin,
        Supports cout,
        Plane positions,
        bool unary = true)
    {
        if (a.Length != b.Length || a.Length != sum.Length)
            throw new ArgumentException("All multi-rung addends and sum must have the same rung count.");
        var addLaw = Rungs.DigitAdd(radix, unary);
        var wirings = new List<Wiring>(a.Length);
        var carryIn = cin;
        for (var i = 0; i < a.Length; i++)
        {
            var carryOut = i == a.Length - 1 ? cout : Supports.On(store, new Field(Shape.Bit, store.AddPlane()));
            var w = new Wiring(addLaw, a[i].Variable, b[i].Variable, carryIn.Variable, sum[i].Variable, carryOut.Variable);
            foreach (var pos in positions.Indexes()) w.Add(pos, pos, pos, pos, pos);
            wirings.Add(w);
            carryIn = carryOut;
        }
        return wirings;
    }
}
