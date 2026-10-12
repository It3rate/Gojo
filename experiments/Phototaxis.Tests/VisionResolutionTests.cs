using Gojo.Core;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public class VisionResolutionTests
{
    private static Bath World(int levels) => new(7, 1, resources:MachineResources.Fixed(.9),
        pulsedLights:true, cognitiveEcology:true, eyeLevels:levels) { ExposureRisk=false, Breeding=false };

    [Fact]
    public void FineEyeRetainsItsResolutionAndUnobservedStateInNativeStructure()
    {
        var eye = new Eye(new Units(), -35, 3, 1, 256);
        Assert.False(TryValue(ReadMany(eye.Signal)[0], out _));
        eye.Admit([43], 4);
        Assert.Equal(43UL, eye.Pixels[0].Value); Assert.Equal(255, eye.Maximum.Ticks);
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(eye.Root)).Single();
        var calibration = Parts((Relation)Parts(restored)[7]);
        Assert.Equal(255, ((CarrierLocation)calibration[0]).Ticks);
        Assert.True(TryValue(ReadMany((Relation)calibration[1])[0], out var signal)); Assert.Equal(43, signal);
        Assert.True(TryValue(ReadMany((Relation)calibration[2])[0], out var coarse)); Assert.Equal(2, coarse);
    }

    [Fact]
    public void SubBandEyeDifferenceCanSteerWithoutIncreasingPhysicalGain()
    {
        var low = World(16).Creatures[0]; var high = World(256).Creatures[0];
        low.LeftEye.Admit([2],4); low.RightEye.Admit([2],4);
        high.LeftEye.Admit([40],4); high.RightEye.Admit([43],4);
        Assert.Equal(2, ((CarrierLocation)ReadMany(high.LeftEye.Mean)[0].Left).Ticks);
        Assert.Equal(2, ((CarrierLocation)ReadMany(high.RightEye.Mean)[0].Left).Ticks);
        foreach (var c in new[] { low, high })
        { c.AdvanceController(4); c.LeftMuscle.Wake(c,4); c.RightMuscle.Wake(c,4); }
        Assert.True(low.ReadMotion(out _,out var coarseYaw)); Assert.Equal(0, coarseYaw);
        Assert.True(high.ReadMotion(out _,out var fineYaw)); Assert.Equal(2, fineYaw);
        Assert.Equal(9UL, high.LeftMuscle.Force.Value); Assert.Equal(7UL, high.RightMuscle.Force.Value);
    }

    [Theory]
    [InlineData(3,4)]
    [InlineData(12,3)]
    [InlineData(8,8)]
    public void EqualPhysicalBrightnessKeepsExistingStrategyAndForce(int left,int right)
    {
        var low = World(16).Creatures[0]; var high = World(256).Creatures[0];
        low.LeftEye.Admit([left],4); low.RightEye.Admit([right],4);
        high.LeftEye.Admit([left*17],4); high.RightEye.Admit([right*17],4);
        foreach (var c in new[] { low, high })
        { c.AdvanceController(4); c.LeftMuscle.Wake(c,4); c.RightMuscle.Wake(c,4); }
        Assert.Equal(low.LeftMuscle.Force.Value, high.LeftMuscle.Force.Value);
        Assert.Equal(low.RightMuscle.Force.Value, high.RightMuscle.Force.Value);
        Assert.Equal(Held(low.Cognition!.Pilot.Appetite), Held(high.Cognition!.Pilot.Appetite));
    }

    [Fact]
    public void FinePhotographUsesTheSameRayAndWorld()
    {
        var low = World(16); var high = World(256);
        var a = low.Creatures[0]; var b = high.Creatures[0];
        Assert.Equal(a.X.Ticks,b.X.Ticks); Assert.Equal(a.Y.Ticks,b.Y.Ticks); Assert.Equal(a.Heading.Ticks,b.Heading.Ticks);
        var coarse = low.Photograph(a,a.LeftEye)[0]; var fine = high.Photograph(b,b.LeftEye)[0];
        Assert.InRange(coarse,0,15); Assert.InRange(fine,0,255); Assert.InRange(Math.Abs(fine-coarse*17),0,9);
        Assert.Equal(256, new Bath(cognitiveEcology:true).Creatures[0].LeftEye.Levels);
    }
}
