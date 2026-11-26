namespace Gojo.EncodingC;

/// <summary>Finding the equation a table or a handful of witnesses keep
/// (ops_claude/GojoOperationsProposal-2026-11.md, §3). It is
/// <see cref="Operations.Bind"/> read backward: from tuples to a polynomial
/// relation, so a law learned as a table can be compressed into a few integers
/// and a binding, and the exceptions it does not explain kept as residue.
/// Everything it returns is numbers: weights and exponents, never a kind.
/// <para><b>Exact first.</b> On a full table there is no noise to fight.
/// <see cref="Relations"/> finds every polynomial relation of the lowest degree
/// the tuples satisfy: the nullspace of the tuples evaluated on monomials,
/// degree by degree, as the vanishing ideal of a finite set of points is found
/// (Möller and Buchberger, 1982). A relation found this way has no direction:
/// it reads every way, as the structure does.</para>
/// <para><b>Sparse when witnesses are few.</b> <see cref="Fits"/> writes one
/// role as a short integer combination of monomials of the others, the
/// simplest first, as sparse regression of a step map does (SINDy, Brunton,
/// Proctor and Kutz, 2016), allowing a stated number of exceptions. Few
/// witnesses leave several candidates; <see cref="Probe"/> chooses the input
/// whose answer splits them most, so a structure can ask the question that
/// teaches it most (from ops_sol's discovery proposal).</para></summary>
public static class Recognition
{
    /// <summary>One way to write the target: the equation, and the witnesses it
    /// does not explain, by index.</summary>
    public sealed record Fit(Polynomial Equation, IReadOnlyList<int> Exceptions);

    /// <summary>A table set beside an equation run through the same binding:
    /// whether the tables are the same, how many before-states differ, and the
    /// bits of the table against the bits of the equation with its exceptions.</summary>
    public sealed record Comparison(bool Same, int Differences, long TableBits, long EquationBits);

