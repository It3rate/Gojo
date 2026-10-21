using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using SER = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation over actual native query/completion receipts. The
/// drawings index support identities or illustrate the selected logical read;
/// their screen lengths and angles do not supply premises to the engine.</summary>
public static class StructuralPotentialGallery
{
    public static void Write(string output, IReadOnlyList<StructuralPotentialStudy.Case> cases)
    {
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var designs = new List<object>(); var queryCount = 0; var samples = 0;
        foreach (var item in cases)
        {
            var f = item.Frame;
            var names = f.Leaves.Concat(f.Goals).ToArray();
            string Name(IElement predicate) => names.FirstOrDefault(row => ReferenceEquals(row.Predicate, predicate))?.Name ?? "Unresolved native participation";
            string[] Names(Relation scope) => E(scope).Select(Name).ToArray();
            File.WriteAllText(Path.Combine(output, "patterns", item.Id + "-source.svg"), SourceSvg(item));
            var reads = new List<object>();
            for (var goalIndex = 0; goalIndex < f.Goals.Count; goalIndex++)
                foreach (var capacity in new[] { 64, 4, 0 })
                {
                    var goal = f.Goals[goalIndex];
                    var finite = ReferenceEquals(goal.Predicate, f.FiniteBoundary) || ReferenceEquals(goal.Predicate, f.FiniteRectangle) || ReferenceEquals(goal.Predicate, f.FiniteSquare);
                    var completion = finite ? f.Complete(item.Source, goal.Predicate, item.AllowedAssumptions, capacity) : null;
                    var read = completion?.SourceRead ?? SER.Read(f.Laws, item.Source, goal.Predicate, capacity);
                    queryCount++;
                    string? sampleImage = null;
                    if (completion?.Sample is { } sample)
                    {
                        sampleImage = $"patterns/{item.Id}-g{goalIndex}-q{capacity}.svg";
                        File.WriteAllText(Path.Combine(output, sampleImage), SampleSvg(item, sample, goal.Predicate)); samples++;
                    }
                    var consulted = f.Leaves.Where(leaf => E(read.Visited).Any(predicate => ReferenceEquals(predicate, leaf.Predicate)))
                        .Select(leaf => new { leaf.Name, Answers = item.Source.Evidence.Where(fact => ReferenceEquals(fact.Predicate, leaf.Predicate)).Select(fact => fact.Answer).ToArray() }).ToArray();
                    reads.Add(new { Goal = "g" + goalIndex, Capacity = capacity, goal.Name, Finite = finite,
                        SourceAnswer = read.Answer, Visited = Names(read.Visited), Unasked = Names(read.Unasked),
                        Missing = Names(read.Missing), BudgetCuts = Names(read.BudgetCuts),
                        Consumed = E(read.Consumed).Length, Consulted = consulted,
                        SourceConflictProofs = E(read.Conflicts).Length, SourceRefusalProofs = E(read.Refuted).Length,
                        SourceBlockedProofs = E(read.Blocked).Length,
                        ConditionalAnswer = completion?.ConditionalRead.Answer,
                        CompletionAnswer = completion?.Answer, Assumed = completion is null ? [] : Names(completion.AssumedPredicates),
                        ConditionalMissing = completion is null ? [] : Names(completion.ConditionalRead.Missing),
                        ConditionalCuts = completion is null ? [] : Names(completion.ConditionalRead.BudgetCuts),
                        ConditionalVisited = completion is null ? [] : Names(completion.ConditionalRead.Visited),
                        SampleImage = sampleImage,
                        SampleSites = completion?.Sample is { } a ? E(a.Observation.Sites).Length : 0,
                        SampleEdges = completion?.Sample is { } b ? E(b.Observation.Segments).Length : 0,
                        CycleWitnesses = completion?.Sample is { } c ? E(c.CycleWitness).Length : 0,
                        SharedReach = completion?.Sample is { } d && E(d.Parameters).OfType<Relation>().Select(row => row.Right).Distinct(ReferenceEqualityComparer.Instance).Count() == 1,
                        Promises = completion?.Sample is { } e ? Names(e.Promises) : [] });
                }
            designs.Add(new { item.Id, item.Name, item.Description,
                SourceImage = "patterns/" + item.Id + "-source.svg",
                SourceSites = E(f.SourceObservation.Sites).Length, SourceEdges = E(f.SourceObservation.Segments).Length,
                Authored = f.Leaves.Select(leaf => new { leaf.Name, Answers = item.Source.Evidence.Where(fact => ReferenceEquals(fact.Predicate, leaf.Predicate)).Select(fact => fact.Answer).ToArray() }),
                Allowed = Names(item.AllowedAssumptions), Reads = reads });
        }
        var model = new { Schema = "structural-potential-v1", Counts = new { Cases = cases.Count, Queries = queryCount, Samples = samples },
            Goals = cases[0].Frame.Goals.Select((goal, i) => new { Id = "g" + i, goal.Name, Finite = i >= 3 }),
            Cases = designs,
            Limits = new[] {
                "Source premises are authored native publications, not geometric facts discovered from these diagrams. The support picture indexes four source passages and eight endpoints; it does not publish their crossings or measured positions.",
                "A Yes result supports the selected conjunctive construction contract. A No result refutes that construction under its stated premises; it is not a theorem that no other construction or support selection can work.",
                "Supporting-line potential and finite-support coverage are separate queries. A known clipped support can refute the finite candidate while preserving the supporting-line result.",
                "Completion may add only explicitly permitted missing leaf premises. Assumptions stay separate from the original source and cannot reverse a known negative or resolve a conflict by choosing a side.",
                "New corner sites belong to a query-scoped observational boundary. They do not weld the original source passages, mutate the eight original endpoints, establish a material joint, or assign filled-interior ownership.",
                "The sample diagrams illustrate topology and any requested angle/reach promises. Their chosen proportions are not calibrated measurements. Independent open parameters do not establish unequal values.",
                "Query counters describe each native read. This gallery eagerly prepares all case/query/capacity comparisons; it makes no frontend or end-to-end demand-driven speed claim. Unasked predicates, missing replies and budget cuts remain different states."
            } };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.STRUCTURAL_POTENTIAL=" + json + ";");
        File.WriteAllText(Path.Combine(output, "gallery.html"), Html());
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview());
    }

    private static string SourceSvg(StructuralPotentialStudy.Case item)
    {
        var svg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 620 300' role='img'><title>Source support identities, not a measured arrangement</title><rect width='620' height='300' fill='#faf8ef'/>");
        svg.Append("<text x='25' y='30' font-family='system-ui,sans-serif' font-size='15' fill='#526b70'>Source passages · separate identity panels</text>");
        for (var family = 0; family < 2; family++)
        {
            var x = 25 + family * 300;
            svg.Append($"<rect x='{x}' y='55' width='270' height='175' rx='5' fill='#edf0e8' stroke='#c5d1c7'/><text x='{x + 16}' y='82' font-family='system-ui,sans-serif' font-size='14' fill='#526b70'>Family {(family == 0 ? "A" : "B")}</text>");
            for (var i = 0; i < 2; i++)
            {
                var y = 125 + i * 65;
                svg.Append($"<line class='source-segment' x1='{x + 52}' y1='{y}' x2='{x + 232}' y2='{y}' stroke='#376b76' stroke-width='3.5'/><circle cx='{x + 52}' cy='{y}' r='5' fill='#faf8ef' stroke='#376b76' stroke-width='2'/><circle cx='{x + 232}' cy='{y}' r='5' fill='#faf8ef' stroke='#376b76' stroke-width='2'/><text x='{x + 15}' y='{y + 5}' font-family='system-ui,sans-serif' font-size='14' fill='#526b70'>{(family == 0 ? "A" : "B")}{i}</text>");
            }
        }
        svg.Append("<text x='25' y='260' font-family='system-ui,sans-serif' font-size='12' fill='#748181'>Eight original endpoints · no published shared crossing sites</text><text x='25' y='280' font-family='system-ui,sans-serif' font-size='12' fill='#748181'>Panel layout is an index, not the geometric position of the supports.</text></svg>");
        return svg.ToString();
    }

    private static string SampleSvg(StructuralPotentialStudy.Case item, StructuralPotential.BoundarySample sample, Relation goal)
    {
        var square = ReferenceEquals(goal, item.Frame.FiniteSquare);
        var rectangle = square || ReferenceEquals(goal, item.Frame.FiniteRectangle);
        (int X, int Y)[] positions = square ? [(210, 55), (410, 55), (410, 255), (210, 255)] :
            rectangle ? [(120, 80), (500, 80), (500, 230), (120, 230)] : [(120, 115), (430, 55), (500, 210), (190, 270)];
        var sites = E(sample.Observation.Sites);
        var svg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 620 330' role='img'><title>Query-scoped boundary read; illustrative proportions</title><rect width='620' height='330' fill='#faf8ef'/>");
        foreach (var edge in E(sample.Observation.Segments).OfType<Relation>())
        {
            sample.Observation.TryReadSegment(edge, out _, out var from, out var to, out _);
            var a = positions[Array.FindIndex(sites, site => ReferenceEquals(site, from))];
            var b = positions[Array.FindIndex(sites, site => ReferenceEquals(site, to))];
            svg.Append($"<line class='actual-segment' x1='{a.X}' y1='{a.Y}' x2='{b.X}' y2='{b.Y}' stroke='#a97836' stroke-width='4'/>");
        }
        for (var i = 0; i < sites.Length; i++)
        {
            var p = positions[i];
            svg.Append($"<circle class='read-site' cx='{p.X}' cy='{p.Y}' r='6' fill='#a97836'/><text x='{p.X + 10}' y='{p.Y - 10}' font-family='system-ui,sans-serif' font-size='13' fill='#785b35'>K{i}</text>");
        }
        svg.Append("<text x='25' y='310' font-family='system-ui,sans-serif' font-size='12' fill='#748181'>Four new observational sites · no measured lengths or source-graph weld</text></svg>");
        return svg.ToString();
    }

    private static IElement[] E(Relation scope) => StructuralPotential.Items(scope);

    private static string Html() => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · structural potential and conditional completion</title><style>
