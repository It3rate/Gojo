using Xunit;

namespace Gojo.EncodingC.Tests;

/// <summary>Knowing only what observers see. Three kinds of local structure
/// meet only through ports:
/// <list type="bullet">
/// <item>the <b>world</b>: a road of sites, home 0, 1, the bridge 2, 3, the
/// store 4, with hikers as units, each on its own occupancy plane so that
/// each is distinct. The script sets each hiker's direction step by step:
/// that is setting the situation up, not evidence;</item>
/// <item><b>observers</b>, each reading part of the world through a port: a
/// counting strip over the bridge's far edge that counts crossings and sees
/// neither who nor which way, a tracker that counts one hiker's crossings,
/// and a watcher that sees someone arrive at the store and not who;</item>
/// <item>a <b>story</b>, holding open values about each hiker, crossings, which
/// side of the bridge it ended on and whether it reached the store, narrowed
/// only from what the observers expose, through authored map knowledge: the
/// store lies only across the bridge.</item>
/// </list>
/// No structure reads another's internals and nothing is told to the story
/// from outside. The test judges the story against what happened, afterwards.</summary>
public sealed class BridgeObserverTests
{
    private const int Sites = 5, Home = 0, Store = 4, Cap = 3;
    private static readonly (int, int)[] ForwardRows = [(3, 4), (2, 3), (1, 2), (0, 1)];
    private const int ForwardAcross = 1, BackAcross = 2;
    private static readonly Shape Count = Shape.Unary(Cap);
    private static readonly Field CountField = new(Count, 0, 1, 2);
    private static readonly Field Direction = new(Sides.Word, 0, 1);

    /// <summary>Adds one when the observed bit is set. Roles: seen (read), count.</summary>
    private static readonly TransitionLaw Tally = TransitionLaw.FromFunction(
        [Shape.Bit, Count], s => s[0] == 1 && s[1] < Cap ? [1, s[1] + 1] : s, new Accounting(true, new Unchanged(0), new NeverDecreases(1)));

    /// <summary>Latches once any watched bit is set. Roles: seen (read), seen (read), latched.</summary>
    private static readonly TransitionLaw Notice = TransitionLaw.FromFunction(
        [Shape.Bit, Shape.Bit, Shape.Bit], s => [s[0], s[1], s[0] | s[1] | s[2]], new Accounting(true, new Unchanged(0), new Unchanged(1), new NeverDecreases(2)));

    private sealed record World(Engine Engine, Store Path, Store Control, Port[][] Crossings, Port AtStore);

    private sealed record Observer(Engine Engine, Store State, Port Shows);

    /// <summary>The road, hikers starting at home, and per hiker a move wiring
    /// each way that records what its rows did. The world exposes, per hiker,
    /// whether a step crossed the bridge's far edge each way, and who is at the
    /// store.</summary>
    private static World Road(int hikers)
    {
        var path = new Store(hikers, Sites);
        var control = new Store(2 * hikers, 1);
        var engine = new Engine();
        var crossings = new Port[hikers][];
        for (var h = 0; h < hikers; h++)
        {
            path.Write(h, Home, true);
            var direction = new Field(Sides.Word, 2 * h, 2 * h + 1);
            direction.Write(control, 0, Sides.AtFromAfter);
            var forward = new Wiring(Paths.Advance(Sides.With), new Binding(path, h), new Binding(control, 2 * h, 2 * h + 1), new Binding(path, h));
            foreach (var (from, to) in ForwardRows) forward.Add(from, 0, to);
            var back = new Wiring(Paths.Advance(Sides.Against), new Binding(path, h), new Binding(control, 2 * h, 2 * h + 1), new Binding(path, h));
            foreach (var (from, to) in ForwardRows.Reverse()) back.Add(to, 0, from);
            var (forwardMoved, backMoved) = (forward.Record().Changed, back.Record().Changed);
            engine.Add(forward).Add(back);
            crossings[h] = [forward.Rows.Publish([ForwardAcross], [forwardMoved]), back.Rows.Publish([BackAcross], [backMoved])];
        }
        engine.Hold(path, control);
        return new World(engine, path, control, crossings, path.Publish([Store], [.. Enumerable.Range(0, hikers)]));
    }

    /// <summary>Counts crossings of the bridge's far edge, either way, by any
    /// hiker it is wired to. It cannot tell who crossed or which way.</summary>
    private static Observer Strip(IEnumerable<Port> crossings)
    {
        var count = new Store(Cap, 1);
        var engine = new Engine().Hold(count);
        foreach (var port in crossings)
        {
            var tally = new Wiring(Tally, new Binding(port, 0), new Binding(count, 0, 1, 2));
            tally.Add(0, 0);
            engine.Add(tally);
        }
        return new Observer(engine, count, count.Publish([0], [0, 1, 2]));
    }

