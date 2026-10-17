using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>An explicit connection-selection application over a captured
/// scaffold. It withdraws only shared interior seams whose two incident chambers
/// splice into one simple boundary. Each proposed view is fully checked before
/// accepting that withdrawal. Original strokes, faces and source history
/// remain available; no earlier material delivery or drawing is retracted.</summary>
public sealed class ScaffoldQuestionWithdrawal
{
    // Surface = (source.Surface -> requested actual strokes) -> cause
    // accepted = actual stroke -> (actual two prior boundaries -> merged boundary)
    // check = actual stroke -> prospective complete-scaffold read
    // Result.Premise retains the accepted applications and their checked views.
    // application = Surface -> (result.Surface ->
    //               (removed -> (retained -> (accepted -> (blocked -> unresolved)))))
    // This changes participation in a derived connection view. It is not
    // non-draining observation, metric polygon subtraction, or physical erasure.
    public ScaffoldQuestionScaffold Source { get; }
    public Relation Requested { get; }
    public IElement Cause { get; }
    public Relation Surface { get; }
    public ScaffoldQuestionScaffold Result { get; }
    public Relation Removed { get; }
    public Relation Retained { get; }
    public Relation Accepted { get; }
    public Relation Checks { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    private readonly Relation read;

    public ScaffoldQuestionWithdrawal(ScaffoldQuestionScaffold source, Relation requested, IElement cause)
    {
        Source = source; Requested = requested; Cause = cause;
        Surface = new(new Relation(source.Surface, requested), cause);
        var no = new List<IElement>(); var pending = new List<IElement>(); var yes = new List<IElement>(); var removed = new List<IElement>();
        var checks = new List<IElement>();
        var boundaries = N.E(source.Boundaries).OfType<Relation>().ToList();
        var active = N.E(source.Graph.Strokes).ToList();
        var valid = N.Read(requested, out var requestedEdges) && N.Distinct(requestedEdges);
        if (!valid) pending.Add(requested);
        if (!source.IsComplete) pending.Add(source.Evaluate());
        foreach (var item in requestedEdges)
        {
            if (!valid || !source.IsComplete) { pending.Add(new Relation(item, source.Surface)); continue; }
            if (item is not Relation edge || !active.Any(actual => ReferenceEquals(actual, edge)))
            { no.Add(new Relation(item, source.Graph.Strokes)); continue; }
            var incident = boundaries.Select(boundary => (Boundary: boundary, Parts: N.E(boundary))).Where(pair =>
                pair.Parts.Any(part => part is Relation actual && ReferenceEquals(actual.Left, edge))).ToArray();
            if (incident.Length != 2)
            { no.Add(new Relation(edge, N.S(incident.Select(pair => (IElement)pair.Boundary).ToArray()))); continue; }
            var first = incident[0]; var second = incident[1];
            var i = Array.FindIndex(first.Parts, part => part is Relation actual && ReferenceEquals(actual.Left, edge));
            var j = Array.FindIndex(second.Parts, part => part is Relation actual && ReferenceEquals(actual.Left, edge));
            var a = (Relation)((Relation)first.Parts[i]).Right; var b = (Relation)((Relation)second.Parts[j]).Right;
            if (!N.Same(a.Left, b.Right) || !N.Same(a.Right, b.Left))
            { no.Add(new Relation(edge, new Relation(first.Boundary, second.Boundary))); continue; }
            var mergedParts = Enumerable.Range(1, first.Parts.Length - 1).Select(offset => first.Parts[(i + offset) % first.Parts.Length])
                .Concat(Enumerable.Range(1, second.Parts.Length - 1).Select(offset => second.Parts[(j + offset) % second.Parts.Length])).ToArray();
            var vertices = mergedParts.OfType<Relation>().Select(part => part.Right is Relation ends ? ends.Left : null).ToArray();
            if (vertices.Any(vertex => vertex is null) || !N.Distinct(vertices!) || mergedParts.Length < 3)
            { no.Add(new Relation(edge, N.S(mergedParts))); continue; }
            var merged = N.S(mergedParts);
            var accepted = new Relation(edge, new Relation(N.S(first.Boundary, second.Boundary), merged));
            var proposedBoundaries = boundaries.Where(actual => !ReferenceEquals(actual, first.Boundary) && !ReferenceEquals(actual, second.Boundary)).Append(merged).ToArray();
            var proposedStrokes = active.Where(actual => !ReferenceEquals(actual, edge)).ToArray();
            var proposedGraph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites, N.S(proposedStrokes), source.Graph.PeriodWord);
            var proposed = new ScaffoldQuestionScaffold(proposedGraph, N.S(proposedBoundaries), source.ExteriorBoundaries,
                new Relation(Surface, accepted));
            var check = new Relation(edge, proposed.Evaluate()); checks.Add(check);
            if (!proposed.IsComplete) { no.Add(check); continue; }
            boundaries = proposedBoundaries.ToList(); active = proposedStrokes.ToList(); removed.Add(edge); yes.Add(accepted);
        }
        Removed = N.S(removed.ToArray()); Retained = N.S(active.ToArray()); Accepted = N.S(yes.ToArray());
        Checks = N.S(checks.ToArray());
        Blocked = N.S(no.ToArray()); Unresolved = N.S(pending.ToArray());
        var graph = new QualitativeStrokeGraph(source.Graph.Frame, source.Graph.Sites, Retained, source.Graph.PeriodWord);
        Result = new ScaffoldQuestionScaffold(graph, N.S(boundaries.ToArray()), source.ExteriorBoundaries,
            new Relation(Surface, new Relation(source.Evaluate(), new Relation(Accepted, Checks))));
        read = new(Surface, new Relation(Result.Surface, new Relation(Removed,
            new Relation(Retained, new Relation(Accepted, new Relation(Blocked, Unresolved))))));
    }
    public Relation Evaluate() => read;
}
