using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A finite exposed graph of token occurrences. A node's symbol scope
/// contains alternative possible native definitions, not concurrent assertions.
/// Exact symbol identity is the selected correspondence law; this reader does
/// not silently identify independently constructed isomorphic definitions.</summary>
public sealed class QualitativePatternTokenGraph
{
    // node = actual occurrence identity -> Open/Closed(possible definitions)
    // link = actual proof identity -> (actual from identity -> actual to identity)
    public Relation Nodes { get; }
    public Relation Links { get; }
    public Relation Branching { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
    private readonly Dictionary<IElement, Relation> nodeIndex = new(N.Participants.Instance);
    private readonly HashSet<Relation> links = new(ReferenceEqualityComparer.Instance);

    public QualitativePatternTokenGraph(Relation nodes, Relation links)
    {
        Nodes = nodes; Links = links;
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        if (!N.Read(nodes, out var rows)) pending.Add(nodes);
        var duplicated = rows.OfType<Relation>().GroupBy(row => row.Left, N.Participants.Instance)
            .Where(group => group.Count() > 1).SelectMany(group => group).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var item in rows)
        {
            if (item is not Relation { Right: Relation possibilities } row || duplicated.Contains(row) ||
                !StructuralConstraint.TryReadScope(possibilities, out var definitions, out var seam, out var tail) ||
                !N.Distinct(definitions) || definitions.Any(definition => definition is not Relation))
            { blocked.Add(item); continue; }
            nodeIndex.Add(row.Left, row);
            if (!seam.Equals(tail)) pending.Add(row);
        }
        if (!N.Read(links, out var linkRows)) pending.Add(links);
        var duplicateProofs = linkRows.OfType<Relation>().GroupBy(row => row.Left, N.Participants.Instance)
            .Where(group => group.Count() > 1).SelectMany(group => group).ToHashSet(ReferenceEqualityComparer.Instance);
        foreach (var item in linkRows)
        {
            if (item is not Relation { Right: Relation endpoints } row || duplicateProofs.Contains(row) ||
                !nodeIndex.ContainsKey(endpoints.Left) || !nodeIndex.ContainsKey(endpoints.Right) || N.Same(endpoints.Left, endpoints.Right))
            { blocked.Add(item); continue; }
            this.links.Add(row);
        }
        Branching = N.S(nodeIndex.Keys.Select(node => new Relation(node,
                N.S(this.links.Where(link => link.Right is Relation pair && N.Same(pair.Left, node)).Cast<IElement>().ToArray())))
            .Where(row => N.E((Relation)row.Right).Length > 1).Cast<IElement>().ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Surface = new(new Relation(nodes, links), new Relation(Branching, new Relation(Blocked, Unresolved)));
    }

    public bool TryFindNode(IElement identity, out Relation row, out Relation symbols)
    {
        symbols = null!;
        if (!nodeIndex.TryGetValue(identity, out row!)) return false;
        symbols = (Relation)row.Right; return true;
    }
    public bool TryReadNode(Relation actual, out IElement identity, out Relation symbols)
    {
        identity = null!; symbols = null!;
        if (!TryFindNode(actual.Left, out var row, out symbols) || !ReferenceEquals(actual, row)) return false;
        identity = row.Left; return true;
    }
    public bool TryReadLink(Relation actual, out IElement from, out IElement to)
    {
        from = to = null!;
        if (!links.Contains(actual) || actual.Right is not Relation pair) return false;
        from = pair.Left; to = pair.Right; return true;
    }
    internal Relation[] Between(IElement from, IElement to) => links.Where(row => row.Right is Relation pair &&
        N.Same(pair.Left, from) && N.Same(pair.Right, to)).ToArray();
}

/// <summary>Induces bounded finite phase cycles from two actual traversals in
/// training paths. The reusable phase graph is frozen before held-out reading.
/// Input coordinates, headings and fixture labels are absent from this API.</summary>
public sealed class QualitativePatternGrammar
{
    // pattern = Closed(phase identity -> expected definition) ->
    //           Closed(phase identity -> successor identity)
    // seed = (actual training path -> actual selected window) ->
    //        (actual link proofs -> Closed(phase row -> (first node -> second node)))
    // candidate = (training definition -> pattern) -> Closed(actual two-cycle seeds)
    // definition = (focused training graph -> selected training paths) -> budget
    // budget = Closed(max period roles) -> (Closed(seed slots) -> Closed(retention slots))
    public QualitativePatternTokenGraph Graph { get; }
    public Relation TrainingPaths { get; }
    public Relation TrainingNodes { get; }
    public Relation TrainingSurface { get; }
    public Relation Budget { get; }
    public Relation Definition { get; }
    public Relation Candidates { get; }
    public Relation FrozenCandidates { get; }
    public Relation Spent { get; }
    public Relation Unsearched { get; }
    public Relation RejectedSeeds { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Branching { get; }
    public Relation Surface { get; }
    public IReadOnlyList<QualitativePatternGrammarCandidate> Grammars { get; }
    public QualitativePatternGrammarEvaluation Training { get; }
    public bool IsComplete => N.E(Unsearched).Length == 0 && N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
    internal IReadOnlyList<PatternPath> Paths { get; }

    private sealed class Seed(PatternPath path, Relation[] rows, Relation[] links, int rotation)
    { public PatternPath Path { get; } = path; public Relation[] Rows { get; } = rows; public Relation[] Links { get; } = links; public int Rotation { get; } = rotation; }
    private sealed class Proposal(IElement[] symbols, IElement retention)
    { public IElement[] Symbols { get; } = symbols; public IElement Retention { get; } = retention; public List<Seed> Seeds { get; } = []; }

    public QualitativePatternGrammar(QualitativePatternTokenGraph graph, Relation trainingPaths,
        int maxPeriod = 4, int seedCapacity = 256, int retainedCapacity = 32)
    {
        if (maxPeriod is < 1 or > 4 || seedCapacity < 0 || retainedCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(maxPeriod));
        Graph = graph; TrainingPaths = trainingPaths;
        var blocked = new List<IElement>(); var pending = new List<IElement>(); var cuts = new List<IElement>();
        var spent = new List<IElement>(); var rejected = new List<IElement>();
        Paths = ReadPaths(graph, trainingPaths, blocked, pending);
        var selectedRows = Paths.SelectMany(path => path.Rows).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray();
        var selectedLinks = Paths.SelectMany(path => path.Links).Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray();
        TrainingNodes = N.S(selectedRows.Select(row => row.Left).Distinct(N.Participants.Instance).ToArray());
        TrainingSurface = new(N.S(selectedRows), N.S(selectedLinks));
        Branching = N.S(N.E(graph.Branching).OfType<Relation>().Where(row => N.E(TrainingNodes).Any(node => N.Same(node, row.Left))).Cast<IElement>().ToArray());
        foreach (var row in selectedRows) if (!N.Read((Relation)row.Right, out _)) pending.Add(row);
        var periodSlots = Slots(maxPeriod); var seedSlots = Slots(seedCapacity); var retentionSlots = Slots(retainedCapacity);
        Budget = new(N.S(periodSlots), new Relation(N.S(seedSlots), N.S(retentionSlots)));
        Definition = new(new Relation(TrainingSurface, trainingPaths), Budget);
        var symbols = selectedRows.SelectMany(row => N.E((Relation)row.Right)).Distinct(N.Participants.Instance).ToArray();
        var proposals = new Dictionary<string, Proposal>(StringComparer.Ordinal);
        var usedSeeds = 0; var usedRetentions = 0;
        foreach (var path in Paths)
            for (var period = 1; period <= maxPeriod; period++)
                for (var start = 0; start + 2 * period <= path.Rows.Length; start++)
                {
                    var window = path.Rows.Skip(start).Take(2 * period).ToArray();
                    var question = new Relation(new Relation(path.Scope, N.S(periodSlots.Take(period).ToArray())), N.S(window));
                    if (!Spend(question)) { cuts.Add(question); continue; }
                    var choices = Enumerable.Range(0, period).Select(index => N.E((Relation)window[index].Right)
                        .Where(symbol => N.E((Relation)window[index + period].Right).Any(other => N.Same(other, symbol)))
                        .Distinct(N.Participants.Instance).ToArray()).ToArray();
                    if (choices.Any(choice => choice.Length == 0))
                    {
                        var unknown = window.Any(row => !N.Read((Relation)row.Right, out _));
                        (unknown ? pending : rejected).Add(question); continue;
                    }
                    var word = new IElement[period]; Enumerate(0);
                    void Enumerate(int index)
                    {
                        if (usedSeeds >= seedSlots.Length) { cuts.Add(new Relation(question, N.S(word.Take(index).ToArray()))); return; }
                        if (index < period)
                        {
                            for (var choiceIndex = 0; choiceIndex < choices[index].Length; choiceIndex++)
                            {
                                word[index] = choices[index][choiceIndex]; Enumerate(index + 1);
                                if (usedSeeds >= seedSlots.Length)
                                {
                                    if (choiceIndex + 1 < choices[index].Length)
                                        cuts.Add(new Relation(question, new Relation(N.S(word.Take(index).ToArray()),
                                            N.S(choices[index].Skip(choiceIndex + 1).ToArray()))));
                                    break;
                                }
                            }
                            return;
                        }
                        if (!Spend(new Relation(question, N.S(word)))) return;
                        var rotation = CanonicalRotation(word, symbols);
                        var canonical = Enumerable.Range(0, period).Select(index => word[(rotation + index) % period]).ToArray();
                        var key = string.Join(',', canonical.Select(symbol => Array.FindIndex(symbols, candidate => N.Same(candidate, symbol))));
                        if (!proposals.TryGetValue(key, out var proposal))
                        {
                            if (usedRetentions >= retentionSlots.Length) { cuts.Add(new Relation(question, N.S(canonical))); return; }
                            proposal = new(canonical, retentionSlots[usedRetentions++]); proposals.Add(key, proposal);
                        }
                        var links = path.Links.Where(link => link.Right is Relation pair &&
                            window.Any(row => N.Same(row.Left, pair.Left)) && window.Any(row => N.Same(row.Left, pair.Right))).ToArray();
                        proposal.Seeds.Add(new(path, window, links, rotation));
                    }
                }
        Grammars = proposals.Values.Select(proposal =>
        {
            var phases = proposal.Symbols.Select(symbol => new Relation(new Placeholder(), symbol)).ToArray();
            var successors = phases.Select((phase, index) => (IElement)new Relation(phase.Left, phases[(index + 1) % phases.Length].Left)).ToArray();
            var pattern = new Relation(N.S(phases), N.S(successors));
            var witnesses = proposal.Seeds.Select(seed => (IElement)new Relation(new Relation(seed.Path.Scope, N.S(seed.Rows)),
                new Relation(N.S(seed.Links), N.S(Enumerable.Range(0, phases.Length).Select(index =>
                {
                    var phase = (index - seed.Rotation + phases.Length) % phases.Length;
                    return (IElement)new Relation(phases[phase], new Relation(seed.Rows[index], seed.Rows[index + phases.Length]));
                }).ToArray())))).ToArray();
            var seeds = N.S(witnesses); var read = new Relation(new Relation(Definition, pattern), seeds);
            spent.Add(new Relation(proposal.Retention, read)); return new QualitativePatternGrammarCandidate(read, pattern, seeds);
        }).ToArray();
        Candidates = N.S(Grammars.Select(candidate => (IElement)candidate.Read).ToArray());
        FrozenCandidates = new(Definition, Candidates);
        Spent = N.S(spent.ToArray()); Unsearched = N.S(cuts.ToArray()); RejectedSeeds = N.S(rejected.ToArray());
        Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.Distinct(N.Participants.Instance).ToArray());
        Training = new(this, graph, trainingPaths, false);
        Surface = new(FrozenCandidates, new Relation(Spent, new Relation(Unsearched, new Relation(RejectedSeeds,
            new Relation(Blocked, new Relation(Unresolved, new Relation(Branching, Training.Surface)))))));

        bool Spend(IElement proposal)
        {
            if (usedSeeds >= seedSlots.Length) return false;
            spent.Add(new Relation(seedSlots[usedSeeds++], proposal)); return true;
        }
    }

