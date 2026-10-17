using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A local non-draining conditional read. Only the advertised
/// publication contract, its support and this receiver's law are inspected.
/// No producer recurrence, private implementation or finer sample is queried.</summary>
public sealed class CoarsePatternConnection
{
    // port = (producer -> receiver) -> (basis -> closed-signal-domain)
    // surface = port -> (closed-column-domain -> finite-law)
    // law: signal x actual-column -> Closed(actual stroke-set alternatives)
    // receipt = (publication -> (surface -> column-support)) ->
    //           (law-read -> (selected-alternatives -> (blocked -> unresolved)))
    // Known denial is closed empty support plus the attempted publication in
    // blocked. An open or competing outcome is pending, not an empty action.
    public Relation Port { get; }
    public Relation Columns { get; }
    public Relation Law { get; }
    public Relation Surface { get; }

    public CoarsePatternConnection(Relation port, Relation columns, Relation law)
    { Port = port; Columns = columns; Law = law; Surface = new Relation(port, new Relation(columns, law)); }

    public Relation Consume(Relation publication, Relation columnSupport)
    {
        var blocked = new List<IElement>(); var unresolved = new List<IElement>();
        var admitted = Port.Left is Relation endpoints && Port.Right is Relation { Right: Relation domain } contract &&
            FiniteLaw.TryRead(Law, out var inputs, out var columns, out _, out _) &&
            ReferenceEquals(inputs, domain) && ReferenceEquals(columns, Columns) &&
            QualitativePublication.TryRead(publication, out var source, out var subject, out _, out var basis,
                out var advertised, out _, out _) && Same(source, endpoints.Left) && Same(subject, endpoints.Right) &&
            Same(basis, contract.Left) && ReferenceEquals(advertised, domain);
        var input = QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _)
            ? support : StructuralConstraint.Open();
        var read = FiniteLaw.Evaluate(Law, input, columnSupport);
        Relation selected = StructuralConstraint.Open();
        if (!admitted) blocked.Add(publication);
        else if (!Closed(input) || E(input).Length != 1 || !Closed(columnSupport) || E(columnSupport).Length != 1 ||
            !FiniteLaw.TryReadResult(read, out var offeredOutcomes, out var residue, out _) || !Closed(offeredOutcomes) || E(residue).Length != 0)
            unresolved.Add(new Relation(publication, read));
        else
        {
            FiniteLaw.TryReadResult(read, out var outcomes, out _, out var rows);
            var options = E(outcomes);
            var rowSupports = E(rows).OfType<Relation>().Select(row => row.Right as Relation).ToArray();
            var completeRows = rowSupports.Length == E(rows).Length && rowSupports.Length > 0 && rowSupports.All(row => row is not null && Closed(row));
            if (options.Length == 0 && completeRows && rowSupports.All(row => E(row!).Length == 0))
            { selected = StructuralConstraint.Closed(); blocked.Add(publication); }
            else if (options is [Relation option] && Closed(option) && completeRows &&
                rowSupports.All(row => E(row!) is [var value] && Same(value, option))) selected = outcomes;
            else unresolved.Add(new Relation(publication, read));
        }
        return new Relation(new Relation(publication, new Relation(Surface, columnSupport)),
            new Relation(read, new Relation(selected, new Relation(S(blocked.ToArray()), S(unresolved.ToArray())))));
    }

    /// <summary>Rechecks context and conditional premises rather than accepting
    /// copied success fields. A closed denial is distinguishable from an open
    /// outcome through blocked and unresolved scopes.</summary>
    public bool TryReadResult(Relation receipt, out Relation selected, out Relation blocked, out Relation unresolved)
    {
        selected = blocked = unresolved = null!;
        if (receipt.Left is not Relation { Left: Relation publication, Right: Relation context } ||
            !ReferenceEquals(context.Left, Surface) || context.Right is not Relation columns ||
            receipt.Right is not Relation { Left: Relation read, Right: Relation result } ||
            result.Left is not Relation options || result.Right is not Relation remainder ||
            remainder.Left is not Relation refused || remainder.Right is not Relation pending ||
            !StructuralConstraint.TryReadScope(options, out _, out _, out _) || !Closed(refused) || !Closed(pending) ||
            !FiniteLaw.TryReadResult(read, out _, out _, out _)) return false;
        var expected = Consume(publication, columns);
        var expectedBody = (Relation)expected.Right;
        var expectedRead = (Relation)expectedBody.Left;
        var expectedResult = (Relation)expectedBody.Right;
        var expectedRest = (Relation)expectedResult.Right;
        var input = QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _)
            ? support : null;
        var atoms = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        Include(Law); Include(publication); Include(columns);
        var inputSlots = atoms.OfType<Placeholder>().ToArray();
        var freshSlots = new List<(Placeholder Expected, Placeholder Actual)>();
        if (!ScopeSame(options, (Relation)expectedResult.Left) || !ScopeSame(refused, (Relation)expectedRest.Left) ||
            E(pending).Length != E((Relation)expectedRest.Right).Length ||
            read.Left is not Relation { Left: Relation request } || !ReferenceEquals(request.Left, Law) ||
            request.Right is not Relation operands || !ReferenceEquals(operands.Right, columns) ||
            input is not null && !ReferenceEquals(operands.Left, input) ||
            FiniteLaw.TryRead(Law, out _, out _, out var outputDomain, out _) && !ReferenceEquals(((Relation)read.Left).Right, outputDomain) ||
            E(pending).Any(item => item is not Relation cause || !ReferenceEquals(cause.Left, publication) || !ReferenceEquals(cause.Right, read)) ||
            !FiniteLaw.TryReadResult(read, out var actualSupport, out var actualResidue, out var actualRows) ||
            !FiniteLaw.TryReadResult(expectedRead, out var expectedSupport, out var expectedResidue, out var expectedRows) ||
            !ScopeSame(actualSupport, expectedSupport) || E(actualResidue).Length != E(expectedResidue).Length ||
            !E(actualResidue).Zip(E(expectedResidue)).All(pair => EvidenceSame(pair.First, pair.Second)) ||
            !ScopeSame(actualRows, expectedRows)) return false;
        selected = options; blocked = refused; unresolved = pending; return true;

        void Include(IElement element)
        {
            if (!atoms.Add(element)) return;
            if (element is Relation relation) { Include(relation.Left); Include(relation.Right); }
        }
        bool EvidenceSame(IElement actual, IElement expectedEvidence)
        {
            if (atoms.Contains(expectedEvidence)) return Same(actual, expectedEvidence);
            if (actual is Relation a && expectedEvidence is Relation b)
                return EvidenceSame(a.Left, b.Left) && EvidenceSame(a.Right, b.Right);
            if (actual is not Placeholder x || expectedEvidence is not Placeholder y) return false;
            if (inputSlots.Any(slot => slot.SameSlotAs(y))) return x.Equals(y);
            if (x.IsInverse != y.IsInverse || inputSlots.Any(slot => slot.SameSlotAs(x))) return false;
            var known = freshSlots.FirstOrDefault(pair => pair.Expected.SameSlotAs(y));
            if (known.Expected is not null) return known.Actual.SameSlotAs(x);
            if (freshSlots.Any(pair => pair.Actual.SameSlotAs(x))) return false;
            freshSlots.Add((y, x)); return true;
        }
    }

    private static bool ScopeSame(Relation left, Relation right) => Closed(left) == Closed(right) &&
        E(left).Length == E(right).Length && E(left).Zip(E(right)).All(pair => Same(pair.First, pair.Second));
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}

