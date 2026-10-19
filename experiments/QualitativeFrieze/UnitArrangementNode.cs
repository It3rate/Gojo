using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>The entire operational contract needed by a template or neighbour.
/// Callers may wrap this contract without exposing programs or drawing internals.</summary>
public interface IUnitArrangementEndpoint
{
    Relation Surface { get; }
    IElement Head { get; }
    Relation Advertisement();
    Relation Propose(IElement signal, IElement cause);
    Relation Commit(Relation proposal);
}

/// <summary>A persistent actuator with a finite native replacement law. A
/// publication is head-bound and appears only after acknowledged completion;
/// replacement edits the active focus while retaining all prior ink/history.</summary>
public sealed class UnitArrangementNode : IUnitArrangementEndpoint
{
    public string Name { get; }
    public QualitativeDrawingReceiver Drawing { get; }
    public IReadOnlyList<UnitArrangementProgram> Programs { get; }
    public Relation PlacementWord { get; }
    public Relation Policy { get; }
    public QualitativePatternTransform? Transform { get; }
    public Relation Surface { get; }
    public IElement Head => Current.Identity;
    public UnitArrangementRevision Current { get; private set; } = null!;
    public PrimitiveFlowLeaf PlacementExecution { get; }
    public Relation Revisions { get; private set; } = S();
    public Relation Timeline { get; private set; } = S();
    public Relation Pending { get; private set; } = S();
    public Relation Unresolved { get; private set; } = S();
    private readonly IElement refinement = new Placeholder();

    public UnitArrangementNode(string name, IReadOnlyList<UnitArrangementProgram> programs,
        UnitArrangementProgram initial, Relation placementWord, Relation? policy = null,
        QualitativePatternTransform? transform = null)
    {
        if (programs.Count == 0 || !programs.Any(program => ReferenceEquals(program, initial)) ||
            programs.Any(program => !ReferenceEquals(program.Frame, initial.Frame)))
            throw new ArgumentException("An initial program must belong to the exposed finite program domain.", nameof(initial));
        Name = name; Programs = Array.AsReadOnly(programs.ToArray()); PlacementWord = placementWord; Transform = transform;
        Drawing = new(transform?.Target ?? initial.Frame);
        var domain = S(programs.Select(program => (IElement)program.Surface).ToArray());
        Policy = policy ?? FiniteLaw.Create(domain, S(), domain);
        Surface = new(new Relation(new Placeholder(), domain), new Relation(new Relation(Drawing.Surface, placementWord),
            new Relation(Policy, transform?.Surface ?? S())));
        PlacementExecution = new(name + " / placement", Drawing, S(Drawing.Jump(placementWord), Drawing.Anchor()));
        if (!Drive(PlacementExecution, out _)) throw new ArgumentException("A placement needs a complete word in the target frame.", nameof(placementWord));
        if (!Build(initial, Surface, out var revision)) throw new ArgumentException("An initial program must be structurally complete and executable.", nameof(initial));
        Current = revision; Revisions = S(revision.Surface);
    }

    // advertisement = (public surface -> active head) -> (frame -> publications)
    // publication = role -> (active head -> (site binding -> boundary proof))
    public Relation Advertisement() => new(new Relation(Surface, Head), new Relation(Drawing.Frame.Surface, Current.Ports));

    public Relation Propose(IElement signal, IElement cause)
    {
        if (E(Pending).Length != 0) return Refuse(new Relation(signal, cause));
        var read = FiniteLaw.Evaluate(Policy, S(Current.Program.Surface), S(signal));
        var routes = S(Programs.Select(program => (IElement)new Relation(program.Surface, program.Surface)).ToArray());
        var question = QualitativeQuestion.Ask(read, routes, refinement);
        if (!QualitativeQuestion.TryReadAnswer(question, out var selected) || Program(selected) is null)
            return Refuse(new Relation(new Relation(signal, cause), question));
        var proposal = new Relation(new Relation(Surface, Head), new Relation(Drawing.State,
            new Relation(new Relation(signal, cause), new Relation(question, selected))));
        Pending = S(proposal); return proposal;
    }

