using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A corner shop that sells one product, as a test of tension in a
/// real setting.
/// <para><b>The shop</b> is one local structure. Its shelf holds up to four
/// items. Beside the shelf, as material tension:
/// <list type="bullet">
/// <item>pressure is the receiving dock: items delivered and not yet shelved;</item>
/// <item>debt is backorders: items customers ordered that have not been handed over.</item>
/// </list>
/// Customers' orders enter as debt, never by taking from the shelf directly,
/// and the shop's own laws pay them from the shelf, first, then shelve what
/// waits at the dock.</para>
/// <para><b>Reordering</b> reads the two-faced gap between what the shop has,
/// shelf plus dock, and what it wants, a target plus what it owes. While it is
/// short, it takes a case through a supplier's port. A case is two items, so
/// the move converts at the boundary; the shop counts items only.</para>
/// <para><b>Suppliers</b> are local structures of their own, each a warehouse of
/// cases behind a port. With two, which one to order from is an open choice,
/// one support plane per supplier, and each order route waits on both "short"
/// and "this supplier is still possible".</para>
/// <para><b>The books</b> keep which supplier each batch came from, as an open
/// value narrowed by delivery notes, and pay a batch's invoice only once that
/// is settled: a question whose answer, or lack of one, gates a move.</para></summary>
public sealed class ShopTests
{
    private const int ShelfRoom = 4, Room = 4, Cases = 3, PerCase = 2;
    private const int A = 0, B = 1;
    private static readonly Plane Here = Plane.Of(1, 0);

    private sealed record Supplier(Engine Engine, Store Warehouse, Field Stock, Port Out, Port Status);

    /// <summary>A wholesaler: a warehouse of up to three cases behind a port,
    /// and whether it is open for business, which anyone may read through a
    /// second port.</summary>
    private static Supplier Wholesaler(int cases, bool open = true)
    {
        var warehouse = new Store(Cases + 1, 1);
        var stock = new Field(Shape.Unary(Cases), 0, 1, 2);
        stock.Write(warehouse, 0, cases);
        warehouse.Write(Cases, 0, open);
        return new Supplier(new Engine().Hold(warehouse), warehouse, stock, warehouse.Publish([0], [0, 1, 2]), warehouse.Publish([0], [Cases]));
    }

    private sealed record Shop(Engine Engine, Store Store, Tension Stock, Difference Gap, Supports? Choice, Wiring[] Orders);

    /// <summary>The shop, with its shelf, dock and backorders, a target, and an
    /// order route per supplier. With <paramref name="countDock"/> false the
    /// gap counts only the shelf, as a naive reorder rule would.</summary>
    private static Shop Open(int target, int shelf = 0, Supplier[]? suppliers = null, bool countDock = true,
        Func<Store, Supports?, IEnumerable<Wiring>>? first = null)
    {
        suppliers ??= [];
        var store = new Store(ShelfRoom, 1);
        var onShelf = new Field(Shape.Unary(ShelfRoom), 0, 1, 2, 3);
        onShelf.Write(store, 0, shelf);
        var stock = new Tension(store, onShelf, Room);
        var wanted = new Field(Shape.Unary(ShelfRoom), [.. Enumerable.Range(0, ShelfRoom).Select(_ => store.AddPlane())]);
        wanted.Write(store, 0, target);
        (Surface, Field)[] have = countDock ? [(store, onShelf), (store, stock.Plus)] : [(store, onShelf)];
        var gap = new Difference(have, [(store, wanted), (store, stock.Minus)]);
        var short_ = new Binding(gap, gap.Deficit.Planes[0]);
        var choice = suppliers.Length > 1 ? Supports.On(store, new Field(Shape.Binary(suppliers.Length), store.AddPlane())) : null;
        var orders = suppliers.Select((s, i) => Tension.Route(
            Flows.Convert(1, PerCase, Cases, Room), s.Stock.On(s.Out), stock.Plus.On(store), Here,
            choice is null ? [short_] : [short_, choice.Possible(i)])).ToArray();
        var engine = new Engine();
        foreach (var wiring in first?.Invoke(store, choice) ?? []) engine.Add(wiring);
        engine.Add(stock.Draw(Here)).Add(stock.Absorb(Here));
        foreach (var order in orders) engine.Add(order);
        return new Shop(engine, store, stock, gap, choice, orders);
    }

