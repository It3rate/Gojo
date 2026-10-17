using Gojo.Core;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class EcologyTests
{
    [Fact]
    public void WorldKnobsRetainLightPositionsPeriodsAndSeededPhases()
    {
        var original = new TimedLightField(7);
        var focused = new TimedLightField(7, EcologySettings.Focused);
        foreach (var (a, b) in original.Schedules.Zip(focused.Schedules))
        {
            Assert.Equal(a.X, b.X); Assert.Equal(a.Y, b.Y);
            Assert.Equal(a.PeriodTicks, b.PeriodTicks); Assert.Equal(a.PhaseTicks, b.PhaseTicks);
            Assert.Equal(60, b.Radius); Assert.Equal((int)Math.Round(a.OnTicks * .85), b.OnTicks);
        }
    }

    private static Bath FeedingFixture(int capacity, int population)
    {
        var world = new Bath(7, population, resources:MachineResources.Fixed(.9), cognitiveEcology:true,
            ecology:new(PatchHarvestPerPulse:capacity)) { ExposureRisk=false, Breeding=false, Learning=false };
        foreach (var c in world.Creatures)
        {
            c.X.Tick.Set((long)(world.LightX * 16)); c.Y.Tick.Set((long)(world.LightY * 16));
            c.LeftMuscle.InstallInherited(new(world.Units)); c.RightMuscle.InstallInherited(new(world.Units));
        }
        world.Step(4); world.CheckConservation();
        return world;
    }

    [Fact]
    public void NeighborsCompeteForOneFiniteNativeStockAndUnusedFoodIsReturned()
    {
        var single = FeedingFixture(12, 1);
        var crowd = FeedingFixture(12, 3);
        var original = FeedingFixture(0, 3);
        Assert.Equal(10, single.TotalFood); Assert.Equal(12, crowd.TotalFood); Assert.Equal(30, original.TotalFood);
        Assert.Equal(18UL, crowd.LocalFoodShortfall.Value);
        Assert.All(crowd.LocalPhotoPools, pool => Assert.Equal(0UL, pool.Value));
        Assert.Same(crowd.FeedingPool(crowd.LightX, crowd.LightY), crowd.LocalPhotoPools[0]);
        var decoded = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(crowd.Root)).Single();
        var stocks = Parts((Relation)Parts(decoded)[2]).Cast<Carrier>().ToArray();
        Assert.Equal(9, stocks.Length);
        var bodies = Parts((Relation)Parts(decoded)[4]).Cast<Relation>();
        var bodyEnergy = bodies.Sum(c => (long)((Carrier)Parts((Relation)Parts(c)[3])[0]).Value);
        Assert.Equal(20_000_000, stocks.Sum(c => (long)c.Value) + bodyEnergy);
        Assert.All(stocks.Skip(4), p => Assert.NotEmpty(p.AttachedRelations));
    }

    [Fact]
    public void AveragedEyeControlKeepsBrightnessButRemovesSpatialSteeringInNativeRecipes()
    {
        var world = new Bath(population:1, resources:MachineResources.Fixed(.9), cognitiveEcology:true, binocular:false);
        var c = world.Creatures[0];
        var delta = Parts(c.Cognition!.Pilot.Modules[1].Uses)[3];
        Assert.False(TryValue(ReadMany(delta)[0], out _));
        c.LeftEye.Admit([40],4); c.RightEye.Admit([43],4);
        c.AdvanceController(4); c.LeftMuscle.Wake(c,4); c.RightMuscle.Wake(c,4);
        Assert.True(c.ReadMotion(out var thrust,out var yaw)); Assert.True(thrust > 0); Assert.Equal(0,yaw);
        Assert.True(TryValue(ReadMany(delta)[0], out var difference)); Assert.Equal(0,difference);
        var brightness = Parts(c.Cognition.Pilot.Modules[0].Uses)[1];
        Assert.True(TryValue(ReadMany(brightness)[0], out var light)); Assert.Equal(83,light);
        var restored = (Relation)GojoNumericFormat.Decode(c.ExportNativeGraph()).Single();
        var brain = (Relation)Parts((Relation)Parts(restored)[2])[4];
        var pilot = (Relation)Parts(brain)[5];
        var gradient = (Relation)Parts((Relation)Parts(pilot)[0])[1];
        var restoredDelta = Parts((Relation)Parts(gradient)[1])[3];
        Assert.True(TryValue(ReadMany(restoredDelta)[0], out var archivedDifference)); Assert.Equal(0,archivedDifference);
    }

    [Fact]
    public void ObserverGradientMatchesPhysicalLightAndHasNoCreaturePort()
    {
        var world = new Bath(population:0, resources:MachineResources.Fixed(.9));
        var x = world.LightX - 40; var y = world.LightY - 10;
        var gradient = world.LightGradient(x,y); const double h = .001;
        Assert.Equal((world.Illumination(x+h,y)-world.Illumination(x-h,y))/(2*h),gradient.X,8);
        Assert.Equal((world.Illumination(x,y+h)-world.Illumination(x,y-h))/(2*h),gradient.Y,8);
        Assert.Null(world.HungryGradientAlignment);
    }

    [Fact]
    public void ChangingWorldSettingsRetainsTheCurrentBodiesProgramsAndMemories()
    {
        var world = new Bath(population:2, resources:MachineResources.Fixed(.9), pulsedLights:true, cognitiveEcology:true)
            { ExposureRisk=false, Breeding=false };
        world.Step(17);
        var c = world.Creatures[0]; var brain = c.Cognition!;
        var program = c.LeftMuscle.Current.Root; var memory = Held(brain.PreviousTime);
        var phase = Held(brain.Pilot.Phase); var energy = c.Energy.Value;
        var tick = world.Tick;
        world.ApplyEcology(EcologySettings.Focused);
        Assert.Same(c,world.Creatures[0]); Assert.Same(brain,c.Cognition);
        Assert.Same(program,c.LeftMuscle.Current.Root); Assert.Equal(memory,Held(brain.PreviousTime));
        Assert.Equal(phase,Held(brain.Pilot.Phase)); Assert.Equal(energy,c.Energy.Value); Assert.Equal(tick,world.Tick);
        Assert.All(world.TimedLights.Schedules, s => Assert.Equal(60,s.Radius));
        Assert.Equal(5,world.LocalPhotoPools.Length); world.CheckConservation();
        world.Step(4); world.ApplyEcology(EcologySettings.Original); world.CheckConservation();
        Assert.Empty(world.LocalPhotoPools); Assert.Same(brain,c.Cognition);
    }
}
