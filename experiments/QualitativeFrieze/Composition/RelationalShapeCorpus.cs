using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze.Learning;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>One hundred independently executed native sources. Authoring may
/// select finite repeated reaches; the published receiver surface contains only
/// actual passages, shared sites, endpoint words and the declared turn basis.
/// Family names, intended completions and construction history stay in the
/// separate teacher control and must never select receiver behavior.</summary>
public static class RelationalShapeCorpus
{
    public sealed record TeacherControl(string Family, string Intent,
        NativeLatticeSources.SourceCase? CompleteTarget, Relation Surface);
    public sealed record Case(string Id, string Name, string Note,
        NativeLatticeSources.SourceCase Source, TeacherControl Control);

    private sealed record Spec(string Name, Func<LatticeLadderAuthor, Relation[]> Paths,
        string Intent = "An authored closed contour or enclosure is observed as it stands.",
        Func<LatticeLadderAuthor, Relation[]>? Complete = null);

    public static IReadOnlyList<Case> Cases()
    {
        var result = new List<Case>(100);
        void Family(string family, params Spec[] examples)
        {
            if (examples.Length != 5) throw new InvalidOperationException("A corpus family supplies five independently authored examples.");
            foreach (var example in examples) result.Add(Build($"R{result.Count + 1:000}", family, example));
        }

        Family("triangular contours",
            Shape("Upright triangle", (0, 0), (8, 0), (4, 6)),
            Shape("Right triangle", (0, 0), (7, 0), (0, 5)),
            Shape("Side-facing triangle", (0, 0), (0, 8), (6, 4)),
            Shape("Unequal triangle", (0, 0), (9, 1), (2, 7)),
            Shape("Inverted triangle", (0, 7), (8, 7), (5, 0)));
        Family("quadrilateral contours",
            Shape("Square", (0, 0), (6, 0), (6, 6), (0, 6)),
            Shape("Wide rectangle", (0, 0), (9, 0), (9, 4), (0, 4)),
            Shape("Slanted parallelogram", (0, 0), (7, 0), (10, 5), (3, 5)),
            Shape("Tapered trapezoid", (0, 0), (10, 0), (7, 6), (3, 6)),
            Shape("Unequal kite", (0, -5), (3, 0), (0, 7), (-3, 0)));
        Family("bevelled contours",
            Shape("One cut corner", (0, 0), (8, 0), (8, 5), (5, 8), (0, 8)),
            Shape("Two opposed bevels", (2, 0), (8, 0), (8, 6), (6, 8), (0, 8), (0, 2)),
            Shape("Four cut corners", (2, 0), (6, 0), (8, 2), (8, 6), (6, 8), (2, 8), (0, 6), (0, 2)),
            Shape("Long hexagonal enclosure", (0, 3), (3, 0), (9, 0), (12, 3), (9, 6), (3, 6)),
            Shape("Pointed roof enclosure", (0, 0), (8, 0), (8, 5), (4, 9), (0, 5)));
        Family("single elbow outlines",
            Shape("Slender elbow", (0, 0), (6, 0), (6, 2), (2, 2), (2, 7), (0, 7)),
            Shape("Broad elbow", (0, 0), (8, 0), (8, 3), (3, 3), (3, 8), (0, 8)),
            Shape("Upper return elbow", (0, 0), (2, 0), (2, 5), (7, 5), (7, 7), (0, 7)),
            Shape("Slanted elbow", (0, 0), (7, 0), (8, 2), (3, 2), (5, 7), (3, 7)),
            Shape("Bevelled elbow", (0, 0), (7, 0), (7, 2), (3, 2), (2, 3), (2, 7), (0, 7)));
        Family("notched contours",
            Shape("Upper notch", (0, 0), (10, 0), (10, 8), (7, 8), (7, 5), (3, 5), (3, 8), (0, 8)),
            Shape("Side notch", (0, 0), (9, 0), (9, 3), (6, 3), (6, 6), (9, 6), (9, 9), (0, 9)),
            Shape("Opposed notches", (0, 0), (4, 0), (4, 2), (6, 2), (6, 0), (10, 0), (10, 8), (6, 8), (6, 6), (4, 6), (4, 8), (0, 8)),
            Shape("Pointed inward notch", (0, 0), (10, 0), (10, 8), (7, 8), (5, 4), (3, 8), (0, 8)),
            Shape("Two upper slots", (0, 0), (12, 0), (12, 8), (10, 8), (10, 5), (8, 5), (8, 8), (4, 8), (4, 5), (2, 5), (2, 8), (0, 8)));
        Family("stepped enclosures",
            Shape("Three rising steps", (0, 0), (9, 0), (9, 2), (6, 2), (6, 4), (3, 4), (3, 6), (0, 6)),
            Shape("Four rising steps", (0, 0), (12, 0), (12, 2), (9, 2), (9, 4), (6, 4), (6, 6), (3, 6), (3, 8), (0, 8)),
            Shape("Stepped diagonal band", (0, 0), (4, 0), (4, 2), (7, 2), (7, 4), (10, 4), (10, 8), (6, 8), (6, 6), (3, 6), (3, 4), (0, 4)),
            Shape("Opposed stepped shoulders", (0, 0), (10, 0), (10, 3), (8, 3), (8, 5), (6, 5), (6, 7), (4, 7), (4, 5), (2, 5), (2, 3), (0, 3)),
            Shape("Indented staircase", (0, 0), (10, 0), (10, 8), (8, 8), (8, 6), (6, 6), (6, 4), (4, 4), (4, 8), (0, 8)));
        Family("multi-arm closed outlines",
            Shape("Plus outline", (3, 0), (5, 0), (5, 3), (8, 3), (8, 5), (5, 5), (5, 8), (3, 8), (3, 5), (0, 5), (0, 3), (3, 3)),
            Shape("T outline", (0, 5), (3, 5), (3, 0), (5, 0), (5, 5), (8, 5), (8, 7), (0, 7)),
            Shape("H outline", (0, 0), (2, 0), (2, 3), (6, 3), (6, 0), (8, 0), (8, 8), (6, 8), (6, 5), (2, 5), (2, 8), (0, 8)),
            Shape("I outline", (0, 0), (8, 0), (8, 2), (5, 2), (5, 6), (8, 6), (8, 8), (0, 8), (0, 6), (3, 6), (3, 2), (0, 2)),
            Shape("Offset opposed arms", (0, 0), (7, 0), (7, 2), (4, 2), (4, 5), (9, 5), (9, 7), (2, 7), (2, 5), (0, 5)));
        Family("pointed alternating outlines",
            Shape("Four-point star", (0, -6), (2, -2), (6, 0), (2, 2), (0, 6), (-2, 2), (-6, 0), (-2, -2)),
            Shape("Broad four-point star", (0, -8), (3, -3), (8, 0), (3, 3), (0, 8), (-3, 3), (-8, 0), (-3, -3)),
            Shape("Eight-point star", (0, -8), (2, -5), (6, -6), (5, -2), (8, 0), (5, 2), (6, 6), (2, 5), (0, 8), (-2, 5), (-6, 6), (-5, 2), (-8, 0), (-5, -2), (-6, -6), (-2, -5)),
            Shape("Flattened six-point outline", (-8, 0), (-4, -2), (-4, -6), (0, -4), (4, -6), (4, -2), (8, 0), (4, 2), (4, 6), (0, 4), (-4, 6), (-4, 2)),
            Shape("Long opposed points", (0, -9), (2, -2), (5, 0), (2, 2), (0, 9), (-2, 2), (-5, 0), (-2, -2)));
        Family("translated disconnected units",
            Pair("Two translated triangles", [(0, 0), (5, 0), (2, 4)], 8, 1),
            Pair("Two translated elbows", [(0, 0), (5, 0), (5, 1), (1, 1), (1, 5), (0, 5)], 8, 0),
            Pair("Three translated diamonds", [(0, -3), (3, 0), (0, 3), (-3, 0)], 8, 1, copies: 3),
            Pair("Two translated roof contours", [(0, 0), (6, 0), (6, 3), (3, 6), (0, 3)], 9, -2),
            Pair("Three translated brackets", [(0, 0), (4, 0), (4, 1), (1, 1), (1, 4), (4, 4), (4, 5), (0, 5)], 7, 0, copies: 3));
        Family("mirrored disconnected units",
            Pair("Opposed elbow pair", [(0, 0), (5, 0), (5, 1), (1, 1), (1, 5), (0, 5)], 12, 0, mirror: true),
            Pair("Mirrored roof pair", [(0, 0), (6, 0), (6, 4), (2, 7), (0, 4)], 15, 0, mirror: true),
            Pair("Mirrored pointed notches", [(0, 0), (8, 0), (8, 6), (5, 6), (4, 3), (3, 6), (0, 6)], 19, 1, mirror: true),
            Pair("Mirrored stair pair", [(0, 0), (6, 0), (6, 2), (4, 2), (4, 4), (2, 4), (2, 6), (0, 6)], 15, 0, mirror: true),
            Pair("Mirrored bent spear pair", [(0, 0), (5, 0), (8, 3), (5, 6), (4, 4), (5, 3), (0, 3)], 20, -1, mirror: true));
        Family("turned disconnected groups",
            Turned("Four polygon lobes", [(2, 1), (5, 1), (6, 2), (3, 3)], 4),
            Turned("Four slender triangular lobes", [(2, 1), (7, 1), (4, 3)], 4),
            Turned("Opposed turned elbows", [(2, 1), (6, 1), (6, 2), (3, 2), (3, 5), (2, 5)], 2, halfTurn: true),
            Turned("Four notched lobes", [(2, 1), (7, 1), (7, 3), (5, 3), (5, 2), (3, 2), (3, 4), (2, 4)], 4),
            Turned("Opposed chamfered lobes", [(2, 1), (6, 1), (7, 2), (6, 4), (2, 3)], 2, halfTurn: true));
        Family("nested separate contours",
            Multi("Square inside square", ClosedRoute([(0, 0), (12, 0), (12, 12), (0, 12)]), ClosedRoute([(4, 4), (8, 4), (8, 8), (4, 8)])),
            Multi("Diamond inside rectangle", ClosedRoute([(0, 0), (14, 0), (14, 10), (0, 10)]), ClosedRoute([(7, 2), (10, 5), (7, 8), (4, 5)])),
            Multi("Three nested diamonds", ClosedRoute([(0, -9), (9, 0), (0, 9), (-9, 0)]), ClosedRoute([(0, -6), (6, 0), (0, 6), (-6, 0)]), ClosedRoute([(0, -3), (3, 0), (0, 3), (-3, 0)])),
            Multi("Two unequal inset rooms", ClosedRoute([(0, 0), (16, 0), (16, 10), (0, 10)]), ClosedRoute([(2, 2), (6, 2), (6, 8), (2, 8)]), ClosedRoute([(9, 3), (13, 3), (13, 7), (9, 7)])),
            Multi("Triangle within clipped contour", ClosedRoute([(3, 0), (11, 0), (14, 3), (14, 11), (11, 14), (3, 14), (0, 11), (0, 3)]), ClosedRoute([(4, 4), (10, 4), (7, 10)])));
        Family("shared-boundary compartments",
            Multi("Two joined rooms", [(0, 0), (4, 0), (8, 0), (8, 6), (4, 6), (0, 6), (0, 0)], [(4, 0), (4, 6)]),
            Multi("Four joined rooms", [(0, 0), (4, 0), (8, 0), (8, 4), (8, 8), (4, 8), (0, 8), (0, 4), (0, 0)], [(0, 4), (4, 4), (8, 4)], [(4, 0), (4, 4), (4, 8)]),
            Multi("Roof with divided base", [(0, 0), (4, 0), (8, 0), (8, 5), (4, 9), (0, 5), (0, 0)], [(4, 0), (4, 5)], [(0, 5), (4, 5), (8, 5)]),
            Multi("Triangle with three inner cells", [(0, 0), (10, 0), (5, 9), (0, 0)], [(0, 0), (5, 3), (10, 0)], [(5, 3), (5, 9)]),
            Multi("Long room with staggered dividers", [(0, 0), (4, 0), (8, 0), (12, 0), (12, 6), (8, 6), (4, 6), (0, 6), (0, 0)], [(4, 0), (4, 3), (4, 6)], [(8, 0), (8, 3), (8, 6)], [(4, 3), (8, 3)]));
        Family("open physical junction networks",
            Open("T shared-site junction", [(-5, 0), (0, 0), (5, 0)], [(0, 0), (0, -5)]),
            Open("Y shared-site junction", [(0, -5), (0, 0), (-4, 4)], [(0, 0), (4, 4)]),
            Open("X shared-site junction", [(-5, -5), (0, 0), (5, 5)], [(-5, 5), (0, 0), (5, -5)]),
            Open("H bridge network", [(-4, -5), (-4, 0), (-4, 5)], [(4, -5), (4, 0), (4, 5)], [(-4, 0), (4, 0)]),
            Open("K shared-site network", [(0, -5), (0, 0), (0, 5)], [(0, 0), (5, 5)], [(0, 0), (5, -5)]));
        Family("unjoined crossing controls",
            Open("Unjoined diagonal X", [(-5, -5), (5, 5)], [(-5, 5), (5, -5)]),
            Open("Unjoined axial plus", [(-5, 0), (5, 0)], [(0, -5), (0, 5)]),
            Open("Three mutually unjoined passages", [(-6, -3), (6, 3)], [(-6, 3), (6, -3)], [(0, -6), (0, 6)]),
            Multi("Two crossing enclosure outlines", ClosedRoute([(-6, -2), (6, -2), (6, 2), (-6, 2)]), ClosedRoute([(-2, -6), (2, -6), (2, 6), (-2, 6)])) with { Intent = "Explicit overlap counterexample: disconnected contour interiors overlap. Coordinate crossings do not create shared joints." },
            Open("Broken collinear passage under crossbar", [(-6, 0), (-1, 0)], [(1, 0), (6, 0)], [(0, -5), (0, 5)]));
        Family("deliberately open turning paths",
            Open("Open U contour", [(0, 6), (0, 0), (8, 0), (8, 6)]),
            Open("Open diagonal zigzag", [(0, 0), (2, 4), (4, 0), (6, 4), (8, 0), (10, 4)]),
            Open("Open square-wave path", [(0, 0), (2, 0), (2, 4), (4, 4), (4, 0), (6, 0), (6, 4), (8, 4)]),
            Open("Open inward rectangular return", [(0, 0), (10, 0), (10, 8), (2, 8), (2, 2), (8, 2), (8, 6), (4, 6)]),
            Open("Open stair sequence", [(0, 0), (3, 0), (3, 2), (6, 2), (6, 4), (9, 4), (9, 6)]));
        Family("damaged closed templates",
            Damaged("Rectangle with upper gap", [(0, 0), (8, 0), (8, 6), (0, 6)], [(0, 0), (8, 0), (8, 6), (5, 6)], [(3, 6), (0, 6), (0, 0)]),
            Damaged("Triangle without one side", [(0, 0), (8, 0), (4, 7)], [(0, 0), (8, 0), (4, 7)]),
            Damaged("Elbow missing a return", [(0, 0), (7, 0), (7, 2), (2, 2), (2, 7), (0, 7)], [(0, 0), (7, 0), (7, 2), (2, 2)], [(2, 7), (0, 7), (0, 0)]),
            Damaged("Diamond with one clipped gap", [(0, -6), (6, 0), (0, 6), (-6, 0)], [(0, -6), (6, 0), (0, 6), (-4, 2)], [(-5, -1), (0, -6)]),
            Damaged("Notched contour missing its base", [(0, 0), (10, 0), (10, 8), (7, 8), (7, 5), (3, 5), (3, 8), (0, 8)], [(10, 0), (10, 8), (7, 8), (7, 5), (3, 5), (3, 8), (0, 8), (0, 0)]));
        Family("near-symmetry deviations",
            Shape("Square with low outward tab", (0, 0), (2, 0), (2, -1), (4, -1), (4, 0), (8, 0), (8, 8), (0, 8)),
            Shape("Rectangle with one clipped corner", (0, 0), (10, 0), (10, 8), (2, 8), (0, 6)),
            Shape("Diamond with unequal lower return", (0, -7), (6, 0), (0, 6), (-6, 0), (-2, -2)),
            Shape("Four arms with one short arm", (3, 0), (5, 0), (5, 3), (8, 3), (8, 5), (5, 5), (5, 7), (3, 7), (3, 5), (0, 5), (0, 3), (3, 3)),
            Shape("Roof contour with lower concave return", (0, 0), (3, 0), (3, 2), (5, 2), (5, 0), (8, 0), (8, 5), (4, 9), (0, 5)));
        Family("ornamental closed turns",
            Shape("Closed fret enclosure", (0, 0), (10, 0), (10, 8), (3, 8), (3, 3), (7, 3), (7, 5), (5, 5), (5, 6), (8, 6), (8, 2), (2, 2), (2, 10), (0, 10)),
            Shape("Opposed arrow enclosure", (-8, 0), (-4, -3), (-4, -1), (4, -1), (4, -3), (8, 0), (4, 3), (4, 1), (-4, 1), (-4, 3)),
            Shape("Battlement enclosure", (0, 0), (12, 0), (12, 7), (10, 7), (10, 4), (8, 4), (8, 7), (6, 7), (6, 4), (4, 4), (4, 7), (2, 7), (2, 4), (0, 4)),
            Shape("Diagonal returned ribbon", (0, 0), (3, 0), (8, 5), (8, 8), (5, 8), (1, 4), (3, 4), (6, 7), (7, 7), (7, 6), (2, 1), (0, 1)),
            Shape("Stepped pointed medallion", (0, -8), (2, -6), (2, -4), (4, -4), (4, -2), (6, -2), (8, 0), (6, 2), (4, 2), (4, 4), (2, 4), (2, 6), (0, 8), (-2, 6), (-2, 4), (-4, 4), (-4, 2), (-6, 2), (-8, 0), (-6, -2), (-4, -2), (-4, -4), (-2, -4), (-2, -6)));

        // The final family contains exact earlier-learning controls plus three
        // construction-sensitive network controls. It remains five cases.
        var descriptors = OrnamentLearningCorpus.Describe(65);
        foreach (var index in new[] { 16, 64 })
        {
            var sample = OrnamentLearningCorpus.Materialize(descriptors[index], includePattern: false);
            var published = new NativeLinePublication(sample.Target with { Id = "public-source", Name = "Native source", Note = "Exposed native geometry only." }).Published;
            result.Add(new($"R{result.Count + 1:000}", $"Earlier learning case L{index:00000}",
                "Exact earlier descriptor re-executed independently. The receiver receives no descriptor or prior family name.", published,
                new("earlier and construction-sensitive controls", "Earlier target observed without correcting its history.", null,
                    new Relation(published.Graph.Surface, published.Bindings))));
        }
        foreach (var example in new[]
        {
            Multi("Closed contour with disconnected inner cross", ClosedRoute([(0, 0), (12, 0), (12, 12), (0, 12)]), [(3, 6), (9, 6)], [(6, 3), (6, 9)]),
            Multi("Closed contour with an attached stem", [(0, 0), (4, 0), (8, 0), (8, 8), (0, 8), (0, 0)], [(4, 0), (4, -4)]) with { Intent = "A closed boundary and an open branch coexist; a global closure default must not erase the branch." },
            Multi("Two closures linked by one bridge", [(0, 0), (4, 0), (4, 2), (4, 4), (0, 4), (0, 0)], [(8, 0), (12, 0), (12, 4), (8, 4), (8, 2), (8, 0)], [(4, 2), (8, 2)])
        }) result.Add(Build($"R{result.Count + 1:000}", "earlier and construction-sensitive controls", example));

        if (result.Count != 100) throw new InvalidOperationException("The reproducible corpus contains exactly one hundred sources.");
        return result.AsReadOnly();
    }

