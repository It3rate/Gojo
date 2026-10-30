namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A position within one owner's arrays, not a global identity.</summary>
public readonly record struct SlotAddress(int OrientationGroup, int DirectionGroup, int Width, int Index);

/// <summary>Root positions 0/1 fix O; their child positions 0/1 fix D and 2 owns D.
/// Root position 2 owns O and D and uses child position 0. Width buckets contain
/// only packed entries: low value bits, then owned D, then owned O. No headers.
/// Additional value bits are storage capacity, not a calibration or finer read.
/// This storage experiment is single-threaded; a containing structure owns access.</summary>
public sealed class PositionalBuckets
{
    private readonly Bucket?[][][] groups =
    [
        [new Bucket?[64], new Bucket?[64], new Bucket?[64]],
        [new Bucket?[64], new Bucket?[64], new Bucket?[64]],
        [new Bucket?[64]]
    ];

    private sealed class Bucket
    {
        public byte[] Packed = [];
        public byte[] Live = [];
        public int Count;
        public int LiveCount;
    }

    public static int ValueBits(int orientationGroup, int directionGroup, int width)
    {
        if (orientationGroup is < 0 or > 2)
            throw new ArgumentOutOfRangeException(nameof(orientationGroup));
        if (directionGroup is < 0 or > 2 || (orientationGroup == 2 && directionGroup != 0))
            throw new ArgumentOutOfRangeException(nameof(directionGroup));
        var owned = orientationGroup == 2 ? 2 : directionGroup == 2 ? 1 : 0;
        if (width <= owned || width > 64) throw new ArgumentOutOfRangeException(nameof(width));
        return width - owned;
    }

    public static long BitPosition(SlotAddress address)
    {
        ValueBits(address.OrientationGroup, address.DirectionGroup, address.Width);
        if (address.Index < 0) throw new ArgumentOutOfRangeException(nameof(address));
        return checked((long)address.Index * address.Width);
    }

    public static (bool PositiveLeft, bool Negative, ulong Value) Decode(SlotAddress address, ulong raw)
    {
        var valueBits = ValueBits(address.OrientationGroup, address.DirectionGroup, address.Width);
        CheckRaw(raw, address.Width);
        var positiveLeft = address.OrientationGroup == 2
            ? ((raw >> (address.Width - 1)) & 1) != 0 : address.OrientationGroup == 1;
        var negative = address.OrientationGroup == 2 || address.DirectionGroup == 2
            ? ((raw >> valueBits) & 1) != 0 : address.DirectionGroup == 1;
        return (positiveLeft, negative, raw & Mask(valueBits));
    }

    public static ulong Encode(int orientationGroup, int directionGroup, int width,
        bool positiveLeft, bool negative, ulong value)
    {
        var valueBits = ValueBits(orientationGroup, directionGroup, width);
        if (orientationGroup != 2 && positiveLeft != (orientationGroup == 1))
            throw new ArgumentException("Orientation is fixed by the array position.", nameof(positiveLeft));
        if (orientationGroup != 2 && directionGroup != 2 && negative != (directionGroup == 1))
            throw new ArgumentException("Direction is fixed by the array position.", nameof(negative));
        CheckRaw(value, valueBits);
        var raw = value;
        if ((orientationGroup == 2 || directionGroup == 2) && negative) raw |= 1UL << valueBits;
        if (orientationGroup == 2 && positiveLeft) raw |= 1UL << (width - 1);
        return raw;
    }

    public static byte CoarseRead(SlotAddress address, ulong raw)
    {
        var (positiveLeft, negative, value) = Decode(address, raw);
        return ThreeBitMeasure.Encode(positiveLeft, negative, value != 0);
    }

