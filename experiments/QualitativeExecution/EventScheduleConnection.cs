using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// An observational connection between two explicitly authored causal schedules.
/// A schedule is a closed scope of predecessor -> successor relations. A window
/// connects a producer event to the receiver events allowed to reuse it. No host
/// time, private source state, draining, or process-global delivery registry is used.
/// </summary>
public sealed class EventScheduleConnection
{
    // surface = ((producer -> producer-schedule) -> (receiver -> receiver-schedule)) -> windows
    // window = producer-event -> Closed(permitted-receiver-events)
    // state = (surface -> receiver-cursor) ->
    //         ((Closed(receiver-history) -> Closed(current-producer-event?)) -> provenance)
    // delivery = publication -> (producer-event -> receiver-event)
    // receipt = ((before -> delivery) -> surface) ->
    //           ((after -> accepted) -> (blocked -> (unresolved -> provenance)))
    public IElement Producer { get; }
    public IElement Receiver { get; }
    public Relation ProducerSchedule { get; }
    public Relation ReceiverSchedule { get; }
    public Relation Windows { get; }
    public Relation Surface { get; }

    public EventScheduleConnection(IElement producer, IElement receiver,
        Relation producerSchedule, Relation receiverSchedule, Relation windows)
    {
        Producer = producer;
        Receiver = receiver;
        ProducerSchedule = producerSchedule;
        ReceiverSchedule = receiverSchedule;
        Windows = windows;
        Surface = new Relation(new Relation(new Relation(producer, producerSchedule),
            new Relation(receiver, receiverSchedule)), windows);
    }

    public static Relation Window(IElement producerEvent, params IElement[] receiverEvents) =>
        new(producerEvent, StructuralConstraint.Closed(receiverEvents));

    /// <summary>The caller supplies the receiver's actual causal predecessor.
    /// This creates an independent history, rather than registering an active head.</summary>
    public Relation Initial(IElement receiverPredecessor) =>
        State(receiverPredecessor, StructuralConstraint.Closed(receiverPredecessor),
            StructuralConstraint.Closed(), StructuralConstraint.Closed());

    /// <summary>Admits one observation for the next explicit receiver event.
    /// The same publication may serve several listed events. Accepted delivery
    /// advances this history; refused attempts retain its cursor for replacement.
    /// Publication/source-event consistency belongs to the producer's exposed
    /// publication contract and must be checked by its connecting adapter.</summary>
    public Relation Deliver(Relation state, IElement publication,
        IElement producerEvent, IElement receiverEvent)
    {
        var delivery = new Relation(publication, new Relation(producerEvent, receiverEvent));
        var admitted = Authorize(state, producerEvent, receiverEvent,
            out var proof, out var blocked, out var unresolved);
        var after = state;
        if (admitted)
        {
            TryReadState(state, out _, out var history, out _);
            after = State(receiverEvent,
                StructuralConstraint.Closed([.. FiniteLaw.Entries(history), receiverEvent]),
                StructuralConstraint.Closed(producerEvent), new Relation(state, new Relation(delivery, proof)));
        }
        return new Relation(new Relation(new Relation(state, delivery), Surface),
            new Relation(new Relation(after, admitted ? StructuralConstraint.Closed(delivery) : StructuralConstraint.Closed()),
                new Relation(blocked, new Relation(unresolved, StructuralConstraint.Closed(proof)))));
    }

    public bool TryReadState(Relation state, out IElement receiverCursor,
        out Relation receiverHistory, out Relation producerSupport)
    {
        receiverCursor = null!;
        receiverHistory = producerSupport = null!;
        if (state.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            state.Right is not Relation { Left: Relation memory } ||
            memory.Left is not Relation history || memory.Right is not Relation producer ||
            !FiniteLaw.ClosedEntries(history, out var events) || events.Length == 0 ||
            !FiniteLaw.ClosedEntries(producer, out var sources) || sources.Length > 1 ||
            !FiniteLaw.SameParticipant(events[^1], context.Right)) return false;
        for (var i = 0; i < events.Length; i++)
            for (var j = 0; j < i; j++)
                if (FiniteLaw.SameParticipant(events[i], events[j])) return false;
        receiverCursor = context.Right;
        receiverHistory = history;
        producerSupport = producer;
        return true;
    }

    public bool TryReadDelivery(Relation receipt, out Relation before, out Relation after,
        out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        before = after = accepted = blocked = unresolved = null!;
        if (!ReadReceipt(receipt, out var prior, out _, out var current, out var admitted,
                out var refused, out var pending) ||
            !TryReadState(prior, out _, out _, out _) || !TryReadState(current, out _, out _, out _)) return false;
        before = prior; after = current; accepted = admitted; blocked = refused; unresolved = pending;
        return true;
    }

