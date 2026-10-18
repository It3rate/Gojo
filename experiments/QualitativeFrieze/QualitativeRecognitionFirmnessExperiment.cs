using System.Net;
using System.Text.Json;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static class QualitativeRecognitionFirmnessExperiment
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);
        var study = new QualitativeRecognitionFirmnessStudy(Console.WriteLine);
        var fields = study.Fields.Select(field =>
        {
            var graph = Graph(field.Observation, field.Positions);
            int[] Ids(Relation scope) => EdgeIds(field.Observation, scope);
            var candidates = field.Evidence.Candidates.OrderByDescending(read => E(read.CircuitProofs).Length)
                .ThenBy(read => E(read.Outside).Length).Select((read, index) => new
                {
                    Id = $"D{index + 1:00}", Name = string.Join(" + ", E(read.Candidate.Basis).Select(channel => Channel(field.Induction, channel))),
                    Admitted = Ids(read.Admitted), Routes = Ids(read.RouteSupport), Circuits = Ids(read.CircuitSupport), Triangles = Ids(read.TriangleSupport),
                    Supported = Ids(read.Supported), AdmittedOnly = Ids(read.AdmittedOnly), Outside = Ids(read.Outside),
                    RouteProofs = E(read.RouteProofs).Length, CircuitProofs = E(read.CircuitProofs).Length, TriangleProofs = E(read.TriangleProofs).Length,
                    Branches = E(read.Competing).Length, Mismatches = E(read.Unsettled).Length, Boundaries = E(read.Boundary).Length,
                    Blocked = E(read.Blocked).Length, Unresolved = E(read.Unresolved).Length
                }).ToArray();
            return new { field.Id, field.Name, field.Description, Graph = graph, Candidates = candidates };
        }).ToArray();
        var chamberIds = study.Chambers.Select(occurrence => occurrence.Read).ToArray();
        var unitIds = study.Units.Occurrences.Select(occurrence => occurrence.Read).ToArray();
        var placements = study.Placements.Select(probe =>
        {
            var assignments = E(probe.Accepted).Cast<Relation>().Select(row => new[]
            {
                Array.FindIndex(chamberIds, item => ReferenceEquals(item, ((Relation)row.Left).Left)),
                Array.FindIndex(unitIds, item => ReferenceEquals(item, ((Relation)row.Right).Left))
            }).ToArray();
            var unknown = E(probe.Unresolved).Cast<Relation>().Select(row => Array.FindIndex(chamberIds,
                item => ReferenceEquals(item, ((Relation)row.Left).Left))).ToArray();
            return new
            {
                probe.Id, probe.Name, probe.Description, Graph = Graph(probe.Placement.Observation, probe.Positions), Assignments = assignments,
                Chambers = study.Chambers.Select(occurrence => E(occurrence.Route).Cast<Relation>().Select(use =>
                    SiteId(probe.Placement.Observation, ((Relation)use.Right).Left)).ToArray()).ToArray(),
                Units = study.Units.Occurrences.Select(occurrence => EdgeIds(probe.Placement.Observation, occurrence.Segments)).ToArray(),
                Unknown = unknown, Accepted = E(probe.Accepted).Length, Empty = E(probe.Empty).Length,
                Competing = E(probe.Competing).Length, Unresolved = E(probe.Unresolved).Length, Unplaced = E(probe.Unplaced).Length,
                InteriorQueries = probe.Queries.Count(query => E(query.InteriorSites).Length != 0),
                ConditionalInside = probe.Queries.Count(query => query.IsStrictlyInside)
            };
        }).ToArray();
        var families = study.PlacementSource.Motifs.Families.OrderBy(family => ReferenceEquals(family.Read, study.Units.Read) ? 0 : 1)
            .ThenBy(family => family.Kind == "cycle" ? 0 : family.Kind == "junction" ? 2 : 1).ThenBy(family => family.SegmentCount)
            .Select((family, index) => new
            {
                Name = $"M{index + 1:000} · {family.SegmentCount}-edge {family.Kind} · {family.Occurrences.Count} matches",
                Frames = study.Revalidations.Select(state =>
                {
                    var checks = state.Read.Checks.Where(check => ReferenceEquals(check.PriorFamily.Read, family.Read)).ToArray();
                    return new
                    {
                        state.Id, state.Name, state.Description, Supported = checks.Count(check => check.Status == "supported"),
                        Refuted = checks.Count(check => check.Status == "refuted"), Unresolved = checks.Count(check => check.Status == "unresolved"),
                        Matches = checks.Select(check => new { check.Status, Edges = EdgeIds(study.PlacementSource.Observation, check.PriorOccurrence.Segments) }).ToArray()
                    };
                }).ToArray()
            }).ToArray();
        var validation = new { Graph = Graph(study.PlacementSource.Observation, study.PlacementSource.Positions),
            Target = Array.FindIndex(E(study.PlacementSource.Observation.Segments), row => ReferenceEquals(row, study.Revalidations[0].Target)), Families = families };
        var model = new { Fields = fields, Placements = placements, Validation = validation };
        File.WriteAllText(Path.Combine(output, "gallery.html"), Head + "<script type=application/json id=native-data>" + JsonSerializer.Serialize(model) + "</script>" + Tail);
        var diagnostics = new
        {
            Fields = fields.Select(field => new { field.Id, field.Name, Segments = field.Graph.Edges.Length,
                Candidates = field.Candidates.Select(read => new { read.Id, read.Name, Admitted = read.Admitted.Length, Routes = read.Routes.Length,
                    Circuits = read.Circuits.Length, Triangles = read.Triangles.Length, Supported = read.Supported.Length, AdmittedOnly = read.AdmittedOnly.Length,
                    Outside = read.Outside.Length, read.RouteProofs, read.CircuitProofs, read.TriangleProofs, read.Branches, read.Mismatches, read.Boundaries, read.Blocked, read.Unresolved }) }),
            Placements = placements.Select(probe => new { probe.Id, probe.Name, probe.Accepted, probe.Empty, probe.Competing, probe.Unresolved, probe.Unplaced, probe.InteriorQueries, probe.ConditionalInside }),
            PreservedDiamond = families[0].Frames.Select(frame => new { frame.Id, frame.Name, frame.Supported, frame.Refuted, frame.Unresolved })
        };
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(diagnostics.Placements));
        Console.WriteLine(JsonSerializer.Serialize(diagnostics.PreservedDiamond));
        Console.WriteLine("Wrote " + Path.GetFullPath(output));
        return 0;
    }

    private static GraphModel Graph(QualitativeRecognitionObservation observation, IReadOnlyDictionary<IElement, QualitativeRecognitionPoint> positions)
    {
        var sites = E(observation.Sites);
        return new(sites.Select(site => new[] { positions[site].X, positions[site].Y }).ToArray(),
            E(observation.Segments).Cast<Relation>().Select(row =>
            {
                observation.TryReadSegment(row, out _, out var from, out var to, out _);
                return new[] { Array.FindIndex(sites, site => ReferenceEquals(site, from)), Array.FindIndex(sites, site => ReferenceEquals(site, to)) };
            }).ToArray());
    }
    private static int SiteId(QualitativeRecognitionObservation observation, IElement site) => Array.FindIndex(E(observation.Sites), actual => ReferenceEquals(actual, site));
    private static int[] EdgeIds(QualitativeRecognitionObservation observation, Relation scope) => E(scope).Select(row =>
        Array.FindIndex(E(observation.Segments), actual => ReferenceEquals(actual, row))).Where(id => id >= 0).Distinct().ToArray();
    private static string Channel(QualitativeDispositionInduction read, IElement channel)
    {
        if (channel is Relation row && E(read.DirectionFamilies).Any(actual => ReferenceEquals(actual, channel))) return Heading((Relation)row.Left);
        if (channel is Relation rhythm && read.TryReadRhythm(rhythm, out var flow)) return "cycle(" + string.Join(" / ", E(flow.Headings).Cast<Relation>().Select(Heading)) + ")";
        return "open";
        string Heading(Relation scope)
        {
            var directions = E(scope); var frame = read.Observation.Frame;
            var x = directions.Any(d => ReferenceEquals(d, frame.Right)) ? 1 : directions.Any(d => ReferenceEquals(d, frame.Left)) ? -1 : 0;
            var y = directions.Any(d => ReferenceEquals(d, frame.Up)) ? 1 : directions.Any(d => ReferenceEquals(d, frame.Down)) ? -1 : 0;
            return (x, y) switch { (1, 0) => "→", (-1, 0) => "←", (0, 1) => "↑", (0, -1) => "↓", (1, 1) => "↗", (-1, 1) => "↖", (1, -1) => "↘", (-1, -1) => "↙", _ => "?" };
        }
    }
    private static IElement[] E(Relation scope) => N.E(scope);
    private sealed record GraphModel(double[][] Nodes, int[][] Edges);

    private const string Head = """
<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>How firm is a structural reading</title><style>
*{box-sizing:border-box}body{margin:0;background:#e8e5db;color:#30443c;font:16px/1.6 system-ui,sans-serif}header,main{max-width:1440px;margin:auto;padding:30px}header{padding-top:55px}h1{font:clamp(36px,5vw,64px)/1.1 Georgia,serif;max-width:900px}h2{font:33px/1.2 Georgia,serif;margin:10px 0 20px}h3{font:22px/1.3 Georgia,serif;margin:8px 0}p{max-width:1080px}.eyebrow{font-size:11px;letter-spacing:.14em;color:#8a7159;font-weight:700}a{color:#855575}article{margin-bottom:38px;background:#faf7ed;border:1px solid #cac4b1;border-radius:5px;overflow:hidden;scroll-margin-top:20px}.copy{padding:25px 28px}.copy p{margin:10px 0}.controls{display:flex;gap:12px 22px;align-items:center;flex-wrap:wrap;padding:18px 28px;background:#f0ebdd;border-block:1px solid #d9d1bf}.controls label{display:flex;align-items:center;gap:8px;font-size:13px;max-width:100%}select,button{font:14px system-ui;color:#30443c;background:#fcf9f0;border:1px solid #bdb49e;padding:8px;border-radius:4px;max-width:100%}input{accent-color:#8d516f}.panels{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr)}figure{margin:0;min-width:0}figure+figure{border-left:1px solid #ddd4c2}figcaption{padding:12px 28px;font-size:13px;font-weight:600}svg{display:block;width:100%;height:auto}.stats{font-size:14px;font-weight:600;max-width:none}.small{font-size:13px;color:#756b57}details{padding:18px 28px}summary{cursor:pointer;font-size:14px}.table-wrap{overflow:auto;margin-top:12px}table{border-collapse:collapse;font-size:12px;white-space:nowrap;width:100%}td,th{padding:8px 12px;border-bottom:1px solid #ded6c4;text-align:left}.legend{display:flex;flex-wrap:wrap;gap:8px 20px;font-size:14px}.legend span:before{content:'●';margin-right:7px}.yes:before{color:#267b6c}.no:before{color:#b44755}.open:before{color:#c18b36}.extra:before{color:#7b5a98}nav{display:flex;flex-wrap:wrap;gap:14px;margin:24px 0}.explainer{border-left:3px solid #a99778;padding-left:18px}@media(max-width:750px){header,main{padding:16px}.copy,.controls{padding:20px}.panels{grid-template-columns:1fr}figure+figure{border-left:0;border-top:1px solid #ddd4c2}.controls label:has(select){display:grid;width:100%;gap:8px}select{width:100%;max-width:100%;min-width:0}h2{font-size:29px}}
</style><header><span class=eyebrow>GOJŌ · EVIDENCE, PLACEMENT AND LOCAL COUNTEREXAMPLES</span><h1>How firm is a<br>structural reading?</h1><p>A matching heading alone establishes neither a connected flow nor a grid circuit. Each claim needs its own evidence. A local motif can remain valid after its place in a larger arrangement changes. These experiments expose the evidence for each claim and the observations still needed to strengthen it.</p><div class=legend><span class=yes>Demonstrated participation / valid match</span><span class=extra>Admitted without the selected evidence</span><span class=no>Local counterexample / unplaced motif</span><span class=open>Unresolved obligation</span></div><nav><a href='#evidence'>1 Admission and evidence</a><a href='#placement'>2 Motifs in chambers</a><a href='#revalidation'>3 Preserved candidates</a><a href='../qualitative-recognition/gallery.html'>Earlier recognition gallery</a><a href='../../../../Gojo/docs/GojoQualitativeRecognitionFirmness.md'>Findings and structural laws</a><a href='diagnostics.json'>Diagnostics</a></nav><p class=small>The renderer retains geometry for inspection. Core recognition receives only published sites, segments and headings. Placement explicitly adds ordinal projection observations and declared whole-passage envelopes; it receives no numerical coordinates, lengths or authored face list.</p></header><main>
<article id=evidence><div class=copy><span class=eyebrow>01 · CLAIM STRENGTH</span><h2>Admission is an invitation to examine</h2><p>A direction basis admits matching segments. Longer routes, phase repetition, distinct-site commuting circuits and triangular circuits each need their own connected witnesses. Their support can overlap; the union counts each segment once.</p><h3 class=field-name></h3><p class=field-description></p></div><div class=controls><label>Field <select class=field></select></label><label>Reading <select class=candidate></select></label><label>Evidence <select class=evidence-mode><option value=Supported>All qualified local evidence</option><option value=Routes>Connected runs / phase routes</option><option value=Circuits>Four-site commuting circuits</option><option value=Triangles>Triangular circuits</option></select></label></div><div class=panels><figure><figcaption>Segments admitted by this direction basis</figcaption><div class=admission-picture></div></figure><figure><figcaption>Segments with the selected actual evidence</figcaption><div class=evidence-picture></div></figure></div><div class=copy><p class='stats evidence-stats'></p><p class=small>Green certifies participation in the selected local proof family. Purple segments are admitted but lack that proof. A complete green union does not certify a unique global grid or the best interpretation. Hollow, separated ends show distinct site identities even when their display positions coincide.</p></div><details><summary>Compare all retained basis questions</summary><div class='table-wrap evidence-table'></div></details></article>
<article id=placement><div class=copy><span class=eyebrow>02 · REFINING THE OBSERVATION</span><h2>The same motifs can occupy different places</h2><p>The original motif and grid readers see exactly the same segment observation in every state below. A supplemental reader receives only ordered projection buckets and explicit endpoint-box constraints. It asks whether each known line motif lies strictly inside each axis-aligned rectangular interval chamber.</p><p class=explainer>The arrangement question supplies one motif slot per chamber. Empty and competing slots describe that chosen goal relative to the selected finite motif family; they do not declare an ornament universally defective.</p><h3 class=placement-name></h3><p class=placement-description></p></div><div class=controls><label>Placement observation <select class=placement-state></select></label><label><input class=show-slots type=checkbox checked> Show chamber slots</label></div><div class=panels><figure><figcaption>Underlying segment observation · local motifs remain valid</figcaption><div class=placement-before></div></figure><figure><figcaption>Conditional whole-support placement and chamber-slot reads</figcaption><div class=placement-after></div></figure></div><div class=copy><p class='stats placement-stats'></p><p class=small>Ordinal equality is equal projection, never an automatic join. Strict placement concerns line support in a product of two intervals, conditional on the supplied passage constraints. It does not infer filled regions, arbitrary polygon interiors or a metric shape. Endpoint order alone cannot constrain an unseen bowing passage.</p></div></article>
<article id=revalidation><div class=copy><span class=eyebrow>03 · A CLAIM AND ITS DEPENDENCIES</span><h2>Keep the pattern, test its bindings</h2><p>The original native motif definitions and occurrence bindings remain fixed. One local heading or passage observation changes; each obligation is reapplied without rediscovering or relabelling the whole field. Missing from a closed inventory and unavailable in an open inventory yield different results.</p><h3 class=validation-name></h3><p class=validation-description></p></div><div class=controls><label>Preserved motif family <select class=validation-family></select></label><label>Observation <select class=validation-state></select></label><button class=previous>Previous</button><button class=next>Next</button></div><div class=panels><figure><figcaption>Original supported occurrences</figcaption><div class=validation-before></div></figure><figure><figcaption>Those same occurrences under the selected observation</figcaption><div class=validation-after></div></figure></div><div class=copy><p class='stats validation-stats'></p><p class=small>The ring marks the probed segment. The old geometric diagram remains a guide; a changed heading does not borrow its old direction from the drawing. Refutation belongs to this occurrence at these bindings. It does not erase the reusable motif, and unrelated matches remain available.</p></div></article>
</main>
""";

    private const string Tail = """
<script>
const study=JSON.parse(document.querySelector('#native-data').textContent),colours={supported:'#277d6b',refuted:'#b24957',unresolved:'#c28a36'};
function draw(g,styles={},options={}){const w=800,h=480,pad=50,xs=g.Nodes.map(p=>p[0]),ys=g.Nodes.map(p=>p[1]),loX=Math.min(...xs),hiX=Math.max(...xs),loY=Math.min(...ys),hiY=Math.max(...ys),scale=Math.min((w-2*pad)/Math.max(1,hiX-loX),(h-2*pad)/Math.max(1,hiY-loY)),p=id=>[w/2+(g.Nodes[id][0]-(loX+hiX)/2)*scale,h/2-(g.Nodes[id][1]-(loY+hiY)/2)*scale];let s='<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 800 480" role="img" aria-label="Native structural evidence"><rect width="800" height="480" fill="#faf7ed"/>';const degree=g.Nodes.map(()=>0);g.Edges.forEach(([a,b])=>{degree[a]++;degree[b]++;});if(options.cells)options.cells.forEach((cell,k)=>{const at=cell.map(p),n=options.counts[k],unknown=options.unknown.includes(k),c=unknown?'#c28a36':n===0?'#b24957':n>1?'#7b5a98':'#277d6b';s+=`<polygon points="${at.map(v=>v.join(',')).join(' ')}" fill="${c}" fill-opacity=".055" stroke="none"/>`;const x=at.reduce((v,a)=>v+a[0],0)/at.length,y=at.reduce((v,a)=>v+a[1],0)/at.length;if(options.labels)s+=`<g><rect x="${x-13}" y="${y-12}" width="26" height="24" rx="12" fill="#faf7ed"/><text x="${x}" y="${y+5}" text-anchor="middle" fill="${c}" font-family="system-ui" font-size="14">${unknown?(n?n+'+?':'?'):n}</text><title>Selected motif bindings for this chamber slot</title></g>`;});g.Edges.forEach(([a,b],i)=>{let q=p(a),r=p(b);if(degree[a]===1&&degree[b]===1&&g.Nodes.some((point,k)=>k!==a&&point[0]===g.Nodes[a][0]&&point[1]===g.Nodes[a][1])){const dx=r[0]-q[0],dy=r[1]-q[1],l=Math.hypot(dx,dy);q=[q[0]+dx/l*5,q[1]+dy/l*5];r=[r[0]-dx/l*5,r[1]-dy/l*5];}const st=styles[i]||{c:'#b8c0b0',w:1.6};s+=`<path data-edge="${i}" d="M ${q} L ${r}" fill="none" stroke="${st.c}" stroke-width="${st.w||3}" ${st.d?'stroke-dasharray="5 5"':''} stroke-linecap="round"/>`;});g.Nodes.forEach((_,id)=>{const at=p(id),peers=g.Nodes.filter(q=>q[0]===g.Nodes[id][0]&&q[1]===g.Nodes[id][1]).length,open=degree[id]===1||peers>1;s+=`<circle cx="${at[0]}" cy="${at[1]}" r="${peers>1?4.2:open?3:2}" fill="${open?'#faf7ed':'#657d6a'}" stroke="#657d6a" stroke-width="1"><title>${peers>1?peers+' independent site roles at this displayed position':'Actual site · '+degree[id]+' incident segments'}</title></circle>`;});if(options.target!==undefined){const edge=g.Edges[options.target],a=p(edge[0]),b=p(edge[1]);s+=`<circle class="target" cx="${(a[0]+b[0])/2}" cy="${(a[1]+b[1])/2}" r="10" fill="none" stroke="#7b5a98" stroke-width="2"/>`;}return s+'</svg>';}
function style(ids,c,w=3,d=false){return Object.fromEntries(ids.map(id=>[id,{c,w,d}]));}
const evidence=document.querySelector('#evidence'),field=evidence.querySelector('.field'),candidate=evidence.querySelector('.candidate'),mode=evidence.querySelector('.evidence-mode');study.Fields.forEach((f,i)=>field.add(new Option(f.Id+' · '+f.Name,i)));function candidates(){const f=study.Fields[Number(field.value)];candidate.replaceChildren();f.Candidates.forEach((r,i)=>candidate.add(new Option(r.Id+' · '+r.Name,i)));evidence.querySelector('.field-name').textContent=f.Id+' · '+f.Name;evidence.querySelector('.field-description').textContent=f.Description;const columns=['Id','Name','Admitted','Routes','Circuits','Triangles','Supported','AdmittedOnly'];evidence.querySelector('.evidence-table').innerHTML='<table><thead><tr>'+columns.map(c=>'<th>'+c+'</th>').join('')+'</tr></thead><tbody>'+f.Candidates.map(r=>'<tr>'+columns.map(c=>'<td>'+(Array.isArray(r[c])?r[c].length:r[c])+'</td>').join('')+'</tr>').join('')+'</tbody></table>';evidenceDraw();}function evidenceDraw(){const f=study.Fields[Number(field.value)],r=f.Candidates[Number(candidate.value)],support=r[mode.value];evidence.querySelector('.admission-picture').innerHTML=draw(f.Graph,style(r.Admitted,'#387d72'));evidence.querySelector('.evidence-picture').innerHTML=draw(f.Graph,{...style(r.Admitted,'#9873ad',2.7,true),...style(support,'#277d6b',3.7)});evidence.querySelector('.evidence-stats').textContent=r.Admitted.length+' admitted · '+r.Routes.length+' in longer routes · '+r.Circuits.length+' in commuting circuits · '+r.Triangles.length+' in triangular circuits. Unique proof union '+r.Supported.length+'; admitted without any selected proof '+r.AdmittedOnly.length+'; outside this basis '+r.Outside.length+'. '+r.CircuitProofs+' circuit witnesses · '+r.Branches+' branch obligations · '+r.Mismatches+' mismatches.';evidence.dataset.field=f.Id;evidence.dataset.candidate=r.Id;evidence.dataset.mode=mode.value;}field.onchange=candidates;candidate.onchange=evidenceDraw;mode.onchange=evidenceDraw;candidates();
const placement=document.querySelector('#placement'),placementState=placement.querySelector('.placement-state'),slots=placement.querySelector('.show-slots');study.Placements.forEach((p,i)=>placementState.add(new Option(p.Id+' · '+p.Name,i)));function placementDraw(){const p=study.Placements[Number(placementState.value)],counts=p.Chambers.map((_,k)=>p.Assignments.filter(a=>a[0]===k).length),styles={};p.Units.forEach((edges,k)=>{const containers=p.Assignments.filter(a=>a[1]===k),unknown=p.Unknown.length>0&&!containers.length,c=containers.length?(containers.some(a=>counts[a[0]]>1)?'#7b5a98':'#277d6b'):unknown?'#c28a36':'#b24957';Object.assign(styles,style(edges,c,3.6,unknown));});placement.querySelector('.placement-before').innerHTML=draw(p.Graph);placement.querySelector('.placement-after').innerHTML=draw(p.Graph,styles,{cells:p.Chambers,counts,unknown:p.Unknown,labels:slots.checked});placement.querySelector('.placement-name').textContent=p.Id+' · '+p.Name;placement.querySelector('.placement-description').textContent=p.Description;placement.querySelector('.placement-stats').textContent=p.Accepted+' strict whole-support assignments · '+p.Empty+' empty slots · '+p.Competing+' competing slots · '+p.Unresolved+' unresolved slots · '+p.Unplaced+' definitely unplaced motifs. The local motif family still has '+p.Units.length+' occurrences.';placement.dataset.state=p.Id;}placementState.onchange=placementDraw;slots.onchange=placementDraw;placementDraw();
const validation=document.querySelector('#revalidation'),family=validation.querySelector('.validation-family'),state=validation.querySelector('.validation-state');study.Validation.Families.forEach((f,i)=>family.add(new Option(f.Name,i)));study.Validation.Families[0].Frames.forEach((f,i)=>state.add(new Option(f.Id+' · '+f.Name,i)));function validate(){const f=study.Validation.Families[Number(family.value)],v=f.Frames[Number(state.value)],styles={};for(const status of ['supported','unresolved','refuted'])for(const match of v.Matches.filter(m=>m.Status===status))Object.assign(styles,style(match.Edges,colours[status],status==='supported'?3:4,status==='unresolved'));validation.querySelector('.validation-before').innerHTML=draw(study.Validation.Graph,style(f.Frames[0].Matches.flatMap(m=>m.Edges),'#277d6b',3),{target:study.Validation.Target});validation.querySelector('.validation-after').innerHTML=draw(study.Validation.Graph,styles,{target:study.Validation.Target});validation.querySelector('.validation-name').textContent=v.Id+' · '+v.Name;validation.querySelector('.validation-description').textContent=v.Description;validation.querySelector('.validation-stats').textContent=v.Supported+' supported · '+v.Refuted+' refuted · '+v.Unresolved+' unresolved retained occurrences. The original definition and bindings remain available.';validation.querySelector('.previous').disabled=Number(state.value)===0;validation.querySelector('.next').disabled=Number(state.value)===f.Frames.length-1;validation.dataset.state=v.Id;validation.dataset.family=family.value;}family.onchange=validate;state.onchange=validate;validation.querySelector('.previous').onclick=()=>{state.value=Number(state.value)-1;validate();};validation.querySelector('.next').onclick=()=>{state.value=Number(state.value)+1;validate();};validate();
</script></html>
""";
}
