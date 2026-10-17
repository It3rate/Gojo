using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Unmeasured strokes joined only through actual shared sites. Chord
/// words declare simultaneous displacement; they are not cursor histories or
/// evidence that drawn intersections are material junctions.</summary>
public sealed class QualitativeStrokeGraph
{
    // Surface = frame.Surface -> ((Closed(actual-sites) -> Closed(stroke-rows)) -> period-word)
    // stroke-row = identity-or-cause -> ((from-site -> to-site) -> chord-word)
    // ray = actual-stroke-row -> (actual-site -> outward-chord-word)
    // junction = (Surface -> site) -> (rays -> (opposed -> (cooriented -> unresolved)))
    // pair = (actual-ray -> actual-ray) -> direction-pair-read
    // direction-pair-read = ((frame.Surface -> word-A) -> word-B) ->
    //                       (opposed -> (cooriented -> unresolved))
    // contour = (Surface -> Closed(ordered oriented edge participations)) -> additive-read
    // oriented participation = actual-stroke-row -> (from-site -> to-site)
    // winding = (actual-cycle -> period-word) -> additive-period-cancellation-read
    // read = Surface -> (junctions -> (contours -> (windings -> (cuts -> unresolved))))
    // map-read = ((Surface -> target.Surface) -> (site-map -> (stroke-map -> chord-map))) ->
    //            (matched-map-rows-and-pair-correspondences -> (unmatched -> unresolved))
    // A supplied chord correspondence is a declared premise. This structural
    // correspondence does not certify translation, reflection or metric isometry.
    public QualitativeMotionFrame Frame { get; }
    public Relation Sites { get; }
    public Relation Strokes { get; }
    public Relation PeriodWord { get; }
    public Relation Surface { get; }
    private Relation? read;
    public sealed record Summary(int Sites, int Strokes, int Endpoints, int Straight, int L,
        int T, int Y, int X, int Branches, int Isolated, int Contours, int Windings, int Cuts, int Unresolved)
    { public int O => Contours; }

    public QualitativeStrokeGraph(QualitativeMotionFrame frame, Relation sites, Relation strokes, Relation? periodWord = null)
    { Frame = frame; Sites = sites; Strokes = strokes; PeriodWord = periodWord ?? N.S(); Surface = new(frame.Surface, new Relation(new Relation(sites, strokes), PeriodWord)); }
    public static Relation Stroke(IElement identity, IElement from, IElement to, Relation chordWord) =>
        new(identity, new Relation(new Relation(from, to), chordWord));
    public static bool TryReadStroke(Relation stroke, out IElement identity, out IElement from, out IElement to, out Relation chordWord)
    {
        identity = from = to = null!; chordWord = null!;
        if (stroke.Right is not Relation { Left: Relation ends, Right: Relation word } || !N.Syntax(word)) return false;
        identity = stroke.Left; from = ends.Left; to = ends.Right; chordWord = word; return true;
    }

