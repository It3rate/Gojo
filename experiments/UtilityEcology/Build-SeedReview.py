"""Create a small, offline review of a completed protected-candidate study.

Reads audited analysis and the first seed's five raw traces once. No external
dependencies, requests or simulation. Does not overwrite earlier review pages.
"""
import argparse
import html
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
NAMES = ("seed/error-only", "seed/progress", "seed/paired", "seed/unfiltered", "seed/no-seed")
LABELS = dict(zip(NAMES, ("Existing error", "Existing progress", "Seed + paired", "Seed + unfiltered", "Paired, no seed")))


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def escape(value):
    return html.escape(str(value), quote=True)


def num(value, places=3):
    return "—" if value is None else f"{value:,.{places}f}"


def percent(value):
    return "—" if value is None else f"{value:.1%}"


def interval(value):
    ci = value["ci95"]
    return "No interval for this pilot" if ci is None else f"95% interval [{ci[0]:.3f}, {ci[1]:.3f}]"


def load(directory):
    completion, manifest, analysis = (read(directory / name) for name in ("run.json", "manifest.json", "analysis.json"))
    require(completion.get("status") == "complete" and manifest.get("study") == "protected-seed-trials", "Completed seed study required")
    require(analysis["protocol"] == completion and analysis["settings"] == manifest["settings"], "Analysis differs from completed study")
    require(set(analysis["arms"]) == set(NAMES), "Five declared arms required")
    require(all(analysis["audit"][k] for k in ("allTaskAccountingChecks", "pairedTapesAndSourceSchedules", "trialCounterAndSeedFundingChecks", "sourceHashesMatch")), "Audit did not pass")
    require(analysis["audit"]["sourceHashes"] == manifest["sourceHashes"], "Frozen hashes differ")
    seed = analysis["seeds"][0]
    runs = []
    for name in NAMES:
        run = read(directory / f"{seed}-{name.replace('/', '-')}.json")
        require((run["arm"], run["seed"]) == (name, seed), "Raw first-seed identity differs")
        ecology = run["ecology"]
        expected = next(r for r in analysis["arms"][name]["perSeed"] if r["seed"] == seed)
        require(ecology["gross"] == expected["gross"] and ecology["burned"] == expected["burned"], "Raw totals differ from analysis")
        require(len(ecology["trace"]) == manifest["settings"]["phaseLength"] * 4, "Raw trace length differs")
        runs.append(run)
    require(len({r["ecology"]["tapeDigest"] for r in runs}) == 1, "First-seed tapes differ")
    return completion, manifest, analysis, runs


def project(run):
    states = {s["tick"]: s for s in run["seedTrace"]}
    keys = ("tick", "phase", "source", "bought", "interpreted", "advice", "action", "correct", "gross", "burned", "learningGranted", "refined")
    return {"arm": run["arm"], "seed": run["seed"], "trace": [{k: s[k] for k in keys} | {"trial": states.get(s["tick"])} for s in run["ecology"]["trace"]]}


