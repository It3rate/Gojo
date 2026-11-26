using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Minimal variable-partition numbers layered over pre-numeric counts.
/// Pre-numeric Rung 0 (<see cref="Odv"/>, <see cref="Sides"/>,
/// <see cref="Paths"/>, unary <see cref="Tension"/>, <see cref="Flows"/>) stays
/// completely untouched and usable without numerics.
/// <para><b>Any partition, no forced binary.</b> Each rung carries its own
/// partition factor <c>p ≥ 2</c> (for example <c>[5]</c> for five-star ratings,
/// <c>[2, 5]</c> for half-stars out of five, <c>[10, 6, 10, 6]</c> for
/// minutes:seconds, or <c>[10, 10, 10]</c> for decimal). Every rung column is
/// one linear <see cref="Polynomial"/> with its partition as the weight:
/// <c>a + b + c_in − p·c_out − s = 0</c>.</para>
/// <para><b>Relational first, functional when stepping.</b> Relational ladders
/// (<see cref="Ladder.Add"/>, <see cref="Ladder.Multiply"/>) keep tables tiny
/// (500 bits for base 5, 4,000 bits for base 10), run forward and backward on
/// the same wiring, and keep coarse/unknown digits honestly open. Split
/// functional bindings (<see cref="WireAdder"/>, <see cref="WireSubtractor"/>,
/// <see cref="WireMultiplier"/>) serve live stepping and derived native
/// compilation (<see cref="CompiledPlan"/>).</para></summary>
public static class Numbers
{
    private static readonly Dictionary<(int, bool), Polynomial> DigitSums = [];
    private static readonly Dictionary<(int, bool), Polynomial> DigitProducts = [];
    private static readonly Dictionary<(int, bool), Polynomial> DigitMulPairs = [];
    private static readonly Dictionary<(int, int, bool), RelationLaw> Squares = [];
    private static readonly Dictionary<(int, int, int, bool), TransitionLaw> Admissions = [];
    private static readonly Dictionary<(int, bool), RelationLaw> OpenGaps = [];
    private static readonly Dictionary<(int, int, bool, bool), TransitionLaw> LandmarkShears = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> AddCombined = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> AddSums = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> AddCarries = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> SubDiffs = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> SubBorrows = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> MulLos = [];
    private static readonly Dictionary<(int, bool), TransitionLaw> MulHis = [];

    /// <summary>One digit of a partition <paramref name="partition"/> ≥ 2:
    /// unary count <c>0..p−1</c> (so larger sets more planes and slices raw
    /// material counts directly) or binary-packed <c>0..p−1</c> (codes ≥ p
    /// decode to null, never silent extra values).</summary>
    public static Shape Digit(int partition, bool unary = true)
    {
        if (partition < 2)
            throw new ArgumentOutOfRangeException(nameof(partition), "A partition needs at least two parts.");
        return unary ? Shape.Unary(partition - 1) : Shape.Binary(partition);
    }

    /// <summary>Carry/borrow role between adjacent rungs.</summary>
    public static Shape Carry(bool unary = true) => unary ? Shape.Unary(1) : Shape.Bit;

    /// <summary>One partition-column balance: <c>a + b + c_in − p·c_out − s = 0</c>.
    /// Roles: a, b, carryIn, carryOut, sum. Works at any partition whose
    /// <c>4·p³ ≤ Law.MaxBits</c> (base 5 is 500 bits, base 10 is 4,000 bits,
    /// base 60 is 864,000 bits).</summary>
    public static Polynomial DigitSum(int partition, bool unary = true)
    {
        lock (DigitSums)
        {
            if (DigitSums.TryGetValue((partition, unary), out var cached)) return cached;
            var d = Digit(partition, unary);
            var c = Carry(unary);
            return DigitSums[(partition, unary)] = Polynomial.Balance([d, d, c, c, d], 1, 1, 1, -partition, -1);
        }
    }

    /// <summary>Fused digit multiply-add polynomial:
    /// <c>a·b + c_in − p·c_out − lo = 0</c>. Roles: a, b, carryIn, carryOut, lo.
    /// Base 4 is 1,024 bits; base 5 is 3,125 bits; base 10 is 100,000 bits.</summary>
    public static Polynomial DigitProduct(int partition, bool unary = true)
    {
        lock (DigitProducts)
        {
            if (DigitProducts.TryGetValue((partition, unary), out var cached)) return cached;
            var d = Digit(partition, unary);
            return DigitProducts[(partition, unary)] = new Polynomial(
                [d, d, d, d, d],
                (1, [1, 1, 0, 0, 0]),
                (1, [0, 0, 1, 0, 0]),
                (-partition, [0, 0, 0, 1, 0]),
                (-1, [0, 0, 0, 0, 1]));
        }
    }

    /// <summary>Exact digit product polynomial <c>a·b − p·hi − lo = 0</c>.
    /// Roles: a, b, lo, hi. Base 5 is 625 bits; base 10 is 10,000 bits.</summary>
    public static Polynomial DigitMulPair(int partition, bool unary = true)
    {
        lock (DigitMulPairs)
        {
            if (DigitMulPairs.TryGetValue((partition, unary), out var cached)) return cached;
            var d = Digit(partition, unary);
            return DigitMulPairs[(partition, unary)] = new Polynomial(
                [d, d, d, d],
                (1, [1, 1, 0, 0]),
                (-1, [0, 0, 1, 0]),
                (-partition, [0, 0, 0, 1]));
        }
    }

