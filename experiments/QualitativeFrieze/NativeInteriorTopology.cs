using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finds bounded observational walks in an explicitly selected planar
/// cyclic-order interpretation. Positions are additive native motion words.
/// Cancellation, axis order and actual endpoint incidence do all geometric work;
/// no scalar coordinates, lengths, angles or render geometry enter this reader.
/// An observational encounter never joins the original physical strokes.</summary>
public sealed class NativeInteriorTopology
{
    // Surface = (public source port -> cyclic order / geometry model) ->
    //   ((observational graph -> positions) ->
    //    (node correspondences -> (edge correspondences ->
    //     (bounded ordered dart walks -> (unresolved -> unsearched)))))
    // node = (fresh observational site -> optional native position word) ->
    //        actual source sites and encounter witnesses
    // edge = actual observational stroke -> Closed(actual source stroke uses)
    // dart = actual observational edge -> (from observational site -> to site)
    // boundary = Closed(ordered actual darts) -> Closed(native cyclic turns)
    // The public graph and all source correspondences remain connected even if
    // the helper objects below disappear. Sector indexes are a compiled read of
    // the published cyclic order, never a source of metric angle information.
    public NativeLinePublication Publication { get; }
    public NativeLatticeSources.SourceCase OriginalSource => Publication.Original;
    public NativeLatticeSources.SourceCase Source { get; }
    public QualitativeStrokeGraph Graph { get; }
    public Relation Bindings { get; }
    public Relation Basis { get; }
    public NativeLineBasis GeometryBasis { get; }
    public Relation Port { get; }
    public IReadOnlyList<Node> Nodes { get; }
    public IReadOnlyList<Edge> Edges { get; }
    public IReadOnlyList<Boundary> Boundaries { get; }
    public Relation Unresolved { get; }
    public Relation EmbeddingUnknown { get; }
    public Relation OpenWalks { get; }
    public Relation Unsearched { get; }
    public Relation Exterior { get; }
    public Relation Surface { get; }

    public NativeInteriorTopology(NativeLatticeSources.SourceCase source, int faceCapacity = 256)
        : this(source, new NativeLineBasis(source.Graph.Frame, source.TurnModel), faceCapacity) { }

