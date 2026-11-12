using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Uncertainty as a working part: open values held in the planes,
/// suppositions narrowed in workspaces, a goal that settles the open choices that
/// reach it, ambiguity given a place, and the gap between what was supposed
/// and what is, read by laws and used to steer.</summary>
public sealed class UncertaintyTests
{
    /// <summary>What is still possible at a position is planes of the
    /// store: one per value, set where the value is ruled out. Everything
    /// starts open. A law can read those planes like any others, here to light
    /// a lamp where a value is settled, and a saved machine keeps them.</summary>
    [Fact]
    public void AnOpenValueIsHeldInThePlanesAndSavedWithTheMachine()
    {
        var line = new Store(1, 3);
        var bit = Field.Bit(0);
        var support = Supports.On(line, bit);
        var lamp = line.AddPlane();

        Assert.Equal(Plane.Ones(3), support.Open);

        line.Write(0, 0, true);
        support.Observe(0);

        var settled = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Bit, Shape.Bit],
            s => [s[0], s[1], s[0] ^ s[1]],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var lamps = new Wiring(settled, new Binding(line, support.Planes[0]), new Binding(line, support.Planes[1]), new Binding(line, lamp));
        for (var i = 0; i < 3; i++) lamps.Add(i, i, i);
        lamps.Apply();

        Assert.Equal(Plane.Of(3, 0), line.Read(lamp));

        var (_, copies) = Transport.Load(Transport.Save(new Engine().Add(lamps), line));
        var restored = new Supports(copies[0], bit, [.. support.Planes]);

