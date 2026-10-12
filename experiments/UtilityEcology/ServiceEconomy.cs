using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace UtilityEcology;

/// <summary>A local handle over one exact native service surface. The body,
/// participation, fee, rent, reserve, unpaid demand and outcome evidence remain
/// connected when this helper is discarded. Empty evidence means unknown
/// utility; neither noise nor an absent publication becomes a numeric zero.</summary>
public sealed class EconomyService
{
    public Relation Body { get; private set; }
    public Carrier Wallet { get; }
    public Carrier Participation { get; }
    public Carrier UnpaidRent { get; }
    public Carrier UtilityAxis { get; }
    public Relation UtilityLedger { get; }
    public Relation Fee { get; }
    public Relation SettlementLaw { get; }
    public Relation ActiveRentLaw { get; }
    public Relation ParkedRentLaw { get; }
    private Relation lastDelivery, lastEdit;
    public bool Active => Participation.Value == 1;
    public long FeeTicks => Point(Fee).Ticks;
    public Relation Root => NativeGraph.G(Body, Wallet, Participation, UnpaidRent,
        UtilityAxis, UtilityLedger, Fee, SettlementLaw, ActiveRentLaw, ParkedRentLaw, lastDelivery, lastEdit);
    public Relation QuoteUtility() => NativeGraph.Score(UtilityLedger, UtilityAxis);

    internal EconomyService(Relation body, Carrier resourceAxis, Carrier timeAxis, long fee,
        Relation settlementLaw, long activeRent, long parkedRent)
    {
        Body = body; Wallet = Money(resourceAxis); Participation = new(); UnpaidRent = Money(resourceAxis);
        Participation.Attach(new Relation(Relation.Point(Participation, 1), Relation.Point(Participation, 1)));
        UtilityAxis = new(resourceAxis.UnitShape); UtilityLedger = NativeGraph.Ledger(new Carrier(), resourceAxis);
        Fee = Relation.Point(resourceAxis, fee); SettlementLaw = settlementLaw;
        ActiveRentLaw = Equation.Affine(timeAxis, 0, 1, resourceAxis, 0, activeRent);
        ParkedRentLaw = Equation.Affine(timeAxis, 0, 1, resourceAxis, 0, parkedRent);
        lastDelivery = lastEdit = NativeGraph.Open(body, new Placeholder()); Participation.Add(1);
    }
    internal EconomyService(Relation root)
    {
        var f = NativeGraph.Parts(root, 12); Body = (Relation)f[0]; Wallet = (Carrier)f[1];
        Participation = (Carrier)f[2]; UnpaidRent = (Carrier)f[3]; UtilityAxis = (Carrier)f[4];
        UtilityLedger = (Relation)f[5]; Fee = (Relation)f[6]; SettlementLaw = (Relation)f[7];
        ActiveRentLaw = (Relation)f[8]; ParkedRentLaw = (Relation)f[9]; lastDelivery = (Relation)f[10]; lastEdit = (Relation)f[11];
    }
    internal void Remember(Relation delivery) => lastDelivery = delivery;
    internal void UpdateBody(Relation body, Relation edit) { Body = body; lastEdit = edit; }
    internal static Carrier Money(Carrier basis)
    {
        var carrier = new Carrier(basis.UnitShape);
        carrier.Attach(new Relation(Relation.Point(carrier, long.MaxValue), Relation.Point(carrier, long.MaxValue)));
        return carrier;
    }
    private static CarrierLocation Point(Relation point) => NativeGraph.TryPoint(point, out var value) ? value :
        throw new ArgumentException("The supplied monetary contract must be a native point.");
}

