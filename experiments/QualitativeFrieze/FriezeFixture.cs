using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authored motion networks with unmeasured shared reaches. Names are
/// presentation only; phases, routing, emission and sampling live in Surface.</summary>
public sealed class FriezeFixture
{
    // Surface = ((network -> motion-frame) -> (output-law -> sampling-routes)) ->
    //           (initial-phase-bindings -> reach-order-obligations)
    // sampling-route = network-surface-or-component-port -> sample-port
    // motion application = (phase-receipt -> output-read) -> motion
    // sample = ((cause -> sample-port) -> occurrence) ->
    //          ((before-displacement -> after-displacement) -> (ordered-applications -> (delta-read -> position-read)))
    // trace = (Surface -> initial-network-state) ->
    //         (final-network-state -> (root-steps -> (motion-applications -> (samples -> unresolved))))
    public string Name { get; }
    public QualitativeMotionFrame Frame { get; }
    public PhaseEventNetwork Network { get; }
    public Relation RootPort { get; }
    public Relation OutputLaw { get; }
    public Relation Sampling { get; }
    public Relation Starts { get; }
    public Relation ReachOrder { get; }
    public Relation Surface { get; }
    public IReadOnlyList<PhaseRecurrence> Components { get; }
    public IReadOnlyList<IElement> Reaches { get; }

    private FriezeFixture(string name, Builder builder, Relation root, Relation reachOrder,
        Relation? samplingSources = null)
    {
        Name = name; Frame = builder.Frame; RootPort = root; ReachOrder = reachOrder;
        Components = builder.Components.ToArray(); Reaches = builder.Reaches.ToArray();
        Network = new PhaseEventNetwork(builder.Components.ToArray(), S(builder.Ports.ToArray()), S(builder.Routes.ToArray()));
        Starts = S(builder.Starts.ToArray());
        OutputLaw = FiniteLaw.Create(S(builder.Phases.ToArray()), S(builder.Ports.ToArray()),
            S(builder.Words.ToArray()), builder.Outputs.ToArray());
        var samplePort = new Relation(Frame.Surface, new Placeholder());
        Sampling = S((samplingSources is null ? new IElement[] { Network.Surface } : E(samplingSources))
            .Select(source => (IElement)new Relation(source, samplePort)).ToArray());
        Surface = new(new Relation(new Relation(Network.Surface, Frame.Surface), new Relation(OutputLaw, Sampling)),
            new Relation(Starts, ReachOrder));
    }

    private FriezeFixture(FriezeFixture source, Relation outputLaw, Relation? sampling = null, string? displayName = null,
        Relation? starts = null)
    {
        Name = displayName ?? source.Name; Frame = source.Frame; Network = source.Network; RootPort = source.RootPort;
        Components = source.Components; Reaches = source.Reaches; Starts = starts ?? source.Starts;
        Sampling = sampling ?? source.Sampling; ReachOrder = source.ReachOrder; OutputLaw = outputLaw;
        Surface = new(new Relation(new Relation(Network.Surface, Frame.Surface), new Relation(OutputLaw, Sampling)),
            new Relation(Starts, ReachOrder));
    }

    /// <summary>An explicit alternative experiment, preserving the old surface
    /// and its laws. Missing emissions are unknown motion, never implicit pauses.</summary>
    public FriezeFixture WithOutputs(Relation outputLaw) => new(this, outputLaw);

    /// <summary>Selects explicit starting phase bindings while retaining the
    /// exact transition, emission and sampling surfaces. Incomplete bindings
    /// remain a matter for the native initial-state read.</summary>
    public FriezeFixture WithStarts(Relation starts, string? displayName = null)
    {
        if (!Closed(starts)) throw new ArgumentException("Starting bindings need a closed offered scope.", nameof(starts));
        return new(this, OutputLaw, displayName: displayName, starts: starts);
    }

