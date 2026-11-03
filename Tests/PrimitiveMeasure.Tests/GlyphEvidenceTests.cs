namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class GlyphEvidenceTests
{
    [Fact]
    public void Withdrawing_one_order_preserves_both_possible_completions()
    {
        var full = GlyphEvidence.Examples[0].Body.Read();
        Assert.Equal(new GlyphClues(1,1,false,true,true),full);
        Assert.Equal(["b"],GlyphEvidence.Candidates(full));
        Assert.Equal(["b","p"],GlyphEvidence.Candidates(full with { TailAbove=null }));
        Assert.Equal(["b","d"],GlyphEvidence.Candidates(full with { BowlRight=null }));
        Assert.Equal(["b","d","p","q"],GlyphEvidence.Candidates(full with { BowlRight=null,TailAbove=null }));
        Assert.Equal(["b","6"],GlyphEvidence.Candidates(full with { TailBends=null }));
        var missingLoop = full with { Loops=null,TailAbove=null };
        var survivors = GlyphEvidence.Admitted(missingLoop);
        Assert.Equal(["b","p"],survivors.Select(e=>e.Name));
        Assert.All(survivors,c=>Assert.Equal(1,c.Body.Read().Loops));
        Assert.Null(missingLoop.Loops);
        Assert.Equal(full,GlyphEvidence.Examples[0].Body.Read());
    }

    [Fact]
    public void Cycles_are_computed_and_even_the_full_local_read_can_collide()
    {
        var round = GlyphEvidence.Examples.Single(e=>e.Name=="0").Body.Read();
        Assert.Equal(["o","0","O"],GlyphEvidence.Candidates(round));
        Assert.Equal(["8"],GlyphEvidence.Candidates(new(2,null,null,null,null)));
        Assert.Equal(["c"],GlyphEvidence.Candidates(new(0,null,null,null,null)));
        Assert.Empty(GlyphEvidence.Candidates(new(3,null,null,null,null)));
        Assert.Equal(GlyphEvidence.Examples.Count,GlyphEvidence.Candidates(default).Length);
    }

    [Fact]
    public void A_held_out_double_bowl_defeats_the_catalogue_prediction_of_exactly_one_loop()
    {
        var actual = GlyphEvidence.Observations.Last().Body.Read();
        Assert.Equal(new GlyphClues(2,1,false,true,true),actual);
        var partial = actual with { Loops=null,TailAbove=null };
        Assert.True(partial.EndCountRequiresLoop);
        Assert.Equal(["b","p"],GlyphEvidence.Candidates(partial));
        Assert.Empty(GlyphEvidence.Candidates(actual));
    }

    [Fact]
    public void The_same_Y_can_merge_from_either_inlet_or_choose_one_branch()
    {
        var merge = GlyphJunction.YMerge(3);
        Assert.Equal(4,GlyphJunction.YMerge(1).Advance());
        Assert.Equal(4,GlyphJunction.YMerge(2).Advance());
        Assert.Equal(6,merge.Advance()); // first arrival accepted, second retained
        var branch = GlyphJunction.YBranch(4);
        Assert.Equal(merge.ArmBytes(),branch.ArmBytes());
        Assert.All(Enumerable.Range(0,3),i => Assert.Equal(merge.Centre,branch.Arrival(i)[1]));
        Assert.Equal((1u << 1) | (1u << 10),branch.Flow.Predict(branch.Flow.Observe(7)));
        Assert.Equal(1,branch.Advance());
        Assert.Equal(10,GlyphJunction.YBranch(12).Advance());
    }

    [Fact]
    public void A_through_run_at_T_allows_distinct_passages_over_identical_ink()
    {
        var corner = GlyphJunction.TCorners(1);
        var bar = GlyphJunction.TBar(4,false);
        var turn = GlyphJunction.TBar(1,true);
        Assert.Equal(corner.ArmBytes(),bar.ArmBytes());
        Assert.Equal(bar.ArmBytes(),turn.ArmBytes());
        Assert.Equal([0,1,1],bar.Arm(1)); // right half of the horizontal run, read back to the joint
        Assert.Equal(4,corner.Advance());
        Assert.Equal(4,GlyphJunction.TCorners(2).Advance());
        Assert.Equal(4,bar.Advance()); // held at stem
        Assert.Equal(2,GlyphJunction.TBar(1,false).Advance());
        Assert.Equal((1u << 2) | (1u << 12),turn.Flow.Predict(turn.Flow.Observe(7)));
        Assert.Equal(2,turn.Advance());
        Assert.Equal(12,GlyphJunction.TBar(9,true).Advance());
    }
}