    /// <summary>Latches when anyone is at the store, without telling who.</summary>
    private static Observer Watcher(Port atStore, int hikers)
    {
        var seen = new Store(1, 1);
        var notice = new Wiring(Notice, new Binding(atStore, 0), new Binding(atStore, hikers > 1 ? 1 : 0), new Binding(seen, 0));
        notice.Add(0, 0, 0);
        return new Observer(new Engine().Add(notice), seen, seen.Publish([0], [0]));
    }

    /// <summary>What the story holds about each hiker: crossings, whether it
    /// ended across the bridge, and whether it reached the store. All open. The
    /// last two share one set of planes: across at position h, reached at
    /// position hikers + h.</summary>
    private sealed record Story(Engine Engine, Supports Crossings, Supports Across, Supports Reached);

    /// <summary>A story about <paramref name="hikers"/> hikers, told by a strip
    /// wired to all of them, and optionally a watcher at the store and a
    /// tracker on hiker 0. Its relations are map knowledge: a hiker who ends
    /// across the bridge crossed it an odd number of times, and one who
    /// reached the store crossed it at least once.</summary>
    private static Story Tell(int hikers, Observer strip, Observer? watcher = null, Observer? tracker = null)
    {
        var counts = new Store(Cap, hikers);
        var crossings = Supports.On(counts, CountField);
        var bits = new Store(1, 2 * hikers);
        var flags = Supports.On(bits, Field.Bit(0));
        var engine = new Engine();

        var total = new Wiring(
            RelationLaw.FromPredicate([Count, .. Enumerable.Repeat(Count, hikers)], t => t[0] == t.Skip(1).Sum()),
            [new Binding(strip.Shows, 0, 1, 2), .. Enumerable.Repeat(crossings.Variable, hikers)]);
        total.Add([0, .. Enumerable.Range(0, hikers)]);
        var across = new Wiring(RelationLaw.FromPredicate([Count, Shape.Bit], t => t[1] == t[0] % 2), crossings.Variable, flags.Variable);
        var reached = new Wiring(RelationLaw.FromPredicate([Count, Shape.Bit], t => t[1] == 0 || t[0] >= 1), crossings.Variable, flags.Variable);
        for (var h = 0; h < hikers; h++)
        {
            across.Add(h, h);
            reached.Add(h, hikers + h);
        }
        engine.Add(total).Add(across).Add(reached);
        if (watcher is not null)
        {
            var anyone = new Wiring(
                RelationLaw.FromPredicate([Shape.Bit, .. Enumerable.Repeat(Shape.Bit, hikers)], t => t[0] == (t.Skip(1).Any(r => r == 1) ? 1 : 0)),
                [new Binding(watcher.Shows, 0), .. Enumerable.Repeat(flags.Variable, hikers)]);
            anyone.Add([0, .. Enumerable.Range(hikers, hikers)]);
            engine.Add(anyone);
        }
        if (tracker is not null)
        {
            var follows = new Wiring(Settling.Same(Count), new Binding(tracker.Shows, 0, 1, 2), crossings.Variable);
            follows.Add(0, 0);
            engine.Add(follows);
        }
        return new Story(engine, crossings, flags, flags);
    }

    /// <summary>Runs the world one step per entry, each hiker taking the given
    /// direction, then lets every observer read that step.</summary>
    private static void Walk(World world, Observer[] observers, params int[][] steps)
    {
        foreach (var step in steps)
        {
            for (var h = 0; h < step.Length; h++) new Field(Sides.Word, 2 * h, 2 * h + 1).Write(world.Control, 0, step[h]);
            world.Engine.Step();
            foreach (var observer in observers) observer.Engine.Step();
        }
    }

    private static int[][] Repeat(int times, params int[] directions) => [.. Enumerable.Repeat(directions, times)];

    private static void Settle(Engine engine)
    {
        for (var round = 0; round < 16; round++)
            if (engine.Step().Steps.All(step => step.Changed.None)) return;
        throw new InvalidOperationException("Narrowing did not come to rest.");
    }

    private static ulong Values(params int[] values) => values.Aggregate(0UL, (mask, v) => mask | 1UL << v);

    private static readonly int F = Sides.With, B = Sides.Against, S = Sides.AtFromAfter;

