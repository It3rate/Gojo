using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;
using PrimitiveLearning;

namespace CompositeEmergence;

public sealed class StudyRunner(StudySettings settings, string output)
{
    private readonly List<StudyResult> results = [];
    private readonly List<CurvePoint> curves = [];
    private readonly List<TracePoint> trace = [];
    private readonly List<ArchiveCheck> archives = [];
    private readonly List<object> discoveryCandidates = [];
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly string started = DateTime.UtcNow.ToString("O");
    private long evaluations, trainingEvents;
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly string[] SourceVariants = ["learned", "no-expansion", "frozen", "shuffled-feedback", "shuffled-communication", "no-memory", "previous-pair"];

    public StudyData Run()
    {
        Directory.CreateDirectory(output);
        SaveManifest();
        Save("run.json", new { status = "running", settings, startedUtc = started });
        for (var offset = 0; offset < settings.Seeds; offset++)
        {
            var seed = settings.SeedStart + offset;
            Console.WriteLine($"Seed {seed}: relation discovery");
            var models = new Dictionary<string, RelationService>();
            foreach (var variant in SourceVariants)
            {
                var service = TrainSource(seed, variant);
                models.Add(variant, service);
                EvaluateSource(seed, variant, service);
                discoveryCandidates.Add(new { seed, variant, service.Expansions, service.TrainingTrials, candidates = service.CandidateDiagnostics });
            }
            Console.WriteLine($"Seed {seed}: source consumers and interventions");
            foreach (var variant in SourceVariants)
                RunConsumer(seed, "source", variant, models[variant], 1, settings.ConsumerTrain, false, false);
            RunConsumer(seed, "source", "flat", models["learned"], 1, settings.ConsumerTrain, true, false);
            RunConsumer(seed, "source", "fixed-sum", models["learned"], 1, settings.ConsumerTrain, false, false, fixedSum: true);
            RunConsumer(seed, "source", "neutral-left", models["learned"], 1, 0, false, false, neutral: true);

            Console.WriteLine($"Seed {seed}: frozen-service transfer");
            foreach (var variant in new[] { "reused-service", "fresh-service", "no-expansion", "shuffled-communication", "flat", "fixed-sum" })
            {
                var source = variant is "no-expansion" or "shuffled-communication" ? models[variant] : models["learned"];
                RunConsumer(seed, "transfer", variant, source, -1, settings.ConsumerTrain, variant == "flat", true,
                    fresh: variant == "fresh-service", fixedSum: variant == "fixed-sum");
            }

            Console.WriteLine($"Seed {seed}: refresh/reuse and recovery");
            RunTemporal(seed, models["learned"]);
            SaveProgress("running");
        }
        var data = SaveProgress("complete");
        Save("run.json", new { status = "complete", settings, startedUtc = started, finishedUtc = data.FinishedUtc,
            elapsedSeconds = elapsed.Elapsed.TotalSeconds, resultRows = results.Count, trainingEvents, evaluations,
            paidJevCalls = 0 });
        Console.WriteLine($"Complete: {results.Count} rows; {trainingEvents} training events; {evaluations} evaluations; {elapsed.Elapsed.TotalSeconds:F1}s.");
        return data;
    }

