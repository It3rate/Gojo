using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Local structures: free connection inside, ports between, each
/// stepping on its own, and saved with its boundary.</summary>
public sealed class LocalStructureTests
{
    private static readonly TransitionLaw Move = Flows.Move(3, 3);

    /// <summary>A well: an inner count at position 0 that moves one unit a step
    /// to an outlet at position 1, which the well exposes.</summary>
    private static (Engine Engine, Store Well, Field Count, Port Outlet) Well(int units)
    {
        var (well, count) = Make.Counts(2, 3);
        count.Write(well, 0, units);
        var inner = new Wiring(Move, new Binding(well, 0, 1, 2), new Binding(well, 0, 1, 2));
        inner.Add(0, 1);
        return (new Engine().Add(inner), well, count, well.Publish([1], [0, 1, 2]));
    }

    /// <summary>A well that splits its inner count between two outlets, 1 and 2,
    /// one unit to each per step, and exposes each outlet as its own port: one
    /// owner serving two takers by its own law.</summary>
    private static (Engine Engine, Store Well, Field Count, Port Left, Port Right) Fork(int units)
    {
        var (well, count) = Make.Counts(3, 3);
        count.Write(well, 0, units);
        var split = new Wiring(Move, new Binding(well, 0, 1, 2), new Binding(well, 0, 1, 2));
        split.Add(0, 1);
        split.Add(0, 2);
        return (new Engine().Add(split), well, count, well.Publish([1], [0, 1, 2]), well.Publish([2], [0, 1, 2]));
    }

    private static (Engine Engine, Store Cup, Field Count, Wiring Take) Taker(Surface from)
    {
        var (cup, count) = Make.Counts(1, 3);
        var take = Make.Wire(Move, from, cup);
        return (new Engine().Add(take), cup, count, take);
    }

    [Fact]
    public void AStructureBindsItsOwnStoresFreelyAndAnotherOnlyThroughAPort()
    {
        var (_, well, count, outlet) = Well(0);
        count.Write(well, 1, 1);
        var (cup, cups) = Make.Counts(1, 3);
        var taker = new Engine();
        var shut = TransitionLaw.FromFunction([Shape.Bit], _ => [0], Accounting.None);
        var lifeOfWell = new Wiring(shut, new Binding(well.Life, 0));
        lifeOfWell.Add(1);
        var supposition = new Workspace(well);
        var supposedWell = new Wiring(Move, new Binding(supposition, 0, 1, 2), new Binding(supposition, 0, 1, 2));
        supposedWell.Add(0, 1);

        Assert.Throws<ArgumentException>(() => taker.Add(Make.Wire(Move, well, cup)));
        Assert.Throws<ArgumentException>(() => taker.Add(lifeOfWell));
        Assert.Throws<ArgumentException>(() => taker.Add(supposedWell));
        Assert.Throws<ArgumentException>(() => taker.Hold(well));
        Assert.Empty(taker.Stores);

        var take = Make.Wire(Move, outlet, cup);
        taker.Add(take);
        taker.Step();

        Assert.Equal(new[] { cup, take.Rows }, taker.Stores);
        Assert.Equal((0, 1), (count.Read(well, 1), cups.Read(cup, 0)));
    }

    /// <summary>One position has one taker, so two structures never race for a
    /// unit. A second taker on the same outlet is refused. An owner that serves
    /// two splits what it has by its own law and exposes a port for each; then
    /// the takers get the same whichever steps first, as structures stepping at
    /// the same moment would.</summary>
    [Fact]
    public void TwoTakersAreServedAlikeWhicheverStepsFirst()
    {
        var (_, _, _, outlet) = Well(1);
        _ = Taker(outlet);
        Assert.Throws<ArgumentException>(() => Taker(outlet));

        foreach (var leftFirst in new[] { true, false })
        {
            var (owner, well, count, leftOutlet, rightOutlet) = Fork(2);
            var left = Taker(leftOutlet);
            var right = Taker(rightOutlet);

            owner.Step();
            foreach (var engine in leftFirst ? new[] { left.Engine, right.Engine } : [right.Engine, left.Engine]) engine.Step();

            Assert.Equal((0, 0, 0), (count.Read(well, 0), count.Read(well, 1), count.Read(well, 2)));
            Assert.Equal((1, 1), (left.Count.Read(left.Cup, 0), right.Count.Read(right.Cup, 0)));
        }
    }

