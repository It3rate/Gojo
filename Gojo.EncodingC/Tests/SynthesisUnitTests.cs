using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class SynthesisUnitTests
{
    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath))]
    public void TwoPortionsAddOnOneSelectedWhole(ExecutionConformancePath path)
    {
        var store = new Store(0, 2);
        var left = new Ladder(store, [5, 2]).AtUnitRung(1);
        var right = new Ladder(store, [5, 2]).AtUnitRung(1);
        var sum = new Ladder(store, [5, 3]).AtUnitRung(1);
        var (columns, carries) = Ladder.Add(left, right, sum, store.Live);
        var engine = new Engine();
        foreach (var column in columns) engine.Add(column);
        left.Restrict(0, 3);
        right.Restrict(0, 2);
        left.Restrict(1, 5);
        right.Restrict(1, 5);

        Assert.NotNull(path.Settle(engine, 8));
        Assert.Equal(5L, sum.TicksPerUnit);
        Assert.Same(sum.Possible[1], sum.Unit);
        Assert.Equal(5L, sum.Read(0));
        Assert.Equal(10L, sum.Read(1));
        Assert.Equal(1UL << 1, sum.Unit.Values(0));
        Assert.Equal(1UL << 2, sum.Unit.Values(1));
        Assert.Equal(1UL, carries[^1].Values(1));

        // Publish the selected whole rung. The saved column and its port
        // still show which result field was read in whole units.
        store.Publish([0, 1], [.. sum.Unit.Planes]);
        var saved = Transport.Save(engine);
        var loaded = Transport.Load(saved);
        var published = Assert.Single(Assert.Single(loaded.Stores, s => s.Ports.Count != 0).Ports);
        Assert.All(path.Step(loaded.Engine).Steps, step => Assert.True(step.Quiet));
        Assert.True(published.Read(1, 0));
        Assert.True(published.Read(2, 1));
        Assert.Equal(saved, Transport.Save(loaded.Engine));
    }

    [Theory]
    [MemberData(nameof(ExecutionConformancePath.All), MemberType = typeof(ExecutionConformancePath))]
    public void DifferentUnitsNeedAnExplicitCommonRefinement(ExecutionConformancePath path)
    {
        var store = new Store(0, 1);
        var fifths = new Ladder(store, [5, 2]).AtUnitRung(1);
        var thirds = new Ladder(store, [3, 2]).AtUnitRung(1);
        var disguisedWhole = fifths.AtUnitRung(0);
        var sum = new Ladder(store, [15, 2]).AtUnitRung(1);
        var count = store.PlaneCount;

        // Same shapes and vocabulary, different selected units: two whole ticks are not two fifths.
        Assert.Same(fifths.Possible[0], disguisedWhole.Unit);
        Assert.Null(Ladder.TryAdd(fifths, disguisedWhole, fifths, store.Live));
        Assert.Null(Ladder.TryAdd(fifths, thirds, sum, store.Live));
        Assert.Equal(count, store.PlaneCount);
        Assert.Null(sum.Read(0));

        var left = new Ladder(store, [15, 2]).AtUnitRung(1);
        var right = new Ladder(store, [15, 2]).AtUnitRung(1);
        var exposeLeft = RefineFractionalPart(fifths, left);
        var exposeRight = RefineFractionalPart(thirds, right);
        var engine = new Engine().Add(exposeLeft).Add(exposeRight);
        foreach (var column in Ladder.Add(left, right, sum, store.Live).Wirings) engine.Add(column);
        fifths.Restrict(0, 2);
        thirds.Restrict(0, 1);
        left.Measure(0, 0, 1);
        right.Measure(0, 0, 1);

        Assert.NotNull(path.Settle(engine, 8));
        Assert.Equal(6L, left.Read(0));
        Assert.Equal(5L, right.Read(0));
        Assert.Equal(11L, sum.Read(0));
        Assert.Equal(15L, sum.TicksPerUnit);
        Assert.Equal(2L, fifths.Read(0));
        Assert.Equal(1L, thirds.Read(0));
    }

    // The test selects the fractional-part ports on a common unit. The
    // calibration is the relation's weights, not a change to either source.
    private static Wiring RefineFractionalPart(Ladder source, Ladder target)
    {
        var coarse = source.Possible[0];
        var fine = target.Possible[0];
        var sameShare = Operations.Balance([coarse.Field.Shape, fine.Field.Shape],
            checked((int)target.TicksPerUnit), checked(-(int)source.TicksPerUnit));
        var refinement = new Wiring(sameShare, coarse.Variable, fine.Variable);
        refinement.Add(0, 0);
        return refinement;
    }

    [Fact]
    public void MeasurementRangeChecksDoNotWrapOrNarrowOnFailure()
    {
        var store = new Store(0, 1);
        var ladder = new Ladder(store, 2, 63);
        var open = ladder.Open(0);

        Assert.Equal(long.MaxValue, ladder.Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(() => ladder.Measure(0, long.MaxValue, 1));
        Assert.Equal(open, ladder.Open(0));
        ladder.Restrict(0, long.MaxValue);
        Assert.Equal(long.MaxValue, ladder.Read(0));
        Assert.Throws<OverflowException>(() => ladder.PlaceValue(63));
    }
}
