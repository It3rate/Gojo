namespace Gojo.EncodingC;

/// <summary>One integer monomial term of a <see cref="Polynomial"/>:
/// <c>Weight · Π value[r]^Exponents[r]</c>. Stored purely as integers —
/// no names, no operator enums, no real numbers.</summary>
public readonly record struct Term(int Weight, IReadOnlyList<int> Exponents);

/// <summary>An equation over role shapes as canonical numbers:
/// <c>Σ weight · Π value[r]^exponent = 0</c>. Terms are combined by exponent
/// vector, ordered lexically, reduced by their greatest common divisor, and
/// oriented so the leading term is positive.
/// <para>Balances (<c>a + w·b − c = 0</c>), partition columns
/// (<c>a + b + c_in − p·c_out − s = 0</c>), products (<c>a·b − c = 0</c>),
/// fused digit multiply-adds (<c>a·b + c_in − p·c_out − d = 0</c>), and
/// Axiom-8 projections (<c>n·d₂ − q·d₁ − r = 0</c>) are all one family.
/// Human-readable text belongs strictly to <see cref="Lens.Describe(Polynomial)"/>.</para></summary>
public sealed class Polynomial
{
    private readonly (int Weight, int[] Exponents)[] terms;
    private RelationLaw? relation;

    public Polynomial(IReadOnlyList<Shape> roles, params (int Weight, int[] Exponents)[] terms)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(terms);
        if (roles.Count == 0) throw new ArgumentException("A polynomial has at least one role.", nameof(roles));
        Roles = Array.AsReadOnly(roles.ToArray());

        var combined = new Dictionary<long, (int Weight, int[] Exponents)>();
        foreach (var (weight, exponents) in terms)
        {
            ArgumentNullException.ThrowIfNull(exponents);
            if (exponents.Length != roles.Count)
                throw new ArgumentException($"Each term needs {roles.Count} exponents.", nameof(terms));
            if (exponents.Any(e => e < 0 || e > 63))
                throw new ArgumentOutOfRangeException(nameof(terms), "Exponents stay in 0..63.");
            if (weight == 0) continue;
            var key = Key(exponents);
            combined[key] = combined.TryGetValue(key, out var existing)
                ? (checked(existing.Weight + weight), existing.Exponents)
                : (weight, (int[])exponents.Clone());
        }

        var ordered = combined.Values
            .Where(t => t.Weight != 0)
            .OrderByDescending(t => t.Exponents.Sum())
            .ThenByDescending(t => t.Exponents, Comparer<int[]>.Create(Lexical))
            .ToArray();
        if (ordered.Length == 0)
            throw new ArgumentException("A polynomial needs at least one non-zero term.", nameof(terms));

        var gcd = ordered.Aggregate(0, (g, t) => Gcd(g, t.Weight < 0 ? -t.Weight : t.Weight));
        var sign = ordered[0].Weight < 0 ? -1 : 1;
        this.terms = ordered.Select(t => (sign * (t.Weight / gcd), t.Exponents)).ToArray();
        Terms = Array.AsReadOnly(this.terms.Select(t => new Term(t.Weight, Array.AsReadOnly(t.Exponents))).ToArray());
    }

    public IReadOnlyList<Shape> Roles { get; }

    public IReadOnlyList<Term> Terms { get; }

    public int Degree => terms.Max(t => t.Exponents.Sum());

    /// <summary>Linear balance: <c>Σ weights[r] · value[r] = 0</c>.</summary>
    public static Polynomial Balance(IReadOnlyList<Shape> roles, params int[] weights)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Length != roles.Count)
            throw new ArgumentException($"Need {roles.Count} weights, one per role.", nameof(weights));
        var items = new List<(int, int[])>(roles.Count);
        for (var r = 0; r < roles.Count; r++)
        {
            if (weights[r] == 0) continue;
            var e = new int[roles.Count];
            e[r] = 1;
            items.Add((weights[r], e));
        }
        return new Polynomial(roles, [.. items]);
    }

    /// <summary>Product: <c>a · b − c = 0</c> over unary counts of capacities
    /// <paramref name="capacityA"/>, <paramref name="capacityB"/>, and their product.</summary>
    public static Polynomial Product(int capacityA, int capacityB) =>
        Product(Shape.Unary(capacityA), Shape.Unary(capacityB), Shape.Unary(capacityA * capacityB));

    /// <summary>Product: <c>a · b − c = 0</c> over explicit role shapes.</summary>
    public static Polynomial Product(Shape left, Shape right, Shape product) =>
        new([left, right, product], (1, [1, 1, 0]), (-1, [0, 0, 1]));

    /// <summary>Evaluates <c>Σ weight · Π value[r]^exponent</c>.</summary>
    public long Value(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Count != Roles.Count) throw new ArgumentException($"Expected {Roles.Count} values.", nameof(values));
        long sum = 0;
        foreach (var (weight, exponents) in terms)
        {
            long term = weight;
            for (var r = 0; r < exponents.Length; r++)
                for (var e = 0; e < exponents[r]; e++) term *= values[r];
            sum += term;
        }
        return sum;
    }

    public bool Holds(IReadOnlyList<int> values) => Value(values) == 0;

    /// <summary>The table of every tuple of role values that satisfies this polynomial.</summary>
    public RelationLaw Relation => relation ??= RelationLaw.FromPredicate(Roles, values => Holds(values));

    /// <summary>Description-length estimate in bits: per term, 1 sign bit +
    /// weight magnitude bits + 2 exponent bits per role.</summary>
    public int Bits => terms.Sum(t => 1 + BitLength(t.Weight < 0 ? -t.Weight : t.Weight) + 2 * t.Exponents.Length);

    public bool Same(Polynomial other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Roles.SequenceEqual(other.Roles)
            && terms.Length == other.terms.Length
            && terms.Zip(other.terms).All(p => p.First.Weight == p.Second.Weight && p.First.Exponents.SequenceEqual(p.Second.Exponents));
    }

    private static long Key(int[] exponents) => exponents.Aggregate(0L, (k, e) => unchecked(k * 65 + e));

    private static int Lexical(int[] x, int[] y)
    {
        for (var i = 0; i < x.Length; i++)
            if (x[i] != y[i]) return x[i].CompareTo(y[i]);
        return 0;
    }

    private static int Gcd(int a, int b) => b == 0 ? a : Gcd(b, a % b);

    private static int BitLength(int value)
    {
        var bits = 0;
        while (value > 0) { bits++; value >>= 1; }
        return bits;
    }
}
