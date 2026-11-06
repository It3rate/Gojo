using Gojo.Experiments.OrnamentPathCampaign;

namespace Gojo.Tests.OrnamentPathCampaign;

public class PathMachineTests
{
    [Fact]
    public void ConvergentDiamondBindsTwoOwnersThroughAnActualSite()
    {
        var hex = PathMachine.Foundation(2);
        var trace = PathMachine.Run(hex,new(2,1,4,2));
        Assert.Equal(8,trace.Edges.Length);
        var a = trace.Edges.Where(e => e.Cell == 0 && e.Owner == 0).ToArray();
        var b = trace.Edges.Where(e => e.Cell == 0 && e.Owner == 1).ToArray();
        Assert.Equal(a[0].From,b[0].From);
        Assert.Equal(a[1].To,b[1].To);
        Assert.Equal(new PathSite(4,0),trace.Sites[b[1].To]);
        Assert.Contains(trace.Events,e => e.Owner == 1 && e.Kind == "branch");
        Assert.Contains(trace.Events,e => e.Owner == 1 && e.Kind == "merge");
        Assert.Contains(trace.Events,e => e.Owner == 0 && e.Cell == 1 && e.Kind == "continue");
        var cut = PathMachine.Run(hex,new(2,1,4,2),PathMode.Cut);
        Assert.DoesNotContain(cut.Edges,e => e.Owner == 1);
        Assert.Contains(cut.Events,e => e.Kind == "blocked");
    }

    [Fact]
    public void CrossingAndCoincidentIndependentEndpointsDoNotBind()
    {
        var owner = new PathOwner(0,0,0,[StrokeRow.Bind(0)]);
        var trace = PathMachine.Run(PathMachine.Encode(owner,owner),new(2,1,2,2));
        var first = trace.Edges[0];
        var second = trace.Edges[1];
        Assert.Equal(trace.Sites[first.To],trace.Sites[second.To]);
        Assert.NotEqual(first.To,second.To);
        Assert.DoesNotContain(trace.Events,e=>e.Kind == "merge" || e.Kind == "continue");
    }

    [Fact]
    public void SavedSiteCanCreateABranchWithoutReplayingTheEarlierDraw()
    {
        StrokeRow[] rows = [StrokeRow.Bind(0,save:true),StrokeRow.Bind(1),StrokeRow.Bind(7,source:2)];
        var trace = PathMachine.Run(PathMachine.Encode(new PathOwner(0,0,0,rows)),new(2,1,4,1));
        Assert.Equal(trace.Edges[0].To,trace.Edges[1].From);
        Assert.Equal(trace.Edges[0].To,trace.Edges[2].From);
        Assert.NotEqual(trace.Edges[1].To,trace.Edges[2].From);
        Assert.Contains(trace.Events,e=>e.Kind == "branch" && e.From == trace.Edges[0].To);
    }

    [Fact]
    public void MissingAndUnknownInputsRetainDifferentGateSupports()
    {
        var owner = new PathOwner(0,0,0,[StrokeRow.Bind(0,law:12)]);
        var bytes = PathMachine.Encode(owner,owner);
        var input = new PathInput(1,2,3,1,0,-1);
        var unknown = PathMachine.Run(bytes,input);
        var missing = PathMachine.Run(bytes,input,PathMode.Cut);
        Assert.All(unknown.Events.Where(e=>e.Row >= 0), e=>Assert.Equal(3,e.Support));
        Assert.All(missing.Events.Where(e=>e.Row >= 0), e=>Assert.Equal(0,e.Support));
        Assert.Empty(unknown.Edges);
        Assert.Empty(missing.Edges);
        Assert.Equal(2,PathMachine.Run(bytes,input,PathMode.Open).Edges.Length);
    }

    [Fact]
    public void ADownstreamOwnerReadsThePreviousBeatForEitherOwnerOrdering()
    {
        var sender = new PathOwner(0,0,0,[StrokeRow.Bind(0),StrokeRow.Bind(0)]);
        var receiver = new PathOwner(1,0,0,[StrokeRow.Bind(0,law:12),StrokeRow.Bind(0,law:12)]);
        foreach (bool senderFirst in new[] { true,false })
        {
            var bytes = senderFirst ? PathMachine.Encode(sender,receiver) : PathMachine.Encode(receiver,sender);
            var trace = PathMachine.Run(bytes,new(1,1,2,1));
            var received = Assert.Single(trace.Edges,e=>e.Owner == (senderFirst ? 1 : 0));
            Assert.Equal(1,received.Beat);
            Assert.DoesNotContain(PathMachine.Run(bytes,new(1,1,2,1),PathMode.Cut).Edges,e=>e.Owner == received.Owner);
        }
    }

