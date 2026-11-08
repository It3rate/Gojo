using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A one-request comparison passage. Dispatch does not free its slot: only the
/// exact receipt for processing that occurrence admits another observation.
/// Transport timing is supplied by the experiment. A receipt says processed,
/// never settled, successful, fresh at delivery, or equal to the goal.
/// </summary>
public sealed class AcknowledgedComparisonChannel
{
    // contract = Closed(identity, receiver sensor, owner, comparison basis,
    //   comparison domain, occupancy port, attached capacity-one clamp,
    //   clock port, journal, acknowledgement port identity)
    // state = Closed(Closed(request), Closed(dispatch), Closed(receipt -> proof))
    // request = actual event -> actual publication
    // dispatch = request -> opaque delivery identity
    // receipt = (acknowledgement identity -> event) -> opaque receipt identity
    // proof = (dispatch -> actual consumed publication) -> actual receiver step
    // surface = contract -> (state -> (accepted event scope -> timeline))
    // A receiver sees only the publication. A sender sees only the opaque
    // receipt. The channel's private witness, rather than either message,
    // connects the actual receiver response to the request it processed.

    private readonly Relation contract;
    private readonly Carrier occupancy;
    private readonly Carrier clock;
    private readonly Carrier journal;
    private readonly IElement sensor;
    private readonly IElement owner;
    private readonly Carrier basis;
    private readonly Relation domain;
    private readonly IElement acknowledgement;
    private Relation state;
    private Relation acceptedEvents;
    private Relation timeline;

    public Relation Surface { get; private set; }
    public sealed record SnapshotRead(bool Waiting, bool Dispatched, bool ReceiptReady,
        long Occupancy, long Beat, IElement? Event, Relation? Packet, Relation? Receipt,
        Relation Surface);
    public sealed record OperationRead(bool Accepted, Relation? Packet, Relation? Receipt,
        Relation Witness);
    public sealed record RequestRead(bool Accepted, CoupledComparisonLink.Observation? Observation,
        Relation Witness);

    private AcknowledgedComparisonChannel(Relation surface)
    {
        if (surface is not { Left: Relation configuration, Right: Relation current } ||
            current.Left is not Relation stateSurface || current.Right is not Relation memory ||
            memory.Left is not Relation events || memory.Right is not Relation history ||
            !Closed(configuration, out var rows) || rows is not
                [Placeholder, IElement sensorValue, IElement ownerValue, Carrier comparisonBasis,
                 Relation comparisonDomain, Relation occupancyPort, Relation clamp,
                 Relation clockPort, Carrier historyCarrier, Placeholder receiptIdentity] ||
            !Port(occupancyPort, out var slot) || !Port(clockPort, out var time) ||
            !slot.IsAttached(clamp) || !Clamp(clamp, slot) ||
            !Closed(comparisonDomain, out var choices) || choices.Length != 3 ||
            choices.Where((item, i) => item is not CarrierLocation location ||
                !ReferenceEquals(location.Carrier, comparisonBasis) || location.Ticks != i - 1).Any() ||
            !Closed(events, out var occurrences) || occurrences.Any(item => item is not Placeholder) ||
            occurrences.Distinct().Count() != occurrences.Length ||
            !Closed(history, out var steps) || steps.Any(item => item is not Relation))
            throw new ArgumentException("Expected the exact one-request comparison passage.", nameof(surface));
        contract = configuration; occupancy = slot; clock = time; journal = historyCarrier;
        sensor = sensorValue; owner = ownerValue; basis = comparisonBasis; domain = comparisonDomain;
        acknowledgement = receiptIdentity; state = stateSurface; acceptedEvents = events; timeline = history;
        Surface = surface;
        ValidateState();
    }

    public static AcknowledgedComparisonChannel Create(CoupledExtentMachine receiver)
    {
        var slot = new Carrier(); var clamp = new Relation(Relation.Point(slot, 1), Relation.Point(slot, 1));
        slot.Attach(clamp);
        var time = new Carrier();
        var configuration = S(new Placeholder(), receiver.Sensor, receiver.Owner,
            receiver.ComparisonBasis, receiver.ComparisonDomain,
            new Relation(slot.ValueSlot, slot), clamp, new Relation(time.ValueSlot, time),
            new Carrier(), new Placeholder());
        return new(new Relation(configuration, new Relation(S(S(), S(), S()), new Relation(S(), S()))));
    }

    /// <summary>Samples a connected public comparison only when its one local
    /// slot is available. Refused requests do not secretly obtain fresh values.</summary>
    public RequestRead Request(CoupledComparisonLink link, bool available = true, bool ambiguous = false)
    {
        if (Snapshot().Waiting)
        {
            var held = Finish(Snapshot().Surface, state, false, null, null);
            return new(false, null, held.Witness);
        }
        var observation = link.Observe(available, ambiguous);
        var admission = Offer(observation.Packet);
        return new(admission.Accepted, observation, admission.Witness);
    }

