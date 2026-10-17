using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// A local impulse reservoir, threshold question and cyclic muscle controller.
/// Phase identities select reusable actions, never preconstructed output sites.
/// Packets are discrete uncalibrated participation, not Carrier.Tension.
/// </summary>
public sealed class PatternMuscle
{
    public string Name { get; }
    public QualitativeImpulseAccumulator Accumulator { get; }
    public PhaseRecurrence Controller { get; }
    public Relation ControllerState { get; private set; }
    public Relation CommandLaw { get; }
    public Relation OutputPort { get; }
    public Relation ActionBundles { get; }
    public Relation InitialState { get; }
    public Relation Surface { get; }
    public Relation Hold { get; } = StructuralConstraint.Closed();
    public IElement Source { get; } = new Placeholder();
    public IElement RefinementPort { get; } = new Placeholder();

    public PatternMuscle(string name, Relation sites, params Relation[] actionBundles)
    {
        if (actionBundles.Length == 0 || actionBundles.Any(bundle => !Closed(bundle)))
            throw new ArgumentException("A muscle requires complete, ordered action bundles.");
        Name = name;
        Accumulator = new(Source, sites, sites);
        var phases = actionBundles.Select(_ => (IElement)new Placeholder()).ToArray();
        var domain = S(phases);
        Controller = new(domain, S(phases.Select((phase, index) => (IElement)new Relation(phase, phases[(index + 1) % phases.Length])).ToArray()));
        ControllerState = Controller.Initial(Source, new Placeholder(), S(phases[0]));
        var outcomes = new List<IElement> { Hold };
        foreach (var bundle in actionBundles)
            if (!outcomes.Any(outcome => ReferenceEquals(outcome, bundle))) outcomes.Add(bundle);
        var outputs = S(outcomes.ToArray());
        var rows = phases.SelectMany((phase, index) => new[] {
            FiniteLaw.Row(Accumulator.Ready, phase, S(actionBundles[index])),
            FiniteLaw.Row(Accumulator.Waiting, phase, S(Hold)) }).ToArray();
        CommandLaw = FiniteLaw.Create(Accumulator.ReadinessDomain, domain, outputs, rows);
        OutputPort = new Relation(Accumulator.Surface, new Placeholder());
        ActionBundles = S(actionBundles);
        InitialState = new Relation(Accumulator.State, ControllerState);
        Surface = new Relation(new Relation(Accumulator.Surface, Controller.Surface),
            new Relation(CommandLaw, new Relation(new Relation(OutputPort, ActionBundles), InitialState)));
    }

