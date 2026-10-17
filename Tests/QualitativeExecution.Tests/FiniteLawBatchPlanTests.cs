using System.Runtime.InteropServices;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class FiniteLawBatchPlanTests
{
    [Fact]
    public void EveryClosedSupportMatchesNativeResultsAndConsumedRows()
    {
        var view = new ThresholdFixture().Forward;
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(view.Law));
        var inputs = new List<FiniteLawMaskPair>();
        for (ulong left = 0; left < 8; left++)
            for (ulong right = 0; right < 8; right++) inputs.Add(new(left, right));
        var results = new ulong[inputs.Count];
        Assert.True(plan.TryEvaluateBatch(inputs.ToArray(), results));
        Assert.Same(view.Law, plan.SourceLaw);
        Assert.Equal(9, plan.AtomicPairCount);

        for (var index = 0; index < inputs.Count; index++)
        {
            var pair = inputs[index];
            var left = Support(pair.Left, view.Below, view.WithinBand, view.Above);
            var right = Support(pair.Right, view.Falling, view.Steady, view.Rising);
            var native = FiniteLaw.Evaluate(view.Law, left, right);
            Assert.True(FiniteLaw.TryReadResult(native, out var nativeSupport, out var nativeResidue, out var nativeProofs));
            Assert.True(plan.TryEncodeOutput(nativeSupport, out var expected));
            Assert.Equal(expected, results[index]);
            Assert.Empty(FiniteLaw.Entries(nativeResidue));
            Assert.True(plan.TryDecodeOutput(results[index], out var decoded));
            AssertSameParticipants(FiniteLaw.Entries(nativeSupport), FiniteLaw.Entries(decoded));
            Assert.True(plan.TryCreateWitness(pair.Left, pair.Right, out var materialized));
            Assert.True(FiniteLaw.TryReadResult(materialized, out var support, out var residue, out var proofs));
            AssertSameParticipants(FiniteLaw.Entries(nativeSupport), FiniteLaw.Entries(support));
            Assert.Empty(FiniteLaw.Entries(residue));
            AssertSameParticipants(FiniteLaw.Entries(nativeProofs), FiniteLaw.Entries(proofs));
            Assert.True(ContainsReference(materialized, plan.SourceLaw));
        }
    }

    [Fact]
    public void NineStockTransitionsCompileAndKeepTheirAcceptedAndBlockedStructure()
    {
        var stock = new QualitativeStockFixture();
        Assert.Null(FiniteLawPlan.Capture(stock.Law));
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(stock.Law));
        Assert.Equal(9, plan.OutputCount);
        var pairs = new List<FiniteLawMaskPair>();
        for (var state = 0; state < 3; state++)
            for (var input = 0; input < 3; input++) pairs.Add(new(1UL << state, 1UL << input));
        var outputs = new ulong[9];
        Assert.True(plan.TryEvaluateBatch(pairs.ToArray(), outputs));
        for (var i = 0; i < outputs.Length; i++) Assert.Equal(1UL << i, outputs[i]);
        Assert.True(plan.TryDecodeOutput(outputs[6], out var fullCharge));
        var transition = Assert.IsType<Relation>(Assert.Single(FiniteLaw.Entries(fullCharge)));
        Assert.True(QualitativeStockFixture.TryReadTransition(transition,
            out var before, out var inputPulse, out var after, out var accepted, out var blocked));
        Assert.Same(stock.Full, before); Assert.Same(stock.Charge, inputPulse); Assert.Same(stock.Full, after);
        Assert.Empty(FiniteLaw.Entries(accepted));
        Assert.Same(stock.Charge, Assert.Single(FiniteLaw.Entries(blocked)));
    }

    [Fact]
    public void SupportsCanUseTheSixtyFourthBitWithoutAnExponentialSubsetTable()
    {
        var states = Enumerable.Range(0, 64).Select(_ => (IElement)new Placeholder()).ToArray();
        var partner = new Placeholder();
        var domain = StructuralConstraint.Closed(states);
        var right = StructuralConstraint.Closed(partner);
        var law = FiniteLaw.Create(domain, right, domain,
            states.Select(state => FiniteLaw.Row(state, partner, StructuralConstraint.Closed(state))).ToArray());
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        Assert.Equal(64, plan.AtomicPairCount);
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(states[63]), out var highBit));
        Assert.Equal(1UL << 63, highBit);
        Assert.True(plan.TryEncodeLeft(domain, out var full));
        Assert.Equal(ulong.MaxValue, full);
        var outputs = new ulong[3];
        Assert.True(plan.TryEvaluateBatch(
            new FiniteLawMaskPair[] { new(highBit, 1), new(ulong.MaxValue, 1), new(0, 1) }, outputs));
        Assert.Equal(new[] { highBit, ulong.MaxValue, 0UL }, outputs);
        Assert.True(plan.TryDecodeOutput(highBit, out var selected));
        Assert.Same(states[63], Assert.Single(FiniteLaw.Entries(selected)));
    }

    [Fact]
    public void RefusalForAnyInvalidMaskLeavesEveryOutputUnchanged()
    {
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(new ThresholdFixture().Forward.Law));
        var outputs = new ulong[] { 99, 98, 97, 96 };
        Assert.False(plan.TryEvaluateBatch(new FiniteLawMaskPair[] { new(1, 1), new(8, 1), new(2, 2) }, outputs));
        Assert.Equal(new ulong[] { 99, 98, 97, 96 }, outputs);
        Assert.False(plan.TryEvaluateBatch(new ulong[] { 1, 2, 4 }, new ulong[] { 1, 8, 1 }, outputs));
        Assert.Equal(new ulong[] { 99, 98, 97, 96 }, outputs);
        Assert.False(plan.TryEvaluateBatch(new ulong[] { 1 }, new ulong[] { 1, 2 }, outputs));
        Assert.False(plan.TryEvaluateBatch(new FiniteLawMaskPair[] { new(1, 1), new(1, 1) }, outputs.AsSpan(0, 1)));
        Assert.Equal(new ulong[] { 99, 98, 97, 96 }, outputs);
    }

    [Fact]
    public void SeparateArraysCanFeedFollowingStagesAndUseExactInPlaceResults()
    {
        var frame = new OrdinalReferenceFrame();
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(frame.CompositionLaw));
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(frame.Higher), out var higher));
        Assert.True(plan.TryEncodeRight(StructuralConstraint.Closed(frame.Equal), out var equal));
        var state = new[] { higher, higher, higher };
        var reference = new[] { equal, equal, equal };
        Assert.True(plan.TryEvaluateBatch(state, reference, state));
        Assert.True(plan.TryEvaluateBatch(state, reference, state));
        Assert.Equal(new[] { higher, higher, higher }, state);
    }

    [Fact]
    public void ShiftedOrPairBufferOverlapCannotDestroyLaterInputs()
    {
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(new ThresholdFixture().Forward.Law));
        var storage = new ulong[] { 1, 2, 4, 777 };
        Assert.False(plan.TryEvaluateBatch(storage.AsSpan(0, 3), new ulong[] { 1, 1, 1 }, storage.AsSpan(1, 3)));
        Assert.Equal(new ulong[] { 1, 2, 4, 777 }, storage);
        var pairs = new FiniteLawMaskPair[] { new(1, 1), new(2, 2) };
        var aliased = MemoryMarshal.Cast<FiniteLawMaskPair, ulong>(pairs.AsSpan());
        Assert.False(plan.TryEvaluateBatch(pairs, aliased));
        Assert.Equal(new FiniteLawMaskPair[] { new(1, 1), new(2, 2) }, pairs);
    }

    [Fact]
    public void SuccessfulBatchesWriteOnlyTheirSelectedOutputPrefix()
    {
        var view = new ThresholdFixture().Forward;
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(view.Law));
        Assert.True(plan.TryEncodeOutput(StructuralConstraint.Closed(view.Departing), out var departing));
        var outputs = new ulong[] { 99, 98, 97 };
        Assert.True(plan.TryEvaluateBatch(new FiniteLawMaskPair[] { new(1, 1) }, outputs));
        Assert.Equal(new[] { departing, 98UL, 97UL }, outputs);
        Assert.True(plan.TryEvaluateBatch(ReadOnlySpan<FiniteLawMaskPair>.Empty, outputs));
        Assert.Equal(new[] { departing, 98UL, 97UL }, outputs);
    }

    [Fact]
    public void TickRetargetingRefusesTheWholeBatchAndRetainsStaleEvidence()
    {
        var tick = new TickRef(4);
        var state = new Relation(tick, new Placeholder());
        var partner = new Placeholder();
        var output = new Placeholder();
        var left = StructuralConstraint.Closed(state);
        var right = StructuralConstraint.Closed(partner);
        var law = FiniteLaw.Create(left, right, StructuralConstraint.Closed(output),
            FiniteLaw.Row(state, partner, StructuralConstraint.Closed(output)));
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        tick.Move(1);
        Assert.False(plan.IsCurrent);
        var outputs = new ulong[] { 444, 555 };
        Assert.False(plan.TryEvaluateBatch(new FiniteLawMaskPair[] { new(1, 1), new(1, 1) }, outputs));
        Assert.Equal(new ulong[] { 444, 555 }, outputs);
        Assert.False(plan.TryEncodeLeft(left, out _));
        Assert.False(plan.TryDecodeOutput(1, out _));
        Assert.False(plan.TryCreateWitness(1, 1, out _));
        var read = plan.Evaluate(left, right);
        Assert.True(FiniteLaw.TryReadResult(read, out var support, out var residue, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        Assert.NotEmpty(FiniteLaw.Entries(residue));
        Assert.True(ContainsReference(residue, tick));
        var recaptured = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        Assert.True(recaptured.TryEvaluateBatch(new FiniteLawMaskPair[] { new(1, 1) }, outputs));
        Assert.Equal(new ulong[] { 1, 555 }, outputs);
    }

    [Fact]
    public void BasisAndParticipantIdentityCannotBeSubstitutedByEqualLookingStructure()
    {
        var endpointA = new Placeholder(); var endpointB = new Placeholder();
        var first = new Relation(endpointA, endpointB);
        var second = new Relation(endpointA, endpointB);
        var partner = new Placeholder();
        var domain = StructuralConstraint.Closed(first, second);
        var law = FiniteLaw.Create(domain, StructuralConstraint.Closed(partner), domain,
            FiniteLaw.Row(first, partner, StructuralConstraint.Closed(first)),
            FiniteLaw.Row(second, partner, StructuralConstraint.Closed(second)));
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(first), out var a));
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(second), out var b));
        Assert.Equal(1UL, a); Assert.Equal(2UL, b);
        Assert.False(plan.TryEncodeLeft(StructuralConstraint.Closed(new Relation(endpointA, endpointB)), out _));
        Assert.False(plan.TryDecodeOutput(StructuralConstraint.Closed(first, second), b, out _));
        Assert.True(plan.TryDecodeOutput(domain, b, out var output));
        Assert.Same(second, Assert.Single(FiniteLaw.Entries(output)));
        Assert.False(plan.TryDecodeOutput(4, out _));
    }

    [Fact]
    public void DuplicateProofRowsAndMissingPairsRemainVisibleInMaterializedReads()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var domain = StructuralConstraint.Closed(a, b);
        var rowA = FiniteLaw.Row(a, b, StructuralConstraint.Closed(a));
        var rowB = FiniteLaw.Row(a, b, StructuralConstraint.Closed(a));
        var law = FiniteLaw.Create(domain, domain, domain, rowA, rowB);
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        Assert.True(plan.TryCreateWitness(1, 2, out var read));
        Assert.True(FiniteLaw.TryReadResult(read, out var output, out var residue, out var proof));
        Assert.Same(a, Assert.Single(FiniteLaw.Entries(output)));
        Assert.Empty(FiniteLaw.Entries(residue));
        AssertSameParticipants([rowA, rowB], FiniteLaw.Entries(proof));
        Assert.True(plan.TryCreateWitness(1, 1, out var missing));
        Assert.True(FiniteLaw.TryReadResult(missing, out var noOutput, out var noResidue, out var noProof));
        Assert.Empty(FiniteLaw.Entries(noOutput)); Assert.Empty(FiniteLaw.Entries(noResidue));
        Assert.Empty(FiniteLaw.Entries(noProof));
    }

    [Fact]
    public void OpenForeignAndMalformedSupportsAreRefusedAsMasksButRetainNativeEvidence()
    {
        var view = new ThresholdFixture().Forward;
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(view.Law));
        var supports = new[]
        {
            StructuralConstraint.Open(view.Above),
            StructuralConstraint.Closed(new Relation(view.Above.Left, view.Above.Right)),
            new Relation(view.Above, view.Above)
        };
        foreach (var offered in supports)
        {
            Assert.False(plan.TryEncodeLeft(offered, out _));
            var read = plan.Evaluate(offered, StructuralConstraint.Closed(view.Rising));
            Assert.True(FiniteLaw.TryReadResult(read, out _, out var residue, out _));
            Assert.NotEmpty(FiniteLaw.Entries(residue));
            Assert.True(ContainsReference(read, offered));
        }
        Assert.True(plan.TryEncodeLeft(StructuralConstraint.Closed(), out var empty));
        Assert.Equal(0UL, empty);
        Assert.False(plan.TryEncodeLeft(StructuralConstraint.Open(), out _));
    }

    [Fact]
    public void UnsupportedLawsAreRefusedBeforeAnyCompiledMeaningIsAssigned()
    {
        var a = new Placeholder(); var b = new Placeholder(); var output = new Placeholder();
        var left = StructuralConstraint.Closed(a); var right = StructuralConstraint.Closed(b);
        var outputs = StructuralConstraint.Closed(output);
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(StructuralConstraint.Open(a), right, outputs)));
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(StructuralConstraint.Closed(a, a), right, outputs)));
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(left, right, outputs,
            FiniteLaw.Row(a, b, StructuralConstraint.Open(output)))));
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(left, right, outputs,
            FiniteLaw.Row(a, b, StructuralConstraint.Closed(new Placeholder())))));
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(left, right, outputs,
            FiniteLaw.Row(new Placeholder(), b, outputs))));
        var oversized = StructuralConstraint.Closed(Enumerable.Range(0, 65).Select(_ => (IElement)new Placeholder()).ToArray());
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(oversized, right, outputs)));
        Assert.Null(FiniteLawBatchPlan.Capture(FiniteLaw.Create(left, right, oversized)));
    }

    [Fact]
    public void ComputeOnlyBatchAllocatesNoPerReadWitnesses()
    {
        var plan = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(new OrdinalReferenceFrame().CompositionLaw));
        var inputs = Enumerable.Repeat(new FiniteLawMaskPair(1, 4), 256).ToArray();
        var outputs = new ulong[inputs.Length];
        Assert.True(plan.TryEvaluateBatch(inputs, outputs));
        var before = GC.GetAllocatedBytesForCurrentThread();
        var success = plan.TryEvaluateBatch(inputs, outputs);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(success);
        Assert.Equal(0, allocated);
        Assert.All(outputs, output => Assert.Equal(7UL, output));
    }

    private static Relation Support(ulong mask, params IElement[] states) =>
        StructuralConstraint.Closed(states.Where((_, index) => (mask & 1UL << index) != 0).ToArray());

    private static void AssertSameParticipants(IEnumerable<IElement> expected, IEnumerable<IElement> actual)
    {
        var a = expected.ToArray(); var b = actual.ToArray();
        Assert.Equal(a.Length, b.Length);
        foreach (var item in a) Assert.Contains(b, value => ReferenceEquals(value, item));
    }

    private static bool ContainsReference(IElement root, IElement participant)
    {
        var pending = new Stack<IElement>(); pending.Push(root);
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        while (pending.TryPop(out var element))
        {
            if (ReferenceEquals(element, participant)) return true;
            if (!seen.Add(element) || element is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
        return false;
    }
}
