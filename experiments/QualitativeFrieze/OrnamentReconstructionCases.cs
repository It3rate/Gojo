using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Freshly executed small ornament panels. The authoring compiler
/// uses integer repetition of an uncalibrated reach; the independent public
/// observation contains only sites, headings, incidence and declared crossings.
/// Names and repertoire references are presentation, never recognition truth.</summary>
public static class OrnamentReconstructionCases
{
    public sealed class Spec
    {
        public string Id { get; }
        public string Family { get; }
        public string Name { get; }
        public string SourceReference { get; }
        public int Transform { get; }
        public int ReachRecipe { get; }
        public int Repeats { get; }
        private readonly Func<Definition> definition;

        private Spec(string id, string family, string name, string reference,
            int transform, int reachRecipe, int repeats, Func<Definition> definition)
        {
            Id = id; Family = family; Name = name; SourceReference = reference;
            Transform = transform; ReachRecipe = reachRecipe; Repeats = repeats;
            this.definition = definition;
        }

        public Sample Create() => Execute(this, definition());

        internal static Spec Make(string id, string family, string name, string reference,
            int transform, int reachRecipe, int repeats, Func<Definition> definition) =>
            new(id, family, name, reference, transform, reachRecipe, repeats, definition);
    }

    public sealed class Sample
    {
        public Spec Spec { get; }
        public QualitativeMotionFrame Frame => Program.Frame;
        public UnitArrangementProgram Program { get; }
        public UnitArrangementNode Execution { get; }
        public QualitativeGraphJoin Join { get; }
        public QualitativeStrokeGraph Graph => Join.Graph;
        public Relation Bindings => Join.Bindings;
        public QualitativeRecognitionPublisher Publisher { get; }
        public QualitativeRecognitionObservation Observation => Publisher.Observation;
        /// <summary>Closed(fresh public group identity ->
        /// (Closed(actual observed segments, in emitted chord order) ->
        /// Closed(actual observed first-site -> last-site closure))). This is a separate explicit
        /// mechanical service, never part of the history-blind observation.</summary>
        public Relation MechanicalGroups { get; }
        public Relation MechanicalEvidence { get; }
        /// <summary>Executable definition plus actual public-role join pairs
        /// and actual path/chord crossing selectors. No authoring factory is
        /// needed to execute this connected recipe again.</summary>
        public Relation Recipe { get; }
        public Relation ReplayEvidence { get; }
        public Relation Surface { get; }

        public Sample Replay() => OrnamentReconstructionCases.Replay(this);

        internal Sample(Spec spec, UnitArrangementProgram program, UnitArrangementNode execution,
            QualitativeGraphJoin join, QualitativeRecognitionPublisher publisher,
            Relation groups, Relation evidence, Relation recipe, Relation replayEvidence)
        {
            Spec = spec; Program = program; Execution = execution; Join = join;
            Publisher = publisher; MechanicalGroups = groups; MechanicalEvidence = evidence;
            Recipe = recipe; ReplayEvidence = replayEvidence;
            Surface = new(new Relation(program.Surface, execution.Current.Surface),
                new Relation(join.Surface, new Relation(publisher.Surface, new Relation(groups,
                    new Relation(evidence, new Relation(recipe, replayEvidence))))));
        }
    }

    internal sealed record PathSpec((int X, int Y)[] Points, bool Closed = false);
    internal sealed record Definition(PathSpec[] Paths, bool JoinCoincident = true,
        (int First, int Second)[]? Crossings = null);
    private sealed record Family(string Id, string Name, string Reference, Func<Definition> Build);

    public static IReadOnlyList<Spec> Specs { get; } = BuildSpecs();

