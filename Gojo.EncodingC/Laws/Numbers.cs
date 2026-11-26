namespace Gojo.EncodingC;

/// <summary>Numbers as rungs: a minimal place-value layer over pre-numeric
/// counts (ops_claude/GojoOperationsProposal-2026-11.md, §6, and
/// GojoOperationsRound15-2026-11.md). It adds range and erases nothing: a count
/// stays a count, readable and movable as it was.
/// <para><b>A rung is a partition.</b> A digit is a count with as many values as
/// its rung's radix, held unary (larger is more planes) or binary-coded (the
/// shape's size still declares exactly which values exist). Each rung has its
/// own radix: fifths under whole stars, seconds under minutes. A number is a
/// ladder of rungs, finest first, each worth the product of the radices below
/// it. The top rung's radix is the whole's partition count plus one, so a
/// rating out of five is a top rung of radix 6 holding 0..5.</para>
/// <para><b>Rungs are joined by balances with the radix as their weight.</b> In
/// arithmetic, a + b + carry in = radix · carry out + sum; in multiplication by
/// a digit, a · k + carry in = radix · carry out + digit. Both are relations,
/// so a ladder of them narrows in any direction and leaves what it cannot
/// settle open. In material, a unit is admitted into the finest rung and
/// carried atomically, and grouping and refining move whole packets, all
/// conserving the place-value sum.</para>
/// <para><b>Resolution is structure.</b> Below a rung that was read, the finer
/// rungs say how much is known: restricted to zero for a count, which is exact,
/// or left open for a measurement read only to that partition, which is not.
/// Operations carry that openness through their relations, so adding two
/// readings known to the fifth gives a sum whose fifths may carry: nothing is
/// rounded away. What a rung cannot hold is residue: the last carry, or a
/// residue role in a rescaling.</para></summary>
public static class Numbers
{
    /// <summary>A digit at a radix: a count with radix values, unary or binary-coded.</summary>
    public static Shape Digit(int radix, bool binary = false) =>
        radix < 2 ? throw new ArgumentOutOfRangeException(nameof(radix), "A radix of at least 2 groups anything.")
            : binary ? Shape.Binary(radix) : Shape.Unary(radix - 1);

    /// <summary>A carry between rungs in a sum: a unary count of capacity 1.</summary>
    public static Shape Carry { get; } = Shape.Unary(1);

    /// <summary>A sum column at one rung: a + b + carry in = radix · carry out +
    /// sum, for any shapes of a and b and a sum digit of the radix. Roles: a, b,
    /// carry in, carry out, sum.</summary>
    public static Polynomial Column(Shape a, Shape b, int radix, bool binary = false) =>
        Polynomial.Balance([a, b, Carry, Carry, Digit(radix, binary)], 1, 1, 1, -radix, -1);

    /// <summary>a + b + carry in = radix · carry out + sum over digits of one radix.</summary>
    public static Polynomial DigitSum(int radix, bool binary = false) =>
        Column(Digit(radix, binary), Digit(radix, binary), radix, binary);

    /// <summary>A digit times a digit with a carry: a · k + carry in = radix ·
    /// carry out + digit, carries running 0..radix − 1. Roles: a, k, carry in,
    /// carry out, digit.</summary>
    public static Polynomial DigitScale(int radix, bool binary = false)
    {
        var d = Digit(radix, binary);
        return new Polynomial([d, d, d, d, d], (1, [1, 1, 0, 0, 0]), (1, [0, 0, 1, 0, 0]), (-radix, [0, 0, 0, 1, 0]), (-1, [0, 0, 0, 0, 1]));
    }

    /// <summary>Two shares are the same where numerator · other denominator
    /// equals other numerator · denominator: with the denominators fixed, a
    /// balance whose weights are the denominators. Roles: a count out of
    /// <paramref name="denominator"/>, a count out of <paramref name="otherDenominator"/>.</summary>
    public static Polynomial SameShare(int denominator, int otherDenominator) =>
        Polynomial.Balance([Shape.Unary(denominator), Shape.Unary(otherDenominator)], otherDenominator, -denominator);

