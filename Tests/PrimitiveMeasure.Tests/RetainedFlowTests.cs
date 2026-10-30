using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class RetainedFlowTests
{
    private static byte Positive => LocalConnection.Link(2, 3);
    private static byte Negative => LocalConnection.Link(6, 7);
    private static ushort Both => LocalConnection.Pair(Positive, Negative);

    [Fact]
    public void BalancedAndEmptyZeroHaveDifferentActualParticipation()
    {
        long[] empty = [0, 0, 0, 0];
        long[] balanced = [5, 0, 5, 0];
        Assert.Equal(0, RetainedFlowFixture.Net(empty, 0));
        Assert.Equal(0, RetainedFlowFixture.Net(balanced, 0));
        Assert.Equal((byte)0x20, RetainedFlowFixture.Observe(empty));
        Assert.Equal((byte)0x64, RetainedFlowFixture.Observe(balanced));
        Assert.Equal(new byte[] { 0, 2 }, RetainedFlowFixture.PredictNet(0x20, 0));
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, RetainedFlowFixture.PredictNet(0x64, 0));
        Assert.Equal(new long[] { 5, 0, 5, 0 }, balanced); // Reads do not consume the opposition.
        Assert.Equal(0, RetainedFlowFixture.TransferPair(empty, Both));
        Assert.Equal(5, RetainedFlowFixture.TransferPair(balanced, Both));
        Assert.Equal(new long[] { 0, 5, 0, 5 }, balanced);
        Assert.Equal((byte)0xA8, RetainedFlowFixture.Observe(balanced));
        Assert.Equal(0, RetainedFlowFixture.Net(balanced, 0));
        Assert.Equal(0, RetainedFlowFixture.Net(balanced, 1));
        Assert.Equal(0, RetainedFlowFixture.TransferPair(balanced, Both));
        Assert.Equal(new long[] { 0, 5, 0, 5 }, balanced); // The same material cannot leave twice.
    }

    [Fact]
    public void TakingOnlyOneSideChangesTheSourceNetRead()
    {
        long[] cells = [5, 0, 5, 0];
        Assert.Equal(5, RetainedFlowFixture.Transfer(cells, Positive));
        Assert.Equal(new long[] { 0, 5, 5, 0 }, cells);
        Assert.Equal(-5, RetainedFlowFixture.Net(cells, 0));
        Assert.Equal(5, RetainedFlowFixture.Net(cells, 1));
        Assert.Equal((byte)0x68, RetainedFlowFixture.Observe(cells));
        Assert.Equal(0, RetainedFlowFixture.Transfer(cells, Positive));
        Assert.Equal(new long[] { 0, 5, 5, 0 }, cells);
    }

    [Fact]
    public void PairedTransferPreservesEachNetAndEachSignTotal()
    {
        for (long positive = 0; positive <= 7; positive++)
            for (long negative = 0; negative <= 7; negative++)
            {
                long[] cells = [positive, 3, negative, 1];
                var moved = RetainedFlowFixture.TransferPair(cells, Both);
                Assert.Equal(Math.Min(positive, negative), moved);
                Assert.Equal(positive - negative, RetainedFlowFixture.Net(cells, 0));
                Assert.Equal(2, RetainedFlowFixture.Net(cells, 1));
                Assert.Equal(positive + 3, cells[0] + cells[1]);
                Assert.Equal(negative + 1, cells[2] + cells[3]);
                Assert.Equal(0, Math.Min(cells[0], cells[2]));
            }
        long[] example = [7, 0, 5, 0];
        Assert.Equal(5, RetainedFlowFixture.TransferPair(example, Both));
        Assert.Equal(new long[] { 2, 5, 0, 5 }, example);
        Assert.Equal((byte)0xAC, RetainedFlowFixture.Observe(example));
    }

    [Fact]
    public void EqualCoarseTilesCanStillHaveDifferentActualNetSigns()
    {
        (long Positive, long Negative, int Sign)[] worlds = [(5, 2, 1), (2, 2, 0), (2, 5, -1)];
        foreach (var world in worlds)
        {
            long[] cells = [world.Positive, 0, world.Negative, 0];
            Assert.Equal((byte)0x64, RetainedFlowFixture.Observe(cells));
            var prediction = RetainedFlowFixture.PredictNet(0x64, 0);
            var actual = RetainedFlowFixture.Net(cells, 0);
            Assert.Equal(world.Sign, Math.Sign(actual));
            Assert.Contains(FineFlowFixture.Observe(actual), prediction);
        }
        // Prediction reads actual O/D headers; this change supplies no common orientation.
        Assert.Empty(RetainedFlowFixture.PredictNet(0x74, 0));
    }

    [Fact]
    public void ZeroControlCanAdmitFlowFromADifferentSourceWithoutCreatingIt()
    {
        var fromB = LocalConnection.Link(3, 2);
        var whenAIsEmpty = LocalConnection.When(fromB, 2, false);
        long[] cells = [0, 5, 0, 0];
        Assert.Equal((byte)0x28, RetainedFlowFixture.Observe(cells));
        Assert.Equal(5, RetainedFlowFixture.TransferWhen(cells, whenAIsEmpty));
        Assert.Equal(new long[] { 5, 0, 0, 0 }, cells);
        Assert.Equal((byte)0x24, RetainedFlowFixture.Observe(cells));

        cells[1] += 2; // Explicit later input in the fine demonstration world.
        Assert.Equal(0, RetainedFlowFixture.TransferWhen(cells, whenAIsEmpty));
        Assert.Equal(new long[] { 5, 2, 0, 0 }, cells); // The gate is reread, not cached open.
        Assert.Equal(2, RetainedFlowFixture.TransferWhen(cells, LocalConnection.When(fromB, 2, true)));
        Assert.Equal(new long[] { 7, 0, 0, 0 }, cells);

        long[] empty = [0, 0, 0, 0];
        Assert.Equal(0, RetainedFlowFixture.TransferWhen(empty, whenAIsEmpty));
        Assert.Equal(new long[] { 0, 0, 0, 0 }, empty);
    }

    [Fact]
    public void WithdrawalClampsMagnitudeWithoutReleasingTheFixedSign()
    {
        foreach (var link in new[] { Positive, Negative })
        {
            long[] cells = [2, 0, 2, 0];
            var moved = RetainedFlowFixture.TransferUpTo(cells, link, 5);
            Assert.Equal(2, moved);
            Assert.Equal(3, 5 - moved); // Requested but unavailable participation stays explicit.
            var tile = RetainedFlowFixture.Observe(cells);
            Assert.Equal(0, tile & 0b00000010); // Positive shared D remains zero.
            Assert.Equal(0b00100000, tile & 0b00100000); // Negative shared D remains one.
            Assert.Equal(0, RetainedFlowFixture.TransferUpTo(cells, link, 5));
        }
        long[] limited = [5, 1, 0, 0];
        Assert.Equal(2, RetainedFlowFixture.TransferUpTo(limited, Positive, 2));
        Assert.Equal(new long[] { 3, 3, 0, 0 }, limited);
        Assert.Throws<ArgumentOutOfRangeException>(() => RetainedFlowFixture.TransferUpTo(limited, Positive, -1));
        Assert.Equal(new long[] { 3, 3, 0, 0 }, limited);
    }

    [Fact]
    public void EqualPositionsAndWrongAttachmentShapesCannotDuplicateFlow()
    {
        long[] cells = [5, 0, 5, 0];
        var before = cells.ToArray();
        Assert.Equal(0, RetainedFlowFixture.Transfer(cells, LocalConnection.Link(2, 2)));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.Transfer(cells, LocalConnection.Link(0, 3)));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.Transfer(cells, LocalConnection.Link(2, 6)));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.TransferPair(cells, LocalConnection.Pair(Negative, Positive)));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.TransferPair(cells, LocalConnection.Pair(Positive, Positive)));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.TransferPair(cells,
            LocalConnection.Pair(Positive, LocalConnection.Link(7, 6))));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.TransferPair(cells,
            LocalConnection.Pair(LocalConnection.Link(2, 2), LocalConnection.Link(6, 6))));
        Assert.Equal(before, cells);

        // A malformed material endpoint must be refused even behind a closed control.
        var closed = LocalConnection.When(LocalConnection.Link(0, 3), 2, false);
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.TransferWhen(cells, closed));
        Assert.Equal(before, cells);
    }

    [Fact]
    public void OverflowIsRefusedBeforeAnySourceOrReceiverChanges()
    {
        long[] single = [2, long.MaxValue, 0, 0];
        Assert.Throws<OverflowException>(() => RetainedFlowFixture.Transfer(single, Positive));
        Assert.Equal(new long[] { 2, long.MaxValue, 0, 0 }, single);

        long[] pair = [5, 0, 5, long.MaxValue];
        Assert.Throws<OverflowException>(() => RetainedFlowFixture.TransferPair(pair, Both));
        Assert.Equal(new long[] { 5, 0, 5, long.MaxValue }, pair);

        long[] conditional = [1, long.MaxValue, 0, 0];
        Assert.Throws<OverflowException>(() => RetainedFlowFixture.TransferWhen(conditional,
            LocalConnection.When(Positive, 2, true)));
        Assert.Equal(new long[] { 1, long.MaxValue, 0, 0 }, conditional);
    }

    [Fact]
    public void InvalidFineStatesAreNotInterpretedAsZeroOrAnotherSign()
    {
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.Observe([1, 0, -1, 0]));
        Assert.Throws<ArgumentException>(() => RetainedFlowFixture.Observe([0, 0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetainedFlowFixture.Net([0, 0, 0, 0], 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => RetainedFlowFixture.PredictNet(0x20, -1));
        Assert.Equal(long.MaxValue, RetainedFlowFixture.Net([long.MaxValue, 0, 0, 0], 0));
        Assert.Equal(-long.MaxValue, RetainedFlowFixture.Net([0, 0, long.MaxValue, 0], 0));
    }
}