    /// <summary>The distinct tuples a functional law witnesses, read through a
    /// binding as <see cref="Operations.Bind"/> reads it: relation role r is law
    /// role <paramref name="reads"/>[r] before the step, or law role
    /// <paramref name="writes"/>[r] after it. A relation is a set, so a tuple
    /// two before-states share is witnessed once.</summary>
    public static IReadOnlyList<int[]> Witnesses(TransitionLaw law, int[] reads, int[] writes)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(writes);
        if (reads.Length != writes.Length || Enumerable.Range(0, reads.Length).Any(r => (reads[r] < 0) == (writes[r] < 0)))
            throw new ArgumentException("Each relation role reads a law role before the step or writes one after it.", nameof(writes));
        var tuples = new List<int[]>();
        for (var before = 0; before < law.StateCount; before++)
        {
            var afters = law.Afters(before);
            if (afters.Count != 1) continue;
            int[] b = law.Tuple(before), a = law.Tuple(afters[0]);
            tuples.Add([.. Enumerable.Range(0, reads.Length).Select(r => reads[r] >= 0 ? b[reads[r]] : a[writes[r]])]);
        }
        return [.. tuples.Distinct(TupleEquality.Instance)];
    }

    /// <summary>Every polynomial relation of the lowest degree, up to
    /// <paramref name="maxDegree"/>, that all the tuples satisfy, as a basis.
    /// Empty when none exists that low. A role with two values is never squared,
    /// since its square is itself.</summary>
    public static IReadOnlyList<Polynomial> Relations(IReadOnlyList<Shape> roles, IEnumerable<IReadOnlyList<int>> tuples, int maxDegree = 2)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(tuples);
        var points = tuples.Select(t => t.ToArray()).ToArray();
        for (var degree = 1; degree <= maxDegree; degree++)
        {
            var monomials = Monomials(roles, Enumerable.Range(0, roles.Count).ToArray(), degree, constant: true);
            var rows = points.Select(p => monomials.Select(m => Evaluate(m, p)).ToArray()).ToArray();
            var basis = Nullspace(rows, monomials.Length);
            if (basis.Count > 0)
                return [.. basis.Select(v => new Polynomial(roles, [.. v.Select((w, k) => ((int)w, monomials[k])).Where(t => t.Item1 != 0)]))];
        }
        return [];
    }

    /// <summary>Every way to write role <paramref name="target"/> as at most
    /// <paramref name="maxTerms"/> monomials of the other roles (a constant
    /// included) of degree at most <paramref name="maxDegree"/>, with integer
    /// weights within ±<paramref name="maxWeight"/>, that misses at most
    /// <paramref name="maxExceptions"/> witnesses. Fewest exceptions first, then
    /// fewest terms, lowest degree and smallest weights.</summary>
    public static IReadOnlyList<Fit> Fits(IReadOnlyList<Shape> roles, int target, IEnumerable<IReadOnlyList<int>> witnesses,
        int maxTerms = 2, int maxDegree = 2, int maxWeight = 3, int maxExceptions = 0)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(witnesses);
        if ((uint)target >= (uint)roles.Count) throw new ArgumentOutOfRangeException(nameof(target));
        var points = witnesses.Select(t => t.ToArray()).ToArray();
        var inputs = Enumerable.Range(0, roles.Count).Where(r => r != target).ToArray();
        var monomials = Monomials(roles, inputs, maxDegree, constant: true);
        var values = points.Select(p => monomials.Select(m => Evaluate(m, p)).ToArray()).ToArray();
        var targetExponents = Enumerable.Range(0, roles.Count).Select(r => r == target ? 1 : 0).ToArray();
        var fits = new List<(Fit Fit, int Terms, int Degree, int Weight)>();
        var chosen = new List<int>();
        var weights = new List<int>();

        void Choose(int from)
        {
            if (chosen.Count > 0) Weigh(0);
            if (chosen.Count == maxTerms) return;
            for (var k = from; k < monomials.Length; k++)
            {
                chosen.Add(k);
                Choose(k + 1);
                chosen.RemoveAt(chosen.Count - 1);
            }
        }

        void Weigh(int index)
        {
            if (index == chosen.Count)
            {
                var exceptions = new List<int>();
                for (var p = 0; p < points.Length && exceptions.Count <= maxExceptions; p++)
                {
                    long predicted = 0;
                    for (var j = 0; j < chosen.Count; j++) predicted += weights[j] * values[p][chosen[j]];
                    if (predicted != points[p][target]) exceptions.Add(p);
                }
                if (exceptions.Count > maxExceptions) return;
                (int, int[])[] terms = [(1, targetExponents), .. chosen.Select((k, j) => (-weights[j], monomials[k]))];
                fits.Add((new Fit(new Polynomial(roles, terms), exceptions), chosen.Count,
                    chosen.Max(k => monomials[k].Sum()), weights.Sum(w => w < 0 ? -w : w)));
                return;
            }
            for (var w = -maxWeight; w <= maxWeight; w++)
            {
                if (w == 0) continue;
                weights.Add(w);
                Weigh(index + 1);
                weights.RemoveAt(weights.Count - 1);
            }
        }

        Choose(0);
        return [.. fits.OrderBy(f => f.Fit.Exceptions.Count).ThenBy(f => f.Terms).ThenBy(f => f.Degree).ThenBy(f => f.Weight).Select(f => f.Fit)];
    }

    /// <summary>What a candidate says the target is for the other roles'
    /// values: null where it gives no whole value inside the target's shape.</summary>
    public static int? Predict(Polynomial equation, int target, IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(equation);
        ArgumentNullException.ThrowIfNull(values);
        long rest = 0, slope = 0;
        foreach (var (weight, exponents) in equation.Terms)
        {
            if (exponents[target] > 1 || (exponents[target] == 1 && exponents.Where((e, r) => r != target && e > 0).Any())) return null;
            if (exponents[target] == 1) { slope += weight; continue; }
            long term = weight;
            for (var r = 0; r < exponents.Count; r++)
                for (var e = 0; e < exponents[r]; e++) term *= values[r];
            rest += term;
        }
        if (slope == 0 || rest % slope != 0) return null;
        var value = -rest / slope;
        return value < 0 || value >= equation.Roles[target].Size ? null : (int)value;
    }

    /// <summary>The question that teaches most: values for every role but the
    /// target where the candidates' answers split them most evenly (the
    /// largest group that agrees is smallest). Null when they agree on every
    /// input, so no question can tell them apart. The target's slot is −1.</summary>
    public static int[]? Probe(IReadOnlyList<Polynomial> candidates, int target)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count < 2) return null;
        var roles = candidates[0].Roles;
        var inputs = Enumerable.Range(0, roles.Count).Where(r => r != target).ToArray();
        var count = inputs.Aggregate(1L, (n, r) => n * roles[r].Size);
        if (count > 1 << 20) throw new ArgumentException("The input domain is too large to ask about exhaustively.", nameof(candidates));
        int[]? best = null;
        var bestLargest = candidates.Count;
        for (long index = 0; index < count; index++)
        {
            var values = new int[roles.Count];
            values[target] = -1;
            var rest = index;
            foreach (var r in inputs) { values[r] = (int)(rest % roles[r].Size); rest /= roles[r].Size; }
            var largest = candidates.GroupBy(c => Predict(c, target, values) ?? -1).Max(g => g.Count());
            if (largest < bestLargest) { bestLargest = largest; best = values; }
        }
        return best;
    }

    /// <summary>A law set beside an equation bound the same way: the same
    /// table or not, the before-states that differ, and description lengths.
    /// The equation's bits count each exception as a before-state and an
    /// after-state written out.</summary>
    public static Comparison Compare(TransitionLaw law, Polynomial equation, int[] reads, int[] writes, int[]? cost = null)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(equation);
        var bound = Operations.Bind(equation.Relation, law.Roles, reads, writes, cost: cost);
        var differences = Enumerable.Range(0, law.StateCount).Count(b => !law.Afters(b).SequenceEqual(bound.Afters(b)));
        var stateBits = 0;
        while (1L << stateBits < law.StateCount) stateBits++;
        return new Comparison(law.SameTable(bound), differences, law.BitCount, equation.Bits + 2L * stateBits * differences);
    }

    /// <summary>Exponent vectors over the given roles, total degree at most
    /// <paramref name="degree"/>, optionally with the constant; a two-valued
    /// role is never raised past 1.</summary>
    private static int[][] Monomials(IReadOnlyList<Shape> roles, int[] over, int degree, bool constant)
    {
        var found = new List<int[]>();
        var exponents = new int[roles.Count];
        void Extend(int i, int left)
        {
            if (i == over.Length)
            {
                if (constant || exponents.Any(e => e > 0)) found.Add((int[])exponents.Clone());
                return;
            }
            var limit = roles[over[i]].Size <= 2 ? 1 : left;
            for (var e = 0; e <= limit && e <= left; e++)
            {
                exponents[over[i]] = e;
                Extend(i + 1, left - e);
            }
            exponents[over[i]] = 0;
        }
        Extend(0, degree);
        return [.. found];
    }

    private static long Evaluate(int[] exponents, int[] values)
    {
        long product = 1;
        for (var r = 0; r < exponents.Length; r++)
            for (var e = 0; e < exponents[r]; e++) product *= values[r];
        return product;
    }

    /// <summary>A basis of the integer vectors x with rows · x = 0, each
    /// reduced by its common factor, by fraction-free Gauss–Jordan elimination.</summary>
    private static List<long[]> Nullspace(long[][] rows, int columns)
    {
        var m = rows.Select(r => (long[])r.Clone()).ToList();
        var pivots = new List<(int Row, int Column)>();
        var row = 0;
        for (var c = 0; c < columns && row < m.Count; c++)
        {
            var p = row;
            while (p < m.Count && m[p][c] == 0) p++;
            if (p == m.Count) continue;
            (m[row], m[p]) = (m[p], m[row]);
            for (var i = 0; i < m.Count; i++)
            {
                if (i == row || m[i][c] == 0) continue;
                long f = m[i][c], g = m[row][c];
                for (var k = 0; k < columns; k++) m[i][k] = checked(m[i][k] * g - m[row][k] * f);
                Reduce(m[i]);
            }
            pivots.Add((row, c));
            row++;
        }
        var pivotColumns = pivots.Select(p => p.Column).ToHashSet();
        var common = pivots.Aggregate(1L, (l, p) => Lcm(l, Magnitude(m[p.Row][p.Column])));
        var basis = new List<long[]>();
        for (var free = 0; free < columns; free++)
        {
            if (pivotColumns.Contains(free)) continue;
            var v = new long[columns];
            v[free] = common;
            foreach (var (r, c) in pivots) v[c] = checked(-m[r][free] * common / m[r][c]);
            Reduce(v);
            basis.Add(v);
        }
        return basis;
    }

    private static void Reduce(long[] v)
    {
        var g = v.Aggregate(0L, (a, x) => Gcd(a, Magnitude(x)));
        if (g > 1) for (var k = 0; k < v.Length; k++) v[k] /= g;
    }

    private static long Magnitude(long x) => x < 0 ? -x : x;

    private sealed class TupleEquality : IEqualityComparer<int[]>
    {
        public static TupleEquality Instance { get; } = new();

        public bool Equals(int[]? x, int[]? y) => x is not null && y is not null && x.SequenceEqual(y);

        public int GetHashCode(int[] tuple) => tuple.Aggregate(17, (h, v) => unchecked(h * 31 + v));
    }

    private static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a % b);

    private static long Lcm(long a, long b) => a / Gcd(a, b) * b;
}
