using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A bounded affine-action read of an already exposed native source.
/// Actual shared-site components, move substitutions, translation words and
/// bijective incidence witnesses carry the meaning. Screen coordinates, shape
/// labels and the source's private construction are not consulted.</summary>
public static class JointSymmetryEvidence
{
    // component = Closed(actual sites) -> Closed(actual strokes)
    // action = actual direction map -> native move substitution
    // operation = (source component -> target component) ->
    //   ((action -> translation word) ->
    //    ((site map -> stroke map) ->
    //     (matched witnesses -> (unmatched source -> (missing target -> open)))))
    // read = (public source port -> actual geometry model) ->
    //        (components -> (retained operations -> (unresolved -> unsearched)))
    // Read.Surface is an ANALYSIS/AUDIT surface. Its graph and coordinate words
    // must not be passed to a receiver promised anonymous joint/symmetry cues.
    // Such a receiver needs an explicit crystallization with fresh roles.

    public sealed class Component
    {
        public int Index { get; }
        public Relation Sites { get; }
        public Relation Strokes { get; }
        public Relation Surface { get; }
        internal Component(int index, Relation sites, Relation strokes)
        { Index = index; Sites = sites; Strokes = strokes; Surface = new(sites, strokes); }
    }

    public sealed class Operation
    {
        public string Name { get; }
        public Component SourceScope { get; }
        public Component TargetScope { get; }
        public QualitativePatternTransform Transform { get; }
        public Relation DirectionMap { get; }
        public Relation Translation { get; }
        public Relation Anchor { get; }
        public Relation SiteMap { get; }
        public Relation EdgeMap { get; }
        public Relation Matched { get; }
        public Relation Unmatched { get; }
        /// <summary>Target participation not accounted for by this map. Open
        /// mapping obligations qualify whether it is definitely absent.</summary>
        public Relation Missing { get; }
        public Relation Open { get; }
        public Relation Geometry { get; }
        public Relation Surface { get; }
        public bool Exact => Empty(Unmatched) && Empty(Missing) && Empty(Open) &&
            E(SiteMap).Length == E(SourceScope.Sites).Length && E(SiteMap).Length == E(TargetScope.Sites).Length &&
            E(EdgeMap).Length == E(SourceScope.Strokes).Length && E(EdgeMap).Length == E(TargetScope.Strokes).Length &&
            E(EdgeMap).Length != 0;
        public bool NonTrivial { get; }
        public int MatchedEdges => E(EdgeMap).Length;
        public int MatchedSites => E(SiteMap).Length;

        internal Operation(string name, Component source, Component target, QualitativePatternTransform transform,
            Relation directions, Relation translation, Relation anchor, Relation sites, Relation edges, Relation matched,
            Relation unmatched, Relation missing, Relation open, Relation geometry, bool nonTrivial)
        {
            Name = name; SourceScope = source; TargetScope = target; Transform = transform; DirectionMap = directions;
            Translation = translation; Anchor = anchor; SiteMap = sites; EdgeMap = edges; Matched = matched;
            Unmatched = unmatched; Missing = missing; Open = open; Geometry = geometry; NonTrivial = nonTrivial;
            Surface = new(new Relation(source.Surface, target.Surface),
                new Relation(new Relation(new Relation(directions, transform.Surface), new Relation(translation, anchor)),
                    new Relation(new Relation(sites, edges), new Relation(matched,
                        new Relation(unmatched, new Relation(missing, new Relation(open, geometry)))))));
        }
    }

    public sealed class Read
    {
        public QualitativeMotionFrame Frame { get; }
        public NativeLineBasis Basis { get; }
        public Relation Port { get; }
        public IReadOnlyList<Component> Components { get; }
        public IReadOnlyList<Operation> Operations { get; }
        public Relation Unresolved { get; }
        public Relation Unsearched { get; }
        public Relation Surface { get; }
        internal Read(QualitativeMotionFrame frame, NativeLineBasis basis, Relation port,
            IReadOnlyList<Component> components, IReadOnlyList<Operation> operations, Relation unresolved, Relation unsearched)
        {
            Frame = frame; Basis = basis; Port = port; Components = components; Operations = operations;
            Unresolved = unresolved; Unsearched = unsearched;
            Surface = new(new Relation(port, basis.Surface), new Relation(S(components.Select(component => (IElement)component.Surface).ToArray()),
                new Relation(S(operations.Select(operation => (IElement)operation.Surface).ToArray()), new Relation(unresolved, unsearched))));
        }
    }

