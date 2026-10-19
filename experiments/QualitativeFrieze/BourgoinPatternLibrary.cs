using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Source-derived finite constructions. Authoring coordinates count
/// participation in one shared unmeasured reach; they build native words,
/// endpoint roles and reusable seam rules. Runtime receives only those native
/// programs. Titles and book references never select execution.</summary>
public sealed class BourgoinPatternLibrary
{
    /// <summary>An optional measured screen interpretation, outside the program.</summary>
    public sealed record DisplayBasis(double RX, double RY, double UX, double UY);
    public sealed record Recipe(string Id, string Name, int Leaf, int PrintedPage,
        string Reference, string Construction, string Fidelity, OrnamentProgram Program,
        Relation Bindings, Relation Window, Relation ColumnPitch, Relation RowPitch,
        Relation SeamContract, DisplayBasis DisplayBasis)
    {
        public QualitativeMotionFrame Frame => Program.Frame;
        public Relation Motifs => Program.Motifs;
        public Relation Transforms => Program.Transforms;
        public string SourceUrl => $"https://archive.org/details/thoriedelorneme01bourgoog/page/n{Leaf}/mode/1up";
    }

    public IReadOnlyList<Recipe> Recipes { get; }
    private static readonly DisplayBasis Cartesian = new(1, 0, 0, 1);
    private static readonly DisplayBasis Triangular = new(1, 0, .5, Math.Sqrt(3) / 2);

