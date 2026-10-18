using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A local observational service exposed through actual node-to-port
/// connections, a native question contract and finite shared capacity. The
/// callback implements that advertised service; it does not receive private
/// internals from the requesting system.</summary>
public sealed class QualitativeEvidenceService
{
    // publication = interface -> (ports -> (question -> (revision -> capacity)))
    // allocation = actual capacity slot -> actual request
    // cached observation = (actual node -> actual port) ->
    //                      (actual revision -> actual reply)
    public Relation Interface { get; }
    public Relation Ports { get; }
    public IElement Question { get; }
    public IElement Revision { get; }
    public Relation Capacity { get; }
    public Relation Publication { get; }
    public Relation Spent => N.S(allocations.ToArray());
    public Relation Available => N.S(N.E(Capacity).Skip(allocations.Count).ToArray());
    public Relation Cache => N.S(cache.Select(entry => (IElement)new Relation(new Relation(entry.Node, entry.Port),
        new Relation(Revision, entry.Reply.Surface))).ToArray());
    public Relation Surface => new(Publication, new Relation(Spent, new Relation(Available, Cache)));
    private readonly Func<QualitativeEvidenceRequest, QualitativeEvidenceReply> read;
    private readonly List<IElement> allocations = [];
    private readonly List<Cached> cache = [];

    public QualitativeEvidenceService(Relation exposedInterface, Relation ports, IElement question, int capacity,
        Func<QualitativeEvidenceRequest, QualitativeEvidenceReply> read, IElement? revision = null)
        : this(exposedInterface, ports, question, Slots(capacity), read, revision) { }

    public QualitativeEvidenceService(Relation exposedInterface, Relation ports, IElement question, Relation capacity,
        Func<QualitativeEvidenceRequest, QualitativeEvidenceReply> read, IElement? revision = null)
    {
        Interface = exposedInterface; Ports = ports; Question = question; Capacity = capacity;
        Revision = revision ?? new Placeholder(); this.read = read;
        Publication = new(exposedInterface, new Relation(ports, new Relation(question, new Relation(Revision, capacity))));
    }

    internal bool Owns(IElement node, IElement port) => N.Read(Ports, out var rows) && rows.OfType<Relation>()
        .Count(row => N.Same(row.Left, node) && N.Same(row.Right, port)) == 1;
    internal bool Reuse(QualitativeEvidenceRequest request, out QualitativeEvidenceReply reply)
    {
        var prior = cache.SingleOrDefault(entry => N.Same(entry.Node, request.Node) && N.Same(entry.Port, request.Port));
        reply = prior?.Reply!; return prior is not null;
    }
    internal bool Allocate(QualitativeEvidenceRequest request, out Relation allocation)
    {
        allocation = null!;
        if (!N.Read(Capacity, out var slots) || !N.Distinct(slots) || slots.Any(slot => slot is not Placeholder) || allocations.Count >= slots.Length) return false;
        allocation = new(slots[allocations.Count], request.Read); allocations.Add(allocation); return true;
    }
    internal QualitativeEvidenceReply Read(QualitativeEvidenceRequest request) => read(request);
    internal void Remember(QualitativeEvidenceRequest request, QualitativeEvidenceReply reply) => cache.Add(new(request.Node, request.Port, reply));
    private static Relation Slots(int capacity)
    { ArgumentOutOfRangeException.ThrowIfNegative(capacity); return N.S(Enumerable.Range(0, capacity).Select(_ => (IElement)new Placeholder()).ToArray()); }
    private sealed record Cached(IElement Node, IElement Port, QualitativeEvidenceReply Reply);
}

/// <summary>A downward question caused by an actual unresolved higher-level
/// obligation. Expected is a question, never a supplied answer.</summary>
public sealed class QualitativeEvidenceRequest
{
    public Relation Cause { get; }
    public Relation Obligation { get; }
    public IElement Node { get; }
    public IElement Expected { get; }
    public IElement Port { get; }
    public Relation Route { get; }
    public IElement Revision { get; }
    public Relation Read { get; }
    internal QualitativeEvidenceRequest(Relation cause, Relation obligation, IElement node, IElement expected,
        IElement port, Relation route, QualitativeEvidenceService service)
    {
        Cause = cause; Obligation = obligation; Node = node; Expected = expected; Port = port; Route = route; Revision = service.Revision;
        Read = new(new Relation(cause, new Relation(obligation, expected)),
            new Relation(new Relation(node, port), new Relation(route, service.Publication)));
    }
}

