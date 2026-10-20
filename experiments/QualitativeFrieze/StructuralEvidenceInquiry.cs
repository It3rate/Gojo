using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Bounded pattern questions over a supplier's published observation.
/// The receiver sees claims at advertised query ports, never the supplier's
/// complete graph. Native motif matching supplies the verdict; the learned
/// policy only ranks which port to request next.</summary>
public static class StructuralEvidenceInquiry
{
    // query = source-free pattern -> request seam
    // candidate = public output observation -> Closed(available output)
    // request = actual receiver-before -> actual query port
    // reply = request -> ((source -> observation event) ->
    //         (Closed(supported query) -> (Closed(refuted query) ->
    //         (Closed(open query) -> scoped proof))))
    // assessment = candidate -> (support -> (refutation -> (open -> blocked)))
    // selection = public hypotheses -> (published replies ->
    //             (candidate assessments -> (eligible -> conflicts)))
    public sealed class Query(string name, CommunicationPattern pattern)
    {
        public string Name { get; } = name;
        public CommunicationPattern Pattern { get; } = pattern;
        public Relation Surface { get; } = new(pattern.Surface, new Placeholder());
    }

    public sealed class Candidate
    {
        public string Name { get; }
        public QualitativeRecognitionObservation Observation { get; }
        public QualitativeMotifInduction Critic { get; }
        public Relation Capability { get; }
        public Relation Surface { get; }
        public bool Available => E(Capability).Any(item => ReferenceEquals(item, Observation.Surface));
        public Candidate(string name, QualitativeRecognitionObservation observation, bool available = true,
            QualitativeMotifInduction? critic = null)
        {
            Name = name; Observation = observation;
            Critic = critic ?? Read(observation);
            if (!ReferenceEquals(Critic.Observation, observation))
                throw new ArgumentException("Candidate critic must read its actual observation.", nameof(critic));
            Capability = available ? S(observation.Surface) : S();
            Surface = new(observation.Surface, Capability);
        }
    }

    public sealed class Response
    {
        public Query Query { get; }
        public Relation Request { get; }
        public IElement Source { get; }
        public IElement Revision { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Proof { get; }
        public Relation Surface { get; }
        public Response(Query query, Relation request, IElement source, IElement revision,
            Relation supported, Relation refuted, Relation open, Relation proof)
        {
            Query = query; Request = request; Source = source; Revision = revision;
            Supported = supported; Refuted = refuted; Open = open; Proof = proof;
            Surface = new(request, new Relation(new Relation(source, revision), new Relation(supported,
                new Relation(refuted, new Relation(open, proof)))));
        }
    }

    public sealed class CandidateRead
    {
        public Candidate Candidate { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public bool IsEligible => E(Refuted).Length == 0 && E(Blocked).Length == 0;
        internal CandidateRead(Candidate candidate, Relation supported, Relation refuted,
            Relation open, Relation blocked)
        {
            Candidate = candidate; Supported = supported; Refuted = refuted; Open = open; Blocked = blocked;
            Surface = new(candidate.Surface, new Relation(supported, new Relation(refuted, new Relation(open, blocked))));
        }
    }

