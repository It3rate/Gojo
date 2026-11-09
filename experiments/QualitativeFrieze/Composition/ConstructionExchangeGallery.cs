using System.Text;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Presentation only: every verdict comes from the recorded native
/// read. SVG coordinates illustrate a selected realization, never establish a
/// seam, an admissible construction or a permission to mutate a source.</summary>
public static class ConstructionExchangeGallery
{
    public static void Write(string output, ConstructionExchangeStudy.Report report, string json)
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "index.html"), Page.Replace("@@DATA@@", json.Replace("<", "\\u003c")));
        var md = new StringBuilder();
        md.AppendLine("# When can one construction stand in for another?");
        md.AppendLine();
        md.AppendLine("This experiment starts with several supplied machines that can draw the same outline. It changes their inputs, keeps each machine's connected constraints, and asks which pairs still agree under a selected read. A matching picture is useful evidence, but does not establish the same topology, flow, construction history or future behavior.");
        md.AppendLine();
        md.AppendLine($"Recorded run: **{report.Machines.Length} machines**, **{report.Shapes.Length} constructions**, **{report.Comparisons.Length} pair comparisons**. {report.Comparisons.Count(c => c.BoundaryExchange)} pairs support the selected boundary exchange; {report.Comparisons.Count(c => c.BoundaryAndTopologyExchange)} also agree under the coarse topology read. These are finite observations, not a universal interchangeability proof.");
        md.AppendLine();
        md.AppendLine("Open [the interactive gallery](index.html) to compare drawings, arrows, zero-at-resolution sections and the pair contracts. The complete recorded data is in [results.json](results.json).");
        md.AppendLine();
        md.AppendLine("Native witness archives: " + string.Join(", ", report.Shapes.Select(s => s.Scenario).Distinct().Select(s => $"[{s}.gojo]({s}.gojo)")) + ", [contours.gojo](contours.gojo). These retain the connected experiment evidence alongside the presentation reads.");
        md.AppendLine();
        md.AppendLine("## What the readings mean");
        md.AppendLine();
        md.AppendLine("- **Outline:** the same multiset of shared-reach line atoms, ignoring direction and section subdivision. Multiplicity is preserved: a doubled line is not one line. The reader supports cardinal and equal-step diagonal sections in the declared basis.");
        md.AppendLine("- **Directed outline:** the same atom comparison with traversal direction retained. Matching undirected ink need not match this read.");
        md.AppendLine("- **Coarse topology:** the same component count, closed-degree condition and sorted non-degree-two incidence counts. Degree-two subdivisions are ignored. This is not general graph isomorphism or a proof of equivalent execution.");
        md.AppendLine("- **Admissibility:** both supplied constructions satisfy the tested obligations. A drawing from a failed candidate is illustrative and cannot authorize exchange.");
        md.AppendLine("- **Boundary exchange:** outline agreement and both candidates admissible. Adding the coarse topology contract narrows this set; direction and other contracts remain separate.");
        md.AppendLine();
        md.AppendLine("## Machines and retained constraints");
        md.AppendLine();
        md.AppendLine("| Machine | What remains connected | What can change |");
        md.AppendLine("| --- | --- | --- |");
        foreach (var machine in report.Machines)
            md.AppendLine($"| {Cell(machine.Name)} | {Cell(machine.Preserves)} | {Cell(machine.Changes)} |");
        md.AppendLine();
        md.AppendLine("## Four input probes");
        md.AppendLine();
        md.AppendLine("The counts bind repeated shared reaches, rather than pixels or universal physical lengths. Width and height are half-extents in the supplied centered realization. The notch input is consumed only where the native program has that role connected to a move.");
        md.AppendLine();
        md.AppendLine("| Probe | W / H / notch | What to inspect |");
        md.AppendLine("| --- | --- | --- |");
        foreach (var group in report.Shapes.GroupBy(s => s.Scenario))
        {
            var shape = group.First();
            md.AppendLine($"| {Cell(group.Key)} | {shape.Width} / {shape.Height} / {shape.Notch} | {Cell(ScenarioNote(group.Key))} |");
        }
        md.AppendLine();
        md.AppendLine("A participating zero remains a zero read at the selected resolution. Zero-length sections retain their phases and sites; a source is not deleted because its rendered line collapses. The model has no evidence about finer physical participation. Replacing a hypothesis also does not mutate the observed source.");
        md.AppendLine();
        md.AppendLine("## Recorded construction states");
        md.AppendLine();
        md.AppendLine("| Probe | Machine | Admissible | Failed / unresolved | Sections / zero reads | Seeds / advances / equalities | Components | Closed degree condition | Equal-turn evidence |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var shape in report.Shapes)
            md.AppendLine($"| {shape.Scenario} | {Cell(shape.Name)} | {Yes(shape.Admissible)} | {shape.Failed} / {shape.Unresolved} | {shape.Sections} / {shape.ZeroSections} | {shape.SeedCount} / {shape.AdvanceCount} / {shape.EqualityCount} | {shape.Components} | {Yes(shape.Closed)} | {(shape.EqualTurns is null ? "Not selected" : shape.EqualTurns.Value ? "Present" : "Open")} |");
        md.AppendLine();
        md.AppendLine("Program counts expose the supplied construction's size in several ways. They are not a weighted complexity score, an algorithmic minimum or a measured search cost.");
        md.AppendLine();
        md.AppendLine("## Pair comparisons");
        md.AppendLine();
        md.AppendLine("Each row is one recorded comparison, under that input only. A rejected stronger read is useful evidence about a construction difference, not a bad ornament score.");
        md.AppendLine();
        md.AppendLine("| Probe | Pair | Outline | Directed | Coarse topology | Both admissible | Boundary exchange | + topology | Matched / source-only / target-only atoms |");
        md.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var pair in report.Comparisons)
            md.AppendLine($"| {pair.Scenario} | {pair.From} → {pair.To} | {Yes(pair.BoundaryAgrees)} | {Yes(pair.DirectedAgrees)} | {Yes(pair.TopologyAgrees)} | {Yes(pair.Admissible)} | {Yes(pair.BoundaryExchange)} | {Yes(pair.BoundaryAndTopologyExchange)} | {pair.Matches} / {pair.SourceOnly} / {pair.TargetOnly} |");
        md.AppendLine();
        md.AppendLine("## Radial count probe");
        md.AppendLine();
        md.AppendLine("These are supplied native contour attachments and successor relations with a conditional equal-turn promise. They test the structural count and closure read without pretending to solve a general polygon's geometry.");
        md.AppendLine();
        md.AppendLine("| Markers | Closed contour | Equal-turn evidence | Open obligations |");
        md.AppendLine("| --- | --- | --- | --- |");
        foreach (var contour in report.Contours)
            md.AppendLine($"| {contour.Markers} | {Yes(contour.Closed)} | {Yes(contour.EqualTurns)} | {contour.OpenObligations} |");
        md.AppendLine();
        md.AppendLine("## Limits and next use");
        md.AppendLine();
        md.AppendLine("The circular examples use an authored four-point centered-rectangle realization connected to contour evidence. Equal turns impose the extra equal-component obligation; open angular spacing is a weaker construction. This is not a general radial settlement solver, automatic inference from a picture, or a discovered construction history.");
        md.AppendLine();
        md.AppendLine("The shown positive notch lies in the bounded domain `1 <= notch < 2 * min(width, height)`, which gives the intended simple concave outline. This run tests a bounded subset of bindings; it does not certify that every value accepted by the general input grammar creates a simple polygon.");
        md.AppendLine();
        md.AppendLine("Candidate programs, extent bindings and tested contracts are supplied. This pass does not learn a new machine, choose an autonomous replacement, perform a material transfer or mutate an observed source. It records where a future exchange could be justified under a specified read, and where additional correspondence or relaxed constraints would be necessary. No agreement here guarantees agreement after an untested change.");
        File.WriteAllText(Path.Combine(output, "README.md"), md.ToString());
    }

    private static string Yes(bool value) => value ? "Yes" : "No";
    private static string Cell(string value) => value.Replace("|", "\\|").Replace("\n", " ");
    private static string ScenarioNote(string scenario) => scenario switch
    {
        "square" => "Matching initial outlines may hide different traversal, subdivisions and site identities.",
        "stretch" => "Aspect ratio changes. The equal-turn radial candidate must satisfy its retained equality.",
        "notch" => "Only a connected detour consumes the notch input. Other machines do not gain new phases automatically.",
        "zero-read" => "The horizontal extent reads zero. Sites and phases remain; coincident lines retain multiplicity.",
        _ => "Read the recorded obligations and correspondence."
    };

    private const string Page = """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Gojo · Construction exchange</title>
<style>
:root{color-scheme:light;--ink:#182e37;--muted:#566a70;--paper:#f4f4ed;--border:#d7ded9;--green:#206849;--red:#a23835;--blue:#2a6289}
*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.55 system-ui,sans-serif}main{max-width:1480px;margin:auto;padding:34px 28px 70px}h1{font:600 clamp(28px,3.5vw,46px)/1.12 Georgia,serif;max-width:950px;margin:12px 0 16px}h2{font-size:23px;margin:0 0 10px}h3{font-size:17px;margin:0}p{margin:0 0 12px}a{color:var(--blue)}.kicker{font:700 12px/1.4 system-ui;letter-spacing:.15em;text-transform:uppercase;color:var(--green)}.intro{max-width:990px;font-size:18px}.muted,small{color:var(--muted)}.lead{border-left:4px solid var(--green);padding:8px 18px;background:#e9eee5;margin:22px 0}.stats{display:flex;gap:12px;flex-wrap:wrap;margin:20px 0}.stat{background:white;border:1px solid var(--border);padding:9px 15px;border-radius:7px}.stat strong{font-size:24px;margin-right:6px}.controls{display:flex;gap:14px;align-items:end;flex-wrap:wrap;padding:16px;background:#e6ece8;border:1px solid var(--border);border-radius:10px}.controls label{display:flex;flex-direction:column;gap:5px;font-size:13px;font-weight:650}select,button{font:inherit;border:1px solid #8d9d9e;background:white;color:var(--ink);border-radius:5px;padding:9px 11px}select{min-width:220px}button{cursor:pointer}button:hover{background:#eef3ef}button:focus-visible,select:focus-visible,a:focus-visible{outline:3px solid #c87b27;outline-offset:3px}.check{display:flex!important;flex-direction:row!important;align-items:center;padding:10px 0}.check input{width:18px;height:18px}.scenario-note{margin:12px 0 20px;color:var(--muted)}.grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px}.card{border:1px solid var(--border);background:white;border-radius:10px;padding:15px;min-width:0}.card.invalid{background:#f6eeee;border-color:#cfa6a1}.card.selected{box-shadow:inset 0 0 0 2px var(--blue)}.card .caption{font-size:13px;min-height:70px;margin-top:10px}.tag{display:inline-block;border-radius:5px;padding:2px 7px;font-size:12px;font-weight:650;background:#edf1ed;color:var(--green);margin:8px 4px 0 0}.tag.bad{background:#f3dedb;color:var(--red)}.tag.neutral{background:#e8eef2;color:#3d5c70}.card svg{display:block;width:100%;height:185px;background:#fafaf5;border-radius:6px;margin:10px 0}.facts{font-size:12px;color:var(--muted);margin:6px 0}.pair{margin-top:30px;padding:22px;background:white;border:1px solid var(--border);border-radius:10px}.pair .controls{background:#f4f6f2}.pair-drawings{display:grid;grid-template-columns:1fr 1fr;gap:16px;margin:15px 0}.pair-drawings svg{width:100%;height:240px;background:#fafaf5;border:1px solid var(--border);border-radius:6px}.pair-drawings figcaption{font-weight:650;margin-bottom:5px}figure{margin:0}.table-scroll{overflow-x:auto}table{border-collapse:collapse;width:100%;font-size:14px}th,td{text-align:left;vertical-align:top;border-bottom:1px solid var(--border);padding:9px 12px}th{background:#f1f4ef}.yes{color:var(--green);font-weight:700}.no{color:var(--red);font-weight:700}.explain{margin:14px 0 0;padding:12px 15px;background:#edf3f0}.matrix th,.matrix td{text-align:center;padding:6px}.matrix th:first-child{text-align:left}.matrix button{font-size:12px;min-width:34px;padding:5px}.matrix button.full{background:#d6eadb;color:#20533b}.matrix button.partial{background:#e5edf6;color:#2b5070}.matrix button.denied{background:#f4e6e2;color:#823a34}.columns{display:grid;grid-template-columns:1fr 1fr;gap:24px;margin:30px 0}.note{border-top:2px solid var(--border);padding-top:16px}.note p,.note li{font-size:14px}details{margin-top:24px;border:1px solid var(--border);border-radius:8px;background:white;padding:15px}summary{font-weight:650;cursor:pointer}.section-label{font:600 10px system-ui;paint-order:stroke;stroke:#fafaf5;stroke-width:3px;stroke-linejoin:round}.axis{stroke:#d4ddd8;stroke-width:1;stroke-dasharray:3 5}.smallcaps{font-size:12px;letter-spacing:.05em;text-transform:uppercase}.noscript{padding:15px;background:#fff1d6}.footer{margin-top:25px;font-size:13px;color:var(--muted)}
@media(max-width:1100px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:650px){main{padding:20px 13px}.grid,.columns,.pair-drawings{grid-template-columns:1fr}.pair{padding:14px}select{min-width:0;max-width:100%}.controls label{width:100%}.stats{gap:6px}.stat{font-size:13px}.stat strong{font-size:20px}.matrix{font-size:11px}}
</style></head><body><main>
<div class="kicker">Gojo · Native construction experiment</div>
<h1>Same outline. Different ways to change.</h1>
<p class="intro">A closed walk, paired rails and radial points can draw the same square. This study keeps their construction rules visible, changes their inputs, and asks when one result can stand in for another.</p>
<div class="lead"><strong>The question is always “equivalent for which read?”</strong> Matching ink, matching traversal and matching connections are different claims. A proposed exchange must also respect both machines’ retained constraints.</div>
<p><a href="README.md">Markdown notes</a> · <a href="results.json">Recorded observations</a> · <a href="#limits">What this pass does not establish</a></p>
<p class="muted" style="font-size:13px">Native witness archives: <a href="square.gojo">square</a> · <a href="stretch.gojo">stretch</a> · <a href="notch.gojo">notch</a> · <a href="zero-read.gojo">zero read</a> · <a href="contours.gojo">contours</a>. These preserve connected evidence, beyond the report’s summaries.</p>
<div class="stats" id="stats"></div>
<noscript><p class="noscript">This page compares several supplied construction programs under square, stretch, notch and zero-at-resolution inputs. JavaScript displays the recorded drawings and pair results; the Markdown notes and JSON above contain the complete run without JavaScript. This experiment does not change an observed source or automatically substitute a machine.</p></noscript>
<div class="controls"><label>Input probe<select id="scenario" aria-describedby="scenario-note"></select></label><button id="previous" type="button">← Previous</button><button id="next" type="button">Next →</button><label class="check"><input id="arrows" type="checkbox" checked>Traversal arrows</label><label class="check"><input id="labels" type="checkbox" checked>Section numbers</label></div>
<p class="scenario-note" id="scenario-note"></p>
<div class="grid" id="machines"></div>
<p class="muted" style="font-size:13px;margin-top:12px">Each numbered line is one retained section. Hollow circles mark section endpoints in this realization; coincident circles do not prove shared identity. Orange dots mark zero-at-resolution sections that remain in the structure.</p>
<section class="pair" aria-labelledby="pair-title"><h2 id="pair-title">Inspect one possible exchange</h2><p>Choose two constructions. The verdict below belongs to this input and the stated observation contract.</p><div class="controls"><label>First machine<select id="from"></select></label><label>Second machine<select id="to"></select></label></div><div id="pair-drawings" class="pair-drawings"></div><div id="pair-result" aria-live="polite"></div></section>
<section class="pair"><h2>Exchange map for this input</h2><p class="muted">Green: outline and coarse topology agree, with both candidates admissible. Blue: boundary exchange only. Red: boundary exchange not supported. Click a cell to inspect the pair; direction is shown separately above.</p><div class="table-scroll" id="matrix"></div></section>
<div class="columns"><section class="note"><h2>What is actually connected?</h2><p>The supplied program contains role bindings, seeded sites, ordered advances, drawing edges and equality obligations. Execution expands the referenced shared reaches and keeps the resulting sections, matched obligations and unresolved or failed claims.</p><p>A path computes the next site from the previous one. Rails can share their intersection sites without having a single traversal. Detached rails can occupy the same screen coordinates while retaining separate endpoints.</p><p>The notch program already contains its extra phases. A zero notch binding hides their extent at this resolution; it does not delete them. Increasing the binding reveals the detour. Other programs do not acquire those phases just because the same notch input was supplied.</p></section><section class="note"><h2>What does “zero” say here?</h2><p>A participating zero is <strong>zero at the selected resolution</strong>. The read has no evidence about finer physical participation. Sites and phases remain unless an explicit structural operation removes them.</p><p>Likewise, revising a candidate description changes a hypothesis. It does not give that hypothesis permission to change its observed source. All drawings here are observations of supplied candidates.</p><p>Counts describe repeated shared reaches in a declared basis. The drawing maps that basis to pixels for inspection; pixel precision is not extra Gojo evidence.</p></section></div>
<details><summary>Radial count probes: changing the number of attached markers</summary><p style="margin-top:12px">These probes retain native contour attachments and successor relations. A supplied equal-turn promise is conditional construction evidence; the probe does not solve general polygon geometry.</p><div class="table-scroll" id="contours"></div></details>
<section class="note" id="limits" style="margin-top:30px"><h2>Limits that matter</h2><p><strong>The radial pictures use an authored four-point centered-rectangle realization.</strong> A common contour and cyclic adjacency are connected. Equal turns add an equal-component obligation; an angularly open construction is weaker. A failed equal-turn candidate is drawn red as a hypothetical result, never presented as an admissible circle construction. There is no general radial interpolation solver in this pass.</p><p>The outline reader splits cardinal and equal-step diagonal sections into shared-reach atoms, retaining occurrence multiplicity. The topology reader compares component count, closed-degree condition and non-degree-two incidence counts; it ignores degree-two subdivision and is <strong>not general graph isomorphism</strong>. Neither reader proves identical histories, flow, behavior under future inputs, or unrestricted substitution.</p><p>The programs, parameter probes and contracts are supplied. This is evidence for potential exchange points, not autonomous machine discovery, learning, material transfer or actual source mutation. A stronger exchange needs the additional correspondence and permissions it promises.</p></section>
<p class="muted" style="font-size:14px">The positive notch shown here satisfies <code>1 ≤ notch &lt; 2 × min(width, height)</code>, giving the intended simple concave outline. This bounded probe does not certify every binding as a simple polygon. Program counts describe seeds, advances and equalities in the supplied construction; they are neither a complexity minimum nor a measured search cost.</p>
<p class="footer">Native structure supplies the results. This page only renders the recorded read. Use the controls to inspect where a correspondence survives, where it fails, and which premise is responsible.</p>
</main><script type="application/json" id="data">@@DATA@@</script><script>
'use strict';
const data=JSON.parse(document.getElementById('data').textContent);
const $=id=>document.getElementById(id), esc=v=>String(v).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const colors=['#256a8c','#7256a1','#287c62','#507c37','#a06a25','#b15a36','#737d88','#63575a'];
const scenarios=[...new Set(data.Shapes.map(s=>s.Scenario))];
const titles={'square':'Square · equal initial reaches','stretch':'Stretch · change the aspect ratio','notch':'Notch · expose a connected detour','zero-read':'Zero read · retain the participating sections'};
const notes={'square':'Start with equal width and height. Similar outlines still have different section counts, traversal and endpoint identities.','stretch':'Increase width while keeping height. The equal-turn radial candidate retains its equality obligation, so an attractive rectangle alone cannot satisfy it.','notch':'Supply a participating notch reach. Only the program whose moves reference that role exposes a detour; the other programs keep their existing construction.','zero-read':'The horizontal extent reads zero. Collapsed sections and overlapping opposite lines retain their participation, multiplicity and site identities.'};
for(const value of scenarios)$('scenario').add(new Option(titles[value]||value,value));
for(const m of data.Machines){$('from').add(new Option(m.Name,m.Id));$('to').add(new Option(m.Name,m.Id));}
$('from').value=data.Machines[0].Id;$('to').value=data.Machines[Math.min(1,data.Machines.length-1)].Id;
$('stats').innerHTML=[['Machines',data.Machines.length],['Constructions',data.Shapes.length],['Pair reads',data.Comparisons.length],['Boundary exchanges',data.Comparisons.filter(c=>c.BoundaryExchange).length],['Also coarse topology',data.Comparisons.filter(c=>c.BoundaryAndTopologyExchange).length]].map(([name,n])=>`<div class="stat"><strong>${n}</strong>${esc(name)}</div>`).join('');
function shapes(){return data.Shapes.filter(s=>s.Scenario===$('scenario').value)}
function shape(id){return shapes().find(s=>s.Id===id)}
function pair(a,b){return data.Comparisons.find(c=>c.Scenario===$('scenario').value&&((c.From===a&&c.To===b)||(c.From===b&&c.To===a)))}
function svg(s,large=false){
 const points=shapes().flatMap(x=>x.Lines.flat()), extent=Math.max(1,...points.flat().map(Math.abs)), scale=68/extent;
 const px=x=>150+x*scale,py=y=>105-y*scale;
 const color=s.Admissible?colors[data.Machines.findIndex(m=>m.Id===s.Id)%colors.length]:'#b57973';
 let out=`<svg viewBox="0 0 300 210" role="img" aria-label="${esc(s.Name+', '+s.Scenario+(s.Admissible?'':', hypothetical failed candidate'))}"><line class="axis" x1="15" y1="105" x2="285" y2="105"/><line class="axis" x1="150" y1="17" x2="150" y2="193"/>`;
 const zeroPositions=new Map();
 s.Lines.forEach((line,index)=>{
   if(!line.length)return;
   const a=line[0],b=line[line.length-1],x=px(a[0]),y=py(a[1]),u=px(b[0]),v=py(b[1]),dx=u-x,dy=v-y,len=Math.hypot(dx,dy);
   if(len===0){const key=a.join(','),stack=zeroPositions.get(key)||0;zeroPositions.set(key,stack+1);out+=`<circle cx="${x}" cy="${y}" r="${5+stack*3}" fill="none" stroke="#c27b20" stroke-width="2"/>`;if($('labels').checked)out+=`<text class="section-label" x="${x+9}" y="${y-8-stack*12}" fill="#985b0d">${index+1} · zero</text>`;return;}
   out+=`<polyline points="${line.map(p=>px(p[0])+','+py(p[1])).join(' ')}" fill="none" stroke="${color}" stroke-width="${large?3.3:3}" stroke-linecap="round" stroke-linejoin="round" ${s.Admissible?'':'stroke-dasharray="6 4"'}/>`;
   out+=`<circle cx="${x}" cy="${y}" r="2.5" fill="#fafaf5" stroke="${color}"/><circle cx="${u}" cy="${v}" r="2.5" fill="#fafaf5" stroke="${color}"/>`;
   const nx=dx/len,ny=dy/len,mx=x+dx*.57,my=y+dy*.57;
   if($('arrows').checked)out+=`<path d="M ${mx-nx*5-ny*3.5} ${my-ny*5+nx*3.5} L ${mx+nx*5} ${my+ny*5} L ${mx-nx*5+ny*3.5} ${my-ny*5-nx*3.5} Z" fill="${color}"/>`;
   if($('labels').checked)out+=`<text class="section-label" text-anchor="middle" x="${(x+u)/2-ny*11}" y="${(y+v)/2+nx*11+3}" fill="${color}">${index+1}</text>`;
 });
 if(!s.Admissible)out+='<text x="150" y="201" text-anchor="middle" font-size="10" fill="#a23835">Hypothetical: retained obligations are not satisfied</text>';
 return out+'</svg>';
}
function badge(value,yes='Agrees',no='Does not agree'){return `<span class="${value?'yes':'no'}">${value?yes:no}</span>`}
function renderCards(){
 $('machines').innerHTML=shapes().map(s=>{const info=data.Machines.find(m=>m.Id===s.Id);return `<article class="card ${s.Admissible?'':'invalid'} ${[$('from').value,$('to').value].includes(s.Id)?'selected':''}"><h3>${esc(s.Name)}</h3><span class="tag ${s.Admissible?'':'bad'}">${s.Admissible?'Admissible candidate':'Failed / unresolved candidate'}</span>${svg(s)}<div class="facts">${s.Sections} sections · ${s.ZeroSections} zero reads · ${s.Components} component${s.Components===1?'':'s'}</div><div class="facts">Program: ${s.SeedCount} seeds · ${s.AdvanceCount} advances · ${s.EqualityCount} equalities</div><div class="facts">${s.Closed?'Closed degree condition':'Open / disconnected degree condition'} · ${s.Failed} failed · ${s.Unresolved} unresolved</div><p class="caption">${esc(info.Preserves)}</p><details><summary>Variation and limits</summary><p style="margin-top:9px">${esc(info.Changes)}</p>${s.EqualTurns===null?'':`<p>Equal-turn evidence: ${s.EqualTurns?'present in the retained contour promise':'open'}. Admissibility is checked separately.</p>`}</details></article>`}).join('');
}
function renderPair(){
 const a=shape($('from').value),b=shape($('to').value);if(!a||!b)return;
 $('pair-drawings').innerHTML=[a,b].map(s=>`<figure><figcaption>${esc(s.Name)}</figcaption>${svg(s,true)}</figure>`).join('');
 const c=pair(a.Id,b.Id);if(!c){$('pair-result').innerHTML='<p class="explain">Choose two different machines to inspect a recorded exchange. Identity is not included as a pair trial.</p>';return;}
 const rows=[['Undirected outline',c.BoundaryAgrees,'Same line atoms and multiplicity; ignores traversal and subdivision.'],['Directed outline',c.DirectedAgrees,'Also preserves the direction of every atom.'],['Coarse topology',c.TopologyAgrees,'Components, closed-degree condition and non-degree-two incidence; not graph isomorphism.'],['Both candidates admissible',c.Admissible,'Neither construction has a failed or unresolved tested obligation.'],['Boundary exchange supported',c.BoundaryExchange,'Undirected outline plus admissibility.'],['Boundary + coarse topology',c.BoundaryAndTopologyExchange,'Adds the selected coarse connectivity contract.']];
 const sourceOnly=c.From===a.Id?c.SourceOnly:c.TargetOnly,targetOnly=c.From===a.Id?c.TargetOnly:c.SourceOnly;
 $('pair-result').innerHTML=`<div class="table-scroll"><table><thead><tr><th>Selected read</th><th>Recorded result</th><th>What it does establish</th></tr></thead><tbody>${rows.map(([label,result,meaning])=>`<tr><td>${label}</td><td>${badge(result,'Yes','No')}</td><td>${meaning}</td></tr>`).join('')}</tbody></table></div><p class="facts">${c.Matches} matched atoms · ${sourceOnly} first-only · ${targetOnly} second-only · ${c.Unresolved} unresolved boundary entries</p><p class="explain">${!c.Admissible?'At least one candidate fails or leaves open a retained obligation. Even matching ink cannot authorize this exchange.':!c.BoundaryAgrees?'The outlines differ at the selected shared-reach resolution. This is not a boundary exchange point under the current bindings.':!c.TopologyAgrees?'The outlines agree, but the coarse connectivity read differs. A picture-only consumer can accept the boundary correspondence; a consumer requiring these connections cannot.':!c.DirectedAgrees?'The outlines and coarse connectivity agree, but directed traversal differs. A consumer that needs the same flow order must retain that distinction.':'The tested reads agree at this input. This is a candidate exchange point under those contracts, not a promise of identical behavior after another change.'}</p>`;
}
function renderMatrix(){
 const ms=data.Machines;
 $('matrix').innerHTML=`<table class="matrix"><thead><tr><th>Machine</th>${ms.map((m,i)=>`<th title="${esc(m.Name)}">${i+1}</th>`).join('')}</tr></thead><tbody>${ms.map((m,i)=>`<tr><th>${i+1}. ${esc(m.Name)}</th>${ms.map(n=>{if(m.Id===n.Id)return '<td>—</td>';const c=pair(m.Id,n.Id),full=c?.BoundaryAndTopologyExchange,partial=c?.BoundaryExchange;return `<td><button type="button" class="${full?'full':partial?'partial':'denied'}" data-from="${m.Id}" data-to="${n.Id}" aria-label="Inspect ${esc(m.Name+' and '+n.Name)}">${full?'B+T':partial?'B':'×'}</button></td>`}).join('')}</tr>`).join('')}</tbody></table>`;
 $('matrix').querySelectorAll('button').forEach(button=>button.addEventListener('click',()=>{$('from').value=button.dataset.from;$('to').value=button.dataset.to;renderCards();renderPair();$('pair-title').scrollIntoView({behavior:'smooth',block:'start'});}));
}
function render(){const sample=shapes()[0];$('scenario-note').textContent=`${notes[$('scenario').value]||''} Supplied reach counts: W ${sample.Width}, H ${sample.Height}, notch ${sample.Notch}.`;renderCards();renderPair();renderMatrix();}
function advance(delta){$('scenario').selectedIndex=($('scenario').selectedIndex+delta+scenarios.length)%scenarios.length;render();}
$('previous').addEventListener('click',()=>advance(-1));$('next').addEventListener('click',()=>advance(1));$('scenario').addEventListener('change',render);
for(const id of ['arrows','labels'])$(id).addEventListener('change',()=>{renderCards();renderPair();});
for(const id of ['from','to'])$(id).addEventListener('change',()=>{renderCards();renderPair();});
for(const id of ['scenario','from','to'])$(id).addEventListener('keydown',event=>{if(event.altKey||event.ctrlKey||event.metaKey||!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(event.key))return;event.preventDefault();const control=event.currentTarget,delta=['ArrowLeft','ArrowUp'].includes(event.key)?-1:1;control.selectedIndex=(control.selectedIndex+delta+control.options.length)%control.options.length;control.dispatchEvent(new Event('change'));});
$('contours').innerHTML=`<table><thead><tr><th>Markers</th><th>Closed contour</th><th>Equal-turn evidence</th><th>Open obligations</th></tr></thead><tbody>${data.Contours.map(c=>`<tr><td>${c.Markers}</td><td>${badge(c.Closed,'Present','Not established')}</td><td>${badge(c.EqualTurns,'Present','Open')}</td><td>${c.OpenObligations}</td></tr>`).join('')}</tbody></table>`;
render();
</script></body></html>
""";
}
