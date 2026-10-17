using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace UtilityEcology.Tests;

public sealed class LearningGateTests
{
    [Fact]
    public void InitialUnknownIsOpenAndFundedInvestigationDoesNotPublishSensorZero()
    {
        var gate = Create(); AssertOpen(gate.ReadError()); Assert.Equal(0UL, gate.EvidenceCount);
        Assert.True(Permit(gate, 0, 0)); Assert.Equal(98UL, gate.Attention.Value);
        Assert.Equal(1UL, gate.InspectionCount); Assert.Equal(1UL, gate.GrantCount); Assert.Equal(2UL, gate.Spent.Value); AssertOpen(gate.ReadError());
        Assert.True(ReadWitness.TryRead(gate.LastDecision, out _, out var policy, out _, out var unresolved, out var provenance));
        Assert.IsType<Relation>(policy); Assert.IsType<Relation>(unresolved);
        Assert.True(TimeStepSequence.TryReadStep((Relation)provenance, out _, out _, out _, out _, out _, out _, out _));
    }

    [Fact]
    public void SupportedLowErrorSettlesToPaidPeriodicTestsAndHighErrorReopensImmediately()
    {
        var gate = Create(); Settle(gate);
        var permitted = Enumerable.Range(0, 32).Count(_ => Permit(gate, 0, 0));
        Assert.Equal(2, permitted); Assert.Equal(32UL, gate.InspectionCount); Assert.Equal(2UL, gate.GrantCount);
        gate.ObservePrediction(P(gate.ResourceAxis, 8), P(gate.ResourceAxis, -12));
        Assert.True(Permit(gate, 0, 0));
        Assert.True(NativeGraph.TryValue(gate.ReadError(), gate.MeanErrorAxis, out var mean)); Assert.True(mean.Ticks > LearningGate.LowMeanError256);
    }

    [Fact]
    public void UnknownPredictionAfterSuccessRemainsPressureRatherThanAZeroErrorObservation()
    {
        var gate = Create(); Settle(gate); Permit(gate, 0, 0);
        var count = gate.EvidenceCount;
        gate.ObservePrediction(NativeGraph.Open(new Placeholder(), new Placeholder()), P(gate.ResourceAxis, 8));
        Assert.Equal(count, gate.EvidenceCount); Assert.Equal(1UL, gate.UnknownObservationCount); Assert.True(Permit(gate, 0, 0));
        Assert.True(ReadWitness.TryRead(gate.LastFeedback, out _, out _, out var latest, out _, out _)); AssertOpen((Relation)latest);
    }

    [Fact]
    public void OpportunityRequiresFundedDemandAndUninterpretedTrafficAtADeclaredCycle()
    {
        var opportunity = Create(); var errorOnly = Create(opportunity: false);
        Settle(opportunity); Settle(errorOnly);
        Assert.Equal(4, Enumerable.Range(0, 32).Count(_ => Permit(opportunity, 1, 1)));
        Assert.Equal(2, Enumerable.Range(0, 32).Count(_ => Permit(errorOnly, 1, 1)));
        var noDemand = Create(); Settle(noDemand);
        Assert.Equal(2, Enumerable.Range(0, 32).Count(_ => Permit(noDemand, 100, 0)));
        var noTraffic = Create(); Settle(noTraffic);
        Assert.Equal(2, Enumerable.Range(0, 32).Count(_ => Permit(noTraffic, 0, 100)));
    }

    [Fact]
    public void IrreducibleErrorAndNoiseTrafficCannotConsumeBeyondAttentionCapacity()
    {
        var gate = LearningGate.Create(new Carrier(), 5);
        gate.ObservePrediction(P(gate.ResourceAxis, 1), P(gate.ResourceAxis, -1));
        Assert.True(Permit(gate, 100, 100)); Assert.True(Permit(gate, 100, 100));
        Assert.False(Permit(gate, 100, 100)); Assert.False(Permit(gate, 100, 100));
        Assert.Equal(0UL, gate.Attention.Value); Assert.Equal(0, gate.Attention.Tension);
        Assert.Equal(3UL, gate.InspectionCount); Assert.Equal(2UL, gate.GrantCount);
    }

    [Fact]
    public void RareUseIsNotAnImplicitDeletionOrEvidenceDecayLaw()
    {
        var gate = LearningGate.Create(new Carrier(), 1000); Settle(gate);
        var ledger = Assert.IsType<Relation>(NativeGraph.Parts(gate.Root, 19)[3]);
        var before = NativeInspection.Signature(ledger);
        for (var index = 0; index < 100; index++) Permit(gate, 0, 0);
        Assert.Equal(before, NativeInspection.Signature(ledger)); Assert.Equal(4UL, gate.EvidenceCount);
        Assert.True(gate.Attention.Value > 0);
    }

    [Fact]
    public void DuplicatePhysicalFeedbackAndFundingAreExcludedWithinTheirBoundedReceiptWindows()
    {
        var gate = Create(); var physical = P(gate.ResourceAxis, 8);
        gate.ObservePrediction(P(gate.ResourceAxis, 8), physical); Assert.Equal(1UL, gate.EvidenceCount);
        AssertOpen(gate.ObservePrediction(P(gate.ResourceAxis, 0), physical)); Assert.Equal(1UL, gate.EvidenceCount);
        var credit = P(gate.ResourceAxis, 3); gate.CreditAttention(credit); Assert.Equal(103UL, gate.Attention.Value);
        AssertOpen(gate.CreditAttention(credit)); Assert.Equal(103UL, gate.Attention.Value);
        for (var index = 0; index < LearningGate.ReceiptCapacity; index++) gate.CreditAttention(P(gate.ResourceAxis, 1));
        gate.CreditAttention(credit); Assert.Equal(114UL, gate.Attention.Value);
        var counters = NativeGraph.Parts((Relation)NativeGraph.Parts(gate.Root, 19)[16], 6);
        Assert.Equal(2UL, Assert.IsType<Carrier>(counters[4]).Value);
    }

