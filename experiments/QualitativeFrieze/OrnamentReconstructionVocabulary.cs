using System.Text;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite source-free read vocabulary compiled from independent native
/// exemplars. The entries contain role graphs and connected questions, never
/// occurrence identities, private execution words or a family truth label.</summary>
public static class OrnamentReconstructionVocabulary
{
    public sealed record Entry(string Id, string Label, string Channel, R.Query Query);
    private sealed record Motif(string Family, CommunicationPattern Pattern, CommunicationShape Shape);
    private static readonly Lazy<IReadOnlyList<Entry>> Cached = new(Build);
    public static IReadOnlyList<Entry> All => Cached.Value;

    public static IReadOnlyList<Entry> Build()
    {
        var frame = new QualitativeMotionFrame();
        var samples = OrnamentReconstructionCases.Specs.Where(spec => spec.Transform == 0 && spec.ReachRecipe == 0 && spec.Repeats == 1)
            .Select(spec => spec.Create()).ToArray();
        var pool = new List<Motif>();
        foreach (var sample in samples)
        {
            var induction = new QualitativeMotifInduction(sample.Observation, maxCycleEdges: 6,
                proposalCapacity: 4096, maxWalkEdges: 3, retainedCapacity: 1024);
            foreach (var family in induction.Families)
            {
                var pattern = Clone(new(sample.Observation.Frame, family.Definition), frame);
                CommunicationShape.TryRead(pattern.Surface, out var shape);
                pool.Add(new(sample.Spec.Family, pattern, shape));
            }
        }
        var unique = pool.DistinctBy(item => item.Pattern.Signature).ToArray();
        var entries = new List<Entry>(); var keys = new HashSet<string>(StringComparer.Ordinal);

        // Balance the finite vocabulary across structural grades. These quotas
        // choose experimental questions, not which output family is true.
        foreach (var (degree, capacity) in new[] { (1, 4), (2, 4), (3, 4), (4, 1), (6, 1) })
            foreach (var item in unique.Where(item => item.Pattern.Kind == "junction" && item.Shape.Edges.Length == degree)
                .OrderBy(item => item.Pattern.Signature, StringComparer.Ordinal).Take(capacity)) AddMotif(item);
        foreach (var (length, capacity) in new[] { (3, 2), (4, 2), (6, 1) })
            foreach (var item in unique.Where(item => item.Pattern.Kind == "cycle" && item.Shape.Edges.Length == length)
                .OrderBy(item => CyclePriority(item.Family)).ThenBy(item => item.Pattern.Signature, StringComparer.Ordinal).Take(capacity)) AddMotif(item);
        foreach (var (length, capacity) in new[] { (2, 3), (3, 2) })
            foreach (var item in unique.Where(item => item.Pattern.Kind == "walk" && item.Shape.Edges.Length == length)
                .OrderBy(item => item.Pattern.Signature, StringComparer.Ordinal).Take(capacity)) AddMotif(item);

        var whole = new List<(string Family, CommunicationPattern Pattern)>();
        foreach (var sample in samples)
        {
            var input = Input(sample);
            foreach (var group in E(sample.MechanicalGroups).Cast<Relation>())
                if (R.TryDescribeGroup(input, group, out var description, out _)) whole.Add((sample.Spec.Family, Clone(description, frame)));
        }
        var priority = new[] { "SQ-PATH", "DIAMOND", "TRIANGLES", "Y", "H", "ZIGZAG", "WAVE", "STAIR", "MEANDER", "BRICK",
            "HONEYCOMB", "LOZENGE", "MEANDER-PAIR", "DIAMOND-NODES", "ALTERNATING-CELLS", "MERGED-CELLS", "LOZENGE-ROSETTE" };
        var wholeChosen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var family in priority)
        {
            var candidate = whole.FirstOrDefault(item => item.Family == family && !wholeChosen.Contains(item.Pattern.Signature));
            if (candidate.Pattern is not null) AddWhole(candidate);
        }
        foreach (var candidate in whole)
        {
            if (wholeChosen.Count >= 20) break;
            if (!wholeChosen.Contains(candidate.Pattern.Signature)) AddWhole(candidate);
        }