    public Relation Tick(Relation packets, IElement drive)
    {
        var receive = Accumulator.Receive(new Placeholder(), packets);
        var publication = Accumulator.Observe();
        if (!QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var ready, out _) ||
            !Controller.TryReadState(ControllerState, out _, out _, out var phase))
            throw new InvalidOperationException("A local machine state lost its contract.");
        var read = FiniteLaw.Evaluate(CommandLaw, ready, phase);
        FiniteLaw.TryRead(CommandLaw, out _, out _, out var domain, out _);
        var routes = S(E(domain).Select(outcome => (IElement)new Relation(outcome, outcome)).ToArray());
        var question = QualitativeQuestion.Ask(FiniteLaw.WithProvenance(read, new Relation(publication, ControllerState)), routes, RefinementPort);
        // The readiness question and command question remain separate from the
        // material release. Waiting is a complete answer, not an unknown action.
        var selected = QualitativeQuestion.TryReadAnswer(question, out var selectedAnswer) && selectedAnswer is Relation selectedBundle && Closed(selectedBundle);
        // Route the small selected command. The complete question stays in this
        // tick's proof; using its causal DAG as a matcher operand needlessly
        // revisits shared histories on every firing.
        var permissionRoutes = selected
            ? S(new Relation(Accumulator.Ready, selectedAnswer), new Relation(Accumulator.Waiting, Hold))
            : S(new Relation(Accumulator.Waiting, Hold));
        var permission = Accumulator.Ask(permissionRoutes, RefinementPort);
        var release = Accumulator.Fire(new Placeholder(), permission);
        if (!Accumulator.TryReadReceipt(release, out _, out _, out _, out _, out _, out _, out var drained, out var pending))
            throw new InvalidOperationException("A reservoir release lost its accounting receipt.");
        var actions = S(); var phaseSteps = S();
        if (E(drained).Length != 0 && E(pending).Length == 0 && QualitativeQuestion.TryReadAnswer(question, out var answer) && answer is Relation bundle && Closed(bundle))
        {
            Controller.TryReadState(ControllerState, out _, out var previous, out _);
            var step = Controller.Step(ControllerState, Controller.Drive(Source, new Placeholder(), previous, release));
            if (!Controller.TryReadAccepted(step, out _, out _, out _) ||
                !Controller.TryReadStep(step, out _, out _, out var after, out _, out _, out _))
                throw new InvalidOperationException("An admitted impulse could not advance its controller.");
            ControllerState = after; actions = bundle; phaseSteps = S(step);
        }
        return new Relation(new Relation(Surface, drive), new Relation(
            new Relation(receive, new Relation(publication, new Relation(question, permission))),
            new Relation(release, new Relation(actions, new Relation(drained, phaseSteps)))));
    }

    public bool TryReadTick(Relation tick, out Relation actions, out Relation drained,
        out Relation question, out Relation release)
    {
        actions = drained = question = release = null!;
        if (tick.Left is not Relation origin || !ReferenceEquals(origin.Left, Surface) ||
            tick.Right is not Relation { Left: Relation { Right: Relation { Right: Relation queries } }, Right: Relation result } ||
            queries.Left is not Relation q || result.Left is not Relation r ||
            result.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation d } } || !Closed(a) || !Closed(d)) return false;
        actions = a; drained = d; question = q; release = r; return true;
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}

