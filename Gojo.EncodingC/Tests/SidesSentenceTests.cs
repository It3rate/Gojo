using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>"Something was moving toward the store, then turned back", on
/// sides of a landmark instead of a chain of sites. The road is one
/// orientation, home to the store, and has one landmark on it, a bridge. At
/// each moment the mover is a side word against the bridge, and the step that
/// arrived there is a step word: with the road or against it, moved
/// detectably or not. Nothing about the road's length is stored.
/// <para>As in the site fixture, the approach owns its moments, the last
/// being the turn, and the return owns only the moment after it: its step
/// back starts from the approach's own turn, read through a socket, and the
/// approach reads the return's moment through a second lane.</para>
/// <code>
/// road:      home ──── bridge ──── store      one landmark, one orientation
/// approach:  m0 ─with→ m1 ─with→ m2 ──────╮   m0 before the bridge
/// return:                    (m2) ─against→ r1   seen at the bridge
/// </code></summary>
public sealed class SidesSentenceTests
{
    private const int Planes = 8;
    private static readonly int After = Sides.After, Before = Sides.Before;
    private static readonly int AtFromAfter = Sides.AtFromAfter, AtFromBefore = Sides.AtFromBefore;

    private sealed record Clause(Engine Engine, Store Store, Supports Side, Supports Step, Wiring Moves, Port Edge, Socket Other);

    private static ulong Values(params int[] words) => words.Aggregate(0UL, (mask, w) => mask | 1UL << w);

    /// <summary>An ODV store on the road's orientation with two words per
    /// moment, the side against the bridge and the arriving step, both open.</summary>
    private static Clause Build(int moments, int edge)
    {
        var store = Odv.Create(moments);
        var stepCode = new Field(Sides.Word, store.AddPlane(), store.AddPlane());
        var side = Supports.On(store, Sides.Field);
        var step = Supports.On(store, stepCode);
        var moves = new Wiring(Sides.Moves(), side.Variable, step.Variable, side.Variable);
        var continuous = new Wiring(Sides.Continuous, side.Variable, side.Variable);
        for (var t = 1; t < moments; t++)
        {
            moves.Add(t - 1, t, t);
            continuous.Add(t - 1, t);
        }
        var exposed = store.Publish([edge], [.. side.Planes, .. step.Planes]);
        return new Clause(new Engine().Add(moves).Add(continuous), store, side, step, moves, exposed, new Socket(1, Planes));
    }

    private static Binding OtherSide(Clause clause) => Binding.Open(clause.Other, 0, 1, 2, 3);

    private static Binding OtherStep(Clause clause) => Binding.Open(clause.Other, 4, 5, 6, 7);

    /// <summary>Set out from home, before the bridge, and moving with the road
    /// twice. Through its socket it reads the moment after its turn and
    /// narrows the turn from the step that left it.</summary>
    private static Clause Approach()
    {
        var clause = Build(3, 2);
        clause.Side.Restrict(0, Values(Before));
        clause.Step.Restrict(1, Values(Sides.With));
        clause.Step.Restrict(2, Values(Sides.With));
        var leaving = new Wiring(Sides.Moves(), clause.Side.Variable, OtherStep(clause), OtherSide(clause));
        leaving.Add(2, 0, 0);
        var still = new Wiring(Sides.Continuous, clause.Side.Variable, OtherSide(clause));
        still.Add(2, 0);
        var turned = new Wiring(Sides.TurnedBack, clause.Step.Variable, OtherStep(clause));
        turned.Add(2, 0);
        clause.Engine.Add(leaving).Add(still).Add(turned);
        return clause;
    }

    /// <summary>Then turned back, and was seen at the bridge: one moment,
    /// reached by a step against the road from the turn it reads through its
    /// socket.</summary>
    private static Clause SeenAtTheBridge()
    {
        var clause = Build(1, 0);
        clause.Step.Restrict(0, Values(Sides.Against));
        clause.Side.Restrict(0, Values(AtFromBefore, AtFromAfter));
        var from = new Wiring(Sides.Moves(), OtherSide(clause), clause.Step.Variable, clause.Side.Variable);
        from.Add(0, 0, 0);
        var still = new Wiring(Sides.Continuous, OtherSide(clause), clause.Side.Variable);
        still.Add(0, 0);
        var turned = new Wiring(Sides.TurnedBack, OtherStep(clause), clause.Step.Variable);
        turned.Add(0, 0);
        clause.Engine.Add(from).Add(still).Add(turned);
        return clause;
    }

