using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// A non-draining cell-incidence read: bounded source cells supply fresh
/// representative roles; shared source seams supply representative adjacency.
/// Complete interior vertex stars supply the next cells. An interior role is
/// not a midpoint, metric centre, coordinate, or identification of source sites.
/// The exposed result can be read again without access to the original builder.
/// </summary>
public sealed class OrnamentDerivedScaffold
{
    // topology = sites -> (Closed(actual-edge -> (from -> to)) ->
    //                      Closed(actual-face -> ordered-oriented-boundary))
    // representative = actual-source-face -> fresh-role
    // edge = (actual-source-seam -> Closed(actual-two-face-participations))
    //        -> (actual-representative -> actual-representative)
    // oriented = actual-derived-edge -> (actual-from-role -> actual-to-role)
    // cell = (actual-source-vertex -> Closed(actual-source-corners))
    //        -> Closed(oriented-derived-edges)
    // corner = actual-source-face -> (actual-incoming-part -> actual-outgoing-part)
    // definition = (source -> question) -> source-topology
    // result = definition -> (derived-topology ->
    //          ((representatives -> (seams -> cell-correspondence)) ->
    //           (boundary-complement -> (retained-source -> (blocked -> unresolved)))))
    public IElement Source { get; }
    public IElement Question { get; }
    public Relation SourceTopology { get; }
    public Relation Definition { get; }
    public Relation Sites { get; }
    public Relation Edges { get; }
    public Relation Faces { get; }
    public Relation Topology { get; }
    public Relation Representatives { get; }
    public Relation Seams { get; }
    public Relation CellCorrespondence { get; }
    public Relation CellSources => CellCorrespondence;
    public Relation Boundary { get; }
    public Relation Retained { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentDerivedScaffold(ScaffoldQuestionScaffold source, IElement question)
        : this(source.Evaluate(), question, SourceBasis(source), source.IsComplete,
            source.Exterior, source.IsComplete ? N.S() : N.S(source.Evaluate())) { }

    public OrnamentDerivedScaffold(OrnamentDerivedScaffold source, IElement question)
        : this(source.Surface, question, source.Topology, source.IsComplete,
            source.Boundary, source.IsComplete ? N.S() : N.S(source.Surface)) { }

    private OrnamentDerivedScaffold(IElement source, IElement question, Relation basis,
        bool supported, Relation priorBoundary, Relation priorUnknown)
    {
        Source = source; Question = question; SourceTopology = basis;
        Definition = new(new Relation(source, question), basis);
        var no = new List<IElement>(); var unknown = new List<IElement>(N.E(priorUnknown));
        var boundary = new List<IElement>();
        var representatives = new List<IElement>(); var edges = new List<IElement>();
        var seams = new List<IElement>(); var cells = new List<IElement>(); var correspondence = new List<IElement>();
        var sites = Array.Empty<IElement>(); var inputEdges = Array.Empty<IElement>(); var inputFaces = Array.Empty<IElement>();
        if (basis.Right is not Relation { Left: Relation edgeScope, Right: Relation faceScope } ||
            basis.Left is not Relation siteScope || !N.Read(siteScope, out sites) || !N.Read(edgeScope, out inputEdges) ||
            !N.Read(faceScope, out inputFaces) || !N.Distinct(sites) || !N.Distinct(inputEdges) || !N.Distinct(inputFaces))
        { unknown.Add(basis); supported = false; }

        var edgeRows = new List<Edge>(); var faceRows = new List<Face>();
        var faceRoles = new Dictionary<IElement, IElement>(N.Participants.Instance);
        if (supported)
        {
            foreach (var item in inputEdges)
            {
                if (item is not Relation { Right: Relation ends } row ||
                    !sites.Any(site => N.Same(site, ends.Left)) || !sites.Any(site => N.Same(site, ends.Right)) ||
                    N.Same(ends.Left, ends.Right) || edgeRows.Any(edge => ReferenceEquals(edge.Identity, row.Left)))
                { no.Add(new Relation(item, basis)); continue; }
                edgeRows.Add(new(row.Left, ends.Left, ends.Right));
            }
            foreach (var item in inputFaces)
            {
                if (item is not Relation { Right: Relation faceBoundary } row || !N.Read(faceBoundary, out var parts) || parts.Length < 3)
                { unknown.Add(new Relation(item, basis)); continue; }
                var darts = new List<Dart>();
                foreach (var part in parts)
                {
                    if (part is not Relation { Right: Relation ends } participation ||
                        edgeRows.SingleOrDefault(edge => ReferenceEquals(edge.Identity, participation.Left)) is not Edge edge ||
                        !(N.Same(ends.Left, edge.From) && N.Same(ends.Right, edge.To) ||
                          N.Same(ends.Left, edge.To) && N.Same(ends.Right, edge.From)))
                    { no.Add(new Relation(part, item)); continue; }
                    darts.Add(new(participation, edge, ends.Left, ends.Right));
                }
                if (darts.Count != parts.Length || !N.Distinct(darts.Select(dart => dart.From).ToArray()) ||
                    darts.Where((dart, i) => !N.Same(dart.To, darts[(i + 1) % darts.Count].From)).Any())
                { no.Add(new Relation(item, basis)); continue; }
                if (faceRows.Any(face => ReferenceEquals(face.Identity, row.Left)))
                { no.Add(new Relation(item, basis)); continue; }
                faceRows.Add(new(row.Left, darts.ToArray()));
            }
            if (no.Count == 0 && unknown.Count == 0)
            {
                foreach (var face in faceRows)
                {
                    var representative = new Placeholder(); faceRoles.Add(face.Identity, representative);
                    representatives.Add(new Relation(face.Identity, representative));
                }
                foreach (var edge in edgeRows)
                {
                    var uses = faceRows.SelectMany(face => face.Darts.Where(dart => ReferenceEquals(dart.Edge, edge))
                        .Select(dart => (Face: face, Dart: dart))).ToArray();
                    if (uses.Length < 2)
                    {
                        boundary.Add(new Relation(edge.Identity, N.S(uses.Select(use => (IElement)new Relation(use.Face.Identity, use.Dart.Part)).ToArray())));
                        continue;
                    }
                    if (uses.Length != 2 || ReferenceEquals(uses[0].Face, uses[1].Face) ||
                        !N.Same(uses[0].Dart.From, uses[1].Dart.To) || !N.Same(uses[0].Dart.To, uses[1].Dart.From))
                    { no.Add(new Relation(edge.Identity, N.S(uses.Select(use => (IElement)use.Dart.Part).ToArray()))); continue; }
                    var proof = N.S(uses.Select(use => (IElement)new Relation(use.Face.Identity, use.Dart.Part)).ToArray());
                    var result = new Relation(new Relation(edge.Identity, proof),
                        new Relation(faceRoles[uses[0].Face.Identity], faceRoles[uses[1].Face.Identity]));
                    edge.Result = result; edges.Add(result); seams.Add(new Relation(edge.Identity, result));
                }
                foreach (var vertex in sites)
                {
                    var incident = edgeRows.Where(edge => N.Same(edge.From, vertex) || N.Same(edge.To, vertex)).ToArray();
                    var touching = faceRows.Where(face => face.Darts.Any(dart => N.Same(dart.From, vertex))).ToArray();
                    var cornerProof = N.S(touching.Select(face =>
                    {
                        var outgoing = Array.FindIndex(face.Darts, dart => N.Same(dart.From, vertex));
                        return (IElement)new Relation(face.Identity, new Relation(face.Darts[(outgoing + face.Darts.Length - 1) % face.Darts.Length].Part,
                            face.Darts[outgoing].Part));
                    }).ToArray());
                    if (incident.Length == 0 || incident.Any(edge => edge.Result is null))
                    { boundary.Add(new Relation(vertex, cornerProof)); continue; }
                    if (touching.Length < 3)
                    { unknown.Add(new Relation(vertex, cornerProof)); continue; }
                    var seen = new HashSet<Face>(ReferenceEqualityComparer.Instance); var cycle = new List<IElement>(); var at = touching[0];
                    while (seen.Add(at))
                    {
                        var outgoing = at.Darts.Single(dart => N.Same(dart.From, vertex));
                        var next = touching.Where(face => !ReferenceEquals(face, at) &&
                            face.Darts.Any(dart => ReferenceEquals(dart.Edge, outgoing.Edge))).ToArray();
                        if (next.Length != 1) break;
                        cycle.Add(new Relation(outgoing.Edge.Result!, new Relation(faceRoles[at.Identity], faceRoles[next[0].Identity])));
                        at = next[0];
                    }
                    if (!ReferenceEquals(at, touching[0]) || seen.Count != touching.Length || cycle.Count != touching.Length)
                    { no.Add(new Relation(vertex, new Relation(cornerProof, N.S(cycle.ToArray())))); continue; }
                    var cell = new Relation(new Relation(vertex, cornerProof), N.S(cycle.ToArray()));
                    cells.Add(cell); correspondence.Add(new Relation(vertex, cell));
                }
            }
        }
        Representatives = N.S(representatives.ToArray()); Sites = N.S(representatives.Cast<Relation>().Select(row => row.Right).ToArray());
        Edges = N.S(edges.ToArray()); Faces = N.S(cells.ToArray()); Seams = N.S(seams.ToArray());
        CellCorrespondence = N.S(correspondence.ToArray()); Boundary = N.S(boundary.ToArray());
        Retained = N.S(new Relation(source, priorBoundary), basis);
        Blocked = N.S(no.ToArray()); Unresolved = N.S(unknown.ToArray());
        Topology = new(Sites, new Relation(N.S(edges.Cast<Relation>().Select(edge => (IElement)new Relation(edge, edge.Right)).ToArray()),
            N.S(cells.Cast<Relation>().Select(cell => (IElement)new Relation(cell, cell.Right)).ToArray())));
        Surface = new(Definition, new Relation(Topology,
            new Relation(new Relation(Representatives, new Relation(Seams, CellCorrespondence)),
                new Relation(Boundary, new Relation(Retained, new Relation(Blocked, Unresolved))))));
    }

    public Relation Evaluate() => Surface;

    public bool TryReadRepresentative(Relation row, out IElement sourceFace, out IElement representative)
    {
        sourceFace = representative = null!;
        if (!IsComplete || !N.E(Representatives).Any(actual => ReferenceEquals(actual, row))) return false;
        sourceFace = row.Left; representative = row.Right; return true;
    }

    public bool TryReadEdge(Relation edge, out IElement sourceSeam, out IElement from, out IElement to, out Relation evidence)
    {
        sourceSeam = from = to = null!; evidence = null!;
        if (!IsComplete || !N.E(Edges).Any(actual => ReferenceEquals(actual, edge)) ||
            edge.Left is not Relation { Right: Relation proof } origin || edge.Right is not Relation ends) return false;
        sourceSeam = origin.Left; from = ends.Left; to = ends.Right; evidence = proof; return true;
    }

    public bool TryReadCell(Relation cell, out IElement sourceVertex, out Relation boundary, out Relation sourceCorners)
    {
        sourceVertex = null!; boundary = sourceCorners = null!;
        if (!IsComplete || !N.E(Faces).Any(actual => ReferenceEquals(actual, cell)) ||
            cell.Left is not Relation { Right: Relation corners } origin || cell.Right is not Relation parts) return false;
        sourceVertex = origin.Left; boundary = parts; sourceCorners = corners; return true;
    }

    private static Relation SourceBasis(ScaffoldQuestionScaffold source)
    {
        var edges = new List<IElement>();
        foreach (var edge in N.E(source.Graph.Strokes).OfType<Relation>())
            if (QualitativeStrokeGraph.TryReadStroke(edge, out _, out var from, out var to, out _))
                edges.Add(new Relation(edge, new Relation(from, to)));
        var faces = new List<IElement>();
        foreach (var face in N.E(source.Faces).OfType<Relation>())
            if (source.TryReadFace(face, out var boundary, out _)) faces.Add(new Relation(face, boundary));
        return new(source.Graph.Sites, new Relation(N.S(edges.ToArray()), N.S(faces.ToArray())));
    }

    private sealed class Edge(IElement identity, IElement from, IElement to)
    {
        internal IElement Identity { get; } = identity;
        internal IElement From { get; } = from;
        internal IElement To { get; } = to;
        internal Relation? Result { get; set; }
    }
    private sealed record Dart(Relation Part, Edge Edge, IElement From, IElement To);
    private sealed record Face(IElement Identity, Dart[] Darts);
}
