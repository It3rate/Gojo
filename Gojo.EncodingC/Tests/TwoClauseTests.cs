using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>"Something was moving toward the store, then turned back", as two
/// local structures joined by one thin lane.
/// <para>Each clause holds the mover's distance to the store at a few moments,
/// as a unary count that starts open: more distance, more participation, and
/// nothing about it known until something rules values out. "Toward" is one
/// relation over consecutive moments, closer than the moment before. "Turned
/// back" is the reverse relation from the turning moment on, with the turn
/// read as happening short of the store. No path type is named: whether the
/// store was reached is a read of the turning moment's supports, open unless
/// something settles it.</para>
/// <para>The clauses share one moment, the turn, and nothing else. Each
/// exposes what it considers possible there and reads the other's through a
/// socket, narrowing only its own. Alone, each makes partial sense; joined,
/// they settle what neither could.</para>
/// <code>
/// approach: d0 ─closer→ d1 ─closer→ d2 ═╗ turn
/// return:                       r0 ═════╝ ─farther→ r1      r0 &gt; 0
/// </code></summary>
public sealed class TwoClauseTests
{
    private const int Far = 4;
    private static readonly Shape Distance = Shape.Unary(Far);
    private static readonly RelationLaw Closer = RelationLaw.FromPredicate([Distance, Distance], t => t[1] < t[0]);
    private static readonly RelationLaw Farther = RelationLaw.FromPredicate([Distance, Distance], t => t[1] > t[0]);
    private static readonly RelationLaw ShortOfTheStore = RelationLaw.FromPredicate([Distance], t => t[0] > 0);
    private static readonly RelationLaw AtTheStore = RelationLaw.FromPredicate([Distance], t => t[0] == 0);
    private static readonly RelationLaw Shared = Settling.Same(Distance);

    private sealed record Clause(Engine Engine, Supports Distance, Wiring Steps, Port Turn, Socket Peer);

    /// <summary>A clause of <paramref name="moments"/> moments, each related to
    /// the next by <paramref name="step"/>, with one-moment marks, exposing and
    /// reading the turning moment.</summary>
    private static Clause Build(int moments, int turn, RelationLaw step, params (RelationLaw Law, int Moment)[] marks)
    {
        var store = new Store(Far, moments);
        var distance = Supports.On(store, new Field(Distance, Enumerable.Range(0, Far).ToArray()));
        var engine = new Engine();
        var steps = new Wiring(step, distance.Variable, distance.Variable);
        for (var t = 0; t + 1 < moments; t++) steps.Add(t, t + 1);
        engine.Add(steps);
        foreach (var (law, moment) in marks)
        {
            var mark = new Wiring(law, distance.Variable);
            mark.Add(moment);
            engine.Add(mark);
        }
        var exposed = store.Publish([turn], [.. distance.Planes]);
        var peer = Socket.For(exposed);
        var link = new Wiring(Shared, distance.Variable, Binding.Open(peer, [.. Enumerable.Range(0, peer.PlaneCount)]));
        link.Add(turn, 0);
        engine.Add(link);
        return new Clause(engine, distance, steps, exposed, peer);
    }

    /// <summary>Moving toward the store, seen three away at the first of three moments.</summary>
    private static Clause Approach(params (RelationLaw, int)[] marks)
    {
        var clause = Build(3, 2, Closer, marks);
        clause.Distance.Restrict(0, Values(3));
        return clause;
    }

    /// <summary>Turned back: farther each moment from a turn short of the store.</summary>
    private static Clause Return() => Build(2, 0, Farther, (ShortOfTheStore, 0));

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

    private static ulong[] Read(Clause clause, int moments) => [.. Enumerable.Range(0, moments).Select(clause.Distance.Values)];

    [Fact]
    public void EachClauseMakesPartialSenseAlone()
    {
        var approach = Approach();
        var back = Return();

        Settle(approach.Engine);
        Settle(back.Engine);

        Assert.Equal([Values(3), Values(1, 2), Values(0, 1)], Read(approach, 3));
        Assert.Equal([Values(1, 2, 3), Values(2, 3, 4)], Read(back, 2));
    }

    /// <summary>Joined by the turn alone, the clauses settle where it turned
    /// and where the mover was between, and rule out that it reached the
    /// store. How far it went back stays open. Narrowing only rules values
    /// out, so it ends the same whichever clause steps first.</summary>
    [Fact]
    public void TheThinLaneSettlesWhatNeitherClauseCould()
    {
        foreach (var approachFirst in new[] { true, false })
        {
            var approach = Approach();
            var back = Return();
            Join(approach, back);

            Settle(approachFirst ? [approach.Engine, back.Engine] : [back.Engine, approach.Engine]);

            Assert.Equal([Values(3), Values(2), Values(1)], Read(approach, 3));
            Assert.Equal([Values(1), Values(2, 3, 4)], Read(back, 2));
            Assert.True(approach.Distance.Of(0).None);
        }
    }

    /// <summary>The removal test. Cutting the lane leaves each clause with its
    /// own partial sense. Cutting a row inside a clause loses what that row
    /// carried, and the lane cannot make up for it: the turn stays open.</summary>
    [Fact]
    public void TheLaneIsASeamAndTheRowsInsideAreNot()
    {
        var approach = Approach();
        var back = Return();
        Join(approach, back);
        approach.Peer.Unplug();
        back.Peer.Unplug();
        Settle(approach.Engine, back.Engine);
        Assert.Equal([Values(3), Values(1, 2), Values(0, 1)], Read(approach, 3));

        var cut = Approach();
        cut.Steps.Retire(0);
        var other = Return();
        Join(cut, other);
        Settle(cut.Engine, other.Engine);
        Assert.Equal(Values(1, 2, 3), cut.Distance.Values(2));
        Assert.Equal(Values(1, 2, 3), other.Distance.Values(0));
    }

    /// <summary>"Moving to the store, then turned back" short of it cannot both
    /// hold. Saying "to" settles the approach alone, and the lane carries the
    /// clash into both clauses, where it stays visible as a moment with nothing
    /// possible.</summary>
    [Fact]
    public void ToTheStoreThenTurningBackShortOfItContradicts()
    {
        var alone = Approach((AtTheStore, 2));
        Settle(alone.Engine);
        Assert.Equal([Values(3), Values(1, 2), Values(0)], Read(alone, 3));

        var approach = Approach((AtTheStore, 2));
        var back = Return();
        Join(approach, back);
        Settle(approach.Engine, back.Engine);

        Assert.Equal(0UL, approach.Distance.Values(2));
        Assert.Equal(0UL, back.Distance.Values(0));
    }

    /// <summary>The clauses fused into one structure settle as the joined pair
    /// did, and survive being saved and restored together.</summary>
    [Fact]
    public void FusedOrSavedTheClausesSettleTheSame()
    {
        var approach = Approach();
        var back = Return();
        Join(approach, back);
        var restored = Transport.LoadLocals(Transport.SaveLocals(approach.Engine, back.Engine));

        approach.Engine.Fuse(back.Engine);
        Settle(approach.Engine);
        Settle([.. restored.Select(local => local.Engine)]);

        Assert.Equal([Values(3), Values(2), Values(1)], Read(approach, 3));
        Assert.Equal([Values(1), Values(2, 3, 4)], Read(back, 2));
        var turn = new Supports(restored[0].Stores[0], approach.Distance.Field, [.. approach.Distance.Planes]);
        Assert.Equal(Values(1), turn.Values(2));
    }
}
