using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OpposedConnectionFixtureTests
{
    [Fact]
    public void EqualCoarseInputsSettleToThreeDifferentNetSignsWithoutLosingOpposition()
    {
        var rows = new EndpointRows();
        var row = rows.Append(2, 0, 1, 2, 3);
        foreach (var (positive, negative, expectedPositive, expectedNegative, matched, word) in new[]
        {
            (5L, 2L, 3L, 0L, 2L, (byte)1),
            (2L, 5L, 0L, 3L, 2L, (byte)3),
            (5L, 5L, 0L, 0L, 5L, (byte)0)
        })
        {
            long[] cells = [positive, negative, 0, 0];
            var before = cells.ToArray();
            Assert.Equal(new byte[] { 0, 1, 2, 3 }, ThreeBitMeasure.Add(1, 3));
            Assert.Throws<InvalidOperationException>(() => OpposedConnectionFixture.ReadSettled(cells, rows, row, 0));
            Assert.Equal(before, cells);

            Assert.Equal(matched, OpposedConnectionFixture.Settle(cells, rows, row));
            Assert.Equal(new[] { expectedPositive, expectedNegative, matched, matched }, cells);
            Assert.Equal(positive, cells[0] + cells[2]);
            Assert.Equal(negative, cells[1] + cells[3]);
            Assert.Equal(word, OpposedConnectionFixture.ReadSettled(cells, rows, row, 0));
            Assert.Equal(new[] { 0, 1, 2, 3 }, rows.Read(row));
            Assert.Equal(new byte[] { 0xE4 }, rows.PackedBytes(4, 2));
        }
    }

    [Fact]
    public void ZeroRetainsDirectionMemoryButDoesNotForgetTheTwoEndpointRoles()
    {
        var rows = new EndpointRows();
        var row = rows.Append(2, 0, 1, 2, 3);
        long[] balanced = [5, 5, 0, 0];
        OpposedConnectionFixture.Settle(balanced, rows, row);
        long[] empty = [0, 0, 0, 0];
        for (byte previous = 0; previous < 8; previous++)
        {
            var expected = (byte)(previous & ~ThreeBitMeasure.Value);
            Assert.Equal(expected, OpposedConnectionFixture.ReadSettled(balanced, rows, row, previous));
            Assert.Equal(expected, OpposedConnectionFixture.ReadSettled(empty, rows, row, previous));
        }
        Assert.Equal(new long[] { 0, 0, 5, 5 }, balanced);
        Assert.Equal(new[] { 0, 1, 2, 3 }, rows.Read(row));
        Assert.NotEqual(rows.Read(row)[0], rows.Read(row)[1]);
        // The same zero word cannot tell empty stores from retained opposition,
        // or recover the connected pair if only that word is subsequently kept.
        Assert.Equal((byte)2, OpposedConnectionFixture.ReadSettled(balanced, rows, row, 3));
        Assert.Equal((byte)0, OpposedConnectionFixture.ReadSettled(balanced, rows, row, 1));
    }

    [Fact]
    public void ArbitraryLocalEndpointsRetainExistingStoresAndSettlementIsIdempotent()
    {
        var rows = new EndpointRows();
        var row = rows.Append(3, 6, 1, 4, 3);
        long[] cells = [-99, 5, 17, 4, 3, 19, 7];
        Assert.Equal(5L, OpposedConnectionFixture.Settle(cells, rows, row));
        Assert.Equal(new long[] { -99, 0, 17, 9, 8, 19, 2 }, cells);
        Assert.Equal((byte)5, OpposedConnectionFixture.ReadSettled(cells, rows, row, 6));
        var settled = cells.ToArray();
        Assert.Equal(0L, OpposedConnectionFixture.Settle(cells, rows, row));
        Assert.Equal(settled, cells);
        Assert.Equal(10L, cells[6] + cells[4]);
        Assert.Equal(9L, cells[1] + cells[3]);
    }

    [Fact]
    public void BothDestinationOverflowsAreRejectedBeforeAnyStoreChanges()
    {
        var rows = new EndpointRows();
        var row = rows.Append(2, 0, 1, 2, 3);
        foreach (var cells in new[]
        {
            new long[] { 5, 2, long.MaxValue, 0 },
            new long[] { 5, 2, 0, long.MaxValue }
        })
        {
            var before = cells.ToArray();
            Assert.Throws<OverflowException>(() => OpposedConnectionFixture.Settle(cells, rows, row));
            Assert.Equal(before, cells);
        }
    }

    [Fact]
    public void AliasedOutOfRangeNegativeAndWrongArityParticipantsCannotPartlySettle()
    {
        var rows = new EndpointRows();
        var alias = rows.Append(2, 0, 1, 0, 3);
        var outside = rows.Append(3, 0, 1, 2, 4);
        var shortRow = rows.Append(2, 0, 1);
        var valid = rows.Append(2, 0, 1, 2, 3);
        long[] cells = [5, 2, 0, 0];
        var before = cells.ToArray();
        Assert.Throws<ArgumentException>(() => OpposedConnectionFixture.Settle(cells, rows, alias));
        Assert.Throws<ArgumentOutOfRangeException>(() => OpposedConnectionFixture.Settle(cells, rows, outside));
        Assert.Throws<ArgumentException>(() => OpposedConnectionFixture.Settle(cells, rows, shortRow));
        Assert.Equal(before, cells);
        long[] negative = [5, 2, -1, 0];
        Assert.Throws<ArgumentException>(() => OpposedConnectionFixture.Settle(negative, rows, valid));
        Assert.Equal(new long[] { 5, 2, -1, 0 }, negative);
        Assert.Throws<ArgumentOutOfRangeException>(() => OpposedConnectionFixture.ReadSettled(new long[4], rows, valid, 8));
    }
}
