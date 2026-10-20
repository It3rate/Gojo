using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Fresh actual executions for questions about a selected occurrence
/// rather than an arbitrary replacement that happens to answer the next query.
/// Only the published incidence and headings are supplied to the readers.</summary>
public sealed class RetainedMatchCases
{
    // initial question -> actual match witness -> Closed(retained role slots)
    // follow-up question reuses those actual slots and pattern surfaces. Which
    // occurrence and site correspondence persist is supplied by the captured
    // native witness, never a caption, ordinal or private generation path.
    public sealed class Case
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string FocusLabel { get; }
        public StructuralEvidenceCases.Sample Source { get; }
        public C.Question InitialQuestion { get; }
        public C.Question FollowupQuestion { get; }
        public Relation RetainedRoles { get; }
        public Relation Surface { get; }

        internal Case(string id, string name, string description, string focusLabel,
            StructuralEvidenceCases.Sample source, C.Question initial, C.Question followup,
            Relation retainedRoles)
        {
            Id = id; Name = name; Description = description; FocusLabel = focusLabel;
            Source = source; InitialQuestion = initial; FollowupQuestion = followup; RetainedRoles = retainedRoles;
            Surface = new(source.Surface, new Relation(initial.Surface, new Relation(followup.Surface, retainedRoles)));
        }
    }

    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    private RetainedMatchCases(IReadOnlyList<Case> cases)
    { Cases = cases; Surface = S(cases.Select(item => (IElement)item.Surface).ToArray()); }

    public static RetainedMatchCases Build()
    {
        var triangle = Pattern(TriangleExemplar(), "cycle", 3);
        var straight = Pattern(PathExemplar(), "walk", 2);
        var cases = new List<Case>();

        AddContactCase("R01", "A new match can answer the wrong question",
            "The first question selects a two-edge path through a loop's upper corner at a site between the path endpoints. A separate lower path has an endpoint join. Searching afresh answers Yes using that other path; retaining the selected path answers No.",
            connectSecondLoop: false, keepLoop: false);
        AddContactCase("R02", "Keep the path while allowing a new partner",
            "The selected path contacts the first loop at a site between its endpoints, and another loop through its right endpoint. Only the path role is retained. The loop role may find a different partner, so the follow-up can establish the endpoint join without replacing the path.",
            connectSecondLoop: true, keepLoop: false);
        AddContactCase("R03", "Keep the whole original pair",
            "Both the path and its original loop are retained. That pair meets at a site between the path endpoints, so an endpoint-contact question answers No. The nearby second loop cannot silently replace the retained loop, although an unrestricted search can find an endpoint join.",
            connectSecondLoop: true, keepLoop: true);
        AddBetweenCase("R04", "Between with unequal uncalibrated steps", equalSplit: false);
        AddBetweenCase("R05", "Between with equal uncalibrated steps", equalSplit: true);
        return new(cases);

        void AddContactCase(string id, string name, string description, bool connectSecondLoop, bool keepLoop)
        {
            var loop = new C.Role(triangle); var path = new C.Role(straight);
            var initial = new C.Question("Select a two-edge straight path whose site between its endpoints joins the loop's upper corner.",
                [loop, path], disjointEdges: S(C.Pair(loop, path)),
                sharedSelectedSites: S(C.Pair(C.Select(loop, UpperCorner(triangle)), C.Select(path, Between(straight)))));
            var followup = new C.Question("Does an endpoint of the selected two-edge path join a three-edge loop?",
                [loop, path], disjointEdges: S(C.Pair(loop, path)),
                sharedSelectedSites: S(C.Pair(C.Select(path, Endpoints(straight)), C.Select(loop, Sites(triangle)))));
            cases.Add(new(id, name, description, keepLoop ? "Retained path and original loop" : "Retained two-edge path",
                ContactScene(connectSecondLoop), initial, followup,
                keepLoop ? S(loop.Slot, path.Slot) : S(path.Slot)));
        }

        void AddBetweenCase(string id, string name, bool equalSplit)
        {
            var loop = new C.Role(triangle); var path = new C.Role(straight);
            var initial = new C.Question("Select a two-edge straight path with an endpoint at the loop's upper corner.",
                [loop, path], disjointEdges: S(C.Pair(loop, path)),
                sharedSelectedSites: S(C.Pair(C.Select(loop, UpperCorner(triangle)), C.Select(path, Endpoints(straight)))));
            var followup = new C.Question("Does the site between the selected path's endpoints join a three-edge loop?",
                [loop, path], disjointEdges: S(C.Pair(loop, path)),
                sharedSelectedSites: S(C.Pair(C.Select(path, Between(straight)), C.Select(loop, Sites(triangle)))));
            cases.Add(new(id, name,
                "The path starts at one loop's upper corner and contacts a second loop at its internal site. The ordered two-edge path places that site between its endpoints. The publication carries incidence and headings, so the follow-up needs no length, equal subdivision or calibration. Only the path is retained; the loop partner may change.",
                "Retained path and its site between the endpoints", BetweenScene(equalSplit), initial, followup, S(path.Slot)));
        }
    }

    public static StructuralEvidenceCases.Sample ContactScene(bool connectSecondLoop)
    {
        var a = new LatticeLadderAuthor();
        var first = TrianglePath(a, 0, 0); var second = TrianglePath(a, 4, 2);
        var selected = LatticeLadderAuthor.Path(a.Word(0, 2), a.Word(2, 0), a.Word(2, 0));
        var distractor = LatticeLadderAuthor.Path(a.Word(-4, 0), a.Word(2, 0), a.Word(2, 0));
        var joins = new List<(int, int, int, int)> { (0, 1, 2, 1), (0, 0, 3, 2) };
        if (connectSecondLoop) joins.Add((1, 0, 2, 2));
        return Execute(connectSecondLoop ? "Selected path contacts a new loop at its endpoint"
            : "Only a different path has an endpoint join", a, [first, second, selected, distractor], [first, second], joins.ToArray());
    }

    public static StructuralEvidenceCases.Sample BetweenScene(bool equalSplit)
    {
        var a = new LatticeLadderAuthor(); var firstReach = equalSplit ? 4 : 2;
        // Both contacts use upper corners, whose incident sides are diagonal.
        // A horizontal side at the between-site would offer another valid
        // two-edge straight walk using that side instead of the intended path.
        var first = TrianglePath(a, 0, 0); var second = TrianglePath(a, firstReach, 0);
        var path = LatticeLadderAuthor.Path(a.Word(2, 2), a.Word(firstReach, 0), a.Word(8 - firstReach, 0));
        return Execute(equalSplit ? "Site between endpoints with equal formal reach words" : "Site between endpoints with unequal formal reach words",
            a, [first, second, path], [first, second], [(0, 1, 2, 0), (1, 1, 2, 1)]);
    }

    private static StructuralEvidenceCases.Sample TriangleExemplar()
    {
        var a = new LatticeLadderAuthor(); var path = TrianglePath(a, 0, 0);
        return Execute("Source-free triangle exemplar", a, [path], [path], []);
    }

    private static StructuralEvidenceCases.Sample PathExemplar()
    {
        var a = new LatticeLadderAuthor(); var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(1, 0), a.Word(1, 0));
        return Execute("Source-free straight path exemplar", a, [path], [], []);
    }

    private static Relation TrianglePath(LatticeLadderAuthor a, int x, int y) =>
        LatticeLadderAuthor.Path(a.Word(x, y), a.Word(2, 2), a.Word(2, -2), a.Word(-4, 0));

    private static CommunicationPattern Pattern(StructuralEvidenceCases.Sample exemplar, string kind, int edges)
    {
        var family = exemplar.Induction.Families.Single(item => item.Kind == kind && item.SegmentCount == edges);
        return new(exemplar.Observation.Frame, family.Definition);
    }

    private static CommunicationShape Read(CommunicationPattern pattern) =>
        CommunicationShape.TryRead(pattern.Surface, out var shape) ? shape :
            throw new InvalidOperationException("Selections require a connected native pattern.");
    private static IElement[] Sites(CommunicationPattern pattern) => Read(pattern).Sites;
    private static IElement[] Endpoints(CommunicationPattern pattern)
    {
        var shape = Read(pattern); return [shape.Ends[0].Left, shape.Ends[^1].Right];
    }
    private static IElement Between(CommunicationPattern pattern) => Read(pattern).Ends[0].Right;

    private static IElement UpperCorner(CommunicationPattern pattern)
    {
        var shape = Read(pattern);
        return shape.Sites.Single(site =>
        {
            var outward = Enumerable.Range(0, shape.Edges.Length).Where(index =>
                ReferenceEquals(shape.Ends[index].Left, site) || ReferenceEquals(shape.Ends[index].Right, site))
                .Select(index => ReferenceEquals(shape.Ends[index].Left, site) ? shape.Headings[index] : Opposite(shape.Headings[index]))
                .Order().ToArray();
            // The heading masks are a derived local read of actual native
            // faces: the selected corner has downward-left and downward-right
            // incident sides. The query stores its actual site placeholder.
            return outward.SequenceEqual(new[] { 9, 10 });
        });
        static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
    }

    private static StructuralEvidenceCases.Sample Execute(string name, LatticeLadderAuthor author,
        Relation[] paths, Relation[] closures, (int FirstPath, int FirstSite, int SecondPath, int SecondSite)[] joins)
    {
        var ports = LatticeLadderAuthor.Ports(paths);
        var program = new UnitArrangementProgram(author.Frame, name, S(paths), ports, S(closures));
        var execution = new UnitArrangementNode(name, [program], program, S());
        var declarations = joins.Select(pair =>
        {
            var first = PublishedSite(pair.FirstPath, pair.FirstSite);
            var second = PublishedSite(pair.SecondPath, pair.SecondSite);
            return (IElement)new Relation(new Relation(first.Site, second.Site),
                new Relation(first.Publication, second.Publication));
        }).ToArray();
        var join = new QualitativeGraphJoin(execution.Current.Join.Graph, execution.Current.Join.Bindings, S(declarations));
        if (!join.TryReadPassed(join.Evaluate())) throw new InvalidOperationException("Case joins must be admitted through actual ports.");
        return new(name, program, execution, join);

        (IElement Site, Relation Publication) PublishedSite(int pathIndex, int siteIndex)
        {
            var path = paths[pathIndex]; var chords = E((Relation)path.Right);
            IElement selection = siteIndex == chords.Length ? path : chords[siteIndex];
            var role = E(ports).Cast<Relation>().Single(port => port.Right is Relation pick &&
                ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selection)).Left;
            var publication = E(execution.Current.Ports).Cast<Relation>().Single(port => ReferenceEquals(port.Left, role));
            var cursor = (Relation)((Relation)publication.Right).Right;
            var sourceSite = ((Relation)cursor.Left).Left;
            var actual = E(execution.Current.Join.SiteMap).Cast<Relation>()
                .Single(row => ReferenceEquals(row.Left, sourceSite)).Right;
            return (actual, publication);
        }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
