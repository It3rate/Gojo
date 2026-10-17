using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Live motif programs with declared boundary ports and explicit
/// neighbouring seam correspondences. A visually coincident endpoint is never
/// joined merely because a position reader finds equal displacement.</summary>
public sealed class ConnectedWallpaperStudy
{
    // port definition = slot -> (cursor-port -> actual action) [before]
    //                 | slot -> (actual action -> cursor-port) [after]
    // connection = ((from cell -> to cell) -> period word) ->
    //              Closed(actual from-port slot -> actual to-port slot)
    // publication = (actual cell -> actual port definition) ->
    //               (actual completed motif -> (actual accepted effect -> actual cursor binding))
    // seam read = ((support -> selected connection definitions) -> publications) ->
    //             (accepted joins -> (blocked -> (unresolved -> finite frontier)))
    public WallpaperFlowStudy Basis { get; } = new();
    public Case[] Cases { get; }
    public WallpaperPatternMachine[] Machines { get; }
    public Relation Surface { get; }

    public sealed record Case(string Name, string Description, WallpaperPatternMachine Machine,
        Relation MotifActions, Relation Cells, Relation ColumnPitch, Relation RowPitch,
        Relation PortDefinitions, Relation ConnectionDefinitions, Relation Frontier,
        Relation Passages, Relation Support);

    public sealed record Field(Case Definition, QualitativeStrokeGraph SourceGraph,
        Relation SourceBindings, Relation PortPublications, Relation SeamRead,
        QualitativeGraphJoin Join, Relation Blocked, Relation Unresolved,
        Relation Frontier, Relation Surface)
    {
        public QualitativeStrokeGraph Graph => Join.Graph;
        public Relation Bindings => Join.Bindings;
        public Relation AcceptedSeams => (Relation)((Relation)SeamRead.Right).Left;
        public Relation BlockedSeams => Blocked;
    }

    public ConnectedWallpaperStudy(int columns = 5, int rows = 4)
    {
        var frame = Basis.Basis.Frame;
        var key = Drawing();
        var keyActions = new[] { key.Draw(W(frame.Right)), key.Draw(W(frame.Up, 3)),
            key.Draw(W(frame.Right, 3)), key.Draw(W(frame.Down)), key.Draw(W(frame.Left, 2)),
            key.Draw(W(frame.Down)), key.Draw(W(frame.Right, 3)), key.Draw(W(frame.Down)), key.Draw(W(frame.Right)) };
        var kw = new Placeholder(); var ke = new Placeholder(); var ks = new Placeholder(); var kn = new Placeholder();
        var keyPorts = S(Before(key, kw, keyActions[0]), After(key, ke, keyActions[^1]),
            After(key, ks, keyActions[0]), After(key, kn, keyActions[1]));
        var keys = Build("Interlocking key mesh",
            "Open Greek-key paths continue across columns; actual top and bottom corner ports connect the rows.",
            key, S(keyActions), W(frame.Right, 6), W(frame.Up, 3), keyPorts,
            S(new Relation(W(frame.Right, 6), S(new Relation(ke, kw))),
              new Relation(W(frame.Up, 3), S(new Relation(kn, ks)))), columns: columns, rows: rows);

        var diamond = Drawing();
        var diamondActions = new[] { diamond.Draw(C(frame.Right, frame.Up)),
            diamond.Draw(C(frame.Right, frame.Down)), diamond.Draw(C(frame.Left, frame.Down)),
            diamond.Draw(C(frame.Left, frame.Up)) };
        var dw = new Placeholder(); var dn = new Placeholder(); var de = new Placeholder();
        var ds = new Placeholder(); var returned = new Placeholder();
        var diamondPorts = S(Before(diamond, dw, diamondActions[0]), After(diamond, dn, diamondActions[0]),
            After(diamond, de, diamondActions[1]), After(diamond, ds, diamondActions[2]), After(diamond, returned, diamondActions[3]));
        var diamonds = Build("Touching diamond network",
            "Four simultaneous two-axis chords close a diamond. Explicit corner and closure correspondences create the network.",
            diamond, S(diamondActions), W(frame.Right, 2), W(frame.Up, 2), diamondPorts,
            S(new Relation(W(frame.Right, 2), S(new Relation(de, dw))),
              new Relation(W(frame.Up, 2), S(new Relation(dn, ds)))),
            S(new Relation(returned, dw)), columns, rows);

        var weave = Crossing(false); var junction = Crossing(true);
        Cases = [keys, diamonds, weave, junction];
        Machines = Cases.Select(example => example.Machine).ToArray();
        Surface = new Relation(Basis.Surface, S(Cases.Select(example => (IElement)example.Machine.Surface).ToArray()));

        QualitativeDrawingReceiver Drawing() => new(frame, initialSite: frame.Origin);
        Relation W(IElement direction, int count = 1) => Word(direction, count);
        Relation C(IElement first, IElement second, int count = 1) => JoinWords(W(first, count), W(second, count));

        Case Crossing(bool split)
        {
            var drawing = Drawing(); var steps = new List<Relation>();
            var sw = new Placeholder(); var ne = new Placeholder(); var nw = new Placeholder(); var se = new Placeholder();
            var ca = new Placeholder(); var cb = new Placeholder();
            var first = drawing.Draw(C(frame.Right, frame.Up, split ? 2 : 4)); steps.Add(first);
            Relation northEnd = first;
            if (split) { northEnd = drawing.Draw(C(frame.Right, frame.Up, 2)); steps.Add(northEnd); }
            steps.Add(drawing.Reset()); steps.Add(drawing.Jump(W(frame.Up, 4)));
            var second = drawing.Draw(C(frame.Right, frame.Down, split ? 2 : 4)); steps.Add(second);
            Relation southEnd = second;
            if (split) { southEnd = drawing.Draw(C(frame.Right, frame.Down, 2)); steps.Add(southEnd); }
            var ports = new List<IElement> { Before(drawing, sw, first), After(drawing, ne, northEnd),
                Before(drawing, nw, second), After(drawing, se, southEnd) };
            if (split) { ports.Add(After(drawing, ca, first)); ports.Add(After(drawing, cb, second)); }
            var x = W(frame.Right, 4); var y = W(frame.Up, 4);
            return Build(split ? "Joined diagonal junctions" : "Unjoined diagonal weave",
                split ? "The same diagonal coverage is split at the centre; an explicit local seam identifies the two actual midpoint sites."
                      : "The diagonal strokes cross in the image. Neither uninterrupted stroke supplies a midpoint site or a junction.",
                drawing, S(steps.ToArray()), x, y, S(ports.ToArray()),
                S(new Relation(x, S(new Relation(ne, nw), new Relation(se, sw))),
                  new Relation(y, S(new Relation(nw, sw), new Relation(ne, se)))),
                split ? S(new Relation(ca, cb)) : S(), columns, rows);
        }
    }

