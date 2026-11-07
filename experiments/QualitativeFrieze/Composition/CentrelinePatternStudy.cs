using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Parts = Gojo.Experiments.QualitativeFrieze.Composition.OrnamentBandComposition;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Authored centre-line readings of eighteen straight-edged reference
/// rows. Each ordered route, common-reach word, placement and overlapping route
/// selection remains connected. Source captions are editorial hypotheses, not
/// a classifier, photograph reconstruction or recovered construction history.</summary>
public static class CentrelinePatternStudy
{
    // root = Closed(frame, repeat displacement, Closed(routes), Closed(selections), Closed(applications))
    // route = Closed(Closed(ordered sections), Closed(site -> position word))
    //         The second scope contains a single point only for a spot pen.
    // selection = Closed(actual route references); no geometry is duplicated.
    // application = (shared source words -> (substitution -> displacement)) ->
    //               (Closed(substitution/sum receipts) -> actual route)
    // A shared endpoint joins adjacent sections within a route. Geometric
    // crossings and coincident endpoints of different routes do not join them.

    public readonly record struct Pt(int X, int Y);
    public sealed record PathRead(int Id, string Role, int[][] Points, bool Closed,
        string TurnWord, string StepWord, int[] SiteIds);
    public sealed record GroupRead(string Id, string Name, int[] PathIds);
    public sealed record Metrics(int Paths, int Segments, int Spots, int ClosedPaths,
        int Components, int LeftTurns, int RightTurns, int StraightSteps, int UTurns,
        int ProjectedCrossings, int ProjectedContacts, int CollinearOverlaps);
    public sealed record PairRead(int First, int Second, string Transform, int Matched,
        int Checked, bool Exact, int[] Translation, string Note);
    public sealed record PatternRead(string Id, string Name, string Source, string Family,
        string Scope, int Pitch, PathRead[] Paths, GroupRead[] Groups, Metrics Metrics,
        PairRead[] PairReads);
    public sealed record Fixture(Curve.Frame Frame, Relation Surface, string[] Roles,
        string[] GroupIds, string[] GroupNames);
    private sealed record Source(string Id, string Name, string Location, string Family, string Scope,
        Func<Fixture> Build);

    private const string Realization = "Authored common-reach centre-line interpretation. Counts and matches apply to this connected realization, not measured pixels or inferred original history. ";

    public static PatternRead[] Patterns() => Sources().Select(s =>
        Read(s.Build(), s.Id, s.Name, s.Location, s.Family, Realization + s.Scope)).ToArray();

    public static PatternRead[] Variants() => new[]
    {
        ("paired-baseline", "One word in opposed vertical frames", "The paired routes share a source word. Their y=0 crossings are isolated; neither route dwells along the reflection axis."),
        ("paired-offset", "Shift one reflected course", "The fixed-axis comparison fails, while a reflection followed by translation remains exact. The selected axis and the family of transforms are different claims."),
        ("paired-expanded", "Expand one vertical response", "The shared source word remains connected, but a rigid reflection and translation cannot account for the changed vertical scale."),
        ("paired-reversed", "Reverse one traversal", "The same reflected support is present, but its ordered traversal is reversed. A support comparison and a directed route comparison answer different questions."),
        ("paired-axis-dwell", "Hold a horizontal section on the axis", "Reflection preserves the construction, but positive-length support is now drawn twice by distinct routes. This is an overlap cost, not a failure of reflection and not a welded junction.")
    }.Select((v, i) => Read(BuildPair(i), v.Item1, v.Item2, "Independent controlled fixture", "paired vertical frames", Realization + v.Item3, includeSelectedPairQueries: true)).ToArray();

    public static object Report() => new
    {
        Schema = "gojo.centreline-patterns.v1",
        Scope = "Eighteen authored straight-edged readings of the supplied sheets, with route selections, shared construction words and bounded comparisons. No automatic image parsing, exhaustive engine search, probability estimate, aesthetic score or inferred material weld is claimed.",
        Patterns = Patterns(), Variants = Variants()
    };

    public static Fixture BuildSource(string id)
    {
        var source = Sources().Single(s => s.Id == id);
        return source.Build();
    }

