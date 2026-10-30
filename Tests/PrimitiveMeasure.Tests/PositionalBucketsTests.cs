using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class PositionalBucketsTests
{
    [Fact]
    public void FixedBitsComeOnlyFromPositionAndHaveNoPackedHeader()
    {
        var buckets = new PositionalBuckets();
        for (var orientation = 0; orientation < 2; orientation++)
            for (var direction = 0; direction < 2; direction++)
            {
                var first = buckets.Append(orientation, direction, 1, 1);
                var zero = buckets.Append(orientation, direction, 1, 0);
                var third = buckets.Append(orientation, direction, 1, 1);
                Assert.Equal(new byte[] { 0b00000101 }, buckets.PackedBytes(orientation, direction, 1));
                Assert.Equal(new SlotAddress(orientation, direction, 1, 0), first);
                Assert.Equal(2L, PositionalBuckets.BitPosition(third));
                Assert.Equal((byte)((orientation << 2) | (direction << 1) | 1), Read(buckets, first));
                Assert.Equal((byte)((orientation << 2) | (direction << 1)), Read(buckets, zero));
                Assert.True(buckets.TryWrite(first, 0));
                Assert.Equal((byte)((orientation << 2) | (direction << 1)), Read(buckets, first));
            }
    }

    [Fact]
    public void OwnedDirectionAndOrientationFollowValueBitsWithoutHeaderPadding()
    {
        var buckets = new PositionalBuckets();
        var first = buckets.Append(1, 2, 2, 1);
        buckets.Append(1, 2, 2, 2);
        buckets.Append(1, 2, 2, 3);
        Assert.Equal(new byte[] { 0x39 }, buckets.PackedBytes(1, 2, 2));
        Assert.Equal((byte)5, Read(buckets, first));
        Assert.True(buckets.TryWrite(first, 3));
        Assert.Equal((byte)7, Read(buckets, first));

        for (ulong word = 0; word < 8; word++) buckets.Append(2, 0, 3, word);
        Assert.Equal(new byte[] { 0x88, 0xC6, 0xFA }, buckets.PackedBytes(2, 0, 3));
        var free = new SlotAddress(2, 0, 3, 7);
        Assert.True(buckets.TryWrite(free, 1));
        Assert.Equal((byte)1, Read(buckets, free));
    }

    [Fact]
    public void WidthsThrough64BitsPreserveRowsAcrossByteBoundariesAndWrites()
    {
        var buckets = new PositionalBuckets();
        foreach (var (orientation, direction) in Layouts())
        {
            var minimum = orientation == 2 ? 3 : direction == 2 ? 2 : 1;
            for (var width = minimum; width <= 64; width++)
            {
                var mask = width == 64 ? ulong.MaxValue : (1UL << width) - 1;
                var expected = Enumerable.Range(0, 19)
                    .Select(i => (unchecked((ulong)i * 0x9E3779B97F4A7C15UL)) & mask).ToArray();
                for (var i = 0; i < expected.Length; i++)
                {
                    var address = buckets.Append(orientation, direction, width, expected[i]);
                    Assert.Equal(i, address.Index);
                    Assert.Equal((long)i * width, PositionalBuckets.BitPosition(address));
                }
                for (var i = 0; i < expected.Length; i += 3)
                {
                    expected[i] = ~expected[i] & mask;
                    Assert.True(buckets.TryWrite(new(orientation, direction, width, i), expected[i]));
                }
                for (var i = 0; i < expected.Length; i++)
                {
                    Assert.True(buckets.TryRead(new(orientation, direction, width, i), out var actual));
                    Assert.Equal(expected[i], actual);
                }
                Assert.Equal((expected.Length * width + 7) / 8, buckets.PackedBytes(orientation, direction, width).Length);
                Assert.Equal(expected.Length, buckets.LiveCount(orientation, direction, width));
            }
        }
    }

    [Fact]
    public void RetiredPayloadStaysDirtyButCannotBeReadWrittenOrReused()
    {
        var buckets = new PositionalBuckets();
        var retired = buckets.Append(0, 1, 1, 1);
        var liveZero = buckets.Append(0, 1, 1, 0);
        Assert.True(buckets.Retire(retired));
        Assert.False(buckets.Retire(retired));
        Assert.False(buckets.TryRead(retired, out _));
        Assert.False(buckets.TryWrite(retired, 0));
        Assert.Equal(new byte[] { 1 }, buckets.PackedBytes(0, 1, 1));
        Assert.Equal((byte)2, Read(buckets, liveZero));
        var appended = buckets.Append(0, 1, 1, 1);
        Assert.Equal(2, appended.Index);
        Assert.Equal(3, buckets.Count(0, 1, 1));
        Assert.Equal(2, buckets.LiveCount(0, 1, 1));
        Assert.Equal(new byte[] { 5 }, buckets.PackedBytes(0, 1, 1));
        var diagnostic = buckets.PackedBytes(0, 1, 1);
        diagnostic[0] = 0;
        Assert.Equal(new byte[] { 5 }, buckets.PackedBytes(0, 1, 1));
    }

    [Fact]
    public void MissingSlotsAndBytePaddingDoNotProduceLiveZeroValues()
    {
        var buckets = new PositionalBuckets();
        Assert.False(buckets.TryRead(new(0, 0, 1, 0), out _));
        buckets.Append(0, 0, 1, 0);
        Assert.False(buckets.TryRead(new(0, 0, 1, 1), out _));
        Assert.False(buckets.TryRead(new(0, 0, 1, -1), out _));
        Assert.False(buckets.TryWrite(new(0, 0, 1, int.MaxValue), 0));
        Assert.False(buckets.Retire(new(0, 0, 1, 1)));
        Assert.Equal(1, buckets.LiveCount(0, 0, 1));
    }

    [Fact]
    public void EncodingCannotChangeConstantsEvenAtZeroAndInvalidWritesAreAtomic()
    {
        var buckets = new PositionalBuckets();
        var address = buckets.Append(0, 1, 1, 1);
        Assert.Throws<ArgumentException>(() => PositionalBuckets.Encode(0, 1, 1, true, true, 0));
        Assert.Throws<ArgumentException>(() => PositionalBuckets.Encode(0, 1, 1, false, false, 0));
        Assert.Throws<ArgumentException>(() => PositionalBuckets.Encode(1, 2, 2, false, false, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.Encode(0, 1, 1, false, true, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => buckets.TryWrite(address, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => buckets.Append(0, 1, 1, 2));
        Assert.Equal(1, buckets.Count(0, 1, 1));
        Assert.Equal((byte)3, Read(buckets, address));
        Assert.Equal(0UL, PositionalBuckets.Encode(0, 1, 1, false, true, 0));
    }

    [Fact]
    public void WiderStorageRetainsCoarseUncertaintyAndDoesNotCountSignBitsAsValue()
    {
        var signed = new SlotAddress(1, 2, 6, 0);
        Assert.Equal((byte)6, PositionalBuckets.CoarseRead(signed, 32));
        Assert.Equal((byte)7, PositionalBuckets.CoarseRead(signed, 33));
        Assert.Equal((byte)7, PositionalBuckets.CoarseRead(signed, 63));
        Assert.Equal((true, true, 31UL), PositionalBuckets.Decode(signed, 63));
        var full = new SlotAddress(2, 0, 64, 0);
        Assert.Equal(62, PositionalBuckets.ValueBits(2, 0, 64));
        Assert.Equal(0xC000000000000000UL, PositionalBuckets.Encode(2, 0, 64, true, true, 0));
        Assert.Equal((byte)6, PositionalBuckets.CoarseRead(full, 0xC000000000000000UL));
        Assert.Equal((byte)7, PositionalBuckets.CoarseRead(full, ulong.MaxValue));
        Assert.Equal((true, true, 0x3FFFFFFFFFFFFFFFUL), PositionalBuckets.Decode(full, ulong.MaxValue));
        Assert.Equal(ulong.MaxValue, PositionalBuckets.Encode(0, 0, 64, false, false, ulong.MaxValue));
        Assert.Null(ThreeBitMeasure.Compare(PositionalBuckets.CoarseRead(signed, 33),
            PositionalBuckets.CoarseRead(new(0, 1, 1, 0), 1)));
    }

    [Fact]
    public void UnsupportedPathsAreRejectedAndBitAddressesDoNotWrapAtIntRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(2, 1, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(3, 0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(0, 3, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(0, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(2, 0, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => PositionalBuckets.ValueBits(0, 0, 65));
        Assert.Equal(137438953408L, PositionalBuckets.BitPosition(new(0, 0, 64, int.MaxValue)));
    }

    [Fact]
    public void EqualLocalAddressesDoNotShareStorageAcrossStructures()
    {
        var first = new PositionalBuckets();
        var second = new PositionalBuckets();
        var address = first.Append(0, 0, 1, 1);
        Assert.Equal(address, second.Append(0, 0, 1, 0));
        Assert.Equal((byte)1, Read(first, address));
        Assert.Equal((byte)0, Read(second, address));
        Assert.True(first.Retire(address));
        Assert.Equal((byte)0, Read(second, address));
    }

    private static byte Read(PositionalBuckets buckets, SlotAddress address)
    {
        Assert.True(buckets.TryRead(address, out var raw));
        return PositionalBuckets.CoarseRead(address, raw);
    }

    private static IEnumerable<(int Orientation, int Direction)> Layouts()
    {
        for (var orientation = 0; orientation < 2; orientation++)
            for (var direction = 0; direction < 3; direction++) yield return (orientation, direction);
        yield return (2, 0);
    }
}
