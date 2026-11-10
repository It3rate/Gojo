namespace Gojo.Encoding.Core.Packed;

/// <summary>One word is O D V: positive-left orientation, negative direction,
/// and detectable value. V=0 is undetectable at the selected resolution, not
/// nonparticipation or exact zero. V=1 is not the quantity one. Selection,
/// identity, calibration and hidden magnitude are not stored in this word.</summary>
public static class ThreeBitMeasure
{
    public const byte Orientation = 0b100;
    public const byte Direction = 0b010;
    public const byte Value = 0b001;

    public static byte Encode(bool positiveLeft, bool negative, bool detectable) =>
        (byte)((positiveLeft ? Orientation : 0) | (negative ? Direction : 0) | (detectable ? Value : 0));

    /// <summary>Apply the explicitly selected symmetric-frame transform O^1,D^1.
    /// This preserves sense only where that frame mapping has been established;
    /// it does not infer calibration, a common origin, or a domain-wide dual law.</summary>
    public static byte Reorient(byte word) { Check(word); return (byte)(word ^ (Orientation | Direction)); }
    public static byte ReverseDirection(byte word) { Check(word); return (byte)(word ^ Direction); }

    /// <summary>One detection face of a shared signed word. At most one face
    /// reads detectable. Two clear faces do not assert absence or exact zero;
    /// neither face owns an independently transferable amount.</summary>
    public static byte ReadSide(byte shared, bool negative)
    {
        Check(shared);
        var detected = (shared & Value) != 0 && ((shared & Direction) != 0) == negative;
        return Encode((shared & Orientation) != 0, negative, detected);
    }

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

    private static void Check(byte word)
    {
        if (word > 7) throw new ArgumentOutOfRangeException(nameof(word), "Expected only O D V, three bits.");
    }
}
