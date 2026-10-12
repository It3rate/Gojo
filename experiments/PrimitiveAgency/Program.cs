using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using PrimitiveAgency;

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
try
{
    var sweep = args.Contains("--sweep");
    var output = sweep ? "artifacts/primitive-agency-sweep" : "artifacts/primitive-agency";
    var ticks = 600;
    var iterations = 2048;
    if (args.Contains("--help"))
    {
        Console.WriteLine("PrimitiveAgency [--sweep] --output <directory> --ticks <1..2400> --math-iterations <256..16384>");
        return 0;
    }
    for (var i = 0; i < args.Length; i++)
    {
        var flag = args[i];
        if (flag == "--sweep") continue;
        if (i + 1 == args.Length) throw new ArgumentException($"Missing value for {flag}.");
        var value = args[++i];
        switch (flag)
        {
            case "--output": output = value; break;
            case "--ticks":
                if (!int.TryParse(value, CultureInfo.InvariantCulture, out ticks) || ticks is < 1 or > 2400)
                    throw new ArgumentException("--ticks must be between 1 and 2400.");
                break;
            case "--math-iterations":
                if (!int.TryParse(value, CultureInfo.InvariantCulture, out iterations) || iterations is < 256 or > 16384)
                    throw new ArgumentException("--math-iterations must be between 256 and 16384.");
                break;
            default: throw new ArgumentException($"Unknown option {flag}.");
        }
    }
    output = Path.GetFullPath(output);
    Directory.CreateDirectory(output);
    // Warm the three execution adapters and the local native rule reader before
    // the recorded trials. Startup, controller construction, file output, and
    // archive readback are excluded from each trial's loop timer.
    foreach (var backend in new[] { "Computed", "Lookup", "Packed" })
        Trials.Run(2, "memory_heat_query", backend, 24, Path.Combine(output, "warmup"));
    var trials = sweep ? ReceiverVariant.All.SelectMany(variant => new[] { "Computed", "Lookup", "Packed" }
        .Select(backend => Trials.Run(2, "memory_heat_query", backend, ticks, output, variant, "discovery"))).ToArray() : new[]
    {
        Trials.Run(0, "stay", "Computed", ticks, output),
        Trials.Run(0, "continue", "Computed", ticks, output),
        Trials.Run(1, "seek_avoid", "Computed", ticks, output),
        Trials.Run(1, "seek_avoid", "Lookup", ticks, output),
        Trials.Run(2, "round2_no_query", "Computed", ticks, output),
        Trials.Run(2, "memory_heat_query", "Computed", ticks, output),
        Trials.Run(2, "memory_heat_query", "Lookup", ticks, output),
        Trials.Run(2, "memory_heat_query", "Packed", ticks, output)
    };
    var trialParity = TrajectoriesMatch(trials);
    var comparisons = sweep ? ReceiverVariant.All.Select(variant => CompareMath(iterations,
        TrajectoriesMatch(trials.Where(trial => trial.VariantId == variant.Id)), variant)).ToArray() : new[] { CompareMath(iterations, trialParity) };
    var verificationTrials = Array.Empty<Trial>();
    SweepSelection? selection = null;
    if (sweep)
    {
        var baseline = trials.Single(trial => trial.VariantId == ReceiverVariant.Baseline.Id && trial.Backend == "Computed");
        var qualifying = trials.Where(trial => trial.Backend == "Computed" && trial.Summary.StarvationTicks == 0 && trial.Summary.OverheatTicks == 0 &&
                trial.Summary.CompletedCycles >= baseline.Summary.CompletedCycles)
            .OrderBy(trial => trial.Summary.RefinementQueries).ThenByDescending(trial => trial.Summary.MeanCharge)
            .ThenByDescending(trial => trial.Summary.CompletedCycles)
            .ThenBy(trial => trial.VariantId == ReceiverVariant.Baseline.Id ? 0 : 1).ThenBy(trial => trial.VariantId, StringComparer.Ordinal).ToArray();
        var chosen = qualifying.FirstOrDefault() ?? baseline;
        var variant = ReceiverVariant.All.Single(candidate => candidate.Id == chosen.VariantId);
        selection = new(variant.Id, chosen.Id, baseline.Id, baseline.Summary.CompletedCycles, qualifying.Select(trial => trial.VariantId).ToArray(),
            "Require zero starvation, zero overheat, and at least the reference cycle count. Minimize queries, then prefer higher mean charge, then more cycles. On a complete tie keep the existing strict reference before comparing variant ids.", 2400,
            "Longer validation in the same periodic world; this is not a held-out world.");
        var verification = new List<Trial>();
        foreach (var backend in new[] { "Computed", "Lookup", "Packed" })
        {
            verification.Add(Trials.Run(2, "memory_heat_query", backend, 2400, Path.Combine(output, "verification"), ReceiverVariant.Baseline, "reference"));
            verification.Add(Trials.Run(2, "memory_heat_query", backend, 2400, Path.Combine(output, "verification"), variant, "selected"));
        }
        verification.Add(Trials.Run(2, "round2_no_query", "Packed", 2400, Path.Combine(output, "verification"), variant, "no_query"));
        verificationTrials = verification.ToArray();
    }
    var overallParity = trialParity && TrajectoriesMatch(verificationTrials);
    var comparison = comparisons[0] with
    {
        AllBackendsMatch = comparisons.All(item => item.AllBackendsMatch) && overallParity,
        TrialParity = overallParity,
        AllInputMasksMatch = comparisons.All(item => item.AllInputMasksMatch),
        MaskPairsChecked = comparisons.Sum(item => item.MaskPairsChecked)
    };
    var config = new
    {
        cells = ChargeWorld.Cells, ticks, relocationEvery = ChargeWorld.RelocationEvery,
        chargeMax = ChargeWorld.Maximum, heatMax = ChargeWorld.Maximum, queryChargeCost = ChargeWorld.QueryChargeCost, handDesigned = true,
        lightMaskAlphabet = new[] { "0–14", "15–44", "45–79", "80–100" },
        chargeMaskAlphabet = new[] { "0–24", "25–49", "50–74", "75–100" },
        heatMaskAlphabet = new[] { "0–24", "25–49", "50–74", "75–100" },
        gradientAlphabet = new[] { "negative · left brighter", "zero", "positive · right brighter" },
        modeLabels = new[] { "Seek charge", "Cool" },
        actionLabels = new Dictionary<string, string> { ["-1"] = "Move left", ["0"] = "Stay", ["1"] = "Move right" },
        trendLabels = new Dictionary<string, string> { ["-1"] = "Dimming", ["0"] = "Unchanged", ["1"] = "Brightening" },
        sourceBoundary = "The world publishes coarse left/right light, charge, heat, and a sign-only temporal-light comparison. No private position, peak location, charge value, heat value, or score is written to controller memory.",
        stepOrder = "Each frame shows the physical state and sensors before its action; the command produces the following frame.",
        cycleDefinition = "Charge reaches 80; later heat is at most 20, distance from the charger is at least 3, and charge is at least 20. This is external outcome accounting only.",
        worldRules = new[] { "31 cells; source relocates among 23, 6, 25, 10 every 160 ticks.", "Light falls by 4 per cell from 100 at the source.",
            "Charge change near the source: +9/+6/+3 at distance 0/1/2; elsewhere −1, with an extra −1 for actual movement.",
            "Heat rises 5 within 2 cells, cools 4 outside; heat 90 or higher drains 3 extra charge.",
            "Each optional spatial comparison costs 1 charge unit, accounted with that frame's action.",
            "Starvation ticks mean charge≤5; overheat ticks mean heat≥90. Trials remain recoverable rather than terminating." }
    };
    var defaultTrialId = selection?.SelectedTrialId ?? "memory_heat_query_computed";
    var result = new { generatedAt = DateTimeOffset.UtcNow, config, trials, verificationTrials, selection, defaultTrialId,
        mathComparison = comparison, mathComparisons = comparisons };
    var json = JsonSerializer.Serialize(result, options);
    File.WriteAllText(Path.Combine(output, "results.json"), json, new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(output, "report.md"), sweep ? SweepReport(trials, verificationTrials, comparisons, selection!, comparison.AllBackendsMatch) : Report(trials, comparison), new UTF8Encoding(false));
    var template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "lab.html"));
    if (!template.Contains("__AGENCY_DATA__", StringComparison.Ordinal)) throw new InvalidDataException("The presentation template has no data placeholder.");
    File.WriteAllText(Path.Combine(output, "index.html"), template.Replace("__AGENCY_DATA__", json, StringComparison.Ordinal), new UTF8Encoding(false));
    Console.WriteLine(JsonSerializer.Serialize(new { output, comparison.AllBackendsMatch, selection,
        trials = trials.Select(trial => new { trial.Id, trial.Summary }), verificationTrials = verificationTrials.Select(trial => new { trial.Id, trial.Summary }), mathComparisons = comparisons }, options));
    return comparison.AllBackendsMatch ? 0 : 1;
}
catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static bool TrajectoriesMatch(IEnumerable<Trial> trials) => trials.GroupBy(trial => $"{trial.Phase}/{trial.VariantId}/{trial.Label}")
    .All(group => group.Select(trial => trial.Summary.ParityHash).Distinct().Count() == 1);

