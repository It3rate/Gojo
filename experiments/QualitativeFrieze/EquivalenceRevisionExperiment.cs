using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation after native selection, execution and independent
/// output reads. No screen coordinates feed the equivalence assessments.</summary>
public static partial class EquivalenceRevisionExperiment
{
    private sealed record ValueModel(string Role, string Word);
    private sealed record PublicationModel(string Source, string Revision, bool Working, ValueModel[] Values);
    private sealed record CandidateModel(string Id, string Name, bool Chosen, bool Eligible, bool Pareto,
        int Parameters, int NativeCost, int Supported, int Refuted, int Open, int Blocked, int Conflicts,
        int WorkingRefuted, int SampleRefuted, int WorkingSampleRefuted, int Retained, int Withdrawn);
    private sealed record StageModel(string Id, string Name, string Description, string? Image, string? Chosen,
        bool CoarseClosed, int Cycles, int Eligible, int Pareto, int Carried, int Withdrawn,
        bool LowerResolution, bool BlockedExact, int Conflicts, string WorkingScope,
        ValueModel[] SampleValues, PublicationModel[] Publications, CandidateModel[] Candidates);
    private sealed record CaseModel(string Id, string Name, string Description, StageModel[] Stages, bool HasNextMessage);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "equivalence-revision");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --equivalence-revision [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study = EquivalenceRevisionStudy.Build();
        var cases = new List<CaseModel>();
        foreach (var item in study.Cases)
        {
            var domain = item.Stages.SelectMany(stage => stage.Selection.Candidates).First().Hypothesis.Domain;
            var roles = E(domain);
            string Role(IElement role)
            {
                var index = Array.FindIndex(roles, actual => ReferenceEquals(actual, role));
                return index < 0 ? "Foreign role" : "Cell " + (index / 2 + 1) + (index % 2 == 0 ? " · horizontal" : " · vertical");
            }
            ValueModel[] Values(Relation values) => E(values).OfType<Relation>().Select(row => new ValueModel(Role(row.Left),
                row.Right is Relation word ? WordLabel(word) : "Unresolved")).ToArray();
            var sources = item.Stages.SelectMany(stage => stage.Publications).Select(pub => pub.Source)
                .Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            var revisions = item.Stages.SelectMany(stage => stage.Publications).Select(pub => pub.Revision)
                .Distinct<IElement>(ReferenceEqualityComparer.Instance).ToArray();
            string Source(IElement source) => "Supplier " + (Array.FindIndex(sources, actual => ReferenceEquals(actual, source)) + 1);
            var stages = new List<StageModel>();
            foreach (var stage in item.Stages)
            {
                string? image = null;
                if (stage.Output is { } realized)
                {
                    image = stage.Id + ".svg";
                    File.WriteAllText(Path.Combine(output, image), Svg(realized.Frame, realized.Graph, realized.Bindings, item.Name + " / " + stage.Name));
                }
                var candidates = stage.Selection.Candidates.Select(read =>
                {
                    var form = study.Forms.Single(form => ReferenceEquals(form.Hypothesis, read.Hypothesis));
                    return new CandidateModel(form.Id, form.Name, ReferenceEquals(stage.Selection.Chosen, read.Hypothesis),
                        read.IsEligible, stage.Selection.Pareto.Any(actual => ReferenceEquals(actual, read)), read.ParameterCount,
                        read.NativeCost, E(read.Support).Length, E(read.Refuted).Length, E(read.Open).Length,
                        E(read.Blocked).Length, E(read.Conflicts).Length, E(read.InScopeRefuted).Length,
                        E(read.SampleRefuted).Length, E(read.InScopeSampleRefuted).Length,
                        E(read.Retained).Length, E(read.Withdrawn).Length);
                }).ToArray();
                var publications = stage.Publications.Select(pub => new PublicationModel(Source(pub.Source),
                    "Publication " + (Array.FindIndex(revisions, actual => ReferenceEquals(actual, pub.Revision)) + 1),
                    stage.Context.WorkingSources is null || E(stage.Context.WorkingSources).Any(source => ReferenceEquals(source, pub.Source)), Values(pub.Values))).ToArray();
                var working = stage.Context.WorkingSources is null ? "All published sources"
                    : E(stage.Context.WorkingSources).Length == 0 ? "Topology only · fine span reports carried outside this read"
                    : string.Join(", ", E(stage.Context.WorkingSources).Select(Source));
                stages.Add(new(stage.Id, stage.Name, stage.Description, image, stage.Output?.Name,
                    stage.CoarseAssessment?.IsSatisfied ?? false, stage.Output?.CycleCount ?? 0,
                    stage.Selection.Eligible.Count, stage.Selection.Pareto.Count, E(stage.Carried).Length,
                    E(stage.Withdrawn).Length, stage.LowerResolution, HasParticipation(stage.BlockedExact),
                    candidates.Select(candidate => candidate.Conflicts).DefaultIfEmpty().Max(), working,
                    stage.Output is null ? [] : Values(stage.Output.SpanValues), publications, candidates));
                Console.WriteLine($"{stage.Id}: {stage.Output?.Name ?? "no admissible output"}; " +
                    $"{stage.Selection.Eligible.Count} eligible, {stages[^1].Conflicts} conflict witnesses; " +
                    $"{stages[^1].Carried} assumptions carried, {stages[^1].Withdrawn} withdrawn.");
            }
            cases.Add(new(item.Id, item.Name, item.Description, stages.ToArray(), HasParticipation(item.NextMessage)));
        }
        var native = Reachable(study.Surface).ToArray();
        var numeric = native.Count(participant => participant is Carrier or CarrierLocation);
        if (numeric != 0) throw new InvalidOperationException("Provisional equivalence must retain an unmeasured native basis.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("equivalence-revision-v1", study.Surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) ||
            Reachable(restored.Root).Count() != native.Length) throw new InvalidOperationException("The native evidence archive must restore its participants.");
        using (var file = File.Create(Path.Combine(output, "equivalence.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var model = new
        {
            Schema = "equivalence-revision-v1", Cases = cases, Stages = cases.Sum(item => item.Stages.Length),
            NativeElements = native.Length, NativeNumericElements = numeric, ArchiveRoundTrip = true,
            Limits = new[] {
                "This is a finite candidate-selection and revision fixture, with authored publication and context schedules. No statistical model has been trained.",
                "Native comparisons concern repeated positive participation in an explicitly shared unmeasured basis. They do not prove Euclidean square geometry or physical cross-axis calibration.",
                "Published source values, proposed equalities and realized sample values remain separate. A generated equal-span sample is not new evidence that the source has equal spans.",
                "An explicit working-source scope permits a conditional choice while all conflicting reports remain visible. Choosing a route does not reconcile the suppliers.",
                "A topology-only answer carries the blocked finer request. Its square-like display is one coarse sample, not a certification of source proportions.",
                "Selection uses capability and source conformance, then separate parameter/program complexity axes and an explicit preference. It is not a universal simplicity or aesthetic law.",
                "Fresh acknowledged output is inspected through a history-blind topology publication and a separately exposed actual-stroke participation read. Rendering feeds neither reader."
            }
        };
        var json = JsonSerializer.Serialize(model, JsonOptions);
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview(cases));
        Console.WriteLine($"Saved {cases.Count} cases, {model.Stages} stages; {native.Length:N0} native participants, {numeric} numeric. Archive restored.");
        Console.WriteLine(Path.GetFullPath(output)); return 0;
    }

    private static string WordLabel(Relation word)
    {
        if (!Gojo.Patterns.StructuralConstraint.TryReadScope(word, out var terms, out var seam, out var tail)) return "Unreadable word";
        var closed = ReferenceEquals(seam, tail);
        return terms.Length == 0 ? closed ? "empty" : "unspecified"
            : string.Join(" · ", terms.Select(_ => "u")) + (closed ? "" : " … open");
    }
    private sealed record Point(double X, double Y);
    internal static string Svg(QualitativeMotionFrame frame, QualitativeStrokeGraph graph, Relation bindings, string title)
    {
        var positions = E(bindings).Cast<Relation>().ToDictionary(binding => binding.Left, binding => Position(frame, (Relation)binding.Right),
            (IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
        var minX = positions.Values.Min(p => p.X); var maxX = positions.Values.Max(p => p.X);
        var minY = positions.Values.Min(p => p.Y); var maxY = positions.Values.Max(p => p.Y);
        var scale = Math.Min(500 / Math.Max(1, maxX - minX), 190 / Math.Max(1, maxY - minY));
        Point P(Point p) => new(310 + (p.X - (minX + maxX) / 2) * scale, 140 - (p.Y - (minY + maxY) / 2) * scale);
        var strokes = E(graph.Strokes).Cast<Relation>().Select(stroke =>
        { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); return (a, b); }).ToArray();
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='620' height='280' viewBox='0 0 620 280' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#fbf8ef'/>");
        foreach (var (from, to) in strokes)
        {
            var a = P(positions[from]); var b = P(positions[to]);
            svg.Append($"<line class='observed' x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='#28616a' stroke-width='5' stroke-linecap='round'/>");
        }
        foreach (var pair in positions)
        {
            var degree = strokes.Count(stroke => ReferenceEquals(stroke.a, pair.Key) || ReferenceEquals(stroke.b, pair.Key));
            var p = P(pair.Value);
            svg.Append($"<circle class='actual-site' cx='{F(p.X)}' cy='{F(p.Y)}' r='4.5' fill='#b76b45'><title>{degree} actual incident segments</title></circle>");
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
        { yield return item; if (item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); } }
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static bool HasParticipation(Relation value) => !Gojo.Patterns.StructuralConstraint.TryReadScope(value,
        out var entries, out var seam, out var tail) || entries.Length != 0 || !ReferenceEquals(seam, tail);
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double number) => number.ToString("0.###", CultureInfo.InvariantCulture);
}