    public QualitativePatternGrammarEvaluation Evaluate(QualitativePatternTokenGraph current, Relation heldoutPaths) => new(this, current, heldoutPaths, true);
    public bool TryReadCandidate(Relation actual, out QualitativePatternGrammarCandidate candidate)
    {
        candidate = Grammars.FirstOrDefault(candidate => ReferenceEquals(candidate.Read, actual))!;
        return candidate is not null && TryReadPattern(candidate.Pattern, out _, out _);
    }
    public static bool TryReadPattern(Relation pattern, out Relation[] phases, out Relation[] successors)
    {
        phases = successors = [];
        if (pattern is not { Left: Relation phaseScope, Right: Relation successorScope } ||
            !N.Read(phaseScope, out var phaseRows) || !N.Read(successorScope, out var successorRows) ||
            phaseRows.Length == 0 || phaseRows.Any(row => row is not Relation { Right: Relation }) ||
            successorRows.Any(row => row is not Relation) || !N.Distinct(phaseRows.Cast<Relation>().Select(row => row.Left).ToArray()) ||
            successorRows.Length != phaseRows.Length) return false;
        phases = phaseRows.Cast<Relation>().ToArray(); successors = successorRows.Cast<Relation>().ToArray();
        for (var i = 0; i < phases.Length; i++)
        {
            var current = phases[i].Left; var next = phases[(i + 1) % phases.Length].Left;
            if (successors.Count(row => N.Same(row.Left, current) && N.Same(row.Right, next)) != 1) return false;
        }
        return true;
    }
    internal static List<PatternPath> ReadPaths(QualitativePatternTokenGraph graph, Relation selection,
        List<IElement> blocked, List<IElement> pending)
    {
        var result = new List<PatternPath>();
        if (!N.Read(selection, out var paths)) pending.Add(selection);
        foreach (var item in paths)
        {
            if (item is not Relation scope || !N.Read(scope, out var nodes)) { pending.Add(item); continue; }
            if (nodes.Length == 0 || !N.Distinct(nodes)) { blocked.Add(item); continue; }
            var rows = new List<Relation>(); var links = new List<Relation>(); var valid = true;
            foreach (var node in nodes)
                if (graph.TryFindNode(node, out var row, out _)) rows.Add(row);
                else { pending.Add(new Relation(scope, node)); valid = false; }
            for (var i = 1; i < nodes.Length; i++)
            {
                var proofs = graph.Between(nodes[i - 1], nodes[i]);
                if (proofs.Length != 0) links.AddRange(proofs);
                else
                {
                    var missing = new Relation(new Relation(nodes[i - 1], nodes[i]), graph.Links);
                    (N.Read(graph.Links, out _) ? blocked : pending).Add(missing); valid = false;
                }
            }
            if (valid) result.Add(new(scope, rows.ToArray(), links.Distinct<Relation>(ReferenceEqualityComparer.Instance).ToArray()));
        }
        return result;
    }
    private static int CanonicalRotation(IElement[] word, IElement[] alphabet)
    {
        var best = 0;
        for (var rotation = 1; rotation < word.Length; rotation++)
            for (var i = 0; i < word.Length; i++)
            {
                var a = Array.FindIndex(alphabet, symbol => N.Same(symbol, word[(rotation + i) % word.Length]));
                var b = Array.FindIndex(alphabet, symbol => N.Same(symbol, word[(best + i) % word.Length]));
                if (a < b) { best = rotation; break; } if (a > b) break;
            }
        return best;
    }
    private static IElement[] Slots(int count) => Enumerable.Range(0, count).Select(_ => (IElement)new Placeholder()).ToArray();
    internal sealed class PatternPath(Relation scope, Relation[] rows, Relation[] links)
    { public Relation Scope { get; } = scope; public Relation[] Rows { get; } = rows; public Relation[] Links { get; } = links; }
}

public sealed class QualitativePatternGrammarCandidate(Relation read, Relation pattern, Relation trainingWitnesses)
{
    public Relation Read { get; } = read;
    public Relation Pattern { get; } = pattern;
    public Relation TrainingWitnesses { get; } = trainingWitnesses;
    public Relation Phases => (Relation)Pattern.Left;
    public Relation Successors => (Relation)Pattern.Right;
    /// <summary>Shared native framed definitions. Reusing these actual rows
    /// permits the next layer to compare the same pattern and starting phase
    /// without replacing structural correspondence with a display label.</summary>
    public Relation FramedSymbols { get; } = N.S(N.E((Relation)pattern.Left).Cast<Relation>()
        .Select(phase => (IElement)new Relation(pattern, phase.Left)).ToArray());
    public int Period => N.E(Phases).Length;
    public Relation Description => N.S(N.E(Phases).Concat(N.E(Successors)).ToArray());
}

/// <summary>Reapplies frozen phase graphs to supplied paths. Every possible
/// starting phase is retained. Held-out nodes must be distinct from training
/// nodes; evaluating a refinement preserves those held-out occurrence IDs.</summary>
public sealed class QualitativePatternGrammarEvaluation
{
    public QualitativePatternGrammar Grammar { get; }
    public QualitativePatternTokenGraph Graph { get; }
    public Relation Paths { get; }
    public Relation Separation { get; }
    public Relation Supported { get; }
    public Relation Refuted { get; }
    public Relation Unresolved { get; }
    public Relation Demands { get; }
    public Relation Blocked { get; }
    public Relation InputUnresolved { get; }
    public Relation Surface { get; }
    public IReadOnlyList<QualitativePatternGrammarMatch> Matches { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(InputUnresolved).Length == 0 && N.E(Unresolved).Length == 0;

    internal QualitativePatternGrammarEvaluation(QualitativePatternGrammar grammar, QualitativePatternTokenGraph graph,
        Relation paths, bool requireDisjoint)
    {
        Grammar = grammar; Graph = graph; Paths = paths;
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        var selected = QualitativePatternGrammar.ReadPaths(graph, paths, blocked, pending);
        var selectedNodes = selected.SelectMany(path => path.Rows).Select(row => row.Left).Distinct(N.Participants.Instance).ToArray();
        var overlap = selectedNodes.Where(node => N.E(grammar.TrainingNodes).Any(old => N.Same(node, old))).ToArray();
        Separation = new(new Relation(grammar.TrainingNodes, N.S(selectedNodes)), N.S(overlap));
        if (requireDisjoint && overlap.Length != 0) { blocked.Add(Separation); selected = []; }
        var matches = new List<QualitativePatternGrammarMatch>();
        foreach (var candidate in grammar.Grammars)
        {
            if (!QualitativePatternGrammar.TryReadPattern(candidate.Pattern, out var phases, out _)) { blocked.Add(candidate.Pattern); continue; }
            foreach (var path in selected)
                for (var start = 0; start < phases.Length; start++)
                {
                    var bindings = new List<IElement>(); var obligations = new List<QualitativePatternNodeObligation>();
                    for (var i = 0; i < path.Rows.Length; i++)
                    {
                        var nodeRow = path.Rows[i]; var phase = phases[(start + i) % phases.Length];
                        var expected = phase.Right; var possibilities = (Relation)nodeRow.Right;
                        var closed = N.Read(possibilities, out var alternatives);
                        var known = alternatives.Any(symbol => N.Same(symbol, expected));
                        var yes = closed && alternatives.Length == 1 && known ? N.S(new Relation(expected, possibilities)) : N.S();
                        var no = closed && !known ? N.S(new Relation(expected, possibilities)) : N.S();
                        var unknown = N.E(yes).Length == 0 && N.E(no).Length == 0 ? N.S(new Relation(expected, possibilities)) : N.S();
                        var cause = new Relation(new Relation(phase, new Relation(nodeRow, expected)), new Relation(yes, new Relation(no, unknown)));
                        var demand = new Relation(cause, new Relation(phase, new Relation(expected, nodeRow.Left)));
                        obligations.Add(new(cause, demand, nodeRow, phase, expected, yes, no, unknown));
                        bindings.Add(new Relation(nodeRow.Left, phase.Left));
                    }
                    var phaseBindings = N.S(bindings.ToArray());
                    var support = N.S(obligations.Where(obligation => obligation.IsSupported).Select(obligation => (IElement)obligation.Read).ToArray());
                    var refute = N.S(obligations.Where(obligation => obligation.IsRefuted).Select(obligation => (IElement)obligation.Read).ToArray());
                    var unknowns = N.S(obligations.Where(obligation => obligation.IsUnresolved).Select(obligation => (IElement)obligation.Read).ToArray());
                    var demands = N.S(obligations.Where(obligation => obligation.IsUnresolved).Select(obligation => (IElement)obligation.Demand).ToArray());
                    var read = new Relation(new Relation(candidate.Read, new Relation(path.Scope, phases[start].Left)),
                        new Relation(phaseBindings, new Relation(support, new Relation(refute, new Relation(unknowns,
                            new Relation(demands, N.S(path.Links)))))));
                    matches.Add(new(candidate, read, path.Scope, phases[start].Left, phaseBindings, support, refute, unknowns, demands, obligations));
                }
        }
        Matches = matches;
        Supported = N.S(matches.Where(match => match.IsSupported).Select(match => (IElement)match.Read).ToArray());
        Refuted = N.S(matches.Where(match => match.IsRefuted).Select(match => (IElement)match.Read).ToArray());
        Unresolved = N.S(matches.Where(match => match.IsUnresolved).Select(match => (IElement)match.Read).ToArray());
        Demands = N.S(matches.Where(match => !match.IsRefuted).SelectMany(match => match.Obligations.Where(obligation => obligation.IsUnresolved))
            .Select(obligation => (IElement)obligation.Demand).ToArray());
        Blocked = N.S(blocked.ToArray()); InputUnresolved = N.S(pending.ToArray());
        var scope = new Relation(new Relation(grammar.FrozenCandidates, graph.Surface), new Relation(paths, Separation));
        Surface = new(scope, new Relation(Supported, new Relation(Refuted, new Relation(Unresolved,
            new Relation(Demands, new Relation(Blocked, new Relation(InputUnresolved, N.S(matches.Select(match => (IElement)match.Read).ToArray()))))))));
    }
    public bool TryReadDemand(Relation actual, out QualitativePatternNodeObligation obligation)
    {
        obligation = null!;
        if (!N.E(Demands).Any(row => ReferenceEquals(row, actual))) return false;
        obligation = Matches.SelectMany(match => match.Obligations).FirstOrDefault(candidate => ReferenceEquals(candidate.Demand, actual))!;
        return obligation is not null && obligation.IsUnresolved;
    }
    public bool TryReadMatch(Relation actual, out QualitativePatternGrammarMatch match)
    { match = Matches.FirstOrDefault(candidate => ReferenceEquals(candidate.Read, actual))!; return match is not null; }
}

public sealed class QualitativePatternGrammarMatch(QualitativePatternGrammarCandidate candidate, Relation read,
    Relation path, IElement startPhase, Relation phaseBindings, Relation supported, Relation refuted,
    Relation unresolved, Relation demands, IReadOnlyList<QualitativePatternNodeObligation> obligations)
{
    public QualitativePatternGrammarCandidate Candidate { get; } = candidate;
    public Relation Pattern => Candidate.Pattern;
    public Relation Read { get; } = read;
    public Relation Path { get; } = path;
    public IElement StartPhase { get; } = startPhase;
    public Relation FramedSymbol => N.E(Candidate.FramedSymbols).OfType<Relation>().Single(symbol => N.Same(symbol.Right, StartPhase));
    public Relation PhaseBindings { get; } = phaseBindings;
    public Relation Supported { get; } = supported;
    public Relation Refuted { get; } = refuted;
    public Relation Unresolved { get; } = unresolved;
    public Relation Demands { get; } = demands;
    public IReadOnlyList<QualitativePatternNodeObligation> Obligations { get; } = obligations;
    public bool IsSupported => N.E(Refuted).Length == 0 && N.E(Unresolved).Length == 0;
    public bool IsRefuted => N.E(Refuted).Length != 0;
    public bool IsUnresolved => !IsRefuted && N.E(Unresolved).Length != 0;
}

public sealed class QualitativePatternNodeObligation(Relation read, Relation demand, Relation nodeRow,
    Relation phase, IElement expected, Relation supported, Relation refuted, Relation unresolved)
{
    public Relation Read { get; } = read;
    public Relation Demand { get; } = demand;
    public IElement Node => NodeRow.Left;
    public Relation NodeRow { get; } = nodeRow;
    public Relation Phase { get; } = phase;
    public IElement Expected { get; } = expected;
    public Relation Supported { get; } = supported;
    public Relation Refuted { get; } = refuted;
    public Relation Unresolved { get; } = unresolved;
    public bool IsSupported => N.E(Supported).Length != 0;
    public bool IsRefuted => N.E(Refuted).Length != 0;
    public bool IsUnresolved => N.E(Unresolved).Length != 0;
}
