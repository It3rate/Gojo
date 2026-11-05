namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>An immutable, ordered arrangement of existing passage laws and
/// local role references. It retains no source owner, current values, glyph,
/// concept label or constructor. Different bindings reuse this whole structure.
/// This is a reusable passage pattern, not automatic discovery or a new unit.</summary>
public sealed class RetainedPassagePattern
{
    private readonly PassageLaw[] laws;
    private readonly EndpointRows rows = new();
    public int RoleCount { get; }
    public int StepCount => laws.Length;
    public int ReferenceBits { get; }

    private RetainedPassagePattern(int roleCount, byte[][] bitmaps, int[][] roles)
    {
        if (roleCount is < 3 or > 5) throw new ArgumentOutOfRangeException(nameof(roleCount));
        if (bitmaps.Length == 0 || bitmaps.Length != roles.Length)
            throw new ArgumentException("Supply one role row for each retained passage.");
        RoleCount = roleCount;
        ReferenceBits = OrderedBoundary.Bits(roleCount);
        laws = new PassageLaw[bitmaps.Length];
        for (var step = 0; step < laws.Length; step++)
        {
            var row = roles[step];
            if (row.Length != 3 || row.Distinct().Count() != 3 || row.Any(r => r < 0 || r >= roleCount))
                throw new ArgumentException("Each passage needs three distinct roles within this pattern.");
            laws[step] = new PassageLaw(bitmaps[step]);
            rows.Append(ReferenceBits, row);
        }
    }

    /// <summary>Capture actual law bits and ordered connections. The source's
    /// observations and material are deliberately not copied into the pattern.</summary>
    public static RetainedPassagePattern Capture(PassageFixture source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var bitmaps = new byte[source.Steps.Count][];
        var roles = new int[source.Steps.Count][];
        for (var step = 0; step < source.Steps.Count; step++)
        {
            var (table, row) = source.Steps[step];
            bitmaps[step] = table.LawBytes();
            roles[step] = table.Wiring(row).Select(port => Array.IndexOf(source.Ports, port)).ToArray();
        }
        return new(source.Ports.Length, bitmaps, roles);
    }

    public int[] Wiring(int step) => rows.Read(new(3, ReferenceBits, step));
    public byte[] RowBytes() => rows.PackedBytes(3, ReferenceBits);
    public byte[] LawBytes(int step) => laws[step].PackedBytes();

    /// <summary>V1: version, role count, then fixed-size ordered records until
    /// the end. Each record contains eight law bytes and a packed three-role row.
    /// Row width follows from role count; final row-byte padding must be zero.
    /// No material state, name, or source identifier is serialized.</summary>
    public byte[] Encode()
    {
        var rowLength = (3 * ReferenceBits + 7) / 8;
        var bytes = new byte[checked(2 + StepCount * (8 + rowLength))];
        bytes[0] = 1;
        bytes[1] = (byte)RoleCount;
        for (var step = 0; step < StepCount; step++)
        {
            var at = 2 + step * (8 + rowLength);
            LawBytes(step).CopyTo(bytes, at);
            var row = Wiring(step);
            for (var role = 0; role < 3; role++)
            for (var bit = 0; bit < ReferenceBits; bit++)
                if ((row[role] & (1 << bit)) != 0)
                {
                    var offset = role * ReferenceBits + bit;
                    bytes[at + 8 + offset / 8] |= (byte)(1 << (offset % 8));
                }
        }
        return bytes;
    }

    public static RetainedPassagePattern Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2 || bytes[0] != 1 || bytes[1] is < 3 or > 5)
            throw new FormatException("Unknown or incomplete passage-pattern framing.");
        var roleCount = bytes[1];
        var bits = OrderedBoundary.Bits(roleCount);
        var rowLength = (3 * bits + 7) / 8;
        var stride = 8 + rowLength;
        if (bytes.Length == 2 || (bytes.Length - 2) % stride != 0)
            throw new FormatException("A retained passage record is missing or truncated.");
        var count = (bytes.Length - 2) / stride;
        var bitmaps = new byte[count][];
        var roles = new int[count][];
        for (var step = 0; step < count; step++)
        {
            var at = 2 + step * stride;
            bitmaps[step] = bytes.Slice(at, 8).ToArray();
            var packed = bytes.Slice(at + 8, rowLength);
            var used = 3 * bits % 8;
            if (used != 0 && (packed[^1] >> used) != 0)
                throw new FormatException("Nonzero padding is not a role reference.");
            var row = new int[3];
            for (var role = 0; role < 3; role++)
            for (var bit = 0; bit < bits; bit++)
            {
                var offset = role * bits + bit;
                if ((packed[offset / 8] & (1 << (offset % 8))) != 0) row[role] |= 1 << bit;
            }
            roles[step] = row;
        }
        try { return new(roleCount, bitmaps, roles); }
        catch (ArgumentException error) { throw new FormatException("Invalid retained passage law or role row.", error); }
    }

    /// <summary>Bind roles to this owner's exposed local ports. Binding neither
    /// supplies values nor runs a step. Incompatible aliases or migrated contexts
    /// remain non-readable through the existing passage grammar.</summary>
    public BoundPassagePattern Bind(PortStructure owner, int[] rolePorts) => new(this, owner, rolePorts);
}

/// <summary>An application of one retained pattern to owner-local exposed ports.
/// All observed states and visibility masks use PATTERN ROLE ORDER, even when
/// those roles map to differently ordered target port positions.</summary>
public sealed class BoundPassagePattern
{
    private readonly int[] ports;
    private readonly (PassageConnections Table, ConnectionRow Row)[] steps;
    public RetainedPassagePattern Pattern { get; }
    public int ReferenceBits { get; }

    internal BoundPassagePattern(RetainedPassagePattern pattern, PortStructure owner, int[] rolePorts)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(rolePorts);
        if (rolePorts.Length != pattern.RoleCount)
            throw new ArgumentException("Bind every retained role exactly once.", nameof(rolePorts));
        if (rolePorts.Any(port => port is < 0 or > 65535))
            throw new ArgumentOutOfRangeException(nameof(rolePorts), "Local port references must fit sixteen bits.");
        owner.RequirePassagePorts(rolePorts);
        Pattern = pattern;
        ports = rolePorts.ToArray();
        ReferenceBits = OrderedBoundary.Bits(ports.Max() + 1);
        steps = new (PassageConnections Table, ConnectionRow Row)[pattern.StepCount];
        for (var step = 0; step < steps.Length; step++)
        {
            var roles = pattern.Wiring(step);
            var table = new PassageConnections(owner, new PassageLaw(pattern.LawBytes(step)));
            var row = table.Connect(ReferenceBits, ports[roles[0]], ports[roles[1]], ports[roles[2]]);
            steps[step] = (table, row);
        }
    }

    public int[] RolePorts() => ports.ToArray();
    public int[] Wiring(int step) => steps[step].Table.Wiring(steps[step].Row);
    public byte[] RowBytes(int step) => steps[step].Table.PackedRows(ReferenceBits);
    public byte[] LawBytes(int step) => steps[step].Table.LawBytes();
    public uint Observe(int visibleRoles = -1) => steps[0].Table.ObserveScope(ports,
        visibleRoles == -1 ? (1 << ports.Length) - 1 : visibleRoles);

    public uint Predict(uint beforeSupport)
    {
        foreach (var (table, row) in steps) beforeSupport = table.PredictScope(row, ports, beforeSupport);
        return beforeSupport;
    }

    public bool TryStep(uint beforeSupport, out byte afterState) =>
        PassageConnections.TrySequence(ports, beforeSupport, out afterState, steps);
}
