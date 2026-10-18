using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Display adapter for native recognition reads. Positions belong to
/// the fixture's external renderer and are never inputs to either inducer.</summary>
public static class QualitativeRecognitionExperiment
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);
        var study = new QualitativeRecognitionStudy(Console.WriteLine);
        var models = study.Cases.Select(Model).ToArray();
        var cards = new StringBuilder();
        for (var i = 0; i < models.Length; i++)
        {
            var model = models[i];
            cards.Append($"""
<article id='{model.Id}' data-case='{i}'>
<div class=copy><span class=eyebrow>{model.Id} · {model.Sites} SITES / {model.Edges.Length} SEGMENTS</span><h2>{H(model.Name)}</h2><p>{H(model.Description)}</p><p class=search>{H(model.Search)}</p></div>
<div class=panels><section><div class=controls><label>Local reading <select class=motif></select></label><label><input class=all type=checkbox checked> All matches</label><label>Match <input class=occurrence type=range min=0 max=0 value=0></label></div><div class='drawing motif-drawing'></div><div class=read-copy><h3 class=motif-name></h3><p class=motif-stats></p><div class=prototype></div></div></section>
<section><div class=controls><label>Flow / disposition <select class=disposition></select></label><label><input class=circuits type=checkbox> Show commuting circuits</label></div><div class='drawing disposition-drawing'></div><div class=read-copy><h3 class=disposition-name></h3><p class=disposition-stats></p><p class=related></p></div></section></div>
<details><summary>Compare all larger readings</summary><div class=table-wrap>{CandidateTable(model)}</div></details>
<div class=copy><p class=small>{H(model.Limits)}</p><a href='{model.Id}.svg'>Published field diagram</a></div></article>
""");
            File.WriteAllText(Path.Combine(output, model.Id + ".svg"), StaticPicture(model));
        }
        var diagnostics = models.Select(model => new
        {
            model.Id, model.Name, model.Sites, Segments = model.Edges.Length,
            model.Search, model.MotifComplete, model.DispositionComplete,
            Motifs = model.Motifs.Select(motif => new { motif.Name, motif.Kind, Matches = motif.Occurrences.Length, motif.Size }),
            Interpretations = model.Readings.Select(reading => new
            {
                reading.Key, reading.Name, reading.Kind, Covered = reading.Selected.Length, Remainder = reading.Residue.Length,
                reading.Commutations, reading.Triangles, reading.Unexpected, reading.Boundary, reading.Branches,
                reading.Unsettled, reading.MotifContained, reading.MotifStraddling, reading.Refinements
            })
        }).ToArray();
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(output, "gallery.html"), Head + cards +
            "</main><script type=application/json id=native-data>" + JsonSerializer.Serialize(models) + "</script>" + Tail);
        Console.WriteLine($"Recognition-only: {models.Length} published fields. Wrote {Path.GetFullPath(output)}");
        foreach (var model in models)
            Console.WriteLine($"{model.Id}: {model.Edges.Length} segments; {model.Motifs.Length} local families; {model.Readings.Length} larger readings; motif search {(model.MotifComplete ? "complete" : "cut / open")}");
        return 0;
    }

    private static FieldModel Model(QualitativeRecognitionCase field)
    {
        var observation = field.Observation;
        var rows = E(observation.Segments).Cast<Relation>().ToArray();
        var sites = E(observation.Sites);
        var siteIds = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        var edgeIds = new Dictionary<IElement, int>(ReferenceEqualityComparer.Instance);
        for (var i = 0; i < sites.Length; i++) siteIds.Add(sites[i], i);
        for (var i = 0; i < rows.Length; i++) edgeIds.Add(rows[i], i);
        var nodes = sites.Select(site => new[] { field.Positions[site].X, field.Positions[site].Y }).ToArray();
        var edges = rows.Select(row =>
        {
            observation.TryReadSegment(row, out _, out var from, out var to, out var heading);
            return new EdgeModel(siteIds[from], siteIds[to], Heading(observation, heading));
        }).ToArray();
        var crossings = E(observation.Crossings).Cast<Relation>().Select(row =>
        {
            observation.TryReadCrossing(row, out var first, out var second);
            return new[] { edgeIds[first], edgeIds[second] };
        }).ToArray();
        int[] Ids(Relation scope) => E(scope).Where(edgeIds.ContainsKey).Select(row => edgeIds[row]).Distinct().ToArray();
        var motifs = field.Motifs.Families.OrderBy(family => family.Kind == "cycle" ? 0 : family.Kind == "walk" ? 1 : 2)
            .ThenBy(family => family.SegmentCount).ThenByDescending(family => family.Occurrences.Count)
            .Select((family, index) => new MotifModel(
                $"M{index + 1:000} · {family.SegmentCount}-{(family.Kind == "junction" ? "arm junction" : "edge " + family.Kind)} · {family.Occurrences.Count} matches",
                family.Kind, family.SegmentCount,
                family.Occurrences.Select(occurrence => Ids(occurrence.Segments)).ToArray())).ToArray();
        var readings = new List<ReadingModel>();
        foreach (var item in E(field.Disposition.Candidates).Cast<Relation>())
        {
            field.Disposition.TryReadCandidate(item, out var candidate);
            var basis = E(candidate.Basis);
            var channelNames = basis.Select(channel => ChannelName(field.Disposition, channel)).ToArray();
            var channelEdges = E(candidate.Strands).Cast<Relation>().Select(channel =>
                E((Relation)channel.Right).Cast<Relation>().SelectMany(QualitativeDispositionInduction.RouteUses)
                    .Select(use => edgeIds[use.Left]).Distinct().ToArray()).ToArray();
            var proofEdges = E(candidate.Commutations).Cast<Relation>().Select(proof =>
            {
                var paths = (Relation)proof.Right;
                return E((Relation)paths.Left).Concat(E((Relation)paths.Right)).Cast<Relation>()
                    .Select(use => edgeIds[use.Left]).Distinct().ToArray();
            }).ToArray();
            var model = new ReadingModel(string.Join(" + ", channelNames), "basis", Ids(candidate.SelectedSegments), Ids(candidate.Residue), channelEdges,
                E(candidate.Commutations).Length, E(candidate.Closures).Length, E(candidate.UnexpectedClosures).Length,
                E(candidate.Boundary).Length, E(candidate.Competing).Length, E(candidate.Unsettled).Length, proofEdges);
            var native = field.Readings.Readings.Single(reading => ReferenceEquals(reading.Candidate.Read, candidate.Read));
            model.MotifContained = E(native.Contained).Length;
            model.MotifStraddling = E(native.Straddling).Length;
            model.Refinements = E(field.Readings.Refinements).Cast<Relation>().Count(extension =>
                extension.Left is Relation pair && ReferenceEquals(pair.Left, native.Surface));
            readings.Add(model);
        }
        foreach (var item in E(field.Disposition.Rhythms).Cast<Relation>())
        {
            field.Disposition.TryReadRhythm(item, out var rhythm);
            var routes = E(rhythm.Routes).Cast<Relation>().Select(route => QualitativeDispositionInduction.RouteUses(route).Select(use => edgeIds[use.Left]).Distinct().ToArray()).ToArray();
            readings.Add(new(ChannelName(field.Disposition, item), "rhythm", Ids(rhythm.SelectedSegments), Ids(rhythm.Residue), routes,
                0, 0, 0, E(rhythm.Boundary).Length, E(rhythm.Competing).Length, 0, []));
        }
        // Presentation order only: no candidate is removed or promoted to a
        // semantic winner by these count projections.
        var ordered = readings.OrderByDescending(reading => reading.Commutations)
            .ThenBy(reading => reading.Residue.Length).ThenBy(reading => reading.Branches).ToArray();
        for (var i = 0; i < ordered.Length; i++) ordered[i].Key = $"D{i + 1:00}";
        var proposalSlots = E((Relation)field.Motifs.Budget.Left).Length;
        var retainedSlots = E((Relation)((Relation)field.Motifs.Budget.Right).Left).Length;
        var expanded = proposalSlots - E((Relation)field.Motifs.Available.Left).Length;
        var retained = motifs.Sum(motif => motif.Occurrences.Length);
        var cuts = E(field.Motifs.Unsearched).Length;
        var search = $"Local search: {expanded} / {proposalSlots} expansions; {retained} / {retainedSlots} retained occurrences, {motifs.Length} families. Walks ≤3 edges; cycles ≤6. " +
            (field.Motifs.IsComplete ? "Selected scope exhausted." : $"Incomplete: {cuts} unsearched cuts and {E(field.Motifs.Unresolved).Length} open observations. Retained matches remain valid.");
        return new(field.Id, field.Name, field.Description, sites.Length, nodes, edges, crossings, motifs, ordered, search,
            field.Motifs.IsComplete, field.Disposition.IsComplete,
            "Cycles mean connected circuits, not proven empty faces or exact metric shapes. Heading-compatible flows need not be geometrically straight. Boundary counts are open finite-patch continuation obligations, not defects. Coordinates are retained by the display supplier; neither recognizer receives them. Overlapping readings observe the same segments without consuming or duplicating material.");
    }

    private static string ChannelName(QualitativeDispositionInduction read, IElement channel)
    {
        if (channel is Relation row && E(read.DirectionFamilies).Any(actual => ReferenceEquals(actual, channel)))
            return Heading(read.Observation, (Relation)row.Left) + " channel";
        if (channel is Relation actual && read.TryReadRhythm(actual, out var rhythm))
            return string.Join(" / ", E(rhythm.Headings).Cast<Relation>().Select(heading => Heading(read.Observation, heading))) + " two-phase flow";
        return "open channel";
    }
    private static string Heading(QualitativeRecognitionObservation observation, Relation scope)
    {
        var values = E(scope);
        var x = values.Any(d => ReferenceEquals(d, observation.Frame.Right)) ? 1 : values.Any(d => ReferenceEquals(d, observation.Frame.Left)) ? -1 : 0;
        var y = values.Any(d => ReferenceEquals(d, observation.Frame.Up)) ? 1 : values.Any(d => ReferenceEquals(d, observation.Frame.Down)) ? -1 : 0;
        return (x, y) switch { (1, 0) => "→", (-1, 0) => "←", (0, 1) => "↑", (0, -1) => "↓", (1, 1) => "↗", (-1, 1) => "↖", (1, -1) => "↘", (-1, -1) => "↙", _ => "open" };
    }
    private static string CandidateTable(FieldModel field) => "<table><thead><tr><th>Actual channel read</th><th>Support</th><th>Remainder</th><th>Commutes</th><th>Triangles</th><th>Cross-basis triangles</th><th>Boundary</th><th>Branches</th><th>Mismatch</th></tr></thead><tbody>" +
        string.Join("", field.Readings.Select(reading => $"<tr><td>{reading.Key} · {H(reading.Name)}</td><td>{reading.Selected.Length}</td><td>{reading.Residue.Length}</td><td>{reading.Commutations}</td><td>{reading.Triangles}</td><td>{reading.Unexpected}</td><td>{reading.Boundary}</td><td>{reading.Branches}</td><td>{reading.Unsettled}</td></tr>")) + "</tbody></table>";
    private static string StaticPicture(FieldModel field)
    {
        var minX = field.Nodes.Min(p => p[0]); var maxX = field.Nodes.Max(p => p[0]);
        var minY = field.Nodes.Min(p => p[1]); var maxY = field.Nodes.Max(p => p[1]);
        var scale = Math.Min(720 / Math.Max(1, maxX - minX), 440 / Math.Max(1, maxY - minY));
        string P(int id) => F(400 + (field.Nodes[id][0] - (minX + maxX) / 2) * scale) + "," + F(260 - (field.Nodes[id][1] - (minY + maxY) / 2) * scale);
        return "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 800 520'><title>" + H(field.Id + " " + field.Name) + "</title><rect width='800' height='520' fill='#faf7ee'/>" +
            string.Join("", field.Edges.Select(edge => $"<path d='M {P(edge.From)} L {P(edge.To)}' fill='none' stroke='#3b716a' stroke-width='2'/>")) +
            string.Join("", Enumerable.Range(0, field.Nodes.Length).Select(id => $"<circle cx='{P(id).Split(',')[0]}' cy='{P(id).Split(',')[1]}' r='2.6' fill='#345952'/>")) + "</svg>";
    }
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static IElement[] E(Relation scope) => N.E(scope);

    private sealed record EdgeModel(int From, int To, string Heading);
    private sealed record MotifModel(string Name, string Kind, int Size, int[][] Occurrences);
    private sealed record FieldModel(string Id, string Name, string Description, int Sites, double[][] Nodes,
        EdgeModel[] Edges, int[][] Crossings, MotifModel[] Motifs, ReadingModel[] Readings,
        string Search, bool MotifComplete, bool DispositionComplete, string Limits);
    private sealed class ReadingModel(string name, string kind, int[] selected, int[] residue, int[][] channels,
        int commutations, int triangles, int unexpected, int boundary, int branches, int unsettled, int[][] circuits)
    {
        public string Key { get; set; } = "";
        public string Name { get; } = name;
        public string Kind { get; } = kind;
        public int[] Selected { get; } = selected;
        public int[] Residue { get; } = residue;
        public int[][] Channels { get; } = channels;
        public int Commutations { get; } = commutations;
        public int Triangles { get; } = triangles;
        public int Unexpected { get; } = unexpected;
        public int Boundary { get; } = boundary;
        public int Branches { get; } = branches;
        public int Unsettled { get; } = unsettled;
        public int[][] Circuits { get; } = circuits;
        public int? MotifContained { get; set; }
        public int? MotifStraddling { get; set; }
        public int? Refinements { get; set; }
    }

    private const string Head = """
<!doctype html><html lang=en><meta charset=utf-8><meta name=viewport content='width=device-width,initial-scale=1'><title>Reading a field without its recipe</title><style>
*{box-sizing:border-box}body{margin:0;background:#e9e7dd;color:#2b413d;font:16px/1.55 system-ui,sans-serif}header,main{max-width:1500px;margin:auto;padding:30px}header{padding-top:55px}h1{font:clamp(38px,5vw,68px)/1.07 Georgia,serif;max-width:1000px;margin:18px 0 26px}h2{font:32px/1.2 Georgia,serif;margin:12px 0}h3{font:19px/1.3 Georgia,serif;margin:0 0 8px}p{max-width:1000px}.eyebrow{font-size:11px;letter-spacing:.15em;font-weight:700;color:#83775a}a{color:#81556e}nav{display:flex;flex-wrap:wrap;gap:8px 20px;margin:24px 0}.legend{display:flex;gap:20px;flex-wrap:wrap;font-size:14px}.legend span:before{content:'●';margin-right:7px}.motif-key:before{color:#a04367}.basis-key:before{color:#2d8177}.residue-key:before{color:#cb923e}article{border:1px solid #ccc7b7;border-radius:5px;background:#faf7ee;margin:0 0 38px;overflow:hidden;scroll-margin-top:20px}.copy{padding:24px 28px}.copy p{margin:10px 0}.search{font-size:13px;color:#756144}.panels{display:grid;grid-template-columns:1fr 1fr;border-block:1px solid #d8d2c2}.panels section+section{border-left:1px solid #d8d2c2}.controls{min-height:110px;display:flex;flex-wrap:wrap;align-content:center;gap:9px 18px;padding:16px 20px;background:#f1eddf;border-bottom:1px solid #ddd6c6}.controls label{font-size:13px;display:flex;gap:7px;align-items:center;max-width:100%}select{max-width:100%;font:13px system-ui;background:#fbf8ef;color:#2b413d;border:1px solid #bfb6a1;border-radius:4px;padding:7px}input{accent-color:#8f4564}input[type=range]{width:100px}.drawing svg{display:block;width:100%;height:auto}.read-copy{padding:8px 24px 22px;min-height:170px}.read-copy p{font-size:13px;margin:7px 0}.prototype{float:right;width:125px;margin-top:-6px}.prototype svg{width:100%;height:80px;background:#f0ebdd;border-radius:4px}details{padding:18px 28px}summary{font-size:14px;cursor:pointer}.table-wrap{overflow:auto;margin-top:15px}table{font-size:12px;white-space:nowrap;border-collapse:collapse;width:100%}td,th{padding:8px 12px;text-align:left;border-bottom:1px solid #ddd5c2}.small{font-size:13px;color:#746d58}.null{padding:90px 30px;color:#8e734d}.pattern-edge{stroke-linecap:round}.reading-label{font:12px system-ui;fill:#78725e}@media(max-width:850px){header,main{padding:18px}.panels{grid-template-columns:1fr}.panels section+section{border-left:0;border-top:1px solid #d8d2c2}.controls{min-height:95px}.copy{padding:20px}h2{font-size:28px}select{max-width:260px}.read-copy{min-height:140px}.prototype{margin-top:0}}
</style><header><span class=eyebrow>GOJŌ · HISTORY-BLIND QUALITATIVE RECOGNITION</span><h1>Reading a field<br>without its recipe</h1><p>The observer receives only fresh sites, connected segments, coarse headings and explicit join / crossing witnesses. Construction faces, names, positions, extents and source history are withheld. A local reader induces motifs; another reader tests larger flow and arrangement questions.</p><p>Several readings can be true at once. A two-channel grid can explain part of a triangular field, leaving an additional direction and its circuits visible. An alternating flow can explain staggered rows more precisely than a constant direction. No single noun is chosen as the answer.</p><div class=legend><span class=motif-key>Local motif / selected match</span><span class=basis-key>Larger channel support</span><span class=residue-key>Unexplained segments</span></div><nav><a href='#R01'>R01 Square</a><a href='#R02'>R02 Shear</a><a href='#R03'>R03 Diamond</a><a href='#R04'>R04 Triangular</a><a href='#R05'>R05 Offset</a><a href='#R06'>R06 Zigzag</a><a href='#R07'>R07 Hexagonal</a><a href='#R08'>R08 Grid + motifs</a><a href='#R09'>R09 Mixed</a><a href='#R10'>R10 Search limit</a></nav><p class=small>The diagrams retain an external geometric interpretation for people to inspect. That geometry is not available to the recognizers. These are bounded structural induction experiments; exact shapes, empty faces and arbitrary spatial layouts are outside this observation contract. No perturbations are introduced in this round.</p><p><a href='../../../../Gojo/docs/GojoQualitativeRecognition.md'>Mechanics, findings and limits</a> · <a href='diagnostics.json'>Diagnostic evidence counts</a> · <a href='../ornament-perturbations/gallery.html'>Earlier supplied-candidate experiment</a></p></header><main>
""";

    private const string Tail = """
<script>
const fields=JSON.parse(document.getElementById('native-data').textContent),palette=['#287c72','#7750a3','#3a70a0','#b26550'];
function svg(field,styles,focus=null,small=false){const nodes=field.Nodes,active=focus?.length?focus.flatMap(id=>[field.Edges[id].From,field.Edges[id].To]):nodes.map((_,i)=>i),p=active.map(id=>nodes[id]);let minX=Math.min(...p.map(v=>v[0])),maxX=Math.max(...p.map(v=>v[0])),minY=Math.min(...p.map(v=>v[1])),maxY=Math.max(...p.map(v=>v[1]));const w=800,h=500,pad=small?30:55,scale=Math.min((w-2*pad)/Math.max(1,maxX-minX),(h-2*pad)/Math.max(1,maxY-minY));const at=id=>[w/2+(nodes[id][0]-(minX+maxX)/2)*scale,h/2-(nodes[id][1]-(minY+maxY)/2)*scale];let s=`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${w} ${h}" role="img" aria-label="${field.Id} native recognition read"><rect width="800" height="500" fill="#faf7ee"/>`;field.Edges.forEach((edge,i)=>{if(focus&&!focus.includes(i))return;const a=at(edge.From),b=at(edge.To),st=styles[i]||{c:'#c8c8b9',w:1.7};s+=`<path class="pattern-edge" data-edge="${i}" d="M ${a} L ${b}" fill="none" stroke="${st.c}" stroke-width="${st.w}" ${st.d?'stroke-dasharray="6 5"':''}/>`;});if(!small){for(const pair of field.Crossings){if(focus&&!pair.every(id=>focus.includes(id)))continue;const first=field.Edges[pair[0]],second=field.Edges[pair[1]],a=at(first.From),b=at(first.To),c=at(second.From),d=at(second.To),den=(a[0]-b[0])*(c[1]-d[1])-(a[1]-b[1])*(c[0]-d[0]);if(Math.abs(den)<.001)continue;const t=((a[0]-c[0])*(c[1]-d[1])-(a[1]-c[1])*(c[0]-d[0]))/den,x=a[0]+t*(b[0]-a[0]),y=a[1]+t*(b[1]-a[1]),len=Math.hypot(b[0]-a[0],b[1]-a[1]),dx=(b[0]-a[0])/len*6,dy=(b[1]-a[1])/len*6;s+=`<path d="M ${x-dx},${y-dy} L ${x+dx},${y+dy}" stroke="#faf7ee" stroke-width="8"/><path d="M ${x-dx},${y-dy} L ${x+dx},${y+dy}" stroke="${styles[pair[0]]?.c||'#a9b1a6'}" stroke-width="3"/>`;}}const degrees=nodes.map(()=>0);field.Edges.forEach(edge=>{degrees[edge.From]++;degrees[edge.To]++;});active.filter((id,i,all)=>all.indexOf(id)===i).forEach(id=>{const q=at(id);if(!small)s+=`<circle data-site="${id}" cx="${q[0]}" cy="${q[1]}" r="${degrees[id]>=3?2.8:1.9}" fill="#50695e"><title>Actual shared site · ${degrees[id]} incident segments</title></circle>`;});return s+'</svg>';}
for(const card of document.querySelectorAll('article[data-case]')){const field=fields[Number(card.dataset.case)],motif=card.querySelector('.motif'),disposition=card.querySelector('.disposition'),occ=card.querySelector('.occurrence'),all=card.querySelector('.all'),circuits=card.querySelector('.circuits');field.Motifs.forEach((m,i)=>motif.add(new Option(m.Name,i)));field.Readings.forEach((r,i)=>disposition.add(new Option(r.Key+' · '+r.Name+' · '+r.Selected.length+'/'+field.Edges.length,i)));function local(){const m=field.Motifs[Number(motif.value)];if(!m){card.querySelector('.motif-drawing').innerHTML=svg(field,{});return;}occ.max=m.Occurrences.length-1;if(Number(occ.value)>Number(occ.max))occ.value=0;const chosen=m.Occurrences[Number(occ.value)],styles={};if(all.checked)for(const edges of m.Occurrences)for(const id of edges)styles[id]={c:'#ba849e',w:3.5};for(const id of chosen)styles[id]={c:'#963459',w:5};card.querySelector('.motif-drawing').innerHTML=svg(field,styles);card.querySelector('.motif-name').textContent=m.Name;card.querySelector('.motif-stats').textContent='Match '+(Number(occ.value)+1)+' of '+m.Occurrences.length+'. Same connected role graph and heading word; cyclic start and reversed traversal are normalized. Rotational variants remain separate. Overlap is allowed.';card.querySelector('.prototype').innerHTML=svg(field,styles,chosen,true);card.dataset.motif=motif.value;}
function larger(){const r=field.Readings[Number(disposition.value)];if(!r){card.querySelector('.disposition-drawing').innerHTML=svg(field,{});card.querySelector('.disposition-name').textContent='No retained larger reading';return;}const styles={};for(const id of r.Residue)styles[id]={c:'#ca923f',w:2.7,d:true};for(const id of r.Selected)styles[id]={c:'#298174',w:3};r.Channels.forEach((edges,k)=>{for(const id of edges)styles[id]={c:palette[k%palette.length],w:3};});if(circuits.checked)r.Circuits.forEach((edges,k)=>{for(const id of edges)styles[id]={c:k%2?'#8a5b92':'#298174',w:4.7};});card.querySelector('.disposition-drawing').innerHTML=svg(field,styles);card.querySelector('.disposition-name').textContent=r.Key+' · '+r.Name;card.querySelector('.disposition-stats').textContent=r.Selected.length+' / '+field.Edges.length+' segments participate · '+r.Residue.length+' remainder. '+r.Commutations+' commuting circuits · '+r.Triangles+' triangular circuits · '+r.Unexpected+' triangles cross this basis. '+r.Boundary+' boundary obligations · '+r.Branches+' branches · '+r.Unsettled+' endpoint mismatches.';card.querySelector('.related').textContent=r.MotifContained===null?'Local motif partitions are linked to basis candidates in this round. This standalone flow retains its own routes and boundaries.':r.MotifContained+' local occurrences fit entirely within this support; '+r.MotifStraddling+' bridge into its remainder. '+r.Refinements+' other readings explain a strict support superset without endpoint mismatches. These extend selected support; they do not establish whole-grammar entailment or a winner.';card.dataset.reading=disposition.value;}
motif.onchange=()=>{occ.value=0;local();};occ.oninput=local;all.onchange=local;disposition.onchange=larger;circuits.onchange=larger;local();larger();}
</script></html>
""";
}
