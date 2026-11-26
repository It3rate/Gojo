namespace Gojo.EncodingC;

/// <summary>Operations as relations, and their runs as bindings
/// (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §3.1). A relation says what holds
/// among some amounts and moves nothing. A law that runs it is a binding: each
/// relation role reads a law role before the step, writes one after it, or is
/// left free. A free role can account for residue outside the written result. Which
/// completion a binding keeps is stated: a cost per free role
/// says what leaving residue there costs, and the cheapest completions are
/// kept. With no cost every completion contributes its written values.
/// <para>So one relation gives several operations that differ only in binding.
/// Heap, the balance a + w·b = total, is a sum written into a new count, the
/// same sum written over the count it read (accumulate, with what overflows
/// capacity as residue), and, narrowed backward, subtraction. The gap, have and
/// what is short against want and what is over, is shortfall with the excess
/// as residue, and excess with the roles swapped. The product, read forward,
/// pairs two counts into a new one; narrowed, it divides, and with both
/// inputs bound to one sample it squares. A run that finds no completion
/// holds, leaving the relation unmet where the structure can see it.</para>
/// <para>Both are polynomial relations (<see cref="Polynomial"/>). A balance
/// is along: amounts on two pans, a weight's sign saying which pan and its
/// size the rate, so units trade one for one at weight 1. A product is across:
/// each unit of the result is present only when a unit of both inputs is, a
/// corner at every cell.</para></summary>
public static class Operations
{
    /// <summary>A balance: Σ weight · value = 0 over the roles, positive weights
    /// on one pan and negative on the other.</summary>
    public static RelationLaw Balance(IReadOnlyList<Shape> roles, params int[] weights) => Polynomial.Balance(roles, weights).Relation;

    /// <summary>A product: a · b = c, with c of capacity a · b. Roles: a, b, c.</summary>
    public static RelationLaw Product(int capacityA, int capacityB) => Polynomial.Product(capacityA, capacityB).Relation;

    /// <summary>The tuples that satisfy every equation in <paramref name="equations"/>
    /// over their shared roles: a system of polynomial relations read as one table.</summary>
    public static RelationLaw System(IReadOnlyList<Polynomial> equations)
    {
        ArgumentNullException.ThrowIfNull(equations);
        if (equations.Count == 0) throw new ArgumentException("A system has at least one equation.", nameof(equations));
        if (equations.Count == 1) return equations[0].Relation;
        var roles = equations[0].Roles;
        for (var i = 1; i < equations.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(equations[i]);
            if (!equations[i].Roles.SequenceEqual(roles))
                throw new ArgumentException("Every equation in a system must share the same role shapes.", nameof(equations));
        }
        return RelationLaw.FromPredicate(roles, values =>
        {
            for (var i = 0; i < equations.Count; i++)
                if (!equations[i].Holds(values)) return false;
            return true;
        });
    }

    /// <summary>Runs a relation as a law over <paramref name="roles"/>. Relation
    /// role r reads law role <paramref name="reads"/>[r] before the step, or
    /// writes law role <paramref name="writes"/>[r] after it; −1 for neither
    /// leaves it free. Every admitted completion is an after-state, or, with a
    /// <paramref name="cost"/> per relation role, every completion whose free
    /// roles cost least (Σ cost · value over the free roles): one where the
    /// relation settles the written roles, and several where it leaves them
    /// open, for a structure to choose. Where
    /// nothing fits, the step holds, as a move holds when its target has no
    /// room: the written roles keep their values and the relation is left
    /// unmet, visible to anything that reads it. Law roles no relation role
    /// writes are unchanged. Binding a role's read and write to the same law
    /// role writes a result over the count it was read from.
    /// <para>A <paramref name="blocked"/> law role, a bit, says which kind of
    /// step it was: 1 where nothing fit and the step held, 0 where a completion
    /// was written. A structure can distinguish those cases by reading the bit.
    /// If several completions have the same written values, the transition
    /// keeps one after-state. Bind a residue to a written role to retain its
    /// value in the running structure.</para></summary>
    public static TransitionLaw Bind(RelationLaw relation, IReadOnlyList<Shape> roles, int[] reads, int[] writes, int blocked = -1, int[]? cost = null)
        => Bind(relation, roles, reads, writes, blocked, cost, equation: null);

    /// <summary>Runs an authored equation by the same binding. A homogeneous
    /// balance whose reads and writes account for the same positive weighted
    /// roles derives a checked preserved sum in the resulting transition.</summary>
    public static TransitionLaw Bind(Polynomial equation, IReadOnlyList<Shape> roles, int[] reads, int[] writes, int blocked = -1, int[]? cost = null)
    {
        ArgumentNullException.ThrowIfNull(equation);
        return Bind(equation.Relation, roles, reads, writes, blocked, cost, equation);
    }

