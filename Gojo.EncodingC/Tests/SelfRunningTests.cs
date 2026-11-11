using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Machines that run themselves. Each test builds structures, laws and
/// wirings, then only steps the engine: observing, narrowing, deciding, acting,
/// forgetting, predicting, judging and rewiring all happen as wirings run from
/// their tables, with no host code in the loop.</summary>
public sealed class SelfRunningTests
{
    private static readonly Field Count = new(Shape.Unary(2), 0, 1);

    /// <summary>A row takes from one of two suppliers. Which one is a reference
    /// held in the planes of the wiring's rows, so a law that reads two trust
    /// counts and writes that reference reroutes the flow to the more trusted
    /// supplier. Nothing in the host rewires anything. A reference written to
    /// nowhere is not an error: the row does not run and says so.</summary>
    [Fact]
    public void ALawRewiresARowAndFlowFollows()
    {
        // Store: 0 supplier A, 1 supplier B, 2 consumer. Trust: 0 for A, 1 for B.
        var (store, count) = Stores.Store(3, 3);
        count.Write(store, 0, 3);
        count.Write(store, 1, 3);
        var take = new Wiring(Flows.Move(3, 3), new Binding(store, 0, 1, 2), new Binding(store, 0, 1, 2));
        take.Add(0, 2);
        var (trust, level) = Stores.Store(2, 3);
        level.Write(trust, 0, 1);
        level.Write(trust, 1, 2);

        var from = take.ReferencePlanes(0);
        var choose = TransitionLaw.FromFunction(
            [Shape.Unary(3), Shape.Unary(3), Shape.Binary(1 << from.Count)],
            s => [s[0], s[1], s[0] >= s[1] ? 0 : 1],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var choosing = new Wiring(choose, new Binding(trust, 0, 1, 2), new Binding(trust, 0, 1, 2), new Binding(take.Rows, [.. from]));
        choosing.Add(0, 1, 0);
        var engine = new Engine().Add(choosing).Add(take);

        Assert.Equal(new[] { new[] { 0 }, new[] { 1 } }, engine.Waves());
        engine.Step();

        Assert.Equal(new[] { 1, 2 }, take.Row(0));
        Assert.Equal((3, 2, 1), (count.Read(store, 0), count.Read(store, 1), count.Read(store, 2)));

        level.Write(trust, 0, 3);
        engine.Step();

        Assert.Equal(new[] { 0, 2 }, take.Row(0));
        Assert.Equal((2, 2, 2), (count.Read(store, 0), count.Read(store, 1), count.Read(store, 2)));

        foreach (var plane in from) take.Rows.Write(plane, 0, true);
        Assert.True(take.Apply().Skipped[0]);
    }

    /// <summary>A wiring that records leaves what each row did in planes of its
    /// rows. An accumulator is then only a count and a law that reads the
    /// "changed" bit: the structure collects its own signal over time. Bits
    /// that are not a count show up the same way, where a law or a selection
    /// can find them.</summary>
    [Fact]
    public void WhatARowDidIsLeftInPlanesALawCanCount()
    {
        var (store, count) = Stores.Store(2, 2);
        count.Write(store, 0, 2);
        var move = new Wiring(Flows.Move(2, 2), new Binding(store, 0, 1), new Binding(store, 0, 1));
        move.Add(0, 1);
        var did = move.Record();

        var (meter, total) = Stores.Store(1, 3);
        var tally = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Unary(3)],
            s => [s[0], s[0] == 1 && s[1] < 3 ? s[1] + 1 : s[1]],
            new Accounting(true, new Unchanged(0), new NeverDecreases(1)));
        var counting = new Wiring(tally, new Binding(move.Rows, did.Changed), new Binding(meter, 0, 1, 2));
        counting.Add(0, 0);
        var engine = new Engine().Add(move).Add(counting);

        engine.Run(4);

        Assert.Equal(2, total.Read(meter, 0));
        Assert.False(move.Rows.Read(did.Changed, 0));

        store.Write(1, 0, true);
        engine.Step();

