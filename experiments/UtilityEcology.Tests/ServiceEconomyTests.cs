using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;

namespace UtilityEcology.Tests;

public sealed class ServiceEconomyTests
{
    [Fact]
    public void EmptyServiceUtilityAndPublicationRemainUnknown()
    {
        var economy = Economy(); var service = Add(economy, 2, 3);
        AssertOpen(service.QuoteUtility()); Assert.Equal(0UL, NativeGraph.Count(service.UtilityLedger).Value);
        Assert.Equal(17UL, economy.Buyer.Value); Assert.Equal(3UL, economy.CostSink.Value);
        Assert.Equal(0UL, service.Wallet.Value); Conserved(economy);
        Assert.True(economy.TryPurchase(service, Command(), Open(), 1, out var purchase, out _));
        AssertOpen(purchase!.Publication); Assert.Equal(2UL, service.Wallet.Value);
        Assert.Equal(4UL, economy.CostSink.Value); Conserved(economy);
    }

    [Fact]
    public void UpfrontFeeConservesResourceAndIndependentOutcomeEstablishesUtility()
    {
        var economy = Economy(); var service = Add(economy, 2, 3); var command = Command();
        Assert.True(economy.TryPurchase(service, command, P(new Carrier(), 1), 1, out var purchase, out _));
        Assert.Equal(14UL, economy.Buyer.Value); Assert.Equal(2UL, service.Wallet.Value);
        var feedback = NativeGraph.BindFeedback(command, P(economy.ResourceAxis, 8), economy.ResourceAxis);
        var receipt = economy.Settle(purchase!, feedback);
        Assert.False(purchase!.Pending); Assert.Equal(22UL, economy.Buyer.Value); Assert.Equal(2UL, service.Wallet.Value);
        Assert.Equal(1UL, NativeGraph.Count(service.UtilityLedger).Value);
        Assert.True(NativeGraph.TryValue(service.QuoteUtility(), service.UtilityAxis, out var score)); Assert.Equal(2048, score.Ticks);
        Assert.True(ReadWitness.TryRead(receipt, out _, out var delivered, out _, out _, out _));
        Assert.Same(purchase.Publication, delivered); Conserved(economy);
    }

    [Theory]
    [InlineData(8, 2, 22)]
    [InlineData(-12, 0, 4)]
    [InlineData(0, 0, 16)]
    public void SuccessPaidLawReleasesOrRefundsNativeEscrow(long physical, ulong paid, ulong buyer)
    {
        var economy = Economy(); var service = Add(economy, 2, 3, true); var command = Command();
        Assert.True(economy.TryPurchase(service, command, P(new Carrier(), 1), 1, out var purchase, out _));
        Assert.Equal(2UL, purchase!.Escrow.Value); Assert.Equal(0UL, service.Wallet.Value); Conserved(economy);
        economy.Settle(purchase, NativeGraph.BindFeedback(command, P(economy.ResourceAxis, physical), economy.ResourceAxis));
        Assert.Equal(0UL, purchase.Escrow.Value); Assert.Equal(paid, service.Wallet.Value); Assert.Equal(buyer, economy.Buyer.Value);
        Assert.Equal(4UL, economy.CostSink.Value); Conserved(economy);
    }

    [Fact]
    public void UnboundWrongCommandAndRepeatedPhysicalFeedbackCannotMintMoneyOrEvidence()
    {
        var economy = Economy(); var service = Add(economy, 2, 3, true); var command = Command();
        Assert.True(economy.TryPurchase(service, command, P(new Carrier(), 1), 1, out var purchase, out _));
        var physical = P(economy.ResourceAxis, 8); var other = NativeGraph.BindFeedback(Command(), physical, economy.ResourceAxis);
        AssertOpen(economy.Settle(purchase!, physical)); AssertOpen(economy.Settle(purchase!, other));
        Assert.True(purchase!.Pending); Assert.Equal(0UL, NativeGraph.Count(service.UtilityLedger).Value); Conserved(economy);
        var bound = NativeGraph.BindFeedback(command, physical, economy.ResourceAxis); economy.Settle(purchase, bound);
        var total = economy.AccountedMoney; AssertOpen(economy.Settle(purchase, bound)); Assert.Equal(total, economy.AccountedMoney);
        Assert.True(economy.TryPurchase(service, Command(), P(new Carrier(), 1), 0, out var second, out _));
        AssertOpen(economy.Settle(second!, NativeGraph.BindFeedback(second!.Command, physical, economy.ResourceAxis)));
        Assert.Equal(1UL, NativeGraph.Count(service.UtilityLedger).Value); Conserved(economy);
    }