    /// <summary>What one structure considers possible informs another through a
    /// port, by reference. The reader narrows its own value, never the
    /// owner's, and cannot reach the owner's supports directly.</summary>
    [Fact]
    public void WhatOneStructureKnowsInformsAnotherAndIsNeverNarrowedThere()
    {
        var owner = new Store(1, 2);
        var known = Supports.On(owner, Field.Bit(0));
        owner.Write(0, 0, true);
        known.Observe(0);
        var shown = owner.Publish([0, 1], [.. known.Planes]);
        new Engine().Hold(owner);

        var reader = new Store(1, 2);
        var mine = Supports.On(reader, Field.Bit(0));
        mine.Restrict(1, 0b01);
        var link = new Wiring(Settling.Same(Shape.Bit), Binding.Open(shown, 0, 1), mine.Variable);
        link.Add(0, 0);
        link.Add(1, 1);
        var direct = new Wiring(Settling.Same(Shape.Bit), known.Variable, mine.Variable);
        direct.Add(1, 1);
        var reading = new Engine().Add(link);

        Assert.Throws<ArgumentException>(() => reading.Add(direct));
        reading.Step();

        Assert.Equal((0b10UL, 0b01UL), (mine.Values(0), mine.Values(1)));
        Assert.Equal(0b11UL, known.Values(1));
    }

    /// <summary>A taker steps on its own whatever its peer does. With nothing
    /// plugged in, its row reaches nowhere and is recorded as skipped. Plugged
    /// in to a peer that has not stepped, it finds the outlet as the peer left
    /// it. Unplugged again, the unit the peer moved waits at the outlet.</summary>
    [Fact]
    public void AStructureKeepsRunningWhenItsPeerStallsOrIsAbsent()
    {
        var (sender, well, count, outlet) = Well(2);
        var socket = Socket.For(outlet);
        var (taker, cup, cups, take) = Taker(socket);
        var skipped = take.Record().Skipped;

        Assert.True(taker.Step().Steps[0].Skipped[0]);
        Assert.True(take.Rows.Read(skipped, 0));

        socket.Plug(outlet);
        Assert.True(taker.Step().Steps[0].Skipped.None);
        Assert.Equal((2, 0, 0), (count.Read(well, 0), count.Read(well, 1), cups.Read(cup, 0)));

        sender.Step();
        taker.Step();
        taker.Step();
        Assert.Equal((1, 0, 1), (count.Read(well, 0), count.Read(well, 1), cups.Read(cup, 0)));

        socket.Unplug();
        sender.Step();
        Assert.True(taker.Step().Steps[0].Skipped[0]);
        Assert.Equal((0, 1, 1), (count.Read(well, 0), count.Read(well, 1), cups.Read(cup, 0)));
    }

    /// <summary>The owner can retire the position behind a lane. The
    /// connection then reaches nowhere and says so; rebinding the lane brings
    /// it back without the taker changing anything.</summary>
    [Fact]
    public void RetiringAnExposedPositionStopsTheConnectionVisibly()
    {
        var (well, count) = Make.Counts(2, 3);
        count.Write(well, 0, 1);
        count.Write(well, 1, 1);
        var port = well.Publish([0], [0, 1, 2]);
        new Engine().Hold(well);
        var (taker, cup, cups, _) = Taker(port);

        well.Retire(0);
        Assert.True(taker.Step().Steps[0].Skipped[0]);
        Assert.Equal(0, cups.Read(cup, 0));

        well.Rebind(port, 0, 1);
        taker.Step();
        Assert.Equal((0, 1), (count.Read(well, 1), cups.Read(cup, 0)));
    }

