namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct ReadLocation(int Table, ConnectionRow Row);

/// <summary>Small append-only observation wiring. Each table owns its actual
/// allowed-tuple bitmap and packed endpoint rows. There is no operation opcode.
/// Endpoint 2*n imports published port n; endpoint 2*n+1 is calculated read n.
/// Only already existing inputs can be connected, so local wiring is acyclic.
/// This experiment is single-threaded, with no scheduler or material transfer.</summary>
public sealed class ReadConnections
{
    private sealed class Table(BitRelation law)
    {
        public readonly BitRelation Law = law;
        public readonly EndpointRows Rows = new();
    }

    private readonly List<IPrimitiveReadPort> sources = [];
    private readonly List<ReadLocation> calculated = [];
    private readonly List<Table> tables = [];

    public int Import(IPrimitiveReadPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        // Reusing the actual exposed port preserves local correspondence. Equal
        // readings or different public ports do not establish that promise.
        var index = sources.FindIndex(candidate => ReferenceEquals(candidate, port) ||
            (candidate is PortConnection first && port is PortConnection second && first.SamePortAs(second)));
        if (index >= 0) return checked(2 * index);
        var endpoint = checked(2 * sources.Count);
        sources.Add(port);
        return endpoint;
    }

    public int AddTable(BitRelation law)
    {
        ArgumentNullException.ThrowIfNull(law);
        tables.Add(new Table(law));
        return tables.Count - 1;
    }

    /// <summary>Adds [inputs..., calculated output]. The output has no value
    /// allocation; its endpoint resolves to this law and this ordered row.</summary>
    public CalculatedPort Connect(int table, int referenceBits, params int[] inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        var selected = tables[table];
        if (inputs.Length != selected.Law.Arity - 1) throw new ArgumentException("Wrong number of input roles.", nameof(inputs));
        foreach (var input in inputs) CheckEndpoint(input);
        var output = checked(2 * calculated.Count + 1);
        calculated.EnsureCapacity(checked(calculated.Count + 1));
        var row = selected.Rows.Append(referenceBits, [.. inputs, output]);
        calculated.Add(new ReadLocation(table, row));
        return new CalculatedPort(this, output);
    }

    /// <summary>Adds a row whose output is an existing endpoint. It records a
    /// read/capture correspondence, not an automatic write or feedback loop.</summary>
    public ConnectionRow Wire(int table, int referenceBits, params int[] endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var selected = tables[table];
        if (endpoints.Length != selected.Law.Arity) throw new ArgumentException("Wrong number of endpoint roles.", nameof(endpoints));
        foreach (var endpoint in endpoints) CheckEndpoint(endpoint);
        return selected.Rows.Append(referenceBits, endpoints);
    }

    public byte Read(int endpoint) => Read(endpoint, new Dictionary<int, byte>());

    /// <summary>Reads possible outputs from the inputs. A held output endpoint's
    /// previous sample is not an input to this prediction.</summary>
    public byte Predict(int table, ConnectionRow row) => Predict(table, row, new Dictionary<int, byte>());

    /// <summary>Caller explicitly supplies the owner and binding to receive a
    /// sample. The row must end at that actual published binding. A missing,
    /// ambiguous, incompatible, or too-wide target leaves the old sample intact.</summary>
    public bool TryCapture(int table, ConnectionRow row, PortStructure owner, int binding)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var ends = Wiring(table, row);
        var target = ends[^1];
        if ((target & 1) != 0 || sources[target / 2] is not PortConnection port || !port.RefersTo(owner, binding))
            return false;
        return owner.TryCaptureObservation(binding, Predict(table, row));
    }

    public ReadLocation LocationOf(int endpoint)
    {
        CheckEndpoint(endpoint);
        if ((endpoint & 1) == 0) throw new ArgumentException("Imported ports have no calculated row.", nameof(endpoint));
        return calculated[endpoint / 2];
    }

    public int[] Wiring(int table, ConnectionRow row)
    {
        var selected = tables[table];
        if (row.Arity != selected.Law.Arity) throw new ArgumentException("Row does not fit this table's law.", nameof(row));
        return selected.Rows.Read(row);
    }

    public byte[] LawBytes(int table) => tables[table].Law.PackedBytes();
    public byte[] PackedRows(int table, int referenceBits) => tables[table].Rows.PackedBytes(tables[table].Law.Arity, referenceBits);
    public int RowCount(int table, int referenceBits) => tables[table].Rows.Count(tables[table].Law.Arity, referenceBits);

    private byte Read(int endpoint, Dictionary<int, byte> sampled)
    {
        CheckEndpoint(endpoint);
        if (sampled.TryGetValue(endpoint, out var previous)) return previous;
        var result = (endpoint & 1) == 0 ? sources[endpoint / 2].ReadSupport()
            : Predict(calculated[endpoint / 2].Table, calculated[endpoint / 2].Row, sampled);
        sampled.Add(endpoint, result);
        return result;
    }

    private byte Predict(int table, ConnectionRow row, Dictionary<int, byte> sampled)
    {
        var ends = Wiring(table, row);
        var inputs = ends[..^1];
        var supports = inputs.Select(endpoint => Read(endpoint, sampled)).ToArray();
        // A repeated endpoint must choose the same candidate word in both
        // roles. Correlations through different derived paths remain coarse.
        return tables[table].Law.Predict(supports, inputs);
    }

    private void CheckEndpoint(int endpoint)
    {
        if (endpoint < 0 || endpoint / 2 >= ((endpoint & 1) == 0 ? sources.Count : calculated.Count))
            throw new ArgumentOutOfRangeException(nameof(endpoint), "Endpoint does not exist in this local structure.");
    }
}

/// <summary>A non-draining calculated output. It owns no independent amount.</summary>
public sealed class CalculatedPort : IPrimitiveReadPort
{
    private readonly ReadConnections owner;
    public int Endpoint { get; }
    internal CalculatedPort(ReadConnections owner, int endpoint) { this.owner = owner; Endpoint = endpoint; }
    public byte ReadSupport() => owner.Read(Endpoint);
}
