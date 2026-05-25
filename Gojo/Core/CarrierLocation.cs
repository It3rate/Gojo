namespace Gojo.Core;

/// <summary>
/// A tick coordinate grounded on a carrier. The carrier gives the coordinate
/// its local read frame; the TickRef may be shared so structure can retarget
/// the same coordinate across multiple carrier faces.
/// </summary>
public readonly struct CarrierLocation : IElement, IEquatable<CarrierLocation>
{
    /// <summary>Grounds an independent signed tick coordinate on a carrier.</summary>
    public CarrierLocation(Carrier carrier, long ticks)
        : this(carrier, new TickRef(ticks))
    {
    }

    /// <summary>Grounds an independent unsigned tick coordinate on a carrier.</summary>
    public CarrierLocation(Carrier carrier, ulong ticks)
        : this(carrier, checked((long)ticks))
    {
    }

    /// <summary>Grounds a possibly shared tick reference on a carrier.</summary>
    public CarrierLocation(Carrier carrier, TickRef tick)
    {
        Carrier = carrier ?? throw new ArgumentNullException(nameof(carrier));
        Tick = tick ?? throw new ArgumentNullException(nameof(tick));
    }

    public Carrier Carrier { get; }

    public TickRef Tick { get; }

    public long Ticks => Tick?.Ticks ?? 0;

    /// <summary>Negates the location coordinate on the same carrier face.</summary>
    public CarrierLocation Dual() => new(Carrier, checked(-Ticks));

    /// <summary>Reads this location from another carrier's perspective.</summary>
    public CarrierRead ReadFrom(Carrier reader) => CarrierRead.From(this, reader);

    /// <summary>True when two grounded locations share the same coordinate ref.</summary>
    public bool SameTickRefAs(CarrierLocation other) => ReferenceEquals(Tick, other.Tick);

    /// <summary>Compares carrier identity plus current tick value.</summary>
    public bool Equals(CarrierLocation other)
    {
        if (Carrier is null || other.Carrier is null)
        {
            return ReferenceEquals(Carrier, other.Carrier) && Ticks == other.Ticks;
        }

        return Carrier.SameIdentityAs(other.Carrier) && Ticks == other.Ticks;
    }

    public override bool Equals(object? obj) => obj is CarrierLocation other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Carrier?.Id ?? 0, Ticks);

    public static bool operator ==(CarrierLocation left, CarrierLocation right) => left.Equals(right);

    public static bool operator !=(CarrierLocation left, CarrierLocation right) => !left.Equals(right);

    public override string ToString() => $"{Carrier}:{Ticks}";

    /// <summary>Returns the structural dual of this carrier location.</summary>
    IElement IElement.Dual() => Dual();
}
