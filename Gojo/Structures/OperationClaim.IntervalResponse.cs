using Gojo.Core;
using Gojo.Equations;

namespace Gojo.Structures;

public static partial class OperationClaim
{
    /// <summary>
    /// Reads a paired interval claim only when both sides resolve on the same
    /// concrete value face and the authored lower bound does not exceed upper.
    /// An open, reversed or stale interval has no scalar demand, including zero.
    /// Retargeted live sites require a new claim read; cached residuals are not
    /// silently promoted to a current answer.
    /// </summary>
    public static bool TryReadResolvedBetweenViolation(Relation claim, out CarrierLocation magnitude)
    {
        magnitude = default;
        if (!TryReadBetween(claim, out var surface, out var lowerClaim, out var upperClaim,
                out _, out _, out var total, out _) ||
            !TryReadRootElements(surface, out var operands) || operands.Length != 3 ||
            !TryReadFaceLocation(operands[0], out var value) || value.Carrier.IsUnknown ||
            !TryReadFaceLocation(operands[1], out var lower) ||
            !TryReadFaceLocation(operands[2], out var upper) ||
            !value.Carrier.SameIdentityAs(lower.Carrier) || !value.Carrier.SameIdentityAs(upper.Carrier) ||
            lower.Ticks > upper.Ticks ||
            !TryRead(lowerClaim, out var lowerSurface, out var lowerResidual, out var lowerSignedViolation,
                out var lowerResidualMagnitude, out _, out _) ||
            !TryRead(upperClaim, out var upperSurface, out var upperResidual, out var upperSignedViolation,
                out var upperResidualMagnitude, out _, out _) ||
            !SameSample(lowerSurface.Left, value) || !SameSample(lowerSurface.Right, lower) ||
            !SameSample(upperSurface.Left, value) || !SameSample(upperSurface.Right, upper) ||
            !TryReadResolvedViolation(lowerClaim, out var lowerViolation) ||
            !TryReadResolvedViolation(upperClaim, out var upperViolation) ||
            total.Carrier.IsUnknown || !lowerViolation.Carrier.SameIdentityAs(total.Carrier) ||
            !upperViolation.Carrier.SameIdentityAs(total.Carrier) ||
            (Int128)lowerViolation.Ticks + upperViolation.Ticks != total.Ticks ||
            !MatchesCurrentIntervalSide(lowerResidual, lowerSignedViolation, lowerResidualMagnitude, lowerViolation,
                (Int128)lower.Ticks - value.Ticks, lowerSide: true) ||
            !MatchesCurrentIntervalSide(upperResidual, upperSignedViolation, upperResidualMagnitude, upperViolation,
                (Int128)upper.Ticks - value.Ticks, lowerSide: false))
            return false;

        magnitude = total;
        return true;
    }

    /// <summary>
    /// Samples before and after against the SAME ordered permitted interval.
    /// Shape: ReadWitness(group(before,after), captured interval,
    /// group(before claim,after claim,ordered-bounds claim,reduction read),
    /// unresolved residue, group(selected transition,difference equation)).
    /// Positive reduction helps this obligation, negative reduction hurts it.
    /// Resolved zero before-demand means no current demand; it does not mean an
    /// unsuccessful opportunity. This read supplies no eligibility or fade law.
    ///
    /// Input and bound coordinates are copied: retargeting the reusable interval
    /// changes a subsequent read, not the earlier sampled obligation. The optional
    /// transition is retained as supplied evidence, not recursively frozen or
    /// executed. Before/after agreement alone does not prove causal contribution.
    /// No carrier state or tension changes during this observation.
    /// </summary>
    public static Relation IntervalResponse(
        Carrier root,
        Relation allowedInterval,
        IElement before,
        IElement after,
        Carrier residual,
        IElement? transition = null)
    {
        var output = residual ?? Carrier.Unknown;
        var beforeSample = CaptureClaimSample(before);
        var afterSample = CaptureClaimSample(after);
        var interval = new Relation(CaptureClaimSample(allowedInterval.Left), CaptureClaimSample(allowedInterval.Right));
        var selected = Relation.OrderedGroup(new Carrier(), beforeSample, afterSample);
        var beforeBasis = new Carrier(output.UnitShape);
        var afterBasis = new Carrier(output.UnitShape);
        var beforeClaim = Between(new Carrier(), beforeSample, interval.Left, interval.Right, beforeBasis);
        var afterClaim = Between(new Carrier(), afterSample, interval.Left, interval.Right, afterBasis);
        var bounds = AtMost(new Carrier(), interval.Left, interval.Right, output);
        var equation = Equation.Difference(afterBasis, beforeBasis, output);
        var premise = Relation.OrderedGroup(new Carrier(), beforeClaim, afterClaim, bounds);
        Relation reduction;
        IElement residue;
        if (!output.IsUnknown &&
            TryReadResolvedViolation(bounds, out var boundsViolation) && boundsViolation.Ticks == 0 &&
            TryReadResolvedBetweenViolation(beforeClaim, out var beforeDemand) &&
            TryReadResolvedBetweenViolation(afterClaim, out var afterDemand) &&
            Equation.TryPredict(equation, new Relation(afterDemand, beforeDemand), output, out reduction) &&
            Equation.TryReadPrediction(reduction, out _, out var remainder))
        {
            residue = remainder;
        }
        else
        {
            residue = premise;
            reduction = Relation.UnresolvedRead(selected, interval, output);
        }
        return ReadWitness.Create(root, selected, interval,
            Relation.OrderedGroup(new Carrier(), beforeClaim, afterClaim, bounds, reduction), residue,
            Relation.OrderedGroup(new Carrier(), transition ?? new Placeholder(), equation));
    }