    public sealed class Assessment
    {
        public IReadOnlyList<CandidateRead> Candidates { get; }
        public IReadOnlyList<CandidateRead> Eligible { get; }
        public Candidate? Chosen => Eligible.Count == 1 ? Eligible[0].Candidate : null;
        public Relation Conflicts { get; }
        public Relation Surface { get; }
        internal Assessment(Receiver receiver)
        {
            var conflicts = new List<IElement>();
            for (var i = 0; i < receiver.responses.Count; i++)
            for (var j = i + 1; j < receiver.responses.Count; j++)
            {
                var a = receiver.responses[i]; var b = receiver.responses[j];
                if (ReferenceEquals(a.Query, b.Query) && ReferenceEquals(a.Revision, b.Revision) &&
                    Opposed(State(a), State(b))) conflicts.Add(new Relation(a.Surface, b.Surface));
            }
            Conflicts = S(conflicts.ToArray());
            Candidates = receiver.Candidates.Select(candidate =>
            {
                var support = new List<IElement>(); var refuted = new List<IElement>(); var open = new List<IElement>();
                var blocked = new List<IElement>();
                if (!candidate.Available) blocked.Add(new Relation(candidate.Surface, candidate.Capability));
                blocked.AddRange(conflicts);
                foreach (var reply in receiver.responses)
                {
                    var prediction = receiver.Predict(candidate, reply.Query);
                    var state = State(reply); var expected = State(prediction);
                    var witness = new Relation(reply.Surface, prediction.Surface);
                    if (state == 0 || expected == 0) open.Add(witness);
                    else if (state == expected) support.Add(witness);
                    else refuted.Add(witness);
                }
                return new CandidateRead(candidate, S(support.ToArray()), S(refuted.ToArray()), S(open.ToArray()), S(blocked.ToArray()));
            }).ToArray();
            Eligible = Candidates.Where(candidate => candidate.IsEligible).ToArray();
            Surface = new(S(receiver.Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()),
                new Relation(S(receiver.responses.Select(reply => (IElement)reply.Surface).ToArray()),
                    new Relation(S(Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()),
                        new Relation(S(Eligible.Select(candidate => (IElement)candidate.Surface).ToArray()), Conflicts))));
        }
    }

    /// <summary>Public candidate library, ports and accepted evidence only.
    /// Neither source truth nor the supplier object is held by this receiver.</summary>
    public sealed class Receiver
    {
        public IReadOnlyList<Candidate> Candidates { get; }
        public IReadOnlyList<Query> Queries { get; }
        public Relation RelevantQueries { get; }
        public IReadOnlyList<Response> Responses => responses;
        public IReadOnlyList<Query> Requested => requested;
        public Relation Rejected { get; private set; } = S();
        public Assessment Selection { get; private set; }
        public Relation Surface => new(S(Candidates.Select(candidate => (IElement)candidate.Surface).ToArray()),
            new Relation(S(Queries.Select(query => (IElement)query.Surface).ToArray()),
                new Relation(RelevantQueries, new Relation(Selection.Surface,
                    new Relation(S(requested.Select(query => (IElement)query.Surface).ToArray()), Rejected)))));
        internal readonly List<Response> responses = [];
        private readonly List<Query> requested = [];
        private readonly HashSet<Relation> pending = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<(Candidate, Query), CommunicationAssessment> predictions = [];
        public Receiver(IReadOnlyList<Candidate> candidates, IReadOnlyList<Query> queries, Relation? relevantQueries = null)
        {
            Candidates = candidates; Queries = queries;
            RelevantQueries = relevantQueries ?? S(queries.Select(query => (IElement)query.Surface).ToArray());
            Selection = new(this);
        }
        internal Relation Issue(Query query)
        {
            var request = new Relation(Surface, query.Surface); pending.Add(request); return request;
        }
        internal CommunicationAssessment Predict(Candidate candidate, Query query)
        {
            if (!predictions.TryGetValue((candidate, query), out var result))
                predictions.Add((candidate, query), result = new(candidate.Critic, new CommunicationMessage([query.Pattern])));
            return result;
        }
        public Assessment Receive(Response response)
        {
            var authorized = pending.Remove(response.Request) &&
                Queries.Any(query => ReferenceEquals(query, response.Query)) &&
                ReferenceEquals(response.Request.Right, response.Query.Surface);
            var scopes = new[] { response.Supported, response.Refuted, response.Open };
            var valid = scopes.All(scope => Closed(scope) && E(scope).All(item => ReferenceEquals(item, response.Query.Surface))) &&
                scopes.Sum(scope => E(scope).Length) == 1;
            if (!authorized || !valid)
            { Rejected = S([.. E(Rejected), response.Surface]); return Selection; }
            if (!requested.Any(query => ReferenceEquals(query, response.Query))) requested.Add(response.Query);
            responses.Add(response); Selection = new(this); return Selection;
        }
    }

