namespace Gojo.EncodingC;

/// <summary>An authored shop with two suppliers and one customer. Operator
/// inputs and the owner schedule come from the driver. Ordinary wiring makes
/// the booking, receipt, and fulfillment decisions.</summary>
internal sealed class CornerShop
{
    private const int ShelfCapacity = 2, DockCapacity = 2, CaseCapacity = 2, RequestCapacity = 2;
    private const int UnitsPerCase = 2, InitiallyAtDock = 1;
    private const int Have = 0, Want = 1, Deficit = 2, Excess = 3;
    private const int Unloading = 0, Rebooking = 1;

    private enum ShopRow
    {
        Accept, Absorb, Deliver, ReceiveA, ReceiveB, QualifyA, QualifyB, Cancel,
        CountHave, CountWant, Compare, RequestChoice, CountChoices, DemandAnswer, Book
    }

    private static readonly Field Bit = Field.Bit(0);
    private static readonly Field Cases = Count(CaseCapacity);
    private static readonly Field PublishedAvailability = Field.Bit(CaseCapacity);
    private static readonly Field Eligible = Field.Bit(0);
    private static readonly Field Booked = Field.Bit(1);
    private static readonly Field Receipts = Count(CaseCapacity, 2);
    private static readonly Field RequestCount = Count(RequestCapacity);
    private static readonly Field CollectedCount = Count(2 * CaseCapacity * UnitsPerCase + InitiallyAtDock);
    private static readonly Field TargetCount = Count(ShelfCapacity);
    private static readonly Field Inventory = Count(ShelfCapacity + DockCapacity);
    private static readonly Field NeedChoice = Field.Bit(0);
    private static readonly Field SeenOne = Field.Bit(1);
    private static readonly Field SeenTwo = Field.Bit(2);
    private static readonly Field ChoiceOpen = Field.Bit(3);
    private static readonly Field ChoiceContradiction = Field.Bit(4);

    private readonly Engine[] suppliers;
    private readonly Socket[] supply;
    private readonly Engine shop, customer;
    private readonly Store stock, comparison, target, controls, routes, status, pickup, requests, collected, collection;
    private readonly Field shelf, dock, backorders;

    // These are positions in the authored wiring preserved by transport. The
    // driver uses them to find manual inputs and readings on a restored shop.
    private CornerShop(Engine a, Engine b, Engine shop, Engine customer)
    {
        suppliers = [a, b];
        this.shop = shop;
        this.customer = customer;

        var accept = Row(ShopRow.Accept);
        var absorb = Row(ShopRow.Absorb);
        var deliver = Row(ShopRow.Deliver);
        var collect = customer.Wirings[0];
        stock = (Store)accept.Bindings[1].Surface;
        controls = (Store)absorb.Bindings[1].Surface;
        pickup = (Store)deliver.Bindings[2].Surface;
        routes = (Store)Row(ShopRow.Book).Bindings[1].Surface;
        status = (Store)Row(ShopRow.DemandAnswer).Bindings[0].Surface;
        comparison = (Store)Row(ShopRow.CountHave).Bindings[2].Surface;
        target = (Store)Row(ShopRow.CountWant).Bindings[0].Surface;
        requests = ((Socket)accept.Bindings[0].Surface).Plugged!.Owner;
        collection = (Store)collect.Bindings[1].Surface;
        collected = (Store)collect.Bindings[2].Surface;

        shelf = FieldAt(absorb, 2);
        dock = FieldAt(absorb, 0);
        backorders = FieldAt(accept, 1);
        supply = [(Socket)Row(ShopRow.ReceiveA).Bindings[0].Surface, (Socket)Row(ShopRow.ReceiveB).Bindings[0].Surface];

        Wiring Row(ShopRow row) => shop.Wirings[(int)row];
    }

