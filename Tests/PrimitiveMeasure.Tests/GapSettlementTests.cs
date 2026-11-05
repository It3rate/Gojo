using Gojo.Experiments.PrimitiveMeasure;
using Xunit;

namespace PrimitiveMeasure.Tests;

public class GapSettlementTests
{
    [Fact]
    public void Complete_comparison_graphs_conserve_every_step_and_reach_the_best_integer_spacing()
    {
        foreach(int n in new[]{3,4})
        {
            int radix=n==3?5:4,totalCases=(int)Math.Pow(radix,n);
            for(int word=0;word<totalCases;word++)
            {
                int rest=word;int[] seed=new int[n];
                for(int i=0;i<n;i++){seed[i]=1+rest%radix;rest/=radix;}
                var pool=new GapSettlement(seed);int total=seed.Sum();long energy=pool.Audit().SquareSum;
                Assert.False(pool.Quiet);
                for(int visit=0;visit<1000 && !pool.Quiet;visit++)
                {
                    var step=pool.Step();var audit=pool.Audit();
                    Assert.Equal(total,audit.Total);
                    if(step.Moved){Assert.True(audit.SquareSum<energy);Assert.Equal(8,step.BeforeSupport);Assert.Equal(6,step.AfterState);}
                    else Assert.Equal(energy,audit.SquareSum);
                    energy=audit.SquareSum;
                    for(int gap=0;gap<n;gap++)
                    {
                        var bits=pool.GapBytes(gap);bool seenZero=false;
                        for(int i=0;i<pool.Capacity;i++)
                        {
                            bool on=(bits[i/8]&(1<<(i%8)))!=0;
                            Assert.False(seenZero&&on);seenZero|=!on;
                        }
                        Assert.Equal(1,bits[0]&1);
                    }
                }
                Assert.True(pool.Quiet);Assert.True(pool.Audit().WithinOne);
                Assert.Equal(total%n==0,pool.Audit().Even);
                Assert.All(pool.Counts(),c=>Assert.InRange(c,total/n,(total+n-1)/n));
            }
        }
    }

    [Fact]
    public void Local_quiet_does_not_establish_global_evenness()
    {
        var adjacent=new GapSettlement([1,2,3],[[0,1],[1,2]]);
        adjacent.Settle();Assert.True(adjacent.Quiet);Assert.Equal(0,adjacent.Moves);
        Assert.False(adjacent.Audit().WithinOne);Assert.Equal(2,adjacent.Audit().Spread);
        var connected=new GapSettlement([1,2,3]);connected.Settle();
        Assert.Equal(new[]{2,2,2},connected.Counts());
    }

    [Fact]
    public void Missing_read_and_incompatible_step_basis_cannot_trigger_a_handoff()
    {
        foreach(bool hidden in new[]{false,true})
        {
            var pool=new GapSettlement([8,2,2],hiddenGap:hidden?0:-1,stepUnits:hidden?null:[1,0,0]);
            var before=pool.GapBytes(0);pool.Settle();
            Assert.True(pool.Quiet);Assert.Equal(before,pool.GapBytes(0));Assert.Equal(0,pool.Moves);
            Assert.Null(pool.Audit().Spread);Assert.False(pool.Audit().Even);Assert.False(pool.Audit().WithinOne);
        }
    }

    [Fact]
    public void Finer_rung_keeps_the_parent_budget_and_can_resolve_the_indivisible_remainder()
    {
        foreach(int shape in Enumerable.Range(0,6))
        {
            var coarse=new GapStudy(shape,1);var fine=new GapStudy(shape,2);
            Assert.Equal(coarse.ParentAllocation,fine.ParentAllocation);
            Assert.Equal(coarse.Initial.Sum()*fine.Subdivision.Count,fine.Initial.Sum());
            coarse.Pool.Settle();fine.Pool.Settle();
            Assert.True(coarse.Pool.Quiet);Assert.True(fine.Pool.Quiet);
            Assert.True(coarse.Pool.Audit().WithinOne);Assert.False(coarse.Pool.Audit().Even);
            Assert.True(fine.Pool.Audit().Even);
            Assert.Equal(shape!=0,fine.RegularNow);
            Assert.Equal(fine.Initial.Sum(),fine.Pool.Counts().Sum());
            if(fine.Sketch is {} sketch)
            {
                Assert.Equal(HintExamples.Sketch(false).SiteBytes(),sketch.SiteBytes());
                Assert.Equal(7,sketch.Read().RightTurns);
            }
        }
    }

    [Fact]
    public void All_demo_wirings_stop_without_claiming_hidden_or_unreached_equality()
    {
        foreach(int shape in Enumerable.Range(0,6))
        foreach(int budget in Enumerable.Range(0,3))
        foreach(int wiring in Enumerable.Range(0,4))
        {
            var study=new GapStudy(shape,budget,wiring);var bits=study.BindingBytes();
            study.Pool.Settle();Assert.True(study.Pool.Quiet);
            Assert.Equal(study.Initial.Sum(),study.Pool.Counts().Sum());Assert.Equal(bits,study.BindingBytes());
            if(wiring==0)Assert.True(study.Pool.Audit().WithinOne);
            if(wiring==2)Assert.Null(study.Pool.Audit().Spread);
            if(wiring==3)Assert.Equal(study.Initial[0],study.Pool.Counts()[0]);
            Assert.Contains("<svg",GapDrawing.Draw(study).Svg());
        }
    }
}
