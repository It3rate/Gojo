using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Lane-aware route projection between value surfaces. This is a route-adjacent
/// experiment: the projection relation moves the signed/whole location, while
/// the target lane basis decides whether the resulting occupied lane is legal.
/// </summary>
public static class BoundaryFlowValueRoute
{
    public static Relation Create(
        Carrier root,
        Relation sourceValue,
        Relation projectionRoute,
        Relation targetBasis,
        Relation targetValue,
        IElement residueOrTension)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            sourceValue ?? ConceptSurface.UnknownPoint(),
            projectionRoute ?? ConceptSurface.UnknownPoint(),
            targetBasis ?? ConceptSurface.UnknownPoint(),
            targetValue ?? ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint());
    }

    public static bool TryProject(
        Relation sourceValue,
        Relation projectionRoute,
        Carrier targetCarrier,
        out Relation routeSurface)
    {
        if (targetCarrier is null ||
            !BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
                targetCarrier.UnitShape,
                out var targetBasis,
                out _,
                out _,
                out _))
        {
            routeSurface = null!;
            return false;
        }

        return TryProject(sourceValue, projectionRoute, targetBasis, targetCarrier, out routeSurface);
    }

    public static bool TryProject(
        Relation sourceValue,
        Relation projectionRoute,
        Relation targetBasis,
        Carrier targetCarrier,
        out Relation routeSurface)
    {
        if (!BoundaryFlowValueSurface.TryReadValue(sourceValue, out var sourceRead) ||
            !sourceRead.IsLegal ||
            projectionRoute is null ||
            targetBasis is null ||
            targetCarrier is null ||
            targetCarrier.IsUnknown ||
            !projectionRoute.TryProject(ProjectedSourceLocation(sourceRead), targetCarrier, out var projected) ||
            !projected.TryGetCarrierLocations(out var targetLocation, out var projectionResidue))
        {
            routeSurface = null!;
            return false;
        }

        var targetLane = ProjectedTargetLane(sourceRead, targetLocation, targetBasis);
        var isAllowed = BoundaryFlowLaneBasis.Allows(targetBasis, targetLane);
        var residueOrTension = isAllowed
            ? Relation.Point(projectionResidue.Carrier, projectionResidue.Ticks)
            : Relation.Point(targetLocation.Carrier, targetLocation.Ticks);
        var targetValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(targetLocation.Carrier, AbsAsLong(targetLocation.Ticks)),
            targetLane,
            targetBasis,
            residueOrTension);

        routeSurface = Create(
            new Carrier(),
            sourceValue,
            projectionRoute,
            targetBasis,
            targetValue,
            residueOrTension);
        return true;
    }

    public static bool TryRead(
        Relation routeSurface,
        out Relation sourceValue,
        out Relation projectionRoute,
        out Relation targetBasis,
        out Relation targetValue,
        out IElement residueOrTension)
    {
        if (ConceptSurfaceReader.TryReadRootElements(routeSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation source &&
            elements[1] is Relation projection &&
            elements[2] is Relation basis &&
            elements[3] is Relation target)
        {
            sourceValue = source;
            projectionRoute = projection;
            targetBasis = basis;
            targetValue = target;
            residueOrTension = elements[4];
            return true;
        }

        sourceValue = null!;
        projectionRoute = null!;
        targetBasis = null!;
        targetValue = null!;
        residueOrTension = default!;
        return false;
    }

    public static bool TryReadTargetValue(
        Relation routeSurface,
        out BoundaryFlowValueRead targetRead)
    {
        if (TryRead(
                routeSurface,
                out _,
                out _,
                out var targetBasis,
                out var targetValue,
                out _))
        {
            return BoundaryFlowValueSurface.TryReadValue(targetValue, targetBasis, out targetRead);
        }

        targetRead = default;
        return false;
    }

    /// <summary>
    /// Reads the source side of a projection route as a basis read, preserving
    /// the projection relation with the signed/whole source coordinate used by
    /// the projection.
    /// </summary>
    public static bool TryReadSourceBasisRead(
        Relation routeSurface,
        Carrier root,
        out Relation basisRead)
    {
        if (TryRead(
                routeSurface,
                out var sourceValue,
                out var projectionRoute,
                out _,
                out _,
                out _) &&
            BoundaryFlowValueSurface.TryRead(sourceValue, out _, out _, out var laneMarker, out _, out _) &&
            BoundaryFlowValueSurface.TryReadValueWithBasis(sourceValue, out var sourceRead))
        {
            basisRead = BasisReadSurface.Create(
                ConceptSurface.RootOrNew(root),
                sourceValue,
                laneMarker,
                sourceRead.LaneBasis,
                RouteMetricEvidence(projectionRoute, sourceRead),
                sourceRead.ResidueOrTension,
                routeSurface);
            return true;
        }

        basisRead = null!;
        return false;
    }

    /// <summary>
    /// Reads the projected target side of a route as a basis read, preserving
    /// the projection relation with the signed/whole target coordinate reached
    /// by the projection.
    /// </summary>
    public static bool TryReadTargetBasisRead(
        Relation routeSurface,
        Carrier root,
        out Relation basisRead)
    {
        if (TryRead(
                routeSurface,
                out _,
                out var projectionRoute,
                out var targetBasis,
                out var targetValue,
                out _) &&
            BoundaryFlowValueSurface.TryRead(targetValue, out _, out _, out var laneMarker, out _, out _) &&
            BoundaryFlowValueSurface.TryReadValueWithBasis(targetValue, targetBasis, out var targetRead))
        {
            basisRead = BasisReadSurface.Create(
                ConceptSurface.RootOrNew(root),
                targetValue,
                laneMarker,
                targetBasis,
                RouteMetricEvidence(projectionRoute, targetRead),
                targetRead.ResidueOrTension,
                routeSurface);
            return true;
        }

        basisRead = null!;
        return false;
    }

    public static bool TryReadBasisReads(
        Relation routeSurface,
        Carrier root,
        out Relation sourceBasisRead,
        out Relation targetBasisRead)
    {
        if (TryReadSourceBasisRead(routeSurface, root, out sourceBasisRead) &&
            TryReadTargetBasisRead(routeSurface, root, out targetBasisRead))
        {
            return true;
        }

        sourceBasisRead = null!;
        targetBasisRead = null!;
        return false;
    }

    /// <summary>
    /// Reads the route as a common witness from source basis read, through the
    /// projection relation, to target basis read, with route residue preserved.
    /// </summary>
    public static bool TryReadProjectionWitness(
        Relation routeSurface,
        Carrier root,
        out Relation witness)
    {
        if (TryRead(
                routeSurface,
                out _,
                out var projectionRoute,
                out _,
                out _,
                out var residueOrTension) &&
            TryReadBasisReads(routeSurface, new Carrier(), out var sourceBasisRead, out var targetBasisRead))
        {
            witness = ReadWitness.Create(
                ConceptSurface.RootOrNew(root),
                sourceBasisRead,
                projectionRoute,
                targetBasisRead,
                residueOrTension,
                routeSurface);
            return true;
        }

        witness = null!;
        return false;
    }

    private static CarrierLocation ProjectedSourceLocation(BoundaryFlowValueRead sourceRead) =>
        sourceRead.IsWholeBoundary
            ? sourceRead.Magnitude
            : new CarrierLocation(sourceRead.Magnitude.Carrier, sourceRead.SignedTicks);

    private static Relation RouteMetricEvidence(Relation projectionRoute, BoundaryFlowValueBasisRead read) =>
        Relation.OrderedGroup(
            new Carrier(),
            projectionRoute ?? ConceptSurface.UnknownPoint(),
            Relation.Point(
                read.Magnitude.Carrier,
                read.IsWholeBoundary ? read.Magnitude.Ticks : read.SignedTicks));

    private static BoundaryFlowLane ProjectedTargetLane(
        BoundaryFlowValueRead sourceRead,
        CarrierLocation targetLocation,
        Relation targetBasis)
    {
        var boundary = sourceRead.IsWholeBoundary
            ? BoundaryLane.Whole
            : targetLocation.Ticks < 0
                ? BoundaryLane.Negative
                : targetLocation.Ticks > 0
                    ? BoundaryLane.Positive
                    : ZeroBoundaryLane(sourceRead.Lane.Boundary, targetBasis);
        return new BoundaryFlowLane(boundary, sourceRead.Lane.Flow);
    }

    private static BoundaryLane ZeroBoundaryLane(BoundaryLane sourceBoundary, Relation targetBasis)
    {
        if (BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out _, out var lanes) &&
            lanes.Select(lane => lane.Boundary).Distinct().Contains(sourceBoundary))
        {
            return sourceBoundary;
        }

        if (BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out _, out lanes) &&
            lanes.Select(lane => lane.Boundary).Distinct().ToArray() is [var only])
        {
            return only;
        }

        return BoundaryLane.Positive;
    }

    private static long AbsAsLong(long value)
    {
        if (value == long.MinValue)
        {
            return long.MaxValue;
        }

        return Math.Abs(value);
    }
}

