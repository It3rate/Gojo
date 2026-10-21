using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Learning;

/// <summary>An experimental, frequency-trained episodic grammar. Its vocabulary
/// consists of native boundary routes and recurrent open subroutes, not names
/// of shapes. Numeric ranking is a selection read above those structures; it
/// neither supplies geometry nor turns a plausible completion into evidence.</summary>
public static class OrnamentLearningMemory
{
    // pattern = (actual motion frame -> Closed(actual vertices)) ->
    //           (Closed((from -> to) -> native chord word) -> closure witness)
    // prototype = actual pattern -> Closed(episode identity -> actual pattern)
    // fragment = actual open pattern -> Closed(actual prototype -> actual edge window)
    // A shared prototype occurrence scope supplies empirical multiplicity;
    // matches do not duplicate its complete episode list for every window.
    // model = (motion frame -> Closed(prototypes)) ->
    //         (Closed(fragments) -> explicit forgotten complement)
    // query = Closed(source -> (Closed(actual route patterns) -> provenance))
    // proposal = (model -> query) -> (prototype ->
    //            (supported route bindings -> (contradicted -> unaccounted)))
    // Training source graphs remain in TrainingWitnesses, outside the compact
    // learned port. Stored episode identities retain frequency and aliasing,
    // not a promise to reconstruct the discarded training scenes.

    public sealed record TrainingItem(Relation SourceIdentity, NativeInteriorFeatures Features);

    public sealed class Pattern
    {
        public QualitativeMotionFrame Frame { get; }
        public Relation Sites { get; }
        public Relation Edges { get; }
        public Relation Closure { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Relation> Words { get; }
        public IReadOnlyList<int> Headings { get; }
        public bool IsClosed { get; }
        public string Key { get; }
        public string ShapeKey { get; }

        private Pattern(QualitativeMotionFrame frame, IReadOnlyList<Relation> words, bool closed, Relation? native = null)
        {
            Frame = frame; Words = words.ToArray(); IsClosed = closed;
            var vertices = Enumerable.Range(0, words.Count + (closed ? 0 : 1)).Select(_ => (IElement)new Placeholder()).ToArray();
            Sites = S(vertices);
            var edges = words.Select((word, i) => (IElement)new Relation(
                new Relation(vertices[i], vertices[(i + 1) % vertices.Length]), word)).ToArray();
            Edges = S(edges);
            Closure = closed && vertices.Length != 0 ? S(new Relation(vertices[0], vertices[0])) : StructuralConstraint.Open();
            Surface = new(new Relation(frame.Surface, Sites), new Relation(Edges, Closure));
            if (native is { Left: Relation { Right: Relation nativeSites }, Right: Relation { Left: Relation nativeEdges, Right: Relation nativeClosure } })
            { Sites = nativeSites; Edges = nativeEdges; Closure = nativeClosure; Surface = native; }
            Headings = words.Select(word => Sector(frame, word)).ToArray();
            Key = Canonical(words, frame, closed, false).Key;
            ShapeKey = Canonical(words, frame, closed, true).Key;
        }

        public static Pattern FromBoundary(NativeInteriorFeatures.BoundaryRead read, QualitativeMotionFrame sourceFrame)
        {
            var words = E(read.Sides).Cast<Relation>().Select(side => (Relation)((Relation)side.Right).Right).ToArray();
            return new(sourceFrame, words, true);
        }

        public static Pattern FromWords(QualitativeMotionFrame frame, IReadOnlyList<Relation> words, bool closed = false)
        {
            if (words.Count == 0 || words.Any(word => Sector(frame, word) < 0))
                throw new ArgumentException("A learned route needs at least one readable nonzero native chord.", nameof(words));
            if (closed && !Cancels(frame, words))
                throw new ArgumentException("A closed route requires native shared-reach cancellation, not only a closing label.", nameof(words));
            return new(frame, Merge(frame, words, closed), closed);
        }

        internal static Pattern FromUses(QualitativeMotionFrame frame,
            IReadOnlyList<(IElement From, IElement To, Relation Word)> uses, bool closed)
        {
            var merged = new List<(IElement From, IElement To, Relation Word)>();
            foreach (var use in uses)
                if (merged.Count != 0 && Mergeable(frame, merged[^1].Word, use.Word))
                {
                    var prior = merged[^1]; merged[^1] = (prior.From, use.To, Join(frame, prior.Word, use.Word));
                }
                else merged.Add(use);
            if (closed && merged.Count > 1 && Mergeable(frame, merged[^1].Word, merged[0].Word))
            {
                var last = merged[^1]; var first = merged[0]; merged[0] = (last.From, first.To, Join(frame, last.Word, first.Word));
                merged.RemoveAt(merged.Count - 1);
            }
            var sites = merged.Select(use => use.From).Append(merged[^1].To).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            var edges = merged.Select(use => (IElement)new Relation(new Relation(use.From, use.To), use.Word)).ToArray();
            var closure = closed ? S(new Relation(merged[0].From, merged[0].From)) : StructuralConstraint.Open();
            var native = new Relation(new Relation(frame.Surface, S(sites)), new Relation(S(edges), closure));
            return new(frame, merged.Select(use => use.Word).ToArray(), closed, native);
        }

        internal static bool TryRead(QualitativeMotionFrame frame, Relation surface, out Pattern pattern)
        {
            pattern = null!;
            if (surface is not { Left: Relation { Left: var actualFrame, Right: Relation sites },
                Right: Relation { Left: Relation edges, Right: Relation closure } } || !ReferenceEquals(actualFrame, frame.Surface) ||
                !Closed(sites) || !Closed(edges) || E(edges).Length == 0) return false;
            var rows = E(edges).OfType<Relation>().ToArray();
            if (rows.Length != E(edges).Length || rows.Any(edge => edge.Left is not Relation || edge.Right is not Relation)) return false;
            var ends = rows.Select(edge => (Relation)edge.Left).ToArray();
            if (ends.Where((end, i) => i != 0 && !ReferenceEquals(end.Left, ends[i - 1].Right)).Any()) return false;
            var vertices = E(sites);
            if (vertices.Distinct(ReferenceEqualityComparer.Instance).Count() != vertices.Length ||
                ends.Any(end => !vertices.Contains(end.Left, ReferenceEqualityComparer.Instance) || !vertices.Contains(end.Right, ReferenceEqualityComparer.Instance))) return false;
            var closed = ReferenceEquals(ends[0].Left, ends[^1].Right);
            var expected = ends.Select(end => end.Left).Concat(closed ? [] : new[] { ends[^1].Right }).ToArray();
            if (expected.Length != vertices.Length || expected.Distinct<IElement>(ReferenceEqualityComparer.Instance).Count() != expected.Length ||
                !expected.SequenceEqual(vertices, ReferenceEqualityComparer.Instance) ||
                rows.Distinct<Relation>(ReferenceEqualityComparer.Instance).Count() != rows.Length) return false;
            if (closed != Closed(closure) || closed && E(closure) is not [Relation] ||
                closed && E(closure)[0] is Relation seam && (!ReferenceEquals(seam.Left, ends[0].Left) || !ReferenceEquals(seam.Right, ends[0].Left))) return false;
            var words = rows.Select(edge => (Relation)edge.Right).ToArray();
            if (words.Any(word => Sector(frame, word) < 0) || closed && !Cancels(frame, words)) return false;
            pattern = new(frame, words, closed, surface); return true;
        }

        internal Pattern Import(QualitativeMotionFrame target, bool canonical = false)
        {
            var choice = canonical ? Canonical(Words, Frame, IsClosed, false) : (Key: "", Offset: 0, Turn: 0, Mirror: false, Reverse: false);
            var reachMap = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
            var words = Enumerable.Range(0, Words.Count).Select(index => TransformWord(
                choice.Reverse ? UnitArrangementRead.Inverse(Frame, Words[(choice.Offset - index + Words.Count) % Words.Count]) :
                    Words[(choice.Offset + index) % Words.Count], Frame, target, choice.Turn, choice.Mirror, reachMap)).ToArray();
            return new(target, words, IsClosed);
        }
    }

