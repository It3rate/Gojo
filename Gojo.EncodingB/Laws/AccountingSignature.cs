namespace Gojo.EncodingB;

/// <summary>One accounting promise a transition law makes about every admitted
/// (before, after) pair. Checked exhaustively when the law is loaded.</summary>
public abstract record AccountingInvariant(IReadOnlyList<string> Roles)
{
    public abstract bool Holds(int[] roleIndexes, int[] before, int[] after);

    public abstract string Describe();
}

/// <summary>The sum of the role values is preserved: nothing created or
/// discarded. For presence roles this is "occupancy is preserved".</summary>
public sealed record PreservedSum(IReadOnlyList<string> Roles) : AccountingInvariant(Roles)
{
    public override bool Holds(int[] roleIndexes, int[] before, int[] after) =>
        roleIndexes.Sum(r => before[r]) == roleIndexes.Sum(r => after[r]);

    public override string Describe() => $"sum of {string.Join(" + ", Roles)} preserved";
}

public sealed record Unchanged(string Role) : AccountingInvariant([Role])
{
    public override bool Holds(int[] roleIndexes, int[] before, int[] after) => before[roleIndexes[0]] == after[roleIndexes[0]];

    public override string Describe() => $"{Role} unchanged";
}

/// <summary>The role never gains: a source can only hold or give.</summary>
public sealed record NeverIncreases(string Role) : AccountingInvariant([Role])
{
    public override bool Holds(int[] roleIndexes, int[] before, int[] after) => after[roleIndexes[0]] <= before[roleIndexes[0]];

    public override string Describe() => $"{Role} never increases";
}

/// <summary>The role never loses: a receiver can only hold or take.</summary>
public sealed record NeverDecreases(string Role) : AccountingInvariant([Role])
{
    public override bool Holds(int[] roleIndexes, int[] before, int[] after) => after[roleIndexes[0]] >= before[roleIndexes[0]];

    public override string Describe() => $"{Role} never decreases";
}

/// <summary>The accounting promises of a transition law. A law whose table
/// breaks its own signature is refused at load.</summary>
public sealed class AccountingSignature
{
    public AccountingSignature(bool functional, params AccountingInvariant[] invariants)
    {
        Functional = functional;
        Invariants = invariants.ToArray();
    }

    /// <summary>Every before-state admits exactly one after-state, so the law
    /// can execute on concrete state.</summary>
    public bool Functional { get; }

    public IReadOnlyList<AccountingInvariant> Invariants { get; }

    public static AccountingSignature None { get; } = new(false);

    /// <summary>Roles whose sum the signature preserves, if any.</summary>
    public IReadOnlyList<string> PreservedRoles =>
        Invariants.OfType<PreservedSum>().SelectMany(p => p.Roles).Distinct().ToArray();

    public string Describe() =>
        string.Join("; ", (Functional ? new[] { "functional" } : []).Concat(Invariants.Select(i => i.Describe())));

    /// <summary>Exhaustive check over every admitted pair of a candidate table.</summary>
    internal IReadOnlyList<string> Check(LawTable law, int states, Func<int, int, bool> admits)
    {
        var violations = new List<string>();
        var resolved = Invariants.Select(i => (Invariant: i, Indexes: i.Roles.Select(law.RoleIndex).ToArray())).ToArray();
        for (var before = 0; before < states; before++)
        {
            var afterCount = 0;
            var b = law.Tuple(before);
            for (var after = 0; after < states; after++)
            {
                if (!admits(before, after)) continue;
                afterCount++;
                var a = law.Tuple(after);
                foreach (var (invariant, indexes) in resolved)
                    if (!invariant.Holds(indexes, b, a))
                        violations.Add($"{law.DescribeTuple(before)} -> {law.DescribeTuple(after)} breaks: {invariant.Describe()}");
            }
            if (Functional && afterCount != 1)
                violations.Add($"{law.DescribeTuple(before)} admits {afterCount} after-states; a functional law admits exactly one");
        }
        return violations;
    }
}