public sealed class ConnectedPatternTransform
{
    // PhaseMap is an explicit proposed phase permutation alongside the graph
    // map. EvaluateMap certifies site/edge/chord correspondence only; it does
    // not certify preservation of producer projections or receiver laws.
    public string Name { get; }
    public Relation SiteMap { get; }
    public Relation StrokeMap { get; }
    public Relation ChordMap { get; }
    public Relation PhaseMap { get; }
    public Relation Read { get; }
    public Relation Surface { get; }
    internal ConnectedPatternTransform(string name, QualitativeStrokeGraph graph, Relation sites, Relation strokes,
        Relation chords, Relation phases)
    {
        Name = name; SiteMap = sites; StrokeMap = strokes; ChordMap = chords; PhaseMap = phases;
        Read = graph.EvaluateMap(graph, sites, strokes, chords);
        Surface = new Relation(new Relation(graph.Surface, graph.Surface),
            new Relation(new Relation(sites, strokes), new Relation(new Relation(chords, phases), Read)));
    }
}

/// <summary>Presentation over an independently connected stroke graph and its
/// actual coarse publications, receiver laws, deliveries and rejected actions.
/// Column and level identities are ordered landmarks, not source coordinates.</summary>
public sealed class ConnectedPatternEntry
{
    // coordinates = Closed(actual-site -> (actual-column -> actual-level))
    // column-cycle explicitly includes the last -> first seam.
    // producer = (private-phase-template -> projection-law) -> source-history
    // output fanout = publication -> Closed(receiver-connection-surfaces)
    // selection = schedule-delivery -> Closed(actual receiver receipts)
    public string Name { get; }
    public QualitativeStrokeGraph Graph { get; }
    public Relation Columns { get; }
    public Relation Levels { get; }
    public Relation ColumnOrder { get; }
    public Relation ColumnCycle { get; }
    public Relation LevelOrder { get; }
    public Relation ColumnSteps { get; }
    public Relation LevelSteps { get; }
    public Relation Period => ColumnCycle;
    public Relation PeriodWord => Graph.PeriodWord;
    public Relation Coordinates { get; }
    public Relation SiteBindings => Coordinates;
    public Relation Publications { get; }
    public Relation Connections { get; }
    public Relation Selections { get; }
    public Relation ConditionalReceipts { get; }
    public Relation Producers { get; }
    public Relation Unresolved { get; }
    public Relation Transforms { get; }
    public IReadOnlyList<ConnectedPatternTransform> Correspondences { get; }
    public Relation Surface { get; }
    public CoarsePatternConnection MainConnection { get; }
    public CoarsePatternConnection? DecorationConnection { get; }

