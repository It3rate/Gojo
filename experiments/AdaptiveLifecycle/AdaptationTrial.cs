using CompositeEmergence;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;
using System.Text.Json.Serialization;

namespace AdaptiveLifecycle;

/// <summary>Host scheduling and evaluation settings, not learner state.</summary>
public sealed record AdaptationSettings(int DiscoveryTrain = 768, int ConsumerTrain = 192,
    int ManagerTrain = 2048, int EvaluationTrials = 2048, double MissingProbability = .10,
    double SwitchProbability = .04, int ProbePrice = 2);

/// <summary>All learning counters and context evidence below are projections
/// of the actual connected native graphs. PrivateRule is evaluator-only.</summary>
public sealed record AdaptationStep(string Arm, int Index, int PrivateRule,
    bool AvailableBefore, long AgeBefore, long AgeBucket, long PreviousOutcome,
    bool ContextSeenBefore, long? ManagerChoice, bool Probe, bool? ProbeMissingA,
    bool? ProbeMissingB, bool AvailableAfter, long? Feature, int Action,
    bool Hold, bool Correct, long GrossResource, long ProbeDebit, long NetResource,
    long ManagerUpdatesBefore, long ManagerUpdatesAfter, long ManagerAgesBefore,
    long ManagerAgesAfter, long MemoryAgeAfter);

public sealed record AdaptationRow(string Arm, int Seed, int Trials, int Correct,
    int Answered, int Queries, int MissingProbes, long GrossResource, long ProbeDebits,
    long NetResource, double Accuracy, double Coverage, double QueryRate,
    double MeanResource, int LongestHoldRun, int TrailingHoldRun,
    int FailedProbesRecovered, int FailedProbesUnrecovered, double? MeanProbeRecovery,
    long ManagerUpdatesBefore, long ManagerUpdatesAfter, int ManagerRowsBefore,
    int ManagerRowsAfter, long ManagerAgesBefore, long ManagerAgesAfter,
    string LowerSignatureBefore, string LowerSignatureAfter,
    string ManagerSignatureBefore, string ManagerSignatureAfter,
    GraphCounts OperationalGraph, IReadOnlyList<MarketEvidence> EvidenceBefore,
    IReadOnlyList<MarketEvidence> EvidenceAfter);

public sealed record AdaptationRun(int Seed, AdaptationSettings Settings,
    string SelectedSource, long SourceTrainingTrials, int SourceCandidates,
    long ConsumerTrainingUpdates, int ConsumerRows, IReadOnlyList<AdaptationRow> Rows,
    IReadOnlyList<AdaptationStep> Trace)
{
    /// <summary>Actual connected execution roots for numeric transport, omitted
    /// from JSON diagnostics. Each arm retains its physical axis aliases.</summary>
    [JsonIgnore]
    public IReadOnlyDictionary<string, Relation> NativeRoots { get; init; } = new Dictionary<string, Relation>();
}

/// <summary>A bounded continuation of CompositeEmergence. Host code supplies
/// physical observations and schedules updates; the same native markets own
/// context discovery, exploration, scores, choices and evidence aging.
/// No learner receives ApparatusTrial.Rule or a success label.
/// </summary>
public static class AdaptationTrial
{
    public static AdaptationRun Run(int seed, AdaptationSettings? settings = null,
        IReadOnlyList<ApparatusTrial>? evaluationTape = null)
    {
        settings ??= new(); Validate(settings);
        if (evaluationTape is not null && evaluationTape.Count == 0)
            throw new ArgumentException("An evaluation tape needs at least one physical trial.", nameof(evaluationTape));
        var source = TrainSource(seed, settings.DiscoveryTrain);
        var consumer = TrainConsumer(seed, source, settings.ConsumerTrain);
        var clean = new RefreshLearner(new Carrier(), new Carrier(), new Carrier(), consumer.ResourceAxis);
        var missing = new RefreshLearner(new Carrier(), new Carrier(), new Carrier(), consumer.ResourceAxis);
        var trainingTape = Tape(seed, 37001, settings.ManagerTrain, settings, 0);
        var missingTrainingTape = Tape(seed, 37001, settings.ManagerTrain, settings, settings.MissingProbability);
        // The two training schedules share hidden rule/input/goal/action/time
        // streams; only the separate physical missingness stream differs.
        _ = Execute(seed, settings, source, consumer, clean, trainingTape, "clean-training", true);
        _ = Execute(seed, settings, source, consumer, missing, missingTrainingTape, "missing-training", true);
        var tape = evaluationTape ?? Tape(seed, 39001, settings.EvaluationTrials, settings, settings.MissingProbability);
        var rows = new List<AdaptationRow>(); var trace = new List<AdaptationStep>(); var nativeRoots = new Dictionary<string, Relation>();
        foreach (var (arm, manager, online) in new[]
        {
            ("clean-trained/frozen", clean, false), ("clean-trained/online", clean, true),
            ("missing-trained/frozen", missing, false), ("missing-trained/online", missing, true),
            ("always-probe", clean, false), ("retry-if-unavailable", clean, false)
        })
        {
            // Clone all roots in a single numeric transport. This preserves
            // source/consumer feature-axis and consumer/manager resource aliases.
            var cloned = Clone(source, consumer, manager);
            var result = Execute(seed, settings, cloned.Source, cloned.Consumer, cloned.Manager,
                tape, arm, online);
            rows.Add(result.Row); trace.AddRange(result.Trace);
            nativeRoots.Add(arm, result.NativeRoot);
        }
        return new(seed, settings, source.SelectedDescription, source.TrainingTrials, source.CandidateCount,
            consumer.UpdateCount, consumer.RowCount, rows, trace) { NativeRoots = nativeRoots };
    }

