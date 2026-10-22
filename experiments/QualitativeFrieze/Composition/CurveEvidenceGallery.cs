using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Presentation reads only. Numeric Bézier controls, viewport bounds,
/// marker choices, labels and percentages are never returned to native evidence.
/// The producer derives these DTOs from its connected receipt surfaces.</summary>
public static class CurveEvidenceGallery
{
    public readonly record struct Point(double X, double Y);
    public enum JoinMark { None, Tangent, Cusp, Corner, Open, Unresolved }
    public sealed record Stroke(Point Start, Point End, Point? Control1 = null,
        Point? Control2 = null, bool Assumed = false, string Note = "");
    public sealed record Joint(Point Position, JoinMark Mark, string Note = "",
        Point? Incoming = null, Point? Outgoing = null);
    public sealed record Drawing(IReadOnlyList<Stroke> Strokes, IReadOnlyList<Joint> Joints,
        string Note = "", bool Closed = false);
    public sealed record Candidate(string Id, string Name, Drawing Output, string Prior,
        string Posterior, string Interpretation, object Audit);
    public sealed record Evidence(string Name, string Outcome, string Basis, string Estimate, string Note);
    public sealed record Example(string Id, string Name, string Note, Drawing Source, Drawing Coarse,
        IReadOnlyList<Candidate> Candidates, IReadOnlyList<Evidence> Evidence, object Diagnostics,
        string? SelectedId = null);

    public static void Write(string output, object summary, IReadOnlyList<Example> examples)
    {
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var rows = new List<object>(); var tiles = new StringBuilder();
        foreach (var example in examples)
        {
            var all = new[] { example.Source, example.Coarse }.Concat(example.Candidates.Select(row => row.Output));
            var frame = Framing(all); var token = Token(example.Id);
            var sourceImage = Save("source", example.Source, "Reference source");
            var coarseImage = Save("coarse", example.Coarse, "Communicated coarse read");
            var candidates = example.Candidates.Select(candidate => new
            {
                candidate.Id, candidate.Name, candidate.Prior, candidate.Posterior,
                candidate.Interpretation, candidate.Audit,
                Image = Save("candidate-" + Token(candidate.Id), candidate.Output, candidate.Name),
                Drawing = Describe(candidate.Output)
            }).ToArray();
            rows.Add(new { example.Id, example.Name, example.Note, SourceImage = sourceImage,
                CoarseImage = coarseImage, Source = Describe(example.Source), Coarse = Describe(example.Coarse),
                example.SelectedId, Candidates = candidates, example.Evidence, example.Diagnostics });
            var chosen = candidates.FirstOrDefault(row => row.Id == example.SelectedId) ?? candidates.FirstOrDefault();
            var href = "gallery.html?case=" + Uri.EscapeDataString(example.Id);
            tiles.Append($"<article class='tile'><h2><a href='{href}'>{H(example.Id)} · {H(example.Name)}</a></h2><div class='tile-images'>");
            Tile(sourceImage, "Reference source"); Tile(coarseImage, "Coarse read");
            if (chosen is not null) Tile(chosen.Image, example.SelectedId is null ? "One retained alternative" : "Receiver representative");
            tiles.Append($"</div><p>{H(example.Note)}</p></article>");

            string Save(string suffix, Drawing drawing, string caption)
            {
                var image = $"patterns/{token}-{suffix}.svg";
                WriteText(Path.Combine(output, image), Svg(drawing, frame, example.Id + " · " + caption, suffix == "coarse"));
                return image;
            }
            void Tile(string image, string caption) => tiles.Append($"<figure><a href='{href}'><img src='{image}' alt='{H(caption)}' loading='lazy'></a><figcaption>{H(caption)}</figcaption></figure>");
        }
        var data = JsonSerializer.Serialize(new { Schema = "curve-evidence-gallery-v1", summary, Examples = rows }, Json);
        WriteText(Path.Combine(output, "gallery-data.json"), data);
        WriteText(Path.Combine(output, "gallery.html"), Page("Gojo · mixed line and curve evidence", Header + Metrics(summary) + Inspector.Replace("PAYLOAD", data, StringComparison.Ordinal)));
        WriteText(Path.Combine(output, "overview.html"), Page("Gojo · line and curve atlas", Header + Metrics(summary) + "<main class='tiles'>" + tiles + "</main>"));
    }

