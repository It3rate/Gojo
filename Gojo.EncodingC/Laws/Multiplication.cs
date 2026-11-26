namespace Gojo.EncodingC;

/// <summary>Factored multiplication networks and arrival-pairing provenance
/// built strictly from small EncodingC laws: two-rung place-value lattice
/// (<see cref="Numbers.DigitMulPair"/> + <see cref="Numbers.DigitSum"/>),
/// Egyptian doubling (<see cref="Booleans.And"/> + <see cref="Booleans.FullAdder"/>),
/// quarter-square 1D lookup, and discrete Leibniz arrival pairing
/// <c>Δ(ab) = b·Δa + a·Δb + Δa·Δb</c>.</summary>
public static class Multiplication
{
    private static readonly Dictionary<(int, int), TransitionLaw> SweepAccums = [];
    private static readonly Dictionary<int, TransitionLaw> CornerAccums = [];

    /// <summary>Two-rung bidirectional place-value lattice multiplier:
    /// (a0 + B·a1) × (b0 + B·b1) = p0 + B·p1 + B²·p2 + B³·p3, using only
    /// <see cref="Numbers.DigitMulPair"/> and <see cref="Numbers.DigitSum"/>.</summary>
    public sealed record TwoRungLattice(Engine Engine, Store Store, int Radix, Ladder A, Ladder B, Ladder Product);

    public static TwoRungLattice BuildTwoRungLattice(int radix, bool unary = false)
    {
        var store = new Store(0, 1);
        var a = new Ladder(store, radix, 2, unary);
        var b = new Ladder(store, radix, 2, unary);
        var p = new Ladder(store, radix, 4, unary);
        var aSup = a.Possible;
        var bSup = b.Possible;
        var pSup = p.Possible;

        var digitShape = Numbers.Digit(radix, unary);
        var carryShape = Numbers.Carry(unary);
        Supports OpenDigit() => Supports.On(store, new Field(digitShape, [.. Enumerable.Range(0, digitShape.Bits).Select(_ => store.AddPlane())]));
        Supports OpenCarry() => Supports.On(store, new Field(carryShape, store.AddPlane()));
        Supports ZeroDigit() { var s = OpenDigit(); s.Restrict(0, 1UL << 0); return s; }
        Supports ZeroCarry() { var s = OpenCarry(); s.Restrict(0, 1UL << 0); return s; }

        var mul = Numbers.DigitMulPair(radix, unary).Relation;
        var add = Numbers.DigitSum(radix, unary).Relation;
        var engine = new Engine();

        void WireMul(Supports x, Supports y, Supports lo, Supports hi)
        {
            var w = new Wiring(mul, x.Variable, y.Variable, lo.Variable, hi.Variable);
            w.Add(0, 0, 0, 0);
            engine.Add(w);
        }

        void WireAdd(Supports x, Supports y, Supports cin, Supports sum, Supports cout)
        {
            // Numbers.DigitSum roles: (a=0, b=1, cin=2, cout=3, sum=4)
            var w = new Wiring(add, x.Variable, y.Variable, cin.Variable, cout.Variable, sum.Variable);
            w.Add(0, 0, 0, 0, 0);
            engine.Add(w);
        }

        var (c00Hi, c01Lo, c01Hi) = (OpenDigit(), OpenDigit(), OpenDigit());
        var (c10Lo, c10Hi, c11Lo, c11Hi) = (OpenDigit(), OpenDigit(), OpenDigit(), OpenDigit());

        WireMul(aSup[0], bSup[0], pSup[0], c00Hi);
        WireMul(aSup[0], bSup[1], c01Lo, c01Hi);
        WireMul(aSup[1], bSup[0], c10Lo, c10Hi);
        WireMul(aSup[1], bSup[1], c11Lo, c11Hi);

        var (t1, k1a, k1b) = (OpenDigit(), OpenCarry(), OpenCarry());
        WireAdd(c00Hi, c01Lo, ZeroCarry(), t1, k1a);
        WireAdd(t1, c10Lo, ZeroCarry(), pSup[1], k1b);

        var (t2, k2a, k2b) = (OpenDigit(), OpenCarry(), OpenCarry());
        WireAdd(c01Hi, c10Hi, k1a, t2, k2a);
        WireAdd(t2, c11Lo, k1b, pSup[2], k2b);

        var t3 = OpenDigit();
        WireAdd(c11Hi, ZeroDigit(), k2a, t3, ZeroCarry());
        WireAdd(t3, ZeroDigit(), k2b, pSup[3], ZeroCarry());

        return new TwoRungLattice(engine, store, radix, a, b, p);
    }

    /// <summary>Egyptian doubling multiplier using zero multiplication tables:
    /// only <see cref="Booleans.And"/> (selecting doubled copies of A by the
    /// bits of B) and <see cref="Booleans.FullAdder"/> ripple chains. Total
    /// shared law tables: 40 bits (5 bytes) for any bit width.</summary>
    public sealed record EgyptianMultiplier(Engine Engine, Store Store, int Bits, Ladder A, Ladder B, Ladder Product);