    private static RelationService TrainSource(int seed, int count)
    {
        var source = new RelationService(candidateOrderSeed: seed); var world = new ApparatusWorld();
        var tape = ApparatusTape.Create(unchecked(seed * 37 + 1), count);
        var a = new SensorPrimitive(source.InputAAxis); var b = new SensorPrimitive(source.InputBAxis);
        for (var index = 0; index < count; index++)
        {
            var trial = tape[index]; Feed(source, a, b, world, trial);
            var action = world.ExploratoryAction(trial, source.ActionAxis);
            _ = source.PredictOutcome(action);
            var physical = world.DirectionConsequence(trial, action, source.ActionAxis, source.OutcomeAxis);
            source.Learn(action, NativeGraph.BindFeedback(action, physical, source.OutcomeAxis));
        }
        return source;
    }

    private static ServiceConsumer TrainConsumer(int seed, RelationService source, int count)
    {
        var consumer = new ServiceConsumer(source.FeatureAxis, new Carrier(), new Carrier(), new Carrier());
        var guard = Signature(source.LearnedRoot); var world = new ApparatusWorld();
        var tape = ApparatusTape.Create(unchecked(seed * 59 + 23001), count);
        var a = new SensorPrimitive(source.InputAAxis); var b = new SensorPrimitive(source.InputBAxis);
        source.ResetOperational();
        for (var index = 0; index < count; index++)
        {
            var trial = tape[index]; Feed(source, a, b, world, trial);
            var decision = consumer.Act(source.PublishFeature(), world.Goal(trial, consumer.GoalAxis));
            consumer.Observe(consumer.BindFeedback(world.Resource(trial, Action(decision, consumer.ActionAxis),
                consumer.ResourceAxis, command: decision)));
        }
        Guard(guard, source.LearnedRoot, "source parameters during consumer training");
        return consumer;
    }

