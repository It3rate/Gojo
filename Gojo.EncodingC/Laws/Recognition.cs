using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Exact structural hints calculated strictly from a law's table bits
/// and checked <see cref="Accounting"/> — never from names or enums.
/// <c>RoleFate[r]</c> is 0 (read-only), 1 (write-only), or 2 (read-and-write).</summary>
public sealed record LawHints(
    int[] RoleFate,
    int[] ReadRoles,
    int[] WrittenRoles,
    int[] ConservedRoles,
    bool IsFunctional,
    bool IsReversible,
    (int From, int To, int Take, int Give)[] Trades);

/// <summary>One sparse polynomial fit for a target role, with the indices of
/// witnesses it does not explain (walls, holds, or mutated corners).</summary>
public sealed record Fit(Polynomial Equation, IReadOnlyList<int> Exceptions);

/// <summary>One non-polynomial min/max fold fit (<c>IsMax == false</c> for
/// <c>min(left, right)</c>, <c>true</c> for <c>max(left, right)</c>).</summary>
public sealed record FoldFit(int LeftRole, int RightRole, int TargetRole, bool IsMax, IReadOnlyList<int> Exceptions);

/// <summary>A transition law compared beside a <see cref="Polynomial"/> bound
/// the same way: table parity, differing before-states, and bit lengths.</summary>
public sealed record Comparison(bool Same, int Differences, long TableBits, long EquationBits);

/// <summary>A bound <see cref="Polynomial"/> recovered directly from a
/// <see cref="TransitionLaw"/> (including any auto-discovered hidden free
/// residue role, optional blocked bit, numeric cost vector, explicit
/// exceptions, and MDL bit counts).</summary>
public sealed record BoundEquationFit(
    Polynomial Equation,
    int[] Reads,
    int[] Writes,
    int BlockedRole,
    int[]? Cost,
    int FreeRole,
    int FreeCapacity,
    (int Before, int After)[] Exceptions,
    long TableBits,
    long EquationBits,
    bool ExactTableMatch)
{
    public long BitsSaved => TableBits - EquationBits;
    public bool CompressesByMdl => EquationBits < TableBits;
}

/// <summary>Runs <see cref="Operations.Bind"/> backward: recovers canonical
/// <see cref="Polynomial"/> equations, hidden free-residue roles, min/max folds,
/// explicit exceptions, and minimax active probes from tables or sparse
/// witnesses. Everything returned is pure numbers — never an operation kind.
/// <para><b>Exact first.</b> <see cref="Relations"/> finds the lowest-degree
/// polynomial basis satisfied by all tuples via exact <see cref="BigInteger"/>
/// fraction-free Gauss–Jordan elimination (vanishing ideal of points). No
/// direction is imposed and arbitrary partition weights (such as 5, 10, or 60)
/// come back directly as integer coefficients.</para>
/// <para><b>Sparse and active when incomplete.</b> <see cref="Fits"/> and
/// <see cref="FitsFold"/> find short equations with explicit exceptions (walls
/// and mutated corners). <see cref="Probe"/> selects the untried input whose
/// answer splits candidates most evenly, optionally steering clear of boundary
/// walls. <see cref="Witnesses(LearnedTable, int[], int[])"/> reads sparse
/// observed pairs directly without dense <c>StateCount²</c> allocation.</para>
/// <para><b>Hidden residue discovery.</b> <see cref="DiscoverBoundBalance"/>
/// automatically discovers a missing free residue role (such as shortfall's
/// excess or a separate-output clamped sum's overflow) and selects the tightest
/// MDL binding.</para></summary>
public static class Recognition
{
    /// <summary>Extracts exact structural hints directly from table bits and
    /// checked <see cref="Accounting"/>.</summary>
    public static LawHints ReadHints(TransitionLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);
        var n = law.Roles.Count;
        var writes = new bool[n];
        var reads = new bool[n];
        var stateCount = law.StateCount;
        var afterCache = new int[stateCount][];
        for (var b = 0; b < stateCount; b++) afterCache[b] = law.Afters(b).ToArray();