    private static Source[] Sources() =>
    [
        new("A1", "Running key", "Sheet A · row 1", "continuous key", "One repeated cardinal course and two independent framing rails.", () => Course(Running, 5, 3, true)),
        new("A2", "Opposed hooks", "Sheet A · row 2", "alternating hooks", "A hook and its vertically opposed continuation form one ordered course; detached rails are separate.", () => Course(Hook, 4, 5, true)),
        new("A3", "Key with four-window inserts", "Sheet A · row 3", "offset courses and inserts", "Two phase-shifted key courses accompany separate four-window inserts. The insert skeleton is an authored boundary reading; native joins at ink contacts remain unselected.", WindowKey),
        new("A4", "Offset angular interlock", "Sheet A · row 4", "offset courses", "Two offset angular courses provide one possible through-routing. Contacts in the source do not determine this routing or a material weld.", () => OffsetKeys(true)),
        new("A5", "Continuous inward-outward square winding", "Sheet A · row 5", "continuous spiral meander", "A central S-shaped bridge connects each inward winding to an outward return and onward to the next bay. The centre-line reading preserves that continuous route, with two detached rails.", () => Course(SpiralMeander, 7, 4, true)),
        new("A6", "Nested squares and four turned hooks", "Sheet A · row 6", "compound medallions", "Nested closed outlines alternate with four rotated finite hooks. Colour is ignored; individual components retain separate identities.", Medallions),
        new("B1", "Hooks against continuous rails", "Sheet B · row 1", "alternating hooks", "The hook family is read with framing contributions filling the rest of the visible rails. Each support interval is drawn once; meeting endpoints remain unresolved as material joins.", () => Course(Hook, 4, 5, true, attachedRails: true)),
        new("B3", "Stepped interlocking courses", "Sheet B · row 3", "offset courses", "Two horizontally phase-shifted stepped routes give a centre-line reading of the angular interlock. Crossings are projected encounters with no inferred over/under law.", () => StepLinks(false)),
        new("B4", "Stacked crosses and diamond openings", "Sheet B · row 4", "repeated double crosses", "Each motif contains two stacked Xs, with a diamond opening between them and a gap before the next motif. Four independent diagonal pens preserve uncertain through-routing; a pair of vertical zigzag pens is an alternative construction.", DoubleCrosses),
        new("B5", "Battlement", "Sheet B · row 5", "square wave", "One equal-step rise/fall course lies between independent rails.", () => Course(Battlement, 4, 5, true)),
        new("B6", "Framed deep interlock", "Sheet B · row 6", "deep alternating hooks", "The deep interlock and the remaining framing intervals are retained separately. Together their projection supplies the visible rails without double-drawing support. Contact does not resolve flow transfer.", () => Course(Interlock, 8, 3, true, attachedRails: true)),
        new("B8", "Offset battlements and detached spots", "Sheet B · row 8", "offset courses and spots", "Two continuous phase-shifted battlement courses cross without a selected weld. Detached spot pens remain separate from both courses; no bridge is asserted from a spot to a nearby line.", BrokenLinks),
        new("CL1", "Narrow framed interlock", "Sheet C · left row 1", "deep alternating hooks", "The familiar inner course is shown with the complementary framing intervals. Their projected rail is continuous, while native joins remain unselected. The reach binding idealizes the narrow printed instance.", () => Course(Interlock, 8, 3, true, attachedRails: true)),
        new("CL2", "Alternating finite coils in cells", "Sheet C · left row 2", "start-stop spiral cells", "Finite square coils alternate by half-turn in a rectangular cell strip. Each coil ends inside its cell. Contact at its frame entry remains unjoined in this reading; the frame and separators have their own participants.", SpiralCells),
        new("CL5", "Diagonal zigzag", "Sheet C · left row 5", "single zigzag", "The light zigzag is selected as foreground; the dark filled strip is outside this centre-line read.", () => Zigzags(1, false)),
        new("CL6", "Dense diagonal lattice", "Sheet C · left row 6", "clipped diagonal families", "Two families of independent straight diagonals are clipped by the observation strip. Clipped endpoints remain open; no turn or reflection at the strip edge is invented. The dark backing is outside this centre-line read.", DiagonalLattice),
        new("CR3", "Standalone deep interlock", "Sheet C · right row 3", "deep alternating hooks", "The previously studied interlock is reused without attached framing rails.", () => Course(Interlock, 8, 3, false)),
        new("CR4", "Four-strand angular weave", "Sheet C · right row 4", "nested opposed courses", "The existing authored weave is read as four continuous routes, with overlapping outer, inner and whole selections. These centre lines deliberately omit ribbon masking; local depth remains in the linked weave study.", Weave)
    ];

    private static readonly Pt[] Running = Points(0,0, 3,0, 3,4, 6,4, 6,2, 5,2, 5,0);
    private static readonly Pt[] Hook = Points(0,2, 3,2, 3,1, 2,1, 2,0, 5,0, 5,1, 4,1, 4,2);
    private static readonly Pt[] Battlement = Points(0,0, 2,0, 2,2, 4,2, 4,0);
    private static readonly Pt[] Interlock = FromMoves(0,0, 7,0, 0,-3, -2,0, 0,1, 1,0, 0,1, -2,0, 0,-3, 7,0, 0,3, -2,0, 0,-1, 1,0, 0,-1, -2,0, 0,3);
    private static readonly Pt[] SpiralMeander = Points(0,0, 0,6, 6,6, 6,1, 2,1, 2,4, 4,4, 4,3, 3,3, 3,2, 5,2, 5,5, 1,5, 1,0, 7,0);
    private static readonly Pt[] Zig = Points(0,0, 4,4, 8,0);