    public static Relation Before(QualitativeDrawingReceiver drawing, IElement slot, Relation action) =>
        new(slot, new Relation(drawing.CursorPort, action));
    public static Relation After(QualitativeDrawingReceiver drawing, IElement slot, Relation action) =>
        new(slot, new Relation(action, drawing.CursorPort));

    /// <summary>Authors a finite native phase route and neighbour declarations.
    /// Supported neighbour words are X, Y and -X+Y. The last selects a diagonal
    /// adjacency of the same native grid, useful for an oblique lattice.</summary>
    public Case Build(string name, string description, QualitativeDrawingReceiver drawing,
        Relation motifActions, Relation columnPitch, Relation rowPitch,
        Relation portDefinitions, Relation pairings, Relation? localPairings = null,
        int columns = 5, int rows = 4, Relation? construction = null)
    {
        if (columns is < 1 or > 8 || rows is < 1 or > 8 || !ReferenceEquals(drawing.Frame, Basis.Basis.Frame) ||
            !Closed(motifActions) || !Closed(portDefinitions) || !Closed(pairings) || localPairings is not null && !Closed(localPairings))
            throw new ArgumentException("A finite authored patch needs closed definitions in the exposed basis.");
        var cells = Enumerable.Range(0, columns * rows).Select(_ => (IElement)new Placeholder()).ToArray();
        var cellRows = S(Enumerable.Range(0, rows).Select(row => (IElement)S(cells.Skip(row * columns).Take(columns).ToArray())).ToArray());
        var directionActions = E(motifActions).OfType<Relation>().Select(action =>
            drawing.TryReadDirectionAction(action, out var map) || drawing.TryReadMirror(action, out map) ? map : null)
            .Where(map => map is not null).Cast<IElement>().ToArray();
        var directionRead = Basis.Basis.DirectionActions.Evaluate(S(directionActions));
        if (!Basis.Basis.DirectionActions.TryReadResult(directionRead, out var finalOrientation, out var blocked, out var unknown, out _) ||
            E(blocked).Length != 0 || E(unknown).Length != 0) throw new ArgumentException("An authored motif needs a complete direction action word.");
        var inverseRead = Basis.Basis.DirectionActions.Inverse(finalOrientation);
        if (!Basis.Basis.DirectionActions.TryReadResult(inverseRead, out var inverse, out _, out _, out _))
            throw new ArgumentException("The motif's output basis needs an explicit inverse.");
        var routes = new List<IElement>();
        for (var index = 0; index < cells.Length - 1; index++)
        {
            var period = index % columns < columns - 1 ? columnPitch :
                JoinWords(Repeat(Basis.Basis.Inverse(columnPitch), columns - 1), rowPitch);
            routes.Add(new Relation(new Relation(cells[index], cells[index + 1]),
                S(drawing.Reset(), drawing.DirectionAction(inverse), drawing.Jump(period), drawing.Anchor())));
        }
        var connections = new List<IElement>(); var frontier = new List<IElement>();
        var diagonal = JoinWords(Basis.Basis.Inverse(columnPitch), rowPitch);
        foreach (var item in E(pairings))
        {
            if (item is not Relation { Left: Relation period, Right: Relation ports } || !Closed(ports))
                throw new ArgumentException("A pairing needs its native period word and closed port pairs.");
            var step = EqualWords(period, columnPitch) ? (X: 1, Y: 0) : EqualWords(period, rowPitch) ? (X: 0, Y: 1) :
                EqualWords(period, diagonal) ? (X: -1, Y: 1) : throw new ArgumentException("A pairing must use an exposed neighbour period.");
            for (var row = 0; row < rows; row++) for (var column = 0; column < columns; column++)
            {
                var cell = cells[row * columns + column]; var nx = column + step.X; var ny = row + step.Y;
                if (nx >= 0 && nx < columns && ny >= 0 && ny < rows)
                    connections.Add(new Relation(new Relation(new Relation(cell, cells[ny * columns + nx]), period), ports));
                else frontier.Add(new Relation(new Relation(cell, period), S(E(ports).OfType<Relation>().Select(pair => pair.Left).ToArray())));
                var px = column - step.X; var py = row - step.Y;
                if (px < 0 || px >= columns || py < 0 || py >= rows)
                    frontier.Add(new Relation(new Relation(cell, Basis.Basis.Inverse(period)),
                        S(E(ports).OfType<Relation>().Select(pair => pair.Right).ToArray())));
            }
        }
        if (localPairings is not null && E(localPairings).Length != 0)
            foreach (var cell in cells) connections.Add(new Relation(new Relation(new Relation(cell, cell), S()), localPairings));
        var passageDefinitions = S(routes.ToArray()); var connectionDefinitions = S(connections.ToArray()); var finiteFrontier = S(frontier.ToArray());
        var support = new Relation(Basis.Surface, new Relation(
            new Relation(cellRows, new Relation(new Relation(columnPitch, rowPitch), passageDefinitions)),
            new Relation(new Relation(motifActions, portDefinitions),
                new Relation(new Relation(connectionDefinitions, finiteFrontier),
                    new Relation(new Relation(directionRead, inverseRead), construction ?? S())))));
        var machine = new WallpaperPatternMachine(name, drawing, motifActions, passageDefinitions, cells[0], support);
        return new(name, description, machine, motifActions, cellRows, columnPitch, rowPitch,
            portDefinitions, connectionDefinitions, finiteFrontier, passageDefinitions, support);
    }