/// <summary>
/// An explicitly scheduled network. Each reservoir output has at most one
/// material destination; observing its firing may also produce a drawing action.
/// Simultaneous draw contributions form one chord. Control actions follow that
/// sample in the declared module order. No drive number selects a stroke.
/// </summary>
public sealed class PatternMachine
{
    public string Name { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public IReadOnlyList<PatternMuscle> Muscles { get; }
    public Relation ExternalPorts { get; }
    public Relation Connections { get; }
    public Relation ExecutionOrder { get; }
    public Relation ActionSchedule { get; }
    public Relation Surface { get; }
    public Relation Timeline { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    public Relation ImportedPackets { get; private set; } = S();
    public Relation RetainedPackets { get; private set; } = S();
    public Relation ReleasedPackets { get; private set; } = S();
    public Relation CurrentStates => S(Muscles.Select(muscle => (IElement)new Relation(muscle.Surface,
        new Relation(muscle.Accumulator.State, muscle.ControllerState))).ToArray());
    private readonly List<IElement> history = [];

    public PatternMachine(string name, QualitativeDrawingReceiver drawing, PatternMuscle[] muscles,
        Relation externalPorts, Relation connections)
    {
        Name = name; Drawing = drawing; Muscles = Array.AsReadOnly(muscles);
        ExternalPorts = externalPorts; Connections = connections;
        if (!Closed(externalPorts) || !Closed(connections) || muscles.Distinct().Count() != muscles.Length ||
            E(externalPorts).Distinct(ReferenceEqualityComparer.Instance).Count() != E(externalPorts).Length)
            throw new ArgumentException("A machine needs complete input and connection contracts.");
        var destinations = new HashSet<PatternMuscle>(); var senders = new HashSet<PatternMuscle>();
        foreach (var row in E(connections).Cast<Relation>())
        {
            var from = Array.FindIndex(muscles, muscle => ReferenceEquals(muscle.OutputPort, row.Left));
            var to = Array.FindIndex(muscles, muscle => ReferenceEquals(muscle.Accumulator.InputPort, row.Right));
            if (from < 0 || to <= from || !senders.Add(muscles[from]) || !destinations.Add(muscles[to]))
                throw new ArgumentException("Material routes must have one destination and follow the explicit acyclic schedule.");
        }
        foreach (var port in E(externalPorts))
            if (!muscles.Any(muscle => ReferenceEquals(muscle.Accumulator.InputPort, port)))
                throw new ArgumentException("A drive port must belong to a local reservoir.");
        foreach (var bundle in muscles.SelectMany(muscle => E(muscle.ActionBundles)).Cast<Relation>())
            if (E(bundle).Any(action => drawing.TryReadDraw(action, out _)) && E(bundle).Any(action => !drawing.TryReadDraw(action, out _)))
                throw new ArgumentException("Parallel draw bundles and ordered post-sample control bundles must be separate muscles.");
        ExecutionOrder = S(muscles.Select(muscle => (IElement)muscle.Surface).ToArray());
        var actions = muscles.SelectMany(muscle => E(muscle.ActionBundles)).Cast<Relation>().SelectMany(E).ToArray();
        ActionSchedule = new Relation(new Relation(S(actions.Where(action => drawing.TryReadDraw(action, out _)).ToArray()), drawing.StrokePort),
            new Relation(S(actions.Where(action => !drawing.TryReadDraw(action, out _)).ToArray()), drawing.Surface));
        Surface = new Relation(drawing.Surface, new Relation(ExecutionOrder,
            new Relation(new Relation(externalPorts, connections), ActionSchedule)));
    }

    /// <summary>Fixture supplier: one fresh actual packet on each declared input
    /// per explicit drive. It promises occurrence order, not measured duration.</summary>
    public Relation Pulse() => Drive(new Placeholder(), S(E(ExternalPorts)
        .Select(port => (IElement)new Relation(port, S(new Placeholder()))).ToArray()));

    public Relation Drive(IElement occurrence, Relation offers)
    {
        if (history.Any(prior => Same(prior, occurrence)) || !Closed(offers) ||
            E(offers).Any(row => row is not Relation offer || !E(ExternalPorts).Any(port => ReferenceEquals(port, offer.Left)) ||
                offer.Right is not Relation support || !Closed(support)) ||
            E(offers).Cast<Relation>().GroupBy(row => row.Left, ReferenceEqualityComparer.Instance).Any(group => group.Count() != 1) ||
            !FreshImports(offers))
        {
            var blocked = new Relation(new Relation(Surface, occurrence), offers);
            Unresolved = S([.. E(Unresolved), blocked]); return blocked;
        }
        var inputs = Muscles.ToDictionary(muscle => muscle, _ => new List<IElement>());
        foreach (var offer in E(offers).Cast<Relation>())
            inputs[Muscles.Single(muscle => ReferenceEquals(muscle.Accumulator.InputPort, offer.Left))].AddRange(E((Relation)offer.Right));
        ImportedPackets = S([.. E(ImportedPackets), .. E(offers).Cast<Relation>().SelectMany(offer => E((Relation)offer.Right))]);
        var ticks = new List<IElement>(); var transfers = new List<IElement>();
        var commands = new List<IElement>(); var contributions = new List<IElement>(); var releases = new List<IElement>();
        foreach (var muscle in Muscles)
        {
            var tick = muscle.Tick(S(inputs[muscle].ToArray()), occurrence); ticks.Add(tick);
            if (!muscle.TryReadTick(tick, out var actions, out var drained, out _, out _))
                throw new InvalidOperationException("A muscle tick lost its native trace.");
            if (tick.Right is not Relation { Left: Relation { Left: Relation receive } })
                throw new InvalidOperationException("A muscle tick lost its input receipt.");
            if (!muscle.Accumulator.TryReadReceipt(receive, out _, out _, out _, out _, out var blockedInput, out var pendingInput, out _, out _))
                throw new InvalidOperationException("A packet intake lost its receipt.");
            var retained = S([.. E(blockedInput), .. E(pendingInput)]);
            if (E(retained).Length != 0)
                RetainedPackets = S([.. E(RetainedPackets), new Relation(muscle.Accumulator.InputPort, new Relation(receive, retained))]);
            foreach (var action in E(actions))
                if (Drawing.TryReadDraw(action, out var chord)) contributions.Add(new Relation(tick, chord));
                else commands.Add(new Relation(tick, action));
            var connection = E(Connections).OfType<Relation>().SingleOrDefault(row => ReferenceEquals(row.Left, muscle.OutputPort));
            if (connection is not null)
            {
                var target = Muscles.Single(candidate => ReferenceEquals(candidate.Accumulator.InputPort, connection.Right));
                inputs[target].AddRange(E(drained));
                transfers.Add(new Relation(connection, new Relation(tick, drained)));
            }
            else
            {
                var output = new Relation(muscle.OutputPort, drained); releases.Add(output);
                ReleasedPackets = S([.. E(ReleasedPackets), output]);
            }
        }
        var effects = new List<IElement>();
        if (contributions.Count != 0)
        {
            var terms = new List<IElement>(); var complete = true;
            foreach (var contribution in contributions.Cast<Relation>())
            {
                if (!StructuralConstraint.TryReadScope((Relation)contribution.Right, out var moves, out var seam, out var tail))
                { complete = false; continue; }
                terms.AddRange(moves); complete &= seam.Equals(tail);
            }
            var chord = complete ? S(terms.ToArray()) : StructuralConstraint.Open(terms.ToArray());
            effects.Add(Drawing.Apply(new Placeholder(), S(Drawing.Draw(chord)), S(contributions.ToArray())));
        }
        foreach (var command in commands.Cast<Relation>())
            effects.Add(Drawing.Apply(new Placeholder(), S(command.Right), command.Left));
        foreach (var effect in effects.Cast<Relation>())
            if (!Drawing.TryReadAccepted(effect, out _, out _)) Unresolved = S([.. E(Unresolved), effect]);
        var receipt = new Relation(new Relation(Surface, new Relation(occurrence, offers)),
            new Relation(S(ticks.ToArray()), new Relation(S(transfers.ToArray()),
                new Relation(S(effects.ToArray()), S(releases.ToArray())))));
        Timeline = S([.. E(Timeline), receipt]); history.Add(occurrence); return receipt;
    }

    public Relation Run(int drives)
    {
        if (drives is < 0 or > 512) throw new ArgumentOutOfRangeException(nameof(drives));
        for (var i = 0; i < drives; i++) Pulse();
        return new Relation(Surface, new Relation(new Relation(Timeline, CurrentStates), new Relation(Drawing.State,
            new Relation(Unresolved, new Relation(ImportedPackets, new Relation(RetainedPackets, ReleasedPackets))))));
    }

    /// <summary>Current packet identity partition. Historical receipts can retain
    /// every identity as provenance; only these current ownership places count.</summary>
    public Relation Account()
    {
        var active = new List<IElement>(); var pending = new List<IElement>();
        foreach (var muscle in Muscles)
        {
            if (!muscle.Accumulator.TryReadState(out _, out var placements, out _, out _))
            { pending.Add(muscle.Accumulator.State); continue; }
            active.AddRange(E(placements).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, muscle.Accumulator.InputPort)));
        }
        var retained = E(RetainedPackets).Cast<Relation>().SelectMany(row => E((Relation)((Relation)row.Right).Right)
            .Select(packet => (IElement)new Relation(packet, row.Left))).ToArray();
        var released = E(ReleasedPackets).Cast<Relation>().SelectMany(row => E((Relation)row.Right)
            .Select(packet => (IElement)new Relation(packet, row.Left))).ToArray();
        var partition = active.Concat(retained).Concat(released).Cast<Relation>().ToArray();
        foreach (var packet in E(ImportedPackets))
            if (partition.Count(row => Same(row.Left, packet)) != 1) pending.Add(new Relation(packet, S(partition)));
        foreach (var row in partition)
            if (!E(ImportedPackets).Any(packet => Same(packet, row.Left))) pending.Add(row);
        return new Relation(new Relation(Surface, ImportedPackets),
            new Relation(S(active.ToArray()), new Relation(S(retained), new Relation(S(released), S(pending.ToArray())))));
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private bool FreshImports(Relation offers)
    {
        var seen = E(ImportedPackets).ToList();
        foreach (var packet in E(offers).Cast<Relation>().SelectMany(offer => E((Relation)offer.Right)))
        {
            if (seen.Any(prior => Same(prior, packet))) return false;
            seen.Add(packet);
        }
        return true;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation c && b is CarrierLocation d && c.Carrier.SameIdentityAs(d.Carrier) && c.SameTickRefAs(d);
}