        Assert.Equal(0b10UL, restored.Values(0));
        Assert.Equal(Plane.Of(3, 1, 2), restored.Open);
    }

    /// <summary>Four positions that must hold equal bits; the first is known to
    /// be 1. A workspace supposes the last is 0 and narrows: a contradiction, found
    /// in the workspace only. Dropped, the workspace reads what is known again. Supposing
    /// 1 instead settles everything, and a commit makes it known.</summary>
    [Fact]
    public void ASuppositionIsNarrowedInAWorkspaceAndDroppedWhenItContradicts()
    {
        var line = new Store(1, 4);
        var known = Supports.On(line, Field.Bit(0));
        line.Write(0, 0, true);
        known.Observe(0);

        var workspace = new Workspace(line).SelectAll();
        var supposed = known.In(workspace);
        var rows = new Wiring(Settling.Same(Shape.Bit), supposed.Variable, supposed.Variable);
        rows.Add(0, 1);
        rows.Add(1, 2);
        rows.Add(2, 3);

        supposed.Restrict(3, 0b01);
        while (workspace.Apply(rows).Step.Changed.Any)
        {
        }

        Assert.True(supposed.Empty.Any);
        Assert.Equal(3, known.Open.Count);

        workspace.Revert();
        Assert.Equal(known.Open, supposed.Open);

        supposed.Restrict(3, 0b10);
        workspace.Apply(rows);

        Assert.Equal(Plane.Ones(4), supposed.Settled);
        Assert.True(workspace.Commit().Committed);
        Assert.Equal(Plane.Ones(4), known.Settled);
    }

    /// <summary>A unit, a closed gate and an empty target; the goal is the unit
    /// at the target. One workspace is now with the gate opened as a place that may
    /// change. Another is the next moment: open, because it has not happened,
    /// except for the wanted state written into it. The passage law, read as a
    /// relation between the two, settles the one thing left open: the gate must
    /// be set. Enacting and committing that is the decision. The next-moment
    /// workspace becomes the prediction, and after the law runs for real its gap is
    /// empty. Without a unit to send, the same goal reaches a contradiction in
    /// the workspaces and what is known stays as it was.</summary>
    [Fact]
    public void AGoalSettlesTheOpenChoiceThatReachesIt()
    {
        var unit = Field.Bit(0);
        var gate = Field.Bit(1);

        (Store Chain, Supports Units, Supports Gates, Workspace Plan, Workspace Goal) Plan(bool hasUnit)
        {
            var chain = new Store(2, 3);
            chain.Write(0, 0, hasUnit);
            var units = Supports.On(chain, unit);
            var gates = Supports.On(chain, gate);
            for (var p = 0; p < 3; p++)
            {
                units.Observe(p);
                gates.Observe(p);
            }

            var plan = new Workspace(chain);
            gates.In(plan).Forget(1);

            var goal = new Workspace(chain);
            unit.Write(goal, 2, 1);
            for (var p = 0; p < 3; p++)
            {
                units.In(goal).Forget(p);
                gates.In(goal).Forget(p);
            }
            units.In(goal).Observe(2);

            var step = new Wiring(Flows.Passage.AsRelation(),
                units.In(plan).Variable, gates.In(plan).Variable, units.In(plan).Variable,
                units.In(goal).Variable, gates.In(goal).Variable, units.In(goal).Variable);
            step.Add(0, 1, 2, 0, 1, 2);
            step.Apply();
            return (chain, units, gates, plan, goal);
        }

        var (chain, units, gates, plan, goal) = Plan(hasUnit: true);

        Assert.Equal(0b10UL, gates.In(plan).Values(1));
        Assert.Equal(0b01UL, gates.Values(1));

        Assert.True(gates.In(plan).Enact(1));
        Assert.True(plan.Commit().Committed);
        Assert.True(chain.Read(1, 1));

        Assert.True(units.In(goal).Enact(0));
        Assert.True(units.In(goal).Enact(2));
        Assert.True(goal.Gap.Read(0, 0));
        Assert.True(goal.Gap.Read(0, 2));

        var passage = new Wiring(Flows.Passage, new Binding(chain, 0), new Binding(chain, 1), new Binding(chain, 0));
        passage.Add(0, 1, 2);
        passage.Apply();

        Assert.True(chain.Read(0, 2));
        Assert.False(goal.Gap.Read(0, 0));
        Assert.False(goal.Gap.Read(0, 2));

        var (_, knownUnits, knownGates, blocked, _) = Plan(hasUnit: false);

        Assert.True(knownGates.In(blocked).Empty[1]);
        Assert.Equal(0b01UL, knownUnits.Values(0));
        Assert.Equal(0b01UL, knownGates.Values(1));
    }

    /// <summary>A unit at a fork may go left or right, and the law does not say
    /// which, so alone it does not run. With a choice role the same law runs,
    /// and the choice is a bit a structure can set. Left open instead, the
    /// choice is narrowed from an outcome: seeing where the unit went says
    /// what the choice was.</summary>
    [Fact]
    public void AmbiguityBecomesAChoiceSomethingCanMake()
    {
        var fork = TransitionLaw.FromPairs(
            [Shape.Bit, Shape.Bit, Shape.Bit],
            Enumerable.Range(0, 8).Where(s => s != 1).Select(s => (s, s)).Concat([(1, 2), (1, 4)]),
            new Accounting(false, new PreservedSum([0, 1, 2])));
        var net = new Store(1, 4);
        net.Write(0, 0, true);
        var undecided = new Wiring(fork, new Binding(net, 0), new Binding(net, 0), new Binding(net, 0));
        undecided.Add(0, 1, 2);

        var stuck = undecided.Apply();

        Assert.True(stuck.Ambiguous[0]);
        Assert.True(net.Read(0, 0));

        var chosen = fork.WithChoice();
        Assert.True(chosen.IsFunctional);
        Assert.False(chosen.Writes(3));
        Assert.True(chosen.Accounting.Conserves(0));

        net.Write(0, 3, true);
        var decided = new Wiring(chosen, new Binding(net, 0), new Binding(net, 0), new Binding(net, 0), new Binding(net, 0));
        decided.Add(0, 1, 2, 3);
        decided.Apply();

        Assert.Equal(Plane.Parse("0011"), net.Read(0));

        // Before: a unit at the fork, both ways empty, the choice unknown. After: the unit is on the right.
        var whichWay = chosen.AsRelation().Project(3, [0b10, 0b01, 0b01, 0b11, 0b11, 0b11, 0b10, 0b11]);
        Assert.Equal(0b10UL, whichWay);
    }

    /// <summary>A workspace predicts both bits will be set; one is. The gap between
    /// the workspace and its source is a surface, so a law reads the miss by
    /// reference and keeps a count of trust per prediction: up on a hit, down
    /// on a miss. How well a structure predicts becomes how far it is trusted,
    /// in the same medium. A gap is read and never written.</summary>
    [Fact]
    public void AMissIsASignalALawReadsAndTrustFollowsIt()
    {
        var world = new Store(1, 2);
        var guess = new Workspace(world);
        guess.Write(0, 0, true);
        guess.Write(0, 1, true);
        world.Write(0, 0, true);

        var (trust, level) = Make.Counts(2, 3);
        level.Write(trust, 0, 2);
        level.Write(trust, 1, 2);
        var judge = TransitionLaw.FromFunction(
            [Shape.Bit, Shape.Unary(3)],
            s => [s[0], s[0] == 1 ? (s[1] > 0 ? s[1] - 1 : 0) : (s[1] < 3 ? s[1] + 1 : 3)],
            new Accounting(true, new Unchanged(0)));
        var judging = new Wiring(judge, new Binding(guess.Gap, 0), new Binding(trust, 0, 1, 2));
        judging.Add(0, 0);
        judging.Add(1, 1);

        judging.Apply();

        Assert.Equal((3, 1), (level.Read(trust, 0), level.Read(trust, 1)));
        Assert.Throws<ArgumentException>(() => new Wiring(Flows.Toggle, new Binding(guess.Gap, 0)));
    }

    /// <summary>A reservoir, a tank and a drain, with a valve into the tank and
    /// one out of it. The goal is a workspace over the plant with the wanted tank
    /// level written in: the prediction is set to the goal. One law reads the
    /// level and the wanted level and opens whichever valve closes the gap, so
    /// the flow itself brings the plant to the goal and the gap empties. The
    /// goal can change, and a unit arriving unasked from outside is noise the
    /// same structure accounts for by sending it on. Nothing is made or lost.</summary>
    [Fact]
    public void FlowIsSteeredTowardAGoalByTheGapItLeaves()
    {
        // Positions: 0 reservoir, 1 tank, 2 drain, 3 valve in, 4 valve out. Planes 0-3 a count, plane 4 a valve.
        var plant = new Store(5, 5);
        var count = new Field(Shape.Unary(4), 0, 1, 2, 3);
        count.Write(plant, 0, 4);
        var goal = new Workspace(plant);
        count.Write(goal, 1, 3);

        var steer = TransitionLaw.FromFunction(
            [Shape.Unary(4), Shape.Unary(4), Shape.Bit, Shape.Bit],
            s => [s[0], s[1], s[0] < s[1] ? 1 : 0, s[0] > s[1] ? 1 : 0],
            new Accounting(true, new Unchanged(0), new Unchanged(1)));
        var steering = new Wiring(steer,
            new Binding(plant, 0, 1, 2, 3), new Binding(goal, 0, 1, 2, 3), new Binding(plant, 4), new Binding(plant, 4));
        steering.Add(1, 1, 3, 4);
        var flow = new Wiring(Flows.GatedMove(4, 4), new Binding(plant, 0, 1, 2, 3), new Binding(plant, 4), new Binding(plant, 0, 1, 2, 3));
        flow.Add(0, 3, 1);
        flow.Add(1, 4, 2);
        var engine = new Engine().Add(steering).Add(flow);
        (int?, int?, int?) Levels() => (count.Read(plant, 0), count.Read(plant, 1), count.Read(plant, 2));

        Assert.True(goal.Gap.Read(0, 1));
        engine.Run(5);

        Assert.Equal((1, 3, 0), Levels());
        Assert.True(goal.Changes().None);

        count.Write(goal, 1, 1);
        engine.Run(4);

        Assert.Equal((1, 1, 2), Levels());
        Assert.True(goal.Changes().None);

        Assert.Equal(Plane.Of(5, 1), Unary.Add(plant, count, Plane.Of(5, 1)).Taken);
        Assert.False(goal.Changes().None);
        engine.Run(2);

        Assert.Equal((1, 1, 3), Levels());
        Assert.True(goal.Changes().None);
    }
}
