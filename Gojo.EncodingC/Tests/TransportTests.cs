using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A machine saved as bits, its builders thrown away, and restored.</summary>
public sealed class TransportTests
{
    /// <summary>A small machine that leaves something of every kind unresolved:
    /// bits that are not a code, several possible next states, a retired row,
    /// a retired position, and a row whose roles a moved lane made meet.</summary>
    private static (Engine Engine, Store[] Stores) Build()
    {
        var (sender, count) = Make.Counts(4, 3);
        count.Write(sender, 0, 3);
        count.Write(sender, 2, 1);
        var outward = sender.Publish([0, 1], [0, 1, 2]);
        var inward = sender.Publish([2, 3], [0, 1, 2]);

        var (receiver, _) = Make.Counts(3, 3);
        receiver.Write(0, 2, true);
        receiver.Write(2, 2, true);

        var deliver = new Wiring(Flows.Move(3, 3), new Binding(outward, 0, 1, 2), new Binding(receiver, 0, 1, 2));
        deliver.Add(0, 0);
        deliver.Add(1, 1);
        deliver.Add(0, 2);
        deliver.Retire(1);

        var shuffle = new Wiring(Flows.Move(3, 3), new Binding(inward, 0, 1, 2), new Binding(inward, 0, 1, 2));
        shuffle.Add(0, 1);
        sender.Rebind(inward, 1, 2);

        var flicker = new Store(1, 2);
        var either = TransitionLaw.FromPairs([Shape.Bit], [(0, 0), (0, 1), (1, 1)], Accounting.None);
        var maybe = new Wiring(either, new Binding(flicker, 0));
        maybe.Add(0);
        maybe.Add(1);
        flicker.Retire(1);

        var chain = new Store(2, 5);
        chain.Write(0, 0, true);
        chain.Write(1, 1, true);
        chain.Write(1, 3, true);
        var passages = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        passages.Add(0, 1, 2);
        passages.Add(2, 3, 4);

        var engine = new Engine().Add(deliver).Add(shuffle).Add(maybe).Add(passages);
        return (engine, [sender, receiver, flicker, chain]);
    }

    [Fact]
    public void AMachineRestoredFromItsBitsRunsTheSameAndLeavesTheSameThingsUnresolved()
    {
        var (engine, stores) = Build();
        var saved = Transport.Save(engine, stores);

        var (restored, copies) = Transport.Load(saved);

        Assert.Equal(saved, Transport.Save(restored, copies.ToArray()));
        Assert.True(restored.Wirings[3].Law.SameTable(Flows.Passage));
        Assert.NotSame(Flows.Passage, restored.Wirings[3].Law);

        var first = default(EngineStep);
        for (var tick = 0; tick < 4; tick++)
        {
            var original = engine.Step();
            var again = restored.Step();
            first ??= original;
            for (var w = 0; w < 4; w++)
            {
                var (a, b) = (original.Steps[w], again.Steps[w]);
                Assert.Equal(a.Changed, b.Changed);
                Assert.Equal(a.Unread, b.Unread);
                Assert.Equal(a.Unadmitted, b.Unadmitted);
                Assert.Equal(a.Ambiguous, b.Ambiguous);
                Assert.Equal(a.Skipped, b.Skipped);
                Assert.Equal(a.Aliased, b.Aliased);
                Assert.Equal(a.Before, b.Before);
                Assert.Equal(a.After, b.After);
            }
            Assert.Equal(original.Waves, again.Waves);
            for (var s = 0; s < stores.Length; s++)
                for (var p = 0; p < stores[s].PlaneCount; p++)
                    Assert.Equal(stores[s].Read(p), copies[s].Read(p));
        }

        Assert.Equal(Plane.Of(3, 2), first!.Steps[0].Unread);
        Assert.False(restored.Wirings[0].Live[1]);
        Assert.Equal(Plane.Of(1, 0), first.Steps[1].Aliased);
        Assert.Equal(Plane.Of(2, 0), first.Steps[2].Ambiguous);
        Assert.Equal(Plane.Of(2, 1), first.Steps[2].Skipped);
        Assert.True(copies[3].Read(0, 4));
    }

    /// <summary>The passage chain: two planes over five positions, the passage
    /// law with its four promises, and two rows of three references. The whole
    /// machine is 22 bytes: 55 bits of stores (the chain and the rows), 80
    /// of law and 36 of binding. The law is saved as its eight next states,
    /// three bits each, which is all a table-driven device needs to run it;
    /// the rows are a store like the chain.</summary>
    [Fact]
    public void AMachineIsAFewBytes()
    {
        var chain = new Store(2, 5);
        var passages = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        passages.Add(0, 1, 2);
        passages.Add(2, 3, 4);
        var engine = new Engine().Add(passages);

        var saved = Transport.Save(engine);
        var size = Transport.Measure(engine);

        Assert.Equal(22, saved.Length);
        Assert.Equal(size.Bytes, saved.Length);
        Assert.Equal((55L, 80L, 36L), (size.StoreBits, size.LawBits, size.WiringBits));
    }

    [Fact]
    public void AWorkspaceIsAWorkingViewAndIsNotSavedWithAMachine()
    {
        var chain = new Store(2, 5);
        var workspace = new Workspace(chain).SelectAll();
        var rows = new Wiring(Flows.Passage, new Binding(workspace, 0), new Binding(workspace, 1), new Binding(workspace, 0));
        rows.Add(0, 1, 2);

        Assert.Throws<InvalidOperationException>(() => Transport.Save(new Engine().Add(rows)));
    }

    [Fact]
    public void BitsLeftOverAfterAMachineAreRefused()
    {
        Assert.Throws<FormatException>(() => Transport.Load([0xFF]));
        Assert.Empty(Transport.Load([0x07]).Stores);
    }

    /// <summary>Bits can parse and still describe something no builder would
    /// make, such as a passage table with one bit flipped, which breaks its
    /// accounting. Every single-bit change to a saved machine either still
    /// loads or is refused as malformed; nothing else escapes.</summary>
    [Fact]
    public void BitsThatDescribeNoMachineAreRefusedTheSameWay()
    {
        var chain = new Store(2, 5);
        var passages = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        passages.Add(0, 1, 2);
        var saved = Transport.Save(new Engine().Add(passages));

        var refused = 0;
        for (var bit = 0; bit < saved.Length * 8; bit++)
        {
            var damaged = (byte[])saved.Clone();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            try
            {
                Transport.Load(damaged);
            }
            catch (FormatException)
            {
                refused++;
            }
        }

        Assert.True(refused > 0);
    }
}
