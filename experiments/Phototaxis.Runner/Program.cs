using System.Diagnostics;
using System.Text.Json;
using Phototaxis;
using Gojo.Core;
using static Phototaxis.Native;

var backendIndex = Array.IndexOf(args, "--backend");
var backend = NativeExecutionBackend.Compact;
if (backendIndex >= 0)
{
    if (backendIndex + 1 >= args.Length || !Enum.TryParse(args[backendIndex + 1], true, out backend) || !Enum.IsDefined(backend))
        throw new ArgumentException("--backend must be compact, compactsimd, planned, or reference.");
    args = args.Where((_, i) => i != backendIndex && i != backendIndex + 1).ToArray();
}
using var execution = new NativeExecution(backend);

if (args.Length > 0 && args[0] == "trials") { await ParallelTrials.Run(args, backend); return; }
if (args.Length > 0 && args[0] == "design") { DesignTrials.Run(args); return; }
if (args.Length > 0 && args[0] == "cognitive") { await CognitiveTrials.Run(args); return; }
if (args.Length > 0 && args[0] == "vision") { await VisionTrials.Run(args); return; }
if (args.Length > 0 && args[0] is "ecology" or "roving") { await EcologyTrials.Run(args); return; }

if (args.Length > 0 && args[0] == "inspect")
{
    var roots = GojoNumericFormat.Decode(File.ReadAllBytes(args[1]));
    var parts = roots.Length == 1 && roots[0] is Relation root ? Parts(root) : [];
    if (parts.Length == 9)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { roots = roots.Length, creature = DescribeCreature(parts, 1) },
            new JsonSerializerOptions { WriteIndented = true }));
        return;
    }
    if (parts.Length != 6 || parts[1] is not Carrier archiveClock)
    { Console.WriteLine("Decoded archive; no complete bath surface is exposed."); return; }
    var calories = Parts((Relation)parts[2]).Cast<Carrier>().Sum(c => (long)c.Value);
    var attention = Parts((Relation)parts[3]).Cast<Carrier>().Sum(c => (long)c.Value);
    var creatures = Parts((Relation)parts[4]).Cast<Relation>().Select(Parts).ToArray();
    foreach (var creature in creatures)
    {
        calories += (long)((Carrier)Parts((Relation)creature[3])[0]).Value;
        attention += (long)((Carrier)Parts((Relation)creature[5])[0]).Value;
    }
    // The original dot bath has three calorie stocks; the photosynthetic bath
    // has a fourth shared pool and exposes its economy in the final group.
    if (Parts((Relation)parts[2]).Length == 3)
        foreach (var dot in Parts((Relation)parts[5]).Cast<Relation>()) calories += (long)((Carrier)Parts(dot)[2]).Value;
    Console.WriteLine(JsonSerializer.Serialize(new { roots = roots.Length, ticks = archiveClock.Value,
        population = creatures.Length, calories, attention,
        circuits = creatures.Select((c, i) => DescribeCreature(c, i + 1)).ToArray() },
        new JsonSerializerOptions { WriteIndented = true }));
    return;
}

