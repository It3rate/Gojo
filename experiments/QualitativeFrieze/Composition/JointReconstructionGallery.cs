using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Measured presentation of the source, independently published
/// message streams and three native reconstructions. No displayed coordinate
/// or pixel returns to the qualitative reader or candidate selection.</summary>
public static class JointReconstructionGallery
{
    public sealed record Example(string Id, string Name, string Note, NativeLatticeSources.SourceCase Source,
        object Description, IReadOnlyList<Attempt> Attempts);
    public sealed record Attempt(int Index, string Name, NativeLatticeSources.SourceCase Output,
        object Feedback, Relation AssumedStrokes, bool Selected);

    public static void Write(string output, object summary, IReadOnlyList<Example> examples)
    {
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var rows = new List<object>(); var overview = new StringBuilder();
        foreach (var example in examples)
        {
            var token = Token(example.Id);
            var description = JsonSerializer.SerializeToElement(example.Description, Json);
            var labels = Labels(description, E(example.Source.Graph.Sites).Length);
            var sourceImage = $"patterns/{token}-source.svg";
            WriteText(Path.Combine(output, sourceImage), Svg(example.Source, labels, StructuralConstraint.Closed(), example.Name));
            var attempts = example.Attempts.Select(attempt =>
            {
                var image = $"patterns/{token}-attempt-{attempt.Index}.svg";
                WriteText(Path.Combine(output, image), Svg(attempt.Output, labels, attempt.AssumedStrokes, attempt.Name));
                return new { index = attempt.Index, name = attempt.Name, image, feedback = attempt.Feedback,
                    assumptions = E(attempt.AssumedStrokes).Length, selected = attempt.Selected };
            }).ToArray();
            rows.Add(new { id = example.Id, name = example.Name, note = example.Note,
                sourceImage, description = example.Description, attempts });
            var selected = attempts.Single(attempt => attempt.selected);
            overview.Append($"<a class='tile' href='gallery.html?case={Uri.EscapeDataString(example.Id)}'><h2>{H(example.Id)} · {H(example.Name)}</h2><div class='tile-pair'><figure><img src='{sourceImage}' alt='Reference source'><figcaption>Source</figcaption></figure><figure><img src='{selected.image}' alt='Selected reconstruction'><figcaption>Selected sample</figcaption></figure></div><p>{H(example.Note)}</p></a>");
        }
        var data = JsonSerializer.Serialize(new { schema = "joint-reconstruction-gallery-v1", summary, examples = rows }, Json);
        WriteText(Path.Combine(output, "gallery-data.json"), data);
        var summaryMarkup = Summary(JsonSerializer.SerializeToElement(summary, Json));
        WriteText(Path.Combine(output, "gallery.html"), Page("Gojo · joint and symmetry reconstruction", """
<header><p class="eyebrow">Gojo · qualitative composition experiment</p><h1>Reconstructing from joints and relations</h1>
<p>The decoder receives joint, qualitative position and separate symmetry streams. It tries three general construction policies, retaining unsupported closure as an assumption. These policies are exploratory and have not been trained.</p>
<nav><a href="overview.html">All source targets</a><a href="gallery-data.json">Inspection data</a></nav></header>
SUMMARY
<section class="chooser"><label for="case">Source case</label><select id="case"></select></section><main id="content"></main>
<script>
globalThis.JOINT_RECONSTRUCTION=DATA;
const data=globalThis.JOINT_RECONSTRUCTION, select=document.querySelector('#case'), content=document.querySelector('#content');
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const value=v=>Array.isArray(v)?v.map(value).join(', '):(v&&typeof v==='object'?JSON.stringify(v):String(v??'—'));
const title=s=>String(s).replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,c=>c.toUpperCase());
const pic=(src,caption)=>`<figure><img src="${esc(src)}" alt="${esc(caption)}"><figcaption>${esc(caption)}</figcaption></figure>`;
const feedback=f=>`<dl class="feedback">${Object.entries(f??{}).map(([k,v])=>`<div><dt>${esc(title(k))}</dt><dd>${esc(value(v))}</dd></div>`).join('')}</dl>`;
const buckets=groups=>Array.isArray(groups)?groups.map(group=>`<span class="bucket">${esc(value(group))}</span>`).join('<span aria-hidden="true"> → </span>'):'<span class="muted">Not published</span>';
for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=`${row.id} · ${row.name}`;select.append(option);}
function show(){const row=data.examples.find(x=>x.id===select.value)??data.examples[0];if(!row){content.innerHTML='<p>No inspected cases.</p>';return;}select.value=row.id;
 const url=new URL(location.href);url.searchParams.set('case',row.id);history.replaceState(null,'',url);const d=row.description??{};
 content.innerHTML=`<h2>${esc(row.id)} · ${esc(row.name)}</h2><p class="note">${esc(row.note)}</p>
 <section class="message"><h2>The communication received by the decoder</h2><p>Labels identify corresponding roles across streams and attempts. Ordering buckets do not specify distances. Joint rays and the boundary/interior context remain distinct evidence.</p><p class="muted">${esc(d.notes??'')}</p>
 <div class="streams"><article><h3>Joint stream</h3><div class="joint-list">${(d.joints??[]).map(j=>`<div><strong>${esc(j.id)}</strong><span>${esc(value(j.kind))}</span><span class="muted">rays ${esc(value(j.rays))}</span><span class="muted">right-angle read ${esc(value(j.rightAngles))} · interior ${esc(value(j.interior))}</span></div>`).join('')||'<p>No joint inventory published.</p>'}</div></article>
 <article><h3>Qualitative position stream</h3><p class="muted">Arrows mean earlier to later in the published axis order.</p><h4>Horizontal order</h4><div class="buckets">${buckets(d.horizontalBuckets)}</div><h4>Vertical order</h4><div class="buckets">${buckets(d.verticalBuckets)}</div><h4>Closure obligations</h4><p>${esc(value(d.closureObligations))}</p><p class="muted">A closed-area default can propose a missing passage. It does not make that passage observed.</p></article>
 <article><h3>Separate symmetry stream</h3><div class="table-scroll"><table><thead><tr><th>Read</th><th>Status</th><th>Edges matched</th><th>Unmatched roles / passages</th><th>Missing roles / passages</th></tr></thead><tbody>${(d.symmetries??[]).map(s=>`<tr><th>${esc(s.name)}</th><td>${esc(value(s.status))}</td><td>${esc(value(s.matched))}</td><td>${esc(value(s.unmatched))}</td><td>${esc(value(s.missing))}</td></tr>`).join('')}</tbody></table></div><p class="muted">Symmetry evidence may support a partial context while leaving a defect or unmatched role visible.</p></article></div></section>
 <section><h2>Three native reconstructions</h2><p>The reference is shown for assessment. Candidate generation uses the communication above. Solid strokes are the candidate's connected structure; dashed amber strokes are retained assumptions. A selected attempt is a policy preference, not a proof of unique identity.</p>
 <div class="attempts">${row.attempts.map(a=>`<article class="attempt ${a.selected?'selected':''}"><span class="badge">${a.selected?'Selected attempt':'Alternative attempt'}</span><h3>${esc(a.index)} · ${esc(a.name)}</h3>${pic(a.image,`${row.id} · attempt ${a.index}`)}<p class="assumptions">${a.assumptions} assumed stroke${a.assumptions===1?'':'s'}</p>${feedback(a.feedback)}</article>`).join('')}</div></section>
 <section class="message"><h2>What happens when a stream is withheld?</h2><p>Each row runs three further proposals with one part of the message omitted. The comparison is with the selected full-message pairing. Removing a critic cue can leave a proposed output unchanged while removing the ability to reject alternatives.</p><div class="table-scroll"><table><thead><tr><th>Message</th><th>Output passages</th><th>Same pairings</th><th>Violations</th><th>Open checks</th></tr></thead><tbody>${(d.messageAblations??[]).map(p=>`<tr><th>${esc(p.profile)}</th><td>${esc(p.strokes)}</td><td>${p.samePairingsAsFull?'Same':'Different'}</td><td>${esc(p.violated)}</td><td>${esc(p.open)}</td></tr>`).join('')}</tbody></table></div><p class="muted">With no axis-order cues, this receiver leaves placement open and produces no passages. Inferring those missing orders is future work. Scores from messages containing different checks are not directly comparable.</p></section>
 <section class="reference"><h2>Reference source · held back from construction</h2><div class="reference-grid">${pic(row.sourceImage,'Original executed and published source')}<div><p>The source was constructed independently before the reconstruction pass. Its actual passages, damage and physical joins can be compared with the generated alternatives.</p><p>The same role labels appear in every attempt. Several constructions can satisfy a coarse description; differences may remain in incidence, direction, shape, calibration or provenance.</p><p class="muted">The renderer assigns one display step to each uncalibrated reach. Display coordinates and pixels are not recognition evidence. Open placement uses explicitly marked local schematic origins.</p></div></div></section>`;
}
select.addEventListener('change',show);const requested=new URL(location.href).searchParams.get('case');select.value=data.examples.some(x=>x.id===requested)?requested:(data.examples[0]?.id??'');show();
</script>
""".Replace("SUMMARY", summaryMarkup, StringComparison.Ordinal).Replace("DATA", data, StringComparison.Ordinal)));
        WriteText(Path.Combine(output, "overview.html"), Page("Gojo · reconstruction source atlas",
            "<header><p class='eyebrow'>Gojo · qualitative composition</p><h1>Independent targets and reconstructed samples</h1><p>Compare the source with the sample selected from three attempts. Reconstruction receives separate native evidence streams, rather than these reference edges or pixels. Dashed amber passages are assumptions.</p><nav><a href='gallery.html'>Detailed attempts</a><a href='gallery-data.json'>Inspection data</a></nav></header>" + summaryMarkup + "<main class='tiles'>" + overview + "</main>"));
    }

