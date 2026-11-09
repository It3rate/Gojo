using System.Security.Cryptography;

namespace Gojo.EncodingB;

/// <summary>A role a law reads: a name and the domain of values it can take.</summary>
public sealed record LawRole(string Name, Domain Domain);

/// <summary>What a law means under parallel execution. Declared per law and
/// part of its identity as a runtime contract.</summary>
public enum ConfluenceClass
{
    /// <summary>C1: only removes possibilities; any fair order reaches the same fixed point.</summary>
    MonotoneNarrowing = 1,

    /// <summary>C2: a site acts only on its own state; order-independent when abelian.</summary>
    SelfLocalTransfer = 2,

    /// <summary>C3: terminates by descent, but order can matter, so the schedule is state.</summary>
    ScheduledDescent = 3,

    /// <summary>C4: driven or periodic; needs a controller and stop evidence.</summary>
    Driven = 4,
}

/// <summary>A law is data: a bitmap over tuples of role values, shared by every
/// row that uses it. Topology (which sites) is kept separate from rule (which
/// tuples). Tuple index is mixed-radix with role 0 lowest, so three binary roles
/// S, C, T give <c>S + 2C + 4T</c>.</summary>
public abstract class LawTable
{
    /// <summary>Tables are kept small enough to check exhaustively at load.</summary>
    public const int MaxBits = 1 << 20;

    private readonly ulong[] table;
    private readonly LawRole[] roles;

    protected LawTable(string name, IReadOnlyList<LawRole> roles, int bitCount, ReadOnlySpan<byte> bytes, string provenance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(roles);
        if (roles.Count == 0) throw new ArgumentException("A law has at least one role.", nameof(roles));
        if (roles.Select(r => r.Name).Distinct(StringComparer.Ordinal).Count() != roles.Count)
            throw new ArgumentException("Role names must be distinct.", nameof(roles));
        if (bitCount is < 1 or > MaxBits) throw new ArgumentOutOfRangeException(nameof(bitCount), "Law table too large to check exhaustively.");
        var byteCount = (bitCount + 7) / 8;
        if (bytes.Length != byteCount)
            throw new ArgumentException($"Expected {byteCount} bytes: one bit per tuple.", nameof(bytes));
        if (bitCount % 8 != 0 && (bytes[^1] >> (bitCount % 8)) != 0)
            throw new ArgumentException("Padding bits above the last tuple must be zero.", nameof(bytes));
        Name = name;
        this.roles = roles.ToArray();
        BitCount = bitCount;
        Provenance = provenance;
        table = new ulong[(bitCount + 63) / 64];
        for (var i = 0; i < bytes.Length; i++) table[i / 8] |= (ulong)bytes[i] << (8 * (i % 8));
        ContentHash = ComputeHash(bytes);
    }

    public string Name { get; }
    public IReadOnlyList<LawRole> Roles => roles;

    /// <summary>Authored, learned from witnesses, adopted, and so on.</summary>
    public string Provenance { get; }

    /// <summary>Number of distinct role tuples: the product of the domain sizes.</summary>
    public int TupleCount => roles.Aggregate(1, (product, role) => product * role.Domain.Size);

    public int BitCount { get; }

    /// <summary>Identifies the <em>table</em>, not a concept. The same table bound
    /// to different planes means different things; names are not hashed.</summary>
    public string ContentHash { get; }

    protected abstract string Kind { get; }

    public int RoleIndex(string role)
    {
        for (var i = 0; i < roles.Length; i++)
            if (roles[i].Name == role) return i;
        throw new ArgumentException($"Law {Name} has no role '{role}'.", nameof(role));
    }

    public int TupleIndex(ReadOnlySpan<int> values)
    {
        if (values.Length != roles.Length) throw new ArgumentException($"Law {Name} has {roles.Length} roles.", nameof(values));
        var index = 0;
        var radix = 1;
        for (var r = 0; r < roles.Length; r++)
        {
            if (values[r] < 0 || values[r] >= roles[r].Domain.Size)
                throw new ArgumentOutOfRangeException(nameof(values), $"Role {roles[r].Name} has {roles[r].Domain.Size} values.");
            index += values[r] * radix;
            radix *= roles[r].Domain.Size;
        }
        return index;
    }

    public int[] Tuple(int index)
    {
        if (index < 0 || index >= TupleCount) throw new ArgumentOutOfRangeException(nameof(index));
        var values = new int[roles.Length];
        for (var r = 0; r < roles.Length; r++)
        {
            values[r] = index % roles[r].Domain.Size;
            index /= roles[r].Domain.Size;
        }
        return values;
    }

    /// <summary>"[S=occupied, C=open, T=empty]".</summary>
    public string DescribeTuple(int index)
    {
        var values = Tuple(index);
        return "[" + string.Join(", ", roles.Select((role, r) => $"{role.Name}={role.Domain.NameOf(values[r])}")) + "]";
    }

    /// <summary>The table in storage order, little-endian, bit 0 of byte 0 first.</summary>
    public byte[] Bytes()
    {
        var bytes = new byte[(BitCount + 7) / 8];
        for (var i = 0; i < bytes.Length; i++) bytes[i] = (byte)(table[i / 8] >> (8 * (i % 8)));
        return bytes;
    }

    /// <summary>The whole table as one integer, when it fits in 64 bits. For a
    /// six-input law this is an FPGA LUT6 initialization value.</summary>
    public ulong? AsUInt64() => BitCount <= 64 ? table[0] : null;

    public bool BitAt(int bit)
    {
        if (bit < 0 || bit >= BitCount) throw new ArgumentOutOfRangeException(nameof(bit));
        return ((table[bit >> 6] >> (bit & 63)) & 1) != 0;
    }

    public int AdmittedCount => table.Sum(Bits.Count);

    private string ComputeHash(ReadOnlySpan<byte> bytes)
    {
        var header = $"{Kind}:{string.Join(",", roles.Select(r => r.Domain.Size))}:";
        var buffer = new byte[header.Length + bytes.Length];
        for (var i = 0; i < header.Length; i++) buffer[i] = (byte)header[i];
        bytes.CopyTo(buffer.AsSpan(header.Length));
        return Convert.ToHexString(SHA256.HashData(buffer))[..16].ToLowerInvariant();
    }

    protected static byte[] PackBits(int bitCount, IEnumerable<int> setBits)
    {
        var bytes = new byte[(bitCount + 7) / 8];
        foreach (var bit in setBits)
        {
            if (bit < 0 || bit >= bitCount) throw new ArgumentOutOfRangeException(nameof(setBits));
            bytes[bit / 8] |= (byte)(1 << (bit % 8));
        }
        return bytes;
    }

    protected static int Product(IReadOnlyList<LawRole> roles) => roles.Aggregate(1, (p, r) => checked(p * r.Domain.Size));

    public override string ToString() => $"{Kind} law {Name} ({ContentHash})";
}