    public Relation Evaluate()
    {
        if (read is not null) return read;
        var pending = new List<IElement>(); var cuts = new List<IElement>(); var basis = Basis(pending, cuts);
        var junctions = new List<IElement>(); var contours = new List<IElement>(); var windings = new List<IElement>();
        var periodKnown = N.Read(PeriodWord, out var periodTerms) && periodTerms.Length != 0 && Profile(PeriodWord, []) is not null;
        foreach (var site in basis.Sites)
        {
            var rays = new List<IElement>(); var local = new List<IElement>(); var opposed = new List<IElement>(); var same = new List<IElement>();
            foreach (var edge in basis.Edges)
            {
                TryReadStroke(edge, out _, out var from, out var to, out var word);
                if (N.Same(from, site)) rays.Add(new Relation(edge, new Relation(site, word)));
                if (N.Same(to, site)) rays.Add(new Relation(edge, new Relation(site, Inverse(word))));
            }
            foreach (var ray in rays.Cast<Relation>()) Profile((Relation)((Relation)ray.Right).Right, local);
            for (var i = 0; i < rays.Count; i++) for (var j = i + 1; j < rays.Count; j++)
            {
                var direction = DirectionPair((Relation)((Relation)((Relation)rays[i]).Right).Right, (Relation)((Relation)((Relation)rays[j]).Right).Right);
                PairScopes(direction, out var opposite, out var parallel, out var unknown);
                var proof = new Relation(new Relation(rays[i], rays[j]), direction);
                if (N.E(unknown).Length != 0) local.Add(proof);
                else { if (N.E(opposite).Length != 0) opposed.Add(proof); if (N.E(parallel).Length != 0) same.Add(proof); }
            }
            if (!basis.Closed) local.Add(new Relation(site, new Relation(Sites, Strokes)));
            var junction = new Relation(new Relation(Surface, site), new Relation(N.Scope(basis.Closed, rays.ToArray()),
                new Relation(N.Scope(local.Count == 0, opposed.ToArray()), new Relation(N.Scope(local.Count == 0, same.ToArray()), N.S(local.ToArray())))));
            junctions.Add(junction); pending.AddRange(local);
        }
        // A spanning forest supplies explicit finite cycle witnesses, including
        // cycles with attached branches. This is a selected cycle basis, not an
        // exhaustive cycle count or a claim that every degree-two site is O.
        var visited = new HashSet<IElement>(N.Participants.Instance); var processed = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var parents = new Dictionary<IElement, (IElement Site, Relation Edge)>(N.Participants.Instance);
        foreach (var seed in basis.Sites)
        {
            if (!visited.Add(seed)) continue; var queue = new Queue<IElement>(); queue.Enqueue(seed);
            while (queue.TryDequeue(out var site)) foreach (var edge in basis.Edges)
            {
                TryReadStroke(edge, out _, out var from, out var to, out _);
                if (!N.Same(from, site) && !N.Same(to, site) || !processed.Add(edge)) continue;
                var next = N.Same(from, site) ? to : from;
                if (visited.Add(next)) { parents[next] = (site, edge); queue.Enqueue(next); continue; }
                var parts = new List<IElement> { new Relation(edge, new Relation(site, next)) };
                var ancestors = new HashSet<IElement>(N.Participants.Instance); var at = site; ancestors.Add(at);
                while (parents.TryGetValue(at, out var parent)) { at = parent.Site; ancestors.Add(at); }
                at = next;
                while (!ancestors.Contains(at)) { var parent = parents[at]; parts.Add(new Relation(parent.Edge, new Relation(at, parent.Site))); at = parent.Site; }
                var reverse = new List<IElement>(); var start = site;
                while (!N.Same(start, at)) { var parent = parents[start]; reverse.Add(new Relation(parent.Edge, new Relation(parent.Site, start))); start = parent.Site; }
                parts.AddRange(reverse.AsEnumerable().Reverse()); var moves = new List<IElement>(); var complete = true;
                foreach (var part in parts.Cast<Relation>())
                {
                    var ends = (Relation)part.Right; TryReadStroke((Relation)part.Left, out _, out var original, out _, out var word);
                    var oriented = N.Same(original, ends.Left) ? word : Inverse(word);
                    complete &= N.Read(oriented, out var terms); moves.AddRange(terms);
                }
                var closure = Frame.Reduce(N.S(), N.Scope(complete, moves.ToArray()));
                var contour = new Relation(new Relation(Surface, N.S(parts.ToArray())), closure);
                if (Zero(closure)) contours.Add(contour);
                else if (periodKnown &&
                    Frame.TryReadResult(closure, out var residual, out _, out _) &&
                    new[] { Frame.Reduce(residual, Inverse(PeriodWord)), Frame.Reduce(residual, PeriodWord) }.FirstOrDefault(Zero) is Relation quotient)
                    windings.Add(new Relation(new Relation(contour, PeriodWord), quotient));
                else pending.Add(contour);
            }
        }
        return read = new Relation(Surface, new Relation(N.S(junctions.ToArray()), new Relation(N.S(contours.ToArray()),
            new Relation(N.S(windings.ToArray()), new Relation(N.S(cuts.ToArray()), N.S(pending.ToArray()))))));
    }

