using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze;

if (args.Contains("--learn")) return FriezeLearningExperiment.Run(args.Where(argument => argument != "--learn").ToArray());
if (args.Contains("--shape")) return PatternShapingExperiment.Run(args.Where(argument => argument != "--shape").ToArray());
if (args.Contains("--layers")) return PatternLayerExperiment.Run(args.Where(argument => argument != "--layers").ToArray());
if (args.Contains("--rhythms")) return RhythmPatternExperiment.Run(args.Where(argument => argument != "--rhythms").ToArray());
if (args.Contains("--rhythm-friezes")) return RhythmFriezeExperiment.Run(args.Where(argument => argument != "--rhythm-friezes").ToArray());
if (args.Contains("--connected")) return ConnectedPatternExperiment.Run(args.Where(argument => argument != "--connected").ToArray());
if (args.Contains("--machines")) return PatternMachineExperiment.Run(args.Where(argument => argument != "--machines").ToArray());
if (args.Contains("--primitive-flows")) return PrimitiveFlowExperiment.Run(args.Where(argument => argument != "--primitive-flows").ToArray());
if (args.Contains("--unit-arrangements")) return UnitArrangementExperiment.Run(args.Where(argument => argument != "--unit-arrangements").ToArray());
if (args.Contains("--ornament-program")) return OrnamentProgramExperiment.Run(args.Where(argument => argument != "--ornament-program").ToArray());
if (args.Contains("--bourgoin-patterns")) return BourgoinPatternExperiment.Run(args.Where(argument => argument != "--bourgoin-patterns").ToArray());
if (args.Contains("--bourgoin-colour")) return BourgoinColourExperiment.Run(args.Where(argument => argument != "--bourgoin-colour").ToArray());
if (args.Contains("--ornament-designs")) return BourgoinColourExperiment.RunDesigns(args.Where(argument => argument != "--ornament-designs").ToArray());
if (args.Contains("--ornament-perspectives")) return OrnamentPerspectiveExperiment.Run(args.Where(argument => argument != "--ornament-perspectives").ToArray());
if (args.Contains("--ornament-discovery")) return OrnamentDiscoveryExperiment.Run(args.Where(argument => argument != "--ornament-discovery").ToArray());
if (args.Contains("--ornament-perturbations")) return OrnamentPerturbationExperiment.Run(args.Where(argument => argument != "--ornament-perturbations").ToArray());
if (args.Contains("--hierarchical-recognition"))
{
    var hierarchicalOutput = Path.Combine("experiments", "QualitativeFrieze", "results", "hierarchical-recognition");
    var hierarchicalArgs = args.Where(argument => argument != "--hierarchical-recognition").ToArray();
    for (var i = 0; i < hierarchicalArgs.Length; i++)
        if (hierarchicalArgs[i] == "--output" && i + 1 < hierarchicalArgs.Length) hierarchicalOutput = hierarchicalArgs[++i];
        else throw new ArgumentException("Usage: --hierarchical-recognition [--output DIRECTORY]");
    return QualitativeHierarchicalRecognitionExperiment.Run(hierarchicalOutput);
}
if (args.Contains("--recognition-firmness"))
{
    var firmnessOutput = Path.Combine("experiments", "QualitativeFrieze", "results", "recognition-firmness");
    var firmnessArgs = args.Where(argument => argument != "--recognition-firmness").ToArray();
    for (var i = 0; i < firmnessArgs.Length; i++)
        if (firmnessArgs[i] == "--output" && i + 1 < firmnessArgs.Length) firmnessOutput = firmnessArgs[++i];
        else throw new ArgumentException("Usage: --recognition-firmness [--output DIRECTORY]");
    return QualitativeRecognitionFirmnessExperiment.Run(firmnessOutput);
}
if (args.Contains("--qualitative-recognition"))
{
    var recognitionOutput = Path.Combine("experiments", "QualitativeFrieze", "results", "qualitative-recognition");
    var recognitionArgs = args.Where(argument => argument != "--qualitative-recognition").ToArray();
    for (var i = 0; i < recognitionArgs.Length; i++)
        if (recognitionArgs[i] == "--output" && i + 1 < recognitionArgs.Length) recognitionOutput = recognitionArgs[++i];
        else throw new ArgumentException("Usage: --qualitative-recognition [--output DIRECTORY]");
    return QualitativeRecognitionExperiment.Run(recognitionOutput);
}
if (args.Contains("--wallpaper-machine")) return WallpaperMachineExperiment.Run(args.Where(argument => argument != "--wallpaper-machine").ToArray());
if (args.Contains("--wallpaper-field")) return ConnectedWallpaperExperiment.Run(args.Where(argument => argument != "--wallpaper-field").ToArray());
if (args.Contains("--junction-feedback")) return JunctionFeedbackExperiment.Run(args.Where(argument => argument != "--junction-feedback").ToArray());
if (args.Contains("--adaptive-wallpaper")) return AdaptiveWallpaperExperiment.Run(args.Where(argument => argument != "--adaptive-wallpaper").ToArray());
if (args.Contains("--wallpaper-flow"))
{
    var study = new WallpaperFlowStudy();
    return PatternMachineExperiment.Run(args.Where(argument => argument != "--wallpaper-flow").ToArray(),
        [study.Rotation, study.Glide, study.Placement], Path.Combine("experiments", "QualitativeFrieze", "results", "wallpaper-flow"), 16);
}
if (args.Contains("--square-cell")) return SquareCellExperiment.Run(args.Where(argument => argument != "--square-cell").ToArray());

