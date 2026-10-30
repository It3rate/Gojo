namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>ODV entries whose containing table supplies their fixed high bits.
/// Width and entry count are supplied by the enclosing layout, not repeated in
/// each entry. Low-to-high storage is [O,D,V0,V1,...] for width 1,
/// [O,V0,D0,V1,D1,...] for width 2, and [V0,D0,O0,...] for width 3.
/// Instance updates cannot alter shared bits. This encodes constraints and
/// observations; it does not itself settle or transfer material.</summary>
public static class ContextTable
{
    public static int BitLength(int bitsPerEntry, int count)
    {
        CheckWidth(bitsPerEntry);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return checked(3 - bitsPerEntry + count * bitsPerEntry);
    }

    /// <summary>Physical bit position of V (0), D (1), or O (2) for an entry.
    /// Several entries may resolve to exactly the same shared header bit.
    /// Equal bit values at different positions do not imply shared storage.</summary>
    public static int BitPosition(int bitsPerEntry, int count, int index, int wordBit)
    {
        _ = BitLength(bitsPerEntry, count);
        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        if (wordBit < 0 || wordBit > 2) throw new ArgumentOutOfRangeException(nameof(wordBit));
        return wordBit < bitsPerEntry
            ? 3 - bitsPerEntry + index * bitsPerEntry + wordBit
            : 2 - wordBit;
    }

    /// <summary>fixedContext is an ODV word with all instance-owned bits clear.
    /// Every supplied word must agree with the fixed bits, including at zero.
    /// The final byte is padded with zero bits.</summary>
    public static byte[] Pack(int bitsPerEntry, byte fixedContext, ReadOnlySpan<byte> words)
    {
        var bits = BitLength(bitsPerEntry, words.Length);
        CheckWord(fixedContext);
        var mutableMask = (1 << bitsPerEntry) - 1;
        if ((fixedContext & mutableMask) != 0)
            throw new ArgumentException("Context may supply only the fixed bits.", nameof(fixedContext));
        foreach (var word in words)
        {
            CheckWord(word);
            if ((word & ~mutableMask) != fixedContext)
                throw new ArgumentException("An entry contradicts the table's fixed bits.", nameof(words));
        }

        var packed = new byte[(int)((bits + 7L) / 8)];
        for (var bit = bitsPerEntry; bit < 3; bit++)
            WriteBit(packed, 2 - bit, (fixedContext >> bit) & 1);
        for (var index = 0; index < words.Length; index++)
            for (var bit = 0; bit < bitsPerEntry; bit++)
                WriteBit(packed, 3 - bitsPerEntry + index * bitsPerEntry + bit, (words[index] >> bit) & 1);
        return packed;
    }

    public static byte Read(ReadOnlySpan<byte> packed, int bitsPerEntry, int count, int index)
    {
        CheckBuffer(packed.Length, bitsPerEntry, count);
        byte word = 0;
        for (var bit = 0; bit < 3; bit++)
            word |= (byte)(ReadBit(packed, BitPosition(bitsPerEntry, count, index, bit)) << bit);
        return word;
    }

    /// <summary>Update an encoded observation without changing its context.
    /// A conflicting O or D returns false and leaves every byte untouched.</summary>
    public static bool TryWrite(Span<byte> packed, int bitsPerEntry, int count, int index, byte word)
    {
        CheckWord(word);
        var before = Read(packed, bitsPerEntry, count, index);
        var mutableMask = (1 << bitsPerEntry) - 1;
        if (((before ^ word) & ~mutableMask) != 0) return false;
        for (var bit = 0; bit < bitsPerEntry; bit++)
            WriteBit(packed, BitPosition(bitsPerEntry, count, index, bit), (word >> bit) & 1);
        return true;
    }

    /// <summary>Explicitly release fixed distinctions into instance storage.
    /// Return a replacement representation preserving every effective word;
    /// never infer reorientation or narrow a signed entry from its current sign.
    /// This does not consume the old buffer or retarget external attachments.
    /// A caller replacing the table must deliberately rebind old bit positions.</summary>
    public static byte[] Release(ReadOnlySpan<byte> packed, int bitsPerEntry, int count, int newBitsPerEntry)
    {
        CheckBuffer(packed.Length, bitsPerEntry, count);
        CheckWidth(newBitsPerEntry);
        if (newBitsPerEntry < bitsPerEntry)
            throw new ArgumentException("Release cannot add a new fixed constraint.", nameof(newBitsPerEntry));
        byte context = 0;
        for (var bit = newBitsPerEntry; bit < 3; bit++)
            context |= (byte)(ReadBit(packed, 2 - bit) << bit);
        var words = new byte[count];
        for (var index = 0; index < count; index++) words[index] = Read(packed, bitsPerEntry, count, index);
        return Pack(newBitsPerEntry, context, words);
    }

    private static int ReadBit(ReadOnlySpan<byte> packed, int position) =>
        (packed[position / 8] >> (position % 8)) & 1;

    private static void WriteBit(Span<byte> packed, int position, int value)
    {
        var mask = 1 << (position % 8);
        packed[position / 8] = (byte)((packed[position / 8] & ~mask) | (value << (position % 8)));
    }

    private static void CheckBuffer(int byteCount, int bitsPerEntry, int count)
    {
        var bits = BitLength(bitsPerEntry, count);
        if (byteCount != (bits + 7L) / 8)
            throw new ArgumentException("The byte count does not match the supplied layout and entry count.");
    }

    private static void CheckWidth(int bitsPerEntry)
    {
        if (bitsPerEntry < 1 || bitsPerEntry > 3) throw new ArgumentOutOfRangeException(nameof(bitsPerEntry));
    }

    private static void CheckWord(byte word)
    {
        if (word > 7) throw new ArgumentOutOfRangeException(nameof(word), "Expected only O D V, three bits.");
    }
}