/// <summary>An actual publication or explicit refusal. A closed empty symbol
/// scope is a valid negative answer; a closed empty publication scope refuses
/// the request. These have different native shapes.</summary>
public sealed class QualitativeEvidenceReply
{
    public Relation Surface { get; }
    public QualitativeEvidenceReply(QualitativeEvidenceRequest request, Relation symbols, Relation evidence, IElement? revision = null)
        : this(request, request.Port, revision ?? request.Revision, N.S(new Relation(request.Node, symbols)), evidence) { }
    public QualitativeEvidenceReply(QualitativeEvidenceRequest request, IElement port, IElement revision,
        Relation publications, Relation evidence) => Surface = new(new Relation(request.Read, port),
            new Relation(publications, new Relation(revision, evidence)));
    public static QualitativeEvidenceReply Refuse(QualitativeEvidenceRequest request, Relation reason) =>
        new(request, request.Port, request.Revision, N.S(), reason);
}

/// <summary>One finite, non-draining refinement round. It queries only actual
/// unresolved node obligations, preserves accepted contrary answers, and keeps
/// capacity blocks, refusals, stale publications and repeated observations
/// distinct. No source geometry or numerical clock is introduced.</summary>
public sealed class QualitativeEvidenceRefinement
{
    // request = (actual higher read -> (actual obligation -> expected question))
    //           -> ((actual node -> actual port) -> (actual route -> service publication))
    // accepted = actual request -> (actual allocation -> actual reply)
    // blocked = actual demand/request -> (requested route -> available service scope)
    // rejected/stale = actual request -> actual reply with native reason
    // correspondence = actual prior node row -> actual accepted publication row
    // qualitative step = before -> (actual event -> (after -> remainder))
    // timeline = Closed(actual ordered steps) -> Closed(actual step -> next step)
    public QualitativePatternGrammarEvaluation Before { get; }
    public IElement ParentCause { get; }
    public Relation CausalBridge { get; }
    public QualitativePatternTokenGraph After { get; }
    public Relation Routes { get; }
    public Relation Requests { get; }
    public Relation Accepted { get; }
    public Relation Reused { get; }
    public Relation Blocked { get; }
    public Relation Rejected { get; }
    public Relation Stale { get; }
    public Relation NewFacts { get; }
    public Relation ReusedFacts { get; }
    public Relation Facts { get; }
    public Relation Correspondence { get; }
    public Relation Remaining { get; }
    public Relation Timeline { get; }
    public Relation Services { get; }
    public Relation Surface { get; }
    public IReadOnlyList<QualitativeEvidenceRequest> RequestReads { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Rejected).Length == 0 && N.E(Stale).Length == 0 && N.E(Remaining).Length == 0;

    public QualitativeEvidenceRefinement(QualitativePatternGrammarEvaluation before, Relation routes,
        params QualitativeEvidenceService[] services)
        : this(before, routes, before.Surface, services) { }

