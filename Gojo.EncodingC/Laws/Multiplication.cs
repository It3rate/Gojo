namespace Gojo.EncodingC;

/// <summary>Factored multiplication networks and arrival-pairing provenance
/// built strictly from small EncodingC laws: two-rung place-value lattice
/// (<see cref="Rungs.DigitMultiply"/> + <see cref="Rungs.DigitAdd"/>),
/// Egyptian doubling (<see cref="Booleans.And"/> + <see cref="Booleans.FullAdder"/>),
/// quarter-square 1D lookup, and discrete Leibniz arrival pairing
/// <c>Δ(ab) = b·Δa + a·Δb + Δa·Δb</c>.</summary>
public static class Multiplication
{
    private static readonly Dictionary<(int, int), TransitionLaw> SweepAccums = [];
    private static readonly Dictionary<int, TransitionLaw> CornerAccums = [];

    /// <summary>Two-rung bidirectional place-value lattice multiplier:
    /// (a0 + B·a1) × (b0 + B·b1) = p0 + B·p1 + B²·p2 + B³·p3, using only
    /// <see cref="Rungs.DigitMultiply"/> and <see cref="Rungs.DigitAdd"/>.</summary>
    public sealed record TwoRungLattice(Engine Engine, Store Store, int Radix, Supports[] A, Supports[] B, Supports[] Product);

    public static TwoRungLattice BuildTwoRungLattice(int radix, bool unary = false)
    {
        var store = new Store(0, 1);
        var here = Plane.Of(1, 0);
        var a = Ladder.Open(store, radix, 2, unary);
        var b = Ladder.Open(store, radix, 2, unary);
        var p = Ladder.Open(store, radix, 4, unary);

        Supports OpenDigit() => Ladder.Open(store, radix, 1, unary)[0];
        Supports OpenBit() => Supports.On(store, new Field(Shape.Bit, store.AddPlane()));
        Supports ZeroDigit() { var s = OpenDigit(); s.Restrict(0, 1UL << 0); return s; }
        Supports ZeroBit() { var s = OpenBit(); s.Restrict(0, 1UL << 0); return s; }

        var mul = Rungs.DigitMultiply(radix, unary);
        var add = Rungs.DigitAdd(radix, unary);
        var engine = new Engine();

        void WireMul(Supports x, Supports y, Supports lo, Supports hi)
        {
            var w = new Wiring(mul, x.Variable, y.Variable, lo.Variable, hi.Variable);
            w.Add(0, 0, 0, 0);
            engine.Add(w);
        }

        void WireAdd(Supports x, Supports y, Supports cin, Supports sum, Supports cout)
        {
            var w = new Wiring(add, x.Variable, y.Variable, cin.Variable, sum.Variable, cout.Variable);
            w.Add(0, 0, 0, 0, 0);
            engine.Add(w);
        }

        // Four digit products:
        // c00 = a0 * b0 (place 0, 1)
        // c01 = a0 * b1 (place 1, 2)
        // c10 = a1 * b0 (place 1, 2)
        // c11 = a1 * b1 (place 2, 3)
        var (c00Hi, c01Lo, c01Hi) = (OpenDigit(), OpenDigit(), OpenDigit());
        var (c10Lo, c10Hi, c11Lo, c11Hi) = (OpenDigit(), OpenDigit(), OpenDigit(), OpenDigit());

        WireMul(a[0], b[0], p[0], c00Hi);
        WireMul(a[0], b[1], c01Lo, c01Hi);
        WireMul(a[1], b[0], c10Lo, c10Hi);
        WireMul(a[1], b[1], c11Lo, c11Hi);

        // Diagonal place 1: c00Hi + c01Lo + c10Lo -> p[1] with carries k1a, k1b
        var (t1, k1a, k1b) = (OpenDigit(), OpenBit(), OpenBit());
        WireAdd(c00Hi, c01Lo, ZeroBit(), t1, k1a);
        WireAdd(t1, c10Lo, ZeroBit(), p[1], k1b);

        // Diagonal place 2: c01Hi + c10Hi + c11Lo + k1a + k1b -> p[2] with carries k2a, k2b
        var (t2, k2a, k2b) = (OpenDigit(), OpenBit(), OpenBit());
        WireAdd(c01Hi, c10Hi, k1a, t2, k2a);
        WireAdd(t2, c11Lo, k1b, p[2], k2b);

        // Diagonal place 3: c11Hi + k2a + k2b -> p[3] (never overflows place 3)
        var t3 = OpenDigit();
        WireAdd(c11Hi, ZeroDigit(), k2a, t3, ZeroBit());
        WireAdd(t3, ZeroDigit(), k2b, p[3], ZeroBit());

        return new TwoRungLattice(engine, store, radix, a, b, p);
    }

