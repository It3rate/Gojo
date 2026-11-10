namespace Gojo.Encoding.Core.Packed;

/// <summary>Versioned transport for the packed family, not a new semantic
/// registry. V1: version byte; field/link counts as unsigned canonical varints;
/// reference-width byte; packed fields and links; width-group count varint;
/// then ascending groups of [value-width byte, row-count varint, packed rows].
/// Counts delimit live rows; final-byte padding never creates another value.
/// Sites, constants and shared fields are reconstructed from the packed shape.</summary>
public static class FamilyCodec
{
    public static byte[] Encode(FamilyTable table)
    {
        ArgumentNullException.ThrowIfNull(table);
        using var output = new MemoryStream();
        output.WriteByte(1);
        WriteCount(output, table.Template.FieldCount);
        WriteCount(output, table.Template.LinkCount);
        output.WriteByte((byte)table.Template.ReferenceBits);
        output.Write(table.Template.FieldBytes());
        output.Write(table.Template.LinkBytes());
        var widths = table.Widths;
        WriteCount(output, widths.Length);
        foreach (var width in widths)
        {
            output.WriteByte((byte)width);
            WriteCount(output, table.Count(width));
            output.Write(table.PackedBytes(width));
        }
        return output.ToArray();
    }

    public static FamilyTable Decode(ReadOnlySpan<byte> bytes)
    {
        var input = new Reader(bytes);
        if (input.Byte() != 1) throw Invalid("Unknown family transport version.");
        var fieldCount = input.Count();
        var linkCount = input.Count();
        var referenceBits = input.Byte();
        if (fieldCount is < 1 or > 65536 || referenceBits is < 1 or > 16)
            throw Invalid("Invalid family shape dimensions.");

        // Length checks happen before allocating arrays from supplied counts.
        var fieldBytes = input.Packed(checked((long)fieldCount * 4));
        var linkBytes = input.Packed(checked((long)linkCount * 3 * referenceBits));
        var fields = new FamilyField[fieldCount];
        for (var i = 0; i < fields.Length; i++)
        {
            var orientation = (int)Bits(fieldBytes, (long)i * 4, 2);
            var direction = (int)Bits(fieldBytes, (long)i * 4 + 2, 2);
            if (orientation > 2 || direction > 2 || (orientation == 2 && direction != 0))
                throw Invalid("An encoded field has invalid fixed-bit selectors.");
            fields[i] = new FamilyField(orientation, direction);
        }
        var links = new FamilyLink[linkCount];
        for (var i = 0; i < links.Length; i++)
        {
            var at = (long)i * 3 * referenceBits;
            var start = (int)Bits(linkBytes, at, referenceBits);
            var end = (int)Bits(linkBytes, at + referenceBits, referenceBits);
            var field = (int)Bits(linkBytes, at + 2 * referenceBits, referenceBits);
            if (field >= fieldCount) throw Invalid("A link refers to a missing field.");
            links[i] = new FamilyLink(start, end, field);
        }

        FamilyTemplate template;
        try { template = new FamilyTemplate(fields, links); }
        catch (ArgumentException) { throw Invalid("The encoded family shape is invalid."); }
        if (template.ReferenceBits != referenceBits)
            throw Invalid("The reference width is not the family's minimal width.");
        var table = new FamilyTable(template);
        var groups = input.Count();
        if (groups > 64 || groups > input.Remaining / 3)
            throw Invalid("The width-group count exceeds its available framing.");
        var previousWidth = 0;
        for (var group = 0; group < groups; group++)
        {
            var valueBits = input.Byte();
            if (valueBits <= previousWidth || valueBits > 64)
                throw Invalid("Value widths must be distinct and ascending.");
            previousWidth = valueBits;
            var count = input.Count();
            if (count == 0) throw Invalid("A stored width group must contain a live row.");
            int rowBits;
            try { rowBits = template.RowBits(valueBits); }
            catch (ArgumentException) { throw Invalid("The value width does not fit this family's fields."); }
            catch (OverflowException) { throw Invalid("The row stride cannot be represented."); }
            var payload = input.Packed(checked((long)count * rowBits));
            var raw = new ulong[fieldCount];
            for (var row = 0; row < count; row++)
            {
                var at = (long)row * rowBits;
                for (var field = 0; field < fieldCount; field++)
                {
                    var width = valueBits + fields[field].OwnedBits;
                    raw[field] = Bits(payload, at, width);
                    at += width;
                }
                table.Append(valueBits, raw);
            }
        }
        if (input.Remaining != 0) throw Invalid("Trailing bytes are not part of the family.");
        return table;
    }

    private static ulong Bits(ReadOnlySpan<byte> bytes, long at, int width)
    {
        ulong result = 0;
        for (var bit = 0; bit < width; bit++)
        {
            var position = at + bit;
            if ((bytes[(int)(position / 8)] & (1 << (int)(position % 8))) != 0)
                result |= 1UL << bit;
        }
        return result;
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
            if (position == bytes.Length) throw Invalid("Truncated family transport.");
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
