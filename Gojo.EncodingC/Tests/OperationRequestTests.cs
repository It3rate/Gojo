using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationRequestTests
{
    private const int FactorCapacity = 4;
    private const int ProductCapacity = 16;
    private const int UnitsPerPacket = 3;
    private const int PacketOrders = 4;
    private const int OfferedUnits = 12;

    [Theory]
    [InlineData(3, 10, 9, 12, 0, 0)]
    [InlineData(4, 10, 12, 3, 1, 9)]
    [InlineData(4, 12, 12, 0, 0, 12)]
    public void AQualifiedAndAuthorizedOfferSpendsOneBookingTicket(
        int rightFactor, int dockCapacity, int expectedProduct, int expectedStock, int expectedOrders, int expectedDock)
    {
        var here = Plane.Of(1, 0);
        var (supplyStore, stock) = Make.Counts(1, OfferedUnits);
        stock.Write(supplyStore, 0, OfferedUnits);
        new Engine().Hold(supplyStore);
        var stockPort = supplyStore.Publish([0], [.. stock.Planes]);
        var exposedStock = new Binding(stockPort, [.. Enumerable.Range(0, stockPort.PlaneCount)]);

        var (factorStore, factorField) = Make.Counts(2, FactorCapacity);
        var factors = Supports.On(factorStore, factorField);
        var supplied = Field.Bit(factorStore.AddPlane());
        var observe = new Wiring(Settling.SameWhen(factorField.Shape), supplied.On(factorStore), factors.Sample, factors.Variable);
        observe.Add(0, 0, 0);
        observe.Add(1, 1, 1);

        var local = new Store(0, 1);
        var product = Supports.On(local, Count(local, ProductCapacity));
        var multiplication = BoundedProduct.CreateRelation(factorField.Shape, factorField.Shape, product.Field.Shape);
        var readProduct = new Wiring(multiplication, factors.Variable, factors.Variable, product.Variable);
        readProduct.Add(0, 1, 0);
        var expected = new Field(product.Field.Shape,
            [.. Enumerable.Range(0, ProductCapacity).Select(tick => local.AddConstantPlane(tick < OfferedUnits))]);
        var disagreement = Supports.On(local, Field.Bit(local.AddPlane()));
        var compare = new Wiring(Settling.ValueGap(product.Field.Shape), product.Variable, expected.On(local), disagreement.Variable);
        compare.Add(0, 0, 0);

        var authorized = Field.Bit(local.AddPlane());
        var opportunity = Field.Bit(local.AddPlane());
        var acknowledged = Field.Bit(local.AddPlane());
        var ticket = new Tension(local, Count(local, 1), 1);
        var dock = new Tension(local, Count(local, dockCapacity), PacketOrders);
        var cannotDisagree = new Binding(new Flipped(local, disagreement.Planes[1]), disagreement.Planes[1]);
        var book = Tension.Wire(
            Flows.Convert(take: 1, give: PacketOrders, sourceCapacity: 1, targetCapacity: PacketOrders),
            [ticket.Value.On(local), dock.Minus.On(local)], here,
            authorized.On(local), disagreement.Possible(0), cannotDisagree);
        var booked = book.Record().Changed;
        var acknowledge = Tension.Wire(Settling.Latch(true), [new Binding(book.Rows, booked), acknowledged.On(local)], here);
        var deliver = Tension.Wire(
            Flows.Fulfill(OfferedUnits, PacketOrders, dockCapacity, take: UnitsPerPacket, give: UnitsPerPacket),
            [exposedStock, dock.Minus.On(local), dock.Value.On(local)], here, opportunity.On(local));
        // Each consumer in this ordered circuit reads the upstream law's current-step result.
        var engine = new Engine().Add(observe).Add(readProduct).Add(compare).Add(ticket.Absorb(here))
            .Add(book).Add(acknowledge).Add(deliver);

        // The ticket admits one request for four orders of three units. The grant is a separate, level-held input.
        Assert.True(ticket.Receive(here).Taken[0]);
        SupplyFactor(0, 3);
        authorized.Write(local, 0, 1);
        opportunity.Write(local, 0, 1);
        Assert.NotNull(engine.Settle(6));
        Assert.True(product.Open[0]);
        Assert.True(product.Of(OfferedUnits)[0]);
        Assert.True(disagreement.Open[0]);
        Assert.Equal((1, 0, 0), ReadRequest());
        Assert.Equal((12, 0), ReadDelivery());

        authorized.Write(local, 0, 0);
        SupplyFactor(1, rightFactor);
        Assert.NotNull(engine.Settle(6));
        var qualifies = expectedProduct == OfferedUnits;
        Assert.Equal(1UL << expectedProduct, product.Values(0));
        Assert.Equal(qualifies ? 1UL : 2UL, disagreement.Values(0));
        Assert.Equal((1, 0, 0), ReadRequest());
        Assert.Equal((12, 0), ReadDelivery());

        // Booking can run without a delivery opportunity. Acknowledgement reads the actual booking write.
        opportunity.Write(local, 0, 0);
        authorized.Write(local, 0, 1);
        engine.Step();
        Assert.Equal(qualifies, book.Rows.Read(booked, 0));
        Assert.Equal(qualifies ? (0, 4, 1) : (1, 0, 0), ReadRequest());
        Assert.Equal((12, 0), ReadDelivery());

        opportunity.Write(local, 0, 1);
        for (var step = 0; step < PacketOrders; step++)
        {
            engine.Step();
            Assert.False(book.Rows.Read(booked, 0));
            Assert.Equal(OfferedUnits, stock.Read(supplyStore, 0) + dock.Value.Read(local, 0));
            Assert.Equal(OfferedUnits, OfferedUnits * ticket.Value.Read(local, 0)
                + UnitsPerPacket * dock.Minus.Read(local, 0) + dock.Value.Read(local, 0));
        }

        Assert.NotNull(engine.Settle(6));
        Assert.Equal((expectedStock, expectedDock), ReadDelivery());
        Assert.Equal((qualifies ? 0 : 1, expectedOrders, qualifies ? 1 : 0), ReadRequest());
        Assert.Equal(0, dock.Plus.Read(local, 0));

        authorized.Write(local, 0, 0);
        engine.Step();
        authorized.Write(local, 0, 1);
        Assert.NotNull(engine.Settle(6));
        Assert.Equal((expectedStock, expectedDock), ReadDelivery());
        Assert.Equal((qualifies ? 0 : 1, expectedOrders, qualifies ? 1 : 0), ReadRequest());

        void SupplyFactor(int position, int value)
        {
            factorField.Write(factorStore, position, value);
            supplied.Write(factorStore, position, 1);
        }

        (int?, int?, int?) ReadRequest() =>
            (ticket.Value.Read(local, 0), dock.Minus.Read(local, 0), acknowledged.Read(local, 0));

        (int?, int?) ReadDelivery() => (stock.Read(supplyStore, 0), dock.Value.Read(local, 0));
    }

    private static Field Count(Store store, int capacity) =>
        new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
}