    private RelationService TrainSource(int seed, string variant)
    {
        var service = new RelationService(memoryEnabled: variant != "no-memory", expansionEnabled: variant != "no-expansion", candidateOrderSeed: seed);
        var a = new SensorPrimitive(service.InputAAxis); var b = new SensorPrimitive(service.InputBAxis);
        var tape = ApparatusTape.Create(seed * 37 + 1, settings.DiscoveryTrain);
        var other = ApparatusTape.Create(seed * 37 + 503, settings.DiscoveryTrain);
        var world = new ApparatusWorld(); var correct = 0;
        for (var i = 0; i < tape.Count; i++)
        {
            var trial = tape[i]; Feed(service, a, b, world, trial, tape, other, variant);
            var action = world.ExploratoryAction(trial, service.ActionAxis);
            var prediction = service.PredictOutcome(action);
            var actual = world.DirectionConsequence(trial, action, service.ActionAxis, service.OutcomeAxis);
            if (variant == "shuffled-feedback") actual = WorldPublication.Value(service.OutcomeAxis,
                trial.ExploratoryAction * other[i].Rule, new CarrierLocation(world.TimeAxis, i * 64L + 6));
            if (WorldPublication.TryValue(prediction, service.OutcomeAxis, out var predicted) && predicted == trial.ExploratoryAction * trial.Rule) correct++;
            service.Learn(action, NativeGraph.BindFeedback(action, actual, service.OutcomeAxis), variant != "frozen");
            trainingEvents++;
            if ((i + 1) % 64 == 0) curves.Add(new("discovery", variant, seed, i + 1, 0, (double)correct / (i + 1), 0));
        }
        return service;
    }

    private void EvaluateSource(int seed, string variant, RelationService trained)
    {
        var service = Clone(trained, variant != "no-memory"); var guard = Digest(service.LearnedRoot);
        var tape = ApparatusTape.Create(seed * 41 + 7001, settings.TestEpisodes);
        var other = ApparatusTape.Create(seed * 41 + 9001, settings.TestEpisodes);
        var world = new ApparatusWorld(); var a = new SensorPrimitive(service.InputAAxis); var b = new SensorPrimitive(service.InputBAxis);
        var correct = 0; var answered = 0; var states = new HashSet<long>();
        foreach (var trial in Enumerable.Range(0, tape.Count).Select(i => tape[i]))
        {
            Feed(service, a, b, world, trial, tape, other, variant, false);
            var feature = service.PublishFeature(); if (WorldPublication.TryValue(feature, service.FeatureAxis, out var f)) states.Add(f);
            var action = world.ExploratoryAction(trial, service.ActionAxis); var prediction = service.PredictOutcome(action);
            if (WorldPublication.TryValue(prediction, service.OutcomeAxis, out var value)) { answered++; if (value == trial.ExploratoryAction * trial.Rule) correct++; }
            evaluations++;
        }
        Guard(guard, service.LearnedRoot, "relation evaluation");
        results.Add(Result("discovery", variant, "fresh-rule", seed, settings.DiscoveryTrain, tape.Count, correct, answered, 0, 0,
            service.SelectedDescription, service.LearnedRoot, service.Root, states.Count, service.CandidateCount));
        if (seed == settings.SeedStart && variant == "learned") ArchiveService(service);
    }