    private static (int?, int?, int?) Floor(Shop shop) =>
        (shop.Stock.Value.Read(shop.Store, 0), shop.Stock.Plus.Read(shop.Store, 0), shop.Stock.Minus.Read(shop.Store, 0));

    private static void Sell(Shop shop, int items)
    {
        for (var i = 0; i < items; i++) Assert.True(shop.Stock.Demand(Here).Blocked.None);
    }

    private static void Settle(Shop shop) => Assert.NotNull(shop.Engine.Settle(64));

    /// <summary>Iteration 1, material only. Three items delivered to a shelf
    /// holding two wait at the dock and are shelved while there is room, the
    /// last staying at the dock. Customers order three: they are served from
    /// the shelf, and the dock's item takes the room freed. Four more: the two
    /// on the shelf are handed over and two stay owed, visible. An order the
    /// books have no room to record is refused, not lost.</summary>
    [Fact]
    public void ShelfDockAndBackordersSettle()
    {
        var shop = Open(target: 0, shelf: 2);
        for (var i = 0; i < 3; i++) Assert.True(shop.Stock.Receive(Here).Blocked.None);

        Settle(shop);
        Assert.Equal((4, 1, 0), Floor(shop));

        Sell(shop, 3);
        Settle(shop);
        Assert.Equal((2, 0, 0), Floor(shop));

        Sell(shop, 4);
        Settle(shop);
        Assert.Equal((0, 0, 2), Floor(shop));

        Sell(shop, 2);
        Assert.True(shop.Stock.Demand(Here).Blocked[0]);
    }

    /// <summary>Iteration 2, reordering through a port. The shop wants three on
    /// hand and has none. It orders a case at a time while shelf plus dock is
    /// short of target plus backorders, and stops at four, the nearest whole
    /// cases, with one case left at the wholesaler. A rule that counts only the
    /// shelf keeps ordering while items wait at the dock and empties the
    /// wholesaler. Every item is accounted for across both structures.</summary>
    [Fact]
    public void ReorderingCountsWhatIsAlreadyAtTheDock()
    {
        var wholesaler = Wholesaler(cases: 3);
        var shop = Open(target: 3, suppliers: [wholesaler]);
        Settle(shop);

        Assert.Equal((4, 0, 0), Floor(shop));
        Assert.Equal(1, wholesaler.Stock.Read(wholesaler.Warehouse, 0));

        var naiveWholesaler = Wholesaler(cases: 3);
        var naive = Open(target: 3, suppliers: [naiveWholesaler], countDock: false);
        Settle(naive);

        Assert.Equal((4, 2, 0), Floor(naive));
        Assert.Equal(0, naiveWholesaler.Stock.Read(naiveWholesaler.Warehouse, 0));
    }

    /// <summary>When the wholesaler runs dry, the shop settles with its tension
    /// showing: after selling three and then four, one item is owed, and the
    /// gap says the shop is four short of target plus backorders. The quiet
    /// state is a result, and where the tension sits is part of it.</summary>
    [Fact]
    public void ADryWholesalerLeavesBackordersAndAShortGapVisible()
    {
        var wholesaler = Wholesaler(cases: 3);
        var shop = Open(target: 3, suppliers: [wholesaler]);
        Settle(shop);
        Sell(shop, 3);
        Settle(shop);
        Assert.Equal((3, 0, 0), Floor(shop));

        Sell(shop, 4);
        Settle(shop);

        Assert.Equal((0, 0, 1), Floor(shop));
        Assert.Equal(0, wholesaler.Stock.Read(wholesaler.Warehouse, 0));
        Assert.Equal(4, shop.Gap.Deficit.Read(shop.Gap, 0));
    }