        var square = FindCycle("SQ-PATH", 4); var diamond = FindCycle("DIAMOND", 4);
        var triangle = FindCycle("TRIANGLES", 3); var hexagon = FindCycle("HONEYCOMB", 6);
        var straight = unique.First(item => item.Pattern.Kind == "walk" && item.Shape.Edges.Length == 2 &&
            item.Shape.Headings.All(mask => mask == 1)).Pattern;
        Two("Two distinct triangular loops", triangle, distinct: true);
        Three("Three distinct triangular loops", triangle);
        Two("Two axial loops meet at a site without sharing an edge", square, distinct: true, sharedSites: true, disjointEdges: true);
        Two("Two axial loops share an actual edge", square, distinct: true, sharedEdges: true);
        Two("Two axial loops are site-disjoint", square, distinct: true, disjointSites: true);
        Two("Two diagonal loops meet at a site without sharing an edge", diamond, distinct: true, sharedSites: true, disjointEdges: true);
        Two("Two six-edge loops share an actual edge", hexagon, distinct: true, sharedEdges: true);
        Contact("A two-edge straight path contacts an axial loop through a path endpoint", straight, square, endpoint: true);
        Contact("A two-edge straight path contacts an axial loop through its internal site", straight, square, endpoint: false);
        {
            var a = new C.Role(triangle); var b = new C.Role(square);
            AddJoint(new("A triangular and an axial loop meet without a common edge", [a, b],
                sharedSites: S(C.Pair(a, b)), disjointEdges: S(C.Pair(a, b))));
        }

        var horizontal = Single(1); var vertical = Single(4); var rising = Single(5); var falling = Single(9);
        Add(R.Query.Crossing("An axial horizontal line crosses an axial vertical line", horizontal, vertical));
        Add(R.Query.Crossing("An ascending diagonal crosses a descending diagonal", rising, falling));
        Add(R.Query.Crossing("An axial horizontal line crosses a diagonal line", horizontal, rising));
        Add(R.Query.Continuation("Two axial horizontal segments have declared continuation", horizontal, horizontal));
        Add(R.Query.Continuation("Two axial vertical segments have declared continuation", vertical, vertical));
        Add(R.Query.Continuation("Two ascending diagonal segments have declared continuation", rising, rising));
        return entries.AsReadOnly();

