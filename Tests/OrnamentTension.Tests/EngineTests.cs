using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Tests.OrnamentTension;

public sealed class EngineTests
{
    [Fact]
    public void CatalogContainsExecutableProgramsAndEditsRemainPortable()
    {
        var rng=new Random(20061107);
        var unique=new HashSet<string>();
        for(int i=0;i<EngineLibrary.Entries.Length;i++)
        {
            var bytes=EngineLibrary.Create(i);
            Assert.True(unique.Add(Convert.ToHexString(bytes)));
            Assert.Equal(bytes,PathMachine.Encode(PathMachine.Decode(bytes)));
            foreach(var variant in new[]{bytes,EngineLibrary.Mutate(bytes,rng),
                    EngineLibrary.Cross(bytes,EngineLibrary.Create((i+1)%EngineLibrary.Entries.Length),rng)})
            {
                Assert.Equal(variant,PathMachine.Encode(PathMachine.Decode(variant)));
                var trace=PathMachine.Run(variant,new(3,1,5,4));
                Assert.All(trace.Edges,e=>Assert.NotEqual(e.From,e.To));
            }
        }
    }

    [Fact]
    public void ThreeDifferentPenProgramsAndAFourArrivalFixtureMakeIdenticalXInk()
    {
        var scenes=JointCensus.Challenges();
        var bottom=scenes[0];
        foreach(var scene in scenes.Take(4))
            Assert.True(Support(bottom.Trace).SetEquals(Support(scene.Trace)));
        Assert.Equal(1,bottom.Read.OpposedVs);
        Assert.Equal(0,bottom.Read.SharedFour);
        Assert.Equal(1,scenes[1].Read.InteriorCrossings);
        Assert.Equal(1,scenes[1].Read.StraightCrossings);
        Assert.Equal(0,scenes[1].Read.CollinearOverlaps);
        Assert.Equal(1,scenes[2].Read.SharedFour);
        Assert.Equal(1,scenes[2].Read.BranchSites);
        Assert.Equal(1,scenes[2].Read.MergeSites);
        Assert.Equal(0,scenes[2].Read.StraightCrossings);
        Assert.Equal(1,scenes[3].Read.FourIntoOne);
        Assert.Equal(1,scenes[4].Read.CollinearOverlaps);
        Assert.Equal(0,scenes[4].Read.InteriorCrossings);
        Assert.Single(scenes[1].Read.Locations,loc=>loc.TwiceX==2 && loc.TwiceY==0 && loc.MaterialSites.Length==0);
    }

    [Fact]
    public void HalfStepCrossingsAndAdjacentCollinearSegmentsHaveSeparateReads()
    {
        // Each diagonal is only one tick long. Their crossing has no whole-site coordinate.
        var trace=new PathTrace([new(0,0),new(1,1),new(0,1),new(1,0)],
            [new(0,1,0,0,0,0,1,0),new(2,3,1,0,0,0,7,0)],[],1,1,2);
        var read=JointCensus.Read(trace,true);
        Assert.Equal(1,read.InteriorCrossings);
        Assert.Contains(read.Locations,l=>l.TwiceX==1 && l.TwiceY==1);
        var adjacent=new PathTrace([new(0,0),new(2,0),new(4,0)],
            [new(0,1,0,0,0,0,0,0),new(1,2,0,1,0,1,0,0)],[],4,1,1);
        Assert.Equal(0,JointCensus.Read(adjacent).CollinearOverlaps);
    }

    [Fact]
    public void RunningKeyAndSpiralExposeDifferentEndingContracts()
    {
        var key=PathMachine.Run(EngineLibrary.Create(0),new(3,1,5,4));
        Assert.Equal(3,key.Events.Count(e=>e.Kind=="continue"));
        Assert.Equal(0,JointCensus.Read(key).CollinearOverlaps);
        Assert.Equal(0,JointCensus.Read(key).LooseTips);
        var coil=PathMachine.Run(EngineLibrary.Create(1),new(3,1,5,4));
        var seen=JointCensus.Read(coil);
        Assert.Equal(4,seen.IntentionalStops);
        Assert.Equal(0,seen.LooseTips);
        Assert.Equal(0,seen.CollinearOverlaps);
    }

    [Fact]
    public void BranchEngineOpensRealSiteIncidencesAndAlternatingPenMirrorsTheNextBay()
    {
        var spray=PathMachine.Run(EngineLibrary.Create(5),new(3,1,6,3));
        var read=JointCensus.Read(spray);
        Assert.Equal(3,read.SharedFour);
        Assert.Equal(3,read.BranchSites);
        Assert.NotEmpty(read.ForkPairs);
        Assert.NotEmpty(read.TurnPairs);
        var pin=PathMachine.Run(EngineLibrary.Create(6),new(3,1,9,3));
        var directions=pin.Edges.Where(e=>e.Owner==0 && e.Row==1).Select(e=>e.Direction).ToArray();
        Assert.Equal(new[]{1,7,1},directions);
        Assert.Equal(2,pin.Events.Count(e=>e.Owner==0 && e.Kind=="continue"));
    }

    private static HashSet<(int X,int Y,int Axis)> Support(PathTrace trace)
    {
        var support=new HashSet<(int,int,int)>();
        foreach(var e in trace.Edges)
        {
            var a=trace.Sites[e.From]; var b=trace.Sites[e.To];
            int dx=Math.Sign(b.X-a.X),dy=Math.Sign(b.Y-a.Y);
            int length=Math.Max(Math.Abs(b.X-a.X),Math.Abs(b.Y-a.Y));
            for(int j=0;j<length;j++)
                support.Add((2*a.X+dx*(2*j+1),2*a.Y+dy*(2*j+1),e.Direction&3));
        }
        return support;
    }
}