/// <summary>One conserved purchase, with a command and the condensed publication
/// actually delivered. Pending escrow remains spendable only by its native
/// settlement law. Closed state prevents reuse even after recent receipts are
/// forgotten; callers must retain the purchase while requesting settlement.</summary>
public sealed class EconomyPurchase
{
    public EconomyService Service { get; }
    public Relation Command { get; }
    public Relation Publication { get; }
    public Carrier Escrow { get; }
    public Carrier Closed { get; }
    public Relation ComputeCost { get; }
    public Relation FeeReceipt { get; }
    private Relation settlement;
    public bool Pending => Closed.Value == 0;
    public Relation Root => NativeGraph.G(Service.Root, Command, Publication, Escrow, Closed, ComputeCost, FeeReceipt, settlement);
    internal EconomyPurchase(EconomyService service, Relation command, Relation publication, Carrier escrow,
        Relation computeCost, Relation feeReceipt)
    {
        Service = service; Command = command; Publication = NativeGraph.FreezeValue(publication); Escrow = escrow;
        Closed = new(); Closed.Attach(new Relation(Relation.Point(Closed, 1), Relation.Point(Closed, 1)));
        ComputeCost = computeCost; FeeReceipt = feeReceipt; settlement = NativeGraph.Open(command, publication);
    }
    internal EconomyPurchase(Relation root, EconomyService service)
    {
        var f = NativeGraph.Parts(root, 8); Service = service; Command = (Relation)f[1]; Publication = (Relation)f[2];
        Escrow = (Carrier)f[3]; Closed = (Carrier)f[4]; ComputeCost = (Relation)f[5]; FeeReceipt = (Relation)f[6];
        settlement = (Relation)f[7];
    }
    internal void Close(Relation witness) { Closed.Add(1); settlement = witness; }
}

/// <summary>Experimental finite service market. Fees conserve monetary packets;
/// compute, construction and rent transfer packets into an explicit cost sink.
/// Only an explicit external grant introduces signed resource. Utility evidence
/// is observational and cannot itself become funding. Unpaid rent is separate
/// demand tension, never minted money. Receipt identity protection is bounded
/// to the retained window, while each purchase has a persistent close site.</summary>
public sealed class ServiceEconomy
{
    public const int ReceiptCapacity = 16;
    public Carrier ResourceAxis { get; }
    public Carrier TimeAxis { get; }
    public Carrier Buyer { get; }
    public Carrier CostSink { get; }
    public Carrier ExternalPositive { get; }
    public Carrier ExternalNegative { get; }
    public Carrier Clock { get; }
    public Carrier ForgottenReceipts { get; }
    /// <summary>Historical observation of demand left at retirement. It does
    /// not transfer the retired body's debt or provide spendable funds.</summary>
    public Carrier RetiredUnpaidRent { get; }
    private readonly Carrier capacityAxis;
    private readonly Relation slotCapacity;
    private readonly Relation purchaseCapacity;
    private readonly List<EconomyService> services;
    private readonly List<EconomyPurchase> purchases;
    private readonly List<Relation> receipts;
    private readonly List<Carrier> attentionWallets, workSinks;
    private Relation lastGrant, lastConstruction, lastLifecycle;
    public IReadOnlyList<EconomyService> Services => services;
    public IReadOnlyList<EconomyPurchase> Purchases => purchases;
    public int SlotCapacity => checked((int)((CarrierLocation)slotCapacity.Left).Ticks);
    public Relation Root => NativeGraph.G(NativeGraph.G(ResourceAxis, TimeAxis), Buyer, CostSink,
        ExternalPositive, ExternalNegative, Clock, capacityAxis, slotCapacity,
        NativeGraph.G(services.Select(service => (IElement)service.Root).ToArray()),
        NativeGraph.G(purchases.Select(purchase => (IElement)purchase.Root).ToArray()),
        NativeGraph.G(receipts.Cast<IElement>().ToArray()), ForgottenReceipts,
        NativeGraph.G(lastGrant, lastConstruction, lastLifecycle),
        NativeGraph.G(NativeGraph.G(attentionWallets.Cast<IElement>().ToArray()), NativeGraph.G(workSinks.Cast<IElement>().ToArray())),
        NativeGraph.G(purchaseCapacity, RetiredUnpaidRent));
    public long AccountedMoney => checked(Balance(Buyer) + Balance(CostSink) +
        services.Sum(service => Balance(service.Wallet)) + purchases.Sum(purchase => Balance(purchase.Escrow)) +
        attentionWallets.Sum(Balance) + workSinks.Sum(Balance));
    public long ExternalNet => checked((long)ExternalPositive.Value - (long)ExternalNegative.Value);

