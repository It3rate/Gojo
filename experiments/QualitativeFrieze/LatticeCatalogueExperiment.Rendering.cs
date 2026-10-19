using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class LatticeCatalogueExperiment
{
    private sealed record Ink(IElement From, IElement To, double X1, double Y1, double X2, double Y2);
    private static string H(string text) => WebUtility.HtmlEncode(text);
    private static string F(double number) => number.ToString("0.###", CultureInfo.InvariantCulture);
    private static (double X, double Y) Position(QualitativeExecution.QualitativeMotionFrame frame, Relation word,
        BourgoinPatternLibrary.DisplayBasis basis)
    {
        double x = 0, y = 0;
        foreach (var term in N.E(word).Cast<Relation>())
        {
            if (!frame.Direction(term, out var axis, out var forward, out _))
                throw new InvalidOperationException("Only actual published motion words can supply a displayed point.");
            var sign = forward ? 1 : -1;
            if (ReferenceEquals(axis, frame.Horizontal)) { x += sign * basis.RX; y += sign * basis.RY; }
            else { x += sign * basis.UX; y += sign * basis.UY; }
        }
        return (x, y);
    }
    private static string FieldSvg(string title, OrnamentProgramField field, BourgoinPatternLibrary.DisplayBasis basis)
    {
        var points = new Dictionary<IElement, (double X, double Y)>(N.Participants.Instance);
        foreach (var binding in N.E(field.Bindings).Cast<Relation>())
        {
            var point = Position(field.Graph.Frame, (Relation)binding.Right, basis);
            if (points.TryGetValue(binding.Left, out var prior) &&
                (Math.Abs(prior.X - point.X) > 1e-9 || Math.Abs(prior.Y - point.Y) > 1e-9))
                throw new InvalidOperationException("An actual shared site cannot have conflicting displayed bindings.");
            points[binding.Left] = point;
        }
        var inks = N.E(field.Graph.Strokes).Cast<Relation>().Select(stroke =>
        {
            if (!QualitativeStrokeGraph.TryReadStroke(stroke, out _, out var from, out var to, out _))
                throw new InvalidOperationException("Every displayed edge must be an actual current stroke.");
            var a = points[from]; var b = points[to]; return new Ink(from, to, a.X, a.Y, b.X, b.Y);
        }).ToArray();
        var bounds = Bounds(inks); var scale = Math.Min(660 / Math.Max(1, bounds.Width), 420 / Math.Max(1, bounds.Height));
        (double X, double Y) Screen(double x, double y) => (360 + (x - bounds.X) * scale, 240 - (y - bounds.Y) * scale);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 720 480' role='img' aria-label='{H(title)}'><title>{H(title)}</title><rect width='720' height='480' fill='#F8F5EB'/><g fill='none' stroke='#345961' stroke-width='1.85' stroke-linejoin='round' stroke-linecap='round'>");
        var neighbours = points.Keys.ToDictionary(site => site, _ => new HashSet<IElement>(N.Participants.Instance), N.Participants.Instance);
        foreach (var ink in inks)
        {
            var a = Screen(ink.X1, ink.Y1); var b = Screen(ink.X2, ink.Y2);
            svg.Append($"<path d='M {F(a.X)} {F(a.Y)} L {F(b.X)} {F(b.Y)}'/>");
            neighbours[ink.From].Add(ink.To); neighbours[ink.To].Add(ink.From);
        }
        svg.Append("</g><g fill='#AB793D'>");
        foreach (var site in neighbours.Where(pair => pair.Value.Count >= 3).Select(pair => pair.Key))
        { var point = Screen(points[site].X, points[site].Y); svg.Append($"<circle cx='{F(point.X)}' cy='{F(point.Y)}' r='2.2'/>"); }
        return svg.Append("</g></svg>").ToString();
    }
    private static (double X, double Y, double Width, double Height) Bounds(IEnumerable<Ink> inks)
    {
        var points = inks.SelectMany(ink => new[] { (X: ink.X1, Y: ink.Y1), (X: ink.X2, Y: ink.Y2) }).ToArray();
        if (points.Length == 0) return (0, 0, 1, 1);
        var minX = points.Min(point => point.X); var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y); var maxY = points.Max(point => point.Y);
        return ((minX + maxX) / 2, (minY + maxY) / 2, maxX - minX, maxY - minY);
    }
    private static string ChangeSvg(string title, LatticeCatalogueStepRead read, BourgoinPatternLibrary.DisplayBasis basis)
    {
        List<Ink> Path(UnitArrangementProgram program, Relation path)
        {
            var result = new List<Ink>(); var at = Position(program.Frame, (Relation)path.Left, basis);
            foreach (var word in N.E((Relation)path.Right).Cast<Relation>())
            { var step = Position(program.Frame, word, basis); var next = (X: at.X + step.X, Y: at.Y + step.Y); result.Add(new(path, path, at.X, at.Y, next.X, next.Y)); at = next; }
            return result;
        }
        var source = read.Source;
        var old = source is null ? [] : N.E(source.Paths).Cast<Relation>().SelectMany(path => Path(source, path)).ToArray();
        var current = N.E(read.Result.Paths).Cast<Relation>().SelectMany(path => Path(read.Result, path)).ToArray();
        var bounds = Bounds(old.Concat(current)); var scale = Math.Min(280 / Math.Max(1, bounds.Width), 194 / Math.Max(1, bounds.Height));
        var selected = N.E(read.SelectedPaths).ToHashSet(N.Participants.Instance);
        var introduced = N.E(read.IntroducedPaths).ToHashSet(N.Participants.Instance);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 1080 280' role='img' aria-label='{H(title + ": source, result and change correspondence")}'><title>{H(title)} · actual selected and carried paths</title><rect width='1080' height='280' fill='#F8F5EB'/>");
        foreach (var (index, label) in new[] { (0, "Source / selected focus"), (1, "Result / retained and new"), (2, "Overlay / correspondence") })
        {
            svg.Append($"<text x='{index * 360 + 18}' y='24' font-size='15' font-family='sans-serif' fill='#455B59'>{label}</text>");
            if (index != 1) foreach (var ink in old)
                Draw(ink, index, selected.Contains(ink.From) ? "#B86449" : "#AEB6AA", selected.Contains(ink.From) ? "5 4" : null, index == 2 ? .55 : 1);
            if (index != 0) foreach (var ink in current)
                Draw(ink, index, introduced.Contains(ink.From) ? "#247C80" : "#8C998C", null, 1);
        }
        return svg.Append("</svg>").ToString();
        void Draw(Ink ink, int panel, string colour, string? dash, double opacity)
        {
            double X(double value) => panel * 360 + 180 + (value - bounds.X) * scale;
            double Y(double value) => 150 - (value - bounds.Y) * scale;
            svg.Append($"<path d='M {F(X(ink.X1))} {F(Y(ink.Y1))} L {F(X(ink.X2))} {F(Y(ink.Y2))}' fill='none' stroke='{colour}' stroke-width='2.7' stroke-linejoin='round' opacity='{F(opacity)}'{(dash is null ? "" : " stroke-dasharray='" + dash + "'")}/>");
        }
    }
    private const string Css = """
*{box-sizing:border-box}body{margin:0;background:#ECE9DE;color:#304C50;font:16px/1.6 system-ui,sans-serif}main{max-width:1700px;margin:auto;padding:32px 28px 70px}h1,h2,h3{font:400 31px/1.15 Georgia,serif}h1{font-size:clamp(38px,5vw,66px);max-width:1000px;margin:18px 0}h2{margin:14px 0}h3{font-size:21px;margin:8px 0}p{max-width:1120px}a{color:#276879;text-underline-offset:4px}.eyebrow{font-size:12px;letter-spacing:.16em;text-transform:uppercase;color:#896D42}nav,.toolbar,.filters,.rating{display:flex;flex-wrap:wrap;gap:10px 20px;align-items:center;margin:18px 0}select,input,button,textarea{font:inherit;color:inherit;max-width:100%;background:#FBF8EF;border:1px solid #B8B6A3;border-radius:3px;padding:8px 12px}button{cursor:pointer}button.active{background:#34656A;color:white}.toolbar{padding:16px;background:#F7F3E7;border:1px solid #C9C2AE}label{display:flex;flex-wrap:wrap;align-items:center;gap:8px}.two{display:grid;grid-template-columns:1.15fr 1fr;gap:22px}.panel{min-width:0;background:#F8F5EB;border:1px solid #D0C7B1;padding:18px}.panel img{display:block;width:100%;height:auto}.note{font-size:14px;color:#64736B}.facts{font-size:14px;color:#586F63}.badge{display:inline-block;font-size:12px;padding:2px 8px;background:#DFE8DA;border-radius:3px}.warn{background:#EEE0C8}.change{width:100%;height:auto;background:#F8F5EB}.strips{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.stage{min-width:0;background:#F8F5EB;padding:12px;border:1px solid #D0C7B1}.stage img{width:100%;height:auto;display:block}.stage.active{outline:3px solid #5B827A}.stage p{font-size:13px}.stage h3{font-size:18px}.cards{display:grid;grid-template-columns:repeat(5,1fr);gap:14px}.card{min-width:0;border:1px solid #CFC7B5;padding:11px;background:#F8F5EB;cursor:pointer}.card img{width:100%;height:auto;display:block}.card h3{font-size:17px}.card p{font-size:12px;margin:6px 0}.card.selected{outline:3px solid #51817A}.card[hidden]{display:none}.reading{overflow:auto}table{width:100%;font-size:13px;border-collapse:collapse}td,th{padding:7px 10px;text-align:left;border-bottom:1px solid #D4CDBD;white-space:nowrap}section{margin:36px 0 52px}.legend{display:flex;flex-wrap:wrap;gap:10px 24px;font-size:13px}.old{color:#B86449}.new{color:#247C80}.carried{color:#748572}.review{border-top:1px solid #CFC7B5;margin-top:18px;padding-top:15px}.review textarea{width:100%;min-height:75px}.overview{display:grid;grid-template-columns:repeat(5,1fr);gap:12px}.overview figure{min-width:0;margin:0;background:#F8F5EB;padding:10px}.overview img{width:100%;display:block}.overview figcaption{font:17px Georgia,serif;min-height:45px}.construction{padding:24px 0;border-top:1px solid #C5C0AE}.construction .strips{grid-template-columns:repeat(5,1fr)}@media(max-width:1100px){.cards,.overview{grid-template-columns:repeat(4,1fr)}.strips,.construction .strips{grid-template-columns:repeat(3,1fr)}}@media(max-width:800px){main{padding:22px 14px}.two{grid-template-columns:1fr}.cards,.overview{grid-template-columns:repeat(3,1fr)}.strips,.construction .strips{grid-template-columns:repeat(2,1fr)}}@media(max-width:500px){.cards,.overview{grid-template-columns:repeat(2,1fr)}h2{font-size:27px}.toolbar{padding:12px}}
""";
    private static string Gallery(IReadOnlyList<Plate> plates)
    {
        var options = string.Join("", plates.Select(plate => $"<option value='{plate.Id}'>{H(plate.Id + " · " + plate.Name)}</option>"));
        var families = string.Join("", plates.Select(plate => plate.Family).Distinct().Select(family => $"<option>{H(family)}</option>"));
        var cards = string.Join("", plates.Select(plate => $"<article class='card' id='{plate.Id}' data-case='{plate.Id}' tabindex='0' role='button' aria-label='Inspect {H(plate.Id + " " + plate.Name)}'><span class='eyebrow'>{H(plate.Id + " / " + plate.Family)}</span><h3>{H(plate.Name)}</h3><img src='{plate.Id}.svg' alt='{H(plate.Name)}' loading='lazy'><p>{H(plate.ReviewCue)} · complexity {plate.Complexity}/5</p><span class='badge'>{(plate.Read.IsStructurallyAdmissible ? "Admitted" : "Revise")}</span><span class='user-rating'></span></article>"));
        return "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>One hundred lattices · Gojo</title><style>" + Css + "</style></head><body><main>" + """
<span class='eyebrow'>Gojo / a bounded construction catalogue</span><h1>One hundred lattices.<br>Edits you can inspect.</h1><p>Withdraw a joint, shift a row, regroup lines as a box, replace a focus, or transform a repeated construction. Each result retains the actual earlier structures and explicit replacement evidence. Acceptance checks the chosen structural rules; whether a design feels satisfying remains a separate question.</p><nav><a href='overview.html'>All 100 together</a><a href='overview.svg'>Single sheet · SVG</a><a href='construction-strips.html'>All construction strips</a><a href='../lattice-ladders/gallery.html'>Earlier ladder study</a><a href='diagnostics.json'>Evidence and limits</a></nav><div class='toolbar'><label>Design <select id='design'>
""" + options + """
</select></label><button id='previous'>← Previous</button><button id='next'>Next →</button><label>Rung <input id='rung' type='range' min='0' step='1'><output id='step'></output></label></div><section id='inspection'><div class='two'><article class='panel'><span class='eyebrow' id='current-id'></span><h2 id='title'></h2><img id='field' alt='Executed lattice at the selected construction step'><p id='description'></p><p class='facts' id='stage-facts'></p><p class='note'>Gold dots mark actual shared junctions. Crossing ink without a dot does not imply a material join. These are unfilled structural scaffolds.</p></article><article class='panel'><span class='eyebrow'>Construction / actual native change</span><h2 id='operation'></h2><img class='change' id='change' alt='Selected old paths, retained structure and introduced paths'><div class='legend'><span class='old'>Dashed rust: selected old focus</span><span class='carried'>Grey: carried paths</span><span class='new'>Teal: introduced result</span></div><p class='note'>All three local panels share one display scale. A new closed-box reading can have the same outline as the old independent lines while its publication roles and path structure change.</p><p><a id='packet'>Native construction packet</a></p><div class='review'><span class='eyebrow'>Your question / satisfying as a lattice?</span><div class='rating' id='ratings'><button data-rating='1'>1</button><button data-rating='2'>2</button><button data-rating='3'>3</button><button data-rating='4'>4</button><button data-rating='5'>5</button><button data-rating='0'>Clear</button></div><textarea id='review-note' aria-label='Review note' placeholder='What works? Which rule would improve it?'></textarea><p class='note' id='rating-status'></p></div></article></div><div class='strips' id='stages'></div><article class='panel'><h3>Calculated rule reads · final design</h3><p id='cue'></p><div class='reading' id='readings'></div><p class='note'>Complexity is an authored-structure estimate, not a discovered conceptual vocabulary or beauty score. Crossing strands, disconnected motifs, shared boundary histories and overlapping projected support are disclosed separately. An undeclared contact is not silently accepted as a join.</p></article></section><section><span class='eyebrow'>Twenty construction families / five variations each</span><h2>Compare, choose and question.</h2><div class='filters'><label>Family <select id='family'><option value=''>All families</option>
""" + families + """
</select></label><label>Review <select id='review-filter'><option value=''>All ratings</option><option value='unrated'>Unrated</option><option value='high'>Rated 4–5</option><option value='low'>Rated 1–2</option><option value='crossings'>Crossing strands</option><option value='contacts'>Unjoined contacts</option><option value='overlaps'>Projected overlap</option></select></label><button id='export'>Export ratings</button><button id='import'>Import ratings</button><input id='import-file' type='file' accept='application/json,.json' hidden><span id='visible-count'></span></div><p class='note'>Ratings stay in this browser when local storage is available. Export them to preserve a review for later comparison or training. No rating changes the native generator.</p><div class='cards'>
""" + cards + """
</div></section><section class='panel'><span class='eyebrow'>Counterexamples / table stakes</span><h2>A plausible picture can still fail.</h2><div class='two'><div><img src='X01.svg' alt='Rejected immediate line retrace'><p>X01 · the same path goes out and directly back. <a href='X01-rules.gojo.json.gz'>Native retrace witness</a></p></div><div><img src='X02.svg' alt='Rejected neighbouring-role agreement'><p>X02 · an exposed neighbouring-role offer has the wrong displacement. <a href='X02-rules.gojo.json.gz'>Native blocked witness</a></p></div></div><p>Two independent periodic directions, complete declared seams and no zero or retraced path chords are required here. These finite checks do not establish infinite tiling, occupied-area coverage or universal aesthetic merit. Additional rules can be tested against your ratings.</p></section><script type='application/json' id='data'>
""" + JsonSerializer.Serialize(plates, Options) + """
</script><script>
const cases=JSON.parse(document.getElementById('data').textContent),byId=new Map(cases.map(c=>[c.id,c])),design=document.getElementById('design'),rung=document.getElementById('rung');let current,ratings={},storage=true;const key='gojo-lattice-catalogue-ratings-v1',deepLinked=byId.has(location.hash.slice(1));try{ratings=JSON.parse(localStorage.getItem(key)||'{}');}catch{storage=false;}
function validReview(c){const review=ratings[c.id];return review&&review.fingerprint===c.read.fingerprint?review:null;}
function save(){try{localStorage.setItem(key,JSON.stringify(ratings));}catch{storage=false;}drawRatings();filter();}
function drawRatings(){const review=validReview(current);document.querySelectorAll('[data-rating]').forEach(b=>{b.classList.toggle('active',Number(b.dataset.rating)===(review?.rating||0));b.setAttribute('aria-pressed',Number(b.dataset.rating)===(review?.rating||0));});document.getElementById('review-note').value=review?.note||'';document.getElementById('rating-status').textContent=(review?.rating?'Rated '+review.rating+'/5. ':'Not yet rated. ')+(storage?'Saved locally in this browser.':'Browser storage unavailable; export to preserve this review.');for(const card of document.querySelectorAll('.card')){const c=byId.get(card.dataset.case),r=validReview(c);card.querySelector('.user-rating').textContent=r?.rating?' · Your rating '+r.rating+'/5':'';}}
function show(){const stage=current.stages[Number(rung.value)];document.getElementById('step').textContent=(Number(rung.value)+1)+' / '+current.stages.length;document.getElementById('current-id').textContent=current.id+' / '+current.family;document.getElementById('title').textContent=current.name;document.getElementById('field').src=stage.field;document.getElementById('description').textContent=stage.description;document.getElementById('operation').textContent=stage.name+' · '+stage.kind;document.getElementById('change').src=stage.change;document.getElementById('stage-facts').textContent=stage.selected+' old paths selected · '+stage.carried+' carried · '+stage.introduced+' introduced · '+stage.withdrawnPorts+' publication roles withdrawn';for(const card of document.querySelectorAll('.stage'))card.classList.toggle('active',Number(card.dataset.index)===Number(rung.value));}
function choose(id){current=byId.get(id);design.value=id;rung.max=current.stages.length-1;rung.value=current.stages.length-1;history.replaceState(null,'','#'+id);document.getElementById('packet').href=id+'.gojo.json.gz';document.getElementById('cue').textContent=current.reviewCue+' · structural complexity '+current.complexity+'/5 · '+current.ancestorAnswers+' actual retained ancestor answers.';const r=current.read,rows=[['Native rule gate',r.isStructurallyAdmissible?'Admitted':'Revise'],['Zero chords / immediate retraces / retraced pairs',r.zeroChords+' / '+r.immediateRetraces+' / '+r.retracedPathPairs],['Independent repeat directions',r.independentRepeatDirections],['Blocked / open seam obligations',r.blockedSeamObligations+' / '+r.missingSeamObligations],['Finite frontier obligations',r.finiteFrontierObligations],['Drawing histories / joined support / shared boundary histories',r.sourceStrokes+' / '+r.uniqueJoinedSupports+' / '+r.sharedBoundaryHistories],['Joined branches / unjoined crossings / unjoined contacts',r.joinedBranchSites+' / '+r.unjoinedCrossings+' / '+r.unjoinedContacts],['Projected overlap pairs',r.geometricOverlaps],['Components / maximum degree',r.components+' / '+r.maxDegree],['Motif paths / chords / repeated word terms',r.motifPaths+' / '+r.motifChords+' / '+r.motifWordTerms]];const table=document.createElement('table');for(const row of rows){const tr=document.createElement('tr');for(const text of row){const td=document.createElement('td');td.textContent=text;tr.append(td);}table.append(tr);}document.getElementById('readings').replaceChildren(table);const stages=document.getElementById('stages');stages.replaceChildren();for(const [index,stage] of current.stages.entries()){const card=document.createElement('button');card.className='stage';card.dataset.index=index;const title=document.createElement('h3');title.textContent=(index+1)+'. '+stage.name;const image=document.createElement('img');image.src=stage.field;image.alt=stage.name;const note=document.createElement('p');note.textContent=stage.kind+' · '+stage.withdrawnPorts+' roles withdrawn';card.append(title,image,note);card.addEventListener('click',()=>{rung.value=index;show();});stages.append(card);}document.querySelectorAll('.card').forEach(card=>card.classList.toggle('selected',card.dataset.case===id));show();drawRatings();}
function filter(){const family=document.getElementById('family').value,mode=document.getElementById('review-filter').value;let count=0;for(const card of document.querySelectorAll('.card')){const c=byId.get(card.dataset.case),r=validReview(c);let visible=!family||c.family===family;if(mode==='unrated')visible&&=!r?.rating;if(mode==='high')visible&&=(r?.rating||0)>=4;if(mode==='low')visible&&=(r?.rating||0)>0&&(r?.rating||0)<=2;if(mode==='crossings')visible&&=c.read.unjoinedCrossings>0;if(mode==='contacts')visible&&=c.read.unjoinedContacts>0;if(mode==='overlaps')visible&&=c.read.geometricOverlaps>0;card.hidden=!visible;if(visible)count++;}document.getElementById('visible-count').textContent=count+' / '+cases.length+' designs';}
design.addEventListener('change',()=>choose(design.value));rung.addEventListener('input',show);document.getElementById('previous').addEventListener('click',()=>choose(cases[(cases.indexOf(current)+cases.length-1)%cases.length].id));document.getElementById('next').addEventListener('click',()=>choose(cases[(cases.indexOf(current)+1)%cases.length].id));document.getElementById('family').addEventListener('change',filter);document.getElementById('review-filter').addEventListener('change',filter);document.querySelectorAll('.card').forEach(card=>{card.addEventListener('click',()=>{choose(card.dataset.case);document.getElementById('inspection').scrollIntoView({behavior:'smooth'});});card.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){e.preventDefault();card.click();}});});document.querySelectorAll('[data-rating]').forEach(button=>button.addEventListener('click',()=>{const review=validReview(current);ratings[current.id]={rating:Number(button.dataset.rating),note:review?.note||'',fingerprint:current.read.fingerprint,packetSha256:current.packetSha256};save();}));document.getElementById('review-note').addEventListener('input',e=>{const review=validReview(current);ratings[current.id]={rating:review?.rating||0,note:e.target.value,fingerprint:current.read.fingerprint,packetSha256:current.packetSha256};try{localStorage.setItem(key,JSON.stringify(ratings));}catch{storage=false;}});
document.getElementById('export').addEventListener('click',()=>{const blob=new Blob([JSON.stringify({schema:key,question:'Satisfying as a lattice?',ratings},null,2)],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='gojo-lattice-ratings.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);});document.getElementById('import').addEventListener('click',()=>document.getElementById('import-file').click());document.getElementById('import-file').addEventListener('change',async e=>{try{const content=JSON.parse(await e.target.files[0].text());if(content.schema!==key||!content.ratings)throw Error('Unknown rating format');for(const [id,review] of Object.entries(content.ratings)){const c=byId.get(id);if(c&&review.fingerprint===c.read.fingerprint&&Number.isInteger(review.rating)&&review.rating>=0&&review.rating<=5&&typeof review.note==='string')ratings[id]=review;}save();}catch(error){document.getElementById('rating-status').textContent='Could not import: '+error.message;}e.target.value='';});window.addEventListener('hashchange',()=>{const id=location.hash.slice(1);if(byId.has(id)){choose(id);document.getElementById('inspection').scrollIntoView();}});window.addEventListener('load',()=>{if(deepLinked)requestAnimationFrame(()=>document.getElementById('inspection').scrollIntoView());});choose(deepLinked?location.hash.slice(1):cases[0].id);filter();
</script></main></body></html>
""";
    }
    private static string Overview(IReadOnlyList<Plate> plates) => "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>100 lattices · whole sheet</title><style>" + Css + "</style><main><span class='eyebrow'>Gojo / structural lattice catalogue</span><h1>One hundred unfilled constructions.</h1><p>Twenty families, five variations each. Each final design is generated by native repeated structure with explicit seams and retained edit history. Click a design to inspect its stages and rate it.</p><nav><a href='gallery.html'>Inspect and rate</a><a href='overview.svg'>Whole sheet · SVG</a><a href='construction-strips.html'>All construction strips</a></nav><div class='overview'>" + string.Join("", plates.Select(plate => $"<figure id='{plate.Id}'><figcaption><a href='gallery.html#{plate.Id}'>{H(plate.Id + " · " + plate.Name)}</a></figcaption><img src='{plate.Id}.svg' alt='{H(plate.Name)}' loading='lazy'><p class='note'>{H(plate.Family)}</p></figure>")) + "</div></main></html>";
    private static string ConstructionStrips(IReadOnlyList<Plate> plates) => "<!doctype html><html lang='en'><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Lattice construction strips</title><style>" + Css + "</style><main><span class='eyebrow'>Gojo / source, withdrawal, replacement and transformation</span><h1>Read the changes across each row.</h1><nav><a href='gallery.html'>Inspect and rate</a><a href='overview.html'>All final designs</a></nav>" + string.Join("", plates.Select(plate => $"<section class='construction' id='{plate.Id}'><h2><a href='gallery.html#{plate.Id}'>{H(plate.Id + " · " + plate.Name)}</a></h2><p>{H(plate.Description)}</p><div class='strips'>" + string.Join("", plate.Stages.Select(stage => $"<article class='stage'><h3>{H(stage.Name)}</h3><img src='{stage.Field}' alt='{H(stage.Name)}' loading='lazy'><p>{H(stage.Kind)} · {stage.WithdrawnPorts} roles withdrawn</p></article>")) + $"</div><img class='change' src='{plate.Stages[^1].Change}' alt='Final native path change' loading='lazy'></section>")) + "</main></html>";
    private static string OverviewSvg(IReadOnlyList<Plate> plates, string output)
    {
        const int columns = 8, cellWidth = 300, cellHeight = 238;
        var height = 110 + (int)Math.Ceiling(plates.Count / (double)columns) * cellHeight;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' width='{columns * cellWidth}' height='{height}' viewBox='0 0 {columns * cellWidth} {height}'><rect width='100%' height='100%' fill='#ECE9DE'/><text x='24' y='44' font-family='Georgia,serif' font-size='32' fill='#304C50'>One hundred lattices · native construction catalogue</text><text x='25' y='78' font-family='sans-serif' font-size='16' fill='#64736B'>Retained bases, released constraints and repeated structure · selected screen proportions</text>");
        for (var index = 0; index < plates.Count; index++)
        {
            var plate = plates[index]; var x = index % columns * cellWidth; var y = 110 + index / columns * cellHeight;
            var root = System.Xml.Linq.XElement.Parse(File.ReadAllText(Path.Combine(output, plate.Id + ".svg")));
            root.SetAttributeValue("x", x + 7); root.SetAttributeValue("y", y + 36); root.SetAttributeValue("width", cellWidth - 14); root.SetAttributeValue("height", cellHeight - 45);
            var title = plate.Name.Length > 35 ? plate.Name[..32] + "…" : plate.Name;
            svg.Append($"<text x='{x + 10}' y='{y + 15}' font-family='Georgia,serif' font-size='14' fill='#304C50'>{H(plate.Id + " · " + title)}</text><text x='{x + 10}' y='{y + 30}' font-family='sans-serif' font-size='10' fill='#64736B'>{H(plate.Family)}</text>{root}");
        }
        return svg.Append("</svg>").ToString();
    }
}
