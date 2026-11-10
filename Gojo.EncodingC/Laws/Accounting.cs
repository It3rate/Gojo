namespace Gojo.EncodingC;

/// <summary>One promise a transition law makes about every admitted
/// (before, after) pair, over role numbers. Checked exhaustively when the law
/// is made.</summary>
public abstract record Invariant
{
    public abstract bool Holds(int[] before, int[] after);
}

/// <summary>Σ weightᵣ · valueᵣ is the same before and after. Weights default to
/// one. With every weight positive this is conservation: a total of amounts,
/// so what one role gains another must lose, and nothing is made or lost, only
/// moved. Unequal positive weights state a rate, such as two units taken for
/// one given, which is how a calibration is written when one is wanted. A zero
/// or negative weight still makes a promise (a preserved difference, say) but
/// not conservation: two roles can then grow together, or one can change
/// without anything paying for it.</summary>
public sealed record PreservedSum : Invariant
{
    public PreservedSum(int[] roles, int[]? weights = null)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Length == 0) throw new ArgumentException("Name at least one role.", nameof(roles));
        weights ??= Enumerable.Repeat(1, roles.Length).ToArray();
        if (weights.Length != roles.Length) throw new ArgumentException("One weight per role.", nameof(weights));
        Roles = Array.AsReadOnly(roles.ToArray());
        Weights = Array.AsReadOnly(weights.ToArray());
    }

    public IReadOnlyList<int> Roles { get; }

    public IReadOnlyList<int> Weights { get; }

    /// <summary>Every weight is positive, so the sum is a total that nothing
    /// can enter or leave without another role paying for it.</summary>
    public bool Conserves => Weights.All(weight => weight > 0);

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
        Invariants = Array.AsReadOnly(invariants.ToArray());
    }

    /// <summary>Every before-state admits exactly one after-state, so the law
    /// can run on concrete state.</summary>
    public bool Functional { get; }

    public IReadOnlyList<Invariant> Invariants { get; }

    public static Accounting None { get; } = new(false);

    /// <summary>The role is part of a preserved sum of positive amounts: what it
    /// gains or loses is paid for by other roles. A sum with a zero or negative
    /// weight does not count.</summary>
    public bool Conserves(int role) => Invariants.OfType<PreservedSum>().Any(sum => sum.Conserves && sum.Roles.Contains(role));

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
