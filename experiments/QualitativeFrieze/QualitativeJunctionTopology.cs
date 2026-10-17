using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A captured, port-justified neighbour topology for a separate live
/// system. It publishes anchor reads and seam passages, never raw drawing state
/// as an implicit flow source. Later changes to the producer leave this
/// historical contract intact.</summary>
public sealed class QualitativeJunctionTopology
{
    // anchor = actual declared cell -> actual captured anchor binding
    // edge = (from-cell -> to-cell) ->
    //        (actual accepted seam -> (actual from-publication -> actual to-publication))
    // The reverse edge retains that same seam and reverses only its exposed
    // publication order. Local closure seams do not become neighbour edges.
    public ConnectedWallpaperStudy.Field Field { get; }
    public Relation Cells { get; }
    public Relation Anchors { get; }
    public Relation Edges { get; }
    public Relation Surface { get; }
    public Relation Unresolved { get; }
    public bool IsValid { get; }

    private Relation acceptedSeams = S();
    private readonly Dictionary<IElement, Cycle> cycles = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Relation, Publication> publications = new(ReferenceEqualityComparer.Instance);
    private Relation selectedConnections = S();
    private QualitativeDrawingReceiver drawing = null!;

    public QualitativeJunctionTopology(ConnectedWallpaperStudy.Field field)
    {
        Field = field;
        var problems = new List<IElement>(); var proof = new List<IElement>();
        var cells = new List<IElement>(); var anchors = new List<IElement>(); var edges = new List<IElement>();
        if (Validate(problems, cells, proof))
        {
            foreach (var cell in cells) anchors.Add(new Relation(cell, cycles[cell].Anchor));
            foreach (var item in E(acceptedSeams).Cast<Relation>())
            {
                if (!ReadSeam(item, out var from, out var to, out var a, out var b))
                { problems.Add(item); continue; }
                if (ReferenceEquals(from, to)) continue;
                edges.Add(new Relation(new Relation(from, to), new Relation(item, new Relation(a, b))));
                edges.Add(new Relation(new Relation(to, from), new Relation(item, new Relation(b, a))));
            }
        }
        IsValid = problems.Count == 0;
        Cells = S(cells.ToArray());
        Anchors = IsValid ? S(anchors.ToArray()) : S();
        Edges = IsValid ? S(edges.ToArray()) : S();
        Unresolved = S(problems.ToArray());
        Surface = new Relation(field.Surface, new Relation(new Relation(Cells, Anchors),
            new Relation(Edges, new Relation(S(proof.ToArray()), Unresolved))));
    }

    /// <summary>Only an actual exposed edge can carry this contract. Copying a
    /// row or supplying an equal-position pair cannot manufacture a passage.</summary>
    public bool TryReadEdge(Relation edge, out IElement from, out IElement to, out Relation actualSeam)
    {
        from = to = null!; actualSeam = null!;
        if (!IsValid || !E(Edges).Any(actual => ReferenceEquals(actual, edge)) ||
            edge.Left is not Relation ends || edge.Right is not Relation { Left: Relation seam, Right: Relation ports } ||
            !ReadSeam(seam, out var forward, out var reverse, out var a, out var b)) return false;
        var direct = ReferenceEquals(ends.Left, forward) && ReferenceEquals(ends.Right, reverse) &&
            ReferenceEquals(ports.Left, a) && ReferenceEquals(ports.Right, b);
        var reversed = ReferenceEquals(ends.Left, reverse) && ReferenceEquals(ends.Right, forward) &&
            ReferenceEquals(ports.Left, b) && ReferenceEquals(ports.Right, a);
        if ((!direct && !reversed) || ReferenceEquals(ends.Left, ends.Right)) return false;
        from = ends.Left; to = ends.Right; actualSeam = seam; return true;
    }

