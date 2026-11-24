using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Real-world scenario #1 — Corner shop: stock, deliveries and
/// backorders. Exercises the full bit-level tension system across multi-owner
/// local structures:
/// <list type="bullet">
/// <item>Strict Axiom 5 material entry (<see cref="Tension.Receive"/> /
/// <see cref="Tension.Demand"/>), <see cref="Field.Room"/>, and calibrated
/// <see cref="Flows.GatedConvert"/> / <see cref="Flows.Fulfill"/>;</item>
/// <item>Emulated unary accumulation (<see cref="Tension.Accumulate"/>:
/// <c>shelf + dock</c> vs. <c>target + backorder</c>) compared via single-writer
/// combinational <see cref="Tension.ObserveMismatch"/> trees;</item>
/// <item>Truth-maintained supplier choice (<see cref="Settling.Justify"/>) vs.
/// monotonic narrowing when a closed supplier reopens;</item>
/// <item>Three-owner customer pickup backpressure, supplier disconnect/reconnect
/// with open <c>OnOrder</c> commitments, and per-step byte-for-byte
/// <see cref="Transport.SaveLocals"/> equivalence;</item>
/// <item>Data-driven unary thresholds (<see cref="Tension.ObserveThreshold"/>)
/// and in-machine lexicographic ranking (<see cref="TensionLaws.RankCombine"/>)
/// as stepping stones toward numeric support;</item>
/// <item>Isolated-port <see cref="Fork"/> trials, <see cref="Question.Pursue"/>
/// batch provenance with <see cref="TensionLaws.YieldByFirmness"/>, duplicate
/// rule retirement, and multi-product limited-dock isolation.</item>
/// </list></summary>
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

        public required Tension Stock { get; init; }
        public required Field Cases { get; init; }
        public required Field Shelf { get; init; }
        public required Field Dock { get; init; }
        public required Field Backorder { get; init; }
        public required Field Target { get; init; }
        public required Supports RouteSupports { get; init; }

        public required Wiring InFlightGate { get; init; }
        public required Wiring DeliverA { get; init; }
        public required Wiring DeliverB { get; init; }

        // ShopStore fixed control planes (after unary fields & accumulated sums)
        public required int NeedMorePlane { get; init; }
        public required int HaveExtraPlane { get; init; }
        public required int NeedAndClearPlane { get; init; }
        public required int OrderAPlane { get; init; }
        public required int OrderBPlane { get; init; }

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

    /// <summary>Builds the three-structure corner shop: Supplier A and
    /// Supplier B behind ports, and the Shop with unary accumulation
    /// (<c>shelf + dock</c> vs. <c>target + backorder</c>), combinational
    /// mismatch/support observers, optional <see cref="Settling.Justify"/>
    /// supplier truth maintenance, and 1:2 case-to-single gated delivery.</summary>
    private static ShopWorld BuildShop(
        int casesA,
        int casesB,
        bool availA,
        bool availB,
        int initialShelf = 0,
        int targetLevel = 4,
        int shelfCapacity = Cap,
        int products = 1,
        bool justifyRoute = false)
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

        // ShopStore: shelf, dock (T+), backorder (T-), target, accumulated (shelf+dock) and (target+backorder), plus gate planes.
        var shopStore = new Store(0, products);
        Field AddUnary(int cap) => new(Shape.Unary(cap), [.. Enumerable.Range(0, cap).Select(_ => shopStore.AddPlane())]);

        var shelf = AddUnary(shelfCapacity);
        var stock = new Tension(shopStore, shelf, Cap);
        var dock = stock.Plus;
        var backorder = stock.Minus;
        var target = AddUnary(shelfCapacity);
        var effectiveStock = AddUnary(shelfCapacity + Cap);
        var effectiveWant = AddUnary(shelfCapacity + Cap);
        var needMorePlane = shopStore.AddPlane();
        var haveExtraPlane = shopStore.AddPlane();
        var needAndClearPlane = shopStore.AddPlane();
        var orderAPlane = shopStore.AddPlane();
        var orderBPlane = shopStore.AddPlane();

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

        // 1. Update supplier choice: either truth-maintained Justify (reopens when supplier returns) or monotonic RelationLaw narrowing.
        if (justifyRoute)
        {
            var justA = new Wiring(Settling.Justify(1), new Binding(routeStore, ShopWorld.AvailAPlane), routeSupports.Possible(0));
            var justB = new Wiring(Settling.Justify(1), new Binding(routeStore, ShopWorld.AvailBPlane), routeSupports.Possible(1));
            for (var p = 0; p < products; p++)
            {
                justA.Add(p, p);
                justB.Add(p, p);
            }
            shop.Add(justA).Add(justB);
        }
        else
        {
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
        }

        // 2. Accumulate effectiveStock = shelf + dock and effectiveWant = target + backorder in planes, then compare.
        Tension.Accumulate(shop, shopStore, shelf, shopStore, dock, shopStore, effectiveStock);
        Tension.Accumulate(shop, shopStore, target, shopStore, backorder, shopStore, effectiveWant);
        Tension.ObserveMismatch(shop, shopStore, effectiveStock, shopStore, effectiveWant, shopStore, needMorePlane, haveExtraPlane);

        // 3. In-flight suppression: only open a reorder when effectiveStock < effectiveWant AND dock is clear (~dock[0]).
        var dockPlane0 = dock.Planes[0];
        var inFlightGate = new Wiring(
            TensionLaws.And,
            new Binding(shopStore, needMorePlane),
            new Binding(new Flipped(shopStore, dockPlane0), dockPlane0),
            new Binding(shopStore, needAndClearPlane));
        for (var p = 0; p < products; p++) inFlightGate.Add(p, p, p);
        shop.Add(inFlightGate);

        // 4. Observe supplier choice under demand = needAndClear -> Seen1, Seen2, Deficit, Excess.
        Tension.ObserveSupports(
            shop,
            routeSupports,
            shopStore,
            needAndClearPlane,
            routeStore,
            ShopWorld.Seen1Plane,
            ShopWorld.Seen2Plane,
            ShopWorld.DeficitPlane,
            ShopWorld.ExcessPlane);

        // 5. Uniquely settled route presence: ChosenA = possibleA & ~Seen2, ChosenB = possibleB & ~Seen2.
        var notTwo = new Flipped(routeStore, ShopWorld.Seen2Plane);
        var selectA = new Wiring(TensionLaws.And, routeSupports.Possible(0), new Binding(notTwo, ShopWorld.Seen2Plane), new Binding(routeStore, ShopWorld.ChosenAPlane));
        var selectB = new Wiring(TensionLaws.And, routeSupports.Possible(1), new Binding(notTwo, ShopWorld.Seen2Plane), new Binding(routeStore, ShopWorld.ChosenBPlane));
        for (var p = 0; p < products; p++)
        {
            selectA.Add(p, p, p);
            selectB.Add(p, p, p);
        }
        shop.Add(selectA).Add(selectB);

        // 6. Supplier order gates: OrderA = needAndClear & ChosenA, OrderB = needAndClear & ChosenB.
        var gateA = new Wiring(TensionLaws.And, new Binding(shopStore, needAndClearPlane), new Binding(routeStore, ShopWorld.ChosenAPlane), new Binding(shopStore, orderAPlane));
        var gateB = new Wiring(TensionLaws.And, new Binding(shopStore, needAndClearPlane), new Binding(routeStore, ShopWorld.ChosenBPlane), new Binding(shopStore, orderBPlane));
        for (var p = 0; p < products; p++)
        {
            gateA.Add(p, p, p);
            gateB.Add(p, p, p);
        }
        shop.Add(gateA).Add(gateB);

        // 7. Calibrated gated delivery across supplier ports: 1 case -> 2 singles onto dock (T+).
        var caseToSingles = Flows.GatedConvert(take: 1, give: 2, sourceCapacity: Cap, targetCapacity: Cap);
        var deliverA = new Wiring(caseToSingles, new Binding(socketA, 0, 1, 2, 3), new Binding(shopStore, orderAPlane), dock.On(shopStore));
        var deliverB = new Wiring(caseToSingles, new Binding(socketB, 0, 1, 2, 3), new Binding(shopStore, orderBPlane), dock.On(shopStore));
        for (var p = 0; p < products; p++)
        {
            deliverA.Add(p, p, p);
            deliverB.Add(p, p, p);
        }
        shop.Add(deliverA).Add(deliverB);

        // 8. Absorb dock singles (T+) onto shelf and pay customer backorders (T-) from shelf.
        var all = Plane.Ones(products);
        shop.Add(stock.Absorb(all)).Add(stock.Draw(all));

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
            Stock = stock,
            Cases = cases,
            Shelf = shelf,
            Dock = dock,
            Backorder = backorder,
            Target = target,
            RouteSupports = routeSupports,
            InFlightGate = inFlightGate,
            DeliverA = deliverA,
            DeliverB = deliverB,
            NeedMorePlane = needMorePlane,
            HaveExtraPlane = haveExtraPlane,
            NeedAndClearPlane = needAndClearPlane,
            OrderAPlane = orderAPlane,
            OrderBPlane = orderBPlane,
        };
    }

    /// <summary>Full material + accumulated comparison + calibrated 1:2 case
    /// delivery loop across three local structures: customer orders enter
    /// strictly as <c>T−</c> (<see cref="Tension.Demand"/>), <c>shelf + dock</c>
    /// is compared in planes against <c>target + backorder</c>, cases deliver
    /// with zero reorder spam and exact conservation at every step, extra steps
    /// draw nothing further, odd-target remainders stay visible in dock <c>T+</c>
    /// while settling quietly, and the three-structure world round-trips through
    /// <see cref="Transport.SaveLocals"/>.</summary>
    [Fact]
    public void CasesDeliverToDockAbsorbToShelfPayBackordersWithoutReorderSpam()
    {
        var world = BuildShop(casesA: 3, casesB: 4, availA: false, availB: true, initialShelf: 0, targetLevel: 4);

        // Strict Axiom 5 entry: customer orders enter as T- debt directly via Stock.Demand.
        Assert.True(world.Stock.Demand(Plane.Ones(1)).Taken[0]);
        Assert.True(world.Stock.Demand(Plane.Ones(1)).Taken[0]);
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        var saved = Transport.SaveLocals([world.SupplierA, world.SupplierB, world.Shop]);
        var restored = Transport.LoadLocals(saved);

        var conserved = world.NetUnits(); // 2*3 + 2*4 + 0 + 0 - 2 = 12
        Assert.Equal(12, conserved);

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

        Assert.Equal(3, world.Cases.Read(world.SupAStore, 0));
        Assert.Equal(1, world.Cases.Read(world.SupBStore, 0)); // exactly 3 cases (6 singles = 2 backorders + 4 shelf)
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 0), world.Dock.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));
        Assert.False(world.ShopStore.Read(world.NeedMorePlane, 0));

        // Extra steps with stock still available at Supplier B must draw nothing further.
        world.Shop.Run(6);
        Assert.Equal(1, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 0), world.Dock.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        // Restored three-structure world settles identically from raw bits alone.
        Assert.NotNull(restored[2].Engine.Settle(24));
        var restoredShopStore = restored[2].Stores.Single(s => s.PlaneCount == world.ShopStore.PlaneCount);
        var restoredSupBStore = restored[1].Stores[0];
        Assert.Equal(1, world.Cases.Read(restoredSupBStore, 0));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(restoredShopStore, 0), world.Dock.Read(restoredShopStore, 0), world.Backorder.Read(restoredShopStore, 0)));

        // Odd target (3) with shelf capacity 3 and 1:2 cases: 2 cases (4 singles) fill shelf to 3
        // and leave 1 leftover single visible in dock T+ while settling quietly.
        var oddWorld = BuildShop(casesA: 0, casesB: 3, availA: false, availB: true, initialShelf: 0, targetLevel: 3, shelfCapacity: 3);
        Assert.NotNull(oddWorld.Shop.Settle(24));
        Assert.Equal(1, oddWorld.Cases.Read(oddWorld.SupBStore, 0));
        Assert.Equal((3, 1, 0), (oddWorld.Shelf.Read(oddWorld.ShopStore, 0), oddWorld.Dock.Read(oddWorld.ShopStore, 0), oddWorld.Backorder.Read(oddWorld.ShopStore, 0)));
    }

    /// <summary>Contrast between monotonic narrowing and truth-maintained
    /// justification (<see cref="Settling.Justify"/>) when a closed supplier
    /// reopens mid-run: under monotonic narrowing, Supplier A stays ruled out
    /// even after reopening and the shop finishes with unpaid backorders while
    /// A holds stock; under <see cref="Settling.Justify"/>, Supplier A reopens
    /// automatically, delivers to fill the backorders, and settles quietly.</summary>
    [Fact]
    public void JustifiedSupplierChoiceReopensWhenClosedSupplierRestocks()
    {
        (int Shelf, int Backorder, int CasesA, int CasesB) Run(bool justify)
        {
            // Start: A closed (3 cases), B open (2 cases). Target = 4.
            var world = BuildShop(casesA: 3, casesB: 2, availA: false, availB: true, initialShelf: 0, targetLevel: 4, justifyRoute: justify);
            Assert.NotNull(world.Shop.Settle(24));
            Assert.Equal((4, 0, 3, 0), (world.Shelf.Read(world.ShopStore, 0)!.Value, world.Backorder.Read(world.ShopStore, 0)!.Value, world.Cases.Read(world.SupAStore, 0)!.Value, world.Cases.Read(world.SupBStore, 0)!.Value));

            // Now Supplier B closes (empty) and Supplier A reopens; 4 new customer orders arrive!
            world.RouteStore.Write(ShopWorld.AvailAPlane, 0, true);
            world.RouteStore.Write(ShopWorld.AvailBPlane, 0, false);
            for (var i = 0; i < 4; i++) world.Stock.Demand(Plane.Ones(1));
            Assert.NotNull(world.Shop.Settle(24));

            return (
                world.Shelf.Read(world.ShopStore, 0)!.Value,
                world.Backorder.Read(world.ShopStore, 0)!.Value,
                world.Cases.Read(world.SupAStore, 0)!.Value,
                world.Cases.Read(world.SupBStore, 0)!.Value);
        }

        // Monotonic narrowing: A stayed ruled out, so B closing leaves contradiction; 4 orders drain shelf to 0, never restocked while A holds 3 cases!
        var narrowed = Run(justify: false);
        Assert.Equal((0, 0, 3, 0), narrowed);

        // Justified truth maintenance: A reopened automatically and delivered 2 cases to restore shelf to target (4), with 1 case left at A!
        var justified = Run(justify: true);
        Assert.Equal((4, 0, 1, 0), justified);
    }

    /// <summary>Three-owner world (Supplier, Shop, Customer) exercising
    /// dual-conserving <see cref="Flows.Fulfill(int, int, int)"/> at both
    /// boundaries: customer pickup backpressure holds backorders even with
    /// stock on the shelf, supplier receipt atomically clears an open
    /// <c>OnOrder</c> commitment across disconnect/reconnect and target drops,
    /// and every step matches byte-for-byte after
    /// <see cref="Transport.SaveLocals"/> / <see cref="Transport.LoadLocals"/>.</summary>
    [Fact]
    public void ThreeOwnerCustomerPickupBackpressureAndSupplierDisconnectSurviveRestore()
    {
        const int ShelfCap = 2;
        const int DockCap = 1;
        const int SupCap = 3;
        const int ReqCap = 2;
        const int PickupCap = 1;
        const int InvCap = ShelfCap + DockCap;

        Field UnaryField(int cap) => new(Shape.Unary(cap), [.. Enumerable.Range(0, cap)]);

        // Supplier owner
        var supField = UnaryField(SupCap);
        var supStore = new Store(SupCap, 1);
        supField.Write(supStore, 0, SupCap);
        var supplier = new Engine().Hold(supStore);
        var supPort = supStore.Publish([0], [.. supField.Planes]);
        var supSocket = Socket.For(supPort);
        supSocket.Plug(supPort);

        // Shop stores (ShelfCap=2, DockCap=1, BackorderCap=ReqCap=2)
        var shopSite = new Store(ShelfCap + DockCap + ReqCap, 1);
        var shelfField = new Field(Shape.Unary(ShelfCap), 0, 1);
        var dockField = new Field(Shape.Unary(DockCap), 2);
        var backorderField = new Field(Shape.Unary(ReqCap), 3, 4);
        var shopTension = new Tension(shopSite, shelfField, dockField, backorderField);
        shopTension.Receive(Plane.Ones(1)); // 1 unit already waiting on full dock!

        var pickupStore = new Store(PickupCap, 1);
        var pickupBit = Field.Bit(0);
        var pickupPort = pickupStore.Publish([0], [0]);
        var pickupSocket = Socket.For(pickupPort);
        pickupSocket.Plug(pickupPort);

        var controls = new Store(1, 2); // pos 0 = unloading gate, pos 1 = OnOrder (cap 1)
        var invField = UnaryField(InvCap);
        var comparison = new Store(InvCap, 4); // pos 0 = have (shelf+dock), pos 1 = want (target), pos 2 = deficit, pos 3 = excess
        invField.Write(comparison, 1, ShelfCap); // target = 2

        // Customer owner (posts requests through a port, collects from pickup bay when permitted)
        var reqField = UnaryField(ReqCap);
        var requestsStore = new Store(ReqCap, 1);
        reqField.Write(requestsStore, 0, 1); // 1 customer request waiting
        var reqPort = requestsStore.Publish([0], [.. reqField.Planes]);
        var reqSocket = Socket.For(reqPort);
        reqSocket.Plug(reqPort);

        var collectedField = UnaryField(SupCap + DockCap);
        var collectedStore = new Store(collectedField.Shape.Bits, 1);
        var collectGate = new Store(1, 1); // starts 0 (customer not collecting yet)
        var collectWiring = new Wiring(Flows.GatedMove(PickupCap, collectedField.Shape.Bits), new Binding(pickupSocket, 0), new Binding(collectGate, 0), collectedField.On(collectedStore));
        collectWiring.Add(0, 0, 0);
        var customer = new Engine().Hold(requestsStore).Add(collectWiring);

        // Shop wirings: accept request -> unload (gated) -> deliver to pickup (Fulfill!) -> receive from supplier (Fulfill!) -> sum & TrackGap -> book OnOrder
        var acceptReq = new Wiring(Flows.Move(ReqCap, ReqCap), new Binding(reqSocket, [.. reqField.Planes]), backorderField.On(shopSite));
        acceptReq.Add(0, 0);
        var unload = shopTension.Absorb(Plane.Ones(1), new Binding(controls, 0));
        var deliverToPickup = shopTension.Deliver(pickupBit.On(pickupStore), PickupCap, Plane.Ones(1));
        var receiveFromSup = new Wiring(Flows.Fulfill(SupCap, 1, DockCap), new Binding(supSocket, [.. supField.Planes]), new Binding(controls, 0), dockField.On(shopSite));
        receiveFromSup.Add(0, 1, 0);
        var sumInv = new Wiring(TensionLaws.Sum(ShelfCap, DockCap), shelfField.On(shopSite), dockField.On(shopSite), invField.On(comparison));
        sumInv.Add(0, 0, 0);
        var trackGap = new Wiring(TensionLaws.TrackGap(InvCap), invField.On(comparison), invField.On(comparison), invField.On(comparison), invField.On(comparison));
        trackGap.Add(0, 1, 2, 3);
        var bookOrderLaw = TransitionLaw.FromFunction([invField.Shape, Shape.Bit], s => [s[0], s[0] > 0 ? 1 : s[1]], new Accounting(true, new Unchanged(0), new NeverDecreases(1)));
        var bookOrder = new Wiring(bookOrderLaw, invField.On(comparison), new Binding(controls, 0));
        bookOrder.Add(2, 1);

        var shop = new Engine().Add(acceptReq).Add(unload).Add(deliverToPickup).Add(receiveFromSup).Add(sumInv).Add(trackGap).Add(bookOrder);

        bool StepAll(Engine s, Engine sh, Engine c) =>
            c.Step().Steps.Concat(s.Step().Steps).Concat(sh.Step().Steps).All(w => w.Quiet);

        // Step 1 & 2 with unloading closed: request accepted into backorder, OnOrder booked, settles quiet on full dock!
        Assert.False(StepAll(supplier, shop, customer));
        Assert.True(StepAll(supplier, shop, customer));
        Assert.Equal((3, 1, 0, 1, 0, 1), (
            supField.Read(supStore, 0),
            dockField.Read(shopSite, 0),
            shelfField.Read(shopSite, 0),
            backorderField.Read(shopSite, 0),
            pickupBit.Read(pickupStore, 0),
            controls.Read(0, 1) ? 1 : 0));

        // Save and restore all three owners, then verify byte-for-byte identical evolution at every step!
        var saved = Transport.SaveLocals(supplier, shop, customer);
        var restored = Transport.LoadLocals(saved);
        Assert.Equal(saved, Transport.SaveLocals(restored[0].Engine, restored[1].Engine, restored[2].Engine));

        // Enable unloading on both original and restored; step until pickup holds the item and shelf reaches 2!
        controls.Write(0, 0, true);
        var rControls = restored[1].Stores.Single(s => s.Count == 2 && s.PlaneCount == 1);
        rControls.Write(0, 0, true);

        for (var i = 0; i < 4; i++)
        {
            var q1 = StepAll(supplier, shop, customer);
            var q2 = StepAll(restored[0].Engine, restored[1].Engine, restored[2].Engine);
            Assert.Equal(q1, q2);
            Assert.Equal(Transport.SaveLocals(supplier, shop, customer), Transport.SaveLocals(restored[0].Engine, restored[1].Engine, restored[2].Engine));
        }

        // Pickup holds 1 unit (not collected yet), shelf=2, dock=0, backorder=0, supplier=1!
        Assert.Equal((1, 0, 2, 0, 1, 0), (
            supField.Read(supStore, 0),
            dockField.Read(shopSite, 0),
            shelfField.Read(shopSite, 0),
            backorderField.Read(shopSite, 0),
            pickupBit.Read(pickupStore, 0),
            collectedField.Read(collectedStore, 0)));

        // Post a 2nd customer request while pickup bay is STILL full: backorder stays held even though shelf has 2!
        Assert.True(Unary.Add(requestsStore, reqField, Plane.Ones(1)).Taken[0]);
        StepAll(supplier, shop, customer);
        StepAll(supplier, shop, customer);
        Assert.Equal((2, 1, 1), (shelfField.Read(shopSite, 0), backorderField.Read(shopSite, 0), pickupBit.Read(pickupStore, 0)));

        // Disconnect supplier while a reorder is open, then reconnect: OnOrder stays visibly 1 and delivers once reconnected!
        collectGate.Write(0, 0, true);
        supSocket.Unplug();
        for (var i = 0; i < 6; i++) StepAll(supplier, shop, customer);
        Assert.Equal((1, 0, 1, 0, 2), (supField.Read(supStore, 0), dockField.Read(shopSite, 0), shelfField.Read(shopSite, 0), backorderField.Read(shopSite, 0), collectedField.Read(collectedStore, 0)));
        Assert.True(controls.Read(0, 1)); // OnOrder survived supplier disconnect!

        // Lower target to 0, then reconnect supplier: the already-booked OnOrder still fulfills atomically and clears!
        invField.Write(comparison, 1, 0);
        supSocket.Plug(supPort);
        for (var i = 0; i < 4; i++) StepAll(supplier, shop, customer);
        Assert.False(controls.Read(0, 1));
        Assert.Equal((0, 0, 2, 2), (supField.Read(supStore, 0), dockField.Read(shopSite, 0), shelfField.Read(shopSite, 0), collectedField.Read(collectedStore, 0)));
    }

    /// <summary>Stepping toward numeric support inside bit tables:
    /// (1) <see cref="Tension.ObserveThreshold"/> reads a data-driven unary
    /// threshold stored in planes—lowering the threshold data in the store
    /// wakes a spare route without changing any law table;
    /// (2) <see cref="TensionLaws.RankCombine"/> performs lexicographic tier
    /// comparison (<c>MaterialMinus</c> over <c>Goal</c>) purely inside bit
    /// tables without calling <see cref="TensionDiagnostics"/>.</summary>
    [Fact]
    public void DataDrivenThresholdAndInMachineRankingRunEntirelyInBitTables()
    {
        // Part 1: Data-driven threshold in planes (cap 4).
        var store = new Store(12, 2); // 0..3 streak, 4..7 threshold (data!), 8 below, 9 above, 10 persistentSignal
        var streak = new Field(Shape.Unary(4), 0, 1, 2, 3);
        var threshold = new Field(Shape.Unary(4), 4, 5, 6, 7);
        threshold.Write(store, 0, 3); // initial threshold = 3
        store.Write(10, 0, true);     // persistent tension signal = 1

        var dummy = new Store(1, 2);
        var spareRoute = new Wiring(Flows.Move(1, 1), new Binding(dummy, 0), new Binding(dummy, 0));
        spareRoute.AddSpare(0, 1);

        var engine = new Engine().Hold(store, dummy);
        var track = new Wiring(TensionLaws.Streak(4, countWhenSet: true), new Binding(store, 10), streak.On(store));
        track.Add(0, 0);
        engine.Add(track);
        Tension.ObserveThreshold(engine, store, streak, store, threshold, store, belowPlane: 8, abovePlane: 9, positions: 0);

        // Recruit spare route whenever streak >= threshold (~belowPlane), using a 1-bit recruit law (RecruitAt(1, 1)).
        var recruit = new Wiring(TensionLaws.RecruitAt(1, 1), new Binding(new Flipped(store, 8), 8), new Binding(spareRoute.Rows.Life, 0));
        recruit.Add(0, 0);
        engine.Add(recruit).Add(spareRoute);

        // Run 2 steps: streak reaches 2 < threshold (3) -> spare route stays dormant!
        engine.Run(2);
        Assert.Equal(2, streak.Read(store, 0));
        Assert.True(store.Read(8, 0));   // below = 1
        Assert.False(spareRoute.Live[0]);

        // Lower threshold DATA in the store from 3 to 2 (no law table change!) and verify Transport round-trip too!
        threshold.Write(store, 0, 2);
        var (restored, copies) = Transport.Load(Transport.Save(engine, store, spareRoute.Rows));
        engine.Step();
        restored.Step();
        Assert.False(store.Read(8, 0)); // streak >= threshold now!
        Assert.True(spareRoute.Live[0]);
        Assert.True(copies[1].IsLive(0));

        // Part 2: In-machine lexicographic ranking (MaterialMinus outranks Goal) using ObserveMismatch + RankCombine.
        var rankStore = new Store(18, 1);
        var matBefore = new Field(Shape.Unary(3), 0, 1, 2);
        var matAfter = new Field(Shape.Unary(3), 3, 4, 5);
        var goalBefore = new Field(Shape.Unary(3), 6, 7, 8);
        var goalAfter = new Field(Shape.Unary(3), 9, 10, 11);
        // Planes 12,13: (matDropped, matRose); 14,15: (goalDropped, goalRose); 16,17: (overallDropped, overallRose).
        matBefore.Write(rankStore, 0, 1);
        matAfter.Write(rankStore, 0, 0);  // MaterialMinus dropped 1 -> 0!
        goalBefore.Write(rankStore, 0, 0);
        goalAfter.Write(rankStore, 0, 1); // Goal rose 0 -> 1!

        var rankEngine = new Engine();
        // Compare after (have) vs before (want): needMore = (after < before = dropped!), haveExtra = (after > before = rose!).
        Tension.ObserveMismatch(rankEngine, rankStore, matAfter, rankStore, matBefore, rankStore, 12, 13);
        Tension.ObserveMismatch(rankEngine, rankStore, goalAfter, rankStore, goalBefore, rankStore, 14, 15);
        var combine = new Wiring(
            TensionLaws.RankCombine,
            new Binding(rankStore, 12), new Binding(rankStore, 13),
            new Binding(rankStore, 14), new Binding(rankStore, 15),
            new Binding(rankStore, 16), new Binding(rankStore, 17));
        combine.Add(0, 0, 0, 0, 0, 0);
        rankEngine.Add(combine);

        Assert.NotNull(rankEngine.Settle(8));
        Assert.Equal((true, false), (rankStore.Read(16, 0), rankStore.Read(17, 0))); // Material drop dominates Goal rise!

        // When Material ties (1 -> 1) and Goal rises (0 -> 1), overall is (dropped=false, rose=true) -> refused!
        matAfter.Write(rankStore, 0, 1);
        Assert.NotNull(rankEngine.Settle(8));
        Assert.Equal((false, true), (rankStore.Read(16, 0), rankStore.Read(17, 0)));
    }

    /// <summary>When both suppliers are available, the open supplier choice
    /// raises demanded information deficit (<c>RouteDeficit = 1</c>) while
    /// effective stock is short, delivers from neither supplier, and settles
    /// quietly in both <see cref="Engine.Settle"/> and
    /// <see cref="Engine.StepTogether"/>. When at target, openness raises zero
    /// tension; when both suppliers are out, contradiction (<c>RouteExcess = 1</c>)
    /// lights under demand and settles quietly.</summary>
    [Fact]
    public void OpenSupplierChoiceRaisesDemandedTensionAndStaysQuietWhenStuck()
    {
        var world = BuildShop(casesA: 2, casesB: 2, availA: true, availB: true, initialShelf: 1, targetLevel: 4);

        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal(0b11UL, world.RouteSupports.Values(0));
        Assert.True(world.ShopStore.Read(world.NeedMorePlane, 0));
        Assert.True(world.RouteStore.Read(ShopWorld.DeficitPlane, 0));
        Assert.False(world.RouteStore.Read(ShopWorld.ExcessPlane, 0));
        Assert.Equal((2, 2, 1), (world.Cases.Read(world.SupAStore, 0), world.Cases.Read(world.SupBStore, 0), world.Shelf.Read(world.ShopStore, 0)));

        // Single-writer combinational fold trees also settle quietly under StepTogether with zero self-contention.
        var togetherWorld = BuildShop(casesA: 2, casesB: 2, availA: true, availB: true, initialShelf: 1, targetLevel: 4);
        Assert.NotNull(togetherWorld.Shop.Settle(16, together: true));
        Assert.True(togetherWorld.ShopStore.Read(togetherWorld.NeedMorePlane, 0));
        Assert.True(togetherWorld.RouteStore.Read(ShopWorld.DeficitPlane, 0));

        // At target: openness without demand produces zero tension.
        world.Shelf.Write(world.ShopStore, 0, 4);
        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal(0b11UL, world.RouteSupports.Values(0));
        Assert.False(world.RouteStore.Read(ShopWorld.DeficitPlane, 0));
        Assert.False(world.RouteStore.Read(ShopWorld.ExcessPlane, 0));

        // Short AND both suppliers out -> contradiction under demand (Excess = 1), settles quietly.
        world.Shelf.Write(world.ShopStore, 0, 1);
        world.RouteStore.Write(ShopWorld.AvailAPlane, 0, false);
        world.RouteStore.Write(ShopWorld.AvailBPlane, 0, false);
        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal(0UL, world.RouteSupports.Values(0));
        Assert.False(world.RouteStore.Read(ShopWorld.DeficitPlane, 0));
        Assert.True(world.RouteStore.Read(ShopWorld.ExcessPlane, 0));

        var profile = TensionDiagnostics.Measure(
            world.Shop,
            sites: [new TensionSite(world.ShopStore, world.Shelf, world.Dock, world.Backorder)],
            demands: [new TensionDemand(world.RouteSupports, world.ShopStore.Read(world.NeedAndClearPlane))]);
        Assert.True(profile.Obligation >= 1);
    }

    /// <summary>Trialing "switch supplier" in an isolated-port
    /// <see cref="Fork"/> over a shop taking through foreign sockets: default
    /// <c>Fork()</c> still throws, <c>Fork(isolatePorts: true)</c> proves
    /// strict admissibility without touching live shop or suppliers, and
    /// refuses <see cref="Fork.Commit"/> to protect conservation.</summary>
    [Fact]
    public void ForkTrialsSwitchingSupplierWithoutTouchingLiveShopOrSuppliers()
    {
        var world = BuildShop(casesA: 0, casesB: 3, availA: true, availB: false, initialShelf: 0, targetLevel: 2);
        world.Stock.Demand(Plane.Ones(1));
        world.Stock.Demand(Plane.Ones(1));

        Assert.NotNull(world.Shop.Settle(16));
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));

        Assert.Throws<ArgumentException>(() => world.Shop.Fork());

        var trial = world.Shop.Fork(isolatePorts: true);
        Assert.True(trial.PortsIsolated);
        var trialSite = new TensionSite(trial.Of(world.ShopStore), world.Shelf, world.Dock, world.Backorder);
        var beforeTrial = TensionDiagnostics.Measure(trial.Engine, sites: [trialSite]);

        var trialRoute = trial.Of(world.RouteStore);
        trialRoute.Write(ShopWorld.AvailAPlane, 0, false);
        trialRoute.Write(ShopWorld.AvailBPlane, 0, true);
        trialRoute.Write(world.RouteSupports.Planes[0], 0, true);
        trialRoute.Write(world.RouteSupports.Planes[1], 0, true);

        Assert.NotNull(trial.Engine.Settle(24));
        var afterTrial = TensionDiagnostics.Measure(trial.Engine, sites: [trialSite]);

        Assert.Equal(2, beforeTrial.MaterialMinus);
        Assert.Equal(0, afterTrial.MaterialMinus);
        Assert.Equal(2, world.Shelf.Read(trial.Of(world.ShopStore), 0));
        Assert.Equal(1, world.Cases.Read(trial.PortView(world.SocketB), 0));
        Assert.True(TensionDiagnostics.AdmissibleStrict(beforeTrial, afterTrial).Admitted);

        Assert.Equal(3, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal((0, 2), (world.Shelf.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));
        Assert.False(trial.Commit());
    }

    /// <summary>Provenance question ("which supplier delivered this batch?")
    /// pursued via <see cref="Question.Pursue"/> with conflicting delivery
    /// notes resolved by <see cref="TensionLaws.YieldByFirmness"/>—and an
    /// equal-firmness tie preserved as a visible contradiction.</summary>
    [Fact]
    public void BatchProvenanceQuestionUsesClosureAndResolvesConflictingDeliveryNotesByFirmness()
    {
        var batches = new Store(1, 2);
        var provenance = Supports.On(batches, Field.Bit(0));

        var notes = new Store(1, 3);
        notes.Write(0, 0, false); // clerk slip: Supplier A
        notes.Write(0, 1, true);  // signed invoice: Supplier B
        notes.Write(0, 2, false); // unrelated batch 1 note

        var firmness = new Store(3, 2);
        var firmField = new Field(Shape.Unary(3), 0, 1, 2);
        firmField.Write(firmness, 0, 1);
        firmField.Write(firmness, 1, 3);

        var clerkClaim = new Wiring(Settling.Same(Shape.Bit), new Binding(notes, 0), provenance.Variable);
        clerkClaim.Add(0, 0);
        var invoiceClaim = new Wiring(Settling.Same(Shape.Bit), new Binding(notes, 0), provenance.Variable);
        invoiceClaim.Add(1, 0);
        var otherNote = new Wiring(Settling.Same(Shape.Bit), new Binding(notes, 0), provenance.Variable);
        otherNote.Add(2, 1);

        var status = new Store(5, 2);
        status.Write(0, 0, true);

        var engine = new Engine().Add(clerkClaim).Add(invoiceClaim).Add(otherNote);
        Tension.ObserveSupports(engine, provenance, status, 0, status, 1, 2, 3, 4, positions: 0);

        var question = new Question(provenance, 0);
        var cone = question.Cone(engine, reach: 1);
        Assert.True(cone.Rows(clerkClaim)[0] && cone.Rows(invoiceClaim)[0]);
        Assert.False(cone.Of(batches)[1]);

        Assert.Equal(0, question.Pursue(engine, reach: 1));
        Assert.True(status.Read(4, 0));
        Assert.Equal(0b11UL, provenance.Values(1));

        var yieldClerk = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(status, 4), firmField.On(firmness), firmField.On(firmness), new Binding(clerkClaim.Rows.Life, 0));
        yieldClerk.Add(0, 0, 1, 0);
        var yieldInvoice = new Wiring(TensionLaws.YieldByFirmness(3),
            new Binding(status, 4), firmField.On(firmness), firmField.On(firmness), new Binding(invoiceClaim.Rows.Life, 0));
        yieldInvoice.Add(0, 1, 0, 0);
        engine.Add(yieldClerk).Add(yieldInvoice);

        Assert.NotNull(engine.Settle(8));
        Assert.True(clerkClaim.Rows.IsRetired(0));
        Assert.True(invoiceClaim.Live[0]);

        provenance.Forget(0);
        Assert.Equal(1, question.Pursue(engine, reach: 1));
        Assert.Equal(1, question.Answer);
        Assert.False(status.Read(3, 0));
        Assert.False(status.Read(4, 0));
        Assert.Equal(0b11UL, provenance.Values(1));

        // Equal-firmness tie (2 vs 2) stays visibly contradicted.
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
        var y1 = new Wiring(TensionLaws.YieldByFirmness(3), new Binding(tiedStatus, 4), firmField.On(tiedFirm), firmField.On(tiedFirm), new Binding(c1.Rows.Life, 0));
        y1.Add(0, 0, 1, 0);
        var y2 = new Wiring(TensionLaws.YieldByFirmness(3), new Binding(tiedStatus, 4), firmField.On(tiedFirm), firmField.On(tiedFirm), new Binding(c2.Rows.Life, 0));
        y2.Add(0, 1, 0, 0);
        tiedEngine.Add(y1).Add(y2);
        Assert.NotNull(tiedEngine.Settle(8));
        Assert.True(c1.Live[0] && c2.Live[0]);
        Assert.Equal(0UL, tiedProv.Values(0));
        Assert.True(tiedStatus.Read(4, 0));
    }

    /// <summary>Host-side <see cref="TensionDiagnostics"/> ranking,
    /// non-netting check, and duplicate reorder-rule detection/retirement.</summary>
    [Fact]
    public void BackorderOutranksShelfTargetAndDuplicateReorderRulesAreDetectedAndRetired()
    {
        var store = new Store(12, 1);
        var shelf = new Field(Shape.Unary(3), 0, 1, 2);
        var target = new Field(Shape.Unary(3), 3, 4, 5);
        var backorder = new Field(Shape.Unary(3), 6, 7, 8);
        var deficit = new Field(Shape.Unary(3), 9, 10, 11);
        shelf.Write(store, 0, 3);
        target.Write(store, 0, 3);
        backorder.Write(store, 0, 1);
        deficit.Write(store, 0, 0);

        var engine = new Engine().Hold(store);
        var beforePay = TensionDiagnostics.Measure(
            engine,
            sites: [new TensionSite(store, shelf, null, backorder)],
            goals: [new TensionGoal(store, deficit, null)]);

        var pay = Tension.Draw(store, backorder, shelf);
        pay.Add(0, 0);
        pay.Apply();
        deficit.Write(store, 0, 1);

        var afterPay = TensionDiagnostics.Measure(
            engine,
            sites: [new TensionSite(store, shelf, null, backorder)],
            goals: [new TensionGoal(store, deficit, null)]);

        Assert.Equal((1, 0), (beforePay.MaterialMinus, beforePay.Goal));
        Assert.Equal((0, 1), (afterPay.MaterialMinus, afterPay.Goal));
        Assert.True(TensionDiagnostics.AdmissibleStrict(beforePay, afterPay).Admitted);

        var world = BuildShop(casesA: 2, casesB: 2, availA: false, availB: true, initialShelf: 2, targetLevel: 4);
        world.Dock.Write(world.ShopStore, 0, 2);
        world.Backorder.Write(world.ShopStore, 0, 1);
        world.InFlightGate.Add(0, 0, 0); // duplicate reorder-gate row

        var dups = TensionDiagnostics.Duplicates(world.Shop);
        var dup = Assert.Single(dups);
        Assert.Equal((0, 1), (dup.FirstRow, dup.DuplicateRowIndex));

        var shopProfile = TensionDiagnostics.Measure(
            world.Shop,
            sites: [new TensionSite(world.ShopStore, world.Shelf, world.Dock, world.Backorder)]);
        Assert.Equal(2, shopProfile.MaterialPlus);
        Assert.Equal(1, shopProfile.MaterialMinus);
        Assert.Equal(3, shopProfile.MaterialTotal); // never netted
        Assert.Equal(1, shopProfile.StructuralExcess);

        var fork = world.Shop.Fork(isolatePorts: true);
        var forkSite = new TensionSite(fork.Of(world.ShopStore), world.Shelf, world.Dock, world.Backorder);
        var beforeRetire = TensionDiagnostics.Measure(fork.Engine, sites: [forkSite]);
        fork.Of(world.InFlightGate).Retire(1);
        var afterRetire = TensionDiagnostics.Measure(fork.Engine, sites: [forkSite]);

        Assert.Equal(1, beforeRetire.StructuralExcess);
        Assert.Equal(0, afterRetire.StructuralExcess);
        Assert.True(afterRetire.DescriptionBits < beforeRetire.DescriptionBits);
        Assert.True(TensionDiagnostics.AdmissibleStrict(beforeRetire, afterRetire).Admitted);
    }

    /// <summary>Two products share one corner-shop store and supplier ports:
    /// product 0 holds cases at the supplier while dock room is full and then
    /// restocks cleanly; product 1 stays at target with an open supplier choice
    /// and zero tension.</summary>
    [Fact]
    public void MultiProductShopIsolatesProductsAndRespectsLimitedDockRoom()
    {
        var world = BuildShop(casesA: 2, casesB: 3, availA: true, availB: true, initialShelf: 0, targetLevel: 4, products: 2);

        world.RouteStore.Write(ShopWorld.AvailAPlane, 0, false);
        world.Dock.Write(world.ShopStore, 0, 3);
        world.Backorder.Write(world.ShopStore, 0, 1);
        world.Shelf.Write(world.ShopStore, 1, 4);

        var net0 = world.NetUnits(0);
        var net1 = world.NetUnits(1);

        world.Shop.Step();
        Assert.Equal(3, world.Cases.Read(world.SupBStore, 0));
        Assert.Equal(net0, world.NetUnits(0));
        Assert.Equal(net1, world.NetUnits(1));

        Assert.NotNull(world.Shop.Settle(24));
        Assert.Equal(net0, world.NetUnits(0));
        Assert.Equal(net1, world.NetUnits(1));
        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 0), world.Dock.Read(world.ShopStore, 0), world.Backorder.Read(world.ShopStore, 0)));
        Assert.Equal(2, world.Cases.Read(world.SupBStore, 0));

        Assert.Equal((4, 0, 0), (world.Shelf.Read(world.ShopStore, 1), world.Dock.Read(world.ShopStore, 1), world.Backorder.Read(world.ShopStore, 1)));
        Assert.Equal(3, world.Cases.Read(world.SupBStore, 1));
        Assert.Equal(0b11UL, world.RouteSupports.Values(1));
        Assert.False(world.RouteStore.Read(ShopWorld.DeficitPlane, 1));
    }
}
