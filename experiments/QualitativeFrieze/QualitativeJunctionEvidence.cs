using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A bounded observational read. Actual incidence and formal projected
/// support are separate families. Only axis-aligned interval intersections are
/// solved; unsupported angular intersections stay open. A proposed bridge or
/// depth order never edits the source graph.</summary>
public sealed class QualitativeJunctionEvidence
{
    // Surface = graph.Surface -> Closed(actual-site -> formal-position-word)
    // read = Surface -> (actual-junctions -> (projected-crossings ->
    //        (facing-gaps -> (candidates -> (blocked -> open)))))
    // crossing = ((actual-stroke -> actual-stroke) -> position-word) ->
    //            Closed(actual-bindings, interval/order/cancellation proofs)
    // gap = ((actual-endpoint -> actual-endpoint) -> proposed-chord-word) ->
    //       Closed(actual-ray-pair, actual-bindings, displacement proof)
    // candidate = actual-cue -> (Closed(proposed-bridges) ->
    //             (Closed(retained-cut-endpoints) ->
    //             (Closed(proposed-front -> proposed-back) -> Open(obligations))))
    // A nonempty bridge scope and retained cuts expose different hypotheses.
    // Depth orders are proposals in a candidate, not measured depth or topology.
    public QualitativeStrokeGraph Graph { get; }
    public Relation Bindings { get; }
    public Relation Surface { get; }
    private Relation? read;

    public sealed record Summary(int ActualJoinedSites, int ActualStrokePairConnections,
        int ActualComponents, int ProjectedCrossings, int FacingGaps, int Candidates,
        int Blocked, int Open, int StrictCrossings, int ProjectedContacts);

    public QualitativeJunctionEvidence(QualitativeStrokeGraph graph, Relation bindings)
    {
        Graph = graph; Bindings = bindings; Surface = new(graph.Surface, bindings);
        if (Participants(Surface).Any(element => element is not (Relation or Placeholder)))
            throw new ArgumentException("This unmeasured read accepts only relation/placeholder participation.");
    }