    public ServiceEconomy(Carrier resourceAxis, Carrier timeAxis, long initialBuyerGrant, int slotCount = 8)
    {
        if (initialBuyerGrant is < 0 or > 1_000_000 || slotCount is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(initialBuyerGrant));
        ResourceAxis = resourceAxis; TimeAxis = timeAxis; Buyer = EconomyService.Money(resourceAxis);
        CostSink = EconomyService.Money(resourceAxis); ExternalPositive = EconomyService.Money(resourceAxis);
        ExternalNegative = EconomyService.Money(resourceAxis); Clock = EconomyService.Money(timeAxis);
        ForgottenReceipts = new(); RetiredUnpaidRent = EconomyService.Money(resourceAxis);
        capacityAxis = new(); slotCapacity = Relation.Point(capacityAxis, slotCount); purchaseCapacity = Relation.Point(capacityAxis, ReceiptCapacity);
        services = []; purchases = []; receipts = []; attentionWallets = []; workSinks = [];
        lastGrant = lastConstruction = lastLifecycle = NativeGraph.Open(slotCapacity, new Placeholder());
        Grant(NativeGraph.Publication(resourceAxis, initialBuyerGrant, new Carrier()));
    }
    private ServiceEconomy(Relation root)
    {
        var f = NativeGraph.Parts(root, 15); var axes = NativeGraph.Parts((Relation)f[0], 2);
        ResourceAxis = (Carrier)axes[0]; TimeAxis = (Carrier)axes[1]; Buyer = (Carrier)f[1]; CostSink = (Carrier)f[2];
        ExternalPositive = (Carrier)f[3]; ExternalNegative = (Carrier)f[4]; Clock = (Carrier)f[5];
        capacityAxis = (Carrier)f[6]; slotCapacity = (Relation)f[7];
        services = NativeGraph.Parts((Relation)f[8]).Cast<Relation>().Select(service => new EconomyService(service)).ToList();
        purchases = NativeGraph.Parts((Relation)f[9]).Cast<Relation>().Select(purchase =>
        {
            var serviceRoot = NativeGraph.Parts(purchase, 8)[0]; var wallet = (Carrier)NativeGraph.Parts((Relation)serviceRoot, 12)[1];
            var service = services.Single(candidate => candidate.Wallet.SameIdentityAs(wallet));
            return new EconomyPurchase(purchase, service);
        }).ToList();
        receipts = NativeGraph.Parts((Relation)f[10]).Cast<Relation>().ToList(); ForgottenReceipts = (Carrier)f[11];
        var recent = NativeGraph.Parts((Relation)f[12], 3); lastGrant = (Relation)recent[0];
        lastConstruction = (Relation)recent[1]; lastLifecycle = (Relation)recent[2];
        var work = NativeGraph.Parts((Relation)f[13], 2);
        attentionWallets = NativeGraph.Parts((Relation)work[0]).Cast<Carrier>().ToList();
        workSinks = NativeGraph.Parts((Relation)work[1]).Cast<Carrier>().ToList();
        var retained = NativeGraph.Parts((Relation)f[14], 2); purchaseCapacity = (Relation)retained[0]; RetiredUnpaidRent = (Carrier)retained[1];
    }
    public static ServiceEconomy FromRoot(Relation root) => new(root);