    private static object Describe(Drawing drawing) => new
    {
        drawing.Note, drawing.Closed, Strokes = drawing.Strokes.Count,
        Straight = drawing.Strokes.Count(stroke => !stroke.Control1.HasValue),
        Quadratic = drawing.Strokes.Count(stroke => stroke.Control1.HasValue && !stroke.Control2.HasValue),
        Cubic = drawing.Strokes.Count(stroke => stroke.Control2.HasValue),
        Assumed = drawing.Strokes.Count(stroke => stroke.Assumed),
        Joints = drawing.Joints.Select(joint => new { Mark = joint.Mark.ToString(), joint.Note,
            Incoming = joint.Incoming.HasValue, Outgoing = joint.Outgoing.HasValue }).ToArray()
    };

    private readonly record struct Frame(double CenterX, double CenterY, double Scale);
    private static Frame Framing(IEnumerable<Drawing> drawings)
    {
        var points = drawings.SelectMany(drawing => drawing.Strokes.SelectMany(stroke =>
            new Point?[] { stroke.Start, stroke.End, stroke.Control1, stroke.Control2 })
            .Where(point => point.HasValue).Select(point => point!.Value)
            .Concat(drawing.Joints.Select(joint => joint.Position))).ToArray();
        if (points.Length == 0) return new(0, 0, 1);
        if (points.Any(point => !double.IsFinite(point.X) || !double.IsFinite(point.Y)))
            throw new ArgumentException("Curve display requires finite boundary points.");
        var xmin = points.Min(point => point.X); var xmax = points.Max(point => point.X);
        var ymin = points.Min(point => point.Y); var ymax = points.Max(point => point.Y);
        var width = Math.Max(xmax - xmin, .001); var height = Math.Max(ymax - ymin, .001);
        return new((xmin + xmax) / 2, (ymin + ymax) / 2, Math.Min(340 / width, 224 / height));
    }