    /// <summary>1D bidirectional square table over (n, low, high) where
    /// <c>n² = low + radix · high</c>.</summary>
    public static RelationLaw SquareDigits(int maxN, int radix, bool unary = false)
    {
        if (maxN < 1) throw new ArgumentOutOfRangeException(nameof(maxN));
        if (radix < 2 || maxN * maxN >= radix * radix)
            throw new ArgumentOutOfRangeException(nameof(radix), "Two radix digits must hold maxN².");
        lock (Squares)
        {
            if (Squares.TryGetValue((maxN, radix, unary), out var cached)) return cached;
            var input = unary ? Shape.Unary(maxN) : Shape.Binary(maxN + 1);
            var d = Digit(radix, unary);
            return Squares[(maxN, radix, unary)] = RelationLaw.FromPredicate(
                [input, d, d],
                t => t[0] * t[0] == t[1] + radix * t[2]);
        }
    }

    /// <summary>Conserving carry upward between unary rungs: takes
    /// <paramref name="partition"/> fine units and gives 1 coarse unit,
    /// preserving <c>1·fine + partition·coarse</c>.</summary>
    public static TransitionLaw Group(int partition, int fineCapacity, int coarseCapacity) =>
        Flows.Convert(partition, 1, fineCapacity, coarseCapacity);

    /// <summary>Conserving refinement downward between unary rungs: takes 1
    /// coarse unit and gives <paramref name="partition"/> fine units.</summary>
    public static TransitionLaw Refine(int partition, int coarseCapacity, int fineCapacity) =>
        Flows.Convert(1, partition, coarseCapacity, fineCapacity);

    /// <summary>Atomic, no-overshoot material admission into a two-rung pair
    /// (generalizing <c>ops_sol</c>'s <c>RadixFour.Admit</c> to any partition
    /// <paramref name="partition"/> ≥ 2). Roles: pressure (unary), coarse
    /// (<c>0..coarseCapacity</c>), fine (<c>0..partition−1</c>). Admits one
    /// fine unit per step, carrying <c>(c, p−1) → (c+1, 0)</c> atomically so
    /// the fine digit never overshoots its partition; when full, excess waits
    /// visibly as supplier pressure. Checked by
    /// <c>PreservedSum(pressure + partition·coarse + fine)</c>.</summary>
    public static TransitionLaw Admit(int partition, int coarseCapacity, int pressureCapacity = 1, bool unary = true)
    {
        if (partition < 2) throw new ArgumentOutOfRangeException(nameof(partition));
        if (coarseCapacity < 1) throw new ArgumentOutOfRangeException(nameof(coarseCapacity));
        if (pressureCapacity < 1) throw new ArgumentOutOfRangeException(nameof(pressureCapacity));
        lock (Admissions)
        {
            var key = (partition, coarseCapacity, pressureCapacity, unary);
            if (Admissions.TryGetValue(key, out var cached)) return cached;
            var coarse = unary ? Shape.Unary(coarseCapacity) : Shape.Binary(coarseCapacity + 1);
            var fine = Digit(partition, unary);
            return Admissions[key] = TransitionLaw.FromFunction(
                [Shape.Unary(pressureCapacity), coarse, fine],
                s =>
                {
                    if (s[0] == 0 || (s[1] == coarseCapacity && s[2] == partition - 1)) return s;
                    return s[2] == partition - 1
                        ? [s[0] - 1, s[1] + 1, 0]
                        : [s[0] - 1, s[1], s[2] + 1];
                },
                new Accounting(true, new PreservedSum([0, 1, 2], [1, partition, 1]), new NeverIncreases(0)));
        }
    }

