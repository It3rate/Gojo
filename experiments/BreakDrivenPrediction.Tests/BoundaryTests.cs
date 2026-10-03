using Gojo.Core;
using Gojo.Equations;
using C = BreakDrivenPrediction.Continuation;

namespace BreakDrivenPrediction.Tests;

public sealed class BoundaryTests
{
    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(8)]
    public void AnUnalignedIssuedComponentCannotBecomeFailedPredictionEvidence(int slot)
    {
        var root = C.Create(50);
        var fields = C.Parts(C.Issue(root));
        fields[slot] = new CarrierLocation(new Carrier(), 1);
        var issue = C.G(fields);
        var before = GojoNumericFormat.Encode(root);
        Assert.False(C.TryObserve(root, issue, new CarrierLocation(C.Sensor(root), 49), out var next, out var read));
        Assert.Same(root, next);
        Assert.False(Equation.TryReadPrediction(read, out _, out _));
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Fact]
    public void UnrepresentableContinuationIsOpenRatherThanSaturated()
    {
        var root = C.Create(long.MaxValue);
        var before = GojoNumericFormat.Encode(root);
        var issue = C.Issue(root);
        Assert.Empty(C.Parts(issue));
        Assert.False(Equation.TryReadPrediction(issue, out _, out _));
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Fact]
    public void UnrepresentableDifferenceCannotPartiallyAdmitTheObservation()
    {
        var root = C.Create(long.MinValue);
        var issue = C.Issue(root);
        var before = GojoNumericFormat.Encode(root);
        Assert.False(C.TryObserve(root, issue, new CarrierLocation(C.Sensor(root), long.MaxValue),
            out var next, out var read));
        Assert.Same(root, next);
        Assert.False(Equation.TryReadPrediction(read, out _, out _));
        Assert.Equal(before, GojoNumericFormat.Encode(root));
    }

    [Fact]
    public void ASharedPrefixCannotKnowWhichFutureFollows()
    {
        var root = C.Create(50);
        for (var i = 0; i < 18; i++)
        {
            var issue = C.Issue(root);
            var level = C.Level(C.Sensor(root)).Ticks + (i % 8 < 3 ? 2 : -2);
            Assert.True(C.TryObserve(root, issue, new CarrierLocation(C.Sensor(root), level), out root, out _));
        }
        var pending = C.Issue(root);
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(root, pending));
        var other = (Relation)restored[0];
        var otherPending = (Relation)restored[1];
        var prediction = ((CarrierLocation)C.Parts(pending)[6]).Ticks;
        Assert.Equal(prediction, ((CarrierLocation)C.Parts(otherPending)[6]).Ticks);
        var levelBefore = C.Level(C.Sensor(root)).Ticks;
        Assert.True(C.TryObserve(root, pending, new CarrierLocation(C.Sensor(root), levelBefore + 2), out _, out var up));
        Assert.True(C.TryObserve(other, otherPending, new CarrierLocation(C.Sensor(other), levelBefore - 2), out _, out var down));
        Assert.NotEqual(C.Pass((Relation)C.Parts(up)[6]), C.Pass((Relation)C.Parts(down)[6]));
        Assert.Equal(prediction, ((CarrierLocation)C.Parts(pending)[6]).Ticks);
        Assert.Equal(prediction, ((CarrierLocation)C.Parts(otherPending)[6]).Ticks);
    }
}
