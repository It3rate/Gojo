using Gojo.Encoding.Core.Packed;

namespace Gojo.Encoding.Core.Tests.Packed;

public sealed class FamilyCodecTests
{
    // version; field/link counts; reference bits; field/link payload;
    // width-group count; value bits; live row count; row payload.
    private static byte[] ThreeValues() => [1, 1, 1, 1, 0, 2, 1, 1, 3, 5];

    [Fact]
    public void IndependentBytesSupplyTheShapeAndEveryLiveRowWithoutAConstructorRegistry()
    {
        var bytes = ThreeValues();
        var decoded = FamilyCodec.Decode(bytes);
        Assert.Equal(new FamilyField(0, 0), decoded.Template.Field(0));
        Assert.Equal(new FamilyLink(0, 1, 0), decoded.Template.Link(0));
        Assert.Equal(2, decoded.Template.SiteCount);
        Assert.Equal(1, decoded.Template.ReferenceBits);
        Assert.Equal(new[] { 1 }, decoded.Widths);
        Assert.Equal(3, decoded.Count(1));
        Assert.Equal(3, decoded.PayloadBitLength(1));
        Assert.Equal(new ulong[] { 1, 0, 1 }, Enumerable.Range(0, 3)
            .Select(i => decoded.ReadRaw(new(1, i), 0)).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => decoded.ReadRaw(new(1, 3), 0));
        Assert.Equal(bytes, FamilyCodec.Encode(decoded));
        bytes[^1] = 0;
        Assert.Equal(1UL, decoded.ReadRaw(new(1, 0), 0));
    }

    [Fact]
    public void SharedSitesAndSharedValueRolesSurviveWithoutFlatteningThemIntoEqualNumbers()
    {
        var template = new FamilyTemplate([new(0, 0), new(1, 1)],
            [new(0, 1, 0), new(0, 2, 0), new(2, 3, 1)]);
        var source = new FamilyTable(template);
        var entry = source.Append(1, 1, 1);
        var decoded = FamilyCodec.Decode(FamilyCodec.Encode(source));
        var spans = decoded.ReadLinks(entry);
        Assert.Equal(spans[0].Start, spans[1].Start);
        Assert.Equal(spans[0].Value, spans[1].Value);
        Assert.Equal(spans[1].End, spans[2].Start);
        Assert.NotEqual(spans[1].Value, spans[2].Value);
        Assert.NotEqual(source.ReadLinks(entry)[0].Start, spans[0].Start);
        Assert.Equal(new FamilyField(1, 1), decoded.Template.Field(1));
        Assert.Equal((byte)1, decoded.ReadCoarse(entry, 0));
        Assert.Equal((byte)7, decoded.ReadCoarse(entry, 1));
        Assert.Equal(template.FieldBytes(), decoded.Template.FieldBytes());
        Assert.Equal(template.LinkBytes(), decoded.Template.LinkBytes());
    }

    [Fact]
    public void MultipleWidthGroupsCrossByteBoundariesAndRowsCanExceedOneMachineWord()
    {
        var template = new FamilyTemplate([new(0, 2), new(1, 2), new(2, 0)],
            [new(0, 1, 0), new(1, 2, 1), new(2, 0, 2)]);
        var table = new FamilyTable(template);
        foreach (var width in new[] { 62, 1, 4 })
            for (var row = 0; row < 13; row++)
            {
                var raw = Enumerable.Range(0, 3).Select(field =>
                {
                    var bits = width + template.Field(field).OwnedBits;
                    var mask = bits == 64 ? ulong.MaxValue : (1UL << bits) - 1;
                    return unchecked((ulong)(row * 3 + field) * 0x9E3779B97F4A7C15UL) & mask;
                }).ToArray();
                table.Append(width, raw);
            }
        var encoded = FamilyCodec.Encode(table);
        var decoded = FamilyCodec.Decode(encoded);
        Assert.Equal(new[] { 1, 4, 62 }, decoded.Widths);
        Assert.Equal(190, decoded.RowBits(62));
        foreach (var width in table.Widths)
        {
            Assert.Equal(table.Count(width), decoded.Count(width));
            Assert.Equal(table.PackedBytes(width), decoded.PackedBytes(width));
            for (var row = 0; row < table.Count(width); row++)
                for (var field = 0; field < template.FieldCount; field++)
                    Assert.Equal(table.ReadRaw(new(width, row), field), decoded.ReadRaw(new(width, row), field));
        }
        Assert.Equal(encoded, FamilyCodec.Encode(decoded));
        decoded.Append(1, 3, 3, 7);
        Assert.Equal(13, table.Count(1));
        Assert.Equal(14, decoded.Count(1));
        Assert.Equal(table.ReadRaw(new(1, 12), 2), decoded.ReadRaw(new(1, 12), 2));
        decoded.Append(3, 15, 15, 31);
        var grown = FamilyCodec.Decode(FamilyCodec.Encode(decoded));
        Assert.Equal(new[] { 1, 3, 4, 62 }, grown.Widths);
        Assert.Equal(31UL, grown.ReadRaw(new(3, 0), 2));
        Assert.Equal(table.PackedBytes(62), grown.PackedBytes(62));
    }