    /// <summary>Reads actual completed cycles and selected seam promises. An
    /// uncompleted promised interior port remains unresolved; a declared finite
    /// frontier is carried separately and is not an execution error.</summary>
    public Field Capture(Case example, Relation? connectionDefinitions = null)
    {
        var machine = example.Machine; var drawing = machine.Drawing;
        var problems = new List<IElement>(); var refused = new List<IElement>();
        var publications = new List<IElement>(); var admitted = new List<IElement>();
        var selected = connectionDefinitions ?? example.ConnectionDefinitions;
        var inkSites = new List<IElement>();
        foreach (var item in E(drawing.Strokes))
            if (item is Relation stroke && QualitativeDrawingReceiver.TryReadStroke(stroke, out _, out var from, out var to, out _))
            { AddSite(from); AddSite(to); }
            else problems.Add(item);
        var bindings = S(E(drawing.SiteBindings).OfType<Relation>().Where(binding => inkSites.Any(site => ReferenceEquals(site, binding.Left))).ToArray());
        var sourceGraph = new QualitativeStrokeGraph(drawing.Frame, S(inkSites.ToArray()), drawing.Strokes);
        var cells = E(example.Cells).OfType<Relation>().SelectMany(E).ToArray();
        var cycles = new Dictionary<IElement, Cycle>(ReferenceEqualityComparer.Instance);
        if (!ReferenceEquals(drawing.Frame, Basis.Basis.Frame) || !ReferenceEquals(example.MotifActions, machine.Motif.Actions))
            problems.Add(new Relation(example.MotifActions, machine.Motif.Surface));
        else foreach (var item in E(machine.Timeline))
        {
            if (item is not Relation { Left: Relation { Right: Relation operation } context } ||
                !ReferenceEquals(context.Left, machine.Surface) || operation.Left is not Relation beforeControl ||
                beforeControl.Left is not Relation control || !ReferenceEquals(control.Right, machine.Motif.InputPort) ||
                operation.Right is not Relation acknowledgment ||
                acknowledgment.Right is not Relation { Right: Relation { Left: Relation completionScope } } || E(completionScope).Length == 0) continue;
            if (!cells.Any(cell => ReferenceEquals(cell, control.Left)) ||
                !machine.Motif.TryReadAccepted(acknowledgment, out _, out _, out _, out var completions) ||
                E(completions) is not [Relation completion] ||
                !E(machine.CompletedMotifs).Any(actual => ReferenceEquals(actual, completion)) ||
                completion.Right is not Relation { Left: Relation receipts } ||
                !ValidateCycle(receipts, example.MotifActions, drawing, out var anchor))
            { problems.Add(item); continue; }
            if (!cycles.TryAdd(control.Left, new(completion, receipts, anchor))) problems.Add(item);
        }
        foreach (var pair in cycles)
            foreach (var definition in E(example.PortDefinitions))
            {
                if (definition is not Relation port || port.Right is not Relation selection ||
                    E(example.PortDefinitions).OfType<Relation>().Count(actual => ReferenceEquals(actual.Left, port.Left)) != 1 ||
                    !ReadPort(selection, drawing, out var action, out var before) ||
                    E(example.MotifActions).Count(item => ReferenceEquals(item, action)) != 1)
                { refused.Add(new Relation(pair.Key, definition)); continue; }
                var index = Array.FindIndex(E(example.MotifActions), item => ReferenceEquals(item, action));
                var receipt = (Relation)E(pair.Value.Receipts)[index];
                drawing.TryReadStep(receipt, out var start, out _, out var end, out _, out _, out _);
                if (!drawing.TryReadState(before ? start : end, out _, out var binding, out _, out _, out _, out _, out _) ||
                    !inkSites.Any(site => ReferenceEquals(site, binding.Left)))
                { problems.Add(new Relation(pair.Key, port)); continue; }
                publications.Add(new Relation(new Relation(pair.Key, port),
                    new Relation(pair.Value.Completion, new Relation(receipt, binding))));
            }
        if (!Closed(selected)) problems.Add(selected);
        foreach (var item in E(selected))
        {
            if (item is not Relation { Left: Relation { Left: Relation endpoints, Right: Relation period }, Right: Relation pairs } connection ||
                !Closed(pairs) || !cells.Any(cell => ReferenceEquals(cell, endpoints.Left)) || !cells.Any(cell => ReferenceEquals(cell, endpoints.Right)))
            { refused.Add(item); continue; }
            if (!cycles.TryGetValue(endpoints.Left, out var fromCycle) || !cycles.TryGetValue(endpoints.Right, out var toCycle))
            { problems.Add(connection); continue; }
            var placement = Compare(JoinWords((Relation)fromCycle.Anchor.Right, period), (Relation)toCycle.Anchor.Right);
            if (!Zero(placement)) { refused.Add(new Relation(connection, placement)); continue; }
            foreach (var itemPair in E(pairs))
            {
                if (itemPair is not Relation ports || !Declared(ports.Left) || !Declared(ports.Right))
                { refused.Add(new Relation(connection, itemPair)); continue; }
                var a = Publication(endpoints.Left, ports.Left); var b = Publication(endpoints.Right, ports.Right);
                if (a is null || b is null) { problems.Add(new Relation(connection, itemPair)); continue; }
                var aBinding = PublicationBinding(a); var bBinding = PublicationBinding(b);
                var equality = Compare((Relation)aBinding.Right, (Relation)bBinding.Right);
                var witness = new Relation(new Relation(connection, ports),
                    new Relation(new Relation(a, b), new Relation(placement, equality)));
                if (Zero(equality)) admitted.Add(new Relation(new Relation(aBinding.Left, bBinding.Left), witness));
                else refused.Add(witness);
            }
        }
        var pubs = S(publications.ToArray()); var joins = S(admitted.ToArray()); var blocked = S(refused.ToArray()); var pending = S(problems.ToArray());
        var seamRead = new Relation(new Relation(new Relation(example.Support, selected), pubs),
            new Relation(joins, new Relation(blocked, new Relation(pending, example.Frontier))));
        var join = new QualitativeGraphJoin(sourceGraph, bindings, joins);
        var surface = new Relation(new Relation(example.Machine.Surface, machine.Run(0)),
            new Relation(new Relation(sourceGraph.Surface, bindings), new Relation(seamRead, join.Surface)));
        return new(example, sourceGraph, bindings, pubs, seamRead, join, blocked, pending, example.Frontier, surface);

        void AddSite(IElement site) { if (!inkSites.Any(prior => ReferenceEquals(prior, site))) inkSites.Add(site); }
        bool Declared(IElement slot) => E(example.PortDefinitions).OfType<Relation>().Count(port => ReferenceEquals(port.Left, slot)) == 1;
        Relation? Publication(IElement cell, IElement slot) => publications.OfType<Relation>().SingleOrDefault(publication =>
            publication.Left is Relation owner && ReferenceEquals(owner.Left, cell) && owner.Right is Relation definition && ReferenceEquals(definition.Left, slot));
    }

