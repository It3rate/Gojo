using System.Globalization;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;

var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
try
{
    if (args.Contains("--help"))
    {
        Console.WriteLine("SamplingEmergence --output <directory> --budget <1..32> [--values <up to 64 comma-delimited integers>]");
        return 0;
    }
    var options = Parse(args);
    Directory.CreateDirectory(options.Output);
    var original = options.Values.Select(value => new FixtureInput(value, value != 0)).ToArray();
    FixtureInput[] rescaled;
    try { rescaled = original.Select(value => new FixtureInput(checked(value.Value * 100 + 1000), value.Exists)).ToArray(); }
    catch (OverflowException) { throw new ArgumentException("The rescaled fixture must fit signed 32-bit integers. Choose smaller --values."); }
    const int millimetresPerMile = 1_609_344;
    var scenarios = new[]
    {
        Run("sparse_presence", "An irregular signal", "Presence and relative ordering, with repeated values and a limited landmark budget.",
            original, options.Budget, "Explicit fixture integer calibration", "fixture ticks", options.Output, false),
        Run("rescaled_presence", "The same signal, rescaled", "Private values become 100 × value + 1000. Presence is unchanged, including missing samples.",
            rescaled, options.Budget, "Explicit rescaled fixture calibration", "fixture ticks", options.Output, false),
        Run("distance_tiny_increment", "A tiny change inside a broad interval", "90 and 110 mile anchors, then 100 miles and 100 miles + 1 mm. Distances are manually chosen test data.",
            [new(90 * millimetresPerMile, true), new(110 * millimetresPerMile, true),
             new(100 * millimetresPerMile, true), new(100 * millimetresPerMile + 1, true)],
            2, "Millimetres, explicitly calibrated: 1 mile = 1,609,344 mm", "mm", options.Output, true)
    };
    var invariance = scenarios[0].Steps.Where(step => step.Phase == "ordinal").Select(OrdinalSignature)
        .SequenceEqual(scenarios[1].Steps.Where(step => step.Phase == "ordinal").Select(OrdinalSignature));
    var result = new LabResult(new Metadata("sampling-emergence-v1", DateTimeOffset.UtcNow, invariance,
        "Private fixture values are display references. The ordinal learner receives only presence and requested -1/0/+1 comparison witnesses.",
        "Landmark rank and repeated sampling do not establish distance, equal spacing, a probability model, or a nominal size series.",
        "The landmark budget limits retained comparison anchors; this small fixture deliberately retains all visits and their witnesses.",
        GojoNumericFormat.Version), scenarios);
    var json = JsonSerializer.Serialize(result, jsonOptions);
    File.WriteAllText(Path.Combine(options.Output, "results.json"), json, new UTF8Encoding(false));
    File.WriteAllText(Path.Combine(options.Output, "report.md"), Report(result), new UTF8Encoding(false));
    var template = Path.Combine(AppContext.BaseDirectory, "lab.html");
    if (!File.Exists(template)) throw new FileNotFoundException("The lab.html presentation template is missing from the build output.", template);
    var html = File.ReadAllText(template);
    if (!html.Contains("__SAMPLING_DATA__", StringComparison.Ordinal)) throw new InvalidDataException("The presentation template has no data placeholder.");
    File.WriteAllText(Path.Combine(options.Output, "index.html"), html.Replace("__SAMPLING_DATA__", json, StringComparison.Ordinal), new UTF8Encoding(false));
    Console.WriteLine(JsonSerializer.Serialize(new
    {
        output = options.Output,
        ordinalRescalingInvariant = invariance,
        scenarios = scenarios.Select(scenario => new { scenario.Id, scenario.Budget, scenario.Summary }),
        files = new[] { "index.html", "results.json", "report.md", "<scenario>-<step>.gojo" }
    }, jsonOptions));
    return result.Scenarios.Any(scenario => scenario.Steps.Any(step => step.Unresolved)) || !invariance ? 1 : 0;
}
catch (Exception error) when (error is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static Options Parse(string[] arguments)
{
    var output = "artifacts/sampling-emergence";
    var budget = 8;
    int[] values = [0, 12, 12, 47, 0, 3, 18, 91, 6, 63, 30, 84, 15, 51, 22, 75, 9, 39, 0, 12];
    for (var i = 0; i < arguments.Length; i++)
    {
        var option = arguments[i];
        if (i + 1 == arguments.Length) throw new ArgumentException($"Missing value for {option}.");
        var text = arguments[++i];
        switch (option)
        {
            case "--output": output = text; break;
            case "--budget":
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out budget) || budget is < 1 or > 32)
                    throw new ArgumentException("--budget must be an integer from 1 through 32.");
                break;
            case "--values":
                var parts = text.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length is < 1 or > 64 || parts.Any(part => !int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)))
                    throw new ArgumentException("--values must contain 1 through 64 signed 32-bit integers separated by commas.");
                values = parts.Select(part => int.Parse(part, CultureInfo.InvariantCulture)).ToArray();
                break;
            default: throw new ArgumentException($"Unknown argument {option}.");
        }
    }
    return new(Path.GetFullPath(output), budget, values);
}