    public Relation Commit(Relation proposal)
    {
        if (E(Pending) is not [var actual] || !ReferenceEquals(actual, proposal) ||
            proposal.Left is not Relation context || !ReferenceEquals(context.Left, Surface) || !ReferenceEquals(context.Right, Head) ||
            proposal.Right is not Relation { Left: var actuatorHead, Right: Relation { Left: Relation incoming, Right: Relation choice } } ||
            !ReferenceEquals(actuatorHead, Drawing.State) || choice.Left is not Relation question ||
            !QualitativeQuestion.TryReadAnswer(question, out var selected) || !ReferenceEquals(selected, choice.Right) || Program(selected) is not { } program)
            return Refuse(proposal);
        var fresh = FiniteLaw.Evaluate(Policy, S(Current.Program.Surface), S(incoming.Left));
        if (!FiniteLaw.TryReadResult(fresh, out var support, out var residue, out _) || E(residue).Length != 0 ||
            !UnitArrangementRead.ClosedEntries(support, out var answers) || answers is not [var answer] || !ReferenceEquals(answer, selected)) return Refuse(proposal);
        var before = Current;
        if (!Build(program, proposal, out var after)) return Refuse(proposal);
        var accepted = new Relation(proposal, new Relation(new Relation(before.Surface, after.Surface), after.Execution.Capture()));
        Current = after; Revisions = Append(Revisions, after.Surface); Timeline = Append(Timeline, accepted); Pending = S();
        return accepted;
    }

    private UnitArrangementProgram? Program(IElement surface) => Programs.SingleOrDefault(program => ReferenceEquals(program.Surface, surface));

    private bool Build(UnitArrangementProgram program, IElement cause, out UnitArrangementRevision revision)
    {
        // This closed synchronous operation has no external callbacks or
        // concurrent graph edits. Keep repeated prefix authentication exact,
        // but reuse the already checked immutable states/receipts locally.
        using var readScope = Drawing.BeginSynchronousReadScope();
        revision = null!;
        if (!program.Complete()) return false;
        UnitArrangementProgram executable;
        try { executable = Transform is null ? program : program.Transform(program.Name + " / placed basis", Transform); }
        catch (ArgumentException) { return false; }
        if (!executable.Complete()) return false;
        var paths = E(executable.Paths).Cast<Relation>().ToArray();
        var portEntries = E(executable.Ports); var closedEntries = E(executable.ClosedPaths);

        var actions = new List<IElement>(); var selections = new List<(Relation Path, Relation Word, Relation Action)>();
        foreach (var path in paths)
        {
            actions.Add(Drawing.Reset()); actions.Add(Drawing.Jump((Relation)path.Left));
            foreach (var word in E((Relation)path.Right).Cast<Relation>())
            { var action = Drawing.Draw(word); actions.Add(action); selections.Add((path, word, action)); }
        }
        var execution = new PrimitiveFlowLeaf(Name + " / active path", Drawing, S(actions.ToArray()));
        var before = Drawing.State;
        if (!Drive(execution, out var receiptRows)) { Unresolved = Append(Unresolved, execution.Capture()); return false; }
        var identity = new Placeholder(); var publications = new List<IElement>(); var strokes = new List<IElement>();
        var closures = new List<IElement>(); var rows = E(receiptRows).Cast<Relation>().ToArray();
        foreach (var row in rows)
            if (Drawing.TryReadAccepted((Relation)row.Right, out _, out var added)) strokes.AddRange(E(added));
        foreach (var port in portEntries.Cast<Relation>())
        {
            var pick = (Relation)port.Right; var path = (Relation)pick.Left;
            var terminal = ReferenceEquals(pick.Right, path);
            var selection = terminal ? selections.Last(item => ReferenceEquals(item.Path, path)) :
                selections.Single(item => ReferenceEquals(item.Path, path) && ReferenceEquals(item.Word, pick.Right));
            var receipt = (Relation)rows.Single(row => ReferenceEquals(row.Left, selection.Action)).Right;
            Drawing.TryReadStep(receipt, out var stateBefore, out _, out var stateAfter, out _, out _, out _);
            Drawing.TryReadState(terminal ? stateAfter : stateBefore, out _, out var cursor, out _, out _, out _, out _, out _);
            publications.Add(new Relation(port.Left, new Relation(identity, new Relation(cursor, new Relation(port, receipt)))));
        }
        foreach (var path in closedEntries.Cast<Relation>())
        {
            var actual = selections.Where(item => ReferenceEquals(item.Path, path)).ToArray();
            var first = (Relation)rows.Single(row => ReferenceEquals(row.Left, actual[0].Action)).Right;
            var last = (Relation)rows.Single(row => ReferenceEquals(row.Left, actual[^1].Action)).Right;
            Drawing.TryReadStep(first, out var startState, out _, out _, out _, out _, out _);
            Drawing.TryReadStep(last, out _, out _, out var endState, out _, out _, out _);
            Drawing.TryReadState(startState, out _, out var start, out _, out _, out _, out _, out _);
            Drawing.TryReadState(endState, out _, out var end, out _, out _, out _, out _, out _);
            closures.Add(new Relation(new Relation(end.Left, start.Left), new Relation(path, execution.CompletionEvents)));
        }
        var sites = strokes.Cast<Relation>().SelectMany(stroke =>
        { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); return new[] { a, b }; })
            .Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
        var bindings = S(E(Drawing.SiteBindings).Cast<Relation>().Where(row => sites.Any(site => ReferenceEquals(site, row.Left))).Cast<IElement>().ToArray());
        var graph = new QualitativeStrokeGraph(Drawing.Frame, S(sites), S(strokes.ToArray()));
        var join = new QualitativeGraphJoin(graph, bindings, S(closures.ToArray()));
        revision = new(identity, program, executable, before, Drawing.State, execution, receiptRows, S(publications.ToArray()), graph, bindings, join, cause);
        return join.TryReadPassed(join.Evaluate());
    }

    private bool Drive(PrimitiveFlowLeaf execution, out Relation receipts)
    {
        using var readScope = Drawing.BeginSynchronousReadScope();
        using var prefixScope = execution.Sequence.BeginSynchronousReadScope();
        var rows = new List<IElement>();
        var budget = E(execution.Sequence.Actions).Length;
        for (var index = 0; index < budget && E(execution.CompletionEvents).Length == 0; index++)
        {
            var request = execution.Propose(new Placeholder(), S(new Placeholder()));
            if (!execution.TryReadRequest(request, out var before, out var offer)) break;
            var receipt = Drawing.Step(before, offer);
            var ack = execution.Acknowledge(request, receipt);
            if (!execution.TryReadAccepted(ack, out _)) break;
            // The admitted offer itself retains the actual selected action.
            var actionSupport = (Relation)((Relation)((Relation)offer.Right).Right).Left;
            rows.Add(new Relation(E(actionSupport).Single(), receipt));
        }
        receipts = S(rows.ToArray()); return E(execution.CompletionEvents).Length == 1 && execution.IsAtBoundary;
    }

    private Relation Refuse(IElement cause) { var read = new Relation(Surface, StructuralConstraint.Open(cause)); Unresolved = Append(Unresolved, read); return read; }
    private static Relation Append(Relation scope, IElement item) => S([.. E(scope), item]);
    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

