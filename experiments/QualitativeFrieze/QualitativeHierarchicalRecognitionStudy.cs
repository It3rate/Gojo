using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Composes existing local readers across two opaque observational
/// boundaries. The fixture supplies observations; its authoring recipe and
/// display coordinates are never passed to a pattern reader or query service.</summary>
public sealed class QualitativeHierarchicalRecognitionSession
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public QualitativeLayeredPatternFixture Fixture { get; }
    public QualitativeMotifArrangement Arrangement { get; }
    public IReadOnlyList<Relation> RowPaths { get; }
    public Relation InitialCollection { get; }
    public Relation HiddenNodes { get; }
    public Relation Partition { get; }
    public QualitativePatternTokenGraph InitialCells { get; }
    public QualitativePatternTokenGraph FinalCells { get; }
    public QualitativePatternGrammar CellGrammar { get; }
    public Relation CellHeldoutPaths { get; }
    public QualitativePatternGrammarEvaluation CellBefore { get; }
    public QualitativePatternGrammarEvaluation CellAfter { get; }
    public QualitativePatternLadder Ladder { get; }
    public IReadOnlyList<QualitativePatternRowRead> InitialRowReads { get; }
    public IReadOnlyList<QualitativePatternRowRead> FinalRowReads { get; }
    public QualitativePatternTokenGraph InitialRows { get; }
    public QualitativePatternGrammar RowGrammar { get; }
    public Relation RowHeldoutPaths { get; }
    public QualitativePatternGrammarEvaluation RowBefore { get; }
    public QualitativePatternGrammarEvaluation RowAfter { get; }
    public QualitativeEvidenceService CellService { get; }
    public QualitativeEvidenceService RowService { get; }
    public QualitativeEvidenceRefinement UpperRound { get; }
    public IReadOnlyList<QualitativeEvidenceRefinement> LowerRounds { get; }
    public Relation CausalRounds { get; }
    public Relation AuditSurface { get; }
    public int TrainingRowCount { get; }
    public int InitiallyCollectedCount { get; }
    public int NewCellQueries => N.E(CellService.Spent).Length;

    public QualitativeHierarchicalRecognitionSession(string id, string name, string description,
        QualitativeLayeredPatternFixture fixture, int trainingRows,
        IReadOnlyCollection<(int Column, int Row)> hidden, int cellCapacity, Relation? alphabet = null)
    {
        Id = id; Name = name; Description = description; Fixture = fixture; TrainingRowCount = trainingRows;
        Arrangement = new(fixture.Motifs, fixture.Placement, fixture.ChamberFocus, alphabet ?? fixture.AlphabetFocus);
        RowPaths = OrderRows(Arrangement.HorizontalPaths, Arrangement.Vertical);
        if (!Arrangement.IsComplete || RowPaths.Count <= trainingRows || trainingRows < 2)
            throw new InvalidOperationException("The selected native chamber paths do not establish the requested training partition.");
        var hiddenNodes = new HashSet<IElement>(N.Participants.Instance);
        foreach (var (column, row) in hidden)
        {
            if (row < trainingRows || row >= RowPaths.Count || column < 0 || column >= N.E(RowPaths[row]).Length)
                throw new ArgumentException("Hidden observations must belong to actual held-out path positions.", nameof(hidden));
            hiddenNodes.Add(N.E(RowPaths[row])[column]);
        }
        HiddenNodes = N.S(hiddenNodes.ToArray());
        var collected = new List<IElement>(); var cellRows = new List<IElement>();
        foreach (var node in N.E(Arrangement.Nodes))
        {
            Relation symbols;
            if (hiddenNodes.Contains(node)) symbols = StructuralConstraint.Open();
            else
            {
                var reply = Arrangement.ReadCell(node); symbols = reply.Symbols;
                collected.Add(new Relation(node, reply.Surface));
            }
            cellRows.Add(new Relation(node, symbols));
        }
        InitiallyCollectedCount = collected.Count;
        InitialCollection = new(Arrangement.Interface, new Relation(N.S(collected.ToArray()), HiddenNodes));
        InitialCells = new(N.S(cellRows.ToArray()), Arrangement.Horizontal);
        var training = N.S(RowPaths.Take(trainingRows).Cast<IElement>().ToArray());
        CellHeldoutPaths = N.S(RowPaths.Skip(trainingRows).Cast<IElement>().ToArray());
        Partition = new(training, CellHeldoutPaths);
        CellGrammar = new(InitialCells, training, maxPeriod: 4, seedCapacity: 2048, retainedCapacity: 32);
        CellBefore = CellGrammar.Evaluate(InitialCells, CellHeldoutPaths);
        Ladder = new(CellGrammar, N.S(RowPaths.Cast<IElement>().ToArray()), Arrangement.Vertical);
        InitialRowReads = RowPaths.Select(path => Ladder.ReadRow(Ladder.NodeForPath(path), CellGrammar.Training, CellBefore)).ToArray();
        InitialRows = new(N.S(InitialRowReads.Select(read => (IElement)new Relation(read.Node, read.Symbols)).ToArray()), Ladder.UpperLinks);
        var rowTraining = N.S(N.S(RowPaths.Take(trainingRows).Select(Ladder.NodeForPath).ToArray()));
        RowHeldoutPaths = N.S(N.S(RowPaths.Skip(trainingRows).Select(Ladder.NodeForPath).ToArray()));
        RowGrammar = new(InitialRows, rowTraining, maxPeriod: 4, seedCapacity: 2048, retainedCapacity: 32);
        RowBefore = RowGrammar.Evaluate(InitialRows, RowHeldoutPaths);
        CellService = new(Arrangement.Interface, Arrangement.CellPorts, Arrangement.Question, cellCapacity, request =>
        {
            var reply = Arrangement.ReadCell(request.Node);
            return new(request, reply.Symbols, reply.Evidence);
        });
        var currentCells = InitialCells;
        var lowerRounds = new List<QualitativeEvidenceRefinement>(); var causal = new List<IElement>();
        RowService = new(Ladder.Interface, Ladder.Ports, Ladder.Question, RowPaths.Count - trainingRows, request =>
        {
            if (!Ladder.TryPathForNode(request.Node, out var path))
                return QualitativeEvidenceReply.Refuse(request, N.S(request.Node));
            var before = CellGrammar.Evaluate(currentCells, N.S(path));
            var round = new QualitativeEvidenceRefinement(before, Arrangement.CellPorts, request.Read, CellService);
            lowerRounds.Add(round); causal.Add(new Relation(request.Read, round.Surface)); currentCells = round.After;
            var after = CellGrammar.Evaluate(currentCells, N.S(path));
            var answer = Ladder.ReadRow(request.Node, after);
            // Only the answer under the exposed contract goes up. The lower
            // matches, instance IDs and nested causal trace stay on the audit.
            return new(request, answer.Symbols, answer.Evidence);
        });
        UpperRound = new(RowBefore, Ladder.Ports, RowService);
        LowerRounds = lowerRounds; CausalRounds = N.S(causal.ToArray()); FinalCells = currentCells;
        CellAfter = CellGrammar.Evaluate(FinalCells, CellHeldoutPaths);
        RowAfter = RowGrammar.Evaluate(UpperRound.After, RowHeldoutPaths);
        FinalRowReads = RowPaths.Select(path => Ladder.ReadRow(Ladder.NodeForPath(path), CellGrammar.Training, CellAfter)).ToArray();
        AuditSurface = new(fixture.AuditSurface, new Relation(Arrangement.AuditSurface,
            new Relation(InitialCollection, new Relation(Partition, new Relation(CellGrammar.Surface,
                new Relation(CellBefore.Surface, new Relation(Ladder.AuditSurface, new Relation(RowGrammar.Surface,
                    new Relation(RowBefore.Surface, new Relation(CausalRounds, new Relation(UpperRound.Surface,
                        new Relation(CellAfter.Surface, new Relation(RowAfter.Surface,
                            N.S(InitialRowReads.Concat(FinalRowReads).Select(read => (IElement)read.AuditSurface).ToArray()))))))))))))));
    }

    // The ordering comes from native inter-row adjacency. Display positions
    // and source recipe indices play no part in this read.
    private static IReadOnlyList<Relation> OrderRows(Relation paths, Relation vertical)
    {
        var rows = N.E(paths).Cast<Relation>().ToArray(); var links = N.E(vertical).Cast<Relation>().ToArray();
        bool Follows(Relation first, Relation second)
        {
            var a = N.E(first); var b = N.E(second);
            return a.Length == b.Length && a.Select((node, i) => links.Any(link => link.Right is Relation pair &&
                N.Same(pair.Left, node) && N.Same(pair.Right, b[i]))).All(value => value);
        }
        var ordered = new List<Relation>();
        var start = rows.Where(row => !rows.Any(other => !ReferenceEquals(row, other) && Follows(other, row))).ToArray();
        if (start.Length != 1) throw new InvalidOperationException("The demonstration requires one unbranched native row chain.");
        var current = start[0];
        while (!ordered.Any(row => ReferenceEquals(row, current)))
        {
            ordered.Add(current);
            var next = rows.Where(row => !ReferenceEquals(row, current) && Follows(current, row)).ToArray();
            if (next.Length == 0) break;
            if (next.Length != 1) throw new InvalidOperationException("A branching row chain must be kept as competing paths.");
            current = next[0];
        }
        if (ordered.Count != rows.Length) throw new InvalidOperationException("The native row chain left other paths unaccounted for.");
        return ordered;
    }
}

