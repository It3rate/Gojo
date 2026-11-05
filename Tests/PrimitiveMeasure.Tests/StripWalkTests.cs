using Gojo.Experiments.PrimitiveMeasure;
using Xunit;

namespace PrimitiveMeasure.Tests;

public class StripWalkTests
{
    private sealed class Sample(byte value):IPrimitiveReadPort
    { public byte Value=value;public byte ReadSupport()=>Value; }

    [Fact]
    public void A_cyclic_body_stops_on_depth_and_keeps_every_unit_and_reference()
    {
        foreach(int sides in new[]{3,4,5})foreach(bool closed in new[]{false,true})
        foreach(int depth in Enumerable.Range(2,31))
        {
            var walk=new StripWalk(sides,depth,closed);var body=walk.BodyBytes();var frames=walk.FrameBytes();
            while(!walk.Complete)
            {
                var visit=walk.Step();var counts=walk.Depth.AuditCounts();
                Assert.Equal(depth,counts.Sum());Assert.InRange(counts[1],0,1);Assert.True(counts[0]>=1);
                Assert.True(visit.Drew || visit.Stopped);
                if(visit.Drew)
                {
                    var e=walk.Edge(visit.Edge!.Value);var line=walk.LineRow(e[2]);
                    Assert.Contains(e[2],walk.JointRow(e[0]));Assert.Contains(e[2],walk.JointRow(e[1]));
                    Assert.InRange(line[1],0,depth-1);
                    if(!closed && visit.Edge>0)Assert.Equal(walk.Edge(visit.Edge.Value-1)[1],e[0]);
                    if(closed && visit.Phase==sides-1)Assert.Equal(walk.Edge(visit.Edge.Value-sides+1)[0],e[1]);
                }
            }
            Assert.Equal(new[]{1,0,depth-1},walk.Depth.AuditCounts());
            Assert.Equal(sides*(depth-1),walk.EdgeCount);Assert.Equal(walk.EdgeCount+1,walk.Visits);
            Assert.Equal(closed?depth-1:0,walk.LiftCount);
            Assert.Equal(body,walk.BodyBytes());Assert.Equal(frames,walk.FrameBytes());
            for(int i=0;i<walk.BandCount;i++)
            {
                var band=walk.Band(i);var a=walk.LineRow(band[0]);var b=walk.LineRow(band[1]);
                Assert.Equal(a[0],b[0]);Assert.Equal(a[1]+1,b[1]);Assert.Equal(0,band[2]);
            }
        }
    }
    [Fact]
    public void Unknown_width_holds_without_reserving_and_a_missing_corner_keeps_a_pending_pass()
    {
        var width=new Sample(3);var corner=new Sample(2);var walk=new StripWalk(5,8,width:width,corner:corner);
        Assert.False(walk.Step().Drew);Assert.False(walk.Complete);Assert.Equal(new[]{8,0,0},walk.Depth.AuditCounts());
        width.Value=2;Assert.True(walk.Step().Drew);Assert.Equal(new[]{7,1,0},walk.Depth.AuditCounts());
        corner.Value=0;var ink=walk.InkBytes();var pending=walk.Depth.BankBytes(1);var phase=walk.Phase.Position;
        Assert.False(walk.Step().Drew);Assert.Equal(ink,walk.InkBytes());Assert.Equal(pending,walk.Depth.BankBytes(1));
        Assert.Equal(phase,walk.Phase.Position);Assert.False(walk.Complete);
        corner.Value=2;walk.Run();Assert.True(walk.Complete);Assert.Equal(new[]{1,0,7},walk.Depth.AuditCounts());
    }
    [Fact]
    public void Incompatible_units_or_unequal_turns_do_not_claim_a_regular_offset_construction()
    {
        foreach(var walk in new[]{new StripWalk(5,8,commonUnit:false),new StripWalk(5,8,equalTurns:false)})
        {
            Assert.Equal(0,walk.Fit.ReadSupport());walk.Run();Assert.False(walk.Complete);
            Assert.Equal(0,walk.EdgeCount);Assert.Equal(new[]{8,0,0},walk.Depth.AuditCounts());
        }
    }
    [Fact]
    public void More_depth_changes_inventory_and_output_but_not_the_loop_program()
    {
        var a=new StripWalk(5,4);var b=new StripWalk(5,24);
        Assert.Equal(a.BodyBytes(),b.BodyBytes());Assert.Equal(a.FrameBytes(),b.FrameBytes());
        Assert.Equal(a.Circuit.PackedRows(a.FeedbackTable),b.Circuit.PackedRows(b.FeedbackTable));
        Assert.Equal(a.Phase.Circuit.PackedRows(a.Phase.Table),b.Phase.Circuit.PackedRows(b.Phase.Table));
        a.Run();b.Run();Assert.Equal(15,a.EdgeCount);Assert.Equal(115,b.EdgeCount);
        Assert.NotEqual(new StripWalk(5,4,true).BodyBytes(),a.BodyBytes());
    }
    [Fact]
    public void Numerical_witnesses_preserve_offset_forward_order_and_clearance_without_driving_the_loop()
    {
        foreach(int n in new[]{3,4,5})foreach(int depth in new[]{2,3,8,16})foreach(bool closed in new[]{false,true})
        {
            var walk=new StripWalk(n,depth,closed);walk.Run();var bytes=walk.InkBytes();
            foreach(double rotation in new[]{0.0,0.37,1.1})
            for(int i=0;i<walk.EdgeCount;i++)
            {
                var e=walk.Edge(i);var line=walk.LineRow(e[2]);var u=StripDrawing.Normal(walk,line[0],rotation);
                var a=StripDrawing.Point(walk,e[0],rotation);var b=StripDrawing.Point(walk,e[1],rotation);
                Assert.InRange(Math.Abs(a.X*u.X+a.Y*u.Y-(depth-line[1])),0,1e-10);
                Assert.InRange(Math.Abs(b.X*u.X+b.Y*u.Y-(depth-line[1])),0,1e-10);
                Assert.True((b.X-a.X)*(-u.Y)+(b.Y-a.Y)*u.X>0);
                for(int j=0;j<i;j++)
                {
                    var other=walk.Edge(j);if(other[0]==e[1] || other[1]==e[0] || other[0]==e[0] || other[1]==e[1])continue;
                    var c=StripDrawing.Point(walk,other[0],rotation);var d=StripDrawing.Point(walk,other[1],rotation);
                    Assert.True(Math.Min(Math.Min(Distance(a,c,d),Distance(b,c,d)),Math.Min(Distance(c,a,b),Distance(d,a,b)))>=1-1e-10);
                }
            }
            Assert.Equal(bytes,walk.InkBytes());Assert.Contains("<svg",StripDrawing.Draw(walk).Svg());
        }
    }
    private static double Distance(PenPoint p,PenPoint a,PenPoint b)
    {
        double x=b.X-a.X,y=b.Y-a.Y,t=Math.Clamp(((p.X-a.X)*x+(p.Y-a.Y)*y)/(x*x+y*y),0,1);
        return Math.Sqrt(Math.Pow(p.X-a.X-t*x,2)+Math.Pow(p.Y-a.Y-t*y,2));
    }
}