    public BourgoinPatternLibrary()
    {
        var recipes = new List<Recipe>();

        var brick = new Cell();
        brick.Path(false, (0, 0), (2, 0), (4, 0));
        brick.Path(false, (0, 2), (2, 2), (4, 2));
        brick.Path(false, (0, 0), (0, 2)); brick.Path(false, (2, 2), (2, 4));
        Add("B01", "Running brick bond", 274, 251, "Fig. 186",
            "Two horizontal mortar courses and alternating vertical joints form one macrocell. The next course shares the existing horizontal endpoint roles; no intersection creates a site implicitly.",
            "Diagram-skeleton reconstruction; brick proportions and the finite crop are selected here.", brick, (4, 0), (0, 4));

        var basket = new Cell();
        basket.Polygon((0, 0), (1, 0), (2, 0), (2, 1), (1, 1), (0, 1));
        basket.Polygon((2, 0), (3, 0), (3, 1), (3, 2), (2, 2), (2, 1));
        basket.Polygon((1, 2), (2, 2), (3, 2), (3, 3), (2, 3), (1, 3));
        basket.Polygon((0, 1), (1, 1), (1, 2), (1, 3), (0, 3), (0, 2));
        Add("B02", "Basket rectangles and square gaps", 275, 252, "Fig. 188",
            "Four turned rectangular contours surround one square gap. The macrocell repeats in both directions; published mid-edge landmarks make partial shared borders explicit.",
            "Source-derived rectangular/square arrangement with chosen proportions; not a line-for-line tracing.", basket, (3, 0), (0, 3));

        var alternation = new Cell();
        Square(alternation, 0, 0); Square(alternation, 3, 3);
        Diamond(alternation, 3, 0, 2); Diamond(alternation, 0, 3, 2);
        Add("B03", "Squares meet diamonds", 275, 252, "Fig. 189",
            "Two squares and two corner-oriented four-side contours form one alternating macrocell. Diamond tips meet explicitly split square sides through endpoint ports.",
            "Adaptation of the square/corner-square opposition; no claim of the source's exact spacing or metric square equivalence.", alternation, (6, 0), (0, 6));

        var honey = new Cell();
        Hexagon(honey, 0, 0, 2); Hexagon(honey, 2, 2, 2); Hexagon(honey, 4, 4, 2);
        Add("B04", "Honeycomb from three reused contours", 277, 254, "Fig. 196",
            "Three translated six-edge contours form a periodic macrocell. Neighbouring contours share advertised endpoint sites; their coincident stroke histories remain separate participation.",
            "Reconstruction of the honeycomb incidence pattern. Regular-looking hexagons come from the optional triangular display, not a native angle or length theorem.", honey, (6, 0), (0, 6), triangular: true);

        var trihex = new Cell(); Hexagon(trihex, 0, 0, 2);
        Add("B05", "Hexagons with triangular interstices", 277, 254, "Fig. 197",
            "One six-edge contour repeats on a triangular disposition. Its vertices meet neighbouring contours; three-sided gaps emerge from those connections rather than a separately placed triangle glyph.",
            "Trihexagonal topology reconstruction with a selected triangular display basis; regular-polygon calibration remains outside the program.", trihex, (4, 0), (0, 4), triangular: true);

        var octagon = new Cell();
        octagon.Polygon((2, 0), (4, 0), (6, 2), (6, 4), (4, 6), (2, 6), (0, 4), (0, 2));
        Add("B06", "Octagons leave small squares", 278, 255, "Fig. 201",
            "Four chamfered corners replace the corners of each repeated square footprint. Joining the published side endpoints closes the small interstitial four-edge compartments.",
            "Octagon/square diagram skeleton. Eight equal Euclidean sides are not asserted; the chamfer and straight-side proportions are selected.", octagon, (6, 0), (0, 6));

        var bands = new Cell();
        bands.Path(false, (0, 0), (2, 0), (6, 0)); bands.Path(false, (0, 2), (2, 2), (6, 2));
        bands.Path(false, (0, 0), (0, 2), (0, 6)); bands.Path(false, (2, 0), (2, 2), (2, 6));
        Add("B07", "Alternating-width orthogonal bands", 279, 256, "Fig. 202",
            "Two narrow/wide interval sequences cross. Every intended junction is already a vertex in both local paths; the field contract supplies the shared-site read.",
            "Diagram reconstruction with an authored narrow/wide relation; the source's proportions are not copied exactly.", bands, (6, 0), (0, 6));

        var trellis = new Cell();
        var along = new[] { (0, 0), (2, 0), (3, 1), (5, 1), (6, 0), (8, 0) };
        var across = new[] { (0, 0), (0, 2), (-1, 3), (-1, 5), (0, 6), (0, 8) };
        trellis.Path(false, along.Select(Slant).ToArray()); trellis.Path(false, across.Select(Slant).ToArray());
        Add("B08", "Cranked diagonal trellis", 279, 256, "Fig. 203",
            "Two cranked line systems use the same local construction under an oblique word correspondence. Their endpoints continue from cell to cell; the remaining compartments are read from the connected field.",
            "Source-inspired lozenge/rectangle trellis. Bend proportions and the explicit endpoint-only incidence are this reconstruction's choices.", trellis, (8, 8), (8, -8));

        var key = new Cell(); key.Path(false, Key());
        key.Path(false, (0, -1), (10, -1)); key.Path(false, (0, 9), (10, 9));
        Add("B09", "Greek-key border control", 392, 343, "Plate 1, fig. 1 (rectilinear border)",
            "One non-retracing inward/outward key passage returns to the next repeat boundary. Two independent rails frame the strip; the experiment repeats the finished strip in rows.",
            "Source-inspired rectilinear Greek-key control, not a tracing of the particular historic border. The second repetition direction is added for this field experiment.", key, (10, 0), (0, 12), rows: 3);

        var bent = BentCell();
        Add("B10", "Direct bent-cross compartments", 408, 349, "Plate 9, fig. 4",
            "A horizontal cranked passage and its quarter-turned counterpart meet at one exposed root. Direct repetition continues both systems across the field.",
            "Reconstructed bent-cross grammar inspired by the source; exact channel widths and handedness are selected, not traced.", bent, (6, 0), (0, 6));

        var returned = BentCell();
        Add("B11", "Returned rows of bent crosses", 408, 350, "Plate 9, fig. 5",
            "The same two-passage motif is read through alternating identity and left/right-reflection substitutions. A two-phase row cycle, not a renderer switch, chooses the actual transform binding.",
            "Source-inspired direct/returned conjugation experiment. Its explicit line network is an adaptation of the plate's construction rule.", returned, (6, 0), (0, 6), alternateRows: true);

        var spirals = new Cell();
        var arm = new[] { (-8, 0), (-6, 0), (-6, 5), (-1, 5), (-1, 1), (-4, 1), (-4, 3), (-3, 3), (-3, 2) };
        spirals.QuarterPaths(arm);
        Add("B12", "Fourfold spiral-arm field", 408, 350, "Plate 9, fig. 10",
            "Four turned inward spiral passages surround one unfilled central region. Their outer endpoint roles pair with neighbouring cells, retaining the inward free ends as part of the construction.",
            "Source-inspired spiral-arm reconstruction. The source's star compartments and exact line continuations are not claimed to be reproduced completely.", spirals, (16, 0), (0, 16));

        var fourfold = new Cell();
        var quarter = new[] { (0, -8), (8, -8), (8, -2), (2, -2), (2, -6), (6, -6),
            (6, -4), (4, -4), (4, -5), (3, -5), (3, -3), (7, -3), (7, -7),
            (1, -7), (1, -1), (8, -1), (8, 0) };
        fourfold.QuarterContour(quarter);
        Add("B13", "Four-part revolved key scaffold", 418, 355, "Plate 14, fig. 3",
            "One nested inward/outward key quarter is substituted around a common reference, then closed into a four-part contour. Parallel return channels preserve the key turns; repeated macrocells communicate through actual exposed corners.",
            "Source-inspired fourfold key scaffold with chosen channel spacing. Native substitution provenance preserves the revolved four-piece construction; the plate's exact crossings and proportions are not traced.", fourfold, (16, 0), (0, 16));

        var maze = new Cell(); maze.Path(false, Key());
        maze.Path(false, (0, 8), (0, 10));
        Add("B14", "Connected meander-grid adaptation", 428, 0, "Plate 19, fig. 4",
            "A reused inward/outward key passage supplies the horizontal flow; a second exposed boundary passage connects successive rows. The contract retains both boundary continuation and internal shared landmarks.",
            "Source-inspired dense rectilinear meander, with an added explicit row-connection construction. It is not an exact tracing or a claim of the same symmetry group.", maze, (10, 0), (0, 10));

        Recipes = recipes.AsReadOnly();

        void Add(string id, string name, int leaf, int page, string reference, string construction,
            string fidelity, Cell cell, (int X, int Y) column, (int X, int Y) row,
            bool triangular = false, bool alternateRows = false, int rows = 4)
        {
            var program = cell.Program(name, column, row, alternateRows, out var bindings);
            var phasePair = (Relation)program.Layout.Left;
            var startU = N.E((Relation)((Relation)phasePair.Left).Left)[0];
            var startV = N.E((Relation)((Relation)phasePair.Right).Left)[0];
            var window = OrnamentProgram.Window(startU, startV, 4, rows);
            recipes.Add(new(id, name, leaf, page, reference, construction, fidelity, program, bindings, window,
                cell.Word(column), cell.Word(row), cell.Seams(program, bindings, column, row), triangular ? Triangular : Cartesian));
        }
    }

