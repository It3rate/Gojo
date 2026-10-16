using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Explicitly driven recurrence over reusable phase identities. A cyclic phase
/// template does not create cyclic occurrence history, an autonomous clock,
/// equal elapsed intervals, or an effect. Those require their own connections.
/// </summary>
public sealed class PhaseRecurrence
{
    // surface = (receiver -> Closed(phases)) -> Closed(from-phase -> to-phase)
    // state = ((source -> receiver) -> (event -> surface)) ->
    //         (phase-support -> (Closed(event-history) -> provenance))
    // drive = ((source -> receiver) -> (event -> prior-event)) -> (surface -> evidence)
    // receipt = ((before -> drive) -> surface) ->
    //           ((after -> accepted-drives) -> (blocked-drives -> (unresolved -> proofs)))
    // A drive requests one immediate edge, never the transitive closure. Its
    // event identity is the new occurrence; its predecessor is the old cursor.
    public IElement Receiver { get; } = new Placeholder();
    public Relation Domain { get; }
    public Relation Transitions { get; }
    public Relation Surface { get; }

    public PhaseRecurrence(Relation domain, Relation transitions)
    {
        Domain = domain;
        Transitions = transitions;
        Surface = new Relation(new Relation(Receiver, domain), transitions);
    }

    /// <summary>Starts an independent history. Unknown phase support is allowed
    /// as an unsettled state, but cannot be advanced as a guessed phase.</summary>
    public Relation Initial(IElement source, IElement eventIdentity, Relation phaseSupport) =>
        State(source, eventIdentity, phaseSupport, StructuralConstraint.Closed(eventIdentity),
            StructuralConstraint.Closed());

    /// <summary>One explicit drive can be admitted once in the supplied history.
    /// Evidence may expose a clock calibration or another controller's output;
    /// this mechanism does not invent a drive by inspecting host time.</summary>
    public Relation Drive(IElement source, IElement eventIdentity, IElement priorEvent,
        IElement? evidence = null) =>
        new(new Relation(new Relation(source, Receiver), new Relation(eventIdentity, priorEvent)),
            new Relation(Surface, evidence ?? StructuralConstraint.Closed()));

    /// <summary>Advances one uniquely determined immediate successor. Missing,
    /// competing, open or foreign phase information preserves the old state and
    /// the attempted drive. Replay, wrong ownership and stale predecessors are
    /// blocked. Historical branching is explicit, with no global active head.</summary>
    public Relation Step(Relation state, Relation drive)
    {
        var accepted = StructuralConstraint.Closed();
        var after = state;
        if (Authorize(state, drive, out var source, out var eventIdentity,
                out var next, out var history, out var blocked, out var unresolved, out var proof))
        {
            accepted = StructuralConstraint.Closed(drive);
            after = State(source, eventIdentity, StructuralConstraint.Closed(next),
                StructuralConstraint.Closed([.. FiniteLaw.Entries(history), eventIdentity]),
                new Relation(state, new Relation(drive, proof)));
        }
        return new Relation(new Relation(new Relation(state, drive), Surface),
            new Relation(new Relation(after, accepted),
                new Relation(blocked, new Relation(unresolved, StructuralConstraint.Closed(proof)))));
    }

    public bool TryReadState(Relation state, out IElement source,
        out IElement eventIdentity, out Relation phaseSupport) =>
        ReadState(state, out source, out eventIdentity, out phaseSupport, out _);

    public bool TryReadHistory(Relation state, out Relation history) =>
        ReadState(state, out _, out _, out _, out history);

    public bool TryReadDrive(Relation drive, out IElement source, out IElement eventIdentity,
        out IElement priorEvent, out IElement evidence)
    {
        source = eventIdentity = priorEvent = evidence = null!;
        if (!ReadSurface() ||
            drive.Left is not Relation { Left: Relation owner, Right: Relation events } ||
            !ReferenceEquals(owner.Right, Receiver) ||
            drive.Right is not Relation publication || !ReferenceEquals(publication.Left, Surface)) return false;
        source = owner.Left; eventIdentity = events.Left; priorEvent = events.Right; evidence = publication.Right;
        return true;
    }