    [Fact]
    public void NegativeFundingRetainsUnpaidDrawAndLaterFundingSettlesItBeforeGranting()
    {
        var gate = LearningGate.Create(new Carrier(), 2);
        gate.CreditAttention(P(gate.ResourceAxis, -5)); Assert.Equal(0UL, gate.Attention.Value); Assert.Equal(-3, gate.Attention.Tension);
        Assert.False(Permit(gate, 1, 1)); gate.CreditAttention(P(gate.ResourceAxis, 4));
        Assert.Equal(1UL, gate.Attention.Value); Assert.Equal(0, gate.Attention.Tension); Assert.False(Permit(gate, 1, 1));
        gate.CreditAttention(P(gate.ResourceAxis, 2)); Assert.True(Permit(gate, 1, 1));
    }

    [Fact]
    public void CoarseOrWrongBasisFundingCannotBecomeNominalExactBudget()
    {
        var gate = Create(); var wrong = P(new Carrier(), 100); AssertOpen(gate.CreditAttention(wrong));
        var capacity = FiniteReadSurface.Capacity(new Carrier(), gate.ResourceAxis, -16, 16, 16);
        var uncertain = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(gate.ResourceAxis, 1), new Placeholder());
        AssertOpen(gate.CreditAttention(uncertain)); Assert.Equal(100UL, gate.Attention.Value);
    }

    [Fact]
    public void NativeTransportPreservesSharedBudgetCyclesRulesAndFeedbackReceipts()
    {
        var gate = Create(); Settle(gate); Permit(gate, 1, 1); var oldCredit = P(gate.ResourceAxis, 5); gate.CreditAttention(oldCredit);
        var restoredRoot = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(gate.Root))));
        var restored = LearningGate.FromRoot(restoredRoot);
        Assert.Equal(NativeInspection.Signature(gate.Root), NativeInspection.Signature(restored.Root));
        Assert.Equal(gate.Attention.Value, restored.Attention.Value); Assert.Equal(gate.GrantCount, restored.GrantCount);
        Assert.Equal(Permit(gate, 1, 1), Permit(restored, 1, 1));
        Assert.Equal(gate.Attention.Value, restored.Attention.Value);
        var receipts = NativeGraph.Parts((Relation)NativeGraph.Parts(restored.Root, 19)[15], 2);
        var creditReceipt = Assert.IsType<Relation>(NativeGraph.Parts((Relation)receipts[1])[0]);
        Assert.True(ReadWitness.TryRead(creditReceipt, out var identity, out _, out _, out _, out _));
        var replay = NativeGraph.Publication(restored.ResourceAxis, 5, identity);
        var before = restored.Attention.Value; AssertOpen(restored.CreditAttention(replay)); Assert.Equal(before, restored.Attention.Value);
        var policy = Assert.IsType<Relation>(NativeGraph.Parts(restored.Root, 19)[9]);
        Assert.Equal(6, NativeGraph.Parts(policy).Length);
    }

    [Fact]
    public void FixedAlwaysLearningControlUsesTheSamePaidAdmissionLaw()
    {
        var gate = LearningGate.Create(new Carrier(), 100, always: true); Settle(gate);
        Assert.Equal(10, Enumerable.Range(0, 10).Count(_ => Permit(gate, 0, 0))); Assert.Equal(80UL, gate.Attention.Value);
    }

    [Fact]
    public void ZeroEndowmentWaitsForFundingAndSpentPlusReserveConservesPaidPackets()
    {
        var gate = LearningGate.Create(new Carrier(), 0); Assert.Equal(0, gate.InitialCapital);
        Assert.False(Permit(gate, 1, 1)); Assert.Equal(0UL, gate.Spent.Value); AssertOpen(gate.ReadError());
        gate.CreditAttention(P(gate.ResourceAxis, 6));
        Assert.True(Permit(gate, 1, 1)); Assert.True(Permit(gate, 1, 1)); Assert.True(Permit(gate, 1, 1));
        Assert.Equal(6UL, gate.Spent.Value); Assert.Equal(0UL, gate.Attention.Value);
        Assert.False(Permit(gate, 1, 1)); Assert.Equal(6UL, gate.Spent.Value);
    }

    private static LearningGate Create(bool opportunity = true) => LearningGate.Create(new Carrier(), 100, opportunity: opportunity);
    private static void Settle(LearningGate gate)
    { for (var index = 0; index < LearningGate.MinimumEvidence; index++) gate.ObservePrediction(P(gate.ResourceAxis, 8), P(gate.ResourceAxis, 8)); }
    private static bool Permit(LearningGate gate, long traffic, long demand)
    { Assert.True(gate.TryPermission(gate.Decide(P(gate.TrafficAxis, traffic), P(gate.DemandAxis, demand)), out var permitted)); return permitted; }
    private static Relation P(Carrier axis, long ticks, IElement? source = null) => NativeGraph.Publication(axis, ticks, source ?? new Carrier());
    private static void AssertOpen(Relation publication)
    { Assert.True(ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result); }
}
