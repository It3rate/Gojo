using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A finite communication experiment, not a trained recognizer. A
/// receiver sees a published library and message only. Its selected program
/// runs again, then publishes fresh observation identities before the sender's
/// held-out commitments are assessed. Names never select execution or matches.</summary>
public sealed class CommunicationLoopStudy
{
    public const int ProposalCapacity = 256;
    public const int RetentionCapacity = 96;

    public sealed class Realization
    {
        public string Id { get; }
        public string Name { get; }
        public QualitativeMotionFrame Frame => Graph.Frame;
        public UnitArrangementProgram Program { get; }
        public UnitArrangementNode Execution { get; }
        public QualitativeGraphJoin Join { get; }
        public QualitativeStrokeGraph Graph => Join.Graph;
        public Relation Bindings => Join.Bindings;
        public QualitativeRecognitionPublisher Publisher { get; }
        public QualitativeMotifInduction Induction { get; }
        public Relation Generation { get; }
        public Relation Surface { get; }
        internal Realization(string id, string name, UnitArrangementProgram program,
            UnitArrangementNode execution, QualitativeGraphJoin join, Relation? generation = null)
        {
            Id = id; Name = name; Program = program; Execution = execution; Join = join;
            Generation = generation ?? S();
            Publisher = Publish(Graph, Bindings); Induction = Induce(Publisher.Observation);
            Surface = new(new Relation(program.Surface, execution.Current.Surface),
                new Relation(join.Surface, new Relation(Publisher.Surface, new Relation(Induction.Surface, Generation))));
        }
    }

    public sealed class ReceiverContext
    {
        public string Id { get; }
        public string Name { get; }
        public IReadOnlyList<Realization> OrderedLibrary { get; }
        public Relation Preference { get; }
        public Relation Surface => Preference;
        public ReceiverContext(string id, string name, IReadOnlyList<Realization> preference)
        {
            Id = id; Name = name; OrderedLibrary = preference;
            Preference = S(preference.Select(item => (IElement)item.Surface).ToArray());
        }
    }

    public sealed record CandidateRead(Realization Realization, CommunicationAssessment Assessment)
    {
        public bool IsAdmissible => Assessment.IsSatisfied;
        public Relation Surface => new(Realization.Surface, Assessment.Surface);
    }

    /// <summary>The receiver's entire result. It has no sender intent argument
    /// or access to an intended example. Candidate ambiguity survives selection.</summary>
    public sealed class ReceiverReceipt
    {
        public CommunicationMessage Message { get; }
        public ReceiverContext Context { get; }
        public IReadOnlyList<CandidateRead> Candidates { get; }
        public IReadOnlyList<CandidateRead> Admissible { get; }
        public Realization? Selected { get; }
        public Realization? Output { get; }
        public QualitativeRecognitionPublisher? Publisher { get; }
        public QualitativeMotifInduction? Critic { get; }
        public Relation Surface { get; }
        internal ReceiverReceipt(CommunicationMessage message, ReceiverContext context)
        {
            Message = message; Context = context;
            Candidates = context.OrderedLibrary.Select(item =>
                new CandidateRead(item, new CommunicationAssessment(item.Induction, message))).ToArray();
            Admissible = Candidates.Where(item => item.IsAdmissible).ToArray();
            Selected = Admissible.FirstOrDefault()?.Realization;
            if (Selected is not null)
            {
                // Selection is followed by actual fresh acknowledged execution.
                // The critic never receives cached library occurrence identities.
                Output = Generate(Selected, message);
                Publisher = Output.Publisher;
                Critic = Output.Induction;
            }
            Surface = new(new Relation(context.Surface, message.Surface),
                new Relation(S(Candidates.Select(item => (IElement)item.Surface).ToArray()),
                    new Relation(S(Admissible.Select(item => (IElement)item.Realization.Surface).ToArray()),
                        Selected is null ? StructuralConstraint.Open() :
                        new Relation(Selected.Surface, new Relation(Output!.Surface,
                            new Relation(Publisher!.Surface, Critic!.Surface))))));
        }
    }