    /// <summary>Exact bidirectional two-faced numeric gap over
    /// (have, want, deficit, excess) where <c>have + deficit = want + excess</c>
    /// and at least one of deficit, excess is zero.</summary>
    public static RelationLaw OpenGap(int capacity, bool unary = true)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        lock (OpenGaps)
        {
            if (OpenGaps.TryGetValue((capacity, unary), out var cached)) return cached;
            var count = unary ? Shape.Unary(capacity) : Shape.Binary(capacity + 1);
            return OpenGaps[(capacity, unary)] = RelationLaw.FromPredicate(
                [count, count, count, count],
                t => t[0] + t[2] == t[1] + t[3] && (t[2] == 0 || t[3] == 0));
        }
    }

    /// <summary>Unsigned landmark-centered shear balance
    /// <c>dst' + weight·sub = dst + weight·add</c> written in place over
    /// <c>dst</c>, with optional <c>blocked</c> bit role. No hardcoded sign
    /// offsets, live perturbable landmark/rest counts, and
    /// <c>kick.SameTable(drift) == true</c> when <c>weight == 1</c>.</summary>
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

    // ---- Concrete functional stepping laws (derived from DigitSum / DigitMulPair) ----

    /// <summary>Combined 5-role functional digit adder for small partitions
    /// <c>2..4</c>. Roles: a, b, cin (read), sum, cout (written).</summary>
    public static TransitionLaw DigitAddStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 4)
            throw new ArgumentOutOfRangeException(nameof(partition), "Combined 5-role transition supports partition 2..4; use split DigitSumStep + DigitCarryStep for 5..10.");
        lock (AddCombined)
        {
            if (AddCombined.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitSum(partition, unary).Relation;
            var d = rel.Roles[0];
            var c = rel.Roles[2];
            // DigitSum roles: (a=0, b=1, cin=2, cout=3, sum=4); law roles: (a, b, cin, sum, cout)
            return AddCombined[(partition, unary)] = Operations.Bind(rel, [d, d, c, d, c], [0, 1, 2, -1, -1], [-1, -1, -1, 4, 3]);
        }
    }

    /// <summary>Single-output functional digit sum bound from
    /// <see cref="DigitSum"/> (carry-out free). Fits partition <c>2..10</c>.
    /// Roles: a, b, cin (read), sum (written).</summary>
    public static TransitionLaw DigitSumStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 10)
            throw new ArgumentOutOfRangeException(nameof(partition), "Functional digit sum fits Law.MaxBits for partition 2..10.");
        lock (AddSums)
        {
            if (AddSums.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitSum(partition, unary).Relation;
            var d = rel.Roles[0];
            var c = rel.Roles[2];
            return AddSums[(partition, unary)] = Operations.Bind(rel, [d, d, c, d], [0, 1, 2, -1, -1], [-1, -1, -1, -1, 3]);
        }
    }

    /// <summary>Single-output functional digit carry bound from
    /// <see cref="DigitSum"/> (sum free). Roles: a, b, cin (read), cout (written).</summary>
    public static TransitionLaw DigitCarryStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 10)
            throw new ArgumentOutOfRangeException(nameof(partition), "Functional digit carry fits Law.MaxBits for partition 2..10.");
        lock (AddCarries)
        {
            if (AddCarries.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitSum(partition, unary).Relation;
            var d = rel.Roles[0];
            var c = rel.Roles[2];
            return AddCarries[(partition, unary)] = Operations.Bind(rel, [d, d, c, c], [0, 1, 2, -1, -1], [-1, -1, -1, 3, -1]);
        }
    }

    /// <summary>Reverse functional digit difference bound from the exact same
    /// <see cref="DigitSum"/>: <c>diff + sub + bin = min + p·bout</c>.
    /// Roles: minuend, subtrahend, bin (read), diff (written).</summary>
    public static TransitionLaw DigitSubDiffStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 10)
            throw new ArgumentOutOfRangeException(nameof(partition), "Functional digit sub fits Law.MaxBits for partition 2..10.");
        lock (SubDiffs)
        {
            if (SubDiffs.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitSum(partition, unary).Relation;
            var d = rel.Roles[0];
            var c = rel.Roles[2];
            // Relation roles: (diff=0, sub=1, bin=2, bout=3, min=4); law roles: (min=0, sub=1, bin=2, diff=3)
            return SubDiffs[(partition, unary)] = Operations.Bind(rel, [d, d, c, d], [-1, 1, 2, -1, 0], [3, -1, -1, -1, -1]);
        }
    }

    /// <summary>Reverse functional digit borrow bound from the exact same
    /// <see cref="DigitSum"/>. Roles: minuend, subtrahend, bin (read), bout (written).</summary>
    public static TransitionLaw DigitSubBorrowStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 10)
            throw new ArgumentOutOfRangeException(nameof(partition), "Functional digit borrow fits Law.MaxBits for partition 2..10.");
        lock (SubBorrows)
        {
            if (SubBorrows.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitSum(partition, unary).Relation;
            var d = rel.Roles[0];
            var c = rel.Roles[2];
            return SubBorrows[(partition, unary)] = Operations.Bind(rel, [d, d, c, c], [-1, 1, 2, -1, 0], [-1, -1, -1, 3, -1]);
        }
    }

    /// <summary>Low digit <c>(a·b) mod p</c> bound from <see cref="DigitMulPair"/>.</summary>
    public static TransitionLaw DigitMulLoStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 10) throw new ArgumentOutOfRangeException(nameof(partition));
        lock (MulLos)
        {
            if (MulLos.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitMulPair(partition, unary).Relation;
            var d = rel.Roles[0];
            return MulLos[(partition, unary)] = Operations.Bind(rel, [d, d, d], [0, 1, -1, -1], [-1, -1, 2, -1]);
        }
    }

    /// <summary>High digit <c>(a·b) / p</c> bound from <see cref="DigitMulPair"/>.</summary>
    public static TransitionLaw DigitMulHiStep(int partition, bool unary = false)
    {
        if (partition is < 2 or > 10) throw new ArgumentOutOfRangeException(nameof(partition));
        lock (MulHis)
        {
            if (MulHis.TryGetValue((partition, unary), out var cached)) return cached;
            var rel = DigitMulPair(partition, unary).Relation;
            var d = rel.Roles[0];
            return MulHis[(partition, unary)] = Operations.Bind(rel, [d, d, d], [0, 1, -1, -1], [-1, -1, -1, 2]);
        }
    }

    /// <summary>Wires a concrete ripple-carry adder over matching (possibly
    /// mixed-partition) ladders <c>a + b + cin = sum + cout·(Capacity+1)</c>.</summary>
    public static int[] WireAdder(Engine engine, Ladder a, Ladder b, Ladder sum, int cinPlane, int coutPlane, Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ValidateMatchingLadders(a, b, sum);
        var store = sum.Store;
        var k = a.Partitions.Count;
        var carries = new int[k - 1];
        for (var i = 0; i < k - 1; i++) carries[i] = store.AddPlane();

        for (var i = 0; i < k; i++)
        {
            var p = a.Partitions[i];
            var cIn = i == 0 ? cinPlane : carries[i - 1];
            var cOut = i == k - 1 ? coutPlane : carries[i];
            if (p <= 4)
            {
                engine.Add(Tension.Wire(DigitAddStep(p, a.Unary),
                    [a.Digits[i].On(a.Store), b.Digits[i].On(b.Store), new Binding(store, cIn), sum.Digits[i].On(store), new Binding(store, cOut)],
                    lanes));
            }
            else
            {
                engine.Add(Tension.Wire(DigitSumStep(p, a.Unary),
                    [a.Digits[i].On(a.Store), b.Digits[i].On(b.Store), new Binding(store, cIn), sum.Digits[i].On(store)],
                    lanes));
                engine.Add(Tension.Wire(DigitCarryStep(p, a.Unary),
                    [a.Digits[i].On(a.Store), b.Digits[i].On(b.Store), new Binding(store, cIn), new Binding(store, cOut)],
                    lanes));
            }
        }
        return carries;
    }

    /// <summary>Wires a concrete ripple-borrow subtractor over matching
    /// (possibly mixed-partition) ladders using reverse bindings of
    /// <see cref="DigitSum"/>.</summary>
    public static int[] WireSubtractor(Engine engine, Ladder minuend, Ladder subtrahend, Ladder diff, int binPlane, int boutPlane, Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ValidateMatchingLadders(minuend, subtrahend, diff);
        var store = diff.Store;
        var k = minuend.Partitions.Count;
        var borrows = new int[k - 1];
        for (var i = 0; i < k - 1; i++) borrows[i] = store.AddPlane();

        for (var i = 0; i < k; i++)
        {
            var p = minuend.Partitions[i];
            var bIn = i == 0 ? binPlane : borrows[i - 1];
            var bOut = i == k - 1 ? boutPlane : borrows[i];
            engine.Add(Tension.Wire(DigitSubDiffStep(p, minuend.Unary),
                [minuend.Digits[i].On(minuend.Store), subtrahend.Digits[i].On(subtrahend.Store), new Binding(store, bIn), diff.Digits[i].On(store)],
                lanes));
            engine.Add(Tension.Wire(DigitSubBorrowStep(p, minuend.Unary),
                [minuend.Digits[i].On(minuend.Store), subtrahend.Digits[i].On(subtrahend.Store), new Binding(store, bIn), new Binding(store, bOut)],
                lanes));
        }
        return borrows;
    }

    /// <summary>Wires a concrete uniform-radix multi-rung multiplier
    /// <c>a (K rungs) × b (M rungs) → product (K+M rungs)</c> for radix 2..10
    /// (e.g., <c>0..99 × 0..99 → 0..9,801</c>).</summary>
    public static void WireMultiplier(Engine engine, Ladder a, Ladder b, Ladder product, Plane lanes)
    {
        ArgumentNullException.ThrowIfNull(engine);
        if (!a.IsUniformRadix || !b.IsUniformRadix || !product.IsUniformRadix || a.Partitions[0] != b.Partitions[0] || a.Partitions[0] != product.Partitions[0])
            throw new ArgumentException("WireMultiplier expects uniform-radix ladders sharing a radix.");
        if (a.Unary != b.Unary || a.Unary != product.Unary)
            throw new ArgumentException("Operands must share digit encoding.");
        if (product.Partitions.Count != a.Partitions.Count + b.Partitions.Count)
            throw new ArgumentException("Product needs a.RungCount + b.RungCount rungs.");

        var store = product.Store;
        var radix = a.Partitions[0];
        var unary = a.Unary;
        var outWidth = product.Partitions.Count;
        var mulLo = DigitMulLoStep(radix, unary);
        var mulHi = DigitMulHiStep(radix, unary);
        var zeroBit = store.AddConstantPlane(false);
        var digitShape = Digit(radix, unary);
        var zeroDigit = new Field(digitShape, [.. Enumerable.Range(0, digitShape.Bits).Select(_ => store.AddConstantPlane(false))]);

        Ladder? acc = null;
        for (var i = 0; i < a.Partitions.Count; i++)
        {
            for (var j = 0; j < b.Partitions.Count; j++)
            {
                var lo = new Field(digitShape, [.. Enumerable.Range(0, digitShape.Bits).Select(_ => store.AddPlane())]);
                var hi = new Field(digitShape, [.. Enumerable.Range(0, digitShape.Bits).Select(_ => store.AddPlane())]);
                engine.Add(Tension.Wire(mulLo, [a.Digits[i].On(a.Store), b.Digits[j].On(b.Store), lo.On(store)], lanes));
                engine.Add(Tension.Wire(mulHi, [a.Digits[i].On(a.Store), b.Digits[j].On(b.Store), hi.On(store)], lanes));

                var termDigits = new Field[outWidth];
                for (var p = 0; p < outWidth; p++)
                    termDigits[p] = p == i + j ? lo : p == i + j + 1 ? hi : zeroDigit;
                var term = new Ladder(store, radix, termDigits);

                var isLast = i == a.Partitions.Count - 1 && j == b.Partitions.Count - 1;
                if (acc is null)
                {
                    if (isLast)
                    {
                        var zeroNum = new Ladder(store, radix, [.. Enumerable.Repeat(zeroDigit, outWidth)]);
                        WireAdder(engine, term, zeroNum, product, zeroBit, store.AddPlane(), lanes);
                    }
                    acc = term;
                }
                else
                {
                    var next = isLast ? product : new Ladder(store, radix, outWidth, unary);
                    WireAdder(engine, acc, term, next, zeroBit, store.AddPlane(), lanes);
                    acc = next;
                }
            }
        }
    }

    internal static void ValidateMatchingLadders(Ladder a, Ladder b, Ladder sum)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(sum);
        if (!a.Partitions.SequenceEqual(b.Partitions) || !a.Partitions.SequenceEqual(sum.Partitions))
            throw new ArgumentException("Ladders must share the same per-rung partitions.");
        if (a.Unary != b.Unary || a.Unary != sum.Unary)
            throw new ArgumentException("Ladders must share unary/binary digit encoding.");
    }
}

