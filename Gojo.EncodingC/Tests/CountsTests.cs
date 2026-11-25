using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Counting as laws: sum, shortfall, tally, a threshold held as data,
/// and a streak, each a small table emulating the arithmetic numeric support
/// will provide. Their meaning is in the tables, so it is saved, inspected and
/// replaced like any other structure.</summary>
public sealed class CountsTests
{
    private static int[] After(TransitionLaw law, params int[] before) => law.Tuple(Assert.Single(law.Afters(law.TupleIndex(before))));

    /// <summary>For every pair of counts, a sum is their total, the shortfall
    /// is how far have is below want, and the same law with the two swapped is
    /// the excess. The two faces are never both nonzero. Inputs never change.</summary>
    [Fact]
    public void SumsAndBothFacesOfAGapAreExact()
    {
        var sum = Counts.Sum(3, 2, weight: 2);
        var shortfall = Counts.Shortfall(3, 4);
        var excess = Counts.Shortfall(4, 3);
        for (var a = 0; a <= 3; a++)
            for (var b = 0; b <= 2; b++)
                Assert.Equal([a, b, a + 2 * b], After(sum, a, b, 0));
        for (var have = 0; have <= 3; have++)
            for (var want = 0; want <= 4; want++)
            {
                var (s, e) = (After(shortfall, have, want, 0)[2], After(excess, want, have, 0)[2]);
                Assert.Equal((want > have ? want - have : 0, have > want ? have - want : 0), (s, e));
                Assert.True(s == 0 || e == 0);
            }
    }

    /// <summary>Two valves, each reading one face of the gap: a shortfall opens
    /// the way in and an excess the way out. The faces are written by laws
    /// declared first, so each step the valves read them fresh. Have settles at
    /// want, from below and then from above, and the structure goes quiet.</summary>
    [Fact]
    public void EachFaceOfTheGapOpensOneValve()
    {
        var store = new Store(0, 1);
        Field Count(int capacity) => new(Shape.Unary(capacity), [.. Enumerable.Range(0, capacity).Select(_ => store.AddPlane())]);
        var (supply, have, drain, want) = (Count(3), Count(3), Count(3), Count(3));
        var (shortBy, overBy) = (Count(3), Count(3));
        supply.Write(store, 0, 3);
        have.Write(store, 0, 1);
        want.Write(store, 0, 3);
        var here = Plane.Of(1, 0);
        var engine = new Engine()
            .Add(Tension.Wire(Counts.Shortfall(3, 3), [have.On(store), want.On(store), shortBy.On(store)], here))
            .Add(Tension.Wire(Counts.Shortfall(3, 3), [want.On(store), have.On(store), overBy.On(store)], here))
            .Add(Tension.Route(supply.On(store), have.On(store), here, new Binding(store, shortBy.Planes[0])))
            .Add(Tension.Route(have.On(store), drain.On(store), here, new Binding(store, overBy.Planes[0])));

        Assert.NotNull(engine.Settle(16));
        Assert.Equal((1, 3, 0), (supply.Read(store, 0), have.Read(store, 0), drain.Read(store, 0)));

        want.Write(store, 0, 1);
        Assert.NotNull(engine.Settle(16));
        Assert.Equal((1, 1, 2), (supply.Read(store, 0), have.Read(store, 0), drain.Read(store, 0)));
    }

    /// <summary>A threshold is data. Whether a count of two is "enough" depends
    /// on the threshold stored beside it, and moving the threshold is writing a
    /// count, not changing a law.</summary>
    [Fact]
    public void AThresholdIsACountInThePlanes()
    {
        var store = new Store(0, 1);
        var count = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
        var threshold = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
        var reached = store.AddPlane();
        count.Write(store, 0, 2);
        var engine = new Engine().Add(Tension.Wire(Counts.AtLeast(3, 3), [count.On(store), threshold.On(store), new Binding(store, reached)], Plane.Of(1, 0)));

        var seen = new List<bool>();
        foreach (var t in new[] { 1, 2, 3, 0 })
        {
            threshold.Write(store, 0, t);
            engine.Settle(4);
            seen.Add(store.Read(reached, 0));
        }

        Assert.Equal([true, true, false, true], seen);
    }

