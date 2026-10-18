using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Lifts actual token paths into fresh row participants. Adjacency is
/// a complete ordered cell-to-cell correspondence, not projected proximity.
/// Public replies contain only prototypes, upper roles and revision evidence;
/// lower occurrences and matches remain on the separate audit surface.</summary>
public sealed class QualitativePatternLadder
{
    // public row label = frozen phase pattern -> actual start phase identity
    // public question = Closed(frozen framed definitions) -> answer role
    // public interface = question -> (fresh rows -> (fresh ports ->
    //                    (fresh upper links -> (upper paths -> branching))))
    // private binding = actual lower path -> fresh upper row
    // private adjacency = actual upper link -> Closed((actual from cell ->
    //                     actual to cell) -> Closed(actual lower link proofs))
    public QualitativePatternGrammar Grammar { get; }
    public Relation Paths { get; }
    public Relation Rows { get; }
    public Relation Ports { get; }
    public Relation UpperLinks { get; }
    public Relation Vertical => UpperLinks;
    public Relation UpperPaths { get; }
    public Relation Branching { get; }
    public Relation Question { get; }
    public Relation Interface { get; }
    public Relation NodeBindings { get; }
    public Relation AdjacencyWitnesses { get; }
    public Relation RejectedAdjacency { get; }
    public Relation AuditSurface { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    private readonly Dictionary<Relation, IElement> rows = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<IElement, Relation> paths = new(N.Participants.Instance);
    private readonly Relation privateInput;

    public QualitativePatternLadder(QualitativePatternGrammar grammar, Relation rowPaths, Relation actualVerticalCellLinks)
    {
        Grammar = grammar; Paths = rowPaths;
        var blocked = new List<IElement>(); var pending = new List<IElement>();
        if (!N.Read(rowPaths, out var pathRows)) pending.Add(rowPaths);
        var admitted = new List<(Relation Path, IElement[] Cells, IElement Node)>();
        foreach (var item in pathRows)
        {
            if (item is not Relation path || !N.Read(path, out var cells) || cells.Length == 0 || !N.Distinct(cells) || rows.ContainsKey(path))
            { blocked.Add(item); continue; }
            var node = new Placeholder(); rows.Add(path, node); paths.Add(node, path); admitted.Add((path, cells, node));
        }
        Rows = N.S(admitted.Select(row => row.Node).ToArray());
        Ports = N.S(admitted.Select(row => (IElement)new Relation(row.Node, new Placeholder())).ToArray());
        NodeBindings = N.S(admitted.Select(row => (IElement)new Relation(row.Path, row.Node)).ToArray());
        if (!N.Read(actualVerticalCellLinks, out var linkRows)) pending.Add(actualVerticalCellLinks);
        var duplicated = linkRows.OfType<Relation>().GroupBy(row => row.Left, N.Participants.Instance)
            .Where(group => group.Count() > 1).SelectMany(group => group).ToHashSet(ReferenceEqualityComparer.Instance);
        var cellLinks = new List<Relation>();
        foreach (var item in linkRows)
            if (item is not Relation { Right: Relation endpoints } link || duplicated.Contains(link) || N.Same(endpoints.Left, endpoints.Right))
                blocked.Add(item);
            else cellLinks.Add(link);
        var upper = new List<IElement>(); var witnesses = new List<IElement>(); var rejected = new List<IElement>();
        foreach (var from in admitted) foreach (var to in admitted)
        {
            if (ReferenceEquals(from.Path, to.Path)) continue;
            if (from.Cells.Length != to.Cells.Length || from.Cells.Any(cell => to.Cells.Any(other => N.Same(cell, other))))
            { rejected.Add(new Relation(new Relation(from.Path, to.Path), new Relation(N.S(from.Cells), N.S(to.Cells)))); continue; }
            var correspondence = from.Cells.Select((cell, i) => new Relation(new Relation(cell, to.Cells[i]),
                N.S(cellLinks.Where(link => link.Right is Relation pair && N.Same(pair.Left, cell) && N.Same(pair.Right, to.Cells[i])).Cast<IElement>().ToArray()))).ToArray();
            if (correspondence.Any(pair => N.E((Relation)pair.Right).Length == 0))
            { rejected.Add(new Relation(new Relation(from.Path, to.Path), N.S(correspondence))); continue; }
            var link = new Relation(new Placeholder(), new Relation(from.Node, to.Node));
            upper.Add(link); witnesses.Add(new Relation(link, N.S(correspondence)));
        }
        UpperLinks = N.S(upper.ToArray()); AdjacencyWitnesses = N.S(witnesses.ToArray());
        RejectedAdjacency = N.S(rejected.ToArray());
        Branching = N.S(admitted.Select(row => new Relation(row.Node,
                N.S(upper.OfType<Relation>().Where(link => link.Right is Relation pair && N.Same(pair.Left, row.Node)).Cast<IElement>().ToArray())))
            .Where(row => N.E((Relation)row.Right).Length > 1).Cast<IElement>().ToArray());
        UpperPaths = FindUpperPaths(N.E(Rows), upper.Cast<Relation>().ToArray());
        Question = new(N.S(grammar.Grammars.SelectMany(candidate => N.E(candidate.FramedSymbols)).Distinct(N.Participants.Instance).ToArray()), new Placeholder());
        // Only fresh unresolved roles cross this boundary. Their actual input
        // causes remain private, rather than leaking lower-level identities.
        Blocked = N.S(blocked.Select(_ => (IElement)new Relation(new Placeholder(), Question)).ToArray());
        Unresolved = N.S(pending.Select(_ => (IElement)new Relation(new Placeholder(), Question)).ToArray());
        Interface = new(Question, new Relation(Rows, new Relation(Ports, new Relation(UpperLinks,
            new Relation(UpperPaths, new Relation(Branching, new Relation(Blocked, Unresolved)))))));
        privateInput = new(grammar.Surface, new Relation(rowPaths, actualVerticalCellLinks));
        AuditSurface = new(privateInput, new Relation(NodeBindings, new Relation(AdjacencyWitnesses,
            new Relation(RejectedAdjacency, new Relation(N.S(blocked.ToArray()), new Relation(N.S(pending.ToArray()), Interface))))));
    }

    public IElement NodeForPath(Relation actualPath) => rows.TryGetValue(actualPath, out var node) ? node :
        throw new ArgumentException("The path is not an actual admitted lower path.", nameof(actualPath));
    public bool TryPathForNode(IElement node, out Relation path) => paths.TryGetValue(node, out path!);

    public QualitativePatternRowRead ReadRow(IElement node, params QualitativePatternGrammarEvaluation[] evaluations)
    {
        if (!paths.TryGetValue(node, out var path)) throw new ArgumentException("The node is not an actual upper row.", nameof(node));
        var supplied = evaluations.Distinct<QualitativePatternGrammarEvaluation>(ReferenceEqualityComparer.Instance).ToArray();
        var matches = new List<QualitativePatternGrammarMatch>(); var unavailable = new List<IElement>();
        var covered = false;
        foreach (var evaluation in supplied)
        {
            if (!ReferenceEquals(evaluation.Grammar.FrozenCandidates, Grammar.FrozenCandidates))
            { unavailable.Add(evaluation.Surface); continue; }
            if (!N.E(evaluation.Paths).Any(selected => ReferenceEquals(selected, path))) continue;
            var blocked = new List<IElement>(); var pending = new List<IElement>();
            var local = QualitativePatternGrammar.ReadPaths(evaluation.Graph, N.S(path), blocked, pending);
            if (local.Count != 1 || blocked.Count != 0 || pending.Count != 0)
            { unavailable.Add(new Relation(path, evaluation.Surface)); continue; }
            var actualMatches = evaluation.Matches.Where(match => ReferenceEquals(match.Path, path)).ToArray();
            if (actualMatches.Length == 0 && Grammar.Grammars.Count != 0)
            { unavailable.Add(new Relation(path, evaluation.Surface)); continue; }
            covered = true; matches.AddRange(actualMatches);
        }
        matches = matches.Distinct<QualitativePatternGrammarMatch>(ReferenceEqualityComparer.Instance).ToList();
        if (!covered) unavailable.Add(path);
        var viable = matches.Where(match => !match.IsRefuted).ToArray();
        var dominated = new HashSet<QualitativePatternGrammarMatch>(ReferenceEqualityComparer.Instance);
        var dominance = new List<IElement>();
        foreach (var simpler in viable) foreach (var larger in viable)
        {
            if (ReferenceEquals(simpler, larger) || !EquivalentPremises(simpler, larger, out var expectedPairs, out var linkPairs)) continue;
            var smallDescription = simpler.Candidate.Description; var largeDescription = larger.Candidate.Description;
            var smallRows = N.E(smallDescription); var largeRows = N.E(largeDescription);
            if (smallRows.Length >= largeRows.Length) continue;
            var cardinalityPairs = N.S(smallRows.Select((row, i) => (IElement)new Relation(row, largeRows[i])).ToArray());
            var excess = N.S(largeRows.Skip(smallRows.Length).ToArray());
            dominance.Add(new Relation(new Relation(simpler.Read, larger.Read), new Relation(expectedPairs,
                new Relation(linkPairs, new Relation(smallDescription, new Relation(largeDescription, new Relation(cardinalityPairs, excess)))))));
            dominated.Add(larger);
        }
        var preferred = viable.Where(match => !dominated.Contains(match)).ToArray();
        var coverage = preferred.Select(match => Coverage(match, out _)).ToArray();
        var strong = preferred.All(match => match.IsSupported && HasTwoCycles(match));
        var labels = preferred.Select(match => (IElement)match.FramedSymbol).Distinct(N.Participants.Instance).ToArray();
        var complete = covered && unavailable.Count == 0 && strong;
        var symbols = complete ? N.S(labels) : StructuralConstraint.Open(labels);
        var revision = new Placeholder(); var query = new Relation(Question, node);
        var evidence = new Relation(query, new Relation(revision, symbols));
        var surface = new Relation(query, new Relation(symbols, evidence));
        var privateMatches = N.S(matches.Select(match => (IElement)match.Read).ToArray());
        var preferredScope = N.S(preferred.Select(match => (IElement)match.Read).ToArray());
        var domination = N.S(dominance.ToArray());
        var audit = new Relation(new Relation(privateInput, N.S(supplied.Select(evaluation => (IElement)evaluation.Surface).ToArray())),
            new Relation(surface, new Relation(privateMatches, new Relation(preferredScope, new Relation(domination,
                new Relation(N.S(coverage), N.S(unavailable.ToArray())))))));
        return new(node, symbols, evidence, surface, revision, privateMatches, preferredScope, domination, N.S(coverage), audit);
    }

    private static bool EquivalentPremises(QualitativePatternGrammarMatch first, QualitativePatternGrammarMatch second,
        out Relation expectedPairs, out Relation linkPairs)
    {
        expectedPairs = linkPairs = N.S();
        if (!ReferenceEquals(first.Path, second.Path) || first.Obligations.Count != second.Obligations.Count) return false;
        var pairs = new List<IElement>();
        for (var i = 0; i < first.Obligations.Count; i++)
        {
            var a = first.Obligations[i]; var b = second.Obligations[i];
            if (!ReferenceEquals(a.NodeRow, b.NodeRow) || !N.Same(a.Node, b.Node) || !N.Same(a.Expected, b.Expected) ||
                a.IsSupported != b.IsSupported || a.IsRefuted != b.IsRefuted || a.IsUnresolved != b.IsUnresolved) return false;
            pairs.Add(new Relation(a.Read, b.Read));
        }
        var aa = MatchLinks(first); var bb = MatchLinks(second);
        if (aa.Length != bb.Length || aa.Any(link => !bb.Any(other => ReferenceEquals(link, other)))) return false;
        expectedPairs = N.S(pairs.ToArray()); linkPairs = N.S(aa.Select(link => (IElement)new Relation(link, link)).ToArray()); return true;
    }
    private static IElement[] MatchLinks(QualitativePatternGrammarMatch match) => match.Read.Right is Relation
    { Right: Relation { Right: Relation { Right: Relation { Right: Relation { Right: Relation links } } } } } ? N.E(links) : [];

    private static bool HasTwoCycles(QualitativePatternGrammarMatch match)
    { Coverage(match, out var complete); return complete; }
    private static Relation Coverage(QualitativePatternGrammarMatch match, out bool complete)
    {
        var all = N.E(match.PhaseBindings).Cast<Relation>().ToArray(); var pairs = new List<IElement>(); var missing = new List<IElement>();
        foreach (var phase in N.E(match.Candidate.Phases).Cast<Relation>())
        {
            var occurrences = all.Where(binding => N.Same(binding.Right, phase.Left)).ToArray();
            if (occurrences.Length < 2) missing.Add(new Relation(phase, N.S(occurrences)));
            else pairs.Add(new Relation(phase, new Relation(occurrences[0], occurrences[1])));
        }
        complete = missing.Count == 0;
        return new(match.Read, new Relation(N.S(pairs.ToArray()), N.S(missing.ToArray())));
    }
    private static Relation FindUpperPaths(IElement[] nodes, Relation[] links)
    {
        var paths = new List<IElement>(); var used = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        Relation[] Out(IElement node) => links.Where(link => link.Right is Relation pair && N.Same(pair.Left, node)).ToArray();
        Relation[] In(IElement node) => links.Where(link => link.Right is Relation pair && N.Same(pair.Right, node)).ToArray();
        foreach (var node in nodes)
        {
            var incoming = In(node); var outgoing = Out(node);
            if (incoming.Length == 0 && outgoing.Length == 0) { paths.Add(N.S(node)); continue; }
            if (incoming.Length != 1 || incoming[0].Right is Relation previous && Out(previous.Left).Length != 1)
                foreach (var link in outgoing) Follow(link);
        }
        foreach (var link in links) if (!used.Contains(link)) Follow(link);
        return N.S(paths.ToArray());

        void Follow(Relation start)
        {
            if (used.Contains(start) || start.Right is not Relation initial) return;
            var chain = new List<IElement> { initial.Left }; var link = start;
            while (used.Add(link) && link.Right is Relation pair)
            {
                if (chain.Any(node => N.Same(node, pair.Right))) break;
                chain.Add(pair.Right); var outgoing = Out(pair.Right);
                if (outgoing.Length != 1 || In(pair.Right).Length != 1) break;
                link = outgoing[0];
            }
            paths.Add(N.S(chain.ToArray()));
        }
    }
}

public sealed class QualitativePatternRowRead(IElement node, Relation symbols, Relation evidence, Relation surface,
    IElement revision, Relation privateMatches, Relation preferred, Relation dominance, Relation coverage, Relation auditSurface)
{
    public IElement Node { get; } = node;
    public Relation Symbols { get; } = symbols;
    public Relation Evidence { get; } = evidence;
    public Relation Surface { get; } = surface;
    public IElement Revision { get; } = revision;
    public Relation PrivateMatches { get; } = privateMatches;
    public Relation Preferred { get; } = preferred;
    public Relation Dominance { get; } = dominance;
    public Relation Coverage { get; } = coverage;
    public Relation AuditSurface { get; } = auditSurface;
}
