using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An exposed input/completion contract. Names describe a diagram;
/// actual action rows, child ports, phase edges and receipts carry execution.</summary>
public abstract class PrimitiveFlowNode
{
    public abstract string Name { get; }
    public abstract QualitativeDrawingReceiver Drawing { get; }
    public abstract Relation Surface { get; }
    public abstract Relation InputPort { get; }
    public abstract Relation CompletionPort { get; }
    public abstract Relation Pending { get; }
    public abstract Relation Unresolved { get; }
    public abstract Relation Timeline { get; }
    public abstract Relation CompletionEvents { get; }
    public abstract bool IsAtBoundary { get; }
    public abstract Relation Propose(IElement eventIdentity, Relation packets);
    public abstract Relation Acknowledge(Relation request, Relation receipt);
    public abstract bool TryReadRequest(Relation request, out Relation beforeDrawing, out Relation offer);
    public abstract bool TryReadAccepted(Relation acknowledgment, out Relation completions);
    public abstract Relation Account();
    public abstract Relation Capture();
}

/// <summary>The established one-packet acknowledged actuator sequence, exposed
/// as a reusable child. A return observes accepted work without copying its packet.</summary>
public sealed class PrimitiveFlowLeaf : PrimitiveFlowNode
{
    public AcknowledgedPatternSequence Sequence { get; }
    public override string Name => Sequence.Name;
    public override QualitativeDrawingReceiver Drawing => Sequence.Drawing;
    public override Relation Surface => Sequence.Surface;
    public override Relation InputPort => Sequence.InputPort;
    public override Relation CompletionPort => Sequence.CompletionPort;
    public override Relation Pending => Sequence.Pending;
    public override Relation Unresolved => Sequence.Unresolved;
    public override Relation Timeline => Sequence.Timeline;
    public override Relation CompletionEvents => Sequence.CompletionEvents;
    public override bool IsAtBoundary => Sequence.IsAtBoundary;

    public PrimitiveFlowLeaf(string name, QualitativeDrawingReceiver drawing, Relation closedActions) =>
        Sequence = new(name, drawing, closedActions);
    public override Relation Propose(IElement eventIdentity, Relation packets) => Sequence.Propose(eventIdentity, packets);
    public override Relation Acknowledge(Relation request, Relation receipt) => Sequence.Acknowledge(request, receipt);
    public override bool TryReadRequest(Relation request, out Relation beforeDrawing, out Relation offer) =>
        Sequence.TryReadRequest(request, out _, out beforeDrawing, out offer);
    public override bool TryReadAccepted(Relation acknowledgment, out Relation completions) =>
        Sequence.TryReadAccepted(acknowledgment, out _, out _, out _, out completions);
    public override Relation Account() => Sequence.Account();
    public override Relation Capture() => new(Surface, new Relation(
        new Relation(Sequence.ControllerState, Sequence.ReservoirState), new Relation(
            new Relation(Sequence.InputHistory, Timeline), new Relation(Pending,
                new Relation(CompletionEvents, new Relation(Unresolved, Account()))))));
}