    [Fact]
    public void MissingPhysicalOutcomeLeavesEscrowFundedAndCancellationRefundsOnlyFee()
    {
        var economy = Economy(); var service = Add(economy, 2, 3, true);
        Assert.True(economy.TryPurchase(service, Command(), Open(), 1, out var purchase, out _));
        AssertOpen(economy.Settle(purchase!, Open())); Assert.True(purchase!.Pending); Assert.Equal(2UL, purchase.Escrow.Value);
        economy.Cancel(purchase); Assert.False(purchase.Pending); Assert.Equal(16UL, economy.Buyer.Value);
        Assert.Equal(4UL, economy.CostSink.Value); AssertOpen(service.QuoteUtility()); Conserved(economy);
        var total = economy.AccountedMoney; AssertOpen(economy.Cancel(purchase)); Assert.Equal(total, economy.AccountedMoney);
    }

    [Fact]
    public void InadequateConstructionAndPurchaseFundingLeaveAllMoneyUnchanged()
    {
        var economy = Economy(2); var body = NativeGraph.G(new Placeholder());
        Assert.False(economy.AddService(body, 1, 3, ServiceEconomy.UpfrontLaw(economy.ResourceAxis), out _, out var blocked));
        AssertOpen(blocked); Assert.Empty(economy.Services); Assert.Equal(0UL, economy.CostSink.Value);
        var service = Add(economy, 2, 1); var before = economy.AccountedMoney;
        Assert.False(economy.TryPurchase(service, Command(), Open(), 1, out _, out blocked)); AssertOpen(blocked);
        Assert.Equal(before, economy.AccountedMoney); Assert.Equal(0UL, service.Wallet.Value); Conserved(economy);
    }

    [Fact]
    public void SlotCapacityBlocksProposalAndRetirementReleasesItsReserve()
    {
        var economy = new ServiceEconomy(new Carrier(), new Carrier(), 20, 1); var service = Add(economy, 2, 3);
        economy.Sponsor(service, 5);
        Assert.False(economy.AddService(NativeGraph.G(), 1, 1, ServiceEconomy.UpfrontLaw(economy.ResourceAxis), out _, out _));
        economy.Retire(service); Assert.Empty(economy.Services); Assert.Equal(17UL, economy.Buyer.Value); Conserved(economy);
        Assert.True(economy.AddService(NativeGraph.G(), 1, 1, ServiceEconomy.UpfrontLaw(economy.ResourceAxis), out _, out _));
        Assert.Equal(16UL, economy.Buyer.Value); Conserved(economy);
    }

    [Fact]
    public void PendingEscrowBlocksServiceRetirement()
    {
        var economy = Economy(); var service = Add(economy, 2, 3, true);
        economy.TryPurchase(service, Command(), Open(), 1, out var purchase, out _);
        AssertOpen(economy.Retire(service)); Assert.Single(economy.Services);
        economy.Cancel(purchase!); economy.Retire(service); Assert.Empty(economy.Services); Conserved(economy);
    }

    [Fact]
    public void RareServiceCanParkThroughLongQuietGapThenResumeWithoutForgettingUtility()
    {
        var economy = Economy(100); var service = Add(economy, 2, 3, activeRent: 2, parkedRent: 0);
        economy.Sponsor(service, 10); var command = Command();
        economy.TryPurchase(service, command, P(new Carrier(), 1), 1, out var purchase, out _);
        economy.Settle(purchase!, NativeGraph.BindFeedback(command, P(economy.ResourceAxis, 8), economy.ResourceAxis));
        var score = Score(service); var reserve = service.Wallet.Value; economy.Park(service); economy.Advance(1000);
        Assert.False(service.Active); Assert.Equal(reserve, service.Wallet.Value); Assert.Equal(score, Score(service));
        Assert.False(economy.TryPurchase(service, Command(), Open(), 1, out _, out _));
        economy.Revive(service); Assert.True(service.Active); economy.Advance(3);
        Assert.Equal(reserve - 6, service.Wallet.Value); Assert.Equal(score, Score(service)); Conserved(economy);
    }

