using Gojo.Core;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class CognitiveTests
{
    private static Bath World(int population = 1, bool inquiries = false) => new(7, population,
        resources: MachineResources.Fixed(.9), pulsedLights: true, cognitiveEcology: true, inquiries: inquiries, eyeLevels:16)
        { ExposureRisk = false, Breeding = false };
    private static void Observe(CognitiveBody b, long x, long y, long t)
    { b.Observations.Admit(x, y, 0, t, 0, 0, 0, 0, 0); b.Observe(); }

    [Fact]
    public void ComputationPriceIsVisibleAndWorkStillCostsEnergyProportionally()
    {
        var w = World(2); var a = w.Creatures[0]; var b = w.Creatures[1];
        Assert.Equal(100UL, a.Computation.Rate.Value);
        a.Computation.Runtime.Add((ulong)System.Diagnostics.Stopwatch.Frequency);
        b.Computation.Runtime.Add((ulong)(System.Diagnostics.Stopwatch.Frequency * 2));
        a.Computation.Bill(a.Energy,w.MetabolicSink); b.Computation.Bill(b.Energy,w.MetabolicSink);
        Assert.Equal(100UL, a.Computation.Billed.Value); Assert.Equal(200UL,b.Computation.Billed.Value);
        w.CheckConservation();
    }

    [Fact]
    public void EstimatedJevUtilityIsConfidenceCalibratedAndNeverAnObservedReturn()
    {
        var w = World(inquiries:true); var c = w.Creatures[0]; var b = c.Cognition!; Observe(b, 1000, 1000, 4);
        var module = b.Pilot.Modules[1];
        var q = b.Inquiry.Ask("Which module deserves an estimated bid?", new("Keep", module.EstimatedUtility, 0),
            new("Gradient", module.EstimatedUtility, 20))!;
        b.Inquiry.Admit(q, "{\"answers\":{\"decision\":{\"type\":\"choice\",\"probabilities\":{\"option_0\":0.2,\"option_1\":0.8},\"confidence\":0.5}}}");
        Assert.Equal(10, Held(module.EstimatedUtility)); Assert.Equal(0UL, module.Utility.Visits.Value);
        Assert.Equal(0, Held(module.Utility.Returns)); Assert.Equal(1UL, b.Inquiry.AdviceTrials.Value);
        var childModule = PilotModule.Copy(w.Units, module, out _); Assert.Equal(0, Held(childModule.EstimatedUtility));
        var forbidden = b.Inquiry.Ask("Try to create food?", new("Keep"), new("Energy", c.Energy, 1200))!;
        var before = c.Energy.Value;
        b.Inquiry.Admit(forbidden, "{\"answers\":{\"decision\":{\"type\":\"choice\",\"probabilities\":{\"option_0\":0.2,\"option_1\":0.8},\"confidence\":0.9}}}");
        Assert.Equal(before, c.Energy.Value); w.CheckConservation();
    }

    [Fact]
    public void FounderPilotUsesItsOwnMemoryToSeekAndLeaveLight()
    {
        var w = World(); var c = w.Creatures[0];
        c.LeftEye.Admit([12],4); c.RightEye.Admit([3],4);
        c.AdvanceController(4); c.LeftMuscle.Wake(c,4); c.RightMuscle.Wake(c,4);
        Assert.True(c.ReadMotion(out var thrust, out var yaw)); Assert.True(thrust > 0); Assert.True(yaw < 0);
        Transfer(w.CalorieReserve, c.Energy, 1150 - Held(c.Energy)); c.AdvanceController(8);
        c.LeftMuscle.Wake(c,8); c.RightMuscle.Wake(c,8);
        Assert.Equal(0UL,c.Cognition!.Pilot.Appetite.Value); Assert.True(c.ReadMotion(out thrust,out yaw)); Assert.True(yaw > 0);
        w.CheckConservation();
        var statelessWorld = World(3); var stateless = statelessWorld.Creatures[2];
        Transfer(statelessWorld.CalorieReserve, stateless.Energy, 1150 - Held(stateless.Energy));
        stateless.AdvanceController(4); Assert.Equal(0UL, stateless.Cognition!.Pilot.Appetite.Value);
    }

    [Fact]
    public void NegativeInheritedParametersRetainTheirNativeSignedTension()
    {
        var world = World(5); var parent = world.Creatures[4].Cognition!.Pilot.Modules[3];
        Assert.Equal(-1, Held(parent.Parameters[3]));
        var copy = PilotModule.Copy(world.Units, parent, out var correspondence);
        Assert.Equal(-1, Held(copy.Parameters[3])); Assert.Equal(-1, copy.Parameters[3].Tension);
        Assert.NotEmpty(Parts(correspondence));
    }

    [Fact]
    public void MotionIsDerivedFromTimestampedPositionAndWrappedDistance()
    {
        var b = World().Creatures[0].Cognition!;
        Assert.False(TryValue(ReadMany(b.MotionRecipe)[0], out _)); // dt=0 remains undefined.
        Observe(b, 599 * 16, 200 * 16, 4);
        b.Observations.Admit(1 * 16, 200 * 16, 0, 8, 0, 0, 0, 0, 0);
        Assert.True(TryValue(ReferenceRead(b.MotionRecipe), out var speed)); Assert.Equal(100, speed);
        b.Observe();
        Assert.Equal(100, Held(b.SpeedSquared)); // 2px / .2 sec = 10px/sec.
        Assert.False(TryValue(ReferenceRead(b.MotionRecipe), out _)); // The next interval has not arrived yet.
        Observe(b, 1 * 16, 202 * 16, 12); Assert.Equal(100, Held(b.SpeedSquared));
        Assert.Equal(3UL, b.Samples.Value);
    }

    [Fact]
    public void ForecastIsFrozenAndMismatchHasAnExplicitPressureImport()
    {
        var c = World().Creatures[0]; var b = c.Cognition!;
        Observe(b, 1000, 1000, 4); Aim(c.LeftMuscle.Force, 8); Aim(c.RightMuscle.Force, 8); b.ForecastNext(4);
        var expected = Held(b.Forecast); Assert.True(expected > 600);
        Observe(b, 1000, 1000, 8);
        Assert.Equal(expected, Held(b.Forecast)); Assert.Equal(expected, Held(b.Error));
        Assert.Equal(expected, b.Pressure.Tension);
        var fields = Parts(b.LastPrediction);
        Assert.True(OperationClaim.TryReadResolvedViolation((Relation)fields[5], out var residual));
        Assert.Equal(expected, residual.Ticks); Assert.NotEmpty(Parts((Relation)fields[7]));
    }

    [Fact]
    public void ParentUtilitySelectsConnectedModulesAndChildMemoryIsIndependent()
    {
        var world = World(3); var a = world.Creatures[0]; var b = world.Creatures[1]; var child = world.Creatures[2];
        var am = a.Cognition!.Pilot; var bm = b.Cognition!.Pilot;
        bm.Modules[1].Utility.Record(L(world.Units.Return, 100), 20, 0);
        var modules = CognitivePilot.Mate(world.Units, am, bm, child, 40, true, out var market);
        child.ConfigureCognition(modules, ancestry: market);
        Assert.Equal(Held(bm.Modules[1].Parameters[0]), Held(modules[1].Parameters[0]));
        Assert.False(bm.Modules[1].Parameters[0].SameIdentityAs(modules[1].Parameters[0]));
        Aim(child.Cognition!.Pilot.Appetite, 0); Assert.Equal(15, Held(am.Appetite));
        Assert.Equal(4, Parts((Relation)Parts(market)[1]).Length);
        Assert.Equal(20UL, child.Wallet.Value); world.CheckConservation();
        var decoded = (Relation)GojoNumericFormat.Decode(child.ExportNativeGraph()).Single();
        var brain = (Relation)Parts((Relation)Parts(decoded)[2])[4];
        Assert.Equal(8, Parts(brain).Length); // All meaning remains available after discarding helpers.
    }

    [Fact]
    public void JevTypedReplyBecomesANativeBoundedTrialAndStaleReplyCannotAct()
    {
        var c = World(inquiries: true).Creatures[0]; var b = c.Cognition!; Observe(b, 1000, 1000, 4);
        var q = b.Inquiry.Ask("Which component causes a right turn?", new("Keep", b.Pilot.AdviceTurn, 0),
            new("Increase left force", b.Pilot.AdviceTurn, 6))!;
        Assert.NotNull(q); Assert.Contains("Which component", NativeInquiry.Request(q, c));
        var reply = "{\"answers\":{\"decision\":{\"type\":\"choice\",\"probabilities\":{\"option_0\":0.1,\"option_1\":0.9},\"confidence\":0.8}}}";
        b.Inquiry.Admit(q, reply); Assert.Equal(6, Held(b.Pilot.AdviceTurn)); Assert.Equal(1UL, b.Inquiry.AdviceTrials.Value);
        Assert.Equal(900_000UL, q.Options[1].Probability.Value);
        var second = b.Inquiry.Ask("Try the opposite?", new("Keep", b.Pilot.AdviceTurn, 0),
            new("Left", b.Pilot.AdviceTurn, -6))!;
        Observe(b, 1000, 1000, 100); b.Inquiry.Admit(second, reply);
        Assert.Equal(6, Held(b.Pilot.AdviceTurn)); Assert.Equal(1UL, b.Inquiry.Stale.Value);
        Assert.Equal(1UL, b.Inquiry.AdviceTrials.Value);
    }

    [Fact]
    public void RecoverableHazardsOnlyTransferPhysicalEnergyAndAdmitRawInjury()
    {
        var w = World(); var c = w.Creatures[0]; var b = c.Cognition!;
        c.X.Tick.Set(159 * 16); c.Y.Tick.Set(233 * 16);
        // Disable thrust for this physical fixture, with empty actuator programs.
        c.LeftMuscle.InstallInherited(new(w.Units)); c.RightMuscle.InstallInherited(new(w.Units));
        w.Step(24); Assert.True(w.Injuries > 0); Assert.True(b.Observations.Injury.Value > 0);
        Assert.True(c.Energy.Value > 0); Assert.Single(w.Creatures); w.CheckConservation();
        Assert.True(b.Utility.Visits.Value > 0); Assert.NotEmpty(Parts(b.LastOutcome));
    }
}
