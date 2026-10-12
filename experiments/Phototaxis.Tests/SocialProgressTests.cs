using Gojo.Core;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class SocialProgressTests
{
    private static Bath World(int population = 2, double scale = 1) => new(7, population,
        resources:MachineResources.Fixed(.9), cognitiveEcology:true, pulsedLights:true,
        ecology:new(LightOnScale:.01, LightRadius:1), arenaScale:scale)
        { ExposureRisk=false, Breeding=false, RecoverableHazards=false };

    private static void Sample(Creature c, long time, long x = 1600, long y = 1600,
        int leftMate = 0, int rightMate = 0, int ahead = 0)
    {
        c.Cognition!.Observations.Admit(x,y,0,time,0,0,0,0,0,leftMate,rightMate,ahead);
        c.AdvanceController(time);
        c.LeftMuscle.Wake(c,time); c.RightMuscle.Wake(c,time);
    }

    [Fact]
    public void FallingEnergyAndLittleNetMovementCauseABoundedStraightEscape()
    {
        var w = World(); var c = w.Creatures[1]; var p = c.Cognition!.Pilot.Progress!;
        c.LeftEye.Admit([0],4); c.RightEye.Admit([255],4);
        Sample(c,4);
        Transfer(c.Energy,w.MetabolicSink,120);
        Sample(c,164,1600+8*16,1600);
        Assert.Equal(1UL,p.Episodes.Value); Assert.Equal(244,Held(p.Until));
        Assert.True(c.ReadMotion(out var thrust,out var yaw)); Assert.True(thrust>0); Assert.Equal(0,yaw);
        Assert.True(TryValue(ReferenceRead(p.Active),out var active)); Assert.Equal(15,active);
        Sample(c,248,2500,1600);
        Assert.True(c.ReadMotion(out _,out yaw)); Assert.True(yaw>0);
        Assert.Equal(1UL,p.Episodes.Value); w.CheckConservation();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FeedingOrUsefulDisplacementDoesNotTriggerEscape(bool feeding)
    {
        var w = World(); var c = w.Creatures[1]; var p = c.Cognition!.Pilot.Progress!;
        Sample(c,4);
        if (feeding) Transfer(w.CalorieReserve,c.Energy,80); else Transfer(c.Energy,w.MetabolicSink,120);
        Sample(c,164,feeding ? 1600 : 1600+100*16,1600);
        Assert.Equal(0UL,p.Episodes.Value); Assert.Equal(0,Held(p.Until)); w.CheckConservation();
    }

    [Fact]
    public void ProgressMemoryAndItsClockRemainNativeAndIndependentInOffspring()
    {
        var w = World(); var parent = w.Creatures[1]; var child = w.Creatures[0];
        Sample(parent,4); Transfer(parent.Energy,w.MetabolicSink,100); Sample(parent,164);
        var inherited = parent.Cognition!.Pilot.Modules.Select(m=>PilotModule.Copy(w.Units,m,out _)).ToArray();
        child.ConfigureCognition(inherited);
        Assert.Equal(244,Held(parent.Cognition.Pilot.Progress!.Until));
        Assert.Equal(0,Held(child.Cognition!.Pilot.Progress!.Until));
        Assert.Equal(0UL,child.Cognition.Pilot.Progress.Ready.Value);
        var decoded = (Relation)GojoNumericFormat.Decode(parent.ExportNativeGraph()).Single();
        var brain = (Relation)Parts((Relation)Parts(decoded)[2])[4];
        var pilot = (Relation)Parts(brain)[5];
        var progress = (Relation)Parts(pilot)[6]; var memory = Parts((Relation)Parts(progress)[0]);
        Assert.Equal(7,memory.Length); Assert.Equal(244,Held((Carrier)memory[5])); Assert.Equal(1UL,((Carrier)memory[6]).Value);
        Assert.True(TryValue(ReadMany(Parts(progress)[3])[0],out var active)); Assert.Equal(15,active);
        w.CheckConservation();
    }

    [Fact]
    public void ProgressAndMotionUseTheSameExpandedPeriodicPositionFrame()
    {
        var w = World(scale:1.25); var c = w.Creatures[1]; var b = c.Cognition!;
        Assert.Equal(750,w.ArenaWidth); Assert.Equal(510,w.ArenaHeight);
        Assert.True(ScopedChoice.TrySpan((Relation)Parts(c.PositionFrame)[0],out _,out _,out var extent)); Assert.Equal(12000,extent);
        b.Observations.Admit(749*16,200*16,0,4,0,0,0,0,0); b.Observe(); c.AdvanceController(4);
        b.Observations.Admit(1*16,200*16,0,8,0,0,0,0,0); b.Observe();
        Assert.Equal(100,Held(b.SpeedSquared));
        var decoded = (Relation)GojoNumericFormat.Decode(c.ExportNativeGraph()).Single();
        var decodedFrame = (Relation)Parts((Relation)Parts(decoded)[0])[3];
        Assert.True(ScopedChoice.TrySpan((Relation)Parts(decodedFrame)[0],out _,out _,out extent)); Assert.Equal(12000,extent);
    }

    [Fact]
    public void CourtshipUsesOwnReadinessAndDirectionalSignalsWithARearFallback()
    {
        var w = World(); var c = w.Creatures[1]; var p = c.Cognition!.Pilot;
        Transfer(w.CalorieReserve,c.Energy,1100-Held(c.Energy)); Aim(c.Age,400);
        c.LeftEye.Admit([0],404); c.RightEye.Admit([0],404);
        Sample(c,404,leftMate:0,rightMate:12,ahead:12);
        Assert.Equal(15UL,p.CourtshipSignal.Value);
        Assert.True(c.ReadMotion(out var thrust,out var yaw)); Assert.True(thrust>0); Assert.True(yaw>0);
        Sample(c,408,leftMate:12,rightMate:0,ahead:12);
        Assert.True(c.ReadMotion(out _,out yaw)); Assert.True(yaw<0);
        Sample(c,412,leftMate:6,rightMate:6,ahead:-15);
        Assert.True(c.ReadMotion(out _,out yaw)); Assert.True(yaw>0); // behind is not mistaken for straight ahead
        Aim(c.NextMating,600); Sample(c,416,leftMate:0,rightMate:12,ahead:12);
        Assert.Equal(0UL,p.CourtshipSignal.Value);
        Assert.True(TryValue(ReadMany(p.CourtshipActive)[0],out var active)); Assert.Equal(0,active);
        w.CheckConservation();
    }

    [Fact]
    public void PhysicalMateSensingReadsEmissionsAndRespectsRangeAndOpaqueRock()
    {
        var w = World(); var a = w.Creatures[0]; var b = w.Creatures[1];
        a.X.Tick.Set(100*16); a.Y.Tick.Set(100*16); a.Heading.Tick.Set(0);
        b.X.Tick.Set(140*16); b.Y.Tick.Set(100*16);
        a.LeftMuscle.InstallInherited(new(w.Units)); a.RightMuscle.InstallInherited(new(w.Units));
        b.LeftMuscle.InstallInherited(new(w.Units)); b.RightMuscle.InstallInherited(new(w.Units));
        w.Clock.Add(400); Aim(b.Age,400); Transfer(w.CalorieReserve,b.Energy,1100-Held(b.Energy));
        b.LeftEye.Admit([0],400); b.RightEye.Admit([0],400);
        Aim(b.Cognition!.Pilot.CourtshipSignal,15); w.Step(4);
        Assert.True(a.Cognition!.Observations.LeftMate.Value+a.Cognition.Observations.RightMate.Value>0);
        Assert.True(Held(a.Cognition.Observations.MateAhead)>0);
        b.X.Tick.Set(200*16); Aim(b.Cognition.Pilot.CourtshipSignal,15); w.Step(4);
        Assert.Equal(0UL,a.Cognition.Observations.LeftMate.Value+a.Cognition.Observations.RightMate.Value);
        a.X.Tick.Set(141*16); a.Y.Tick.Set(126*16); b.X.Tick.Set(201*16); b.Y.Tick.Set(126*16);
        w.RocksEnabled=true; Aim(b.Cognition.Pilot.CourtshipSignal,15); w.Step(4);
        Assert.Equal(0UL,a.Cognition.Observations.LeftMate.Value+a.Cognition.Observations.RightMate.Value);
        w.CheckConservation();
    }

    [Fact]
    public void TwoReadyBodiesCanSteerIntoPhysicalContactAndBreed()
    {
        var w = World(); var a = w.Creatures[0]; var b = w.Creatures[1];
        a.ConfigureCognition(CognitivePilot.Founder(w.Units,1)); // active courtship on both sides
        a.X.Tick.Set(80*16); a.Y.Tick.Set(80*16); a.Heading.Tick.Set(0);
        b.X.Tick.Set(140*16); b.Y.Tick.Set(80*16); b.Heading.Tick.Set(2048);
        foreach (var c in w.Creatures)
        { Transfer(w.CalorieReserve,c.Energy,1100-Held(c.Energy)); }
        w.Clock.Add(400); w.Breeding=true;
        for (var tick=0;tick<120 && w.Births==0;tick++) w.Step();
        Assert.True(w.Births>0); Assert.True(w.Creatures.Length>=3);
        Assert.True(w.ArenaDistance(a.Px,a.Py,b.Px,b.Py)<=Bath.MatingDistance);
        Assert.NotEmpty(Parts(w.LastBreeding)); w.CheckConservation();
    }
}
