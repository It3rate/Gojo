using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>"Something was moving toward the store, then turned back", as two
/// local structures sharing one path definition and joined by thin lanes.
/// <para>The goal defines the path: home, its origin and lower end, through
/// three sites to the store, on one orientation. "Toward the store" is not a
/// distance; it is the direction of the definition. Each clause is a use of
/// that path: at each moment the mover's site is an open role, and each step
/// has a direction, forward, staying or back, relative to the path.</para>
/// <para><b>One turning moment.</b> The approach owns its moments, the last
/// being the turn. The return owns only what came after: its first step
/// starts from the approach's own turning moment, reached through a socket,
/// not from a copy of it. "The same something" is that connection. An
/// equal-looking mover, another approach whose every bit matches, is a
/// different position, and <see cref="Surface.SamePosition"/> tells them
/// apart where no comparison of values can.</para>
/// <para>Each side narrows only its own moments: the return reads the turn,
/// and the approach reads the return's next moment through a second lane.
/// Admitting the store as a site makes arrival possible; only an observation
/// there establishes it.</para>
/// <code>
/// path:      home 0 → 1 → 2 → 3 → store 4
/// approach:  m0 ─fwd→ m1 ─fwd→ m2 ──────────╮ the turn, one position
/// return:                      (m2) ─back→ r1   forward then back
/// </code></summary>
public sealed class TwoClauseTests
{
    private const int Sites = 5;
    private const int Planes = Sites + 3;
    private static readonly Shape Site = Shape.Binary(Sites);
    private static readonly (int, int)[] Road = [(0, 1), (1, 2), (2, 3), (3, 4)];
    private static readonly RelationLaw Step = Paths.Step(Sites, Road);
    private static readonly RelationLaw Moving = RelationLaw.FromPredicate([Paths.Direction], t => t[0] == Paths.Forward);
    private static readonly RelationLaw MovingBack = RelationLaw.FromPredicate([Paths.Direction], t => t[0] == Paths.Back);

    /// <summary>A clause: its store of moments, a site and an arrival
    /// direction at each, the port showing its edge moment, and the socket
    /// through which it reaches the other clause's.</summary>
    private sealed record Clause(Engine Engine, Store Store, Supports Site, Supports Arrival, Wiring Steps, Port Edge, Socket Other);

    private static Clause Build(int moments, int edge)
    {
        var store = new Store(Site.Bits + Paths.Direction.Bits, moments);
        var site = Supports.On(store, new Field(Site, 0, 1, 2));
        var arrival = Supports.On(store, new Field(Paths.Direction, 3, 4));
        var steps = new Wiring(Step, site.Variable, site.Variable, arrival.Variable);
        for (var t = 1; t < moments; t++) steps.Add(t - 1, t, t);
        var exposed = store.Publish([edge], [.. site.Planes, .. arrival.Planes]);
        return new Clause(new Engine().Add(steps), store, site, arrival, steps, exposed, new Socket(1, Planes));
    }

    private static Binding OtherSite(Clause clause) => Binding.Open(clause.Other, [.. Enumerable.Range(0, Sites)]);

    private static Binding OtherArrival(Clause clause) => Binding.Open(clause.Other, [.. Enumerable.Range(Sites, 3)]);

    /// <summary>Moving toward the store: three moments, each step forward. Its
    /// last moment is the turn. Through its socket it reads the moment after,
    /// and narrows its turn from the step that left it.</summary>
    private static Clause Approach()
    {
        var clause = Build(3, 2);
        var moving = new Wiring(Moving, clause.Arrival.Variable);
        moving.Add(1);
        moving.Add(2);
        var leaving = new Wiring(Step, clause.Site.Variable, OtherSite(clause), OtherArrival(clause));
        leaving.Add(2, 0, 0);
        var turned = new Wiring(Paths.TurnedBack, clause.Arrival.Variable, OtherArrival(clause));
        turned.Add(2, 0);
        clause.Engine.Add(moving).Add(leaving).Add(turned);
        return clause;
    }

