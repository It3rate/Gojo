namespace Gojo.EncodingC;

/// <summary>Bits written one after another, low bit of each byte first. Whole
/// numbers are self-delimiting Elias gamma codes of n + 1: small numbers take
/// few bits (0 takes one bit, 1 and 2 take three), and nothing needs a fixed
/// width or a header.</summary>
internal sealed class BitWriter
{
    private readonly List<byte> bytes = [];
    private long length;

    public void Bit(bool bit)
    {
        if (length % 8 == 0) bytes.Add(0);
        if (bit) bytes[^1] |= (byte)(1 << (int)(length % 8));
        length++;
    }

    /// <summary>A whole number 0, 1, 2 …</summary>
    public void Number(long value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        var n = (ulong)value + 1;
        var top = 63 - System.Numerics.BitOperations.LeadingZeroCount(n);
        for (var k = 0; k < top; k++) Bit(false);
        for (var k = top; k >= 0; k--) Bit(((n >> k) & 1) != 0);
    }

    /// <summary>A signed number, folded so that small magnitudes stay short.</summary>
    public void Signed(long value) => Number(value >= 0 ? 2 * value : -2 * value - 1);

    public void Bits(Plane plane, int length)
    {
        for (var i = 0; i < length; i++) Bit(i < plane.Length && plane[i]);
    }

    /// <summary>Bits written so far.</summary>
    public long Length => length;

    public byte[] ToArray() => bytes.ToArray();
}

/// <summary>Reads bits back. Where they stop describing a machine it records
/// the first refusal, and every read after it returns zeros, so a parse runs
/// out without inventing structure and without throwing.</summary>
internal sealed class BitReader(byte[] bytes)
{
    private long position;

    /// <summary>The first place the bits stopped describing a machine, or null.</summary>
    public Refusal? Refused { get; private set; }

    /// <summary>Records a refusal here, unless one is already recorded.</summary>
    public void Refuse(long found, long allowed) => Refused ??= new Refusal(position, found, allowed);

    public bool Bit()
    {
        if (Refused is not null) return false;
        if (position >= bytes.LongLength * 8)
        {
            Refuse(position, bytes.LongLength * 8 - 1); // the bits end early
            return false;
        }
        var bit = ((bytes[position / 8] >> (int)(position % 8)) & 1) != 0;
        position++;
        return bit;
    }

    public long Number()
    {
        var top = 0;
        while (!Bit())
        {
            if (Refused is not null) return 0;
            if (++top > 63) { Refuse(top, 63); return 0; } // a number longer than 64 bits
        }
        ulong n = 1;
        for (var k = 0; k < top; k++) n = (n << 1) | (Bit() ? 1UL : 0UL);
        return (long)(n - 1);
    }

    public int Count(int limit = int.MaxValue)
    {
        var value = Number();
        if (value < 0 || value > limit) { Refuse(value < 0 ? long.MaxValue : value, limit); return 0; } // a count out of range; a code past long reads as the largest
        return (int)value;
    }

    public long Signed()
    {
        var folded = Number();
        return (folded & 1) == 0 ? folded / 2 : -(folded + 1) / 2;
    }

    public Plane Bits(int length)
    {
        var plane = new Plane(length);
        for (var i = 0; i < length; i++) plane[i] = Bit();
        return plane;
    }

    /// <summary>Only zero padding may follow the last field.</summary>
    public void End()
    {
        while (Refused is null && position < bytes.LongLength * 8)
            if (Bit()) Refuse(1, 0); // a bit set after the end of the machine
    }
}
