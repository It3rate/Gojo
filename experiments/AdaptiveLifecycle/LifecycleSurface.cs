using CompositeEmergence;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace AdaptiveLifecycle;

/// <summary>A supplied native body and its one exposed participation route.
/// Birth transports this entire connected graph together, preserving aliases.
/// It does not invent a learned architecture.</summary>
public static class LifecycleSeed
{
    // seed = G(body, attachment owner, retained participation relation)
    public static Relation Create(Relation body, Carrier participationOwner, Relation participation) =>
        NativeGraph.G(body, participationOwner, participation);
}

/// <summary>Exploratory local maintenance law. Resource accounting is physical
/// carrier value/tension; viability is a native support decision. Depletion
/// withdraws participation without destroying the body, evidence or debt.
/// Credit is not automatic reattachment. Recent receipts are bounded memory,
/// so duplicate detection promises only the retained receipt window.</summary>
public sealed class LocalLifeBudget
{
    public const int ReceiptCapacity = 4;
    public Carrier ResourceAxis { get; }
    public Carrier TimeAxis { get; }
    public Carrier Reserve { get; }
    public Carrier Clock { get; }
    public Relation Body { get; }
    public Carrier ParticipationOwner { get; }
    public Relation Participation { get; }
    private readonly Relation maintenanceLaw, viabilityPolicy, capacity;
    private readonly Carrier viabilityAxis, reserveRole, forgottenReceipts;
    private readonly List<Relation> receipts;
    private Relation lastAdvance, lastCredit, lastParticipation;

    // life = G(G(resource basis,time basis), reserve, clock, maintenance equation,
    // viability basis, reserve role, viability policy, body, attachment owner,
    // retained route, G(last timed draw,last credit,last attachment change),
    // G(recent credited receipts), forgotten receipt count, reserve capacity)
    public Relation Root => NativeGraph.G(NativeGraph.G(ResourceAxis, TimeAxis), Reserve, Clock,
        maintenanceLaw, viabilityAxis, reserveRole, viabilityPolicy, Body, ParticipationOwner,
        Participation, NativeGraph.G(lastAdvance, lastCredit, lastParticipation),
        NativeGraph.G(receipts.Cast<IElement>().ToArray()), forgottenReceipts, capacity);
    public bool Participating => ParticipationOwner.IsAttached(Participation);
    public bool Viable => NativeGraph.TryValue(ReadViability(), viabilityAxis, out var value) && value.Ticks == 1;
    public ulong ForgottenReceiptCount => forgottenReceipts.Value;

