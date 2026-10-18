using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded, non-draining induction over a published segment observation.
/// No faces, positions, construction recipes or source names enter this read.
/// Junction stars, short simple walks and simple cycles are role-renamed local
/// graphs. A cycle is not a claim that its interior is empty or is a face.</summary>
public sealed class QualitativeMotifInduction
{
    // pattern = (frame -> (Closed(site roles) -> Closed(edge roles ->
    //             ((from role -> to role) -> Closed(direction participation)))))
    //           -> (Closed(distinguished centre roles) -> Closed(ordered paths))
    // path = Closed(edge role -> (from role -> to role)); its own topology
    // distinguishes a closed cycle from an open walk. No noun tag is stored.
    // occurrence = (pattern -> observation) ->
    //              (site bindings -> (edge bindings -> actual ordered uses))
    // family = pattern -> Closed(occurrences)
    // budget = Closed(proposal slots) -> (Closed(retention slots) ->
    //          (Closed(cycle length slots) -> Closed(walk length slots)))
    // search cut = actual current site -> Closed(actual unexpanded edges)
    public QualitativeRecognitionObservation Observation { get; }
    public Relation Budget { get; }
    public Relation Consumed { get; }
    public Relation Available { get; }
    public Relation Candidates { get; }
    public Relation Covered { get; }
    public Relation Remainder { get; }
    public Relation Unsearched { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public IReadOnlyList<QualitativeMotifFamily> Families { get; }
    public bool IsComplete => N.E(Unsearched).Length == 0 && N.E(Unresolved).Length == 0;

    private sealed class Edge(int index, Relation row, int from, int to, int mask)
    {
        public int Index { get; } = index;
        public Relation Row { get; } = row;
        public int From { get; } = from;
        public int To { get; } = to;
        public int Mask { get; } = mask;
        public int Other(int site) => site == From ? To : From;
        public int Heading(int site) => site == From ? Mask : Opposite(Mask);
    }
    private sealed class Proposal(string kind, int[] sites, Edge[] edges, int[] masks)
    {
        public string Kind { get; } = kind;
        public int[] Sites { get; } = sites;
        public Edge[] Edges { get; } = edges;
        public int[] Masks { get; } = masks;
    }

    public QualitativeMotifInduction(QualitativeRecognitionObservation observation,
        int maxCycleEdges = 8, int proposalCapacity = 20000, int maxWalkEdges = 3,
        int retainedCapacity = 3000)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (maxCycleEdges is < 0 or > 12 || maxWalkEdges is < 0 or > 8 ||
            proposalCapacity < 0 || retainedCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(maxCycleEdges));
        Observation = observation;
        var proposalSlots = Slots(proposalCapacity); var retainedSlots = Slots(retainedCapacity);
        Budget = new(N.S(proposalSlots), new Relation(N.S(retainedSlots),
            new Relation(N.S(Slots(maxCycleEdges)), N.S(Slots(maxWalkEdges)))));
        var used = new List<IElement>(); var cuts = new List<IElement>();
        var unresolved = new List<IElement>(N.E(observation.Unresolved));
        var sourceSites = N.E(observation.Sites);
        // Repeated scope entries are not repeated independent observations.
        var sites = sourceSites.Distinct(N.Participants.Instance).ToArray();
        var adjacency = Enumerable.Range(0, sites.Length).Select(_ => new List<Edge>()).ToArray();
        var edges = new List<Edge>();
        var groups = new Dictionary<string, List<Proposal>>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var proposalCursor = 0; var retainedCursor = 0;
        var completeIncidence = N.Read(observation.Sites, out _) && N.Distinct(sourceSites) && N.Read(observation.Segments, out _);
        var uncertainStars = new HashSet<int>();
        var duplicateIdentities = N.E(observation.Segments).OfType<Relation>()
            .GroupBy(row => row.Left, N.Participants.Instance).Where(group => group.Count() > 1)
            .SelectMany(group => group).ToHashSet(ReferenceEqualityComparer.Instance);
        if (!completeIncidence)
            unresolved.Add(observation.Surface);
        foreach (var item in N.E(observation.Segments))
        {
            if (item is not Relation row || duplicateIdentities.Contains(row) || !observation.TryReadSegment(row, out _, out var from, out var to, out var heading))
            {
                unresolved.Add(item);
                if (item is Relation { Right: Relation { Left: Relation incompleteEnds } })
                    for (var index = 0; index < sites.Length; index++)
                        if (N.Same(sites[index], incompleteEnds.Left) || N.Same(sites[index], incompleteEnds.Right)) uncertainStars.Add(index);
                else completeIncidence = false;
                continue;
            }
            var a = Array.FindIndex(sites, value => N.Same(value, from));
            var b = Array.FindIndex(sites, value => N.Same(value, to));
            if (a < 0 || b < 0 || a == b || !TryMask(heading, observation, out var mask))
            { unresolved.Add(row); if (a >= 0) uncertainStars.Add(a); if (b >= 0) uncertainStars.Add(b); continue; }
            var edge = new Edge(edges.Count, row, a, b, mask); edges.Add(edge);
            adjacency[a].Add(edge); adjacency[b].Add(edge);
        }

        // Closed incidence stars include the boundary's lower-degree stars.
        for (var site = 0; site < sites.Length; site++)
        {
            if (!completeIncidence || uncertainStars.Contains(site))
            { unresolved.Add(new Relation(sites[site], observation.Segments)); continue; }
            if (!Spend(sites[site], N.S(adjacency[site].Select(edge => (IElement)edge.Row).ToArray())))
            { for (var rest = site; rest < sites.Length; rest++) Cut(rest, adjacency[rest]); break; }
            var star = adjacency[site].OrderBy(edge => edge.Heading(site)).ThenBy(edge => edge.Index).ToArray();
            if (star.Length == 0) continue;
            var starSites = new[] { site }.Concat(star.Select(edge => edge.Other(site))).Distinct().ToArray();
            Keep(new("junction", starSites, star,
                star.Select(edge => edge.Heading(site)).ToArray()), "J:" + string.Join(',', star.Select(edge =>
                    edge.Heading(site) + ">" + Array.IndexOf(starSites, edge.Other(site)))),
                "J:" + site);
        }

        Search(false, maxWalkEdges);
        Search(true, maxCycleEdges);
        Families = groups.Select(group => BuildFamily(group.Key, group.Value)).ToArray();
        Candidates = N.S(Families.Select(family => (IElement)family.Read).ToArray());
        var covered = Families.Where(family => family.Occurrences.Count > 1)
            .SelectMany(family => family.Occurrences).SelectMany(occurrence => N.E(occurrence.Segments))
            .Distinct(N.Participants.Instance).ToArray();
        Covered = N.S(covered);
        Remainder = N.S(N.E(observation.Segments).Where(edge => !covered.Any(actual => N.Same(actual, edge))).ToArray());
        Consumed = N.S(used.ToArray());
        Available = new(N.S(proposalSlots.Skip(proposalCursor).ToArray()), N.S(retainedSlots.Skip(retainedCursor).ToArray()));
        Unsearched = N.S(cuts.ToArray()); Unresolved = N.S(unresolved.Distinct(N.Participants.Instance).ToArray());
        Surface = new(new Relation(observation.Surface, Budget), new Relation(Candidates,
            new Relation(Covered, new Relation(Remainder, new Relation(Consumed,
                new Relation(Available, new Relation(Unsearched, Unresolved)))))));

        bool Spend(IElement from, IElement next)
        {
            if (proposalCursor >= proposalSlots.Length) return false;
            used.Add(new Relation(proposalSlots[proposalCursor++], new Relation(from, next))); return true;
        }
        void Cut(int site, IEnumerable<Edge> unfinished) => cuts.Add(new Relation(sites[site],
            N.S(unfinished.Select(edge => (IElement)edge.Row).ToArray())));
        void Keep(Proposal proposal, string signature, string identity)
        {
            if (!seen.Add(identity)) return;
            if (retainedCursor >= retainedSlots.Length)
            {
                cuts.Add(new Relation(N.S(proposal.Edges.Select(edge => (IElement)edge.Row).ToArray()),
                    N.S(proposal.Sites.Select(index => sites[index]).ToArray()))); return;
            }
            used.Add(new Relation(retainedSlots[retainedCursor++], N.S(proposal.Edges.Select(edge => (IElement)edge.Row).ToArray())));
            if (!groups.TryGetValue(signature, out var group)) groups.Add(signature, group = []);
            group.Add(proposal);
        }
        void Search(bool cycles, int maximum)
        {
            if (maximum < 2) return;
            var visited = new bool[sites.Length]; var pathSites = new List<int>(); var pathEdges = new List<Edge>();
            for (var start = 0; start < sites.Length; start++)
            {
                if (proposalCursor >= proposalSlots.Length)
                { for (var rest = start; rest < sites.Length; rest++) Cut(rest, adjacency[rest]); break; }
                pathSites.Add(start); visited[start] = true;
                Walk(start, start); visited[start] = false; pathSites.Clear();
            }
            void Walk(int start, int current)
            {
                for (var k = 0; k < adjacency[current].Count; k++)
                {
                    var edge = adjacency[current][k];
                    if (pathEdges.Contains(edge)) continue;
                    var next = edge.Other(current);
                    if (cycles && next < start) continue;
                    if (visited[next] && (!cycles || next != start || pathEdges.Count < 2)) continue;
                    if (!Spend(sites[current], edge.Row))
                    { Cut(current, adjacency[current].Skip(k)); return; }
                    pathEdges.Add(edge); pathSites.Add(next);
                    if (cycles && next == start)
                    {
                        var canonical = Canonical(true, pathSites.ToArray(), pathEdges.ToArray());
                        Keep(canonical, "C:" + string.Join(',', canonical.Masks),
                            "C:" + string.Join(',', pathEdges.Select(entry => entry.Index).Order()));
                    }
                    else
                    {
                        if (!cycles && pathEdges.Count >= 2)
                        {
                            var canonical = Canonical(false, pathSites.ToArray(), pathEdges.ToArray());
                            var ids = pathEdges.Select(entry => entry.Index).ToArray();
                            var forward = string.Join(',', ids); var reverse = string.Join(',', ids.Reverse());
                            Keep(canonical, "W:" + string.Join(',', canonical.Masks),
                                "W:" + (string.CompareOrdinal(forward, reverse) <= 0 ? forward : reverse));
                        }
                        if (pathEdges.Count < maximum)
                        { visited[next] = true; Walk(start, next); visited[next] = false; }
                    }
                    pathSites.RemoveAt(pathSites.Count - 1); pathEdges.RemoveAt(pathEdges.Count - 1);
                    if (proposalCursor >= proposalSlots.Length)
                    { if (k + 1 < adjacency[current].Count) Cut(current, adjacency[current].Skip(k + 1)); return; }
                }
            }
        }
        QualitativeMotifFamily BuildFamily(string signature, List<Proposal> entries)
        {
            var first = entries[0]; var star = first.Kind == "junction";
            var roleCount = star ? first.Sites.Length : first.Sites.Length - (first.Kind == "cycle" ? 1 : 0);
            var siteRoles = Slots(roleCount); var edgeRoles = Slots(first.Edges.Length);
            var roleEdges = first.Edges.Select((edge, index) => (IElement)new Relation(edgeRoles[index],
                new Relation(new Relation(siteRoles[star ? 0 : index], siteRoles[star ? Array.IndexOf(first.Sites, edge.Other(first.Sites[0])) : (index + 1) % roleCount]),
                    Heading(first.Masks[index], observation)))).ToArray();
            var roleRoute = star ? N.S() : N.S(roleEdges.Select(entry =>
            {
                var row = (Relation)entry; var data = (Relation)row.Right;
                return (IElement)new Relation(row.Left, data.Left);
            }).ToArray());
            var definition = new Relation(new Relation(observation.Frame.Surface,
                new Relation(N.S(siteRoles), N.S(roleEdges))),
                new Relation(star ? N.S(siteRoles[0]) : N.S(), star ? N.S() : N.S(roleRoute)));
            var occurrences = entries.Select(entry =>
            {
                var siteBindings = N.S(siteRoles.Select((role, index) => (IElement)new Relation(role, sites[entry.Sites[index]])).ToArray());
                var segmentBindings = N.S(edgeRoles.Select((role, index) => (IElement)new Relation(role, entry.Edges[index].Row)).ToArray());
                var route = N.S(entry.Edges.Select((edge, index) => (IElement)new Relation(edge.Row,
                    new Relation(sites[entry.Sites[star ? 0 : index]],
                        sites[star ? edge.Other(entry.Sites[0]) : entry.Sites[index + 1]]))).ToArray());
                var read = new Relation(new Relation(definition, observation.Surface),
                    new Relation(siteBindings, new Relation(segmentBindings, route)));
                return new QualitativeMotifOccurrence(read, siteBindings, segmentBindings, route);
            }).ToArray();
            var familyRead = new Relation(definition, N.S(occurrences.Select(entry => (IElement)entry.Read).ToArray()));
            return new(familyRead, definition, signature, first.Kind, occurrences);
        }
    }

