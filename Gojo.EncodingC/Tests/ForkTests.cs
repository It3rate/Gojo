using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Forking a local structure to try arrangements without changing it:
/// state read by reference through copy-on-write workspaces, topology copied
/// so it can be rewired, laws shared. A unit walks a road, home 0 to the store
/// 4, in the direction its control says.</summary>
public sealed class ForkTests
{
    private const int Sites = 5;
    private static readonly (int, int)[] ForwardRows = [(3, 4), (2, 3), (1, 2), (0, 1)];
    private static readonly Field Direction = new(Sides.Word, 0, 1);

    private sealed record Walk(Engine Engine, Store Path, Store Control, Wiring Forward);

    private static Walk Road(int start, int direction)
    {
        var path = new Store(1, Sites);
        path.Write(0, start, true);
        var control = new Store(2, 1);
        Direction.Write(control, 0, direction);
        var forward = new Wiring(Paths.Advance(Sides.With), new Binding(path, 0), new Binding(control, 0, 1), new Binding(path, 0));
        foreach (var (from, to) in ForwardRows) forward.Add(from, 0, to);
        var back = new Wiring(Paths.Advance(Sides.Against), new Binding(path, 0), new Binding(control, 0, 1), new Binding(path, 0));
        foreach (var (from, to) in ForwardRows.Reverse()) back.Add(to, 0, from);
        return new Walk(new Engine().Add(forward).Add(back).Hold(path, control), path, control, forward);
    }

    private static int At(Surface path) => Assert.Single(Enumerable.Range(0, Sites), i => path.Read(0, i));

    private static int[] Units(Surface path) => [.. Enumerable.Range(0, Sites).Where(i => path.Read(0, i))];

    private static void Run(Engine engine, int steps)
    {
        for (var i = 0; i < steps; i++) engine.Step();
    }

    /// <summary>A fork that reads everything live walks two steps while its
    /// parent stays put: the unit is at site 2 in the fork and still at home in
    /// the parent. The fork copied only the path positions it changed and none
    /// of the control. And it reads the parent live where it has not written:
    /// when the parent's direction changes, the fork turns with it.</summary>
    [Fact]
    public void AForkReadsItsParentLiveAndCopiesOnlyWhatItWrites()
    {
        var walk = Road(start: 0, direction: Sides.With);
        var fork = walk.Engine.Fork(holdMaterial: false);

        Run(fork.Engine, 2);

        Assert.Equal((2, 0), (At(fork.Of(walk.Path)), At(walk.Path)));
        Assert.Equal(Plane.Of(Sites, 0, 1, 2), fork.Of(walk.Path).Held);
        Assert.True(fork.Of(walk.Control).Held.None);

        Direction.Write(walk.Control, 0, Sides.Against);
        Run(fork.Engine, 1);
        Assert.Equal((1, 0), (At(fork.Of(walk.Path)), At(walk.Path)));
    }