    public static CornerShop Create(int casesA = CaseCapacity, int casesB = CaseCapacity)
    {
        var stock = new MaterialTension(1, ShelfCapacity, DockCapacity, RequestCapacity);
        stock.Pressure.Write(stock.Sites, 0, InitiallyAtDock);
        var pickup = new Store(1, 1);
        var controls = new Store(1, 2);
        var routes = new Store(2 + Receipts.Shape.Bits, 2);
        var status = new Store(5, 1);
        var comparison = new Store(Inventory.Shape.Bits, 4);
        var target = new Store(TargetCount.Shape.Bits, 1);
        TargetCount.Write(target, 0, ShelfCapacity);

        var a = BuildSupplier(casesA, available: true);
        var b = BuildSupplier(casesB, available: false);
        var requests = new Store(RequestCapacity, 1);
        RequestCount.Write(requests, 0, 1);
        var request = Connect(requests, [.. RequestCount.Planes]);
        var collected = new Store(CollectedCount.Shape.Bits, 1);
        var collection = new Store(1, 1);
        var collect = AddRow(new Wiring(Flows.GatedMove(1, Capacity(CollectedCount)),
            Bind(Connect(pickup, 0), Bit), Bind(collection, Bit), Bind(collected, CollectedCount)), 0, 0, 0);
        var customer = new Engine().Hold(requests).Add(collect);

        var accept = AddRow(stock.AcceptDemand(Bind(request, RequestCount), RequestCapacity), 0, 0);
        var absorb = AddRow(stock.Absorb(Bind(controls, Bit)), 0, Unloading, 0);
        var deliver = AddRow(stock.Deliver(Bind(pickup, Bit), 1), 0, 0, 0);

        var eligible = Bind(routes, Eligible);
        var booked = Bind(routes, Booked);
        Wiring Qualify(Socket published, Supplier supplier) => AddRow(new Wiring(Settling.Justify(2),
            new Binding(published, 0), new Binding(published, 1), eligible), 0, 0, (int)supplier);
        var qualifyA = Qualify(a.Availability, Supplier.A);
        var qualifyB = Qualify(b.Availability, Supplier.B);
        var cancelLaw = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit, Shape.Bit],
            state => [state[0], state[1], state[0] == 1 && state[1] == 0 ? 0 : state[2]],
            new Accounting(true, new Unchanged(0), new Unchanged(1), new NeverIncreases(2)));
        var cancel = new Wiring(cancelLaw, Bind(controls, Bit), eligible, booked);
        cancel.Add(Rebooking, (int)Supplier.A, (int)Supplier.A);
        cancel.Add(Rebooking, (int)Supplier.B, (int)Supplier.B);

        // The receipt reads willingness at the supplier and checks the case
        // itself. Qualify new orders after receiving, when the last case might
        // just have left. A dock needs room for the whole case.
        var receiptLaw = Flows.When(Flows.FulfillBatch(CaseCapacity, 1, DockCapacity, UnitsPerCase, Capacity(Receipts)));
        Wiring Receive(Socket source, Socket published, Supplier supplier) => AddRow(new Wiring(receiptLaw,
            Bind(source, Cases), booked, Bind(stock.Sites, stock.Pressure), Bind(routes, Receipts), new Binding(published, 0)),
            0, (int)supplier, 0, (int)supplier, 0);
        var receiveA = Receive(a.Stock, a.Availability, Supplier.A);
        var receiveB = Receive(b.Stock, b.Availability, Supplier.B);

        // The two sums are exact in this fixture: each operand is at most two.
        var sum = TransitionLaw.FromFunction([TargetCount.Shape, TargetCount.Shape, Inventory.Shape],
            state => [state[0], state[1], state[0] + state[1]], new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var inventory = Bind(comparison, Inventory);
        var countHave = AddRow(new Wiring(sum, Bind(stock.Sites, stock.Value), Bind(stock.Sites, stock.Pressure), inventory), 0, 0, Have);
        var countWant = AddRow(new Wiring(sum, Bind(target, TargetCount), Bind(stock.Sites, stock.Demand), inventory), 0, 0, Want);
        var compare = AddRow(new Wiring(TensionGap.Track(Capacity(Inventory)), inventory, inventory, inventory, inventory),
            Have, Want, Deficit, Excess);

        var requestChoiceLaw = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
            state => [state[0], state[1], state[2], state[0] & (1 - state[1]) & (1 - state[2])],
            new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2)));
        var need = Bind(status, NeedChoice);
        var requestChoice = AddRow(new Wiring(requestChoiceLaw, new Binding(comparison, Inventory.Planes[0]), booked, booked, need),
            Deficit, (int)Supplier.A, (int)Supplier.B, 0);
        var seenOne = Bind(status, SeenOne);
        var seenTwo = Bind(status, SeenTwo);
        var countChoices = AddRow(new Wiring(InformationTension.SupportPair, eligible, eligible, seenOne, seenTwo),
            (int)Supplier.A, (int)Supplier.B, 0, 0);
        var demandAnswer = AddRow(new Wiring(InformationTension.DemandedStatus,
            need, seenOne, seenTwo, Bind(status, ChoiceOpen), Bind(status, ChoiceContradiction)), 0, 0, 0, 0, 0);

        var bookLaw = TransitionLaw.FromFunction([Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit, Shape.Bit],
            state => state[0] == 1 && state[1] != state[2] && state[3] == 0 && state[4] == 0
                ? [state[0], state[1], state[2], state[1], state[2]]
                : state,
            new Accounting(true, new Unchanged(0), new Unchanged(1), new Unchanged(2)));
        var book = AddRow(new Wiring(bookLaw, need, eligible, eligible, booked, booked),
            0, (int)Supplier.A, (int)Supplier.B, (int)Supplier.A, (int)Supplier.B);

        var shop = new Engine().Add(accept).Add(absorb).Add(deliver).Add(receiveA).Add(receiveB)
            .Add(qualifyA).Add(qualifyB).Add(cancel).Add(countHave).Add(countWant).Add(compare).Add(requestChoice).Add(countChoices)
            .Add(demandAnswer).Add(book);
        return new CornerShop(a.Engine, b.Engine, shop, customer);
    }

    public void SetUnloading(bool enabled) => controls.Write(0, Unloading, enabled);

    public void SetRebooking(bool enabled) => controls.Write(0, Rebooking, enabled);

    public void SetCollection(bool enabled) => collection.Write(0, 0, enabled);

    public void SetTarget(int amount) => TargetCount.Write(target, 0, amount);

    public bool PostRequest() => Unary.Add(requests, RequestCount, requests.Live).Taken.Any;

    public void SetAvailability(Supplier supplier, bool available) =>
        suppliers[(int)supplier].Stores[0].Write(PublishedAvailability.Planes[0], 0, available);

    public void ConnectSupplier(Supplier supplier, bool connected)
    {
        var socket = supply[(int)supplier];
        if (connected) socket.Plug(suppliers[(int)supplier].Stores[0].Ports[0]);
        else socket.Unplug();
    }

    /// <summary>Gives each owner one declared step and reports whether it was quiet.</summary>
    public bool Step()
    {
        var pickupStep = customer.Step();
        var a = suppliers[(int)Supplier.A].Step();
        var b = suppliers[(int)Supplier.B].Step();
        var shopStep = shop.Step();
        return pickupStep.Steps.Concat(a.Steps).Concat(b.Steps).Concat(shopStep.Steps).All(step => step.Quiet);
    }

    public CornerShopReading Read() => new(
        ReadSupplier(Supplier.A), ReadSupplier(Supplier.B), Read(RequestCount, requests), Read(dock, stock), Read(shelf, stock),
        Read(backorders, stock), Read(Bit, pickup), Read(CollectedCount, collected), Read(TargetCount, target),
        Read(Inventory, comparison, Deficit), Read(Inventory, comparison, Excess), Read(ChoiceOpen, status) == 1,
        Read(ChoiceContradiction, status) == 1);

    public byte[] Save() => Transport.SaveLocals(suppliers[0], suppliers[1], shop, customer);

    public CornerShop Restore(byte[] saved)
    {
        var owners = Transport.LoadLocals(saved);
        return new CornerShop(owners[0].Engine, owners[1].Engine, owners[2].Engine, owners[3].Engine);
    }

    private SupplierReading ReadSupplier(Supplier supplier)
    {
        var index = (int)supplier;
        return new SupplierReading(Read(Cases, suppliers[index].Stores[0]), Read(Eligible, routes, index) == 1,
            Read(Booked, routes, index) == 1, Read(Receipts, routes, index));
    }

    private static (Engine Engine, Socket Stock, Socket Availability) BuildSupplier(int cases, bool available)
    {
        var store = new Store(CaseCapacity + 1, 1);
        Cases.Write(store, 0, cases);
        PublishedAvailability.Write(store, 0, available ? 1 : 0);
        // The lowest unary bit witnesses at least one offered case. Keep the
        // read port live even when the connection that can take stock is out.
        return (new Engine().Hold(store), Connect(store, [.. Cases.Planes]),
            Connect(store, PublishedAvailability.Planes[0], Cases.Planes[0]));
    }

    private static Field Count(int capacity, int start = 0) => new(Shape.Unary(capacity), [.. Enumerable.Range(start, capacity)]);

    private static int Capacity(Field field) => field.Shape.Size - 1;

    private static Binding Bind(Surface surface, Field field) => new(surface, [.. field.Planes]);

    private static Field FieldAt(Wiring wiring, int role) => new(wiring.Law.Roles[role], [.. wiring.Bindings[role].Planes]);

    private static int Read(Field field, Store store, int site = 0) => field.Read(store, site)
        ?? throw new InvalidOperationException("A shop field does not contain a count.");

    private static Socket Connect(Store store, params int[] planes)
    {
        var port = store.Publish([0], planes);
        var socket = Socket.For(port);
        socket.Plug(port);
        return socket;
    }

    private static Wiring AddRow(Wiring wiring, params int[] positions)
    {
        wiring.Add(positions);
        return wiring;
    }
}

internal enum Supplier { A, B }

internal sealed record SupplierReading(int Cases, bool Eligible, bool Booked, int Receipts);

internal sealed record CornerShopReading(SupplierReading A, SupplierReading B, int WaitingRequests, int Dock, int Shelf,
    int Backorders, int Pickup, int Collected, int Target, int Deficit, int Excess, bool ChoiceOpen, bool ChoiceContradiction);