static MathComparison CompareMath(int iterations, bool trialParity, ReceiverVariant? variant = null)
{
    var brain = new NativeBrain(2, "memory_heat_query", "Computed", variant);
    var input = brain.LightPartition;
    var output = brain.GradientPartition;
    var law = brain.DifferenceLaw;
    var timer = Stopwatch.StartNew();
    var table = CoarseReadSurface.CompileDifference(new Carrier(), law);
    timer.Stop();
    var nativeCompileMs = timer.Elapsed.TotalMilliseconds;
    timer.Restart();
    var plan = CoarseDifferencePlan.Capture(table) ?? throw new InvalidDataException("Native lookup capture failed.");
    timer.Stop();
    var packedCaptureMs = timer.Elapsed.TotalMilliseconds;
    var publications = Enumerable.Range(0, 16).Select(mask => CoarseReadSurface.Publish(new Carrier(), input, mask, new Placeholder())).ToArray();
    Relation Read(string backend, Relation a, Relation b) => backend switch
    {
        "Computed" => CoarseReadSurface.DifferenceComputed(new Carrier(), law, a, b),
        "Lookup" => CoarseReadSurface.DifferenceLookup(new Carrier(), table, a, b),
        _ => plan.Evaluate(new Carrier(), a, b)
    };
    var exhaustive = true;
    for (var a = 0; a < publications.Length; a++)
        for (var b = 0; b < publications.Length; b++)
        {
            var masks = new[] { "Computed", "Lookup", "Packed" }.Select(backend => NativeBrain.Mask(Read(backend, publications[a], publications[b]), output)).ToArray();
            exhaustive &= masks.Distinct().Count() == 1;
        }
    // Identical nonempty-mask sequence; warm all adapters, then rotate their
    // order across three passes. Every timed read includes native observation
    // traversal and witness allocation; this is not a raw array microbenchmark.
    var backends = new[] { "Computed", "Lookup", "Packed" };
    foreach (var backend in backends)
        for (var n = 0; n < 256; n++) Read(backend, publications[1 + n % 15], publications[1 + n * 7 % 15]);
    var timings = new List<MathTiming>();
    for (var pass = 0; pass < 3; pass++)
        for (var slot = 0; slot < 3; slot++)
        {
            var backend = backends[(slot + pass) % 3];
            var allocation = GC.GetAllocatedBytesForCurrentThread();
            var checksum = 0L;
            timer.Restart();
            for (var n = 0; n < iterations; n++)
            {
                var result = Read(backend, publications[1 + n % 15], publications[1 + n * 7 % 15]);
                checksum += NativeBrain.Mask(result, output) ?? -1;
            }
            timer.Stop();
            timings.Add(new(backend, pass + 1, iterations, timer.Elapsed.TotalMilliseconds,
                GC.GetAllocatedBytesForCurrentThread() - allocation, checksum));
        }
    var sameChecksums = timings.Select(timing => timing.Checksum).Distinct().Count() == 1;
    var medians = backends.Select(backend =>
    {
        var measurements = timings.Where(timing => timing.Backend == backend).OrderBy(timing => timing.ElapsedMs).ToArray();
        var middle = measurements[1];
        return new MathBackend(backend, middle.ElapsedMs, middle.ElapsedMs * 1_000_000 / iterations,
            (double)middle.AllocatedBytes / iterations);
    }).ToArray();
    return new(exhaustive && sameChecksums && trialParity, trialParity, exhaustive, 256, nativeCompileMs, packedCaptureMs,
        "Right minus left over native region unions. Computed arithmetic, validated native row traversal, and captured packed rows preserve the same exposed contract.",
        "Three warmed passes in rotating order; native reads, result inspections, and witness allocations included. Policy construction and capture are reported separately. These short process-local timings are exploratory.", medians, timings.ToArray(), variant?.Id ?? ReceiverVariant.Baseline.Id);
}