var output = Path.Combine("experiments", "QualitativeFrieze", "results");
var rounds = 72;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
    else if (args[i] == "--rounds" && i + 1 < args.Length && int.TryParse(args[++i], out var count) && count is > 0 and <= 256) rounds = count;
    else throw new ArgumentException("Usage: --output DIRECTORY [--rounds 1..256]");
}
Directory.CreateDirectory(output);
var slowFast = FriezeFixture.SlowFastDrift();
var individualFirings = slowFast.WithSampling(StructuralConstraint.Closed(Entries(slowFast.Network.Ports)
    .Where(port => !ReferenceEquals(port, slowFast.RootPort)).ToArray()), "Slow / fast / drift — every firing");
FriezeFixture[] fixtures = [FriezeFixture.SquareWave(), FriezeFixture.SquareWave(false),
    FriezeFixture.Meander(), slowFast, individualFirings,
    FriezeFixture.PausesAndDrift(), FriezeFixture.VariableSteps()];
var panels = new List<Panel>();
var diagnostics = new List<RunDiagnostic>();
var failed = false;
foreach (var fixture in fixtures)
{
    var trace = fixture.Run(rounds);
    if (!fixture.TryReadTrace(trace, out var initial, out _, out var steps, out var motions, out var samples, out var pending))
        throw new InvalidOperationException("The native trace contract did not read.");
    var source = new Relation(fixture.Surface, trace);
    var sourceElements = Traverse(source).ToArray();
    var carrierCount = sourceElements.Count(element => element is Carrier);
    var locationCount = sourceElements.Count(element => element is CarrierLocation);
    var firstReturn = 0; var returns = 0; var stepIndex = 0;
    foreach (var step in Entries(steps).OfType<Relation>())
    {
        stepIndex++;
        if (fixture.Network.TryReadStep(step, out _, out _, out var after, out _, out _, out _) && fixture.SamePhaseConfiguration(initial, after))
        { if (firstReturn == 0) firstReturn = stepIndex; returns++; }
    }
    var openComparisons = 0; var executionPending = 0;
    foreach (var item in Entries(pending))
        if (item is Relation { Left: Relation sample, Right: Relation } &&
            FriezeFixture.TryReadSample(sample, out _, out _, out _, out _, out _, out _)) openComparisons++;
        else executionPending++;
    var displayRounds = firstReturn == 0 ? Entries(steps).Length : Math.Min(Entries(steps).Length, firstReturn * 4);
    var displayedCauses = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
    foreach (var step in Entries(steps).Take(displayRounds).Cast<Relation>())
    {
        displayedCauses.Add(step);
        if (fixture.Network.TryReadStep(step, out _, out _, out _, out var admitted, out _, out _))
            foreach (var phaseReceipt in Entries(admitted)) displayedCauses.Add(phaseReceipt);
    }
    var embeddings = new List<EmbeddingDiagnostic>();
    foreach (var basis in new[] { (Horizontal: 12L, Vertical: 20L), (Horizontal: 24L, Vertical: 10L) })
    {
        var lengths = FriezeRealization.DefaultLengths(fixture, basis.Horizontal, basis.Vertical);
        var calibration = FriezeRealization.CreateCalibration(fixture, lengths);
        var complete = FriezeRealization.TryRealize(fixture, trace, calibration, out var realization);
        if (!FriezeRealization.TryRead(realization, out var original, out _, out var clauses, out var unresolved) || !ReferenceEquals(trace, original))
            throw new InvalidOperationException("The realization lost its source trace.");
        var points = new List<(long X, long Y)>();
        (long X, long Y) end = (0, 0);
        foreach (var clause in Entries(clauses).Cast<Relation>())
        {
            if (!FriezeRealization.TryReadSamplePoints(clause, out var sample, out var before, out var after) ||
                !FriezeRealization.TryReadPoint(before, out var bx, out var by) || !FriezeRealization.TryReadPoint(after, out var x, out var y))
                throw new InvalidOperationException("A complete point clause did not read.");
            end = (x, y);
            FriezeFixture.TryReadSample(sample, out var cause, out _, out _, out _, out _, out _);
            if (!displayedCauses.Contains(cause)) continue;
            if (points.Count == 0) points.Add((bx, by));
            points.Add((x, y));
        }
        panels.Add(new(fixture.Name, $"H {basis.Horizontal}, V {basis.Vertical}; {points.Count - 1} samples in first {displayRounds} root drives", points));
        var sizes = lengths.Select(pair => pair.Value).Order().ToArray();
        embeddings.Add(new(basis.Horizontal, basis.Vertical, sizes, complete, Entries(unresolved).Length, end.X, end.Y));
        failed |= !complete;
    }
    diagnostics.Add(new(fixture.Name, rounds, Entries(steps).Length, Entries(motions).Length,
        Entries(samples).Length, carrierCount, locationCount, firstReturn == 0 ? null : firstReturn,
        returns, openComparisons, executionPending, embeddings));
    failed |= carrierCount != 0 || locationCount != 0 || executionPending != 0;
    Console.WriteLine($"{fixture.Name}: {Entries(steps).Length} root drives, {Entries(motions).Length} motions, {Entries(samples).Length} samples; phase return {firstReturn}; source carriers {carrierCount}; unresolved ordinal reads {openComparisons}.");
}
var svg = Gallery(panels);
File.WriteAllText(Path.Combine(output, "gallery.svg"), svg);
var html = "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">" +
    "<title>Qualitative motion generators</title><style>body{margin:0;background:#f2f3f0;color:#192b35;font:16px system-ui}main{max-width:1500px;margin:auto;padding:28px}h1{font-size:28px}p{max-width:1000px;line-height:1.55}svg{width:100%;height:auto}a{color:#176b70}</style>" +
    "<main><h1>Qualitative motion generators</h1><p>The source contains ordered motion, shared unmeasured reaches, explicit event routes and sampling. Each column supplies a separate positive measured interpretation of the same trace. Distances, aspect ratio and Euclidean symmetry are not inferred by the qualitative source.</p>" +
    "<p>Coupled and direct square waves preserve the same sampled path through different causal histories. Sampling each firing reveals motion that joint samples can hide. Open ordinal comparisons remain evidence; they are not execution errors. Learning is outside this experiment.</p>" +
    svg + "<p><a href=\"diagnostics.json\">Diagnostics</a> · <a href=\"gallery.svg\">Standalone SVG</a></p></main></html>";
