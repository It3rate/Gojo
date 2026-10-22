using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Presentation only. Coordinates are read for SVG output, never fed
/// back as evidence or used to rank reconstruction candidates.</summary>
public static class RelationalShapeGallery
{
    public sealed record Example(string Id, string Name, string Note, NativeLatticeSources.SourceCase Source,
        object Evidence, IReadOnlyList<Result> Results);
    public sealed record Result(string Id, string Name, NativeLatticeSources.SourceCase Output,
        Relation Assumptions, object Feedback, IReadOnlyList<Attempt> Attempts);
    public sealed record Attempt(int Index, string Name, NativeLatticeSources.SourceCase Output,
        Relation Assumptions, object Feedback, bool Selected);

    public static void Write(string output, object summary, IReadOnlyList<Example> examples)
    {
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var rows = new List<object>(); var tiles = new StringBuilder();
        foreach (var example in examples)
        {
            var token = Token(example.Id); var evidence = JsonSerializer.SerializeToElement(example.Evidence, Json);
            var labels = Labels(evidence, E(example.Source.Graph.Sites).Length);
            var sourceImage = $"patterns/{token}-source.svg";
            Save(sourceImage, example.Source, StructuralConstraint.Closed(), example.Name);
            var results = example.Results.Select(result =>
            {
                var profile = Token(result.Id); var image = $"patterns/{token}-{profile}-selected.svg";
                Save(image, result.Output, result.Assumptions, result.Name);
                var attempts = result.Attempts.Select(attempt =>
                {
                    var attemptImage = $"patterns/{token}-{profile}-attempt-{attempt.Index}.svg";
                    Save(attemptImage, attempt.Output, attempt.Assumptions, attempt.Name);
                    return new { index = attempt.Index, name = attempt.Name, image = attemptImage,
                        assumptions = E(attempt.Assumptions).Length, feedback = attempt.Feedback, selected = attempt.Selected };
                }).ToArray();
                return new { id = result.Id, name = result.Name, image, assumptions = E(result.Assumptions).Length,
                    feedback = result.Feedback, attempts };
            }).ToArray();
            rows.Add(new { id = example.Id, name = example.Name, note = example.Note, sourceImage, evidence = example.Evidence, results });
            var href = "gallery.html?case=" + Uri.EscapeDataString(example.Id);
            tiles.Append($"<article class='tile'><h2><a href='{href}'>{H(example.Id)} · {H(example.Name)}</a></h2><div class='tile-images'><figure><a href='{href}'><img src='{sourceImage}' alt='Reference source' loading='lazy'></a><figcaption>Source</figcaption></figure>");
            foreach (var result in results)
                tiles.Append($"<figure><a href='{href}&amp;profile={Uri.EscapeDataString(result.id)}'><img src='{result.image}' alt='{H(result.name)}' loading='lazy'></a><figcaption>{H(result.name)}</figcaption></figure>");
            tiles.Append($"</div><p>{H(example.Note)}</p></article>");

            void Save(string relative, NativeLatticeSources.SourceCase source, Relation assumptions, string name) =>
                JointReconstructionGallery.WriteText(Path.Combine(output, relative), JointReconstructionGallery.Svg(source, labels, assumptions, name));
        }
        var data = JsonSerializer.Serialize(new { schema = "relational-shape-gallery-v1", summary, examples = rows }, Json);
        JointReconstructionGallery.WriteText(Path.Combine(output, "gallery-data.json"), data);
        var metrics = Summary(JsonSerializer.SerializeToElement(summary, Json));
        JointReconstructionGallery.WriteText(Path.Combine(output, "gallery.html"), Page("Gojo · relational shape reconstruction", """
<header><p class="eyebrow">Gojo · pre-numeric composition</p><h1>Shapes from joints, equivalences and revisable context</h1>
<p>Each source is built before its joints and relations are read. A separate receiver proposes new connected shapes from these messages. The reference passages and measured display coordinates are withheld from construction.</p>
<nav><a href="overview.html">All cases and selected samples</a><a href="gallery-data.json">Inspection data</a></nav></header>
METRICS
<section class="chooser"><label for="case">Source case</label><select id="case"></select></section><main id="content"></main>
<script>
globalThis.RELATIONAL_SHAPES=PAYLOAD;
const data=globalThis.RELATIONAL_SHAPES,select=document.querySelector('#case'),content=document.querySelector('#content');
let activeProfile=new URL(location.href).searchParams.get('profile');
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const value=v=>Array.isArray(v)?v.map(value).join(' · '):(v&&typeof v==='object'?Object.entries(v).map(([k,w])=>`${human(k)}: ${value(w)}`).join('; '):String(v??'—'));
const human=s=>String(s).replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,c=>c.toUpperCase());
const figure=(src,caption)=>`<figure><img src="${esc(src)}" alt="${esc(caption)}"><figcaption>${esc(caption)}</figcaption></figure>`;
const feedback=f=>`<dl class="feedback">${Object.entries(f??{}).map(([k,v])=>`<div><dt>${esc(human(k))}</dt><dd>${esc(value(v))}</dd></div>`).join('')}</dl>`;
const groups=xs=>Array.isArray(xs)&&xs.length?`<ul class="group-list">${xs.map(x=>`<li>${esc(value(x))}</li>`).join('')}</ul>`:'<p class="muted">No group published.</p>';
const streamTable=rows=>rows.length?`<table><thead><tr><th>Claim</th><th>Support</th><th>Doubt</th></tr></thead><tbody>${rows.map(s=>`<tr><th>${esc(value(s.claim))}</th><td>${esc(value(s.support))}</td><td>${esc(value(s.opposition??s.doubt))}</td></tr>`).join('')}</tbody></table>`:'<p class="muted">No stream published in this context.</p>';
const contextKind=s=>/observ|evidence/i.test(s)?'observation':/hypoth/i.test(s)?'hypothesis':/default/i.test(s)?'default':/goal|desir/i.test(s)?'goal':'other';
for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=`${row.id} · ${row.name}`;select.append(option);}
function show(){const row=data.examples.find(x=>x.id===select.value)??data.examples[0];if(!row){content.innerHTML='<p>No inspected cases.</p>';return;}
 select.value=row.id;const result=row.results.find(x=>x.id===activeProfile)??row.results[0];activeProfile=result?.id??null;
 const url=new URL(location.href);url.searchParams.set('case',row.id);if(activeProfile)url.searchParams.set('profile',activeProfile);else url.searchParams.delete('profile');history.replaceState(null,'',url);
 const e=row.evidence??{},streams=e.streams??[],contexts=[['observation','Evidence','Retained observations about the current source.'],['hypothesis','Hypotheses','Proposed explanations; alternatives can remain together.'],['default','Defaults','Revisable completion when the message leaves a choice open.'],['goal','Goals','Desired outcomes; these do not assert current truth.']];
 content.innerHTML=`<h2>${esc(row.id)} · ${esc(row.name)}</h2><p class="note">${esc(row.note)}</p>
 <section class="reference"><h2>Reference source</h2><div class="reference-grid">${figure(row.sourceImage,'Original independently executed source')}<div><p>The source is visible here for inspection. Its original passages and position words are held back from the receiver.</p><p>J1, J2 and later labels are anonymous role names. Their listing is neither a path nor an inherent joint order. Spatial relations say above, below, left, right or aligned without specifying distance.</p><p class="muted">Solid ink shows actual candidate passages. Dashed amber shows a proposed bridge or other explicitly retained assumption. Neither the palette nor a selected badge means “true”.</p></div></div></section>
 <section class="message"><h2>Independent communication streams</h2><p>Length equivalence, angle equivalence and coarse angle similarity are separate relations. They do not supply a measurement. Graded support and doubt remain independent, so a stream can support a familiar reading while retaining conflicting detail.</p>
 <div class="evidence-grid"><article><h3>Joints and local rays</h3><div class="joint-list">${(e.joints??[]).map(j=>`<div><strong>${esc(j.id)}</strong><span>${esc(value(j.kind))}</span><span class="muted">${esc(value(j.rays))}</span></div>`).join('')||'<p>No joint inventory published.</p>'}</div></article><article><h3>Relative locations</h3><p>${esc(value(e.spatialSummary))}</p><h3>Length equivalence groups</h3>${groups(e.lengthGroups)}<h3>Angle equivalence groups</h3>${groups(e.angleGroups)}</article></div>
 ${e.symmetries?`<article class="symmetry"><h3>Symmetry correspondences</h3><p>${esc(value(e.symmetries))}</p></article>`:''}
 <div class="contexts">${contexts.map(([key,name,note])=>`<article class="context ${key}"><h3>${name}</h3><p class="muted">${note}</p><div class="table-scroll">${streamTable(streams.filter(s=>contextKind(s.context??'')===key))}</div></article>`).join('')}</div>
 ${streams.some(s=>contextKind(s.context??'')==='other')?`<article><h3>Other published contexts</h3>${streamTable(streams.filter(s=>contextKind(s.context??'')==='other'))}</article>`:''}
 <article class="closure"><h3>Closure context</h3>${feedback(e.closure??{})}<p class="muted">A closed-area default can be opposed by a known opening. “Closed parent with damage” and “genuinely open path” can remain alternative explanations; a repair goal requests a new passage rather than declaring that passage observed.</p></article>
 <p class="muted">${esc(value(e.notes))}</p></section>
 <section><h2>Selected samples under different contexts</h2><p>These are alternative receiver readings of the same communication. A profile can change which defaults participate or which constraints are available.</p><div class="profiles">${row.results.map(r=>`<article class="profile ${r.id===activeProfile?'active':''}" data-profile="${esc(r.id)}"><h3>${esc(r.name)}</h3>${figure(r.image,`${row.id} · ${r.name}`)}<p class="assumption-label">${r.assumptions} assumed passage${r.assumptions===1?'':'s'}</p><button type="button" class="profile-choice" data-profile="${esc(r.id)}" aria-pressed="${r.id===activeProfile}">Inspect attempts</button>${feedback(r.feedback)}</article>`).join('')}</div></section>
 <section class="attempt-section" data-profile="${esc(activeProfile??'')}"><h2>${esc(result?.name??'')} · retained attempts</h2><p>Three inspection samples are retained from this profile’s bounded search. The selected badge records the receiver’s preference under its current claims; comparison with the reference is a separate audit.</p><div class="attempts">${(result?.attempts??[]).map(a=>`<article class="attempt ${a.selected?'selected':''}"><span class="badge">${a.selected?'Selected sample':'Retained alternative'}</span><h3>${esc(a.index)} · ${esc(a.name)}</h3>${figure(a.image,`${row.id} · ${result?.name} · attempt ${a.index}`)}<p class="assumption-label">${a.assumptions} assumed passage${a.assumptions===1?'':'s'}</p>${feedback(a.feedback)}</article>`).join('')}</div></section>`;
 for(const button of content.querySelectorAll('.profile-choice'))button.addEventListener('click',()=>{activeProfile=button.dataset.profile;show();});
}
select.addEventListener('change',show);const requested=new URL(location.href).searchParams.get('case');select.value=data.examples.some(x=>x.id===requested)?requested:(data.examples[0]?.id??'');show();
</script>
""".Replace("METRICS", metrics, StringComparison.Ordinal).Replace("PAYLOAD", data, StringComparison.Ordinal)));
        JointReconstructionGallery.WriteText(Path.Combine(output, "overview.html"), Page("Gojo · relational shape atlas",
            "<header><p class='eyebrow'>Gojo · pre-numeric shape descriptions</p><h1>Reference shapes and revisable reconstructions</h1><p>Each card shows an independent source and the selected samples produced under different communication contexts. Click any sample for its evidence streams and retained attempts. Dashed amber passages are assumptions.</p><nav><a href='gallery.html'>Detailed inspection</a><a href='gallery-data.json'>Inspection data</a></nav></header>" + metrics + "<main class='tiles'>" + tiles + "</main>"));
    }

