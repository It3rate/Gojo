namespace Gojo.EncodingC;

/// <summary>Operations as relations, and their runs as bindings
/// (ops_claude/GojoOperations-2026-11.md, §1.3). A relation says what holds
/// among some amounts and moves nothing. A law that runs it is a binding: each
/// relation role reads a law role before the step, writes one after it, or is
/// left free. A free role is residue: what the step had nowhere to put, kept as
/// small as the relation allows and then dropped, which is how a fold loses it.
/// <para>So one relation gives several operations that differ only in binding.
/// Heap, the balance a + w·b = total, is a sum written into a new count, the
/// same sum written over the count it read (accumulate, with what overflows
/// capacity as residue), and, narrowed backward, subtraction. The gap, have and
/// what is short against want and what is over, is shortfall with the excess
/// as residue, and excess with the roles swapped. The product, read forward,
/// pairs two counts into a new one; narrowed, it divides, and with both
/// inputs bound to one sample it squares. A run that finds no completion
/// holds, leaving the relation unmet where the structure can see it.</para>
/// <para>Two relations are the mechanisms. A balance is along: amounts on two
/// pans, a weight's sign saying which pan and its size the rate, so units
/// trade one for one at weight 1. A product is across: each unit of the
/// result is present only when a unit of both inputs is, a corner at every
/// cell.</para></summary>
public static class Operations
{
    /// <summary>A balance: Σ weight · value = 0 over the roles, positive weights
    /// on one pan and negative on the other.</summary>
    public static RelationLaw Balance(IReadOnlyList<Shape> roles, params int[] weights)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != roles.Count) throw new ArgumentException("One weight per role.", nameof(weights));
        if (weights.Any(w => w == 0)) throw new ArgumentException("A role of weight zero is on neither pan.", nameof(weights));
        return RelationLaw.FromPredicate(roles, values => values.Select((v, r) => weights[r] * v).Sum() == 0);
    }

    /// <summary>A product: a · b = c, with c of capacity a · b. Roles: a, b, c.</summary>
    public static RelationLaw Product(int capacityA, int capacityB) =>
        RelationLaw.FromPredicate([Shape.Unary(capacityA), Shape.Unary(capacityB), Shape.Unary(checked(capacityA * capacityB))],
            values => values[0] * values[1] == values[2]);

    /// <summary>Runs a relation as a law over <paramref name="roles"/>. Relation
    /// role r reads law role <paramref name="reads"/>[r] before the step, or
    /// writes law role <paramref name="writes"/>[r] after it; −1 for neither
    /// leaves it free. Every admitted completion with the least free total is
    /// an after-state: one where the relation settles the written roles, and
    /// several where it leaves them open, for a structure to choose. Where
    /// nothing fits, the step holds, as a move holds when its target has no
    /// room: the written roles keep their values and the relation is left
    /// unmet, visible to anything that reads it. Law roles no relation role
    /// writes are unchanged. Binding a role's read and write to the same law
    /// role writes a result over the count it was read from.
    /// <para>A <paramref name="blocked"/> law role, a bit, says which kind of
    /// step it was: 1 where nothing fit and the step held, 0 where a completion
    /// was written. A structure settles quietly either way; this bit tells
    /// quiet because met from quiet because blocked, where structure can read
    /// it.</para></summary>
    public static TransitionLaw Bind(RelationLaw relation, IReadOnlyList<Shape> roles, int[] reads, int[] writes, int blocked = -1)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(writes);
        var count = relation.Roles.Count;
        if (reads.Length != count || writes.Length != count) throw new ArgumentException("Bind every relation role.", nameof(reads));
        for (var r = 0; r < count; r++)
        {
            if (reads[r] >= 0 && writes[r] >= 0) throw new ArgumentException($"Relation role {r} reads or writes, not both.", nameof(writes));
            var bound = reads[r] >= 0 ? reads[r] : writes[r];
            if (bound >= roles.Count) throw new ArgumentOutOfRangeException(nameof(roles), $"Relation role {r} names law role {bound}.");
            if (bound >= 0 && roles[bound] != relation.Roles[r]) throw new ArgumentException($"Relation role {r} and law role {bound} differ in shape.", nameof(roles));
        }
        int[] written = [.. writes.Where(w => w >= 0), .. blocked >= 0 ? [blocked] : Array.Empty<int>()];
        if (written.Distinct().Count() != written.Length) throw new ArgumentException("Each law role has one writer.", nameof(writes));
        if (blocked >= roles.Count || (blocked >= 0 && roles[blocked] != Shape.Bit)) throw new ArgumentException("Blocked is a bit role of the law.", nameof(blocked));

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
        var unchanged = Enumerable.Range(0, roles.Count).Where(r => !written.Contains(r)).Select(r => (Invariant)new Unchanged(r)).ToArray();
        return TransitionLaw.FromPairs(roles, pairs, new Accounting(afterCounts.All(n => n == 1), unchanged));

        int Free(IReadOnlyList<int> tuple) => Enumerable.Range(0, count).Where(r => reads[r] < 0 && writes[r] < 0).Sum(r => tuple[r]);
    }
}
