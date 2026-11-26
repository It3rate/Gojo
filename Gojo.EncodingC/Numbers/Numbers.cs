namespace Gojo.EncodingC;

/// <summary>A minimal multi-rung positional number layered directly on top of
/// pre-numeric <see cref="Store"/> planes: an ordered array of small-radix
/// digit <see cref="Field"/>s (low rung 0 to high rung k-1), with optional
/// pre-numeric unary representation on Rung 0. All state lives in ordinary
/// planes; deleting this wrapper leaves every digit plane and wiring intact.</summary>
public sealed class RungNumber
{
    public RungNumber(Store store, int radix, int rungs, bool unary = false)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (radix is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(radix), "Keep per-rung radix in 2..16 so every rung stays well inside the 64-value support read cap.");
        if (rungs < 1) throw new ArgumentOutOfRangeException(nameof(rungs));
        Store = store;
        Radix = radix;
        Unary = unary;
        var shape = unary ? Shape.Unary(radix - 1) : Shape.Binary(radix);
        Digits = new Field[rungs];
        for (var i = 0; i < rungs; i++)
            Digits[i] = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
    }

    public RungNumber(Store store, int radix, Field[] digits)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(digits);
        if (radix is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(radix));
        if (digits.Length < 1) throw new ArgumentException("Need at least one rung.", nameof(digits));
        Store = store;
        Radix = radix;
        Unary = digits[0].Shape.IsUnary;
        Digits = digits;
    }

    public Store Store { get; }
    public int Radix { get; }
    public bool Unary { get; }
    public Field[] Digits { get; }
    public int RungCount => Digits.Length;

    /// <summary>Adapter-boundary write of a non-negative integer across rungs
    /// (lowest digit at rung 0).</summary>
    public void Write(int position, long value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var rem = value;
        for (var i = 0; i < Digits.Length; i++)
        {
            Digits[i].Write(Store, position, (int)(rem % Radix));
            rem /= Radix;
        }
        if (rem != 0) throw new ArgumentOutOfRangeException(nameof(value), "Value exceeds the number's rung capacity.");
    }

    /// <summary>Adapter-boundary read across rungs; returns null if any rung
    /// holds a malformed code.</summary>
    public long? Read(Surface surface, int position)
    {
        long total = 0;
        long weight = 1;
        for (var i = 0; i < Digits.Length; i++)
        {
            var d = Digits[i].Read(surface, position);
            if (d is null) return null;
            total += d.Value * weight;
            weight *= Radix;
        }
        return total;
    }

    public long? Read(int position) => Read(Store, position);

    /// <summary>Allocates an open uncertain multi-rung number as per-rung
    /// <see cref="Supports"/> (each rung stays &lt;= 16 values, well inside the
    /// 64-value support cap).</summary>
    public static Supports[] Open(Store store, int radix, int rungs, bool unary = false)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (radix is < 2 or > 16) throw new ArgumentOutOfRangeException(nameof(radix));
        var shape = unary ? Shape.Unary(radix - 1) : Shape.Binary(radix);
        var open = new Supports[rungs];
        for (var i = 0; i < rungs; i++)
            open[i] = Supports.On(store, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]));
        return open;
    }
}

/// <summary>Minimal multi-rung numeric laws and wiring builders layered cleanly
/// above pre-numeric <see cref="Operations.Balance"/> and
/// <see cref="Operations.Bind"/>. Every pre-numeric structure (Rung 0:
/// <see cref="Odv"/>, <see cref="Sides"/>, <see cref="Paths"/>, unary
/// <see cref="Tension"/>, <see cref="Flows"/>, <see cref="Counts"/>) remains
/// untouched and usable without numerics.</summary>
public static class Numbers
{
    private static readonly Dictionary<(int, bool), RelationLaw> AddRels = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> AddCombined = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> AddSums = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> AddCarries = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> SubDiffs = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> SubBorrows = [];
    private static readonly Dictionary<(int, bool), RelationLaw> MulRels = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> MulLos = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> MulHis = [];
    private static readonly Dictionary<(int, int, bool, bool), TransitionLaw> LandmarkShears = [];

    private static Shape DigitShape(int radix, bool unary)
    {
        if (radix is < 2 or > 10) throw new ArgumentOutOfRangeException(nameof(radix), "Digit tables support radix 2..10 inside Law.MaxBits.");
        return unary ? Shape.Unary(radix - 1) : Shape.Binary(radix);
    }

