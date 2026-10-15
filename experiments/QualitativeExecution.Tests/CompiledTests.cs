using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class CompiledTests
{
    public static IEnumerable<object[]> ThresholdSupports()
    {
        foreach (var reversed in new[] { false, true })
            for (var side = 0; side < 8; side++)
                for (var trend = 0; trend < 8; trend++)
                    yield return [reversed, side, trend];
    }

    [Theory]
    [MemberData(nameof(ThresholdSupports))]
    public void EveryClosedSupportHasTheSameNativeWitnessAndMaskAnswer(bool reversed, int side, int trend)
    {
        var fixture = new ThresholdFixture();
        var view = reversed ? fixture.Reverse : fixture.Forward;
        var left = Support(side, view.Below, view.WithinBand, view.Above);
        var right = Support(trend, view.Falling, view.Steady, view.Rising);
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(view.Law));

        var native = FiniteLaw.Evaluate(view.Law, left, right);
        var compiled = plan.Evaluate(left, right);

        Assert.Same(view.Law, plan.SourceLaw);
        Assert.True(plan.TryEncodeLeft(left, out var leftMask));
        Assert.True(plan.TryEncodeRight(right, out var rightMask));
        Assert.Equal((byte)side, leftMask);
        Assert.Equal((byte)trend, rightMask);
        Assert.True(plan.TryEvaluateMasks(leftMask, rightMask, out var computed));
        Assert.True(FiniteLaw.TryReadResult(native, out var nativeSupport, out var nativeResidue, out var nativeRows));
        Assert.True(FiniteLaw.TryReadResult(compiled, out var compiledSupport, out var compiledResidue, out var compiledRows));
        AssertClosed(nativeSupport);
        AssertClosed(compiledSupport);
        Assert.Empty(FiniteLaw.Entries(nativeResidue));
        Assert.Empty(FiniteLaw.Entries(compiledResidue));
        AssertSameParticipants(FiniteLaw.Entries(nativeSupport), FiniteLaw.Entries(compiledSupport));
        AssertSameParticipants(FiniteLaw.Entries(nativeRows), FiniteLaw.Entries(compiledRows));
        Assert.True(plan.TryEncodeOutput(nativeSupport, out var nativeMask));
        Assert.True(plan.TryEncodeOutput(compiledSupport, out var compiledMask));
        Assert.Equal(nativeMask, compiledMask);
        Assert.Equal(nativeMask, computed);

        // The oracle is expressed in state identities, independent of the
        // compiler's bit ordering. WithinBand is a detector category, not zero.
        IElement[][] grid =
        [
            [view.Departing, view.OutsideSteady, view.Approaching],
            [view.WithinChanging, view.WithinSteady, view.WithinChanging],
            [view.Approaching, view.OutsideSteady, view.Departing]
        ];
        var expected = new List<IElement>();
        for (var a = 0; a < 3; a++)
            for (var b = 0; b < 3; b++)
                if ((side & (1 << a)) != 0 && (trend & (1 << b)) != 0 &&
                    !expected.Any(item => ReferenceEquals(item, grid[a][b])))
                    expected.Add(grid[a][b]);
        AssertSameParticipants(expected, FiniteLaw.Entries(nativeSupport));
        Assert.True(ContainsReference(native, view.Law));
        Assert.True(ContainsReference(compiled, view.Law));
        Assert.True(ContainsReference(compiled, left));
        Assert.True(ContainsReference(compiled, right));
    }

    [Fact]
    public void EmptyUnknownAndAllKnownAlternativesRemainDifferentReads()
    {
        var view = new ThresholdFixture().Forward;
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(view.Law));
        var empty = StructuralConstraint.Closed();
        var unknown = StructuralConstraint.Open();
        var all = StructuralConstraint.Closed(view.Below, view.WithinBand, view.Above);
        var rising = StructuralConstraint.Closed(view.Rising);

        Assert.True(plan.TryEncodeLeft(empty, out var emptyMask));
        Assert.Equal((byte)0, emptyMask);
        Assert.False(plan.TryEncodeLeft(unknown, out _));
        Assert.True(plan.TryEncodeLeft(all, out var allMask));
        Assert.Equal((byte)7, allMask);
        Assert.True(FiniteLaw.TryReadResult(plan.Evaluate(empty, rising), out var impossible, out var emptyResidue, out _));
        AssertClosed(impossible);
        Assert.Empty(FiniteLaw.Entries(impossible));
        Assert.Empty(FiniteLaw.Entries(emptyResidue));
        Assert.True(FiniteLaw.TryReadResult(plan.Evaluate(unknown, rising), out var open, out var residue, out _));
        AssertOpen(open);
        Assert.Empty(FiniteLaw.Entries(open));
        Assert.NotEmpty(FiniteLaw.Entries(residue));
        Assert.True(FiniteLaw.TryReadResult(plan.Evaluate(all, rising), out var alternatives, out var allResidue, out _));
        AssertClosed(alternatives);
        AssertSameParticipants([view.Approaching, view.WithinChanging, view.Departing], FiniteLaw.Entries(alternatives));
        Assert.Empty(FiniteLaw.Entries(allResidue));

        // The contradictory empty operand still rules out every pair. Open
        // evidence remains residue and is not silently promoted to a result.
        Assert.True(FiniteLaw.TryReadResult(plan.Evaluate(empty, StructuralConstraint.Open()),
            out var emptyWithOpenEvidence, out var preservedEvidence, out _));
        AssertClosed(emptyWithOpenEvidence);
        Assert.Empty(FiniteLaw.Entries(emptyWithOpenEvidence));
        Assert.NotEmpty(FiniteLaw.Entries(preservedEvidence));
    }

    [Fact]
    public void OpenForeignAndDisconnectedInputsCannotBecomeOrdinaryMasks()
    {
        var view = new ThresholdFixture().Forward;
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(view.Law));
        var foreign = new Relation(view.Above.Left, view.Above.Right);
        Assert.Equal(view.Above, foreign);
        Assert.False(view.Above.SameIdentityAs(foreign));
        var inputs = new[]
        {
            StructuralConstraint.Open(view.Above),
            StructuralConstraint.Closed(foreign),
            StructuralConstraint.Closed(StructuralConstraint.Closed(view.Above)),
            new Relation(StructuralConstraint.Closed(view.Above), StructuralConstraint.Closed(view.Above))
        };
        foreach (var input in inputs)
        {
            Assert.False(plan.TryEncodeLeft(input, out _));
            var native = FiniteLaw.Evaluate(view.Law, input, StructuralConstraint.Closed(view.Falling));
            var compiled = plan.Evaluate(input, StructuralConstraint.Closed(view.Falling));
            Assert.True(FiniteLaw.TryReadResult(native, out var nativeSupport, out var nativeResidue, out _));
            Assert.True(FiniteLaw.TryReadResult(compiled, out var compiledSupport, out var compiledResidue, out _));
            AssertOpen(nativeSupport);
            AssertOpen(compiledSupport);
            Assert.NotEmpty(FiniteLaw.Entries(nativeResidue));
            Assert.NotEmpty(FiniteLaw.Entries(compiledResidue));
            AssertSameParticipants(FiniteLaw.Entries(nativeSupport), FiniteLaw.Entries(compiledSupport));
        }
        Assert.False(plan.TryEvaluateMasks(8, 1, out _));
        Assert.False(plan.TryEvaluateMasks(1, 8, out _));
    }

    [Fact]
    public void EqualLookingRelationsAndPlaceholderFacesKeepTheirOwnAddresses()
    {
        var leftEndpoint = new Placeholder();
        var rightEndpoint = new Placeholder();
        var first = new Relation(leftEndpoint, rightEndpoint);
        var second = new Relation(leftEndpoint, rightEndpoint);
        var foreign = new Relation(leftEndpoint, rightEndpoint);
        var slot = new Placeholder();
        var right = StructuralConstraint.Closed(slot);
        var firstOutput = new Placeholder();
        var secondOutput = new Placeholder();
        var law = FiniteLaw.Create(StructuralConstraint.Closed(first, second), right,
            StructuralConstraint.Closed(firstOutput, secondOutput),
            FiniteLaw.Row(first, slot, StructuralConstraint.Closed(firstOutput)),
            FiniteLaw.Row(second, slot, StructuralConstraint.Closed(secondOutput)));
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));

        Assert.Equal(first, second);
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(first), out var firstMask));
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(second), out var secondMask));
        Assert.Equal((byte)1, firstMask);
        Assert.Equal((byte)2, secondMask);
        Assert.False(plan.TryEncodeLeft(StructuralConstraint.Closed(foreign), out _));
        Assert.True(plan.TryEncodeRight(StructuralConstraint.Closed(slot.Dual().Dual()), out var sameFace));
        Assert.Equal((byte)1, sameFace);
        Assert.False(plan.TryEncodeRight(StructuralConstraint.Closed(slot.Dual()), out _));
        AssertResult(plan.Evaluate(StructuralConstraint.Closed(first), right), firstOutput);
        AssertResult(plan.Evaluate(StructuralConstraint.Closed(second), right), secondOutput);
        Assert.True(ContainsReference(plan.SourceLaw, first));
        Assert.True(ContainsReference(plan.SourceLaw, second));
        Assert.True(ContainsReference(plan.SourceLaw, slot));
    }

    [Fact]
    public void ChangingNativeRowsChangesTheCapturedAlgebra()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var first = new Placeholder();
        var second = new Placeholder();
        var left = StructuralConstraint.Closed(a);
        var right = StructuralConstraint.Closed(b);
        var outputs = StructuralConstraint.Closed(first, second);
        var firstRow = FiniteLaw.Row(a, b, StructuralConstraint.Closed(first));
        var changedRow = FiniteLaw.Row(a, b, StructuralConstraint.Closed(second));
        var original = FiniteLaw.Create(left, right, outputs, firstRow);
        var changed = FiniteLaw.Create(left, right, outputs, changedRow);
        var originalPlan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(original));
        var changedPlan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(changed));

        Assert.Same(original, originalPlan.SourceLaw);
        Assert.Same(changed, changedPlan.SourceLaw);
        AssertResult(originalPlan.Evaluate(left, right), first);
        AssertResult(changedPlan.Evaluate(left, right), second);
        Assert.True(originalPlan.TryEvaluateMasks(1, 1, out var originalMask));
        Assert.True(changedPlan.TryEvaluateMasks(1, 1, out var changedMask));
        Assert.Equal((byte)1, originalMask);
        Assert.Equal((byte)2, changedMask);
        Assert.True(FiniteLaw.TryReadResult(changedPlan.Evaluate(left, right), out _, out _, out var evidence));
        Assert.Same(changedRow, Assert.Single(FiniteLaw.Entries(evidence)));
        Assert.DoesNotContain(FiniteLaw.Entries(evidence), entry => ReferenceEquals(entry, firstRow));
    }

    [Fact]
    public void AChangedCapturedTickLeavesThePlanStaleUntilExplicitRecapture()
    {
        // Cache-integrity control only: this tick is a reachable structural
        // site, not a detector measurement or a numeric category code.
        var tick = new TickRef(0);
        var input = new Relation(tick, new Placeholder());
        var partner = new Placeholder();
        var output = new Placeholder();
        var left = StructuralConstraint.Closed(input);
        var right = StructuralConstraint.Closed(partner);
        var law = FiniteLaw.Create(left, right, StructuralConstraint.Closed(output),
            FiniteLaw.Row(input, partner, StructuralConstraint.Closed(output)));
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));
        Assert.True(plan.IsCurrent);
        tick.Move(1);

        Assert.False(plan.IsCurrent);
        Assert.False(plan.TryEvaluateMasks(1, 1, out _));
        Assert.False(plan.TryEncodeLeft(left, out _));
        Assert.True(FiniteLaw.TryReadResult(plan.Evaluate(left, right), out var support, out var residue, out _));
        AssertOpen(support);
        Assert.NotEmpty(FiniteLaw.Entries(residue));
        var recaptured = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));
        Assert.True(recaptured.IsCurrent);
        AssertResult(recaptured.Evaluate(left, right), output);
    }

    [Fact]
    public void OrderedRolesAndDistinctProofRowsSurviveCompilation()
    {
        var first = new Placeholder();
        var second = new Placeholder();
        var domain = StructuralConstraint.Closed(first, second);
        var firstRoute = FiniteLaw.Row(first, second, StructuralConstraint.Closed(first));
        var secondRoute = FiniteLaw.Row(first, second, StructuralConstraint.Closed(first));
        var reversedRoute = FiniteLaw.Row(second, first, StructuralConstraint.Closed(second));
        var law = FiniteLaw.Create(domain, domain, domain, firstRoute, secondRoute, reversedRoute);
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));

        var forward = plan.Evaluate(StructuralConstraint.Closed(first), StructuralConstraint.Closed(second));
        var reverse = plan.Evaluate(StructuralConstraint.Closed(second), StructuralConstraint.Closed(first));
        AssertResult(forward, first);
        AssertResult(reverse, second);
        Assert.True(FiniteLaw.TryReadResult(forward, out _, out _, out var forwardProofs));
        AssertSameParticipants([firstRoute, secondRoute], FiniteLaw.Entries(forwardProofs));
        Assert.True(FiniteLaw.TryReadResult(reverse, out _, out _, out var reverseProofs));
        Assert.Same(reversedRoute, Assert.Single(FiniteLaw.Entries(reverseProofs)));
        AssertResult(plan.Evaluate(StructuralConstraint.Closed(first), StructuralConstraint.Closed(first)));
    }

    [Fact]
    public void ScalarSignMultiplicationUsesTheSameCompilerWithItsOwnExactZero()
    {
        // Native nodes have no numeric codes or numeric operands. This law is
        // supplied explicitly; detector WithinBand is never used as exact zero.
        var order = new Placeholder();
        var negative = new Relation(order, new Placeholder());
        var zero = new Relation(order, new Placeholder());
        var positive = new Relation(order, new Placeholder());
        var domain = StructuralConstraint.Closed(negative, zero, positive);
        var law = FiniteLaw.Create(domain, domain, domain,
            FiniteLaw.Row(negative, negative, StructuralConstraint.Closed(positive)),
            FiniteLaw.Row(negative, zero, StructuralConstraint.Closed(zero)),
            FiniteLaw.Row(negative, positive, StructuralConstraint.Closed(negative)),
            FiniteLaw.Row(zero, negative, StructuralConstraint.Closed(zero)),
            FiniteLaw.Row(zero, zero, StructuralConstraint.Closed(zero)),
            FiniteLaw.Row(zero, positive, StructuralConstraint.Closed(zero)),
            FiniteLaw.Row(positive, negative, StructuralConstraint.Closed(negative)),
            FiniteLaw.Row(positive, zero, StructuralConstraint.Closed(zero)),
            FiniteLaw.Row(positive, positive, StructuralConstraint.Closed(positive)));
        var plan = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));
        Assert.Empty(law.Carriers());
        Assert.Empty(law.CarrierLocations());
        AssertResult(plan.Evaluate(StructuralConstraint.Closed(negative), StructuralConstraint.Closed(positive)), negative);
        AssertResult(plan.Evaluate(StructuralConstraint.Closed(negative), StructuralConstraint.Closed(negative)), positive);
        AssertResult(plan.Evaluate(StructuralConstraint.Closed(zero), domain), zero);
        Assert.False(plan.TryEncodeLeft(StructuralConstraint.Closed(new ThresholdFixture().Forward.WithinBand), out _));
        for (var leftMask = 0; leftMask < 8; leftMask++)
            for (var rightMask = 0; rightMask < 8; rightMask++)
            {
                var left = Support(leftMask, negative, zero, positive);
                var right = Support(rightMask, negative, zero, positive);
                var native = FiniteLaw.Evaluate(law, left, right);
                Assert.True(FiniteLaw.TryReadResult(native, out var support, out var residue, out _));
                Assert.Empty(FiniteLaw.Entries(residue));
                Assert.True(plan.TryEncodeOutput(support, out var expected));
                Assert.True(plan.TryEvaluateMasks((byte)leftMask, (byte)rightMask, out var actual));
                Assert.Equal(expected, actual);
            }
    }

    [Fact]
    public void TheCompilerRefusesUnsupportedLawsRatherThanInventingCodes()
    {
        var a = new Placeholder();
        var b = new Placeholder();
        var output = new Placeholder();
        var domain = StructuralConstraint.Closed(a);
        var right = StructuralConstraint.Closed(b);
        var outputs = StructuralConstraint.Closed(output);
        Assert.Null(FiniteLawPlan.Capture(FiniteLaw.Create(StructuralConstraint.Open(a), right, outputs)));
        Assert.Null(FiniteLawPlan.Capture(FiniteLaw.Create(StructuralConstraint.Closed(a, a), right, outputs)));
        Assert.Null(FiniteLawPlan.Capture(FiniteLaw.Create(domain, right, outputs,
            FiniteLaw.Row(a, b, StructuralConstraint.Open(output)))));
        Assert.Null(FiniteLawPlan.Capture(FiniteLaw.Create(domain, right, outputs,
            FiniteLaw.Row(a, b, StructuralConstraint.Closed(new Placeholder())))));
        Assert.Null(FiniteLawPlan.Capture(FiniteLaw.Create(domain, right, outputs,
            FiniteLaw.Row(new Placeholder(), b, StructuralConstraint.Closed(output)))));
        Assert.Null(FiniteLawPlan.Capture(FiniteLaw.Create(
            StructuralConstraint.Closed(Enumerable.Range(0, 9).Select(_ => (IElement)new Placeholder()).ToArray()),
            right, outputs)));
    }

    private static Relation Support(int mask, params IElement[] participants) =>
        StructuralConstraint.Closed(participants.Where((_, index) => (mask & (1 << index)) != 0).ToArray());

    private static void AssertResult(Relation read, params IElement[] expected)
    {
        Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out _));
        AssertClosed(support);
        Assert.Empty(FiniteLaw.Entries(residue));
        AssertSameParticipants(expected, FiniteLaw.Entries(support));
    }

    private static void AssertClosed(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.Equal(seam, tail);
    }

    private static void AssertOpen(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
    }

    private static void AssertSameParticipants(IEnumerable<IElement> expected, IEnumerable<IElement> actual)
    {
        var expectedEntries = expected.ToArray();
        var actualEntries = actual.ToArray();
        Assert.Equal(expectedEntries.Length, actualEntries.Length);
        foreach (var entry in expectedEntries)
            Assert.Contains(actualEntries, candidate => ReferenceEquals(entry, candidate));
    }

    private static bool ContainsReference(IElement root, IElement participant)
    {
        var pending = new Stack<IElement>();
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out var element))
        {
            if (ReferenceEquals(element, participant)) return true;
            if (!visited.Add(element) || element is not Relation relation) continue;
            pending.Push(relation.Left);
            pending.Push(relation.Right);
        }
        return false;
    }
}
