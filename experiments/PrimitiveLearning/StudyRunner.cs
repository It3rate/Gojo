using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Equations;
using Gojo.Structures;

namespace PrimitiveLearning;

public sealed record StudySettings(string Name, int Seeds, int Train, int Test, int Warmup, string Output, int SeedStart);
public sealed record ResultRow(string Experiment, string Variant, string Condition, int Seed, string Phase,
    int Episodes, double ReturnPerEpisode, double CorrectRate, double Coverage, double CoveredAccuracy,
    double QueryRate, double StableQueryRate, double UnstableQueryRate,
    double StableCoverage, double StableAccuracy, double UnstableCoverage, double UnstableAccuracy, double ActiveAccuracy,
    int Comparisons, int FitProbes,
    int Queries, int AuditArrivals, int ShadowActions, int Landmarks, int PredictorRows,
    long AcceptedEdits, long SelectedComplexity, GraphCounts Graph, string DecisionDigest, string TapeDigest, string LearnedDigest);
public sealed record CurvePoint(string Variant, int Seed, int Tick, double ReturnPerEpisode, double CorrectRate,
    int Landmarks, int PredictorRows, long AcceptedEdits);
public sealed record TracePoint(int Tick, int Context, int Prior, int Actual, bool Fits, bool Missing, bool Absent,
    long? FitRead, long? Forecast, bool Query, long? Direction, long? Action, long? Outcome);

