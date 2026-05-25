using Gojo.Core;

namespace Gojo.Reads;

/// <summary>
/// Observational accounting reads for carrier locations. These helpers compare
/// a local location against already-accounted locations or relations and return
/// remaining visible tension-shaped participation.
/// </summary>
public static class CarrierLocationReads
{
    /// <summary>Reads new tension after one accounted location in a reader frame.</summary>
    public static CarrierLocation NewTensionAfter(
        this CarrierLocation location,
        CarrierLocation accounted,
        Carrier reader) =>
        location.NewTensionAfter(reader, accounted);

    /// <summary>Reads the portion accounted by a relation's carrier locations.</summary>
    public static Relation AccountedBy(
        this CarrierLocation location,
        Carrier reader,
        Relation accounted) =>
        location.AccountedBy(reader, accounted.CarrierLocations());

    /// <summary>Reads the portion accounted by several relation surfaces.</summary>
    public static Relation AccountedBy(
        this CarrierLocation location,
        Carrier reader,
        Relation firstAccounted,
        params Relation[] remainingAccounted) =>
        location.AccountedBy(reader, Flatten(firstAccounted, remainingAccounted));

    /// <summary>Splits a visible location into accounted and remaining tension parts.</summary>
    public static Relation AccountedBy(
        this CarrierLocation location,
        Carrier reader,
        params CarrierLocation[] accounted)
    {
        if (!location.ReadFrom(reader).IsVisible)
        {
            var zero = new CarrierLocation(location.Carrier, 0);
            return new(zero, zero);
        }

        var tension = location.NewTensionAfter(reader, accounted);
        return new(new CarrierLocation(location.Carrier, location.Ticks - tension.Ticks), tension);
    }

    /// <summary>Reads remaining tension after one accounted relation.</summary>
    public static CarrierLocation NewTensionAfter(
        this CarrierLocation location,
        Carrier reader,
        Relation accounted) =>
        location.NewTensionAfter(reader, accounted.CarrierLocations());

    /// <summary>Reads remaining tension after several accounted relations.</summary>
    public static CarrierLocation NewTensionAfter(
        this CarrierLocation location,
        Carrier reader,
        Relation firstAccounted,
        params Relation[] remainingAccounted) =>
        location.NewTensionAfter(reader, Flatten(firstAccounted, remainingAccounted));

    /// <summary>Reads remaining visible magnitude after same-direction accounted slots.</summary>
    public static CarrierLocation NewTensionAfter(
        this CarrierLocation location,
        Carrier reader,
        params CarrierLocation[] accounted)
    {
        var current = location.ReadFrom(reader);
        if (!current.IsVisible)
        {
            return new(location.Carrier, 0);
        }

        var coveredTicks = 0UL;
        foreach (var account in accounted)
        {
            var covered = account.ReadFrom(reader);
            if (!covered.IsVisible || covered.Direction != current.Direction)
            {
                continue;
            }

            var remaining = current.Magnitude - coveredTicks;
            if (remaining == 0 || covered.Magnitude >= remaining)
            {
                return new(location.Carrier, 0);
            }

            coveredTicks = checked(coveredTicks + covered.Magnitude);
        }

        var uncovered = checked((long)(current.Magnitude - coveredTicks));
        return new(location.Carrier, location.Ticks < 0 ? -uncovered : uncovered);
    }

    /// <summary>Flattens relation carrier locations for accounting reads.</summary>
    private static CarrierLocation[] Flatten(Relation first, Relation[] rest)
    {
        var locations = new List<CarrierLocation>(first.CarrierLocations());
        foreach (var relation in rest)
        {
            locations.AddRange(relation.CarrierLocations());
        }

        return [.. locations];
    }
}
