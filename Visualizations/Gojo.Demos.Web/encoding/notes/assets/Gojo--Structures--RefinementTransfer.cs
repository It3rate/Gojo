using System.Numerics;
using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Explicit material movement through a positive, zero-origin refinement basis.
/// Whole packets are conserved between source and receiver; unavailable demand
/// and unrepresentable remainder stay visible. Projection alone never calls it.
/// </summary>
public static class RefinementTransfer
{
    /// <summary>Connects the selected calibration and requested source amount.</summary>
    public static Relation Create(Carrier root, Relation calibration, IElement requested) =>
        Relation.OrderedGroup(root, calibration, requested);

    /// <summary>
    /// Moves only whole source/target packets supported by the calibration and
    /// native units. Incoming request = accepted source + unavailable + remainder.
    /// Target settlement may retain or route its accepted injection as tension.
    /// </summary>
    public static bool TryApply(Relation request, out Relation witness)
    {
        if (!request.TryReadOrderedGroup(out var fields) || fields.Length != 2 || fields[0] is not Relation calibration)
        {
            witness = null!;
            return false;
        }
        if (fields[1] is CarrierLocation amount && amount.Ticks >= 0 &&
            TryReadCalibration(calibration, out var source, out var a, out var target, out var b) &&
            amount.Carrier.SameIdentityAs(source))
        {
            var available = Math.Min(amount.Ticks, checked((long)source.Value));
            var divisor = (long)BigInteger.GreatestCommonDivisor(a, b);
            var sourcePacket = a / divisor;
            var targetPacket = b / divisor;
            var packets = available / sourcePacket;
            var accepted = checked(packets * sourcePacket);
            var delivered = checked(packets * targetPacket);
            var unavailable = amount.Ticks - available;
            var remainder = available - accepted;
            var before = Carrier.CaptureTransferState(source, target);
            if (accepted != 0) source.TransferTo(target, accepted, delivered);
            var after = Carrier.CaptureTransferState(source, target);
            witness = ReadWitness.Create(new Carrier(), request, calibration,
                new Relation(new CarrierLocation(source, -accepted), new CarrierLocation(target, delivered)),
                Relation.OrderedGroup(new Carrier(), new CarrierLocation(source, unavailable), new CarrierLocation(source, remainder)),
                new Relation(before, after));
            return true;
        }
        witness = ReadWitness.Create(new Carrier(), request, calibration, new Placeholder(), fields[1], calibration);
        return false;
    }

    /// <summary>
    /// Explicitly refills an unmet receiver draw using the smallest whole
    /// calibration packets needed. A lack of source value leaves the draw as
    /// receiver tension; no value is invented and no unrelated route is chosen.
    /// </summary>
    public static bool TryBorrow(Relation calibration, out Relation witness)
    {
        if (TryReadCalibration(calibration, out var source, out var a, out var target, out var b))
        {
            var divisor = (long)BigInteger.GreatestCommonDivisor(a, b);
            var sourcePacket = a / divisor;
            var targetPacket = b / divisor;
            var draw = BigInteger.Max(0, -(BigInteger)target.Tension);
            var packets = (draw + targetPacket - 1) / targetPacket;
            var requested = checked((long)(packets * sourcePacket));
            var request = Create(new Carrier(), calibration, new CarrierLocation(source, requested));
            return TryApply(request, out witness);
        }
        witness = ReadWitness.Create(new Carrier(), calibration, calibration, new Placeholder(), calibration, calibration);
        return false;
    }

    private static bool TryReadCalibration(Relation calibration, out Carrier source, out long a, out Carrier target, out long b)
    {
        if (TryReadPositiveFrame(calibration.Left, out source, out a) &&
            TryReadPositiveFrame(calibration.Right, out target, out b) &&
            !source.IsUnknown && !target.IsUnknown && !source.SameIdentityAs(target) &&
            source.IsInverse == target.IsInverse &&
            UnitShape.ResolutionOf(source.UnitShape, a).Equals(UnitShape.ResolutionOf(target.UnitShape, b)))
            return true;
        source = target = null!;
        a = b = 0;
        return false;
    }

    private static bool TryReadPositiveFrame(IElement frame, out Carrier carrier, out long ticks)
    {
        if (frame is Relation { Left: Relation start, Right: Relation end } &&
            start is { Left: CarrierLocation origin, Right: Carrier c } &&
            end is { Left: CarrierLocation extent, Right: Carrier d } &&
            c.SameIdentityAs(d) && origin.Carrier.SameIdentityAs(c) && extent.Carrier.SameIdentityAs(c) &&
            origin.Ticks == 0 && extent.Ticks > 0)
        {
            carrier = c;
            ticks = extent.Ticks;
            return true;
        }
        carrier = null!;
        ticks = 0;
        return false;
    }
}