public static class StudyRunner
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static RelayCondition Baseline = new("baseline");
    private static RelayCondition[] Heldout => [Baseline, Baseline with { Name = "novel-loads", NovelLoads = true },
        Baseline with { Name = "less-predictable", Persistence = Baseline.Persistence < .5 ? .30 : .70 },
        Baseline with { Name = "reversed-flow-law", Persistence = 1 - Baseline.Persistence },
        Baseline with { Name = "expensive-query", QueryPrice = 5 },
        Baseline with { Name = "changed-fit", Threshold = 10 },
        Baseline with { Name = "missing-and-absent", MissingEvery = 17, AbsentEvery = 31 }];

    public static int Run(string[] args)
    {
        if (args.Length == 2 && args[0] == "--inspect-query")
        {
            var restored = QueryLearner.FromRoot((Relation)GojoNumericFormat.Decode(File.ReadAllBytes(args[1]))[0]);
            for (var context = 0; context < 2; context++) for (var choice = 0; choice < 2; choice++)
            {
                QueryLearner.TryInspectEvidence(restored.ReadEvidence(context, choice), out var evidence);
                Console.WriteLine(JsonSerializer.Serialize(evidence, Json));
            }
            return 0;
        }
        var pilot = !args.Contains("--campaign");
        string Option(string name, string fallback) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback; }
        Baseline = new("baseline", Persistence: double.Parse(Option("--persistence", "0.05"), System.Globalization.CultureInfo.InvariantCulture),
            QueryPrice: int.Parse(Option("--query-price", "1")));
        var settings = new StudySettings(pilot ? "pilot" : "campaign", int.Parse(Option("--seeds", pilot ? "2" : "8")),
            int.Parse(Option("--train", pilot ? "640" : "1536")), int.Parse(Option("--test", pilot ? "256" : "768")),
            int.Parse(Option("--warmup", "128")), Path.GetFullPath(Option("--output", $"artifacts/primitive-learning/{(pilot ? "pilot" : "campaign")}")),
            int.Parse(Option("--seed-start", pilot ? "0" : "100")));
        var sourceDirectory = FindSourceDirectory();
        var sourceHashes = Directory.GetFiles(sourceDirectory, "*.cs").Order().ToDictionary(path => Path.GetFileName(path)!,
            path => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
        Directory.CreateDirectory(settings.Output);
        Save(settings.Output, "manifest.json", new { settings, heldout = Heldout,
            sourceHashes,
            trainSeedRule = "41000 + seed", testSeedRule = "91000 + seed",
            factorial = "A=adaptive partitions, P=predictor, Q=query allocator, S=program selection. Off=frozen initialization.",
            baseline = Baseline,
            resourceLaw = "Correct route+4; incorrect-4; hold/absent0; query has declared price (shift5). Comparison work reported separately.",
            schedule = "A/P learn immediately; Q/S after128 warmup. Simultaneous-query Q starts0. Frozen tests; post-action arrival audits.",
            status = "Saved before test results. Pilot is development data; campaign confirms this fixed protocol." });
        var watch = Stopwatch.StartNew(); var results = new List<ResultRow>(); var curves = new List<CurvePoint>(); var traces = new List<TracePoint>();
        for (var index = 0; index < settings.Seeds; index++)
        {
            var seed = settings.SeedStart + index;
            Console.WriteLine($"{settings.Name}: seed {index + 1}/{settings.Seeds} (identity {seed}); local learners");
            Isolated(settings, seed, results);
            var train = RelayWorld.Tape(41000 + seed, settings.Train, Baseline);
            for (var bits = 0; bits < 16; bits++)
            {
                var variant = $"A{((bits & 1) != 0 ? 1 : 0)}P{((bits & 2) != 0 ? 1 : 0)}Q{((bits & 4) != 0 ? 1 : 0)}S{((bits & 8) != 0 ? 1 : 0)}";
                var world = new RelayWorld(Baseline); var brain = new RelayBrain(world);
                results.Add(Composite(brain, world, train, variant, seed, "train", bits, settings.Warmup, curves));
                foreach (var condition in bits == 15 ? Heldout : [Baseline])
                    results.Add(Composite(brain, world, RelayWorld.Tape(91000 + seed, settings.Test, condition), variant, seed,
                        "test", bits, settings.Warmup, null, testCondition: condition,
                        trace: bits == 15 && index == 0 && condition == Baseline ? traces : null));
                if (bits != 15) continue;
                foreach (var ablation in new[] { "no-communication", "inverted-fit", "inverted-flow", "always-query", "never-query" })
                    results.Add(Composite(brain, world, RelayWorld.Tape(91000 + seed, settings.Test, Baseline), ablation,
                        seed, "test", 15, settings.Warmup, null, ablation));
                if (index == 0) ArchiveChecks(brain, settings.Output);
            }
            var shuffledWorld = new RelayWorld(Baseline); var shuffledBrain = new RelayBrain(shuffledWorld);
            results.Add(Composite(shuffledBrain, shuffledWorld, train, "shuffled-feedback", seed, "train", 15,
                settings.Warmup, null, shuffle: true));
            results.Add(Composite(shuffledBrain, shuffledWorld, RelayWorld.Tape(91000 + seed, settings.Test, Baseline),
                "shuffled-feedback", seed, "test", 15, settings.Warmup, null));
            var creditWorld = new RelayWorld(Baseline); var creditBrain = new RelayBrain(creditWorld);
            results.Add(Composite(creditBrain, creditWorld, train, "whole-route-credit", seed, "train", 15,
                settings.Warmup, null, wholeRouteCredit: true));
            results.Add(Composite(creditBrain, creditWorld, RelayWorld.Tape(91000 + seed, settings.Test, Baseline),
                "whole-route-credit", seed, "test", 15, settings.Warmup, null));
            var concurrentWorld = new RelayWorld(Baseline); var concurrentBrain = new RelayBrain(concurrentWorld);
            results.Add(Composite(concurrentBrain, concurrentWorld, train, "simultaneous-query", seed, "train", 15,
                settings.Warmup, null, simultaneousQuery: true));
            results.Add(Composite(concurrentBrain, concurrentWorld, RelayWorld.Tape(91000 + seed, settings.Test, Baseline),
                "simultaneous-query", seed, "test", 15, settings.Warmup, null));
            foreach (var neutral in new[] { "neutral-hold", "neutral-left", "neutral-alternating" })
                results.Add(Neutral(new RelayWorld(Baseline), RelayWorld.Tape(91000 + seed, settings.Test, Baseline), neutral, seed));
            foreach (var encoding in new[] { 1, 2 })
            {
                var condition = Baseline with { Name = $"encoding-{encoding}", Encoding = encoding };
                var world = new RelayWorld(condition); var brain = new RelayBrain(world);
                Composite(brain, world, RelayWorld.Tape(41000 + seed, settings.Train, condition), "A1P1Q1S1", seed,
                    "train", 15, settings.Warmup, null);
                results.Add(Composite(brain, world, RelayWorld.Tape(91000 + seed, settings.Test, condition), "A1P1Q1S1", seed,
                    "test", 15, settings.Warmup, null, testCondition: condition));
            }
            Save(settings.Output, "results.json", results); Save(settings.Output, "curves.json", curves);
            Console.WriteLine($"seed {seed}: {results.Count} rows saved; elapsed {watch.Elapsed.TotalSeconds:F1}s");
        }
        Save(settings.Output, "trace.json", traces);
        Save(settings.Output, "run.json", new { elapsedSeconds = watch.Elapsed.TotalSeconds, rows = results.Count,
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription, processors = Environment.ProcessorCount, settings });
        File.WriteAllText(Path.Combine(settings.Output, "results.csv"), Csv(results));
        File.WriteAllText(Path.Combine(settings.Output, "SUMMARY.md"), Summarize(results, settings, watch.Elapsed));
        var template = Path.Combine(AppContext.BaseDirectory, "lab.html");
        if (File.Exists(template)) File.WriteAllText(Path.Combine(settings.Output, "index.html"), File.ReadAllText(template)
            .Replace("/*RESULTS*/[]", JsonSerializer.Serialize(results)).Replace("/*CURVES*/[]", JsonSerializer.Serialize(curves))
            .Replace("/*TRACE*/[]", JsonSerializer.Serialize(traces)));
        Console.WriteLine($"Complete: {settings.Output}; {watch.Elapsed.TotalSeconds:F1}s"); return 0;
    }

    private static string FindSourceDirectory()
    {
        foreach (var starting in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
            for (var directory = new DirectoryInfo(starting); directory is not null; directory = directory.Parent)
            {
                var source = Path.Combine(directory.FullName, "experiments", "PrimitiveLearning");
                if (File.Exists(Path.Combine(directory.FullName, "Gojo.slnx")) &&
                    File.Exists(Path.Combine(source, "PrimitiveLearning.csproj")))
                    return source;
            }
        throw new DirectoryNotFoundException("Cannot locate PrimitiveLearning sources in a Gojo checkout for the run manifest.");
    }

    private static void Isolated(StudySettings settings, int seed, List<ResultRow> results)
    {
        var train = RelayWorld.Tape(41000 + seed, settings.Train, Baseline);
        var test = RelayWorld.Tape(91000 + seed, settings.Test, Baseline);
        foreach (var experiment in new[] { "distinctions", "prediction", "queries", "programs" })
        foreach (var variant in new[] { "learned", "frozen-initial", "shuffled-feedback" })
        {
            var world = new RelayWorld(Baseline); var brain = new RelayBrain(world);
            var shuffled = RelayWorld.Tape(141000 + seed, settings.Train, Baseline);
            foreach (var (phase, tape) in new[] { ("train", train), ("test", test) })
            {
                var meter = new Meter(world); var learn = phase == "train" && variant != "frozen-initial";
                var frozenSignature = phase == "test" ? brain.LearnedSignature() : null;
                foreach (var ep in tape)
                {
                    var feedback = variant == "shuffled-feedback" && learn ? shuffled[ep.Tick] : ep;
                    if (experiment == "distinctions")
                    {
                        var sample = world.Acquire(ep); var prediction = brain.Adaptive.Predict(sample, world.Compare);
                        meter.Classification(NativePublication.Read(prediction, world.FitAxis), ep.Fits ? 1 : 0, ep.Continuity);
                        if (learn) brain.Adaptive.Observe(sample, variant == "shuffled-feedback"
                            ? NativePublication.Point(world.FitAxis, feedback.Fits ? 1 : 0, sample, 0, 1) : world.ProbeFit(ep, sample), world.Compare);
                    }
                    else if (experiment == "prediction")
                    {
                        var prediction = brain.Predictor.Predict(world.Context(ep), world.Previous(ep));
                        meter.Classification(NativePublication.Read(prediction, world.DirectionAxis), ep.ArrivingDirection, ep.Continuity);
                        meter.AuditArrivals++;
                        if (learn) brain.Predictor.Observe(prediction, world.Arrival(feedback));
                    }
                    else if (experiment == "queries")
                    {
                        // Local contract is a known-fitting parcel. Prior direction is a cheap hypothesis.
                        var parcel = ep with { Fits = true };
                        var choice = brain.Query.Decide(world.Context(ep), learn);
                        var asks = NativePublication.Read(choice, brain.Query.ChoiceAxis) == 1;
                        var direction = asks ? world.QueryDirection(ep) : world.Previous(ep);
                        var action = RelayBrain.Calibrate(direction, world.DirectionAxis, world.ActionAxis, -1, 1);
                        var outcome = world.Execute(parcel, action, asks ? Baseline.QueryPrice : 0);
                        meter.Action(parcel, NativePublication.Read(action, world.ActionAxis), NativePublication.Read(outcome, world.UtilityAxis), asks);
                        meter.AuditArrivals++;
                        if (learn)
                        {
                            var benefit = world.Execute(parcel with { ArrivingDirection = feedback.ArrivingDirection }, action, shadow: true);
                            brain.Query.Observe(world.Context(ep), brain.Query.BindBenefit(RelayBrain.Calibrate(benefit, world.UtilityAxis, world.QueryBenefitAxis, -4, 4)),
                                brain.Query.BindCost(NativePublication.Point(world.CostAxis, asks ? Baseline.QueryPrice : 0, choice, 0, 16)));
                        }
                    }
                    else
                    {
                        var sample = world.Acquire(ep); var mode = world.ProbeFit(ep, sample); var direction = world.QueryDirection(ep);
                        var action = brain.Program.Act(mode, direction, learn); var outcome = world.Execute(ep, action);
                        meter.Action(ep, NativePublication.Read(action, world.ActionAxis), NativePublication.Read(outcome, world.UtilityAxis), false);
                        if (learn) brain.Program.Feedback(brain.Program.BindUtility(variant == "shuffled-feedback"
                            ? world.Execute(ep with { Fits = feedback.Fits, ArrivingDirection = feedback.ArrivingDirection }, action, shadow: true) : outcome));
                    }
                }
                var root = experiment switch { "distinctions" => brain.Adaptive.Root, "prediction" => brain.Predictor.Root,
                    "queries" => brain.Query.Root, _ => brain.Program.Root };
                if (frozenSignature is not null && frozenSignature != brain.LearnedSignature())
                    throw new InvalidDataException("An isolated learned model changed during its frozen test.");
                results.Add(meter.Row(experiment, variant, Baseline, seed, phase, tape, root,
                    experiment == "distinctions" ? brain.Adaptive.LandmarkCount : 0, experiment == "prediction" ? brain.Predictor.RowCount : 0,
                    experiment == "programs" ? brain.Program.AcceptedEdits : 0, experiment == "programs" ? brain.Program.SelectedComplexity : 0));
            }
        }
    }

    private static ResultRow Composite(RelayBrain brain, RelayWorld world, RelayEpisode[] tape, string variant, int seed,
        string phase, int bits, int warmup, List<CurvePoint>? curves, string? ablation = null,
        RelayCondition? testCondition = null, bool shuffle = false, List<TracePoint>? trace = null,
        bool wholeRouteCredit = false, bool simultaneousQuery = false)
    {
        var training = phase == "train"; var meter = new Meter(world); var block = new Meter(world);
        var frozenSignature = training ? null : brain.LearnedSignature();
        var condition = testCondition ?? world.Condition;
        var noise = shuffle ? RelayWorld.Tape(141000 + seed, tape.Length, Baseline) : tape;
        foreach (var ep in tape)
        {
            var sample = world.Acquire(ep); var fit = brain.Adaptive.Predict(sample, world.Compare);
            var forecast = brain.Predictor.Predict(world.Context(ep), world.Previous(ep));
            var mode = NativeFallback.Choose(fit, world.FitAxis, 0);
            var usable = NativeFallback.Choose(forecast, world.DirectionAxis, ep.PreviousDirection);
            if (ablation == "no-communication") { mode = NativePublication.Open(world.FitAxis, fit); usable = NativePublication.Open(world.DirectionAxis, forecast); }
            if (ablation == "inverted-fit") mode = RelayBrain.Calibrate(mode, world.FitAxis, world.FitAxis, 0, 1, invert: true);
            var queryLearn = training && (bits & 4) != 0 && (ep.Tick >= warmup || simultaneousQuery);
            var choice = brain.Query.Decide(world.Context(ep), queryLearn);
            var asks = ablation == "always-query" || ablation != "never-query" && NativePublication.Read(choice, brain.Query.ChoiceAxis) == 1;
            var direction = asks ? world.QueryDirection(ep) : usable;
            if (ablation == "no-communication") direction = NativePublication.Open(world.DirectionAxis, direction);
            if (ablation == "inverted-flow") direction = RelayBrain.Calibrate(direction, world.DirectionAxis, world.DirectionAxis, -1, 1, invert: true);
            var programLearn = training && (bits & 8) != 0 && ep.Tick >= warmup;
            var intent = brain.Program.Act(mode, direction, programLearn);
            // The actuator has an explicit hold response when intent remains open.
            // The open intent stays in this witness; paid query fees are still debited.
            var action = NativeFallback.Choose(intent, world.ActionAxis, 0);
            var outcome = world.Execute(ep, action, asks ? condition.QueryPrice : 0);
            var av = NativePublication.Read(action, world.ActionAxis); var ov = NativePublication.Read(outcome, world.UtilityAxis);
            meter.Action(ep, av, ov, asks); block.Action(ep, av, ov, asks); meter.AuditArrivals++;
            var actual = world.Arrival(ep); // Arrives after command; no skipped-query selective labels.
            if (training)
            {
                var feedback = noise[ep.Tick];
                if ((bits & 1) != 0) brain.Adaptive.Observe(sample, shuffle ? NativePublication.Point(world.FitAxis, feedback.Fits ? 1 : 0, sample, 0, 1)
                    : world.ProbeFit(ep, sample), world.Compare);
                if ((bits & 2) != 0) brain.Predictor.Observe(forecast, shuffle ? world.Arrival(feedback) : actual);
                if (queryLearn)
                {
                    var paid = NativePublication.Point(world.CostAxis, asks ? condition.QueryPrice : 0, choice, 0, 16);
                    Relation benefitRead;
                    if (wholeRouteCredit) benefitRead = RelayBrain.GrossBenefit(outcome, paid, world.UtilityAxis, world.CostAxis, world.QueryBenefitAxis);
                    else
                    {
                        var parcel = ep with { Fits = true, ArrivingDirection = feedback.ArrivingDirection };
                        var shadowAction = RelayBrain.Calibrate(direction, world.DirectionAxis, world.ActionAxis, -1, 1);
                        var benefit = world.Execute(parcel, shadowAction, shadow: true);
                        benefitRead = RelayBrain.Calibrate(benefit, world.UtilityAxis, world.QueryBenefitAxis, -4, 4);
                    }
                    brain.Query.Observe(world.Context(ep), brain.Query.BindBenefit(benefitRead), brain.Query.BindCost(paid));
                }
                if (programLearn) brain.Program.Feedback(brain.Program.BindUtility(shuffle
                    ? world.Execute(ep with { Fits = feedback.Fits, ArrivingDirection = feedback.ArrivingDirection }, action, asks ? condition.QueryPrice : 0, shadow: true) : outcome));
            }
            brain.Connect(sample, fit, forecast, choice, mode, direction, intent, action, outcome);
            if (trace is not null && trace.Count < 120) trace.Add(new(ep.Tick, ep.Continuity, ep.PreviousDirection, ep.ArrivingDirection, ep.Fits,
                ep.Unavailable, ep.Absent, NativePublication.Read(fit, world.FitAxis), NativePublication.Read(forecast, world.DirectionAxis), asks,
                NativePublication.Read(direction, world.DirectionAxis), av, ov));
            if (curves is not null && (ep.Tick + 1) % 128 == 0)
            {
                curves.Add(new(variant, seed, ep.Tick + 1, block.Return / block.Episodes, (double)block.Correct / block.Episodes,
                    brain.Adaptive.LandmarkCount, brain.Predictor.RowCount, brain.Program.AcceptedEdits)); block = new Meter(world);
            }
        }
        if (frozenSignature is not null && frozenSignature != brain.LearnedSignature())
            throw new InvalidDataException("A learned model changed during a frozen test.");
        return meter.Row("composite", variant, condition, seed, phase, tape, brain.Root, brain.Adaptive.LandmarkCount,
            brain.Predictor.RowCount, brain.Program.AcceptedEdits, brain.Program.SelectedComplexity, brain.LearnedSignature());
    }

    private static ResultRow Neutral(RelayWorld world, RelayEpisode[] tape, string variant, int seed)
    {
        var meter = new Meter(world); Relation root = Relation.OrderedGroup(new Carrier());
        foreach (var ep in tape)
        {
            var value = variant == "neutral-hold" ? 0 : variant == "neutral-left" ? -1 : ep.Tick % 2 == 0 ? -1 : 1;
            root = NativePublication.Point(world.ActionAxis, value); var outcome = world.Execute(ep, root);
            meter.Action(ep, value, NativePublication.Read(outcome, world.UtilityAxis), false);
        }
        return meter.Row("composite", variant, world.Condition, seed, "test", tape, root);
    }

    private static void ArchiveChecks(RelayBrain brain, string output)
    {
        NativeInspection.RestoreVerified(brain.Root, Path.Combine(output, "composite.gojo"));
        var qRoot = NativeInspection.RestoreVerified(brain.Query.Root, Path.Combine(output, "query.gojo"));
        var pRoot = NativeInspection.RestoreVerified(brain.Program.Root, Path.Combine(output, "program.gojo"));
        NativeInspection.RestoreVerified(brain.Predictor.Root, Path.Combine(output, "predictor.gojo"));
        NativeInspection.RestoreVerified(brain.Adaptive.Root, Path.Combine(output, "distinctions.gojo"));
        var q = QueryLearner.FromRoot(qRoot); var program = ProgramLearner.FromRoot(pRoot); var checks = new List<object>();
        var queryEvidence = new List<QueryEvidence>();
        for (var mode = 0; mode < 2; mode++)
        {
            var expectedQuery = NativePublication.Read(brain.Query.Decide(NativePublication.Point(brain.Query.ContextAxis, mode), false), brain.Query.ChoiceAxis);
            var actualQuery = NativePublication.Read(q.Decide(NativePublication.Point(q.ContextAxis, mode), false), q.ChoiceAxis);
            if (expectedQuery != actualQuery) throw new InvalidDataException("Query behavior changed after native transport.");
            foreach (var direction in new[] { -1, 1 })
            {
                var expected = NativePublication.Read(brain.Program.Act(NativePublication.Point(brain.Program.ModeAxis, mode), NativePublication.Point(brain.Program.DirectionAxis, direction), false), brain.Program.ActionAxis);
                var actual = NativePublication.Read(program.Act(NativePublication.Point(program.ModeAxis, mode), NativePublication.Point(program.DirectionAxis, direction), false), program.ActionAxis);
                if (actual != expected) throw new InvalidDataException("Program behavior changed after native transport.");
                checks.Add(new { mode, direction, action = actual, query = actualQuery });
            }
        }
        for (var context = 0; context < 2; context++) for (var choice = 0; choice < 2; choice++)
            if (QueryLearner.TryInspectEvidence(q.ReadEvidence(context, choice), out var evidence)) queryEvidence.Add(evidence);
        Save(output, "archive-check.json", new { signatures = "four native graph roundtrips passed", behavior = checks,
            queryEvidence,
            limitation = "Prediction/distinction structurally verified; opaque anchor comparison service also requires restoration for adaptive deployment." });
    }
    private static void Save<T>(string output, string name, T data) => File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(data, Json));
    private static string Csv(List<ResultRow> rows)
    {
        // JSON carries the complete native-work fields; this compact CSV is for routine analysis.
        var b = new StringBuilder("experiment,variant,condition,seed,phase,episodes,return,correct,coverage,coveredAccuracy,queries,landmarks,predictorRows,edits,complexity,carriers,relations\n");
        foreach (var r in rows) b.AppendLine(FormattableString.Invariant($"{r.Experiment},{r.Variant},{r.Condition},{r.Seed},{r.Phase},{r.Episodes},{r.ReturnPerEpisode:F6},{r.CorrectRate:F6},{r.Coverage:F6},{r.CoveredAccuracy:F6},{r.QueryRate:F6},{r.Landmarks},{r.PredictorRows},{r.AcceptedEdits},{r.SelectedComplexity},{r.Graph.Carriers},{r.Graph.Relations}"));
        return b.ToString();
    }
    private static string Summarize(List<ResultRow> rows, StudySettings settings, TimeSpan elapsed)
    {
        var b = new StringBuilder($"# Primitive learning {settings.Name}\n\n{settings.Seeds} seeds; {settings.Train} training + {settings.Test} held-out episodes per run; {elapsed.TotalSeconds:F1} seconds.\n\nTest phases freeze learned ledgers. Query fees are debited; comparison work and training audits are reported separately.\n\n");
        b.AppendLine("| Experiment | Variant | Condition | Mean return | Correct | Coverage | Queries |\n|---|---|---|---:|---:|---:|---:|");
        foreach (var g in rows.Where(r => r.Phase == "test").GroupBy(r => (r.Experiment, r.Variant, r.Condition)))
            b.AppendLine($"| {g.Key.Experiment} | {g.Key.Variant} | {g.Key.Condition} | {g.Average(r => r.ReturnPerEpisode):F3} | {g.Average(r => r.CorrectRate):P1} | {g.Average(r => r.Coverage):P1} | {g.Average(r => r.QueryRate):P1} |");
        b.AppendLine("\nFinite episodic decisions, authored local feedback contracts, and a16-program grammar. This does not establish closed-loop survival, unrestricted synthesis or hierarchy discovery. See experiment PROTOCOL.md and LITERATURE.md."); return b.ToString();
    }
    private sealed class Meter
    {
        private readonly RelayWorld world; private readonly int comparisons, probes, queries, shadows;
        private readonly StringBuilder decisions = new();
        public int Episodes, Correct, Covered, CoveredCorrect, AuditArrivals;
        private int asked, stable, unstable, stableAsked, unstableAsked, stableCovered, unstableCovered, stableCorrect, unstableCorrect, active;
        public double Return;
        public Meter(RelayWorld world) { this.world = world; comparisons = world.ComparisonQueries; probes = world.FitProbes; queries = world.DirectionQueries; shadows = world.ShadowActions; }
        public void Classification(long? value, long actual, int context)
        {
            Episodes++; if (value is not null) { Covered++; if (value == actual) { Correct++; CoveredCorrect++; } }
            Context(context, value is not null, value == actual); active++;
            decisions.Append(value?.ToString() ?? "?").Append(';');
        }
        public void Action(RelayEpisode ep, long? action, long? outcome, bool query)
        {
            Episodes++; if (action is not null && outcome is not null) { Covered++; if (!ep.Absent && action == (ep.Fits ? ep.ArrivingDirection : -ep.ArrivingDirection)) { Correct++; CoveredCorrect++; } }
            Return += outcome ?? 0; if (query) asked++;
            Context(ep.Continuity, action is not null && outcome is not null, !ep.Absent && action == (ep.Fits ? ep.ArrivingDirection : -ep.ArrivingDirection));
            if (!ep.Absent) active++;
            if (ep.Continuity == 0) { if (query) stableAsked++; } else { if (query) unstableAsked++; }
            decisions.Append(action?.ToString() ?? "?").Append(':').Append(query ? '1' : '0').Append(';');
        }
        private void Context(int context, bool covered, bool correct)
        {
            if (context == 0) { stable++; if (covered) stableCovered++; if (correct) stableCorrect++; }
            else { unstable++; if (covered) unstableCovered++; if (correct) unstableCorrect++; }
        }
        public ResultRow Row(string experiment, string variant, RelayCondition condition, int seed, string phase,
            RelayEpisode[] tape, Relation root, int landmarks = 0, int predictorRows = 0, long edits = 0, long complexity = 0, string learnedDigest = "") =>
            new(experiment, variant, condition.Name, seed, phase, Episodes, Return / Math.Max(1, Episodes), (double)Correct / Math.Max(1, Episodes),
                (double)Covered / Math.Max(1, Episodes), (double)CoveredCorrect / Math.Max(1, Covered), (double)asked / Math.Max(1, Episodes),
                (double)stableAsked / Math.Max(1, stable), (double)unstableAsked / Math.Max(1, unstable),
                (double)stableCovered / Math.Max(1, stable), (double)stableCorrect / Math.Max(1, stableCovered),
                (double)unstableCovered / Math.Max(1, unstable), (double)unstableCorrect / Math.Max(1, unstableCovered),
                (double)Correct / Math.Max(1, active), world.ComparisonQueries - comparisons,
                world.FitProbes - probes, world.DirectionQueries - queries, AuditArrivals, world.ShadowActions - shadows, landmarks,
                predictorRows, edits, complexity, NativeInspection.Count(root),
                Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(decisions.ToString()))).ToLowerInvariant(), RelayWorld.Digest(tape), learnedDigest);
    }
}

