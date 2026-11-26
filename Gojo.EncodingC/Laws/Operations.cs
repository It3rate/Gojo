namespace Gojo.EncodingC;

/// <summary>How <see cref="Operations.Bind"/> selects among admitted relation
/// tuples when one or more relation roles are left free (residue).</summary>
public enum FreeRolePolicy
{
    /// <summary>Keep only completions minimizing the sum of free roles (canonical
    /// residue fold: shortfall drops minimal excess, accumulate drops minimal
    /// overflow).</summary>
    LeastTotal,

    /// <summary>Keep every admitted completion across free roles, leaving the
    /// choice explicitly open for a structure or fork to settle.</summary>
    AllAdmitted,
}

/// <summary>Operations as relations, and their directional runs as bindings
/// (combining ops_claude's <c>Balance</c>/<c>Bind</c> with ops_sol's explicit
/// free-role policy and blocked-step visibility). A relation states what holds
/// among roles and moves nothing; a binding turns it into a directional
/// <see cref="TransitionLaw"/> where each relation role reads a law role before
/// the step, writes one after it, or is left free as explicit residue.</summary>
public static class Operations
{
    /// <summary>A linear balance: <c>Σ weights[r] · values[r] = 0</c> over the
    /// roles, positive weights on one pan and negative on the other.</summary>
    public static RelationLaw Balance(IReadOnlyList<Shape> roles, params int[] weights)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != roles.Count) throw new ArgumentException("One weight per role.", nameof(weights));
        if (weights.Any(w => w == 0)) throw new ArgumentException("A role of weight zero is on neither pan.", nameof(weights));
        return RelationLaw.FromPredicate(roles, values =>
        {
            long total = 0;
            for (var r = 0; r < weights.Length; r++) total += (long)weights[r] * values[r];
            return total == 0;
        });
    }

    /// <summary>A unary product relation: <c>a · b = c</c> with <c>c</c> of
    /// capacity <c>capacityA · capacityB</c>. Roles: a, b, c.</summary>
    public static RelationLaw Product(int capacityA, int capacityB) =>
        Product(Shape.Unary(capacityA), Shape.Unary(capacityB), Shape.Unary(checked(capacityA * capacityB)));

    /// <summary>A bounded product relation <c>a · b = c</c> over declared finite
    /// shapes (unary or binary).</summary>
    public static RelationLaw Product(Shape left, Shape right, Shape product) =>
        RelationLaw.FromPredicate([left, right, product], values => (long)values[0] * values[1] == values[2]);

    /// <summary>Runs <paramref name="relation"/> as a directional transition law
    /// over <paramref name="roles"/>. Relation role <c>r</c> reads law role
    /// <c>reads[r]</c> before the step, writes law role <c>writes[r]</c> after
    /// it, or stays free as residue (<c>-1</c> in both). Where no completion
    /// fits, the step holds (keeping written roles unchanged) and sets the
    /// optional <paramref name="blocked"/> bit role to 1 (0 when a completion
    /// wrote), so quiet-because-met and quiet-because-blocked stay structurally
    /// distinct.</summary>
    public static TransitionLaw Bind(
        RelationLaw relation,
        IReadOnlyList<Shape> roles,
        int[] reads,
        int[] writes,
        int blocked = -1,
        FreeRolePolicy policy = FreeRolePolicy.LeastTotal)
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
        if (blocked >= roles.Count || (blocked >= 0 && roles[blocked] != Shape.Bit))
            throw new ArgumentException("Blocked is a bit role of the law.", nameof(blocked));

        var readRoles = Enumerable.Range(0, count).Where(r => reads[r] >= 0).ToArray();
        var freeRoles = Enumerable.Range(0, count).Where(r => reads[r] < 0 && writes[r] < 0).ToArray();
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

            IEnumerable<IReadOnlyList<int>> chosen = fits;
            if (policy == FreeRolePolicy.LeastTotal && freeRoles.Length > 0)
            {
                var least = int.MaxValue;
                foreach (var t in fits)
                {
                    var s = FreeTotal(t);
                    if (s < least) least = s;
                }
                chosen = fits.Where(t => FreeTotal(t) == least);
            }

            foreach (var after in chosen.Select(t =>
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

        int FreeTotal(IReadOnlyList<int> tuple)
        {
            var s = 0;
            for (var i = 0; i < freeRoles.Length; i++) s += tuple[freeRoles[i]];
            return s;
        }
    }
}
