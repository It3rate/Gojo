using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A reusable, source-free role graph under a declared heading frame.
/// The surface contains no occurrence, construction recipe or sender identity.</summary>
public sealed class CommunicationPattern
{
    // surface = declared frame -> native motif definition
    public QualitativeMotionFrame Frame { get; }
    public Relation Definition { get; }
    public Relation Surface { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public bool IsValid { get; }
    public string Kind { get; }
    public string Signature { get; }

    public CommunicationPattern(QualitativeMotionFrame frame, Relation definition)
    {
        Frame = frame; Definition = definition; Surface = new(frame.Surface, definition);
        // Parsing a supplied graph does not request occurrence discovery.
        // Empty budgets avoid allocating unused search/retention slots, while
        // TryReadPattern still validates stars, walks and cycles of any legal
        // supplied length independently of the discovery grammar bounds.
        var reader = new QualitativeMotifInduction(new(frame, N.S(), N.S()),
            maxCycleEdges: 0, proposalCapacity: 0, maxWalkEdges: 0, retainedCapacity: 0);
        IsValid = reader.TryReadPattern(definition, out var kind, out var signature) &&
            CommunicationShape.TryRead(Surface, out _);
        Kind = IsValid ? kind : ""; Signature = IsValid ? signature : "";
        Blocked = IsValid ? N.S() : N.S(Surface);
        Unresolved = IsValid ? N.S() : StructuralConstraint.Open(Surface);
    }
}

/// <summary>Finite existential and exclusion scopes. Missing clauses make no
/// commitment; they do not mean false, zero or a probability distribution.</summary>
public sealed class CommunicationMessage
{
    // surface = Closed(required pattern surfaces) -> Closed(forbidden pattern surfaces)
    public Relation Required { get; }
    public Relation Forbidden { get; }
    public Relation Surface { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public CommunicationMessage(IReadOnlyList<CommunicationPattern> required,
        IReadOnlyList<CommunicationPattern>? forbidden = null)
        : this(N.S(required.Select(pattern => (IElement)pattern.Surface).ToArray()),
            N.S((forbidden ?? []).Select(pattern => (IElement)pattern.Surface).ToArray())) { }

    public CommunicationMessage(Relation required, Relation forbidden)
    {
        Required = required; Forbidden = forbidden; Surface = new(required, forbidden);
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        foreach (var scope in new[] { required, forbidden })
        {
            if (!N.Read(scope, out var entries)) pending.Add(scope);
            foreach (var entry in N.E(scope))
                if (entry is not Relation pattern || !CommunicationShape.TryRead(pattern, out _))
                    blocked.Add(new Relation(scope, entry));
        }
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
    }
}

/// <summary>A certified output occurrence. Textual family signatures are never
/// sufficient: every match retains native frame, site and edge bijections.</summary>
public sealed record CommunicationMatch(Relation Pattern, QualitativeMotifFamily Family,
    QualitativeMotifOccurrence Occurrence, Relation Witness);

/// <summary>Checks a message against only a history-blind induction result.
/// Existence needs an actual match. Absence additionally needs completed search
/// within the declared star/walk/cycle grammar and its visible length bounds.</summary>
public sealed class CommunicationAssessment
{
    // clause = actual required/forbidden scope -> actual pattern surface
    // supported/refuted = clause -> Closed(actual match witnesses), or
    //                     clause -> (actual output read -> actual search budget)
    // unresolved = clause -> Open(remaining search or unsupported length scope)
    // match = (pattern -> actual occurrence) ->
    //         (frame mapping -> (site role bijection -> edge role bijection))
    public QualitativeMotifInduction Output { get; }
    public CommunicationMessage Message { get; }
    public Relation Supported { get; }
    public Relation Refuted { get; }
    public Relation Unresolved { get; }
    public Relation Blocked { get; }
    public Relation Surface { get; }
    public IReadOnlyList<CommunicationMatch> Matches { get; }
    public int SupportedCount => N.E(Supported).Length;
    public int RefutedCount => N.E(Refuted).Length;
    public int UnresolvedCount => N.E(Unresolved).Length;
    public int BlockedCount => N.E(Blocked).Length;
    public bool IsSatisfied => Message.IsComplete && RefutedCount == 0 && UnresolvedCount == 0 && BlockedCount == 0;

