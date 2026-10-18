using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An opaque local supplier that lifts recognized rectangular circuits
/// into a coarse adjacency world. Public roles are fresh: private occurrences,
/// ordinal observations and full containment proofs remain in AuditSurface.
/// Contents are computed only when an exposed cell is explicitly queried.</summary>
public sealed class QualitativeMotifArrangement
{
    // private support = (actual occurrence -> fresh cell) -> rectangle read
    // public cell port = fresh cell -> fresh port
    // public adjacency = (macro axis -> ((cell A -> boundary port) ->
    //                                  (cell B -> boundary port))) -> (A -> B)
    // question = advertised prototype scope -> (cell role -> result role)
    // reply = ((cell -> port) -> (question -> revision)) ->
    //         (symbols -> (local alternatives -> unresolved))
    // A reply is a supplier assertion under this advertised contract. The
    // private proof correspondence is deliberately not traversable from it.
    public QualitativeMotionFrame Frame { get; } = new();
    public Relation Question { get; }
    public Relation PrototypeScope { get; }
    public Relation Chambers { get; }
    public Relation Nodes => Chambers;
    public Relation CellPorts { get; }
    public Relation Horizontal { get; }
    public Relation Vertical { get; }
    public Relation HorizontalPaths { get; }
    public Relation VerticalPaths { get; }
    public Relation Interface { get; }
    public Relation NodeBindings { get; }
    public Relation BoundaryBindings { get; }
    public Relation OtherViews { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public int ActualQueryCount => cache.Count;
    public int QueryCount => ActualQueryCount;
    public int CacheHits { get; private set; }
    public int RequestCount => requests.Count;
    public int FullContainmentReadCount { get; private set; }
    public int OrdinalExclusionCount { get; private set; }
    /// <summary>Supplier-only audit inventory. It contains actual finer
    /// occurrences and is deliberately excluded from Interface/PublicSurface.</summary>
    public Relation AuditCandidates { get; }
    public Relation AuditQueries => S(privateQueries.ToArray());
    public Relation PublicSurface => new(Interface, new Relation(S(cache.Values.Select(reply => (IElement)reply.Surface).ToArray()), S(requests.ToArray())));
    public Relation Surface => PublicSurface;
    public Relation AuditSurface => new(definition, new Relation(NodeBindings, new Relation(BoundaryBindings,
        new Relation(S(chambers.Select(chamber => (IElement)chamber.Support).ToArray()), new Relation(adjacencyProofs,
            new Relation(S(privateQueries.ToArray()), new Relation(OtherViews, new Relation(Blocked, new Relation(Unresolved, PublicSurface)))))))));
    public bool IsComplete => E(Blocked).Length == 0 && E(Unresolved).Length == 0 && C(Chambers, out _) && C(PrototypeScope, out _);

    private readonly QualitativeMotifInduction motifs;
    private readonly QualitativeRecognitionPlacement placement;
    private readonly Relation definition;
    private readonly Relation adjacencyProofs;
    private readonly List<Chamber> chambers = [];
    private readonly List<Candidate> candidates = [];
    private readonly Dictionary<IElement, QualitativeMotifCellReply> cache = new(N.Participants.Instance);
    private readonly List<IElement> privateQueries = [];
    private readonly List<IElement> requests = [];
    private readonly bool catalogueComplete;
    private readonly Dictionary<IElement, int>? horizontalIndex;
    private readonly Dictionary<IElement, int>? verticalIndex;

    public QualitativeMotifArrangement(QualitativeMotifInduction motifs, QualitativeRecognitionPlacement placement,
        Relation? chamberOccurrenceSelection = null, Relation? symbolFamilySelection = null)
    {
        this.motifs = motifs; this.placement = placement;
        var pending = new List<IElement>(); var blocked = new List<IElement>(); var other = new List<IElement>();
        var familySelection = symbolFamilySelection ?? S(motifs.Families.Select(family => (IElement)family.Read).ToArray());
        var familyClosed = C(familySelection, out var selectedFamilies); var families = new List<QualitativeMotifFamily>();
        foreach (var item in selectedFamilies)
        {
            var family = motifs.Families.FirstOrDefault(actual => ReferenceEquals(actual.Read, item) || ReferenceEquals(actual.Definition, item));
            if (family is null || !motifs.TryReadFamily(family.Read, out _) || families.Contains(family))
            { blocked.Add(item); continue; }
            families.Add(family);
            candidates.AddRange(family.Occurrences.Select(occurrence => new Candidate(family, occurrence,
                E(occurrence.Sites), CanCertifyEndpoints(occurrence))));
        }
        AuditCandidates = S(candidates.Select(candidate => (IElement)new Relation(candidate.Family.Read, candidate.Occurrence.Read)).ToArray());
        horizontalIndex = CompileOrder(placement.HorizontalBuckets);
        verticalIndex = CompileOrder(placement.VerticalBuckets);
        catalogueComplete = familyClosed && motifs.IsComplete && blocked.Count == 0;
        PrototypeScope = catalogueComplete ? S(families.Select(family => (IElement)family.Definition).ToArray()) :
            StructuralConstraint.Open(families.Select(family => (IElement)family.Definition).ToArray());
        Question = new(PrototypeScope, new Relation(new Placeholder(), S(new Placeholder())));
        var available = motifs.Families.Where(family => motifs.TryReadPattern(family.Definition, out var kind, out _) &&
            kind == "cycle" && family.SegmentCount == 4).SelectMany(family => family.Occurrences.Select(occurrence => (Family: family, Occurrence: occurrence))).ToArray();
        var selection = chamberOccurrenceSelection ?? S(available.Select(item => (IElement)item.Occurrence.Read).ToArray());
        var selectionClosed = C(selection, out var selectedChambers);
        if (!familyClosed) pending.Add(familySelection);
        if (!selectionClosed) pending.Add(selection);
        if (!ReferenceEquals(motifs.Observation.Surface, placement.Observation.Surface))
            blocked.Add(new Relation(motifs.Observation.Surface, placement.Observation.Surface));
        if (blocked.Count == 0)
            foreach (var item in selectedChambers)
            {
                var matches = available.Where(actual => ReferenceEquals(actual.Occurrence.Read, item)).ToArray();
                if (matches is not [var match] || chambers.Any(chamber => ReferenceEquals(chamber.Occurrence.Read, item)))
                { blocked.Add(item); continue; }
                var check = placement.ReadContainment(match.Occurrence.Segments, match.Occurrence.Segments);
                if (E(check.Blocked).Length != 0) { other.Add(new Relation(item, check.Surface)); continue; }
                if (E(check.Unresolved).Length != 0 || E(check.TouchingSites).Length != 4 ||
                    !Bounds(check.Rectangle, out var low, out var high))
                { pending.Add(new Relation(item, check.Surface)); continue; }
                var node = new Placeholder(); var port = new Placeholder();
                chambers.Add(new(node, port, match.Family, match.Occurrence, check, low, high,
                    new Relation(new Relation(item, node), check.Surface)));
            }
        if (chamberOccurrenceSelection is null && !motifs.IsComplete) pending.Add(motifs.Unsearched);
        var chamberComplete = selectionClosed && pending.Count == 0 && blocked.Count == 0;
        Chambers = chamberComplete ? S(chambers.Select(chamber => chamber.Node).ToArray()) :
            StructuralConstraint.Open(chambers.Select(chamber => chamber.Node).ToArray());
        CellPorts = chamberComplete ? S(chambers.Select(chamber => (IElement)new Relation(chamber.Node, chamber.Port)).ToArray()) :
            StructuralConstraint.Open(chambers.Select(chamber => (IElement)new Relation(chamber.Node, chamber.Port)).ToArray());
        NodeBindings = S(chambers.Select(chamber => (IElement)new Relation(chamber.Occurrence.Read, chamber.Node)).ToArray());
        var horizontal = new List<IElement>(); var vertical = new List<IElement>(); var proofs = new List<IElement>();
        var boundaries = new Dictionary<IElement, IElement>(N.Participants.Instance);
        for (var i = 0; i < chambers.Count; i++) for (var j = i + 1; j < chambers.Count; j++)
        {
            var a = chambers[i]; var b = chambers[j];
            var shared = E(a.Occurrence.Segments).Where(edge => E(b.Occurrence.Segments).Any(otherEdge => ReferenceEquals(edge, otherEdge))).ToArray();
            if (shared.Length == 0) continue;
            if (Adjacent(a, b, true, out var h)) Connect(a, b, shared, h, true);
            else if (Adjacent(b, a, true, out h)) Connect(b, a, shared, h, true);
            else if (Adjacent(a, b, false, out var v)) Connect(a, b, shared, v, false);
            else if (Adjacent(b, a, false, out v)) Connect(b, a, shared, v, false);
            // Shared participation without the opposed-side ordinal relation
            // remains overlapping/alternative structure, not a neighbour claim.
        }
        Horizontal = chamberComplete ? S(horizontal.ToArray()) : StructuralConstraint.Open(horizontal.ToArray());
        Vertical = chamberComplete ? S(vertical.ToArray()) : StructuralConstraint.Open(vertical.ToArray());
        var horizontalPaths = Paths(Horizontal); var verticalPaths = Paths(Vertical);
        HorizontalPaths = chamberComplete ? horizontalPaths : StructuralConstraint.Open(E(horizontalPaths));
        VerticalPaths = chamberComplete ? verticalPaths : StructuralConstraint.Open(E(verticalPaths));
        BoundaryBindings = S(boundaries.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray());
        adjacencyProofs = S(proofs.ToArray()); OtherViews = S(other.ToArray()); Blocked = S(blocked.ToArray()); Unresolved = S(pending.ToArray());
        var service = new Placeholder();
        Interface = new(new Relation(service, Question), new Relation(CellPorts, new Relation(
            new Relation(Frame.Horizontal, new Relation(Horizontal, HorizontalPaths)), new Relation(Frame.Vertical, new Relation(Vertical, VerticalPaths)))));
        definition = new(new Relation(motifs.Surface, placement.Surface), new Relation(new Relation(selection, familySelection), Interface));

        bool Adjacent(Chamber left, Chamber right, bool horizontalDirection, out Relation evidence)
        {
            var axis = horizontalDirection ? placement.Observation.Frame.Horizontal : placement.Observation.Frame.Vertical;
            var otherAxis = horizontalDirection ? placement.Observation.Frame.Vertical : placement.Observation.Frame.Horizontal;
            var seam = placement.Compare(axis, left.High, right.Low);
            var order = placement.Compare(axis, left.Low, right.Low);
            var low = placement.Compare(otherAxis, left.Low, right.Low);
            var high = placement.Compare(otherAxis, left.High, right.High);
            evidence = S(seam, order, low, high);
            return Equal(seam) && Before(order) && Equal(low) && Equal(high);
        }
        bool Equal(Relation read) => placement.TryReadComparison(read, out _, out var equal, out _, out var unknown) && E(equal).Length == 1 && E(unknown).Length == 0;
        bool Before(Relation read) => placement.TryReadComparison(read, out var before, out _, out _, out var unknown) && E(before).Length == 1 && E(unknown).Length == 0;
        void Connect(Chamber a, Chamber b, IElement[] shared, Relation evidence, bool horizontalDirection)
        {
            var boundaryPorts = shared.Select(edge =>
            {
                if (!boundaries.TryGetValue(edge, out var port)) boundaries.Add(edge, port = new Placeholder());
                return port;
            }).ToArray();
            var participation = S(boundaryPorts.Select(port => (IElement)new Relation(new Relation(a.Node, port), new Relation(b.Node, port))).ToArray());
            var link = new Relation(new Relation(horizontalDirection ? Frame.Horizontal : Frame.Vertical, participation), new Relation(a.Node, b.Node));
            (horizontalDirection ? horizontal : vertical).Add(link);
            proofs.Add(new Relation(link, new Relation(S(shared), evidence)));
        }
    }

    /// <summary>Private audit correspondence for an actual exposed cell. This
    /// method is for the supplier/evaluator; its result is not a public port.</summary>
    public Relation Support(IElement cell) => chambers.SingleOrDefault(chamber => N.Same(chamber.Node, cell))?.Support ?? StructuralConstraint.Open();
    public bool TryNodeForOccurrence(IElement occurrence, out IElement node)
    {
        node = chambers.SingleOrDefault(chamber => ReferenceEquals(chamber.Occurrence.Read, occurrence))?.Node!; return node is not null;
    }
    public IElement NodeForOccurrence(IElement occurrence) => TryNodeForOccurrence(occurrence, out var node) ? node :
        throw new ArgumentException("The supplied occurrence has no accepted chamber port.", nameof(occurrence));

    /// <summary>Explicit local refinement. Repeated reads return the actual
    /// cached reply, retaining a public route back to that earlier response.
    /// Symbols are prototypes, not desired answers injected by the caller.</summary>
    public QualitativeMotifCellReply ReadCell(IElement cell)
    {
        var chamber = chambers.SingleOrDefault(candidate => N.Same(candidate.Node, cell));
        if (chamber is null)
        {
            var rejectedCell = new Placeholder(); var port = new Placeholder(); var pending = S(new Relation(rejectedCell, Question));
            var rejected = Reply(rejectedCell, port, StructuralConstraint.Open(), S(), S(), pending);
            privateQueries.Add(new Relation(cell, rejected.Surface)); Register(rejected, S()); return rejected;
        }
        if (cache.TryGetValue(cell, out var existing)) { CacheHits++; Register(existing, S(existing.Surface)); return existing; }
        var supported = new List<(QualitativeMotifFamily Family, QualitativeMotifOccurrence Occurrence, QualitativeRecognitionContainment Read)>();
        var proofs = new List<IElement>(); var exclusions = new List<IElement>(); var unknown = new List<IElement>();
        foreach (var candidate in candidates)
        {
            if (TryExclude(chamber, candidate, out var exclusion))
            { exclusions.Add(exclusion); OrdinalExclusionCount++; continue; }
            var check = placement.ReadContainment(chamber.Occurrence.Segments, candidate.Occurrence.Segments);
            FullContainmentReadCount++;
            proofs.Add(new Relation(new Relation(candidate.Family.Read, candidate.Occurrence.Read), check.Surface));
            if (check.IsStrictlyInside) supported.Add((candidate.Family, candidate.Occurrence, check));
            else if (E(check.OutsideSites).Length == 0 && E(check.TouchingSites).Length == 0 &&
                (E(check.Unresolved).Length != 0 || E(check.Blocked).Length != 0)) unknown.Add(check.Surface);
        }
        var prototypes = supported.Select(item => (IElement)item.Family.Definition).Distinct(N.Participants.Instance).ToArray();
        var alternatives = S(supported.Select(item => (IElement)new Relation(new Placeholder(), item.Family.Definition)).ToArray());
        // The advertised question is one unit reading per chamber. Several
        // actual occurrences are preserved as alternatives; none wins by order.
        var complete = catalogueComplete && unknown.Count == 0;
        var symbols = complete ? S(prototypes) : StructuralConstraint.Open(prototypes);
        var competing = supported.Count > 1 ? S(new Relation(chamber.Node, alternatives)) : S();
        var pendingPublic = complete ? S() : S(new Relation(chamber.Port, new Placeholder()));
        var reply = Reply(chamber.Node, chamber.Port, symbols, alternatives, competing, pendingPublic);
        cache.Add(chamber.Node, reply);
        privateQueries.Add(new Relation(new Relation(reply.Surface, chamber.Support), new Relation(AuditCandidates,
            new Relation(S(proofs.ToArray()), new Relation(S(exclusions.ToArray()), new Relation(S(unknown.ToArray()),
                motifs.IsComplete ? S() : motifs.Unsearched))))));
        Register(reply, S()); return reply;
    }

    /// <summary>Read the actual supplier audit for one computed cell. Every
    /// selected candidate has either a complete containment read or a witnessed
    /// noninterior endpoint. Search incompleteness remains an independent tail.</summary>
    public bool TryReadQueryAudit(IElement cell, out Relation inventory, out Relation checks,
        out Relation exclusions, out Relation unresolved, out Relation unsearched)
    {
        inventory = checks = exclusions = unresolved = unsearched = null!;
        if (!cache.TryGetValue(cell, out var reply)) return false;
        var actual = privateQueries.OfType<Relation>().SingleOrDefault(row => row.Left is Relation header && ReferenceEquals(header.Left, reply.Surface));
        if (actual?.Right is not Relation { Left: Relation all, Right: Relation { Left: Relation full,
                Right: Relation { Left: Relation omitted, Right: Relation { Left: Relation unknown, Right: Relation frontier } } } }) return false;
        inventory = all; checks = full; exclusions = omitted; unresolved = unknown; unsearched = frontier; return true;
    }

    private bool TryExclude(Chamber chamber, Candidate candidate, out Relation exclusion)
    {
        exclusion = null!;
        if (!candidate.CertifiedEndpoints) return false;
        foreach (var point in candidate.Sites)
        {
            if (Noninterior(horizontalIndex, placement.Observation.Frame.Horizontal, point, out var comparison) ||
                Noninterior(verticalIndex, placement.Observation.Frame.Vertical, point, out comparison))
            {
                exclusion = new(new Relation(candidate.Family.Read, candidate.Occurrence.Read),
                    new Relation(chamber.Validation.Rectangle, new Relation(point, comparison)));
                return true;
            }
        }
        return false;

        bool Noninterior(Dictionary<IElement, int>? index, IElement axis, IElement point, out Relation comparison)
        {
            comparison = null!;
            if (index is null || !index.TryGetValue(point, out var at) || !index.TryGetValue(chamber.Low, out var low) ||
                !index.TryGetValue(chamber.High, out var high)) return false;
            // The compiled bucket index only selects a useful local question.
            // It cannot certify a result or bypass an inconsistent/open axis.
            if (at <= low) comparison = placement.Compare(axis, point, chamber.Low);
            else if (at >= high) comparison = placement.Compare(axis, chamber.High, point);
            else return false;
            return placement.TryReadComparison(comparison, out var before, out var equal, out _, out var unknown) &&
                E(unknown).Length == 0 && (E(before).Length == 1 || E(equal).Length == 1);
        }
    }

    private bool CanCertifyEndpoints(QualitativeMotifOccurrence occurrence)
    {
        if (!C(motifs.Observation.Segments, out var segments) || !C(motifs.Observation.Sites, out var sites)) return false;
        foreach (var item in E(occurrence.Segments))
        {
            if (item is not Relation row || !motifs.Observation.TryReadSegment(row, out var identity, out var from, out var to, out _) ||
                segments.OfType<Relation>().Count(other => N.Same(other.Left, identity)) != 1 ||
                sites.Count(site => N.Same(site, from)) != 1 || sites.Count(site => N.Same(site, to)) != 1) return false;
        }
        return true;
    }
    private static Dictionary<IElement, int>? CompileOrder(Relation buckets)
    {
        if (!C(buckets, out var rows)) return null;
        var result = new Dictionary<IElement, int>(N.Participants.Instance);
        for (var index = 0; index < rows.Length; index++)
        {
            if (rows[index] is not Relation bucket || !C(bucket, out var sites)) return null;
            foreach (var site in sites) if (!result.TryAdd(site, index)) return null;
        }
        return result;
    }

    private QualitativeMotifCellReply Reply(IElement cell, IElement port, Relation symbols, Relation alternatives, Relation competing, Relation unresolved)
    {
        var revision = new Placeholder(); var contract = new Relation(new Relation(cell, port), new Relation(Question, revision));
        var evidence = new Relation(contract, new Relation(alternatives, new Relation(competing, unresolved)));
        var surface = new Relation(contract, new Relation(symbols, new Relation(evidence, new Relation(competing, unresolved))));
        return new(cell, port, symbols, evidence, competing, unresolved, surface);
    }
    private void Register(QualitativeMotifCellReply reply, Relation cachedOrigin) => requests.Add(new Relation(new Placeholder(),
        new Relation(new Relation(reply.Cell, reply.Port), new Relation(reply.Surface, cachedOrigin))));
    private static bool Bounds(Relation rectangle, out IElement low, out IElement high)
    {
        low = high = null!;
        if (rectangle.Right is not Relation { Left: Relation { Right: Relation x }, Right: Relation { Right: Relation y } } ||
            !N.Same(x.Left, y.Left) || !N.Same(x.Right, y.Right)) return false;
        low = x.Left; high = x.Right; return true;
    }
    private Relation Paths(Relation links)
    {
        var nodes = chambers.Select(chamber => chamber.Node).ToArray(); var entries = E(links).Cast<Relation>().ToArray();
        var outgoing = nodes.ToDictionary(node => node, node => entries.Where(link => link.Right is Relation ends && N.Same(ends.Left, node)).ToArray(), N.Participants.Instance);
        var incoming = nodes.ToDictionary(node => node, node => entries.Where(link => link.Right is Relation ends && N.Same(ends.Right, node)).ToArray(), N.Participants.Instance);
        var visited = new HashSet<Relation>(ReferenceEqualityComparer.Instance); var paths = new List<IElement>();
        foreach (var node in nodes.Where(node => incoming[node].Length != 1 || outgoing[((Relation)incoming[node][0].Right).Left].Length != 1).Concat(nodes))
            foreach (var first in outgoing[node])
            {
                if (!visited.Add(first)) continue;
                var path = new List<IElement> { node }; var current = first;
                while (true)
                {
                    var next = ((Relation)current.Right).Right; path.Add(next);
                    if (incoming[next].Length != 1 || outgoing[next].Length != 1 || !visited.Add(outgoing[next][0])) break;
                    current = outgoing[next][0];
                }
                paths.Add(S(path.ToArray()));
            }
        paths.AddRange(nodes.Where(node => incoming[node].Length == 0 && outgoing[node].Length == 0).Select(node => (IElement)S(node)));
        return S(paths.ToArray());
    }
    private sealed class Chamber(IElement node, IElement port, QualitativeMotifFamily family,
        QualitativeMotifOccurrence occurrence, QualitativeRecognitionContainment validation, IElement low, IElement high, Relation support)
    {
        public IElement Node { get; } = node; public IElement Port { get; } = port;
        public QualitativeMotifFamily Family { get; } = family; public QualitativeMotifOccurrence Occurrence { get; } = occurrence;
        public QualitativeRecognitionContainment Validation { get; } = validation;
        public IElement Low { get; } = low; public IElement High { get; } = high; public Relation Support { get; } = support;
    }
    private sealed class Candidate(QualitativeMotifFamily family, QualitativeMotifOccurrence occurrence, IElement[] sites, bool certifiedEndpoints)
    {
        public QualitativeMotifFamily Family { get; } = family;
        public QualitativeMotifOccurrence Occurrence { get; } = occurrence;
        public IElement[] Sites { get; } = sites;
        public bool CertifiedEndpoints { get; } = certifiedEndpoints;
    }
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static bool C(Relation scope, out IElement[] entries) => QualitativeRecognitionObservation.Closed(scope, out entries);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

/// <summary>A bounded public supplier reply. Evidence records its advertised
/// cell port, question, revision and published alternatives. Detailed geometric
/// witnesses stay private and can be inspected through the supplier audit.</summary>
public sealed class QualitativeMotifCellReply(IElement cell, IElement port, Relation symbols, Relation evidence,
    Relation competing, Relation unresolved, Relation surface)
{
    public IElement Cell { get; } = cell;
    public IElement Port { get; } = port;
    public Relation Symbols { get; } = symbols;
    public Relation Evidence { get; } = evidence;
    public Relation Competing { get; } = competing;
    public Relation Unresolved { get; } = unresolved;
    public Relation Surface { get; } = surface;
    public bool IsComplete => QualitativeRecognitionObservation.Closed(Symbols, out _) && N.E(Unresolved).Length == 0;
}