    private sealed record Stroke(Relation Row, IElement From, IElement To, Relation Word);
    private sealed record Position(Relation Binding, Relation Word);
    private sealed record Equality(string Answer, Relation Witness);

    public static Read Analyze(NativeLatticeSources.SourceCase source, int operationCapacity = 32,
        int translationCapacity = 12, int candidateCapacity = 256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(operationCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(translationCapacity);
        ArgumentOutOfRangeException.ThrowIfNegative(candidateCapacity);
        var frame = source.Graph.Frame;
        var basis = new NativeLineBasis(frame, source.TurnModel);
        var port = new Relation(new Relation(source.Graph.Surface, source.Bindings), source.StraightPassages);
        var unresolved = new List<IElement>(); var cuts = new List<IElement>();
        var rawSites = E(source.Graph.Sites); var rawStrokes = E(source.Graph.Strokes);
        foreach (var inventory in new[] { source.Graph.Sites, source.Graph.Strokes, source.Bindings, source.StraightPassages })
            if (!Closed(inventory)) unresolved.Add(inventory);
        var sites = rawSites.Distinct<IElement>(N.Participants.Instance).ToArray();
        if (sites.Length != rawSites.Length) unresolved.Add(source.Graph.Sites);
        var strokes = new List<Stroke>(); var incidence = new List<Stroke>();
        foreach (var item in rawStrokes)
        {
            if (item is not Relation row || !QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out var word) ||
                !sites.Any(site => N.Same(site, from)) || !sites.Any(site => N.Same(site, to)) || N.Same(from, to))
            { unresolved.Add(item); continue; }
            // Unknown motion does not erase an actual incidence relation.
            incidence.Add(new(row, from, to, word));
            if (!KnownWord(word)) { unresolved.Add(item); continue; }
            strokes.Add(new(row, from, to, word));
            if (!E(source.StraightPassages).Any(offered => N.Same(offered, row))) unresolved.Add(new Relation(row, source.StraightPassages));
        }
        var positions = new Dictionary<IElement, Position>(N.Participants.Instance);
        foreach (var site in sites)
        {
            var bindings = E(source.Bindings).OfType<Relation>().Where(row => N.Same(row.Left, site)).ToArray();
            if (!Closed(source.Bindings) || bindings is not [var binding] || binding.Right is not Relation word || !KnownWord(word))
            { unresolved.Add(new Relation(site, S(bindings.Cast<IElement>().ToArray()))); continue; }
            positions.Add(site, new(binding, Reduce(word)));
        }
        foreach (var stroke in strokes)
            if (positions.TryGetValue(stroke.From, out var fromPosition) && positions.TryGetValue(stroke.To, out var toPosition))
            {
                var expected = Join(fromPosition.Word, stroke.Word);
                var consistency = Compare(expected, toPosition.Word);
                if (consistency.Answer != "Yes")
                    unresolved.Add(new Relation(stroke.Row, new Relation(new Relation(fromPosition.Binding, toPosition.Binding), consistency.Witness)));
            }
        var components = Components(sites, incidence);
        var whole = new Component(-1, S(sites), S(incidence.Select(stroke => (IElement)stroke.Row).ToArray()));
        var reaches = positions.Values.SelectMany(position => E(position.Word)).Concat(strokes.SelectMany(stroke => E(stroke.Word)))
            .OfType<Relation>().Select(move => move.Right).Distinct<IElement>(N.Participants.Instance).ToArray();
        var directions = new[] { frame.Right, frame.Up, frame.Left, frame.Down };
        // Native action rows, not names or enum values, are the operational law.
        var actions = new[] {
            (Name: "translation", Map: new[] { 0, 1, 2, 3 }, Geometry: false),
            (Name: "half-turn", Map: new[] { 2, 3, 0, 1 }, Geometry: false),
            (Name: "quarter-turn", Map: new[] { 1, 2, 3, 0 }, Geometry: true),
            (Name: "three-quarter-turn", Map: new[] { 3, 0, 1, 2 }, Geometry: true),
            (Name: "mirror across vertical", Map: new[] { 2, 1, 0, 3 }, Geometry: true),
            (Name: "mirror across horizontal", Map: new[] { 0, 3, 2, 1 }, Geometry: true),
            (Name: "mirror across rising diagonal", Map: new[] { 1, 0, 3, 2 }, Geometry: true),
            (Name: "mirror across falling diagonal", Map: new[] { 3, 2, 1, 0 }, Geometry: true)
        };
        var retained = new List<Operation>(); var spent = 0;
        foreach (var action in actions)
        {
            var actionSpent = 0;
            var actionCapacity = candidateCapacity / actions.Length + (candidateCapacity % actions.Length == 0 ? 0 : 1);
            var directionMap = S(directions.Select((direction, index) => (IElement)new Relation(direction, directions[action.Map[index]])).ToArray());
            var rows = S(reaches.SelectMany(reach => directions.Select((direction, index) => (IElement)new Relation(
                QualitativeMotionFrame.Move(direction, reach), S(QualitativeMotionFrame.Move(directions[action.Map[index]], reach))))).ToArray());
            var transform = new QualitativePatternTransform(frame, frame, rows, new Relation(basis.Surface, directionMap));
            var geometry = action.Geometry ? basis.ReadRightAngle(new Relation(directionMap, basis.Surface)) : null;
            var mapped = new Dictionary<Relation, (Relation Word, Relation Witness)>(ReferenceEqualityComparer.Instance);
            var scopes = new List<(Component Source, Component Target)> { (whole, whole) };
            if (components.Count > 1)
                foreach (var a in components) foreach (var b in components) scopes.Add((a, b));
            foreach (var focus in scopes)
            {
                var fromSites = E(focus.Source.Sites); var toSites = E(focus.Target.Sites);
                var sourceAnchor = fromSites.FirstOrDefault(site => positions.ContainsKey(site));
                if (sourceAnchor is null)
                { unresolved.Add(new Relation(focus.Source.Surface, transform.Surface)); continue; }
                var anchorWord = Map(positions[sourceAnchor].Word);
                var offsets = new List<Relation>(); Operation? best = null; Operation? bestNonTrivial = null; var searchedTranslations = 0;
                foreach (var targetAnchor in toSites.Where(site => positions.ContainsKey(site)))
                {
                    var translation = Difference(positions[targetAnchor].Word, anchorWord.Word);
                    if (offsets.Any(prior => Compare(prior, translation).Answer == "Yes")) continue;
                    offsets.Add(translation);
                    var request = new Relation(new Relation(focus.Source.Surface, focus.Target.Surface),
                        new Relation(transform.Surface, new Relation(new Relation(sourceAnchor, targetAnchor), translation)));
                    if (searchedTranslations++ >= translationCapacity || spent >= candidateCapacity || actionSpent >= actionCapacity)
                    { cuts.Add(request); continue; }
                    spent++; actionSpent++;
                    var operation = Evaluate(focus.Source, focus.Target, translation,
                        new Relation(new Relation(sourceAnchor, targetAnchor), new Relation(anchorWord.Witness,
                            new Relation(positions[sourceAnchor].Binding, positions[targetAnchor].Binding))));
                    if (best is null || Better(operation, best)) best = operation;
                    if (operation.NonTrivial && operation.MatchedEdges != 0 && (bestNonTrivial is null || Better(operation, bestNonTrivial))) bestNonTrivial = operation;
                }
                if (best is not null) retained.Add(best);
                if (bestNonTrivial is not null && !ReferenceEquals(best, bestNonTrivial)) retained.Add(bestNonTrivial);
            }

            (Relation Word, Relation Witness) Map(Relation word)
            {
                if (mapped.TryGetValue(word, out var previous)) return previous;
                var read = transform.Apply(word);
                if (!transform.TryReadComplete(read, out var result)) throw new InvalidOperationException("A complete native signed-axis substitution failed its own source words.");
                var pair = (Reduce(result), read); mapped.Add(word, pair); return pair;
            }

            Operation Evaluate(Component from, Component to, Relation translation, Relation anchor)
            {
                var siteMaps = new List<IElement>(); var edgeMaps = new List<IElement>(); var matches = new List<IElement>();
                var unmatched = new List<IElement>(); var missing = new List<IElement>(); var open = new List<IElement>();
                open.AddRange(unresolved);
                var siteTargets = new Dictionary<IElement, IElement>(N.Participants.Instance);
                var failedSites = new HashSet<IElement>(N.Participants.Instance);
                var sourceSites = E(from.Sites); var targetSites = E(to.Sites);
                foreach (var site in sourceSites)
                {
                    if (!positions.TryGetValue(site, out var position)) { open.Add(site); continue; }
                    var image = Map(position.Word); var shifted = Join(image.Word, translation);
                    var candidates = new List<(IElement Site, Equality Read)>(); var possible = new List<IElement>();
                    foreach (var other in targetSites)
                    {
                        if (!positions.TryGetValue(other, out var targetPosition)) { possible.Add(other); continue; }
                        var comparison = Compare(shifted, targetPosition.Word);
                        if (comparison.Answer == "Yes") candidates.Add((other, comparison));
                        else if (comparison.Answer == "Open") possible.Add(new Relation(other, comparison.Witness));
                    }
                    if (candidates.Count == 1 && possible.Count == 0 && !siteTargets.Values.Any(value => N.Same(value, candidates[0].Site)))
                    {
                        var selected = candidates[0]; var row = new Relation(site, selected.Site);
                        siteTargets.Add(site, selected.Site); siteMaps.Add(row);
                        matches.Add(new Relation(row, new Relation(position.Binding, new Relation(image.Witness, selected.Read.Witness))));
                    }
                    else if (possible.Count != 0 || candidates.Count > 1)
                        open.Add(new Relation(site, new Relation(shifted, S(candidates.Select(candidate => (IElement)candidate.Site).Concat(possible).ToArray()))));
                    else { unmatched.Add(new Relation(site, shifted)); failedSites.Add(site); }
                }
                var selectedEdges = strokes.Where(stroke => E(from.Strokes).Contains(stroke.Row, ReferenceEqualityComparer.Instance)).ToArray();
                var targetEdges = strokes.Where(stroke => E(to.Strokes).Contains(stroke.Row, ReferenceEqualityComparer.Instance)).ToArray();
                var usedEdges = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
                foreach (var stroke in selectedEdges)
                {
                    if (!siteTargets.TryGetValue(stroke.From, out var imageFrom) || !siteTargets.TryGetValue(stroke.To, out var imageTo))
                    {
                        if (failedSites.Contains(stroke.From) || failedSites.Contains(stroke.To)) unmatched.Add(stroke.Row);
                        else open.Add(stroke.Row);
                        continue;
                    }
                    var expected = Map(stroke.Word); var candidates = new List<(Stroke Edge, bool Forward, Equality Read)>(); var pending = new List<IElement>();
                    foreach (var other in targetEdges)
                    {
                        var forward = N.Same(other.From, imageFrom) && N.Same(other.To, imageTo);
                        var reverse = N.Same(other.From, imageTo) && N.Same(other.To, imageFrom);
                        if (!forward && !reverse) continue;
                        var comparison = Compare(expected.Word, forward ? other.Word : UnitArrangementRead.Inverse(frame, other.Word));
                        if (comparison.Answer == "Yes") candidates.Add((other, forward, comparison));
                        else if (comparison.Answer == "Open") pending.Add(new Relation(other.Row, comparison.Witness));
                    }
                    if (candidates.Count == 1 && pending.Count == 0 && usedEdges.Add(candidates[0].Edge.Row))
                    {
                        var selected = candidates[0]; var row = new Relation(stroke.Row, selected.Edge.Row); edgeMaps.Add(row);
                        var ends = new Relation(imageFrom, imageTo);
                        matches.Add(new Relation(row, new Relation(ends, new Relation(expected.Witness, selected.Read.Witness))));
                    }
                    else if (pending.Count != 0 || candidates.Count > 1) open.Add(new Relation(stroke.Row, S(pending.Concat(candidates.Select(candidate => (IElement)candidate.Edge.Row)).ToArray())));
                    else unmatched.Add(new Relation(stroke.Row, expected.Word));
                }
                foreach (var site in targetSites.Where(site => !siteTargets.Values.Any(value => N.Same(value, site)))) missing.Add(site);
                foreach (var edge in targetEdges.Where(edge => !usedEdges.Contains(edge.Row))) missing.Add(edge.Row);
                foreach (var scope in new[] { source.Graph.Sites, source.Graph.Strokes, source.Bindings, source.StraightPassages })
                    if (!Closed(scope)) open.Add(scope);
                foreach (var edge in selectedEdges.Concat(targetEdges).Distinct())
                    if (!E(source.StraightPassages).Any(offered => N.Same(offered, edge.Row))) open.Add(new Relation(edge.Row, source.StraightPassages));
                if (geometry is not null && geometry.Answer != "Yes") open.Add(geometry.Surface);
                var geometryProof = geometry?.Surface ?? new Relation(basis.Surface, new Relation(directionMap, frame.Opposites));
                var trivial = action.Map.SequenceEqual(new[] { 0, 1, 2, 3 }) && Empty(translation) && ReferenceEquals(from, to);
                return new(action.Name, from, to, transform, directionMap, translation, anchor,
                    S(siteMaps.ToArray()), S(edgeMaps.ToArray()), S(matches.ToArray()), S(unmatched.ToArray()), S(missing.ToArray()), S(open.ToArray()), geometryProof, !trivial);
            }
        }
        var ordered = retained.OrderByDescending(operation => ReferenceEquals(operation.SourceScope, whole) && ReferenceEquals(operation.TargetScope, whole))
            .ThenByDescending(operation => operation.Exact).ThenByDescending(operation => operation.NonTrivial)
            .ThenByDescending(operation => operation.MatchedEdges).ThenByDescending(operation => operation.MatchedSites).ToArray();
        foreach (var operation in ordered.Skip(operationCapacity)) cuts.Add(new Relation(operation.SourceScope.Surface, new Relation(operation.TargetScope.Surface, operation.Transform.Surface)));
        return new(frame, basis, port, components, ordered.Take(operationCapacity).ToArray(), S(unresolved.ToArray()), S(cuts.ToArray()));

        bool KnownWord(Relation word) => Closed(word) && E(word).All(item => item is Relation move && frame.Direction(move, out _, out _, out _));
        Relation Reduce(Relation word)
        { frame.TryReadResult(frame.Reduce(S(), word), out var remaining, out _, out _); return remaining; }
        Relation Join(Relation a, Relation b) => Reduce(S(E(a).Concat(E(b)).ToArray()));
        Relation Difference(Relation a, Relation b) => Join(a, UnitArrangementRead.Inverse(frame, b));
        Equality Compare(Relation a, Relation b)
        {
            var witness = frame.Reduce(UnitArrangementRead.Inverse(frame, a), b);
            if (!frame.TryReadResult(witness, out var remaining, out _, out var pending) || !Closed(remaining)) return new("Open", witness);
            return new(Empty(remaining) ? "Yes" : Empty(pending) ? "No" : "Open", witness);
        }
    }