    /// <summary>Then turned back: one moment after the turn, reached by a step
    /// back from the turning moment it reads through its socket.</summary>
    private static Clause Return()
    {
        var clause = Build(1, 0);
        var back = new Wiring(MovingBack, clause.Arrival.Variable);
        back.Add(0);
        var from = new Wiring(Step, OtherSite(clause), clause.Site.Variable, clause.Arrival.Variable);
        from.Add(0, 0, 0);
        var turned = new Wiring(Paths.TurnedBack, OtherArrival(clause), clause.Arrival.Variable);
        turned.Add(0, 0);
        clause.Engine.Add(back).Add(from).Add(turned);
        return clause;
    }

    /// <summary>Seen at site 1 after turning back.</summary>
    private static Clause SeenOnTheWayBack()
    {
        var clause = Return();
        clause.Site.Restrict(0, Values(1));
        return clause;
    }

    private static void Join(Clause approach, Clause back)
    {
        approach.Other.Plug(back.Edge);
        back.Other.Plug(approach.Edge);
    }

    /// <summary>The return continues this approach: its step back starts from
    /// the approach's own turning moment, and the approach reads that return.
    /// A comparison of positions, not of values.</summary>
    private static bool Continues(Clause back, Clause approach) =>
        back.Other.SamePosition(0, approach.Store, 2) && approach.Other.SamePosition(0, back.Store, 0);

