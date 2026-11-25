using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Numbers as rungs over counts (ops_claude/GojoOperationsProposal-2026-11.md,
/// §6). A digit is a count, a column is a balance with the radix as its
/// weight, and a ladder of columns narrows in any direction. Counts stay
/// counts: grouping a count into rungs and refining it back are moves that
/// keep the place-value sum.</summary>
public sealed class NumberTests
{
    private static ulong Mask(params int[] digits) => digits.Aggregate(0UL, (m, d) => m | 1UL << d);

    /// <summary>a + b + carry in = radix · carry out + sum: one admitted
    /// (carry out, sum) for each of the 2·radix² inputs, at every radix. At
    /// radix 2 it is the bit adder's balance, at radix 10 a decimal column.</summary>
    [Fact]
    public void AColumnIsABalanceWithTheRadixAsItsWeight()
    {
        foreach (var radix in new[] { 2, 3, 4, 8, 10, 16 })
        {
            var column = Numbers.DigitSum(radix);
            Assert.Equal(2 * radix * radix, column.Relation.AdmittedCount);
            Assert.Equal([1, 1, 1, -radix, -1], column.Terms.Select(t => t.Weight));
        }
        Assert.Equal("a + b + c − 10d − e = 0", new Lens().Describe(Numbers.DigitSum(10)));
    }

    /// <summary>Two rungs of radix 4 over 256 lanes add every pair of numbers
    /// below 16, each lane agreeing with the wide balance a + b = total, the
    /// last carry holding what passes 15.</summary>
    [Fact]
    public void TwoRungsAddEveryPairAsTheWideBalanceDoes()
    {
        var store = new Store(0, 256);
        Ladder a = new(store, 4, 2), b = new(store, 4, 2), sum = new(store, 4, 2);
        var all = Plane.Ones(256);
        var (wirings, carries) = Ladder.Add(a, b, sum, all);
        for (var lane = 0; lane < 256; lane++)
        {
            a.Restrict(lane, lane & 15);
            b.Restrict(lane, lane >> 4);
        }
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);
        Assert.NotNull(engine.Settle(8));

        var wide = Operations.Balance([Shape.Unary(15), Shape.Unary(15), Shape.Unary(30)], 1, 1, -1);
        for (var lane = 0; lane < 256; lane++)
        {
            var overflow = System.Numerics.BitOperations.TrailingZeroCount(carries[2].Values(lane));
            Assert.True(wide.Admits(lane & 15, lane >> 4, (int)sum.Read(lane)!.Value + 16 * overflow));
        }
    }

    /// <summary>Three decimal rungs hold 0..999, past where one table can
    /// reach: a wide sum over those values would need about two billion bits.
    /// Forward, 487 + 356 settles to 843. Backward, 843 − 487 settles to 356
    /// from the same columns. With the middle digit of the first operand open,
    /// the sum's middle digit stays open too, coupled; narrowing the sum to 843
    /// settles the operand's digit to 8.</summary>
    [Fact]
    public void DecimalRungsAddSubtractAndKeepOpenDigitsOpen()
    {
        Assert.True(1000L * 1000 * 1999 > Law.MaxBits);

        (Ladder A, Ladder B, Ladder Sum, IReadOnlyList<Supports> Carries, Engine Engine) Column()
        {
            var store = new Store(0, 1);
            Ladder a = new(store, 10, 3), b = new(store, 10, 3), sum = new(store, 10, 3);
            var (wirings, carries) = Ladder.Add(a, b, sum, Plane.Ones(1));
            var engine = new Engine();
            foreach (var w in wirings) engine.Add(w);
            return (a, b, sum, carries, engine);
        }

        var forward = Column();
        forward.A.Restrict(0, 487);
        forward.B.Restrict(0, 356);
        Assert.NotNull(forward.Engine.Settle(10));
        Assert.Equal(843, forward.Sum.Read(0));
        Assert.Equal(Mask(0), forward.Carries[3].Values(0));

        var backward = Column();
        backward.Sum.Restrict(0, 843);
        backward.A.Restrict(0, 487);
        Assert.NotNull(backward.Engine.Settle(10));
        Assert.Equal(356, backward.B.Read(0));

        var open = Column();
        open.A.Restrict(0, 0, Mask(7));
        open.A.Restrict(0, 2, Mask(4));
        open.B.Restrict(0, 356);
        open.Carries[3].Restrict(0, Mask(0));
        Assert.NotNull(open.Engine.Settle(10));
        Assert.Null(open.A.Read(0));
        Assert.Equal(10, System.Numerics.BitOperations.PopCount(open.A.Open(0)[1]));
        Assert.Equal(Mask(3), open.Sum.Open(0)[0]);
        Assert.True(System.Numerics.BitOperations.PopCount(open.Sum.Open(0)[1]) > 1);
        open.Sum.Restrict(0, 843);
        Assert.NotNull(open.Engine.Settle(10));
        Assert.Equal(487, open.A.Read(0));
    }

    /// <summary>What does not fit is residue, visible on the last carry. 999 + 1
    /// gives 000 with an overflow of 1; forbidding overflow empties a support.
    /// Subtraction reads the same way (from ops_sol): two sources hold 9 and
    /// 11, both two groups of four on the coarse rung. Asked whether 10 can
    /// come out with no borrow, 11 leaves 1, and 9 contradicts: only the fine
    /// rung tells them apart.</summary>
    [Fact]
    public void WhatDoesNotFitIsTheLastCarry()
    {
        var store = new Store(0, 2);
        Ladder a = new(store, 10, 3), b = new(store, 10, 3), sum = new(store, 10, 3);
        var (wirings, carries) = Ladder.Add(a, b, sum, Plane.Ones(2));
        var engine = new Engine();
        foreach (var w in wirings) engine.Add(w);
        a.Restrict(0, 999); b.Restrict(0, 1);
        a.Restrict(1, 999); b.Restrict(1, 1); carries[3].Restrict(1, Mask(0));
        engine.Settle(10);
        Assert.Equal((0L, Mask(1)), (sum.Read(0)!.Value, carries[3].Values(0)));
        Assert.False(carries.Concat(sum.Possible).All(p => p.Empty.None));

        var quarters = new Store(0, 2);
        Ladder want = new(quarters, 4, 2), rest = new(quarters, 4, 2), have = new(quarters, 4, 2);
        var (take, borrow) = Ladder.Add(want, rest, have, Plane.Ones(2));
        var books = new Engine();
        foreach (var w in take) books.Add(w);
        for (var lane = 0; lane < 2; lane++)
        {
            want.Restrict(lane, 10);
            borrow[2].Restrict(lane, Mask(0));
        }
        have.Restrict(0, 11);
        have.Restrict(1, 9);
        Assert.Equal(have.Open(0)[1], have.Open(1)[1]);
        books.Settle(10);
        Assert.Equal(1, rest.Read(0));
        Assert.Contains(0UL, rest.Open(1));
    }

    /// <summary>A count of 11 groups into rungs of four by moving four ones into
    /// one four, three times, ending as 3 ones and 2 fours; the place-value sum
    /// ones + 4 · fours stays 11 at every step. The ladder reads the two counts
    /// through their first three planes, observes 3 and 2, and reads 11.
    /// Refining moves the fours back: 11 ones, as it began.</summary>
    [Fact]
    public void ACountGroupsIntoRungsAndRefinesBack()
    {
        var here = Plane.Of(1, 0);
        var store = new Store(0, 1);
        var ones = new Field(Shape.Unary(11), [.. Enumerable.Range(0, 11).Select(_ => store.AddPlane())]);
        var fours = new Field(Shape.Unary(3), [.. Enumerable.Range(0, 3).Select(_ => store.AddPlane())]);
        ones.Write(store, 0, 11);
        var ladder = new Ladder(store, 4, [new Field(Numbers.Digit(4), [.. ones.Planes.Take(3)]), fours]);

        var group = new Engine().Add(Tension.Wire(Numbers.Group(4, 11, 3), [ones.On(store), fours.On(store)], here));
        var seen = new List<(int Ones, int Fours)>();
        for (var i = 0; i < 4; i++)
        {
            group.Step();
            seen.Add((ones.Read(store, 0)!.Value, fours.Read(store, 0)!.Value));
        }
        Assert.Equal([(7, 1), (3, 2), (3, 2), (3, 2)], seen);
        Assert.All(seen, s => Assert.Equal(11, s.Ones + 4 * s.Fours));

        ladder.Observe(0);
        Assert.Equal(11, ladder.Read(0));

        var refined = new Store(0, 1);
        var back = new Field(Shape.Unary(11), [.. Enumerable.Range(0, 11).Select(_ => refined.AddPlane())]);
        var coarse = new Field(Shape.Unary(3), [.. Enumerable.Range(0, 3).Select(_ => refined.AddPlane())]);
        back.Write(refined, 0, 3);
        coarse.Write(refined, 0, 2);
        Assert.NotNull(new Engine().Add(Tension.Wire(Numbers.Refine(4, 3, 11), [coarse.On(refined), back.On(refined)], here)).Settle(4));
        Assert.Equal((11, 0), (back.Read(refined, 0)!.Value, coarse.Read(refined, 0)!.Value));
    }
}