static string SweepReport(Trial[] trials, Trial[] verification, MathComparison[] comparisons, SweepSelection selection, bool parity)
{
    var text = new StringBuilder("# Primitive agency: receiver buckets and steering interpretation\n\n");
    text.AppendLine($"Twelve hand-designed native calibrations ran for {trials[0].Steps.Length} ticks through Computed, native Lookup, and captured Packed backends. This is a bounded manual comparison, not learning. The previous default experiment and its output folder remain separate.");
    text.AppendLine("\n## Fixed world and controller\n");
    text.AppendLine("The 31-cell world, relocation every160 ticks, initial position15/charge60/heat0, physiological buckets, round2 memory/heat/rest rules, temporal trend, and one-charge-unit spatial-query fee are identical. All receiver instruments encounter the same physical light field; only their declared four-region local quantization changes. No fine light coordinate enters the brain.");
    text.AppendLine("\nStrict0 retains negative, exact zero, and positive regions. Gentle4 and broad10 split each sign into near/far regions while preserving a separate exact-zero region. A higher-priority native condition admits the union of near-negative/zero/near-positive only when the entire exposed difference support is contained there: seeking stays and cooling continues its remembered heading. Whole-sign conditions follow. This interprets conservative interval arithmetic; it does not subtract bucket ranks or silently pick a sign.");
    text.AppendLine("\nThe paid comparator remains ordinal. It publishes both near/far negative regions or both near/far positive regions, and exact zero alone. It cannot establish a small nonzero magnitude. Original coarse support and the unresolved initial action witness remain in the native snapshot after a query.");
    text.AppendLine("\n## Discovery: 600-tick window\n");
    if (trials[0].Steps.Length != 600) text.AppendLine($"This invocation used {trials[0].Steps.Length} discovery ticks rather than the default600.");
    AppendTable(text, trials.Where(trial => trial.Backend == "Computed"));
    text.AppendLine("\nMulti-region gradients and unresolved first actions are distinct: several sign-consistent regions can establish a motion, and a fully supported near-group can establish a hold. Neither requires resolving its exact member.");
    text.AppendLine($"\nSelection: **{selection.SelectedVariantId}**. {selection.Rule} Reference cycles: {selection.BaselineCycles}. Qualifying calibrations: {string.Join(", ", selection.QualifyingVariantIds)}.");
    text.AppendLine("\n## 2400-tick validation and no-query ablation\n");
    text.AppendLine(selection.ValidationDescription);
    AppendTable(text, verification);
    text.AppendLine("\nThe selected no-query ablation keeps its receiver partition, interpretation, memory, heat/rest policy, and temporal fallback while setting the native refinement gate to zero. There was no further search or controller/world tuning after selection.");
    text.AppendLine($"\nAll matched trajectories and all3072 input-mask pairs agree across the three adapters: **{parity}**. Each calibration checks all256 pairs, including empty/open inputs. Native archives are decoded to verify memory, all chosen light/output regions, and ordered action rules. The final snapshot retains original arithmetic support and any actual comparator publication.");
    text.AppendLine("\n## Measured arithmetic adapters\n\n| Calibration | Computed ns/read | Native lookup ns/read | Packed ns/read | Computed bytes/read | Lookup bytes/read | Packed bytes/read |\n|---|---:|---:|---:|---:|---:|---:|");
    foreach (var comparison in comparisons)
    {
        var byBackend = comparison.Backends.ToDictionary(item => item.Backend);
        text.AppendLine($"| {comparison.VariantId} | {byBackend["Computed"].NanosecondsPerRead:F0} | {byBackend["Lookup"].NanosecondsPerRead:F0} | {byBackend["Packed"].NanosecondsPerRead:F0} | {byBackend["Computed"].BytesPerRead:F0} | {byBackend["Lookup"].BytesPerRead:F0} | {byBackend["Packed"].BytesPerRead:F0} |");
    }
    text.AppendLine("\nThree warmed rotating passes per calibration use identical native publication sequences. Native inspection/witness allocation is included; construction/capture is separate. These short process-local measurements are exploratory. Lookup revalidates native rows; Packed derives masks from a captured native contract and still publishes native witnesses. Broader output partitions can allocate more support without improving behavior.");
    text.AppendLine("\n`totalOperations` counts publications, native condition checks/projections, memory writes, coarse reads, and queries rather than CPU instructions. Trial loop timers exclude construction, archive readback, and file output. JSON and replay data are display projections; the `.gojo` archives retain the chosen native partitions/rules/memory. Charge/cool cycles remain external outcome accounting, never world-written brain states.");
    return text.ToString();
}