/// <summary>A variable-partition multi-rung number held in ordinary
/// <see cref="Store"/> planes (finest rung 0 to coarsest rung <c>k−1</c>).
/// Each rung <c>r</c> has its own partition factor <see cref="Partitions"/><c>[r]</c>
/// (uniform radix is simply the equal-partitions special case).
/// <para><see cref="UnitRung"/> anchors which rung boundary represents one
/// whole unit: <c>0</c> for integers, <c>Partitions.Count</c> for a bounded
/// partitioned whole (such as five-star <c>[5]</c> or half-stars
/// <c>[2, 5]</c>), or an interior index for fixed-point scales
/// (<c>[10, 10 | 10, 10]</c>).</para>
/// <para>Supports are allocated lazily on first relational access so pure
/// concrete/compiled ladders pay zero extra planes, while relational
/// narrowing (<see cref="Add"/>, <see cref="Multiply"/>) keeps open digits
/// and coarse uncertainty intact.</para></summary>
public sealed class Ladder
{
    private readonly int[] partitions;
    private readonly Field[] digits;
    private Supports[]? possible;

    /// <summary>Fresh variable-partition ladder (finest partition first).</summary>
    public Ladder(Store store, int[] partitions, int unitRung = 0, bool unary = true)
        : this(store, partitions, AllocateDigits(store, partitions, unary), unitRung)
    {
    }

