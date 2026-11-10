namespace Gojo.Encoding.Core.Packed;

/// <summary>A cycle of explicit references, with a one-bit activation moving
/// through ordinary passages. Equal geometric subdivisions, when intended,
/// are an additional shared-whole contract, not a consequence of cycle size.</summary>
public sealed class ReferenceCycle
{
    private readonly int[] sites;
    public PulseCircuit Circuit { get; }
    public int[] Sites => sites.ToArray();
    public int Enable { get; }
    public int Table { get; }

    public ReferenceCycle(int positions, IPrimitiveReadPort? enable = null)
    {
        // One local guard follows the state sites. This bound comes from the
        // selected 16-bit endpoint adapter, not a conceptual limit on loops.
        if (positions is < 2 or > 65535) throw new ArgumentOutOfRangeException(nameof(positions));
        Circuit = new(OrderedBoundary.Bits(positions + 1));
        sites = Enumerable.Range(0, positions).Select(i => Circuit.State(i == 0)).ToArray();
        Enable = Circuit.Guard(true);
        if (enable is not null) Circuit.Bind(Enable, enable);
        Table = Circuit.AddTable(PassageLaw.Controlled());
        for (var i = 0; i < positions; i++) Circuit.Link(Table, sites[i], Enable, sites[(i + 1) % positions]);
    }

    public IPrimitiveReadPort AtStart => Circuit.Expose(sites[0]);
    public int Position => Circuit.Active is int active ? Array.IndexOf(sites, active) : -1;
}

/// <summary>Read-only composition through existing bitmap laws and public
/// ports. It publishes detection/Boolean evidence; it does not copy activation
/// or material. The historical Presence name does not make V a selection bit.
/// All uses the selected positive Boolean basis; Any requires matching O/D.</summary>
public static class PresencePorts
{
    public static IPrimitiveReadPort Any(params IPrimitiveReadPort[] inputs)
        => Combine(ConnectionLaws.Presence(), inputs);

    public static IPrimitiveReadPort All(params IPrimitiveReadPort[] inputs)
        => Combine(BitRelation.FromTuples(3,
            from a in new byte[] { 0, 1 }
            from b in new byte[] { 0, 1 }
            select new byte[] { a, b, (byte)(a & b) }), inputs);

    private static IPrimitiveReadPort Combine(BitRelation law, IPrimitiveReadPort[] inputs)
    {
        if (inputs.Length == 0) throw new ArgumentException("Supply at least one source.", nameof(inputs));
        var referenceBits = OrderedBoundary.Bits(checked(inputs.Length * 2));
        if (referenceBits > 16) throw new ArgumentOutOfRangeException(nameof(inputs), "This adapter uses at most 16-bit references.");
        var reads = new ReadConnections();
        var table = reads.AddTable(law);
        var current = reads.Import(inputs[0]);
        for (var i = 1; i < inputs.Length; i++)
            current = reads.Connect(table, referenceBits, current, reads.Import(inputs[i])).Endpoint;
        return new PublishedRead(reads, current);
    }

    private sealed class PublishedRead(ReadConnections reads, int endpoint) : IPrimitiveReadPort
    {
        public byte ReadSupport() => reads.Read(endpoint);
    }
}
