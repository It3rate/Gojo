namespace Gojo.EncodingC;

/// <summary>One promise a transition law makes about every admitted
/// (before, after) pair, over role numbers. Checked exhaustively when the law
/// is made.</summary>
public abstract record Invariant
{
    public abstract bool Holds(int[] before, int[] after);
}

/// <summary>Σ weightᵣ · valueᵣ is the same before and after: nothing is made or
/// lost, only moved. Weights default to one. Unequal weights state a rate, such
/// as two units taken for one given, which is how a calibration is written when
/// one is wanted.</summary>
public sealed record PreservedSum : Invariant
{
    public PreservedSum(int[] roles, int[]? weights = null)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length == 0) throw new ArgumentException("Name at least one role.", nameof(roles));
        weights ??= Enumerable.Repeat(1, roles.Length).ToArray();
        if (weights.Length != roles.Length) throw new ArgumentException("One weight per role.", nameof(weights));
        Roles = roles.ToArray();
        Weights = weights.ToArray();
    }

    public IReadOnlyList<int> Roles { get; }

    public IReadOnlyList<int> Weights { get; }

    public override bool Holds(int[] before, int[] after)
    {
        long b = 0, a = 0;
        for (var i = 0; i < Roles.Count; i++)
        {
            b += (long)Weights[i] * before[Roles[i]];
            a += (long)Weights[i] * after[Roles[i]];
        }
        return a == b;
    }
}

/// <summary>The role never changes.</summary>
public sealed record Unchanged(int Role) : Invariant
{
    public override bool Holds(int[] before, int[] after) => before[Role] == after[Role];
}

/// <summary>The role only holds or gives.</summary>
public sealed record NeverIncreases(int Role) : Invariant
{
    public override bool Holds(int[] before, int[] after) => after[Role] <= before[Role];
}

/// <summary>The role only holds or takes.</summary>
public sealed record NeverDecreases(int Role) : Invariant
{
    public override bool Holds(int[] before, int[] after) => after[Role] >= before[Role];
}

/// <summary>An admitted pair that breaks a promise. <see cref="Invariant"/> is
/// the index of the broken invariant, or −1 when a functional law admits other
/// than exactly one after-state for <see cref="Before"/>.</summary>
public readonly record struct Violation(int Before, int After, int Invariant);

/// <summary>The promises of a transition law. A law whose table breaks them is
/// not made.</summary>
public sealed class Accounting
{
    public Accounting(bool functional, params Invariant[] invariants)
    {
        ArgumentNullException.ThrowIfNull(invariants);
        Functional = functional;
        Invariants = invariants.ToArray();
    }

    /// <summary>Every before-state admits exactly one after-state, so the law
    /// can run on concrete state.</summary>
    public bool Functional { get; }

    public IReadOnlyList<Invariant> Invariants { get; }

    public static Accounting None { get; } = new(false);

    /// <summary>The role is part of a preserved sum: what it gains or loses is
    /// accounted against other roles.</summary>
    public bool Conserves(int role) => Invariants.OfType<PreservedSum>().Any(sum => sum.Roles.Contains(role));

    internal IReadOnlyList<Violation> Check(Law law, int states, Func<int, int, bool> admits)
    {
        var violations = new List<Violation>();
        for (var before = 0; before < states; before++)
        {
            var afterCount = 0;
            var b = law.Tuple(before);
            for (var after = 0; after < states; after++)
            {
                if (!admits(before, after)) continue;
                afterCount++;
                var a = law.Tuple(after);
                for (var i = 0; i < Invariants.Count; i++)
                    if (!Invariants[i].Holds(b, a)) violations.Add(new Violation(before, after, i));
            }
            if (Functional && afterCount != 1) violations.Add(new Violation(before, -1, -1));
        }
        return violations;
    }
}
