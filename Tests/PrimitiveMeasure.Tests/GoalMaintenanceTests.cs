using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class GoalMaintenanceTests
{
    private sealed class Signal(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public byte ReadSupport() => Support;
    }

    [Fact]
    public void OneStandingTargetCanRepairTwoVacanciesThenLeavesTheThirdUnmet()
    {
        var target = new Signal(1);
        var control = new GoalMaintenance(target);
        control.Settle();
        Assert.Equal(0b00011, control.Read().Material);
        target.Support = 2;
        Assert.Equal(new MaintenanceRead(2, 2, 0b00011), control.Read());
        control.Settle();
        control.Settle();
        Assert.Equal(0b00110, control.Read().Material);
        control.TakeOne();
        control.Settle();
        Assert.Equal(0b01100, control.Read().Material);
        control.TakeOne();
        control.Settle();
        Assert.Equal(new MaintenanceRead(2, 2, 0b11000), control.Read());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void WithdrawalOrConcealmentCannotSpendTheSecondPacket(byte next)
    {
        var target = new Signal(2);
        var control = new GoalMaintenance(target);
        control.Settle();
        control.TakeOne();
        target.Support = next;
        var before = control.Read();
        control.Settle();
        Assert.Equal(before.Material, control.Read().Material);
        Assert.Equal(next, control.Read().Gap);
        target.Support = 2;
        control.Settle();
        Assert.Equal(0b01100, control.Read().Material);
    }
}
