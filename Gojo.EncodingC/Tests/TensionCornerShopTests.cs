using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Real-world scenario #1 — Corner shop: stock, deliveries and
/// backorders. Exercises material tension (<c>T+</c> dock pressure, <c>T−</c>
/// backorders), calibrated gated case-to-single conversion across supplier
/// ports, in-flight reorder suppression, open supplier choice narrowed by
/// facts, demanded information tension settling quietly even when stuck,
/// isolated-port <see cref="Fork"/> supplier trials, batch provenance
/// <see cref="Closure"/> with firmness-based conflict resolution, duplicate
/// reorder-rule detection, and ranked admissibility.</summary>
public sealed class TensionCornerShopTests
{
    private const int Cap = 4;

    private sealed class ShopWorld
    {
        public required Engine SupplierA { get; init; }
        public required Store SupAStore { get; init; }
        public required Port PortA { get; init; }

        public required Engine SupplierB { get; init; }
        public required Store SupBStore { get; init; }
        public required Port PortB { get; init; }

        public required Engine Shop { get; init; }
        public required Store ShopStore { get; init; }
        public required Store RouteStore { get; init; }
        public required Socket SocketA { get; init; }
        public required Socket SocketB { get; init; }

        public required Field Cases { get; init; }
        public required Field Shelf { get; init; }
        public required Field Dock { get; init; }
        public required Field Backorder { get; init; }
        public required Field Target { get; init; }
        public required Supports RouteSupports { get; init; }

        public required Wiring InFlightGate { get; init; }
        public required Wiring DeliverA { get; init; }
        public required Wiring DeliverB { get; init; }

        // ShopStore plane indices
        public const int NeedMorePlane = 16;
        public const int HaveExtraPlane = 17;
        public const int NeedAndClearPlane = 18;
        public const int OrderAPlane = 19;
        public const int OrderBPlane = 20;

        // RouteStore plane indices (plane 0 = route code, 1..2 = RouteSupports)
        public const int AvailAPlane = 3;
        public const int AvailBPlane = 4;
        public const int Seen1Plane = 5;
        public const int Seen2Plane = 6;
        public const int DeficitPlane = 7;
        public const int ExcessPlane = 8;
        public const int ChosenAPlane = 9;
        public const int ChosenBPlane = 10;

        /// <summary>Total conserved single-item equivalents across both
        /// suppliers (2 singles/case), dock, shelf, minus unpaid backorders.</summary>
        public int NetUnits(int product = 0) =>
            2 * Cases.Read(SupAStore, product)!.Value +
            2 * Cases.Read(SupBStore, product)!.Value +
            Dock.Read(ShopStore, product)!.Value +
            Shelf.Read(ShopStore, product)!.Value -
            Backorder.Read(ShopStore, product)!.Value;
    }

