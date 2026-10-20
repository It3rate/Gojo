using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Actual executions that refine whole-motif contact into contact at
/// connected pattern landmarks. Authored placement remains renderer evidence;
/// readers receive only fresh incidence and qualitative headings.</summary>
public sealed class LandmarkCompositionCases
{
    // landmark selection = actual occurrence role -> Closed(pattern site roles)
    // selected contact = selection -> selection
    // Endpoint, internal path site and oriented corner are local reads of the
    // exemplar's connected graph. Their names and array positions are not the
    // source of query meaning: the selected placeholders remain connected to
    // actual incidence, ordered paths and heading faces in the native pattern.
    public IReadOnlyList<OccurrenceCompositionCases.Case> Cases { get; }
    public Relation Surface { get; }

    private LandmarkCompositionCases(IReadOnlyList<OccurrenceCompositionCases.Case> cases)
    { Cases = cases; Surface = S(cases.Select(item => (IElement)item.Surface).ToArray()); }

    public static LandmarkCompositionCases Build()
    {
        var triangle = Pattern(TriangleExemplar(), "cycle", 3);
        var square = Pattern(SquareExemplar(), "cycle", 4);
        var straight = Pattern(PathExemplar(false), "walk", 2);
        var diagonal = Pattern(PathExemplar(true), "walk", 2);
        var cases = new List<OccurrenceCompositionCases.Case>();

        var loop = new C.Role(triangle); var path = new C.Role(straight);
        var contact = new C.Question("Does a separate two-edge straight path share any site with a three-edge loop?",
            [loop, path], sharedSites: S(C.Pair(loop, path)), disjointEdges: S(C.Pair(loop, path)));
        var endpoint = new C.Question("Does an endpoint of that two-edge straight path join the three-edge loop?",
            [loop, path], disjointEdges: S(C.Pair(loop, path)),
            sharedSelectedSites: S(C.Pair(C.Select(path, Endpoints(straight)), C.Select(loop, Sites(triangle)))));
        cases.Add(new("L01", "Contact somewhere is not endpoint contact",
            "A path can join a triangle at its internal site or at an endpoint. The whole-motif contact question accepts both. Selecting the endpoint placeholders in the same reusable path pattern rejects the middle-contact alternative. Coincident unjoined sites remain separate.",
            "First read whole-motif contact, then refine the contact to the path's actual endpoint roles.",
            OneLoopContact("endpoint"), [OneLoopContact("middle"), OneLoopContact("endpoint"), OneLoopContact("unjoined")],
            [contact, endpoint]));

        var bridgeQuestions = BridgeQuestions(triangle, straight);
        cases.Add(new("L02", "A bridge must contact through both ends",
            "Both connected alternatives let a two-edge path touch two site-disjoint loops. One uses its internal site and one endpoint; the other uses both endpoints. Requiring the two separately selected endpoint roles to contact different loops distinguishes these structures in one joint assignment.",
            "Keep the coarse three-role contact claim, then require each particular endpoint to join a different loop.",
            TwoLoopContact(true), [TwoLoopContact(false), TwoLoopContact(true)], bridgeQuestions));

        var squareRole = new C.Role(square); var diagonalRole = new C.Role(diagonal);
        var cornerContact = new C.Question("Does the diagonal two-edge path share any site with a four-edge loop?",
            [squareRole, diagonalRole], sharedSites: S(C.Pair(squareRole, diagonalRole)),
            disjointEdges: S(C.Pair(squareRole, diagonalRole)));
        var shape = Read(square);
        var selectedCorner = Corner(square, shape.Directions[0], shape.Directions[2]);
        var corner = new C.Question("Does a path endpoint join the loop corner whose two sides continue rightward and upward?",
            [squareRole, diagonalRole], disjointEdges: S(C.Pair(squareRole, diagonalRole)),
            sharedSelectedSites: S(C.Pair(C.Select(squareRole, selectedCorner),
                C.Select(diagonalRole, Endpoints(diagonal)))));
        cases.Add(new("L03", "A corner can be specified without a coordinate",
            "The closed loop supplies four corner roles. The question selects the role where the loop's incident sides extend along the rightward and upward frame faces. Both alternatives have an endpoint join, but at different corners. The selected corner is a connected incidence-and-heading read, not a coordinate or corner-name tag.",
            "Refine contact to one oriented corner using its actual pattern participant and local heading faces.",
            SquareContact(true), [SquareContact(false), SquareContact(true)], [cornerContact, corner]));

        cases.Add(new("L04", "An internal site is not a measured midpoint",
            "These bridges use the same two endpoint joins. One divides its uncalibrated path reach into equal formal words; the other uses different formal words on its two edges. The advertised observations omit reach and calibration, so both answer the endpoint questions alike. The internal site is between the endpoints topologically; this vocabulary cannot certify that it is halfway between them.",
            "Preserve endpoint agreement while leaving equal subdivision and measured midpoint undecided.",
            TwoLoopContact(true, equalSplit: true),
            [TwoLoopContact(true, equalSplit: false), TwoLoopContact(true, equalSplit: true)],
            BridgeQuestions(triangle, straight), expectedAmbiguity: true));
        return new(cases);
    }