    private static string[] Labels(JsonElement evidence, int count)
    {
        var labels = evidence.TryGetProperty("joints", out var joints) && joints.ValueKind == JsonValueKind.Array
            ? joints.EnumerateArray().Select(joint => joint.TryGetProperty("id", out var id) ? id.GetString() : null).ToArray() : [];
        return Enumerable.Range(0, count).Select(index => index < labels.Length && labels[index] is string label ? label : "J" + (index + 1)).ToArray();
    }
    private static string Summary(JsonElement summary)
    {
        var html = new StringBuilder("<section class='run-summary'><h2>Run measurements</h2><dl class='metrics'>");
        foreach (var field in summary.EnumerateObject().Where(field => field.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            html.Append($"<div><dt>{H(Human(field.Name))}</dt><dd>{H(field.Value.ToString())}</dd></div>");
        html.Append("</dl><details><summary>Scope and limitations</summary>");
        foreach (var field in summary.EnumerateObject().Where(field => field.Value.ValueKind == JsonValueKind.String))
            html.Append($"<p><strong>{H(Human(field.Name))}:</strong> {H(field.Value.GetString() ?? "")}</p>");
        return html.Append("<p>Confidence is an ordinal support/doubt read, not a calibrated probability. Reconstruction policies and defaults remain exploratory; this run does not train a general model.</p></details></section>").ToString();
    }
    private static string Page(string title, string body) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + H(title) + "</title><style>" + Css + "</style></head><body>" + body + "</body></html>";
    private static string Token(string text) => new(text.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string Human(string text) => System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private const string Css = """
*{box-sizing:border-box}body{margin:0;background:#f3f0e6;color:#263f49;font:15px/1.55 system-ui,sans-serif}header,main,.chooser{max-width:1660px;padding:25px 28px;margin:auto}header{padding-bottom:8px}h1{font:500 clamp(28px,4vw,45px)/1.2 Georgia,serif;margin:10px 0 18px}h2{font:500 27px/1.25 Georgia,serif;margin:10px 0 16px}h3{font-size:17px;font-weight:600;margin:7px 0 13px}p{max-width:110ch}.eyebrow{font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#6d7d78}a{color:#355f78}nav{display:flex;flex-wrap:wrap;gap:24px}section{margin:24px 0 40px}.chooser{display:flex;gap:18px;align-items:center;padding-top:6px;padding-bottom:6px}select,button{font:inherit;color:inherit;background:#fffdf7;border:1px solid #a5b4b5;border-radius:5px;padding:10px 13px;max-width:100%}select{min-width:0}button{cursor:pointer}button:hover{background:#e9eef0}button[aria-pressed=true]{background:#dce7ee;border-color:#6b8fa4}.note{padding:15px 20px;border-left:4px solid #82928a;background:#e9ecdf}.reference{background:#e9e5d8;padding:24px;border-radius:6px}.reference-grid{display:grid;grid-template-columns:minmax(250px,430px) 1fr;gap:28px;align-items:center}figure{margin:0;background:#fbf9f0}img{display:block;width:100%;height:auto}figcaption{font-size:12px;padding:7px 10px;color:#6c7d78}.message{background:#edece1;padding:24px;border-radius:6px}.evidence-grid{display:grid;grid-template-columns:1fr 1fr;gap:22px}.evidence-grid>article,.context,.closure,.symmetry{padding:18px;background:#fffdf7;border:1px solid #d7dccf;border-radius:5px;min-width:0}.joint-list{max-height:380px;overflow:auto}.joint-list>div{display:grid;grid-template-columns:42px 1fr;gap:3px 9px;border-bottom:1px solid #e5e8db;padding:8px 0}.joint-list .muted{grid-column:2}.group-list{margin:8px 0 20px;padding-left:22px;overflow-wrap:anywhere}.group-list li{margin:6px 0}.contexts{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px;margin-top:22px}.context{border-top:4px solid #8ba5ae}.context.hypothesis{border-top-color:#a495b5}.context.default{border-top-color:#b6a681}.context.goal{border-top-color:#7d8d9c}.context p{font-size:12px;min-height:40px}.table-scroll{overflow:auto;max-width:100%}table{border-collapse:collapse;width:100%;font-size:12px}th,td{padding:8px 6px;border-bottom:1px solid #e2e5da;vertical-align:top;text-align:left;overflow-wrap:anywhere}th{font-weight:600}.closure,.symmetry{margin-top:20px}.muted{color:#71807c;font-size:13px}.profiles{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:18px;align-items:start}.profile,.attempt,.tile{background:#fffdf7;border:1px solid #d3dacf;border-radius:5px;padding:16px;min-width:0}.profile{border-top:5px solid #acb6b7}.profile.active{border-top-color:#617f9b}.profile h3{min-height:52px}.profile-choice{width:100%;font-size:13px}.assumption-label{color:#8c6b3e;font-size:12px;margin:9px 0}.attempts{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px}.attempt{border-top:5px solid #b0babc}.attempt.selected{border-top-color:#6187a3}.badge{font-size:12px;display:inline-block;padding:4px 8px;border-radius:4px;background:#e8eceb;margin-bottom:12px}.selected .badge{background:#dfe9f0}.feedback{font-size:12px;margin:12px 0 0}.feedback>div{display:grid;grid-template-columns:minmax(90px,1fr) 1.2fr;gap:8px;padding:8px 0;border-top:1px solid #e3e6db}.feedback dt{color:#647a7b}.feedback dd{margin:0;overflow-wrap:anywhere}.run-summary{max-width:1604px;margin:18px auto;padding:20px 24px;border-radius:6px;background:#e8ecdf;width:calc(100% - 56px)}.metrics{display:grid;grid-template-columns:repeat(auto-fit,minmax(145px,1fr));gap:12px;margin:12px 0}.metrics>div{background:#fffdf5;padding:10px 13px;min-width:0}.metrics dt{color:#617771;font-size:12px;overflow-wrap:anywhere}.metrics dd{font-size:25px;margin:4px 0;overflow-wrap:anywhere}details{margin-top:16px}summary{cursor:pointer}.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,520px),1fr));gap:22px}.tile h2{font-size:21px}.tile h2 a{text-decoration:none}.tile-images{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}.tile-images figcaption{font-size:10px;padding:5px 7px}.tile p{font-size:12px}.tile img{min-height:1px}[hidden]{display:none!important}@media(max-width:1200px){.contexts{grid-template-columns:1fr 1fr}.profiles{grid-template-columns:1fr 1fr}.profile h3{min-height:0}}@media(max-width:760px){header,main,.chooser{padding:18px 14px}.chooser{display:block}.chooser label{display:block;margin-bottom:8px}select{width:100%}.reference,.message{padding:16px}.reference-grid,.evidence-grid,.contexts,.attempts{grid-template-columns:1fr}.profiles{gap:10px}.profile{padding:10px}.profile .feedback>div{grid-template-columns:1fr;gap:3px}.profile h3{font-size:14px}.profile figcaption{font-size:10px}.profile button{font-size:12px;padding:8px}.run-summary{width:calc(100% - 28px);padding:15px}.metrics{grid-template-columns:repeat(2,minmax(0,1fr))}.tile{padding:12px}.tile-images{grid-template-columns:repeat(2,minmax(0,1fr))}}
""";
}
