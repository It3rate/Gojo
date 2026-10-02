using System.Globalization;
using System.Text;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Structures;
using PredictorScratchpad;
using N = PredictorScratchpad.NativeCircuit;

// Host: environment, arrival scheduling, read/commit orchestration, and display.
// Learner and evaluator arithmetic: Gojo equations, claims and carrier inputs.
var output = Path.GetFullPath("experiments/PredictorScratchpad/out");
Directory.CreateDirectory(output);
var checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}
CarrierLocation V(Relation read) => PredictorNetwork.Scalar(read);
string Cell(CarrierLocation value) => value.Ticks.ToString(CultureInfo.InvariantCulture);

// Mechanism checks precede reading trial outcomes; they do not tune the model.
Check(N.Value(N.Sum((N.Constant(3), 2), (N.Constant(5), -1))).Ticks == 1, "Native sum failed.");
Check(N.Value(N.Product(N.Constant(-3), N.Constant(-4))).Ticks == 12, "Signed product failed.");
Check(N.Value(N.Divide(N.Constant(11), N.Constant(4))).Ticks == 2, "Dynamic division failed.");
Check(!Equation.TryReadPrediction(N.Read(N.Divide(N.Constant(3), N.Constant(0))), out _, out _), "Zero basis invented a result.");
var signedState = N.State(-5);
var frozenSigned = N.Read(signedState);
Check(N.Assign(signedState, new CarrierLocation(signedState.Carrier, -9)) && V(frozenSigned).Ticks == -5,
    "A frozen native read changed with its source.");

Relation Metric()
{
    var count = new Carrier();
    var absolute = new Carrier();
    var squared = new Carrier();
    var bias = N.State();
    return N.Group(count, absolute, squared, bias,
        N.Divide(absolute.ValueSlot, count.ValueSlot), N.Divide(squared.ValueSlot, count.ValueSlot),
        N.Divide(bias, count.ValueSlot));
}
void Accumulate(Relation metric, CarrierLocation absolute, CarrierLocation squared, CarrierLocation error)
{
    var parts = N.Parts(metric);
    var count = (Carrier)parts[0];
    var abs = (Carrier)parts[1];
    var sq = (Carrier)parts[2];
    var bias = (CarrierLocation)parts[3];
    var reads = N.ReadMany(N.Reframe(absolute, abs), N.Reframe(squared, sq),
        N.Reframe(N.Sum((bias, 1), (error, 1)), bias.Carrier));
    Carrier.ReceiveTogether(N.Group(new CarrierLocation(count, 1), V(reads[0]), V(reads[1])));
    Check(N.Assign(bias, V(reads[2])), "Metric bias lacks a calibration.");
}
CarrierLocation Average(Relation metric, int slot, long grain = 100) =>
    N.Value(N.Divide(N.Sum((N.Parts(metric)[slot] is Carrier c ? c.ValueSlot : N.Parts(metric)[slot], grain)),
        ((Carrier)N.Parts(metric)[0]).ValueSlot));
string DecimalCell(CarrierLocation hundredths) => (hundredths.Ticks / 100m).ToString("0.00", CultureInfo.InvariantCulture);