    /// <summary>One hiker turns back before the bridge. The strip counts
    /// nothing, so the story knows the hiker is on the home side and never
    /// reached the store, though nobody saw where it turned.</summary>
    [Fact]
    public void NoCrossingMeansTheStoreWasNotReached()
    {
        var world = Road(1);
        var strip = Strip(world.Crossings[0]);
        Walk(world, [strip], [.. Repeat(2, F), .. Repeat(2, B)]);
        var story = Tell(1, strip);

        Settle(story.Engine);

        Assert.Equal(Values(0), story.Crossings.Values(0));
        Assert.Equal((Values(0), Values(0)), (story.Across.Values(0), story.Reached.Values(1)));
    }

    /// <summary>A hiker who went to the store and back, and one who turned
    /// after the bridge and came back, leave the strip the same two crossings.
    /// With only the strip the stories are identical: back on the home side,
    /// and the store possible but not established. A watcher at the store tells
    /// them apart.</summary>
    [Fact]
    public void TheStripCannotTellTheStoreFromTurningPastTheBridgeAndAWatcherCan()
    {
        Story Run(int outward, bool watch)
        {
            var world = Road(1);
            var strip = Strip(world.Crossings[0]);
            var watcher = Watcher(world.AtStore, 1);
            Walk(world, [strip, watcher], [.. Repeat(outward, F), .. Repeat(outward, B)]);
            var story = Tell(1, strip, watch ? watcher : null);
            Settle(story.Engine);
            return story;
        }

        var toStore = Run(outward: 4, watch: false);
        var pastBridge = Run(outward: 3, watch: false);

        Assert.Equal((Values(2), Values(0), Values(0, 1)), (toStore.Crossings.Values(0), toStore.Across.Values(0), toStore.Reached.Values(1)));
        Assert.Equal(
            (toStore.Crossings.Values(0), toStore.Across.Values(0), toStore.Reached.Values(1)),
            (pastBridge.Crossings.Values(0), pastBridge.Across.Values(0), pastBridge.Reached.Values(1)));
        Assert.Equal(Values(1), Run(outward: 4, watch: true).Reached.Values(1));
        Assert.Equal(Values(0), Run(outward: 3, watch: true).Reached.Values(1));
    }

    /// <summary>Two hikers, one to the store and back and one staying home. The
    /// strip's two crossings could be one each, or two by either, so neither
    /// hiker's side is known. A tracker on the first settles it, and with it
    /// the second: two crossings by the first leaves none for the other.</summary>
    [Fact]
    public void TwoHikersShareOneCountUntilATrackerSplitsIt()
    {
        Story Run(bool track)
        {
            var world = Road(2);
            var strip = Strip(world.Crossings.SelectMany(ports => ports));
            var tracker = Strip(world.Crossings[0]);
            Walk(world, [strip, tracker], [.. Repeat(4, F, S), .. Repeat(4, B, S)]);
            var story = Tell(2, strip, tracker: track ? tracker : null);
            Settle(story.Engine);
            return story;
        }

        var shared = Run(track: false);
        Assert.Equal((Values(0, 1, 2), Values(0, 1, 2)), (shared.Crossings.Values(0), shared.Crossings.Values(1)));
        Assert.Equal((Values(0, 1), Values(0, 1)), (shared.Across.Values(0), shared.Across.Values(1)));

        var tracked = Run(track: true);
        Assert.Equal((Values(2), Values(0)), (tracked.Crossings.Values(0), tracked.Crossings.Values(1)));
        Assert.Equal((Values(0), Values(0)), (tracked.Across.Values(0), tracked.Across.Values(1)));
        Assert.Equal(Values(0), tracked.Reached.Values(3));
    }

    /// <summary>A tracker's trail of one hiker: for each moment, a stamp, and the
    /// hiker's site, step word and side of the bridge, all open; and bits for
    /// turning back and for passing the bridge. A clock of its own says which
    /// moment is being recorded; the world is read only through a port showing
    /// the hiker's occupancy at every site.</summary>
    private sealed record Trail(Engine Engine, Supports Site, Supports Step, Supports Side, Supports Turned, Supports Passed);