        void AddMotif(Motif item) => Add(R.Query.Motif(MotifLabel(item), item.Pattern));
        void AddWhole((string Family, CommunicationPattern Pattern) item)
        {
            if (!wholeChosen.Add(item.Pattern.Signature)) return;
            CommunicationShape.TryRead(item.Pattern.Surface, out var shape);
            var kind = item.Pattern.Kind == "junction" ? "path" : item.Pattern.Kind;
            var label = $"One whole mechanical {kind}: {HeadingSequence(shape.Headings)}";
            Add(R.Query.WholePath(label, item.Pattern));
        }
        void Add(R.Query query)
        {
            if (!query.IsValid) throw new InvalidOperationException("A compiled experimental question must have a valid connected read surface.");
            var key = query.Pattern is not null && CommunicationShape.TryRead(query.Pattern, out var shape)
                ? query.Kind + ":" + new CommunicationPattern(frame, shape.Definition).Signature : NativeKey(query.Surface);
            if (!keys.Add(key)) return;
            entries.Add(new($"Q{entries.Count + 1:000}", query.Name, query.Kind, query));
        }
        void AddJoint(C.Question question) => Add(R.Query.Joint(question.Name, question));
        void Two(string label, CommunicationPattern pattern, bool distinct = false, bool disjointSites = false,
            bool sharedSites = false, bool sharedEdges = false, bool disjointEdges = false)
        {
            var a = new C.Role(pattern); var b = new C.Role(pattern); var pair = C.Pair(a, b);
            AddJoint(new(label, [a, b], distinct: distinct ? S(pair) : S(), disjointSites: disjointSites ? S(pair) : S(),
                sharedSites: sharedSites ? S(pair) : S(), sharedEdges: sharedEdges ? S(pair) : S(), disjointEdges: disjointEdges ? S(pair) : S()));
        }
        void Three(string label, CommunicationPattern pattern)
        {
            var a = new C.Role(pattern); var b = new C.Role(pattern); var c = new C.Role(pattern);
            AddJoint(new(label, [a, b, c], distinct: S(C.Pair(a, b), C.Pair(a, c), C.Pair(b, c))));
        }
        void Contact(string label, CommunicationPattern path, CommunicationPattern loop, bool endpoint)
        {
            CommunicationShape.TryRead(path.Surface, out var pathShape); CommunicationShape.TryRead(loop.Surface, out var loopShape);
            var a = new C.Role(path); var b = new C.Role(loop);
            var selected = endpoint ? new[] { pathShape.Ends[0].Left, pathShape.Ends[^1].Right } : new[] { pathShape.Ends[0].Right };
            AddJoint(new(label, [a, b], disjointEdges: S(C.Pair(a, b)),
                sharedSelectedSites: S(C.Pair(C.Select(a, selected), C.Select(b, loopShape.Sites)))));
        }
        CommunicationPattern FindCycle(string family, int edges) => pool.First(item => item.Family == family &&
            item.Pattern.Kind == "cycle" && item.Shape.Edges.Length == edges).Pattern;
        CommunicationPattern Single(int mask)
        {
            foreach (var sample in samples)
                foreach (var segment in E(sample.Observation.Segments).Cast<Relation>())
                    if (R.TryDescribeEdge(Input(sample), segment, out var pattern, out _) &&
                        CommunicationShape.TryRead(pattern.Surface, out var shape) && shape.Headings is [var heading] && heading == mask)
                        return Clone(pattern, frame);
            throw new InvalidOperationException("The exemplar corpus must expose the selected native heading participation.");
        }
    }

    private static int CyclePriority(string family) => family switch { "SQ-PATH" => 0, "DIAMOND" => 1, "TRIANGLES" => 2, "HONEYCOMB" => 3, _ => 4 };
    private static string MotifLabel(Motif item) => item.Pattern.Kind == "junction"
        ? $"A {item.Shape.Edges.Length}-arm site: {HeadingSequence(item.Shape.Headings)}"
        : $"A {item.Shape.Edges.Length}-edge {(item.Shape.IsCycle ? "closed loop" : "path")}: {HeadingSequence(item.Shape.Headings)}";
    private static string HeadingSequence(IEnumerable<int> masks) => string.Join(" · ", masks.Select(mask =>
        (mask & 4) != 0 ? (mask & 1) != 0 ? "up-right" : (mask & 2) != 0 ? "up-left" : "up" :
        (mask & 8) != 0 ? (mask & 1) != 0 ? "down-right" : (mask & 2) != 0 ? "down-left" : "down" :
        (mask & 1) != 0 ? "right" : "left"));

    private static CommunicationPattern Clone(CommunicationPattern source, QualitativeMotionFrame frame)
    {
        if (!CommunicationShape.TryRead(source.Surface, out var shape)) throw new ArgumentException("A reusable pattern needs its exact native role graph.", nameof(source));
        var directions = new IElement[] { frame.Right, frame.Left, frame.Up, frame.Down };
        var sites = shape.Sites.Select(_ => (IElement)new Placeholder()).ToArray();
        IElement Site(IElement old) => sites[Array.FindIndex(shape.Sites, site => ReferenceEquals(site, old))];
        var rows = new List<IElement>(); var uses = new List<IElement>();
        for (var index = 0; index < shape.Edges.Length; index++)
        {
            var ends = new Relation(Site(shape.Ends[index].Left), Site(shape.Ends[index].Right));
            var heading = S(directions.Where((_, bit) => (shape.Headings[index] & (1 << bit)) != 0).ToArray());
            var edge = new Relation(new Placeholder(), new Relation(ends, heading)); rows.Add(edge); uses.Add(new Relation(edge.Left, ends));
        }
        var definition = new Relation(new Relation(frame.Surface, new Relation(S(sites), S(rows.ToArray()))),
            new Relation(S(shape.Centres.Select(Site).ToArray()), shape.Centres.Length == 0 ? S(S(uses.ToArray())) : S()));
        var result = new CommunicationPattern(frame, definition);
        if (!result.IsValid) throw new InvalidOperationException("Native role and heading renaming must preserve the selected structural pattern.");
        return result;
    }

    private static string NativeKey(IElement root)
    {
        var ids = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance); var queue = new Queue<IElement>(); var text = new StringBuilder();
        int Id(IElement item) { if (ids.TryGetValue(item, out var id)) return id; id = ids.Count; ids.Add(item, id); queue.Enqueue(item); return id; }
        Id(root);
        while (queue.TryDequeue(out var item))
        {
            text.Append(ids[item]).Append(':');
            if (item is Relation relation) text.Append('R').Append(Id(relation.Left)).Append(',').Append(Id(relation.Right));
            else if (item is Placeholder placeholder) text.Append(placeholder.IsInverse ? "PI" : "PF");
            else throw new InvalidOperationException("The read vocabulary must stay pre-numeric.");
            text.Append(';');
        }
        return text.ToString();
    }

    private static R.Input Input(OrnamentReconstructionCases.Sample sample) => new(sample.Spec.Name, sample.Observation, sample.MechanicalGroups);
    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
