using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveLearning.Tests;

public sealed class ExecutionBoundaryTests
{
    [Fact]
    public void OpenIntentRetainsItsWitnessAndExecutesHoldWithKnownQueryFee()
    {
        var world = new RelayWorld(new("fixture"));
        var open = NativePublication.Open(world.ActionAxis);
        var command = NativeFallback.Choose(open, world.ActionAxis, 0);
        Assert.Equal(0, NativePublication.Read(command, world.ActionAxis));
        Assert.True(ReadWitness.TryRead(command, out var source, out _, out _, out _, out _));
        Assert.Same(open, source);
        var episode = new RelayEpisode(0, 4, true, 0, 1, 1, false, false);
        Assert.Equal(-1, NativePublication.Read(world.Execute(episode, command, 1), world.UtilityAxis));
    }

    [Fact]
    public void PortCalibrationPublishesDirectionAndExplicitInverseWithoutChangingSource()
    {
        var source = new Carrier(); var target = new Carrier(); var input = NativePublication.Point(source, 1);
        Assert.Equal(1, NativePublication.Read(RelayBrain.Calibrate(input, source, target, -1, 1), target));
        Assert.Equal(-1, NativePublication.Read(RelayBrain.Calibrate(input, source, target, -1, 1, true), target));
        Assert.Equal(1, NativePublication.Read(input, source));
        Assert.Null(NativePublication.Read(RelayBrain.Calibrate(input, new Carrier(), target, -1, 1), target));
    }

    [Fact]
    public void WholeRouteFeedbackAddsBackFeeBeforeSeparateQueryCostAccounting()
    {
        var utility = new Carrier(); var cost = new Carrier(); var benefit = new Carrier();
        var gross = RelayBrain.GrossBenefit(NativePublication.Point(utility, 3), NativePublication.Point(cost, 1), utility, cost, benefit);
        Assert.Equal(4, NativePublication.Read(gross, benefit));
        Assert.Null(NativePublication.Read(RelayBrain.GrossBenefit(NativePublication.Open(utility),
            NativePublication.Point(cost, 1), utility, cost, benefit), benefit));
    }

    [Fact]
    public void FrozenProjectionAllowsOperationalReadsWithoutChangingLearnedSlots()
    {
        var world = new RelayWorld(new("fixture")); var brain = new RelayBrain(world); var before = brain.LearnedSignature();
        var ep = new RelayEpisode(0, 4, true, 0, 1, 1, false, false);
        brain.Adaptive.Predict(world.Acquire(ep), world.Compare);
        brain.Predictor.Predict(world.Context(ep), world.Previous(ep));
        brain.Query.Decide(world.Context(ep), false);
        brain.Program.Act(NativePublication.Point(world.FitAxis, 1), world.Previous(ep), false);
        Assert.Equal(before, brain.LearnedSignature());
    }
}
