using Gojo.Core;
using Gojo.Patterns;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite demand-driven reads of explicitly chosen conjunctive
/// construction contracts. A false premise refutes this candidate recipe;
/// it does not establish that no other construction can satisfy the goal.
/// These rules are neither general material implication nor geometric axioms.</summary>
public static class StructuralEvidenceRules
{
    // rule = Closed(actual premise predicates) -> actual conclusion predicate
    // program = Closed(rules)
    // publication = actual source port -> Closed(actual evidence surfaces)
    // node = predicate -> ((source evidence -> rule scope) ->
    //        (child reads -> (supported -> (refuted -> (conflicts ->
    //        (missing -> (unasked -> blocked)))))))
    // receipt = ((program -> publication) -> goal) ->
    //   (root read -> (supported -> (refuted -> (conflicts -> (missing ->
    //   (visited -> (unasked -> (blocked -> (budget ->
    //   (consumed -> (budget cuts -> open))))))))))))
    // Predicate identity is exact native alias identity. A closed publication
    // inventory is not a closed world of true/false predicates.

    public sealed class Rule
    {
        public string Name { get; }
        public Relation Conclusion { get; } = null!;
        public IReadOnlyList<Relation> Premises { get; } = [];
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public bool IsValid => Empty(Blocked);

        public Rule(string name, Relation conclusion, IReadOnlyList<Relation> premises)
            : this(name, new(S(premises.Cast<IElement>().ToArray()), conclusion)) { }

        public Rule(string name, Relation surface)
        {
            Name = name; Surface = surface;
            var blocked = new List<IElement>();
            if (surface is not { Left: Relation premiseScope, Right: Relation conclusion } ||
                !Closed(premiseScope, out var rows) || rows.Any(row => row is not Relation))
                blocked.Add(surface);
            else
            { Conclusion = conclusion; Premises = rows.Cast<Relation>().ToArray(); }
            Blocked = S(blocked.ToArray());
        }
    }

    public sealed class Program
    {
        public IReadOnlyList<Rule> Rules { get; }
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public bool IsValid => Empty(Blocked);

        /// <summary>An explicitly declared empty-premise rule is a vacuous
        /// construction contract for its actual candidate conclusion. It does
        /// not assert a universally existing object. An empty program supplies
        /// no rule or default fact for an unknown goal.</summary>
        public Program(IReadOnlyList<Rule> rules) : this(S(rules.Select(rule => (IElement)rule.Surface).ToArray())) { }

        public Program(Relation surface)
        {
            Surface = surface;
            var blocked = new List<IElement>(); var rules = new List<Rule>();
            if (!Closed(surface, out var rows)) blocked.Add(surface);
            foreach (var item in E(surface))
                if (item is not Relation raw) blocked.Add(item);
                else
                {
                    var rule = new Rule("native conjunctive contract", raw);
                    rules.Add(rule); blocked.AddRange(E(rule.Blocked));
                }
            Rules = rules;
            var valid = rules.Where(rule => rule.IsValid).ToArray();
            foreach (var rule in valid)
                if (valid.Count(other => ReferenceEquals(other.Conclusion, rule.Conclusion)) != 1)
                    blocked.Add(rule.Surface);
            var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            var done = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            foreach (var rule in valid) Visit(rule);
            Blocked = S(blocked.ToArray());

            void Visit(Rule rule)
            {
                if (done.Contains(rule.Conclusion)) return;
                if (!active.Add(rule.Conclusion)) { blocked.Add(rule.Surface); return; }
                foreach (var premise in rule.Premises)
                    foreach (var dependency in valid.Where(other => ReferenceEquals(other.Conclusion, premise)))
                        Visit(dependency);
                active.Remove(rule.Conclusion); done.Add(rule.Conclusion);
            }
        }

        public static bool TryRead(Relation surface, out Program program)
        { program = new(surface); return program.IsValid; }

        public static bool TryRead(Relation surface, out Program program, out Relation witness)
        { var read = TryRead(surface, out program); witness = new(surface, program.Blocked); return read; }
    }

    public sealed class Publication
    {
        public Relation Source { get; } = null!;
        public IReadOnlyList<D.Evidence> Evidence { get; } = [];
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public bool IsValid => Empty(Blocked);