    /// <summary>Reads an accepted, connection-owned delivery and rechecks its
    /// native authorization. A receipt from another connection is not authority.</summary>
    public bool TryReadAccepted(Relation receipt, out IElement publication,
        out IElement producerEvent, out IElement receiverEvent)
    {
        publication = producerEvent = receiverEvent = null!;
        if (!ReadReceipt(receipt, out var before, out var delivery, out var after,
                out var accepted, out var blocked, out var unresolved) ||
            delivery.Right is not Relation events ||
            FiniteLaw.Entries(accepted) is not [var entry] || !ReferenceEquals(entry, delivery) ||
            FiniteLaw.Entries(blocked).Length != 0 || FiniteLaw.Entries(unresolved).Length != 0 ||
            !TryReadState(after, out var cursor, out var history, out var producer) ||
            !FiniteLaw.SameParticipant(cursor, events.Right) ||
            FiniteLaw.Entries(producer) is not [var actualSource] ||
            !FiniteLaw.SameParticipant(actualSource, events.Left) ||
            !Authorize(before, events.Left, events.Right, out _, out _, out _) ||
            !TryReadState(before, out _, out var previousHistory, out _)) return false;
        var trail = FiniteLaw.Entries(history);
        var priorTrail = FiniteLaw.Entries(previousHistory);
        if (trail.Length != priorTrail.Length + 1) return false;
        for (var i = 0; i < priorTrail.Length; i++)
            if (!FiniteLaw.SameParticipant(trail[i], priorTrail[i])) return false;
        publication = delivery.Left; producerEvent = events.Left; receiverEvent = events.Right;
        return true;
    }

    private Relation State(IElement receiverEvent, Relation history, Relation producer, IElement provenance) =>
        new(new Relation(Surface, receiverEvent), new Relation(new Relation(history, producer), provenance));

    private bool Authorize(Relation state, IElement producerEvent, IElement receiverEvent,
        out Relation proof, out Relation blocked, out Relation unresolved)
    {
        var evidence = new List<IElement> { Surface, new Relation(producerEvent, receiverEvent) };
        blocked = unresolved = StructuralConstraint.Closed();
        if (!TryReadState(state, out var prior, out var history, out var producer) ||
            !ReadSurface() || !ReadSchedule(ProducerSchedule, out var producerEdges) ||
            !ReadSchedule(ReceiverSchedule, out var receiverEdges) ||
            !ReadWindows(out var windows))
            return Pending(state, evidence, out proof, out unresolved);
        var trail = FiniteLaw.Entries(history);
        var historyEdges = new List<IElement>();
        for (var i = 1; i < trail.Length; i++)
        {
            var matches = receiverEdges.Where(edge => FiniteLaw.SameParticipant(edge.Left, trail[i - 1]) &&
                FiniteLaw.SameParticipant(edge.Right, trail[i])).ToArray();
            if (matches.Length == 0)
                return Pending(new Relation(history, ReceiverSchedule), evidence, out proof, out unresolved);
            historyEdges.AddRange(matches);
        }
        evidence.Add(new Relation(history, StructuralConstraint.Closed(historyEdges.ToArray())));
        if (FiniteLaw.Entries(history).Any(e => FiniteLaw.SameParticipant(e, receiverEvent)))
            return Refuse(new Relation(history, receiverEvent), evidence, out proof, out blocked);
        var previousSlot = new Placeholder();
        var nextSlot = new Placeholder();
        var receiverRead = StructuralConstraint.Match(new Relation(previousSlot, nextSlot),
            StructuralConstraint.Closed(previousSlot, nextSlot), ReceiverSchedule,
            new Relation(previousSlot, prior), new Relation(nextSlot, receiverEvent));
        evidence.Add(receiverRead);
        if (!receiverEdges.Any(e => FiniteLaw.SameParticipant(e.Left, prior) &&
                FiniteLaw.SameParticipant(e.Right, receiverEvent)) || !ContainsEvent(producerEdges, producerEvent))
            return Pending(new Relation(state, receiverRead), evidence, out proof, out unresolved);

        var sources = FiniteLaw.Entries(producer);
        if (sources.Length == 1 && !FiniteLaw.SameParticipant(sources[0], producerEvent))
        {
            var forward = Path(producerEdges, sources[0], producerEvent, out var forwardPath);
            var backward = Path(producerEdges, producerEvent, sources[0], out var backwardPath);
            evidence.Add(new Relation(forwardPath, backwardPath));
            if (backward) return Refuse(new Relation(producer, backwardPath), evidence, out proof, out blocked);
            if (!forward) return Pending(new Relation(producer, forwardPath), evidence, out proof, out unresolved);
        }
        else evidence.Add(new Relation(producer, producerEvent));

        var matching = windows.Where(window => FiniteLaw.SameParticipant(window.Left, producerEvent) &&
            FiniteLaw.Entries((Relation)window.Right).Any(e => FiniteLaw.SameParticipant(e, receiverEvent))).ToArray();
        var permitted = StructuralConstraint.Closed(matching);
        evidence.Add(permitted);
        if (matching.Length == 0)
            return Refuse(new Relation(new Relation(producerEvent, receiverEvent), Windows),
                evidence, out proof, out blocked);
        proof = StructuralConstraint.Closed(evidence.ToArray());
        return true;
    }

