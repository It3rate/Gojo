using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A corner shop selling one product, as a test of tension in a real
/// setting. Every part is a local structure of its own, joined only by ports:
/// <list type="bullet">
/// <item><b>A customer</b> posts requests, which the shop takes through the
/// customer's port as debt (backorders), and collects goods from the shop's
/// pickup counter through the shop's port.</item>
/// <item><b>Wholesalers</b> hold cases of two items behind a port. The shop
/// reaches each through a socket, so a line can be cut and restored.</item>
/// <item><b>The shop</b> holds a shelf of four, with the receiving dock beside
/// it as pressure and backorders as debt, a pickup counter, and orders on the
/// books.</item>
/// </list>
/// <para><b>How the shop thinks</b> is counting laws, declared before what reads
/// them: on hand is shelf plus dock plus two per case on order; wanted is a
/// target plus backorders; the shortfall between them, read against a reorder
/// point held as data, places an order. A wholesaler's delivery fills an order:
/// a case leaves the warehouse, two items arrive at the dock and the order is
/// closed in one move. Customers are served the same way, from the shelf to the
/// pickup counter against a backorder. Every unit is accounted for across all
/// the structures.</para>
/// <para>Nothing here is computed in host code: the host only builds the
/// structures, steps them, and posts what arrives from outside.</para></summary>
public sealed class ShopTests
{
    private const int Shelf = 4, Dock = 4, Owed = 4, Pickup = 2, OnOrder = 3, Asked = 8, Cases = 3, PerCase = 2, Point = 4;
    private const int A = 0, B = 1;
    private static readonly Plane Here = Plane.Of(1, 0);

    private sealed record Customer(Engine Engine, Store Store, Field Requests, Field Basket, Port Out);

    private sealed record Wholesaler(Engine Engine, Store Warehouse, Field Cases, Port Out, Port Status);

    private sealed record Shop(Engine Engine, Store Store, Tension Stock, Field Target, Field OnOrder, Field Pickup, Supports? Choice, Wiring[] Fills, Socket[] Lines, Field[] Receipts);

    private sealed record World(Customer Customer, Wholesaler[] Wholesalers, Shop Shop)
    {
        public Engine[] Engines => [Customer.Engine, .. Wholesalers.Select(w => w.Engine), Shop.Engine];

        /// <summary>Steps every structure, customer first, until a round
        /// changes nothing anywhere.</summary>
        public void Settle()
        {
            for (var round = 0; round < 256; round++)
                if (Engines.Select(engine => engine.Step()).ToArray().All(step => step.Steps.All(s => s.Quiet))) return;
            throw new InvalidOperationException("The shop did not come to rest.");
        }

        public void Request(int items)
        {
            for (var i = 0; i < items; i++) Assert.True(Unary.Add(Customer.Store, Customer.Requests, Here).Blocked.None);
        }

        public (int? Shelf, int? Dock, int? Owed, int? OnOrder, int? Basket) Floor => (
            Shop.Stock.Value.Read(Shop.Store, 0), Shop.Stock.Plus.Read(Shop.Store, 0), Shop.Stock.Minus.Read(Shop.Store, 0),
            Shop.OnOrder.Read(Shop.Store, 0), Customer.Basket.Read(Customer.Store, 0));

        public int[] Warehouses => [.. Wholesalers.Select(w => w.Cases.Read(w.Warehouse, 0)!.Value)];

        /// <summary>Items anywhere: in cases, at the dock, on the shelf, at the
        /// counter, in the basket. Requests anywhere: still posted, owed, at
        /// the counter or collected. Both are conserved by every move.</summary>
        public (int Items, int Requests) Totals
        {
            get
            {
                int Read(Field f, Store s) => f.Read(s, 0)!.Value;
                var counter = Read(Shop.Pickup, Shop.Store);
                var basket = Read(Customer.Basket, Customer.Store);
                return (PerCase * Warehouses.Sum() + Read(Shop.Stock.Plus, Shop.Store) + Read(Shop.Stock.Value, Shop.Store) + counter + basket,
                    Read(Customer.Requests, Customer.Store) + Read(Shop.Stock.Minus, Shop.Store) + counter + basket);
            }
        }
    }

    private static Wholesaler Supplier(int cases, bool open = true)
    {
        var warehouse = new Store(Cases + 1, 1);
        var count = new Field(Shape.Unary(Cases), 0, 1, 2);
        count.Write(warehouse, 0, cases);
        warehouse.Write(Cases, 0, open);
        return new Wholesaler(new Engine().Hold(warehouse), warehouse, count, warehouse.Publish([0], [0, 1, 2]), warehouse.Publish([0], [Cases]));
    }

