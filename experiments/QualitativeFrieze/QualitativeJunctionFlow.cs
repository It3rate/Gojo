using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A local conditional response over two exposed packet passages.
/// Policy factories construct native finite laws; execution reads those laws.
/// Its drawing is a separate local trace over a fixed captured substrate.</summary>
public sealed class JunctionFlowNode
{
    public string Name { get; }
    public IElement Cell { get; }
    public Relation Anchor { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public QualitativePatternTransform Projection { get; }
    public Relation InputPort { get; }
    public Relation AuxiliaryPort { get; }
    public Relation SinkPort { get; }
    public Relation InputSites { get; }
    public Relation AuxiliarySites { get; }
    public Relation SinkSites { get; }
    public Relation Ready { get; }
    public Relation Waiting { get; }
    public Relation AuxiliaryReady { get; }
    public Relation AuxiliaryWaiting { get; }
    public Relation Hold { get; } = S();
    public Relation Up { get; }
    public Relation Down { get; }
    public Relation Actions { get; }
    public Relation SignalLaw { get; }
    public Relation PhaseLaw { get; }
    public PhaseRecurrence Controller { get; }
    public Relation ControllerState { get; internal set; }
    public IElement Source { get; } = new Placeholder();
    public Relation Surface { get; }
    public Relation Activations { get; internal set; } = S();
    public Relation Pending { get; internal set; } = S();
    public Relation Conditions { get; internal set; } = S();
    public IEnumerable<Relation> Inputs => new[] { InputPort, AuxiliaryPort };

    public JunctionFlowNode(string name, IElement cell, Relation anchor,
        QualitativeMotionFrame worldFrame, IElement reach,
        Func<JunctionFlowNode, Relation> signalBuilder, bool latch = false)
    {
        Name = name; Cell = cell; Anchor = anchor;
        var frame = new QualitativeMotionFrame(); Drawing = new(frame);
        InputSites = S(new Placeholder()); AuxiliarySites = S(new Placeholder()); SinkSites = S(new Placeholder(), new Placeholder());
        InputPort = new(cell, InputSites); AuxiliaryPort = new(cell, AuxiliarySites); SinkPort = new(cell, SinkSites);
        Ready = new(InputSites, InputSites); Waiting = new(InputSites, StructuralConstraint.Open());
        AuxiliaryReady = new(AuxiliarySites, AuxiliarySites); AuxiliaryWaiting = new(AuxiliarySites, StructuralConstraint.Open());
        Relation W(IElement a, IElement b) => S(QualitativeMotionFrame.Move(a, reach), QualitativeMotionFrame.Move(b, reach));
        Up = Drawing.Draw(W(frame.Right, frame.Up)); Down = Drawing.Draw(W(frame.Right, frame.Down)); Actions = S(Up, Down, Hold);
        Projection = new(frame, worldFrame, S(
            Row(frame.Right, worldFrame.Right), Row(frame.Left, worldFrame.Left),
            Row(frame.Up, worldFrame.Up), Row(frame.Down, worldFrame.Down)));
        var first = new Placeholder(); var later = new Placeholder();
        Controller = new(S(first, later), S(new Relation(first, later), new Relation(later, latch ? later : first)));
        ControllerState = Controller.Initial(Source, new Placeholder(), S(first));
        SignalLaw = signalBuilder(this);
        PhaseLaw = FiniteLaw.Create(Actions, Controller.Domain, Actions,
            FiniteLaw.Row(Up, first, S(Up)), FiniteLaw.Row(Down, first, S(Down)), FiniteLaw.Row(Hold, first, S(Hold)),
            FiniteLaw.Row(Up, later, S(latch ? Hold : Down)), FiniteLaw.Row(Down, later, S(latch ? Hold : Up)), FiniteLaw.Row(Hold, later, S(Hold)));
        Surface = new(new Relation(cell, anchor), new Relation(new Relation(Drawing.Surface, Projection.Surface),
            new Relation(new Relation(new Relation(InputPort, AuxiliaryPort), SinkPort),
                new Relation(new Relation(SignalLaw, PhaseLaw), new Relation(Controller.Surface, ControllerState)))));
        Relation Row(IElement from, IElement to) => new(QualitativeMotionFrame.Move(from, reach), S(QualitativeMotionFrame.Move(to, reach)));
    }

    public static JunctionFlowNode Relay(string name, IElement cell, Relation anchor, QualitativeMotionFrame frame, IElement reach) =>
        new(name, cell, anchor, frame, reach, node => node.Signal((a, _) => a ? node.Up : node.Hold));
    public static JunctionFlowNode Coincidence(string name, IElement cell, Relation anchor, QualitativeMotionFrame frame, IElement reach) =>
        new(name, cell, anchor, frame, reach, node => node.Signal((a, b) => a && b ? node.Up : node.Hold), true);
    public static JunctionFlowNode Reflection(string name, IElement cell, Relation anchor, QualitativeMotionFrame frame, IElement reach) =>
        new(name, cell, anchor, frame, reach, node => node.Signal((a, b) => a ? b ? node.Down : node.Up : node.Hold), true);

    public Relation Signal(Func<bool, bool, Relation> construction) => FiniteLaw.Create(S(Waiting, Ready), S(AuxiliaryWaiting, AuxiliaryReady), Actions,
        FiniteLaw.Row(Waiting, AuxiliaryWaiting, S(construction(false, false))), FiniteLaw.Row(Waiting, AuxiliaryReady, S(construction(false, true))),
        FiniteLaw.Row(Ready, AuxiliaryWaiting, S(construction(true, false))), FiniteLaw.Row(Ready, AuxiliaryReady, S(construction(true, true))));

    public Relation Ask(QualitativePacketTransport transport, Relation snapshot, IElement refinement)
    {
        if (!transport.TryReadState(snapshot, out var occurrence, out var placements, out _, out _) ||
            !E(transport.Capacities).OfType<Relation>().Any(row => ReferenceEquals(row.Left, InputPort) && ReferenceEquals(row.Right, InputSites)) ||
            !E(transport.Capacities).OfType<Relation>().Any(row => ReferenceEquals(row.Left, AuxiliaryPort) && ReferenceEquals(row.Right, AuxiliarySites)))
            return new Relation(Surface, StructuralConstraint.Open(new Relation(transport.Surface, snapshot)));
        var a = Publish(InputPort, InputSites, Waiting, Ready); var b = Publish(AuxiliaryPort, AuxiliarySites, AuxiliaryWaiting, AuxiliaryReady);
        QualitativePublication.TryRead(a, out _, out _, out _, out _, out _, out var first, out _);
        QualitativePublication.TryRead(b, out _, out _, out _, out _, out _, out var second, out _);
        var read = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(SignalLaw, first, second), new Relation(a, b));
        if (!FiniteLaw.TryReadResult(read, out var commands, out _, out _) ||
            !Controller.TryReadState(ControllerState, out _, out _, out var phases)) return new Relation(Surface, StructuralConstraint.Open(read));
        var gated = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(PhaseLaw, commands, phases), new Relation(read, ControllerState));
        return QualitativeQuestion.Ask(gated, S(E(Actions).Select(action => (IElement)new Relation(action, action)).ToArray()), refinement);

