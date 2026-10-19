using System.Text;
using System.Text.Json;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class LatticeLadderExperiment
{
    private static string Svg(string title, OrnamentProgramField field, BourgoinPatternLibrary.DisplayBasis basis)
    {
        var positions = new Dictionary<IElement, (double X, double Y)>(N.Participants.Instance);
        foreach (var row in N.E(field.Bindings).Cast<Relation>())
        {
            double x = 0, y = 0;
            foreach (var move in N.E((Relation)row.Right).Cast<Relation>())
            {
                if (!field.Graph.Frame.Direction(move, out var axis, out var forward, out _))
                    throw new InvalidOperationException("A displayed site must read from its actual exposed word.");
                var sign = forward ? 1 : -1;
                if (ReferenceEquals(axis, field.Graph.Frame.Horizontal)) { x += sign * basis.RX; y += sign * basis.RY; }
                else { x += sign * basis.UX; y += sign * basis.UY; }
            }
            if (positions.TryGetValue(row.Left, out var earlier) && (Math.Abs(earlier.X - x) > 1e-9 || Math.Abs(earlier.Y - y) > 1e-9))
                throw new InvalidOperationException("A current shared site cannot acquire inconsistent display bindings.");
            positions[row.Left] = (x, y);
        }
        var points = positions.Values.ToArray();
        var minX = points.Min(point => point.X); var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y); var maxY = points.Max(point => point.Y);
        var scale = Math.Min(660 / Math.Max(1, maxX - minX), 420 / Math.Max(1, maxY - minY));
        (double X, double Y) Screen(IElement site)
        { var point = positions[site]; return (360 + (point.X - (minX + maxX) / 2) * scale, 240 - (point.Y - (minY + maxY) / 2) * scale); }
        var neighbours = positions.Keys.ToDictionary(site => site, _ => new HashSet<IElement>(N.Participants.Instance), N.Participants.Instance);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 720 480' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='720' height='480' fill='#F8F5EC'/><g fill='none' stroke='#35535A' stroke-width='1.9' stroke-linejoin='round' stroke-linecap='round'>");
        foreach (var stroke in N.E(field.Graph.Strokes).Cast<Relation>())
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                throw new InvalidOperationException("A presented line must be an actual current graph stroke.");
            var a = Screen(from); var b = Screen(to);
            svg.Append($"<path d='M {F(a.X)} {F(a.Y)} L {F(b.X)} {F(b.Y)}'/>");
            neighbours[from].Add(to); neighbours[to].Add(from);
        }
        svg.Append("</g><g fill='#A87A42' opacity='.78'>");
        foreach (var pair in neighbours.Where(pair => pair.Value.Count >= 3))
        { var point = Screen(pair.Key); svg.Append($"<circle cx='{F(point.X)}' cy='{F(point.Y)}' r='2.45'/>"); }
        return svg.Append("</g></svg>").ToString();
    }

    private const string Css = """
*{box-sizing:border-box}body{margin:0;background:#ECE9DE;color:#30474C;font:16px/1.65 system-ui,sans-serif}main{max-width:1560px;margin:auto;padding:34px 28px 70px}h1,h2,h3{font-family:Georgia,serif;font-weight:400;line-height:1.17}h1{font-size:clamp(38px,5vw,68px);margin:16px 0}h2{font-size:33px;margin:12px 0}h3{font-size:23px;margin:10px 0}p{max-width:1050px}.eyebrow{font-size:12px;letter-spacing:.16em;text-transform:uppercase;color:#826D4A}a{color:#276370;text-underline-offset:4px}nav{display:flex;flex-wrap:wrap;gap:12px 24px;margin:22px 0}button,select{font:inherit;color:inherit;background:#FCFAF1;border:1px solid #B6B5A5;padding:8px 12px;max-width:100%;border-radius:3px}button{cursor:pointer}button[aria-pressed=true]{background:#355A62;color:white}label{display:flex;align-items:center;gap:12px;flex-wrap:wrap}.toolbar{display:flex;align-items:center;gap:14px 30px;flex-wrap:wrap;padding:18px;background:#F5F2E7;border:1px solid #CCC7B4;margin-top:28px}input[type=range]{width:220px;max-width:100%}.two{display:grid;grid-template-columns:1fr 1fr;gap:22px}.panel{min-width:0;background:#F7F4EB;border:1px solid #C9C3B0;padding:18px}.panel img,.card img{display:block;width:100%;height:auto}.note{font-size:14px;color:#6D756E}.status{font-size:14px;padding:10px 14px;background:#E3EADC;border-left:3px solid #5C8174}.status.open{background:#EFE2CD;border-color:#AE8243}.crumbs{display:flex;flex-wrap:wrap;gap:8px;margin:18px 0}.crumbs button{font-size:13px}.facts{display:flex;flex-wrap:wrap;gap:10px 24px;font-size:13px;color:#62736D}.cards{display:grid;grid-template-columns:repeat(4,1fr);gap:18px}.card{min-width:0;background:#F8F5EC;border:1px solid #D0CAB8;padding:14px;cursor:pointer}.card h3{font-size:19px}.card p{font-size:13px;margin:8px 0}.card.selected{outline:3px solid #5B827B}.reading{overflow:auto}table{border-collapse:collapse;font-size:13px;width:100%;margin:14px 0}th,td{padding:7px 10px;border-bottom:1px solid #D3CEBE;text-align:left;white-space:nowrap}section{margin:36px 0 55px}.concept{border-top:1px solid #C7C3B0;padding-top:22px}details{margin:16px 0}summary{cursor:pointer;color:#315F65}.branch-list{columns:2}code{font-size:.9em}.big-overview{display:grid;grid-template-columns:repeat(4,1fr);gap:16px}.big-overview figure{min-width:0;margin:0;background:#F8F5EC;padding:12px}.big-overview img{width:100%;height:auto;display:block}.big-overview figcaption{font:17px Georgia,serif;min-height:42px}@media(max-width:1000px){.cards,.big-overview{grid-template-columns:repeat(3,1fr)}}@media(max-width:700px){main{padding:24px 14px}.two{grid-template-columns:1fr}.cards,.big-overview{grid-template-columns:repeat(2,1fr)}.branch-list{columns:1}h2{font-size:27px}}@media(max-width:420px){.cards,.big-overview{grid-template-columns:1fr}.toolbar{padding:12px}}
""";

    private static string Html(IReadOnlyList<Plate> plates, IReadOnlyList<LatticeLadderCase> cases, object crystal)
    {
        var options = string.Join("", plates.Select(plate => $"<option value='{plate.Id}'>{H(plate.Id + " · " + plate.Name)}</option>"));
        var cards = string.Join("", plates.Select(plate => $"<article class='card' id='{plate.Id}' data-case='{plate.Id}' tabindex='0' role='button' aria-label='Inspect {H(plate.Name)}'><span class='eyebrow'>{plate.Id} / rung {plate.Depth}</span><h3>{H(plate.Name)}</h3><img src='{plate.Id}.svg' alt='{H(plate.Name)}' loading='lazy'><p>{H(plate.Atlas)}</p></article>"));
        var coverage = new BourgoinPatternLibrary().Recipes.Select(recipe =>
        {
            var hits = cases.Where(item => System.Text.RegularExpressions.Regex.IsMatch(item.AtlasReferences, @"\b" + System.Text.RegularExpressions.Regex.Escape(recipe.Id) + @"\b"))
                .Select(item => item.Id).ToArray();
            return $"<tr><td>{H(recipe.Id)}</td><td>{H(recipe.Name)}</td><td>{(hits.Length == 0 ? "Not constructed in this round" : H(string.Join(", ", hits)) + " · related construction; exact atlas equivalence unproved")}</td></tr>";
        });
        return """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Lattices by construction · Gojo</title><style>
""" + Css + """
</style></head><body><main><span class='eyebrow'>Gojo / structural ladders / native and pre-numeric</span><h1>A lattice can remember<br>what it was built from.</h1><p>A shared starting grid branches through native substitutions, chord refinements and explicit edits. Every earlier rung stays an actual structure that can answer a construction-history question. Current shape is a separate question, read from the executed graph with the recipe and history hidden.</p><nav><a href='overview.html'>All constructions together</a><a href='overview.svg'>Whole sheet · SVG</a><a href='../bourgoin-patterns/gallery.html'>Original Bourgoin atlas</a><a href='#crystallization'>Forget history, then read again</a><a href='#coverage'>Atlas coverage and limits</a><a href='diagnostics.json'>Diagnostics</a></nav>
<div class='toolbar'><label>Choose a branch end <select id='branch'>
""" + options + """
</select></label><label>Walk its ladder <input id='rung' type='range' min='0' max='1' value='1' step='1'><output id='step'></output></label></div><div class='crumbs' id='crumbs'></div>
<section id='inspection'><div class='two'><article class='panel'><span class='eyebrow' id='current-label'></span><h2 id='current-title'></h2><img id='current-image' alt='Current executed lattice'><p id='description'></p><p class='note' id='atlas'></p><div class='facts' id='facts'></div></article><article class='panel'><span class='eyebrow'>Question / retained construction basis</span><h2>An earlier rung, literally.</h2><label>Ask for <select id='basis'></select></label><img id='basis-image' alt='Selected retained construction basis'><p class='status' id='basis-answer'></p><p class='note'>This answer follows retained native references. It does not analyze pixels or prove that every property of the earlier graph remains true of the current graph. A brick-joint edit changes connectivity; a shear can preserve it.</p><p><a id='native-link'>Native ladder + field packet</a> · <a id='question-link'>Native questions and witnesses</a></p></article></div>
<article class='panel concept'><span class='eyebrow'>Question / current incidence / construction history hidden</span><h2>What can this graph support now?</h2><p class='note' id='support-note'></p><div class='reading' id='candidates'></div><details><summary>Drawing histories, support and limits</summary><p>Every acknowledged stroke remains in the fine view. The incidence-support question coalesces only already-shared endpoint pairs with compatible headings and retains a many-to-one correspondence. The strand-support question may additionally contract justified straight degree-two landmarks, keeping their paths and sites as carried evidence. Corners, branches, unjoined crossings and coordinate coincidences cannot supply those contractions. Counts are calculated reads, not confidence scores.</p><div class='reading' id='raw-candidates'></div></details><p class='note'>Commutations are witnessed two-step routes to the same actual site. Triangles are witnessed closed routes. Residue, competing continuations, unsearched proposals and finite boundary obligations stay visible. A channel count alone is not a grid diagnosis.</p></article></section>
<section><span class='eyebrow'>Shared source / several branches</span><h2>Small operations, different constructions.</h2><div class='cards'>
""" + cards + """
</div></section><section id='crystallization' class='concept'><span class='eyebrow'>Deliberate loss / independently identified current world</span><h2>Keep the lattice. Forget its route here.</h2><div class='two'><article class='panel'><h3>L05 · retained shear history</h3><img src='L05.svg' alt='Sheared grid with its construction history'><p>The actual square-grid basis remains available through the ladder.</p></article><article class='panel'><h3>The same current incidence, standalone</h3><img src='crystallized.svg' alt='Independently crystallized current lattice'><p>The current frame, paths, ports, periods and seams are independently rebuilt. The saved standalone world contains no participant from the original history. Exact ancestry lookup is open; a fresh history-blind support read can still recover a grid organization.</p><p><a href='crystallized.gojo.json.gz'>Standalone native world</a> · <a href='crystallization-loss.gojo.json.gz'>External loss receipt</a> · <a href='crystallized-questions.gojo.json.gz'>New observation and queries</a></p></article></div><p class='note'>The external loss receipt explains the deliberate forgetting and is stored separately. Retaining that receipt is different from supplying history to the standalone lattice. Recovering a coarse grid does not reconstruct the exact forgotten sequence of edits.</p></section>
<section class='concept'><span class='eyebrow'>Counterexample / blocked evidence remains visible</span><h2>A false seam cannot become a join.</h2><div class='two'><div class='panel'><img src='blocked-seam.svg' alt='Current geometry under a deliberately false seam offer'></div><div class='panel'><p>One offered neighbouring role has the wrong displacement. The actual equality read fails and the offered connection stays blocked. A plausible drawing cannot override that obligation.</p><p><a href='blocked-seam.gojo.json.gz'>Native blocked-seam witness</a></p><p class='note'>Accepted seams authenticate exposed endpoint agreements. They do not prove infinite tiling, occupied-area coverage, no retracing or aesthetic merit.</p></div></div></section>
<section id='coverage' class='concept'><span class='eyebrow'>Starting vocabulary / 36 atlas skeletons</span><h2>Construction families, with a clear frontier.</h2><p>This first round explores grid, brick, oblique mesh, triangular ties, ribbons, corner cuts and basket assemblies. The last three siblings deliberately vary existing substitutions to produce new nearby arrangements. We do not claim to have reconstructed all 36 atlas patterns through ladders.</p><div class='reading'><table><thead><tr><th>Atlas</th><th>Skeleton</th><th>Current ladder relation</th></tr></thead><tbody>
""" + string.Join("", coverage) + """
</tbody></table></div><p>Three different questions stay separate: “what was this built from?”, “what organization does this current graph support?”, and “which other construction might explain it?”. Native ancestry answers the first directly. The bounded structural readers supply evidence for the second. General inverse reconstruction remains an experiment.</p><p class='note'>Host integers author repetitions of an unmeasured reach. Optional screen coordinates, line widths and aspect ratios belong to presentation. Native ladders, field programs and observations use relations and placeholders, with no numeric carriers. Selected square and triangular appearances do not establish a native Euclidean angle or length theorem.</p></section>
<script type='application/json' id='cases'>
""" + JsonSerializer.Serialize(plates, Options) + """
</script><script>
const cases=JSON.parse(document.getElementById('cases').textContent),byId=new Map(cases.map(c=>[c.id,c]));
const branch=document.getElementById('branch'),rung=document.getElementById('rung'),basis=document.getElementById('basis');let current,chain;
function table(rows){return '<table><thead><tr><th>Channels</th><th>Selected</th><th>Commutations</th><th>Triangles</th><th>Unexpected triangles</th><th>Residue</th><th>Competing</th><th>Boundary</th><th>Unsettled</th></tr></thead><tbody>'+rows.map(r=>'<tr>'+[r.channels,r.selected,r.commutations,r.triangles,r.unexpectedTriangles,r.residue,r.competing,r.boundary,r.unsettled].map(v=>'<td>'+v+'</td>').join('')+'</tr>').join('')+'</tbody></table>';}
function showBasis(){const selected=byId.get(basis.value);document.getElementById('basis-image').src=selected.id+'.svg';const actual=current.ancestors.find(a=>a.id===selected.id);document.getElementById('basis-answer').textContent=actual?'Returned the actual retained '+selected.id+' program. Native identity preserved: '+actual.sameActualProgram+'. No shape search was requested.':'This is the current starting rung itself; no earlier construction is required.';}
function show(){current=byId.get(chain[Number(rung.value)]);document.getElementById('step').textContent=(Number(rung.value)+1)+' / '+chain.length;document.getElementById('current-label').textContent=current.id+' / construction depth '+current.depth;document.getElementById('current-title').textContent=current.name;document.getElementById('current-image').src=current.id+'.svg';document.getElementById('description').textContent=current.description;document.getElementById('atlas').textContent=current.atlas;document.getElementById('facts').textContent=current.paths+' paths / '+current.chords+' chords per unit · '+current.accepted+' accepted seams · '+current.blocked+' blocked · '+current.frontier+' finite frontier obligations';basis.replaceChildren();for(const a of current.ancestors.length?current.ancestors:[current]){const option=document.createElement('option');option.value=a.id;option.textContent=a.id+' · '+a.name;basis.append(option);}showBasis();document.getElementById('native-link').href=current.id+'.gojo.json.gz';document.getElementById('question-link').href=current.id+'-questions.gojo.json.gz';document.getElementById('support-note').textContent=current.rawStrokes+' acknowledged drawing strokes → '+current.incidenceStrokes+' incidence supports → '+current.supportStrokes+' segments in the selected straight-strand question. '+current.unsearched+' proposals remain unsearched. This read receives no recipe or ancestry.';document.getElementById('candidates').innerHTML=table(current.support);document.getElementById('raw-candidates').innerHTML=table(current.raw);for(const card of document.querySelectorAll('.card'))card.classList.toggle('selected',card.dataset.case===current.id);for(const button of document.querySelectorAll('#crumbs button'))button.setAttribute('aria-pressed',button.dataset.case===current.id);}
function choose(id){const end=byId.get(id);branch.value=id;chain=[...end.ancestors.map(a=>a.id),id];rung.max=chain.length-1;rung.value=chain.length-1;document.getElementById('crumbs').replaceChildren();chain.forEach((item,index)=>{const button=document.createElement('button');button.textContent=item+' · '+byId.get(item).name;button.dataset.case=item;button.addEventListener('click',()=>{rung.value=index;show();});document.getElementById('crumbs').append(button);});history.replaceState(null,'','#'+id);show();}
const deepLinked=byId.has(location.hash.slice(1));
window.addEventListener('hashchange',()=>{const id=location.hash.slice(1);if(byId.has(id)){choose(id);document.getElementById('inspection').scrollIntoView();}});
window.addEventListener('load',()=>{if(deepLinked)requestAnimationFrame(()=>document.getElementById('inspection').scrollIntoView());});
branch.addEventListener('change',()=>choose(branch.value));rung.addEventListener('input',show);basis.addEventListener('change',showBasis);for(const card of document.querySelectorAll('.card')){card.addEventListener('click',()=>{choose(card.dataset.case);document.getElementById('inspection').scrollIntoView({behavior:'smooth',block:'start'});});card.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();card.click();}});}choose(byId.has(location.hash.slice(1))?location.hash.slice(1):'L04');
</script></main></body></html>
""";
    }

    private static string OverviewHtml(IReadOnlyList<Plate> plates) =>
        "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Lattice ladders · whole sheet</title><style>" + Css +
        "</style><main><span class='eyebrow'>Gojo / native lattice constructions</span><h1>One starting grid.<br>Several construction branches.</h1><p>Each image is an executed native field. Parent links name actual retained earlier rungs. Atlas references indicate related construction families, rather than certified image equality.</p><nav><a href='gallery.html'>Walk the ladders</a><a href='overview.svg'>Whole sheet · SVG</a><a href='../bourgoin-patterns/overview.html'>Bourgoin atlas</a></nav><div class='big-overview'>" +
        string.Join("", plates.Select(plate => $"<figure id='{plate.Id}'><figcaption><a href='gallery.html#{plate.Id}'>{plate.Id} · {H(plate.Name)}</a></figcaption><img src='{plate.Id}.svg' alt='{H(plate.Name)}'><p class='note'>{(plate.ParentId is null ? "Starting structure" : "From " + plate.ParentId)} · {H(plate.Atlas)}</p></figure>")) + "</div></main></html>";

    private static string OverviewSvg(IReadOnlyList<Plate> plates, string output)
    {
        var height = 115 + (int)Math.Ceiling(plates.Count / 4d) * 340;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1880 {height}' width='1880' height='{height}'><rect width='1880' height='{height}' fill='#ECE9DE'/><text x='30' y='44' font-family='Georgia,serif' font-size='34' fill='#30474C'>Lattices by construction · native ladder branches</text><text x='32' y='79' font-family='sans-serif' font-size='17' fill='#66746D'>Actual retained bases · word substitutions, explicit edits and assemblies · selected screen proportions</text>");
        for (var index = 0; index < plates.Count; index++)
        {
            var item = plates[index]; var x = 20 + index % 4 * 465; var y = 115 + index / 4 * 340;
            var root = System.Xml.Linq.XElement.Parse(File.ReadAllText(Path.Combine(output, item.Id + ".svg")));
            root.SetAttributeValue("x", x); root.SetAttributeValue("y", y + 35); root.SetAttributeValue("width", 450); root.SetAttributeValue("height", 300);
            svg.Append($"<text x='{x + 4}' y='{y + 20}' font-family='Georgia,serif' font-size='18' fill='#30474C'>{H(item.Id + " · " + item.Name)}</text>{root}");
        }
        return svg.Append("</svg>").ToString();
    }
}
