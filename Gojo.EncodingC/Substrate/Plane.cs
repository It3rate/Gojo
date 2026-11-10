using System.Numerics;
using System.Text;

namespace Gojo.EncodingC;

/// <summary>A sequence of bits, one per position. This is the only storage
/// shape in the project: a state plane over a structure's positions, a
/// selection, a set of lanes and a support are all planes. A plane is exactly
/// as long as it needs to be. Holding it in 64-bit words is layout for a
/// digital machine; nothing outside this class sees the words, and no length
/// is a quota.</summary>
public sealed class Plane : IEquatable<Plane>
{
    private ulong[] words;

    public Plane(int length)
    {
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        Length = length;
        words = new ulong[WordsFor(length)];
    }

    private Plane(int length, ulong[] words)
    {
        Length = length;
        this.words = words;
        Trim();
    }

    public int Length { get; private set; }

    public static Plane Of(int length, params int[] set)
    {
        ArgumentNullException.ThrowIfNull(set);
        var plane = new Plane(length);
        foreach (var index in set) plane[index] = true;
        return plane;
    }

    public static Plane Of(int length, IEnumerable<int> set) => Of(length, set.ToArray());

    public static Plane Ones(int length)
    {
        var plane = new Plane(length);
        Array.Fill(plane.words, ulong.MaxValue);
        plane.Trim();
        return plane;
    }

    public bool this[int index]
    {
        get
        {
            Check(index);
            return ((words[index >> 6] >> (index & 63)) & 1) != 0;
        }
        set
        {
            Check(index);
            if (value) words[index >> 6] |= 1UL << (index & 63);
            else words[index >> 6] &= ~(1UL << (index & 63));
        }
    }

    public int Count => words.Sum(BitOperations.PopCount);

    public bool Any => words.Any(word => word != 0);

    public bool None => !Any;

    /// <summary>Set positions in ascending order.</summary>
    public IEnumerable<int> Indexes()
    {
        for (var w = 0; w < words.Length; w++)
        {
            var word = words[w];
            while (word != 0)
            {
                yield return (w << 6) + BitOperations.TrailingZeroCount(word);
                word &= word - 1;
            }
        }
    }

    public Plane Clone() => new(Length, (ulong[])words.Clone());

    /// <summary>Lengthens the plane. New positions read clear.</summary>
    public void Grow(int length)
    {
        if (length < Length) throw new ArgumentOutOfRangeException(nameof(length), "A plane only grows.");
        Array.Resize(ref words, WordsFor(length));
        Length = length;
    }

    public Plane And(Plane other) => Combine(other, static (a, b) => a & b);

    public Plane Or(Plane other) => Combine(other, static (a, b) => a | b);

    public Plane AndNot(Plane other) => Combine(other, static (a, b) => a & ~b);

    public Plane Xor(Plane other) => Combine(other, static (a, b) => a ^ b);

    public Plane Not()
    {
        var result = new ulong[words.Length];
        for (var w = 0; w < words.Length; w++) result[w] = ~words[w];
        return new Plane(Length, result);
    }

    public bool Overlaps(Plane other)
    {
        SameLength(other);
        for (var w = 0; w < words.Length; w++)
            if ((words[w] & other.words[w]) != 0) return true;
        return false;
    }

    /// <summary>Every set position of <paramref name="other"/> is set here.</summary>
    public bool Covers(Plane other)
    {
        SameLength(other);
        for (var w = 0; w < words.Length; w++)
            if ((other.words[w] & ~words[w]) != 0) return false;
        return true;
    }

    internal int WordCount => words.Length;

    internal ulong Word(int w) => words[w];

    internal void OrWord(int w, ulong bits)
    {
        words[w] |= bits;
        if (w == words.Length - 1) Trim();
    }

    public bool Equals(Plane? other) =>
        other is not null && other.Length == Length && words.AsSpan().SequenceEqual(other.words);

    public override bool Equals(object? obj) => obj is Plane other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Length);
        foreach (var word in words) hash.Add(word);
        return hash.ToHashCode();
    }

    /// <summary>Notation for people: character i is position i, so a selection
    /// of every second marker of six prints as "101010".</summary>
    public override string ToString()
    {
        var text = new StringBuilder(Length);
        for (var i = 0; i < Length; i++) text.Append(this[i] ? '1' : '0');
        return text.ToString();
    }

    /// <summary>Reads the <see cref="ToString"/> notation. Spaces are ignored.</summary>
    public static Plane Parse(string bits)
    {
        ArgumentNullException.ThrowIfNull(bits);
        var compact = bits.Replace(" ", string.Empty);
        var plane = new Plane(compact.Length);
        for (var i = 0; i < compact.Length; i++)
            plane[i] = compact[i] switch
            {
                '1' => true,
                '0' => false,
                _ => throw new ArgumentException("Use only 0 and 1.", nameof(bits)),
            };
        return plane;
    }

    private Plane Combine(Plane other, Func<ulong, ulong, ulong> op)
    {
        SameLength(other);
        var result = new ulong[words.Length];
        for (var w = 0; w < words.Length; w++) result[w] = op(words[w], other.words[w]);
        return new Plane(Length, result);
    }

    private void SameLength(Plane other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Length != Length) throw new ArgumentException($"Planes of {Length} and {other.Length} positions do not line up.");
    }

    private void Check(int index)
    {
        if ((uint)index >= (uint)Length) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private void Trim()
    {
        if (words.Length > 0 && (Length & 63) != 0) words[^1] &= (1UL << (Length & 63)) - 1;
    }

    private static int WordsFor(int length) => (length + 63) >> 6;
}