    /// <summary>Changes the sampling connections while sharing the exact event
    /// network, motion law and reach identities. Runs still have fresh histories.</summary>
    public FriezeFixture WithSampling(Relation sources, string? displayName = null)
    {
        if (!Closed(sources)) throw new ArgumentException("Sampling needs an explicit finite source scope.", nameof(sources));
        var samplePort = E(Sampling).OfType<Relation>().FirstOrDefault()?.Right ?? new Relation(Frame.Surface, new Placeholder());
        return new(this, OutputLaw, S(E(sources).Select(source => (IElement)new Relation(source, samplePort)).ToArray()), displayName);
    }

    public static FriezeFixture SquareWave(bool coupled = true, QualitativeMotionFrame? frame = null,
        IElement? horizontalReach = null, IElement? verticalReach = null)
    {
        var b = new Builder(frame); var h = horizontalReach ?? new Placeholder(); var v = verticalReach ?? new Placeholder();
        var right = b.Move(b.Frame.Right, h); var up = b.Move(b.Frame.Up, v); var down = b.Move(b.Frame.Down, v);
        if (!coupled)
        {
            var clock = b.Cycle(S(up), S(right), S(down), S(right));
            return new("Square wave — direct sequence", b, clock.Port, S());
        }
        var beat = b.Cycle(S());
        var horizontal = b.Cycle(S(), S(right));
        var vertical = b.Cycle(S(up), S(down));
        b.Connect(beat, beat.Phases[0], horizontal);
        b.Connect(horizontal, horizontal.Phases[0], vertical);
        return new("Square wave — coupled clocks", b, beat.Port, S());
    }

    public static FriezeFixture Meander(QualitativeMotionFrame? frame = null)
    {
        var b = new Builder(frame); var h = new Placeholder(); var v = new Placeholder();
        var right = b.Move(b.Frame.Right, h); var left = b.Move(b.Frame.Left, h);
        var up = b.Move(b.Frame.Up, v); var down = b.Move(b.Frame.Down, v);
        var clock = b.Cycle(S(right), S(up), S(left), S(up), S(right), S(right), S(down), S(down), S(right));
        return new("Greek-key meander", b, clock.Port, S());
    }

    // root-phase -> horizontal-port; root-phase -> alternating-vertical-port.
    // Root sampling commits both emissions together. Sampling the two component
    // ports instead commits the same causal applications as separate strokes.
    // The slower version has a second root phase with only a horizontal route;
    // that explicit missing vertical trigger is neither an elapsed-time claim
    // nor a host modulo schedule.
    public static FriezeFixture CombinedDriveZigzag(bool slowerVertical = false,
        QualitativeMotionFrame? frame = null, IElement? horizontalReach = null,
        IElement? verticalReach = null)
    {
        var b = new Builder(frame); var h = horizontalReach ?? new Placeholder(); var v = verticalReach ?? new Placeholder();
        var right = b.Move(b.Frame.Right, h); var up = b.Move(b.Frame.Up, v); var down = b.Move(b.Frame.Down, v);
        var beat = slowerVertical ? b.Cycle(S(), S()) : b.Cycle(S());
        var horizontal = b.Cycle(S(right));
        var vertical = b.Cycle(S(up), S(down));
        b.Connect(beat, beat.Phases[0], horizontal);
        b.Connect(beat, beat.Phases[0], vertical);
        if (slowerVertical) b.Connect(beat, beat.Phases[1], horizontal);
        return new(slowerVertical ? "Combined drives — slower vertical recurrence" : "Combined drives — alternating vertical recurrence",
            b, beat.Port, S());
    }

    public static FriezeFixture SlowFastDrift(bool sampleIndividualFirings = false, QualitativeMotionFrame? frame = null)
    {
        var b = new Builder(frame); var fastReach = new Placeholder(); var driftReach = new Placeholder(); var v = new Placeholder();
        var beat = b.Cycle(S(), S(), S());
        var fast = b.Cycle(S(b.Move(b.Frame.Right, fastReach)), S(b.Move(b.Frame.Left, fastReach)));
        var slow = b.Cycle(S(b.Move(b.Frame.Up, v)), S(b.Move(b.Frame.Down, v)));
        var drift = b.Cycle(S(b.Move(b.Frame.Right, driftReach)));
        foreach (var phase in beat.Phases)
        {
            b.Connect(beat, phase, fast);
            if (ReferenceEquals(phase, beat.Phases[^1])) b.Connect(beat, phase, slow);
            b.Connect(beat, phase, drift);
        }
        return new(sampleIndividualFirings ? "Slow / fast / drift — every firing" : "Slow / fast / drift — sampled together",
            b, beat.Port, S(new Relation(driftReach, fastReach)),
            sampleIndividualFirings ? S(fast.Port, slow.Port, drift.Port) : null);
    }

