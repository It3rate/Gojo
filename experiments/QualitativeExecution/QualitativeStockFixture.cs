using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution;

/// <summary>
/// Experimental uncalibrated, equal-quantum occupancy. These are declared
/// discrete event transitions, not measurements of continuous stock or time.
/// The law, local capacity, offered event, accepted/blocked pulse and causal
/// predecessor all remain native relations. No carrier value is assigned.
/// </summary>
public sealed class QualitativeStockFixture
{
    // transition = ((before -> input) -> after) -> (accepted -> blocked)
    // state = ((source -> receiver) -> (event -> surface)) ->
    //         (state-support -> (Closed(event-history) -> provenance))
    // offer = ((source -> receiver) -> (event -> prior-event)) ->
    //         ((surface -> input-support) -> evidence)
    // step = ((before -> offer) -> law) ->
    //        ((after -> transition-support) -> (unresolved -> provenance))
    public IElement Receiver { get; } = new Placeholder();
    public IElement Quantum { get; } = new Placeholder();
    public Relation Empty { get; }
    public Relation Available { get; }
    public Relation Full { get; }
    public Relation Charge { get; }
    public Relation Hold { get; }
    public Relation Draw { get; }
    public Relation Capacity { get; }
    public Relation EqualQuantum { get; }
    public Relation StateDomain { get; }
    public Relation InputDomain { get; }
    public Relation TransitionDomain { get; }
    public Relation Law { get; }
    public Relation Surface { get; }

    public QualitativeStockFixture()
    {
        var bottom = new Placeholder();
        var interior = new Placeholder();
        var top = new Placeholder();
        Empty = new Relation(bottom, bottom);
        Available = new Relation(bottom, interior);
        Full = new Relation(bottom, top);
        Capacity = new Relation(Empty, Full);
        var firstIncrement = new Relation(Empty, Available);
        var secondIncrement = new Relation(Available, Full);
        EqualQuantum = StructuralConstraint.Closed(
            new Relation(firstIncrement, Quantum), new Relation(secondIncrement, Quantum));
        Charge = new Relation(Quantum, new Relation(Empty, Full));
        Draw = new Relation(Quantum, new Relation(Full, Empty));
        Hold = new Relation(Empty, Empty);
        StateDomain = StructuralConstraint.Closed(Empty, Available, Full);
        InputDomain = StructuralConstraint.Closed(Charge, Hold, Draw);
        var transitions = new[]
        {
            Transition(Empty, Charge, Available, Charge),
            Transition(Empty, Hold, Empty),
            Transition(Empty, Draw, Empty, blocked: Draw),
            Transition(Available, Charge, Full, Charge),
            Transition(Available, Hold, Available),
            Transition(Available, Draw, Empty, Draw),
            Transition(Full, Charge, Full, blocked: Charge),
            Transition(Full, Hold, Full),
            Transition(Full, Draw, Available, Draw)
        };
        TransitionDomain = StructuralConstraint.Closed(transitions);
        var rows = transitions.Select(transition =>
        {
            TryReadTransition(transition, out var before, out var input, out _, out _, out _);
            return FiniteLaw.Row(before, input, StructuralConstraint.Closed(transition));
        }).ToArray();
        Law = FiniteLaw.Create(StateDomain, InputDomain, TransitionDomain, rows);
        Surface = new Relation(new Relation(Receiver, new Relation(Capacity, EqualQuantum)),
            new Relation(new Relation(StateDomain, InputDomain), new Relation(TransitionDomain, Law)));
    }

    public Relation Initial(IElement source, IElement eventIdentity, Relation stateSupport) =>
        State(source, eventIdentity, stateSupport, StructuralConstraint.Closed(eventIdentity),
            StructuralConstraint.Closed());

    /// <summary>A pulse can be supplied by an independent explicit clock/route.
    /// Mere passage of host time never creates a draw event.</summary>
    public Relation Offer(IElement source, IElement eventIdentity, IElement priorEvent,
        Relation inputSupport, IElement? evidence = null) =>
        new(new Relation(new Relation(source, Receiver), new Relation(eventIdentity, priorEvent)),
            new Relation(new Relation(Surface, inputSupport), evidence ?? StructuralConstraint.Closed()));

