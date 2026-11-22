using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>"Something was moving toward the store, then turned back", as two
/// local structures sharing one path definition and joined by one thin lane.
/// <para>The goal defines the path: home, its origin and lower end, through
/// three sites to the store, on one orientation. "Toward the store" is not a
/// distance; it is the direction of the definition. Each clause is a use of
/// that path: at each moment the mover's site is an open role, and each step
/// has a direction, forward, staying or back, relative to the path. The
/// approach's steps go forward. The return holds "turned back", a change of
/// step direction from forward to back, across the turning moment.</para>
/// <para>The clauses share one moment and nothing else: the mover's site there
/// and the direction it arrived in. Each exposes what it considers possible at
/// that moment and reads the other's through a socket, narrowing only its own.
/// Admitting the store as a site makes arrival possible; only an observation
/// there establishes it.</para>
/// <code>
/// path:      home 0 → 1 → 2 → 3 → store 4
/// approach:  m0 ─fwd→ m1 ─fwd→ m2 ═╗ the turn: site and arrival
/// return:                      r0 ═╝ ─back→ r1        forward then back
/// </code></summary>
public sealed class TwoClauseTests
{
    private const int Sites = 5;
    private static readonly Shape Site = Shape.Binary(Sites);
    private static readonly (int, int)[] Road = [(0, 1), (1, 2), (2, 3), (3, 4)];
    private static readonly RelationLaw Step = Paths.Step(Sites, Road);
    private static readonly RelationLaw Moving = RelationLaw.FromPredicate([Paths.Direction], t => t[0] == Paths.Forward);

    private sealed record Clause(Engine Engine, Supports Site, Supports Arrival, Wiring Steps, Port Turn, Socket Peer);

    /// <summary>A use of the path over <paramref name="moments"/> moments: a
    /// site per moment, and the direction of the step that arrived there,
    /// exposing and reading the turning moment.</summary>
    private static Clause Build(int moments, int turn)
    {
        var store = new Store(Site.Bits + Paths.Direction.Bits, moments);
        var site = Supports.On(store, new Field(Site, 0, 1, 2));
        var arrival = Supports.On(store, new Field(Paths.Direction, 3, 4));
        var engine = new Engine();
        var steps = new Wiring(Step, site.Variable, site.Variable, arrival.Variable);
        for (var t = 1; t < moments; t++) steps.Add(t - 1, t, t);
        engine.Add(steps);
        var exposed = store.Publish([turn], [.. site.Planes, .. arrival.Planes]);
        var peer = Socket.For(exposed);
        var sameSite = new Wiring(Settling.Same(Site), site.Variable, Binding.Open(peer, [.. Enumerable.Range(0, Sites)]));
        sameSite.Add(turn, 0);
        var sameArrival = new Wiring(Settling.Same(Paths.Direction), arrival.Variable, Binding.Open(peer, [.. Enumerable.Range(Sites, 3)]));
        sameArrival.Add(turn, 0);
        engine.Add(sameSite).Add(sameArrival);
        return new Clause(engine, site, arrival, steps, exposed, peer);
    }

    /// <summary>Moving toward the store: three moments, each step forward.</summary>
    private static Clause Approach()
    {
        var clause = Build(3, 2);
        var moving = new Wiring(Moving, clause.Arrival.Variable);
        moving.Add(1);
        moving.Add(2);
        clause.Engine.Add(moving);
        return clause;
    }

    /// <summary>Then turned back: from the turning moment, one more step, the
    /// arrival there forward and the step after it back.</summary>
    private static Clause Return()
    {
        var clause = Build(2, 0);
        var turned = new Wiring(Paths.TurnedBack, clause.Arrival.Variable, clause.Arrival.Variable);
        turned.Add(0, 1);
        clause.Engine.Add(turned);
        return clause;
    }

    /// <summary>Seen at site 1 after turning back.</summary>
    private static Clause SeenOnTheWayBack()
    {
        var clause = Return();
        clause.Site.Restrict(1, Values(1));
        return clause;
    }

    private static void Join(Clause a, Clause b)
    {
        a.Peer.Plug(b.Turn);
        b.Peer.Plug(a.Turn);
    }