    public CommunicationAssessment(QualitativeMotifInduction output, CommunicationMessage message)
    {
        Output = output; Message = message;
        var supported = new List<IElement>(); var refuted = new List<IElement>();
        var pending = new List<IElement>(N.E(message.Unresolved)); var blocked = new List<IElement>(N.E(message.Blocked));
        var matches = new List<CommunicationMatch>();
        Assess(message.Required, true); Assess(message.Forbidden, false);
        Supported = N.S(supported.ToArray()); Refuted = N.S(refuted.ToArray());
        Unresolved = N.S(pending.ToArray()); Blocked = N.S(blocked.ToArray()); Matches = matches;
        Surface = new(new Relation(output.Surface, message.Surface), new Relation(Supported,
            new Relation(Refuted, new Relation(Blocked, Unresolved))));

        void Assess(Relation scope, bool required)
        {
            foreach (var item in N.E(scope))
            {
                if (item is not Relation pattern || !CommunicationShape.TryRead(pattern, out var shape)) continue;
                var clause = new Relation(scope, pattern); var found = new List<IElement>();
                foreach (var family in output.Families)
                {
                    if (!output.TryReadFamily(family.Read, out _) ||
                        !CommunicationShape.TryRead(new(output.Observation.Frame.Surface, family.Definition), out var candidate) ||
                        !shape.TryMap(candidate, out var siteMap, out var edgeMap)) continue;
                    foreach (var occurrence in family.Occurrences)
                    {
                        if (!output.TryReadOccurrence(family.Read, occurrence.Read, out _) ||
                            !candidate.Certifies(output.Observation, occurrence)) continue;
                        var frameMap = N.S(shape.Directions.Select((direction, index) =>
                            (IElement)new Relation(direction, candidate.Directions[index])).ToArray());
                        var witness = new Relation(new Relation(pattern, occurrence.Read),
                            new Relation(new Relation(shape.Frame, candidate.Frame),
                                new Relation(frameMap, new Relation(siteMap, edgeMap))));
                        found.Add(witness); matches.Add(new(pattern, family, occurrence, witness));
                    }
                }
                if (found.Count != 0)
                    (required ? supported : refuted).Add(new Relation(clause, N.S(found.ToArray())));
                else if (output.IsComplete && WithinGrammar(shape))
                    (required ? refuted : supported).Add(new Relation(clause, new Relation(output.Surface, output.Budget)));
                else pending.Add(new Relation(clause, StructuralConstraint.Open(output.Unsearched, output.Unresolved, output.Budget)));
            }
        }
        bool WithinGrammar(CommunicationShape shape)
        {
            if (shape.Centres.Length == 1) return true;
            if (output.Budget.Right is not Relation { Right: Relation lengths } ||
                lengths.Left is not Relation cycleScope || lengths.Right is not Relation walkScope ||
                !N.Read(cycleScope, out var cycleSlots) || !N.Read(walkScope, out var walkSlots)) return false;
            // The induction grammar closes cycles only after at least three
            // edge uses. A native two-edge multigraph cycle is readable but
            // outside this search, even when the configured upper bound fits.
            return shape.IsCycle ? shape.Edges.Length >= 3 && shape.Edges.Length <= cycleSlots.Length :
                shape.Edges.Length <= walkSlots.Length;
        }
    }
}

/// <summary>Exact native decoder and bounded ordered-role correspondence. This
/// is a read of the graph, not a second noun or operation ontology.</summary>
internal sealed class CommunicationShape
{
    public Relation Frame { get; private init; } = null!;
    public Relation Definition { get; private init; } = null!;
    public IElement[] Directions { get; private init; } = [];
    public IElement[] Sites { get; private init; } = [];
    public Relation[] Edges { get; private init; } = [];
    public Relation[] Ends { get; private init; } = [];
    public int[] Headings { get; private init; } = [];
    public IElement[] Centres { get; private init; } = [];
    public bool IsCycle { get; private init; }

