using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An evaluator-only, bounded graph correspondence at the published
/// incidence/heading resolution. It does not inspect positions, names, drawing
/// recipes, fills or a receiver's selected vocabulary. Explicit crossing and
/// continuation row order is preserved when those scopes are selected.</summary>
public sealed class OrnamentReconstructionGraphRead
{
    // scope = (Closed(left selected scopes) -> Closed(right selected scopes))
    //      -> (Closed(left carried scopes) -> Closed(right carried scopes))
    // capacity = one quantum -> Closed(selected powers of that quantum)
    // power(next) = power(previous) -> power(previous); repeated participation
    // is shared as a DAG, rather than allocating every unused search slot.
    // edge correspondence = actual left row ->
    //      (actual right row -> (mapped ordered from -> mapped ordered to))
    // A reverse traversal is admitted only with its opposite heading. It is
    // retained explicitly, not a mutation or erasure of either ordered row.
    public Relation Surface { get; }
    public Relation Scope { get; }
    public Relation Budget { get; }
    public Relation Consumed { get; }
    public Relation SiteCorrespondence { get; }
    public Relation EdgeCorrespondence { get; }
    public Relation RelationCorrespondence { get; }
    public Relation Supported { get; }
    public Relation Refuted { get; }
    public Relation Open { get; }
    public Relation Blocked { get; }
    public int Attempts { get; }
    public string Answer => N.E(Blocked).Length != 0 ? "Blocked" : N.E(Supported).Length != 0 ? "Yes" :
        N.E(Refuted).Length != 0 ? "No" : "Open";

    private sealed record Edge(Relation Row, int From, int To, int Heading);
    private sealed record Continuation(Relation Row, int Site, int First, int Second);
    private sealed record Crossing(Relation Row, int First, int Second);
    private sealed record Graph(IElement[] Sites, Edge[] Edges, Continuation[] Continuations, Crossing[] Crossings);