    internal ConnectedPatternEntry(string name, QualitativeStrokeGraph graph, Relation columns, Relation levels,
        Relation columnOrder, Relation columnCycle, Relation levelOrder, Relation coordinates, Relation publications,
        Relation connections, Relation selections, Relation conditionals, Relation producers, Relation unresolved,
        CoarsePatternConnection main, CoarsePatternConnection? decoration, ConnectedPatternTransform[] transforms,
        Relation columnSteps, Relation levelSteps)
    {
        Name = name; Graph = graph; Columns = columns; Levels = levels; ColumnOrder = columnOrder;
        ColumnCycle = columnCycle; LevelOrder = levelOrder; Coordinates = coordinates; Publications = publications;
        ColumnSteps = columnSteps; LevelSteps = levelSteps;
        Connections = connections; Selections = selections; ConditionalReceipts = conditionals; Producers = producers;
        Unresolved = unresolved; MainConnection = main; DecorationConnection = decoration;
        Correspondences = Array.AsReadOnly(transforms); Transforms = S(transforms.Select(transform => (IElement)transform.Surface).ToArray());
        Surface = new Relation(new Relation(graph.Surface, new Relation(coordinates, new Relation(columns, levels))),
            new Relation(new Relation(new Relation(columnOrder, new Relation(columnCycle, graph.PeriodWord)),
                new Relation(levelOrder, new Relation(columnSteps, levelSteps))),
                new Relation(new Relation(producers, connections),
                    new Relation(new Relation(publications, selections), new Relation(conditionals, new Relation(Transforms, unresolved))))));
    }
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

/// <summary>Curated elementary graph generators, composed through coarse ports.
/// Each option is an independently placed set of actual strokes; no global
/// cursor or rhythm-to-macro expansion is used. Finite candidate variants are
/// authored changes of exposed phase/conditional bindings, not random search.</summary>
public sealed class ConnectedPatternLibrary
{
    public QualitativeMotionFrame Frame { get; }
    public IElement HorizontalReach { get; }
    public IElement VerticalReach { get; }
    public IElement CoarseBasis { get; }
    public Relation SignalDomain { get; }
    public IReadOnlyList<ConnectedPatternEntry> Entries { get; }
    public IReadOnlyList<ConnectedPatternEntry> Patterns => Entries;
    public IReadOnlyList<ConnectedPatternEntry> Candidates { get; }
    public ConnectedPatternEntry BlockedExample => Entries[^1];
    public ConnectedPatternEntry UnknownExample { get; }
    public Relation Surface { get; }

    // Builder records are discarded adapters. Native ordered sites, chord
    // participation, finite rows, publications and maps retain every choice.
    private readonly record struct Edge(int FromColumn, int FromLevel, int ToColumn, int ToLevel);
    private sealed record Plan(string Name, int Period, Edge[][] Cells, bool Decorations = false);

