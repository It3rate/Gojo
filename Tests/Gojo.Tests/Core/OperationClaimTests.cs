using Gojo.Core;
using Gojo.Reads;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class OperationClaimTests
{
    [Fact]
    public void EqualityClaimKeepsNegativeValueOnSameCarrierFace()
    {
        var value = new Carrier();
        var residualCarrier = new Carrier(value.UnitShape);

        var claim = OperationClaim.Equality(
            new Carrier(),
            new CarrierLocation(value, 5),
            new CarrierLocation(value, 3),
            residualCarrier);

        Assert.True(OperationClaim.TryRead(
            claim,
            out var surface,
            out var residual,
            out var violation,
            out var residualMagnitude,
            out var violationMagnitude,
            out _));
        Assert.Equal(new Relation(new CarrierLocation(value, 5), new CarrierLocation(value, 3)), surface);

        var residualLocation = Assert.IsType<CarrierLocation>(residual);
        Assert.True(residualLocation.Carrier.SameIdentityAs(residualCarrier));
        Assert.False(residualLocation.Carrier.IsInverse);
        Assert.Equal(-2, residualLocation.Ticks);
        Assert.Equal(residualLocation, Assert.IsType<CarrierLocation>(violation));
        Assert.Equal(new CarrierLocation(residualCarrier, 2), residualMagnitude);
        Assert.Equal(new CarrierLocation(residualCarrier, 2), violationMagnitude);
    }

    [Fact]
    public void EqualityClaimPreservesOpposedFlowAsFusedResidual()
    {
        var value = new Carrier();
        var inverse = value.Inverse();
        var residualCarrier = new Carrier(value.UnitShape);

        var claim = OperationClaim.Equality(
            new Carrier(),
            new CarrierLocation(inverse, 3),
            new CarrierLocation(value, 5),
            residualCarrier);

        Assert.True(OperationClaim.TryRead(
            claim,
            out _,
            out var residual,
            out var violation,
            out var residualMagnitude,
            out var violationMagnitude,
            out var residue));

        var fused = Assert.IsType<Relation>(residual);
        Assert.True(fused.TryReadFusedCarrierFaces(out var inverseFace, out var positiveFace));
        Assert.True(inverseFace.Carrier.SameIdentityAs(residualCarrier.Inverse()));
        Assert.Equal(-3, inverseFace.Ticks);
        Assert.True(positiveFace.Carrier.SameIdentityAs(residualCarrier));
        Assert.Equal(5, positiveFace.Ticks);
        Assert.Equal(fused, violation);
        Assert.Equal(fused, residue);
        Assert.Equal(new CarrierLocation(residualCarrier, 8), residualMagnitude);
        Assert.Equal(new CarrierLocation(residualCarrier, 8), violationMagnitude);
    }

    [Fact]
    public void OneSidedClaimsExposeOnlyViolatingShare()
    {
        var value = new Carrier();
        var residualCarrier = new Carrier(value.UnitShape);

        var quiet = OperationClaim.AtMost(
            new Carrier(),
            new CarrierLocation(value, 3),
            new CarrierLocation(value, 5),
            residualCarrier);
        var violating = OperationClaim.AtMost(
            new Carrier(),
            new CarrierLocation(value, 9),
            new CarrierLocation(value, 5),
            residualCarrier);

        Assert.True(OperationClaim.TryRead(
            quiet,
            out _,
            out var quietResidual,
            out var quietViolation,
            out var quietResidualMagnitude,
            out var quietViolationMagnitude,
            out _));
        Assert.Equal(new CarrierLocation(residualCarrier, 2), Assert.IsType<CarrierLocation>(quietResidual));
        Assert.Equal(new CarrierLocation(residualCarrier, 0), Assert.IsType<CarrierLocation>(quietViolation));
        Assert.Equal(new CarrierLocation(residualCarrier, 2), quietResidualMagnitude);
        Assert.Equal(new CarrierLocation(residualCarrier, 0), quietViolationMagnitude);

        Assert.True(OperationClaim.TryRead(
            violating,
            out _,
            out var violatingResidual,
            out var violatingViolation,
            out var violatingResidualMagnitude,
            out var violatingViolationMagnitude,
            out _));
        Assert.Equal(new CarrierLocation(residualCarrier, -4), Assert.IsType<CarrierLocation>(violatingResidual));
        Assert.Equal(new CarrierLocation(residualCarrier, 4), Assert.IsType<CarrierLocation>(violatingViolation));
        Assert.Equal(new CarrierLocation(residualCarrier, 4), violatingResidualMagnitude);
        Assert.Equal(new CarrierLocation(residualCarrier, 4), violatingViolationMagnitude);
    }

    [Fact]
    public void OneSidedClaimAcrossOpposedFlowKeepsViolationQuietUntilAFoldIsNamed()
    {
        var value = new Carrier();
        var residualCarrier = new Carrier(value.UnitShape);

        var claim = OperationClaim.AtMost(
            new Carrier(),
            new CarrierLocation(value.Inverse(), 3),
            new CarrierLocation(value, 5),
            residualCarrier);

        Assert.True(OperationClaim.TryRead(
            claim,
            out _,
            out var residual,
            out var violation,
            out var residualMagnitude,
            out var violationMagnitude,
            out var residue));

        var fused = Assert.IsType<Relation>(residual);
        Assert.True(fused.TryReadFusedCarrierFaces(out var inverseFace, out var positiveFace));
        Assert.True(inverseFace.Carrier.SameIdentityAs(residualCarrier.Inverse()));
        Assert.Equal(-3, inverseFace.Ticks);
        Assert.True(positiveFace.Carrier.SameIdentityAs(residualCarrier));
        Assert.Equal(5, positiveFace.Ticks);
        Assert.Equal(fused, residue);
        Assert.Equal(new CarrierLocation(residualCarrier, 8), residualMagnitude);
        Assert.Equal(new CarrierLocation(residualCarrier, 0), Assert.IsType<CarrierLocation>(violation));
        Assert.Equal(new CarrierLocation(residualCarrier, 0), violationMagnitude);
    }

    [Fact]
    public void BetweenClaimPairsLowerAndUpperOneSidedClaims()
    {
        var value = new Carrier();
        var residualCarrier = new Carrier(value.UnitShape);

        var inside = OperationClaim.Between(
            new Carrier(),
            new CarrierLocation(value, 4),
            new CarrierLocation(value, 2),
            new CarrierLocation(value, 6),
            residualCarrier);
        var above = OperationClaim.Between(
            new Carrier(),
            new CarrierLocation(value, 8),
            new CarrierLocation(value, 2),
            new CarrierLocation(value, 6),
            residualCarrier);

        Assert.True(OperationClaim.TryReadBetween(
            inside,
            out var surface,
            out var lowerClaim,
            out var upperClaim,
            out var lowerViolation,
            out var upperViolation,
            out var totalViolation,
            out var residue));
        Assert.True(surface.TryReadOrderedElementsOn(Assert.IsType<Carrier>(surface.Right), out var surfaceElements));
        Assert.Equal(3, surfaceElements.Length);
        Assert.True(OperationClaim.TryRead(lowerClaim, out _, out _, out _, out _, out _, out _));
        Assert.True(OperationClaim.TryRead(upperClaim, out _, out _, out _, out _, out _, out _));
        Assert.Equal(new CarrierLocation(residualCarrier, 0), lowerViolation);
        Assert.Equal(new CarrierLocation(residualCarrier, 0), upperViolation);
        Assert.Equal(new CarrierLocation(residualCarrier, 0), totalViolation);
        Assert.IsType<Relation>(residue);

        Assert.True(OperationClaim.TryReadBetween(
            above,
            out _,
            out _,
            out _,
            out var aboveLowerViolation,
            out var aboveUpperViolation,
            out var aboveTotalViolation,
            out _));
        Assert.Equal(new CarrierLocation(residualCarrier, 0), aboveLowerViolation);
        Assert.Equal(new CarrierLocation(residualCarrier, 2), aboveUpperViolation);
        Assert.Equal(new CarrierLocation(residualCarrier, 2), aboveTotalViolation);
    }
}