public sealed class UnitArrangementRevision
{
    public IElement Identity { get; }
    public UnitArrangementProgram Program { get; }
    public UnitArrangementProgram ExecutedProgram { get; }
    public Relation Before { get; }
    public Relation After { get; }
    public PrimitiveFlowLeaf Execution { get; }
    /// <summary>Actual action -> acknowledged drawing receipt rows.</summary>
    public Relation Receipts { get; }
    public Relation Ports { get; }
    public QualitativeStrokeGraph Graph { get; }
    public Relation Bindings { get; }
    public QualitativeGraphJoin Join { get; }
    public Relation Surface { get; }
    internal UnitArrangementRevision(IElement identity, UnitArrangementProgram program, UnitArrangementProgram executedProgram,
        Relation before, Relation after, PrimitiveFlowLeaf execution, Relation receipts, Relation ports,
        QualitativeStrokeGraph graph, Relation bindings, QualitativeGraphJoin join, IElement cause)
    {
        Identity = identity; Program = program; ExecutedProgram = executedProgram; Before = before; After = after;
        Execution = execution; Receipts = receipts; Ports = ports; Graph = graph; Bindings = bindings; Join = join;
        Surface = new(new Relation(identity, program.Surface), new Relation(executedProgram.Surface,
            new Relation(new Relation(before, after), new Relation(execution.Capture(),
                new Relation(receipts, new Relation(ports, new Relation(join.Surface, cause)))))));
    }
}