    public ConnectedPatternLibrary(QualitativeMotionFrame? frame = null,
        IElement? horizontalReach = null, IElement? verticalReach = null)
    {
        Frame = frame ?? new QualitativeMotionFrame(); HorizontalReach = horizontalReach ?? new Placeholder();
        VerticalReach = verticalReach ?? new Placeholder();
        CoarseBasis = new Placeholder(); SignalDomain = S(new Placeholder(), new Placeholder());
        var plans = Plans();
        var entries = plans.Select(plan => Build(plan, flip: false)).ToArray();
        Entries = Array.AsReadOnly(entries);
        Candidates = Array.AsReadOnly(entries.Concat(plans.Select(plan => Build(plan, flip: true))).ToArray());
        UnknownExample = Build(plans[^1], flip: false, unknownAt: 1);
        Surface = new Relation(new Relation(Frame.Surface, new Relation(S(HorizontalReach, VerticalReach), new Relation(CoarseBasis, SignalDomain))),
            new Relation(S(Candidates.Select(entry => (IElement)entry.Surface).ToArray()), UnknownExample.Surface));
    }

    private ConnectedPatternEntry Build(Plan plan, bool flip, int unknownAt = -1)
    {
        var columns = Enumerable.Range(0, plan.Period).Select(_ => (IElement)new Placeholder()).ToArray();
        var levels = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        var columnScope = S(columns); var levelScope = S(levels);
        var columnEdges = Enumerable.Range(1, columns.Length - 1).Select(i => (IElement)new Relation(columns[i - 1], columns[i])).ToArray();
        var cycle = S([.. columnEdges, new Relation(columns[^1], columns[0])]);
        var levelOrder = S(Enumerable.Range(1, 3).Select(i => (IElement)new Relation(levels[i - 1], levels[i])).ToArray());
        var sites = new IElement[columns.Length, 4]; var coordinates = new List<IElement>();
        for (var x = 0; x < columns.Length; x++)
            for (var y = 0; y < levels.Length; y++)
            { sites[x, y] = new Placeholder(); coordinates.Add(new Relation(sites[x, y], new Relation(columns[x], levels[y]))); }
        var source = new Placeholder(); var receiver = new Placeholder(); var basis = CoarseBasis;
        var positive = E(SignalDomain)[0]; var negative = E(SignalDomain)[1]; var signals = SignalDomain;
        var port = ConditionalPassage.Decision(source, receiver, basis, signals);
        var right = QualitativeMotionFrame.Move(Frame.Right, HorizontalReach);
        var left = QualitativeMotionFrame.Move(Frame.Left, HorizontalReach);
        var up = QualitativeMotionFrame.Move(Frame.Up, VerticalReach);
        var down = QualitativeMotionFrame.Move(Frame.Down, VerticalReach);
        var wordCache = new Dictionary<(int X, int Y), Relation>();
        var edgeDescriptions = new Dictionary<Relation, Edge>(ReferenceEqualityComparer.Instance);
        Relation Word(int dx, int dy)
        {
            if (wordCache.TryGetValue((dx, dy), out var known)) return known;
            var moves = Enumerable.Repeat(dx >= 0 ? right : left, Math.Abs(dx)).Cast<IElement>()
                .Concat(Enumerable.Repeat(dy >= 0 ? up : down, Math.Abs(dy)));
            var result = S(moves.ToArray()); wordCache.Add((dx, dy), result); return result;
        }
        Relation Stroke(Edge edge)
        {
            // The seam uses the actual first site again, with positive chord
            // participation retaining its forward period crossing.
            var end = edge.ToColumn == columns.Length ? 0 : edge.ToColumn;
            var row = QualitativeStrokeGraph.Stroke(new Placeholder(), sites[edge.FromColumn, edge.FromLevel],
                sites[end, edge.ToLevel], Word(edge.ToColumn - edge.FromColumn, edge.ToLevel - edge.FromLevel));
            edgeDescriptions.Add(row, edge); return row;
        }
        var options = new List<IElement>(); var mainRows = new List<Relation>();
        var decorationOptions = new List<IElement>(); var decorationRows = new List<Relation>();
        for (var x = 0; x < columns.Length; x++)
        {
            var original = S(plan.Cells[x].Select(edge => (IElement)Stroke(edge)).ToArray());
            var opposed = S(plan.Cells[x].Select(edge => (IElement)Stroke(edge with { FromLevel = 3 - edge.FromLevel, ToLevel = 3 - edge.ToLevel })).ToArray());
            options.Add(original); options.Add(opposed);
            // The authored alternating source exposes actual participants.
            // Variant edits change these bindings, never a host scheduler.
            var authored = (x & 1) == 0 ? positive : negative;
            mainRows.Add(FiniteLaw.Row(authored, columns[x], S(original)));
            mainRows.Add(FiniteLaw.Row(ReferenceEquals(authored, positive) ? negative : positive, columns[x], S(opposed)));
            if (!plan.Decorations) continue;
            var branch = S(Stroke(new Edge(x, 1, x, 2)), Stroke(new Edge(x, 2, x, 3)));
            decorationOptions.Add(branch);
            decorationRows.Add(FiniteLaw.Row(positive, columns[x], S(branch)));
            decorationRows.Add(FiniteLaw.Row(negative, columns[x], S()));
        }
        var law = FiniteLaw.Create(signals, columnScope, S(options.ToArray()), mainRows.ToArray());
        var main = new CoarsePatternConnection(port, columnScope, law);
        var decorationSource = new Placeholder(); var decorationReceiver = new Placeholder();
        var decorationPort = ConditionalPassage.Decision(decorationSource, decorationReceiver, basis, signals);
        CoarsePatternConnection? decoration = plan.Decorations ? new(decorationPort, columnScope,
            FiniteLaw.Create(signals, columnScope, S(decorationOptions.ToArray()), decorationRows.ToArray())) : null;

        // Private recurrence is a supplier implementation. Its projection law
        // and exact history are retained on the supplier side of the interface.
        // Publications carry opaque supplier evidence; receiver reads do not
        // receive, inspect or refresh this private phase support.
        var privatePhases = columns.Select(_ => (IElement)new Placeholder()).ToArray();
        var privateDomain = S(privatePhases);
        var privateEdges = privatePhases.Select((phase, i) => (IElement)new Relation(phase, privatePhases[i + 1 == privatePhases.Length ? 0 : i + 1])).ToArray();
        var clock = new PhaseRecurrence(privateDomain, S(privateEdges)); var probe = new Placeholder(); var probes = S(probe);
        var projection = FiniteLaw.Create(privateDomain, probes, signals, privatePhases.Select((phase, i) =>
            FiniteLaw.Row(phase, probe, S(((i & 1) == 0) != (flip && i == 1) ? positive : negative))).ToArray());
        var producerOrigin = new Placeholder(); var producerEvents = columns.Select(_ => (IElement)new Placeholder()).ToArray();
        var receiverOrigin = new Placeholder(); var receiverEvents = columns.Select(_ => (IElement)new Placeholder()).ToArray();
        Relation Schedule(IElement origin, IElement[] events) => S(events.Select((current, i) =>
            (IElement)new Relation(i == 0 ? origin : events[i - 1], current)).ToArray());
        var schedule = new EventScheduleConnection(source, receiver, Schedule(producerOrigin, producerEvents), Schedule(receiverOrigin, receiverEvents),
            S(producerEvents.Zip(receiverEvents).Select(pair => (IElement)EventScheduleConnection.Window(pair.First, pair.Second)).ToArray()));
        var privateState = clock.Initial(source, producerOrigin, S(privatePhases[^1]));
        var receivingState = schedule.Initial(receiverOrigin); var publications = new List<IElement>();
        var selections = new List<IElement>(); var conditionals = new List<IElement>(); var privateHistory = new List<IElement>();
        var committed = new List<IElement>(); var pending = new List<IElement>(); var fanout = new List<IElement>();
        for (var x = 0; x < columns.Length; x++)
        {
            var predecessor = x == 0 ? producerOrigin : producerEvents[x - 1];
            var drive = clock.Drive(source, producerEvents[x], predecessor);
            var phaseReceipt = clock.Step(privateState, drive);
            if (!clock.TryReadAccepted(phaseReceipt, out _, out _, out var phase) ||
                !clock.TryReadStep(phaseReceipt, out _, out _, out var after, out _, out _, out _))
                throw new InvalidOperationException("Authored supplier recurrence must admit its explicit drive.");
            privateState = after;
            var projected = FiniteLaw.Evaluate(projection, S(phase), probes);
            FiniteLaw.TryReadResult(projected, out var exposed, out _, out _);
            if (x == unknownAt) exposed = StructuralConstraint.Open(E(exposed));
            var publication = QualitativePublication.Create(source, receiver, producerEvents[x], basis, signals, exposed, new Placeholder());
            publications.Add(publication); privateHistory.Add(new Relation(phaseReceipt, new Relation(projected, publication)));
            var delivered = schedule.Deliver(receivingState, publication, producerEvents[x], receiverEvents[x]);
            if (!schedule.TryReadAccepted(delivered, out var deliveredPublication, out _, out _) ||
                !ReferenceEquals(deliveredPublication, publication) ||
                !schedule.TryReadDelivery(delivered, out _, out var receiverAfter, out _, out _, out _))
                throw new InvalidOperationException("Authored source delivery must retain an inspectable receipt.");
            receivingState = receiverAfter;
            var receipts = new List<IElement>();
            Commit(main);
            selections.Add(new Relation(delivered, S(receipts.ToArray())));
            fanout.Add(new Relation(publication, S(main.Surface)));

            void Commit(CoarsePatternConnection connection)
            {
                var receipt = connection.Consume(publication, S(columns[x])); receipts.Add(receipt); conditionals.Add(receipt);
                if (!connection.TryReadResult(receipt, out var chosen, out _, out var unknown))
                { pending.Add(receipt); return; }
                if (!Closed(chosen) || E(unknown).Length > 0) { pending.Add(receipt); return; }
                foreach (var selected in E(chosen).OfType<Relation>()) committed.AddRange(E(selected));
            }
        }
        var producerRecords = new List<IElement>
        { new Relation(new Relation(clock.Surface, projection), S(privateHistory.ToArray())) };
        var extraConnections = new List<IElement>();
        if (decoration is not null)
        {
            // This supplier has its own three-state recurrence, event lineage,
            // observation schedule and exposed port. Main's six-state phase is
            // never a decoration input. One of three coarse states permits a
            // branch; two deny it. Six explicit paired deliveries return both
            // reusable phase templates while all occurrence events stay fresh.
            var gatePhases = new IElement[] { new Placeholder(), new Placeholder(), new Placeholder() };
            var gateClock = new PhaseRecurrence(S(gatePhases), S(new Relation(gatePhases[0], gatePhases[1]),
                new Relation(gatePhases[1], gatePhases[2]), new Relation(gatePhases[2], gatePhases[0])));
            var gateProbe = new Placeholder(); var gateProbes = S(gateProbe);
            var gateProjection = FiniteLaw.Create(gateClock.Domain, gateProbes, signals,
                FiniteLaw.Row(gatePhases[0], gateProbe, S(positive)),
                FiniteLaw.Row(gatePhases[1], gateProbe, S(negative)),
                FiniteLaw.Row(gatePhases[2], gateProbe, S(negative)));
            var gateOrigin = new Placeholder(); var gateEvents = columns.Select(_ => (IElement)new Placeholder()).ToArray();
            var gateReceiverOrigin = new Placeholder(); var gateReceiverEvents = columns.Select(_ => (IElement)new Placeholder()).ToArray();
            var gateSchedule = new EventScheduleConnection(decorationSource, decorationReceiver,
                Schedule(gateOrigin, gateEvents), Schedule(gateReceiverOrigin, gateReceiverEvents),
                S(gateEvents.Zip(gateReceiverEvents).Select(pair => (IElement)EventScheduleConnection.Window(pair.First, pair.Second)).ToArray()));
            var gateState = gateClock.Initial(decorationSource, gateOrigin, S(gatePhases[^1]));
            var gateReceiving = gateSchedule.Initial(gateReceiverOrigin); var gateHistory = new List<IElement>();
            for (var x = 0; x < columns.Length; x++)
            {
                var before = x == 0 ? gateOrigin : gateEvents[x - 1];
                var drive = gateClock.Drive(decorationSource, gateEvents[x], before);
                var gateReceipt = gateClock.Step(gateState, drive);
                if (!gateClock.TryReadAccepted(gateReceipt, out _, out _, out var gatePhase) ||
                    !gateClock.TryReadStep(gateReceipt, out _, out _, out var gateAfter, out _, out _, out _))
                    throw new InvalidOperationException("Authored independent gate recurrence must admit its explicit drive.");
                gateState = gateAfter;
                var gateRead = FiniteLaw.Evaluate(gateProjection, S(gatePhase), gateProbes);
                FiniteLaw.TryReadResult(gateRead, out var gateSupport, out _, out _);
                var publication = QualitativePublication.Create(decorationSource, decorationReceiver, gateEvents[x], basis,
                    signals, gateSupport, new Placeholder());
                publications.Add(publication); gateHistory.Add(new Relation(gateReceipt, new Relation(gateRead, publication)));
                var delivery = gateSchedule.Deliver(gateReceiving, publication, gateEvents[x], gateReceiverEvents[x]);
                if (!gateSchedule.TryReadAccepted(delivery, out var deliveredPublication, out _, out _) ||
                    !ReferenceEquals(deliveredPublication, publication) ||
                    !gateSchedule.TryReadDelivery(delivery, out _, out var gateReceivingAfter, out _, out _, out _))
                    throw new InvalidOperationException("Authored independent gate delivery must be admitted.");
                gateReceiving = gateReceivingAfter;
                var receipt = decoration.Consume(publication, S(columns[x])); conditionals.Add(receipt);
                selections.Add(new Relation(delivery, S(receipt))); fanout.Add(new Relation(publication, S(decoration.Surface)));
                if (!decoration.TryReadResult(receipt, out var chosen, out _, out var unresolved) || !Closed(chosen) || E(unresolved).Length > 0)
                { pending.Add(receipt); continue; }
                foreach (var selected in E(chosen).OfType<Relation>()) committed.AddRange(E(selected));
            }
            producerRecords.Add(new Relation(new Relation(gateClock.Surface, gateProjection), S(gateHistory.ToArray())));
            extraConnections.Add(decoration.Surface); extraConnections.Add(gateSchedule.Surface);
        }
        var periodWord = S(Enumerable.Repeat<IElement>(right, columns.Length).ToArray());
        var graph = new QualitativeStrokeGraph(Frame, S(sites.Cast<IElement>().ToArray()),
            pending.Count == 0 ? S(committed.ToArray()) : StructuralConstraint.Open(committed.ToArray()), periodWord);
        var transforms = new List<ConnectedPatternTransform>();
        // These are supplied native graph correspondences. They do not assert
        // that a future metric interpretation is an isometry or a color swap.
        AddTransform("Level reflection", shift: 0, reflect: true);
        if (columns.Length % 2 == 0)
        {
            AddTransform("Half-cycle translation", columns.Length / 2, reflect: false);
            AddTransform("Half-cycle glide", columns.Length / 2, reflect: true);
        }
        AddTransform("One-column glide", shift: 1, reflect: true);
        void AddTransform(string name, int shift, bool reflect)
        {
            if (pending.Count != 0) return;
            var siteMap = new List<IElement>(); var strokeMap = new List<IElement>(); var chordMap = new List<IElement>();
            int Shift(int x) => x + shift < columns.Length ? x + shift : x + shift - columns.Length;
            for (var x = 0; x < columns.Length; x++)
                for (var y = 0; y < levels.Length; y++)
                    siteMap.Add(new Relation(sites[x, y], sites[Shift(x), reflect ? 3 - y : y]));
            foreach (var sourceStroke in committed.Cast<Relation>())
            {
                var edge = edgeDescriptions[sourceStroke]; var targetStart = sites[Shift(edge.FromColumn), reflect ? 3 - edge.FromLevel : edge.FromLevel];
                var targetEnd = sites[Shift(edge.ToColumn == columns.Length ? 0 : edge.ToColumn), reflect ? 3 - edge.ToLevel : edge.ToLevel];
                var dx = edge.ToColumn - edge.FromColumn; var dy = (edge.ToLevel - edge.FromLevel) * (reflect ? -1 : 1);
                if (!wordCache.TryGetValue((dx, dy), out var targetWord)) return;
                var targets = committed.Cast<Relation>().Where(target => QualitativeStrokeGraph.TryReadStroke(target, out _, out var from, out var to, out var word) &&
                    ReferenceEquals(from, targetStart) && ReferenceEquals(to, targetEnd) && ReferenceEquals(word, targetWord)).ToArray();
                if (targets.Length != 1) return;
                strokeMap.Add(new Relation(sourceStroke, targets[0]));
                QualitativeStrokeGraph.TryReadStroke(sourceStroke, out _, out _, out _, out var sourceWord);
                if (!chordMap.Cast<Relation>().Any(map => ReferenceEquals(map.Left, sourceWord))) chordMap.Add(new Relation(sourceWord, targetWord));
            }
            var phaseMap = S(privatePhases.Select((phase, x) => (IElement)new Relation(phase, privatePhases[Shift(x)])).ToArray());
            transforms.Add(new ConnectedPatternTransform(name, graph, S(siteMap.ToArray()), S(strokeMap.ToArray()), S(chordMap.ToArray()), phaseMap));
        }
        var connections = S(new IElement[] { main.Surface, schedule.Surface, S(fanout.ToArray()) }.Concat(extraConnections).ToArray());
        // Adjacent landmarks share an unmeasured pitch promise. Ordering alone
        // would not establish equal gaps; these explicit step-to-word bindings
        // do. A numeric pitch remains a separately selected interpretation.
        var columnSteps = S(E(cycle).Select(edge => (IElement)new Relation(edge, Word(1, 0))).ToArray());
        var levelSteps = S(E(levelOrder).Select(edge => (IElement)new Relation(edge, Word(0, 1))).ToArray());
        return new ConnectedPatternEntry(plan.Name + (flip ? " — authored one-phase variation" : "") + (unknownAt >= 0 ? " — unknown publication" : ""),
            graph, columnScope, levelScope, S(columnEdges), cycle, levelOrder, S(coordinates.ToArray()), S(publications.ToArray()),
            connections, S(selections.ToArray()), S(conditionals.ToArray()), S(producerRecords.ToArray()), S(pending.ToArray()), main, decoration, transforms.ToArray(), columnSteps, levelSteps);
    }