    public NativeInteriorTopology(NativeLatticeSources.SourceCase source, NativeLineBasis geometryBasis, int faceCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(faceCapacity);
        Publication = new(source); Source = source = Publication.Published;
        var frame = source.Graph.Frame;
        if (!ReferenceEquals(geometryBasis.Frame, frame)) throw new ArgumentException("The geometry model must use the actual source frame.", nameof(geometryBasis));
        GeometryBasis = geometryBasis;
        var sectors = new[] { S(frame.Right), S(frame.Right, frame.Up), S(frame.Up),
            S(frame.Left, frame.Up), S(frame.Left), S(frame.Left, frame.Down),
            S(frame.Down), S(frame.Right, frame.Down) };
        Basis = new(frame.Surface, S(sectors.Select((sector, index) =>
            (IElement)new Relation(sector, sectors[(index + 1) % sectors.Length])).ToArray()));
        Port = new(new Relation(source.Graph.Surface, source.Bindings), new Relation(source.StraightPassages, GeometryBasis.Surface));
        var pending = new List<IElement>(); var openWalks = new List<IElement>(); var cuts = new List<IElement>();
        if (!Closed(source.Graph.Sites)) pending.Add(new Relation(source.Graph.Surface, source.Graph.Sites));
        if (!Closed(source.Graph.Strokes)) pending.Add(new Relation(source.Graph.Surface, source.Graph.Strokes));
        var nodes = new List<Node>(); var lines = new List<Line>(); var edges = new List<Edge>();
        var sourceNode = new Dictionary<IElement, Node>(N.Participants.Instance);
        foreach (var site in Entries(source.Graph.Sites))
        {
            var position = ReadPosition(site);
            var node = position is null ? null : nodes.FirstOrDefault(other => other.Word is not null && Equal(other.Word, position));
            if (node is null) { node = new(new Placeholder(), position); nodes.Add(node); }
            node.Add(site, new Relation(site, position ?? StructuralConstraint.Open())); sourceNode[site] = node;
        }
        foreach (var item in Entries(source.Graph.Strokes))
        {
            if (item is not Relation stroke || !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word) ||
                !sourceNode.TryGetValue(from, out var a) || !sourceNode.TryGetValue(to, out var b))
            { pending.Add(new Relation(item, source.Graph.Surface)); continue; }
            if (!Entries(source.StraightPassages).Any(offered => N.Same(offered, stroke)))
            { pending.Add(new Relation(stroke, source.StraightPassages)); continue; }
            var reduced = Reduce(word);
            if (reduced is null || Sector(reduced) is not int sector || ReferenceEquals(a, b))
            { pending.Add(new Relation(stroke, frame.Reduce(S(), word))); continue; }
            if (a.Word is not null && b.Word is not null && !Equal(Difference(b.Word, a.Word), reduced))
            { pending.Add(new Relation(stroke, new Relation(a.Word, b.Word))); continue; }
            lines.Add(new(stroke, a, b, reduced, sector));
        }
        // First establish axis encounters as native words. The projection says
        // where two observational strokes meet, not that material can transfer.
        for (var i = 0; i < lines.Count; i++) for (var j = i + 1; j < lines.Count; j++)
        {
            var first = lines[i]; var second = lines[j];
            if (first.From.Word is null || first.To.Word is null || second.From.Word is null || second.To.Word is null)
            { pending.Add(new Relation(first.Stroke, second.Stroke)); continue; }
            var firstAxis = Axis(first); var secondAxis = Axis(second);
            if (firstAxis is not null && secondAxis is not null)
            {
                if (ReferenceEquals(firstAxis, secondAxis)) continue;
                var h = ReferenceEquals(firstAxis, frame.Horizontal) ? first : second;
                var v = ReferenceEquals(firstAxis, frame.Vertical) ? first : second;
                var point = S(Entries(Project(v.From.Word!, frame.Horizontal))
                    .Concat(Entries(Project(h.From.Word!, frame.Vertical))).ToArray());
                var onH = Contains(h, point); var onV = Contains(v, point);
                if (onH == true && onV == true)
                {
                    var node = nodes.FirstOrDefault(other => other.Word is not null && Equal(other.Word, point));
                    if (node is null) { node = new(new Placeholder(), point); nodes.Add(node); }
                    node.AddEncounter(new Relation(new Relation(h.Stroke, v.Stroke), point));
                }
                else if (onH is null || onV is null) pending.Add(new Relation(new Relation(h.Stroke, v.Stroke), point));
                continue;
            }
            if (SharesEndpoint(first, second) || DisjointBoxes(first, second) || ParallelAndSeparated(first, second)) continue;
            // Coarse sectors cannot locate an arbitrary diagonal crossing. The
            // possible missing split qualifies every downstream clear-face read.
            pending.Add(new Relation(first.Stroke, second.Stroke));
        }
        foreach (var line in lines)
        {
            var split = new List<Node> { line.From, line.To };
            if (Axis(line) is not null)
            {
                foreach (var node in nodes)
                {
                    if (node.Word is null || split.Contains(node)) continue;
                    var inside = Contains(line, node.Word);
                    if (inside == true) split.Add(node);
                    else if (inside is null) pending.Add(new Relation(line.Stroke, node.Surface));
                }
                var ambiguous = false;
                for (var i = 1; i < split.Count; i++)
                {
                    var at = i;
                    while (at > 0)
                    {
                        var order = CompareAlong(line, split[at - 1], split[at]);
                        if (order is null) { ambiguous = true; break; }
                        if (order <= 0) break;
                        (split[at - 1], split[at]) = (split[at], split[at - 1]); at--;
                    }
                }
                if (ambiguous) { pending.Add(new Relation(line.Stroke, S(split.Select(node => (IElement)node.Surface).ToArray()))); continue; }
            }
            for (var i = 0; i + 1 < split.Count; i++)
            {
                var from = split[i]; var to = split[i + 1];
                if (ReferenceEquals(from, to)) continue;
                var word = from.Word is not null && to.Word is not null ? Difference(to.Word, from.Word) : line.Word;
                if (Sector(word) is not int sector) { pending.Add(new Relation(line.Stroke, word)); continue; }
                var prior = edges.FirstOrDefault(edge => ReferenceEquals(edge.From, from) && ReferenceEquals(edge.To, to) ||
                    ReferenceEquals(edge.From, to) && ReferenceEquals(edge.To, from));
                var use = new Relation(line.Stroke, new Relation(from.Site, to.Site));
                if (prior is null) edges.Add(new(from, to, word, sector, use));
                else prior.Add(use);
            }
        }
        Nodes = nodes; Edges = edges;
        Graph = new(frame, S(nodes.Select(node => node.Site).ToArray()), S(edges.Select(edge => (IElement)edge.Stroke).ToArray()));
        Bindings = S(nodes.Where(node => node.Word is not null).Select(node => (IElement)new Relation(node.Site, node.Word!)).ToArray());
        var allDarts = edges.SelectMany(edge => new[] { new Dart(edge, false, frame), new Dart(edge, true, frame) }).ToArray();
        var outgoing = nodes.ToDictionary(node => node, node => allDarts.Where(dart => ReferenceEquals(dart.From, node)).ToArray());
        var walked = new HashSet<Dart>(); var boundaries = new List<Boundary>(); var exterior = new List<IElement>();
        foreach (var first in allDarts)
        {
            if (walked.Contains(first)) continue;
            var walk = new List<Dart>(); var local = new HashSet<Dart>(); var current = first;
            var turns = new List<IElement>(); var winding = 0; var finished = false;
            while (local.Add(current))
            {
                walk.Add(current); walked.Add(current);
                var reverse = (current.Sector + 4) % 8;
                var choices = outgoing[current.To].Select(dart => (Dart: dart, Steps: (reverse - dart.Sector + 8) % 8))
                    .Where(choice => !ReferenceEquals(choice.Dart.Edge, current.Edge)).ToArray();
                if (choices.Length == 0) { openWalks.Add(new Relation(current.Surface, S())); break; }
                var closest = choices.Min(choice => choice.Steps);
                var next = choices.Where(choice => choice.Steps == closest).ToArray();
                if (next.Length != 1 || next[0].Steps == 0)
                { pending.Add(new Relation(current.Surface, S(next.Select(choice => (IElement)choice.Dart.Surface).ToArray()))); break; }
                var selected = next[0].Dart;
                var turn = (selected.Sector - current.Sector + 8) % 8;
                if (turn > 4) turn -= 8;
                if (turn == 4) { pending.Add(new Relation(current.Surface, selected.Surface)); break; }
                if (turn == 0 && current.Sector % 2 != 0)
                    pending.Add(new Relation(current.Surface, selected.Surface));
                winding += turn;
                // The turn keeps the actual incoming/outgoing sector scopes and
                // their selected directed route through the cyclic basis.
                var route = new List<IElement>(); var cursor = current.Sector;
                for (var count = 0; count < Math.Abs(turn); count++)
                {
                    var following = (cursor + (turn < 0 ? 7 : 1)) % 8;
                    route.Add(new Relation(sectors[cursor], sectors[following])); cursor = following;
                }
                turns.Add(new Relation(new Relation(current.Surface, selected.Surface), S(route.ToArray())));
                if (ReferenceEquals(selected, first)) { finished = true; break; }
                current = selected;
            }
            if (!finished) { if (walk.Count != 0) openWalks.Add(S(walk.Select(dart => (IElement)dart.Surface).ToArray())); continue; }
            var distinct = walk.Select(dart => dart.From).Distinct().Count() == walk.Count;
            var nativeWalk = new Relation(S(walk.Select(dart => (IElement)dart.Surface).ToArray()), S(turns.ToArray()));
            if (!distinct || winding != 8) { exterior.Add(new Relation(Basis, nativeWalk)); continue; }
            if (boundaries.Count == faceCapacity) { cuts.Add(nativeWalk); continue; }
            boundaries.Add(new(walk.ToArray(), S(turns.ToArray()), new Relation(new Relation(Port, Basis), nativeWalk)));
        }
        // A positive simple face walk can still enclose a disconnected island.
        // This separate read prevents chordlessness from pretending to certify
        // empty interior. Axis contours admit a native ordered-ray parity read;
        // unknown diagonal nesting remains explicitly unaccounted participation.
        foreach (var boundary in boundaries)
        {
            var obstructions = new List<IElement>(); var unknown = new List<IElement>();
            var boundaryNodes = boundary.Darts.Select(dart => dart.From).ToHashSet();
            foreach (var node in nodes)
            {
                if (boundaryNodes.Contains(node) || !edges.Any(edge => ReferenceEquals(edge.From, node) || ReferenceEquals(edge.To, node))) continue;
                var inside = InsideBoundary(boundary, node, out var proof);
                if (inside == true) obstructions.Add(new Relation(node.Surface, proof));
                else if (inside is null) unknown.Add(new Relation(node.Surface, proof));
            }
            boundary.Qualify(S(obstructions.ToArray()), S(unknown.ToArray()));
        }
        Boundaries = boundaries; EmbeddingUnknown = S(pending.ToArray()); OpenWalks = S(openWalks.ToArray());
        Unresolved = S(pending.Concat(openWalks).ToArray()); Unsearched = S(cuts.ToArray()); Exterior = S(exterior.ToArray());
        Surface = new(new Relation(Port, Basis), new Relation(new Relation(Graph.Surface, Bindings),
            new Relation(S(nodes.Select(node => (IElement)node.Surface).ToArray()),
                new Relation(S(edges.Select(edge => (IElement)edge.Surface).ToArray()),
                    new Relation(S(boundaries.Select(boundary => (IElement)boundary.Surface).ToArray()),
                        new Relation(Exterior, new Relation(new Relation(EmbeddingUnknown, OpenWalks), Unsearched)))))));

