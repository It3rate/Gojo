namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class WritingLineTests
{
    [Theory]
    [InlineData(0,0,1)] [InlineData(1,4,2)] [InlineData(2,16,2)] [InlineData(3,20,2)]
    public void Collecting_both_inputs_publishes_OR_and_retains_each_arrival(int inputs,int stored,int support)
    {
        var y = GlyphJunction.YCollect((byte)inputs);
        Assert.Equal(GlyphJunction.YMerge(0).ArmBytes(),y.ArmBytes());
        var reader = y.Stem;
        Assert.Same(reader,y.Surface(2));
        Assert.Equal(1,reader.ReadSupport());
        Assert.Equal(stored,y.Advance());
        Assert.Equal(support,reader.ReadSupport());
        Assert.Equal(support,y.Stem.ReadSupport());
        Assert.Equal(stored,y.Flow.Stored);
        Assert.Equal(new byte[]{0x18},y.Collection!.PackedRows(0,2));
        Assert.Equal(new byte[]{0x06},y.OutletBytes());
        Assert.Equal(1,y.Surface(0).ReadSupport());
        Assert.Equal(1,y.Surface(1).ReadSupport());
    }

    [Fact]
    public void Selecting_an_inlet_can_hold_an_arrival_even_with_an_empty_stem()
    {
        Assert.Equal(2,GlyphJunction.YSelectInlet(2).Advance());
        Assert.Equal(13,GlyphJunction.YSelectInlet(11).Advance()); // left waits, gate set
        Assert.Equal(6,GlyphJunction.YSelectInlet(3).Advance());
        var y = GlyphJunction.YSelectInlet(3);
        var hiddenGate = y.Flow.Observe(7);
        Assert.Equal((1u<<6)|(1u<<13),y.Flow.Predict(hiddenGate));
        Assert.False(y.Flow.TryStep(hiddenGate,out _));
        Assert.Equal(3,y.Flow.Stored);
        // Left collection slot is already occupied. Only the right can accept.
        var full = GlyphJunction.YCollect(7);
        Assert.Equal(21,full.Advance());
        Assert.Equal(2,full.Stem.ReadSupport());
    }

    [Fact]
    public void Both_stems_project_to_one_frame_and_the_hook_is_independent_evidence()
    {
        var line = new WritingLine();
        Assert.Equal(line.Level(line.Stems.Edge(0)[0]),line.Level(line.Stems.Edge(1)[0]));
        Assert.Equal(line.UprightFrame[0],line.Level(line.Stems.Edge(0)[1]));
        Assert.Equal(["i"],WritingLine.Candidates(line.Read(0)));
        Assert.Equal(["j"],WritingLine.Candidates(line.Read(1)));
        Assert.Equal(["i","j"],WritingLine.Candidates(line.Read(1,showBaseline:false)));
        Assert.Equal(["j"],WritingLine.Candidates(line.Read(1,showBaseline:false,showBend:true)));
        Assert.Equal(line.Read(0,showBaseline:false),line.Read(1,showBaseline:false));
    }

    [Fact]
    public void A_neighbouring_dot_can_belong_elsewhere_without_an_ink_edge()
    {
        var own = new WritingLine();
        var unassigned = new WritingLine(includeRightDot:false);
        var swapped = new WritingLine(swapMembership:true);
        Assert.Equal(own.Stems.EdgeBytes(),swapped.Stems.EdgeBytes());
        Assert.Equal(own.DotBytes(),unassigned.DotBytes());
        Assert.Equal(own.LevelBytes(),unassigned.LevelBytes());
        Assert.Equal(4,swapped.InkComponents);
        Assert.Equal([2,4],swapped.Membership()[1]);
        Assert.False(unassigned.Read(1).Dotted);
        Assert.Empty(WritingLine.Candidates(unassigned.Read(1)));
        Assert.Null(unassigned.Read(1,showMembership:false).Dotted);
        Assert.Equal(["j"],WritingLine.Candidates(unassigned.Read(1,showMembership:false)));
    }

    [Theory]
    [InlineData(false,0,"open 4","T")] [InlineData(true,1,"closed 4","A")]
    public void Two_hands_share_a_four_read_but_disagree_about_a_walk(bool closed,int loops,string exact,string foil)
    {
        var ink = FourHands.Build(closed);
        var features = new StrokeFeatures(ink);
        Assert.Equal(loops,features.Loops.Count);
        if (closed) Assert.Equal(features.Loops[0].Sites()[0],features.Loops[0].Sites()[^1]);
        Assert.Equal([exact],FourHands.Candidates(ink,true));
        Assert.Equal(["open 4","closed 4"],FourHands.Candidates(ink,false));
        var sharedView = FourHands.ReleaseFirstTip(ink);
        var otherView = FourHands.ReleaseFirstTip(FourHands.Build(!closed));
        for (var i=0;i<sharedView.RunCount;i++)
            Assert.Equal(otherView.Run(i).SiteBytes(),sharedView.Run(i).SiteBytes());
        Assert.Contains(foil,FourHands.ByLoops(loops));
        Assert.Equal(loops,ink.Incidence().Cycles);
        Assert.DoesNotContain("closed 4",FourHands.Candidates(StraightCapitals.Build("T"),false));
    }
}
