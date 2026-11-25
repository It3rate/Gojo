using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationDeliveryTests
{
    private const int UnitsPerPacket = 3;
    private const int PacketOrders = 4;
    private const int OfferedUnits = UnitsPerPacket * PacketOrders;

    [Theory]
    [InlineData(10, 3, 1, 9, 1)]
    [InlineData(12, 0, 0, 12, 0)]
    public void DeliveringAPacketNeedsRoomAndClosesExactlyOneOrder(
        int dockCapacity, int stockLeft, int ordersLeft, int received, int unusedRoom)
    {
        var here = Plane.Of(1, 0);
        var supplyStore = new Store(0, 1);
        var stock = Count(supplyStore, OfferedUnits);
        var supply = new Tension(supplyStore, stock, OfferedUnits);
        var supplyEngine = new Engine().Add(supply.Absorb(here));
        var supplyPort = supplyStore.Publish([0], [.. stock.Planes]);

        var dockStore = new Store(0, 1);
        var dock = Count(dockStore, dockCapacity);
        var booking = new Tension(dockStore, dock, PacketOrders);
        var opportunity = dockStore.AddPlane();
        var exposedStock = new Binding(supplyPort, [.. Enumerable.Range(0, supplyPort.PlaneCount)]);
        // The debt count is in packet orders. Only this three-unit delivery route pays it.
        var delivery = Tension.Wire(
            Flows.Fulfill(OfferedUnits, PacketOrders, dockCapacity, take: UnitsPerPacket, give: UnitsPerPacket),
            [exposedStock, booking.Minus.On(dockStore), dock.On(dockStore)],
            here, new Binding(dockStore, opportunity));
        var evidence = delivery.Record();
        var dockEngine = new Engine().Add(delivery);

        // Only the capacity-ten case needs the bounded numeric shortfall read.
        var shortfall = dockCapacity == 10 ? Count(dockStore, OfferedUnits) : null;
        if (shortfall is not null)
        {
            var goal = new Field(Shape.Unary(OfferedUnits),
                [.. Enumerable.Range(0, OfferedUnits).Select(_ => dockStore.AddConstantPlane(true))]);
            dockEngine.Add(Tension.Wire(Counts.Shortfall(dockCapacity, OfferedUnits),
                [dock.On(dockStore), goal.On(dockStore), shortfall.On(dockStore)], here));
        }

        for (var unit = 0; unit < OfferedUnits; unit++)
            Assert.Equal(here, supply.Receive(here).Taken);
        Assert.Equal(0, stock.Read(supplyStore, 0));
        Assert.NotNull(supplyEngine.Settle(OfferedUnits + 1));
        Assert.Equal(0, supply.Plus.Read(supplyStore, 0));

        Assert.True(supplyPort.IsTaken);
        dockStore.Write(opportunity, 0, true);
        Assert.True(dockEngine.Step().Steps[0].Changed.None);
        Assert.Equal((12, 0, 0), ReadState());
        if (shortfall is not null) Assert.Equal(12, shortfall.Read(dockStore, 0));

        dockStore.Write(opportunity, 0, false);
        for (var order = 0; order < PacketOrders; order++)
            Assert.Equal(here, booking.Demand(here).Taken);

        Assert.True(dockEngine.Step().Steps[0].Changed.None);
        Assert.Equal((12, 4, 0), ReadState());

        dockStore.Write(opportunity, 0, true);
        foreach (var expected in new[] { (9, 3, 3), (6, 2, 6), (3, 1, 9) })
        {
            Assert.Equal(here, dockEngine.Step().Steps[0].Changed);
            Assert.Equal(expected, ReadState());
        }

        var fourth = dockEngine.Step().Steps[0];
        Assert.Equal(dockCapacity == 12, fourth.Changed[0]);
        Assert.Equal(fourth.Changed[0], delivery.Rows.Read(evidence.Changed, 0));
        Assert.Equal((stockLeft, ordersLeft, received), ReadState());
        var room = dock.Room(dockStore);
        Assert.Equal(unusedRoom, new Field(dock.Shape, [.. room.Planes]).Read(room.Surface, 0));
        if (shortfall is not null) Assert.Equal(3, shortfall.Read(dockStore, 0));

        Assert.NotNull(dockEngine.Settle(3));
        Assert.True(dockEngine.Step().Steps[0].Changed.None);
        Assert.Equal((stockLeft, ordersLeft, received), ReadState());
        Assert.Equal(0, booking.Plus.Read(dockStore, 0));
        if (shortfall is not null) Assert.Equal(3, shortfall.Read(dockStore, 0));

        (int?, int?, int?) ReadState() =>
            (stock.Read(supplyStore, 0), booking.Minus.Read(dockStore, 0), dock.Read(dockStore, 0));
    }

    private static Field Count(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
}
