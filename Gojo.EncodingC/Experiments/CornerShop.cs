namespace Gojo.EncodingC;

/// <summary>An authored shop, supplier, and customer. The driver posts inputs
/// and schedules the owners. Every transfer and reorder runs in its wiring.</summary>
internal sealed class CornerShop
{
    private const int ShelfCapacity = 2;
    private const int DockCapacity = 1;
    private const int SupplierCapacity = 3;
    private const int RequestCapacity = 2;
    private const int PickupCapacity = 1;

    private const int Have = 0, Want = 1, Deficit = 2, Excess = 3;
    private const int Unloading = 0, Pending = 1;

    private static readonly Field Bit = Field.Bit(0);
    private static readonly Field SupplierCount = Count(SupplierCapacity);
    private static readonly Field RequestCount = Count(RequestCapacity);
    private static readonly Field CollectedCount = Count(SupplierCapacity + DockCapacity);
    private static readonly Field Inventory = Count(ShelfCapacity + DockCapacity);

    private readonly Engine supplier, shop, customer;
    private readonly Store stock, comparison, controls, pickup, requests, collected, pickupPermission;
    private readonly Field shelf, dock, backorders;
    private readonly Socket supply;

    // These are positions on the authored wiring, which transport preserves.
    // The driver can recover inputs and reads from a restored machine as well.
    private CornerShop(Engine supplier, Engine shop, Engine customer)
    {
        this.supplier = supplier;
        this.shop = shop;
        this.customer = customer;

        var accept = shop.Wirings[0];
        var absorb = shop.Wirings[1];
        var deliver = shop.Wirings[2];
        var receive = shop.Wirings[3];
        var measure = shop.Wirings[4];
        var collect = customer.Wirings[0];

        stock = (Store)accept.Bindings[1].Surface;
        comparison = (Store)measure.Bindings[2].Surface;
        controls = (Store)absorb.Bindings[1].Surface;
        pickup = (Store)deliver.Bindings[2].Surface;
        requests = ((Socket)accept.Bindings[0].Surface).Plugged!.Owner;
        collected = (Store)collect.Bindings[2].Surface;
        pickupPermission = (Store)collect.Bindings[1].Surface;
        shelf = new Field(absorb.Law.Roles[2], [.. absorb.Bindings[2].Planes]);
        dock = new Field(absorb.Law.Roles[0], [.. absorb.Bindings[0].Planes]);
        backorders = new Field(accept.Law.Roles[1], [.. accept.Bindings[1].Planes]);
        supply = (Socket)receive.Bindings[0].Surface;
    }