    /// <summary>An opaque, non-draining pattern-test service. AuditSurface is
    /// evaluator-only: no reply or receiver relation points back to it.</summary>
    public sealed class Supplier
    {
        private readonly QualitativeMotifInduction critic;
        private readonly IReadOnlyList<Query> queries;
        private readonly IElement source;
        private readonly IElement revision;
        private readonly List<Relation> audit = [];
        public Relation AuditSurface => new(critic.Surface, S(audit.Cast<IElement>().ToArray()));
        public Supplier(QualitativeRecognitionObservation observation, IReadOnlyList<Query> queries,
            int proposalCapacity = 4096, IElement? revision = null, IElement? source = null)
        {
            this.queries = queries; this.source = source ?? new Placeholder(); this.revision = revision ?? new Placeholder();
            critic = Read(observation, proposalCapacity);
        }
        public Response Request(Receiver receiver, Query query)
        {
            var request = receiver.Issue(query);
            if (!queries.Any(actual => ReferenceEquals(actual, query)) ||
                !receiver.Queries.Any(actual => ReferenceEquals(actual, query)))
                return new(query, request, source, revision, S(), S(), S(), S(query.Surface));
            var assessment = new CommunicationAssessment(critic, new CommunicationMessage([query.Pattern]));
            var state = State(assessment);
            // Never return Occurrence.Read or the complete matching witness:
            // both retain the entire source observation. Publish only selected
            // incidence/heading bindings, plus the finite grammar contract.
            var occurrences = assessment.Matches.Take(1).Select(match => (IElement)new Relation(match.Family.Definition,
                new Relation(match.Occurrence.SiteBindings, new Relation(match.Occurrence.SegmentBindings, match.Occurrence.Route)))).ToArray();
            var proof = new Relation(query.Pattern.Surface, new Relation(S(occurrences), critic.Budget));
            var response = new Response(query, request, source, revision,
                state > 0 ? S(query.Surface) : S(), state < 0 ? S(query.Surface) : S(), state == 0 ? S(query.Surface) : S(), proof);
            audit.Add(new Relation(response.Surface, assessment.Surface)); return response;
        }
        public CommunicationAssessment Evaluate(Query query) => new(critic, new CommunicationMessage([query.Pattern]));
    }

    public sealed record FeatureRead(Query Query, Relation Surface, Relation Correspondence, double[] Values);

    /// <summary>Evaluator-only comparison of the complete bounded existential
    /// motif vocabularies in both directions. This can refute a unique library
    /// guess without exposing the source to the receiver. Agreement does not
    /// establish graph isomorphism, repetition count, extent or provenance.</summary>
    public sealed class ObservationComparison
    {
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public bool IsCompatibleAtVocabulary => E(Refuted).Length == 0 && E(Open).Length == 0 && E(Blocked).Length == 0;
        internal ObservationComparison(QualitativeMotifInduction left, QualitativeMotifInduction right)
        {
            var supported = new List<IElement>(); var refuted = new List<IElement>();
            var open = new List<IElement>(); var blocked = new List<IElement>();
            Compare(left, right); Compare(right, left);
            foreach (var read in new[] { left, right })
                if (!read.IsComplete) open.Add(new Relation(read.Surface, new Relation(read.Unsearched, read.Unresolved)));
            Supported = S(supported.ToArray()); Refuted = S(refuted.ToArray());
            Open = S(open.ToArray()); Blocked = S(blocked.ToArray());
            Surface = new(new Relation(left.Surface, right.Surface),
                new Relation(Supported, new Relation(Refuted, new Relation(Open, Blocked))));

            void Compare(QualitativeMotifInduction source, QualitativeMotifInduction target)
            {
                foreach (var family in source.Families)
                {
                    var pattern = new CommunicationPattern(source.Observation.Frame, family.Definition);
                    if (!pattern.IsValid)
                    { blocked.Add(new Relation(source.Surface, family.Read)); continue; }
                    var assessment = new CommunicationAssessment(target, new CommunicationMessage([pattern]));
                    var selection = new Relation(source.Surface, family.Read);
                    foreach (var item in E(assessment.Supported)) supported.Add(new Relation(selection, item));
                    foreach (var item in E(assessment.Refuted)) refuted.Add(new Relation(selection, item));
                    foreach (var item in E(assessment.Unresolved)) open.Add(new Relation(selection, item));
                    foreach (var item in E(assessment.Blocked)) blocked.Add(new Relation(selection, item));
                }
            }
        }
    }