static Scenario Run(string id, string title, string description, FixtureInput[] inputs, int budget,
    string basisLabel, string unit, string output, bool distanceCalibration)
{
    var source = new Carrier();
    var presenceAxis = new Carrier();
    var orderAxis = new Carrier();
    var countAxis = new Carrier();
    var state = SamplingSurface.Create(new Carrier(), source, presenceAxis, orderAxis, countAxis, budget);
    // This dictionary is the external test instrument. Its private numbers
    // never enter Sample, Observe, or an ordinal native archive.
    var oracleValues = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
    var eventIds = new Dictionary<Relation, int>(ReferenceEqualityComparer.Instance);
    var steps = new List<Step>();
    for (var i = 0; i < inputs.Length; i++)
    {
        var input = inputs[i];
        var sample = SamplingSurface.Sample(new Carrier(), source, presenceAxis, input.Exists);
        oracleValues.Add(sample, input.Value);
        eventIds.Add(sample, i + 1);
        state = SamplingSurface.Observe(new Carrier(), state, sample, (candidate, anchor) =>
            SamplingSurface.Comparison(new Carrier(), candidate, anchor, orderAxis,
                oracleValues[candidate].CompareTo(oracleValues[anchor])));
        steps.Add(Project(state, i + 1, sample, input, oracleValues, eventIds, id, output, basisLabel, unit));
    }

    // Only now does the supplier expose an explicitly selected calibrated read.
    // The old sample magnitude slots remain unbound; the publications are new
    // evidence attached through original sample identities.
    var metricAxis = new Carrier();
    var basis = Relation.Segment(metricAxis, 0, 1);
    var capacity = FiniteReadSurface.Capacity(new Carrier(), metricAxis, -int.MaxValue, int.MaxValue, 1);
    IElement calibration = distanceCalibration
        ? new Relation(Relation.Point(new Carrier(), 1), Relation.Point(metricAxis, 1_609_344))
        : basis;
    var bindings = oracleValues.Where(entry => SamplingSurface.TryReadSample(entry.Key, out var presence, out _) && presence.Ticks == 1)
        .Select(entry => (IElement)new Relation(entry.Key, FiniteReadSurface.Publish(new Carrier(), capacity,
            new CarrierLocation(metricAxis, entry.Value), new Relation(entry.Key, calibration)))).ToArray();
    if (bindings.Length > 0)
    {
        state = SamplingSurface.WithMetric(new Carrier(), state, basis, Relation.OrderedGroup(new Carrier(), bindings));
        var lastSample = eventIds.Single(entry => entry.Value == inputs.Length).Key;
        steps.Add(Project(state, inputs.Length + 1, lastSample, inputs[^1], oracleValues, eventIds, id, output, basisLabel, unit));
    }
    var final = steps[^1];
    return new(id, title, description, budget, steps.ToArray(), new Summary(inputs.Length,
        inputs.Count(input => input.Exists), inputs.Where(input => input.Exists).Select(input => input.Value).Distinct().Count(),
        final.LandmarkCount, final.ComparisonCount, steps.Sum(step => step.ArchiveBytes), final.ArchiveBytes,
        steps.Count(step => step.Phase == "ordinal" && step.Exists && step.Bucket.Resolved && !step.Bucket.Equal),
        final.MetricAvailable, steps.Count(step => step.Unresolved)));
}