    private static Plan[] Plans()
    {
        static Edge[][] Cells(int period, params Edge[] edges) => Enumerable.Range(0, period)
            .Select(x => edges.Where(edge => edge.FromColumn == x).ToArray()).ToArray();
        static Edge[] Path(params int[] levels) => levels.Select((y, x) => new Edge(x, y, x + 1, levels[x + 1 == levels.Length ? 0 : x + 1])).ToArray();
        static Edge Mirror(Edge edge) => edge with { FromLevel = 3 - edge.FromLevel, ToLevel = 3 - edge.ToLevel };
        var chevron = Path(0, 1, 2, 3, 2, 1);
        var paired = chevron.Concat(chevron.Select(Mirror)).ToArray();
        var diamond = Path(1, 2, 1, 2).Concat(Path(1, 0, 1, 0)).Concat(new[]
        { new Edge(1, 2, 1, 3), new Edge(3, 2, 3, 3), new Edge(1, 3, 2, 3), new Edge(2, 3, 3, 3), new Edge(3, 3, 4, 3), new Edge(0, 3, 1, 3) }).ToArray();
        var braces = Enumerable.Range(0, 4).SelectMany(x => new[]
        { new Edge(x, 0, x + 1, 3), new Edge(x, 3, x + 1, 0), new Edge(x, 1, x + 1, 1), new Edge(x, 2, x + 1, 2),
            new Edge(x, 0, x, 1), new Edge(x, 1, x, 2), new Edge(x, 2, x, 3) }).ToArray();
        var forks = Enumerable.Range(0, 6).SelectMany(x => x % 2 == 0
            ? new[] { new Edge(x, 1, x + 1, 1), new Edge(x, 1, x, 2), new Edge(x, 2, x, 3) }
            : new[] { new Edge(x, 1, x + 1, 1), new Edge(x, 1, x, 0) }).ToArray();
        var glide = Enumerable.Range(0, 4).SelectMany(x => new[]
        { new Edge(x, 1, x + 1, 1), new Edge(x, 2, x + 1, 2),
            x % 2 == 0 ? new Edge(x, 1, x + 1, 3) : new Edge(x, 2, x + 1, 0) }).ToArray();
        var terrace = Path(0, 0, 1, 1, 2, 3, 3, 1);
        var conditional = Enumerable.Range(0, 6).Select(x => new Edge(x, 1, x + 1, 1)).ToArray();
        return
        [
            new("Four-level chevron", 6, Cells(6, chevron)),
            new("Coupled counter-moving chevrons", 6, Cells(6, paired)),
            new("Diamond lattice with upper branches", 4, Cells(4, diamond)),
            new("Four-level crossed braces", 4, Cells(4, braces)),
            new("Alternating forked ladder", 6, Cells(6, forks)),
            new("Glide-related diagonal fans", 4, Cells(4, glide)),
            new("Terraced diagonal bays", 8, Cells(8, terrace)),
            new("Conditionally pruned ladder", 6, Cells(6, conditional), Decorations: true)
        ];
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}
