namespace Gojo.EncodingC;

/// <summary>Live-denominator portion relations for scales whose partition count
/// itself sits in open supports (GojoOperationsSynthesis-2026-11.md, §7 item 5).
/// Fixed-partition arithmetic and units belong to <see cref="Numbers"/> and
/// <see cref="Ladder"/>; this helper covers only the live-denominator cases a
/// fixed-radix ladder cannot express:
/// <list type="bullet">
/// <item><b>Validity (<see cref="Valid"/>):</b> enforces <c>d ≥ 1</c> and
/// <c>n ≤ d</c>, rejecting zero-denominator or over-full operands such as
/// <c>(1, 0)</c> as an empty support contradiction.</item>
/// <item><b>Pooling (<see cref="WirePool"/>):</b>
/// <c>(n₁, d₁) ⊕ (n₂, d₂) = (n₁ + n₂, d₁ + d₂)</c> over valid portions,
/// preserving accumulated sample count without reducing <c>5/10</c> to
/// <c>1/2</c>. Because each balance narrows its own roles locally, separate
/// open supports are conservative (use a <see cref="Fork"/> to rule out
/// jointly impossible combinations).</item>
/// <item><b>Same-whole addition (<see cref="AddSameWhole"/>):</b>
/// <c>n₁ + n₂ = whole · d + rem</c> with <c>whole ∈ {0, 1, 2}</c>, so two full
/// portions (<c>5/5 + 5/5</c>) keep both wholes (<c>whole = 2, rem = 0</c>)
/// while incompatible denominators contradict.</item>
/// <item><b>Live re-partition projection (<see cref="Project"/>):</b>
/// <c>n · d₂ = q · d₁ + r</c> (<c>0 ≤ r &lt; d₁</c>), keeping exact residue
/// <c>r</c> (<c>r / d₂</c> source ticks, or <c>r / (d₁ · d₂)</c> of the whole)
/// visible instead of rounding.</item>
/// <item><b>Cross-multiplication comparison (<see cref="Compare"/>):</b>
/// compares <c>(n₁, d₁)</c> against <c>(n₂, d₂)</c> without floats or binary
/// approximation.</item>
/// </list>
/// Because the five-role tables grow as <c>(maxDenominator + 1)⁵</c>,
/// <see cref="Project"/>, <see cref="AddSameWhole"/>, and <see cref="Compare"/>
/// stay within <see cref="Law.MaxBits"/> only for small denominators (up to
/// about 20).</summary>
public static class Portions
{
    private static readonly Dictionary<(Shape, Shape), RelationLaw> Valids = [];
    private static readonly Dictionary<(int, bool), RelationLaw> SameWholeAdds = [];
    private static readonly Dictionary<(int, bool), RelationLaw> Projections = [];
    private static readonly Dictionary<(int, bool), RelationLaw> Comparisons = [];

    private static Shape CountShape(int max, bool binary) =>
        binary ? Shape.Binary(max + 1) : Shape.Unary(max);

    /// <summary>Live-denominator portion validity: <c>d ≥ 1</c> and
    /// <c>n ≤ d</c>. Roles: numerator, denominator. Zero denominator (including
    /// <c>(1, 0)</c> and <c>(0, 0)</c>) and <c>n &gt; d</c> are excluded.</summary>
    public static RelationLaw Valid(int maxDenominator, bool binary = false) =>
        Valid(CountShape(maxDenominator, binary), CountShape(maxDenominator, binary));

    private static RelationLaw Valid(Shape numerator, Shape denominator)
    {
        if (denominator.Size < 2) throw new ArgumentOutOfRangeException(nameof(denominator), "A denominator needs room for at least 1.");
        var key = (numerator, denominator);
        lock (Valids)
        {
            if (Valids.TryGetValue(key, out var cached)) return cached;
            var law = RelationLaw.FromPredicate([numerator, denominator], t => t[1] >= 1 && t[0] <= t[1]);
            Valids[key] = law;
            return law;
        }
    }

    /// <summary>Same-whole addition over a live shared denominator:
    /// <c>n₁ + n₂ = whole · d + rem</c> with <c>d ≥ 1</c>, <c>n₁ ≤ d</c>,
    /// <c>n₂ ≤ d</c>, <c>rem &lt; d</c>. Roles: n1, n2, d, whole (0..2), rem.
    /// Because <c>whole</c> holds 0, 1 or 2, <c>5/5 + 5/5</c> settles cleanly
    /// to <c>(whole: 2, rem: 0)</c>.</summary>
    public static RelationLaw AddSameWhole(int maxDenominator, bool binary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (SameWholeAdds)
        {
            if (SameWholeAdds.TryGetValue((maxDenominator, binary), out var cached)) return cached;
            var s = CountShape(maxDenominator, binary);
            var whole = CountShape(2, binary);
            return SameWholeAdds[(maxDenominator, binary)] = RelationLaw.FromPredicate(
                [s, s, s, whole, s],
                t => t[2] >= 1 && t[0] <= t[2] && t[1] <= t[2] && t[4] < t[2] && t[0] + t[1] == t[3] * t[2] + t[4]);
        }
    }

