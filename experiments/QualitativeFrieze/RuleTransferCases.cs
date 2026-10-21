using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Spec = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Spec;
using Sample = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Sample;
using PathSpec = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.PathSpec;
using Definition = Gojo.Experiments.QualitativeFrieze.OrnamentReconstructionCases.Definition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Three presentation domains exercise the same native readers.
/// Domain and family labels never establish membership. Each factory authors
/// actual paths and exposed joins, and the retained recipe executes without
/// the factory. Open contacts and declared crossings are distinct contracts.</summary>
public static class RuleTransferCases
{
    public sealed record Case(string Id, string Domain, string Family, string Name, string Note, Spec Spec)
    {
        public Sample Create() => Spec.Create();
    }

    public sealed record GenerationCase(string Id, string Domain, string Name, string Note, string Family,
        RevisablePatternTemplate Template, IElement HorizontalRole, IElement VerticalRole, IElement Reach);

    public static IReadOnlyList<Case> All { get; } = Build();
    public static Sample Create(string id) => All.Single(item => item.Id == id).Create();

    private static IReadOnlyList<Case> Build()
    {
        var result = new List<Case>();
        Add("O-C01", "ORNAMENT", "CONTOUR", "Lozenge compartment", "Reference: one closed course with four turning landmarks.",
            () => new([Ring((0, 0), (3, 0), (5, 3), (2, 3))]));
        Add("O-C02", "ORNAMENT", "CONTOUR", "Notched compartment", "Inward corners change the silhouette while retaining a single closed course.",
            () => new([Ring((0, 0), (6, 0), (6, 6), (4, 6), (4, 4), (2, 4), (2, 6), (0, 6))]));
        Add("O-C03", "ORNAMENT", "CONTOUR", "Compartment assembled from sides", "Four separate mechanical paths join their actual terminal sites.",
            () => Split(new([Ring((0, 0), (4, 0), (4, 4), (0, 4))])));
        Add("O-C04", "ORNAMENT", "CONTOUR", "Unjoined matching sides", "The same visible placement leaves eight independent endpoints.",
            () => Split(new([Ring((0, 0), (4, 0), (4, 4), (0, 4))])) with { JoinCoincident = false });
        Add("O-C05", "ORNAMENT", "CONTOUR", "Open compartment", "A missing returning chord leaves a path rather than a circuit.",
            () => new([Open((0, 0), (4, 0), (4, 4), (0, 4))]));
        Add("O-C06", "ORNAMENT", "CONTOUR", "Needle compartment", "A genuine closure uses unusually long and thin common-reach words; a separate boundary contract could limit its extent.",
            () => new([Ring((0, 0), (24, 0), (24, 1), (0, 1))]));

        Add("G-C01", "GLYPH", "CONTOUR", "Faceted O", "Eight joined corners make a closed counter-form without requiring a circle metric.",
            () => new([Ring((-3, -2), (-1, -4), (1, -4), (3, -2), (3, 2), (1, 4), (-1, 4), (-3, 2))]));
        Add("G-C02", "GLYPH", "CONTOUR", "Angular D", "One upright stem returns through a bowed sequence of straight chords.",
            () => new([Ring((-2, -4), (1, -4), (4, -2), (4, 2), (1, 4), (-2, 4))]));
        Add("G-C03", "GLYPH", "CONTOUR", "Open C", "The outer course has a deliberate aperture on its right side.",
            () => new([Open((3, -2), (1, -4), (-1, -4), (-3, -2), (-3, 2), (-1, 4), (1, 4), (3, 2))]));
        Add("G-C04", "GLYPH", "CONTOUR", "Angular B", "Two closed chambers share a central side endpoint; the extra circuit is actual topology.",
            () => new([Open((0, -4), (0, 0), (0, 4)), Open((0, 4), (3, 4), (4, 2), (3, 0), (0, 0)),
                Open((0, 0), (3, 0), (4, -2), (3, -4), (0, -4))]));
        Add("G-C05", "GLYPH", "CONTOUR", "O from separate sides", "Independent emitted sides become one closure through declared corner joins.",
            () => Split(new([Ring((-3, -2), (-1, -4), (1, -4), (3, -2), (3, 2), (1, 4), (-1, 4), (-3, 2))])));
        Add("G-C06", "GLYPH", "CONTOUR", "Nearly closed O", "A short opening preserves two actual terminal sites even when closure is a plausible interpretation.",
            () => new([Open((-3, 0), (-3, 2), (-1, 4), (1, 4), (3, 2), (3, -2), (1, -4), (-1, -4), (-3, -2), (-3, -1))]));

        Add("N-C01", "NETWORK", "CONTOUR", "Detouring ring route", "A circuit bends around an interior obstruction without introducing a branch.",
            () => new([Ring((-5, -2), (-2, -2), (-2, -4), (4, -4), (4, 3), (0, 3), (0, 5), (-5, 5))]));
        Add("N-C02", "NETWORK", "CONTOUR", "Long ring route", "A connected circuit retains its topology while a long corridor changes the finer default reads.",
            () => new([Ring((-10, -2), (10, -2), (10, 2), (2, 2), (2, 4), (-10, 4))]));
        Add("N-C03", "NETWORK", "CONTOUR", "Ring with a terminal spur", "One outgoing route shares an actual ring corner and adds branch participation.",
            () => new([Ring((-4, -3), (4, -3), (4, 3), (-4, 3)), Open((4, 3), (7, 5))]));
        Add("N-C04", "NETWORK", "CONTOUR", "Interrupted ring", "The last corridor has not been connected to the origin.",
            () => new([Open((-4, -3), (4, -3), (4, 3), (-4, 3), (-4, -2))]));
        Add("N-C05", "NETWORK", "CONTOUR", "Subdivided triangular circuit", "Additional collinear waypoints do not create branches in the closed route.",
            () => new([Ring((-4, -3), (0, -3), (4, -3), (2, 0), (0, 3), (-2, 0))]));
        Add("N-C06", "NETWORK", "CONTOUR", "Two isolated ring routes", "A second independent circuit changes the complete participation scope.",
            () => new([Ring((-6, -2), (-2, -2), (-2, 2), (-6, 2)), Ring((2, -2), (6, -2), (6, 2), (2, 2))]));

        Add("O-B01", "ORNAMENT", "BRIDGE", "Double-clamped border", "Reference: a central bridge joins two through-going stems at their interior sites.",
            () => H(4, 3));
        Add("O-B02", "ORNAMENT", "BRIDGE", "Bent border clamps", "Elbowed stems and a stepped bridge preserve two three-arm joins.",
            () => new([Open((-2, -3), (0, -2), (0, 0), (0, 2), (-2, 3)),
                Open((8, -3), (6, -2), (6, 0), (6, 2), (8, 3)), Open((0, 0), (2, 1), (4, 1), (6, 0))]));
        Add("O-B03", "ORNAMENT", "BRIDGE", "Unclamped visual border", "The bridge ends share positions with the stems but no public-site connections.",
            () => H(4, 3) with { JoinCoincident = false });
        Add("O-B04", "ORNAMENT", "BRIDGE", "Two crossbars", "A second bridge adds a chamber and two further branch sites.",
            () => new([Open((0, -3), (0, -1), (0, 1), (0, 3)), Open((5, -3), (5, -1), (5, 1), (5, 3)),
                Open((0, -1), (5, -1)), Open((0, 1), (5, 1))]));
        Add("O-B05", "ORNAMENT", "BRIDGE", "Short crossbar", "The bridge is joined at one clamp but stops before reaching the other.",
            () => new([Open((0, -3), (0, 0), (0, 3)), Open((5, -3), (5, 0), (5, 3)), Open((0, 0), (4, 0))]));
        Add("O-B06", "ORNAMENT", "BRIDGE", "Very long border clamp", "Both joins remain genuine despite an extreme span and a small stem reach.",
            () => H(24, 1));

        Add("G-B01", "GLYPH", "BRIDGE", "H", "Two uninterrupted stems share a single joining crossbar.", () => H(6, 4));
        Add("G-B02", "GLYPH", "BRIDGE", "Barred I", "The same branch arrangement is read through horizontal caps and a vertical joining stem.",
            () => new([Open((-3, -4), (0, -4), (3, -4)), Open((-3, 4), (0, 4), (3, 4)), Open((0, -4), (0, 4))]));
        Add("G-B03", "GLYPH", "BRIDGE", "F", "Ordered arm choices on one stem are not two three-arm clamps.",
            () => new([Open((0, -4), (0, 0), (0, 4)), Open((0, 4), (5, 4)), Open((0, 0), (4, 0))]));
        Add("G-B04", "GLYPH", "BRIDGE", "Stencil H", "A gap splits the crossbar into two independently attached halves.",
            () => new([Open((0, -4), (0, 0), (0, 4)), Open((6, -4), (6, 0), (6, 4)), Open((0, 0), (2, 0)), Open((4, 0), (6, 0))]));
        Add("G-B05", "GLYPH", "BRIDGE", "Branching serif H", "Extra arms at the stem ends add topology rather than merely changing line weight.",
            () => new([Open((0, -4), (0, 0), (0, 4)), Open((6, -4), (6, 0), (6, 4)), Open((0, 0), (6, 0)),
                Open((-1, -4), (0, -4), (1, -4)), Open((5, 4), (6, 4), (7, 4))]));
        Add("G-B06", "GLYPH", "BRIDGE", "Oblique H", "A common shear changes the stem directions while retaining the bridge joins.",
            () => Map(H(5, 3), p => (p.X + p.Y, p.Y)));

        Add("N-B01", "NETWORK", "BRIDGE", "Rail transfer passage", "A cross-passage connects two through-going routes at two explicit junctions.", () => H(8, 3));
        Add("N-B02", "NETWORK", "BRIDGE", "Detouring transfer passage", "The two joining terminals survive a long stepped corridor between them.",
            () => new([Open((-4, -4), (-4, 0), (-4, 4)), Open((5, -4), (5, 0), (5, 4)),
                Open((-4, 0), (-1, 0), (-1, 2), (2, 2), (2, 0), (5, 0))]));
        Add("N-B03", "NETWORK", "BRIDGE", "Unconnected crossing corridor", "A corridor crosses the stem positions internally without binding to them.",
            () => new([Open((0, -3), (0, 0), (0, 3)), Open((6, -3), (6, 0), (6, 3)), Open((-2, 0), (8, 0))]));
        Add("N-B04", "NETWORK", "BRIDGE", "Fork-to-fork passage", "Diagonal fork arms provide the same two branch landmarks as axial clamps.",
            () => new([Open((-4, -3), (-2, 0), (-4, 3)), Open((6, -3), (4, 0), (6, 3)), Open((-2, 0), (4, 0))]));
        Add("N-B05", "NETWORK", "BRIDGE", "Single-ended transfer", "Only one terminal is a branch; the second through-going route is absent.",
            () => new([Open((0, -3), (0, 0), (0, 3)), Open((0, 0), (6, 0))]));
        Add("N-B06", "NETWORK", "BRIDGE", "Untied bent transfer", "The bridge meets both route positions without actual terminal joins.",
            () => new([Open((-3, -4), (-3, 0), (-3, 4)), Open((4, -4), (4, 0), (4, 4)), Open((-3, 0), (0, 1), (4, 0))], false));

        Add("O-R01", "ORNAMENT", "COURSES", "Independent angled courses", "Reference: two open courses have independent endpoints and no connecting participation.",
            () => new([Open((-5, -3), (-2, -1), (1, -3), (4, -1)), Open((-5, 1), (-2, 3), (1, 1), (4, 3))]));
        Add("O-R02", "ORNAMENT", "COURSES", "Declared crossing ribbons", "Two independent chords have a declared encounter without a shared crossing site or over/under order.",
            () => new([Open((-4, -3), (4, 3)), Open((-4, 3), (4, -3))], false, [(0, 1)]));
        Add("O-R03", "ORNAMENT", "COURSES", "Unexplained crossing ribbons", "The same crossing geometry has no exposed encounter declaration.",
            () => new([Open((-4, -3), (4, 3)), Open((-4, 3), (4, -3))], false));
        Add("O-R04", "ORNAMENT", "COURSES", "Courses joined into one", "One actual shared terminal makes the two courses a single connected route.",
            () => new([Open((-4, -2), (0, 0)), Open((0, 0), (4, 2))]));
        Add("O-R05", "ORNAMENT", "COURSES", "Three parallel courses", "A third independent strand changes whole-scope participation.",
            () => new([Open((-5, -3), (5, -3)), Open((-5, 0), (5, 0)), Open((-5, 3), (5, 3))]));
        Add("O-R06", "ORNAMENT", "COURSES", "Coincident returning courses", "Independent intervals retrace each other geometrically, requiring an exception to ordinary contact assumptions.",
            () => new([Open((0, 0), (6, 0)), Open((4, 0), (-2, 0))], false));

        Add("G-R01", "GLYPH", "COURSES", "Independent-stroke X", "A declared crossing identifies the encounter while the two strokes retain distinct sites.",
            () => new([Open((-3, -4), (3, 4)), Open((-3, 4), (3, -4))], false, [(0, 1)]));
        Add("G-R02", "GLYPH", "COURSES", "Independent-stroke plus", "Axial strokes cross without becoming a four-arm shared junction.",
            () => new([Open((-4, 0), (4, 0)), Open((0, -4), (0, 4))], false, [(0, 1)]));
        Add("G-R03", "GLYPH", "COURSES", "Joined plus", "Four emitted arms share a common hub; this is not two independent courses.",
            () => FourWay());
        Add("G-R04", "GLYPH", "COURSES", "Double slash", "Two separated open strokes have parallel diagonal headings.",
            () => new([Open((-4, -4), (0, 4)), Open((0, -4), (4, 4))]));
        Add("G-R05", "GLYPH", "COURSES", "Joined K", "The two angled arms share a stem landmark, creating a branch.",
            () => new([Open((-2, -4), (-2, 0), (-2, 4)), Open((-2, 0), (3, 4)), Open((-2, 0), (3, -4))]));
        Add("G-R06", "GLYPH", "COURSES", "Separated Z strokes", "Both independent courses contain turns; local straightness is not required.",
            () => new([Open((-5, 2), (-1, 2), (-5, -2), (-1, -2)), Open((1, 2), (5, 2), (1, -2), (5, -2))]));

        Add("N-R01", "NETWORK", "COURSES", "Two-route overpass", "The first corridors cross by declaration; later detours retain independent route identity.",
            () => new([Open((-4, 0), (4, 0), (4, 3)), Open((0, -4), (0, 4), (3, 4))], false, [(0, 1)]));
        Add("N-R02", "NETWORK", "COURSES", "Separated detour routes", "Each route has its own stepped corridor and terminal pair.",
            () => new([Open((-6, -4), (-2, -4), (-2, -2), (1, -2)), Open((0, 3), (3, 3), (3, 1), (6, 1))]));
        Add("N-R03", "NETWORK", "COURSES", "Fused four-way junction", "All four route arms are connected to one actual site.", FourWay);
        Add("N-R04", "NETWORK", "COURSES", "Unjoined terminal contact", "Two endpoints occupy one position without sharing topology; the unexpected touch remains evidence.",
            () => new([Open((-4, 0), (0, 0)), Open((0, 0), (0, 4))], false));
        Add("N-R05", "NETWORK", "COURSES", "Circuit beside an open route", "There are two components, but one is closed rather than an endpoint-to-endpoint course.",
            () => new([Ring((-6, -2), (-2, -2), (-2, 2), (-6, 2)), Open((2, -3), (4, 0), (2, 3))]));
        Add("N-R06", "NETWORK", "COURSES", "Independent coincident routes", "Distinct route identities occupy the same interval, a structural match that ordinary overlap defaults can disfavor.",
            () => new([Open((-4, 0), (4, 0)), Open((-4, 0), (4, 0))], false));
        return result.AsReadOnly();

        void Add(string id, string domain, string family, string name, string note, Func<Definition> definition)
        {
            var spec = Spec.Make(id, family, name, "Cross-domain structural rule-transfer fixture; labels are presentation", 0, 0, 1, definition);
            result.Add(new(id, domain, family, name, note, spec));
        }
    }