if (args.Length > 0 && args[0] == "train")
{
    var simulationSeconds = args.Length > 1 ? int.Parse(args[1]) : 300;
    var seed = args.Length > 2 ? int.Parse(args[2]) : 7;
    var trainPopulation = args.Length > 3 ? int.Parse(args[3]) : 20;
    var output = Path.GetFullPath(args.Length > 4 ? args[4] :
        Path.Combine("artifacts", "phototaxis", "training-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
    var wallLimit = args.Length > 5 ? double.Parse(args[5], System.Globalization.CultureInfo.InvariantCulture) : 120;
    if (simulationSeconds <= 0 || trainPopulation <= 0 || wallLimit <= 0)
        throw new ArgumentException("Training seconds, population and wall limit must be positive.");
    Directory.CreateDirectory(output);
    var cancelled = false;
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelled = true; };
    var initialization = Stopwatch.StartNew();
    var fixedResources = args.Contains("--fixed-resources");
    var world = new Bath(seed, trainPopulation, resources: fixedResources ? MachineResources.Fixed(.9) : null);
    initialization.Stop();
    var timer = Stopwatch.StartNew();
    var allocatedBefore = GC.GetTotalAllocatedBytes();
    var target = checked(simulationSeconds * (long)Bath.TicksPerSecond);
    var nextReport = Bath.TicksPerSecond * 10L;
    var lastFood = 0L;
    var lastDeaths = 0L;
    var lastReportTick = 0L;
    var lastReportWall = 0.0;
    using (var progress = new StreamWriter(Path.Combine(output, "progress.jsonl")))
    {
        while (world.Tick < target && !cancelled && world.Creatures.Length != 0 && timer.Elapsed.TotalSeconds < wallLimit)
        {
            // The simulation clock advances one tick regardless of wall speed.
            world.Step();
            if (world.Tick < nextReport && world.Tick < target && world.Creatures.Length != 0) continue;
            world.CheckConservation();
            var wall = timer.Elapsed.TotalSeconds;
            var deaths = world.PredationDeaths + world.StarvationDeaths;
            var row = JsonSerializer.Serialize(new {
                seed, ticks = world.Tick, simulatedSeconds = world.Seconds,
                actualSpeed = (world.Tick - lastReportTick) / (double)Bath.TicksPerSecond / (wall - lastReportWall),
                caloriesAbsorbed = world.TotalFood, windowCaloriesAbsorbed = world.TotalFood - lastFood,
                population = world.Creatures.Length, components = world.Components, operations = world.Operations, rewrites = world.Rewrites,
                machineDuty = world.Resources.Duty, resourceYield = world.FoodAvailability, computeCalories = world.ComputeCalories,
                meanEnergy = world.MeanEnergy, meanExposure = world.MeanExposure, meanSpeed = world.MeanSpeed,
                stationaryFraction = world.StationaryFraction, stationaryShare = world.StationaryShare,
                hungryTowardLight = world.HungryTowardLight, fullAwayFromLight = world.FullAwayFromLight,
                births = world.Births, windowDeaths = deaths - lastDeaths,
                credits = world.SpentAttention.Value,
                unresolved = world.Creatures.Sum(c => c.LeftMuscle.Unresolved + c.RightMuscle.Unresolved)
            });
            Console.WriteLine(row); progress.WriteLine(row); progress.Flush();
            nextReport += Bath.TicksPerSecond * 10;
            lastFood = world.TotalFood; lastDeaths = deaths; lastReportTick = world.Tick; lastReportWall = wall;
        }
    }
    timer.Stop();
    world.CheckConservation();
    var archiveTimer = Stopwatch.StartNew();
    // Complete retained native world, for inspection. External random state and
    // host schedules are not included: this is not a resumable world checkpoint.
    var graph = Gojo.Core.GojoNumericFormat.Encode(world.Root);
    var archivePath = Path.Combine(output, "world.gojo");
    File.WriteAllBytes(archivePath, graph);
    archiveTimer.Stop();
    var process = Process.GetCurrentProcess();
    var summary = new {
        seed, initialPopulation = trainPopulation, population = world.Creatures.Length, components = world.Components, operations = world.Operations,
        measuredResources = world.Resources.Measured, machineDuty = world.Resources.Duty,
        resourceYield = world.FoodAvailability, computeCalories = world.ComputeCalories, rewrites = world.Rewrites,
        requestedSeconds = simulationSeconds, simulatedSeconds = world.Seconds,
        ticks = world.Tick, stopReason = cancelled ? "cancelled" : world.Creatures.Length == 0 ? "extinction" : world.Tick >= target ? "target reached" : "wall limit reached",
        wallLimitSeconds = wallLimit, initializationSeconds = initialization.Elapsed.TotalSeconds,
        computeSeconds = timer.Elapsed.TotalSeconds, actualSpeed = world.Seconds / timer.Elapsed.TotalSeconds,
        archiveSeconds = archiveTimer.Elapsed.TotalSeconds, archiveBytes = graph.Length, archivePath,
        caloriesAbsorbed = world.TotalFood, births = world.Births,
        predation = world.PredationDeaths, starvation = world.StarvationDeaths,
        meanEnergy = world.MeanEnergy, meanExposure = world.MeanExposure, credits = world.SpentAttention.Value,
        meanSpeed = world.MeanSpeed, stationaryFraction = world.StationaryFraction, stationaryShare = world.StationaryShare,
        hungryTowardLight = world.HungryTowardLight, fullAwayFromLight = world.FullAwayFromLight,
        hungryMovementSamples = world.HungryMovementSamples, fullMovementSamples = world.FullMovementSamples,
        unresolved = world.Creatures.Sum(c => c.LeftMuscle.Unresolved + c.RightMuscle.Unresolved),
        allocatedBytesIncludingArchive = GC.GetTotalAllocatedBytes() - allocatedBefore,
        managedHeapBytes = GC.GetTotalMemory(false), peakWorkingSetBytes = process.PeakWorkingSet64,
        caloriesConserved = world.TotalCalories == 20_000_000, attentionConserved = world.TotalAttention == 10_000_000
    };
    var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(output, "summary.json"), json);
    Console.WriteLine(json);
    return;
}