    /// <summary>Iteration 3, two wholesalers. Both have stock, so ordering is an
    /// open choice, and taken together the two order routes are contested: the
    /// contest is the open choice. A is closed today, which the shop reads
    /// through A's status port, and a law makes A impossible, so from then on
    /// only B delivers. Later A opens again, and the shop sells eight more.
    /// <para><b>Narrowed,</b> A stays ruled out: narrowing only rules out. B runs
    /// dry and the shop settles with two items owed while A has stock and is
    /// open. <b>Justified,</b> "A is possible" is held only while A is open, by
    /// one law that writes it from that reason each step. It reopens with A,
    /// both wholesalers deliver, and nothing is owed.</para></summary>
    [Fact]
    public void AClosedWholesalerIsOrderedFromAgainOnlyWhenItsPossibilityIsJustified()
    {
        (Shop Shop, Supplier A, Supplier B) Run(bool justified)
        {
            var (a, b) = (Wholesaler(cases: 3, open: false), Wholesaler(cases: 3));
            var shop = Open(target: 3, suppliers: [a, b], first: (store, choice) =>
            {
                var rule = justified
                    ? new Wiring(Settling.Justify(1), new Binding(a.Status, 0), choice!.Possible(A))
                    : new Wiring(RelationLaw.FromPredicate([Shape.Bit, Shape.Binary(2)], t => t[0] == 1 || t[1] != A), new Binding(a.Status, 0), choice!.Variable);
                rule.Add(0, 0);
                return [rule];
            });

            var first = shop.Engine.StepTogether();
            Assert.All(shop.Orders, order => Assert.True(first.Steps[shop.Engine.Wirings.ToList().IndexOf(order)].Contested![0]));
            Assert.Equal(1UL << B, shop.Choice!.Values(0));
            Settle(shop);
            Assert.Equal((4, 0, 0), Floor(shop));
            Assert.Equal((3, 1), (a.Stock.Read(a.Warehouse, 0), b.Stock.Read(b.Warehouse, 0)));

            a.Warehouse.Write(Cases, 0, true);
            for (var round = 0; round < 2; round++)
            {
                Sell(shop, 4);
                Settle(shop);
            }
            return (shop, a, b);
        }

        var narrowed = Run(justified: false);
        Assert.Equal((0, 0, 2), Floor(narrowed.Shop));
        Assert.Equal((3, 0), (narrowed.A.Stock.Read(narrowed.A.Warehouse, 0), narrowed.B.Stock.Read(narrowed.B.Warehouse, 0)));
        Assert.Equal(1UL << B, narrowed.Shop.Choice!.Values(0));

        var justified = Run(justified: true);
        Assert.Equal((4, 0, 0), Floor(justified.Shop));
        Assert.Equal((0, 0), (justified.A.Stock.Read(justified.A.Warehouse, 0), justified.B.Stock.Read(justified.B.Warehouse, 0)));
        Assert.Equal(0b11UL, justified.Shop.Choice!.Values(0));
    }

