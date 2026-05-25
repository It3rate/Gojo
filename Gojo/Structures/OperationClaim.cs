using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Builds small claim surfaces whose readable residual remains ordinary Gojo
/// structure. Helper names describe construction intent; the claim itself is
/// the connected surface, residual, one-sided violation, magnitudes, and residue.
/// </summary>
public static class OperationClaim
{
    /// <summary>Claims that an actual value equals an expected value.</summary>
    public static Relation Equality(Carrier root, IElement actual, IElement expected, Carrier residual) =>
        Build(root, actual, expected, residual, EqualityViolation, preserveOpposedViolation: true);

    /// <summary>Claims that a value does not exceed a ceiling.</summary>
    public static Relation AtMost(Carrier root, IElement value, IElement ceiling, Carrier residual) =>
        Build(root, value, ceiling, residual, AtMostViolation, preserveOpposedViolation: false);

    /// <summary>Claims that a value does not fall below a floor.</summary>
    public static Relation AtLeast(Carrier root, IElement value, IElement floor, Carrier residual) =>
        Build(root, value, floor, residual, AtLeastViolation, preserveOpposedViolation: false);

    /// <summary>Claims that a value remains inside a lower/upper interval.</summary>
    public static Relation Between(Carrier root, IElement value, IElement lower, IElement upper, Carrier residual)
    {
        var holder = root ?? new Carrier();
        var residualCarrier = residual ?? Carrier.Unknown;
        var valueElement = value ?? Relation.Point(Carrier.Unknown, 0);
        var lowerElement = lower ?? Relation.Point(Carrier.Unknown, 0);
        var upperElement = upper ?? Relation.Point(Carrier.Unknown, 0);
        var surface = Relation.OrderedGroup(new Carrier(), valueElement, lowerElement, upperElement);
        var lowerClaim = AtLeast(new Carrier(), valueElement, lowerElement, residualCarrier);
        var upperClaim = AtMost(new Carrier(), valueElement, upperElement, residualCarrier);

        TryRead(lowerClaim, out _, out _, out _, out _, out var lowerViolation, out _);
        TryRead(upperClaim, out _, out _, out _, out _, out var upperViolation, out _);
        var totalViolation = new CarrierLocation(
            residualCarrier,
            SaturatingAdd(lowerViolation.Ticks, upperViolation.Ticks));

        return Relation.OrderedGroup(
            holder,
            surface,
            lowerClaim,
            upperClaim,
            lowerViolation,
            upperViolation,
            totalViolation,
            Relation.OrderedGroup(new Carrier(), lowerClaim, upperClaim));
    }

    /// <summary>Reads the surface, residuals, magnitudes, and residue from a claim group.</summary>
    public static bool TryRead(
        Relation claim,
        out Relation surface,
        out IElement residual,
        out IElement violation,
        out CarrierLocation residualMagnitude,
        out CarrierLocation violationMagnitude,
        out IElement residue)
    {
        if (TryReadRootElements(claim, out var elements) &&
            elements.Length >= 6 &&
            elements[0] is Relation claimSurface &&
            elements[3] is CarrierLocation residualMagnitudeLocation &&
            elements[4] is CarrierLocation violationMagnitudeLocation)
        {
            surface = claimSurface;
            residual = elements[1];
            violation = elements[2];
            residualMagnitude = residualMagnitudeLocation;
            violationMagnitude = violationMagnitudeLocation;
            residue = elements[5];
            return true;
        }

        surface = null!;
        residual = default!;
        violation = default!;
        residualMagnitude = default;
        violationMagnitude = default;
        residue = default!;
        return false;
    }

    /// <summary>Reads a paired lower/upper interval claim.</summary>
    public static bool TryReadBetween(
        Relation claim,
        out Relation surface,
        out Relation lowerClaim,
        out Relation upperClaim,
        out CarrierLocation lowerViolationMagnitude,
        out CarrierLocation upperViolationMagnitude,
        out CarrierLocation totalViolationMagnitude,
        out IElement residue)
    {
        if (TryReadRootElements(claim, out var elements) &&
            elements.Length >= 7 &&
            elements[0] is Relation intervalSurface &&
            elements[1] is Relation lower &&
            elements[2] is Relation upper &&
            elements[3] is CarrierLocation lowerViolation &&
            elements[4] is CarrierLocation upperViolation &&
            elements[5] is CarrierLocation totalViolation &&
            TryRead(lower, out _, out _, out _, out _, out _, out _) &&
            TryRead(upper, out _, out _, out _, out _, out _, out _))
        {
            surface = intervalSurface;
            lowerClaim = lower;
            upperClaim = upper;
            lowerViolationMagnitude = lowerViolation;
            upperViolationMagnitude = upperViolation;
            totalViolationMagnitude = totalViolation;
            residue = elements[6];
            return true;
        }

        surface = null!;
        lowerClaim = null!;
        upperClaim = null!;
        lowerViolationMagnitude = default;
        upperViolationMagnitude = default;
        totalViolationMagnitude = default;
        residue = default!;
        return false;
    }

    private static Relation Build(
        Carrier root,
        IElement actual,
        IElement expected,
        Carrier residual,
        Func<long, long, long> violationRead,
        bool preserveOpposedViolation)
    {
        var holder = root ?? new Carrier();
        var residualCarrier = residual ?? Carrier.Unknown;
        var actualElement = actual ?? Relation.Point(Carrier.Unknown, 0);
        var expectedElement = expected ?? Relation.Point(Carrier.Unknown, 0);
        var surface = new Relation(
            actualElement,
            expectedElement);

        var read = Read(actualElement, expectedElement, residualCarrier, violationRead, preserveOpposedViolation, surface);
        return Relation.OrderedGroup(
            holder,
            surface,
            read.Residual,
            read.Violation,
            new CarrierLocation(residualCarrier, read.ResidualMagnitude),
            new CarrierLocation(residualCarrier, read.ViolationMagnitude),
            read.Residue);
    }