    private ServiceConsumer RunConsumer(int seed, string stage, string variant, RelationService trained, int wire, int train,
        bool flat, bool checkpoints, bool fresh = false, bool fixedSum = false, bool neutral = false)
    {
        var service = fresh ? new RelationService(candidateOrderSeed: seed) : Clone(trained, variant != "no-memory");
        var consumer = flat ? new ServiceConsumer([service.InputAAxis, service.InputBAxis], new Carrier(), new Carrier(), new Carrier()) :
            new ServiceConsumer(service.FeatureAxis, new Carrier(), new Carrier(), new Carrier());
        var sourceGuard = Digest(service.LearnedRoot);
        var world = new ApparatusWorld(wire);
        var tape = ApparatusTape.Create(seed * 47 + (stage == "source" ? 17 : 1017), Math.Max(train, 1));
        var other = ApparatusTape.Create(seed * 47 + 9017, Math.Max(train, 1));
        var a = new SensorPrimitive(service.InputAAxis); var b = new SensorPrimitive(service.InputBAxis);
        var milestones = new HashSet<int>(new[] { 0, 8, 16, 32, 64, 128, train }.Where(x => x <= train));
        var total = 0L; var correct = 0;
        if (checkpoints) EvaluateConsumer(seed, stage, variant, service, consumer, wire, 0, flat, fixedSum, neutral, "baseline");
        for (var i = 0; i < train; i++)
        {
            var trial = tape[i]; Feed(service, a, b, world, trial, tape, other, variant, true);
            var feature = Feature(service, a.Current, b.Current, fixedSum);
            var goal = world.Goal(trial, consumer.GoalAxis);
            var decision = flat ? consumer.Act([a.Current, b.Current], goal, true) : consumer.Act(feature, goal, true);
            var action = Action(decision, consumer.ActionAxis); var resource = world.Resource(trial, action, consumer.ResourceAxis, command: decision);
            consumer.Observe(consumer.BindFeedback(resource));
            if (fresh)
            {
                var observedAction = WorldPublication.Value(service.ActionAxis, action, new CarrierLocation(world.TimeAxis, i * 64L + 5));
                var observedOutcome = world.DirectionConsequence(trial, observedAction, service.ActionAxis, service.OutcomeAxis);
                service.Learn(observedAction, NativeGraph.BindFeedback(observedAction, observedOutcome, service.OutcomeAxis));
            }
            trainingEvents++;
            WorldPublication.TryValue(resource, consumer.ResourceAxis, out var amount); total += amount; if (world.IsCorrect(trial, action)) correct++;
            if ((i + 1) % 8 == 0) curves.Add(new(stage, variant, seed, i + 1, (double)total / (i + 1), (double)correct / (i + 1), 0));
            if (checkpoints && milestones.Contains(i + 1)) EvaluateConsumer(seed, stage, variant, service, consumer, wire, i + 1, flat, fixedSum, neutral, "baseline");
        }
        if (!fresh) Guard(sourceGuard, service.LearnedRoot, "consumer training must freeze source");
        if (!checkpoints) EvaluateConsumer(seed, stage, variant, service, consumer, wire, train, flat, fixedSum, neutral, "baseline");
        if (stage == "source" && variant == "learned")
        {
            foreach (var condition in new[] { "long-delay", "missing-signals", "opposite-state", "same-state", "absent-state", "inverted-input" })
                EvaluateConsumer(seed, stage, variant, service, consumer, wire, train, false, false, false, condition);
            if (seed == settings.SeedStart) ArchiveConsumer(consumer);
        }
        Save($"consumer-{stage}-{variant}-{seed}.json", new { seed, stage, variant, trained = train, evidence = consumer.InspectEvidence(),
            selectedDescription = service.SelectedDescription, learnedGraph = NativeInspection.Count(NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot)) });
        return consumer;
    }

    private void EvaluateConsumer(int seed, string stage, string variant, RelationService source, ServiceConsumer trained,
        int wire, int train, bool flat, bool fixedSum, bool neutral, string condition)
    {
        var (service, consumer, _) = CloneBundle(source, trained, memoryEnabled: variant != "no-memory");
        service.ResetOperational(); consumer.ResetOperational();
        var guard = Digest(NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot));
        var tape = ApparatusTape.Create(seed * 53 + 17001, settings.TestEpisodes, maximumDelay: condition == "long-delay" ? 31 : 3,
            missingProbability: condition == "missing-signals" ? .10 : 0);
        var other = ApparatusTape.Create(seed * 53 + 19001, settings.TestEpisodes);
        var world = new ApparatusWorld(wire); var a = new SensorPrimitive(service.InputAAxis); var b = new SensorPrimitive(service.InputBAxis);
        var donors = new Dictionary<int, long>(); var states = new HashSet<long>(); var correct = 0; var answered = 0; var total = 0L;
        if (condition is "opposite-state" or "same-state")
            for (var i = 0; i < Math.Min(64, tape.Count); i++)
            {
                Feed(service, a, b, world, tape[i], tape, other, "learned", false);
                var probe = service.PublishFeature(); if (WorldPublication.TryValue(probe, service.FeatureAxis, out var donor)) donors.TryAdd(tape[i].Rule, donor);
            }
        service.ResetOperational();
        foreach (var trial in Enumerable.Range(0, tape.Count).Select(i => tape[i]))
        {
            Feed(service, a, b, world, trial, tape, other, variant, false, condition == "inverted-input" ? -1 : 1);
            var feature = Feature(service, a.Current, b.Current, fixedSum);
            if (condition is "opposite-state" or "same-state")
            {
                var donorRule = condition == "opposite-state" ? -trial.Rule : trial.Rule;
                feature = donors.TryGetValue(donorRule, out var donor) ? WorldPublication.Value(service.FeatureAxis, donor, new CarrierLocation(world.TimeAxis, trial.Index * 64L + 8)) :
                    WorldPublication.Open(service.FeatureAxis, new Carrier());
            }
            else if (condition == "absent-state") feature = WorldPublication.Open(service.FeatureAxis, new Carrier());
            var hasFeature = WorldPublication.TryValue(feature, service.FeatureAxis, out var f); if (hasFeature) states.Add(f);
            var goal = world.Goal(trial, consumer.GoalAxis);
            var decision = flat ? consumer.Act([a.Current, b.Current], goal, false) : consumer.Act(feature, goal, false);
            var action = neutral ? -1 : Action(decision, consumer.ActionAxis);
            if (action != 0) answered++; var right = world.IsCorrect(trial, action); if (right) correct++;
            var resource = world.Resource(trial, action, consumer.ResourceAxis, command: decision); WorldPublication.TryValue(resource, consumer.ResourceAxis, out var amount); total += amount;
            if (seed == settings.SeedStart && stage == "source" && variant == "learned" && condition == "baseline" && trial.Index < 48)
                trace.Add(new(stage, trial.Index, trial.Rule, trial.Input, trial.Input * trial.Rule, trial.Goal, wire, hasFeature ? f : null,
                    hasFeature, 0, 0, true, action, right, (int)amount, false, trial.Delay, service.SelectedDescription));
            evaluations++;
        }
        var learned = NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot); Guard(guard, learned, "consumer frozen evaluation");
        results.Add(Result(stage, variant, condition, seed, train, tape.Count, correct, answered, tape.Count, total,
            fixedSum ? "fixed sum(A,B)" : flat ? "flat A,B,goal market" : service.SelectedDescription, learned,
            NativeGraph.G(service.Root, consumer.Root), states.Count, service.CandidateCount));
    }

    private static Relation Feature(RelationService service, Relation a, Relation b, bool fixedSum)
    {
        if (!fixedSum) return service.PublishFeature();
        if (!NativeGraph.TryValue(a, service.InputAAxis, out var av) || !NativeGraph.TryValue(b, service.InputBAxis, out var bv))
            return NativeGraph.Open(new Carrier(), Relation.Segment(service.FeatureAxis, -2, 2));
        var sum = Equation.Sum(service.FeatureAxis, Equation.UnitPort(service.InputAAxis), Equation.UnitPort(service.InputBAxis));
        var prediction = Equation.Predict(sum, new Relation(av, bv), service.FeatureAxis);
        return Equation.TryReadPrediction(prediction, out var result, out _) ? NativeGraph.Publication(service.FeatureAxis, result.Ticks, new Carrier()) :
            NativeGraph.Open(new Carrier(), sum);
    }

    private static void Feed(RelationService service, SensorPrimitive a, SensorPrimitive b, ApparatusWorld world,
        ApparatusTrial trial, ApparatusTape tape, ApparatusTape other, string variant, bool sensorLearning = true, int bOrientation = 1)
    {
        var input = variant == "previous-pair" && trial.Index > 0 ? tape[trial.Index - 1] : trial;
        a.Accept(world.Input(input, service.InputAAxis), sensorLearning); service.AcceptA(a.Current);
        var replacement = variant == "shuffled-communication" ? other[trial.Index].Input * other[trial.Index].Rule : (int?)null;
        b.Accept(world.Response(trial, service.InputBAxis, bOrientation, replacement), sensorLearning); service.AcceptB(b.Current);
    }

    private void RunTemporal(int seed, RelationService trained)
    {
        // The action consumer is calibrated once. Query training cannot change
        // the relation or action law, so it measures the information allocator.
        var service = Clone(trained); var consumer = TrainTemporalConsumer(seed, service);
        foreach (var feedbackDelay in new[] { 0, 4 })
        {
            var manager = new RefreshLearner(new Carrier(), new Carrier(), new Carrier(), consumer.ResourceAxis);
            ExecuteTemporal(seed, service, consumer, manager, "learned-refresh", "training", settings.RefreshTrain, true, settings.SwitchProbability, feedbackDelay);
            Save($"refresh-{feedbackDelay}-{seed}.json", new { seed, historyDelay = feedbackDelay + 1, evidence = manager.InspectEvidence(),
                manager.RowCount, manager.UpdateCount, manager.AgingEvents, learnedGraph = NativeInspection.Count(manager.LearnedRoot) });
            foreach (var condition in new[] { "baseline", "fast-switch", "expensive-probe", "missing-signals" })
            {
                var hazard = condition == "fast-switch" ? .12 : settings.SwitchProbability;
                foreach (var variant in new[] { "learned-refresh", "always-probe", "never-probe", "outcome-memory", "failure-probe", "fixed-interval" })
                    ExecuteTemporal(seed, service, consumer, manager, variant, (feedbackDelay == 0 ? "immediate/" : "delayed-history/") + condition,
                        settings.RefreshTest, false, hazard, feedbackDelay, condition == "expensive-probe" ? 5 : settings.ProbePrice,
                        condition == "missing-signals" ? .10 : 0);
            }
            if (seed == settings.SeedStart) ArchiveManager(manager, feedbackDelay);
        }
    }

    private ServiceConsumer TrainTemporalConsumer(int seed, RelationService service)
    {
        var consumer = new ServiceConsumer(service.FeatureAxis, new Carrier(), new Carrier(), new Carrier());
        var tape = ApparatusTape.Create(seed * 59 + 23001, settings.ConsumerTrain);
        var other = ApparatusTape.Create(seed * 59 + 25001, settings.ConsumerTrain); var world = new ApparatusWorld();
        var a = new SensorPrimitive(service.InputAAxis); var b = new SensorPrimitive(service.InputBAxis); var guard = Digest(service.LearnedRoot);
        for (var i = 0; i < tape.Count; i++)
        {
            var trial = tape[i]; Feed(service, a, b, world, trial, tape, other, "learned");
            var decision = consumer.Act(service.PublishFeature(), world.Goal(trial, consumer.GoalAxis));
            consumer.Observe(consumer.BindFeedback(world.Resource(trial, Action(decision, consumer.ActionAxis), consumer.ResourceAxis, command: decision)));
            trainingEvents++;
        }
        Guard(guard, service.LearnedRoot, "temporal consumer calibration"); return consumer;
    }

    private void ExecuteTemporal(int seed, RelationService source, ServiceConsumer actionModel, RefreshLearner trainedManager,
        string variant, string condition, int count, bool learning, double hazard, int feedbackDelay, int? price = null, double missing = 0)
    {
        var service = source; var consumer = actionModel; var manager = trainedManager;
        if (!learning)
        {
            var bundle = CloneBundle(source, actionModel, trainedManager);
            service = bundle.Service; consumer = bundle.Consumer; manager = bundle.Manager!;
        }
        service.ResetOperational(); consumer.ResetOperational(); manager.ResetOperational();
        var memory = new FeatureMemory(service.FeatureAxis, manager.AvailableAxis, manager.AgeAxis, manager.OutcomeAxis);
        var guard = Digest(NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot, manager.LearnedRoot));
        var lowerGuard = Digest(NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot));
        // Outcome-only comparison has its own authored native law. It does not
        // depend on the discovered service's representation being a product.
        var outcomeActionLaw = Equation.Product(consumer.ActionAxis, Equation.UnitPort(service.FeatureAxis), Equation.UnitPort(consumer.GoalAxis));
        var tape = ApparatusTape.Create(seed * 61 + (learning ? 27001 : 29001), count, true, hazard, missingProbability: missing);
        var world = new ApparatusWorld(probePrice: price ?? settings.ProbePrice);
        var a = new SensorPrimitive(service.InputAAxis); var b = new SensorPrimitive(service.InputBAxis);
        var pending = new Queue<(int Due, Relation Action, Relation Goal, Relation Gross)>();
        var correct = 0; var answered = 0; var queries = 0; var total = 0L; var lastRule = tape[0].Rule;
        var switches = 0; var recovery = new List<int>(); int? waiting = null;
        for (var i = 0; i < count; i++)
        {
            while (pending.TryPeek(out var due) && due.Due <= i)
            {
                pending.Dequeue(); memory.RememberOutcome(due.Gross);
                if (variant == "outcome-memory") memory.Retain(OutcomeRetrodiction.InferFromOutcome(due.Action, due.Goal, due.Gross,
                    consumer.ActionAxis, consumer.GoalAxis, service.FeatureAxis));
            }
            var trial = tape[i]; var switched = trial.Rule != lastRule;
            if (switched) { switches++; if (waiting is not null) recovery.Add(-1); waiting = i; }
            lastRule = trial.Rule;
            var choice = manager.Decide(memory.Available(), memory.Age(), memory.PreviousOutcome(), learning);
            var query = WorldPublication.TryValue(choice, manager.QueryAxis, out var q) && q == 1;
            if (variant == "always-probe") query = true;
            else if (variant == "never-probe") query = i == 0;
            else if (variant == "outcome-memory") query = false;
            else if (variant == "failure-probe") query = i == 0 || memory.LastOutcomeNow < 0;
            else if (variant == "fixed-interval") query = i % 8 == 0;
            if (query)
            {
                queries++; a.Accept(world.Input(trial, service.InputAAxis), false); service.AcceptA(a.Current);
                b.Accept(world.Response(trial, service.InputBAxis), false); service.AcceptB(b.Current);
                memory.Retain(service.PublishFeature());
            }
            var feature = memory.ReadFeature(); var hasFeature = WorldPublication.TryValue(feature, service.FeatureAxis, out var f);
            var goal = world.Goal(trial, consumer.GoalAxis);
            var decision = variant == "outcome-memory" ? OutcomeAction(feature, goal, service.FeatureAxis, consumer.GoalAxis, consumer.ActionAxis, outcomeActionLaw) :
                consumer.Act(feature, goal, false);
            var action = Action(decision, consumer.ActionAxis);
            var right = world.IsCorrect(trial, action); if (right) correct++; if (action != 0) answered++;
            var gross = world.Resource(trial, action, consumer.ResourceAxis, command: decision);
            var resource = world.Resource(trial, action, consumer.ResourceAxis, query, decision); WorldPublication.TryValue(resource, consumer.ResourceAxis, out var amount); total += amount;
            if (learning) manager.Observe(manager.BindFeedback(resource));
            pending.Enqueue((i + feedbackDelay + 1, decision, goal, gross));
            if (waiting is not null && right) { recovery.Add(i - waiting.Value); waiting = null; }
            if (seed == settings.SeedStart && variant == "learned-refresh" && (condition is "immediate/baseline" or "delayed-history/baseline") && i < 160)
                trace.Add(new(condition, i, trial.Rule, trial.Input, trial.Input * trial.Rule, trial.Goal, 1, hasFeature ? f : null,
                    hasFeature, AgeBucket(memory.ActualAge), memory.LastOutcomeNow, query, action, right, (int)amount, switched, trial.Delay, service.SelectedDescription));
            memory.Advance();
            if (learning) { trainingEvents++; if ((i + 1) % 64 == 0) curves.Add(new("refresh", feedbackDelay == 0 ? "immediate" : "delayed", seed, i + 1,
                (double)total / (i + 1), (double)correct / (i + 1), (double)queries / (i + 1))); }
            else evaluations++;
        }
        if (waiting is not null) recovery.Add(-1);
        if (learning)
        {
            Guard(lowerGuard, NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot), "refresh training must freeze lower services");
            if (seed == settings.SeedStart) ArchivePipeline(service, consumer, manager, memory, feedbackDelay);
            return;
        }
        var learned = NativeGraph.G(service.LearnedRoot, consumer.LearnedRoot, manager.LearnedRoot); Guard(guard, learned, "refresh frozen evaluation");
        var resolved = recovery.Where(x => x >= 0).ToArray();
        var result = Result("refresh", variant, condition, seed, settings.RefreshTrain, count, correct, answered, queries, total,
            service.SelectedDescription, learned, NativeGraph.G(service.Root, consumer.Root, manager.Root, memory.Root));
        results.Add(result with { Switches = switches, MeanRecovery = resolved.Length == 0 ? 0 : resolved.Average(), Unrecovered = recovery.Count(x => x < 0) });
    }

    private void ArchiveService(RelationService service)
    {
        var path = Path.Combine(output, "relation.gojo"); var bytes = GojoNumericFormat.Encode(service.Root); File.WriteAllBytes(path, bytes);
        var restored = RelationService.FromRoot((Relation)GojoNumericFormat.Decode(bytes)[0]);
        var same = Digest(service.Root) == Digest(restored.Root); var behavior = ArchiveVerifier.VerifyService(service, restored);
        archives.Add(new("relation", same, behavior, bytes.Length, Path.GetFileName(path)));
    }
    private void ArchiveConsumer(ServiceConsumer consumer)
    {
        var path = Path.Combine(output, "consumer.gojo"); var bytes = GojoNumericFormat.Encode(consumer.Root); File.WriteAllBytes(path, bytes);
        var restored = ServiceConsumer.FromRoot((Relation)GojoNumericFormat.Decode(bytes)[0]);
        archives.Add(new("consumer", Digest(consumer.Root) == Digest(restored.Root), ArchiveVerifier.VerifyConsumer(consumer, restored),
            bytes.Length, Path.GetFileName(path)));
    }
    private void ArchiveManager(RefreshLearner manager, int delay)
    {
        var path = Path.Combine(output, $"refresh-{delay}.gojo"); var bytes = GojoNumericFormat.Encode(manager.Root); File.WriteAllBytes(path, bytes);
        var restored = RefreshLearner.FromRoot((Relation)GojoNumericFormat.Decode(bytes)[0]);
        archives.Add(new($"refresh-{delay}", Digest(manager.Root) == Digest(restored.Root), ArchiveVerifier.VerifyManager(manager, restored),
            bytes.Length, Path.GetFileName(path)));
    }
    private void ArchivePipeline(RelationService service, ServiceConsumer consumer, RefreshLearner manager, FeatureMemory memory, int delay)
    {
        var root = NativeGraph.G(service.Root, consumer.Root, manager.Root, memory.Root);
        var path = Path.Combine(output, $"pipeline-{delay}.gojo"); var bytes = GojoNumericFormat.Encode(root); File.WriteAllBytes(path, bytes);
        var restored = (Relation)GojoNumericFormat.Decode(bytes)[0];
        archives.Add(new($"pipeline-{delay}", Digest(root) == Digest(restored), ArchiveVerifier.VerifyPipeline(root, restored),
            bytes.Length, Path.GetFileName(path)));
    }
    private static StudyResult Result(string stage, string variant, string condition, int seed, int train, int count,
        int correct, int answered, int queries, long total, string description, IElement learned, IElement graph, int states = 0, int candidates = 0) =>
        new(stage, variant, condition, seed, train, count, correct, answered, queries, total, (double)total / count, (double)correct / count,
            (double)answered / count, (double)queries / count, description, Digest(learned), NativeInspection.Count(graph), FeatureStates: states, CandidateCount: candidates);
    private static RelationService Clone(RelationService source, bool memory = true) => RelationService.FromLearnedRoot(Roundtrip(source.LearnedRoot), memory);
    private static (RelationService Service, ServiceConsumer Consumer, RefreshLearner? Manager) CloneBundle(
        RelationService source, ServiceConsumer consumer, RefreshLearner? manager = null, bool memoryEnabled = true)
    {
        // A single transport preserves contract-axis aliases between learners.
        // Restoring each root independently would create incompatible ports.
        var restored = Roundtrip(manager is null ? NativeGraph.G(source.LearnedRoot, consumer.Root) :
            NativeGraph.G(source.LearnedRoot, consumer.Root, manager.Root));
        var fields = NativeGraph.Parts(restored, manager is null ? 2 : 3);
        return (RelationService.FromLearnedRoot((Relation)fields[0], memoryEnabled), ServiceConsumer.FromRoot((Relation)fields[1]),
            manager is null ? null : RefreshLearner.FromRoot((Relation)fields[2]));
    }
    private static Relation OutcomeAction(Relation feature, Relation goal, Carrier featureAxis, Carrier goalAxis, Carrier actionAxis, Relation law)
    {
        if (!NativeGraph.TryValue(feature, featureAxis, out var previous) || !NativeGraph.TryValue(goal, goalAxis, out var desired))
            return NativeFallback.Choose(NativeGraph.Open(new Carrier(), law), actionAxis, -1);
        var result = Equation.Predict(law, new Relation(previous, desired), actionAxis);
        return NativeGraph.TryValue(result, actionAxis, out var value) ?
            ReadWitness.Create(new Carrier(), NativeGraph.G(feature, goal), law, new CarrierLocation(actionAxis, value.Ticks), result, result) :
            NativeFallback.Choose(result, actionAxis, -1);
    }
    private static Relation Roundtrip(Relation root) => (Relation)GojoNumericFormat.Decode(GojoNumericFormat.Encode(root))[0];
    private static int Action(Relation publication, Carrier axis) => WorldPublication.TryValue(publication, axis, out var value) && value is -1 or 1 ? (int)value : 0;
    private static string Digest(IElement root) => NativeInspection.Signature(root);
    private static void Guard(string before, IElement root, string label)
    {
        if (Digest(root) != before) throw new InvalidOperationException($"Frozen learned parameters changed: {label}.");
    }
    private static int AgeBucket(long age) => age == 0 ? 0 : age <= 3 ? 1 : age <= 7 ? 2 : 3;
    private void Save(string name, object data) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(data, Json));
    private StudyData SaveProgress(string status)
    {
        var data = new StudyData(settings, results, curves, trace, archives, discoveryCandidates.ToArray(), status, started, DateTime.UtcNow.ToString("O"), elapsed.Elapsed.TotalSeconds);
        Save("data.json", data); Save("results.json", results); Save("curves.json", curves); Save("trace.json", trace); Save("archive-check.json", archives);
        var template = Path.Combine(AppContext.BaseDirectory, "lab.html");
        if (File.Exists(template)) File.WriteAllText(Path.Combine(output, "index.html"), File.ReadAllText(template).Replace("__STUDY_DATA__", JsonSerializer.Serialize(data, Json)));
        return data;
    }
    private void SaveManifest()
    {
        var repository = FindRepository(); var files = new SortedDictionary<string, string>();
        foreach (var folder in new[] { "Gojo", "experiments/PrimitiveLearning", "experiments/CompositeEmergence" })
            foreach (var path in Directory.EnumerateFiles(Path.Combine(repository, folder), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj")))
                files[Path.GetRelativePath(repository, path).Replace('\\', '/')] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        Save("manifest.json", new { settings, startedUtc = started, runtime = Environment.Version.ToString(), sourceHashes = files,
            primaryClaim = "learned bounded relational service and reusable consumer capability", paidJevCalls = 0 });
    }
    private static string FindRepository()
    {
        foreach (var starting in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(starting); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Gojo.slnx")) && Directory.Exists(Path.Combine(directory.FullName, "Gojo")))
                    return directory.FullName;
        throw new DirectoryNotFoundException("Cannot locate Gojo repository sources for the run manifest.");
    }
}
