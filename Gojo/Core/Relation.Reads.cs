namespace Gojo.Core;

public sealed partial class Relation
{
    #region Local State Reads

    /// <summary>Reads boolean-like support participation from a carrier frame.</summary>
    public Relation SupportOn(Carrier reader)
    {
        var (reverse, forward) = ReadStatePair(reader);
        return SlotPair(reader, reverse, forward);
    }

    /// <summary>Reads overlapping opposed support as tension participation.</summary>
    public Relation TensionOn(Carrier reader)
    {
        var (reverse, forward) = ReadStatePair(reader);
        var tension = Math.Min(reverse, forward);
        return SlotPair(reader, tension, tension);
    }

    /// <summary>Reads non-overlapped support as exclusive participation.</summary>
    public Relation ExclusiveOn(Carrier reader)
    {
        var (reverse, forward) = ReadStatePair(reader);
        return forward >= reverse
            ? SlotPair(reader, 0, forward - reverse)
            : SlotPair(reader, reverse - forward, 0);
    }

    /// <summary>Reads this relation's sorted boundary sites in a carrier frame.</summary>
    public Relation BoundariesOn(Carrier reader)
    {
        if (!TryReadFrame(this, out var frame) || !FrameAccepts(frame, reader))
        {
            var zero = new CarrierLocation(reader, 0);
            return new(zero, zero);
        }

        frame = ResolveFrameCarrier(frame, reader);
        var leftTicks = frame.Start;
        var rightTicks = frame.End;
        var start = Math.Min(leftTicks, rightTicks);
        var end = Math.Max(leftTicks, rightTicks);
        return new(new CarrierLocation(reader, start), new CarrierLocation(reader, end));
    }

    /// <summary>Reads the unsigned span between sorted boundaries.</summary>
    public CarrierLocation SpanOn(Carrier reader)
    {
        var boundaries = BoundariesOn(reader);
        return new CarrierLocation(
            boundaries.LeftLocation.Carrier,
            boundaries.RightLocation.Ticks - boundaries.LeftLocation.Ticks);
    }

    /// <summary>Reads directed flow from left site to right site in a carrier frame.</summary>
    public CarrierLocation FlowOn(Carrier reader)
    {
        if (!TryReadFrame(this, out var frame) || !FrameAccepts(frame, reader))
        {
            return new CarrierLocation(reader, 0);
        }

        frame = ResolveFrameCarrier(frame, reader);
        return new CarrierLocation(reader, frame.Extent);
    }

    /// <summary>Reads traversal start/end states carrying boundary point plus flow.</summary>
    public Relation TraversalSitesOn(Carrier reader)
    {
        if (!TryReadFrame(this, out var frame) || !FrameAccepts(frame, reader))
        {
            var zero = new CarrierLocation(reader, 0);
            return new(new Relation(zero, zero), new Relation(zero, zero));
        }

        frame = ResolveFrameCarrier(frame, reader);
        var flow = FlowOn(reader);
        if (flow.Ticks == 0)
        {
            var zero = new CarrierLocation(reader, 0);
            return new(new Relation(zero, zero), new Relation(zero, zero));
        }

        var zeroFlow = new CarrierLocation(flow.Carrier, 0);
        return new(
            new Relation(new CarrierLocation(flow.Carrier, frame.Start), zeroFlow),
            new Relation(new CarrierLocation(flow.Carrier, frame.End), flow));
    }

    #endregion

    #region Frame Readers

    /// <summary>Reads this relation as a point relation grounded on its carrier.</summary>
    private bool IsPoint(out CarrierLocation location, out Carrier carrier)
    {
        if (Left is CarrierLocation leftLocation &&
            Right is Carrier rightCarrier &&
            leftLocation.Carrier.SameIdentityAs(rightCarrier))
        {
            location = leftLocation;
            carrier = rightCarrier;
            return true;
        }

        location = default;
        carrier = null!;
        return false;
    }

    /// <summary>Reads two locations only when both are visible in reader identity.</summary>
    private static bool TryReadInCarrier(
        Carrier reader,
        CarrierLocation left,
        CarrierLocation right,
        out long leftTicks,
        out long rightTicks)
    {
        if (!left.Carrier.SameIdentityAs(reader) || !right.Carrier.SameIdentityAs(reader))
        {
            leftTicks = 0;
            rightTicks = 0;
            return false;
        }

        leftTicks = left.Ticks;
        rightTicks = right.Ticks;
        return true;
    }

