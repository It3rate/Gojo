using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// A non-draining recurrence question over an advertised cellular topology.
/// Complete simple boundary cycles are compared through fresh role-renamed
/// cycles. This deliberately coarse read discovers boundary arity, not lengths,
/// angles, regularity, geometric congruence, or independent statistical evidence.
/// It needs no authoring grid, history, motif name, or coordinate interpretation.
/// </summary>
public sealed class OrnamentCellDiscovery
{
    // topology = Closed(sites) -> (Closed(edge -> (from -> to)) ->
    //                             Closed(face -> Closed(oriented darts)))
    // oriented dart = actual edge -> (actual from -> actual to)
    // signature = the same topology with one fresh role-renamed cycle.
    // occurrence = actual source face row -> (actual boundary ->
    //              Closed(canonical dart -> actual source dart))
    // family = (definition -> signature) -> Closed(occurrences)
    // definition = (source -> question) -> supplied topology
    // read = definition -> (families -> (singleton-face complement ->
    //                                   (blocked -> unresolved)))
    public IElement Source { get; }
    public Relation SourceTopology { get; }
    public IElement Question { get; }
    public Relation Definition { get; }
    public Relation Families { get; }
    public Relation Complement { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentCellDiscovery(IElement source, Relation topology, IElement question)
    {
        Source = source; SourceTopology = topology; Question = question;
        Definition = new(new Relation(source, question), topology);
        var blocked = new List<IElement>(); var unresolved = new List<IElement>();
        var families = new List<IElement>(); var complement = new List<IElement>();
        if (TryReadTopology(topology, blocked, unresolved, out var faces))
        {
            // The host grouping is only an index into complete native cycle
            // witnesses. Every selected occurrence retains its full bijection.
            foreach (var group in faces.GroupBy(face => face.Darts.Length))
            {
                var occurrences = group.ToArray();
                if (occurrences.Length == 1) { complement.Add(occurrences[0].Row); continue; }
                var pattern = Cycle(group.Key, out var canonicalDarts);
                var rows = occurrences.Select(face => (IElement)new Relation(face.Row,
                    new Relation(face.Boundary, N.S(canonicalDarts.Select((dart, i) =>
                        (IElement)new Relation(dart, face.Darts[i])).ToArray())))).ToArray();
                families.Add(new Relation(new Relation(Definition, pattern), N.S(rows)));
            }
        }
        Families = N.S(families.ToArray()); Complement = N.S(complement.ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(unresolved.ToArray());
        Surface = new(Definition, new Relation(Families,
            new Relation(Complement, new Relation(Blocked, Unresolved))));
    }

    public Relation Evaluate() => Surface;

    public bool TryReadFamily(Relation actualRow, out OrnamentCellFamily family)
    {
        family = null!;
        if (!N.E(Families).Any(row => ReferenceEquals(row, actualRow)) ||
            actualRow is not { Left: Relation origin, Right: Relation occurrences } ||
            !ReferenceEquals(origin.Left, Definition) || origin.Right is not Relation signature ||
            !N.Read(occurrences, out var rows) || rows.Length < 2 ||
            signature.Right is not Relation { Right: Relation patternFaces } ||
            N.E(patternFaces).SingleOrDefault() is not Relation { Right: Relation boundary } ||
            !N.Read(boundary, out var darts)) return false;
        family = new(actualRow, signature, occurrences, darts.Length); return true;
    }

    public bool TryReadOccurrence(Relation actualFamily, Relation actualOccurrence,
        out IElement faceIdentity, out Relation boundary, out Relation correspondence)
    {
        faceIdentity = null!; boundary = correspondence = null!;
        if (!TryReadFamily(actualFamily, out var family) ||
            !N.E(family.Occurrences).Any(row => ReferenceEquals(row, actualOccurrence)) ||
            actualOccurrence is not { Left: Relation face, Right: Relation { Left: Relation actualBoundary,
                Right: Relation proof } } || !ReferenceEquals(face.Right, actualBoundary)) return false;
        faceIdentity = face.Left; boundary = actualBoundary; correspondence = proof; return true;
    }

    private static bool TryReadTopology(Relation topology, List<IElement> blocked,
        List<IElement> unresolved, out List<Face> faces)
    {
        faces = [];
        if (topology is not { Left: Relation sites, Right: Relation { Left: Relation edges, Right: Relation faceScope } })
        { blocked.Add(topology); return false; }
        var ready = ReadClosed(sites, blocked, unresolved, out var siteRows);
        ready &= ReadClosed(edges, blocked, unresolved, out var edgeRows);
        ready &= ReadClosed(faceScope, blocked, unresolved, out var faceRows);
        if (!ready) return false;
        if (!N.Distinct(siteRows) || !N.Distinct(edgeRows) || !N.Distinct(faceRows))
        { blocked.Add(topology); return false; }
        var edgeEnds = new Dictionary<IElement, Relation>(N.Participants.Instance);
        var duplicateEdgeIds = edgeRows.OfType<Relation>().GroupBy(row => row.Left, N.Participants.Instance)
            .Where(group => group.Count() != 1).Select(group => group.Key).ToHashSet(N.Participants.Instance);
        foreach (var item in edgeRows)
        {
            if (item is not Relation { Right: Relation ends } row || duplicateEdgeIds.Contains(row.Left) ||
                N.Same(ends.Left, ends.Right) || !Contains(siteRows, ends.Left) || !Contains(siteRows, ends.Right))
            { blocked.Add(new Relation(item, edges)); continue; }
            edgeEnds.Add(row.Left, ends);
        }
        var duplicateFaceIds = faceRows.OfType<Relation>().GroupBy(row => row.Left, N.Participants.Instance)
            .Where(group => group.Count() != 1).Select(group => group.Key).ToHashSet(N.Participants.Instance);
        foreach (var item in faceRows)
        {
            if (item is not Relation { Right: Relation boundary } face || duplicateFaceIds.Contains(face.Left))
            { blocked.Add(new Relation(item, faceScope)); continue; }
            if (!ReadClosed(boundary, blocked, unresolved, out var parts)) continue;
            if (parts.Length < 3 || !N.Distinct(parts))
            { blocked.Add(new Relation(face, boundary)); continue; }
            var darts = new List<Relation>();
            foreach (var part in parts)
            {
                if (part is not Relation { Right: Relation ends } dart ||
                    !edgeEnds.TryGetValue(dart.Left, out var advertised) ||
                    !(N.Same(ends.Left, advertised.Left) && N.Same(ends.Right, advertised.Right) ||
                      N.Same(ends.Left, advertised.Right) && N.Same(ends.Right, advertised.Left)))
                { blocked.Add(new Relation(part, face)); continue; }
                darts.Add(dart);
            }
            if (darts.Count != parts.Length || !N.Distinct(darts.Select(dart => dart.Left).ToArray()) ||
                !N.Distinct(darts.Select(dart => ((Relation)dart.Right).Left).ToArray()) ||
                darts.Where((dart, i) => !N.Same(((Relation)dart.Right).Right,
                    ((Relation)darts[(i + 1) % darts.Count].Right).Left)).Any())
            { blocked.Add(new Relation(face, boundary)); continue; }
            faces.Add(new(face, boundary, darts.ToArray()));
        }
        return true;
    }

    private static bool ReadClosed(Relation scope, List<IElement> blocked,
        List<IElement> unresolved, out IElement[] entries)
    {
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail))
        { blocked.Add(scope); return false; }
        if (!seam.Equals(tail)) { unresolved.Add(new Relation(scope, tail)); return false; }
        return true;
    }

