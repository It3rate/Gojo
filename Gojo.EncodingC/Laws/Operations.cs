namespace Gojo.EncodingC;

/// <summary>Operations as relations, and their runs as bindings.
/// A relation says what holds among roles and moves nothing. A transition law
/// that runs it is a binding: each relation role reads a law role before the
/// step, writes one after it, or is left free (residue).
/// <para>Completion over free roles is stated numerically, never assumed by an
/// enum: pass a <c>cost</c> per relation role to keep completions that minimize
/// <c>Σ cost[r] · tuple[r]</c> over free roles, or leave <c>cost</c> null to
/// keep every admitted completion open. Where no completion fits, the step
/// holds; an optional <c>blocked</c> bit role records 1 on hold and 0 on a
/// written completion so quiet-because-met stays distinct from
/// quiet-because-blocked.</para></summary>
public static class Operations
{
    /// <summary>Linear balance relation <c>Σ weights[r] · value[r] = 0</c>.</summary>
    public static RelationLaw Balance(IReadOnlyList<Shape> roles, params int[] weights) =>
        Polynomial.Balance(roles, weights).Relation;

    /// <summary>Product relation <c>a · b = c</c> over unary counts of
    /// capacities <paramref name="capacityA"/>, <paramref name="capacityB"/>,
    /// and <c>capacityA · capacityB</c>.</summary>
    public static RelationLaw Product(int capacityA, int capacityB) =>
        Polynomial.Product(capacityA, capacityB).Relation;

    /// <summary>Product relation <c>a · b = c</c> over explicit shapes.</summary>
    public static RelationLaw Product(Shape left, Shape right, Shape product) =>
        Polynomial.Product(left, right, product).Relation;

    /// <summary>Binds a <paramref name="relation"/> into a <see cref="TransitionLaw"/>
    /// over <paramref name="roles"/>. For each relation role <c>r</c>:
    /// <list type="bullet">
    /// <item><c>reads[r] &gt;= 0</c>: reads law role <c>reads[r]</c> before the step;</item>
    /// <item><c>writes[r] &gt;= 0</c>: writes law role <c>writes[r]</c> after the step;</item>
    /// <item>both <c>-1</c>: free residue role.</item>
    /// </list>
    /// When <paramref name="cost"/> is null, every admitted completion is kept
    /// open; when supplied, completions minimizing <c>Σ cost[r] · tuple[r]</c>
    /// over free roles are kept. When nothing fits, the law holds (and sets
    /// <paramref name="blocked"/> to 1 if present).</summary>
    public static TransitionLaw Bind(
        RelationLaw relation,
        IReadOnlyList<Shape> roles,
        int[] reads,
        int[] writes,
        int blocked = -1,
        int[]? cost = null)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(reads);
        ArgumentNullException.ThrowIfNull(writes);
        var count = relation.Roles.Count;
        if (reads.Length != count || writes.Length != count)
            throw new ArgumentException("Bind every relation role.", nameof(reads));
        if (cost is not null && cost.Length != count)
            throw new ArgumentException("Supply one cost per relation role.", nameof(cost));
        if (blocked >= roles.Count || (blocked >= 0 && roles[blocked] != Shape.Bit))
            throw new ArgumentException("Blocked must be a Shape.Bit role of the law.", nameof(blocked));

        var written = new List<int>();
        for (var r = 0; r < count; r++)
        {
            if (reads[r] >= 0 && writes[r] >= 0)
                throw new ArgumentException($"Relation role {r} reads or writes, not both.", nameof(writes));
            var bound = reads[r] >= 0 ? reads[r] : writes[r];
            if (bound >= roles.Count)
                throw new ArgumentOutOfRangeException(nameof(roles), $"Relation role {r} references law role {bound}.");
            if (bound >= 0 && roles[bound] != relation.Roles[r])
                throw new ArgumentException($"Relation role {r} shape {relation.Roles[r]} differs from law role {bound} shape {roles[bound]}.", nameof(roles));
            if (writes[r] >= 0) written.Add(writes[r]);
        }
        if (blocked >= 0) written.Add(blocked);
        if (written.Distinct().Count() != written.Count)
            throw new ArgumentException("Each written law role can have at most one writer.", nameof(writes));

        var readRoles = Enumerable.Range(0, count).Where(r => reads[r] >= 0).ToArray();
        long ReadKey(Func<int, int> val)
        {
            long key = 0;
            for (var i = 0; i < readRoles.Length; i++)
            {
                var r = readRoles[i];
                key = key * relation.Roles[r].Size + val(r);
            }
            return key;
        }

        var byReads = new Dictionary<long, List<IReadOnlyList<int>>>();
        foreach (var tuple in relation.AdmittedTuples())
        {
            var key = ReadKey(r => tuple[r]);
            if (!byReads.TryGetValue(key, out var list)) byReads[key] = list = [];
            list.Add(tuple);
        }

        var probe = TransitionLaw.FromPairs(roles, [], Accounting.None);
        var pairs = new List<(int Before, int After)>();
        var functional = true;
        var nextState = new int[roles.Count];

        for (var before = 0; before < probe.StateCount; before++)
        {
            var state = probe.Tuple(before);
            var key = ReadKey(r => state[reads[r]]);
            if (!byReads.TryGetValue(key, out var fits))
            {
                Array.Copy(state, nextState, roles.Count);
                if (blocked >= 0) nextState[blocked] = 1;
                pairs.Add((before, probe.TupleIndex(nextState)));
                continue;
            }

            long bestCost = long.MaxValue;
            if (cost is not null)
            {
                foreach (var tuple in fits)
                {
                    var c = FreeCost(tuple);
                    if (c < bestCost) bestCost = c;
                }
            }

            var seenAfter = new HashSet<int>();
            foreach (var tuple in fits)
            {
                if (cost is not null && FreeCost(tuple) != bestCost) continue;
                Array.Copy(state, nextState, roles.Count);
                for (var r = 0; r < count; r++)
                    if (writes[r] >= 0) nextState[writes[r]] = tuple[r];
                if (blocked >= 0) nextState[blocked] = 0;
                seenAfter.Add(probe.TupleIndex(nextState));
            }

            if (seenAfter.Count != 1) functional = false;
            foreach (var after in seenAfter) pairs.Add((before, after));
        }

        var writtenSet = written.ToHashSet();
        var unchanged = Enumerable.Range(0, roles.Count)
            .Where(r => !writtenSet.Contains(r))
            .Select(r => (Invariant)new Unchanged(r))
            .ToArray();
        return TransitionLaw.FromPairs(roles, pairs, new Accounting(functional, unchanged));

        long FreeCost(IReadOnlyList<int> tuple)
        {
            long sum = 0;
            for (var r = 0; r < count; r++)
                if (reads[r] < 0 && writes[r] < 0) sum += (long)cost![r] * tuple[r];
            return sum;
        }
    }
}