    /// <summary>Steps the structures in the given order until no step rules anything out.</summary>
    private static void Settle(params Engine[] order)
    {
        for (var round = 0; round < 16; round++)
            if (!order.Select(engine => engine.Step()).ToArray().Any(step => step.Steps.Any(wiring => wiring.Changed.Any))) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    private static ulong[] Read(Clause clause) => [.. Enumerable.Range(0, clause.Store.Count).Select(clause.Site.Values)];

    /// <summary>Alone, the approach knows only that it went forward twice, so
    /// it may have started anywhere up to site 2. The return alone knows where
    /// it was seen and that it was going back; what it turned back from is
    /// not there, so its rows through the empty socket are skipped and say so.</summary>
    [Fact]
    public void EachClauseMakesPartialSenseAlone()
    {
        var approach = Approach();
        var back = SeenOnTheWayBack();

        Settle(approach.Engine);
        var skipped = back.Engine.Step().Steps.Select(step => step.Skipped.Any).ToArray();
        Settle(back.Engine);

        Assert.Equal([Values(0, 1, 2), Values(1, 2, 3), Values(2, 3, 4)], Read(approach));
        Assert.Equal(Values(Paths.Forward), approach.Arrival.Values(2));
        Assert.Equal((Values(1), Values(Paths.Back)), (back.Site.Values(0), back.Arrival.Values(0)));
        Assert.Equal([false, false, true, true], skipped);
    }

    /// <summary>Joined, the return's step back starts from the approach's own
    /// turn, so the approach learns where it turned and, from that, that it set
    /// out from home. It never reached the store. The return holds no copy of
    /// the turn. Narrowing only rules values out, so it ends the same whichever
    /// clause steps first.</summary>
    [Fact]
    public void TheReturnContinuesTheApproachesOwnTurn()
    {
        foreach (var approachFirst in new[] { true, false })
        {
            var approach = Approach();
            var back = SeenOnTheWayBack();
            Join(approach, back);

            Settle(approachFirst ? [approach.Engine, back.Engine] : [back.Engine, approach.Engine]);

            Assert.Equal([Values(0), Values(1), Values(2)], Read(approach));
            Assert.True(approach.Site.Of(4).None);
            Assert.Equal(1, back.Store.Count);
            Assert.True(Continues(back, approach));
        }
    }

    /// <summary>Two approaches built alike, equal in every bit. The return
    /// continues the first. Any number may read the return, so the second can
    /// narrow itself from it and come to look exactly like the first, but the
    /// return did not start from its turn: the values cannot tell them apart,
    /// and the positions do.</summary>
    [Fact]
    public void AnEqualLookingMoverIsNotTheSameSomething()
    {
        var first = Approach();
        var lookalike = Approach();
        var back = SeenOnTheWayBack();
        Join(first, back);
        lookalike.Other.Plug(back.Edge);

        Settle(first.Engine, lookalike.Engine, back.Engine);

        Assert.Equal(Read(first), Read(lookalike));
        Assert.True(Continues(back, first));
        Assert.False(Continues(back, lookalike));
        Assert.False(back.Other.SamePosition(0, lookalike.Store, 2));
    }

    /// <summary>The store is a site of the path, so arrival is possible: with
    /// nothing seen on the way back, the turn may be there. Only seeing the
    /// mover at the store establishes arrival, and then the way back starts
    /// from it.</summary>
    [Fact]
    public void AdmittingTheStoreMakesArrivalPossibleAndSeeingTheMoverThereEstablishesIt()
    {
        var approach = Approach();
        var back = Return();
        Join(approach, back);
        Settle(approach.Engine, back.Engine);
        Assert.Equal(Values(2, 3, 4), approach.Site.Values(2));
        Assert.Equal(Values(1, 2, 3), back.Site.Values(0));

        approach.Site.Restrict(2, Values(4));
        Settle(approach.Engine, back.Engine);

        Assert.Equal([Values(2), Values(3), Values(4)], Read(approach));
        Assert.Equal(Values(3), back.Site.Values(0));
    }

    /// <summary>The removal test. Cutting the lanes leaves each clause with its
    /// own partial sense. Cutting a step inside a clause loses what that step
    /// carried, and the lanes cannot make up for it.</summary>
    [Fact]
    public void TheLanesAreASeamAndTheStepsInsideAreNot()
    {
        var approach = Approach();
        var back = SeenOnTheWayBack();
        Join(approach, back);
        approach.Other.Unplug();
        back.Other.Unplug();
        Settle(approach.Engine, back.Engine);
        Assert.Equal([Values(0, 1, 2), Values(1, 2, 3), Values(2, 3, 4)], Read(approach));

        var cut = Approach();
        cut.Steps.Retire(0);
        var other = SeenOnTheWayBack();
        Join(cut, other);
        Settle(cut.Engine, other.Engine);
        Assert.Equal([Values(0, 1, 2, 3, 4), Values(1), Values(2)], Read(cut));
    }

    /// <summary>The clauses fused into one structure settle as the joined pair
    /// did. Saved and restored together, the return still continues the
    /// restored approach's own turn.</summary>
    [Fact]
    public void FusedOrSavedTheReturnStillContinuesTheSameTurn()
    {
        var approach = Approach();
        var back = SeenOnTheWayBack();
        Join(approach, back);
        var restored = Transport.LoadLocals(Transport.SaveLocals(approach.Engine, back.Engine));

        approach.Engine.Fuse(back.Engine);
        Settle(approach.Engine);
        Settle([.. restored.Select(local => local.Engine)]);

        Assert.Equal([Values(0), Values(1), Values(2)], Read(approach));
        var (restoredApproach, restoredBack) = (restored[0].Stores[0], restored[1].Stores[0]);
        Assert.Equal(Values(0), new Supports(restoredApproach, approach.Site.Field, [.. approach.Site.Planes]).Values(0));
        var previous = Assert.Single(restored[1].Engine.Sockets);
        Assert.True(previous.SamePosition(0, restoredApproach, 2));
        Assert.False(previous.SamePosition(0, approach.Store, 2));
        Assert.True(Assert.Single(restored[0].Engine.Sockets).SamePosition(0, restoredBack, 0));
    }
}
