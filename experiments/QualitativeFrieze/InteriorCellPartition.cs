using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// An exclusive occupancy read over an advertised atomic cell complex. Removing
/// a shared edge merges its two atomic interiors; it never paints one polygon
/// over another. Topological ownership and the separately supplied embedding
/// remain distinct premises, with actual failed participants retained.
/// </summary>
public sealed partial class InteriorCellPartition
{
    // complex = Closed(sites) -> (Closed(edge-id -> (from -> to)) ->
    //                             Closed(face-id -> Closed(oriented darts)))
    // dart = actual edge-id -> (actual from -> actual to)
    // claims = Closed(group-id -> Closed(actual face ROWS))
    // embedding = bindings -> (actual frame surface -> common reach)
    // binding = actual site -> closed formal motion word
    // source = complex -> (claims -> embedding)
    // group read = actual claim -> (boundary ->
    //               (removed internal edge rows -> paired actual darts))
    // read = source -> (accepted -> (blocked -> (open -> embedding read)))
    public Relation Source { get; }
    public Relation Complex { get; }
    public Relation GroupingClaims { get; }
    public Relation Accepted { get; }
    public Relation Blocked { get; }
    public Relation Open { get; }
    public Relation Embedding { get; }
    public Relation EmbeddingAccepted { get; }
    public Relation EmbeddingBlocked { get; }
    public Relation EmbeddingOpen { get; }
    public Relation Surface { get; }
    public IReadOnlyList<InteriorCellGroupRead> GroupReads { get; }
    public bool IsPartition { get; }
    public bool IsEmbeddedPartition => IsPartition && N.E(EmbeddingBlocked).Length == 0 && N.E(EmbeddingOpen).Length == 0;
    public InteriorPartitionSummary Summary { get; }

    public static Relation Topology(Relation sites, Relation edges, Relation atomicFaces) =>
        new(sites, new Relation(edges, atomicFaces));
    public static Relation Edge(IElement identity, IElement from, IElement to) => new(identity, new Relation(from, to));
    public static Relation Dart(Relation actualEdge, IElement from, IElement to) => new(actualEdge.Left, new Relation(from, to));
    public static Relation Face(IElement identity, params Relation[] darts) => new(identity, N.S(darts));
    public static Relation Claim(IElement identity, params Relation[] actualAtomicFaces) => new(identity, N.S(actualAtomicFaces));

    public static InteriorCellPartition Read(Relation complex, Relation groupingClaims, Relation positionBindings,
        Relation frameSurface, IElement reach) => new(complex, groupingClaims, positionBindings, frameSurface, reach);
    public static InteriorCellPartition Read(Relation complex, Relation groupingClaims, QualitativeMotionFrame frame,
        Relation positionBindings, IElement reach) => Read(complex, groupingClaims, positionBindings, frame.Surface, reach);
    public static InteriorCellPartition Read(Relation complex, Relation groupingClaims) => new(complex, groupingClaims, null, null, null);