    public static bool TryRead(Relation surface, out CommunicationShape shape)
    {
        shape = null!;
        if (surface.Left is not Relation frame || surface.Right is not Relation definition ||
            frame.Left is not Relation { Right: Relation { Left: Relation horizontal, Right: Relation vertical } } ||
            frame.Right is not Relation { Left: Relation opposites } ||
            ReferenceEquals(horizontal, vertical) || N.Same(horizontal.Left, horizontal.Right) ||
            N.Same(vertical.Left, vertical.Right) || new[] { horizontal.Left, horizontal.Right }.Any(a =>
                new[] { vertical.Left, vertical.Right }.Any(b => N.Same(a, b))) ||
            !N.Read(opposites, out var pairs) || pairs.Length != 4 ||
            !FindInverse(horizontal, out var left) || !FindInverse(vertical, out var down) ||
            !FindInverse(left, out var right) || !ReferenceEquals(right, horizontal) ||
            !FindInverse(down, out var up) || !ReferenceEquals(up, vertical) ||
            definition is not { Left: Relation { Left: var definitionFrame, Right: Relation { Left: Relation siteScope,
                Right: Relation edgeScope } }, Right: Relation { Left: Relation centreScope, Right: Relation pathsScope } } ||
            !ReferenceEquals(definitionFrame, frame) || !N.Read(siteScope, out var sites) ||
            sites.Length == 0 || sites.Any(site => site is not Placeholder) || !N.Distinct(sites) ||
            !N.Read(edgeScope, out var edgeRows) || edgeRows.Length == 0 ||
            !N.Read(centreScope, out var centres) || !N.Read(pathsScope, out var paths)) return false;
        var directions = new IElement[] { horizontal, left, vertical, down };
        var edges = new List<Relation>(); var ends = new List<Relation>(); var headings = new List<int>();
        foreach (var item in edgeRows)
        {
            if (item is not Relation { Left: Placeholder, Right: Relation { Left: Relation pair, Right: Relation heading } } edge ||
                !sites.Any(site => N.Same(site, pair.Left)) || !sites.Any(site => N.Same(site, pair.Right)) ||
                N.Same(pair.Left, pair.Right) || sites.Any(site => N.Same(site, edge.Left)) ||
                edges.Any(prior => N.Same(prior.Left, edge.Left)) ||
                !TryHeading(heading, directions, out var mask)) return false;
            edges.Add(edge); ends.Add(pair); headings.Add(mask);
        }
        if (sites.Any(site => !ends.Any(pair => N.Same(site, pair.Left) || N.Same(site, pair.Right)))) return false;
        var star = centres is [var centre] && sites.Any(site => N.Same(site, centre)) && paths.Length == 0 &&
            ends.All(pair => N.Same(pair.Left, centre));
        var cycle = false;
        if (!star)
        {
            if (centres.Length != 0 || paths is not [Relation path] || !N.Read(path, out var uses) ||
                uses.Length != edges.Count || uses.Length < 2) return false;
            for (var i = 0; i < uses.Length; i++)
                if (uses[i] is not Relation use || !N.Same(use.Left, edges[i].Left) ||
                    !ReferenceEquals(use.Right, ends[i]) || i > 0 && !N.Same(ends[i - 1].Right, ends[i].Left)) return false;
            cycle = N.Same(ends[^1].Right, ends[0].Left);
            var visited = ends.Select(pair => pair.Left).Append(ends[^1].Right).ToArray();
            if (!N.Distinct(cycle ? visited[..^1] : visited) || sites.Length != edges.Count + (cycle ? 0 : 1)) return false;
        }
        shape = new() { Frame = frame, Definition = definition, Directions = directions, Sites = sites,
            Edges = edges.ToArray(), Ends = ends.ToArray(), Headings = headings.ToArray(), Centres = centres, IsCycle = cycle };
        return true;

        bool FindInverse(Relation forward, out Relation inverse)
        {
            inverse = null!;
            var found = pairs.OfType<Relation>().Where(pair => ReferenceEquals(pair.Left, forward)).ToArray();
            if (found is not [Relation { Right: Relation target }] ||
                !ReferenceEquals(target.Left, forward.Right) || !ReferenceEquals(target.Right, forward.Left)) return false;
            inverse = target; return true;
        }
    }