    public SlotAddress Append(int orientationGroup, int directionGroup, int width, ulong raw)
    {
        ValueBits(orientationGroup, directionGroup, width);
        CheckRaw(raw, width);
        var bucket = groups[orientationGroup][directionGroup][width - 1] ??= new Bucket();
        var nextCount = checked(bucket.Count + 1);
        var packedSize = checked((int)((nextCount * (long)width + 7) / 8));
        var liveSize = checked((int)((nextCount + 7L) / 8));
        if (packedSize != bucket.Packed.Length) Array.Resize(ref bucket.Packed, packedSize);
        if (liveSize != bucket.Live.Length) Array.Resize(ref bucket.Live, liveSize);
        var address = new SlotAddress(orientationGroup, directionGroup, width, bucket.Count);
        Write(bucket.Packed, address, raw);
        bucket.Live[address.Index / 8] |= (byte)(1 << (address.Index % 8));
        bucket.Count = nextCount;
        bucket.LiveCount++;
        return address;
    }

    public bool TryRead(SlotAddress address, out ulong raw)
    {
        raw = 0;
        if (!TryLive(address, out var bucket)) return false;
        var start = BitPosition(address);
        for (var bit = 0; bit < address.Width; bit++)
        {
            var position = start + bit;
            if ((bucket!.Packed[checked((int)(position / 8))] & (1 << (int)(position % 8))) != 0)
                raw |= 1UL << bit;
        }
        return true;
    }

    /// <summary>Writes only owned bits. A different inherited O/D requires moving
    /// to another array; this method cannot edit the containing array position.</summary>
    public bool TryWrite(SlotAddress address, ulong raw)
    {
        if (!TryLive(address, out var bucket)) return false;
        CheckRaw(raw, address.Width);
        Write(bucket!.Packed, address, raw);
        return true;
    }

    /// <summary>Retirement clears allocation liveness only. Payload bits remain
    /// dirty and the slot is never reused or compacted by this experiment.</summary>
    public bool Retire(SlotAddress address)
    {
        if (!TryLive(address, out var bucket)) return false;
        bucket!.Live[address.Index / 8] &= (byte)~(1 << (address.Index % 8));
        bucket.LiveCount--;
        return true;
    }

    public int Count(int orientationGroup, int directionGroup, int width) =>
        Find(orientationGroup, directionGroup, width)?.Count ?? 0;

    public int LiveCount(int orientationGroup, int directionGroup, int width) =>
        Find(orientationGroup, directionGroup, width)?.LiveCount ?? 0;

    /// <summary>Diagnostic copy, including dirty retired bits and final byte
    /// padding. Neither retired slots nor padding are live value reads.</summary>
    public byte[] PackedBytes(int orientationGroup, int directionGroup, int width) =>
        Find(orientationGroup, directionGroup, width)?.Packed.ToArray() ?? [];

    private Bucket? Find(int orientationGroup, int directionGroup, int width)
    {
        ValueBits(orientationGroup, directionGroup, width);
        return groups[orientationGroup][directionGroup][width - 1];
    }

    private bool TryLive(SlotAddress address, out Bucket? bucket)
    {
        bucket = Find(address.OrientationGroup, address.DirectionGroup, address.Width);
        return bucket is not null && address.Index >= 0 && address.Index < bucket.Count
            && (bucket.Live[address.Index / 8] & (1 << (address.Index % 8))) != 0;
    }

    private static void Write(byte[] packed, SlotAddress address, ulong raw)
    {
        var start = BitPosition(address);
        for (var bit = 0; bit < address.Width; bit++)
        {
            var position = start + bit;
            var index = checked((int)(position / 8));
            var mask = (byte)(1 << (int)(position % 8));
            packed[index] = (byte)((packed[index] & ~mask) | (((raw >> bit) & 1) != 0 ? mask : 0));
        }
    }

    private static ulong Mask(int bits) => bits == 64 ? ulong.MaxValue : (1UL << bits) - 1;

    private static void CheckRaw(ulong raw, int width)
    {
        if ((raw & ~Mask(width)) != 0) throw new ArgumentOutOfRangeException(nameof(raw));
    }
}
