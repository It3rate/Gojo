using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A supplied finite cellular embedding of actual stroke incidence.
/// Complete oriented boundary coverage, local rotation and planar Euler
/// obligations are checked. The embedding is a declared premise, not geometry
/// inferred from a renderer, and a three-edge chamber does not prove metric
/// straightness, angle or area.</summary>
public sealed class ScaffoldQuestionScaffold
{
    // Surface = (graph.Surface -> (bounded-boundaries -> exterior-boundaries)) -> premise
    // boundary = Closed(actual-stroke -> (actual-from-site -> actual-to-site), ...)
    // face = (Surface -> actual-boundary) -> (vertices -> (formal-closure -> corners))
    // corner = (actual incoming participation -> actual outgoing participation)
    //          -> actual shared site
    // read = Surface -> (faces -> (exterior -> (rotation -> (Euler -> (blocked -> unresolved)))))
    // Every stroke has exactly one occurrence on each face side. The exterior
    // is supplied explicitly, one for each connected component. Its selection
    // cannot be established from an abstract rotation system alone.
    public QualitativeStrokeGraph Graph { get; }
    public Relation Boundaries { get; }
    public Relation ExteriorBoundaries { get; }
    public IElement Premise { get; }
    public Relation Surface { get; }
    public Relation Faces { get; }
    public Relation Exterior { get; }
    public Relation Rotation { get; }
    public Relation Euler { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
    private readonly Relation read;

    public ScaffoldQuestionScaffold(QualitativeStrokeGraph graph, Relation boundedBoundaries,
        Relation exteriorBoundaries, IElement premise)
    {
        Graph = graph; Boundaries = boundedBoundaries; ExteriorBoundaries = exteriorBoundaries; Premise = premise;
        Surface = new(new Relation(graph.Surface, new Relation(boundedBoundaries, exteriorBoundaries)), premise);
        var no = new List<IElement>(); var pending = new List<IElement>();
        var faces = new List<IElement>(); var exterior = new List<IElement>();
        if (!N.Read(graph.Sites, out var sites) || !N.Distinct(sites)) pending.Add(graph.Sites);
        if (!N.Read(graph.Strokes, out var strokeItems) || !N.Distinct(strokeItems)) pending.Add(graph.Strokes);
        if (!N.Read(boundedBoundaries, out var boundaries)) pending.Add(boundedBoundaries);
        if (!N.Read(exteriorBoundaries, out var outer)) pending.Add(exteriorBoundaries);
        if (sites.Length == 0 || strokeItems.Length == 0 || boundaries.Length == 0 || outer.Length == 0)
            pending.Add(new Relation(graph.Surface, new Relation(boundedBoundaries, exteriorBoundaries)));
        if (!N.Distinct(boundaries) || !N.Distinct(outer)) no.Add(new Relation(boundedBoundaries, exteriorBoundaries));
        var strokes = strokeItems.OfType<Relation>().ToArray();
        var endpoints = new (IElement From, IElement To)[strokes.Length];
        for (var i = 0; i < strokeItems.Length; i++)
        {
            if (strokeItems[i] is not Relation edge || !QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out var word) ||
                !sites.Any(site => N.Same(site, a)) || !sites.Any(site => N.Same(site, b)) || N.Same(a, b))
                no.Add(new Relation(strokeItems[i], graph.Surface));
            else
            {
                var extent = graph.Frame.Reduce(N.S(), word);
                if (!graph.Frame.TryReadResult(extent, out var remaining, out _, out var unknown) ||
                    !N.Read(word, out _) || !N.Read(remaining, out _) || N.E(unknown).Length != 0)
                    pending.Add(new Relation(edge, extent));
                else if (N.E(remaining).Length == 0) no.Add(new Relation(edge, extent));
            }
        }
        for (var i = 0; i < strokes.Length; i++)
            if (QualitativeStrokeGraph.TryReadStroke(strokes[i], out _, out var a, out var b, out _)) endpoints[i] = (a, b);
        var uses = new List<Relation>[strokes.Length * 2];
        for (var i = 0; i < uses.Length; i++) uses[i] = [];
        var successor = Enumerable.Repeat(-1, uses.Length).ToArray();
        var cornerRows = new List<IElement>();
        foreach (var (item, outside) in boundaries.Select(item => (item, false)).Concat(outer.Select(item => (item, true))))
        {
            if (item is not Relation boundary || !N.Read(boundary, out var parts))
            { pending.Add(new Relation(item, Surface)); continue; }
            var darts = new List<(Relation Part, int Index, IElement From, IElement To, Relation Word)>();
            var local = new List<IElement>();
            foreach (var part in parts)
            {
                if (part is not Relation { Left: Relation edge, Right: Relation ends } participation ||
                    !QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out var word))
                { local.Add(new Relation(part, graph.Strokes)); continue; }
                var edgeIndex = Array.FindIndex(strokes, actual => ReferenceEquals(actual, edge));
                if (edgeIndex < 0) { local.Add(new Relation(part, graph.Strokes)); continue; }
                var forward = N.Same(a, ends.Left) && N.Same(b, ends.Right);
                if (!forward && !(N.Same(b, ends.Left) && N.Same(a, ends.Right)))
                { local.Add(new Relation(part, edge)); continue; }
                var index = edgeIndex * 2 + (forward ? 0 : 1);
                uses[index].Add(participation);
                darts.Add((participation, index, ends.Left, ends.Right, forward ? word : Inverse(graph.Frame, word)));
            }
            var vertices = darts.Select(dart => dart.From).ToArray(); var corners = new List<IElement>();
            if (darts.Count != parts.Length || parts.Length < 3 || !outside && !N.Distinct(vertices)) local.Add(boundary);
            for (var i = 0; i < darts.Count; i++)
            {
                var a = darts[i]; var b = darts[(i + 1) % darts.Count];
                var corner = new Relation(new Relation(a.Part, b.Part), a.To); corners.Add(corner); cornerRows.Add(corner);
                if (!N.Same(a.To, b.From)) { local.Add(corner); continue; }
                var inverse = a.Index ^ 1;
                if (successor[inverse] != -1) local.Add(corner);
                else successor[inverse] = b.Index;
            }
            var words = darts.SelectMany(dart => N.E(dart.Word)).ToArray();
            var closure = graph.Frame.Reduce(N.S(), N.S(words));
            if (!graph.Frame.TryReadResult(closure, out var remaining, out _, out var unknown) ||
                N.E(remaining).Length != 0 || N.E(unknown).Length != 0) local.Add(closure);
            var face = new Relation(new Relation(Surface, boundary), new Relation(N.S(vertices), new Relation(closure, N.S(corners.ToArray()))));
            (outside ? exterior : faces).Add(face);
            if (local.Count != 0) no.Add(new Relation(face, N.S(local.ToArray())));
        }
        for (var i = 0; i < uses.Length; i++)
            if (uses[i].Count != 1) no.Add(new Relation(strokes[i / 2], N.S(uses[i].Cast<IElement>().ToArray())));
        // Face corners induce the cyclic order of outgoing darts at every site.
        // Several cycles at one site would pinch separate sheets together.
        var rotations = new List<IElement>();
        foreach (var site in sites)
        {
            var incident = Enumerable.Range(0, endpoints.Length).SelectMany(i =>
                N.Same(endpoints[i].From, site) ? new[] { i * 2 } : N.Same(endpoints[i].To, site) ? new[] { i * 2 + 1 } : []).ToArray();
            var visited = new HashSet<int>(); var ordered = new List<IElement>();
            if (incident.Length != 0)
            {
                var at = incident[0];
                while (incident.Contains(at) && visited.Add(at))
                { ordered.AddRange(uses[at]); at = successor[at]; }
                if (at != incident[0] || visited.Count != incident.Length) no.Add(new Relation(site, N.S(ordered.ToArray())));
            }
            else no.Add(new Relation(site, graph.Strokes));
            rotations.Add(new Relation(site, N.S(ordered.ToArray())));
        }
        var components = Components(sites, endpoints); var euler = new List<IElement>();
        foreach (var component in components)
        {
            var edges = Enumerable.Range(0, endpoints.Length).Where(i => component.Any(site => N.Same(site, endpoints[i].From))).ToArray();
            bool InComponent(IElement item) => item is Relation boundary && N.E(boundary).FirstOrDefault() is Relation { Right: Relation ends } && component.Any(site => N.Same(site, ends.Left));
            var inner = boundaries.Where(InComponent).ToArray(); var outside = outer.Where(InComponent).ToArray();
            var left = component.Concat(inner).Concat(outside).ToArray();
            // Two anchored root occurrences are the sphere's Euler remainder;
            // all finite cardinality is visible as actual scoped participation.
            var roots = component.Length == 0 ? [] : new[] { component[0], component[0] };
            var right = edges.Select(i => (IElement)strokes[i]).Concat(roots).ToArray();
            var equation = new Relation(new Relation(N.S(component), N.S(inner.Concat(outside).ToArray())),
                new Relation(N.S(edges.Select(i => (IElement)strokes[i]).ToArray()), N.S(roots)));
            var correspondence = N.S(left.Zip(right).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray());
            var proof = new Relation(equation, correspondence); euler.Add(proof);
            if (outside.Length != 1 || left.Length != right.Length) no.Add(proof);
        }
        Faces = N.S(faces.ToArray()); Exterior = N.S(exterior.ToArray()); Rotation = N.S(rotations.ToArray()); Euler = N.S(euler.ToArray());
        Blocked = N.S(no.ToArray()); Unresolved = N.S(pending.ToArray());
        read = new(Surface, new Relation(Faces, new Relation(Exterior, new Relation(Rotation,
            new Relation(Euler, new Relation(Blocked, Unresolved))))));
    }

    public Relation Evaluate() => read;
    public static Relation Oriented(Relation actualStroke, IElement from, IElement to) => new(actualStroke, new Relation(from, to));
    public static Relation Boundary(params Relation[] participations) => N.S(participations);
    public bool TryReadFace(Relation face, out Relation boundary, out Relation vertices)
    {
        boundary = vertices = null!;
        if (!IsComplete || !N.E(Faces).Concat(N.E(Exterior)).Any(actual => ReferenceEquals(actual, face)) ||
            face.Left is not Relation { Right: Relation actualBoundary } || face.Right is not Relation { Left: Relation actualVertices }) return false;
        boundary = actualBoundary; vertices = actualVertices; return true;
    }

    internal static Relation Inverse(QualitativeMotionFrame frame, Relation word)
    {
        var moves = new List<IElement>();
        foreach (var item in N.E(word))
        {
            if (item is not Relation move || N.E(frame.Opposites).OfType<Relation>().SingleOrDefault(row => ReferenceEquals(row.Left, move.Left)) is not Relation opposite)
                return StructuralConstraint.Open(moves.ToArray());
            moves.Add(QualitativeMotionFrame.Move(opposite.Right, move.Right));
        }
        return N.Read(word, out _) ? N.S(moves.ToArray()) : StructuralConstraint.Open(moves.ToArray());
    }

    private static IElement[][] Components(IElement[] sites, (IElement From, IElement To)[] edges)
    {
        var seen = new HashSet<IElement>(N.Participants.Instance); var result = new List<IElement[]>();
        foreach (var seed in sites)
        {
            if (!seen.Add(seed)) continue;
            var found = new List<IElement>(); var queue = new Queue<IElement>(); queue.Enqueue(seed);
            while (queue.TryDequeue(out var at))
            {
                found.Add(at);
                foreach (var (a, b) in edges)
                {
                    var next = a is not null && N.Same(a, at) ? b : b is not null && N.Same(b, at) ? a : null;
                    if (next is not null && seen.Add(next)) queue.Enqueue(next);
                }
            }
            result.Add(found.ToArray());
        }
        return result.ToArray();
    }
}

