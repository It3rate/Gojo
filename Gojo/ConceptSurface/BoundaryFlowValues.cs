using Gojo.Core;
using Gojo.Structures;

namespace Gojo.ConceptSurface;

/// <summary>
/// Attaches an unsigned magnitude point to one occupied boundary/flow lane.
/// The magnitude carrier's unit shape normally supplies possible lanes; this
/// structure describes where this value actually sits.
/// </summary>
public static class BoundaryFlowValueSurface
{
    /// <summary>
    /// Creates a value with an explicit basis witness. Prefer
    /// <see cref="CreateOccupied(Carrier,Carrier,ulong,BoundaryFlowLane)"/> for
    /// ordinary carrier-backed values whose possible lanes live on UnitShape.
    /// </summary>
    public static Relation Create(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        IElement laneBasis,
        IElement residueOrTension)
    {
        var carrier = valueCarrier ?? Carrier.Unknown;
        var point = Relation.Point(carrier, checked((long)magnitude));
        return Create(root, point, lane, laneBasis, residueOrTension);
    }

    public static Relation Create(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        IElement laneBasis) =>
        Create(root, valueCarrier, magnitude, lane, laneBasis, ConceptSurface.UnknownPoint());

    /// <summary>
    /// Creates the normal occupied value shape: magnitude plus occupied lane,
    /// with possible lanes resolved from the magnitude carrier's unit shape.
    /// </summary>
    public static Relation CreateOccupied(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        IElement residueOrTension)
    {
        var carrier = valueCarrier ?? Carrier.Unknown;
        var point = Relation.Point(carrier, checked((long)magnitude));
        return CreateOccupied(root, point, lane, residueOrTension);
    }

    public static Relation CreateOccupied(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane) =>
        CreateOccupied(root, valueCarrier, magnitude, lane, ConceptSurface.UnknownPoint());

