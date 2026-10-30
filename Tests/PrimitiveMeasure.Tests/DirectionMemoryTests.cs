using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class DirectionMemoryTests
{
    [Fact]
    public void SameZeroObservationRetainsDifferentPriorDirections()
    {
        Assert.Equal((byte)0b000, DirectionMemory.Update(0b001, 0b000));
        Assert.Equal((byte)0b010, DirectionMemory.Update(0b011, 0b000));
        // Input zero's direction is not a new resolved direction.
        Assert.Equal((byte)0b000, DirectionMemory.Update(0b001, 0b010));
        Assert.Equal((byte)0b010, DirectionMemory.Update(0b011, 0b010));
        Assert.Equal((byte)0b010, DirectionMemory.Update(0b010, 0b000));
    }

    [Fact]
    public void ResolvedCounterDirectionChangesStateWithoutChangingTheObservation()
    {
        for (byte orientation = 0; orientation <= 4; orientation += 4)
        {
            byte previous = (byte)(orientation | 1);
            byte observed = (byte)(orientation | 3);
            byte next = DirectionMemory.Update(previous, observed)!.Value;
            Assert.Equal(observed, next);
            Assert.Equal((byte)(orientation | 1), previous);
            Assert.Equal((byte)(orientation | 3), observed);
            Assert.Equal((byte)(orientation | 2), DirectionMemory.Update(next, orientation));
            Assert.Equal((byte)(orientation | 1), DirectionMemory.Update(next, (byte)(orientation | 1)));
        }
    }

    [Fact]
    public void ReaderResolutionAndRetainedDirectionProduceDifferentConcreteTraces()
    {
        long[] samples = [3, 1, 0, -1, -3, 0, 1, 3];
        Assert.Equal(new byte[] { 1, 1, 0, 3, 3, 2, 1, 1 }, Trace(samples, 0));
        Assert.Equal(new byte[] { 1, 0, 0, 0, 3, 2, 2, 1 }, Trace(samples, 2));
        // Resolution alone emits the same unresolved word for either weak sign.
        Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(-1, 2));
        Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(1, 2));
        Assert.Equal((byte)2, DirectionMemory.Update(2, DirectionMemory.ObserveAtThreshold(1, 2)));
        Assert.Equal((byte)0, DirectionMemory.Update(0, DirectionMemory.ObserveAtThreshold(-1, 2)));
    }

    [Fact]
    public void ThresholdBoundaryIsExplicitAndTrueZeroNeverBecomesResolved()
    {
        Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(2, 2));
        Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(-2, 2));
        Assert.Equal((byte)1, DirectionMemory.ObserveAtThreshold(3, 2));
        Assert.Equal((byte)3, DirectionMemory.ObserveAtThreshold(-3, 2));
        foreach (long threshold in new long[] { 0, 1, 2, long.MaxValue })
            Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(0, threshold));
        foreach (long amount in new long[] { long.MinValue, -1, 0, 1, long.MaxValue })
            Assert.Equal(FineFlowFixture.Observe(amount), DirectionMemory.ObserveAtThreshold(amount, 0));
    }

    [Fact]
    public void FullHostRangeIsReadWithoutOverflowOrAnInventedFineSign()
    {
        Assert.Equal((byte)3, DirectionMemory.ObserveAtThreshold(long.MinValue, long.MaxValue));
        Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(-long.MaxValue, long.MaxValue));
        Assert.Equal((byte)0, DirectionMemory.ObserveAtThreshold(long.MaxValue, long.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => DirectionMemory.ObserveAtThreshold(0, -1));
    }

    [Fact]
    public void DifferentOrientationSelectsNoTransitionAndInvalidBitsAreRefused()
    {
        Assert.Null(DirectionMemory.Update(1, 7));
        Assert.Null(DirectionMemory.Update(3, 4));
        Assert.Equal((byte)1, DirectionMemory.Update(3, ThreeBitMeasure.Reorient(7)));
        Assert.Throws<ArgumentOutOfRangeException>(() => DirectionMemory.Update(8, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => DirectionMemory.Update(0, 8));
    }

    [Fact]
    public void FixedFrameUpdatesMemoryAtItsActualBitsAndPreservesTheInput()
    {
        // Low-to-high fields: input 000, memory 011 -> input 000, memory 010.
        byte[] frame = [0x18];
        Assert.True(DirectionMemory.UpdateFrame(frame));
        Assert.Equal((byte)0x10, frame[0]);
        Assert.Equal((byte)0, (byte)(frame[0] & 7));
        // Introduce input 001 while preserving the same memory field 010.
        frame[0] |= 1;
        Assert.Equal((byte)0x11, frame[0]);
        Assert.True(DirectionMemory.UpdateFrame(frame));
        Assert.Equal((byte)0x09, frame[0]); // input 001, memory 001
        Assert.Equal((byte)1, (byte)(frame[0] & 7));
        Assert.True(DirectionMemory.UpdateFrame(frame)); // Selected but unchanged.
        Assert.Equal((byte)0x09, frame[0]);
    }

    [Fact]
    public void CoarseTraceKeepsItsEntireMemoryInsideTheSixBitFrame()
    {
        byte[] frame = [0]; // Explicit initial positive memory, no resolved magnitude.
        long[] samples = [3, 1, 0, -1, -3, 0, 1, 3];
        byte[] expectedMemory = [1, 0, 0, 0, 3, 2, 2, 1];
        for (var index = 0; index < samples.Length; index++)
        {
            var input = DirectionMemory.ObserveAtThreshold(samples[index], 2);
            frame[0] = (byte)((frame[0] & 0b111000) | input);
            Assert.True(DirectionMemory.UpdateFrame(frame));
            Assert.Equal(input, (byte)(frame[0] & 7));
            Assert.Equal(expectedMemory[index], (byte)(frame[0] >> 3));
            Assert.Equal(0, frame[0] & 0b11000000);
        }
    }

    [Fact]
    public void FrameOrientationMismatchAndMalformedStorageNeverPartiallyWrite()
    {
        byte[] frame = [0x21]; // Input 001, memory 100: different orientations.
        Assert.False(DirectionMemory.UpdateFrame(frame));
        Assert.Equal((byte)0x21, frame[0]);
        foreach (byte extraBits in new byte[] { 0x40, 0x80, 0xC0 })
        {
            byte[] malformed = [(byte)(0x18 | extraBits)];
            var before = malformed[0];
            Assert.Throws<ArgumentException>(() => DirectionMemory.UpdateFrame(malformed));
            Assert.Equal(before, malformed[0]);
        }
        Assert.Throws<ArgumentException>(() => DirectionMemory.UpdateFrame([]));
        byte[] tooLong = [0x18, 0];
        Assert.Throws<ArgumentException>(() => DirectionMemory.UpdateFrame(tooLong));
        Assert.Equal(new byte[] { 0x18, 0 }, tooLong);
    }

    private static byte[] Trace(long[] samples, long threshold)
    {
        byte state = 0; // Explicit initial positive direction at unresolved magnitude.
        return samples.Select(amount => state = DirectionMemory.Update(state,
            DirectionMemory.ObserveAtThreshold(amount, threshold))!.Value).ToArray();
    }
}
