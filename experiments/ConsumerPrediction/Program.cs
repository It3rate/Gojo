using System.Globalization;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;
using P = ConsumerPrediction.PredictionProbe;

namespace ConsumerPrediction;

internal static class Program
{
    private static int Main(string[] args)
    {
        var checks = Checks.Run();
        Console.WriteLine($"Consumer prediction checks: {checks.Length} passed.");
        foreach (var check in checks) Console.WriteLine("  " + check);
        if (args.Contains("--checks-only")) return 0;
        var folder = Path.GetFullPath(args.FirstOrDefault(arg => !arg.StartsWith("--")) ??
            "experiments/ConsumerPrediction/results");
        Directory.CreateDirectory(folder);
        var results = new List<Run>();
        var selected = new HashSet<string>();
        foreach (var (name, tolerance, samples) in TrialInputs.All())
        foreach (var variant in new[] { "redundant", "restored-scale" })
        {
            var root = variant == "redundant" ? P.CreateRedundant() : P.CreateScaleRestoration();
            var operational = P.Operational(root);
            var equations = P.Equations(root);
            var rows = new List<Row>(); // Observer-only; never passed to the learner.
            long allocation = 0;
            var attached = false;
            var attachments = 0;
            var withdrawals = 0;
            for (var index = 0; index < samples.Length; index++)
            {
                var sample = samples[index];
                var before = GC.GetTotalAllocatedBytes();
                if (!P.TryObserve(root, sample.Value, sample.Demand, tolerance, out var next, out var witness))
                    throw new InvalidOperationException("A fixed experimental admission was rejected.");
                allocation += GC.GetTotalAllocatedBytes() - before;
                root = next;
                var nowAttached = P.Attached(root);
                if (!attached && nowAttached) attachments++;
                if (attached && !nowAttached) withdrawals++;
                attached = nowAttached;
                var row = Read(index + 1, sample.Phase, sample.Value, witness, nowAttached);
                rows.Add(row);
                if (variant == "restored-scale" && row.Eligible && row.Demand && row.Evaluable)
                {
                    Capture("first-shadow-help", row.ShadowResponse > 0 && !row.DeliveredRoute);
                    Capture("first-delivered-help", row.DeliveredResponse > 0 && row.DeliveredRoute);
                    Capture("first-harm", row.ShadowResponse < 0);
                    Capture("first-withdrawal", row.SupportBefore > 0 && row.SupportAfter == 0);
                }
                void Capture(string key, bool qualifies)
                {
                    if (!qualifies || !selected.Add(key)) return;
                    File.WriteAllBytes(Path.Combine(folder, key + ".gojo"), GojoNumericFormat.Encode(witness));
                    File.WriteAllText(Path.Combine(folder, key + ".json"), JsonSerializer.Serialize(new
                    { trial = name, candidate = variant, row }, JsonOptions));
                }
            }
            var graph = GojoEncodedSnapshot.CaptureGraph(root);
            var run = new Run(name, variant, tolerance, samples.Length, rows, allocation,
                graph.Carriers.Length, graph.Relations.Length, attachments, withdrawals,
                ReferenceEquals(operational, P.Operational(root)) && equations.Zip(P.Equations(root)).All(pair =>
                    ReferenceEquals(pair.First, pair.Second)), (long)P.Support(root).Value,
                P.Pending(root) is Relation pending && P.ForecastEligible((Relation)P.Parts(pending)[2]));
            results.Add(run);
            File.WriteAllText(Path.Combine(folder, name + "-" + variant + ".csv"), Csv(rows));
            File.WriteAllBytes(Path.Combine(folder, name + "-" + variant + "-final.gojo"), GojoNumericFormat.Encode(root));
            Console.WriteLine($"{name}/{variant}: {run.Eligible.Length} eligible outcomes; " +
                $"shadow {run.Demanded.Sum(row => row.ShadowResponse)}, delivered {run.Demanded.Sum(row => row.DeliveredResponse)}; " +
                $"support {run.FinalSupport}; {attachments} attach/{withdrawals} withdraw.");
        }
        var quiet = new[] { "redundant", "restored-scale" }.Select(variant =>
        {
            var shortRun = results.Single(run => run.Name == "mixed-episodes" && run.Candidate == variant);
            var longRun = results.Single(run => run.Name == "mixed-episodes-long-quiet" && run.Candidate == variant);
            var exact = shortRun.Rows.Where(row => row.Eligible).Select(EligibleTrace)
                .SequenceEqual(longRun.Rows.Where(row => row.Eligible).Select(EligibleTrace));
            return new { candidate = variant, exactEligibleTraceMatch = exact };
        }).ToArray();
        File.WriteAllText(Path.Combine(folder, "summary.json"), JsonSerializer.Serialize(new
        {
            protocol = "03d9910", provenance = EvidenceProvenance, checks, quietGapControls = quiet,
            runs = results.Select(run => new
            {
                run.Name, run.Candidate, run.Tolerance, run.Arrivals,
                totalEligible = run.Rows.Count(row => row.Eligible),
                evaluatedEligible = run.Eligible.Length, demandedEligible = run.Demanded.Length,
                noDemandEligible = run.Eligible.Count(row => !row.Demand),
                unresolvedEligible = run.Rows.Count(row => row.Eligible && !row.Evaluable),
                unresolvedReadyIssues = run.Rows.Count(row => row.Ready && !row.Evaluable),
                incompleteIssues = run.Rows.Count(row => row.Issue.HasValue && !row.Ready),
                evaluableWithoutTurn = run.Rows.Count(row => row.Evaluable && !row.Eligible),
                baselineDemand = run.Demanded.Sum(row => row.BaselineDemand),
                candidateDemand = run.Demanded.Sum(row => row.CandidateDemand),
                deliveredDemand = run.Demanded.Sum(row => row.DeliveredDemand),
                shadowResponse = run.Demanded.Sum(row => row.ShadowResponse),
                deliveredResponse = run.Demanded.Sum(row => row.DeliveredResponse),
                positive = run.Demanded.Count(row => row.ShadowResponse > 0),
                zero = run.Demanded.Count(row => row.ShadowResponse == 0),
                negative = run.Demanded.Count(row => row.ShadowResponse < 0),
                run.Attachments, run.Withdrawals, run.FinalSupport, run.PersistentEquations,
                run.FinalCarriers, run.FinalRelations, run.AdmissionAllocatedBytes,
                run.OutstandingEligibleIssue
            })
        }, JsonOptions));
        File.WriteAllText(Path.Combine(folder, "first-run.md"), Report(results, checks, quiet.Select(q =>
            $"{q.candidate}: {q.exactEligibleTraceMatch}").ToArray()));
        Console.WriteLine("Results: " + folder);
        return 0;
    }