static void AppendTable(StringBuilder text, IEnumerable<Trial> trials)
{
    text.AppendLine("\n| Trial | Backend | Ticks | Cycles | Starvation | Overheat | Mean charge | Moves | Multi-region gradients | First actions unresolved | Queries |\n|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
    foreach (var trial in trials)
    {
        var s = trial.Summary;
        text.AppendLine($"| {trial.Phase}/{trial.VariantId}{(trial.Label.Contains("no query") ? "/no_query" : "")} | {trial.Backend} | {trial.Steps.Length} | {s.CompletedCycles} | {s.StarvationTicks} | {s.OverheatTicks} | {s.MeanCharge:F2} | {s.Movements} | {s.AmbiguityTicks} | {s.InitialActionUnresolvedTicks} | {s.RefinementQueries} |");
    }
}

static string Report(Trial[] trials, MathComparison math)
{
    var text = new StringBuilder("# Primitive agency: charge, leave, cool\n\n");
    text.AppendLine($"These are hand-designed native Gojo controllers, compared over the same deterministic {trials[0].Steps.Length}-tick world. They do not learn or evolve during these trials. The visible behavior tests connected coarse decisions, memory, and selective information requests.");
    text.AppendLine("\n| Controller | Backend | Cycles | Starvation ticks | Overheat ticks | Mean charge | Movements | Ambiguous gradients | Queries | Loop ms |\n|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
    foreach (var trial in trials)
    {
        var s = trial.Summary;
        text.AppendLine($"| {trial.Label} | {trial.Backend} | {s.CompletedCycles} | {s.StarvationTicks} | {s.OverheatTicks} | {s.MeanCharge:F1} | {s.Movements} | {s.AmbiguityTicks} | {s.RefinementQueries} | {s.ElapsedMs:F2} |");
    }
    text.AppendLine("\n## Native controller rounds\n");
    text.AppendLine("The baselines stay or continue right. Round1 seeks while charge is below the high bucket and avoids light while charged or hot; ambiguous gradients use the last motion and the sign-only temporal-light trend. Round2 adds persistent seek/cool hysteresis, a heat guard, resting after cooling, and an explicitly requested spatial comparison when a coarse gradient cannot establish a relevant action.");
    text.AppendLine("\nThe no-query ablation keeps the same round2 memory, heat, rest, and temporal fallback rules while setting its native refinement gate to zero. It separates the benefit of extra information from the benefit of hysteresis.");
    text.AppendLine("\nEvery optional spatial comparison costs one physical charge unit as well as a counted request. The controller sees that expenditure through the following physiological sensor sample; no world outcome writes its memory.");
    text.AppendLine("\nConditions are native sensor-role/partition/permitted-region groups. Ordered native rules publish a response source and affine calibration. A condition fires only when its entire exposed support is inside its admitted regions. Open or mixed support never becomes a guessed sign. Memory updates are derived from native response coordinates and enter the two memory carriers through delta settlement.");
    text.AppendLine("\nThe world alone owns charge, heat, position, charging/light location, and outcome counters. It publishes coarse physiological sensors and sign-only comparison events; it never writes a seek/cool mode, motion memory, or reward into the controller. Controller inputs in the replay correspond to the physical state before that frame's command.");
    text.AppendLine($"\nComputed/native lookup/packed results match: **{math.AllBackendsMatch}**. Active-controller trajectories are compared using deterministic hashes of physical outcomes and native decisions; all256 input-mask pairs are checked separately, including open results from empty admissible sets.");
    text.AppendLine("\n## Small arithmetic comparison\n");
    text.AppendLine("| Adapter | Median ns/read | Allocated bytes/read |\n|---|---:|---:|");
    foreach (var measurement in math.Backends)
        text.AppendLine($"| {measurement.Backend} | {measurement.NanosecondsPerRead:F0} | {measurement.BytesPerRead:F0} |");
    text.AppendLine($"\nNative table compilation: {math.NativeCompileMs:F2}ms. Captured packed plan: {math.PackedCaptureMs:F2}ms. {math.TimingMethod}");
    text.AppendLine("\nThe native table reader revalidates and traverses its rows, so a lookup can cost more than direct coarse arithmetic. The packed adapter validates and captures that native table once, then derives flat masks while still producing native result/provenance witnesses. It is a captured execution projection; changed laws require a new capture. These costs do not predict a fully packed multi-agent simulator's speed.");
    text.AppendLine("\n`totalOperations` counts exposed sensor/memory publications, coarse difference reads, native condition checks, response projections, memory writes, and optional requests. It is an instrumentation count, not CPU instructions. Trial timing excludes construction, archive encoding/readback, and display output; allocated bytes cover the actual loop on its executing thread.");
    text.AppendLine("\nEach final `.gojo` brain archive preserves native rules, calibrated responses, persistent memory, latest sensor bindings, the original ambiguous gradient, the optional query, and decision witnesses. The archive is decoded to check its native memory cells. `results.json` and `index.html` are display projections. Completed cycles are externally defined outcomes, not proof of learned intent or general intelligence.");
    return text.ToString();
}

record MathTiming(string Backend, int Pass, int Iterations, double ElapsedMs, long AllocatedBytes, long Checksum);
record MathBackend(string Backend, double MedianMs, double NanosecondsPerRead, double BytesPerRead);
record MathComparison(bool AllBackendsMatch, bool TrialParity, bool AllInputMasksMatch, int MaskPairsChecked,
    double NativeCompileMs, double PackedCaptureMs, string Description, string TimingMethod, MathBackend[] Backends, MathTiming[] Passes, string VariantId);
record SweepSelection(string SelectedVariantId, string SelectedTrialId, string BaselineTrialId, int BaselineCycles,
    string[] QualifyingVariantIds, string Rule, int ValidationTicks, string ValidationDescription);
