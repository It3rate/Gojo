using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded route through a reusable motif and acknowledged cell
/// passages. Completion is an observed control connection; every instruction
/// consumes its own packet once. The shared receiver is the exposed actuator,
/// not a source of private state for the participating controllers.</summary>
public sealed class WallpaperPatternMachine
{
    // passage = (from-cell -> to-cell) -> Closed(single drawing actions)
    // dispatch row = (current-cell -> actual completion port) ->
    //                Closed(next-cell -> next input port)
    // control state = (current-cell -> admitted input port) -> Closed(history)
    // step = (Surface -> (control-before -> component acknowledgment)) ->
    //        (control-after -> (Closed(dispatch reads) -> Closed(problems)))
    // A terminal port is a declared closed endpoint, not a wrap to the start.
    private readonly IElement source = new Placeholder();
    private readonly IElement refinement = new Placeholder();
    public string Name { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public AcknowledgedPatternSequence Motif { get; }
    public IReadOnlyList<AcknowledgedPatternSequence> Placements { get; }
    public Relation Passages { get; }
    public Relation Connections { get; }
    public Relation DispatchLaw { get; }
    public Relation TerminalPort { get; }
    public Relation InitialState { get; }
    public Relation State { get; private set; }
    public Relation Surface { get; }
    public Relation Timeline { get; private set; } = S();
    public Relation Dispatches { get; private set; } = S();
    public Relation DrawingReceipts { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    public Relation ReturnedInputs { get; private set; } = S();
    public Relation PendingDispatch { get; private set; } = S();
    public IElement CurrentCell => ((Relation)State.Left).Left;
    public IElement ActiveInput => ((Relation)State.Left).Right;
    public bool IsComplete => ReferenceEquals(ActiveInput, TerminalPort);
    public Relation CompletedMotifs => Motif.CompletionEvents;
    public Relation AcceptedPassages => S(Placements.SelectMany(sequence => E(sequence.CompletionEvents)).ToArray());
    public Relation Pending => S(Components.SelectMany(sequence => E(sequence.Pending)).Concat(E(PendingDispatch)).ToArray());
    public IEnumerable<AcknowledgedPatternSequence> Components => new[] { Motif }.Concat(Placements);

    public WallpaperPatternMachine(string name, QualitativeDrawingReceiver drawing,
        Relation motifActions, Relation passages, IElement initialCell, Relation support)
    {
        if (!Closed(passages)) throw new ArgumentException("Passages must be closed.", nameof(passages));
        Name = name; Drawing = drawing; Passages = passages;
        Motif = new("motif", drawing, motifActions);
        TerminalPort = new Relation(source, S());
        var placements = new List<AcknowledgedPatternSequence>();
        var cells = new List<IElement> { initialCell };
        var routes = new List<Relation>(); var destinations = new List<IElement>();
        var links = new List<IElement>(); var current = initialCell;
        foreach (var item in E(passages))
        {
            if (item is not Relation { Left: Relation edge, Right: Relation actions } ||
                !ReferenceEquals(edge.Left, current) || cells.Any(cell => ReferenceEquals(cell, edge.Right)))
                throw new ArgumentException("Passages must form one finite simple ordered route from the initial cell.", nameof(passages));
            var placement = new AcknowledgedPatternSequence("accepted cell passage", drawing, actions);
            placements.Add(placement);
            Add(current, Motif.CompletionPort, current, placement.InputPort);
            Add(current, placement.CompletionPort, edge.Right, Motif.InputPort);
            links.Add(new Relation(item, placement.Surface));
            cells.Add(edge.Right); current = edge.Right;
        }
        Add(current, Motif.CompletionPort, current, TerminalPort);
        Placements = placements;
        DispatchLaw = FiniteLaw.Create(S(cells.ToArray()),
            S(new IElement[] { Motif.CompletionPort }.Concat(placements.Select(p => (IElement)p.CompletionPort)).ToArray()),
            S(destinations.ToArray()), routes.ToArray());
        Connections = S(links.ToArray());
        InitialState = State = new Relation(new Relation(initialCell, Motif.InputPort), S());
        Surface = new Relation(new Relation(Drawing.Surface, support),
            new Relation(new Relation(Motif.Surface, new Relation(Passages, Connections)),
                new Relation(DispatchLaw, new Relation(TerminalPort, InitialState))));

        void Add(IElement from, Relation completion, IElement nextCell, Relation nextInput)
        {
            var destination = new Relation(nextCell, nextInput); destinations.Add(destination);
            routes.Add(FiniteLaw.Row(from, completion, S(destination)));
        }
    }

    public bool TryReadActive(out AcknowledgedPatternSequence sequence)
    {
        sequence = Components.SingleOrDefault(component => ReferenceEquals(component.InputPort, ActiveInput))!;
        return sequence is not null;
    }

    /// <summary>The native active input surface admits a fresh pulse. No input
    /// means no request. Terminal input is returned without importing it.</summary>
    public Relation Propose(IElement eventIdentity, Relation packets)
    {
        var returned = new Relation(new Relation(ActiveInput, eventIdentity), packets);
        var seen = Components.SelectMany(sequence => E(sequence.ImportedPackets)).ToList();
        var fresh = Closed(packets);
        if (fresh)
            foreach (var packet in E(packets))
            { if (seen.Any(prior => Same(prior, packet))) fresh = false; seen.Add(packet); }
        if (!fresh || Components.Any(sequence => E(sequence.ProposalEvents).Any(prior => Same(prior, eventIdentity))))
        {
            ReturnedInputs = Append(ReturnedInputs, returned);
            Unresolved = Append(Unresolved, returned); return returned;
        }
        if (TryReadActive(out var component) && E(PendingDispatch).Length == 0) return component.Propose(eventIdentity, packets);
        ReturnedInputs = Append(ReturnedInputs, returned); return returned;
    }

    public Relation Acknowledge(Relation request, Relation receipt)
    {
        var before = State; Relation reads = S(), problems = S();
        if (!TryReadActive(out var component)) return Failed(new Relation(request, receipt));
        var acknowledgment = component.Acknowledge(request, receipt);
        if (!component.TryReadAccepted(acknowledgment, out var effect, out _, out _, out var completions))
            return Failed(acknowledgment);
        DrawingReceipts = Append(DrawingReceipts, effect);
        if (E(completions) is [Relation completion])
        {
            // The completion port is an observation of this exact accepted
            // cycle. It supplies permission, not a duplicated material packet.
            var read = FiniteLaw.WithProvenance(FiniteLaw.Evaluate(DispatchLaw,
                S(CurrentCell), S(component.CompletionPort)), new Relation(completion, acknowledgment));
            if (!FiniteLaw.TryRead(DispatchLaw, out _, out _, out var outputs, out _))
            { HoldDispatch(read); return Failed(read); }
            var question = QualitativeQuestion.Ask(read,
                S(E(outputs).Select(value => (IElement)new Relation(value, value)).ToArray()), refinement);
            var dispatch = new Relation(new Relation(before, completion), new Relation(read, question));
            reads = S(dispatch); Dispatches = Append(Dispatches, dispatch);
            if (!QualitativeQuestion.TryReadAnswer(question, out var answer) || answer is not Relation destination)
            {
                HoldDispatch(dispatch);
                return Failed(dispatch);
            }
            State = new Relation(destination, Append((Relation)before.Right, dispatch));
        }
        return Finish(acknowledgment);

        Relation Failed(IElement problem)
        { problems = S(problem); Unresolved = Append(Unresolved, problem); return Finish(new Relation(request, receipt)); }
        void HoldDispatch(IElement problem)
        {
            PendingDispatch = S(problem);
            State = new Relation(new Relation(CurrentCell, refinement), Append((Relation)before.Right, problem));
        }
        Relation Finish(IElement result)
        {
            var step = new Relation(new Relation(Surface, new Relation(before, result)),
                new Relation(State, new Relation(reads, problems)));
            Timeline = Append(Timeline, step); return step;
        }
    }

    public Relation Drive(IElement eventIdentity, Relation packets)
    {
        if (!TryReadActive(out var component)) return Propose(eventIdentity, packets);
        var request = Propose(eventIdentity, packets);
        return component.TryReadRequest(request, out _, out var before, out var offer)
            ? Acknowledge(request, Drawing.Step(before, offer)) : request;
    }
    public Relation Pulse() => Drive(new Placeholder(), S(new Placeholder()));

    /// <summary>Host sampling budget only; the finite native route determines
    /// completion. Reaching a budget never manufactures a completion event.</summary>
    public Relation Run(int maximumPulses = 2048)
    {
        if (maximumPulses is < 0 or > 8192) throw new ArgumentOutOfRangeException(nameof(maximumPulses));
        for (var pulse = 0; pulse < maximumPulses && !IsComplete; pulse++)
        {
            Pulse();
            if (E(Pending).Length != 0) break;
        }
        return new Relation(Surface, new Relation(State,
            new Relation(Timeline, new Relation(Drawing.State,
                new Relation(Pending, new Relation(Account(),
                    new Relation(ReadComponents(), new Relation(ReturnedInputs, Unresolved))))))));
    }

    /// <summary>Publishes local histories even when an input only waits or is
    /// retained. Completion history alone would omit those observations.</summary>
    public Relation ReadComponents() => S(Components.Select(sequence => (IElement)new Relation(sequence.Surface,
        new Relation(new Relation(sequence.ControllerState, sequence.ReservoirState),
            new Relation(sequence.InputHistory, new Relation(sequence.Timeline, sequence.Unresolved))))).ToArray());

    public Relation Account() => S(Components.Select(sequence => (IElement)sequence.Account()).ToArray());
    private static Relation Append(Relation scope, IElement entry) => S([.. E(scope), entry]);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation p && b is CarrierLocation q && p.Carrier.SameIdentityAs(q.Carrier) && p.SameTickRefAs(q);
}
