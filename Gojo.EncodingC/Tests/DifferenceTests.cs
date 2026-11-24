using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>The two-faced gap: have against want as a deficit and an excess,
/// each a unary count, read live. One unit short and one unit over no longer
/// look alike, and a response reads one operand.</summary>
public sealed class DifferenceTests
{
    /// <summary>For every pair of counts, with capacities equal or not, the
    /// deficit is how far want is above have and the excess how far have is
    /// above want, each a valid unary count, never both nonzero.</summary>
    [Fact]
    public void DeficitAndExcessAreCountsAndNeverBothPresent()
    {
        foreach (var (haveCap, wantCap) in new[] { (3, 3), (2, 5), (5, 2) })
            for (var h = 0; h <= haveCap; h++)
                for (var w = 0; w <= wantCap; w++)
                {
                    var store = new Store(haveCap + wantCap, 1);
                    var have = new Field(Shape.Unary(haveCap), [.. Enumerable.Range(0, haveCap)]);
                    var want = new Field(Shape.Unary(wantCap), [.. Enumerable.Range(haveCap, wantCap)]);
                    have.Write(store, 0, h);
                    want.Write(store, 0, w);
                    var gap = new Difference(store, have, store, want);

                    Assert.Equal((w > h ? w - h : 0, h > w ? h - w : 0), (gap.Deficit.Read(gap, 0), gap.Excess.Read(gap, 0)));
                }
    }

    /// <summary>A goal written into a workspace over the store: the gap reads
    /// the store live, so as have moves the gap follows without any step, and
    /// it is empty once have has arrived.</summary>
    [Fact]
    public void TheGapFollowsHaveLive()
    {
        var store = new Store(3, 1);
        var count = new Field(Shape.Unary(3), 0, 1, 2);
        var goal = new Workspace(store);
        count.Write(goal, 0, 2);
        var gap = new Difference(store, count, goal, count);

        var seen = new List<(int?, int?)>();
        foreach (var have in new[] { 0, 2, 3 })
        {
            count.Write(store, 0, have);
            seen.Add((gap.Deficit.Read(gap, 0), gap.Excess.Read(gap, 0)));
        }

        Assert.Equal([(2, 0), (0, 0), (0, 1)], seen);
    }

    /// <summary>A response reads one operand. A valve brings a unit in while the
    /// deficit's lowest bit says "short", and another lets one out while the
    /// excess's says "over". Have settles at want, and settles again when want
    /// changes, from whichever side.</summary>
    [Fact]
    public void ADeficitOpensTheWayInAndAnExcessTheWayOut()
    {
        var store = new Store(12, 1);
        var (supply, have, drain, want) = (Count(0), Count(3), Count(6), Count(9));
        Field Count(int first) => new(Shape.Unary(3), first, first + 1, first + 2);
        supply.Write(store, 0, 3);
        have.Write(store, 0, 1);
        want.Write(store, 0, 3);
        var gap = new Difference(store, have, store, want);
        var here = Plane.Of(1, 0);
        var engine = new Engine()
            .Add(Tension.Moves(supply.On(store), have.On(store), here, new Binding(gap, gap.Deficit.Planes[0])))
            .Add(Tension.Moves(have.On(store), drain.On(store), here, new Binding(gap, gap.Excess.Planes[0])));

        Assert.NotNull(engine.Settle(8));
        Assert.Equal((1, 3, 0), (supply.Read(store, 0), have.Read(store, 0), drain.Read(store, 0)));

        want.Write(store, 0, 1);
        Assert.NotNull(engine.Settle(8));
        Assert.Equal((1, 1, 2), (supply.Read(store, 0), have.Read(store, 0), drain.Read(store, 0)));
    }

    /// <summary>A gap against a goal in a workspace is a supposition: a relation
    /// reading it does not narrow what is known. Have and want of two
    /// different stores are refused, since a law reading the gap would read
    /// across a boundary it could not bind.</summary>
    [Fact]
    public void AGapAgainstAGoalIsSupposedAndStaysInsideOneStructure()
    {
        var store = new Store(1, 1);
        var count = new Field(Shape.Unary(1), 0);
        var goal = new Workspace(store);
        count.Write(goal, 0, 1);
        var known = Supports.On(store, Field.Bit(0));
        var gap = new Difference(store, count, goal, count);
        var same = new Wiring(Settling.Same(Shape.Bit), new Binding(gap, gap.Deficit.Planes[0]), known.Variable);
        same.Add(0, 0);

        same.Apply();
        Assert.False(same.Writes(1));
        Assert.Equal(0b11UL, known.Values(0));

        Assert.Throws<ArgumentException>(() => new Difference(store, count, new Store(1, 1), count));
    }
}
