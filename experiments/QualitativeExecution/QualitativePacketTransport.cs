using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>Identity-preserving packet ownership through declared passages.
/// Storage sites provide local/shared capacity. Permission, questions, motors
/// and delayed scheduling belong to the connected caller, not this ledger.</summary>
public sealed class QualitativePacketTransport
{
    // capacity = actual port -> Closed(actual storage sites)
    // route = actual source port -> actual target port
    // placement = actual packet -> (actual port -> actual storage site)
    // state = (Surface -> event) ->
    //         (placements -> (imported packets -> (event history -> provenance)))
    // request = ((boundary -> (event -> prior-event)) -> input) -> evidence
    // import input = actual port -> Closed(fresh packets)
    // movement input = Closed(actual packet -> actual declared route)
    // receipt = ((before -> request) -> Surface) ->
    //           ((after -> accepted packets) ->
    //            (blocked packets -> (remainder -> (unresolved -> proofs))))
    // Every accepted operation is one atomic placement change. Released sites
    // may be reused within that batch; event order never invents elapsed time.
    public Relation Capacities { get; }
    public Relation Routes { get; }
    public Relation ImportBoundary { get; }
    public Relation TransferBoundary { get; }
    public Relation Surface { get; }
    public Relation State { get; private set; }
    public Relation ImportedPackets { get { ReadCurrent(out _, out var imported, out _); return imported; } }
    public Relation Timeline { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();

    public QualitativePacketTransport(Relation capacities, Relation routes, IElement? evidence = null)
    {
        Capacities = capacities; Routes = routes;
        ImportBoundary = new Relation(new Placeholder(), capacities);
        TransferBoundary = new Relation(capacities, routes);
        Surface = new Relation(new Relation(ImportBoundary, TransferBoundary),
            new Relation(new Relation(capacities, routes), evidence ?? S()));
        var initial = new Placeholder(); State = MakeState(initial, S(), S(), S(initial), S());
    }

    /// <summary>Imports an entire fresh batch or retains the entire rejected
    /// offer. An already imported identity cannot be imported again, even after
    /// legal transfers have returned it to an earlier port.</summary>
    public Relation Import(IElement eventIdentity, IElement port, Relation packets, IElement? evidence = null)
    {
        var receipt = Build(State, Request(ImportBoundary, eventIdentity, new Relation(port, packets), evidence ?? S()));
        TryCommit(receipt); return receipt;
    }

    /// <summary>Pure preflight against the current native head. A batch removes
    /// every selected source placement before allocating target sites. Any
    /// refused move leaves all packets at their original owners.</summary>
    public Relation Prepare(IElement eventIdentity, Relation moves, IElement evidence) =>
        Build(State, Request(TransferBoundary, eventIdentity, moves, evidence));

    public Relation Move(IElement eventIdentity, Relation moves, IElement? evidence = null)
    { var receipt = Prepare(eventIdentity, moves, evidence ?? S()); TryCommit(receipt); return receipt; }

    public bool TryCommit(Relation receipt)
    {
        if (!TryReadStep(receipt, out var before, out _, out _, out _, out _, out _) ||
            !ReferenceEquals(before, State) || !TryReadAccepted(receipt, out var after, out _))
        { Unresolved = Append(Unresolved, receipt); return false; }
        State = after; Timeline = Append(Timeline, receipt); return true;
    }

    public bool TryReadState(Relation state, out IElement eventIdentity, out Relation placements,
        out Relation imported, out Relation history)
    {
        eventIdentity = null!; placements = imported = history = null!;
        if (!Contract(out var capacities) || state.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            state.Right is not Relation { Left: Relation held, Right: Relation { Left: Relation packets, Right: Relation tail } } ||
            tail.Left is not Relation events || !Domain(packets) || !Domain(events) || E(events).Length == 0 ||
            !Same(E(events)[^1], context.Right) || !Closed(held) || E(held).Length != E(packets).Length) return false;
        var seen = new List<IElement>(); var occupied = new List<IElement>();
        foreach (var item in E(held))
        {
            if (item is not Relation { Right: Relation place } allocation || !E(packets).Any(packet => Same(packet, allocation.Left)) ||
                seen.Any(packet => Same(packet, allocation.Left)) || occupied.Any(site => Same(site, place.Right)) ||
                !capacities.Any(row => Same(row.Left, place.Left) && E((Relation)row.Right).Any(site => Same(site, place.Right)))) return false;
            seen.Add(allocation.Left); occupied.Add(place.Right);
        }
        eventIdentity = context.Right; placements = held; imported = packets; history = events; return true;
    }

    public bool TryReadStep(Relation receipt, out Relation before, out Relation request, out Relation after,
        out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        before = request = after = accepted = blocked = unresolved = null!;
        if (!ReadReceipt(receipt, out var previous, out var asked, out var current, out var yes, out var no, out _, out var pending, out _)) return false;
        before = previous; request = asked; after = current; accepted = yes; blocked = no; unresolved = pending; return true;
    }

    /// <summary>Recomputes the whole local move/import law. Native proofs must
    /// connect the exact source head, request, source placements and output
    /// placements; copied success flags cannot authorize a commit.</summary>
    public bool TryReadAccepted(Relation receipt, out Relation after, out Relation movedPackets)
    {
        after = movedPackets = null!;
        if (!ReadReceipt(receipt, out var before, out var request, out var actualAfter, out var accepted, out var blocked,
                out var remainder, out var pending, out var proofs) || E(blocked).Length != 0 || E(remainder).Length != 0 || E(pending).Length != 0 ||
            !ReadRequest(request, out _, out var eventIdentity, out _, out _) ||
            !TryReadState(actualAfter, out var actualEvent, out var placements, out var imported, out var history) ||
            !ReferenceEquals(actualEvent, eventIdentity)) return false;
        var plan = PlanChange(before, request);
        if (!plan.Admitted || !SameEntries(accepted, plan.Accepted) || !SameEntries(imported, plan.Imported) ||
            !TryReadState(before, out _, out var priorPlacements, out _, out var priorHistory) || !Appended(priorHistory, history, eventIdentity) ||
            E(placements).Length != E(plan.Placements).Length ||
            actualAfter.Right is not Relation { Right: Relation { Right: Relation { Right: Relation provenance } } } ||
            !ReferenceEquals(provenance.Left, before) || provenance.Right is not Relation application ||
            !ReferenceEquals(application.Left, request) || !ReferenceEquals(application.Right, proofs)) return false;
        var replacements = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        var expected = E(plan.Placements); var actual = E(placements);
        for (var i = 0; i < expected.Length; i++)
        {
            if (expected[i] is not Relation { Right: Relation a } first || actual[i] is not Relation { Right: Relation b } second ||
                !ReferenceEquals(first.Left, second.Left) || !ReferenceEquals(a.Left, b.Left) || !ReferenceEquals(a.Right, b.Right) ||
                E(priorPlacements).Any(row => ReferenceEquals(row, first)) && !ReferenceEquals(first, second)) return false;
            replacements[first] = second;
        }
        var anchors = new HashSet<IElement>(ReferenceEqualityComparer.Instance); Anchor(Surface); Anchor(before); Anchor(request);
        if (E(proofs).Length != E(plan.Proofs).Length || !E(plan.Proofs).Zip(E(proofs)).All(pair => Equivalent(pair.First, pair.Second))) return false;
        after = actualAfter; movedPackets = accepted; return true;

        void Anchor(IElement item)
        {
            var pending = new Stack<IElement>(); pending.Push(item);
            while (pending.TryPop(out var current))
                if (anchors.Add(current) && current is Relation row) { pending.Push(row.Left); pending.Push(row.Right); }
        }
        bool Equivalent(IElement wanted, IElement offered)
        {
            if (anchors.Contains(wanted)) return ReferenceEquals(wanted, offered);
            if (replacements.TryGetValue(wanted, out var replacement)) return ReferenceEquals(replacement, offered);
            return ReferenceEquals(wanted, offered) || wanted is Relation x && offered is Relation y && Equivalent(x.Left, y.Left) && Equivalent(x.Right, y.Right);
        }
    }

    public bool TryReadAccounting(Relation receipt, out Relation incoming, out Relation accepted,
        out Relation blocked, out Relation remainder)
    {
        incoming = accepted = blocked = remainder = null!;
        if (!ReadReceipt(receipt, out _, out var request, out _, out var yes, out var no, out var rest, out _, out _) ||
            !ReadRequest(request, out var boundary, out _, out _, out var input)) return false;
        incoming = Incoming(boundary, input); accepted = yes; blocked = no; remainder = rest;
        if (!Closed(incoming) || !Closed(rest)) return false;
        var left = E(incoming).ToList();
        foreach (var packet in E(yes).Concat(E(no)).Concat(E(rest)))
        { var index = left.FindIndex(item => Same(item, packet)); if (index < 0) return false; left.RemoveAt(index); }
        return left.Count == 0;
    }

    public Relation PortPackets(IElement port) => PortPackets(State, port);
    public Relation PortPackets(Relation state, IElement port) => TryReadState(state, out _, out var placements, out _, out _) &&
        Contract(out var capacities) && capacities.Any(row => Same(row.Left, port))
            ? S(E(placements).Cast<Relation>().Where(row => Same(((Relation)row.Right).Left, port)).Select(row => row.Left).ToArray())
            : StructuralConstraint.Open(new Relation(state, port));

    public Relation ReadPort(Relation state, IElement port) => new(new Relation(Surface, new Relation(state, port)), PortPackets(state, port));

    public Relation Account()
    {
        if (!TryReadState(State, out _, out var placements, out var imported, out _))
            return new Relation(new Relation(Surface, State), StructuralConstraint.Open(State));
        return new Relation(new Relation(Surface, imported), new Relation(placements, S()));
    }

    private Relation Request(Relation boundary, IElement eventIdentity, IElement input, IElement evidence)
    {
        TryReadState(State, out var prior, out _, out _, out _);
        return new Relation(new Relation(new Relation(boundary, new Relation(eventIdentity, prior ?? new Placeholder())), input), evidence);
    }

    private Relation Build(Relation before, Relation request)
    {
        var plan = PlanChange(before, request); var after = before;
        if (plan.Admitted && ReadRequest(request, out _, out var occurrence, out _, out _) &&
            TryReadState(before, out _, out _, out _, out var history))
            after = MakeState(occurrence, plan.Placements, plan.Imported, Append(history, occurrence),
                new Relation(before, new Relation(request, plan.Proofs)));
        return new Relation(new Relation(new Relation(before, request), Surface), new Relation(new Relation(after, plan.Accepted),
            new Relation(plan.Blocked, new Relation(plan.Remainder, new Relation(plan.Unresolved, plan.Proofs)))));
    }

    private sealed record Plan(bool Admitted, Relation Placements, Relation Imported, Relation Accepted,
        Relation Blocked, Relation Remainder, Relation Unresolved, Relation Proofs);

    private Plan PlanChange(Relation before, Relation request)
    {
        var none = S();
        if (!ReadRequest(request, out var boundary, out var occurrence, out var predecessor, out var input))
            return new(false, none, none, none, none, none, S(new Relation(before, request)), S());
        var incoming = Incoming(boundary, input);
        if (!Contract(out var capacities) || !TryReadState(before, out var previous, out var placements, out var imported, out var history))
            return new(false, none, none, none, none, incoming, S(new Relation(before, request)), S());
        if (!Domain(incoming)) return Refuse(new Relation(request, incoming), unresolved: true);
        if (!Same(previous, predecessor) || E(history).Any(prior => Same(prior, occurrence))) return Refuse(new Relation(request, history));
        var active = E(placements).Cast<Relation>().ToList(); var imports = E(imported).ToList(); var proofs = new List<IElement>();
        if (ReferenceEquals(boundary, ImportBoundary))
        {
            if (input is not Relation intake || !capacities.Any(row => Same(row.Left, intake.Left)) ||
                E(incoming).Any(packet => imports.Any(prior => Same(prior, packet)))) return Refuse(new Relation(request, imported));
            foreach (var packet in E(incoming))
            {
                var site = Free(intake.Left, active, capacities);
                if (site is null) return Refuse(new Relation(request, Capacities));
                var placement = new Relation(packet, new Relation(intake.Left, site)); active.Add(placement); imports.Add(packet);
                proofs.Add(new Relation(intake, placement));
            }
        }
        else if (ReferenceEquals(boundary, TransferBoundary))
        {
            if (input is not Relation moves || !Closed(moves)) return Refuse(request, unresolved: true);
            var changes = new List<(Relation Move, Relation Route, Relation Source)>();
            foreach (var item in E(moves))
            {
                if (item is not Relation { Right: Relation route } move || !E(Routes).Any(actual => ReferenceEquals(actual, route)))
                    return Refuse(new Relation(request, item));
                var sources = active.Where(row => Same(row.Left, move.Left) && Same(((Relation)row.Right).Left, route.Left)).ToArray();
                if (sources.Length != 1) return Refuse(new Relation(move, placements));
                changes.Add((move, route, sources[0]));
            }
            foreach (var change in changes) active.Remove(change.Source);
            foreach (var change in changes)
            {
                var site = Free(change.Route.Right, active, capacities);
                if (site is null) return Refuse(new Relation(change.Move, Capacities));
                var placement = new Relation(change.Move.Left, new Relation(change.Route.Right, site)); active.Add(placement);
                proofs.Add(new Relation(change.Move, new Relation(change.Source, placement)));
            }
        }
        else return Refuse(request);
        return new(true, S(active.Cast<IElement>().ToArray()), S(imports.ToArray()), incoming, none, none, none, S(proofs.ToArray()));

        Plan Refuse(IElement reason, bool unresolved = false) => new(false, placements, imported, none,
            unresolved ? none : incoming, unresolved ? incoming : none, unresolved ? S(reason) : none, S(new Relation(request, reason)));
    }

    private IElement? Free(IElement port, List<Relation> placements, Relation[] capacities) =>
        capacities.Where(row => Same(row.Left, port)).SelectMany(row => E((Relation)row.Right))
            .FirstOrDefault(site => !placements.Any(row => Same(((Relation)row.Right).Right, site)));

    private Relation Incoming(IElement boundary, IElement input)
    {
        if (ReferenceEquals(boundary, ImportBoundary) && input is Relation { Right: Relation packets }) return packets;
        if (ReferenceEquals(boundary, TransferBoundary) && input is Relation moves)
            return Scope(Closed(moves), E(moves).Select(item => item is Relation row ? row.Left : item).ToArray());
        return StructuralConstraint.Open(input);
    }

    private bool Contract(out Relation[] capacities)
    {
        capacities = [];
        if (!Closed(Capacities) || !Domain(Routes) || E(Capacities).Any(item => item is not Relation { Right: Relation sites } || !Domain(sites))) return false;
        var rows = E(Capacities).Cast<Relation>().ToArray();
        if (rows.Select(row => row.Left).Where((port, index) => rows.Take(index).Any(row => Same(row.Left, port))).Any() ||
            E(Routes).Any(item => item is not Relation route || !rows.Any(row => Same(row.Left, route.Left)) || !rows.Any(row => Same(row.Left, route.Right)))) return false;
        capacities = rows; return true;
    }

    private bool ReadRequest(Relation request, out IElement boundary, out IElement occurrence, out IElement predecessor, out IElement input)
    {
        boundary = occurrence = predecessor = input = null!;
        if (request.Left is not Relation { Left: Relation context } body || context.Right is not Relation events ||
            !ReferenceEquals(context.Left, ImportBoundary) && !ReferenceEquals(context.Left, TransferBoundary)) return false;
        boundary = context.Left; occurrence = events.Left; predecessor = events.Right; input = body.Right; return true;
    }

    private bool ReadReceipt(Relation receipt, out Relation before, out Relation request, out Relation after, out Relation accepted,
        out Relation blocked, out Relation remainder, out Relation unresolved, out Relation proofs)
    {
        before = request = after = accepted = blocked = remainder = unresolved = proofs = null!;
        if (receipt.Left is not Relation { Left: Relation premise } context || !ReferenceEquals(context.Right, Surface) ||
            premise.Left is not Relation previous || premise.Right is not Relation asked ||
            receipt.Right is not Relation { Left: Relation outcome, Right: Relation refusal } ||
            outcome.Left is not Relation current || outcome.Right is not Relation yes || refusal.Left is not Relation no ||
            refusal.Right is not Relation { Left: Relation rest, Right: Relation { Left: Relation pending, Right: Relation evidence } } ||
            !Closed(yes) || !Closed(no) || !StructuralConstraint.TryReadScope(rest, out _, out _, out _) || !Closed(pending) || !Closed(evidence)) return false;
        before = previous; request = asked; after = current; accepted = yes; blocked = no; remainder = rest; unresolved = pending; proofs = evidence; return true;
    }

    private Relation MakeState(IElement occurrence, Relation placements, Relation imported, Relation history, IElement proof) =>
        new(new Relation(Surface, occurrence), new Relation(placements, new Relation(imported, new Relation(history, proof))));
    private void ReadCurrent(out Relation placements, out Relation imported, out Relation history)
    {
        if (!TryReadState(State, out _, out placements, out imported, out history))
            throw new InvalidOperationException("The current transport state has no valid ownership contract.");
    }
    private static bool Appended(Relation before, Relation after, IElement last) => E(after).Length == E(before).Length + 1 &&
        E(before).Zip(E(after)).All(pair => ReferenceEquals(pair.First, pair.Second)) && ReferenceEquals(E(after)[^1], last);
    private static bool SameEntries(Relation first, Relation second) => Closed(first) && Closed(second) && E(first).Length == E(second).Length &&
        E(first).Zip(E(second)).All(pair => ReferenceEquals(pair.First, pair.Second));
    private static bool Domain(Relation scope) => Closed(scope) && !E(scope).Where((item, index) => E(scope).Take(index).Any(prior => Same(prior, item))).Any();
    private static bool Same(IElement first, IElement second) => FiniteLaw.SameParticipant(first, second);
    private static Relation Append(Relation scope, IElement value) => S([.. E(scope), value]);
    private static Relation Scope(bool complete, params IElement[] values) => complete ? S(values) : StructuralConstraint.Open(values);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