/// <summary>A serial, nestable call/return circuit over advertised child ports.
/// Each occurrence of a reused child has its own phase and native call row.
/// Parent permission is observational; material is imported only by a leaf.</summary>
public sealed class PrimitiveFlowGroup : PrimitiveFlowNode
{
    // call row = phase -> (actual child input -> actual child completion port)
    // Surface = (owner -> Closed(child surfaces in occurrence order)) ->
    //           ((input -> completion) -> (controller -> (call rows -> law)))
    // request = (Surface -> (event -> call row)) ->
    //           (actual child request -> (controller-before -> cycle-return-prefix))
    // return = (actual call row -> accepted child acknowledgment) -> parent-phase-step
    // completion = (completion-port -> event) -> (Closed(cycle returns) -> phase-step)
    // acknowledgment = ((Surface -> request) -> child acknowledgment) ->
    //                  ((Closed(parent step) -> Closed(completions)) -> Closed(problems))
    // No host phase index selects a child: the current phase binds the exposed
    // call row through the finite relation law. Indexes below only verify order.
    private readonly IElement owner = new Placeholder();
    private readonly IElement refinement = new Placeholder();
    public override string Name { get; }
    public override QualitativeDrawingReceiver Drawing { get; }
    public IReadOnlyList<PrimitiveFlowNode> Children { get; }
    public PhaseRecurrence Controller { get; }
    public Relation ControllerState { get; private set; }
    public Relation CallRows { get; }
    public Relation DispatchLaw { get; }
    public override Relation InputPort { get; }
    public override Relation CompletionPort { get; }
    public override Relation Surface { get; }
    private Relation pending = S(), unresolved = S(), timeline = S(), completionEvents = S();
    public override Relation Pending => pending;
    public override Relation Unresolved => unresolved;
    public override Relation Timeline => timeline;
    public override Relation CompletionEvents => completionEvents;
    public Relation CurrentCycleReturns { get; private set; } = S();
    public Relation InputHistory { get; private set; } = S();
    public Relation ProposalEvents { get; private set; } = S();
    public override bool IsAtBoundary => E(Pending).Length == 0 && E(CurrentCycleReturns).Length == 0 &&
        Controller.TryReadState(ControllerState, out _, out _, out var support) && E(support) is [var phase] &&
        ReferenceEquals(phase, E(Controller.Domain)[0]) && Selected(out _, out var child, out _) && child.IsAtBoundary;

    public PrimitiveFlowGroup(string name, params PrimitiveFlowNode[] children)
    {
        if (children.Length == 0 || children.Any(child => child is null || !child.IsAtBoundary) ||
            children.Any(child => !ReferenceEquals(child.Drawing, children[0].Drawing)))
            throw new ArgumentException("A group needs nonempty, boundary-ready children sharing one advertised drawing actuator.", nameof(children));
        Name = name; Drawing = children[0].Drawing; Children = Array.AsReadOnly((PrimitiveFlowNode[])children.Clone());
        var phases = children.Select(_ => (IElement)new Placeholder()).ToArray();
        var domain = S(phases);
        Controller = new(domain, S(phases.Select((phase, index) =>
            (IElement)new Relation(phase, phases[(index + 1) % phases.Length])).ToArray()));
        ControllerState = Controller.Initial(owner, new Placeholder(), S(phases[0]));
        CallRows = S(children.Select((child, index) => (IElement)new Relation(phases[index],
            new Relation(child.InputPort, child.CompletionPort))).ToArray());
        var rows = E(CallRows).Cast<Relation>().ToArray();
        // Domain occurrences are distinct call rows even when ports are reused.
        DispatchLaw = FiniteLaw.Create(domain, domain, S(rows), rows.Select(row =>
            FiniteLaw.Row(row.Left, row.Left, S(row))).ToArray());
        InputPort = new Relation(owner, Controller.Surface);
        CompletionPort = new Relation(Controller.Surface, phases[0]);
        Surface = new Relation(new Relation(owner, S(children.Select(child => (IElement)child.Surface).ToArray())),
            new Relation(new Relation(InputPort, CompletionPort),
                new Relation(Controller.Surface, new Relation(CallRows, DispatchLaw))));
    }

    public override Relation Propose(IElement eventIdentity, Relation packets)
    {
        var intake = new Relation(new Relation(InputPort, eventIdentity), packets);
        if (E(Pending).Length != 0 || E(ProposalEvents).Any(prior => Same(prior, eventIdentity)) ||
            !FreshImports(packets) || !Selected(out var row, out var child, out var question))
        { unresolved = Append(Unresolved, new Relation(intake, Pending)); return intake; }
        ProposalEvents = Append(ProposalEvents, eventIdentity);
        var childRequest = child.Propose(eventIdentity, packets);
        InputHistory = Append(InputHistory, new Relation(intake, new Relation(row, new Relation(question, childRequest))));
        if (!child.TryReadRequest(childRequest, out _, out _)) return new Relation(intake, childRequest);
        var request = new Relation(new Relation(Surface, new Relation(eventIdentity, row)),
            new Relation(childRequest, new Relation(ControllerState, CurrentCycleReturns)));
        pending = S(request); return request;
    }