    /// <summary>One balance relation for both radix-B addition AND subtraction:
    /// <c>a + b + cin = sum + B·cout</c> (weights 1, 1, 1, −1, −B). Fits base
    /// 2, 4, 8, and 10 cleanly (at base 10: 4,000 bits = 500 bytes).</summary>
    public static RelationLaw DigitAddRelation(int radix, bool unary = false)
    {
        lock (AddRels)
        {
            if (AddRels.TryGetValue((radix, unary), out var cached)) return cached;
            var d = DigitShape(radix, unary);
            return AddRels[(radix, unary)] = Operations.Balance([d, d, Shape.Bit, d, Shape.Bit], 1, 1, 1, -1, -radix);
        }
    }

    /// <summary>Combined 5-role functional digit adder bound from
    /// <see cref="DigitAddRelation"/> (for radix 2..4; compact 32..256 states).
    /// Roles: a, b, cin (read), sum, cout (written).</summary>
    public static TransitionLaw DigitAddCombined(int radix, bool unary = false)
    {
        if (radix > 4) throw new ArgumentOutOfRangeException(nameof(radix), "Use split DigitSum + DigitCarry for radix > 4.");
        lock (AddCombined)
        {
            if (AddCombined.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitAddRelation(radix, unary);
            return AddCombined[(radix, unary)] = Operations.Bind(rel, rel.Roles, [0, 1, 2, -1, -1], [-1, -1, -1, 3, 4]);
        }
    }

    /// <summary>Single-output digit sum bound from <see cref="DigitAddRelation"/>
    /// with carry-out left free. Fits radix 2..10 inside <see cref="Law.MaxBits"/>
    /// (at base 10: 2,000 states). Roles: a, b, cin (read), sum (written).</summary>
    public static TransitionLaw DigitSum(int radix, bool unary = false)
    {
        lock (AddSums)
        {
            if (AddSums.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitAddRelation(radix, unary);
            var d = rel.Roles[0];
            return AddSums[(radix, unary)] = Operations.Bind(rel, [d, d, Shape.Bit, d], [0, 1, 2, -1, -1], [-1, -1, -1, 3, -1]);
        }
    }

    /// <summary>Single-output digit carry bound from <see cref="DigitAddRelation"/>
    /// with sum left free. Fits radix 2..10 compactly (at base 10: 400 states).
    /// Roles: a, b, cin (read), cout (written).</summary>
    public static TransitionLaw DigitCarry(int radix, bool unary = false)
    {
        lock (AddCarries)
        {
            if (AddCarries.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitAddRelation(radix, unary);
            var d = rel.Roles[0];
            return AddCarries[(radix, unary)] = Operations.Bind(rel, [d, d, Shape.Bit, Shape.Bit], [0, 1, 2, -1, -1], [-1, -1, -1, -1, 3]);
        }
    }

    /// <summary>Reverse directional binding of the EXACT SAME
    /// <see cref="DigitAddRelation"/> for digit subtraction:
    /// <c>diff + sub + bin = min + B·bout</c>. Roles: minuend, subtrahend, bin
    /// (read), diff (written).</summary>
    public static TransitionLaw DigitSubDiff(int radix, bool unary = false)
    {
        lock (SubDiffs)
        {
            if (SubDiffs.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitAddRelation(radix, unary);
            var d = rel.Roles[0];
            // Relation roles: (diff=0, sub=1, bin=2, min=3, bout=4)
            // Law roles:      (min=0, sub=1, bin=2, diff=3)
            return SubDiffs[(radix, unary)] = Operations.Bind(rel, [d, d, Shape.Bit, d], [-1, 1, 2, 0, -1], [3, -1, -1, -1, -1]);
        }
    }

    /// <summary>Borrow-out binding of the EXACT SAME <see cref="DigitAddRelation"/>.
    /// Roles: minuend, subtrahend, bin (read), bout (written).</summary>
    public static TransitionLaw DigitSubBorrow(int radix, bool unary = false)
    {
        lock (SubBorrows)
        {
            if (SubBorrows.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitAddRelation(radix, unary);
            var d = rel.Roles[0];
            return SubBorrows[(radix, unary)] = Operations.Bind(rel, [d, d, Shape.Bit, Shape.Bit], [-1, 1, 2, 0, -1], [-1, -1, -1, -1, 3]);
        }
    }

    /// <summary>Exact radix-B digit multiplication relation:
    /// <c>a · b = lo + B·hi</c>. At base 10: 10,000 bits (1.25 KB).</summary>
    public static RelationLaw DigitMulRelation(int radix, bool unary = false)
    {
        lock (MulRels)
        {
            if (MulRels.TryGetValue((radix, unary), out var cached)) return cached;
            var d = DigitShape(radix, unary);
            return MulRels[(radix, unary)] = RelationLaw.FromPredicate([d, d, d, d], t => t[0] * t[1] == t[2] + radix * t[3]);
        }
    }

    /// <summary>Low digit <c>(a·b) mod B</c> bound from <see cref="DigitMulRelation"/>.
    /// At base 10: 1,000 states (1M bits, 4× below <see cref="Law.MaxBits"/>).</summary>
    public static TransitionLaw DigitMulLo(int radix, bool unary = false)
    {
        lock (MulLos)
        {
            if (MulLos.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitMulRelation(radix, unary);
            var d = rel.Roles[0];
            return MulLos[(radix, unary)] = Operations.Bind(rel, [d, d, d], [0, 1, -1, -1], [-1, -1, 2, -1]);
        }
    }

    /// <summary>High digit <c>(a·b) / B</c> bound from <see cref="DigitMulRelation"/>.
    /// At base 10: 1,000 states (1M bits).</summary>
    public static TransitionLaw DigitMulHi(int radix, bool unary = false)
    {
        lock (MulHis)
        {
            if (MulHis.TryGetValue((radix, unary), out var cached)) return cached;
            var rel = DigitMulRelation(radix, unary);
            var d = rel.Roles[0];
            return MulHis[(radix, unary)] = Operations.Bind(rel, [d, d, d], [0, 1, -1, -1], [-1, -1, -1, 2]);
        }
    }

    /// <summary>Wires a concrete multi-rung ripple-carry adder across any number
    /// of rungs at radix 2..10 using strictly <see cref="DigitAddRelation"/>
    /// bindings. Returns the intermediate carry planes.</summary>
    public static int[] WireAdder(
        Engine engine,
        RungNumber a,
        RungNumber b,
        RungNumber sum,
        int cinPlane,
        int coutPlane,
        Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        if (a.Radix != b.Radix || a.Radix != sum.Radix) throw new ArgumentException("All operands must share a radix.");
        if (a.RungCount != b.RungCount || a.RungCount != sum.RungCount) throw new ArgumentException("All operands must share rung count.");
        var store = sum.Store;
        var k = a.RungCount;
        var carries = new int[k - 1];
        for (var i = 0; i < k - 1; i++) carries[i] = store.AddPlane();

        var useCombined = a.Radix <= 4;
        for (var i = 0; i < k; i++)
        {
            var cIn = i == 0 ? cinPlane : carries[i - 1];
            var cOut = i == k - 1 ? coutPlane : carries[i];
            if (useCombined)
            {
                engine.Add(Tension.Wire(DigitAddCombined(a.Radix, a.Unary),
                    [a.Digits[i].On(a.Store), b.Digits[i].On(b.Store), new Binding(store, cIn), sum.Digits[i].On(store), new Binding(store, cOut)],
                    lanes));
            }
            else
            {
                engine.Add(Tension.Wire(DigitSum(a.Radix, a.Unary),
                    [a.Digits[i].On(a.Store), b.Digits[i].On(b.Store), new Binding(store, cIn), sum.Digits[i].On(store)],
                    lanes));
                engine.Add(Tension.Wire(DigitCarry(a.Radix, a.Unary),
                    [a.Digits[i].On(a.Store), b.Digits[i].On(b.Store), new Binding(store, cIn), new Binding(store, cOut)],
                    lanes));
            }
        }
        return carries;
    }

    /// <summary>Wires a concrete multi-rung ripple-borrow subtractor across any
    /// number of rungs at radix 2..10 using reverse bindings of the exact same
    /// <see cref="DigitAddRelation"/>.</summary>
    public static int[] WireSubtractor(
        Engine engine,
        RungNumber minuend,
        RungNumber subtrahend,
        RungNumber diff,
        int binPlane,
        int boutPlane,
        Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        if (minuend.Radix != subtrahend.Radix || minuend.Radix != diff.Radix) throw new ArgumentException("All operands must share a radix.");
        if (minuend.RungCount != subtrahend.RungCount || minuend.RungCount != diff.RungCount) throw new ArgumentException("All operands must share rung count.");
        var store = diff.Store;
        var k = minuend.RungCount;
        var borrows = new int[k - 1];
        for (var i = 0; i < k - 1; i++) borrows[i] = store.AddPlane();

        var subDiff = DigitSubDiff(minuend.Radix, minuend.Unary);
        var subBorrow = DigitSubBorrow(minuend.Radix, minuend.Unary);
        for (var i = 0; i < k; i++)
        {
            var bIn = i == 0 ? binPlane : borrows[i - 1];
            var bOut = i == k - 1 ? boutPlane : borrows[i];
            engine.Add(Tension.Wire(subDiff,
                [minuend.Digits[i].On(minuend.Store), subtrahend.Digits[i].On(subtrahend.Store), new Binding(store, bIn), diff.Digits[i].On(store)],
                lanes));
            engine.Add(Tension.Wire(subBorrow,
                [minuend.Digits[i].On(minuend.Store), subtrahend.Digits[i].On(subtrahend.Store), new Binding(store, bIn), new Binding(store, bOut)],
                lanes));
        }
        return borrows;
    }

    /// <summary>Wires a concrete multi-rung digit-lattice multiplier
    /// <c>a (K rungs) × b (K rungs) → product (2K rungs)</c> at any radix 2..10
    /// using only <see cref="DigitMulLo"/>, <see cref="DigitMulHi"/>, and
    /// <see cref="WireAdder"/>. Scales cleanly to multi-digit base-10 products
    /// (e.g. 0..99 × 0..99 → 0..9,801) far beyond <see cref="Law.MaxBits"/>.</summary>
    public static void WireMultiplier(Engine engine, RungNumber a, RungNumber b, RungNumber product, Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        if (a.Radix != b.Radix || a.Radix != product.Radix) throw new ArgumentException("All operands must share a radix.");
        if (product.RungCount != a.RungCount + b.RungCount) throw new ArgumentException("Product needs a.RungCount + b.RungCount rungs.");

        var store = product.Store;
        var radix = a.Radix;
        var unary = a.Unary;
        var outWidth = product.RungCount;
        var mulLo = DigitMulLo(radix, unary);
        var mulHi = DigitMulHi(radix, unary);
        var zeroBit = store.AddConstantPlane(false);
        var zeroDigit = new Field(a.Digits[0].Shape, [.. Enumerable.Range(0, a.Digits[0].Shape.Bits).Select(_ => store.AddConstantPlane(false))]);

        // Accumulate partial shifted products (a[i] * b[j]) * radix^(i+j) into running sum of width outWidth.
        RungNumber? acc = null;
        for (var i = 0; i < a.RungCount; i++)
        {
            for (var j = 0; j < b.RungCount; j++)
            {
                var lo = new RungNumber(store, radix, 1, unary).Digits[0];
                var hi = new RungNumber(store, radix, 1, unary).Digits[0];
                engine.Add(Tension.Wire(mulLo, [a.Digits[i].On(a.Store), b.Digits[j].On(b.Store), lo.On(store)], lanes));
                engine.Add(Tension.Wire(mulHi, [a.Digits[i].On(a.Store), b.Digits[j].On(b.Store), hi.On(store)], lanes));

                var termDigits = new Field[outWidth];
                for (var p = 0; p < outWidth; p++)
                    termDigits[p] = p == i + j ? lo : p == i + j + 1 ? hi : zeroDigit;
                var term = new RungNumber(store, radix, termDigits);

                var isLast = i == a.RungCount - 1 && j == b.RungCount - 1;
                if (acc is null)
                {
                    if (isLast)
                    {
                        var zeroNum = new RungNumber(store, radix, [.. Enumerable.Repeat(zeroDigit, outWidth)]);
                        WireAdder(engine, term, zeroNum, product, zeroBit, store.AddPlane(), lanes);
                    }
                    acc = term;
                }
                else
                {
                    var next = isLast ? product : new RungNumber(store, radix, outWidth, unary);
                    WireAdder(engine, acc, term, next, zeroBit, store.AddPlane(), lanes);
                    acc = next;
                }
            }
        }
    }

    /// <summary>Unsigned landmark-centered shear balance
    /// <c>dst' + m·sub = dst + m·add</c> written in place over <c>dst</c>, with
    /// an optional <c>blocked</c> bit role (ops_claude's unsigned dynamics):
    /// no hardcoded sign offsets, live perturbable landmark/rest counts, and
    /// <c>kick.SameTable(drift) == true</c> when <c>m == 1</c> (52× smaller in
    /// <see cref="Transport"/> than non-functional signed shears).</summary>
    public static TransitionLaw LandmarkShear(int trackCapacity, int weight = 1, bool withBlocked = false, bool unary = true)
    {
        if (trackCapacity < 2) throw new ArgumentOutOfRangeException(nameof(trackCapacity));
        if (weight == 0) throw new ArgumentOutOfRangeException(nameof(weight));
        lock (LandmarkShears)
        {
            var key = (trackCapacity, weight, withBlocked, unary);
            if (LandmarkShears.TryGetValue(key, out var cached)) return cached;
            var c = unary ? Shape.Unary(trackCapacity) : Shape.Binary(trackCapacity + 1);
            var rel = Operations.Balance([c, c, c, c], 1, weight, -weight, -1);
            Shape[] roles = withBlocked ? [c, c, c, Shape.Bit] : [c, c, c];
            return LandmarkShears[key] = Operations.Bind(rel, roles, [0, 1, 2, -1], [-1, -1, -1, 0], blocked: withBlocked ? 3 : -1);
        }
    }
}
