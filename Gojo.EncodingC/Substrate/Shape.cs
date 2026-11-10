namespace Gojo.EncodingC;

/// <summary>How a value sits in bits. A shape says how many values there are
/// and how they are laid out. It does not say what they mean.
/// <list type="bullet">
/// <item><b>Binary</b>: the value is the code Σ bitₖ·2ᵏ over ⌈log₂ size⌉ bits.</item>
/// <item><b>Unary</b>: value v sets the first v of size − 1 bits. More of
/// something sets more bits, adding one is a shift and comparing is a mask
/// test, so a count needs no arithmetic unit. Only prefixes decode.</item>
/// </list></summary>
public readonly record struct Shape
{
    private Shape(int size, bool isUnary)
    {
        Size = size;
        IsUnary = isUnary;
    }

    /// <summary>Number of values: 0 … Size − 1.</summary>
    public int Size { get; }

    public bool IsUnary { get; }

    public int Bits => IsUnary ? Size - 1 : PackedInts.WidthFor(Size - 1);

    public static Shape Bit { get; } = Binary(2);

    public static Shape Binary(int size)
    {
        if (size is < 2 or > 1 << 16) throw new ArgumentOutOfRangeException(nameof(size));
        return new Shape(size, false);
    }

    /// <summary>A unary count from 0 to <paramref name="capacity"/>.</summary>
    public static Shape Unary(int capacity)
    {
        if (capacity is < 1 or > 63) throw new ArgumentOutOfRangeException(nameof(capacity));
        return new Shape(capacity + 1, true);
    }

    /// <summary>The bit pattern of a value, low bit first.</summary>
    public ulong Encode(int value)
    {
        if ((uint)value >= (uint)Size) throw new ArgumentOutOfRangeException(nameof(value));
        return IsUnary ? (1UL << value) - 1 : (ulong)value;
    }

    /// <summary>The value of a bit pattern, or null when the pattern is not a
    /// code of this shape (a binary code past the last value, or a unary
    /// pattern that is not a prefix).</summary>
    public int? Decode(ulong pattern)
    {
        if (!IsUnary) return pattern < (ulong)Size ? (int)pattern : null;
        var count = System.Numerics.BitOperations.PopCount(pattern);
        return pattern == (1UL << count) - 1 && count < Size ? count : null;
    }
}