    private static Trail Follow(World world, int hiker, int moments)
    {
        const int Bridge = 2;
        var lanes = world.Path.Publish([.. Enumerable.Range(0, Sites)], [hiker]);
        var words = Odv.Create(moments);
        var stepCode = new Field(Sides.Word, words.AddPlane(), words.AddPlane());
        var siteCode = new Field(Shape.Binary(Sites), words.AddPlane(), words.AddPlane(), words.AddPlane());
        var stampPlanes = Enumerable.Range(0, moments).Select(_ => words.AddPlane()).ToArray();
        var stamp = new Field(Shape.Unary(moments), stampPlanes);
        for (var t = 0; t < moments; t++) stamp.Write(words, t, t);
        var side = Supports.On(words, Sides.Field);
        var step = Supports.On(words, stepCode);
        var site = Supports.On(words, siteCode);
        var turned = Supports.On(new Store(1, moments), Field.Bit(0));
        var passed = Supports.On(new Store(1, moments), Field.Bit(0));
        var clock = new Store(moments, 1);
        var road = ForwardRows.Select(row => (row.Item2 - 1, row.Item2)).ToArray();

        var seen = new Wiring(Paths.Seen(Sites, moments),
            [new Binding(clock, [.. Enumerable.Range(0, moments)]), new Binding(words, stampPlanes), .. Enumerable.Repeat(new Binding(lanes, 0), Sites), site.Variable]);
        var sideOf = new Wiring(Paths.SideOf(Sites, road, Bridge), site.Variable, side.Variable);
        var steps = new Wiring(Paths.Step(Sites, road), site.Variable, site.Variable, step.Variable);
        var turns = new Wiring(Sides.TurnsBack, step.Variable, step.Variable, turned.Variable);
        var passes = new Wiring(Sides.Passes, side.Variable, side.Variable, passed.Variable);
        for (var t = 0; t < moments; t++)
        {
            seen.Add([0, t, .. Enumerable.Range(0, Sites), t]);
            sideOf.Add(t, t);
            if (t >= 1) steps.Add(t - 1, t, t);
            if (t >= 2) turns.Add(t - 1, t, t);
            if (t >= 1 && t + 1 < moments) passes.Add(t - 1, t + 1, t);
        }
        foreach (var bits in new[] { turned, passed }) bits.Restrict(0, 0b01);
        turned.Restrict(1, 0b01);
        passed.Restrict(moments - 1, 0b01);
        var tick = new Wiring(Settling.Clock(moments), new Binding(clock, [.. Enumerable.Range(0, moments)]));
        tick.Add(0);
        var engine = new Engine().Add(seen).Add(sideOf).Add(steps).Add(turns).Add(passes).Add(tick);
        return new Trail(engine, site, step, side, turned, passed);
    }

    private static int?[] Settled(Supports values) =>
        [.. Enumerable.Range(0, values.Surface.Count).Select(t => System.Numerics.BitOperations.PopCount(values.Values(t)) == 1
            ? System.Numerics.BitOperations.TrailingZeroCount(values.Values(t)) : (int?)null)];

    /// <summary>The hiker walks three sites out, over the bridge, and back
    /// home. The tracker records only occupancy, one moment per step, by its
    /// own clock. From its own trail it reads its own step words, sees the turn
    /// where a step with the road is followed by one against it, and sees the
    /// bridge passed twice, out and back, though the moments on the bridge
    /// leave their edge open. Nothing was told to it but what the port showed.</summary>
    [Fact]
    public void ATrackersTrailShowsTheTurnAndBothCrossingsFromEvidenceAlone()
    {
        const int Moments = 7;
        var world = Road(1);
        var trail = Follow(world, 0, Moments);

        trail.Engine.Step();
        foreach (var step in new[] { F, F, F, B, B, B })
        {
            Walk(world, [], [step]);
            trail.Engine.Step();
        }
        Settle(trail.Engine);

        Assert.Equal([0, 1, 2, 3, 2, 1, 0], Settled(trail.Site));
        Assert.Equal([null, F, F, F, B, B, B], Settled(trail.Step));
        Assert.Equal([0, 0, 0, 0, 1, 0, 0], Settled(trail.Turned));
        Assert.Equal([0, 0, 1, 0, 1, 0, 0], Settled(trail.Passed));
        Assert.Equal(Values(Sides.AtFromBefore, Sides.AtFromAfter), trail.Side.Values(2));
    }

    /// <summary>No structure has a god's view. The story binds only its own
    /// stores and the ports observers expose; it holds no binding to the world.
    /// Every observer reaches the world only through ports.</summary>
    [Fact]
    public void TheStoryKnowsOnlyWhatObserversExpose()
    {
        var world = Road(2);
        var strip = Strip(world.Crossings.SelectMany(ports => ports));
        var watcher = Watcher(world.AtStore, 2);
        var story = Tell(2, strip, watcher);

        Assert.All(story.Engine.Wirings.SelectMany(w => w.Bindings), binding =>
            Assert.True(binding.Surface is Port port ? port.Owner.Local != world.Engine : story.Engine.Stores.Contains(binding.Surface.Home!)));
        Assert.All(new[] { strip, watcher }.SelectMany(o => o.Engine.Wirings).SelectMany(w => w.Bindings), binding =>
            Assert.True(binding.Surface is Port || binding.Surface.Home!.Local != world.Engine));
        Assert.Throws<ArgumentException>(() => story.Engine.Add(new Wiring(Settling.Same(Shape.Bit), new Binding(world.Path, 0), new Binding(world.Path, 1))));
    }
}