    public bool TryReadResult(Relation candidate, out Relation junctions, out Relation contours, out Relation cuts, out Relation unresolved)
        => TryReadResult(candidate, out junctions, out contours, out _, out cuts, out unresolved);
    public bool TryReadResult(Relation candidate, out Relation junctions, out Relation contours, out Relation windings, out Relation cuts, out Relation unresolved)
    {
        junctions = contours = windings = cuts = unresolved = null!; var expected = Evaluate();
        if (!ReferenceEquals(candidate.Left, Surface) || !ReferenceEquals(candidate.Right, expected.Right) ||
            candidate.Right is not Relation { Left: Relation nodes, Right: Relation { Left: Relation loops, Right: Relation { Left: Relation wrapped,
                Right: Relation { Left: Relation boundary, Right: Relation pending } } } }) return false;
        junctions = nodes; contours = loops; windings = wrapped; cuts = boundary; unresolved = pending; return true;
    }
    public bool TryReadJunction(Relation junction, out IElement site, out Relation rays, out Relation opposedPairs, out Relation unresolved)
        => TryReadJunction(junction, out site, out rays, out opposedPairs, out _, out unresolved);
    public bool TryReadJunction(Relation junction, out IElement site, out Relation rays, out Relation opposedPairs, out Relation coorientedPairs, out Relation unresolved)
    {
        site = null!; rays = opposedPairs = coorientedPairs = unresolved = null!; TryReadResult(Evaluate(), out var nodes, out _, out _, out _);
        if (!N.E(nodes).Any(node => ReferenceEquals(node, junction)) || junction.Left is not Relation owner ||
            junction.Right is not Relation { Left: Relation actualRays, Right: Relation { Left: Relation pairs, Right: Relation { Left: Relation parallel, Right: Relation pending } } }) return false;
        site = owner.Right; rays = actualRays; opposedPairs = pairs; coorientedPairs = parallel; unresolved = pending; return true;
    }
    public Summary Describe(Relation candidate)
    {
        if (!TryReadResult(candidate, out var nodes, out var contours, out var windings, out var cuts, out var pending)) throw new ArgumentException("Expected this graph's native read.", nameof(candidate));
        var endpoint = 0; var straight = 0; var l = 0; var t = 0; var y = 0; var x = 0; var branches = 0; var isolated = 0;
        foreach (var node in N.E(nodes).Cast<Relation>())
        {
            TryReadJunction(node, out _, out var rays, out var opposed, out var unknown); if (!N.Read(rays, out var incidences)) continue;
            if (incidences.Length == 0) isolated++; if (incidences.Length == 1) endpoint++; if (incidences.Length >= 3) branches++;
            var same = (Relation)((Relation)((Relation)node.Right).Right).Right;
            if (N.E(unknown).Length != 0 || N.E((Relation)same.Left).Length != 0) continue;
            var pairs = N.E(opposed);
            if (incidences.Length == 2) { if (pairs.Length == 1) straight++; else l++; }
            if (incidences.Length == 3) { if (pairs.Length == 1) t++; else if (pairs.Length == 0) y++; }
            if (incidences.Length == 4 && pairs.Length == 2 && incidences.All(ray => pairs.Cast<Relation>().Count(pair =>
                pair.Left is Relation actual && (ReferenceEquals(actual.Left, ray) || ReferenceEquals(actual.Right, ray))) == 1)) x++;
        }
        var closedContours = N.E(contours).Cast<Relation>().Count(contour => contour.Right is Relation proof &&
            Frame.TryReadResult(proof, out var residual, out _, out var unknown) && N.E(residual).Length == 0 && N.E(unknown).Length == 0);
        return new(N.E(Sites).Distinct(N.Participants.Instance).Count(), N.E(Strokes).Length, endpoint, straight, l, t, y, x, branches, isolated,
            closedContours, N.E(windings).Length, N.E(cuts).Length, N.E(pending).Length);
    }

