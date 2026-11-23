using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A unit flowing along a path. The path is one definition; the
/// mover is a unit at one site; the use's direction is one value the move
/// rows read. Each step moves the unit at most one site, in the direction set,
/// and never makes or loses it. What the mover did is then read from the
/// sites it was at, by the same step law the clauses narrow with.
/// <para>One step, one site, comes from declared order, which is structure:
/// forward rows run from the far end back, and back rows from the near end
/// out, so no row reads a unit an earlier row has just moved.</para></summary>
public sealed class PathFlowTests
{
    private const int Sites = 5;
    private static readonly (int, int)[] Road = [(0, 1), (1, 2), (2, 3), (3, 4)];

    /// <summary>The path's sites, a control holding the use's direction, and
    /// the move rows over them.</summary>
    private sealed record Walk(Engine Engine, Store Path, Store Control, Field Direction, Wiring Forward, Wiring Back);

    /// <summary>Move rows for each neighbour pair, forward ones from the far
    /// end and back ones from the near end, as listed.</summary>
    private static Walk Build(int sites, (int From, int To)[] forwardRows, int start)
    {
        var path = new Store(1, sites);
        path.Write(0, start, true);
        var control = new Store(Sides.Word.Bits, 1);
        var direction = new Field(Sides.Word, 0, 1);
        direction.Write(control, 0, Sides.AtFromAfter);
        var forward = new Wiring(Paths.Advance(Sides.With), new Binding(path, 0), new Binding(control, 0, 1), new Binding(path, 0));
        foreach (var (from, to) in forwardRows) forward.Add(from, 0, to);
        var back = new Wiring(Paths.Advance(Sides.Against), new Binding(path, 0), new Binding(control, 0, 1), new Binding(path, 0));
        foreach (var (from, to) in forwardRows.Reverse()) back.Add(to, 0, from);
        forward.Record();
        return new Walk(new Engine().Add(forward).Add(back), path, control, direction, forward, back);
    }

    private static Walk OnTheRoad(int start = 0) => Build(Sites, [(3, 4), (2, 3), (1, 2), (0, 1)], start);

    private static int At(Walk walk) => Assert.Single(walk.Path.Read(0).Indexes());

    /// <summary>Steps the walk, returning where the unit was after each step.</summary>
    private static List<int> Run(Walk walk, int steps)
    {
        var trail = new List<int>();
        for (var i = 0; i < steps; i++)
        {
            walk.Engine.Step();
            trail.Add(At(walk));
        }
        return trail;
    }

    /// <summary>The use read from a trail: the direction of each step, narrowed
    /// by the path's step law from the sites alone.</summary>
    private static int?[] Directions(IReadOnlyList<int> trail)
    {
        var store = new Store(3, trail.Count);
        var site = Supports.On(store, new Field(Shape.Binary(Sites), [0, 1, 2]));
        var arrival = Supports.On(store, new Field(Sides.Word, 0, 1));
        for (var t = 0; t < trail.Count; t++) site.Restrict(t, 1UL << trail[t]);
        var steps = new Wiring(Paths.Step(Sites, Road), site.Variable, site.Variable, arrival.Variable);
        for (var t = 1; t < trail.Count; t++) steps.Add(t - 1, t, t);
        steps.Apply();
        return [.. Enumerable.Range(1, trail.Count - 1).Select(t => steps.Value(t - 1, 2))];
    }

    /// <summary>Set forward, the unit advances one site a step to the store and
    /// stays there, since nothing is beyond it. It is never more than one unit.</summary>
    [Fact]
    public void AUnitAdvancesOneSiteAStepAndRestsAtTheEnd()
    {
        var walk = OnTheRoad();
        walk.Direction.Write(walk.Control, 0, Sides.With);

        var trail = Run(walk, 6);

        Assert.Equal([1, 2, 3, 4, 4, 4], trail);
        Assert.Equal(1, walk.Path.Read(0).Count);
        Assert.True(walk.Engine.Step().Steps.All(step => step.Changed.None));
    }

    /// <summary>Forward twice, then turned back: the same path used with the
    /// direction set back. The unit retraces its sites to home and rests, and
    /// stays at rest without new input. Read from its sites, the use goes
    /// forward, forward, back, back, then stays, a step that moved nothing
    /// detectably and whose heading the sites alone leave open; the turn is the
    /// change from forward to back.</summary>
    [Fact]
    public void TurningBackIsTheSamePathUsedTheOtherWay()
    {
        var walk = OnTheRoad();
        walk.Direction.Write(walk.Control, 0, Sides.With);
        var trail = new List<int> { At(walk) };
        trail.AddRange(Run(walk, 2));

        walk.Direction.Write(walk.Control, 0, Sides.Against);
        trail.AddRange(Run(walk, 3));

        Assert.Equal([0, 1, 2, 1, 0, 0], trail);
        var steps = Directions(trail);
        Assert.Equal([Sides.With, Sides.With, Sides.Against, Sides.Against, null], steps);
        bool Reads(RelationLaw law, int i) => steps[i - 1] is int a && steps[i] is int b && law.BitAt(law.TupleIndex([a, b]));
        Assert.Equal([2], Enumerable.Range(1, steps.Length - 1).Where(i => Reads(Sides.Reversal, i)));
        Assert.True(Reads(Sides.TurnedBack, 2));
        Assert.True(walk.Engine.Step().Steps.All(step => step.Changed.None));
        Assert.Equal(1, walk.Path.Read(0).Count);
    }

    /// <summary>A site taken out of the path stops the unit before it. The
    /// move into it does not run and is reported as skipped; nothing jumps the
    /// gap and nothing is lost.</summary>
    [Fact]
    public void ACutInThePathHoldsTheUnitAndSaysWhere()
    {
        var walk = OnTheRoad();
        walk.Path.Retire(3);
        walk.Direction.Write(walk.Control, 0, Sides.With);

        var trail = Run(walk, 4);
        var step = walk.Engine.Step();

        Assert.Equal([1, 2, 2, 2], trail);
        Assert.True(step.Steps[0].Skipped[1]);
        Assert.True(walk.Forward.Rows.Read(walk.Forward.Recorded!.Skipped, 1));
    }

    /// <summary>At a fork both branches are neighbours, and the path does not
    /// say which to take. A unit still goes one way, and here the declared
    /// order of the rows is what chose: list the other branch first and it
    /// goes there. The route choice is hidden in the schedule, not stated.</summary>
    [Fact]
    public void AtAForkTheRowOrderChoosesTheRouteAndThePathDoesNot()
    {
        var left = Build(Sites, [(3, 4), (2, 4), (1, 2), (1, 3), (0, 1)], 1);
        var right = Build(Sites, [(3, 4), (2, 4), (1, 3), (1, 2), (0, 1)], 1);
        foreach (var walk in new[] { left, right }) walk.Direction.Write(walk.Control, 0, Sides.With);

        Assert.Equal([2, 4], Run(left, 2));
        Assert.Equal([3, 4], Run(right, 2));
    }
}
