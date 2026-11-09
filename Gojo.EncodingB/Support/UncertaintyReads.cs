namespace Gojo.EncodingB;

/// <summary>Reads that rank or restore without inventing a confidence value.</summary>
public static class UncertaintyReads
{
    /// <summary>U3: a cardinality constraint implied by a law's accounting
    /// signature. When every row of a chain preserves the sum of its preserved
    /// roles and writes nothing else, the total over all their variables is
    /// preserved too. Returns null when that cannot be derived: the signature
    /// preserves nothing, the law writes an unpreserved role, or the starting
    /// total is not settled.</summary>
    public static ConservationConstraint? Conservation(
        TransitionLaw law,
        IReadOnlyList<string[]> rowVariables,
        Func<string, ulong> startingSupport)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(rowVariables);
        ArgumentNullException.ThrowIfNull(startingSupport);
        var preserved = law.Signature.PreservedRoles;
        if (preserved.Count == 0 || law.WrittenRoles.Any(role => !preserved.Contains(role))) return null;
        var variables = new List<string>();
        var domains = new List<Domain>();
        foreach (var row in rowVariables)
        {
            if (row.Length != law.Roles.Count) throw new ArgumentException("Bind every role in every row.", nameof(rowVariables));
            foreach (var role in preserved)
            {
                var variable = row[law.RoleIndex(role)];
                if (variables.Contains(variable)) continue;
                variables.Add(variable);
                domains.Add(law.Roles[law.RoleIndex(role)].Domain);
            }
        }
        var total = 0;
        foreach (var variable in variables)
        {
            var support = startingSupport(variable);
            if (Bits.Count(support) != 1) return null;
            total += Bits.Indexes(support).First();
        }
        var roles = variables.Select((v, i) => new LawRole(v, domains[i])).ToArray();
        var relation = RelationLaw.FromPredicate($"{law.Name} conserves {total}", roles, values => values.Sum() == total,
            $"derived from accounting signature of {law.Name} ({law.ContentHash})");
        return new ConservationConstraint(relation, variables, total);
    }

    /// <summary>Counting semiring: how many admitted tuples the supports allow.
    /// The size of an ambiguity, without probability.</summary>
    public static long CountCompletions(RelationLaw law, ReadOnlySpan<ulong> supports)
    {
        ArgumentNullException.ThrowIfNull(law);
        var local = supports.ToArray();
        return law.AdmittedTuples().LongCount(tuple => Supported(tuple, local));
    }

    /// <summary>Tropical semiring (min, +): the admitted, supported tuple with the
    /// least total cost, such as least tension. Null when none is admitted.</summary>
    public static (long Cost, int[] Tuple)? MinimumCost(RelationLaw law, ReadOnlySpan<ulong> supports, Func<int, int, long> cost)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(cost);
        var local = supports.ToArray();
        (long Cost, int[] Tuple)? best = null;
        foreach (var tuple in law.AdmittedTuples())
        {
            if (!Supported(tuple, local)) continue;
            var total = tuple.Select((value, role) => cost(role, value)).Sum();
            if (best is null || total < best.Value.Cost) best = (total, tuple);
        }
        return best;
    }

    /// <summary>Bottleneck semiring (max, min): the supported tuple whose weakest
    /// role has the greatest capacity. Null when none is admitted.</summary>
    public static (long Capacity, int[] Tuple)? WidestPath(RelationLaw law, ReadOnlySpan<ulong> supports, Func<int, int, long> capacity)
    {
        ArgumentNullException.ThrowIfNull(law);
        ArgumentNullException.ThrowIfNull(capacity);
        var local = supports.ToArray();
        (long Capacity, int[] Tuple)? best = null;
        foreach (var tuple in law.AdmittedTuples())
        {
            if (!Supported(tuple, local)) continue;
            var narrowest = tuple.Select((value, role) => capacity(role, value)).Min();
            if (best is null || narrowest > best.Value.Capacity) best = (narrowest, tuple);
        }
        return best;
    }

    private static bool Supported(int[] tuple, ulong[] supports)
    {
        if (supports.Length != tuple.Length) throw new ArgumentException("Supply a support for every role.");
        for (var r = 0; r < tuple.Length; r++)
            if (!Bits.Get(supports[r], tuple[r])) return false;
        return true;
    }
}

/// <summary>A cardinality constraint derived from conservation, over the
/// variables it covers.</summary>
public sealed record ConservationConstraint(RelationLaw Relation, IReadOnlyList<string> Variables, int Total);
