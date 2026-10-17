using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An explicitly driven cyclic instruction sequence. Its single local
/// packet remains held until the exact offered drawing effect is accepted.
/// Completion publishes the actual accepted effects of one whole traversal;
/// it is a notification, not another copy of the drained material.</summary>
public sealed class AcknowledgedPatternSequence
{
    // action row = actual phase -> actual single drawing action
    // request = (Surface -> (event -> action-row)) ->
    //           ((drawing-before -> actual-offer) ->
    //            (current-question -> (controller-before ->
    //             (reservoir-before -> prior-cycle-receipts))))
    // acknowledgment = ((Surface -> request) -> drawing-receipt) ->
    //                  ((Closed(phase-step) -> Closed(material-release)) ->
    //                   (Closed(completions) -> Closed(unresolved)))
    // completion = (CompletionPort -> event) ->
    //              (Closed(actual accepted drawing receipts) -> phase-step)
    private readonly QualitativeImpulseAccumulator accumulator;
    private readonly IElement source = new Placeholder();
    private readonly IElement refinement = new Placeholder();
    private readonly Relation hold = S();

    public string Name { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public Relation Actions { get; }
    public Relation ActionRows { get; }
    public PhaseRecurrence Controller { get; }
    public Relation ControllerState { get; private set; }
    public Relation InputPort => accumulator.InputPort;
    public Relation OutputPort => accumulator.DrainPort;
    public Relation ReservoirSurface => accumulator.Surface;
    public Relation ReservoirState => accumulator.State;
    public Relation CompletionPort { get; }
    public Relation Surface { get; }
    public Relation Pending { get; private set; } = S();
    public Relation Timeline { get; private set; } = S();
    public Relation InputHistory { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    public Relation ImportedPackets { get; private set; } = S();
    public Relation ReleasedPackets { get; private set; } = S();
    public Relation RetainedPackets { get; private set; } = S();
    public Relation CompletionEvents { get; private set; } = S();
    public Relation CurrentCycleReceipts { get; private set; } = S();
    public Relation ProposalEvents { get; private set; } = S();
    public Relation? LastCompletion => E(CompletionEvents).LastOrDefault() as Relation;
    public bool IsAtBoundary => E(Pending).Length == 0 && E(CurrentCycleReceipts).Length == 0 &&
        Controller.TryReadState(ControllerState, out _, out _, out var phases) && E(phases) is [var phase] &&
        ReferenceEquals(phase, E(Controller.Domain)[0]);

    public AcknowledgedPatternSequence(string name, QualitativeDrawingReceiver drawing, Relation actionsClosed)
    {
        if (!Closed(actionsClosed) || E(actionsClosed).Length == 0 || E(actionsClosed).Any(action => action is not Relation))
            throw new ArgumentException("A sequence needs a nonempty closed order of actual single actions.", nameof(actionsClosed));
        Name = name; Drawing = drawing; Actions = actionsClosed;
        var phases = E(Actions).Select(_ => (IElement)new Placeholder()).ToArray();
        Controller = new(S(phases), S(phases.Select((phase, index) =>
            (IElement)new Relation(phase, phases[(index + 1) % phases.Length])).ToArray()));
        ControllerState = Controller.Initial(source, new Placeholder(), S(phases[0]));
        ActionRows = S(phases.Select((phase, index) => (IElement)new Relation(phase, E(Actions)[index])).ToArray());
        var site = new Placeholder(); var sites = S(site);
        accumulator = new(source, sites, sites);
        CompletionPort = new Relation(Controller.Surface, phases[0]);
        Surface = new Relation(Drawing.Surface, new Relation(new Relation(Controller.Surface, accumulator.Surface),
            new Relation(new Relation(Actions, ActionRows), new Relation(InputPort, new Relation(OutputPort, CompletionPort)))));
    }

    /// <summary>Imports fresh packet occurrences through the exposed passage.
    /// During an outstanding action, new packets stay explicitly at that port;
    /// they do not mutate or replace the held request's permission.</summary>
    public Relation Propose(IElement eventIdentity, Relation packets)
    {
        var intake = new Relation(new Relation(InputPort, eventIdentity), packets);
        if (!Closed(packets) || E(ProposalEvents).Any(prior => Same(prior, eventIdentity)) || !Fresh(packets))
        { Unresolved = Append(Unresolved, intake); InputHistory = Append(InputHistory, intake); return intake; }
        ProposalEvents = Append(ProposalEvents, eventIdentity);
        ImportedPackets = S([.. E(ImportedPackets), .. E(packets)]);
        if (E(Pending).Length != 0)
        {
            RetainedPackets = Append(RetainedPackets, new Relation(InputPort, new Relation(intake, packets)));
            InputHistory = Append(InputHistory, new Relation(intake, Pending)); return intake;
        }
        var receive = accumulator.Receive(new Placeholder(), packets);
        InputHistory = Append(InputHistory, new Relation(intake, receive));
        if (!accumulator.TryReadReceipt(receive, out _, out _, out _, out _, out var blocked,
                out var remainder, out _, out var pending))
        { Unresolved = Append(Unresolved, receive); return receive; }
        var retained = S([.. E(blocked), .. E(remainder)]);
        if (E(retained).Length != 0)
            RetainedPackets = Append(RetainedPackets, new Relation(InputPort, new Relation(receive, retained)));
        if (E(pending).Length != 0 || !SelectedRow(out var row))
        { Unresolved = Append(Unresolved, receive); return receive; }
        var routes = S(new Relation(accumulator.Ready, row.Right), new Relation(accumulator.Waiting, hold));
        var question = accumulator.Ask(routes, refinement);
        if (!QualitativeQuestion.TryReadAnswer(question, out var answer) || !ReferenceEquals(answer, row.Right))
            return new Relation(intake, new Relation(receive, question));
        if (!Drawing.TryReadState(Drawing.State, out var previous, out _, out _, out _, out _, out _, out _))
        { Unresolved = Append(Unresolved, Drawing.State); return receive; }
        var before = Drawing.State;
        var offer = Drawing.Offer(Drawing.Source, eventIdentity, previous, S(row.Right),
            new Relation(Surface, new Relation(ControllerState, new Relation(question, receive))));
        var request = new Relation(new Relation(Surface, new Relation(eventIdentity, row)),
            new Relation(new Relation(before, offer), new Relation(question, new Relation(ControllerState,
                new Relation(accumulator.State, CurrentCycleReceipts)))));
        Pending = S(request); return request;
    }

    public bool TryReadRequest(Relation request, out Relation action, out Relation beforeDrawing, out Relation offer)
    {
        action = beforeDrawing = offer = null!;
        if (request.Left is not Relation { Right: Relation selection } context || !ReferenceEquals(context.Left, Surface) ||
            selection.Right is not Relation row || !E(ActionRows).Any(actual => ReferenceEquals(actual, row)) ||
            row.Right is not Relation selected || request.Right is not Relation { Left: Relation effect, Right: Relation gate } ||
            effect.Left is not Relation before || effect.Right is not Relation offered || gate.Left is not Relation ||
            gate.Right is not Relation { Left: Relation controllerBefore, Right: Relation { Left: Relation, Right: Relation priorCycle } } ||
            !Closed(priorCycle) ||
            !Controller.TryReadState(controllerBefore, out _, out _, out var phases) || !Closed(phases) || E(phases) is not [var phase] ||
            !ReferenceEquals(phase, row.Left) || offered.Left is not Relation { Right: Relation events } ||
            !Same(events.Left, selection.Left) || offered.Right is not Relation { Right: Relation body } ||
            body.Left is not Relation support || !Closed(support) || E(support) is not [var actual] || !ReferenceEquals(actual, selected)) return false;
        action = selected; beforeDrawing = before; offer = offered; return true;
    }

    /// <summary>Authenticates an exact pending effect and preflights its phase
    /// edge before committing anything. Failed attempts retain both the request
    /// and its packet; an accepted prefix is never replayed.</summary>
    public Relation Acknowledge(Relation request, Relation drawingReceipt)
    {
        Relation phaseSteps = S(), releases = S(), completions = S(), problems = S();
        if (E(Pending) is not [var actualRequest] || !ReferenceEquals(actualRequest, request) ||
            !TryReadRequest(request, out var action, out var before, out var offer) || !ReferenceEquals(before, Drawing.State) ||
            !TryReadGate(request, out var question, out var controllerBefore, out var reservoirBefore, out var priorCycle) ||
            !ReferenceEquals(controllerBefore, ControllerState) || !ReferenceEquals(reservoirBefore, accumulator.State) ||
            !ReferenceEquals(priorCycle, CurrentCycleReceipts) || !ValidCyclePrefix(priorCycle, controllerBefore) ||
            !Drawing.TryReadStep(drawingReceipt, out var receiptBefore, out var receiptOffer, out _, out _, out _, out _) ||
            !ReferenceEquals(before, receiptBefore) || !ReferenceEquals(offer, receiptOffer) ||
            !Drawing.TryReadAccepted(drawingReceipt, out _, out _) || !SelectedRow(out var selected) || !ReferenceEquals(selected.Right, action) ||
            !QualitativeQuestion.TryReadAnswer(question, out var answer) || !ReferenceEquals(answer, action))
            return Failed(new Relation(request, drawingReceipt));
        if (!Controller.TryReadState(ControllerState, out _, out var previous, out _) ||
            request.Left is not Relation { Right: Relation eventAndRow }) return Failed(ControllerState);
        var phaseStep = Controller.Step(ControllerState,
            Controller.Drive(source, eventAndRow.Left, previous, new Relation(request, drawingReceipt)));
        if (!Controller.TryReadAccepted(phaseStep, out _, out _, out var nextPhase) ||
            !Controller.TryReadStep(phaseStep, out _, out _, out var controllerAfter, out _, out _, out _)) return Failed(phaseStep);
        // The held readiness proof is still current. A fresh read guards the
        // material operation before the drawing patch is committed.
        var currentQuestion = accumulator.Ask(S(new Relation(accumulator.Ready, action), new Relation(accumulator.Waiting, hold)), refinement);
        if (!QualitativeQuestion.TryReadAnswer(currentQuestion, out var currentAnswer) || !ReferenceEquals(currentAnswer, action) ||
            !Drawing.TryCommit(drawingReceipt)) return Failed(currentQuestion);
        var release = accumulator.Fire(new Placeholder(), question);
        if (!accumulator.TryReadReceipt(release, out _, out _, out _, out _, out _, out _, out var drained, out var unresolved) ||
            E(drained).Length != 1 || E(unresolved).Length != 0)
            throw new InvalidOperationException("A preflighted current single-site release lost its native contract.");
        ControllerState = controllerAfter; Pending = S();
        ReleasedPackets = Append(ReleasedPackets, new Relation(OutputPort, drained));
        CurrentCycleReceipts = Append(CurrentCycleReceipts, drawingReceipt);
        phaseSteps = S(phaseStep); releases = S(release);
        if (ReferenceEquals(nextPhase, E(Controller.Domain)[0]))
        {
            var completion = new Relation(new Relation(CompletionPort, eventAndRow.Left), new Relation(CurrentCycleReceipts, phaseStep));
            CompletionEvents = Append(CompletionEvents, completion); completions = S(completion); CurrentCycleReceipts = S();
        }
        return Finish();

        Relation Failed(IElement problem)
        { problems = S(problem); Unresolved = Append(Unresolved, problem); return Finish(); }
        Relation Finish()
        {
            var result = new Relation(new Relation(new Relation(Surface, request), drawingReceipt),
                new Relation(new Relation(phaseSteps, releases), new Relation(completions, problems)));
            Timeline = Append(Timeline, result); return result;
        }
    }

    public bool TryReadAccepted(Relation acknowledgment, out Relation drawingReceipt, out Relation phaseStep,
        out Relation release, out Relation completions)
    {
        drawingReceipt = phaseStep = release = completions = null!;
        if (acknowledgment.Left is not Relation { Left: Relation context, Right: Relation effect } ||
            !ReferenceEquals(context.Left, Surface) || context.Right is not Relation request ||
            !TryReadRequest(request, out _, out var before, out var offered) ||
            acknowledgment.Right is not Relation { Left: Relation steps, Right: Relation outcome } ||
            steps.Left is not Relation phases || !Closed(phases) || E(phases) is not [Relation step] ||
            steps.Right is not Relation material || !Closed(material) || E(material) is not [Relation drain] ||
            outcome.Left is not Relation complete || !Closed(complete) || outcome.Right is not Relation pending ||
            !Closed(pending) || E(pending).Length != 0 || !Drawing.TryReadAccepted(effect, out _, out _) ||
            !Drawing.TryReadStep(effect, out var actualBefore, out var actualOffer, out _, out _, out _, out _) ||
            !ReferenceEquals(actualBefore, before) || !ReferenceEquals(actualOffer, offered) ||
            !Controller.TryReadAccepted(step, out _, out _, out _) ||
            !TryReadGate(request, out var question, out var controllerBefore, out var reservoirBefore, out var priorCycle) ||
            !Controller.TryReadStep(step, out var phaseBefore, out var phaseDrive, out _, out _, out _, out _) ||
            !ReferenceEquals(phaseBefore, controllerBefore) || !ValidCyclePrefix(priorCycle, controllerBefore) ||
            !Controller.TryReadDrive(phaseDrive, out _, out _, out _, out var cause) ||
            cause is not Relation route || !ReferenceEquals(route.Left, request) || !ReferenceEquals(route.Right, effect) ||
            !accumulator.TryReadReceipt(drain, out var actualReservoirBefore, out var releaseRequest, out _, out _, out _, out _, out var drained, out var residual) ||
            !ReferenceEquals(actualReservoirBefore, reservoirBefore) || releaseRequest.Right is not Relation permission ||
            !ReferenceEquals(permission.Right, question) || E(drained).Length != 1 || E(residual).Length != 0 ||
            !ValidCompletions(complete, request, effect, step, priorCycle)) return false;
        drawingReceipt = effect; phaseStep = step; release = drain; completions = complete; return true;
    }

    public Relation Pulse() => Drive(new Placeholder(), S(new Placeholder()));
    public Relation Drive(IElement eventIdentity, Relation packets)
    {
        var request = Propose(eventIdentity, packets);
        return TryReadRequest(request, out _, out var before, out var offer)
            ? Acknowledge(request, Drawing.Step(before, offer)) : request;
    }

    /// <summary>Current material ownership partition; retained history and
    /// completion notifications do not duplicate packet ownership.</summary>
    public Relation Account()
    {
        var pending = new List<IElement>(); var active = new List<IElement>();
        if (accumulator.TryReadState(out _, out var placements, out _, out _))
            active.AddRange(E(placements).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, InputPort)));
        else pending.Add(accumulator.State);
        var retained = E(RetainedPackets).Cast<Relation>().SelectMany(row => E((Relation)((Relation)row.Right).Right)
            .Select(packet => (IElement)new Relation(packet, row.Left))).ToArray();
        var released = E(ReleasedPackets).Cast<Relation>().SelectMany(row => E((Relation)row.Right)
            .Select(packet => (IElement)new Relation(packet, row.Left))).ToArray();
        var partition = active.Concat(retained).Concat(released).Cast<Relation>().ToArray();
        foreach (var packet in E(ImportedPackets))
            if (partition.Count(row => Same(row.Left, packet)) != 1) pending.Add(new Relation(packet, S(partition)));
        foreach (var row in partition)
            if (!E(ImportedPackets).Any(packet => Same(packet, row.Left))) pending.Add(row);
        return new Relation(new Relation(Surface, ImportedPackets),
            new Relation(S(active.ToArray()), new Relation(S(retained), new Relation(S(released), S(pending.ToArray())))));
    }

