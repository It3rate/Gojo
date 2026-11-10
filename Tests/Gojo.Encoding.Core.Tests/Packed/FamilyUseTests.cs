using Gojo.Encoding.Core.Packed;

namespace Gojo.Encoding.Core.Tests.Packed;

public sealed class FamilyUseTests
{
    // Family: one fixed positive field, one [0,1,field0] link, one live value1.
    // Selected entry [width1,index0]; groups [forward], [reverse], [].
    private static byte[] Literal() =>
        [1, 10, 1, 1, 1, 1, 0, 2, 1, 1, 1, 1, 1, 0, 2, 3, 2, 0x94, 0x0A];

    [Fact]
    public void SharedSeamIsOneLinkWithOpposedUsesRatherThanEqualEndpointsOrFields()
    {
        var table = new FamilyTable(new([new(2, 0), new(2, 0), new(2, 0), new(2, 0)],
            [new(0, 1, 0), new(1, 2, 1), new(2, 0, 1), new(0, 3, 2), new(3, 4, 2),
             new(4, 1, 2), new(0, 1, 0), new(0, 1, 3)]));
        var entry = table.Append(1, 7, 7, 7, 7);
        var uses = new FamilyUseTable(table.Template, [0, 2, 4], [1, 6, 8, 10], [13, 6, 8, 10], [15, 6, 8, 10]);
        var roof = uses.ReadGroup(table, entry, 0)[0];
        var room = uses.ReadGroup(table, entry, 1)[0];
        var duplicate = uses.ReadGroup(table, entry, 2)[0];
        var differentField = uses.ReadGroup(table, entry, 3)[0];

        Assert.Equal(roof.Link, room.Link);
        Assert.NotEqual(roof.Position, room.Position);
        Assert.NotEqual(roof.Reversed, room.Reversed);
        Assert.Equal(roof.Span.Start, room.Span.End);
        Assert.Equal(roof.Span.End, room.Span.Start);
        Assert.Equal(roof.Span.Value, room.Span.Value);
        Assert.Equal(room.Span, duplicate.Span);
        Assert.NotEqual(room.Link, duplicate.Link);
        Assert.Equal(room.Span.Start, differentField.Span.Start);
        Assert.Equal(room.Span.End, differentField.Span.End);
        Assert.NotEqual(room.Span.Value, differentField.Span.Value);
        Assert.Equal(table.ReadCoarse(entry, 0), table.ReadCoarse(entry, 3));
        for (var group = 0; group < 4; group++) Assert.True(uses.IsClosed(table, entry, group));

        var boundary = uses.ReadGroup(table, entry, 0).Concat(uses.ReadGroup(table, entry, 1));
        Assert.Equal(0, boundary.Where(u => u.Link == roof.Link).Sum(u => u.Reversed ? -1 : 1));
        var lookalikeBoundary = uses.ReadGroup(table, entry, 0).Concat(uses.ReadGroup(table, entry, 2));
        Assert.Equal(1, lookalikeBoundary.Where(u => u.Link == roof.Link).Sum(u => u.Reversed ? -1 : 1));
        Assert.Equal(7UL, table.ReadRaw(entry, 0));
    }

    [Fact]
    public void ReverseChangesOnlyTheSelectedOccurrenceViewAndPreservesStoredState()
    {
        var table = new FamilyTable(new([new(2, 0)], [new(0, 1, 0), new(2, 1, 0), new(2, 3, 0)]));
        var entry = table.Append(1, 7);
        var uses = new FamilyUseTable(table.Template, [0, 3, 4], [1]);
        var raw = table.PackedBytes(1);
        var packed = uses.UseBytes();
        var forward = uses.ReadGroup(table, entry, 0);
        var reverse = uses.ReadGroup(table, entry, 0, reverse: true);
        Assert.True(uses.IsContinuous(table, entry, 0));
        Assert.True(uses.IsContinuous(table, entry, 0, reverse: true));
        Assert.False(uses.IsClosed(table, entry, 0));
        for (var i = 0; i < forward.Length; i++)
        {
            var original = forward[^(i + 1)];
            Assert.Equal(original.Position, reverse[i].Position);
            Assert.Equal(original.Link, reverse[i].Link);
            Assert.NotEqual(original.Reversed, reverse[i].Reversed);
            Assert.Equal(original.Span.Start, reverse[i].Span.End);
            Assert.Equal(original.Span.End, reverse[i].Span.Start);
            Assert.Equal(original.Span.Value, reverse[i].Span.Value);
        }
        Assert.Equal(forward, uses.ReadGroup(table, entry, 0));
        Assert.True(uses.ReadGroup(table, entry, 1)[0].Reversed);
        Assert.Equal(packed, uses.UseBytes());
        Assert.Equal(raw, table.PackedBytes(1));
        Assert.Equal((byte)7, table.ReadCoarse(entry, 0));
    }