    public QualitativeEvidenceRefinement(QualitativePatternGrammarEvaluation before, Relation routes,
        IElement parentCause, params QualitativeEvidenceService[] services)
    {
        Before = before; Routes = routes; ParentCause = parentCause; CausalBridge = new(parentCause, before.Surface);
        var requests = new List<IElement>(); var accepted = new List<IElement>(); var reused = new List<IElement>();
        var requestReads = new List<QualitativeEvidenceRequest>();
        var blocked = new List<IElement>(); var rejected = new List<IElement>(); var stale = new List<IElement>();
        var newFacts = new List<IElement>(); var reusedFacts = new List<IElement>(); var correspondence = new List<IElement>();
        var remaining = new List<IElement>(); var steps = new List<Relation>();
        IElement currentEvidence = before.Graph.Surface;
        var updates = new Dictionary<IElement, Relation>(N.Participants.Instance);
        var routeReady = N.Read(routes, out var routeRows);
        foreach (var item in N.E(before.Demands))
        {
            if (item is not Relation demand || !before.TryReadDemand(demand, out var obligation) || !obligation.IsUnresolved ||
                !before.Graph.TryFindNode(obligation.Node, out var priorRow, out var priorSymbols) || !ReferenceEquals(priorRow, obligation.NodeRow))
            { var eventRead = new Relation(item, before.Graph.Surface); blocked.Add(eventRead); remaining.Add(item); Record(eventRead); continue; }
            var connections = routeRows.OfType<Relation>().Where(row => N.Same(row.Left, obligation.Node)).ToArray();
            if (!routeReady || connections is not [var connection])
            { var eventRead = new Relation(demand, routes); blocked.Add(eventRead); remaining.Add(demand); Record(eventRead); continue; }
            var suppliers = services.Where(service => service.Owns(obligation.Node, connection.Right)).ToArray();
            if (suppliers is not [var supplier])
            { var eventRead = new Relation(demand, new Relation(connection, N.S(services.Select(service => (IElement)service.Publication).ToArray()))); blocked.Add(eventRead); remaining.Add(demand); Record(eventRead); continue; }
            var request = new QualitativeEvidenceRequest(CausalBridge, demand, obligation.Node, obligation.Expected, connection.Right, connection, supplier);
            requests.Add(request.Read); requestReads.Add(request);
            if (supplier.Reuse(request, out var cached))
            {
                if (!ReadReply(cached, null, request.Port, request.Revision, request.Node, out var reusedRow, out _, out _) ||
                    !Refines(priorSymbols, (Relation)reusedRow.Right))
                { var rejectedRead = new Relation(request.Read, cached.Surface); rejected.Add(rejectedRead); remaining.Add(demand); Record(rejectedRead); continue; }
                var eventRead = new Relation(request.Read, cached.Surface); reused.Add(eventRead); reusedFacts.Add(reusedRow);
                updates[request.Node] = reusedRow; correspondence.Add(new Relation(priorRow, reusedRow));
                if (!Settled((Relation)reusedRow.Right, request.Expected)) remaining.Add(demand);
                Record(eventRead, reusedRow); continue;
            }
            if (!supplier.Allocate(request, out var allocation))
            { var blockedRead = new Relation(request.Read, new Relation(supplier.Publication, supplier.Available)); blocked.Add(blockedRead); remaining.Add(demand); Record(blockedRead); continue; }
            var reply = supplier.Read(request);
            if (!ReadReply(reply, request.Read, request.Port, null, request.Node, out var fact, out var actualRevision, out var evidence))
            { var eventRead = new Relation(request.Read, new Relation(allocation, reply.Surface)); rejected.Add(eventRead); remaining.Add(demand); Record(eventRead); continue; }
            if (!N.Same(actualRevision, request.Revision))
            { var eventRead = new Relation(request.Read, new Relation(allocation, reply.Surface)); stale.Add(eventRead); remaining.Add(demand); Record(eventRead); continue; }
            if (!Refines(priorSymbols, (Relation)fact.Right))
            { var eventRead = new Relation(new Relation(request.Read, priorRow), new Relation(allocation, reply.Surface)); rejected.Add(eventRead); remaining.Add(demand); Record(eventRead); continue; }
            var acceptedRead = new Relation(request.Read, new Relation(allocation, reply.Surface));
            accepted.Add(acceptedRead); newFacts.Add(fact); updates[request.Node] = fact;
            correspondence.Add(new Relation(priorRow, fact)); supplier.Remember(request, reply);
            if (!Settled((Relation)fact.Right, request.Expected)) remaining.Add(demand);
            Record(acceptedRead, fact);
        }
        var nodes = N.E(before.Graph.Nodes).Select(row => row is Relation prior && updates.TryGetValue(prior.Left, out var replacement) ? replacement : row).ToArray();
        After = updates.Count == 0 ? before.Graph : new QualitativePatternTokenGraph(
            N.Read(before.Graph.Nodes, out _) ? N.S(nodes) : StructuralConstraint.Open(nodes), before.Graph.Links);
        Requests = N.S(requests.ToArray()); Accepted = N.S(accepted.ToArray()); Reused = N.S(reused.ToArray());
        RequestReads = requestReads;
        Blocked = N.S(blocked.ToArray()); Rejected = N.S(rejected.ToArray()); Stale = N.S(stale.ToArray());
        NewFacts = N.S(Unique(newFacts)); ReusedFacts = N.S(Unique(reusedFacts)); Facts = N.S(Unique(newFacts.Concat(reusedFacts)));
        Correspondence = N.S(Unique(correspondence)); Remaining = N.S(remaining.ToArray());
        var fold = new Relation(Correspondence, new Relation(Facts, After.Surface));
        steps.Add(new(currentEvidence, new Relation(fold, new Relation(After.Surface, Remaining))));
        Timeline = new(N.S(steps.Cast<IElement>().ToArray()), N.S(steps.Zip(steps.Skip(1)).Select(pair => (IElement)new Relation(pair.First, pair.Second)).ToArray()));
        Services = N.S(services.Select(service => (IElement)service.Surface).ToArray());
        Surface = new(new Relation(CausalBridge, new Relation(routes, Services)), new Relation(Requests,
            new Relation(Accepted, new Relation(Reused, new Relation(Blocked, new Relation(Rejected, new Relation(Stale,
                new Relation(NewFacts, new Relation(ReusedFacts, new Relation(Correspondence,
                    new Relation(After.Surface, new Relation(Remaining, Timeline))))))))))));

        void Record(Relation eventRead, IElement? received = null)
        {
            var nextEvidence = received is null ? currentEvidence :
                new Relation(before.Graph.Surface, N.S(Unique(newFacts.Concat(reusedFacts))));
            steps.Add(new(currentEvidence, new Relation(eventRead, new Relation(nextEvidence, N.S(remaining.ToArray())))));
            currentEvidence = nextEvidence;
        }
    }

