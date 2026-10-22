using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Presentation over native weighted reads. Measured display positions,
/// fractions and percentages are never returned to reconstruction or weighting.</summary>
public static class WeightedShapeGallery
{
    public sealed record Example(string Id, string Name, string Note, NativeLatticeSources.SourceCase Source,
        object Evidence, IReadOnlyList<Result> Results);
    public sealed record Result(string Id, string Name, string Premise, string BaselineId, string SelectedId,
        IReadOnlyList<Candidate> Candidates, IReadOnlyList<Evidence> Evidence, object Diagnostics);
    public sealed record Candidate(string Id, string Name, NativeLatticeSources.SourceCase Output,
        Relation AssumedStrokes, string Prior, string Posterior, object Audit);
    public sealed record Evidence(string Name, string Outcome, string Family, string Estimate, string Note);

    public static void Write(string output, object summary, IReadOnlyList<Example> examples)
    {
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var rows = new List<object>(); var tiles = new StringBuilder();
        foreach (var example in examples)
        {
            var token = Token(example.Id); var sourceImage = $"patterns/{token}-source.svg";
            Save(sourceImage, example.Source, StructuralConstraint.Closed(), example.Name);
            var results = example.Results.Select(result =>
            {
                var candidates = result.Candidates.Select(candidate =>
                {
                    var image = $"patterns/{token}-{Token(result.Id)}-{Token(candidate.Id)}.svg";
                    Save(image, candidate.Output, candidate.AssumedStrokes, candidate.Name);
                    return new { candidate.Id, candidate.Name, Image = image, candidate.Prior, candidate.Posterior,
                        Assumptions = E(candidate.AssumedStrokes).Length, candidate.Audit };
                }).ToArray();
                return new { result.Id, result.Name, result.Premise, result.BaselineId, result.SelectedId,
                    Candidates = candidates, result.Evidence, result.Diagnostics };
            }).ToArray();
            rows.Add(new { example.Id, example.Name, example.Note, SourceImage = sourceImage, example.Evidence, Results = results });
            var selected = results.FirstOrDefault();
            var baseline = selected?.Candidates.FirstOrDefault(candidate => candidate.Id == selected.BaselineId);
            var weighted = selected?.Candidates.FirstOrDefault(candidate => candidate.Id == selected.SelectedId);
            var href = "gallery.html?case=" + Uri.EscapeDataString(example.Id);
            tiles.Append($"<article class='tile'><h2><a href='{href}'>{H(example.Id)} · {H(example.Name)}</a></h2><div class='tile-images'>");
            TileImage(sourceImage, "Reference source");
            if (baseline is not null) TileImage(baseline.Image, "Baseline preference");
            if (weighted is not null) TileImage(weighted.Image, $"Weighted preference · {H(weighted.Posterior)}");
            tiles.Append("</div>");
            if (selected is not null)
                tiles.Append($"<p class='selection-note'>{H(selected.Name)} · {(selected.BaselineId == selected.SelectedId ? "Same preferred sample" : "Preference changed")}</p>");
            tiles.Append($"<p>{H(example.Note)}</p></article>");

            void TileImage(string image, string caption) => tiles.Append($"<figure><a href='{href}'><img src='{image}' alt='{H(caption)}' loading='lazy'></a><figcaption>{caption}</figcaption></figure>");
            void Save(string image, NativeLatticeSources.SourceCase source, Relation assumptions, string name) =>
                JointReconstructionGallery.WriteText(Path.Combine(output, image),
                    JointReconstructionGallery.Svg(source, Enumerable.Range(1, E(source.Graph.Sites).Length).Select(index => "J" + index).ToArray(), assumptions, name));
        }
        var data = JsonSerializer.Serialize(new { Schema = "weighted-shape-gallery-v1", summary, Examples = rows }, Json);
        JointReconstructionGallery.WriteText(Path.Combine(output, "gallery-data.json"), data);
        var metrics = Metrics(JsonSerializer.SerializeToElement(summary, Json));
        JointReconstructionGallery.WriteText(Path.Combine(output, "gallery.html"), Page("Gojo · weighted shape evidence", Header + metrics + Inspector.Replace("PAYLOAD", data, StringComparison.Ordinal)));
        JointReconstructionGallery.WriteText(Path.Combine(output, "overview.html"), Page("Gojo · weighted shape atlas", Header + metrics + "<main class='tiles'>" + tiles + "</main>"));
    }