    /// <summary>Fresh symbolic program worlds. Repeated H/V occurrences are
    /// actual shared parameter roles, not equal numeric values inferred later.
    /// Explicit connections use published path/chord selector roles.</summary>
    public static IReadOnlyList<GenerationCase> Scenarios()
    {
        return [Contour(), Bridge(), Theta()];

        GenerationCase Contour()
        {
            var frame = new QualitativeMotionFrame(); IElement h = new Placeholder(), v = new Placeholder(), reach = new Placeholder();
            Relation W(params (IElement Direction, IElement Extent)[] moves) => S(moves.Select(m => (IElement)QualitativeMotionFrame.Move(m.Direction, m.Extent)).ToArray());
            var path = LatticeLadderAuthor.Path(S(), W((frame.Right, h)), W((frame.Up, v)), W((frame.Left, h)), W((frame.Down, v)));
            var program = new UnitArrangementProgram(frame, "Symbolic ornament compartment", S(path), LatticeLadderAuthor.Ports(path), S(path));
            return new("GEN-O-C01", "ORNAMENT", "Shared-role compartment", "Opposed chords reuse the same H/V extents, preserving a closed course for every complete positive binding.",
                "CONTOUR", new(program, S(h, v)), h, v, reach);
        }

        GenerationCase Bridge()
        {
            var frame = new QualitativeMotionFrame(); IElement h = new Placeholder(), v = new Placeholder(), reach = new Placeholder();
            Relation W(params (IElement Direction, IElement Extent)[] moves) => S(moves.Select(m => (IElement)QualitativeMotionFrame.Move(m.Direction, m.Extent)).ToArray());
            var first = LatticeLadderAuthor.Path(S(), W((frame.Up, v)), W((frame.Up, v)));
            var second = LatticeLadderAuthor.Path(W((frame.Right, h)), W((frame.Up, v)), W((frame.Up, v)));
            var bar = LatticeLadderAuthor.Path(W((frame.Up, v)), W((frame.Right, h)));
            var ports = LatticeLadderAuthor.Ports(first, second, bar);
            var program = new UnitArrangementProgram(frame, "Symbolic glyph H", S(first, second, bar), ports);
            var connections = S(new Relation(Role(ports, first, 1), Role(ports, bar, 0)),
                new Relation(Role(ports, second, 1), Role(ports, bar, 1)));
            return new("GEN-G-B01", "GLYPH", "Shared-role H", "The bridge joins exposed interior stem roles explicitly. All repeated V roles retain the same chosen extent.",
                "BRIDGE", new(program, S(h, v), connections), h, v, reach);
        }

        GenerationCase Theta()
        {
            var frame = new QualitativeMotionFrame(); IElement h = new Placeholder(), v = new Placeholder(), reach = new Placeholder();
            Relation W(params (IElement Direction, IElement Extent)[] moves) => S(moves.Select(m => (IElement)QualitativeMotionFrame.Move(m.Direction, m.Extent)).ToArray());
            var upper = LatticeLadderAuthor.Path(S(), W((frame.Up, v)), W((frame.Right, h)), W((frame.Down, v)));
            var middle = LatticeLadderAuthor.Path(S(), W((frame.Right, h)));
            var lower = LatticeLadderAuthor.Path(S(), W((frame.Down, v)), W((frame.Right, h)), W((frame.Up, v)));
            var ports = LatticeLadderAuthor.Ports(upper, middle, lower);
            var program = new UnitArrangementProgram(frame, "Symbolic three-route network", S(upper, middle, lower), ports);
            var connections = S(new Relation(Role(ports, upper, 0), Role(ports, middle, 0)),
                new Relation(Role(ports, upper, 0), Role(ports, lower, 0)),
                new Relation(Role(ports, upper, 3), Role(ports, middle, 1)),
                new Relation(Role(ports, upper, 3), Role(ports, lower, 3)));
            return new("GEN-N-T01", "NETWORK", "Shared-role three-route bridge", "Two terminal groups join upper, middle and lower paths. Shared H/V roles retain their correspondence across all three routes.",
                "THETA", new(program, S(h, v), connections), h, v, reach);
        }
    }