    [Fact]
    public void UnpaidRentIsDemandTensionAndDoesNotBecomeSpendableCurrency()
    {
        var economy = Economy(); var service = Add(economy, 2, 3, activeRent: 3); economy.Sponsor(service, 1);
        economy.Advance(); Assert.Equal(0UL, service.Wallet.Value); Assert.Equal(-2, service.UnpaidRent.Tension);
        Assert.Equal(4UL, economy.CostSink.Value); Conserved(economy);
        economy.Park(service); economy.Sponsor(service, 2); AssertOpen(economy.Revive(service)); Assert.False(service.Active);
        economy.Advance(); Assert.Equal(0, service.UnpaidRent.Tension); Assert.Equal(6UL, economy.CostSink.Value);
        economy.Sponsor(service, 1); economy.Revive(service); Assert.True(service.Active); Conserved(economy);
    }

    [Fact]
    public void LearningAttentionRequiresConservedFundingAndActualSpentSinkRemainsCounted()
    {
        var economy = Economy(); var service = Add(economy, 2, 3); var attention = new Carrier(economy.ResourceAxis.UnitShape);
        var spent = new Carrier(economy.ResourceAxis.UnitShape); economy.RegisterWorkSink(spent); economy.RegisterWorkSink(spent);
        Assert.False(economy.PayWork(service, attention, 1, out var blocked)); AssertOpen(blocked);
        economy.Sponsor(service, 5); Assert.True(economy.PayWork(service, attention, 3, out _));
        Assert.Equal(2UL, service.Wallet.Value); Assert.Equal(3UL, attention.Value); Conserved(economy);
        var calibration = new Relation(Relation.Segment(attention, 0, 1), Relation.Segment(spent, 0, 1));
        RefinementTransfer.TryApply(RefinementTransfer.Create(new Carrier(), calibration, new CarrierLocation(attention, 2)), out _);
        Assert.Equal(1UL, attention.Value); Assert.Equal(2UL, spent.Value); Conserved(economy);
        Assert.True(economy.BurnWork(economy.Buyer, 1, out _)); Assert.False(economy.BurnWork(new Carrier(), 1, out _)); Conserved(economy);
    }

    [Fact]
    public void BodyReplacementKeepsLocalEditWitnessAndLeavesFundingAndEvidenceAlone()
    {
        var economy = Economy(); var service = Add(economy, 2, 3); var replacement = NativeGraph.G(new Placeholder(), new Carrier());
        var edit = NativeGraph.G(service.Body, replacement); var total = economy.AccountedMoney;
        economy.UpdateBody(service, replacement, edit); Assert.Same(replacement, service.Body);
        Assert.Same(edit, NativeGraph.Parts(service.Root, 12)[11]); Assert.Equal(total, economy.AccountedMoney); AssertOpen(service.QuoteUtility());
    }

    [Fact]
    public void PendingCapacityBlocksFurtherReservationsUntilOneIsClosed()
    {
        var economy = Economy(100); var service = Add(economy, 1, 1, true);
        for (var index = 0; index < ServiceEconomy.ReceiptCapacity; index++)
            Assert.True(economy.TryPurchase(service, Command(), Open(), 0, out _, out _));
        var before = economy.Buyer.Value;
        Assert.False(economy.TryPurchase(service, Command(), Open(), 0, out _, out _)); Assert.Equal(before, economy.Buyer.Value);
        economy.Cancel(economy.Purchases[0]); Assert.True(economy.TryPurchase(service, Command(), Open(), 0, out _, out _)); Conserved(economy);
    }

    [Fact]
    public void RetirementAndLaterForgettingPreserveAggregateUnpaidDemandWithoutMintingMoney()
    {
        var economy = Economy(); var service = Add(economy, 1, 1, activeRent: 3); economy.Sponsor(service, 1);
        economy.Advance(); Assert.Equal(-2, service.UnpaidRent.Tension); economy.Retire(service); economy.Advance();
        Assert.Equal(2UL, economy.RetiredUnpaidRent.Value); Assert.Equal(-2, service.UnpaidRent.Tension);
        Assert.Empty(economy.Services); Conserved(economy);
    }

    [Fact]
    public void ExternalWorkAccountsCannotBeRegisteredTwiceOrBringUnaccountedFunding()
    {
        var economy = Economy(); var service = Add(economy, 1, 1); economy.Sponsor(service, 2);
        var foreign = new Carrier(economy.ResourceAxis.UnitShape); foreign.Add(1);
        AssertOpen(economy.RegisterWorkSink(foreign)); Assert.False(economy.PayWork(service, foreign, 1, out _));
        var attention = new Carrier(economy.ResourceAxis.UnitShape); Assert.True(economy.PayWork(service, attention, 1, out _));
        AssertOpen(economy.RegisterWorkSink(attention));
        var spent = new Carrier(economy.ResourceAxis.UnitShape); economy.RegisterWorkSink(spent);
        Assert.False(economy.PayWork(service, spent, 1, out _)); Conserved(economy);
    }