    private static string[] Labels(JsonElement description, int count)
    {
        var labels = description.TryGetProperty("joints", out var joints) && joints.ValueKind == JsonValueKind.Array
            ? joints.EnumerateArray().Select(joint => joint.TryGetProperty("id", out var id) ? id.GetString() : null).ToArray() : [];
        return Enumerable.Range(0, count).Select(index => index < labels.Length && labels[index] is string label ? label : "J" + (index + 1)).ToArray();
    }

    internal static void WriteText(string path, string text)
    {
        // A preview can keep an SVG mapped on Windows. Replace its directory
        // entry instead of truncating that still-mapped file in place.
        var pending = path + ".pending";
        File.WriteAllText(pending, text);
        for (var attempt = 0; ; attempt++)
        {
            try { File.Move(pending, path, overwrite: true); return; }
            // File watchers and Windows previews can briefly hold a directory
            // entry without delete sharing. Keep the complete pending content
            // and retry the replacement instead of truncating the live file.
            catch (Exception error) when (attempt < 8 && error is IOException or UnauthorizedAccessException)
            { Thread.Sleep(25 * (attempt + 1)); }
        }
    }

    private static string Summary(JsonElement summary)
    {
        var text = new StringBuilder("<section class='run-summary'><h2>Experiment measurements</h2><dl class='metrics'>");
        foreach (var field in summary.EnumerateObject().Where(field => field.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            text.Append($"<div><dt>{H(Human(field.Name))}</dt><dd>{H(field.Value.ToString())}</dd></div>");
        text.Append("</dl><details><summary>Scope and interpretation</summary>");
        foreach (var field in summary.EnumerateObject().Where(field => field.Value.ValueKind == JsonValueKind.String))
            text.Append($"<p><strong>{H(Human(field.Name))}:</strong> {H(field.Value.GetString() ?? "")}</p>");
        return text.Append("<p>Scores compare retained structural evidence under a bounded policy. They are not calibrated probabilities. The three policies are general construction experiments, not learned ornament recipes.</p></details></section>").ToString();
    }

    internal static string Svg(NativeLatticeSources.SourceCase source, IReadOnlyList<string> labels, Relation assumed, string title)
    {
        var frame = source.Graph.Frame; var sites = E(source.Graph.Sites);
        var positions = new Dictionary<IElement, Point>(ReferenceEqualityComparer.Instance);
        foreach (var binding in E(source.Bindings).OfType<Relation>())
            if (binding.Right is Relation word && Read(frame, word) is Point point) positions.TryAdd(binding.Left, point);
        var strokes = E(source.Graph.Strokes).OfType<Relation>().Where(stroke => QualitativeStrokeGraph.TryReadStroke(stroke, out _, out _, out _, out _)).ToArray();
        var assumptions = new HashSet<IElement>(E(assumed), ReferenceEqualityComparer.Instance);
        var origins = new List<Point>(); Propagate();
        foreach (var stroke in strokes)
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word);
            if (positions.ContainsKey(from) || positions.ContainsKey(to) || Read(frame, word) is null) continue;
            var earlier = new HashSet<IElement>(positions.Keys, ReferenceEqualityComparer.Instance);
            var next = positions.Count == 0 ? 0 : positions.Values.Max(point => point.X) + 2;
            positions[from] = new(0, 0); Propagate();
            var added = positions.Keys.Where(site => !earlier.Contains(site)).ToArray();
            var offset = new Point(next - added.Min(site => positions[site].X), -added.Min(site => positions[site].Y));
            foreach (var site in added) positions[site] += offset;
            origins.Add(positions[from]);
        }
        void Propagate()
        {
            for (var pass = 0; pass < strokes.Length; pass++)
            {
                var changed = false;
                foreach (var stroke in strokes)
                {
                    QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out var word);
                    if (Read(frame, word) is not Point step) continue;
                    if (positions.TryGetValue(from, out var a) && !positions.ContainsKey(to)) { positions[to] = a + step; changed = true; }
                    else if (positions.TryGetValue(to, out var b) && !positions.ContainsKey(from)) { positions[from] = b - step; changed = true; }
                }
                if (!changed) break;
            }
        }
        var points = positions.Values.ToArray();
        var minX = points.Length == 0 ? 0 : points.Min(point => point.X); var maxX = points.Length == 0 ? 1 : points.Max(point => point.X);
        var minY = points.Length == 0 ? 0 : points.Min(point => point.Y); var maxY = points.Length == 0 ? 1 : points.Max(point => point.Y);
        var width = Math.Max(1, maxX - minX); var height = Math.Max(1, maxY - minY);
        var scale = Math.Min(330 / width, 220 / height); var x0 = (430 - width * scale) / 2; var y0 = (320 - height * scale) / 2;
        double PX(Point point) => x0 + (point.X - minX) * scale;
        double PY(Point point) => 320 - y0 - (point.Y - minY) * scale;
        string X(Point point) => F(PX(point)); string Y(Point point) => F(PY(point));
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 430 320' role='img'><title>{H(title)}</title><rect width='430' height='320' fill='#fbf9f0'/>");
        foreach (var stroke in strokes)
        {
            QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _);
            if (!positions.TryGetValue(from, out var a) || !positions.TryGetValue(to, out var b)) continue;
            var assumption = assumptions.Contains(stroke);
            svg.Append($"<path d='M{X(a)},{Y(a)}L{X(b)},{Y(b)}' fill='none' stroke='{(assumption ? "#b57b29" : "#265661")}' stroke-width='3.3' stroke-linecap='round'{(assumption ? " stroke-dasharray='7 5'" : "")}/>");
        }
        for (var index = 0; index < sites.Length; index++)
        {
            if (!positions.TryGetValue(sites[index], out var point)) continue;
            var label = index < labels.Count ? labels[index] : "J" + (index + 1);
            svg.Append($"<circle cx='{X(point)}' cy='{Y(point)}' r='3.5' fill='#fbf9f0' stroke='#658483' stroke-width='1.5'/><text x='{F(PX(point) + 7)}' y='{F(PY(point) - 7)}' font-family='system-ui' font-size='11' fill='#536768' paint-order='stroke' stroke='#fbf9f0' stroke-width='3'>{H(label)}</text>");
        }
        if (origins.Count != 0) svg.Append("<text x='215' y='308' text-anchor='middle' font-family='system-ui' font-size='11' fill='#71817a'>Unanchored component · local schematic origin</text>");
        if (strokes.Length == 0) svg.Append("<text x='215' y='160' text-anchor='middle' font-family='system-ui' fill='#71817a'>No constructed passages</text>");
        return svg.Append("</svg>").ToString();
    }

    private static Point? Read(QualitativeMotionFrame frame, Relation word)
    {
        if (!StructuralConstraint.TryReadScope(word, out var moves, out var head, out var tail) || !head.Equals(tail)) return null;
        var x = 0d; var y = 0d;
        foreach (var item in moves)
        {
            if (item is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) return null;
            if (ReferenceEquals(axis, frame.Horizontal)) x += forward ? 1 : -1;
            else if (ReferenceEquals(axis, frame.Vertical)) y += forward ? 1 : -1;
            else return null;
        }
        return new(x, y);
    }
    private readonly record struct Point(double X, double Y)
    {
        public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
        public static Point operator -(Point a, Point b) => new(a.X - b.X, a.Y - b.Y);
    }
    private static string Page(string title, string body) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + H(title) + "</title><style>" + Css + "</style></head><body>" + body + "</body></html>";
    private static string Human(string text) => System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    private static string Token(string text) => new(text.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string Css = """
.tile-pair{display:grid;grid-template-columns:1fr 1fr;gap:8px}.tile-pair figcaption{padding:6px;font-size:11px}
*{box-sizing:border-box}body{margin:0;background:#f4f1e7;color:#223e46;font:16px/1.55 system-ui,sans-serif}header,main,.chooser{max-width:1480px;margin:auto;padding:24px 28px}header{padding-bottom:10px}h1{font:500 clamp(29px,4vw,45px)/1.2 Georgia,serif;margin:9px 0 18px}h2{font:500 26px/1.3 Georgia,serif;margin:10px 0 15px}h3{font-size:18px;font-weight:600;margin:5px 0 13px}h4{font-size:14px;margin:18px 0 8px}p{max-width:100ch}.eyebrow{font-size:12px;letter-spacing:.12em;text-transform:uppercase;color:#697c76}nav{display:flex;gap:24px;flex-wrap:wrap}a{color:#2a6470}section{margin:24px 0 40px}.chooser{display:flex;gap:18px;align-items:center;padding-top:8px;padding-bottom:8px}select{font:inherit;padding:11px;max-width:100%;min-width:0;border:1px solid #a2b1a6;border-radius:5px;background:#fffdf7;color:inherit}.note{padding:16px 20px;border-left:4px solid #80988f;background:#e5eade}.streams,.attempts,.tiles{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}.streams article,.attempt,.tile{background:#fffdf8;border:1px solid #d1d8cc;border-radius:6px;padding:18px;min-width:0}.attempt{border-top:5px solid #9baab0}.attempt.selected{border-top-color:#426f83}.badge{font-size:12px;padding:4px 9px;background:#e7eceb;display:inline-block;border-radius:4px;margin:0 0 13px}.selected .badge{background:#dfeaf0}figure{margin:0;background:#fbf9f0}img{display:block;width:100%;height:auto}figcaption{font-size:13px;padding:8px 12px;color:#687d78}.feedback{font-size:13px;margin:15px 0 0}.feedback div{display:grid;grid-template-columns:minmax(100px,1fr) 1fr;gap:10px;padding:8px 0;border-top:1px solid #e1e5d9}.feedback dt{color:#5b7471}.feedback dd{margin:0;overflow-wrap:anywhere}.assumptions{font-size:13px;color:#8b642e}.reference{background:#e9e6d9;padding:24px;border-radius:6px}.reference-grid{display:grid;grid-template-columns:minmax(240px,430px) 1fr;gap:28px;align-items:center}.muted{color:#71817b;font-size:13px}.joint-list{max-height:430px;overflow:auto}.joint-list>div{display:grid;grid-template-columns:34px 1fr;gap:3px 8px;padding:9px 0;border-bottom:1px solid #e4e8dd}.joint-list .muted{grid-column:2}.buckets{font-size:13px;line-height:2.5}.bucket{display:inline-block;background:#eef0e5;border-radius:4px;padding:1px 7px;margin:2px}.table-scroll{overflow:auto;max-width:100%}table{border-collapse:collapse;font-size:12px;width:100%}th,td{padding:8px 7px;border-bottom:1px solid #dce2d4;text-align:left;vertical-align:top}th{font-weight:600}td{overflow-wrap:anywhere}.run-summary{max-width:1424px;margin:20px auto;padding:20px 24px;border-radius:6px;background:#e9eddf;width:calc(100% - 56px)}.metrics{display:grid;grid-template-columns:repeat(auto-fit,minmax(130px,1fr));gap:12px;margin:12px 0}.metrics div{background:#fffdf6;padding:10px 13px}.metrics dt{font-size:12px;color:#5a746d}.metrics dd{font-size:24px;margin:4px 0}details{margin-top:18px}summary{cursor:pointer;font-size:13px}.tile{text-decoration:none}.tile h2{font-size:20px}.tile p{font-size:13px}.tiles{grid-template-columns:repeat(auto-fit,minmax(min(100%,280px),1fr))}[hidden]{display:none!important}@media(max-width:1050px){.streams{grid-template-columns:1fr 1fr}.streams article:last-child{grid-column:1/-1}}@media(max-width:760px){header,main,.chooser{padding:18px 14px}.streams,.attempts,.reference-grid{grid-template-columns:1fr}.streams article:last-child{grid-column:auto}.run-summary{width:calc(100% - 28px);padding:15px}.chooser{display:block}.chooser label{display:block;margin-bottom:7px}select{width:100%}.reference{padding:16px}.metrics{grid-template-columns:repeat(2,minmax(0,1fr))}}
""";
}