    /// <summary>Executes an independently imported native recipe. Its exact
    /// program, public-role join pairs and path/chord crossing selectors are
    /// read from the relation surface; neither a Sample nor an authoring
    /// factory is required. Witness = (source -> Closed(admitted output)) ->
    /// (Closed(stage proofs) -> (Closed(open causes) -> Closed(blocked causes))).
    /// Missing closed-scope detail stays Open; illegal selectors stay Blocked.
    /// Execution, if reached, happens only in a new local drawing world.</summary>
    public static bool TryGenerate(Relation nativeRecipe, out Sample? sample, out Relation witness)
    {
        var proofs = new List<IElement>(); var open = new List<IElement>(); var blocked = new List<IElement>();
        sample = null;
        if (nativeRecipe is not { Left: Relation { Left: Relation sourceFrame } })
        {
            blocked.Add(nativeRecipe);
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        }
        if (!QualitativeMotionFrameRead.TryReadStandard(sourceFrame, out var frameRead, out var frameWitness))
        {
            proofs.Add(frameWitness); blocked.Add(sourceFrame);
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        }
        proofs.Add(frameWitness);
        if (!frameRead.TryRebind(nativeRecipe, out var renamed, out var rebinding))
        {
            proofs.Add(rebinding); blocked.Add(nativeRecipe);
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        }
        proofs.Add(rebinding);
        if (renamed is not Relation { Left: Relation { Left: var actualFrame,
                Right: Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                    Right: Relation { Left: Relation closed, Right: var evidence } } } actualProgram,
                Right: Relation { Left: Relation connections, Right: Relation crossings } } recipe ||
            !ReferenceEquals(actualFrame, frameRead.Target.Surface))
        {
            blocked.Add(renamed);
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        }
        foreach (var scope in new[] { paths, ports, closed, connections, crossings }) ReadScope(scope);
        if (open.Count != 0 || blocked.Count != 0)
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        foreach (var item in E(paths))
        {
            if (item is not Relation { Left: Relation start, Right: Relation chords }) { blocked.Add(item); continue; }
            ReadScope(start); ReadScope(chords);
            if (open.Count != 0 || blocked.Count != 0) continue;
            foreach (var chord in E(chords))
                if (chord is Relation word) ReadScope(word); else blocked.Add(chord);
        }
        if (open.Count != 0 || blocked.Count != 0)
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        var program = new UnitArrangementProgram(frameRead.Target, "Imported native ornament recipe", paths, ports, closed, evidence, actualProgram);
        if (!program.Complete())
        {
            blocked.Add(actualProgram);
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        }
        var roles = E(ports).Cast<Relation>().Select(port => port.Left).ToArray();
        foreach (var row in E(connections))
            if (row is not Relation pair || !roles.Any(role => ReferenceEquals(role, pair.Left)) ||
                !roles.Any(role => ReferenceEquals(role, pair.Right))) blocked.Add(row);
        foreach (var row in E(crossings))
            if (row is not Relation { Left: Relation first, Right: Relation second } || !Selector(first) || !Selector(second)) blocked.Add(row);
        if (blocked.Count != 0)
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        try
        {
            var node = new UnitArrangementNode(program.Name, [program], program, S());
            proofs.Add(node.Current.Surface);
            var publications = E(node.Current.Ports).Cast<Relation>().ToArray();
            var declarations = E(connections).Cast<Relation>().Select(row =>
            {
                var from = publications.Single(port => ReferenceEquals(port.Left, row.Left));
                var to = publications.Single(port => ReferenceEquals(port.Left, row.Right));
                return (IElement)new Relation(new Relation(PublishedSite(node, from), PublishedSite(node, to)),
                    new Relation(row, new Relation(from, to)));
            }).ToArray();
            var join = new QualitativeGraphJoin(node.Current.Join.Graph, node.Current.Join.Bindings, S(declarations));
            proofs.Add(join.Surface);
            if (!join.TryReadPassed(join.Evaluate()))
            {
                open.AddRange(E(join.Unresolved)); blocked.AddRange(E(join.Blocked));
                if (open.Count == 0 && blocked.Count == 0) blocked.Add(join.Surface);
                return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
            }
            // This presentation-only specification deliberately cannot author
            // another source. Successful generation is entirely native.
            var metadata = Spec.Make("IMPORTED", "IMPORTED", program.Name, "Standalone native recipe", 0, 0, 1,
                () => throw new InvalidOperationException("An imported recipe executes through Replay or TryGenerate; it has no authoring factory."));
            var importEvidence = new Relation(nativeRecipe, new Relation(frameWitness, new Relation(rebinding,
                new Relation(node.Current.Surface, join.Surface))));
            sample = Complete(metadata, program, node, join, recipe, importEvidence);
            witness = GenerationWitness(nativeRecipe, sample, proofs, open, blocked); return true;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            // Refusing a well-shaped but unexecutable contract is an explicit
            // structural non-read. Existing source worlds remain unchanged.
            blocked.Add(recipe);
            return NotGenerated(nativeRecipe, proofs, open, blocked, out sample, out witness);
        }