        for (var b = 0; b < stateCount; b++)
        {
            var bt = law.Tuple(b);
            foreach (var a in afterCache[b])
            {
                var at = law.Tuple(a);
                for (var r = 0; r < n; r++)
                    if (at[r] != bt[r]) writes[r] = true;
            }
        }

        // Role r is read if varying r alone changes the written outputs of some state.
        var strides = new int[n];
        strides[0] = 1;
        for (var r = 1; r < n; r++) strides[r] = strides[r - 1] * law.Roles[r - 1].Size;

        for (var r = 0; r < n; r++)
        {
            var size = law.Roles[r].Size;
            var stride = strides[r];
            for (var b = 0; b < stateCount && !reads[r]; b++)
            {
                var bt = law.Tuple(b);
                if (bt[r] != 0) continue;
                var baseNext = afterCache[b];
                for (var v = 1; v < size && !reads[r]; v++)
                {
                    var other = b + v * stride;
                    var otherNext = afterCache[other];
                    if (baseNext.Length != otherNext.Length) { reads[r] = true; break; }
                    for (var i = 0; i < baseNext.Length; i++)
                    {
                        var a0 = law.Tuple(baseNext[i]);
                        var a1 = law.Tuple(otherNext[i]);
                        for (var w = 0; w < n; w++)
                            if (writes[w] && a0[w] != a1[w]) { reads[r] = true; break; }
                        if (reads[r]) break;
                    }
                }
            }
        }

        var fate = new int[n];
        var readList = new List<int>();
        var writtenList = new List<int>();
        for (var r = 0; r < n; r++)
        {
            fate[r] = writes[r] ? (reads[r] ? 2 : 1) : 0;
            if (reads[r] || !writes[r]) readList.Add(r);
            if (writes[r]) writtenList.Add(r);
        }

        var conserved = new HashSet<int>();
        var trades = new List<(int From, int To, int Take, int Give)>();
        foreach (var inv in law.Accounting.Invariants)
        {
            if (inv is PreservedSum ps)
            {
                foreach (var role in ps.Roles) conserved.Add(role);
                if (ps.Roles.Count == 2)
                {
                    int r0 = ps.Roles[0], r1 = ps.Roles[1], w0 = ps.Weights[0], w1 = ps.Weights[1];
                    if (IsDecreasing(law, r0) && IsIncreasing(law, r1)) trades.Add((r0, r1, w1, w0));
                    else if (IsDecreasing(law, r1) && IsIncreasing(law, r0)) trades.Add((r1, r0, w0, w1));
                }
            }
        }