    /// <summary>Admits exactly one complete transition. Ambiguous, open, stale or
    /// foreign offers preserve the prior operational state and retain the offer
    /// with its alternatives; they never manufacture a hold or empty state.</summary>
    public Relation Step(Relation state, Relation offer)
    {
        var read = FiniteLaw.Evaluate(Law, StructuralConstraint.Open(), StructuralConstraint.Open());
        var after = state;
        var unresolved = new List<IElement>();
        if (!ReadState(state, out var source, out var previousEvent, out var states, out var history) ||
            !ReadOffer(offer, out var offeredSource, out var currentEvent, out var offeredPrior, out var inputs) ||
            !FiniteLaw.SameParticipant(source, offeredSource) ||
            !FiniteLaw.SameParticipant(previousEvent, offeredPrior) ||
            FiniteLaw.Entries(history).Any(e => FiniteLaw.SameParticipant(e, currentEvent)))
        {
            unresolved.Add(new Relation(state, offer));
        }
        else
        {
            read = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(Law, states, inputs), offer);
            if (FiniteLaw.TryReadResult(read, out var support, out var residue, out _) &&
                FiniteLaw.ClosedEntries(support, out var transitions) && transitions.Length == 1 &&
                FiniteLaw.ClosedEntries(residue, out var pending) && pending.Length == 0 &&
                CompleteSingle(states) && CompleteSingle(inputs) &&
                TryReadTransition(transitions[0], out _, out _, out var next, out _, out _))
            {
                after = State(source, currentEvent, StructuralConstraint.Closed(next),
                    StructuralConstraint.Closed([.. FiniteLaw.Entries(history), currentEvent]),
                    new Relation(state, new Relation(offer, read)));
            }
            else unresolved.Add(read);
        }
        FiniteLaw.TryReadResult(read, out var transitionSupport, out _, out _);
        return new Relation(new Relation(new Relation(state, offer), Law),
            new Relation(new Relation(after, transitionSupport),
                new Relation(StructuralConstraint.Closed(unresolved.ToArray()),
                    StructuralConstraint.Closed(read))));
    }

    public bool TryReadState(Relation state, out IElement source,
        out IElement eventIdentity, out Relation stateSupport) =>
        ReadState(state, out source, out eventIdentity, out stateSupport, out _);

    public bool TryReadStep(Relation step, out Relation before, out Relation offer,
        out Relation after, out Relation transitionSupport, out Relation unresolved)
    {
        before = offer = after = transitionSupport = unresolved = null!;
        if (step.Left is not Relation { Left: Relation premise, Right: var law } ||
            !ReferenceEquals(law, Law) || premise.Left is not Relation old || premise.Right is not Relation supplied ||
            step.Right is not Relation { Left: Relation result, Right: Relation remainder } ||
            result.Left is not Relation current || result.Right is not Relation transitions ||
            remainder.Left is not Relation pending || remainder.Right is not Relation provenance ||
            !StructuralConstraint.TryReadScope(transitions, out _, out _, out _) ||
            !FiniteLaw.ClosedEntries(pending, out _) || !FiniteLaw.ClosedEntries(provenance, out _)) return false;
        before = old; offer = supplied; after = current;
        transitionSupport = transitions; unresolved = pending;
        return true;
    }

    public static bool TryReadTransition(IElement transition, out IElement before,
        out IElement input, out IElement after, out Relation accepted, out Relation blocked)
    {
        before = input = after = null!; accepted = blocked = null!;
        if (transition is not Relation { Left: Relation path, Right: Relation accounting } ||
            path.Left is not Relation entry || accounting.Left is not Relation admitted ||
            accounting.Right is not Relation refused ||
            !FiniteLaw.ClosedEntries(admitted, out _) || !FiniteLaw.ClosedEntries(refused, out _)) return false;
        before = entry.Left; input = entry.Right; after = path.Right;
        accepted = admitted; blocked = refused;
        return true;
    }

    private Relation State(IElement source, IElement eventIdentity, Relation support,
        Relation history, IElement provenance) =>
        new(new Relation(new Relation(source, Receiver), new Relation(eventIdentity, Surface)),
            new Relation(support, new Relation(history, provenance)));

    private bool ReadState(Relation state, out IElement source,
        out IElement eventIdentity, out Relation support, out Relation history)
    {
        source = eventIdentity = null!; support = history = null!;
        if (state.Left is not Relation { Left: Relation owner, Right: Relation context } ||
            !ReferenceEquals(owner.Right, Receiver) || !ReferenceEquals(context.Right, Surface) ||
            state.Right is not Relation { Left: Relation states, Right: Relation evidence } ||
            evidence.Left is not Relation events || !FiniteLaw.ClosedEntries(events, out var trail) ||
            trail.Length == 0 || !FiniteLaw.SameParticipant(trail[^1], context.Left) ||
            !StructuralConstraint.TryReadScope(states, out _, out _, out _)) return false;
        for (var i = 0; i < trail.Length; i++)
            for (var j = 0; j < i; j++)
                if (FiniteLaw.SameParticipant(trail[i], trail[j])) return false;
        source = owner.Left; eventIdentity = context.Left; support = states; history = events;
        return true;
    }

    private bool ReadOffer(Relation offer, out IElement source, out IElement eventIdentity,
        out IElement prior, out Relation inputs)
    {
        source = eventIdentity = prior = null!; inputs = null!;
        if (offer.Left is not Relation { Left: Relation owner, Right: Relation events } ||
            !ReferenceEquals(owner.Right, Receiver) ||
            offer.Right is not Relation { Left: Relation publication } ||
            !ReferenceEquals(publication.Left, Surface) || publication.Right is not Relation support ||
            !StructuralConstraint.TryReadScope(support, out _, out _, out _)) return false;
        source = owner.Left; eventIdentity = events.Left; prior = events.Right; inputs = support;
        return true;
    }

    private static bool CompleteSingle(Relation support) =>
        FiniteLaw.ClosedEntries(support, out var entries) && entries.Length == 1;

    private static Relation Transition(IElement before, IElement input, IElement after,
        IElement? accepted = null, IElement? blocked = null) =>
        new(new Relation(new Relation(before, input), after),
            new Relation(accepted is null ? StructuralConstraint.Closed() : StructuralConstraint.Closed(accepted),
                blocked is null ? StructuralConstraint.Closed() : StructuralConstraint.Closed(blocked)));
}
