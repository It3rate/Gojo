using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Attaches a capacity witness to a specific target site and lane basis. The
/// capacity value is still ordinary point structure; this surface makes the
/// target-local attachment inspectable.
/// </summary>
public static class BoundaryFlowTargetCapacitySurface
{
    public static Relation Create(
        Carrier root,
        IElement targetSite,
        Relation targetBasis,
        Relation capacityPoint,
        IElement partition,
        IElement overflowWitness)
    {
        var site = targetSite ?? targetBasis ?? ConceptSurface.UnknownPoint();
        var capacity = ConceptCapacity.Create(
            new Carrier(),
            site,
            capacityPoint ?? ConceptSurface.UnknownPoint(),
            partition ?? ConceptSurface.UnknownPoint(),
            overflowWitness ?? ConceptSurface.UnknownPoint());

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            site,
            targetBasis ?? ConceptSurface.UnknownPoint(),
            capacity);
    }

    public static Relation Create(
        Carrier root,
        IElement targetSite,
        Relation targetBasis,
        Relation capacityPoint) =>
        Create(
            root,
            targetSite,
            targetBasis,
            capacityPoint,
            ConceptSurface.UnknownPoint(),
            ConceptSurface.UnknownPoint());

    public static bool TryCreate(
        Carrier root,
        Relation targetValue,
        long capacityTicks,
        out Relation targetCapacitySurface)
    {
        if (capacityTicks < 0 ||
            !BoundaryFlowValueSurface.TryReadValueWithBasis(
                targetValue,
                out var targetRead))
        {
            targetCapacitySurface = null!;
            return false;
        }

        targetCapacitySurface = Create(
            root,
            targetValue,
            targetRead.LaneBasis,
            Relation.Point(targetRead.Magnitude.Carrier, capacityTicks));
        return true;
    }

    public static bool TryCreatePartitioned(
        Carrier root,
        Relation targetValue,
        long capacityTicks,
        long partitionCount,
        IElement selectedPartition,
        out Relation targetCapacitySurface)
    {
        if (capacityTicks < 0 ||
            !BoundaryFlowValueSurface.TryReadValueWithBasis(
                targetValue,
                out var targetRead))
        {
            targetCapacitySurface = null!;
            return false;
        }

        var capacityPoint = Relation.Point(targetRead.Magnitude.Carrier, capacityTicks);
        var partition = ConceptPartition.Create(
            new Carrier(),
            capacityPoint,
            new Carrier(),
            partitionCount,
            selectedPartition ?? ConceptSurface.UnknownPoint());
        targetCapacitySurface = Create(
            root,
            targetValue,
            targetRead.LaneBasis,
            capacityPoint,
            partition,
            ConceptSurface.UnknownPoint());
        return true;
    }

    public static bool TryCreatePartitioned(
        Carrier root,
        Relation targetValue,
        long capacityTicks,
        long partitionCount,
        out Relation targetCapacitySurface) =>
        TryCreatePartitioned(
            root,
            targetValue,
            capacityTicks,
            partitionCount,
            ConceptSurface.UnknownPoint(),
            out targetCapacitySurface);

    public static bool TryRead(
        Relation surface,
        out IElement targetSite,
        out Relation targetBasis,
        out Relation capacitySurface,
        out Relation capacityPoint,
        out IElement partition,
        out IElement overflowWitness)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 3 &&
            elements[1] is Relation basis &&
            elements[2] is Relation capacity &&
            ConceptCapacity.TryRead(
                capacity,
                out _,
                out var wholeCapacity,
                out var partitionRead,
                out var overflowRead) &&
            wholeCapacity is Relation capacityRelation &&
            ConceptSurfaceReader.TryReadPoint(capacityRelation, out _))
        {
            targetSite = elements[0];
            targetBasis = basis;
            capacitySurface = capacity;
            capacityPoint = capacityRelation;
            partition = partitionRead;
            overflowWitness = overflowRead;
            return true;
        }

        targetSite = default!;
        targetBasis = null!;
        capacitySurface = null!;
        capacityPoint = null!;
        partition = default!;
        overflowWitness = default!;
        return false;
    }

    public static bool TryReadCapacity(
        Relation surface,
        out BoundaryFlowTargetCapacityRead read)
    {
        if (TryRead(
                surface,
                out _,
                out var targetBasis,
                out _,
                out var capacityPoint,
                out _,
                out _) &&
            ConceptSurfaceReader.TryReadPoint(capacityPoint, out var capacityLocation) &&
            BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out var exposure, out _) &&
            TryReadTargetCarrier(surface, out var targetCarrier))
        {
            read = new BoundaryFlowTargetCapacityRead(
                capacityLocation,
                targetCarrier,
                exposure);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadPartition(
        Relation surface,
        out Relation partition,
        out long partitionCount,
        out IElement selectedPartition)
    {
        if (TryRead(
                surface,
                out _,
                out _,
                out _,
                out _,
                out var partitionCandidate,
                out _) &&
            partitionCandidate is Relation partitionRelation &&
            ConceptPartition.TryRead(
                partitionRelation,
                out _,
                out _,
                out _,
                out var count,
                out var selected))
        {
            partition = partitionRelation;
            partitionCount = count.Ticks;
            selectedPartition = selected;
            return true;
        }

        partition = null!;
        partitionCount = 0;
        selectedPartition = default!;
        return false;
    }

    internal static bool TryReadTargetCarrier(Relation surface, out Carrier carrier)
    {
        if (!TryRead(
                surface,
                out var targetSite,
                out var targetBasis,
                out _,
                out var capacityPoint,
                out _,
                out _))
        {
            carrier = null!;
            return false;
        }

        if (targetSite is Relation targetValue &&
            BoundaryFlowValueSurface.TryReadValue(targetValue, targetBasis, out var targetRead))
        {
            carrier = targetRead.Magnitude.Carrier;
            return true;
        }

        if (CarrierAxisSurface.TryRead(targetBasis, out var axisCarrier, out _, out _, out _, out _))
        {
            carrier = axisCarrier;
            return true;
        }

        if (targetSite is Carrier selectedCarrier)
        {
            carrier = selectedCarrier;
            return true;
        }

        if (ConceptSurfaceReader.TryReadPoint(capacityPoint, out var capacityLocation))
        {
            carrier = capacityLocation.Carrier;
            return true;
        }

        carrier = null!;
        return false;
    }
}