    public Relation Evaluate() => Surface;

    /// <summary>Decodes the connected reusable graph without consulting family
    /// DTOs, occurrence history, text names or the proposal dictionary.</summary>
    public bool TryReadPattern(Relation definition, out string kind, out string signature)
    {
        kind = signature = "";
        if (definition is not { Left: Relation { Left: var frame, Right: Relation { Left: Relation siteScope,
                Right: Relation edgeScope } }, Right: Relation { Left: Relation centreScope, Right: Relation pathsScope } } ||
            !ReferenceEquals(frame, Observation.Frame.Surface) || !N.Read(siteScope, out var siteRoles) ||
            !N.Distinct(siteRoles) || !N.Read(edgeScope, out var roleRows) || !N.Read(centreScope, out var centres) ||
            !N.Read(pathsScope, out var paths)) return false;
        var edgeRoles = new List<IElement>(); var masks = new List<int>(); var ends = new List<Relation>();
        foreach (var item in roleRows)
        {
            if (item is not Relation { Right: Relation { Left: Relation edgeEnds, Right: Relation heading } } row ||
                !siteRoles.Any(role => N.Same(role, edgeEnds.Left)) || !siteRoles.Any(role => N.Same(role, edgeEnds.Right)) ||
                N.Same(edgeEnds.Left, edgeEnds.Right) || !TryMask(heading, Observation, out var mask) ||
                edgeRoles.Any(role => N.Same(role, row.Left))) return false;
            edgeRoles.Add(row.Left); masks.Add(mask); ends.Add(edgeEnds);
        }
        if (centres is [var centre] && paths.Length == 0 && roleRows.Length != 0 &&
            ends.All(pair => N.Same(pair.Left, centre)) && siteRoles.Any(role => N.Same(role, centre)))
        {
            kind = "junction";
            signature = "J:" + string.Join(',', masks.Select((mask, index) => mask + ">" +
                Array.FindIndex(siteRoles, role => N.Same(role, ends[index].Right)))); return true;
        }
        if (centres.Length != 0 || paths is not [Relation path] || !N.Read(path, out var uses) ||
            uses.Length != roleRows.Length || uses.Length < 2) return false;
        for (var i = 0; i < uses.Length; i++)
            if (uses[i] is not Relation use || !N.Same(use.Left, edgeRoles[i]) || !ReferenceEquals(use.Right, ends[i]) ||
                i != 0 && !N.Same(ends[i - 1].Right, ends[i].Left)) return false;
        var cycle = N.Same(ends[^1].Right, ends[0].Left);
        if (cycle && siteRoles.Length != uses.Length || !cycle && siteRoles.Length != uses.Length + 1) return false;
        var visited = ends.Select(pair => pair.Left).Append(ends[^1].Right).ToArray();
        if (!N.Distinct(cycle ? visited[..^1] : visited)) return false;
        kind = cycle ? "cycle" : "walk";
        signature = (cycle ? "C:" : "W:") + string.Join(',', masks); return true;
    }
    public bool TryReadFamily(Relation actual, out QualitativeMotifFamily family)
    {
        family = Families.FirstOrDefault(candidate => ReferenceEquals(candidate.Read, actual))!;
        return family is not null && TryReadPattern(family.Definition, out var kind, out var signature) &&
            kind == family.Kind && signature == family.Signature;
    }
    public bool TryReadOccurrence(Relation actualFamily, Relation actual, out QualitativeMotifOccurrence occurrence)
    {
        occurrence = null!;
        if (!TryReadFamily(actualFamily, out var family)) return false;
        occurrence = family.Occurrences.FirstOrDefault(candidate => ReferenceEquals(candidate.Read, actual))!;
        return occurrence is not null;
    }
    private static IElement[] Slots(int count) => Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
    private static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
    private static bool TryMask(Relation heading, QualitativeRecognitionObservation observation, out int mask)
    {
        mask = 0; if (!N.Read(heading, out var directions) || directions.Length == 0) return false;
        foreach (var direction in directions)
        {
            var bit = N.Same(direction, observation.Frame.Right) ? 1 : N.Same(direction, observation.Frame.Left) ? 2 :
                N.Same(direction, observation.Frame.Up) ? 4 : N.Same(direction, observation.Frame.Down) ? 8 : 0;
            if (bit == 0 || (mask & bit) != 0) return false; mask |= bit;
        }
        return (mask & 3) != 3 && (mask & 12) != 12;
    }
    private static Relation Heading(int mask, QualitativeRecognitionObservation observation) => N.S(
        new[] { observation.Frame.Right, observation.Frame.Left, observation.Frame.Up, observation.Frame.Down }
            .Where((_, index) => (mask & (1 << index)) != 0).Cast<IElement>().ToArray());
    private static Proposal Canonical(bool cycle, int[] sites, Edge[] edges)
    {
        Proposal? best = null; string? bestWord = null;
        var n = edges.Length;
        for (var reverse = 0; reverse < 2; reverse++)
            for (var start = 0; start < (cycle ? n : 1); start++)
            {
                var orderedSites = new int[n + 1]; var orderedEdges = new Edge[n]; var masks = new int[n];
                for (var i = 0; i <= n; i++)
                {
                    var siteIndex = cycle ? (start + (reverse == 0 ? i : -i) + n * 2) % n : reverse == 0 ? i : n - i;
                    orderedSites[i] = sites[siteIndex];
                    if (i == n) continue;
                    var edgeIndex = cycle ? (start + (reverse == 0 ? i : -i - 1) + n * 2) % n : reverse == 0 ? i : n - i - 1;
                    orderedEdges[i] = edges[edgeIndex]; masks[i] = orderedEdges[i].Heading(orderedSites[i]);
                }
                var word = string.Join(',', masks.Select(mask => mask.ToString("D2")));
                if (bestWord is null || string.CompareOrdinal(word, bestWord) < 0)
                { bestWord = word; best = new(cycle ? "cycle" : "walk", orderedSites, orderedEdges, masks); }
            }
        return best!;
    }
}

