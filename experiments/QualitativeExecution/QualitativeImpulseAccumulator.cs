using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Experimental, uncalibrated discrete packet occupancy. This is an explicit
/// intake/release ledger, not continuous carrier tension, energy measurement,
/// automatic firing or a claim about conservation outside its declared port.
/// </summary>
public sealed class QualitativeImpulseAccumulator
{
    // Surface = ((Owner -> Receiver) -> (InputPort -> DrainPort)) ->
    //           ((Sites -> ThresholdSites) -> (ReadinessDomain -> Law))
    // allocation = actual-packet -> actual-site
    // state = (Surface -> event) -> (allocations -> (used-packets -> (history -> proof)))
    // coverage = (Surface -> state) -> (satisfied-allocations -> (missing-sites -> unresolved))
    // request = ((port -> (event -> prior-event)) -> packets) -> evidence
    // receipt = ((before -> request) -> Surface) ->
    //           ((after -> accepted) -> (blocked -> (remainder -> (drained -> (unresolved -> proofs)))))
    // An intake is a declared import through Owner -> Receiver. A drain reverses
    // that passage and retains the actual selecting question. Used packet IDs
    // remain in native state after release. There is no hidden packet registry.
    public IElement Owner { get; }
    public IElement Receiver { get; } = new Placeholder();
    public Relation Sites { get; }
    public Relation ThresholdSites { get; }
    public Relation InputPort { get; }
    public Relation DrainPort { get; }
    public Relation Ready { get; }
    public Relation Waiting { get; }
    public Relation ReadinessDomain { get; }
    public Relation Law { get; }
    public Relation Surface { get; }
    public Relation State { get; private set; }
    private Relation? observation;
    private Relation? readinessRead;

    public QualitativeImpulseAccumulator(IElement owner, Relation sites, Relation thresholdSites)
    {
        Owner = owner; Sites = sites; ThresholdSites = thresholdSites;
        InputPort = new Relation(owner, Receiver); DrainPort = InputPort.Reverse();
        // The two boundary roles are connected to the actual required scope:
        // coverage of the whole required surface, or its incomplete complement.
        Ready = new Relation(thresholdSites, thresholdSites);
        Waiting = new Relation(thresholdSites, StructuralConstraint.Open());
        ReadinessDomain = S(Waiting, Ready);
        Law = FiniteLaw.Create(ReadinessDomain, ReadinessDomain, ReadinessDomain,
            FiniteLaw.Row(Ready, Ready, S(Ready)), FiniteLaw.Row(Ready, Waiting, S(Waiting)),
            FiniteLaw.Row(Waiting, Ready, S(Waiting)), FiniteLaw.Row(Waiting, Waiting, S(Waiting)));
        Surface = new Relation(new Relation(new Relation(owner, Receiver), new Relation(InputPort, DrainPort)),
            new Relation(new Relation(sites, thresholdSites), new Relation(ReadinessDomain, Law)));
        var initialEvent = new Placeholder();
        State = MakeState(initialEvent, S(), S(), S(initialEvent), S());
    }

    /// <summary>Admits each fresh source packet into one free declared site.
    /// Incomplete/duplicate offers remain wholly pending; capacity and previously
    /// accepted packet identities are explicit reasons for blocking.</summary>
    public Relation Receive(IElement eventIdentity, Relation packets)
    {
        var before = State;
        ReadState(before, out var previous, out var allocated, out var used, out var history);
        var request = Request(InputPort, eventIdentity, previous, packets, S());
        var accepted = new List<IElement>(); var blocked = new List<IElement>();
        var pending = new List<IElement>(); var proofs = new List<IElement>();
        var remainder = packets;
        if (!Contract(out var sites, out _) || !FiniteLaw.DomainEntries(packets, out var incoming))
        { pending.Add(new Relation(request, Surface)); return Finish(); }
        if (Member(FiniteLaw.Entries(history), eventIdentity))
        { blocked.AddRange(incoming); remainder = S(); proofs.Add(new Relation(eventIdentity, history)); return Finish(); }
        var placements = FiniteLaw.Entries(allocated).ToList(); var seen = FiniteLaw.Entries(used).ToList();
        foreach (var packet in incoming)
        {
            var free = sites.FirstOrDefault(site => !placements.Cast<Relation>().Any(row => Same(row.Right, site)));
            if (Member(seen, packet) || free is null)
            { blocked.Add(packet); proofs.Add(new Relation(packet, new Relation(used, allocated))); }
            else
            {
                var placement = new Relation(packet, free);
                placements.Add(placement); seen.Add(packet); accepted.Add(packet); proofs.Add(placement);
            }
        }
        remainder = S();
        SetState(eventIdentity, S(placements.ToArray()), S(seen.ToArray()), history, before, request, proofs);
        return Finish();

        Relation Finish() => Receipt(before, request, S(accepted.ToArray()), S(blocked.ToArray()), remainder,
            S(), S(pending.ToArray()), S(proofs.ToArray()));
    }

