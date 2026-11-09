namespace Gojo.EncodingB;

/// <summary>The meaning of one bit, read only from its address.</summary>
public sealed record BitMeaning(string Address, bool Value, string Meaning)
{
    public override string ToString() => $"{Address} = {(Value ? 1 : 0)}: {Meaning}";
}

/// <summary>The universal decoder. Every stored bit has an address — (tile,
/// plane, site) or (law, tuple bit) — and that address, read through the site
/// grammar and the law, determines what the bit means. No bit's meaning depends
/// on a host object or a name outside that basis. Padding is declared padding.</summary>
public static class BitDecoder
{
    public static BitMeaning Explain(Tile tile, string plane, int site)
    {
        ArgumentNullException.ThrowIfNull(tile);
        if (site is < 0 or >= Bits.Width) throw new ArgumentOutOfRangeException(nameof(site));
        var descriptor = tile.Grammar.Plane(plane);
        var address = $"{tile.Name}.{plane}[{site}]";
        var value = Bits.Get(tile.Read(plane), site);
        if (site >= tile.SiteCount)
        {
            var padding = descriptor.BasisSupplied
                ? "beyond the allocated sites; the basis covers allocated sites only"
                : "declared padding beyond the allocated sites; must read 0";
            return new BitMeaning(address, value, value && !descriptor.BasisSupplied ? padding + " (VIOLATED)" : padding);
        }
        var siteName = tile.SiteNames[site];
        var meaning = (value ? descriptor.WhenSet : descriptor.WhenClear) + $" at site {siteName}";
        if (descriptor.BasisSupplied) meaning += "; supplied by the tile basis, not stored";
        if (tile.Grammar.FieldOf(plane) is { } membership)
            meaning += "; " + FieldContext(tile, membership.Field, membership.Bit, site);
        return new BitMeaning(address, value, meaning);
    }

    /// <summary>Every bit of every plane, including padding: 64 per plane.</summary>
    public static IReadOnlyList<BitMeaning> DecodeAll(Tile tile)
    {
        ArgumentNullException.ThrowIfNull(tile);
        var meanings = new List<BitMeaning>();
        foreach (var plane in tile.Grammar.Planes)
            for (var site = 0; site < Bits.Width; site++)
                meanings.Add(Explain(tile, plane.Name, site));
        return meanings;
    }

    public static BitMeaning Explain(LawTable law, int bit)
    {
        ArgumentNullException.ThrowIfNull(law);
        var address = $"{law.Name}#{law.ContentHash}[{bit}]";
        if (bit >= law.BitCount && bit < (law.BitCount + 7) / 8 * 8)
            return new BitMeaning(address, false, "declared padding above the last tuple; must read 0");
        var value = law.BitAt(bit);
        var meaning = law switch
        {
            TransitionLaw transition => transition.DescribeBit(bit),
            _ => $"{law.DescribeTuple(bit)}: {(value ? "admitted" : "not admitted")}",
        };
        return new BitMeaning(address, value, meaning);
    }

    private static string FieldContext(Tile tile, FieldDescriptor field, int bit, int site) => field switch
    {
        CodeField code =>
            $"bit {bit} of field '{field.Name}', which reads {code.Domain.NameOf(Code(tile, code, site))}",
        ThermometerField thermometer => tile.Magnitude(thermometer.Name, site) is int magnitude
            ? $"bit {bit} of thermometer '{field.Name}', which reads {magnitude} of {thermometer.Capacity}"
            : $"bit {bit} of thermometer '{field.Name}', whose bits are not a prefix (broken code)",
        _ => $"bit {bit} of field '{field.Name}'",
    };

    private static int Code(Tile tile, CodeField field, int site)
    {
        var code = tile.Code(field.Name, site);
        return code < field.Domain.Size ? code : throw new InvalidOperationException(
            $"Field {field.Name} at site {site} holds code {code}, outside domain {field.Domain}.");
    }
}