    public static ObservationComparison CompareObservations(QualitativeMotifInduction left,
        QualitativeMotifInduction right) => new(left, right);
    public static ObservationComparison CompareObservations(QualitativeRecognitionObservation left,
        QualitativeRecognitionObservation right) => new(Read(left), Read(right));

    /// <summary>The five slots retain their previous meanings and scale. The
    /// query's single pattern is one payload item; no source answer is inspected.</summary>
    public static FeatureRead Features(Receiver receiver, Query query, Relation basis)
    {
        var candidates = receiver.Selection.Eligible; var split = new List<IElement>();
        for (var i = 0; i < candidates.Count; i++) for (var j = i + 1; j < candidates.Count; j++)
            if (Opposed(State(receiver.Predict(candidates[i].Candidate, query)), State(receiver.Predict(candidates[j].Candidate, query))))
                split.Add(new Relation(candidates[i].Candidate.Surface, candidates[j].Candidate.Surface));
        var relevant = E(receiver.RelevantQueries).Where(item => ReferenceEquals(item, query.Surface)).ToArray();
        var known = receiver.Responses.Any(reply => ReferenceEquals(reply.Query, query) && State(reply) != 0);
        var payload = S(query.Pattern.Surface);
        var scopes = new[] { S(query.Surface), S(split.ToArray()), S(relevant), payload, known ? payload : S() };
        var keys = E(basis);
        if (keys.Length != 5) throw new ArgumentException("The transferred policy requires its actual five-slot basis.", nameof(basis));
        var rows = keys.Select((key, index) => (IElement)new Relation(key, scopes[index])).ToArray();
        var focusPairs = S(E(receiver.RelevantQueries).Select(item => (IElement)new Relation(item, item)).ToArray());
        var surface = new Relation(query.Surface, new Relation(focusPairs, S(rows)));
        var values = new[] { 1d, (double)split.Count / Math.Max(1, candidates.Count * (candidates.Count - 1) / 2),
            (double)relevant.Length / Math.Max(1, E(receiver.RelevantQueries).Length), .5, known ? 1d : 0d };
        return new(query, surface, S(rows), values);
    }
    public static Query[] Available(Receiver receiver) => receiver.Queries
        .Where(query => !receiver.Requested.Any(previous => ReferenceEquals(previous, query))).ToArray();
    public static double Predict(FeatureRead features, EvidenceSelectionStudy.LearnedPolicy learned) =>
        features.Values.Select((value, index) => value * learned.Coefficients[index]).Sum();
    public static Query? Select(Receiver receiver, EvidenceSelectionStudy.LearnedPolicy learned) => Available(receiver)
        .OrderByDescending(query => Predict(Features(receiver, query, learned.Basis), learned)).FirstOrDefault();
    private static QualitativeMotifInduction Read(QualitativeRecognitionObservation observation, int capacity = 4096) =>
        new(observation, maxCycleEdges: 6, proposalCapacity: capacity, maxWalkEdges: 3, retainedCapacity: 1024);
    private static int State(CommunicationAssessment assessment) => assessment.SupportedCount == 1 ? 1 : assessment.RefutedCount == 1 ? -1 : 0;
    private static int State(Response response) => E(response.Supported).Length == 1 ? 1 : E(response.Refuted).Length == 1 ? -1 : 0;
    private static bool Opposed(int a, int b) => a != 0 && b != 0 && a != b;
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}