    public Relation EvaluateMap(QualitativeStrokeGraph target, Relation siteMap, Relation strokeMap, Relation chordMap)
    {
        var pending = new List<IElement>(); var cuts = new List<IElement>(); var source = Basis(pending, cuts); var destination = target.Basis(pending, cuts);
        var matched = new List<IElement>(); var no = new List<IElement>();
        var sites = Bijection(siteMap, source.Sites, destination.Sites, matched, no, pending);
        var strokes = Bijection(strokeMap, source.Edges.Cast<IElement>().ToArray(), destination.Edges.Cast<IElement>().ToArray(), [], no, pending);
        var sourceWords = source.Edges.Select(edge => { TryReadStroke(edge, out _, out _, out _, out var word); return (IElement)word; }).Distinct(N.Participants.Instance).ToArray();
        var targetWords = destination.Edges.Select(edge => { TryReadStroke(edge, out _, out _, out _, out var word); return (IElement)word; }).Distinct(N.Participants.Instance).ToArray();
        var words = Bijection(chordMap, sourceWords, targetWords, matched, no, pending);
        foreach (var row in strokes)
        {
            TryReadStroke((Relation)row.Left, out _, out var from, out var to, out var word); TryReadStroke((Relation)row.Right, out _, out var a, out var b, out var output);
            var start = sites.SingleOrDefault(map => N.Same(map.Left, from)); var end = sites.SingleOrDefault(map => N.Same(map.Left, to));
            var chord = words.SingleOrDefault(map => ReferenceEquals(map.Left, word));
            if (start is null || end is null || chord is null) pending.Add(row);
            else if (N.Same(start.Right, a) && N.Same(end.Right, b) && ReferenceEquals(chord.Right, output)) matched.Add(row);
            else no.Add(new Relation(row, new Relation(new Relation(start, end), chord)));
        }
        for (var i = 0; i < words.Length; i++) for (var j = i; j < words.Length; j++)
        {
            var a = words[i]; var b = words[j]; var before = DirectionPair((Relation)a.Left, (Relation)b.Left); var after = target.DirectionPair((Relation)a.Right, (Relation)b.Right);
            PairScopes(before, out var bo, out var bs, out var bu); PairScopes(after, out var ao, out var afterSame, out var au);
            var witness = new Relation(new Relation(a, b), new Relation(before, after));
            if (N.E(bu).Length != 0 || N.E(au).Length != 0) pending.Add(witness);
            else (N.E(bo).Length == N.E(ao).Length && N.E(bs).Length == N.E(afterSame).Length ? matched : no).Add(witness);
        }
        pending.AddRange(cuts);
        return new Relation(new Relation(new Relation(Surface, target.Surface), new Relation(siteMap, new Relation(strokeMap, chordMap))),
            new Relation(N.S(matched.ToArray()), new Relation(N.S(no.ToArray()), N.S(pending.ToArray()))));
    }
    public bool TryReadMap(Relation candidate, out Relation matched, out Relation unmatched, out Relation unresolved)
    {
        matched = unmatched = unresolved = null!;
        if (candidate.Left is not Relation { Left: Relation context, Right: Relation { Left: Relation, Right: Relation { Left: Relation, Right: Relation } } } ||
            !ReferenceEquals(context.Left, Surface) || candidate.Right is not Relation { Left: Relation yes, Right: Relation { Left: Relation no, Right: Relation pending } } ||
            !new[] { yes, no, pending }.All(scope => N.Read(scope, out _))) return false;
        matched = yes; unmatched = no; unresolved = pending; return true;
    }
    public bool TryReadMapPassed(QualitativeStrokeGraph target, Relation candidate)
    {
        if (!TryReadMap(candidate, out var matches, out var no, out var pending) || N.E(no).Length != 0 || N.E(pending).Length != 0 ||
            candidate.Left is not Relation { Left: Relation context, Right: Relation maps } || !ReferenceEquals(context.Right, target.Surface) ||
            maps.Left is not Relation sites || maps.Right is not Relation { Left: Relation strokes, Right: Relation chords }) return false;
        var fresh = EvaluateMap(target, sites, strokes, chords);
        return TryReadMap(fresh, out var expected, out var freshNo, out var freshPending) && N.E(freshNo).Length == 0 && N.E(freshPending).Length == 0 &&
            N.E(matches).Length == N.E(expected).Length && N.E(matches).Zip(N.E(expected)).All(pair => SameMapEvidence(pair.First, pair.Second, target));
    }

