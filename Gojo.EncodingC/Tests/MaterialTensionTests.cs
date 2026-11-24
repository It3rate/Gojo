using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class MaterialTensionTests
{
    [Fact]
    public void FullTensionLeavesTheOfferWithTheSenderUntilThereIsRoom()
    {
        const int Capacity = 2;
        var (offer, count) = Make.Counts(1, Capacity);
        count.Write(offer, 0, 1);
        var sender = new Engine().Hold(offer);
        var outlet = offer.Publish([0], [.. count.Planes]);
        var inlet = Socket.For(outlet);
        inlet.Plug(outlet);

        var tension = new MaterialTension(1, Capacity, Capacity, Capacity);
        tension.Value.Write(tension.Sites, 0, Capacity);
        tension.Pressure.Write(tension.Sites, 0, Capacity);
        tension.Demand.Write(tension.Sites, 0, 1);
        var gate = new Store(1, 1);
        var accept = tension.AcceptPressure(new Binding(inlet, [.. count.Planes]), Capacity);
        var absorb = tension.Absorb();
        var draw = tension.Draw(new Binding(gate, 0));
        accept.Add(0, 0);
        absorb.Add(0, 0);
        draw.Add(0, 0, 0);
        var receiver = new Engine().Hold(tension.Sites, gate).Add(accept).Add(absorb).Add(draw);

        var blocked = receiver.Step();
        Assert.All(blocked.Steps, step => Assert.True(step.Changed.None));
        Assert.Equal((1, 2, 2, 1), Read(offer, tension.Sites));

        var saved = Transport.SaveLocals(sender, receiver);
        var restored = Transport.LoadLocals(saved);
        var restoredSender = restored[0];
        var restoredReceiver = restored[1];
        Assert.Same(restoredSender.Stores[0].Ports[0], Assert.Single(restoredReceiver.Engine.Sockets).Plugged);
        Assert.Equal(saved, Transport.SaveLocals(restoredSender.Engine, restoredReceiver.Engine));

        Retry(receiver, offer, tension.Sites, gate);
        var restoredSites = Assert.IsType<Store>(restoredReceiver.Engine.Wirings[0].Bindings[1].Surface);
        var restoredGate = Assert.IsType<Store>(restoredReceiver.Engine.Wirings[2].Bindings[1].Surface);
        Retry(restoredReceiver.Engine, Assert.Single(restoredSender.Stores), restoredSites, restoredGate);
        Assert.Equal(Transport.SaveLocals(sender, receiver), Transport.SaveLocals(restoredSender.Engine, restoredReceiver.Engine));

        (int? Offer, int? Value, int? Pressure, int? Demand) Read(Store source, Store target) =>
            (count.Read(source, 0), tension.Value.Read(target, 0), tension.Pressure.Read(target, 0), tension.Demand.Read(target, 0));

        void Retry(Engine engine, Store source, Store target, Store control)
        {
            AssertState((1, 2, 2, 1));
            control.Write(0, 0, true);
            Assert.True(engine.Step().Steps[2].Changed[0]);
            AssertState((1, 1, 2, 0));

            Assert.True(engine.Step().Steps[1].Changed[0]);
            AssertState((1, 2, 1, 0));

            Assert.True(engine.Step().Steps[0].Changed[0]);
            AssertState((0, 2, 2, 0));

            Assert.All(engine.Step().Steps, step => Assert.True(step.Changed.None));
            AssertState((0, 2, 2, 0));

            void AssertState((int Offer, int Value, int Pressure, int Demand) expected)
            {
                var actual = Read(source, target);
                Assert.Equal(expected, actual);
                Assert.Equal(4, actual.Offer + actual.Value + actual.Pressure - actual.Demand);
            }
        }
    }

    [Fact]
    public void UnpaidDrawPersistsUntilAnExplicitCancellationMatchesIt()
    {
        var (requests, count) = Make.Counts(1, 2);
        count.Write(requests, 0, 2);
        var tension = new MaterialTension(1, 1, 1, 2);
        tension.Pressure.Write(tension.Sites, 0, 1);
        var gate = new Store(1, 1);
        var request = tension.AcceptDemand(new Binding(requests, [.. count.Planes]), 2);
        var draw = tension.Draw();
        var cancel = tension.Cancel(new Binding(gate, 0));
        request.Add(0, 0);
        draw.Add(0, 0);
        cancel.Add(0, 0, 0);
        var receiver = new Engine().Add(request).Add(draw).Add(cancel);

        receiver.Step();
        Assert.Equal((1, 0, 1, 1), Read());
        receiver.Step();
        Assert.Equal((0, 0, 1, 2), Read());
        Assert.All(receiver.Step().Steps, step => Assert.True(step.Changed.None));
        Assert.Equal((0, 0, 1, 2), Read());

        gate.Write(0, 0, true);
        Assert.True(receiver.Step().Steps[2].Changed[0]);
        Assert.Equal((0, 0, 0, 1), Read());
        Assert.All(receiver.Step().Steps, step => Assert.True(step.Changed.None));
        Assert.Equal((0, 0, 0, 1), Read());

        (int?, int?, int?, int?) Read() =>
            (count.Read(requests, 0), tension.Value.Read(tension.Sites, 0), tension.Pressure.Read(tension.Sites, 0), tension.Demand.Read(tension.Sites, 0));
    }

    [Fact]
    public void AbsorptionAndDrawCannotBothWriteTheSameValueInOneStepTogether()
    {
        var tension = new MaterialTension(1, 3, 2, 1);
        tension.Value.Write(tension.Sites, 0, 1);
        tension.Pressure.Write(tension.Sites, 0, 1);
        tension.Demand.Write(tension.Sites, 0, 1);
        var absorb = tension.Absorb();
        var draw = tension.Draw();
        absorb.Add(0, 0);
        draw.Add(0, 0);
        var engine = new Engine().Add(absorb).Add(draw);

        var together = engine.StepTogether();
        Assert.All(together.Steps, step => Assert.True(step.Contested![0]));
        Assert.Equal((1, 1, 1), Read());

        engine.Step();
        Assert.Equal((1, 0, 0), Read());

        (int?, int?, int?) Read() =>
            (tension.Value.Read(tension.Sites, 0), tension.Pressure.Read(tension.Sites, 0), tension.Demand.Read(tension.Sites, 0));
    }
}
