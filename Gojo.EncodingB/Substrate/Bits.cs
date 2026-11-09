using System.Numerics;
using System.Text;

namespace Gojo.EncodingB;

/// <summary>Small helpers over 64-bit planes. Bit i of a plane belongs to
/// site (or lane, or row) i. These helpers carry no meaning of their own; the
/// address of a bit, read through a site grammar or a law, does.</summary>
public static class Bits
{
    /// <summary>Sites per plane word.</summary>
    public const int Width = 64;

    /// <summary>The low <paramref name="count"/> bits set.</summary>
    public static ulong Low(int count)
    {
        if (count is < 0 or > Width) throw new ArgumentOutOfRangeException(nameof(count));
        return count == Width ? ulong.MaxValue : (1UL << count) - 1;
    }

    public static ulong Of(int index)
    {
        CheckIndex(index);
        return 1UL << index;
    }

    public static ulong Of(IEnumerable<int> indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);
        ulong mask = 0;
        foreach (var index in indexes) mask |= Of(index);
        return mask;
    }

    public static bool Get(ulong word, int index)
    {
        CheckIndex(index);
        return ((word >> index) & 1) != 0;
    }

    public static ulong With(ulong word, int index, bool value) =>
        value ? word | Of(index) : word & ~Of(index);

    public static int Count(ulong word) => BitOperations.PopCount(word);

    /// <summary>Set bit indexes in ascending order.</summary>
    public static IEnumerable<int> Indexes(ulong word)
    {
        while (word != 0)
        {
            yield return BitOperations.TrailingZeroCount(word);
            word &= word - 1;
        }
    }

    /// <summary>Site order, left to right: character i is bit i. A selection
    /// over six markers prints as "101101", the way it is read along a contour.</summary>
    public static string Format(ulong word, int width)
    {
        if (width is < 0 or > Width) throw new ArgumentOutOfRangeException(nameof(width));
        var text = new StringBuilder(width);
        for (var i = 0; i < width; i++) text.Append(Get(word, i) ? '1' : '0');
        return text.ToString();
    }

    /// <summary>Parses the <see cref="Format"/> notation: character i is bit i.</summary>
    public static ulong Parse(string sites)
    {
        ArgumentNullException.ThrowIfNull(sites);
        var compact = sites.Replace(" ", string.Empty);
        if (compact.Length > Width) throw new ArgumentException("At most 64 sites.", nameof(sites));
        ulong word = 0;
        for (var i = 0; i < compact.Length; i++)
            word = compact[i] switch
            {
                '1' => word | (1UL << i),
                '0' => word,
                _ => throw new ArgumentException("Use only 0 and 1.", nameof(sites)),
            };
        return word;
    }

    private static void CheckIndex(int index)
    {
        if (index is < 0 or >= Width) throw new ArgumentOutOfRangeException(nameof(index));
    }
}
