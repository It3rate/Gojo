namespace Gojo.Encoding.Core.Packed;

/// <summary>The existing positional grammar: O group 0/1 fixes O, 2 owns O/D;
/// D group 0/1 fixes D, 2 owns D. Group 2 for O only admits D group 0.</summary>
public readonly record struct FamilyField(int OrientationGroup, int DirectionGroup)
{
    public int OwnedBits => OrientationGroup == 2 ? 2 : DirectionGroup == 2 ? 1 : 0;
}

/// <summary>Local ordered sites attached to one value role. These references
/// are topology, not coordinates, geometric angles or transfer instructions.</summary>
public readonly record struct FamilyLink(int Start, int End, int Field);

/// <summary>Immutable packed connections shared by the instances of one family.
/// No shape name or constructor identity is needed to read the connections.</summary>
public sealed class FamilyTemplate
{
    private readonly EndpointRows fields = new();
    private readonly EndpointRows links = new();

    public int FieldCount => fields.Count(2, 2);
    public int LinkCount => links.Count(3, ReferenceBits);
    public int SiteCount { get; }
    public int ReferenceBits { get; }

    public FamilyTemplate(IReadOnlyList<FamilyField> fields, IReadOnlyList<FamilyLink> links)
    {
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(links);
        if (fields.Count is < 1 or > 65536) throw new ArgumentOutOfRangeException(nameof(fields));
        var maximum = fields.Count - 1;
        foreach (var field in fields)
        {
            PositionalBuckets.ValueBits(field.OrientationGroup, field.DirectionGroup, 1 + field.OwnedBits);
            this.fields.Append(2, field.OrientationGroup, field.DirectionGroup);
        }
        foreach (var link in links)
        {
            if (link.Start is < 0 or > 65535 || link.End is < 0 or > 65535 ||
                link.Field < 0 || link.Field >= fields.Count)
                throw new ArgumentOutOfRangeException(nameof(links));
            maximum = Math.Max(maximum, Math.Max(link.Start, link.End));
            SiteCount = Math.Max(SiteCount, Math.Max(link.Start, link.End) + 1);
        }
        ReferenceBits = 1;
        while ((maximum >>= 1) != 0) ReferenceBits++;
        foreach (var link in links) this.links.Append(ReferenceBits, link.Start, link.End, link.Field);
    }

    public FamilyField Field(int index)
    {
        var row = fields.Read(new(2, 2, index));
        return new(row[0], row[1]);
    }

    public FamilyLink Link(int index)
    {
        var row = links.Read(new(3, ReferenceBits, index));
        return new(row[0], row[1], row[2]);
    }

    public byte[] FieldBytes() => fields.PackedBytes(2, 2);
    public byte[] LinkBytes() => links.PackedBytes(3, ReferenceBits);

    public int RowBits(int valueBits)
    {
        if (valueBits is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(valueBits));
        var total = 0;
        for (var i = 0; i < FieldCount; i++)
        {
            var field = Field(i);
            var width = checked(valueBits + field.OwnedBits);
            PositionalBuckets.ValueBits(field.OrientationGroup, field.DirectionGroup, width);
            total = checked(total + width);
        }
        return total;
    }

    public ulong Encode(int field, int valueBits, bool positiveLeft, bool negative, ulong value)
    {
        RowBits(valueBits);
        var position = Field(field);
        return PositionalBuckets.Encode(position.OrientationGroup, position.DirectionGroup,
            valueBits + position.OwnedBits, positiveLeft, negative, value);
    }
}
