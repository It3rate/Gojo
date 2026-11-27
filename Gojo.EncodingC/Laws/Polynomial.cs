using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>A relation written as numbers: Σ weight · Π value^exponent = 0 over
/// a relation's roles (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §3.2). Each
/// term is an integer weight and one exponent per role. A balance is the
/// polynomial whose terms are single roles at exponent 1, its weights the
/// coefficients; a product a·b = c is the term ab against the term c; a carry
/// between rungs is a balance whose weight is the radix.
/// <para>It is the compressed form of a table: a few small integers that
/// rebuild the table exactly over its declared roles, and the form a fit
/// recovers from witnessed tuples. Because every term is numbers, an equation
/// can be perturbed, compared and searched without naming what it is. It is
/// stored in canonical form: equal monomials merged, zero terms dropped, the
/// weights divided by their common factor and signed so the first term is
/// positive. Its text form ("a + 2b − c = 0") is a read for people, made by
/// <see cref="Lens.Describe(Polynomial)"/>.</para></summary>
public sealed class Polynomial
{
    private readonly (int Weight, int[] Exponents)[] terms;
    private RelationLaw? relation;

    public Polynomial(IReadOnlyList<Shape> roles, params (int Weight, int[] Exponents)[] terms)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(terms);
        if (roles.Count == 0) throw new ArgumentException("A relation has at least one role.", nameof(roles));
        if (terms.Any(t => t.Exponents is null || t.Exponents.Length != roles.Count || t.Exponents.Any(e => e < 0)))
            throw new ArgumentException("Each term has one non-negative exponent per role.", nameof(terms));
        Roles = Array.AsReadOnly(roles.ToArray());
        var merged = new List<(BigInteger Weight, int[] Exponents)>();
        foreach (var (weight, exponents) in terms.OrderByDescending(t => t.Exponents, Comparer<int[]>.Create(Lexical)))
        {
            if (merged.Count > 0 && merged[^1].Exponents.AsSpan().SequenceEqual(exponents))
                merged[^1] = (merged[^1].Weight + weight, exponents);
            else merged.Add((weight, exponents));
        }
        merged.RemoveAll(t => t.Weight.IsZero);
        if (merged.Count == 0) throw new ArgumentException("Every term cancelled: nothing is related.", nameof(terms));
        var common = merged.Aggregate(BigInteger.Zero, (g, t) => BigInteger.GreatestCommonDivisor(g, t.Weight));
        if (merged[0].Weight.Sign < 0) common = -common;
        this.terms = [.. merged.Select(t => (checked((int)(t.Weight / common)), t.Exponents.ToArray()))];
    }

    public IReadOnlyList<Shape> Roles { get; }

    /// <summary>The terms in canonical order, each a weight and an exponent per role.</summary>
    public IReadOnlyList<(int Weight, IReadOnlyList<int> Exponents)> Terms =>
        [.. terms.Select(t => (t.Weight, (IReadOnlyList<int>)Array.AsReadOnly(t.Exponents)))];

    /// <summary>Σ weight · value: a term per role at exponent 1.</summary>
    public static Polynomial Balance(IReadOnlyList<Shape> roles, params int[] weights)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != roles.Count) throw new ArgumentException("One weight per role.", nameof(weights));
        if (weights.Any(w => w == 0)) throw new ArgumentException("A role of weight zero is on neither pan.", nameof(weights));
        return new(roles, [.. weights.Select((w, r) => (w, Enumerable.Range(0, roles.Count).Select(i => i == r ? 1 : 0).ToArray()))]);
    }

    /// <summary>a · b = c, with c of capacity a · b. Roles: a, b, c.</summary>
    public static Polynomial Product(int capacityA, int capacityB) =>
        new([Shape.Unary(capacityA), Shape.Unary(capacityB), Shape.Unary(checked(capacityA * capacityB))], (1, [1, 1, 0]), (-1, [0, 0, 1]));

    /// <summary>The highest total exponent of any term.</summary>
    public int Degree => terms.Max(t => t.Exponents.Sum());

    /// <summary>The exact left side at these role values; the relation holds
    /// where it is 0. Exact at any size, so a large power never wraps.</summary>
    public BigInteger Value(IReadOnlyList<int> values) => ExactValue(values);

    private BigInteger ExactValue(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var sum = BigInteger.Zero;
        foreach (var (weight, exponents) in terms)
            sum += weight * EvaluateMonomial(values, exponents);
        return sum;
    }

    internal static BigInteger EvaluateMonomial(IReadOnlyList<int> values, IReadOnlyList<int> exponents)
    {
        var term = BigInteger.One;
        for (var r = 0; r < exponents.Count && !term.IsZero; r++)
        {
            var exponent = exponents[r];
            if (exponent == 1) term *= values[r];
            else if (exponent > 1) term *= BigInteger.Pow(values[r], exponent);
        }
        return term;
    }

    public bool Holds(IReadOnlyList<int> values) => ExactValue(values).IsZero;

    /// <summary>The table of every tuple of role values that satisfies it.</summary>
    public RelationLaw Relation => relation ??= RelationLaw.FromPredicate(Roles, values => Holds(values));

    /// <summary>A size for comparing descriptions: per term, a sign, the
    /// weight's bits, and two bits of exponent per role. An estimate, used to
    /// ask whether an equation is shorter than the table it rebuilds.</summary>
    public int Bits => terms.Sum(t => 1 + BitLength(t.Weight < 0 ? -(long)t.Weight : t.Weight) + 2 * t.Exponents.Length);

    /// <summary>The same equation over the same role shapes.</summary>
    public bool Same(Polynomial other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Roles.SequenceEqual(other.Roles) && terms.Length == other.terms.Length &&
            terms.Zip(other.terms).All(p => p.First.Weight == p.Second.Weight && p.First.Exponents.SequenceEqual(p.Second.Exponents));
    }

    private static int Lexical(int[] x, int[] y)
    {
        for (var i = 0; i < x.Length; i++)
            if (x[i] != y[i]) return x[i].CompareTo(y[i]);
        return 0;
    }

    private static int BitLength(long value)
    {
        var bits = 0;
        while (value > 0) { bits++; value >>= 1; }
        return bits;
    }
}