    [Fact]
    public void ZeroBookkeepingSitesCannotBecomeWorkAccountsAndLaterLossStillConserves()
    {
        var economy = Economy(); var service = Add(economy, 1, 1); economy.Sponsor(service, 2);
        Assert.Equal(0UL, economy.ExternalNegative.Value); Assert.Equal(0UL, economy.Clock.Value);
        foreach (var site in new[] { economy.ExternalNegative, economy.Clock, economy.ResourceAxis,
                     economy.TimeAxis, economy.ForgottenReceipts, economy.RetiredUnpaidRent,
                     service.UnpaidRent, service.UtilityAxis })
        {
            AssertOpen(economy.RegisterWorkSink(site)); Assert.False(economy.PayWork(service, site, 1, out _));
        }
        economy.Grant(P(economy.ResourceAxis, -5)); Assert.Equal(5UL, economy.ExternalNegative.Value);
        Assert.Equal(15, economy.ExternalNet); Assert.Equal(15, economy.AccountedMoney); Conserved(economy);
    }

    [Fact]
    public void ExplicitWorldLossRetainsUnpaidBuyerTensionAndLaterGrantSettlesIt()
    {
        var economy = Economy(2); economy.Grant(P(economy.ResourceAxis, -5));
        Assert.Equal(0UL, economy.Buyer.Value); Assert.Equal(-3, economy.Buyer.Tension); Assert.Equal(-3, economy.AccountedMoney); Conserved(economy);
        economy.Grant(P(economy.ResourceAxis, 4)); Assert.Equal(1UL, economy.Buyer.Value); Assert.Equal(0, economy.Buyer.Tension); Conserved(economy);
    }

    [Fact]
    public void ConnectedNativeRoundtripPreservesEscrowAliasesLawAndConservation()
    {
        var economy = Economy(); var service = Add(economy, 2, 3, true); var command = Command();
        economy.Sponsor(service, 4); economy.TryPurchase(service, command, P(new Carrier(), 1), 1, out _, out _);
        var restored = ServiceEconomy.FromRoot((Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(economy.Root))[0]);
        Conserved(restored); Assert.Single(restored.Services); Assert.Equal(4UL, restored.Services[0].Wallet.Value);
        var purchase = restored.Purchases[0]; var restoredCommand = purchase.Command;
        var bound = NativeGraph.BindFeedback(restoredCommand, P(restored.ResourceAxis, 8), restored.ResourceAxis);
        restored.Settle(purchase, bound); Assert.False(purchase.Pending); Assert.Equal(6UL, restored.Services[0].Wallet.Value); Conserved(restored);
        Assert.Equal(4UL, economy.Services[0].Wallet.Value); Assert.NotSame(economy.Buyer, restored.Buyer);
    }

    private static ServiceEconomy Economy(long grant = 20) => new(new Carrier(), new Carrier(), grant);
    private static EconomyService Add(ServiceEconomy economy, long fee, long construction, bool contingent = false, long activeRent = 1, long parkedRent = 0)
    {
        Assert.True(economy.AddService(NativeGraph.G(new Placeholder()), fee, construction,
            contingent ? ServiceEconomy.SuccessPaidLaw(economy.ResourceAxis) : ServiceEconomy.UpfrontLaw(economy.ResourceAxis),
            out var service, out _, activeRent, parkedRent)); return service!;
    }
    private static Relation Command() => P(new Carrier(), 1);
    private static Relation P(Carrier axis, long value) => NativeGraph.Publication(axis, value, new Carrier());
    private static Relation Open() => NativeGraph.Open(new Placeholder(), new Placeholder());
    private static void Conserved(ServiceEconomy economy) => Assert.Equal(economy.ExternalNet, economy.AccountedMoney);
    private static long Score(EconomyService service) => NativeGraph.TryValue(service.QuoteUtility(), service.UtilityAxis, out var value) ? value.Ticks : long.MinValue;
    private static void AssertOpen(Relation witness)
    {
        Assert.True(ReadWitness.TryRead(witness, out _, out _, out var result, out _, out _)); Assert.IsType<Placeholder>(result);
    }
}