    private static bool Better(Operation candidate, Operation previous) => candidate.Exact && !previous.Exact ||
        candidate.Exact == previous.Exact && (candidate.MatchedEdges > previous.MatchedEdges ||
            candidate.MatchedEdges == previous.MatchedEdges && candidate.MatchedSites > previous.MatchedSites);

    private static IReadOnlyList<Component> Components(IElement[] sites, IReadOnlyList<Stroke> strokes)
    {
        var remaining = sites.ToHashSet<IElement>(N.Participants.Instance); var components = new List<Component>();
        foreach (var seed in sites)
        {
            if (!remaining.Remove(seed)) continue;
            var members = new List<IElement> { seed }; var edges = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            for (var index = 0; index < members.Count; index++)
                foreach (var stroke in strokes)
                {
                    if (!N.Same(stroke.From, members[index]) && !N.Same(stroke.To, members[index])) continue;
                    edges.Add(stroke.Row);
                    var other = N.Same(stroke.From, members[index]) ? stroke.To : stroke.From;
                    if (remaining.Remove(other)) members.Add(other);
                }
            components.Add(new(components.Count, S(members.ToArray()), S(edges.Cast<IElement>().ToArray())));
        }
        return components;
    }

    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var items, out _, out _) ? items : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static bool Empty(Relation scope) => Closed(scope) && E(scope).Length == 0;
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