if (args.Length > 0 && args[0] == "profile")
{
    var cognitive = args.Contains("--cognitive");
    var arenaIndex = Array.IndexOf(args, "--arena-scale");
    var arenaScale = arenaIndex < 0 ? 1 : double.Parse(args[arenaIndex + 1], System.Globalization.CultureInfo.InvariantCulture);
    var seedIndex = Array.IndexOf(args, "--seed");
    var profileSeed = seedIndex < 0 ? 7 : int.Parse(args[seedIndex + 1]);
    var profilePopulation = args.Length > 2 ? int.Parse(args[2]) : 20;
    var secondsToRun = args.Length > 1 ? int.Parse(args[1]) : 15;
    var warmupIndex = Array.IndexOf(args, "--warmup");
    var warmupSeconds = warmupIndex < 0 ? 0 : int.Parse(args[warmupIndex + 1]);
    if (secondsToRun <= 0 || profilePopulation <= 0 || warmupSeconds < 0)
        throw new ArgumentException("Profile seconds and population must be positive; warmup must be nonnegative.");
    var bath = new Bath(profileSeed, profilePopulation,
        resources:args.Contains("--fixed-resources") ? MachineResources.Fixed(.9) : null,
        rocks:cognitive, pulsedLights:cognitive, cognitiveEcology:cognitive,
        ecology:args.Contains("--roving") ? EcologySettings.Roving : EcologySettings.Original,
        arenaScale:arenaScale, pilotFeatures:new(!args.Contains("--no-progress"), !args.Contains("--no-courtship")))
        { Measure = new PhaseCosts(), Learning = !cognitive };
    bath.Step(checked(warmupSeconds * Bath.TicksPerSecond));
    bath.Measure = new PhaseCosts();
    var batchesBefore = execution.Batches; var buildsBefore = execution.PlanBuilds;
    var hitsBefore = execution.CacheHits; var stepsBefore = execution.NumericSteps; var fallbacksBefore = execution.Fallbacks;
    var deferredBefore = execution.DeferredBatches;
    var simdBatchesBefore = execution.SimdBatches; var simdLanesBefore = execution.SimdLanes;
    var collectionsBefore = Enumerable.Range(0, 3).Select(GC.CollectionCount).ToArray();
    var allocatedBefore = GC.GetTotalAllocatedBytes();
    using var profileProcess = Process.GetCurrentProcess();
    var cpuBefore = profileProcess.TotalProcessorTime;
    var timer = Stopwatch.StartNew();
    bath.Step(checked(secondsToRun * Bath.TicksPerSecond));
    timer.Stop();
    var cpuSeconds = (profileProcess.TotalProcessorTime - cpuBefore).TotalSeconds;
    var runAllocation = GC.GetTotalAllocatedBytes() - allocatedBefore;
    var runCollections = Enumerable.Range(0, 3).Select(i => GC.CollectionCount(i) - collectionsBefore[i]).ToArray();
    bath.CheckConservation();
    var state = new { bath.Tick, bath.TotalFood, bath.Births, bath.PredationDeaths, bath.StarvationDeaths,
        credits = bath.SpentAttention.Value, calories = bath.TotalCalories, attention = bath.TotalAttention,
        bodies = bath.Creatures.Select(c => new { c.Slot, x = c.X.Ticks, y = c.Y.Ticks, heading = c.Heading.Ticks,
            energy = c.Energy.Value, tension = c.Energy.Tension, food = c.Eaten.Value, age = c.Age.Value,
            left = c.LeftMuscle.Force.Value, right = c.RightMuscle.Force.Value,
            leftTension = c.LeftMuscle.Force.Tension, rightTension = c.RightMuscle.Force.Tension,
            brain = c.Cognition is { } brain ? new { speed = Held(brain.SpeedSquared), forecast = Held(brain.Forecast),
                error = Held(brain.Error), pressure = Held(brain.Pressure), samples = brain.Samples.Value } : null }).ToArray() };
    var stateDigest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state)));
    Console.WriteLine(JsonSerializer.Serialize(new { seed = profileSeed, initialPopulation = profilePopulation,
        secondsToRun, warmupSeconds, cognitive, bath.Ecology, bath.ArenaScale,
        fixedResources = !bath.Resources.Measured, nativeAssembly = typeof(Bath).Module.ModuleVersionId,
        backend = backend.ToString(), wallSeconds = timer.Elapsed.TotalSeconds, actualSpeed = secondsToRun / timer.Elapsed.TotalSeconds,
        alive = bath.Creatures.Length, allocatedBytes = runAllocation, cpuSeconds,
        host = ParallelTrials.HostInfo(), simdSupported = NativeExecution.SimdSupported,
        collections = runCollections,
        execution = new { batches = execution.Batches - batchesBefore, planBuilds = execution.PlanBuilds - buildsBefore,
            cacheHits = execution.CacheHits - hitsBefore, numericSteps = execution.NumericSteps - stepsBefore,
            fallbacks = execution.Fallbacks - fallbacksBefore, deferredBatches = execution.DeferredBatches - deferredBefore,
            simdBatches = execution.SimdBatches - simdBatchesBefore, simdLanes = execution.SimdLanes - simdLanesBefore },
        stateDigest, phases = bath.Measure.Seconds }));
    return;
}

