using Gojo.Core;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class OperationFollowTests
{
    [Fact]
    public void FollowRoutesShareOfEqualityResidualIntoFollower()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);

        var follow = OperationFollow.Step(
            new Carrier(),
            new CarrierLocation(value, 10),
            new CarrierLocation(value, 18),
            residual,
            responseNumerator: 1,
            responseDenominator: 2);

        Assert.True(OperationFollow.TryRead(
            follow,
            out var claim,
            out var responseRoute,
            out var applied,
            out var next,
            out var remaining,
            out var routeResidue));
        Assert.Equal(new Relation(Relation.Point(residual, 2), Relation.Point(value, 1)), responseRoute);
        Assert.Equal(new CarrierLocation(value, 4), Assert.IsType<CarrierLocation>(applied));
        Assert.Equal(new CarrierLocation(value, 14), Assert.IsType<CarrierLocation>(next));
        Assert.Equal(new CarrierLocation(residual, 4), Assert.IsType<CarrierLocation>(remaining));
        Assert.Equal(new CarrierLocation(residual, 0), Assert.IsType<CarrierLocation>(routeResidue));
        Assert.True(OperationClaim.TryRead(claim, out _, out var claimResidual, out _, out _, out _, out _));
        Assert.Equal(new CarrierLocation(residual, 8), Assert.IsType<CarrierLocation>(claimResidual));
    }

    [Fact]
    public void FollowKeepsProjectionResidueVisible()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);

        var follow = OperationFollow.Step(
            new Carrier(),
            new CarrierLocation(value, 0),
            new CarrierLocation(value, 5),
            residual,
            responseNumerator: 1,
            responseDenominator: 2);

        Assert.True(OperationFollow.TryRead(
            follow,
            out _,
            out _,
            out var applied,
            out var next,
            out var remaining,
            out var routeResidue));
        Assert.Equal(new CarrierLocation(value, 2), Assert.IsType<CarrierLocation>(applied));
        Assert.Equal(new CarrierLocation(value, 2), Assert.IsType<CarrierLocation>(next));
        Assert.Equal(new CarrierLocation(residual, 3), Assert.IsType<CarrierLocation>(remaining));
        Assert.Equal(new CarrierLocation(residual, 1), Assert.IsType<CarrierLocation>(routeResidue));
    }

    [Fact]
    public void FollowHandlesNegativeResidualWithoutCreatingInverseFlow()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);

        var follow = OperationFollow.Step(
            new Carrier(),
            new CarrierLocation(value, 10),
            new CarrierLocation(value, 3),
            residual,
            responseNumerator: 1,
            responseDenominator: 2);

        Assert.True(OperationFollow.TryRead(
            follow,
            out _,
            out _,
            out var applied,
            out var next,
            out var remaining,
            out var routeResidue));
        var appliedLocation = Assert.IsType<CarrierLocation>(applied);
        Assert.True(appliedLocation.Carrier.SameIdentityAs(value));
        Assert.False(appliedLocation.Carrier.IsInverse);
        Assert.Equal(-3, appliedLocation.Ticks);
        Assert.Equal(new CarrierLocation(value, 7), Assert.IsType<CarrierLocation>(next));
        Assert.Equal(new CarrierLocation(residual, -4), Assert.IsType<CarrierLocation>(remaining));
        Assert.Equal(new CarrierLocation(residual, -1), Assert.IsType<CarrierLocation>(routeResidue));
    }

    [Fact]
    public void FollowLeavesOpposedFlowResidualUnfolded()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);

        var follow = OperationFollow.Step(
            new Carrier(),
            new CarrierLocation(value.Inverse(), 3),
            new CarrierLocation(value, 5),
            residual,
            responseNumerator: 1,
            responseDenominator: 2);

        Assert.True(OperationFollow.TryRead(
            follow,
            out var claim,
            out _,
            out var applied,
            out var next,
            out var remaining,
            out var routeResidue));
        Assert.Equal(new CarrierLocation(value.Inverse(), 0), Assert.IsType<CarrierLocation>(applied));
        Assert.Equal(new CarrierLocation(value.Inverse(), 3), Assert.IsType<CarrierLocation>(next));

        var fusedRemaining = Assert.IsType<Relation>(remaining);
        Assert.True(fusedRemaining.TryReadFusedCarrierFaces(out var inverseFace, out var positiveFace));
        Assert.True(inverseFace.Carrier.SameIdentityAs(residual.Inverse()));
        Assert.Equal(-3, inverseFace.Ticks);
        Assert.True(positiveFace.Carrier.SameIdentityAs(residual));
        Assert.Equal(5, positiveFace.Ticks);
        Assert.Equal(fusedRemaining, routeResidue);
        Assert.True(OperationClaim.TryRead(claim, out _, out var claimResidual, out _, out _, out _, out _));
        Assert.Equal(fusedRemaining, claimResidual);
    }

    [Fact]
    public void ApplyingFollowFeedsPositiveAdjustmentThroughCarrierTension()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        value.Set(10);
        var follow = OperationFollow.Step(
            new Carrier(),
            value.ValueSlot,
            new CarrierLocation(value, 18),
            residual,
            responseNumerator: 1,
            responseDenominator: 2);

        Assert.True(OperationFollow.TryApply(follow, out var application));

        Assert.Equal(14UL, value.Value);
        Assert.Equal(0, value.Tension);
        Assert.True(OperationFollow.TryReadApplication(
            application,
            out var appliedFollow,
            out var before,
            out var applied,
            out var after));
        Assert.Equal(follow, appliedFollow);
        Assert.Equal(new CarrierLocation(value, 4), applied);
        Assert.True(OperationFollow.TryReadCarrierState(before, out var beforeValue, out var beforeTension));
        Assert.Equal(new CarrierLocation(value, 10), beforeValue);
        Assert.Equal(new CarrierLocation(value, 0), beforeTension);
        Assert.True(OperationFollow.TryReadCarrierState(after, out var afterValue, out var afterTension));
        Assert.Equal(new CarrierLocation(value, 14), afterValue);
        Assert.Equal(new CarrierLocation(value, 0), afterTension);
    }

    [Fact]
    public void ApplyingFollowFeedsNegativeAdjustmentThroughSameCarrierFace()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        value.Set(10);

        Assert.True(OperationFollow.TryStepAndApply(
            new Carrier(),
            value.ValueSlot,
            new CarrierLocation(value, 4),
            residual,
            responseNumerator: 1,
            responseDenominator: 2,
            out var application));

        Assert.Equal(7UL, value.Value);
        Assert.Equal(0, value.Tension);
        Assert.True(OperationFollow.TryReadApplication(application, out _, out _, out var applied, out _));
        Assert.True(applied.Carrier.SameIdentityAs(value));
        Assert.False(applied.Carrier.IsInverse);
        Assert.Equal(-3, applied.Ticks);
    }

    [Fact]
    public void ApplyingFollowCanLeaveUnderflowAsTension()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        value.Set(2);

        Assert.True(OperationFollow.TryStepAndApply(
            new Carrier(),
            value.ValueSlot,
            new CarrierLocation(value, -6),
            residual,
            responseNumerator: 1,
            responseDenominator: 2,
            out var application));

        Assert.Equal(0UL, value.Value);
        Assert.Equal(-2, value.Tension);
        Assert.True(OperationFollow.TryReadApplication(application, out _, out _, out var applied, out var after));
        Assert.Equal(new CarrierLocation(value, -4), applied);
        Assert.True(OperationFollow.TryReadCarrierState(after, out var afterValue, out var afterTension));
        Assert.Equal(new CarrierLocation(value, 0), afterValue);
        Assert.Equal(new CarrierLocation(value, -2), afterTension);
    }

    [Fact]
    public void ApplyingUnfoldedOpposedResidualDoesNotMutatePositiveCarrier()
    {
        var value = new Carrier();
        var inverse = value.Inverse();
        var residual = new Carrier(value.UnitShape);
        value.Set(5);
        inverse.Set(3);
        var follow = OperationFollow.Step(
            new Carrier(),
            inverse.ValueSlot,
            value.ValueSlot,
            residual,
            responseNumerator: 1,
            responseDenominator: 2);

        Assert.True(OperationFollow.TryApply(follow, out var application));

        Assert.Equal(5UL, value.Value);
        Assert.Equal(3UL, inverse.Value);
        Assert.True(OperationFollow.TryReadApplication(application, out _, out var before, out var applied, out var after));
        Assert.True(applied.Carrier.SameIdentityAs(inverse));
        Assert.Equal(0, applied.Ticks);
        Assert.True(OperationFollow.TryReadCarrierState(before, out var beforeValue, out _));
        Assert.True(OperationFollow.TryReadCarrierState(after, out var afterValue, out _));
        Assert.Equal(new CarrierLocation(inverse, 3), beforeValue);
        Assert.Equal(new CarrierLocation(inverse, 3), afterValue);
    }

    [Fact]
    public void SettleRecordsRepeatedFollowApplicationsAsTimeSteps()
    {
        var value = new Carrier();
        var residual = new Carrier(value.UnitShape);
        var timeBasis = new Carrier();
        var clock = TimeStepSequence.CreateClock(timeBasis);
        value.Set(0);

        var sequence = OperationFollow.Settle(
            new Carrier(),
            clock,
            value,
            new CarrierLocation(value, 10),
            residual,
            responseNumerator: 1,
            responseDenominator: 2,
            stepLimit: 10,
            out var steps);

        Assert.Equal(9UL, value.Value);
        Assert.Equal(0, value.Tension);
        Assert.Equal(5, steps.Length);
        Assert.True(TimeStepSequence.TryRead(sequence, out var origin, out var initial, out var readSteps));
        Assert.Equal(new CarrierLocation(clock, 0), origin);
        Assert.Equal(steps, readSteps);
        Assert.True(OperationFollow.TryReadCarrierState((Relation)initial, out var initialValue, out var initialTension));
        Assert.Equal(new CarrierLocation(value, 0), initialValue);
        Assert.Equal(new CarrierLocation(value, 0), initialTension);

        Assert.True(TimeStepSequence.TryReadStep(
            steps[0],
            out _,
            out var firstStart,
            out var firstEnd,
            out var firstBefore,
            out var firstFollow,
            out var firstAfter,
            out var firstResidue));
        Assert.Equal(new CarrierLocation(clock, 0), firstStart);
        Assert.Equal(new CarrierLocation(clock, 1), firstEnd);
        Assert.True(OperationFollow.TryReadCarrierState((Relation)firstBefore, out var beforeValue, out _));
        Assert.Equal(new CarrierLocation(value, 0), beforeValue);
        Assert.True(OperationFollow.TryRead((Relation)firstFollow, out _, out _, out var firstApplied, out _, out var firstRemaining, out _));
        Assert.Equal(new CarrierLocation(value, 5), Assert.IsType<CarrierLocation>(firstApplied));
        Assert.Equal(new CarrierLocation(residual, 5), Assert.IsType<CarrierLocation>(firstRemaining));
        Assert.True(OperationFollow.TryReadCarrierState((Relation)firstAfter, out var afterValue, out _));
        Assert.Equal(new CarrierLocation(value, 5), afterValue);
        var firstResidueElements = OrderedElements((Relation)firstResidue);
        Assert.Equal(new CarrierLocation(value, 5), Assert.IsType<CarrierLocation>(firstResidueElements[0]));
        Assert.Equal(new CarrierLocation(residual, 5), Assert.IsType<CarrierLocation>(firstResidueElements[1]));

        Assert.True(TimeStepSequence.TryReadStep(
            steps[^1],
            out _,
            out var lastStart,
            out var lastEnd,
            out _,
            out _,
            out var lastAfter,
            out var lastResidue));
        Assert.Equal(new CarrierLocation(clock, 4), lastStart);
        Assert.Equal(new CarrierLocation(clock, 5), lastEnd);
        Assert.True(OperationFollow.TryReadCarrierState((Relation)lastAfter, out var lastAfterValue, out _));
        Assert.Equal(new CarrierLocation(value, 9), lastAfterValue);
        var lastResidueElements = OrderedElements((Relation)lastResidue);
        Assert.Equal(new CarrierLocation(value, 0), Assert.IsType<CarrierLocation>(lastResidueElements[0]));
        Assert.Equal(new CarrierLocation(residual, 1), Assert.IsType<CarrierLocation>(lastResidueElements[1]));
        Assert.Equal(new CarrierLocation(residual, 1), Assert.IsType<CarrierLocation>(lastResidueElements[2]));
    }

    private static IElement[] OrderedElements(Relation structure)
    {
        var holder = Assert.IsType<Carrier>(structure.Right);
        Assert.True(structure.TryReadOrderedElementsOn(holder, out var elements));
        return elements;
    }
}
