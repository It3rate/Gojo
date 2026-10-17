using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Tests.Patterns;

public sealed class RepeatControllerTests
{
    [Fact]
    public void WrapControllerLeavesResidueAndCrossingTension()
    {
        var phase = new Carrier();
        RepeatController.AttachWrap(phase, 4);

        phase.Add(11);

        Assert.Equal(3UL, phase.Value);
        Assert.Equal(8, phase.Tension);
    }

    [Fact]
    public void CycleCounterRoutesCrossedWraps()
    {
        var phase = new Carrier();
        var cycles = new Carrier();
        RepeatController.AttachCycleCounter(phase, 4, cycles);

        phase.Add(11);

        Assert.Equal(3UL, phase.Value);
        Assert.Equal(0, phase.Tension);
        Assert.Equal(2UL, cycles.Value);
    }

    [Fact]
    public void ClampControllerLeavesRejectedTension()
    {
        var phase = new Carrier();
        RepeatController.AttachClamp(phase, 4);

        phase.Add(11);

        Assert.Equal(4UL, phase.Value);
        Assert.Equal(7, phase.Tension);
    }

    [Fact]
    public void WrappedPhaseSelectsRepeatedFrame()
    {
        var slot = new Placeholder();
        var first = new Carrier();
        var second = new Carrier();
        var third = new Carrier();
        var frames = StructuralPattern.Repeat(
            slot,
            [StructuralPattern.Bind(slot, first)],
            [StructuralPattern.Bind(slot, second)],
            [StructuralPattern.Bind(slot, third)]);
        var phase = new Carrier();
        RepeatController.AttachWrap(phase, frames.Length);

        phase.Add(5);

        Assert.True(RepeatController.TryReadFrame(frames, phase, out var frame));
        Assert.Same(third, frame);
    }

    [Fact]
    public void PingPongPhaseReadsReflectedPosition()
    {
        var phase = new Carrier();
        RepeatController.AttachPingPongPhase(phase, 4);

        phase.Add(6);

        Assert.Equal(6UL, phase.Value);
        Assert.Equal(0, phase.Tension);
        Assert.Equal(2UL, RepeatController.PingPongValue(phase, 4));
    }

    [Fact]
    public void EmptyFrameListDoesNotSelectAFrame()
    {
        var phase = new Carrier();

        Assert.False(RepeatController.TryReadFrame([], phase, out _));
    }
}