        return new LawHints(
            fate,
            [.. readList],
            [.. writtenList],
            [.. conserved.Order()],
            law.IsFunctional,
            law.IsReversible,
            [.. trades]);
    }

    /// <summary>Distinct tuples witnessed by a functional transition law read
    /// through a binding (<c>reads[r] &gt;= 0</c> from before-state,
    /// <c>writes[r] &gt;= 0</c> from after-state).</summary>
    public static IReadOnlyList<int[]> Witnesses(TransitionLaw law, int[] reads, int[] writes)
    {
        ArgumentNullException.ThrowIfNull(law);
        ValidateBinding(reads, writes);
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

    /// <summary>Distinct tuples witnessed so far by a (possibly incomplete)
    /// <see cref="LearnedTable"/>, read directly from admitted entries without
    /// instantiating a dense <c>StateCount²</c> transition table.</summary>
    public static IReadOnlyList<int[]> Witnesses(LearnedTable table, int[] reads, int[] writes)
    {
        ArgumentNullException.ThrowIfNull(table);
        ValidateBinding(reads, writes);
        var tuples = new List<int[]>();
        for (var before = 0; before < table.StateCount; before++)
        {
            var admitted = table.Predict(before).Admitted;
            if (admitted.Count != 1) continue;
            int[] b = DecodeTuple(table.Roles, before), a = DecodeTuple(table.Roles, admitted[0]);
            tuples.Add([.. Enumerable.Range(0, reads.Length).Select(r => reads[r] >= 0 ? b[reads[r]] : a[writes[r]])]);
        }
        return [.. tuples.Distinct(TupleEquality.Instance)];
    }

    /// <summary>Every polynomial relation of lowest degree up to
    /// <paramref name="maxDegree"/> satisfied by all <paramref name="tuples"/>,
    /// as an exact reduced integer basis (vanishing ideal of points). Uses
    /// <see cref="BigInteger"/> internally so high radices (e.g. base 60) and
    /// degree-2/3 point sets never overflow 64-bit intermediates.</summary>
    public static IReadOnlyList<Polynomial> Relations(
        IReadOnlyList<Shape> roles,
        IEnumerable<IReadOnlyList<int>> tuples,
        int maxDegree = 2)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(tuples);
        var points = tuples.Select(t => t.ToArray()).ToArray();
        if (points.Length == 0) return [];
        var allRoles = Enumerable.Range(0, roles.Count).ToArray();
        for (var degree = 1; degree <= maxDegree; degree++)
        {
            var monomials = Monomials(roles, allRoles, degree, constant: true);
            var rows = points.Select(p => monomials.Select(m => EvaluateBig(m, p)).ToArray()).ToArray();
            var basis = Nullspace(rows, monomials.Length);
            if (basis.Count == 0) continue;
            var polys = new List<Polynomial>(basis.Count);
            foreach (var v in basis)
            {
                if (v.Any(c => c > int.MaxValue || c < int.MinValue)) continue;
                var terms = v.Select((w, k) => ((int)w, monomials[k])).Where(t => t.Item1 != 0).ToArray();
                if (terms.Length > 0) polys.Add(new Polynomial(roles, terms));
            }
            if (polys.Count > 0) return polys;
        }
        return [];
    }

    /// <summary>Sparse regression: writes role <paramref name="target"/> as at
    /// most <paramref name="maxTerms"/> monomials of the other roles of degree
    /// at most <paramref name="maxDegree"/> and integer weights within
    /// ±<paramref name="maxWeight"/>, allowing up to
    /// <paramref name="maxExceptions"/> exceptions (walls or wrong corners).
    /// Ordered simplest first: fewest exceptions, terms, degree, weight.</summary>
    public static IReadOnlyList<Fit> Fits(
        IReadOnlyList<Shape> roles,
        int target,
        IEnumerable<IReadOnlyList<int>> witnesses,
        int maxTerms = 2,
        int maxDegree = 2,
        int maxWeight = 3,
        int maxExceptions = 0)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(witnesses);
        if ((uint)target >= (uint)roles.Count) throw new ArgumentOutOfRangeException(nameof(target));
        var points = witnesses.Select(t => t.ToArray()).ToArray();
        var inputs = Enumerable.Range(0, roles.Count).Where(r => r != target).ToArray();
        var monomials = Monomials(roles, inputs, maxDegree, constant: true);
        var values = points.Select(p => monomials.Select(m => EvaluateLong(m, p)).ToArray()).ToArray();
        var targetExp = new int[roles.Count];
        targetExp[target] = 1;

        var fits = new List<(Fit Fit, int Terms, int Degree, int Weight)>();
        var chosen = new List<int>(maxTerms);
        var weights = new List<int>(maxTerms);

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
                (int, int[])[] terms = [(1, targetExp), .. chosen.Select((k, j) => (-weights[j], monomials[k]))];
                fits.Add((
                    new Fit(new Polynomial(roles, terms), exceptions),
                    chosen.Count,
                    chosen.Max(k => monomials[k].Sum()),
                    weights.Sum(w => w < 0 ? -w : w)));
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
        return [.. fits
            .OrderBy(f => f.Fit.Exceptions.Count)
            .ThenBy(f => f.Terms)
            .ThenBy(f => f.Degree)
            .ThenBy(f => f.Weight)
            .Select(f => f.Fit)];
    }

    /// <summary>Discovers non-polynomial <c>min(left, right)</c> or
    /// <c>max(left, right)</c> fold hypotheses for <paramref name="target"/>
    /// over pairs of input roles, allowing up to <paramref name="maxExceptions"/>
    /// exceptions.</summary>
    public static IReadOnlyList<FoldFit> FitsFold(
        IReadOnlyList<Shape> roles,
        int target,
        IEnumerable<IReadOnlyList<int>> witnesses,
        int maxExceptions = 0)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(witnesses);
        if ((uint)target >= (uint)roles.Count) throw new ArgumentOutOfRangeException(nameof(target));
        var points = witnesses.Select(t => t.ToArray()).ToArray();
        var results = new List<FoldFit>();
        for (var i = 0; i < roles.Count; i++)
        {
            if (i == target) continue;
            for (var j = i + 1; j < roles.Count; j++)
            {
                if (j == target) continue;
                foreach (var isMax in new[] { false, true })
                {
                    var exceptions = new List<int>();
                    for (var p = 0; p < points.Length && exceptions.Count <= maxExceptions; p++)
                    {
                        var expected = isMax
                            ? (points[p][i] >= points[p][j] ? points[p][i] : points[p][j])
                            : (points[p][i] <= points[p][j] ? points[p][i] : points[p][j]);
                        if (expected != points[p][target]) exceptions.Add(p);
                    }
                    if (exceptions.Count <= maxExceptions)
                        results.Add(new FoldFit(i, j, target, isMax, exceptions));
                }
            }
        }
        return [.. results.OrderBy(f => f.Exceptions.Count)];
    }

    /// <summary>Recovers a bound linear <see cref="Polynomial"/> directly from a
    /// functional <see cref="TransitionLaw"/>, automatically testing both direct
    /// 0-residue bindings (handling in-place walls like <c>Accumulate</c> and
    /// <c>LandmarkShear</c>) AND 1-free-role hidden residue bindings (recovering
    /// <c>Shortfall</c>'s excess and separate-output clamped <c>OR</c>'s
    /// overflow bit), and returning the lowest-MDL fit.</summary>
    public static BoundEquationFit? DiscoverBoundBalance(
        TransitionLaw law,
        int maxWeight = 16,
        int maxExceptions = 0)
    {
        ArgumentNullException.ThrowIfNull(law);
        if (!law.IsFunctional) return null;
        var hints = ReadHints(law);
        if (hints.WrittenRoles.Length is 0 or > 2) return null;

        // Separate an optional blocked bit role (written bit that is 1 iff all other written roles held).
        var blockedRole = -1;
        var valueWritten = hints.WrittenRoles;
        if (hints.WrittenRoles.Length == 2)
        {
            foreach (var candidate in hints.WrittenRoles)
            {
                if (law.Roles[candidate] != Shape.Bit) continue;
                var other = hints.WrittenRoles[0] == candidate ? hints.WrittenRoles[1] : hints.WrittenRoles[0];
                if (IsBlockedIndicator(law, candidate, other))
                {
                    blockedRole = candidate;
                    valueWritten = [other];
                    break;
                }
            }
        }

        if (valueWritten.Length != 1) return null;
        var wRole = valueWritten[0];

        // Build relation role layout: all read roles (before), plus written role (after).
        var readRoles = Enumerable.Range(0, law.Roles.Count)
            .Where(r => r != blockedRole && (r != wRole || hints.RoleFate[r] == 2))
            .ToArray();
        var relShapes = new Shape[readRoles.Length + 1];
        var reads = new int[relShapes.Length];
        var writes = new int[relShapes.Length];
        Array.Fill(reads, -1);
        Array.Fill(writes, -1);
        for (var i = 0; i < readRoles.Length; i++)
        {
            relShapes[i] = law.Roles[readRoles[i]];
            reads[i] = readRoles[i];
        }
        relShapes[^1] = law.Roles[wRole];
        writes[^1] = wRole;

        // Collect interior (non-wall if blockedRole >= 0) tuples for direct balance inference.
        var interiorTuples = new List<int[]>();
        for (var b = 0; b < law.StateCount; b++)
        {
            var bt = law.Tuple(b);
            var at = law.Tuple(law.Afters(b)[0]);
            if (blockedRole >= 0 && at[blockedRole] == 1) continue;
            var tuple = new int[relShapes.Length];
            for (var i = 0; i < readRoles.Length; i++) tuple[i] = bt[readRoles[i]];
            tuple[^1] = at[wRole];
            interiorTuples.Add(tuple);
        }

        var candidates = new List<BoundEquationFit>();

        // 1. Direct 0-residue linear balance via mode-of-first-differences (fast, handles walls naturally!).
        var inferred = InferLinearWeightsByStepDifferences(law, readRoles, wRole, blockedRole, maxWeight);
        if (inferred is not null)
        {
            var poly = Polynomial.Balance(relShapes, inferred);
            var fit = EvaluateBoundCandidate(law, poly, reads, writes, blockedRole, cost: null, freeRole: -1, freeCap: 0);
            if (fit.Exceptions.Length <= maxExceptions) candidates.Add(fit);

            // 2. Hidden 1-free-role residue auto-discovery (e.g. Shortfall excess, clamped OR overflow, split DigitSum carry).
            // Check residual r = Σ w_i * x_i + w_out * y across all states.
            int minRes = 0, maxRes = 0;
            foreach (var t in interiorTuples)
            {
                var r = 0;
                for (var i = 0; i < inferred.Length; i++) r += inferred[i] * t[i];
                if (r < minRes) minRes = r;
                if (r > maxRes) maxRes = r;
            }
            if (minRes != 0 || maxRes != 0)
            {
                // Try residue weight s in {+1, -1} or exact radix divisor when residual takes discrete multiples {0, -p}.
                var residueCandidates = new List<(int ResWeight, int Cap, int CostSign)>();
                if (minRes == 0 && maxRes > 0) residueCandidates.Add((-1, maxRes, 1));
                if (maxRes == 0 && minRes < 0) residueCandidates.Add((1, -minRes, 1));
                if (minRes < 0 && maxRes > 0)
                {
                    var absMin = -minRes;
                    var absMax = maxRes;
                    // Check if residue is strictly 0 or a multiple of a partition p (like DigitSum where cout*p is missing!).
                    var nzGcd = 0;
                    foreach (var t in interiorTuples)
                    {
                        var r = 0;
                        for (var i = 0; i < inferred.Length; i++) r += inferred[i] * t[i];
                        if (r != 0) nzGcd = Gcd(nzGcd, r < 0 ? -r : r);
                    }
                    if (nzGcd >= 2)
                    {
                        if (minRes == 0) residueCandidates.Add((-nzGcd, maxRes / nzGcd, 0));
                        else if (maxRes == 0) residueCandidates.Add((nzGcd, absMin / nzGcd, 0));
                    }
                    residueCandidates.Add((-1, absMax > absMin ? absMax : absMin, 1));
                    residueCandidates.Add((1, absMax > absMin ? absMax : absMin, 1));
                }
                else
                {
                    var nzGcd = 0;
                    foreach (var t in interiorTuples)
                    {
                        var r = 0;
                        for (var i = 0; i < inferred.Length; i++) r += inferred[i] * t[i];
                        if (r != 0) nzGcd = Gcd(nzGcd, r < 0 ? -r : r);
                    }
                    if (nzGcd >= 2)
                    {
                        if (minRes == 0 && maxRes > 0) residueCandidates.Add((-nzGcd, maxRes / nzGcd, 0));
                        if (maxRes == 0 && minRes < 0) residueCandidates.Add((nzGcd, (-minRes) / nzGcd, 0));
                    }
                }

                foreach (var (resWeight, cap, costWeight) in residueCandidates)
                {
                    if (cap < 1 || cap > 63) continue;
                    var resShape = law.Roles[wRole].IsUnary ? Shape.Unary(cap) : Shape.Binary(cap + 1);
                    Shape[] extShapes = [.. relShapes, resShape];
                    // Ensure table stays well inside Law.MaxBits
                    long relBits = 1;
                    foreach (var sh in extShapes) { relBits *= sh.Size; if (relBits > Law.MaxBits) break; }
                    if (relBits > Law.MaxBits) continue;

                    int[] extWeights = [.. inferred, resWeight];
                    int[] extReads = [.. reads, -1];
                    int[] extWrites = [.. writes, -1];
                    int[]? extCost = costWeight == 0 ? null : [.. Enumerable.Repeat(0, relShapes.Length), costWeight];
                    var extPoly = Polynomial.Balance(extShapes, extWeights);
                    var extFit = EvaluateBoundCandidate(law, extPoly, extReads, extWrites, blockedRole, extCost, relShapes.Length, cap);
                    if (extFit.Exceptions.Length <= maxExceptions) candidates.Add(extFit);
                }
            }
        }

        // 3. Fallback: exact degree-1 nullspace on all interior tuples (catches laws where single-step diffs are sparse).
        if (candidates.Count == 0 && interiorTuples.Count > 0)
        {
            foreach (var poly in Relations(relShapes, interiorTuples, maxDegree: 1))
            {
                var fit = EvaluateBoundCandidate(law, poly, reads, writes, blockedRole, cost: null, freeRole: -1, freeCap: 0);
                if (fit.Exceptions.Length <= maxExceptions) candidates.Add(fit);
            }
        }

        return candidates
            .OrderBy(c => c.Exceptions.Length)
            .ThenBy(c => c.EquationBits)
            .FirstOrDefault();
    }

    /// <summary>Predicts the integer value of <paramref name="target"/> under
    /// <paramref name="equation"/> given the other roles' values; returns null
    /// if the equation is non-linear in target, has zero slope, does not divide
    /// evenly, or leaves the target's shape.</summary>
    public static int? Predict(Polynomial equation, int target, IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(equation);
        ArgumentNullException.ThrowIfNull(values);
        long rest = 0, slope = 0;
        foreach (var (weight, exponents) in equation.Terms)
        {
            if (exponents[target] > 1 || (exponents[target] == 1 && exponents.Where((e, r) => r != target && e > 0).Any()))
                return null;
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

    /// <summary>Minimax active query selection: returns the input tuple (target
    /// slot = −1) whose predicted answers partition <paramref name="candidates"/>
    /// into the smallest worst-case group. Pass <paramref name="isWall"/> to
    /// skip known boundary wall states so wall holds do not falsely refute true
    /// interior laws. Returns null when all candidates agree everywhere allowed.</summary>
    public static int[]? Probe(
        IReadOnlyList<Polynomial> candidates,
        int target,
        Func<int[], bool>? isWall = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count < 2) return null;
        var roles = candidates[0].Roles;
        var inputs = Enumerable.Range(0, roles.Count).Where(r => r != target).ToArray();
        var count = inputs.Aggregate(1L, (n, r) => n * roles[r].Size);
        if (count > 1 << 20) throw new ArgumentException("Input domain too large to probe exhaustively.", nameof(candidates));

        int[]? best = null;
        var bestLargest = candidates.Count;
        for (long index = 0; index < count; index++)
        {
            var values = new int[roles.Count];
            values[target] = -1;
            var rest = index;
            foreach (var r in inputs) { values[r] = (int)(rest % roles[r].Size); rest /= roles[r].Size; }
            if (isWall is not null && isWall(values)) continue;
            var largest = candidates.GroupBy(c => Predict(c, target, values) ?? -1).Max(g => g.Count());
            if (largest < bestLargest) { bestLargest = largest; best = values; }
        }
        return best;
    }

    /// <summary>Compares a transition law beside a bound <see cref="Polynomial"/>:
    /// exact table parity, differing before-states, and description lengths.</summary>
    public static Comparison Compare(
        TransitionLaw law,
        Polynomial equation,
        int[] reads,
        int[] writes,
        int[]? cost = null,
        int blocked = -1)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(equation);
        var bound = Operations.Bind(equation.Relation, law.Roles, reads, writes, blocked: blocked, cost: cost);
        var differences = 0;
        for (var b = 0; b < law.StateCount; b++)
            if (!law.Afters(b).SequenceEqual(bound.Afters(b))) differences++;
        var stateBits = PackedInts.WidthFor(law.StateCount - 1);
        var eqBits = equation.Bits + 2L * stateBits * differences;
        return new Comparison(law.SameTable(bound), differences, law.BitCount, eqBits);
    }

    private static BoundEquationFit EvaluateBoundCandidate(
        TransitionLaw law,
        Polynomial equation,
        int[] reads,
        int[] writes,
        int blockedRole,
        int[]? cost,
        int freeRole,
        int freeCap)
    {
        var bound = Operations.Bind(equation.Relation, law.Roles, reads, writes, blocked: blockedRole, cost: cost);
        var exceptions = new List<(int Before, int After)>();
        for (var b = 0; b < law.StateCount; b++)
        {
            var actual = law.Afters(b);
            var predicted = bound.Afters(b);
            if (!actual.SequenceEqual(predicted))
                exceptions.Add((b, actual.Count == 1 ? actual[0] : -1));
        }
        var stateBits = PackedInts.WidthFor(law.StateCount - 1);
        var extraRoleBits = freeRole >= 0 ? 12L : 0L;
        var eqBits = equation.Bits + extraRoleBits + 2L * stateBits * exceptions.Count;
        return new BoundEquationFit(
            equation,
            reads,
            writes,
            blockedRole,
            cost,
            freeRole,
            freeCap,
            [.. exceptions],
            law.BitCount,
            eqBits,
            law.SameTable(bound));
    }

    private static int[]? InferLinearWeightsByStepDifferences(
        TransitionLaw law,
        int[] readRoles,
        int wRole,
        int blockedRole,
        int maxWeight)
    {
        var n = readRoles.Length;
        var strides = new int[law.Roles.Count];
        strides[0] = 1;
        for (var r = 1; r < law.Roles.Count; r++) strides[r] = strides[r - 1] * law.Roles[r - 1].Size;

        var weights = new int[n + 1];
        weights[^1] = -1;
        for (var i = 0; i < n; i++)
        {
            var r = readRoles[i];
            var stride = strides[r];
            var size = law.Roles[r].Size;
            var counts = new Dictionary<int, int>();
            for (var b = 0; b < law.StateCount; b++)
            {
                var bt = law.Tuple(b);
                if (bt[r] + 1 >= size) continue;
                var a0 = law.Tuple(law.Afters(b)[0]);
                var a1 = law.Tuple(law.Afters(b + stride)[0]);
                if (blockedRole >= 0 && (a0[blockedRole] == 1 || a1[blockedRole] == 1)) continue;
                // Ignore transitions where both endpoints sit at output boundaries (wall saturation).
                var y0 = a0[wRole];
                var y1 = a1[wRole];
                var yMax = law.Roles[wRole].Size - 1;
                if ((y0 == 0 && y1 == 0) || (y0 == yMax && y1 == yMax)) continue;
                var diff = y1 - y0;
                counts[diff] = counts.GetValueOrDefault(diff) + 1;
            }
            if (counts.Count == 0) return null;
            var mode = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key < 0 ? -kv.Key : kv.Key).First().Key;
            if (mode < -maxWeight || mode > maxWeight) return null;
            weights[i] = mode;
        }
        return weights.All(w => w == 0) ? null : weights;
    }

    private static bool IsBlockedIndicator(TransitionLaw law, int bitRole, int valueRole)
    {
        var sawBlocked = false;
        for (var b = 0; b < law.StateCount; b++)
        {
            var bt = law.Tuple(b);
            var at = law.Tuple(law.Afters(b)[0]);
            if (at[bitRole] == 1)
            {
                sawBlocked = true;
                if (at[valueRole] != bt[valueRole]) return false;
            }
        }
        return sawBlocked;
    }

    private static bool IsDecreasing(TransitionLaw law, int role) =>
        law.Accounting.Invariants.Any(inv => inv is NeverIncreases ni && ni.Role == role);

    private static bool IsIncreasing(TransitionLaw law, int role) =>
        law.Accounting.Invariants.Any(inv => inv is NeverDecreases nd && nd.Role == role);

    private static void ValidateBinding(int[] reads, int[] writes)
    {
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(writes);
        if (reads.Length != writes.Length || Enumerable.Range(0, reads.Length).Any(r => (reads[r] < 0) == (writes[r] < 0)))
            throw new ArgumentException("Each relation role reads a law role before the step or writes one after it.", nameof(writes));
    }

    private static int[] DecodeTuple(IReadOnlyList<Shape> roles, int index)
    {
        var values = new int[roles.Count];
        for (var r = 0; r < roles.Count; r++)
        {
            values[r] = index % roles[r].Size;
            index /= roles[r].Size;
        }
        return values;
    }

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

    private static long EvaluateLong(int[] exponents, int[] values)
    {
        long product = 1;
        for (var r = 0; r < exponents.Length; r++)
            for (var e = 0; e < exponents[r]; e++) product *= values[r];
        return product;
    }

    private static BigInteger EvaluateBig(int[] exponents, int[] values)
    {
        var product = BigInteger.One;
        for (var r = 0; r < exponents.Length; r++)
            for (var e = 0; e < exponents[r]; e++) product *= values[r];
        return product;
    }

    /// <summary>Exact reduced integer nullspace basis via fraction-free
    /// Gauss–Jordan elimination over <see cref="BigInteger"/>.</summary>
    private static List<BigInteger[]> Nullspace(BigInteger[][] rows, int columns)
    {
        var m = rows.Select(r => (BigInteger[])r.Clone()).ToList();
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
                var f = m[i][c];
                var g = m[row][c];
                for (var k = 0; k < columns; k++) m[i][k] = m[i][k] * g - m[row][k] * f;
                ReduceBig(m[i]);
            }
            pivots.Add((row, c));
            row++;
        }

        var pivotCols = pivots.Select(p => p.Column).ToHashSet();
        var common = BigInteger.One;
        foreach (var (r, c) in pivots) common = LcmBig(common, BigInteger.Abs(m[r][c]));

        var basis = new List<BigInteger[]>();
        for (var free = 0; free < columns; free++)
        {
            if (pivotCols.Contains(free)) continue;
            var v = new BigInteger[columns];
            v[free] = common;
            foreach (var (r, c) in pivots) v[c] = -m[r][free] * common / m[r][c];
            ReduceBig(v);
            basis.Add(v);
        }
        return basis;
    }

    private static void ReduceBig(BigInteger[] v)
    {
        var g = BigInteger.Zero;
        for (var k = 0; k < v.Length; k++)
            if (!v[k].IsZero) g = BigInteger.GreatestCommonDivisor(g, BigInteger.Abs(v[k]));
        if (g > BigInteger.One)
            for (var k = 0; k < v.Length; k++) v[k] /= g;
    }

    private static BigInteger LcmBig(BigInteger a, BigInteger b) =>
        a.IsZero || b.IsZero ? BigInteger.Zero : a / BigInteger.GreatestCommonDivisor(a, b) * b;

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    private sealed class TupleEquality : IEqualityComparer<int[]>
    {
        public static TupleEquality Instance { get; } = new();
        public bool Equals(int[]? x, int[]? y) => x is not null && y is not null && x.SequenceEqual(y);
        public int GetHashCode(int[] tuple) => tuple.Aggregate(17, (h, v) => unchecked(h * 31 + v));
    }
}
