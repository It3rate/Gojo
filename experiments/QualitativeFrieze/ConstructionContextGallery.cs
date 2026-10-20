using System.Text;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class ConstructionContextExperiment
{
    private static string Html(string data) => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Construction contexts · Gojo</title><style>
:root{--ink:#263d3c;--muted:#65746e;--line:#d9d9cb;--paper:#faf8f1;--accent:#246a70}*{box-sizing:border-box}body{margin:0;background:#eeeede;color:var(--ink);font:16px/1.6 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:40px 30px 80px}header{max-width:1000px;margin-bottom:32px}.eyebrow{letter-spacing:.14em;font-size:12px;font-weight:700;color:#9b623c;text-transform:uppercase}h1{font:clamp(30px,4vw,49px)/1.15 Georgia,serif;margin:14px 0}h2{font:30px/1.2 Georgia,serif;margin:10px 0}h3{font-size:20px;line-height:1.35;margin:10px 0}p{margin:10px 0 16px}a{color:var(--accent);text-underline-offset:3px}.lede{font-size:19px;max-width:950px}nav{display:flex;gap:8px;flex-wrap:wrap;margin:18px 0}nav a,.link-pill{padding:6px 12px;border:1px solid var(--line);border-radius:20px;background:#faf8f1;text-decoration:none;font-size:14px}.toolbar{position:sticky;top:0;z-index:2;display:flex;align-items:center;flex-wrap:wrap;gap:16px;background:#eeeedef2;border-block:1px solid var(--line);padding:12px 0;margin-bottom:32px}.toolbar label{display:flex;align-items:center;gap:8px}.case{scroll-margin-top:85px;margin:34px 0 52px;padding-top:12px}.case>p{max-width:1030px}.variants{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,430px),1fr));gap:20px}.variant{background:var(--paper);border:1px solid var(--line);padding:22px;border-radius:4px;min-width:0}.variant>p{color:var(--muted);font-size:14px}.picture{display:block;width:100%;height:auto;aspect-ratio:2;background:#faf8f1}.step-title{font-size:18px;font-weight:650;min-height:25px}.step-description{min-height:72px;font-size:14px}.timeline{display:flex;gap:7px;flex-wrap:wrap;margin:12px 0}button,select{font:inherit}button{border:1px solid #afbbb0;background:transparent;color:var(--ink);border-radius:3px;cursor:pointer;padding:5px 12px}button[aria-pressed=true]{background:var(--accent);border-color:var(--accent);color:#fff}button:hover{border-color:var(--accent)}.step-range{width:100%;accent-color:var(--accent)}dl{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;border-top:1px solid var(--line);padding-top:15px;margin:16px 0}dt{font-size:11px;color:var(--muted);line-height:1.2}dd{font-size:21px;margin:3px 0 0}.status{font-size:13px;padding:7px 10px;border-left:3px solid var(--accent);background:#eaf0e5}.status.blocked{border-color:#ab623c;background:#f4e8da}.download{font-size:13px}.field-card{background:var(--paper);border:1px solid var(--line);margin:18px 0;padding:24px}.triptych{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px}figure{margin:0;min-width:0}figcaption{font-size:13px;color:var(--muted);margin:8px 0}figure img{display:block;width:100%;height:auto}.field-facts{font-size:13px;color:var(--muted)}.summary{background:#e2e6d9;border-left:3px solid #8c9855;padding:18px 24px;margin-top:30px}.summary p{margin:0}details{border-top:1px solid var(--line);padding-top:12px;margin-top:22px}summary{cursor:pointer;font-weight:600}li{margin:7px 0}footer{border-top:1px solid var(--line);margin-top:36px;padding-top:22px;color:var(--muted);font-size:14px}@media(max-width:700px){main{padding:24px 16px 50px}.triptych{grid-template-columns:1fr}.field-card,.variant{padding:16px}.toolbar{font-size:13px;gap:8px}.step-description{min-height:0}dl{gap:8px}h2{font-size:26px}}
</style></head><body><main><header><span class="eyebrow">Gojo · experimental constructions</span><h1>Same drawing.<br>Different next possibilities.</h1><p class="lede">A construction can supply useful handles, expectations and routes for change. These small experiments keep the drawing, the construction contract and the available history separately inspectable.</p><p>Explore the steps below, then compare three readings of one ornament field. Colours distinguish path components or observation groups. They do not create physical joins.</p><nav><a href="overview.html">All designs together</a><a href="diagnostics.json">Recorded evidence</a><a href="contexts.gojo.json.gz">Native Gojo archive</a><a href="../ornament-perspectives/gallery.html">Earlier overlapping views</a></nav></header>
<div class="toolbar"><label><input type="checkbox" id="construction" checked> Show component colours and path starts</label><span id="summary"></span></div><p>Steps share a fixed display window within each construction case. The final four-unit repeat is fitted to its own larger window.</p>
<div id="cases"></div><section class="case" id="views"><span class="eyebrow">C05 · observation becomes construction</span><h2>One field, three useful organizations</h2><p>The source has 48 actual triangular chambers. Courts, overlapping vertex diamonds and row parents select different groups from that same source. Each view's exposed boundary-contact network is consumed by new acknowledged drawing flow. The same eight-chord ornament program is then placed at the published group landmarks.</p><p>Stripes mark shared chamber participation. Grey is the original field, shown as a guide; teal is accepted construction. Dots mark actual constructed endpoint roles. A bridge gap marks a projected crossing without a joined site. Shared observation alone never creates a passage.</p><div id="view-cards"></div></section>
<section class="summary"><p><strong>What to look for:</strong> useful context can be a current editing surface, a correspondence to a coarser construction, or a remembered route. It can also be unnecessary for the present drawing. Changing the chosen view changes the units available to the next construction.</p></section>
<details><summary>Interpretation and limits</summary><ul id="limits"></ul><p>The square-like drawings use a chosen equal-axis display. Formal shared reaches and cancellation are native evidence; perpendicular screen axes and Euclidean appearance are supplied by this renderer. Grouping proposals are authored in this round.</p></details><footer><p>Native structure is relations and placeholders. Numerical screen coordinates and colours are added only for display. Structural counts are observations of these constructions, rather than a claim about general recognition speed or human salience.</p><p><a href="overview.html">Overview</a> · <a href="diagnostics.json">Diagnostics</a> · <a href="contexts.gojo.json.gz">Captured native structure</a></p></footer>
<script id="data" type="application/json">__DATA__</script><script>
'use strict';
const model=JSON.parse(document.getElementById('data').textContent), mode=document.getElementById('construction');
const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
document.getElementById('summary').textContent=`${model.cases.length} construction cases · ${model.views.length} field readings · ${model.nativeNumericElements} native numeric elements`;
document.getElementById('limits').innerHTML=model.limits.map(s=>`<li>${escape(s)}</li>`).join('');
const renderers=[];
for(const experiment of model.cases){
 const section=document.createElement('section');section.className='case';section.id=experiment.id;
 section.innerHTML=`<span class="eyebrow">${escape(experiment.id)} · construction context</span><h2>${escape(experiment.name)}</h2><p>${escape(experiment.description)}</p><div class="variants"></div>`;
 for(const variant of experiment.variants){
  const card=document.createElement('article');card.className='variant';card.dataset.variant=variant.id;
  card.innerHTML=`<span class="eyebrow">${escape(variant.id)}</span><h3>${escape(variant.name)}</h3><p>${escape(variant.description)}</p><a class="art-link"><img class="picture" alt=""></a><div class="timeline" role="group" aria-label="Construction steps"></div><label class="range-label">Step <span class="step-number"></span><input class="step-range" type="range" min="0" max="${variant.steps.length-1}" value="0" step="1" aria-label="${escape(variant.name)} construction step"></label><div class="step-title"></div><p class="step-description"></p><div class="status"></div><dl></dl><a class="download">Open this drawing</a>`;
  const slider=card.querySelector('.step-range'),timeline=card.querySelector('.timeline');
  variant.steps.forEach((step,i)=>{const b=document.createElement('button');b.type='button';b.textContent=String(i+1);b.title=step.name;b.className='step-button';b.addEventListener('click',()=>{slider.value=String(i);update()});timeline.append(b)});
  function update(){
   const index=Number(slider.value),step=variant.steps[index],src=mode.checked?step.construction:step.plain;
   card.dataset.step=step.id;card.querySelector('img').src=src;card.querySelector('img').alt=`${experiment.id}: ${step.name}`;
   card.querySelector('.art-link').href=src;card.querySelector('.download').href=src;
   card.querySelector('.step-number').textContent=`${index+1} / ${variant.steps.length}`;
   card.querySelector('.step-title').textContent=step.name;card.querySelector('.step-description').textContent=step.description;
   timeline.querySelectorAll('button').forEach((b,i)=>b.setAttribute('aria-pressed',String(i===index)));
   const status=card.querySelector('.status');status.className='status'+(step.blocked||step.open?' blocked':'');
   status.textContent=step.blocked?`${step.blocked} blocked edit evidence · last admitted drawing retained`:step.open?`${step.open} open edit evidence · last admitted drawing retained`:'Admitted drawing · question evidence described above';
   const facts=[['Paths / resets',step.paths],['Ordered chords',step.chords],['Closure claims',step.closedClaims],['Exposed ports',step.ports],['Retained rungs',step.rungs],['Accepted actions',step.acceptedActions],['Blocked edit scope',step.blocked],['Native numbers',step.nativeNumericElements]];
   card.querySelector('dl').innerHTML=facts.map(([label,value])=>`<div><dt>${label}</dt><dd>${value}</dd></div>`).join('');
  }
  slider.addEventListener('input',update);renderers.push(update);update();section.querySelector('.variants').append(card);
 }
 document.getElementById('cases').append(section);
}
mode.addEventListener('change',()=>renderers.forEach(render=>render()));
for(const view of model.views){const card=document.createElement('article');card.className='field-card';card.dataset.view=view.id;
 card.innerHTML=`<span class="eyebrow">${escape(view.id)}</span><h3>${escape(view.name)}</h3><p>${escape(view.description)}</p><div class="triptych"><figure><a href="${view.groups}"><img src="${view.groups}" alt="${escape(view.name)} actual group membership"></a><figcaption>Selected groups · shared chambers striped</figcaption></figure><figure><a href="${view.result}"><img src="${view.result}" alt="${escape(view.name)} accepted contact network"></a><figcaption>Accepted contact network · source in grey</figcaption></figure><figure><a href="${view.ornament}"><img src="${view.ornament}" alt="${escape(view.name)} shared ornament program at selected landmarks"></a><figcaption>Same ornament unit · different placement context</figcaption></figure></div><p class="field-facts">${view.groupCount} groups · ${view.selectedFaces} selected source chambers · ${view.complement} complementary chambers · ${view.contacts} contact passages · ${view.overlaps} overlap relations · ${view.decorations} motif placements</p>`;
 document.getElementById('view-cards').append(card);
}
const requested=new URLSearchParams(location.search).get('case');if(requested)document.getElementById(requested)?.scrollIntoView();
</script></main></body></html>
""".Replace("__DATA__", data);

    private static string Overview(IReadOnlyList<CaseModel> cases, IReadOnlyList<ViewModel> views)
    {
        var html = new StringBuilder("<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Construction context overview · Gojo</title><style>*{box-sizing:border-box}body{margin:0;background:#eeeede;color:#263d3c;font:15px/1.5 system-ui}main{max-width:1700px;margin:auto;padding:30px}h1,h2{font-family:Georgia,serif;font-weight:400}h1{font-size:38px}h2{font-size:26px}a{color:#246a70}section{margin:30px 0}.strip{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,270px),1fr));gap:12px}figure{margin:0;background:#faf8f1;border:1px solid #d9d9cb;padding:12px}img{width:100%;height:auto;display:block}figcaption{font-size:13px}small{color:#65746e}header p{max-width:950px}</style></head><body><main><header><h1>Construction contexts · all steps</h1><p>The same visible support can expose different edits. Retained history can answer construction questions; an independent capture still draws the current result. Different overlapping reads can become genuinely different construction networks.</p><p><a href='gallery.html'>Open the interactive experiment</a> · <a href='diagnostics.json'>Recorded evidence</a></p></header>");
        foreach (var item in cases)
        {
            html.Append($"<section><h2>{H(item.Id + " · " + item.Name)}</h2><p>{H(item.Description)}</p>");
            foreach (var variant in item.Variants)
            {
                html.Append($"<h3>{H(variant.Name)}</h3><div class='strip'>");
                foreach (var step in variant.Steps)
                    html.Append($"<figure><a href='gallery.html?case={item.Id}'><img src='{step.Construction}' alt='{H(step.Name)}'/></a><figcaption><strong>{H(step.Id + " · " + step.Name)}</strong><br><small>{step.Paths} paths · {step.Chords} chords · {step.ClosedClaims} closure claims · {step.Rungs} rungs · {step.Blocked} blocked edit evidence</small></figcaption></figure>");
                html.Append("</div>");
            }
            html.Append("</section>");
        }
        html.Append("<section><h2>C05 · one field, three next constructions</h2>");
        foreach (var view in views)
            html.Append($"<h3>{H(view.Id + " · " + view.Name)}</h3><div class='strip'><figure><img src='{view.Groups}' alt='{H(view.Name + " groups")}'/><figcaption>Actual source membership</figcaption></figure><figure><img src='{view.Result}' alt='{H(view.Name + " contact network")}'/><figcaption>{view.GroupCount} nodes · {view.Contacts} passages</figcaption></figure><figure><img src='{view.Ornament}' alt='{H(view.Name + " ornament")}'/><figcaption>{view.Decorations} placements of one shared program</figcaption></figure></div>");
        return html.Append("</section><p><a href='gallery.html'>Interactive experiment</a> · <a href='contexts.gojo.json.gz'>Native Gojo archive</a></p></main></body></html>").ToString();
    }
}
