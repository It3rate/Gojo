using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;

namespace AdaptiveLifecycle.Tests;

public sealed class LifecycleSurfaceTests
{
    [Fact]
    public void DormantEntitySpendsReserveThenWithdrawsWithoutErasingBodyOrDebt()
    {
        var (seed, source, receiver, connection) = Seed(); source.Set(3);
        var life = LocalLifeBudget.Create(new Carrier(), new Carrier(), seed, 3);
        Assert.True(life.Viable); Assert.True(life.Participating); Assert.Equal(6UL, receiver.Value);
        life.Advance(2); Assert.Equal(1UL, life.Reserve.Value); Assert.True(life.Participating);
        var step = life.Advance(2);
        Assert.Equal(0UL, life.Reserve.Value); Assert.Equal(-1, life.Reserve.Tension);
        Assert.False(life.Viable); Assert.False(life.Participating);
        Assert.Same(connection, life.Participation);
        source.Set(9); Assert.Equal(6UL, receiver.Value); Assert.False(source.IsAttached(connection));
        Assert.True(TimeStepSequence.TryReadStep(step, out _, out var start, out var end, out _, out _, out _, out var unresolved));
        Assert.Equal(2, start.Ticks); Assert.Equal(4, end.Ticks);
        Assert.Contains(NativeGraph.Parts((Relation)unresolved), element => element is CarrierLocation { Ticks: -1 });
    }

    [Fact]
    public void EarnedPhysicalCreditsPayMaintenanceAndRareUseCanPreserveReserve()
    {
        var axis = new Carrier(); var life = LocalLifeBudget.Create(axis, new Carrier(), Seed().Seed, 12, 2);
        life.Advance(5); Assert.Equal(2UL, life.Reserve.Value); Assert.True(life.Viable);
        life.Credit(P(axis, 10)); life.Advance(5);
        Assert.Equal(2UL, life.Reserve.Value); Assert.True(life.Viable); Assert.True(life.Participating);
        // No usage counter controls survival: a valuable infrequent receipt
        // can fund the same elapsed maintenance as frequent receipts.
        Assert.Equal(10UL, life.Clock.Value);
    }

    [Fact]
    public void CreditPaysDebtButOnlyExplicitResumeRestoresParticipation()
    {
        var axis = new Carrier(); var life = LocalLifeBudget.Create(axis, new Carrier(), Seed().Seed, 2);
        life.Advance(5); Assert.Equal(-3, life.Reserve.Tension);
        var refusal = life.Resume(); Assert.True(ReadWitness.TryRead(refusal, out _, out _, out var open, out _, out _)); Assert.IsType<Placeholder>(open);
        life.Credit(P(axis, 2)); Assert.Equal(-1, life.Reserve.Tension); Assert.Equal(0UL, life.Reserve.Value); Assert.False(life.Viable);
        life.Credit(P(axis, 3)); Assert.Equal(0, life.Reserve.Tension); Assert.Equal(2UL, life.Reserve.Value);
        Assert.True(life.Viable); Assert.False(life.Participating);
        life.Resume(); Assert.True(life.Participating);
    }

    [Fact]
    public void MissingResourceDoesNotInventCreditAndRecentDuplicateCannotMintMore()
    {
        var axis = new Carrier(); var life = LocalLifeBudget.Create(axis, new Carrier(), Seed().Seed, 2);
        var physical = P(axis, 3); life.Credit(physical); life.Credit(physical);
        Assert.Equal(5UL, life.Reserve.Value);
        var missing = NativeGraph.Open(new Carrier(), Relation.Segment(axis, -16, 16));
        var result = life.Credit(missing); Assert.True(ReadWitness.TryRead(result, out _, out _, out var open, out _, out _));
        Assert.IsType<Placeholder>(open); Assert.Equal(5UL, life.Reserve.Value);
    }

