using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;
using C = Gojo.Experiments.QualitativeFrieze.OccurrenceComposition;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite communication over advertised observation and mechanical
/// surfaces. Query names and fixture identities never choose a read. The bank
/// caches witnesses for the same immutable observation event and actual query;
/// it does not transfer answers to another execution.</summary>
public static class OrnamentReconstruction
{
    public sealed record Options(int MaxCycleEdges = 6, int ProposalCapacity = 2048,
        int MaxWalkEdges = 3, int RetainedCapacity = 1024,
        int AssignmentCapacity = 128, int CorrespondenceCapacity = 256);

    public sealed class Input(string name, QualitativeRecognitionObservation observation, Relation mechanicalGroups)
    {
        public string Name { get; } = name;
        public QualitativeRecognitionObservation Observation { get; } = observation;
        public Relation MechanicalGroups { get; } = mechanicalGroups;
        public Relation Surface { get; } = new(observation.Surface, mechanicalGroups);
    }

    /// <summary>Exactly one query in five ordered native publication scopes:
    /// visible motifs, whole mechanical paths, joint occurrences, declared
    /// continuations, declared crossings. These are separate exposed services.</summary>
    public sealed class Query
    {
        // query = motifs -> (whole paths -> (joint questions ->
        //         (continuation pattern pairs -> crossing pattern pairs)))
        public string Name { get; }
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => E(Blocked).Length == 0 && E(Open).Length == 0;
        public string Kind => Branch switch { 0 => "visible motif", 1 => "mechanical path",
            2 => "joint occurrence", 3 => "continuation", 4 => "crossing", _ => "unreadable" };
        internal int Branch { get; } = -1;
        internal Relation? Payload { get; }
        public Relation? Pattern => Branch is 0 or 1 ? Payload : null;
        public C.Question? JointQuestion => Branch == 2 && Payload is not null ? new(Name, Payload) : null;

        public static Query Motif(string name, CommunicationPattern pattern) => Make(name, 0, pattern.Surface);
        public static Query WholePath(string name, CommunicationPattern pattern) => Make(name, 1, pattern.Surface);
        public static Query Joint(string name, C.Question question) => Make(name, 2, question.Surface);
        public static Query Continuation(string name, CommunicationPattern first, CommunicationPattern second) =>
            Make(name, 3, new(first.Surface, second.Surface));
        public static Query Crossing(string name, CommunicationPattern first, CommunicationPattern second) =>
            Make(name, 4, new(first.Surface, second.Surface));

        private static Query Make(string name, int branch, Relation payload)
        {
            var scopes = Enumerable.Range(0, 5).Select(index => index == branch ? S(payload) : S()).ToArray();
            return new(name, new(scopes[0], new Relation(scopes[1], new Relation(scopes[2], new Relation(scopes[3], scopes[4])))));
        }

