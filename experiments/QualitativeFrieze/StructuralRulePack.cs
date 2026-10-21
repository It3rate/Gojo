using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using R = Gojo.Experiments.QualitativeFrieze.OrnamentReconstruction;
using G = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultGeometry;
using A = Gojo.Experiments.QualitativeFrieze.OrnamentArrangementRead;
using C = Gojo.Experiments.QualitativeFrieze.OrnamentParticipationCount;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A portable native policy and its exposed question services. A pack
/// has an unbound target role; an application connects a concrete observation
/// and retains every service receipt. Names never choose a reader or verdict.</summary>
public sealed class StructuralRulePack
{
    // pack = target role -> (policy -> (Closed(observation queries) ->
    //        (Closed(word-basis questions) ->
    //        (Closed(arrangement questions) -> Closed(participation questions)))))
    // connection = actual target role -> actual input surface
    // application = pack -> (connection -> (geometry passage ->
    //               (Closed(producer receipts) -> (observation -> policy receipt))))
    public string Name { get; }
    public Relation Surface { get; }
    public Placeholder TargetRole { get; } = null!;
    public D.Policy Policy { get; } = null!;
    public IReadOnlyList<R.Query> ObservationQueries { get; } = [];
    public IReadOnlyList<G.Question> GeometryQuestions { get; } = [];
    public IReadOnlyList<A.Question> ArrangementQuestions { get; } = [];
    public IReadOnlyList<C.Question> CountQuestions { get; } = [];
    public IReadOnlyList<Relation> ObservationQuerySurfaces { get; } = [];
    public IReadOnlyList<Relation> GeometryQuestionSurfaces { get; } = [];
    /// <summary>Exact advertised predicates. An arrangement parser performs an
    /// explicit alpha-rebinding; its compiled Question.Surface can differ from
    /// this actual policy predicate. Applications retain that correspondence.</summary>
    public IReadOnlyList<Relation> ArrangementQuestionSurfaces { get; } = [];
    public IReadOnlyList<Relation> CountQuestionSurfaces { get; } = [];
    public Relation Blocked { get; }
    public Relation Open { get; }
    public Relation Validation { get; }
    public bool IsValid => Empty(Blocked) && Empty(Open);

    public StructuralRulePack(string name, D.Policy policy,
        IReadOnlyList<R.Query>? observationQueries = null,
        IReadOnlyList<G.Question>? geometryQuestions = null,
        IReadOnlyList<A.Question>? arrangementQuestions = null,
        IReadOnlyList<C.Question>? countQuestions = null)
        : this(name, Make(policy, observationQueries ?? [], geometryQuestions ?? [], arrangementQuestions ?? [], countQuestions ?? [])) { }

    private StructuralRulePack(string name, Relation surface)
    {
        Name = name; Surface = surface;
        var blocked = new List<IElement>(); var open = new List<IElement>(); var proofs = new List<IElement>();
        if (surface is not { Left: Placeholder { IsInverse: false } target,
            Right: Relation { Left: Relation policy, Right: Relation { Left: Relation observation,
                Right: Relation { Left: Relation geometry, Right: Relation { Left: Relation arrangement, Right: Relation counts } } } } })
            blocked.Add(surface);
        else
        {
            TargetRole = target; Policy = new(policy);
            blocked.AddRange(E(Policy.Blocked)); open.AddRange(E(Policy.Open));
            ObservationQuerySurfaces = Rows(observation); GeometryQuestionSurfaces = Rows(geometry);
            ArrangementQuestionSurfaces = Rows(arrangement); CountQuestionSurfaces = Rows(counts);
            var all = ObservationQuerySurfaces.Concat(GeometryQuestionSurfaces)
                .Concat(ArrangementQuestionSurfaces).Concat(CountQuestionSurfaces).ToArray();
            if (all.Distinct(ReferenceEqualityComparer.Instance).Count() != all.Length) blocked.Add(surface);
            foreach (var rule in Policy.Rules)
                if (all.Count(actual => ReferenceEquals(actual, rule.Predicate)) != 1) blocked.Add(rule.Surface);
            if (R.Participants(new Relation(policy, ((Relation)surface.Right).Right))
                .Any(participant => FieldNative.Same(participant, target))) blocked.Add(target);

            ObservationQueries = ObservationQuerySurfaces.Select(row =>
            {
                var read = new R.Query("exposed observation question", row);
                blocked.AddRange(E(read.Blocked)); open.AddRange(E(read.Open)); return read;
            }).ToArray();
            var metric = new List<G.Question>();
            foreach (var row in GeometryQuestionSurfaces)
                if (G.Question.TryRead("exposed word-basis question", row, out var question, out var proof))
                { metric.Add(question); proofs.Add(proof); }
                else blocked.Add(new Relation(row, proof));
            GeometryQuestions = metric.ToArray();
            ArrangementQuestions = ArrangementQuestionSurfaces.Select(row =>
            {
                var read = new A.Question("exposed arrangement question", row);
                blocked.AddRange(E(read.Blocked)); open.AddRange(E(read.Open));
                proofs.Add(new Relation(row, read.Surface)); return read;
            }).ToArray();
            CountQuestions = CountQuestionSurfaces.Select(row =>
            {
                var read = new C.Question(row); blocked.AddRange(E(read.Blocked)); open.AddRange(E(read.Open)); return read;
            }).ToArray();
        }
        Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        Validation = new(surface, new Relation(S(proofs.ToArray()), new Relation(Blocked, Open)));

        Relation[] Rows(Relation scope)
        {
            Scope(scope, blocked, open); var rows = new List<Relation>();
            foreach (var item in E(scope)) if (item is Relation row) rows.Add(row); else blocked.Add(item);
            return rows.ToArray();
        }
    }

