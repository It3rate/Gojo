using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class EndpointRowsTests
{
    [Fact]
    public void ThreeAndFourEndedRowsHaveExactHeaderlessBytes()
    {
        var rows = new EndpointRows();
        var first = rows.Append(2, 0, 1, 2);
        var second = rows.Append(2, 3, 2, 1);
        Assert.Equal(new ConnectionRow(3, 2, 0), first);
        Assert.Equal(new ConnectionRow(3, 2, 1), second);
        Assert.Equal(6L, EndpointRows.BitPosition(second));
        Assert.Equal(new byte[] { 0xE4, 0x06 }, rows.PackedBytes(3, 2));
        Assert.Equal(new[] { 0, 1, 2 }, rows.Read(first));
        Assert.Equal(new[] { 3, 2, 1 }, rows.Read(second));

        var four = rows.Append(2, 3, 0, 2, 1);
        rows.Append(2, 0, 3, 1, 2);
        Assert.Equal(new byte[] { 0x63, 0x9C }, rows.PackedBytes(4, 2));
        Assert.Equal(new[] { 3, 0, 2, 1 }, rows.Read(four));
        Assert.Equal(2, rows.Count(3, 2));
        Assert.Equal(2, rows.Count(4, 2));
    }

    [Fact]
    public void RowsCrossByteBoundariesWithoutPaddingBetweenThem()
    {
        var rows = new EndpointRows();
        var first = rows.Append(3, 7, 0, 5);
        var second = rows.Append(3, 1, 2, 3);
        Assert.Equal(new byte[] { 0x47, 0xA3, 0x01 }, rows.PackedBytes(3, 3));
        Assert.Equal(9L, EndpointRows.BitPosition(second));
        Assert.Equal(new[] { 7, 0, 5 }, rows.Read(first));
        Assert.Equal(new[] { 1, 2, 3 }, rows.Read(second));
    }

    [Fact]
    public void EndpointOrderAndRepeatedRolesSurviveEvenAtTheSameLocalPosition()
    {
        var rows = new EndpointRows();
        var point = rows.Append(2, 0);
        var repeated = rows.Append(2, 0, 0);
        var pair = rows.Append(2, 0, 1);
        var reversed = rows.Append(2, 1, 0);
        var anotherPair = rows.Append(2, 0, 1);

        Assert.Equal(new[] { 0 }, rows.Read(point));
        Assert.Equal(new[] { 0, 0 }, rows.Read(repeated));
        Assert.Equal(new[] { 0, 1 }, rows.Read(pair));
        Assert.Equal(new[] { 1, 0 }, rows.Read(reversed));
        Assert.NotEqual(pair, anotherPair);
        Assert.Equal(rows.Read(pair), rows.Read(anotherPair));
        Assert.Equal(1, rows.Count(1, 2));
        Assert.Equal(4, rows.Count(2, 2));
    }

    [Fact]
    public void EverySupportedLayoutPreservesPositionsAndOlderRowsAcrossAppends()
    {
        var rows = new EndpointRows();
        for (var arity = 1; arity <= 8; arity++)
            for (var width = 1; width <= 16; width++)
            {
                var mask = (1 << width) - 1;
                var expected = Enumerable.Range(0, 19)
                    .Select(index => Enumerable.Range(0, arity)
                        .Select(role => index == 0 ? mask : ((index * 0x45D9) ^ (role * 0xA53)) & mask)
                        .ToArray()).ToArray();
                for (var index = 0; index < expected.Length; index++)
                    Assert.Equal(new ConnectionRow(arity, width, index), rows.Append(width, expected[index]));
                for (var index = 0; index < expected.Length; index++)
                {
                    var row = new ConnectionRow(arity, width, index);
                    Assert.Equal(expected[index], rows.Read(row));
                    Assert.Equal((long)index * arity * width, EndpointRows.BitPosition(row));
                }
                Assert.Equal(expected.Length, rows.Count(arity, width));
                Assert.Equal((expected.Length * arity * width + 7) / 8, rows.PackedBytes(arity, width).Length);
            }
    }

    [Fact]
    public void InvalidAppendCannotChangeExistingRowsOrAllocateAPhantomRow()
    {
        var rows = new EndpointRows();
        var valid = rows.Append(2, 1, 2, 3);
        var before = rows.PackedBytes(3, 2);
        Assert.Throws<ArgumentNullException>(() => rows.Append(2, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(2, new int[9]));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(17, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(2, 1, -1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(2, 1, 4, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Append(16, 65536));
        Assert.Equal(before, rows.PackedBytes(3, 2));
        Assert.Equal(new[] { 1, 2, 3 }, rows.Read(valid));
        Assert.Equal(1, rows.Count(3, 2));
        Assert.Equal(0, rows.Count(1, 16));
        Assert.Equal(new ConnectionRow(1, 16, 0), rows.Append(16, 65535));
    }

    [Fact]
    public void MissingRowsAndTrailingPaddingAreNotConnections()
    {
        var rows = new EndpointRows();
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Read(new(1, 1, 0)));
        var zero = rows.Append(1, 0);
        Assert.Equal(new[] { 0 }, rows.Read(zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Read(new(1, 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Read(new(1, 1, -1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Read(new(0, 1, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Read(new(1, 17, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.Count(9, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => rows.PackedBytes(1, 0));
        Assert.Equal(274877906816L, EndpointRows.BitPosition(new(8, 16, int.MaxValue)));
    }

    [Fact]
    public void RowPositionsAreOwnerLocalAndReturnedArraysCannotRewriteThem()
    {
        var first = new EndpointRows();
        var second = new EndpointRows();
        var position = first.Append(3, 1, 2);
        Assert.Equal(position, second.Append(3, 2, 1));
        var read = first.Read(position);
        var bytes = first.PackedBytes(2, 3);
        read[0] = 7;
        bytes[0] = 0;
        Assert.Equal(new[] { 1, 2 }, first.Read(position));
        Assert.Equal(new[] { 2, 1 }, second.Read(position));
        Assert.Equal(new byte[] { 0x11 }, first.PackedBytes(2, 3));
        Assert.Equal(new byte[] { 0x0A }, second.PackedBytes(2, 3));
    }
}