    /// <summary>The customer, the shop and its wholesalers. <paramref name="first"/>
    /// adds laws that run before the shop's own, such as what justifies a
    /// wholesaler being possible. Wholesalers listed in <paramref name="spare"/>
    /// are wired but not in use until the shop recruits them: when backorders
    /// have persisted for <paramref name="patience"/> steps.</summary>
    private static World Open(int target, int shelf = 0, int dock = 0, Wholesaler[]? wholesalers = null, bool countOnOrder = true,
        Func<Store, Supports?, IEnumerable<Wiring>>? first = null, int[]? spare = null, int patience = 3)
    {
        wholesalers ??= [];
        spare ??= [];
        var customerStore = new Store(2 * Asked, 1);
        var requests = new Field(Shape.Unary(Asked), [.. Enumerable.Range(0, Asked)]);
        var basket = new Field(Shape.Unary(Asked), [.. Enumerable.Range(Asked, Asked)]);
        var customer = new Customer(new Engine().Hold(customerStore), customerStore, requests, basket, customerStore.Publish([0], [.. requests.Planes]));

        var store = new Store(0, 1);
        Field Count(int capacity) => new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
        Binding Bit(int plane) => new(store, plane);
        var stock = new Tension(store, Count(Shelf), Dock);
        stock.Value.Write(store, 0, shelf);
        stock.Plus.Write(store, 0, dock);
        var (wanted, onOrder, counter, point) = (Count(Shelf), Count(OnOrder), Count(Pickup), Count(Point));
        wanted.Write(store, 0, target);
        point.Write(store, 0, 1);
        var (have, want) = (Count(Shelf + Dock), Count(Shelf + Owed));
        var all = countOnOrder ? Count(Shelf + Dock + PerCase * OnOrder) : have;
        var (shortBy, orderNow) = (Count(Shelf + Owed), store.AddPlane());
        var choice = wholesalers.Length > 1 ? Supports.On(store, new Field(Shape.Binary(wholesalers.Length), store.AddPlane())) : null;

        var engine = new Engine();
        foreach (var wiring in first?.Invoke(store, choice) ?? []) engine.Add(wiring);
        engine.Add(Tension.Wire(Counts.Sum(Shelf, Dock), [stock.Value.On(store), stock.Plus.On(store), have.On(store)], Here));
        if (countOnOrder) engine.Add(Tension.Wire(Counts.Sum(Shelf + Dock, OnOrder, PerCase), [have.On(store), onOrder.On(store), all.On(store)], Here));
        engine.Add(Tension.Wire(Counts.Sum(Shelf, Owed), [wanted.On(store), stock.Minus.On(store), want.On(store)], Here));
        engine.Add(Tension.Wire(Counts.Shortfall(all.Planes.Count, Shelf + Owed), [all.On(store), want.On(store), shortBy.On(store)], Here));
        engine.Add(Tension.Wire(Counts.AtLeast(Shelf + Owed, Point), [shortBy.On(store), point.On(store), Bit(orderNow)], Here));

        var lines = wholesalers.Select(w => { var line = Socket.For(w.Out); line.Plug(w.Out); return line; }).ToArray();
        var receipts = wholesalers.Select(_ => Count(Cases)).ToArray();
        var fills = wholesalers.Select((w, i) =>
        {
            var roles = new[] { w.Cases.On(lines[i]), onOrder.On(store), stock.Plus.On(store), receipts[i].On(store) };
            var law = Flows.Fulfill(Cases, OnOrder, Dock, take: 1, give: PerCase, tallyCapacity: Cases);
            if (!spare.Contains(i)) return Tension.Wire(law, roles, Here, choice is null ? [] : [choice.Possible(i)]);
            var fill = new Wiring(law, roles);
            fill.AddSpare(0, 0, 0, 0);
            return fill;
        }).ToArray();
        if (spare.Length > 0)
        {
            var (streak, wait, due) = (Count(3), Count(3), store.AddPlane());
            wait.Write(store, 0, patience);
            engine.Add(Tension.Wire(Counts.Streak(3), [Bit(stock.Minus.Planes[0]), streak.On(store)], Here));
            engine.Add(Tension.Wire(Counts.AtLeast(3, 3), [streak.On(store), wait.On(store), Bit(due)], Here));
            foreach (var i in spare)
            {
                var recruit = new Wiring(Settling.Latch(true), Bit(due), new Binding(fills[i].Rows.Life, 0));
                recruit.Add(0, 0);
                engine.Add(recruit);
            }
        }
        engine.Add(Tension.Wire(Counts.Accumulate(OnOrder), [Bit(orderNow), onOrder.On(store)], Here));
        engine.Add(Tension.Route(customer.Requests.On(customer.Out), stock.Minus.On(store), Here));
        engine.Add(stock.Deliver(counter.On(store), Here));
        engine.Add(stock.Absorb(Here));
        foreach (var fill in fills) engine.Add(fill);

        var counterPort = store.Publish([0], [.. counter.Planes]);
        customer.Engine.Add(Tension.Route(new Field(Shape.Unary(Pickup), [.. Enumerable.Range(0, Pickup)]).On(counterPort), basket.On(customerStore), Here));
        return new World(customer, wholesalers, new Shop(engine, store, stock, wanted, onOrder, counter, choice, fills, lines, receipts));
    }