    private bool Validate(List<IElement> problems, List<IElement> cells, List<IElement> proof)
    {
        var definition = Field.Definition; var machine = definition.Machine; drawing = machine.Drawing;
        if (Field.Surface.Left is not Relation source || !ReferenceEquals(source.Left, machine.Surface) ||
            source.Right is not Relation capturedRun || !ReferenceEquals(capturedRun.Left, machine.Surface) ||
            Field.Surface.Right is not Relation { Left: Relation raw, Right: Relation result } ||
            !ReferenceEquals(raw.Left, Field.SourceGraph.Surface) || !ReferenceEquals(raw.Right, Field.SourceBindings) ||
            !ReferenceEquals(result.Left, Field.SeamRead) || !ReferenceEquals(result.Right, Field.Join.Surface) ||
            capturedRun.Right is not Relation { Right: Relation history } || history.Left is not Relation timeline ||
            history.Right is not Relation { Left: Relation capturedDrawing } ||
            !drawing.TryReadState(capturedDrawing, out _, out _, out _, out _, out var historicalBindings, out var historicalStrokes, out _) ||
            !ReferenceEquals(historicalStrokes, Field.SourceGraph.Strokes) || !ReferenceEquals(Field.SourceGraph.Frame, drawing.Frame) ||
            !Closed(Field.SourceBindings) || !Closed(timeline) ||
            !ReferenceEquals(Field.Join.Source, Field.SourceGraph) || !ReferenceEquals(Field.Join.SourceBindings, Field.SourceBindings))
            return Fail(Field.Surface);

        if (Field.SeamRead.Left is not Relation { Left: Relation context, Right: Relation ports } ||
            !ReferenceEquals(context.Left, definition.Support) || context.Right is not Relation connections ||
            !ReferenceEquals(ports, Field.PortPublications) ||
            Field.SeamRead.Right is not Relation { Left: Relation seams, Right: Relation remainder } ||
            !ReferenceEquals(seams, Field.Join.DeclaredJoins) || !ReferenceEquals(remainder.Left, Field.Blocked) ||
            remainder.Right is not Relation { Left: Relation pending, Right: Relation frontier } ||
            !ReferenceEquals(pending, Field.Unresolved) || !ReferenceEquals(frontier, Field.Frontier) ||
            !ReferenceEquals(frontier, definition.Frontier) || !Closed(connections) || !Closed(ports) || !Closed(seams) ||
            !Closed(Field.Blocked) || E(Field.Blocked).Length != 0 || !Closed(pending) || E(pending).Length != 0 || !Closed(frontier) ||
            !Field.Join.TryReadPassed(Field.Join.Evaluate())) return Fail(Field.SeamRead);
        acceptedSeams = seams; selectedConnections = connections;

        if (!ReferenceEquals(definition.MotifActions, machine.Motif.Actions) || !ReferenceEquals(definition.Passages, machine.Passages) ||
            machine.Surface.Left is not Relation owner || !ReferenceEquals(owner.Right, definition.Support) ||
            definition.Support.Right is not Relation { Left: Relation plan, Right: Relation design } ||
            !ReferenceEquals(plan.Left, definition.Cells) || plan.Right is not Relation { Left: Relation pitches, Right: Relation passages } ||
            !ReferenceEquals(pitches.Left, definition.ColumnPitch) || !ReferenceEquals(pitches.Right, definition.RowPitch) || !ReferenceEquals(passages, definition.Passages) ||
            design.Left is not Relation motif || !ReferenceEquals(motif.Left, definition.MotifActions) || !ReferenceEquals(motif.Right, definition.PortDefinitions) ||
            design.Right is not Relation { Left: Relation boundaries } || !ReferenceEquals(boundaries.Left, definition.ConnectionDefinitions) ||
            !ReferenceEquals(boundaries.Right, definition.Frontier) || !Closed(definition.Cells) || !Closed(definition.PortDefinitions) ||
            !Closed(definition.ConnectionDefinitions) || !Closed(definition.Passages)) return Fail(definition.Support);
        foreach (var row in E(definition.Cells))
        {
            if (row is not Relation ordered || !Closed(ordered)) return Fail(definition.Cells);
            foreach (var cell in E(ordered))
            { if (cells.Any(actual => ReferenceEquals(actual, cell))) return Fail(definition.Cells); cells.Add(cell); }
        }
        if (cells.Count == 0 || machine.InitialState.Left is not Relation initial || !ReferenceEquals(initial.Left, cells[0])) return Fail(definition.Cells);
        var routeCells = new List<IElement> { cells[0] };
        foreach (var passage in E(definition.Passages))
        {
            if (passage is not Relation { Left: Relation ends } || !ReferenceEquals(ends.Left, routeCells[^1])) return Fail(definition.Passages);
            routeCells.Add(ends.Right);
        }
        if (!SameEntries(routeCells, cells)) return Fail(definition.Cells);
        if (E(connections).Any(connection => !E(definition.ConnectionDefinitions).Any(actual => ReferenceEquals(actual, connection)))) return Fail(connections);
        var portDefinitions = E(definition.PortDefinitions).OfType<Relation>().ToArray();
        if (portDefinitions.Length != E(definition.PortDefinitions).Length ||
            portDefinitions.Any(port => portDefinitions.Count(other => ReferenceEquals(port.Left, other.Left)) != 1)) return Fail(definition.PortDefinitions);

        var sourceSites = E(Field.SourceGraph.Sites); var inkSites = new List<IElement>();
        foreach (var item in E(Field.SourceGraph.Strokes))
        {
            if (item is not Relation stroke || !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _)) return Fail(Field.SourceGraph.Surface);
            Add(a); Add(b);
        }
        if (!Closed(Field.SourceGraph.Sites) || sourceSites.Length != inkSites.Count ||
            sourceSites.Any(site => !inkSites.Any(actual => ReferenceEquals(actual, site))) ||
            E(Field.SourceBindings).Length != sourceSites.Length ||
            E(Field.SourceBindings).Any(binding => !E(historicalBindings).Any(actual => ReferenceEquals(actual, binding)))) return Fail(Field.SourceBindings);