if (args.Length > 0 && args[0] == "checkpoint")
{
    var seed = int.Parse(args[1]);
    var ticks = int.Parse(args[2]);
    var path = Path.GetFullPath(args[3]);
    var world = new Bath(seed, resources: MachineResources.Fixed(.9));
    world.Step(ticks);
    world.CheckConservation();
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    var selected = world.Creatures.FirstOrDefault();
    var bytes = selected is null ? GojoNumericFormat.Encode(world.Root) : selected.ExportNativeGraph();
    File.WriteAllBytes(path, bytes);
    var restored = Gojo.Core.GojoNumericFormat.Decode(bytes);
    Console.WriteLine(JsonSerializer.Serialize(new { path, bytes = bytes.Length, roots = restored.Length,
        seed, ticks, food = world.TotalFood, births = world.Births, credits = world.SpentAttention.Value,
        population = world.Creatures.Length, energy = selected?.Energy.Value,
        leftForce = selected?.LeftMuscle.Force.Value, rightForce = selected?.RightMuscle.Force.Value }));
    return;
}

var seconds = args.Length > 0 ? int.Parse(args[0]) : 60;
var seeds = args.Length > 1 ? args[1].Split(',').Select(int.Parse).ToArray() : [7, 19, 41];
var population = args.Length > 2 ? int.Parse(args[2]) : 20;
var results = new List<object>();
foreach (var seed in seeds)
foreach (var learning in new[] { false, true })
{
    var bath = new Bath(seed, population, resources: MachineResources.Fixed(.9)) { Learning = learning };
    var timer = Stopwatch.StartNew();
    var allocatedBefore = GC.GetTotalAllocatedBytes();
    while (bath.Seconds < seconds && bath.Creatures.Length != 0) bath.Step();
    bath.CheckConservation();
    timer.Stop();
    results.Add(new {
        seed, learning, requestedSeconds = seconds, simulatedSeconds = bath.Seconds, initialPopulation = population,
        population = bath.Creatures.Length, caloriesAbsorbed = bath.TotalFood, components = bath.Components, operations = bath.Operations,
        rewrites = bath.Rewrites, measuredResources = false, resourceYield = bath.FoodAvailability, computeCalories = bath.ComputeCalories,
        predation = bath.PredationDeaths, starvation = bath.StarvationDeaths,
        births = bath.Births, meanEnergy = bath.MeanEnergy, meanExposure = bath.MeanExposure, meanSpeed = bath.MeanSpeed,
        stationaryFraction = bath.StationaryFraction, stationaryShare = bath.StationaryShare,
        hungryTowardLight = bath.HungryTowardLight, fullAwayFromLight = bath.FullAwayFromLight,
        hungryMovementSamples = bath.HungryMovementSamples, fullMovementSamples = bath.FullMovementSamples,
        credits = bath.SpentAttention.Value,
        reviews = bath.Creatures.Sum(c => c.LeftMuscle.Reviews + c.RightMuscle.Reviews),
        unresolved = bath.Creatures.Sum(c => c.LeftMuscle.Unresolved + c.RightMuscle.Unresolved),
        elapsedSeconds = timer.Elapsed.TotalSeconds,
        allocatedBytes = GC.GetTotalAllocatedBytes() - allocatedBefore,
        caloriesConserved = bath.TotalCalories == 20_000_000,
        attentionConserved = bath.TotalAttention == 10_000_000
    });
    Console.WriteLine(JsonSerializer.Serialize(results[^1]));
}
if (args.Length > 3)
    File.WriteAllText(args[3], JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));