        public Query(string name, Relation surface)
        {
            Name = name; Surface = surface;
            var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation motifs, Right: Relation { Left: Relation paths,
                Right: Relation { Left: Relation joints, Right: Relation { Left: Relation continuations,
                    Right: Relation crossings } } } }) blocked.Add(surface);
            else
            {
                var scopes = new[] { motifs, paths, joints, continuations, crossings };
                var found = new List<(int Branch, Relation Payload)>();
                for (var index = 0; index < scopes.Length; index++)
                {
                    if (!Closed(scopes[index], out var rows)) open.Add(scopes[index]);
                    foreach (var item in E(scopes[index]))
                        if (item is Relation row) found.Add((index, row)); else blocked.Add(item);
                }
                if (found.Count != 1) blocked.Add(surface);
                else
                {
                    Branch = found[0].Branch; Payload = found[0].Payload;
                    if (Branch is 0 or 1)
                    { if (!CommunicationShape.TryRead(Payload, out _)) blocked.Add(Payload); }
                    else if (Branch == 2)
                    {
                        var joint = new C.Question(name, Payload);
                        blocked.AddRange(E(joint.Blocked)); open.AddRange(E(joint.Unresolved));
                    }
                    else if (Payload is not { Left: Relation first, Right: Relation second } ||
                        !CommunicationShape.TryRead(first, out var a) || !CommunicationShape.TryRead(second, out var b) ||
                        a.Edges.Length != 1 || b.Edges.Length != 1) blocked.Add(Payload);
                }
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Claim
    {
        // claim = actual query -> (Closed(positive query) -> Closed(negative query))
        public Query Query { get; }
        public bool Expected { get; }
        public string Answer => Expected ? "Yes" : "No";
        public Relation Surface { get; }
        public Claim(Query query, bool expected)
        {
            Query = query; Expected = expected;
            Surface = new(query.Surface, new Relation(expected ? S(query.Surface) : S(), expected ? S() : S(query.Surface)));
        }
        public Claim(Query query, string expected) : this(query,
            expected == "Yes" ? true : expected == "No" ? false :
            throw new ArgumentException("A sent claim commits to Yes or No; Open and Blocked are read outcomes.", nameof(expected))) { }
        internal Claim(Query query, bool expected, Relation surface) : this(query, expected) => Surface = surface;
    }

    public sealed class Message
    {
        public IReadOnlyList<Claim> Claims { get; }
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => E(Blocked).Length == 0 && E(Open).Length == 0;
        public int HintCount => Claims.Count;
        public int NativeCost => Participants(Surface).Count;
        public Message(IReadOnlyList<Claim> claims) : this(S(claims.Select(claim => (IElement)claim.Surface).ToArray())) { }
        public Message(Relation surface)
        {
            Surface = surface; var claims = new List<Claim>(); var blocked = new List<IElement>(); var open = new List<IElement>();
            if (!Closed(surface, out _)) open.Add(surface);
            foreach (var item in E(surface))
            {
                if (item is not Relation { Left: Relation querySurface,
                    Right: Relation { Left: Relation yes, Right: Relation no } } row ||
                    !Closed(yes, out var positive) || !Closed(no, out var negative) ||
                    !((positive.Length == 1 && negative.Length == 0 && ReferenceEquals(positive[0], querySurface)) ||
                      (negative.Length == 1 && positive.Length == 0 && ReferenceEquals(negative[0], querySurface))))
                { blocked.Add(item); continue; }
                var query = new Query("published query", querySurface);
                blocked.AddRange(E(query.Blocked)); open.AddRange(E(query.Open));
                var expected = positive.Length == 1;
                if (claims.Any(old => ReferenceEquals(old.Query.Surface, querySurface) && old.Expected != expected)) blocked.Add(row);
                if (claims.Any(old => ReferenceEquals(old.Query.Surface, querySurface) && old.Expected == expected)) blocked.Add(row);
                claims.Add(new(query, expected, row));
            }
            Claims = claims; Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Focus
    {
        // focus = Closed(actual query basis) ->
        //         (Closed(selected actual queries) -> Closed(carried complement))
        public Relation Surface { get; }
        public Relation Basis { get; } = S();
        public Relation Selected { get; } = S();
        public Relation Complement { get; } = S();
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => E(Blocked).Length == 0 && E(Open).Length == 0;
        public Focus(IReadOnlyList<Query> basis, IReadOnlyList<Query> selected) : this(new(
            S(basis.Select(query => (IElement)query.Surface).ToArray()), new Relation(
                S(selected.Select(query => (IElement)query.Surface).ToArray()),
                S(basis.Where(query => !selected.Any(actual => ReferenceEquals(actual.Surface, query.Surface)))
                    .Select(query => (IElement)query.Surface).ToArray())))) { }
        public Focus(Relation surface)
        {
            Surface = surface; var blocked = new List<IElement>(); var open = new List<IElement>();
            if (surface is not { Left: Relation basis, Right: Relation { Left: Relation selected, Right: Relation complement } }) blocked.Add(surface);
            else
            {
                Basis = basis; Selected = selected; Complement = complement;
                foreach (var scope in new[] { basis, selected, complement })
                { if (!Closed(scope, out _)) open.Add(scope); if (!N.Distinct(E(scope))) blocked.Add(scope); }
                foreach (var item in E(basis))
                {
                    if (item is not Relation query) { blocked.Add(item); continue; }
                    var read = new Query("focus query", query);
                    blocked.AddRange(E(read.Blocked)); open.AddRange(E(read.Open));
                }
                var chosen = E(selected); var carried = E(complement); var all = E(basis);
                if (chosen.Concat(carried).Any(item => !all.Any(actual => ReferenceEquals(actual, item))) ||
                    chosen.Any(item => carried.Any(actual => ReferenceEquals(actual, item))) ||
                    all.Any(item => !chosen.Concat(carried).Any(actual => ReferenceEquals(actual, item)))) blocked.Add(surface);
            }
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Context
    {
        // context = Closed(actual candidate preference) -> Closed(actual available queries)
        public IReadOnlyList<Input> Preference { get; }
        public IReadOnlyList<Query> Available { get; }
        public Relation Surface { get; }
        public Relation Blocked { get; }
        public Relation Open { get; }
        public bool IsValid => E(Blocked).Length == 0 && E(Open).Length == 0;
        public Context(IReadOnlyList<Input> preference, IReadOnlyList<Query> available) : this(new(
            S(preference.Select(input => (IElement)input.Surface).ToArray()),
            S(available.Select(query => (IElement)query.Surface).ToArray())), preference, available) { }
        public Context(Relation surface, IReadOnlyList<Input> inputs, IReadOnlyList<Query> queries)
        {
            Surface = surface; var blocked = new List<IElement>(); var open = new List<IElement>();
            var preference = new List<Input>(); var available = new List<Query>();
            if (surface is not { Left: Relation candidates, Right: Relation capabilities }) blocked.Add(surface);
            else
            {
                foreach (var scope in new[] { candidates, capabilities })
                { if (!Closed(scope, out _)) open.Add(scope); if (!N.Distinct(E(scope))) blocked.Add(scope); }
                foreach (var item in E(candidates))
                {
                    var found = inputs.FirstOrDefault(input => ReferenceEquals(input.Surface, item));
                    if (found is null) blocked.Add(item); else preference.Add(found);
                }
                foreach (var item in E(capabilities))
                {
                    var found = queries.FirstOrDefault(query => ReferenceEquals(query.Surface, item));
                    if (found is null) blocked.Add(item); else available.Add(found);
                }
            }
            Preference = preference; Available = available;
            Blocked = S(blocked.ToArray()); Open = S(open.ToArray());
        }
    }

    public sealed class Receipt
    {
        public Input Input { get; }
        public Query Query { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Budget { get; }
        public Relation Surface { get; }
        public int Work { get; }
        public string Answer => E(Blocked).Length != 0 ? "Blocked" : E(Open).Length != 0 ? "Open" :
            E(Supported).Length != 0 && E(Refuted).Length == 0 ? "Yes" :
            E(Refuted).Length != 0 && E(Supported).Length == 0 ? "No" : "Open";
        internal Receipt(Input input, Query query, Relation supported, Relation refuted, Relation open,
            Relation blocked, Relation budget, int work)
        {
            Input = input; Query = query; Supported = supported; Refuted = refuted;
            Open = open; Blocked = blocked; Budget = budget; Work = work;
            Surface = new(new Relation(input.Surface, query.Surface), new Relation(supported,
                new Relation(refuted, new Relation(open, new Relation(blocked, budget)))));
        }
    }

    public sealed class Candidate
    {
        public Input Input { get; }
        public IReadOnlyList<Receipt> Receipts { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public bool IsAdmissible => E(Refuted).Length == 0 && E(Open).Length == 0 && E(Blocked).Length == 0;
        internal Candidate(Input input, IReadOnlyList<Receipt> receipts, Relation supported, Relation refuted,
            Relation open, Relation blocked, Relation surface)
        { Input = input; Receipts = receipts; Supported = supported; Refuted = refuted; Open = open; Blocked = blocked; Surface = surface; }
    }

    public sealed class Selection
    {
        public Message Message { get; }
        public Context Context { get; }
        public Focus Focus { get; }
        public IReadOnlyList<Candidate> Candidates { get; }
        public IReadOnlyList<Candidate> Admissible { get; }
        public Input? Selected => Admissible.FirstOrDefault()?.Input;
        public Relation Surface { get; }
        internal Selection(Message message, Context context, Focus focus, IReadOnlyList<Candidate> candidates)
        {
            Message = message; Context = context; Focus = focus; Candidates = candidates;
            Admissible = candidates.Where(candidate => candidate.IsAdmissible).ToArray();
            Surface = new(new Relation(message.Surface, new Relation(context.Surface, focus.Surface)),
                new Relation(S(candidates.Select(candidate => (IElement)candidate.Surface).ToArray()),
                    S(Admissible.Select(candidate => (IElement)candidate.Input.Surface).ToArray())));
        }
    }

    public sealed class Bank
    {
        public IReadOnlyList<Input> Inputs { get; }
        public IReadOnlyList<Query> Queries { get; }
        public Options Configuration { get; }
        public Relation Surface { get; }
        public long EvaluatedReads { get; private set; }
        public long CachedReads { get; private set; }
        public long Inductions { get; private set; }
        private readonly Dictionary<Input, QualitativeMotifInduction> inductions = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<(Input Input, Relation Query), Receipt> cache = [];
        public Bank(IReadOnlyList<Input> inputs, IReadOnlyList<Query> queries, Options? options = null)
        {
            Inputs = inputs; Queries = queries; Configuration = options ?? new();
            if (inputs.Distinct(ReferenceEqualityComparer.Instance).Count() != inputs.Count ||
                queries.Select(query => query.Surface).Distinct(ReferenceEqualityComparer.Instance).Count() != queries.Count)
                throw new ArgumentException("A bank advertises each actual input and query once.");
            Surface = new(S(inputs.Select(input => (IElement)input.Surface).ToArray()),
                S(queries.Select(query => (IElement)query.Surface).ToArray()));
        }

        public Receipt Read(Input input, Query query, bool useCache = true)
        {
            if (!Inputs.Any(actual => ReferenceEquals(actual, input)) ||
                !Queries.Any(actual => ReferenceEquals(actual.Surface, query.Surface)))
                return new(input, query, S(), S(), S(), S(new Relation(Surface, new Relation(input.Surface, query.Surface))), S(), 0);
            if (useCache && cache.TryGetValue((input, query.Surface), out var retained)) { CachedReads++; return retained; }
            EvaluatedReads++;
            var result = Assess(input, query, Configuration, Induce);
            if (useCache) cache[(input, query.Surface)] = result;
            return result;

            QualitativeMotifInduction Induce()
            {
                if (useCache && inductions.TryGetValue(input, out var old)) return old;
                Inductions++;
                var made = new QualitativeMotifInduction(input.Observation, Configuration.MaxCycleEdges,
                    Configuration.ProposalCapacity, Configuration.MaxWalkEdges, Configuration.RetainedCapacity);
                if (useCache) inductions[input] = made;
                return made;
            }
        }

        public Selection Evaluate(Message message, Context context, Focus? focus = null, bool useCache = true)
        {
            focus ??= new Focus(message.Claims.Select(claim => claim.Query).ToArray(), message.Claims.Select(claim => claim.Query).ToArray());
            var results = new List<Candidate>();
            var globalBlocked = E(message.Blocked).Concat(E(context.Blocked)).Concat(E(focus.Blocked)).ToList();
            var globalOpen = E(message.Open).Concat(E(context.Open)).Concat(E(focus.Open)).ToList();
            if (context.Preference.Any(input => !Inputs.Any(actual => ReferenceEquals(actual, input))) ||
                context.Available.Any(query => !Queries.Any(actual => ReferenceEquals(actual.Surface, query.Surface)))) globalBlocked.Add(context.Surface);
            if (message.Claims.Any(claim => !E(focus.Basis).Any(query => ReferenceEquals(query, claim.Query.Surface)))) globalBlocked.Add(focus.Surface);
            foreach (var input in context.Preference)
            {
                var receipts = new List<Receipt>(); var yes = new List<IElement>(); var no = new List<IElement>();
                var open = new List<IElement>(globalOpen); var blocked = new List<IElement>(globalBlocked);
                foreach (var claim in message.Claims)
                {
                    // Omitted focus participation stays carried by Focus;
                    // it is not converted into a positive or negative claim.
                    if (!E(focus.Selected).Any(query => ReferenceEquals(query, claim.Query.Surface))) continue;
                    if (!context.Available.Any(query => ReferenceEquals(query.Surface, claim.Query.Surface)))
                    { open.Add(new Relation(claim.Surface, StructuralConstraint.Open(context.Surface))); continue; }
                    var receipt = Read(input, claim.Query, useCache); receipts.Add(receipt);
                    var evidence = new Relation(claim.Surface, receipt.Surface);
                    if (receipt.Answer == "Blocked") blocked.Add(evidence);
                    else if (receipt.Answer == "Open") open.Add(evidence);
                    else if ((receipt.Answer == "Yes") == claim.Expected) yes.Add(evidence); else no.Add(evidence);
                }
                var supported = S(yes.ToArray()); var refuted = S(no.ToArray()); var pending = S(open.ToArray()); var refused = S(blocked.ToArray());
                results.Add(new(input, receipts, supported, refuted, pending, refused,
                    new Relation(new Relation(input.Surface, new Relation(message.Surface, focus.Surface)),
                        new Relation(supported, new Relation(refuted, new Relation(pending, refused))))));
            }
            return new(message, context, focus, results);
        }
    }

    /// <summary>Extracts a reusable role description from one advertised
    /// mechanical group. Actual occurrence bindings are returned separately;
    /// the pattern contains fresh site/edge slots and no observed identity.</summary>
    public static bool TryDescribeGroup(Input input, Relation actualGroup,
        out CommunicationPattern pattern, out Relation correspondence)
    {
        pattern = null!; correspondence = null!;
        if (!E(input.MechanicalGroups).Any(group => ReferenceEquals(group, actualGroup)) ||
            actualGroup is not { Left: Placeholder, Right: Relation { Left: Relation scope, Right: Relation closure } } ||
            !Closed(scope, out var rows) || !Closed(closure, out _) || rows.Length == 0 || !N.Distinct(rows) ||
            rows.Any(row => row is not Relation edge || !input.Observation.TryReadSegment(edge, out _, out _, out _, out _)) ||
            !TryPathDefinition(input.Observation, rows.Cast<Relation>().ToArray(), out var definition, out correspondence)) return false;
        pattern = new(input.Observation.Frame, definition);
        return pattern.IsValid;
    }

    /// <summary>Describes an actual observed edge under fresh source-free
    /// roles. Its observed endpoints remain only in the separate witness.</summary>
    public static bool TryDescribeEdge(Input input, Relation actualEdge,
        out CommunicationPattern pattern, out Relation correspondence)
    {
        pattern = null!; correspondence = null!;
        if (!TryPathDefinition(input.Observation, [actualEdge], out var definition, out correspondence)) return false;
        pattern = new(input.Observation.Frame, definition); return pattern.IsValid;
    }

    private static Receipt Assess(Input input, Query query, Options options, Func<QualitativeMotifInduction> induce)
    {
        if (!query.IsValid) return new(input, query, S(), S(), query.Open, query.Blocked, S(), 0);
        if (query.Branch == 0)
        {
            var induction = induce(); var assessment = new CommunicationAssessment(induction, new(S(query.Payload!), S()));
            return new(input, query, assessment.Supported, assessment.Refuted, assessment.Unresolved, assessment.Blocked,
                induction.Budget, E(induction.Consumed).Length);
        }
        if (query.Branch == 2)
        {
            var assessment = new C.Assessment(induce(), query.JointQuestion!, options.AssignmentCapacity, options.CorrespondenceCapacity);
            return new(input, query, assessment.Supported, assessment.Refuted, assessment.Open, assessment.Blocked,
                new(assessment.Budget, assessment.CorrespondenceBudget), E(assessment.Consumed).Length + E(assessment.CorrespondenceConsumed).Length);
        }
        return query.Branch == 1 ? AssessMechanical(input, query) : AssessRelation(input, query);
    }

    private static Receipt AssessRelation(Input input, Query query)
    {
        var observation = input.Observation; var scope = query.Branch == 3 ? observation.Continuations : observation.Crossings;
        var yes = new List<IElement>(); var blocked = new List<IElement>(); var open = new List<IElement>();
        if (!Closed(scope, out _)) open.Add(scope);
        var pair = query.Payload!; var firstPattern = (Relation)pair.Left; var secondPattern = (Relation)pair.Right;
        var work = 0;
        foreach (var item in E(scope))
        {
            work++;
            Relation first, second;
            if (item is not Relation row) { blocked.Add(item); continue; }
            var valid = query.Branch == 3 ? observation.TryReadContinuation(row, out _, out first!, out second!) :
                observation.TryReadCrossing(row, out first!, out second!);
            if (!valid) { blocked.Add(row); continue; }
            // This question deliberately reads an unordered encounter between
            // the two described lines. The original ordered publication row
            // remains in the witness for a stricter directed read; this does
            // not establish foreground, over/under or ordered transfer.
            if (SingleEdgeMatches(observation, firstPattern, first, out var a) && SingleEdgeMatches(observation, secondPattern, second, out var b) ||
                SingleEdgeMatches(observation, firstPattern, second, out a) && SingleEdgeMatches(observation, secondPattern, first, out b))
                yes.Add(new Relation(row, new Relation(a, b)));
        }
        if (yes.Count != 0) open.Clear();
        var no = yes.Count == 0 && open.Count == 0 && blocked.Count == 0 ? S(new Relation(query.Surface, scope)) : S();
        return new(input, query, S(yes.ToArray()), no, S(open.ToArray()), S(blocked.ToArray()), S(), work);
    }

    private static Receipt AssessMechanical(Input input, Query query)
    {
        var observation = input.Observation; var scope = input.MechanicalGroups;
        var yes = new List<IElement>(); var blocked = new List<IElement>(); var open = new List<IElement>();
        if (!Closed(scope, out _)) open.Add(scope);
        CommunicationShape.TryRead(query.Payload!, out var pattern);
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = 0;
        foreach (var item in E(scope))
        {
            work++;
            if (item is not Relation { Left: Placeholder groupIdentity, Right: Relation { Left: Relation edges, Right: Relation closure } } group ||
                !seen.Add(groupIdentity) || !Closed(edges, out var rows) || !Closed(closure, out var closures) ||
                rows.Length == 0 || !N.Distinct(rows) || rows.Any(row => row is not Relation edge || !observation.TryReadSegment(edge, out _, out _, out _, out _)))
            { blocked.Add(item); continue; }
            // Membership and ordering are checked against the advertised group.
            // No author's path name, control program or measured word is read.
            if (!TryPathDefinition(observation, rows.Cast<Relation>().ToArray(), out var definition, out var correspondence))
            { blocked.Add(group); continue; }
            var bindings = E((Relation)correspondence.Left).Cast<Relation>().ToArray();
            var firstSite = bindings[0].Right;
            var lastUse = (Relation)E((Relation)((Relation)definition.Right).Right).FirstOrDefault()!;
            IElement lastSite;
            if (rows.Length == 1)
            {
                observation.TryReadSegment((Relation)rows[0], out _, out _, out lastSite!, out _);
            }
            else
            {
                var final = (Relation)E(lastUse)[^1];
                var finalRole = ((Relation)final.Right).Right;
                lastSite = bindings.Single(binding => ReferenceEquals(binding.Left, finalRole)).Right;
            }
            if (closures.Any(item => item is not Relation closureRow ||
                !N.Same(closureRow.Left, firstSite) || !N.Same(closureRow.Right, lastSite) ||
                !N.Same(firstSite, lastSite)) || closures.Length > 1)
            { blocked.Add(group); continue; }
            if (!CommunicationShape.TryRead(new(observation.Frame.Surface, definition), out var target)) continue;
            if (!pattern.TryMap(target, out var sites, out var edgeMap)) continue;
            // A mechanical closure promise requires actual end-to-start site
            // identity. Merely publishing a closure row cannot create it.
            if (pattern.IsCycle && E(closure).Length == 0) continue;
            yes.Add(new Relation(group, new Relation(new Relation(query.Payload!, definition),
                new Relation(correspondence, new Relation(sites, edgeMap)))));
        }
        if (yes.Count != 0) open.Clear();
        var no = yes.Count == 0 && blocked.Count == 0 && open.Count == 0 ? S(new Relation(query.Surface, scope)) : S();
        return new(input, query, S(yes.ToArray()), no, S(open.ToArray()), S(blocked.ToArray()), S(), work);
    }

    private static bool SingleEdgeMatches(QualitativeRecognitionObservation observation, Relation patternSurface, Relation edge, out Relation witness)
    {
        witness = null!;
        if (!CommunicationShape.TryRead(patternSurface, out var pattern)) return false;
        foreach (var reverse in new[] { false, true })
        {
            if (!TryPathDefinition(observation, [edge], out var definition, out var correspondence, reverse) ||
                !CommunicationShape.TryRead(new(observation.Frame.Surface, definition), out var target) ||
                !pattern.TryMap(target, out var sites, out var edges)) continue;
            witness = new(new Relation(patternSurface, edge), new Relation(correspondence, new Relation(sites, edges))); return true;
        }
        return false;
    }

    private static bool TryPathDefinition(QualitativeRecognitionObservation observation, Relation[] edges,
        out Relation definition, out Relation correspondence, bool reverseFirst = false)
    {
        definition = correspondence = null!;
        var sites = new List<IElement>(); var roles = new List<IElement>(); var rows = new List<IElement>(); var uses = new List<IElement>();
        var siteBindings = new List<IElement>(); var edgeBindings = new List<IElement>();
        IElement? previous = null; IElement? firstSite = null;
        foreach (var edge in edges)
        {
            if (!observation.TryReadSegment(edge, out _, out var from, out var to, out var heading) || !Closed(heading, out var directions)) return false;
            var reverse = previous is null ? reverseFirst : N.Same(previous, to) && !N.Same(previous, from);
            if (previous is not null && !N.Same(previous, from) && !N.Same(previous, to)) return false;
            var actualFrom = reverse ? to : from; var actualTo = reverse ? from : to;
            firstSite ??= actualFrom; var a = Role(actualFrom); var b = Role(actualTo); var edgeRole = new Placeholder(); var ends = new Relation(a, b);
            var orientedHeading = reverse ? S(directions.Select(direction => Opposite(observation, direction)).ToArray()) : heading;
            rows.Add(new Relation(edgeRole, new Relation(ends, orientedHeading))); uses.Add(new Relation(edgeRole, ends));
            edgeBindings.Add(new Relation(edgeRole, edge)); previous = actualTo;
        }
        if (edges.Length == 0) return false;
        var centres = edges.Length == 1 ? S(Role(firstSite!)) : S();
        definition = new(new Relation(observation.Frame.Surface, new Relation(S(roles.ToArray()), S(rows.ToArray()))),
            new Relation(centres, edges.Length == 1 ? S() : S(S(uses.ToArray()))));
        correspondence = new(S(siteBindings.ToArray()), S(edgeBindings.ToArray())); return true;

        IElement Role(IElement actual)
        {
            var index = sites.FindIndex(site => N.Same(site, actual));
            if (index >= 0) return roles[index];
            var role = new Placeholder(); sites.Add(actual); roles.Add(role); siteBindings.Add(new Relation(role, actual)); return role;
        }
    }

    private static IElement Opposite(QualitativeRecognitionObservation observation, IElement direction) =>
        E(observation.Frame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, direction)).Right;
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static bool Closed(Relation scope, out IElement[] entries) => QualitativeRecognitionObservation.Closed(scope, out entries);
    public static HashSet<IElement> Participants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item) && item is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }
}
