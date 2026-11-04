namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class BorderScoreTests
{
    [Fact]
    public void Same_wave_can_come_from_different_clocks_and_exposes_different_sources()
    {
        var a = BorderStudy.Direct();
        var b = BorderStudy.Alternating();
        Assert.Equal(new byte[]{9,1,11,1},a.VoiceBytes(0));
        Assert.Equal(new byte[]{9},b.ClockBytes()); // first voice, then second
        var direct = BorderStudy.Trace(a,4).SelectMany(beat => beat).ToArray();
        var coupled = BorderStudy.Trace(b,4).SelectMany(beat => beat).ToArray();
        Assert.Equal(direct.Select(e => e.Move),coupled.Select(e => e.Move));
        Assert.Equal([0,1,0,1],coupled.Select(e => e.Voice));
        Assert.Equal([0,0,1,0],coupled.Select(e => e.Phase));
        Assert.Equal(4,b.Predict().Beats);
        Assert.Equal(1,b.Predict().FirstLaps);
        Assert.Equal(2,b.Predict().SecondLaps);
        Assert.Equal(2,b.Predict().End.Uses(0,0));
        Assert.Equal(0,b.Predict().End.Uses(1,0));
    }

    [Fact]
    public void Joint_sampling_changes_the_strokes_and_unwinds_every_group_from_an_arbitrary_phase()
    {
        var score = BorderStudy.Together();
        Assert.Equal(2, score.Predict().Beats);
        Assert.Equal(2, score.Predict().End.Uses(0,0));
        BorderPhase initial = new(0,1,0), cursor = initial;
        var first = score.Step(ref cursor);
        Assert.Equal(new byte[]{11,1},first.Select(e => e.Move.Bits));
        var back = score.Step(ref cursor,true);
        Assert.Equal(initial,cursor);
        Assert.Equal([(1,0,(byte)3),(0,1,(byte)9)],back.Select(e => (e.Voice,e.Phase,e.Move.Bits)));
        var rectangle = BorderStudy.LoopWithDrift(true);
        Assert.Equal(2,rectangle.ReadLaps(1,0).SignSupport(0));
        Assert.Equal(1,rectangle.Predict().End.Uses(0,0));
        Assert.Equal(0,BorderStudy.LoopWithDrift(false).Predict().End.Uses(0,0));
        var rest = new BorderScore([1,1,1],[9,11],0,1,3);
        Assert.Equal(18,rest.Predict().Beats);
        Assert.Equal(4,rest.Predict().FirstLaps);
        Assert.Equal(3,rest.Predict().SecondLaps);
    }

    [Fact]
    public void Assumed_equal_forward_links_give_a_prediction_without_rebinding_the_observed_reaches()
    {
        var separate = new BorderScore([1,1,1],[17,17],3);
        var difference = separate.ReadLaps(1,-1);
        Assert.Equal(7,difference.SignSupport(0));
        Assert.Equal(3,difference.Uses(0,0));
        Assert.Equal(-2,difference.Uses(0,1));
        Assert.Equal(4,difference.AssumingEqualReaches().SignSupport(0));
        Assert.Equal(7,difference.SignSupport(0));
        Assert.Equal(new byte[]{17,17},separate.VoiceBytes(1));
        var shared = new BorderScore([1,1,1],[1,1],3);
        Assert.Equal(4,shared.ReadLaps(1,-1).SignSupport(0));
        var split = BorderStudy.SeparateFall().Predict().End;
        Assert.Equal(7,split.SignSupport(1));
        Assert.Equal(2,split.AssumingEqualReaches().SignSupport(1));
        foreach (byte word in Enumerable.Range(0,8).Select(i=>(byte)i))
        {
            Assert.Equal(new BorderMove(word).Sense,new BorderMove(ThreeBitMeasure.Reorient(word)).Sense);
            Assert.Equal(-new BorderMove(word).Sense,new BorderMove(word).Backward().Sense);
        }
    }

    [Fact]
    public void Scheduling_the_same_opposed_emissions_changes_the_enclosed_sweep()
    {
        var separate = BorderStudy.OpposedSiblings(false);
        var together = BorderStudy.OpposedSiblings(true);
        Assert.Equal(separate.VoiceBytes(0),together.VoiceBytes(0));
        Assert.Equal(separate.VoiceBytes(1),together.VoiceBytes(1));
        Assert.Equal(-2L,BorderStudy.TwiceClosedSweep(BorderStudy.Trace(separate,4)));
        Assert.Equal(2L,BorderStudy.TwiceClosedSweep(BorderStudy.Trace(separate,4,true)));
        Assert.Equal(0L,BorderStudy.TwiceClosedSweep(BorderStudy.Trace(together,2)));
        Assert.Null(BorderStudy.TwiceClosedSweep(BorderStudy.Trace(BorderStudy.Direct(),4)));
    }

    [Fact]
    public void Prediction_can_continue_across_a_gap_and_fails_visibly_on_observed_detached_ink()
    {
        var beats = BorderStudy.Trace(BorderStudy.Direct(),4);
        var intact = BorderStudy.InkWord(beats,15);
        var cut = BorderStudy.InkWord(beats,11);
        Assert.Equal(0x1412,intact);
        Assert.Equal(0x1012,cut);
        var hidden = BorderEvidence.Observe(cut,11);
        Assert.Equal(BorderEvidence.Observe(intact,11),hidden);
        Assert.Equal(5,hidden.CompletionCount());
        var guesses = BorderStudy.Predict(hidden);
        Assert.Equal(4,guesses.Length); // different symmetry witnesses
        Assert.All(guesses, h => { Assert.Equal(intact,h.Word); Assert.Equal(4,h.PredictedMark(6)); });
        Assert.True(hidden.Admits(cut));
        var revealed = hidden.BindSameSlots(BorderEvidence.Observe(cut,4));
        Assert.Equal(1,revealed.CompletionCount());
        Assert.Empty(BorderStudy.Predict(revealed));
        Assert.Equal(0,revealed.BindSameSlots(BorderEvidence.Observe(intact,4)).CompletionCount());
        var twoHidden = BorderStudy.Predict(BorderEvidence.Observe(intact,3));
        Assert.True(twoHidden.Select(h=>h.Word).Distinct().Count() > 1);
        Assert.Equal(64,BorderStudy.Family.Count);
        Assert.Equal(16,BorderStudy.Family.Select(h=>h.Word).Distinct().Count());
    }

    [Fact]
    public void Occupied_points_and_open_intervals_remain_boolean_operands_in_the_same_scope()
    {
        var rail = new LandmarkRun(2,[2,0,3,1]);
        var a = OrdinalInk.Closed(rail,2,3);
        var b = OrdinalInk.Closed(rail,1,0); // reversed endpoints
        var overlap = a.Combine(b,8)!;
        Assert.Equal(0b0011100UL,overlap.Bits);
        var exclusive = a.Combine(b,6)!;
        Assert.Equal(0b1100011UL,exclusive.Bits);
        Assert.Equal(0b1111111UL,overlap.Combine(exclusive,14)!.Bits);
        var onlyA = a.Combine(b,2)!;
        Assert.Equal(3UL,onlyA.Bits); // [P,Q)
        var contact = a.Combine(OrdinalInk.Closed(rail,3,1),8)!;
        Assert.Equal(16UL,contact.Bits);
        Assert.False(contact.ContainsInterval);
        Assert.Null(a.Combine(OrdinalInk.Empty(new(2,[2,0,3,1])),14));
        var longRail = new LandmarkRun(5, Enumerable.Range(0,32).ToArray());
        Assert.Equal((ulong)long.MaxValue,OrdinalInk.Closed(longRail,31,0).Bits);
    }

    [Theory]
    [InlineData(15,15,1,false)] // complete projection
    [InlineData(11,15,1,false)] // pen lifted while moving vertically
    [InlineData(13,15,2,true)]  // pen lifted while advancing across H
    [InlineData(13,13,3,false)] // that stroke is hidden from this reader
    [InlineData(15,13,3,false)]
    public void Coverage_can_request_a_branch_while_the_pen_has_the_same_journey(
        uint ink, uint visible, byte demand, bool released)
    {
        var score = BorderStudy.Direct();
        var coverage = new BorderCoverage(BorderStudy.Trace(score,4),ink,visible);
        Assert.Equal(demand,coverage.ReadSupport());
        var response = new GuardedOffer(coverage);
        Assert.Equal(released,response.Request().Moved);
        Assert.False(response.Request().Moved);
        Assert.Equal(2,score.Predict().End.Uses(0,0));
        if (released) Assert.Equal(2UL,coverage.DefiniteWhite.Bits);
    }

    [Fact]
    public void Two_and_three_forward_links_fit_the_predicted_signs_under_concrete_positive_realizations()
    {
        var score = new BorderScore([1,1,1],[17,17],3);
        byte support = 0;
        for (int lengthA = 1; lengthA <= 6; lengthA++)
            for (int lengthB = 1; lengthB <= 6; lengthB++)
            {
                int gap = 3*lengthA-2*lengthB;
                byte sign = (byte)(1 << (Math.Sign(gap)+1));
                support |= sign;
                if (lengthA == lengthB) Assert.Equal(score.ReadLaps(1,-1).AssumingEqualReaches().SignSupport(0),sign);
            }
        Assert.Equal(support,score.ReadLaps(1,-1).SignSupport(0));
    }

    [Fact]
    public void Every_boolean_law_operates_pointwise_in_the_declared_domain()
    {
        var rail = new LandmarkRun(2,[3,0,2,1]);
        var a = OrdinalInk.Closed(rail,3,2);
        var b = OrdinalInk.Closed(rail,2,1);
        for (byte law = 0; law < 16; law++)
        {
            var result = a.Combine(b,law)!;
            Assert.Equal(0UL,result.Bits >> 7);
            for (int i = 0; i < 7; i++)
            {
                int pair = (i<=4 ? 1 : 0) + (i>=4 ? 2 : 0);
                Assert.Equal((ulong)((law >> pair) & 1),(result.Bits >> i) & 1);
            }
        }
    }

    [Theory]
    [InlineData(1, false)] [InlineData(2, true)] [InlineData(3, false)]
    public void Spacing_can_drive_a_branch_with_full_horizontal_coverage(byte observed, bool branch)
    {
        var coverage = new BorderCoverage(BorderStudy.Trace(BorderStudy.Direct(),4),15,15);
        var spacingEvidence = PresencePairPort.Bind(observed);
        var gauge = BorderStudy.LaneSpacing(spacingEvidence.Test(2));
        var wide = gauge.CompareTips().Test(2);
        Assert.Equal(observed,wide.ReadSupport());
        Assert.Equal(1,coverage.ReadSupport());
        var outlet = new GuardedOffer(PresencePorts.Any(coverage,wide));
        Assert.Equal(branch,outlet.Request().Moved);
    }

    [Fact]
    public void Published_surveys_are_reproducible_and_keep_wrong_unique_predictions()
    {
        var scores = new StringWriter();
        BorderSurvey.Scores(scores,false);
        Assert.Contains("8192 scores; 3584 contain a grouped beat",scores.ToString());
        Assert.Contains("628,224,0",scores.ToString());
        var windows = new StringWriter();
        BorderSurvey.Windows(windows,false);
        Assert.Contains("1,2180,64,256,0",windows.ToString());
    }
}
