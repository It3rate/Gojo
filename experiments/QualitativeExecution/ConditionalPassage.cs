using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Explicit finite packet participation through conditionally available passages.
/// Source inventory, receiver sites and consumed occurrences belong to one native
/// history. Observation never consumes packets. This is an experimental discrete
/// accounting law, not an automatic carrier-transfer or global active-head law.
/// </summary>
public sealed class ConditionalPassage
{
    // Surface = ((source -> target) -> (decision-port -> clock-port)) ->
    //           (opening-law -> capacities)
    // decision-port = (controller -> target) -> (basis -> command-domain)
    // clock-port = clock-source -> phase-recurrence-surface
    // capacity = passage -> Closed(ordered receiver sites)
    // allocation = packet -> (passage -> receiver-site)
    // state = (Surface -> event) ->
    //         (((source -> available) -> (target -> allocations)) -> (history -> evidence))
    // offer = (event -> prior-event) -> ((phase-state -> decision-publication) -> packets)
    // receipt = ((before -> offer) -> Surface) ->
    //           ((after -> accepted-packets) -> (blocked-packets -> (unresolved -> proofs)))
    public IElement Source { get; }
    public IElement Target { get; }
    public Relation DecisionPort { get; }
    public Relation ClockPort { get; }
    public PhaseRecurrence Clock { get; }
    public Relation OpeningLaw { get; }
    public Relation Capacities { get; }
    public Relation Surface { get; }

    public ConditionalPassage(IElement source, IElement target, Relation decisionPort,
        PhaseRecurrence clock, IElement clockSource, Relation openingLaw, Relation capacities)
    {
        Source = source; Target = target; DecisionPort = decisionPort; Clock = clock;
        ClockPort = new Relation(clockSource, clock.Surface); OpeningLaw = openingLaw; Capacities = capacities;
        Surface = new Relation(new Relation(new Relation(source, target), new Relation(decisionPort, ClockPort)),
            new Relation(openingLaw, capacities));
    }

    public static Relation Decision(IElement controller, IElement target, IElement basis, Relation commands) =>
        new(new Relation(controller, target), new Relation(basis, commands));

    public static Relation Capacity(IElement passage, params IElement[] sites) =>
        new(passage, StructuralConstraint.Closed(sites));

    public Relation Initial(IElement eventIdentity, Relation availablePackets) =>
        State(eventIdentity, availablePackets, StructuralConstraint.Closed(),
            StructuralConstraint.Closed(eventIdentity), StructuralConstraint.Closed());

    public Relation Offer(IElement eventIdentity, IElement priorEvent, Relation phaseState,
        Relation decision, Relation packets) =>
        new(new Relation(eventIdentity, priorEvent), new Relation(new Relation(phaseState, decision), packets));