    private InteriorCellPartition(Relation complex, Relation groupingClaims, Relation? bindings, Relation? frame, IElement? reach)
    {
        Complex = complex; GroupingClaims = groupingClaims;
        Embedding = new(bindings ?? N.S(), frame is null || reach is null ? N.S() : new Relation(frame, reach));
        Source = new(complex, new Relation(groupingClaims, Embedding));
        var blocked = new List<IElement>(); var open = new List<IElement>();
        var accepted = new List<InteriorCellGroupRead>();
        var sites = Array.Empty<IElement>(); var edgeRows = Array.Empty<IElement>(); var faceRows = Array.Empty<IElement>();
        var edges = new Dictionary<IElement, Relation>(N.Participants.Instance);
        var atoms = new Dictionary<IElement, Atom>(N.Participants.Instance);
        var edgeUses = new Dictionary<IElement, List<(Atom Atom, Relation Dart)>>(N.Participants.Instance);
        var ready = complex is { Left: Relation, Right: Relation { Left: Relation, Right: Relation } };
        if (complex is { Left: Relation siteScope, Right: Relation { Left: Relation edgeScope, Right: Relation faceScope } })
        {
            ready &= Closed(siteScope, blocked, open, out sites);
            ready &= Closed(edgeScope, blocked, open, out edgeRows);
            ready &= Closed(faceScope, blocked, open, out faceRows);
            if (!N.Distinct(sites) || !N.Distinct(edgeRows) || !N.Distinct(faceRows)) { blocked.Add(complex); ready = false; }
            var siteSet = sites.ToHashSet(N.Participants.Instance);
            foreach (var item in edgeRows)
            {
                if (item is not Relation { Right: Relation ends } row || N.Same(ends.Left, ends.Right) ||
                    !siteSet.Contains(ends.Left) || !siteSet.Contains(ends.Right) || !edges.TryAdd(row.Left, row))
                { blocked.Add(new Relation(item, edgeScope)); ready = false; }
            }
            foreach (var item in faceRows)
            {
                if (item is not Relation { Right: Relation boundary } face || atoms.ContainsKey(face.Left))
                { blocked.Add(new Relation(item, faceScope)); ready = false; continue; }
                if (!Closed(boundary, blocked, open, out var parts)) { ready = false; continue; }
                var darts = parts.OfType<Relation>().ToArray();
                var valid = darts.Length == parts.Length && darts.Length >= 3 && N.Distinct(darts.Select(d => d.Left).ToArray());
                for (var i = 0; i < darts.Length && valid; i++)
                {
                    var dart = darts[i];
                    valid &= dart.Right is Relation ends && edges.TryGetValue(dart.Left, out var edge) &&
                        Matches(ends, (Relation)edge.Right) && darts[(i + 1) % darts.Length].Right is Relation next && N.Same(ends.Right, next.Left);
                }
                valid &= N.Distinct(darts.Where(d => d.Right is Relation).Select(d => ((Relation)d.Right).Left).ToArray());
                if (!valid) { blocked.Add(new Relation(face, boundary)); ready = false; continue; }
                var atom = new Atom(face, darts); atoms.Add(face.Left, atom);
                foreach (var dart in darts)
                {
                    if (!edgeUses.TryGetValue(dart.Left, out var uses)) edgeUses.Add(dart.Left, uses = []);
                    uses.Add((atom, dart));
                }
            }
            foreach (var (identity, uses) in edgeUses)
                if (uses.Count > 2 || uses.Count == 2 && !Opposed(uses[0].Dart, uses[1].Dart))
                { blocked.Add(new Relation(edges[identity], N.S(uses.Select(u => (IElement)new Relation(u.Atom.Row, u.Dart)).ToArray()))); ready = false; }
            foreach (var edge in edges.Values.Where(e => !edgeUses.ContainsKey(e.Left)))
            { blocked.Add(new Relation(edge, faceScope)); ready = false; }
        }
        else blocked.Add(complex);

        var claimsReady = Closed(groupingClaims, blocked, open, out var claimRows);
        var owners = new Dictionary<Atom, List<Relation>>();
        var candidates = new List<(Relation Claim, Atom[] Atoms)>();
        var groupIds = new HashSet<IElement>(N.Participants.Instance);
        foreach (var entry in claimRows)
        {
            if (entry is not Relation { Right: Relation selected } claim || !groupIds.Add(claim.Left))
            { blocked.Add(new Relation(entry, groupingClaims)); claimsReady = false; continue; }
            if (!Closed(selected, blocked, open, out var selectedRows)) { claimsReady = false; continue; }
            var groupAtoms = new List<Atom>(); var valid = selectedRows.Length != 0 && N.Distinct(selectedRows);
            foreach (var row in selectedRows)
            {
                var atom = row is Relation face && atoms.TryGetValue(face.Left, out var found) && ReferenceEquals(found.Row, row) ? found : null;
                if (atom is null) { blocked.Add(new Relation(claim, row)); valid = false; continue; }
                groupAtoms.Add(atom);
                if (!owners.TryGetValue(atom, out var atomOwners)) owners.Add(atom, atomOwners = []);
                atomOwners.Add(claim);
            }
            if (!valid) { blocked.Add(new Relation(claim, selected)); claimsReady = false; }
            else candidates.Add((claim, groupAtoms.ToArray()));
        }
        var conflictedClaims = new HashSet<IElement>(N.Participants.Instance);
        foreach (var atom in atoms.Values)
        {
            if (!owners.TryGetValue(atom, out var participation)) { open.Add(new Relation(atom.Row, groupingClaims)); continue; }
            if (participation.Count == 1) continue;
            blocked.Add(new Relation(atom.Row, N.S(participation.Cast<IElement>().ToArray())));
            foreach (var owner in participation) conflictedClaims.Add(owner);
        }
        foreach (var (claim, selectedAtoms) in candidates)
        {
            if (conflictedClaims.Contains(claim)) continue;
            var selected = selectedAtoms.ToHashSet(); var seen = new HashSet<Atom>(); var pending = new Queue<Atom>();
            pending.Enqueue(selectedAtoms[0]); seen.Add(selectedAtoms[0]);
            while (pending.TryDequeue(out var at))
                foreach (var dart in at.Darts)
                    foreach (var use in edgeUses[dart.Left])
                        if (selected.Contains(use.Atom) && seen.Add(use.Atom)) pending.Enqueue(use.Atom);
            if (seen.Count != selected.Count)
            { blocked.Add(new Relation(claim, N.S(selectedAtoms.Where(a => !seen.Contains(a)).Select(a => (IElement)a.Row).ToArray()))); continue; }
            var external = new List<Relation>(); var internalEdges = new List<IElement>(); var pairs = new List<IElement>();
            foreach (var edge in selectedAtoms.SelectMany(a => a.Darts).Select(d => d.Left).Distinct(N.Participants.Instance))
            {
                var uses = edgeUses[edge].Where(u => selected.Contains(u.Atom)).ToArray();
                if (uses.Length == 1) external.Add(uses[0].Dart);
                else if (uses.Length == 2 && Opposed(uses[0].Dart, uses[1].Dart))
                { internalEdges.Add(edges[edge]); pairs.Add(new Relation(uses[0].Dart, uses[1].Dart)); }
                else blocked.Add(new Relation(claim, edges[edge]));
            }
            // A disk requires one directed boundary cycle. Connectivity alone
            // would admit annuli and pinched collections of otherwise valid atoms.
            if (!TryBoundary(external, out var cycle))
            { blocked.Add(new Relation(claim, N.S(external.ToArray()))); continue; }
            accepted.Add(new InteriorCellGroupRead(claim, selectedAtoms.Select(a => a.Row).ToArray(), cycle,
                N.S(internalEdges.ToArray()), N.S(pairs.ToArray())));
        }
        GroupReads = accepted;
        Accepted = N.S(accepted.Select(g => (IElement)g.Surface).ToArray()); Blocked = N.S(blocked.ToArray()); Open = N.S(open.ToArray());
        IsPartition = ready && claimsReady && blocked.Count == 0 && open.Count == 0 && accepted.Count == claimRows.Length && atoms.Count == faceRows.Length;
        var embedding = ReadEmbedding(sites, atoms.Values.ToArray(), edges, accepted, bindings, frame, reach);
        EmbeddingAccepted = N.S(embedding.Accepted.ToArray()); EmbeddingBlocked = N.S(embedding.Blocked.ToArray()); EmbeddingOpen = N.S(embedding.Open.ToArray());
        Surface = new(Source, new Relation(Accepted, new Relation(Blocked, new Relation(Open,
            new Relation(EmbeddingAccepted, new Relation(EmbeddingBlocked, EmbeddingOpen))))));
        Summary = new(sites.Length, edges.Count, atoms.Count, claimRows.Length, accepted.Count,
            accepted.Sum(g => g.BoundaryDarts.Count), accepted.Sum(g => N.E(g.RemovedInternalEdges).Length),
            owners.Count(kv => kv.Value.Count > 1), atoms.Count - owners.Count,
            embedding.ComparedPairs, embedding.OverlappingPairs, IsPartition, IsEmbeddedPartition);
    }

