using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class TensionDeliveryTests
{
    [Fact]
    public void OversizedDeliveryIsRejectedByTheLawTableLimit()
    {
        const int capacity = 63;
        var account = new MaterialTension(1, capacity, 1, capacity);
        var outlet = new Store(capacity, 1);
        var binding = new Binding(outlet, [.. Enumerable.Range(0, capacity)]);

        Assert.Throws<ArgumentOutOfRangeException>("bitCount", () => account.Deliver(binding, capacity));
    }

    [Fact]
    public void ADrawIsDeliveredOnlyWhenTheOutletHasRoom()
    {
        var account = new MaterialTension(sites: 1, valueCapacity: 3, pressureCapacity: 1, demandCapacity: 2);
        account.Value.Write(account.Sites, 0, 3);
        account.Demand.Write(account.Sites, 0, 2);

        // An earlier delivery still occupies the outlet.
        var outlet = new Store(1, 1);
        outlet.Write(0, 0, true);
        var fulfill = account.Deliver(new Binding(outlet, 0), outletCapacity: 1);
        fulfill.Add(0, 0, 0);
        var supplier = new Engine().Add(fulfill);

        var port = outlet.Publish([0], [0]);
        var socket = Socket.For(port);
        socket.Plug(port);
        var delivered = new Store(3, 1);
        var receipt = new Field(Shape.Unary(3), 0, 1, 2);
        var take = new Wiring(Flows.Move(1, 3), new Binding(socket, 0), new Binding(delivered, 0, 1, 2));
        take.Add(0, 0);
        var receiver = new Engine().Add(take);

        void State(int value, int demand, int inOutlet, int received)
        {
            Assert.Equal(value, account.Value.Read(account.Sites, 0));
            Assert.Equal(demand, account.Demand.Read(account.Sites, 0));
            Assert.Equal(inOutlet, Field.Bit(0).Read(outlet, 0));
            Assert.Equal(received, receipt.Read(delivered, 0));
            Assert.Equal(4, value + inOutlet + received);
            Assert.Equal(3, demand + inOutlet + received);
        }

        Assert.True(Assert.Single(supplier.Step().Steps).Changed.None);
        State(3, 2, 1, 0);
        receiver.Step();
        State(3, 2, 0, 1);

        supplier.Step();
        State(2, 1, 1, 1);
        Assert.True(Assert.Single(supplier.Step().Steps).Changed.None);
        State(2, 1, 1, 1);

        receiver.Step();
        supplier.Step();
        State(1, 0, 1, 2);
        receiver.Step();
        Assert.True(Assert.Single(supplier.Step().Steps).Changed.None);
        State(1, 0, 0, 3);
    }
}