/// <summary>A non-draining boundary-arity question. Slots are actual connected
/// required occurrences, not a numeric side-count tag. Different questions keep
/// distinct source/focus/context while sharing the same unchanged scaffold.</summary>
public sealed class ScaffoldQuestion
{
    // Surface = (source.Surface -> requested-boundary-slots) -> context
    // match = actual-face -> (requested-boundary-slots -> Closed(slot -> actual oriented edge))
    // read = Surface -> (selected -> (complement -> (exterior -> unresolved)))
    public ScaffoldQuestionScaffold Source { get; }
    public Relation BoundarySlots { get; }
    public IElement Context { get; }
    public Relation Surface { get; }
    public Relation Selected { get; }
    public Relation Complement { get; }
    public Relation Unresolved { get; }
    private readonly Relation read;

    public ScaffoldQuestion(ScaffoldQuestionScaffold source, Relation boundarySlots, IElement context)
    {
        Source = source; BoundarySlots = boundarySlots; Context = context;
        Surface = new(new Relation(source.Surface, boundarySlots), context);
        var yes = new List<IElement>(); var other = new List<IElement>(); var pending = new List<IElement>();
        var valid = N.Read(boundarySlots, out var slots) && N.Distinct(slots) && slots.Length >= 3 && slots.All(slot => slot is Placeholder { IsInverse: false });
        if (!valid) pending.Add(boundarySlots);
        if (!source.IsComplete) pending.Add(source.Evaluate());
        foreach (var face in N.E(source.Faces).OfType<Relation>())
        {
            if (!valid || !source.TryReadFace(face, out var boundary, out _)) { pending.Add(face); continue; }
            var parts = N.E(boundary);
            if (parts.Length == slots.Length)
                yes.Add(new Relation(face, new Relation(boundarySlots, N.S(slots.Zip(parts).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray()))));
            else other.Add(face);
        }
        Selected = N.S(yes.ToArray()); Complement = N.S(other.ToArray()); Unresolved = N.S(pending.ToArray());
        read = new(Surface, new Relation(Selected, new Relation(Complement, new Relation(source.Exterior, Unresolved))));
    }
    public Relation Evaluate() => read;
    public bool TryReadSelected(Relation match, out Relation face, out Relation boundary, out Relation correspondence)
    {
        face = boundary = correspondence = null!;
        if (!N.E(Selected).Any(actual => ReferenceEquals(actual, match)) || match.Left is not Relation actualFace ||
            match.Right is not Relation { Left: Relation slots, Right: Relation pairs } || !ReferenceEquals(slots, BoundarySlots) ||
            !Source.TryReadFace(actualFace, out boundary, out _)) return false;
        face = actualFace; correspondence = pairs; return true;
    }
}