static object DescribeCreature(IElement[] parts, int index)
{
    if (parts.Length != 9 || parts[2] is not Relation controllers || parts[4] is not Relation catalog)
        return new { index, processing = "unresolved creature surface" };
    var muscles = Parts(controllers);
    if (muscles.Length < 2 || muscles[0] is not Relation a || muscles[1] is not Relation b)
        return new { index, processing = "unresolved muscle surfaces" };
    var left = Parts(a); var right = Parts(b);
    if (left.Length < 11 || right.Length < 11 || left[3] is not Relation l || right[3] is not Relation r ||
        !CircuitInspection.TryComponents(l, out var leftTerms, out _) || !CircuitInspection.TryComponents(r, out var rightTerms, out _))
        return new { index, processing = "circuit format is not exposed by this archive" };
    var feedback = muscles.Length > 3 && muscles[3] is Relation cells ? Parts(cells).OfType<Relation>().Select(cell =>
    {
        var fields = Parts(cell);
        return new { value = fields.Length == 6 && fields[0] is Carrier stock ? (long?)Held(stock) : null,
            period = fields.Length == 6 && fields[3] is Carrier interval ? (long?)interval.Value : null,
            recipe = fields.Length == 6 ? ExpressionGraph.Describe(catalog, fields[1]) : "open feedback",
            operations = fields.Length == 6 ? ExpressionGraph.Count(catalog, fields[1]) : 0 };
    }).ToArray() : [];
    return new { index, processingTerms = leftTerms.Length + rightTerms.Length, marketTermLimit = Circuit.MaximumComponents * 2,
        operations = ExpressionGraph.Count(catalog, leftTerms.Concat(rightTerms).Select(t => Parts(t)[0]).ToArray()),
        marketOperationBudgetPerMuscle = ExpressionGraph.MaximumOperations, processingGraph = CircuitInspection.Size(l, r), feedback,
        left = DescribeCircuit(l, catalog), right = DescribeCircuit(r, catalog),
        cognition = muscles.Length > 4 && muscles[4] is Relation brain ? DescribeCognition(brain) : null,
        lastLeftEdit = DescribeSizeChange((Relation)left[10]), lastRightEdit = DescribeSizeChange((Relation)right[10]) };
}

