using Gojo.Core;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class InjuryRecoveryTests
{
    private static Bath World() => new(7, 2, resources:MachineResources.Fixed(.9),
        cognitiveEcology:true, pulsedLights:true, ecology:EcologySettings.Roving, eyeLevels:16)
        { ExposureRisk=false, Breeding=false };

    private static void Sample(Creature c, long time, int injury)
    {
        c.Cognition!.Observations.Admit(1000, 1000, 0, time, 0, 0, 0, 0, injury);
        c.Cognition.Observe(); c.AdvanceController(time);
        c.LeftMuscle.Wake(c,time); c.RightMuscle.Wake(c,time);
    }

    [Fact]
    public void ZeroTurnFounderEscapesStraightAndParticipatesInItsRecoveryModule()
    {
        var w = World(); var c = w.Creatures[0]; var pilot = c.Cognition!.Pilot;
        c.LeftEye.Admit([12],4); c.RightEye.Admit([3],4);
        Assert.Equal(0,Held(pilot.Modules[3].Parameters[0]));
        Sample(c,4,7);
        Assert.Equal(104,Held(pilot.RecoveryUntil));
        Assert.True(c.ReadMotion(out var thrust,out var yaw)); Assert.True(thrust>0); Assert.Equal(0,yaw);
        Assert.True(TryValue(ReadMany(pilot.Modules[3].Activity)[0],out var active)); Assert.Equal(15,active);
        Assert.NotEmpty(Parts(pilot.Modules[3].Uses)); Assert.Equal(11,Parts(c.ControllerState).Length);
        Sample(c,28,7); Assert.True(c.ReadMotion(out thrust,out yaw)); Assert.True(thrust>0); Assert.Equal(0,yaw);
        w.CheckConservation();
    }

    [Fact]
    public void ContinuedInjuryExtendsStraightEscapeWithoutExtendingTheTurn()
    {
        var w = World(); var c = w.Creatures[1]; var pilot = c.Cognition!.Pilot;
        c.LeftEye.Admit([12],4); c.RightEye.Admit([3],4);
        Sample(c,4,7);
        Assert.Equal(24,Held(pilot.RecoveryTurnUntil)); Assert.Equal(104,Held(pilot.RecoveryUntil));
        Assert.True(c.ReadMotion(out var thrust,out var yaw)); Assert.True(thrust>0); Assert.True(yaw>0);
        Assert.True(TryValue(ReadMany(pilot.Turn)[0],out var lightTurn)); Assert.Equal(0,lightTurn);
        Sample(c,16,7);
        Assert.Equal(24,Held(pilot.RecoveryTurnUntil)); Assert.Equal(116,Held(pilot.RecoveryUntil));
        Sample(c,28,7);
        Assert.Equal(24,Held(pilot.RecoveryTurnUntil)); Assert.Equal(128,Held(pilot.RecoveryUntil));
        Assert.True(c.ReadMotion(out thrust,out yaw)); Assert.True(thrust>0); Assert.Equal(0,yaw);
        Assert.True(TryValue(ReferenceRead(pilot.RecoveryActive),out var active)); Assert.Equal(15,active);
        Assert.Equal(ReferenceRead(pilot.Turn).Left is CarrierLocation a ? a.Ticks : long.MinValue,
            ReadMany(pilot.Turn)[0].Left is CarrierLocation b ? b.Ticks : long.MaxValue);
        w.CheckConservation();
    }

    [Fact]
    public void ClearedInjuryRestoresLightSteeringAndAFreshEpisodeHasANewTurn()
    {
        var w = World(); var c = w.Creatures[1]; var pilot = c.Cognition!.Pilot;
        c.LeftEye.Admit([12],4); c.RightEye.Admit([3],4);
        Sample(c,4,7); Sample(c,28,0);
        Assert.True(c.ReadMotion(out _,out var yaw)); Assert.Equal(0,yaw);
        Sample(c,108,0);
        Assert.True(TryValue(ReadMany(pilot.RecoveryActive)[0],out var active)); Assert.Equal(0,active);
        Assert.True(c.ReadMotion(out _,out yaw)); Assert.True(yaw<0);
        Sample(c,112,7);
        Assert.Equal(132,Held(pilot.RecoveryTurnUntil)); Assert.Equal(212,Held(pilot.RecoveryUntil));
        Assert.True(c.ReadMotion(out _,out yaw)); Assert.True(yaw>0);
        w.CheckConservation();
    }

    [Fact]
    public void RecoveryMemoryIsIndependentInOffspringAndRetainedAsNativeStructure()
    {
        var w = World(); var parent = w.Creatures[1]; var child = w.Creatures[0];
        Sample(parent,4,7);
        var modules = parent.Cognition!.Pilot.Modules.Select(m=>PilotModule.Copy(w.Units,m,out _)).ToArray();
        child.ConfigureCognition(modules);
        Assert.Equal(0,Held(child.Cognition!.Pilot.RecoveryUntil));
        Assert.Equal(104,Held(parent.Cognition.Pilot.RecoveryUntil));
        Assert.Equal(10,modules[3].Parameters.Length);
        var graph = (Relation)GojoNumericFormat.Decode(parent.ExportNativeGraph()).Single();
        var brain = (Relation)Parts((Relation)Parts(graph)[2])[4];
        var pilot = (Relation)Parts(brain)[5];
        var memory = Parts((Relation)Parts(pilot)[1]);
        Assert.Equal(6,memory.Length);
        Assert.Equal(24,Held((Carrier)memory[4])); Assert.Equal(104,Held((Carrier)memory[5]));
        Assert.Equal(12,Parts(parent.ControllerState).Length); // previous four cells, seven progress cells, emission
        w.CheckConservation();
    }

    [Fact]
    public void ABodyCanLeaveARealStingPatchUsingOnlyItsPhysicalObservations()
    {
        var w = World(); var c = w.Creatures[1]; w.RocksEnabled=false;
        var zone = w.StingZones[0];
        c.X.Tick.Set((long)(zone.X*16)); c.Y.Tick.Set((long)(zone.Y*16)); c.Heading.Tick.Set(0);
        var hurt = false; var lateContact = 0;
        for (var tick=0;tick<140;tick++)
        {
            w.Step(); hurt |= c.Cognition!.Observations.Injury.Value>0;
            if (tick>=80 && Bath.WrappedDistance(c.Px,c.Py,zone.X,zone.Y)<zone.Radius+Bath.BodyRadius) lateContact++;
        }
        Assert.True(hurt); Assert.Contains(c,w.Creatures); Assert.Equal(0,lateContact);
        Assert.True(Bath.WrappedDistance(c.Px,c.Py,zone.X,zone.Y)>zone.Radius+Bath.BodyRadius);
        w.CheckConservation();
    }
}
