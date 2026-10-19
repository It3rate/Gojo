using System.Security.Cryptography;
using System.Text;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A non-draining rule read over an actual unit, execution and seam
/// quotient. The native witness retains its selected source, word reductions,
/// accepted obligations and failed/open participation. Common-reach integer
/// coordinates below are a separate derived diagnostic basis: they never add
/// a site, a join, a seam rule, or a measured length to the source.</summary>
public sealed class LatticeCatalogueAssessment
{
    // ((unit -> run) -> field) -> (frame -> (accepted -> (blocked -> (open -> diagnostics))))
    // diagnostic = ((frame -> common reach) -> actual binding rows) ->
    //              (actual intersecting stroke pairs -> actual same-support groups)
    public Relation Source { get; }
    public Relation Accepted { get; }
    public Relation Blocked { get; }
    public Relation Open { get; }
    public Relation Geometry { get; }
    public Relation Surface { get; }
    public LatticeCatalogueSummary Summary { get; }

    public static LatticeCatalogueAssessment Read(UnitArrangementProgram unit,
        OrnamentProgramRun run, OrnamentProgramField field) => new(unit, run, field);

    private LatticeCatalogueAssessment(UnitArrangementProgram unit, OrnamentProgramRun run, OrnamentProgramField field)
    {
        Source = new(new Relation(unit.Surface, run.Surface), field.Surface);
        var accepted = new List<IElement>(); var blocked = new List<IElement>(); var open = new List<IElement>();
        if (!ReferenceEquals(field.Run, run)) blocked.Add(new Relation(run.Surface, field.Run.Surface));
        if (!ReferenceEquals(unit.Frame, run.Program.Frame) || !ReferenceEquals(unit.Frame, field.Graph.Frame))
            blocked.Add(new Relation(unit.Frame.Surface, field.Graph.Frame.Surface));
        if (!N.E(run.Program.Motifs).Any(motif => ReferenceEquals(motif, unit.Surface)))
            blocked.Add(new Relation(unit.Surface, run.Program.Motifs));
        if (unit.Complete()) accepted.Add(new Relation(unit.Surface, new Relation(unit.Paths, unit.Ports)));
        else open.Add(unit.Surface);
        blocked.AddRange(N.E(run.Blocked)); blocked.AddRange(N.E(field.Blocked));
        open.AddRange(N.E(run.Unresolved)); open.AddRange(N.E(field.Unresolved));
        accepted.Add(new Relation(field.SeamContract, field.Accepted));

        var allWords = N.E(unit.Paths).OfType<Relation>().SelectMany(path =>
            (path.Right is Relation scope ? N.E(scope).OfType<Relation>() : [])
            .Concat(path.Left is Relation start ? [start] : [])).ToList();
        var bindings = N.E(field.Bindings).OfType<Relation>().ToArray();
        allWords.AddRange(bindings.Select(binding => binding.Right).OfType<Relation>());
        var reaches = allWords.SelectMany(N.E).OfType<Relation>().Select(move => move.Right)
            .Distinct(N.Participants.Instance).ToArray();
        var commonReach = reaches is [var reach] ? reach : null;
        if (commonReach is null) open.Add(new Relation(unit.Frame.Surface, N.S(reaches)));
        var basis = new Relation(unit.Frame.Surface, commonReach ?? N.S(reaches));
        var geometryPairs = new List<IElement>(); var histories = new List<IElement>();
        var motifSegments = new List<(Relation Path, Relation Word, Point From, Point To)>();
        var zeroChords = 0; var immediateRetraces = 0; var retracedPairs = 0; var motifChords = 0; var motifTerms = 0;
        foreach (var path in N.E(unit.Paths).OfType<Relation>())
        {
            if (path.Left is not Relation start || path.Right is not Relation scope)
            { open.Add(path); continue; }
            var words = N.E(scope).OfType<Relation>().ToArray();
            motifChords += words.Length; motifTerms += words.Sum(word => N.E(word).Length);
            var local = new List<(Relation Path, Relation Word, Point From, Point To)>();
            var readable = TryPoint(start, out var at);
            foreach (var word in words)
            {
                var reduction = unit.Frame.Reduce(N.S(), word);
                if (UnitArrangementRead.IsZero(unit.Frame, reduction))
                { zeroChords++; blocked.Add(new Relation(new Relation(path, word), reduction)); }
                else accepted.Add(new Relation(new Relation(path, word), reduction));
                if (!readable || !TryPoint(word, out var step)) { readable = false; open.Add(word); continue; }
                var segment = (Path: path, Word: word, From: at, To: at + step);
                local.Add(segment); motifSegments.Add(segment); at += step;
            }
            // A chord is one simultaneous displacement. Its internal R/U word
            // terms are not interpreted as a hidden stair-step cursor path.
            for (var a = 0; a < local.Count; a++) for (var b = a + 1; b < local.Count; b++)
            {
                var first = local[a]; var second = local[b];
                if (!Overlaps(first.From, first.To, second.From, second.To)) continue;
                retracedPairs++;
                if (b == a + 1 && Dot(first.To - first.From, second.To - second.From) < 0) immediateRetraces++;
                blocked.Add(new Relation(new Relation(path, new Relation(first.Word, second.Word)),
                    new Relation(basis, unit.Frame.Reduce(first.Word, second.Word))));
            }
        }

        var repeatDirections = 0; Relation repeatEvidence = N.S();
        if (run.Program.Layout.Right is Relation { Left: Relation stepSlots } &&
            N.E(run.Bindings).OfType<Relation>().Where(row => N.Same(row.Left, stepSlots.Left)).ToArray() is [{ Right: Relation u }] &&
            N.E(run.Bindings).OfType<Relation>().Where(row => N.Same(row.Left, stepSlots.Right)).ToArray() is [{ Right: Relation v }] &&
            TryPoint(u, out var pu) && TryPoint(v, out var pv))
        {
            repeatEvidence = new(new Relation(run.Program.Layout, run.Bindings), new Relation(u, v));
            repeatDirections = pu == default && pv == default ? 0 : Cross(pu, pv) == 0 ? 1 : 2;
            if (repeatDirections == 2) accepted.Add(new Relation(basis, repeatEvidence));
            else blocked.Add(new Relation(basis, repeatEvidence));
        }
        else open.Add(new Relation(run.Program.Layout, run.Bindings));

        var positions = new Dictionary<IElement, Point>(N.Participants.Instance);
        foreach (var binding in bindings)
        {
            if (binding.Right is not Relation word || !TryPoint(word, out var point)) { open.Add(binding); continue; }
            if (positions.TryGetValue(binding.Left, out var previous) && previous != point) blocked.Add(binding);
            else positions[binding.Left] = point;
        }
        var edges = new List<Edge>();
        var adjacency = new Dictionary<IElement, HashSet<IElement>>(N.Participants.Instance);
        foreach (var site in N.E(field.Graph.Sites)) adjacency.TryAdd(site, new(N.Participants.Instance));
        var strokes = N.E(field.Graph.Strokes);
        foreach (var item in strokes)
        {
            if (item is not Relation stroke || !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _) ||
                !positions.TryGetValue(from, out var a) || !positions.TryGetValue(to, out var b)) { open.Add(item); continue; }
            var same = edges.FirstOrDefault(edge => N.Same(edge.From, from) && N.Same(edge.To, to) || N.Same(edge.From, to) && N.Same(edge.To, from));
            if (same is not null) { histories.Add(new Relation(stroke, same.Stroke)); continue; }
            edges.Add(new(stroke, from, to, a, b));
            if (!adjacency.ContainsKey(from)) adjacency.Add(from, new(N.Participants.Instance));
            if (!adjacency.ContainsKey(to)) adjacency.Add(to, new(N.Participants.Instance));
            adjacency[from].Add(to); adjacency[to].Add(from);
        }
        var unjoinedCrossings = 0; var unjoinedContacts = 0; var overlaps = 0;
        for (var a = 0; a < edges.Count; a++) for (var b = a + 1; b < edges.Count; b++)
        {
            var first = edges[a]; var second = edges[b];
            if (Overlaps(first.A, first.B, second.A, second.B))
            { overlaps++; geometryPairs.Add(new Relation(first.Stroke, second.Stroke)); continue; }
            if (ProperCross(first.A, first.B, second.A, second.B))
            { unjoinedCrossings++; geometryPairs.Add(new Relation(first.Stroke, second.Stroke)); continue; }
            if (N.Same(first.From, second.From) || N.Same(first.From, second.To) || N.Same(first.To, second.From) || N.Same(first.To, second.To)) continue;
            if (Touches(first.A, first.B, second.A, second.B))
            { unjoinedContacts++; geometryPairs.Add(new Relation(first.Stroke, second.Stroke)); }
        }
        var visited = new HashSet<IElement>(N.Participants.Instance); var components = 0;
        foreach (var site in adjacency.Keys)
        {
            if (!visited.Add(site)) continue;
            components++; var queue = new Queue<IElement>(); queue.Enqueue(site);
            while (queue.TryDequeue(out var at)) foreach (var next in adjacency[at]) if (visited.Add(next)) queue.Enqueue(next);
        }
        Geometry = new(new Relation(basis, N.S(bindings)), new Relation(N.S(geometryPairs.ToArray()), N.S(histories.ToArray())));
        Accepted = N.S(accepted.ToArray()); Blocked = N.S(blocked.ToArray()); Open = N.S(open.Distinct(N.Participants.Instance).ToArray());
        Surface = new(Source, new Relation(unit.Frame.Surface, new Relation(Accepted, new Relation(Blocked, new Relation(Open, Geometry)))));
        var fingerprint = commonReach is not null && edges.Count > 0 && positions.Count == adjacency.Count
            ? Fingerprint(edges, positions, adjacency) : null;
        var degrees = adjacency.Values.Select(neighbours => neighbours.Count).ToArray();
        var directions = edges.Where(edge => edge.A != edge.B).Select(edge => Direction(edge.B - edge.A)).Distinct().Count();
        var spans = motifSegments.Select(segment => segment.To - segment.From).Where(vector => vector != default)
            .Select(vector => (Math.Abs(vector.X), Math.Abs(vector.Y))).Distinct().Count();
        var chordSpans = motifSegments.Select(segment => segment.To - segment.From)
            .Select(vector => Math.Abs(vector.X) + Math.Abs(vector.Y)).Where(span => span > 0).ToArray();
        Summary = new()
        {
            IsStructurallyAdmissible = unit.Complete() && run.IsComplete && field.IsComplete &&
                repeatDirections == 2 && zeroChords == 0 && retracedPairs == 0 && N.E(Blocked).Length == 0 && N.E(Open).Length == 0,
            ZeroChords = zeroChords, ImmediateRetraces = immediateRetraces, RetracedPathPairs = retracedPairs,
            MissingSeamObligations = N.E(field.Unresolved).Length, BlockedSeamObligations = N.E(field.Blocked).Length,
            FiniteFrontierObligations = N.E(field.Frontier).Length, IndependentRepeatDirections = repeatDirections,
            SourceStrokes = strokes.Length, UniqueJoinedSupports = edges.Count, SharedBoundaryHistories = histories.Count,
            JoinedBranchSites = degrees.Count(degree => degree >= 3), UnjoinedCrossings = unjoinedCrossings,
            UnjoinedContacts = unjoinedContacts, GeometricOverlaps = overlaps, Components = components,
            BoundaryEnds = degrees.Count(degree => degree == 1), MaxDegree = degrees.Length == 0 ? 0 : degrees.Max(),
            DegreeKinds = degrees.Distinct().Count(), MotifPaths = N.E(unit.Paths).Length, MotifChords = motifChords,
            MotifWordTerms = motifTerms, DistinctSupportDirections = directions, DistinctChordSpans = spans,
            Fingerprint = fingerprint, HasCommonReachDiagnostic = commonReach is not null,
            BlockedWitnesses = N.E(Blocked).Length, OpenWitnesses = N.E(Open).Length,
            SupportPerRepeatCell = run.Units.Count == 0 ? 0 : edges.Count / (double)run.Units.Count,
            BranchSiteFraction = degrees.Length == 0 ? 0 : degrees.Count(degree => degree >= 3) / (double)degrees.Length,
            CommonReachSpanRangeRatio = chordSpans.Length == 0 ? null : chordSpans.Max() / (double)chordSpans.Min()
        };