        public Publication(Relation source, IReadOnlyList<D.Evidence> evidence)
            : this(new(source, S(evidence.Select(fact => (IElement)fact.Surface).ToArray()))) { }

        public Publication(Relation surface)
        {
            Surface = surface; var blocked = new List<IElement>(); var facts = new List<D.Evidence>();
            if (surface is not { Left: Relation source, Right: Relation inventory } || !Closed(inventory, out _))
                blocked.Add(surface);
            else
            {
                Source = source;
                foreach (var item in E(inventory))
                    if (item is not Relation raw) blocked.Add(item);
                    else
                    {
                        var fact = new D.Evidence(raw);
                        if (fact.Predicate is null) blocked.Add(raw); else facts.Add(fact);
                    }
            }
            Evidence = facts; Blocked = S(blocked.ToArray());
        }

        public static bool TryRead(Relation surface, out Publication publication)
        { publication = new(surface); return publication.IsValid; }

        public static bool TryRead(Relation surface, out Publication publication, out Relation witness)
        { var read = TryRead(surface, out publication); witness = new(surface, publication.Blocked); return read; }
    }

    /// <summary>An explicitly exposed observational service. The callback
    /// reads this public predicate contract; it is not an inferred capability
    /// or an instruction to inspect another system's private state. Capacity
    /// slots are allocated independently in each explicit Read application.</summary>
    public sealed class Service
    {
        // service = source -> (port -> (revision -> (predicates -> capacity)))
        public Relation Source { get; }
        public Relation Port { get; }
        public IElement Revision { get; }
        public Relation Predicates { get; }
        public Relation Capacity { get; }
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public bool IsValid => Empty(Blocked);
        internal Func<Request, Reply> Handler { get; }