    /// <summary>Egyptian doubling multiplier using zero multiplication tables:
    /// only <see cref="Booleans.And"/> (selecting doubled copies of A by the
    /// bits of B) and <see cref="Booleans.FullAdder"/> ripple chains. Total
    /// shared law tables: 40 bits (5 bytes) for any bit width.</summary>
    public sealed record EgyptianMultiplier(Engine Engine, Store Store, int Bits, Supports[] A, Supports[] B, Supports[] Product);

    public static EgyptianMultiplier BuildEgyptianDoubling(int bits)
    {
        if (bits is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(bits));
        var store = new Store(0, 1);
        var a = Ladder.Open(store, 2, bits, unary: false);
        var b = Ladder.Open(store, 2, bits, unary: false);
        var engine = new Engine();

        Supports OpenBit() => Supports.On(store, new Field(Shape.Bit, store.AddPlane()));
        Supports ZeroBit() { var s = OpenBit(); s.Restrict(0, 1UL << 0); return s; }

        Supports AndBit(Supports x, Supports y)
        {
            var r = OpenBit();
            var w = new Wiring(Booleans.And, x.Variable, y.Variable, r.Variable);
            w.Add(0, 0, 0);
            engine.Add(w);
            return r;
        }

        // Accumulate partial shifted rows: acc starts at row 0 (a & b[0]) padded to 2*bits.
        var width = bits * 2;
        var acc = new Supports[width];
        for (var k = 0; k < width; k++)
            acc[k] = k < bits ? AndBit(a[k], b[0]) : ZeroBit();

        for (var shift = 1; shift < bits; shift++)
        {
            var next = new Supports[width];
            var cin = ZeroBit();
            for (var k = 0; k < width; k++)
            {
                if (k < shift)
                {
                    next[k] = acc[k];
                    continue;
                }
                var term = k - shift < bits ? AndBit(a[k - shift], b[shift]) : ZeroBit();
                var sum = OpenBit();
                var cout = k == width - 1 ? ZeroBit() : OpenBit();
                var w = new Wiring(Booleans.FullAdder, acc[k].Variable, term.Variable, cin.Variable, sum.Variable, cout.Variable);
                w.Add(0, 0, 0, 0, 0);
                engine.Add(w);
                next[k] = sum;
                cin = cout;
            }
            acc = next;
        }

        return new EgyptianMultiplier(engine, store, bits, a, b, acc);
    }

    /// <summary>Bidirectional quarter-square multiplier over a, b in 0..maxFactor:
    /// uses one 1D square table (n &lt;-&gt; n²) plus sum, absolute difference,
    /// and 4·product = (a+b)² − |a−b|².</summary>
    public sealed record QuarterSquareMultiplier(Engine Engine, Store Store, Supports A, Supports B, Supports Product);

