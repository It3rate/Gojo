using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>The oscillator family (ops_claude/GojoOperations-2026-11.md, §9).
/// Position is a count on a track and the landmark is a count on the same
/// track; velocity is a count against a rest count. The pull and the drift are
/// balances written over the count they change: the kick v' = v − m·(x − L),
/// the drift x' = x + (v − R). Nothing is signed: which side of the landmark
/// the mover is on is read by comparing counts, as Sides does.
/// <para>Step order is part of the structure: in declared order the orbit
/// closes; taken together it spirals out; and taken together with the kick and
/// the drift waiting on a toggling turn bit, it is the declared order again, at
/// two engine steps a step. At rung 0 the events the orbit passes through,
/// turning back and crossing, are the orientation generator J run backward,
/// and J is the face flip or the reorientation chosen by the face.</para></summary>
public sealed class OscillatorTests
{
    private const int Track = 8, Mark = 4;

    private static readonly Plane Here = Plane.Of(1, 0);

    private static readonly Shape Count = Shape.Unary(Track);

    private static Field Add(Store store, int value)
    {
        var field = new Field(Count, [.. Enumerable.Range(0, Track).Select(_ => store.AddPlane())]);
        field.Write(store, 0, value);
        return field;
    }

    /// <summary>The kick, v' = v − m·(x − L), as a balance over v, L, x and
    /// v', written over v; and the drift, x' = x + v − R, written over x.</summary>
    private static TransitionLaw Kick(int m) =>
        Operations.Bind(Operations.Balance([Count, Count, Count, Count], 1, m, -m, -1), [Count, Count, Count], [0, 1, 2, -1], [-1, -1, -1, 0]);

    private static readonly TransitionLaw Drift =
        Operations.Bind(Operations.Balance([Count, Count, Count, Count], 1, 1, -1, -1), [Count, Count, Count], [0, 1, 2, -1], [-1, -1, -1, 0]);

    private sealed record Mover(Store Store, Field X, Field V, Field Landmark, Field Rest, int Turn)
    {
        /// <summary>Displacement from the landmark and velocity against rest.</summary>
        public (int X, int V) Read() =>
            (X.Read(Store, 0)!.Value - Landmark.Read(Store, 0)!.Value, V.Read(Store, 0)!.Value - Rest.Read(Store, 0)!.Value);
    }

    private static Mover Start(int x, int v)
    {
        var store = new Store(0, 1);
        return new Mover(store, Add(store, Mark + x), Add(store, Mark + v), Add(store, Mark), Add(store, Mark), store.AddPlane());
    }

    private static Wiring KickOf(Mover o, int m, params Binding[] gates) =>
        Tension.Wire(Kick(m), [o.V.On(o.Store), o.Landmark.On(o.Store), o.X.On(o.Store)], Here, gates);

    private static Wiring DriftOf(Mover o, params Binding[] gates) =>
        Tension.Wire(Drift, [o.X.On(o.Store), o.V.On(o.Store), o.Rest.On(o.Store)], Here, gates);

    /// <summary>Kick then drift, in declared order, from a displacement of 2
    /// at rest.</summary>
    private static (Mover Mover, Engine Engine) InOrder(int m = 1, int x = 2, int v = 0)
    {
        var o = Start(x, v);
        var engine = new Engine();
        if (m != 0) engine.Add(KickOf(o, m));
        return (o, engine.Add(DriftOf(o)));
    }

    private static List<(int X, int V)> Run(Mover o, Func<EngineStep> step, int steps)
    {
        var seen = new List<(int X, int V)> { o.Read() };
        for (var i = 0; i < steps; i++)
        {
            step();
            seen.Add(o.Read());
        }
        return seen;
    }

    /// <summary>In declared order the drift reads the velocity the kick just
    /// wrote: the orbit closes after six steps and keeps v² + x² − x·v, the
    /// step's exact shadow energy. Taken together, the drift reads the old
    /// velocity: the same start leaves the orbit at once and grows until the
    /// track has no room for the next drift, which holds at the end of the
    /// track with the drift's balance unmet.</summary>
    [Fact]
    public void InOrderTheOrbitClosesAndTogetherItSpirals()
    {
        var (o, engine) = InOrder();
        var orbit = Run(o, engine.Step, 6);
        Assert.Equal([(2, 0), (0, -2), (-2, -2), (-2, 0), (0, 2), (2, 2), (2, 0)], orbit);
        Assert.All(orbit, s => Assert.Equal(4, s.V * s.V + s.X * s.X - s.X * s.V));

        var t = Start(2, 0);
        var together = new Engine().Add(KickOf(t, 1)).Add(DriftOf(t));
        var spiral = Run(t, together.StepTogether, 3);
        Assert.Equal([(2, 0), (2, -2), (0, -4), (-4, -4)], spiral);
        together.StepTogether();
        Assert.Equal((-4, 0), t.Read());
    }

    /// <summary>Kick waits on the turn bit's flipped view and drift on the bit,
    /// and a toggle flips it every step. Taken together, exactly one of them
    /// runs each engine step, so every second engine step is a step of the
    /// declared order: the order is saved in the wiring, not chosen by the host.</summary>
    [Fact]
    public void ATurnBitMakesTheOrderStructure()
    {
        var o = Start(2, 0);
        var turn = new Binding(o.Store, o.Turn);
        var engine = new Engine()
            .Add(Tension.Wire(Flows.Toggle, [turn], Here))
            .Add(KickOf(o, 1, new Binding(new Flipped(o.Store, o.Turn), o.Turn)))
            .Add(DriftOf(o, turn));

        var seen = Run(o, engine.StepTogether, 12);
        var (mover, inOrder) = InOrder();
        Assert.Equal(Run(mover, inOrder.Step, 6), seen.Where((_, i) => i % 2 == 0));
    }

