namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A full observer walk, not a cached operational claim or geometry proof.</summary>
public readonly record struct StructuralRouteRead(bool Continuous, bool Closed, int EdgesRead);

/// <summary>Witness of an explicit local reference edit. Counts are packed topology
/// rows decoded/written by this fixture, not machine instructions or bytes moved.</summary>
public sealed record StructuralRouteEdit(bool Applied, int RowsRead, int RowsWritten,
    bool LayoutChanged, int[] AffectedEdges);

/// <summary>
/// Three local storage grammars: endpoint pairs (0), shared successive vertices
/// (1), and shared cyclic vertices (2). The final cyclic successor is the first
/// actual vertex field, not a freely editable final reference. These layouts
/// specify topology; none specifies geometry, flow permission or construction history.
/// A separate positional bucket stores one coarse ODV observation per occurrence.
/// </summary>
public sealed class StructuralRoute
{
    private EndpointRows topology = new();
    private readonly PositionalBuckets words = new();
    private int cursor, first, arrival;

    public int Layout { get; private set; }
    public int SiteCount { get; }
    public int SiteBits { get; }
    public int StepCount { get; }
    public int RowArity => Layout == 0 ? 2 : 1;
    public int RowCount => topology.Count(RowArity, SiteBits);
    public int OrientationGroup { get; }
    public int DirectionGroup { get; }
    public int WordWidth { get; }
    public long TopologyBits => (long)RowCount * RowArity * SiteBits;
    public long ValueBits => (long)StepCount * WordWidth;
    public long PayloadBits => TopologyBits + ValueBits;
    public long TopologyRevision { get; private set; }
    public long Inspections { get; private set; }
    public long RowsRead { get; private set; }
    public long RowsWritten { get; private set; }
    public long WordWrites { get; private set; }
    public byte ContinuitySupport { get; private set; } = 3;
    public byte ClosureSupport { get; private set; } = 3;
    public int? FailedStep { get; private set; }
    public IPrimitiveReadPort Continuity { get; }
    public IPrimitiveReadPort Closure { get; }

    private sealed class ReadPort(StructuralRoute route, bool closure) : IPrimitiveReadPort
    {
        public byte ReadSupport() => closure ? route.ClosureSupport : route.ContinuitySupport;
    }