    /// <summary>Offers an already sampled packet. A full slot or replay retains
    /// the offered packet as blocked evidence without replacing the held one.</summary>
    public OperationRead Offer(Relation packet)
    {
        var before = Snapshot().Surface;
        var admitted = occupancy.Value == 0 && ReadPacket(packet, out var occurrence) &&
            !Entries(acceptedEvents).Any(prior => Same(prior, occurrence));
        if (admitted)
        {
            ReadPacket(packet, out occurrence);
            state = S(S(new Relation(occurrence, packet)), S(), S());
            acceptedEvents = S([.. Entries(acceptedEvents), occurrence]);
            occupancy.Add(1);
        }
        return Finish(before, packet, admitted, admitted ? packet : null, null);
    }

    /// <summary>Releases the held observation once for delivery, while retaining
    /// its one-slot reservation until acknowledgement returns.</summary>
    public OperationRead Dispatch()
    {
        var before = Snapshot().Surface;
        var rows = Entries(state); var requests = Entries((Relation)rows[0]);
        var admitted = requests is [Relation] && Entries((Relation)rows[1]).Length == 0;
        Relation? packet = null;
        if (admitted)
        {
            var request = (Relation)requests[0]; packet = (Relation)request.Right;
            state = S(rows[0], S(new Relation(request, new Placeholder())), S());
        }
        return Finish(before, packet ?? (IElement)state, admitted, packet, null);
    }

    /// <summary>The receiver's local receipt service authenticates an actual
    /// retained transition. A gated publication may narrow/change the support,
    /// but must preserve the request's event and receiver contract. The outward
    /// receipt exposes none of the receiver's private transition or state.</summary>
    public OperationRead Receipt(CoupledExtentMachine receiver, CoupledExtentMachine.StepRead response)
    {
        var before = Snapshot().Surface;
        var rows = Entries(state); var dispatches = Entries((Relation)rows[1]);
        var receipts = Entries((Relation)rows[2]);
        var admitted = dispatches is [Relation] && receipts.Length == 0 &&
            Same(receiver.Sensor, sensor) && Same(receiver.Owner, owner) &&
            ReferenceEquals(receiver.ComparisonBasis, basis) && ReferenceEquals(receiver.ComparisonDomain, domain) &&
            response.ConsumedEvent && receiver.History.Any(step => ReferenceEquals(step, response.Surface)) &&
            TimeStepSequence.TryReadStep(response.Surface, out _, out var start, out var end,
                out _, out var transition, out _, out _) && end.Ticks - start.Ticks == 1 &&
            transition is Relation { Left: Relation { Left: Relation consumed } } &&
            ReadPacket(consumed, out var consumedEvent) && Same(consumedEvent, Snapshot().Event!);
        Relation? receipt = null;
        if (admitted)
        {
            TimeStepSequence.TryReadStep(response.Surface, out _, out _, out _, out _, out var actualTransition, out _, out _);
            var actualConsumed = (Relation)((Relation)((Relation)actualTransition).Left).Left;
            var dispatch = (Relation)dispatches[0];
            receipt = new Relation(new Relation(acknowledgement, Snapshot().Event!), new Placeholder());
            var proof = new Relation(new Relation(dispatch, actualConsumed), response.Surface);
            state = S(rows[0], rows[1], S(new Relation(receipt, proof)));
        }
        return Finish(before, response.Surface, admitted, null, receipt);
    }

    /// <summary>Only the exact issued receipt can release this active slot.
    /// Lost receipts leave a visible wait. There is no automatic timeout,
    /// cancellation, retransmission, or claim that the goal has been met.</summary>
    public OperationRead Acknowledge(Relation receipt)
    {
        var before = Snapshot().Surface;
        var issued = Snapshot().Receipt;
        var admitted = issued is not null && ReferenceEquals(issued, receipt);
        if (admitted) { state = S(S(), S(), S()); occupancy.Remove(1); }
        return Finish(before, receipt, admitted, null, admitted ? receipt : null);
    }

    public SnapshotRead Snapshot()
    {
        ValidateState();
        var rows = Entries(state); var requests = Entries((Relation)rows[0]);
        var dispatches = Entries((Relation)rows[1]); var receipts = Entries((Relation)rows[2]);
        var request = requests.FirstOrDefault() as Relation;
        var receipt = receipts.FirstOrDefault() is Relation evidence ? (Relation)evidence.Left : null;
        return new(request is not null, dispatches.Length == 1, receipt is not null,
            checked((long)occupancy.Value), checked((long)clock.Value), request?.Left,
            request?.Right as Relation, receipt,
            S(state, new CarrierLocation(occupancy, checked((long)occupancy.Value)),
                new CarrierLocation(clock, checked((long)clock.Value))));
    }