    /// <summary>A count out of <paramref name="from"/> read out of
    /// <paramref name="to"/>: to · a = from · b + residue, the residue in units
    /// of 1/(from · to) and below from. Exact where the partitions divide; where
    /// they do not, the part that does not fit is kept as the residue role, not
    /// rounded away. Roles: a, b, residue.</summary>
    public static Polynomial Rescale(int from, int to) =>
        from < 2 || to < 1 ? throw new ArgumentOutOfRangeException(nameof(from))
            : Polynomial.Balance([Shape.Unary(from), Shape.Unary(to), Shape.Unary(from - 1)], to, -from, -1);

    /// <summary>Radix units of a finer rung become one of the next: a
    /// conserving move of a whole packet. Roles: fine, coarse.</summary>
    public static TransitionLaw Group(int radix, int fineCapacity, int coarseCapacity) => Flows.Convert(radix, 1, fineCapacity, coarseCapacity);

    /// <summary>One unit of a coarser rung becomes radix of the next finer
    /// one. Roles: coarse, fine.</summary>
    public static TransitionLaw Refine(int radix, int coarseCapacity, int fineCapacity) => Flows.Convert(1, radix, coarseCapacity, fineCapacity);

    private static readonly Dictionary<(int, int, int, bool), TransitionLaw> Admits = [];

    /// <summary>One unit of pending pressure admitted into a two-rung number,
    /// atomically (from ops_sol): the fine digit takes it, or, when the fine
    /// digit is full, the coarse digit takes one and the fine digit empties.
    /// A stored digit never leaves its partition, no staging room is needed,
    /// and at the top the unit waits as pressure. pressure + radix · coarse +
    /// fine is preserved. Roles: pressure, coarse, fine.</summary>
    public static TransitionLaw Admit(int radix, int pressureCapacity, int coarseCapacity, bool binary = false)
    {
        lock (Admits)
        {
            var key = (radix, pressureCapacity, coarseCapacity, binary);
            if (Admits.TryGetValue(key, out var cached)) return cached;
            return Admits[key] = TransitionLaw.FromFunction(
                [Shape.Unary(pressureCapacity), binary ? Shape.Binary(coarseCapacity + 1) : Shape.Unary(coarseCapacity), Digit(radix, binary)],
                s => s[0] == 0 || (s[1] == coarseCapacity && s[2] == radix - 1) ? s
                    : s[2] == radix - 1 ? [s[0] - 1, s[1] + 1, 0] : [s[0] - 1, s[1], s[2] + 1],
                new Accounting(true, new PreservedSum([0, 1, 2], [1, radix, 1]), new NeverIncreases(0)));
        }
    }
}

/// <summary>A number held as rungs in a store: one digit field per rung,
/// finest first, each with its own radix, and the digits each rung still
/// allows. Values cross in and out only at the edge, as observations a host
/// restricts or reads; between rungs everything is relations narrowing
/// supports. Narrowing is monotone within one observation: when the source
/// changes, <see cref="Reopen"/> opens the rungs before the next one (from
/// ops_sol's review).</summary>
public sealed class Ladder
{
    private readonly Supports[] possible;
    private readonly int[] radices;

    /// <summary>A ladder of fresh digits, one per radix, every digit open.</summary>
    public Ladder(Store store, IReadOnlyList<int> radices, bool binary = false)
        : this(store, radices, [.. radices.Select(r => new Field(Numbers.Digit(r, binary), [.. Enumerable.Range(0, Numbers.Digit(r, binary).Bits).Select(_ => store.AddPlane())]))])
    {
    }

    /// <summary>A ladder of <paramref name="rungs"/> fresh digits of one radix.</summary>
    public Ladder(Store store, int radix, int rungs, bool binary = false)
        : this(store, [.. Enumerable.Repeat(radix, rungs)], binary)
    {
    }

