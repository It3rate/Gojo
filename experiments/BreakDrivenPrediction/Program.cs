using System.Globalization;
using System.Text;
using Gojo.Core;
using Gojo.Structures;
using C = BreakDrivenPrediction.Continuation;

if (args.FirstOrDefault() == "--timing")
{
    BreakDrivenPrediction.TimingTrials.Run(args.Skip(1).FirstOrDefault()
        ?? "experiments/BreakDrivenPrediction/results/timing");
    return;
}

if (args.FirstOrDefault() == "--events")
{
    BreakDrivenPrediction.EventTrials.Run(args.Skip(1).FirstOrDefault()
        ?? "experiments/BreakDrivenPrediction/results/events");
    return;
}

// Environment generation, scheduling, measurement counters and file output are
// host apparatus. No generator label, future value or cadence enters the graph.
var output = Path.GetFullPath(args.FirstOrDefault() ?? "experiments/BreakDrivenPrediction/results");
Directory.CreateDirectory(output);
var csv = new StringBuilder("trial,arrival,input,actual_delta,export_delta,baseline_delta,shadow_delta,direction_correct,no_timing_correct,scale_error,timing_connections,pressure\n");
var report = new StringBuilder("# First break-driven continuation trials\n\nAll forecasts precede their outcomes. All arrivals are scored, including startup.\n\n");
var graphNotes = new List<string>();
report.AppendLine("| Trial | Correct direction: export / no timing / last change | Turns anticipated | False turns | Mean magnitude error | Timing links added / removed | Candidate inspections | Final carriers / relations | Final pressure |");
report.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
foreach (var (name, increments, inspectionAt) in Trials())
{
    var root = C.Create(1000);
    // Same learner, but the declared floor exceeds support capacity.
    var control = C.Create(1000, requiredSupport: 5);
    long level = 1000, direction = 0, controlDirection = 0, lastDirection = 0, scale = 0;
    int turns = 0, anticipated = 0, falseTurns = 0, added = 0, removed = 0, inspected = 0;
    CarrierLocation? previous = null;
    var sizes = new List<(int At, int Carriers, int Relations)>();
    for (var n = 0; n < increments.Length; n++)
    {
        if (n == inspectionAt)
            Carrier.ReceiveTogether(C.G(new CarrierLocation(C.Pressure(root), 3)));
        var issue = C.Issue(root);
        var controlIssue = C.Issue(control);
        var before = C.Connections(root);
        var f = C.Parts(issue);
        // Only now may the harness reveal the next input.
        level += increments[n];
        if (!C.TryObserve(root, issue, new CarrierLocation(C.Sensor(root), level), out root, out var assessment) ||
            !C.TryObserve(control, controlIssue, new CarrierLocation(C.Sensor(control), level), out control, out var controlAssessment))
            throw new InvalidOperationException("A trial admission remained unresolved.");
        var a = C.Parts(assessment);
        inspected += C.Parts((Relation)a[9]).Length;
        var actual = (CarrierLocation)a[4];
        var correct = C.Pass((Relation)a[6]);
        if (correct) direction++;
        var controlCorrect = C.Pass((Relation)C.Parts(controlAssessment)[6]);
        if (controlCorrect) controlDirection++;
        var error = ReadMagnitude((Relation)a[7]);
        scale += error;
        if (previous is CarrierLocation last)
        {
            var continuationCorrect = C.Pass(C.Agreement(last, actual));
            if (continuationCorrect) lastDirection++;
            var predictsChange = !C.Pass(C.Agreement(last, (CarrierLocation)f[6]));
            if (!continuationCorrect && last.Ticks != 0 && actual.Ticks != 0)
            {
                turns++;
                if (correct) anticipated++;
            }
            if (predictsChange && continuationCorrect) falseTurns++;
        }
        previous = C.Copy(actual);
        var after = C.Connections(root);
        added += after.Count(x => !before.Any(y => ReferenceEquals(x, y)));
        removed += before.Count(x => !after.Any(y => ReferenceEquals(x, y)));
        var shadow = f[8] is CarrierLocation s ? s.Ticks.ToString(CultureInfo.InvariantCulture) : "";
        csv.AppendLine($"{name},{n + 1},{level},{actual.Ticks},{((CarrierLocation)f[6]).Ticks},{((CarrierLocation)f[5]).Ticks},{shadow},{(correct ? 1 : 0)},{(controlCorrect ? 1 : 0)},{error},{after.Length},{C.Pressure(root).Tension}");
        if ((n + 1) % 32 == 0)
        {
            var snapshot = GojoEncodedSnapshot.CaptureGraph(root);
            sizes.Add((n + 1, snapshot.Carriers.Length, snapshot.Relations.Length));
        }
    }
    var end = GojoEncodedSnapshot.CaptureGraph(root);
    report.AppendLine($"| {name} | {direction}/{increments.Length} / {controlDirection}/{increments.Length} / {lastDirection}/{increments.Length - 1} | {anticipated}/{turns} | {falseTurns} | {((double)scale / increments.Length).ToString("F2", CultureInfo.InvariantCulture)} | {added} / {removed} | {inspected} | {end.Carriers.Length} / {end.Relations.Length} | {C.Pressure(root).Tension} |");
    Console.WriteLine($"{name}: direction {direction}/{increments.Length}, turns {anticipated}/{turns}, false turns {falseTurns}, links +{added}/-{removed}");
    if (sizes.Count != 0)
        graphNotes.Add($"- {name}: {string.Join("; ", sizes.Select(x => $"{x.At}: {x.Carriers}/{x.Relations}"))}");
}
report.AppendLine("\nDirection means strict nonzero signed agreement at the supplied integer basis. Zero deltas remain uncovered by the two primitives. Last-change has no forecast for the first arrival. Magnitude error ignores sign deliberately; wrong direction with correct scale can therefore have zero magnitude error. Timing links may remain available but dormant when their source is not selected.\n");
report.AppendLine("The environment uses host arithmetic; learner forecasts, difference reads, comparison claims, support changes and timing calculations use native Gojo structure. Host code applies the declared recruitment and bounded support policy. These trials do not establish autonomous primitive discovery, physical cadence, learned utility, adaptive resolution or crystallization.");
report.AppendLine("\nCandidate inspections count primitive match reads during break-triggered recruitment. Active recurrence, assessment and any existing timing route still require per-arrival reads; this count is not a measure of total compute savings.");
report.AppendLine("\nReachable graph samples, observation count: carriers/relations. These include attachment ownership and retained evidence; they exclude discarded calculation allocations.\n");
report.AppendLine(string.Join("\n", graphNotes));
File.WriteAllText(Path.Combine(output, "first-run.md"), report.ToString());
File.WriteAllText(Path.Combine(output, "traces.csv"), csv.ToString());