    /// <summary>Iteration 4, the books. Three batches, and which wholesaler each
    /// came from is an open value, narrowed by delivery notes: batch 0 has one
    /// note, from B; batch 1 has two that disagree; batch 2 has none. A batch's
    /// invoice is paid to a supplier only once the batch's supplier is settled:
    /// the payment waits on that supplier still being possible, on "not open"
    /// and on "not contradicted", the last two read through flipped views of
    /// the batch's openness. So batch 0 is paid to B, batch 1 is held as a
    /// contradiction and batch 2 as unanswered, each invoice still owed.
    /// "Which supplier did batch 1 come from?" selects only the notes about
    /// batch 1. The books fork, views and all, and the fork settles to the same; saved and loaded, they save to the same bits.</summary>
    [Fact]
    public void TheBooksPayABatchOnlyOnceItsSupplierIsSettled()
    {
        const int Batches = 3;
        var books = new Store(0, Batches);
        var from = Supports.On(books, new Field(Shape.Binary(2), books.AddPlane()));
        var owed = new Field(Shape.Unary(1), books.AddPlane());
        Field[] paid = [new(Shape.Unary(1), books.AddPlane()), new(Shape.Unary(1), books.AddPlane())];
        for (var batch = 0; batch < Batches; batch++) owed.Write(books, batch, 1);
        (int Batch, int Supplier)[] said = [(0, B), (1, A), (1, B)];
        var notes = new Store(1, said.Length);
        var supplier = new Field(Shape.Binary(2), 0);
        var reading = new Wiring(Settling.Same(Shape.Binary(2)), supplier.On(notes), from.Variable);
        for (var n = 0; n < said.Length; n++)
        {
            supplier.Write(notes, n, said[n].Supplier);
            reading.Add(n, said[n].Batch);
        }
        var open = from.Openness;
        Binding Not(int plane) => new(new Flipped(open, plane), plane);
        var all = Plane.Ones(Batches);
        var engine = new Engine().Add(reading);
        foreach (var s in new[] { A, B })
            engine.Add(Tension.Route(owed.On(books), paid[s].On(books), all, from.Possible(s), Not(open.Deficit.Planes[0]), Not(open.Excess.Planes[0])));

        Assert.NotNull(engine.Settle(16));

        Assert.Equal([1, 0, 2], Enumerable.Range(0, Batches).Select(batch => new Question(from, batch).Remaining));
        Assert.Equal([0, 1, 1], Enumerable.Range(0, Batches).Select(batch => owed.Read(books, batch)));
        Assert.Equal([1, 0, 0], Enumerable.Range(0, Batches).Select(batch => paid[B].Read(books, batch)));
        Assert.All(Enumerable.Range(0, Batches), batch => Assert.Equal(0, paid[A].Read(books, batch)));
        Assert.Equal((0, true), (open.Deficit.Read(open, 1), open.Excess.Pattern(open, 1) == 1));
        Assert.Equal(1, open.Deficit.Read(open, 2));
        Assert.Equal(Plane.Of(3, 1, 2), new Question(from, 1).Cone(engine).Rows(reading));

        var fork = engine.Fork();
        Assert.NotNull(fork.Engine.Settle(16));
        Assert.Equal(1, owed.Read(fork.Of(books), 1));

        var saved = Transport.Save(engine);
        Assert.Equal(saved, Transport.Save(Transport.Load(saved).Engine));
    }

    /// <summary>Iteration 5, trying "buy only from B" without touching the shop.
    /// A fork is refused, because the shop takes through its wholesalers'
    /// ports and a fork would act on them. Instead the shop and both
    /// wholesalers are saved together and loaded as a copy, gap and all, and
    /// the copy is run with A ruled out. In the shop, A, declared first, gives
    /// its one case; in the trial B gives both. The shop and its wholesalers
    /// are unchanged by the trial, bit for bit.</summary>
    [Fact]
    public void ATrialRunsOnACopyBecauseAShopThatTakesCannotFork()
    {
        var (a, b) = (Wholesaler(cases: 1), Wholesaler(cases: 3));
        var shop = Open(target: 3, suppliers: [a, b]);
        Assert.Throws<ArgumentException>(() => shop.Engine.Fork());

        var before = Transport.SaveLocals(shop.Engine, a.Engine, b.Engine);
        var copies = Transport.LoadLocals(before);
        var trialStore = copies[0].Stores.First(store => store.PlaneCount == shop.Store.PlaneCount);
        new Supports(trialStore, shop.Choice!.Field, [.. shop.Choice.Planes]).Restrict(0, 1UL << B);
        Assert.NotNull(copies[0].Engine.Settle(64));

        Assert.Equal(before, Transport.SaveLocals(shop.Engine, a.Engine, b.Engine));
        Assert.Equal((4, 0, 0), (shop.Stock.Value.Read(trialStore, 0), shop.Stock.Plus.Read(trialStore, 0), shop.Stock.Minus.Read(trialStore, 0)));
        Assert.Equal((1, 1), (a.Stock.Read(copies[1].Stores[0], 0), b.Stock.Read(copies[2].Stores[0], 0)));

        Settle(shop);
        Assert.Equal((0, 2), (a.Stock.Read(a.Warehouse, 0), b.Stock.Read(b.Warehouse, 0)));
    }
}