    private static void Square(Cell cell, int x, int y) => cell.Polygon((x - 1, y - 1), (x, y - 1),
        (x + 1, y - 1), (x + 1, y), (x + 1, y + 1), (x, y + 1), (x - 1, y + 1), (x - 1, y));
    private static void Diamond(Cell cell, int x, int y, int r) => cell.Polygon((x + r, y), (x, y + r), (x - r, y), (x, y - r));
    private static void Hexagon(Cell cell, int x, int y, int r) => cell.Polygon((x + r, y), (x, y + r),
        (x - r, y + r), (x - r, y), (x, y - r), (x + r, y - r));
    private static (int X, int Y) Slant((int X, int Y) p) => (p.X + p.Y, p.X - p.Y);
    private static (int X, int Y)[] Key() => [(0, 0), (0, 8), (8, 8), (8, 2), (2, 2),
        (2, 6), (6, 6), (6, 4), (4, 4), (4, 5), (3, 5), (3, 3), (7, 3), (7, 7),
        (1, 7), (1, 1), (9, 1), (9, 0), (10, 0)];
    private static Cell BentCell()
    {
        var cell = new Cell();
        cell.Path(false, (0, 0), (2, 0), (2, 1), (4, 1), (4, 0), (6, 0));
        cell.Path(false, (0, 0), (0, 2), (-1, 2), (-1, 4), (0, 4), (0, 6));
        return cell;
    }