    [Fact]
    public void EvidenceAndInternalBodyRemainUnchangedByStructuralStarvation()
    {
        var (seed, _, _, _) = Seed(); var body = (Relation)NativeGraph.Parts(seed, 3)[0];
        var evidence = (Carrier)NativeGraph.Parts(body)[2]; evidence.Add(7);
        var life = LocalLifeBudget.Create(new Carrier(), new Carrier(), seed, 1);
        life.Advance(3);
        Assert.Same(body, life.Body); Assert.Equal(7UL, evidence.Value); Assert.Equal(-2, life.Reserve.Tension);
        Assert.False(life.Participating);
    }

    [Fact]
    public void WithdrawnBudgetTransportPreservesSharedSitesAndDoesNotReactivateDuringLoad()
    {
        var axis = new Carrier(); var (seed, source, receiver, _) = Seed(); source.Set(4);
        var life = LocalLifeBudget.Create(axis, new Carrier(), seed, 2); life.Advance(5);
        var restored = LocalLifeBudget.FromRoot(Roundtrip(life.Root));
        var body = NativeGraph.Parts(restored.Body); var rs = (Carrier)body[0]; var rt = (Carrier)body[1];
        Assert.Same(rs, restored.ParticipationOwner); Assert.Equal(receiver.Value, rt.Value);
        Assert.False(restored.Participating); Assert.Equal(-3, restored.Reserve.Tension);
        rs.Set(7); Assert.Equal(8UL, rt.Value);
        restored.Credit(P(restored.ResourceAxis, 6)); restored.Resume(); Assert.Equal(14UL, rt.Value);
        Assert.True(restored.Participating); Assert.NotSame(source, rs); Assert.Equal(4UL, source.Value);
        var rr = (Relation)body[3]; Assert.Same(restored.Participation, rr);
    }

    [Fact]
    public void NurseryBirthIsFreshFundedConnectedAndCapacityBounded()
    {
        var axis = new Carrier(); var nursery = new BoundedNursery(axis, new Carrier(), 20, 1, 6);
        var (seed, source, receiver, originalConnection) = Seed(); source.Set(3);
        Assert.True(nursery.TryBirth(seed, out var born, out var birth)); Assert.NotNull(born);
        Assert.Equal(14UL, nursery.Funding.Value); Assert.Equal(6UL, born.Reserve.Value); Assert.True(born.Participating);
        Assert.Equal(1, nursery.ActiveCount); Assert.Equal(1UL, nursery.BirthCount);
        var parts = NativeGraph.Parts(born.Body); var bs = (Carrier)parts[0]; var br = (Carrier)parts[1];
        Assert.NotSame(source, bs); Assert.NotSame(receiver, br); Assert.NotSame(originalConnection, born.Participation);
        Assert.Same(bs, born.ParticipationOwner); Assert.Same((Relation)parts[3], born.Participation);
        bs.Set(5); Assert.Equal(10UL, br.Value); Assert.Equal(6UL, receiver.Value);
        Assert.True(ReadWitness.TryRead(birth, out var selected, out _, out var actual, out _, out _));
        Assert.Same(seed, selected); Assert.IsType<Relation>(actual);
        Assert.False(nursery.TryBirth(seed, out var excess, out var denied)); Assert.Null(excess);
        Assert.Equal(14UL, nursery.Funding.Value); Assert.True(ReadWitness.TryRead(denied, out _, out _, out var open, out _, out _)); Assert.IsType<Placeholder>(open);
    }

