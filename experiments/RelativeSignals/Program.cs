using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
try
{
    var output = "artifacts/relative-signals";
    var scriptOnly = false;
    for (var n = 0; n < args.Length; n++)
        switch (args[n])
        {
            case "--help": Console.WriteLine("RelativeSignals [--output <directory>] [--script-only]"); return 0;
            case "--script-only": scriptOnly = true; break;
            case "--output" when n + 1 < args.Length: output = args[++n]; break;
            default: throw new ArgumentException($"Unknown or incomplete option {args[n]}.");
        }
    output = Path.GetFullPath(output);
    Directory.CreateDirectory(output);
    var scenarios = new List<Scenario>();
    foreach (var kind in scriptOnly ? new[] { "scripted" } : new[] { "scripted", "pursuit" })
        foreach (var variant in kind == "scripted" ? new[] { "previous", "fast", "fast_slow" } : new[] { "alwaysright", "previous", "fast", "fast_slow" })
            foreach (var transform in new[] { "day", "dusk", "nonlinear" })
                scenarios.Add(Lab.Run(kind, variant, transform, output));
    var invarianceChecks = scenarios.GroupBy(scenario => $"{scenario.Kind}/{scenario.Variant}").SelectMany(group =>
    {
        var reference = group.Single(scenario => scenario.Transform == "day");
        return group.Where(scenario => scenario.Transform != "day").Select(scenario => new InvarianceCheck(
            $"{group.Key}/{scenario.Transform}", scenario.Kind, scenario.Variant, "day", scenario.Transform,
            reference.Summary.Signature == scenario.Summary.Signature, scenario.Steps.Length,
            reference.Summary.Signature, scenario.Summary.Signature,
            "Full native score/status/reference/weight/query and controller intent/action/cooldown signatures match; private fixture coordinates are excluded."));
    }).ToArray();
    var allInvariantsMatch = invarianceChecks.All(check => check.Matched);
    var controller = new NativeController(new Carrier(), "fast_slow");
    var metadata = new
    {
        handDesigned = true, noMetric = true, cells = Lab.Cells, scriptedTicks = Lab.ScriptTicks, pursuitTicks = Lab.PursuitTicks,
        scoreRange = new[] { -16, 16 }, fastThreshold = controller.ReadThreshold(controller.FastRole), slowThreshold = controller.ReadThreshold(controller.SlowRole),
        scoreMeaning = "Sixteen times the attention-weighted mean of comparison signs, truncated toward zero. This is ordinal evidence against retained events, not brightness or a metric mean.",
        zeroMeaning = "Zero may mean all indistinguishable comparisons, balanced signs, a rounded small vote, or no readable evidence. Native status distinguishes these cases.",
        sourceBoundary = "Only presence and requested candidate-versus-anchor order witnesses reach the observer. Private brightness and position never enter the controller. Heading and refractory memory are native Gojo carriers.",
        transforms = new[] { new { id = "day", formula = "8x + 1000" }, new { id = "dusk", formula = "x + 20" }, new { id = "nonlinear", formula = "x² + 20 (x≥0)" } },
        fastRetention = "3/4", slowRetention = "31/32", initialWeight = 64, maxReferences = 64,
        previousDescription = "The previous-only observer uses one reference per bank. Its two reads agree; policy consumes only fast evidence.",
        fastOnlyDescription = "Fast-only policy consumes fast evidence but the same two-bank observer still maintains slow memory; all actual comparison costs are reported.",
        refractoryDescription = "A reversal resets a native carrier to 4. Its connected affine 3/4 leak reads 4→3→2→1→0; reversal rules require zero. The update retains projection residue and uses Set delta settlement.",
        defaultScenarioId = "scripted_fast_slow_day", actionLabels = new[] { "left", "stay", "right" },
        scriptSegments = new[] { "0–15 plateau", "16–47 slow rise with repeated values", "48–63 plateau", "64–79 abrupt rise", "80–95 slow fall", "96–111 abrupt fall then rise", "112–127 single-tick pulses", "128–143 missing/reused acquisitions", "144–159 slow fall", "160–223 sustained new plateau" },
        worldRules = "31 cells; hidden peak follows 23, 6, 25, 10 every 96 ticks; underlying light max(0,100−4|x−peak|). One opaque center sample per tick. A comparison-driven controller moves at most one cell; all trials start at 15 heading right. No hunger, heat, learning or outcome-written brain state."
    };
    var result = new { generatedAt = DateTimeOffset.UtcNow, metadata, scenarios, invarianceChecks, allInvariantsMatch };
    var json = JsonSerializer.Serialize(result, options);
    File.WriteAllText(Path.Combine(output, "results.json"), json, new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(output, "report.md"), Lab.Report(scenarios, invarianceChecks), new UTF8Encoding(false));
    var template = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "lab.html"));
    if (!template.Contains("__RELATIVE_DATA__", StringComparison.Ordinal)) throw new InvalidDataException("Replay template has no data placeholder.");
    File.WriteAllText(Path.Combine(output, "index.html"), template.Replace("__RELATIVE_DATA__", json, StringComparison.Ordinal), new UTF8Encoding(false));
    Console.WriteLine(JsonSerializer.Serialize(new { output, allInvariantsMatch, scenarios = scenarios.Select(scenario => new { scenario.Id, scenario.Summary }), invarianceChecks }, options));
    return allInvariantsMatch ? 0 : 1;
}
catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static class Lab
{
    public const int Cells = 31, ScriptTicks = 224, PursuitTicks = 384;
    private static readonly int[] Peaks = [23, 6, 25, 10];
    public static Scenario Run(string kind, string variant, string transform, string output)
    {
        var source = new Carrier(); var presenceAxis = new Carrier(); var orderAxis = new Carrier();
        var state = RelativeSignalSurface.Create(new Carrier(), source, presenceAxis, orderAxis, maxReferences: variant == "previous" ? 1 : 64);
        if (!RelativeSignalSurface.TryReadState(state, out _, out var policy, out _, out _, out _) ||
            !RelativeSignalSurface.TryReadPolicy(policy, out _, out _, out _, out var scoreAxis, out _, out _))
            throw new InvalidDataException("The native relative-signal policy is unreadable.");
        var controller = new NativeController(scoreAxis, variant);
        var privateValues = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
        var eventTicks = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
        var acquisitions = new Dictionary<int, Relation>();
        var steps = new List<SignalStep>();
        var x = 15; var queryCount = 0; var missing = 0; var repeated = 0;
        var ticks = kind == "scripted" ? ScriptTicks : PursuitTicks;
        var timer = Stopwatch.StartNew();
        var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var tick = 0; tick < ticks; tick++)
        {
            var peak = Peaks[tick / 96 % Peaks.Length];
            var fixture = kind == "scripted" ? Script(tick) : new Acquisition(Math.Max(0, 100 - 4 * Math.Abs(x - peak)), true, null);
            var reused = fixture.ReuseTick.HasValue;
            Relation sample;
            if (reused) { sample = acquisitions[fixture.ReuseTick!.Value]; repeated++; }
            else
            {
                sample = SamplingSurface.Sample(new Carrier(), source, presenceAxis, fixture.Exists);
                privateValues.Add(sample, Transform(fixture.Value, transform));
                eventTicks.Add(sample, tick);
            }
            acquisitions.Add(tick, sample);
            if (!fixture.Exists) missing++;
            var queriesBefore = queryCount;
            state = RelativeSignalSurface.Observe(new Carrier(), state, sample, (candidate, anchor) =>
            {
                queryCount++;
                return SamplingSurface.Comparison(new Carrier(), candidate, anchor, orderAxis, privateValues[candidate].CompareTo(privateValues[anchor]));
            });
            if (!RelativeSignalSurface.TryReadState(state, out _, out _, out var fastBank, out var slowBank, out var latest) ||
                !RelativeSignalSurface.TryReadLatest(latest, out _, out var fastRead, out var slowRead, out _, out _))
                throw new InvalidDataException("The relative-signal state lost its exposed reads.");
            var fast = Read(fastRead, orderAxis); var slow = Read(slowRead, orderAxis);
            var fastMemory = Memory(fastBank, tick, eventTicks); var slowMemory = Memory(slowBank, tick, eventTicks);
            var decision = controller.Decide(fastRead, slowRead);
            steps.Add(new(tick, fixture.Exists ? privateValues[sample] : null, fast.Score, slow.Score,
                fastMemory.Length, slowMemory.Length, fastMemory.Sum(reference => reference.Weight), slowMemory.Sum(reference => reference.Weight),
                decision.Action < 0 ? "left" : decision.Action > 0 ? "right" : "stay", decision.Reason, queryCount - queriesBefore,
                fast.State, slow.State, fast.Evidence, slow.Evidence, fastMemory, slowMemory, decision.Cooldown, decision.Intent,
                reused, fixture.Exists, kind == "pursuit" ? x : null, kind == "pursuit" ? peak : null,
                kind == "pursuit" ? Transform(100, transform) : null, decision.Action, decision.Heading,
                fast.ComparedWeight, slow.ComparedWeight, fast.ComparedReferences, slow.ComparedReferences));
            if (kind == "pursuit") x = Math.Clamp(x + decision.Action, 0, Cells - 1);
        }
        timer.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocation;
        var signature = Signature(steps);
        var id = $"{kind}_{variant}_{transform}";
        var graph = Group(state, controller.Root, controller.Latest);
        var bytes = GojoNumericFormat.Encode(graph);
        var archiveFile = id + ".gojo";
        File.WriteAllBytes(Path.Combine(output, archiveFile), bytes);
        ReadBack(bytes, state, controller);
        var title = kind == "scripted" ? "Plateaus, slopes, shifts and pulses" : "Moving hill pursuit";
        var summary = new Summary(ticks, queryCount, missing, repeated,
            steps.Count(step => step.FastScore is null), steps.Count(step => step.SlowScore is null),
            steps.Count(step => step.FastScore == 0 && step.FastState != "all_same"), steps.Count(step => step.SlowScore == 0 && step.SlowState != "all_same"),
            steps.Count(step => step.Intent == "hold"), steps.Count(step => step.Intent == "reverse"),
            steps.Zip(steps.Skip(1)).Count(pair => pair.First.Heading != pair.Second.Heading),
            kind == "pursuit" ? steps.Average(step => (double)Math.Abs(step.X!.Value - step.PeakX!.Value)) : null,
            kind == "pursuit" ? steps.Count(step => Math.Abs(step.X!.Value - step.PeakX!.Value) <= 2) : null,
            kind == "pursuit" ? x : null, bytes.Length, archiveFile, signature, timer.Elapsed.TotalMilliseconds, allocated,
            steps.Count(step => step.FastScore < 0 && step.SlowScore > 0 || step.FastScore > 0 && step.SlowScore < 0),
            kind == "scripted" ? steps.Where(step => step.Tick >= 160 && step.FastState == "all_same" && step.SlowState == "all_same").Select(step => (int?)step.Tick).FirstOrDefault() : null);
        return new(id, title, kind == "scripted" ? "Same opaque acquisitions under three strictly increasing private transforms. Missing samples and reused acquisition identities are explicit."
            : "An exploratory hand-designed controller follows one center sample along a 31-cell track. The hidden peak relocates; no absolute light or position is available internally.", kind, variant, transform, summary, steps.ToArray());
    }

    private static Acquisition Script(int tick) => tick switch
    {
        < 16 => new(40, true, null),
        < 48 => new(40 + (tick - 16) / 2, true, null),
        < 64 => new(56, true, null),
        < 80 => new(90, true, null),
        < 96 => new(90 - (tick - 80) * 3, true, null),
        < 112 => new(20 + tick - 96, true, null),
        < 128 => new(tick is 114 or 125 ? 85 : tick == 120 ? 5 : 35, true, null),
        < 144 => new(35, tick is not (132 or 136), tick == 135 ? 134 : tick == 138 ? 130 : null),
        < 160 => new(35 - (tick - 144), true, null),
        _ => new(20, true, null)
    };
    private static int Transform(int value, string transform) => transform switch
    {
        "day" => checked(8 * value + 1000), "dusk" => checked(value + 20), "nonlinear" => checked(value * value + 20),
        _ => throw new ArgumentException("Unknown private transform.")
    };
    private static BankRead Read(Relation read, Carrier orderAxis)
    {
        if (!RelativeSignalSurface.TryReadBankRead(read, out var score, out var evidence, out var weight, out var references))
            return new(null, null, 0, 0, "open");
        var allSame = RelativeSignalSurface.TryReadBankComparisons(read, out var comparisons) && comparisons.Length > 0 &&
            comparisons.All(comparison => ReadWitness.TryRead(comparison, out _, out _, out var result, out _, out _) &&
                result is CarrierLocation order && order.Carrier.SameIdentityAs(orderAxis) && order.Ticks == 0);
        return new((int)score.Ticks, evidence, weight, references,
            allSame ? "all_same" : score.Ticks == 0 ? "balanced_or_rounded" : score.Ticks > 0 ? "positive_evidence" : "negative_evidence");
    }
    private static ReferenceMemory[] Memory(Relation bank, int tick, Dictionary<Relation, int> eventTicks)
    {
        if (!RelativeSignalSurface.TryReadBank(bank, out _, out var references)) throw new InvalidDataException("Unreadable native bank.");
        return references.Select(reference =>
        {
            if (!RelativeSignalSurface.TryReadReference(reference, out var sample, out var weight) || !eventTicks.TryGetValue(sample, out var acquisitionTick))
                throw new InvalidDataException("Native reference lost its acquisition identity.");
            return new ReferenceMemory(tick - acquisitionTick, (int)weight.Value);
        }).ToArray();
    }
    private static string Signature(IEnumerable<SignalStep> steps) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
        steps.Select(step => new { step.Tick, step.FastScore, step.SlowScore, step.FastReferences, step.SlowReferences, step.FastWeight, step.SlowWeight,
            step.Action, step.Reason, step.Queries, step.FastState, step.SlowState, step.FastEvidence, step.SlowEvidence, step.FastMemory, step.SlowMemory,
            step.Cooldown, step.Intent, step.Repeated, step.Exists, step.X, step.PeakX, step.ActionCoordinate, step.Heading,
            step.FastComparedWeight, step.SlowComparedWeight, step.FastComparedReferences, step.SlowComparedReferences }))))).ToLowerInvariant();
    private static void ReadBack(byte[] bytes, Relation liveState, NativeController controller)
    {
        var roots = GojoNumericFormat.Decode(bytes);
        if (roots.Length != 1 || roots[0] is not Relation graph || !graph.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
            fields[0] is not Relation restoredState || !RelativeSignalSurface.TryReadState(restoredState, out _, out _, out var fastBank, out var slowBank, out var latest) ||
            !RelativeSignalSurface.TryReadLatest(latest, out var sample, out var fastRead, out var slowRead, out _, out _) ||
            !SamplingSurface.TryReadSample(sample, out _, out _) || fields[1] is not Relation restoredPolicy || !restoredPolicy.TryReadOrderedGroup(out var policyFields) ||
            policyFields[2] is not Relation memory || !memory.TryReadOrderedGroup(out var cells) || cells[0] is not Carrier heading || cells[1] is not Carrier cooldown ||
            heading.Value != controller.Heading.Value || cooldown.Value != controller.Cooldown.Value)
            throw new InvalidDataException("Archive did not retain native observer and controller memory.");
        RelativeSignalSurface.TryReadState(liveState, out _, out _, out var liveFast, out var liveSlow, out var liveLatest);
        RelativeSignalSurface.TryReadLatest(liveLatest, out _, out var liveFastRead, out var liveSlowRead, out _, out _);
        foreach (var pair in new[] { (fastBank, liveFast), (slowBank, liveSlow) })
        {
            RelativeSignalSurface.TryReadBank(pair.Item1, out _, out var restoredRefs);
            RelativeSignalSurface.TryReadBank(pair.Item2, out _, out var liveRefs);
            if (restoredRefs.Length != liveRefs.Length) throw new InvalidDataException("Archive reference count changed.");
            for (var n = 0; n < restoredRefs.Length; n++)
                if (!RelativeSignalSurface.TryReadReference(restoredRefs[n], out var acquisition, out var restoredWeight) ||
                    !SamplingSurface.TryReadSample(acquisition, out _, out _) ||
                    !RelativeSignalSurface.TryReadReference(liveRefs[n], out _, out var liveWeight) || restoredWeight.Value != liveWeight.Value)
                    throw new InvalidDataException("Archive did not retain opaque references and their attention.");
        }
        foreach (var pair in new[] { (fastRead, liveFastRead), (slowRead, liveSlowRead) })
        {
            var restored = RelativeSignalSurface.TryReadBankRead(pair.Item1, out var r, out var re, out var rw, out var rc);
            var live = RelativeSignalSurface.TryReadBankRead(pair.Item2, out var l, out var le, out var lw, out var lc);
            if (restored != live || restored && (r.Ticks != l.Ticks || re != le || rw != lw || rc != lc))
                throw new InvalidDataException("Archive changed a native ordinal read.");
        }
        if (NativeController.RuleSignature(restoredPolicy) != NativeController.RuleSignature(controller.Root))
            throw new InvalidDataException("Archive changed native controller conditions or calibrations.");
    }
    public static string Report(IEnumerable<Scenario> scenarios, InvarianceCheck[] checks)
    {
        var text = new StringBuilder("# Relative signals: comparison-only fast and slow memory\n\n");
        text.AppendLine("This is a small hand-designed controller experiment, not learning. The observer receives opaque acquisitions and requested candidate-versus-reference order witnesses. No absolute brightness coordinate, fixed light threshold, metric mean, or outcome-written brain state is supplied.");
        text.AppendLine("\nEach bank retains acquisition→attention-carrier references. Fast attention decays by 3/4; slow by 31/32, with initial weight 64 and maximum 64 references. Its exposed score is truncate(16×weighted signed comparisons/compared weight), in −16…16. This score is relative evidence, not brightness. A zero score is labeled all-same only when every native comparison witness establishes equality; mixed/rounded zero remains distinct and an absent/warmup/duplicate-failed read remains open.");
        text.AppendLine("\n## Scripted observations\n");
        text.AppendLine("The 224-acquisition trace includes plateaus, slow repeated-value rises/falls, abrupt shifts, one-tick pulses, two missing events, immediate reuse of an event, reuse of an older retained event, and a 64-tick final plateau at a new lower level. Distinct acquisitions with equal values remain distinct references. Exact reuse is not counted as new independent information.");
        Table(text, scenarios.Where(scenario => scenario.Kind == "scripted" && scenario.Transform == "day"));
        foreach (var scenario in scenarios.Where(scenario => scenario.Kind == "scripted" && scenario.Transform == "day"))
            text.AppendLine($"\n{scenario.Variant}: opposite-signed timescale evidence on {scenario.Summary.OppositeTimescaleTicks} ticks; final plateau first has both native reads all-same at tick {scenario.Summary.FinalPlateauAllSameTick?.ToString() ?? "unresolved"}.");
        var example = scenarios.First(scenario => scenario.Kind == "scripted" && scenario.Variant == "fast_slow" && scenario.Transform == "day");
        var recovering = example.Steps.FirstOrDefault(step => step.FastScore > 0 && step.SlowScore < 0);
        var briefDip = example.Steps.FirstOrDefault(step => step.FastScore < 0 && step.SlowScore > 0);
        if (recovering is not null && briefDip is not null)
            text.AppendLine($"\nFor example, at tick {recovering.Tick} the fast/slow scores are {recovering.FastScore}/{recovering.SlowScore}: recent recovery against a longer decline. At tick {briefDip.Tick} they are {briefDip.FastScore}/{briefDip.SlowScore}: a short dip while the longer evidence is positive. These are relative comparisons, with no amplitude calibration.");
        text.AppendLine("\n## Exploratory moving hill\n");
        Table(text, scenarios.Where(scenario => scenario.Kind == "pursuit" && scenario.Transform == "day"));
        text.AppendLine("\nThe optional 31-cell world exposes one center acquisition per tick, starts at 15 heading right, and relocates its hidden hill every 96 ticks over 384 ticks. Mean distance and near-peak ticks are external observer metrics. They never update native memory. This simple policy may trail or oscillate; performance here is not evidence of general navigation, intelligence, or a learned behavior.");
        text.AppendLine("\n## Native decisions and memory\n");
        text.AppendLine("Rules are native ordered groups of score-role/closed-evidence-frame conditions, a response coordinate, and an affine calibration. Previous/fast policies reverse on fast evidence≤−4 and otherwise continue. Fast+slow reverses on a supported decline in both banks, holds on a short decline against a longer baseline, continues on fast evidence≥4, and uses strong slow evidence during weak fast evidence. The ±4 thresholds are declared comparison-vote thresholds, not absolute light thresholds.");
        text.AppendLine("\nA selected intent feeds native motion rules; continue/reverse uses the remembered heading through retained forward/reversed affine calibrations. Heading is updated through native action/memory rules and Set delta settlement. A native refractory carrier resets to 4 after reversal and decays through a connected 3/4 projection; reversal conditions require zero, preventing an unbounded flip every tick on one falling episode. Missing/open evidence follows an explicit native continue rule and remains inspectable.");
        text.AppendLine("\nPrevious-only uses maximum one reference per bank. Fast-only ignores the slow score in its policy but still maintains the same two-bank observer; its actual shared comparison cost is reported. The observer reuses same-pair queries between banks. Reported requests are order-service calls, not CPU instructions or paid model calls.");
        text.AppendLine($"\n## Transform invariance\n\nAll {checks.Length} full-signature checks passed: **{checks.All(check => check.Matched)}**. Day 8x+1000, dusk x+20, and nonlinear x²+20 are strictly increasing on the chosen nonnegative fixture domain. Every score/status/reference/weight/request/intent/action/cooldown and closed-loop position signature matches within each controller. Private plotting values are intentionally excluded. This verifies these fixtures, not all possible comparator contracts or sensor noise.");
        text.AppendLine("\nEach final `.gojo` archive is decoded to verify opaque samples, reference weights, ordinal reads, native decision conditions/calibrations, and heading/refractory memory. Native graphs remain authoritative; JSON/replay are read-only display projections. Only final graphs are retained, not an exact native archive of every earlier operational state. The replay retains measured step projections. Source values remain external display references.");
        return text.ToString();
    }
    private static void Table(StringBuilder text, IEnumerable<Scenario> scenarios)
    {
        text.AppendLine("\n| Controller | Ticks | Queries | Open fast / slow | Mixed/rounded zero fast / slow | Holds | Reversals | Mean distance | Near peak ticks |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|");
        foreach (var scenario in scenarios)
        {
            var s = scenario.Summary;
            text.AppendLine($"| {scenario.Variant} | {s.Ticks} | {s.ComparisonQueries} | {s.OpenFastReads}/{s.OpenSlowReads} | {s.ZeroButMixedFastReads}/{s.ZeroButMixedSlowReads} | {s.Holds} | {s.Reversals} | {(s.MeanDistance?.ToString("F2") ?? "—")} | {s.NearPeakTicks?.ToString() ?? "—"} |");
        }
    }
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private record Acquisition(int Value, bool Exists, int? ReuseTick);
    private record BankRead(int? Score, int? Evidence, int ComparedWeight, int ComparedReferences, string State);
}

