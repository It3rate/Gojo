using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;

namespace CompositeEmergence.Tests;

public sealed class FeatureMemoryTests
{
    [Fact]
    public void InitialMemoryDistinguishesUnavailableFeatureAndNoPriorOutcome()
    {
        var memory = Create(); AssertOpen(memory.ReadFeature());
        Assert.Equal(0, Value(memory.Available(), memory.AvailableAxis));
        Assert.Equal(0, Value(memory.Age(), memory.AgeAxis));
        Assert.Equal(0, Value(memory.PreviousOutcome(), memory.OutcomeAxis));
        var fields = NativeGraph.Parts(memory.Root, 14);
        Assert.Equal(0UL, Assert.IsType<Carrier>(fields[5]).Value);
    }

    [Fact]
    public void RetainedSignedFeatureIsIndependentOfOldPublicationAndResetsAge()
    {
        var memory = Create(); memory.Retain(P(memory.FeatureAxis, -1));
        var earlier = memory.ReadFeature(); Assert.Equal(-1, Value(earlier, memory.FeatureAxis));
        for (var step = 0; step < 5; step++) memory.Advance();
        memory.Retain(P(memory.FeatureAxis, 1));
        Assert.Equal(1, Value(memory.ReadFeature(), memory.FeatureAxis));
        Assert.Equal(-1, Value(earlier, memory.FeatureAxis));
        Assert.Equal(1, Value(memory.Available(), memory.AvailableAxis));
        Assert.Equal(0, memory.ActualAge);
        var fields = NativeGraph.Parts(memory.Root, 14);
        var site = Assert.IsType<Relation>(fields[3]); var point = Assert.IsType<CarrierLocation>(site.Left);
        Assert.Equal(1, point.Ticks);
    }

    [Theory]
    [InlineData(0, 0)] [InlineData(1, 1)] [InlineData(3, 1)] [InlineData(4, 2)] [InlineData(7, 2)] [InlineData(8, 3)] [InlineData(100, 3)]
    public void NativeAgeLadderPublishesDeclaredCoarseBuckets(int actual, long bucket)
    {
        var memory = Create(); memory.Retain(P(memory.FeatureAxis, 1));
        for (var step = 0; step < actual; step++) memory.Advance();
        Assert.Equal(bucket, Value(memory.Age(), memory.AgeAxis)); Assert.Equal(actual, memory.ActualAge);
        var policies = NativeGraph.Parts((Relation)NativeGraph.Parts(memory.Root, 14)[7], 3);
        Assert.Equal(4, NativeGraph.Parts((Relation)policies[1]).Length);
    }

    [Fact]
    public void FiniteAgeOverflowRemainsTensionAndIsAccountedOnRefresh()
    {
        var memory = Create(); memory.Retain(P(memory.FeatureAxis, 1));
        for (var step = 0; step < FeatureMemory.AgeCapacity + 5; step++) memory.Advance();
        Assert.Equal(FeatureMemory.AgeCapacity, memory.ActualAge); Assert.Equal(5, memory.AgeOverflow);
        Assert.Equal(3, Value(memory.Age(), memory.AgeAxis));
        memory.Retain(P(memory.FeatureAxis, -1)); Assert.Equal(0, memory.ActualAge); Assert.Equal(0, memory.AgeOverflow);
        var retained = (Relation)NativeGraph.Parts(memory.Root, 14)[9];
        Assert.True(ReadWitness.TryRead(retained, out _, out _, out _, out _, out var provenance));
        var beforeAge = NativeGraph.Parts((Relation)NativeGraph.Parts((Relation)provenance, 6)[4], 2);
        Assert.True(NativeGraph.TryPoint(beforeAge[0], out var previous)); Assert.Equal(FeatureMemory.AgeCapacity, previous.Ticks);
        Assert.True(NativeGraph.TryPoint(beforeAge[1], out var excess)); Assert.Equal(5, excess.Ticks);
    }

    [Fact]
    public void ResourceOutcomeSignUsesNativeRulesAndPublicPriorState()
    {
        var memory = Create(); var resourceAxis = new Carrier();
        foreach (var (resource, expected) in new[] { (8L, 1L), (-12L, -1L), (0L, 0L) })
        {
            memory.RememberOutcome(P(resourceAxis, resource));
            Assert.Equal(expected, memory.LastOutcomeNow); Assert.Equal(expected, Value(memory.PreviousOutcome(), memory.OutcomeAxis));
        }
        Assert.Equal(1UL, Assert.IsType<Carrier>(NativeGraph.Parts(memory.Root, 14)[5]).Value);
    }

    [Fact]
    public void MissingResourceDoesNotReplaceRememberedIndependentOutcome()
    {
        var memory = Create(); memory.RememberOutcome(P(new Carrier(), -12));
        AssertOpen(memory.RememberOutcome(NativeGraph.Open(new Placeholder(), new Placeholder())));
        Assert.Equal(-1, Value(memory.PreviousOutcome(), memory.OutcomeAxis));
    }

    [Fact]
    public void FailedFeaturePublicationWithdrawsAvailabilityWithoutCreatingZeroEvidence()
    {
        var memory = Create(); memory.Retain(P(memory.FeatureAxis, 1));
        AssertOpen(memory.Retain(NativeGraph.Open(new Placeholder(), new Placeholder())));
        Assert.Equal(0, Value(memory.Available(), memory.AvailableAxis)); AssertOpen(memory.ReadFeature());
    }