    public sealed class Prototype
    {
        public Pattern Pattern { get; }
        public Relation Occurrences { get; }
        public Relation Surface { get; }
        public string Key => Pattern.Key;
        public string ShapeKey => Pattern.ShapeKey;
        public int Support { get; }
        internal Prototype(Pattern pattern, Relation occurrences, Relation? native = null)
        { Pattern = pattern; Occurrences = occurrences; Surface = native ?? new(pattern.Surface, occurrences); Support = E(occurrences).Length; }
    }

    public sealed class Fragment
    {
        public Pattern Pattern { get; }
        public Relation Matches { get; }
        public Relation Surface { get; }
        public int Support { get; }
        internal Fragment(Pattern pattern, Relation matches, Relation? native = null)
        {
            Pattern = pattern; Matches = matches; Surface = native ?? new(pattern.Surface, matches);
            Support = E(matches).OfType<Relation>().Select(row => row.Left).OfType<Relation>()
                .Where(prototype => prototype.Right is Relation).Sum(prototype => E((Relation)prototype.Right).Length);
        }
    }

    public sealed record ClueStream(Relation Source, IReadOnlyList<Pattern> Routes, Relation Provenance)
    {
        public QualitativeMotionFrame? Frame { get; init; }
        public Relation Bindings { get; init; } = S();
        public Relation Surface => new(Source, new Relation(new Relation(S(Routes.Select(route => (IElement)route.Surface).ToArray()), Bindings), Provenance));
    }

    public sealed class Query
    {
        public IReadOnlyList<ClueStream> Streams { get; }
        public Relation Surface { get; }
        public Relation Unresolved { get; }
        public Relation Contradictions { get; }
        public Relation Loss { get; }
        public Query(IEnumerable<ClueStream> streams, Relation? unresolved = null)
        {
            Streams = streams.ToArray();
            var chords = InputChordConsistency(Streams);
            Unresolved = S(E(unresolved ?? S()).Concat(E(BindingUnknown(Streams))).Concat(E(chords.Unknown)).ToArray());
            Contradictions = S(E(BindingContradictions(Streams)).Concat(E(chords.Contradictions)).ToArray());
            // The heading projection deliberately forgets reach calibration,
            // absolute position and face ownership. Their absence is not zero.
            Loss = StructuralConstraint.Open(S(Streams.Select(stream => (IElement)stream.Surface).ToArray()));
            Surface = new(S(Streams.Select(stream => (IElement)stream.Surface).ToArray()),
                new Relation(Unresolved, new Relation(Contradictions, Loss)));
        }
    }

    public sealed class Prediction
    {
        public Prototype Prototype { get; }
        public double Score { get; }
        /// <summary>A normalized ranking allocation, not a calibrated probability.</summary>
        public double Share { get; internal set; }
        /// <summary>Support in the explicitly selected heading projection;
        /// unresolved interior-site correspondence remains in Open.</summary>
        public Relation Supported { get; }
        public Relation Contradicted { get; }
        public Relation Open { get; }
        public Relation Surface { get; }
        /// <summary>Not refuted. This may still require native correspondence
        /// obligations in Open; it does not assert a completed graph embedding.</summary>
        public bool IsAdmissible => E(Contradicted).Length == 0;
        internal Prediction(Model model, Query query, Prototype prototype, double score,
            Relation supported, Relation contradicted, Relation open)
        {
            Prototype = prototype; Score = score; Supported = supported; Contradicted = contradicted; Open = open;
            Surface = new(new Relation(model.Surface, query.Surface), new Relation(prototype.Surface,
                new Relation(supported, new Relation(contradicted, open))));
        }
    }