    /// <summary>A fork rewires one row so the walk skips site 2, and reaches
    /// the store by its own route. The parent, run afterwards, keeps its own
    /// topology and its own route.</summary>
    [Fact]
    public void ARewiredForkTriesAnotherArrangementAndTheParentKeepsItsOwn()
    {
        var walk = Road(start: 0, direction: Sides.With);
        var fork = walk.Engine.Fork();
        fork.Of(walk.Forward).Rewire(2, 2, 3);

        var forked = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            fork.Engine.Step();
            forked.Add(At(fork.Of(walk.Path)));
        }
        var parent = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            walk.Engine.Step();
            parent.Add(At(walk.Path));
        }

        Assert.Equal([1, 3, 4], forked);
        Assert.Equal([1, 2, 3], parent);
        Assert.Equal([1, 0, 2], walk.Forward.Row(2));
    }

    /// <summary>A live read and a conserved unit do not mix. When the parent
    /// moves while the fork moves differently, the fork still reads the parent
    /// where it has not written, and sees the parent's unit beside its own:
    /// two versions of one unit in one view. Nothing is lost or made in either
    /// structure, and the fork can tell: home, which it copied while the unit
    /// was there and the parent has since left, is stale, and a commit is
    /// refused. Site 1 is not stale: the parent's unit passed through it and
    /// left it as the fork copied it, since staleness compares bits, not
    /// history. This is why forks hold material fixed by default.</summary>
    [Fact]
    public void AForkReadingAMovingParentSeesBothVersionsAndKnowsIt()
    {
        var walk = Road(start: 0, direction: Sides.With);
        var fork = walk.Engine.Fork(holdMaterial: false);
        fork.Of(walk.Forward).Rewire(2, 2, 3);

        for (var i = 0; i < 2; i++)
        {
            fork.Engine.Step();
            walk.Engine.Step();
        }

        Assert.Equal([2, 3], Units(fork.Of(walk.Path)));
        Assert.Equal(2, At(walk.Path));
        Assert.Equal(Plane.Of(Sites, 0), fork.Of(walk.Path).Stale());
        Assert.False(fork.Commit());
    }

    /// <summary>By default a fork holds its material fixed: the planes the
    /// structure's own conserving laws move units on, here the road's
    /// occupancy, found from the laws and not declared. Run while the parent
    /// moves differently, the fork keeps exactly its own unit; the parent's
    /// does not show through. Information is still live: when the parent's
    /// direction changes, the fork turns with it. And because the held copy is
    /// what a commit checks, the fork will not commit over a parent that moved.</summary>
    [Fact]
    public void AForkHoldsItsMaterialFixedAndStillReadsInformationLive()
    {
        var walk = Road(start: 0, direction: Sides.With);
        var fork = walk.Engine.Fork();
        fork.Of(walk.Forward).Rewire(2, 2, 3);

        Assert.Equal([(walk.Path, 0)], fork.Held);
        for (var i = 0; i < 2; i++)
        {
            fork.Engine.Step();
            walk.Engine.Step();
        }

        Assert.Equal([3], Units(fork.Of(walk.Path)));
        Assert.Equal(2, At(walk.Path));

        Direction.Write(walk.Control, 0, Sides.Against);
        fork.Engine.Step();
        Assert.Equal([2], Units(fork.Of(walk.Path)));
        Assert.False(fork.Commit());
        Assert.Equal(2, At(walk.Path));
    }

    /// <summary>Two forks from one start hold two versions at once: one walks
    /// on, one waits. Each reads differently from the other where they acted,
    /// and the parent holds neither.</summary>
    [Fact]
    public void SiblingForksHoldDifferentVersions()
    {
        var walk = Road(start: 1, direction: Sides.With);
        var onward = walk.Engine.Fork();
        var waiting = walk.Engine.Fork();
        Direction.Write(waiting.Of(walk.Control), 0, Sides.AtFromAfter);

        Run(onward.Engine, 2);
        Run(waiting.Engine, 2);

        Assert.Equal((3, 1, 1), (At(onward.Of(walk.Path)), At(waiting.Of(walk.Path)), At(walk.Path)));
        Assert.True(onward.Of(walk.Path).Differs(waiting.Of(walk.Path)).Any);
    }

    /// <summary>Committing writes the fork's state into the parent. If the
    /// parent has moved since the fork copied what it would overwrite, the
    /// commit is refused and writes nothing.</summary>
    [Fact]
    public void CommittingAForkWritesItsStateBackUnlessTheParentMoved()
    {
        var walk = Road(start: 0, direction: Sides.With);
        var fork = walk.Engine.Fork();
        Run(fork.Engine, 2);

        Assert.True(fork.Commit());
        Assert.Equal(2, At(walk.Path));

        var late = walk.Engine.Fork();
        Run(late.Engine, 1);
        walk.Engine.Step();

        Assert.False(late.Commit());
        Assert.Equal(3, At(walk.Path));
    }

    /// <summary>A fork cannot touch the world. Binding its parent's store
    /// directly is refused, and a structure that takes through a port cannot
    /// be forked, since taking would act on another structure.</summary>
    [Fact]
    public void AForkCannotReachTheWorldDirectly()
    {
        var walk = Road(start: 0, direction: Sides.With);
        var fork = walk.Engine.Fork();
        var reach = new Wiring(Settling.Same(Shape.Bit), new Binding(walk.Path, 0), new Binding(walk.Path, 0));
        reach.Add(0, 1);

        Assert.Throws<ArgumentException>(() => fork.Engine.Add(reach));

        var (owner, count) = Make.Counts(1, 3);
        count.Write(owner, 0, 1);
        new Engine().Hold(owner);
        var (cup, _) = Make.Counts(1, 3);
        var taker = new Engine().Add(Make.Wire(Flows.Move(3, 3), owner.Publish([0], [0, 1, 2]), cup));

        Assert.Throws<ArgumentException>(() => taker.Fork());
    }
}