    private StructuralRoute(int layout, int siteCount, int[][] rows,
        int orientationGroup, int directionGroup, byte[]? observed)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (layout is < 0 or > 2 || siteCount is < 1 or > 65536)
            throw new ArgumentOutOfRangeException(nameof(layout));
        Layout = layout;
        SiteCount = siteCount;
        SiteBits = OrderedBoundary.Bits(siteCount);
        StepCount = layout == 1 ? rows.Length - 1 : rows.Length;
        if (StepCount is < 1 or > 65535)
            throw new ArgumentException("Supply one to 65535 actual edge occurrences.", nameof(rows));
        OrientationGroup = orientationGroup;
        DirectionGroup = directionGroup;
        WordWidth = 1 + (orientationGroup == 2 ? 2 : directionGroup == 2 ? 1 : 0);
        PositionalBuckets.ValueBits(orientationGroup, directionGroup, WordWidth);
        if (observed is not null && observed.Length != StepCount)
            throw new ArgumentException("Supply one observation per edge occurrence.", nameof(observed));
        foreach (var row in rows)
        {
            if (row is null || row.Length != RowArity)
                throw new ArgumentException("A row must match the selected storage grammar.", nameof(rows));
            foreach (var site in row) CheckSite(site);
            topology.Append(SiteBits, row);
        }
        for (var edge = 0; edge < StepCount; edge++)
        {
            var word = observed is null
                ? ThreeBitMeasure.Encode(orientationGroup == 1, directionGroup == 1, true)
                : observed[edge];
            words.Append(orientationGroup, directionGroup, WordWidth, EncodeWord(word));
        }
        Continuity = new ReadPort(this, false);
        Closure = new ReadPort(this, true);
    }

    public static StructuralRoute Pairs(int siteCount, int[][] pairs,
        int orientationGroup = 0, int directionGroup = 0, byte[]? words = null) =>
        new(0, siteCount, pairs, orientationGroup, directionGroup, words);

    public static StructuralRoute Chain(int siteCount, int[] vertices,
        int orientationGroup = 0, int directionGroup = 0, byte[]? words = null) =>
        new(1, siteCount, VertexRows(vertices), orientationGroup, directionGroup, words);

    public static StructuralRoute Cycle(int siteCount, int[] vertices,
        int orientationGroup = 0, int directionGroup = 0, byte[]? words = null) =>
        new(2, siteCount, VertexRows(vertices), orientationGroup, directionGroup, words);

    private static int[][] VertexRows(int[] vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        return vertices.Select(site => new[] { site }).ToArray();
    }

    private void CheckSite(int site) =>
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)site, (uint)SiteCount);

    private void CheckEdge(int edge) =>
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)edge, (uint)StepCount);

    private ulong EncodeWord(byte word)
    {
        if (word > 7) throw new ArgumentOutOfRangeException(nameof(word));
        return PositionalBuckets.Encode(OrientationGroup, DirectionGroup, WordWidth,
            (word & 4) != 0, (word & 2) != 0, (ulong)(word & 1));
    }

    private SlotAddress Address(int edge) => new(OrientationGroup, DirectionGroup, WordWidth, edge);

    public byte Word(int edge)
    {
        CheckEdge(edge);
        words.TryRead(Address(edge), out var raw);
        return PositionalBuckets.CoarseRead(Address(edge), raw);
    }

    /// <summary>Supply a new local observation. This is not a material delta or
    /// settlement, and never changes topology or the topology inspection witness.</summary>
    public bool SetWord(int edge, byte word)
    {
        CheckEdge(edge);
        var raw = EncodeWord(word); // Reject inherited-bit edits before mutation.
        words.TryRead(Address(edge), out var before);
        if (raw == before) return false;
        words.TryWrite(Address(edge), raw);
        WordWrites++;
        return true;
    }

    public byte[] WordBytes() => words.PackedBytes(OrientationGroup, DirectionGroup, WordWidth);
    public byte[] TopologyBytes() => topology.PackedBytes(RowArity, SiteBits);
    public int[][] TopologyRows() => Enumerable.Range(0, RowCount)
        .Select(i => topology.Read(new(RowArity, SiteBits, i))).ToArray();

    public int[] Edge(int edge)
    {
        CheckEdge(edge);
        if (Layout == 0) return topology.Read(new(2, SiteBits, edge));
        var from = topology.Read(new(1, SiteBits, edge))[0];
        var next = Layout == 2 ? (edge + 1) % RowCount : edge + 1;
        return [from, topology.Read(new(1, SiteBits, next))[0]];
    }

    /// <summary>Independent full observer scan. Does not advance the budgeted
    /// operational reader or supply its answer through an inspection shortcut.</summary>
    public StructuralRouteRead ReadAll()
    {
        var beginning = Edge(0);
        var previous = beginning[1];
        var continuous = true;
        for (var edge = 1; edge < StepCount; edge++)
        {
            var next = Edge(edge);
            continuous &= previous == next[0];
            previous = next[1];
        }
        return new(continuous, continuous && previous == beginning[0], StepCount);
    }

    /// <summary>Budget counts logical inspections. Pairs consume one per row;
    /// a shared run consumes one inspection and reads its two end rows; a cycle
    /// consumes one grammar inspection and needs no endpoint rows. Payload has
    /// already been structurally validated at construction/load, whose cost is
    /// not included. Support 3 is uninspected, never an empty or false claim.</summary>
    public int Inspect(int budget)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(budget);
        var used = 0;
        while (used < budget && ContinuitySupport == 3)
        {
            used++;
            Inspections++;
            if (Layout != 0)
            {
                ContinuitySupport = 2;
                if (Layout == 2) ClosureSupport = 2;
                else
                {
                    var from = topology.Read(new(1, SiteBits, 0))[0];
                    var to = topology.Read(new(1, SiteBits, RowCount - 1))[0];
                    RowsRead += 2;
                    ClosureSupport = from == to ? (byte)2 : (byte)1;
                }
                continue;
            }
            var row = topology.Read(new(2, SiteBits, cursor));
            RowsRead++;
            if (cursor == 0) first = row[0];
            else if (arrival != row[0])
            {
                ContinuitySupport = ClosureSupport = 1;
                FailedStep = cursor;
                continue;
            }
            arrival = row[1];
            cursor++;
            if (cursor == StepCount)
            {
                ContinuitySupport = 2;
                ClosureSupport = arrival == first ? (byte)2 : (byte)1;
            }
        }
        return used;
    }

    private void Invalidate()
    {
        TopologyRevision++;
        cursor = first = arrival = 0;
        ContinuitySupport = ClosureSupport = 3;
        FailedStep = null;
    }

    /// <summary>Retarget one independently stored endpoint. Shared layouts have
    /// no such independently writable endpoint and return an unapplied witness.</summary>
    public StructuralRouteEdit TryRetargetEndpoint(int edge, int role, int site)
    {
        CheckEdge(edge);
        CheckSite(site);
        if (role is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(role));
        if (Layout != 0) return new(false, 0, 0, false, []);
        var rows = TopologyRows();
        RowsRead += rows.Length;
        if (rows[edge][role] == site) return new(false, rows.Length, 0, false, []);
        rows[edge][role] = site;
        Replace(rows);
        Invalidate();
        return new(true, rows.Length, rows.Length, false, [edge]);
    }

    /// <summary>Retarget one shared vertex field, moving all edge endpoints
    /// that actually use that field. Equal site references in other fields are
    /// not silently retargeted. This is an explicit structural edit.</summary>
    public StructuralRouteEdit RetargetVertex(int row, int site)
    {
        CheckSite(site);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)row, (uint)RowCount);
        if (Layout == 0) return new(false, 0, 0, false, []);
        var rows = TopologyRows();
        RowsRead += rows.Length;
        if (rows[row][0] == site) return new(false, rows.Length, 0, false, []);
        rows[row][0] = site;
        var affected = Enumerable.Range(0, StepCount).Where(edge => edge == row ||
            (Layout == 2 ? (edge + 1) % RowCount : edge + 1) == row).ToArray();
        Replace(rows);
        Invalidate();
        return new(true, rows.Length, rows.Length, false, affected);
    }

    /// <summary>Expand positional topology into pairs. It preserves occurrence
    /// order, site identity and observations, while enabling independent endpoint
    /// edits. The budgeted read restarts conservatively after the representation
    /// edit; the exact observer can verify equivalence without supplying it.</summary>
    public StructuralRouteEdit Unpack()
    {
        if (Layout == 0) return new(false, 0, 0, false, []);
        var source = TopologyRows();
        var rows = Enumerable.Range(0, StepCount).Select(edge => new[]
        {
            source[edge][0], source[Layout == 2 ? (edge + 1) % source.Length : edge + 1][0]
        }).ToArray();
        RowsRead += source.Length;
        Layout = 0;
        Replace(rows);
        Invalidate();
        return new(true, source.Length, rows.Length, true, []);
    }

    private void Replace(int[][] rows)
    {
        var replacement = new EndpointRows();
        foreach (var row in rows) replacement.Append(SiteBits, row);
        topology = replacement;
        RowsWritten += rows.Length;
    }

    /// <summary>V1 framing: version, layout, O group, D group, site count varint,
    /// topology-row count varint, topology bytes, observation bytes. Counts and
    /// padding delimit storage only. Derived inspection state is not serialized.</summary>
    public byte[] Encode()
    {
        using var output = new MemoryStream();
        output.WriteByte(1);
        output.WriteByte((byte)Layout);
        output.WriteByte((byte)OrientationGroup);
        output.WriteByte((byte)DirectionGroup);
        Count(output, SiteCount);
        Count(output, RowCount);
        output.Write(TopologyBytes());
        output.Write(WordBytes());
        return output.ToArray();
    }

    public static StructuralRoute Decode(ReadOnlySpan<byte> bytes)
    {
        var input = new Reader(bytes);
        if (input.Byte() != 1) throw new FormatException("Unknown route transport version.");
        var layout = input.Byte();
        var orientation = input.Byte();
        var direction = input.Byte();
        var sites = input.Count();
        var count = input.Count();
        if (layout > 2 || sites is < 1 or > 65536 || count < 1 || count > (layout == 1 ? 65536 : 65535))
            throw new FormatException("Invalid route dimensions.");
        var steps = layout == 1 ? count - 1 : count;
        if (steps == 0) throw new FormatException("A route needs an actual occurrence.");
        var bits = OrderedBoundary.Bits(sites);
        var arity = layout == 0 ? 2 : 1;
        var width = 1 + (orientation == 2 ? 2 : direction == 2 ? 1 : 0);
        try { PositionalBuckets.ValueBits(orientation, direction, width); }
        catch (ArgumentException) { throw new FormatException("Invalid inherited-bit positions."); }
        var encodedRows = input.Packed((long)count * arity * bits);
        var encodedWords = input.Packed((long)steps * width);
        if (input.Remaining != 0) throw new FormatException("Trailing route transport bytes.");
        var rows = new int[count][];
        for (var row = 0; row < count; row++)
        {
            rows[row] = new int[arity];
            for (var role = 0; role < arity; role++)
            {
                var site = ReadBits(encodedRows, ((long)row * arity + role) * bits, bits);
                if (site >= sites) throw new FormatException("A reference is outside its owner.");
                rows[row][role] = site;
            }
        }
        var observations = new byte[steps];
        for (var edge = 0; edge < steps; edge++)
        {
            var raw = ReadBits(encodedWords, (long)edge * width, width);
            observations[edge] = PositionalBuckets.CoarseRead(new(orientation, direction, width, edge), (ulong)raw);
        }
        return new(layout, sites, rows, orientation, direction, observations);
    }

    private static int ReadBits(ReadOnlySpan<byte> source, long at, int bits)
    {
        var result = 0;
        for (var bit = 0; bit < bits; bit++)
        {
            var position = at + bit;
            if ((source[(int)(position / 8)] & (1 << (int)(position % 8))) != 0) result |= 1 << bit;
        }
        return result;
    }

    private static void Count(Stream output, int count)
    {
        do
        {
            var value = (byte)(count & 127);
            count >>= 7;
            output.WriteByte((byte)(value | (count == 0 ? 0 : 128)));
        } while (count != 0);
    }

    private ref struct Reader(ReadOnlySpan<byte> bytes)
    {
        private readonly ReadOnlySpan<byte> bytes = bytes;
        private int position;
        public int Remaining => bytes.Length - position;
        public byte Byte() => position < bytes.Length ? bytes[position++] : throw new FormatException("Truncated route transport.");
        public int Count()
        {
            uint count = 0;
            for (var shift = 0; shift <= 28; shift += 7)
            {
                var next = Byte();
                if (shift == 28 && next > 7) throw new FormatException("Route count overflow.");
                count |= (uint)(next & 127) << shift;
                if ((next & 128) == 0)
                {
                    if (shift != 0 && next == 0) throw new FormatException("Noncanonical route count.");
                    return (int)count;
                }
            }
            throw new FormatException("Unterminated route count.");
        }
        public ReadOnlySpan<byte> Packed(long bits)
        {
            var length = (bits + 7) / 8;
            if (length > Remaining) throw new FormatException("Truncated route payload.");
            var result = bytes.Slice(position, (int)length);
            position += (int)length;
            var used = (int)(bits % 8);
            if (used != 0 && (result[^1] >> used) != 0) throw new FormatException("Nonzero route padding.");
            return result;
        }
    }
}