    // Report projection only. A row is never accepted as an input to the probe.
    private sealed record Row(long Arrival, string Phase, long? Input, long? Issue, bool Ready,
        bool Eligible, bool Demand, long? Tolerance, bool DeliveredRoute, bool Evaluable,
        long? Baseline, long? Candidate, long? Delivered, long? Actual,
        long? BaselineDemand, long? CandidateDemand, long? DeliveredDemand,
        long? ShadowResponse, long? DeliveredResponse,
        bool? BaselineDirection, bool? CandidateDirection, bool? DeliveredDirection,
        long? BaselineScaleError, long? CandidateScaleError, long? DeliveredScaleError,
        long SupportBefore, long SupportAfter, bool Attached);

    private sealed record Run(string Name, string Candidate, long Tolerance, int Arrivals,
        List<Row> Rows, long AdmissionAllocatedBytes, int FinalCarriers, int FinalRelations,
        int Attachments, int Withdrawals, bool PersistentEquations, long FinalSupport, bool OutstandingEligibleIssue)
    {
        public Row[] Eligible => Rows.Where(row => row.Eligible && row.Evaluable).ToArray();
        public Row[] Demanded => Eligible.Where(row => row.Demand).ToArray();
    }

    private static Row Read(long arrival, string phase, long? input, Relation outcome, bool attached)
    {
        var p = P.Parts(outcome);
        var before = ((CarrierLocation)p[5]).Ticks;
        var after = ((CarrierLocation)p[6]).Ticks;
        if (p[0] is not Relation issue)
            return Empty(null, false, false, false, null, false);
        var i = P.Parts(issue);
        var forecast = (Relation)i[2];
        var ready = P.ForecastReady(forecast);
        var eligible = P.ForecastEligible(forecast);
        var demanded = ((CarrierLocation)i[3]).Ticks == 1;
        if (!TimeStepSequence.TryReadSpan((Relation)i[4], out _, out var upperTolerance))
            throw new InvalidOperationException("Missing native issue interval.");
        var tolerance = upperTolerance.Ticks;
        var participating = ((CarrierLocation)i[6]).Ticks == 1;
        var issueNumber = ((CarrierLocation)i[0]).Ticks;
        if (!ready || p[2] is not Relation shadow ||
            !OperationClaim.TryReadIntervalResponse(shadow, out _, out _, out _, out var baselineDemand,
                out var candidateDemand, out var shadowResponse) || p[3] is not Relation deliveredResponseWitness ||
            !OperationClaim.TryReadIntervalResponse(deliveredResponseWitness, out _, out _, out _, out _,
                out var deliveredDemand, out var deliveredResponse))
            return Empty(issueNumber, ready, eligible, demanded, tolerance, participating);
        var read = P.Parts((Relation)P.Parts(forecast)[0]).Cast<Relation>().ToArray();
        var detail = P.Parts((Relation)p[4]);
        var diagnostics = P.Parts((Relation)detail[2]).Cast<Relation>().ToArray();
        return new(arrival, phase, input, issueNumber, ready, eligible, demanded, tolerance, participating, true,
            P.Value(read[1]).Ticks, P.Value(read[3]).Ticks, ((CarrierLocation)i[5]).Ticks,
            ((CarrierLocation)detail[0]).Ticks, baselineDemand.Ticks, candidateDemand.Ticks, deliveredDemand.Ticks,
            shadowResponse.Ticks, deliveredResponse.Ticks,
            P.DirectionAgrees(diagnostics[0]), P.DirectionAgrees(diagnostics[1]), P.DirectionAgrees(diagnostics[2]),
            P.ScaleError(diagnostics[0]), P.ScaleError(diagnostics[1]), P.ScaleError(diagnostics[2]), before, after, attached);

        Row Empty(long? issued, bool wasReady, bool wasEligible, bool wasDemanded, long? allowed, bool route) =>
            new(arrival, phase, input, issued, wasReady, wasEligible, wasDemanded, allowed, route, false,
                null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
                before, after, attached);
    }

