using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>A goal as structure, and an outcome selected from it.
/// <para>A goal has three parts, all ordinary structure: a <b>selection</b>,
/// the plan positions its rows reference; a <b>condition</b>, the law on
/// those rows; and a <b>target</b>, values it holds fixed, read as samples
/// that narrowing never changes. Its horizon is the plan's last moment, and a
/// stay is a step, so "arrive by then" is "be there then". What the goal does
/// not select it does not constrain.</para>
/// <para>Two goals over one plan show the difference. "End at the store"
/// selects only the last moment, so any way there satisfies it, by the road
/// over the bridge or by the helicopter. "Go by the road" selects every
/// moment. Whether a goal is met is a read too: the violations of its rows
/// against a plan or a trail.</para>
/// <code>
/// home 0 ─ 1 ─ bridge 2 ─ 3 ─ store 4
///    └──── helipad 5 ──────────┘
/// </code></summary>
public sealed class GoalTests
{
    private const int Sites = 6, Home = 0, Bridge = 2, Store = 4, Helipad = 5;
    private const int Moments = 5;
    private static readonly (int, int)[] Map = [(0, 1), (1, 2), (2, 3), (3, 4), (0, 5), (5, 4)];
    private static readonly Shape Site = Shape.Binary(Sites);
    private static readonly Field SiteCode = new(Site, 0, 1, 2);

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    /// <summary>A plan: what the traveller's site may be at each moment, and
    /// the step that arrived there, all open; it starts at home.</summary>
    private sealed record Plan(Engine Engine, Store Moments, Supports Site, Supports Step);

    private static Plan Open()
    {
        var moments = new Store(SiteCode.Planes.Count + 2, Moments);
        var site = Supports.On(moments, SiteCode);
        var step = Supports.On(moments, new Field(Sides.Word, 3, 4));
        var steps = new Wiring(Paths.Step(Sites, Map), site.Variable, site.Variable, step.Variable);
        for (var t = 1; t < Moments; t++) steps.Add(t - 1, t, t);
        site.Restrict(0, Values(Home));
        return new Plan(new Engine().Add(steps), moments, site, step);
    }

    /// <summary>A goal's target: fixed site values, one per selected moment,
    /// read as samples.</summary>
    private static Store Target(params int[] sites)
    {
        var target = new Store(SiteCode.Planes.Count, sites.Length);
        for (var i = 0; i < sites.Length; i++) SiteCode.Write(target, i, sites[i]);
        return target;
    }

    /// <summary>A goal on a plan: one row per selected moment, holding the
    /// plan's site there to the target's value.</summary>
    private static Wiring Goal(Plan plan, Store target, params int[] selected)
    {
        var goal = new Wiring(Settling.Same(Site), new Binding(target, [.. SiteCode.Planes]), plan.Site.Variable);
        for (var i = 0; i < selected.Length; i++) goal.Add(i, selected[i]);
        plan.Engine.Add(goal);
        return goal;
    }

    private static void Settle(Engine engine)
    {
        for (var round = 0; round < 16; round++)
            if (engine.Step().Steps.All(step => step.Changed.None)) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    private static ulong[] SitesOf(Plan plan) => [.. Enumerable.Range(0, Moments).Select(plan.Site.Values)];

    /// <summary>"End at the store" selects only the last moment. The plan then
    /// keeps every way there open: first steps toward the bridge, toward the
    /// helipad, or waiting at home. Nothing it did not select is decided.</summary>
    [Fact]
    public void AnOutcomeGoalSelectsTheEndAndLeavesTheRouteFree()
    {
        var plan = Open();
        Goal(plan, Target(Store), Moments - 1);

        Settle(plan.Engine);

        Assert.Equal(Values(Store), plan.Site.Values(Moments - 1));
        Assert.Equal(Values(Home, 1, Helipad), plan.Site.Values(1));
        Assert.Equal(Values(Store, 3, Helipad), plan.Site.Values(Moments - 2));
        Assert.True(plan.Site.Empty.None);
    }

    /// <summary>"Go by the road" selects every moment, and settles the whole
    /// plan: over the bridge, each step with the road.</summary>
    [Fact]
    public void ARouteGoalSelectsEveryMomentAndSettlesTheWayThere()
    {
        var plan = Open();
        Goal(plan, Target(Home, 1, Bridge, 3, Store), 0, 1, 2, 3, 4);

        Settle(plan.Engine);

        Assert.Equal([Values(Home), Values(1), Values(Bridge), Values(3), Values(Store)], SitesOf(plan));
        Assert.All(Enumerable.Range(1, Moments - 1), t => Assert.Equal(Values(Sides.With), plan.Step.Values(t)));
    }

    /// <summary>Whether a goal is met is a read: the rows it violates against
    /// what happened. By helicopter, the outcome goal is met and the route goal
    /// is not, from the first moment the routes part; by road, both are met.
    /// The traveller's trail is the same kind of structure as a plan, with
    /// every moment settled.</summary>
    [Fact]
    public void AnOutcomeIsCheckedAgainstEachGoalByItsViolations()
    {
        (Plane Outcome, Plane Route) Check(params int[] trail)
        {
            var plan = Open();
            for (var t = 0; t < Moments; t++) plan.Site.Restrict(t, Values(trail[t]));
            var outcome = Goal(plan, Target(Store), Moments - 1);
            var route = Goal(plan, Target(Home, 1, Bridge, 3, Store), 0, 1, 2, 3, 4);
            return (outcome.Violations(), route.Violations());
        }

        var byHelicopter = Check(Home, Helipad, Store, Store, Store);
        var byRoad = Check(Home, 1, Bridge, 3, Store);

        Assert.True(byHelicopter.Outcome.None);
        Assert.Equal(Plane.Of(5, 1, 2, 3), byHelicopter.Route);
        Assert.True(byRoad.Outcome.None && byRoad.Route.None);
    }

    /// <summary>A goal can select a waypoint, not a whole route: "be on the
    /// bridge at the second step". It rules the helicopter out, since the
    /// helipad route never touches the bridge, and leaves the rest of the road
    /// trip settled by the map alone.</summary>
    [Fact]
    public void AWaypointGoalSelectsOneMomentInTheMiddle()
    {
        var plan = Open();
        Goal(plan, Target(Store), Moments - 1);
        Goal(plan, Target(Bridge), 2);

        Settle(plan.Engine);

        Assert.Equal([Values(Home), Values(1), Values(Bridge), Values(3), Values(Store)], SitesOf(plan));
    }
}