    /// <summary>At rung 0 the orbit is read only as sides: turned back where the
    /// velocity is at rest, crossed where the position is at the landmark, and
    /// open between. Turning back is read on the position's face F, crossing on
    /// the velocity's face I, each with its side. The events run (F, +),
    /// (I, −), (F, −), (I, +): each is J of the next, where J is the face flip
    /// from F and the reorientation from I. J twice reverses every word, J four
    /// times is the identity, and none of the three involutions is J.</summary>
    [Fact]
    public void TheEventsAreTheGeneratorRunBackward()
    {
        static OdvWord J(OdvWord w) => w.PositiveLeft ? w.FaceFlip() : w.Reorient();
        var words = Enumerable.Range(0, 8).Select(c => new OdvWord((byte)c)).ToArray();
        Assert.All(words, w => Assert.Equal(w.Reverse(), J(J(w))));
        Assert.All(words, w => Assert.Equal(w, J(J(J(J(w))))));
        Assert.Contains(words, w => J(w) != w.Reverse());
        Assert.Contains(words, w => J(w) != w.FaceFlip());
        Assert.Contains(words, w => J(w) != w.Reorient());

        var side = Sides.Compare(Track);
        (bool At, bool Negative) Read(int count)
        {
            var mask = side.Project(2, [1UL << count, 1UL << Mark, (1UL << Sides.Word.Size) - 1]);
            return ((mask & (1UL << Sides.After | 1UL << Sides.Before)) == 0, (mask & 1UL << Sides.Before) != 0);
        }

        var (o, engine) = InOrder();
        var events = new List<OdvWord>();
        for (var i = 0; i < 12; i++)
        {
            var (x, v) = (Read(o.X.Read(o.Store, 0)!.Value), Read(o.V.Read(o.Store, 0)!.Value));
            if (v.At) events.Add(OdvWord.Of(positiveLeft: true, x.Negative, detectable: true));
            else if (x.At) events.Add(OdvWord.Of(positiveLeft: false, v.Negative, detectable: true));
            engine.Step();
        }

        Assert.Equal(8, events.Count);
        Assert.Equal(OdvWord.Of(true, negative: false, true), events[0]);
        for (var i = 0; i + 1 < events.Count; i++) Assert.Equal(events[i], J(events[i + 1]));
    }

    /// <summary>One family, two orders and three signs. First order, x' = x −
    /// m·x/2 rounded by a residue bit: m = +1 relaxes toward the landmark,
    /// halving each step, so equal ratios take equal times (Napier); m = 0 has
    /// no pull and holds; m = −1 compounds until the track has no room. Second
    /// order, kick then drift: m = +1 oscillates, m = 0 coasts, the distance
    /// growing by the velocity each step (rate times time), and m = −1 runs
    /// away, 0, 1, 3, until neither kick nor drift has room and both hold.</summary>
    [Fact]
    public void OrderAndSignAreOneFamily()
    {
        List<int> First(int m, int x0, int steps)
        {
            var store = new Store(0, 1);
            var x = Add(store, x0);
            var engine = new Engine();
            if (m != 0)
                engine.Add(Tension.Wire(Operations.Bind(Operations.Balance([Count, Count, Shape.Bit], 2 - m, -2, -m), [Count],
                    [0, -1, -1], [-1, 0, -1]), [x.On(store)], Here));
            var seen = new List<int> { x.Read(store, 0)!.Value };
            for (var i = 0; i < steps; i++)
            {
                if (engine.Wirings.Count > 0) engine.Step();
                seen.Add(x.Read(store, 0)!.Value);
            }
            return seen;
        }

        Assert.Equal([8, 4, 2, 1, 0, 0], First(+1, 8, 5));
        Assert.Equal([5, 5, 5], First(0, 5, 2));
        Assert.Equal([1, 2, 3, 5, 8, 8], First(-1, 1, 5));

        var (osc, oscEngine) = InOrder(m: +1);
        Assert.Equal((2, 0), Run(osc, oscEngine.Step, 6)[^1]);

        var (coast, coastEngine) = InOrder(m: 0, x: -4, v: 2);
        Assert.Equal([(-4, 2), (-2, 2), (0, 2), (2, 2), (4, 2)], Run(coast, coastEngine.Step, 4));

        var (away, awayEngine) = InOrder(m: -1, x: 0, v: 1);
        Assert.Equal([(0, 1), (1, 1), (3, 2), (3, 2)], Run(away, awayEngine.Step, 3));
    }

    /// <summary>Saved mid-orbit and loaded, the machine steps to the same bits
    /// as the one that kept running: the laws, their order and the landmark
    /// all travel with it.</summary>
    [Fact]
    public void ASavedOscillatorStepsToTheSameBits()
    {
        var (o, engine) = InOrder();
        engine.Run(2);
        var (loaded, stores) = Transport.Load(Transport.Save(engine, o.Store));
        engine.Run(3);
        loaded.Run(3);
        Assert.Equal((2, 2), o.Read());
        Assert.Equal(o.Read(), (o with { Store = stores[0] }).Read());
        Assert.Equal(Transport.Save(engine, o.Store), Transport.Save(loaded, stores[0]));
    }
}
