using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Numbers with any partition, and how much they know
/// (Gojo/docs/GojoOperationsSynthesis-2026-11.md, §3.3 and §3.4). A rung is a partition with its
/// own radix; a count's capacity is its denominator; and what is known below
/// a reading is structure: finer rungs held at zero for a count, left open for
/// a measurement. Operations carry that openness through their relations, so
/// uncertainty is never rounded away, and what a partition cannot hold is kept
/// as residue.</summary>
public sealed class NumberResolutionTests
{
    private static ulong Mask(params int[] digits) => digits.Aggregate(0UL, (m, d) => m | 1UL << d);

    private static Engine Wired(IEnumerable<Wiring> wirings)
    {
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);
        return engine;
    }

    /// <summary>Each rung has its own radix. Fifths under whole stars (radices
    /// 5 and 6): 3 3/5 + 1 4/5 = 5 2/5, the fifths carrying at five. A clock in
    /// binary-coded digits (radices 60 and 24): 23:59 + 0:01 = 0:00 with an
    /// overflow of one day. Nothing is a power of two unless the data is.</summary>
    [Fact]
    public void EachRungHasItsOwnPartition()
    {
        var store = new Store(0, 1);
        Ladder a = new(store, [5, 6]), b = new(store, [5, 6]), sum = new(store, [5, 6]);
        var (wirings, carries) = Ladder.Add(a, b, sum, Plane.Ones(1));
        a.Restrict(0, 3 * 5 + 3);
        b.Restrict(0, 1 * 5 + 4);
        Assert.NotNull(Wired(wirings).Settle(8));
        Assert.Equal(5 * 5 + 2, sum.Read(0));
        Assert.Equal([Mask(2), Mask(5)], sum.Open(0));
        Assert.Equal(Mask(0), carries[^1].Values(0));

        var clock = new Store(0, 1);
        Ladder now = new(clock, [60, 24], binary: true), step = new(clock, [60, 24], binary: true), then = new(clock, [60, 24], binary: true);
        Assert.Equal(6, now.Digits[0].Planes.Count);
        var (ticks, days) = Ladder.Add(now, step, then, Plane.Ones(1));
        now.Restrict(0, 23 * 60 + 59);
        step.Restrict(0, 1);
        Assert.NotNull(Wired(ticks).Settle(8));
        Assert.Equal((0L, Mask(1)), (then.Read(0)!.Value, days[^1].Values(0)));
    }

    /// <summary>Partitions add (the user's example): three out of five and two
    /// out of five, as counts, are five out of ten, exactly. The sum's whole has
    /// ten partitions because each operand brought five, and the sum's rung is
    /// out of ten without any rule saying so: the column's shapes do it.</summary>
    [Fact]
    public void ThreeOfFivePlusTwoOfFiveIsFiveOfTen()
    {
        var store = new Store(0, 1);
        Ladder a = new(store, [6]), b = new(store, [6]), sum = new(store, [11]);
        var (wirings, _) = Ladder.Add(a, b, sum, Plane.Ones(1));
        a.Restrict(0, 3);
        b.Restrict(0, 2);
        Assert.NotNull(Wired(wirings).Settle(4));
        Assert.Equal((5L, 10L), (sum.Read(0)!.Value, sum.Capacity));
    }

    /// <summary>A count is exact; a measurement is not. Read to the whole star
    /// with tenths below, 3 stars counted is 3.0 exactly, but 3 stars measured
    /// means at least 3.0 and below 4.0. Counted, 3 + 2 is 5.0. Measured, the
    /// tenths may carry, so the whole stars of the sum are 5 or 6 and the sum
    /// lies within 5.0 to 6.9 as each rung alone reads it. That bound is sound
    /// but not tight: supposing the sum were 6.9 contradicts, since no two
    /// readings below 4.0 and 3.0 make it, while 6.8 is consistent. The
    /// uncertainty is carried, not rounded away, and how tight it is can be
    /// asked.</summary>
    [Fact]
    public void AMeasurementCarriesItsUncertaintyThroughASum()
    {
        (Ladder A, Ladder B, Ladder Sum, Engine Engine) Pair()
        {
            var store = new Store(0, 1);
            Ladder a = new(store, [10, 6]), b = new(store, [10, 6]), sum = new(store, [10, 11]);
            var (wirings, _) = Ladder.Add(a, b, sum, Plane.Ones(1));
            return (a, b, sum, Wired(wirings));
        }

        var counted = Pair();
        counted.A.Restrict(0, 30);
        counted.B.Restrict(0, 20);
        Assert.NotNull(counted.Engine.Settle(6));
        Assert.Equal(50, counted.Sum.Read(0));

        var measured = Pair();
        measured.A.Measure(0, 3, rung: 1);
        measured.B.Measure(0, 2, rung: 1);
        Assert.Equal((30L, 39L), measured.A.Interval(0));
        Assert.NotNull(measured.Engine.Settle(6));
        Assert.Null(measured.Sum.Read(0));
        Assert.Equal(Mask(5, 6), measured.Sum.Open(0)[1]);
        Assert.Equal((50L, 69L), measured.Sum.Interval(0));

        bool Consistent(long total)
        {
            var supposed = Pair();
            supposed.A.Measure(0, 3, rung: 1);
            supposed.B.Measure(0, 2, rung: 1);
            supposed.Sum.Restrict(0, total);
            supposed.Engine.Settle(6);
            return supposed.A.Interval(0) is not null && supposed.B.Interval(0) is not null && supposed.Sum.Interval(0) is not null;
        }
        Assert.False(Consistent(69));
        Assert.True(Consistent(68));
        Assert.True(Consistent(50));
    }

    /// <summary>The same share is not the same knowledge. Five tenths and one
    /// half are the same share (5 · 2 = 1 · 10), but one half measured to the
    /// half means anything from 5 to 9 tenths, while five tenths measured to
    /// the tenth means 5. Reducing 5/10 to 1/2 throws that away; so it is a
    /// read, never done for you. The same five, read as a total of two ratings
    /// out of five, is 5 of 10; read as their mean, in halves of a star, it is
    /// 2 1/2 of 5.</summary>
    [Fact]
    public void TheSameShareIsNotTheSameKnowledge()
    {
        Assert.True(Numbers.SameShare(2, 10).Holds([1, 5]));
        Assert.False(Numbers.SameShare(2, 10).Holds([1, 6]));

        var store = new Store(0, 2);
        var halves = new Ladder(store, [5, 2]);
        halves.Measure(0, 1, rung: 1);
        halves.Measure(1, 5, rung: 0);
        Assert.Equal((5L, 9L), halves.Interval(0));
        Assert.Equal((5L, 5L), halves.Interval(1));

        var mean = new Ladder(new Store(0, 1), [2, 6]);
        mean.Restrict(0, 5);
        Assert.Equal([Mask(1), Mask(2)], mean.Open(0));
    }

    /// <summary>Changing partition keeps what does not fit. Three fifths read in
    /// tenths are six tenths, exactly. Read in eighths they are four eighths,
    /// and the residue of four fortieths (one tenth) is kept as a role, not
    /// rounded away: 4/8 + 1/10 = 3/5 (the same answer as the native kernel's
    /// projection with residue, from ops_sol's round). Four fifths read in
    /// thirds are two thirds with a residue of two fifteenths.</summary>
    [Fact]
    public void ChangingPartitionKeepsWhatDoesNotFit()
    {
        (int Value, int Residue) Read(int from, int to, int count)
        {
            var rescale = Numbers.Rescale(from, to);
            var law = Operations.Bind(rescale.Relation, rescale.Roles, [0, -1, -1], [-1, 1, 2]);
            var after = law.Tuple(Assert.Single(law.Afters(law.TupleIndex([count, 0, 0]))));
            return (after[1], after[2]);
        }
        Assert.Equal((6, 0), Read(5, 10, 3));
        Assert.Equal((4, 4), Read(5, 8, 3));
        Assert.Equal((2, 2), Read(5, 3, 4));
        Assert.Equal("8a − 5b − c = 0", new Lens().Describe(Numbers.Rescale(5, 8)));
    }

    /// <summary>Partitions multiply under a product. Three of five paired with
    /// two of five are six of twenty-five cells, as make hold pairs them. With
    /// measurements, a relation carries the exact set, not an interval: a
    /// value known to be 4 or 5 halves times one known to be 2 or 3 is 8, 10,
    /// 12 or 15 quarters, and nothing between.</summary>
    [Fact]
    public void PartitionsMultiplyAndMeasurementsKeepTheirSets()
    {
        var cells = Polynomial.Product(5, 5).Relation;
        Assert.Equal(Mask(6), cells.Project(2, [Mask(3), Mask(2), (1UL << 26) - 1]));

        var quarters = Polynomial.Product(6, 6).Relation;
        Assert.Equal(Mask(8, 10, 12, 15), quarters.Project(2, [Mask(4, 5), Mask(2, 3), (1UL << 37) - 1]));
    }

    /// <summary>Multiplication across rungs, as a lattice of relations: 47 × 36
    /// is 1,692 in decimal rungs, and every pair of two-digit numbers over 64
    /// lanes agrees with ordinary multiplication. Narrowed backward, 1,692
    /// over 47 settles 36: the lattice divides here, though a lattice need not
    /// settle every question the whole product could (from ops_sol).</summary>
    [Fact]
    public void LaddersMultiplyAcrossRungs()
    {
        var store = new Store(0, 64);
        Ladder a = new(store, 10, 2), b = new(store, 10, 2), product = new(store, 10, 4);
        var engine = Wired(Ladder.Multiply(a, b, product, Plane.Ones(64)));
        uint x = 2463534242;
        var expected = new long[64];
        for (var lane = 0; lane < 64; lane++)
        {
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            long p = x % 100, q = (x >> 8) % 100;
            a.Restrict(lane, lane == 0 ? 47 : p);
            b.Restrict(lane, lane == 0 ? 36 : q);
            expected[lane] = lane == 0 ? 1692 : p * q;
        }
        Assert.NotNull(engine.Settle(20));
        Assert.Equal(1692, product.Read(0));
        for (var lane = 0; lane < 64; lane++) Assert.Equal(expected[lane], product.Read(lane));

        var back = new Store(0, 1);
        Ladder known = new(back, 10, 2), unknown = new(back, 10, 2), result = new(back, 10, 4);
        var divide = Wired(Ladder.Multiply(known, unknown, result, Plane.Ones(1)));
        known.Restrict(0, 47);
        result.Restrict(0, 1692);
        Assert.NotNull(divide.Settle(20));
        Assert.Equal(36, unknown.Read(0));
    }

    /// <summary>Material enters one unit at a time and carries atomically (from
    /// ops_sol). Fifteen units offered to two rungs of fifths under a coarse
    /// digit of capacity two: fourteen are admitted, the fine digit never
    /// leaves 0..4, the fifteenth waits as pressure, and pressure + 5 · coarse
    /// + fine stays 15 throughout. A ladder over the two digits reads 14.</summary>
    [Fact]
    public void UnitsAreAdmittedAndCarriedAtomically()
    {
        var here = Plane.Of(1, 0);
        var store = new Store(0, 1);
        Field pressure = new(Shape.Unary(15), [.. Enumerable.Range(0, 15).Select(_ => store.AddPlane())]),
            coarse = new(Shape.Unary(2), store.AddPlane(), store.AddPlane()),
            fine = new(Numbers.Digit(5), [.. Enumerable.Range(0, 4).Select(_ => store.AddPlane())]);
        pressure.Write(store, 0, 15);
        var engine = new Engine().Add(Tension.Wire(Numbers.Admit(5, 15, 2), [pressure.On(store), coarse.On(store), fine.On(store)], here));
        for (var step = 0; step < 16; step++)
        {
            engine.Step();
            Assert.Equal(15, pressure.Read(store, 0)!.Value + 5 * coarse.Read(store, 0)!.Value + fine.Read(store, 0)!.Value);
        }
        Assert.Equal((1, 2, 4), (pressure.Read(store, 0)!.Value, coarse.Read(store, 0)!.Value, fine.Read(store, 0)!.Value));

        var ladder = new Ladder(store, [5, 3], [fine, coarse]);
        ladder.Observe(0);
        Assert.Equal(14, ladder.Read(0));
    }

    /// <summary>Narrowing is monotone within one observation (from ops_sol):
    /// once a ladder has read 7, a new reading of 9 contradicts it. Reopening
    /// starts a new observation, and then 9 reads as 9.</summary>
    [Fact]
    public void ANewObservationReopensTheLadder()
    {
        var ladder = new Ladder(new Store(0, 1), 4, 2);
        ladder.Restrict(0, 7);
        Assert.Equal(7, ladder.Read(0));
        ladder.Restrict(0, 9);
        Assert.Null(ladder.Interval(0));
        ladder.Reopen(0);
        ladder.Restrict(0, 9);
        Assert.Equal(9, ladder.Read(0));
    }

    /// <summary>A new observation of an input must reach what was derived
    /// from it (from ops_sol's review). 7 + 2 settles to 9. Reopening only the
    /// input and observing 5 leaves the sum narrowed by the old reading, and
    /// the network contradicts itself: visible, not silently stale. Reopening
    /// the whole network first, the new observation reads 5 + 2 = 7.</summary>
    [Fact]
    public void AReobservationReopensWhatWasDerivedFromIt()
    {
        var store = new Store(0, 1);
        Ladder a = new(store, 4, 2), b = new(store, 4, 2), sum = new(store, 4, 2);
        var (wirings, carries) = Ladder.Add(a, b, sum, Plane.Ones(1));
        var engine = Wired(wirings);
        a.Restrict(0, 7);
        b.Restrict(0, 2);
        Assert.NotNull(engine.Settle(6));
        Assert.Equal(9, sum.Read(0));

        a.Reopen(0);
        a.Restrict(0, 5);
        engine.Settle(6);
        Assert.Contains(a.Possible.Concat(b.Possible).Concat(sum.Possible).Concat(carries), p => !p.Empty.None);

        Ladder.Reopen(wirings, 0);
        a.Restrict(0, 5);
        b.Restrict(0, 2);
        Assert.NotNull(engine.Settle(6));
        Assert.Equal(7, sum.Read(0));
    }

    /// <summary>A product network re-observes too: 12 × 34 = 408, then the
    /// whole network reopened and 21 × 43 read as 903, then reopened again and
    /// read backward, 903 over 21 settling to 43. The lattice's padding, the
    /// sums' first carries and the fact that each running sum fits are
    /// constants, so reopening cannot open them.</summary>
    [Fact]
    public void AProductNetworkReobservesAfterReopening()
    {
        var store = new Store(0, 1);
        Ladder a = new(store, 10, 2), b = new(store, 10, 2), product = new(store, 10, 4);
        var network = Ladder.Multiply(a, b, product, Plane.Ones(1));
        var engine = Wired(network);
        a.Restrict(0, 12);
        b.Restrict(0, 34);
        Assert.NotNull(engine.Settle(20));
        Assert.Equal(408, product.Read(0));

        Ladder.Reopen(network, 0);
        a.Restrict(0, 21);
        b.Restrict(0, 43);
        Assert.NotNull(engine.Settle(20));
        Assert.Equal(903, product.Read(0));

        Ladder.Reopen(network, 0);
        a.Restrict(0, 21);
        product.Restrict(0, 903);
        Assert.NotNull(engine.Settle(20));
        Assert.Equal(43, b.Read(0));
    }
}