    private LocalLifeBudget(Carrier resourceAxis, Carrier timeAxis, Relation seed, long initialReserve,
        long maintenancePerTick, long maximumReserve)
    {
        if (initialReserve < 0 || maintenancePerTick <= 0 || maximumReserve <= 0 || initialReserve > maximumReserve)
            throw new ArgumentOutOfRangeException(nameof(initialReserve), "The authored maintenance/capacity contract must be positive and the initial endowment must fit.");
        ResourceAxis = resourceAxis; TimeAxis = timeAxis;
        var seedParts = NativeGraph.Parts(seed, 3);
        Body = (Relation)seedParts[0]; ParticipationOwner = (Carrier)seedParts[1]; Participation = (Relation)seedParts[2];
        Reserve = new(resourceAxis.UnitShape); Clock = new(timeAxis.UnitShape);
        capacity = new(Relation.Point(Reserve, maximumReserve), Relation.Point(Reserve, maximumReserve)); Reserve.Attach(capacity);
        maintenanceLaw = Equation.Affine(TimeAxis, 0, 1, Reserve, 0, maintenancePerTick);
        viabilityAxis = new(); reserveRole = new(); forgottenReceipts = new(); receipts = [];
        viabilityPolicy = SupportDecisionSurface.Create(new Carrier(),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(viabilityAxis, 1),
                SupportDecisionSurface.Condition(new Carrier(), reserveRole, Relation.Segment(Reserve, 1, maximumReserve))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(viabilityAxis, 0),
                SupportDecisionSurface.Condition(new Carrier(), reserveRole, Relation.Segment(Reserve, 0, 0))));
        lastAdvance = lastCredit = lastParticipation = NativeGraph.Open(seed, capacity);
        var initial = Relation.Point(ResourceAxis, initialReserve); var empty = CaptureState();
        var introduced = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(Reserve, initialReserve)));
        lastCredit = ReadWitness.Create(new Carrier(), initial, capacity, CaptureState(), Reserve.TensionSlot,
            NativeGraph.G(empty, introduced));
        if (Viable) ParticipationOwner.Attach(Participation); else ParticipationOwner.Detach(Participation);
    }

    private LocalLifeBudget(Relation root)
    {
        var fields = NativeGraph.Parts(root, 14); var axes = NativeGraph.Parts((Relation)fields[0], 2);
        ResourceAxis = (Carrier)axes[0]; TimeAxis = (Carrier)axes[1]; Reserve = (Carrier)fields[1]; Clock = (Carrier)fields[2];
        maintenanceLaw = (Relation)fields[3]; viabilityAxis = (Carrier)fields[4]; reserveRole = (Carrier)fields[5];
        viabilityPolicy = (Relation)fields[6]; Body = (Relation)fields[7]; ParticipationOwner = (Carrier)fields[8];
        Participation = (Relation)fields[9]; var recent = NativeGraph.Parts((Relation)fields[10], 3);
        lastAdvance = (Relation)recent[0]; lastCredit = (Relation)recent[1]; lastParticipation = (Relation)recent[2];
        receipts = NativeGraph.Parts((Relation)fields[11]).Cast<Relation>().ToList(); forgottenReceipts = (Carrier)fields[12]; capacity = (Relation)fields[13];
    }

    public static LocalLifeBudget Create(Carrier resourceAxis, Carrier timeAxis, Relation seed, long initialReserve,
        long maintenancePerTick = 1, long capacity = 4096) => new(resourceAxis, timeAxis, seed, initialReserve, maintenancePerTick, capacity);
    public static LocalLifeBudget FromRoot(Relation root) => new(root);

    public Relation ReadViability()
    {
        var sample = ReadWitness.Create(new Carrier(), Reserve, capacity,
            new CarrierLocation(Reserve, checked((long)Reserve.Value)), Reserve.TensionSlot, capacity);
        return SupportDecisionSurface.Select(new Carrier(), viabilityPolicy,
            SupportDecisionSurface.Binding(new Carrier(), reserveRole, sample));
    }

    public Relation Advance(long elapsed = 1)
    {
        var start = checked((long)Clock.Value); var before = CaptureState();
        if (elapsed <= 0 || elapsed > long.MaxValue - start || !Equation.TryPredict(maintenanceLaw,
            new CarrierLocation(TimeAxis, elapsed), Reserve, out var prediction) ||
            !Equation.TryReadPrediction(prediction, out var draw, out var residue) || draw.Ticks <= 0)
            return lastAdvance = NativeGraph.Open(new CarrierLocation(TimeAxis, elapsed), maintenanceLaw);
        var settlement = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(Clock, elapsed), new CarrierLocation(Reserve, -draw.Ticks)));
        if (!SettlementObservation.TryRead(settlement, out _, out var used, out _, out _) || used.Ticks == 0)
            return lastAdvance = NativeGraph.Open(settlement, maintenanceLaw);
        var participation = !Viable && Participating ? Withdraw() : NativeGraph.G(Participation);
        var after = CaptureState();
        return lastAdvance = TimeStepSequence.Step(new Carrier(), Clock, start, checked((long)Clock.Value), before,
            NativeGraph.G(maintenanceLaw, prediction, settlement, participation), after, NativeGraph.G(residue, Reserve.TensionSlot));
    }

    /// <summary>Admits a public physical resource coordinate as one finite
    /// packet. It settles debt before leaving visible reserve. Negative packets
    /// are retained draws; an unavailable public coordinate stays open.</summary>
    public Relation Credit(Relation physicalResource)
    {
        if (!NativeGraph.TryValue(physicalResource, ResourceAxis, out var resource) ||
            receipts.Any(receipt => ReadWitness.TryRead(receipt, out var selected, out _, out _, out _, out _) &&
                ReferenceEquals(selected, NativeGraph.Identity(physicalResource))))
            return lastCredit = NativeGraph.Open(NativeGraph.Identity(physicalResource), capacity);
        var before = CaptureState();
        var importLaw = Equation.Affine(ResourceAxis, 0, 1, Reserve, 0, 1);
        var predicted = Equation.Predict(importLaw, resource, Reserve);
        if (!Equation.TryReadPrediction(predicted, out var packet, out var residue))
            return lastCredit = NativeGraph.Open(physicalResource, importLaw);
        var settled = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(Reserve, packet.Ticks)));
        if (!SettlementObservation.TryRead(settled, out _, out var used, out _, out _) || used.Ticks == 0)
            return lastCredit = NativeGraph.Open(settled, importLaw);
        if (!Viable && Participating) Withdraw();
        lastCredit = ReadWitness.Create(new Carrier(), NativeGraph.Identity(physicalResource), importLaw, CaptureState(),
            NativeGraph.G(residue, Reserve.TensionSlot), NativeGraph.G(physicalResource, before, predicted, settled));
        receipts.Add(lastCredit);
        if (receipts.Count > ReceiptCapacity) { receipts.RemoveAt(0); forgottenReceipts.Add(1); }
        return lastCredit;
    }

    public Relation Withdraw()
    {
        var before = CaptureAttachment(); ParticipationOwner.Detach(Participation);
        return lastParticipation = ReadWitness.Create(new Carrier(), Participation, new Relation(ParticipationOwner, Participation),
            CaptureAttachment(), Reserve.TensionSlot, NativeGraph.G(before, CaptureState()));
    }

    public Relation Resume()
    {
        var viability = ReadViability();
        if (!NativeGraph.TryValue(viability, viabilityAxis, out var value) || value.Ticks != 1)
            return lastParticipation = NativeGraph.Open(viability, Participation);
        var before = CaptureAttachment(); ParticipationOwner.Attach(Participation);
        return lastParticipation = ReadWitness.Create(new Carrier(), Participation, viability, CaptureAttachment(),
            Reserve.TensionSlot, NativeGraph.G(before, CaptureState()));
    }

    private Relation CaptureState() => NativeGraph.G(new CarrierLocation(Reserve, checked((long)Reserve.Value)), Reserve.TensionSlot,
        new CarrierLocation(Clock, checked((long)Clock.Value)));
    private Relation CaptureAttachment() => NativeGraph.G(ParticipationOwner, Participation,
        NativeGraph.G(ParticipationOwner.AttachedRelations.Where(relation => relation.SameIdentityAs(Participation)).Cast<IElement>().ToArray()));
}

