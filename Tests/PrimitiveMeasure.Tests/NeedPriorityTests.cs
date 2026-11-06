using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class NeedPriorityTests
{
    [Theory]
    [InlineData(7, 1, 2)]  // Not due: purse before standing.
    [InlineData(7, 2, 8)]  // Due and reachable: pickup.
    [InlineData(3, 2, 2)]  // Due but unreachable: purse.
    [InlineData(7, 3, 10)] // Two possible bases; keep both.
    [InlineData(7, 0, 0)]  // Missing deadline evidence.
    [InlineData(2, 1, 4)]  // Only the standing outlet can act.
    [InlineData(0, 2, 1)]  // Known closed for all three.
    public void PriorityReadsEligibleNeedsAndTheDeadline(byte access, byte due, byte expected)
    {
        var machine = new TensionChoiceMachine([5, 4, 2], [[1, 0, 0], [0, 1, 0], [0, 0, 1]]);
        Assert.Equal(expected, new NeedPriority(machine).Select(access, due));
    }

    [Fact]
    public void AChosenPickupCanIncreaseUnpaidCashAndAnUncertainDeadlineDoesNotSpend()
    {
        var history = NeedPriority.RunExample().Timeline;
        var uncertain = history[3];
        Assert.Equal((byte)10, uncertain.FocusSupport);
        Assert.Equal(-1, uncertain.Action);
        Assert.Equal(uncertain.Before, uncertain.After);
        Assert.Equal(2, history[2].After[2]);
        var pickup = history[4];
        Assert.Equal(2, pickup.Focus);
        Assert.Equal(2, pickup.Action);
        Assert.Equal(pickup.Before[0] + 1, pickup.After[0]);
        Assert.Equal(pickup.Before[2] - 1, pickup.After[2]);
        Assert.Equal(0, history[5].After[2]);
        Assert.Equal(0, history[6].Focus);
        Assert.Equal(-1, history[^1].Action);
    }

    [Fact]
    public void AnUrgentOfferCanHoldTheTurnWhenItsOnlyRememberedEffectIsHarmful()
    {
        var machine = new TensionChoiceMachine([4, 2, 3], [[1, 0, 0], [0, 1, 0], [0, 0, -1]],
            priors: new[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, -1 } });
        var priority = new NeedPriority(machine);
        var urgent = machine.StepWithFocus(priority.Select(7, 2), 7);
        Assert.Equal(2, urgent.Focus);
        Assert.Equal(-1, urgent.Action);
        Assert.Equal(urgent.Before, urgent.After);
        var ordinary = machine.StepWithFocus(priority.Select(7, 1), 7);
        Assert.Equal(0, ordinary.Action);
        Assert.Equal(urgent.After[0] - 1, ordinary.After[0]);
    }
}