    public sealed class ReceiverLibrary
    {
        public IReadOnlyList<Realization> Realizations { get; }
        public Relation Surface { get; }
        internal ReceiverLibrary(IReadOnlyList<Realization> realizations)
        { Realizations = realizations; Surface = S(realizations.Select(item => (IElement)item.Surface).ToArray()); }
        public ReceiverReceipt Receive(CommunicationMessage message, ReceiverContext context)
        {
            if (context.OrderedLibrary.Count != Realizations.Count ||
                context.OrderedLibrary.Distinct(ReferenceEqualityComparer.Instance).Count() != Realizations.Count ||
                context.OrderedLibrary.Any(item => !Realizations.Any(actual => ReferenceEquals(actual, item))))
                throw new ArgumentException("A context orders the same actual published library.", nameof(context));
            return new(message, context);
        }
    }

    public sealed class Selection
    {
        public ReceiverReceipt Receipt { get; }
        public CommunicationMessage Message => Receipt.Message;
        public IReadOnlyList<CandidateRead> Candidates => Receipt.Candidates;
        public IReadOnlyList<CandidateRead> Admissible => Receipt.Admissible;
        public Realization? Selected => Receipt.Selected;
        public Realization? Output => Receipt.Output;
        public QualitativeRecognitionPublisher? Publisher => Receipt.Publisher;
        public QualitativeMotifInduction? Critic => Receipt.Critic;
        public CommunicationAssessment? Assessment { get; }
        public Relation Surface { get; }
        internal Selection(ReceiverReceipt receipt, CommunicationMessage intent)
        {
            Receipt = receipt;
            Assessment = receipt.Critic is null ? null : new(receipt.Critic, intent);
            Surface = new(receipt.Surface, Assessment?.Surface ?? StructuralConstraint.Open(intent.Surface));
        }
    }

    public sealed record Repair(Relation AddedClause, bool Forbidden,
        CommunicationMessage BeforeMessage, CommunicationMessage AfterMessage, Relation Surface);
    public sealed record Trial(ReceiverContext Context, Selection Before, Selection After, Repair? Repair)
    { public Relation Surface => new(Before.Surface, new Relation(Repair?.Surface ?? S(), After.Surface)); }
    public sealed record Case(string Id, string Name, string Description, Realization IntentExemplar,
        CommunicationMessage Intent, CommunicationMessage InitialMessage, IReadOnlyList<Trial> Trials)
    {
        public Relation Surface => new(new Relation(IntentExemplar.Surface, Intent.Surface),
            new Relation(InitialMessage.Surface, S(Trials.Select(item => (IElement)item.Surface).ToArray())));
    }

    public ReceiverLibrary Library { get; }
    public IReadOnlyList<ReceiverContext> Contexts { get; }
    public IReadOnlyList<Case> Cases { get; }
    public Relation Surface { get; }
    private CommunicationLoopStudy(ReceiverLibrary library, IReadOnlyList<ReceiverContext> contexts, IReadOnlyList<Case> cases)
    {
        Library = library; Contexts = contexts; Cases = cases;
        Surface = new(library.Surface, new Relation(S(contexts.Select(item => (IElement)item.Surface).ToArray()),
            S(cases.Select(item => (IElement)item.Surface).ToArray())));
    }