/// <summary>
/// Accepts or blocks a lane-aware projected route value. The source route
/// remains visible; this surface adds accepted value, blocked value, and the
/// capacity or gate witness used by the local read.
/// </summary>

/// <summary>
/// Accepts or blocks a lane-aware projected route value. The source route
/// remains visible; this surface adds accepted value, blocked value, and the
/// capacity or gate witness used by the local read.
/// </summary>
public static class BoundaryFlowValueTransfer
{
    public static Relation Create(
        Carrier root,
        Relation routeSurface,
        IElement acceptedValue,
        IElement blockedValue,
        IElement capacityOrGate)
    {
        var route = routeSurface ?? ConceptSurface.UnknownPoint();
        var accepted = acceptedValue ?? ConceptSurface.UnknownPoint();
        var blocked = blockedValue ?? ConceptSurface.UnknownPoint();
        var gate = capacityOrGate ?? ConceptSurface.UnknownPoint();
        var acceptedWitness = TransferValueWitnessOrUnknown(route, accepted, blocked, gate);
        var blockedWitness = TransferValueWitnessOrUnknown(route, blocked, accepted, gate);

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            route,
            accepted,
            blocked,
            gate,
            acceptedWitness,
            blockedWitness);
    }

    public static bool TryAccept(
        Relation routeSurface,
        out Relation transferSurface) =>
        TryAccept(routeSurface, null, out transferSurface);

    public static bool TryAccept(
        Relation routeSurface,
        Relation? capacity,
        out Relation transferSurface)
    {
        if (!BoundaryFlowValueRoute.TryRead(
                routeSurface,
                out _,
                out _,
                out var targetBasis,
                out var projectedValue,
                out _) ||
            !BoundaryFlowValueSurface.TryReadValue(projectedValue, targetBasis, out var projectedRead) ||
            !projectedRead.IsUnsignedMagnitude)
        {
            transferSurface = null!;
            return false;
        }

        var capacityTicks = ReadCapacity(capacity, projectedRead.Magnitude.Ticks);
        if (!projectedRead.IsLegal)
        {
            var blocked = CreateTransferValue(
                projectedRead,
                projectedRead.Magnitude.Ticks,
                SignedResidueTicks(projectedRead.Lane, projectedRead.Magnitude.Ticks),
                targetBasis);
            transferSurface = Create(
                new Carrier(),
                routeSurface,
                ConceptSurface.UnknownPoint(),
                blocked,
                capacity ?? ConceptSurface.UnknownPoint());
            return true;
        }

        var acceptedTicks = Math.Min(projectedRead.Magnitude.Ticks, capacityTicks);
        var blockedTicks = checked(projectedRead.Magnitude.Ticks - acceptedTicks);
        var accepted = CreateTransferValue(projectedRead, acceptedTicks, 0, targetBasis);
        var blockedValue = CreateTransferValue(
            projectedRead,
            blockedTicks,
            SignedResidueTicks(projectedRead.Lane, blockedTicks),
            targetBasis);

        transferSurface = Create(
            new Carrier(),
            routeSurface,
            accepted,
            blockedValue,
            capacity ?? ConceptSurface.UnknownPoint());
        return true;
    }

    public static bool TryRead(
        Relation transferSurface,
        out Relation routeSurface,
        out IElement acceptedValue,
        out IElement blockedValue,
        out IElement capacityOrGate)
    {
        if (ConceptSurfaceReader.TryReadRootElements(transferSurface, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation route)
        {
            routeSurface = route;
            acceptedValue = elements[1];
            blockedValue = elements[2];
            capacityOrGate = elements[3];
            return true;
        }

        routeSurface = null!;
        acceptedValue = default!;
        blockedValue = default!;
        capacityOrGate = default!;
        return false;
    }

    public static bool TryReadAcceptedValue(
        Relation transferSurface,
        out BoundaryFlowValueRead acceptedRead)
    {
        if (TryRead(transferSurface, out _, out var accepted, out _, out _) &&
            accepted is Relation acceptedRelation)
        {
            return BoundaryFlowValueSurface.TryReadValue(acceptedRelation, out acceptedRead);
        }

        acceptedRead = default;
        return false;
    }

    public static bool TryReadBlockedValue(
        Relation transferSurface,
        out BoundaryFlowValueRead blockedRead)
    {
        if (TryRead(transferSurface, out _, out _, out var blocked, out _) &&
            blocked is Relation blockedRelation)
        {
            return BoundaryFlowValueSurface.TryReadValue(blockedRelation, out blockedRead);
        }

        blockedRead = default;
        return false;
    }

    public static bool TryReadSummary(
        Relation transferSurface,
        out BoundaryFlowTransferRead read)
    {
        var hasAccepted = TryReadAcceptedValue(transferSurface, out var accepted);
        if (!TryReadBlockedValue(transferSurface, out var blocked))
        {
            read = default;
            return false;
        }

        read = new BoundaryFlowTransferRead(
            hasAccepted ? accepted : default,
            blocked,
            hasAccepted);
        return true;
    }

    public static bool TryReadAcceptedWitness(
        Relation transferSurface,
        Carrier root,
        out Relation witness)
    {
        if (ReadWitness.TryReadEmbedded(transferSurface, index: 4, out witness))
        {
            return true;
        }

        if (TryRead(
                transferSurface,
                out var routeSurface,
                out var acceptedValue,
                out var blockedValue,
                out var capacityOrGate) &&
            TryCreateTransferValueWitness(
                routeSurface,
                acceptedValue,
                blockedValue,
                capacityOrGate,
                root,
                out witness))
        {
            return true;
        }

        witness = null!;
        return false;
    }

    public static bool TryReadBlockedWitness(
        Relation transferSurface,
        Carrier root,
        out Relation witness)
    {
        if (ReadWitness.TryReadEmbedded(transferSurface, index: 5, out witness))
        {
            return true;
        }

        if (TryRead(
                transferSurface,
                out var routeSurface,
                out var acceptedValue,
                out var blockedValue,
                out var capacityOrGate) &&
            TryCreateTransferValueWitness(
                routeSurface,
                blockedValue,
                acceptedValue,
                capacityOrGate,
                root,
                out witness))
        {
            return true;
        }

        witness = null!;
        return false;
    }

    public static bool TryReadTransferWitnesses(
        Relation transferSurface,
        Carrier root,
        out Relation acceptedWitness,
        out Relation blockedWitness)
    {
        var hasAccepted = TryReadAcceptedWitness(transferSurface, root, out acceptedWitness);
        var hasBlocked = TryReadBlockedWitness(transferSurface, root, out blockedWitness);
        if (hasAccepted || hasBlocked)
        {
            return true;
        }

        acceptedWitness = null!;
        blockedWitness = null!;
        return false;
    }

    private static Relation CreateTransferValue(
        BoundaryFlowValueRead projectedRead,
        long magnitudeTicks,
        long residueTicks,
        Relation targetBasis) =>
        BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(projectedRead.Magnitude.Carrier, magnitudeTicks),
            projectedRead.Lane,
            targetBasis,
            Relation.Point(projectedRead.Magnitude.Carrier, residueTicks));

    private static long ReadCapacity(Relation? capacity, long fallback)
    {
        if (capacity is null ||
            !ConceptSurfaceReader.TryReadPoint(capacity, out var capacityLocation))
        {
            return fallback;
        }

        return Math.Max(0, capacityLocation.Ticks);
    }

    private static long SignedResidueTicks(BoundaryFlowLane lane, long magnitudeTicks) =>
        lane.Boundary.IsNegative ? checked(-magnitudeTicks) : magnitudeTicks;

    private static IElement TransferValueWitnessOrUnknown(
        Relation routeSurface,
        IElement resultValue,
        IElement residueValue,
        IElement capacityOrGate) =>
        TryCreateTransferValueWitness(
            routeSurface,
            resultValue,
            residueValue,
            capacityOrGate,
            new Carrier(),
            out var witness)
            ? witness
            : ConceptSurface.UnknownPoint();

    private static bool TryCreateTransferValueWitness(
        Relation routeSurface,
        IElement resultValue,
        IElement residueValue,
        IElement capacityOrGate,
        Carrier root,
        out Relation witness)
    {
        if (resultValue is not Relation resultRelation ||
            !BoundaryFlowValueSurface.TryReadBasisRead(resultRelation, new Carrier(), out var resultBasisRead))
        {
            witness = null!;
            return false;
        }

        var residue = residueValue is Relation residueRelation &&
            BoundaryFlowValueSurface.TryReadBasisRead(residueRelation, new Carrier(), out var residueBasisRead)
                ? residueBasisRead
                : residueValue ?? ConceptSurface.UnknownPoint();
        var route = routeSurface ?? ConceptSurface.UnknownPoint();
        var gate = capacityOrGate ?? ConceptSurface.UnknownPoint();
        var provenance = Relation.OrderedGroup(
            new Carrier(),
            route,
            gate);

        witness = ReadWitness.Create(
            ConceptSurface.RootOrNew(root),
            route,
            TransferReadSurface(route, gate),
            resultBasisRead,
            residue,
            provenance);
        return true;
    }

    private static Relation TransferReadSurface(Relation routeSurface, IElement capacityOrGate)
    {
        if (routeSurface is not null &&
            BoundaryFlowValueRoute.TryReadProjectionWitness(routeSurface, new Carrier(), out var projectionWitness))
        {
            return Relation.OrderedGroup(
                new Carrier(),
                projectionWitness,
                capacityOrGate ?? ConceptSurface.UnknownPoint());
        }

        return Relation.OrderedGroup(
            new Carrier(),
            routeSurface ?? ConceptSurface.UnknownPoint(),
            capacityOrGate ?? ConceptSurface.UnknownPoint());
    }

}