    /// <summary>A ladder read over digit fields that already exist, such as the
    /// first planes of material counts: the digits are views of the counts, and
    /// their supports start open.</summary>
    public Ladder(Store store, IReadOnlyList<int> radices, IReadOnlyList<Field> digits)
        : this(store, radices, digits, null)
    {
    }

    /// <summary>A ladder over digit fields that already exist, of one radix.</summary>
    public Ladder(Store store, int radix, IReadOnlyList<Field> digits)
        : this(store, [.. Enumerable.Repeat(radix, digits.Count)], digits, null)
    {
    }

    private Ladder(Store store, IReadOnlyList<int> radices, IReadOnlyList<Field> digits, IReadOnlyList<Supports>? supports)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(radices);
        ArgumentNullException.ThrowIfNull(digits);
        if (digits.Count == 0 || digits.Count != radices.Count) throw new ArgumentException("One radix per rung, and at least one rung.", nameof(digits));
        for (var r = 0; r < digits.Count; r++)
            if (radices[r] < 2 || digits[r].Shape.Size != radices[r])
                throw new ArgumentException($"Rung {r} holds {digits[r].Shape.Size} values but its radix is {radices[r]}.", nameof(digits));
        Store = store;
        this.radices = [.. radices];
        Digits = Array.AsReadOnly(digits.ToArray());
        possible = supports is null ? [.. digits.Select(d => Supports.On(store, d))] : [.. supports];
    }

    public Store Store { get; }

    /// <summary>Each rung's radix, finest first.</summary>
    public IReadOnlyList<int> Radices => Array.AsReadOnly(radices);

    /// <summary>The radix when every rung shares one; 0 when they differ.</summary>
    public int Radix => radices.All(r => r == radices[0]) ? radices[0] : 0;

    public IReadOnlyList<Field> Digits { get; }

    /// <summary>The digits each rung still allows.</summary>
    public IReadOnlyList<Supports> Possible => Array.AsReadOnly(possible);

    /// <summary>What one unit of a rung is worth in units of the finest rung.</summary>
    public long PlaceValue(int rung) => radices.Take(rung).Aggregate(1L, (p, r) => p * r);

    /// <summary>The largest value the rungs hold, in units of the finest rung.</summary>
    public long Capacity => PlaceValue(radices.Length) - 1;

    /// <summary>Narrows every rung to the digits of a value, in units of the
    /// finest rung: an exact observation, a count.</summary>
    public void Restrict(int position, long value) => Measure(position, value, 0);

    /// <summary>Narrows one rung to some digits, leaving the others as they are.</summary>
    public void Restrict(int position, int rung, ulong digits) => possible[rung].Restrict(position, digits);

    /// <summary>A reading known only to <paramref name="rung"/>: the value, in
    /// units of that rung, settles it and the rungs above; the finer rungs stay
    /// open, so the reading means at least the value and less than one more
    /// unit of that rung. A measurement to the fifth is not a count of fifths.</summary>
    public void Measure(int position, long value, int rung)
    {
        if ((uint)rung >= (uint)radices.Length) throw new ArgumentOutOfRangeException(nameof(rung));
        if (value < 0 || value * PlaceValue(rung) > Capacity) throw new ArgumentOutOfRangeException(nameof(value));
        for (var r = rung; r < radices.Length; r++, value /= radices[r - 1])
            possible[r].Restrict(position, 1UL << (int)(value % radices[r]));
    }

    /// <summary>Narrows every rung to the digit its field holds.</summary>
    public void Observe(int position)
    {
        foreach (var rung in possible) rung.Observe(position);
    }

    /// <summary>Opens every rung at a position again: a new observation scope.</summary>
    public void Reopen(int position)
    {
        foreach (var rung in possible) rung.Forget(position);
    }

    /// <summary>Per rung, finest first, the digits still possible.</summary>
    public ulong[] Open(int position) => [.. possible.Select(p => p.Values(position))];

    /// <summary>The value when every rung has one digit left, else null.</summary>
    public long? Read(int position)
    {
        var (low, high) = Interval(position) ?? (-1, -2);
        return low == high ? low : null;
    }

    /// <summary>The least and greatest values the rungs' digits still allow, in
    /// units of the finest rung, or null where a rung allows nothing. A bound
    /// read from each rung alone: every value the joint relation allows lies
    /// inside it, though not every value inside it need be allowed jointly.</summary>
    public (long Low, long High)? Interval(int position)
    {
        long low = 0, high = 0;
        for (var r = 0; r < possible.Length; r++)
        {
            var digits = possible[r].Values(position);
            if (digits == 0) return null;
            low += System.Numerics.BitOperations.TrailingZeroCount(digits) * PlaceValue(r);
            high += (63 - System.Numerics.BitOperations.LeadingZeroCount(digits)) * PlaceValue(r);
        }
        return (low, high);
    }

    /// <summary>A ladder of <paramref name="rungs"/> digits at a radix, every one held at 0.</summary>
    private static Ladder Zero(Store store, int radix, int rungs, Plane positions)
    {
        var zero = new Ladder(store, radix, rungs);
        foreach (var position in positions.Indexes())
            foreach (var rung in zero.possible) rung.Restrict(position, 1UL);
        return zero;
    }

    /// <summary>The same digits read as rungs further up, with zero rungs below
    /// and above to make <paramref name="rungs"/> in all.</summary>
    private static Ladder Shifted(Ladder digits, int below, int rungs, Plane positions)
    {
        var radix = digits.radices[0];
        var low = below == 0 ? null : Zero(digits.Store, radix, below, positions);
        var high = rungs - below - digits.Digits.Count is var n and > 0 ? Zero(digits.Store, radix, n, positions) : null;
        var parts = new[] { low, digits, high }.Where(p => p is not null).Select(p => p!).ToArray();
        return new Ladder(digits.Store, [.. Enumerable.Repeat(radix, rungs)], [.. parts.SelectMany(p => p.Digits)], [.. parts.SelectMany(p => p.possible)]);
    }

    private static Supports[] Carries(Store store, Shape shape, int count, Plane positions)
    {
        var carries = Enumerable.Range(0, count).Select(_ => Supports.On(store, new Field(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]))).ToArray();
        foreach (var position in positions.Indexes()) carries[0].Restrict(position, 1UL);
        return carries;
    }

    private static Wiring Row(RelationLaw relation, Plane positions, params Binding[] roles)
    {
        var wiring = new Wiring(relation, roles);
        foreach (var position in positions.Indexes()) wiring.Add([.. Enumerable.Repeat(position, roles.Length)]);
        return wiring;
    }

    /// <summary>Wires a + b = sum rung by rung at <paramref name="positions"/>:
    /// one sum column per rung, joined by carry supports. Below the top the
    /// three share each rung's radix, so place values align. The top rungs may
    /// differ: two ratings out of five sum into a top rung out of ten, so the
    /// partitions add. The finest carry in is held at 0; the last carry out is
    /// the overflow, left open unless the caller narrows it. Returns the
    /// wirings and the carries, finest first.</summary>
    public static (IReadOnlyList<Wiring> Wirings, IReadOnlyList<Supports> Carries) Add(Ladder a, Ladder b, Ladder sum, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(sum);
        ArgumentNullException.ThrowIfNull(positions);
        if (a.Store != b.Store || a.Store != sum.Store) throw new ArgumentException("A sum is wired within one store.", nameof(sum));
        var rungs = sum.radices.Length;
        if (a.radices.Length != rungs || b.radices.Length != rungs) throw new ArgumentException("The ladders have the same number of rungs.", nameof(sum));
        for (var r = 0; r < rungs - 1; r++)
            if (a.radices[r] != sum.radices[r] || b.radices[r] != sum.radices[r])
                throw new ArgumentException($"Below the top, every rung shares a radix; rung {r} does not.", nameof(sum));
        var carries = Carries(a.Store, Numbers.Carry, rungs + 1, positions);
        var wirings = Enumerable.Range(0, rungs).Select(r => Row(
            Numbers.Column(a.Digits[r].Shape, b.Digits[r].Shape, sum.radices[r], sum.Digits[r].Shape != Numbers.Digit(sum.radices[r])).Relation, positions,
            a.possible[r].Variable, b.possible[r].Variable, carries[r].Variable, carries[r + 1].Variable, sum.possible[r].Variable)).ToArray();
        return (Array.AsReadOnly(wirings), Array.AsReadOnly(carries));
    }

    /// <summary>Wires product = a · k for one digit k, rung by rung, at one
    /// radix: a digit-times-digit column per rung, carries running 0..radix − 1,
    /// the last carry becoming the product's top rung. The product has one rung
    /// more than a.</summary>
    public static IReadOnlyList<Wiring> Scale(Ladder a, Supports k, Ladder product, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(k);
        ArgumentNullException.ThrowIfNull(product);
        var radix = a.Radix;
        if (radix == 0 || product.Radix != radix || k.Field.Shape.Size != radix || product.radices.Length != a.radices.Length + 1)
            throw new ArgumentException("Scaling is at one radix, into a product one rung longer.", nameof(product));
        var binary = a.Digits[0].Shape != Numbers.Digit(radix);
        var carries = Carries(a.Store, Numbers.Digit(radix, binary), a.radices.Length + 1, positions);
        var column = Numbers.DigitScale(radix, binary).Relation;
        var wirings = Enumerable.Range(0, a.radices.Length).Select(r =>
            Row(column, positions, a.possible[r].Variable, k.Variable, carries[r].Variable, carries[r + 1].Variable, product.possible[r].Variable)).ToList();
        var top = Polynomial.Balance([Numbers.Digit(radix, binary), Numbers.Digit(radix, binary)], 1, -1).Relation;
        wirings.Add(Row(top, positions, carries[^1].Variable, product.possible[^1].Variable));
        return wirings;
    }

    /// <summary>Wires product = a · b at one radix as a lattice: a partial
    /// product of a by each digit of b, shifted to that digit's rung, summed
    /// rung by rung. The product has as many rungs as a and b together. It is
    /// built from relations, so it also narrows backward, but a lattice keeps
    /// only what each column knows: a question the whole product could answer
    /// at once may stay open here (from ops_sol's factoring comparison).</summary>
    public static IReadOnlyList<Wiring> Multiply(Ladder a, Ladder b, Ladder product, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(product);
        var radix = a.Radix;
        int n = a.radices.Length, m = b.radices.Length;
        if (radix == 0 || b.Radix != radix || product.Radix != radix || product.radices.Length != n + m)
            throw new ArgumentException("Multiplying is at one radix, into a product as long as both factors.", nameof(product));
        var store = a.Store;
        var binary = a.Digits[0].Shape != Numbers.Digit(radix);
        var wirings = new List<Wiring>();
        if (m == 1) return Scale(a, b.possible[0], product, positions);
        Ladder? total = null;
        for (var j = 0; j < m; j++)
        {
            var partial = new Ladder(store, radix, n + 1, binary);
            wirings.AddRange(Scale(a, b.possible[j], partial, positions));
            var shifted = Shifted(partial, j, n + m, positions);
            if (total is null) { total = shifted; continue; }
            var next = j == m - 1 ? product : new Ladder(store, radix, n + m, binary);
            var (sum, carries) = Add(total, shifted, next, positions);
            foreach (var position in positions.Indexes()) carries[^1].Restrict(position, 1UL);
            wirings.AddRange(sum);
            total = next;
        }
        return wirings;
    }
}