    public static CornerShop Create()
    {
        var stock = new MaterialTension(1, ShelfCapacity, DockCapacity, RequestCapacity);
        stock.Pressure.Write(stock.Sites, 0, 1);
        var pickup = new Store(PickupCapacity, 1);
        var controls = new Store(1, 2);
        var comparison = new Store(Inventory.Shape.Bits, 4);
        Inventory.Write(comparison, Want, ShelfCapacity);

        var offered = new Store(SupplierCapacity, 1);
        SupplierCount.Write(offered, 0, SupplierCapacity);
        var supplier = new Engine().Hold(offered);
        var supply = Connect(offered, SupplierCount);

        var requests = new Store(RequestCapacity, 1);
        RequestCount.Write(requests, 0, 1);
        var request = Connect(requests, RequestCount);
        var collected = new Store(CollectedCount.Shape.Bits, 1);
        var collection = new Store(1, 1);
        var collect = AddRow(new Wiring(Flows.GatedMove(PickupCapacity, CollectedCount.Shape.Size - 1),
            Bind(Connect(pickup, Bit), Bit), Bind(collection, Bit), Bind(collected, CollectedCount)), 0, 0, 0);
        var customer = new Engine().Hold(requests).Add(collect);

        var accept = AddRow(stock.AcceptDemand(Bind(request, RequestCount), RequestCapacity), 0, 0);
        var absorb = AddRow(stock.Absorb(Bind(controls, Bit)), 0, Unloading, 0);
        var deliver = AddRow(stock.Deliver(Bind(pickup, Bit), PickupCapacity), 0, 0, 0);
        var receive = AddRow(new Wiring(Flows.Fulfill(SupplierCapacity, 1, DockCapacity),
            Bind(supply, SupplierCount), Bind(controls, Bit), Bind(stock.Sites, stock.Pressure)), 0, Pending, 0);

        // Inventory evidence includes stock already on the dock. An incoming
        // unit clears its order before this sample is taken, so it counts once.
        var inventoryLaw = TransitionLaw.FromFunction([stock.Value.Shape, stock.Pressure.Shape, Inventory.Shape],
            state => [state[0], state[1], state[0] + state[1]], new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var inventory = Bind(comparison, Inventory);
        var measure = AddRow(new Wiring(inventoryLaw, Bind(stock.Sites, stock.Value), Bind(stock.Sites, stock.Pressure), inventory), 0, 0, Have);
        var compare = AddRow(new Wiring(TensionGap.Track(Inventory.Shape.Size - 1), inventory, inventory, inventory, inventory),
            Have, Want, Deficit, Excess);
        var orderLaw = TransitionLaw.FromFunction([Inventory.Shape, Shape.Bit],
            state => [state[0], state[0] > 0 ? 1 : state[1]], new Accounting(true, new Unchanged(0), new NeverDecreases(1)));
        var order = AddRow(new Wiring(orderLaw, inventory, Bind(controls, Bit)), Deficit, Pending);

        var shop = new Engine().Add(accept).Add(absorb).Add(deliver).Add(receive).Add(measure).Add(compare).Add(order);
        return new CornerShop(supplier, shop, customer);
    }

    public void SetUnloading(bool enabled) => controls.Write(0, Unloading, enabled);

    public void SetCollection(bool enabled) => pickupPermission.Write(0, 0, enabled);

    public void SetTarget(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(amount, ShelfCapacity);
        Inventory.Write(comparison, Want, amount);
    }

    public bool PostRequest() => Unary.Add(requests, RequestCount, requests.Live).Taken.Any;

    public void ConnectSupplier(bool connected)
    {
        if (connected) supply.Plug(supplier.Stores[0].Ports[0]);
        else supply.Unplug();
    }

    /// <summary>Gives each owner one declared step and reports whether it was quiet.</summary>
    public bool Step()
    {
        var pickupStep = customer.Step();
        var supplyStep = supplier.Step();
        var shopStep = shop.Step();
        return pickupStep.Steps.Concat(supplyStep.Steps).Concat(shopStep.Steps).All(step => step.Quiet);
    }

    public CornerShopReading Read() => new(
        Read(SupplierCount, supplier.Stores[0]), Read(RequestCount, requests), Read(dock, stock), Read(shelf, stock),
        Read(backorders, stock), Read(Bit, pickup), Read(CollectedCount, collected), Read(Inventory, comparison, Want),
        Read(Bit, controls, Pending), Read(Inventory, comparison, Deficit), Read(Inventory, comparison, Excess));

    public byte[] Save() => Transport.SaveLocals(supplier, shop, customer);

    public CornerShop Restore(byte[] saved)
    {
        var owners = Transport.LoadLocals(saved);
        return new CornerShop(owners[0].Engine, owners[1].Engine, owners[2].Engine);
    }

    private static Field Count(int capacity) => new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity)]);

    private static Binding Bind(Surface surface, Field field) => new(surface, [.. field.Planes]);

    private static int Read(Field field, Store store, int site = 0) => field.Read(store, site)
        ?? throw new InvalidOperationException("A shop field does not contain a count.");

    private static Socket Connect(Store store, Field field)
    {
        var port = store.Publish([0], [.. field.Planes]);
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

internal sealed record CornerShopReading(int SupplierStock, int WaitingRequests, int Dock, int Shelf, int Backorders,
    int Pickup, int Collected, int Target, int OnOrder, int Deficit, int Excess);
