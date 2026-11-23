using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Working on a small part of a large structure. A workspace reads
/// its source live and copies only what it writes; its selection decides which
/// rows of a wiring run there. A row that reaches just past the selection
/// brings what it touches in by use, one step further from the request, while
/// reach is left; an ignored area never comes in, and rows that would need it
/// are reported as beyond. So the selection need not be right at the start:
/// what is touched comes into context, and what is set aside stays out.
/// <para>In a running structure the same connection is an attention plane on
/// a wiring's rows: an explicit execution input that laws can move.</para></summary>
public sealed class SelectionTests
{
    private const int Length = 1000;

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    /// <summary>A long chain of open bits, each held equal to the next: a large
    /// structure whose every position is related to its neighbours.</summary>
    private static (Store Store, Supports Bits, Wiring Chain) Chain()
    {
        var store = new Store(1, Length);
        var bits = Supports.On(store, Field.Bit(0));
        return (store, bits, Link(bits));
    }

    /// <summary>Each position held equal to the next, on whatever surface the
    /// supports are seen through: the store, or a workspace over it.</summary>
    private static Wiring Link(Supports bits)
    {
        var chain = new Wiring(Settling.Same(Shape.Bit), bits.Variable, bits.Variable);
        for (var i = 0; i + 1 < Length; i++) chain.Add(i, i + 1);
        return chain;
    }

    private static Plane Window(int from, int to) => Plane.Of(Length, Enumerable.Range(from, to - from + 1));

    /// <summary>Applies a wiring in a workspace until nothing changes, and
    /// returns every row that went beyond the selection.</summary>
    private static Plane Settle(Workspace workspace, Wiring wiring)
    {
        var beyond = new Plane(wiring.Count);
        for (var round = 0; round < 64; round++)
        {
            var step = workspace.Apply(wiring);
            beyond = beyond.Or(step.Beyond);
            if (step.Step.Changed.None && step.Brought.None) return beyond;
        }
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    /// <summary>A thousand related positions, and a window of five. Seeing one
    /// bit in the window settles the window and nothing else: only the window
    /// is copied, the two rows that leave it are reported as beyond, and the
    /// rest of the structure is never touched or copied.</summary>
    [Fact]
    public void ASmallWindowOfALargeStructureCostsOnlyTheWindow()
    {
        var (store, bits, _) = Chain();
        var workspace = new Workspace(store, reach: 0).Select(Window(500, 504));
        var supposed = bits.In(workspace);
        supposed.Restrict(502, Values(1));

        var beyond = Settle(workspace, Link(supposed));

        Assert.All(Enumerable.Range(500, 5), i => Assert.Equal(Values(1), supposed.Values(i)));
        Assert.Equal(Values(0, 1), supposed.Values(505));
        Assert.Equal(Window(500, 504), workspace.Held);
        Assert.Equal(Plane.Of(Length - 1, 499, 504), beyond);
        Assert.Equal(Values(0, 1), bits.Values(502));
    }

    /// <summary>With reach, the rows at the window's edge bring their
    /// neighbours in by use, up to two steps out, and the read spreads that far
    /// and stops.</summary>
    [Fact]
    public void ReachBringsInWhatUseTouches()
    {
        var (store, bits, _) = Chain();
        var workspace = new Workspace(store, reach: 2).Select(Window(500, 504));
        var supposed = bits.In(workspace);
        supposed.Restrict(502, Values(1));

        var beyond = Settle(workspace, Link(supposed));

        Assert.Equal(Plane.Of(Length, 498, 499, 505, 506), workspace.Used);
        Assert.Equal(Window(498, 506), workspace.Held);
        Assert.Equal(Plane.Of(Length - 1, 497, 506), beyond);
    }

    /// <summary>An ignored area stays out even when a row touches it: use never
    /// brings it in, and the row that would is reported as beyond.</summary>
    [Fact]
    public void AnIgnoredAreaStaysOutEvenWhenTouched()
    {
        var (store, bits, _) = Chain();
        var workspace = new Workspace(store, reach: 2).Select(Window(500, 504)).Ignore(Window(505, 510));
        var supposed = bits.In(workspace);
        supposed.Restrict(502, Values(1));

        var beyond = Settle(workspace, Link(supposed));

        Assert.Equal(Plane.Of(Length, 498, 499), workspace.Used);
        Assert.True(beyond[504]);
        Assert.False(workspace.Held[505]);
    }

    /// <summary>Going to the store means using every segment of the route,
    /// and using the bridge segment needs the bridge to be open. The plan
    /// selects only the route. The bridge's status was never selected, but the
    /// row from the bridge segment to it brings it in by use, and the status in
    /// the world, closed, makes the route impossible in the plan. Nothing in
    /// the world is changed by finding that out.</summary>
    [Fact]
    public void WhatTheRouteTouchesComesIntoContextWithoutBeingSelected()
    {
        const int Segments = 10, Bridge = 4, Status = 10;
        var world = new Store(1, Segments + 1);
        var bits = Supports.On(world, Field.Bit(0));
        bits.Restrict(Status, Values(0));
        var plan = new Workspace(world, reach: 1).Select(Plane.Of(Segments + 1, Enumerable.Range(0, Segments)));
        var supposed = bits.In(plan);
        var route = new Wiring(Settling.Same(Shape.Bit), supposed.Variable, supposed.Variable);
        for (var i = 0; i + 1 < Segments; i++) route.Add(i, i + 1);
        var needs = new Wiring(RelationLaw.FromPredicate([Shape.Bit, Shape.Bit], t => t[0] == 0 || t[1] == 1), supposed.Variable, supposed.Variable);
        needs.Add(Bridge, Status);
        supposed.Restrict(0, Values(1));

        for (var round = 0; round < 16; round++)
        {
            var a = plan.Apply(route);
            var b = plan.Apply(needs);
            if (a.Step.Changed.None && b.Step.Changed.None && b.Brought.None) break;
        }

        Assert.True(plan.Used[Status]);
        Assert.False(plan.Requested[Status]);
        Assert.Equal(0UL, supposed.Values(Bridge));
        Assert.Equal(Values(0, 1), bits.Values(Bridge));
        Assert.Equal(Values(0), bits.Values(Status));
    }

    /// <summary>In a running structure, selection reaches execution through an
    /// attention plane on the rows. Attending to four rows of a thousand, the
    /// engine narrows only there; widening attention is writing one more bit,
    /// which a law could do as well as the host. The plane is saved with the
    /// machine.</summary>
    [Fact]
    public void AnAttentionPlaneIsSelectionConnectedToExecution()
    {
        var (store, bits, chain) = Chain();
        var attention = chain.Attend();
        foreach (var row in Enumerable.Range(0, Length - 1).Where(r => r is < 500 or > 503)) chain.Rows.Write(attention, row, false);
        var engine = new Engine().Add(chain);
        bits.Restrict(501, Values(1));

        for (var round = 0; round < 8; round++) engine.Step();

        Assert.All(Enumerable.Range(500, 5), i => Assert.Equal(Values(1), bits.Values(i)));
        Assert.Equal((Values(0, 1), Values(0, 1)), (bits.Values(499), bits.Values(505)));

        chain.Rows.Write(attention, 504, true);
        engine.Step();
        Assert.Equal(Values(1), bits.Values(505));

        var saved = Transport.Save(engine);
        var (restored, _) = Transport.Load(saved);
        Assert.Equal(attention, restored.Wirings[0].Attention);
        Assert.Equal(saved, Transport.Save(restored));
    }
}
