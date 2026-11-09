namespace Gojo.EncodingB;

/// <summary>Joint support over a small scope of named variables: which whole
/// combinations are still possible. Exact within its scope (U1 for one law's
/// roles, U2 for a merged cluster). Projecting it to per-variable marginals can
/// lose correlation; that loss is returned as a record, never hidden.</summary>
public sealed class JointSupport
{
    /// <summary>Joint scopes are exponential; keep them small and explicit.</summary>
    public const int MaxStates = 1 << 16;

    private readonly bool[] states;
    private readonly string[] variables;
    private readonly Domain[] domains;

    private JointSupport(string[] variables, Domain[] domains, bool[] states)
    {
        this.variables = variables;
        this.domains = domains;
        this.states = states;
    }

    public IReadOnlyList<string> Variables => variables;
    public IReadOnlyList<Domain> Domains => domains;
    public int StateCount => states.Length;

    /// <summary>Number of whole combinations still possible.</summary>
    public int Count => states.Count(s => s);

    /// <summary>The product of independent marginals. Exact only if the
    /// variables really are unrelated; otherwise it admits spurious states.</summary>
    public static JointSupport FromMarginals(IReadOnlyList<(string Variable, Domain Domain, ulong Support)> marginals)
    {
        ArgumentNullException.ThrowIfNull(marginals);
        if (marginals.Select(m => m.Variable).Distinct(StringComparer.Ordinal).Count() != marginals.Count)
            throw new ArgumentException("Variables must be distinct.", nameof(marginals));
        var domains = marginals.Select(m => m.Domain).ToArray();
        var count = domains.Aggregate(1L, (p, d) => p * d.Size);
        if (count > MaxStates) throw new ArgumentException("Joint scope too large; split it and record the decorrelation.", nameof(marginals));
        var joint = new JointSupport(marginals.Select(m => m.Variable).ToArray(), domains, new bool[count]);
        for (var index = 0; index < count; index++)
        {
            var values = joint.Decode(index);
            joint.states[index] = marginals.Select((m, i) => Bits.Get(m.Support, values[i])).All(x => x);
        }
        return joint;
    }

    public IEnumerable<int[]> States()
    {
        for (var index = 0; index < states.Length; index++)
            if (states[index]) yield return Decode(index);
    }

    public bool Contains(params int[] values) => states[Encode(values)];

    public ulong Marginal(string variable)
    {
        var position = PositionOf(variable);
        ulong support = 0;
        foreach (var values in States()) support |= Bits.Of(values[position]);
        return support;
    }

    /// <summary>The joint over a subset of variables. Exact: a projection of a
    /// joint is still a joint.</summary>
    public JointSupport Project(params string[] keep)
    {
        var positions = keep.Select(PositionOf).ToArray();
        var result = new JointSupport(keep.ToArray(), positions.Select(p => domains[p]).ToArray(),
            new bool[positions.Aggregate(1, (p, i) => p * domains[i].Size)]);
        foreach (var values in States()) result.states[result.Encode(positions.Select(p => values[p]).ToArray())] = true;
        return result;
    }

    /// <summary>Prediction through a transition law whose roles read the named
    /// variables. Every other variable keeps its value and its correspondence,
    /// so a control shared by two passages stays one control. Never executes.</summary>
    public JointSupport ApplyTransition(TransitionLaw law, params string[] roleVariables)
    {
        ArgumentNullException.ThrowIfNull(law);
        var positions = BindRoles(law, roleVariables);
        var next = new JointSupport(variables, domains, new bool[states.Length]);
        foreach (var values in States())
        {
            var before = law.TupleIndex(positions.Select(p => values[p]).ToArray());
            foreach (var afterIndex in law.Afters(before))
            {
                var after = law.Tuple(afterIndex);
                var copy = values.ToArray();
                for (var r = 0; r < positions.Length; r++) copy[positions[r]] = after[r];
                next.states[next.Encode(copy)] = true;
            }
        }
        return next;
    }

    /// <summary>Keeps only combinations a relation admits: monotone narrowing.</summary>
    public JointSupport Restrict(RelationLaw law, params string[] roleVariables)
    {
        ArgumentNullException.ThrowIfNull(law);
        var positions = BindRoles(law, roleVariables);
        var next = new JointSupport(variables, domains, new bool[states.Length]);
        foreach (var values in States())
            if (law.Admits(positions.Select(p => values[p]).ToArray()))
                next.states[next.Encode(values)] = true;
        return next;
    }

    /// <summary>Per-variable marginals, plus the record of what projecting to
    /// them loses: the combinations their product admits that this joint does not.</summary>
    public (IReadOnlyList<(string Variable, Domain Domain, ulong Support)> Marginals, DecorrelationRecord Record) ToMarginals()
    {
        var marginals = variables.Select((v, i) => (v, domains[i], Marginal(v))).ToArray();
        var product = FromMarginals(marginals);
        var spurious = product.States().Where(values => !Contains(values)).Select(DescribeState).ToArray();
        return (marginals, new DecorrelationRecord(variables, Count, product.Count, spurious));
    }

    public string DescribeState(int[] values) =>
        "[" + string.Join(", ", values.Select((v, i) => $"{variables[i]}={domains[i].NameOf(v)}")) + "]";

    private int[] BindRoles(LawTable law, string[] roleVariables)
    {
        if (roleVariables.Length != law.Roles.Count) throw new ArgumentException($"Bind all {law.Roles.Count} roles.", nameof(roleVariables));
        var positions = roleVariables.Select(PositionOf).ToArray();
        for (var r = 0; r < positions.Length; r++)
            if (domains[positions[r]].Size != law.Roles[r].Domain.Size)
                throw new ArgumentException($"Variable {roleVariables[r]} does not match role {law.Roles[r].Name}.", nameof(roleVariables));
        if (positions.Distinct().Count() != positions.Length)
            throw new ArgumentException("Each role binds a distinct variable here; one variable used twice needs a diagonal law.", nameof(roleVariables));
        return positions;
    }

    private int PositionOf(string variable)
    {
        var index = Array.IndexOf(variables, variable);
        return index >= 0 ? index : throw new ArgumentException($"No variable '{variable}' in this scope.", nameof(variable));
    }

    private int Encode(int[] values)
    {
        var index = 0;
        var radix = 1;
        for (var i = 0; i < values.Length; i++)
        {
            index += values[i] * radix;
            radix *= domains[i].Size;
        }
        return index;
    }

    private int[] Decode(int index)
    {
        var values = new int[domains.Length];
        for (var i = 0; i < domains.Length; i++)
        {
            values[i] = index % domains[i].Size;
            index /= domains[i].Size;
        }
        return values;
    }
}

/// <summary>What projecting a joint to marginals lost: the product of marginals
/// admits <see cref="ProductStates"/> combinations, the joint only
/// <see cref="JointStates"/>. The difference is not independence.</summary>
public sealed record DecorrelationRecord(
    IReadOnlyList<string> Variables,
    int JointStates,
    int ProductStates,
    IReadOnlyList<string> Spurious)
{
    public bool Lossless => Spurious.Count == 0;
}