    public static CommunicationLoopStudy Build()
    {
        var forms = new List<Realization>();
        for (var extent = 1; extent <= 2; extent++)
        {
            var author = new Author(extent);
            forms.Add(author.Cross(true)); forms.Add(author.Cross(false));
            forms.Add(author.Rectangle(true)); forms.Add(author.Rectangle(false)); forms.Add(author.CutRectangle());
            forms.Add(author.Zigzag(true)); forms.Add(author.Zigzag(false));
        }
        var library = new ReceiverLibrary(forms);
        var contexts = new[] {
            new ReceiverContext("R01", "Fragments first · first extent", Order(["cross-cut", "rectangle-cut", "rectangle-open", "zigzag-cut", "cross-joined", "rectangle-closed", "zigzag-chain"], false)),
            new ReceiverContext("R02", "Open paths first · second extent", Order(["rectangle-open", "rectangle-cut", "cross-cut", "zigzag-cut", "cross-joined", "rectangle-closed", "zigzag-chain"], true))
        };
        var joined = forms.Single(item => item.Id == "cross-joined-1");
        var closed = forms.Single(item => item.Id == "rectangle-closed-1");
        var chain = forms.Single(item => item.Id == "zigzag-chain-1");
        var crossGoal = Pattern(joined, family => family.Kind == "junction" && family.SegmentCount == 4);
        var horizontal = Pattern(joined, family => family.Kind == "junction" && family.SegmentCount == 1 && HasAxis(joined, family, true));
        var vertical = Pattern(joined, family => family.Kind == "junction" && family.SegmentCount == 1 && HasAxis(joined, family, false));
        var closureGoal = Pattern(closed, family => family.Kind == "cycle" && family.SegmentCount == 4);
        // This chosen corner also survives in the open receiver contour.
        var open = forms.Single(item => item.Id == "rectangle-open-1");
        var corner = closed.Induction.Families.Where(family => family.Kind == "junction" && family.SegmentCount == 2)
            .Select(family => new CommunicationPattern(closed.Induction.Observation.Frame, family.Definition))
            .First(pattern => new CommunicationAssessment(open.Induction, new CommunicationMessage([pattern])).IsSatisfied);
        var chainGoal = Pattern(chain, family => family.Kind == "walk" && family.SegmentCount == 3);
        var diagonalEndpoint = Pattern(chain, family => family.Kind == "junction" && family.SegmentCount == 1);
        var cases = new[] {
            Make("C01", "A shared junction, not coincident arms", "Axial endpoint hints admit independent arms and other open paths. One actual four-arm incidence promise repairs the receiver's guess without fixing reach or screen dimensions.", joined,
                new CommunicationMessage([crossGoal]), new CommunicationMessage([horizontal, vertical])),
            Make("C02", "Closure, not an almost-closed walk", "A corner hint survives in an open rectangular path. Adding the actual cyclic participation promise selects a closed contour; the equal-looking unjoined endpoints do not certify closure.", closed,
                new CommunicationMessage([closureGoal]), new CommunicationMessage([corner])),
            Make("C03", "A connected alternating walk, not loose segments", "A diagonal endpoint alone also describes separate strokes. One ordered three-edge walk promise preserves connected alternation while leaving unmeasured extents free.", chain,
                new CommunicationMessage([chainGoal]), new CommunicationMessage([diagonalEndpoint]))
        };
        return new(library, contexts, cases);

        Realization[] Order(string[] ids, bool secondFirst) => ids.SelectMany(id => forms.Where(item => item.Id.StartsWith(id + "-", StringComparison.Ordinal))
            .OrderBy(item => secondFirst ? item.Id.EndsWith("-2", StringComparison.Ordinal) ? 0 : 1 : item.Id.EndsWith("-1", StringComparison.Ordinal) ? 0 : 1)).ToArray();
        Case Make(string id, string name, string description, Realization exemplar, CommunicationMessage intent, CommunicationMessage initial)
        {
            var trials = contexts.Select(context =>
            {
                var before = new Selection(library.Receive(initial, context), intent);
                var repair = FindRepair(before, intent);
                var after = new Selection(library.Receive(repair?.AfterMessage ?? initial, context), intent);
                return new Trial(context, before, after, repair);
            }).ToArray();
            return new(id, name, description, exemplar, intent, initial, trials);
        }
    }

    /// <summary>Correction is a finite sender-side counterexample read. It
    /// adds one previously unsent, actually refuted clause; it does not train a
    /// policy, transmit the intended example or replace the receiver context.</summary>
    private static Repair? FindRepair(Selection before, CommunicationMessage intent)
    {
        if (before.Critic is null) return null;
        foreach (var forbidden in new[] { false, true })
        {
            var goalScope = forbidden ? intent.Forbidden : intent.Required;
            var sent = forbidden ? before.Message.Forbidden : before.Message.Required;
            foreach (var clause in E(goalScope).Cast<Relation>())
            {
                if (E(sent).Any(actual => ReferenceEquals(actual, clause))) continue;
                var isolated = new CommunicationMessage(forbidden ? S() : S(clause), forbidden ? S(clause) : S());
                var failure = new CommunicationAssessment(before.Critic, isolated);
                if (failure.RefutedCount == 0) continue;
                var after = new CommunicationMessage(forbidden ? before.Message.Required : S([.. E(before.Message.Required), clause]),
                    forbidden ? S([.. E(before.Message.Forbidden), clause]) : before.Message.Forbidden);
                var witness = new Relation(new Relation(before.Message.Surface, before.Critic.Surface),
                    new Relation(failure.Surface, new Relation(new Relation(goalScope, clause), after.Surface)));
                return new(clause, forbidden, before.Message, after, witness);
            }
        }
        return null;
    }