    public bool TryReadStep(Relation receipt, out Relation before, out Relation drive,
        out Relation after, out Relation accepted, out Relation blocked, out Relation unresolved)
    {
        before = drive = after = accepted = blocked = unresolved = null!;
        if (receipt.Left is not Relation { Left: Relation premise, Right: var surface } ||
            !ReferenceEquals(surface, Surface) || premise.Left is not Relation previous ||
            premise.Right is not Relation offered ||
            receipt.Right is not Relation { Left: Relation result, Right: Relation refusal } ||
            result.Left is not Relation current || result.Right is not Relation admitted ||
            refusal.Left is not Relation refused || refusal.Right is not Relation remainder ||
            remainder.Left is not Relation pending || remainder.Right is not Relation proof ||
            !FiniteLaw.ClosedEntries(admitted, out _) || !FiniteLaw.ClosedEntries(refused, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _) || !FiniteLaw.ClosedEntries(proof, out _)) return false;
        before = previous; drive = offered; after = current;
        accepted = admitted; blocked = refused; unresolved = pending;
        return true;
    }

    /// <summary>An effect adapter can require this guarded read before using a
    /// receipt. Reading permission does not consume or execute the effect.</summary>
    public bool TryReadAccepted(Relation receipt, out IElement source,
        out IElement eventIdentity, out IElement phase)
    {
        source = eventIdentity = phase = null!;
        if (!TryReadStep(receipt, out var before, out var drive, out var after,
                out var accepted, out var blocked, out var unresolved) ||
            FiniteLaw.Entries(accepted) is not [var admitted] || !ReferenceEquals(admitted, drive) ||
            FiniteLaw.Entries(blocked).Length != 0 || FiniteLaw.Entries(unresolved).Length != 0 ||
            !Authorize(before, drive, out var owner, out var occurrence, out var next,
                out var previousHistory, out _, out _, out _) ||
            !ReadState(after, out var actualOwner, out var actualEvent, out var phases, out var history) ||
            !FiniteLaw.SameParticipant(owner, actualOwner) || !FiniteLaw.SameParticipant(occurrence, actualEvent) ||
            !FiniteLaw.ClosedEntries(phases, out var support) || support.Length != 1 ||
            !FiniteLaw.SameParticipant(next, support[0])) return false;
        var priorTrail = FiniteLaw.Entries(previousHistory);
        var trail = FiniteLaw.Entries(history);
        if (trail.Length != priorTrail.Length + 1) return false;
        for (var i = 0; i < priorTrail.Length; i++)
            if (!FiniteLaw.SameParticipant(trail[i], priorTrail[i])) return false;
        source = owner; eventIdentity = occurrence; phase = next;
        return true;
    }

    private Relation State(IElement source, IElement eventIdentity, Relation phaseSupport,
        Relation history, IElement provenance) =>
        new(new Relation(new Relation(source, Receiver), new Relation(eventIdentity, Surface)),
            new Relation(phaseSupport, new Relation(history, provenance)));

    private bool ReadSurface() =>
        Surface.Left is Relation owner && ReferenceEquals(owner.Left, Receiver) &&
        ReferenceEquals(owner.Right, Domain) && ReferenceEquals(Surface.Right, Transitions);