    /// <summary>The owner lays out its ports as it likes, but never so that two
    /// taken ports show one position. Unplugging lets a port go, and another
    /// connection may then take through it.</summary>
    [Fact]
    public void OnePositionHasOneTakerHoweverTheOwnerLaysOutItsPorts()
    {
        var (_, well, _, left, right) = Fork(0);
        var first = Socket.For(left);
        _ = Taker(first);
        first.Plug(left);
        _ = Taker(right);

        Assert.Throws<ArgumentException>(() => well.Rebind(right, 0, 1));
        Assert.Throws<ArgumentException>(() => well.Extend(right, 1));
        Assert.Throws<ArgumentException>(() => Taker(well.Publish([1, 0], [0, 1, 2])));
        well.Extend(right, 0);

        var second = Socket.For(left);
        _ = Taker(second);
        Assert.Throws<ArgumentException>(() => second.Plug(left));
        first.Unplug();
        second.Plug(left);
        Assert.Throws<ArgumentException>(() => first.Plug(left));
    }

    /// <summary>A position is the same position whether reached directly,
    /// through a port or through a socket plugged into that port. Another
    /// position with equal bits is not it, and rebinding the lane moves what
    /// the connection reaches.</summary>
    [Fact]
    public void IdentityIsPositionNotEqualBits()
    {
        var (well, count) = Make.Counts(2, 3);
        count.Write(well, 0, 2);
        count.Write(well, 1, 2);
        var port = well.Publish([0], [0, 1, 2]);
        var socket = Socket.For(port);

        Assert.False(socket.SamePosition(0, well, 0));
        socket.Plug(port);

        Assert.True(socket.SamePosition(0, well, 0));
        Assert.True(port.SamePosition(0, socket, 0));
        Assert.False(socket.SamePosition(0, well, 1));
        well.Rebind(port, 0, 1);
        Assert.True(socket.SamePosition(0, well, 1));
        Assert.False(socket.SamePosition(0, well, 0));
    }

    /// <summary>A socket fits a port with as many planes, constant in the same
    /// places. Lane counts may differ.</summary>
    [Fact]
    public void ASocketTakesOnlyAPortOfItsShape()
    {
        var store = new Store(2, 3);
        var turned = store.AddConstantPlane(true);
        var plain = store.Publish([0, 1], [0, 1]);
        var withOrientation = store.Publish([0], [0, turned]);
        var socket = new Socket(4, 2);

        Assert.True(socket.Fits(plain));
        Assert.False(socket.Fits(withOrientation));
        Assert.False(socket.Fits(store.Publish([0], [0])));
        Assert.True(Socket.For(withOrientation).Fits(withOrientation));
        Assert.Throws<ArgumentException>(() => socket.Plug(withOrientation));

        socket.Plug(plain);
        Assert.Equal((true, true, false), (socket.IsLive(1), socket.Read(1, 0) == store.Read(1, 0), socket.IsLive(2)));
    }

    /// <summary>A structure saved on its own keeps the shape of what it expects
    /// from others as a socket, and no bit of the peer. Restored, it runs with
    /// its socket empty until something is plugged in.</summary>
    [Fact]
    public void AStructureSavedAloneKeepsItsSocketAndIsPluggedInAgain()
    {
        var (_, well, count, outlet) = Well(0);
        count.Write(well, 1, 3);
        var original = Socket.For(outlet);
        original.Plug(outlet);
        var (taker, _, _, _) = Taker(original);

        var saved = Transport.Save(taker);
        var (restored, stores) = Transport.Load(saved);

        Assert.Throws<ArgumentException>(() => Transport.Save(taker, well));
        Assert.Equal(taker.Stores.Count, stores.Count);
        Assert.Equal(saved, Transport.Save(restored));
        var socket = Assert.Single(restored.Sockets);
        Assert.Equal((1, 3, null), (socket.Count, socket.PlaneCount, socket.Plugged));
        Assert.True(restored.Step().Steps[0].Skipped[0]);

        Assert.Throws<ArgumentException>(() => socket.Plug(outlet));
        original.Unplug();
        socket.Plug(outlet);
        restored.Step();
        var (cup, cups) = (stores[0], new Field(Shape.Unary(3), [0, 1, 2]));
        Assert.Equal((2, 1), (count.Read(well, 1), cups.Read(cup, 0)));
    }