var metrics = new Dictionary<(string Trial, string Seed, string Phase, int Channel, string Model), Relation>();
void Record(string trial, string seed, string phase, int channel, string model,
    CarrierLocation absolute, CarrierLocation squared, CarrierLocation error)
{
    var key = (trial, seed, phase, channel, model);
    if (!metrics.TryGetValue(key, out var metric)) metrics[key] = metric = Metric();
    Accumulate(metric, absolute, squared, error);
}
using var trace = new StreamWriter(Path.Combine(output, "trace.csv"), false, Encoding.UTF8);
trace.WriteLine("trial,seed,arrival,phase,channel,model,observed,forecast,error,absolute_error,squared_error,scored");
using var participation = new StreamWriter(Path.Combine(output, "participation.csv"), false, Encoding.UTF8);
participation.WriteLine("trial,seed,arrival,channel,branch,weight_before_observation");
var observations = new List<string>();
var graphSizes = new List<string>();
var selectedEvidence = new List<IElement>();
var trials = SignalTrials.Generate().ToArray();
if (args.Contains("--mechanism-only")) trials = trials.Take(1).ToArray();
foreach (var trial in trials)
{
    var network = PredictorNetwork.Create(trial.Samples[0].A, trial.Samples[0].B);
    var sizeBefore = GojoEncodedSnapshot.CaptureArchive(network);
    var firstForecast = PredictorNetwork.Forecast(network);
    var repeatedForecast = PredictorNetwork.Forecast(network);
    long[] Values(Relation read) => N.Parts(read).Cast<Relation>()
        .SelectMany(group => N.Parts(group).Cast<Relation>()).Select(prediction => V(prediction).Ticks).ToArray();
    Check(Values(firstForecast).SequenceEqual(Values(repeatedForecast)), "Unchanged reads changed a forecast.");
    var limit = args.Contains("--mechanism-only") ? 12 : trial.Samples.Length;
    for (var index = 1; index < limit; index++)
    {
        // Both channels, ALL branches and mixtures are frozen before peeking at
        // the next sample. Observe receives no environment/evaluator metadata.
        var issued = PredictorNetwork.Forecast(network);
        var issuedNumbers = Values(issued);
        var weights = PredictorNetwork.Participation(network);
        var sample = trial.Samples[index];
        var update = PredictorNetwork.Observe(network, issued, sample.A, sample.B, calibrateFrozen: index <= 40);
        Check(issuedNumbers.SequenceEqual(Values(issued)), "Input/update changed an already issued forecast.");
        var channels = N.Parts((Relation)N.Parts(network)[0]).Cast<Carrier>().ToArray();
        var forecasts = N.Parts(issued).Cast<Relation>().ToArray();
        var weightReads = N.Parts(weights).Cast<Relation>().ToArray();
        for (var channel = 0; channel < channels.Length; channel++)
        {
            var observed = channels[channel].ValueSlot;
            var predictions = N.Parts(forecasts[channel]).Cast<Relation>().ToArray();
            for (var model = 0; model < predictions.Length; model++)
            {
                var prediction = V(predictions[model]);
                var error = N.Value(N.Reframe(N.Sum((observed, 1), (prediction, -1)), observed.Carrier));
                var equality = OperationClaim.Equality(new Carrier(), error,
                    new CarrierLocation(error.Carrier, 0), error.Carrier);
                Check(OperationClaim.TryReadResolvedViolation(equality, out var absolute), "Metric comparison was unresolved.");
                var squared = N.Value(N.Product(error, error));
                var name = PredictorNetwork.DisplayNames[model];
                trace.WriteLine($"{trial.Name},{trial.Seed},{index},{sample.Phase},{channel},{name},{Cell(observed)},{Cell(prediction)},{Cell(error)},{Cell(absolute)},{Cell(squared)},{index >= 8}");
                if (index >= 8)
                {
                    Record(trial.Name, trial.Seed.ToString(), "all", channel, name, absolute, squared, error);
                    Record(trial.Name, "pooled", "all", channel, name, absolute, squared, error);
                    Record(trial.Name, "pooled", sample.Phase, channel, name, absolute, squared, error);
                }
            }
            var branchWeights = N.Parts(weightReads[channel]).Cast<Relation>().ToArray();
            for (var branch = 0; branch < branchWeights.Length; branch++)
                participation.WriteLine($"{trial.Name},{trial.Seed},{index},{channel},{PredictorNetwork.DisplayNames[branch]},{Cell(V(branchWeights[branch]))}");
        }
        if (index == 40 || index == limit - 1)
            selectedEvidence.Add(N.Group(issued, weights, update));
    }
    var sizeAfter = GojoEncodedSnapshot.CaptureArchive(network);
    Check(sizeBefore.Carriers.Length == sizeAfter.Carriers.Length && sizeBefore.Relations.Length == sizeAfter.Relations.Length &&
        sizeBefore.TickRefs.Length == sizeAfter.TickRefs.Length, "Learner graph grew with evaluator history.");
    graphSizes.Add($"{trial.Name}/{trial.Seed}: {sizeAfter.Carriers.Length} carriers, {sizeAfter.Relations.Length} relations, {sizeAfter.TickRefs.Length} coordinates; unchanged from creation.");

    // Archive one representative circuit per condition. Earlier observations
    // are evaluator evidence; only bounded current structure drives the replay.
    if (trial.Seed == 103)
    {
        var beforeSave = PredictorNetwork.Forecast(network);
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement(trial.Name, network));
        File.WriteAllText(Path.Combine(output, trial.Name + ".gojo.json"), json);
        Check(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored), "Network archive incomplete.");
        var loaded = (Relation)restored.Root;
        Check(Values(beforeSave).SequenceEqual(Values(PredictorNetwork.Forecast(loaded))), "Replay did not recalculate matching forecasts.");
        var original = Values(PredictorNetwork.Forecast(network));
        var last = trial.Samples[limit - 1];
        PredictorNetwork.Observe(loaded, PredictorNetwork.Forecast(loaded), last.A, last.B, calibrateFrozen: false);
        Check(original.SequenceEqual(Values(PredictorNetwork.Forecast(network))), "Restored update mutated original state.");
    }
    observations.Add($"Completed {trial.Name}, seed {trial.Seed}.");
    Console.WriteLine(observations[^1]);
}