    public sealed class Model
    {
        public QualitativeMotionFrame Frame { get; }
        public IReadOnlyList<Prototype> Prototypes { get; }
        public IReadOnlyList<Fragment> Fragments { get; }
        public Relation Loss { get; }
        public Relation Surface { get; }
        public Relation TrainingWitnesses { get; }
        public int TrainingEpisodes => Prototypes.SelectMany(prototype => E(prototype.Occurrences).Cast<Relation>())
            .Select(occurrence => occurrence.Left).Distinct(ReferenceEqualityComparer.Instance).Count();
        private readonly Dictionary<Fragment, HashSet<Relation>> fragmentPatterns = [];
        private readonly Dictionary<(string Query, string Candidate), bool> coarseMatches = [];
        internal Model(QualitativeMotionFrame frame, IReadOnlyList<Prototype> prototypes, IReadOnlyList<Fragment> fragments,
            Relation? witnesses = null, Relation? loss = null, Relation? native = null)
        {
            Frame = frame; Prototypes = prototypes; Fragments = fragments;
            TrainingWitnesses = witnesses ?? S(); Loss = loss ?? StructuralConstraint.Open();
            Surface = native ?? new(new Relation(frame.Surface, S(prototypes.Select(item => (IElement)item.Surface).ToArray())),
                new Relation(S(fragments.Select(item => (IElement)item.Surface).ToArray()), Loss));
            foreach (var fragment in fragments)
                fragmentPatterns[fragment] = E(fragment.Matches).OfType<Relation>().Select(row => row.Left)
                    .OfType<Relation>().Select(prototype => prototype.Left).OfType<Relation>()
                    .ToHashSet<Relation>(ReferenceEqualityComparer.Instance);
        }

        public IReadOnlyList<Prediction> Rank(Query query, int limit = 8, bool useLearnedFragments = true, int bindingCapacity = 512)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(limit);
            ArgumentOutOfRangeException.ThrowIfNegative(bindingCapacity);
            var result = new List<Prediction>();
            var routeFragments = query.Streams.SelectMany(stream => stream.Routes).Distinct().ToDictionary(route => route,
                route => useLearnedFragments ? Fragments.Where(fragment => fragment.Pattern.Words.Count <= route.Words.Count &&
                    Fits(fragment.Pattern, route)).ToArray() : []);
            foreach (var prototype in Prototypes)
            {
                var supported = new List<IElement>(); var contradicted = new List<IElement>(); var open = new List<IElement>();
                contradicted.AddRange(E(query.Contradictions));
                var score = useLearnedFragments ? Math.Log(1 + prototype.Support) : 0.0;
                // Repeated routes from the same advertised source are retained
                // but do not multiply their ranking contribution. Distinct
                // sources are not asserted to be statistically independent.
                var sourceScore = new Dictionary<Relation, double>(ReferenceEqualityComparer.Instance);
                foreach (var stream in query.Streams)
                {
                    var local = 0.0;
                    foreach (var route in stream.Routes)
                    {
                        var bindings = Fits(route, prototype.Pattern) ? Match(route, prototype.Pattern) : S();
                        var witness = new Relation(stream.Surface, new Relation(route.Surface, bindings));
                        if (E(bindings).Length == 0)
                        { contradicted.Add(witness); local -= route.IsClosed ? 12 : 3; }
                        else
                        {
                            supported.Add(witness); local += Math.Min(6, route.Words.Count);
                            if (useLearnedFragments)
                                foreach (var fragment in routeFragments[route].Where(fragment => fragmentPatterns[fragment].Contains(prototype.Pattern.Surface)))
                                    local += Math.Log(1 + fragment.Support) / Math.Max(1, Fragments.Count);
                        }
                    }
                    if (stream.Routes.Count == 0) open.Add(stream.Surface);
                    if (!sourceScore.TryGetValue(stream.Source, out var prior) || Math.Abs(local) > Math.Abs(prior)) sourceScore[stream.Source] = local;
                }
                if (query.Streams.Count == 0) open.Add(query.Surface);
                if (E(query.Unresolved).Length != 0) open.Add(query.Unresolved);
                if (contradicted.Count == 0)
                {
                    var joint = CoherentMatch(query, prototype.Pattern, bindingCapacity);
                    if (joint.State == "No") contradicted.Add(joint.Witness);
                    else if (joint.State == "Open") open.Add(joint.Witness);
                    else supported.Add(joint.Witness);
                }
                score += sourceScore.Values.Sum();
                result.Add(new(this, query, prototype, score, S(supported.ToArray()), S(contradicted.ToArray()), S(open.ToArray())));
            }
            var ordered = result.OrderByDescending(item => item.Score).ThenBy(item => item.Prototype.Key, StringComparer.Ordinal).Take(limit).ToArray();
            var maximum = ordered.Length == 0 ? 0 : ordered[0].Score;
            var total = ordered.Sum(item => Math.Exp(item.Score - maximum));
            foreach (var item in ordered) item.Share = Math.Exp(item.Score - maximum) / total;
            return ordered;

            bool Fits(Pattern queryPattern, Pattern candidate)
            {
                var key = (queryPattern.ShapeKey, candidate.ShapeKey);
                if (!coarseMatches.TryGetValue(key, out var fits))
                { fits = HeadingMatches(queryPattern, candidate); coarseMatches.Add(key, fits); }
                return fits;
            }
        }