    [Fact]
    public void InsufficientNurseryFundsLeaveOpenWithoutConsumingSlotOrSeed()
    {
        var nursery = new BoundedNursery(new Carrier(), new Carrier(), 3, 2, 4); var (seed, source, _, connection) = Seed();
        Assert.False(nursery.TryBirth(seed, out var child, out var witness)); Assert.Null(child);
        Assert.Equal(3UL, nursery.Funding.Value); Assert.Equal(0, nursery.ActiveCount); Assert.True(source.IsAttached(connection));
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var open, out _, out _)); Assert.IsType<Placeholder>(open);
    }

    [Fact]
    public void RetiringStarvedChildFreesSlotForNewIdentityAndPreservesOldShortfall()
    {
        var nursery = new BoundedNursery(new Carrier(), new Carrier(), 20, 1, 5);
        var seed = Seed().Seed; Assert.True(nursery.TryBirth(seed, out var old, out _)); Assert.NotNull(old);
        var activeRefusal = nursery.Retire(old); Assert.Equal(1, nursery.ActiveCount);
        Assert.True(ReadWitness.TryRead(activeRefusal, out _, out _, out var stillOpen, out _, out _)); Assert.IsType<Placeholder>(stillOpen);
        old.Advance(7); Assert.False(old.Participating); var retirement = nursery.Retire(old);
        Assert.Equal(0, nursery.ActiveCount); Assert.Equal(1UL, nursery.RetirementCount); Assert.Equal(-2, old.Reserve.Tension);
        Assert.True(nursery.TryBirth(seed, out var replacement, out _)); Assert.NotNull(replacement);
        Assert.Equal(10UL, nursery.Funding.Value); Assert.NotSame(old.Reserve, replacement.Reserve);
        Assert.Equal(5UL, replacement.Reserve.Value); Assert.True(replacement.Participating); Assert.False(old.Participating);
        Assert.True(ReadWitness.TryRead(retirement, out var retired, out _, out _, out var debt, out _));
        Assert.IsType<Relation>(retired); Assert.Equal(-2, Assert.IsType<CarrierLocation>(debt).Ticks);
        Assert.Equal(2UL, nursery.BirthCount); Assert.Equal(1, nursery.ActiveCount);
    }

    [Fact]
    public void NurseryRestartPreservesOccupiedSlotsFundingAndCanContinueRetirementBirth()
    {
        var nursery = new BoundedNursery(new Carrier(), new Carrier(), 20, 2, 4);
        Assert.True(nursery.TryBirth(Seed().Seed, out var child, out _)); Assert.NotNull(child);
        child.Advance(1); var restored = BoundedNursery.FromRoot(Roundtrip(nursery.Root));
        Assert.Equal(16UL, restored.Funding.Value); Assert.Equal(1, restored.ActiveCount);
        var occupant = Assert.Single(restored.Slots); Assert.Equal(3UL, occupant.Reserve.Value);
        Assert.Same(restored.ResourceAxis, occupant.ResourceAxis); Assert.Same(restored.TimeAxis, occupant.TimeAxis);
        occupant.Advance(4); restored.Retire(occupant); Assert.Equal(0, restored.ActiveCount);
        Assert.True(restored.TryBirth(Seed().Seed, out _, out _)); Assert.Equal(12UL, restored.Funding.Value);
        Assert.Equal(2UL, restored.BirthCount); Assert.Equal(1UL, restored.RetirementCount);
        Assert.Equal(1, nursery.ActiveCount); Assert.Equal(3UL, child.Reserve.Value);
    }

    [Fact]
    public void ClippedEndowmentAndFurtherDrawRemainAccountedAsCarrierTension()
    {
        var axis = new Carrier(); var life = LocalLifeBudget.Create(axis, new Carrier(), Seed().Seed, 4, capacity: 5);
        life.Credit(P(axis, 4)); Assert.Equal(5UL, life.Reserve.Value); Assert.Equal(3, life.Reserve.Tension);
        life.Advance(7); Assert.Equal(1UL, life.Reserve.Value); Assert.Equal(0, life.Reserve.Tension); Assert.True(life.Viable);
        life.Advance(3); Assert.Equal(0UL, life.Reserve.Value); Assert.Equal(-2, life.Reserve.Tension); Assert.False(life.Viable);
    }

    [Fact]
    public void RepeatedReplacementRetainsBoundedNativeGraphAndCountsForgottenEvents()
    {
        var nursery = new BoundedNursery(new Carrier(), new Carrier(), 1000, 1, 4);
        var seed = Seed().Seed; var counts = new List<(int Carriers, int Relations)>();
        for (var generation = 0; generation < 100; generation++)
        {
            Assert.True(nursery.TryBirth(seed, out var child, out _)); Assert.NotNull(child);
            child.Advance(5); nursery.Retire(child);
            if (generation is 9 or 49 or 99)
            {
                var graph = GojoEncodedSnapshot.CaptureGraph(nursery.Root);
                counts.Add((graph.Carriers.Length, graph.Relations.Length));
            }
        }
        Assert.Equal(counts[0], counts[1]); Assert.Equal(counts[0], counts[2]);
        Assert.Equal(100UL, nursery.BirthCount); Assert.Equal(100UL, nursery.RetirementCount);
        Assert.Equal(198UL, nursery.ForgottenEventCount); Assert.Equal(600UL, nursery.Funding.Value);
        Assert.Equal(100UL, nursery.RetiredShortfall.Value);
        var restored = BoundedNursery.FromRoot(Roundtrip(nursery.Root));
        Assert.Equal(100UL, restored.RetiredShortfall.Value);
        Assert.True(restored.ResourceAxis.UnitShape.CompatibleWith(restored.RetiredShortfall.UnitShape));
    }

    [Fact]
    public void LateFundingRequiresExplicitResumeAndDoesNotRewriteCapturedRetirementDebt()
    {
        var axis = new Carrier(); var nursery = new BoundedNursery(axis, new Carrier(), 8, 1, 4);
        Assert.True(nursery.TryBirth(Seed().Seed, out var retired, out _)); Assert.NotNull(retired);
        retired.Advance(6); var witness = nursery.Retire(retired);
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out _, out var debt, out var provenance));
        Assert.Equal(-2, Assert.IsType<CarrierLocation>(debt).Ticks);
        var fields = NativeGraph.Parts((Relation)provenance, 4);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(fields[1]).Ticks);
        retired.Credit(P(axis, 5)); Assert.True(retired.Viable); Assert.False(retired.Participating);
        retired.Resume(); Assert.True(retired.Participating); Assert.Equal(3UL, retired.Reserve.Value);
        Assert.Equal(-2, Assert.IsType<CarrierLocation>(debt).Ticks);
        Assert.Equal(2UL, nursery.RetiredShortfall.Value); // late recovery is not a rewrite of the earlier observation
        Assert.True(nursery.TryBirth(Seed().Seed, out var replacement, out _)); Assert.NotNull(replacement);
        Assert.Equal(0, Assert.IsType<CarrierLocation>(fields[1]).Ticks); // new occupancy must not rewrite the historical release
        Assert.Equal(1, nursery.ActiveCount); Assert.NotSame(retired.Reserve, replacement.Reserve);
        // Native retirement removes participation and slot ownership; it is
        // not an immutable process-global tombstone. External re-funding is
        // an explicit new action outside this nursery's occupied scope.
    }

    [Fact]
    public void UnrepresentablePhysicalCreditAndDebtStayOpenWithoutPartialAdmission()
    {
        var axis = new Carrier(); var life = LocalLifeBudget.Create(axis, new Carrier(), Seed().Seed, 4);
        var credit = life.Credit(P(axis, long.MaxValue));
        Assert.True(ReadWitness.TryRead(credit, out _, out _, out var creditResult, out _, out _));
        Assert.IsType<Placeholder>(creditResult); Assert.Equal(4UL, life.Reserve.Value); Assert.Equal(0, life.Reserve.Tension);
        life.Advance(5); Assert.Equal(-1, life.Reserve.Tension);
        // The clock prevents a range-overflowing elapsed packet from being
        // partially admitted. Rejection does not erase retained shortfall.
        var draw = life.Advance(long.MaxValue);
        Assert.True(ReadWitness.TryRead(draw, out _, out _, out var drawResult, out _, out _));
        Assert.IsType<Placeholder>(drawResult); Assert.Equal(5UL, life.Clock.Value); Assert.Equal(-1, life.Reserve.Tension);
    }

    [Fact]
    public void RetirementAggregatesUnpaidMaintenanceWithoutTransferringOrChargingDebtAgain()
    {
        var nursery = new BoundedNursery(new Carrier(), new Carrier(), 8, 1, 4);
        Assert.True(nursery.TryBirth(Seed().Seed, out var first, out _)); Assert.NotNull(first);
        first.Advance(6); nursery.Retire(first);
        Assert.Equal(2UL, nursery.RetiredShortfall.Value); Assert.Equal(-2, first.Reserve.Tension);
        Assert.True(nursery.TryBirth(Seed().Seed, out var second, out _)); Assert.NotNull(second);
        second.Advance(7); var retired = nursery.Retire(second);
        Assert.Equal(5UL, nursery.RetiredShortfall.Value); Assert.Equal(-3, second.Reserve.Tension); Assert.Equal(-2, first.Reserve.Tension);
        Assert.Equal(0UL, nursery.Funding.Value); // only the two paid endowments were charged
        nursery.Retire(second); Assert.Equal(5UL, nursery.RetiredShortfall.Value); // absent slot ownership cannot double count
        Assert.True(ReadWitness.TryRead(retired, out _, out _, out _, out _, out var provenance));
        var aggregation = NativeGraph.Parts((Relation)NativeGraph.Parts((Relation)provenance, 4)[2], 7);
        Assert.Equal(2, Assert.IsType<CarrierLocation>(aggregation[5]).Ticks);
        Assert.Equal(5, Assert.IsType<CarrierLocation>(aggregation[6]).Ticks);
        var restored = BoundedNursery.FromRoot(Roundtrip(nursery.Root));
        Assert.Equal(5UL, restored.RetiredShortfall.Value); Assert.Equal(2UL, restored.RetirementCount);
        Assert.NotSame(nursery.RetiredShortfall, restored.RetiredShortfall);
    }

    [Fact]
    public void HistoricalShortfallCapacityFailureDoesNotPartiallyRetireOrEraseDebt()
    {
        var nursery = new BoundedNursery(new Carrier(), new Carrier(), 4, 1, 4);
        Assert.True(nursery.TryBirth(Seed().Seed, out var child, out _)); Assert.NotNull(child);
        child.Advance(6); nursery.RetiredShortfall.Add(checked((ulong)(long.MaxValue - 1)));
        var refusal = nursery.Retire(child);
        Assert.True(ReadWitness.TryRead(refusal, out _, out _, out var open, out _, out _)); Assert.IsType<Placeholder>(open);
        Assert.Equal(1, nursery.ActiveCount); Assert.Same(child, Assert.Single(nursery.Slots));
        Assert.Equal(0UL, nursery.RetirementCount); Assert.Equal(0UL, nursery.ForgottenEventCount);
        Assert.Equal(checked((ulong)(long.MaxValue - 1)), nursery.RetiredShortfall.Value);
        Assert.Equal(0, nursery.RetiredShortfall.Tension); Assert.Equal(-2, child.Reserve.Tension);
        Assert.False(child.Participating); Assert.Equal(0UL, nursery.Funding.Value);
    }

    private static (Relation Seed, Carrier Source, Carrier Receiver, Relation Connection) Seed()
    {
        var source = new Carrier(); var receiver = new Carrier(); var evidence = new Carrier();
        var connection = source.AttachLiveOutput(source.ValueSlot,
            new Relation(Relation.Segment(source, 0, 1), Relation.Segment(receiver, 0, 2)));
        var body = NativeGraph.G(source, receiver, evidence, connection);
        return (LifecycleSeed.Create(body, source, connection), source, receiver, connection);
    }
    private static Relation P(Carrier axis, long value) => NativeGraph.Publication(axis, value, new Carrier());
    private static Relation Roundtrip(Relation root) => (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
}
