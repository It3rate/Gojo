using System.Numerics;

namespace Gojo.EncodingC;

/// <summary>Non-negative integers stored back to back at one width: the width
/// the largest stored value needs. There is no header and no padding between
/// values, so a reference to one of n positions costs ⌈log₂ n⌉ bits rather
/// than a machine word. When a larger value arrives the run is repacked wider;
/// nothing is reserved in advance.</summary>
public sealed class PackedInts
{
    private ulong[] words = [];

    public int Count { get; private set; }

    /// <summary>Bits per stored value.</summary>
    public int Width { get; private set; } = 1;

    /// <summary>Payload bits: Count × Width.</summary>
    public long BitLength => (long)Count * Width;

    public int this[int index]
    {
        get
        {
            Check(index);
            return (int)Get((long)index * Width);
        }
        set
        {
            Check(index);
            Ensure(value);
            Put((long)index * Width, (ulong)value);
        }
    }

    public void Add(int value)
    {
        Ensure(value);
        Reserve((long)(Count + 1) * Width);
        Count++;
        Put((long)(Count - 1) * Width, (ulong)value);
    }

    public int[] ToArray()
    {
        var values = new int[Count];
        for (var i = 0; i < Count; i++) values[i] = (int)Get((long)i * Width);
        return values;
    }

    /// <summary>Bits needed to store every value from 0 to <paramref name="value"/>.</summary>
    public static int WidthFor(int value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        return value <= 1 ? 1 : 32 - BitOperations.LeadingZeroCount((uint)value);
    }

    private void Ensure(int value)
    {
        var width = WidthFor(value);
        if (width <= Width) return;
        var values = ToArray();
        Width = width;
        words = new ulong[(int)((BitLength + 63) >> 6)];
        for (var i = 0; i < values.Length; i++) Put((long)i * Width, (ulong)values[i]);
    }

    private void Reserve(long bits)
    {
        var needed = (int)((bits + 63) >> 6);
        if (needed > words.Length) Array.Resize(ref words, Math.Max(needed, words.Length * 2));
    }

    private ulong Get(long bit)
    {
        ulong value = 0;
        for (var k = 0; k < Width; k++)
        {
            var at = bit + k;
            if (((words[at >> 6] >> (int)(at & 63)) & 1) != 0) value |= 1UL << k;
        }
        return value;
    }

    private void Put(long bit, ulong value)
    {
        for (var k = 0; k < Width; k++)
        {
            var at = bit + k;
            var mask = 1UL << (int)(at & 63);
            if (((value >> k) & 1) != 0) words[at >> 6] |= mask;
            else words[at >> 6] &= ~mask;
        }
    }

    private void Check(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
    }
}