    public bool TryMap(CommunicationShape target, out Relation sites, out Relation edges)
    {
        sites = edges = null!;
        if (Sites.Length != target.Sites.Length || Edges.Length != target.Edges.Length ||
            Centres.Length != target.Centres.Length || IsCycle != target.IsCycle) return false;
        var siteIndices = Enumerable.Repeat(-1, Sites.Length).ToArray();
        var edgeIndices = Enumerable.Repeat(-1, Edges.Length).ToArray();
        if (Centres.Length == 1)
        {
            var sourceCentre = Index(Sites, Centres[0]); var targetCentre = Index(target.Sites, target.Centres[0]);
            siteIndices[sourceCentre] = targetCentre;
            // A star's leaves are classified by their actual parallel-edge
            // heading participation. Equal profiles can be paired directly:
            // there is no factorial search over same-heading permutations.
            for (var i = 0; i < Sites.Length; i++)
            {
                if (i == sourceCentre) continue;
                var chosen = -1;
                for (var j = 0; j < target.Sites.Length; j++)
                    if (j != targetCentre && !siteIndices.Contains(j) && SameLeaf(i, j)) { chosen = j; break; }
                if (chosen < 0) return false;
                siteIndices[i] = chosen;
            }
            for (var i = 0; i < Edges.Length; i++)
            {
                var leaf = siteIndices[Index(Sites, Ends[i].Right)];
                // Parallel edges can have structurally equal endpoint pairs.
                // Preserve their distinct edge occurrences by selecting the
                // actual array index, never Relation.Equals/Array.IndexOf.
                for (var j = 0; j < target.Ends.Length; j++)
                    if (Index(target.Sites, target.Ends[j].Right) == leaf && target.Headings[j] == Headings[i] &&
                        !edgeIndices.Contains(j)) { edgeIndices[i] = j; break; }
                if (edgeIndices[i] < 0) return false;
            }
        }
        else
        {
            var found = false; var count = Edges.Length;
            for (var reverse = 0; reverse < 2 && !found; reverse++)
            for (var start = 0; start < (IsCycle ? count : 1) && !found; start++)
            {
                Array.Fill(siteIndices, -1); Array.Fill(edgeIndices, -1); var valid = true;
                for (var i = 0; i < count && valid; i++)
                {
                    var j = IsCycle ? (start + (reverse == 0 ? i : -i - 1) + 2 * count) % count :
                        reverse == 0 ? i : count - i - 1;
                    var heading = reverse == 0 ? target.Headings[j] : Opposite(target.Headings[j]);
                    var from = reverse == 0 ? target.Ends[j].Left : target.Ends[j].Right;
                    var to = reverse == 0 ? target.Ends[j].Right : target.Ends[j].Left;
                    valid = Headings[i] == heading && Bind(Ends[i].Left, from) && Bind(Ends[i].Right, to);
                    edgeIndices[i] = j;
                }
                found = valid && siteIndices.All(index => index >= 0);
            }
            if (!found) return false;
        }
        sites = N.S(Sites.Select((site, i) => (IElement)new Relation(site, target.Sites[siteIndices[i]])).ToArray());
        edges = N.S(Edges.Select((edge, i) => (IElement)new Relation(edge.Left, target.Edges[edgeIndices[i]].Left)).ToArray()); return true;

        bool Bind(IElement sourceSite, IElement targetSite)
        {
            var a = Index(Sites, sourceSite); var b = Index(target.Sites, targetSite);
            if (siteIndices[a] >= 0) return siteIndices[a] == b;
            if (siteIndices.Contains(b)) return false;
            siteIndices[a] = b; return true;
        }
        bool SameLeaf(int a, int b)
        {
            var first = Enumerable.Range(0, Edges.Length).Where(i => N.Same(Ends[i].Right, Sites[a])).Select(i => Headings[i]).Order().ToArray();
            var second = Enumerable.Range(0, target.Edges.Length).Where(i => N.Same(target.Ends[i].Right, target.Sites[b])).Select(i => target.Headings[i]).Order().ToArray();
            return first.SequenceEqual(second);
        }
    }

