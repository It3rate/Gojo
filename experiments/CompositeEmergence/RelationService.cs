using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace CompositeEmergence;

/// <summary>Bounded discovery of a reusable relational feature. The learner
/// predicts only a later physical consequence from its candidate feature and
/// an independently published action. It has no hidden-mode input.</summary>
public sealed class RelationService
{
    // source = G(exposed input port, temporal offset point).
    // candidate = G(native equation, ordered sources, empirical outcome rows,
    //               prequential signed-correctness ledger, complexity point).
    // row = G(G(feature point, action point), G(outcome point -> count ...)).
    // model = G(candidates, selected-index point -> selected candidate,
    //           G(trial count, expansion count, latest structural edit)).
    // Root = G(contract, model, operational memory, bounded receipts, forgetting).
    // LearnedRoot = G(contract, frozen parameter-only model).
    private readonly Carrier countAxis, scoreAxis, timeAxis, trials, expanded, forgotten;
    private readonly Relation contract, portA, portB;
    private readonly bool memoryEnabled, expansionEnabled;
    private readonly int orderSeed;
    private readonly List<Relation> candidates = [], receipts = [];
    private Relation currentA, currentB, priorA, priorB, lastFeature, lastPrediction, edit, retention;
    private int selectedIndex;
    public Carrier InputAAxis { get; }
    public Carrier InputBAxis { get; }
    public Carrier FeatureAxis { get; }
    public Carrier ActionAxis { get; }
    public Carrier OutcomeAxis { get; }
    public Relation Root { get; private set; }
    public Relation LearnedRoot => NativeGraph.G(contract, FrozenModel(Model()));
    public int CandidateCount => candidates.Count;
    public int Expansions => checked((int)expanded.Value);
    public long TrainingTrials => checked((long)trials.Value);
    public string SelectedDescription => Describe(candidates[selectedIndex]);
    public IReadOnlyList<RelationCandidateDiagnostic> CandidateDiagnostics => candidates.Select((candidate, index) =>
    {
        var f = NativeGraph.Parts(candidate, 5); var ledger = (Relation)f[3]; var cells = NativeGraph.Parts(ledger, 3).Cast<Carrier>().ToArray();
        var score = NativeGraph.Score(ledger, scoreAxis, Complexity(candidate));
        return new RelationCandidateDiagnostic(index, Describe(candidate), checked((long)cells[0].Value), checked((long)cells[1].Value),
            checked((long)cells[2].Value), Rows(candidate).Length, Complexity(candidate),
            NativeGraph.TryValue(score, scoreAxis, out var point) ? point.Ticks : null, index == selectedIndex);
    }).ToArray();

