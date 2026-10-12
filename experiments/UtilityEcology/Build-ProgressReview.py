"""Build a compact offline review of a completed learning-progress study.

Reads analysis, manifest, completion and first-seed raw runs once. Uses only the
Python standard library; the resulting HTML makes no requests or simulations.
"""
import argparse
import html
import json
from pathlib import Path
import statistics


HERE = Path(__file__).resolve().parent
ERROR = "progress/error-only/upfront"
SIGNED = "progress/signed/upfront"
CYCLE = "progress/cycle-only/upfront"
CHANGE = "progress/change/upfront"
NAMES = (ERROR, SIGNED, CYCLE, CHANGE)
LABELS = {ERROR: "Error only", SIGNED: "Signed progress",
          CYCLE: "Cycle only", CHANGE: "Either direction"}


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def require(condition, message):
    if not condition:
        raise ValueError(message)


def number(value, digits=3):
    return "—" if value is None else f"{value:,.{digits}f}"


def percent(value):
    return "—" if value is None else f"{value:.1%}"


def escaped(value):
    return html.escape(str(value), quote=True)


def interval(value):
    ci = value.get("ci95")
    return "No interval for this pilot" if ci is None else f"[{ci[0]:.3f}, {ci[1]:.3f}]"


def direction_class(value):
    return "good" if value > 0 else "bad" if value < 0 else ""


def load(directory):
    completion = read(directory / "run.json")
    require(completion.get("status") == "complete", "A completed run is required")
    manifest, analysis = read(directory / "manifest.json"), read(directory / "analysis.json")
    require(manifest.get("study") == "learning-progress", "Expected a learning-progress study")
    require(analysis["protocol"] == completion, "Analysis does not match run completion")
    require(set(analysis["arms"]) == set(NAMES), "Expected the four learning-progress arms")
    require(analysis["settings"] == manifest["settings"], "Analysis settings differ")
    require(analysis["audit"]["sourceHashes"] == manifest["sourceHashes"], "Analysis source hashes differ")
    require(all(analysis["audit"][key] for key in
                ("allTaskAccountingChecks", "pairedTapesAndSourceSchedules", "sourceHashesMatch",
                 "progressCountsAndCoverageChecks")), "Analysis integrity audit did not pass")
    require(analysis["seeds"][0] == manifest["seedStart"], "First seed differs")
    seed = analysis["seeds"][0]
    runs = []
    for name in NAMES:
        run = read(directory / f"{seed}-{name.replace('/', '-')}.json")
        require(run["seed"] == seed and run["arm"] == name, "First-seed identity differs")
        require(len(run["trace"]) == manifest["settings"]["phaseLength"] * 4, "First-seed trace length differs")
        require(run["accountedMoney"] == run["externalNet"], "First-seed currency differs")
        metric = next(v for v in analysis["arms"][name]["perSeed"] if v["seed"] == seed)
        require(metric["gross"] == run["gross"] and metric["burned"] == run["burned"], "First-seed analysis totals differ")
        runs.append(run)
    require(len({run["tapeDigest"] for run in runs}) == 1, "First-seed tapes differ")
    return completion, manifest, analysis, runs


def trace_projection(run):
    events = {source: [] for source in range(4)}
    seen = set()
    points, trace = [], []
    for step in run["trace"]:
        source = step["source"]
        p = step.get("progress")
        known = None
        if p and p["version"] > 0:
            complete = len(events[source]) // 16
            pair = [events[source][(complete - 2) * 16:(complete - 1) * 16],
                    events[source][(complete - 1) * 16:complete * 16]]
            known = [sum(block) for block in pair]
            key = (source, p["version"])
            if key not in seen:
                seen.add(key)
                points.append({"source": source, "version": p["version"], "tick": step["tick"],
                               "phase": step["phase"], "error": p["errorProgress256"],
                               "coverage": p["coverageChange256"], "known": known})
        trace.append({key: step[key] for key in
                      ("tick", "phase", "source", "bought", "interpreted", "advice", "action",
                       "gross", "burned", "learningGranted", "refined")} |
                     {"progress": p, "known": known})
        if step["bought"] and step["action"] in (-1, 1) and step["gross"] != 0:
            events[source].append(step["advice"] in (-1, 1))
    return {"seed": run["seed"], "trace": trace, "points": points}


