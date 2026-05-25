using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Builds a follow surface from an equality claim and a response route. The
/// helper reads how much residual would route back into the follower without
/// mutating the source carrier.
/// </summary>
public static class OperationFollow
{
    /// <summary>
    /// Claims current equals target, then routes numerator/denominator of the
    /// readable residual back into current's carrier face.
    /// </summary>
    public static Relation Step(
        Carrier root,
        IElement current,
        IElement target,
        Carrier residual,
        long responseNumerator,
        long responseDenominator)
    {
        var holder = root ?? new Carrier();
        var residualCarrier = residual ?? Carrier.Unknown;
        var currentElement = current ?? Relation.Point(Carrier.Unknown, 0);
        var targetElement = target ?? Relation.Point(Carrier.Unknown, 0);
        var follower = TryReadFaceLocation(currentElement, out var currentLocation)
            ? currentLocation.Carrier
            : Carrier.Unknown;
        var claim = OperationClaim.Equality(new Carrier(), currentElement, targetElement, residualCarrier);
        var responseRoute = new Relation(
            Relation.Point(residualCarrier, responseDenominator),
            Relation.Point(follower, responseNumerator));

        var read = ReadFollow(
            currentLocation,
            currentElement,
            follower,
            claim,
            responseRoute,
            residualCarrier,
            responseNumerator,
            responseDenominator);

        return Relation.OrderedGroup(
            holder,
            claim,
            responseRoute,
            read.Applied,
            read.Next,
            read.Remaining,
            read.RouteResidue);
    }

    /// <summary>Reads a follow step and its routed response structure.</summary>
    public static bool TryRead(
        Relation follow,
        out Relation claim,
        out Relation responseRoute,
        out IElement applied,
        out IElement next,
        out IElement remaining,
        out IElement routeResidue)
    {
        if (TryReadRootElements(follow, out var elements) &&
            elements.Length >= 6 &&
            elements[0] is Relation equalityClaim &&
            elements[1] is Relation route &&
            OperationClaim.TryRead(equalityClaim, out _, out _, out _, out _, out _, out _))
        {
            claim = equalityClaim;
            responseRoute = route;
            applied = elements[2];
            next = elements[3];
            remaining = elements[4];
            routeResidue = elements[5];
            return true;
        }

        claim = null!;
        responseRoute = null!;
        applied = default!;
        next = default!;
        remaining = default!;
        routeResidue = default!;
        return false;
    }

    /// <summary>
    /// Applies the already-read follow adjustment through the follower carrier's
    /// ordinary tension input and records before/after carrier state.
    /// </summary>
    public static bool TryApply(Relation follow, out Relation application)
    {
        if (!TryRead(
                follow,
                out _,
                out _,
                out var applied,
                out _,
                out _,
                out _) ||
            applied is not CarrierLocation appliedLocation ||
            appliedLocation.Carrier.IsUnknown)
        {
            application = null!;
            return false;
        }

        var before = CarrierState(appliedLocation.Carrier);
        FeedTension(appliedLocation);
        var after = CarrierState(appliedLocation.Carrier);

        application = Relation.OrderedGroup(
            new Carrier(),
            follow,
            before,
            appliedLocation,
            after);
        return true;
    }

    /// <summary>Builds and applies one follow step.</summary>
    public static bool TryStepAndApply(
        Carrier root,
        IElement current,
        IElement target,
        Carrier residual,
        long responseNumerator,
        long responseDenominator,
        out Relation application)
    {
        var follow = Step(root, current, target, residual, responseNumerator, responseDenominator);
        return TryApply(follow, out application);
    }

    /// <summary>
    /// Repeatedly follows a target through explicit time-step records. Each
    /// timed step stores the carrier state before, the follow surface, the
    /// carrier state after, and the readable applied/residual evidence.
    /// </summary>
    public static Relation Settle(
        Carrier root,
        Carrier time,
        Carrier follower,
        IElement target,
        Carrier residual,
        long responseNumerator,
        long responseDenominator,
        int stepLimit,
        out Relation[] steps)
    {
        var holder = root ?? new Carrier();
        var clock = time ?? new Carrier();
        var followed = follower ?? Carrier.Unknown;
        var initial = CarrierState(followed);
        var builtSteps = new List<Relation>();
        var count = Math.Max(0, stepLimit);

        for (var index = 0; index < count; index++)
        {
            var follow = Step(
                new Carrier(),
                followed.ValueSlot,
                target,
                residual,
                responseNumerator,
                responseDenominator);
            var before = CarrierState(followed);
            var after = before;
            var residueEvidence = (IElement)Relation.Point(Carrier.Unknown, 0);

            if (!TryRead(follow, out _, out _, out var appliedElement, out _, out var remaining, out var routeResidue))
            {
                builtSteps.Add(TimeStepSequence.Step(
                    new Carrier(),
                    clock,
                    index,
                    index + 1,
                    before,
                    follow,
                    after,
                    residueEvidence));
                break;
            }

            if (!TryApply(follow, out var application) ||
                !TryReadApplication(application, out _, out var appliedBefore, out var applied, out var appliedAfter))
            {
                residueEvidence = FollowStepResidue(appliedElement, remaining, routeResidue);
                builtSteps.Add(TimeStepSequence.Step(
                    new Carrier(),
                    clock,
                    index,
                    index + 1,
                    before,
                    follow,
                    after,
                    residueEvidence));
                break;
            }

            before = appliedBefore;
            after = appliedAfter;
            residueEvidence = FollowStepResidue(applied, remaining, routeResidue);
            builtSteps.Add(TimeStepSequence.Step(
                new Carrier(),
                clock,
                index,
                index + 1,
                before,
                follow,
                after,
                residueEvidence));

            if (applied.Ticks == 0 || IsQuiet(remaining))
            {
                break;
            }
        }

        steps = [.. builtSteps];
        return TimeStepSequence.Create(holder, clock, initial, steps);
    }