public readonly record struct BoundaryFlowTransferRead(
    BoundaryFlowValueRead Accepted,
    BoundaryFlowValueRead Blocked,
    bool HasAccepted)
{
    public bool IsFullyAccepted =>
        HasAccepted &&
        Accepted.IsLegal &&
        Blocked.Magnitude.Ticks == 0;

    public bool IsPartiallyAccepted =>
        HasAccepted &&
        Accepted.Magnitude.Ticks > 0 &&
        Blocked.Magnitude.Ticks > 0;

    public bool IsFullyBlocked =>
        !HasAccepted ||
        Accepted.Magnitude.Ticks == 0 && Blocked.Magnitude.Ticks > 0;
}

/// <summary>
/// Builds a visible route -> transfer -> settlement timeline from the
/// projection, acceptance, and target-settlement witnesses already present in
/// the local surfaces.
/// </summary>

/// <summary>
/// Builds a visible route -> transfer -> settlement timeline from the
/// projection, acceptance, and target-settlement witnesses already present in
/// the local surfaces.
/// </summary>
public static class BoundaryFlowRouteSettlementTimeline
{
    public static bool TryCreate(
        Relation routeSurface,
        Relation transferSurface,
        Relation settlementSurface,
        out Relation timeline)
    {
        if (!BoundaryFlowValueTransfer.TryRead(
                transferSurface,
                out var transferRoute,
                out _,
                out var blockedValue,
                out _) ||
            (routeSurface is not null && !transferRoute.Equals(routeSurface)))
        {
            timeline = null!;
            return false;
        }

        var route = routeSurface ?? transferRoute;
        return TryCreate(route, transferSurface, settlementSurface, blockedValue, out timeline);
    }