    /// <summary>Fresh uniform-radix ladder (<c>[radix, radix, …]</c>).</summary>
    public Ladder(Store store, int radix, int rungs, bool unary = true, int unitRung = 0)
        : this(store, Uniform(radix, rungs), unitRung, unary)
    {
    }

    /// <summary>Variable-partition ladder viewed over existing digit fields
    /// (for example the first <c>p−1</c> planes of pre-numeric unary counts).</summary>
    public Ladder(Store store, int[] partitions, IReadOnlyList<Field> digits, int unitRung = 0)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(partitions);
        ArgumentNullException.ThrowIfNull(digits);
        if (partitions.Length == 0) throw new ArgumentException("A ladder needs at least one rung.", nameof(partitions));
        if (digits.Count != partitions.Length) throw new ArgumentException("Supply one field per partition.", nameof(digits));
        if ((uint)unitRung > (uint)partitions.Length)
            throw new ArgumentOutOfRangeException(nameof(unitRung), "UnitRung must lie in 0..Partitions.Count.");
        var unary = digits[0].Shape.IsUnary;
        for (var i = 0; i < partitions.Length; i++)
        {
            if (partitions[i] < 2) throw new ArgumentOutOfRangeException(nameof(partitions), "Each partition must be at least 2.");
            var expected = Numbers.Digit(partitions[i], unary);
            if (digits[i].Shape != expected)
                throw new ArgumentException($"Rung {i} expects shape {expected}, got {digits[i].Shape}.", nameof(digits));
        }
        Store = store;
        this.partitions = (int[])partitions.Clone();
        this.digits = digits.ToArray();
        Partitions = Array.AsReadOnly(this.partitions);
        Digits = Array.AsReadOnly(this.digits);
        UnitRung = unitRung;
        Unary = unary;
    }

    /// <summary>Uniform-radix view over existing digit fields.</summary>
    public Ladder(Store store, int radix, IReadOnlyList<Field> digits, int unitRung = 0)
        : this(store, Uniform(radix, digits?.Count ?? 0), digits!, unitRung)
    {
    }

    public Store Store { get; }
    public IReadOnlyList<int> Partitions { get; }
    public IReadOnlyList<Field> Digits { get; }
    public int UnitRung { get; }
    public bool Unary { get; }
    public int RungCount => partitions.Length;
    public bool IsUniformRadix => partitions.All(p => p == partitions[0]);

    /// <summary>The largest whole tick count held across all rungs:
    /// <c>Π Partitions[i] − 1</c>.</summary>
    public long Capacity => checked(partitions.Aggregate(1L, (acc, p) => checked(acc * p)) - 1);

    /// <summary>How many finest ticks equal one whole unit at
    /// <see cref="UnitRung"/> (<c>1</c> for integers, <c>5</c> for <c>[5]</c>
    /// anchored at top, <c>10</c> for half-stars <c>[2, 5]</c> anchored at top).</summary>
    public long UnitScale
    {
        get
        {
            long scale = 1;
            for (var i = 0; i < UnitRung; i++) scale = checked(scale * partitions[i]);
            return scale;
        }
    }

    /// <summary>Place value of rung <paramref name="rung"/> in finest ticks.</summary>
    public long PlaceWeight(int rung)
    {
        if ((uint)rung >= (uint)partitions.Length) throw new ArgumentOutOfRangeException(nameof(rung));
        long weight = 1;
        for (var i = 0; i < rung; i++) weight = checked(weight * partitions[i]);
        return weight;
    }

    /// <summary>Per-rung open digit supports (allocated lazily on first access).</summary>
    public IReadOnlyList<Supports> Possible => Array.AsReadOnly(EnsureSupports());

    /// <summary>Atomically writes a concrete tick count across all rungs
    /// (validating range BEFORE touching any plane in <see cref="Store"/>).</summary>
    public void WriteConcrete(int position, long value)
    {
        if (value < 0 || value > Capacity)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is outside 0..{Capacity}.");
        var rem = value;
        for (var i = 0; i < digits.Length; i++)
        {
            digits[i].Write(Store, position, (int)(rem % partitions[i]));
            rem /= partitions[i];
        }
    }

    /// <summary>Reads the concrete tick count directly from the digit planes;
    /// returns null if any rung holds a malformed code.</summary>
    public long? ReadConcrete(Surface surface, int position)
    {
        long total = 0, weight = 1;
        for (var i = 0; i < digits.Length; i++)
        {
            var d = digits[i].Read(surface, position);
            if (d is null) return null;
            total += d.Value * weight;
            weight *= partitions[i];
        }
        return total;
    }

    public long? ReadConcrete(int position) => ReadConcrete(Store, position);

    /// <summary>Narrows every rung's support to the digits of
    /// <paramref name="value"/> (validated before mutating any support).</summary>
    public void Restrict(int position, long value)
    {
        if (value < 0 || value > Capacity)
            throw new ArgumentOutOfRangeException(nameof(value), $"Value {value} is outside 0..{Capacity}.");
        var sup = EnsureSupports();
        var rem = value;
        for (var r = 0; r < sup.Length; r++)
        {
            sup[r].Restrict(position, 1UL << (int)(rem % partitions[r]));
            rem /= partitions[r];
        }
    }

    /// <summary>Narrows one rung's support mask, leaving other rungs as they are.</summary>
    public void Restrict(int position, int rung, ulong digitsMask) => EnsureSupports()[rung].Restrict(position, digitsMask);

    /// <summary>Narrows every rung's support to the value its digit field holds.</summary>
    public void Observe(int position)
    {
        foreach (var rung in EnsureSupports()) rung.Observe(position);
    }

    /// <summary>Explicitly reopens every rung's support at
    /// <paramref name="position"/> back to all-possible (required before
    /// re-narrowing on a changed source under monotone narrowing).</summary>
    public void Reopen(int position)
    {
        foreach (var rung in EnsureSupports()) rung.Forget(position);
    }

    /// <summary>Per-rung masks of digits still possible, finest first.</summary>
    public ulong[] Open(int position) => [.. EnsureSupports().Select(p => p.Values(position))];

    /// <summary>Settled tick count when every rung has exactly one digit left,
    /// else null (open or contradicted).</summary>
    public long? Read(int position)
    {
        var sup = EnsureSupports();
        long value = 0, weight = 1;
        for (var r = 0; r < sup.Length; r++)
        {
            var mask = sup[r].Values(position);
            if (mask == 0 || (mask & (mask - 1)) != 0) return null;
            value += weight * BitOperations.TrailingZeroCount(mask);
            weight *= partitions[r];
        }
        return value;
    }

    /// <summary>Enumerates all tick counts admitted by the current per-rung
    /// open digit supports (in ascending order; empty if any rung is contradicted).</summary>
    public long[] PossibleValues(int position)
    {
        var sup = EnsureSupports();
        var masks = sup.Select(s => s.Values(position)).ToArray();
        if (masks.Any(m => m == 0)) return [];
        var acc = new List<long> { 0 };
        long weight = 1;
        for (var r = 0; r < masks.Length; r++)
        {
            var next = new List<long>();
            var mask = masks[r];
            for (var d = 0; d < partitions[r]; d++)
            {
                if (((mask >> d) & 1) == 0) continue;
                var add = d * weight;
                foreach (var v in acc) next.Add(v + add);
            }
            acc = next;
            weight *= partitions[r];
        }
        acc.Sort();
        return [.. acc];
    }

    /// <summary>Wires relational mixed-partition addition
    /// <c>a + b = sum</c> rung by rung using <see cref="Numbers.DigitSum"/>.
    /// Runs forward (sum), backward (subtraction), and on open/coarse digits.
    /// The finest carry-in is held at 0; the top carry-out stays open for
    /// visible overflow/borrow (or <c>1.0</c> whole on a bounded partitioned scale).</summary>
    public static (IReadOnlyList<Wiring> Wirings, IReadOnlyList<Supports> Carries) Add(
        Ladder a, Ladder b, Ladder sum, Plane positions)
    {
        Numbers.ValidateMatchingLadders(a, b, sum);
        ArgumentNullException.ThrowIfNull(positions);
        if (a.Store != b.Store || a.Store != sum.Store)
            throw new ArgumentException("Relational Ladder.Add wires within one store.", nameof(sum));
        var store = a.Store;
        var aSup = a.EnsureSupports();
        var bSup = b.EnsureSupports();
        var sSup = sum.EnsureSupports();
        var carryShape = Numbers.Carry(a.Unary);
        var carries = Enumerable.Range(0, a.RungCount + 1)
            .Select(_ => Supports.On(store, new Field(carryShape, store.AddPlane())))
            .ToArray();
        foreach (var position in positions.Indexes()) carries[0].Restrict(position, 1UL);

        var wirings = new Wiring[a.RungCount];
        for (var r = 0; r < a.RungCount; r++)
        {
            var column = Numbers.DigitSum(a.partitions[r], a.Unary).Relation;
            var w = new Wiring(column, aSup[r].Variable, bSup[r].Variable, carries[r].Variable, carries[r + 1].Variable, sSup[r].Variable);
            foreach (var position in positions.Indexes()) w.Add(position, position, position, position, position);
            wirings[r] = w;
        }
        return (Array.AsReadOnly(wirings), Array.AsReadOnly(carries));
    }

    /// <summary>Wires relational uniform-radix multiplication
    /// <c>a (K rungs) × b (M rungs) = product (K+M rungs)</c> over
    /// <see cref="Supports"/> using <see cref="Numbers.DigitProduct"/> and
    /// <see cref="Numbers.DigitSum"/>. Works forward, backward (factoring /
    /// division), and on open digits.</summary>
    public static IReadOnlyList<Wiring> Multiply(Ladder a, Ladder b, Ladder product, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(positions);
        if (a.Store != b.Store || a.Store != product.Store)
            throw new ArgumentException("Relational Ladder.Multiply wires within one store.", nameof(product));
        if (!a.IsUniformRadix || !b.IsUniformRadix || !product.IsUniformRadix || a.partitions[0] != b.partitions[0] || a.partitions[0] != product.partitions[0])
            throw new ArgumentException("Relational Ladder.Multiply expects uniform-radix ladders sharing a radix.");
        if (a.Unary != b.Unary || a.Unary != product.Unary)
            throw new ArgumentException("Ladders must share digit encoding.");
        if (product.RungCount != a.RungCount + b.RungCount)
            throw new ArgumentException("Product needs a.RungCount + b.RungCount rungs.");

        var store = a.Store;
        var radix = a.partitions[0];
        var unary = a.Unary;
        var digitShape = Numbers.Digit(radix, unary);
        var aSup = a.EnsureSupports();
        var bSup = b.EnsureSupports();
        var pSup = product.EnsureSupports();
        var posList = positions.Indexes().ToArray();

        Supports NewDigit(bool zero = false)
        {
            var f = new Field(digitShape, [.. Enumerable.Range(0, digitShape.Bits).Select(_ => store.AddPlane())]);
            var s = Supports.On(store, f);
            if (zero) foreach (var pos in posList) s.Restrict(pos, 1UL);
            return s;
        }

        var wirings = new List<Wiring>();
        var zeroDigit = NewDigit(zero: true);
        var prodRel = Numbers.DigitProduct(radix, unary).Relation;
        var outWidth = product.RungCount;

        // Build one row per b[j]: row = a * b[j] shifted by j, using DigitProduct(a[i], b[j], c_in, c_out, lo).
        Ladder? acc = null;
        for (var j = 0; j < b.RungCount; j++)
        {
            var rowDigits = new Supports[outWidth];
            for (var p = 0; p < outWidth; p++) rowDigits[p] = zeroDigit;

            var carry = zeroDigit;
            for (var i = 0; i < a.RungCount; i++)
            {
                var lo = NewDigit();
                var nextCarry = NewDigit();
                var w = new Wiring(prodRel, aSup[i].Variable, bSup[j].Variable, carry.Variable, nextCarry.Variable, lo.Variable);
                foreach (var pos in posList) w.Add(pos, pos, pos, pos, pos);
                wirings.Add(w);
                rowDigits[i + j] = lo;
                carry = nextCarry;
            }
            rowDigits[a.RungCount + j] = carry;

            var rowLadder = FromSupports(store, Uniform(radix, outWidth), rowDigits, unary);
            var isLast = j == b.RungCount - 1;
            if (acc is null)
            {
                if (isLast)
                {
                    var zeroLadder = FromSupports(store, Uniform(radix, outWidth), [.. Enumerable.Repeat(zeroDigit, outWidth)], unary);
                    var (addW, carries) = Add(rowLadder, zeroLadder, product, positions);
                    foreach (var pos in posList) carries[^1].Restrict(pos, 1UL);
                    wirings.AddRange(addW);
                }
                acc = rowLadder;
            }
            else
            {
                var next = isLast ? product : new Ladder(store, radix, outWidth, unary);
                var (addW, carries) = Add(acc, rowLadder, next, positions);
                foreach (var pos in posList) carries[^1].Restrict(pos, 1UL);
                wirings.AddRange(addW);
                acc = next;
            }
        }
        return wirings.AsReadOnly();
    }

    private static Ladder FromSupports(Store store, int[] partitions, Supports[] supports, bool unary)
    {
        var l = new Ladder(store, partitions, [.. supports.Select(s => s.Field)], unitRung: 0);
        l.possible = supports;
        return l;
    }

    private Supports[] EnsureSupports() => possible ??= [.. digits.Select(d => Supports.On(Store, d))];

    private static int[] Uniform(int radix, int rungs)
    {
        if (radix < 2) throw new ArgumentOutOfRangeException(nameof(radix));
        if (rungs < 1) throw new ArgumentOutOfRangeException(nameof(rungs));
        var arr = new int[rungs];
        Array.Fill(arr, radix);
        return arr;
    }

    private static Field[] AllocateDigits(Store store, int[] partitions, bool unary)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(partitions);
        var fields = new Field[partitions.Length];
        for (var i = 0; i < partitions.Length; i++)
        {
            var shape = Numbers.Digit(partitions[i], unary);
            fields[i] = new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);
        }
        return fields;
    }
}