    /// <summary>Reads an application record returned by <see cref="TryApply"/>.</summary>
    public static bool TryReadApplication(
        Relation application,
        out Relation follow,
        out Relation before,
        out CarrierLocation applied,
        out Relation after)
    {
        if (TryReadRootElements(application, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation followStep &&
            elements[1] is Relation beforeState &&
            elements[2] is CarrierLocation appliedLocation &&
            elements[3] is Relation afterState &&
            TryRead(followStep, out _, out _, out _, out _, out _, out _) &&
            TryReadCarrierState(beforeState, out _, out _) &&
            TryReadCarrierState(afterState, out _, out _))
        {
            follow = followStep;
            before = beforeState;
            applied = appliedLocation;
            after = afterState;
            return true;
        }

        follow = null!;
        before = null!;
        applied = default;
        after = null!;
        return false;
    }

    /// <summary>Reads a carrier state snapshot as visible value and tension.</summary>
    public static bool TryReadCarrierState(
        Relation state,
        out CarrierLocation value,
        out CarrierLocation tension)
    {
        if (TryReadRootElements(state, out var elements) &&
            elements.Length >= 2 &&
            elements[0] is CarrierLocation valueLocation &&
            elements[1] is CarrierLocation tensionLocation)
        {
            value = valueLocation;
            tension = tensionLocation;
            return true;
        }

        value = default;
        tension = default;
        return false;
    }

    private static FollowRead ReadFollow(
        CarrierLocation current,
        IElement currentElement,
        Carrier follower,
        Relation claim,
        Relation responseRoute,
        Carrier residualCarrier,
        long responseNumerator,
        long responseDenominator)
    {
        if (!OperationClaim.TryRead(
                claim,
                out _,
                out var residual,
                out _,
                out _,
                out _,
                out _) ||
            residual is not CarrierLocation residualLocation ||
            responseNumerator == 0 ||
            responseDenominator == 0 ||
            !responseRoute.TryProject(residualLocation, follower, out var projected) ||
            !projected.TryGetCarrierLocations(out var applied, out var routeResidue) ||
            !applied.Carrier.SameIdentityAs(follower))
        {
            return new FollowRead(
                new CarrierLocation(follower, 0),
                currentElement,
                residual,
                residual);
        }

        var next = new CarrierLocation(follower, SaturatingAdd(current.Ticks, applied.Ticks));
        var remaining = new CarrierLocation(residualCarrier, SaturatingSubtract(residualLocation.Ticks, applied.Ticks));
        return new FollowRead(applied, next, remaining, routeResidue);
    }

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

    private static Relation CarrierState(Carrier carrier) =>
        Relation.OrderedGroup(
            new Carrier(),
            new CarrierLocation(carrier, checked((long)carrier.Value)),
            carrier.TensionSlot);

    private static Relation FollowStepResidue(IElement applied, IElement remaining, IElement routeResidue) =>
        Relation.OrderedGroup(
            new Carrier(),
            applied ?? Relation.Point(Carrier.Unknown, 0),
            remaining ?? Relation.Point(Carrier.Unknown, 0),
            routeResidue ?? Relation.Point(Carrier.Unknown, 0));

    private static bool IsQuiet(IElement remaining) =>
        remaining is CarrierLocation remainingLocation && remainingLocation.Ticks == 0;

    private static void FeedTension(CarrierLocation adjustment)
    {
        if (adjustment.Ticks > 0)
        {
            adjustment.Carrier.Add(checked((ulong)adjustment.Ticks));
            return;
        }

        if (adjustment.Ticks == 0)
        {
            return;
        }

        if (adjustment.Ticks == long.MinValue)
        {
            adjustment.Carrier.Remove((ulong)long.MaxValue);
            adjustment.Carrier.Remove(1);
            return;
        }

        adjustment.Carrier.Remove(checked((ulong)-adjustment.Ticks));
    }

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

    private static long SaturatingAdd(long left, long right)
    {
        var sum = (Int128)left + right;
        return ClampToLong(sum);
    }

    private static long SaturatingSubtract(long left, long right)
    {
        var difference = (Int128)left - right;
        return ClampToLong(difference);
    }

    private static long ClampToLong(Int128 value)
    {
        if (value > long.MaxValue)
        {
            return long.MaxValue;
        }

        return value < long.MinValue ? long.MinValue : (long)value;
    }

    private readonly record struct FollowRead(
        IElement Applied,
        IElement Next,
        IElement Remaining,
        IElement RouteResidue);
}