    public Relation ArchiveSurface() => Surface;
    public byte[] Encode() => GojoNumericFormat.Encode(Surface);
    public static AcknowledgedComparisonChannel Restore(Relation archive) => new(archive);
    public static AcknowledgedComparisonChannel Decode(ReadOnlySpan<byte> bytes) =>
        GojoNumericFormat.Decode(bytes) is [Relation surface] ? Restore(surface) :
            throw new ArgumentException("Expected one comparison-channel archive.", nameof(bytes));

    private OperationRead Finish(IElement before, IElement cause, bool admitted, Relation? packet, Relation? receipt)
    {
        var previousBeat = checked((long)clock.Value); clock.Add(1);
        var after = Snapshot().Surface;
        var acceptance = admitted ? S(cause) : S();
        var blocked = admitted ? S() : S(cause);
        var witness = TimeStepSequence.Step(journal, clock, previousBeat, checked((long)clock.Value),
            before, new Relation(contract, new Relation(cause, acceptance)), after, blocked);
        timeline = S([.. Entries(timeline), witness]);
        Surface = new Relation(contract, new Relation(state, new Relation(acceptedEvents, timeline)));
        return new(admitted, packet, receipt, witness);
    }

    private bool ReadPacket(Relation packet, out IElement occurrence)
    {
        occurrence = null!;
        return QualitativePublication.TryRead(packet, out var from, out var to, out occurrence,
            out var packetBasis, out var packetDomain, out _, out _) && occurrence is Placeholder &&
            Same(from, sensor) && Same(to, owner) && ReferenceEquals(packetBasis, basis) && ReferenceEquals(packetDomain, domain);
    }

    private void ValidateState()
    {
        if (!Closed(state, out var rows) || rows is not [Relation requests, Relation dispatches, Relation receipts] ||
            !Closed(requests, out var requestItems) || !Closed(dispatches, out var dispatchItems) ||
            !Closed(receipts, out var receiptItems) || requestItems.Length > 1 || dispatchItems.Length > 1 || receiptItems.Length > 1 ||
            occupancy.Tension != 0 || occupancy.Value != (ulong)requestItems.Length ||
            dispatchItems.Length > requestItems.Length || receiptItems.Length > dispatchItems.Length)
            throw new ArgumentException("The comparison passage's slot and connected pending state disagree.");
        if (requestItems.Length == 0) return;
        if (requestItems[0] is not Relation { Left: Placeholder occurrence, Right: Relation packet } request ||
            !ReadPacket(packet, out var packetEvent) || !Same(occurrence, packetEvent) ||
            !Entries(acceptedEvents).Any(prior => Same(prior, occurrence)))
            throw new ArgumentException("The held request has no accepted publication occurrence.");
        if (dispatchItems.Length == 0) return;
        if (dispatchItems[0] is not Relation { Right: Placeholder } dispatch || !ReferenceEquals(dispatch.Left, request))
            throw new ArgumentException("Dispatch must correspond to the actual held request.");
        if (receiptItems.Length == 0) return;
        if (receiptItems[0] is not Relation { Left: Relation receipt, Right: Relation proof } ||
            receipt is not { Left: Relation addressed, Right: Placeholder } ||
            !Same(addressed.Left, acknowledgement) || !Same(addressed.Right, occurrence) ||
            proof.Left is not Relation { Right: Relation consumed } delivery || !ReferenceEquals(delivery.Left, dispatch) ||
            !ReadPacket(consumed, out var consumedEvent) || !Same(consumedEvent, occurrence) ||
            proof.Right is not Relation step || !TimeStepSequence.TryReadStep(step, out _, out var start,
                out var end, out _, out var transition, out _, out _) || end.Ticks - start.Ticks != 1 ||
            transition is not Relation { Left: Relation execution } || !ReferenceEquals(execution.Left, consumed))
            throw new ArgumentException("The issued receipt is not connected to this consumed occurrence.");
    }

    private static bool Port(Relation port, out Carrier carrier)
    {
        carrier = null!;
        if (port is not { Left: CarrierLocation location, Right: Carrier at } ||
            at.IsUnknown || !ReferenceEquals(location.Carrier, at) || !location.SameTickRefAs(at.ValueSlot)) return false;
        carrier = at; return true;
    }
    private static bool Clamp(Relation clamp, Carrier carrier) => clamp.Left is Relation from && clamp.Right is Relation to &&
        Point(from, carrier, 1) && Point(to, carrier, 1);
    private static bool Point(Relation point, Carrier carrier, long ticks) =>
        point is { Left: CarrierLocation at, Right: Carrier to } && ReferenceEquals(to, carrier) &&
        ReferenceEquals(at.Carrier, carrier) && at.Ticks == ticks;
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] Entries(Relation scope) => Closed(scope, out var entries) ? entries :
        throw new ArgumentException("Expected a closed scope.");
    private static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
}