/// <summary>Soft-denominator portion relations for variable-partition scales,
/// evidence pooling, and Axiom-8 projection residue (zero floats, zero silent
/// fraction reduction).
/// <para>Distinguishes three fundamentally different additive/comparative acts
/// that ordinary rational fractions conflate:
/// <list type="bullet">
/// <item><b>Pooling (<see cref="WirePool"/>):</b> <c>(3, 5) ⊕ (2, 5) = (5, 10)</c>,
/// preserving accumulated certainty and sample count without reducing to <c>1/2</c>.</item>
/// <item><b>Same-whole addition (<see cref="AddSameWhole"/>):</b> <c>3/5 + 2/5 = 1 whole + 0/5</c>,
/// refusing incompatible denominators (<c>d₁ ≠ d₂</c>) via empty support.</item>
/// <item><b>Axiom-8 re-partition projection (<see cref="Project"/>):</b>
/// <c>n·d₂ = q·d₁ + r</c> (<c>0 ≤ r &lt; d₁</c>), keeping exact source-basis
/// residue visible (<c>3/5 → 4/8</c> with residue <c>4</c>) rather than rounding.</item>
/// </list></para></summary>
public static class Portions
{
    private static readonly Dictionary<(int, bool), RelationLaw> Bounds = [];
    private static readonly Dictionary<(int, bool), RelationLaw> SameWholeAdds = [];
    private static readonly Dictionary<(int, bool), RelationLaw> Projections = [];
    private static readonly Dictionary<(int, bool), RelationLaw> Comparisons = [];

