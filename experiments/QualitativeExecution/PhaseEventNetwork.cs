using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// A finite, explicitly driven network of phase components. Native routes emit
/// one receiving occurrence for each matching producer-occurrence/route pair.
/// FIFO delivery and an explicit work budget expose feedback without inventing
/// a global clock, elapsed intervals, material transfer, or an unbounded loop.
/// </summary>
public sealed class PhaseEventNetwork
{
    // port = source -> phase-surface
    // route = (producer-port -> triggering-phase) -> target-port
    // surface = Closed(ports) -> Closed(routes)
    // state = (surface -> root-event) ->
    //         ((Closed(port -> phase-state) -> Closed(producer-event -> route)) ->
    //          ((Closed(root-history) -> Closed(pending-deliveries)) -> proof))
    // pulse = port -> (new-root-event -> prior-root-event)
    // delivery = (target-port -> receiving-event) -> cause
    // cause = root-pulse | (producer-phase-receipt -> route)
    // receipt = ((before -> request) -> surface) ->
    //           ((after -> Closed(accepted-phase-receipts)) -> (blocked -> pending))
    // Delivery binds the target's actual predecessor only when it executes.
    // A route notification is not a conserved material packet.
    private readonly PhaseRecurrence[] components;

    public Relation Ports { get; }
    public Relation Routes { get; }
    public Relation Surface { get; }

    public PhaseEventNetwork(PhaseRecurrence[] components, Relation ports, Relation routes)
    {
        this.components = components.ToArray();
        Ports = ports; Routes = routes;
        Surface = new Relation(ports, routes);
    }

    public static Relation Port(IElement source, PhaseRecurrence phase) => new(source, phase.Surface);

    public static Relation Route(Relation producerPort, IElement phase, Relation targetPort) =>
        new(new Relation(producerPort, phase), targetPort);

    /// <summary>Each declared port needs one port -> phase-support start row.
    /// A malformed start remains attached and cannot be executed as a default.</summary>
    public Relation Initial(IElement initialEvent, Relation starts)
    {
        var rows = new List<IElement>();
        var good = Contract(out var ports, out _) && FiniteLaw.ClosedEntries(starts, out _);
        if (good)
        {
            var entries = FiniteLaw.Entries(starts);
            good = entries.Length == ports.Length;
            foreach (var (port, phase) in ports)
            {
                var matches = entries.OfType<Relation>().Where(row => ReferenceEquals(row.Left, port)).ToArray();
                if (matches.Length != 1 || matches[0].Right is not Relation support ||
                    !StructuralConstraint.TryReadScope(support, out _, out _, out _))
                { good = false; continue; }
                rows.Add(new Relation(port, phase.Initial(port.Left, initialEvent, support)));
            }
        }
        return State(initialEvent, good ? StructuralConstraint.Closed(rows.ToArray()) : starts,
            StructuralConstraint.Closed(), StructuralConstraint.Closed(initialEvent), StructuralConstraint.Closed(),
            new Relation(starts, good ? StructuralConstraint.Closed() : StructuralConstraint.Open(starts)));
    }

    public Relation Pulse(Relation port, IElement eventIdentity, IElement priorRoot) =>
        new(port, new Relation(eventIdentity, priorRoot));

    public Relation Step(Relation state, Relation pulse, int budget = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(budget);
        if (!Read(state, out var root, out var rows, out var consumed, out var history, out var queue,
                out var ports, out var routes))
            return Receipt(state, pulse, state, [], [], StructuralConstraint.Closed(new Relation(state, pulse)));
        if (queue.Length != 0)
            return Receipt(state, pulse, state, [], [pulse], StructuralConstraint.Closed(queue));
        if (pulse.Left is not Relation port || !ports.Any(view => ReferenceEquals(view.Port, port)) ||
            pulse.Right is not Relation events || !FiniteLaw.SameParticipant(events.Right, root) ||
            history.Any(e => FiniteLaw.SameParticipant(e, events.Left)))
            return Receipt(state, pulse, state, [], [pulse], StructuralConstraint.Closed());

        var delivery = new Relation(new Relation(port, events.Left), pulse);
        return Execute(state, pulse, events.Left, rows, consumed, [.. history, events.Left],
            [delivery], ports, routes, budget);
    }