    private static string Svg(Drawing drawing, Frame frame, string title, bool coarse)
    {
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 420 300' role='img'><title>{H(title)}</title><desc>{H(drawing.Note)} Join markers report supplied local reads; curve geometry is one display interpretation.</desc><rect width='420' height='300' fill='#fbf8ee'/><defs><marker id='arrow' markerWidth='6' markerHeight='6' refX='4' refY='3' orient='auto' markerUnits='strokeWidth'><path d='M0 0L6 3L0 6' fill='none' stroke='#929caa' stroke-width='1.1'/></marker></defs>");
        foreach (var stroke in drawing.Strokes)
        {
            if (stroke.Control2.HasValue && !stroke.Control1.HasValue)
                throw new ArgumentException("A cubic display requires both control points.");
            var start = At(stroke.Start); var end = At(stroke.End);
            var path = "M" + Pair(start);
            if (stroke.Control2 is { } second && stroke.Control1 is { } first)
                path += " C" + Pair(At(first)) + " " + Pair(At(second)) + " " + Pair(end);
            else if (stroke.Control1 is { } control) path += " Q" + Pair(At(control)) + " " + Pair(end);
            else path += " L" + Pair(end);
            svg.Append($"<path class='stroke{(stroke.Assumed ? " assumed" : "")}' d='{path}' fill='none' stroke='{(stroke.Assumed ? "#ab762e" : coarse ? "#7e969a" : "#285762")}' stroke-width='{(coarse ? "3.1" : "3.8")}' stroke-linejoin='round' stroke-linecap='round'{(stroke.Assumed ? " stroke-dasharray='8 5'" : "")}><title>{H(stroke.Note)}</title></path>");
        }
        foreach (var joint in drawing.Joints)
        {
            if (joint.Mark == JoinMark.None) continue;
            var site = At(joint.Position); var x = F(site.X); var y = F(site.Y);
            svg.Append($"<g class='joint' data-mark='{joint.Mark}'><title>{H(joint.Mark + (joint.Note.Length == 0 ? "" : ": " + joint.Note))}</title>");
            Direction(joint.Incoming, true); Direction(joint.Outgoing, false);
            switch (joint.Mark)
            {
                case JoinMark.Tangent:
                    svg.Append($"<circle cx='{x}' cy='{y}' r='4.7' fill='#fbf8ee' stroke='#4e8490' stroke-width='2.3'/>"); break;
                case JoinMark.Cusp:
                    svg.Append($"<path d='M{F(site.X)} {F(site.Y - 6)}L{F(site.X + 5)} {y}L{x} {F(site.Y + 6)}L{F(site.X - 5)} {y}Z' fill='#76618d' stroke='#fbf8ee' stroke-width='1.1'/>"); break;
                case JoinMark.Corner:
                    svg.Append($"<rect x='{F(site.X - 3.9)}' y='{F(site.Y - 3.9)}' width='7.8' height='7.8' fill='#285762' stroke='#fbf8ee' stroke-width='1.1'/>"); break;
                case JoinMark.Open:
                    svg.Append($"<circle cx='{x}' cy='{y}' r='5.1' fill='#fbf8ee' stroke='#ab762e' stroke-width='2.2'/>"); break;
                default:
                    svg.Append($"<circle cx='{x}' cy='{y}' r='7' fill='#fbf8ee' stroke='#8e849f' stroke-width='1.7' stroke-dasharray='2.7 2.2'/><text x='{x}' y='{F(site.Y + 3)}' text-anchor='middle' font-family='sans-serif' font-size='9' fill='#76618d'>?</text>"); break;
            }
            svg.Append("</g>");
            void Direction(Point? direction, bool incoming)
            {
                if (direction is not { } vector || !double.IsFinite(vector.X) || !double.IsFinite(vector.Y)) return;
                var length = Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y); if (length <= 1e-12) return;
                var dx = 26 * vector.X / length; var dy = -26 * vector.Y / length;
                var from = incoming ? new Point(site.X - dx, site.Y - dy) : new Point(site.X + dx * .25, site.Y + dy * .25);
                var to = incoming ? new Point(site.X - dx * .25, site.Y - dy * .25) : new Point(site.X + dx, site.Y + dy);
                svg.Append($"<path class='tangent-direction' d='M{Pair(from)}L{Pair(to)}' fill='none' stroke='#929caa' stroke-width='1.2' marker-end='url(#arrow)' opacity='.85'/>");
            }
        }
        svg.Append("</svg>"); return svg.ToString();
        Point At(Point point) => new(210 + (point.X - frame.CenterX) * frame.Scale, 150 - (point.Y - frame.CenterY) * frame.Scale);
    }

