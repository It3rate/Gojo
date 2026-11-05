using Gojo.Experiments.PrimitiveMeasure;
using Xunit;

namespace PrimitiveMeasure.Tests;

public class HintDrawingTests
{
    [Fact]
    public void Zero_skips_a_mark_but_keeps_its_step_and_padding_never_becomes_a_step()
    {
        var strip=new ParticipationStrip(1,0,1,1,0,1);
        Assert.Equal(new byte[]{0x2D},strip.Bytes());
        var walk=new GateWalk(strip);var trace=walk.Run();
        Assert.True(walk.Complete);
        Assert.Equal(Enumerable.Range(0,6),trace.Select(t=>t.Slot));
        Assert.Equal(new[]{0,2,3,5},trace.Where(t=>t.Marked).Select(t=>t.Slot));
        Assert.All(trace,t=>{Assert.True(t.Completed);Assert.NotEqual(t.Before,t.After);});
        Assert.Throws<InvalidOperationException>(()=>walk.Step());
        Assert.Equal(new byte[]{0x2D},strip.Bytes());
        var blank=new GateWalk(new(0,0,0,0,0,0));
        Assert.Equal(6,blank.Run().Length);Assert.True(blank.Complete);
    }

    [Fact]
    public void Hidden_gate_preserves_cursor_and_source_and_never_supplies_a_closure()
    {
        var c=HintExamples.Circle(0,3);
        Assert.Equal(2,c.Trace.Length);Assert.False(c.Walk.Complete);
        Assert.Equal(1,c.Walk.Position);Assert.Equal(3,c.Trace[^1].Support);
        var before=c.Walk.Circuit.ValueBytes();
        Assert.False(c.Walk.Step().Completed);Assert.Equal(before,c.Walk.Circuit.ValueBytes());
        Assert.Equal(new[]{0},c.Selected);Assert.Equal(new byte[]{0x2D},c.Marks.Bytes());
        Assert.Empty(c.SeamBytes());Assert.False(c.Read().Rectangle);
    }

    [Fact]
    public void Skipping_opposite_sixth_sites_yields_a_rectangle_from_two_diameters()
    {
        var c=HintExamples.Circle(0);var read=c.Read();
        Assert.True(read.Rectangle);Assert.False(read.Square);Assert.False(read.RegularPolygon);
        Assert.Equal(new[]{0,2,3,5},c.Selected);
        Assert.Equal(c.Chord(0)[0],c.Chord(3)[1]);
        Assert.False(HintExamples.Circle(0,1).Read().Rectangle);
        Assert.False(HintExamples.Circle(0,2).Read().Rectangle);
        Assert.True(HintExamples.Circle(3).Read().Square);
        Assert.False(HintExamples.Circle(4).Read().Rectangle);
    }

    [Theory]
    [InlineData(4,1,1)]
    [InlineData(6,3,0)]
    [InlineData(8,6,2)]
    [InlineData(10,10,0)]
    [InlineData(12,15,3)]
    public void Every_subset_agrees_with_independent_diameter_and_numerical_corner_checks(int count,int rectangles,int squares)
    {
        int found=0,squareCount=0;
        for(int mask=0;mask<(1<<count);mask++)
        {
            var bits=Enumerable.Range(0,count).Select(i=>(byte)((mask>>i)&1)).ToArray();
            var c=new MarkedContour(RepetitionExamples.Contour(count,true,false),new(bits));
            var read=c.Read();int[] selected=c.Selected;
            bool expected=selected.Length==4 && selected[2]-selected[0]==count/2 && selected[3]-selected[1]==count/2;
            Assert.Equal(expected,read.Rectangle);
            if(read.Rectangle)found++;if(read.Square)squareCount++;
            if(selected.Length!=4)continue;
            var p=ContourRealization.Sample(c,0).Points;
            bool corners=true;
            for(int i=0;i<4;i++)
            {
                var a=p[selected[(i+3)%4]];var b=p[selected[i]];var d=p[selected[(i+1)%4]];
                double dot=(a.X-b.X)*(d.X-b.X)+(a.Y-b.Y)*(d.Y-b.Y);
                corners &= Math.Abs(dot)<1e-9;
            }
            Assert.Equal(expected,corners);
        }
        Assert.Equal(rectangles,found);Assert.Equal(squares,squareCount);
    }

    [Fact]
    public void Rail_hints_survive_many_drawings_and_missing_hints_do_not_gain_proof_from_pixels()
    {
        foreach(bool rectangle in new[]{false,true})
        foreach(int relaxed in Enumerable.Range(0,5))
        {
            var sketch=HintExamples.Sketch(rectangle,relaxed);
            var bytes=sketch.SiteBytes();var proof=sketch.Read();
            for(int variation=0;variation<36;variation++)
            {
                var sample=RailRealization.Sample(sketch,variation);
                if(relaxed==4){Assert.Null(sample);continue;}
                Assert.NotNull(sample);Assert.True(sample.Admitted(sketch));
                Assert.Equal(bytes,sketch.SiteBytes());Assert.Equal(proof,sketch.Read());
            }
            Assert.Equal(relaxed==4,proof.Conflict);
            if(rectangle)Assert.Equal(relaxed==0,proof.Rectangle);
        }
        var intact=HintExamples.Sketch(true);
        var good=RailRealization.Sample(intact,0)!;
        var shifted=good.Points.ToArray();shifted[0]=new(shifted[0].X+.1,shifted[0].Y);
        Assert.False((good with{Points=shifted}).Admitted(intact));
        var reversed=good.Across.Reverse().ToArray();
        Assert.False((good with{Across=reversed}).Admitted(intact));
    }

    [Fact]
    public void Gate_pairing_changes_intermediate_path_without_changing_signed_participation()
    {
        var separate=HintExamples.Streams(0);var together=HintExamples.Streams(1);
        Assert.Equal(separate.Score.VoiceBytes(0),together.Score.VoiceBytes(0));
        Assert.Equal(separate.Score.VoiceBytes(1),together.Score.VoiceBytes(1));
        Assert.Equal(8,separate.Score.Predict().Beats);Assert.Equal(4,together.Score.Predict().Beats);
        foreach(var score in new[]{separate.Score,together.Score,HintExamples.Streams(3).Score})
        {
            var phase=new BorderPhase(0,0,0);var emissions=new List<BorderEmission>();
            for(int i=0;i<score.ClockLength;i++)emissions.AddRange(score.Step(ref phase));
            Assert.Equal(new BorderPhase(0,0,0),phase);
            Assert.Equal(8,emissions.Count);
            Assert.Equal(2,score.Predict().End.SignSupport(0));Assert.Equal(2,score.Predict().End.SignSupport(1));
        }
        Assert.NotEqual(HintDrawing.Streams(0).Svg(),HintDrawing.Streams(1).Svg());
        var held=HintExamples.Streams(2).Score;var at=new BorderPhase(0,0,0);
        for(int i=0;i<held.ClockLength;i++){held.Step(ref at);Assert.Equal(0,at.Second);}
    }
}