    private static ClaimRead Read(
        IElement actual,
        IElement expected,
        Carrier residual,
        Func<long, long, long> violationRead,
        bool preserveOpposedViolation,
        Relation surface)
    {
        if (!TryReadFaceLocation(actual, out var actualLocation) ||
            !TryReadFaceLocation(expected, out var expectedLocation))
        {
            return Unresolved(residual, surface);
        }

        if (actualLocation.Carrier.SameIdentityAs(expectedLocation.Carrier))
        {
            var delta = checked(expectedLocation.Ticks - actualLocation.Ticks);
            var violation = violationRead(actualLocation.Ticks, expectedLocation.Ticks);
            return new ClaimRead(
                new CarrierLocation(residual, delta),
                new CarrierLocation(residual, violation),
                MagnitudeOf(delta),
                MagnitudeOf(violation),
                ZeroResidue());
        }

        if (TryReadOpposedFaces(actualLocation.Carrier, expectedLocation.Carrier, out var inverse, out var positive))
        {
            var residualStructure = DifferenceAcrossFaces(
                actualLocation,
                expectedLocation,
                residual,
                inverse,
                positive);
            var magnitude = SupportMagnitude(actualLocation.Ticks, expectedLocation.Ticks);
            return new ClaimRead(
                residualStructure,
                preserveOpposedViolation ? residualStructure : new CarrierLocation(residual, 0),
                magnitude,
                preserveOpposedViolation ? magnitude : 0,
                residualStructure);
        }

        return Unresolved(residual, surface);
    }

    private static IElement DifferenceAcrossFaces(
        CarrierLocation actual,
        CarrierLocation expected,
        Carrier residual,
        Carrier inverse,
        Carrier positive)
    {
        var actualContribution = new CarrierLocation(
            actual.Carrier.SameIdentityAs(inverse) ? OpposedFace(residual) : residual,
            checked(-actual.Ticks));
        var expectedContribution = new CarrierLocation(
            expected.Carrier.SameIdentityAs(inverse) ? OpposedFace(residual) : residual,
            expected.Ticks);

        if (actualContribution.Carrier.SameIdentityAs(expectedContribution.Carrier))
        {
            return new CarrierLocation(
                actualContribution.Carrier,
                checked(actualContribution.Ticks + expectedContribution.Ticks));
        }

        return expected.Carrier.SameIdentityAs(positive)
            ? new Relation(actualContribution, expectedContribution)
            : new Relation(expectedContribution, actualContribution);
    }

    private static ClaimRead Unresolved(Carrier residual, Relation surface) =>
        new(
            new Relation(surface.Left, surface.Right),
            new CarrierLocation(residual, 0),
            long.MaxValue,
            0,
            surface);

    private static long EqualityViolation(long actual, long expected) => checked(expected - actual);

    private static long AtMostViolation(long actual, long expected) => PositivePart(checked(actual - expected));

    private static long AtLeastViolation(long actual, long expected) => PositivePart(checked(expected - actual));

    private static bool TryReadFaceLocation(IElement element, out CarrierLocation location)
    {
        if (element is CarrierLocation direct)
        {
            location = direct;
            return true;
        }

        if (element is Relation point &&
            point.Left is CarrierLocation pointLocation &&
            point.Right is Carrier pointCarrier &&
            pointLocation.Carrier.SameIdentityAs(pointCarrier))
        {
            location = pointLocation;
            return true;
        }

        location = default;
        return false;
    }

    private static bool TryReadOpposedFaces(
        Carrier first,
        Carrier second,
        out Carrier inverse,
        out Carrier positive)
    {
        if (first.IsInverse == second.IsInverse)
        {
            inverse = null!;
            positive = null!;
            return false;
        }

        inverse = first.IsInverse ? first : second;
        positive = first.IsInverse ? second : first;
        return inverse.ExistingInverse is { } inversePositive && inversePositive.SameIdentityAs(positive) ||
            positive.ExistingInverse is { } positiveInverse && positiveInverse.SameIdentityAs(inverse);
    }

    private static Carrier OpposedFace(Carrier carrier) =>
        carrier.IsUnknown ? Carrier.Unknown : carrier.Inverse();

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static IElement ZeroResidue() => new CarrierLocation(Carrier.Unknown, 0);

    private static long PositivePart(long ticks) => ticks > 0 ? ticks : 0;

    private static long SaturatingAdd(long left, long right)
    {
        var sum = (Int128)left + right;
        return ClampToLong(sum);
    }

    private static long MagnitudeOf(long ticks)
    {
        var magnitude = ticks < 0 ? -(Int128)ticks : (Int128)ticks;
        return magnitude > long.MaxValue ? long.MaxValue : (long)magnitude;
    }

    private static long ClampToLong(Int128 value)
    {
        if (value > long.MaxValue)
        {
            return long.MaxValue;
        }

        return value < long.MinValue ? long.MinValue : (long)value;
    }

    private static long SupportMagnitude(long left, long right)
    {
        var magnitude = (Int128)MagnitudeOf(left) + MagnitudeOf(right);
        return magnitude > long.MaxValue ? long.MaxValue : (long)magnitude;
    }

    private readonly record struct ClaimRead(
        IElement Residual,
        IElement Violation,
        long ResidualMagnitude,
        long ViolationMagnitude,
        IElement Residue);

}
