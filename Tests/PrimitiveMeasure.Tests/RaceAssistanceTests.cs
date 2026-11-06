using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class RaceAssistanceTests
{
    private sealed class Signal(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public byte ReadSupport() => Support;
    }

    [Fact]
    public void OnlyAnUnmetGoalAndARefusedRequestFromARunningEntrantReleaseTheReserve()
    {
        for (var bits = 0; bits < 16; bits++)
        {
            Signal SignalFor(int bit) => new((byte)((bits & (1 << bit)) != 0 ? 2 : 1));
            var device = new RaceAssistance(SignalFor(0), SignalFor(1), SignalFor(2), SignalFor(3));
            var shouldRelease = bits == 0b0110;
            Assert.Equal(shouldRelease, device.Step().Moved);
            Assert.Equal(shouldRelease ? 2 : 1, device.Released.ReadSupport());
            Assert.Equal((bits >= 8 || shouldRelease) ? 2 : 1, device.EffectiveAccess.ReadSupport());
            Assert.False(device.Step().Moved);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void TheDeviceWaitsForGoalEvidenceAndCanUseItWhenItArrives(byte withheld)
    {
        var goal = new Signal(withheld);
        var device = new RaceAssistance(goal, new Signal(2), new Signal(2), new Signal(1));
        var waiting = device.Step();
        Assert.False(waiting.Committed);
        Assert.Equal(1, device.Released.ReadSupport());
        Assert.Equal(1, device.EffectiveAccess.ReadSupport());

        goal.Support = 1;
        Assert.True(device.Step().Moved);
        Assert.Equal(2, device.EffectiveAccess.ReadSupport());
        goal.Support = 2;
        Assert.False(device.Step().Moved);
        Assert.Equal(2, device.Released.ReadSupport());
    }
}