    private bool ReadSurface() =>
        Surface.Left is Relation { Left: Relation source, Right: Relation target } &&
        FiniteLaw.SameParticipant(source.Left, Producer) && ReferenceEquals(source.Right, ProducerSchedule) &&
        FiniteLaw.SameParticipant(target.Left, Receiver) && ReferenceEquals(target.Right, ReceiverSchedule) &&
        ReferenceEquals(Surface.Right, Windows);

    private bool ReadWindows(out Relation[] windows)
    {
        windows = [];
        if (!FiniteLaw.ClosedEntries(Windows, out var entries) || entries.Any(e =>
                e is not Relation { Right: Relation scope } || !FiniteLaw.ClosedEntries(scope, out _))) return false;
        windows = entries.Cast<Relation>().ToArray();
        return true;
    }

    private static bool ReadSchedule(Relation schedule, out Relation[] edges)
    {
        edges = [];
        if (!FiniteLaw.ClosedEntries(schedule, out var entries) || entries.Any(e => e is not Relation)) return false;
        edges = entries.Cast<Relation>().ToArray();
        // Strict causal order cannot contain an event returning to itself. The
        // finite traversal is a read of this exposed schedule, not a global clock.
        foreach (var edge in edges)
            if (FiniteLaw.SameParticipant(edge.Left, edge.Right) || Path(edges, edge.Right, edge.Left, out _)) return false;
        return true;
    }

    private static bool ContainsEvent(Relation[] edges, IElement identity) =>
        edges.Any(edge => FiniteLaw.SameParticipant(edge.Left, identity) || FiniteLaw.SameParticipant(edge.Right, identity));

    private static bool Path(Relation[] edges, IElement from, IElement to, out Relation proof)
    {
        var pending = new Queue<(IElement Current, Relation[] Path)>();
        var visited = new List<IElement> { from };
        pending.Enqueue((from, []));
        while (pending.TryDequeue(out var next))
            foreach (var edge in edges.Where(edge => FiniteLaw.SameParticipant(edge.Left, next.Current)))
            {
                Relation[] path = [.. next.Path, edge];
                if (FiniteLaw.SameParticipant(edge.Right, to))
                { proof = StructuralConstraint.Closed(path); return true; }
                if (visited.Any(e => FiniteLaw.SameParticipant(e, edge.Right))) continue;
                visited.Add(edge.Right);
                pending.Enqueue((edge.Right, path));
            }
        proof = StructuralConstraint.Closed();
        return false;
    }

    private static bool Pending(IElement reason, List<IElement> evidence,
        out Relation proof, out Relation unresolved)
    {
        proof = StructuralConstraint.Closed(evidence.ToArray());
        unresolved = StructuralConstraint.Closed(new Relation(reason, proof));
        return false;
    }

    private static bool Refuse(IElement reason, List<IElement> evidence,
        out Relation proof, out Relation blocked)
    {
        proof = StructuralConstraint.Closed(evidence.ToArray());
        blocked = StructuralConstraint.Closed(new Relation(reason, proof));
        return false;
    }

    private bool ReadReceipt(Relation receipt, out Relation before, out Relation delivery,
        out Relation after, out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        before = delivery = after = accepted = blocked = unresolved = null!;
        if (receipt.Left is not Relation { Left: Relation premise, Right: var surface } ||
            !ReferenceEquals(surface, Surface) || premise.Left is not Relation previous ||
            premise.Right is not Relation offered || offered.Right is not Relation ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation refusal } ||
            result.Left is not Relation current || result.Right is not Relation admitted ||
            refusal.Left is not Relation refused || refusal.Right is not Relation remainder ||
            remainder.Left is not Relation pending || remainder.Right is not Relation provenance ||
            !FiniteLaw.ClosedEntries(admitted, out _) || !FiniteLaw.ClosedEntries(refused, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _) || !FiniteLaw.ClosedEntries(provenance, out _)) return false;
        before = previous; delivery = offered; after = current;
        accepted = admitted; blocked = refused; unresolved = pending;
        return true;
    }
}