    private sealed class Builder
    {
        public Curve.Frame Frame { get; } = new(new QualitativeMotionFrame());
        public List<Relation> Routes { get; } = [];
        public List<string> Roles { get; } = [];
        public List<Relation> Applications { get; } = [];
        private readonly List<Relation> selections = [];
        private readonly List<string> groupIds = [];
        private readonly List<string> groupNames = [];
        public Relation Words(Pt[] points) => S(points.Select(p => (IElement)Frame.Word(p.X, p.Y)).ToArray());

        public int Add(Relation words, string role, int dx = 0, int dy = 0,
            int xx = 1, int xy = 0, int yx = 0, int yy = 1, bool reverse = false,
            int repeats = 1, int pitch = 0, bool close = false)
        {
            var map = Parts.Map(Frame, Frame.Word(xx, yx), Frame.Word(-xx, -yx),
                Frame.Word(xy, yy), Frame.Word(-xy, -yy));
            var sections = new List<IElement>(); var receipts = new List<IElement>();
            var spots = new List<IElement>();
            IElement origin = new Placeholder(); IElement cursor = origin;
            Relation? previous = null;
            for (var r = 0; r < repeats; r++)
            {
                var shift = Frame.Word(dx + r * pitch, dy);
                var source = E(words).Cast<Relation>().ToArray();
                if (reverse) Array.Reverse(source);
                var positions = source.Select(word =>
                {
                    var substitution = map.Apply(word);
                    if (!map.TryReadComplete(substitution, out var image)) throw new InvalidOperationException("Incomplete authored substitution.");
                    var sum = Frame.Motion.Reduce(shift, image);
                    if (!Frame.Motion.TryReadResult(sum, out var result, out _, out _)) throw new InvalidOperationException("Incomplete authored placement.");
                    receipts.Add(new Relation(new Relation(word, result), new Relation(substitution, sum)));
                    return result;
                }).ToArray();
                if (positions.Length == 1) spots.Add(new Relation(cursor, positions[0]));
                if (previous is not null && (!Frame.Vector(previous, out var p) || !Frame.Vector(positions[0], out var q) || p != q))
                    throw new InvalidOperationException("A repeated course cannot attach one shared site at different positions.");
                for (var i = 1; i < positions.Length; i++)
                {
                    IElement next = close && r == repeats - 1 && i == positions.Length - 1 ? origin : new Placeholder();
                    var section = Frame.Line(cursor, next, positions[i - 1], positions[i]);
                    if (!Frame.Read(section).Complete) throw new InvalidOperationException("Unreadable authored section.");
                    sections.Add(section); cursor = next;
                }
                previous = positions[^1];
            }
            var route = S(S(sections.ToArray()), S(spots.ToArray()));
            var instruction = new Relation(map.Surface, Frame.Word(dx, dy));
            var repeat = new Relation(Frame.Word(pitch, 0), S(Enumerable.Range(0, repeats).Select(_ => (IElement)new Placeholder()).ToArray()));
            Applications.Add(new Relation(new Relation(words, new Relation(instruction, repeat)), new Relation(S(receipts.ToArray()), route)));
            Routes.Add(route); Roles.Add(role); return Routes.Count - 1;
        }

        public void Group(string id, string name, params int[] pathIds)
        { selections.Add(S(pathIds.Select(i => (IElement)Routes[i]).ToArray())); groupIds.Add(id); groupNames.Add(name); }
        public void Rails(int pitch, int repeats, int low, int high, bool avoidExistingSupport = false)
        {
            var indices = new List<int>(); var width = Math.Max(pitch * repeats, Routes.SelectMany(route => E((Relation)E(route)[0]).Cast<Relation>())
                .Select(Frame.Read).Where(r => r.Complete).SelectMany(r => r.Controls).Select(p => checked((int)p.X)).DefaultIfEmpty(0).Max());
            foreach (var level in new[] { low, high })
            {
                var occupied = avoidExistingSupport ? Routes.SelectMany(route => E((Relation)E(route)[0]).Cast<Relation>())
                    .Select(Frame.Read).Where(r => r.Complete && r.Controls[0].Y == level && r.Controls[^1].Y == level)
                    .Select(r => (A: Math.Max(0, checked((int)BigInteger.Min(r.Controls[0].X,r.Controls[^1].X))),
                        B: Math.Min(width, checked((int)BigInteger.Max(r.Controls[0].X,r.Controls[^1].X)))))
                    .Where(r => r.B > r.A).OrderBy(r => r.A).ToArray() : [];
                var cursor = 0;
                foreach (var span in occupied.Append((A: width, B: width)))
                {
                    if (span.A > cursor) indices.Add(Add(Words(Points(cursor,level, span.A,level)),
                        level == low ? "lower framing contribution" : "upper framing contribution"));
                    cursor = Math.Max(cursor,span.B);
                }
            }
            Group("rails", avoidExistingSupport ? "Framing contributions" : "Independent framing rails", indices.ToArray());
        }
        public Fixture Finish(int pitch)
        {
            Group("all", "All centre lines", Enumerable.Range(0, Routes.Count).ToArray());
            return new(Frame, S(Frame.Surface, Frame.Word(pitch, 0), S(Routes.Cast<IElement>().ToArray()),
                S(selections.Cast<IElement>().ToArray()), S(Applications.Cast<IElement>().ToArray())),
                Roles.ToArray(), groupIds.ToArray(), groupNames.ToArray());
        }
    }