    private static bool TryBoundary(IReadOnlyList<Relation> darts, out Relation[] cycle)
    {
        cycle = [];
        if (darts.Count < 3) return false;
        var starts = new Dictionary<IElement, Relation>(N.Participants.Instance);
        var ends = new HashSet<IElement>(N.Participants.Instance);
        foreach (var dart in darts)
            if (dart.Right is not Relation endpoints || !starts.TryAdd(endpoints.Left, dart) || !ends.Add(endpoints.Right)) return false;
        if (starts.Keys.Any(site => !ends.Contains(site))) return false;
        var result = new List<Relation>(); var used = new HashSet<IElement>(N.Participants.Instance); var current = darts[0];
        while (used.Add(current))
        {
            result.Add(current);
            if (!starts.TryGetValue(((Relation)current.Right).Right, out current!)) return false;
        }
        if (!ReferenceEquals(current, darts[0]) || result.Count != darts.Count) return false;
        cycle = result.ToArray(); return true;
    }
    private static bool Matches(Relation a, Relation b) => N.Same(a.Left, b.Left) && N.Same(a.Right, b.Right) || N.Same(a.Left, b.Right) && N.Same(a.Right, b.Left);
    private static bool Opposed(Relation a, Relation b) => a.Right is Relation x && b.Right is Relation y && N.Same(x.Left, y.Right) && N.Same(x.Right, y.Left);
    private static bool Closed(Relation scope, List<IElement> blocked, List<IElement> open, out IElement[] entries)
    {
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail)) { blocked.Add(scope); return false; }
        if (!seam.Equals(tail)) { open.Add(new Relation(scope, tail)); return false; }
        return true;
    }
    private sealed record Atom(Relation Row, Relation[] Darts);
}

/// <summary>Decoded projection of an actual accepted grouping witness.</summary>
public sealed class InteriorCellGroupRead
{
    public Relation Group { get; }
    public IReadOnlyList<Relation> Atoms { get; }
    public IReadOnlyList<Relation> BoundaryDarts { get; }
    public Relation Boundary { get; }
    public Relation RemovedInternalEdges { get; }
    public Relation PairedInternalDarts { get; }
    public Relation Surface { get; }
    public InteriorCellShapeSummary? Shape { get; internal set; }
    public Relation? ShapeWitness { get; internal set; }
    internal InteriorCellGroupRead(Relation group, Relation[] atoms, Relation[] boundary, Relation removed, Relation paired)
    {
        Group = group; Atoms = atoms; BoundaryDarts = boundary; Boundary = N.S(boundary);
        RemovedInternalEdges = removed; PairedInternalDarts = paired;
        Surface = new(group, new Relation(Boundary, new Relation(removed, paired)));
    }
}

public sealed record InteriorPartitionSummary(int Sites, int Edges, int AtomicFaces, int ClaimedGroups,
    int AcceptedGroups, int BoundaryDarts, int RemovedInternalEdges, int MultiplyOwnedAtoms, int UnownedAtoms,
    int EmbeddingComparedPairs, int OverlappingAtomPairs, bool IsPartition, bool IsEmbeddedPartition);