public sealed class QualitativeHierarchicalRecognitionStudy
{
    public IReadOnlyList<QualitativeHierarchicalRecognitionSession> Sessions { get; }
    public QualitativeHierarchicalRecognitionStudy(Action<string>? log = null)
    {
        var sessions = new List<QualitativeHierarchicalRecognitionSession>();
        void Add(string id, string name, string description, QualitativeLayeredPatternFixture fixture,
            int trainingRows, (int Column, int Row)[] holes, int budget, Relation? alphabet = null)
        {
            log?.Invoke($"{id}: {name}");
            var session = new QualitativeHierarchicalRecognitionSession(id, name, description, fixture, trainingRows, holes, budget, alphabet);
            sessions.Add(session);
            log?.Invoke($"  cells {N.E(session.Arrangement.Nodes).Length}; programs {session.CellGrammar.Grammars.Count}/{session.RowGrammar.Grammars.Count}; new local queries {session.NewCellQueries}; upper support/open/refute {N.E(session.RowAfter.Supported).Length}/{N.E(session.RowAfter.Unresolved).Length}/{N.E(session.RowAfter.Refuted).Length}");
        }
        var checker = new QualitativeLayeredPatternFixture(8, 8, (x, y) => (x + y) % 2);
        Add("H01", "A checkerboard of row programs", "An alternating motif sequence becomes a row identity; alternating row start phases then form another sequence. Three missing local readings are requested through both layers.", checker, 4,
            [(2, 5), (5, 6), (6, 7)], 3);
        var thirds = new QualitativeLayeredPatternFixture(12, 12, (x, y) => (x + y) % 3);
        Add("H02", "Three phases at both levels", "Diamond, square and triangle recur along each row. The row starting phases recur across rows. Six complete rows train each frozen program; the other six are held out.", thirds, 6,
            Enumerable.Range(6, 6).Select(row => (row % 12, row)).ToArray(), 6);
        var fourths = new QualitativeLayeredPatternFixture(12, 8, (x, y) => (x + 2 * y) % 4);
        var fourthHoles = Enumerable.Range(4, 4).Select(row => (row % 12, row)).ToArray();
        Add("H03", "Four local phases, two row phases", "An eight-spoke star junction joins three circuit prototypes. Four distinct local roles recur below, while two row alignments recur above.", fourths, 4, fourthHoles, 4);
        var contrary = new QualitativeLayeredPatternFixture(8, 8, (x, y) => x == 2 && y == 5 ? 2 : (x + y) % 2);
        Add("H04", "A contrary local answer", "The hidden position contains a triangle where the frozen alternating program asks about a square. The valid local motif is accepted; the larger claim gains a counterexample. No replacement program is invented from held-out data.", contrary, 4,
            [(2, 5), (5, 6), (6, 7)], 3);
        Add("H05", "A question with insufficient capacity", "The same four-phase field has only one local query slot for four unknown positions. The unpaid questions remain blocked and the upper reading stays open.", fourths, 4, fourthHoles, 1);
        var competing = new QualitativeLayeredPatternFixture(8, 8, (x, y) => (x + y) % 2, overlappingUnit: (2, 5));
        Add("H06", "Two motifs compete for one role", "One chamber contains both a square and a triangle. A one-motif question receives two valid alternatives. Re-reading the same revision cannot manufacture certainty.", competing, 4,
            [(2, 5)], 1);
        var fourEdgeAlphabet = N.S(competing.Motifs.Families.Where(family => family.Kind == "cycle" && family.SegmentCount == 4)
            .Select(family => (IElement)family.Read).ToArray());
        Add("H07", "Change the question, preserve the field", "The same observation is read through a four-edge-circuit question. The triangle remains visible in the fine field and outside this declared role scope; this narrower interpretation supports the alternating program.", competing, 4,
            [(2, 5)], 1, fourEdgeAlphabet);
        Sessions = sessions;
    }
}
