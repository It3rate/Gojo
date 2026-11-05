using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class FamilyTableTests
{
    [Fact]
    public void AliasesShareOneStoredValueWhileEqualCoarseReadsKeepIndependentValues()
    {
        var shared = new FamilyTable(new([new(0, 0)], [new(0, 1, 0), new(0, 2, 0)]));
        var independent = new FamilyTable(new([new(0, 0), new(0, 0)], [new(0, 1, 0), new(0, 2, 1)]));
        var one = shared.Append(5, 7);
        var two = independent.Append(5, 7, 19);
        var aliases = shared.ReadLinks(one);
        var pair = independent.ReadLinks(two);

        Assert.Equal(aliases[0].Value, aliases[1].Value);
        Assert.NotEqual(aliases[0].End, aliases[1].End);
        Assert.NotEqual(pair[0].Value, pair[1].Value);
        Assert.Equal(pair[0].Start, pair[1].Start);
        Assert.Equal(independent.ReadCoarse(two, 0), independent.ReadCoarse(two, 1));
        Assert.Equal(7UL, independent.ReadRaw(two, 0));
        Assert.Equal(19UL, independent.ReadRaw(two, 1));
        Assert.Equal(5, shared.RowBits(5));
        Assert.Equal(10, independent.RowBits(5));
    }

    [Fact]
    public void SharedSitesAndValuesRemainLocalToTheOwnerEntryAndWidth()
    {
        var template = new FamilyTemplate([new(0, 0)], [new(0, 1, 0), new(0, 2, 0)]);
        var first = new FamilyTable(template);
        var second = new FamilyTable(template);
        var entry = first.Append(1, 1);
        var later = first.Append(1, 1);
        var wider = first.Append(2, 1);
        var other = second.Append(1, 1);
        var spans = first.ReadLinks(entry);

        Assert.Equal(entry, other);
        Assert.Equal(spans[0].Start, spans[1].Start);
        Assert.Equal(spans[0].Value, spans[1].Value);
        foreach (var otherSpan in new[]
                 { first.ReadLinks(later)[0], first.ReadLinks(wider)[0], second.ReadLinks(other)[0] })
        {
            Assert.NotEqual(spans[0].Start, otherSpan.Start);
            Assert.NotEqual(spans[0].Value, otherSpan.Value);
        }
    }

    [Fact]
    public void EndpointMapsDistinguishHubSharedTipChainAndOrderedCycle()
    {
        var hub = Make(new(0, 1, 0), new(0, 2, 0), new(0, 3, 0));
        var tip = Make(new(0, 2, 0), new(1, 2, 0));
        var chain = Make(new(0, 1, 0), new(1, 2, 0), new(2, 3, 0));
        var cycle = Make(new(0, 1, 0), new(1, 2, 0), new(2, 0, 0));
        var scrambled = Make(new(0, 1, 0), new(2, 0, 0), new(1, 2, 0));
        var at = new FamilyEntry(1, 0);

        Assert.All(hub.ReadLinks(at), span => Assert.Equal(hub.ReadLinks(at)[0].Start, span.Start));
        Assert.Equal(tip.ReadLinks(at)[0].End, tip.ReadLinks(at)[1].End);
        Assert.NotEqual(tip.ReadLinks(at)[0].Start, tip.ReadLinks(at)[1].Start);
        Assert.Equal(chain.ReadLinks(at)[0].End, chain.ReadLinks(at)[1].Start);
        Assert.Equal(chain.ReadLinks(at)[1].End, chain.ReadLinks(at)[2].Start);
        Assert.True(cycle.IsClosedWalk(at));
        Assert.True(cycle.IsClosedWalk(at, reverse: true));
        foreach (var table in new[] { hub, tip, chain, scrambled }) Assert.False(table.IsClosedWalk(at));
    }

    [Fact]
    public void ReverseChangesOrderedViewWithoutChangingWordsSitesOrValueOwnership()
    {
        var table = new FamilyTable(new([new(2, 0), new(0, 2)], [new(0, 1, 0), new(1, 2, 1)]));
        var entry = table.Append(1, 7, 3);
        var bytes = table.PackedBytes(1);
        var forward = table.ReadLinks(entry);
        var reverse = table.ReadLinks(entry, reverse: true);

        for (var i = 0; i < forward.Length; i++)
        {
            var original = forward[^(i + 1)];
            Assert.Equal(original.End, reverse[i].Start);
            Assert.Equal(original.Start, reverse[i].End);
            Assert.Equal(original.Value, reverse[i].Value);
        }
        Assert.Equal(forward, table.ReadLinks(entry));
        Assert.Equal(bytes, table.PackedBytes(1));
        Assert.Equal((byte)7, table.ReadCoarse(entry, 0));
        Assert.Equal((byte)3, table.ReadCoarse(entry, 1));
    }

    [Fact]
    public void ZeroRetainsTopologyAndBareValuesDoNotInventEndpoints()
    {
        var cycle = new FamilyTable(new([new(0, 0)], [new(0, 1, 0), new(1, 0, 0)]));
        var zero = cycle.Append(1, 0);
        var spans = cycle.ReadLinks(zero);
        Assert.Equal((byte)0, cycle.ReadCoarse(zero, 0));
        Assert.Equal(2, spans.Length);
        Assert.NotEqual(spans[0].Start, spans[0].End);
        Assert.True(cycle.IsClosedWalk(zero));

        var self = Make(new FamilyLink(0, 0, 0));
        Assert.True(self.IsClosedWalk(new(1, 0)));
        Assert.Equal((byte)1, self.ReadCoarse(new(1, 0), 0));

        var bare = new FamilyTable(new([new(0, 2)], []));
        var entry = bare.Append(1, 3);
        Assert.Equal(0, bare.Template.SiteCount);
        Assert.Empty(bare.ReadLinks(entry));
        Assert.Empty(bare.ReadLinks(entry, reverse: true));
        Assert.False(bare.IsClosedWalk(entry));
        Assert.Equal((byte)3, bare.ReadCoarse(entry, 0));
    }

    [Theory]
    [InlineData(0, 0, 2)]
    [InlineData(1, 1, 2)]
    [InlineData(2, 0, 2)]
    [InlineData(3, 0, 3)]
    public void OpposedReadsShareOneSignedStoreAndRetainBothZeroDirections(int raw, int positive, int negative)
    {
        var table = new FamilyTable(new([new(0, 2)], []));
        var entry = table.Append(1, (ulong)raw);
        Assert.Equal((byte)raw, table.ReadCoarse(entry, 0));
        Assert.Equal((byte)positive, table.ReadSide(entry, 0, negative: false));
        Assert.Equal((byte)negative, table.ReadSide(entry, 0, negative: true));
        Assert.Equal((ulong)raw, table.ReadRaw(entry, 0));
        Assert.Equal(2L, table.PayloadBitLength(1));
        Assert.Equal(new byte[] { (byte)raw }, table.PackedBytes(1));
    }

    [Fact]
    public void MixedFixedAndOwnedBitsPackContiguouslyAcrossRecordBoundaries()
    {
        var table = new FamilyTable(new([new(0, 0), new(1, 1), new(0, 2), new(2, 0)], []));
        var first = table.Append(1, 1, 0, 3, 5);
        var second = table.Append(1, 1, 1, 2, 6);
        Assert.Equal(7, table.RowBits(1));
        Assert.Equal(14L, table.PayloadBitLength(1));
        Assert.Equal(new byte[] { 0xDD, 0x35 }, table.PackedBytes(1));
        Assert.Equal(new byte[] { 1, 6, 3, 5 }, Enumerable.Range(0, 4).Select(i => table.ReadCoarse(first, i)).ToArray());
        Assert.Equal(new byte[] { 1, 7, 2, 6 }, Enumerable.Range(0, 4).Select(i => table.ReadCoarse(second, i)).ToArray());
        Assert.Throws<ArgumentException>(() => table.Template.Encode(0, 1, true, false, 0));
        Assert.Throws<ArgumentException>(() => table.Template.Encode(1, 1, true, false, 0));
    }

    [Fact]
    public void WideMixedRecordsRemainAddressableAtUnalignedOffsets()
    {
        var fields = new FamilyField[] { new(0, 0), new(1, 1), new(0, 2), new(2, 0) };
        var table = new FamilyTable(new(fields, []));
        foreach (var width in new[] { 1, 2, 5, 17, 31, 62 })
        {
            var expected = new List<ulong[]>();
            var mask = (1UL << width) - 1;
            for (var row = 0; row < 11; row++)
            {
                var raw = new ulong[fields.Length];
                for (var field = 0; field < raw.Length; field++)
                {
                    var value = row == 0 ? mask : row == 1 ? 0UL : unchecked((ulong)(row * 4 + field) * 0x9E3779B97F4A7C15UL) & mask;
                    var left = field == 1 || field == 3 && row % 2 == 0;
                    var negative = field == 1 || field >= 2 && row % 3 == 0;
                    raw[field] = table.Template.Encode(field, width, left, negative, value);
                }
                expected.Add(raw);
                Assert.Equal(new FamilyEntry(width, row), table.Append(width, raw));
            }
            for (var row = 0; row < expected.Count; row++)
                for (var field = 0; field < fields.Length; field++)
                    Assert.Equal(expected[row][field], table.ReadRaw(new(width, row), field));
            Assert.Equal(4 * width + 3, table.RowBits(width));
            Assert.Equal((11 * (4 * width + 3) + 7) / 8, table.PackedBytes(width).Length);
        }
        Assert.Equal(new[] { 1, 2, 5, 17, 31, 62 }, table.Widths);
        Assert.True(table.RowBits(62) > 64);
    }

    [Fact]
    public void Full64BitFixedFieldsRemainIndependentWithin128BitRows()
    {
        var table = new FamilyTable(new([new(0, 0), new(1, 1)], []));
        var first = table.Append(64, ulong.MaxValue, 0x8000000000000000UL);
        var second = table.Append(64, 0, ulong.MaxValue);
        Assert.Equal(128, table.RowBits(64));
        Assert.Equal(256L, table.PayloadBitLength(64));
        Assert.Equal(ulong.MaxValue, table.ReadRaw(first, 0));
        Assert.Equal(0x8000000000000000UL, table.ReadRaw(first, 1));
        Assert.Equal(0UL, table.ReadRaw(second, 0));
        Assert.Equal(ulong.MaxValue, table.ReadRaw(second, 1));
        Assert.Equal((byte)7, table.ReadCoarse(first, 1));
        Assert.Equal((byte)0, table.ReadCoarse(second, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyTemplate([new(0, 2)], []).RowBits(64));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyTemplate([new(2, 0)], []).RowBits(63));
    }

    [Fact]
    public void InvalidAppendDoesNotChangeExistingPayloadOrCreateAnEmptyWidthGroup()
    {
        var table = new FamilyTable(new([new(0, 0), new(0, 2)], []));
        var entry = table.Append(1, 1, 3);
        var before = table.PackedBytes(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Append(1, 0, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Append(2, 0, 8));
        Assert.Throws<ArgumentException>(() => table.Append(3, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Append(64, 0, 0));
        Assert.Equal(new[] { 1 }, table.Widths);
        Assert.Equal(1, table.Count(1));
        Assert.Equal(0, table.Count(2));
        Assert.Equal(before, table.PackedBytes(1));
        Assert.Equal(3UL, table.ReadRaw(entry, 1));
        Assert.Equal(new FamilyEntry(1, 1), table.Append(1, 0, 2));
    }

    [Fact]
    public void ConstructorAndDiagnosticCopiesCannotRewriteTheFamilyOrItsEntries()
    {
        var fields = new FamilyField[] { new(1, 1) };
        var links = new FamilyLink[] { new(0, 1, 0) };
        var template = new FamilyTemplate(fields, links);
        var table = new FamilyTable(template);
        var input = new ulong[] { 1 };
        var entry = table.Append(1, input);
        fields[0] = new(2, 0);
        links[0] = new(1, 0, 0);
        input[0] = 0;
        Array.Fill(template.FieldBytes(), (byte)0);
        Array.Fill(template.LinkBytes(), (byte)0);
        Array.Fill(table.PackedBytes(1), (byte)0);
        table.Widths[0] = 42;
        var read = table.ReadLinks(entry);
        read[0] = default;

        Assert.Equal(new FamilyField(1, 1), template.Field(0));
        Assert.Equal(new FamilyLink(0, 1, 0), template.Link(0));
        Assert.Equal(new[] { 1 }, table.Widths);
        Assert.Equal((byte)7, table.ReadCoarse(entry, 0));
        Assert.Equal(0, table.ReadLinks(entry)[0].Start.Local);
        Assert.Equal(1, table.ReadLinks(entry)[0].End.Local);
    }

    [Fact]
    public void MissingEntriesFieldsAndLinksCannotReadPaddingAsParticipation()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0)]));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.ReadLinks(new(1, 0)));
        var entry = table.Append(1, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => table.ReadRaw(new(1, 1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.ReadRaw(new(1, -1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.ReadRaw(new(2, 0), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.ReadRaw(entry, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.ReadRaw(entry, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.Template.Link(1));
        Assert.Equal((byte)0, table.ReadCoarse(entry, 0));
    }

    private static FamilyTable Make(params FamilyLink[] links)
    {
        var table = new FamilyTable(new([new(0, 0)], links));
        table.Append(1, 1);
        return table;
    }
}