    [Fact]
    public void CountsGrowPastOneVarintByteWithoutChangingPackedReferencesOrLiveRows()
    {
        var fields = Enumerable.Repeat(new FamilyField(0, 0), 130).ToArray();
        var links = Enumerable.Range(0, 130).Select(i => new FamilyLink(i, (i + 1) % 130, i)).ToArray();
        var table = new FamilyTable(new(fields, links));
        for (var row = 0; row < 130; row++)
            table.Append(1, Enumerable.Range(0, 130).Select(field => (ulong)((field + row) & 1)).ToArray());
        var encoded = FamilyCodec.Encode(table);
        Assert.Equal(new byte[] { 1, 130, 1, 130, 1, 8 }, encoded[..6]);
        var decoded = FamilyCodec.Decode(encoded);
        Assert.Equal(130, decoded.Template.FieldCount);
        Assert.Equal(130, decoded.Template.LinkCount);
        Assert.Equal(new FamilyLink(129, 0, 129), decoded.Template.Link(129));
        Assert.Equal(130, decoded.Count(1));
        Assert.Equal(1UL, decoded.ReadRaw(new(1, 129), 128));
        Assert.Equal(0UL, decoded.ReadRaw(new(1, 129), 129));
        Assert.Equal(encoded, FamilyCodec.Encode(decoded));
    }

    [Fact]
    public void ZeroRowsRetainTheirDirectionAndDoNotBecomeMissingRowsOrPadding()
    {
        var table = new FamilyTable(new([new(0, 2)], [new(0, 1, 0)]));
        table.Append(1, 0);
        table.Append(1, 2);
        table.Append(1, 0);
        var decoded = FamilyCodec.Decode(FamilyCodec.Encode(table));
        Assert.Equal(3, decoded.Count(1));
        Assert.Equal(6, decoded.PayloadBitLength(1));
        Assert.Equal(new byte[] { 8 }, decoded.PackedBytes(1));
        Assert.Equal((byte)0, decoded.ReadCoarse(new(1, 0), 0));
        Assert.Equal((byte)2, decoded.ReadCoarse(new(1, 1), 0));
        Assert.Equal((byte)0, decoded.ReadCoarse(new(1, 2), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => decoded.ReadRaw(new(1, 3), 0));
    }

    [Fact]
    public void EmptyTablesAndUnconnectedFieldsRemainInspectable()
    {
        var table = new FamilyTable(new([new(2, 0)], []));
        var decoded = FamilyCodec.Decode(FamilyCodec.Encode(table));
        Assert.Empty(decoded.Widths);
        Assert.Equal(0, decoded.Template.SiteCount);
        Assert.Equal(0, decoded.Template.LinkCount);
        Assert.Equal(new FamilyField(2, 0), decoded.Template.Field(0));
        var entry = decoded.Append(62, ulong.MaxValue);
        var restored = FamilyCodec.Decode(FamilyCodec.Encode(decoded));
        Assert.Equal(ulong.MaxValue, restored.ReadRaw(entry, 0));
    }

    [Fact]
    public void EveryTruncatedPrefixAndTrailingBytesAreRejected()
    {
        var bytes = ThreeValues();
        for (var length = 0; length < bytes.Length; length++)
        {
            var prefix = bytes[..length];
            Assert.Throws<FormatException>(() => FamilyCodec.Decode(prefix));
        }
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([.. bytes, 0]));
    }

    [Fact]
    public void InvalidSelectorsMissingFieldsAndAllNonzeroPaddingAreRejected()
    {
        foreach (var (index, value) in new (int, byte)[]
        {
            (0, 2), (1, 0), (3, 0), (3, 17), // version, count, reference widths
            (4, 3), (4, 12), (4, 6),         // invalid OG, DG, and OG=2/DG=1
            (5, 6),                         // link refers to nonexistent field 1
            (4, 128), (5, 130), (9, 133),   // field/link/row final-byte padding
            (7, 0), (7, 65), (8, 0)         // value width and empty group
        })
        {
            var bytes = ThreeValues();
            bytes[index] = value;
            Assert.Throws<FormatException>(() => FamilyCodec.Decode(bytes));
        }
        var nonminimal = ThreeValues();
        nonminimal[3] = 2;
        nonminimal[5] = 4; // Still [0,1,0], but wasting a bit in every reference.
        Assert.Throws<FormatException>(() => FamilyCodec.Decode(nonminimal));
        var overflowingField = ThreeValues();
        overflowingField[4] = 8; // Owns D, so 64 value bits cannot fit the field.
        overflowingField[7] = 64;
        Assert.Throws<FormatException>(() => FamilyCodec.Decode(overflowingField));
    }

    [Fact]
    public void DuplicateWidthGroupsAndHostileCountsAreRejectedBeforeLargeAllocation()
    {
        var duplicate = ThreeValues();
        duplicate[6] = 2;
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([.. duplicate, 1, 1, 1]));
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([1, 0x81, 0, 1, 1])); // overlong field count
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([1, 0xff, 0xff, 0xff, 0xff, 7, 1, 1]));
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([1, 1, 0xff, 0xff, 0xff, 0xff, 7, 1]));
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([1, 1, 0xff, 0xff, 0xff, 0xff, 8, 1]));
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([1, 1, 1, 1, 0, 2, 1, 1,
            0xff, 0xff, 0xff, 0xff, 7])); // huge row count, no payload
        Assert.Throws<FormatException>(() => FamilyCodec.Decode([1, 1, 1, 1, 0, 2, 65]));
    }
}
