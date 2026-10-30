using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class ThreeBitMeasureTests
{
    [Fact]
    public void EightWordsNeedExactlyThreePackedBytesAndKeepStoredOrder()
    {
        byte[] words = [0, 1, 2, 3, 4, 5, 6, 7];
        var packed = ThreeBitMeasure.Pack(words);
        Assert.Equal(new byte[] { 0x88, 0xC6, 0xFA }, packed);
        for (var index = 0; index < words.Length; index++) Assert.Equal(words[index], ThreeBitMeasure.At(packed, index));
        // Exercise every bit offset and final partial byte, including repeated words.
        for (var length = 1; length <= 24; length++)
        {
            var sequence = Enumerable.Range(0, length).Select(i => (byte)((i * 5) & 7)).ToArray();
            var bytes = ThreeBitMeasure.Pack(sequence);
            Assert.Equal((length * 3 + 7) / 8, bytes.Length);
            for (var index = 0; index < length; index++) Assert.Equal(sequence[index], ThreeBitMeasure.At(bytes, index));
        }
    }

    [Fact]
    public void AllFourOrientationDirectionPairsRetainTheirZeroAndNonzeroWords()
    {
        for (byte word = 0; word < 8; word++)
            Assert.Equal(word, ThreeBitMeasure.Encode((word & 4) != 0, (word & 2) != 0, (word & 1) != 0));
        Assert.NotEqual(0b000, 0b010); // Exact encoding equality retains zero's direction.
        Assert.Equal(0, ThreeBitMeasure.Compare(0b000, 0b010)); // This magnitude read sees both as zero.
    }

    [Fact]
    public void ComparisonReadsOnlyTheAvailableSameOrientationDistinctions()
    {
        int[] classes = [0, 1, 0, -1];
        for (byte a = 0; a < 8; a++)
            for (byte b = 0; b < 8; b++)
                if (((a ^ b) & 4) != 0) Assert.Null(ThreeBitMeasure.Compare(a, b));
                else Assert.Equal(classes[a & 3].CompareTo(classes[b & 3]), ThreeBitMeasure.Compare(a, b));
        Assert.Equal(0, ThreeBitMeasure.Compare(1, 1));
        Assert.Equal(1, ThreeBitMeasure.Compare(1, 0));
        Assert.Equal(-1, ThreeBitMeasure.Compare(3, 2));
    }

    [Fact]
    public void SameDirectionCannotRevealMoreMagnitudeAndZeroAddsNothing()
    {
        for (byte word = 0; word < 8; word++)
        {
            Assert.Equal(new[] { word }, ThreeBitMeasure.Add(word, word));
            if ((word & 1) == 0) continue;
            var zero = (byte)(word & 6);
            Assert.Equal(new[] { word }, ThreeBitMeasure.Add(word, zero));
            Assert.Equal(new[] { word }, ThreeBitMeasure.Add(zero, word));
        }
    }

    [Fact]
    public void OppositionCannotChooseCancellationOrAnArbitraryZeroDirection()
    {
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, ThreeBitMeasure.Add(1, 3));
        Assert.Equal(new byte[] { 4, 5, 6, 7 }, ThreeBitMeasure.Add(5, 7));
        Assert.Equal(new byte[] { 0, 2 }, ThreeBitMeasure.Add(0, 2));
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, ThreeBitMeasure.Subtract(1, 1));
        Assert.Equal(new byte[] { 4, 5, 6, 7 }, ThreeBitMeasure.Subtract(5, 5));
    }

    [Fact]
    public void EveryAdditionAgreesWithPossibleSignsWithoutRuntimeHiddenMagnitudes()
    {
        // Concrete numbers appear only in this independent test oracle. They
        // supply examples of every possible sign outcome, never runtime state.
        int[][] realizations = [[0], [1, 2], [0], [-1, -2]];
        for (byte a = 0; a < 8; a++)
            for (byte b = 0; b < 8; b++)
            {
                var answers = ThreeBitMeasure.Add(a, b);
                Assert.Equal(answers.Order(), ThreeBitMeasure.Add(b, a).Order());
                if (((a ^ b) & 4) != 0) { Assert.Empty(answers); continue; }
                var expectedSigns = (from x in realizations[a & 3] from y in realizations[b & 3]
                    select Math.Sign(x + y)).Distinct().Order();
                var actualSigns = answers.Select(w => (w & 1) == 0 ? 0 : (w & 2) == 0 ? 1 : -1).Distinct().Order();
                Assert.Equal(expectedSigns, actualSigns);
                Assert.All(answers, w => Assert.Equal(a & 4, w & 4));
            }
    }

    [Fact]
    public void ReorientationAndDirectionReversalAreDifferentInvolutions()
    {
        for (byte word = 0; word < 8; word++)
        {
            Assert.Equal(word, ThreeBitMeasure.Reorient(ThreeBitMeasure.Reorient(word)));
            Assert.Equal(word, ThreeBitMeasure.ReverseDirection(ThreeBitMeasure.ReverseDirection(word)));
            Assert.Equal(word & 1, ThreeBitMeasure.Reorient(word) & 1);
            Assert.NotEqual(ThreeBitMeasure.Reorient(word), ThreeBitMeasure.ReverseDirection(word));
        }
        Assert.Equal(0b111, ThreeBitMeasure.Reorient(0b001));
        Assert.Equal(0b011, ThreeBitMeasure.ReverseDirection(0b001));
        for (byte a = 0; a < 8; a++)
            for (byte b = 0; b < 8; b++)
            {
                var read = ThreeBitMeasure.Compare(a, b);
                Assert.Equal(read is null ? null : -read, ThreeBitMeasure.Compare(ThreeBitMeasure.Reorient(a), ThreeBitMeasure.Reorient(b)));
                Assert.Equal(ThreeBitMeasure.Add(a, b).Select(ThreeBitMeasure.Reorient).Order(),
                    ThreeBitMeasure.Add(ThreeBitMeasure.Reorient(a), ThreeBitMeasure.Reorient(b)).Order());
            }
    }

    [Fact]
    public void ExtraBitsAreRefusedAndUnknownIsNeverManufacturedAsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.Add(8, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.Compare(0, 255));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.Pack([1, 8]));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.At([0], -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ThreeBitMeasure.At([0], 2));
        Assert.Empty(ThreeBitMeasure.Add(1, 5));
        Assert.Null(ThreeBitMeasure.Compare(1, 5));
    }
}