    /// <summary>Steps the structures in the given order until no step rules anything out.</summary>
    private static void Settle(params Engine[] order)
    {
        for (var round = 0; round < 16; round++)
            if (!order.Select(engine => engine.Step()).ToArray().Any(step => step.Steps.Any(wiring => wiring.Changed.Any))) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    private static ulong[] Read(Clause clause, int moments) => [.. Enumerable.Range(0, moments).Select(clause.Site.Values)];

    private static ulong AnyDirection => Values(Paths.Back, Paths.Stay, Paths.Forward);

    /// <summary>Alone, the approach knows only that it went forward twice, so
    /// it may have started anywhere up to site 2. The return, seen at site 1
    /// after turning back, settles its own turn at site 2 and that the mover
    /// arrived there going forward.</summary>
    [Fact]
    public void EachClauseMakesPartialSenseAlone()
    {
        var approach = Approach();
        var back = SeenOnTheWayBack();

        Settle(approach.Engine);
        Settle(back.Engine);

        Assert.Equal([Values(0, 1, 2), Values(1, 2, 3), Values(2, 3, 4)], Read(approach, 3));
        Assert.Equal((AnyDirection, Values(Paths.Forward)), (approach.Arrival.Values(0), approach.Arrival.Values(2)));
        Assert.Equal([Values(2), Values(1)], Read(back, 2));
        Assert.Equal((Values(Paths.Forward), Values(Paths.Back)), (back.Arrival.Values(0), back.Arrival.Values(1)));
    }

    /// <summary>Joined by the turn alone, the approach learns where it turned,
    /// and from that where it was before: it set out from home. It never
    /// reached the store. Narrowing only rules values out, so it ends the same
    /// whichever clause steps first.</summary>
    [Fact]
    public void TheThinLaneSettlesWhereTheMoverSetOutFrom()
    {
        foreach (var approachFirst in new[] { true, false })
        {
            var approach = Approach();
            var back = SeenOnTheWayBack();
            Join(approach, back);

            Settle(approachFirst ? [approach.Engine, back.Engine] : [back.Engine, approach.Engine]);

            Assert.Equal([Values(0), Values(1), Values(2)], Read(approach, 3));
            Assert.True(approach.Site.Of(4).None);
            Assert.Equal([Values(2), Values(1)], Read(back, 2));
        }
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
        Assert.Equal(Values(1, 2, 3), back.Site.Values(1));

        approach.Site.Restrict(2, Values(4));
        Settle(approach.Engine, back.Engine);

        Assert.Equal([Values(2), Values(3), Values(4)], Read(approach, 3));
        Assert.Equal([Values(4), Values(3)], Read(back, 2));
    }

    /// <summary>The removal test. Cutting the lane leaves each clause with its
    /// own partial sense. Cutting a step inside a clause loses what that step
    /// carried, and the lane cannot make up for it.</summary>
    [Fact]
    public void TheLaneIsASeamAndTheStepsInsideAreNot()
    {
        var approach = Approach();
        var back = SeenOnTheWayBack();
        Join(approach, back);
        approach.Peer.Unplug();
        back.Peer.Unplug();
        Settle(approach.Engine, back.Engine);
        Assert.Equal([Values(0, 1, 2), Values(1, 2, 3), Values(2, 3, 4)], Read(approach, 3));

        var cut = Approach();
        cut.Steps.Retire(0);
        var other = SeenOnTheWayBack();
        Join(cut, other);
        Settle(cut.Engine, other.Engine);
        Assert.Equal([Values(0, 1, 2, 3, 4), Values(1), Values(2)], Read(cut, 3));
    }

    /// <summary>The clauses fused into one structure settle as the joined pair
    /// did, and survive being saved and restored together.</summary>
    [Fact]
    public void FusedOrSavedTheClausesSettleTheSame()
    {
        var approach = Approach();
        var back = SeenOnTheWayBack();
        Join(approach, back);
        var restored = Transport.LoadLocals(Transport.SaveLocals(approach.Engine, back.Engine));

        approach.Engine.Fuse(back.Engine);
        Settle(approach.Engine);
        Settle([.. restored.Select(local => local.Engine)]);

        Assert.Equal([Values(0), Values(1), Values(2)], Read(approach, 3));
        Assert.Equal([Values(2), Values(1)], Read(back, 2));
        var setOut = new Supports(restored[0].Stores[0], approach.Site.Field, [.. approach.Site.Planes]);
        Assert.Equal(Values(0), setOut.Values(0));
    }
}
