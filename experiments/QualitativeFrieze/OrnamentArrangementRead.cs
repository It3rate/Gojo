using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A whole-incidence question at a declared route resolution. This
/// reader preserves branch/loop/component participation and route multiplicity;
/// it does not claim equal heading, shape, embedding, crossing or ancestry.</summary>
public static class OrnamentArrangementRead
{
    // question = fresh public template -> (Closed(retained template sites) ->
    //   (Closed(atomic template segments) -> Closed(path template segments))).
    // Atomic mode retains all sites and one segment per route. Path mode keeps
    // every non-degree-two site and one arbitrary reference per isolated cycle.
    // Its routes may contain any nonempty degree-two chain. The two segment
    // scopes partition the complete template segment inventory; mixed mode is
    // a valid exploratory contract for which this reader leaves an Open result.
    // resolution = (observation -> Closed(retained sites)) ->
    //   (Closed(routes) -> (Closed(segment -> actual route) -> Closed(remainder))).
    // route = (actual first site -> actual last site) ->
    //   Closed(actual segment -> (traversed from site -> traversed to site)).
    // witness = Closed(template anchor -> target anchor) ->
    //   Closed(template route -> (target route -> Closed(ordered traversal))).

    /// <summary>Forgets unselected heading/encounter/detail information and
    /// stores the whole route question as a smaller independent native graph.
    /// Source correspondence and discarded complement are returned separately;
    /// no source occurrence or execution is reachable through the question.
    /// A returning route needs a nonempty native cycle. This builder chooses
    /// three segments; it does not claim a globally minimal encoding.</summary>
    public static Question Crystallize(string name, QualitativeRecognitionObservation source, out Relation witness)
    {
        var blocked = new List<IElement>(); var open = new List<IElement>();
        var graph = Graph.Read(source, blocked, open);
        if (graph is null || blocked.Count != 0 || open.Count != 0)
        {
            var unresolved = new Question(name, source);
            witness = new(source.Surface, new Relation(unresolved.Surface, new Relation(S(), new Relation(S(open.ToArray()), S(blocked.ToArray())))));
            return unresolved;
        }
        var resolution = Resolve(graph, Anchors(graph));
        var frame = new QualitativeMotionFrame();
        var sites = resolution.Anchors.Select(_ => (IElement)new Placeholder()).ToList();
        var segments = new List<IElement>(); var routeRanges = new List<(Route Source, int Start, int Length)>();
        foreach (var route in resolution.Routes)
        {
            var start = segments.Count;
            if (route.From != route.To) Segment(sites[route.From], sites[route.To]);
            else
            {
                var first = new Placeholder(); var second = new Placeholder(); sites.Add(first); sites.Add(second);
                Segment(sites[route.From], first); Segment(first, second); Segment(second, sites[route.To]);
            }
            routeRanges.Add((route, start, segments.Count - start));
        }
        var result = new Question(name, new QualitativeRecognitionObservation(frame, S(sites.ToArray()), S(segments.ToArray()),
            StructuralConstraint.Open(), StructuralConstraint.Open()));
        var anchors = E(result.Template.Sites); var chords = E(result.Template.Segments);
        var siteMap = S(resolution.Anchors.Select((anchor, index) => (IElement)new Relation(anchor, anchors[index])).ToArray());
        var routeMap = S(routeRanges.Select(range => (IElement)new Relation(range.Source.Surface,
            S(chords.Skip(range.Start).Take(range.Length).ToArray()))).ToArray());
        witness = new(source.Surface, new Relation(result.Surface, new Relation(resolution.Surface,
            new Relation(new Relation(siteMap, routeMap), Complement(source)))));
        return result;

        void Segment(IElement from, IElement to) => segments.Add(new Relation(new Placeholder(),
            new Relation(new Relation(from, to), StructuralConstraint.Open())));
    }

    public sealed class Question
    {
        public string Name { get; }
        public Relation Surface { get; }
        public QualitativeRecognitionObservation Template { get; } = null!;
        public Relation Retained { get; } = S();
        public Relation AtomicSegments { get; } = S();
        public Relation PathSegments { get; } = S();
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool AllowSubdivision { get; }
        public bool IsValid => E(Blocked).Length == 0 && E(Open).Length == 0;

        public Question(string name, QualitativeRecognitionObservation template, bool allowSubdivision = true)
            : this(name, Make(template, allowSubdivision)) { }

