using Xunit;

namespace Gojo.EncodingB.Tests;

public sealed class LearningAndRungTests
{
    private static LearnedTransitionTable Observe(int ticks, int seed, LearnedTransitionTable? into = null)
    {
        var learned = into ?? new LearnedTransitionTable("passage (learned)", Passage.Roles, admitThreshold: 2, unsupportedAfter: 8);
        var random = new Random(seed);
        var rows = Passage.Rows("handoff", [[0, 1, 2]]);
        for (var tick = 0; tick < ticks; tick++)
        {
            var tile = new Tile("valve", Passage.Grammar, 3);
            tile.Seed("occupied", (ulong)random.Next(4) switch { var b => (b & 1) | ((b & 2) << 1) });
            tile.Seed("open", (ulong)random.Next(2) << 1);
            learned.Witness(rows.Apply(tile, rows.AllRows), tick);
        }
        return learned;
    }

    [Fact]
    public void APassageLawIsLearnedBackFromWitnessedTransitions()
    {
        var early = Observe(3, seed: 11);
        Assert.True(Enumerable.Range(0, 8).Any(b => early.Predict(b).Open));
        var (none, why) = early.Crystallize(Passage.Signature, ConfluenceClass.ScheduledDescent);
        Assert.Null(none);
        Assert.Contains("still unknown", why);

        var learned = Observe(400, seed: 11);
        Assert.Equal(0, learned.UnknownCount);
        Assert.Equal(LearnedRead.Unsupported, learned.Read(3, 3));
        var (law, read) = learned.Crystallize(Passage.Signature, ConfluenceClass.ScheduledDescent);
        Assert.NotNull(law);
        Assert.Equal("crystallized", read);
        Assert.Equal(Passage.Law.Bytes(), law.Bytes());
        Assert.Equal(Passage.Law.ContentHash, law.ContentHash);
        Assert.Contains("learned from 400 witnessed transitions", law.Provenance);
        Assert.NotEmpty(learned.WitnessesOf(3, 6));
    }

    [Fact]
    public void ALearnedTableThatBreaksConservationIsRefused()
    {
        var learned = Observe(400, seed: 5);
        learned.Witness(3, 7, tick: 1000);
        learned.Witness(3, 7, tick: 1001);
        var (law, read) = learned.Crystallize(Passage.Signature, ConfluenceClass.ScheduledDescent);
        Assert.Null(law);
        Assert.Contains("accounting signature", read);
    }

    [Fact]
    public void RouteStrengthSaturates()
    {
        var tile = new Tile("r", TensionField.Grammar(1), 2);
        tile.AddRowSet("routes", "from", "to").Add(0, 1);
        var routes = new RouteTable(tile, "routes");
        for (var i = 0; i < 10; i++) routes.Revise(0, relieved: true);
        Assert.Equal(RouteTable.MaxStrength, routes.Strength(0));
        for (var i = 0; i < 10; i++) routes.Revise(0, relieved: false);
        Assert.Equal(0, routes.Strength(0));
    }

    [Fact]
    public void PresenceRungIsAGaloisPair()
    {
        var rung = Rungs.Presence(4);
        Assert.Equal(0, rung.Abstract(0));
        Assert.Equal(1, rung.Abstract(3));
        Assert.Equal(0b11110UL, rung.Concretize(1));
        Assert.True(rung.Consistent(Bits.Of(2), Bits.Of(1)));
        Assert.False(rung.Consistent(Bits.Of(0), Bits.Of(1)));
        Assert.Equal(0b00110UL, rung.Refine(Bits.Of(1), 0b00111));
        for (var fine = 0; fine <= 4; fine++) Assert.True(Bits.Get(rung.Concretize(rung.Abstract(fine)), fine));
    }

    [Fact]
    public void PoolsBuildCoarseRungs()
    {
        Assert.Equal(0b011UL, Rungs.OrPool(0b0000_0110, [0b0011, 0b1100, 0b11_0000]));
        var exact = Rungs.SumPool([40, 260], unit: 100);
        Assert.True(exact.IsExact);
        Assert.Equal((3, 0), (exact.Coarse, exact.Residue));
        Assert.Equal(300, exact.Value);
    }

    [Fact]
    public void HundredsAloneIsNotThreeHundred()
    {
        var coarse = CoarseRead.Witness(3, 100);
        Assert.False(coarse.IsExact);
        Assert.Null(coarse.Value);
        Assert.Equal((300, 399), coarse.Range);
        var exact = CoarseRead.Exact(300, 100);
        Assert.Equal((300, 300), exact.Range);
    }
}
