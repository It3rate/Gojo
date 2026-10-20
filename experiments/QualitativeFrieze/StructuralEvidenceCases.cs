using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Small independently executed sources and finite receiver libraries.
/// A name is a caption only. Every question is an actual induced pattern;
/// its answer comes from a fresh history-blind observation, not a truth tag.
/// Program/path ancestry stays outside the receiver's advertised evidence.</summary>
public sealed class StructuralEvidenceCases
{
    // sample = actual acknowledged execution -> (admitted join view ->
    //          (history-blind publication -> bounded native motif read))
    // case = private sampled source -> (public candidate library -> queries)
    // Only Candidate.Observation and Query.Pattern cross to the receiver.
    public sealed class Sample
    {
        public string Name { get; }
        public QualitativeMotionFrame Frame => Graph.Frame;
        public UnitArrangementProgram Program { get; }
        public UnitArrangementNode Execution { get; }
        public QualitativeGraphJoin Join { get; }
        public QualitativeStrokeGraph Graph => Join.Graph;
        public Relation Bindings => Join.Bindings;
        public QualitativeRecognitionPublisher Publisher { get; }
        public QualitativeRecognitionObservation Observation => Publisher.Observation;
        public QualitativeMotifInduction Induction { get; }
        public Relation Surface { get; }

