namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>A bounded, single-threaded clocked composition of existing passages.
/// One occupied state site is an activation, not an amount of drawing material.
/// Each pulse attempts at most one outgoing row from the initial active site.
/// Table laws and packed references own the behavior; there are no action tags.
/// Bound guards import observations through public ports, never private stores.</summary>
public sealed class PulseCircuit
{
    private readonly PortStructure owner = new();
    private readonly List<int> states = [];
    private readonly Dictionary<int, int> guards = [];
    private readonly Dictionary<int, IPrimitiveReadPort> inputs = [];
    private readonly List<(PassageLaw Law, PassageConnections Table, List<ConnectionRow> Rows)> tables = [];
    public int ReferenceBits { get; }

    public PulseCircuit(int referenceBits = 6)
    {
        if (referenceBits is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(referenceBits));
        ReferenceBits = referenceBits;
    }

    public int State(bool active = false)
    {
        if (active && Active is not null) throw new InvalidOperationException("Only one activation is allowed.");
        var port = Create(active);
        states.Add(port);
        return port;
    }

    public int Guard(bool present = false)
    {
        var port = Create(present);
        guards.Add(port, port); // One published binding per local site in this fixture.
        return port;
    }

    public void Bind(int guard, IPrimitiveReadPort input)
    {
        if (!guards.ContainsKey(guard)) throw new ArgumentOutOfRangeException(nameof(guard));
        ArgumentNullException.ThrowIfNull(input);
        inputs[guard] = input;
    }

    public IPrimitiveReadPort Expose(int state)
    {
        if (!states.Contains(state)) throw new ArgumentOutOfRangeException(nameof(state));
        return owner.Connect(state);
    }

    public int AddTable(PassageLaw law)
    {
        ArgumentNullException.ThrowIfNull(law);
        tables.Add((law, new(owner, law), []));
        return tables.Count - 1;
    }

    public ConnectionRow Link(int table, int from, int guard, int to)
    {
        if (!states.Contains(from) || !states.Contains(to) || from == to || !guards.ContainsKey(guard))
            throw new ArgumentException("Use distinct state sites and a separate guard site.");
        var entry = tables[table];
        var row = entry.Table.Connect(ReferenceBits, from, guard, to);
        entry.Rows.Add(row);
        return row;
    }

    public int? Active
    {
        get
        {
            var occupied = states.Where(s => (owner.ReadPort(s) & 1) != 0).ToArray();
            return occupied.Length == 1 ? occupied[0] : null;
        }
    }

    public byte[] ValueBytes() => owner.PackedBytes(0, 0, 1);
    public byte[] PackedRows(int table) => tables[table].Table.PackedRows(ReferenceBits);
    public byte[] LawBytes(int table) => tables[table].Law.PackedBytes();
    public int[] Wiring(int table, ConnectionRow row) => tables[table].Table.Wiring(row);

    public PulsePrediction Predict() => Read().Prediction;

    public PulseStep Pulse()
    {
        var read = Read();
        var prediction = read.Prediction;
        if (!prediction.Resolved || prediction.Conflict || prediction.Missing || prediction.Before is null)
            return new(false, false, prediction.Before, prediction.Before, prediction);
        // Only resolved public observations are imported. An uncertain pulse
        // changes neither guard mirrors nor activation sites.
        foreach (var (guard, support) in read.Samples)
            owner.StoreObservation(guards[guard], false, false, support == 2 ? 1UL : 0UL);
        if (read.Selected is null) return new(true, false, prediction.Before, prediction.Before, prediction);
        var (table, row) = read.Selected.Value;
        var ports = tables[table].Table.Wiring(row);
        var control = read.Samples[ports[1]] == 2;
        var before = (byte)(1 << PassageLaw.State(true, control, false));
        var committed = tables[table].Table.TryStep(row, before, out _);
        return new(committed, committed, prediction.Before, Active, prediction);
    }

    private (PulsePrediction Prediction, (int Table, ConnectionRow Row)? Selected,
        Dictionary<int, byte> Samples) Read()
    {
        var current = Active;
        var samples = new Dictionary<int, byte>();
        if (current is null) return (new(null, [], false, false, true), null, samples);
        var outgoing = tables.SelectMany((t, i) => t.Rows.Select(row =>
            (Table: i, Row: row, Ports: t.Table.Wiring(row))))
            .Where(r => r.Ports[0] == current).ToArray();
        foreach (var guard in outgoing.Select(r => r.Ports[1]).Distinct())
        {
            // If every outgoing use has the same material result for either
            // C, the bound observation is irrelevant. Preserve the local C
            // bit without consulting that source; uncertainty there cannot
            // veto an unconditional continuation after a branch was chosen.
            var needed = outgoing.Where(r => r.Ports[1] == guard).Any(r =>
            {
                var law = tables[r.Table].Law;
                return law.Predict(1 << 1) != (law.Predict(1 << 3) >> 2);
            });
            var support = needed && inputs.TryGetValue(guard, out var input)
                ? input.ReadSupport() : ReadSupport.Of(owner.ReadPort(guard));
            if ((support & ~3) != 0) throw new InvalidOperationException("Guards require positive-context V observations.");
            samples.Add(guard, support);
        }
        if (samples.Count > 8) throw new InvalidOperationException("This local predictor admits at most eight distinct outgoing guards.");
        var missing = samples.Values.Any(s => s == 0);
        var resolved = samples.Values.All(s => s is 1 or 2);
        var conflict = false;
        var targets = new HashSet<int>();
        (int, ConnectionRow)? selected = null;
        var guardRefs = samples.Keys.ToArray();
        for (var candidate = 0; candidate < 1 << guardRefs.Length && !missing; candidate++)
        {
            if (guardRefs.Where((g, i) => (samples[g] & (1 << ((candidate >> i) & 1))) == 0).Any()) continue;
            var enabled = new List<(int Table, ConnectionRow Row, int Target)>();
            foreach (var row in outgoing)
            {
                var control = ((candidate >> Array.IndexOf(guardRefs, row.Ports[1])) & 1) != 0;
                var before = PassageLaw.State(true, control, false);
                var afterSupport = tables[row.Table].Law.Predict((byte)(1 << before));
                if (!ReadSupport.TrySingle(afterSupport, out var after)) { missing = true; break; }
                if ((after & 4) != 0) enabled.Add((row.Table, row.Row, row.Ports[2]));
            }
            if (missing) break;
            if (enabled.Count > 1) { conflict = true; continue; }
            targets.Add(enabled.Count == 0 ? current.Value : enabled[0].Target);
            if (resolved && enabled.Count == 1) selected = (enabled[0].Table, enabled[0].Row);
        }
        return (new(current, targets.Order().ToArray(), resolved, conflict, missing), selected, samples);
    }

    private int Create(bool present)
    {
        if (states.Count + guards.Count >= 1 << ReferenceBits)
            throw new InvalidOperationException("Local references do not fit the selected width.");
        return owner.Publish(owner.Create(0, 0, 1, present ? 1UL : 0UL));
    }
}

// Derived observations, not persistent fields on the encoded sites.
public sealed record PulsePrediction(int? Before, int[] Targets, bool Resolved, bool Conflict, bool Missing);
public sealed record PulseStep(bool Committed, bool Moved, int? Before, int? After, PulsePrediction Prediction);