        public Question(string name, Relation surface)
        {
            Name = name; Surface = surface;
            var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation { Left: Relation { Left: Relation frame } },
                    Right: Relation { Left: Relation retained, Right: Relation { Left: Relation atomic, Right: Relation paths } } })
                blocked.Add(surface);
            else if (!QualitativeMotionFrameRead.TryReadStandard(frame, out var frameRead) ||
                !frameRead.TryRebind(surface, out var renamed, out _) || renamed is not Relation normalized ||
                normalized.Left is not Relation { Left: Relation { Right: Relation sites },
                    Right: Relation { Left: Relation segments, Right: Relation { Left: Relation continuations, Right: Relation crossings } } })
                blocked.Add(surface);
            else
            {
                Surface = normalized;
                Template = new(frameRead.Target, sites, segments, continuations, crossings);
                var details = (Relation)normalized.Right;
                Retained = (Relation)details.Left;
                AtomicSegments = (Relation)((Relation)details.Right).Left;
                PathSegments = (Relation)((Relation)details.Right).Right;
                foreach (var scope in new[] { Retained, AtomicSegments, PathSegments }) Scope(scope, blocked, open);
                var graph = Graph.Read(Template, blocked, open);
                if (graph is not null && open.Count == 0 && blocked.Count == 0)
                {
                    var anchors = E(Retained); var atoms = E(AtomicSegments); var routes = E(PathSegments);
                    if (!N.Distinct(anchors) || anchors.Any(site => !graph.Sites.Any(actual => N.Same(site, actual))) ||
                        !N.Distinct(atoms.Concat(routes).ToArray()) || atoms.Concat(routes).Any(edge => !graph.Edges.Any(actual => ReferenceEquals(actual.Row, edge))) ||
                        graph.Edges.Any(edge => !atoms.Concat(routes).Any(actual => ReferenceEquals(actual, edge.Row)))) blocked.Add(normalized);
                    AllowSubdivision = routes.Length != 0;
                    if (atoms.Length != 0 && routes.Length != 0) open.Add(new Relation(AtomicSegments, PathSegments));
                    else if (!AllowSubdivision && !SameSites(anchors, graph.Sites)) blocked.Add(Retained);
                    else if (AllowSubdivision && !ValidAnchors(graph, anchors)) blocked.Add(Retained);
                }
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }

