using System.Numerics;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrnamentCampaignScienceTests
{
    private static ulong Marks(params int[] sites) => sites.Aggregate(0UL, (mask, s) => mask | 1UL << s);
    private static CampaignEpisode Fixture(string goal, ulong seed, ulong protect = 0) =>
        new(goal, "fixture", "unit-only", 0, new RailInput(12, seed, seed, protect, 3, 0, 20, 12, 24));

    private static CampaignScore Judge(CampaignEpisode episode, ulong final)
    {
        var input = episode.Input;
        var stock = input.Stock + BitOperations.PopCount(input.Initial) - BitOperations.PopCount(final);
        return CampaignScience.Evaluate(episode, new(final, stock, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void EqualizeAndFillCanRequireDifferentAmountsOfInkOnTheSameSeed()
    {
        var three = Marks(0, 3, 8);
        Assert.True(Judge(Fixture("balance", three), Marks(0, 4, 8)).Exact);
        Assert.False(Judge(Fixture("balance", three), Marks(0, 3, 6, 9)).Exact);
        Assert.True(Judge(Fixture("infill", three), Marks(0, 3, 6, 9)).Exact);
        Assert.False(Judge(Fixture("infill", three), Marks(0, 4, 8)).Exact);
    }

    [Fact]
    public void MirrorPreservesTheSourceAndTheInterveningGround()
    {
        var episode = Fixture("mirror", Marks(1, 4));
        Assert.True(Judge(episode, Marks(1, 4, 7, 10)).Exact);
        Assert.False(Judge(episode, Marks(4, 7)).Exact);
        Assert.False(Judge(episode, 0xFFF).Exact);
    }

    [Fact]
    public void SeamReadsTheWrapAndPenalizesUnnecessaryPrivateInteriorChanges()
    {
        var episode = Fixture("seam", Marks(0, 5, 9));
        Assert.True(Judge(episode, episode.Input.Initial).Exact);
        Assert.Equal(900, Judge(episode, Marks(0, 9)).Utility);
        Assert.False(Judge(episode, Marks(0, 5, 10)).Exact);
    }

    [Fact]
    public void ProtectedMaterialAndActualTreatmentCapacityAreCheckedIndependently()
    {
        var episode = Fixture("seam", Marks(0, 5, 9), 1UL << 5);
        Assert.Equal(0, Judge(episode, Marks(0, 9)).Utility);
        var depleted = episode.Input with { Stock = 0, EditBudget = 0 };
        var idle = new RailResult(depleted.Initial, 0, 0, 0, 0, 0, 0);
        Assert.True(CampaignScience.Evaluate(episode, idle, depleted).Exact);
        Assert.True(CampaignScience.Evaluate(episode, idle with { Stock = 1 }, depleted).Violations > 0);
        Assert.True(CampaignScience.Evaluate(episode, idle with { Attempts = 1 }, depleted).Violations > 0);
    }

    [Fact]
    public void AllRecipeViewsAndConflictingBriefsStayTogetherAcrossPartitions()
    {
        var canonical = new HashSet<(int Width, ulong Shape)>();
        foreach (var split in CampaignScience.Splits)
        {
            var parents = CampaignScience.Episodes(split, "mixed").GroupBy(e => e.Group).ToArray();
            foreach (var p in parents)
            {
                Assert.Equal(8, p.Count());
                var exemplar = p.First(x => x.Variant == 0).Input;
                Assert.All(p, e => Assert.Equal(e.Input.Initial, e.Input.Seed));
                Assert.All(p, e => Assert.Equal(0UL, e.Input.Protected & e.Input.InterventionXor));
                var variants = new HashSet<ulong>();
                ulong straight = exemplar.Seed;
                ulong reverse = 0;
                for (var i = 0; i < exemplar.Width; i++)
                    reverse |= ((straight >> i) & 1) << (exemplar.Width - i - 1);
                var mask = (1UL << exemplar.Width) - 1;
                for (int i = 0; i < exemplar.Width; i++)
                {
                    variants.Add(straight); variants.Add(reverse);
                    straight = ((straight << 1) & mask) | (straight >> (exemplar.Width - 1));
                    reverse = ((reverse << 1) & mask) | (reverse >> (exemplar.Width - 1));
                }
                Assert.True(canonical.Add((exemplar.Width, variants.Min())));
                Assert.Equal(4, p.Select(x => x.Goal).Distinct().Count());
            }
        }
        Assert.Equal(264, canonical.Count);
        Assert.Equal(240, CampaignScience.Hypotheses.Select(h => h.Id).Distinct().Count());
        Assert.Contains(CampaignScience.Hypotheses, h => h.Relation == "lower");
    }

    [Fact]
    public void ContrastsWeightParentsOnceAndTheInstrumentControlKeepsEveryCounterexample()
    {
        var h = CampaignScience.Hypotheses.First(x => x.Relation == "higher");
        var pairs = Enumerable.Range(0, 8).Select(i => new CampaignPair(i.ToString(), i == 0 ? 1000 : 0, 200)).ToList();
        pairs.AddRange(Enumerable.Repeat(pairs[0], 20));
        Assert.Equal("contrary", CampaignScience.Classify(h, pairs).Status);
        var control = CampaignScience.Hypotheses.First(x => x.Relation == "identical");
        var same = Enumerable.Range(0, 8).Select(i => new CampaignPair(i.ToString(), 500, 500)).ToList();
        Assert.Equal("supported", CampaignScience.Classify(control, same).Status);
        same.Add(new("0", 500, 500, false));
        Assert.Equal("contrary", CampaignScience.Classify(control, same).Status);
    }

    [Fact]
    public void LedgersRepeatExactlyAndRefuseToRewriteAnEarlierDesign()
    {
        var dir = Path.Combine(Path.GetTempPath(), "gojo-ornament-science-" + Guid.NewGuid().ToString("N"));
        try
        {
            CampaignScience.WriteManifest(dir);
            var seal = File.ReadAllText(Path.Combine(dir, "design.sha256"));
            CampaignScience.WriteManifest(dir);
            Assert.Equal(seal, File.ReadAllText(Path.Combine(dir, "design.sha256")));
            Assert.Equal(2113, File.ReadLines(Path.Combine(dir, "episodes.csv")).Count());
            Assert.Equal(241, File.ReadLines(Path.Combine(dir, "hypotheses.csv")).Count());
            File.AppendAllText(Path.Combine(dir, "hypotheses.csv"), "altered");
            Assert.Throws<InvalidOperationException>(() => CampaignScience.WriteManifest(dir));
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(dir)) File.Delete(file);
            Directory.Delete(dir);
        }
    }
}