    /// <summary>Iteration 1, material only. The shop has two on the shelf and
    /// three at the dock, and no wholesaler. A customer asks for six. Requests
    /// become backorders, five are handed over at the counter and collected,
    /// and the sixth stays owed, visible, with a case booked against it that
    /// nobody can fill. Items and requests are conserved throughout.</summary>
    [Fact]
    public void RequestsBecomeBackordersAndAreHandedOverAcrossTheCounter()
    {
        var world = Open(target: 0, shelf: 2, dock: 3);
        world.Request(6);
        var before = world.Totals;

        world.Settle();

        Assert.Equal((0, 0, 1, 1, 5), world.Floor);
        Assert.Equal(before, world.Totals);
    }

    /// <summary>Iteration 2, reordering. The shop wants three on hand. It books a
    /// case while it is short of its reorder point, counting what is on order,
    /// and a delivery fills each order. It stops at four, whole cases, with one
    /// case left at the wholesaler. Selling three then books one more.</summary>
    [Fact]
    public void TheShopReordersByItsShortfallAndDeliveriesCloseTheOrders()
    {
        var world = Open(target: 3, wholesalers: [Supplier(cases: 3)]);
        world.Settle();
        Assert.Equal((4, 0, 0, 0, 0), world.Floor);
        Assert.Equal([1], world.Warehouses);

        world.Request(3);
        world.Settle();
        Assert.Equal((3, 0, 0, 0, 3), world.Floor);
        Assert.Equal([0], world.Warehouses);
    }

    /// <summary>With the line to the wholesaler cut, orders stay on the books.
    /// Counting them, the shop books two cases, enough, and waits; restored,
    /// the line delivers them and the shop is at four. A rule that does not
    /// count what is on order keeps booking, three cases, and when the line is
    /// restored it takes the wholesaler's last case and leaves two extra at the
    /// dock and one case still owed.</summary>
    [Fact]
    public void OrdersOnTheBooksAreCountedWhileTheLineIsCut()
    {
        foreach (var counted in new[] { true, false })
        {
            var world = Open(target: 3, wholesalers: [Supplier(cases: 3)], countOnOrder: counted);
            world.Shop.Lines[0].Unplug();
            world.Settle();
            Assert.Equal((0, 0, 0, counted ? 2 : 3, 0), world.Floor);

            world.Shop.Lines[0].Plug(world.Wholesalers[0].Out);
            world.Settle();
            Assert.Equal(counted ? (4, 0, 0, 0, 0) : (4, 2, 0, 1, 0), world.Floor);
            Assert.Equal([counted ? 1 : 0], world.Warehouses);
        }
    }

    /// <summary>Iteration 3, two wholesalers, A closed today. Which one to order
    /// from is an open choice, one support per wholesaler. A law reading A's
    /// status through a port makes A impossible, so B fills the first orders.
    /// Then A opens again and the customer asks for eight.
    /// <para><b>Narrowed,</b> A stays ruled out: B runs dry and the shop ends with
    /// two owed and three cases on order while A has three. <b>Justified,</b> "A
    /// is possible" is held only while A is open, by one law writing it from that
    /// reason each step; it reopens with A, and everything is delivered. Each
    /// line's receipt tally says which wholesaler the cases came from.</para></summary>
    [Fact]
    public void AClosedWholesalerIsOrderedFromAgainOnlyWhenItsPossibilityIsJustified()
    {
        foreach (var justified in new[] { false, true })
        {
            var (a, b) = (Supplier(cases: 3, open: false), Supplier(cases: 3));
            var world = Open(target: 3, wholesalers: [a, b], first: (store, choice) =>
            {
                var rule = justified
                    ? new Wiring(Settling.Justify(1), new Binding(a.Status, 0), choice!.Possible(A))
                    : new Wiring(RelationLaw.FromPredicate([Shape.Bit, Shape.Binary(2)], t => t[0] == 1 || t[1] != A), new Binding(a.Status, 0), choice!.Variable);
                rule.Add(0, 0);
                return [rule];
            });
            world.Settle();
            Assert.Equal((4, 0, 0, 0, 0), world.Floor);
            Assert.Equal([3, 1], world.Warehouses);

            a.Warehouse.Write(Cases, 0, true);
            world.Request(4);
            world.Settle();
            world.Request(4);
            world.Settle();

            Assert.Equal(justified ? (4, 0, 0, 0, 8) : (0, 0, 2, 3, 6), world.Floor);
            Assert.Equal(justified ? [0, 0] : [3, 0], world.Warehouses);
            Assert.Equal(justified ? [3, 3] : [0, 3], world.Shop.Receipts.Select(r => r.Read(world.Shop.Store, 0)!.Value));
        }
    }