    /// <summary>A non-draining publication at the current state/event. The
    /// satisfied allocations and missing required sites justify the support.</summary>
    public Relation Observe()
    {
        if (observation is not null) return observation;
        ReadState(State, out var eventIdentity, out var allocated, out _, out _);
        var satisfied = new List<IElement>(); var missing = new List<IElement>(); var pending = new List<IElement>();
        if (!Contract(out _, out var required)) pending.Add(new Relation(ThresholdSites, Sites));
        else foreach (var site in required)
        {
            var placement = FiniteLaw.Entries(allocated).Cast<Relation>().FirstOrDefault(row => Same(row.Right, site));
            if (placement is null) missing.Add(site); else satisfied.Add(placement);
        }
        var coverage = new Relation(new Relation(Surface, State), new Relation(S(satisfied.ToArray()),
            new Relation(S(missing.ToArray()), S(pending.ToArray()))));
        var support = pending.Count == 0 ? S(missing.Count == 0 ? Ready : Waiting) : StructuralConstraint.Open();
        observation = QualitativePublication.Create(Owner, Receiver, eventIdentity, Surface, ReadinessDomain, support, coverage);
        return observation;
    }

    public Relation Ask(Relation routes, IElement refinementPort) =>
        QualitativeQuestion.Ask(CurrentRead(), routes, refinementPort);

    /// <summary>Explicit release selected by a current complete question. The
    /// question is recomputed through its actual routes; a copied/forged answer
    /// cannot authorize release. Only required-site packets drain; surplus stays.
    /// Any unique routed answer may describe the action, but Ready is required.</summary>
    public Relation Fire(IElement eventIdentity, Relation question)
    {
        var before = State;
        ReadState(before, out var previous, out var allocated, out var used, out var history);
        var publication = Observe();
        TryReadReadiness(publication, out _, out var satisfied, out _, out _);
        var incoming = S(FiniteLaw.Entries(satisfied).Cast<Relation>().Select(row => row.Left).ToArray());
        var request = Request(DrainPort, eventIdentity, previous, incoming, new Relation(publication, question));
        var accepted = S(); var blocked = S(); var remainder = incoming; var pending = S();
        var proofs = new List<IElement> { publication, question };
        if (Member(FiniteLaw.Entries(history), eventIdentity))
        { blocked = incoming; remainder = S(); proofs.Add(new Relation(eventIdentity, history)); return Finish(); }
        if (!CurrentQuestion(question, out var fresh))
        { pending = S(new Relation(question, CurrentRead())); return Finish(); }
        proofs.Add(fresh);
        if (!QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _) ||
            !FiniteLaw.ClosedEntries(support, out var outcomes) || outcomes.Length != 1)
        { pending = S(publication); return Finish(); }
        if (!Same(outcomes[0], Ready))
        {
            blocked = incoming; remainder = S();
            SetState(eventIdentity, allocated, used, history, before, request, proofs);
            return Finish();
        }
        accepted = incoming; remainder = S();
        var remaining = FiniteLaw.Entries(allocated).Where(row => row is Relation placement &&
            !Member(FiniteLaw.Entries(satisfied), placement)).ToArray();
        SetState(eventIdentity, S(remaining), used, history, before, request, proofs);
        return Finish();

