namespace Gojo.EncodingB;

/// <summary>Rung R1: a thermometer (unary) code. Bit k is set when the magnitude
/// is greater than k, so 3 is 0b0111. Adding one shifts a bit in, removing one
/// shifts a bit out, and comparing is a mask test. More participation sets more
/// bits. Only prefixes are valid codes.</summary>
public static class Thermometer
{
    public static ulong Encode(int magnitude, int capacity)
    {
        if (capacity is < 1 or > Bits.Width) throw new ArgumentOutOfRangeException(nameof(capacity));
        if (magnitude < 0 || magnitude > capacity) throw new ArgumentOutOfRangeException(nameof(magnitude));
        return Bits.Low(magnitude);
    }

    /// <summary>The magnitude, or null when the bits are not a prefix.</summary>
    public static int? Decode(ulong code)
    {
        var magnitude = Bits.Count(code);
        return code == Bits.Low(magnitude) ? magnitude : null;
    }

    public static bool GreaterThan(ulong code, int k) => k < Bits.Width && Bits.Get(code, k);

    /// <summary>Shift one unit in.</summary>
    public static ulong AddOne(ulong code) => (code << 1) | 1;

    /// <summary>Shift one unit out.</summary>
    public static ulong RemoveOne(ulong code) => code >> 1;

    /// <summary><c>a ≥ b + 2</c> as two bit tests: a's bit 1 is set (a ≥ 2), and
    /// every bit of b fits inside a shifted right by two (b ≤ a − 2).</summary>
    public static bool ExceedsByTwo(ulong a, ulong b) => (a & 0b10) != 0 && (b & ~(a >> 2)) == 0;

    /// <summary>Adds one unit to every site in <paramref name="sites"/> at once, on
    /// bit-sliced thermometer planes (plane k = "greater than k"). Sites already
    /// full are returned as overflow and left unchanged.</summary>
    public static ulong AddOneAt(ulong[] planes, ulong sites)
    {
        ArgumentNullException.ThrowIfNull(planes);
        var overflow = sites & planes[^1];
        var take = sites & ~overflow;
        for (var k = planes.Length - 1; k >= 1; k--) planes[k] |= planes[k - 1] & take;
        planes[0] |= take;
        return overflow;
    }

    /// <summary>Removes one unit from every site in <paramref name="sites"/> at once.
    /// Sites already empty are returned as underflow and left unchanged.</summary>
    public static ulong RemoveOneAt(ulong[] planes, ulong sites)
    {
        ArgumentNullException.ThrowIfNull(planes);
        var underflow = sites & ~planes[0];
        var take = sites & ~underflow;
        for (var k = 0; k < planes.Length - 1; k++) planes[k] &= planes[k + 1] | ~take;
        planes[^1] &= ~take;
        return underflow;
    }

    /// <summary>Sites whose magnitude is above zero.</summary>
    public static ulong NonZero(ulong[] planes) => planes[0];

    /// <summary>Sites at capacity.</summary>
    public static ulong Full(ulong[] planes) => planes[^1];
}