/// <summary>A finite local nursery. Slots, occupancy, resource endowment and
/// authored birth cost are native structure. Retiring a depleted occupant
/// explicitly frees capacity while its returned witness retains old state.
/// The nursery remembers only its latest birth and retirement; older records
/// have an explicit forgotten-count, not an exact-replay promise.</summary>
public sealed class BoundedNursery
{
    public Carrier ResourceAxis { get; }
    public Carrier TimeAxis { get; }
    public Carrier Funding { get; }
    /// <summary>Cumulative historical observation of unpaid maintenance at
    /// successful retirement. This is not transferred debt, available funding
    /// or a second charge; every retired child's original tension remains.
    /// Later recovery does not rewrite the fact recorded at retirement.</summary>
    public Carrier RetiredShortfall { get; }
    private readonly Carrier slotAxis, birthCostAxis, birthCount, retirementCount, forgottenEvents;
    private readonly Relation birthCost, maintenance, reserveCapacity;
    private readonly List<(Carrier Occupied, LocalLifeBudget? Child)> slots;
    private Relation lastBirth, lastRetirement;
    public Relation Root => NativeGraph.G(NativeGraph.G(ResourceAxis, TimeAxis), Funding, slotAxis, birthCostAxis,
        birthCost, maintenance, reserveCapacity,
        NativeGraph.G(slots.Select((slot, index) => (IElement)NativeGraph.G(Relation.Point(slotAxis, index), slot.Occupied,
            slot.Child is null ? new Placeholder() : slot.Child.Root)).ToArray()), birthCount, retirementCount,
        forgottenEvents, lastBirth, lastRetirement, RetiredShortfall);
    public int ActiveCount => slots.Count(slot => slot.Occupied.Value == 1);
    public IReadOnlyList<LocalLifeBudget> Slots => slots.Where(slot => slot.Child is not null).Select(slot => slot.Child!).ToArray();
    public ulong BirthCount => birthCount.Value;
    public ulong RetirementCount => retirementCount.Value;
    public ulong ForgottenEventCount => forgottenEvents.Value;