    /// <summary>Builds the three-structure corner shop world: Supplier A,
    /// Supplier B (each exposing case stock through a port), and the Shop
    /// connected through sockets, with cases unpacked 1:2 into dock singles
    /// only when shelf is below target and dock is clear.</summary>
    private static ShopWorld BuildShop(
        int casesA,
        int casesB,
        bool availA,
        bool availB,
        int initialShelf = 0,
        int targetLevel = 4,
        int shelfCapacity = Cap,
        int products = 1)
    {
        var cases = new Field(Shape.Unary(Cap), 0, 1, 2, 3);

        var supAStore = new Store(Cap, products);
        var supBStore = new Store(Cap, products);
        for (var p = 0; p < products; p++)
        {
            cases.Write(supAStore, p, casesA);
            cases.Write(supBStore, p, casesB);
        }
        var supplierA = new Engine().Hold(supAStore);
        var supplierB = new Engine().Hold(supBStore);
        var lanes = Enumerable.Range(0, products).ToArray();
        var portA = supAStore.Publish(lanes, [0, 1, 2, 3]);
        var portB = supBStore.Publish(lanes, [0, 1, 2, 3]);

        var socketA = Socket.For(portA);
        socketA.Plug(portA);
        var socketB = Socket.For(portB);
        socketB.Plug(portB);

        // ShopStore: 0..3 shelf (or subset up to shelfCapacity), 4..7 dock (T+), 8..11 backorder (T-), 12..15 target, 16..20 gates.
        var shopStore = new Store(21, products);
        var shelfPlanes = Enumerable.Range(0, shelfCapacity).ToArray();
        var targetPlanes = Enumerable.Range(12, shelfCapacity).ToArray();
        var shelf = new Field(Shape.Unary(shelfCapacity), shelfPlanes);
        var dock = new Field(Shape.Unary(Cap), 4, 5, 6, 7);
        var backorder = new Field(Shape.Unary(Cap), 8, 9, 10, 11);
        var target = new Field(Shape.Unary(shelfCapacity), targetPlanes);
        for (var p = 0; p < products; p++)
        {
            shelf.Write(shopStore, p, initialShelf);
            target.Write(shopStore, p, targetLevel);
        }

        // RouteStore: plane 0 code; Supports.On adds planes 1, 2 (possible A, possible B); then 3..10 facts/status/chosen.
        var routeStore = new Store(1, products);
        var routeSupports = Supports.On(routeStore, Field.Bit(0)); // planes 1, 2
        for (var plane = 3; plane <= 10; plane++) routeStore.AddPlane();
        for (var p = 0; p < products; p++)
        {
            routeStore.Write(ShopWorld.AvailAPlane, p, availA);
            routeStore.Write(ShopWorld.AvailBPlane, p, availB);
        }

        var shop = new Engine().Hold(shopStore, routeStore);

        // 1. Narrow open supplier choice from availability facts: choice=0 requires availA=1, choice=1 requires availB=1.
        var availLaw = RelationLaw.FromPredicate(
            [Shape.Bit, Shape.Bit, Shape.Bit],
            t => (t[2] == 0 && t[0] == 1) || (t[2] == 1 && t[1] == 1));
        var narrowRoute = new Wiring(
            availLaw,
            new Binding(routeStore, ShopWorld.AvailAPlane),
            new Binding(routeStore, ShopWorld.AvailBPlane),
            routeSupports.Variable);
        for (var p = 0; p < products; p++) narrowRoute.Add(p, p, p);
        shop.Add(narrowRoute);

        // 2. Compare shelf vs target -> NeedMore, HaveExtra (single-writer combinational tree).
        Tension.ObserveMismatch(shop, shopStore, shelf, shopStore, target, shopStore, ShopWorld.NeedMorePlane, ShopWorld.HaveExtraPlane);

        // 3. In-flight suppression: only open a reorder when NeedMore=1 AND dock is empty (~dock[plane 0]).
        var inFlightGate = new Wiring(
            TensionLaws.And,
            new Binding(shopStore, ShopWorld.NeedMorePlane),
            new Binding(new Flipped(shopStore, 4), 4),
            new Binding(shopStore, ShopWorld.NeedAndClearPlane));
        for (var p = 0; p < products; p++) inFlightGate.Add(p, p, p);
        shop.Add(inFlightGate);

        // 4. Observe supplier choice under demand = NeedAndClear -> Seen1, Seen2, Deficit, Excess.
        Tension.ObserveSupports(
            shop,
            routeSupports,
            shopStore,
            ShopWorld.NeedAndClearPlane,
            routeStore,
            ShopWorld.Seen1Plane,
            ShopWorld.Seen2Plane,
            ShopWorld.DeficitPlane,
            ShopWorld.ExcessPlane);

        // 5. Uniquely settled route presence: ChosenA = possibleA & ~Seen2, ChosenB = possibleB & ~Seen2.
        var notTwo = new Flipped(routeStore, ShopWorld.Seen2Plane);
        var selectA = new Wiring(
            TensionLaws.And,
            new Binding(routeStore, routeSupports.Planes[0]),
            new Binding(notTwo, ShopWorld.Seen2Plane),
            new Binding(routeStore, ShopWorld.ChosenAPlane));
        var selectB = new Wiring(
            TensionLaws.And,
            new Binding(routeStore, routeSupports.Planes[1]),
            new Binding(notTwo, ShopWorld.Seen2Plane),
            new Binding(routeStore, ShopWorld.ChosenBPlane));
        for (var p = 0; p < products; p++)
        {
            selectA.Add(p, p, p);
            selectB.Add(p, p, p);
        }
        shop.Add(selectA).Add(selectB);

        // 6. Supplier order gates: OrderA = NeedAndClear & ChosenA, OrderB = NeedAndClear & ChosenB.
        var gateA = new Wiring(
            TensionLaws.And,
            new Binding(shopStore, ShopWorld.NeedAndClearPlane),
            new Binding(routeStore, ShopWorld.ChosenAPlane),
            new Binding(shopStore, ShopWorld.OrderAPlane));
        var gateB = new Wiring(
            TensionLaws.And,
            new Binding(shopStore, ShopWorld.NeedAndClearPlane),
            new Binding(routeStore, ShopWorld.ChosenBPlane),
            new Binding(shopStore, ShopWorld.OrderBPlane));
        for (var p = 0; p < products; p++)
        {
            gateA.Add(p, p, p);
            gateB.Add(p, p, p);
        }
        shop.Add(gateA).Add(gateB);

        // 7. Calibrated gated delivery across supplier ports: 1 case -> 2 singles onto dock (T+).
        var caseToSingles = Flows.GatedConvert(take: 1, give: 2, sourceCapacity: Cap, targetCapacity: Cap);
        var deliverA = new Wiring(
            caseToSingles,
            new Binding(socketA, 0, 1, 2, 3),
            new Binding(shopStore, ShopWorld.OrderAPlane),
            new Binding(shopStore, [.. dock.Planes]));
        var deliverB = new Wiring(
            caseToSingles,
            new Binding(socketB, 0, 1, 2, 3),
            new Binding(shopStore, ShopWorld.OrderBPlane),
            new Binding(shopStore, [.. dock.Planes]));
        for (var p = 0; p < products; p++)
        {
            deliverA.Add(p, p, p);
            deliverB.Add(p, p, p);
        }
        shop.Add(deliverA).Add(deliverB);

        // 8. Absorb dock singles (T+) onto shelf and pay customer backorders (T-) from shelf.
        var absorb = Tension.Absorb(shopStore, dock, shelf);
        var payBackorder = Tension.Draw(shopStore, backorder, shelf);
        for (var p = 0; p < products; p++)
        {
            absorb.Add(p, p);
            payBackorder.Add(p, p);
        }
        shop.Add(absorb).Add(payBackorder);

        return new ShopWorld
        {
            SupplierA = supplierA,
            SupAStore = supAStore,
            PortA = portA,
            SupplierB = supplierB,
            SupBStore = supBStore,
            PortB = portB,
            Shop = shop,
            ShopStore = shopStore,
            RouteStore = routeStore,
            SocketA = socketA,
            SocketB = socketB,
            Cases = cases,
            Shelf = shelf,
            Dock = dock,
            Backorder = backorder,
            Target = target,
            RouteSupports = routeSupports,
            InFlightGate = inFlightGate,
            DeliverA = deliverA,
            DeliverB = deliverB,
        };
    }

