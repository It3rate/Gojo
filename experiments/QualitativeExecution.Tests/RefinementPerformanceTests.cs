using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.RefinementPerformance;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class RefinementPerformanceTests
{
    [Theory]
    [InlineData(2, 5)] [InlineData(7, 9)] [InlineData(16, 16)]
    public void NativeAndCompiledCircuitsRunTheSameStatefulNativeLawProgram(int size, int rounds)
    {
        var native = new QualitativeCircuitWorkload(size, CircuitBackend.NativeWitness);
        var witnessed = new QualitativeCircuitWorkload(size, CircuitBackend.CompiledWitness);
        var batch = new QualitativeCircuitWorkload(size, CircuitBackend.BatchedMasks);
        var expected = native.Run(rounds);
        Assert.Equal(expected, witnessed.Run(rounds)); Assert.Equal(expected, batch.Run(rounds));
        Assert.Equal((long)size * rounds, batch.ExecutedCellRounds);
        Assert.Empty(Assert.IsType<Relation>(batch.Surface).Carriers());
        Assert.Empty(Assert.IsType<Relation>(batch.Surface).CarrierLocations());
        Assert.True(StructuralConstraint.TryReadScope(batch.Snapshot(), out var cells, out var seam, out var tail));
        Assert.Equal(size, cells.Length); Assert.True(seam.Equals(tail));
        Assert.NotNull(native.LatestWitness); Assert.NotNull(witnessed.LatestWitness);
        Assert.Null(batch.LatestWitness); // This lane explicitly omits read witnesses.
        Assert.Equal(native.Run(3), batch.Run(3)); // Feedback persists across calls.
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void BreadthWorkloadKeepsTheSameVisibleAnswersAcrossMeasuredInterpretation(bool measured, bool compiled)
    {
        var fixture = new StructuralCoverageWorkload(3, measured, compiled);
        Assert.Equal(662, fixture.Run(2));
        Assert.NotNull(fixture.LatestWitness);
        if (measured) Assert.NotEmpty(Assert.IsType<Relation>(fixture.Surface).Carriers());
        else Assert.Empty(Assert.IsType<Relation>(fixture.Surface).Carriers());
    }

    [Fact]
    public void NumericCircuitReusesConnectedCarriersAndRepeatsDeterministically()
    {
        var first = new NumericWorkload(4); var second = new NumericWorkload(4);
        Assert.NotEmpty(Assert.IsType<Relation>(first.Surface).Carriers());
        Assert.Equal(first.Run(5), second.Run(5));
        Assert.NotNull(first.LatestWitness);
        Assert.Equal(first.Run(2), second.Run(2));
    }
}