    private static string Metrics(object summary)
    {
        var data = JsonSerializer.SerializeToElement(summary, Json);
        var result = new StringBuilder("<section class='run-summary'><h2>Run measurements</h2><dl class='metrics'>");
        foreach (var property in data.EnumerateObject().Where(row => row.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            result.Append($"<div><dt>{H(Human(property.Name))}</dt><dd>{H(property.Value.ToString())}</dd></div>");
        result.Append("</dl><details><summary>Premises and limits</summary>");
        foreach (var property in data.EnumerateObject().Where(row => row.Value.ValueKind == JsonValueKind.String))
            result.Append($"<p><strong>{H(Human(property.Name))}:</strong> {H(property.Value.GetString() ?? "")}</p>");
        return result.Append("</details></section>").ToString();
    }
    private static string Page(string title, string body) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + H(title) + "</title><style>" + Css + "</style></head><body>" + body + "</body></html>";
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string Human(string text) => System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    private static string Token(string text) => new(text.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
    private static string F(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static string Pair(Point point) => F(point.X) + " " + F(point.Y);
    private static void WriteText(string path, string text) => File.WriteAllText(path, text, new UTF8Encoding(false));
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private const string Header = """
<header><p class="eyebrow">Gojo · qualitative curve and joint evidence</p><h1>What must a curve description preserve?</h1><p>Straight passages, bows, tangent joins, cusps and missing spans are separate pieces of information. A coarse overall reading can anchor a fine local exception without replacing it. These cases compare several concrete receiver interpretations of native evidence; the drawings do not feed a pixel classifier.</p><nav><a href="gallery.html">Inspect cases</a><a href="overview.html">Whole atlas</a><a href="gallery-data.json">Inspection data</a><a href="summary.json">Run data</a></nav></header>
""";
    private const string Inspector = """
<section class="chooser"><label>Case<select id="case"></select></label></section><main id="content"></main>
<script>
globalThis.CURVE_EVIDENCE=PAYLOAD;
const data=globalThis.CURVE_EVIDENCE,choose=document.querySelector('#case'),content=document.querySelector('#content');
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const human=s=>String(s).replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,c=>c.toUpperCase());
const text=v=>Array.isArray(v)?v.map(text).join(' · '):v&&typeof v==='object'?Object.entries(v).map(([k,x])=>`${human(k)}: ${text(x)}`).join('; '):String(v??'—');
const detail=v=>`<dl class="feedback">${Object.entries(v??{}).map(([k,x])=>`<div><dt>${esc(human(k))}</dt><dd>${esc(text(x))}</dd></div>`).join('')}</dl>`;
const fraction=s=>{try{const a=String(s).split('/');if(a.length!==2)return null;const n=BigInt(a[0]),d=BigInt(a[1]);return d>0n&&n>=0n?{n,d}:null;}catch{return null;}};
const sameWeight=(a,b)=>{const x=fraction(a),y=fraction(b);return x&&y?x.n*y.d===y.n*x.d:false;};
const percent=s=>{const q=fraction(s);if(!q)return 'Open';const p=Number(q.n*10000n/q.d)/100;return p===0&&q.n>0n?'<0.01%':`${p.toFixed(2).replace(/\.00$/,'')}%`;};
const width=s=>{const q=fraction(s);return q?Math.min(100,Number(q.n*10000n/q.d)/100):0;};
const figure=(src,caption)=>`<figure><img src="${esc(src)}" alt="${esc(caption)}"><figcaption>${esc(caption)}</figcaption></figure>`;
const drawingNote=d=>`<p class="drawing-note">${esc(d.note)}${d.closed?' A closed-area display context is supplied.':''}</p>`;
for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=`${row.id} · ${row.name}`;choose.append(option);}
function show(){const row=data.examples.find(x=>x.id===choose.value)??data.examples[0];if(!row){content.textContent='No retained cases.';return;}choose.value=row.id;
const url=new URL(location.href);url.searchParams.set('case',row.id);history.replaceState(null,'',url);
const selected=row.candidates.find(x=>x.id===row.selectedId),sample=selected??row.candidates[0];
const ties=selected?row.candidates.filter(candidate=>sameWeight(candidate.posterior,selected.posterior)):[];
const preferred=candidate=>ties.some(tied=>tied.id===candidate.id);
content.innerHTML=`<article class="case" data-case="${esc(row.id)}"><h2>${esc(row.id)} · ${esc(row.name)}</h2><p class="case-note">${esc(row.note)}</p>
<section class="comparison"><article>${figure(row.sourceImage,'Reference source')}<h3>Source construction</h3>${drawingNote(row.source)}</article><article>${figure(row.coarseImage,'Communicated coarse reading')}<h3>Coarse or selected evidence</h3>${drawingNote(row.coarse)}</article><article class="sample">${sample?figure(sample.image,sample.name):''}<h3>${selected?ties.length>1?`One of ${ties.length} tied samples`:'Preferred receiver sample':'One retained alternative'}</h3><p>${esc(sample?.name??'No complete sample')} ${selected?`· ${esc(percent(selected.posterior))} relative weight`:''}</p>${ties.length>1?'<p class="tie-note">The displayed representative does not resolve this tie.</p>':''}${sample?drawingNote(sample.drawing):''}</article></section>
<p class="legend"><span class="key ink"></span> Supplied passage <span class="key assumed"></span> Inferred passage <span class="join-circle"></span> Tangent <span class="join-square"></span> Corner <span class="join-diamond"></span> Cusp <span class="join-open"></span> Open <span class="join-question">?</span> Unresolved</p>
<p class="interpretation-note">Markers describe the local joint reading, not correctness. Arrows show supplied traversal directions. Blue identifies a preference; it does not mean “yes” or “true”. Amber dashed passages remain assumptions even when they match the source.</p>
<section><h2>Evidence streams</h2><div class="table-scroll"><table class="evidence"><thead><tr><th>Read</th><th>Outcome</th><th>Basis / scope</th><th>Likelihood estimate</th><th>Interpretation</th></tr></thead><tbody>${row.evidence.map(e=>`<tr><th>${esc(e.name)}</th><td>${esc(e.outcome)}</td><td>${esc(e.basis)}</td><td>${esc(e.estimate)}</td><td>${esc(e.note)}</td></tr>`).join('')}</tbody></table></div></section>
<section><h2>Retained receiver alternatives</h2><p class="interpretation-note">Fractions are exact derived reads within this candidate set. Their premises and estimates are supplied for this experiment; they are not learned real-world probabilities. A coarse anchor and a local mismatch can both remain useful evidence.</p><div class="alternatives">${row.candidates.map(candidate=>`<article class="candidate${preferred(candidate)?' preferred':''}" data-candidate="${esc(candidate.id)}"><h3>${esc(candidate.name)}${preferred(candidate)?`<span class="badge">${ties.length>1?'Tied maximum':'Preferred sample'}</span>`:''}</h3>${figure(candidate.image,'Concrete receiver interpretation')}<p>${esc(candidate.interpretation)}</p><dl class="weights"><div><dt>Prior</dt><dd>${esc(percent(candidate.prior))}<small>${esc(candidate.prior)}</small></dd></div><div><dt>Posterior</dt><dd>${esc(percent(candidate.posterior))}<small>${esc(candidate.posterior)}</small></dd></div></dl><div class="bar" role="img" aria-label="${esc(percent(candidate.posterior))} relative posterior weight"><span style="width:${width(candidate.posterior)}%"></span></div>${drawingNote(candidate.drawing)}<details><summary>Inspect local structure and audit</summary>${detail(candidate.drawing)}${detail(candidate.audit)}</details></article>`).join('')}</div></section><section><details><summary>Inspect native read diagnostics</summary>${detail(row.diagnostics)}</details></section></article>`;
}
choose.addEventListener('change',show);const requested=new URL(location.href).searchParams.get('case');choose.value=data.examples.some(x=>x.id===requested)?requested:(data.examples[0]?.id??'');show();
</script>
""";
    private const string Css = """
*{box-sizing:border-box}body{margin:0;background:#f1eee4;color:#283f49;font:15px/1.55 system-ui,sans-serif}header,main,.chooser{max-width:1660px;padding:25px 28px;margin:auto}header{padding-bottom:9px}h1{font:500 clamp(28px,4vw,45px)/1.2 Georgia,serif;margin:10px 0 18px}h2{font:500 27px/1.25 Georgia,serif;margin:10px 0 16px}h3{font-size:17px;font-weight:600;margin:12px 0}.eyebrow{font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#6d7d78}p{max-width:115ch}a{color:#355f78}nav{display:flex;gap:22px;flex-wrap:wrap}section{margin:25px 0 36px}.chooser{padding-top:10px;padding-bottom:3px}.chooser label{display:grid;gap:7px;font-size:13px;max-width:750px}select{font:inherit;color:inherit;background:#fffdf7;border:1px solid #a5b4b5;border-radius:5px;padding:11px 12px;width:100%;min-width:0}figure{margin:0;background:#fbf8ee}img{display:block;width:100%;height:auto}figcaption{overflow-wrap:anywhere;font-size:12px;padding:7px 10px;color:#6b7b76}.case-note{padding:15px 20px;border-left:4px solid #81918a;background:#e7ebdf}.comparison{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px;align-items:start}.comparison>article,.candidate,.tile{background:#fffdf7;border:1px solid #d3dacf;border-radius:5px;padding:17px;min-width:0}.comparison>article{border-top:5px solid #aeb7b7}.comparison>.sample,.candidate.preferred{border-top:5px solid #6b8aa4}.comparison p,.drawing-note{font-size:12px;color:#677b79}.legend{display:flex;align-items:center;flex-wrap:wrap;gap:8px;font-size:12px;color:#627775}.key{width:25px;border-top:3px solid #285762;display:inline-block;margin-left:8px}.key.assumed{border-color:#ab762e;border-top-style:dashed}.join-circle,.join-open,.join-question{display:inline-block;width:11px;height:11px;border:2px solid #4e8490;border-radius:50%;margin-left:9px}.join-open{border-color:#ab762e}.join-question{width:16px;height:16px;line-height:12px;text-align:center;border:1px dashed #8e849f;color:#76618d}.join-square,.join-diamond{display:inline-block;width:9px;height:9px;background:#285762;margin-left:9px}.join-diamond{background:#76618d;transform:rotate(45deg)}.interpretation-note{font-size:13px;color:#657b7b}.table-scroll{overflow:auto;max-width:100%;border:1px solid #d5dccf;border-radius:5px;background:#fffdf7}table{width:100%;border-collapse:collapse;text-align:left;font-size:13px}th,td{padding:12px 14px;border-bottom:1px solid #e0e4d8;vertical-align:top;overflow-wrap:anywhere}thead{background:#e6ebe5}tbody th{font-weight:600}.alternatives{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,330px),1fr));gap:20px;align-items:start}.candidate{border-top:5px solid #b4bfbd}.candidate h3{min-height:28px}.badge{display:inline-block;font-size:10px;padding:3px 7px;background:#dfe8ed;border-radius:3px;margin:0 0 5px 8px;white-space:nowrap;vertical-align:middle}.weights{display:grid;grid-template-columns:1fr 1fr;gap:20px;font-size:12px}.weights dt{color:#6a7c79}.weights dd{margin:0;font-size:22px;color:#4d7188}.weights small{display:block;font-size:11px;overflow-wrap:anywhere;max-width:100%}.bar{height:9px;background:#e3e9e9;border-radius:2px;overflow:hidden}.bar>span{display:block;height:100%;background:#7494ac}.feedback{font-size:12px;margin:12px 0 0;display:grid;gap:0 24px}.feedback>div{display:grid;grid-template-columns:minmax(90px,1fr) 1.35fr;gap:12px;padding:8px 0;border-top:1px solid #e0e4d8;min-width:0}.feedback dt{color:#647a7b}.feedback dd{margin:0;overflow-wrap:anywhere}details{margin-top:15px}summary{cursor:pointer}.run-summary{max-width:1604px;margin:18px auto;padding:20px 24px;border-radius:6px;background:#e7ebdf;width:calc(100% - 56px)}.metrics{display:grid;grid-template-columns:repeat(auto-fit,minmax(135px,1fr));gap:12px;margin:12px 0}.metrics>div{background:#fffdf5;padding:10px 13px;min-width:0}.metrics dt{font-size:12px;color:#617771;overflow-wrap:anywhere}.metrics dd{font-size:25px;margin:4px 0;overflow-wrap:anywhere}.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,520px),1fr));gap:22px}.tile h2{font-size:20px}.tile h2 a{text-decoration:none}.tile-images{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}.tile figcaption{font-size:10px;padding:5px 7px}.tile p{font-size:12px}a:focus-visible,select:focus-visible,summary:focus-visible{outline:3px solid #7196af;outline-offset:3px}@media(max-width:950px){.comparison{gap:10px}.comparison>article{padding:12px}.comparison h3{font-size:15px}}@media(max-width:700px){header,main,.chooser{padding:18px 14px}.comparison{grid-template-columns:1fr}.comparison>article{padding:14px}.comparison figure{max-width:460px;margin:auto}.run-summary{width:calc(100% - 28px);padding:16px}.metrics{grid-template-columns:repeat(2,minmax(0,1fr))}.metrics dd{font-size:22px}.tile{padding:12px}.tile-images{gap:3px}.tile figcaption{font-size:9px}.evidence{min-width:700px}.legend{gap:6px}.weights{gap:12px}.feedback>div{grid-template-columns:1fr 1.15fr}}
""";
}
