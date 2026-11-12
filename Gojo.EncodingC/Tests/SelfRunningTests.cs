using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Machines that run themselves. Each test builds structures, laws and
/// wirings, then only steps the engine: observing, narrowing, supposing,
/// deciding, acting, forgetting, predicting, judging, rewiring, growing and
/// pruning all happen as wirings run from their tables, with no host code in
/// the loop.</summary>
public sealed class SelfRunningTests
{
    private static readonly Field Count = new(Shape.Unary(2), 0, 1);

    /// <summary>What is aimed for: the wanted count lowered by how far the aim has given way.</summary>
    private static readonly RelationLaw Lowered = RelationLaw.FromPredicate(
        [Count.Shape, Count.Shape, Count.Shape], t => t[2] == (t[0] > t[1] ? t[0] - t[1] : 0));

    /// <summary>Adds one to a count whenever a bit is set.</summary>
    private static readonly TransitionLaw Tally = TransitionLaw.FromFunction(
        [Shape.Bit, Count.Shape],
        s => [s[0], s[0] == 1 && s[1] < 2 ? s[1] + 1 : s[1]],
        new Accounting(true, new Unchanged(0), new NeverDecreases(1)));

    /// <summary>Copies a bit.</summary>
    private static readonly TransitionLaw Copy = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit], s => [s[0], s[0]], new Accounting(true, new Unchanged(0)));

    /// <summary>Sets a bit once another is set, and leaves it otherwise.</summary>
    private static readonly TransitionLaw Latch = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit], s => [s[0], s[0] == 1 ? 1 : s[1]], new Accounting(true, new Unchanged(0), new NeverDecreases(1)));

    // ---- Topology as state ----

    /// <summary>A row takes from one of two suppliers. Which one is a reference
    /// held in the planes of the wiring's rows, so a law that reads two trust
    /// counts and writes that reference reroutes the flow to the more trusted
    /// supplier. Nothing in the host rewires anything. A reference written to
    /// nowhere is not an error: the row does not run and says so.</summary>
    [Fact]
    public void ALawRewiresARowAndFlowFollows()
    {
        // Store: 0 supplier A, 1 supplier B, 2 consumer. Trust: 0 for A, 1 for B.
        var (store, count) = Make.Counts(3, 3);
        count.Write(store, 0, 3);
        count.Write(store, 1, 3);
        var take = new Wiring(Flows.Move(3, 3), new Binding(store, 0, 1, 2), new Binding(store, 0, 1, 2));
        take.Add(0, 2);
        var (trust, level) = Make.Counts(2, 3);
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

    /// <summary>A source feeds one target that has room for a single unit. A
    /// second route is wired and dormant: a row that exists as room, not in
    /// use. A law reads the first target and, once it is full, sets the
    /// dormant row's bit of life. The structure grew a connection because flow
    /// was blocked, by recruiting what was already there. A saved machine
    /// keeps its dormant rows.</summary>
    [Fact]
    public void ALawBringsADormantRowToLifeWhenFlowIsBlocked()
    {
        // Store: 0 source, 1 first target, 2 second target. A target holds one unit, in plane 0.
        var (store, count) = Make.Counts(3, 3);
        count.Write(store, 0, 3);
        var routes = new Wiring(Flows.Move(3, 1), new Binding(store, 0, 1, 2), new Binding(store, 0));
        routes.Add(0, 1);
        routes.AddSpare(0, 2);
        var grow = new Wiring(Latch, new Binding(store, 0), new Binding(routes.Rows.Life, 0));
        grow.Add(1, 1);
        var engine = new Engine().Add(routes).Add(grow);
        var (restored, copies) = Transport.Load(Transport.Save(engine, store));

        Assert.Equal(Plane.Of(2, 0), routes.Live);
        Assert.Equal(new[] { new[] { 0 }, new[] { 1 } }, engine.Waves());

        engine.Step();
        Assert.Equal(Plane.Ones(2), routes.Live);
        Assert.Equal((2, 1, 0), (count.Read(store, 0), count.Read(store, 1), count.Read(store, 2)));

        engine.Step();
        Assert.Equal((1, 1, 1), (count.Read(store, 0), count.Read(store, 1), count.Read(store, 2)));

        restored.Run(2);
        Assert.Equal(store.Read(0), copies[0].Read(0));
    }

    /// <summary>A unit moves along a chain, one hop per step. A watcher copies
    /// one bit of the chain into its own. Which bit is a reference in the
    /// planes of the watcher's row, and a law moves it on whenever the watcher
    /// sees nothing. Attention is state: looking somewhere is a reference, and
    /// one focus leads to the next by a law writing it.</summary>
    [Fact]
    public void AWatcherMovesItsOwnAttentionUntilItFindsTheUnit()
    {
        var chain = new Store(2, 5);
        chain.Write(0, 0, true);
        chain.Write(1, 1, true);
        chain.Write(1, 3, true);
        var flow = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        flow.Add(2, 3, 4);
        flow.Add(0, 1, 2);

        var watcher = new Store(1, 1);
        var look = new Wiring(Copy, new Binding(chain, 0), new Binding(watcher, 0));
        look.Add(0, 0);
        var where = look.ReferencePlanes(0);
        var moveOn = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Binary(1 << where.Count)],
            s => [s[0], s[0] == 0 && s[1] < 4 ? s[1] + 2 : s[1]],
            new Accounting(true, new Unchanged(0)));
        var follow = new Wiring(moveOn, new Binding(watcher, 0), new Binding(look.Rows, [.. where]));
        follow.Add(0, 0);
        var engine = new Engine().Add(flow).Add(look).Add(follow);

        engine.Step();
        Assert.Equal(2, look.Row(0)[0]);
        Assert.False(watcher.Read(0, 0));

        engine.Run(2);
        Assert.Equal(4, look.Row(0)[0]);
        Assert.True(watcher.Read(0, 0));
    }

    /// <summary>A wiring that records leaves what each row did in planes of its
    /// rows. An accumulator is then only a count and a law that reads the
    /// "changed" bit: the structure collects its own signal over time. Bits
    /// that are not a count show up the same way, where a law or a selection
    /// can find them.</summary>
    [Fact]
    public void WhatARowDidIsLeftInPlanesALawCanCount()
    {
        var (store, count) = Make.Counts(2, 2);
        count.Write(store, 0, 2);
        var move = new Wiring(Flows.Move(2, 2), new Binding(store, 0, 1), new Binding(store, 0, 1));
        move.Add(0, 1);
        var did = move.Record();

        var meter = new Store(2, 1);
        var counting = new Wiring(Tally, new Binding(move.Rows, did.Changed), new Binding(meter, 0, 1));
        counting.Add(0, 0);
        var engine = new Engine().Add(move).Add(counting);

        engine.Run(4);

        Assert.Equal(2, Count.Read(meter, 0));
        Assert.False(move.Rows.Read(did.Changed, 0));

        store.Write(1, 0, true);
        engine.Step();

        Assert.True(move.Rows.Read(did.Unread, 0));
        Assert.Equal(2, Count.Read(meter, 0));
    }

    // ---- Prediction, trust and pruning ----

    /// <summary>A bit flips every step. One predictor expects a flip, another
    /// expects no change; each writes its guess into its own bit before the
    /// world moves. A judge then compares each guess with what happened and
    /// moves a small count of trust up or down. Prediction, error and trust
    /// are all planes and tables in one structure, run by the engine in
    /// declared order. Only the trust is kept, not the history.</summary>
    [Fact]
    public void AStructurePredictsJudgesItselfAndTrustFollows()
    {
        var (engine, mind, trust, _, _) = Mind();

        engine.Run(3);

        Assert.Equal((3, 0), (trust.Read(mind, 0), trust.Read(mind, 1)));
        Assert.Equal(47, Transport.Measure(engine).Bytes);
    }

    /// <summary>The same machine with one more small law: a predictor whose
    /// trust has run out is retired, and so is the row that judged it. The law
    /// reads the trust count and clears the row's bit of life. The structure
    /// prunes itself, and what is retired never runs again.</summary>
    [Fact]
    public void APredictorWhoseTrustRunsOutIsRetiredByTheStructure()
    {
        var (engine, mind, trust, expectsSame, judging) = Mind();
        var drop = TransitionLaw.FromFunction(
            [Shape.Unary(3), Shape.Bit],
            s => [s[0], s[0] == 0 ? 0 : s[1]],
            new Accounting(true, new Unchanged(0), new NeverIncreases(1)));
        var dropGuess = new Wiring(drop, new Binding(mind, 2, 3, 4), new Binding(expectsSame.Rows.Life, 0));
        dropGuess.Add(1, 0);
        var dropJudge = new Wiring(drop, new Binding(mind, 2, 3, 4), new Binding(judging.Rows.Life, 0));
        dropJudge.Add(1, 1);
        engine.Add(dropGuess).Add(dropJudge);

        engine.Run(3);

        Assert.True(expectsSame.Rows.IsRetired(0));
        Assert.Equal(Plane.Of(2, 0), judging.Live);
        Assert.Equal((3, 0), (trust.Read(mind, 0), trust.Read(mind, 1)));
    }

    // ---- Reaching a goal ----

    /// <summary>Two units must reach a target that takes one per step, through a
    /// gate, within two steps. Nothing says "open the gate": a plan, one store,
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
        var (engine, plant, plan, _) = Planner(units: 2);
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
        Assert.Equal((298L, 291L, 320L), (size.StoreBits, size.LawBits, size.WiringBits));

        var (stuck, starved, doubt, _) = Planner(units: 1);
        var refused = stuck.Step();

        Assert.True(refused.Steps[5].Unadmitted[0]);
        Assert.True(doubt.Read(3, 1) && doubt.Read(4, 1));
        Assert.False(starved.Read(2, 1));
        Assert.Equal((1, 0), (Count.Read(starved, 0), Count.Read(starved, 2)));
    }

    /// <summary>One unit, and a goal of two within one step: it cannot be done.
    /// The plan's contradiction is left in a plane of its rows, and a law that
    /// reads that bit adds one to how far the aim has given way. Next step the
    /// machine aims for one, which it can reach, and does. The error was used
    /// to change the goal, not only reported. The aim gives way no further
    /// once it can be met.</summary>
    [Fact]
    public void AGoalThatCannotBeReachedGivesWayUntilItCan()
    {
        var (engine, plant, _, ease) = Planner(units: 1, moments: 2, givesWay: true);

        engine.Step();

        Assert.Equal(1, Count.Read(ease, 0));
        Assert.Equal((1, 0), (Count.Read(plant, 0), Count.Read(plant, 2)));

        engine.Run(2);

        Assert.Equal((0, 1), (Count.Read(plant, 0), Count.Read(plant, 2)));
        Assert.Equal(1, Count.Read(ease, 0));
    }

    /// <summary>Narrowing settles what is forced and nothing more. With one unit
    /// and a goal of one within two steps, opening the gate is the only way,
    /// but no single row can see that: each row finds both gate values
    /// possible, because what rules one out lies in the other row. So the gate
    /// is left open and nothing is enacted. This is sound and incomplete.</summary>
    [Fact]
    public void NarrowingSettlesOnlyWhatOneRowCanSee()
    {
        var (engine, plant, plan, ease) = Planner(units: 1, moments: 3, givesWay: true);
        Count.Write(ease, 0, 1);

        var step = engine.Step();

        Assert.False(plan.Read(3, 1) || plan.Read(4, 1));
        Assert.True(step.Steps[5].Ambiguous[0]);
        Assert.Equal((1, 0), (Count.Read(plant, 0), Count.Read(plant, 2)));
    }

    /// <summary>The same case, settled by a supposition made of structure. A
    /// second plan, the probe, is wired like the first with one thing more: it
    /// supposes the gate stays shut. Under that supposition its rows reach a
    /// contradiction, and a law that reads that bit rules "shut" out in the
    /// real plan, where the gate now has one value left. The idea was wrong,
    /// and being wrong is what settled the choice. Once the unit has arrived
    /// the probe no longer contradicts and nothing is ruled out. A try costs a
    /// second plan, so what can be tried is bounded by room.</summary>
    [Fact]
    public void ASuppositionThatContradictsRulesTheChoiceOut()
    {
        var world = World(units: 1);
        Count.Write(world.Ease, 0, 1);
        var plan = Think(world, moments: 3, givesWay: true);
        var probe = Think(world, moments: 3, givesWay: true);

        var shut = new Store(1, 1);
        var suppose = new Wiring(Settling.Same(Shape.Bit), new Binding(shut, 0), probe.Gates);
        suppose.Add(0, 1);
        var learn = new Wiring(Latch, new Binding(probe.Ahead.Rows, probe.Ahead.Record().Unadmitted), new Binding(plan.Moments, 3));
        learn.Add(0, 1);

        var engine = new Engine();
        foreach (var wiring in probe.Prepare) engine.Add(wiring);
        engine.Add(suppose).Add(probe.Ahead).Add(probe.Ahead);
        foreach (var wiring in plan.Prepare) engine.Add(wiring);
        engine.Add(learn).Add(plan.Ahead).Add(plan.Ahead).Add(Act(world, plan)).Add(world.Flow);

        engine.Step();

        Assert.True(plan.Moments.Read(3, 1));
        Assert.False(plan.Moments.Read(4, 1));
        Assert.Equal((0, 1), (Count.Read(world.Plant, 0), Count.Read(world.Plant, 2)));

        engine.Step();

        Assert.False(plan.Moments.Read(3, 1));
        Assert.Equal((0, 1), (Count.Read(world.Plant, 0), Count.Read(world.Plant, 2)));
        Assert.Equal(221, Transport.Measure(engine).Bytes);
    }

    /// <summary>A unit at a fork may go left or right and the law does not say
    /// which. Run on bits, such a law stops. Read as a relation from now to
    /// the next moment, it leaves the next moment open instead: the unit will
    /// have left, and each way may or may not hold it. Seeing it on the right
    /// then settles the left.</summary>
    [Fact]
    public void AnAmbiguousLawLeavesTheNextMomentOpenInsteadOfStopping()
    {
        var fork = TransitionLaw.FromPairs(
            [Shape.Bit, Shape.Bit, Shape.Bit],
            Enumerable.Range(0, 8).Where(s => s != 1).Select(s => (s, s)).Concat([(1, 2), (1, 4)]),
            new Accounting(false, new PreservedSum([0, 1, 2])));

        // Positions 0-2 are the fork, left and right now; 3-5 the same next. Planes rule out a bit's 0 and 1.
        var moments = new Store(2, 6);
        var bit = Binding.Open(moments, 0, 1);
        var step = new Wiring(fork.AsRelation(), bit, bit, bit, bit, bit, bit);
        step.Add(0, 1, 2, 3, 4, 5);
        moments.Write(0, 0, true);
        moments.Write(1, 1, true);
        moments.Write(1, 2, true);

        var first = step.Apply();

        Assert.True(first.Ambiguous[0]);
        Assert.Equal(0, step.Value(0, 3));
        Assert.Equal((null, null), (step.Value(0, 4), step.Value(0, 5)));

        moments.Write(0, 5, true);
        step.Apply();

        Assert.Equal(0, step.Value(0, 4));
    }

    // ---- Ladders, boundaries and linked values ----

    /// <summary>Units arrive one at a time at a count that holds three. With a
    /// carry wired to a coarser count, three fine units become one coarse
    /// unit, and nothing is ever turned away: seven arrivals read as one fine
    /// and two coarse. The carry law preserves fine + 3 × coarse, so the
    /// weights are the place values, and the two counts are a ladder with two
    /// rungs. Without the carry, four of the seven are blocked.</summary>
    [Fact]
    public void WhatDoesNotFitIsCarriedUpARung()
    {
        var (ladder, count) = Make.Counts(2, 3);
        var carry = new Wiring(Flows.Convert(3, 1, 3, 3), new Binding(ladder, 0, 1, 2), new Binding(ladder, 0, 1, 2));
        carry.Add(0, 1);
        var engine = new Engine().Add(carry);
        var (flat, _) = Make.Counts(2, 3);
        var (turnedAway, lost) = (0, 0);

        for (var unit = 0; unit < 7; unit++)
        {
            turnedAway += Unary.Add(ladder, count, Plane.Of(2, 0)).Blocked.Count;
            lost += Unary.Add(flat, count, Plane.Of(2, 0)).Blocked.Count;
            engine.Step();
        }

        Assert.Equal(0, turnedAway);
        Assert.Equal((1, 2), (count.Read(ladder, 0), count.Read(ladder, 1)));
        Assert.Equal(4, lost);
    }

    /// <summary>A structure can expose what it considers possible, not only its
    /// bits. A reader narrows its own value from that, by reference. What the
    /// reader believes never rules anything out behind the port: knowledge
    /// crosses a boundary one way unless the owner chooses otherwise.</summary>
    [Fact]
    public void WhatAnotherStructureKnowsIsReadThroughAPortAndNeverNarrowedThere()
    {
        var owner = new Store(1, 2);
        var known = Supports.On(owner, Field.Bit(0));
        owner.Write(0, 0, true);
        known.Observe(0);
        var shown = owner.Publish([0, 1], [.. known.Planes]);

        var reader = new Store(1, 2);
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
        var linked = new Store(6, 3);
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

    // ---- Builders ----

    /// <summary>Two predictors of a bit that flips every step, a judge, and a
    /// count of trust for each.</summary>
    private static (Engine Engine, Store Mind, Field Trust, Wiring ExpectsSame, Wiring Judging) Mind()
    {
        // Planes: 0 guess, 1 the bit, 2-4 trust. Positions: 0 expects a flip, 1 expects none, 2 the world.
        var mind = new Store(5, 3);
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
        return (new Engine().Add(expectsFlip).Add(expectsSame).Add(world).Add(judging), mind, trust, expectsSame, judging);
    }

    /// <summary>A plant (0 source, 1 gate, 2 target; planes 0-1 a count, plane 2
    /// the gate) with its flow, the level wanted at the target, and how far
    /// the aim has given way.</summary>
    private sealed record Setting(Store Plant, Store Wanted, Store Ease, Wiring Flow);

    private static Setting World(int units)
    {
        var plant = new Store(3, 3);
        Count.Write(plant, 0, units);
        var wanted = new Store(2, 1);
        Count.Write(wanted, 0, 2);
        var flow = new Wiring(Flows.GatedMove(2, 2), new Binding(plant, 0, 1), new Binding(plant, 2), new Binding(plant, 0, 1));
        flow.Add(0, 1, 2);
        return new Setting(plant, wanted, new Store(2, 1), flow);
    }

    /// <summary>What is possible at several moments of (source, gate, target):
    /// planes 0-2 rule out counts 0, 1, 2 and planes 3-4 rule out gate 0, 1.
    /// <see cref="Prepare"/> forgets, observes the plant into the first moment
    /// and aims the last; <see cref="Ahead"/> narrows from moment to moment.</summary>
    private sealed record Thought(Store Moments, Binding Gates, Wiring[] Prepare, Wiring Ahead);

    private static Thought Think(Setting world, int moments, bool givesWay)
    {
        var plan = new Store(5, 3 * moments);
        var counts = Binding.Open(plan, 0, 1, 2);
        var gates = Binding.Open(plan, 3, 4);

        var forgetCounts = new Wiring(Settling.Forget(3), new Binding(plan, 0, 1, 2));
        var forgetGates = new Wiring(Settling.Forget(2), new Binding(plan, 3, 4));
        for (var moment = 0; moment < moments; moment++)
        {
            forgetCounts.Add(3 * moment);
            forgetCounts.Add(3 * moment + 2);
            forgetGates.Add(3 * moment + 1);
        }
        var observe = new Wiring(Settling.Same(Count.Shape), new Binding(world.Plant, 0, 1), counts);
        observe.Add(0, 0);
        observe.Add(2, 2);
        var last = 3 * moments - 1;
        var aim = givesWay
            ? new Wiring(Lowered, new Binding(world.Wanted, 0, 1), new Binding(world.Ease, 0, 1), counts)
            : new Wiring(Settling.Same(Count.Shape), new Binding(world.Wanted, 0, 1), counts);
        if (givesWay) aim.Add(0, 0, last);
        else aim.Add(0, last);
        var ahead = new Wiring(Flows.GatedMove(2, 2).AsRelation(), counts, gates, counts, counts, gates, counts);
        for (var m = 0; m + 1 < moments; m++) ahead.Add(3 * m, 3 * m + 1, 3 * m + 2, 3 * m + 3, 3 * m + 4, 3 * m + 5);
        return new Thought(plan, gates, [forgetCounts, forgetGates, observe, aim], ahead);
    }

    /// <summary>Writes the gate's one remaining value at the first moment into the plant.</summary>
    private static Wiring Act(Setting world, Thought plan)
    {
        var act = new Wiring(Settling.Enact(Shape.Bit), new Binding(plan.Moments, 3, 4), new Binding(world.Plant, 2));
        act.Add(1, 1);
        return act;
    }

    /// <summary>A plant and one plan, wired to forget, observe, aim, narrow
    /// twice, act and flow, in that order. With <paramref name="givesWay"/> the
    /// aim is the wanted level lowered by a count that grows whenever the plan
    /// contradicts itself.</summary>
    private static (Engine Engine, Store Plant, Store Plan, Store Ease) Planner(int units, int moments = 3, bool givesWay = false)
    {
        var world = World(units);
        var plan = Think(world, moments, givesWay);
        var engine = new Engine();
        foreach (var wiring in plan.Prepare) engine.Add(wiring);
        engine.Add(plan.Ahead).Add(plan.Ahead);
        if (givesWay)
        {
            var giveWay = new Wiring(Tally, new Binding(plan.Ahead.Rows, plan.Ahead.Record().Unadmitted), new Binding(world.Ease, 0, 1));
            giveWay.Add(0, 0);
            engine.Add(giveWay);
        }
        return (engine.Add(Act(world, plan)).Add(world.Flow), world.Plant, plan.Moments, world.Ease);
    }
}