        void ReadScope(Relation scope)
        {
            if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)) blocked.Add(scope);
            else if (!seam.Equals(tail)) open.Add(StructuralConstraint.Open(scope));
        }
        bool Selector(Relation selector) => E(paths).OfType<Relation>().Any(path =>
            ReferenceEquals(path, selector.Left) && E((Relation)path.Right).Any(chord => ReferenceEquals(chord, selector.Right)));
    }

    private static bool NotGenerated(Relation source, List<IElement> proofs, List<IElement> open,
        List<IElement> blocked, out Sample? sample, out Relation witness)
    {
        sample = null; witness = GenerationWitness(source, null, proofs, open, blocked); return false;
    }

    private static Relation GenerationWitness(Relation source, Sample? sample, IReadOnlyList<IElement> proofs,
        IReadOnlyList<IElement> open, IReadOnlyList<IElement> blocked) => new(new Relation(source,
            sample is null ? S() : S(sample.Surface)), new Relation(S(proofs.ToArray()), new Relation(S(open.ToArray()), S(blocked.ToArray()))));

    private static IReadOnlyList<Spec> BuildSpecs()
    {
        PathSpec P(params (int X, int Y)[] points) => new(points);
        PathSpec Ring(params (int X, int Y)[] points) => new([.. points, points[0]], true);
        var families = new Family[]
        {
            new("SQ-PATH", "Square contour as one turning path", "Constructed square / Bourgoin B18 mesh unit",
                () => new([Ring((0, 0), (2, 0), (2, 2), (0, 2))])),
            new("SQ-SIDES", "Square contour from four joined sides", "Constructed mechanical alternative to SQ-PATH",
                () => new([P((0, 0), (2, 0)), P((2, 0), (2, 2)), P((2, 2), (0, 2)), P((0, 2), (0, 0))])),
            new("SQ-OPEN", "Four unjoined sides in a square disposition", "Constructed visible lookalike; independent endpoints",
                () => new([P((0, 0), (2, 0)), P((2, 0), (2, 2)), P((2, 2), (0, 2)), P((0, 2), (0, 0))], false)),
            new("DIAMOND", "Diamond contour", "Bourgoin B03 / B18 diagonal compartment vocabulary",
                () => new([Ring((0, 1), (1, 2), (2, 1), (1, 0))])),
            new("TRIANGLES", "Two triangular cells with a shared side", "Bourgoin B16 three-family mesh fragment",
                () => new([Ring((0, 0), (2, 0), (1, 2)), P((2, 0), (3, 2), (1, 2))])),
            new("X-JOIN", "Four arms with one actual shared site", "Constructed junction contrast",
                () => new([P((0, 0), (-2, 0)), P((0, 0), (2, 0)), P((0, 0), (0, 2)), P((0, 0), (0, -2))])),
            new("X-CROSS", "Two independent lines with a declared crossing", "Constructed weave / overpass evidence",
                () => new([P((-2, 0), (2, 0)), P((0, -2), (0, 2))], false, [(0, 1)])),
            new("X-UNRELATED", "Two independent lines without a crossing claim", "Constructed unspecified encounter",
                () => new([P((-2, 0), (2, 0)), P((0, -2), (0, 2))], false)),
            new("T", "Stem meeting an axial cap", "Primitive T junction in ornamental scaffolds",
                () => new([P((0, 0), (-2, 0)), P((0, 0), (2, 0)), P((0, 0), (0, -2))])),
            new("Y", "Stem meeting a diagonal fork", "Primitive Y junction in triangular scaffolds",
                () => new([P((0, 0), (-2, 2)), P((0, 0), (2, 2)), P((0, 0), (0, -2))])),
            new("H", "Bridge joining two opposed stems", "Primitive double-T bridge / clamped path",
                () => new([P((0, -1), (0, 0), (0, 1)), P((2, -1), (2, 0), (2, 1)), P((0, 0), (2, 0))])),
            new("ZIGZAG", "Alternating diagonal advance", "Bourgoin B27 angled-band vocabulary",
                () => new([P((0, 0), (1, 2), (2, 0), (3, 2), (4, 0))])),
            new("WAVE", "Separated-axis square wave", "Greek-key and square-wave border repertoire",
                () => new([P((0, 0), (1, 0), (1, 2), (2, 2), (2, 0), (3, 0), (3, 2), (4, 2))])),
            new("STAIR", "Ascending staircase", "Constructed ordered-step scaffold",
                () => new([P((0, 0), (1, 0), (1, 1), (2, 1), (2, 2), (3, 2), (3, 3))])),
            new("MEANDER", "Detached returning S-shaped fret", "Bourgoin B09 detached S-shaped fret units",
                () => new([P((0, 0), (0, 2), (3, 2), (3, 1), (1, 1), (1, 0), (4, 0))])),
            new("BRICK", "Running-bond mortar fragment", "Bourgoin B01 running brick bond",
                () => new([P((0, 0), (2, 0), (4, 0)), P((0, 2), (2, 2), (4, 2)),
                    P((0, 4), (2, 4), (4, 4)), P((0, 0), (0, 2)), P((2, 2), (2, 4)), P((4, 0), (4, 2))])),
            new("GRID", "Four square cells from line families", "Bourgoin B18 rectangular mesh before diagonal ties",
                () => new([P((0, 0), (1, 0), (2, 0)), P((0, 1), (1, 1), (2, 1)),
                    P((0, 2), (1, 2), (2, 2)), P((0, 0), (0, 1), (0, 2)),
                    P((1, 0), (1, 1), (1, 2)), P((2, 0), (2, 1), (2, 2))])),
            new("HONEYCOMB", "Two hexagonal cells sharing one edge", "Bourgoin B04 honeycomb incidence fragment",
                () => new([Ring((0, 0), (2, 0), (3, 1), (2, 2), (0, 2), (-1, 1)),
                    P((3, 1), (5, 1), (6, 2), (5, 3), (3, 3), (2, 2))])),
            new("STAR", "Six opposed rays at one shared site", "Bourgoin B25 six-point junction vocabulary",
                () => new([P((0, 0), (2, 0)), P((0, 0), (1, 2)), P((0, 0), (-1, 2)),
                    P((0, 0), (-2, 0)), P((0, 0), (-1, -2)), P((0, 0), (1, -2))])),
            new("LOZENGE", "Opposed lozenges with a common tip", "Bourgoin B20 / B21 lozenge vocabulary",
                () => new([Ring((0, 0), (1, 2), (3, 2), (2, 0)), Ring((3, 2), (4, 4), (6, 4), (5, 2))])),
            new("BASKET", "Unjoined horizontal and vertical ribbon pairs", "Bourgoin B02 basket-rectangle vocabulary",
                () => new([P((0, 1), (4, 1)), P((0, 2), (4, 2)), P((1, 0), (1, 3)), P((3, 0), (3, 3))],
                    false, [(0, 2), (0, 3), (1, 2), (1, 3)])),
            new("MEANDER-PAIR", "Returning meanders joined across a reflected course", "Constructed joined elaboration of Bourgoin B09 frets",
                () => new([P((0, 0), (0, 2), (3, 2), (3, 1), (1, 1), (1, 0), (4, 0)),
                    P((4, 0), (4, -2), (7, -2), (7, -1), (5, -1), (5, 0), (8, 0))])),
            new("DIAMOND-NODES", "Four diamond cells with flags at shared landmarks", "Constructed lattice / subordinate motif composition",
                () => new([P((0, 0), (2, -2), (4, -4)), P((2, 2), (4, 0), (6, -2)),
                    P((4, 4), (6, 2), (8, 0)), P((0, 0), (2, 2), (4, 4)),
                    P((2, -2), (4, 0), (6, 2)), P((4, -4), (6, -2), (8, 0)),
                    P((0, 0), (1, 0), (1, 1)), P((4, -4), (5, -4), (5, -3)),
                    P((4, 4), (5, 4), (5, 5)), P((8, 0), (9, 0), (9, 1))])),
            new("ALTERNATING-CELLS", "Alternating axial and diagonal closed compartments", "Bourgoin B03 square / diamond opposition, connected construction",
                () => new([Ring((0, 0), (2, 0), (2, 1), (2, 2), (0, 2), (0, 1)),
                    Ring((2, 1), (3, 0), (4, 1), (3, 2)),
                    Ring((4, 0), (6, 0), (6, 1), (6, 2), (4, 2), (4, 1)),
                    Ring((6, 1), (7, 0), (8, 1), (7, 2))])),
            new("MERGED-CELLS", "L and T compartments from selected grid seams", "Constructed selective-seam grid; absent seams are omitted participation",
                () =>
                {
                    var paths = new List<PathSpec>();
                    for (var y = 0; y <= 3; y++) for (var x = 0; x < 3; x++)
                        if ((x, y) is not (0, 2) and not (1, 1)) paths.Add(P((x, y), (x + 1, y)));
                    for (var x = 0; x <= 3; x++) for (var y = 0; y < 3; y++)
                        if ((x, y) is not (1, 2) and not (1, 0) and not (2, 0)) paths.Add(P((x, y), (x, y + 1)));
                    return new(paths.ToArray());
                }),
            new("LOZENGE-ROSETTE", "Four lozenge lobes tied by an outer turning contour", "Constructed fourfold lozenge / radial scaffold composition",
                () => new([Ring((0, 0), (2, 1), (2, 3), (0, 2)), Ring((0, 0), (-1, 2), (-3, 2), (-2, 0)),
                    Ring((0, 0), (-2, -1), (-2, -3), (0, -2)), Ring((0, 0), (1, -2), (3, -2), (2, 0)),
                    Ring((2, 3), (-3, 2), (-2, -3), (3, -2))]))
        };
        var specs = new List<Spec>();
        foreach (var family in families)
            for (var transform = 0; transform < 4; transform++)
                for (var reach = 0; reach < 2; reach++)
                    for (var repeats = 1; repeats <= 2; repeats++)
                        specs.Add(Spec.Make($"{family.Id}-Q{transform}-R{reach}-N{repeats}", family.Id,
                            family.Name, family.Reference, transform, reach, repeats, family.Build));
        return specs.AsReadOnly();
    }

    private static Sample Execute(Spec spec, Definition definition)
    {
        var author = new LatticeLadderAuthor();
        var declared = new List<(Relation Path, (int X, int Y)[] Points)>();
        var originalPaths = new List<Relation>(); var closed = new List<IElement>();
        var width = definition.Paths.SelectMany(path => path.Points).Max(point => point.X) -
            definition.Paths.SelectMany(path => path.Points).Min(point => point.X);
        // The second panel is a translated independent occurrence. Its actual
        // displacement remains a native word; no cross-panel contact is implied.
        for (var repeat = 0; repeat < spec.Repeats; repeat++)
            foreach (var path in definition.Paths)
            {
                var points = path.Points.Select(point => (X: point.X + repeat * (width + 4), point.Y)).ToArray();
                Relation Word((int X, int Y) point) => author.Word(point.X * (spec.ReachRecipe + 1), point.Y);
                var output = LatticeLadderAuthor.Path(Word(points[0]), Enumerable.Range(0, points.Length - 1)
                    .Select(index => Word((points[index + 1].X - points[index].X, points[index + 1].Y - points[index].Y))).ToArray());
                originalPaths.Add(output); declared.Add((output, points)); if (path.Closed) closed.Add(output);
            }
        var ports = LatticeLadderAuthor.Ports(originalPaths.ToArray());
        var seed = new UnitArrangementProgram(author.Frame, spec.Name, S(originalPaths.Cast<IElement>().ToArray()), ports, S(closed.ToArray()));
        var right = spec.Transform switch { 0 => author.Word(1, 0), 1 => author.Word(0, 1), 2 => author.Word(-1, 0), _ => author.Word(0, -1) };
        var up = spec.Transform switch { 0 => author.Word(0, 1), 1 => author.Word(-1, 0), 2 => author.Word(0, -1), _ => author.Word(1, 0) };
        var program = seed.Transform(spec.Name, author.Map(right, up));
        var paths = E(program.Paths).Cast<Relation>().ToArray();
        var node = new UnitArrangementNode(spec.Name, [program], program, S());
        var joins = new List<IElement>();
        if (definition.JoinCoincident)
        {
            // Authoring-only equality selects which explicit public roles the
            // fixture means to connect. Admission is still checked by the real
            // native join from their actual published site/receipt evidence.
            var landmarks = declared.SelectMany((path, pathIndex) => path.Points.Select((point, index) =>
                (Point: point, Path: paths[pathIndex], Index: index))).ToArray();
            foreach (var group in landmarks.GroupBy(item => item.Point))
            {
                var members = group.ToArray();
                for (var index = 1; index < members.Length; index++)
                {
                    var a = Publication(node, program, members[0].Path, members[0].Index);
                    var b = Publication(node, program, members[index].Path, members[index].Index);
                    if (!ReferenceEquals(a.Site, b.Site)) joins.Add(new Relation(new Relation(a.Site, b.Site), new Relation(a.Port, b.Port)));
                }
            }
        }
        var join = new QualitativeGraphJoin(node.Current.Join.Graph, node.Current.Join.Bindings, S(joins.ToArray()));
        if (!join.TryReadPassed(join.Evaluate())) throw new InvalidOperationException("A fixture's explicit public-role joins must be admitted.");
        var connectionContract = S(joins.Cast<Relation>().Select(declaration =>
        {
            var support = (Relation)declaration.Right;
            return (IElement)new Relation(((Relation)support.Left).Left, ((Relation)support.Right).Left);
        }).ToArray());
        var crossingContract = new List<IElement>();
        for (var repeat = 0; repeat < spec.Repeats; repeat++)
            foreach (var crossing in definition.Crossings ?? [])
            {
                var first = paths[repeat * definition.Paths.Length + crossing.First];
                var second = paths[repeat * definition.Paths.Length + crossing.Second];
                crossingContract.Add(new Relation(new Relation(first, E((Relation)first.Right)[0]),
                    new Relation(second, E((Relation)second.Right)[0])));
            }
        var recipe = new Relation(program.Surface, new Relation(connectionContract, S(crossingContract.ToArray())));
        return Complete(spec, program, node, join, recipe, S());
    }

    private static Sample Replay(Sample source)
    {
        var program = source.Program;
        if (source.Recipe is not { Left: var declaredProgram, Right: Relation { Left: Relation connections, Right: Relation crossings } } ||
            !ReferenceEquals(declaredProgram, program.Surface)) throw new InvalidOperationException("Replay requires the actual connected executable recipe.");
        var node = new UnitArrangementNode(program.Name, [program], program, S());
        var ports = E(node.Current.Ports).Cast<Relation>().ToArray();
        var declarations = new List<IElement>(); var correspondences = new List<IElement>();
        foreach (var row in E(connections).Cast<Relation>())
        {
            var from = ports.Single(port => ReferenceEquals(port.Left, row.Left));
            var to = ports.Single(port => ReferenceEquals(port.Left, row.Right));
            var a = PublishedSite(node, from); var b = PublishedSite(node, to);
            declarations.Add(new Relation(new Relation(a, b), new Relation(row, new Relation(from, to))));
        }
        foreach (var prior in E(source.Execution.Current.Ports).Cast<Relation>())
            correspondences.Add(new Relation(prior, ports.Single(port => ReferenceEquals(port.Left, prior.Left))));
        var join = new QualitativeGraphJoin(node.Current.Join.Graph, node.Current.Join.Bindings, S(declarations.ToArray()));
        if (!join.TryReadPassed(join.Evaluate())) throw new InvalidOperationException("Replayed actual public-role joins must be admitted again.");
        var evidence = new Relation(source.Recipe, new Relation(source.Execution.Current.Surface,
            new Relation(S(correspondences.ToArray()), new Relation(node.Current.Surface, join.Surface))));
        return Complete(source.Spec, program, node, join, source.Recipe, evidence);
    }

    private static Sample Complete(Spec spec, UnitArrangementProgram program, UnitArrangementNode node,
        QualitativeGraphJoin join, Relation recipe, Relation replayEvidence)
    {
        var paths = E(program.Paths).Cast<Relation>().ToArray();
        var rawByPath = new List<Relation[]>(); var proofByPath = new List<Relation[]>();
        var actionRows = E(node.Current.Receipts).Cast<Relation>().ToArray();
        var executionPorts = E(node.Current.Ports).Cast<Relation>().ToArray();
        foreach (var path in paths)
        {
            var actual = new List<Relation>(); var proofs = new List<Relation>();
            foreach (var word in E((Relation)path.Right))
            {
                var port = E(program.Ports).Cast<Relation>().Single(row => row.Right is Relation pick && ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, word));
                var publication = executionPorts.Single(row => ReferenceEquals(row.Left, port.Left));
                var receipt = (Relation)((Relation)((Relation)((Relation)publication.Right).Right).Right).Right;
                var accepted = actionRows.Single(row => ReferenceEquals(row.Right, receipt));
                if (!node.Drawing.TryReadAccepted(receipt, out _, out var emitted) || E(emitted) is not [Relation stroke])
                    throw new InvalidOperationException("One actual executed chord must have one acknowledged stroke.");
                var local = (Relation)E(node.Current.Join.StrokeMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, stroke)).Right;
                var global = (Relation)E(join.StrokeMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, local)).Right;
                actual.Add(global); proofs.Add(new Relation(port, new Relation(publication, new Relation(accepted, new Relation(stroke, global)))));
            }
            rawByPath.Add(actual.ToArray()); proofByPath.Add(proofs.ToArray());
        }
        var crossings = new List<IElement>();
        foreach (var crossing in E((Relation)((Relation)recipe.Right).Right).Cast<Relation>())
            crossings.Add(new Relation(SelectedStroke((Relation)crossing.Left), SelectedStroke((Relation)crossing.Right)));
        var edges = S(E(join.Graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
            return (IElement)new Relation(stroke, new Relation(from, to));
        }).ToArray());
        var publisher = new QualitativeRecognitionPublisher(program.Frame,
            new Relation(join.Graph.Sites, new Relation(edges, S())), join.Bindings, S(crossings.ToArray()));
        if (!publisher.IsComplete) throw new InvalidOperationException("A fixture needs a complete native publication.");
        var groups = new List<IElement>(); var evidence = new List<IElement>();
        for (var index = 0; index < paths.Length; index++)
        {
            var observed = rawByPath[index].Select(stroke =>
            {
                if (!publisher.TryMapSegment(stroke, out var segment)) throw new InvalidOperationException("An emitted stroke must map to the actual published segment.");
                return (IElement)segment;
            }).ToArray();
            var first = (Relation)observed[0]; var last = (Relation)observed[^1];
            publisher.Observation.TryReadSegment(first, out _, out var firstSite, out _, out _);
            publisher.Observation.TryReadSegment(last, out _, out _, out var lastSite, out _);
            var closure = E(program.ClosedPaths).Any(path => ReferenceEquals(path, paths[index]))
                ? S(new Relation(firstSite, lastSite)) : S();
            var group = new Relation(new Placeholder(), new Relation(S(observed), closure)); groups.Add(group);
            evidence.Add(new Relation(group, new Relation(paths[index], S(proofByPath[index]))));
        }
        return new(spec, program, node, join, publisher, S(groups.ToArray()), S(evidence.ToArray()), recipe, replayEvidence);

        Relation SelectedStroke(Relation selector)
        {
            var pathIndex = Array.FindIndex(paths, path => ReferenceEquals(path, selector.Left));
            if (pathIndex < 0) throw new InvalidOperationException("A native crossing selector must refer to an actual executable path.");
            var chord = Array.FindIndex(E((Relation)paths[pathIndex].Right), word => ReferenceEquals(word, selector.Right));
            if (chord < 0) throw new InvalidOperationException("A native crossing selector must refer to an actual executable chord.");
            return rawByPath[pathIndex][chord];
        }
    }

    private static (IElement Site, Relation Port) Publication(UnitArrangementNode node, UnitArrangementProgram program, Relation path, int index)
    {
        var words = E((Relation)path.Right); var selector = index == words.Length ? path : words[index];
        var role = E(program.Ports).Cast<Relation>().Single(row => row.Right is Relation pick &&
            ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selector)).Left;
        var publication = E(node.Current.Ports).Cast<Relation>().Single(row => ReferenceEquals(row.Left, role));
        return (PublishedSite(node, publication), publication);
    }

    private static IElement PublishedSite(UnitArrangementNode node, Relation publication)
    {
        var cursor = (Relation)((Relation)publication.Right).Right;
        var raw = ((Relation)cursor.Left).Left;
        return E(node.Current.Join.SiteMap).Cast<Relation>().Single(row => ReferenceEquals(row.Left, raw)).Right;
    }

    private static Relation S(params IElement[] elements) => StructuralConstraint.Closed(elements);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