    private static (AdaptationRow Row, List<AdaptationStep> Trace, Relation NativeRoot) Execute(int seed, AdaptationSettings settings,
        RelationService source, ServiceConsumer consumer, RefreshLearner manager,
        IReadOnlyList<ApparatusTrial> tape, string arm, bool online)
    {
        source.ResetOperational(); consumer.ResetOperational(); manager.ResetOperational();
        var memory = new FeatureMemory(source.FeatureAxis, manager.AvailableAxis, manager.AgeAxis, manager.OutcomeAxis);
        var a = new SensorPrimitive(source.InputAAxis); var b = new SensorPrimitive(source.InputBAxis);
        var world = new ApparatusWorld(probePrice: settings.ProbePrice);
        var lowerBefore = Signature(NativeGraph.G(source.LearnedRoot, consumer.LearnedRoot));
        var managerBefore = Signature(manager.LearnedRoot); var evidenceBefore = manager.InspectEvidence();
        var updateBefore = manager.UpdateCount; var rowsBefore = manager.RowCount; var agesBefore = manager.AgingEvents;
        var retryRole = new Carrier();
        // Authored repair comparator: probe when the retained publication is
        // absent; otherwise retain the frozen learned manager's choice. The
        // connected conditional rule is visible, not a hidden host preference.
        var retryPolicy = SupportDecisionSurface.Create(new Carrier(),
            SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(manager.QueryAxis, 1),
                SupportDecisionSurface.Condition(new Carrier(), retryRole, Relation.Segment(manager.AvailableAxis, 0, 0))));
        var lastControl = NativeGraph.Open(retryPolicy, new Placeholder());
        Relation? pendingOutcome = null; var trace = new List<AdaptationStep>(tape.Count);
        var correct = 0; var answered = 0; var queries = 0; var missingProbes = 0;
        var grossSum = 0L; var debitSum = 0L; var netSum = 0L;
        var holdRun = 0; var longestHold = 0; var recoveries = new List<int>(); int? waitingProbe = null;
        for (var index = 0; index < tape.Count; index++)
        {
            if (pendingOutcome is not null) memory.RememberOutcome(pendingOutcome);
            var trial = tape[index]; var available = memory.Available(); var age = memory.Age(); var outcome = memory.PreviousOutcome();
            var hasBefore = WorldPublication.TryValue(available, manager.AvailableAxis, out var availableValue) && availableValue == 1;
            WorldPublication.TryValue(age, manager.AgeAxis, out var ageBucket);
            WorldPublication.TryValue(outcome, manager.OutcomeAxis, out var previousOutcome);
            var actualAge = memory.ActualAge; var seen = manager.InspectEvidence().Any(e =>
                e.Context.Length == 3 && e.Context[0] == availableValue && e.Context[1] == ageBucket && e.Context[2] == previousOutcome);
            var updatesAtStart = manager.UpdateCount; var agingAtStart = manager.AgingEvents;
            var choice = manager.Decide(available, age, outcome, online);
            long? chosen = WorldPublication.TryValue(choice, manager.QueryAxis, out var queryValue) ? queryValue : null;
            var query = chosen == 1;
            if (arm == "always-probe")
            {
                retryPolicy = SupportDecisionSurface.Create(new Carrier(),
                    SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(manager.QueryAxis, 1)));
                lastControl = SupportDecisionSurface.Select(new Carrier(), retryPolicy);
                query = NativeGraph.TryValue(lastControl, manager.QueryAxis, out var constant) && constant.Ticks == 1;
            }
            else if (arm == "retry-if-unavailable")
            {
                // Select the explicit availability rule first. If it does not
                // match, the ordinary learned decision is the fallback rule.
                retryPolicy = SupportDecisionSurface.Create(new Carrier(),
                    SupportDecisionSurface.Rule(new Carrier(), new CarrierLocation(manager.QueryAxis, 1),
                        SupportDecisionSurface.Condition(new Carrier(), retryRole, Relation.Segment(manager.AvailableAxis, 0, 0))),
                    SupportDecisionSurface.Rule(new Carrier(), chosen is not null ? new CarrierLocation(manager.QueryAxis, chosen.Value) : new Placeholder()));
                lastControl = SupportDecisionSurface.Select(new Carrier(), retryPolicy,
                    SupportDecisionSurface.Binding(new Carrier(), retryRole, available));
                query = NativeGraph.TryValue(lastControl, manager.QueryAxis, out var repaired) && repaired.Ticks == 1;
            }
            if (query)
            {
                queries++; Feed(source, a, b, world, trial); memory.Retain(source.PublishFeature());
            }
            var feature = memory.ReadFeature(); var present = WorldPublication.TryValue(feature, source.FeatureAxis, out var featureValue);
            var failedProbe = query && !present;
            if (failedProbe)
            {
                missingProbes++;
                // A second failure supersedes the outstanding failure interval,
                // whose lack of recovery remains explicit in the denominator.
                if (waitingProbe is not null) recoveries.Add(-1);
                waitingProbe = index;
            }
            if (waitingProbe is not null && present) { recoveries.Add(index - waitingProbe.Value); waitingProbe = null; }
            var decision = consumer.Act(feature, world.Goal(trial, consumer.GoalAxis), false);
            var action = Action(decision, consumer.ActionAxis); var right = world.IsCorrect(trial, action);
            if (right) correct++; if (action != 0) answered++;
            var gross = world.Resource(trial, action, consumer.ResourceAxis, command: decision);
            var net = world.Resource(trial, action, consumer.ResourceAxis, query, decision);
            WorldPublication.TryValue(gross, consumer.ResourceAxis, out var grossValue);
            WorldPublication.TryValue(net, consumer.ResourceAxis, out var netValue);
            var debit = grossValue - netValue; grossSum += grossValue; debitSum += debit; netSum += netValue;
            if (online) manager.Observe(manager.BindFeedback(net));
            pendingOutcome = gross;
            holdRun = action == 0 ? holdRun + 1 : 0; longestHold = Math.Max(longestHold, holdRun);
            memory.Advance();
            trace.Add(new(arm, index, trial.Rule, hasBefore, actualAge, ageBucket, previousOutcome, seen, chosen, query,
                query ? trial.MissingA : null, query ? trial.MissingB : null, present, present ? featureValue : null,
                action, action == 0, right, grossValue, debit, netValue, updatesAtStart, manager.UpdateCount,
                agingAtStart, manager.AgingEvents, memory.ActualAge));
        }
        if (waitingProbe is not null) recoveries.Add(-1);
        var lowerAfter = Signature(NativeGraph.G(source.LearnedRoot, consumer.LearnedRoot));
        if (lowerBefore != lowerAfter) throw new InvalidOperationException("Manager work changed frozen source/consumer parameters.");
        var managerAfter = Signature(manager.LearnedRoot);
        if (!online && managerBefore != managerAfter) throw new InvalidOperationException("A frozen manager changed learned parameters.");
        if (grossSum - debitSum != netSum || debitSum != (long)settings.ProbePrice * queries)
            throw new InvalidOperationException("Physical resource accounting did not conserve gross less query debits.");
        var resolved = recoveries.Where(value => value >= 0).ToArray();
        return (new(arm, seed, tape.Count, correct, answered, queries, missingProbes, grossSum, debitSum, netSum,
            (double)correct / tape.Count, (double)answered / tape.Count, (double)queries / tape.Count,
            (double)netSum / tape.Count, longestHold, holdRun, resolved.Length, recoveries.Count(value => value < 0),
            resolved.Length == 0 ? null : resolved.Average(), updateBefore, manager.UpdateCount, rowsBefore, manager.RowCount,
            agesBefore, manager.AgingEvents, lowerBefore, lowerAfter, managerBefore, managerAfter,
            NativeInspection.Count(NativeGraph.G(source.Root, consumer.Root, manager.Root, memory.Root, NativeGraph.G(retryPolicy, lastControl))),
            evidenceBefore, manager.InspectEvidence()), trace, NativeGraph.G(source.Root, consumer.Root, manager.Root, memory.Root, NativeGraph.G(retryPolicy, lastControl)));
    }

    private static void Feed(RelationService source, SensorPrimitive a, SensorPrimitive b, ApparatusWorld world, ApparatusTrial trial)
    {
        a.Accept(world.Input(trial, source.InputAAxis), false); source.AcceptA(a.Current);
        b.Accept(world.Response(trial, source.InputBAxis), false); source.AcceptB(b.Current);
    }
    private static IReadOnlyList<ApparatusTrial> Tape(int seed, int offset, int count, AdaptationSettings settings, double missing)
    {
        var tape = ApparatusTape.Create(unchecked(seed * 61 + offset), count, true, settings.SwitchProbability, missingProbability: missing);
        return Enumerable.Range(0, tape.Count).Select(index => tape[index]).ToArray();
    }
    private static (RelationService Source, ServiceConsumer Consumer, RefreshLearner Manager) Clone(
        RelationService source, ServiceConsumer consumer, RefreshLearner manager)
    {
        var root = NativeGraph.G(source.LearnedRoot, consumer.Root, manager.Root);
        var restored = (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
        var fields = NativeGraph.Parts(restored, 3);
        return (RelationService.FromLearnedRoot((Relation)fields[0]), ServiceConsumer.FromRoot((Relation)fields[1]), RefreshLearner.FromRoot((Relation)fields[2]));
    }
    private static int Action(Relation decision, Carrier axis) => WorldPublication.TryValue(decision, axis, out var value) && value is -1 or 1 ? (int)value : 0;
    private static string Signature(IElement root) => NativeInspection.Signature(root);
    private static void Guard(string expected, IElement root, string message)
    {
        if (Signature(root) != expected) throw new InvalidOperationException($"Frozen {message} changed.");
    }
    private static void Validate(AdaptationSettings settings)
    {
        if (settings.DiscoveryTrain < 1 || settings.ConsumerTrain < 1 || settings.ManagerTrain < 1 || settings.EvaluationTrials < 1 ||
            !double.IsFinite(settings.MissingProbability) || settings.MissingProbability is < 0 or > 1 ||
            !double.IsFinite(settings.SwitchProbability) || settings.SwitchProbability is < 0 or > 1 || settings.ProbePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(settings));
    }
}