    private static bool Contains(IElement[] entries, IElement item) => entries.Any(entry => N.Same(entry, item));

    private static Relation Cycle(int arity, out Relation[] darts)
    {
        var sites = Enumerable.Range(0, arity).Select(_ => (IElement)new Placeholder()).ToArray();
        var edges = Enumerable.Range(0, arity).Select(i => new Relation(new Placeholder(),
            new Relation(sites[i], sites[(i + 1) % arity]))).ToArray();
        darts = edges.Select(edge => new Relation(edge.Left, edge.Right)).ToArray();
        return new(N.S(sites), new Relation(N.S(edges), N.S(new Relation(new Placeholder(), N.S(darts)))));
    }

    private sealed class Face(Relation row, Relation boundary, Relation[] darts)
    {
        internal Relation Row { get; } = row;
        internal Relation Boundary { get; } = boundary;
        internal Relation[] Darts { get; } = darts;
    }
}

/// <summary>A decoded adapter over a native family witness; it does not own the
/// signature or membership. BoundaryArity is a count read, not a geometric type.</summary>
public sealed class OrnamentCellFamily(Relation read, Relation signature, Relation occurrences, int boundaryArity)
{
    public Relation Read { get; } = read;
    public Relation Signature { get; } = signature;
    public Relation Occurrences { get; } = occurrences;
    public int BoundaryArity { get; } = boundaryArity;
}
