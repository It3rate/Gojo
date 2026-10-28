using System.Text.Json;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// Offline presentation of calculated observations. This page never constructs,
/// settles, or edits native machines; its only geometric work is fitting the view.
/// </summary>
public static class ReviewPage
{
    public static void Write(string path, string json)
    {
        using var document = JsonDocument.Parse(json);
        // The default encoder escapes '<', including any embedded script terminator.
        var safeJson = JsonSerializer.Serialize(document.RootElement);
        var html = Template.Replace("__OBSERVATION_DATA__", safeJson, StringComparison.Ordinal);
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (parent is not null) Directory.CreateDirectory(parent);
        File.WriteAllText(path, html);
    }

    private const string Template = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data:; base-uri 'none'; form-action 'none'">
<title>Gojo · Construction machines</title>
<style>
:root{color-scheme:light;--ink:#203e40;--muted:#617577;--paper:#f3f5f0;--edge:#d5dfd8;--accent:#206d63;--wall:#3577a0;--roof:#b97234;--door:#348574;--window:#8a67aa;--path:#346c68;--guide:#a5aeaa;--residue:#bd5161}
*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif}button,input{font:inherit}button{cursor:pointer}button:focus-visible,input:focus-visible,summary:focus-visible{outline:3px solid #d4ad50;outline-offset:3px}button:disabled{cursor:default;opacity:.4}.shell{max-width:1440px;margin:auto;padding:30px 28px 45px}.eyebrow{font-size:12px;letter-spacing:.15em;font-weight:650;text-transform:uppercase;color:var(--accent);margin:0 0 5px}h1{font-size:clamp(27px,3vw,39px);font-weight:550;letter-spacing:-.035em;line-height:1.15;margin:0 0 12px}h2,h3,p{margin:0}.intro{max-width:850px;color:var(--muted);font-size:16px}.topline{display:flex;justify-content:space-between;align-items:flex-start;gap:24px}.status{border:1px solid var(--edge);border-radius:30px;background:#fff;padding:7px 12px;font-size:12px;white-space:nowrap;margin-top:6px}.toolbar{display:flex;flex-wrap:wrap;align-items:center;justify-content:space-between;gap:10px;margin:20px 0 18px;padding:11px 0;border-top:1px solid var(--edge);border-bottom:1px solid var(--edge)}.settings{display:flex;gap:18px;align-items:center;flex-wrap:wrap}.settings label{display:flex;gap:7px;align-items:center;font-size:13px;color:var(--muted)}input{accent-color:var(--accent)}.count{font-size:13px;color:var(--muted)}.legend{display:flex;gap:16px;align-items:center;flex-wrap:wrap;font-size:12px;color:var(--muted)}.legend span{display:inline-flex;gap:5px;align-items:center}.swatch{width:19px;height:0;border-top:2px solid var(--ink)}.guide-swatch{border-top-style:dashed}.overview{margin:0 0 20px;color:var(--muted);font-size:14px}.overview summary{cursor:pointer;color:var(--ink);width:fit-content}.overview ul{margin:10px 0 0;padding-left:21px;max-width:1000px}.overview li+li{margin-top:5px}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:17px}.card{display:flex;flex-direction:column;background:#fff;border:1px solid var(--edge);border-radius:15px;overflow:hidden;text-align:left;padding:0;color:inherit;box-shadow:0 2px 6px #203e4003;transition:box-shadow .15s,border-color .15s}.card:hover{border-color:#91b4aa;box-shadow:0 5px 17px #203e400a}.cardhead{padding:17px 18px 3px;display:flex;align-items:baseline;gap:10px}.number{font-size:12px;color:var(--muted);font-variant-numeric:tabular-nums}.card h2{font-size:17px;line-height:1.3;font-weight:600}.card figure{margin:0;height:258px;padding:14px 14px 8px}.drawing{width:100%;height:100%;display:block;background:#fff}.cardbody{padding:11px 18px 17px;border-top:1px solid #eff2ed;flex:1}.cardbody p{font-size:13px;color:var(--muted);line-height:1.55}.inspect{display:block;margin-top:11px;color:var(--accent);font-size:12px;font-weight:550}.empty{border:1px dashed var(--edge);padding:30px;color:var(--muted);border-radius:12px}.footer{font-size:12px;color:var(--muted);margin-top:22px;max-width:1000px}.line{fill:none;stroke-linecap:round;stroke-linejoin:round;vector-effect:non-scaling-stroke;stroke-width:2.1}.line.guide{stroke-width:1.2;stroke-dasharray:4 5;opacity:.75}.line.ghost{stroke:#c6ceca!important;stroke-width:1.5;opacity:.6}.line.residue{stroke-width:2.3}.line.dashed{stroke-dasharray:5 4}.point{vector-effect:non-scaling-stroke;stroke-width:1;fill:white}.emptyplot{font-size:13px;fill:var(--muted)}dialog{border:1px solid var(--edge);border-radius:17px;padding:0;color:var(--ink);max-width:1100px;width:calc(100% - 32px);max-height:calc(100% - 32px);background:#fff;box-shadow:0 20px 90px #1e3e3a33}dialog::backdrop{background:#173b3855;backdrop-filter:blur(3px)}.detailhead{padding:20px 24px 14px;border-bottom:1px solid var(--edge)}.detailtitle{display:flex;gap:20px;justify-content:space-between;align-items:flex-start}.detailtitle h2{font-size:24px;line-height:1.25;font-weight:550;letter-spacing:-.02em}.detailtitle p{font-size:13px;color:var(--muted);margin-top:6px}.close,.navbutton{border:1px solid var(--edge);background:#f8faf7;border-radius:8px;color:var(--ink);padding:7px 13px}.close{white-space:nowrap}.detailnav{display:flex;gap:9px;align-items:center;margin-top:15px}.detailnav .count{margin-left:5px}.keyhint{margin-left:auto!important;font-size:12px;color:var(--muted)}.detailfigure{height:min(56vh,560px);min-height:240px;margin:0;padding:23px 30px}.detailbody{border-top:1px solid var(--edge);padding:17px 24px 22px;background:#fafbf8}.detailbody p{font-size:15px;color:var(--muted)}.detailbody ul{margin:12px 0 0;padding-left:20px;font-size:14px;line-height:1.55}.detailbody li+li{margin-top:5px}@media(max-width:1050px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}.card figure{height:240px}.status{display:none}}@media(max-width:650px){.shell{padding:23px 15px 35px}.grid{grid-template-columns:1fr}.card figure{height:270px}.legend{gap:10px}.topline{display:block}.detailhead{padding:16px}.detailtitle h2{font-size:20px}.detailfigure{padding:18px;height:46vh}.detailbody{padding:16px}.keyhint{display:none}.toolbar{align-items:flex-start;flex-direction:column}.cardbody p{font-size:14px}}@media print{body{background:white}.shell{max-width:none;padding:0}.toolbar,.inspect,.overview,.status,.footer{display:none}.grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:12px;margin-top:20px}.card{break-inside:avoid}.card figure{height:220px}.cardbody{min-height:80px}dialog{display:none}}
</style>
</head>
<body>
<main class="shell">
  <header class="topline">
    <div><p class="eyebrow">Gojo · Construction study</p><h1>One boundary, different machines</h1><p class="intro">A rectangle, a roof and a door. These observations explore what stays connected when the construction changes, and where each construction leaves a different remainder.</p></div>
    <span class="status">Offline experiment · calculated observations</span>
  </header>
  <div class="toolbar">
    <div class="settings"><label><input id="guides" type="checkbox" checked> Construction guides</label><label><input id="ghosts" type="checkbox" checked> Earlier geometry</label><span id="count" class="count"></span></div>
    <div class="legend" aria-label="Drawing legend">
      <span><i class="swatch" style="border-color:var(--wall)"></i>Boundary</span><span><i class="swatch" style="border-color:var(--roof)"></i>Roof</span><span><i class="swatch" style="border-color:var(--door)"></i>Door</span><span><i class="swatch guide-swatch" style="border-color:var(--guide)"></i>Guide</span><span><i class="swatch" style="border-color:var(--residue)"></i>Remainder</span>
    </div>
  </div>
  <details id="overview" class="overview"><summary>What this experiment preserves</summary><ul id="summary"></ul></details>
  <div id="grid" class="grid"></div>
  <p class="footer">The pictures are presentation reads of the saved construction results. The page does not infer shapes or run settlement. Open a case to read its retained assumptions and limits; use the left and right arrow keys to compare cases.</p>
</main>
<dialog id="detail" aria-labelledby="detailTitle">
  <div class="detailhead">
    <div class="detailtitle"><div><h2 id="detailTitle"></h2><p id="detailId"></p></div><button class="close" id="close" type="button">Close</button></div>
    <div class="detailnav"><button class="navbutton" id="prev" type="button" aria-label="Previous case">← Previous</button><button class="navbutton" id="next" type="button" aria-label="Next case">Next →</button><span class="count" id="detailCount"></span><span class="keyhint">← / → case · Esc close</span></div>
  </div>
  <figure id="detailFigure" class="detailfigure"></figure>
  <div class="detailbody"><p id="detailDescription"></p><ul id="detailNotes"></ul></div>
</dialog>
<script id="observations" type="application/json">__OBSERVATION_DATA__</script>
<script>
'use strict';
const data=JSON.parse(document.getElementById('observations').textContent);
const cases=Array.isArray(data.cases)?data.cases:[];
const svgNS='http://www.w3.org/2000/svg';
const palette={wall:'#3577a0',roof:'#b97234',door:'#348574',window:'#8a67aa',path:'#346c68',guide:'#a5aeaa',residue:'#bd5161'};
const detail=document.getElementById('detail');
let selected=0;
const asText=value=>value==null?'':String(value);
function element(tag,cls,text){const e=document.createElement(tag);if(cls)e.className=cls;if(text!==undefined)e.textContent=asText(text);return e;}
function svgElement(tag,attributes){const e=document.createElementNS(svgNS,tag);for(const [name,value]of Object.entries(attributes))e.setAttribute(name,asText(value));return e;}
function points(line){return Array.isArray(line?.points)?line.points.filter(p=>Array.isArray(p)&&p.length>=2&&Number.isFinite(p[0])&&Number.isFinite(p[1])):[];}
function linesOf(item){return [...(Array.isArray(item.ghostLines)?item.ghostLines:[]),...(Array.isArray(item.lines)?item.lines:[])];}
function drawing(item){
  const svg=svgElement('svg',{class:'drawing',role:'img','aria-label':asText(item.title)});
  const title=svgElement('title',{});title.textContent=asText(item.title);svg.append(title);
  const all=linesOf(item).flatMap(points);
  if(!all.length){svg.setAttribute('viewBox','0 0 300 200');const note=svgElement('text',{x:150,y:100,'text-anchor':'middle',class:'emptyplot'});note.textContent='Open structure · no metric drawing';svg.append(note);return svg;}
  let minX=Math.min(...all.map(p=>p[0])),maxX=Math.max(...all.map(p=>p[0]));
  let minY=Math.min(...all.map(p=>p[1])),maxY=Math.max(...all.map(p=>p[1]));
  const extent=Math.max(maxX-minX,maxY-minY,1),pad=extent*.1;
  const width=Math.max(maxX-minX,extent*.12),height=Math.max(maxY-minY,extent*.12);
  const centreX=(minX+maxX)/2,centreY=(minY+maxY)/2;
  svg.setAttribute('viewBox',`${centreX-width/2-pad} ${-centreY-height/2-pad} ${width+pad*2} ${height+pad*2}`);
  const ink=svgElement('g',{transform:'scale(1,-1)'});svg.append(ink);
  function append(line,ghost){
    const ps=points(line);if(!ps.length)return;
    const role=Object.hasOwn(palette,line.role)?line.role:'path';
    if(role==='guide'&&!document.getElementById('guides').checked)return;
    const cls=['line',role,ghost?'ghost':'',line.dashed?'dashed':''].filter(Boolean).join(' ');
    const p=svgElement('polyline',{points:ps.map(v=>`${v[0]},${v[1]}`).join(' '),class:cls,stroke:palette[role]});
    const label=svgElement('title',{});label.textContent=ghost?`Earlier ${role}`:role;p.append(label);ink.append(p);
    if(ps.length===1){ink.append(svgElement('circle',{cx:ps[0][0],cy:ps[0][1],r:extent*.008,stroke:palette[role],class:'point'}));}
  }
  if(document.getElementById('ghosts').checked)for(const line of item.ghostLines??[])append(line,true);
  for(const line of item.lines??[])append(line,false);
  return svg;
}
function renderGallery(){
  const grid=document.getElementById('grid');grid.replaceChildren();
  if(!cases.length){grid.append(element('p','empty','No construction observations were supplied.'));return;}
  cases.forEach((item,index)=>{
    const card=element('button','card');card.type='button';card.dataset.caseId=asText(item.id);
    card.setAttribute('aria-label',`Inspect ${asText(item.title)}`);
    const heading=element('div','cardhead');heading.append(element('span','number',String(index+1).padStart(2,'0')),element('h2','',item.title));
    const figure=element('figure');figure.append(drawing(item));
    const body=element('div','cardbody');body.append(element('p','',item.description),element('span','inspect','Inspect construction →'));
    card.append(heading,figure,body);card.addEventListener('click',()=>openCase(index));grid.append(card);
  });
}
function renderDetail(){
  const item=cases[selected];if(!item)return;
  document.getElementById('detailTitle').textContent=asText(item.title);
  document.getElementById('detailId').textContent=asText(item.id);
  document.getElementById('detailDescription').textContent=asText(item.description);
  document.getElementById('detailCount').textContent=`${selected+1} of ${cases.length}`;
  document.getElementById('detailFigure').replaceChildren(drawing(item));
  const notes=document.getElementById('detailNotes');notes.replaceChildren();
  for(const note of item.notes??[])notes.append(element('li','',note));
  notes.hidden=!notes.children.length;
  document.getElementById('prev').disabled=selected===0;
  document.getElementById('next').disabled=selected===cases.length-1;
}
function openCase(index){if(index<0||index>=cases.length)return;selected=index;renderDetail();if(!detail.open)detail.showModal();history.replaceState(null,'','#'+encodeURIComponent(asText(cases[index].id)));}
function closeCase(){detail.close();history.replaceState(null,'',location.pathname+location.search);}
document.getElementById('close').addEventListener('click',closeCase);
document.getElementById('prev').addEventListener('click',()=>openCase(selected-1));
document.getElementById('next').addEventListener('click',()=>openCase(selected+1));
detail.addEventListener('cancel',event=>{event.preventDefault();closeCase();});
document.addEventListener('keydown',event=>{if(!detail.open||event.altKey||event.ctrlKey||event.metaKey||event.target.matches('input,textarea,select'))return;if(event.key==='ArrowLeft'||event.key==='ArrowRight'){event.preventDefault();openCase(selected+(event.key==='ArrowRight'?1:-1));}});
for(const id of ['guides','ghosts'])document.getElementById(id).addEventListener('change',()=>{renderGallery();if(detail.open)renderDetail();});
const summary=document.getElementById('summary');for(const note of data.summary??[])summary.append(element('li','',note));document.getElementById('overview').hidden=!summary.children.length;
document.getElementById('count').textContent=`${cases.length} construction observations`;
renderGallery();
let requested='';try{requested=decodeURIComponent(location.hash.slice(1));}catch{}
const initial=cases.findIndex(item=>asText(item.id)===requested);if(requested&&initial>=0)openCase(initial);
</script>
</body>
</html>
""";
}