def build(completion, manifest, analysis, runs):
    arms, contrasts = analysis["arms"], analysis["pairedContrasts"]
    n = len(analysis["seeds"])
    contrast = contrasts["paired minus unfiltered"]
    ci = contrast["ci95"]
    qualified = ci is not None and ci[0] <= 0 <= ci[1]
    headline = ("The paired admission effect was small and inconclusive: "
                if qualified else "The paired admission contrast was: ")
    headline += f"<strong>{contrast['mean']:+.3f} net resource per request</strong> versus unconditional replacement ({interval(contrast)})."
    best = max(NAMES, key=lambda name: arms[name]["netPerStep"]["mean"])
    headline += f" <strong>{LABELS[best]}</strong> had the highest mean whole-group return."
    paired, unfiltered = arms["seed/paired"], arms["seed/unfiltered"]
    if paired["gross"] == unfiltered["gross"]:
        headline += " Their aggregate physical gross was equal; the net difference came from paid burn. Individual decisions and seed returns can still differ."
    group, trials, tasks, rows, financing = [], [], [], [], []
    for name in NAMES:
        a = arms[name]; t = a["trials"]
        group.append(f"<tr><th scope='row'>{LABELS[name]}</th><td>{num(a['netPerStep']['mean'])}</td><td>{num(a['grossPerStep']['mean'])}</td><td>{num(a['burnPerStep']['mean'])}</td><td>{percent(a['coverage'])}</td><td>{percent(a['interpretationCoverage'])}</td></tr>")
        pending = t['launches'] - t['completions'] - t['aborts']
        trials.append(f"<tr><th scope='row'>{LABELS[name]}</th><td>{t['launches']}</td><td>{t['completions']}</td><td>{t['aborts']}</td><td>{pending}</td><td>{t['promotions']}</td><td>{t['coverageFailures']}</td><td>{t['seedWork']:,}</td><td>{t['earnedWork']:,}</td></tr>")
        closed = sum(s['allFourClosed'] for s in a['afterAllStartupClosed'])
        unused = sum(s['seedRemaining'] for r in a['financing'] for s in r['sources'])
        financing.append(f"<tr><th scope='row'>{LABELS[name]}</th><td>{num(a['finalPhase']['netPerStep']['mean'])}</td><td>{unused:,}</td><td>{closed}/{n}</td></tr>")
        noise, new, rare, common = a["pureNoise"], a["newFinalUtility"], a["rareReturnFirst16Requests"], a["commonAdaptFirst16Requests"]
        nt = noise["trials"]
        tasks.append(f"<tr><th scope='row'>{LABELS[name]}</th><td>{noise['grants']:,}</td><td>{nt['seedWork'] + nt['earnedWork']:,}</td><td>{nt['promotions']}</td><td>{common['correct']}/{common['steps']}</td><td>{rare['correct']}/{rare['steps']}</td><td>{num(new['netPerStep']['mean'])}<small>{new['profitableSeeds']}/{n} profitable seeds</small></td></tr>")
        for value in a["perSeed"]:
            rows.append(f"<tr><th scope='row'>{value['seed']}</th><td>{LABELS[name]}</td><td>{num(value['netPerStep'])}</td><td>{value['gross']:,}</td><td>{value['burned']:,}</td></tr>")
    cards = []
    for label, value in contrasts.items():
        cards.append(f"<article class='card'><h3>{escape(label)}</h3><p class='big'>{value['mean']:+.3f}</p><p>{interval(value)}</p><small>{value['positiveSeeds']}/{n} positive seed differences · {'primary' if value['primary'] else 'package diagnostic'}</small></article>")
    options = "".join(f"<option value='{escape(name)}'{ ' selected' if name == 'seed/paired' else ''}>{LABELS[name]}</option>" for name in NAMES)
    notes = "".join(f"<li>{escape(note)}</li>" for note in analysis["notes"])
    data = {"runs": [project(r) for r in runs], "labels": LABELS,
            "closures": {name: arms[name]["trials"]["closedTrials"] for name in NAMES}, "law": analysis["trialLaw"]}
    payload = json.dumps(data, separators=(",", ":"), allow_nan=False).replace("<", "\\u003c").replace("&", "\\u0026")
    replacements = {"@@N@@": str(n), "@@TASKS@@": f"{completion['trials']:,}", "@@SEED@@": str(analysis["seeds"][0]),
                    "@@HEADLINE@@": headline, "@@GROUP@@": "".join(group),
                    "@@TRIALS@@": "".join(trials), "@@TASKROWS@@": "".join(tasks), "@@CARDS@@": "".join(cards),
                    "@@SEEDROWS@@": "".join(rows), "@@OPTIONS@@": options, "@@NOTES@@": notes, "@@DATA@@": payload,
                    "@@FINANCING@@": "".join(financing),
                    "@@LAW@@": f"Train {manifest['trainingEvents']} · validate {manifest['validationEvents']} · seed cap {manifest['trialWorkCap']} · known ≥{manifest['minimumKnownValidation']} · retry ≥{manifest['retryPeriod']} purchases"}
    page = TEMPLATE
    for marker, value in replacements.items():
        page = page.replace(marker, value)
    return page


