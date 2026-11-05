namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>V1 framing: version; FamilyCodec byte length and bytes; selected
/// value-width byte and entry index; use/group counts; packed uses; packed ranges.
/// Counts use canonical unsigned varints. Reference widths follow from the
/// family link count and use count; final-byte padding is zero. No labels,
/// geometry, constructor registry, or interpretation history is transported.</summary>
public static class FamilyUseCodec
{
    public static byte[] Encode(FamilyTable table, FamilyEntry entry, FamilyUseTable uses)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(uses);
        if (!ReferenceEquals(table.Template, uses.Template))
            throw new ArgumentException("Uses must refer to the table's exact family template.", nameof(uses));
        table.ReadRaw(entry, 0);
        var family = FamilyCodec.Encode(table);
        using var output = new MemoryStream();
        output.WriteByte(1);
        WriteCount(output, family.Length);
        output.Write(family);
        output.WriteByte((byte)entry.ValueBits);
        WriteCount(output, entry.Index);
        WriteCount(output, uses.UseCount);
        WriteCount(output, uses.GroupCount);
        output.Write(uses.UseBytes());
        output.Write(uses.RangeBytes());
        return output.ToArray();
    }

    public static (FamilyTable Table, FamilyEntry Entry, FamilyUseTable Uses) Decode(ReadOnlySpan<byte> bytes)
    {
        var input = new Reader(bytes);
        if (input.Byte() != 1) throw Invalid("Unknown grouped-use transport version.");
        var familyLength = input.Count();
        var table = FamilyCodec.Decode(input.Packed((long)familyLength * 8));
        var entry = new FamilyEntry(input.Byte(), input.Count());
        try { table.ReadRaw(entry, 0); }
        catch (ArgumentException) { throw Invalid("The selected family entry does not exist."); }
        var useCount = input.Count();
        var groupCount = input.Count();
        if (table.Template.LinkCount > 32768 || useCount > 65535 || groupCount > 65536)
            throw Invalid("Grouped-use dimensions exceed the 16-bit reference grammar.");
        var useBits = OrderedBoundary.Bits(Math.Max(2, table.Template.LinkCount * 2));
        var rangeBits = OrderedBoundary.Bits(useCount + 1);
        var packedUses = input.Packed((long)useCount * useBits);
        var packedRanges = input.Packed((long)groupCount * 2 * rangeBits);
        if (input.Remaining != 0) throw Invalid("Trailing bytes are not part of the grouped uses.");

        // Check packed lengths before allocating from supplied counts. Groups
        // partition the stored uses in order; empty ranges remain valid groups.
        var groups = new int[groupCount][];
        var expectedStart = 0;
        for (var group = 0; group < groupCount; group++)
        {
            var first = Bits(packedRanges, (long)group * 2 * rangeBits, rangeBits);
            var end = Bits(packedRanges, ((long)group * 2 + 1) * rangeBits, rangeBits);
            if (first != expectedStart || end < first || end > useCount)
                throw Invalid("Group ranges must partition the ordered uses.");
            var values = new int[end - first];
            for (var i = 0; i < values.Length; i++)
            {
                var use = Bits(packedUses, (long)(first + i) * useBits, useBits);
                if ((use >> 1) >= table.Template.LinkCount)
                    throw Invalid("A use refers to a missing family link.");
                values[i] = use;
            }
            groups[group] = values;
            expectedStart = end;
        }
        if (expectedStart != useCount) throw Invalid("Some uses have no group.");
        return (table, entry, new FamilyUseTable(table.Template, groups));
    }

    private static int Bits(ReadOnlySpan<byte> bytes, long position, int width)
    {
        var value = 0;
        for (var bit = 0; bit < width; bit++)
        {
            var at = position + bit;
            if ((bytes[(int)(at / 8)] & (1 << (int)(at % 8))) != 0) value |= 1 << bit;
        }
        return value;
    }

    private static void WriteCount(Stream output, int count)
    {
        var remaining = (uint)count;
        do
        {
            var next = (byte)(remaining & 127);
            remaining >>= 7;
            output.WriteByte((byte)(next | (remaining == 0 ? 0 : 128)));
        } while (remaining != 0);
    }

    private static FormatException Invalid(string reason) => new(reason);

    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes = bytes;
        private int position;
        public int Remaining => bytes.Length - position;

        public byte Byte()
        {
            if (position == bytes.Length) throw Invalid("Truncated grouped-use transport.");
            return bytes[position++];
        }

        public int Count()
        {
            uint result = 0;
            for (var shift = 0; shift <= 28; shift += 7)
            {
                var next = Byte();
                if (shift == 28 && next > 7) throw Invalid("Count exceeds the host range.");
                result |= (uint)(next & 127) << shift;
                if ((next & 128) == 0)
                {
                    if (shift != 0 && next == 0) throw Invalid("Noncanonical count encoding.");
                    return (int)result;
                }
            }
            throw Invalid("Unterminated count encoding.");
        }

        public ReadOnlySpan<byte> Packed(long bitLength)
        {
            var byteLength = (bitLength + 7) / 8;
            if (bitLength < 0 || byteLength > Remaining)
                throw Invalid("Packed length exceeds the remaining transport.");
            var packed = bytes.Slice(position, (int)byteLength);
            position += (int)byteLength;
            var used = (int)(bitLength % 8);
            if (used != 0 && (packed[^1] >> used) != 0)
                throw Invalid("Nonzero final-byte padding.");
            return packed;
        }
    }
}
