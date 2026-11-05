namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A retained observation of one attempted pulse. CompatibleUses are
/// all signed uses matching the observed movement, not an invented record of
/// which parallel row was selected. InputTransfer identifies the separate input
/// passage. Predictions and movement witnesses do not control later pulses.</summary>
public sealed record FamilyWalkWitness(int Pulse, PulseStep Step, int? FromSite,
    int? ToSite, bool InputTransfer, int[] CompatibleUses);

/// <summary>An explicitly selected operational interpretation of retained family
/// uses. Each distinct signed use supplies one passage; its law is given by the
/// caller or defaults to unconditional handoff. Family values are not rewritten
/// or treated as drawing material. One preloaded request enters through its own
/// passage. There are no shape names, geometric bases, or hidden route choices.
/// This bounded adapter excludes self-loop uses: its passage grammar requires
/// distinct activation sites. A later phase construction may support them.</summary>
public sealed class FamilyWalk
{
    private readonly PulseCircuit circuit;
    private readonly EndpointRows sites = new(), routes = new(), input = new();
    private readonly List<FamilyWalkWitness> trace = [];
    private readonly Dictionary<string, int> lawTables = [];
    public FamilyTable Family { get; }
    public FamilyEntry Entry { get; }
    public FamilyUseTable Uses { get; }
    public int ReferenceBits { get; }
    public int InputState { get; }
    public int GuardState { get; }
    public int StartSite { get; }
    public int StateCount => Family.Template.SiteCount + 1;
    public int GuardCount => 1;
    public int RouteCount => routes.Count(6, ReferenceBits);
    public int TableCount => lawTables.Count;

    public FamilyWalk(FamilyTable table, FamilyEntry entry, FamilyUseTable uses,
        int startSite = 0, IReadOnlyDictionary<int, PassageLaw>? laws = null, IPrimitiveReadPort? control = null)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(uses);
        if (!ReferenceEquals(table.Template, uses.Template))
            throw new ArgumentException("Uses must refer to this exact family template.", nameof(uses));
        table.ReadRaw(entry, 0);
        if (startSite < 0 || startSite >= table.Template.SiteCount || table.Template.SiteCount > 65534)
            throw new ArgumentOutOfRangeException(nameof(startSite), "A local start and the extra input/guard sites must fit sixteen bits.");
        var selected = Enumerable.Range(0, uses.UseCount).Select(uses.SignedUse).Distinct().ToArray();
        foreach (var use in selected)
        {
            var link = table.Template.Link(use >> 1);
            if (link.Start == link.End)
                throw new ArgumentException("Self-loop uses need an additional phase construction; this adapter requires distinct passage sites.", nameof(uses));
        }
        if (laws is not null)
            foreach (var pair in laws)
            {
                if (!selected.Contains(pair.Key))
                    throw new ArgumentException("A law binding must name a selected signed use.", nameof(laws));
                ArgumentNullException.ThrowIfNull(pair.Value);
            }

        Family = table; Entry = entry; Uses = uses; StartSite = startSite;
        ReferenceBits = Math.Max(uses.UseBits, OrderedBoundary.Bits(table.Template.SiteCount + 2));
        // At most one table per distinct use plus the input law. These table
        // and row positions are themselves stored in the correspondence rows.
        ReferenceBits = Math.Max(ReferenceBits, OrderedBoundary.Bits(selected.Length + 1));
        circuit = new PulseCircuit(ReferenceBits);
        InputState = circuit.State(active: true);
        GuardState = circuit.Guard();
        if (control is not null) circuit.Bind(GuardState, control);
        for (var site = 0; site < table.Template.SiteCount; site++)
            sites.Append(ReferenceBits, site, circuit.State());
        var unconditional = PassageLaw.Unconditional();
        var inputTable = LawTable(unconditional);
        var inputRow = circuit.Link(inputTable, InputState, GuardState, StateForSite(startSite));
        input.Append(ReferenceBits, InputState, GuardState, StateForSite(startSite), inputTable, inputRow.Index);
        foreach (var use in selected)
        {
            var link = table.Template.Link(use >> 1);
            var reverse = (use & 1) != 0;
            var source = StateForSite(reverse ? link.End : link.Start);
            var target = StateForSite(reverse ? link.Start : link.End);
            var law = laws is not null && laws.TryGetValue(use, out var supplied) ? supplied : unconditional;
            var tableIndex = LawTable(law);
            var row = circuit.Link(tableIndex, source, GuardState, target);
            routes.Append(ReferenceBits, use, source, GuardState, target, tableIndex, row.Index);
        }
    }

    private int LawTable(PassageLaw law)
    {
        var key = Convert.ToHexString(law.PackedBytes());
        if (lawTables.TryGetValue(key, out var existing)) return existing;
        var created = circuit.AddTable(law);
        lawTables.Add(key, created);
        return created;
    }

    public int StateForSite(int familySite) => SiteRow(familySite)[1];
    public int[] SiteRow(int site) => sites.Read(new(2, ReferenceBits, site));
    public byte[] SiteBytes() => sites.PackedBytes(2, ReferenceBits);
    public int[] RouteRow(int route) => routes.Read(new(6, ReferenceBits, route));
    public byte[] RouteBytes() => routes.PackedBytes(6, ReferenceBits);
    public int[] InputRow() => input.Read(new(5, ReferenceBits, 0));
    public byte[] InputBytes() => input.PackedBytes(5, ReferenceBits);
    public byte[] PackedRows(int table) => circuit.PackedRows(table);
    public byte[] LawBytes(int table) => circuit.LawBytes(table);
    public byte[] ValueBytes() => circuit.ValueBytes();
    public byte ReadStateSupport(int state) => circuit.Expose(state).ReadSupport();

    public int? ActiveFamilySite => FamilySite(circuit.Active);
    public int OccupiedStateCount => (ReadStateSupport(InputState) == 2 ? 1 : 0) +
        Enumerable.Range(0, Family.Template.SiteCount).Count(site => ReadStateSupport(StateForSite(site)) == 2);

    private int? FamilySite(int? state)
    {
        if (state is null) return null;
        for (var site = 0; site < Family.Template.SiteCount; site++)
            if (StateForSite(site) == state) return site;
        return null;
    }

    public PulsePrediction Predict() => circuit.Predict();

    public PulseStep Pulse()
    {
        var step = circuit.Pulse();
        var isInput = step.Moved && step.Before == InputState && step.After == StateForSite(StartSite);
        var compatible = step.Moved && !isInput
            ? Enumerable.Range(0, RouteCount).Select(RouteRow)
                .Where(row => row[1] == step.Before && row[3] == step.After).Select(row => row[0]).ToArray()
            : [];
        trace.Add(new(trace.Count, Copy(step), FamilySite(step.Before), FamilySite(step.After), isInput, compatible));
        return step;
    }

    /// <summary>Copies preserve witness history even if a consumer edits a
    /// returned prediction or route list. Taking a trace does not pulse the circuit.</summary>
    public IReadOnlyList<FamilyWalkWitness> Trace => Array.AsReadOnly(trace.Select(witness => witness with
    {
        Step = Copy(witness.Step), CompatibleUses = witness.CompatibleUses.ToArray()
    }).ToArray());

    private static PulseStep Copy(PulseStep step) => step with
    {
        Prediction = step.Prediction with { Targets = step.Prediction.Targets.ToArray() }
    };
}
