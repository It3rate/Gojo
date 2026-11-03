using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class StrokeGraphTests
{
    [Theory]
    [InlineData("H", 6, 2, 0, "H")]
    [InlineData("A", 5, 3, 1, "A")]
    [InlineData("E", 6, 3, 0, "E")]
    [InlineData("N", 4, 2, 0, "N,Z")]
    [InlineData("Z", 4, 2, 0, "N,Z")]
    public void Read_comes_from_shared_sites_and_runs(string example, int sites, int joins, int cycles, string candidates)
    {
        var graph = CapitalStudy.Build(example);
        Assert.Equal((sites, joins, 1, cycles), graph.Incidence());
        Assert.Equal(candidates, string.Join(",", CapitalStudy.Candidates(graph)));
        Assert.All(Enumerable.Range(0, graph.RunCount), i => Assert.True(graph.Run(i).IsChain));
        Assert.Equal(0, graph.AxisCount);
    }

    [Fact]
    public void Input_names_site_numbers_run_order_and_traversal_direction_are_unnecessary()
    {
        var h = new StrokeGraph([new(4,[12,9]), new(4,[11,12,13]), new(4,[8,9,10])], [], []);
        Assert.Equal(new[] { "H" }, CapitalStudy.Candidates(h));
        // Same vertices, run sizes, shared site count and cycle count, but
        // the bar meets the END of one run. The detailed mapping must fail.
        var impostor = new StrokeGraph([new(4,[12,8]), new(4,[11,12,13]), new(4,[8,9,10])], [], []);
        Assert.Equal(h.Incidence(), impostor.Incidence());
        Assert.Empty(CapitalStudy.Candidates(impostor));
    }

    [Fact]
    public void Only_the_ground_links_separate_these_N_and_Z_templates()
    {
        var n = CapitalStudy.Build("N", ground: true);
        var z = CapitalStudy.Build("Z", ground: true);
        Assert.Equal(n.PenBytes(), z.PenBytes());
        for (var i = 0; i < n.RunCount; i++) Assert.Equal(n.Run(i).SiteBytes(), z.Run(i).SiteBytes());
        Assert.NotEqual(n.GroundBytes(), z.GroundBytes());
        Assert.Equal(new[] { "N" }, CapitalStudy.Candidates(n));
        Assert.Equal(new[] { "Z" }, CapitalStudy.Candidates(z));
        var rebound = new StrokeGraph([n.Run(0), n.Run(1), n.Run(2)], n.PenUses(), z.GroundLinks());
        Assert.Equal(new[] { "Z" }, CapitalStudy.Candidates(rebound));
        var inconsistent = new StrokeGraph([n.Run(0), n.Run(1), n.Run(2)], n.PenUses(), [[1,0],[1,1]]);
        Assert.Empty(CapitalStudy.Candidates(inconsistent));
    }

    [Fact]
    public void Alternative_pen_history_does_not_supply_the_missing_Q_R_comparison()
    {
        foreach (var hand in new[] { false, true })
        {
            var graph = CapitalStudy.Build("E", otherHand: hand, partialE: true);
            Assert.Equal(new[] { "E" }, CapitalStudy.Candidates(graph));
            var spine = graph.Run(0);
            Assert.Equal(new[] { 2,0,1 }, spine.Sites());
            Assert.Equal(LandmarkRead.Before, spine.Compare(0, 2));
            Assert.Equal(LandmarkRead.After, spine.Compare(0, 2, reversed: true));
            Assert.Equal(LandmarkRead.Unresolved, spine.Compare(1, 2));
        }
        Assert.NotEqual(CapitalStudy.Build("E").PenBytes(), CapitalStudy.Build("E", otherHand: true).PenBytes());
        Assert.Equal(LandmarkRead.Before, CapitalStudy.Build("E").Run(0).Compare(1, 2));
    }

    [Fact]
    public void Partial_order_is_transitive_and_preserves_outside_identity_and_contradiction()
    {
        var order = new LandmarkRun(2, [2,0,1], [[0,1],[1,2]]);
        Assert.Equal(LandmarkRead.Before, order.Compare(0,2));
        Assert.Equal(LandmarkRead.SameSite, order.Compare(2,2));
        Assert.Equal(LandmarkRead.Outside, order.Compare(1,3));
        var cycle = new LandmarkRun(2, [2,0,1], [[0,1],[1,0]]);
        Assert.Equal(LandmarkRead.Conflict, cycle.Compare(0,2));
        var model = CapitalStudy.Build("E");
        Assert.Empty(CapitalStudy.Candidates(new StrokeGraph([cycle,model.Run(1),model.Run(2),model.Run(3)], [], [])));
    }
}
