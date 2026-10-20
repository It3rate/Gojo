using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A small finite assumption/reveal/revision study. Shared reach is a
/// formal participation promise, not an inferred Euclidean length or angle.
/// Candidate names are presentation only; selection reads native hypotheses,
/// published observations, capability and explicit contextual preferences.</summary>
public sealed class EquivalenceRevisionStudy
{
    public sealed record Form(string Id, string Name, UnitArrangementProgram Program,
        ProvisionalEquivalence.Hypothesis Hypothesis, Relation RoleSelectors)
    { public Relation Surface => new(Program.Surface, new Relation(Hypothesis.Surface, RoleSelectors)); }

    public sealed class Realization
    {
        public string Id { get; }
        public string Name { get; }
        public QualitativeMotionFrame Frame => Graph.Frame;
        public UnitArrangementProgram Program { get; }
        public UnitArrangementNode Execution { get; }
        public QualitativeStrokeGraph Graph => Execution.Current.Join.Graph;
        public Relation Bindings => Execution.Current.Join.Bindings;
        public QualitativeRecognitionPublisher Publisher { get; }
        public QualitativeMotifInduction Critic { get; }
        public Relation CoarseRead { get; }
        public int CycleCount { get; }
        public bool CoarseClosed { get; }
        public Relation SpanValues { get; }
        public Relation SpanEvidence { get; }
        public Relation Surface { get; }

        internal Realization(Form form)
        {
            Id = form.Id; Name = form.Name; Program = form.Program;
            Execution = new("Fresh receiver output", [Program], Program, S());
            Publisher = CommunicationLoopStudy.Publish(Graph, Bindings);
            Critic = CommunicationLoopStudy.Induce(Publisher.Observation);
            CoarseRead = Graph.Evaluate();
            var summary = Graph.Describe(CoarseRead);
            CycleCount = summary.Contours;
            CoarseClosed = summary.Contours > 0 && summary.Endpoints == 0 && summary.Unresolved == 0 &&
                Critic.Families.Any(family => family.Kind == "cycle" && family.SegmentCount == 4);
            // The fine supplier publishes an intentionally selected extra
            // surface: actual role -> actual executed stroke. The span reader
            // receives this surface and actual graph, never intended values.
            var roleStrokes = S(E(form.RoleSelectors).Cast<Relation>().Select(selector =>
            {
                var port = E(Execution.Current.Ports).Cast<Relation>()
                    .Single(actual => ReferenceEquals(actual.Left, selector.Left));
                var cursor = (Relation)((Relation)port.Right).Right;
                var boundary = (Relation)cursor.Right;
                var receipt = (Relation)boundary.Right;
                if (!Execution.Drawing.TryReadAccepted(receipt, out _, out var added) || E(added).Length != 1)
                    throw new InvalidOperationException("A span supplier selects one acknowledged actual stroke.");
                var sourceStroke = E(added).Single();
                var target = E(Execution.Current.Join.StrokeMap).Cast<Relation>()
                    .Single(row => ReferenceEquals(row.Left, sourceStroke)).Right;
                return (IElement)new Relation(selector.Left, target);
            }).ToArray());
            SpanEvidence = ReadSpans(Graph, roleStrokes, out var values);
            SpanValues = values;
            Surface = new(Execution.Current.Surface, new Relation(Publisher.Surface,
                new Relation(Critic.Surface, new Relation(CoarseRead, SpanEvidence))));
        }
    }

