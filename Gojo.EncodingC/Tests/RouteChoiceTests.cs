using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Explicit route choice (docs/GojoTension-2026-11.md, §3.5). Two moves
/// that would take from one count in one step taken together contend. Each
/// move here runs only while a gate bit is 1. Gates read from an open choice,
/// one support plane per route, leave every possible route open, so
/// contention is exactly an open choice: the rows contested are the routes
/// still possible, and narrowing the choice settles which one runs. Gates
/// read from a settled code, a turn bit and its flipped view, open one route
/// at a time, so nothing contends.</summary>
public sealed class RouteChoiceTests
{
    private const int A = 0, B = 1;

    /// <summary>A source count holding two, two targets of room two, and the
    /// choice of route, open, one support plane per route.</summary>
    private sealed record Fork(Engine Engine, Store Store, Field Source, Field[] Targets, Supports Choice, Wiring[] Routes);

    private static Fork Routes()
    {
        var store = new Store(7, 1);
        var source = new Field(Shape.Unary(3), 0, 1, 2);
        Field[] targets = [new(Shape.Unary(2), 3, 4), new(Shape.Unary(2), 5, 6)];
        source.Write(store, 0, 2);
        var choice = Supports.On(store, new Field(Shape.Binary(2), store.AddPlane()));
        var here = Plane.Of(1, 0);
        Wiring[] routes = [.. new[] { A, B }.Select(r => Tension.Route(source.On(store), targets[r].On(store), here, choice.Possible(r)))];
        var engine = new Engine();
        foreach (var route in routes) engine.Add(route);
        return new Fork(engine, store, source, targets, choice, routes);
    }

    private static (int?, int?, int?) Read(Fork f) =>
        (f.Source.Read(f.Store, 0), f.Targets[A].Read(f.Store, 0), f.Targets[B].Read(f.Store, 0));

    /// <summary>While both routes are possible, both are contested and nothing
    /// moves: the contest is the open choice. Narrowing the choice to B
    /// settles it, and taken together the unit goes to B. Opening the choice
    /// again brings the contest back.</summary>
    [Fact]
    public void ContentionIsAnOpenChoiceAndNarrowingSettlesIt()
    {
        var fork = Routes();

        var open = fork.Engine.StepTogether();
        Assert.Equal((2, 0, 0), Read(fork));
        Assert.All(open.Steps, step => Assert.True(step.Contested![0]));

        fork.Choice.Restrict(0, 1UL << B);
        var chosen = fork.Engine.StepTogether();
        Assert.Equal((1, 0, 1), Read(fork));
        Assert.All(chosen.Steps, step => Assert.False(step.Contested![0]));

        fork.Choice.Forget(0);
        fork.Engine.StepTogether();
        Assert.Equal((1, 0, 1), Read(fork));
    }

    /// <summary>The structure settles the choice itself: a fact, read as a
    /// sample, says to send to B, and a law narrows the choice to it. The first
    /// step taken together narrows while the routes still contest; from then
    /// on only B runs, until it is full. Nothing reaches A.</summary>
    [Fact]
    public void ALawSettlesTheChoiceAndOnlyTheChosenRouteRuns()
    {
        var fork = Routes();
        var preference = new Field(Shape.Binary(2), fork.Store.AddPlane());
        preference.Write(fork.Store, 0, B);
        var decide = new Wiring(Settling.Same(Shape.Binary(2)), preference.On(fork.Store), fork.Choice.Variable);
        decide.Add(0, 0);
        fork.Engine.Add(decide);

        var first = fork.Engine.StepTogether();
        Assert.True(first.Steps[0].Contested![0]);
        Assert.NotNull(fork.Engine.Settle(8, together: true));

        Assert.Equal((0, 0, 2), Read(fork));
        Assert.Equal(1UL << B, fork.Choice.Values(0));
    }

    /// <summary>Turn-taking that settles. Whose turn it is, absorbing (0) or
    /// drawing (1), is a bit written by a law reading which side has work: with
    /// work on both sides the turn alternates, with work on one side it goes
    /// there, with none it holds. Absorbing waits on the flipped turn and drawing
    /// on the turn, so exactly one may run in any step. Taken together they never
    /// contend, two units of pressure and two of debt both settle, and then the
    /// structure is quiet; a turn bit that toggled every step never would be.</summary>
    [Fact]
    public void ATurnThatFollowsTheWorkLetsAbsorbingAndDrawingSettle()
    {
        var store = new Store(3, 1);
        var value = new Field(Shape.Unary(3), 0, 1, 2);
        value.Write(store, 0, 1);
        var site = new Tension(store, value, 3);
        var turn = store.AddPlane();
        var here = Plane.Of(1, 0);
        var engine = new Engine()
            .Add(Tension.Wire(Flows.Turn, [new Binding(store, site.Plus.Planes[0]), new Binding(store, site.Minus.Planes[0]), new Binding(store, turn)], here))
            .Add(site.Absorb(here, new Binding(new Flipped(store, turn), turn)))
            .Add(site.Draw(here, new Binding(store, turn)));
        for (var i = 0; i < 2; i++)
        {
            site.Receive(here);
            site.Demand(here);
        }

        var seen = new List<(int?, int?, int?)>();
        for (var i = 0; i < 4; i++)
        {
            var step = engine.StepTogether();
            Assert.All(step.Steps, s => Assert.True(s.Contested!.None));
            seen.Add((value.Read(store, 0), site.Plus.Read(store, 0), site.Minus.Read(store, 0)));
        }

        Assert.Equal([(2, 1, 2), (1, 1, 1), (2, 0, 1), (1, 0, 0)], seen);
        Assert.NotNull(engine.Settle(4, together: true));
    }
}