    /// <summary>Full material + conversion + reorder loop across three local
    /// structures: Supplier A is out, narrowing selects Supplier B, cases unpack
    /// 1:2 onto the dock only when the dock is clear, backorders are paid, the
    /// shelf hits target with zero over-ordering on extra steps, odd-target
    /// case remainders stay visible in dock <c>T+</c> while settling quietly,
    /// and the entire three-structure world saves and loads via
    /// <see cref="Transport.SaveLocals"/>.</summary>
    [Fact]
    public void CasesDeliverToDockAbsorbToShelfPayBackordersWithoutReorderSpam()
    {
        var world = BuildShop(casesA: 3, casesB: 4, availA: false, availB: true, initialShelf: 0, targetLevel: 4);

        // Customer places 2 orders against an empty shelf -> 2 backorders in T-.
        var draw1 = Unary.Draw(world.ShopStore, world.Shelf, world.Backorder, Plane.Ones(1));
        var draw2 = Unary.Draw(world.ShopStore, world.Shelf, world.Backorder, Plane.Ones(1));
        Assert.True(draw1.Held[0] && draw2.Held[0]);
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        // Save the whole 3-structure world before running so we can verify Transport round-trip.
        var saved = Transport.SaveLocals([world.SupplierA, world.SupplierB, world.Shop]);
        var restored = Transport.LoadLocals(saved);

        var conserved = world.NetUnits(); // 2*3 + 2*4 + 0 + 0 - 2 = 12
        Assert.Equal(12, conserved);

        // Step one by one until quiet, checking exact conservation at every single step.
        int? settledStep = null;
        for (var s = 1; s <= 24; s++)
        {
            var step = world.Shop.Step();
            Assert.Equal(conserved, world.NetUnits());
            if (step.Steps.All(w => w.Quiet))
            {
                settledStep = s;
                break;
            }
        }
        Assert.NotNull(settledStep);

        // Supplier B delivered exactly 3 cases (6 singles = 2 backorders + 4 shelf); Supplier A untouched; 1 case remains at B.
        Assert.Equal(3, world.Cases.Read(world.SupAStore, 0));
        Assert.Equal(1, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 0), world.Dock.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));
        Assert.False(world.ShopStore.Read(ShopWorld.NeedMorePlane, 0));

        // Extra steps with spare stock still at Supplier B must draw nothing further!
        world.Shop.Run(6);
        Assert.Equal(1, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 0), world.Dock.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        // Restored three-structure world settles identically from bits alone.
        Assert.NotNull(restored[2].Engine.Settle(24));
        var restoredShopStore = restored[2].Stores.Single(s => s.PlaneCount == world.ShopStore.PlaneCount);
        var restoredSupBStore = restored[1].Stores[0];
        Assert.Equal(1, world.Cases.Read(restoredSupBStore, 0));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(restoredShopStore, 0), world.Dock.Read(restoredShopStore, 0), world.Backorder.Read(restoredShopStore, 0)));

        // Odd target (3) with shelf capacity 3 and 1:2 cases: 2 cases (4 singles) fill shelf to 3
        // and leave the 1 leftover single visible in dock T+ while still settling quietly!
        var oddWorld = BuildShop(casesA: 0, casesB: 3, availA: false, availB: true, initialShelf: 0, targetLevel: 3, shelfCapacity: 3);
        Assert.NotNull(oddWorld.Shop.Settle(24));
        Assert.Equal(1, oddWorld.Cases.Read(oddWorld.SupBStore, 0)); // 2 cases taken -> 1 left
        Assert.Equal((3, 1, 0), (oddWorld.Shelf.Read(oddWorld.ShopStore, 0), oddWorld.Dock.Read(oddWorld.ShopStore, 0), oddWorld.Backorder.Read(oddWorld.ShopStore, 0)));
    }

    /// <summary>When both suppliers are available, the open supplier choice
    /// raises demanded information deficit (<c>RouteDeficit = 1</c>) while the
    /// shelf is short, delivers from neither supplier, and still settles
    /// quietly in <see cref="Engine.Settle"/> (both in order and under
    /// <see cref="Engine.StepTogether"/>). When the shelf is already at target,
    /// the exact same openness raises zero tension. When both suppliers are out,
    /// contradiction (<c>RouteExcess = 1</c>) lights under demand and also
    /// settles quietly.</summary>
    [Fact]
    public void OpenSupplierChoiceRaisesDemandedTensionAndStaysQuietWhenStuck()
    {
        var world = BuildShop(casesA: 2, casesB: 2, availA: true, availB: true, initialShelf: 1, targetLevel: 4);

        // Both suppliers available -> choice stays open {A, B}; shelf is short -> demanded deficit = 1, nothing delivered, settles quietly!
        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal(0b11UL, world.RouteSupports.Values(0));
        Assert.True(world.ShopStore.Read(ShopWorld.NeedMorePlane, 0));
        Assert.True(world.RouteStore.Read(ShopWorld.DeficitPlane, 0));
        Assert.False(world.RouteStore.Read(ShopWorld.ExcessPlane, 0));
        Assert.Equal((2, 2, 1), (world.Cases.Read(world.SupAStore, 0), world.Cases.Read(world.SupBStore, 0), world.Shelf.Read(world.ShopStore, 0)));

        // Single-writer combinational fold trees also settle quietly under StepTogether with zero self-contention!
        var togetherWorld = BuildShop(casesA: 2, casesB: 2, availA: true, availB: true, initialShelf: 1, targetLevel: 4);
        Assert.NotNull(togetherWorld.Shop.Settle(16, together: true));
        Assert.True(togetherWorld.ShopStore.Read(ShopWorld.NeedMorePlane, 0));
        Assert.True(togetherWorld.RouteStore.Read(ShopWorld.DeficitPlane, 0));

        // Bring shelf to target: openness without demand produces ZERO tension!
        world.Shelf.Write(world.ShopStore, 0, 4);
        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal(0b11UL, world.RouteSupports.Values(0));
        Assert.False(world.RouteStore.Read(ShopWorld.DeficitPlane, 0));
        Assert.False(world.RouteStore.Read(ShopWorld.ExcessPlane, 0));

        // Now shelf is short AND both suppliers are out -> contradiction under demand (Excess = 1), settles quietly!
        world.Shelf.Write(world.ShopStore, 0, 1);
        world.RouteStore.Write(ShopWorld.AvailAPlane, 0, false);
        world.RouteStore.Write(ShopWorld.AvailBPlane, 0, false);
        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal(0UL, world.RouteSupports.Values(0));
        Assert.False(world.RouteStore.Read(ShopWorld.DeficitPlane, 0));
        Assert.True(world.RouteStore.Read(ShopWorld.ExcessPlane, 0));

        var profile = Tension.Measure(
            world.Shop,
            sites: [new TensionSite(world.ShopStore, world.Shelf, world.Dock, world.Backorder)],
            demands: [new TensionDemand(world.RouteSupports, world.ShopStore.Read(ShopWorld.NeedAndClearPlane))]);
        Assert.True(profile.Obligation >= 1);
    }

    /// <summary>Trialing "switch supplier" in a <see cref="Fork"/> over a shop
    /// that takes through foreign supplier sockets: default <c>Fork()</c>
    /// refuses taking through a port; <c>Fork(isolatePorts: true)</c> snapshots
    /// only the exposed supplier ports so switching from empty Supplier A to
    /// stocked Supplier B runs to completion, proves strict admissibility via
    /// <see cref="Tension.Measure"/>, leaves live shop and live suppliers
    /// completely untouched, and refuses <see cref="Fork.Commit"/> so
    /// hypothetical units can never be created out of thin air.</summary>
    [Fact]
    public void ForkTrialsSwitchingSupplierWithoutTouchingLiveShopOrSuppliers()
    {
        // Supplier A is empty (0 cases) though currently selected; Supplier B has 3 cases.
        var world = BuildShop(casesA: 0, casesB: 3, availA: true, availB: false, initialShelf: 0, targetLevel: 2);
        Unary.Draw(world.ShopStore, world.Shelf, world.Backorder, Plane.Ones(1));
        Unary.Draw(world.ShopStore, world.Shelf, world.Backorder, Plane.Ones(1));

        // Live shop settles stuck on empty Supplier A: backorder=2, shelf=0.
        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        // Safety contract: plain Fork() refuses taking through a live foreign port/socket.
        Assert.Throws<ArgumentException>(() => world.Shop.Fork());

        // Isolated-port Fork trial: snapshot supplier ports, flip availability to Supplier B, reopen route, and settle!
        var trial = world.Shop.Fork(isolatePorts: true);
        Assert.True(trial.PortsIsolated);
        var trialSite = new TensionSite(trial.Of(world.ShopStore), world.Shelf, world.Dock, world.Backorder);
        var beforeTrial = Tension.Measure(trial.Engine, sites: [trialSite]);

        var trialRoute = trial.Of(world.RouteStore);
        trialRoute.Write(ShopWorld.AvailAPlane, 0, false);
        trialRoute.Write(ShopWorld.AvailBPlane, 0, true);
        // Reopen route supports in the fork workspace (planes 1 and 2 back to 1).
        trialRoute.Write(world.RouteSupports.Planes[0], 0, true);
        trialRoute.Write(world.RouteSupports.Planes[1], 0, true);

        Assert.NotNull(trial.Engine.Settle(24));
        var afterTrial = Tension.Measure(trial.Engine, sites: [trialSite]);

        Assert.Equal(2, beforeTrial.MaterialMinus);
        Assert.Equal(0, afterTrial.MaterialMinus);
        Assert.Equal(2, world.Shelf.Read(trial.Of(world.ShopStore), 0));
        Assert.Equal(1, world.Cases.Read(trial.PortView(world.SocketB), 0)); // 2 hypothetical cases drawn from snapshot
        Assert.True(Tension.AdmissibleStrict(beforeTrial, afterTrial).Admitted);

        // Live shop AND live Supplier B are completely untouched!
        Assert.Equal(3, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        // Conservation guard: isolated-port trial cannot commit hypothetical units into live shop.
        Assert.False(trial.Commit());
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));
    }

    /// <summary>Provenance question ("which supplier delivered this batch?")
    /// scoped by <see cref="Closure.Of"/>, with two conflicting delivery notes
    /// (tentative clerk slip firmness 1 vs signed invoice firmness 3) resolved
    /// by <see cref="TensionLaws.YieldByFirmness"/>—and an equal-firmness tie
    /// preserved as a visible contradiction.</summary>
    [Fact]
    public void BatchProvenanceQuestionUsesClosureAndResolvesConflictingDeliveryNotesByFirmness()
    {
        // Two batches: pos 0 = batch under question, pos 1 = unrelated batch.
        var batches = new Store(1, 2);
        var provenance = Supports.On(batches, Field.Bit(0)); // 0 = Supplier A, 1 = Supplier B

        // Delivery notes: pos 0 = clerk slip (Supplier A = 0), pos 1 = signed invoice (Supplier B = 1), pos 2 = unrelated batch 1 note.
        var notes = new Store(1, 3);
        notes.Write(0, 0, false);
        notes.Write(0, 1, true);
        notes.Write(0, 2, false);

        var firmness = new Store(3, 2);
        var firmField = new Field(Shape.Unary(3), 0, 1, 2);
        firmField.Write(firmness, 0, 1); // clerk slip: firmness 1
        firmField.Write(firmness, 1, 3); // signed invoice: firmness 3

        var clerkClaim = new Wiring(Settling.Same(Shape.Bit), new Binding(notes, 0), provenance.Variable);
        clerkClaim.Add(0, 0);
        var invoiceClaim = new Wiring(Settling.Same(Shape.Bit), new Binding(notes, 0), provenance.Variable);
        invoiceClaim.Add(1, 0);

        // Unrelated wiring on batch 1 with its own note to verify Closure.Of stays local to batch 0.
        var otherNote = new Wiring(Settling.Same(Shape.Bit), new Binding(notes, 0), provenance.Variable);
        otherNote.Add(2, 1);

        var status = new Store(5, 2); // 0 demand, 1 seen1, 2 seen2, 3 deficit, 4 excess
        status.Write(0, 0, true);     // demand sits only on batch 0

        var engine = new Engine().Add(clerkClaim).Add(invoiceClaim).Add(otherNote);
        Tension.ObserveSupports(engine, provenance, status, 0, status, 1, 2, 3, 4, positions: 0);

        // Question("which supplier delivered batch 0?"): Cone(reach: 1) covers both notes and DemandedStatus on batch 0, excluding batch 1.
        var question = new Question(provenance, 0);
        var cone = question.Cone(engine, reach: 1);
        Assert.True(cone.Rows(clerkClaim)[0] && cone.Rows(invoiceClaim)[0]);
        Assert.False(cone.Of(batches)[1]);

        // Pursuing the question attends only to batch 0's cone and settles to contradiction (Remaining == 0, excess == 1),
        // leaving unrelated batch 1 completely untouched (still open 0b11)!
        Assert.Equal(0, question.Pursue(engine, reach: 1));
        Assert.True(status.Read(4, 0));
        Assert.Equal(0b11UL, provenance.Values(1));

        // Wire firmness resolution onto both delivery-note rows' Life surfaces.
        var yieldClerk = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(status, 4), new Binding(firmness, 0, 1, 2), new Binding(firmness, 0, 1, 2), new Binding(clerkClaim.Rows.Life, 0));
        yieldClerk.Add(0, 0, 1, 0);
        var yieldInvoice = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(status, 4), new Binding(firmness, 0, 1, 2), new Binding(firmness, 0, 1, 2), new Binding(invoiceClaim.Rows.Life, 0));
        yieldInvoice.Add(0, 1, 0, 0);
        engine.Add(yieldClerk).Add(yieldInvoice);

        Assert.NotNull(engine.Settle(8));
        Assert.True(clerkClaim.Rows.IsRetired(0)); // weaker clerk slip yielded
        Assert.True(invoiceClaim.Live[0]);         // signed invoice survived

        // Reopen batch 0 and pursue again: provenance resolves uniquely to Supplier B (1) with zero tension,
        // and unrelated batch 1 is still untouched!
        provenance.Forget(0);
        Assert.Equal(1, question.Pursue(engine, reach: 1));
        Assert.Equal(1, question.Answer);
        Assert.False(status.Read(3, 0));
        Assert.False(status.Read(4, 0));
        Assert.Equal(0b11UL, provenance.Values(1));

        // Negative test: two equally firm conflicting notes (2 vs 2) stay visibly contradicted.
        var tiedBatches = new Store(1, 1);
        var tiedProv = Supports.On(tiedBatches, Field.Bit(0));
        var tiedNotes = new Store(1, 2);
        tiedNotes.Write(0, 0, false);
        tiedNotes.Write(0, 1, true);
        var tiedFirm = new Store(3, 2);
        firmField.Write(tiedFirm, 0, 2);
        firmField.Write(tiedFirm, 1, 2);
        var c1 = new Wiring(Settling.Same(Shape.Bit), new Binding(tiedNotes, 0), tiedProv.Variable);
        c1.Add(0, 0);
        var c2 = new Wiring(Settling.Same(Shape.Bit), new Binding(tiedNotes, 0), tiedProv.Variable);
        c2.Add(1, 0);
        var tiedStatus = new Store(5, 1);
        tiedStatus.Write(0, 0, true);
        var tiedEngine = new Engine().Add(c1).Add(c2);
        Tension.ObserveSupports(tiedEngine, tiedProv, tiedStatus, 0, tiedStatus, 1, 2, 3, 4);
        var y1 = new Wiring(TensionLaws.YieldByFirmness(3), new Binding(tiedStatus, 4), new Binding(tiedFirm, 0, 1, 2), new Binding(tiedFirm, 0, 1, 2), new Binding(c1.Rows.Life, 0));
        y1.Add(0, 0, 1, 0);
        var y2 = new Wiring(TensionLaws.YieldByFirmness(3), new Binding(tiedStatus, 4), new Binding(tiedFirm, 0, 1, 2), new Binding(tiedFirm, 0, 1, 2), new Binding(c2.Rows.Life, 0));
        y2.Add(0, 1, 0, 0);
        tiedEngine.Add(y1).Add(y2);
        Assert.NotNull(tiedEngine.Settle(8));
        Assert.True(c1.Live[0] && c2.Live[0]);
        Assert.Equal(0UL, tiedProv.Values(0));
        Assert.True(tiedStatus.Read(4, 0));
    }

    /// <summary>Ranked admissibility and structural excess on the corner shop:
    /// paying a customer backorder (<c>MaterialMinus</c>) strictly outranks
    /// keeping the shelf at target (<c>Goal</c>); dock <c>T+</c> and backorder
    /// <c>T−</c> are never netted in <see cref="Tension.Measure"/>; and a
    /// duplicate reorder rule is flagged by <see cref="Tension.Duplicates"/>
    /// and strictly admissible to retire in a <see cref="Fork"/>.</summary>
    [Fact]
    public void BackorderOutranksShelfTargetAndDuplicateReorderRulesAreDetectedAndRetired()
    {
        // Standalone shelf/backorder/goal comparison: shelf=3, target=3, backorder=1 arrives.
        var store = new Store(12, 1);
        var shelf = new Field(Shape.Unary(3), 0, 1, 2);
        var target = new Field(Shape.Unary(3), 3, 4, 5);
        var backorder = new Field(Shape.Unary(3), 6, 7, 8);
        var deficit = new Field(Shape.Unary(3), 9, 10, 11);
        shelf.Write(store, 0, 3);
        target.Write(store, 0, 3);
        backorder.Write(store, 0, 1);
        deficit.Write(store, 0, 0); // shelf == target before paying backorder

        var engine = new Engine().Hold(store);
        var beforePay = Tension.Measure(
            engine,
            sites: [new TensionSite(store, shelf, null, backorder)],
            goals: [new TensionGoal(store, deficit, null)]);

        // Pay the backorder from shelf: shelf drops 3->2 (goal deficit rises 0->1), backorder drops 1->0.
        var pay = Tension.Draw(store, backorder, shelf);
        pay.Add(0, 0);
        pay.Apply();
        deficit.Write(store, 0, 1); // shelf is now 1 short of target

        var afterPay = Tension.Measure(
            engine,
            sites: [new TensionSite(store, shelf, null, backorder)],
            goals: [new TensionGoal(store, deficit, null)]);

        Assert.Equal((1, 0), (beforePay.MaterialMinus, beforePay.Goal));
        Assert.Equal((0, 1), (afterPay.MaterialMinus, afterPay.Goal));
        Assert.True(Tension.AdmissibleStrict(beforePay, afterPay).Admitted);

        // Non-netting + duplicate reorder rule detection and retirement in the full shop.
        var world = BuildShop(casesA: 2, casesB: 2, availA: false, availB: true, initialShelf: 2, targetLevel: 4);
        world.Dock.Write(world.ShopStore, 0, 2);
        world.Backorder.Write(world.ShopStore, 0, 1);
        world.InFlightGate.Add(0, 0, 0); // duplicate reorder-gate row!

        var dups = Tension.Duplicates(world.Shop);
        var dup = Assert.Single(dups);
        Assert.Equal((0, 1), (dup.FirstRow, dup.DuplicateRowIndex));

        var shopProfile = Tension.Measure(
            world.Shop,
            sites: [new TensionSite(world.ShopStore, world.Shelf, world.Dock, world.Backorder)]);
        Assert.Equal(2, shopProfile.MaterialPlus);
        Assert.Equal(1, shopProfile.MaterialMinus);
        Assert.Equal(3, shopProfile.MaterialTotal); // never netted
        Assert.Equal(1, shopProfile.StructuralExcess);

        // Retiring the duplicate row inside an isolated fork strictly lowers StructuralExcess and DescriptionBits.
        var fork = world.Shop.Fork(isolatePorts: true);
        var forkSite = new TensionSite(fork.Of(world.ShopStore), world.Shelf, world.Dock, world.Backorder);
        var beforeRetire = Tension.Measure(fork.Engine, sites: [forkSite]);
        fork.Of(world.InFlightGate).Retire(1);
        var afterRetire = Tension.Measure(fork.Engine, sites: [forkSite]);

        Assert.Equal(1, beforeRetire.StructuralExcess);
        Assert.Equal(0, afterRetire.StructuralExcess);
        Assert.True(afterRetire.DescriptionBits < beforeRetire.DescriptionBits);
        Assert.True(Tension.AdmissibleStrict(beforeRetire, afterRetire).Admitted);
    }

    /// <summary>Two products share one corner-shop store and supplier ports:
    /// product 0 is short with a backorder and constrained to Supplier B, while
    /// product 1 is already at target with an open supplier choice. Product 0
    /// restocks cleanly (holding cases at the supplier while dock room is full)
    /// while product 1 draws nothing and raises zero tension.</summary>
    [Fact]
    public void MultiProductShopIsolatesProductsAndRespectsLimitedDockRoom()
    {
        var world = BuildShop(casesA: 2, casesB: 3, availA: true, availB: true, initialShelf: 0, targetLevel: 4, products: 2);

        // Product 0: Supplier A out, shelf=0, dock already holds 3 singles (only 1 room left < case size 2), 1 backorder.
        world.RouteStore.Write(ShopWorld.AvailAPlane, 0, false);
        world.Dock.Write(world.ShopStore, 0, 3);
        world.Backorder.Write(world.ShopStore, 0, 1);

        // Product 1: already at target (shelf=4), both suppliers available (route open {A, B}).
        world.Shelf.Write(world.ShopStore, 1, 4);

        var net0 = world.NetUnits(0);
        var net1 = world.NetUnits(1);

        // Step 1: dock on product 0 has 3 singles (room 1 < case size 2) AND dock non-empty suppresses reorder -> Supplier B keeps all 3 cases!
        world.Shop.Step();
        Assert.Equal(3, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal(net0, world.NetUnits(0));
        Assert.Equal(net1, world.NetUnits(1));

        // Settle completely: product 0 drains its 3 dock singles + orders 1 case (2 singles) -> 1 backorder paid + shelf=4!
        Assert.NotNull(world.Shop.Settle(24));
        Assert.Equal(net0, world.NetUnits(0));
        Assert.Equal(net1, world.NetUnits(1));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 0), world.Dock.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));
        Assert.Equal(2, world.Cases.Read(world.SupBStore, 0)); // exactly 1 case delivered for product 0

        // Product 1 stayed untouched with open route and zero demanded tension.
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 1), world.Dock.Read(world.ShopStore, 1), world.Backorder.Read(world.ShopStore, 1)));
        Assert.Equal(3, world.Cases.Read(world.SupBStore, 1));
        Assert.Equal(0b11UL, world.RouteSupports.Values(1));
        Assert.False(world.RouteStore.Read(ShopWorld.DeficitPlane, 1));
    }
}