    public override bool TryReadRequest(Relation request, out Relation beforeDrawing, out Relation offer)
    {
        beforeDrawing = offer = null!;
        return ReadRequest(request, out _, out var child, out var childRequest, out _, out _, out _) &&
            child.TryReadRequest(childRequest, out beforeDrawing, out offer);
    }

    public override Relation Acknowledge(Relation request, Relation receipt)
    {
        Relation childAcknowledgment = new(request, receipt), steps = S(), completions = S(), problems = S();
        if (E(Pending) is not [var actual] || !ReferenceEquals(actual, request) ||
            !ReadRequest(request, out var row, out var child, out var childRequest,
                out var controllerBefore, out var prefix, out var occurrence) ||
            !ReferenceEquals(controllerBefore, ControllerState) || !ReferenceEquals(prefix, CurrentCycleReturns) ||
            !ValidPrefix(prefix, controllerBefore) || !Selected(out var selected, out var selectedChild, out _) ||
            !ReferenceEquals(row, selected) || !ReferenceEquals(child, selectedChild))
            return Failed(new Relation(request, receipt));
        childAcknowledgment = child.Acknowledge(childRequest, receipt);
        if (!child.TryReadAccepted(childAcknowledgment, out var childCompletions)) return Failed(childAcknowledgment);
        if (E(childCompletions).Length == 0) { pending = S(); return Finish(); }
        if (E(childCompletions) is not [Relation completion] ||
            completion.Left is not Relation output || !ReferenceEquals(output.Left, child.CompletionPort) ||
            !Controller.TryReadState(controllerBefore, out _, out var previous, out _)) return Failed(childCompletions);
        var cause = new Relation(row, new Relation(childAcknowledgment, completion));
        var step = Controller.Step(controllerBefore, Controller.Drive(owner, occurrence, previous, cause));
        if (!Controller.TryReadAccepted(step, out _, out _, out var next) ||
            !Controller.TryReadStep(step, out _, out _, out var after, out _, out _, out _)) return Failed(step);
        var returned = new Relation(new Relation(row, childAcknowledgment), step);
        var returns = Append(prefix, returned);
        ControllerState = after; pending = S(); steps = S(step); CurrentCycleReturns = returns;
        if (ReferenceEquals(next, E(Controller.Domain)[0]))
        {
            var completed = new Relation(new Relation(CompletionPort, occurrence), new Relation(returns, step));
            completions = S(completed); completionEvents = Append(CompletionEvents, completed); CurrentCycleReturns = S();
        }
        return Finish();

        Relation Failed(IElement problem)
        { problems = S(problem); unresolved = Append(Unresolved, problem); return Finish(); }
        Relation Finish()
        {
            var acknowledgment = new Relation(new Relation(new Relation(Surface, request), childAcknowledgment),
                new Relation(new Relation(steps, completions), problems));
            timeline = Append(Timeline, acknowledgment); return acknowledgment;
        }
    }

    public override bool TryReadAccepted(Relation acknowledgment, out Relation completions)
    {
        completions = null!;
        if (acknowledgment.Left is not Relation { Left: Relation context, Right: Relation childAcknowledgment } ||
            !ReferenceEquals(context.Left, Surface) || context.Right is not Relation request ||
            !ReadRequest(request, out var row, out var child, out var childRequest,
                out var before, out var prefix, out var occurrence) ||
            !child.TryReadAccepted(childAcknowledgment, out var childCompletions) ||
            !Acknowledges(childAcknowledgment, child.Surface, childRequest) ||
            acknowledgment.Right is not Relation { Left: Relation result, Right: Relation problems } ||
            result.Left is not Relation steps || result.Right is not Relation returned || !Closed(steps) ||
            !Closed(returned) || !Closed(problems) || E(problems).Length != 0 || !ValidPrefix(prefix, before)) return false;
        if (E(childCompletions).Length == 0)
        { if (E(steps).Length != 0 || E(returned).Length != 0) return false; completions = returned; return true; }
        if (E(childCompletions) is not [Relation completion] || E(steps) is not [Relation step] ||
            !ValidReturn(new Relation(new Relation(row, childAcknowledgment), step), out var actualBefore, out var after) ||
            !ReferenceEquals(actualBefore, before) ||
            !Controller.TryReadState(after, out _, out _, out var nextPhases) || E(nextPhases) is not [var next]) return false;
        if (!ReferenceEquals(next, E(Controller.Domain)[0]))
        { if (E(returned).Length != 0) return false; completions = returned; return true; }
        if (E(returned) is not [Relation completed] || completed.Left is not Relation portAndEvent ||
            !ReferenceEquals(portAndEvent.Left, CompletionPort) || !Same(portAndEvent.Right, occurrence) ||
            completed.Right is not Relation { Left: Relation cycle, Right: var last } || !ReferenceEquals(last, step) ||
            !Closed(cycle) || E(cycle).Length != Children.Count) return false;
        var actualCycle = E(cycle); var expectedPrefix = E(prefix);
        if (actualCycle.Length != expectedPrefix.Length + 1 ||
            expectedPrefix.Where((item, index) => !ReferenceEquals(item, actualCycle[index])).Any() ||
            actualCycle[^1] is not Relation endReturn || endReturn.Left is not Relation endCall ||
            !ReferenceEquals(endCall.Left, row) || !ReferenceEquals(endCall.Right, childAcknowledgment) ||
            !ReferenceEquals(endReturn.Right, step)) return false;
        completions = returned; return true;
    }