def build(completion, manifest, analysis, runs):
    arms, contrasts = analysis["arms"], analysis["pairedContrasts"]
    n = len(analysis["seeds"])
    mean_change = contrasts["signed minus error"]["mean"]
    finding = ("higher" if mean_change > 0 else "lower" if mean_change < 0 else "equal")
    group, tradeoffs, grants, seed_rows, options = [], [], [], [], []
    for name in NAMES:
        a, p = arms[name], arms[name]["progress"]
        group.append(f"<tr><th scope='row'>{LABELS[name]}</th><td>{number(a['netPerStep']['mean'])}</td>"
                     f"<td>{number(a['burnPerStep']['mean'])}</td><td>{percent(a['coverage'])}</td>"
                     f"<td>{p['fundedGrants']:,}</td><td>{a['updates']:,}</td></tr>")
        noise, new, rare = a["pureNoise"], a["newFinalUtility"], a["rareReturnFirst16Requests"]
        delays = [v["firstWindow"]["localRequestsThroughWindow"] for v in a.get("commonAdaptation", [])
                  if v["firstWindow"] is not None]
        adaptation = number(statistics.median(delays), 0) if delays else "Not reached"
        reached = f" ({len(delays)}/{n})" if delays else ""
        tradeoffs.append(f"<tr><th scope='row'>{LABELS[name]}</th>"
                         f"<td>{number(noise['burnPerStep']['mean'])}<small>{noise['progress']['fundedGrants']} grants</small></td>"
                         f"<td>{adaptation}{reached}</td><td>{percent(rare['coverage'])}</td>"
                         f"<td class='{direction_class(new['netPerStep']['mean'])}'>{number(new['netPerStep']['mean'])}"
                         f"<small>{new['profitableSeeds']}/{n} profitable seeds</small></td>"
                         f"<td>{percent(new['coverage'])}</td></tr>")
        grants.append(f"<tr><th scope='row'>{LABELS[name]}</th><td>{p['errorRequests']:,}</td>"
                      f"<td>{p['gateRequests']:,}</td><td>{p['fundedGrants']:,}</td>"
                      f"<td>{p['requestedNotFunded']:,}</td><td>{p['errorRequestsSuppressed']:,}</td></tr>")
        options.append(f"<option value='{escaped(name)}'{' selected' if name == SIGNED else ''}>{LABELS[name]}</option>")
    for seed_index, seed in enumerate(analysis["seeds"]):
        seed_rows.append("<tr><th scope='row'>" + str(seed) + "</th>" +
                         "".join("<td>" + number(arms[name]["netPerStep"]["values"][seed_index]) + "</td>"
                                 for name in NAMES) + "</tr>")
    contrast_cards = []
    for key in ("signed minus error", "signed minus cycle", "change minus signed"):
        value = contrasts[key]
        label = "Primary comparison" if value["primary"] else "Diagnostic comparison"
        contrast_cards.append(f"<article class='card'><small>{label}</small><h3>{escaped(key.capitalize())}</h3>"
                              f"<strong class='{direction_class(value['mean'])}'>{value['mean']:+.3f}</strong>"
                              f"<p>Net per task change<br><span class='muted'>{interval(value)}</span></p>"
                              f"<p>{value['positiveSeeds']}/{n} seeds favored the left arm.</p></article>")
    retest = sorted({arm["retestPeriod"] for arm in manifest["arms"]})
    retest_text = ", ".join(str(value) for value in retest)
    traces = {run["arm"]: trace_projection(run) for run in runs}
    payload = json.dumps({"traces": traces, "labels": LABELS}, separators=(",", ":"), allow_nan=False).replace("</", "<\\/")
    replacements = {
        "__DATA__": payload, "__GROUP__": "".join(group), "__TRADEOFFS__": "".join(tradeoffs),
        "__GRANTS__": "".join(grants), "__CONTRASTS__": "".join(contrast_cards),
        "__SEEDS__": "".join(seed_rows), "__OPTIONS__": "".join(options),
        "__N__": str(n), "__TASKS__": f"{completion['trials']:,}", "__SEED__": str(runs[0]["seed"]),
        "__FINDING__": finding, "__CHANGE__": f"{mean_change:+.3f}", "__RETEST__": retest_text,
        "__INTERVALNOTE__": ("Intervals describe eight independent paired seed changes using unadjusted Student t(7). They are not simultaneous significance claims."
                             if n == 8 else "This is a development pilot. No confidence intervals are shown; these results are not fresh confirmation evidence.")
    }
    page = r"""<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<meta name="color-scheme" content="dark light"><title>Gojo · Learning progress review</title>
<style>
:root{color-scheme:dark;--bg:#102322;--panel:#193330;--line:#37514c;--text:#eaf4ef;--muted:#b1c7bf;--accent:#8ad5b3;--bad:#f4b7b1;--gold:#edce87}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font:16px/1.5 system-ui,sans-serif}
main{max-width:1050px;margin:auto;padding:28px 22px 60px}h1{font-size:clamp(1.9rem,5vw,3rem);line-height:1.1;margin:10px 0}
h2{font-size:1.3rem;margin:30px 0 12px}h3{font-size:1rem;margin:10px 0}p{max-width:76ch}a{color:var(--accent)}
small,.muted{color:var(--muted)}small{display:block}.eyebrow{letter-spacing:.08em;text-transform:uppercase;font-size:.8rem}
.cards{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.card,.notice,.detail{background:var(--panel);border:1px solid var(--line);border-radius:12px;padding:16px}
.card strong{font-size:1.9rem;line-height:1.2}.card p{margin:10px 0 0}.good{color:var(--accent)}.bad{color:var(--bad)}
.notice{border-left:4px solid var(--gold);margin:20px 0}.scroll{overflow:auto;border:1px solid var(--line);border-radius:10px}
table{border-collapse:collapse;width:100%;font-size:.9rem}caption{padding:10px;text-align:left;background:var(--panel);color:var(--muted)}
th,td{padding:10px 12px;text-align:right;vertical-align:top;border-bottom:1px solid var(--line);white-space:nowrap}th:first-child{text-align:left}
thead{background:var(--panel)}tbody tr:last-child>*{border-bottom:0}.controls{display:flex;gap:12px;flex-wrap:wrap;margin:14px 0}
label{display:grid;gap:4px;font-size:.9rem}select,button{font:inherit;color:var(--text);background:var(--panel);border:1px solid var(--line);padding:9px;border-radius:7px}
button{cursor:pointer}button:disabled{opacity:.5;cursor:default}input[type=range]{width:min(100%,500px);accent-color:var(--accent)}
:focus-visible{outline:3px solid var(--gold);outline-offset:3px}summary{cursor:pointer;padding:12px 0;color:var(--accent)}
.plots{display:grid;grid-template-columns:1fr 1fr;gap:14px}svg{width:100%;height:auto;display:block;background:var(--panel);border:1px solid var(--line);border-radius:10px}
.detailgrid{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}.detailgrid span{font-size:1.05rem}
.traceactions{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-top:12px}
footer{margin-top:30px;color:var(--muted);font-size:.9rem}noscript{display:block;margin:20px 0;color:var(--gold)}
@media(max-width:650px){main{padding:20px 14px 40px}.cards,.plots{grid-template-columns:1fr}.detailgrid{grid-template-columns:repeat(2,1fr)}
th,td{padding:9px}.card strong{font-size:1.7rem}.controls label{width:100%}select{width:100%}}
</style></head><body><main>
<header><div class="eyebrow">Gojo · Experiment review</div><h1>Error and learning progress</h1>
<p>__N__ paired seeds · __TASKS__ world tasks · four fixed controllers</p>
<p>The signed-progress controller has <strong>__FINDING__ observed mean net return</strong> than error alone:
<strong>__CHANGE__ resource units per task</strong>. Both primary comparisons and their seed variation are shown below.</p>
<nav aria-label="Experiment documents"><a href="PROGRESS-REPORT.md">Full report</a> · <a href="PROGRESS-PLAN.md">Protocol</a> · <a href="README.md">Mechanism</a></nav></header>
<div class="notice"><strong>What is being tested</strong><p>All four arms observe the same kind of local evidence.
The new gate is fixed and authored. It uses recent progress to decide when a finite investigation may continue.
Currency prices are abstract; the passive monitor is included in the existing one-unit inspection price.</p></div>
<section aria-labelledby="group"><h2 id="group">Group return and paid work</h2>
<div class="scroll" tabindex="0" role="region" aria-label="Group result table"><table>
<caption>Means per all world tasks; paid grants and updates are totals across all seeds.</caption>
<thead><tr><th scope="col">Arm</th><th scope="col">Net/task</th><th scope="col">Burn/task</th><th scope="col">Action coverage</th><th scope="col">Paid grants</th><th scope="col">Updates</th></tr></thead>
<tbody>__GROUP__</tbody></table></div></section>
<section aria-labelledby="contrasts"><h2 id="contrasts">Paired differences</h2><div class="cards">__CONTRASTS__</div>
<p class="muted">__INTERVALNOTE__</p>
<details><summary>Net return for each seed</summary><div class="scroll" tabindex="0" role="region" aria-label="Per-seed returns"><table>
<thead><tr><th>Seed</th><th>Error only</th><th>Signed progress</th><th>Cycle only</th><th>Either direction</th></tr></thead>
<tbody>__SEEDS__</tbody></table></div></details></section>
<section aria-labelledby="tradeoffs"><h2 id="tradeoffs">Noise savings and useful recovery</h2>
<div class="scroll" tabindex="0" role="region" aria-label="Noise and recovery comparison"><table>
<caption>Noise is source 2 throughout. New utility is source 3 in the final phase.</caption>
<thead><tr><th scope="col">Arm</th><th scope="col">Noise burn/task</th><th scope="col">Common recovery</th>
<th scope="col">Rare return coverage</th><th scope="col">New utility net/task</th><th scope="col">New utility coverage</th></tr></thead>
<tbody>__TRADEOFFS__</tbody></table></div>
<p class="muted">Common recovery is the median number of common requests through the first error-free 16-request window after its change;
the seed count shows how often that window was reached. Rare return coverage uses the first 16 returning world requests.
A first successful window does not establish permanent mastery. Attributed source costs are not causal contribution scores.</p></section>
<details><summary>Requested permissions and funding</summary><div class="scroll" tabindex="0" role="region" aria-label="Permission and funding counts"><table>
<caption>An opened episode, a paid permission and an actual model update are different events.</caption>
<thead><tr><th>Arm</th><th>Error requests</th><th>Gate requests</th><th>Paid grants</th><th>Unfunded requests</th><th>Suppressed requests</th></tr></thead>
<tbody>__GRANTS__</tbody></table></div></details>
<section aria-labelledby="trace"><h2 id="trace">Inspect one actual run</h2>
<p>First seed <strong>__SEED__</strong>. Source names and phase labels here are analyst descriptions; services receive neither.
The decision snapshot reads previous completed blocks and current funding, before this step's feedback.</p>
<noscript>Group comparisons above work without JavaScript. Enable it only to use the local trace selector and charts.</noscript>
<div class="controls"><label>Controller<select id="arm">__OPTIONS__</select></label>
<label>Source<select id="source"><option value="0">0 · Common</option><option value="1">1 · Rare returning</option>
<option value="2">2 · Pure noise</option><option value="3">3 · Initially noisy, later useful</option></select></label>
<label>Comparison chart<select id="range"><option value="first">First 16 versions</option><option value="last">Last 16 versions</option></select></label></div>
<p id="chartnote" class="muted"></p><div class="plots"><div id="errorplot"></div><div id="coverageplot"></div></div>
<p class="muted">Each point is one completed comparison version. Missing error points mean <strong>open evidence</strong>.
Error change 128 means 25 points less mismatch; coverage change 128 means 50 points more supported predictions.
Adjacent versions can share a block and are not independent tests.</p>
<label for="event">Selected source event <input id="event" type="range" min="0" max="0" value="0"></label>
<div class="traceactions"><button id="previous" type="button">Previous</button><button id="next" type="button">Next</button><span id="eventlabel" class="muted"></span></div>
<div id="detail" class="detail detailgrid" aria-live="polite"></div>
<p class="muted">Open advice is not an estimated zero. A zero progress reading means a supported comparison found no change;
an open reading means that comparison did not establish a number. An unpurchased event has no new paid decision.</p></section>
<section aria-labelledby="rules"><h2 id="rules">The permission rules</h2>
<p>Cold start permits at most 16 actual paid grants. A fresh qualifying block comparison can open one continuation of at most four grants.
The signed arm admits improving error with nonfalling coverage, or a sufficient gain in supported answers.
The diagnostic arm also treats worsening error as bounded interest. The cycle arm withdraws those progress signals.</p>
<p>Finite four-grant retests occur on the configured __RETEST__-inspection cycle; ordinary settled low-error learning keeps its existing schedule.
Models and observers can survive cooldown. A service still needs funded acquisition and learning; progress does not mint currency.</p>
<p class="muted">Both blocks contain 16 admitted bought physical outcomes, including open predictions. Numeric error requires at least eight known losses in each block.
The error observation is independent of whether permission was granted. A signal that remains noisy can create a fortunate positive trend;
bounded work and the noise control test its practical consequences.</p></section>
<footer>Read-only local review. No network requests or live training. The analyzer checked per-step currency, paired tapes,
source hashes, progress window counts and saved native graph behavior within the declared fixture.
<a href="PROGRESS-REPORT.md">The report</a> records failures and claim limits.</footer>
</main><script id="data" type="application/json">__DATA__</script><script>
"use strict";
const data=JSON.parse(document.getElementById("data").textContent);
const arm=document.getElementById("arm"), source=document.getElementById("source"), range=document.getElementById("range");
const event=document.getElementById("event"), detail=document.getElementById("detail");
const previous=document.getElementById("previous"), next=document.getElementById("next");
let selected=[];
function text(id,value){document.getElementById(id).textContent=value;}
function numeric(value){return value===null||value===undefined?"open":String(value);}
function node(tag,attrs,parent,content){const x=document.createElementNS("http://www.w3.org/2000/svg",tag);
Object.entries(attrs||{}).forEach(function(entry){x.setAttribute(entry[0],String(entry[1]));});
if(content!==undefined)x.textContent=content;if(parent)parent.appendChild(x);return x;}
function plot(id,points,key,title,bound,color){
const host=document.getElementById(id);host.replaceChildren();
const svg=node("svg",{viewBox:"0 0 480 195",role:"img","aria-label":title+": missing points are open readings."},host);
node("title",{},svg,title);node("desc",{},svg,"One point per comparison version; gaps retain unknown values.");
const left=48,right=455,top=38,bottom=160,mid=(top+bottom)/2;
node("text",{x:15,y:21,fill:"#eaf4ef","font-size":13},svg,title);
const y=function(v){return mid-v/bound*(bottom-top)/2;};
[-bound,0,bound].forEach(function(v){node("line",{x1:left,x2:right,y1:y(v),y2:y(v),stroke:"#37514c"},svg);
node("text",{x:8,y:y(v)+4,fill:"#b1c7bf","font-size":11},svg,String(v));});
[-128,128].forEach(function(v){node("line",{x1:left,x2:right,y1:y(v),y2:y(v),stroke:"#edce87","stroke-dasharray":"3 5",opacity:.45},svg);});
if(!points.length){node("text",{x:65,y:105,fill:"#b1c7bf","font-size":13},svg,"No completed pair read in this source");return;}
let segment=[];function flush(){if(segment.length>1)node("polyline",{points:segment.join(" "),fill:"none",stroke:color,"stroke-width":2},svg);segment=[];}
points.forEach(function(p,i){const x=points.length===1?(left+right)/2:left+i/(points.length-1)*(right-left);
const v=p[key];if(v===null){flush();node("text",{x:x,y:182,"text-anchor":"middle",fill:"#b1c7bf","font-size":10},svg,"?");}
else{const yy=y(v);segment.push(x+","+yy);const dot=node("circle",{cx:x,cy:yy,r:3.5,fill:color},svg);
node("title",{},dot,"Version "+p.version+", tick "+p.tick+", phase "+p.phase+": "+v+"; known "+p.known[0]+"/16 then "+p.known[1]+"/16");}
if(i===0||i===points.length-1)node("text",{x:x,y:192,"text-anchor":"middle",fill:"#b1c7bf","font-size":10},svg,"v"+p.version);
});flush();}
function detailItem(label,value){const box=document.createElement("div"), name=document.createElement("small"), content=document.createElement("span");
name.textContent=label;content.textContent=value;box.append(name,content);detail.appendChild(box);}
function renderEvent(){
detail.replaceChildren();const i=Number(event.value),s=selected[i];if(!s)return;
const p=s.progress;previous.disabled=i===0;next.disabled=i===selected.length-1;
text("eventlabel",(i+1)+" / "+selected.length+" source events");
detailItem("World tick / analyst phase",s.tick+" / "+s.phase);
detailItem("Purchased / usable advice",(s.bought?"yes":"no")+" / "+(s.interpreted?"yes":"no"));
detailItem("Advice → action",numeric(s.advice)+" → "+s.action);
detailItem("Physical gross / burned work",s.gross+" / "+s.burned);
detailItem("Prior comparison",p?"version "+p.version+", "+p.observations+" observations":"no decision");
detailItem("Prior signed error / coverage",p?numeric(p.errorProgress256)+" / "+numeric(p.coverageChange256):"no decision");
detailItem("Known in completed blocks",s.known?s.known[0]+"/16 → "+s.known[1]+"/16":"no complete pair");
detailItem("Current error request / gate request",p?(p.errorRequested?"yes":"no")+" / "+(p.gateRequested?"yes":"no"):"no decision");
detailItem("Current paid learning",s.learningGranted?"granted":"not granted");
detailItem("Trigger / remaining episode",p?p.trigger+" / "+p.episodeRemaining:"no decision");
detailItem("Opened positive / change / coverage",p?p.positiveEpisodes+" / "+p.changeEpisodes+" / "+p.coverageEpisodes:"no decision");
detailItem("Opened retests / suppressed requests",p?p.retestEpisodes+" / "+p.suppressedRequests:"no decision");}
function render(reset){
const run=data.traces[arm.value],id=Number(source.value);
selected=run.trace.filter(function(s){return s.source===id;});event.max=Math.max(0,selected.length-1);
if(reset)event.value=0;else event.value=Math.min(Number(event.value),Number(event.max));
let points=run.points.filter(function(p){return p.source===id;});
const total=points.length;points=range.value==="first"?points.slice(0,16):points.slice(-16);
text("chartnote","Seed "+run.seed+" · "+data.labels[arm.value]+" · "+total+" versions read; displaying "+points.length+
" ("+points.filter(function(p){return p.error!==null;}).length+" numeric error comparisons).");
plot("errorplot",points,"error","Signed error change · native 256 scale",512,"#8ad5b3");
plot("coverageplot",points,"coverage","Coverage change · native 256 scale",256,"#edce87");renderEvent();}
arm.addEventListener("change",function(){render(true);});source.addEventListener("change",function(){render(true);});
range.addEventListener("change",function(){render(false);});event.addEventListener("input",renderEvent);
previous.addEventListener("click",function(){event.value=Math.max(0,Number(event.value)-1);renderEvent();});
next.addEventListener("click",function(){event.value=Math.min(Number(event.max),Number(event.value)+1);renderEvent();});
render(true);
</script></body></html>"""
    for key, value in replacements.items():
        page = page.replace(key, value)
    return page


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", nargs="?", default="results/progress-confirmation-01",
                        help="Relative paths resolve beside this script")
    parser.add_argument("--output", type=Path, default=HERE / "progress-review.html")
    args = parser.parse_args()
    directory = Path(args.directory)
    if not directory.is_absolute():
        directory = HERE / directory
    completion, manifest, analysis, runs = load(directory)
    page = build(completion, manifest, analysis, runs)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(page, encoding="utf-8")
    print(f"Built {args.output.resolve()} from {len(analysis['seeds'])} completed paired seeds.")


if __name__ == "__main__":
    main()
