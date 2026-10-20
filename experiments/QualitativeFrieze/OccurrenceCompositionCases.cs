using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Small actual executions that separate motif existence from the
/// relations between occurrences. Names and illustrative positions are not
/// supplied to the occurrence reader.</summary>
public sealed class OccurrenceCompositionCases
{
    // case = private fresh execution -> (public fresh alternatives -> questions)
    // question roles refer only to reusable, source-free native patterns.
    // Publication retains site/edge identity and headings, but deliberately
    // omits placement words, distances and construction ancestry.
    public sealed class Case
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string QuestionPurpose { get; }
        public StructuralEvidenceCases.Sample Source { get; }
        public IReadOnlyList<StructuralEvidenceCases.Sample> Candidates { get; }
        public IReadOnlyList<OccurrenceComposition.Question> Queries { get; }
        public bool ExpectedAmbiguity { get; }
        public Relation Surface { get; }

        internal Case(string id, string name, string description, string questionPurpose,
            StructuralEvidenceCases.Sample source, StructuralEvidenceCases.Sample[] candidates,
            OccurrenceComposition.Question[] queries, bool expectedAmbiguity = false)
        {
            Id = id; Name = name; Description = description; QuestionPurpose = questionPurpose;
            Source = source; Candidates = candidates; Queries = queries;
            ExpectedAmbiguity = expectedAmbiguity;
            Surface = new(source.Surface, new Relation(S(candidates.Select(item => (IElement)item.Surface).ToArray()),
                S(queries.Select(item => (IElement)item.Surface).ToArray())));
        }
    }

    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    internal OccurrenceCompositionCases(IReadOnlyList<Case> cases)
    { Cases = cases; Surface = S(cases.Select(item => (IElement)item.Surface).ToArray()); }

    public static OccurrenceCompositionCases Build()
    {
        // Separate exemplars expose reusable definitions rather than occurrence
        // identity, a source inventory, or an author's recipe.
        var trianglePattern = Pattern(DisjointTriangles(1), "cycle", 3);
        var squarePattern = Pattern(Squares("disjoint"), "cycle", 4);
        var bridgePattern = Pattern(BridgeExemplar(), "walk", 2);
        var cases = new List<Case>();

        var one = new OccurrenceComposition.Role(trianglePattern);
        var twoA = new OccurrenceComposition.Role(trianglePattern);
        var twoB = new OccurrenceComposition.Role(trianglePattern);
        var oneTriangle = new OccurrenceComposition.Question("Does a closed three-edge loop exist?", [one]);
        var twoTriangles = new OccurrenceComposition.Question("Are there at least two distinct closed three-edge loops?",
            [twoA, twoB], distinct: S(OccurrenceComposition.Pair(twoA, twoB)));
        var threeA = new OccurrenceComposition.Role(trianglePattern);
        var threeB = new OccurrenceComposition.Role(trianglePattern);
        var threeC = new OccurrenceComposition.Role(trianglePattern);
        var threeTriangles = new OccurrenceComposition.Question("Are there at least three distinct closed three-edge loops?",
            [threeA, threeB, threeC], distinct: S(OccurrenceComposition.Pair(threeA, threeB),
                OccurrenceComposition.Pair(threeA, threeC), OccurrenceComposition.Pair(threeB, threeC)));
        cases.Add(new("C01", "One motif is not two occurrences",
            "One, two and three triangles answer the same old existential motif vocabulary. Separate role slots with explicit distinct-occurrence constraints ask stronger questions. At least two does not establish exactly two; a third question distinguishes three copies. Reversing a traversal cannot supply another copy.",
            "Confirm the motif, require two distinct footprints, then test the three-occurrence alternative.",
            DisjointTriangles(2), [DisjointTriangles(1), DisjointTriangles(2), DisjointTriangles(3)],
            [oneTriangle, twoTriangles, threeTriangles]));

        var touchA = new OccurrenceComposition.Role(squarePattern);
        var touchB = new OccurrenceComposition.Role(squarePattern);
        var touchQuestion = new OccurrenceComposition.Question("Do two distinct four-edge loops share a site without sharing an edge?",
            [touchA, touchB], distinct: S(OccurrenceComposition.Pair(touchA, touchB)),
            sharedSites: S(OccurrenceComposition.Pair(touchA, touchB)),
            disjointEdges: S(OccurrenceComposition.Pair(touchA, touchB)));
        cases.Add(new("C02", "Touching is a relation between motifs",
            "Two square contours may be separate, meet at an explicitly joined corner, or share an actual boundary edge. A shared site and disjoint edges select corner contact without inventing a measured distance.",
            "Compose two contour roles with shared-site and disjoint-edge obligations.",
            Squares("site"), [Squares("disjoint"), Squares("edge"), Squares("site")], [touchQuestion]));

        var seamA = new OccurrenceComposition.Role(squarePattern);
        var seamB = new OccurrenceComposition.Role(squarePattern);
        var seamQuestion = new OccurrenceComposition.Question("Do two distinct four-edge loops share an actual edge?",
            [seamA, seamB], distinct: S(OccurrenceComposition.Pair(seamA, seamB)),
            sharedEdges: S(OccurrenceComposition.Pair(seamA, seamB)));
        cases.Add(new("C03", "A common seam is not a duplicated line",
            "The adjacent contours reuse one actual edge. The common boundary is drawn once; the neighbour's three remaining sides are attached through exposed endpoint ports. Equal positions alone never create the shared edge.",
            "Require two different contour footprints with a common actual edge identity.",
            Squares("edge"), [Squares("disjoint"), Squares("site"), Squares("edge")], [seamQuestion]));

        var left = new OccurrenceComposition.Role(squarePattern);
        var bridge = new OccurrenceComposition.Role(bridgePattern);
        var right = new OccurrenceComposition.Role(squarePattern);
        var bridgeQuestion = new OccurrenceComposition.Question("Does a separate two-edge path share a site with each of two site-disjoint four-edge loops?",
            [left, bridge, right], distinct: S(OccurrenceComposition.Pair(left, right)),
            disjointSites: S(OccurrenceComposition.Pair(left, right)),
            sharedSites: S(OccurrenceComposition.Pair(left, bridge), OccurrenceComposition.Pair(bridge, right)),
            disjointEdges: S(OccurrenceComposition.Pair(left, bridge), OccurrenceComposition.Pair(bridge, right)));
        cases.Add(new("C04", "One path contacts two motifs",
            "Both alternatives draw two loops and an intervening two-edge path in the same positions. Only one accepts the endpoint joins. A third motif role tests contact with both loops. The query checks shared sites anywhere in these motifs; it does not specifically select the path endpoints.",
            "Compose three local patterns through shared-site obligations; keep the endpoint-specific limit explicit.",
            BridgedSquares(true), [BridgedSquares(false), BridgedSquares(true)], [bridgeQuestion]));

        var near = new OccurrenceComposition.Role(trianglePattern);
        var far = new OccurrenceComposition.Role(trianglePattern);
        var arrangementQuestion = new OccurrenceComposition.Question("Are two distinct three-edge loops site-disjoint?",
            [near, far], distinct: S(OccurrenceComposition.Pair(near, far)),
            disjointSites: S(OccurrenceComposition.Pair(near, far)));
        cases.Add(new("C05", "Topology does not supply spacing",
            "The source and alternatives place two triangles at different separations. The advertised port deliberately publishes the same incidence and heading information in each case. Both remain compatible: no spacing, period or relative-order service has been exposed.",
            "Keep the coarse two-motif read and preserve the unasked arrangement ambiguity.",
            DisjointTriangles(2, 7), [DisjointTriangles(2, 4), DisjointTriangles(2, 7)], [arrangementQuestion], true));

        return new(cases);
    }

    /// <summary>Fresh independently executed copies for controlled scaling.
    /// Gap affects only authored placement; the published reader receives no
    /// coordinate or distance. Count is construction control, not a motif tag.</summary>
    public static StructuralEvidenceCases.Sample DisjointTriangles(int count, int gap = 4)
    {
        if (count < 1 || gap < 3) throw new ArgumentOutOfRangeException(nameof(count));
        var author = new LatticeLadderAuthor();
        var paths = Enumerable.Range(0, count).Select(index => TrianglePath(author, index * gap)).ToArray();
        var caption = count switch { 1 => "One triangle contour", 2 => "Two separate triangle contours",
            3 => "Three separate triangle contours", _ => $"{count} separate triangle contours" };
        return Execute(caption, author, paths, paths, []);
    }

    /// <summary>Coincident placement remains separate identity: no join is
    /// declared merely because two executions have equal formal positions.</summary>
    public static StructuralEvidenceCases.Sample CoincidentTriangles(int count)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        var author = new LatticeLadderAuthor();
        var paths = Enumerable.Range(0, count).Select(_ => TrianglePath(author, 0)).ToArray();
        return Execute($"{count} coincident unjoined triangle contours", author, paths, paths, []);
    }

    private static Relation TrianglePath(LatticeLadderAuthor author, int x) =>
        LatticeLadderAuthor.Path(author.Word(x, 0), author.Word(1, 2), author.Word(1, -2), author.Word(-2, 0));

    private static Relation SquarePath(LatticeLadderAuthor author, int x, int y) =>
        LatticeLadderAuthor.Path(author.Word(x, y), author.Word(2, 0), author.Word(0, 2),
            author.Word(-2, 0), author.Word(0, -2));

    private static StructuralEvidenceCases.Sample Squares(string contact)
    {
        var a = new LatticeLadderAuthor();
        var first = SquarePath(a, 0, 0);
        if (contact == "edge")
        {
            // Three new sides plus the first contour's actual right edge.
            var second = LatticeLadderAuthor.Path(a.Word(2, 0), a.Word(2, 0), a.Word(0, 2), a.Word(-2, 0));
            return Execute("Two contours sharing one actual boundary edge", a, [first, second], [first],
                [(0, 1, 1, 0), (0, 2, 1, 3)]);
        }
        var other = contact == "site" ? SquarePath(a, 2, 2) : SquarePath(a, 4, 0);
        return Execute(contact == "site" ? "Two contours joined at one corner" : "Two separate four-edge contours",
            a, [first, other], [first, other], contact == "site" ? [(0, 2, 1, 0)] : []);
    }

    private static StructuralEvidenceCases.Sample BridgeExemplar()
    {
        var a = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(1, 0), a.Word(1, 0));
        return Execute("Separate straight two-edge path", a, [path], [], []);
    }

    private static StructuralEvidenceCases.Sample BridgedSquares(bool joined)
    {
        var a = new LatticeLadderAuthor();
        var first = SquarePath(a, 0, 0); var other = SquarePath(a, 4, 0);
        var bridge = LatticeLadderAuthor.Path(a.Word(2, 0), a.Word(1, 0), a.Word(1, 0));
        return Execute(joined ? "Two contours joined by a two-edge bridge" : "Bridge endpoints touch without joining",
            a, [first, other, bridge], [first, other], joined ? [(0, 1, 2, 0), (1, 0, 2, 2)] : []);
    }

    private static CommunicationPattern Pattern(StructuralEvidenceCases.Sample exemplar, string kind, int edges)
    {
        var family = exemplar.Induction.Families.Single(item => item.Kind == kind && item.SegmentCount == edges);
        return new(exemplar.Observation.Frame, family.Definition);
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
        if (!join.TryReadPassed(join.Evaluate()))
            throw new InvalidOperationException("A case must accept its actual public-port joins before publication.");
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