    private static CommunicationPattern Pattern(Realization source, Func<QualitativeMotifFamily, bool> predicate)
    {
        var family = source.Induction.Families.First(predicate);
        return new(source.Induction.Observation.Frame, family.Definition);
    }
    private static bool HasAxis(Realization source, QualitativeMotifFamily family, bool horizontal)
    {
        var edges = (Relation)((Relation)family.Definition.Left).Right;
        var first = (Relation)E((Relation)edges.Right).Single();
        var heading = (Relation)((Relation)first.Right).Right;
        var directions = E(heading); var frame = source.Induction.Observation.Frame;
        return directions.Length == 1 && (horizontal
            ? ReferenceEquals(directions[0], frame.Left) || ReferenceEquals(directions[0], frame.Right)
            : ReferenceEquals(directions[0], frame.Up) || ReferenceEquals(directions[0], frame.Down));
    }

    private static Realization Generate(Realization selected, CommunicationMessage message)
    {
        var program = selected.Program;
        var node = new UnitArrangementNode("Receiver realization", [program], program, S());
        var oldPorts = E(selected.Execution.Advertisement().Right is Relation oldAdvertisement
            ? (Relation)oldAdvertisement.Right : throw new InvalidOperationException("A source advertises its publication surface."));
        var newPorts = E(node.Advertisement().Right is Relation newAdvertisement
            ? (Relation)newAdvertisement.Right : throw new InvalidOperationException("A receiver advertises its publication surface."));
        var ports = oldPorts.Cast<Relation>().Select(oldPort =>
        {
            var next = newPorts.Cast<Relation>().Single(port => ReferenceEquals(port.Left, oldPort.Left));
            var before = PublicSite(selected.Execution, oldPort);
            var after = PublicSite(node, next);
            var evidence = new Relation(new Relation(oldPort, next), new Relation(before, after));
            return (Before: before, After: after, Evidence: evidence);
        }).ToArray();
        var declarations = E(selected.Join.DeclaredJoins).Cast<Relation>().Select(declaration =>
        {
            var pair = (Relation)declaration.Left;
            var first = ports.First(port => ReferenceEquals(port.Before, pair.Left));
            var second = ports.First(port => ReferenceEquals(port.Before, pair.Right));
            return (IElement)new Relation(new Relation(first.After, second.After),
                new Relation(declaration, new Relation(first.Evidence, second.Evidence)));
        }).ToArray();
        var join = new QualitativeGraphJoin(node.Current.Join.Graph, node.Current.Join.Bindings, S(declarations));
        if (!join.TryReadPassed(join.Evaluate()))
            throw new InvalidOperationException("A replayed receiver seam must be admitted through its actual public ports.");
        var generation = new Relation(message.Surface, new Relation(selected.Surface,
            new Relation(S(ports.Select(port => (IElement)port.Evidence).ToArray()),
                new Relation(node.Current.Surface, join.Surface))));
        return new(selected.Id, selected.Name, program, node, join, generation);

        static IElement PublicSite(UnitArrangementNode owner, Relation actualPort)
        {
            if (!E(owner.Current.Ports).Any(port => ReferenceEquals(port, actualPort)) ||
                actualPort.Right is not Relation { Right: Relation { Left: Relation binding } })
                throw new InvalidOperationException("A port correspondence must use actual current publications.");
            return E(owner.Current.Join.SiteMap).Cast<Relation>()
                .Single(row => ReferenceEquals(row.Left, binding.Left)).Right;
        }
    }