static long ReadMagnitude(Relation claim)
{
    if (!OperationClaim.TryReadResolvedViolation(claim, out var value))
        throw new InvalidOperationException("Unresolved comparison is not zero error.");
    return value.Ticks;
}

static IEnumerable<(string Name, long[] Increments, int InspectionAt)> Trials()
{
    static long[] Cycles(int count, int up, int down, long magnitude = 2) =>
        Enumerable.Range(0, count).SelectMany(_ =>
            Enumerable.Repeat(magnitude, up).Concat(Enumerable.Repeat(-magnitude, down))).ToArray();
    yield return ("regular-3-5", Cycles(14, 3, 5), -1);
    yield return ("mirrored-3-5", Cycles(14, 3, 5).Select(x => -x).ToArray(), -1);
    yield return ("cadence-change", Cycles(7, 3, 5).Concat(Cycles(7, 6, 2)).ToArray(), -1);
    int[] irregular = [2, 5, 3, 1, 6, 4, 2, 7, 1, 3, 5, 2, 4, 1, 7, 2, 3, 6, 2, 1];
    yield return ("irregular", Cycles(5, 3, 5).Concat(irregular.SelectMany((length, i) =>
        Enumerable.Repeat(i % 2 == 0 ? 2L : -2L, length))).ToArray(), -1);
    long[] magnitudes = [1, 1, 4, 4, 2, 2, 7, 7, 3, 3, 1, 1, 5, 5];
    yield return ("scale-change", magnitudes.SelectMany(m => Cycles(1, 3, 5, m)).ToArray(), -1);
    var steady = Enumerable.Repeat(2L, 10).Concat(Enumerable.Repeat(7L, 24)).ToArray();
    yield return ("stable-no-request", steady, -1);
    yield return ("stable-with-request", steady, 16);
    yield return ("zero-and-chatter", Cycles(5, 3, 5).Concat(Enumerable.Repeat(0L, 5))
        .Concat(Cycles(8, 1, 1)).Concat(Enumerable.Repeat(2L, 20)).ToArray(), -1);
}