    public Relation Step(Relation state, Relation offer)
    {
        var after = state;
        var accepted = new List<IElement>(); var blocked = new List<IElement>();
        var pending = new List<IElement>(); var proofs = new List<IElement>();
        if (!ReadContract(out var controller, out var basis, out var commands, out var capacityRows) ||
            !ReadState(state, capacityRows, out var previous, out var available, out var allocations, out var history) ||
            offer.Left is not Relation events || offer.Right is not Relation { Left: Relation premises, Right: Relation packets } ||
            premises.Left is not Relation phaseState || premises.Right is not Relation decision ||
            !FiniteLaw.DomainEntries(packets, out var incoming))
        { pending.Add(new Relation(state, offer)); return Receipt(); }

        if (!FiniteLaw.SameParticipant(events.Right, previous) ||
            FiniteLaw.Entries(history).Any(e => FiniteLaw.SameParticipant(e, events.Left)) ||
            !Clock.TryReadState(phaseState, out var clockSource, out var phaseEvent, out var phases) ||
            !FiniteLaw.SameParticipant(clockSource, ClockPort.Left) ||
            !FiniteLaw.SameParticipant(phaseEvent, events.Left) ||
            !Clock.TryReadHistory(phaseState, out var phaseHistory) ||
            !OrderedSubsequence(FiniteLaw.Entries(history), FiniteLaw.Entries(phaseHistory)) ||
            !QualitativePublication.TryRead(decision, out var owner, out var subject, out var decisionEvent,
                out var frame, out var domain, out var support, out _) ||
            !FiniteLaw.SameParticipant(owner, controller) || !FiniteLaw.SameParticipant(subject, Target) ||
            !FiniteLaw.SameParticipant(decisionEvent, events.Left) || !FiniteLaw.SameParticipant(frame, basis) ||
            !ReferenceEquals(domain, commands))
        { proofs.Add(new Relation(offer, Surface)); blocked.AddRange(incoming); return Receipt(); }

        // Union alone cannot certify permission: allowed OR denied has the same
        // nonempty union as allowed. Check each admitted atomic premise as well.
        var permission = FiniteLaw.Evaluate(OpeningLaw, support, phases);
        proofs.Add(permission);
        if (!FiniteLaw.ClosedEntries(support, out var actions) || actions.Length == 0 ||
            !FiniteLaw.ClosedEntries(phases, out var phaseValues) || phaseValues.Length == 0)
        { pending.Add(new Relation(packets, permission)); return Receipt(); }
        IElement? passage = null;
        var denied = false; var admitted = false; var incomplete = false;
        foreach (var action in actions)
            foreach (var phase in phaseValues)
            {
                var pair = FiniteLaw.Evaluate(OpeningLaw, StructuralConstraint.Closed(action), StructuralConstraint.Closed(phase));
                proofs.Add(pair);
                if (!FiniteLaw.TryReadResult(pair, out var outputs, out var residue, out var rows) ||
                    !FiniteLaw.ClosedEntries(outputs, out _) || !FiniteLaw.ClosedEntries(residue, out var rest) ||
                    rest.Length != 0 || FiniteLaw.Entries(rows).Length == 0)
                { incomplete = true; continue; }
                // Every supplied admissibility claim must agree. A denied row
                // cannot be erased by union with a second permitting row.
                foreach (var consumed in FiniteLaw.Entries(rows))
                {
                    if (consumed is not Relation row || !FiniteLaw.TryReadRow(row, out _, out _, out var rowSupport) ||
                        !FiniteLaw.ClosedEntries(rowSupport, out var values)) { incomplete = true; continue; }
                    if (values.Length == 0) denied = true;
                    else if (values.All(value => FiniteLaw.SameParticipant(value, values[0])))
                    {
                        admitted = true;
                        if (passage is null) passage = values[0];
                        else if (!FiniteLaw.SameParticipant(passage, values[0])) incomplete = true;
                    }
                    else incomplete = true;
                }
            }
        if (incomplete || denied && admitted)
        { pending.Add(new Relation(packets, permission)); return Receipt(); }

        var inventory = FiniteLaw.Entries(available).ToList();
        var occupied = FiniteLaw.Entries(allocations).ToList();
        IElement[] sites = [];
        if (admitted)
        {
            var capacity = capacityRows.Where(row => FiniteLaw.SameParticipant(row.Left, passage!)).ToArray();
            if (capacity.Length != 1 || !FiniteLaw.DomainEntries((Relation)capacity[0].Right, out sites))
            { pending.Add(new Relation(permission, Capacities)); return Receipt(); }
            proofs.Add(capacity[0]);
        }
        foreach (var packet in incoming)
        {
            var owned = inventory.FindIndex(p => FiniteLaw.SameParticipant(p, packet));
            var free = sites.FirstOrDefault(site => !occupied.Any(allocation => allocation is Relation
                { Right: Relation placement } && FiniteLaw.SameParticipant(placement.Right, site)));
            if (!admitted || owned < 0 || free is null)
            {
                blocked.Add(packet);
                proofs.Add(new Relation(packet, new Relation(permission, new Relation(available, Capacities))));
            }
            else
            {
                inventory.RemoveAt(owned);
                var allocation = new Relation(packet, new Relation(passage!, free));
                occupied.Add(allocation); accepted.Add(packet); proofs.Add(allocation);
            }
        }
        after = State(events.Left, StructuralConstraint.Closed(inventory.ToArray()),
            StructuralConstraint.Closed(occupied.ToArray()),
            StructuralConstraint.Closed([.. FiniteLaw.Entries(history), events.Left]),
            new Relation(state, new Relation(offer, StructuralConstraint.Closed(proofs.ToArray()))));
        return Receipt();

        Relation Receipt() => new(new Relation(new Relation(state, offer), Surface),
            new Relation(new Relation(after, StructuralConstraint.Closed(accepted.ToArray())),
                new Relation(StructuralConstraint.Closed(blocked.ToArray()),
                    new Relation(StructuralConstraint.Closed(pending.ToArray()), StructuralConstraint.Closed(proofs.ToArray())))));
    }

    public bool TryReadState(Relation state, out IElement eventIdentity, out Relation available, out Relation allocations)
    {
        eventIdentity = null!; available = allocations = null!;
        return ReadContract(out _, out _, out _, out var capacities) &&
            ReadState(state, capacities, out eventIdentity, out available, out allocations, out _);
    }

    /// <summary>Calculated accounting at the packet identity basis. Pending
    /// permission retains a concrete packet remainder without guessing a route.</summary>
    public bool TryReadAccounting(Relation receipt, out Relation incoming, out Relation accepted,
        out Relation blocked, out Relation remainder)
    {
        incoming = accepted = blocked = remainder = null!;
        if (!TryReadStep(receipt, out _, out var offer, out _, out var admitted, out var refused, out _) ||
            offer.Right is not Relation { Right: Relation packets } ||
            !FiniteLaw.DomainEntries(packets, out var offered)) return false;
        var yes = FiniteLaw.Entries(admitted); var no = FiniteLaw.Entries(refused);
        if (yes.Concat(no).Any(p => FiniteLaw.IndexOf(offered, p) < 0) ||
            yes.Any(p => FiniteLaw.IndexOf(no, p) >= 0)) return false;
        incoming = packets; accepted = admitted; blocked = refused;
        remainder = StructuralConstraint.Closed(offered.Where(p =>
            FiniteLaw.IndexOf(yes, p) < 0 && FiniteLaw.IndexOf(no, p) < 0).ToArray());
        return true;
    }