    public static FriezeFixture PausesAndDrift(QualitativeMotionFrame? frame = null)
    {
        var b = new Builder(frame); var h = new Placeholder(); var v = new Placeholder();
        var beat = b.Cycle(S(), S(), S(), S()); // Three explicit pause phases, then the trigger phase.
        var vertical = b.Cycle(S(b.Move(b.Frame.Up, v)), S(b.Move(b.Frame.Down, v)));
        var drift = b.Cycle(S(b.Move(b.Frame.Right, h)));
        foreach (var phase in beat.Phases)
        {
            if (ReferenceEquals(phase, beat.Phases[^1])) b.Connect(beat, phase, vertical);
            b.Connect(beat, phase, drift);
        }
        return new("Three pauses, move; continuous drift", b, beat.Port, S());
    }

    public static FriezeFixture VariableSteps(QualitativeMotionFrame? frame = null)
    {
        var b = new Builder(frame); var shortReach = new Placeholder(); var longReach = new Placeholder(); var v = new Placeholder();
        var clock = b.Cycle(S(b.Move(b.Frame.Right, shortReach)), S(b.Move(b.Frame.Up, v)),
            S(b.Move(b.Frame.Right, longReach)), S(b.Move(b.Frame.Down, v)));
        return new("Alternating short and long reaches", b, clock.Port, S(new Relation(shortReach, longReach)));
    }

    /// <summary>Builds an authored cycle from native emission words. The ordered
    /// phase graph and phase-to-word law retain the program; names are display.
    /// Later output edits can share this graph through WithOutputs.</summary>
    public static FriezeFixture Sequence(QualitativeMotionFrame frame, Relation words,
        string name = "Candidate sequence", Relation? reachOrder = null)
    {
        if (!Closed(words) || E(words).Length is 0 or > 64 || E(words).Any(word => word is not Relation))
            throw new ArgumentException("A finite sequence needs one to 64 native emission words.", nameof(words));
        var b = new Builder(frame);
        foreach (var word in E(words).Cast<Relation>())
            if (StructuralConstraint.TryReadScope(word, out var moves, out _, out _))
                foreach (var move in moves.OfType<Relation>())
                    if (frame.Direction(move, out _, out _, out var reach) && !b.Reaches.Any(r => ReferenceEquals(r, reach)))
                        b.Reaches.Add(reach);
        var clock = b.Cycle(E(words).Cast<Relation>().ToArray());
        return new(name, b, clock.Port, reachOrder ?? S());
    }