/// <summary>Schedules four native services. Connected learner roots and explicit
/// port publications/calibrations form the inspectable composite model.</summary>
public sealed class RelayBrain
{
    public AdaptiveOrdinal Adaptive { get; }
    public OrdinalPredictor Predictor { get; }
    public QueryLearner Query { get; }
    public ProgramLearner Program { get; }
    public Relation Root { get; private set; }
    public RelayBrain(RelayWorld world)
    {
        Adaptive = new(world.LoadSource, world.PresenceAxis, world.OrderAxis, world.FitAxis);
        Predictor = new(world.DirectionAxis, world.ContextAxis, world.DirectionAxis);
        Query = new(world.ContextAxis, world.QueryBenefitAxis, world.CostAxis);
        Program = new(world.FitAxis, world.DirectionAxis, world.ActionAxis, world.UtilityAxis);
        Root = Relation.OrderedGroup(new Carrier(), Adaptive.Root, Predictor.Root, Query.Root, Program.Root);
    }
    public void Connect(params Relation[] publications) => Root = Relation.OrderedGroup(new Carrier(), Adaptive.Root,
        Predictor.Root, Query.Root, Program.Root, Relation.OrderedGroup(new Carrier(), publications));
    public string LearnedSignature()
    {
        AdaptiveOrdinal.TryReadModel(Adaptive.Root, out var adaptivePolicy, out var landmarks, out _);
        OrdinalPredictor.TryReadModel(Predictor.Root, out var predictionPolicy, out var rows, out _);
        Query.Root.TryReadOrderedGroup(out var query); ((Relation)query[7]).TryReadOrderedGroup(out var queryRows);
        var queryState = queryRows.Cast<Relation>().Select(row =>
        {
            row.TryReadOrderedGroup(out var fields);
            return (IElement)Relation.OrderedGroup(new Carrier(), fields[0], fields[1], fields[3]);
        }).ToArray();
        Program.Root.TryReadOrderedGroup(out var program);
        return NativeInspection.Signature(Relation.OrderedGroup(new Carrier(), adaptivePolicy, landmarks, predictionPolicy, rows,
            Relation.Point(new Carrier(), Adaptive.Updates), Relation.Point(new Carrier(), Predictor.Updates),
            query[6], Relation.OrderedGroup(new Carrier(), queryState), program[10], program[11], program[13], program[14]));
    }
    public static Relation Calibrate(Relation publication, Carrier sourceAxis, Carrier targetAxis, long low, long high, bool invert = false)
    {
        var calibration = Equation.Affine(sourceAxis, low, high, targetAxis, invert ? high : low, invert ? low : high);
        if (!ReadWitness.TryRead(publication, out _, out _, out var result, out _, out _) || result is not CarrierLocation point || !point.Carrier.SameIdentityAs(sourceAxis))
            return NativePublication.Open(targetAxis, new Relation(publication, calibration));
        var prediction = Equation.Predict(calibration, point, targetAxis);
        if (!Equation.TryReadPrediction(prediction, out var answer, out var residue)) return NativePublication.Open(targetAxis, new Relation(publication, prediction));
        return ReadWitness.Create(new Carrier(), publication, calibration, answer, residue, prediction);
    }
    public static Relation GrossBenefit(Relation netOutcome, Relation paidCost, Carrier utility, Carrier cost, Carrier benefit)
    {
        var sum = Equation.Sum(benefit, Equation.WeightedPort(utility, 0, 1, 0, 1), Equation.WeightedPort(cost, 0, 1, 0, 1));
        if (!ReadWitness.TryRead(netOutcome, out _, out _, out var net, out _, out _) || net is not CarrierLocation ||
            !ReadWitness.TryRead(paidCost, out _, out _, out var fee, out _, out _) || fee is not CarrierLocation)
            return NativePublication.Open(benefit, new Relation(netOutcome, paidCost));
        var prediction = Equation.Predict(sum, new Relation(net, fee), benefit);
        if (!Equation.TryReadPrediction(prediction, out var result, out var residue)) return NativePublication.Open(benefit, prediction);
        return ReadWitness.Create(new Carrier(), new Relation(netOutcome, paidCost), sum, result, residue, prediction);
    }
}