    /// <summary>Continues the same root's retained FIFO queue. A new root is
    /// refused until the current queue is empty; resumption injects no new drive.</summary>
    public Relation Resume(Relation state, int budget = 64)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(budget);
        var request = new Relation(Surface, state);
        if (!Read(state, out var root, out var rows, out var consumed, out var history, out var queue,
                out var ports, out var routes))
            return Receipt(state, request, state, [], [], StructuralConstraint.Closed(new Relation(state, request)));
        return Execute(state, request, root, rows, consumed, history, queue, ports, routes, budget);
    }

    public bool TryReadState(Relation state, out IElement rootEvent, out Relation componentStates,
        out Relation consumed, out Relation pending)
    {
        rootEvent = null!; componentStates = consumed = pending = null!;
        if (!Read(state, out var root, out _, out _, out _, out _, out _, out _) ||
            state.Right is not Relation { Left: Relation components, Right: Relation { Left: Relation run } } ||
            components.Left is not Relation states || components.Right is not Relation history ||
            run.Right is not Relation queue) return false;
        rootEvent = root; componentStates = states; consumed = history; pending = queue;
        return true;
    }

    /// <summary>Inspects a network receipt. Accepted entries remain guarded
    /// PhaseRecurrence receipts; this reader does not authenticate fabricated ledgers.</summary>
    public bool TryReadStep(Relation receipt, out Relation before, out Relation request,
        out Relation after, out Relation acceptedPhaseReceipts, out Relation blocked, out Relation pending)
    {
        before = request = after = acceptedPhaseReceipts = blocked = pending = null!;
        if (receipt.Left is not Relation { Left: Relation premise, Right: var surface } ||
            !ReferenceEquals(surface, Surface) || premise.Left is not Relation previous ||
            premise.Right is not Relation asked ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation refusal } ||
            result.Left is not Relation current || result.Right is not Relation accepted ||
            refusal.Left is not Relation refused || refusal.Right is not Relation queue ||
            !FiniteLaw.ClosedEntries(accepted, out _) || !FiniteLaw.ClosedEntries(refused, out _) ||
            !FiniteLaw.ClosedEntries(queue, out _)) return false;
        before = previous; request = asked; after = current;
        acceptedPhaseReceipts = accepted; blocked = refused; pending = queue;
        return true;
    }

    private Relation Execute(Relation before, Relation request, IElement root,
        Relation[] rows, Relation[] consumed, IElement[] history, Relation[] deliveries,
        (Relation Port, PhaseRecurrence Phase)[] ports, Relation[] routes, int budget)
    {
        var states = rows.ToList();
        var used = consumed.ToList();
        var queue = new Queue<Relation>(deliveries);
        var accepted = new List<IElement>();
        var blocked = new List<IElement>();
        var proof = new List<IElement> { new Relation(before, request) };
        for (var spent = 0; spent < budget && queue.TryPeek(out var delivery); spent++)
        {
            var target = (Relation)((Relation)delivery.Left).Left;
            var occurrence = ((Relation)delivery.Left).Right;
            var phase = ports.Single(view => ReferenceEquals(view.Port, target)).Phase;
            var index = states.FindIndex(row => ReferenceEquals(row.Left, target));
            var state = (Relation)states[index].Right;
            phase.TryReadState(state, out _, out var previous, out _);
            var drive = phase.Drive(target.Left, occurrence, previous, delivery);
            var result = phase.Step(state, drive);
            proof.Add(new Relation(delivery, result));
            if (!phase.TryReadStep(result, out _, out _, out var after, out _, out var refused, out var pending) ||
                FiniteLaw.Entries(pending).Length != 0)
                break; // The attempted delivery remains at the head with its failure evidence.
            if (!phase.TryReadAccepted(result, out _, out var eventIdentity, out var output))
            {
                blocked.Add(result);
                queue.Dequeue();
                if (FiniteLaw.Entries(refused).Length == 0) break;
                continue;
            }
            queue.Dequeue();
            accepted.Add(result);
            states[index] = new Relation(target, after);
            foreach (var route in routes)
            {
                var trigger = (Relation)route.Left;
                if (!ReferenceEquals(trigger.Left, target) || !FiniteLaw.SameParticipant(trigger.Right, output)) continue;
                if (used.Any(pair => FiniteLaw.SameParticipant(pair.Left, eventIdentity) && ReferenceEquals(pair.Right, route)))
                { proof.Add(new Relation(new Relation(eventIdentity, route), StructuralConstraint.Closed(used.Cast<IElement>().ToArray()))); continue; }
                used.Add(new Relation(eventIdentity, route));
                queue.Enqueue(new Relation(new Relation(route.Right, new Placeholder()), new Relation(result, route)));
            }
        }
        var pendingScope = StructuralConstraint.Closed(queue.Cast<IElement>().ToArray());
        var next = State(root, StructuralConstraint.Closed(states.Cast<IElement>().ToArray()),
            StructuralConstraint.Closed(used.Cast<IElement>().ToArray()), StructuralConstraint.Closed(history),
            pendingScope, StructuralConstraint.Closed(proof.ToArray()));
        return Receipt(before, request, next, accepted.ToArray(), blocked.ToArray(), pendingScope);
    }

    private Relation State(IElement root, Relation states, Relation consumed, Relation history, Relation pending, IElement proof) =>
        new(new Relation(Surface, root), new Relation(new Relation(states, consumed),
            new Relation(new Relation(history, pending), proof)));

    private Relation Receipt(Relation before, Relation request, Relation after, IElement[] accepted,
        IElement[] blocked, Relation pending) =>
        new(new Relation(new Relation(before, request), Surface),
            new Relation(new Relation(after, StructuralConstraint.Closed(accepted)),
                new Relation(StructuralConstraint.Closed(blocked), pending)));

    private bool Contract(out (Relation Port, PhaseRecurrence Phase)[] ports, out Relation[] routes)
    {
        ports = []; routes = [];
        if (!ReferenceEquals(Surface.Left, Ports) || !ReferenceEquals(Surface.Right, Routes) ||
            !FiniteLaw.DomainEntries(Ports, out var entries) || entries.Length == 0 ||
            !FiniteLaw.ClosedEntries(Routes, out var routeEntries)) return false;
        var views = new List<(Relation Port, PhaseRecurrence Phase)>();
        foreach (var entry in entries)
        {
            if (entry is not Relation port) return false;
            var adapters = components.Where(component => ReferenceEquals(component.Surface, port.Right)).ToArray();
            if (adapters.Length != 1 || !FiniteLaw.DomainEntries(adapters[0].Domain, out var phases) || phases.Length == 0 ||
                !FiniteLaw.ClosedEntries(adapters[0].Transitions, out var edges) || edges.Any(edge =>
                    edge is not Relation order || FiniteLaw.IndexOf(phases, order.Left) < 0 ||
                    FiniteLaw.IndexOf(phases, order.Right) < 0) || views.Any(view =>
                    ReferenceEquals(view.Port.Right, port.Right) && FiniteLaw.SameParticipant(view.Port.Left, port.Left))) return false;
            views.Add((port, adapters[0]));
        }
        if (components.Any(component => !views.Any(view => ReferenceEquals(view.Phase, component)))) return false;
        foreach (var entry in routeEntries)
        {
            if (entry is not Relation { Left: Relation trigger, Right: Relation target } ||
                trigger.Left is not Relation source || !views.Any(view => ReferenceEquals(view.Port, target))) return false;
            var producer = views.Where(view => ReferenceEquals(view.Port, source)).ToArray();
            if (producer.Length != 1 || !FiniteLaw.Entries(producer[0].Phase.Domain).Any(phase =>
                    FiniteLaw.SameParticipant(phase, trigger.Right))) return false;
        }
        ports = views.ToArray(); routes = routeEntries.Cast<Relation>().ToArray();
        return true;
    }

    private bool Read(Relation state, out IElement root, out Relation[] rows, out Relation[] consumed,
        out IElement[] history, out Relation[] pending, out (Relation Port, PhaseRecurrence Phase)[] ports,
        out Relation[] routes)
    {
        root = null!; rows = consumed = pending = []; history = [];
        if (!Contract(out ports, out routes) ||
            state.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            state.Right is not Relation { Left: Relation componentPart, Right: Relation { Left: Relation run } } ||
            componentPart.Left is not Relation componentScope || componentPart.Right is not Relation consumedScope ||
            run.Left is not Relation rootHistory || run.Right is not Relation queue ||
            !FiniteLaw.ClosedEntries(componentScope, out var componentEntries) || componentEntries.Length != ports.Length ||
            componentEntries.Any(entry => entry is not Relation) || !FiniteLaw.ClosedEntries(consumedScope, out var used) ||
            used.Any(entry => entry is not Relation) || !FiniteLaw.ClosedEntries(rootHistory, out history) ||
            history.Length == 0 || !FiniteLaw.SameParticipant(history[^1], context.Right) ||
            !FiniteLaw.ClosedEntries(queue, out var queued) || queued.Any(entry => entry is not Relation)) return false;
        var states = componentEntries.Cast<Relation>().ToArray();
        foreach (var view in ports)
        {
            var matches = states.Where(row => ReferenceEquals(row.Left, view.Port)).ToArray();
            if (matches.Length != 1 || matches[0].Right is not Relation phaseState ||
                !view.Phase.TryReadState(phaseState, out var owner, out _, out _) ||
                !FiniteLaw.SameParticipant(owner, view.Port.Left)) return false;
        }
        for (var i = 0; i < history.Length; i++)
            for (var j = 0; j < i; j++)
                if (FiniteLaw.SameParticipant(history[i], history[j])) return false;
        var pairs = used.Cast<Relation>().ToArray();
        for (var i = 0; i < pairs.Length; i++)
        {
            if (!routes.Any(route => ReferenceEquals(route, pairs[i].Right))) return false;
            var producerPort = (Relation)((Relation)((Relation)pairs[i].Right).Left).Left;
            var producerAdapter = ports.Single(view => ReferenceEquals(view.Port, producerPort));
            var producerState = (Relation)states.Single(row => ReferenceEquals(row.Left, producerPort)).Right;
            if (!producerAdapter.Phase.TryReadHistory(producerState, out var trail) ||
                !FiniteLaw.Entries(trail).Any(e => FiniteLaw.SameParticipant(e, pairs[i].Left))) return false;
            for (var j = 0; j < i; j++)
                if (FiniteLaw.SameParticipant(pairs[i].Left, pairs[j].Left) && ReferenceEquals(pairs[i].Right, pairs[j].Right)) return false;
        }
        var deliveries = queued.Cast<Relation>().ToArray();
        var causes = new List<(IElement Event, Relation Route)>();
        var receivingEvents = new List<IElement>();
        foreach (var delivery in deliveries)
        {
            if (!Delivery(delivery, context.Right, history, states, pairs, ports, routes,
                    out var producerEvent, out var route)) return false;
            var receivingEvent = ((Relation)delivery.Left).Right;
            if (receivingEvents.Any(previous => FiniteLaw.SameParticipant(previous, receivingEvent))) return false;
            receivingEvents.Add(receivingEvent);
            if (producerEvent is null || route is null) continue;
            if (causes.Any(previous => FiniteLaw.SameParticipant(previous.Event, producerEvent) &&
                    ReferenceEquals(previous.Route, route))) return false;
            causes.Add((producerEvent, route));
        }
        root = context.Right; rows = states; consumed = pairs; pending = deliveries;
        return true;
    }

    private bool Delivery(Relation delivery, IElement root, IElement[] history, Relation[] states, Relation[] consumed,
        (Relation Port, PhaseRecurrence Phase)[] ports, Relation[] routes,
        out IElement? producerOccurrence, out Relation? triggerRoute)
    {
        producerOccurrence = null; triggerRoute = null;
        if (delivery.Left is not Relation { Left: Relation target } addressed ||
            !ports.Any(view => ReferenceEquals(view.Port, target)) || delivery.Right is not Relation cause) return false;
        if (cause.Left is Relation rootPort && ReferenceEquals(rootPort, target) && cause.Right is Relation rootEvents &&
            history.Length >= 2 && FiniteLaw.SameParticipant(rootEvents.Right, history[^2]) &&
            FiniteLaw.SameParticipant(rootEvents.Left, root) && FiniteLaw.SameParticipant(addressed.Right, root)) return true;
        if (cause.Left is not Relation producerReceipt || cause.Right is not Relation route ||
            !routes.Any(declared => ReferenceEquals(declared, route)) || !ReferenceEquals(route.Right, target)) return false;
        var producerPort = (Relation)((Relation)route.Left).Left;
        var phase = ports.Single(view => ReferenceEquals(view.Port, producerPort)).Phase;
        if (!phase.TryReadAccepted(producerReceipt, out var source, out var producerEvent, out var producedPhase) ||
            !FiniteLaw.SameParticipant(source, producerPort.Left) ||
            !FiniteLaw.SameParticipant(producedPhase, ((Relation)route.Left).Right) ||
            !consumed.Any(pair => FiniteLaw.SameParticipant(pair.Left, producerEvent) && ReferenceEquals(pair.Right, route)) ||
            !phase.TryReadStep(producerReceipt, out _, out _, out var producerAfter, out _, out _, out _) ||
            !phase.TryReadHistory(producerAfter, out var prefix) ||
            !phase.TryReadHistory((Relation)states.Single(row => ReferenceEquals(row.Left, producerPort)).Right, out var current)) return false;
        var previous = FiniteLaw.Entries(prefix); var trail = FiniteLaw.Entries(current);
        if (previous.Length > trail.Length || !previous.Select((e, i) => FiniteLaw.SameParticipant(e, trail[i])).All(same => same))
            return false;
        producerOccurrence = producerEvent; triggerRoute = route;
        return true;
    }
}
