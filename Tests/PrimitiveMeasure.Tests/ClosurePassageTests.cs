using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ClosurePassageTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void GateObservationDoesNotChangeTopologyOrDuplicateActivation(byte support)
    {
        for (var count = 2; count <= 6; count++)
        for (var controlled = 0; controlled < count; controlled++)
        {
            var run = new ClosurePassage(count, controlled);
            var source = run.SourceBytes();
            var initial = run.Walk.ValueBytes();
            run.ObserveControl(support);
            Assert.Equal(initial, run.Walk.ValueBytes());
            for (var beat = 0; beat < 2 * count + 2; beat++)
            {
                var before = run.Walk.ValueBytes();
                var step = run.Pulse();
                Assert.True(run.Closed);
                Assert.Equal(source, run.SourceBytes());
                Assert.Equal(1, run.Walk.OccupiedStateCount);
                if (!step.Committed) Assert.Equal(before, run.Walk.ValueBytes());
            }
            if (support != 2) Assert.Equal(controlled, run.Walk.ActiveFamilySite);
            else Assert.All(run.Walk.Trace, step => Assert.True(step.Step.Moved));
        }
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(3)]
    public void RevealingAnOpenGateContinuesTheSameWaitingRequest(byte support)
    {
        var run = new ClosurePassage();
        run.ObserveControl(support);
        for (var i = 0; i < 7; i++) run.Pulse();
        Assert.Equal(3, run.Walk.ActiveFamilySite);
        var bytes = run.Walk.ValueBytes();
        run.ObserveControl(2);
        Assert.Equal(bytes, run.Walk.ValueBytes());
        Assert.True(run.Pulse().Moved);
        Assert.Equal(0, run.Walk.ActiveFamilySite);
        Assert.Equal(1, run.Walk.OccupiedStateCount);
        Assert.True(run.Closed);
    }

    [Fact]
    public void OutOfScopeInputsFailBeforeCreatingAFalseCircuit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClosurePassage(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClosurePassage(17));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClosurePassage(4, 4));
        var run = new ClosurePassage();
        var before = run.Walk.ValueBytes();
        Assert.Throws<ArgumentOutOfRangeException>(() => run.ObserveControl(4));
        Assert.Equal(before, run.Walk.ValueBytes());
    }
}