        bool TryPoint(Relation word, out Point result)
        {
            result = default;
            if (commonReach is null || !N.Read(word, out var terms)) return false;
            foreach (var item in terms)
            {
                if (item is not Relation move || !unit.Frame.Direction(move, out var axis, out var forward, out var actualReach) ||
                    !N.Same(actualReach, commonReach)) return false;
                var sign = forward ? 1 : -1;
                result += ReferenceEquals(axis, unit.Frame.Horizontal) ? new Point(sign, 0) : new Point(0, sign);
            }
            return true;
        }
    }

    private sealed record Edge(Relation Stroke, IElement From, IElement To, Point A, Point B);
    private readonly record struct Point(long X, long Y)
    {
        public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    }
    private static long Cross(Point a, Point b) => checked(a.X * b.Y - a.Y * b.X);
    private static long Dot(Point a, Point b) => checked(a.X * b.X + a.Y * b.Y);
    private static long Gcd(long a, long b) { a = Math.Abs(a); b = Math.Abs(b); while (b != 0) (a, b) = (b, a % b); return a; }
    private static Point Direction(Point vector)
    {
        var gcd = Gcd(vector.X, vector.Y); if (gcd == 0) return default;
        vector = new(vector.X / gcd, vector.Y / gcd);
        return vector.X < 0 || vector.X == 0 && vector.Y < 0 ? new(-vector.X, -vector.Y) : vector;
    }
    private static bool Overlaps(Point a, Point b, Point c, Point d)
    {
        if (a == b || c == d || Cross(b - a, d - c) != 0 || Cross(b - a, c - a) != 0) return false;
        return a.X != b.X ? Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X)) < Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X))
            : Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)) < Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y));
    }
    private static bool ProperCross(Point a, Point b, Point c, Point d) =>
        Opposed(Cross(b - a, c - a), Cross(b - a, d - a)) && Opposed(Cross(d - c, a - c), Cross(d - c, b - c));
    private static bool Opposed(long a, long b) => a < 0 && b > 0 || a > 0 && b < 0;
    private static bool On(Point a, Point b, Point p) => Cross(b - a, p - a) == 0 &&
        Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) && Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);
    private static bool Touches(Point a, Point b, Point c, Point d) => On(a, b, c) || On(a, b, d) || On(c, d, a) || On(c, d, b);

    /// <summary>A finite-window comparison read, invariant to translation,
    /// uniform positive integral reach scaling, stroke order, duplicate outline
    /// histories and straight degree-two subdivisions. Geometry and actual
    /// junction partitions are both retained; a crossing never becomes a join.
    /// This is not a general graph-isomorphism or infinite-tiling certificate.</summary>
    private static string Fingerprint(IReadOnlyList<Edge> edges, Dictionary<IElement, Point> positions,
        Dictionary<IElement, HashSet<IElement>> adjacency)
    {
        var nativeJunctions = new List<(Point Site, Point[] Rays)>();
        foreach (var pair in adjacency)
        {
            var at = positions[pair.Key];
            var rays = pair.Value.Select(site => positions[site] - at).Where(vector => vector != default).Select(vector =>
            { var step = Gcd(vector.X, vector.Y); return new Point(vector.X / step, vector.Y / step); }).Distinct().ToArray();
            if (rays is [var a, var b] && a.X == -b.X && a.Y == -b.Y) continue;
            nativeJunctions.Add((at, rays));
        }
        var merged = Merge(edges.Select(edge => (edge.A, edge.B))).ToArray();
        // Straight landmarks are excluded from the normalization basis as
        // well as the topology key. Subdividing a chord must not secretly
        // change its scale-normalized comparison coordinates.
        var retained = merged.SelectMany(edge => new[] { edge.A, edge.B }).Concat(nativeJunctions.Select(row => row.Site)).ToArray();
        var minX = retained.Min(point => point.X); var minY = retained.Min(point => point.Y);
        var gcd = retained.Aggregate(0L, (at, point) => Gcd(Gcd(at, point.X - minX), point.Y - minY));
        if (gcd == 0) gcd = 1;
        Point Normalize(Point point) => new((point.X - minX) / gcd, (point.Y - minY) / gcd);
        var support = new List<string>();
        foreach (var edge in merged)
        {
            var a = Normalize(edge.A); var b = Normalize(edge.B);
            var direction = Direction(b - a); var offset = Cross(direction, a);
            support.Add($"{direction.X},{direction.Y}:{offset}:{Math.Min(Dot(direction, a), Dot(direction, b))},{Math.Max(Dot(direction, a), Dot(direction, b))}");
        }
        var junctions = nativeJunctions.Select(row =>
        {
            var at = Normalize(row.Site);
            return $"{at.X},{at.Y}:[{string.Join(';', row.Rays.Select(ray => $"{ray.X},{ray.Y}").Order(StringComparer.Ordinal))}]";
        });
        var text = string.Join('|', support.Order(StringComparer.Ordinal)) + "#" + string.Join('|', junctions.Order(StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }

    private static IEnumerable<(Point A, Point B)> Merge(IEnumerable<(Point A, Point B)> source)
    {
        foreach (var line in source.Where(edge => edge.A != edge.B).GroupBy(edge =>
            { var direction = Direction(edge.B - edge.A); return (direction, offset: Cross(direction, edge.A)); }))
        {
            var intervals = line.Select(edge => Dot(line.Key.direction, edge.A) <= Dot(line.Key.direction, edge.B) ? edge : (A: edge.B, B: edge.A))
                .OrderBy(edge => Dot(line.Key.direction, edge.A)).ThenBy(edge => Dot(line.Key.direction, edge.B)).ToArray();
            var start = intervals[0].A; var end = intervals[0].B;
            foreach (var edge in intervals.Skip(1))
            {
                if (Dot(line.Key.direction, edge.A) <= Dot(line.Key.direction, end))
                { if (Dot(line.Key.direction, edge.B) > Dot(line.Key.direction, end)) end = edge.B; continue; }
                yield return (start, end); start = edge.A; end = edge.B;
            }
            yield return (start, end);
        }
    }
}

/// <summary>Calculated counts, not stored native values or a universal beauty
/// score. Crossings, disconnected motifs and overlap diagnostics are disclosed
/// separately; only authored retracing, zero chords, incomplete obligations and
/// missing independent repetition are mandatory rejection rules here.</summary>
public sealed record LatticeCatalogueSummary
{
    public bool IsStructurallyAdmissible { get; init; }
    public int ZeroChords { get; init; }
    public int ImmediateRetraces { get; init; }
    public int RetracedPathPairs { get; init; }
    public int MissingSeamObligations { get; init; }
    public int BlockedSeamObligations { get; init; }
    public int FiniteFrontierObligations { get; init; }
    public int IndependentRepeatDirections { get; init; }
    public int SourceStrokes { get; init; }
    public int UniqueJoinedSupports { get; init; }
    public int SharedBoundaryHistories { get; init; }
    public int JoinedBranchSites { get; init; }
    public int UnjoinedCrossings { get; init; }
    public int UnjoinedContacts { get; init; }
    public int GeometricOverlaps { get; init; }
    public int Components { get; init; }
    public int BoundaryEnds { get; init; }
    public int MaxDegree { get; init; }
    public int DegreeKinds { get; init; }
    public int MotifPaths { get; init; }
    public int MotifChords { get; init; }
    public int MotifWordTerms { get; init; }
    public int DistinctSupportDirections { get; init; }
    public int DistinctChordSpans { get; init; }
    public string? Fingerprint { get; init; }
    public bool HasCommonReachDiagnostic { get; init; }
    public int BlockedWitnesses { get; init; }
    public int OpenWitnesses { get; init; }
    /// <summary>Incidence-support density per sampled motif application;
    /// neither filled-area occupancy nor a measured screen density.</summary>
    public double SupportPerRepeatCell { get; init; }
    public double BranchSiteFraction { get; init; }
    /// <summary>Range of authored L1 word spans in the selected common reach.
    /// A diagnostic of spacing variety; not Euclidean length or beauty.</summary>
    public double? CommonReachSpanRangeRatio { get; init; }
}
