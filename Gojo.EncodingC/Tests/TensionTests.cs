using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Material tension (docs/GojoTension-2026-11.md): what arrives from
/// outside enters as pressure or debt, never into the value, and the
/// structure's own laws absorb it or pay it, one unit a step, within capacity.
/// Pressure and debt are two unary counts beside the value, kept apart; every
/// move is the one-unit move, into the value or into the room of a count.</summary>
public sealed class TensionTests
{
    private const int Capacity = 3;

    /// <summary>One position: a value of capacity three, with pressure and debt
    /// of capacity three beside it.</summary>
    private static Tension Site(int value = 0)
    {
        var store = new Store(Capacity, 1);
        var field = new Field(Shape.Unary(Capacity), 0, 1, 2);
        field.Write(store, 0, value);
        return new Tension(store, field, Capacity);
    }

    private static readonly Plane Here = Plane.Of(1, 0);

    private static (int? Value, int? Plus, int? Minus) Read(Tension t) =>
        (t.Value.Read(t.Store, 0), t.Plus.Read(t.Store, 0), t.Minus.Read(t.Store, 0));

    private static void Receive(Tension t, int units)
    {
        for (var i = 0; i < units; i++) Assert.True(t.Receive(Here).Blocked.None);
    }

    private static void Demand(Tension t, int units)
    {
        for (var i = 0; i < units; i++) Assert.True(t.Demand(Here).Blocked.None);
    }

    /// <summary>Room is a view: inverted and read from the top, a count of one
    /// in three reads two of room, and filling the room empties the count.</summary>
    [Fact]
    public void RoomIsTheCountReadFromItsCapacity()
    {
        var site = Site(value: 1);
        var room = site.Value.Room(site.Store);

        Assert.Equal(2, Shape.Unary(Capacity).Decode(Enumerable.Range(0, Capacity)
            .Aggregate(0UL, (m, b) => m | (room.Surface.Read(room.Planes[b], 0) ? 1UL << b : 0))));
        Assert.Throws<InvalidOperationException>(() => new Field(Shape.Binary(4), 0, 1).Room(site.Store));
    }

    /// <summary>Four units arrive at a value with room for three. They wait as
    /// pressure and are absorbed one a step; value plus pressure never changes.
    /// At capacity the last unit stays as pressure, and once a draw makes room
    /// it is absorbed.</summary>
    [Fact]
    public void ArrivingUnitsWaitAsPressureAndAreAbsorbedWithinCapacity()
    {
        var site = Site();
        var engine = new Engine().Add(site.Absorb(Here));
        Receive(site, 3);

        var seen = new List<(int?, int?, int?)>();
        for (var i = 0; i < 4; i++)
        {
            engine.Step();
            if (i == 0) Receive(site, 1);
            seen.Add(Read(site));
        }

        Assert.Equal([(1, 3, 0), (2, 2, 0), (3, 1, 0), (3, 1, 0)], seen);

        engine.Add(site.Draw(Here));
        Demand(site, 1);
        Assert.NotNull(engine.Settle(8));
        Assert.Equal((3, 0, 0), Read(site));
    }

    /// <summary>A draw is debt until it is paid from the value. Value minus
    /// debt never changes while paying. Drawing more than is held pays what
    /// is there and leaves the rest as debt, visible, not lost.</summary>
    [Fact]
    public void ADrawIsPaidFromTheValueAndWhatCannotBePaidStaysAsDebt()
    {
        var site = Site(value: 2);
        var engine = new Engine().Add(site.Draw(Here));
        Demand(site, 3);

        var seen = new List<(int?, int?, int?)>();
        for (var i = 0; i < 3; i++)
        {
            engine.Step();
            seen.Add(Read(site));
        }

        Assert.Equal([(1, 0, 2), (0, 0, 1), (0, 0, 1)], seen);
    }

    /// <summary>Pressure and debt at one position are never netted by default:
    /// with nothing wired they both stay. Cancelling is a law, wired where they
    /// are the same quantity, and pressure minus debt never changes as it runs.</summary>
    [Fact]
    public void PressureAndDebtAreNettedOnlyByAWiredLaw()
    {
        var site = Site(value: 3);
        Receive(site, 2);
        Demand(site, 3);
        var engine = new Engine().Hold(site.Store);

        Assert.NotNull(engine.Settle(4));
        Assert.Equal((3, 2, 3), Read(site));

        engine.Add(site.Cancel(Here));
        Assert.NotNull(engine.Settle(8));
        Assert.Equal((3, 0, 1), Read(site));
    }

    /// <summary>Absorbing and drawing on one value in a step taken together
    /// write different bits of one count, so they are contested, and the
    /// contest is left where laws can read it: contention, an open choice of
    /// which goes first. Taken in order, declared order chooses and both settle.</summary>
    [Fact]
    public void AbsorbingAndDrawingTogetherContendForTheValue()
    {
        var site = Site(value: 1);
        var absorb = site.Absorb(Here);
        var draw = site.Draw(Here);
        var engine = new Engine().Add(absorb).Add(draw);
        var contested = absorb.Record().Contested;
        Receive(site, 1);
        Demand(site, 1);

        engine.StepTogether();

        Assert.Equal((1, 1, 1), Read(site));
        Assert.True(absorb.Rows.Read(contested, 0));

        engine.Step();
        Assert.Equal((1, 0, 0), Read(site));
    }

    /// <summary>Value, pressure and debt are material: the moves conserve
    /// them, so a fork finds and holds all three without being told, and they
    /// are saved and restored as ordinary planes and wirings.</summary>
    [Fact]
    public void TensionIsMaterialAForkHoldsAndAMachineSaves()
    {
        var site = Site(value: 1);
        var engine = new Engine().Add(site.Absorb(Here)).Add(site.Draw(Here));
        Receive(site, 1);

        var fork = engine.Fork();
        var planes = site.Value.Planes.Concat(site.Plus.Planes).Concat(site.Minus.Planes).Select(p => (site.Store, p));
        Assert.All(planes, held => Assert.Contains(held, fork.Held));

        var saved = Transport.Save(engine);
        var (restored, stores) = Transport.Load(saved);
        restored.Settle(8);
        engine.Settle(8);
        Assert.Equal(Read(site), (site.Value.Read(stores[0], 0), site.Plus.Read(stores[0], 0), site.Minus.Read(stores[0], 0)));
    }

    /// <summary>A fork runs a draw through the value's room: the room is a view
    /// of the fork's workspace, and the workspace's selection reaches through
    /// it, so the row runs in the fork (a regression: it was skipped).</summary>
    [Fact]
    public void AForkPaysADebtThroughTheValuesRoom()
    {
        var site = Site(value: 2);
        Demand(site, 1);
        var fork = new Engine().Add(site.Draw(Here)).Fork();

        Assert.NotNull(fork.Engine.Settle(8));

        Assert.Equal((1, 0, 0), (site.Value.Read(fork.Of(site.Store), 0), site.Plus.Read(fork.Of(site.Store), 0), site.Minus.Read(fork.Of(site.Store), 0)));
        Assert.Equal((2, 0, 1), Read(site));
    }
}
