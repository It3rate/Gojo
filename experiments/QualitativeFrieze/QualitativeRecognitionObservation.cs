using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A deliberately history-blind segment observation. Directions belong
/// to its own declared frame; no source position word, reach, face or generating
/// recipe is needed to read the topology. Open headings remain open evidence.</summary>
public sealed class QualitativeRecognitionObservation
{
    // segment = fresh identity -> ((from role -> to role) -> heading scope)
    // continuation = shared role -> (actual first segment -> actual second segment)
    // crossing = actual first segment -> actual second segment; no shared role
    // surface = (frame -> sites) -> (segments -> (continuations -> crossings))
    public QualitativeMotionFrame Frame { get; }
    public Relation Sites { get; }
    public Relation Segments { get; }
    public Relation Continuations { get; }
    public Relation Opposed => Continuations;
    public Relation Crossings { get; }
    public Relation Surface { get; }
    public Relation Unresolved { get; }
    public bool IsComplete => Entries(Unresolved).Length == 0;

    public QualitativeRecognitionObservation(QualitativeMotionFrame frame, Relation sites,
        Relation segments, Relation? continuations = null, Relation? crossings = null)
    {
        Frame = frame; Sites = sites; Segments = segments;
        Continuations = continuations ?? S(); Crossings = crossings ?? S();
        Surface = new(new Relation(frame.Surface, sites), new Relation(segments, new Relation(Continuations, Crossings)));
        var pending = new List<IElement>();
        if (!Closed(sites, out var siteRows) || !N.Distinct(siteRows)) pending.Add(sites);
        if (!Closed(segments, out var rows)) pending.Add(segments);
        var identities = new List<IElement>();
        foreach (var row in rows)
        {
            if (row is not Relation segment || !TryReadSegment(segment, out var identity, out _, out _, out var heading))
            { pending.Add(row); continue; }
            if (identities.Any(prior => N.Same(prior, identity))) pending.Add(segment);
            identities.Add(identity);
            if (!Closed(heading, out var directions) || directions.Length == 0) pending.Add(new Relation(segment, heading));
        }
        if (!Closed(Continuations, out var continuationsRows)) pending.Add(Continuations);
        foreach (var row in continuationsRows)
            if (row is not Relation continuation || !TryReadContinuation(continuation, out _, out _, out _)) pending.Add(row);
        if (!Closed(Crossings, out var crossingRows)) pending.Add(Crossings);
        foreach (var row in crossingRows)
            if (row is not Relation crossing || !TryReadCrossing(crossing, out _, out _)) pending.Add(row);
        Unresolved = S(pending.ToArray());
    }

    /// <summary>Reads a published segment, including a valid but open heading.
    /// A copied row is not the observed occurrence.</summary>
    public bool TryReadSegment(Relation row, out IElement identity, out IElement from,
        out IElement to, out Relation heading)
    {
        identity = from = to = null!; heading = null!;
        if (!Entries(Segments).Any(actual => ReferenceEquals(actual, row)) ||
            row.Right is not Relation { Left: Relation ends, Right: Relation directions } ||
            !Entries(Sites).Any(site => N.Same(site, ends.Left)) || !Entries(Sites).Any(site => N.Same(site, ends.Right)) ||
            N.Same(ends.Left, ends.Right) || !StructuralConstraint.TryReadScope(directions, out var dirs, out _, out _) ||
            dirs.Length > 2 || !N.Distinct(dirs) || dirs.Any(direction => !Known(direction)) ||
            dirs.Length == 2 && Opposites(dirs[0], dirs[1])) return false;
        identity = row.Left; from = ends.Left; to = ends.Right; heading = directions; return true;
    }

    public bool TryReadContinuation(Relation row, out IElement site, out Relation first, out Relation second)
    {
        site = null!; first = second = null!;
        if (!Entries(Continuations).Any(actual => ReferenceEquals(actual, row)) ||
            row.Right is not Relation { Left: Relation a, Right: Relation b } || ReferenceEquals(a, b) ||
            !TryReadSegment(a, out _, out var af, out var at, out _) || !TryReadSegment(b, out _, out var bf, out var bt, out _) ||
            !(N.Same(row.Left, af) || N.Same(row.Left, at)) || !(N.Same(row.Left, bf) || N.Same(row.Left, bt))) return false;
        site = row.Left; first = a; second = b; return true;
    }