        Assert.True(move.Rows.Read(did.Unread, 0));
        Assert.Equal(2, total.Read(meter, 0));
    }

    /// <summary>A bit flips every step. One predictor expects a flip, another
    /// expects no change; each writes its guess into its own bit before the
    /// world moves. A judge then compares each guess with what happened and
    /// moves a small count of trust up or down. Prediction, error and trust
    /// are all planes and tables in one structure, run by the engine in
    /// declared order. Only the trust is kept, not the history.</summary>
    [Fact]
    public void AStructurePredictsJudgesItselfAndTrustFollows()
    {
        // Planes: 0 guess, 1 the bit, 2-4 trust. Positions: 0 expects a flip, 1 expects none, 2 the world.
        var mind = new Structure(5, 3);
        var trust = new Field(Shape.Unary(3), 2, 3, 4);
        trust.Write(mind, 0, 1);
        trust.Write(mind, 1, 1);

        TransitionLaw Guess(bool flips) => TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit], s => [s[0], flips ? 1 - s[0] : s[0]], new Accounting(true, new Unchanged(0)));
        var expectsFlip = new Wiring(Guess(true), new Binding(mind, 1), new Binding(mind, 0));
        expectsFlip.Add(2, 0);
        var expectsSame = new Wiring(Guess(false), new Binding(mind, 1), new Binding(mind, 0));
        expectsSame.Add(2, 1);
        var world = new Wiring(Flows.Toggle, new Binding(mind, 1));
        world.Add(2);
        var judge = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit, Shape.Unary(3)],
            s => [s[0], s[1], s[0] == s[1] ? (s[2] < 3 ? s[2] + 1 : 3) : (s[2] > 0 ? s[2] - 1 : 0)],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var judging = new Wiring(judge, new Binding(mind, 0), new Binding(mind, 1), new Binding(mind, 2, 3, 4));
        judging.Add(0, 2, 0);
        judging.Add(1, 2, 1);
        var engine = new Engine().Add(expectsFlip).Add(expectsSame).Add(world).Add(judging);

        engine.Run(3);

        Assert.Equal((3, 0), (trust.Read(mind, 0), trust.Read(mind, 1)));
        Assert.Equal(47, Transport.Measure(engine).Bytes);
    }

    /// <summary>Two units must reach a target that takes one per step, through a
    /// gate, within two steps. Nothing says "open the gate": a plan structure
    /// holds what is possible at three moments, all open, because none has
    /// been worked out. Each step the machine forgets the plan, observes the
    /// plant into the first moment, sets the wanted level at the last, and
    /// narrows through the move law read as a relation between moments. Two
    /// passes carry the goal back to now, where the gate has one value left.
    /// A law enacts it, and the unit moves. The goal reached back two steps to
    /// a choice that had to be made at once. Asked for what cannot be reached,
    /// the plan shows a contradiction, the gate stays as it was, and the next
    /// step starts clean. The whole machine is saved and restored as bits.</summary>
    [Fact]
    public void AGoalTwoStepsAwayOpensTheValveNow()
    {
        (Engine Engine, Structure Plant, Structure Plan) Build(int units)
        {
            // Plant: 0 source, 1 gate, 2 target. Planes 0-1 a count, plane 2 the gate.
            var plant = new Structure(3, 3);
            Count.Write(plant, 0, units);
            var wanted = new Structure(2, 1);
            Count.Write(wanted, 0, 2);

            // Plan: three moments of (source, gate, target). Planes 0-2 rule out counts 0, 1, 2; planes 3-4 rule out gate 0, 1.
            var plan = new Structure(5, 9);
            var counts = Binding.Open(plan, 0, 1, 2);
            var gates = Binding.Open(plan, 3, 4);

            var forgetCounts = new Wiring(Settling.Forget(3), new Binding(plan, 0, 1, 2));
            var forgetGates = new Wiring(Settling.Forget(2), new Binding(plan, 3, 4));
            for (var moment = 0; moment < 3; moment++)
            {
                forgetCounts.Add(3 * moment);
                forgetCounts.Add(3 * moment + 2);
                forgetGates.Add(3 * moment + 1);
            }
            var observe = new Wiring(Settling.Same(Count.Shape), new Binding(plant, 0, 1), counts);
            observe.Add(0, 0);
            observe.Add(2, 2);
            var aim = new Wiring(Settling.Same(Count.Shape), new Binding(wanted, 0, 1), counts);
            aim.Add(0, 8);
            var ahead = new Wiring(Flows.GatedMove(2, 2).AsRelation(), counts, gates, counts, counts, gates, counts);
            ahead.Add(0, 1, 2, 3, 4, 5);
            ahead.Add(3, 4, 5, 6, 7, 8);
            var act = new Wiring(Settling.Enact(Shape.Bit), new Binding(plan, 3, 4), new Binding(plant, 2));
            act.Add(1, 1);
            var flow = new Wiring(Flows.GatedMove(2, 2), new Binding(plant, 0, 1), new Binding(plant, 2), new Binding(plant, 0, 1));
            flow.Add(0, 1, 2);

            var engine = new Engine()
                .Add(forgetCounts).Add(forgetGates).Add(observe).Add(aim).Add(ahead).Add(ahead).Add(act).Add(flow);
            return (engine, plant, plan);
        }

        var (engine, plant, plan) = Build(units: 2);
        var saved = Transport.Save(engine, plant, plan);
        var (restored, copies) = Transport.Load(saved);

        engine.Step();

        Assert.True(plan.Read(3, 1));
        Assert.False(plan.Read(4, 1));
        Assert.True(plant.Read(2, 1));
        Assert.Equal((1, 1), (Count.Read(plant, 0), Count.Read(plant, 2)));

        engine.Step();
        var settled = engine.Step();

        Assert.Equal((0, 2), (Count.Read(plant, 0), Count.Read(plant, 2)));
        Assert.All(settled.Steps, step => Assert.True(step.Unadmitted.None));

        restored.Run(3);
        for (var plane = 0; plane < plant.PlaneCount; plane++) Assert.Equal(plant.Read(plane), copies[0].Read(plane));
        var size = Transport.Measure(engine, plant, plan);
        Assert.Equal(114, saved.Length);
        Assert.Equal((298L, 291L, 320L), (size.StructureBits, size.LawBits, size.WiringBits));

        var (stuck, starved, doubt) = Build(units: 1);
        var refused = stuck.Step();

        Assert.True(refused.Steps[5].Unadmitted[0]);
        Assert.True(doubt.Read(3, 1) && doubt.Read(4, 1));
        Assert.False(starved.Read(2, 1));
        Assert.Equal((1, 0), (Count.Read(starved, 0), Count.Read(starved, 2)));
    }

    /// <summary>A structure can expose what it considers possible, not only its
    /// bits. A reader narrows its own value from that, by reference. What the
    /// reader believes never rules anything out behind the port: knowledge
    /// crosses a boundary one way unless the owner chooses otherwise.</summary>
    [Fact]
    public void WhatAnotherStructureKnowsIsReadThroughAPortAndNeverNarrowedThere()
    {
        var owner = new Structure(1, 2);
        var known = Supports.On(owner, Field.Bit(0));
        owner.Write(0, 0, true);
        known.Observe(0);
        var shown = owner.Publish([0, 1], [.. known.Planes]);

        var reader = new Structure(1, 2);
        var mine = Supports.On(reader, Field.Bit(0));
        mine.Restrict(1, 0b01);
        var link = new Wiring(Settling.Same(Shape.Bit), Binding.Open(shown, 0, 1), mine.Variable);
        link.Add(0, 0);
        link.Add(1, 1);

        link.Apply();

        Assert.Equal(0b10UL, mine.Values(0));
        Assert.Equal(0b11UL, known.Values(1));
        Assert.Equal((false, true), (link.Writes(0), link.Writes(1)));
    }

    /// <summary>Two bits, each open, that are known to be equal. Neither bit's
    /// own planes can say that, since each alone may still be 0 or 1. The link
    /// is a third value, the pair, with two of its four values ruled out, and
    /// a relation from the pair to each bit. Seeing one bit then settles the
    /// other through the pair.</summary>
    [Fact]
    public void ValuesThatVaryTogetherAreOneValueWithTwoReads()
    {
        // Positions: 0 first bit, 1 second bit, 2 the pair. Planes 0-1 rule out a bit's 0, 1; planes 2-5 rule out pair values.
        var linked = new Structure(6, 3);
        var bit = Binding.Open(linked, 0, 1);
        var pair = Binding.Open(linked, 2, 3, 4, 5);
        var first = new Wiring(RelationLaw.FromPredicate([Shape.Binary(4), Shape.Bit], t => t[1] == (t[0] & 1)), pair, bit);
        first.Add(2, 0);
        var second = new Wiring(RelationLaw.FromPredicate([Shape.Binary(4), Shape.Bit], t => t[1] == t[0] >> 1), pair, bit);
        second.Add(2, 1);
        var engine = new Engine().Add(first).Add(second);
        linked.Write(3, 2, true);
        linked.Write(4, 2, true);

        engine.Step();
        Assert.Equal((null, null), (first.Value(0, 1), second.Value(0, 1)));

        linked.Write(0, 0, true);
        engine.Step();

        Assert.Equal(3, first.Value(0, 0));
        Assert.Equal(1, second.Value(0, 1));
    }
}
