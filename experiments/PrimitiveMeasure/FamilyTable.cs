namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Address inside an owner-local family. Width is value bits per field,
/// excluding any owned O/D bits. The table supplies the address's scope.</summary>
public readonly record struct FamilyEntry(int ValueBits, int Index);
public readonly record struct FamilySite(FamilyTable Owner, FamilyEntry Entry, int Local);
public readonly record struct FamilyValue(FamilyTable Owner, FamilyEntry Entry, int Field);
public readonly record struct FamilySpan(FamilySite Start, FamilySite End, FamilyValue Value);

/// <summary>One template and only its populated width groups. Appending constructs
/// encoded state; this is not a material input or settlement API. The table is
/// append-only. Reads do not alter entries or the immutable shared template.</summary>
public sealed class FamilyTable(FamilyTemplate template)
{
    private sealed class Group
    {
        public byte[] Bytes = [];
        public int Count;
    }

    private readonly SortedDictionary<int, Group> groups = new();
    public FamilyTemplate Template { get; } = template ?? throw new ArgumentNullException(nameof(template));
    public int[] Widths => groups.Keys.ToArray();
    public int Count(int valueBits) => groups.TryGetValue(valueBits, out var group) ? group.Count : 0;
    public int RowBits(int valueBits) => Template.RowBits(valueBits);
    public long PayloadBitLength(int valueBits) => checked((long)Count(valueBits) * RowBits(valueBits));
    public byte[] PackedBytes(int valueBits) => groups.TryGetValue(valueBits, out var group) ? group.Bytes.ToArray() : [];

    public FamilyEntry Append(int valueBits, params ulong[] rawFields)
    {
        ArgumentNullException.ThrowIfNull(rawFields);
        var stride = RowBits(valueBits);
        if (rawFields.Length != Template.FieldCount) throw new ArgumentException("One raw field per template role is required.", nameof(rawFields));
        for (var i = 0; i < rawFields.Length; i++)
        {
            var field = Template.Field(i);
            PositionalBuckets.Decode(new(field.OrientationGroup, field.DirectionGroup, valueBits + field.OwnedBits, 0), rawFields[i]);
        }
        var group = groups.GetValueOrDefault(valueBits) ?? new Group();
        var next = checked(group.Count + 1);
        var length = checked((int)((checked((long)next * stride) + 7) / 8));
        if (length != group.Bytes.Length) Array.Resize(ref group.Bytes, length);
        var offset = checked((long)group.Count * stride);
        for (var field = 0; field < rawFields.Length; field++)
        {
            var width = valueBits + Template.Field(field).OwnedBits;
            Put(group.Bytes, offset, width, rawFields[field]);
            offset += width;
        }
        var entry = new FamilyEntry(valueBits, group.Count);
        group.Count = next;
        groups[valueBits] = group;
        return entry;
    }

    public ulong ReadRaw(FamilyEntry entry, int field)
    {
        var group = Require(entry);
        var position = Template.Field(field);
        long offset = checked((long)entry.Index * RowBits(entry.ValueBits));
        for (var i = 0; i < field; i++) offset += entry.ValueBits + Template.Field(i).OwnedBits;
        return Get(group.Bytes, offset, entry.ValueBits + position.OwnedBits);
    }

    public byte ReadCoarse(FamilyEntry entry, int field)
    {
        var position = Template.Field(field);
        return PositionalBuckets.CoarseRead(new(position.OrientationGroup, position.DirectionGroup,
            entry.ValueBits + position.OwnedBits, 0), ReadRaw(entry, field));
    }

    /// <summary>Observes one occupied face of a shared signed value. These two
    /// reads do not allocate two stores or authorize duplicated material flow.</summary>
    public byte ReadSide(FamilyEntry entry, int field, bool negative) =>
        ThreeBitMeasure.ReadSide(ReadCoarse(entry, field), negative);

    /// <summary>Reverses ordered endpoints and sequence, not the stored O/D word,
    /// physical flow, or magnitude. Site and value identity remain instance-local.</summary>
    public FamilySpan[] ReadLinks(FamilyEntry entry, bool reverse = false)
    {
        Require(entry);
        var result = new FamilySpan[Template.LinkCount];
        for (var i = 0; i < result.Length; i++)
        {
            var link = Template.Link(reverse ? result.Length - 1 - i : i);
            result[i] = new(new(this, entry, reverse ? link.End : link.Start),
                new(this, entry, reverse ? link.Start : link.End), new(this, entry, link.Field));
        }
        return result;
    }

    /// <summary>Reads an ordered topological return. It does not claim positive
    /// reach, geometric closure under a placement, equal angles, or a polygon.</summary>
    public bool IsClosedWalk(FamilyEntry entry, bool reverse = false)
    {
        var spans = ReadLinks(entry, reverse);
        if (spans.Length == 0) return false;
        for (var i = 0; i < spans.Length; i++)
            if (spans[i].End != spans[(i + 1) % spans.Length].Start) return false;
        return true;
    }

    private Group Require(FamilyEntry entry)
    {
        RowBits(entry.ValueBits);
        if (entry.Index < 0 || !groups.TryGetValue(entry.ValueBits, out var group) || entry.Index >= group.Count)
            throw new ArgumentOutOfRangeException(nameof(entry));
        return group;
    }

    private static ulong Get(byte[] bytes, long offset, int width)
    {
        ulong value = 0;
        for (var bit = 0; bit < width; bit++)
        {
            var at = offset + bit;
            if ((bytes[checked((int)(at / 8))] & (1 << (int)(at % 8))) != 0) value |= 1UL << bit;
        }
        return value;
    }

    private static void Put(byte[] bytes, long offset, int width, ulong value)
    {
        for (var bit = 0; bit < width; bit++)
        {
            var at = offset + bit;
            var index = checked((int)(at / 8));
            var mask = (byte)(1 << (int)(at % 8));
            bytes[index] = (byte)((bytes[index] & ~mask) | (((value >> bit) & 1) != 0 ? mask : 0));
        }
    }
}
