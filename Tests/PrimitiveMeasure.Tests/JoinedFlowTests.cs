using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class JoinedFlowTests
{
    [Fact]
    public void OpposedReadsFollowOneWordAndNeverOwnSimultaneousValue()
    {
        // One live word -> two derived reads, including changes of sign and orientation.
        byte shared = 0;
        foreach (byte next in new byte[] { 1, 3, 0, 2, 5, 7, 4, 6 })
        {
            shared = next;
            var positive = ThreeBitMeasure.ReadSide(shared, negative: false);
            var negative = ThreeBitMeasure.ReadSide(shared, negative: true);
            Assert.Equal(shared & 4, positive & 6);
            Assert.Equal((shared & 4) | 2, negative & 6);
            Assert.Equal(shared & 1, (positive & 1) + (negative & 1));
            Assert.False((positive & 1) != 0 && (negative & 1) != 0);
            Assert.Equal((shared & 3) == 1, (positive & 1) != 0);
            Assert.Equal((shared & 3) == 3, (negative & 1) != 0);
            Assert.Equal(next, shared); // Observation does not change the shared word.
        }
    }

    [Fact]
    public void IdenticalCoarseInputsHaveThreeDifferentActualSettlements()
    {
        // These finer amounts are explicit test apparatus, never fields on an ODV word.
        (long First, long Second, long Result, int Branch)[] worlds =
            [(5, -2, 3, 2), (2, -2, 0, 1), (2, -5, -3, 0)];
        foreach (var world in worlds)
        {
            long[] separate = [world.First, world.Second];
            byte firstRead = FineFlowFixture.Observe(separate[0]);
            byte secondRead = FineFlowFixture.Observe(separate[1]);
            Assert.Equal((byte)1, firstRead);
            Assert.Equal((byte)3, secondRead);
            var prediction = ThreeBitMeasure.Add(firstRead, secondRead);
            Assert.Equal(new byte[] { 0, 1, 2, 3 }, prediction);
            Assert.Equal(new long[] { world.First, world.Second }, separate);
            Assert.Equal(new int?[] { 0, 1, 2 }, prediction
                .Select(word => SignPassage.Select(word, SignPassage.All)).Distinct().Order());

            long actual = FineFlowFixture.Join(separate);
            byte observed = FineFlowFixture.Observe(actual);
            Assert.Equal(world.Result, actual);
            Assert.Equal(new long[] { 0, 0 }, separate);
            Assert.Contains(observed, prediction);
            Assert.Equal(world.Branch, SignPassage.Select(observed, SignPassage.All));
            Assert.Equal(new byte[] { 0, 1, 2, 3 }, prediction); // Execution does not rewrite prediction.
        }
    }

    [Fact]
    public void EverySmallActualMergeRefinesItsPredictionAndDrainsBothInputs()
    {
        for (long first = -6; first <= 6; first++)
            for (long second = -6; second <= 6; second++)
            {
                long[] separate = [first, second];
                var prediction = ThreeBitMeasure.Add(FineFlowFixture.Observe(first), FineFlowFixture.Observe(second));
                long joined = FineFlowFixture.Join(separate);
                Assert.Equal(first + second, joined);
                Assert.Contains(FineFlowFixture.Observe(joined), prediction);
                Assert.Equal(new long[] { 0, 0 }, separate);
            }
    }

    [Fact]
    public void EveryObservedWordSelectsOnlyItsOpenSignPosition()
    {
        int[] positions = [1, 2, 1, 0, 1, 2, 1, 0];
        for (byte word = 0; word < 8; word++)
            for (byte openings = 0; openings < 8; openings++)
            {
                int position = positions[word];
                int? expected = (openings & (1 << position)) != 0 ? position : null;
                Assert.Equal(expected, SignPassage.Select(word, openings));
            }
        // Both direction-marked zeros are real zero observations, not absent observations.
        Assert.Equal(1, SignPassage.Select(0, SignPassage.Zero));
        Assert.Equal(1, SignPassage.Select(2, SignPassage.Zero));
        Assert.Null(SignPassage.Select(1, SignPassage.Zero));
    }

    [Fact]
    public void ActualRoutingMovesValueOnceAndLeavesOtherPositionsUntouched()
    {
        foreach (long amount in new long[] { -3, 0, 3 })
        {
            // [flow, negative destination, zero destination, positive destination]
            long[] cells = [amount, -11, 17, 23];
            long[] before = cells.ToArray();
            int position = Math.Sign(amount) + 1;
            Assert.Equal(position, FineFlowFixture.Route(cells, SignPassage.All));
            Assert.Equal(0, cells[0]);
            Assert.Equal(before.Sum(), cells.Sum());
            for (var index = 1; index < 4; index++)
                Assert.Equal(before[index] + (index == position + 1 ? amount : 0), cells[index]);
            if (amount == 0) Assert.Equal(before, cells); // Selecting zero creates no material pulse.
        }
    }

    [Fact]
    public void ClosedConditionalPreservesFlowUntilItsOwnPositionOpens()
    {
        long[] cells = [-7, -2, 0, 5];
        long[] before = cells.ToArray();
        Assert.Null(FineFlowFixture.Route(cells, SignPassage.Positive));
        Assert.Equal(before, cells);
        Assert.Equal(0, FineFlowFixture.Route(cells, SignPassage.Negative));
        Assert.Equal(new long[] { 0, -9, 0, 5 }, cells);
        Assert.Equal(before.Sum(), cells.Sum());
        Assert.Null(FineFlowFixture.Route(cells, SignPassage.Negative));
        Assert.Equal(new long[] { 0, -9, 0, 5 }, cells);
    }

    [Fact]
    public void PassageFiltersTheSourceSignAndConservesEveryPassedOrBlockedAmount()
    {
        foreach (long source in new long[] { -5, 0, 5 })
            for (byte openings = 0; openings < 8; openings++)
            {
                long[] pair = [source, -2];
                bool open = (openings & (1 << (Math.Sign(source) + 1))) != 0;
                Assert.Equal(open, FineFlowFixture.Transfer(pair, openings));
                Assert.Equal(open ? new long[] { 0, source - 2 } : new long[] { source, -2 }, pair);
                Assert.Equal(source - 2, pair.Sum());
            }
    }

    [Fact]
    public void IndependentStoresCanBehaveLikeOneAfterTransferAndLaterSeparate()
    {
        long[] pair = [5, -2];
        long[] toJoin = pair.ToArray();
        long joined = FineFlowFixture.Join(toJoin);
        Assert.True(FineFlowFixture.Transfer(pair, SignPassage.All));
        Assert.Equal(new long[] { 0, joined }, pair);

        // A subsequent external input belongs to the first independent store.
        pair[0] = -5;
        Assert.False(FineFlowFixture.Transfer(pair, 0));
        Assert.Equal(new long[] { -5, 3 }, pair);
        Assert.Equal((byte)3, FineFlowFixture.Observe(pair[0]));
        Assert.Equal((byte)1, FineFlowFixture.Observe(pair[1]));

        // The corresponding merged flow still has just one occupied side.
        long[] joinedAndInput = [joined, -5];
        byte shared = FineFlowFixture.Observe(FineFlowFixture.Join(joinedAndInput));
        Assert.Equal((byte)0, ThreeBitMeasure.ReadSide(shared, negative: false));
        Assert.Equal((byte)3, ThreeBitMeasure.ReadSide(shared, negative: true));
    }

    [Fact]
    public void HostOverflowCannotPartiallyConsumeAnActualFlow()
    {
        long[] join = [long.MaxValue, 1];
        Assert.Throws<OverflowException>(() => FineFlowFixture.Join(join));
        Assert.Equal(new long[] { long.MaxValue, 1 }, join);

        long[] pair = [1, long.MaxValue];
        Assert.Throws<OverflowException>(() => FineFlowFixture.Transfer(pair, SignPassage.All));
        Assert.Equal(new long[] { 1, long.MaxValue }, pair);
        Assert.False(FineFlowFixture.Transfer(pair, 0));
        Assert.Equal(new long[] { 1, long.MaxValue }, pair);

        long[] cells = [-1, long.MinValue, 0, 0];
        Assert.Throws<OverflowException>(() => FineFlowFixture.Route(cells, SignPassage.All));
        Assert.Equal(new long[] { -1, long.MinValue, 0, 0 }, cells);
        Assert.Null(FineFlowFixture.Route(cells, SignPassage.Positive));
        Assert.Equal(new long[] { -1, long.MinValue, 0, 0 }, cells);
    }

    [Fact]
    public void MissingCommonOrientationDoesNotManufactureAZeroCondition()
    {
        var absentPrediction = ThreeBitMeasure.Add(1, 5);
        Assert.Empty(absentPrediction);
        Assert.Empty(absentPrediction.Select(word => SignPassage.Select(word, SignPassage.All)));
        Assert.Null(ThreeBitMeasure.Compare(1, 5));
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, ThreeBitMeasure.Add(1, ThreeBitMeasure.Reorient(5)));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.ReadSide(8, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignPassage.Select(8, SignPassage.All));
        Assert.Throws<ArgumentOutOfRangeException>(() => SignPassage.Select(1, 8));
    }
}