    private static string Metrics(JsonElement summary)
    {
        var html = new StringBuilder("<section class='run-summary'><h2>Run measurements</h2><dl class='metrics'>");
        foreach (var property in summary.EnumerateObject().Where(property => property.Value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False))
            html.Append($"<div><dt>{H(Human(property.Name))}</dt><dd>{H(property.Value.ToString())}</dd></div>");
        html.Append("</dl><details><summary>Premises and limits</summary>");
        foreach (var property in summary.EnumerateObject().Where(property => property.Value.ValueKind == JsonValueKind.String))
            html.Append($"<p><strong>{H(Human(property.Name))}:</strong> {H(property.Value.GetString() ?? "")}</p>");
        return html.Append("<p>Posterior weights are conditional on the published candidate set and authored likelihood estimates. They are not learned or calibrated probabilities of being the true shape. The reference comparison is an audit, not an input to inference.</p></details></section>").ToString();
    }
    private static string Page(string title, string body) => "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>" + H(title) + "</title><style>" + Css + "</style></head><body>" + body + "</body></html>";
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string Human(string text) => System.Text.RegularExpressions.Regex.Replace(text, "([a-z])([A-Z])", "$1 $2");
    private static string Token(string text) => new(text.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private const string Header = """
<header><p class="eyebrow">Gojo · statistical calculation on relational shapes</p><h1>What changes when evidence has weight?</h1><p>The same pre-numeric messages and bounded candidate proposals are read by two preference rules: the existing conflict ordering, and native Bayesian conditioning under explicit likelihood estimates. The diagrams show reconstructions, not inputs to a pixel classifier.</p><nav><a href="gallery.html">Detailed inspection</a><a href="overview.html">All sources and preferences</a><a href="gallery-data.json">Inspection data</a><a href="summary.json">Run data</a></nav></header>
""";
    private const string Inspector = """
<section class="chooser"><label>Source case<select id="case"></select></label><label>Evidence context<select id="profile"></select></label></section><main id="content"></main>
<script>
globalThis.WEIGHTED_SHAPES=PAYLOAD;
const data=globalThis.WEIGHTED_SHAPES,select=document.querySelector('#case'),profiles=document.querySelector('#profile'),content=document.querySelector('#content');
let requestedProfile=new URL(location.href).searchParams.get('profile');
const esc=s=>String(s??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const human=s=>String(s).replace(/([a-z])([A-Z])/g,'$1 $2').replace(/^./,c=>c.toUpperCase());
const text=v=>Array.isArray(v)?v.map(text).join(' · '):v&&typeof v==='object'?Object.entries(v).map(([k,x])=>`${human(k)}: ${text(x)}`).join('; '):String(v??'—');
const detail=v=>`<dl class="feedback">${Object.entries(v??{}).map(([k,x])=>`<div><dt>${esc(human(k))}</dt><dd>${esc(text(x))}</dd></div>`).join('')}</dl>`;
const fraction=s=>{try{const bits=String(s).split('/');if(bits.length!==2)return null;const n=BigInt(bits[0]),d=BigInt(bits[1]);return d>0n&&n>=0n?{n,d}:null;}catch{return null;}};
const compare=(a,b)=>{const x=fraction(a),y=fraction(b);return !x||!y?0:x.n*y.d>y.n*x.d?1:x.n*y.d<y.n*x.d?-1:0;};
const percent=s=>{const q=fraction(s);if(!q)return 'Open';const p=Number(q.n*10000n/q.d)/100;return p===0&&q.n>0n?'<0.01%':`${p.toFixed(2).replace(/\.00$/,'')}%`;};
const width=s=>{const q=fraction(s);return q?Math.min(100,Number(q.n*10000n/q.d)/100):0;};
const figure=(src,caption)=>`<figure><img src="${esc(src)}" alt="${esc(caption)}"><figcaption>${esc(caption)}</figcaption></figure>`;
const auditLine=c=>{const a=c?.audit??{},s=a.sourceConnectivity??{};return `source edges ${esc(s.matched??'—')}/${esc(s.expected??'—')}; ${esc(s.additional??'—')} other edges; ${esc(a.hardConflicts??'—')} structural conflicts; ${esc(a.unpairedRays??'—')} unpaired rays`;};
for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=`${row.id} · ${row.name}`;select.append(option);}
function showCase(){const row=data.examples.find(x=>x.id===select.value)??data.examples[0];if(!row){content.innerHTML='<p>No inspected cases.</p>';return;}select.value=row.id;profiles.innerHTML='';for(const result of row.results){const option=document.createElement('option');option.value=result.id;option.textContent=result.name;profiles.append(option);}profiles.value=row.results.some(x=>x.id===requestedProfile)?requestedProfile:row.results[0]?.id??'';show();}
function show(){const row=data.examples.find(x=>x.id===select.value),result=row?.results.find(x=>x.id===profiles.value);if(!row||!result)return;requestedProfile=result.id;
const url=new URL(location.href);url.searchParams.set('case',row.id);url.searchParams.set('profile',result.id);history.replaceState(null,'',url);
const baseline=result.candidates.find(x=>x.id===result.baselineId),selected=result.candidates.find(x=>x.id===result.selectedId),best=result.candidates.reduce((a,b)=>compare(a.posterior,b.posterior)>=0?a:b,result.candidates[0]);
const ties=best?result.candidates.filter(x=>compare(x.posterior,best.posterior)===0):[],changed=result.baselineId!==result.selectedId;
content.innerHTML=`<article class="case" data-case="${esc(row.id)}" data-profile="${esc(result.id)}"><h2>${esc(row.id)} · ${esc(row.name)}</h2><p class="note">${esc(row.note)}</p>
<section class="comparison"><article>${figure(row.sourceImage,'Reference source')}<h3>Independent source</h3><p>Shown for inspection. Original endpoint pairings, constructor order and teacher family are withheld from candidate generation.</p></article>
<article class="baseline">${baseline?figure(baseline.image,baseline.name):''}<h3>Baseline preference</h3><p>${esc(baseline?.name??'None retained')}</p><p class="muted">Conflict ordering before statistical weighting.</p></article>
<article class="weighted">${selected?figure(selected.image,selected.name):''}<h3>Weighted preference</h3><p>${esc(selected?.name??'No complete read')} · <strong>${esc(percent(selected?.posterior))}</strong></p><p class="muted">${changed?'A different sample is preferred.':'The same sample remains preferred.'} ${ties.length>1?`${ties.length} displayed samples share the largest weight; a selected representative does not resolve the tie.`:''}</p></article></section>
<section class="audit-readout"><h3>Separate connectivity and consistency audit</h3><p>Matching the source’s endpoint pairings does not establish matching placement, containment, proportions or valid geometry. A weighted preference can match more source edges while violating more structural claims.</p><dl class="feedback"><div><dt>Baseline preference</dt><dd>${auditLine(baseline)}</dd></div><div><dt>Weighted preference</dt><dd>${auditLine(selected)}</dd></div></dl></section>
<p class="legend"><span class="ink-key"></span> Constructed passage <span class="assumed-key"></span> Assumed passage. Blue marks a preference or relative weight; it does not mean “yes” or “true”.</p>
<section class="model"><h2>${esc(result.name)}</h2><p>${esc(result.premise)}</p><p class="caution">These weights distribute support within this supplied candidate set. An omitted candidate cannot receive weight here. Authored estimates test the calculation and sensitivity; they do not establish real-world calibration.</p>${detail(result.diagnostics)}</section>
<section><h2>Displayed candidate weights</h2><p>Up to six candidates are shown. Weights are normalized across the whole distinct proposal pool, so the displayed fractions need not sum to one. The fractions are exact derived reads; bars and percentages are display rounding. A large posterior can coexist with a poor candidate set. Reference comparisons below are audit only.</p><div class="table-scroll"><table class="ranking"><thead><tr><th>Candidate</th><th>Prior</th><th>Posterior</th><th>Relative mass</th><th>Preference</th></tr></thead><tbody>${result.candidates.map(candidate=>`<tr data-candidate="${esc(candidate.id)}"><th>${esc(candidate.name)}</th><td><span class="fraction">${esc(candidate.prior)}</span><small>${esc(percent(candidate.prior))}</small></td><td><span class="fraction">${esc(candidate.posterior)}</span><small>${esc(percent(candidate.posterior))}</small></td><td><div class="bar" role="img" aria-label="${esc(percent(candidate.posterior))} posterior mass"><span style="width:${width(candidate.posterior)}%"></span></div></td><td>${candidate.id===result.baselineId?'<span class="badge">Baseline</span>':''}${candidate.id===result.selectedId?'<span class="badge weighted-badge">Weighted</span>':''}${ties.length>1&&ties.some(x=>x.id===candidate.id)?'<span class="badge">Tied maximum</span>':''}</td></tr>`).join('')}</tbody></table></div></section>
<section class="evidence-section"><h2>Published evidence and estimated likelihoods</h2><p>Reports from the same structural group are handled under the stated grouping law. A new route to the same event is not another acquisition. Goals and defaults remain distinct from observations.</p><div class="table-scroll"><table class="evidence"><thead><tr><th>Read</th><th>Report</th><th>Group</th><th>Estimate / assumption</th><th>Explanation</th></tr></thead><tbody>${result.evidence.map(e=>`<tr><th>${esc(e.name)}</th><td>${esc(e.outcome)}</td><td>${esc(e.family)}</td><td>${esc(e.estimate)}</td><td>${esc(e.note)}</td></tr>`).join('')}</tbody></table></div></section>
<section><h2>Concrete alternatives and separate audit</h2><p>Different structures can satisfy the same communicated information. Dashed passages show where the receiver completed missing structure. Neither preference rule receives the reference edge comparison.</p><div class="candidates">${result.candidates.map(candidate=>`<article class="candidate"><h3>${esc(candidate.name)}</h3>${figure(candidate.image,`${candidate.id} · concrete reconstruction`)}<p class="assumption-label">${candidate.assumptions} assumed passage${candidate.assumptions===1?'':'s'} · posterior ${esc(percent(candidate.posterior))}</p><details><summary>Inspect structural feedback and audit</summary>${detail(candidate.audit)}</details></article>`).join('')}</div></section><section><details><summary>Inspect communicated source evidence</summary>${detail(row.evidence)}</details></section></article>`;
}
select.addEventListener('change',showCase);profiles.addEventListener('change',show);const requested=new URL(location.href).searchParams.get('case');select.value=data.examples.some(x=>x.id===requested)?requested:(data.examples[0]?.id??'');showCase();
</script>
""";
    private const string Css = """
*{box-sizing:border-box}body{margin:0;background:#f3f0e6;color:#263f49;font:15px/1.55 system-ui,sans-serif}header,main,.chooser{max-width:1660px;padding:25px 28px;margin:auto}header{padding-bottom:8px}h1{font:500 clamp(28px,4vw,45px)/1.2 Georgia,serif;margin:10px 0 18px}h2{font:500 27px/1.25 Georgia,serif;margin:10px 0 16px}h3{font-size:17px;font-weight:600;margin:12px 0}p{max-width:115ch}.eyebrow{font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#6d7d78}a{color:#355f78}nav{display:flex;gap:22px;flex-wrap:wrap}section{margin:25px 0 36px}.chooser{display:grid;grid-template-columns:1fr 1fr;gap:22px;padding-top:10px;padding-bottom:4px}.chooser label{display:grid;gap:8px;font-size:13px}select{font:inherit;color:inherit;background:#fffdf7;border:1px solid #a5b4b5;border-radius:5px;padding:11px 12px;width:100%;min-width:0}figure{margin:0;background:#fbf9f0}img{display:block;width:100%;height:auto}figcaption{overflow-wrap:anywhere;font-size:12px;padding:7px 10px;color:#6c7d78}.note{padding:15px 20px;border-left:4px solid #82928a;background:#e9ecdf}.comparison{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:20px;align-items:start}.comparison>article,.candidate,.model,.tile{background:#fffdf7;border:1px solid #d3dacf;border-radius:5px;padding:17px;min-width:0}.comparison>article{border-top:5px solid #acb6b7}.comparison>.baseline{border-top-color:#7b8f99}.comparison>.weighted{border-top-color:#6586a5}.comparison p{font-size:13px}.muted{color:#667c7c;font-size:13px}.legend{display:flex;flex-wrap:wrap;align-items:center;gap:8px;font-size:12px;color:#607674}.ink-key,.assumed-key{width:26px;display:inline-block;border-top:3px solid #265661}.assumed-key{border-color:#b57b29;border-top-style:dashed;margin-left:14px}.caution{padding:12px 16px;background:#f1eddf;border-left:3px solid #a39872;font-size:13px}.feedback{font-size:12px;margin:12px 0 0;display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:0 24px}.feedback>div{display:grid;grid-template-columns:minmax(90px,1fr) 1.35fr;gap:12px;padding:8px 0;border-top:1px solid #e0e4d8;min-width:0}.feedback dt{color:#647a7b}.feedback dd{margin:0;overflow-wrap:anywhere}.table-scroll{overflow:auto;max-width:100%;border:1px solid #d5dccf;border-radius:5px;background:#fffdf7}table{width:100%;border-collapse:collapse;text-align:left;font-size:13px}th,td{padding:12px 14px;border-bottom:1px solid #e0e4d8;vertical-align:top;overflow-wrap:anywhere}thead{background:#e6ebe5}tbody th{font-weight:600}small{display:block;color:#6d7c79;font-size:11px;margin-top:4px}.fraction{display:inline-block;max-width:250px;overflow-wrap:anywhere;font-variant-numeric:tabular-nums}.bar{width:150px;max-width:100%;height:13px;background:#e1e7e8;border-radius:2px;overflow:hidden;margin-top:5px}.bar>span{display:block;height:100%;background:#6d90ac}.badge{display:inline-block;font-size:10px;padding:3px 7px;background:#e8eceb;border-radius:3px;margin:0 5px 5px 0;white-space:nowrap}.weighted-badge{background:#dbe8f1}.candidates{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,330px),1fr));gap:20px;align-items:start}.candidate .feedback{grid-template-columns:1fr}.assumption-label{font-size:12px;color:#8c6b3e}details{margin-top:15px}summary{cursor:pointer}.run-summary{max-width:1604px;margin:18px auto;padding:20px 24px;border-radius:6px;background:#e8ecdf;width:calc(100% - 56px)}.metrics{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:12px;margin:12px 0}.metrics>div{background:#fffdf5;padding:10px 13px;min-width:0}.metrics dt{color:#617771;font-size:12px;overflow-wrap:anywhere}.metrics dd{font-size:25px;margin:4px 0;overflow-wrap:anywhere}.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,520px),1fr));gap:22px}.tile h2{font-size:20px}.tile h2 a{text-decoration:none}.tile-images{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}.tile figcaption{font-size:10px;padding:5px 7px}.tile p{font-size:12px}.selection-note{font-weight:600;color:#627d8c}a:focus-visible,select:focus-visible,summary:focus-visible{outline:3px solid #7196af;outline-offset:3px}@media(max-width:950px){.comparison{gap:10px}.comparison>article{padding:12px}.feedback{grid-template-columns:1fr}.comparison h3{font-size:15px}}@media(max-width:700px){header,main,.chooser{padding:18px 14px}.chooser{grid-template-columns:1fr;gap:12px}.comparison{grid-template-columns:1fr}.comparison>article{padding:14px}.comparison figure{max-width:460px;margin:auto}.model{padding:15px}.run-summary{width:calc(100% - 28px);padding:16px}.metrics{grid-template-columns:repeat(2,minmax(0,1fr))}.metrics dd{font-size:22px}.tile{padding:12px}.tile-images{gap:3px}.tile figcaption{font-size:9px}.fraction{max-width:130px}.feedback>div{grid-template-columns:1fr 1.15fr}.ranking,.evidence{min-width:700px}}
""";
}