    private static TransitionLaw Bind(RelationLaw relation, IReadOnlyList<Shape> roles, int[] reads, int[] writes,
        int blocked, int[]? cost, Polynomial? equation)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(writes);
        var count = relation.Roles.Count;
        if (reads.Length != count || writes.Length != count) throw new ArgumentException("Bind every relation role.", nameof(reads));
        if (cost is not null && cost.Length != count) throw new ArgumentException("One cost per relation role.", nameof(cost));
        for (var r = 0; r < count; r++)
        {
            if (reads[r] < -1) throw new ArgumentOutOfRangeException(nameof(reads), "An unbound role is −1.");
            if (writes[r] < -1) throw new ArgumentOutOfRangeException(nameof(writes), "An unbound role is −1.");
            if (reads[r] >= 0 && writes[r] >= 0) throw new ArgumentException($"Relation role {r} reads or writes, not both.", nameof(writes));
            var bound = reads[r] >= 0 ? reads[r] : writes[r];
            if (bound >= roles.Count) throw new ArgumentOutOfRangeException(nameof(roles), $"Relation role {r} names law role {bound}.");
            if (bound >= 0 && roles[bound] != relation.Roles[r]) throw new ArgumentException($"Relation role {r} and law role {bound} differ in shape.", nameof(roles));
        }
        CheckBlockedRole(roles, blocked);
        int[] written = [.. writes.Where(w => w >= 0), .. blocked >= 0 ? [blocked] : Array.Empty<int>()];
        if (written.Distinct().Count() != written.Length) throw new ArgumentException("Each law role has one writer.", nameof(writes));

        // The admitted tuples, grouped by the values their read roles see.
        var readRoles = Enumerable.Range(0, count).Where(r => reads[r] >= 0).ToArray();
        long Key(Func<int, int> value) => readRoles.Aggregate(0L, (key, r) => key * relation.Roles[r].Size + value(r));
        var byReads = relation.AdmittedTuples().GroupBy(t => Key(r => t[r])).ToDictionary(g => g.Key, g => g.ToArray());
        var probe = TransitionLaw.FromPairs(roles, [], Accounting.None);
        var pairs = new List<(int Before, int After)>();
        var afterCounts = new int[probe.StateCount];
        for (var before = 0; before < probe.StateCount; before++)
        {
            var state = probe.Tuple(before);
            if (!byReads.TryGetValue(Key(r => state[reads[r]]), out var fits))
            {
                var held = (int[])state.Clone();
                if (blocked >= 0) held[blocked] = 1;
                pairs.Add((before, probe.TupleIndex(held)));
                afterCounts[before]++;
                continue;
            }
            var least = fits.Min(t => Free(t));
            foreach (var after in fits.Where(t => Free(t) == least).Select(t =>
                     {
                         var next = (int[])state.Clone();
                         for (var r = 0; r < count; r++) if (writes[r] >= 0) next[writes[r]] = t[r];
                         if (blocked >= 0) next[blocked] = 0;
                         return probe.TupleIndex(next);
                     }).Distinct())
            {
                pairs.Add((before, after));
                afterCounts[before]++;
            }
        }
        Invariant[] invariants = [.. Enumerable.Range(0, roles.Count).Where(r => !written.Contains(r)).Select(r => new Unchanged(r))];
        if (equation is not null && ConservedBalance(equation, reads, writes, roles.Count) is { } sum)
            invariants = [.. invariants, sum];
        return TransitionLaw.FromPairs(roles, pairs, new Accounting(afterCounts.All(n => n == 1), invariants));

        long Free(IReadOnlyList<int> tuple) => cost is null ? 0 : Enumerable.Range(0, count).Where(r => reads[r] < 0 && writes[r] < 0).Sum(r => (long)cost[r] * tuple[r]);
    }

    internal static void CheckBlockedRole(IReadOnlyList<Shape> roles, int blocked)
    {
        if (blocked < -1 || blocked >= roles.Count || (blocked >= 0 && roles[blocked] != Shape.Bit))
            throw new ArgumentException("Blocked is a bit role of the law or −1.", nameof(blocked));
    }

    private static PreservedSum? ConservedBalance(Polynomial equation, int[] reads, int[] writes, int roleCount)
    {
        var before = new long[roleCount];
        var after = new long[roleCount];
        foreach (var (weight, exponents) in equation.Terms)
        {
            var r = -1;
            for (var i = 0; i < exponents.Count; i++)
            {
                if (exponents[i] == 0) continue;
                if (exponents[i] != 1 || r >= 0) return null;
                r = i;
            }
            if (r < 0) return null;
            if (reads[r] >= 0) before[reads[r]] += weight;
            else if (writes[r] >= 0) after[writes[r]] -= (long)weight;
            else return null;
        }

        // The same accounts must appear on both sides once the distinct
        // relation roles have been collected at their bound law roles.
        if (!before.SequenceEqual(after)) return null;
        int[] conserved = [.. Enumerable.Range(0, roleCount).Where(r => before[r] != 0)];
        if (conserved.Length < 2) return null;
        var orientation = before[conserved[0]] < 0 ? -1 : 1;
        long[] weights = [.. conserved.Select(r => orientation * before[r])];
        if (weights.Any(weight => weight <= 0 || weight > int.MaxValue)) return null;
        return new PreservedSum(conserved, [.. weights.Select(weight => (int)weight)]);
    }
}