    /// <summary>Iteration 4, the slow clock. A has one case; B is wired but not in
    /// use. A single request is served from the shelf at once, so no backorder
    /// lasts and B stays unused. Three more leave two owed with A dry; once
    /// backorders have lasted three steps, a streak read against that patience
    /// recruits B's line, and everything owed is delivered. The structure
    /// changed itself because tension persisted, not because the host chose.</summary>
    [Fact]
    public void PersistentBackordersRecruitASpareWholesaler()
    {
        var world = Open(target: 3, wholesalers: [Supplier(cases: 1), Supplier(cases: 3)], spare: [B]);
        world.Settle();
        Assert.Equal((2, 0, 0, 1, 0), world.Floor);

        world.Request(1);
        world.Settle();
        Assert.False(world.Shop.Fills[B].Live[0]);
        Assert.Equal((1, 0, 0, 1, 1), world.Floor);

        world.Request(3);
        world.Settle();
        Assert.True(world.Shop.Fills[B].Live[0]);
        Assert.Equal((4, 0, 0, 0, 4), world.Floor);
        Assert.Equal([0, 0], world.Warehouses);
    }

    /// <summary>Iteration 5, the books. Which wholesaler each batch came from is
    /// an open value narrowed by delivery notes, and an invoice is paid only once
    /// that is settled: a tally of the batch's supports, read as "one possible,
    /// not two".
    /// <list type="bullet">
    /// <item>Batch 0 has one note, from B: paid to B.</item>
    /// <item>Batch 1 has a clerk's slip saying A and a signed invoice saying B.
    /// They contradict, and the slip is less firm, so a law retires the slip's
    /// row and another reopens the batch; the invoice alone narrows it to B, and
    /// it is paid to B. Revision is done by structure.</item>
    /// <item>Batch 2 has two equally firm notes that disagree: it stays
    /// contradicted and unpaid.</item>
    /// <item>Batch 3 has no note: it stays open and unpaid.</item>
    /// </list>
    /// "Which supplier did batch 1 come from?" is answered, so its cone is sealed
    /// at the batch; opened once, it holds only the invoice, since the slip's row
    /// is retired.</summary>
    [Fact]
    public void TheBooksPayOnceSettledAndRetireTheWeakerOfTwoConflictingNotes()
    {
        const int Batches = 4;
        var books = new Store(0, Batches);
        var from = Supports.On(books, new Field(Shape.Binary(2), books.AddPlane()));
        var possible = new Field(Shape.Unary(2), books.AddPlane(), books.AddPlane());
        var owed = new Field(Shape.Unary(1), books.AddPlane());
        Field[] paid = [new(Shape.Unary(1), books.AddPlane()), new(Shape.Unary(1), books.AddPlane())];
        for (var batch = 0; batch < Batches; batch++) owed.Write(books, batch, 1);

        (int Batch, int Supplier, int Firmness)[] said = [(0, B, 1), (1, A, 1), (1, B, 3), (2, A, 2), (2, B, 2)];
        (int Note, int Rival)[] rivals = [(1, 2), (2, 1), (3, 4), (4, 3)];
        var notes = new Store(0, said.Length);
        var supplier = new Field(Shape.Binary(2), notes.AddPlane());
        var firmness = new Field(Shape.Unary(3), notes.AddPlane(), notes.AddPlane(), notes.AddPlane());
        var outranked = new Field(Shape.Unary(3), notes.AddPlane(), notes.AddPlane(), notes.AddPlane());
        var yields = notes.AddPlane();
        var reading = new Wiring(Settling.Same(Shape.Binary(2)), supplier.On(notes), from.Variable);
        for (var n = 0; n < said.Length; n++)
        {
            supplier.Write(notes, n, said[n].Supplier);
            firmness.Write(notes, n, said[n].Firmness);
            reading.Add(n, said[n].Batch);
        }

        var anyPossible = new Binding(books, possible.Planes[0]);
        var contradicted = new Binding(new Flipped(books, possible.Planes[0]), possible.Planes[0]);
        var notTwo = new Binding(new Flipped(books, possible.Planes[1]), possible.Planes[1]);
        var tally = Tension.Wire(Counts.Tally(2), [.. from.Planes.Select(p => new Binding(books, p)), possible.On(books)], Plane.Ones(Batches));
        var outrank = new Wiring(Counts.Shortfall(3, 3), firmness.On(notes), firmness.On(notes), outranked.On(notes));
        var yield = new Wiring(Settling.Justify(2), contradicted, new Binding(notes, outranked.Planes[0]), new Binding(notes, yields));
        var retire = new Wiring(Settling.Latch(false), new Binding(notes, yields), new Binding(reading.Rows.Life, 0));
        var reopen = new Wiring(Flows.When(Settling.Forget(2)), new Binding(books, [.. from.Planes]), new Binding(notes, yields));
        foreach (var (note, rival) in rivals)
        {
            outrank.Add(note, rival, note);
            yield.Add(said[note].Batch, note, note);
            retire.Add(note, note);
            reopen.Add(said[note].Batch, note);
        }
        var engine = new Engine().Add(reading).Add(tally).Add(outrank).Add(yield).Add(retire).Add(reopen);
        foreach (var s in new[] { A, B })
            engine.Add(Tension.Route(owed.On(books), paid[s].On(books), Plane.Ones(Batches), from.Possible(s), anyPossible, notTwo));

        Assert.NotNull(engine.Settle(32));

        Assert.Equal([1, 1, 0, 2], Enumerable.Range(0, Batches).Select(batch => new Question(from, batch).Remaining));
        Assert.Equal([B, B], new[] { 0, 1 }.Select(batch => new Question(from, batch).Answer!.Value));
        Assert.Equal([0, 0, 1, 1], Enumerable.Range(0, Batches).Select(batch => owed.Read(books, batch)));
        Assert.Equal([1, 1, 0, 0], Enumerable.Range(0, Batches).Select(batch => paid[B].Read(books, batch)));
        Assert.Equal(Plane.Of(said.Length, 0, 2, 3, 4), reading.Live);
        Assert.True(new Question(from, 1).Cone(engine).Rows(reading).None);
        Assert.Equal(Plane.Of(said.Length, 2), new Question(from, 1).Cone(engine, open: 1).Rows(reading));

        var saved = Transport.Save(engine);
        Assert.Equal(saved, Transport.Save(Transport.Load(saved).Engine));
    }