    public bool TryReadCrossing(Relation row, out Relation first, out Relation second)
    {
        first = second = null!;
        if (!Entries(Crossings).Any(actual => ReferenceEquals(actual, row)) ||
            row.Left is not Relation a || row.Right is not Relation b || ReferenceEquals(a, b) ||
            !TryReadSegment(a, out _, out var af, out var at, out _) || !TryReadSegment(b, out _, out var bf, out var bt, out _) ||
            N.Same(af, bf) || N.Same(af, bt) || N.Same(at, bf) || N.Same(at, bt)) return false;
        first = a; second = b; return true;
    }

    private bool Known(IElement direction) => ReferenceEquals(direction, Frame.Right) || ReferenceEquals(direction, Frame.Left) ||
        ReferenceEquals(direction, Frame.Up) || ReferenceEquals(direction, Frame.Down);
    private bool Opposites(IElement a, IElement b) => Entries(Frame.Opposites).Cast<Relation>()
        .Any(pair => ReferenceEquals(pair.Left, a) && ReferenceEquals(pair.Right, b));
    internal static IElement[] Entries(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    internal static bool Closed(Relation scope, out IElement[] entries) =>
        StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail) && seam.Equals(tail);
    internal static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

/// <summary>Observation adapter and external provenance harness. Only Observation
/// is supplied to recognition. Maps and word-read witnesses deliberately remain
/// outside it, available to the evaluator and renderer.</summary>
public sealed class QualitativeRecognitionPublisher
{
    public QualitativeRecognitionObservation Observation { get; }
    public Relation SiteMap { get; }
    public Relation SegmentMap { get; }
    public Relation Reads { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => Observation.IsComplete && E(Blocked).Length == 0 && E(Unresolved).Length == 0;

    public QualitativeRecognitionPublisher(QualitativeMotionFrame sourceFrame, Relation topology, Relation bindings,
        Relation? declaredCrossings = null)
    {
        var frame = new QualitativeMotionFrame(); var siteMap = new List<IElement>(); var segmentMap = new List<IElement>();
        var reads = new List<IElement>(); var pending = new List<IElement>(); var blocked = new List<IElement>();
        var sites = new List<IElement>(); var segments = new List<IElement>(); var continuations = new List<IElement>(); var crossings = new List<IElement>();
        var sourceSites = Array.Empty<IElement>(); var edgeRows = Array.Empty<IElement>();
        var siteScope = topology.Left as Relation; var edgeScope = (topology.Right as Relation)?.Left as Relation;
        var closedSites = siteScope is not null && C(siteScope, out sourceSites);
        var closedEdges = edgeScope is not null && C(edgeScope, out edgeRows);
        var closedBindings = C(bindings, out var bindingRows);
        if (!closedSites) pending.Add(new Relation(topology, siteScope ?? StructuralConstraint.Open()));
        if (!closedEdges) pending.Add(new Relation(topology, edgeScope ?? StructuralConstraint.Open()));
        if (!closedBindings) pending.Add(bindings);
        var mapping = new Dictionary<IElement, IElement>(N.Participants.Instance);
        foreach (var site in sourceSites)
        {
            if (mapping.ContainsKey(site)) { blocked.Add(new Relation(site, topology)); continue; }
            var fresh = new Placeholder(); mapping.Add(site, fresh); sites.Add(fresh); siteMap.Add(new Relation(site, fresh));
        }
        var observed = new List<(IElement Edge, IElement From, IElement To, Relation Segment, Relation? Word)>();
        foreach (var edgeRow in edgeRows)
        {
            if (edgeRow is not Relation { Right: Relation ends } edge ||
                !mapping.TryGetValue(ends.Left, out var from) || !mapping.TryGetValue(ends.Right, out var to) ||
                N.Same(from, to) || observed.Any(item => N.Same(item.Edge, edge.Left)))
            { blocked.Add(new Relation(edgeRow, topology)); continue; }
            Relation heading = StructuralConstraint.Open(); Relation? displacement = null;
            var fromRows = bindingRows.OfType<Relation>().Where(row => N.Same(row.Left, ends.Left)).ToArray();
            var toRows = bindingRows.OfType<Relation>().Where(row => N.Same(row.Left, ends.Right)).ToArray();
            if (closedBindings && fromRows is [var fromBinding] && toRows is [var toBinding] &&
                fromBinding.Right is Relation fromWord && toBinding.Right is Relation toWord &&
                ValidWord(sourceFrame, fromWord) && ValidWord(sourceFrame, toWord))
            {
                var read = sourceFrame.Reduce(toWord, Inverse(sourceFrame, fromWord));
                reads.Add(new Relation(edge.Left, new Relation(new Relation(fromBinding, toBinding), read)));
                if (sourceFrame.TryReadResult(read, out var remaining, out _, out var unknown) && C(remaining, out var terms))
                {
                    var directions = new List<IElement>();
                    foreach (var direction in new IElement[] { sourceFrame.Right, sourceFrame.Left, sourceFrame.Up, sourceFrame.Down })
                        if (terms.Cast<Relation>().Any(move => ReferenceEquals(move.Left, direction)) &&
                            !terms.Cast<Relation>().Any(move => ReferenceEquals(move.Left, Opposite(sourceFrame, direction))))
                            directions.Add(MapDirection(sourceFrame, frame, direction));
                    if (E(unknown).Length == 0 && directions.Count > 0) { heading = S(directions.ToArray()); displacement = remaining; }
                    else { heading = StructuralConstraint.Open(directions.ToArray()); pending.Add(new Relation(edge.Left, read)); }
                }
                else pending.Add(new Relation(edge.Left, read));
            }
            else pending.Add(new Relation(edge.Left, new Relation(topology, bindings)));
            var segment = new Relation(new Placeholder(), new Relation(new Relation(from, to), heading));
            segments.Add(segment); segmentMap.Add(new Relation(edge.Left, segment));
            observed.Add((edge.Left, ends.Left, ends.Right, segment, displacement));
        }
        foreach (var site in sourceSites)
        {
            var incident = observed.Where(item => N.Same(item.From, site) || N.Same(item.To, site)).ToArray();
            for (var i = 0; i < incident.Length; i++)
                for (var j = i + 1; j < incident.Length; j++)
                {
                    var a = incident[i]; var b = incident[j];
                    if (a.Word is null || b.Word is null) continue;
                    var aw = N.Same(a.From, site) ? a.Word : Inverse(sourceFrame, a.Word);
                    var bw = N.Same(b.From, site) ? b.Word : Inverse(sourceFrame, b.Word);
                    if (!OppositeRays(sourceFrame, aw, bw)) continue;
                    var continuation = new Relation(mapping[site], new Relation(a.Segment, b.Segment));
                    continuations.Add(continuation);
                    reads.Add(new Relation(new Relation(site, new Relation(a.Edge, b.Edge)), new Relation(continuation, new Relation(aw, bw))));
                }
        }
        // Crossing declarations are an independent observation family. A failed
        // crossing claim cannot open an otherwise complete segment inventory.
        var topologyBlockedCount = blocked.Count;
        var inputCrossings = declaredCrossings ?? S();
        if (!C(inputCrossings, out var crossingRows)) pending.Add(inputCrossings);
        foreach (var row in crossingRows)
        {
            if (row is not Relation pair) { blocked.Add(row); continue; }
            var first = observed.SingleOrDefault(item => N.Same(item.Edge, pair.Left));
            var second = observed.SingleOrDefault(item => N.Same(item.Edge, pair.Right));
            if (first.Segment is null || second.Segment is null || ReferenceEquals(first.Segment, second.Segment) ||
                N.Same(first.From, second.From) || N.Same(first.From, second.To) || N.Same(first.To, second.From) || N.Same(first.To, second.To))
            { blocked.Add(row); continue; }
            if (crossings.OfType<Relation>().Any(prior =>
                ReferenceEquals(prior.Left, first.Segment) && ReferenceEquals(prior.Right, second.Segment) ||
                ReferenceEquals(prior.Left, second.Segment) && ReferenceEquals(prior.Right, first.Segment))) continue;
            var crossing = new Relation(first.Segment, second.Segment); crossings.Add(crossing); reads.Add(new Relation(row, crossing));
        }
        // Open source scopes publish an open boundary even when their known
        // prefix is useful. Face declarations are intentionally never consumed.
        Observation = new(frame, closedSites ? S(sites.ToArray()) : StructuralConstraint.Open(sites.ToArray()),
            closedEdges && topologyBlockedCount == 0 ? S(segments.ToArray()) : StructuralConstraint.Open(segments.ToArray()),
            S(continuations.ToArray()), C(inputCrossings, out _) ? S(crossings.ToArray()) : StructuralConstraint.Open(crossings.ToArray()));
        SiteMap = S(siteMap.ToArray()); SegmentMap = S(segmentMap.ToArray()); Reads = S(reads.ToArray());
        Blocked = S(blocked.ToArray()); Unresolved = S(pending.ToArray());
        Surface = new(new Relation(sourceFrame.Surface, new Relation(topology, bindings)),
            new Relation(Observation.Surface, new Relation(new Relation(SiteMap, SegmentMap), new Relation(Reads, new Relation(Blocked, Unresolved)))));
    }

    public bool TryMapSite(IElement source, out IElement observed)
    {
        var row = E(SiteMap).OfType<Relation>().SingleOrDefault(item => N.Same(item.Left, source));
        observed = row?.Right!; return row is not null;
    }
    public bool TryMapSegment(IElement source, out Relation observed)
    {
        var row = E(SegmentMap).OfType<Relation>().SingleOrDefault(item => N.Same(item.Left, source));
        observed = row?.Right as Relation ?? null!; return observed is not null;
    }

    private static bool ValidWord(QualitativeMotionFrame frame, Relation word) => C(word, out var terms) &&
        terms.All(item => item is Relation move && frame.Direction(move, out _, out _, out _));
    private static Relation Inverse(QualitativeMotionFrame frame, Relation word) => S(E(word).Reverse().Cast<Relation>()
        .Select(move => (IElement)QualitativeMotionFrame.Move(Opposite(frame, move.Left), move.Right)).ToArray());
    private static IElement Opposite(QualitativeMotionFrame frame, IElement direction) => E(frame.Opposites).Cast<Relation>()
        .Single(row => ReferenceEquals(row.Left, direction)).Right;
    private static IElement MapDirection(QualitativeMotionFrame source, QualitativeMotionFrame target, IElement direction) =>
        ReferenceEquals(direction, source.Right) ? target.Right : ReferenceEquals(direction, source.Left) ? target.Left :
        ReferenceEquals(direction, source.Up) ? target.Up : target.Down;

    // The selected formal-word law proves opposite rays, rather than inferring
    // a straight diagonal from NE and SW labels. Single-axis rays need no reach
    // comparison. For mixed axes every symbolic extent profile must have the
    // same opposite proportional multiplicity. Counts are a calculated read of
    // repeated participation, never supplied numeric measurements.
    private static bool OppositeRays(QualitativeMotionFrame frame, Relation first, Relation second)
    {
        var a = E(first).Cast<Relation>().ToArray(); var b = E(second).Cast<Relation>().ToArray();
        if (a.Length == 0 || b.Length == 0) return false;
        frame.Direction(a[0], out var axis, out var face, out _);
        if (a.All(move => frame.Direction(move, out var x, out var f, out _) && ReferenceEquals(x, axis) && f == face) &&
            b.All(move => frame.Direction(move, out var x, out var f, out _) && ReferenceEquals(x, axis) && f != face)) return true;
        var profiles = new List<(IElement Direction, IElement Reach, int A, int B)>();
        foreach (var move in a)
            if (!profiles.Any(item => ReferenceEquals(item.Direction, move.Left) && N.Same(item.Reach, move.Right)))
                profiles.Add((move.Left, move.Right,
                    a.Count(other => ReferenceEquals(other.Left, move.Left) && N.Same(other.Right, move.Right)),
                    b.Count(other => ReferenceEquals(other.Left, Opposite(frame, move.Left)) && N.Same(other.Right, move.Right))));
        if (profiles.Any(item => item.B == 0) || profiles.Sum(item => item.B) != b.Length) return false;
        var scale = profiles[0];
        return profiles.All(item => checked((long)item.A * scale.B) == checked((long)item.B * scale.A));
    }
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static bool C(Relation scope, out IElement[] entries) => QualitativeRecognitionObservation.Closed(scope, out entries);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}
