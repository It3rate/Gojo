using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Display only: native topology, projected support and speculative
/// continuations have separate pictures and separately qualified query counts.</summary>
public static partial class JunctionInterpretationExperiment
{
    private sealed record Point(double X, double Y);
    private sealed record Stroke(IElement Identity, IElement From, IElement To, Point A, Point B);
    private sealed record SourceFacts(int Sites, int Strokes, int JoinedSites, int StrokePairConnections,
        int Components, int Endpoints, int L, int T, int Y, int X);
    private sealed record InferenceFacts(int ProjectedCrossings, int StrictCrossings, int ProjectedContacts,
        int FacingGaps, int Candidates, int Blocked, int Open);
    private sealed record CandidateModel(string Id, string Name, string Image, int BridgeCount, int CutCount,
        int DepthCount, int ObligationCount, int VirtualComponents, bool Open);
    private sealed record CaseModel(string Id, string Name, string Description, string ComparisonGroup,
        string Source, string Actual, string Cues, SourceFacts SourceFacts, InferenceFacts InferenceFacts,
        CandidateModel[] Candidates);
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static int Run(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "junction-interpretations");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --junction-interpretations [--output DIRECTORY]");
        Directory.CreateDirectory(output);
        var study = JunctionInterpretationStudy.Build();
        var models = new List<CaseModel>();
        foreach (var item in study.Cases)
        {
            var summary = item.Evidence.Describe(); var actual = item.Graph.Describe(item.Graph.Evaluate());
            item.Evidence.TryReadResult(item.Read, out _, out var crossings, out _, out var candidates, out _, out _);
            var positions = Positions(item);
            var strokes = E(item.Graph.Strokes).Cast<Relation>().Select(stroke =>
            {
                QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _);
                return new Stroke(stroke, a, b, positions[a], positions[b]);
            }).ToArray();
            var projectionSites = E(crossings).Cast<Relation>().Select(cue =>
            {
                item.Evidence.TryReadCrossing(cue, out _, out var word, out _); return Word(item.Frame, word);
            }).ToArray();
            var source = item.Id + "-source.svg"; var topology = item.Id + "-actual.svg"; var cues = item.Id + "-cues.svg";
            File.WriteAllText(Path.Combine(output, source), Svg(item.Name, strokes, positions, [], [], false));
            File.WriteAllText(Path.Combine(output, topology), Svg(item.Name + " / actual topology", strokes, positions, [], [], true));
            File.WriteAllText(Path.Combine(output, cues), Svg(item.Name + " / projected cues", strokes, positions, projectionSites, [], false));
            var hypotheses = new List<CandidateModel>();
            foreach (var (candidate, index) in E(candidates).Cast<Relation>().Select((candidate, index) => (candidate, index)))
            {
                if (!item.Evidence.TryReadCandidate(candidate, out _, out var bridges, out var cuts, out var depth, out var obligations))
                    throw new InvalidOperationException("A displayed hypothesis must retain its actual native candidate.");
                var additions = E(bridges).Cast<Relation>().ToArray();
                var lines = additions.Select(link => new Stroke(link, link.Left, link.Right, positions[link.Left], positions[link.Right])).ToArray();
                var foreground = E(depth).Cast<Relation>().Select(order => strokes.FirstOrDefault(stroke => ReferenceEquals(stroke.Identity, order.Left)))
                    .OfType<Stroke>().ToArray();
                var image = item.Id + "-H" + (index + 1) + ".svg";
                File.WriteAllText(Path.Combine(output, image), Svg(item.Name + " / candidate " + (index + 1),
                    strokes, positions, projectionSites, lines, false, foreground));
                Gojo.Patterns.StructuralConstraint.TryReadScope(obligations, out var pending, out var seam, out var tail);
                var open = !seam.Equals(tail) || pending.Length != 0;
                var name = additions.Length != 0 ? "Propose a hidden continuation" : E(cuts).Length != 0 ? "Retain separate cut endpoints" : "Retain separate strands; propose a depth order";
                hypotheses.Add(new(item.Id + "-H" + (index + 1), name, image, additions.Length,
                    E(cuts).Length, E(depth).Length, pending.Length, Components(item.Graph, additions), open));
            }
            models.Add(new(item.Id, item.Name, item.Description, item.ComparisonGroup, source, topology, cues,
                new(actual.Sites, actual.Strokes, summary.ActualJoinedSites, summary.ActualStrokePairConnections,
                    summary.ActualComponents, actual.Endpoints, actual.L, actual.T, actual.Y, actual.X),
                new(summary.ProjectedCrossings, summary.StrictCrossings, summary.ProjectedContacts,
                    summary.FacingGaps, summary.Candidates, summary.Blocked, summary.Open), hypotheses.ToArray()));
            Console.WriteLine($"{item.Id}: {summary.ActualComponents} source components, {summary.ActualJoinedSites} joined sites; {summary.ProjectedCrossings} projected intersections, {summary.FacingGaps} gaps, {summary.Candidates} candidates; {summary.Open} open evidence.");
        }
        var native = Reachable(study.Surface).ToArray();
        var numeric = native.Count(value => value is Carrier or CarrierLocation);
        if (numeric != 0) throw new InvalidOperationException("The native junction source and reads must remain unmeasured.");
        var archive = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("junction-interpretations-v1", study.Surface));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(archive), out var restored) || Reachable(restored.Root).Count() != native.Length)
            throw new InvalidOperationException("The native archive must restore its connected participants.");
        using (var file = File.Create(Path.Combine(output, "junctions.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(archive);
        var model = new
        {
            Schema = "junction-interpretations-v1", Cases = models, NativeElements = native.Length,
            NativeNumericElements = numeric, ArchiveRoundTrip = true,
            Limits = new[] {
                "Sources are authored native stroke observations, not parsed photographs or generated-recipe metadata.",
                "Shared sites, formal projected intersections, continuation proposals, depth-order proposals and legal material transfer are separate reads.",
                "The projected-intersection reader is bounded to declared axis-aligned intervals. Unsupported angular intersections and unresolved formal orders remain open.",
                "Every continuation or depth proposal retains open obligations. No candidate installs a junction or modifies the observed source.",
                "Component counts under a continuation proposal describe an observational grouping, not accepted physical connectivity or a transfer route.",
                "T/Y/X/L summaries are native ray-incidence descriptions. L does not establish a right angle, and T does not establish absorption.",
                "Screen angles, equal illustrative lengths, line weight and colour are renderer choices. They are not input to the inference reader.",
                "Glyph recognition, inferred object ancestry, automatic junction commitment, learned salience and general weave reconstruction remain untested."
            }
        };
        var json = JsonSerializer.Serialize(model, Options);
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html(json));
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview(models));
        Console.WriteLine($"Saved {models.Count} cases, {models.Sum(item => item.Candidates.Length)} candidate readings; {numeric} numeric participants. Native archive round-trip verified.");
        Console.WriteLine(Path.GetFullPath(output));
        return 0;
    }

    private static int Components(QualitativeStrokeGraph graph, Relation[] additions)
    {
        var unseen = new HashSet<IElement>(E(graph.Sites), ReferenceEqualityComparer.Instance);
        var links = E(graph.Strokes).Cast<Relation>().Select(stroke =>
        { QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var a, out var b, out _); return (a, b); })
            .Concat(additions.Select(link => (link.Left, link.Right))).ToArray();
        var result = 0;
        while (unseen.Count > 0)
        {
            var queue = new Queue<IElement>(); var first = unseen.First(); unseen.Remove(first); queue.Enqueue(first); result++;
            while (queue.TryDequeue(out var site)) foreach (var (a, b) in links)
            {
                if (ReferenceEquals(site, a) && unseen.Remove(b)) queue.Enqueue(b);
                if (ReferenceEquals(site, b) && unseen.Remove(a)) queue.Enqueue(a);
            }
        }
        return result;
    }
    private static Dictionary<IElement, Point> Positions(JunctionInterpretationStudy.Case item) =>
        E(item.Bindings).Cast<Relation>().ToDictionary(binding => binding.Left, binding => Word(item.Frame, (Relation)binding.Right),
            (IEqualityComparer<IElement>)ReferenceEqualityComparer.Instance);
    private static Point Word(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0d; var y = 0d;
        foreach (var move in E(word).Cast<Relation>())
        {
            if (!frame.Direction(move, out var axis, out var face, out _)) throw new InvalidOperationException("The renderer needs a declared word direction.");
            if (ReferenceEquals(axis, frame.Horizontal)) x += face ? 1 : -1;
            else if (ReferenceEquals(axis, frame.Vertical)) y += face ? 1 : -1;
            else throw new InvalidOperationException("The renderer has no interpretation for this axis.");
        }
        return new(x, y);
    }
    private static string Svg(string title, Stroke[] strokes, Dictionary<IElement, Point> positions,
        Point[] projected, Stroke[] proposals, bool actual, Stroke[]? foreground = null)
    {
        const double width = 600, height = 330, scale = 49;
        Point P(Point point) => new(width / 2 + point.X * scale, height / 2 - point.Y * scale);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='600' height='330' viewBox='0 0 600 330' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='100%' height='100%' fill='#fbf8ef'/>");
        foreach (var stroke in proposals)
        {
            var a = P(stroke.A); var b = P(stroke.B);
            svg.Append($"<line class='proposed-continuation' x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='#a45c85' stroke-width='4' stroke-dasharray='7 6'><title>Proposed continuation only; source remains unchanged</title></line>");
        }
        foreach (var stroke in strokes)
        {
            var a = P(stroke.A); var b = P(stroke.B);
            svg.Append($"<line class='observed' x1='{F(a.X)}' y1='{F(a.Y)}' x2='{F(b.X)}' y2='{F(b.Y)}' stroke='#285d66' stroke-width='5' stroke-linecap='round'/>");
        }
        if (actual) foreach (var pair in positions)
        {
            var degree = strokes.Count(stroke => ReferenceEquals(stroke.From, pair.Key) || ReferenceEquals(stroke.To, pair.Key));
            var p = P(pair.Value); var joined = degree >= 2;
            svg.Append($"<circle class='{(joined ? "actual-join" : "endpoint")}' cx='{F(p.X)}' cy='{F(p.Y)}' r='{(joined ? "6" : "3.6")}' fill='{(joined ? "#b35d3a" : "#fbf8ef")}' stroke='{(joined ? "#b35d3a" : "#285d66")}' stroke-width='2'><title>{(joined ? "Actual shared site" : "Actual endpoint site")} · {degree} incident strokes</title></circle>");
        }
        foreach (var point in projected)
        {
            var p = P(point);
            svg.Append($"<circle class='projected-cue' cx='{F(p.X)}' cy='{F(p.Y)}' r='11' fill='none' stroke='#c49835' stroke-width='2.3' stroke-dasharray='3 3'><title>Formal projected support; no site is created</title></circle>");
        }
        foreach (var stroke in foreground ?? [])
        {
            var a = P(stroke.A); var b = P(stroke.B);
            svg.Append($"<path d='M{F(a.X)} {F(a.Y)}L{F(b.X)} {F(b.Y)}' fill='none' stroke='#fbf8ef' stroke-width='12'/><path class='proposed-foreground' d='M{F(a.X)} {F(a.Y)}L{F(b.X)} {F(b.Y)}' fill='none' stroke='#b97635' stroke-width='5'><title>Proposed front/back order, still unresolved</title></path>");
        }
        return svg.Append("</svg>").ToString();
    }
    private static IEnumerable<IElement> Reachable(IElement root)
    {
        var visited = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var stack = new Stack<IElement>(); stack.Push(root);
        while (stack.TryPop(out var item))
        { if (!visited.Add(item)) continue; yield return item; if (item is Relation relation) { stack.Push(relation.Left); stack.Push(relation.Right); } }
    }
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