    private sealed record Cycle(Relation Completion, Relation Receipts, Relation Anchor);

    private static bool ValidateCycle(Relation receipts, Relation actions, QualitativeDrawingReceiver drawing, out Relation anchor)
    {
        anchor = null!;
        if (!Closed(receipts) || E(receipts).Length != E(actions).Length || E(receipts).Length == 0) return false;
        Relation? previousAfter = null;
        for (var index = 0; index < E(receipts).Length; index++)
        {
            if (E(receipts)[index] is not Relation receipt || !drawing.TryReadAccepted(receipt, out _, out _) ||
                !drawing.TryReadStep(receipt, out var before, out var offer, out var after, out _, out _, out _) ||
                previousAfter is not null && !ReferenceEquals(previousAfter, before) ||
                offer.Right is not Relation { Right: Relation { Left: Relation support } } || E(support) is not [var actual] ||
                !ReferenceEquals(actual, E(actions)[index])) return false;
            if (index == 0 && !drawing.TryReadState(before, out _, out _, out anchor, out _, out _, out _, out _)) return false;
            previousAfter = after;
        }
        return true;
    }

    private static bool ReadPort(Relation selection, QualitativeDrawingReceiver drawing, out Relation action, out bool before)
    {
        action = null!; before = false;
        if (ReferenceEquals(selection.Left, drawing.CursorPort) && selection.Right is Relation next) { action = next; before = true; return true; }
        if (ReferenceEquals(selection.Right, drawing.CursorPort) && selection.Left is Relation prior) { action = prior; return true; }
        return false;
    }

    private static Relation PublicationBinding(Relation publication) => (Relation)((Relation)((Relation)publication.Right).Right).Right;
    public Relation Word(IElement direction, int count = 1) => S(Enumerable.Range(0, count)
        .Select(_ => (IElement)QualitativeMotionFrame.Move(direction, Basis.Basis.Reach)).ToArray());
    private Relation Compare(Relation first, Relation second) => Basis.Basis.Frame.Reduce(first, Basis.Basis.Inverse(second));
    private bool EqualWords(Relation first, Relation second) => Zero(Compare(first, second));
    private bool Zero(Relation read) => Basis.Basis.Frame.TryReadResult(read, out var result, out _, out var pending) &&
        E(result).Length == 0 && E(pending).Length == 0;
    private static Relation Repeat(Relation word, int count) => S(Enumerable.Range(0, count).SelectMany(_ => E(word)).ToArray());
    private static Relation JoinWords(params Relation[] words) => S(words.SelectMany(E).ToArray());
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