    public static bool TryCreate(
        Relation transferSurface,
        Relation settlementSurface,
        out Relation timeline)
    {
        if (!BoundaryFlowValueTransfer.TryRead(
                transferSurface,
                out var routeSurface,
                out _,
                out var blockedValue,
                out _))
        {
            timeline = null!;
            return false;
        }

        return TryCreate(routeSurface, transferSurface, settlementSurface, blockedValue, out timeline);
    }

    public static bool TryRead(
        Relation timeline,
        out Relation projectionStep,
        out Relation transferStep,
        out Relation settlementStep)
    {
        if (TimeStepSequence.TryRead(timeline, out _, out _, out var steps) &&
            steps.Length >= 3)
        {
            projectionStep = steps[0];
            transferStep = steps[1];
            settlementStep = steps[2];
            return true;
        }

        projectionStep = null!;
        transferStep = null!;
        settlementStep = null!;
        return false;
    }

    private static bool TryCreate(
        Relation routeSurface,
        Relation transferSurface,
        Relation settlementSurface,
        IElement blockedValue,
        out Relation timeline)
    {
        if (!BoundaryFlowValueRoute.TryRead(
                routeSurface,
                out _,
                out _,
                out _,
                out _,
                out var routeResidue) ||
            !BoundaryFlowValueRoute.TryReadProjectionWitness(
                routeSurface,
                new Carrier(),
                out var projectionWitness) ||
            !BoundaryFlowValueTransfer.TryReadTransferWitnesses(
                transferSurface,
                new Carrier(),
                out var acceptedWitness,
                out var blockedWitness) ||
            !BoundaryFlowTargetSettlement.TryRead(
                settlementSurface,
                out var targetBefore,
                out var settlementTransfer,
                out var targetAfter,
                out var settlementResidue,
                out _) ||
            !settlementTransfer.Equals(transferSurface) ||
            !BoundaryFlowTargetSettlement.TryReadWitness(
                settlementSurface,
                new Carrier(),
                out var settlementWitness))
        {
            timeline = null!;
            return false;
        }

        var time = new Carrier();
        var transferWitnesses = TransferWitnessGroup(acceptedWitness, blockedWitness);
        var projectionStep = TimeStepSequence.Step(
            new Carrier(),
            time,
            0,
            1,
            routeSurface,
            projectionWitness,
            transferSurface,
            routeResidue);
        var transferStep = TimeStepSequence.Step(
            new Carrier(),
            time,
            1,
            2,
            transferSurface,
            transferWitnesses,
            settlementSurface,
            blockedValue);
        var settlementStep = TimeStepSequence.Step(
            new Carrier(),
            time,
            2,
            3,
            targetBefore,
            settlementWitness,
            targetAfter,
            settlementResidue);

        timeline = TimeStepSequence.Create(
            new Carrier(),
            time,
            routeSurface,
            projectionStep,
            transferStep,
            settlementStep);
        return true;
    }

    private static Relation TransferWitnessGroup(Relation acceptedWitness, Relation blockedWitness)
    {
        var witnesses = new List<IElement>(2);
        if (acceptedWitness is not null)
        {
            witnesses.Add(acceptedWitness);
        }

        if (blockedWitness is not null)
        {
            witnesses.Add(blockedWitness);
        }

        return witnesses.Count == 0
            ? Relation.OrderedGroup(new Carrier(), ConceptSurface.UnknownPoint())
            : Relation.OrderedGroup(new Carrier(), [.. witnesses]);
    }
}

/// <summary>
/// Attaches a capacity witness to a specific target site and lane basis. The
/// capacity value is still ordinary point structure; this surface makes the
/// target-local attachment inspectable.
/// </summary>