    private sealed class Cell
    {
        private readonly QualitativeMotionFrame frame = new();
        private readonly IElement reach = new Placeholder();
        private readonly List<IElement> paths = [], ports = [], closed = [];
        private readonly List<IElement> construction = [];
        private readonly List<(IElement Role, Relation Position)> published = [];

        public Relation Word((int X, int Y) point) => N.S(
            Enumerable.Range(0, Math.Abs(point.X)).Select(_ => (IElement)QualitativeMotionFrame.Move(point.X < 0 ? frame.Left : frame.Right, reach))
                .Concat(Enumerable.Range(0, Math.Abs(point.Y)).Select(_ => (IElement)QualitativeMotionFrame.Move(point.Y < 0 ? frame.Down : frame.Up, reach))).ToArray());

        public void Polygon(params (int X, int Y)[] vertices) => Path(true, [.. vertices, vertices[0]]);
        public void Path(bool closes, params (int X, int Y)[] vertices)
        {
            if (vertices.Length < 2) throw new ArgumentException("A source path needs at least one chord.", nameof(vertices));
            var chords = Enumerable.Range(0, vertices.Length - 1).Select(index => Word(
                (vertices[index + 1].X - vertices[index].X, vertices[index + 1].Y - vertices[index].Y))).ToArray();
            var path = new Relation(Word(vertices[0]), N.S(chords)); paths.Add(path);
            if (closes) closed.Add(path);
            for (var index = 0; index < vertices.Length; index++)
            {
                var role = new Placeholder();
                ports.Add(new Relation(role, new Relation(path, index == chords.Length ? path : chords[index])));
                published.Add((role, Word(vertices[index])));
            }
        }

        /// <summary>Actual output path words come from the preceding native
        /// substitution receipts. The source coordinates author only the seed.</summary>
        public void QuarterPaths((int X, int Y)[] seed)
        {
            var orbit = QuarterWords(seed);
            var outputs = Enumerable.Range(0, 4).Select(index => NativePath(orbit.Starts[index], orbit.Words[index], false)).ToArray();
            var parts = Enumerable.Range(0, 4).Select(index => Part(outputs[index], 0, orbit.Words[index], orbit.Starts[index])).ToArray();
            RetainQuarterConstruction(orbit, parts);
        }

        /// <summary>The executable contour remains flat. Actual quarter chord
        /// selections and cursor-prefix proofs retain its construction ladder.</summary>
        public void QuarterContour((int X, int Y)[] seed)
        {
            var orbit = QuarterWords(seed);
            var output = NativePath(orbit.Starts[0], orbit.Words.SelectMany(words => words).ToArray(), true);
            var parts = Enumerable.Range(0, 4).Select(index =>
                Part(output, index * orbit.Words[0].Length, orbit.Words[index], orbit.Starts[index])).ToArray();
            RetainQuarterConstruction(orbit, parts);
        }

