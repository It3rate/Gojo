using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation boundary for reproducible, bounded rule-driven search.
/// Draws native samples and publishes projections of retained rule witnesses.</summary>
public static class FriezeLearningExperiment
{
    public static int Run(string[] arguments)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "learning");
        var trials = 240; var seed = 431;
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == "--output" && i + 1 < arguments.Length) output = arguments[++i];
            else if (arguments[i] == "--trials" && i + 1 < arguments.Length && int.TryParse(arguments[++i], out var count) && count > 0) trials = count;
            else if (arguments[i] == "--seed" && i + 1 < arguments.Length && int.TryParse(arguments[++i], out var value)) seed = value;
            else throw new ArgumentException("Usage: --learn [--trials COUNT] [--seed INTEGER] [--output DIRECTORY]");
        }
        Directory.CreateDirectory(output);
        Console.WriteLine($"Searching three authored rule profiles; trials {trials}, seed {seed}.");
        var runs = FriezeLearning.Run(trials, seed);
        var panels = new List<Panel>();
        var diagnostics = new List<object>();
        foreach (var run in runs)
        {
            var feasible = run.Candidates.FirstOrDefault(candidate => candidate.HardFailures == 0);
            panels.Add(Prepare(run.Name, feasible is null ? "No feasible candidate" : "First feasible", feasible ?? run.Initial));
            panels.Add(Prepare(run.Name, "Retained", run.Best));
            var crossed = run.Candidates.Where(candidate => candidate.HardFailures == 0 &&
                candidate.MeasuredRead is not null && FriezeMeasuredRules.Describe(candidate.MeasuredRead).CrossingCount > 0).ToArray();
            var bestCrossed = crossed.OrderBy(candidate => candidate.CostTicks).FirstOrDefault();
            if (run.AllowCrossings && bestCrossed is not null)
            {
                panels.Add(Prepare("Diagonal / crossed candidates", "First crossed", crossed[0]));
                panels.Add(Prepare("Diagonal / crossed candidates", "Best crossed", bestCrossed));
            }
            var initial = Projection(run.Initial); var best = Projection(run.Best);
            diagnostics.Add(new
            {
                run.Name, run.AllowCrossings, Initial = initial,
                FirstFeasible = feasible is null ? null : Projection(feasible), Best = best,
                LegalCrossedCandidates = crossed.Length,
                BestCrossed = bestCrossed is null ? null : Projection(bestCrossed),
                FeasibleCostReduction = feasible is null || run.Best.HardFailures != 0 ? (long?)null : feasible.CostTicks - run.Best.CostTicks,
                CandidateCount = run.Candidates.Count, run.StructuralRejected, run.MeasuredQueries, run.CoverageQueries,
                HistoryNodes = Traverse(new Relation(run.History, run.ArchiveHistory)).Count(),
                ArchiveTransitions = E(run.ArchiveHistory).Length,
                SearchNodes = Traverse(run.SearchSurface).Count(),
                Candidates = run.Candidates.Select(candidate => new
                {
                    candidate.Generation, ParentGeneration = candidate.Parent?.Generation,
                    candidate.HardFailures, CostTicks = candidate.HardFailures == 0 ? (long?)candidate.CostTicks : null,
                    Measured = candidate.MeasuredRead is null ? null : FriezeMeasuredRules.Describe(candidate.MeasuredRead),
                    Coverage = candidate.Coverage?.Projection,
                    SourceCarriers = Traverse(new Relation(candidate.Fixture.Surface, candidate.Trace)).Count(element => element is Carrier or CarrierLocation)
                }).ToArray()
            });
            var initialCost = feasible is null ? "unranked" : feasible.CostTicks.ToString(CultureInfo.InvariantCulture);
            var bestCost = run.Best.HardFailures != 0 ? "unranked" : run.Best.CostTicks.ToString(CultureInfo.InvariantCulture);
            Console.WriteLine($"{run.Name}: {run.Candidates.Count} candidates; initial hard failures {run.Initial.HardFailures}; first feasible cost {initialCost}; retained cost {bestCost}; structural rejections {run.StructuralRejected}.");
        }
        var svg = Draw(panels);
        File.WriteAllText(Path.Combine(output, "gallery.svg"), svg);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(svg, runs, seed, trials));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(new
        {
            Schema = "qualitative-frieze-rule-search-v1", Seed = seed, Trials = trials,
            Notes = new[]
            {
                "Hard rules and the bounded candidate grammar are authored; this is not unrestricted discovery or general learning.",
                "Exact periodic overlap/crossing reads are distinct from finite-radius probe occupancy.",
                "Display traces are fresh executions of the same retained native program. Display-only calibration is marked when the learner skipped measurement.",
                "Source carrier counts exclude the explicitly selected measurement and occupancy suppliers."
            }, Runs = diagnostics
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"Wrote learning SVG, HTML and diagnostics to {Path.GetFullPath(output)}.");
        return runs.Any(run => run.Best.HardFailures != 0) ? 1 : 0;
    }

    private static object Projection(FriezeLearning.Evaluation evaluation) => new
    {
        evaluation.Generation, evaluation.HardFailures,
        Ranked = evaluation.HardFailures == 0, CostTicks = evaluation.HardFailures == 0 ? (long?)evaluation.CostTicks : null,
        WordSequence = WordSequence(evaluation),
        Measured = evaluation.MeasuredRead is null ? null : FriezeMeasuredRules.Describe(evaluation.MeasuredRead),
        Coverage = evaluation.Coverage?.Projection,
        SourceCarriers = Traverse(new Relation(evaluation.Fixture.Surface, evaluation.Trace)).Count(element => element is Carrier or CarrierLocation),
        DisplayOnlyCalibration = evaluation.Calibration is null
    };

    private static Panel Prepare(string profile, string role, FriezeLearning.Evaluation evaluation)
    {
        var fixture = evaluation.Fixture;
        var periodRounds = PeriodRounds(evaluation.StructuralRead);
        var trace = periodRounds > 0 ? fixture.Run(Math.Min(2048, periodRounds * 3)) : evaluation.Trace;
        var calibration = evaluation.Calibration ?? FriezeRealization.CreateCalibration(fixture,
            FriezeRealization.DefaultLengths(fixture, 4, 4));
        var points = new List<(long X, long Y)>();
        if (FriezeRealization.TryRealize(fixture, trace, calibration, out var realized) &&
            FriezeRealization.TryRead(realized, out _, out _, out var samples, out _))
            foreach (var item in E(samples).OfType<Relation>())
                if (FriezeRealization.TryReadSamplePoints(item, out _, out var before, out var after) &&
                    FriezeRealization.TryReadPoint(before, out var bx, out var by) && FriezeRealization.TryReadPoint(after, out var x, out var y))
                { if (points.Count == 0) points.Add((bx, by)); points.Add((x, y)); }
        return new(profile, role, evaluation, points, periodRounds);
    }

    private static int PeriodRounds(Relation structuralRead) => structuralRead.Right is Relation
    {
        Left: Relation { Left: Relation { Right: Relation prefix } }
    } && StructuralConstraintScope(prefix, out var entries) ? entries.Length : 0;

    private static string Draw(IReadOnlyList<Panel> panels)
    {
        const int width = 1460, rowHeight = 325, header = 94;
        var height = header + (panels.Count + 1) / 2 * rowHeight + 50;
        var svg = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" role=\"img\" aria-label=\"First feasible and retained frieze candidates under three authored rule profiles\"><style>.grid{{display:none}}.ink{{stroke-width:1.8}}</style>");
        svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#f1f3ef\"/><g font-family=\"system-ui,sans-serif\" fill=\"#173439\">");
        svg.Append("<text x=\"20\" y=\"32\" font-size=\"25\" font-weight=\"650\">Rules constrain the search; retained examples explain the result</text>");
        svg.Append("<text x=\"20\" y=\"60\" font-size=\"14\">Three controller periods per panel. Measured geometry refines the same unmeasured motion program.</text>");
        for (var index = 0; index < panels.Count; index++)
        {
            var panel = panels[index]; var left = 14 + index % 2 * 724; var top = header + index / 2 * rowHeight;
            var best = index % 2 == 1; var evaluation = panel.Evaluation;
            svg.Append($"<rect x=\"{left}\" y=\"{top}\" width=\"710\" height=\"312\" rx=\"13\" fill=\"#fff\" stroke=\"{(best ? "#91b5a5" : "#d7dfd8")}\"/>");
            Text(left + 18, top + 26, panel.Profile, 17, "600");
            var cost = evaluation.HardFailures == 0 ? evaluation.CostTicks.ToString(CultureInfo.InvariantCulture) : "unranked";
            Text(left + 18, top + 48, $"{panel.Role} · generation {evaluation.Generation} · hard failures {evaluation.HardFailures} · cost {cost}", 12);
            var measured = evaluation.MeasuredRead is null ? null : FriezeMeasuredRules.Describe(evaluation.MeasuredRead);
            var coverage = evaluation.Coverage?.Projection;
            Text(left + 18, top + 69, measured is null ? "Measured rules not queried by search; picture is an explicit display request." :
                $"Overlaps {measured.OverlapCount} · crossings {measured.CrossingCount} · period {measured.PeriodX} · height {measured.Height}", 12);
            if (panel.Points.Count != 0)
            {
                var row = panels.Skip(index / 2 * 2).Take(2).ToArray();
                var rowPoints = row.SelectMany(p => p.Points).ToArray();
                var probePoints = row.Where(p => p.Evaluation.Coverage is not null)
                    .SelectMany(p => ProbePositions(p.Evaluation.Coverage!)).ToArray();
                var minX = rowPoints.Min(p => p.X); var maxX = rowPoints.Max(p => p.X);
                var minY = rowPoints.Min(p => p.Y); var maxY = rowPoints.Max(p => p.Y);
                if (probePoints.Length != 0)
                { minY = Math.Min(minY, probePoints.Min(p => p.Y)); maxY = Math.Max(maxY, probePoints.Max(p => p.Y)); }
                var scale = Math.Min(660.0 / Math.Max(1, maxX - minX), 128.0 / Math.Max(1, maxY - minY));
                var ox = left + 25 + (660 - (maxX - minX) * scale) / 2;
                var oy = top + 91 + (128 - (maxY - minY) * scale) / 2;
                var coordinates = string.Join(" ", panel.Points.Select(p => F(ox + (p.X - minX) * scale) + "," + F(oy + (maxY - p.Y) * scale)));
                svg.Append($"<defs><clipPath id=\"plot-{index}\"><rect x=\"{left + 18}\" y=\"{top + 82}\" width=\"674\" height=\"146\"/></clipPath></defs><g clip-path=\"url(#plot-{index})\">");
                if (evaluation.Coverage is not null) Grid(evaluation.Coverage, minX, maxX, maxY, ox, oy, scale);
                svg.Append($"<polyline class=\"ink\" style=\"--footprint:{F(scale * 2)}\" points=\"{coordinates}\" fill=\"none\" stroke=\"{(best ? "#147b73" : "#94634d")}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
                svg.Append("</g>");
            }
            else Text(left + 25, top + 150, "This source has no complete measured sample path.", 14);
            Text(left + 18, top + 246, coverage is null ? "Coverage supplier not queried." :
                $"Probe cells {coverage.Cells} · empty {coverage.EmptyFraction:P0} · multiple {coverage.MultipleFraction:P0}", 12);
            Text(left + 18, top + 266, coverage is null ? "Unsettled hard rules remain visible before finer observations." :
                $"Clearance maxima {coverage.Centers} · spread {coverage.ClearanceSpread} · greatest shell {coverage.MaximumClearance}", 12);
            Text(left + 18, top + 289, "Verified ink symmetry: " + (evaluation.MeasuredRead is null ? "not queried" :
                evaluation.Symmetries.Length == 0 ? "none established" : string.Join(", ", evaluation.Symmetries)), 11);
        }
        Text(20, height - 18, "Probe occupancy has finite radius and resolution. Multiple occupancy is not an exact geometric retracing verdict.", 13);
        return svg.Append("</g></svg>").ToString();

        void Text(int x, int y, string text, int size, string weight = "400") => svg.Append($"<text x=\"{x}\" y=\"{y}\" font-size=\"{size}\" font-weight=\"{weight}\">{WebUtility.HtmlEncode(text)}</text>");
        void Grid(FriezeCoverage.Evaluation coverage, long minX, long maxX, long maxY, double ox, double oy, double scale)
        {
            if (coverage.Surface.Right is not Relation { Left: Relation { Left: Relation positions, Right: Relation joins } } ||
                E(joins).FirstOrDefault() is not Relation { Right: Relation span } ||
                span.Right is not Relation { Left: CarrierLocation end } || end.Ticks <= 0) return;
            var period = end.Ticks;
            if (!coverage.Coverage.TryReadResult(coverage.Read, out var empty, out var single, out var multiple, out _, out _, out _)) return;
            var colors = new Dictionary<IElement, string>(ReferenceEqualityComparer.Instance);
            foreach (var (scope, color) in new[] { (empty, "#d8e0df"), (single, "#81c4ac"), (multiple, "#e6af6c") })
                foreach (var row in E(scope).OfType<Relation>()) colors[row.Left] = color;
            svg.Append("<g class=\"grid\" opacity=\"0.7\">");
            foreach (var item in E(positions).OfType<Relation>())
                if (item.Right is Relation point && FriezeRealization.TryReadPoint(point, out var x, out var y))
                {
                    var first = (long)Math.Ceiling((double)(minX - x) / period);
                    var last = (long)Math.Floor((double)(maxX - x) / period);
                    for (var shift = first; shift <= last; shift++)
                        svg.Append($"<circle cx=\"{F(ox + (x + shift * period - minX) * scale)}\" cy=\"{F(oy + (maxY - y) * scale)}\" r=\"{F(Math.Max(0.8, scale * 0.15))}\" fill=\"{colors.GetValueOrDefault(item.Left, "#af9ccc")}\"/>");
                }
            svg.Append("</g>");
        }
    }

    private static string Html(string svg, IReadOnlyList<FriezeLearning.RunResult> runs, int seed, int trials) =>
        "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Rule-driven frieze search</title>" +
        "<style>body{margin:0;background:#f1f3ef;color:#173439;font:16px system-ui}main{max-width:1460px;margin:auto;padding:28px}h1{font-size:29px}p{max-width:1080px;line-height:1.55}.controls{display:flex;gap:24px;padding:14px 0}svg{width:100%;height:auto}.show-grid .grid{display:inline}.show-footprint .ink{stroke-width:var(--footprint);opacity:.6}a{color:#147b73}table{border-collapse:collapse;background:white}td,th{padding:10px 16px;border-bottom:1px solid #d7dfd8;text-align:left}</style>" +
        $"<main><h1>Bounded search over native motion programs</h1><p>{runs.Count} authored profiles; {trials} trials; seed {seed}. Hard rules reject periodic retracing and optionally proper crossings, and cap the program's phase participation. Softer preferences compare probe coverage, repeated occupancy and ordinal clearance. Every retained score belongs to its explicit interpretation.</p>" +
        "<p>The gallery compares the first feasible candidate with the retained candidate, so cost changes compare measured, valid programs. Rejected starting programs are unranked; their rejection sentinel is not an improvement score. The search history connects proposals, parents, native rule witnesses and measured suppliers.</p>" +
        ProgressTable(runs) +
        "<div class=\"controls\"><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('show-grid',this.checked)\"> Show periodic probes</label><label><input type=\"checkbox\" onchange=\"document.body.classList.toggle('show-footprint',this.checked)\"> Show selected radius</label></div>" +
        svg + "<p>Green probes have one footprint; amber probes have several; grey probes are empty. Legitimate joints can cover several probes without geometric retracing. Probe cells and graph shells are selected observations, not exact continuous area or an innate distance.</p>" +
        "<p>The fixed probe band and finite radius strongly shape these preferences. H4/V4 to H6/V4 is an affine aspect check, not independent random calibration trials. This bounded search does not establish general learning or replace the source with numeric coordinates.</p>" +
        "<p><a href=\"diagnostics.json\">Complete candidate diagnostics</a> · <a href=\"gallery.svg\">Standalone SVG</a></p></main></html>";

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string[] WordSequence(FriezeLearning.Evaluation evaluation)
    {
        var words = new List<string>();
        if (!evaluation.Fixture.TryReadTrace(evaluation.Trace, out _, out _, out _, out _, out var samples, out _)) return [];
        foreach (var item in E(samples).OfType<Relation>())
        {
            if (!FriezeFixture.TryReadSample(item, out _, out _, out _, out _, out var applications, out _))
            { words.Add("open sample"); continue; }
            var terms = new List<string>();
            foreach (var application in E(applications))
                if (application is Relation { Right: Relation motion } &&
                    evaluation.Fixture.Frame.Direction(motion, out var axis, out var forward, out var reach))
                {
                    var horizontal = ReferenceEquals(axis, evaluation.Fixture.Frame.Horizontal);
                    var direction = horizontal ? forward ? "right" : "left" : forward ? "up" : "down";
                    var index = Array.FindIndex(evaluation.Fixture.Reaches.ToArray(), source => ReferenceEquals(source, reach));
                    terms.Add($"{direction}[{(index < 0 ? "open reach" : $"r{index + 1}")}]");
                }
                else terms.Add("open motion");
            words.Add(terms.Count == 0 ? "pause" : string.Join(" + ", terms));
        }
        return words.ToArray();
    }
    private static string ProgressTable(IReadOnlyList<FriezeLearning.RunResult> runs)
    {
        var text = new StringBuilder("<table><thead><tr><th>Profile</th><th>Initial</th><th>First feasible cost</th><th>Retained cost</th><th>Cost reduction</th></tr></thead><tbody>");
        foreach (var run in runs)
        {
            var first = run.Candidates.FirstOrDefault(candidate => candidate.HardFailures == 0);
            string Cost(FriezeLearning.Evaluation? value) => value is null || value.HardFailures != 0 ? "unranked" : value.CostTicks.ToString(CultureInfo.InvariantCulture);
            var reduction = first is null || run.Best.HardFailures != 0 ? "—" : (first.CostTicks - run.Best.CostTicks).ToString(CultureInfo.InvariantCulture);
            text.Append($"<tr><td>{WebUtility.HtmlEncode(run.Name)}</td><td>{(run.Initial.HardFailures == 0 ? "feasible" : $"rejected ({run.Initial.HardFailures})")}</td><td>{Cost(first)}</td><td>{Cost(run.Best)}</td><td>{reduction}</td></tr>");
        }
        return text.Append("</tbody></table>").ToString();
    }
    private static IEnumerable<(IElement Cell, long X, long Y)> ProbePositions(FriezeCoverage.Evaluation coverage)
    {
        if (coverage.Surface.Right is not Relation { Left: Relation { Left: Relation coordinates } }) yield break;
        foreach (var row in E(coordinates).OfType<Relation>())
            if (row.Right is Relation point && FriezeRealization.TryReadPoint(point, out var x, out var y)) yield return (row.Left, x, y);
    }
    private static bool StructuralConstraintScope(Relation scope, out IElement[] entries) =>
        Gojo.Patterns.StructuralConstraint.TryReadScope(scope, out entries, out var start, out var end) && start.Equals(end);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static IEnumerable<IElement> Traverse(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var current))
        { if (!seen.Add(current)) continue; yield return current; if (current is Relation r) { stack.Push(r.Right); stack.Push(r.Left); } }
    }
    private sealed record Panel(string Profile, string Role, FriezeLearning.Evaluation Evaluation,
        IReadOnlyList<(long X, long Y)> Points, int PeriodRounds);
}