    private Relation DirectionPair(Relation first, Relation second)
    {
        var pending = new List<IElement>(); var a = Profile(first, pending); var b = Profile(second, pending);
        var opposed = new List<IElement>(); var same = new List<IElement>(); var proof = new Relation(Frame.Opposites, Frame.Reduce(first, second));
        if (a is not null && b is not null)
        {
            var axes = a.Select(term => term.Axis).Distinct().ToArray(); var others = b.Select(term => term.Axis).Distinct().ToArray();
            if (axes.Length == others.Length && axes.All(axis => others.Contains(axis)))
            {
                var faces = axes.Select(axis => a.First(term => ReferenceEquals(term.Axis, axis)).Forward == b.First(term => ReferenceEquals(term.Axis, axis)).Forward).ToArray();
                if (faces.All(face => face) || faces.All(face => !face))
                {
                    var parallel = SameTerms(a, b, !faces[0]) ? true : Parallel(a, b);
                    if (axes.Length == 1 || parallel == true) (faces[0] ? same : opposed).Add(proof);
                    else if (parallel is null) pending.Add(proof);
                }
            }
        }
        return new Relation(new Relation(new Relation(Frame.Surface, first), second), new Relation(N.S(opposed.ToArray()), new Relation(N.S(same.ToArray()), N.S(pending.ToArray()))));
    }
    private static bool PairScopes(Relation read, out Relation opposed, out Relation same, out Relation pending)
    {
        opposed = same = pending = null!;
        if (read.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation b, Right: Relation c } } || !new[] { a, b, c }.All(scope => N.Read(scope, out _))) return false;
        opposed = a; same = b; pending = c; return true;
    }
    private bool SameMapEvidence(IElement actual, IElement expected, QualitativeStrokeGraph target) => ReferenceEquals(actual, expected) ||
        actual is Relation { Left: Relation a, Right: Relation facts } && expected is Relation { Left: Relation b, Right: Relation wanted } &&
        ReferenceEquals(a.Left, b.Left) && ReferenceEquals(a.Right, b.Right) && facts.Left is Relation source && facts.Right is Relation destination &&
        wanted.Left is Relation wantedSource && wanted.Right is Relation wantedDestination && SamePair(source, wantedSource) && target.SamePair(destination, wantedDestination);
    private bool SamePair(Relation actual, Relation expected)
    {
        if (actual.Left is not Relation { Left: Relation a } header || expected.Left is not Relation { Left: Relation b } wanted ||
            !ReferenceEquals(a.Left, Frame.Surface) || !ReferenceEquals(a.Left, b.Left) || !ReferenceEquals(a.Right, b.Right) || !ReferenceEquals(header.Right, wanted.Right) ||
            !PairScopes(actual, out var opposite, out var same, out var pending) || !PairScopes(expected, out var eo, out var es, out var ep) ||
            N.E(opposite).Length != N.E(eo).Length || N.E(same).Length != N.E(es).Length || N.E(pending).Length != N.E(ep).Length) return false;
        return N.E(opposite).Concat(N.E(same)).All(item => item is Relation proof && ReferenceEquals(proof.Left, Frame.Opposites) && proof.Right is Relation reduction &&
            reduction.Left is Relation { Left: var frame, Right: Relation inputs } && ReferenceEquals(frame, Frame.Surface) && ReferenceEquals(inputs.Left, a.Right) &&
            ReferenceEquals(inputs.Right, header.Right) && Frame.TryReadResult(reduction, out _, out _, out _));
    }

    private (IElement[] Sites, Relation[] Edges, bool Closed) Basis(List<IElement> pending, List<IElement> cuts)
    {
        var closed = N.Read(Sites, out var rawSites) && N.Distinct(rawSites); var sites = rawSites.Distinct(N.Participants.Instance).ToArray();
        if (!closed) pending.Add(Sites); var strokesClosed = N.Read(Strokes, out var raw); closed &= strokesClosed;
        if (!strokesClosed) pending.Add(Strokes); var edges = new List<Relation>();
        if (!N.Read(PeriodWord, out var periodMoves) || periodMoves.Length != 0 && Profile(PeriodWord, []) is null) pending.Add(PeriodWord);
        foreach (var item in raw)
        {
            if (item is not Relation edge || edges.Any(actual => ReferenceEquals(actual, edge)) ||
                !TryReadStroke(edge, out _, out var from, out var to, out var word) || !sites.Any(site => N.Same(site, from)) || !sites.Any(site => N.Same(site, to)))
            { cuts.Add(item); pending.Add(item); closed = false; continue; }
            edges.Add(edge);
            if (!N.Read(word, out var moves) || moves.Length == 0 || moves.Any(move => move is not Relation term || !Frame.Direction(term, out _, out _, out _))) pending.Add(word);
        }
        return (sites, edges.ToArray(), closed);
    }
    private Relation Inverse(Relation word)
    {
        var terms = new List<IElement>(); var complete = N.Read(word, out var values);
        foreach (var value in values)
            if (value is Relation move && N.E(Frame.Opposites).OfType<Relation>().FirstOrDefault(pair => ReferenceEquals(pair.Left, move.Left)) is Relation opposite)
                terms.Add(QualitativeMotionFrame.Move(opposite.Right, move.Right));
            else { complete = false; terms.Add(value); }
        return N.Scope(complete, terms.ToArray());
    }
    private record Term(Relation Axis, bool Forward, IElement Reach);
    private Term[]? Profile(Relation word, List<IElement> pending)
    {
        var proof = Frame.Reduce(N.S(), word);
        if (!Frame.TryReadResult(proof, out var residual, out _, out var unknown) || !N.Read(residual, out var values) || values.Length == 0 || N.E(unknown).Length != 0)
        { pending.Add(proof); return null; }
        var terms = new List<Term>();
        foreach (var value in values)
            if (value is Relation move && Frame.Direction(move, out var axis, out var forward, out var reach)) terms.Add(new(axis, forward, reach));
            else { pending.Add(proof); return null; }
        return terms.ToArray();
    }
    private static bool SameTerms(Term[] a, Term[] b, bool opposite)
    {
        var remaining = b.ToList();
        foreach (var term in a)
        {
            var at = remaining.FindIndex(other => ReferenceEquals(term.Axis, other.Axis) && term.Forward == (opposite ? !other.Forward : other.Forward) && N.Same(term.Reach, other.Reach));
            if (at < 0) return false; remaining.RemoveAt(at);
        }
        return remaining.Count == 0;
    }
    // Shared repeated extents support an unmeasured ratio comparison: h+v and
    // h+v+v are distinct rays in every positive calibration of those same two
    // extents. Unrelated reach identities cannot justify that conclusion.
    private static bool? Parallel(Term[] a, Term[] b)
    {
        var axes = a.Select(term => term.Axis).Distinct().ToArray(); long firstA = 0, firstB = 0;
        foreach (var axis in axes)
        {
            var left = a.Where(term => ReferenceEquals(term.Axis, axis)).ToArray(); var right = b.Where(term => ReferenceEquals(term.Axis, axis)).ToArray();
            if (left.Concat(right).Any(term => !N.Same(term.Reach, left[0].Reach))) return null;
            if (firstA == 0) { firstA = left.Length; firstB = right.Length; }
            else if ((long)left.Length * firstB != (long)right.Length * firstA) return false;
        }
        return true;
    }
    private bool Zero(Relation proof) => Frame.TryReadResult(proof, out var residual, out _, out var unknown) &&
        N.Read(residual, out var values) && values.Length == 0 && N.E(unknown).Length == 0;
    private static Relation[] Bijection(Relation map, IElement[] source, IElement[] target, List<IElement> matched, List<IElement> no, List<IElement> pending)
    {
        if (!N.Read(map, out var raw)) pending.Add(map); var rows = new List<Relation>();
        foreach (var entry in raw)
            if (entry is Relation row && source.Any(item => N.Same(item, row.Left)) && target.Any(item => N.Same(item, row.Right))) rows.Add(row);
            else no.Add(new Relation(entry, map));
        var valid = new List<Relation>();
        foreach (var item in source)
        {
            var offered = rows.Where(row => N.Same(row.Left, item)).ToArray();
            if (offered.Length == 0) pending.Add(new Relation(item, map));
            else if (offered.Length != 1 || rows.Count(row => N.Same(row.Right, offered[0].Right)) != 1) no.Add(new Relation(item, N.S(offered)));
            else { valid.Add(offered[0]); matched.Add(offered[0]); }
        }
        foreach (var item in target) if (!rows.Any(row => N.Same(row.Right, item))) pending.Add(new Relation(map, item));
        return valid.ToArray();
    }
}