    /// <summary>
    /// Reads fully qualified demand and its signed reduction. False leaves the
    /// witness inspectable and never promotes an open comparison to zero utility.
    /// </summary>
    public static bool TryReadIntervalResponse(
        Relation witness,
        out Relation capturedInterval,
        out Relation beforeClaim,
        out Relation afterClaim,
        out CarrierLocation beforeDemand,
        out CarrierLocation afterDemand,
        out CarrierLocation reduction)
    {
        capturedInterval = beforeClaim = afterClaim = null!;
        beforeDemand = afterDemand = reduction = default;
        if (!ReadWitness.TryRead(witness, out var selected, out var surface, out var result, out var witnessResidue, out _) ||
            surface is not Relation interval || !TimeStepSequence.TryReadSpan(interval, out var lower, out var upper) ||
            lower.Carrier.IsUnknown || lower.Ticks > upper.Ticks ||
            selected is not Relation samples || !TryReadRootElements(samples, out var sampleFields) || sampleFields.Length != 2 ||
            result is not Relation results || !TryReadRootElements(results, out var fields) || fields.Length != 4 ||
            fields[0] is not Relation first || fields[1] is not Relation second ||
            fields[2] is not Relation bounds || fields[3] is not Relation difference ||
            !MatchesInterval(first, sampleFields[0], lower, upper) || !MatchesInterval(second, sampleFields[1], lower, upper) ||
            !TryRead(bounds, out var boundSurface, out _, out _, out _, out _, out _) ||
            !SameSample(boundSurface.Left, lower) || !SameSample(boundSurface.Right, upper) ||
            !TryReadResolvedViolation(bounds, out var boundsViolation) || boundsViolation.Ticks != 0 ||
            !TryReadResolvedBetweenViolation(first, out var initial) ||
            !TryReadResolvedBetweenViolation(second, out var final) ||
            !Equation.TryReadPrediction(difference, out var delta, out var differenceResidue) || delta.Carrier.IsUnknown ||
            !delta.Carrier.SameIdentityAs(boundsViolation.Carrier) ||
            !QuietIntervalDifferenceResidue(witnessResidue, final.Carrier, initial.Carrier) ||
            !QuietIntervalDifferenceResidue(differenceResidue, final.Carrier, initial.Carrier) ||
            (Int128)initial.Ticks - final.Ticks != delta.Ticks)
            return false;

        capturedInterval = interval;
        beforeClaim = first;
        afterClaim = second;
        beforeDemand = initial;
        afterDemand = final;
        reduction = delta;
        return true;
    }

    private static bool MatchesInterval(Relation claim, IElement sample, CarrierLocation lower, CarrierLocation upper) =>
        TryReadBetween(claim, out var surface, out _, out _, out _, out _, out _, out _) &&
        TryReadRootElements(surface, out var operands) && operands.Length == 3 &&
        TryReadFaceLocation(sample, out var value) && SameSample(operands[0], value) &&
        SameSample(operands[1], lower) && SameSample(operands[2], upper);

    private static bool SameSample(IElement element, CarrierLocation sample) =>
        TryReadFaceLocation(element, out var location) && location.Equals(sample);

    private static bool MatchesCurrentIntervalSide(IElement residual, IElement signedViolation,
        CarrierLocation residualMagnitude, CarrierLocation violationMagnitude, Int128 expectedDelta, bool lowerSide)
    {
        var expectedViolation = lowerSide ? Int128.Max(0, expectedDelta) : Int128.Max(0, -expectedDelta);
        return residual is CarrierLocation difference && signedViolation is CarrierLocation violation &&
            difference.Carrier.SameIdentityAs(violationMagnitude.Carrier) &&
            violation.Carrier.SameIdentityAs(violationMagnitude.Carrier) &&
            residualMagnitude.Carrier.SameIdentityAs(violationMagnitude.Carrier) &&
            difference.Ticks == expectedDelta && residualMagnitude.Ticks == Int128.Abs(expectedDelta) &&
            violation.Ticks == expectedViolation && violationMagnitude.Ticks == expectedViolation;
    }

    // Difference has exactly two unit-resolution operand ports. Its residue is
    // the ordered pair of whole source remainders, not an arbitrary zero-looking
    // nested structure. Open or fractional residue cannot establish this read.
    private static bool QuietIntervalDifferenceResidue(IElement residue, Carrier after, Carrier before) =>
        residue is Relation { Left: CarrierLocation left, Right: CarrierLocation right } &&
        left.Carrier.SameIdentityAs(after) && right.Carrier.SameIdentityAs(before) &&
        left.Ticks == 0 && right.Ticks == 0;

    private static IElement CaptureClaimSample(IElement element)
    {
        if (element is CarrierLocation location) return new CarrierLocation(location.Carrier, location.Ticks);
        if (element is Relation point && TryReadFaceLocation(point, out location))
            return Relation.Point(location.Carrier, location.Ticks);
        return element ?? new Placeholder();
    }
}
