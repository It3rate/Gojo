using Gojo.Core;
using Gojo.Structures;

namespace PrimitiveForager.Tests;

public sealed class ForagingBoundaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ACompletedCycleDoesNotRearmFromAnExistingHighCharge(bool dark)
    {
        var world = new ForagingWorld(7);
        var tick = 0;
        while (world.X != world.PeakX(tick))
        {
            Assert.True(tick < 30, "The deterministic first patch should be reachable before its cloud.");
            var action = Math.Sign(world.PeakX(tick) - world.X);
            world.Advance(tick++, action, 0);
        }
        while (world.Charge < 100)
        {
            Assert.True(tick < 34, "The first feeding bout should fill charge before its cloud.");
            world.Advance(tick++, 0, 0);
        }
        var away = world.PeakX(tick) < ForagingWorld.Cells / 2 ? 1 : -1;
        while (Math.Abs(world.X - world.PeakX(tick)) < 3) world.Advance(tick++, away, 0);
        while (world.Cycles == 0)
        {
            Assert.True(tick < 80, "Cooling should complete the first feeding cycle.");
            world.Advance(tick++, 0, 0);
        }
        Assert.Equal(1, world.Cycles);
        Assert.True(world.Charge > 80);

        // Jumping the external clock to the source's dark window does not
        // simulate the intervening physics; these are eight further steps.
        if (dark) tick = 92;
        var observedHighCharge = false;
        for (var n = 0; n < (dark ? 8 : 15); n++)
        {
            if (dark) Assert.True(world.IsDark(tick));
            observedHighCharge |= world.Charge >= 80;
            world.Advance(tick++, 0, 0);
            Assert.Equal(1, world.Cycles);
        }
        Assert.True(observedHighCharge);
    }

    [Theory]
    [InlineData(90, 1, -1)]
    [InlineData(10, 0, 1)]
    public void HeatSafetyWinsOverTheConflictingLowEnergyRoute(int heat, int expectedMode, int expectedAction)
    {
        var decision = Decide(energy: 5, heat: heat, present: true, variant: "full", failedFine: false);
        Assert.Equal(expectedMode, decision.Mode);
        Assert.Equal(expectedAction, decision.Action);
        Assert.Contains(expectedMode == 1 ? "heat" : "energy", decision.Used);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnUnusedFineRequestIsNotCreditedAsAnActionDependency(bool failedFine)
    {
        var decision = Decide(energy: 79, heat: 80, present: false, variant: "always_fine", failedFine: failedFine);
        Assert.Equal(2, decision.FineQueries);
        Assert.Equal(0, decision.Action);
        Assert.Contains("Measured absence", decision.Reason, StringComparison.Ordinal);
        Assert.DoesNotContain("precision", decision.Used);
        Assert.DoesNotContain("energy", decision.Used);
        Assert.DoesNotContain("heat", decision.Used);
        Assert.All(decision.Evidence.Where(evidence => evidence.Id is "energy" or "heat"), evidence =>
        {
            Assert.True(evidence.Queried);
            Assert.False(evidence.Used);
            if (failedFine) Assert.Equal("open", evidence.State);
        });
    }

    private static BrainDecision Decide(int energy, int heat, bool present, string variant, bool failedFine)
    {
        var source = new Carrier(); var presenceAxis = new Carrier(); var orderAxis = new Carrier();
        var state = RelativeSignalSurface.Create(new Carrier(), source, presenceAxis, orderAxis);
        // All present fixture values are privately equal. The observer still
        // performs its actual acquisition/comparison/weighted vote path.
        Relation Compare(Relation candidate, Relation anchor) =>
            SamplingSurface.Comparison(new Carrier(), candidate, anchor, orderAxis, 0);
        for (var n = 0; n < 2; n++) state = RelativeSignalSurface.Observe(new Carrier(), state,
            SamplingSurface.Sample(new Carrier(), source, presenceAxis, true), Compare);
        var sample = SamplingSurface.Sample(new Carrier(), source, presenceAxis, present);
        if (!present) state = RelativeSignalSurface.Observe(new Carrier(), state, sample, Compare);
        Assert.True(RelativeSignalSurface.TryReadState(state, out _, out var policy, out _, out _, out var latest));
        Assert.True(RelativeSignalSurface.TryReadPolicy(policy, out _, out _, out _, out var scoreAxis, out _, out _));
        Assert.True(RelativeSignalSurface.TryReadLatest(latest, out _, out var fast, out var slow, out _, out _));
        var sensorOrder = new Carrier(); var bodyOrder = new Carrier();
        var brain = new NativeForager(scoreAxis, presenceAxis, bodyOrder,
            new ForagingWorld(7).Orientation(sensorOrder, bodyOrder), variant);
        var coarseEnergy = CoarseReadSurface.Measure(new Carrier(), brain.EnergyPartition, energy, new Placeholder());
        var coarseHeat = CoarseReadSurface.Measure(new Carrier(), brain.HeatPartition, heat, new Placeholder());
        Relation Fine(Relation capacity, Carrier axis, int value) => failedFine ?
            ReadWitness.Create(new Carrier(), capacity, capacity, new Placeholder(), new Relation(capacity, axis), capacity) :
            FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(axis, value), new Placeholder());
        Relation Spatial()
        {
            // Opaque physical eye values 100 (left) and 101 (right) have the
            // requested order +1. Only that comparison sign is published.
            var left = SamplingSurface.Sample(new Carrier(), source, presenceAxis, true);
            var right = SamplingSurface.Sample(new Carrier(), source, presenceAxis, true);
            return SamplingSurface.Comparison(new Carrier(), right, left, sensorOrder, 1);
        }
        return brain.Decide(sample, fast, slow, coarseEnergy, coarseHeat,
            () => Fine(brain.EnergyCapacity, brain.EnergyAxis, energy),
            () => Fine(brain.HeatCapacity, brain.HeatAxis, heat), Spatial);
    }
}
