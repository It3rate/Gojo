using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class PatternAestheticProfileTests
{
    [Fact]
    public void DeclaredCriteriaKeepActualWitnessesBeforeDiagnosticCounts()
    {
        var low = new Placeholder(); var middle = new Placeholder(); var high = new Placeholder();
        var feature = new Relation(new Placeholder(), new Placeholder());
        var first = Criterion(feature, S(low, middle, high), S(middle, high), S(high));
        var second = Criterion(feature, S(low, middle, high), S(middle, high), S(high));
        var third = Criterion(feature, S(low, middle, high), S(middle, high), S(high));
        var witness = new Relation(feature, new Relation(low, high));
        var a = Observation(first, witness, S(high)); var b = Observation(second, witness, S(middle));
        var c = Observation(third, witness, S(low)); var profile = new PatternAestheticProfile(S(first, second, third));
        var read = profile.Evaluate(S(a, b, c));
        Assert.Equal(new PatternAestheticProfile.Summary(1, 1, 1, 0, true, false), profile.Describe(read));
        Assert.True(profile.TryReadResult(read, out var preferred, out var passed, out var violations, out var unknown));
        Assert.Same(a, Assert.Single(E(Assert.IsType<Relation>(Assert.Single(E(preferred))).Right)));
        Assert.Same(b, Assert.Single(E(Assert.IsType<Relation>(Assert.Single(E(passed))).Right)));
        Assert.Same(c, Assert.Single(E(Assert.IsType<Relation>(Assert.Single(E(violations))).Right)));
        Assert.Empty(E(unknown)); Assert.Contains(Traverse(read), value => ReferenceEquals(value, witness));
        Assert.DoesNotContain(Traverse(read), value => value is Carrier or CarrierLocation);
    }

    [Fact]
    public void MissingOpenEmptyForeignOrMixedSupportCannotBecomeFavorableZero()
    {
        var rejected = new Placeholder(); var accepted = new Placeholder(); var preferred = new Placeholder();
        var rule = Criterion(new Placeholder(), S(rejected, accepted, preferred), S(accepted, preferred), S(preferred));
        var profile = new PatternAestheticProfile(S(rule)); var witness = new Placeholder();
        foreach (var observations in new[] {
            S(), S(Observation(rule, witness, O(preferred))), S(Observation(rule, witness, S())),
            S(Observation(rule, witness, S(new Placeholder()))), S(Observation(rule, witness, S(rejected, preferred))),
            O(Observation(rule, witness, S(preferred))), S(Observation(rule, witness, S(preferred)), Observation(rule, witness, S(preferred))) })
        {
            var summary = profile.Describe(profile.Evaluate(observations));
            Assert.False(summary.Complete); Assert.False(summary.Acceptable); Assert.True(summary.Unknown > 0);
            Assert.Equal(0, summary.Preferred); Assert.Equal(0, summary.Passed); Assert.Equal(0, summary.Violations);
        }
    }

    [Fact]
    public void ACoarseSupportCanSettleAcceptabilityWithoutInventingItsExactOutcome()
    {
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var rule = Criterion(new Placeholder(), S(a, b, c), S(a, b, c), S(b, c));
        var profile = new PatternAestheticProfile(S(rule)); var witness = new Placeholder();
        var preferred = profile.Describe(profile.Evaluate(S(Observation(rule, witness, S(b, c)))));
        Assert.Equal(new PatternAestheticProfile.Summary(1, 0, 0, 0, true, true), preferred);
        // Both outcomes are admitted. Only one is preferred, so preference is
        // unproved while the coarser acceptable verdict is already settled.
        Assert.Equal(new PatternAestheticProfile.Summary(0, 1, 0, 0, true, true),
            profile.Describe(profile.Evaluate(S(Observation(rule, witness, S(a, b))))));
    }

    [Fact]
    public void KnownRejectionAndUnavailableObservationRemainDifferentReads()
    {
        var no = new Placeholder(); var yes = new Placeholder();
        var rule = Criterion(new Placeholder(), S(no, yes), S(yes), S(yes)); var profile = new PatternAestheticProfile(S(rule));
        Assert.Equal(new PatternAestheticProfile.Summary(0, 0, 1, 0, true, false),
            profile.Describe(profile.Evaluate(S(Observation(rule, new Placeholder(), S(no))))));
        Assert.Equal(new PatternAestheticProfile.Summary(0, 0, 0, 1, false, false), profile.Describe(profile.Evaluate(S())));
    }

    [Fact]
    public void IntermediatePreferenceIsAuthoredRatherThanAMaximumOrdinalLaw()
    {
        var low = new Placeholder(); var middle = new Placeholder(); var high = new Placeholder();
        var rule = Criterion(new Placeholder(), S(low, middle, high), S(low, middle, high), S(middle));
        var profile = new PatternAestheticProfile(S(rule));
        Assert.Equal(1, profile.Describe(profile.Evaluate(S(Observation(rule, new Placeholder(), S(middle))))).Preferred);
        Assert.Equal(1, profile.Describe(profile.Evaluate(S(Observation(rule, new Placeholder(), S(high))))).Passed);
        Assert.True(PatternAestheticProfile.TryReadCriterion(rule, out _, out var domain, out _, out _));
        Assert.Equal(new IElement[] { low, middle, high }, E(domain));
    }

    [Fact]
    public void SymmetryOrReuseAloneDoesNotDischargeContrastAndVariationCriteria()
    {
        // Supplied evidence is an explicit observer contract. The profile does
        // not infer that a uniform line/field is ugly or that 50% fill is ideal.
        var present = new Placeholder(); var absent = new Placeholder(); var domain = S(absent, present);
        var reuseSurface = new Relation(new Placeholder(), new Placeholder());
        var contrastSurface = new Relation(new Placeholder(), new Placeholder());
        var variationSurface = new Relation(new Placeholder(), new Placeholder());
        var reuse = Criterion(reuseSurface, domain, S(present), S(present));
        var contrast = Criterion(contrastSurface, domain, S(present), S(present));
        var variation = Criterion(variationSurface, domain, S(present), S(present));
        var profile = new PatternAestheticProfile(S(reuse, contrast, variation));
        var constantLine = S(Observation(reuse, reuseSurface, S(present)), Observation(contrast, contrastSurface, S(present)),
            Observation(variation, variationSurface, S(absent)));
        var allFilled = S(Observation(reuse, reuseSurface, S(present)), Observation(contrast, contrastSurface, S(absent)),
            Observation(variation, variationSurface, S(absent)));
        Assert.False(profile.Describe(profile.Evaluate(constantLine)).Acceptable);
        Assert.Equal(2, profile.Describe(profile.Evaluate(allFilled)).Violations);
    }

    [Fact]
    public void MalformedOrOpenProfileAndForeignObservationStayExplicitlyUnknown()
    {
        var a = new Placeholder(); var b = new Placeholder(); var feature = new Placeholder();
        var invalid = Criterion(feature, S(a, b), S(a), S(b));
        var valid = Criterion(feature, S(a, b), S(a, b), S(b));
        foreach (var criteria in new[] { S(), O(valid), S(invalid), S(valid, valid) })
        {
            var profile = new PatternAestheticProfile(criteria);
            Assert.False(profile.Describe(profile.Evaluate(S(Observation(valid, feature, S(b))))).Complete);
        }
        var ordinary = new PatternAestheticProfile(S(valid));
        var foreign = Criterion(feature, S(a, b), S(a, b), S(b));
        Assert.False(ordinary.Describe(ordinary.Evaluate(S(Observation(valid, feature, S(b)), Observation(foreign, feature, S(b))))).Complete);
    }

    [Fact]
    public void ReceiptReaderRejectsOmittedMovedAndClonedActualEvidence()
    {
        var yes = new Placeholder(); var rule = Criterion(new Placeholder(), S(yes), S(yes), S(yes));
        var observation = Observation(rule, new Placeholder(), S(yes)); var profile = new PatternAestheticProfile(S(rule));
        var read = profile.Evaluate(S(observation));
        Assert.True(profile.TryReadResult(read, out var preferred, out var passed, out var violations, out var unknown));
        var omitted = new Relation(read.Left, new Relation(S(), new Relation(passed, new Relation(violations, unknown))));
        var moved = new Relation(read.Left, new Relation(S(), new Relation(preferred, new Relation(violations, unknown))));
        var clone = new Relation(observation.Left, observation.Right);
        var invented = new Relation(read.Left, new Relation(S(new Relation(rule, S(clone))), new Relation(passed, new Relation(violations, unknown))));
        Assert.False(profile.TryReadResult(omitted, out _, out _, out _, out _));
        Assert.False(profile.TryReadResult(moved, out _, out _, out _, out _));
        Assert.False(profile.TryReadResult(invented, out _, out _, out _, out _));
        var wrappers = new Relation(read.Left, new Relation(S(new Relation(rule, S(observation))), new Relation(passed, new Relation(violations, unknown))));
        Assert.True(profile.TryReadResult(wrappers, out _, out _, out _, out _));
    }

    private static Relation Criterion(IElement feature, Relation domain, Relation acceptable, Relation preferred) =>
        PatternAestheticProfile.Criterion(feature, domain, acceptable, preferred);
    private static Relation Observation(Relation criterion, IElement witness, Relation outcomes) =>
        PatternAestheticProfile.Observation(criterion, witness, outcomes);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static Relation O(params IElement[] values) => StructuralConstraint.Open(values);
    private static IElement[] E(IElement scope) => FiniteLaw.Entries(Assert.IsType<Relation>(scope));
    private static IEnumerable<IElement> Traverse(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(source);
        while (pending.TryPop(out var value))
        { if (!seen.Add(value)) continue; yield return value; if (value is Relation row) { pending.Push(row.Left); pending.Push(row.Right); } }
    }
}