    private static Fixture Course(Pt[] points, int pitch, int repeats, bool rails, bool attachedRails = false)
    {
        var b = new Builder(); b.Add(b.Words(points), "continuous course", repeats: repeats, pitch: pitch);
        b.Group("course", "Main course", 0);
        if (rails) b.Rails(pitch, repeats, points.Min(p => p.Y) - (attachedRails ? 0 : 1), points.Max(p => p.Y) + (attachedRails ? 0 : 1), avoidExistingSupport: attachedRails);
        return b.Finish(pitch);
    }

    private static Fixture SpiralCells()
    {
        var b = new Builder(); var words = b.Words(Points(0,4, 4,4, 4,1, 1,1, 1,3, 3,3, 3,2, 2,2));
        const int count = 6;
        for (var i = 0; i < count; i++)
            b.Add(words, "finite cell coil", dx: 5*i + (i%2 == 0 ? 0 : 5), dy: i%2 == 0 ? 0 : 5,
                xx: i%2 == 0 ? 1 : -1, yy: i%2 == 0 ? 1 : -1);
        b.Group("spirals", "Independent finite coils", Enumerable.Range(0,count).ToArray());
        var separator = b.Words(Points(0,0, 0,5)); var dividers = new List<int>();
        for (var i = 0; i <= count; i++) dividers.Add(b.Add(separator, "cell separator", dx: 5*i));
        b.Group("separators", "Cell separators", dividers.ToArray());
        b.Rails(5, count, 0, 5); return b.Finish(10);
    }

    private static Fixture WindowKey()
    {
        var b = new Builder(); var route = b.Words(Points(0,0, 7,0, 7,-3, 9,-3, 9,-1, 11,-1, 11,-4, 18,-4, 18,-2, 16,-2, 16,0));
        b.Add(route, "first running key", repeats: 3, pitch: 16);
        b.Add(route, "phase-shifted running key", dx: 8, repeats: 3, pitch: 16);
        b.Group("first", "First key course", 0); b.Group("second", "Offset key course", 1); b.Group("course", "Both key courses", 0, 1);
        var square = b.Words(Points(0,0, 2,0, 2,2, 0,2, 0,0));
        var dividerH = b.Words(Points(0,1, 2,1)); var dividerV = b.Words(Points(1,0, 1,2));
        var inserts = new List<int>();
        for (var i = 0; i < 7; i++)
        { inserts.Add(b.Add(square, "window boundary", dx: 4 + 8 * i, dy: -3, close: true)); inserts.Add(b.Add(dividerH, "window crossbar", dx: 4 + 8 * i, dy: -3)); inserts.Add(b.Add(dividerV, "window upright", dx: 4 + 8 * i, dy: -3)); }
        b.Group("inserts", "Four-window inserts", inserts.ToArray()); b.Rails(16, 3, -5, 1); return b.Finish(16);
    }

    private static Fixture OffsetKeys(bool rails)
    {
        var b = new Builder(); var words = b.Words(Points(0,0, 4,0, 4,-2, 2,-2, 2,-4, 6,-4, 6,-1, 8,-1, 8,-3, 10,-3, 10,0));
        b.Add(words, "first key course", repeats: 3, pitch: 10);
        b.Add(words, "offset key course", dx: 5, repeats: 3, pitch: 10);
        b.Group("first", "First course", 0); b.Group("second", "Offset course", 1); b.Group("pair", "Both offset courses", 0, 1);
        if (rails) b.Rails(10, 3, -5, 1); return b.Finish(10);
    }