    public static EgyptianMultiplier BuildEgyptianDoubling(int bits)
    {
        if (bits is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(bits));
        var store = new Store(0, 1);
        var a = new Ladder(store, 2, bits, unary: false);
        var b = new Ladder(store, 2, bits, unary: false);
        var aSup = a.Possible;
        var bSup = b.Possible;
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

        var width = bits * 2;
        var acc = new Supports[width];
        for (var k = 0; k < width; k++)
            acc[k] = k < bits ? AndBit(aSup[k], bSup[0]) : ZeroBit();

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
                var term = k - shift < bits ? AndBit(aSup[k - shift], bSup[shift]) : ZeroBit();
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

        // Bind final accumulator supports into a clean product Ladder.
        var prod = new Ladder(store, 2, width, unary: false);
        var prodSup = prod.Possible;
        var sameBit = Settling.Same(Shape.Bit);
        for (var k = 0; k < width; k++)
        {
            var w = new Wiring(sameBit, acc[k].Variable, prodSup[k].Variable);
            w.Add(0, 0);
            engine.Add(w);
        }

        return new EgyptianMultiplier(engine, store, bits, a, b, prod);
    }

    /// <summary>Bidirectional quarter-square multiplier over a, b in 0..maxFactor:
    /// uses one 1D square table (n &lt;-&gt; n²) plus sum, absolute difference,
    /// and 4·product = (a+b)² − |a−b|².</summary>
    public sealed record QuarterSquareMultiplier(Engine Engine, Store Store, Supports A, Supports B, Supports Product);

    public static QuarterSquareMultiplier BuildQuarterSquare(int maxFactor)
    {
        if (maxFactor is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(maxFactor), "Keep single-role support <= 64 values for compact comparison.");
        var maxSum = 2 * maxFactor;
        var maxProd = maxFactor * maxFactor;

        var fShape = Shape.Binary(maxFactor + 1);
        var sShape = Shape.Binary(maxSum + 1);
        var pShape = Shape.Binary(maxProd + 1);
        var sqRadix = maxSum + 1;
        var sqDigit = Shape.Binary(sqRadix);

        var addRel = RelationLaw.FromPredicate([fShape, fShape, sShape], t => t[0] + t[1] == t[2]);
        var absDiffRel = RelationLaw.FromPredicate([fShape, fShape, fShape], t => (t[0] >= t[1] ? t[0] - t[1] : t[1] - t[0]) == t[2]);
        var sqRel = Numbers.SquareDigits(maxSum, sqRadix, unary: false);
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

    private static readonly Dictionary<(int, int, int, int, bool), TransitionLaw> BoundedMults = [];

    /// <summary>Functional bounded multiplication with explicit <c>T+</c>
    /// overflow role: reads <c>a</c> and <c>b</c> unchanged, writes
    /// <c>value = min(a·b, maxValue)</c> and
    /// <c>overflow = max(0, a·b − maxValue)</c>. Requires
    /// <c>maxValue + maxOverflow ≥ maxA · maxB</c> so excess never silently
    /// wraps or double-clips.</summary>
    public static TransitionLaw BoundedMultiply(int maxA, int maxB, int maxValue, int maxOverflow, bool unary = false)
    {
        if (maxA < 1) throw new ArgumentOutOfRangeException(nameof(maxA));
        if (maxB < 1) throw new ArgumentOutOfRangeException(nameof(maxB));
        if (maxValue < 1) throw new ArgumentOutOfRangeException(nameof(maxValue));
        if (maxOverflow < 1) throw new ArgumentOutOfRangeException(nameof(maxOverflow));
        if ((long)maxValue + maxOverflow < (long)maxA * maxB)
            throw new ArgumentOutOfRangeException(nameof(maxOverflow),
                "maxValue + maxOverflow must cover maxA * maxB so overflow never silently double-clips.");
        lock (BoundedMults)
        {
            var key = (maxA, maxB, maxValue, maxOverflow, unary);
            if (BoundedMults.TryGetValue(key, out var cached)) return cached;
            Shape Make(int max) => unary ? Shape.Unary(max) : Shape.Binary(max + 1);
            return BoundedMults[key] = TransitionLaw.FromFunction(
                [Make(maxA), Make(maxB), Make(maxValue), Make(maxOverflow)],
                s =>
                {
                    var prod = (ulong)s[0] * (ulong)s[1];
                    var val = prod <= (ulong)maxValue ? (int)prod : maxValue;
                    var ovf = prod > (ulong)maxValue ? (int)(prod - (ulong)maxValue) : 0;
                    return [s[0], s[1], val, ovf];
                },
                new Accounting(true, new Unchanged(0), new Unchanged(1)));
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