        foreach (var item in E(timeline))
        {
            if (item is not Relation { Left: Relation { Right: Relation operation } header } || !ReferenceEquals(header.Left, machine.Surface) ||
                operation.Left is not Relation beforeControl || beforeControl.Left is not Relation control ||
                !ReferenceEquals(control.Right, machine.Motif.InputPort) || operation.Right is not Relation acknowledgment ||
                acknowledgment.Right is not Relation { Right: Relation { Left: Relation completions } } || E(completions).Length == 0) continue;
            if (!cells.Any(cell => ReferenceEquals(cell, control.Left)) ||
                !machine.Motif.TryReadAccepted(acknowledgment, out _, out _, out _, out var actualCompletions) ||
                E(actualCompletions) is not [Relation completion] || completion.Right is not Relation { Left: Relation receipts } ||
                !ValidCycle(receipts, out var anchor) || !cycles.TryAdd(control.Left, new(completion, receipts, anchor))) return Fail(item);
            proof.Add(new Relation(control.Left, new Relation(acknowledgment, anchor)));
        }
        if (cycles.Count != cells.Count) return Fail(timeline);
        foreach (var item in E(ports))
        {
            if (item is not Relation publication || !ReadPublication(publication, out var read) || !publications.TryAdd(publication, read)) return Fail(item);
        }
        if (publications.Count != cells.Count * portDefinitions.Length) return Fail(ports);
        foreach (var cell in cells) foreach (var port in portDefinitions)
            if (publications.Values.Count(publication => ReferenceEquals(publication.Cell, cell) && ReferenceEquals(publication.Definition, port)) != 1) return Fail(ports);
        foreach (var seam in E(seams))
            if (seam is not Relation actual || !ReadSeam(actual, out _, out _, out _, out _)) return Fail(seam);
        foreach (var item in E(connections))
        {
            if (item is not Relation connection || connection.Right is not Relation pairs || !Closed(pairs)) return Fail(item);
            foreach (var pair in E(pairs))
                if (E(seams).OfType<Relation>().Count(seam => seam.Right is Relation { Left: Relation premise } &&
                    ReferenceEquals(premise.Left, connection) && ReferenceEquals(premise.Right, pair)) != 1) return Fail(connection);
        }
        return true;