    public BoundedNursery(Carrier resourceAxis, Carrier timeAxis, long funding, int slotCount, long birthReserve,
        long maintenancePerTick = 1, long capacity = 4096)
    {
        if (funding < 0 || slotCount <= 0 || birthReserve <= 0 || birthReserve > capacity || maintenancePerTick <= 0)
            throw new ArgumentOutOfRangeException(nameof(funding), "The supplied finite nursery contract must have positive slots and endowment.");
        ResourceAxis = resourceAxis; TimeAxis = timeAxis; Funding = new(resourceAxis.UnitShape); Funding.Add(checked((ulong)funding));
        RetiredShortfall = new(resourceAxis.UnitShape);
        RetiredShortfall.Attach(new Relation(Relation.Point(RetiredShortfall, long.MaxValue), Relation.Point(RetiredShortfall, long.MaxValue)));
        slotAxis = new(); birthCostAxis = new(resourceAxis.UnitShape); birthCount = new(); retirementCount = new(); forgottenEvents = new();
        birthCost = Relation.Point(birthCostAxis, birthReserve); maintenance = Relation.Point(timeAxis, maintenancePerTick);
        reserveCapacity = Relation.Point(birthCostAxis, capacity); slots = [];
        for (var index = 0; index < slotCount; index++)
        {
            var occupied = new Carrier(); occupied.Attach(new Relation(Relation.Point(occupied, 1), Relation.Point(occupied, 1)));
            slots.Add((occupied, null));
        }
        lastBirth = lastRetirement = NativeGraph.Open(birthCost, reserveCapacity);
    }

    private BoundedNursery(Relation root)
    {
        var fields = NativeGraph.Parts(root, 14); var axes = NativeGraph.Parts((Relation)fields[0], 2);
        ResourceAxis = (Carrier)axes[0]; TimeAxis = (Carrier)axes[1]; Funding = (Carrier)fields[1]; slotAxis = (Carrier)fields[2];
        birthCostAxis = (Carrier)fields[3]; birthCost = (Relation)fields[4]; maintenance = (Relation)fields[5]; reserveCapacity = (Relation)fields[6];
        slots = NativeGraph.Parts((Relation)fields[7]).Cast<Relation>().Select(row =>
        {
            var parts = NativeGraph.Parts(row, 3);
            return ((Carrier)parts[1], parts[2] is Relation child ? LocalLifeBudget.FromRoot(child) : null);
        }).ToList();
        birthCount = (Carrier)fields[8]; retirementCount = (Carrier)fields[9]; forgottenEvents = (Carrier)fields[10];
        lastBirth = (Relation)fields[11]; lastRetirement = (Relation)fields[12];
        RetiredShortfall = (Carrier)fields[13];
    }
    public static BoundedNursery FromRoot(Relation root) => new(root);