    [Fact]
    public void AllSixteenStrokeLawsGiveTheDeclaredBooleanOutputs()
    {
        for (int law=0; law<16; law++)
            for (int clock=0; clock<2; clock++)
                for (int seed=0; seed<2; seed++)
                {
                    var owner = new PathOwner(0,(byte)clock,0,[StrokeRow.Bind(0,law:law)]);
                    var trace = PathMachine.Run(PathMachine.Encode(owner),new(1,1,2,1,SeedBit:seed));
                    Assert.Equal((law >> (clock+2*seed)) & 1,trace.Edges.Length);
                }
    }

    [Fact]
    public void AdmittedStopStopsOnlyItsOwnBodyUntilNextRepeat()
    {
        var a = new PathOwner(0,0,0,[StrokeRow.Bind(0,stop:true),StrokeRow.Bind(2)]);
        var b = new PathOwner(0,0,0,[StrokeRow.Bind(0),StrokeRow.Bind(2)]);
        var trace = PathMachine.Run(PathMachine.Encode(a,b),new(1,2,3,2));
        Assert.Equal(2,trace.Edges.Count(e => e.Owner==0));
        Assert.Equal(4,trace.Edges.Count(e => e.Owner==1));
        Assert.Equal(2,trace.Events.Count(e => e.Kind=="stop"));
    }

    [Fact]
    public void LocalWriteChangesTheResumableBytesAndFrozenKeepsTheInitialFrame()
    {
        var owner = new PathOwner(0,0,63,[StrokeRow.Bind(1)]);
        var program = PathMachine.Encode(owner);
        var input = new PathInput(1,1,2,1);
        var trace = PathMachine.Run(program,input);
        Assert.NotEqual(program,trace.FinalProgram);
        Assert.Equal(1,trace.Edges[0].Direction);
        Assert.Equal(7,PathMachine.Run(trace.FinalProgram,input).Edges[0].Direction);
        var frozen = PathMachine.Run(program,input,PathMode.Frozen);
        Assert.Equal(program,frozen.FinalProgram);
        Assert.DoesNotContain(frozen.Events,e => e.Kind == "switch");
    }

    [Fact]
    public void ChangingAnExtentBindingPreservesRowsAndExposesFailedContact()
    {
        var program = PathMachine.Foundation(2);
        var matched = PathMachine.Run(program,new(2,1,4,2));
        var separated = PathMachine.Run(program,new(2,1,5,2));
        Assert.Equal(matched.Edges.Length,separated.Edges.Length);
        Assert.Contains(matched.Events, e => e.Kind=="continue");
        Assert.DoesNotContain(separated.Events, e => e.Kind=="continue");
        var forced = PathMachine.Encode(new PathOwner(0,0,0,[StrokeRow.Bind(0,target:3)]));
        Assert.Empty(PathMachine.Run(forced,new(2,1,5,1)).Edges);
    }

    [Fact]
    public void SearchBytesRoundTripAndDoNotMutateParents()
    {
        var rng = new Random(67);
        for (int i = 0; i < 96; i++)
        {
            var parent = PathMachine.Create(rng);
            var before = Convert.ToHexString(parent);
            var child = PathMachine.Cross(PathMachine.Mutate(parent,rng),parent,rng);
            Assert.Equal(child,PathMachine.Encode(PathMachine.Decode(child)));
            var input = new PathInput(1+i%5,2,6,3,i,i%2);
            var original = PathMachine.Run(child,input);
            var replay = PathMachine.Run(Convert.FromHexString(Convert.ToHexString(child)),input);
            Assert.Equal(original.Edges,replay.Edges);
            Assert.Equal(original.Sites,replay.Sites);
            Assert.Equal(original.Events,replay.Events);
            Assert.Equal(before,Convert.ToHexString(parent));
        }
    }
}