    public Relation Evaluate()
    {
        if (read is not null) return read;
        Graph.TryReadResult(Graph.Evaluate(), out var junctions, out _, out var graphCuts, out var graphOpen);
        var crossings = new List<IElement>(); var gaps = new List<IElement>();
        var candidates = new List<IElement>(); var blocked = new List<IElement>(); var open = new List<IElement>();
        var bindings = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var valid = Closed(Graph.Sites) && Closed(Graph.Strokes) && Closed(Bindings);
        if (E(graphCuts).Length != 0 || E(graphOpen).Length != 0)
        { open.Add(new Relation(graphCuts, graphOpen)); valid = false; }
        if (!valid) open.Add(Surface);
        foreach (var entry in E(Bindings))
        {
            if (entry is not Relation { Right: Relation word } binding ||
                !E(Graph.Sites).Any(site => ReferenceEquals(site, binding.Left)) ||
                !Known(word) || !bindings.TryAdd(binding.Left, binding))
            { open.Add(entry); valid = false; }
        }
        foreach (var site in E(Graph.Sites)) if (!bindings.ContainsKey(site)) { open.Add(site); valid = false; }
        var edges = new List<Edge>();
        if (valid) foreach (var entry in E(Graph.Strokes))
        {
            if (entry is not Relation stroke || !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var chord) ||
                !bindings.TryGetValue(from, out var a) || !bindings.TryGetValue(to, out var b) || !Known(chord))
            { open.Add(entry); continue; }
            var promise = Difference(Join((Relation)a.Right, chord), (Relation)b.Right);
            if (!Zero(promise))
            {
                var claim = new Relation(stroke, promise);
                if (Graph.Frame.TryReadResult(promise, out _, out _, out var unpaidOrder) && E(unpaidOrder).Length != 0) open.Add(claim);
                else blocked.Add(claim);
                continue;
            }
            var reduction = Graph.Frame.Reduce(S(), chord);
            Graph.Frame.TryReadResult(reduction, out var delta, out _, out _);
            if (E(delta).Length == 0) { open.Add(new Relation(stroke, reduction)); continue; }
            var axis = Pure(delta, out _, out var actualAxis) ? actualAxis : null;
            edges.Add(new(stroke, from, to, (Relation)a.Right, (Relation)b.Right, axis));
        }
        for (var i = 0; i < edges.Count; i++) for (var j = i + 1; j < edges.Count; j++)
        {
            var a = edges[i]; var b = edges[j];
            if (SharedEndpoint(a, b)) continue;
            if (a.Axis is null || b.Axis is null)
            { open.Add(new Relation(new Relation(a.Stroke, b.Stroke), Graph.Frame.Surface)); continue; }
            if (ReferenceEquals(a.Axis, b.Axis)) continue;
            if (Cross(a, b, out var point, out var proof, out var uncertain))
            {
                var cue = new Relation(new Relation(new Relation(a.Stroke, b.Stroke), point), proof);
                crossings.Add(cue);
                // Neither possible depth ordering establishes a connection.
                candidates.Add(Candidate(cue, S(), S(), S(new Relation(a.Stroke, b.Stroke))));
                candidates.Add(Candidate(cue, S(), S(), S(new Relation(b.Stroke, a.Stroke))));
            }
            else if (uncertain) open.Add(new Relation(new Relation(a.Stroke, b.Stroke), proof));
        }
        var endpoints = new List<Endpoint>();
        if (valid) foreach (var node in E(junctions).OfType<Relation>())
            if (Graph.TryReadJunction(node, out var site, out var rays, out _, out _) &&
                Closed(rays) && E(rays) is [Relation ray] && bindings.TryGetValue(site, out var binding) &&
                edges.Any(edge => ReferenceEquals(edge.Stroke, ray.Left)) &&
                ray.Right is Relation { Right: Relation word } && Pure(word, out var face, out var axis))
                endpoints.Add(new(site, ray, (Relation)binding.Right, axis, face));
        for (var i = 0; i < endpoints.Count; i++) for (var j = i + 1; j < endpoints.Count; j++)
        {
            var a = endpoints[i]; var b = endpoints[j];
            if (!ReferenceEquals(a.Axis, b.Axis) || a.Forward == b.Forward || ReferenceEquals(a.Ray.Left, b.Ray.Left)) continue;
            var difference = Difference(b.Position, a.Position);
            Graph.Frame.TryReadResult(difference, out var chord, out _, out var unresolved);
            if (E(unresolved).Length != 0)
            { open.Add(new Relation(new Relation(a.Ray, b.Ray), difference)); continue; }
            if (!Pure(chord, out var face, out var axis))
            { blocked.Add(new Relation(new Relation(a.Ray, b.Ray), difference)); continue; }
            // Rays point away from the missing passage: only inward-facing
            // endpoint pairs are possible bridge candidates.
            if (!ReferenceEquals(axis, a.Axis) || face == a.Forward) continue;
            var gap = new Relation(new Relation(new Relation(a.Site, b.Site), chord),
                S(new Relation(a.Ray, b.Ray), bindings[a.Site], bindings[b.Site], difference));
            gaps.Add(gap);
            var virtualEdge = new Edge(gap, a.Site, b.Site, a.Position, b.Position, a.Axis);
            var occluders = new List<IElement>();
            foreach (var edge in edges.Where(edge => edge.Axis is not null && !ReferenceEquals(edge.Axis, a.Axis) &&
                !ReferenceEquals(edge.Stroke, a.Ray.Left) && !ReferenceEquals(edge.Stroke, b.Ray.Left)))
                if (Cross(virtualEdge, edge, out var point, out var proof, out var uncertain))
                    occluders.Add(new Relation(new Relation(edge.Stroke, gap), new Relation(point, proof)));
                else if (uncertain) open.Add(new Relation(new Relation(gap, edge.Stroke), proof));
            var proposedEndpoints = (Relation)((Relation)gap.Left).Left;
            if (occluders.Count == 0) candidates.Add(Candidate(gap, S(proposedEndpoints), S(), S()));
            else foreach (var occluder in occluders.Cast<Relation>())
                candidates.Add(Candidate(new Relation(gap, occluder), S(proposedEndpoints), S(), S(occluder.Left)));
            // Deliberate termination remains an alternative even if another
            // stroke happens to lie in the gap. No hidden strand is inferred.
            candidates.Add(Candidate(gap, S(), S(a.Site, b.Site), S()));
        }
        return read = new(Surface, new Relation(junctions, new Relation(S(crossings.ToArray()),
            new Relation(S(gaps.ToArray()), new Relation(S(candidates.ToArray()),
                new Relation(S(blocked.ToArray()), S(open.ToArray())))))));
    }

    public bool TryReadResult(Relation candidate, out Relation junctions, out Relation crossings,
        out Relation gaps, out Relation candidates, out Relation blocked, out Relation open)
    {
        junctions = crossings = gaps = candidates = blocked = open = null!;
        if (!ReferenceEquals(candidate.Left, Surface) || !ReferenceEquals(candidate.Right, Evaluate().Right) ||
            candidate.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation b,
                Right: Relation { Left: Relation c, Right: Relation { Left: Relation d,
                    Right: Relation { Left: Relation e, Right: Relation f } } } } }) return false;
        junctions = a; crossings = b; gaps = c; candidates = d; blocked = e; open = f; return true;
    }

    public bool TryReadCrossing(Relation cue, out Relation strokePair, out Relation position, out Relation proof)
    {
        strokePair = position = proof = null!;
        TryReadResult(Evaluate(), out _, out var crossings, out _, out _, out _, out _);
        if (!E(crossings).Any(actual => ReferenceEquals(actual, cue)) || cue.Left is not Relation { Left: Relation pair, Right: Relation word } || cue.Right is not Relation witness) return false;
        strokePair = pair; position = word; proof = witness; return true;
    }

    public bool TryReadGap(Relation cue, out Relation endpoints, out Relation chord, out Relation proof)
    {
        endpoints = chord = proof = null!;
        TryReadResult(Evaluate(), out _, out _, out var gaps, out _, out _, out _);
        if (!E(gaps).Any(actual => ReferenceEquals(actual, cue)) || cue.Left is not Relation { Left: Relation pair, Right: Relation word } || cue.Right is not Relation witness) return false;
        endpoints = pair; chord = word; proof = witness; return true;
    }

    /// <summary>Actual endpoints that merely project onto this cue. A nonempty
    /// result distinguishes endpoint contact from strict interior crossing;
    /// these participants are not merged with each other or with a rail.</summary>
    public bool TryReadProjectedContact(Relation cue, out Relation endpoints)
    {
        endpoints = S();
        if (!TryReadCrossing(cue, out var pair, out var position, out _)) return false;
        var selected = new List<IElement>();
        foreach (var stroke in new[] { pair.Left, pair.Right }.OfType<Relation>())
            if (QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _))
                foreach (var site in new[] { a, b })
                    if (E(Bindings).OfType<Relation>().SingleOrDefault(binding => ReferenceEquals(binding.Left, site)) is Relation binding &&
                        Zero(Difference((Relation)binding.Right, position))) selected.Add(site);
        endpoints = S(selected.ToArray()); return selected.Count != 0;
    }

    public bool TryReadCandidate(Relation candidate, out IElement cue, out Relation bridges,
        out Relation retainedCuts, out Relation depthOrders, out Relation obligations)
    {
        cue = null!; bridges = retainedCuts = depthOrders = obligations = null!;
        TryReadResult(Evaluate(), out _, out _, out _, out var candidates, out _, out _);
        if (!E(candidates).Any(actual => ReferenceEquals(actual, candidate)) || candidate.Right is not Relation { Left: Relation a,
            Right: Relation { Left: Relation b, Right: Relation { Left: Relation c, Right: Relation d } } }) return false;
        cue = candidate.Left; bridges = a; retainedCuts = b; depthOrders = c; obligations = d; return true;
    }

    public Summary Describe()
    {
        TryReadResult(Evaluate(), out var nodes, out var crossings, out var gaps, out var candidates, out var blocked, out var open);
        var joins = 0; var pairs = 0;
        foreach (var node in E(nodes).OfType<Relation>())
            if (Graph.TryReadJunction(node, out _, out var rays, out _, out _) && Closed(rays))
            { var count = E(rays).Length; if (count >= 2) joins++; pairs += count * (count - 1) / 2; }
        var unseen = new HashSet<IElement>(E(Graph.Sites), ReferenceEqualityComparer.Instance); var components = 0;
        while (unseen.Count != 0)
        {
            var queue = new Queue<IElement>(); queue.Enqueue(unseen.First()); unseen.Remove(queue.Peek()); components++;
            while (queue.TryDequeue(out var site)) foreach (var stroke in E(Graph.Strokes).OfType<Relation>())
                if (QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                { if (ReferenceEquals(site, from) && unseen.Remove(to)) queue.Enqueue(to); if (ReferenceEquals(site, to) && unseen.Remove(from)) queue.Enqueue(from); }
        }
        var contacts = E(crossings).OfType<Relation>().Count(cue => TryReadProjectedContact(cue, out _));
        return new(joins, pairs, components, E(crossings).Length, E(gaps).Length, E(candidates).Length, E(blocked).Length, E(open).Length,
            E(crossings).Length - contacts, contacts);
    }

    private Relation Candidate(IElement cue, Relation bridges, Relation cuts, Relation depth) =>
        new(cue, new Relation(bridges, new Relation(cuts, new Relation(depth, StructuralConstraint.Open(new Placeholder())))));
    private sealed record Edge(Relation Stroke, IElement From, IElement To, Relation Start, Relation End, Relation? Axis);
    private sealed record Endpoint(IElement Site, Relation Ray, Relation Position, Relation Axis, bool Forward);
    private static bool SharedEndpoint(Edge a, Edge b) => new[] { a.From, a.To }.Any(site => ReferenceEquals(site, b.From) || ReferenceEquals(site, b.To));

    private bool Cross(Edge a, Edge b, out Relation point, out Relation proof, out bool unresolved)
    {
        point = proof = null!; unresolved = false;
        if (a.Axis is null || b.Axis is null || ReferenceEquals(a.Axis, b.Axis)) return false;
        var horizontal = ReferenceEquals(a.Axis, Graph.Frame.Horizontal) ? a : b;
        var vertical = ReferenceEquals(a.Axis, Graph.Frame.Vertical) ? a : b;
        point = Join(Project(vertical.Start, Graph.Frame.Horizontal), Project(horizontal.Start, Graph.Frame.Vertical));
        var insideH = Within(horizontal, point, out var h, out var openH);
        var insideV = Within(vertical, point, out var v, out var openV);
        var endpoints = new[] { horizontal.From, horizontal.To, vertical.From, vertical.To };
        var bindings = E(Bindings).OfType<Relation>().Where(binding => endpoints.Any(site => ReferenceEquals(site, binding.Left)));
        proof = S([.. bindings, new Relation(horizontal.Stroke, h), new Relation(vertical.Stroke, v)]);
        // One known outside interval suffices; otherwise missing order remains
        // open rather than being mistaken for a proven absent crossing.
        if (!insideH && !openH || !insideV && !openV) return false;
        unresolved = openH || openV; return !unresolved && insideH && insideV;
    }

    private bool Within(Edge edge, Relation point, out Relation proof, out bool unresolved)
    {
        var from = Difference(point, edge.Start); var to = Difference(edge.End, point); proof = new(from, to); unresolved = false;
        if (!Graph.Frame.TryReadResult(from, out var a, out _, out var au) || !Graph.Frame.TryReadResult(to, out var b, out _, out var bu) || E(au).Length != 0 || E(bu).Length != 0)
        { unresolved = true; return false; }
        if (E(a).Length == 0) { var pure = Pure(b, out _, out var axis) && ReferenceEquals(axis, edge.Axis); unresolved = !pure; return pure; }
        if (E(b).Length == 0) { var pure = Pure(a, out _, out var axis) && ReferenceEquals(axis, edge.Axis); unresolved = !pure; return pure; }
        if (!Pure(a, out var first, out var aa) || !Pure(b, out var second, out var ba) || !ReferenceEquals(aa, edge.Axis) || !ReferenceEquals(ba, edge.Axis))
        { unresolved = true; return false; }
        return first == second;
    }

    private Relation Project(Relation word, Relation selectedAxis) => S(E(word).OfType<Relation>()
        .Where(move => Graph.Frame.Direction(move, out var axis, out _, out _) && ReferenceEquals(axis, selectedAxis)).Cast<IElement>().ToArray());
    private Relation Difference(Relation first, Relation second) => Graph.Frame.Reduce(first, UnitArrangementRead.Inverse(Graph.Frame, second));
    private bool Zero(Relation witness) => Graph.Frame.TryReadResult(witness, out var rest, out _, out var unresolved) && Closed(rest) && E(rest).Length == 0 && E(unresolved).Length == 0;
    private bool Known(Relation word) => Closed(word) && E(word).All(entry => entry is Relation move && Graph.Frame.Direction(move, out _, out _, out _));
    private bool Pure(Relation word, out bool forward, out Relation axis)
    {
        forward = false; axis = null!; var entries = E(word);
        if (!Closed(word) || entries.Length == 0 || entries[0] is not Relation first || !Graph.Frame.Direction(first, out axis, out forward, out _)) return false;
        var selectedAxis = axis; var selectedFace = forward;
        return entries.All(entry => entry is Relation move && Graph.Frame.Direction(move, out var actualAxis, out var actualFace, out _) && ReferenceEquals(selectedAxis, actualAxis) && selectedFace == actualFace);
    }
    private static Relation Join(Relation first, Relation second) => S([.. E(first), .. E(second)]);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IEnumerable<IElement> Participants(IElement source)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var queue = new Stack<IElement>(); queue.Push(source);
        while (queue.TryPop(out var element))
        { if (!seen.Add(element)) continue; yield return element; if (element is Relation relation) { queue.Push(relation.Left); queue.Push(relation.Right); } }
    }
}
