using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class FamilyAdjustmentTests
{
    [Fact]
    public void OneSharedFieldMovesEveryAppearanceButConsumesOnlyOneRequest()
    {
        var body = Body(shared: true);
        var sourceBytes = FamilyUseCodec.Encode(body.Table, body.Entry, body.Uses);
        var before = body.Predict();
        Assert.Equal(new[] { 1 }, body.Tips());
        Assert.Empty(body.Trace);
        var step = body.Push();
        Assert.Equal(before.AfterSupport, step.ActualScopeSupport);
        Assert.True(step.Moved);
        Assert.False(step.Waiting);
        Assert.False(body.ReservePresent);
        Assert.Equal(new[] { 0 }, body.AffectedFields());
        Assert.Equal(new[] { 0, 1, 2, 3 }, body.AffectedLinks());
        Assert.Equal(new[] { 0, 1 }, body.AffectedGroups());
        Assert.Equal(new[] { 2 }, body.Tips());
        Assert.Equal((byte)2, body.DialPort(0).ReadSupport());
        Assert.Equal(sourceBytes, FamilyUseCodec.Encode(body.Table, body.Entry, body.Uses));
        Assert.All(Enumerable.Range(0, body.Table.Template.FieldCount), field => Assert.Equal((byte)1, body.Table.ReadCoarse(body.Entry, field)));
    }

    [Fact]
    public void EqualLookingIndependentFieldsMoveOnlyTheSelectedSource()
    {
        var body = Body(shared: false);
        Assert.All(Enumerable.Range(0, 4), field => Assert.Equal((byte)1, body.Table.ReadCoarse(body.Entry, field)));
        var step = body.Push();
        Assert.True(step.Moved);
        Assert.Equal(new[] { 2, 1, 1, 1 }, body.Tips());
        Assert.Equal(new[] { 0 }, body.AffectedFields());
        Assert.Equal(new[] { 0 }, body.AffectedLinks());
        Assert.Equal(new[] { 0 }, body.AffectedGroups());
        Assert.NotSame(body.Rail(0), body.Rail(1));
        Assert.Equal(LandmarkRead.Before, body.Rail(0).Compare(step.BeforeTips[0], step.AfterTips[0]));
        Assert.Equal(LandmarkRead.SameSite, body.Rail(1).Compare(step.BeforeTips[1], step.AfterTips[1]));
    }

    [Fact]
    public void TheExplicitPinHoldsTheRequestWithoutChangingAnyFamilyField()
    {
        var body = Body(shared: true, pins: [0]);
        var predicted = body.Predict();
        var pin = Assert.Single(body.PinRows());
        var binding = Assert.Single(body.FieldRows());
        var route = Assert.Single(body.SettleRows());
        Assert.Equal(binding[1], pin[1]);
        Assert.Equal(pin[2], route[1]);
        var step = body.Push();
        Assert.True(step.OfferCommitted);
        Assert.True(step.SettlementCommitted); // A resolved hold, not a handoff.
        Assert.False(step.Moved);
        Assert.True(body.Waiting);
        Assert.False(body.ReservePresent);
        Assert.Equal(predicted.AfterSupport, step.ActualScopeSupport);
        Assert.Empty(body.AffectedFields());
        Assert.Equal((byte)2, body.WaitingPort.ReadSupport());
    }

    [Fact]
    public void OpenResponsePreservesWaitingAndDoesNotInventAFieldOrRoute()
    {
        var body = Body(shared: false, selected: null);
        Assert.Equal(4, body.FieldCount);
        Assert.False(body.HasResponse);
        Assert.Empty(body.SettleRows());
        Assert.Empty(body.SettleBytes());
        var prediction = body.Predict();
        Assert.True(prediction.OpenResponse);
        Assert.Equal(3, prediction.ScopePorts.Length);
        var step = body.Push();
        Assert.Null(step.SettleBeforeSupport);
        Assert.Null(step.SettleAfterState);
        Assert.False(step.SettlementCommitted);
        Assert.True(step.Waiting);
        Assert.False(step.Moved);
        Assert.Equal(prediction.AfterSupport, step.ActualScopeSupport);
    }

    [Fact]
    public void ConcealedControlDoesNotChooseItsPrivateValueAndCanLaterRetryTheSameRequest()
    {
        var free = Body(shared: true);
        var pinned = Body(shared: true, pins: [0]);
        var freePrediction = free.Predict(concealControl: true);
        var pinPrediction = pinned.Predict(concealControl: true);
        Assert.Equal(freePrediction.BeforeSupport, pinPrediction.BeforeSupport);
        Assert.Equal(freePrediction.AfterSupport, pinPrediction.AfterSupport);
        Assert.Equal(freePrediction.SettlementSupport, pinPrediction.SettlementSupport);
        Assert.NotEqual(freePrediction.AfterSupport, freePrediction.SettlementSupport);
        Assert.Equal(2, System.Numerics.BitOperations.PopCount(freePrediction.AfterSupport));
        Assert.Equal(2, System.Numerics.BitOperations.PopCount(freePrediction.SettlementSupport));
        var freeStep = free.Push(concealControl: true);
        var pinStep = pinned.Push(concealControl: true);
        Assert.Equal(freeStep.SettleBeforeSupport, pinStep.SettleBeforeSupport);
        Assert.Null(freeStep.SettleAfterState);
        Assert.Null(pinStep.SettleAfterState);
        Assert.False(freeStep.SettlementCommitted);
        Assert.False(pinStep.SettlementCommitted);
        Assert.True(free.Waiting);
        Assert.True(pinned.Waiting);
        Assert.Empty(free.AffectedFields());
        Assert.Empty(pinned.AffectedFields());
        Assert.Equal(freeStep.ActualScopeSupport, pinStep.ActualScopeSupport);
        Assert.Equal(freePrediction.AfterSupport, freeStep.ActualScopeSupport);
        Assert.Equal(pinPrediction.AfterSupport, pinStep.ActualScopeSupport);

        var freeResolved = free.Push(); var pinResolved = pinned.Push();
        Assert.True(freeResolved.Moved);
        Assert.False(pinResolved.Moved);
        Assert.False(free.Waiting);
        Assert.True(pinned.Waiting);
        Assert.False(free.ReservePresent);
        Assert.False(pinned.ReservePresent);
        Assert.Equal(freeResolved.ActualScopeSupport, freeResolved.Prediction.AfterSupport);
        Assert.Equal(pinResolved.ActualScopeSupport, pinResolved.Prediction.AfterSupport);
    }

    [Fact]
    public void PolicyPredictionsContainActualReadsForFreePinnedOpenAndConcealedTrials()
    {
        foreach (var pinned in new[] { false, true })
            foreach (var selected in new int?[] { 0, null })
                foreach (var concealed in new[] { false, true })
                {
                    var body = Body(false, selected, pinned ? [0] : []);
                    foreach (var concealNow in new[] { concealed, false, true })
                    {
                        var step = body.Push(concealNow);
                        Assert.NotEqual(0U, step.ActualScopeSupport);
                        Assert.Equal(0U, step.ActualScopeSupport & ~step.Prediction.AfterSupport);
                        if (!concealNow || !body.HasResponse)
                            Assert.Equal(step.Prediction.AfterSupport, step.Prediction.SettlementSupport);
                    }
                }
    }

    [Fact]
    public void ReturnedWitnessArraysCannotRewriteTheCapturedTrialOrLiveState()
    {
        var body = Body(true);
        var step = body.Push();
        var originalScope = step.Prediction.ScopePorts.ToArray();
        step.Prediction.ScopePorts[0] = 999;
        step.BeforeTips[0] = 999;
        step.AfterTips[0] = 999;
        var captured = Assert.Single(body.Trace);
        Assert.Equal(originalScope, captured.Prediction.ScopePorts);
        Assert.Equal(new[] { 1 }, captured.BeforeTips);
        Assert.Equal(new[] { 2 }, captured.AfterTips);
        captured.AfterTips[0] = 999;
        captured.Prediction.ScopePorts[0] = 999;
        var reread = Assert.Single(body.Trace);
        Assert.Equal(originalScope, reread.Prediction.ScopePorts);
        Assert.Equal(new[] { 2 }, reread.AfterTips);
        Assert.Equal(new[] { 2 }, body.Tips());
        Assert.Equal(originalScope, body.ScopePorts);
    }

    [Fact]
    public void RepeatedPushesDoNotReplenishReserveOrDuplicateTheRequest()
    {
        foreach (var body in new[] { Body(true), Body(false), Body(true, pins: [0]), Body(false, selected: null) })
        {
            body.Push();
            var settled = body.StoreBytes();
            var tips = body.Tips();
            for (var i = 0; i < 4; i++)
            {
                var step = body.Push();
                Assert.False(step.Moved);
                Assert.False(body.ReservePresent);
                Assert.Equal(settled, body.StoreBytes());
                Assert.Equal(tips, body.Tips());
            }
            Assert.Equal(5, body.Trace.Count);
        }
    }

    [Fact]
    public void ReversingUsesChangesNoAdjustmentSharingOrOwnedDirection()
    {
        var original = Body(true);
        var template = original.Table.Template;
        var reverse = new FamilyUseTable(template, [3, 1], [7, 5]);
        var body = new FamilyAdjustment(original.Table, original.Entry, reverse, 0);
        body.Push();
        Assert.Equal(new[] { 0, 1, 2, 3 }, body.AffectedLinks());
        Assert.Equal(new[] { 0, 1 }, body.AffectedGroups());
        Assert.Equal((byte)1, body.Table.ReadCoarse(body.Entry, 0));
        Assert.Equal(new byte[] { 1 }, body.Table.PackedBytes(1));
    }

    [Fact]
    public void InvalidBindingsAndUnsupportedSourceStatesAreRejectedBeforeARequestExists()
    {
        var source = Body(true);
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyAdjustment(source.Table, source.Entry, source.Uses, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyAdjustment(source.Table, source.Entry, source.Uses, 0, [1]));
        var other = Body(true);
        Assert.Throws<ArgumentException>(() => new FamilyAdjustment(source.Table, source.Entry, other.Uses, 0));
        var zero = source.Table.Append(1, 0);
        Assert.Throws<ArgumentException>(() => new FamilyAdjustment(source.Table, zero, source.Uses, 0));
    }

    private static FamilyAdjustment Body(bool shared, int? selected = 0, int[]? pins = null)
    {
        var fields = Enumerable.Repeat(new FamilyField(0, 0), shared ? 1 : 4).ToArray();
        var links = Enumerable.Range(0, 4).Select(i => new FamilyLink(i, (i + 1) % 4, shared ? 0 : i)).ToArray();
        var table = new FamilyTable(new(fields, links));
        var entry = table.Append(1, Enumerable.Repeat(1UL, fields.Length).ToArray());
        return new(table, entry, new(table.Template, [0, 2], [4, 6]), selected, pins);
    }
}