    public bool Certifies(QualitativeRecognitionObservation observation, QualitativeMotifOccurrence occurrence)
    {
        if (!ReferenceEquals(Frame, observation.Frame.Surface) ||
            occurrence.Read is not { Left: Relation selected, Right: Relation { Left: Relation sb, Right: Relation { Left: Relation eb, Right: Relation route } } } ||
            !ReferenceEquals(selected.Left, Definition) || !ReferenceEquals(selected.Right, observation.Surface) ||
            !ReferenceEquals(sb, occurrence.SiteBindings) || !ReferenceEquals(eb, occurrence.SegmentBindings) || !ReferenceEquals(route, occurrence.Route) ||
            !N.Read(sb, out var siteRows) || siteRows.Length != Sites.Length ||
            !N.Read(eb, out var edgeRows) || edgeRows.Length != Edges.Length ||
            !N.Read(route, out var uses) || uses.Length != Edges.Length) return false;
        var actualSites = new List<IElement>(); var actualEdges = new List<Relation>();
        foreach (var site in Sites)
        {
            var bindings = siteRows.OfType<Relation>().Where(row => N.Same(row.Left, site)).ToArray();
            if (bindings is not [var binding] || !N.E(observation.Sites).Any(actual => N.Same(actual, binding.Right))) return false;
            actualSites.Add(binding.Right);
        }
        if (!N.Distinct(actualSites.ToArray())) return false;
        for (var i = 0; i < Edges.Length; i++)
        {
            var bindings = edgeRows.OfType<Relation>().Where(row => N.Same(row.Left, Edges[i].Left)).ToArray();
            if (bindings is not [Relation { Right: Relation segment }] ||
                !observation.TryReadSegment(segment, out _, out var from, out var to, out var heading) ||
                !TryHeading(heading, Directions, out var mask) || uses[i] is not Relation { Left: Relation actual, Right: Relation ends } ||
                !ReferenceEquals(actual, segment) ||
                !N.Same(ends.Left, actualSites[Index(Sites, Ends[i].Left)]) ||
                !N.Same(ends.Right, actualSites[Index(Sites, Ends[i].Right)])) return false;
            var forward = N.Same(from, ends.Left) && N.Same(to, ends.Right);
            var reverse = N.Same(from, ends.Right) && N.Same(to, ends.Left);
            if (!forward && !reverse || (forward ? mask : Opposite(mask)) != Headings[i]) return false;
            actualEdges.Add(segment);
        }
        if (!N.Distinct(actualEdges.Cast<IElement>().ToArray())) return false;
        if (Centres is [var centre])
        {
            if (!N.Read(observation.Segments, out var observedEdges)) return false;
            var actualCentre = actualSites[Index(Sites, centre)]; var incident = new List<Relation>();
            foreach (var item in observedEdges)
            {
                if (item is not Relation segment || !observation.TryReadSegment(segment, out _, out var from, out var to, out _)) return false;
                if (N.Same(from, actualCentre) || N.Same(to, actualCentre)) incident.Add(segment);
            }
            if (incident.Count != actualEdges.Count || incident.Any(edge => !actualEdges.Any(actual => ReferenceEquals(actual, edge)))) return false;
        }
        return true;
    }

    private static int Index(IElement[] items, IElement selected) => Array.FindIndex(items, item => N.Same(item, selected));
    private static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
    private static bool TryHeading(Relation heading, IElement[] directions, out int mask)
    {
        mask = 0;
        if (!N.Read(heading, out var entries) || entries.Length is < 1 or > 2) return false;
        foreach (var entry in entries)
        {
            var index = Array.FindIndex(directions, direction => ReferenceEquals(direction, entry));
            if (index < 0 || (mask & (1 << index)) != 0) return false;
            mask |= 1 << index;
        }
        return (mask & 3) != 3 && (mask & 12) != 12;
    }
}