    public static QuarterSquareMultiplier BuildQuarterSquare(int maxFactor)
    {
        if (maxFactor is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(maxFactor), "Keep single-role support <= 64 values for compact comparison.");
        var maxSum = 2 * maxFactor;
        var maxSq = maxSum * maxSum;
        var maxProd = maxFactor * maxFactor;

        var fShape = Shape.Binary(maxFactor + 1);
        var sShape = Shape.Binary(maxSum + 1);
        var pShape = Shape.Binary(maxProd + 1);
        // Represent squares in radix (maxSum + 1) so each square digit stays <= maxSum <= 10 (tiny table!).
        var sqRadix = maxSum + 1;
        var sqDigit = Shape.Binary(sqRadix);

        var addRel = RelationLaw.FromPredicate([fShape, fShape, sShape], t => t[0] + t[1] == t[2]);
        var absDiffRel = RelationLaw.FromPredicate([fShape, fShape, fShape], t => (t[0] >= t[1] ? t[0] - t[1] : t[1] - t[0]) == t[2]);
        var sqRel = Rungs.SquareDigits(maxSum, sqRadix, unary: false);
        var sqDiffToProd = RelationLaw.FromPredicate(
            [sqDigit, sqDigit, sqDigit, sqDigit, pShape],
            t => (t[0] + sqRadix * t[1]) - (t[2] + sqRadix * t[3]) == 4 * t[4]);

        var store = new Store(0, 1);
        Supports Open(Shape s) => Supports.On(store, new Field(s, [.. Enumerable.Range(0, s.Bits).Select(_ => store.AddPlane())]));

        var a = Open(fShape);
        var b = Open(fShape);
        var sum = Open(sShape);
        var diff = Open(fShape);
        var diffWide = Open(sShape);
        var (s2Lo, s2Hi) = (Open(sqDigit), Open(sqDigit));
        var (d2Lo, d2Hi) = (Open(sqDigit), Open(sqDigit));
        var prod = Open(pShape);

        // Embed diff (0..maxFactor) into sShape (0..2*maxFactor) via exact equality relation.
        var widen = RelationLaw.FromPredicate([fShape, sShape], t => t[0] == t[1]);

        var engine = new Engine();
        void AddW(RelationLaw law, params Supports[] roles)
        {
            var w = new Wiring(law, [.. roles.Select(r => r.Variable)]);
            w.Add(new int[roles.Length]);
            engine.Add(w);
        }
        AddW(addRel, a, b, sum);
        AddW(absDiffRel, a, b, diff);
        AddW(widen, diff, diffWide);
        AddW(sqRel, sum, s2Lo, s2Hi);
        AddW(sqRel, diffWide, d2Lo, d2Hi);
        AddW(sqDiffToProd, s2Lo, s2Hi, d2Lo, d2Hi, prod);

        return new QuarterSquareMultiplier(engine, store, a, b, prod);
    }

    /// <summary>Arrival-pairing sweep accumulator: when arrival bit is 1, adds
    /// the current opposite factor <c>other</c> (before-state, Unchanged) into
    /// <c>swept</c>. Roles: other (Unchanged), arrived (Unchanged), swept (written).</summary>
    public static TransitionLaw SweepAccumulate(int maxFactor, int maxProduct)
    {
        lock (SweepAccums)
        {
            if (SweepAccums.TryGetValue((maxFactor, maxProduct), out var cached)) return cached;
            var factor = Shape.Unary(maxFactor);
            var prod = Shape.Unary(maxProduct);
            return SweepAccums[(maxFactor, maxProduct)] = TransitionLaw.FromFunction(
                [factor, Shape.Bit, prod],
                s => [s[0], s[1], s[1] == 1 && s[2] + s[0] <= maxProduct ? s[2] + s[0] : s[2]],
                new Accounting(true, new Unchanged(0), new Unchanged(1), new NeverDecreases(2)));
        }
    }

    /// <summary>Simultaneous co-arrival corner accumulator: when both da and db
    /// are 1, increments <c>corners</c> by 1 (the Δa·Δb gnomon corner).
    /// Roles: da (Unchanged), db (Unchanged), corners (written).</summary>
    public static TransitionLaw CornerAccumulate(int maxCorners)
    {
        lock (CornerAccums)
        {
            if (CornerAccums.TryGetValue(maxCorners, out var cached)) return cached;
            var c = Shape.Unary(maxCorners);
            return CornerAccums[maxCorners] = TransitionLaw.FromFunction(
                [Shape.Bit, Shape.Bit, c],
                s => [s[0], s[1], s[0] == 1 && s[1] == 1 && s[2] < maxCorners ? s[2] + 1 : s[2]],
                new Accounting(true, new Unchanged(0), new Unchanged(1), new NeverDecreases(2)));
        }
    }
}
