namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class GlyphCompletionTests
{
    [Fact]
    public void Moving_one_cut_preserves_the_whole_and_reversing_swaps_the_tips()
    {
        var source = new GlyphCut(1,false,true).RadialRows();
        for (var cut = 1; cut < GlyphCut.Landmarks; cut++)
        {
            var forward = new GlyphCut(cut,false,true);
            var backward = new GlyphCut(cut,true,true);
            Assert.Equal((forward.Start,forward.Tip),(backward.Tip,backward.Start));
            Assert.Equal(forward.AttemptReturn().Uses.Reverse().Select(u=>u^1),backward.AttemptReturn().Uses);
            foreach (var reverse in new[]{false,true})
            {
                var retained = new GlyphCut(cut,reverse,true);
                var open = new GlyphCut(cut,reverse,false);
                Assert.Equal(source,retained.RadialRows());
                Assert.True(retained.Windows.IsClosed(2));
                Assert.Empty(open.RadialRows());
                Assert.Equal(open.AttemptInk().Uses,retained.AttemptInk().Uses);
                Assert.False(open.AttemptInk().Arrived);
                Assert.False(retained.InkCloses);
                Assert.Equal(3,GlyphCut.Predict(true,retained.InkCloses,null));
                Assert.Equal(3,GlyphCut.Predict(true,open.InkCloses,null));
                Assert.Equal(2,retained.InspectReturn());
                Assert.Equal(1,open.InspectReturn());
                Assert.True(retained.AttemptReturn().Arrived);
                Assert.Equal(8-cut,retained.AttemptReturn().Uses.Length);
                Assert.False(open.AttemptReturn().Arrived);
                Assert.Empty(open.AttemptReturn().Uses);
            }
        }
    }

    [Theory]
    [InlineData("D",true)] [InlineData("D · corners",true)] [InlineData("D · half of O",true)]
    [InlineData("Straight · closure sought",false)] [InlineData("O",false)]
    public void Trying_a_return_keeps_the_observed_interior_open(string name,bool fits)
    {
        var observed = CurvedCapitals.Ink("D · ends only");
        var trial = new ReturnTrial(observed,CurvedCapitals.Ink(name));
        Assert.Equal(fits,trial.Fits);
        Assert.Equal(new[]{0,1},new[]{trial.Binding(0)[1],trial.Binding(1)[1]});
        Assert.Equal(3,observed.Read().BendSupport);
        Assert.Single(observed.DriveLinks());
    }
}