        private sealed record GeneratedPath(Relation Path, Relation[] PortRows, Relation[] PrefixReads);
        private sealed record QuarterData(QualitativePatternTransform Transform, Relation[] Starts,
            Relation[][] Words, Relation[][] Receipts, Relation Cycle, QualitativeWordOrbit Orbit);

        private GeneratedPath NativePath(Relation start, Relation[] chords, bool closes)
        {
            var path = new Relation(start, N.S(chords)); paths.Add(path); if (closes) closed.Add(path);
            var actualPorts = new List<Relation>(); var prefixes = new List<Relation>(); var prefix = new List<IElement>();
            for (var index = 0; index <= chords.Length; index++)
            {
                var read = frame.Reduce(start, N.S(prefix.ToArray()));
                if (!frame.TryReadResult(read, out var position, out _, out var unknown) || !N.Read(position, out _) || N.E(unknown).Length != 0)
                    throw new InvalidOperationException("A generated part needs an exact formal cursor prefix.");
                var role = new Placeholder();
                var port = new Relation(role, new Relation(path, index == chords.Length ? path : chords[index]));
                ports.Add(port); actualPorts.Add(port); prefixes.Add(read); published.Add((role, position));
                if (index < chords.Length) prefix.AddRange(N.E(chords[index]));
            }
            return new(path, actualPorts.ToArray(), prefixes.ToArray());
        }

        // part = (actual path -> actual first-chord port selector) ->
        //        (declared start word -> (Closed(actual selected chord words) ->
        //         (actual path-prefix read -> zero start-correspondence read)))
        private Relation Part(GeneratedPath output, int first, Relation[] words, Relation start)
        {
            var prefixRead = output.PrefixReads[first];
            frame.TryReadResult(prefixRead, out var actualStart, out _, out _);
            var equality = frame.Reduce(actualStart, UnitArrangementRead.Inverse(frame, start));
            if (!UnitArrangementRead.IsZero(frame, equality)) throw new InvalidOperationException("A transformed quarter does not start at its actual path cursor.");
            return new(new Relation(output.Path, output.PortRows[first]),
                new Relation(start, new Relation(N.S(words), new Relation(prefixRead, equality))));
        }

        private QuarterData QuarterWords((int X, int Y)[] seed)
        {
            var transform = Map(frame.Up, frame.Down, frame.Left, frame.Right);
            var starts = new Relation[4]; var words = new Relation[4][]; var receipts = new Relation[4][];
            starts[0] = Word(seed[0]);
            words[0] = Enumerable.Range(0, seed.Length - 1).Select(index => Word(
                (seed[index + 1].X - seed[index].X, seed[index + 1].Y - seed[index].Y))).ToArray();
            for (var step = 0; step < 4; step++)
            {
                var reads = new List<Relation>();
                var mappedStart = Apply(starts[step]); var mappedWords = words[step].Select(Apply).ToArray();
                receipts[step] = reads.ToArray();
                if (step < 3) { starts[step + 1] = mappedStart; words[step + 1] = mappedWords; }
                Relation Apply(Relation source)
                {
                    var read = transform.Apply(source); reads.Add(read);
                    if (!transform.TryReadComplete(read, out var mapped)) throw new InvalidOperationException("A quarter-turn word substitution did not complete.");
                    return mapped;
                }
            }
            var phases = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
            var cycle = new Relation(N.S(phases), N.S(phases.Select((phase, index) =>
                (IElement)new Relation(phase, phases[(index + 1) % phases.Length])).ToArray()));
            var orbit = new QualitativeWordOrbit(transform, N.S([starts[0], .. words[0]]), cycle);
            if (!orbit.IsComplete) throw new InvalidOperationException("The actual source words did not return through the declared four-phase substitution.");
            return new(transform, starts, words, receipts, cycle, orbit);
        }