    private static Case Build(string id, string family, Spec spec)
    {
        var source = Execute(spec.Paths);
        var complete = spec.Complete is null ? null : Execute(spec.Complete);
        var comparison = complete is null ? N.S() : new Relation(complete.Graph.Surface, complete.Bindings);
        var audit = new Relation(new Relation(source.Graph.Surface, source.Bindings), comparison);
        return new(id, spec.Name,
            "Source created and executed before analysis. Names, family, intent and complete-target controls are withheld from reconstruction.",
            source, new(family, spec.Intent, complete, audit));
    }

    private static NativeLatticeSources.SourceCase Execute(Func<LatticeLadderAuthor, Relation[]> makePaths)
    {
        var author = new LatticeLadderAuthor(); var paths = makePaths(author);
        var closed = paths.Where(path => UnitArrangementRead.IsZero(author.Frame, author.Frame.Reduce(N.S(),
            LatticeLadderAuthor.JoinWords(N.E((Relation)path.Right).Cast<Relation>().ToArray())))).ToArray();
        var unit = new UnitArrangementProgram(author.Frame, "native teacher construction", N.S(paths),
            LatticeLadderAuthor.Ports(paths), N.S(closed));
        var recipe = author.Compile("source", "native teacher construction", "Executed passages", "Reconstruction controls",
            unit, author.Word(64, 0), author.Word(0, 64), 1, 1);
        var run = recipe.Program.Generate(recipe.Bindings, recipe.Window);
        var field = new OrnamentProgramField(run, recipe.SeamContract);
        if (!run.IsComplete || !field.IsComplete) throw new InvalidOperationException("A source must execute before publication.");
        var source = new NativeLatticeSources.SourceCase("public-source", "Native source", "Exposed native geometry only.",
            field.Graph, field.Bindings, field.Graph.Strokes, N.S(), true);
        return new NativeLinePublication(source).Published;
    }