    private bool SelectedRow(out Relation row)
    {
        row = null!;
        if (!Controller.TryReadState(ControllerState, out _, out _, out var phases) || E(phases) is not [var phase]) return false;
        var matches = E(ActionRows).OfType<Relation>().Where(item => ReferenceEquals(item.Left, phase)).ToArray();
        if (matches.Length != 1) return false; row = matches[0]; return true;
    }

    private static bool TryReadGate(Relation request, out Relation question, out Relation controllerBefore,
        out Relation reservoirBefore, out Relation priorCycle)
    {
        question = controllerBefore = reservoirBefore = priorCycle = null!;
        if (request.Right is not Relation { Right: Relation { Left: Relation q, Right: Relation gate } } ||
            gate.Left is not Relation controller || gate.Right is not Relation { Left: Relation reservoir, Right: Relation cycle } ||
            !Closed(cycle)) return false;
        question = q; controllerBefore = controller; reservoirBefore = reservoir; priorCycle = cycle; return true;
    }

    // Retained cycle receipts are authenticated by the actual phase state's
    // producer evidence, rather than by membership in a hosted history list.
    private bool ValidCyclePrefix(Relation prefix, Relation controllerBefore)
    {
        if (!Closed(prefix) || !Controller.TryReadState(controllerBefore, out _, out _, out var phases) ||
            !Closed(phases) || E(phases) is not [var phase]) return false;
        var rows = E(ActionRows).Cast<Relation>().ToArray(); var index = Array.FindIndex(rows, row => ReferenceEquals(row.Left, phase));
        var receipts = E(prefix);
        if (index < 0 || receipts.Length != index) return false;
        var current = controllerBefore;
        for (var i = receipts.Length - 1; i >= 0; i--)
        {
            if (receipts[i] is not Relation effect || !Drawing.TryReadAccepted(effect, out _, out _) ||
                current.Right is not Relation { Right: Relation { Right: Relation provenance } } ||
                provenance.Left is not Relation previous || provenance.Right is not Relation { Left: Relation drive } ||
                !Controller.TryReadDrive(drive, out var owner, out var occurrence, out _, out var evidence) || !Same(owner, source) ||
                evidence is not Relation { Left: Relation request, Right: Relation actualEffect } || !ReferenceEquals(actualEffect, effect) ||
                !TryReadRequest(request, out var action, out var drawingBefore, out var offer) || !ReferenceEquals(action, rows[i].Right) ||
                !TryReadGate(request, out _, out var phaseBefore, out _, out var earlier) || !ReferenceEquals(previous, phaseBefore) ||
                E(earlier).Length != i || !E(earlier).Select((item, offset) => ReferenceEquals(item, receipts[offset])).All(equal => equal) ||
                !Drawing.TryReadStep(effect, out var actualBefore, out var actualOffer, out var after, out _, out _, out _) ||
                !ReferenceEquals(actualBefore, drawingBefore) || !ReferenceEquals(actualOffer, offer) ||
                !Drawing.TryReadState(after, out var effectEvent, out _, out _, out _, out _, out _, out _) || !Same(effectEvent, occurrence) ||
                !Controller.TryReadState(current, out _, out var phaseEvent, out var currentPhases) || !Same(phaseEvent, occurrence) ||
                E(currentPhases) is not [var currentPhase] || !ReferenceEquals(currentPhase, rows[i + 1].Left)) return false;
            var checkedStep = Controller.Step(previous, drive);
            if (!Controller.TryReadAccepted(checkedStep, out _, out _, out var checkedPhase) || !ReferenceEquals(checkedPhase, currentPhase)) return false;
            current = previous;
        }
        return Controller.TryReadState(current, out _, out _, out var start) && E(start) is [var first] && ReferenceEquals(first, rows[0].Left);
    }