        public static bool TryRead(Relation surface, out Model model)
        {
            model = null!;
            if (surface is not { Left: Relation { Left: Relation rawFrame, Right: Relation prototypes },
                Right: Relation { Left: Relation fragments, Right: Relation loss } } || !Closed(prototypes) || !Closed(fragments) ||
                !QualitativeMotionFrameRead.TryReadStandard(rawFrame, out var rebind) ||
                !rebind.TryRebind(surface, out var imported, out _) || imported is not Relation bound ||
                bound.Left is not Relation { Right: Relation prototypeRows } || bound.Right is not Relation { Left: Relation fragmentRows, Right: Relation boundLoss }) return false;
            var learned = new List<Prototype>(); var mined = new List<Fragment>();
            foreach (var item in E(prototypeRows))
            {
                if (item is not Relation { Left: Relation pattern, Right: Relation occurrences } actual || !Closed(occurrences) ||
                    !Pattern.TryRead(rebind.Target, pattern, out var read) || !read.IsClosed || E(occurrences).Length == 0) return false;
                if (E(occurrences).Any(entry => entry is not Relation occurrence || !ReferenceEquals(occurrence.Right, pattern))) return false;
                if (E(occurrences).Cast<Relation>().Select(occurrence => occurrence.Left)
                    .Distinct<IElement>(ReferenceEqualityComparer.Instance).Count() != E(occurrences).Length) return false;
                learned.Add(new(read, occurrences, actual));
            }
            var windowKeys = new Dictionary<Relation, string>(ReferenceEqualityComparer.Instance);
            var owners = new Dictionary<Relation, Prototype>(ReferenceEqualityComparer.Instance);
            foreach (var prototype in learned) owners.Add(prototype.Surface, prototype);
            foreach (var item in E(fragmentRows))
            {
                if (item is not Relation { Left: Relation pattern, Right: Relation matches } actual || !Closed(matches) ||
                    !Pattern.TryRead(rebind.Target, pattern, out var read) || read.IsClosed) return false;
                if (E(matches).Length == 0) return false;
                foreach (var entry in E(matches))
                {
                    if (entry is not Relation { Left: Relation parent, Right: Relation window } ||
                        !Closed(window)) return false;
                    if (!owners.TryGetValue(parent, out var owner)) return false;
                    var edges = E(owner.Pattern.Edges); var uses = E(window);
                    if (uses.Length < 2 || uses.Length >= edges.Length || uses.Any(use => !edges.Contains(use, ReferenceEqualityComparer.Instance))) return false;
                    var start = Array.FindIndex(edges, edge => ReferenceEquals(edge, uses[0]));
                    if (uses.Where((use, offset) => !ReferenceEquals(use, edges[(start + offset) % edges.Length])).Any()) return false;
                    if (!windowKeys.TryGetValue(window, out var expectedKey))
                    {
                        expectedKey = Pattern.FromWords(rebind.Target, uses.Cast<Relation>().Select(edge => (Relation)edge.Right).ToArray()).Key;
                        windowKeys.Add(window, expectedKey);
                    }
                    if (expectedKey != read.Key) return false;
                }
                mined.Add(new(read, matches, actual));
            }
            if (learned.Select(item => item.Key).Distinct().Count() != learned.Count) return false;
            model = new(rebind.Target, learned, mined, loss: boundLoss, native: bound); return true;
        }
    }