    /// <summary>Reads a frame's extent as a basis carrier location.</summary>
    private static bool TryReadBasis(IElement element, out CarrierLocation basis)
    {
        if (TryReadFrame(element, out var frame))
        {
            basis = new CarrierLocation(frame.Carrier, frame.Extent);
            return true;
        }

        basis = default;
        return false;
    }

    /// <summary>Reads this relation as a bounded non-point fill frame.</summary>
    private bool TryReadFillFrame(out CarrierFrame frame)
    {
        // TODO: Generalize deferred fill measurement beyond single-carrier
        // interval frames once higher-grade fills can expose bounded measure
        // ports, approximation resolution, and unresolved measure tension.
        if (IsPoint(out _, out _) ||
            !TryReadFrame(this, out frame) ||
            frame.Carrier.IsUnknown ||
            frame.Extent == 0)
        {
            frame = default;
            return false;
        }

        return true;
    }

    /// <summary>Reads a location, point, or segment-like surface as a carrier frame.</summary>
    private static bool TryReadFrame(IElement element, out CarrierFrame frame)
    {
        if (element is CarrierLocation location)
        {
            frame = new CarrierFrame(location.Carrier, 0, location.Ticks);
            return true;
        }

        if (element is not Relation relation)
        {
            frame = default;
            return false;
        }

        if (relation.IsPoint(out var pointLocation, out _))
        {
            frame = new CarrierFrame(pointLocation.Carrier, 0, pointLocation.Ticks);
            return true;
        }

        if (relation.TryGetCarrierLocations(out var left, out var right) &&
            left.Carrier.SameIdentityAs(right.Carrier))
        {
            frame = new CarrierFrame(left.Carrier, left.Ticks, right.Ticks);
            return true;
        }

        if (relation.Left is Relation leftPoint &&
            relation.Right is Relation rightPoint &&
            leftPoint.IsPoint(out var leftPointLocation, out _) &&
            rightPoint.IsPoint(out var rightPointLocation, out _) &&
            leftPointLocation.Carrier.SameIdentityAs(rightPointLocation.Carrier))
        {
            frame = new CarrierFrame(
                leftPointLocation.Carrier,
                leftPointLocation.Ticks,
                rightPointLocation.Ticks);
            return true;
        }

        frame = default;
        return false;
    }

    /// <summary>Reads only point surfaces as origin-to-point frames.</summary>
    private static bool TryReadPointFrame(IElement element, out CarrierFrame frame)
    {
        if (element is Relation relation && relation.IsPoint(out var pointLocation, out _))
        {
            frame = new CarrierFrame(pointLocation.Carrier, 0, pointLocation.Ticks);
            return true;
        }

        frame = default;
        return false;
    }

    #endregion

    #region Support Read Helpers

    /// <summary>Builds a reverse/forward slot pair on a reader carrier.</summary>
    private static Relation SlotPair(Carrier reader, ulong reverse, ulong forward) =>
        new(
            new CarrierLocation(reader, -checked((long)reverse)),
            new CarrierLocation(reader, forward));

    /// <summary>Reads nested state as reverse and forward support magnitudes.</summary>
    private (ulong Reverse, ulong Forward) ReadStatePair(Carrier reader)
    {
        if (TryGetCarrierLocations(out var left, out var right))
        {
            return ReadSlots(reader, left, right);
        }

        return Right is Relation state ? state.ReadStatePair(reader) : (0, 0);
    }

    /// <summary>Reads two direct slots as reverse/forward participation.</summary>
    private static (ulong Reverse, ulong Forward) ReadSlots(
        Carrier reader,
        CarrierLocation left,
        CarrierLocation right)
    {
        var reverse = 0UL;
        var forward = 0UL;
        AddSlot(left);
        AddSlot(right);
        return (reverse, forward);

        void AddSlot(CarrierLocation slot)
        {
            var read = slot.ReadFrom(reader);
            if (read.Direction > 0)
            {
                forward += read.Magnitude;
            }
            else if (read.Direction < 0)
            {
                reverse += read.Magnitude;
            }
        }
    }

    #endregion
}