    private static C.Question[] BridgeQuestions(CommunicationPattern triangle, CommunicationPattern straight)
    {
        var a = new C.Role(triangle); var b = new C.Role(triangle); var path = new C.Role(straight);
        var separateLoops = S(C.Pair(a, b));
        var independentEdges = S(C.Pair(a, path), C.Pair(path, b));
        var coarse = new C.Question("Does a two-edge path share a site with each of two site-disjoint three-edge loops?",
            [a, path, b], distinct: separateLoops, disjointSites: separateLoops,
            sharedSites: S(C.Pair(a, path), C.Pair(path, b)), disjointEdges: independentEdges);
        var shape = Read(straight);
        var first = shape.Ends[0].Left; var last = shape.Ends[^1].Right;
        var exact = new C.Question("Does each different endpoint of that path join a different one of the two loops?",
            [a, path, b], distinct: separateLoops, disjointSites: separateLoops, disjointEdges: independentEdges,
            sharedSelectedSites: S(C.Pair(C.Select(path, first), C.Select(a, Sites(triangle))),
                C.Pair(C.Select(path, last), C.Select(b, Sites(triangle)))));
        return [coarse, exact];
    }

    /// <summary>Public generators supply independently executed instances to
    /// tests and budget controls. Contact is declared through actual published
    /// site ports. Construction coordinates never create joins automatically.</summary>
    public static StructuralEvidenceCases.Sample OneLoopContact(string contact)
    {
        if (contact is not ("endpoint" or "middle" or "unjoined")) throw new ArgumentOutOfRangeException(nameof(contact));
        var a = new LatticeLadderAuthor(); var loop = TrianglePath(a, 0);
        var middle = contact == "middle";
        var path = LatticeLadderAuthor.Path(a.Word(middle ? 0 : 2, 2), a.Word(2, 0), a.Word(2, 0));
        return Execute(contact switch { "middle" => "Loop joined at the path's internal site",
            "endpoint" => "Loop joined at a path endpoint", _ => "Coincident endpoint remains unjoined" },
            a, [loop, path], [loop], contact == "unjoined" ? [] : [(0, 1, 1, middle ? 1 : 0)]);
    }

    public static StructuralEvidenceCases.Sample TwoLoopContact(bool bothEndpoints, bool equalSplit = true)
    {
        var a = new LatticeLadderAuthor(); var first = TrianglePath(a, 0); var second = TrianglePath(a, 8);
        var start = bothEndpoints ? 2 : -2;
        var firstReach = bothEndpoints ? equalSplit ? 4 : 2 : 4;
        var lastReach = 10 - (start + firstReach);
        var path = LatticeLadderAuthor.Path(a.Word(start, 2), a.Word(firstReach, 0), a.Word(lastReach, 0));
        var name = !bothEndpoints ? "Two loops contacted by a path's middle and endpoint" : equalSplit
            ? "Two loops joined through both path endpoints, equal formal subdivision"
            : "Two loops joined through both path endpoints, unequal formal subdivision";
        return Execute(name, a, [first, second, path], [first, second],
            [(0, 1, 2, bothEndpoints ? 0 : 1), (1, 1, 2, 2)]);
    }