    public static Model Train(IEnumerable<TrainingItem> examples, int maxFragmentLength = 4, int maximumAuditWitnesses = 32)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxFragmentLength);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumAuditWitnesses);
        var frame = new QualitativeMotionFrame();
        var groups = new Dictionary<string, (Pattern Pattern, List<IElement> Occurrences)>();
        var witnesses = new List<IElement>();
        foreach (var item in examples)
        {
            var episodeSeen = new HashSet<string>();
            foreach (var boundary in item.Features.Reads)
            {
                if (boundary.Find("Closed").Evidence.Answer != "Yes" || !boundary.SideCountCertain) continue;
                var original = Pattern.FromBoundary(boundary, item.Features.Topology.Source.Graph.Frame);
                if (original.Headings.Any(heading => heading < 0) || !Cancels(original.Frame, original.Words)) continue;
                var pattern = original.Import(frame, true);
                if (!groups.TryGetValue(pattern.Key, out var group))
                { group = (pattern, []); groups.Add(pattern.Key, group); }
                // One scene with many equal cells is one exposure for this
                // prototype, rather than many independent training samples.
                if (episodeSeen.Add(pattern.Key) && !group.Occurrences.Cast<Relation>().Any(prior => ReferenceEquals(prior.Left, item.SourceIdentity)))
                    group.Occurrences.Add(new Relation(item.SourceIdentity, group.Pattern.Surface));
                if (witnesses.Count < maximumAuditWitnesses)
                    witnesses.Add(new Relation(new Relation(item.SourceIdentity, boundary.Surface), group.Pattern.Surface));
            }
        }
        var prototypes = groups.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new Prototype(pair.Value.Pattern, S(pair.Value.Occurrences.ToArray()))).ToArray();
        var fragmentGroups = new Dictionary<string, (Pattern Pattern, List<IElement> Matches)>();
        foreach (var prototype in prototypes)
            for (var length = 2; length <= Math.Min(maxFragmentLength, prototype.Pattern.Words.Count - 1); length++)
                for (var start = 0; start < prototype.Pattern.Words.Count; start++)
                {
                    var words = Enumerable.Range(0, length).Select(offset => prototype.Pattern.Words[(start + offset) % prototype.Pattern.Words.Count]).ToArray();
                    var fragment = Pattern.FromWords(frame, words).Import(frame, true);
                    if (!fragmentGroups.TryGetValue(fragment.Key, out var group))
                    { group = (fragment, []); fragmentGroups.Add(fragment.Key, group); }
                    var edgeRows = E(prototype.Pattern.Edges);
                    var window = S(Enumerable.Range(0, length).Select(offset => edgeRows[(start + offset) % edgeRows.Length]).ToArray());
                    // Multiplicity lives once on the actual prototype. Every
                    // observed window can read that shared occurrence scope.
                    group.Matches.Add(new Relation(prototype.Surface, window));
                }
        var fragments = fragmentGroups.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new Fragment(pair.Value.Pattern, S(pair.Value.Matches.ToArray()))).ToArray();
        return new(frame, prototypes, fragments, S(witnesses.ToArray()));
    }

    /// <summary>Reads only advertised native passages and actual endpoint
    /// incidence. Degree-two routes are followed; a branch remains a set of
    /// shorter alternatives. No teacher boundary or factory history is read.</summary>
    public static Query FromSource(NativeLatticeSources.SourceCase source, int routeCapacity = 128)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(routeCapacity);
        // The caller supplies an exposed source. Re-publishing each stream here
        // would destroy the shared site aliases needed to retain contradictions.
        var published = source;
        var frame = published.Graph.Frame;
        var edges = new List<(Relation Stroke, IElement From, IElement To, Relation Word)>();
        var unresolved = new List<IElement>();
        foreach (var scope in new[] { published.Graph.Sites, published.Graph.Strokes, published.Bindings, published.StraightPassages })
            if (!Closed(scope)) unresolved.Add(scope);
        foreach (var item in E(published.Graph.Strokes))
        {
            if (item is not Relation row || !QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out var word) ||
                !E(published.StraightPassages).Contains(row, ReferenceEqualityComparer.Instance) || Sector(frame, word) < 0)
            { unresolved.Add(item); continue; }
            edges.Add((row, from, to, word));
        }
        var used = new HashSet<Relation>(ReferenceEqualityComparer.Instance); var routes = new List<Pattern>();
        int Degree(IElement site) => edges.Count(edge => ReferenceEquals(edge.From, site) || ReferenceEquals(edge.To, site));
        foreach (var first in edges.OrderBy(edge => Degree(edge.From) == 2 && Degree(edge.To) == 2 ? 1 : 0))
        {
            if (used.Contains(first.Stroke)) continue;
            var start = Degree(first.From) != 2 ? first.From : Degree(first.To) != 2 ? first.To : first.From;
            var at = start; var current = first; var words = new List<Relation>();
            var uses = new List<(IElement From, IElement To, Relation Word)>(); var closed = false;
            while (used.Add(current.Stroke))
            {
                var forward = ReferenceEquals(at, current.From);
                var word = forward ? current.Word : UnitArrangementRead.Inverse(frame, current.Word);
                var nextSite = forward ? current.To : current.From;
                words.Add(word); uses.Add((at, nextSite, word)); at = nextSite;
                if (ReferenceEquals(at, start)) { closed = true; break; }
                if (Degree(at) != 2) break;
                var next = edges.Where(edge => !used.Contains(edge.Stroke) &&
                    (ReferenceEquals(edge.From, at) || ReferenceEquals(edge.To, at))).ToArray();
                if (next.Length != 1) break; current = next[0];
            }
            if (routes.Count == routeCapacity) { unresolved.Add(S(words.Cast<IElement>().ToArray())); continue; }
            if (closed && !Cancels(frame, words)) { unresolved.Add(S(words.Cast<IElement>().ToArray())); closed = false; }
            if (words.Count != 0) routes.Add(Pattern.FromUses(frame, uses, closed));
        }
        return new([new ClueStream(published.Graph.Surface, routes, new Relation(published.Graph.Surface, published.StraightPassages))
            { Frame = frame, Bindings = published.Bindings }], S(unresolved.ToArray()));
    }

    public static Query Combine(IEnumerable<ClueStream> streams) => new(streams);
    public static Query CombineQueries(IEnumerable<Query> queries)
    {
        var inputs = queries.ToArray();
        return new(inputs.SelectMany(query => query.Streams), S(inputs.SelectMany(query => E(query.Unresolved)).ToArray()));
    }

    public sealed record Realization(UnitArrangementProgram Unit, UnitArrangementNode Execution,
        NativeLatticeSources.SourceCase Source, Relation Surface);

    /// <summary>Executes stored native chord words through the existing muscle /
    /// drawing engine. No recipe name or original C# factory selects output.</summary>
    public static Realization Realize(Prototype prototype)
    {
        var pattern = prototype.Pattern.Import(new QualitativeMotionFrame());
        var path = LatticeLadderAuthor.Path(S(), pattern.Words.ToArray());
        var unit = new UnitArrangementProgram(pattern.Frame, "learned route", S(path), LatticeLadderAuthor.Ports(path), S(path), prototype.Surface);
        var execution = new UnitArrangementNode("learned route", [unit], unit, S());
        var current = execution.Current;
        var raw = new NativeLatticeSources.SourceCase("learned", "Learned native route", "A sampled learned prototype; source correspondence is an assumption, not observed identity.",
            current.Join.Graph, current.Join.Bindings, S(E(current.Join.Graph.Strokes)), current.Surface, true);
        var published = new NativeLinePublication(raw).Published;
        return new(unit, execution, published, new Relation(prototype.Surface, new Relation(unit.Surface, current.Surface)));
    }

    private static Relation Match(Pattern query, Pattern candidate)
    {
        var source = query.Headings; var target = candidate.Headings; var result = new List<IElement>();
        if (query.IsClosed && (!candidate.IsClosed || source.Count != target.Count) || source.Count > target.Count) return S();
        var qEdges = E(query.Edges); var cEdges = E(candidate.Edges);
        for (var reversed = 0; reversed < 2; reversed++) for (var mirror = 0; mirror < 2; mirror++) for (var turn = 0; turn < 4; turn++)
            for (var offset = 0; offset < (candidate.IsClosed ? target.Count : Math.Max(0, target.Count - source.Count + 1)); offset++)
            {
                var okay = true;
                for (var index = 0; index < source.Count; index++)
                {
                    var heading = reversed == 0 ? source[index] : (source[source.Count - 1 - index] + 4) % 8;
                    var actual = (heading * (mirror == 0 ? 1 : -1) + turn * 2 + 16) % 8;
                    if (actual != target[(offset + index) % target.Count]) { okay = false; break; }
                }
                if (okay) result.Add(new Relation(HeadingBasis(query.Frame, candidate.Frame, turn, mirror != 0),
                    S(Enumerable.Range(0, source.Count).Select(index =>
                    {
                        var original = (Relation)qEdges[reversed == 0 ? index : source.Count - 1 - index];
                        var ends = (Relation)original.Left;
                        return (IElement)new Relation(new Relation(original, reversed == 0 ? ends : ends.Reverse()), cEdges[(offset + index) % target.Count]);
                    }).ToArray())));
            }
        return S(result.ToArray());
    }

    private sealed record RouteAlignment(Dictionary<IElement, IElement> Bindings, Relation Witness);
    private sealed record JointRead(string State, Relation Witness);

    private static JointRead CoherentMatch(Query query, Pattern candidate, int capacity = 512)
    {
        var routes = query.Streams.SelectMany(stream => stream.Routes).Distinct().ToArray();
        if (routes.Length == 0) return new("Open", new Relation(query.Surface, StructuralConstraint.Open(candidate.Surface)));
        var consumed = new List<IElement>(); var cuts = new List<IElement>(); var failures = new List<IElement>();
        var candidateEdges = E(candidate.Edges).Cast<Relation>().ToArray();
        var visited = 0;
        for (var mirror = 0; mirror < 2; mirror++) for (var turn = 0; turn < 4; turn++)
        {
            var alternatives = routes.Select(route => Alignments(route, turn, mirror != 0).ToArray()).ToArray();
            if (alternatives.Any(items => items.Length == 0)) continue;
            var order = Enumerable.Range(0, routes.Length).OrderBy(index => alternatives[index].Length).ToArray();
            var map = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
            var reverse = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
            var selected = new List<IElement>();
            if (Search(0)) return new("Yes", new Relation(query.Surface, new Relation(candidate.Surface, S(selected.ToArray()))));
            if (visited >= capacity)
            {
                // Hitting the final slot is still a search cut even when that
                // slot happened to exhaust one orientation's last alignment.
                // Other orientations have not thereby been refuted.
                cuts.Add(new Relation(candidate.Surface, HeadingBasis(routes[0].Frame, candidate.Frame, turn, mirror != 0)));
                break;
            }

            bool Search(int depth)
            {
                if (depth == order.Length) return true;
                foreach (var alignment in alternatives[order[depth]])
                {
                    if (visited++ >= capacity) { cuts.Add(alignment.Witness); return false; }
                    var additions = new List<(IElement From, IElement To)>(); var compatible = true;
                    foreach (var pair in alignment.Bindings)
                    {
                        if (map.TryGetValue(pair.Key, out var prior) && !ReferenceEquals(prior, pair.Value) ||
                            reverse.TryGetValue(pair.Value, out var other) && !ReferenceEquals(other, pair.Key))
                        { compatible = false; break; }
                        if (!map.ContainsKey(pair.Key)) { map.Add(pair.Key, pair.Value); reverse.Add(pair.Value, pair.Key); additions.Add((pair.Key, pair.Value)); }
                    }
                    consumed.Add(alignment.Witness);
                    if (compatible)
                    {
                        selected.Add(alignment.Witness);
                        if (Search(depth + 1)) return true;
                        selected.RemoveAt(selected.Count - 1);
                    }
                    else failures.Add(alignment.Witness);
                    foreach (var addition in additions) { map.Remove(addition.From); reverse.Remove(addition.To); }
                    if (visited >= capacity) return false;
                }
                return false;
            }
        }
        var failedCornerRead = new Relation(new Relation(query.Surface, candidate.Surface),
            new Relation(S(consumed.ToArray()), new Relation(S(failures.ToArray()), cuts.Count != 0 ? StructuralConstraint.Open(cuts.ToArray()) : S())));
        if (cuts.Count != 0) return new("Open", failedCornerRead);
        if (routes.All(route => !route.IsClosed))
        {
            // A partial source route can stop between a learned side's corners.
            // Exhausting corner-to-corner bindings does not refute that finer
            // correspondence. No interior placement is silently accepted here:
            // actual roles and candidate ordered side bounds remain an explicit
            // open obligation, beside the exhausted corner search.
            var roles = S(routes.SelectMany(route => E(route.Sites)).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray());
            var bounds = S(candidateEdges.Select(edge => (IElement)new Relation(edge, edge.Left)).ToArray());
            var interiorObligation = new Relation(roles, new Relation(bounds, StructuralConstraint.Open()));
            return new("Open", new Relation(failedCornerRead, interiorObligation));
        }
        return new("No", failedCornerRead);

        IEnumerable<RouteAlignment> Alignments(Pattern route, int turn, bool mirror)
        {
            if (route.IsClosed && route.Words.Count != candidate.Words.Count || route.Words.Count > candidate.Words.Count) yield break;
            var routeEdges = E(route.Edges).Cast<Relation>().ToArray();
            for (var reversed = 0; reversed < 2; reversed++)
                for (var offset = 0; offset < candidate.Words.Count; offset++)
                {
                    var bindings = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance); var rows = new List<IElement>(); var valid = true;
                    for (var index = 0; index < route.Words.Count; index++)
                    {
                        var sourceIndex = reversed == 0 ? index : route.Words.Count - 1 - index;
                        var heading = (route.Headings[sourceIndex] + (reversed == 0 ? 0 : 4)) % 8;
                        heading = (heading * (mirror ? -1 : 1) + turn * 2 + 16) % 8;
                        var targetIndex = (offset + index) % candidate.Words.Count;
                        if (heading != candidate.Headings[targetIndex]) { valid = false; break; }
                        var sourceEdge = routeEdges[sourceIndex]; var targetEdge = candidateEdges[targetIndex];
                        var sourceEnds = (Relation)sourceEdge.Left; var targetEnds = (Relation)targetEdge.Left;
                        var from = reversed == 0 ? sourceEnds.Left : sourceEnds.Right;
                        var to = reversed == 0 ? sourceEnds.Right : sourceEnds.Left;
                        if (!Bind(from, targetEnds.Left) || !Bind(to, targetEnds.Right)) { valid = false; break; }
                        rows.Add(new Relation(new Relation(sourceEdge, new Relation(from, to)), targetEdge));
                    }
                    if (valid) yield return new(bindings, new Relation(HeadingBasis(route.Frame, candidate.Frame, turn, mirror),
                        new Relation(S(rows.ToArray()), S(bindings.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()))));
                    bool Bind(IElement from, IElement to)
                    {
                        if (bindings.TryGetValue(from, out var prior)) return ReferenceEquals(prior, to);
                        bindings.Add(from, to); return true;
                    }
                }
        }
    }

    private static Relation BindingContradictions(IEnumerable<ClueStream> streams)
    {
        var rows = streams.Where(stream => stream.Frame is not null).SelectMany(stream => E(stream.Bindings)
            .OfType<Relation>().Select(row => (Frame: stream.Frame!, Row: row))).ToArray();
        var conflicts = new List<IElement>();
        for (var i = 0; i < rows.Length; i++) for (var j = i + 1; j < rows.Length; j++)
        {
            var a = rows[i]; var b = rows[j];
            if (!ReferenceEquals(a.Row.Left, b.Row.Left) || !ReferenceEquals(a.Frame, b.Frame) ||
                a.Row.Right is not Relation aw || b.Row.Right is not Relation bw || !Closed(aw) || !Closed(bw)) continue;
            var reduction = a.Frame.Reduce(UnitArrangementRead.Inverse(a.Frame, aw), bw);
            if (a.Frame.TryReadResult(reduction, out var remaining, out _, out var unresolved) && Closed(remaining) &&
                E(unresolved).Length == 0 && E(remaining).Length != 0)
                conflicts.Add(new Relation(new Relation(a.Row, b.Row), reduction));
        }
        return S(conflicts.ToArray());
    }

    private static (Relation Contradictions, Relation Unknown) InputChordConsistency(IEnumerable<ClueStream> streams)
    {
        var chords = new List<(QualitativeMotionFrame Frame, Relation Row, IElement From, IElement To, Relation Word)>();
        var seen = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var stream in streams)
        {
            if (stream.Frame is not { } frame || stream.Source is not
                { Left: var actualFrame, Right: Relation { Left: Relation { Right: Relation strokes } } } ||
                !ReferenceEquals(actualFrame, frame.Surface) || stream.Provenance.Right is not Relation offered) continue;
            var passageRows = E(offered);
            foreach (var item in E(strokes))
                if (item is Relation row && passageRows.Contains(row, ReferenceEqualityComparer.Instance) &&
                    QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out var word) && Closed(word) && seen.Add(row))
                    chords.Add((frame, row, from, to, word));
        }
        var conflicts = new List<IElement>(); var unknown = new List<IElement>();
        for (var i = 0; i < chords.Count; i++) for (var j = i + 1; j < chords.Count; j++)
        {
            var a = chords[i]; var b = chords[j];
            if (!ReferenceEquals(a.Frame, b.Frame)) continue;
            var same = ReferenceEquals(a.From, b.From) && ReferenceEquals(a.To, b.To);
            var reversed = ReferenceEquals(a.From, b.To) && ReferenceEquals(a.To, b.From);
            if (!same && !reversed) continue;
            var second = same ? b.Word : UnitArrangementRead.Inverse(a.Frame, b.Word);
            var reduction = a.Frame.Reduce(UnitArrangementRead.Inverse(a.Frame, a.Word), second);
            var witness = new Relation(new Relation(a.Row, b.Row), reduction);
            if (!a.Frame.TryReadResult(reduction, out var remaining, out _, out var unresolved) || !Closed(remaining) || E(unresolved).Length != 0)
                unknown.Add(witness);
            else if (E(remaining).Length != 0) conflicts.Add(witness);
        }
        return (S(conflicts.ToArray()), S(unknown.ToArray()));
    }

    private static Relation BindingUnknown(IEnumerable<ClueStream> streams)
    {
        var rows = streams.Where(stream => stream.Frame is not null).SelectMany(stream => E(stream.Bindings)
            .OfType<Relation>().Select(row => (Frame: stream.Frame!, Row: row))).ToArray();
        var unknown = new List<IElement>();
        for (var i = 0; i < rows.Length; i++) for (var j = i + 1; j < rows.Length; j++)
        {
            var a = rows[i]; var b = rows[j];
            if (!ReferenceEquals(a.Row.Left, b.Row.Left) || !ReferenceEquals(a.Frame, b.Frame)) continue;
            if (a.Row.Right is not Relation aw || b.Row.Right is not Relation bw || !Closed(aw) || !Closed(bw))
            { unknown.Add(new Relation(a.Row, b.Row)); continue; }
            var reduction = a.Frame.Reduce(UnitArrangementRead.Inverse(a.Frame, aw), bw);
            if (!a.Frame.TryReadResult(reduction, out _, out _, out var unresolved) || E(unresolved).Length != 0)
                unknown.Add(new Relation(new Relation(a.Row, b.Row), reduction));
        }
        return S(unknown.ToArray());
    }

    private static Relation HeadingBasis(QualitativeMotionFrame source, QualitativeMotionFrame target, int turn, bool mirror)
    {
        var sourceDirections = new[] { source.Right, source.Up, source.Left, source.Down };
        var targetDirections = new[] { target.Right, target.Up, target.Left, target.Down };
        var map = S(sourceDirections.Select((direction, index) => (IElement)new Relation(direction,
            targetDirections[((index * (mirror ? -1 : 1) + turn + 8) % 4)])).ToArray());
        // This explicitly selects a direction-only projection. Reach values,
        // absolute locations and face occupancy are an unaccounted complement.
        return new(new Relation(source.Surface, target.Surface), new Relation(map, StructuralConstraint.Open()));
    }

    private static bool HeadingMatches(Pattern query, Pattern candidate)
    {
        if (query.IsClosed && (!candidate.IsClosed || query.Words.Count != candidate.Words.Count) || query.Words.Count > candidate.Words.Count) return false;
        for (var reversed = 0; reversed < 2; reversed++) for (var mirror = 0; mirror < 2; mirror++) for (var turn = 0; turn < 4; turn++)
            for (var offset = 0; offset < (candidate.IsClosed ? candidate.Words.Count : candidate.Words.Count - query.Words.Count + 1); offset++)
            {
                var valid = true;
                for (var index = 0; index < query.Words.Count; index++)
                {
                    var heading = reversed == 0 ? query.Headings[index] : (query.Headings[query.Words.Count - 1 - index] + 4) % 8;
                    if ((heading * (mirror == 0 ? 1 : -1) + turn * 2 + 16) % 8 != candidate.Headings[(offset + index) % candidate.Words.Count])
                    { valid = false; break; }
                }
                if (valid) return true;
            }
        return false;
    }

    private static Relation[] Merge(QualitativeMotionFrame frame, IReadOnlyList<Relation> words, bool closed)
    {
        var result = new List<Relation>();
        foreach (var word in words)
            if (result.Count != 0 && Mergeable(frame, result[^1], word)) result[^1] = Join(frame, result[^1], word);
            else result.Add(word);
        if (closed && result.Count > 1 && Mergeable(frame, result[^1], result[0]))
        { result[0] = Join(frame, result[^1], result[0]); result.RemoveAt(result.Count - 1); }
        return result.ToArray();
    }
    private static Relation Join(QualitativeMotionFrame frame, Relation a, Relation b)
    { frame.TryReadResult(frame.Reduce(a, b), out var remaining, out _, out _); return remaining; }
    private static bool Mergeable(QualitativeMotionFrame frame, Relation a, Relation b)
    {
        var sector = Sector(frame, a);
        return sector >= 0 && sector == Sector(frame, b) && ((sector & 1) == 0 ||
            Cancels(frame, [a, UnitArrangementRead.Inverse(frame, b)]) || Balanced(frame, a) && Balanced(frame, b));
    }
    private static bool Balanced(QualitativeMotionFrame frame, Relation word)
    {
        var horizontal = new List<IElement>(); var vertical = new List<IElement>();
        foreach (var item in E(word))
        {
            if (item is not Relation move || !frame.Direction(move, out var axis, out _, out var reach)) return false;
            (ReferenceEquals(axis, frame.Horizontal) ? horizontal : vertical).Add(QualitativeMotionFrame.Move(frame.Right, reach));
        }
        return Cancels(frame, [S(horizontal.ToArray()), UnitArrangementRead.Inverse(frame, S(vertical.ToArray()))]);
    }

    private static (string Key, int Offset, int Turn, bool Mirror, bool Reverse) Canonical(IReadOnlyList<Relation> words, QualitativeMotionFrame frame, bool closed, bool shapeOnly)
    {
        (string Key, int Offset, int Turn, bool Mirror, bool Reverse)? best = null;
        var sectors = shapeOnly ? words.Select(word => Sector(frame, word)).ToArray() : [];
        // This is a compiled key read, not a transform application. Cache the
        // actual native move participants once instead of constructing inverse
        // Gojo words for every candidate orientation of every cyclic start.
        var nativeMoves = shapeOnly ? [] : words.Select(word => E(word).OfType<Relation>().Select(move =>
            (Direction: Direction(frame, move.Left), Reach: move.Right)).ToArray()).ToArray();
        for (var reversed = 0; reversed < 2; reversed++) for (var mirror = 0; mirror < 2; mirror++) for (var turn = 0; turn < 4; turn++)
            for (var offset = 0; offset < (closed ? words.Count : 1); offset++)
            {
                var beginning = closed || reversed == 0 ? offset : words.Count - 1;
                var reaches = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
                var encoded = new List<string>();
                for (var index = 0; index < words.Count; index++)
                {
                    var sourceIndex = reversed == 0 ? (beginning + index) % words.Count : (beginning - index + words.Count) % words.Count;
                    if (shapeOnly)
                    {
                        var sector = (sectors[sourceIndex] + (reversed == 0 ? 0 : 4)) % 8;
                        encoded.Add(((sector * (mirror == 0 ? 1 : -1) + turn * 2 + 16) % 8).ToString()); continue;
                    }
                    var tokens = new List<string>();
                    foreach (var move in reversed == 0 ? nativeMoves[sourceIndex] : nativeMoves[sourceIndex].Reverse())
                    {
                        if (move.Direction < 0) { tokens.Add("?"); continue; }
                        var reach = move.Reach;
                        if (!reaches.TryGetValue(reach, out var id)) { id = reaches.Count; reaches.Add(reach, id); }
                        var direction = (move.Direction + (reversed == 0 ? 0 : 4)) % 8;
                        var heading = (direction * (mirror == 0 ? 1 : -1) + turn * 2 + 16) % 8;
                        tokens.Add(heading + ":" + id);
                    }
                    encoded.Add(string.Join(',', tokens.Order(StringComparer.Ordinal)));
                }
                var key = (closed ? "C|" : "P|") + string.Join(';', encoded);
                if (best is null || string.CompareOrdinal(key, best.Value.Key) < 0) best = (key, beginning, turn, mirror != 0, reversed != 0);
            }
        return best ?? (closed ? "C|" : "P|", 0, 0, false, false);
    }

    private static Relation TransformWord(Relation word, QualitativeMotionFrame source, QualitativeMotionFrame target,
        int turn, bool mirror, Dictionary<IElement, IElement> reaches)
    {
        var directions = new[] { target.Right, target.Up, target.Left, target.Down };
        return S(E(word).Cast<Relation>().Select(move =>
        {
            source.Direction(move, out _, out _, out var reach);
            if (!reaches.TryGetValue(reach, out var replacement)) { replacement = new Placeholder(); reaches.Add(reach, replacement); }
            var sector = (Direction(source, move.Left) * (mirror ? -1 : 1) + turn * 2 + 16) % 8;
            return (IElement)QualitativeMotionFrame.Move(directions[sector / 2], replacement);
        }).ToArray());
    }

    private static int Sector(QualitativeMotionFrame frame, Relation word)
    {
        var reduced = frame.Reduce(S(), word);
        if (!frame.TryReadResult(reduced, out var remaining, out _, out var unresolved) || !Closed(remaining) || E(unresolved).Length != 0) return -1;
        var headings = E(remaining).OfType<Relation>().Select(move => Direction(frame, move.Left)).Distinct().ToArray();
        if (headings.Length == 1) return headings[0];
        if (headings.Length != 2 || headings.Any(heading => heading < 0)) return -1;
        var x = headings.Contains(0) ? 1 : headings.Contains(4) ? -1 : 0;
        var y = headings.Contains(2) ? 1 : headings.Contains(6) ? -1 : 0;
        return x == 1 && y == 1 ? 1 : x == -1 && y == 1 ? 3 : x == -1 && y == -1 ? 5 : x == 1 && y == -1 ? 7 : -1;
    }
    private static int Direction(QualitativeMotionFrame frame, IElement direction) => ReferenceEquals(direction, frame.Right) ? 0 :
        ReferenceEquals(direction, frame.Up) ? 2 : ReferenceEquals(direction, frame.Left) ? 4 : ReferenceEquals(direction, frame.Down) ? 6 : -1;
    private static bool Cancels(QualitativeMotionFrame frame, IReadOnlyList<Relation> words)
    {
        var read = frame.Reduce(S(), S(words.SelectMany(E).ToArray()));
        return frame.TryReadResult(read, out var remaining, out _, out var unresolved) && Closed(remaining) && E(remaining).Length == 0 && E(unresolved).Length == 0;
    }
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