File.WriteAllText(Path.Combine(output, "gallery.html"), html);
File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
{
    Schema = "qualitative-frieze-run-v1", Rounds = rounds, Variants = fixtures.Length,
    TotalSourceCarriers = diagnostics.Sum(run => run.SourceCarriers),
    TotalSourceLocations = diagnostics.Sum(run => run.SourceLocations),
    Notes = new[] { "Positive illustrative calibrations are separate from source structure.",
        "Phase recurrence rounds do not establish equal elapsed duration or metric frieze symmetry.",
        "Unresolved ordinal comparisons do not imply a lost motion application." }, Runs = diagnostics
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Wrote SVG, HTML and JSON to {Path.GetFullPath(output)}.");
return failed ? 1 : 0;

static string Gallery(IReadOnlyList<Panel> panels)
{
    const int width = 1460, rowHeight = 205, header = 90;
    var height = header + (panels.Count / 2) * rowHeight + 55;
    var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"Seven qualitative motion generators under two illustrative calibrations\">");
    svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#f2f3f0\"/><g font-family=\"system-ui,sans-serif\" fill=\"#192b35\">");
    svg.Append("<text x=\"22\" y=\"31\" font-size=\"23\" font-weight=\"650\">Same qualitative structure, different measured interpretations</text>");
    svg.Append("<text x=\"22\" y=\"58\" font-size=\"14\">Geometry below is an explicit rendering calibration. Shared reaches and their inverses retain one source.</text>");
    for (var index = 0; index < panels.Count; index++)
    {
        var panel = panels[index]; var column = index % 2; var row = index / 2;
        var left = 14 + column * 724; var top = header + row * rowHeight;
        svg.Append($"<rect x=\"{left}\" y=\"{top}\" width=\"710\" height=\"192\" rx=\"12\" fill=\"#fff\" stroke=\"#d7dfdb\"/>");
        svg.Append($"<text x=\"{left + 17}\" y=\"{top + 26}\" font-size=\"16\" font-weight=\"600\">{WebUtility.HtmlEncode(panel.Name)}</text>");
        svg.Append($"<text x=\"{left + 17}\" y=\"{top + 47}\" font-size=\"12\" fill=\"#607575\">{WebUtility.HtmlEncode(panel.Basis)}</text>");
        if (panel.Points.Count == 0) continue;
        var minX = panel.Points.Min(point => point.X); var maxX = panel.Points.Max(point => point.X);
        var minY = panel.Points.Min(point => point.Y); var maxY = panel.Points.Max(point => point.Y);
        var scale = Math.Min(662.0 / Math.Max(1L, maxX - minX), 108.0 / Math.Max(1L, maxY - minY));
        var offsetX = left + 24 + (662 - (maxX - minX) * scale) / 2;
        var offsetY = top + 67 + (108 - (maxY - minY) * scale) / 2;
        string Position((long X, long Y) point) => F(offsetX + (point.X - minX) * scale) + "," + F(offsetY + (maxY - point.Y) * scale);
        var coordinates = string.Join(" ", panel.Points.Select(Position));
        svg.Append($"<polyline points=\"{coordinates}\" fill=\"none\" stroke=\"#147d80\" stroke-width=\"1.6\" stroke-linejoin=\"round\"/>");
        svg.Append($"<circle cx=\"{F(offsetX + (panel.Points[0].X - minX) * scale)}\" cy=\"{F(offsetY + (maxY - panel.Points[0].Y) * scale)}\" r=\"3\" fill=\"#d8793b\"/>");
    }
    svg.Append($"<text x=\"22\" y=\"{height - 18}\" font-size=\"13\">Orange marks the initial sample. Each line joins actual committed samples; individual motion history remains in the native trace.</text></g></svg>");
    return svg.ToString();
}
static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
static IElement[] Entries(Relation scope) => FiniteLaw.Entries(scope);
static IEnumerable<IElement> Traverse(IElement root)
{
    var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
    while (stack.TryPop(out var current))
    {
        if (!seen.Add(current)) continue;
        yield return current;
        if (current is Relation relation) { stack.Push(relation.Right); stack.Push(relation.Left); }
    }
}
record Panel(string Name, string Basis, IReadOnlyList<(long X, long Y)> Points);
record EmbeddingDiagnostic(long HorizontalBasis, long VerticalBasis, long[] ReachLengths, bool Complete,
    int Unresolved, long FinalX, long FinalY);
record RunDiagnostic(string Name, int RequestedRounds, int RootDrives, int MotionApplications, int Samples,
    int SourceCarriers, int SourceLocations, int? PhaseRecurrenceRounds, int PhaseReturns,
    int OpenOrdinalReads, int ExecutionUnresolved, IReadOnlyList<EmbeddingDiagnostic> Embeddings);
