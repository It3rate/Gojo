using Gojo.Core;
using Gojo.Structures;
using Phototaxis;
using static Phototaxis.Native;

namespace Phototaxis.Tests;

public sealed class PredictionErrorTests
{
    private static Bath World() => new(7, 1, resources: MachineResources.Fixed(.9),
        pulsedLights: true, cognitiveEcology: true)
        { ExposureRisk = false, Breeding = false };

    private static void Observe(CognitiveBody body, long x, long time)
    {
        body.Observations.Admit(x, 1000, 0, time, 0, 0, 0, 0, 0);
        body.Observe();
    }

    [Theory]
    [InlineData(100, 0, -100, 100)]
    [InlineData(50, 32, 50, 50)]
    [InlineData(100, 32, 0, 0)]
    [InlineData(-50, 0, 50, 50)]
    public void ResolvedPredictionKeepsSignedClaimAndImportsItsAbsoluteMismatch(
        long forecast, long displacement, long signedMismatch, long expectedError)
    {
        var body = World().Creatures[0].Cognition!;
        Observe(body, 1000, 4);
        Aim(body.Forecast, forecast);
        var recipe = body.PredictionErrorRecipe;
        Observe(body, 1000 + displacement, 8);

        var prediction = Parts(body.LastPrediction);
        var claim = (Relation)prediction[5];
        Assert.True(OperationClaim.TryRead(claim, out _, out var signed, out _, out _, out _, out _));
        Assert.Equal(signedMismatch, Assert.IsType<CarrierLocation>(signed).Ticks);
        Assert.True(OperationClaim.TryReadResolvedViolation(claim, out var magnitude));
        Assert.Equal(expectedError, magnitude.Ticks);
        Assert.Equal(expectedError, Held(body.Error));
        Assert.Equal(expectedError, body.Pressure.Tension);
        Assert.Equal((ulong)expectedError, body.ErrorTotal.Value);
        Assert.Equal(1UL, body.PredictionChecks.Value);
        Assert.Same(recipe, body.PredictionErrorRecipe);
        Assert.Same(recipe, Parts((Relation)Parts(body.Root)[3])[^1]);
    }

    [Fact]
    public void ReusedRecipeReadsSignedLiveStocksAndPriorAdmissionStaysFrozen()
    {
        using var execution = new NativeExecution(NativeExecutionBackend.Compact);
        var body = World().Creatures[0].Cognition!;
        var recipe = body.PredictionErrorRecipe;
        Aim(body.Forecast, 40); Aim(body.SpeedSquared, 50);
        var earlier = ReadMany(recipe)[0];
        Assert.True(TryValue(earlier, out var error)); Assert.Equal(10, error);
        Aim(body.SpeedSquared, 100);
        Assert.True(TryValue(ReadMany(recipe)[0], out error)); Assert.Equal(60, error);
        var builds = execution.PlanBuilds;
        Aim(body.Forecast, -20); Aim(body.SpeedSquared, -30);
        Assert.True(TryValue(ReadMany(recipe)[0], out error)); Assert.Equal(10, error);
        Aim(body.SpeedSquared, -20);
        Assert.True(TryValue(ReadMany(recipe)[0], out error)); Assert.Equal(0, error);
        Assert.Equal(builds, execution.PlanBuilds);
        Assert.True(execution.CacheHits > 0);
        Assert.True(TryValue(earlier, out error)); Assert.Equal(10, error);

        Observe(body, 1000, 4); Aim(body.Forecast, 100);
        Observe(body, 1000, 8);
        var prior = Parts(body.LastPrediction);
        var priorError = (Relation)Parts((Relation)prior[7])[0];
        Aim(body.Forecast, 300); Observe(body, 1032, 12);
        Assert.Equal(200, Held(body.Error));
        Assert.Equal(100, Assert.IsType<CarrierLocation>(prior[2]).Ticks);
        Assert.True(TryValue(priorError, out error)); Assert.Equal(100, error);
        Assert.True(OperationClaim.TryReadResolvedViolation((Relation)prior[5], out var priorMagnitude));
        Assert.Equal(100, priorMagnitude.Ticks);
    }

    [Theory]
    [InlineData(-1048577, false)]
    [InlineData(-1048576, false)]
    [InlineData(-1048575, true)]
    [InlineData(0, true)]
    [InlineData(1048575, true)]
    [InlineData(1048576, false)]
    [InlineData(1048577, false)]
    public void SignedScopeMatchesTheFormerMagnitudeReadAtBothBoundaries(long forecast, bool complete)
    {
        var world = World(); var body = world.Creatures[0].Cognition!;
        Aim(body.Forecast, forecast);
        var magnitude = L(world.Units.Feature, Math.Abs(forecast));
        var former = ExpressionGraph.Choose(world.Units,
            ExpressionGraph.Less(world.Units, magnitude, L(world.Units.Feature, 0)),
            Scale(magnitude, -1, 1), magnitude);
        var previous = ReadMany(former)[0];
        var current = ReadMany(body.PredictionErrorRecipe)[0];
        Assert.Equal(complete, TryValue(previous, out var oldValue));
        Assert.Equal(complete, TryValue(current, out var newValue));
        if (complete) Assert.Equal(oldValue, newValue);

        Observe(body, 1000, 4); Aim(body.Error, 17);
        Observe(body, 1000, 8);
        Assert.Equal(complete ? Math.Abs(forecast) : 17, Held(body.Error));
        Assert.Equal(complete ? 1UL : 0UL, body.PredictionChecks.Value);
    }

    [Fact]
    public void UnresolvedMotionCannotAdmitAnOtherwiseReadablePredictionError()
    {
        var body = World().Creatures[0].Cognition!;
        Observe(body, 1000, 4); Aim(body.Forecast, 100); Aim(body.Error, 17);
        Assert.True(TryValue(ReadMany(body.PredictionErrorRecipe)[0], out _));
        Observe(body, 1000 + 2 * ExpressionGraph.SelectionExtent, 8);
        var prediction = Parts(body.LastPrediction);
        Assert.False(TryValue((Relation)prediction[4], out _));
        Assert.True(OperationClaim.TryReadResolvedViolation((Relation)prediction[5], out _));
        Assert.Equal(17, Held(body.Error));
        Assert.Equal(0UL, body.PredictionChecks.Value);
        Assert.IsType<Placeholder>(Assert.Single(Parts((Relation)prediction[7])));
    }
}
