namespace Gojo.Core;

/// <summary>
/// A local read of a carrier location from a reader carrier. Orthogonal or
/// hidden carrier participation reads as zero rather than as an error.
/// </summary>
public readonly record struct CarrierRead(
    Carrier Reader,
    Carrier LocationCarrier,
    ulong Magnitude,
    long Direction)
{
    /// <summary>Reads a grounded location from the supplied reader carrier.</summary>
    public static CarrierRead From(CarrierLocation location, Carrier reader) =>
        new(
            reader,
            location.Carrier,
            MagnitudeOf(location.Ticks),
            reader.ReadSignOf(location.Carrier) * LocationSign(location.Ticks));

    /// <summary>True when this read has nonzero participation in the reader.</summary>
    public bool IsVisible => Direction != 0 && Magnitude != 0;

    /// <summary>Signed ticks visible to the reader carrier.</summary>
    public long ProjectedTicks => Direction == 0 ? 0 : checked((long)Magnitude) * Math.Sign(Direction);

    /// <summary>Returns the positive magnitude of a signed tick coordinate.</summary>
    private static ulong MagnitudeOf(long ticks) =>
        ticks >= 0 ? checked((ulong)ticks) : checked((ulong)-ticks);

    /// <summary>Returns scalar sign carried by the location coordinate.</summary>
    private static long LocationSign(long ticks) => ticks < 0 ? -1 : 1;
}