    // law = G(settlement delay, resource role, payment basis, native policy).
    // Delay 0 authorizes transfer at purchase; delay 1 requires a physical receipt.
    public static Relation UpfrontLaw(Carrier resourceAxis) => SettlementLaw(resourceAxis, false);
    public static Relation SuccessPaidLaw(Carrier resourceAxis) => SettlementLaw(resourceAxis, true);
    private static Relation SettlementLaw(Carrier resourceAxis, bool conditional)
    {
        var role = new Carrier(); var pay = new Carrier(); var delay = new Carrier();
        var rules = conditional
            ? SupportDecisionSurface.Create(new Carrier(),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(pay, 1),
                    SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(resourceAxis, 1, 1_000_000))),
                SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(pay, 0),
                    SupportDecisionSurface.Condition(new Carrier(), role, Relation.Segment(resourceAxis, -1_000_000, 0))))
            : SupportDecisionSurface.Create(new Carrier(), SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(pay, 1)));
        return NativeGraph.G(Relation.Point(delay, conditional ? 1 : 0), role, pay, rules);
    }

    /// <summary>A bounded authored proposal, paid before admission. A body may
    /// still have entirely open public interpretation. Construction does not
    /// infer correctness or utility and grants no service endowment.</summary>
    public bool AddService(Relation body, long fee, long constructionCost, Relation settlementLaw,
        out EconomyService? service, out Relation witness, long activeRent = 1, long parkedRent = 0)
    {
        service = null;
        if (fee < 0 || constructionCost < 0 || activeRent < 0 || parkedRent < 0 ||
            !TryLaw(settlementLaw, out _, out _, out _, out _) || services.Count >= SlotCapacity ||
            !CanSpend(Buyer, constructionCost))
        { witness = NativeGraph.Open(NativeGraph.G(body, Buyer), NativeGraph.G(slotCapacity, settlementLaw)); return false; }
        var before = CaptureMoney(); var paid = Transfer(Buyer, CostSink, constructionCost);
        service = new EconomyService(body, ResourceAxis, TimeAxis, fee, settlementLaw, activeRent, parkedRent); services.Add(service);
        witness = lastConstruction = ReadWitness.Create(new Carrier(), body,
            NativeGraph.G(Relation.Point(ResourceAxis, constructionCost), slotCapacity), service.Root,
            NativeGraph.G(), NativeGraph.G(before, paid, CaptureMoney()));
        return true;
    }

    public Relation Sponsor(EconomyService service, long amount)
    {
        if (!Owns(service) || amount < 0 || !CanSpend(Buyer, amount)) return NativeGraph.Open(service.Root, Buyer);
        var before = CaptureMoney(); var transfer = Transfer(Buyer, service.Wallet, amount);
        return ReadWitness.Create(new Carrier(), Buyer, Relation.Point(ResourceAxis, amount), service.Root,
            NativeGraph.G(), NativeGraph.G(before, transfer, CaptureMoney()));
    }

    /// <summary>Registers the gate's native spent sink so attention consumption
    /// remains included in this local conservation read. It introduces no value.</summary>
    public Relation RegisterWorkSink(Carrier spent)
    {
        if (!CompatibleNewAccount(spent) || attentionWallets.Any(account => account.SameIdentityAs(spent)) ||
            (!workSinks.Any(account => account.SameIdentityAs(spent)) && spent.HasState)) return NativeGraph.Open(spent, ResourceAxis);
        if (!workSinks.Any(account => account.SameIdentityAs(spent))) workSinks.Add(spent);
        return NativeGraph.G(spent, ResourceAxis);
    }

    public bool PayWork(EconomyService service, Carrier targetAttention, long amount, out Relation witness)
    {
        if (!Owns(service) || amount < 0 || !CompatibleNewAccount(targetAttention) ||
            workSinks.Any(account => account.SameIdentityAs(targetAttention)) ||
            (!attentionWallets.Any(account => account.SameIdentityAs(targetAttention)) && targetAttention.HasState) || !CanSpend(service.Wallet, amount))
        { witness = NativeGraph.Open(service.Root, targetAttention); return false; }
        var before = CaptureMoney();
        if (!attentionWallets.Any(account => account.SameIdentityAs(targetAttention))) attentionWallets.Add(targetAttention);
        var transfer = Transfer(service.Wallet, targetAttention, amount);
        witness = ReadWitness.Create(new Carrier(), service.Root, transfer, targetAttention, NativeGraph.G(),
            NativeGraph.G(before, transfer, CaptureMoney())); return true;
    }

    public bool BurnWork(Carrier from, long amount, out Relation witness)
    {
        if (amount < 0 || !OwnedWallet(from) || !CanSpend(from, amount))
        { witness = NativeGraph.Open(from, Relation.Point(ResourceAxis, amount)); return false; }
        var before = CaptureMoney(); var transfer = Transfer(from, CostSink, amount);
        witness = ReadWitness.Create(new Carrier(), from, Relation.Point(ResourceAxis, amount), transfer, NativeGraph.G(),
            NativeGraph.G(before, transfer, CaptureMoney())); return true;
    }

    public Relation UpdateBody(EconomyService service, Relation body, Relation editWitness)
    {
        if (!Owns(service)) return NativeGraph.Open(body, service.Root);
        var prior = service.Body; service.UpdateBody(body, editWitness);
        return ReadWitness.Create(new Carrier(), prior, editWitness, body, NativeGraph.G(), editWitness);
    }

    public bool TryPurchase(EconomyService service, Relation command, Relation publication, long computeCost,
        out EconomyPurchase? purchase, out Relation witness)
    {
        purchase = null;
        if (!Owns(service) || !service.Active || computeCost < 0 ||
            purchases.Count(candidate => candidate.Pending) >= ((CarrierLocation)purchaseCapacity.Left).Ticks ||
            service.FeeTicks > long.MaxValue - computeCost || !CanSpend(Buyer, service.FeeTicks + computeCost) ||
            !TryLaw(service.SettlementLaw, out var delay, out _, out _, out _))
        { witness = NativeGraph.Open(NativeGraph.G(command, publication), NativeGraph.G(service.Root, Buyer)); return false; }
        var before = CaptureMoney(); var escrow = EconomyService.Money(ResourceAxis);
        var feeReceipt = Transfer(Buyer, delay == 0 ? service.Wallet : escrow, service.FeeTicks);
        var compute = Transfer(Buyer, CostSink, computeCost);
        purchase = new EconomyPurchase(service, command, publication, escrow,
            NativeGraph.G(Relation.Point(ResourceAxis, computeCost), compute), feeReceipt);
        purchases.Add(purchase); PruneClosedPurchases();
        witness = ReadWitness.Create(new Carrier(), NativeGraph.G(command, NativeGraph.FreezeValue(publication)),
            service.SettlementLaw, purchase.Root, NativeGraph.G(), NativeGraph.G(before, feeReceipt, compute, CaptureMoney()));
        return true;
    }

    /// <summary>Only the independently delivered physical resource publication,
    /// explicitly bound to this command, can settle and train this purchase.
    /// Missing or mismatched feedback leaves the funded escrow pending.</summary>
    public Relation Settle(EconomyPurchase purchase, Relation boundPhysicalResource)
    {
        if (!purchases.Any(candidate => candidate.Closed.SameIdentityAs(purchase.Closed)) || !purchase.Pending ||
            !NativeGraph.IsBoundTo(boundPhysicalResource, purchase.Command, ResourceAxis) ||
            !NativeGraph.TryValue(boundPhysicalResource, ResourceAxis, out var outcome) ||
            outcome.Ticks is < -1_000_000 or > 1_000_000 || Seen(boundPhysicalResource) ||
            !TryLaw(purchase.Service.SettlementLaw, out _, out var role, out var payAxis, out var policy))
            return NativeGraph.Open(boundPhysicalResource, purchase.Root);
        var decision = SupportDecisionSurface.Select(new Carrier(), policy,
            SupportDecisionSurface.Binding(new Carrier(), role, boundPhysicalResource));
        if (!NativeGraph.TryValue(decision, payAxis, out var pay) || pay.Ticks is < 0 or > 1)
            return NativeGraph.Open(boundPhysicalResource, decision);
        var before = CaptureMoney();
        var credit = CreditExternal(boundPhysicalResource, outcome.Ticks);
        var fee = Transfer(purchase.Escrow, pay.Ticks == 1 ? purchase.Service.Wallet : Buyer, checked((long)purchase.Escrow.Value));
        var utility = NativeGraph.Update(purchase.Service.UtilityLedger, outcome.Ticks);
        var receipt = ReadWitness.Create(new Carrier(), NativeGraph.Identity(boundPhysicalResource), purchase.Publication,
            purchase.Service.QuoteUtility(), NativeGraph.G(outcome, Buyer.TensionSlot),
            NativeGraph.G(purchase.Command, boundPhysicalResource, decision, credit, fee, utility, before, CaptureMoney()));
        purchase.Service.Remember(receipt); purchase.Close(receipt); Remember(receipt); PruneClosedPurchases();
        return receipt;
    }

    /// <summary>Explicit cancellation refunds only outstanding escrow. Work
    /// already burned and an upfront fee remain paid; no utility is invented.</summary>
    public Relation Cancel(EconomyPurchase purchase)
    {
        if (!purchases.Any(candidate => candidate.Closed.SameIdentityAs(purchase.Closed)) || !purchase.Pending)
            return NativeGraph.Open(purchase.Root, new Placeholder());
        var before = CaptureMoney(); var refund = Transfer(purchase.Escrow, Buyer, checked((long)purchase.Escrow.Value));
        var receipt = ReadWitness.Create(new Carrier(), purchase.Root, refund, new Placeholder(), purchase.Publication,
            NativeGraph.G(before, refund, CaptureMoney()));
        purchase.Close(receipt); PruneClosedPurchases(); return receipt;
    }

    public Relation Park(EconomyService service) => ChangeParticipation(service, 0);
    public Relation Revive(EconomyService service) => service.Wallet.Value > 0 && service.UnpaidRent.Tension == 0
        ? ChangeParticipation(service, 1) : NativeGraph.Open(service.Root, service.UnpaidRent.TensionSlot);
    private Relation ChangeParticipation(EconomyService service, long target)
    {
        if (!Owns(service)) return NativeGraph.Open(service.Root, slotCapacity);
        var before = new CarrierLocation(service.Participation, checked((long)service.Participation.Value)); service.Participation.Set(target);
        return lastLifecycle = ReadWitness.Create(new Carrier(), service.Body, Relation.Segment(service.Participation, 0, 1),
            new CarrierLocation(service.Participation, target), service.UnpaidRent.TensionSlot, NativeGraph.G(before, service.Root));
    }

    public Relation Advance(long elapsed = 1)
    {
        var start = checked((long)Clock.Value);
        if (elapsed <= 0 || elapsed > long.MaxValue - start) return NativeGraph.Open(new CarrierLocation(TimeAxis, elapsed), Clock);
        var before = CaptureMoney(); var draws = new List<IElement>();
        foreach (var service in services)
        {
            var law = service.Active ? service.ActiveRentLaw : service.ParkedRentLaw;
            if (!Equation.TryPredict(law, new CarrierLocation(TimeAxis, elapsed), ResourceAxis, out var predicted) ||
                !Equation.TryReadPrediction(predicted, out var due, out var residue) || due.Ticks < 0)
            { draws.Add(NativeGraph.Open(new CarrierLocation(TimeAxis, elapsed), law)); continue; }
            Input(service.UnpaidRent, -due.Ticks);
            var demand = checked(-service.UnpaidRent.Tension);
            var payable = Math.Min(demand, checked((long)service.Wallet.Value));
            var paid = Transfer(service.Wallet, CostSink, payable); Input(service.UnpaidRent, payable);
            draws.Add(ReadWitness.Create(new Carrier(), law, predicted, paid, service.UnpaidRent.TensionSlot,
                NativeGraph.G(residue, due, service.Root)));
        }
        Clock.Add(checked((ulong)elapsed));
        return lastLifecycle = TimeStepSequence.Step(new Carrier(), Clock, start, checked((long)Clock.Value), before,
            NativeGraph.G(draws.ToArray()), CaptureMoney(), NativeGraph.G(services.Select(service => (IElement)service.UnpaidRent.TensionSlot).ToArray()));
    }

    public Relation Retire(EconomyService service)
    {
        if (!Owns(service) || purchases.Any(purchase => purchase.Pending && purchase.Service.Wallet.SameIdentityAs(service.Wallet)))
            return NativeGraph.Open(service.Root, slotCapacity);
        var retained = service.Root; var before = CaptureMoney(); var refund = Transfer(service.Wallet, Buyer, checked((long)service.Wallet.Value));
        var observation = Equation.Affine(service.UnpaidRent, 0, 1, RetiredUnpaidRent, 0, -1);
        var observed = Equation.Predict(observation, service.UnpaidRent.TensionSlot, RetiredUnpaidRent);
        if (Equation.TryReadPrediction(observed, out var demand, out _) && demand.Ticks >= 0)
            RetiredUnpaidRent.Add(checked((ulong)demand.Ticks));
        services.RemoveAll(candidate => candidate.Wallet.SameIdentityAs(service.Wallet));
        purchases.RemoveAll(purchase => purchase.Service.Wallet.SameIdentityAs(service.Wallet));
        return lastLifecycle = ReadWitness.Create(new Carrier(), retained, slotCapacity, new Placeholder(), service.UnpaidRent.TensionSlot,
            NativeGraph.G(before, refund, observation, observed, CaptureMoney()));
    }

    /// <summary>An explicitly authored world endowment/physical change. Callers
    /// must not use this to turn internally computed pressure into money.</summary>
    public Relation Grant(Relation physicalResource)
    {
        if (!NativeGraph.TryValue(physicalResource, ResourceAxis, out var amount) || amount.Ticks is < -1_000_000 or > 1_000_000 || Seen(physicalResource))
            return NativeGraph.Open(physicalResource, Buyer);
        var receipt = CreditExternal(physicalResource, amount.Ticks); Remember(receipt); return lastGrant = receipt;
    }
    private Relation CreditExternal(Relation physical, long amount)
    {
        var before = CaptureMoney(); Input(Buyer, amount);
        (amount >= 0 ? ExternalPositive : ExternalNegative).Add(checked((ulong)Math.Abs(amount)));
        return ReadWitness.Create(new Carrier(), NativeGraph.Identity(physical), Relation.Point(ResourceAxis, amount), CaptureMoney(),
            Buyer.TensionSlot, NativeGraph.G(physical, before, CaptureMoney()));
    }
    private bool Seen(Relation physical) => receipts.Any(receipt => ReadWitness.TryRead(receipt,
        out var source, out _, out _, out _, out _) && ReferenceEquals(source, NativeGraph.Identity(physical)));
    private void Remember(Relation receipt)
    {
        receipts.Add(receipt); if (receipts.Count <= ReceiptCapacity) return;
        receipts.RemoveAt(0); ForgottenReceipts.Add(1);
    }
    private void PruneClosedPurchases()
    {
        while (purchases.Count(purchase => !purchase.Pending) > ReceiptCapacity)
            purchases.RemoveAt(purchases.FindIndex(purchase => !purchase.Pending));
    }
    private bool Owns(EconomyService service) => services.Any(candidate => candidate.Wallet.SameIdentityAs(service.Wallet));
    private bool OwnedWallet(Carrier account) => Buyer.SameIdentityAs(account) ||
        services.Any(service => service.Wallet.SameIdentityAs(account)) || attentionWallets.Any(wallet => wallet.SameIdentityAs(account));
    private bool CompatibleNewAccount(Carrier account) => !account.IsUnknown && account.UnitShape.Equals(ResourceAxis.UnitShape) &&
        !new[] { ResourceAxis, TimeAxis, ExternalPositive, ExternalNegative, Clock, ForgottenReceipts,
            RetiredUnpaidRent, capacityAxis }.Any(bookkeeping => bookkeeping.SameIdentityAs(account)) &&
        !Buyer.SameIdentityAs(account) && !CostSink.SameIdentityAs(account) &&
        !services.Any(service => service.Wallet.SameIdentityAs(account) || service.Participation.SameIdentityAs(account) ||
            service.UnpaidRent.SameIdentityAs(account) || service.UtilityAxis.SameIdentityAs(account) ||
            NativeGraph.Parts(service.UtilityLedger, 3).Cast<Carrier>().Any(bookkeeping => bookkeeping.SameIdentityAs(account))) &&
        !purchases.Any(purchase => purchase.Escrow.SameIdentityAs(account) || purchase.Closed.SameIdentityAs(account));
    private static void Input(Carrier carrier, long ticks) => Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(carrier, ticks)));
    private static bool CanSpend(Carrier source, long amount) => amount >= 0 && source.Tension == 0 && source.Value >= checked((ulong)amount);
    private static long Balance(Carrier money) => checked((long)money.Value + money.Tension);
    private Relation CaptureMoney() => NativeGraph.G(NativeGraph.G(new CarrierLocation(Buyer, checked((long)Buyer.Value)), Buyer.TensionSlot),
        new CarrierLocation(CostSink, checked((long)CostSink.Value)),
        NativeGraph.G(services.Select(service => (IElement)NativeGraph.G(new CarrierLocation(service.Wallet, checked((long)service.Wallet.Value)), service.Wallet.TensionSlot)).ToArray()),
        NativeGraph.G(purchases.Select(purchase => (IElement)new CarrierLocation(purchase.Escrow, checked((long)purchase.Escrow.Value))).ToArray()),
        NativeGraph.G(attentionWallets.Concat(workSinks).Select(account => (IElement)NativeGraph.G(new CarrierLocation(account, checked((long)account.Value)), account.TensionSlot)).ToArray()));
    private static Relation Transfer(Carrier source, Carrier target, long amount)
    {
        var calibration = new Relation(Relation.Segment(source, 0, 1), Relation.Segment(target, 0, 1));
        var request = RefinementTransfer.Create(new Carrier(), calibration, new CarrierLocation(source, amount));
        return RefinementTransfer.TryApply(request, out var witness) ? witness : NativeGraph.Open(request, calibration);
    }
    private static bool TryLaw(Relation law, out long delay, out IElement role, out Carrier payAxis, out Relation policy)
    {
        delay = 0; role = null!; payAxis = null!; policy = null!;
        if (!law.TryReadOrderedGroup(out var f) || f.Length != 4 || !NativeGraph.TryPoint(f[0], out var time) ||
            time.Ticks is < 0 or > 1 || f[2] is not Carrier pay || f[3] is not Relation rules || !rules.TryReadOrderedGroup(out _)) return false;
        delay = time.Ticks; role = f[1]; payAxis = pay; policy = rules; return true;
    }
}
