namespace Gojo.EncodingC;

/// <summary>A law is a table of bits: one bit for every combination of role
/// values (a relation) or every before-and-after pair (a transition). Roles are
/// numbered and have a shape; they have no names. The table is shared by every
/// row that uses it, so topology (which positions) stays separate from rule
/// (which combinations). Tuple index is mixed-radix with role 0 lowest, so
/// three one-bit roles give <c>r0 + 2·r1 + 4·r2</c>.</summary>
public abstract class Law
{
    /// <summary>Tables are checked exhaustively when they are made; this bound
    /// keeps that cheap. It limits this adapter, not what a law can mean.</summary>
    public const int MaxBits = 1 << 22;

    private readonly ulong[] table;
    private readonly Shape[] roles;

    private protected Law(IReadOnlyList<Shape> roles, long bitCount, ReadOnlySpan<byte> bytes)
    {
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Count == 0) throw new ArgumentException("A law has at least one role.", nameof(roles));
        if (bitCount is < 1 or > MaxBits) throw new ArgumentOutOfRangeException(nameof(bitCount), "Table too large to check exhaustively.");
        var byteCount = (int)((bitCount + 7) / 8);
        if (bytes.Length != byteCount) throw new ArgumentException($"Expected {byteCount} bytes: one bit per tuple.", nameof(bytes));
        if (bitCount % 8 != 0 && (bytes[^1] >> (int)(bitCount % 8)) != 0)
            throw new ArgumentException("Padding above the last tuple must be zero.", nameof(bytes));
        this.roles = roles.ToArray();
        Roles = Array.AsReadOnly(this.roles);
        BitCount = (int)bitCount;
        table = new ulong[(BitCount + 63) / 64];
        for (var i = 0; i < bytes.Length; i++) table[i / 8] |= (ulong)bytes[i] << (8 * (i % 8));
    }

    /// <summary>The role shapes. Read-only: the shapes are part of the law.</summary>
    public IReadOnlyList<Shape> Roles { get; }

    /// <summary>Number of role tuples: the product of the role sizes.</summary>
    public int TupleCount => Product(roles);

    public int BitCount { get; }

    public int AdmittedCount => table.Sum(System.Numerics.BitOperations.PopCount);

    public bool BitAt(int bit)
    {
        if ((uint)bit >= (uint)BitCount) throw new ArgumentOutOfRangeException(nameof(bit));
        return ((table[bit >> 6] >> (bit & 63)) & 1) != 0;
    }

    public int TupleIndex(ReadOnlySpan<int> values)
    {
        if (values.Length != roles.Length) throw new ArgumentException($"The law has {roles.Length} roles.", nameof(values));
        var index = 0;
        var radix = 1;
        for (var r = 0; r < roles.Length; r++)
        {
            if ((uint)values[r] >= (uint)roles[r].Size) throw new ArgumentOutOfRangeException(nameof(values));
            index += values[r] * radix;
            radix *= roles[r].Size;
        }
        return index;
    }

    public int[] Tuple(int index)
    {
        if ((uint)index >= (uint)TupleCount) throw new ArgumentOutOfRangeException(nameof(index));
        var values = new int[roles.Length];
        for (var r = 0; r < roles.Length; r++)
        {
            values[r] = index % roles[r].Size;
            index /= roles[r].Size;
        }
        return values;
    }

    /// <summary>The table in storage order, bit 0 of byte 0 first.</summary>
    public byte[] Bytes()
    {
        var bytes = new byte[(BitCount + 7) / 8];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(table[i / 8] >> (8 * (i % 8)));
        return bytes;
    }

    /// <summary>The whole table as one integer when it fits. A six-input law is
    /// then an FPGA LUT6 initialization value.</summary>
    public ulong? AsUInt64() => BitCount <= 64 ? table[0] : null;

    /// <summary>The same kind of law, the same role shapes and the same table bits.</summary>
    public bool SameTable(Law other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.GetType() == GetType() && other.roles.SequenceEqual(roles) && other.table.SequenceEqual(table);
    }

    private protected static byte[] PackBits(long bitCount, IEnumerable<long> set)
    {
        var bytes = new byte[(bitCount + 7) / 8];
        foreach (var bit in set)
        {
            if (bit < 0 || bit >= bitCount) throw new ArgumentOutOfRangeException(nameof(set));
            bytes[bit / 8] |= (byte)(1 << (int)(bit % 8));
        }
        return bytes;
    }

    private protected static int Product(IReadOnlyList<Shape> roles) => roles.Aggregate(1, (p, r) => checked(p * r.Size));
}