    public static StructuralEvidenceCases.Sample SquareContact(bool selectedCorner)
    {
        var a = new LatticeLadderAuthor(); var loop = SquarePath(a);
        var top = selectedCorner ? 0 : 2;
        var path = LatticeLadderAuthor.Path(a.Word(-2, top - 2), a.Word(1, 1), a.Word(1, 1));
        return Execute(selectedCorner ? "Path endpoint joins the rightward/upward loop corner"
            : "Path endpoint joins the rightward/downward loop corner", a, [loop, path], [loop],
            [(0, selectedCorner ? 0 : 3, 1, 2)]);
    }

    private static StructuralEvidenceCases.Sample TriangleExemplar()
    {
        var a = new LatticeLadderAuthor(); var path = TrianglePath(a, 0);
        return Execute("Independent triangle template", a, [path], [path], []);
    }

    private static StructuralEvidenceCases.Sample SquareExemplar()
    {
        var a = new LatticeLadderAuthor(); var path = SquarePath(a);
        return Execute("Independent square template", a, [path], [path], []);
    }

    private static StructuralEvidenceCases.Sample PathExemplar(bool diagonal)
    {
        var a = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(1, diagonal ? 1 : 0), a.Word(1, diagonal ? 1 : 0));
        return Execute(diagonal ? "Independent two-edge diagonal template" : "Independent two-edge straight template",
            a, [path], [], []);
    }

    private static Relation TrianglePath(LatticeLadderAuthor a, int x) =>
        LatticeLadderAuthor.Path(a.Word(x, 0), a.Word(2, 2), a.Word(2, -2), a.Word(-4, 0));

    private static Relation SquarePath(LatticeLadderAuthor a) =>
        LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(2, 0), a.Word(0, 2), a.Word(-2, 0), a.Word(0, -2));

    private static CommunicationPattern Pattern(StructuralEvidenceCases.Sample exemplar, string kind, int edges)
    {
        var family = exemplar.Induction.Families.Single(item => item.Kind == kind && item.SegmentCount == edges);
        return new(exemplar.Observation.Frame, family.Definition);
    }

    private static CommunicationShape Read(CommunicationPattern pattern) =>
        CommunicationShape.TryRead(pattern.Surface, out var shape) ? shape :
            throw new InvalidOperationException("A landmark must belong to a readable connected exemplar pattern.");

    private static IElement[] Sites(CommunicationPattern pattern) => Read(pattern).Sites;
    private static IElement[] Endpoints(CommunicationPattern pattern)
    {
        var shape = Read(pattern);
        if (shape.IsCycle || shape.Centres.Length != 0) throw new InvalidOperationException("Expected an open path template.");
        return [shape.Ends[0].Left, shape.Ends[^1].Right];
    }

    private static IElement Corner(CommunicationPattern pattern, IElement first, IElement second)
    {
        var shape = Read(pattern);
        return shape.Sites.Single(site =>
        {
            var outward = shape.Edges.SelectMany(edge =>
            {
                var data = (Relation)edge.Right; var ends = (Relation)data.Left;
                var directions = E((Relation)data.Right);
                if (ReferenceEquals(ends.Left, site)) return directions;
                if (!ReferenceEquals(ends.Right, site)) return [];
                var oppositeScope = (Relation)((Relation)shape.Frame.Right).Left;
                return directions.Select(direction => E(oppositeScope).Cast<Relation>()
                    .Single(pair => ReferenceEquals(pair.Left, direction)).Right).ToArray();
            }).ToArray();
            return outward.Length == 2 && outward.Any(direction => ReferenceEquals(direction, first)) &&
                outward.Any(direction => ReferenceEquals(direction, second));
        });
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
        if (!join.TryReadPassed(join.Evaluate())) throw new InvalidOperationException("All landmark case port joins must be admitted.");
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
            var quotient = E(execution.Current.Join.SiteMap).Cast<Relation>()
                .Single(row => ReferenceEquals(row.Left, sourceSite)).Right;
            return (quotient, publication);
        }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
