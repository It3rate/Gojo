namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A cycle of explicit references, with a one-bit activation moving
/// through ordinary passages. Equal geometric subdivisions, when intended,
/// are an additional shared-whole contract, not a consequence of cycle size.</summary>
public sealed class ReferenceCycle
{
    public PulseCircuit Circuit { get; }
    public int[] Sites { get; }
    public int Enable { get; }
    public int Table { get; }

    public ReferenceCycle(int positions, IPrimitiveReadPort? enable = null)
    {
        if (positions is < 2 or > 32) throw new ArgumentOutOfRangeException(nameof(positions));
        Circuit = new();
        Sites = Enumerable.Range(0, positions).Select(i => Circuit.State(i == 0)).ToArray();
        Enable = Circuit.Guard(true);
        if (enable is not null) Circuit.Bind(Enable, enable);
        Table = Circuit.AddTable(PassageLaw.Controlled());
        for (var i = 0; i < positions; i++) Circuit.Link(Table, Sites[i], Enable, Sites[(i + 1) % positions]);
    }

    public IPrimitiveReadPort AtStart => Circuit.Expose(Sites[0]);
    public int Position => Array.IndexOf(Sites, Circuit.Active!.Value);
}

/// <summary>Read-only composition through existing bitmap laws and public
/// ports. It publishes evidence; it does not copy activation or material.</summary>
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
        var reads = new ReadConnections();
        var table = reads.AddTable(law);
        var current = reads.Import(inputs[0]);
        for (var i = 1; i < inputs.Length; i++)
            current = reads.Connect(table, 8, current, reads.Import(inputs[i])).Endpoint;
        return new PublishedRead(reads, current);
    }

    private sealed class PublishedRead(ReadConnections reads, int endpoint) : IPrimitiveReadPort
    {
        public byte ReadSupport() => reads.Read(endpoint);
    }
}
