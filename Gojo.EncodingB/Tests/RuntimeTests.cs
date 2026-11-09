using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class RuntimeTests
{
    [Fact]
    public void ThermometerComparisonIsTwoBitTests()
    {
        for (var a = 0; a <= 10; a++)
            for (var b = 0; b <= 10; b++)
                Assert.Equal(a >= b + 2, Thermometer.ExceedsByTwo(Thermometer.Encode(a, 10), Thermometer.Encode(b, 10)));
        Assert.Equal(0b0111UL, Thermometer.Encode(3, 4));
        Assert.Equal(4, Thermometer.Decode(Thermometer.AddOne(0b0111)));
        Assert.Null(Thermometer.Decode(0b0101));
    }

    [Fact]
    public void BitSlicedUnitsMoveInAndOutOfEverySiteAtOnce()
    {
        ulong[] planes = [0b0110, 0b0100, 0b0000];
        var overflow = Thermometer.AddOneAt(planes, 0b1111);
        Assert.Equal(0UL, overflow);
        Assert.Equal([0b1111UL, 0b0110UL, 0b0100UL], planes);
        Assert.Equal(0b0100UL, Thermometer.AddOneAt(planes, 0b0100));
        var underflow = Thermometer.RemoveOneAt(planes, 0b1111);
        Assert.Equal(0UL, underflow);
        Assert.Equal([0b0110UL, 0b0100UL, 0b0000UL], planes);
    }

    [Fact]
    public void ChangeEntersAsTensionAndOpposedTensionIsNotNetted()
    {
        var tile = new Tile("sites", TensionField.Grammar(4), 2);
        var tension = new TensionField(tile);
        var record = tension.Ingest(0, 6);
        Assert.Equal((4, 2), (record.Accepted, record.Overflow));
        Assert.Equal(2, tension.Overflow(0));
        tension.Absorb(tile.Allocated);
        Assert.Equal((4, 0), (tension.Magnitude(0), tension.Positive(0)));

        tension.Ingest(1, 2);
        tension.Ingest(1, -2);
        tension.Absorb(Bits.Of(1));
        Assert.Equal((2, 0, 2), (tension.Magnitude(1), tension.Positive(1), tension.Negative(1)));
        tension.Absorb(Bits.Of(1));
        Assert.Equal((0, 0, 0), (tension.Magnitude(1), tension.Positive(1), tension.Negative(1)));

        tension.Ingest(1, -3);
        tension.Absorb(Bits.Of(1));
        Assert.Equal(3, tension.Negative(1));
    }

    [Fact]
    public void QuietIsNotOptimalAndTheWitnessNamesTheMissingRoute()
    {
        var grammar = SiteGrammar.Create("gaps").Thermometer("G", "value", 4, "gap", material: true).Build();
        var tile = new Tile("gaps", grammar, 3);
        for (var site = 0; site < 3; site++) tile.SeedMagnitude("G", site, site + 1);
        var pairs = tile.AddRowSet("pairs", "a", "b");
        pairs.Add(0, 1);
        pairs.Add(1, 2);
        var balance = new GapBalance(tile, "G", "pairs");
        var settled = balance.Settle();
        Assert.True(settled.Quiet);
        Assert.Empty(settled.Moves);
        Assert.Equal(2, settled.Spread);
        Assert.Equal([(0, 2)], settled.MissingRoutes);

        pairs.Add(0, 2);
        settled = balance.Settle();
        Assert.True(settled.Quiet);
        Assert.Equal([2, 2, 2], Enumerable.Range(0, 3).Select(balance.Gap));
        Assert.All(settled.Moves, m => Assert.True(m.SquareSumAfter <= m.SquareSumBefore - 2));
        Assert.Empty(settled.MissingRoutes);
    }

    private static Runtime MakeRoutedRuntime()
    {
        var tile = new Tile("routes", TensionField.Grammar(4), 3, ["source", "full", "empty"]);
        tile.SeedMagnitude("M", 0, 4);
        tile.SeedMagnitude("M", 1, 4);
        var routes = tile.AddRowSet("routes", "from", "to");
        routes.Add(0, 1);
        routes.Add(0, 2);
        return new Runtime(new TensionField(tile), new RouteTable(tile, "routes"));
    }

    [Fact]
    public void RoutesLearnFromAcknowledgedReliefAndTheAccountsBalance()
    {
        var runtime = MakeRoutedRuntime();
        var first = runtime.Tick(TickInput.Change((0, 2)));
        Assert.Single(first.Transfers.Blocked);
        Assert.Equal(1, runtime.Routes!.Strength(0));

        var second = runtime.Tick(TickInput.Empty);
        Assert.Equal(1, second.Transfers.Accepted.Single().Route);
        var third = runtime.Tick(TickInput.Empty);
        Assert.Single(third.Transfers.Accepted);
        var fourth = runtime.Tick(TickInput.Empty);
        Assert.Empty(fourth.Transfers.Offered);

        Assert.Equal((1, 4), (runtime.Routes.Strength(0), runtime.Routes.Strength(1)));
        Assert.Equal(2, runtime.Tension.Magnitude(2));
        Assert.All(new[] { first, second, third, fourth }, w => Assert.Empty(w.HardViolations));
        Assert.True(second.After < first.After);
        Assert.True(fourth.After.Quiet);
    }

    [Fact]
    public void ReplayFromACheckpointRematerializesEveryTick()
    {
        var runtime = MakeRoutedRuntime();
        var checkpoint = runtime.Checkpoint();
        TickInput[] inputs = [TickInput.Change((0, 2)), TickInput.Empty, TickInput.Change((2, -1)), TickInput.Empty];
        var original = inputs.Select(runtime.Tick).ToArray();
        var finalTile = runtime.Tile.Snapshot();
        var finalRoutes = runtime.Routes!.Snapshot();

        var replayed = runtime.Replay(checkpoint, inputs);
        Assert.Equal(finalTile.Stored, runtime.Tile.Snapshot().Stored);
        Assert.Equal(finalRoutes, runtime.Routes.Snapshot());
        Assert.Equal(original.Select(w => (w.Transfers.Accepted.Count, w.Transfers.Blocked.Count, w.After)),
            replayed.Select(w => (w.Transfers.Accepted.Count, w.Transfers.Blocked.Count, w.After)));
    }

    [Fact]
    public void EnergyTiersCompareLexicographically()
    {
        Assert.True(new Energy(0, 0, 100, 100) < new Energy(0, 1, 0, 0));
        Assert.True(new Energy(1, 0, 0, 0) > new Energy(0, 99, 99, 99));
        Assert.True(new Energy(0, 0, 0, 5).Quiet);
    }

    [Fact]
    public void RuntimeLawsSettleWhateverWorkspacesLookAt()
    {
        var grammar = TensionField.Grammar(2, b => b
            .Plane("occupied", "value", "empty", "occupied", material: true)
            .Plane("open", "control", "gate reads closed", "gate reads open"));
        var tile = new Tile("plant", grammar, 3);
        tile.Seed("occupied", "100");
        var passage = new LawRows("handoff", Passage.Law, grammar, ["occupied", "open", "occupied"], [[0, 1, 2]]);
        var runtime = new Runtime(new TensionField(tile), transitions: [passage]);
        var looker = new PredictionWorkspace("looker", tile, SelectionMachines.None(SelectionScope.Sites(tile)));

        runtime.Tick(TickInput.Empty);
        Assert.Equal("100", Bits.Format(tile.Read("occupied"), 3));
        var tick = runtime.Tick(new TickInput([], [("open", 1, true)]));
        Assert.Equal("001", Bits.Format(tile.Read("occupied"), 3));
        Assert.True(tick.Transitions.Single().Changed);
        Assert.Equal(0UL, looker.Sel);
    }
}
