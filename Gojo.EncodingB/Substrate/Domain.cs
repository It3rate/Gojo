namespace Gojo.EncodingB;

/// <summary>A finite set of named values that a role or field can take. Code
/// <c>k</c> is the k-th value. A support over a domain is a mask with bit k
/// meaning "value k is still possible"; there is no extra "unknown" value.</summary>
public sealed class Domain
{
    /// <summary>Supports are single 64-bit masks, so a domain holds at most 64 values.</summary>
    public const int MaxSize = 64;

    private readonly string[] values;

    public Domain(string name, params string[] values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length is < 1 or > MaxSize)
            throw new ArgumentException("A domain has 1 to 64 values.", nameof(values));
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length)
            throw new ArgumentException("Domain values must be distinct.", nameof(values));
        Name = name;
        this.values = values.ToArray();
        var bits = 1;
        while ((1 << bits) < values.Length) bits++;
        CodeBits = bits;
    }

    public string Name { get; }
    public IReadOnlyList<string> Values => values;
    public int Size => values.Length;

    /// <summary>Planes needed to store one value as a binary code, low bit first.</summary>
    public int CodeBits { get; }

    /// <summary>Every value possible: the open support.</summary>
    public ulong Full => Bits.Low(Size);

    public static Domain Binary(string name, string whenClear, string whenSet) => new(name, whenClear, whenSet);

    public string NameOf(int code)
    {
        if (code < 0 || code >= Size) throw new ArgumentOutOfRangeException(nameof(code));
        return values[code];
    }

    public int CodeOf(string value)
    {
        var code = Array.IndexOf(values, value);
        if (code < 0) throw new ArgumentException($"'{value}' is not a value of domain {Name}.", nameof(value));
        return code;
    }

    /// <summary>Names of the values a support admits, in code order.</summary>
    public string Describe(ulong support)
    {
        if ((support & ~Full) != 0) throw new ArgumentOutOfRangeException(nameof(support));
        return support == 0 ? "{} (no admitted value)" : "{" + string.Join(", ", Bits.Indexes(support).Select(NameOf)) + "}";
    }

    public override string ToString() => Name;
}