    public override Relation Account()
    {
        var accounts = new List<IElement>(); var complete = true;
        var seenChildren = new HashSet<PrimitiveFlowNode>(ReferenceEqualityComparer.Instance);
        var seenOwners = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var child in Children)
        {
            if (!seenChildren.Add(child)) continue;
            var publication = child.Account(); IElement[] rows;
            if (StructuralConstraint.TryReadScope(publication, out rows, out var seam, out var tail))
                complete &= seam.Equals(tail);
            else rows = [publication];
            foreach (var item in rows)
            {
                if (item is not Relation { Left: Relation header } account || header.Right is not Relation imports || !Closed(imports))
                { complete = false; accounts.Add(new Relation(child.Surface, item)); continue; }
                // A reused child's account, or the same descendant advertised
                // by two children, is one ownership surface. No parent traverses
                // a child's private hierarchy, reservoir or instruction state.
                if (seenOwners.Add(header.Left)) accounts.Add(account);
            }
        }
        return complete ? S(accounts.ToArray()) : StructuralConstraint.Open(accounts.ToArray());
    }
    public override Relation Capture() => new(Surface, new Relation(new Relation(ControllerState, CurrentCycleReturns),
        new Relation(S(Children.Select(child => (IElement)child.Capture()).ToArray()),
            new Relation(new Relation(InputHistory, ProposalEvents), new Relation(Timeline,
                new Relation(Pending, new Relation(CompletionEvents, new Relation(Unresolved, Account()))))))));

    private bool Selected(out Relation row, out PrimitiveFlowNode child, out Relation question)
    {
        row = question = null!; child = null!;
        if (!Controller.TryReadState(ControllerState, out _, out _, out var phases) || E(phases) is not [var phase]) return false;
        var read = FiniteLaw.Evaluate(DispatchLaw, S(phase), S(phase));
        question = QualitativeQuestion.Ask(read, S(E(CallRows).Select(item => (IElement)new Relation(item, item)).ToArray()), refinement);
        if (!QualitativeQuestion.TryReadAnswer(question, out var answer) || answer is not Relation selected) return false;
        var index = Array.FindIndex(E(CallRows), item => ReferenceEquals(item, selected));
        if (index < 0) return false;
        row = selected; child = Children[index]; return true;
    }

    private bool ReadRequest(Relation request, out Relation row, out PrimitiveFlowNode child,
        out Relation childRequest, out Relation before, out Relation prefix, out IElement occurrence)
    {
        row = childRequest = before = prefix = null!; child = null!; occurrence = null!;
        if (request.Left is not Relation { Right: Relation selection } context || !ReferenceEquals(context.Left, Surface) ||
            selection.Right is not Relation call || request.Right is not Relation { Left: Relation requested, Right: Relation gate } ||
            gate.Left is not Relation controller || gate.Right is not Relation cycle || !Closed(cycle) ||
            !Controller.TryReadState(controller, out _, out _, out var phases) || E(phases) is not [var phase] ||
            !ReferenceEquals(phase, call.Left)) return false;
        var index = Array.FindIndex(E(CallRows), item => ReferenceEquals(item, call));
        if (index < 0 || call.Right is not Relation ports || !ReferenceEquals(ports.Left, Children[index].InputPort) ||
            !ReferenceEquals(ports.Right, Children[index].CompletionPort) ||
            requested.Left is not Relation { Right: Relation childSelection } || !Same(childSelection.Left, selection.Left) ||
            !Children[index].TryReadRequest(requested, out _, out _)) return false;
        row = call; child = Children[index]; childRequest = requested; before = controller; prefix = cycle; occurrence = selection.Left; return true;
    }

    private bool ValidPrefix(Relation prefix, Relation controllerBefore)
    {
        if (!Closed(prefix) || !Controller.TryReadState(controllerBefore, out _, out _, out var phases) || E(phases) is not [var phase]) return false;
        var rows = E(CallRows); var index = Array.FindIndex(rows, item => ReferenceEquals(((Relation)item).Left, phase));
        var returns = E(prefix);
        if (index < 0 || returns.Length != index) return false;
        var current = controllerBefore;
        for (var i = returns.Length - 1; i >= 0; i--)
        {
            if (returns[i] is not Relation returned || returned.Left is not Relation call || !ReferenceEquals(call.Left, rows[i]) ||
                !ValidReturn(returned, out var before, out var after) || !ReferenceEquals(after, current)) return false;
            current = before;
        }
        return Controller.TryReadState(current, out _, out _, out var first) && E(first) is [var initial] &&
            ReferenceEquals(initial, ((Relation)rows[0]).Left);
    }

    private bool ValidReturn(Relation returned, out Relation before, out Relation after)
    {
        before = after = null!;
        if (returned.Left is not Relation { Left: Relation row, Right: Relation acknowledgment } || returned.Right is not Relation step) return false;
        var index = Array.FindIndex(E(CallRows), item => ReferenceEquals(item, row));
        if (index < 0 || !Children[index].TryReadAccepted(acknowledgment, out var completions) ||
            E(completions) is not [Relation completion] || completion.Left is not Relation portAndEvent ||
            !ReferenceEquals(portAndEvent.Left, Children[index].CompletionPort) ||
            !Controller.TryReadAccepted(step, out _, out var phaseEvent, out _) || !Same(phaseEvent, portAndEvent.Right) ||
            !Controller.TryReadStep(step, out var old, out var drive, out var current, out _, out _, out _) ||
            !Controller.TryReadDrive(drive, out var source, out _, out _, out var evidence) || !Same(source, owner) ||
            evidence is not Relation { Left: var call, Right: Relation cause } || !ReferenceEquals(call, row) ||
            !ReferenceEquals(cause.Left, acknowledgment) || !ReferenceEquals(cause.Right, completion) ||
            !Controller.TryReadState(old, out _, out _, out var phases) || E(phases) is not [var phase] || !ReferenceEquals(phase, row.Left)) return false;
        before = old; after = current; return true;
    }

    private static bool Acknowledges(Relation acknowledgment, Relation surface, Relation request) =>
        acknowledgment.Left is Relation { Left: Relation context } &&
        ReferenceEquals(context.Left, surface) && ReferenceEquals(context.Right, request);
    private bool FreshImports(Relation packets)
    {
        if (!Closed(packets)) return false;
        var accounts = Account(); if (!Closed(accounts)) return false;
        var seen = new List<IElement>();
        foreach (var item in E(accounts))
        {
            // Read the child's advertised ownership account, rather than its
            // reservoir or private instruction state.
            if (item is not Relation { Left: Relation { Right: Relation imports } }) return false;
            seen.AddRange(E(imports));
        }
        foreach (var packet in E(packets))
        { if (seen.Any(prior => Same(prior, packet))) return false; seen.Add(packet); }
        return true;
    }
    private static Relation Append(Relation scope, IElement item) => S([.. E(scope), item]);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) ||
        a is Placeholder x && b is Placeholder y && x.Equals(y) ||
        a is CarrierLocation c && b is CarrierLocation d && c.Carrier.SameIdentityAs(d.Carrier) && c.SameTickRefAs(d);
}