        internal Sample(string name, UnitArrangementProgram program, UnitArrangementNode execution,
            QualitativeGraphJoin join, Relation? crossings = null)
        {
            Name = name; Program = program; Execution = execution; Join = join;
            var edges = S(E(Graph.Strokes).Cast<Relation>().Select(stroke =>
            {
                QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _);
                return (IElement)new Relation(stroke, new Relation(a, b));
            }).ToArray());
            var topology = new Relation(Graph.Sites, new Relation(edges, S()));
            Publisher = new(Frame, topology, Bindings, crossings);
            Induction = new(Observation, maxCycleEdges: 4, proposalCapacity: 4096,
                maxWalkEdges: 3, retainedCapacity: 1024);
            if (!Publisher.IsComplete || !Induction.IsComplete)
                throw new InvalidOperationException("A structural example requires a complete independently published bounded read.");
            Surface = new(new Relation(program.Surface, execution.Current.Surface),
                new Relation(join.Surface, new Relation(Publisher.Surface, Induction.Surface)));
        }
    }

    public sealed class Case
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string CoarseHint { get; }
        public string QuestionPurpose { get; }
        public Sample Source { get; }
        public IReadOnlyList<Sample> Candidates { get; }
        public IReadOnlyList<StructuralEvidenceInquiry.Query> Queries { get; }
        public bool ExpectedAmbiguity { get; }
        public Relation Surface { get; }

        internal Case(string id, string name, string description, string coarseHint,
            string questionPurpose, Sample source, IReadOnlyList<Sample> candidates,
            IReadOnlyList<StructuralEvidenceInquiry.Query> queries, bool expectedAmbiguity = false)
        {
            Id = id; Name = name; Description = description; CoarseHint = coarseHint;
            QuestionPurpose = questionPurpose; Source = source; Candidates = candidates;
            Queries = queries; ExpectedAmbiguity = expectedAmbiguity;
            Surface = new(source.Surface, new Relation(S(candidates.Select(sample => (IElement)sample.Surface).ToArray()),
                S(queries.Select(query => (IElement)query.Surface).ToArray())));
        }

        public StructuralEvidenceInquiry.Receiver CreateReceiver() => new(
            Candidates.Select(sample => new StructuralEvidenceInquiry.Candidate(sample.Name, sample.Observation,
                critic: sample.Induction)).ToArray(), Queries);
        public StructuralEvidenceInquiry.Supplier CreateSupplier(int proposalCapacity = 4096) =>
            new(Source.Observation, Queries, proposalCapacity);
    }

    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    private StructuralEvidenceCases(IReadOnlyList<Case> cases)
    { Cases = cases; Surface = S(cases.Select(item => (IElement)item.Surface).ToArray()); }

    public static StructuralEvidenceCases Build()
    {
        var cases = new List<Case>();

        var chevron = Triangle(false); var triangle = Triangle(true);
        cases.Add(new("S01", "A triangle needs closure", "Two rising and falling strokes do not establish a closed triangular contour. A source can expose one actual three-edge cycle without specifying angles or lengths.",
            "A pointed outline; the base has not been reported.", "Ask whether a three-edge cycle actually exists.",
            Triangle(true), [chevron, OpenTurn(), Square(false), triangle], [Query("Is there a three-edge cycle?", triangle, "cycle", 3)]));

        var joined = Cross(true); var crossing = Cross(false); var crossTee = Branch(false);
        cases.Add(new("S02", "Crossing does not establish a junction", "The unjoined source has two continuous strokes with a declared crossing. The alternative has four arms sharing one actual site. Equal screen positions never create a shared site.",
            "Horizontal and vertical participation meet in the drawing.", "Ask for actual four-arm incidence; a negative answer rejects the joined interpretation.",
            Cross(false), [joined, crossTee, crossing], [Query("Do four arms share one site?", joined, "junction", 4),
                Query("Is there a three-arm axial T?", crossTee, "junction", 3)]));

        var tee = Branch(false); var fork = Branch(true);
        cases.Add(new("S03", "A T and a Y offer different routes", "Both candidates have a three-arm shared site. Their actual outgoing heading participation distinguishes an axial cap from a diagonal fork without assigning calibrated angles.",
            "One stem meets two arms at a common site.", "Ask which three-arm heading pattern is present.",
            Branch(true), [tee, Cross(true), fork], [Query("Is the junction an axial T?", tee, "junction", 3),
                Query("Is the junction a diagonal Y?", fork, "junction", 3)]));

        var fragments = Zigzag(false); var chain = Zigzag(true);
        cases.Add(new("S04", "Alternating directions need not form a path", "Separate strokes can have the same placement and headings as a connected zigzag. A three-edge walk query tests the intermediate shared-site relations.",
            "The visible headings alternate up-right and down-right.", "Ask whether three consecutive diagonal edges are actually connected.",
            Zigzag(true), [fragments, SquareWave(), chain], [Query("Is there a connected three-edge diagonal walk?", chain, "walk", 3)]));

        var diagonal = Zigzag(true); var stepped = SquareWave();
        cases.Add(new("S05", "Simultaneous and separated motion differ", "A zigzag combines horizontal and vertical participation within each edge. A square wave separates them into alternating axial edges. The question distinguishes the native walk, rather than a rendered bend style.",
            "The path advances while alternately rising and falling.", "Ask whether an axial horizontal/vertical three-edge walk is present.",
            SquareWave(), [diagonal, Zigzag(false), stepped], [Query("Is there a three-edge axial stepped walk?", stepped, "walk", 3),
                Query("Is there a three-edge diagonal zigzag?", diagonal, "walk", 3)]));

        var pathSquare = Square(false); var sideSquare = Square(true);
        cases.Add(new("S06", "The same outline cannot reveal its recipe", "One sample is a single turning path; the other is four separate sides joined through actual public ports. Their exposed incidence and headings agree. Without a provenance service, the reader must retain both construction hypotheses.",
            "A closed four-sided axial outline.", "Confirm the contour, then retain the recipe ambiguity; the source does not advertise ancestry.",
            Square(true), [pathSquare, sideSquare], [Query("Is there a four-edge cycle?", pathSquare, "cycle", 4)], true));

        return new(cases);
    }

    private static StructuralEvidenceInquiry.Query Query(string name, Sample exemplar, string kind, int edges)
    {
        var family = exemplar.Induction.Families.First(item => item.Kind == kind && item.SegmentCount == edges);
        return new(name, new CommunicationPattern(exemplar.Observation.Frame, family.Definition));
    }

    private static Sample Triangle(bool closed)
    {
        var a = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(1, 2), a.Word(1, -2),
            a.Word(-2, 0));
        if (!closed) path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(1, 2), a.Word(1, -2));
        return Execute(closed ? "Closed triangle" : "Open chevron", a, [path], closed ? [path] : []);
    }

    private static Sample OpenTurn()
    {
        var a = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(2, 0), a.Word(0, 2), a.Word(-2, 0));
        return Execute("Open three-edge turn", a, [path], []);
    }

    private static Sample Cross(bool joined)
    {
        var a = new LatticeLadderAuthor();
        if (joined)
        {
            var paths = new[] { (2, 0), (-2, 0), (0, 2), (0, -2) }
                .Select(delta => LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(delta.Item1, delta.Item2))).ToArray();
            return Execute("One joined four-arm cross", a, paths, [], joinStarts: true);
        }
        var horizontal = LatticeLadderAuthor.Path(a.Word(-2, 0), a.Word(4, 0));
        var vertical = LatticeLadderAuthor.Path(a.Word(0, -2), a.Word(0, 4));
        return Execute("Two crossing continuous lines", a, [horizontal, vertical], [], declareCrossing: true);
    }

    private static Sample Branch(bool diagonal)
    {
        var a = new LatticeLadderAuthor();
        var directions = diagonal ? new[] { (0, -2), (-2, 2), (2, 2) } : [(0, -2), (-2, 0), (2, 0)];
        var paths = directions.Select(delta => LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(delta.Item1, delta.Item2))).ToArray();
        return Execute(diagonal ? "Diagonal Y junction" : "Axial T junction", a, paths, [], joinStarts: true);
    }

    private static Sample Zigzag(bool connected)
    {
        var a = new LatticeLadderAuthor();
        var chords = new[] { a.Word(1, 2), a.Word(1, -2), a.Word(1, 2), a.Word(1, -2) };
        var paths = connected ? new[] { LatticeLadderAuthor.Path(a.Word(0, 0), chords) } :
            chords.Select((chord, index) => LatticeLadderAuthor.Path(a.Word(index, index % 2 == 0 ? 0 : 2), chord)).ToArray();
        return Execute(connected ? "Connected zigzag" : "Four separate alternating strokes", a, paths, []);
    }

    private static Sample SquareWave()
    {
        var a = new LatticeLadderAuthor();
        var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(1, 0), a.Word(0, 2), a.Word(1, 0),
            a.Word(0, -2), a.Word(1, 0), a.Word(0, 2), a.Word(1, 0), a.Word(0, -2));
        return Execute("Separated-axis square wave", a, [path], []);
    }

    private static Sample Square(bool separateSides)
    {
        var a = new LatticeLadderAuthor();
        if (!separateSides)
        {
            var path = LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(2, 0), a.Word(0, 2), a.Word(-2, 0), a.Word(0, -2));
            return Execute("One closed turning path", a, [path], [path]);
        }
        var paths = new[] { LatticeLadderAuthor.Path(a.Word(0, 0), a.Word(2, 0)),
            LatticeLadderAuthor.Path(a.Word(2, 0), a.Word(0, 2)),
            LatticeLadderAuthor.Path(a.Word(2, 2), a.Word(-2, 0)),
            LatticeLadderAuthor.Path(a.Word(0, 2), a.Word(0, -2)) };
        return Execute("Four independently drawn joined sides", a, paths, [], joinRing: true);
    }

    private static Sample Execute(string name, LatticeLadderAuthor author, Relation[] paths,
        Relation[] closures, bool joinStarts = false, bool joinRing = false, bool declareCrossing = false)
    {
        var ports = LatticeLadderAuthor.Ports(paths);
        var program = new UnitArrangementProgram(author.Frame, name, S(paths), ports, S(closures));
        var node = new UnitArrangementNode(name, [program], program, S());
        var declarations = new List<IElement>();
        if (joinStarts)
        {
            var starts = paths.Select(path => PublishedSite(path, false)).ToArray();
            foreach (var other in starts.Skip(1)) declarations.Add(new Relation(new Relation(starts[0].Site, other.Site),
                new Relation(starts[0].Publication, other.Publication)));
        }
        if (joinRing)
            for (var index = 0; index < paths.Length; index++)
            {
                var end = PublishedSite(paths[index], true); var start = PublishedSite(paths[(index + 1) % paths.Length], false);
                declarations.Add(new Relation(new Relation(end.Site, start.Site), new Relation(end.Publication, start.Publication)));
            }
        var join = new QualitativeGraphJoin(node.Current.Join.Graph, node.Current.Join.Bindings, S(declarations.ToArray()));
        if (!join.TryReadPassed(join.Evaluate())) throw new InvalidOperationException("Actual public-port joins must be admitted before the sample is published.");
        var crossings = declareCrossing ? S(new Relation(E(join.Graph.Strokes)[0], E(join.Graph.Strokes)[1])) : S();
        return new(name, program, node, join, crossings);

        (IElement Site, Relation Publication) PublishedSite(Relation path, bool terminal)
        {
            var selection = terminal ? path : E((Relation)path.Right)[0];
            var role = E(ports).Cast<Relation>().Single(port => port.Right is Relation pick &&
                ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, selection)).Left;
            var publication = E(node.Current.Ports).Cast<Relation>().Single(port => ReferenceEquals(port.Left, role));
            var cursor = (Relation)((Relation)publication.Right).Right;
            var sourceSite = ((Relation)cursor.Left).Left;
            var quotient = E(node.Current.Join.SiteMap).Cast<Relation>()
                .Single(row => ReferenceEquals(row.Left, sourceSite)).Right;
            return (quotient, publication);
        }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