    internal static QualitativeRecognitionPublisher Publish(QualitativeStrokeGraph graph, Relation bindings)
    {
        var edges = S(E(graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _);
            return (IElement)new Relation(stroke, new Relation(a, b));
        }).ToArray());
        var topology = new Relation(graph.Sites, new Relation(edges, S()));
        return new(graph.Frame, topology, bindings);
    }
    internal static QualitativeMotifInduction Induce(QualitativeRecognitionObservation observation) =>
        new(observation, maxCycleEdges: 4, proposalCapacity: ProposalCapacity, maxWalkEdges: 3, retainedCapacity: RetentionCapacity);

    private sealed class Author(int extent)
    {
        private readonly QualitativeMotionFrame frame = new();
        private readonly IElement h = new Placeholder(), v = new Placeholder();
        private Relation Word(int x, int y) => S([
            .. Enumerable.Repeat(x < 0 ? frame.Left : frame.Right, Math.Abs(x)).Select(direction => (IElement)QualitativeMotionFrame.Move(direction, h)),
            .. Enumerable.Repeat(y < 0 ? frame.Down : frame.Up, Math.Abs(y)).Select(direction => (IElement)QualitativeMotionFrame.Move(direction, v))]);
        public Realization Cross(bool joined)
        {
            var paths = new[] { (extent, 0), (-extent, 0), (0, extent + 1), (0, -extent - 1) }
                .Select(delta => LatticeLadderAuthor.Path(Word(0, 0), Word(delta.Item1, delta.Item2))).ToArray();
            return Execute(joined ? "cross-joined" : "cross-cut", joined ? "One four-arm junction" : "Four coincident independent arms", paths, [], joined);
        }
        public Realization Rectangle(bool closed)
        {
            var path = LatticeLadderAuthor.Path(Word(0, 0), Word(extent + 1, 0), Word(0, extent), Word(-extent - 1, 0), Word(0, -extent));
            return Execute(closed ? "rectangle-closed" : "rectangle-open", closed ? "Closed four-sided contour" : "Unjoined endpoint return", [path], closed ? [path] : []);
        }
        public Realization CutRectangle()
        {
            var paths = new[] {
                LatticeLadderAuthor.Path(Word(0, 0), Word(extent + 1, 0)),
                LatticeLadderAuthor.Path(Word(extent + 1, 0), Word(0, extent)),
                LatticeLadderAuthor.Path(Word(extent + 1, extent), Word(-extent - 1, 0)),
                LatticeLadderAuthor.Path(Word(0, extent), Word(0, -extent)) };
            return Execute("rectangle-cut", "Four separate contour sides", paths, []);
        }
        public Realization Zigzag(bool joined)
        {
            var chords = new[] { Word(extent, extent + 1), Word(extent, -extent - 1), Word(extent, extent + 1), Word(extent, -extent - 1) };
            var paths = joined ? new[] { LatticeLadderAuthor.Path(Word(0, 0), chords) } : chords.Select((word, index) =>
                LatticeLadderAuthor.Path(Word(index * extent, index % 2 == 0 ? 0 : extent + 1), word)).ToArray();
            return Execute(joined ? "zigzag-chain" : "zigzag-cut", joined ? "Connected alternating path" : "Separate alternating strokes", paths, []);
        }
        private Realization Execute(string id, string name, Relation[] paths, Relation[] closures, bool joinStarts = false)
        {
            var ports = LatticeLadderAuthor.Ports(paths);
            var program = new UnitArrangementProgram(frame, name, S(paths), ports, S(closures));
            var node = new UnitArrangementNode(name, [program], program, S());
            var declarations = new List<IElement>();
            if (joinStarts)
            {
                // Select actual published starting ports, then propose equality
                // joins. The join mechanism must admit the formal support.
                var starts = paths.Select(path =>
                {
                    var chord = E((Relation)path.Right)[0];
                    var role = E(ports).Cast<Relation>().Single(port => port.Right is Relation pick &&
                        ReferenceEquals(pick.Left, path) && ReferenceEquals(pick.Right, chord)).Left;
                    var publication = E(node.Current.Ports).Cast<Relation>().Single(port => ReferenceEquals(port.Left, role));
                    var cursor = (Relation)((Relation)publication.Right).Right;
                    return (Site: ((Relation)cursor.Left).Left, Cause: publication);
                }).ToArray();
                foreach (var other in starts.Skip(1)) declarations.Add(new Relation(new Relation(starts[0].Site, other.Site), new Relation(starts[0].Cause, other.Cause)));
            }
            var join = new QualitativeGraphJoin(node.Current.Join.Graph, node.Current.Join.Bindings, S(declarations.ToArray()));
            if (!join.TryReadPassed(join.Evaluate())) throw new InvalidOperationException("A receiver library construction must retain admitted joins.");
            return new(id + "-" + extent, name + " / extent " + extent, program, node, join);
        }
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}