    /// <summary>Iteration 6, a trial: "buy only from B". A fork as it is would
    /// act on the wholesalers through the shop's lines, so it is refused. With
    /// ports isolated, the fork takes from snapshots of what each line, and the
    /// customer's requests, showed when it was made, and runs there with A
    /// ruled out: B's snapshot gives both cases and the fork's shop is at four.
    /// Nothing reaches the live structures, and the fork will not commit, since
    /// what it took from a snapshot was never taken from the wholesaler.</summary>
    [Fact]
    public void ATrialRunsInAForkThatTakesFromSnapshotsOfItsLines()
    {
        var world = Open(target: 3, wholesalers: [Supplier(cases: 1), Supplier(cases: 3)]);
        Assert.Throws<ArgumentException>(() => world.Shop.Engine.Fork());

        var before = Transport.SaveLocals(world.Engines);
        var trial = world.Shop.Engine.Fork(isolatePorts: true);
        var view = trial.Of(world.Shop.Store);
        new Supports(view, world.Shop.Choice!.Field, [.. world.Shop.Choice.Planes]).Restrict(0, 1UL << B);
        Assert.NotNull(trial.Engine.Settle(64));

        Assert.Equal((4, 0, 0), (world.Shop.Stock.Value.Read(view, 0), world.Shop.Stock.Plus.Read(view, 0), world.Shop.OnOrder.Read(view, 0)));
        Assert.Equal([1, 1], world.Shop.Lines.Select(line => world.Wholesalers[0].Cases.Read(trial.Of(line), 0)!.Value));
        Assert.Equal(before, Transport.SaveLocals(world.Engines));
        Assert.False(trial.Commit());

        world.Settle();
        Assert.Equal([0, 2], world.Warehouses);
    }
}
