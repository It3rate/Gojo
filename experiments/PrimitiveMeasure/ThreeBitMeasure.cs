namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One word is O D V: positive-left orientation, negative direction,
/// and nonzero magnitude. A clear V is zero; a set V is NOT the quantity one.
/// No identity, calibration or hidden magnitude is stored.</summary>
public static class ThreeBitMeasure
{
    public const byte Orientation = 0b100;
    public const byte Direction = 0b010;
    public const byte Value = 0b001;

    public static byte Encode(bool positiveLeft, bool negative, bool nonzero) =>
        (byte)((positiveLeft ? Orientation : 0) | (negative ? Direction : 0) | (nonzero ? Value : 0));

    /// <summary>Explicitly read in the opposite orientation, retaining the
    /// represented sense. Changing direction alone is a different operation.</summary>
    public static byte Reorient(byte word) { Check(word); return (byte)(word ^ (Orientation | Direction)); }
    public static byte ReverseDirection(byte word) { Check(word); return (byte)(word ^ Direction); }

    /// <summary>Comparison at this resolution: negative nonzero &lt; zero &lt;
    /// positive nonzero. Equal here does not assert equal underlying magnitudes.
    /// Different orientations have no selected comparison.</summary>
    public static int? Compare(byte a, byte b)
    {
        Check(a); Check(b);
        if (((a ^ b) & Orientation) != 0) return null;
        return CoarseOrder(a).CompareTo(CoarseOrder(b));
    }

    /// <summary>Possible answer words, not extra fields on either operand.
    /// No answer is selected across orientations. A zero result retains both
    /// direction possibilities when the operands do not settle that bit.</summary>
    public static byte[] Add(byte a, byte b)
    {
        Check(a); Check(b);
        if (((a ^ b) & Orientation) != 0) return [];
        var orientation = (byte)(a & Orientation);
        if (((a | b) & Value) == 0)
            return a == b ? [a] : [orientation, (byte)(orientation | Direction)];
        if ((a & Value) == 0) return [b];
        if ((b & Value) == 0 || ((a ^ b) & Direction) == 0) return [a];
        return [orientation, (byte)(orientation | Value),
            (byte)(orientation | Direction), (byte)(orientation | Direction | Value)];
    }

    /// <summary>Equal words are not evidence for exact cancellation.</summary>
    public static byte[] Subtract(byte a, byte b) => Add(a, ReverseDirection(b));

    /// <summary>Three bits per word, in supplied order, low bit first. No header
    /// or per-word metadata. The caller knows the word count; final padding is zero.</summary>
    public static byte[] Pack(ReadOnlySpan<byte> words)
    {
        foreach (var word in words) Check(word);
        var packed = new byte[checked((int)((words.Length * 3L + 7) / 8))];
        for (var index = 0; index < words.Length; index++)
        {
            var bit = index * 3L; var at = (int)(bit / 8); var shift = (int)(bit % 8);
            packed[at] |= (byte)(words[index] << shift);
            if (shift > 5) packed[at + 1] |= (byte)(words[index] >> (8 - shift));
        }
        return packed;
    }

    /// <summary>Reads a word by stored position. The caller must restrict index
    /// to the original word count: trailing padding is not another zero measure.</summary>
    public static byte At(ReadOnlySpan<byte> packed, int index)
    {
        var bit = index * 3L;
        if (index < 0 || bit + 3 > packed.Length * 8L) throw new ArgumentOutOfRangeException(nameof(index));
        var at = (int)(bit / 8); var shift = (int)(bit % 8);
        var bits = (int)packed[at];
        if (shift > 5) bits |= packed[at + 1] << 8;
        return (byte)((bits >> shift) & 7);
    }

    private static int CoarseOrder(byte word) => (word & Value) == 0 ? 0 : (word & Direction) == 0 ? 1 : -1;
    private static void Check(byte word)
    {
        if (word > 7) throw new ArgumentOutOfRangeException(nameof(word), "Expected only O D V, three bits.");
    }
}