    [Fact]
    public void ClosureNeedsTheActualOrderedWalkAndDoesNotRequirePresentMagnitude()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0), new(1, 2, 0), new(2, 0, 0)]));
        var entry = table.Append(1, 0);
        var uses = new FamilyUseTable(table.Template, [0, 2, 4], [0, 4, 2], [], [0, 1]);
        Assert.True(uses.IsClosed(table, entry, 0));
        Assert.True(uses.IsClosed(table, entry, 0, reverse: true));
        Assert.False(uses.IsContinuous(table, entry, 1));
        Assert.False(uses.IsClosed(table, entry, 1));
        Assert.Empty(uses.ReadGroup(table, entry, 2));
        Assert.False(uses.IsContinuous(table, entry, 2));
        Assert.False(uses.IsClosed(table, entry, 2));
        Assert.True(uses.IsClosed(table, entry, 3)); // Out and back; not a polygon claim.
        Assert.Equal((byte)0, table.ReadCoarse(entry, 0));
        Assert.Equal(3, table.Template.LinkCount);
    }

    [Fact]
    public void PackedUsesAndRangesHaveLiteralMeaningsWithoutLabels()
    {
        var (table, entry, uses) = FamilyUseCodec.Decode(Literal());
        Assert.Same(table.Template, uses.Template);
        Assert.Equal(new FamilyEntry(1, 0), entry);
        Assert.Equal(2, uses.UseCount);
        Assert.Equal(3, uses.GroupCount);
        Assert.Equal(1, uses.UseBits);
        Assert.Equal(2, uses.RangeBits);
        Assert.Equal(new[] { 0, 1 }, uses.Range(0));
        Assert.Equal(new[] { 1, 2 }, uses.Range(1));
        Assert.Equal(new[] { 2, 2 }, uses.Range(2));
        Assert.Equal(0, uses.SignedUse(0));
        Assert.Equal(1, uses.SignedUse(1));
        Assert.Equal(new byte[] { 2 }, uses.UseBytes());
        Assert.Equal(new byte[] { 0x94, 0x0A }, uses.RangeBytes());
        Assert.Equal(0, uses.ReadGroup(table, entry, 0)[0].Span.Start.Local);
        Assert.Equal(1, uses.ReadGroup(table, entry, 1)[0].Span.Start.Local);
        Assert.Equal(Literal(), FamilyUseCodec.Encode(table, entry, uses));
    }

    [Fact]
    public void ConstructionAndDiagnosticsCannotRewriteUsesOrGroups()
    {
        var table = new FamilyTable(new([new(0, 0)], [new(0, 1, 0)]));
        var entry = table.Append(1, 1);
        var groups = new int[][] { [0], [1], [] };
        var uses = new FamilyUseTable(table.Template, groups);
        groups[0][0] = 1;
        groups[1] = [];
        Array.Fill(uses.UseBytes(), (byte)0);
        Array.Fill(uses.RangeBytes(), (byte)0);
        Array.Fill(uses.Range(0), 0);
        var read = uses.ReadGroup(table, entry, 0);
        read[0] = default;
        Assert.Equal(0, uses.SignedUse(0));
        Assert.Equal(1, uses.SignedUse(1));
        Assert.Equal(new[] { 0, 1 }, uses.Range(0));
        Assert.Equal(new[] { 1, 2 }, uses.Range(1));
        Assert.Equal(1, uses.ReadGroup(table, entry, 0)[0].Span.End.Local);
    }

    [Fact]
    public void OwnerEntryAndTemplateScopesRemainDistinct()
    {
        var template = new FamilyTemplate([new(0, 0)], [new(0, 1, 0)]);
        var first = new FamilyTable(template);
        var second = new FamilyTable(template);
        var sameShape = new FamilyTable(new([new(0, 0)], [new(0, 1, 0)]));
        var entry = first.Append(1, 1);
        var another = first.Append(1, 1);
        second.Append(1, 1);
        sameShape.Append(1, 1);
        var uses = new FamilyUseTable(template, [0], []);
        var span = uses.ReadGroup(first, entry, 0)[0].Span;
        Assert.NotEqual(span.Start, uses.ReadGroup(second, entry, 0)[0].Span.Start);
        Assert.NotEqual(span.Value, uses.ReadGroup(first, another, 0)[0].Span.Value);
        Assert.Throws<ArgumentException>(() => uses.ReadGroup(sameShape, entry, 0));
        Assert.Throws<ArgumentException>(() => FamilyUseCodec.Encode(sameShape, entry, uses));
        Assert.Throws<ArgumentOutOfRangeException>(() => uses.ReadGroup(first, new(1, 2), 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => uses.SignedUse(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => uses.Range(2));
    }

    [Fact]
    public void TransportPreservesAllEntriesButSelectsOneAndRebindsUsesToTheRestoredTemplate()
    {
        var table = new FamilyTable(new([new(0, 2), new(2, 0)],
            [new(0, 1, 0), new(1, 2, 1), new(2, 0, 0)]));
        table.Append(1, 3, 7);
        table.Append(5, 33, 97);
        var entry = table.Append(5, 0, 65);
        var groups = Enumerable.Range(0, 130).Select(i => i % 2 == 0 ? new[] { 0, 2, 4 } : new[] { 5, 3, 1 }).ToArray();
        var uses = new FamilyUseTable(table.Template, groups);
        var bytes = FamilyUseCodec.Encode(table, entry, uses);
        var restored = FamilyUseCodec.Decode(bytes);
        Assert.Equal(entry, restored.Entry);
        Assert.NotSame(table, restored.Table);
        Assert.Same(restored.Table.Template, restored.Uses.Template);
        Assert.Equal(new[] { 1, 5 }, restored.Table.Widths);
        Assert.Equal(table.PackedBytes(1), restored.Table.PackedBytes(1));
        Assert.Equal(table.PackedBytes(5), restored.Table.PackedBytes(5));
        Assert.Equal(130, restored.Uses.GroupCount);
        Assert.Equal(390, restored.Uses.UseCount);
        Assert.Equal(uses.UseBytes(), restored.Uses.UseBytes());
        Assert.Equal(uses.RangeBytes(), restored.Uses.RangeBytes());
        for (var group = 0; group < groups.Length; group++)
        {
            Assert.True(restored.Uses.IsClosed(restored.Table, restored.Entry, group));
            Assert.Equal(uses.Range(group), restored.Uses.Range(group));
        }
        Assert.NotEqual(uses.ReadGroup(table, entry, 0)[0].Span.Start,
            restored.Uses.ReadGroup(restored.Table, restored.Entry, 0)[0].Span.Start);
        Assert.Equal(bytes, FamilyUseCodec.Encode(restored.Table, restored.Entry, restored.Uses));
    }

    [Fact]
    public void BareFamilyAndEmptyGroupsCanRoundTripWithoutInventingUses()
    {
        var table = new FamilyTable(new([new(2, 0)], []));
        var entry = table.Append(1, 7);
        foreach (var groups in new[] { Array.Empty<int[]>(), new int[][] { [], [] } })
        {
            var uses = new FamilyUseTable(table.Template, groups);
            var restored = FamilyUseCodec.Decode(FamilyUseCodec.Encode(table, entry, uses));
            Assert.Equal(0, restored.Uses.UseCount);
            Assert.Equal(groups.Length, restored.Uses.GroupCount);
            Assert.Equal(0, restored.Table.Template.LinkCount);
            Assert.Equal((byte)7, restored.Table.ReadCoarse(restored.Entry, 0));
        }
    }

    [Fact]
    public void SixteenBitUseReferencesWorkAndLargerGrammarIsRejected()
    {
        var template = new FamilyTemplate([new(0, 0)], Enumerable.Repeat(new FamilyLink(0, 1, 0), 32768).ToArray());
        var table = new FamilyTable(template);
        var entry = table.Append(1, 1);
        var uses = new FamilyUseTable(template, [65535]);
        Assert.Equal(16, uses.UseBits);
        Assert.Equal(new byte[] { 255, 255 }, uses.UseBytes());
        Assert.Equal(32767, uses.ReadGroup(table, entry, 0)[0].Link);
        Assert.Equal(1, uses.ReadGroup(table, entry, 0)[0].Span.Start.Local);
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyUseTable(template, [65536]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyUseTable(template, new int[65536]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyUseTable(template, new int[65537][]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FamilyUseTable(template, [-1]));
    }

    [Fact]
    public void TruncationTrailingBytesAndInvalidEntryOrOuterFramingAreRejected()
    {
        var bytes = Literal();
        for (var length = 0; length < bytes.Length; length++)
        {
            var truncated = bytes[..length];
            Assert.Throws<FormatException>(() => FamilyUseCodec.Decode(truncated));
        }
        Assert.Throws<FormatException>(() => FamilyUseCodec.Decode([.. bytes, 0]));
        foreach (var (at, value) in new (int, byte)[] { (0, 2), (1, 9), (1, 11), (12, 0), (12, 2), (13, 1) })
        {
            var invalid = Literal();
            invalid[at] = value;
            Assert.Throws<FormatException>(() => FamilyUseCodec.Decode(invalid));
        }
        Assert.Throws<FormatException>(() => FamilyUseCodec.Decode([1, 0x8A, 0, .. bytes[2..]]));
        Assert.Throws<FormatException>(() => FamilyUseCodec.Decode([1, 255, 255, 255, 255, 7]));
        Assert.Throws<FormatException>(() => FamilyUseCodec.Decode([1, 255, 255, 255, 255, 8]));
        Assert.Throws<FormatException>(() => FamilyUseCodec.Decode([.. bytes[..14], 128, 128, 4, 0]));
        Assert.Throws<FormatException>(() => FamilyUseCodec.Decode([.. bytes[..14], 0, 129, 128, 4]));
    }

    [Fact]
    public void MissingLinksInvalidRangePartitionsAndNonzeroPaddingAreRejected()
    {
        // Three links forming a cycle, with one group [0,2,4].
        byte[] cycle = [1, 12, 1, 1, 3, 2, 0, 0x44, 0x22, 0, 1, 1, 1, 1, 1, 0, 3, 1, 0x10, 1, 0x0C];
        Assert.True(FamilyUseCodec.Decode(cycle).Uses.UseCount == 3);
        foreach (var (at, value) in new (int, byte)[]
        {
            (18, 0x16), // First use names missing link3, not one of the three links.
            (19, 0x81), // Use payload padding.
            (20, 0x8C), // Range payload padding.
            (20, 0x0D), // First range starts at1, leaving use0 ungrouped.
            (20, 0x08), // Range ends at2, leaving use2 ungrouped.
            (20, 0x03)  // Reversed range [3,0].
        })
        {
            var invalid = cycle.ToArray();
            invalid[at] = value;
            Assert.Throws<FormatException>(() => FamilyUseCodec.Decode(invalid));
        }
        foreach (var (at, value) in new (int, byte)[] { (16, 0x82), (17, 0x90), (18, 0x8A) })
        {
            var invalid = Literal();
            invalid[at] = value;
            Assert.Throws<FormatException>(() => FamilyUseCodec.Decode(invalid));
        }
    }
}