    public OrnamentReconstructionGraphRead(QualitativeRecognitionObservation left,
        QualitativeRecognitionObservation right, int capacity = 100000, bool includeRelations = true)
    {
        ArgumentNullException.ThrowIfNull(left); ArgumentNullException.ThrowIfNull(right);
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        Scope = new(new Relation(Selected(left), Selected(right)),
            new Relation(Carried(left), Carried(right)));
        Budget = Capacity(capacity);
        var directions = new Relation(left.Frame.Surface, right.Frame.Surface);
        var frameMap = N.S(new Relation(left.Frame.Right, right.Frame.Right), new Relation(left.Frame.Left, right.Frame.Left),
            new Relation(left.Frame.Up, right.Frame.Up), new Relation(left.Frame.Down, right.Frame.Down));
        var basis = new Relation(new Relation(left.Surface, right.Surface),
            new Relation(new Relation(directions, frameMap), new Relation(Scope, Budget)));
        var used = new List<IElement>(); var blocked = new List<IElement>(); var pending = new List<IElement>();
        var siteRows = new List<IElement>(); var edgeRows = new List<IElement>(); var relationRows = new List<IElement>();
        var supported = new List<IElement>(); var refuted = new List<IElement>();
        var a = Read(left, includeRelations, pending); var b = Read(right, includeRelations, pending);
        if (a is not null && b is not null)
        {
            if (a.Edges.Length > 48 || b.Edges.Length > 48)
                pending.Add(StructuralConstraint.Open(Scope));
            else if (a.Sites.Length != b.Sites.Length || a.Edges.Length != b.Edges.Length ||
                a.Continuations.Length != b.Continuations.Length || a.Crossings.Length != b.Crossings.Length)
                refuted.Add(new Relation(Scope, new Relation(Selected(left), Selected(right))));
            else
            {
                var (firstColors, secondColors) = Colors(a, b);
                if (!firstColors.Order().SequenceEqual(secondColors.Order()))
                    refuted.Add(new Relation(Scope, new Relation(Selected(left), Selected(right))));
                else
                {
                    var maps = Enumerable.Repeat(-1, a.Sites.Length).ToArray();
                    var edges = Enumerable.Repeat(-1, a.Edges.Length).ToArray();
                    var cut = false; var found = false;
                    var choices = Enumerable.Range(0, a.Sites.Length).Select(i => Enumerable.Range(0, b.Sites.Length)
                        .Where(j => firstColors[i] == secondColors[j]).ToArray()).ToArray();
                    var order = Enumerable.Range(0, a.Sites.Length).OrderBy(i => choices[i].Length)
                        .ThenByDescending(i => Degree(a, i)).ThenBy(i => i).ToArray();
                    MatchSites(0);
                    if (found)
                    {
                        siteRows.AddRange(Enumerable.Range(0, maps.Length).Select(i => (IElement)new Relation(a.Sites[i], b.Sites[maps[i]])));
                        edgeRows.AddRange(Enumerable.Range(0, edges.Length).Select(i => (IElement)new Relation(a.Edges[i].Row,
                            new Relation(b.Edges[edges[i]].Row, new Relation(b.Sites[maps[a.Edges[i].From]], b.Sites[maps[a.Edges[i].To]])))));
                        foreach (var row in a.Continuations)
                            relationRows.Add(new Relation(row.Row, b.Continuations.First(other => other.Site == maps[row.Site] &&
                                other.First == edges[row.First] && other.Second == edges[row.Second]).Row));
                        foreach (var row in a.Crossings)
                            relationRows.Add(new Relation(row.Row, b.Crossings.First(other => other.First == edges[row.First] &&
                                other.Second == edges[row.Second]).Row));
                        supported.Add(new Relation(Scope, new Relation(N.S(siteRows.ToArray()),
                            new Relation(N.S(edgeRows.ToArray()), N.S(relationRows.ToArray())))));
                    }
                    else if (cut) pending.Add(StructuralConstraint.Open(Scope, Budget, N.S(used.ToArray())));
                    else refuted.Add(new Relation(Scope, N.S(used.ToArray())));

                    bool Spend(IElement from, IElement to)
                    {
                        if (used.Count >= capacity) { cut = true; return false; }
                        used.Add(new Relation(new Placeholder(), new Relation(from, to))); return true;
                    }
                    void MatchSites(int depth)
                    {
                        if (depth == order.Length) { MatchEdges(0); return; }
                        var i = order[depth];
                        foreach (var j in choices[i])
                        {
                            if (maps.Contains(j)) continue;
                            if (!Spend(a.Sites[i], b.Sites[j])) return;
                            maps[i] = j;
                            if (Enumerable.Range(0, maps.Length).Where(k => maps[k] >= 0).All(k =>
                                Between(a, i, k).SequenceEqual(Between(b, j, maps[k])))) MatchSites(depth + 1);
                            if (found || cut) return;
                            maps[i] = -1;
                        }
                    }
                    void MatchEdges(int index)
                    {
                        if (index == edges.Length) { found = RelationsPass(); return; }
                        var edge = a.Edges[index];
                        for (var j = 0; j < b.Edges.Length; j++)
                        {
                            if (edges.Contains(j)) continue;
                            var other = b.Edges[j];
                            if (!(other.From == maps[edge.From] && other.To == maps[edge.To] && other.Heading == edge.Heading ||
                                  other.From == maps[edge.To] && other.To == maps[edge.From] && other.Heading == Opposite(edge.Heading))) continue;
                            if (!Spend(edge.Row, other.Row)) return;
                            edges[index] = j;
                            if (RelationsPass()) MatchEdges(index + 1);
                            if (found || cut) return;
                            edges[index] = -1;
                        }
                    }
                    bool RelationsPass()
                    {
                        foreach (var row in a.Continuations)
                            if (edges[row.First] >= 0 && edges[row.Second] >= 0 &&
                                !b.Continuations.Any(other => other.Site == maps[row.Site] && other.First == edges[row.First] &&
                                    other.Second == edges[row.Second])) return false;
                        foreach (var row in a.Crossings)
                            if (edges[row.First] >= 0 && edges[row.Second] >= 0 && !b.Crossings.Any(other =>
                                other.First == edges[row.First] && other.Second == edges[row.Second])) return false;
                        // Scopes have equal cardinality. Bijective segment/site maps
                        // plus distinct native relation rows establish both directions.
                        return true;
                    }
                }
            }
        }
        Attempts = used.Count; Consumed = N.S(used.ToArray()); SiteCorrespondence = N.S(siteRows.ToArray());
        EdgeCorrespondence = N.S(edgeRows.ToArray()); RelationCorrespondence = N.S(relationRows.ToArray());
        Supported = N.S(supported.ToArray()); Refuted = N.S(refuted.ToArray()); Open = N.S(pending.ToArray()); Blocked = N.S(blocked.ToArray());
        Surface = new(basis, new Relation(Supported, new Relation(Refuted, new Relation(Open,
            new Relation(Blocked, new Relation(Consumed, new Relation(SiteCorrespondence,
                new Relation(EdgeCorrespondence, RelationCorrespondence))))))));

        Relation Selected(QualitativeRecognitionObservation observation) => includeRelations
            ? N.S(observation.Sites, observation.Segments, observation.Continuations, observation.Crossings)
            : N.S(observation.Sites, observation.Segments);
        Relation Carried(QualitativeRecognitionObservation observation) => includeRelations ? N.S() :
            N.S(observation.Continuations, observation.Crossings);
    }

