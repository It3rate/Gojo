using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FiniteLawConformanceTests
{
    [Fact]
    public void ExistingOrdinalReferenceCompositionMeetsItsSuppliedIdentityAndAssociativityCases()
    {
        var frame = new OrdinalReferenceFrame();
        var law = frame.CompositionLaw;
        IElement[] states = [frame.Lower, frame.Equal, frame.Higher];
        var obligations = new List<Relation>();
        foreach (var state in states)
        {
            obligations.Add(Eq(Op(law, S(frame.Equal), S(state)), S(state)));
            obligations.Add(Eq(Op(law, S(state), S(frame.Equal)), S(state)));
        }
        foreach (var a in states)
            foreach (var b in states)
                foreach (var c in states)
                    obligations.Add(Eq(Op(law, Op(law, S(a), S(b)), S(c)),
                        Op(law, S(a), Op(law, S(b), S(c)))));

        var claims = S(obligations.ToArray());
        var report = FiniteLawConformance.Check(law, claims);
        var (passed, violated, unresolved) = Read(report);

        Assert.Equal(33, passed.Length);
        Assert.Empty(violated);
        Assert.Empty(unresolved);
        Assert.True(FiniteLawConformance.TryReadReport(report, out var exposedLaw, out var exposedClaims,
            out _, out _, out _));
        Assert.Same(frame.CompositionLaw, exposedLaw);
        Assert.Same(claims, exposedClaims);
        AssertNoNumericParticipants(report);
    }

    [Fact]
    public void ReversingTheExistingStrictOrderRowViolatesTheDeclaredOrderObligation()
    {
        var frame = new OrdinalReferenceFrame();
        Assert.True(FiniteLaw.TryRead(frame.CompositionLaw, out var leftDomain, out var rightDomain,
            out var outputDomain, out var rows));
        var changedRow = FiniteLaw.Row(frame.Higher, frame.Higher, S(frame.Lower));
        var replaced = rows.Select(row => row.Left is Relation pair &&
            ReferenceEquals(pair.Left, frame.Higher) && ReferenceEquals(pair.Right, frame.Higher)
                ? changedRow : row).ToArray();
        var alteredLaw = FiniteLaw.Create(leftDomain, rightDomain, outputDomain, replaced);
        var strictOrder = Eq(Op(alteredLaw, S(frame.Higher), S(frame.Higher)), S(frame.Higher));

        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(alteredLaw, S(strictOrder)));

        Assert.Empty(passed);
        Assert.Empty(unresolved);
        var counterexample = Assert.IsType<Relation>(Assert.Single(violated));
        Assert.True(FiniteLawConformance.TryReadObligation(counterexample, out var original,
            out var actual, out var expected, out var proofs));
        Assert.Same(strictOrder, original);
        Assert.Same(frame.Lower, Assert.Single(FiniteLaw.Entries(actual)));
        Assert.Same(frame.Higher, Assert.Single(FiniteLaw.Entries(expected)));
        Assert.True(ContainsReference(proofs, changedRow));
        AssertNoNumericParticipants(counterexample);
    }

    [Fact]
    public void OrdinalSelectionMeetsBothIdentitiesAndAllSuppliedAssociativityCases()
    {
        var (law, low, middle, high) = OrdinalMaximum();
        IElement[] states = [low, middle, high];
        var obligations = new List<Relation>();
        foreach (var state in states)
        {
            obligations.Add(Eq(Op(law, S(low), S(state)), S(state)));
            obligations.Add(Eq(Op(law, S(state), S(low)), S(state)));
        }
        foreach (var a in states)
            foreach (var b in states)
                foreach (var c in states)
                    obligations.Add(Eq(Op(law, Op(law, S(a), S(b)), S(c)),
                        Op(law, S(a), Op(law, S(b), S(c)))));

        var sourceSites = Sites(law);
        var claims = StructuralConstraint.Closed(obligations.ToArray());
        var report = FiniteLawConformance.Check(law, claims);
        var (passed, violated, unresolved) = Read(report);

        Assert.Equal(33, passed.Length);
        Assert.Empty(violated);
        Assert.Empty(unresolved);
        Assert.True(FiniteLawConformance.TryReadReport(report, out var readLaw, out var readClaims, out _, out _, out _));
        Assert.Same(law, readLaw);
        Assert.Same(claims, readClaims);
        Assert.All(sourceSites, site =>
        {
            Assert.Same(site.Left, site.Site.Left);
            Assert.Same(site.Right, site.Site.Right);
        });
        AssertNoNumericParticipants(report);
    }

    [Fact]
    public void AlteredNativeRowProducesItsActualIdentityCounterexample()
    {
        var (law, low, middle, high) = OrdinalMaximum();
        var altered = FiniteLaw.Create(S(low, middle, high), S(low, middle, high), S(low, middle, high),
            FiniteLaw.Row(low, middle, S(high)));
        var obligation = Eq(Op(altered, S(low), S(middle)), S(middle));

        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(altered, S(obligation)));

        Assert.Empty(passed);
        Assert.Empty(unresolved);
        var counterexample = Assert.IsType<Relation>(Assert.Single(violated));
        Assert.True(FiniteLawConformance.TryReadObligation(counterexample, out var original,
            out var left, out var right, out var proofs));
        Assert.Same(obligation, original);
        Assert.Same(high, Assert.Single(FiniteLaw.Entries(left)));
        Assert.Same(middle, Assert.Single(FiniteLaw.Entries(right)));
        Assert.True(ContainsReference(proofs, altered));
        Assert.False(ContainsReference(counterexample, law));
    }

    [Fact]
    public void SaturatingCompositionHasItsOwnIdentityButFailsOrdinaryCancellationAndAssociativity()
    {
        var below = new Placeholder(); var origin = new Placeholder(); var above = new Placeholder();
        var domain = S(below, origin, above);
        var law = FiniteLaw.Create(domain, domain, domain,
            FiniteLaw.Row(below, below, S(below)), FiniteLaw.Row(below, origin, S(below)),
            FiniteLaw.Row(below, above, S(origin)), FiniteLaw.Row(origin, below, S(below)),
            FiniteLaw.Row(origin, origin, S(origin)), FiniteLaw.Row(origin, above, S(above)),
            FiniteLaw.Row(above, below, S(origin)), FiniteLaw.Row(above, origin, S(above)),
            FiniteLaw.Row(above, above, S(above)));
        var identity = Eq(Op(law, S(origin), S(above)), S(above));
        var saturation = Eq(Op(law, S(above), S(above)), S(above));
        var ordinaryCancellation = Eq(Op(law, Op(law, S(above), S(above)), S(below)), S(above));
        var ordinaryAssociativity = Eq(Op(law, Op(law, S(above), S(above)), S(below)),
            Op(law, S(above), Op(law, S(above), S(below))));

        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law,
            S(identity, saturation, ordinaryCancellation, ordinaryAssociativity)));

        Assert.Equal(2, passed.Length);
        Assert.Equal(2, violated.Length);
        Assert.Empty(unresolved);
        Assert.All(violated, entry =>
        {
            Assert.True(FiniteLawConformance.TryReadObligation(Assert.IsType<Relation>(entry), out _,
                out var actual, out var expected, out var proofs));
            Assert.Same(origin, Assert.Single(FiniteLaw.Entries(actual)));
            Assert.Same(above, Assert.Single(FiniteLaw.Entries(expected)));
            Assert.NotEmpty(FiniteLaw.Entries(proofs));
        });
    }

    [Fact]
    public void ExplicitWideningCasesRetainEveryAdmittedOutcome()
    {
        var (law, low, middle, high) = OrdinalMaximum();
        var narrow = Eq(Op(law, S(low), S(middle)), S(middle));
        var wider = Eq(Op(law, S(low, high), S(middle)), S(high, middle));
        var falseCollapsed = Eq(Op(law, S(low, high), S(middle)), S(high));

        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, S(narrow, wider, falseCollapsed)));

        Assert.Equal(2, passed.Length);
        Assert.Single(violated);
        Assert.Empty(unresolved);
    }

    [Fact]
    public void RepeatedSupportEntriesCompareAsIdentitySets()
    {
        var (law, low, middle, high) = OrdinalMaximum();
        var obligation = Eq(Op(law, S(low, high), S(middle)), S(high, middle, high));
        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, S(obligation)));
        Assert.Single(passed);
        Assert.Empty(violated);
        Assert.Empty(unresolved);
    }

    [Fact]
    public void MissingOneOfSeveralRequestedPairsCannotHideBehindKnownOutcomes()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var law = FiniteLaw.Create(S(a, b), S(a, b), S(a, b), FiniteLaw.Row(a, a, S(a)));
        var obligation = Eq(Op(law, S(a, b), S(a)), S(a));

        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, S(obligation)));

        Assert.Empty(passed);
        Assert.Empty(violated);
        var pending = Assert.IsType<Relation>(Assert.Single(unresolved));
        Assert.True(FiniteLawConformance.TryReadObligation(pending, out _, out var known, out _, out var proofs));
        Assert.Same(a, Assert.Single(FiniteLaw.Entries(known)));
        Assert.True(FiniteLaw.Entries(proofs).Length >= 2);
    }

    [Fact]
    public void CertificationConcernsSuppliedCasesAndDoesNotAssumeUndeclaredTotality()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var law = FiniteLaw.Create(S(a, b), S(a, b), S(a, b), FiniteLaw.Row(a, a, S(a)));
        var selectedCase = Eq(Op(law, S(a), S(a)), S(a));
        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, S(selectedCase)));
        Assert.Single(passed);
        Assert.Empty(violated);
        Assert.Empty(unresolved);
    }

    [Fact]
    public void OpenObligationScopeRetainsKnownSuccessAndUnaccountedTail()
    {
        var (law, low, middle, _) = OrdinalMaximum();
        var obligation = Eq(Op(law, S(low), S(middle)), S(middle));
        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, StructuralConstraint.Open(obligation)));
        Assert.Single(passed);
        Assert.Empty(violated);
        Assert.Single(unresolved);
    }

    [Fact]
    public void OpenAndEmptyInputsAndExpectedSupportsNeverPassVacuously()
    {
        var (law, low, middle, _) = OrdinalMaximum();
        var cases = new[]
        {
            Eq(Op(law, StructuralConstraint.Open(low), S(middle)), S(middle)),
            Eq(Op(law, S(), S(middle)), S()),
            Eq(Op(law, S(low), S(middle)), StructuralConstraint.Open(middle)),
            Eq(S(), S())
        };
        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, S(cases)));
        Assert.Empty(passed);
        Assert.Empty(violated);
        Assert.Equal(cases.Length, unresolved.Length);
    }

    [Fact]
    public void EqualLookingForeignParticipantAndOpposedFaceRemainUnresolved()
    {
        var start = new Placeholder(); var end = new Placeholder();
        var state = new Relation(start, end); var lookalike = new Relation(start, end);
        var law = FiniteLaw.Create(S(state), S(state), S(state), FiniteLaw.Row(state, state, S(state)));
        var slot = new Placeholder();
        var otherLaw = FiniteLaw.Create(S(slot), S(slot), S(slot), FiniteLaw.Row(slot, slot, S(slot)));
        var cases = S(Eq(Op(law, S(state), S(state)), S(lookalike)),
            Eq(Op(law, S(lookalike), S(state)), S(state)));
        Assert.Equal(state, lookalike);
        var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law, cases));
        Assert.Empty(passed); Assert.Empty(violated); Assert.Equal(2, unresolved.Length);
        var (facePassed, faceViolated, faceUnresolved) = Read(FiniteLawConformance.Check(otherLaw,
            S(Eq(Op(otherLaw, S(slot), S(slot)), S(slot.Dual())))));
        Assert.Empty(facePassed); Assert.Empty(faceViolated); Assert.Single(faceUnresolved);
    }

    [Fact]
    public void MalformedAndForeignLawApplicationsAreUnresolvedWithOriginalEvidence()
    {
        var (law, low, middle, _) = OrdinalMaximum();
        var (otherLaw, _, _, _) = OrdinalMaximum();
        var foreignApplication = Op(otherLaw, S(low), S(middle));
        var malformed = new Relation(low, middle);
        var obligations = S(Eq(foreignApplication, S(middle)), Eq(malformed, S(middle)), low);
        var report = FiniteLawConformance.Check(law, obligations);
        var (passed, violated, unresolved) = Read(report);
        Assert.Empty(passed); Assert.Empty(violated); Assert.Equal(3, unresolved.Length);
        Assert.True(ContainsReference(report, foreignApplication));
        Assert.True(ContainsReference(report, malformed));
    }

    [Fact]
    public void OpenOrForeignNativeOutputCannotBecomeAnEqualityProof()
    {
        var a = new Placeholder(); var foreign = new Placeholder();
        foreach (var output in new[] { StructuralConstraint.Open(a), S(foreign), S() })
        {
            var law = FiniteLaw.Create(S(a), S(a), S(a), FiniteLaw.Row(a, a, output));
            var (passed, violated, unresolved) = Read(FiniteLawConformance.Check(law,
                S(Eq(Op(law, S(a), S(a)), S(a)))));
            Assert.Empty(passed); Assert.Empty(violated); Assert.Single(unresolved);
        }
    }

    [Fact]
    public void MalformedLawOrObligationScopeRemainsVisible()
    {
        var (law, low, middle, _) = OrdinalMaximum();
        var malformed = new Relation(S(low), middle);
        var claims = S(Eq(S(low), S(low)));
        var invalidLawReport = FiniteLawConformance.Check(malformed, claims);
        var (passed, violated, unresolved) = Read(invalidLawReport);
        Assert.Empty(passed); Assert.Empty(violated); Assert.NotEmpty(unresolved);
        Assert.True(ContainsReference(invalidLawReport, malformed));
        var (otherPassed, otherViolated, otherUnresolved) = Read(FiniteLawConformance.Check(law, malformed));
        Assert.Empty(otherPassed); Assert.Empty(otherViolated); Assert.Same(malformed, Assert.Single(otherUnresolved));
    }

    private static (Relation Law, IElement Low, IElement Middle, IElement High) OrdinalMaximum()
    {
        var low = new Placeholder(); var middle = new Placeholder(); var high = new Placeholder();
        var domain = S(low, middle, high);
        return (FiniteLaw.Create(domain, domain, domain,
            FiniteLaw.Row(low, low, S(low)), FiniteLaw.Row(low, middle, S(middle)),
            FiniteLaw.Row(low, high, S(high)), FiniteLaw.Row(middle, low, S(middle)),
            FiniteLaw.Row(middle, middle, S(middle)), FiniteLaw.Row(middle, high, S(high)),
            FiniteLaw.Row(high, low, S(high)), FiniteLaw.Row(high, middle, S(high)),
            FiniteLaw.Row(high, high, S(high))), low, middle, high);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static Relation Op(Relation law, Relation left, Relation right) => FiniteLawConformance.Apply(law, left, right);
    private static Relation Eq(Relation left, Relation right) => FiniteLawConformance.Equation(left, right);
    private static (IElement[] Passed, IElement[] Violated, IElement[] Unresolved) Read(Relation report)
    {
        Assert.True(FiniteLawConformance.TryReadReport(report, out _, out _, out var passed, out var violated, out var unresolved));
        return (FiniteLaw.Entries(passed), FiniteLaw.Entries(violated), FiniteLaw.Entries(unresolved));
    }

    private static bool ContainsReference(IElement root, IElement expected) =>
        ReferenceEquals(root, expected) || root is Relation relation &&
        (ContainsReference(relation.Left, expected) || ContainsReference(relation.Right, expected));

    private static void AssertNoNumericParticipants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        {
            if (!seen.Add(item)) continue;
            Assert.False(item is Carrier or CarrierLocation or TickRef);
            if (item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }

    private static (Relation Site, IElement Left, IElement Right)[] Sites(Relation root)
    {
        var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<Relation>(); pending.Push(root);
        while (pending.TryPop(out var relation))
        {
            if (!seen.Add(relation)) continue;
            if (relation.Left is Relation left) pending.Push(left);
            if (relation.Right is Relation right) pending.Push(right);
        }
        return seen.Select(relation => (relation, relation.Left, relation.Right)).ToArray();
    }
}
