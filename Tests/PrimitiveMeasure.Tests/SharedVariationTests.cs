using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class SharedVariationTests
{
    [Fact]
    public void EqualCoarseParticipationDoesNotCreateSharedReach()
    {
        var shared = SharedVariation.Reaches(true);
        var detached = SharedVariation.Reaches(false);
        Assert.Equal(shared.Reads().Select(r => (r.From, r.To, r.Tip, r.CoarseWord)),
            detached.Reads().Select(r => (r.From, r.To, r.Tip, r.CoarseWord)));
        Assert.True(shared.SameDirectedSource(0, 3));
        Assert.False(detached.SharesSource(0, 3));
        Assert.True(shared.Uses.IsClosed(shared.Table, shared.Entry, 0));
        Assert.True(detached.Uses.IsClosed(detached.Table, detached.Entry, 0));
        Assert.Equal(1, shared.Table.Template.FieldCount);
        Assert.Equal(4, detached.Table.Template.FieldCount);
    }

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 1)]
    public void OneRequestChangesOnlyTheUsesOfItsActualSource(bool shared, int changed)
    {
        var trial = SharedVariation.Reaches(shared);
        var source = trial.SourceBytes();
        var expected = trial.Predict();
        var step = trial.Push();
        Assert.True(step.Moved);
        Assert.Equal(changed, trial.ChangedUses().Length);
        Assert.Equal(0U, step.ActualScopeSupport & ~expected.AfterSupport);
        Assert.Equal(1, RequestCount(trial));
        Assert.Equal(source, trial.SourceBytes());
        Assert.All(trial.Reads(), r => Assert.Equal((byte)1, r.CoarseWord));
        var after = trial.StoreBytes();
        Assert.False(trial.Push().Moved);
        Assert.Equal(after, trial.StoreBytes());
        Assert.Equal(1, RequestCount(trial));
    }

    [Fact]
    public void OpposedUsesShareOneLinkButDetachedLookalikesDoNot()
    {
        var shared = SharedVariation.OpposedReads(true);
        var detached = SharedVariation.OpposedReads(false);
        Assert.Equal(shared.Reads().Select(r => (r.From, r.To, r.Reversed, r.Tip, r.CoarseWord)),
            detached.Reads().Select(r => (r.From, r.To, r.Reversed, r.Tip, r.CoarseWord)));
        Assert.Equal(shared.Read(0).Link, shared.Read(1).Link);
        Assert.NotEqual(detached.Read(0).Link, detached.Read(1).Link);
        Assert.True(shared.OpposedSource(0, 1));
        Assert.False(shared.SameDirectedSource(0, 1));
        Assert.False(detached.OpposedSource(0, 1));
        Assert.True(shared.Push().Moved);
        Assert.True(detached.Push().Moved);
        Assert.Equal(new[] { 2, 2 }, shared.Reads().Select(r => r.Tip));
        Assert.Equal(new[] { 2, 1 }, detached.Reads().Select(r => r.Tip));
        Assert.Equal(1, RequestCount(shared));
        Assert.Equal(1, RequestCount(detached));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReversingAUseChangesItsReadWithoutChangingOdvOrSource(bool shared)
    {
        var trial = SharedVariation.OpposedReads(shared);
        var bytes = trial.SourceBytes();
        var reversedUses = new FamilyUseTable(trial.Table.Template,
            Enumerable.Range(0, trial.UseCount).Select(i => trial.Uses.SignedUse(i) ^ 1).ToArray());
        var once = new SharedVariation(trial.Table, trial.Entry, reversedUses);
        for (var i = 0; i < trial.UseCount; i++)
        {
            var before = trial.Read(i);
            var reverse = trial.Read(i, true);
            Assert.Equal((before.From, before.To), (reverse.To, reverse.From));
            Assert.NotEqual(before.Reversed, reverse.Reversed);
            Assert.Equal(before.Field, reverse.Field);
            Assert.Equal(before.Tip, reverse.Tip);
            Assert.Equal(before.CoarseWord, reverse.CoarseWord);
            Assert.Equal(reverse, once.Read(i));
            Assert.Equal(before, once.Read(i, true));
        }
        Assert.Equal(bytes, trial.SourceBytes());
        Assert.Empty(trial.Adjustment.Trace);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void ConcealedResponseWaitsThenReusesTheSameRequest(bool shared, bool opposed)
    {
        var trial = opposed ? SharedVariation.OpposedReads(shared) : SharedVariation.Reaches(shared);
        var predicted = trial.Predict(true);
        var step = trial.Push(true);
        Assert.False(step.Moved);
        Assert.True(step.Waiting);
        Assert.NotEqual(predicted.AfterSupport, predicted.SettlementSupport);
        Assert.Equal(0U, step.ActualScopeSupport & ~predicted.AfterSupport);
        Assert.Equal(1, RequestCount(trial));
        Assert.Empty(trial.ChangedUses());
        Assert.True(trial.Push().Moved);
        Assert.Equal(1, RequestCount(trial));
        Assert.False(trial.Adjustment.ReservePresent);
        Assert.False(trial.Adjustment.Waiting);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnExplicitPinPreservesTheRequestWithoutPretendingToAdjust(bool opposed)
    {
        var trial = opposed ? SharedVariation.OpposedReads(true, true) : SharedVariation.Reaches(true, pinned: true);
        var expected = trial.Predict();
        var step = trial.Push();
        Assert.False(step.Moved);
        Assert.True(trial.Adjustment.Waiting);
        Assert.Equal(0U, step.ActualScopeSupport & ~expected.AfterSupport);
        Assert.Empty(trial.ChangedUses());
        Assert.Equal(1, RequestCount(trial));
        Assert.All(trial.Reads(), r => Assert.Equal(1, r.Tip));
    }

    [Fact]
    public void AnOpenResponseDoesNotInventAFieldOrConsumeTheWaitingRequest()
    {
        var source = SharedVariation.Reaches(true);
        var open = new SharedVariation(source.Table, source.Entry, source.Uses, null);
        Assert.True(open.Predict().OpenResponse);
        Assert.False(open.Push().Moved);
        Assert.True(open.Adjustment.Waiting);
        Assert.Empty(open.ChangedUses());
        Assert.Equal(1, RequestCount(open));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void RetainedCorrespondenceSurvivesIndependentDecoding(bool shared, bool opposed)
    {
        var first = opposed ? SharedVariation.OpposedReads(shared) : SharedVariation.Reaches(shared);
        var encoded = first.SourceBytes();
        var decoded = FamilyUseCodec.Decode(encoded);
        var second = new SharedVariation(decoded.Table, decoded.Entry, decoded.Uses);
        Assert.NotSame(first.Table, second.Table);
        Assert.NotSame(first.Table.Template, second.Table.Template);
        Assert.Equal(encoded, second.SourceBytes());
        Assert.Equal(first.BindingRows(), second.BindingRows());
        Assert.Equal(first.BindingBytes(), second.BindingBytes());
        Assert.Equal(first.Reads(), second.Reads());
        first.Push(); second.Push();
        Assert.Equal(first.Reads(), second.Reads());
        Assert.Equal(first.StoreBytes(), second.StoreBytes());
        Assert.Equal(first.SharesSource(0, 1), second.SharesSource(0, 1));
    }

    [Fact]
    public void RepetitionCountIsInspectableAndNotAClaimOfCalibratedEquality()
    {
        for (var count = 1; count <= 8; count++)
        {
            var same = SharedVariation.Reaches(true, count);
            var separate = SharedVariation.Reaches(false, count);
            Assert.Equal(count, same.UseCount);
            Assert.Equal(count, separate.UseCount);
            same.Push(); separate.Push();
            Assert.Equal(count, same.ChangedUses().Length);
            Assert.Single(separate.ChangedUses());
            Assert.Equal(1, RequestCount(same));
            Assert.Equal(1, RequestCount(separate));
        }
    }

    [Fact]
    public void ConstructorsRejectBrokenOwnershipAndUnobservableInput()
    {
        var source = SharedVariation.Reaches(true);
        var other = SharedVariation.Reaches(true);
        Assert.Throws<ArgumentException>(() => new SharedVariation(source.Table, source.Entry, other.Uses));
        Assert.Throws<ArgumentException>(() => new SharedVariation(source.Table, source.Entry,
            new FamilyUseTable(source.Table.Template, [])));
        Assert.Throws<ArgumentOutOfRangeException>(() => SharedVariation.Reaches(true, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SharedVariation.Reaches(true, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Read(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Read(source.UseCount));
    }

    private static int RequestCount(SharedVariation trial) =>
        (trial.Adjustment.ReservePresent ? 1 : 0) + (trial.Adjustment.Waiting ? 1 : 0)
        + trial.Adjustment.AffectedFields().Length;
}
