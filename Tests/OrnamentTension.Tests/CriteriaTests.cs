using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;

namespace Gojo.Tests.OrnamentTension;

public class CriteriaTests
{
    private static PathTrace TriangleBand(bool shared)
    {
        PathSite[] sites=[new(0,0),new(2,2),new(4,0),new(4,0),new(6,2),new(8,0)];
        PathEdge[] edges=[new(0,1,0,0,0,0,1,0),new(1,2,0,1,0,1,7,0),
            new(shared?2:3,4,0,2,1,0,1,0),new(4,5,0,3,1,1,7,0)];
        return new(sites,edges,[],4,2,1);
    }

    [Fact]
    public void ARealSeamHelpsOneOwnerFinishItsCourse()
    {
        var a=TensionCritics.Measure(TriangleBand(true),3,"zigzag");
        var b=TensionCritics.Measure(TriangleBand(false),3,"zigzag");
        Assert.Equal(1,a.Raw["shared-seam"]);
        Assert.Equal(0,b.Raw["shared-seam"]);
        Assert.Equal(1,a.Raw["whole-course-span"]);
        Assert.Equal(.5,b.Raw["whole-course-span"]);
        Assert.True(a.Values[0]>b.Values[0]);
        Assert.Equal(a.Reference,b.Reference,10);
    }

    [Fact]
    public void CrossingIsLegalAndPartialCoincidenceUsesUpLineLength()
    {
        PathSite[] sites=[new(0,0),new(4,4),new(0,4),new(4,0),new(2,2)];
        PathEdge[] clean=[new(0,1,0,0,0,0,1,0),new(2,3,1,0,0,0,7,0)];
        var a=TensionCritics.Measure(new(sites,clean,[],4,1,2),3,"crossing-ribbons");
        var b=TensionCritics.Measure(new(sites,[..clean,new(4,1,0,1,0,1,1,0)],[],4,1,2),3,"crossing-ribbons");
        Assert.Equal(1,a.Raw["no-overdraw"],10);
        Assert.Equal(0,a.Raw["transverse-clear"],10);
        Assert.Equal(.8,b.Raw["no-overdraw"],10);
        Assert.True(a.Values[1]>b.Values[1]);
    }

    [Fact]
    public void ASpiralMustActuallyWindAndContractToExcuseItsInnerEnd()
    {
        var t=HistoricalReferences.Draw(HistoricalReferences.Examples.Single(x=>x.Family=="inward-spiral" && !x.Holdout));
        var terminal=TensionCritics.Measure(t,9,"inward-spiral");
        var generic=TensionCritics.Measure(t,9,"running-key");
        Assert.True(terminal.Raw["winding-terminal"]>0);
        Assert.True(terminal.Raw["unwanted-terminal"]<generic.Raw["unwanted-terminal"]);
        Assert.Equal(0,TensionCritics.Measure(TriangleBand(true),9,"inward-spiral").Raw["winding-terminal"]);
    }

    [Fact]
    public void PortfolioSharesAndCodeRemainBoundedUnderMutation()
    {
        var rng=new Random(7175);
        var portfolio=JudgePortfolio.Sample(rng);
        for(int i=0;i<400;i++)
        {
            portfolio=portfolio.Mutate(rng);
            var bytes=portfolio.Encode();
            Assert.InRange(bytes.Count(x=>x>0),2,4);
            Assert.Equal(16,bytes.Sum(x=>(int)x));
            Assert.Equal(bytes,JudgePortfolio.Decode(bytes).Encode());
            bytes[0]=99;
            Assert.NotEqual(99,portfolio.Shares[0]);
        }
        Assert.Throws<ArgumentException>(()=>new JudgePortfolio([16,0,0,0,0,0,0,0]));
    }

    [Fact]
    public void HistoricalSourceObjectsAreSplitAndMatchedDrawingsBeatTheFixedWrongPacket()
    {
        var examples=HistoricalReferences.Examples;
        var train=examples.Where(e=>!e.Holdout).Select(e=>e.Object).ToHashSet();
        Assert.DoesNotContain(examples.Where(e=>e.Holdout),e=>train.Contains(e.Object));
        foreach(var example in examples)
        {
            var trace=HistoricalReferences.Draw(example);
            var a=TensionCritics.Measure(trace,4,example.Family,example.Holdout);
            Assert.Equal(1,a.Reference,10);
            Assert.True(a.Reference>a.WrongReference);
        }
    }

    [Fact]
    public void CatalogueNoveltyIsSeparateFromRepeatingAUnit()
    {
        var t=TriangleBand(true);
        var same=TensionCritics.Describe(t,4);
        var archive=TensionCritics.Measure(t,4,"zigzag",archive:[same]);
        Assert.Equal(1,archive.Raw["period-match"],10);
        Assert.Equal(1,archive.Raw["archive-opportunity"]);
        Assert.Equal(0,archive.Raw["shape-novelty"],10);
        Assert.Equal(0,TensionCritics.Measure(t,4,"zigzag").Raw["archive-opportunity"]);
        Assert.True(TensionCritics.Measure(t,2,"zigzag").Raw["slot-reach"]>
            TensionCritics.Measure(t,9,"zigzag").Raw["slot-reach"]);
    }
}