    private static void Join(Clause approach, Clause back)
    {
        approach.Other.Plug(back.Edge);
        back.Other.Plug(approach.Edge);
    }

    private static void Settle(params Engine[] order)
    {
        for (var round = 0; round < 16; round++)
            if (!order.Select(engine => engine.Step()).ToArray().Any(step => step.Steps.Any(wiring => wiring.Changed.Any))) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    private static ulong[] SidesOf(Clause clause) => [.. Enumerable.Range(0, clause.Store.Count).Select(clause.Side.Values)];

    /// <summary>Alone, the approach knows it set out before the bridge and
    /// moved with the road twice: it may have reached the bridge, even got
    /// onto its far side, or still be short of it. The return alone knows only
    /// that it stepped back and was seen at the bridge; what it came back from
    /// is not there, and its rows through the empty socket say so.</summary>
    [Fact]
    public void EachClauseMakesPartialSenseAlone()
    {
        var approach = Approach();
        var back = SeenAtTheBridge();

        Settle(approach.Engine);
        var skipped = back.Engine.Step().Steps.Select(step => step.Skipped.Any).ToArray();
        Settle(back.Engine);

        Assert.Equal([Values(Before), Values(Before, AtFromBefore), Values(Before, AtFromBefore, AtFromAfter)], SidesOf(approach));
        Assert.Equal(Values(AtFromBefore, AtFromAfter), back.Side.Values(0));
        Assert.Equal([false, false, true, true, true], skipped);
    }

    /// <summary>Joined, being seen at the bridge one step back from the turn
    /// means the turn was on the bridge's far side: the mover reached the bridge
    /// at its first step, got onto the far side at its second, turned, and
    /// came back onto the near side. Nothing in between was stored, and the
    /// crossing is established, not supposed. It ends the same whichever
    /// clause steps first.</summary>
    [Fact]
    public void BeingSeenAtTheBridgeOnTheWayBackShowsTheApproachCrossedIt()
    {
        foreach (var approachFirst in new[] { true, false })
        {
            var approach = Approach();
            var back = SeenAtTheBridge();
            Join(approach, back);

            Settle(approachFirst ? [approach.Engine, back.Engine] : [back.Engine, approach.Engine]);

            Assert.Equal([Values(Before), Values(AtFromBefore), Values(AtFromAfter)], SidesOf(approach));
            Assert.Equal(Values(AtFromBefore), back.Side.Values(0));
            Assert.True(back.Other.SamePosition(0, approach.Store, 2));
        }
    }

    /// <summary>Seen detectably short of the bridge one step after turning, the
    /// mover cannot have turned on the bridge's far side: getting back would
    /// have crossed the mark in one step. Whether it reached the bridge at all
    /// stays open. Partial evidence rules out what it can reach and no more.</summary>
    [Fact]
    public void SeenShortOfTheBridgeTheTurnWasNotPastIt()
    {
        var approach = Approach();
        var back = Build(1, 0);
        back.Step.Restrict(0, Values(Sides.Against));
        back.Side.Restrict(0, Values(Before));
        var from = new Wiring(Sides.Moves(), OtherSide(back), back.Step.Variable, back.Side.Variable);
        from.Add(0, 0, 0);
        back.Engine.Add(from);
        Join(approach, back);

        Settle(approach.Engine, back.Engine);

        Assert.Equal(Values(Before, AtFromBefore), approach.Side.Values(2));
        Assert.True(approach.Side.Empty.None);
    }

    /// <summary>Cutting the lanes leaves each clause with its partial sense.</summary>
    [Fact]
    public void TheLanesAreTheSeam()
    {
        var approach = Approach();
        var back = SeenAtTheBridge();
        Join(approach, back);
        approach.Other.Unplug();
        back.Other.Unplug();

        Settle(approach.Engine, back.Engine);

        Assert.Equal(Values(Before, AtFromBefore, AtFromAfter), approach.Side.Values(2));
        Assert.Equal(Values(AtFromBefore, AtFromAfter), back.Side.Values(0));
    }
}