:root{--paper:#f7f5ed;--ink:#29464d;--muted:#65767a;--line:#cbd3cd;--white:#fffdf7;--source:#3c737c;--deduced:#687982;--assumed:#a47b42}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.5 system-ui,sans-serif}main{max-width:1560px;margin:auto;padding:28px 30px 45px}h1,h2,h3{font-family:Georgia,serif;font-weight:500;line-height:1.18}h1{font-size:clamp(31px,4vw,47px);max-width:1100px;margin:8px 0 16px}h2{font-size:26px;margin:0 0 12px}h3{font-size:20px;margin:0 0 8px}p{margin:8px 0 15px}a{color:var(--source);text-underline-offset:3px}.eyebrow{font-size:12px;text-transform:uppercase;letter-spacing:.13em;color:var(--muted)}.intro{font-size:18px;max-width:1050px}.links{display:flex;gap:18px;flex-wrap:wrap;font-size:14px;margin:18px 0 24px}.controls{display:grid;grid-template-columns:1.5fr 1.4fr 1fr;gap:15px;padding:17px;background:#e9ede5;border:1px solid var(--line);border-radius:7px}label{display:flex;flex-direction:column;gap:5px;font-size:13px;font-weight:650}select,button{font:inherit;color:inherit;background:var(--white);border:1px solid #aebfb8;border-radius:5px;padding:9px 11px;max-width:100%}select{font-size:14px;width:100%;min-width:0}button{cursor:pointer}button:focus-visible,select:focus-visible,summary:focus-visible{outline:3px solid #c9a266;outline-offset:3px}.case-note{margin:17px 0 23px;padding:13px 17px;background:var(--white);border-left:3px solid #8aa39b}.panels{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px}.panel{min-width:0;background:var(--white);border:1px solid var(--line);border-top:4px solid var(--source);border-radius:6px;padding:18px}.panel.deduced{border-top-color:var(--deduced)}.panel.assumed{border-top-color:var(--assumed)}.panel img{width:100%;height:215px;object-fit:contain;background:#faf8ef;display:block}.caption{font-size:12px;color:var(--muted);margin:8px 0 16px}.status{font-size:14px;font-weight:650;border:1px solid #b8c6bf;background:#edf1e9;display:block;padding:8px 10px;border-radius:4px;margin:10px 0 13px}.status.neutral{background:#efede5;border-color:#c8bca8}.status.unresolved{background:#f4eddc;border-color:#cfb88c}.fact-list{display:grid;gap:8px;font-size:13px}.fact-row{border-bottom:1px solid #e2e5dd;padding-bottom:7px}.fact-row strong{display:block;font-size:13px;margin-bottom:3px}.fact-value{font-weight:650}.muted,small{color:var(--muted)}.audit{margin-top:16px;font-size:13px}summary{cursor:pointer;font-size:13px;font-weight:650;padding:5px 0}.allowed{font-size:13px;color:var(--muted);margin-top:14px}.allowed ul{padding-left:18px}.assumptions{font-size:13px;margin:10px 0 17px;padding-left:19px}.assumptions li{margin-bottom:7px}.empty{height:215px;display:grid;place-content:center;border:1px dashed #b9c4bc;color:var(--muted);text-align:center;padding:25px;background:#faf8ef}.sample-summary{font-size:13px}.potentials{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:12px;margin:23px 0}.potential{border:1px solid var(--line);border-radius:6px;background:var(--white);padding:13px;text-align:left;min-width:0;font-size:13px}.potential[aria-selected=true]{border:2px solid #7e9991;padding:12px;background:#edf1e9}.potential strong{font:19px Georgia,serif;display:block;margin-bottom:8px}.potential span{display:block;font-size:12px;color:var(--muted)}.potential .answer{font-weight:650;color:var(--ink);font-size:14px;margin-bottom:5px}.read-section{margin-top:27px;padding:20px;border:1px solid var(--line);border-radius:6px;background:var(--white)}.trace{display:flex;flex-wrap:wrap;gap:8px;margin:13px 0 20px}.trace span{padding:6px 9px;background:#edf0e8;border:1px solid #d7dfd5;border-radius:4px;font-size:12px}.trace small{margin-right:6px}.remainders{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:15px}.remainder{padding:12px;background:#f1f1e9;font-size:13px;border-left:2px solid #a0b0a7}.remainder ul{padding-left:17px;margin:8px 0}.hint{padding:12px 15px;background:#edf0e8;border-left:3px solid #8aa498;font-size:14px}.limits{margin-top:30px;padding-top:22px;border-top:1px solid var(--line)}.limits li{font-size:13px;margin:9px 0}footer{font-size:13px;color:var(--muted);margin-top:25px}[hidden]{display:none!important}@media(max-width:1100px){.panels{grid-template-columns:1fr 1fr}.panel.assumed{grid-column:1/-1}.panel.assumed img{height:260px}.potentials{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:650px){main{padding:20px 15px 35px}.controls,.panels,.potentials,.remainders{grid-template-columns:1fr}.panel{padding:15px}.panel.assumed{grid-column:auto}.panel.assumed img{height:220px}.read-section{padding:15px}}
</style></head><body><main><div class="eyebrow">Gojo · partial sources and query-scoped structure</div><h1>What the source says, what a query supports, and what a completion adds</h1><p class="intro">Four finite passages can support a richer construction question without already being a joined shape. These examples keep published premises, conditional deductions and added assumptions separate.</p><div class="links"><a href="overview.html">All source cases</a><a href="../rule-pack-transfer/gallery.html">Reusable packs and revision</a><a href="../ornament-defaults/gallery.html">Requirements and defaults</a></div><div class="controls"><label>Authored source case<select id="case"></select></label><label>Requested construction read<select id="question"></select></label><label>Native query capacity<select id="capacity"><option value="64">64 predicate slots</option><option value="4">4 predicate slots</option><option value="0">0 slots · leave unread</option></select></label></div><p id="case-note" class="case-note"></p>
<div class="panels"><section class="panel"><div class="eyebrow">1 · source</div><h2>Published support structure</h2><img id="source-image" alt="Four source passage identities"><p class="caption">Identity panels, not a measured arrangement. No crossing sites have been published or welded into the source.</p><h3>Premises inspected for this question</h3><div id="source-facts" class="fact-list"></div><details class="audit"><summary>Audit all authored premises · includes unasked detail</summary><div id="authored" class="fact-list"></div></details></section>
<section class="panel deduced"><div class="eyebrow">2 · selected query</div><h2 id="deduction-title"></h2><div id="source-answer" class="status"></div><p id="deduction-note"></p><div id="deduction-counts" class="hint"></div><p class="caption">A supported construction contract does not assert a material joint, a filled region, or a complete measured object.</p><h3>Unresolved source obligations</h3><div id="source-missing"></div><p id="source-conflicts" class="muted"></p></section>
<section class="panel assumed"><div class="eyebrow">3 · conditional model</div><h2>Selected boundary completion</h2><div id="completion-answer" class="status"></div><img id="completion-image" alt="Query-scoped logical boundary" hidden><div id="no-completion" class="empty"></div><p id="completion-caption" class="caption"></p><div id="completion-assumptions"></div><p id="sample-summary" class="sample-summary"></p><details class="allowed"><summary>Permitted assumption vocabulary</summary><div id="allowed"></div></details></section></div>
<h2 style="margin-top:29px">Other construction questions</h2><p class="muted">Each tile is a separate native query prepared for comparison. It is not additional evidence consulted by the selected query.</p><div id="potentials" class="potentials"></div>
<section class="read-section"><h2>Which obligations did this query actually visit?</h2><p id="query-summary"></p><div id="query-stages" class="trace"></div><div class="remainders"><div class="remainder"><strong>Missing or open replies</strong><div id="missing-remainder"></div></div><div class="remainder"><strong>Not evaluated in this read</strong><div id="unasked-remainder"></div></div><div class="remainder"><strong>Cut by the query budget</strong><div id="budget-remainder"></div></div></div><p class="caption">Unasked obligations are not false. Budget cuts are not source refusals. The original publication remains available for another question.</p><details><summary>Conditional model footprint</summary><div id="conditional-footprint"></div></details></section>
<section class="limits"><h2>Interpretation and scope</h2><ul id="limits"></ul></section><footer id="run-note"></footer></main><script src="data.js"></script><script>
(()=>{'use strict';const d=globalThis.STRUCTURAL_POTENTIAL,$=id=>document.getElementById(id),esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));const chosen=()=>d.cases.find(c=>c.id===$('case').value)||d.cases[0];const current=()=>chosen().reads.find(r=>r.goal===$('question').value&&r.capacity===Number($('capacity').value));const list=(items,empty='None in this read.')=>items.length?'<ul>'+items.map(i=>'<li>'+esc(i)+'</li>').join('')+'</ul>':'<p class="muted">'+esc(empty)+'</p>';const status=answer=>({Yes:'Construction contract supported',No:'This construction recipe is refuted',Conflict:'Conflicting premises remain visible',Open:'Construction remains unresolved',Blocked:'This read is blocked'})[answer]||answer;const supplied=answers=>answers.length?answers.join(' + '):'No reply published';
function rows(items,read){return items.map(f=>'<div class="fact-row"><strong>'+esc(f.name)+'</strong><span class="fact-value">'+esc(supplied(f.answers))+'</span>'+(read?'':' <small>· '+(current().visited.includes(f.name)?'inspected':'not consulted by this query')+'</small>')+'</div>').join('')||'<p class="muted">No leaf premise was inspected within this capacity.</p>'}
function render(){const c=chosen(),r=current();$('case-note').textContent=c.description;$('source-image').src=c.sourceImage;$('source-facts').innerHTML=rows(r.consulted,true);$('authored').innerHTML=rows(c.authored,false);$('deduction-title').textContent=r.name;$('source-answer').textContent=status(r.sourceAnswer);$('source-answer').className='status '+(r.sourceAnswer==='Open'?'unresolved':r.sourceAnswer==='Yes'?'':'neutral');$('deduction-note').textContent=r.finite?'This asks whether the selected encounters are covered by all four finite supports, together with any more specific requested promises.':'This concerns the extended supporting-line construction. It does not establish that the finite passages reach the proposed encounters.';$('deduction-counts').textContent=r.consumed+' of '+r.capacity+' predicate slots consumed; '+r.visited.length+' obligations visited. No source value or relation is changed.';$('source-missing').innerHTML=list(r.missing,'No missing leaf premise recorded.');$('source-conflicts').textContent=r.sourceConflictProofs?'The requested contract exposes conflicting native evidence.':r.sourceRefusalProofs?'A supplied refusal refutes this candidate recipe.':r.sourceBlockedProofs?'An invalid or blocked participation prevents this read.':'';const completionStatus=!r.finite?'Observation only · no completion requested':r.sampleImage?(r.assumed.length?'Witnessed conditional boundary read':'Boundary read witnessed from supplied premises'):status(r.completionAnswer);$('completion-answer').textContent=completionStatus;$('completion-answer').className='status '+(!r.finite?'neutral':r.completionAnswer==='Open'?'unresolved':r.sampleImage?'':'neutral');$('completion-image').hidden=!r.sampleImage;$('no-completion').hidden=!!r.sampleImage;if(r.sampleImage){$('completion-image').src=r.sampleImage}else{$('completion-image').removeAttribute('src');$('no-completion').textContent=r.finite?'No completed boundary is committed at this read.':'Supporting-line questions are observed; only finite goals request a completion.'}$('completion-caption').textContent=r.sampleImage?'New observational corner sites; chosen diagram proportions are not measurements. The four source passages and eight endpoints are preserved.':'A missing drawing is an unresolved or unrequested state, not an invented zero-size object.';$('completion-assumptions').innerHTML=!r.finite?'':r.assumed.length?'<h3>Added premises · not source facts</h3>'+list(r.assumed):'<p class="muted">No assumption was introduced.</p>';$('sample-summary').textContent=r.sampleImage?r.sampleSites+' read sites · '+r.sampleEdges+' boundary edges · native four-edge cycle witness '+(r.cycleWitnesses?'present. ':'unresolved. ')+(r.sharedReach?'One shared open reach; no magnitude assigned.':'Two open reach roles; this read does not assert either equality or inequality.'):'The source result stays '+r.sourceAnswer+'; conditional permission does not rewrite it.';$('allowed').innerHTML=list(c.allowed,'This context authorizes no missing-premise assumptions.');$('potentials').innerHTML=d.goals.map(goal=>{const other=c.reads.find(x=>x.goal===goal.id&&x.capacity===r.capacity);return '<button class="potential" data-question="'+esc(goal.id)+'" aria-selected="'+(goal.id===r.goal)+'"><strong>'+esc(goal.name)+'</strong><span class="answer">'+esc(status(other.sourceAnswer))+'</span><span>'+other.consumed+' visited; '+other.missing.length+' missing; '+other.budgetCuts.length+' budget cuts</span></button>'}).join('');$('potentials').querySelectorAll('[data-question]').forEach(button=>button.onclick=()=>{$('question').value=button.dataset.question;render()});$('query-summary').textContent='Requested: '+r.name+'. '+r.consumed+' actual query slots consumed. The sequence below follows the native source read, before assumptions.';$('query-stages').innerHTML=r.visited.length?r.visited.map((name,i)=>'<span><small>'+(i+1)+'</small>'+esc(name)+'</span>').join(''):'<p class="muted">No predicate was visited.</p>';$('missing-remainder').innerHTML=list(r.missing);$('unasked-remainder').innerHTML=list(r.unasked);$('budget-remainder').innerHTML=list(r.budgetCuts);$('conditional-footprint').innerHTML=!r.finite?'<p class="muted">No conditional model requested.</p>':'<p>Conditional result: <strong>'+esc(r.conditionalAnswer)+'</strong>. This result belongs to source premises plus the explicitly listed assumptions.</p><p>Missing after assumptions:</p>'+list(r.conditionalMissing)+'<p>Budget cuts after assumptions:</p>'+list(r.conditionalCuts)+'<p>Visited:</p>'+list(r.conditionalVisited)}
$('case').innerHTML=d.cases.map(c=>'<option value="'+esc(c.id)+'">'+esc(c.id+' · '+c.name)+'</option>').join('');$('question').innerHTML=d.goals.map(g=>'<option value="'+esc(g.id)+'">'+esc(g.name)+'</option>').join('');$('question').value='g3';const params=new URLSearchParams(location.search);if(d.cases.some(c=>c.id===params.get('case')))$('case').value=params.get('case');if(d.goals.some(g=>g.id===params.get('question')))$('question').value=params.get('question');for(const id of ['case','question','capacity'])$(id).onchange=render;$('limits').innerHTML=d.limits.map(text=>'<li>'+esc(text)+'</li>').join('');$('run-note').textContent=d.counts.cases+' authored source cases · '+d.counts.queries+' independent native reads · '+d.counts.samples+' witnessed finite boundary completions. Illustrative geometry never supplies rule truth.';render();
})();
</script></body></html>
""";

    private static string Overview() => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · structural potential cases</title><style>*{box-sizing:border-box}body{margin:0;background:#f7f5ed;color:#29464d;font:15px/1.5 system-ui,sans-serif}main{max-width:1600px;margin:auto;padding:25px}h1,h2{font-family:Georgia,serif;font-weight:500}h1{font-size:36px;line-height:1.2}h2{font-size:22px;margin:0 0 12px}a{color:#3c737c}.cards{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:17px}.card{background:#fffdf7;border:1px solid #cbd3cd;border-radius:7px;padding:17px;min-width:0}.card img{display:block;width:100%;height:170px;object-fit:contain}.card p{font-size:13px;color:#65767a}.queries{display:flex;gap:7px;flex-wrap:wrap}.queries a{font-size:12px;text-decoration:none;border:1px solid #b8c6bf;border-radius:4px;padding:5px 7px}.id{font:12px system-ui,sans-serif;color:#65767a;display:block;margin-bottom:5px}@media(max-width:1000px){.cards{grid-template-columns:1fr 1fr}}@media(max-width:600px){main{padding:20px 15px}.cards{grid-template-columns:1fr}}</style></head><body><main><h1>Partial premises, different construction questions</h1><p>All source diagrams index the same four passage roles. The authored publications differ; no angle or length is inferred from these pictures.</p><p><a href="gallery.html">Inspect source facts, native query footprints and conditional completions</a></p><div id="cases" class="cards"></div></main><script src="data.js"></script><script>(()=>{const d=globalThis.STRUCTURAL_POTENTIAL,esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));document.getElementById('cases').innerHTML=d.cases.map(c=>'<article class="card" data-case="'+esc(c.id)+'"><h2><span class="id">'+esc(c.id)+'</span>'+esc(c.name)+'</h2><img loading="lazy" src="'+esc(c.sourceImage)+'" alt="Source support identity diagram"><p>'+esc(c.description)+'</p><div class="queries">'+d.goals.filter(g=>g.finite).map(g=>{const r=c.reads.find(r=>r.goal===g.id&&r.capacity===64);return '<a href="gallery.html?case='+encodeURIComponent(c.id)+'&amp;question='+encodeURIComponent(g.id)+'">'+esc(g.name)+': source '+esc(r.sourceAnswer)+' / model '+esc(r.completionAnswer)+'</a>'}).join('')+'</div></article>').join('')})()</script></body></html>
""";
}
