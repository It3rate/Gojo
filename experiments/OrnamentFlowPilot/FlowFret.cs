using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal readonly record struct FretFrame(PathSite Origin, PathSite Across, PathSite Rise)
{
    public static FretFrame Cartesian => new(default, new(1, 0), new(0, 1));

    internal void Check()
    {
        static bool Cardinal(PathSite p) => p is { X: 1 or -1, Y: 0 } or { X: 0, Y: 1 or -1 };
        if (!Cardinal(Across) || !Cardinal(Rise) || Across.X * Rise.X + Across.Y * Rise.Y != 0)
            throw new ArgumentException("The fret needs two distinct signed cardinal axes.");
    }

    internal int Height(PathSite point) => checked((int)(
        ((long)point.X - Origin.X) * Rise.X + ((long)point.Y - Origin.Y) * Rise.Y));
}

internal sealed record FretRead(int Guard, byte Support);
internal sealed record FretBeat(int Beat, int Bay, int Before, int After, PathSite Start, PathSite End,
    bool Emitted, bool Reflected, FretRead[] Reads);
internal sealed record FretLaw(string Law, string Rows);
internal sealed record FretBinding(int Guard, string Source);
internal sealed record FretMirrorWiring(int ReferenceBits, int SeamSource, int Guard, int ForwardState, int ReflectedState,
    FretLaw Transitions, byte AxisMap);
internal sealed record FretWiring(int ReferenceBits, int Seam, int Fork, FretLaw[] Passages, string OutletRows,
    int OutletCount, FretBinding[] Inputs, FretMirrorWiring? Alternation);
internal sealed record FretRun(PathTrace Trace, PathSite[] BayEnds, bool Completed, string Stop,
    int BaselineOffset, FretBeat[] Beats, FretWiring Wiring);

/// <summary>A single activation circulates through an ordered passage ring. Pen outlets
/// read only active sites. The feedback detour draws one tick and returns to its height
/// reader until the origin line is sensed. A pulse is an activation, never conserved ink.</summary>
internal static class FlowFret
{
    private sealed class SamplePort(Func<byte> sample) : IPrimitiveReadPort
    {
        internal byte? Last { get; set; }
        public byte ReadSupport()
        {
            var support = sample();
            Last = support;
            return support;
        }
    }

    internal static FretRun Run(int a, int b, bool followBaseline, int bays = 2, int pulseBudget = 4096,
        Action<int, IPrimitiveReadPort>? beforeBeat = null, FretFrame? frame = null, IPrimitiveReadPort? choice = null,
        bool alternateAtSeam = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(a);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(b);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bays);
        ArgumentOutOfRangeException.ThrowIfNegative(pulseBudget);
        var basis = frame ?? FretFrame.Cartesian;
        basis.Check();
        var pen = new FlowPen(basis.Origin);
        var circuit = new PulseCircuit(referenceBits: 5);
        var pass = circuit.AddTable(PassageLaw.Unconditional());
        var yes = circuit.AddTable(PassageLaw.Controlled());
        var no = circuit.AddTable(PassageLaw.Controlled(false));
        int seam = circuit.State(true), always = circuit.Guard(true);
        PulseCircuit? mirror = null;
        IPrimitiveReadPort? mirrorPhase = null;
        FretMirrorWiring? mirrorWiring = null;
        if (alternateAtSeam)
        {
            mirror = new(referenceBits: 2);
            int forward = mirror.State(true), reflected = mirror.State(), boundary = mirror.Guard();
            mirror.Bind(boundary, circuit.Expose(seam));
            int toggle = mirror.AddTable(PassageLaw.Controlled());
            mirror.Link(toggle, forward, boundary, reflected);
            mirror.Link(toggle, reflected, boundary, forward);
            mirrorPhase = mirror.Expose(reflected);
            // Four packed two-bit lanes: across+, rise-, across-, rise+.
            mirrorWiring = new(mirror.ReferenceBits, seam, boundary, forward, reflected,
                new(Convert.ToHexString(mirror.LawBytes(toggle)), Convert.ToHexString(mirror.PackedRows(toggle))), 0x6C);
        }
        var outlets = new EndpointRows();
        var motionPorts = new Dictionary<int, IPrimitiveReadPort>();
        var inputs = new List<(FretBinding Binding, SamplePort Port)>();
        PathSite[] units = [basis.Across, basis.Rise, new(-basis.Across.X, -basis.Across.Y), new(-basis.Rise.X, -basis.Rise.Y)];
        int[] extents = [a, b, 1];

        // A packed pen outlet is [participation port, directed axis port, extent port].
        int Stroke(int axis, int extent)
        {
            int state = circuit.State();
            outlets.Append(circuit.ReferenceBits, state, axis, extent);
            motionPorts.Add(state, circuit.Expose(state));
            return state;
        }
        int Sensor(string name, Func<byte> sample)
        {
            int guard = circuit.Guard();
            var port = new SamplePort(sample);
            circuit.Bind(guard, port);
            inputs.Add((new(guard, name), port));
            return guard;
        }
        void Link(int from, int to) => circuit.Link(pass, from, always, to);

        int rise = Stroke(1, 0), roof = Stroke(0, 0), drop = Stroke(3, 1), inset = Stroke(2, 1);
        int fork = circuit.State(), exit = Stroke(0, 0);
        Link(seam, rise);
        Link(rise, roof);
        Link(roof, drop);
        Link(drop, inset);
        Link(inset, fork);
        Link(exit, seam);