    /// <summary>The count bounds the observation window, not a stored time or
    /// distance. Which component fires is entirely in the native route graph.</summary>
    public Relation Run(int rounds, int eventBudget = 64)
    {
        if (rounds < 0 || rounds > 2048) throw new ArgumentOutOfRangeException(nameof(rounds));
        var initial = Network.Initial(new Placeholder(), Starts); var state = initial;
        var steps = new List<IElement>(); var applications = new List<IElement>(); var samples = new List<IElement>();
        var pendingApplications = new List<IElement>(); var unresolved = new List<IElement>();
        var position = S();
        for (var round = 0; round < rounds; round++)
        {
            if (!Network.TryReadState(state, out var previous, out _, out _, out var queued) || E(queued).Length != 0)
            { unresolved.Add(state); break; }
            var receipt = Network.Step(state, Network.Pulse(RootPort, new Placeholder(), previous), eventBudget);
            steps.Add(receipt);
            if (!Network.TryReadStep(receipt, out _, out _, out state, out var accepted, out var blocked, out var unfinished))
            { unresolved.Add(receipt); break; }
            unresolved.AddRange(E(blocked)); unresolved.AddRange(E(unfinished));
            foreach (var item in E(accepted))
            {
                var phaseReceipt = (Relation)item;
                var component = Components.SingleOrDefault(clock => phaseReceipt.Left is Relation context && ReferenceEquals(context.Right, clock.Surface));
                if (component is null || !component.TryReadAccepted(phaseReceipt, out _, out var eventIdentity, out var phase))
                { unresolved.Add(phaseReceipt); continue; }
                var port = E(Network.Ports).OfType<Relation>().Single(p => ReferenceEquals(p.Right, component.Surface));
                var output = FiniteLaw.Evaluate(OutputLaw, S(phase), S(port));
                if (!FiniteLaw.TryReadResult(output, out var support, out var residue, out _) || !Closed(support) ||
                    E(support) is not [Relation word] || !Closed(word) || E(residue).Length != 0)
                {
                    unresolved.Add(output);
                    var unknown = new Relation(new Relation(phaseReceipt, output), new Placeholder());
                    applications.Add(unknown); pendingApplications.Add(unknown);
                    continue;
                }
                foreach (var move in E(word))
                {
                    var application = new Relation(new Relation(phaseReceipt, output), move);
                    applications.Add(application); pendingApplications.Add(application);
                }
                foreach (var route in E(Sampling).OfType<Relation>().Where(route => ReferenceEquals(route.Left, port)))
                    Commit(phaseReceipt, eventIdentity, route.Right);
            }
            if (E(blocked).Length == 0 && E(unfinished).Length == 0)
            {
                Network.TryReadState(state, out var eventIdentity, out _, out _, out _);
                foreach (var route in E(Sampling).OfType<Relation>().Where(route => ReferenceEquals(route.Left, Network.Surface)))
                    Commit(receipt, eventIdentity, route.Right);
            }
            else break;
        }
        if (pendingApplications.Count != 0) unresolved.Add(new Relation(S(pendingApplications.ToArray()), Sampling));
        return new(new Relation(Surface, initial), new Relation(state, new Relation(S(steps.ToArray()),
            new Relation(S(applications.ToArray()), new Relation(S(samples.ToArray()), S(unresolved.ToArray()))))));

        void Commit(IElement cause, IElement eventIdentity, IElement samplePort)
        {
            var incoming = S(pendingApplications.Cast<Relation>().Select(app => app.Right).ToArray());
            var delta = Frame.Reduce(S(), incoming);
            var reduction = Frame.Reduce(position, incoming);
            if (!Frame.TryReadResult(reduction, out var after, out _, out var pending) ||
                !Frame.TryReadResult(delta, out _, out _, out var deltaPending))
            { unresolved.Add(reduction); return; }
            var sample = new Relation(new Relation(new Relation(cause, samplePort), new Relation(eventIdentity, samplePort)),
                new Relation(new Relation(position, after), new Relation(S(pendingApplications.ToArray()), new Relation(delta, reduction))));
            samples.Add(sample); position = after; pendingApplications.Clear();
            // Unknown net comparisons do not stop a known finite expression or
            // its execution schedule. They remain attached to the sampled read.
            if (E(pending).Length != 0) unresolved.Add(new Relation(sample, pending));
            if (E(deltaPending).Length != 0) unresolved.Add(new Relation(sample, deltaPending));
        }
    }

    public bool TryReadTrace(Relation trace, out Relation initial, out Relation final, out Relation steps,
        out Relation applications, out Relation samples, out Relation unresolved)
    {
        initial = final = steps = applications = samples = unresolved = null!;
        if (trace.Left is not Relation { Right: Relation start } context || !ReferenceEquals(context.Left, Surface) ||
            trace.Right is not Relation { Left: Relation end, Right: Relation body } ||
            body.Left is not Relation events || body.Right is not Relation { Left: Relation moves, Right: Relation rest } ||
            rest.Left is not Relation commits || rest.Right is not Relation pending ||
            !Closed(events) || !Closed(moves) || !Closed(commits) || !Closed(pending)) return false;
        initial = start; final = end; steps = events; applications = moves; samples = commits; unresolved = pending; return true;
    }