    public static bool TryRead(Relation surface, out StructuralRulePack pack, out Relation witness)
    {
        var read = new StructuralRulePack("imported structural rule pack", surface); witness = read.Validation;
        pack = read.IsValid ? read : null!; return read.IsValid;
    }

    public sealed class Application
    {
        public StructuralRulePack Pack { get; }
        public R.Input Input { get; }
        public Relation Connection { get; }
        public Relation GeometryConnection { get; }
        public Relation GeometryAccepted { get; }
        public Relation GeometryBlocked { get; }
        public Relation GeometryOpen { get; }
        public Relation ProducerReads { get; }
        public D.Observation Observation { get; }
        public D.Receipt Receipt { get; }
        public Relation Surface { get; }

        public Application(StructuralRulePack pack, R.Input input, G.Publication? geometry = null, D.Directive? directive = null)
        {
            Pack = pack; Input = input; Connection = new(pack.TargetRole ?? new Placeholder(), input.Surface);
            var facts = new List<D.Evidence>(); var producers = new List<IElement>();
            var requestedGeometry = pack.GeometryQuestions.Count != 0;
            var usableGeometry = requestedGeometry && geometry is not null && SameObservation(input.Observation, geometry.Observation)
                && SameScope(input.MechanicalGroups, geometry.MechanicalGroups);
            var port = geometry?.Port ?? StructuralConstraint.Open(input.Observation.Surface);
            var passage = new Relation(Connection, port);
            GeometryAccepted = usableGeometry ? S(passage) : S();
            GeometryBlocked = requestedGeometry && geometry is not null && !usableGeometry ? S(passage) : S();
            GeometryOpen = requestedGeometry && !usableGeometry ? S(passage) : S();
            GeometryConnection = new(passage, new Relation(GeometryAccepted, new Relation(GeometryBlocked, GeometryOpen)));
            if (pack.IsValid)
            {
                var bank = new R.Bank([input], pack.ObservationQueries);
                foreach (var query in pack.ObservationQueries)
                {
                    var read = bank.Read(input, query); Add(query.Surface, read.Answer, read.Surface);
                }
                foreach (var question in pack.GeometryQuestions)
                    if (usableGeometry)
                    {
                        var read = new G.Receipt(question, geometry!);
                        Add(question.Surface, read.Answer, new Relation(GeometryConnection, read.Surface));
                    }
                    else Add(question.Surface, "Open", GeometryConnection);
                for (var index = 0; index < pack.ArrangementQuestions.Count; index++)
                {
                    var question = pack.ArrangementQuestions[index]; var read = new A.Receipt(question, input.Observation);
                    var predicate = pack.ArrangementQuestionSurfaces[index];
                    Add(predicate, read.Answer, new Relation(new Relation(predicate, question.Surface), read.Surface));
                }
                foreach (var question in pack.CountQuestions)
                {
                    var read = new C.Receipt(question, input.MechanicalGroups); Add(question.Surface, read.Answer, read.Surface);
                }
            }
            else
                foreach (var rule in pack.Policy?.Rules ?? []) Add(rule.Predicate, "Open", pack.Validation);
            ProducerReads = S(producers.ToArray());
            Observation = pack.IsValid ? new(Connection, facts) : new(new Relation(Connection,
                StructuralConstraint.Open(facts.Select(fact => (IElement)fact.Surface).ToArray())));
            Receipt = new(pack.Policy ?? new D.Policy(new Relation(S(), StructuralConstraint.Open())), Observation, directive);
            Surface = new(pack.Surface, new Relation(Connection, new Relation(GeometryConnection,
                new Relation(ProducerReads, new Relation(Observation.Surface, Receipt.Surface)))));

            void Add(Relation predicate, string answer, Relation witness)
            {
                var producer = new Relation(Connection, new Relation(predicate, witness)); producers.Add(producer);
                facts.Add(D.Evidence.FromAnswer(predicate, answer, producer));
            }
        }
    }

    private static Relation Make(D.Policy policy, IReadOnlyList<R.Query> observation, IReadOnlyList<G.Question> geometry,
        IReadOnlyList<A.Question> arrangement, IReadOnlyList<C.Question> counts) => new(new Placeholder(),
        new Relation(policy.Surface, new Relation(S(observation.Select(query => (IElement)query.Surface).ToArray()),
            new Relation(S(geometry.Select(question => (IElement)question.Surface).ToArray()),
                new Relation(S(arrangement.Select(question => (IElement)question.Surface).ToArray()),
                    S(counts.Select(question => (IElement)question.Surface).ToArray()))))));

    private static bool SameObservation(QualitativeRecognitionObservation first, QualitativeRecognitionObservation second) =>
        ReferenceEquals(first.Frame.Surface, second.Frame.Surface) && SameScope(first.Sites, second.Sites) &&
        SameScope(first.Segments, second.Segments) && SameScope(first.Continuations, second.Continuations) && SameScope(first.Crossings, second.Crossings);

    private static bool SameScope(Relation first, Relation second)
    {
        if (ReferenceEquals(first, second)) return true;
        return StructuralConstraint.TryReadScope(first, out var a, out var from, out var to) &&
            StructuralConstraint.TryReadScope(second, out var b, out var otherFrom, out var otherTo) &&
            FieldNative.Same(from, to) == FieldNative.Same(otherFrom, otherTo) && a.Length == b.Length &&
            a.Select((item, index) => FieldNative.Same(item, b[index])).All(equal => equal);
    }
    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Empty(Relation scope) => E(scope).Length == 0;
    private static void Scope(Relation scope, List<IElement> blocked, List<IElement> open)
    {
        if (!StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail)) blocked.Add(scope);
        else if (!FieldNative.Same(seam, tail)) open.Add(scope);
    }
}