        // The two fixed machines have genuinely different surfaces. The optional trial
        // supplies both outlets and a public choice guard sampled only at this fork.
        int selected = choice is null ? -1 : Sensor("trial selection", choice.ReadSupport);
        if (!followBaseline || choice is not null)
        {
            int secondDrop = Stroke(3, 1);
            if (choice is null) Link(fork, secondDrop);
            else circuit.Link(no, fork, selected, secondDrop);
            Link(secondDrop, exit);
        }
        if (followBaseline || choice is not null)
        {
            int heightRead = circuit.State(), downTick = Stroke(3, 2);
            var abovePort = pen.ReadHeight(basis, above: true);
            var atPort = pen.ReadHeight(basis, above: false);
            if (mirrorPhase is not null)
            {
                // On the reflected face, positive height is below in the original frame.
                // XOR selects that face, then the zero read excludes the baseline itself.
                var signedSide = PresencePairPort.Bind(15, abovePort, mirrorPhase).Test(0b0110);
                abovePort = PresencePairPort.Bind(15, signedSide, atPort).Test(0b0010);
            }
            int above = Sensor("above active baseline", abovePort.ReadSupport);
            int at = Sensor("at baseline", atPort.ReadSupport);
            if (choice is null) Link(fork, heightRead);
            else circuit.Link(yes, fork, selected, heightRead);
            circuit.Link(yes, heightRead, above, downTick);
            circuit.Link(yes, heightRead, at, exit);
            Link(downTick, heightRead);
        }

        var tables = new[] { pass, yes, no }.Select(table =>
            new FretLaw(Convert.ToHexString(circuit.LawBytes(table)), Convert.ToHexString(circuit.PackedRows(table)))).ToArray();
        int outletCount = outlets.Count(3, circuit.ReferenceBits);
        var wiring = new FretWiring(circuit.ReferenceBits, seam, fork, tables,
            Convert.ToHexString(outlets.PackedBytes(3, circuit.ReferenceBits)), outletCount,
            inputs.Select(input => input.Binding).ToArray(), mirrorWiring);
        var sites = new List<PathSite> { basis.Origin };
        var edges = new List<PathEdge>();
        var events = new List<PathEvent>();
        var ends = new List<PathSite>();
        var beats = new List<FretBeat>();
        var rest = new ParticipationStrip(0).Expose(0);
        string stop = "pulse budget";
        for (int beat = 0; beat < pulseBudget; beat++)
        {
            int bay = ends.Count;
            var start = pen.Cursor;
            beforeBeat?.Invoke(beat, pen.MarkPort);
            foreach (var input in inputs) input.Port.Last = null;
            bool reflected = mirrorPhase?.ReadSupport() == 2;
            var pulse = circuit.Pulse();
            var reads = inputs.Where(input => input.Port.Last.HasValue)
                .Select(input => new FretRead(input.Binding.Guard, input.Port.Last!.Value)).ToArray();
            bool emitted = false;
            // Only a moved activation can deposit. A stalled circuit never redraws
            // its last stroke. Select an outlet by its actual published P first.
            for (int row = 0; pulse.Moved && row < outletCount; row++)
            {
                int[] wire = outlets.Read(new(3, circuit.ReferenceBits, row));
                var participation = motionPorts[wire[0]];
                if (participation.ReadSupport() != 2) continue;
                int lane = reflected ? (mirrorWiring!.AxisMap >> (2 * wire[1])) & 3 : wire[1];
                var unit = units[lane];
                var contact = pen.Touch(participation, unit, () => extents[wire[2]]);
                if (!contact.DrewStroke) throw new InvalidOperationException("An active positive reach must deposit.");
                int from = sites.Count - 1;
                sites.Add(contact.To);
                int direction = unit.X > 0 ? 0 : unit.X < 0 ? 4 : unit.Y > 0 ? 2 : 6;
                // -1 denotes the new unit-tick port outside the earlier A/B reach grammar.
                edges.Add(new(from, from + 1, 0, beat, bay, row, direction, wire[2] == 2 ? -1 : wire[2]));
                events.Add(new(0, beat, bay, row, "draw", from, from + 1, 2));
                emitted = true;
                break;
            }
            if (!emitted) pen.Touch(rest, default);
            beats.Add(new(beat, bay, pulse.Before!.Value, pulse.After!.Value, start, pen.Cursor, emitted, reflected, reads));
            if (!pulse.Moved)
            {
                stop = pulse.Prediction.Missing ? "missing guard" :
                    pulse.Prediction.Conflict ? "competing outlets" :
                    !pulse.Prediction.Resolved ? "unresolved guard" : "no admitted outlet";
                break;
            }
            // The seam port is sampled after the primary pulse. Its arrival can
            // turn the next bay, never the stroke that produced this arrival.
            mirror?.Pulse();
            if (pulse.After == seam)
            {
                ends.Add(pen.Cursor);
                if (ends.Count == bays) { stop = "complete"; break; }
                int sharedSite = sites.Count - 1;
                events.Add(new(0, beat, bay + 1, -1, "continue", sharedSite, sharedSite, 2));
            }
        }
        int pitch = checked((int)(2L * a - b));
        return new(new(sites.ToArray(), edges.ToArray(), events.ToArray(), pitch, bays, 1),
            ends.ToArray(), ends.Count == bays, stop, basis.Height(pen.Cursor), beats.ToArray(), wiring);
    }
}