        private void RetainQuarterConstruction(QuarterData orbit, Relation[] parts)
        {
            var correspondences = new List<IElement>();
            for (var step = 0; step < 4; step++)
            {
                var next = (step + 1) % 4;
                var sourceWords = new[] { orbit.Starts[step] }.Concat(orbit.Words[step]).ToArray();
                var targetWords = new[] { orbit.Starts[next] }.Concat(orbit.Words[next]).ToArray();
                var maps = sourceWords.Select((source, index) =>
                {
                    var read = orbit.Receipts[step][index];
                    if (!orbit.Transform.TryReadComplete(read, out var actualOutput) ||
                        read.Left is not Relation selected || !ReferenceEquals(selected.Right, source))
                        throw new InvalidOperationException("A construction correspondence needs the actual source substitution receipt.");
                    var equality = frame.Reduce(actualOutput, UnitArrangementRead.Inverse(frame, targetWords[index]));
                    if (!UnitArrangementRead.IsZero(frame, equality)) throw new InvalidOperationException("A construction output does not match its selected actual target word.");
                    return (IElement)new Relation(new Relation(source, targetWords[index]), new Relation(read, equality));
                }).ToArray();
                correspondences.Add(new Relation(new Relation(parts[step], parts[next]), new Relation(orbit.Transform.Surface, N.S(maps))));
            }
            var phaseBindings = N.S(N.E((Relation)orbit.Cycle.Left).Select((phase, index) =>
                (IElement)new Relation(phase, parts[index])).ToArray());
            construction.Add(new Relation(new Relation(parts[0], N.S(parts)),
                new Relation(new Relation(orbit.Cycle, phaseBindings), new Relation(N.S(correspondences.ToArray()), orbit.Orbit.Surface))));
        }

        public OrnamentProgram Program(string name, (int X, int Y) column, (int X, int Y) row,
            bool alternateRows, out Relation bindings)
        {
            var motif = new UnitArrangementProgram(frame, name, N.S(paths.ToArray()), N.S(ports.ToArray()), N.S(closed.ToArray()), N.S(construction.ToArray()));
            var identity = Map(frame.Right, frame.Left, frame.Up, frame.Down);
            var mirror = Map(frame.Left, frame.Right, frame.Up, frame.Down);
            var turn = Map(frame.Up, frame.Down, frame.Left, frame.Right);
            var half = Map(frame.Left, frame.Right, frame.Down, frame.Up);
            var motifs = N.S(motif.Surface); var transforms = N.S(identity.Surface, mirror.Surface, turn.Surface, half.Surface);
            var m = new Placeholder(); var a = new Placeholder(); var b = new Placeholder();
            var cu = new Placeholder(); var rv = new Placeholder();
            var stepU = Word(column); var stepV = Word(row);
            var parameterRows = new List<IElement> { new Relation(m, motifs), new Relation(a, transforms),
                new Relation(cu, N.S(stepU)), new Relation(rv, N.S(stepV)) };
            var chosen = new List<IElement> { new Relation(m, motif.Surface), new Relation(a, identity.Surface),
                new Relation(cu, stepU), new Relation(rv, stepV) };
            if (alternateRows) { parameterRows.Add(new Relation(b, transforms)); chosen.Add(new Relation(b, mirror.Surface)); }
            IElement up = new Placeholder(), vp = new Placeholder(), nextV = new Placeholder();
            var uCycle = new Relation(N.S(up), N.S(new Relation(up, up)));
            var vCycle = alternateRows ? new Relation(N.S(vp, nextV), N.S(new Relation(vp, nextV), new Relation(nextV, vp)))
                : new Relation(N.S(vp), N.S(new Relation(vp, vp)));
            var rules = new List<IElement> { new Relation(new Relation(up, vp), new Relation(m, a)) };
            if (alternateRows) rules.Add(new Relation(new Relation(up, nextV), new Relation(m, b)));
            var layout = new Relation(new Relation(uCycle, vCycle), new Relation(new Relation(cu, rv), N.S(rules.ToArray())));
            bindings = N.S(chosen.ToArray());
            return new(frame, motifs, transforms, N.S(parameterRows.ToArray()), layout);
        }