TEMPLATE = r'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Gojo · Protected candidate trials</title><style>
:root{color-scheme:light;--ink:#153437;--muted:#54676b;--line:#d8e3e1;--paper:#f4f7f4;--good:#196551;--bad:#a04434}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.5 system-ui,sans-serif}main{max-width:1120px;margin:auto;padding:28px 20px 56px}h1{font-size:clamp(1.8rem,4vw,2.6rem);line-height:1.15;margin:.25em 0}h2{font-size:1.35rem;margin:0 0 12px}h3{font-size:1rem;margin:0 0 8px}p{max-width:80ch;margin:8px 0 16px}.eyebrow{text-transform:uppercase;letter-spacing:.12em;font-size:.75rem;color:var(--muted);font-weight:700}.lead{font-size:1.1rem}.notice{border-left:4px solid #6f9481;padding:12px 16px;background:#e8f0e8}section{margin-top:30px;background:white;border:1px solid var(--line);border-radius:14px;padding:20px}.scroll{overflow-x:auto}table{border-collapse:collapse;width:100%;font-size:.89rem}th,td{text-align:right;padding:10px 12px;border-bottom:1px solid var(--line);white-space:nowrap}th:first-child,td:first-child{text-align:left}th{font-weight:650}thead{background:#f2f6f2}small{display:block;color:var(--muted);font-size:.8rem}td small{max-width:17ch;white-space:normal}a{color:#195d59}summary{cursor:pointer;font-weight:650;padding:8px 0}.cards{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.card{padding:16px;border:1px solid var(--line);border-radius:10px;background:#f7faf6}.big{font-size:1.7rem;font-weight:700;margin:0}.controls{display:flex;gap:16px;flex-wrap:wrap}label{display:block;color:var(--muted);font-size:.9rem}select,button{font:inherit;padding:8px 10px;border:1px solid #9bafaa;border-radius:6px;background:white;color:var(--ink)}input[type=range]{width:100%;min-height:32px;accent-color:#286353}button{cursor:pointer}button:disabled{opacity:.4;cursor:default}:focus-visible{outline:3px solid #5a89ba;outline-offset:2px}svg{width:100%;height:auto;display:block;margin:16px 0}.key{display:flex;gap:16px;flex-wrap:wrap;font-size:.85rem}.dot{display:inline-block;width:10px;height:10px;border-radius:50%;margin-right:5px}.facts{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:8px}.fact{background:#f2f6f2;padding:10px;border-radius:6px}.fact strong{display:block;font-size:1.05rem}.details{margin-top:12px;padding-top:8px;border-top:1px solid var(--line)}.bad{color:var(--bad)}.good{color:var(--good)}ul{padding-left:20px}li{margin:7px 0}.footer{color:var(--muted);font-size:.9rem;margin-top:26px}@media(max-width:600px){main{padding:20px 12px 40px}section{padding:16px 12px}.cards{grid-template-columns:1fr}.facts{grid-template-columns:repeat(2,minmax(0,1fr))}th,td{padding:9px 10px}section h2{font-size:1.2rem}}
</style></head><body><main>
<header><div class="eyebrow">Gojo · opt-in experiment</div><h1>Protect a new idea, then make it pay its way.</h1>
<p class="lead">@@N@@ seeds · @@TASKS@@ physical world requests · five fixed controllers</p>
<p>@@HEADLINE@@</p>
<p class="notice">Startup funds are finite. Later candidate work uses service-earned fees. Those fees move existing resource between structures; <strong>physical gross minus paid burn</strong> tells whether the group gains resource. Prices are abstract.</p>
<p><a href="SEED-PLAN.md">Protocol</a> · <a href="SEED-REPORT.md">Full report</a> · <a href="progress-review.html">Previous progress study</a></p></header>
<section><h2>Actual physical performance</h2><p>Every request is included: holds, failed work, startup and rejected trials. “Advice” is a known model direction; action coverage also includes the supplied fallback.</p>
<div class="scroll"><table><thead><tr><th>Controller</th><th>Net/request</th><th>Gross</th><th>Burn</th><th>Action coverage</th><th>Advice coverage</th></tr></thead><tbody>@@GROUP@@</tbody></table></div>
<details><summary>Show each seed</summary><div class="scroll"><table><thead><tr><th>Seed</th><th>Controller</th><th>Net/request</th><th>Gross total</th><th>Burn total</th></tr></thead><tbody>@@SEEDROWS@@</tbody></table></div></details></section>
<section><h2>Paired contrasts</h2><p>The same request tape is used within each seed. Two primary contrasts test the admission connection and startup support. Existing-controller comparisons concern the whole package.</p>
<div class="cards">@@CARDS@@</div><p><small>Intervals are descriptive, unadjusted Student t(7), only for eight independent seed differences. Requests and trials within a seed are not extra replicates.</small></p></section>
<section><h2>Where the work went</h2><p>@@LAW@@. Paired admission requires a positive resource gain after all trial work and the known-coverage threshold. The unfiltered control pays for the same block but promotes every completed candidate. Unknown is open, not zero; an open candidate uses the incumbent decision for its projected consequence.</p>
<div class="scroll"><table><thead><tr><th>Controller</th><th>Launched</th><th>Completed</th><th>Aborted</th><th>Pending</th><th>Promoted</th><th>Low coverage</th><th>Seed work</th><th>Earned work</th></tr></thead><tbody>@@TRIALS@@</tbody></table></div>
<p><small>Pending trials were still training or validating when the run ended. “Low coverage” includes completed blocks with fewer than 24 known candidate reads. Aborted blocks are incomplete; that alone does not establish a funding cause. Legacy controllers have no candidate-trial records.</small></p>
<details><summary>Startup closure and fixed late return</summary><div class="scroll"><table><thead><tr><th>Controller</th><th>Final-phase group net/request</th><th>Unused seed across runs</th><th>All4 startup trials closed</th></tr></thead><tbody>@@FINANCING@@</tbody></table></div><p><small>The final phase is the same fixed interval for all arms. Legacy controllers have no startup-trial lifecycle. Post-closure windows start at different times and must not replace this fixed comparison. Unused seed stays conserved and cannot subsidize retries.</small></p></details></section>
<section><h2>Noise versus useful adaptation</h2><p>These labels belong to the evaluator. The learners see opaque source ports and physical outcomes; they receive no phase, quality or desired-action label.</p>
<div class="scroll"><table><thead><tr><th>Controller</th><th>Noise grants</th><th>Noise trial work</th><th>Noise promoted</th><th>Common change first16 correct</th><th>Rare return first16 correct</th><th>New utility net/request</th></tr></thead><tbody>@@TASKROWS@@</tbody></table></div>
<p><small>Noise is source 2 throughout. Source 3's earlier noise and later useful phase are kept separate in analysis. First16 figures use world requests, so unserved demand remains visible. Grants are paid base-gate permissions, including during validation freezes; they are not counts of successful ordinal updates. Per-source burn is allocated by request, not counterfactual causal credit.</small></p></section>
<section id="story"><h2>Inspect one actual run</h2><p>Seed @@SEED@@. The trace is observed data; changing these controls only changes the view. Both models are frozen for a paid shared validation block, then the candidate is admitted or refused. Fresh purchases after admission are reported separately.</p>
<div class="controls"><label>Controller<select id="arm">@@OPTIONS@@</select></label><label>Opaque source<select id="source"><option value="0">0 · common</option><option value="1">1 · rare</option><option value="2">2 · independent noise</option><option value="3">3 · later useful</option></select></label></div>
<svg id="plot" viewBox="0 0 800 200" role="img" aria-labelledby="plot-title plot-desc"><title id="plot-title">Cumulative actual gross minus burn for the selected source</title><desc id="plot-desc">A line of actual cumulative physical net return, with markers for trial launches and closures.</desc></svg>
<div class="key"><span><i class="dot" style="background:#256252"></i>Physical cumulative net</span><span><i class="dot" style="background:#947521"></i>Launch</span><span><i class="dot" style="background:#225ba1"></i>Promotion</span><span><i class="dot" style="background:#aa5043"></i>Refusal</span></div>
<p><small>Costs plotted here occur on this source's requests. This is neither global net return nor a causal contribution estimate. Missing physical decisions remain holds.</small></p>
<label for="event">Local source request <span id="position"></span></label><input id="event" type="range" min="0" value="0" aria-label="Selected source request">
<div class="controls"><button id="previous" type="button">Previous</button><button id="next" type="button">Next</button></div>
<div id="facts" class="facts details" aria-live="polite"></div>
<details><summary>Trial closures and fresh outcomes for this source</summary><div id="closures"></div></details></section>
<section><h2>What this can establish</h2><p>The test evaluates a supplied candidate grammar and a fixed native funding/admission law. Favorable selection on a small block can still be luck. Fresh physical results and independent-noise trials are needed to check that.</p><details><summary>Audit scope and interpretation limits</summary><ul>@@NOTES@@</ul></details></section>
<p class="footer">Offline review. All data are embedded; this page performs no simulation, network requests or paid model calls. Earlier review pages and modes remain available.</p>
</main><script type="application/json" id="data">@@DATA@@</script><script>
'use strict';
const data=JSON.parse(document.getElementById('data').textContent), arm=document.getElementById('arm'), source=document.getElementById('source'), slider=document.getElementById('event');
let selected=[];
const svgNS='http://www.w3.org/2000/svg';
function svg(tag,attrs,text){const e=document.createElementNS(svgNS,tag);for(const[k,v]of Object.entries(attrs))e.setAttribute(k,v);if(text!==undefined)e.textContent=text;return e;}
function fmt(v){return v===null||v===undefined?'open':Number(v).toLocaleString(undefined,{maximumFractionDigits:3});}
function fact(label,value){const e=document.createElement('div');e.className='fact';const s=document.createElement('small'),v=document.createElement('strong');s.textContent=label;v.textContent=value;e.append(s,v);return e;}
function current(){return data.runs.find(r=>r.arm===arm.value);}
function plot(){const node=document.getElementById('plot');while(node.children.length>2)node.lastChild.remove();if(!selected.length)return;
let total=0;const points=selected.map((s,i)=>({i,s,v:total+=s.gross-s.burned}));const min=Math.min(0,...points.map(p=>p.v)),max=Math.max(1,...points.map(p=>p.v)),x=i=>58+i*710/Math.max(1,points.length-1),y=v=>170-(v-min)*140/(max-min);
node.append(svg('line',{x1:58,y1:y(0),x2:768,y2:y(0),stroke:'#bacac5','stroke-dasharray':'4 4'}));
for(const value of[min,max])node.append(svg('text',{x:50,y:y(value)+4,'text-anchor':'end',fill:'#54676b','font-size':12},Math.round(value).toLocaleString()));
node.append(svg('polyline',{points:points.map(p=>x(p.i)+','+y(p.v)).join(' '),fill:'none',stroke:'#256252','stroke-width':2}));
for(const p of points){const t=p.s.trial;if(!t)continue;const color=t.promoted?'#225ba1':t.refused?'#aa5043':t.launched?'#947521':null;if(color)node.append(svg('circle',{cx:x(p.i),cy:y(p.v),r:4,fill:color}));}
node.append(svg('text',{x:58,y:194,fill:'#54676b','font-size':12},'First request'));
node.append(svg('text',{x:768,y:194,'text-anchor':'end',fill:'#54676b','font-size':12},'Last request'));
}
function show(){const i=Number(slider.value),s=selected[i],box=document.getElementById('facts');box.replaceChildren();document.getElementById('position').textContent=(i+1)+' / '+selected.length;document.getElementById('previous').disabled=i===0;document.getElementById('next').disabled=i===selected.length-1;if(!s)return;
const t=s.trial;for(const[label,value]of[['World tick / analyst phase',s.tick+' / '+s.phase],['Model advice',s.advice===null?'unknown':s.advice===1?'direction +1':'direction −1'],['Actual command',s.action===0?'hold':fmt(s.action)],['Actual physical gross',fmt(s.gross)],['Paid burn this request',fmt(s.burned)],['Trial status',t?t.status:'legacy controller'],['Seed spent / remaining',t?fmt(t.seedSpent)+' / '+fmt(t.seedRemaining):'not applicable'],['Earned trial work',t?fmt(t.earnedSpent):'not applicable'],['Training / validation events',t?t.trainingCount+' / '+t.validationCount:'not applicable']])box.append(fact(label,value));
}
function closures(){const node=document.getElementById('closures');node.replaceChildren();const values=data.closures[arm.value].filter(v=>v.seed===current().seed&&v.source===Number(source.value));if(!values.length){const p=document.createElement('p');p.textContent='No closed candidate trials for this selection.';node.append(p);return;}
for(const value of values){const article=document.createElement('article');article.className='details';const heading=document.createElement('h3'),p=document.createElement('p');heading.textContent='Trial '+value.id+' · tick '+value.tick+' · '+(value.promoted?'promoted':'refused');p.textContent='Paired projected gain '+fmt(value.gain)+' − paid trial work '+fmt(value.burn)+' = '+fmt(value.netGain)+'. Known candidate reads '+value.coverage+'/32. Diagnostic: '+value.reason+'.';article.append(heading,p);
if(value.fresh16){const f=document.createElement('p'),v=value.fresh16;f.textContent='Next fresh purchases: '+v.correct+'/'+v.steps+' correct; physical net/request '+fmt(v.netPerStep)+'. '+(v.windowComplete?'Complete 16-event window.':'Partial window; it ended at another promotion or run end.')+' Online learning may follow each prediction.';article.append(f);}node.append(article);}
}
function refresh(){selected=current().trace.filter(s=>s.source===Number(source.value));slider.max=Math.max(0,selected.length-1);slider.value=0;plot();show();closures();}
arm.addEventListener('change',refresh);source.addEventListener('change',refresh);slider.addEventListener('input',show);document.getElementById('previous').addEventListener('click',()=>{slider.value=Math.max(0,Number(slider.value)-1);show();});document.getElementById('next').addEventListener('click',()=>{slider.value=Math.min(selected.length-1,Number(slider.value)+1);show();});refresh();
</script></body></html>'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", nargs="?", default="results/seed-confirmation-01")
    parser.add_argument("--output", default=str(HERE / "seed-review.html"))
    args = parser.parse_args()
    path = Path(args.directory); directory = path if path.is_absolute() else HERE / path
    page = build(*load(directory))
    output = Path(args.output)
    output.write_text(page, encoding="utf-8")
    print(f"Wrote {output} ({len(page):,} characters, offline)")


if __name__ == "__main__":
    main()
