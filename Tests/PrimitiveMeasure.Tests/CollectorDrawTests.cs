namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class CollectorDrawTests
{
    // Receiver positions are gate, inbox, shelf. Only its intake crosses the seam.
    private static PortSeams Connect(GlyphJunction y, PassageFixture receiver, bool rightFirst = false) =>
        y.ConnectDraw(receiver.Owner.ExportIntake(receiver.Ports[0], receiver.Ports[1]), rightFirst);

    [Theory]
    [InlineData(false, 0x04, 0x10)]
    [InlineData(true, 0x01, 0x04)]
    public void Two_draws_empty_the_stem_while_a_sample_can_still_say_present(
        bool rightFirst, byte packedRows, byte waiting)
    {
        var y = GlyphJunction.YCollect(3);
        var receiver = new PassageFixture(3, 0);
        receiver.Add(receiver.Forward, 1, 0, 2);
        var seam = Connect(y, receiver, rightFirst);
        Assert.Equal(new[] { packedRows }, seam.RowBytes());

        var reader = new ReadConnections();
        var identity = reader.AddTable(ConnectionLaws.Identity());
        var input = reader.Import(y.Stem);
        var live = reader.Connect(identity, 2, input);
        var memory = new PortStructure();
        var binding = memory.Create(0, 0, 1, 0);
        var held = memory.Connect(memory.Publish(binding));
        var sample = reader.Wire(identity, 2, input, reader.Import(held));

        Assert.Equal(0x14, y.Advance());
        Assert.True(reader.TryCapture(identity, sample, memory, binding));
        Assert.Equal(2, live.ReadSupport());
        Assert.Equal(2, held.ReadSupport());
        Assert.Equal(0x14, y.Flow.Stored);
        Assert.Equal(0, receiver.Stored);

        var first = seam.Pulse();
        Assert.Equal(new HandoffRead(1, 4), first[0].Transfer);
        Assert.Equal(new HandoffRead(5, 5), first[1].Transfer);
        Assert.Equal(waiting, y.Flow.Stored);
        Assert.Equal(2, receiver.Stored);
        Assert.Equal(2, live.ReadSupport());
        Assert.DoesNotContain(seam.Pulse(), step => step.Transfer.Moved);
        Assert.Equal(waiting, y.Flow.Stored);

        Assert.True(receiver.TryStep(receiver.Observe(), out var parked));
        Assert.Equal(4, parked);
        Assert.Single(seam.Pulse(), step => step.Transfer.Moved);
        Assert.Equal(6, receiver.Stored);
        Assert.Equal(0, y.Flow.Stored);
        Assert.Equal(1, live.ReadSupport());
        Assert.Equal(2, held.ReadSupport());
        Assert.True(reader.TryCapture(identity, sample, memory, binding));
        Assert.Equal(1, held.ReadSupport());
        Assert.Equal(6, receiver.Stored);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void Either_private_slot_can_supply_a_request_and_an_empty_stem_cannot(byte arrivals)
    {
        foreach (var rightFirst in new[] { true, false })
        {
            var y = GlyphJunction.YCollect(arrivals);
            y.Advance();
            var receiver = new PassageFixture(3, 0);
            var result = Connect(y, receiver, rightFirst).Pulse();
            Assert.Equal(arrivals == 0 ? 0 : 1, result.Count(step => step.Transfer.Moved));
            Assert.Equal(arrivals == 0 ? 0 : 2, receiver.Stored);
            Assert.Equal(arrivals == 3 ? 2 : 1, y.Stem.ReadSupport());
        }
    }

    [Fact]
    public void Two_neighbours_can_share_a_read_while_only_one_can_receive_the_last_arrival()
    {
        var y = GlyphJunction.YCollect(1);
        y.Advance();
        var first = new PassageFixture(3, 0);
        var second = new PassageFixture(3, 0);
        var firstDraw = Connect(y, first);
        var secondDraw = Connect(y, second);
        foreach (var observer in new[] { new ReadConnections(), new ReadConnections() })
            Assert.Equal(2, observer.Read(observer.Import(y.Stem)));

        Assert.Single(firstDraw.Pulse(), step => step.Transfer.Moved);
        Assert.DoesNotContain(secondDraw.Pulse(), step => step.Transfer.Moved);
        Assert.Equal(2, first.Stored);
        Assert.Equal(0, second.Stored);
        Assert.Equal(1, y.Stem.ReadSupport());
    }

    [Fact]
    public void A_sensor_can_respond_with_its_own_activation_while_the_material_gate_is_closed()
    {
        var y = GlyphJunction.YCollect(3);
        var receiver = new PassageFixture(3, 1);
        var draw = Connect(y, receiver);
        var sensor = new PulseCircuit(2);
        var ready = sensor.State(true);
        var seen = sensor.State();
        var guard = sensor.Guard();
        sensor.Bind(guard, y.Stem);
        sensor.Link(sensor.AddTable(PassageLaw.Controlled()), ready, guard, seen);

        Assert.False(sensor.Pulse().Moved);
        y.Advance();
        Assert.All(draw.Pulse(), step => Assert.Equal(new HandoffRead(3, 3), step.Transfer));
        Assert.True(sensor.Pulse().Moved);
        Assert.Equal(seen, sensor.Active);
        Assert.Equal(0x14, y.Flow.Stored);
        Assert.Equal(1, receiver.Stored);
    }

    [Fact]
    public void One_handoff_can_publish_a_repeatable_signal_that_means_delivered()
    {
        var y = GlyphJunction.YCollect(3);
        y.Advance();
        var receiver = new PassageFixture(3, 0);
        var seam = Connect(y, receiver);
        // Row state 1 -> 4 means source full -> inbox full under a clear gate.
        byte[] accepted = [0, 0, 0, 0, 2, 0, 0, 0];
        var delivered = PresencePorts.Any(seam.ObserveTransition(0, accepted),
            seam.ObserveTransition(1, accepted));
        Assert.Equal(0, delivered.ReadSupport()); // No witnessed request yet.
        seam.Pulse();
        Assert.Equal(2, delivered.ReadSupport());
        Assert.Equal(2, delivered.ReadSupport());
        Assert.Equal(0x10, y.Flow.Stored);
        Assert.Equal(2, receiver.Stored);
        seam.Pulse(); // Inbox is full on both attempts.
        Assert.Equal(1, delivered.ReadSupport());
        Assert.Equal(2, y.Stem.ReadSupport());
        Assert.Equal(0x10, y.Flow.Stored);
        Assert.Equal(2, receiver.Stored);
    }
}