    [Fact]
    public void OperationalNativeTransportPreservesSharedRetargetableFeatureSites()
    {
        var memory = Create(); memory.Retain(P(memory.FeatureAxis, -1)); memory.RememberOutcome(P(new Carrier(), -12));
        for (var step = 0; step < 6; step++) memory.Advance();
        var restored = FeatureMemory.FromRoot(Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(memory.Root)))));
        Assert.Equal(NativeInspection.Signature(memory.Root), NativeInspection.Signature(restored.Root));
        Assert.Equal(-1, Value(restored.ReadFeature(), restored.FeatureAxis)); Assert.Equal(2, Value(restored.Age(), restored.AgeAxis));
        Assert.Equal(-1, Value(restored.PreviousOutcome(), restored.OutcomeAxis));
        restored.Retain(P(restored.FeatureAxis, 1)); Assert.Equal(1, Value(restored.ReadFeature(), restored.FeatureAxis));
    }

    [Fact]
    public void ResetForgetsOperationalStateAndKeepsBoundedGraph()
    {
        var memory = Create(); memory.Retain(P(memory.FeatureAxis, -1)); memory.RememberOutcome(P(new Carrier(), -12));
        for (var step = 0; step < 1000; step++) { memory.Advance(); memory.Age(); memory.ReadFeature(); }
        Assert.InRange(NativeInspection.Count(memory.Root).Relations, 1, 1500);
        memory.Reset(); AssertOpen(memory.ReadFeature()); Assert.Equal(0, memory.ActualAge); Assert.Equal(0, memory.AgeOverflow);
        Assert.Equal(0, Value(memory.Available(), memory.AvailableAxis)); Assert.Equal(0, Value(memory.PreviousOutcome(), memory.OutcomeAxis));
    }

    [Fact]
    public void OutcomeOnlyBenchmarkInfersPreviousSourceRuleWithNativeProduct()
    {
        var actionAxis = new Carrier(); var goalAxis = new Carrier(); var resourceAxis = new Carrier(); var featureAxis = new Carrier();
        foreach (var hiddenRule in new[] { -1L, 1L })
        foreach (var action in new[] { -1L, 1L })
        foreach (var goal in new[] { -1L, 1L })
        {
            var gross = action * goal * hiddenRule == 1 ? 8 : -12;
            var inferred = FeatureMemory.InferFromOutcome(P(actionAxis, action), P(goalAxis, goal), P(resourceAxis, gross), actionAxis, goalAxis, featureAxis);
            Assert.Equal(hiddenRule, Value(inferred, featureAxis));
            Assert.True(ReadWitness.TryRead(inferred, out _, out var equation, out _, out _, out _));
            Assert.True(Equation.TryPredict((Relation)equation,
                new Relation(new Relation(new CarrierLocation(actionAxis, action), new CarrierLocation(goalAxis, goal)),
                    new CarrierLocation(SignAxis((Relation)equation), gross > 0 ? 1 : -1)), featureAxis, out _));
        }
    }

    [Fact]
    public void HoldZeroMissingOrUncertainFeedbackCannotRetrodictAConcreteRule()
    {
        var actionAxis = new Carrier(); var goalAxis = new Carrier(); var resourceAxis = new Carrier(); var featureAxis = new Carrier();
        AssertOpen(FeatureMemory.InferFromOutcome(P(actionAxis, 0), P(goalAxis, 1), P(resourceAxis, 8), actionAxis, goalAxis, featureAxis));
        AssertOpen(FeatureMemory.InferFromOutcome(P(actionAxis, 1), P(goalAxis, 1), P(resourceAxis, 0), actionAxis, goalAxis, featureAxis));
        AssertOpen(FeatureMemory.InferFromOutcome(P(actionAxis, 1), P(goalAxis, 1), NativeGraph.Open(new Placeholder(), new Placeholder()), actionAxis, goalAxis, featureAxis));
        var capacity = FiniteReadSurface.Capacity(new Carrier(), resourceAxis, -16, 16, 16);
        var uncertain = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(resourceAxis, 1), new Placeholder());
        AssertOpen(FeatureMemory.InferFromOutcome(P(actionAxis, 1), P(goalAxis, 1), uncertain, actionAxis, goalAxis, featureAxis));
    }

    private static Carrier SignAxis(Relation product)
    {
        // Product operand ports remain discoverable in their native ordered
        // port tree. The last port's declared source frame grounds the sign.
        var current = product.Left;
        while (current is Relation relation && relation.Right is not Carrier && relation.Left is Relation)
        {
            if (relation.Right is Relation port && port.Left is Relation frame && NativeGraph.TryPoint(frame.Left, out var point)) return point.Carrier;
            current = relation.Right;
        }
        throw new InvalidOperationException("Product sign port not discovered.");
    }
    private static FeatureMemory Create() => new(new(), new(), new(), new());
    private static Relation P(Carrier axis, long ticks) => NativeGraph.Publication(axis, ticks, new Carrier());
    private static long Value(Relation publication, Carrier axis) { Assert.True(NativeGraph.TryValue(publication, axis, out var value)); return value.Ticks; }
    private static void AssertOpen(Relation publication) { Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out var residue, out _)); Assert.IsType<Placeholder>(result); Assert.IsType<Relation>(residue); }
}