        bool Fail(IElement item) { problems.Add(item); return false; }
        void Add(IElement site) { if (!inkSites.Any(actual => ReferenceEquals(actual, site))) inkSites.Add(site); }
    }

    private sealed record Cycle(Relation Completion, Relation Receipts, Relation Anchor);
    private sealed record Publication(IElement Cell, Relation Definition, Relation Binding);

    private bool ValidCycle(Relation receipts, out Relation anchor)
    {
        anchor = null!; var actions = E(Field.Definition.MotifActions); var effects = E(receipts);
        if (!Closed(receipts) || effects.Length != actions.Length || effects.Length == 0) return false;
        Relation? priorAfter = null;
        for (var index = 0; index < effects.Length; index++)
        {
            if (effects[index] is not Relation effect || !drawing.TryReadAccepted(effect, out _, out _) ||
                !drawing.TryReadStep(effect, out var before, out var offer, out var after, out _, out _, out _) ||
                priorAfter is not null && !ReferenceEquals(priorAfter, before) ||
                offer.Right is not Relation { Right: Relation { Left: Relation support } } || E(support) is not [var action] || !ReferenceEquals(action, actions[index])) return false;
            if (index == 0 && !drawing.TryReadState(before, out _, out _, out anchor, out _, out _, out _, out _)) return false;
            priorAfter = after;
        }
        return true;
    }

    private bool ReadPublication(Relation publication, out Publication read)
    {
        read = null!;
        if (publication.Left is not Relation owner || owner.Right is not Relation definition ||
            !cycles.TryGetValue(owner.Left, out var cycle) || !E(Field.Definition.PortDefinitions).Any(port => ReferenceEquals(port, definition)) ||
            definition.Right is not Relation selector || publication.Right is not Relation proof || !ReferenceEquals(proof.Left, cycle.Completion) ||
            proof.Right is not Relation { Left: Relation receipt, Right: Relation binding }) return false;
        Relation action; bool before;
        if (ReferenceEquals(selector.Left, drawing.CursorPort) && selector.Right is Relation next) { action = next; before = true; }
        else if (ReferenceEquals(selector.Right, drawing.CursorPort) && selector.Left is Relation prior) { action = prior; before = false; }
        else return false;
        var actions = E(Field.Definition.MotifActions); var indices = Enumerable.Range(0, actions.Length).Where(index => ReferenceEquals(actions[index], action)).ToArray();
        if (indices.Length != 1 || !ReferenceEquals(E(cycle.Receipts)[indices[0]], receipt) ||
            !drawing.TryReadStep(receipt, out var start, out _, out var end, out _, out _, out _) ||
            !drawing.TryReadState(before ? start : end, out _, out var actualBinding, out _, out _, out _, out _, out _) ||
            !ReferenceEquals(binding, actualBinding) || !E(Field.SourceBindings).Any(actual => ReferenceEquals(actual, binding))) return false;
        read = new(owner.Left, definition, binding); return true;
    }

    private bool ReadSeam(Relation seam, out IElement from, out IElement to, out Relation fromPublication, out Relation toPublication)
    {
        from = to = null!; fromPublication = toPublication = null!;
        if (!E(acceptedSeams).Any(actual => ReferenceEquals(actual, seam)) || !E(Field.Join.Accepted).Any(actual => ReferenceEquals(actual, seam)) ||
            seam.Left is not Relation sites || seam.Right is not Relation { Left: Relation premise, Right: Relation evidence } ||
            premise.Left is not Relation connection || premise.Right is not Relation pair ||
            !E(selectedConnections).Any(actual => ReferenceEquals(actual, connection)) ||
            connection.Left is not Relation { Left: Relation cells, Right: Relation period } || connection.Right is not Relation portPairs ||
            !Closed(period) || !Closed(portPairs) ||
            !E(portPairs).Any(actual => ReferenceEquals(actual, pair)) ||
            evidence.Left is not Relation { Left: Relation a, Right: Relation b } ||
            !publications.TryGetValue(a, out var ap) || !publications.TryGetValue(b, out var bp) ||
            !ReferenceEquals(ap.Cell, cells.Left) || !ReferenceEquals(bp.Cell, cells.Right) ||
            !ReferenceEquals(ap.Definition.Left, pair.Left) || !ReferenceEquals(bp.Definition.Left, pair.Right) ||
            !ReferenceEquals(sites.Left, ap.Binding.Left) || !ReferenceEquals(sites.Right, bp.Binding.Left) ||
            evidence.Right is not Relation { Left: Relation placement, Right: Relation equality } ||
            !cycles.TryGetValue(ap.Cell, out var ac) || !cycles.TryGetValue(bp.Cell, out var bc) ||
            !ReadEquality(placement, E((Relation)ac.Anchor.Right).Concat(E(period)).ToArray(), (Relation)bc.Anchor.Right) ||
            !ReadEquality(equality, E((Relation)ap.Binding.Right), (Relation)bp.Binding.Right)) return false;
        from = cells.Left; to = cells.Right; fromPublication = a; toPublication = b; return true;
    }

    private bool ReadEquality(Relation equality, IElement[] expectedFirst, Relation second)
    {
        if (equality.Left is not Relation { Left: Relation frame, Right: Relation operands } || !ReferenceEquals(frame, drawing.Frame.Surface) ||
            operands.Left is not Relation first || operands.Right is not Relation opposite || !Closed(first) || !Closed(opposite) || !Closed(second) ||
            !SameEntries(E(first), expectedFirst) || E(opposite).Length != E(second).Length ||
            !drawing.Frame.TryReadResult(equality, out var remaining, out _, out var pending) || !Closed(remaining) || E(remaining).Length != 0 || E(pending).Length != 0) return false;
        for (var index = 0; index < E(second).Length; index++)
        {
            if (E(second)[E(second).Length - 1 - index] is not Relation original || E(opposite)[index] is not Relation inverse || !ReferenceEquals(original.Right, inverse.Right) ||
                !E(drawing.Frame.Opposites).OfType<Relation>().Any(pair => ReferenceEquals(pair.Left, original.Left) && ReferenceEquals(pair.Right, inverse.Left))) return false;
        }
        return true;
    }

    private static bool SameEntries(IEnumerable<IElement> first, IEnumerable<IElement> second)
    {
        var a = first.ToArray(); var b = second.ToArray();
        return a.Length == b.Length && a.Select((item, index) => ReferenceEquals(item, b[index])).All(equal => equal);
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
