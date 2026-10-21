using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.IO.Compression;
using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using C = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningCorpus;
using M = Gojo.Experiments.QualitativeFrieze.Learning.OrnamentLearningMemory;

namespace Gojo.Experiments.QualitativeFrieze.Learning;

/// <summary>A measured display adapter over the actual native training and
/// receiver surfaces. Render coordinates never return to training, matching or
/// feature discovery. Candidate colours describe compatibility, not Yes/No.</summary>
public static class OrnamentLearningGallery
{
    public sealed record Example(string Id, string Split, string Note, C.Sample Sample,
        IReadOnlyList<M.Prediction> Predictions, M.Realization? Generated);

    public static void Write(string output, object summary, IReadOnlyList<Example> examples)
    {
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var rows = new List<object>(); var tiles = new StringBuilder();
        foreach (var example in examples)
        {
            var token = Token(example.Id); var sample = example.Sample;
            string Save(string suffix, string svg)
            {
                var relative = $"patterns/{token}-{suffix}.svg";
                File.WriteAllText(Path.Combine(output, relative), svg); return relative;
            }
            var teacher = Save("teacher", SourceSvg(sample.Target, "Teacher: complete unit"));
            var pattern = Save("pattern", SourceSvg(sample.Pattern, "Teacher: repeated arrangement"));
            var streams = sample.Streams.Select((stream, index) => new
            {
                name = stream.Name, image = Save("stream-" + index, SourceSvg(stream.Source, "Receiver: " + stream.Name)),
                passages = E(stream.Source.Graph.Strokes).Length, bindings = E(stream.Source.Bindings).Length,
                passageInventory = Closed(stream.Source.Graph.Strokes) ? "closed inventory" : "open inventory",
                bindingInventory = Closed(stream.Source.Bindings) ? "closed inventory" : "open inventory"
            }).ToArray();
            var selected = example.Predictions.Where(row => row.IsAdmissible).Take(3)
                .Concat(example.Predictions.Where(row => !row.IsAdmissible).Take(1)).ToArray();
            var candidates = selected.Select((prediction, index) => new
            {
                image = Save("candidate-" + index, PatternSvg(prediction.Prototype.Pattern, "Learned native candidate")),
                score = prediction.Score, share = prediction.Share, compatible = prediction.IsAdmissible,
                support = prediction.Prototype.Support, sides = prediction.Prototype.Pattern.Words.Count,
                matched = E(prediction.Supported).Length, contradicted = E(prediction.Contradicted).Length,
                open = E(prediction.Open).Length, shapeKey = prediction.Prototype.ShapeKey
            }).ToArray();
            var generated = example.Generated is null ? null : Save("generated", SourceSvg(example.Generated.Source, "Executed learned native program"));
            rows.Add(new { id = example.Id, split = example.Split, note = example.Note,
                group = sample.Descriptor.Group, variant = sample.Descriptor.Variant,
                teacher, pattern, streams, candidates, generated,
                candidateCount = example.Predictions.Count, compatibleCount = example.Predictions.Count(row => row.IsAdmissible) });
            tiles.Append($"<a class='tile' href='gallery.html?id={Uri.EscapeDataString(example.Id)}'><h2>{H(example.Id)} · {H(example.Split)}</h2><img src='{teacher}' alt='Complete teacher unit'><p>{H(example.Note)}</p></a>");
        }
        var data = JsonSerializer.Serialize(new { schema = "ornament-learning-gallery-v1", summary, examples = rows }, Json);
        var measurements = SummaryMarkup(JsonSerializer.SerializeToElement(summary, Json));
        File.WriteAllText(Path.Combine(output, "gallery-data.json"), data);
        // Keep receiver-only native explanations. Teacher recipes and their
        // private audit complement are not reachable from these episode roots.
        var episodes = examples.Select(example => {
            var observed = StructuralConstraint.Closed(example.Sample.Streams.Select(stream => (IElement)stream.Surface).ToArray());
            var proposals = StructuralConstraint.Closed(example.Predictions.Select(prediction => (IElement)prediction.Surface).ToArray());
            var selected = example.Predictions.FirstOrDefault(prediction => prediction.IsAdmissible);
            IElement result = example.Generated is null || selected is null ? StructuralConstraint.Open() : new Relation(selected.Surface, example.Generated.Surface);
            return (IElement)new Relation(observed, new Relation(proposals, result));
        }).ToArray();
        var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("ornament-learning-retained-receiver-episodes-v1", StructuralConstraint.Closed(episodes)));
        using (var file = File.Create(Path.Combine(output, "retained-episodes.gojo.json.gz")))
        using (var zip = new GZipStream(file, CompressionLevel.SmallestSize))
        using (var writer = new StreamWriter(zip)) writer.Write(packet);
        WritePages(output, data, measurements, tiles.ToString());
    }

    /// <summary>Rebuilds presentation from retained run data and existing SVGs.
    /// No model, training data or native geometry is regenerated.</summary>
    public static void RefreshPages(string output)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "gallery-data.json")));
        var root = document.RootElement;
        if (!root.TryGetProperty("schema", out var schema) || schema.GetString() != "ornament-learning-gallery-v1" ||
            !root.TryGetProperty("examples", out var examples) || examples.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("summary", out var summary))
            throw new InvalidOperationException("This is not retained ornament learning gallery data.");
        var tiles = new StringBuilder();
        foreach (var row in examples.EnumerateArray())
        {
            var id = row.GetProperty("id").GetString() ?? ""; var split = row.GetProperty("split").GetString() ?? "";
            var teacher = row.GetProperty("teacher").GetString() ?? ""; var note = row.GetProperty("note").GetString() ?? "";
            tiles.Append($"<a class='tile' href='gallery.html?id={Uri.EscapeDataString(id)}'><h2>{H(id)} · {H(split)}</h2><img src='{H(teacher)}' alt='Complete teacher unit'><p>{H(note)}</p></a>");
        }
        WritePages(output, root.GetRawText(), SummaryMarkup(summary), tiles.ToString());
    }

    private static void WritePages(string output, string data, string measurements, string tiles)
    {
        File.WriteAllText(Path.Combine(output, "gallery.html"), Page("Native ornament learning", """
<header><p class="eyebrow">Gojo · experimental native grammar</p><h1>Learning from ornamental structure</h1>
<p>Partial streams enter the receiver. Complete teacher units are withheld until assessment. Learned candidates remain proposals; the final image executes a stored native route through the drawing engine.</p>
<nav><a href="overview.html">All inspected cases</a><a href="summary.json">Run measurements</a><a href="gallery-data.json">Gallery data</a></nav></header>
SUMMARY
<section class="chooser"><label for="case">Inspected case</label><select id="case"></select><span id="partition"></span></section>
<main id="content"></main>
<script>
globalThis.ORNAMENT_LEARNING = DATA;
const data=globalThis.ORNAMENT_LEARNING, choice=document.querySelector('#case'), content=document.querySelector('#content');
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const picture=(src,caption)=>`<figure><img src="${esc(src)}" alt="${esc(caption)}"><figcaption>${esc(caption)}</figcaption></figure>`;
for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=`${row.id} · ${row.split} · ${row.group}`;choice.append(option);}
function show(){const row=data.examples.find(x=>x.id===choice.value)??data.examples[0];if(!row){content.innerHTML='<p>No inspected cases in this run.</p>';return;}
 choice.value=row.id;document.querySelector('#partition').textContent=`${row.split} · variant ${row.variant}`;
 const url=new URL(location.href);url.searchParams.set('id',row.id);history.replaceState(null,'',url);
 content.innerHTML=`<h2>${esc(row.id)}</h2><p class="note">${esc(row.note)}</p>
 <section><h2>What the receiver receives</h2><p>These are native passage and landmark streams. Open inventories withhold participation; absent strokes are not claims that no stroke exists. Equal site identities permit evidence to meet across streams. Routes without an exposed position anchor use a local schematic origin in the preview; their placement remains open.</p><div class="grid">${row.streams.map(x=>`<article>${picture(x.image,x.name)}<p>${x.passages} offered passages · ${x.bindings} binding replies</p><p class="muted">${esc(x.passageInventory)} · ${esc(x.bindingInventory)}</p></article>`).join('')}</div></section>
 <section><h2>Learned alternatives</h2><p>${row.candidateCount} candidates retained for this inspection: up to three compatible proposals and one conflicting proposal. This is a display subset of the ranked vocabulary. Scores and ranking shares are selection heuristics, not calibrated probabilities. Shared hints may support several concepts.</p><div class="grid">${row.candidates.length?row.candidates.map((x,i)=>`<article class="candidate ${x.compatible?'compatible':'conflicting'}"><span class="badge">${x.compatible?'Compatible proposal':'Conflict retained'}</span>${picture(x.image,`Candidate ${i+1} · ${x.sides} native sides`)}<p>Score ${Number(x.score).toFixed(3)} · ranking share ${Number(x.share).toFixed(3)}</p><p>${x.support} training episodes · ${x.matched} matched route accounts · ${x.contradicted} contradictions · ${x.open} open accounts</p><details><summary>Coarse native heading key</summary><code>${esc(x.shapeKey)}</code></details></article>`).join(''):'<p>No retained candidate at the requested capacity.</p>'}</div></section>
 <section><h2>A native sample of a proposal</h2><p>The selected stored chord words run through the existing native drawing engine. This adds specifics to the incomplete communication. It does not turn the completion into an observed fact.</p>${row.generated?picture(row.generated,'Executed learned route'):'<p>No admissible generated example was retained.</p>'}</section>
 <section class="teacher"><h2>Teacher comparison · withheld from the receiver</h2><p>The teacher has the original complete unit and its repeated disposition. Matching is structural rather than pixel-to-pixel; a valid alternative may have different proportions. This first learner learns local boundary routes, not the entire arrangement.</p><div class="grid">${picture(row.teacher,'Complete teacher unit')}${picture(row.pattern,'Repeated teacher arrangement')}</div></section>
 <aside><strong>Display convention.</strong> All exposed uncalibrated reaches use one visual step here. Geometry, colour and these numeric display coordinates are never recognition evidence. Coarse heading matching deliberately leaves dimensions and face ownership unresolved.</aside>`;
}
choice.addEventListener('change',show);const requested=new URL(location.href).searchParams.get('id');choice.value=data.examples.some(x=>x.id===requested)?requested:(data.examples[0]?.id??'');show();
</script>
""".Replace("SUMMARY", measurements, StringComparison.Ordinal).Replace("DATA", data, StringComparison.Ordinal)));
        File.WriteAllText(Path.Combine(output, "overview.html"), Page("Native ornament learning · inspected cases",
            "<header><p class='eyebrow'>Gojo · native grammar</p><h1>Inspected learning cases</h1><p>Teacher thumbnails identify the cases for human inspection. The learner only receives the partial streams shown on each case page.</p><nav><a href='gallery.html'>Detailed inspection</a><a href='summary.json'>Run measurements</a></nav></header>" + measurements + "<main class='tiles'>" + tiles + "</main>"));
    }

    private static string SummaryMarkup(JsonElement summary)
    {
        string Value(JsonElement parent, string field) => parent.TryGetProperty(field, out var item) ? item.ValueKind switch
        { JsonValueKind.Number => item.GetDouble().ToString("0.###", CultureInfo.InvariantCulture), JsonValueKind.String => H(item.GetString() ?? ""), _ => H(item.ToString()) } : "—";
        string Ratio(JsonElement row, string numerator, string denominator) => Value(row, numerator) + " / " + Value(row, denominator);
        var text = new StringBuilder("<section class='run-summary'><h2>Run measurements</h2><dl class='metrics'>");
        foreach (var (field, label) in new[] { ("count", "Generated descriptors"), ("requestedTrainRows", "Requested training rows"),
            ("usableTrainRows", "Rows with a usable training boundary"), ("trainBoundaries", "Discovered training boundaries"),
            ("prototypes", "Learned native routes"), ("coarseRouteTypes", "Coarse heading types"), ("fragments", "Mined subroutes") })
            text.Append($"<div><dt>{label}</dt><dd>{Value(summary, field)}</dd></div>");
        text.Append($"</dl><p class='muted'>Partition protocol: {Value(summary, "splitProtocol")}. Teacher agreement measures the held-back target preference; another compatible proposal can still be valid.</p>");
        if (summary.TryGetProperty("partitions", out var partitions) && partitions.ValueKind == JsonValueKind.Array)
        {
            text.Append("<div class='table-scroll'><table><caption>Agreement counts / evaluated rows containing a closed teacher boundary</caption><thead><tr><th>Partition</th><th>Evaluated rows</th><th>Closed teacher rows</th><th>Learned top 1</th><th>Learned top 3</th><th>First stream top 1</th><th>Uniform top 1</th><th>Frequency top 1</th><th>Random top 1</th></tr></thead><tbody>");
            foreach (var row in partitions.EnumerateArray())
                text.Append($"<tr><th>{Value(row, "split")}</th><td>{Value(row, "rows")}</td><td>{Value(row, "withTeacherBoundary")}</td><td>{Ratio(row, "shapeTop1", "withTeacherBoundary")}</td><td>{Ratio(row, "shapeTop3", "withTeacherBoundary")}</td><td>{Ratio(row, "firstStreamShapeTop1", "withTeacherBoundary")}</td><td>{Ratio(row, "uniformShapeTop1", "withTeacherBoundary")}</td><td>{Ratio(row, "frequencyShapeTop1", "withTeacherBoundary")}</td><td>{Ratio(row, "randomShapeTop1", "withTeacherBoundary")}</td></tr>");
            text.Append("</tbody></table></div><div class='table-scroll'><table><caption>Uncertainty, conflicting observations and vocabulary coverage</caption><thead><tr><th>Partition</th><th>Abstentions / all rows</th><th>Conflict abstentions / conflicting rows</th><th>Unseen teacher heading rows</th><th>Unseen heading top 1 / unseen rows</th><th>Generated closed routes / all rows</th></tr></thead><tbody>");
            foreach (var row in partitions.EnumerateArray())
                text.Append($"<tr><th>{Value(row, "split")}</th><td>{Ratio(row, "abstentions", "rows")}</td><td>{Ratio(row, "conflictAbstentions", "conflicting")}</td><td>{Value(row, "unseenShapeRows")}</td><td>{Ratio(row, "unseenShapeTop1", "unseenShapeRows")}</td><td>{Ratio(row, "generatedClosed", "rows")}</td></tr>");
            text.Append("</tbody></table></div>");
        }
        text.Append("<details><summary>What this run does and does not learn</summary>");
        foreach (var field in new[] { "learningScope", "evidenceScope", "baselines", "budget" })
            if (summary.TryGetProperty(field, out var note) && note.ValueKind == JsonValueKind.String) text.Append("<p>" + H(note.GetString() ?? "") + "</p>");
        return text.Append("</details></section>").ToString();
    }

    public static string SourceSvg(NativeLatticeSources.SourceCase source, string title)
    {
        var frame = source.Graph.Frame; var positions = new Dictionary<IElement, List<Position>>(ReferenceEqualityComparer.Instance);
        foreach (var item in E(source.Bindings))
            if (item is Relation { Right: Relation word } binding && Read(frame, word) is Position point)
            {
                if (!positions.TryGetValue(binding.Left, out var alternatives)) positions.Add(binding.Left, alternatives = []);
                if (!alternatives.Contains(point)) alternatives.Add(point);
            }
        var strokes = E(source.Graph.Strokes).OfType<Relation>().Where(stroke => QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out _)).ToArray();
        var schematicOrigins = new List<Position>();
        // Display-only propagation can place an unbound endpoint from a known
        // one. It neither asserts a binding nor edits the native observation.
        Propagate();
        // A route can carry direction and incidence without any global anchor.
        // Give each such component a separate display-only local origin. This
        // is a schematic placement, never a newly authored native binding.
        foreach (var stroke in strokes)
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word);
            if (Read(frame, word) is null || positions.ContainsKey(from) || positions.ContainsKey(to)) continue;
            var occupied = new HashSet<IElement>(positions.Keys, ReferenceEqualityComparer.Instance);
            var nextX = positions.Count == 0 ? 0 : positions.Values.Max(points => points[0].X) + 2;
            positions[from] = [new Position(0, 0)];
            Propagate();
            var localSites = positions.Keys.Where(site => !occupied.Contains(site)).ToArray();
            var localPoints = localSites.SelectMany(site => positions[site]).ToArray();
            var offset = new Position(nextX - localPoints.Min(point => point.X), -localPoints.Min(point => point.Y));
            foreach (var site in localSites) positions[site] = positions[site].Select(point => point + offset).ToList();
            schematicOrigins.Add(positions[from][0]);
        }
        void Propagate()
        {
            for (var pass = 0; pass < strokes.Length; pass++)
            {
                var changed = false;
                foreach (var stroke in strokes)
                {
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word);
                    if (Read(frame, word) is not Position step) continue;
                    if (positions.TryGetValue(from, out var a) && !positions.ContainsKey(to)) { positions[to] = [a[0] + step]; changed = true; }
                    else if (positions.TryGetValue(to, out var b) && !positions.ContainsKey(from)) { positions[from] = [b[0] - step]; changed = true; }
                }
                if (!changed) break;
            }
        }
        var lines = new List<(Position From, Position To)>();
        foreach (var stroke in strokes)
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
            if (positions.TryGetValue(from, out var a) && positions.TryGetValue(to, out var b)) lines.Add((a[0], b[0]));
        }
        return Svg(lines, positions.Values.Where(points => points.Count > 1).SelectMany(points => points).ToArray(), title, schematicOrigins);
    }

    private static string PatternSvg(M.Pattern pattern, string title)
    {
        var cursor = new Position(0, 0); var lines = new List<(Position, Position)>();
        foreach (var word in pattern.Words)
            if (Read(pattern.Frame, word) is Position step) { var next = cursor + step; lines.Add((cursor, next)); cursor = next; }
        return Svg(lines, [], title);
    }

    private static Position? Read(QualitativeMotionFrame frame, Relation word)
    {
        var x = 0d; var y = 0d;
        if (!StructuralConstraint.TryReadScope(word, out var terms, out var head, out var tail) || !head.Equals(tail)) return null;
        foreach (var item in terms)
        {
            if (item is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) return null;
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1;
            else if (ReferenceEquals(axis, frame.Vertical)) y += forward ? 1 : -1;
            else return null;
        }
        return new(x, y);
    }

    private static string Svg(IReadOnlyList<(Position From, Position To)> lines, IReadOnlyList<Position> conflicts, string title,
        IReadOnlyList<Position>? schematicOrigins = null)
    {
        var all = lines.SelectMany(line => new[] { line.From, line.To }).Concat(conflicts).ToArray();
        var minX = all.Length == 0 ? 0 : all.Min(point => point.X); var maxX = all.Length == 0 ? 1 : all.Max(point => point.X);
        var minY = all.Length == 0 ? 0 : all.Min(point => point.Y); var maxY = all.Length == 0 ? 1 : all.Max(point => point.Y);
        var width = Math.Max(1, maxX - minX); var height = Math.Max(1, maxY - minY);
        var scale = Math.Min(330 / width, 220 / height); var originX = (390 - width * scale) / 2; var originY = (280 - height * scale) / 2;
        string X(Position p) => F(originX + (p.X - minX) * scale);
        string Y(Position p) => F(280 - originY - (p.Y - minY) * scale);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 390 280' role='img'><title>{H(title)}</title><rect width='390' height='280' fill='#faf8ef'/><g fill='none' stroke='#24515d' stroke-width='3.5' stroke-linecap='round' stroke-linejoin='round'>");
        foreach (var line in lines) svg.Append($"<path d='M{X(line.From)},{Y(line.From)}L{X(line.To)},{Y(line.To)}'/>");
        svg.Append("</g><g fill='none' stroke='#a66420' stroke-width='2' stroke-dasharray='3 3'>");
        foreach (var point in conflicts) svg.Append($"<circle cx='{X(point)}' cy='{Y(point)}' r='7'/>");
        svg.Append("</g>");
        if (schematicOrigins is { Count: > 0 })
        {
            svg.Append("<g fill='#faf8ef' stroke='#7c9891' stroke-width='1.5'>");
            foreach (var point in schematicOrigins) svg.Append($"<circle cx='{X(point)}' cy='{Y(point)}' r='4'/>");
            svg.Append("</g><text x='195' y='271' text-anchor='middle' fill='#667570' font-family='system-ui' font-size='11'>Unanchored routes · local schematic origins</text>");
        }
        if (lines.Count == 0) svg.Append("<text x='195' y='145' text-anchor='middle' fill='#53636b' font-family='system-ui' font-size='15'>No offered drawable passages</text>");
        return svg.Append("</svg>").ToString();
    }

    private readonly record struct Position(double X, double Y)
    {
        public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);
        public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);
    }
    private static string Page(string title, string content) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + H(title) + "</title><style>" + Css + "</style></head><body>" + content + "</body></html>";
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string Token(string value) => new(value.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var head, out var tail) && head.Equals(tail);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string Css = """
*{box-sizing:border-box}body{margin:0;background:#f5f2e8;color:#203b46;font:16px/1.55 system-ui,sans-serif}header,main,.chooser{max-width:1400px;margin:auto;padding:24px 28px}header{padding-bottom:12px}h1{font:500 clamp(28px,4vw,45px)/1.2 Georgia,serif;margin:8px 0 18px}h2{font:500 25px/1.3 Georgia,serif;margin:8px 0 12px}p{max-width:100ch}.eyebrow{font-size:13px;letter-spacing:.12em;text-transform:uppercase;color:#62716e}nav{display:flex;gap:24px;flex-wrap:wrap}a{color:#245968}section{margin:20px 0 42px}.chooser{display:flex;align-items:center;gap:16px;flex-wrap:wrap;padding-top:10px;padding-bottom:10px}select{font:inherit;max-width:100%;min-width:0;padding:10px;border:1px solid #a3b0a7;border-radius:5px;background:#fffdf7;color:inherit}.grid,.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,290px),1fr));gap:20px}.grid{align-items:start}article,figure,.tile{margin:0;min-width:0}article,.tile{background:#fffdf7;padding:16px;border:1px solid #d3d7ca;border-radius:6px}figure{background:#faf8ef}img{display:block;width:100%;height:auto;min-height:100px}figcaption{padding:9px 12px;font-size:14px;color:#526b6c}article p{font-size:14px;margin:12px 0 0}.candidate.compatible{border-top:5px solid #3b6a83}.candidate.conflicting{border-top:5px solid #b7863f}.badge{display:inline-block;font-weight:650;font-size:13px;padding:3px 8px;background:#e6edf0;border-radius:3px;margin:0 0 12px}.conflicting .badge{background:#f2e5ce}.muted{color:#667570}.note,aside{padding:15px 18px;background:#e8ede6;border-left:4px solid #7c9891}.teacher{padding:22px;background:#ebe8db;border-radius:6px}code{overflow-wrap:anywhere;font-size:12px}details{margin-top:14px}summary{cursor:pointer;font-size:13px}.tile{text-decoration:none}.tile h2{font-size:20px}.tile p{font-size:14px}.tile:hover{border-color:#5c8589}.run-summary{max-width:1344px;width:calc(100% - 56px);margin:24px auto;padding:22px;background:#eeeee2;border:1px solid #d7dbce;border-radius:6px}.metrics{display:grid;grid-template-columns:repeat(auto-fit,minmax(145px,1fr));gap:14px;margin:20px 0}.metrics div{padding:12px;background:#fffdf5}.metrics dt{font-size:12px;color:#526b6c}.metrics dd{margin:5px 0 0;font-size:25px}.table-scroll{max-width:100%;overflow:auto;margin:18px 0}table{border-collapse:collapse;font-size:13px;width:100%}caption{text-align:left;font-weight:650;padding:8px 0}th,td{padding:9px 12px;text-align:left;border-bottom:1px solid #cdd3c8;white-space:nowrap}thead{background:#e1e7dd}[hidden]{display:none!important}@media(max-width:600px){header,main,.chooser{padding:18px 14px}.teacher{padding:14px}.grid,.tiles{gap:14px}.chooser{display:block}.chooser label,.chooser span{display:block;margin:6px 0}select{width:100%}.run-summary{width:calc(100% - 28px);padding:14px}.metrics{grid-template-columns:repeat(2,minmax(0,1fr));gap:8px}}
""";
}