var report = new List<string>
{
    "# Predictor network: observed results",
    "The protocol was committed before execution. The learner sees only two sensor values after issuing each forecast. All prediction, loss, weighting and metric arithmetic uses connected Gojo equations; the host schedules reads/commits and generates the environment.",
    "Seven supplied branches compose change, curvature, slow response and a calibrated cross-input contribution. Participation adapts; topology does not grow. Weights are not probabilities or confidence. Squared error drives adaptation; absolute error is the primary reported metric.",
    "## Pooled mean absolute error (original sensor ticks)",
    "Lower is better. Each channel stays separate. All post-warm-up observations, including transitions, contribute.",
    "| Condition | Channel | Hold | Last change | Adaptive | Equal | Frozen 40 | Running mean | Adaptive - hold | Adaptive - equal |",
    "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|"
};
foreach (var trialName in trials.Select(trial => trial.Name).Distinct())
    foreach (var channel in new[] { 0, 1 })
    {
        Relation M(string model) => metrics[(trialName, "pooled", "all", channel, model)];
        var names = new[] { "hold", "last-change", "adaptive", "equal", "frozen-40", "running-mean" };
        var values = names.Select(name => Average(M(name), 1)).ToArray();
        // Native comparisons, not differences calculated by the report.
        var versusHold = N.Value(N.Sum((values[2], 1), (values[0], -1)));
        var versusEqual = N.Value(N.Sum((values[2], 1), (values[3], -1)));
        report.Add($"| {trialName} | {channel} | {string.Join(" | ", values.Select(DecimalCell))} | {DecimalCell(versusHold)} | {DecimalCell(versusEqual)} |");
    }
using (var summaries = new StreamWriter(Path.Combine(output, "metrics.csv"), false, Encoding.UTF8))
{
    summaries.WriteLine("trial,seed,phase,channel,model,count,mean_absolute_error,mean_squared_error,signed_bias");
    foreach (var (key, metric) in metrics)
    {
        var parts = N.Parts(metric);
        summaries.WriteLine($"{key.Trial},{key.Seed},{key.Phase},{key.Channel},{key.Model},{((Carrier)parts[0]).Value},{DecimalCell(Average(metric, 1))},{DecimalCell(Average(metric, 2))},{DecimalCell(Average(metric, 3))}");
    }
}
report.Add("## Bounded learner structure");
report.AddRange(graphSizes);
report.Add($"Mechanism assertions passed: {checks}. Assertions check execution and preservation, not predictive superiority.");
report.Add("## Limits\nFixed supplied palette and update law; host-scheduled reads/temporal commits; manually calibrated cross-input contribution and loss prior; integer quantization residue; no autonomous growth, pruning, learned source separation, intervention or phase discovery. Exact numerical forecasts should not be mistaken for calibrated uncertainty. A running mean is a crucial control for shuffled input.");
File.WriteAllLines(Path.Combine(output, "observations.md"), report);
// Evaluator artifacts deliberately remain outside every learner root.
File.WriteAllText(Path.Combine(output, "metric-evidence.gojo.json"),
    GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("predictor/evaluation",
        N.Group(metrics.Where(pair => pair.Key.Seed == "pooled" && pair.Key.Phase == "all")
            .Select(pair => (IElement)pair.Value).ToArray()))));
Console.WriteLine(string.Join(Environment.NewLine, report));