    private static IElement Role(Relation ports, Relation path, int point)
    {
        var words = E((Relation)path.Right); var selector = point == words.Length ? path : words[point];
        return E(ports).Cast<Relation>().Single(row => row.Right is Relation pick &&
            ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selector)).Left;
    }
    private static Definition H(int width, int halfHeight) => new([Open((0, -halfHeight), (0, 0), (0, halfHeight)),
        Open((width, -halfHeight), (width, 0), (width, halfHeight)), Open((0, 0), (width, 0))]);
    private static Definition FourWay() => new([Open((0, 0), (-4, 0)), Open((0, 0), (4, 0)), Open((0, 0), (0, -4)), Open((0, 0), (0, 4))]);
    private static PathSpec Open(params (int X, int Y)[] points) => new(points);
    private static PathSpec Ring(params (int X, int Y)[] points) => new([.. points, points[0]], true);
    private static Definition Split(Definition source) => source with { Paths = source.Paths.SelectMany(path =>
        Enumerable.Range(1, path.Points.Length - 1).Select(i => Open(path.Points[i - 1], path.Points[i]))).ToArray() };
    private static Definition Map(Definition source, Func<(int X, int Y), (int X, int Y)> map) => source with {
        Paths = source.Paths.Select(path => new PathSpec(path.Points.Select(map).ToArray(), path.Closed)).ToArray() };
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