    public sealed class Stage
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public ProvisionalEquivalence.Selection Selection { get; }
        public IReadOnlyList<ProvisionalEquivalence.Publication> Publications { get; }
        public ProvisionalEquivalence.Context Context { get; }
        public Realization? Output { get; }
        public CommunicationAssessment? CoarseAssessment { get; }
        public Relation Carried { get; }
        public Relation Withdrawn { get; }
        public Relation BlockedExact { get; }
        public bool LowerResolution { get; }
        public Relation Surface { get; }
        public Stage(string id, string name, string description,
            ProvisionalEquivalence.Selection selection,
            IReadOnlyList<ProvisionalEquivalence.Publication> publications,
            ProvisionalEquivalence.Context context, IReadOnlyList<Form> forms,
            CommunicationMessage coarsePromise,
            Stage? previous = null, bool lowerResolution = false, Relation? blockedExact = null)
        {
            Id = id; Name = name; Description = description; Selection = selection;
            Publications = publications; Context = context; LowerResolution = lowerResolution;
            BlockedExact = blockedExact ?? S();
            var selected = forms.SingleOrDefault(form => ReferenceEquals(form.Hypothesis, selection.Chosen));
            Output = selected is null ? null : new Realization(selected);
            CoarseAssessment = Output is null ? null : new(Output.Critic, coarsePromise);
            var beforeEqualities = previous?.Selection.Chosen?.Equalities ?? S();
            var afterEqualities = selection.Chosen?.Equalities ?? S();
            bool Present(IElement item, Relation scope) => item is Relation pair && E(scope).OfType<Relation>()
                .Any(other => ReferenceEquals(pair.Left, other.Left) && ReferenceEquals(pair.Right, other.Right));
            Carried = S(E(beforeEqualities).Where(item => Present(item, afterEqualities)).ToArray());
            Withdrawn = S(E(beforeEqualities).Where(item => !Present(item, afterEqualities)).ToArray());
            Surface = new(new Relation(coarsePromise.Surface, selection.Surface), new Relation(Output?.Surface ?? StructuralConstraint.Open(),
                new Relation(new Relation(Carried, Withdrawn),
                    new Relation(BlockedExact, new Relation(CoarseAssessment?.Surface ?? StructuralConstraint.Open(), previous?.Surface ?? S())))));
        }
    }

    public sealed record Case(string Id, string Name, string Description, IReadOnlyList<Stage> Stages,
        Relation NextMessage, Relation Correction, Relation CoarsePromise)
    { public Relation Surface => new(S(Stages.Select(stage => (IElement)stage.Surface).ToArray()),
        new Relation(NextMessage, new Relation(Correction, CoarsePromise))); }

    public QualitativeMotionFrame Frame { get; }
    public IElement Reach { get; }
    public IReadOnlyList<Form> Forms { get; }
    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    private EquivalenceRevisionStudy(QualitativeMotionFrame frame, IElement reach,
        IReadOnlyList<Form> forms, IReadOnlyList<Case> cases)
    {
        Frame = frame; Reach = reach; Forms = forms; Cases = cases;
        Surface = new(frame.Surface, new Relation(reach,
            new Relation(S(forms.Select(form => (IElement)form.Surface).ToArray()),
                S(cases.Select(item => (IElement)item.Surface).ToArray()))));
    }

    public static EquivalenceRevisionStudy Build()
    {
        var frame = new QualitativeMotionFrame(); IElement reach = new Placeholder();
        var author = new LatticeLadderAuthor(frame, reach);
        var allForms = new List<Form>(); var cases = new List<Case>();

        var one = BuildDomain(1);
        var square = Make("square", "Shared-reach four-cycle", one, [1], [0, 0]);
        var rectangle = Make("rectangle", "Unequal-reach four-cycle", one, [2], [0, 1]);
        var oneForms = new[] { square, rectangle };
        // The sender publishes an explicit coarse structural commitment. Its
        // definition contains incidence and headings, not selected sample spans.
        var exemplar = new Realization(rectangle);
        var cycle = exemplar.Critic.Families.First(family => family.Kind == "cycle" && family.SegmentCount == 4);
        var coarsePromise = new CommunicationMessage([new CommunicationPattern(exemplar.Publisher.Observation.Frame, cycle.Definition)]);
        var receiver = new ProvisionalEquivalence(one.Domain, S(reach));
        IElement instance = new Placeholder(), observedEvent = new Placeholder();
        IElement source = new Relation(instance, new Placeholder());
        var revealed = new ProvisionalEquivalence.Publication(source, observedEvent,
            Values(one.Roles, [2, 1]));
        var context = Context(oneForms);
        var initial = Run("E01-a", "A useful default", "Coarse headings and actual closure admit both constructions. Reusing one parameter is the simpler provisional choice; no source spans have been published.", receiver, oneForms, [], context);
        var refined = Run("E01-b", "Reveal a distinguishing span", "The source now exposes two shared-basis participations horizontally and one vertically. The shared-span assumption is refuted; the same coarse four-cycle remains valid.", receiver, oneForms, [revealed], context, initial);
        var next = Run("E01-c", "Send the distinction next time", "The next receiver receives the actual distinguishing publication immediately and need not make and repair the same default.", receiver, oneForms, [revealed], context);
        cases.Add(new("E01", "Square-like first, rectangular on refinement",
            "A formal common-reach default is revised without retracting the true coarse contour. Screen right angles are presentation; no Euclidean square inference is claimed.", [initial, refined, next], revealed.Surface,
            new Relation(refined.Surface, revealed.Surface), coarsePromise.Surface));

        var three = BuildDomain(3);
        var uniform = Make("uniform", "One shared parameter across three cells", three, [1, 1, 1], [0, 0, 0, 0, 0, 0]);
        var exception = Make("exception", "One local middle-span exception", three, [1, 2, 1], [0, 0, 1, 0, 0, 0]);
        var allWide = Make("wide", "Repeat the wider cell everywhere", three, [2, 2, 2], [0, 1, 0, 1, 0, 1]);
        var threeForms = new[] { uniform, exception, allWide };
        var repeatedReceiver = new ProvisionalEquivalence(three.Domain, S(reach));
        var repeatedSource = new Relation(new Placeholder(), new Placeholder());
        var partial = new ProvisionalEquivalence.Publication(repeatedSource, new Placeholder(),
            S(new Relation(three.Roles[0], S(reach)), new Relation(three.Roles[1], S(reach)),
                new Relation(three.Roles[4], S(reach)), new Relation(three.Roles[5], S(reach))));
        var middle = new ProvisionalEquivalence.Publication(repeatedSource, new Placeholder(), Values(three.Roles, [1, 1, 2, 1, 1, 1]));
        var repeatContext = Context(threeForms);
        var repeatBefore = Run("E02-a", "Reuse through the hidden cell", "The exposed outside cells agree. The middle cell remains unspecified, so uniform sharing is an economical guess.", repeatedReceiver, threeForms, [partial], repeatContext);
        var repeatAfter = Run("E02-b", "Keep the rule, scope the exception", "Only the middle horizontal span differs. The revised interpretation preserves outside equivalences and all three closures instead of abandoning the template.", repeatedReceiver, threeForms, [middle], repeatContext, repeatBefore);
        cases.Add(new("E02", "Repeated cells with one local exception", "A finite reusable template changes one binding, keeping unaffected correspondences inspectable.", [repeatBefore, repeatAfter], middle.Surface,
            new Relation(repeatAfter.Surface, middle.Surface), coarsePromise.Surface));

        IElement sharedInstance = new Placeholder(), sharedEvent = new Placeholder();
        IElement supplierA = new Relation(sharedInstance, new Placeholder()), supplierB = new Relation(sharedInstance, new Placeholder());
        var reportA = new ProvisionalEquivalence.Publication(supplierA, sharedEvent, Values(one.Roles, [1, 1]));
        var reportB = new ProvisionalEquivalence.Publication(supplierB, sharedEvent, Values(one.Roles, [2, 1]));
        var conflict = Run("E03-a", "Two incompatible reports", "Two exposed routes assert incompatible spans for the same roles, source instance and observation event. No hypothesis satisfies the whole evidence set.", receiver, oneForms, [reportA, reportB], context);
        var basisA = Context(oneForms, S(supplierA));
        var workingA = Run("E03-b", "Work provisionally through route A", "An explicit context scopes working selection to route A. The contradictory route-B report remains visible; the conflict has not been settled.", receiver, oneForms, [reportA, reportB], basisA, conflict);
        var basisB = Context(oneForms, S(supplierB));
        var workingB = Run("E03-c", "Work provisionally through route B", "Changing the declared working source changes the working construction. Both reports remain assessed and the disagreement is still present.", receiver, oneForms, [reportA, reportB], basisB, workingA);
        cases.Add(new("E03", "Contradiction is retained, context selects a working route", "Choosing an actionable interpretation is separate from declaring all observations reconciled.", [conflict, workingA, workingB], S(reportA.Surface, reportB.Surface),
            S(), coarsePromise.Surface));

        var limited = Context([square]);
        var blocked = Run("E04-a", "The fine request is blocked", "The engine can execute only the shared-reach program. Exposed unequal source spans refute that candidate; missing capability cannot make the fine evidence disappear.", receiver, oneForms, [revealed], limited);
        var coarseContext = Context([square], S());
        var coarse = Run("E04-b", "Accept an explicitly coarser answer", "A separate topology-only working request leaves the fine publication outside the selected read. The blocked exact request and unaccounted fine evidence are carried alongside the useful four-cycle output.", receiver, oneForms, [revealed], coarseContext, blocked, true, blocked.Selection.Surface);
        cases.Add(new("E04", "Capability changes the answer contract", "Lower-resolution success is useful and explicit; it does not certify exact reconstruction of the source spans.", [blocked, coarse], revealed.Surface,
            S(), coarsePromise.Surface));

        return new(frame, reach, allForms, cases);

        (Relation Domain, IElement[] Roles) BuildDomain(int cells)
        {
            var roles = Enumerable.Range(0, cells * 2).Select(_ => (IElement)new Placeholder()).ToArray();
            return (S(roles), roles);
        }
        Form Make(string id, string name, (Relation Domain, IElement[] Roles) scope, int[] widths, int[] groups)
        {
            var paths = widths.Select((width, index) => LatticeLadderAuthor.Path(author.Word(index * 4, 0),
                author.Word(width, 0), author.Word(0, 1), author.Word(-width, 0), author.Word(0, -1))).ToArray();
            var selectors = S(paths.SelectMany((path, index) => new IElement[] {
                new Relation(scope.Roles[index * 2], new Relation(path, E((Relation)path.Right)[0])),
                new Relation(scope.Roles[index * 2 + 1], new Relation(path, E((Relation)path.Right)[1])) }).ToArray());
            var ports = S([.. E(selectors), .. E(LatticeLadderAuthor.Ports(paths))]);
            var program = new UnitArrangementProgram(frame, name, S(paths), ports, S(paths));
            var parameters = groups.Distinct().Order().Select(_ => (IElement)new Placeholder()).ToArray();
            var roleValues = widths.SelectMany(width => new[] { width, 1 }).ToArray();
            var bindings = S(scope.Roles.Select((role, index) => (IElement)new Relation(role, parameters[groups[index]])).ToArray());
            var parameterValues = S(parameters.Select((parameter, index) => (IElement)new Relation(parameter,
                S(Enumerable.Repeat(reach, roleValues[Array.IndexOf(groups, index)]).ToArray()))).ToArray());
            var equalities = S(scope.Roles.SelectMany((role, index) => scope.Roles.Skip(index + 1)
                .Where((_, offset) => groups[index] == groups[index + offset + 1])
                .Select(other => (IElement)new Relation(role, other))).ToArray());
            var hypothesis = new ProvisionalEquivalence.Hypothesis(scope.Domain, program.Surface, bindings, parameterValues, equalities,
                new Relation(program.Surface, selectors));
            var result = new Form(id, name, program, hypothesis, selectors); allForms.Add(result); return result;
        }
        Relation Values(IElement[] roles, int[] participation) => S(roles.Select((role, index) =>
            (IElement)new Relation(role, S(Enumerable.Repeat(reach, participation[index]).ToArray()))).ToArray());
        ProvisionalEquivalence.Context Context(IReadOnlyList<Form> forms, Relation? sources = null) =>
            new(S(forms.Select(form => (IElement)form.Program.Surface).ToArray()), S(),
                S(forms.Select(form => (IElement)form.Hypothesis.Surface).ToArray()), sources);
        Stage Run(string id, string name, string description, ProvisionalEquivalence engine, IReadOnlyList<Form> forms,
            IReadOnlyList<ProvisionalEquivalence.Publication> publications, ProvisionalEquivalence.Context selectedContext,
            Stage? previous = null, bool lowerResolution = false, Relation? blockedExact = null) =>
            new(id, name, description, engine.Assess(forms.Select(form => form.Hypothesis).ToArray(), publications, selectedContext),
                publications, selectedContext, forms, coarsePromise, previous, lowerResolution, blockedExact);
    }

    /// <summary>An extra exposed span service over actual output strokes. It
    /// strips direction only after verifying one consistent pure-axis face;
    /// repeated unmeasured reaches remain ordered participation, never floats.</summary>
    public static Relation ReadSpans(QualitativeStrokeGraph graph, Relation roleStrokes, out Relation values)
    {
        var rows = new List<IElement>(); var proofs = new List<IElement>(); var open = new List<IElement>();
        if (!Closed(roleStrokes) || !Closed(graph.Strokes) || !Closed(graph.Sites))
        {
            values = S();
            return new(new Relation(graph.Surface, roleStrokes), new Relation(values,
                new Relation(S(), StructuralConstraint.Open(roleStrokes, graph.Strokes, graph.Sites))));
        }
        foreach (var item in E(roleStrokes))
        {
            if (item is not Relation focus || focus.Right is not Relation stroke ||
                E(roleStrokes).OfType<Relation>().Count(other => ReferenceEquals(other.Left, focus.Left)) != 1 ||
                !E(graph.Strokes).Any(actual => ReferenceEquals(actual, stroke)) ||
                !QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word) ||
                !E(graph.Sites).Any(site => ReferenceEquals(site, from)) || !E(graph.Sites).Any(site => ReferenceEquals(site, to)) || !Closed(word))
            { open.Add(item); continue; }
            var moves = E(word).OfType<Relation>().ToArray();
            if (moves.Length == 0 || moves.Length != E(word).Length ||
                !graph.Frame.Direction(moves[0], out var axis, out var forward, out _))
            { open.Add(item); continue; }
            var reaches = new List<IElement>(); var valid = true;
            foreach (var move in moves)
            {
                if (!graph.Frame.Direction(move, out var selectedAxis, out var face, out var participation) ||
                    !ReferenceEquals(axis, selectedAxis) || forward != face) { valid = false; break; }
                reaches.Add(participation);
            }
            if (!valid) { open.Add(item); continue; }
            var value = new Relation(focus.Left, S(reaches.ToArray())); rows.Add(value);
            proofs.Add(new Relation(focus, new Relation(word, value)));
        }
        values = S(rows.ToArray());
        return new(new Relation(graph.Surface, roleStrokes), new Relation(values,
            new Relation(S(proofs.ToArray()), open.Count == 0 ? S() : StructuralConstraint.Open(open.ToArray()))));
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