    private static Spec Shape(string name, params (int X, int Y)[] vertices) =>
        Multi(name, ClosedRoute(vertices));
    private static Spec Multi(string name, params (int X, int Y)[][] paths) =>
        new(name, author => paths.Select(points => Route(author, points)).ToArray());
    private static Spec Open(string name, params (int X, int Y)[][] paths) =>
        Multi(name, paths) with { Intent = "The authored passages are deliberately open; a closed-area default is revisable context, not source truth." };
    private static Spec Damaged(string name, (int X, int Y)[] complete, params (int X, int Y)[][] paths) =>
        new(name, author => paths.Select(points => Route(author, points)).ToArray(),
            "A closed teacher template lost participation. The observed gap remains evidence; completion is only a receiver hypothesis.",
            author => [Route(author, ClosedRoute(complete))]);
    private static Spec Pair(string name, (int X, int Y)[] vertices, int dx, int dy, int copies = 2, bool mirror = false) =>
        new(name, author =>
        {
            var first = Route(author, ClosedRoute(vertices)); var paths = new List<Relation> { first };
            var unit = author.Unit("teacher repeated unit", first);
            var reflection = author.Map(author.Word(-1, 0), author.Word(0, 1));
            for (var copy = 1; copy < copies; copy++)
            {
                var next = mirror ? (Relation)N.E(unit.Transform("teacher reflected unit", reflection).Paths).Single() : first;
                paths.Add(author.Translate(next, author.Word(dx * copy, dy * copy), out _));
            }
            return paths.ToArray();
        });
    private static Spec Turned(string name, (int X, int Y)[] vertices, int copies, bool halfTurn = false) =>
        new(name, author =>
        {
            var unit = author.Unit("teacher turned unit", Route(author, ClosedRoute(vertices)));
            var turn = halfTurn ? author.Map(author.Word(-1, 0), author.Word(0, -1)) : author.Map(author.Word(0, 1), author.Word(-1, 0));
            var paths = new List<Relation>();
            for (var copy = 0; copy < copies; copy++)
            { paths.Add((Relation)N.E(unit.Paths).Single()); unit = unit.Transform("teacher next turned unit", turn); }
            return paths.ToArray();
        });
    private static (int X, int Y)[] ClosedRoute((int X, int Y)[] vertices) =>
        vertices.Length > 1 && vertices[0] == vertices[^1] ? vertices : [.. vertices, vertices[0]];
    private static Relation Route(LatticeLadderAuthor author, (int X, int Y)[] points) =>
        LatticeLadderAuthor.Path(author.Word(points[0].X, points[0].Y), Enumerable.Range(0, points.Length - 1)
            .Select(index => author.Word(points[index + 1].X - points[index].X, points[index + 1].Y - points[index].Y)).ToArray());
}
