using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public class ForkFixtureTests
{
    private static List<ForkFrame> Run(ForkFixture machine, ForkTape tape)
    {
        var frames = new List<ForkFrame> { machine.Snapshot() };
        for (var beat = 1; beat <= 5; beat++)
        {
            tape.Apply(machine, beat);
            frames.Add(machine.Advance());
        }
        Assert.All(frames, frame => Assert.Equal(1, frame.PulseSites));
        return frames;
    }

    [Fact]
    public void A_stale_report_refuses_without_draining_copying_or_rescuing_the_offer()
    {
        var frames = Run(new(), new(false, false, 2));
        Assert.Equal(2, frames[1].Fork & 7);
        Assert.All(frames.Skip(2), frame =>
        {
            Assert.Equal(new HandoffRead(3, 3), frame.Transfers[0].Transfer);
            Assert.Equal(2, frame.Fork & 7);
            Assert.Equal(0, frame.Exits & 0x3E);
            Assert.Equal(1, frame.Receipt & 3);
        });
        Assert.Equal(new ForkCounts(4, 4, 0, 0), ForkCounts.From(frames));
    }

    [Fact]
    public void Release_accepts_the_held_offer_once_and_arrival_returns_one_beat_later()
    {
        var frames = Run(new(), new(false, false, 2, 4));
        Assert.Equal(new HandoffRead(1, 4), frames[4].Transfers[0].Transfer);
        Assert.Equal(0, frames[4].Fork & 7);
        Assert.Equal(4, frames[4].Exits & 0x3C);
        Assert.Equal(1, frames[4].Receipt & 3);
        Assert.Equal(16, frames[5].Exits & 0x3C);
        Assert.Equal(2, frames[5].Receipt & 3);
        Assert.Equal(new ForkCounts(3, 2, 1, 1), ForkCounts.From(frames));
    }

    [Theory]
    [InlineData(false, false, 2, 16)]
    [InlineData(false, true, 2, 16)]
    [InlineData(true, false, 4, 32)]
    [InlineData(true, true, 1, 0)]
    public void Route_depends_on_both_receiver_ports(bool left, bool right, int offer, int end)
    {
        var frames = Run(new(left, right), new(left, right));
        Assert.Equal(offer, frames[1].Fork & 7);
        Assert.Equal(end, frames[^1].Exits & 0x30);
    }

    [Fact]
    public void Removing_a_wire_breaks_only_its_causal_path()
    {
        var tape = new ForkTape(false, false);
        var noSeam = Run(new(connectLeft: false), tape);
        Assert.Equal(2, noSeam[^1].Fork & 7);
        Assert.Equal(default(ForkCounts), ForkCounts.From(noSeam));
        var noReport = Run(new(advertiseLeft: false), tape);
        Assert.Equal(1, noReport[^1].Fork & 7);
        var noReply = Run(new(connectReceipt: false), tape);
        Assert.Equal(16, noReply[^1].Exits & 0x30);
        Assert.Equal(1, noReply[^1].Receipt & 3);
    }

    [Fact]
    public void Unobserved_replay_matches_all_physical_state_bytes_at_every_beat()
    {
        var tape = new ForkTape(false, false, 2, 4);
        var instrumented = new ForkFixture();
        var counter = instrumented.AttachRefusalCounter();
        var observed = new List<ForkFrame> { instrumented.Snapshot() };
        for (var beat = 1; beat <= 5; beat++)
        {
            tape.Apply(instrumented, beat);
            observed.Add(instrumented.Advance());
            counter.Sample();
            Assert.Equal(Math.Clamp(beat - 1, 0, 2), counter.Reading);
        }
        _ = ForkCounts.From(observed);
        // Independent replays with no frame capture until the selected endpoint.
        for (var stop = 0; stop <= 5; stop++)
        {
            var bare = new ForkFixture();
            for (var beat = 1; beat <= stop; beat++)
            {
                tape.Apply(bare, beat);
                bare.Tick();
            }
            var actual = bare.Snapshot();
            Assert.Equal((observed[stop].Fork, observed[stop].Exits, observed[stop].Receipt),
                (actual.Fork, actual.Exits, actual.Receipt));
        }
    }

    [Fact]
    public void An_unconnected_offer_is_neither_an_attempt_nor_a_measured_refusal()
    {
        var machine = new ForkFixture(connectLeft: false);
        var counter = machine.AttachRefusalCounter();
        for (var beat = 1; beat <= 5; beat++)
        {
            machine.Tick();
            counter.Sample();
        }
        Assert.Equal(2, machine.Snapshot().Fork & 7);
        Assert.Equal(0, counter.Reading);
    }

    [Fact]
    public void Competing_exports_cannot_overwrite_or_duplicate_an_accepted_pulse()
    {
        var sender = new PortStructure();
        var receiver = new PortStructure();
        var sources = Enumerable.Range(0, 2).Select(_ => sender.ExportOutlet(sender.Publish(sender.Create(0, 0, 1, 1)))).ToArray();
        var gate = receiver.Publish(receiver.Create(0, 0, 1, 0));
        var target = receiver.Publish(receiver.Create(0, 0, 1, 0));
        var wires = new PortSeams(sources, [receiver.ExportIntake(gate, target)], PassageLaw.Controlled(false));
        wires.Link(0, 0);
        wires.Link(1, 0);
        var outcome = wires.Pulse();
        Assert.Equal(new HandoffRead(1, 4), outcome[0].Transfer);
        Assert.Equal(new HandoffRead(5, 5), outcome[1].Transfer);
        Assert.Equal(2, sender.PackedBytes(0, 0, 1)[0]);
        Assert.Equal(1, receiver.ReadPort(target));
    }

    [Fact]
    public void Material_port_rejects_a_migrated_width_without_partial_writes()
    {
        var a = new PortStructure();
        var send = a.Publish(a.Create(0, 0, 1, 1));
        var b = new PortStructure();
        var gate = b.Publish(b.Create(0, 0, 1, 0));
        var binding = b.Create(0, 0, 1, 0);
        var target = b.Publish(binding);
        var seams = new PortSeams([a.ExportOutlet(send)], [b.ExportIntake(gate, target)], PassageLaw.Controlled(false));
        seams.Link(0, 0);
        Assert.True(b.TryMove(binding, 0, 0, 2));
        Assert.Null(seams.Pulse()[0].Transfer.Before);
        Assert.Equal(1, a.ReadPort(send)); Assert.Equal(0, b.ReadPort(target));
    }
}
