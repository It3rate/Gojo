using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Measured presentation only. Screen positions, colors and fill do
/// not feed the native interior or feature readers.</summary>
public static class NativeInteriorGallery
{
    public sealed record Study(NativeInteriorTopology Topology, NativeInteriorFeatures Features, NativeInteriorCompletion Completion);

    public static void Write(string output, IReadOnlyList<Study> studies)
    {
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var sources = new List<object>(); var total = 0; var conditional = 0;
        foreach (var study in studies)
        {
            var topology = study.Topology; var id = topology.Source.Id;
            var sourceImage = $"patterns/{id}-source.svg"; var mapImage = $"patterns/{id}-interiors.svg";
            var entries = new List<(string Id, NativeInteriorTopology Topology, NativeInteriorFeatures.BoundaryRead Read, Relation? Bridge, bool Conditional)>();
            for (var i = 0; i < study.Features.Reads.Count; i++) entries.Add(($"F{i + 1:000}", topology, study.Features.Reads[i], null, false));
            for (var p = 0; p < study.Completion.Proposals.Count; p++)
            {
                var proposal = study.Completion.Proposals[p];
                proposal.Topology.Publication.TryMapStroke(proposal.Bridge, out var bridge);
                for (var i = 0; i < proposal.Reads.Count; i++) entries.Add(($"H{p + 1:00}-F{i + 1:000}", proposal.Topology, proposal.Reads[i], bridge, true));
            }
            File.WriteAllText(Path.Combine(output, sourceImage), Svg(topology, [], null, null, "Published source passages"));
            File.WriteAllText(Path.Combine(output, mapImage), Svg(topology, study.Features.Reads, null, null, "Discovered bounded walks"));
            var boundaries = new List<object>();
            foreach (var entry in entries)
            {
                var image = $"patterns/{id}-{entry.Id}.svg";
                File.WriteAllText(Path.Combine(output, image), Svg(entry.Topology, [entry.Read], entry.Read, entry.Bridge,
                    entry.Conditional ? "One-gap completion hypothesis" : "Discovered boundary read"));
                var r = entry.Read;
                boundaries.Add(new { entry.Id, Image = image, entry.Conditional, r.SideCount,
                    ObservedSegments = r.Boundary.Darts.Count,
                    Obstructions = E(r.Boundary.InteriorObstructions).Length, UnknownInterior = E(r.Boundary.InteriorUnknown).Length,
                    Claims = r.Claims.Select(claim => new { claim.Name, claim.Status, Answer = claim.Evidence.Answer,
                        Support = E(claim.Evidence.Supported).Length, Refutation = E(claim.Evidence.Refuted).Length,
                        Open = E(claim.Evidence.Open).Length, Assumptions = E(claim.Assumptions).Length,
                        Missing = E(claim.Missing).Length }).ToArray() });
                total++; if (entry.Conditional) conditional++;
            }
            var supported = study.Features.Reads.Where(read => read.Find("Elementary").Status == "Supported")
                .SelectMany(read => read.Claims).Where(claim => claim.Status == "Supported")
                .GroupBy(claim => claim.Name).ToDictionary(group => group.Key, group => group.Count());
            sources.Add(new { id, topology.Source.Name, topology.Source.Note, SourceImage = sourceImage, MapImage = mapImage,
                SourceStrokes = E(topology.Source.Graph.Strokes).Length, ObservationEdges = topology.Edges.Count,
                ObservationNodes = topology.Nodes.Count, ObservedBoundaries = topology.Boundaries.Count,
                ConditionalBoundaries = entries.Count(entry => entry.Conditional),
                UnknownEmbedding = E(topology.EmbeddingUnknown).Length, OpenWalks = E(topology.OpenWalks).Length,
                Unsearched = E(topology.Unsearched).Length, ClosureCuts = E(study.Completion.Unsearched).Length,
                Proposals = study.Completion.Proposals.Count, Unsupported = E(topology.Unresolved).Length,
                SupportedFeatures = supported, Boundaries = boundaries });
        }
        var data = JsonSerializer.Serialize(new { Schema = "native-interiors-v1", Sources = sources,
            Counts = new { Sources = studies.Count, Boundaries = total, Conditional = conditional } },
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), data);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.NATIVE_INTERIORS=" + data + ";\n");
        File.WriteAllText(Path.Combine(output, "gallery.html"), Page(false));
        File.WriteAllText(Path.Combine(output, "overview.html"), Page(true));
    }

    private static string Svg(NativeInteriorTopology topology, IReadOnlyList<NativeInteriorFeatures.BoundaryRead> regions,
        NativeInteriorFeatures.BoundaryRead? selected, Relation? bridge, string title)
    {
        var frame = topology.Source.Graph.Frame;
        var quarterTurn = topology.GeometryBasis.Contour.TryReadEqualTurns(topology.GeometryBasis.Contour.Evaluate(), out _);
        // One explicit display of each unmeasured reach. A selected oblique
        // display helps expose hexagonal structure; it is not recognition input.
        var points = new Dictionary<NativeInteriorTopology.Node, (double X, double Y)>();
        foreach (var node in topology.Nodes)
        {
            if (node.Word is null) continue;
            double x = 0, y = 0; var known = true;
            foreach (var term in E(node.Word))
            {
                if (term is not Relation move || !frame.Direction(move, out var axis, out var forward, out _)) { known = false; break; }
                var step = forward ? 1 : -1;
                if (ReferenceEquals(axis, frame.Horizontal)) x += step; else y += step;
            }
            if (known) points[node] = quarterTurn ? (x, y) : (x + .5 * y, Math.Sqrt(3) / 2 * y);
        }
        var focus = selected?.Boundary.Darts.Select(dart => dart.From).Where(points.ContainsKey).Select(node => points[node]).ToArray();
        var extent = focus is { Length: >= 3 } ? focus : points.Values.ToArray();
        var minX = extent.Length == 0 ? 0 : extent.Min(p => p.X); var maxX = extent.Length == 0 ? 1 : extent.Max(p => p.X);
        var minY = extent.Length == 0 ? 0 : extent.Min(p => p.Y); var maxY = extent.Length == 0 ? 1 : extent.Max(p => p.Y);
        var scale = Math.Min(690 / Math.Max(1, maxX - minX), 410 / Math.Max(1, maxY - minY)) * (selected is null ? .94 : .76);
        (double X, double Y) Screen((double X, double Y) p) => (400 + (p.X - (minX + maxX) / 2) * scale, 255 - (p.Y - (minY + maxY) / 2) * scale);
        string Point(NativeInteriorTopology.Node node) { var p = Screen(points[node]); return F(p.X) + "," + F(p.Y); }
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 800 530' role='img'><title>{H(title)}</title><defs><clipPath id='patch'><rect x='12' y='18' width='776' height='480'/></clipPath></defs><rect width='800' height='530' fill='#faf8f0'/><g clip-path='url(#patch)' stroke-linejoin='round' stroke-linecap='round'>");
        foreach (var read in regions)
        {
            if (!read.Boundary.Darts.All(dart => points.ContainsKey(dart.From))) continue;
            var elementary = read.Find("Elementary").Status;
            var fill = elementary == "Refuted" ? "none" : Color(read);
            var alpha = elementary == "Open" ? ".13" : ".42";
            svg.Append($"<polygon class='interior-region' points='{string.Join(' ', read.Boundary.Darts.Select(dart => Point(dart.From)))}' fill='{fill}' fill-opacity='{alpha}' stroke='none'/>");
        }
        foreach (var edge in topology.Edges)
        {
            if (!points.ContainsKey(edge.From) || !points.ContainsKey(edge.To)) continue;
            var assumed = bridge is not null && E(edge.SourceUses).OfType<Relation>().Any(use => ReferenceEquals(use.Left, bridge));
            svg.Append($"<polyline class='{(assumed ? "assumed-edge" : "observed-edge")}' points='{Point(edge.From)} {Point(edge.To)}' fill='none' stroke='{(assumed ? "#a87538" : selected is null ? "#425f66" : "#bec9c4")}' stroke-width='{(assumed ? "3.2" : "1.8")}'{(assumed ? " stroke-dasharray='8 6'" : "")}/>");
        }
        if (selected is not null)
            foreach (var dart in selected.Boundary.Darts)
            {
                if (!points.ContainsKey(dart.From) || !points.ContainsKey(dart.To)) continue;
                var assumed = bridge is not null && E(dart.Edge.SourceUses).OfType<Relation>().Any(use => ReferenceEquals(use.Left, bridge));
                svg.Append($"<polyline class='selected-boundary{(assumed ? " assumed-edge" : "")}' points='{Point(dart.From)} {Point(dart.To)}' fill='none' stroke='{(assumed ? "#a87538" : "#246b77")}' stroke-width='4'{(assumed ? " stroke-dasharray='8 6'" : "")}/>");
            }
        svg.Append($"</g><text x='20' y='516' font-family='system-ui,sans-serif' font-size='12' fill='#6e7d7b'>{H(title)} · chosen display of uncalibrated words</text></svg>");
        return svg.ToString();
    }

    private static string Color(NativeInteriorFeatures.BoundaryRead read)
    {
        bool Has(string name) => read.Find(name).Status is "Supported" or "Possible";
        return Has("Star-like") ? "#b87988" : Has("Contains notch") ? "#b79b60" : Has("Triangle") ? "#d5a168" :
            Has("Hexagon") ? "#9d91c2" : Has("Square") ? "#6ba6a9" : Has("Rectangle") ? "#a4b985" : "#a39b83";
    }

    private static string Page(bool overview) => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · native interior discovery</title><style>
""" + Css + """
</style></head><body><main><div class="eyebrow">Gojo · discovery from native lattice passages</div><h1>Reading the shapes between the lines</h1><p class="intro">Discover local enclosures, then inspect the evidence for their side structure, corners, notches and zigzags. A proposed closing line stays an assumption. No measured geometry or image pixels feed the reader.</p><nav><a href="gallery.html">Inspect discoveries</a><a href="overview.html">All sources</a><a href="diagnostics.json">Read diagnostics</a><a href="../bourgoin-patterns/gallery.html">Original lattice repertoire</a></nav>
""" + (overview ? "<div id='atlas' class='atlas'></div>" : """
<div class="controls"><label>Published lattice<select id="source"></select></label><label>Discovered / proposed enclosure<select id="boundary"></select></label></div><p id="source-note" class="note"></p><div class="panels"><section><h2>Whole observation</h2><img id="source-image" alt="Published native lattice passages"><p id="source-counts" class="muted"></p></section><section><h2>Local interior candidates</h2><img id="map-image" alt="Discovered inner boundary candidates"><p id="map-note" class="muted"></p></section><section><h2 id="selected-title">Selected boundary</h2><img id="selected-image" alt="Selected discovered or conditional boundary"><div id="empty" class="empty" hidden>No bounded walk is established by this read. Unsupported encounters and open fragments remain visible.</div><p id="selected-note" class="muted"></p></section></div><section id="features"><h2>Several readings of the same boundary</h2><p>Supported means witnessed under the offered native model. Conditional means a claim also depends on the dashed closing passage. Unresolved keeps the missing condition visible; contradicted concerns this candidate and question.</p><div id="claims" class="claims"></div><details><summary>Contradicted candidate descriptions</summary><div id="refuted" class="claims"></div></details></section><section><h2>All discovered boundaries in this source</h2><div id="boundary-list" class="boundary-list"></div></section><section class="remainder"><h2>What this read has not settled</h2><div id="remainders"></div></section>
""") + """
<section class="scope"><h2>Read scope</h2><p>Smallest means a local boundary with no established divider, rather than the shortest measured perimeter. A known divider produces smaller regions. Uncertain crossings or nested islands can leave the elementary-interior question open.</p><p>Side counts and cyclic order are structural. Right angles require an explicit native turn model; regularity is not inferred from a pleasing drawing. Colors separate feature families and do not mean yes/no. Geometry for the SVG is a chosen display only.</p><p>Paired inset / notch is a specific inward-return signature, not every concavity. The star candidate is distributed alternating-turn evidence; a common radial center, equal rays and rotational symmetry are separate questions.</p><p>Source passages are not mechanically joined by this observation. A boundary may be closed observationally while its source strands remain independent. There are no calibrated probability estimates in this study.</p></section><footer id="run-note"></footer></main><script src="data.js"></script><script>
""" + Script + "</script></body></html>";

    private const string Css = """
[hidden]{display:none!important}*{box-sizing:border-box}body{margin:0;background:#f2f1e9;color:#2c4d54;font:15px/1.55 system-ui,sans-serif}main{max-width:1640px;padding:30px;margin:auto}h1,h2,h3{font-family:Georgia,serif;font-weight:500;line-height:1.2}h1{font-size:45px;margin:12px 0}h2{font-size:25px;margin:10px 0 18px}.intro{font-size:18px;max-width:1080px}.eyebrow{font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#7b8271}nav{display:flex;gap:22px;flex-wrap:wrap;margin:24px 0}a{color:#397480;text-underline-offset:3px}.controls{display:grid;grid-template-columns:1.4fr 1fr;gap:20px;padding:17px;background:#e5ebe2;border:1px solid #c9d1c6}label{display:grid;gap:8px;font-size:13px;font-weight:650}select,button{min-width:0;max-width:100%;font:inherit;color:inherit;padding:10px;border:1px solid #b2c0b8;background:#fffdf7;border-radius:4px}.note{padding:15px;border-left:3px solid #879d87;background:#fcfbf3}.panels{display:grid;grid-template-columns:1fr 1fr 1fr;gap:18px}section,.card{min-width:0}section{margin:24px 0}.panels section{margin:0;background:#fffdf7;padding:15px;border:1px solid #cdd4c9}.panels img{display:block;width:100%;height:285px;object-fit:contain}.muted{font-size:13px;color:#6a7e7e}.claims{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}.claim{padding:14px;background:#fffdf7;border:1px solid #c7d1c8;border-top:3px solid #789c9c}.claim.open{border-top-color:#c1ad7f;background:#f6f1e4}.claim.possible{border-top-color:#b38b52;background:#f8f0df}.claim.refuted{border-top-color:#a6aaa1;background:#eeeee8}.claim strong{display:block;font-size:15px}.claim p{font-size:12px;margin:8px 0 0}.status{font-size:12px;text-transform:uppercase;letter-spacing:.04em;color:#6d7a72}.boundary-list{display:flex;gap:9px;flex-wrap:wrap}.boundary-list button{cursor:pointer;font-size:12px;text-align:left}.boundary-list [aria-selected=true]{outline:2px solid #407b84;background:#e2eeea}.remainder,.scope{padding:20px;background:#e4e9df}.remainder p,.scope p{font-size:13px}.empty{padding:35px 20px;border:1px dashed #b6bda8;background:#f7f4e9}.atlas{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px}.card{background:#fffdf7;border:1px solid #cbd4c8;padding:18px}.card h2{font-size:23px}.card img{width:100%;height:235px;object-fit:contain}.card p{font-size:13px}.tag{display:inline-block;font-size:11px;padding:4px 7px;background:#e8ede1;border:1px solid #c8d2c3;margin:3px}.scope{margin-top:30px}details{margin:20px 0;font-size:13px}summary{cursor:pointer}footer{font-size:12px;color:#758272;margin:25px 0}button:focus-visible,select:focus-visible,a:focus-visible{outline:3px solid #b58a47;outline-offset:3px}@media(max-width:1100px){.claims{grid-template-columns:repeat(3,minmax(0,1fr))}.panels{grid-template-columns:1fr 1fr}.panels section:last-child{grid-column:1/-1}.atlas{grid-template-columns:1fr 1fr}}@media(max-width:650px){main{padding:16px}h1{font-size:32px}.controls,.panels,.atlas{grid-template-columns:1fr}.panels section:last-child{grid-column:auto}.claims{grid-template-columns:1fr 1fr}.panels img{height:245px}nav{gap:13px}.claim{padding:11px}}
""";

    private const string Script = """
(()=>{const d=globalThis.NATIVE_INTERIORS,$=id=>document.getElementById(id),esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));const featureLabel=name=>({'Star-like':'Distributed alternating turns · star candidate','Contains notch':'Paired inset / notch'}[name]||name);const textStatus=s=>({Supported:'Supported',Possible:'Conditional',Open:'Unresolved',Refuted:'Contradicted'})[s];$('run-note').textContent=d.counts.sources+' native sources · '+d.counts.boundaries+' boundary readings · '+d.counts.conditional+' depend on a proposed closing passage.';const atlas=$('atlas');if(atlas){atlas.innerHTML=d.sources.map(s=>'<article class="card" data-source="'+esc(s.id)+'"><div class="eyebrow">'+esc(s.id)+'</div><h2>'+esc(s.name)+'</h2><a href="gallery.html?source='+s.id+'"><img loading="lazy" src="'+s.mapImage+'" alt="'+esc(s.name)+' discoveries"></a><p>'+s.observedBoundaries+' observed bounded walks · '+s.conditionalBoundaries+' conditional.</p><div>'+Object.entries(s.supportedFeatures).filter(([k])=>!['Closed','Elementary','Equal-sided'].includes(k)).map(([k,v])=>'<span class="tag">'+esc(featureLabel(k))+' '+v+'</span>').join('')+'</div><p class="muted">'+s.unknownEmbedding+' unresolved embedding witness(es); '+s.unsearched+' boundary cuts.</p></article>').join('');return}const choice=(el,items,label)=>{el.innerHTML=items.map(x=>'<option value="'+esc(x.id)+'">'+esc(label(x))+'</option>').join('')};choice($('source'),d.sources,s=>s.id+' · '+s.name);const params=new URLSearchParams(location.search);if(d.sources.some(s=>s.id===params.get('source')))$('source').value=params.get('source');const current=()=>d.sources.find(s=>s.id===$('source').value);function options(){choice($('boundary'),current().boundaries,b=>b.id+(b.conditional?' · one-gap hypothesis':' · observed')+' · '+b.sideCount+' retained runs')}options();if(current().boundaries.some(b=>b.id===params.get('boundary')))$('boundary').value=params.get('boundary');function claim(c){return '<article class="claim '+c.status.toLowerCase()+'" data-claim="'+esc(c.name)+'"><span class="status">'+textStatus(c.status)+'</span><strong>'+esc(featureLabel(c.name))+'</strong><p>'+c.support+' supporting witness(es) · '+c.missing+' missing condition(s)'+(c.assumptions?' · '+c.assumptions+' assumed passage(s)':'')+'</p></article>'}function render(){const s=current(),b=s.boundaries.find(b=>b.id===$('boundary').value);$('source-note').textContent=s.note;$('source-image').src=s.sourceImage;$('map-image').src=s.mapImage;$('source-counts').textContent=s.sourceStrokes+' published source passages → '+s.observationEdges+' observational edge pieces, '+s.observationNodes+' read sites. Read sites do not weld source strands.';$('map-note').textContent=s.observedBoundaries+' positive local boundary walk(s). Pale fill can retain an unresolved elementary-interior claim. Larger enclosing cycles are not substituted for smaller regions.';$('selected-image').hidden=!b;$('empty').hidden=!!b;$('features').hidden=!b;if(b){$('selected-image').src=b.image;$('selected-title').textContent=b.id+(b.conditional?' · assumed closure':' · observed boundary');$('selected-note').textContent=b.sideCount+' retained side runs from '+b.observedSegments+' edge pieces. '+(b.conditional?'Dashed amber ink is a proposed passage, not observed source evidence. ':'')+b.obstructions+' known interior obstruction(s); '+b.unknownInterior+' local unresolved interior witness(es).';$('claims').innerHTML=b.claims.filter(c=>c.status!=='Refuted').map(claim).join('');$('refuted').innerHTML=b.claims.filter(c=>c.status==='Refuted').map(claim).join('')}else{$('selected-image').removeAttribute('src');$('selected-note').textContent=s.proposals?'A closure was proposed but has not established a new bounded walk.':'No supported one-gap proposal is available in the current bounded read.';$('claims').innerHTML='';$('refuted').innerHTML=''}$('boundary-list').innerHTML=s.boundaries.map(x=>'<button data-boundary="'+x.id+'" aria-selected="'+(b?.id===x.id)+'">'+esc(x.id)+(x.conditional?' · conditional':'')+'<br>'+x.claims.filter(c=>['Triangle','Square','Rectangle','Hexagon','Contains notch','Contains zigzag','Star-like'].includes(c.name)&&['Supported','Possible'].includes(c.status)).map(c=>esc(featureLabel(c.name))).join(' · ')+'</button>').join('')||'<p class="muted">No positive bounded walk was found under the current read.</p>';$('boundary-list').querySelectorAll('button').forEach(button=>button.onclick=()=>{$('boundary').value=button.dataset.boundary;render()});$('remainders').innerHTML='<p>'+s.unknownEmbedding+' unresolved embedding witness(es), '+s.openWalks+' open walk fragment(s), '+s.unsearched+' unsearched boundary walk(s).</p><p>'+s.proposals+' one-gap proposal(s); '+s.closureCuts+' closure proposal(s) left unsearched. A known open fragment is different from an uncertain crossing.</p>';document.title='Gojo · '+s.id+(b?' '+b.id:'')+' · interior discovery';const p=new URLSearchParams({source:s.id});if(b)p.set('boundary',b.id);history.replaceState(null,'','?'+p.toString())}$('source').onchange=()=>{options();render()};$('boundary').onchange=render;render()})();
""";
    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static string H(string value) => WebUtility.HtmlEncode(value);
}