    /// <summary>Live-denominator re-partition projection:
    /// <c>n · d₂ = q · d₁ + r</c> with <c>d₁ ≥ 1</c>, <c>d₂ ≥ 1</c>,
    /// <c>n ≤ d₁</c>, <c>q ≤ d₂</c>, <c>0 ≤ r &lt; d₁</c>. Roles: n, d1, d2, q, r.
    /// The stored integer remainder <c>r</c> is <c>r / d₂</c> source ticks
    /// (<c>r / (d₁ · d₂)</c> of the whole), rejecting zero denominators.</summary>
    public static RelationLaw Project(int maxDenominator, bool binary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (Projections)
        {
            if (Projections.TryGetValue((maxDenominator, binary), out var cached)) return cached;
            var s = CountShape(maxDenominator, binary);
            return Projections[(maxDenominator, binary)] = RelationLaw.FromPredicate(
                [s, s, s, s, s],
                t => t[1] >= 1 && t[2] >= 1 && t[0] <= t[1] && t[3] <= t[2] && t[4] < t[1] && t[0] * t[2] == t[3] * t[1] + t[4]);
        }
    }

    /// <summary>Exact cross-multiplication comparison across arbitrary live
    /// denominators without floats. Roles: n1, d1, n2, d2, order (<c>0</c> when
    /// <c>n₁/d₁ &lt; n₂/d₂</c>, <c>1</c> when equal in share, <c>2</c> when
    /// greater). Denominators remain distinct.</summary>
    public static RelationLaw Compare(int maxDenominator, bool binary = false)
    {
        if (maxDenominator < 1) throw new ArgumentOutOfRangeException(nameof(maxDenominator));
        lock (Comparisons)
        {
            if (Comparisons.TryGetValue((maxDenominator, binary), out var cached)) return cached;
            var s = CountShape(maxDenominator, binary);
            var order = CountShape(2, binary);
            return Comparisons[(maxDenominator, binary)] = RelationLaw.FromPredicate(
                [s, s, s, s, order],
                t =>
                {
                    if (t[1] < 1 || t[3] < 1 || t[0] > t[1] || t[2] > t[3]) return false;
                    var lhs = t[0] * t[3];
                    var rhs = t[2] * t[1];
                    var expected = lhs < rhs ? 0 : lhs == rhs ? 1 : 2;
                    return t[4] == expected;
                });
        }
    }

    /// <summary>Wires guarded evidence pooling
    /// <c>(n₁, d₁) ⊕ (n₂, d₂) = (n₁ + n₂, d₁ + d₂)</c> at
    /// <paramref name="positions"/>: two <see cref="Valid"/> relations guard
    /// both input portions (refusing <c>d = 0</c> or <c>n &gt; d</c>, such as
    /// <c>(1, 0)</c>), and two linear balances sum the numerators and
    /// denominators without reducing the pooled share. Scope disjointness and
    /// basis alignment come from the caller, and separate open supports remain
    /// conservative under local settlement.</summary>
    public static IReadOnlyList<Wiring> WirePool(
        Supports n1, Supports d1, Supports n2, Supports d2, Supports nOut, Supports dOut, Plane positions)
    {
        ArgumentNullException.ThrowIfNull(n1);
        ArgumentNullException.ThrowIfNull(d1);
        ArgumentNullException.ThrowIfNull(n2);
        ArgumentNullException.ThrowIfNull(d2);
        ArgumentNullException.ThrowIfNull(nOut);
        ArgumentNullException.ThrowIfNull(dOut);
        ArgumentNullException.ThrowIfNull(positions);
        var v1 = new Wiring(Valid(n1.Field.Shape, d1.Field.Shape), n1.Variable, d1.Variable);
        var v2 = new Wiring(Valid(n2.Field.Shape, d2.Field.Shape), n2.Variable, d2.Variable);
        var numW = new Wiring(Operations.Balance([n1.Field.Shape, n2.Field.Shape, nOut.Field.Shape], 1, 1, -1),
            n1.Variable, n2.Variable, nOut.Variable);
        var denW = new Wiring(Operations.Balance([d1.Field.Shape, d2.Field.Shape, dOut.Field.Shape], 1, 1, -1),
            d1.Variable, d2.Variable, dOut.Variable);
        foreach (var pos in positions.Indexes())
        {
            v1.Add(pos, pos);
            v2.Add(pos, pos);
            numW.Add(pos, pos, pos);
            denW.Add(pos, pos, pos);
        }
        return [v1, v2, numW, denW];
    }
}
