using Gojo.Core;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class RovingLightTests
{
    [Theory]
    [InlineData(7)]
    [InlineData(19)]
    public void RovingLightsKeepTheOriginalSchedulesAndHaveReproducibleSlowMotion(int seed)
    {
        var original = new TimedLightField(seed);
        var roving = new TimedLightField(seed, EcologySettings.Roving);
        var repeat = new TimedLightField(seed, EcologySettings.Roving);
        for (var i = 0; i < original.Schedules.Length; i++)
        {
            var a = original.Schedules[i]; var b = roving.Schedules[i];
            Assert.Equal((a.X,a.Y),roving.PositionAt(i,0));
            Assert.Equal(a.PeriodTicks,b.PeriodTicks); Assert.Equal(a.PhaseTicks,b.PhaseTicks);
            Assert.Equal(a.OnTicks,b.OnTicks); Assert.Equal(a.Radius,b.Radius);
            Assert.Equal(1.5,Math.Sqrt(b.VelocityX*b.VelocityX+b.VelocityY*b.VelocityY),10);
            Assert.NotEqual(roving.PositionAt(i,0),roving.PositionAt(i,600));
            Assert.Equal(roving.PositionAt(i,600),repeat.PositionAt(i,600));
            Assert.Equal((a.X,a.Y),original.PositionAt(i,50_000));
        }
    }

    [Fact]
    public void ReflectionAndLightSwitchingNeverTeleportTheMovingSource()
    {
        var field = new TimedLightField(7, EcologySettings.Roving);
        for (var i = 0; i < field.Schedules.Length; i++)
        {
            var s = field.Schedules[i];
            var on = s.PeriodTicks-s.PhaseTicks;
            var off = on+s.OnTicks;
            Assert.Contains(field.At(off-1),p=>p.Source==i);
            Assert.DoesNotContain(field.At(off),p=>p.Source==i);
            Assert.Contains(field.At(on+s.PeriodTicks),p=>p.Source==i);
            // Include off intervals, reactivation and many edge reflections.
            foreach (var tick in new long[] { off-1,off,on+s.PeriodTicks-1,10_000,40_000,100_000 })
            {
                var a = field.PositionAt(i,tick); var b = field.PositionAt(i,tick+1);
                Assert.InRange(b.X,45,Bath.Width-45); Assert.InRange(b.Y,45,Bath.Height-45);
                var moved = Math.Sqrt(Math.Pow(b.X-a.X,2)+Math.Pow(b.Y-a.Y,2));
                Assert.InRange(moved,0,1.5/Bath.TicksPerSecond+1e-9);
            }
            Assert.NotEqual(field.PositionAt(i,off),field.PositionAt(i,on+s.PeriodTicks));
        }
    }

    [Fact]
    public void StartingAndStoppingDriftPreservesCurrentPositionsIncludingInactiveLights()
    {
        var original = new TimedLightField(7);
        var roving = original.WithSettings(EcologySettings.Roving,500);
        var stopped = roving.WithSettings(EcologySettings.Original,1700);
        for (var i = 0; i < original.Schedules.Length; i++)
        {
            Assert.Equal(original.PositionAt(i,500),roving.PositionAt(i,500));
            Assert.Equal(roving.PositionAt(i,1700),stopped.PositionAt(i,1700));
            Assert.Equal(stopped.PositionAt(i,1700),stopped.PositionAt(i,50_000));
            Assert.Equal(original.Schedules[i].PhaseTicks,stopped.Schedules[i].PhaseTicks);
        }
    }

    [Fact]
    public void MovingFoodCapacityRetainsItsNativeStockAndWitnessedSourcePosition()
    {
        var world = new Bath(7,1,resources:MachineResources.Fixed(.9),pulsedLights:true,cognitiveEcology:true,
            ecology:EcologySettings.Roving) { ExposureRisk=false,Breeding=false };
        var pools = world.LocalPhotoPools.ToArray(); var brain = world.Creatures[0].Cognition!;
        world.Step(160); world.CheckConservation();
        var active = world.ActiveLights;
        Assert.NotEmpty(active); Assert.Same(brain,world.Creatures[0].Cognition);
        foreach (var source in active)
        {
            Assert.Same(pools[source.Source],world.FeedingPool(source.X,source.Y));
            var pulse = Parts((Relation)Parts(world.LastPool)[0]);
            var allocation = Parts((Relation)pulse[6]).Cast<Relation>().Single(r=>
                ((CarrierLocation)Parts(r)[1]).Carrier.SameIdentityAs(pools[source.Source]));
            var fields = Parts(allocation); var position = Parts((Relation)fields[0]);
            Assert.Equal((long)(source.X*16),((CarrierLocation)((Relation)position[0]).Left).Ticks);
            Assert.Equal((long)(source.Y*16),((CarrierLocation)((Relation)position[1]).Left).Ticks);
            Assert.Equal(36,((CarrierLocation)fields[1]).Ticks);
        }
        world.ApplyEcology(EcologySettings.Original); world.CheckConservation();
        Assert.Same(brain,world.Creatures[0].Cognition);
        Assert.Equal(active.Select(p=>(p.Source,p.X,p.Y)),world.ActiveLights.Select(p=>(p.Source,p.X,p.Y)));
    }
}