        Relation Publish(Relation port, Relation sites, Relation wait, Relation ready)
        {
            var filled = E(placements).OfType<Relation>().Where(row => row.Right is Relation location && ReferenceEquals(location.Left, port)).ToArray();
            var missing = E(sites).Where(site => !filled.Any(row => ReferenceEquals(((Relation)row.Right).Right, site))).ToArray();
            var evidence = new Relation(new Relation(transport.Surface, snapshot), new Relation(port, new Relation(S(filled), S(missing))));
            return QualitativePublication.Create(Cell, port, occurrence, sites, S(wait, ready), S(missing.Length == 0 ? ready : wait), evidence);
        }
    }
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

/// <summary>Live, delayed material flow over actual accepted motif junctions.
/// A selected finite local world owns each packet once, including queues and
/// explicit sinks. Neighbour pressure gates a drawing; the observation itself
/// does not duplicate or consume material.</summary>
public sealed class QualitativeJunctionFlow
{
    // channel = (actual topology edge -> (source input -> target input)) -> queue port
    // channel application = channel -> (queue capacity -> (export route -> delivery route))
    // request = (Surface -> (round-state -> (node.Surface -> question))) ->
    //           ((drawing-before -> offer) -> (transport-before -> (moves -> controller-before)))
    // activation = (request -> accepted drawing receipt) -> (phase step -> transport step)
    private sealed record Channel(Relation Definition, Relation Edge, Relation Queue, Relation Export, Relation Delivery);
    private readonly List<Channel> channels = [];
    private readonly IElement source = new Placeholder();
    private readonly IElement refinement = new Placeholder();
    public string Name { get; }
    public QualitativeJunctionTopology Topology { get; }
    public IReadOnlyList<JunctionFlowNode> Nodes { get; }
    public Relation Channels { get; }
    public Relation ChannelApplications { get; }
    public Relation OutputRoutes { get; }
    public QualitativePacketTransport Transport { get; }
    public PhaseRecurrence Clock { get; }
    public Relation ClockState { get; private set; }
    public Relation Surface { get; }
    public Relation Timeline { get; private set; } = S();
    public Relation Deliveries { get; private set; } = S();
    public Relation Emissions { get; private set; } = S();
    public Relation Blocked { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    public Relation DefinitionProblems { get; }
    public Relation Pending => S(Nodes.SelectMany(node => E(node.Pending)).ToArray());
    public bool IsValid => E(DefinitionProblems).Length == 0;

    public QualitativeJunctionFlow(string name, QualitativeJunctionTopology topology,
        IReadOnlyList<JunctionFlowNode> nodes, Relation channelDefinitions)
    {
        Name = name; Topology = topology; Nodes = Array.AsReadOnly(nodes.ToArray()); Channels = channelDefinitions;
        var problems = new List<IElement>(); var capacities = new List<IElement>(); var routes = new List<IElement>();
        var applications = new List<IElement>(); var outputs = new List<IElement>();
        if (!topology.IsValid) problems.Add(topology.Surface);
        if (!Closed(channelDefinitions)) problems.Add(channelDefinitions);
        foreach (var node in Nodes)
        {
            if (!E(topology.Cells).Any(cell => ReferenceEquals(cell, node.Cell)) || Nodes.Count(other => ReferenceEquals(other.Cell, node.Cell)) != 1 ||
                !ReferenceEquals(node.Projection.Target, topology.Field.SourceGraph.Frame) ||
                !E(topology.Anchors).OfType<Relation>().Any(row => ReferenceEquals(row.Left, node.Cell) && ReferenceEquals(row.Right, node.Anchor))) problems.Add(node.Surface);
            capacities.Add(new Relation(node.InputPort, node.InputSites)); capacities.Add(new Relation(node.AuxiliaryPort, node.AuxiliarySites)); capacities.Add(new Relation(node.SinkPort, node.SinkSites));
        }
        foreach (var item in E(channelDefinitions))
        {
            if (item is not Relation { Left: Relation context, Right: Relation queue } definition ||
                context.Left is not Relation edge || context.Right is not Relation ports ||
                !topology.TryReadEdge(edge, out var from, out var to, out _) ||
                Nodes.Where(node => ReferenceEquals(node.Cell, from)).ToArray() is not [var before] ||
                Nodes.Where(node => ReferenceEquals(node.Cell, to)).ToArray() is not [var after] ||
                ports.Left is not Relation input || ports.Right is not Relation target ||
                !before.Inputs.Any(port => ReferenceEquals(port, input)) || !after.Inputs.Any(port => ReferenceEquals(port, target)) ||
                channels.Any(channel => ReferenceEquals(channel.Export.Left, input) || ReferenceEquals(channel.Queue, queue)))
            { problems.Add(item); continue; }
            var export = new Relation(input, queue); var delivery = new Relation(queue, target); var capacity = new Relation(queue, S(new Placeholder()));
            capacities.Add(capacity); routes.Add(export); routes.Add(delivery); outputs.Add(new Relation(input, export));
            channels.Add(new(definition, edge, queue, export, delivery));
            applications.Add(new Relation(definition, new Relation(capacity, new Relation(export, delivery))));
        }
        foreach (var node in Nodes) foreach (var port in node.Inputs)
            if (!outputs.OfType<Relation>().Any(row => ReferenceEquals(row.Left, port)))
            { var sink = new Relation(port, node.SinkPort); routes.Add(sink); outputs.Add(new Relation(port, sink)); }
        ChannelApplications = S(applications.ToArray()); OutputRoutes = S(outputs.ToArray());
        var phase = new Placeholder(); Clock = new(S(phase), S(new Relation(phase, phase))); ClockState = Clock.Initial(source, new Placeholder(), S(phase));
        Transport = new(S(capacities.ToArray()), S(routes.ToArray()), new Relation(topology.Surface, new Relation(ChannelApplications, OutputRoutes)));
        if (!Transport.TryReadState(Transport.State, out _, out _, out _, out _)) problems.Add(Transport.Surface);
        DefinitionProblems = S(problems.ToArray());
        Surface = new(new Relation(topology.Surface, S(Nodes.Select(node => (IElement)node.Surface).ToArray())),
            new Relation(new Relation(channelDefinitions, ChannelApplications), new Relation(OutputRoutes,
                new Relation(Transport.Surface, new Relation(Clock.Surface, new Relation(ClockState, DefinitionProblems))))));
    }

    public Relation Seed(IElement occurrence, Relation port, Relation packets)
    {
        if (!IsValid || E(Pending).Length != 0 || !Nodes.Any(node => node.Inputs.Any(input => ReferenceEquals(input, port))))
            return Refuse(new Relation(port, packets));
        return Transport.Import(occurrence, port, packets, new Relation(Surface, ClockState));
    }

    public Relation Propose(JunctionFlowNode node) => Propose(node, Transport.State);
    private Relation Propose(JunctionFlowNode node, Relation snapshot)
    {
        if (!IsValid || !Nodes.Any(actual => ReferenceEquals(actual, node)) || E(Pending).Length != 0) return Refuse(node.Surface);
        var question = node.Ask(Transport, snapshot, refinement); node.Conditions = Append(node.Conditions, question);
        if (!QualitativeQuestion.TryReadAnswer(question, out var answer))
        { Unresolved = Append(Unresolved, question); return question; }
        if (ReferenceEquals(answer, node.Hold)) return question;
        if (answer is not Relation action || !E(node.Actions).Any(actual => ReferenceEquals(actual, action))) return Refuse(question);
        var moves = new List<IElement>();
        foreach (var port in node.Inputs)
        {
            var priorPackets = Transport.PortPackets(snapshot, port); var current = Transport.PortPackets(port);
            if (!SameSet(E(priorPackets), E(current))) return Refuse(new Relation(snapshot, Transport.State));
            var route = (Relation)E(OutputRoutes).Cast<Relation>().Single(row => ReferenceEquals(row.Left, port)).Right;
            moves.AddRange(E(current).Select(packet => (IElement)new Relation(packet, route)));
        }
        if (moves.Count == 0) return Refuse(question);
        var eventIdentity = new Placeholder(); var movement = S(moves.ToArray());
        var preview = Transport.Prepare(new Placeholder(), movement, question);
        if (!Transport.TryReadAccepted(preview, out _, out _)) { Blocked = Append(Blocked, preview); return preview; }
        node.Drawing.TryReadState(node.Drawing.State, out var previous, out _, out _, out _, out _, out _, out _);
        var offer = node.Drawing.Offer(node.Drawing.Source, eventIdentity, previous, S(action), new Relation(question, new Relation(snapshot, preview)));
        var request = new Relation(new Relation(Surface, new Relation(ClockState, new Relation(node.Surface, question))),
            new Relation(new Relation(node.Drawing.State, offer), new Relation(Transport.State, new Relation(movement, node.ControllerState))));
        node.Pending = S(request); return request;
    }

    public bool TryReadRequest(Relation request, out JunctionFlowNode node, out Relation action, out Relation drawingBefore, out Relation offer)
    {
        node = null!; action = drawingBefore = offer = null!;
        if (request.Left is not Relation { Left: var owner, Right: Relation { Right: Relation choice } } || !ReferenceEquals(owner, Surface) ||
            choice.Right is not Relation question || Nodes.SingleOrDefault(candidate => ReferenceEquals(candidate.Surface, choice.Left)) is not JunctionFlowNode found ||
            !QualitativeQuestion.TryReadAnswer(question, out var selected) || selected is not Relation command || ReferenceEquals(command, found.Hold) ||
            !E(found.Actions).Any(actual => ReferenceEquals(actual, command)) ||
            request.Right is not Relation { Left: Relation effect, Right: Relation } || effect.Left is not Relation before || effect.Right is not Relation offered) return false;
        node = found; action = command; drawingBefore = before; offer = offered; return true;
    }

    public Relation Acknowledge(Relation request, Relation drawingReceipt)
    {
        if (!TryReadRequest(request, out var node, out var action, out var before, out var offer) ||
            E(node.Pending) is not [var exact] || !ReferenceEquals(exact, request) || !ReferenceEquals(before, node.Drawing.State) ||
            request.Left is not Relation { Right: Relation { Left: var round, Right: Relation { Right: Relation question } } } || !ReferenceEquals(round, ClockState) ||
            request.Right is not Relation { Right: Relation { Left: Relation transportBefore, Right: Relation motion } } ||
            !ReferenceEquals(transportBefore, Transport.State) || !ReferenceEquals(motion.Right, node.ControllerState) || motion.Left is not Relation moves ||
            !node.Drawing.TryReadStep(drawingReceipt, out var actualBefore, out var actualOffer, out _, out _, out _, out _) ||
            !ReferenceEquals(actualBefore, before) || !ReferenceEquals(actualOffer, offer) || !node.Drawing.TryReadAccepted(drawingReceipt, out _, out _))
            return Refuse(new Relation(request, drawingReceipt));
        // Re-read the exact current local decision before any mutation. The
        // request's question retains its earlier input snapshot as provenance.
        var fresh = node.Ask(Transport, Transport.State, refinement);
        if (!QualitativeQuestion.TryReadAnswer(fresh, out var current) || !ReferenceEquals(current, action)) return Refuse(fresh);
        var transfer = Transport.Prepare(new Placeholder(), moves, new Relation(request, drawingReceipt));
        if (!Transport.TryReadAccepted(transfer, out _, out _)) return Refuse(transfer);
        node.Controller.TryReadState(node.ControllerState, out _, out var prior, out _);
        var effectEvent = ((Relation)((Relation)offer.Left).Right).Left;
        var step = node.Controller.Step(node.ControllerState, node.Controller.Drive(node.Source, effectEvent, prior,
            new Relation(question, new Relation(drawingReceipt, transfer))));
        if (!node.Controller.TryReadAccepted(step, out _, out _, out _) || !node.Controller.TryReadStep(step, out _, out _, out var after, out _, out _, out _)) return Refuse(step);
        if (!node.Drawing.TryCommit(drawingReceipt) || !Transport.TryCommit(transfer))
            throw new InvalidOperationException("A preflighted local action and ownership patch lost their exact source state.");
        node.ControllerState = after; node.Pending = S();
        var activation = new Relation(new Relation(request, drawingReceipt), new Relation(step, transfer));
        node.Activations = Append(node.Activations, activation); Emissions = Append(Emissions, activation); return activation;
    }

    public Relation Round(Relation? schedule = null)
    {
        if (!IsValid || E(Pending).Length != 0) return Refuse(new Relation(ClockState, Pending));
        var selected = schedule ?? S(Nodes.Select(node => (IElement)node.Surface).ToArray());
        if (!Closed(selected) || E(selected).Length != Nodes.Count || Nodes.Any(node => E(selected).Count(surface => ReferenceEquals(surface, node.Surface)) != 1)) return Refuse(selected);
        var beforeClock = ClockState; var beforeTransport = Transport.State;
        Clock.TryReadState(ClockState, out _, out var prior, out _);
        var tick = Clock.Step(ClockState, Clock.Drive(source, new Placeholder(), prior, new Relation(beforeTransport, selected)));
        if (!Clock.TryReadAccepted(tick, out _, out _, out _) || !Clock.TryReadStep(tick, out _, out _, out var afterClock, out _, out _, out _)) return Refuse(tick);
        ClockState = afterClock; var arrivals = new List<IElement>(); var responses = new List<IElement>();
        // Only packets present in the before-round queue are eligible. New
        // emissions cannot be delivered by this occurrence, even in a cycle.
        foreach (var channel in channels)
            foreach (var packet in E(Transport.PortPackets(beforeTransport, channel.Queue)))
            {
                var transfer = Transport.Prepare(new Placeholder(), S(new Relation(packet, channel.Delivery)),
                    new Relation(tick, new Relation(beforeTransport, channel.Definition)));
                if (Transport.TryReadAccepted(transfer, out _, out _) && Transport.TryCommit(transfer))
                { arrivals.Add(transfer); Deliveries = Append(Deliveries, transfer); }
                else { Blocked = Append(Blocked, transfer); arrivals.Add(transfer); }
            }
        var snapshot = Transport.State;
        foreach (var surface in E(selected))
        {
            var node = Nodes.Single(candidate => ReferenceEquals(candidate.Surface, surface));
            var request = Propose(node, snapshot);
            if (TryReadRequest(request, out _, out _, out var before, out var offer))
                responses.Add(Acknowledge(request, node.Drawing.Step(before, offer)));
            else responses.Add(request);
        }
        var read = new Relation(new Relation(Surface, new Relation(new Relation(beforeClock, beforeTransport), tick)),
            new Relation(S(arrivals.ToArray()), new Relation(snapshot, new Relation(S(responses.ToArray()),
                new Relation(Transport.State, new Relation(NodeStates(), Pending))))));
        Timeline = Append(Timeline, read); return read;
    }

    public Relation Run(int rounds)
    {
        if (rounds is < 0 or > 128) throw new ArgumentOutOfRangeException(nameof(rounds));
        for (var i = 0; i < rounds && E(Pending).Length == 0; i++) Round();
        return new Relation(Surface, new Relation(Timeline, new Relation(NodeStates(),
            new Relation(Transport.State, new Relation(Transport.Account(), new Relation(Pending, new Relation(Blocked, Unresolved)))))));
    }
    public Relation NodeStates() => S(Nodes.Select(node => (IElement)new Relation(node.Surface,
        new Relation(node.ControllerState, new Relation(node.Drawing.State, new Relation(node.Conditions, new Relation(node.Activations, node.Pending)))))).ToArray());
    private Relation Refuse(IElement evidence) { Unresolved = Append(Unresolved, evidence); return new Relation(Surface, StructuralConstraint.Open(evidence)); }
    private static bool SameSet(IElement[] a, IElement[] b) => a.Length == b.Length && a.All(item => b.Any(other => ReferenceEquals(item, other)));
    private static Relation Append(Relation scope, IElement value) => S([.. E(scope), value]);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