static Step Project(Relation state, int index, Relation currentSample, FixtureInput input,
    Dictionary<Relation, int> values, Dictionary<Relation, int> eventIds, string scenarioId, string output, string basisLabel, string unit)
{
    var bytes = GojoNumericFormat.Encode(state);
    var archive = $"{scenarioId}-{index:D2}.gojo";
    File.WriteAllBytes(Path.Combine(output, archive), bytes);
    var snapshot = GojoNumericFormat.ReadSnapshot(bytes);
    var unresolved = !SamplingSurface.TryReadState(state, out _, out _, out var landmarks, out var visits, out var metric, out _);
    var anchors = !unresolved && landmarks.TryReadOrderedGroup(out var nativeAnchors) ? nativeAnchors : [];
    var history = !unresolved && visits.TryReadOrderedGroup(out var nativeVisits) ? nativeVisits : [];
    var landmarkDisplay = anchors.OfType<Relation>().Select(anchor => new Landmark(eventIds[anchor], values[anchor])).ToArray();
    long? direction = null;
    var bucket = new Bucket(null, null, false, false);
    var comparisons = 0;
    foreach (var item in history)
    {
        if (item is not Relation visit || !SamplingSurface.TryReadVisit(visit, out var selected, out var region, out var directionRead, out var probes))
        { unresolved = true; continue; }
        if (probes.TryReadOrderedGroup(out var queries)) comparisons += queries.Length;
        else unresolved = true;
        if (!ReferenceEquals(selected, currentSample)) continue;
        if (directionRead is Relation witness && ReadWitness.TryRead(witness, out _, out _, out var answer, out _, out _) && answer is CarrierLocation signed)
            direction = signed.Ticks;
        if (region is Relation pair)
        {
            int? lower = pair.Left is Relation left && eventIds.TryGetValue(left, out var leftId) ? leftId : null;
            int? upper = pair.Right is Relation right && eventIds.TryGetValue(right, out var rightId) ? rightId : null;
            bucket = new(lower, upper, lower.HasValue && lower == upper, lower.HasValue || upper.HasValue);
        }
    }
    var metricValues = new List<MetricValue>();
    var metricAvailable = false;
    if (metric is Relation calibrated && calibrated.TryReadOrderedGroup(out var metricParts) && metricParts.Length == 2 &&
        metricParts[1] is Relation publications && publications.TryReadOrderedGroup(out var nativeBindings))
    {
        metricAvailable = true;
        foreach (var item in nativeBindings)
            if (item is Relation { Left: Relation sample, Right: Relation publication } && eventIds.TryGetValue(sample, out var eventId) &&
                FiniteReadSurface.TryReadValue(publication, out var value) && FiniteReadSurface.TryReadSupport(publication, out var low, out var high))
                metricValues.Add(new(eventId, value.Ticks, unit, low?.Ticks, high?.Ticks));
            else unresolved = true;
    }
    var sampleReadable = SamplingSurface.TryReadSample(currentSample, out var presence, out _);
    unresolved |= !sampleReadable;
    VerifyTransport(bytes, anchors.Length, history.Length, metricAvailable, metricValues);
    return new(index, eventIds[currentSample], input.Value, input.Exists, sampleReadable ? presence.Ticks : null,
        direction, landmarkDisplay, bucket, anchors.Length, history.Length, comparisons, metricAvailable,
        metricValues.ToArray(), false, bytes.Length, history.Length, unresolved, metricAvailable ? "metric" : "ordinal",
        metricAvailable ? basisLabel : null, archive, snapshot.Carriers.Length, snapshot.Relations.Length);
}

static void VerifyTransport(byte[] bytes, int landmarks, int samples, bool hasMetric, IReadOnlyList<MetricValue> values)
{
    var roots = GojoNumericFormat.Decode(bytes);
    if (roots.Length != 1 || roots[0] is not Relation restored ||
        !SamplingSurface.TryReadState(restored, out _, out _, out var restoredLandmarks, out var visits, out var metric, out _) ||
        !restoredLandmarks.TryReadOrderedGroup(out var anchors) || anchors.Length != landmarks ||
        !visits.TryReadOrderedGroup(out var history) || history.Length != samples)
        throw new InvalidDataException("Native state archive did not retain its sampling groups.");
    var acquisitions = new List<Relation>();
    foreach (var item in history)
        if (item is Relation visit && ReadWitness.TryRead(visit, out var selected, out _, out _, out _, out _) &&
            selected is Relation sample && SamplingSurface.TryReadSample(sample, out _, out _)) acquisitions.Add(sample);
        else throw new InvalidDataException("Native state archive did not retain an acquisition's open magnitude and presence.");
    if (anchors.Any(anchor => !acquisitions.Any(sample => ReferenceEquals(sample, anchor))))
        throw new InvalidDataException("Native state archive lost landmark-to-acquisition identity.");
    if (!hasMetric)
    {
        if (metric is not Placeholder) throw new InvalidDataException("An ordinal archive unexpectedly acquired metric evidence.");
        return;
    }
    if (metric is not Relation calibrated || !calibrated.TryReadOrderedGroup(out var fields) || fields.Length != 2 ||
        fields[1] is not Relation observations || !observations.TryReadOrderedGroup(out var bindings) || bindings.Length != values.Count)
        throw new InvalidDataException("Native state archive did not retain its metric publications.");
    for (var i = 0; i < bindings.Length; i++)
        if (bindings[i] is not Relation { Left: Relation sample, Right: Relation publication } ||
            !acquisitions.Any(acquisition => ReferenceEquals(acquisition, sample)) ||
            !FiniteReadSurface.TryReadValue(publication, out var value) || value.Ticks != values[i].Value ||
            !FiniteReadSurface.TryReadSupport(publication, out var low, out var high) ||
            low?.Ticks != values[i].Lower || high?.Ticks != values[i].Upper)
            throw new InvalidDataException("Native state archive lost a metric value, support, or acquisition binding.");
}