        Relation Finish() => Receipt(before, request, accepted, blocked, remainder, accepted, pending, S(proofs.ToArray()));
    }

    public bool TryReadState(out IElement eventIdentity, out Relation allocations,
        out Relation usedPackets, out Relation history) => ReadState(State, out eventIdentity, out allocations, out usedPackets, out history);

    public bool TryReadState(Relation state, out IElement eventIdentity, out Relation allocations,
        out Relation usedPackets, out Relation history) => ReadState(state, out eventIdentity, out allocations, out usedPackets, out history);

    /// <summary>Historical inspection, not authorization to change this machine.</summary>
    public bool TryReadReadiness(Relation publication, out Relation state, out Relation satisfied,
        out Relation missing, out Relation unresolved)
    {
        state = satisfied = missing = unresolved = null!;
        if (!QualitativePublication.TryRead(publication, out var owner, out var receiver, out var eventIdentity,
                out var basis, out var domain, out var support, out var evidence) || !Same(owner, Owner) || !Same(receiver, Receiver) ||
            !ReferenceEquals(basis, Surface) || !ReferenceEquals(domain, ReadinessDomain) ||
            evidence is not Relation { Left: Relation source, Right: Relation result } || !ReferenceEquals(source.Left, Surface) ||
            source.Right is not Relation prior || !ReadState(prior, out var priorEvent, out var allocations, out _, out _) || !Same(eventIdentity, priorEvent) ||
            result.Left is not Relation filled || result.Right is not Relation { Left: Relation absent, Right: Relation pending } ||
            !FiniteLaw.ClosedEntries(filled, out var actualFilled) || !FiniteLaw.ClosedEntries(absent, out var actualAbsent) ||
            !FiniteLaw.ClosedEntries(pending, out var actualPending)) return false;
        if (Contract(out _, out var required))
        {
            var expectedFilled = FiniteLaw.Entries(allocations).Cast<Relation>().Where(row => Member(required, row.Right)).ToArray();
            var expectedAbsent = required.Where(site => !expectedFilled.Any(row => Same(row.Right, site))).ToArray();
            if (!SameSet(actualFilled, expectedFilled) || !SameSet(actualAbsent, expectedAbsent) || actualPending.Length != 0 ||
                !FiniteLaw.DomainEntries(support, out var values) || values.Length != 1 ||
                !Same(values[0], expectedAbsent.Length == 0 ? Ready : Waiting)) return false;
        }
        else if (actualFilled.Length != 0 || actualAbsent.Length != 0 || actualPending.Length != 1 ||
            actualPending[0] is not Relation problem || !ReferenceEquals(problem.Left, ThresholdSites) || !ReferenceEquals(problem.Right, Sites) ||
            !StructuralConstraint.TryReadScope(support, out var unknownValues, out var seam, out var tail) || unknownValues.Length != 0 || seam.Equals(tail)) return false;
        state = prior; satisfied = filled; missing = absent; unresolved = pending; return true;
    }

    public bool TryReadReceipt(Relation receipt, out Relation before, out Relation request, out Relation after,
        out Relation accepted, out Relation blocked, out Relation remainder, out Relation drained, out Relation unresolved)
    {
        before = request = after = accepted = blocked = remainder = drained = unresolved = null!;
        if (receipt.Left is not Relation { Left: Relation premise, Right: var surface } || !ReferenceEquals(surface, Surface) ||
            premise.Left is not Relation old || premise.Right is not Relation supplied ||
            !ReadState(old, out var previous, out _, out _, out _) ||
            supplied.Left is not Relation { Left: Relation intake, Right: Relation } ||
            intake.Left is not Relation port || !(ReferenceEquals(port, InputPort) || ReferenceEquals(port, DrainPort)) ||
            intake.Right is not Relation events || !Same(events.Right, previous) ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation rest } ||
            result.Left is not Relation current || !ReadState(current, out _, out _, out _, out _) || result.Right is not Relation yes ||
            rest.Left is not Relation no || rest.Right is not Relation { Left: Relation remaining, Right: Relation tail } ||
            tail.Left is not Relation released || tail.Right is not Relation { Left: Relation pending, Right: Relation proof } ||
            !FiniteLaw.DomainEntries(yes, out _) || !FiniteLaw.DomainEntries(no, out _) || !Scope(remaining) ||
            !FiniteLaw.DomainEntries(released, out _) || !FiniteLaw.ClosedEntries(pending, out _) || !FiniteLaw.ClosedEntries(proof, out _)) return false;
        before = old; request = supplied; after = current; accepted = yes; blocked = no;
        remainder = remaining; drained = released; unresolved = pending; return true;
    }

    public bool TryReadProofs(Relation receipt, out Relation proofs)
    {
        proofs = null!;
        if (!TryReadReceipt(receipt, out _, out _, out _, out _, out _, out _, out _, out _)) return false;
        var blocked = (Relation)((Relation)receipt.Right).Right;
        var remainder = (Relation)blocked.Right;
        var drained = (Relation)remainder.Right;
        var unresolved = (Relation)drained.Right;
        proofs = (Relation)unresolved.Right;
        return true;
    }

    /// <summary>Partitions actual offered packet occurrences, preserving an open
    /// tail. An unresolved duplicate offer is retained, never cloned into intake.</summary>
    public bool TryReadAccounting(Relation receipt, out Relation incoming, out Relation accepted,
        out Relation blocked, out Relation remainder)
    {
        incoming = accepted = blocked = remainder = null!;
        if (!TryReadReceipt(receipt, out _, out var request, out _, out var yes, out var no, out var rest, out _, out _) ||
            request.Left is not Relation { Right: Relation packets } ||
            !StructuralConstraint.TryReadScope(packets, out var offered, out var seam, out var end) ||
            !StructuralConstraint.TryReadScope(rest, out var retained, out var restSeam, out var restEnd) ||
            seam.Equals(end) != restSeam.Equals(restEnd)) return false;
        var left = offered.ToList();
        foreach (var packet in FiniteLaw.Entries(yes).Concat(FiniteLaw.Entries(no)).Concat(retained))
        {
            var index = left.FindIndex(item => Same(item, packet));
            if (index < 0) return false;
            left.RemoveAt(index);
        }
        if (left.Count != 0) return false;
        incoming = packets; accepted = yes; blocked = no; remainder = rest; return true;
    }

    private Relation CurrentRead() => readinessRead ??= Read();
    private Relation Read()
    {
        var publication = Observe();
        QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _);
        return FiniteLaw.WithProvenance(FiniteLaw.Evaluate(Law, support, S(Ready)), publication);
    }

    private bool CurrentQuestion(Relation question, out Relation fresh)
    {
        fresh = null!;
        if (question.Left is not Relation { Left: Relation premise } query ||
            !ReferenceEquals(premise.Left, CurrentRead()) || premise.Right is not Relation routes ||
            !QualitativeQuestion.TryReadAnswer(question, out var answer)) return false;
        fresh = QualitativeQuestion.Ask(CurrentRead(), routes, query.Right);
        return QualitativeQuestion.TryReadAnswer(fresh, out var actual) && Same(answer, actual);
    }

    private bool Contract(out IElement[] sites, out IElement[] required)
    {
        sites = required = [];
        if (!FiniteLaw.DomainEntries(Sites, out sites) || !FiniteLaw.DomainEntries(ThresholdSites, out required) || required.Length == 0) return false;
        foreach (var site in required) if (!Member(sites, site)) return false;
        return true;
    }

    private bool ReadState(Relation state, out IElement eventIdentity, out Relation allocations, out Relation used, out Relation history)
    {
        eventIdentity = null!; allocations = used = history = null!;
        if (state.Left is not Relation context || !ReferenceEquals(context.Left, Surface) ||
            state.Right is not Relation { Left: Relation occupied, Right: Relation body } ||
            body.Left is not Relation seen || body.Right is not Relation { Left: Relation trail } ||
            !FiniteLaw.ClosedEntries(occupied, out var placements) || !FiniteLaw.DomainEntries(seen, out var packets) ||
            !FiniteLaw.DomainEntries(trail, out var events) || events.Length == 0 || !Same(events[^1], context.Right)) return false;
        var packetIds = new List<IElement>(); var siteIds = new List<IElement>();
        foreach (var entry in placements)
        {
            if (entry is not Relation row || !Member(packets, row.Left) || Member(packetIds, row.Left) || Member(siteIds, row.Right) ||
                !StructuralConstraint.TryReadScope(Sites, out var knownSites, out _, out _) || !Member(knownSites, row.Right)) return false;
            packetIds.Add(row.Left); siteIds.Add(row.Right);
        }
        eventIdentity = context.Right; allocations = occupied; used = seen; history = trail; return true;
    }

    private Relation MakeState(IElement eventIdentity, Relation allocations, Relation used, Relation history, IElement proof) =>
        new(new Relation(Surface, eventIdentity), new Relation(allocations, new Relation(used, new Relation(history, proof))));

    private void SetState(IElement eventIdentity, Relation allocations, Relation used, Relation history,
        Relation before, Relation request, List<IElement> proof)
    {
        State = MakeState(eventIdentity, allocations, used, S([.. FiniteLaw.Entries(history), eventIdentity]),
            new Relation(before, new Relation(request, S(proof.ToArray()))));
        observation = readinessRead = null;
    }

    private static Relation Request(Relation port, IElement eventIdentity, IElement prior, Relation packets, IElement evidence) =>
        new(new Relation(new Relation(port, new Relation(eventIdentity, prior)), packets), evidence);

    private Relation Receipt(Relation before, Relation request, Relation accepted, Relation blocked, Relation remainder,
        Relation drained, Relation pending, Relation proof) => new(new Relation(new Relation(before, request), Surface),
            new Relation(new Relation(State, accepted), new Relation(blocked,
                new Relation(remainder, new Relation(drained, new Relation(pending, proof))))));

    private static bool Scope(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out _, out _);
    private static bool Same(IElement a, IElement b) => FiniteLaw.SameParticipant(a, b);
    private static bool Member(IEnumerable<IElement> values, IElement value) => values.Any(item => Same(item, value));
    private static bool SameSet(IElement[] actual, IElement[] expected) => actual.Length == expected.Length &&
        actual.All(item => Member(expected, item)) && actual.Select(item => Array.FindIndex(expected, candidate => Same(item, candidate))).Distinct().Count() == expected.Length;
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}