        public Service(Relation source, Relation port, IElement revision, Relation predicates,
            int capacity, Func<Request, Reply> handler)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            ArgumentNullException.ThrowIfNull(handler);
            Source = source; Port = port; Revision = revision; Predicates = predicates; Handler = handler;
            Capacity = S(Enumerable.Range(0, capacity).Select(_ => (IElement)new Placeholder()).ToArray());
            Surface = new(source, new Relation(port, new Relation(revision, new Relation(predicates, Capacity))));
            Blocked = Closed(predicates, out var values) && values.All(value => value is Relation) &&
                values.Distinct<IElement>(ReferenceEqualityComparer.Instance).Count() == values.Length
                ? S() : S(predicates);
        }
    }

    public sealed class Request
    {
        // request = context -> ((source -> port) -> (revision -> predicate))
        public Relation Source { get; }
        public Relation Port { get; }
        public IElement Revision { get; }
        public Relation Predicate { get; }
        public Relation Context { get; }
        public Relation Surface { get; }

        public Request(Relation source, Relation port, IElement revision, Relation predicate, Relation context)
        {
            Source = source; Port = port; Revision = revision; Predicate = predicate; Context = context;
            Surface = new(context, new Relation(new Relation(source, port), new Relation(revision, predicate)));
        }
    }

    public sealed class Reply
    {
        // reply = actual request surface -> actual evidence surface
        public Relation Surface { get; }
        public Reply(Request request, D.Evidence evidence) : this(new(request.Surface, evidence.Surface)) { }
        public Reply(Relation surface) => Surface = surface;
    }

    public sealed class Receipt
    {
        public Relation Goal { get; }
        public Publication Source { get; }
        public Program Program { get; }
        public string Answer { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Conflicts { get; }
        public Relation Missing { get; }
        public Relation Visited { get; }
        public Relation Unasked { get; }
        public Relation Blocked { get; }
        public Relation Budget { get; }
        public Relation Consumed { get; }
        public Relation BudgetCuts { get; }
        public Relation Open { get; }
        public Publication ObservedPublication { get; }
        public Relation ServiceClaims { get; }
        public Relation ServiceRequests { get; }
        public Relation ServiceAccepted { get; }
        public Relation ServiceBlocked { get; }
        public Relation ServiceOpen { get; }
        public Relation ServiceConsumed { get; }
        public Relation Surface { get; }

        /// <summary>Publishes this selected read for an explicit downstream
        /// evidence connection. Complete root polarity proofs remain separate;
        /// an inherited child conflict is Open evidence for the root, not two
        /// invented proofs of the conjunction. The complete receipt remains
        /// reachable through every emitted proof route.</summary>
        public D.Evidence ToEvidence()
        {
            var positive = Empty(Supported) ? S() : S(new Relation(Surface, Supported));
            var negative = Empty(Refuted) ? S() : S(new Relation(Surface, Refuted));
            var inheritedConflict = Answer == "Conflict" && (Empty(Supported) || Empty(Refuted));
            var pending = Answer == "Open" || inheritedConflict
                ? S(new Relation(Surface, new Relation(Conflicts, new Relation(Open, new Relation(Missing, BudgetCuts))))) : S();
            var refused = Empty(Blocked) ? S() : S(new Relation(Surface, Blocked));
            return new(Goal, positive, negative, pending, refused);
        }

        internal Receipt(Program program, Publication source, Relation goal, Node root,
            IReadOnlyList<Relation> visited, IReadOnlyList<Relation> unasked,
            Relation budget, Relation consumed, Publication observed, Relation serviceClaims,
            Relation serviceRequests, Relation serviceAccepted, Relation serviceBlocked, Relation serviceOpen,
            Relation serviceConsumed)
        {
            Goal = goal; Source = source; Program = program; Answer = root.Answer;
            Supported = root.Supported; Refuted = root.Refuted; Conflicts = root.Conflicts;
            Missing = root.Missing; Blocked = root.Blocked; Visited = S(visited.Cast<IElement>().ToArray());
            Unasked = S(unasked.Distinct<Relation>(ReferenceEqualityComparer.Instance).Cast<IElement>().ToArray());
            Budget = budget; Consumed = consumed; BudgetCuts = root.BudgetCuts; Open = root.Open;
            ObservedPublication = observed; ServiceClaims = serviceClaims; ServiceRequests = serviceRequests;
            ServiceAccepted = serviceAccepted; ServiceBlocked = serviceBlocked; ServiceOpen = serviceOpen;
            ServiceConsumed = serviceConsumed;
            var services = new Relation(serviceClaims, new Relation(serviceRequests, new Relation(serviceAccepted,
                new Relation(serviceBlocked, new Relation(serviceOpen, serviceConsumed)))));
            var tail = new Relation(Budget, new Relation(Consumed, new Relation(BudgetCuts,
                new Relation(Open, new Relation(observed.Surface, services)))));
            Surface = new(new Relation(new Relation(program.Surface, source.Surface), goal),
                new Relation(root.Surface, new Relation(Supported, new Relation(Refuted, new Relation(Conflicts,
                    new Relation(Missing, new Relation(Visited, new Relation(Unasked, new Relation(Blocked,
                        tail)))))))));
        }
    }

    /// <summary>Reads only obligations reachable from the requested goal.
    /// Explicit premise order controls refinement. A decisive negative,
    /// contradiction or blocked read leaves later premises unasked. Unknown
    /// facts and finite query cuts remain Open; no default fact is supplied.</summary>
    public static Receipt Read(Program program, Publication source, Relation goal, int queryCapacity = 64) =>
        ReadCore(program, source, goal, null, queryCapacity);

    /// <summary>Queries only reached leaves that have no source account.
    /// Existing Open, conflicting or negative accounts are never overwritten
    /// by a service reply. Derived conclusions remain owned by their rules.</summary>
    public static Receipt Read(Program program, Publication source, Relation goal, Service service, int queryCapacity = 64) =>
        ReadCore(program, source, goal, service, queryCapacity);

    private static Receipt ReadCore(Program program, Publication source, Relation goal, Service? service, int queryCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(queryCapacity);
        var cache = new Dictionary<Relation, Node>(ReferenceEqualityComparer.Instance);
        var visited = new List<Relation>(); var unasked = new List<Relation>(); var consumed = new List<IElement>();
        var budget = S(Enumerable.Range(0, queryCapacity).Select(_ => (IElement)new Placeholder()).ToArray());
        var slots = E(budget);
        var observedFacts = source.Evidence.ToList();
        var requests = new List<IElement>(); var accepted = new List<IElement>(); var refused = new List<IElement>();
        var serviceOpen = new List<IElement>(); var serviceConsumed = new List<IElement>();
        var serviceSlots = service is null ? [] : E(service.Capacity);
        var context = new Relation(new Relation(program.Surface, source.Surface),
            new Relation(goal, service?.Surface ?? S()));
        var root = !program.IsValid || !source.IsValid
            ? new Node(goal, S(), S(), [], S(), S(), S(), S(), S(),
                S(E(program.Blocked).Concat(E(source.Blocked)).ToArray()))
            : Visit(goal);
        var observed = service is null || !source.IsValid ? source : new Publication(source.Source, observedFacts);
        return new(program, source, goal, root, visited, unasked, budget, S(consumed.ToArray()), observed,
            service is null ? S() : S(service.Surface), S(requests.ToArray()), S(accepted.ToArray()),
            S(refused.ToArray()), S(serviceOpen.ToArray()), S(serviceConsumed.ToArray()));

        Node Visit(Relation predicate)
        {
            if (cache.TryGetValue(predicate, out var retained)) return retained;
            if (visited.Count == slots.Length)
            {
                unasked.Add(predicate);
                return new(predicate, S(), S(), [], S(), S(), S(),
                    S(), S(predicate), S(), S(predicate), S(new Relation(predicate, budget)));
            }
            consumed.Add(new Relation(slots[visited.Count], predicate)); visited.Add(predicate);
            var rule = program.Rules.SingleOrDefault(rule => ReferenceEquals(rule.Conclusion, predicate));
            var facts = observedFacts.Where(fact => ReferenceEquals(fact.Predicate, predicate)).ToArray();
            var rejectedService = S();
            if (rule is null && facts.Length == 0 && service is not null)
            {
                rejectedService = QueryService(predicate);
                facts = observedFacts.Where(fact => ReferenceEquals(fact.Predicate, predicate)).ToArray();
            }
            var factScope = S(facts.Select(fact => (IElement)fact.Surface).ToArray());
            var sourceYes = facts.SelectMany(fact => E(fact.Supported)).ToArray();
            var sourceNo = facts.SelectMany(fact => E(fact.Refuted)).ToArray();
            var sourceOpen = facts.SelectMany(fact => E(fact.Open)).ToArray();
            var sourceBlocked = facts.SelectMany(fact => E(fact.Blocked)).ToArray();
            var children = new List<Node>(); var pending = new List<IElement>(); var skipped = new List<IElement>();
            if (rule is not null)
                for (var index = 0; index < rule.Premises.Count; index++)
                {
                    var child = Visit(rule.Premises[index]); children.Add(child);
                    if (child.Answer is not ("No" or "Conflict" or "Blocked")) continue;
                    foreach (var remaining in rule.Premises.Skip(index + 1))
                    { skipped.Add(remaining); unasked.Add(remaining); }
                    break;
                }
            var basis = new Relation(predicate, new Relation(factScope,
                new Relation(rule?.Surface ?? S(), S(children.Select(child => (IElement)child.Surface).ToArray()))));
            var derived = rule is null ? null : children.Any(child => child.Answer == "Blocked") ? "Blocked" :
                children.Any(child => child.Answer == "Conflict") ? "Conflict" :
                children.Any(child => child.Answer == "No") ? "No" :
                children.Any(child => child.Answer == "Open") ? "Open" : "Yes";
            var sourceConflict = sourceYes.Length != 0 && sourceNo.Length != 0;
            var yes = sourceYes.Length != 0 && (sourceOpen.Length == 0 || sourceConflict) || derived == "Yes";
            var no = sourceNo.Length != 0 && (sourceOpen.Length == 0 || sourceConflict) || derived == "No";
            var blocked = sourceBlocked.Length != 0 || derived == "Blocked" || !Empty(rejectedService);
            var conflict = sourceConflict || yes && no || derived == "Conflict";
            // A supplied open assessment is not completed by a concurrent
            // positive/negative assessment. A proved construction can discharge
            // its own missing premise only through its actual child reads.
            var open = sourceOpen.Length != 0 || derived == "Open";
            if (rule is null && (facts.Length == 0 || sourceOpen.Length != 0)) pending.Add(predicate);
            pending.AddRange(children.SelectMany(child => E(child.Missing)));
            var cuts = S(children.SelectMany(child => E(child.BudgetCuts)).Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray());
            var node = new Node(predicate, factScope, rule is null ? S() : S(rule.Surface), children,
                yes ? S(basis) : S(), no ? S(basis) : S(), conflict ? S(basis) : S(),
                S(pending.Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray()),
                S(skipped.ToArray()), blocked ? S(new Relation(basis, rejectedService)) : S(), cuts, open ? S(basis) : S());
            cache.Add(predicate, node); return node;
        }

        Relation QueryService(Relation predicate)
        {
            var request = new Request(service!.Source, service.Port, service.Revision, predicate, context);
            requests.Add(request.Surface);
            if (!service.IsValid || !ReferenceEquals(source.Source, service.Source))
                return Reject(new Relation(request.Surface, new Relation(source.Surface, service.Surface)));
            if (!E(service.Predicates).Any(actual => ReferenceEquals(actual, predicate)))
            {
                serviceOpen.Add(new Relation(request.Surface, service.Predicates)); return S();
            }
            if (serviceConsumed.Count == serviceSlots.Length)
            {
                serviceOpen.Add(new Relation(request.Surface, new Relation(service.Capacity, S(serviceConsumed.ToArray()))));
                return S();
            }
            var allocation = new Relation(serviceSlots[serviceConsumed.Count], request.Surface);
            serviceConsumed.Add(allocation);
            var reply = service.Handler(request);
            if (reply is null)
                return Reject(new Relation(request.Surface, new Relation(allocation, StructuralConstraint.Open())));
            if (reply.Surface is not { Left: var actualRequest, Right: Relation raw } ||
                !ReferenceEquals(actualRequest, request.Surface))
                return Reject(new Relation(request.Surface, new Relation(allocation, reply.Surface)));
            var evidence = new D.Evidence(raw);
            if (!ReferenceEquals(evidence.Predicate, predicate))
                return Reject(new Relation(request.Surface, new Relation(allocation, reply.Surface)));
            var receipt = new Relation(request.Surface, new Relation(allocation, reply.Surface));
            // A reusable observation retains its actual admission route even
            // when separated from this evaluation receipt. The provider's raw
            // evidence and complete request remain nested in that route.
            var admitted = new D.Evidence(predicate, Through(evidence.Supported), Through(evidence.Refuted),
                Through(evidence.Open), Through(evidence.Blocked));
            observedFacts.Add(admitted); accepted.Add(receipt);
            if (evidence.Answer == "Open") serviceOpen.Add(receipt);
            return S();

            Relation Through(Relation scope) => S(E(scope).Select(proof => (IElement)new Relation(receipt, proof)).ToArray());
            Relation Reject(Relation witness) { refused.Add(witness); return S(witness); }
        }
    }

    internal sealed class Node
    {
        internal Relation Surface { get; }
        internal Relation Supported { get; }
        internal Relation Refuted { get; }
        internal Relation Conflicts { get; }
        internal Relation Missing { get; }
        internal Relation Blocked { get; }
        internal Relation BudgetCuts { get; }
        internal Relation Open { get; }
        internal string Answer => !Empty(Blocked) ? "Blocked" : !Empty(Conflicts) ? "Conflict" :
            !Empty(Refuted) ? "No" : !Empty(Missing) || !Empty(BudgetCuts) || !Empty(Open) ? "Open" :
            !Empty(Supported) ? "Yes" : "Open";

        internal Node(Relation predicate, Relation facts, Relation rules, IReadOnlyList<Node> children,
            Relation supported, Relation refuted, Relation conflicts, Relation missing, Relation unasked, Relation blocked,
            Relation? budgetCuts = null, Relation? open = null)
        {
            Supported = supported; Refuted = refuted; Conflicts = conflicts; Missing = missing; Blocked = blocked;
            BudgetCuts = budgetCuts ?? S(); Open = open ?? S();
            Surface = new(predicate, new Relation(new Relation(facts, rules),
                new Relation(S(children.Select(child => (IElement)child.Surface).ToArray()), new Relation(supported,
                    new Relation(refuted, new Relation(conflicts, new Relation(missing,
                        new Relation(unasked, new Relation(blocked, new Relation(BudgetCuts, Open))))))))));
        }
    }

    private static Relation S(params IElement[] items) => StructuralConstraint.Closed(items);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static bool Closed(Relation scope, out IElement[] rows) =>
        StructuralConstraint.TryReadScope(scope, out rows, out var seam, out var tail) && ReferenceEquals(seam, tail);
    private static bool Empty(Relation scope) => E(scope).Length == 0;
}