    private static Shape CountShape(int max, bool unary) =>
        unary ? Shape.Unary(max) : Shape.Binary(max + 1);

    /// <summary>Live-denominator portion bound <c>n + room = d</c> with
    /// <c>d ≥ 1</c>. Roles: numerator, room, denominator. Zero denominator is
    /// excluded (contradiction if forced).</summary>
    public static RelationLaw Bound(int maxDenominator, bool unary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (Bounds)
        {
            if (Bounds.TryGetValue((maxDenominator, unary), out var cached)) return cached;
            var s = CountShape(maxDenominator, unary);
            return Bounds[(maxDenominator, unary)] = RelationLaw.FromPredicate(
                [s, s, s],
                t => t[2] >= 1 && t[0] + t[1] == t[2]);
        }
    }

    /// <summary>Same-whole addition over a live shared denominator:
    /// <c>n₁ + n₂ = whole·d + rem</c> with <c>d ≥ 1</c>, <c>n₁ ≤ d</c>,
    /// <c>n₂ ≤ d</c>, <c>rem &lt; d</c>. Roles: n1, n2, d, whole (bit), rem.</summary>
    public static RelationLaw AddSameWhole(int maxDenominator, bool unary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (SameWholeAdds)
        {
            if (SameWholeAdds.TryGetValue((maxDenominator, unary), out var cached)) return cached;
            var s = CountShape(maxDenominator, unary);
            var w = Numbers.Carry(unary);
            return SameWholeAdds[(maxDenominator, unary)] = RelationLaw.FromPredicate(
                [s, s, s, w, s],
                t => t[2] >= 1 && t[0] <= t[2] && t[1] <= t[2] && t[4] < t[2] && t[0] + t[1] == t[3] * t[2] + t[4]);
        }
    }