    public Relation Evaluate() => Surface;
    public bool TryReadRequest(Relation actual, out QualitativeEvidenceRequest request)
    {
        var found = RequestReads.FirstOrDefault(entry => ReferenceEquals(entry.Read, actual)); request = found!;
        return found is not null && Before.TryReadDemand(found.Obligation, out var obligation) && obligation.IsUnresolved &&
            N.Same(found.Node, obligation.Node) && N.Same(found.Expected, obligation.Expected) &&
            N.E(Routes).Any(route => ReferenceEquals(route, found.Route));
    }
    private static bool ReadReply(QualitativeEvidenceReply reply, Relation? request, IElement port, IElement? revision, IElement node,
        out Relation fact, out IElement actualRevision, out Relation evidence)
    {
        fact = null!; actualRevision = null!; evidence = null!;
        if (reply.Surface is not { Left: Relation { Left: Relation actualRequest, Right: var actualPort },
                Right: Relation { Left: Relation publications, Right: Relation { Left: var version, Right: Relation proof } } } ||
            request is not null && !ReferenceEquals(actualRequest, request) || !N.Same(actualPort, port) ||
            revision is not null && !N.Same(version, revision) || !N.Read(publications, out var rows) ||
            rows is not [Relation { Right: Relation symbols } published] || !N.Same(published.Left, node) ||
            !StructuralConstraint.TryReadScope(symbols, out var alternatives, out _, out _) || !N.Distinct(alternatives) ||
            alternatives.Any(symbol => symbol is not Relation)) return false;
        fact = published; actualRevision = version; evidence = proof; return true;
    }
    private static bool Refines(Relation before, Relation after)
    {
        if (!StructuralConstraint.TryReadScope(before, out var prior, out var seam, out var tail)) return false;
        if (!seam.Equals(tail)) return true;
        return N.Read(after, out var current) && current.All(symbol => prior.Any(old => N.Same(old, symbol)));
    }
    private static bool Settled(Relation alternatives, IElement expected) => N.Read(alternatives, out var entries) &&
        (entries.Length == 1 || !entries.Any(symbol => N.Same(symbol, expected)));
    private static IElement[] Unique(IEnumerable<IElement> entries) => entries.Distinct(N.Participants.Instance).ToArray();
}