    private static Graph? Read(QualitativeRecognitionObservation observation, bool relations, List<IElement> pending)
    {
        if (!N.Read(observation.Sites, out var sites) || !N.Distinct(sites) || !N.Read(observation.Segments, out var rows))
        { pending.Add(StructuralConstraint.Open(observation.Sites, observation.Segments)); return null; }
        var edges = new List<Edge>();
        foreach (var row in rows)
        {
            if (row is not Relation segment || !observation.TryReadSegment(segment, out var identity, out var from, out var to, out var heading) ||
                edges.Any(prior => N.Same(prior.Row.Left, identity)) || !N.Read(heading, out var directions) || directions.Length == 0)
            { pending.Add(new Relation(observation.Surface, row)); continue; }
            var mask = directions.Sum(direction => ReferenceEquals(direction, observation.Frame.Right) ? 1 :
                ReferenceEquals(direction, observation.Frame.Left) ? 2 : ReferenceEquals(direction, observation.Frame.Up) ? 4 : 8);
            edges.Add(new(segment, Array.FindIndex(sites, site => N.Same(site, from)),
                Array.FindIndex(sites, site => N.Same(site, to)), mask));
        }
        if (pending.Count != 0) return null;
        var continuations = new List<Continuation>(); var crossings = new List<Crossing>();
        if (relations)
        {
            if (!N.Read(observation.Continuations, out var continuationRows) || !N.Read(observation.Crossings, out var crossingRows))
            { pending.Add(StructuralConstraint.Open(observation.Continuations, observation.Crossings)); return null; }
            foreach (var item in continuationRows)
                if (item is Relation row && observation.TryReadContinuation(row, out var site, out var first, out var second) &&
                    !continuations.Any(prior => N.Same(sites[prior.Site], site) &&
                        ReferenceEquals(edges[prior.First].Row, first) && ReferenceEquals(edges[prior.Second].Row, second)))
                    continuations.Add(new(row, Array.FindIndex(sites, actual => N.Same(actual, site)),
                        edges.FindIndex(edge => ReferenceEquals(edge.Row, first)), edges.FindIndex(edge => ReferenceEquals(edge.Row, second))));
                else pending.Add(new Relation(observation.Continuations, item));
            foreach (var item in crossingRows)
                if (item is Relation row && observation.TryReadCrossing(row, out var first, out var second) &&
                    !crossings.Any(prior => ReferenceEquals(prior.Row.Left, row.Left) && ReferenceEquals(prior.Row.Right, row.Right)))
                    crossings.Add(new(row, edges.FindIndex(edge => ReferenceEquals(edge.Row, first)), edges.FindIndex(edge => ReferenceEquals(edge.Row, second))));
                else pending.Add(new Relation(observation.Crossings, item));
        }
        return pending.Count != 0 ? null : new(sites, edges.ToArray(), continuations.ToArray(), crossings.ToArray());
    }

    private static (int[] Left, int[] Right) Colors(Graph a, Graph b)
    {
        var first = new int[a.Sites.Length]; var second = new int[b.Sites.Length];
        for (var round = 0; round <= a.Sites.Length; round++)
        {
            var signatures = new Dictionary<string, int>(StringComparer.Ordinal);
            var nextFirst = Build(a, first); var nextSecond = Build(b, second);
            if (nextFirst.SequenceEqual(first) && nextSecond.SequenceEqual(second)) return (first, second);
            first = nextFirst; second = nextSecond;

            int[] Build(Graph graph, int[] colors) => Enumerable.Range(0, graph.Sites.Length).Select(i =>
            {
                var neighborhood = graph.Edges.Where(edge => edge.From == i || edge.To == i)
                    .Select(edge => (Mask: edge.From == i ? edge.Heading : Opposite(edge.Heading),
                        Color: colors[edge.From == i ? edge.To : edge.From])).OrderBy(pair => pair.Mask).ThenBy(pair => pair.Color);
                var signature = colors[i] + ":" + string.Join(';', neighborhood.Select(pair => pair.Mask + ">" + pair.Color)) +
                    ":" + graph.Continuations.Count(row => row.Site == i);
                if (!signatures.TryGetValue(signature, out var color)) signatures.Add(signature, color = signatures.Count);
                return color;
            }).ToArray();
        }
        return (first, second);
    }
    private static int Degree(Graph graph, int site) => graph.Edges.Count(edge => edge.From == site || edge.To == site);
    private static int[] Between(Graph graph, int from, int to) => graph.Edges.Where(edge => edge.From == from && edge.To == to ||
        edge.To == from && edge.From == to).Select(edge => edge.From == from ? edge.Heading : Opposite(edge.Heading)).Order().ToArray();
    private static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
    private static Relation Capacity(int count)
    {
        IElement power = new Placeholder(); var quantum = power; var selected = new List<IElement>();
        while (count != 0)
        {
            if ((count & 1) != 0) selected.Add(power);
            count >>= 1; if (count != 0) power = new Relation(power, power);
        }
        return new(quantum, N.S(selected.ToArray()));
    }
}