/// <summary>Decoded native pattern and matches. Text is a diagnostic projection;
/// the role graph and per-occurrence bindings retain the reusable definition.</summary>
public sealed class QualitativeMotifFamily(Relation read, Relation definition, string signature,
    string kind, IReadOnlyList<QualitativeMotifOccurrence> occurrences)
{
    public Relation Read { get; } = read;
    public Relation Definition { get; } = definition;
    public string Signature { get; } = signature;
    public string Kind { get; } = kind;
    public IReadOnlyList<QualitativeMotifOccurrence> Occurrences { get; } = occurrences;
    public int SegmentCount => Occurrences.Count == 0 ? 0 : N.E(Occurrences[0].SegmentBindings).Length;
    public int SiteCount => Occurrences.Count == 0 ? 0 : N.E(Occurrences[0].SiteBindings).Length;
}

public sealed class QualitativeMotifOccurrence(Relation read, Relation siteBindings, Relation segmentBindings, Relation route)
{
    public Relation Read { get; } = read;
    public Relation SiteBindings { get; } = siteBindings;
    public Relation SegmentBindings { get; } = segmentBindings;
    public Relation Route { get; } = route;
    public Relation Sites => N.S(N.E(SiteBindings).Cast<Relation>().Select(binding => binding.Right).Distinct(N.Participants.Instance).ToArray());
    public Relation Segments => N.S(N.E(SegmentBindings).Cast<Relation>().Select(binding => binding.Right).Distinct(N.Participants.Instance).ToArray());
}
