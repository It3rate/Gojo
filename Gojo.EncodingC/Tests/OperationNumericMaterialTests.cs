using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class OperationNumericMaterialTests
{
    private const int PressureCapacity = 1;
    private const int RungCapacity = 15;
    private static readonly Plane Here = Plane.Of(1, 0);

    [Fact]
    public void AdmittedUnitsCarryAndTheSixteenthWaitsAtTheSource()
    {
        var fixture = Arrange();
        Assert.True(fixture.SourceOutlet.IsTaken);
        Assert.True(Assert.Single(fixture.Receiver.Step().Steps).Quiet);

        for (var offered = 1; offered <= RungCapacity + 1; offered++)
        {
            Assert.Equal(Here, fixture.Incoming.Receive(Here).Taken);

            var admission = Assert.Single(fixture.Receiver.Step().Steps);
            var received = offered <= RungCapacity ? offered : RungCapacity;
            var pending = Read(fixture.Incoming.Plus, fixture.Incoming.Store);
            var coarse = Read(fixture.Coarse, fixture.Rung);
            var fine = Read(fixture.Fine, fixture.Rung);

            Assert.Equal(offered <= RungCapacity, admission.Changed[0]);
            Assert.Equal((received / 4, received % 4), (coarse, fine));
            Assert.Equal(offered - received, pending);
            Assert.Equal(offered, pending + 4 * coarse + fine);
        }

        Assert.Equal(Here, fixture.Incoming.Receive(Here).Blocked);
        Assert.Equal(0, Read(fixture.Incoming.Value, fixture.Incoming.Store));
        Assert.Equal(1, fixture.Receiver.Settle(3));

        var publication = fixture.Rung.Publish([0], [.. fixture.Coarse.Planes, .. fixture.Fine.Planes]);
        var observerStore = new Store(0, 1);
        var coarseRead = ObserveDigit(observerStore, new Binding(publication, 0, 1));
        var fineRead = ObserveDigit(observerStore, new Binding(publication, 2, 3));
        var observer = new Engine().Add(coarseRead).Add(fineRead);
        observer.Step();

        Assert.Equal(3, coarseRead.Value(0, 1));
        Assert.Equal(3, fineRead.Value(0, 1));
        Assert.False(coarseRead.Writes(0));
        Assert.False(fineRead.Writes(0));
        Assert.False(publication.IsTaken);
        Assert.Equal((3, 3), (Read(fixture.Coarse, fixture.Rung), Read(fixture.Fine, fixture.Rung)));
        Assert.Equal(1, Read(fixture.Incoming.Plus, fixture.Incoming.Store));
    }

    [Fact]
    public void SavedLocalsRetainPendingPressureAndContinueTheSameCarry()
    {
        var fixture = Arrange();
        for (var unit = 0; unit < 3; unit++)
        {
            Assert.Equal(Here, fixture.Incoming.Receive(Here).Taken);
            Assert.Equal(Here, Assert.Single(fixture.Receiver.Step().Steps).Changed);
        }
        Assert.Equal(Here, fixture.Incoming.Receive(Here).Taken);

        var saved = Transport.SaveLocals(fixture.Source, fixture.Receiver);
        var restored = Transport.LoadLocals(saved);
        var source = restored[0].Engine;
        var receiver = restored[1].Engine;
        var incomingPort = Assert.Single(receiver.Sockets).Plugged;
        Assert.NotNull(incomingPort);
        Assert.Same(source, incomingPort.Owner.Local);
        Assert.NotSame(fixture.Incoming.Store, incomingPort.Owner);
        var rung = Assert.IsType<Store>(Assert.Single(receiver.Wirings).Bindings[1].Surface);

        Assert.Equal(saved, Transport.SaveLocals(source, receiver));
        Assert.Equal(1, Read(fixture.Incoming.Plus, incomingPort.Owner));
        Assert.Equal((0, 3), (Read(fixture.Coarse, rung), Read(fixture.Fine, rung)));

        Assert.Equal(Here, Assert.Single(fixture.Receiver.Step().Steps).Changed);
        Assert.Equal(Here, Assert.Single(receiver.Step().Steps).Changed);

        Assert.Equal((1, 0), (Read(fixture.Coarse, rung), Read(fixture.Fine, rung)));
        Assert.Equal(0, Read(fixture.Incoming.Plus, incomingPort.Owner));
        Assert.Equal(Transport.SaveLocals(fixture.Source, fixture.Receiver), Transport.SaveLocals(source, receiver));
    }

    private sealed record Admission(Engine Source, Tension Incoming, Port SourceOutlet,
        Engine Receiver, Store Rung, Field Coarse, Field Fine);

    private static Admission Arrange()
    {
        var sourceStore = new Store(0, 1);
        var value = Allocate(sourceStore, Shape.Unary(PressureCapacity));
        var incoming = new Tension(sourceStore, value, PressureCapacity);
        var source = new Engine().Hold(sourceStore);
        var outlet = sourceStore.Publish([0], [.. incoming.Plus.Planes]);

        var rung = new Store(0, 1);
        var coarse = Allocate(rung, RadixFour.Digit);
        var fine = Allocate(rung, RadixFour.Digit);
        var offer = new Binding(outlet, [.. Enumerable.Range(0, outlet.PlaneCount)]);
        var admit = Tension.Wire(RadixFour.Admit(PressureCapacity), [offer, coarse.On(rung), fine.On(rung)], Here);
        return new Admission(source, incoming, outlet, new Engine().Add(admit), rung, coarse, fine);
    }

    private static Wiring ObserveDigit(Store reader, Binding sample)
    {
        var support = Supports.On(reader, Allocate(reader, RadixFour.Digit));
        var read = new Wiring(Settling.Same(RadixFour.Digit), sample, support.Variable);
        read.Add(0, 0);
        return read;
    }

    private static Field Allocate(Store store, Shape shape) =>
        new(shape, [.. Enumerable.Range(0, shape.Bits).Select(_ => store.AddPlane())]);

    private static int Read(Field field, Store store) => Assert.IsType<int>(field.Read(store, 0));
}
