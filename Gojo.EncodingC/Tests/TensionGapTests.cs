using Xunit;

namespace Gojo.EncodingC.Tests;

public sealed class TensionGapTests
{
    private const int Capacity = 3;
    private static readonly Field Count = new(Shape.Unary(Capacity), 0, 1, 2);

    [Theory]
    [InlineData(1, 3, 2, 0)]
    [InlineData(3, 1, 0, 2)]
    [InlineData(2, 2, 0, 0)]
    public void AGoalComparisonReadsItsSamplesAndNarrowsOnlyResultSupports(int have, int want, int deficit, int excess)
    {
        var samples = new Store(Capacity, 2);
        Count.Write(samples, 0, have);
        Count.Write(samples, 1, want);
        var sample = new Binding(samples, [.. Count.Planes]);

        var results = new Store(Capacity, 2);
        var amounts = Supports.On(results, Count);
        var gap = new Wiring(TensionGap.Between(Capacity), sample, sample, amounts.Variable, amounts.Variable);
        gap.Add(0, 1, 0, 1);

        gap.Apply();

        Assert.Equal((deficit, excess), (gap.Value(0, 2), gap.Value(0, 3)));
        Assert.Equal((have, want), (Count.Read(samples, 0), Count.Read(samples, 1)));
        Assert.Equal((0, 0), (Count.Read(results, 0), Count.Read(results, 1)));
    }

    [Fact]
    public void OpenInputLeavesBothDirectionsPossibleAndTheirPairConstrained()
    {
        var source = new Store(Capacity, 1);
        var have = Supports.On(source, Count);
        const ulong before = (1UL << 1) | (1UL << 3);
        have.Restrict(0, before);
        var port = source.Publish([0], [.. have.Planes]);

        var comparison = new Store(Capacity, 3);
        Count.Write(comparison, 0, 2);
        var amounts = Supports.On(comparison, Count);
        var gap = new Wiring(TensionGap.Between(Capacity), Binding.Open(port, 0, 1, 2, 3),
            amounts.Sample, amounts.Variable, amounts.Variable);
        gap.Add(0, 0, 1, 2);

        gap.Apply();

        Assert.Equal((0b0011UL, 0b0011UL), (amounts.Values(1), amounts.Values(2)));

        amounts.Restrict(1, 1UL << 1);
        gap.Apply();

        Assert.Equal((1, 0), (gap.Value(0, 2), gap.Value(0, 3)));
        Assert.Equal(before, have.Values(0));
        Assert.False(port.IsTaken);
    }

    [Fact]
    public void ReopenedGoalOutputsFollowANewSourceSample()
    {
        var source = new Store(Capacity, 3);
        Count.Write(source, 0, 1);
        var observed = Supports.On(source, Count);
        var focus = new Workspace(source).Select(Plane.Of(source.Count, 0));
        var goal = observed.In(focus);
        Count.Write(focus, 0, 3);
        var gap = new Wiring(TensionGap.Between(Capacity), observed.Sample, goal.Sample, goal.Variable, goal.Variable);
        gap.Add(0, 0, 1, 2);

        focus.Apply(gap);

        Assert.Equal((2, 0), (gap.Value(0, 2), gap.Value(0, 3)));

        Count.Write(source, 0, 3);
        goal.Forget(1);
        goal.Forget(2);
        focus.Apply(gap);

        Assert.Equal((0, 0), (gap.Value(0, 2), gap.Value(0, 3)));
        Assert.Equal(Plane.Ones(source.Count), observed.Open);
    }
}