        private static Relation Make(QualitativeRecognitionObservation source, bool paths)
        {
            // Alpha-copy every participant, including site and segment roles.
            // Neither author occurrence nor retained execution enters the query.
            var cloned = (Relation)Clone(source.Surface);
            var sites = (Relation)((Relation)cloned.Left).Right;
            var segments = (Relation)((Relation)cloned.Right).Left;
            var graphSites = E(sites);
            var rows = E(segments).OfType<Relation>().ToArray();
            var retained = paths ? graphSites.Where(site => rows.Count(row => row.Right is Relation { Left: Relation ends } &&
                    (N.Same(ends.Left, site) || N.Same(ends.Right, site))) != 2).ToList() : graphSites.ToList();
            if (paths)
            {
                // A cycle has no branch anchor. One reference preserves its
                // whole nonzero traversal; matching may rotate/reverse it.
                var pending = graphSites.ToHashSet(N.Participants.Instance);
                while (pending.Count != 0)
                {
                    var component = new HashSet<IElement>(N.Participants.Instance); var queue = new Queue<IElement>();
                    queue.Enqueue(pending.First());
                    while (queue.TryDequeue(out var site))
                    {
                        if (!component.Add(site)) continue; pending.Remove(site);
                        foreach (var row in rows)
                            if (row.Right is Relation { Left: Relation ends })
                            {
                                if (N.Same(ends.Left, site)) queue.Enqueue(ends.Right);
                                if (N.Same(ends.Right, site)) queue.Enqueue(ends.Left);
                            }
                    }
                    if (!retained.Any(component.Contains)) retained.Add(component.First());
                }
            }
            return new(cloned, new Relation(S(retained.ToArray()), new Relation(paths ? S() : segments, paths ? segments : S())));
        }
    }

    public sealed class Receipt
    {
        public Question Question { get; }
        public QualitativeRecognitionObservation Target { get; }
        public Relation SourceResolution { get; }
        public Relation TargetResolution { get; }
        public Relation Correspondence { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Carried { get; }
        public Relation Budget { get; }
        public Relation Consumed { get; }
        public Relation Surface { get; }
        public int Work { get; }
        public string Answer => E(Blocked).Length != 0 ? "Blocked" : E(Supported).Length != 0 ? "Yes" :
            E(Refuted).Length != 0 ? "No" : "Open";

        public Receipt(Question question, QualitativeRecognitionObservation target, int capacity = 20000)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Question = question; Target = target; Budget = Capacity(capacity);
            var blocked = E(question.Blocked).ToList(); var open = E(question.Open).ToList();
            var yes = new List<IElement>(); var no = new List<IElement>(); var consumed = new List<IElement>();
            SourceResolution = TargetResolution = Correspondence = S();
            Carried = question.Template is null ? new(target.Continuations, target.Crossings) : new(
                Complement(question.Template), Complement(target));
            if (blocked.Count == 0 && open.Count == 0)
            {
                var source = Graph.Read(question.Template!, blocked, open);
                var destination = Graph.Read(target, blocked, open);
                if (source is not null && destination is not null && blocked.Count == 0 && open.Count == 0)
                {
                    var a = Resolve(source, E(question.Retained));
                    var b = Resolve(destination, question.AllowSubdivision ? Anchors(destination) : destination.Sites);
                    SourceResolution = a.Surface; TargetResolution = b.Surface;
                    if (a.Anchors.Length != b.Anchors.Length || a.Routes.Length != b.Routes.Length ||
                        !a.Degrees.Order().SequenceEqual(b.Degrees.Order())) no.Add(new Relation(a.Surface, b.Surface));
                    else
                    {
                        var mapping = Enumerable.Repeat(-1, a.Anchors.Length).ToArray(); var occupied = new bool[b.Anchors.Length];
                        var order = Enumerable.Range(0, a.Anchors.Length).OrderByDescending(index => a.Degrees[index])
                            .ThenByDescending(index => a.Between(index, index)).ToArray();
                        var exhausted = false;
                        if (Find(0))
                        {
                            var siteMap = S(mapping.Select((mapped, index) => (IElement)new Relation(a.Anchors[index], b.Anchors[mapped])).ToArray());
                            var available = b.Routes.ToList(); var routeMap = new List<IElement>();
                            foreach (var route in a.Routes)
                            {
                                var from = mapping[route.From]; var to = mapping[route.To];
                                var actual = available.First(other => other.From == from && other.To == to || other.From == to && other.To == from);
                                available.Remove(actual);
                                var traversed = actual.From == from ? actual.Traversal : Reverse(actual.Traversal);
                                routeMap.Add(new Relation(route.Surface, new Relation(actual.Surface, traversed)));
                            }
                            Correspondence = new(siteMap, S(routeMap.ToArray()));
                            yes.Add(new Relation(new Relation(a.Surface, b.Surface), Correspondence));
                        }
                        else if (exhausted) open.Add(new Relation(Budget, new Relation(a.Surface, b.Surface)));
                        else no.Add(new Relation(new Relation(a.Surface, b.Surface), S(consumed.ToArray())));

                        bool Find(int depth)
                        {
                            if (depth == order.Length) return true;
                            var selected = order[depth];
                            for (var candidate = 0; candidate < b.Anchors.Length; candidate++)
                            {
                                if (occupied[candidate] || a.Degrees[selected] != b.Degrees[candidate] ||
                                    a.Between(selected, selected) != b.Between(candidate, candidate)) continue;
                                if (consumed.Count == capacity) { exhausted = true; return false; }
                                var premise = S(mapping.Select((mapped, index) => (mapped, index)).Where(pair => pair.mapped >= 0)
                                    .Select(pair => (IElement)new Relation(a.Anchors[pair.index], b.Anchors[pair.mapped])).ToArray());
                                consumed.Add(new Relation(premise, new Relation(a.Anchors[selected], b.Anchors[candidate])));
                                if (mapping.Select((mapped, index) => (mapped, index)).Any(pair => pair.mapped >= 0 &&
                                    a.Between(selected, pair.index) != b.Between(candidate, pair.mapped))) continue;
                                mapping[selected] = candidate; occupied[candidate] = true;
                                if (Find(depth + 1)) return true;
                                mapping[selected] = -1; occupied[candidate] = false;
                                if (exhausted) return false;
                            }
                            return false;
                        }
                    }
                }
            }
            Work = consumed.Count; Consumed = S(consumed.ToArray());
            Supported = S(yes.ToArray()); Refuted = S(no.ToArray()); Open = S(open.ToArray()); Blocked = S(blocked.ToArray());
            Surface = new(new Relation(new Relation(question.Surface, target.Surface), Budget),
                new Relation(SourceResolution, new Relation(TargetResolution, new Relation(Consumed,
                    new Relation(Supported, new Relation(Refuted, new Relation(Open, new Relation(Blocked, Carried))))))));
        }
    }

    private sealed record Edge(Relation Row, int From, int To);
    private sealed class Graph(QualitativeRecognitionObservation observation, IElement[] sites, Edge[] edges)
    {
        internal QualitativeRecognitionObservation Observation { get; } = observation;
        internal IElement[] Sites { get; } = sites;
        internal Edge[] Edges { get; } = edges;
        internal int Degree(int site) => Edges.Count(edge => edge.From == site || edge.To == site);
        internal static Graph? Read(QualitativeRecognitionObservation observation, List<IElement> blocked, List<IElement> open)
        {
            Scope(observation.Sites, blocked, open); Scope(observation.Segments, blocked, open);
            var sites = E(observation.Sites); var rows = E(observation.Segments); var edges = new List<Edge>();
            if (!N.Distinct(sites)) blocked.Add(observation.Sites);
            foreach (var item in rows)
            {
                if (item is not Relation { Right: Relation { Left: Relation ends, Right: Relation } } row)
                { blocked.Add(item); continue; }
                var from = Array.FindIndex(sites, site => N.Same(site, ends.Left));
                var to = Array.FindIndex(sites, site => N.Same(site, ends.Right));
                if (from < 0 || to < 0 || from == to || edges.Any(edge => N.Same(edge.Row.Left, row.Left)))
                { blocked.Add(row); continue; }
                edges.Add(new(row, from, to));
            }
            return new(observation, sites, edges.ToArray());
        }
    }

    private sealed record Route(int From, int To, Relation Traversal, Relation Surface);
    private sealed class Resolution(IElement[] anchors, Route[] routes, Relation surface)
    {
        internal IElement[] Anchors { get; } = anchors;
        internal Route[] Routes { get; } = routes;
        internal Relation Surface { get; } = surface;
        internal int[] Degrees { get; } = Enumerable.Range(0, anchors.Length)
            .Select(index => routes.Sum(route => (route.From == index ? 1 : 0) + (route.To == index ? 1 : 0))).ToArray();
        internal int Between(int a, int b) => Routes.Count(route => route.From == a && route.To == b || route.From == b && route.To == a);
    }

    private static Resolution Resolve(Graph graph, IElement[] anchors)
    {
        var anchorIndices = anchors.Select(anchor => Array.FindIndex(graph.Sites, site => N.Same(site, anchor))).ToArray();
        var used = new bool[graph.Edges.Length]; var routes = new List<Route>(); var coverage = new List<IElement>();
        for (var anchor = 0; anchor < anchors.Length; anchor++)
            for (var edgeIndex = 0; edgeIndex < graph.Edges.Length; edgeIndex++)
            {
                var first = graph.Edges[edgeIndex];
                if (used[edgeIndex] || first.From != anchorIndices[anchor] && first.To != anchorIndices[anchor]) continue;
                var traversal = new List<IElement>(); var cursor = anchorIndices[anchor]; var current = edgeIndex; var nextAnchor = -1;
                while (true)
                {
                    var edge = graph.Edges[current]; used[current] = true;
                    var next = edge.From == cursor ? edge.To : edge.From;
                    traversal.Add(new Relation(edge.Row, new Relation(graph.Sites[cursor], graph.Sites[next])));
                    nextAnchor = Array.IndexOf(anchorIndices, next);
                    if (nextAnchor >= 0) break;
                    cursor = next;
                    current = Array.FindIndex(graph.Edges, candidate => !ReferenceEquals(candidate, edge) &&
                        (candidate.From == cursor || candidate.To == cursor));
                    if (current < 0 || used[current]) throw new InvalidOperationException("A route partition needs a retained endpoint or cycle reference.");
                }
                var scope = S(traversal.ToArray()); var surface = new Relation(new Relation(anchors[anchor], anchors[nextAnchor]), scope);
                routes.Add(new(anchor, nextAnchor, scope, surface));
                coverage.AddRange(traversal.Cast<Relation>().Select(row => (IElement)new Relation(row.Left, surface)));
            }
        var remainder = graph.Edges.Where((_, index) => !used[index]).Select(edge => (IElement)edge.Row).ToArray();
        if (remainder.Length != 0) throw new InvalidOperationException("A closed arrangement resolution must cover every published segment.");
        return new(anchors, routes.ToArray(), new Relation(new Relation(graph.Observation.Surface, S(anchors)),
            new Relation(S(routes.Select(route => (IElement)route.Surface).ToArray()), new Relation(S(coverage.ToArray()), S(remainder)))));
    }

    private static IElement[] Anchors(Graph graph)
    {
        var anchors = Enumerable.Range(0, graph.Sites.Length).Where(index => graph.Degree(index) != 2).ToList();
        foreach (var component in Components(graph))
            if (!anchors.Any(component.Contains)) anchors.Add(component[0]);
        return anchors.Select(index => graph.Sites[index]).ToArray();
    }

    private static bool ValidAnchors(Graph graph, IElement[] anchors)
    {
        var indices = anchors.Select(anchor => Array.FindIndex(graph.Sites, site => N.Same(site, anchor))).ToArray();
        foreach (var component in Components(graph))
        {
            var branches = component.Where(index => graph.Degree(index) != 2).ToArray();
            var chosen = indices.Where(component.Contains).ToArray();
            if (branches.Length != 0 ? chosen.Length != branches.Length || branches.Any(index => !chosen.Contains(index)) : chosen.Length != 1) return false;
        }
        return true;
    }

    private static IEnumerable<int[]> Components(Graph graph)
    {
        var visited = new bool[graph.Sites.Length];
        for (var start = 0; start < graph.Sites.Length; start++)
        {
            if (visited[start]) continue;
            var component = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(start);
            while (queue.TryDequeue(out var site))
            {
                if (visited[site]) continue; visited[site] = true; component.Add(site);
                foreach (var edge in graph.Edges)
                { if (edge.From == site) queue.Enqueue(edge.To); if (edge.To == site) queue.Enqueue(edge.From); }
            }
            yield return component.ToArray();
        }
    }

    private static Relation Complement(QualitativeRecognitionObservation observation) => new(
        S(E(observation.Segments).OfType<Relation>().Where(row => row.Right is Relation { Right: Relation })
            .Select(row => (IElement)new Relation(row, ((Relation)row.Right).Right)).ToArray()),
        new Relation(observation.Continuations, observation.Crossings));
    private static Relation Reverse(Relation traversal) => S(E(traversal).Reverse().Cast<Relation>()
        .Select(row => (IElement)new Relation(row.Left, new Relation(((Relation)row.Right).Right, ((Relation)row.Right).Left))).ToArray());
    private static bool SameSites(IElement[] a, IElement[] b) => a.Length == b.Length && a.All(site => b.Any(other => N.Same(site, other)));
    private static void Scope(Relation scope, List<IElement> blocked, List<IElement> open)
    {
        if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)) blocked.Add(scope);
        else if (!N.Same(seam, tail)) open.Add(scope);
    }
    private static IElement Clone(IElement source)
    {
        var copies = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var pending = new Stack<(IElement Item, bool Finish)>(); pending.Push((source, false));
        while (pending.TryPop(out var item))
        {
            if (copies.ContainsKey(item.Item)) continue;
            if (item.Item is Placeholder slot)
            {
                if (copies.TryGetValue(slot.Dual(), out var opposed)) copies.Add(slot, opposed.Dual());
                else { var fresh = new Placeholder(); copies.Add(slot, slot.IsInverse ? fresh.Dual() : fresh); }
            }
            else if (item.Item is Relation relation)
            {
                if (!item.Finish) { pending.Push((relation, true)); pending.Push((relation.Right, false)); pending.Push((relation.Left, false)); }
                else copies.Add(relation, new Relation(copies[relation.Left], copies[relation.Right]));
            }
            else throw new ArgumentException("An unmeasured arrangement template contains only relation and placeholder participation.", nameof(source));
        }
        return copies[source];
    }
    private static Relation Capacity(int count)
    {
        IElement power = new Placeholder(); var quantum = power; var selected = new List<IElement>();
        while (count != 0) { if ((count & 1) != 0) selected.Add(power); count >>= 1; if (count != 0) power = new Relation(power, power); }
        return new(quantum, S(selected.ToArray()));
    }
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