    public bool TryBirth(Relation suppliedSeed, out LocalLifeBudget? child, out Relation witness)
    {
        child = null; var index = slots.FindIndex(slot => slot.Occupied.Value == 0);
        var cost = ((CarrierLocation)birthCost.Left).Ticks;
        if (index < 0 || Funding.Value < checked((ulong)cost) || Funding.Tension < 0)
        {
            witness = NativeGraph.Open(NativeGraph.G(suppliedSeed, new CarrierLocation(Funding, checked((long)Funding.Value)), Funding.TensionSlot),
                NativeGraph.G(birthCost, NativeGraph.G(slots.Select(slot => (IElement)slot.Occupied).ToArray())));
            return false;
        }
        // This is explicit authored seed reuse. Cloning all roots together is
        // required: separate clones would break internal physical aliases.
        var freshSeed = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(suppliedSeed))[0];
        child = LocalLifeBudget.Create(ResourceAxis, TimeAxis, freshSeed, 0,
            ((CarrierLocation)maintenance.Left).Ticks, ((CarrierLocation)reserveCapacity.Left).Ticks);
        var before = NativeGraph.G(new CarrierLocation(Funding, checked((long)Funding.Value)),
            new CarrierLocation(child.Reserve, checked((long)child.Reserve.Value)), Funding.TensionSlot, child.Reserve.TensionSlot);
        var transfer = new Relation(Relation.Point(Funding, cost), Relation.Point(child.Reserve, cost));
        var settled = Carrier.ReceiveTogether(NativeGraph.G(new CarrierLocation(Funding, -cost), new CarrierLocation(child.Reserve, cost)));
        var resumed = child.Resume(); slots[index].Occupied.Add(1); slots[index] = (slots[index].Occupied, child); birthCount.Add(1);
        if (birthCount.Value > 1) forgottenEvents.Add(1);
        witness = lastBirth = ReadWitness.Create(new Carrier(), suppliedSeed, NativeGraph.G(birthCost, transfer, Relation.Point(slotAxis, index)),
            child.Root, NativeGraph.G(Funding.TensionSlot, child.Reserve.TensionSlot),
            NativeGraph.G(before, freshSeed, settled, resumed, new CarrierLocation(Funding, checked((long)Funding.Value))));
        return true;
    }

    public Relation Retire(LocalLifeBudget child)
    {
        var index = slots.FindIndex(slot => slot.Child?.Reserve.SameIdentityAs(child.Reserve) == true && slot.Occupied.Value == 1);
        if (index < 0 || child.Viable) return NativeGraph.Open(child.Root, NativeGraph.G(birthCost, child.ReadViability()));
        // Observation keeps the original debt in place. The explicit native
        // -1 calibration turns its signed coordinate into a positive amount
        // for bounded historical accounting; it is not a material route.
        var debt = child.Reserve.TensionSlot;
        var observation = Equation.Affine(child.Reserve, 0, 1, RetiredShortfall, 0, -1);
        if (!Equation.TryPredict(observation, debt, RetiredShortfall, out var predicted) ||
            !Equation.TryReadPrediction(predicted, out var amount, out var residue) || amount.Ticks < 0)
            return NativeGraph.Open(debt, observation);
        var occupancy = slots[index].Occupied; var before = new CarrierLocation(occupancy, checked((long)occupancy.Value));
        var totalBefore = new CarrierLocation(RetiredShortfall, checked((long)RetiredShortfall.Value));
        var arrivals = NativeGraph.G(new CarrierLocation(RetiredShortfall, amount.Ticks), new CarrierLocation(occupancy, -1),
            new CarrierLocation(retirementCount, 1), new CarrierLocation(forgottenEvents, retirementCount.Value == 0 ? 0 : 1));
        var settled = Carrier.ReceiveTogether(arrivals);
        if (!SettlementObservation.TryRead(settled, out _, out var used, out _, out _) || used.Ticks == 0)
            return NativeGraph.Open(NativeGraph.G(debt, predicted, settled), NativeGraph.G(observation, RetiredShortfall));
        var withdrawn = child.Withdraw(); var retiredRoot = child.Root; slots[index] = (occupancy, null);
        return lastRetirement = ReadWitness.Create(new Carrier(), retiredRoot, Relation.Point(slotAxis, index),
            new Placeholder(), debt,
            NativeGraph.G(before, new CarrierLocation(occupancy, checked((long)occupancy.Value)),
                NativeGraph.G(withdrawn, observation, predicted, residue, settled, totalBefore,
                    new CarrierLocation(RetiredShortfall, checked((long)RetiredShortfall.Value))), retiredRoot));
    }
}