    private bool ValidCompletions(Relation completions, Relation request, Relation effect, Relation phaseStep, Relation prefix)
    {
        if (!Closed(completions) || !Controller.TryReadAccepted(phaseStep, out _, out var eventIdentity, out var next)) return false;
        if (!ReferenceEquals(next, E(Controller.Domain)[0])) return E(completions).Length == 0;
        if (E(completions) is not [Relation completion] ||
            completion.Left is not Relation occurrence || !ReferenceEquals(occurrence.Left, CompletionPort) || !Same(occurrence.Right, eventIdentity) ||
            request.Left is not Relation { Right: Relation selection } || !Same(selection.Left, eventIdentity) ||
            completion.Right is not Relation { Left: Relation receipts } proof || !ReferenceEquals(proof.Right, phaseStep) || !Closed(receipts)) return false;
        var expected = E(prefix).Append(effect).ToArray(); var actual = E(receipts);
        return actual.Length == E(Actions).Length && actual.Length == expected.Length &&
            actual.Select((item, index) => ReferenceEquals(item, expected[index])).All(equal => equal);
    }
    private bool Fresh(Relation packets)
    {
        var seen = E(ImportedPackets).ToList();
        foreach (var packet in E(packets))
        { if (seen.Any(prior => Same(prior, packet))) return false; seen.Add(packet); }
        return true;
    }
    private static Relation Append(Relation scope, IElement value) => S([.. E(scope), value]);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation c && b is CarrierLocation d && c.Carrier.SameIdentityAs(d.Carrier) && c.SameTickRefAs(d);
}