public readonly record struct BoundaryFlowTargetCapacityRead(
    CarrierLocation Capacity,
    Carrier TargetCarrier,
    BoundaryFlowExposure TargetBasis);

/// <summary>
/// Reads usable capacity from a target capacity surface and the target's
/// current value. Whole capacity, occupied value, remaining capacity, and
/// overflow stay visible as ordinary connected structure.
/// </summary>

/// <summary>
/// Reads usable capacity from a target capacity surface and the target's
/// current value. Whole capacity, occupied value, remaining capacity, and
/// overflow stay visible as ordinary connected structure.
/// </summary>
public static class BoundaryFlowRemainingCapacitySurface
{
    public static Relation Create(
        Carrier root,
        Relation targetCapacity,
        Relation targetValue,
        Relation remainingCapacity,
        Relation overflow,
        Relation effectiveCapacity,
        Relation partitionResidue)
    {
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            targetCapacity ?? ConceptSurface.UnknownPoint(),
            targetValue ?? ConceptSurface.UnknownPoint(),
            remainingCapacity ?? ConceptSurface.UnknownPoint(),
            overflow ?? ConceptSurface.UnknownPoint(),
            effectiveCapacity ?? ConceptSurface.UnknownPoint(),
            partitionResidue ?? ConceptSurface.UnknownPoint());
    }

    public static Relation Create(
        Carrier root,
        Relation targetCapacity,
        Relation targetValue,
        Relation remainingCapacity,
        Relation overflow) =>
        Create(
            root,
            targetCapacity,
            targetValue,
            remainingCapacity,
            overflow,
            remainingCapacity,
            ConceptSurface.UnknownPoint());

    public static bool TryCreate(
        Carrier root,
        Relation targetCapacity,
        Relation targetValue,
        out Relation remainingCapacitySurface)
    {
        if (!BoundaryFlowTargetCapacitySurface.TryReadCapacity(targetCapacity, out var capacityRead) ||
            !BoundaryFlowValueSurface.TryReadValue(targetValue, out var targetRead) ||
            !targetRead.IsLegal ||
            !targetRead.IsUnsignedMagnitude ||
            !targetRead.Basis.Equals(capacityRead.TargetBasis) ||
            !targetRead.Magnitude.Carrier.SameIdentityAs(capacityRead.TargetCarrier))
        {
            remainingCapacitySurface = null!;
            return false;
        }

        var occupiedTicks = targetRead.Magnitude.Ticks;
        var effectiveCapacity = EffectiveCapacityOf(targetCapacity, capacityRead.Capacity);
        var remainingTicks = Math.Max(0, checked(effectiveCapacity.Capacity.Ticks - occupiedTicks));
        var overflowTicks = Math.Max(0, checked(occupiedTicks - effectiveCapacity.Capacity.Ticks));
        remainingCapacitySurface = Create(
            root,
            targetCapacity,
            targetValue,
            Relation.Point(effectiveCapacity.Capacity.Carrier, remainingTicks),
            Relation.Point(effectiveCapacity.Capacity.Carrier, overflowTicks),
            Relation.Point(effectiveCapacity.Capacity.Carrier, effectiveCapacity.Capacity.Ticks),
            Relation.Point(effectiveCapacity.Residue.Carrier, effectiveCapacity.Residue.Ticks));
        return true;
    }

    public static bool TryRead(
        Relation surface,
        out Relation targetCapacity,
        out Relation targetValue,
        out Relation remainingCapacity,
        out Relation overflow)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 4 &&
            elements[0] is Relation capacity &&
            elements[1] is Relation value &&
            elements[2] is Relation remaining &&
            elements[3] is Relation overflowRelation &&
            BoundaryFlowTargetCapacitySurface.TryReadCapacity(capacity, out _) &&
            BoundaryFlowValueSurface.TryReadValue(value, out _) &&
            ConceptSurfaceReader.TryReadPoint(remaining, out _) &&
            ConceptSurfaceReader.TryReadPoint(overflowRelation, out _))
        {
            targetCapacity = capacity;
            targetValue = value;
            remainingCapacity = remaining;
            overflow = overflowRelation;
            return true;
        }

        targetCapacity = null!;
        targetValue = null!;
        remainingCapacity = null!;
        overflow = null!;
        return false;
    }

    public static bool TryReadEffectiveCapacity(
        Relation surface,
        out Relation effectiveCapacity,
        out Relation partitionResidue)
    {
        if (ConceptSurfaceReader.TryReadRootElements(surface, out var elements) &&
            elements.Length >= 6 &&
            elements[4] is Relation effective &&
            elements[5] is Relation residue &&
            ConceptSurfaceReader.TryReadPoint(effective, out _) &&
            ConceptSurfaceReader.TryReadPoint(residue, out _))
        {
            effectiveCapacity = effective;
            partitionResidue = residue;
            return true;
        }

        if (TryRead(
                surface,
                out var targetCapacity,
                out _,
                out _,
                out _) &&
            BoundaryFlowTargetCapacitySurface.TryReadCapacity(targetCapacity, out var capacityRead))
        {
            effectiveCapacity = Relation.Point(capacityRead.Capacity.Carrier, capacityRead.Capacity.Ticks);
            partitionResidue = Relation.Point(capacityRead.Capacity.Carrier, 0);
            return true;
        }

        effectiveCapacity = null!;
        partitionResidue = null!;
        return false;
    }

    public static bool TryReadCapacity(
        Relation surface,
        out BoundaryFlowRemainingCapacityRead read)
    {
        if (TryRead(
                surface,
                out var targetCapacity,
                out var targetValue,
                out var remainingCapacity,
                out var overflow) &&
            BoundaryFlowTargetCapacitySurface.TryReadCapacity(targetCapacity, out var capacityRead) &&
            BoundaryFlowValueSurface.TryReadValue(targetValue, out var targetRead) &&
            ConceptSurfaceReader.TryReadPoint(remainingCapacity, out var remainingLocation) &&
            ConceptSurfaceReader.TryReadPoint(overflow, out var overflowLocation))
        {
            var effective = TryReadEffectiveCapacity(
                    surface,
                    out var effectiveCapacity,
                    out var partitionResidue) &&
                ConceptSurfaceReader.TryReadPoint(effectiveCapacity, out var effectiveLocation) &&
                ConceptSurfaceReader.TryReadPoint(partitionResidue, out var residueLocation)
                    ? new EffectiveCapacityRead(effectiveLocation, residueLocation)
                    : EffectiveCapacityOf(targetCapacity, capacityRead.Capacity);

            read = new BoundaryFlowRemainingCapacityRead(
                capacityRead.Capacity,
                effective.Capacity,
                targetRead.Magnitude,
                remainingLocation,
                overflowLocation,
                effective.Residue,
                capacityRead.TargetCarrier,
                capacityRead.TargetBasis);
            return true;
        }

        read = default;
        return false;
    }

    private static EffectiveCapacityRead EffectiveCapacityOf(
        Relation targetCapacity,
        CarrierLocation wholeCapacity)
    {
        if (!BoundaryFlowTargetCapacitySurface.TryReadPartition(
                targetCapacity,
                out _,
                out var partitionCount,
                out var selectedPartition) ||
            partitionCount <= 0 ||
            !ConceptSurfaceReader.TryReadPoint(selectedPartition, out var selected))
        {
            return new EffectiveCapacityRead(
                wholeCapacity,
                new CarrierLocation(wholeCapacity.Carrier, 0));
        }

        var selectedTicks = Math.Clamp(selected.Ticks, 0, partitionCount);
        var scaled = checked(wholeCapacity.Ticks * selectedTicks);
        var effectiveTicks = scaled / partitionCount;
        var residueTicks = scaled % partitionCount;
        return new EffectiveCapacityRead(
            new CarrierLocation(wholeCapacity.Carrier, effectiveTicks),
            new CarrierLocation(selected.Carrier, residueTicks));
    }
}


