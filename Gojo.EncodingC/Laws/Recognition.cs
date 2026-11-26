namespace Gojo.EncodingC;

/// <summary>Finding the equation a table or a handful of witnesses keep
/// (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §3.5). It is
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
    /// bits of the table against the bits of the equation with its exceptions,
    /// and whether the equation's table keeps the law's own accounting. Matching
    /// bits is not the whole law: a move rebuilt without its preserved sum
    /// cannot take through a port the original could (from ops_sol's review).</summary>
    public sealed record Comparison(bool Same, int Differences, long TableBits, long EquationBits, bool KeepsAccounting);

    /// <summary>A system of equations found from tuples, and how exactly it
    /// describes them: the tuples of the roles' domain the system admits that
    /// were never witnessed. Empty is exact. A system that admits more is
    /// weaker than the data, and those tuples say where (from ops_sol's review:
    /// the lowest-degree equation alone can admit a forbidden tuple). Checked
    /// is false where the domain was too large to enumerate.</summary>
    public sealed record Found(IReadOnlyList<Polynomial> System, IReadOnlyList<int[]> Unexplained, bool Checked);

    /// <summary>An equation with a residue role it found: the law's observed
    /// roles, then one free role that the binding keeps as small as it can.</summary>
    public sealed record WithResidue(Polynomial Equation, int[] Reads, int[] Writes, int[] Cost);

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

    /// <summary>The tuples a learned table has admitted so far, read through a
    /// binding: each before-state with exactly one admitted after-state gives
    /// one. The table need not be complete: what is still unknown is simply
    /// not a witness yet, so fitting can start from the first few runs. States
    /// are decoded directly, so nothing the size of the table is built.</summary>
    public static IReadOnlyList<int[]> Witnesses(LearnedTable table, int[] reads, int[] writes)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(writes);
        var tuples = new List<int[]>();
        for (var before = 0; before < table.StateCount; before++)
        {
            var admitted = table.Predict(before).Admitted;
            if (admitted.Count != 1) continue;
            int[] b = Decode(table.Roles, before), a = Decode(table.Roles, admitted[0]);
            tuples.Add([.. Enumerable.Range(0, reads.Length).Select(r => reads[r] >= 0 ? b[reads[r]] : a[writes[r]])]);
        }
        return [.. tuples.Distinct(TupleEquality.Instance)];
    }

    /// <summary>A system of polynomial relations, degree by degree up to
    /// <paramref name="maxDegree"/>, that all the tuples satisfy. It starts from
    /// the lowest degree where any relation exists, and adds a higher-degree one
    /// only where it rules out a tuple the system still admits but nobody
    /// witnessed, so the system ends as tight as the degrees allow and says
    /// what it still fails to exclude. Over a domain too large to enumerate the
    /// check is skipped and the lowest-degree basis returned. A role with two
    /// values is never squared, since its square is itself.</summary>
    public static Found Relations(IReadOnlyList<Shape> roles, IEnumerable<IReadOnlyList<int>> tuples, int maxDegree = 2)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(tuples);
        var points = tuples.Select(t => t.ToArray()).Distinct(TupleEquality.Instance).ToArray();
        var domain = roles.Aggregate(1L, (n, r) => n * r.Size);
        var check = domain <= 1 << 20;
        var witnessed = points.ToHashSet(TupleEquality.Instance);
        var extra = check ? Enumerable.Range(0, (int)domain).Select(i => Decode(roles, i)).Where(t => !witnessed.Contains(t)).ToList() : [];
        var system = new List<Polynomial>();
        var lowest = 0;
        for (var degree = 1; degree <= maxDegree; degree++)
        {
            var monomials = Monomials(roles, Enumerable.Range(0, roles.Count).ToArray(), degree, constant: true);
            var rows = points.Select(p => monomials.Select(m => Evaluate(m, p)).ToArray()).ToArray();
            foreach (var v in Nullspace(rows, monomials.Length))
            {
                var equation = new Polynomial(roles, [.. v.Select((w, k) => (w, monomials[k])).Where(t => t.w != 0)]);
                var keep = check ? system.Count == 0 && lowest == 0 || extra.Any(t => !equation.Holds(t)) : lowest == 0 || lowest == degree;
                if (!keep) continue;
                system.Add(equation);
                extra.RemoveAll(t => !equation.Holds(t));
            }
            if (system.Count > 0 && lowest == 0) lowest = degree;
            if (system.Count > 0 && (!check || extra.Count == 0)) break;
        }
        return new Found(system, extra, check);
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

    /// <summary>The candidates that agree with the witnesses except at no more
    /// than <paramref name="maxExceptions"/> of them: a wall where a law held,
    /// or an observation nobody can explain yet, is counted, not fatal. A
    /// candidate that cannot complete a witness disagrees with it.</summary>
    public static IReadOnlyList<Polynomial> Survivors(IReadOnlyList<Polynomial> candidates, int target, IEnumerable<IReadOnlyList<int>> witnesses, int maxExceptions = 0)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(witnesses);
        var points = witnesses.ToArray();
        return [.. candidates.Where(c => points.Count(w => Predict(c, target, w) != w[target]) <= maxExceptions)];
    }

    /// <summary>A balance over the law's observed roles plus one hidden residue
    /// role that the binding keeps as small as it can, for a law no balance of
    /// its observed roles reproduces (adapted from ops_neon's residue
    /// discovery). Shortfall is the gap with its excess restored; accumulate is
    /// a sum with its overflow restored. Weights run within
    /// ±<paramref name="maxWeight"/> and the residue's capacity up to
    /// <paramref name="maxCapacity"/>; each candidate is checked cheaply on the
    /// witnesses, then exactly, by rebuilding the law. Null when none is found.</summary>
    public static WithResidue? Residue(TransitionLaw law, int[] reads, int[] writes, int maxWeight = 2, int maxCapacity = 8)
    {
        ArgumentNullException.ThrowIfNull(law);
        var witnesses = Witnesses(law, reads, writes);
        var observed = reads.Length;
        int[] allReads = [.. reads, -1], allWrites = [.. writes, -1], cost = [.. Enumerable.Repeat(0, observed), 1];
        var weights = new int[observed];
        WithResidue? found = null;

        bool Search(int r)
        {
            if (r == observed)
            {
                for (var wf = 1; wf <= maxWeight; wf++)
                    for (var capacity = 1; capacity <= maxCapacity; capacity++)
                        foreach (var sign in new[] { -1, 1 })
                        {
                            var fits = witnesses.All(t =>
                            {
                                var rest = Enumerable.Range(0, observed).Sum(k => (long)weights[k] * t[k]);
                                var f = -sign * rest;
                                return f % wf == 0 && f / wf >= 0 && f / wf <= capacity;
                            });
                            if (!fits) continue;
                            Shape[] roles = [.. Enumerable.Range(0, observed).Select(k => law.Roles[reads[k] >= 0 ? reads[k] : writes[k]]), Shape.Unary(capacity)];
                            var equation = Polynomial.Balance(roles, [.. weights, sign * wf]);
                            if (!Compare(law, equation, allReads, allWrites, cost).Same) continue;
                            found = new WithResidue(equation, allReads, allWrites, cost);
                            return true;
                        }
                return false;
            }
            for (var w = 1; w <= maxWeight; w++)
                foreach (var sign in new[] { 1, -1 })
                {
                    weights[r] = sign * w;
                    if (Search(r + 1)) return true;
                }
            return false;
        }

        Search(0);
        return found;
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
        return new Comparison(law.SameTable(bound), differences, law.BitCount, equation.Bits + 2L * stateBits * differences, Keeps(bound, law.Accounting));
    }

    /// <summary>The law an equation compresses, rebuilt from the equation with
    /// the original law's accounting attached, so the rebuilt law makes the
    /// same promises: a move stays a move at a port. It must also behave as the
    /// original: an equation whose table differs is refused even where it
    /// keeps the accounting (from ops_sol's review), and so is one whose table
    /// breaks the accounting. A fit with exceptions is not a restoration; the
    /// original table stays the law.</summary>
    public static TransitionLaw Restore(TransitionLaw law, Polynomial equation, int[] reads, int[] writes, int[]? cost = null)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(equation);
        var bound = Operations.Bind(equation.Relation, law.Roles, reads, writes, cost: cost);
        if (!law.SameTable(bound))
            throw new ArgumentException("The equation does not reproduce the law: a restored law must behave as the original, not only keep its promises.", nameof(equation));
        return new TransitionLaw(bound.Roles, bound.Bytes(), law.Accounting);
    }

    private static bool Keeps(TransitionLaw table, Accounting accounting)
    {
        try
        {
            _ = new TransitionLaw(table.Roles, table.Bytes(), accounting);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>The role values of a state index, the first role varying fastest.</summary>
    private static int[] Decode(IReadOnlyList<Shape> roles, long index)
    {
        var values = new int[roles.Count];
        for (var r = 0; r < roles.Count; r++)
        {
            values[r] = (int)(index % roles[r].Size);
            index /= roles[r].Size;
        }
        return values;
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
    /// reduced by its common factor, by fraction-free Gauss–Jordan elimination
    /// in unbounded integers, so a large or high-degree table cannot overflow.</summary>
    private static List<int[]> Nullspace(long[][] rows, int columns)
    {
        var m = rows.Select(r => r.Select(x => (System.Numerics.BigInteger)x).ToArray()).ToList();
        var pivots = new List<(int Row, int Column)>();
        var row = 0;
        for (var c = 0; c < columns && row < m.Count; c++)
        {
            var p = row;
            while (p < m.Count && m[p][c].IsZero) p++;
            if (p == m.Count) continue;
            (m[row], m[p]) = (m[p], m[row]);
            for (var i = 0; i < m.Count; i++)
            {
                if (i == row || m[i][c].IsZero) continue;
                System.Numerics.BigInteger f = m[i][c], g = m[row][c];
                for (var k = 0; k < columns; k++) m[i][k] = m[i][k] * g - m[row][k] * f;
                Reduce(m[i]);
            }
            pivots.Add((row, c));
            row++;
        }
        var pivotColumns = pivots.Select(p => p.Column).ToHashSet();
        var common = pivots.Aggregate(System.Numerics.BigInteger.One, (l, p) =>
        {
            var x = System.Numerics.BigInteger.Abs(m[p.Row][p.Column]);
            return l / System.Numerics.BigInteger.GreatestCommonDivisor(l, x) * x;
        });
        var basis = new List<int[]>();
        for (var free = 0; free < columns; free++)
        {
            if (pivotColumns.Contains(free)) continue;
            var v = new System.Numerics.BigInteger[columns];
            v[free] = common;
            foreach (var (r, c) in pivots) v[c] = -m[r][free] * common / m[r][c];
            Reduce(v);
            basis.Add([.. v.Select(x => checked((int)x))]);
        }
        return basis;
    }

    private static void Reduce(System.Numerics.BigInteger[] v)
    {
        var g = v.Aggregate(System.Numerics.BigInteger.Zero, System.Numerics.BigInteger.GreatestCommonDivisor);
        if (g > 1) for (var k = 0; k < v.Length; k++) v[k] /= g;
    }

    private sealed class TupleEquality : IEqualityComparer<int[]>
    {
        public static TupleEquality Instance { get; } = new();

        public bool Equals(int[]? x, int[]? y) => x is not null && y is not null && x.SequenceEqual(y);

        public int GetHashCode(int[] tuple) => tuple.Aggregate(17, (h, v) => unchecked(h * 31 + v));
    }

    private static long Gcd(long a, long b) => b == 0 ? a : Gcd(b, a % b);

    private static long Lcm(long a, long b) => a / Gcd(a, b) * b;
}