    private static string EligibleTrace(Row row) => string.Join("|", row.Ready, row.Eligible, row.Demand,
        row.Tolerance, row.DeliveredRoute, row.Evaluable, row.Baseline, row.Candidate, row.Delivered, row.Actual,
        row.BaselineDemand, row.CandidateDemand, row.DeliveredDemand, row.ShadowResponse, row.DeliveredResponse,
        row.SupportBefore, row.SupportAfter, row.Attached);

    private static string Csv(IEnumerable<Row> rows)
    {
        var properties = typeof(Row).GetProperties();
        var text = new StringBuilder(string.Join(",", properties.Select(p => p.Name))).AppendLine();
        foreach (var row in rows)
            text.AppendLine(string.Join(",", properties.Select(property =>
                Convert.ToString(property.GetValue(row), CultureInfo.InvariantCulture) ?? "")));
        return text.ToString();
    }

    private static string Report(List<Run> runs, string[] checks, string[] quiet)
    {
        var text = new StringBuilder("# Consumer-specific prospective usefulness: fixed protocol results\n\n");
        text.AppendLine("Protocol was committed as `03d9910` before these trials. These are supplied small structures, not discovered predictors. All next-change predictions, signed errors, interval reductions, direction/scale diagnostics and reinforcement arithmetic are Gojo reads. The host supplies candidate wiring, admission order, control inputs, attachment policy and report aggregation.\n");
        text.AppendLine(EvidenceProvenance + "\n");
        text.AppendLine($"Focused checks: {checks.Length} passed. Quiet-gap ordered eligible traces: {string.Join("; ", quiet)}.\n");
        text.AppendLine("Only demanded eligible evaluable outcomes contribute to the table's demand/response totals. Shadow improvement can recruit for a later issue; it is never counted as already delivered help. Direction and absolute-magnitude error remain separate CSV columns.\n");
        text.AppendLine("| Trial | Candidate | Evaluated eligible / demanded | Baseline / candidate / delivered demand | Shadow / delivered relief | Shadow help / unchanged / harm | Attach / withdraw | Final support |\n|---|---|---:|---:|---:|---:|---:|---:|");
        foreach (var run in runs)
            text.AppendLine($"| {run.Name} | {run.Candidate} | {run.Eligible.Length} / {run.Demanded.Length} | " +
                $"{run.Demanded.Sum(r => r.BaselineDemand)} / {run.Demanded.Sum(r => r.CandidateDemand)} / {run.Demanded.Sum(r => r.DeliveredDemand)} | " +
                $"{run.Demanded.Sum(r => r.ShadowResponse)} / {run.Demanded.Sum(r => r.DeliveredResponse)} | " +
                $"{run.Demanded.Count(r => r.ShadowResponse > 0)} / {run.Demanded.Count(r => r.ShadowResponse == 0)} / {run.Demanded.Count(r => r.ShadowResponse < 0)} | " +
                $"{run.Attachments} / {run.Withdrawals} | {run.FinalSupport} |");
        text.AppendLine("\n## Retained structure and limits\n");
        text.AppendLine("The learner retains three current levels/presence spans, one frozen pending issue, bounded support, persistent equations and one latest outcome. Historical CSV rows are observer output and never enter the learner. Pure forecasts are recalculated; there is no stored successful-value lookup. Capturing the pending issue is a causal commitment, not a long-term memory model. The latest outcome is diagnostic and does not drive the next prediction.\n");
        text.AppendLine("| Trial | Candidate | Persistent equations | Final carriers / relations | Cumulative admission allocation, bytes |\n|---|---|---|---:|---:|");
        foreach (var run in runs)
            text.AppendLine($"| {run.Name} | {run.Candidate} | {run.PersistentEquations} | {run.FinalCarriers} / {run.FinalRelations} | {run.AdmissionAllocatedBytes} |");
        text.AppendLine("\nAllocation is cumulative construction during admissions, not simultaneous retained memory or a controlled benchmark. Selected `.gojo` files preserve native witnesses and final live worlds; their references to operational identities are live at capture time, while numeric issue/outcome premises are copied.\n");
        text.AppendLine("There is one fixed supplied shadow probe per run, no population search and no learned timing/segmentation. Eligibility is observed reversal, not prediction of a future reversal. The support law values signed error reduction at one supplied consumer tolerance. It does not price computation, trace retention, exploration, route maintenance or competing consumers. With no external demand, ability/support is retained and the route is silent; adverse no-demand observations therefore cannot penalize it under this protocol. Flat padding supplies valid no-turn data. Unknown separators explicitly reset readiness.\n");
        text.AppendLine("A retained route is not always an active publication: the demand/eligibility gate is zero when it has no present task. A withdrawn route retains its recipe and can be recruited again. These distinctions do not establish autonomous evolution or globally useful memory allocation.\n");
        text.AppendLine("All ready arrivals still calculate counterfactual diagnostics, including ineligible and undemanded ones. The gate filters learning and publication, not the cost of inspecting a quiet signal. The long quiet control therefore exposes extra allocation even while its eligible response trace and retained footprint remain unchanged. Restricting those reads to demanded opportunities would be a later attention/cost experiment, not a result already established here.\n");
        text.AppendLine("| Trial | Candidate | Incomplete issued premises | Ready issue, unresolved next observation | Evaluable without turn | Eligible without demand |\n|---|---|---:|---:|---:|---:|");
        foreach (var run in runs)
            text.AppendLine($"| {run.Name} | {run.Candidate} | {run.Rows.Count(r => r.Issue.HasValue && !r.Ready)} | " +
                $"{run.Rows.Count(r => r.Ready && !r.Evaluable)} | {run.Rows.Count(r => r.Evaluable && !r.Eligible)} | " +
                $"{run.Eligible.Count(r => !r.Demand)} |");
        text.AppendLine("\n## Focused checks\n");
        foreach (var check in checks) text.AppendLine("- " + check);
        return text.ToString().TrimEnd() + Environment.NewLine;
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private const string EvidenceProvenance = "Observer counters were added after the initial fixed-protocol run. " +
        "The checked-in artifacts were regenerated with unchanged learner, policies and inputs; " +
        "the original aggregate behavior was reproduced. Allocation values belong to the regenerated run, " +
        "not a splice of measurements from different runs.";
}
