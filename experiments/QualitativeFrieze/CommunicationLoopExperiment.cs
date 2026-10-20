using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation adapter. Native output is analyzed before any screen
/// positions, colours, labels or JSON diagnostics are created.</summary>
public static partial class CommunicationLoopExperiment
{
    private sealed record PatternModel(string Kind, int Sites, int Segments, int Occurrences, string Signature);
    private sealed record ReadModel(int Supported, int Refuted, int Unresolved, bool Satisfied);
    private sealed record SampleModel(string Id, string Name, string Image, int Admissible, int Examined,
        int SentHints, string[] SentPatterns, int ProposalWork, ReadModel Read, PatternModel[] Patterns);
    private sealed record TrialModel(string Context, SampleModel Before, SampleModel After, string Repair);
    private sealed record CaseModel(string Id, string Name, string Description, string IntentImage,
        int Commitments, string[] Promises, int InitialHints, TrialModel[] Trials);
    private sealed record AblationOptionModel(int Hints, int MessageParticipants, int Admissible,
        bool Robust, string[] SentPatterns);
    private sealed record AblationModel(string Case, int Options, int MinimumHints,
        AblationOptionModel[] Best, AblationOptionModel[] All);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "communication-loop");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --communication-loop [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study = CommunicationLoopStudy.Build();
        var ablation = CommunicationHintAblation.Build(study);
        var cases = new List<CaseModel>();
        foreach (var item in study.Cases)
        {
            var intentImage = item.Id + "-intent.svg";
            File.WriteAllText(Path.Combine(output, intentImage), Svg(item.IntentExemplar.Frame,
                item.IntentExemplar.Graph, item.IntentExemplar.Bindings, item.Name + " / one example of the intended structure"));
            var trials = new List<TrialModel>();
            foreach (var (trial, index) in item.Trials.Select((trial, index) => (trial, index)))
            {
                SampleModel Sample(CommunicationLoopStudy.Selection selection, string suffix)
                {
                    if (selection.Selected is not { } selected || selection.Critic is not { } critic || selection.Assessment is not { } assessment)
                        throw new InvalidOperationException("The illustrated run requires a selected, independently assessed output.");
                    var realized = selection.Output ?? throw new InvalidOperationException("A sample needs a newly executed receiver output.");
                    var image = item.Id + "-" + (index + 1) + "-" + suffix + ".svg";
                    File.WriteAllText(Path.Combine(output, image), Svg(realized.Frame, realized.Graph, realized.Bindings, item.Name + " / " + suffix));
                    var patterns = critic.Families.Select(family => new PatternModel(family.Kind,
                        family.SiteCount, family.SegmentCount, family.Occurrences.Count, family.Signature)).ToArray();
                    return new(selected.Id, selected.Name, image,
                        selection.Candidates.Count(candidate => candidate.Assessment.IsSatisfied), selection.Candidates.Count,
                        Hints(selection.Message), Clauses(selection.Message), E(critic.Consumed).Length,
                        new(E(assessment.Supported).Length, E(assessment.Refuted).Length,
                            E(assessment.Unresolved).Length, assessment.IsSatisfied), patterns);
                }
                var before = Sample(trial.Before, "before"); var after = Sample(trial.After, "after");
                var repair = trial.Repair is null ? "No additional hint was needed."
                    : "Add one " + (trial.Repair.Forbidden ? "forbidden" : "required") + " native pattern after inspecting the failed commitment.";
                trials.Add(new(trial.Context.Name, before, after, repair));
                Console.WriteLine($"{item.Id} / {trial.Context.Name}: {before.Read.Satisfied} -> {after.Read.Satisfied}; " +
                    $"{before.SentHints} -> {after.SentHints} hints, {before.Admissible} -> {after.Admissible} admissible samples.");
            }
            cases.Add(new(item.Id, item.Name, item.Description, intentImage, Hints(item.Intent), Clauses(item.Intent), Hints(item.InitialMessage), trials.ToArray()));
        }
        var capture = new Relation(study.Surface, ablation.Surface);
        var native = Reachable(capture).ToArray();
        var numeric = native.Count(item => item is Carrier or CarrierLocation);
        if (numeric != 0) throw new InvalidOperationException("This communication experiment must remain pre numeric.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("communication-loop-v1", capture));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            Reachable(restored.Root).Count() != native.Length)
            throw new InvalidOperationException("The connected archive must restore its native participants.");
        using (var file = File.Create(Path.Combine(output, "communication.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        AblationOptionModel Option(CommunicationHintAblation.Option option) => new(option.HintCount,
            option.HintParticipation, option.Admissible.Count, option.IsRobust, Clauses(option.Message));
        var model = new
        {
            Schema = "communication-loop-v1", Cases = cases, LibrarySize = study.Library.Realizations.Count,
            Ablations = ablation.Cases.Select(item => new AblationModel(item.Source.Id, item.Options.Count,
                item.Best.Count == 0 ? -1 : item.Best[0].HintCount,
                item.Best.Select(Option).ToArray(), item.Options.Select(Option).ToArray())).ToArray(),
            NativeElements = native.Length, NativeNumericElements = numeric, ArchiveRoundTrip = true,
            BeforeSatisfied = cases.Sum(item => item.Trials.Count(trial => trial.Before.Read.Satisfied)),
            AfterSatisfied = cases.Sum(item => item.Trials.Count(trial => trial.After.Read.Satisfied)),
            Trials = cases.Sum(item => item.Trials.Length),
            Limits = new[] {
                "The receiver samples a finite executable library through an exposed preference order. This round does not train a model or synthesize arbitrary new programs.",
                "Each sample is executed through acknowledged native drawing flow. The critic independently republishes and induces patterns from the selected output.",
                "Reader inputs contain fresh sites, segments, coarse headings and actual incidence. Sender requirements, recipes, labels and rendering are outside that boundary.",
                "A required pattern asks for an occurrence, not exact whole-output identity. Omitted properties remain unspecified; no probability is implied.",
                "Absence requires completed search within the declared finite motif grammar. Exhausted or unsupported questions remain unresolved.",
                "The reader does not establish metric squares, empty interiors, equal lengths, physical transfer laws, visual occlusion or general ornament recognition.",
                "Numeric counters and screen coordinates are diagnostics and presentation. The connected experiment archive contains no numeric carriers.",
                "Shared engine/library cost and reconstruction work matter alongside message size. Sparse hints are not an end-to-end compression or speed result."
            }
        };
        var json = JsonSerializer.Serialize(model, JsonOptions);
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview(cases));
        Console.WriteLine($"Saved {model.Trials} trials: {model.BeforeSatisfied} -> {model.AfterSatisfied} satisfy sender commitments; " +
            $"{native.Length:N0} native participants; {numeric} numeric. Archive restoration verified.");
        Console.WriteLine(Path.GetFullPath(output));
        return 0;
    }

    private static int Hints(CommunicationMessage message) => E(message.Required).Length + E(message.Forbidden).Length;
    private static string[] Clauses(CommunicationMessage message) => E(message.Required).Select(pattern => Label(pattern, false))
        .Concat(E(message.Forbidden).Select(pattern => Label(pattern, true))).ToArray();
    private static string Label(IElement pattern, bool forbidden)
    {
        if (pattern is not Relation row || !CommunicationShape.TryRead(row, out var shape)) return "Unresolved pattern";
        var label = shape.Centres.Length == 1 ? shape.Edges.Length == 1
            ? "Endpoint facing " + string.Join(" + ", new[] { "right", "left", "up", "down" }
                .Where((_, index) => (shape.Headings[0] & (1 << index)) != 0))
            : "Shared site with " + shape.Edges.Length + " outgoing segments"
            : (shape.IsCycle ? "Closed route with " : "Ordered walk with ") + shape.Edges.Length + " segments";
        return (forbidden ? "Forbid: " : "Require: ") + label;
    }
    private sealed record Point(double X, double Y);
    private static string Svg(QualitativeMotionFrame frame, QualitativeStrokeGraph graph, Relation bindings, string title)
    {
        var positions = E(bindings).Cast<Relation>().ToDictionary(binding => binding.Left,
            binding => Position(frame, (Relation)binding.Right), (IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
        var minX = positions.Values.Min(p => p.X); var maxX = positions.Values.Max(p => p.X);
        var minY = positions.Values.Min(p => p.Y); var maxY = positions.Values.Max(p => p.Y);
        var scale = Math.Min(430 / Math.Max(1, maxX - minX), 245 / Math.Max(1, maxY - minY));
        Point P(Point p) => new(260 + (p.X - (minX + maxX) / 2) * scale, 165 - (p.Y - (minY + maxY) / 2) * scale);
        var strokes = E(graph.Strokes).Cast<Relation>().Select(stroke =>
        { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); return (a, b); }).ToArray();
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='520' height='330' viewBox='0 0 520 330' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#fbf8ef'/>");
        foreach (var (from, to) in strokes)
        {
            var a = P(positions[from]); var b = P(positions[to]);
            svg.Append($"<line class='observed' x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='#28616a' stroke-width='6' stroke-linecap='round'/>");
        }
        foreach (var pair in positions)
        {
            var degree = strokes.Count(stroke => ReferenceEquals(stroke.a, pair.Key) || ReferenceEquals(stroke.b, pair.Key));
            var p = P(pair.Value); var joined = degree >= 2;
            svg.Append($"<circle class='{(joined ? "actual-join" : "endpoint")}' cx='{F(p.X)}' cy='{F(p.Y)}' r='{(joined ? "5.5" : "3.5")}' fill='{(joined ? "#b76b45" : "#fbf8ef")}' stroke='{(joined ? "#b76b45" : "#28616a")}' stroke-width='2'><title>{degree} actual incident segments</title></circle>");
        }
        return svg.Append("</svg>").ToString();
    }
    private static Point Position(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0d; var y = 0d;
        foreach (var move in E(word).Cast<Relation>())
        {
            if (!frame.Direction(move, out var axis, out var forward, out _)) throw new InvalidOperationException("Unknown display direction.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1;
            else if (ReferenceEquals(axis, frame.Vertical)) y += forward ? 1 : -1;
            else throw new InvalidOperationException("No display interpretation for this axis.");
        }
        return new(x, y);
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item)) if (seen.Add(item))
        { yield return item; if (item is Relation pair) { stack.Push(pair.Left); stack.Push(pair.Right); } }
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double number) => number.ToString("0.###", CultureInfo.InvariantCulture);
}