    private static Fixture Medallions()
    {
        var b = new Builder(); var loops = new List<int>(); var hooks = new List<int>();
        var squares = new[] { Points(0,0, 10,0, 10,10, 0,10, 0,0), Points(2,2, 8,2, 8,8, 2,8, 2,2), Points(4,4, 6,4, 6,6, 4,6, 4,4) }.Select(b.Words).ToArray();
        var hook = b.Words(Points(1,1, 4,1, 4,4, 1,4, 1,2, 3,2, 3,3, 2,3));
        for (var tile = 0; tile < 3; tile++)
        {
            foreach (var square in squares) loops.Add(b.Add(square, "nested square", dx: tile * 24, close: true));
            hooks.Add(b.Add(hook, "quarter hook 0", dx: tile * 24 + 17, dy: 5));
            hooks.Add(b.Add(hook, "quarter hook 1", dx: tile * 24 + 17, dy: 5, xx: 0, xy: -1, yx: 1, yy: 0));
            hooks.Add(b.Add(hook, "quarter hook 2", dx: tile * 24 + 17, dy: 5, xx: -1, yy: -1));
            hooks.Add(b.Add(hook, "quarter hook 3", dx: tile * 24 + 17, dy: 5, xx: 0, xy: 1, yx: -1, yy: 0));
        }
        b.Group("squares", "Nested square medallions", loops.ToArray()); b.Group("hooks", "Four turned hooks", hooks.ToArray());
        b.Rails(24, 3, -1, 11); return b.Finish(24);
    }

    private static Fixture StepLinks(bool rails)
    {
        var b = new Builder(); var words = b.Words(Points(0,0, 0,1, 2,1, 2,2, 4,2, 4,0, 6,0));
        b.Add(words, "stepped course", repeats: 4, pitch: 6);
        b.Add(words, "phase-shifted stepped course", dx: 3, repeats: 4, pitch: 6);
        b.Group("first", "First stepped course", 0); b.Group("second", "Opposed stepped course", 1); b.Group("pair", "Paired steps", 0, 1);
        if (rails) b.Rails(6, 4, -1, 3); return b.Finish(6);
    }