    public bool TryReadStep(Relation receipt, out Relation before, out Relation offer,
        out Relation after, out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        before = offer = after = accepted = blocked = unresolved = null!;
        if (receipt.Left is not Relation { Left: Relation premise, Right: var surface } ||
            !ReferenceEquals(surface, Surface) || premise.Left is not Relation old || premise.Right is not Relation request ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation remainder } ||
            result.Left is not Relation current || result.Right is not Relation admitted ||
            remainder.Left is not Relation refused || remainder.Right is not Relation rest ||
            rest.Left is not Relation pending || rest.Right is not Relation proof ||
            !FiniteLaw.DomainEntries(admitted, out _) || !FiniteLaw.DomainEntries(refused, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _) || !FiniteLaw.ClosedEntries(proof, out _)) return false;
        before = old; offer = request; after = current; accepted = admitted; blocked = refused; unresolved = pending;
        return true;
    }

    private Relation State(IElement eventIdentity, Relation available, Relation allocations, Relation history, IElement proof) =>
        new(new Relation(Surface, eventIdentity), new Relation(
            new Relation(new Relation(Source, available), new Relation(Target, allocations)), new Relation(history, proof)));

    private bool ReadContract(out IElement controller, out IElement basis, out Relation commands, out Relation[] capacities)
    {
        controller = basis = null!; commands = null!; capacities = [];
        if (Surface.Left is not Relation { Left: Relation endpoints, Right: Relation ports } ||
            !FiniteLaw.SameParticipant(endpoints.Left, Source) || !FiniteLaw.SameParticipant(endpoints.Right, Target) ||
            !ReferenceEquals(ports.Left, DecisionPort) || !ReferenceEquals(ports.Right, ClockPort) ||
            !ReferenceEquals(ClockPort.Right, Clock.Surface) ||
            Surface.Right is not Relation mechanism || !ReferenceEquals(mechanism.Left, OpeningLaw) ||
            !ReferenceEquals(mechanism.Right, Capacities) ||
            DecisionPort.Left is not Relation owner || !FiniteLaw.SameParticipant(owner.Right, Target) ||
            DecisionPort.Right is not Relation { Right: Relation domain } contract ||
            !FiniteLaw.TryRead(OpeningLaw, out var left, out var right, out var outputs, out _) ||
            !ReferenceEquals(left, domain) || !ReferenceEquals(right, Clock.Domain) ||
            !FiniteLaw.ClosedEntries(Capacities, out var entries)) return false;
        var rows = entries.OfType<Relation>().ToArray();
        if (rows.Length != entries.Length || rows.Any(row => row.Right is not Relation sites ||
                !FiniteLaw.DomainEntries(sites, out _) || FiniteLaw.IndexOf(FiniteLaw.Entries(outputs), row.Left) < 0)) return false;
        for (var i = 0; i < rows.Length; i++)
            for (var j = 0; j < i; j++) if (FiniteLaw.SameParticipant(rows[i].Left, rows[j].Left)) return false;
        controller = owner.Left; basis = contract.Left; commands = domain; capacities = rows;
        return true;
    }

    private bool ReadState(Relation state, Relation[] capacities, out IElement eventIdentity,
        out Relation available, out Relation allocations, out Relation history)
    {
        eventIdentity = null!; available = allocations = history = null!;
        if (state.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            state.Right is not Relation { Left: Relation inventories, Right: Relation evidence } ||
            inventories.Left is not Relation source || !FiniteLaw.SameParticipant(source.Left, Source) ||
            inventories.Right is not Relation target || !FiniteLaw.SameParticipant(target.Left, Target) ||
            source.Right is not Relation stock || !FiniteLaw.DomainEntries(stock, out var packets) ||
            target.Right is not Relation placed || !FiniteLaw.ClosedEntries(placed, out var occupied) ||
            evidence.Left is not Relation trail || !FiniteLaw.DomainEntries(trail, out var events) ||
            events.Length == 0 || !FiniteLaw.SameParticipant(events[^1], context.Right)) return false;
        var seenPackets = packets.ToList(); var seenSites = new List<IElement>();
        foreach (var entry in occupied)
        {
            if (entry is not Relation { Right: Relation placement } allocation ||
                seenPackets.Any(p => FiniteLaw.SameParticipant(p, allocation.Left)) ||
                seenSites.Any(site => FiniteLaw.SameParticipant(site, placement.Right)) ||
                !capacities.Any(row => FiniteLaw.SameParticipant(row.Left, placement.Left) &&
                    FiniteLaw.IndexOf(FiniteLaw.Entries((Relation)row.Right), placement.Right) >= 0)) return false;
            seenPackets.Add(allocation.Left); seenSites.Add(placement.Right);
        }
        eventIdentity = context.Right; available = stock; allocations = placed; history = trail;
        return true;
    }

    private static bool OrderedSubsequence(IElement[] selected, IElement[] history)
    {
        var next = 0;
        foreach (var occurrence in history)
            if (next < selected.Length && FiniteLaw.SameParticipant(selected[next], occurrence)) next++;
        return next == selected.Length;
    }
}
