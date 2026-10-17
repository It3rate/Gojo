using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A declared local preference over supplied observation supports.
/// This reads conformance to an authored profile, not human beauty or the
/// factual validity of another observer's evidence. No unavailable outcome is
/// assigned a favorable score. Native criteria and evidence precede counts.</summary>
public sealed class PatternAestheticProfile
{
    private static readonly IEqualityComparer<IElement> Identity = ReferenceEqualityComparer.Instance;
    // criterion = (actual-feature-surface -> ordered-outcome-domain) ->
    //             (acceptable-support -> preferred-support)
    // observation = (actual-criterion -> actual-feature-witness) -> outcome-support
    // read = (actual-criteria -> observations) ->
    //        (preferred -> (passed -> (violations -> unknown)))
    // Each result entry = actual-subject -> Closed(actual supporting inputs).
    // Outcome order is a declared observer basis, not a universal aesthetic rank.
    // "Passed" proves acceptability; it does not prove non-preference.
    public Relation Criteria { get; }
    public Relation Surface => Criteria;
    public sealed record Summary(int Preferred, int Passed, int Violations, int Unknown,
        bool Complete, bool Acceptable);

    public PatternAestheticProfile(Relation criteria) => Criteria = criteria;
    public static Relation Criterion(IElement featureSurface, Relation orderedOutcomes,
        Relation acceptable, Relation preferred) =>
        new(new Relation(featureSurface, orderedOutcomes), new Relation(acceptable, preferred));
    public static Relation Observation(Relation criterion, IElement featureWitness, Relation outcomes) =>
        new(new Relation(criterion, featureWitness), outcomes);

    public Relation Evaluate(Relation observations)
    {
        var preferred = new List<IElement>(); var passed = new List<IElement>();
        var violations = new List<IElement>(); var unknown = new List<IElement>();
        var criteria = E(Criteria); var offered = E(observations);
        var closedObservations = Closed(observations);
        if (!Closed(Criteria) || criteria.Length == 0) unknown.Add(Evidence(Criteria, Criteria));
        if (!closedObservations) unknown.Add(Evidence(observations, observations));
        foreach (var item in offered)
            if (item is not Relation { Left: Relation context, Right: Relation } ||
                !criteria.Any(criterion => ReferenceEquals(criterion, context.Left)))
                unknown.Add(Evidence(item, item));
        foreach (var criterion in criteria.Distinct(Identity))
        {
            var rows = offered.OfType<Relation>().Where(row => row.Left is Relation context &&
                ReferenceEquals(context.Left, criterion)).ToArray();
            var evidence = Evidence(criterion, rows.Cast<IElement>().ToArray());
            if (criteria.Count(actual => ReferenceEquals(actual, criterion)) != 1 ||
                criterion is not Relation rule || !ReadCriterion(rule, out var domain, out var allowed, out var desired) ||
                !closedObservations || rows.Length != 1 || rows[0].Right is not Relation support ||
                !Closed(support) || E(support).Length == 0 ||
                E(support).Any(outcome => !Contains(domain, outcome)))
            { unknown.Add(evidence); continue; }
            var outcomes = E(support);
            if (outcomes.All(outcome => Contains(desired, outcome))) preferred.Add(evidence);
            else if (outcomes.All(outcome => Contains(allowed, outcome))) passed.Add(evidence);
            else if (outcomes.All(outcome => !Contains(allowed, outcome))) violations.Add(evidence);
            else unknown.Add(evidence);
        }
        return new(new Relation(Surface, observations), new Relation(S(preferred.ToArray()),
            new Relation(S(passed.ToArray()), new Relation(S(violations.ToArray()), S(unknown.ToArray())))));
    }

    /// <summary>Rechecks the result against the same actual input rows. Derived
    /// wrappers may be rebuilt, but borrowed, omitted or invented evidence fails.</summary>
    public bool TryReadResult(Relation read, out Relation preferred, out Relation passed,
        out Relation violations, out Relation unknown)
    {
        preferred = passed = violations = unknown = null!;
        if (read.Left is not Relation request || !ReferenceEquals(request.Left, Surface) ||
            request.Right is not Relation observations || !Buckets(read, out var a, out var b, out var c, out var d) ||
            !Buckets(Evaluate(observations), out var freshA, out var freshB, out var freshC, out var freshD) ||
            !SameBucket(a, freshA) || !SameBucket(b, freshB) || !SameBucket(c, freshC) || !SameBucket(d, freshD)) return false;
        preferred = a; passed = b; violations = c; unknown = d; return true;
    }

    public Summary Describe(Relation read)
    {
        if (!TryReadResult(read, out var preferred, out var passed, out var violations, out var unknown))
            throw new ArgumentException("Expected this profile's complete evidence grammar.", nameof(read));
        var complete = E(unknown).Length == 0;
        return new(E(preferred).Length, E(passed).Length, E(violations).Length, E(unknown).Length,
            complete, complete && E(violations).Length == 0);
    }

    public static bool TryReadCriterion(Relation criterion, out IElement featureSurface,
        out Relation orderedOutcomes, out Relation acceptable, out Relation preferred)
    {
        featureSurface = null!; orderedOutcomes = acceptable = preferred = null!;
        if (!ReadCriterion(criterion, out var domain, out var admitted, out var desired) ||
            criterion.Left is not Relation feature) return false;
        featureSurface = feature.Left; orderedOutcomes = domain; acceptable = admitted; preferred = desired; return true;
    }

    private static bool ReadCriterion(Relation criterion, out Relation domain, out Relation acceptable, out Relation preferred)
    {
        domain = acceptable = preferred = null!;
        if (criterion.Left is not Relation { Right: Relation outcomes } ||
            criterion.Right is not Relation { Left: Relation allowed, Right: Relation desired } ||
            !Closed(outcomes) || E(outcomes).Length == 0 || !Unique(E(outcomes)) ||
            !Closed(allowed) || !Unique(E(allowed)) || !Closed(desired) || !Unique(E(desired)) ||
            E(allowed).Any(outcome => !Contains(outcomes, outcome)) ||
            E(desired).Any(outcome => !Contains(allowed, outcome))) return false;
        domain = outcomes; acceptable = allowed; preferred = desired; return true;
    }

    private static bool Buckets(Relation read, out Relation preferred, out Relation passed,
        out Relation violations, out Relation unknown)
    {
        preferred = passed = violations = unknown = null!;
        if (read.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation b,
                Right: Relation { Left: Relation c, Right: Relation d } } } ||
            new[] { a, b, c, d }.Any(scope => !Closed(scope))) return false;
        preferred = a; passed = b; violations = c; unknown = d; return true;
    }
    private static bool SameBucket(Relation actual, Relation expected)
    {
        var a = E(actual); var b = E(expected);
        return a.Length == b.Length && a.Zip(b).All(pair => pair.First is Relation x && pair.Second is Relation y &&
            ReferenceEquals(x.Left, y.Left) && x.Right is Relation xs && y.Right is Relation ys && Closed(xs) &&
            E(xs).Length == E(ys).Length && E(xs).Zip(E(ys)).All(row => ReferenceEquals(row.First, row.Second)));
    }
    private static bool Unique(IElement[] values) => !values.Where((value, index) => values.Take(index).Any(other => Same(value, other))).Any();
    private static bool Contains(Relation scope, IElement value) => E(scope).Any(actual => Same(actual, value));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q && p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);
    private static Relation Evidence(IElement subject, params IElement[] inputs) => new(subject, S(inputs));
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