        Relation? ReadPosition(IElement site)
        {
            var bindings = Entries(source.Bindings).OfType<Relation>().Where(row => N.Same(row.Left, site)).ToArray();
            if (!Closed(source.Bindings) || bindings.Length != 1 || bindings[0].Right is not Relation word || Reduce(word) is not Relation result)
            { pending.Add(new Relation(site, S(bindings.Cast<IElement>().ToArray()))); return null; }
            return result;
        }
        Relation? Reduce(Relation word)
        {
            var read = frame.Reduce(S(), word);
            return frame.TryReadResult(read, out var remaining, out _, out var unknown) && Closed(remaining) && Entries(unknown).Length == 0 ? remaining : null;
        }
        Relation Difference(Relation first, Relation second)
        {
            var read = frame.Reduce(first, UnitArrangementRead.Inverse(frame, second));
            return frame.TryReadResult(read, out var remaining, out _, out _) ? remaining : StructuralConstraint.Open();
        }
        bool Equal(Relation a, Relation b) => Reduce(Difference(a, b)) is Relation reduced && Entries(reduced).Length == 0;
        Relation Project(Relation word, Relation axis) => S(Entries(word).OfType<Relation>()
            .Where(move => frame.Direction(move, out var actual, out _, out _) && ReferenceEquals(actual, axis)).Cast<IElement>().ToArray());
        int? Sign(Relation word, Relation axis)
        {
            var remaining = Reduce(Project(word, axis)); if (remaining is null) return null;
            var terms = Entries(remaining); if (terms.Length == 0) return 0;
            bool? face = null;
            foreach (var term in terms)
            {
                if (term is not Relation move || !frame.Direction(move, out _, out var positive, out _)) return null;
                if (face is not null && face != positive) return null; face = positive;
            }
            return face == true ? 1 : -1;
        }
        int? Sector(Relation word)
        {
            var x = Sign(word, frame.Horizontal); var y = Sign(word, frame.Vertical);
            return (x, y) switch { (1, 0) => 0, (1, 1) => 1, (0, 1) => 2, (-1, 1) => 3,
                (-1, 0) => 4, (-1, -1) => 5, (0, -1) => 6, (1, -1) => 7, _ => null };
        }
        Relation? Axis(Line line) => line.Sector is 0 or 4 ? frame.Horizontal : line.Sector is 2 or 6 ? frame.Vertical : null;
        int? CompareAlong(Line line, Node first, Node second)
        {
            if (first.Word is null || second.Word is null) return null;
            var value = Sign(Difference(first.Word, second.Word), Axis(line)!);
            return line.Sector is 4 or 6 ? -value : value;
        }
        bool? Contains(Line line, Relation point)
        {
            var axis = Axis(line); if (axis is null || line.From.Word is null || line.To.Word is null) return null;
            var transverse = ReferenceEquals(axis, frame.Horizontal) ? frame.Vertical : frame.Horizontal;
            var across = Sign(Difference(point, line.From.Word), transverse);
            if (across is null) return null; if (across != 0) return false;
            var before = Sign(Difference(point, line.From.Word), axis); var after = Sign(Difference(line.To.Word, point), axis);
            if (line.Sector is 4 or 6) { before = -before; after = -after; }
            if (before is < 0 || after is < 0) return false;
            return before is null || after is null ? null : true;
        }
        bool DisjointBoxes(Line a, Line b)
        {
            foreach (var axis in new[] { frame.Horizontal, frame.Vertical })
            {
                var comparisons = new[] { Sign(Difference(a.From.Word!, b.From.Word!), axis), Sign(Difference(a.From.Word!, b.To.Word!), axis),
                    Sign(Difference(a.To.Word!, b.From.Word!), axis), Sign(Difference(a.To.Word!, b.To.Word!), axis) };
                if (comparisons.All(sign => sign == 1) || comparisons.All(sign => sign == -1)) return true;
            }
            return false;
        }
        bool ParallelAndSeparated(Line a, Line b)
        {
            if (!Equal(a.Word, b.Word) && !Equal(a.Word, UnitArrangementRead.Inverse(frame, b.Word))) return false;
            var offset = Sector(Difference(b.From.Word!, a.From.Word!));
            return offset is int direction && direction != a.Sector && direction != (a.Sector + 4) % 8;
        }
        bool? InsideBoundary(Boundary boundary, Node node, out Relation proof)
        {
            proof = new(new Relation(Basis, boundary.Surface), node.Surface);
            if (node.Word is null || boundary.Darts.Any(dart => dart.From.Word is null)) return null;
            foreach (var axis in new[] { frame.Horizontal, frame.Vertical })
            {
                var relations = boundary.Darts.Select(dart => Sign(Difference(node.Word, dart.From.Word!), axis)).ToArray();
                if (relations.All(sign => sign == 1) || relations.All(sign => sign == -1)) return false;
            }
            if (boundary.Darts.Any(dart => dart.Sector % 2 != 0)) return null;
            var crossings = new List<IElement>();
            foreach (var dart in boundary.Darts.Where(dart => dart.Sector is 2 or 6))
            {
                var low = dart.Sector == 2 ? dart.From.Word! : dart.To.Word!;
                var high = dart.Sector == 2 ? dart.To.Word! : dart.From.Word!;
                var before = Sign(Difference(node.Word, low), frame.Vertical);
                var after = Sign(Difference(high, node.Word), frame.Vertical);
                var right = Sign(Difference(low, node.Word), frame.Horizontal);
                if (before is null || after is null || right is null) return null;
                // Half-open traversal counts a shared endpoint exactly once.
                if (before >= 0 && after > 0 && right > 0)
                    crossings.Add(new Relation(dart.Surface, new Relation(node.Word,
                        new Relation(Difference(node.Word, low), new Relation(Difference(high, node.Word), Difference(low, node.Word))))));
            }
            proof = new(new Relation(new Relation(Basis, boundary.Surface), new Relation(node.Site, node.Word)), S(crossings.ToArray()));
            return crossings.Count % 2 != 0;
        }
    }

    /// <summary>Restores discovery from the exposed graph, endpoint words,
    /// straight-interpolation offers and actual native geometric model alone.
    /// One shared frame rebinding preserves all within-port aliases; no source
    /// factory, supplied face labels or private construction history is read.</summary>
    public static bool TryReadPort(Relation port, out NativeInteriorTopology topology, out Relation witness, int faceCapacity = 256)
    {
        topology = null!; witness = new(port, StructuralConstraint.Open());
        if (port.Left is not Relation { Left: Relation { Left: Relation frameSurface } } ||
            !QualitativeMotionFrameRead.TryReadStandard(frameSurface, out var frame) ||
            !frame.TryRebind(port, out var rebound, out var change) ||
            rebound is not Relation { Left: Relation { Left: Relation graphSurface, Right: Relation words },
                Right: Relation { Left: Relation straight, Right: Relation model } } ||
            graphSurface.Right is not Relation { Left: Relation { Left: Relation sites, Right: Relation strokes }, Right: Relation period } ||
            !ReferenceEquals(graphSurface.Left, frame.Target.Surface) ||
            !NativeLineBasis.TryReadInFrame(frame.Target, model, out var basis, out var basisRead)) return false;
        var graph = new QualitativeStrokeGraph(frame.Target, sites, strokes, period);
        var source = new NativeLatticeSources.SourceCase("imported", "Imported native interior port", "Only the exposed native structure is available.",
            graph, words, straight, S(), false);
        topology = new(source, basis, faceCapacity);
        witness = new(port, new Relation(change, new Relation(new Relation(graphSurface, graph.Surface), new Relation(basisRead, topology.Port))));
        return true;
    }

    public sealed class Node
    {
        private readonly List<IElement> sourceSites = [];
        private readonly List<IElement> encounters = [];
        public IElement Site { get; }
        public Relation? Word { get; }
        public Relation SourceSites => S(sourceSites.ToArray());
        public Relation Encounters => S(encounters.ToArray());
        public Relation Surface => new(new Relation(Site, Word ?? StructuralConstraint.Open()), new Relation(SourceSites, Encounters));
        internal Node(IElement site, Relation? word) { Site = site; Word = word; }
        internal void Add(IElement source, Relation proof) { sourceSites.Add(source); encounters.Add(proof); }
        internal void AddEncounter(Relation proof) => encounters.Add(proof);
    }
    public sealed class Edge
    {
        private readonly List<IElement> uses = [];
        public Node From { get; }
        public Node To { get; }
        public Relation Word { get; }
        public int Sector { get; }
        public Relation Stroke { get; }
        public Relation SourceUses => S(uses.ToArray());
        public Relation Surface => new(Stroke, SourceUses);
        internal Edge(Node from, Node to, Relation word, int sector, Relation use)
        { From = from; To = to; Word = word; Sector = sector; Stroke = QualitativeStrokeGraph.Stroke(new Placeholder(), from.Site, to.Site, word); uses.Add(use); }
        internal void Add(Relation use) => uses.Add(use);
    }
    public sealed class Dart
    {
        public Edge Edge { get; }
        public Node From { get; }
        public Node To { get; }
        public Relation Word { get; }
        public int Sector { get; }
        public Relation Surface { get; }
        internal Dart(Edge edge, bool reversed, QualitativeMotionFrame frame)
        {
            Edge = edge; From = reversed ? edge.To : edge.From; To = reversed ? edge.From : edge.To;
            Word = reversed ? UnitArrangementRead.Inverse(frame, edge.Word) : edge.Word;
            Sector = (edge.Sector + (reversed ? 4 : 0)) % 8;
            Surface = new(edge.Stroke, new Relation(From.Site, To.Site));
        }
    }
    public sealed class Boundary
    {
        public IReadOnlyList<Dart> Darts { get; }
        public bool Closed => true;
        public bool Inferred => false;
        public Relation Turns { get; }
        public Relation InteriorObstructions { get; private set; } = S();
        public Relation InteriorUnknown { get; private set; } = S();
        public Relation Surface { get; private set; }
        internal Boundary(IReadOnlyList<Dart> darts, Relation turns, Relation surface) { Darts = darts; Turns = turns; Surface = surface; }
        internal void Qualify(Relation obstructions, Relation unknown)
        { InteriorObstructions = obstructions; InteriorUnknown = unknown; Surface = new(Surface, new Relation(obstructions, unknown)); }
    }
    private sealed record Line(Relation Stroke, Node From, Node To, Relation Word, int Sector);
    private static bool SharesEndpoint(Line a, Line b) => ReferenceEquals(a.From, b.From) || ReferenceEquals(a.From, b.To) ||
        ReferenceEquals(a.To, b.From) || ReferenceEquals(a.To, b.To);
    private static Relation S(params IElement[] entries) => N.S(entries);
    private static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var head, out var tail) && head.Equals(tail);
}