    private static Fixture BrokenLinks()
    {
        var b = new Builder(); var course = b.Words(Points(0,1, 0,3, 2,3, 2,1, 4,1));
        b.Add(course, "upper battlement", repeats: 4, pitch: 4);
        b.Add(course, "offset lower battlement", dx: 1, dy: -1, repeats: 4, pitch: 4);
        var spot = b.Words(Points(0,0)); var spots = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            spots.Add(b.Add(spot, "upper detached spot", dx: 4 * i + 3, dy: 3));
            spots.Add(b.Add(spot, "lower detached spot", dx: 4 * i + 2, dy: 0));
        }
        b.Group("first", "Upper battlement", 0); b.Group("second", "Offset lower battlement", 1); b.Group("hooks", "Both battlement courses", 0, 1);
        b.Group("spots", "Detached spot pens", spots.ToArray()); return b.Finish(4);
    }

    private static Fixture DoubleCrosses()
    {
        var b = new Builder(); var word = b.Words(Points(0,0, 4,4));
        var lower = new List<int>(); var upper = new List<int>();
        for (var tile = 0; tile < 5; tile++)
        {
            lower.Add(b.Add(word, "lower rising arm", dx: tile * 6));
            lower.Add(b.Add(word, "lower falling arm", dx: tile * 6, dy: 4, yy: -1));
            upper.Add(b.Add(word, "upper rising arm", dx: tile * 6, dy: 4));
            upper.Add(b.Add(word, "upper falling arm", dx: tile * 6, dy: 8, yy: -1));
        }
        b.Group("lower", "Lower crosses", lower.ToArray()); b.Group("upper", "Upper crosses", upper.ToArray());
        b.Rails(6, 5, -1, 9); return b.Finish(6);
    }

    private static Fixture Zigzags(int count, bool rails)
    {
        var b = new Builder(); var words = b.Words(Zig);
        b.Add(words, "ascending zigzag", repeats: 4, pitch: 8);
        if (count > 1) b.Add(words, "opposed zigzag", dy: 4, yy: -1, repeats: 4, pitch: 8);
        if (count > 2)
        { b.Add(words, "offset ascending zigzag", dx: 2, repeats: 4, pitch: 8); b.Add(words, "offset opposed zigzag", dx: 2, dy: 4, yy: -1, repeats: 4, pitch: 8); }
        b.Group("first", "First course", 0);
        if (count > 1) b.Group("pair", "First opposed pair", 0, 1);
        if (count > 2) b.Group("offset", "Offset opposed pair", 2, 3);
        if (rails) b.Rails(8, 4, -1, 5); return b.Finish(8);
    }

    private static Fixture DiagonalLattice()
    {
        var b = new Builder(); var rising = new List<int>(); var falling = new List<int>();
        var words = new Dictionary<int, Relation>();
        const int width = 32, height = 4;
        for (var c = -height + 2; c < width; c += 2)
        {
            var fromX = Math.Max(0,c); var fromY = Math.Max(0,-c);
            var toX = Math.Min(width,c+height); var reach = toX-fromX;
            if (!words.TryGetValue(reach, out var word)) words.Add(reach, word = b.Words(Points(0,0, reach,reach)));
            rising.Add(b.Add(word, "clipped rising diagonal", dx: fromX, dy: fromY));
            falling.Add(b.Add(word, "clipped falling diagonal", dx: fromX, dy: height-fromY, yy: -1));
        }
        b.Group("rising", "Rising diagonal family", rising.ToArray());
        b.Group("falling", "Falling diagonal family", falling.ToArray());
        return b.Finish(2);
    }

    private static Fixture Weave()
    {
        var weave = AngularWeaveStudy.Build(); var root = E(weave.Surface);
        var routes = E((Relation)root[3]).Cast<Relation>().Select(s => S((Relation)E(s)[0], S())).ToArray();
        var selections = S(S(routes[0], routes[1]), S(routes[2], routes[3]), S(routes));
        return new(weave.Frame, S(weave.Frame.Surface, root[1], S(routes), selections, S(weave.Surface)),
            ["outer course A", "outer course B", "inner course A", "inner course B"],
            ["outer", "inner", "all"], ["Outer pair", "Inner pair", "All four strands"]);
    }

    public static Fixture BuildPair(int variant = 0)
    {
        if (variant is < 0 or > 4) throw new ArgumentOutOfRangeException(nameof(variant));
        var b = new Builder();
        var points = variant == 4 ? Points(0,0, 2,0, 4,2, 8,2, 10,0) : Points(0,0, 2,2, 6,2, 8,0);
        var words = b.Words(points); var pitch = points[^1].X;
        b.Add(words, "source course");
        b.Add(words, "opposed course", dx: variant == 1 ? 2 : 0, dy: variant == 1 ? 1 : 0,
            yy: variant == 2 ? -2 : -1, reverse: variant == 3);
        b.Group("first", "Source course", 0); b.Group("second", "Opposed course", 1); b.Group("pair", "Both courses", 0, 1);
        return b.Finish(pitch);
    }

    /// <summary>The captions only name the projection. Geometry, order,
    /// membership, topology and comparisons are obtained from the native root.</summary>
    public static PatternRead Read(Fixture f, string id = "fixture", string name = "Centre-line fixture",
        string source = "", string family = "", string scope = "", bool includeSelectedPairQueries = false)
    {
        var fields = E(f.Surface);
        if (fields.Length != 5 || !Same(fields[0], f.Frame.Surface) || fields[1] is not Relation pitchWord ||
            !f.Frame.Vector(pitchWord, out var pitch)) throw new ArgumentException("Expected the centre-line root grammar.");
        var routes = E((Relation)fields[2]).Cast<Relation>().ToArray(); var selections = E((Relation)fields[3]).Cast<Relation>().ToArray();
        if (routes.Length != f.Roles.Length || selections.Length != f.GroupIds.Length || selections.Length != f.GroupNames.Length)
            throw new ArgumentException("Editorial names must cover the native projection without changing it.");
        var sites = new List<IElement>(); var sitePositions = new List<Pt>();
        int Site(IElement site, Pt position)
        {
            int at = sites.FindIndex(s => Same(s, site));
            if (at >= 0)
            {
                if (sitePositions[at] != position) throw new ArgumentException("One native site cannot occupy incompatible positions in this frame.");
                return at;
            }
            sites.Add(site); sitePositions.Add(position); return sites.Count - 1;
        }
        var paths = new List<PathRead>(); var edges = new List<(int Path, Pt A, Pt B, int From, int To)>();
        int left = 0, right = 0, straight = 0, uTurns = 0, spotCount = 0;
        for (var p = 0; p < routes.Length; p++)
        {
            var body = E(routes[p]);
            if (body.Length != 2) throw new ArgumentException("Expected ordered sections and spot scope.");
            var sections = E((Relation)body[0]).Cast<Relation>().Select(f.Frame.Read).ToArray();
            var spots = E((Relation)body[1]).Cast<Relation>().ToArray();
            var points = new List<Pt>(); var pathSites = new List<int>();
            if (sections.Length > 0)
            {
                if (spots.Length > 0 || sections.Any(s => !s.Complete || s.Controls.Length != 2)) throw new ArgumentException("This reader accepts complete line sections or a point pen.");
                foreach (var pair in sections.Zip(sections.Skip(1)))
                    if (!Same(pair.First.End, pair.Second.Start) || pair.First.Controls[^1] != pair.Second.Controls[0]) throw new ArgumentException("Route has an unaccounted handoff.");
                points.Add(Point(sections[0].Controls[0])); pathSites.Add(Site(sections[0].Start, points[0]));
                foreach (var section in sections)
                {
                    var a = Point(section.Controls[0]); var b = Point(section.Controls[1]);
                    if (a == b) throw new ArgumentException("A zero line is not a point pen.");
                    edges.Add((p, a, b, Site(section.Start, a), Site(section.End, b)));
                    points.Add(b); pathSites.Add(Site(section.End, b));
                }
            }
            else
            {
                if (spots.Length != 1 || spots[0].Right is not Relation word || !f.Frame.Vector(word, out var point)) throw new ArgumentException("A spot pen needs one connected site and position.");
                points.Add(Point(point)); pathSites.Add(Site(spots[0].Left, points[0])); spotCount++;
            }
            bool closed = sections.Length > 0 && Same(sections[0].Start, sections[^1].End) && points[0] == points[^1];
            var vectors = points.Zip(points.Skip(1), (a, b) => new Pt(b.X-a.X, b.Y-a.Y)).ToArray();
            var turns = new List<string>();
            var adjacent = vectors.Zip(vectors.Skip(1));
            if (closed) adjacent = adjacent.Append((vectors[^1], vectors[0]));
            foreach (var pair in adjacent)
            {
                var cross = Cross(pair.First, pair.Second);
                if (cross > 0) { turns.Add("L"); left++; }
                else if (cross < 0) { turns.Add("R"); right++; }
                else if ((BigInteger)pair.First.X * pair.Second.X + (BigInteger)pair.First.Y * pair.Second.Y < 0) { turns.Add("U"); uTurns++; }
                else { turns.Add("S"); straight++; }
            }
            paths.Add(new(p, f.Roles[p], points.Select(v => new[] { v.X, v.Y }).ToArray(), closed,
                string.Join(" ", turns), string.Join(" ", vectors.Select(v => $"({v.X},{v.Y})")), pathSites.ToArray()));
        }
        var groups = selections.Select((selection, i) =>
        {
            var ids = E(selection).Select(route => Array.FindIndex(routes, r => Same(r, route))).ToArray();
            if (ids.Any(at => at < 0)) throw new ArgumentException("A selection reaches outside this structure's route surface.");
            return new GroupRead(f.GroupIds[i], f.GroupNames[i], ids);
        }).ToArray();
        int crossings = 0, contacts = 0, overlaps = 0;
        for (var a = 0; a < edges.Count; a++) for (var b = a + 1; b < edges.Count; b++)
        {
            var first = edges[a]; var second = edges[b];
            if (InteriorCross(first.A, first.B, second.A, second.B)) crossings++;
            else if (Overlap(first.A, first.B, second.A, second.B)) overlaps++;
            else
            {
                bool Unjoined(Pt point, int site, Pt a, Pt b, int from, int to) => OnSegment(point, a, b) &&
                    !(point == a && site == from) && !(point == b && site == to);
                if (Unjoined(first.A, first.From, second.A, second.B, second.From, second.To) ||
                    Unjoined(first.B, first.To, second.A, second.B, second.From, second.To) ||
                    Unjoined(second.A, second.From, first.A, first.B, first.From, first.To) ||
                    Unjoined(second.B, second.To, first.A, first.B, first.From, first.To)) contacts++;
            }
        }
        var parent = Enumerable.Range(0, sites.Count).ToArray();
        int Root(int i) { while (parent[i] != i) i = parent[i]; return i; }
        foreach (var edge in edges) parent[Root(edge.To)] = Root(edge.From);
        var metrics = new Metrics(paths.Count, edges.Count, spotCount, paths.Count(p => p.Closed), parent.Select(Root).Distinct().Count(),
            left, right, straight, uTurns, crossings, contacts, overlaps);
        var pairs = new List<PairRead>();
        for (var a = 0; a < paths.Count; a++) for (var b = a + 1; b < paths.Count; b++)
        {
            if (paths[a].Points.Length < 2 || paths[a].Points.Length != paths[b].Points.Length) continue;
            // These are bounded ordered correspondence proposals, not discovery
            // of an arbitrary subpath, cyclic phase or graph isomorphism.
            foreach (var transform in new[] { "translation", "vertical-reflection+translation", "half-turn+translation", "quarter-turn+translation" })
            {
                var read = Compare(paths[a], paths[b], transform);
                if (read.Exact) pairs.Add(read);
            }
        }
        if (includeSelectedPairQueries && paths.Count >= 2)
        {
            pairs.RemoveAll(p => p.First == 0 && p.Second == 1 && p.Transform is
                "fixed-axis vertical-reflection" or "vertical-reflection+translation" or "vertical-reflection+reverse");
            pairs.Insert(0, Compare(paths[0], paths[1], "fixed-axis vertical-reflection"));
            pairs.Insert(1, Compare(paths[0], paths[1], "vertical-reflection+translation"));
            pairs.Insert(2, Compare(paths[0], paths[1], "vertical-reflection+reverse"));
        }
        return new(id, name, source, family, scope, checked((int)pitch.X), paths.ToArray(), groups, metrics, pairs.ToArray());
    }

    public static PairRead Compare(PathRead first, PathRead second, string transform)
    {
        if (first.Points.Length == 0 || second.Points.Length == 0 || first.Points.Concat(second.Points).Any(p => p.Length != 2))
            throw new ArgumentException("A bounded comparison needs nonempty two-coordinate point sequences.");
        var a = first.Points.Select(p => new Pt(p[0], p[1])).ToArray();
        var b = second.Points.Select(p => new Pt(p[0], p[1])).ToArray();
        if (transform == "vertical-reflection+reverse") Array.Reverse(b);
        Pt Map(Pt p) => transform switch
        {
            "translation" => p,
            "vertical-reflection+translation" or "fixed-axis vertical-reflection" or "vertical-reflection+reverse" => new(p.X, -p.Y),
            "half-turn+translation" => new(-p.X, -p.Y),
            "quarter-turn+translation" => new(-p.Y, p.X),
            _ => throw new ArgumentException("Unknown bounded transform.", nameof(transform))
        };
        var mapped = a.Select(Map).ToArray();
        var shift = transform == "fixed-axis vertical-reflection" ? new Pt(0,0) : new Pt(b[0].X-mapped[0].X, b[0].Y-mapped[0].Y);
        var matched = mapped.Zip(b).Count(p => p.First.X + shift.X == p.Second.X && p.First.Y + shift.Y == p.Second.Y);
        return new(first.Id, second.Id, transform, matched, Math.Max(a.Length, b.Length), a.Length == b.Length && matched == a.Length,
            [shift.X, shift.Y], transform == "vertical-reflection+reverse" ? "The target is traversed backwards for this comparison; native ownership and flow are unchanged." :
            "Ordered point correspondence in the declared common-reach frame. Translation is fitted from the first pair; it does not imply shared sites, flow transfer or original construction history.");
    }

    private static bool InteriorCross(Pt a, Pt b, Pt c, Pt d)
    {
        var ab = Sub(b,a); var cd = Sub(d,c); var ac = Sub(c,a); var den = Cross(ab,cd);
        if (den == 0) return false; var t = Cross(ac,cd); var u = Cross(ac,ab);
        if (den < 0) { den = -den; t = -t; u = -u; }
        return t > 0 && t < den && u > 0 && u < den;
    }
    private static bool Overlap(Pt a, Pt b, Pt c, Pt d)
    {
        var ab = Sub(b,a); if (Cross(ab, Sub(c,a)) != 0 || Cross(ab, Sub(d,a)) != 0) return false;
        return ab.X != 0 ? Math.Max(Math.Min(a.X,b.X), Math.Min(c.X,d.X)) < Math.Min(Math.Max(a.X,b.X), Math.Max(c.X,d.X)) :
            Math.Max(Math.Min(a.Y,b.Y), Math.Min(c.Y,d.Y)) < Math.Min(Math.Max(a.Y,b.Y), Math.Max(c.Y,d.Y));
    }
    private static bool OnSegment(Pt p, Pt a, Pt b) => Cross(Sub(p,a), Sub(b,a)) == 0 &&
        p.X >= Math.Min(a.X,b.X) && p.X <= Math.Max(a.X,b.X) && p.Y >= Math.Min(a.Y,b.Y) && p.Y <= Math.Max(a.Y,b.Y);
    private static Pt Sub(Pt a, Pt b) => new(a.X-b.X,a.Y-b.Y);
    private static BigInteger Cross(Pt a, Pt b) => (BigInteger)a.X*b.Y-(BigInteger)a.Y*b.X;
    private static Pt Point(Curve.Vector p) => new(checked((int)p.X), checked((int)p.Y));
    private static Pt[] Points(params int[] pairs) => Enumerable.Range(0, pairs.Length / 2).Select(i => new Pt(pairs[2*i], pairs[2*i+1])).ToArray();
    private static Pt[] FromMoves(int x, int y, params int[] moves)
    { var points = new List<Pt> { new(x,y) }; for (var i=0;i<moves.Length;i+=2) { x+=moves[i]; y+=moves[i+1]; points.Add(new(x,y)); } return points.ToArray(); }
    private static Relation S(params IElement[] parts) => StructuralConstraint.Closed(parts);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var parts, out var first, out var last) && first.Equals(last) ? parts.ToArray() : throw new ArgumentException("Expected closed native scope.");
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a,b) || a.Equals(b);
}