    public static Relation CreateOccupied(
        Carrier root,
        Relation magnitudePoint,
        BoundaryFlowLane lane,
        IElement residueOrTension)
    {
        var marker = BoundaryFlowLaneSurface.Create(new Carrier(), lane);
        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            magnitudePoint ?? ConceptSurface.UnknownPoint(),
            marker,
            ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint());
    }

    /// <summary>
    /// Creates a value with an explicit basis witness or route/target context.
    /// This is not the normal home for possible lanes.
    /// </summary>
    public static Relation Create(
        Carrier root,
        Relation magnitudePoint,
        BoundaryFlowLane lane,
        IElement laneBasis,
        IElement residueOrTension)
    {
        var marker = BoundaryFlowLaneSurface.Create(
            new Carrier(),
            lane,
            laneBasis ?? ConceptSurface.UnknownPoint(),
            laneBasis ?? ConceptSurface.UnknownPoint());

        return Relation.OrderedGroup(
            ConceptSurface.RootOrNew(root),
            magnitudePoint ?? ConceptSurface.UnknownPoint(),
            marker,
            laneBasis ?? ConceptSurface.UnknownPoint(),
            residueOrTension ?? ConceptSurface.UnknownPoint());
    }

    public static bool TryRead(
        Relation valueSurface,
        out CarrierLocation magnitude,
        out BoundaryFlowLane lane,
        out Relation laneMarker,
        out IElement laneBasis,
        out IElement residueOrTension)
    {
        if (ConceptSurfaceReader.TryReadRootElements(valueSurface, out var elements) &&
            elements.Length >= 4 &&
            ConceptSurfaceReader.TryReadPoint(elements[0], out var magnitudeRead) &&
            elements[1] is Relation laneSurface &&
            BoundaryFlowLaneSurface.TryRead(laneSurface, out var laneRead, out _, out _))
        {
            magnitude = magnitudeRead;
            lane = laneRead;
            laneMarker = laneSurface;
            laneBasis = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        magnitude = default;
        lane = default;
        laneMarker = null!;
        laneBasis = default!;
        residueOrTension = default!;
        return false;
    }

    public static bool TryReadOccupation(
        Relation valueSurface,
        Relation laneBasis,
        out CarrierLocation magnitude,
        out BoundaryFlowLane lane,
        out bool isLegal)
    {
        if (!TryRead(valueSurface, out magnitude, out lane, out _, out _, out _) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(laneBasis, out _, out var lanes))
        {
            isLegal = false;
            return false;
        }

        isLegal = lanes.Contains(lane);
        return true;
    }

    public static bool TryReadValue(
        Relation valueSurface,
        out BoundaryFlowValueRead read)
    {
        if (TryReadValueWithBasis(valueSurface, out var basisRead))
        {
            read = basisRead.ToCompactRead();
            return true;
        }

        read = default;
        return false;
    }

    /// <summary>
    /// Reads a value through possible lanes resolved from the carrier unit-shape
    /// descriptor, then explicit basis witness, then the unit-shape default.
    /// </summary>
    public static bool TryReadValue(
        Relation valueSurface,
        Relation laneBasis,
        out BoundaryFlowValueRead read)
    {
        if (TryReadValueWithBasis(valueSurface, laneBasis, out var basisRead))
        {
            read = basisRead.ToCompactRead();
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadValueWithBasis(
        Relation valueSurface,
        out BoundaryFlowValueBasisRead read)
    {
        if (!TryRead(valueSurface, out var magnitude, out var lane, out _, out var basisElement, out var residueOrTension) ||
            !TryResolveLaneBasis(magnitude, basisElement, out var laneBasis, out var exposure, out var lanes))
        {
            read = default;
            return false;
        }

        read = new BoundaryFlowValueBasisRead(
            magnitude,
            lane,
            laneBasis,
            exposure,
            lanes,
            lanes.Contains(lane),
            residueOrTension);
        return true;
    }

    public static bool TryReadValueWithBasis(
        Relation valueSurface,
        Relation laneBasis,
        out BoundaryFlowValueBasisRead read)
    {
        if (!TryRead(valueSurface, out var magnitude, out var lane, out _, out _, out var residueOrTension) ||
            !BoundaryFlowLaneBasis.TryReadLegalLanes(laneBasis, out var exposure, out var lanes))
        {
            read = default;
            return false;
        }

        read = new BoundaryFlowValueBasisRead(
            magnitude,
            lane,
            laneBasis,
            exposure,
            lanes,
            lanes.Contains(lane),
            residueOrTension);
        return true;
    }

    /// <summary>
    /// Reads a value's occupied lane against its resolved possible-lane basis
    /// as the common axis/face/lane basis-read surface.
    /// </summary>
    public static bool TryReadBasisRead(
        Relation valueSurface,
        Carrier root,
        out Relation basisRead)
    {
        if (ConceptSurfaceReader.TryReadRootElements(valueSurface, out var elements) &&
            elements.Length >= 2 &&
            elements[1] is Relation laneMarker &&
            TryReadValueWithBasis(valueSurface, out var read))
        {
            basisRead = BasisReadSurface.Create(
                ConceptSurface.RootOrNew(root),
                valueSurface,
                laneMarker,
                read.LaneBasis,
                elements[0],
                read.ResidueOrTension,
                valueSurface);
            return true;
        }

        basisRead = null!;
        return false;
    }

    public static bool TryCreateInferred(
        Carrier root,
        Relation magnitudePoint,
        Relation laneBasis,
        out Relation valueSurface)
    {
        if (!ConceptSurfaceReader.TryReadPoint(magnitudePoint, out _) ||
            !BoundaryFlowLaneBasis.TryInferSingleLane(laneBasis, out var lane))
        {
            valueSurface = null!;
            return false;
        }

        valueSurface = Create(
            ConceptSurface.RootOrNew(root),
            magnitudePoint,
            lane,
            laneBasis,
            ConceptSurface.UnknownPoint());
        return true;
    }

    public static bool TryCreateFromCarrierUnitShape(
        Carrier root,
        Carrier valueCarrier,
        ulong magnitude,
        BoundaryFlowLane lane,
        out Relation valueSurface)
    {
        if (valueCarrier is null ||
            !BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
                valueCarrier.UnitShape,
                out _,
                out _,
                out _,
                out var lanes) ||
            !lanes.Contains(lane))
        {
            valueSurface = null!;
            return false;
        }

        valueSurface = CreateOccupied(
            ConceptSurface.RootOrNew(root),
            valueCarrier,
            magnitude,
            lane);
        return true;
    }

    private static bool TryResolveLaneBasis(
        CarrierLocation magnitude,
        IElement explicitBasis,
        out Relation laneBasis,
        out BoundaryFlowExposure exposure,
        out BoundaryFlowLane[] lanes)
    {
        if (BoundaryFlowLaneBasis.TryReadUnitShapeDescriptorBasis(
                magnitude.Carrier.UnitShape,
                out laneBasis,
                out exposure,
                out _,
                out lanes))
        {
            return true;
        }

        if (explicitBasis is Relation basisRelation &&
            BoundaryFlowLaneBasis.TryReadLegalLanes(basisRelation, out exposure, out lanes))
        {
            laneBasis = basisRelation;
            return true;
        }

        if (BoundaryFlowLaneBasis.TryReadUnitShapeBasis(
                magnitude.Carrier.UnitShape,
                out laneBasis,
                out exposure,
                out _,
                out lanes))
        {
            return true;
        }

        laneBasis = null!;
        exposure = default;
        lanes = [];
        return false;
    }
}

/// <summary>
/// Compact read of a value surface through its resolved possible-lane basis.
/// The raw magnitude and occupied lane remain visible in the source structure.
/// </summary>

/// <summary>
/// Compact read of a value surface through its resolved possible-lane basis.
/// The raw magnitude and occupied lane remain visible in the source structure.
/// </summary>
public readonly record struct BoundaryFlowValueRead(
    CarrierLocation Magnitude,
    BoundaryFlowLane Lane,
    BoundaryFlowExposure Basis,
    bool IsLegal,
    IElement ResidueOrTension)
{
    public bool IsUnsignedMagnitude => Magnitude.Ticks >= 0;

    public bool IsSignedBoundary => Lane.Boundary.IsPositive || Lane.Boundary.IsNegative;

    public bool IsWholeBoundary => Lane.Boundary.IsWhole;

    public bool IsNoFlow => Lane.Flow.IsNone;

    public bool IsForwardFlow => Lane.Flow.IsForward;

    public bool IsInverseFlow => Lane.Flow.IsInverse;

    public long SignedTicks => Lane.Boundary.IsNegative
        ? checked(-Magnitude.Ticks)
        : Magnitude.Ticks;

    public string Notation
    {
        get
        {
            var sign = Lane.Boundary.IsNegative ? "-" : string.Empty;
            var suffix = Lane.Flow.IsInverse
                ? "i"
                : Lane.Flow.IsForward
                    ? "f"
                    : Lane.Boundary.IsWhole
                        ? "s"
                        : string.Empty;

            return $"{sign}{Magnitude.Ticks}{suffix}";
        }
    }
}

/// <summary>
/// Structural read of a value surface through its resolved possible-lane basis.
/// This keeps the physical basis surface and visible legal lanes available
/// alongside the compact exposure read.
/// </summary>

/// <summary>
/// Structural read of a value surface through its resolved possible-lane basis.
/// This keeps the physical basis surface and visible legal lanes available
/// alongside the compact exposure read.
/// </summary>
public readonly record struct BoundaryFlowValueBasisRead(
    CarrierLocation Magnitude,
    BoundaryFlowLane Lane,
    Relation LaneBasis,
    BoundaryFlowExposure Basis,
    BoundaryFlowLane[] LegalLanes,
    bool IsLegal,
    IElement ResidueOrTension)
{
    public BoundaryFlowValueRead ToCompactRead() =>
        new(Magnitude, Lane, Basis, IsLegal, ResidueOrTension);

    public bool IsUnsignedMagnitude => ToCompactRead().IsUnsignedMagnitude;

    public bool IsSignedBoundary => ToCompactRead().IsSignedBoundary;

    public bool IsWholeBoundary => ToCompactRead().IsWholeBoundary;

    public bool IsNoFlow => ToCompactRead().IsNoFlow;

    public bool IsForwardFlow => ToCompactRead().IsForwardFlow;

    public bool IsInverseFlow => ToCompactRead().IsInverseFlow;

    public long SignedTicks => ToCompactRead().SignedTicks;

    public string Notation => ToCompactRead().Notation;
}

/// <summary>
/// Small lane-settlement experiment. It applies a signed delta to the visible
/// value read without changing core carrier settlement.
/// </summary>

/// <summary>
/// Small lane-settlement experiment. It applies a signed delta to the visible
/// value read without changing core carrier settlement.
/// </summary>
public static class BoundaryFlowValueSettlement
{
    public static bool TryApplySignedDelta(
        Relation valueSurface,
        long deltaTicks,
        out Relation settledValue)
    {
        if (!BoundaryFlowValueSurface.TryReadValueWithBasis(valueSurface, out var basisRead))
        {
            settledValue = null!;
            return false;
        }

        return TryApplySignedDelta(valueSurface, basisRead.LaneBasis, deltaTicks, out settledValue);
    }

    public static bool TryApplySignedDelta(
        Relation valueSurface,
        Relation laneBasis,
        long deltaTicks,
        out Relation settledValue)
    {
        if (!BoundaryFlowValueSurface.TryReadValue(valueSurface, laneBasis, out var read) ||
            !read.IsUnsignedMagnitude ||
            !read.IsSignedBoundary)
        {
            settledValue = null!;
            return false;
        }

        var target = checked(read.SignedTicks + deltaTicks);
        var targetLane = new BoundaryFlowLane(
            target < 0
                ? BoundaryLane.Negative
                : target > 0
                    ? BoundaryLane.Positive
                    : read.Lane.Boundary,
            read.Lane.Flow);
        var targetMagnitude = AbsAsLong(target);

        if (BoundaryFlowLaneBasis.Allows(laneBasis, targetLane) &&
            CanMoveToLane(read.Magnitude.Carrier.UnitShape, laneBasis, read.Lane, targetLane))
        {
            settledValue = BoundaryFlowValueSurface.Create(
                new Carrier(),
                Relation.Point(read.Magnitude.Carrier, targetMagnitude),
                targetLane,
                laneBasis,
                Relation.Point(read.Magnitude.Carrier, 0));
            return true;
        }

        var heldLane = new BoundaryFlowLane(read.Lane.Boundary, read.Lane.Flow);
        settledValue = BoundaryFlowValueSurface.Create(
            new Carrier(),
            Relation.Point(read.Magnitude.Carrier, 0),
            heldLane,
            laneBasis,
            Relation.Point(read.Magnitude.Carrier, target));
        return true;
    }

    public static bool TryApplySignedDeltaWitness(
        Relation valueSurface,
        long deltaTicks,
        out Relation witness)
    {
        if (!BoundaryFlowValueSurface.TryReadValueWithBasis(valueSurface, out var basisRead))
        {
            witness = null!;
            return false;
        }

        return TryApplySignedDeltaWitness(valueSurface, basisRead.LaneBasis, deltaTicks, out witness);
    }

    public static bool TryApplySignedDeltaWitness(
        Relation valueSurface,
        Relation laneBasis,
        long deltaTicks,
        out Relation witness)
    {
        if (!BoundaryFlowValueSurface.TryReadValueWithBasis(valueSurface, laneBasis, out var beforeRead) ||
            !TryApplySignedDelta(valueSurface, laneBasis, deltaTicks, out var settledValue) ||
            !BoundaryFlowValueSurface.TryReadValue(settledValue, laneBasis, out var afterRead))
        {
            witness = null!;
            return false;
        }

        var deltaSurface = Relation.OrderedGroup(
            new Carrier(),
            laneBasis,
            Relation.Point(beforeRead.Magnitude.Carrier, deltaTicks));
        var provenance = Relation.OrderedGroup(
            new Carrier(),
            valueSurface,
            laneBasis);

        witness = ReadWitness.Create(
            new Carrier(),
            valueSurface,
            deltaSurface,
            settledValue,
            afterRead.ResidueOrTension,
            provenance);
        return true;
    }

    private static bool CanMoveToLane(
        UnitShape unitShape,
        Relation laneBasis,
        BoundaryFlowLane source,
        BoundaryFlowLane target) =>
        source.Equals(target) ||
        BoundaryFlowLaneTransitionSurface.TryFindUnitShapeTransition(unitShape, source, target, out _) ||
        BoundaryFlowLaneTransitionSurface.TryFindTransition(laneBasis, source, target, out _);

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
/// Lane-aware route projection between value surfaces. This is a route-adjacent
/// experiment: the projection relation moves the signed/whole location, while
/// the target lane basis decides whether the resulting occupied lane is legal.
/// </summary>