public readonly record struct BoundaryFlowRemainingCapacityRead(
    CarrierLocation WholeCapacity,
    CarrierLocation EffectiveCapacity,
    CarrierLocation Occupied,
    CarrierLocation Remaining,
    CarrierLocation Overflow,
    CarrierLocation PartitionResidue,
    Carrier TargetCarrier,
    BoundaryFlowExposure TargetBasis);


internal readonly record struct EffectiveCapacityRead(
    CarrierLocation Capacity,
    CarrierLocation Residue);

/// <summary>
/// Accepts several projected routes against one shared capacity witness. Legal
/// projected lanes compete for capacity; illegal lanes bypass capacity and
/// remain blocked as lane residue.
/// </summary>

/// <summary>
/// Accepts several projected routes against one shared capacity witness. Legal
/// projected lanes compete for capacity; illegal lanes bypass capacity and
/// remain blocked as lane residue.
/// </summary>
public static class BoundaryFlowSharedCapacityTransfer
{
    public static Relation Create(
        Carrier root,
        Relation capacity,
        Relation routes,
        Relation transfers,
        Relation residues,
        Relation unusedCapacity)
    {
        var capacitySurface = capacity ?? ConceptSurface.UnknownPoint();
        var routeGroup = routes ?? ConceptSurface.UnknownPoint();
        var transferGroup = transfers ?? ConceptSurface.UnknownPoint();
        var residueGroup = residues ?? ConceptSurface.UnknownPoint();
        var unused = unusedCapacity ?? ConceptSurface.UnknownPoint();
        var allocationWitness = CreateAllocationWitness(
            capacitySurface,
            routeGroup,
            transferGroup,
            residueGroup,
            unused,
            Relation.OrderedGroup(new Carrier(), capacitySurface, routeGroup, residueGroup, unused));
        var timeline = CreateTimeline(
            capacitySurface,
            routeGroup,
            transferGroup,
            residueGroup,
            unused,
            allocationWitness);

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            capacitySurface,
            routeGroup,
            transferGroup,
            residueGroup,
            unused,
            allocationWitness,
            timeline);
    }

    public static bool TryAccept(
        Relation capacity,
        IReadOnlyList<Relation> routeSurfaces,
        out Relation sharedTransferSurface)
    {
        if (!TryReadCapacityWitness(
                capacity,
                out var capacityLocation,
                out var targetBasis,
                out var targetCarrier) ||
            capacityLocation.Ticks < 0)
        {
            sharedTransferSurface = null!;
            return false;
        }

        var routes = CleanRelations(routeSurfaces);
        var projectedReads = new BoundaryFlowValueRead[routes.Length];
        var demands = new long[routes.Length];
        for (var index = 0; index < routes.Length; index++)
        {
            if (targetBasis is not null &&
                !IsRouteLocalToTargetCapacity(routes[index], targetBasis, targetCarrier))
            {
                sharedTransferSurface = null!;
                return false;
            }

            if (!BoundaryFlowValueRoute.TryReadTargetValue(routes[index], out var projectedRead) ||
                !projectedRead.IsUnsignedMagnitude)
            {
                sharedTransferSurface = null!;
                return false;
            }

            projectedReads[index] = projectedRead;
            demands[index] = projectedRead.IsLegal ? projectedRead.Magnitude.Ticks : 0;
        }

        var accepted = AllocateSharedCapacity(demands, capacityLocation.Ticks);
        var transfers = new Relation[routes.Length];
        var residues = new IElement[routes.Length];
        var acceptedTotal = 0L;

        for (var index = 0; index < routes.Length; index++)
        {
            var routeCapacity = Relation.Point(projectedReads[index].Magnitude.Carrier, accepted[index]);
            if (!BoundaryFlowValueTransfer.TryAccept(routes[index], routeCapacity, out var transfer) ||
                !BoundaryFlowValueTransfer.TryRead(
                    transfer,
                    out _,
                    out _,
                    out var blocked,
                    out _))
            {
                sharedTransferSurface = null!;
                return false;
            }

            transfers[index] = transfer;
            residues[index] = blocked;
            acceptedTotal = checked(acceptedTotal + accepted[index]);
        }

        var unusedTicks = Math.Max(0, checked(capacityLocation.Ticks - acceptedTotal));
        sharedTransferSurface = Create(
            new Carrier(),
            capacity,
            Relation.OrderedGroup(new Carrier(), routes),
            Relation.OrderedGroup(new Carrier(), transfers),
            Relation.OrderedGroup(new Carrier(), residues),
            Relation.Point(capacityLocation.Carrier, unusedTicks));
        return true;
    }

    public static bool TryRead(
        Relation sharedTransferSurface,
        out Relation capacity,
        out Relation routes,
        out Relation transfers,
        out Relation residues,
        out Relation unusedCapacity)
    {
        if (ConceptSurfaceReader.TryReadRootElements(sharedTransferSurface, out var elements) &&
            elements.Length >= 5 &&
            elements[0] is Relation capacityRelation &&
            elements[1] is Relation routeGroup &&
            elements[2] is Relation transferGroup &&
            elements[3] is Relation residueGroup &&
            elements[4] is Relation unused)
        {
            capacity = capacityRelation;
            routes = routeGroup;
            transfers = transferGroup;
            residues = residueGroup;
            unusedCapacity = unused;
            return true;
        }

        capacity = null!;
        routes = null!;
        transfers = null!;
        residues = null!;
        unusedCapacity = null!;
        return false;
    }

    public static bool TryReadTransfers(
        Relation sharedTransferSurface,
        out Relation[] transfers)
    {
        if (TryRead(sharedTransferSurface, out _, out _, out var transferGroup, out _, out _) &&
            ConceptSurfaceReader.TryReadRootElements(transferGroup, out var elements))
        {
            transfers = [.. elements.OfType<Relation>()];
            return true;
        }

        transfers = [];
        return false;
    }

    public static bool TryReadSummaries(
        Relation sharedTransferSurface,
        out BoundaryFlowTransferRead[] summaries)
    {
        if (!TryReadTransfers(sharedTransferSurface, out var transfers))
        {
            summaries = [];
            return false;
        }

        var reads = new BoundaryFlowTransferRead[transfers.Length];
        for (var index = 0; index < transfers.Length; index++)
        {
            if (!BoundaryFlowValueTransfer.TryReadSummary(transfers[index], out reads[index]))
            {
                summaries = [];
                return false;
            }
        }

        summaries = reads;
        return true;
    }

    public static bool TryReadTotals(
        Relation sharedTransferSurface,
        out BoundaryFlowSharedCapacityRead read)
    {
        if (!TryRead(
                sharedTransferSurface,
                out var capacity,
                out _,
                out _,
                out _,
                out var unusedCapacity) ||
            !TryReadCapacityLocation(capacity, out var capacityLocation) ||
            !ConceptSurfaceReader.TryReadPoint(unusedCapacity, out var unusedLocation) ||
            !TryReadSummaries(sharedTransferSurface, out var summaries))
        {
            read = default;
            return false;
        }

        var accepted = 0L;
        var blocked = 0L;
        foreach (var summary in summaries)
        {
            if (summary.HasAccepted)
            {
                accepted = checked(accepted + summary.Accepted.Magnitude.Ticks);
            }

            blocked = checked(blocked + summary.Blocked.Magnitude.Ticks);
        }

        read = new BoundaryFlowSharedCapacityRead(
            capacityLocation.Ticks,
            accepted,
            blocked,
            unusedLocation.Ticks,
            summaries.Length);
        return true;
    }

    public static bool TryReadAllocationWitness(
        Relation sharedTransferSurface,
        Carrier root,
        out Relation witness)
    {
        if (ReadWitness.TryReadEmbedded(sharedTransferSurface, index: 5, out witness))
        {
            return true;
        }

        if (TryRead(
                sharedTransferSurface,
                out var capacity,
                out var routes,
                out var transfers,
                out var residues,
                out var unusedCapacity))
        {
            witness = CreateAllocationWitness(
                capacity,
                routes,
                transfers,
                residues,
                unusedCapacity,
                sharedTransferSurface,
                root);
            return true;
        }

        witness = null!;
        return false;
    }

    public static bool TryReadTimeline(
        Relation sharedTransferSurface,
        out Relation timeline)
    {
        if (ConceptSurfaceReader.TryReadRootElements(sharedTransferSurface, out var elements) &&
            elements.Length >= 7 &&
            elements[6] is Relation embeddedTimeline &&
            TimeStepSequence.TryRead(embeddedTimeline, out _, out _, out _))
        {
            timeline = embeddedTimeline;
            return true;
        }

        if (TryRead(
                sharedTransferSurface,
                out var capacity,
                out var routes,
                out var transfers,
                out var residues,
                out var unusedCapacity))
        {
            var allocationWitness = CreateAllocationWitness(
                capacity,
                routes,
                transfers,
                residues,
                unusedCapacity,
                sharedTransferSurface);
            timeline = CreateTimeline(
                capacity,
                routes,
                transfers,
                residues,
                unusedCapacity,
                allocationWitness);
            return true;
        }

        timeline = null!;
        return false;
    }

    private static bool TryReadCapacityWitness(
        Relation capacity,
        out CarrierLocation capacityLocation,
        out Relation? targetBasis,
        out Carrier? targetCarrier)
    {
        if (BoundaryFlowRemainingCapacitySurface.TryReadCapacity(
                capacity,
                out var remainingRead))
        {
            capacityLocation = remainingRead.Remaining;
            targetBasis = BoundaryFlowRemainingCapacitySurface.TryRead(
                    capacity,
                    out var targetCapacity,
                    out _,
                    out _,
                    out _) &&
                BoundaryFlowTargetCapacitySurface.TryRead(
                    targetCapacity,
                    out _,
                    out var remainingBasis,
                    out _,
                    out _,
                    out _,
                    out _)
                    ? remainingBasis
                    : null;
            targetCarrier = remainingRead.TargetCarrier;
            return true;
        }

        if (ConceptSurfaceReader.TryReadPoint(capacity, out capacityLocation))
        {
            targetBasis = null;
            targetCarrier = null;
            return true;
        }

        if (BoundaryFlowTargetCapacitySurface.TryRead(
                capacity,
                out _,
                out var basis,
                out _,
                out var capacityPoint,
                out _,
                out _) &&
            ConceptSurfaceReader.TryReadPoint(capacityPoint, out capacityLocation))
        {
            targetBasis = basis;
            targetCarrier = BoundaryFlowTargetCapacitySurface.TryReadTargetCarrier(
                capacity,
                out var carrier)
                ? carrier
                : null;
            return true;
        }

        capacityLocation = default;
        targetBasis = null;
        targetCarrier = null;
        return false;
    }

    private static bool TryReadCapacityLocation(
        Relation capacity,
        out CarrierLocation capacityLocation)
    {
        if (BoundaryFlowRemainingCapacitySurface.TryReadCapacity(
                capacity,
                out var remainingRead))
        {
            capacityLocation = remainingRead.Remaining;
            return true;
        }

        if (ConceptSurfaceReader.TryReadPoint(capacity, out capacityLocation))
        {
            return true;
        }

        if (BoundaryFlowTargetCapacitySurface.TryRead(
                capacity,
                out _,
                out _,
                out _,
                out var capacityPoint,
                out _,
                out _) &&
            ConceptSurfaceReader.TryReadPoint(capacityPoint, out capacityLocation))
        {
            return true;
        }

        capacityLocation = default;
        return false;
    }

    private static bool IsRouteLocalToTargetCapacity(
        Relation routeSurface,
        Relation targetBasis,
        Carrier? targetCarrier)
    {
        if (!BoundaryFlowValueRoute.TryRead(
                routeSurface,
                out _,
                out _,
                out var routeTargetBasis,
                out var routeTargetValue,
                out _) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(targetBasis, out var targetExposure, out _) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(routeTargetBasis, out var routeExposure, out _) ||
            !targetExposure.Equals(routeExposure))
        {
            return false;
        }

        if (targetCarrier is null || targetCarrier.IsUnknown)
        {
            return true;
        }

        return BoundaryFlowValueSurface.TryReadValue(
                routeTargetValue,
                routeTargetBasis,
                out var routeTargetRead) &&
            routeTargetRead.Magnitude.Carrier.SameIdentityAs(targetCarrier);
    }

    private static long[] AllocateSharedCapacity(IReadOnlyList<long> demands, long capacity)
    {
        var accepted = new long[demands.Count];
        var remaining = Math.Max(0, capacity);
        var active = Enumerable.Range(0, demands.Count)
            .Where(index => demands[index] > 0)
            .ToList();

        while (remaining > 0 && active.Count != 0)
        {
            var count = active.Count;
            var baseShare = remaining / count;
            var extra = remaining % count;
            var moved = 0L;

            for (var order = 0; order < active.Count; order++)
            {
                var index = active[order];
                var demandLeft = demands[index] - accepted[index];
                var offer = baseShare + (order < extra ? 1 : 0);
                var acceptedTicks = Math.Min(demandLeft, offer);
                accepted[index] = checked(accepted[index] + acceptedTicks);
                moved = checked(moved + acceptedTicks);
            }

            if (moved == 0)
            {
                break;
            }

            remaining = checked(remaining - moved);
            active.RemoveAll(index => accepted[index] >= demands[index]);
        }

        return accepted;
    }

    private static Relation CreateAllocationWitness(
        Relation capacity,
        Relation routes,
        Relation transfers,
        Relation residues,
        Relation unusedCapacity,
        IElement provenance,
        Carrier root = null!) =>
        ReadWitness.Create(
            ConceptSurface.RootOrNew(root),
            routes,
            capacity,
            transfers,
            Relation.OrderedGroup(new Carrier(), unusedCapacity, residues),
            provenance ?? ConceptSurface.UnknownPoint());

    private static Relation CreateTimeline(
        Relation capacity,
        Relation routes,
        Relation transfers,
        Relation residues,
        Relation unusedCapacity,
        Relation allocationWitness)
    {
        var time = new Carrier();
        var routeElements = ReadGroupElements(routes);
        var transferElements = ReadGroupElements(transfers);
        var residueElements = ReadGroupElements(residues);
        var count = Math.Max(routeElements.Length, transferElements.Length);
        var steps = new List<Relation>(count + 1)
        {
            TimeStepSequence.Step(
                new Carrier(),
                time,
                0,
                1,
                routes,
                allocationWitness,
                transfers,
                Relation.OrderedGroup(new Carrier(), unusedCapacity, residues))
        };

        for (var index = 0; index < count; index++)
        {
            var before = ElementOrUnknown(routeElements, index);
            var after = ElementOrUnknown(transferElements, index);
            var residue = ElementOrUnknown(residueElements, index);
            steps.Add(TimeStepSequence.Step(
                new Carrier(),
                time,
                index + 1,
                index + 2,
                before,
                TransferWitnessGroup(after),
                after,
                residue));
        }

        return TimeStepSequence.Create(new Carrier(), time, routes, [.. steps]);
    }

    private static Relation TransferWitnessGroup(IElement transfer)
    {
        if (transfer is not Relation transferSurface ||
            !BoundaryFlowValueTransfer.TryReadTransferWitnesses(
                transferSurface,
                new Carrier(),
                out var acceptedWitness,
                out var blockedWitness))
        {
            return Relation.OrderedGroup(new Carrier(), ConceptSurface.UnknownPoint());
        }

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

    private static IElement[] ReadGroupElements(Relation group) =>
        ConceptSurfaceReader.TryReadRootElements(group, out var elements)
            ? elements
            : [];

    private static IElement ElementOrUnknown(IElement[] elements, int index) =>
        index >= 0 && index < elements.Length
            ? elements[index]
            : ConceptSurface.UnknownPoint();

    private static Relation[] CleanRelations(IReadOnlyList<Relation> relations)
    {
        if (relations is null || relations.Count == 0)
        {
            return [];
        }

        return [.. relations.Where(relation => relation is not null)];
    }
}


public readonly record struct BoundaryFlowSharedCapacityRead(
    long CapacityMagnitude,
    long AcceptedMagnitude,
    long BlockedMagnitude,
    long UnusedCapacity,
    int TransferCount);

/// <summary>
/// Applies an accepted transfer to an existing target value and records the
/// transition as a time-step witness. This is still a concept-surface
/// settlement prototype; it does not mutate carrier state.
/// </summary>