    /// <summary>Three structures saved together: a well and two takers, one
    /// bound to the port and one through a socket. Restored, each store
    /// belongs to its own structure, every socket is plugged into the restored
    /// port, and stepping in the same order gives the same bits.</summary>
    [Fact]
    public void StructuresSavedTogetherKeepTheirBoundariesAndConnections()
    {
        var (sender, well, _, leftOutlet, rightOutlet) = Fork(3);
        var socket = Socket.For(rightOutlet);
        socket.Plug(rightOutlet);
        var left = Taker(leftOutlet);
        var right = Taker(socket);
        var originals = new[] { sender, left.Engine, right.Engine };

        var saved = Transport.SaveLocals(originals);
        var restored = Transport.LoadLocals(saved).Select(local => local.Engine).ToArray();

        Assert.Equal(saved, Transport.SaveLocals(restored));
        var plugged = Assert.Single(restored[2].Sockets).Plugged!;
        Assert.Same(restored[0], plugged.Owner.Local);
        Assert.NotSame(well, plugged.Owner);
        Assert.Throws<ArgumentException>(() => restored[1].Add(Make.Wire(Move, plugged.Owner, (Store)restored[1].Wirings[0].Bindings[1].Surface)));

        for (var tick = 0; tick < 5; tick++)
        {
            foreach (var engine in originals) engine.Step();
            foreach (var engine in restored) engine.Step();
            Assert.Equal(Transport.SaveLocals(originals), Transport.SaveLocals(restored));
        }
        Assert.Equal(3, left.Count.Read(left.Cup, 0) + right.Count.Read(right.Cup, 0));
    }

    /// <summary>Two partial machines, a well and a taker joined through a
    /// socket, fuse into one local structure. It steps as the two did in that
    /// order, may now bind the well's store directly, and saves as one machine
    /// with nothing left to plug in.</summary>
    [Fact]
    public void PartialStructuresFuseIntoOne()
    {
        var (sender, well, count, outlet) = Well(3);
        var socket = Socket.For(outlet);
        socket.Plug(outlet);
        var (taker, cup, cups, _) = Taker(socket);
        var (twinSender, twinWell, twinCount, twinOutlet) = Well(3);
        var (twinTaker, twinCup, _, _) = Taker(twinOutlet);

        var fused = sender.Fuse(taker);

        Assert.Empty(taker.Stores);
        Assert.Empty(taker.Wirings);
        Assert.Contains(cup, fused.Stores);
        Assert.Throws<ArgumentException>(() => taker.Hold(cup));
        for (var tick = 0; tick < 3; tick++)
        {
            fused.Step();
            twinSender.Step();
            twinTaker.Step();
        }
        Assert.Equal((twinCount.Read(twinWell, 0), cups.Read(twinCup, 0)), (count.Read(well, 0), cups.Read(cup, 0)));
        fused.Add(Make.Wire(Move, well, cup));

        var (restored, _) = Transport.Load(Transport.Save(fused));
        Assert.Empty(restored.Sockets);
        Assert.Equal(Transport.Save(fused), Transport.Save(restored));
    }

    [Fact]
    public void StructuresSavedTogetherMustHoldEveryStoreTheyReach()
    {
        var (well, _) = Make.Counts(1, 3);
        var (alone, _, _, _) = Taker(well.Publish([0], [0, 1, 2]));
        var (sender, _, _, outlet) = Well(0);
        var (elsewhere, _, _, _) = Taker(outlet);

        Assert.NotEmpty(Transport.Save(alone));
        Assert.Throws<InvalidOperationException>(() => Transport.SaveLocals(alone));
        Assert.Throws<InvalidOperationException>(() => Transport.SaveLocals(elsewhere));
        Assert.NotEmpty(Transport.SaveLocals(sender, elsewhere));
    }
}