    public static bool TryReadSample(Relation sample, out IElement cause, out IElement occurrence,
        out Relation before, out Relation after, out Relation applications, out Relation reduction)
    {
        cause = occurrence = null!; before = after = applications = reduction = null!;
        if (sample.Left is not Relation { Left: Relation origin, Right: Relation eventIdentity } ||
            !ReferenceEquals(eventIdentity.Right, origin.Right) ||
            sample.Right is not Relation { Left: Relation positions, Right: Relation proof } ||
            positions.Left is not Relation first || positions.Right is not Relation last ||
            proof.Left is not Relation moves || proof.Right is not Relation { Left: Relation, Right: Relation read } ||
            !StructuralConstraint.TryReadScope(first, out _, out _, out _) ||
            !StructuralConstraint.TryReadScope(last, out _, out _, out _) || !Closed(moves)) return false;
        cause = origin.Left; occurrence = eventIdentity; before = first; after = last; applications = moves; reduction = read; return true;
    }

    public static bool TryReadSampleDelta(Relation sample, out Relation delta)
    {
        delta = null!;
        if (!TryReadSample(sample, out _, out _, out _, out _, out _, out _) ||
            sample.Right is not Relation { Right: Relation { Right: Relation { Left: Relation read } } }) return false;
        delta = read; return true;
    }

    public bool SamePhaseConfiguration(Relation first, Relation second)
    {
        if (!Network.TryReadState(first, out _, out var a, out _, out var aq) ||
            !Network.TryReadState(second, out _, out var b, out _, out var bq) || E(aq).Length != 0 || E(bq).Length != 0) return false;
        var left = E(a).Cast<Relation>().ToArray(); var right = E(b).Cast<Relation>().ToArray();
        return left.Length == right.Length && left.All(row => right.Any(other => ReferenceEquals(row.Left, other.Left) &&
            ReadPhase(row, out var x) && ReadPhase(other, out var y) && Closed(x) && Closed(y) &&
            E(x) is [var firstPhase] && E(y) is [var secondPhase] && ReferenceEquals(firstPhase, secondPhase)));
    }

    private bool ReadPhase(Relation binding, out Relation support)
    {
        support = null!;
        if (binding.Left is not Relation port || binding.Right is not Relation state) return false;
        var clock = Components.SingleOrDefault(c => ReferenceEquals(c.Surface, port.Right));
        return clock is not null && clock.TryReadState(state, out _, out _, out support);
    }

    private sealed record Component(PhaseRecurrence Clock, Relation Port, IElement[] Phases);
    private sealed class Builder(QualitativeMotionFrame? frame)
    {
        public QualitativeMotionFrame Frame { get; } = frame ?? new();
        public List<PhaseRecurrence> Components { get; } = [];
        public List<IElement> Ports { get; } = [];
        public List<IElement> Phases { get; } = [];
        public List<IElement> Words { get; } = [];
        public List<Relation> Outputs { get; } = [];
        public List<IElement> Starts { get; } = [];
        public List<IElement> Routes { get; } = [];
        public List<IElement> Reaches { get; } = [];

        public Relation Move(IElement direction, IElement reach)
        {
            if (!Reaches.Any(r => ReferenceEquals(r, reach))) Reaches.Add(reach);
            return QualitativeMotionFrame.Move(direction, reach);
        }
        public Component Cycle(params Relation[] words)
        {
            IElement[] phases = words.Select(_ => (IElement)new Placeholder()).ToArray();
            var edges = phases.Zip(phases.Skip(1).Append(phases[0])).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray();
            var clock = new PhaseRecurrence(S(phases), S(edges)); var port = PhaseEventNetwork.Port(new Placeholder(), clock);
            Components.Add(clock); Ports.Add(port); Phases.AddRange(phases); Starts.Add(new Relation(port, S(phases[^1])));
            for (var i = 0; i < words.Length; i++)
            {
                if (!Words.Any(word => ReferenceEquals(word, words[i]))) Words.Add(words[i]);
                Outputs.Add(FiniteLaw.Row(phases[i], port, S(words[i])));
            }
            return new(clock, port, phases);
        }
        public void Connect(Component source, IElement phase, Component target) =>
            Routes.Add(PhaseEventNetwork.Route(source.Port, phase, target.Port));
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) =>
        StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