    /// <summary>How settled a value is, as a tally of its support planes: two
    /// possible, one, or none. Bit 0 of the tally is "any possible" and bit 1
    /// "more than one", so settled is bit 0 without bit 1 and contradicted is
    /// no bit 0.</summary>
    [Fact]
    public void ATallyOfSupportsSaysOpenSettledOrContradicted()
    {
        var store = new Store(0, 3);
        var value = Supports.On(store, new Field(Shape.Binary(2), store.AddPlane()));
        var possible = new Field(Shape.Unary(2), store.AddPlane(), store.AddPlane());
        value.Restrict(1, 0b10);
        value.Restrict(2, 0b00);
        var engine = new Engine().Add(Tension.Wire(Counts.Tally(2), [.. value.Planes.Select(p => new Binding(store, p)), possible.On(store)], Plane.Ones(3)));

        engine.Settle(4);

        Assert.Equal([2, 1, 0], Enumerable.Range(0, 3).Select(i => possible.Read(store, i)));
    }

    /// <summary>The slow clock, from three small laws: a streak counts the
    /// steps a signal holds, a threshold read says when it has held long
    /// enough, and a latch recruits a spare row then. A short blip does not
    /// recruit it; a signal that persists for three steps does, and the row,
    /// once recruited, runs.</summary>
    [Fact]
    public void APersistentSignalRecruitsASpareRowABlipDoesNot()
    {
        var store = new Store(0, 1);
        var signal = store.AddPlane();
        var streak = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
        var patience = new Field(Shape.Unary(3), store.AddPlane(), store.AddPlane(), store.AddPlane());
        var due = store.AddPlane();
        var (from, to) = (new Field(Shape.Unary(2), store.AddPlane(), store.AddPlane()), new Field(Shape.Unary(2), store.AddPlane(), store.AddPlane()));
        patience.Write(store, 0, 3);
        from.Write(store, 0, 2);
        var spare = new Wiring(Flows.Move(2, 2), from.On(store), to.On(store));
        spare.AddSpare(0, 0);
        var here = Plane.Of(1, 0);
        var recruit = new Wiring(Settling.Latch(true), new Binding(store, due), new Binding(spare.Rows.Life, 0));
        recruit.Add(0, 0);
        var engine = new Engine()
            .Add(Tension.Wire(Counts.Streak(3), [new Binding(store, signal), streak.On(store)], here))
            .Add(Tension.Wire(Counts.AtLeast(3, 3), [streak.On(store), patience.On(store), new Binding(store, due)], here))
            .Add(recruit)
            .Add(spare);

        store.Write(signal, 0, true);
        engine.Run(2);
        store.Write(signal, 0, false);
        engine.Run(2);
        Assert.False(spare.Live[0]);

        store.Write(signal, 0, true);
        engine.Run(5);
        Assert.True(spare.Live[0]);
        Assert.Equal((0, 2), (from.Read(store, 0), to.Read(store, 0)));
    }

    /// <summary>Filling an order keeps both accounts: a unit leaves the
    /// source, arrives at the target and closes one unit of order in the same
    /// move. With nothing on order nothing moves, and a full target holds both
    /// the unit and the order.</summary>
    [Fact]
    public void FillingAnOrderMovesAUnitAndClosesTheOrderTogether()
    {
        var fill = Flows.Fulfill(3, 2, 2, take: 1, give: 2);

        Assert.Equal([2, 0, 2], After(fill, 3, 1, 0));
        Assert.Equal([3, 0, 0], After(fill, 3, 0, 0));
        Assert.Equal([3, 1, 1], After(fill, 3, 1, 1));
    }

    /// <summary>A table too large to check exhaustively is refused before it is
    /// built, whether asked for directly, by gating a law on too many bits, or
    /// by justifying on too many reasons. The refusal names the limit rather
    /// than failing while allocating.</summary>
    [Fact]
    public void OversizedTablesAreRefusedBeforeTheyAreBuilt()
    {
        var wide = Shape.Unary(40);
        Assert.Throws<ArgumentOutOfRangeException>(() => RelationLaw.FromPredicate([wide, wide, wide, wide, wide], _ => true));
        Assert.Throws<ArgumentOutOfRangeException>(() => Flows.When(Counts.Sum(6, 6), gates: 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => Settling.Justify(11));
        Assert.Equal(2, Flows.When(Flows.Move(2, 2), gates: 2).Roles.Count - 2);
    }
}