    /// <summary>Exact Gojo Axiom-8 re-partition projection:
    /// <c>n·d₂ = q·d₁ + r</c> with <c>d₁ ≥ 1, d₂ ≥ 1, n ≤ d₁, q ≤ d₂, 0 ≤ r &lt; d₁</c>.
    /// Roles: n, d1, d2, q, r. Preserves exact source-basis residue <c>r</c>
    /// (never rounds) and rejects <c>d₁ = 0</c> or <c>d₂ = 0</c>.</summary>
    public static RelationLaw Project(int maxDenominator, bool unary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (Projections)
        {
            if (Projections.TryGetValue((maxDenominator, unary), out var cached)) return cached;
            var s = CountShape(maxDenominator, unary);
            return Projections[(maxDenominator, unary)] = RelationLaw.FromPredicate(
                [s, s, s, s, s],
                t => t[1] >= 1 && t[2] >= 1 && t[0] <= t[1] && t[3] <= t[2] && t[4] < t[1] && t[0] * t[2] == t[3] * t[1] + t[4]);
        }
    }

    /// <summary>Exact cross-multiplication comparison across arbitrary
    /// denominators without floats or binary conversion. Roles: n1, d1, n2, d2,
    /// order (<c>0</c> when <c>n₁/d₁ &lt; n₂/d₂</c>, <c>1</c> when equal in
    /// magnitude, <c>2</c> when greater). Denominators remain distinct.</summary>
    public static RelationLaw Compare(int maxDenominator, bool unary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (Comparisons)
        {
            if (Comparisons.TryGetValue((maxDenominator, unary), out var cached)) return cached;
            var s = CountShape(maxDenominator, unary);
            var ord = Shape.Binary(3);
            return Comparisons[(maxDenominator, unary)] = RelationLaw.FromPredicate(
                [s, s, s, s, ord],
                t =>
                {
                    if (t[1] < 1 || t[3] < 1 || t[0] > t[1] || t[2] > t[3]) return false;
                    var lhs = t[0] * t[3];
                    var rhs = t[2] * t[1];
                    var expected = lhs < rhs ? 0 : lhs == rhs ? 1 : 2;
                    return t[4] == expected;
                });
        }
    }

    /// <summary>Wires evidence pooling <c>(n₁, d₁) ⊕ (n₂, d₂) = (n₁+n₂, d₁+d₂)</c>
    /// (for example <c>3/5 ⊕ 2/5 = 5/10</c>) as two linear balances so total
    /// positive evidence AND total partition/sample support stay connected
    /// without ever reducing <c>5/10</c> to <c>1/2</c>.</summary>
    public static (Wiring NumeratorSum, Wiring DenominatorSum) WirePool(
        Supports n1, Supports d1, Supports n2, Supports d2, Supports nOut, Supports dOut, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var numLaw = Operations.Balance([n1.Field.Shape, n2.Field.Shape, nOut.Field.Shape], 1, 1, -1);
        var denLaw = Operations.Balance([d1.Field.Shape, d2.Field.Shape, dOut.Field.Shape], 1, 1, -1);
        var numW = new Wiring(numLaw, n1.Variable, n2.Variable, nOut.Variable);
        var denW = new Wiring(denLaw, d1.Variable, d2.Variable, dOut.Variable);
        foreach (var pos in positions.Indexes())
        {
            numW.Add(pos, pos, pos);
            denW.Add(pos, pos, pos);
        }
        return (numW, denW);
    }
}
