using System.Text.Json;

namespace Gojo.Experiments.ConstructionMachines;

/// <summary>
/// Offline playback of retained negotiation observations. This view does not
/// settle machines or infer outcomes; every round and status is supplied by the
/// experiment. Labels and colours belong only to this presentation.
/// </summary>
public static class NegotiationReview
{
    public static void Write(string path, string json)
    {
        using var document = JsonDocument.Parse(json);
        // Default JSON escaping prevents data from terminating the script block.
        var safeJson = JsonSerializer.Serialize(document.RootElement);
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (parent is not null) Directory.CreateDirectory(parent);
        File.WriteAllText(path, Template.Replace("__NEGOTIATION_DATA__", safeJson, StringComparison.Ordinal));
    }

    private const string Template = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta http-equiv="Content-Security-Policy" content="default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data:; base-uri 'none'; form-action 'none'">
<title>Gojo · Construction negotiation</title>
<style>
:root{color-scheme:light;--ink:#244241;--muted:#637874;--paper:#f3f5f0;--edge:#d6dfd8;--accent:#246d61;--wall:#397c9c;--roof:#b97838;--door:#36836e;--residue:#bd5263}
*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:15px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif}button,input,select{font:inherit}button,select,summary{cursor:pointer}button:disabled{cursor:default;opacity:.4}button:focus-visible,input:focus-visible,select:focus-visible,summary:focus-visible,a:focus-visible{outline:3px solid #d3ad4c;outline-offset:3px}button{background:#fff;border:1px solid var(--edge);border-radius:8px;color:var(--ink);padding:7px 12px;white-space:nowrap}button:hover:not(:disabled){background:#eaf1eb;border-color:#9bb8aa}button.primary{background:var(--accent);border-color:var(--accent);color:white}a{color:var(--accent)}input{accent-color:var(--accent)}.shell{max-width:1500px;margin:auto;padding:21px 25px 32px}header{display:flex;align-items:baseline;justify-content:space-between;gap:20px;margin-bottom:15px}.eyebrow{margin:0 0 3px;font-size:11px;letter-spacing:.12em;text-transform:uppercase;color:var(--accent);font-weight:650}h1{font-weight:550;font-size:clamp(25px,2.5vw,34px);letter-spacing:-.035em;line-height:1.2;margin:0}h2,h3,p{margin:0}.offline{font-size:12px;color:var(--muted)}.toolbar{background:#fff;border:1px solid var(--edge);border-radius:12px;padding:11px 14px;margin-bottom:15px}.scenario-row{display:flex;align-items:center;gap:9px}.scenario-label{font-size:12px;color:var(--muted);white-space:nowrap}.scenario-row select{min-width:0;flex:1;padding:8px 10px;color:var(--ink);border:1px solid var(--edge);border-radius:7px;background:#fff;font-weight:550}.scenario-count{font-size:12px;color:var(--muted);white-space:nowrap}.round-row{display:flex;align-items:center;gap:9px;border-top:1px solid #edf0eb;margin-top:10px;padding-top:10px}.round-row>input{min-width:60px;flex:1}.round-count{min-width:94px;text-align:center;font-size:13px;font-variant-numeric:tabular-nums}.settings{display:flex;align-items:center;gap:14px;margin-left:7px;font-size:12px;color:var(--muted);white-space:nowrap}.settings label{display:flex;gap:5px;align-items:center}.workspace{display:grid;grid-template-columns:minmax(0,1fr) 330px;align-items:start;gap:16px}.canvas-card{border:1px solid var(--edge);border-radius:14px;overflow:hidden;background:#fff}.scene-heading{padding:14px 17px 10px;display:flex;align-items:flex-start;justify-content:space-between;gap:16px}.scene-heading h2{font-size:18px;font-weight:600;line-height:1.35}.scene-heading p{font-size:13px;line-height:1.5;color:var(--muted);max-width:820px;margin-top:5px}.status{font-size:12px;border:1px solid #d8e1d9;background:#f3f6f0;padding:5px 9px;border-radius:8px;max-width:260px;flex-shrink:0}.figure{margin:0;height:clamp(330px,61vh,670px);padding:6px 18px 13px}.drawing{display:block;width:100%;height:100%}.legend{padding:10px 17px;border-top:1px solid #edf0eb;display:flex;gap:15px;align-items:center;flex-wrap:wrap;font-size:12px;color:var(--muted)}.legend span{display:inline-flex;align-items:center;gap:5px}.swatch{display:inline-block;width:18px;border-top:2px solid}.dashed{border-top-style:dashed}.round-note{padding:0 17px 13px;font-size:12px;color:var(--muted)}.rail{display:flex;flex-direction:column;gap:13px}.panel{background:#fff;border:1px solid var(--edge);border-radius:12px;padding:14px}.panel h3,.panel summary{font-size:14px;font-weight:600}.panel summary{list-style-position:outside;margin-left:13px}.panel .help{font-size:12px;color:var(--muted);margin:5px 0 10px}.metrics{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:9px}.metric{padding:10px 11px;background:#f7f8f4;border-radius:8px;min-width:0}.metric .label{display:block;font-size:11px;color:var(--muted)}.metric .value{display:block;font-variant-numeric:tabular-nums;font-size:22px;line-height:1.35;margin-top:2px;overflow-wrap:anywhere}.metric .unit{font-size:10px;color:var(--muted)}.history{margin-top:12px}.history label{font-size:11px;color:var(--muted);display:flex;align-items:center;justify-content:space-between}.spark{display:block;width:100%;height:60px;margin-top:3px;background:#fafbf8;border-radius:5px}.notes{padding-left:19px;margin:9px 0 0;font-size:13px;color:var(--muted);line-height:1.55}.notes li+li{margin-top:6px}.empty-note{font-size:12px;color:var(--muted);margin-top:9px}.events{margin:10px 0 0;padding-left:18px;max-height:340px;overflow:auto;font-size:12px;line-height:1.65;overflow-wrap:anywhere}.events li+li{margin-top:5px}.links{display:grid;gap:8px;margin-top:10px}.link-item{border-left:3px solid #b85969;padding:2px 0 2px 10px;font-size:12px;overflow-wrap:anywhere}.link-item strong{font-weight:550;display:block}.link-item span{color:var(--muted)}.ports{width:100%;border-collapse:collapse;font-size:12px;margin-top:9px}.ports th{text-align:left;font-weight:500;color:var(--muted);padding-bottom:5px}.ports td{border-top:1px solid #edf0eb;padding:6px 0;vertical-align:top;overflow-wrap:anywhere}.ports td:last-child{text-align:right;font-variant-numeric:tabular-nums;padding-left:9px}.explanation{margin-top:16px;background:transparent;border-top:1px solid var(--edge);padding:14px 0 0;color:var(--muted);font-size:13px}.explanation summary{width:fit-content;color:var(--ink);font-weight:550}.explanation p{max-width:1100px;margin-top:9px}.explanation ul{margin-top:8px;max-width:1100px;padding-left:19px}.downloads{display:flex;flex-wrap:wrap;gap:16px;font-size:12px;margin-top:15px}.keyboard{margin-left:auto;color:var(--muted)}.line{fill:none;stroke-linecap:round;stroke-linejoin:round;vector-effect:non-scaling-stroke;stroke-width:2.25}.line.guide{stroke-width:1.2;stroke-dasharray:4 5;opacity:.7}.line.ghost{stroke:#c3ccc5!important;stroke-width:1.5;opacity:.6}.line.dashed{stroke-dasharray:5 4}.line.residue,.line.request{stroke-width:1.7}.link-line{fill:none;stroke:#b85a6b;stroke-width:1.35;stroke-dasharray:3 4;vector-effect:non-scaling-stroke;opacity:.85}.port-dot{stroke:#386d65;stroke-width:1.5;fill:#fff;vector-effect:non-scaling-stroke}.port-label{fill:#476c63;font-family:system-ui,sans-serif;font-weight:500;paint-order:stroke;stroke:#fff;stroke-width:3px;stroke-linejoin:round}.emptyplot{font-size:13px;fill:var(--muted)}
@media(max-width:1100px){.workspace{grid-template-columns:minmax(0,1fr) 290px}.settings{gap:8px;margin-left:0}.scenario-label{display:none}.round-row{flex-wrap:wrap}.round-row>input{flex-basis:140px}.settings{margin-left:auto}.figure{height:57vh;min-height:330px}.scene-heading{display:block}.status{display:inline-block;margin-top:8px;max-width:100%}}
@media(max-width:760px){.shell{padding:16px 12px 25px}header{margin-bottom:12px}.offline{display:none}.toolbar{padding:10px}.scenario-count{display:none}.scenario-row{gap:5px}.scenario-row button{padding:7px 9px}.scenario-row select{font-size:13px}.round-row{gap:6px}.round-row button{padding:6px 9px;font-size:13px}.round-count{min-width:80px;font-size:12px}.settings{width:100%;margin:1px 0 0;justify-content:flex-start}.workspace{grid-template-columns:1fr}.figure{height:49vh;min-height:300px}.rail{display:grid;grid-template-columns:1fr 1fr}.metric .value{font-size:20px}.panel{padding:12px}.rail>.panel:first-child{grid-column:1/-1}.metrics{grid-template-columns:repeat(4,minmax(0,1fr))}.history{display:none}.events-panel,.ports-panel{grid-column:1/-1}.scene-heading{padding:12px}.scene-heading h2{font-size:17px}.legend{padding:10px 12px;gap:10px}.round-note{padding:0 12px 12px}.keyboard{display:none}}
@media(max-width:420px){.rail{grid-template-columns:1fr}.rail>.panel{grid-column:1}.metrics{grid-template-columns:repeat(2,minmax(0,1fr))}.settings{font-size:11px}.round-row>input{flex-basis:80px}.figure{padding:3px 8px 8px}}
@media print{body{background:#fff}.shell{padding:0;max-width:none}.toolbar,.downloads,.explanation{display:none}.workspace{grid-template-columns:1fr 260px}.figure{height:440px}.panel,.canvas-card{break-inside:avoid}.events{max-height:none}}
</style>
</head>
<body>
<main class="shell">
  <header><div><p class="eyebrow">Gojo · Connected construction study</p><h1>Different machines, shared requests</h1></div><span class="offline">Saved rounds · offline review</span></header>
  <section class="toolbar" aria-label="Experiment controls">
    <div class="scenario-row"><label class="scenario-label" for="scenario">Experiment</label><button id="previousScenario" type="button" aria-label="Previous experiment">‹</button><select id="scenario" aria-label="Select experiment"></select><button id="nextScenario" type="button" aria-label="Next experiment">›</button><span class="scenario-count" id="scenarioCount"></span></div>
    <div class="round-row"><button id="previousRound" type="button" aria-label="Previous round">←</button><button id="play" class="primary" type="button" aria-label="Play saved rounds">Play</button><button id="nextRound" type="button" aria-label="Next round">→</button><span class="round-count" id="roundCount"></span><input id="round" type="range" min="0" max="0" value="0" aria-label="Saved round"><div class="settings"><label><input id="ghosts" type="checkbox" checked> Starting shape</label><label><input id="connections" type="checkbox" checked> Connections</label><label><input id="portToggle" type="checkbox"> Ports</label></div></div>
  </section>
  <div class="workspace">
    <section class="canvas-card" aria-labelledby="sceneTitle">
      <div class="scene-heading"><div><h2 id="sceneTitle"></h2><p id="description"></p></div><span id="status" class="status" role="status"></span></div>
      <figure id="figure" class="figure"></figure>
      <div class="legend"><span><i class="swatch" style="border-color:var(--wall)"></i>Boundary</span><span><i class="swatch" style="border-color:var(--roof)"></i>Roof</span><span><i class="swatch" style="border-color:var(--door)"></i>Door</span><span><i class="swatch dashed" style="border-color:var(--residue)"></i>Connection / mismatch</span><span><i class="swatch" style="border-color:#c3ccc5"></i>Starting shape</span></div>
      <p class="round-note" id="roundNote">Each round is a retained observation. The view stays fixed while the geometry changes.</p>
    </section>
    <aside class="rail" aria-label="Round observations">
      <section class="panel"><h3>This round</h3><p class="help">Mismatch and movement use drawing coordinates. Blocked requests and remainders use the selected actuator bases; these totals are diagnostics, not energy.</p><div class="metrics" id="metrics"></div><div class="history"><label>Mismatch across saved rounds<span id="historyCount"></span></label><div id="mismatchHistory"></div></div><div class="history"><label>Largest site movement across saved rounds</label><div id="movementHistory"></div></div></section>
      <section class="panel"><h3>What to look for</h3><ul id="notes" class="notes"></ul></section>
      <details class="panel" open><summary>Connection observations</summary><div id="links" class="links"></div></details>
      <details class="panel events-panel"><summary>Requests and receipts</summary><ol id="events" class="events"></ol><p id="noEvents" class="empty-note" hidden>No requests have been applied in this observation.</p></details>
      <details class="panel ports-panel"><summary>Published port values</summary><table class="ports"><thead><tr><th>Exposed site</th><th>Value</th></tr></thead><tbody id="ports"></tbody></table></details>
    </aside>
  </div>
  <details class="explanation"><summary>What is being compared?</summary><p>These are interpreted publications of connected construction machines. A drawing-derived observation may be a strong task reference, but that does not make it an unprocessed truth. The same interface can be supported by different private constructions, each with different ways to absorb a request.</p><ul id="summary"></ul><p>The page plays saved results; it does not run settlement. A quiet picture can still contain blocked requests, unresolved disagreement or changes below the chosen resolution. The displayed outcome comes from the recorded experiment, not from a visual test for convergence.</p></details>
  <footer class="downloads"><a href="negotiation.gojo.bin" download>Saved native structure</a><a href="negotiation-observations.json" download>Presentation observations</a><span class="keyboard">← / → round · Shift + ← / → experiment · Space play/pause</span></footer>
</main>
<script id="observations" type="application/json">__NEGOTIATION_DATA__</script>
<script>
'use strict';
const data=JSON.parse(document.getElementById('observations').textContent);
const scenarios=Array.isArray(data.scenarios)?data.scenarios:[];
const NS='http://www.w3.org/2000/svg';
const palette={wall:'#397c9c',roof:'#b97838',door:'#36836e',window:'#8868a8',guide:'#a3ada5',request:'#bd5263',residue:'#bd5263',path:'#377368'};
const $=id=>document.getElementById(id);
const list=value=>Array.isArray(value)?value:[];
const text=value=>value==null?'':String(value);
let scenarioIndex=0,roundIndex=0,timer=null;
function element(tag,cls,value){const e=document.createElement(tag);if(cls)e.className=cls;if(value!==undefined)e.textContent=text(value);return e;}
function svgElement(tag,attributes={}){const e=document.createElementNS(NS,tag);for(const [key,value]of Object.entries(attributes))e.setAttribute(key,text(value));return e;}
function validPoint(p){return Array.isArray(p)&&p.length>=2&&Number.isFinite(p[0])&&Number.isFinite(p[1]);}
function linePoints(line){return list(line?.points).filter(validPoint);}
function title(node,value){const t=svgElement('title');t.textContent=text(value);node.append(t);}
function currentScenario(){return scenarios[scenarioIndex];}
function rounds(){return list(currentScenario()?.rounds);}
function currentRound(){return rounds()[roundIndex];}
function number(value){return Number.isFinite(value)?new Intl.NumberFormat(undefined,{maximumFractionDigits:4}).format(value):'—';}
function viewBounds(scenario){
  const points=[];
  for(const round of list(scenario.rounds)){
    for(const line of [...list(round.lines),...list(round.ghostLines)])points.push(...linePoints(line));
    for(const port of list(round.ports))if(validPoint(port.point))points.push(port.point);
    for(const link of list(round.links)){if(validPoint(link.from))points.push(link.from);if(validPoint(link.to))points.push(link.to);}
  }
  if(!points.length)return {x:0,y:0,width:300,height:200,extent:200};
  let minX=Infinity,maxX=-Infinity,minY=Infinity,maxY=-Infinity;
  for(const [x,y]of points){minX=Math.min(minX,x);maxX=Math.max(maxX,x);minY=Math.min(minY,y);maxY=Math.max(maxY,y);}
  const extent=Math.max(maxX-minX,maxY-minY,1),pad=extent*.13;
  return {x:minX-pad,y:-maxY-pad,width:Math.max(maxX-minX,extent*.2)+2*pad,height:Math.max(maxY-minY,extent*.2)+2*pad,extent};
}
const bounds=scenarios.map(viewBounds);
function drawing(round){
  const box=bounds[scenarioIndex],svg=svgElement('svg',{class:'drawing',role:'img','aria-label':`${currentScenario().title}, round ${round.index??roundIndex}`,viewBox:`${box.x} ${box.y} ${box.width} ${box.height}`});
  title(svg,`${currentScenario().title} · ${round.status??''}`);
  const allLines=list(round.lines);
  if(!allLines.length){const note=svgElement('text',{x:box.x+box.width/2,y:box.y+box.height/2,'text-anchor':'middle',class:'emptyplot'});note.textContent='Open structure · no metric drawing';svg.append(note);}
  function stroke(line,ghost=false){
    const points=linePoints(line);if(points.length<2)return;
    const role=Object.hasOwn(palette,line.role)?line.role:'guide';
    const path=svgElement('polyline',{points:points.map(([x,y])=>`${x},${-y}`).join(' '),class:`line ${role}${ghost?' ghost':''}${line.dashed?' dashed':''}`,stroke:palette[role]});
    title(path,ghost?'Starting shape':role);svg.append(path);
  }
  if($('ghosts').checked){const ghosts=list(round.ghostLines);for(const line of ghosts.length?ghosts:list(rounds()[0]?.lines))stroke(line,true);}
  for(const line of allLines)stroke(line);
  if($('connections').checked)for(const link of list(round.links)){
    if(!validPoint(link.from)||!validPoint(link.to))continue;
    const [ax,ay]=link.from,[bx,by]=link.to;
    const path=svgElement('line',{x1:ax,y1:-ay,x2:bx,y2:-by,class:'link-line'});
    title(path,`${link.label}: mismatch ${link.residual}`);svg.append(path);
    for(const [x,y]of [link.from,link.to]){const cap=svgElement('circle',{cx:x,cy:-y,r:box.extent*.009,fill:'none',stroke:'#bd5263','stroke-width':1,'vector-effect':'non-scaling-stroke'});title(cap,`${link.label}: mismatch ${link.residual}`);svg.append(cap);}
  }
  if($('portToggle').checked)list(round.ports).forEach((port,i)=>{
    if(!validPoint(port.point))return;
    const [x,y]=port.point,r=box.extent*.009;
    const dot=svgElement('circle',{cx:x,cy:-y,r,class:'port-dot'});title(dot,`${port.label}: ${port.value}`);svg.append(dot);
    const label=svgElement('text',{x:x+r*1.8,y:-y+(i%2?3:-1.8)*r,'font-size':box.extent*.034,class:'port-label'});label.textContent=`P${i+1}`;title(label,`${port.label}: ${port.value}`);svg.append(label);
  });
  return svg;
}
function sparkline(key,colour){
  const values=rounds().map(r=>Number.isFinite(r.metrics?.[key])?r.metrics[key]:null),w=290,h=60,pad=7;
  const svg=svgElement('svg',{class:'spark',viewBox:`0 0 ${w} ${h}`,role:'img','aria-label':`${key} across saved rounds`});
  const finite=values.filter(v=>v!==null);if(!finite.length)return svg;
  const low=Math.min(0,...finite),high=Math.max(...finite,low+1);
  const x=i=>pad+(w-2*pad)*i/Math.max(1,values.length-1),y=v=>h-pad-(h-2*pad)*(v-low)/(high-low);
  svg.append(svgElement('line',{x1:pad,y1:y(0),x2:w-pad,y2:y(0),stroke:'#dfe6df','stroke-width':1}));
  let points=[];const flush=()=>{if(points.length)svg.append(svgElement('polyline',{points:points.join(' '),fill:'none',stroke:colour,'stroke-width':1.8}));points=[];};
  values.forEach((v,i)=>{if(v===null){flush();return;}points.push(`${x(i)},${y(v)}`);});flush();
  svg.append(svgElement('line',{x1:x(roundIndex),y1:4,x2:x(roundIndex),y2:h-4,stroke:'#9eafa6','stroke-dasharray':'2 3'}));
  if(values[roundIndex]!==null)svg.append(svgElement('circle',{cx:x(roundIndex),cy:y(values[roundIndex]),r:3.3,fill:colour}));
  return svg;
}
function render(){
  const scenario=currentScenario(),round=currentRound();
  if(!scenario||!round){$('sceneTitle').textContent='No saved rounds';$('description').textContent='This report does not contain any calculated observations.';for(const id of ['previousScenario','nextScenario','previousRound','nextRound','play','round'])$(id).disabled=true;return;}
  $('scenario').value=String(scenarioIndex);$('scenarioCount').textContent=`${scenarioIndex+1} of ${scenarios.length}`;
  $('previousScenario').disabled=scenarioIndex===0;$('nextScenario').disabled=scenarioIndex===scenarios.length-1;
  $('round').max=String(rounds().length-1);$('round').value=String(roundIndex);
  $('roundCount').textContent=`Round ${round.index??roundIndex} / ${rounds().at(-1)?.index??rounds().length-1}`;
  $('previousRound').disabled=roundIndex===0;$('nextRound').disabled=roundIndex===rounds().length-1;$('play').disabled=rounds().length<2;
  $('sceneTitle').textContent=scenario.title;$('description').textContent=scenario.description;$('status').textContent=round.status??'';
  $('figure').replaceChildren(drawing(round));
  $('notes').replaceChildren(...list(scenario.notes).map(note=>element('li','',note)));
  $('metrics').replaceChildren(...[['mismatch','Mismatch'],['movement','Largest site movement'],['blocked','Parameter deferral'],['remainder','Resolution remainder']].map(([key,label])=>{
    const card=element('div','metric');card.append(element('span','label',label),element('span','value',number(round.metrics?.[key])),element('span','unit',key==='mismatch'||key==='movement'?'coordinate ticks':'actuator ticks'));return card;
  }));
  $('historyCount').textContent=`${rounds().length} observations`;
  $('mismatchHistory').replaceChildren(sparkline('mismatch','#bd5263'));$('movementHistory').replaceChildren(sparkline('movement','#397c9c'));
  $('links').replaceChildren(...list(round.links).map(link=>{const e=element('div','link-item');e.append(element('strong','',link.label),element('span','',`Mismatch: ${text(link.residual)}`));return e;}));
  if(!list(round.links).length)$('links').append(element('p','empty-note','No connection observations in this round.'));
  $('events').replaceChildren(...list(round.events).map(event=>element('li','',event)));$('noEvents').hidden=list(round.events).length>0;
  $('ports').replaceChildren(...list(round.ports).map((port,i)=>{const row=element('tr');row.append(element('td','',`P${i+1} · ${text(port.label)}`),element('td','',port.value));return row;}));
  $('roundNote').textContent=roundIndex===0?'Initial interpreted publication, before the first exchange. The view stays fixed throughout this experiment.':'Captured publications → local responses → updated constructions. Hover over a port or connection to inspect its retained value.';
}
function updateHash(){const s=currentScenario();if(s)history.replaceState(null,'',`#${encodeURIComponent(s.id)}/${roundIndex}`);}
function stop(){if(timer!==null)clearInterval(timer);timer=null;$('play').textContent='Play';$('play').setAttribute('aria-label','Play saved rounds');}
function goRound(index){roundIndex=Math.max(0,Math.min(rounds().length-1,index));render();updateHash();}
function goScenario(index){stop();scenarioIndex=Math.max(0,Math.min(scenarios.length-1,index));roundIndex=0;render();updateHash();}
function play(){if(timer!==null){stop();return;}if(roundIndex>=rounds().length-1)goRound(0);$('play').textContent='Pause';$('play').setAttribute('aria-label','Pause saved rounds');timer=setInterval(()=>{goRound(roundIndex+1);if(roundIndex>=rounds().length-1)stop();},750);}
function readHash(){
  const parts=location.hash.slice(1).split('/');let id='';try{id=decodeURIComponent(parts[0]??'');}catch{}
  const found=scenarios.findIndex(s=>s.id===id);if(found<0)return;
  scenarioIndex=found;const n=Number(parts[1]??0);roundIndex=Number.isFinite(n)?Math.max(0,Math.min(rounds().length-1,Math.trunc(n))):0;
}
scenarios.forEach((s,i)=>{const option=element('option','',`${i+1}. ${s.title}`);option.value=String(i);$('scenario').append(option);});
$('summary').replaceChildren(...list(data.summary).map(s=>element('li','',s)));
$('scenario').addEventListener('change',()=>goScenario(Number($('scenario').value)));
$('previousScenario').addEventListener('click',()=>goScenario(scenarioIndex-1));$('nextScenario').addEventListener('click',()=>goScenario(scenarioIndex+1));
$('previousRound').addEventListener('click',()=>{stop();goRound(roundIndex-1);});$('nextRound').addEventListener('click',()=>{stop();goRound(roundIndex+1);});
$('round').addEventListener('input',()=>{stop();goRound(Number($('round').value));});$('play').addEventListener('click',play);
for(const id of ['ghosts','connections','portToggle'])$(id).addEventListener('change',render);
window.addEventListener('hashchange',()=>{stop();readHash();render();});
document.addEventListener('keydown',event=>{
  if(event.altKey||event.ctrlKey||event.metaKey||event.target.closest('input,select,textarea,[contenteditable="true"]'))return;
  if(event.key==='ArrowLeft'||event.key==='ArrowRight'){event.preventDefault();stop();const direction=event.key==='ArrowRight'?1:-1;if(event.shiftKey)goScenario(scenarioIndex+direction);else goRound(roundIndex+direction);}
  else if(event.code==='Space'&&!event.target.closest('button,summary,a')){event.preventDefault();play();}
});
document.addEventListener('visibilitychange',()=>{if(document.hidden)stop();});
readHash();render();
</script>
</body>
</html>
""";
}