static string OrdinalSignature(Step step) => JsonSerializer.Serialize(new
{
    step.Presence, step.Direction, ids = step.Landmarks.Select(landmark => landmark.EventId), step.Bucket,
    step.LandmarkCount, step.SampleCount, step.ComparisonCount, step.MetricAvailable, step.Unresolved
});

static string Report(LabResult result)
{
    var text = new StringBuilder("# Sampling before measurement\n\n");
    text.AppendLine("This small exploratory Gojo fixture receives a presence predicate and a service that answers only candidate-versus-anchor order. It does not receive the private source numbers while learning its landmarks.");
    text.AppendLine($"\nThe original and transformed signals have identical ordinal projections: **{result.Metadata.OrdinalRescalingInvariant}**. The transformation changes private magnitudes by 100 × value + 1000 while preserving absence. It cannot establish a metric from ordering.");
    text.AppendLine("\n| Scenario | Acquisitions | Present | Distinct fixture values | Landmarks | Comparison queries | Coarse intervals | Final archive bytes |\n|---|---:|---:|---:|---:|---:|---:|---:|");
    foreach (var scenario in result.Scenarios)
    {
        var summary = scenario.Summary;
        text.AppendLine($"| {scenario.Title} | {summary.Acquisitions} | {summary.PresentAcquisitions} | {summary.DistinctPresentFixtureValues} | {summary.FinalLandmarks}/{scenario.Budget} | {summary.ComparisonQueries} | {summary.CoarseIntervalVisits} | {summary.FinalArchiveBytes:N0} |");
    }
    text.AppendLine("\n## What the distance example establishes\n");
    text.AppendLine("With just two anchors, both 100 miles and 100 miles + 1 mm occupy the same 90-to-110-mile interval. A separate comparison against the preceding present observation still detects the +1 direction. Neither the interval nor that sign says how large the change was.");
    text.AppendLine("\nOnly the final step supplies explicit integer-millimetre publications through a common Gojo basis, retaining the calibration 1 mile = 1,609,344 mm. The two values then read 160,934,400 mm and 160,934,401 mm. These manually chosen reference distances demonstrate the contract; they are not measurements acquired from a physical instrument.");
    text.AppendLine("\n## Structural boundary\n");
    text.AppendLine("`SamplingSurface` retains native samples, sorted anchors, visit witnesses, requested comparisons, open magnitude slots, and later sample-to-qualified-publication bindings. The C# dictionary is only the external comparison instrument. JSON and the browser view inspect the native structure and may show private reference values in a clearly separate display.");
    text.AppendLine("\nThe finite metric publication uses a signed int.MaxValue domain and quantum 1. Clipped or discarded detail follows that exposed finite-read contract; no wider exact answer is calculated to rescue it. No equal-spacing assumption, distribution, learned nominal series, or learned metric is claimed.");
    text.AppendLine("\nThe anchor budget is **not** a graph-memory bound. All visits and comparison witnesses remain retained in this small fixture, so native archive sizes can continue growing after the anchor budget fills. Each step has its own `.gojo` archive; `results.json` and `index.html` are presentation projections.");
    return text.ToString();
}

record Options(string Output, int Budget, int[] Values);
record FixtureInput(int Value, bool Exists);
record LabResult(Metadata Metadata, Scenario[] Scenarios);
record Metadata(string Schema, DateTimeOffset GeneratedAt, bool OrdinalRescalingInvariant, string SourceBoundary,
    string Limits, string HistoryPolicy, int NativeFormatVersion);
record Scenario(string Id, string Title, string Description, int Budget, Step[] Steps, Summary Summary);
record Step(int Index, int EventId, int SourceValue, bool Exists, long? Presence, long? Direction, Landmark[] Landmarks,
    Bucket Bucket, int LandmarkCount, int SampleCount, int ComparisonCount, bool MetricAvailable, MetricValue[] MetricValues,
    bool NominalAvailable, long ArchiveBytes, int HistorySamples, bool Unresolved, string Phase, string? BasisLabel,
    string ArchiveFile, int ArchiveCarriers, int ArchiveRelations);
record Landmark(int EventId, int SourceValue);
record Bucket(int? LowerEventId, int? UpperEventId, bool Equal, bool Resolved);
record MetricValue(int EventId, long Value, string Unit, long? Lower, long? Upper);
record Summary(int Acquisitions, int PresentAcquisitions, int DistinctPresentFixtureValues, int FinalLandmarks,
    int ComparisonQueries, long AllArchiveBytes, long FinalArchiveBytes, int CoarseIntervalVisits,
    bool MetricAvailable, int UnresolvedVisits);