sealed class NativeController
{
    public Carrier FastRole { get; } = new();
    public Carrier SlowRole { get; } = new();
    private readonly Carrier cooldownRole = new(), intentRole = new(), actionRole = new();
    private readonly Carrier scoreAxis, intentAxis = new(), actionAxis = new();
    public Carrier Heading { get; } = new();
    public Carrier Cooldown { get; } = new();
    public Relation Root { get; }
    public Relation Latest { get; private set; }
    private readonly Relation policyRules, motionRules, headingRules, cooldownRules, leak;
    private readonly Dictionary<Relation, string> labels = new(ReferenceEqualityComparer.Instance);
    public NativeController(Carrier evidenceAxis, string variant)
    {
        scoreAxis = evidenceAxis;
        Heading.Set(2);
        Relation Condition(Carrier role, Carrier axis, int low, int high) => Group(role, Relation.Segment(axis, low, high));
        Relation Fast(int low, int high) => Condition(FastRole, scoreAxis, low, high);
        Relation Slow(int low, int high) => Condition(SlowRole, scoreAxis, low, high);
        Relation Ready() => Condition(cooldownRole, Cooldown, 0, 0);
        Relation Intent(int value) => Condition(intentRole, intentAxis, value, value);
        Relation Action(int value) => Condition(actionRole, actionAxis, value, value);
        Relation Constant(string reason, Carrier axis, int value, params Relation[] conditions) => Rule(reason, conditions, new CarrierLocation(axis, value), Identity(axis));
        var rules = new List<IElement>();
        if (variant == "fast_slow")
        {
            rules.Add(Constant("Fast and slow decline: reverse when refractory memory is ready", intentAxis, 2, Fast(-16, -4), Slow(-16, -4), Ready()));
            rules.Add(Constant("Short decline against longer baseline: hold", intentAxis, 0, Fast(-16, -4), Slow(-3, 16)));
            rules.Add(Constant("Fast rise: continue", intentAxis, 1, Fast(4, 16)));
            rules.Add(Constant("Weak fast evidence and longer decline: reverse when ready", intentAxis, 2, Fast(-3, 3), Slow(-16, -4), Ready()));
            rules.Add(Constant("Weak fast evidence above longer baseline: hold", intentAxis, 0, Fast(-3, 3), Slow(4, 16)));
        }
        else if (variant != "alwaysright")
            rules.Add(Constant("Fast decline: reverse when refractory memory is ready", intentAxis, 2, Fast(-16, -4), Ready()));
        rules.Add(Constant(variant == "alwaysright" ? "Neutral: continue right" : "Continue with weak, open, or refractory evidence", intentAxis, 1));
        policyRules = Group(rules.ToArray());
        motionRules = Group(Constant("Hold position", actionAxis, 0, Intent(0)),
            Rule("Continue remembered heading", [Intent(1)], Heading.ValueSlot, new Relation(Relation.Segment(Heading, 0, 2), Relation.Segment(actionAxis, -1, 1))),
            Rule("Reverse remembered heading", [Intent(2)], Heading.ValueSlot, new Relation(Relation.Segment(Heading, 0, 2), Relation.Segment(actionAxis, 1, -1))));
        headingRules = Group(Constant("Remember left", Heading, 0, Action(-1)), Constant("Remember right", Heading, 2, Action(1)),
            Rule("Retain heading while holding", [], Heading.ValueSlot, Identity(Heading)));
        cooldownRules = Group(Constant("Reversal starts refractory memory", Cooldown, 4, Intent(2)), Rule("Retain decayed refractory memory", [], Cooldown.ValueSlot, Identity(Cooldown)));
        leak = new Relation(Relation.Segment(Cooldown, 0, 4), Relation.Segment(Cooldown, 0, 3));
        Root = Group(Group(Relation.Segment(scoreAxis, -16, 16), Relation.Segment(intentAxis, 0, 2), Relation.Segment(actionAxis, -1, 1)),
            Group(FastRole, SlowRole, cooldownRole, intentRole, actionRole), Group(Heading, Cooldown), policyRules, motionRules, headingRules, cooldownRules, leak);
        Latest = Root;
    }
    public ControllerDecision Decide(Relation fastRead, Relation slowRead)
    {
        if (!leak.TryProject(Cooldown.ValueSlot, Cooldown, out var decay) || decay.Left is not CarrierLocation decayed || decayed.Ticks is < 0 or > 4)
            throw new InvalidDataException("Native refractory leak failed.");
        Cooldown.Set((ulong)decayed.Ticks);
        var inputs = new Dictionary<Carrier, Relation>(ReferenceEqualityComparer.Instance)
        {
            [FastRole] = SignalPublication(fastRead), [SlowRole] = SignalPublication(slowRead),
            [cooldownRole] = Publication(new CarrierLocation(Cooldown, (long)Cooldown.Value), leak, decay)
        };
        var intent = Select(policyRules, inputs);
        inputs[intentRole] = intent.Witness;
        var action = Select(motionRules, inputs);
        inputs[actionRole] = action.Witness;
        var heading = Select(headingRules, inputs); Apply(Heading, heading);
        var refractory = Select(cooldownRules, inputs); Apply(Cooldown, refractory);
        Latest = Group(Group(inputs.Select(input => (IElement)new Relation(input.Key, input.Value)).ToArray()),
            decay, intent.Witness, action.Witness, heading.Witness, refractory.Witness);
        return new((int)action.Value.Ticks, intent.Value.Ticks == 0 ? "hold" : intent.Value.Ticks == 2 ? "reverse" : "continue", intent.Reason,
            (int)Cooldown.Value, (int)Heading.Value);
    }
    private Relation SignalPublication(Relation read)
    {
        IElement result = RelativeSignalSurface.TryReadBankRead(read, out var score, out _, out _, out _) ? score : new Placeholder();
        ReadWitness.TryRead(read, out _, out _, out _, out var residue, out _);
        return ReadWitness.Create(new Carrier(), read, Relation.Segment(scoreAxis, -16, 16), result, residue, read);
    }
    private Selection Select(Relation rules, Dictionary<Carrier, Relation> inputs)
    {
        rules.TryReadOrderedGroup(out var entries);
        foreach (var entry in entries)
        {
            var evidence = new List<IElement>();
            if (entry is not Relation rule || !rule.TryReadOrderedGroup(out var fields) || fields.Length != 3 ||
                fields[0] is not Relation conditions || !conditions.TryReadOrderedGroup(out var checks) ||
                fields[1] is not CarrierLocation source || fields[2] is not Relation calibration) continue;
            var matched = true;
            foreach (var element in checks)
            {
                if (element is not Relation condition || !condition.TryReadOrderedGroup(out var parts) || parts.Length != 2 ||
                    parts[0] is not Carrier role || parts[1] is not Relation frame || !inputs.TryGetValue(role, out var publication) ||
                    !ReadWitness.TryRead(publication, out _, out _, out var value, out _, out _) || value is not CarrierLocation point ||
                    !Bounds(frame, out var axis, out var low, out var high) || !point.Carrier.SameIdentityAs(axis) || point.Ticks < low || point.Ticks > high)
                { matched = false; break; }
                evidence.Add(new Relation(condition, publication));
            }
            if (!matched || calibration.Right is not Relation target || !Bounds(target, out var outputAxis, out _, out _) ||
                !calibration.TryProject(source, outputAxis, out var projection) || projection.Left is not CarrierLocation result) continue;
            // Capture the admitted operand coordinate; the reusable rule still
            // exposes a live memory slot for the next operational read.
            var witness = ReadWitness.Create(new Carrier(), rules, rule, result, projection.Right,
                Group(evidence.Append(new CarrierLocation(source.Carrier, source.Ticks)).Append(projection).ToArray()));
            return new(result, witness, labels[rule]);
        }
        throw new InvalidDataException("The constructed controller has no native default response.");
    }
    public int ReadThreshold(Carrier role)
    {
        policyRules.TryReadOrderedGroup(out var entries);
        foreach (var entry in entries.Cast<Relation>())
        {
            entry.TryReadOrderedGroup(out var fields); ((Relation)fields[0]).TryReadOrderedGroup(out var conditions);
            foreach (var condition in conditions.Cast<Relation>())
            {
                condition.TryReadOrderedGroup(out var parts);
                if (ReferenceEquals(parts[0], role) && Bounds((Relation)parts[1], out _, out var low, out var high) && high < 0) return (int)-high;
            }
        }
        return 0;
    }
    public static string RuleSignature(Relation root)
    {
        root.TryReadOrderedGroup(out var fields); ((Relation)fields[0]).TryReadOrderedGroup(out var frames);
        ((Relation)fields[1]).TryReadOrderedGroup(out var roles); ((Relation)fields[2]).TryReadOrderedGroup(out var cells);
        var axes = frames.Cast<Relation>().Select(frame => { Bounds(frame, out var axis, out _, out _); return axis; }).Concat(cells.Cast<Carrier>()).ToArray();
        static CarrierLocation Point(IElement element) => element is CarrierLocation point ? point : element is Relation { Left: CarrierLocation coordinate } ? coordinate : throw new InvalidDataException("Missing native coordinate.");
        string Coordinate(CarrierLocation point) => $"{Array.FindIndex(axes, axis => axis.SameIdentityAs(point.Carrier))}:{point.Ticks}";
        string Frame(Relation frame) => $"{Coordinate(Point(frame.Left))}..{Coordinate(Point(frame.Right))}";
        var text = new StringBuilder();
        for (var groupIndex = 3; groupIndex <= 6; groupIndex++)
        {
            ((Relation)fields[groupIndex]).TryReadOrderedGroup(out var entries);
            foreach (var entry in entries.Cast<Relation>())
            {
                entry.TryReadOrderedGroup(out var rule); ((Relation)rule[0]).TryReadOrderedGroup(out var conditions);
                foreach (var condition in conditions.Cast<Relation>())
                {
                    condition.TryReadOrderedGroup(out var parts);
                    var roleIndex = Array.FindIndex(roles, role => ReferenceEquals(role, parts[0]));
                    if (roleIndex < 0) throw new InvalidDataException("Archive lost native role alias.");
                    text.Append($"[{roleIndex}/{Frame((Relation)parts[1])}]");
                }
                var source = Point(rule[1]); var calibration = (Relation)rule[2];
                var liveCell = Array.FindIndex(cells, cell => ((Carrier)cell).ValueSlot.SameTickRefAs(source));
                text.AppendLine($"=>{Coordinate(source)};live{liveCell};{Frame((Relation)calibration.Left)}->{Frame((Relation)calibration.Right)}");
            }
        }
        var leak = (Relation)fields[7]; text.Append($"leak:{Frame((Relation)leak.Left)}->{Frame((Relation)leak.Right)}");
        return text.ToString();
    }
    private Relation Rule(string reason, Relation[] conditions, CarrierLocation source, Relation calibration)
    { var rule = Group(Group(conditions), source, calibration); labels.Add(rule, reason); return rule; }
    private static bool Bounds(Relation frame, out Carrier axis, out long low, out long high)
    {
        if (frame.Left is Relation { Left: CarrierLocation a } && frame.Right is Relation { Left: CarrierLocation b } && a.Carrier.SameIdentityAs(b.Carrier))
        { axis = a.Carrier; low = a.Ticks; high = b.Ticks; return true; }
        axis = null!; low = high = 0; return false;
    }
    private static void Apply(Carrier memory, Selection selection)
    {
        if (!selection.Value.Carrier.SameIdentityAs(memory) || selection.Value.Ticks < 0) throw new InvalidDataException("Native response does not address this memory.");
        memory.Set((ulong)selection.Value.Ticks);
    }
    private static Relation Publication(CarrierLocation result, IElement surface, IElement evidence) => ReadWitness.Create(new Carrier(), result, surface, result, new Placeholder(), evidence);
    private static Relation Identity(Carrier axis) => new(Relation.Segment(axis, 0, 1), Relation.Segment(axis, 0, 1));
    private static Relation Group(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private record Selection(CarrierLocation Value, Relation Witness, string Reason);
}

record ReferenceMemory(int Age, int Weight);
record SignalStep(int Tick, int? PrivateValue, int? FastScore, int? SlowScore, int FastReferences, int SlowReferences, int FastWeight, int SlowWeight,
    string Action, string Reason, int Queries, string FastState, string SlowState, int? FastEvidence, int? SlowEvidence,
    ReferenceMemory[] FastMemory, ReferenceMemory[] SlowMemory, int Cooldown, string Intent, bool Repeated, bool Exists,
    int? X, int? PeakX, int? ReferenceValue, int ActionCoordinate, int Heading, int FastComparedWeight, int SlowComparedWeight, int FastComparedReferences, int SlowComparedReferences);
record Summary(int Ticks, int ComparisonQueries, int MissingTicks, int RepeatedAcquisitions, int OpenFastReads, int OpenSlowReads,
    int ZeroButMixedFastReads, int ZeroButMixedSlowReads, int Holds, int Reversals, int HeadingChanges, double? MeanDistance,
    int? NearPeakTicks, int? FinalX, long ArchiveBytes, string ArchiveFile, string Signature, double ElapsedMs, long AllocatedBytes,
    int OppositeTimescaleTicks, int? FinalPlateauAllSameTick);
record Scenario(string Id, string Title, string Description, string Kind, string Variant, string Transform, Summary Summary, SignalStep[] Steps);
record InvarianceCheck(string Id, string Kind, string Variant, string ReferenceTransform, string ComparedTransform, bool Matched,
    int ComparedTicks, string SignatureReference, string SignatureCompared, string Description);
record ControllerDecision(int Action, string Intent, string Reason, int Cooldown, int Heading);