        public Relation Seams(OrnamentProgram program, Relation bindings, (int X, int Y) column, (int X, int Y) row)
        {
            var motif = (Relation)N.E(program.Motifs).Single();
            var choices = N.E(bindings).Cast<Relation>().ToArray();
            var transformScope = N.E(program.Transforms);
            var selected = choices.Select(choice => choice.Right).Where(value => transformScope.Any(actual => N.Same(actual, value)))
                .Distinct(N.Participants.Instance).Cast<Relation>().ToArray();
            var maps = selected.Select(surface =>
            {
                var data = (Relation)surface.Right;
                var transform = new QualitativePatternTransform(frame, frame, (Relation)data.Left, data.Right);
                var endpoints = published.Select(endpoint =>
                {
                    var read = transform.Apply(endpoint.Position);
                    if (!transform.TryReadComplete(read, out var mapped)) throw new InvalidOperationException("A declared source endpoint transform did not read.");
                    return (endpoint.Role, Word: mapped, At: Index(mapped));
                }).ToArray();
                return (Surface: surface, Endpoints: endpoints);
            }).ToArray();
            var seamRules = new List<Relation>();
            var offsets = new[] { (C: 0, R: 0), (C: 1, R: 0), (C: 0, R: 1), (C: 1, R: 1), (C: -1, R: 1) };
            foreach (var offset in offsets)
            {
                var displacement = (X: offset.C * column.X + offset.R * row.X, Y: offset.C * column.Y + offset.R * row.Y);
                var word = Word(displacement);
                foreach (var from in maps) foreach (var to in maps)
                    for (var first = 0; first < from.Endpoints.Length; first++)
                        for (var second = 0; second < to.Endpoints.Length; second++)
                        {
                            if (offset == (0, 0) && ReferenceEquals(from.Surface, to.Surface) && first >= second) continue;
                            var a = from.Endpoints[first]; var b = to.Endpoints[second];
                            // This compiled authoring index only chooses which
                            // native equality question to ask. Execution neither
                            // retains these pairs of integers nor trusts them.
                            if (a.At.X != b.At.X + displacement.X || a.At.Y != b.At.Y + displacement.Y) continue;
                            var translated = frame.Reduce(b.Word, word);
                            if (!frame.TryReadResult(translated, out var moved, out _, out _)) continue;
                            var proof = frame.Reduce(moved, UnitArrangementRead.Inverse(frame, a.Word));
                            if (!UnitArrangementRead.IsZero(frame, proof)) continue;
                            seamRules.Add(OrnamentProgramField.Rule(motif, from.Surface, motif, to.Surface,
                                OrnamentProgramField.Offset(program, offset.C, offset.R), a.Role, b.Role));
                        }
            }
            return OrnamentProgramField.Contract(program, seamRules.ToArray());
        }

        private QualitativePatternTransform Map(IElement right, IElement left, IElement up, IElement down) => new(frame, frame,
            N.S(Row(frame.Right, right), Row(frame.Left, left), Row(frame.Up, up), Row(frame.Down, down)));
        private Relation Row(IElement source, IElement target) => new(QualitativeMotionFrame.Move(source, reach),
            N.S(QualitativeMotionFrame.Move(target, reach)));
        private (int X, int Y) Index(Relation word)
        {
            var x = 0; var y = 0;
            foreach (var move in N.E(word).Cast<Relation>())
            {
                if (!frame.Direction(move, out var axis, out var forward, out var actualReach) || !N.Same(actualReach, reach))
                    throw new InvalidOperationException("The source authoring index only reads its one declared shared reach.");
                if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1; else y += forward ? 1 : -1;
            }
            return (x, y);
        }
    }
}