    public RelationService(Carrier? inputAAxis = null, Carrier? inputBAxis = null, Carrier? actionAxis = null,
        Carrier? outcomeAxis = null, bool memoryEnabled = true, bool expansionEnabled = true, int candidateOrderSeed = 0)
    {
        InputAAxis = inputAAxis ?? new Carrier(); InputBAxis = inputBAxis ?? new Carrier(); ActionAxis = actionAxis ?? new Carrier();
        OutcomeAxis = outcomeAxis ?? new Carrier(); FeatureAxis = new Carrier(); countAxis = new Carrier(); scoreAxis = new Carrier(); timeAxis = new Carrier();
        trials = new Carrier(countAxis.UnitShape); expanded = new Carrier(countAxis.UnitShape); forgotten = new Carrier(countAxis.UnitShape);
        this.memoryEnabled = memoryEnabled; this.expansionEnabled = expansionEnabled; orderSeed = candidateOrderSeed;
        portA = new Relation(new Carrier(), Relation.Segment(InputAAxis, -1, 1)); portB = new Relation(new Carrier(), Relation.Segment(InputBAxis, -1, 1));
        contract = NativeGraph.G(InputAAxis, InputBAxis, FeatureAxis, ActionAxis, OutcomeAxis, countAxis, scoreAxis, timeAxis,
            NativeGraph.G(portA, portB), NativeGraph.G(Relation.Point(countAxis, 24), Relation.Point(countAxis, 16), Relation.Point(countAxis, 64), Relation.Point(countAxis, 8),
                Equation.Affine(countAxis, 0, 2, countAxis, 0, 1)),
            NativeGraph.G(Relation.Point(timeAxis, memoryEnabled ? 1 : 0), Relation.Point(timeAxis, expansionEnabled ? 1 : 0), Relation.Point(timeAxis, candidateOrderSeed)));
        foreach (var source in Sources())
        {
            var input = Axis(source); candidates.Add(Candidate(Equation.Affine(input, 0, 1, FeatureAxis, 0, 1), [source], 1));
        }
        edit = NativeGraph.Open(NativeGraph.G(candidates.ToArray()), Relation.Point(countAxis, 16));
        retention = NativeGraph.Open(contract, Relation.Point(countAxis, 0));
        currentA = currentB = priorA = priorB = lastFeature = lastPrediction = NativeGraph.Open(contract, new Placeholder());
        Root = Build();
    }
    private RelationService(Relation root)
    {
        var fields = NativeGraph.Parts(root, 5); contract = (Relation)fields[0]; var p = NativeGraph.Parts(contract, 11);
        InputAAxis = (Carrier)p[0]; InputBAxis = (Carrier)p[1]; FeatureAxis = (Carrier)p[2]; ActionAxis = (Carrier)p[3]; OutcomeAxis = (Carrier)p[4];
        countAxis = (Carrier)p[5]; scoreAxis = (Carrier)p[6]; timeAxis = (Carrier)p[7]; var ports = NativeGraph.Parts((Relation)p[8], 2);
        portA = (Relation)ports[0]; portB = (Relation)ports[1]; var switches = NativeGraph.Parts((Relation)p[10], 3);
        NativeGraph.TryPoint(switches[0], out var memory); NativeGraph.TryPoint(switches[1], out var expansion); NativeGraph.TryPoint(switches[2], out var seed);
        memoryEnabled = memory.Ticks == 1; expansionEnabled = expansion.Ticks == 1; orderSeed = checked((int)seed.Ticks);
        var model = NativeGraph.Parts((Relation)fields[1], 3); candidates.AddRange(NativeGraph.Parts((Relation)model[0]).Cast<Relation>());
        if (model[1] is not Relation binding || !NativeGraph.TryPoint(binding.Left, out var index) || index.Ticks < 0 || index.Ticks >= candidates.Count ||
            !ReferenceEquals(binding.Right, candidates[(int)index.Ticks])) throw new ArgumentException("Selected candidate binding is not locally readable.", nameof(root));
        selectedIndex = checked((int)index.Ticks); var expansionState = NativeGraph.Parts((Relation)model[2], 3);
        trials = (Carrier)expansionState[0]; expanded = (Carrier)expansionState[1]; edit = (Relation)expansionState[2];
        var operational = NativeGraph.Parts((Relation)fields[2], 7); currentA = (Relation)operational[0]; currentB = (Relation)operational[1];
        priorA = (Relation)operational[2]; priorB = (Relation)operational[3]; lastFeature = (Relation)operational[4]; lastPrediction = (Relation)operational[5];
        retention = (Relation)operational[6];
        receipts.AddRange(NativeGraph.Parts((Relation)fields[3]).Cast<Relation>()); forgotten = (Carrier)fields[4]; Root = root;
    }
    public static RelationService FromRoot(Relation root) => new(root);
    public static RelationService FromLearnedRoot(Relation learned, bool memoryEnabled = true)
    {
        var f = NativeGraph.Parts(learned, 2); var contract = (Relation)f[0]; var p = NativeGraph.Parts(contract, 11);
        var options = NativeGraph.Parts((Relation)p[10], 3); NativeGraph.TryPoint(options[0], out var oldMemory);
        // A deployment can explicitly withdraw temporal retention while keeping
        // the same frozen algorithm. This changes its visible contract only.
        p[10] = NativeGraph.G(Relation.Point(oldMemory.Carrier, memoryEnabled ? 1 : 0), options[1], options[2]);
        var deployedContract = NativeGraph.G(p); var open = NativeGraph.Open(deployedContract, new Placeholder());
        return new RelationService(NativeGraph.G(deployedContract, FrozenModel((Relation)f[1]), NativeGraph.G(open, open, open, open, open, open, open), NativeGraph.G(), new Carrier()));
    }
    public Relation AcceptA(Relation publication)
    {
        priorA = currentA; priorB = currentB;
        currentA = ReadSensor(publication, InputAAxis); currentB = NativeGraph.Open(portB, Relation.Segment(InputBAxis, -1, 1));
        lastFeature = lastPrediction = NativeGraph.Open(currentA, portB); Root = Build(); return currentA;
    }
    public Relation AcceptB(Relation publication)
    {
        currentB = ReadSensor(publication, InputBAxis);
        if (!memoryEnabled) currentA = NativeGraph.Open(portA, NativeGraph.G(currentB, Relation.Point(timeAxis, -1)));
        if (NativeGraph.TryTimestamp(currentA, out var aTime) && NativeGraph.TryTimestamp(currentB, out var bTime) && bTime < aTime)
            currentB = NativeGraph.Open(NativeGraph.Identity(publication), NativeGraph.G(portB, currentA));
        lastFeature = lastPrediction = NativeGraph.Open(NativeGraph.G(currentA, currentB), candidates[selectedIndex]); Root = Build(); return currentB;
    }
    public Relation PublishFeature()
    {
        var calculation = Feature(candidates[selectedIndex]); NativeGraph.TryTimestamp(currentB, out var time);
        var boundary = new Carrier();
        var publication = NativeGraph.TryValue(calculation, FeatureAxis, out var value)
            ? NativeGraph.Publication(FeatureAxis, value.Ticks, boundary, time)
            : NativeGraph.Open(boundary, Relation.Segment(FeatureAxis, -16, 16));
        // The derivation remains private. The advertised service physically
        // withholds raw input ancestry, not merely by consumer convention.
        lastFeature = ReadWitness.Create(new Carrier(), calculation, candidates[selectedIndex].Left,
            NativeGraph.TryValue(publication, FeatureAxis, out var exposed) ? exposed : new Placeholder(), new Placeholder(), publication);
        Root = Build(); return publication;
    }
    public Relation PredictOutcome(Relation action)
    {
        lastFeature = Feature(candidates[selectedIndex]); lastPrediction = Prediction(candidates[selectedIndex], lastFeature, action);
        Root = Build(); return lastPrediction;
    }
    public Relation Learn(Relation actualAction, Relation actualOutcome, bool learn = true)
    {
        var input = NativeGraph.G(NativeGraph.Identity(actualAction), NativeGraph.Identity(actualOutcome));
        if (!learn || !NativeGraph.TryValue(actualAction, ActionAxis, out var action) || action.Ticks is not (-1 or 1) ||
            !NativeGraph.TryValue(actualOutcome, OutcomeAxis, out var outcome) || outcome.Ticks is not (-1 or 1) ||
            !NativeGraph.IsBoundTo(actualOutcome, actualAction, OutcomeAxis) || NativeGraph.ReceiptSeen(receipts, actualAction, actualOutcome))
            return NativeGraph.Open(input, contract);
        var changes = new List<IElement>(); var trained = false;
        for (var index = 0; index < candidates.Count; index++)
        {
            var candidate = candidates[index]; var feature = Feature(candidate);
            if (!NativeGraph.TryValue(feature, FeatureAxis, out var featureValue)) continue;
            var prediction = Prediction(candidate, feature, actualAction); var answered = NativeGraph.TryValue(prediction, OutcomeAxis, out var predicted);
            var correct = answered && predicted.Ticks == outcome.Ticks;
            var f = NativeGraph.Parts(candidate, 5); var ledger = (Relation)f[3]; var before = NativeGraph.CaptureLedger(ledger);
            var ledgerUpdate = NativeGraph.Update(ledger, correct ? 1 : -1);
            var rows = Rows(candidate).ToList(); var row = rows.FirstOrDefault(row => KeyMatches(row, featureValue.Ticks, action.Ticks));
            if (row is null && rows.Count < 32) { row = NewRow(featureValue.Ticks, action.Ticks); rows.Add(row); }
            if (row is null) continue;
            var counts = Counts(row); var old = NativeGraph.G(counts.Select(c => (IElement)new CarrierLocation(c, checked((long)c.Value))).ToArray());
            var agingReads = new List<IElement>();
            if (counts.Sum(c => checked((long)c.Value)) >= 64)
                foreach (var cell in counts)
                {
                    var amount = checked((long)cell.Value); var law = Equation.Affine(cell, 0, 2, cell, 0, 1);
                    var read = Equation.Predict(law, new CarrierLocation(cell, amount), cell);
                    Equation.TryReadPrediction(read, out var target, out var loss); cell.Set(target.Ticks);
                    agingReads.Add(ReadWitness.Create(new Carrier(), new CarrierLocation(cell, amount), law,
                        new CarrierLocation(cell, target.Ticks), loss, NativeGraph.G()));
                }
            counts[outcome.Ticks == -1 ? 0 : 1].Add(1);
            var frozenPredicted = answered ? (IElement)new CarrierLocation(OutcomeAxis, predicted.Ticks) : new Placeholder();
            var claim = answered ? (IElement)OperationClaim.Equality(new Carrier(), new CarrierLocation(OutcomeAxis, outcome.Ticks), frozenPredicted, new Carrier()) : NativeGraph.Open(frozenPredicted, candidate.Left);
            changes.Add(NativeGraph.G(Relation.Point(countAxis, index), Relation.Point(FeatureAxis, featureValue.Ticks), Relation.Point(ActionAxis, action.Ticks),
                before, NativeGraph.CaptureLedger(ledger), old,
                NativeGraph.G(counts.Select(c => (IElement)new CarrierLocation(c, checked((long)c.Value))).ToArray()), claim,
                ledgerUpdate, NativeGraph.G(agingReads.ToArray())));
            f[2] = NativeGraph.G(rows.ToArray()); candidates[index] = NativeGraph.G(f); trained = true;
        }
        if (trained) { trials.Add(1); TryExpand(); RetainBest(); }
        var receipt = ReadWitness.Create(new Carrier(), input, NativeGraph.G(Relation.Point(ActionAxis, action.Ticks), Relation.Point(OutcomeAxis, outcome.Ticks)),
            Relation.Point(countAxis, checked((long)trials.Value)), NativeGraph.G(), NativeGraph.G(NativeGraph.Identity(actualAction), NativeGraph.Identity(actualOutcome), NativeGraph.G(changes.ToArray())));
        receipts.Add(receipt); if (receipts.Count > NativeGraph.ReceiptLimit) { receipts.RemoveAt(0); forgotten.Add(1); }
        Root = Build(); return receipt;
    }
    public void ResetOperational()
    {
        currentA = currentB = priorA = priorB = lastFeature = lastPrediction = NativeGraph.Open(contract, new Placeholder()); receipts.Clear(); Root = Build();
    }
    public Relation CaptureOperational() => NativeGraph.G(currentA, currentB, priorA, priorB, lastFeature, lastPrediction, retention);
    private Relation ReadSensor(Relation publication, Carrier axis) => NativeGraph.TryValue(publication, axis, out var value) && value.Ticks is -1 or 1
        ? NativeGraph.CapturePublication(publication, axis) : NativeGraph.Open(NativeGraph.Identity(publication), Relation.Segment(axis, -1, 1));
    private Relation[] Sources() => [Source(portA, 0), Source(portB, 0), Source(portA, -1), Source(portB, -1)];
    private Relation Source(Relation port, long offset) => NativeGraph.G(port, Relation.Point(timeAxis, offset));
    private static Carrier Axis(Relation source)
    {
        var port = (Relation)NativeGraph.Parts(source, 2)[0]; var frame = (Relation)port.Right;
        return ((CarrierLocation)((Relation)frame.Left).Left).Carrier;
    }
    private Relation Resolve(Relation source)
    {
        var f = NativeGraph.Parts(source, 2); NativeGraph.TryPoint(f[1], out var offset);
        if (ReferenceEquals(f[0], portA)) return offset.Ticks == 0 ? currentA : memoryEnabled ? priorA : NativeGraph.Open(source, contract);
        if (ReferenceEquals(f[0], portB)) return offset.Ticks == 0 ? currentB : memoryEnabled ? priorB : NativeGraph.Open(source, contract);
        return NativeGraph.Open(source, contract);
    }
    private Relation Candidate(Relation equation, Relation[] sources, int complexity) => NativeGraph.G(equation, NativeGraph.G(sources), NativeGraph.G(),
        NativeGraph.Ledger(countAxis, scoreAxis), Relation.Point(countAxis, complexity));
    private Relation Feature(Relation candidate)
    {
        var f = NativeGraph.Parts(candidate, 5); var sources = NativeGraph.Parts((Relation)f[1]).Cast<Relation>().ToArray();
        var supplied = sources.Select(Resolve).ToArray(); var values = new List<IElement>();
        foreach (var (source, publication) in sources.Zip(supplied))
        {
            if (!NativeGraph.TryValue(publication, Axis(source), out var point)) return NativeGraph.Open(NativeGraph.G(supplied), NativeGraph.G(f[0], f[1]));
            values.Add(new CarrierLocation(point.Carrier, point.Ticks));
        }
        IElement input = values[0]; foreach (var value in values.Skip(1)) input = new Relation(input, value);
        var raw = Equation.Predict((Relation)f[0], input, FeatureAxis);
        if (!Equation.TryReadPrediction(raw, out var feature, out var residue) || feature.Ticks is < -16 or > 16)
            return NativeGraph.Open(NativeGraph.G(supplied), f[0]);
        NativeGraph.TryTimestamp(currentB, out var time);
        // Only stable equation/source ports and captured premises cross this
        // boundary. Learned ledgers and entire upstream roots are not published.
        return ReadWitness.Create(new Carrier(), NativeGraph.G(supplied), NativeGraph.G(f[0], f[1]), new CarrierLocation(FeatureAxis, feature.Ticks), residue,
            NativeGraph.G(new Carrier(), Relation.Point(timeAxis, time), NativeGraph.G(supplied), raw));
    }
    private Relation Prediction(Relation candidate, Relation feature, Relation action)
    {
        if (!NativeGraph.TryValue(feature, FeatureAxis, out var value) || !NativeGraph.TryValue(action, ActionAxis, out var act) || act.Ticks is not (-1 or 1))
            return NativeGraph.Open(NativeGraph.G(feature, action), candidate.Left);
        var row = Rows(candidate).FirstOrDefault(row => KeyMatches(row, value.Ticks, act.Ticks));
        if (row is null) return NativeGraph.Open(NativeGraph.G(feature, NativeGraph.CapturePublication(action, ActionAxis)), candidate.Left);
        var counts = Counts(row); var negative = checked((long)counts[0].Value); var positive = checked((long)counts[1].Value);
        var totalRole = new Carrier(); var marginRole = new Carrier();
        var total = Equation.PredictWitness(new Carrier(), Equation.Sum(countAxis, Equation.UnitPort(counts[0]), Equation.UnitPort(counts[1])),
            new Relation(new CarrierLocation(counts[0], negative), new CarrierLocation(counts[1], positive)), countAxis);
        var margin = Equation.PredictWitness(new Carrier(), Equation.Sum(countAxis, Equation.WeightedPort(counts[0], 0, 1, 0, -1), Equation.UnitPort(counts[1])),
            new Relation(new CarrierLocation(counts[0], negative), new CarrierLocation(counts[1], positive)), countAxis);
        var rules = SupportDecisionSurface.Create(new Carrier(),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(OutcomeAxis, -1), SupportDecisionSurface.Condition(new Carrier(), totalRole, Relation.Segment(countAxis, 4, 64)),
                SupportDecisionSurface.Condition(new Carrier(), marginRole, Relation.Segment(countAxis, -64, -2))),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(OutcomeAxis, 1), SupportDecisionSurface.Condition(new Carrier(), totalRole, Relation.Segment(countAxis, 4, 64)),
                SupportDecisionSurface.Condition(new Carrier(), marginRole, Relation.Segment(countAxis, 2, 64))));
        var decision = SupportDecisionSurface.Select(new Carrier(), rules, SupportDecisionSurface.Binding(new Carrier(), totalRole, total), SupportDecisionSurface.Binding(new Carrier(), marginRole, margin));
        SupportDecisionSurface.TryReadSelection(decision, out _, out var response, out _, out var residue);
        return ReadWitness.Create(new Carrier(), NativeGraph.G(feature, NativeGraph.CapturePublication(action, ActionAxis)),
            NativeGraph.G((Relation)frozenEquation(candidate), NativeGraph.G(Relation.Point(FeatureAxis, value.Ticks), Relation.Point(ActionAxis, act.Ticks))), response, residue,
            NativeGraph.G(new Carrier(), Relation.Point(timeAxis, 0), NativeGraph.G(new CarrierLocation(counts[0], negative), new CarrierLocation(counts[1], positive)), decision));
        static IElement frozenEquation(Relation candidate) => NativeGraph.Parts(candidate, 5)[0];
    }
    private Relation NewRow(long feature, long action)
    {
        var cells = Enumerable.Range(0, 2).Select(_ => new Carrier(countAxis.UnitShape)).ToArray();
        foreach (var cell in cells) cell.Attach(new Relation(Relation.Point(cell, 64), Relation.Point(cell, 64)));
        return NativeGraph.G(NativeGraph.G(Relation.Point(FeatureAxis, feature), Relation.Point(ActionAxis, action)),
            NativeGraph.G(new Relation(Relation.Point(OutcomeAxis, -1), cells[0]), new Relation(Relation.Point(OutcomeAxis, 1), cells[1])));
    }
    private static Relation[] Rows(Relation candidate) => NativeGraph.Parts((Relation)NativeGraph.Parts(candidate, 5)[2]).Cast<Relation>().ToArray();
    private static Carrier[] Counts(Relation row) => NativeGraph.Parts((Relation)NativeGraph.Parts(row, 2)[1], 2).Cast<Relation>().Select(entry => (Carrier)entry.Right).ToArray();
    private static bool KeyMatches(Relation row, long feature, long action)
    {
        var key = NativeGraph.Parts((Relation)NativeGraph.Parts(row, 2)[0], 2);
        return NativeGraph.TryPoint(key[0], out var a) && NativeGraph.TryPoint(key[1], out var b) && a.Ticks == feature && b.Ticks == action;
    }
    private static long Complexity(Relation candidate)
    {
        NativeGraph.TryPoint(NativeGraph.Parts(candidate, 5)[4], out var baseCost);
        return baseCost.Ticks + Rows(candidate).Length;
    }
    private void TryExpand()
    {
        if (!expansionEnabled || expanded.Value > 0 || trials.Value < 64) return;
        var eligible = candidates.Where(c => NativeGraph.Count((Relation)NativeGraph.Parts(c, 5)[3]).Value >= 16).ToArray();
        if (eligible.Length == 0) return;
        var scores = eligible.Select(c => NativeGraph.Score((Relation)NativeGraph.Parts(c, 5)[3], scoreAxis)).ToArray();
        if (scores.Any(s => NativeGraph.TryValue(s, scoreAxis, out var p) && p.Ticks >= 205)) return;
        var sources = Sources(); var generated = new List<Relation>();
        for (var left = 0; left < sources.Length; left++) for (var right = left + 1; right < sources.Length; right++)
        {
            var a = Axis(sources[left]); var b = Axis(sources[right]);
            generated.Add(Candidate(Equation.Product(FeatureAxis, Equation.UnitPort(a), Equation.UnitPort(b)), [sources[left], sources[right]], 3));
            generated.Add(Candidate(Equation.Sum(FeatureAxis, Equation.UnitPort(a), Equation.UnitPort(b)), [sources[left], sources[right]], 3));
            generated.Add(Candidate(Equation.Sum(FeatureAxis, Equation.UnitPort(a), Equation.WeightedPort(b, 0, 1, 0, -1)), [sources[left], sources[right]], 3));
        }
        var random = new Random(orderSeed); generated = generated.OrderBy(_ => random.Next()).ToList();
        var failure = OperationClaim.AtMost(new Carrier(), new CarrierLocation(scoreAxis, scores.Max(score => NativeGraph.TryValue(score, scoreAxis, out var p) ? p.Ticks : -256)),
            new CarrierLocation(scoreAxis, 204), new Carrier());
        candidates.AddRange(generated.Take(24 - candidates.Count)); expanded.Add(1);
        edit = ReadWitness.Create(new Carrier(), NativeGraph.G(scores), failure, NativeGraph.G(generated.Select(c => (IElement)NativeGraph.G(NativeGraph.Parts(c, 5)[0], NativeGraph.Parts(c, 5)[1])).ToArray()),
            NativeGraph.G(), NativeGraph.G(Relation.Point(countAxis, checked((long)trials.Value)), Relation.Point(countAxis, candidates.Count)));
    }
    private void RetainBest()
    {
        Relation? current = null; var scores = new List<IElement>(); var claims = new List<IElement>();
        foreach (var (candidate, index) in candidates.Select((candidate, index) => (candidate, index)))
        {
            var ledger = (Relation)NativeGraph.Parts(candidate, 5)[3]; if (NativeGraph.Count(ledger).Value < 16) continue;
            var score = NativeGraph.Score(ledger, scoreAxis, Complexity(candidate)); scores.Add(NativeGraph.G(Relation.Point(countAxis, index), score));
            if (!NativeGraph.TryValue(score, scoreAxis, out var proposed)) continue;
            if (current is null) { selectedIndex = index; current = score; continue; }
            NativeGraph.TryValue(current, scoreAxis, out var incumbent);
            var claim = OperationClaim.AtLeast(new Carrier(), proposed, new CarrierLocation(scoreAxis, incumbent.Ticks + 1), new Carrier());
            claims.Add(claim);
            if (NativeGraph.Pass(claim)) { selectedIndex = index; current = score; }
        }
        retention = ReadWitness.Create(new Carrier(), NativeGraph.G(scores.ToArray()), NativeGraph.G(claims.ToArray()),
            Relation.Point(countAxis, selectedIndex), NativeGraph.G(),
            NativeGraph.G(Relation.Point(countAxis, checked((long)trials.Value)), NativeGraph.Parts(candidates[selectedIndex], 5)[0]));
    }
    private Relation Model() => NativeGraph.G(NativeGraph.G(candidates.ToArray()), new Relation(Relation.Point(countAxis, selectedIndex), candidates[selectedIndex]), NativeGraph.G(trials, expanded, edit));
    private Relation Build() => NativeGraph.G(contract, Model(), CaptureOperational(), NativeGraph.G(receipts.ToArray()), forgotten);
    private string Describe(Relation candidate)
    {
        var f = NativeGraph.Parts(candidate, 5); var sources = NativeGraph.Parts((Relation)f[1]).Cast<Relation>().ToArray();
        var labels = sources.Select(source => { var s = NativeGraph.Parts(source, 2); NativeGraph.TryPoint(s[1], out var offset);
            return (ReferenceEquals(s[0], portA) ? "A" : "B") + (offset.Ticks == 0 ? "" : "[previous]"); }).ToArray();
        if (sources.Length == 1) return labels[0];
        var equation = (Relation)f[0]; if (equation.Right is Relation output && NativeGraph.TryPoint(output.Left, out var start) && NativeGraph.TryPoint(output.Right, out var end) && start.Ticks == 0 && end.Ticks == 1)
            return string.Join(" * ", labels);
        var ports = equation.Left is Relation pair ? new[] { pair.Left, pair.Right } : [];
        var isSubtraction = ports.Length == 2 && ports[1] is Relation weighted && weighted.Right is Relation frame &&
            NativeGraph.TryPoint(frame.Left, out var begin) && NativeGraph.TryPoint(frame.Right, out var finish) && finish.Ticks < begin.Ticks;
        return string.Join(isSubtraction ? " - " : " + ", labels);
    }
    private static Relation FrozenModel(Relation model)
    {
        var fields = NativeGraph.Parts(model, 3); var originals = NativeGraph.Parts((Relation)fields[0]).Cast<Relation>().ToArray();
        var frozen = originals.Select(candidate =>
        {
            var f = NativeGraph.Parts(candidate, 5);
            f[2] = NativeGraph.G(Rows(candidate).Select(row => { var r = NativeGraph.Parts(row, 2); r[1] = NativeGraph.G(NativeGraph.Parts((Relation)r[1], 2).Cast<Relation>().Select(entry =>
                (IElement)new Relation(entry.Left, Copy((Carrier)entry.Right))).ToArray()); return (IElement)NativeGraph.G(r); }).ToArray());
            f[3] = NativeGraph.G(NativeGraph.Parts((Relation)f[3], 3).Cast<Carrier>().Select(c => (IElement)Copy(c)).ToArray()); return NativeGraph.G(f);
        }).ToArray();
        var binding = (Relation)fields[1]; NativeGraph.TryPoint(binding.Left, out var selected); var state = NativeGraph.Parts((Relation)fields[2], 3);
        // Parameter snapshots retain a compact edit instruction, not the large
        // historical score read or any previous live model.
        var frozenEdit = NativeGraph.G(originals.Select(candidate => (IElement)NativeGraph.G(NativeGraph.Parts(candidate, 5)[0], NativeGraph.Parts(candidate, 5)[1])).ToArray());
        return NativeGraph.G(NativeGraph.G(frozen), new Relation(new CarrierLocation(selected.Carrier, selected.Ticks), frozen[(int)selected.Ticks]), NativeGraph.G(Copy((Carrier)state[0]), Copy((Carrier)state[1]), frozenEdit));
        static Carrier Copy(Carrier original) { var copy = new Carrier(original.UnitShape); copy.Add(original.Value); foreach (var attachment in original.AttachedRelations)
            if (attachment.Left is Relation from && attachment.Right is Relation to && NativeGraph.TryPoint(from, out var a) && NativeGraph.TryPoint(to, out var b))
                copy.Attach(new Relation(Relation.Point(copy, a.Ticks), Relation.Point(copy, b.Ticks))); return copy; }
    }
}

/// <summary>Display-only inspection of connected candidate rows.</summary>
public sealed record RelationCandidateDiagnostic(int Index, string Description, long Observations, long Correct,
    long IncorrectOrUnanswered, int PredictionRows, long Complexity, long? Score256, bool Selected);
