using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Tests.OrnamentTension;

public class RuntimeTests
{
    private static byte[] One(params int[] directions)=>PathMachine.Encode(
        new PathOwner(8,85,0,directions.Select(d=>StrokeRow.Bind(d)).ToArray()));

    [Fact]
    public void ContactSteersAwayFromRetracingButAllowsTransverseCrossings()
    {
        var input=new PathInput(2,1,4,1);
        var policy=TensionMachine.DefaultPolicy();
        var path=One(0,4);
        var quiet=TensionMachine.Run(path,input,3,policy,FeedbackMode.Quiet,true);
        var live=TensionMachine.Run(path,input,3,policy,FeedbackMode.Live,true);
        Assert.Equal(4,quiet.Trace.Edges[1].Direction);
        Assert.NotEqual(4,live.Trace.Edges[1].Direction);
        Assert.Contains(live.Timeline,e=>e.Kind=="sense" && e.Source==0 && e.Lane==1 && e.Delta>0);
        Assert.Contains(live.Timeline,e=>e.Kind=="act" && e.Lane==1);
        var crossed=PathMachine.Encode(new PathOwner(0,85,0,[StrokeRow.Bind(1)]),
            new PathOwner(3,85,0,[StrokeRow.Bind(7)]));
        var crossing=TensionMachine.Run(crossed,input,3,policy,FeedbackMode.Live,true);
        Assert.Equal(new[]{1,7},crossing.Trace.Edges.Select(e=>e.Direction));
        Assert.Equal(4,crossing.Trace.Edges.SelectMany(e=>new[]{e.From,e.To}).Distinct().Count());
        Assert.DoesNotContain(crossing.Timeline,e=>e.Kind=="act");
    }

    [Fact]
    public void TopDownImpulseActsThroughTwoPassagesAndOpposedSignsMoveDifferentAmounts()
    {
        var path=One(0,0,0,0,0,0,0,0);
        var input=new PathInput(3,2,12,2);
        var policy=TensionMachine.DefaultPolicy();
        policy[0]=32;
        TensionResult Run(int sign,FeedbackMode mode)=>TensionMachine.Run(path,input,3,policy,mode,true,
            [new(0,2,0,3*sign)]);
        var grow=Run(1,FeedbackMode.Live);
        var shrink=Run(-1,FeedbackMode.Live);
        var cut=Run(1,FeedbackMode.CutDown);
        Assert.Contains(grow.Timeline,e=>e.Kind=="delivery" && e.Source==2 && e.Target==1 && e.Accepted==1);
        Assert.Contains(grow.Timeline,e=>e.Kind=="delivery" && e.Source==1 && e.Target==0 && e.Accepted==1);
        Assert.Contains(grow.Trace.Edges,e=>e.Extent==2);
        Assert.Contains(shrink.Trace.Edges,e=>e.Extent==1);
        Assert.All(cut.Trace.Edges,e=>Assert.Equal(0,e.Extent));
        Assert.True(cut.Stats.Blocked>0);
        foreach(var run in new[]{grow,shrink,cut})
            Assert.Equal(run.Stats.Offered,run.Stats.Accepted+run.Stats.Blocked+run.Stats.Remainder);
    }

    [Fact]
    public void PhysicalSlotBoundaryAppliesEvenToUncoupledPen()
    {
        var input=new PathInput(2,1,5,1);
        var p=One(2,2,2);
        var narrow=TensionMachine.Run(p,input,2,TensionMachine.DefaultPolicy(),FeedbackMode.Quiet,true);
        var wide=TensionMachine.Run(p,input,9,TensionMachine.DefaultPolicy(),FeedbackMode.Quiet,true);
        Assert.Single(narrow.Trace.Edges);
        Assert.Equal(2,narrow.Stats.SlotBlocks);
        Assert.Equal(3,wide.Trace.Edges.Length);
        Assert.Equal(PathMachine.Run(p,input).Edges,wide.Trace.Edges);
        Assert.Equal(0,narrow.Stats.Actuations);
    }

    [Fact]
    public void FixedRoutingCannotLearnAndSilenceCannotReceiveCredit()
    {
        var path=PathMachine.Foundation(4);
        var input=new PathInput(2,1,6,6);
        var policy=TensionMachine.DefaultPolicy();
        var fixedRun=TensionMachine.Run(path,input,3,policy,FeedbackMode.Fixed,true);
        var quiet=TensionMachine.Run(path,input,3,policy,FeedbackMode.Quiet,true);
        Assert.Equal(0,fixedRun.Stats.LearnedChanges);
        Assert.Equal(0,quiet.Stats.Relief+quiet.Stats.Misses);
        Assert.DoesNotContain(quiet.Timeline,e=>e.Kind=="act");
    }

    [Fact]
    public void DrawingAndRouteCountsAgreeWithAndWithoutDetailedTimeline()
    {
        var path=PathMachine.Foundation(3);
        var input=new PathInput(1,3,7,6);
        var policy=TensionMachine.DefaultPolicy();
        var first=TensionMachine.Run(path,input,4,policy,FeedbackMode.Live,true);
        var second=TensionMachine.Run(path,input,4,policy);
        Assert.Equal(first.Trace.Edges,second.Trace.Edges);
        Assert.Equal(first.Stats.Routes,second.Stats.Routes);
        Assert.Empty(second.Timeline);
        Assert.Equal(first.Stats.Accepted,first.Timeline.Count(e=>e.Kind=="delivery" && e.Accepted!=0));
        Assert.Equal(first.Stats.Blocked,first.Timeline.Count(e=>e.Kind=="delivery" && e.Blocked!=0));
    }

    [Fact]
    public void CoarseSurfaceDistinguishesSlopesAndAliasesDifferentPhysicalLengths()
    {
        var input=new PathInput(2,1,6,3);
        var policy=TensionMachine.DefaultPolicy();
        var ascending=TensionMachine.Run(One(1),input,9,policy,FeedbackMode.Quiet);
        var stretched=TensionMachine.Run(One(1),input with{A=3},9,policy,FeedbackMode.Quiet);
        var descending=TensionMachine.Run(One(7),input,9,policy,FeedbackMode.Quiet);
        Assert.Equal(4,ascending.Publication&15);
        Assert.Equal(8,descending.Publication&15);
        Assert.Equal(ascending.Publication,stretched.Publication);
        Assert.NotEqual(ascending.Trace.Sites[^1],stretched.Trace.Sites[^1]);
    }

    [Fact]
    public void WhenReliefChangesRouteStrengthAFutureStrokeCanActuallyDiffer()
    {
        var p=TensionMachine.DefaultPolicy();
        var input=new PathInput(2,1,6,6);
        var helped=TensionMachine.Run(PathMachine.Foundation(2),input,5,p,FeedbackMode.Live,true);
        Assert.True(helped.Stats.Relief>0);
        Assert.Contains(helped.Timeline,e=>e.Kind=="relief" && e.After>e.Before);
        var live=TensionMachine.Run(PathMachine.Foundation(4),input,5,p,FeedbackMode.Live,true);
        var fixedRoute=TensionMachine.Run(PathMachine.Foundation(4),input,5,p,FeedbackMode.Fixed,true);
        Assert.True(live.Stats.LearnedChanges>0);
        Assert.False(live.Trace.Edges.SequenceEqual(fixedRoute.Trace.Edges));
    }
}