static object? DescribeCognition(Relation brain)
{
    var fields = Parts(brain); if (fields.Length != 8) return null;
    var observations = Parts((Relation)fields[0]); var motion = Parts((Relation)fields[2]);
    var prediction = Parts((Relation)fields[3]); var pilot = Parts((Relation)fields[5]);
    var inquiry = Parts((Relation)fields[6]); var memory = Parts((Relation)pilot[1]);
    return new { ownMotion = new { speedSquared = Held((Carrier)motion[1]),
            formula = "(wrapped dx^2 + wrapped dy^2) * 400 / (256 * dt^2)",
            currentMotionRead = TryValue(ReferenceRead((Relation)motion[0]), out var value) ? (long?)value : null },
        prediction = new { expected = Held((Carrier)prediction[1]), actualError = Held((Carrier)prediction[6]),
            pressure = ((Carrier)prediction[5]).Tension, blocked = Held((Carrier)prediction[7]), pain = Held((Carrier)prediction[8]),
            predictionChecks = ((Carrier)prediction[11]).Value },
        appetite = Held((Carrier)memory[0]), proximity = new { left = Held((Carrier)observations[6]), right = Held((Carrier)observations[7]) },
        modules = Parts((Relation)pilot[0]).Cast<Relation>().Select(module =>
        {
            var m = Parts(module); var utility = Parts((Relation)m[2]);
            return new { parameters = Parts((Relation)m[0]).Cast<Carrier>().Select(Held).ToArray(),
                observedReturns = Held((Carrier)utility[1]), support = ((Carrier)utility[0]).Value,
                estimatedUtility = m.Length > 5 ? Held((Carrier)m[5]) : 0 };
        }).ToArray(),
        inquiry = new { fuel = Held((Carrier)inquiry[0]), requests = ((Carrier)inquiry[4]).Value,
            answers = ((Carrier)inquiry[5]).Value, stale = ((Carrier)inquiry[6]).Value,
            failed = ((Carrier)inquiry[7]).Value, nonzeroTrials = ((Carrier)inquiry[8]).Value } };
}

static object? DescribeSizeChange(Relation step) => CircuitInspection.TryLastSizeChange(step, out var before, out var after)
    ? new { before, after } : null;

static object DescribeCircuit(Relation circuit, Relation catalog)
{
    string[] labels = ["constant", "left eye", "right eye", "energy deficit", "fullness", "left eye × deficit",
        "right eye × deficit", "left eye × fullness", "right eye × fullness"];
    var fields = Parts(CircuitInspection.Observe(circuit));
    if (fields.Length != 3) return new { processing = "unresolved circuit surface" };
    var terms = Parts((Relation)fields[1]).Cast<Relation>().Select(term =>
    {
        var item = Parts(term); var source = Parts((Relation)item[0])[0];
        var index = CircuitInspection.SourceIndex(catalog, source);
        return new { input = index >= 0 && index < labels.Length ? labels[index] : "other input", sourceIndex = index,
            expression = ExpressionGraph.Describe(catalog, source), operations = ExpressionGraph.Count(catalog, source),
            depth = ExpressionGraph.Depth(catalog, source),
            inputValue = TryValue((Relation)item[1], out var input) ? (long?)input : null,
            coefficient = ((CarrierLocation)item[2]).Ticks,
            contribution = TryValue((Relation)item[3], out var value) ? (long?)value : null };
    }).ToArray();
    return new { terms, computedSum = TryValue((Relation)fields[2], out var total) ? (long?)total : null };
}