    private bool ReadState(Relation state, out IElement source, out IElement eventIdentity,
        out Relation phaseSupport, out Relation history)
    {
        source = eventIdentity = null!; phaseSupport = history = null!;
        if (!ReadSurface() ||
            state.Left is not Relation { Left: Relation owner, Right: Relation context } ||
            !ReferenceEquals(owner.Right, Receiver) || !ReferenceEquals(context.Right, Surface) ||
            state.Right is not Relation { Left: Relation phases, Right: Relation evidence } ||
            evidence.Left is not Relation events || !FiniteLaw.ClosedEntries(events, out var trail) ||
            trail.Length == 0 || !FiniteLaw.SameParticipant(trail[^1], context.Left) ||
            !StructuralConstraint.TryReadScope(phases, out _, out _, out _)) return false;
        for (var i = 0; i < trail.Length; i++)
            for (var j = 0; j < i; j++)
                if (FiniteLaw.SameParticipant(trail[i], trail[j])) return false;
        source = owner.Left; eventIdentity = context.Left; phaseSupport = phases; history = events;
        return true;
    }

    private bool Authorize(Relation state, Relation drive, out IElement source,
        out IElement eventIdentity, out IElement next, out Relation history,
        out Relation blocked, out Relation unresolved, out Relation proof)
    {
        source = eventIdentity = next = null!; history = null!;
        blocked = unresolved = StructuralConstraint.Closed();
        var evidence = new List<IElement> { Surface, new Relation(state, drive) };
        if (!ReadState(state, out var owner, out var previousEvent, out var phases, out var events) ||
            !TryReadDrive(drive, out var offeredOwner, out var occurrence, out var offeredPrior, out _))
            return Pending(new Relation(state, drive), drive, evidence, out unresolved, out proof);
        if (!FiniteLaw.SameParticipant(owner, offeredOwner) ||
            !FiniteLaw.SameParticipant(previousEvent, offeredPrior) ||
            FiniteLaw.Entries(events).Any(e => FiniteLaw.SameParticipant(e, occurrence)))
        {
            evidence.Add(new Relation(events, new Relation(previousEvent, offeredPrior)));
            blocked = StructuralConstraint.Closed(drive);
            proof = StructuralConstraint.Closed(evidence.ToArray());
            return false;
        }
        if (!FiniteLaw.DomainEntries(Domain, out var domain) || domain.Length == 0 ||
            !FiniteLaw.ClosedEntries(Transitions, out var entries) || entries.Any(entry =>
                entry is not Relation edge || FiniteLaw.IndexOf(domain, edge.Left) < 0 ||
                FiniteLaw.IndexOf(domain, edge.Right) < 0))
            return Pending(new Relation(Domain, Transitions), drive, evidence, out unresolved, out proof);
        if (!FiniteLaw.ClosedEntries(phases, out var current) || current.Length != 1 ||
            FiniteLaw.IndexOf(domain, current[0]) < 0)
            return Pending(new Relation(phases, Domain), drive, evidence, out unresolved, out proof);

        var beforeSlot = new Placeholder();
        var afterSlot = new Placeholder();
        var match = StructuralConstraint.Match(new Relation(beforeSlot, afterSlot),
            StructuralConstraint.Closed(beforeSlot, afterSlot), Transitions,
            new Relation(beforeSlot, current[0]));
        evidence.Add(match);
        var successors = new List<IElement>();
        foreach (var edge in entries.Cast<Relation>().Where(edge =>
                     FiniteLaw.SameParticipant(edge.Left, current[0])))
            if (!successors.Any(successor => FiniteLaw.SameParticipant(successor, edge.Right)))
                successors.Add(edge.Right);
        var alternatives = StructuralConstraint.Closed(successors.ToArray());
        evidence.Add(new Relation(phases, alternatives));
        if (successors.Count != 1)
            return Pending(new Relation(match, alternatives), drive, evidence, out unresolved, out proof);
        source = owner; eventIdentity = occurrence; next = successors[0]; history = events;
        proof = StructuralConstraint.Closed(evidence.ToArray());
        return true;
    }

    private static bool Pending(IElement reason, Relation drive, List<IElement> evidence,
        out Relation unresolved, out Relation proof)
    {
        proof = StructuralConstraint.Closed(evidence.ToArray());
        unresolved = StructuralConstraint.Closed(new Relation(drive, new Relation(reason, proof)));
        return false;
    }
}
