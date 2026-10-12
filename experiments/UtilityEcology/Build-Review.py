"""Build an offline review from a completed UtilityEcology run.

Uses Python's standard library. Reads result files once, embeds only the data
needed by the review, and never changes experimental results or native graphs.
"""
from __future__ import annotations

import argparse
import html
import json
from pathlib import Path
import re
import statistics


HERE = Path(__file__).resolve().parent
FULL = "opportunity/success-paid"
SOURCE_NAMES = ("Common", "Rare valuable", "Noisy", "Future opportunity")
PHASE_NAMES = (
    "0 · initial demand", "1 · common law changes", "2 · rare demand stays absent", "3 · return and new utility"
)


def read_json(path: Path):
    with path.open(encoding="utf-8-sig") as stream:
        return json.load(stream)


def number(value, digits=2):
    return "—" if value is None else f"{value:,.{digits}f}"


def percent(value):
    return "—" if value is None else f"{100 * value:.1f}%"


def escaped(value):
    return html.escape(str(value), quote=True)


def describe(values):
    values = list(values)
    if not values:
        return {"mean": None, "low": None, "high": None, "n": 0}
    return {"mean": statistics.fmean(values), "low": min(values), "high": max(values), "n": len(values)}


def cell_class(value):
    return "positive" if value is not None and value > 0 else "negative" if value is not None and value < 0 else ""


def row_summary(rows):
    rows = list(rows)
    result = {}
    for phase in range(4):
        for source in range(4):
            selected = [row for row in rows if row["phase"] == phase and row["source"] == source]
            result[f"{phase}:{source}"] = {
                "net": describe((row["gross"] - row["burned"]) / row["steps"] for row in selected),
                "correct": describe(row["correct"] / row["steps"] for row in selected),
                "covered": describe(row["answered"] / row["steps"] for row in selected),
                "interpreted": describe(row["interpreted"] / row["steps"] for row in selected),
                "steps": describe(row["steps"] for row in selected),
                "updates": describe(row["learningUpdates"] for row in selected),
                "refinements": describe(row["refinements"] for row in selected),
            }
    return result


def compare(runs, first, second, phase=None, source=None):
    by_key = {(run["arm"], run["seed"]): run for run in runs}
    values = []
    for arm, seed in by_key:
        if arm != first or (second, seed) not in by_key:
            continue
        a, b = by_key[first, seed], by_key[second, seed]
        if phase is None:
            values.append(a["netPerStep"] - b["netPerStep"])
        else:
            ar = next((row for row in a["rows"] if row["phase"] == phase and row["source"] == source), None)
            br = next((row for row in b["rows"] if row["phase"] == phase and row["source"] == source), None)
            if ar is not None and br is not None:
                values.append((ar["gross"] - ar["burned"]) / ar["steps"] - (br["gross"] - br["burned"]) / br["steps"])
    return describe(values)


def phase_table(summary):
    lines = []
    for phase in range(4):
        for source, label in enumerate(SOURCE_NAMES):
            value = summary[f"{phase}:{source}"]
            if not value["net"]["n"]:
                lines.append(f'<tr><th scope="row">{escaped(PHASE_NAMES[phase])}</th><td>{escaped(label)}</td><td colspan="6" class="muted">No requests</td></tr>')
                continue
            net = value["net"]
            lines.append(f'<tr><th scope="row">{escaped(PHASE_NAMES[phase])}</th><td>{escaped(label)}</td>'
                         f'<td class="{cell_class(net["mean"])}">{number(net["mean"])}</td>'
                         f'<td>{number(net["low"])} to {number(net["high"])}</td>'
                         f'<td>{percent(value["correct"]["mean"])}</td><td>{percent(value["covered"]["mean"])}</td>'
                         f'<td>{percent(value["interpreted"]["mean"])}</td><td>{number(value["updates"]["mean"], 1)}</td></tr>')
    return "".join(lines)


def load_completed(directory: Path):
    status = read_json(directory / "run.json")
    if status.get("status") != "complete":
        raise ValueError("Build the review only after run.json reports complete.")
    manifest = read_json(directory / "manifest.json")
    arms = [arm["name"] for arm in manifest["arms"]]
    first_seed = manifest["seedStart"]
    expected = {(name, seed) for name in arms for seed in range(first_seed, first_seed + manifest["seeds"])}
    runs = []
    trace = None
    for path in sorted(directory.glob("*.json")):
        if not re.fullmatch(r"\d+-.+\.json", path.name):
            continue
        run = read_json(path)
        key = (run.get("arm"), run.get("seed"))
        if key not in expected:
            continue
        steps = sum(row["steps"] for row in run["rows"])
        if steps != 4 * manifest["settings"]["phaseLength"] or len(run["trace"]) != steps:
            raise ValueError(f"Incomplete row/trace data: {path.name}")
        if run["accountedMoney"] != run["externalNet"]:
            raise ValueError(f"Monetary mismatch: {path.name}")
        if run["arm"] == FULL and run["seed"] == first_seed:
            trace = run["trace"]
        runs.append({key: value for key, value in run.items() if key in (
            "arm", "seed", "rows", "gross", "burned", "netPerStep", "tapeDigest", "accountedMoney", "externalNet"
        )})
    keys = [(run["arm"], run["seed"]) for run in runs]
    if len(keys) != len(set(keys)) or set(keys) != expected:
        raise ValueError("Expected one completed result for every declared arm and seed.")
    if trace is None:
        raise ValueError(f"Missing first-seed {FULL} trace.")
    for seed in range(first_seed, first_seed + manifest["seeds"]):
        if len({run["tapeDigest"] for run in runs if run["seed"] == seed}) != 1:
            raise ValueError(f"Arm tapes differ within seed {seed}.")
    archive_path = directory / "archive-check.json"
    archives = read_json(archive_path) if archive_path.exists() else []
    return status, manifest, runs, trace, archives


def build(directory: Path, output: Path):
    status, manifest, runs, trace, archives = load_completed(directory)
    names = [arm["name"] for arm in manifest["arms"]]
    summaries = {}
    group_rows = []
    for name in names:
        selected = [run for run in runs if run["arm"] == name]
        summary = {
            "net": describe(run["netPerStep"] for run in selected),
            "gross": describe(run["gross"] / sum(row["steps"] for row in run["rows"]) for run in selected),
            "burned": describe(run["burned"] / sum(row["steps"] for row in run["rows"]) for run in selected),
            "correct": describe(sum(row["correct"] for row in run["rows"]) / sum(row["steps"] for row in run["rows"]) for run in selected),
            "covered": describe(sum(row["answered"] for row in run["rows"]) / sum(row["steps"] for row in run["rows"]) for run in selected),
            "updates": describe(sum(row["learningUpdates"] for row in run["rows"]) for run in selected),
            "phases": row_summary(row for run in selected for row in run["rows"]),
        }
        summaries[name] = summary
        net = summary["net"]
        group_rows.append(f'<tr><th scope="row">{escaped(name)}</th><td class="{cell_class(net["mean"])}">{number(net["mean"])}</td>'
                          f'<td>{number(net["low"])} to {number(net["high"])}</td><td>{number(summary["gross"]["mean"])}</td>'
                          f'<td>{number(summary["burned"]["mean"])}</td><td>{percent(summary["correct"]["mean"])}</td>'
                          f'<td>{percent(summary["covered"]["mean"])}</td><td>{number(summary["updates"]["mean"], 0)}</td></tr>')
    full = summaries[FULL]
    retention = compare(runs, FULL, FULL + "/use-reset", 3, 1)
    gate = compare(runs, "opportunity/upfront", "matched/error/upfront")
    future = full["phases"]["3:3"]["net"]
    future_title = "New utility remains costly in the full arm" if future["mean"] < 0 else "New utility has positive attributed return in the full arm"
    future_seeds = [row for run in runs if run["arm"] == FULL for row in run["rows"] if row["phase"] == 3 and row["source"] == 3]
    future_positive = sum(row["gross"] > row["burned"] for row in future_seeds)
    future_body = ("The full arm has negative mean return on the newly useful source. Periodic opportunity tests did not make this source profitable within the supplied budget."
                   if future["mean"] < 0 else "The full arm has positive mean return on the newly useful source. Compare seeds and controls before attributing that improvement to a particular mechanism.")
    retained_rows = []
    for name in (FULL, FULL + "/use-reset", FULL + "/no-renewal"):
        for phase in (0, 3):
            phase = int(phase)
            value = summaries[name]["phases"][f"{phase}:1"]
            retained_rows.append(f'<tr><th scope="row">{escaped(name)}</th><td>{phase}</td>'
                                 f'<td class="{cell_class(value["net"]["mean"])}">{number(value["net"]["mean"])}</td>'
                                 f'<td>{number(value["net"]["low"])} to {number(value["net"]["high"])}</td>'
                                 f'<td>{percent(value["correct"]["mean"])}</td><td>{percent(value["covered"]["mean"])}</td></tr>')
    future_rows = []
    for name in names:
        value = summaries[name]["phases"]["3:3"]
        future_rows.append(f'<tr><th scope="row">{escaped(name)}</th><td class="{cell_class(value["net"]["mean"])}">{number(value["net"]["mean"])}</td>'
                           f'<td>{number(value["net"]["low"])} to {number(value["net"]["high"])}</td><td>{percent(value["correct"]["mean"])}</td>'
                           f'<td>{number(value["updates"]["mean"], 1)}</td><td>{number(value["refinements"]["mean"], 1)}</td></tr>')
    seed_rows = []
    for seed in range(manifest["seedStart"], manifest["seedStart"] + manifest["seeds"]):
        values = {run["arm"]: run["netPerStep"] for run in runs if run["seed"] == seed}
        seed_rows.append(f'<tr><th scope="row">{seed}</th>' + "".join(f'<td class="{cell_class(values[name])}">{number(values[name])}</td>' for name in names) + '</tr>')
    controls = []
    for arm in manifest["arms"]:
        period = arm.get("buyerProbePeriod") or (8 if arm["opportunity"] else 32)
        controls.append(f'<tr><th scope="row">{escaped(arm["name"])}</th><td>{"on" if arm["learning"] else "off"}</td>'
                        f'<td>{"always" if arm["always"] else "error + opportunity" if arm["opportunity"] else "error"}</td>'
                        f'<td>{period}</td><td>{arm["trialAllowance"]}</td><td>{"success" if arm["successPaid"] else "upfront"}</td>'
                        f'<td>{"yes" if arm["renew"] else "no"}</td><td>{"reset" if arm["resetIdle"] else "retain"}</td>'
                        f'<td>{"delivered" if arm["communicate"] else "withdrawn"}</td></tr>')
    archive_ok = all(item.get("signatureMatched") and item.get("monetaryMatched") and item.get("aliasesMatched")
                     and item.get("behavior", {}).get("matched") and item.get("behavior", {}).get("continuedMoneyConserved") for item in archives)
    payload = {
        "runs": runs, "summaries": summaries, "trace": trace, "names": names,
        "sources": SOURCE_NAMES, "phases": PHASE_NAMES, "firstSeed": manifest["seedStart"],
    }
    encoded = json.dumps(payload, separators=(",", ":"), ensure_ascii=True).replace("</", "<\\/")
    replacements = {
        "__DATA__": encoded,
        "__META__": f'{manifest["seeds"]} fresh seeds · {len(names)} arms · {status["trials"]:,} complete world steps',
        "__RUNTIME__": number(status["elapsedSeconds"] / 60, 1),
        "__NET__": number(full["net"]["mean"]), "__COST__": number(full["burned"]["mean"]),
        "__COVERAGE__": percent(full["covered"]["mean"]), "__RETENTION__": number(retention["mean"]),
        "__GATEDELTA__": number(gate["mean"]), "__GATERANGE__": f'{number(gate["low"])} to {number(gate["high"])}',
        "__FUTURETITLE__": future_title, "__FUTUREBODY__": future_body, "__FUTURENET__": number(future["mean"]),
        "__FUTURECOUNT__": f'{future_positive} of {len(future_seeds)} full-arm seeds',
        "__GROUPROWS__": "".join(group_rows), "__PHASEROWS__": phase_table(full["phases"]),
        "__RETENTIONROWS__": "".join(retained_rows), "__FUTUREROWS__": "".join(future_rows),
        "__SEEDHEADERS__": "".join(f'<th scope="col">{escaped(name)}</th>' for name in names),
        "__SEEDROWS__": "".join(seed_rows), "__CONTROLROWS__": "".join(controls),
        "__ARCHIVES__": f'{len(archives)} saved first-seed graphs; {"all declared checks passed" if archives and archive_ok else "inspect archive-check.json for verification status"}',
        "__PHASELEN__": str(manifest["settings"]["phaseLength"]), "__TRACESEED__": str(manifest["seedStart"]),
    }
    document = TEMPLATE
    for key, value in replacements.items():
        document = document.replace(key, value)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(document, encoding="utf-8")
    print(f"Review: {output}")


TEMPLATE = r'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Utility ecology · Gojo experiment review</title>
<style>
:root{color-scheme:dark;--bg:#101f21;--panel:#172b2d;--line:#345052;--ink:#edf4ec;--muted:#bdceca;--green:#9cd8b0;--amber:#efcc86;--rose:#f3a6a1}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.55 system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif}main{max-width:1180px;margin:auto;padding:32px 24px 70px}h1,h2,h3{line-height:1.17;font-weight:650;letter-spacing:-.025em}h1{font-size:clamp(2rem,6vw,3.6rem);max-width:850px;margin:10px 0 16px}h2{font-size:1.65rem;margin:0 0 16px}h3{font-size:1.15rem;margin:0 0 10px}p{max-width:870px;margin:10px 0 18px}a{color:var(--green);text-underline-offset:3px}small,.muted{color:var(--muted)}.eyebrow{text-transform:uppercase;letter-spacing:.14em;color:var(--amber);font-size:.78rem;font-weight:650}.meta{font-size:.95rem;color:var(--muted)}section{margin:32px 0;padding:24px;background:var(--panel);border:1px solid var(--line);border-radius:16px}.notice{border-left:4px solid var(--amber);padding:10px 16px;background:#253233}.warning{border-left-color:var(--rose)}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px}.stage{padding:18px;border:1px solid var(--line);border-radius:12px}.stage .index{font-size:.8rem;color:var(--amber)}.stage p{font-size:.94rem;margin-bottom:0}.metrics{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:14px;margin:24px 0}.metric{border-top:2px solid var(--green);padding:14px 12px;background:var(--panel);border-radius:0 0 10px 10px}.metric strong{display:block;font-size:1.95rem;line-height:1.3;font-variant-numeric:tabular-nums}.metric span{font-size:.85rem;color:var(--muted)}.scroll{overflow:auto;border:1px solid var(--line);border-radius:10px}.scroll:focus{outline:2px solid var(--amber)}table{border-collapse:collapse;width:100%;font-size:.86rem;white-space:nowrap;font-variant-numeric:tabular-nums}caption{text-align:left;padding:14px;color:var(--muted);white-space:normal}th,td{padding:10px 12px;text-align:right;border-bottom:1px solid var(--line)}th:first-child,td:first-child{text-align:left}th{font-weight:600}thead{background:#233b3d}tbody tr:hover{background:#23383a}.positive{color:var(--green);font-weight:600}.negative{color:var(--rose);font-weight:600}.filters{display:flex;gap:14px;flex-wrap:wrap;align-items:end;margin-bottom:16px}label{font-size:.85rem;color:var(--muted);display:flex;flex-direction:column;gap:5px}select,button{background:#233b3d;color:var(--ink);border:1px solid #678180;border-radius:6px;padding:9px 12px;font:inherit}select{max-width:100%}button{cursor:pointer}button:disabled{opacity:.45;cursor:default}select:focus,button:focus,summary:focus{outline:2px solid var(--amber);outline-offset:3px}details{margin:18px 0}summary{cursor:pointer;font-weight:600;padding:8px 0}ul{padding-left:24px}li{margin:8px 0}.stories{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px;margin:16px 0}.story{border:1px solid var(--line);border-radius:10px;padding:15px}.story .tick{font-size:.8rem;color:var(--amber)}.story p{font-size:.9rem;margin:8px 0 0}.pager{display:flex;gap:12px;align-items:center;flex-wrap:wrap;margin:14px 0}.longlabel{white-space:normal;max-width:380px}noscript p{color:var(--amber)}footer{color:var(--muted);font-size:.85rem}.sr-only{position:absolute;left:-10000px;width:1px;height:1px;overflow:hidden}
@media(max-width:700px){main{padding:22px 12px 45px}section{padding:17px 13px;margin:23px 0}.grid{grid-template-columns:1fr}.metrics{grid-template-columns:repeat(2,minmax(0,1fr))}.metric strong{font-size:1.65rem}.stories{grid-template-columns:1fr}h2{font-size:1.4rem}.filters>label{width:100%}select{width:100%}th,td{padding:9px}}
</style></head><body><main>
<header><div class="eyebrow">Gojo · Utility ecology</div><h1>A small market for useful distinctions</h1>
<p>Empty local models learn from physical consequences, publish condensed direction advice, and spend finite resources on further work. This review shows what helped, what remained costly, and how the controls differ.</p>
<p class="meta">__META__ · __RUNTIME__ minutes · no paid Jev calls</p>
<p><a href="REPORT.md">Full report</a> · <a href="README.md">Mechanism and run instructions</a></p></header>
<div class="notice"><strong>A bounded mechanism trial.</strong> Costs are abstract resource units. The supplied buyer, service grammar and prices are experimental contracts; the study does not establish an optimal market or open-ended invention.</div>
<div class="metrics" aria-label="Full opportunity, success-paid arm means">
<div class="metric"><strong>__NET__</strong><span>full-arm net resource per world step</span></div>
<div class="metric"><strong>__COST__</strong><span>burned work per world step</span></div>
<div class="metric"><strong>__COVERAGE__</strong><span>steps with non-hold actions</span></div>
<div class="metric"><strong>__RETENTION__</strong><span>returning rare-source net gain over use-reset</span></div></div>
<section aria-labelledby="stages"><h2 id="stages">From open interpretation to a useful distinction</h2>
<div class="grid"><article class="stage"><div class="index">1 · Open</div><h3>No supported answer</h3><p>Samples expose physical presence with open magnitude. Initially no direction is supported; fallback actions can still generate learning evidence.</p></article>
<article class="stage"><div class="index">2 · Presence</div><h3>One response may suffice</h3><p>A source can learn a useful direction without comparing magnitudes. Stable common demand initially permits this simple publication.</p></article>
<article class="stage"><div class="index">3 · Ordinal</div><h3>Pay for needed differences</h3><p>Pressure can sponsor a bounded model of opaque anchors and order. Refinement can remain open or fail; a larger model does not guarantee utility.</p></article></div>
<p class="muted">Four service scaffolds, orientation and binary opposed actions are supplied. A native product of issued direction and actual consequence sign derives the successful direction. The world never supplies that label through a learner port.</p></section>
<section aria-labelledby="allarms"><h2 id="allarms">Ten comparative arms</h2><p>Each mean uses seeds as replicates and includes holds, failed work and all world steps. Seed ranges describe variability; statistical intervals and interpretation belong in the report.</p>
<div class="scroll" tabindex="0"><table><caption>Resource per world step and actual-action coverage</caption><thead><tr><th scope="col">Arm</th><th scope="col">Net</th><th scope="col">Seed range</th><th scope="col">Gross</th><th scope="col">Burned</th><th scope="col">Correct / all steps</th><th scope="col">Non-hold actions</th><th scope="col">Updates / run</th></tr></thead><tbody>__GROUPROWS__</tbody></table></div>
<details><summary>See every seed's group return</summary><div class="scroll" tabindex="0"><table><caption>Independent seed-level net resource per complete world step</caption><thead><tr><th scope="col">Seed</th>__SEEDHEADERS__</tr></thead><tbody>__SEEDROWS__</tbody></table></div></details>
<p class="notice"><strong>Matched gate comparison:</strong> opportunity/upfront minus matched/error/upfront has mean net difference <strong>__GATEDELTA__</strong>, with paired seed differences from __GATERANGE__. Both use buyer probing every eight requests and permit renewal. Other error-versus-opportunity comparisons also change buyer probing and renewal.</p></section>
<section aria-labelledby="phases"><h2 id="phases">Which source helped, and when?</h2><p>Four phases of __PHASELEN__ steps change physical utility without announcing a quality label to learners. Rare demand is absent throughout phases 1 and 2.</p>
<div class="filters"><label>Arm<select id="phase-arm" aria-controls="phase-body"></select></label><label>Replicate<select id="phase-seed" aria-controls="phase-body"><option value="mean">Seed means and ranges</option></select></label></div>
<div class="scroll" tabindex="0"><table><caption id="phase-caption">Full success-paid arm · means across seeds</caption><thead><tr><th scope="col">Phase</th><th scope="col">Source</th><th scope="col">Net / source step</th><th scope="col">Seed range</th><th scope="col">Correct / source steps</th><th scope="col">Non-hold actions</th><th scope="col">Producer advice</th><th scope="col">Updates</th></tr></thead><tbody id="phase-body">__PHASEROWS__</tbody></table></div>
<p class="muted">Producer advice can remain available in the communication-withdrawn arm. It does not mean that the consumer received it. Phase/source burn assigns globally scheduled maintenance and initial construction to the current request, so this allocation is not a component's causal marginal cost.</p></section>
<section aria-labelledby="retention"><h2 id="retention">Occasional utility and retained knowledge</h2><p>A useful rare service is quiet for two whole phases, then returns with the same physical law. The use-reset arm pays to discard and rebuild after inactivity; the retained arm can reuse its interpretation. Parked storage has zero rent in this trial.</p>
<div class="scroll" tabindex="0"><table><caption>Rare source before the drought and after its return</caption><thead><tr><th scope="col">Arm</th><th scope="col">Phase</th><th scope="col">Net / source step</th><th scope="col">Seed range</th><th scope="col">Correct / source steps</th><th scope="col">Non-hold actions</th></tr></thead><tbody>__RETENTIONROWS__</tbody></table></div></section>
<section aria-labelledby="opportunity"><h2 id="opportunity">__FUTURETITLE__</h2><div class="notice warning"><strong>Full-arm new-source net: __FUTURENET__ per request; __FUTURECOUNT__ were profitable.</strong> __FUTUREBODY__</div>
<p>Source 3 begins as unpredictable traffic and becomes ordinally useful only in phase 3. Funding, inherited evidence and buyer rejection can still prevent useful re-entry. No hidden change announcement or direct success label repairs those failures.</p>
<div class="scroll" tabindex="0"><table><caption>Newly useful source · phase 3</caption><thead><tr><th scope="col">Arm</th><th scope="col">Net / source step</th><th scope="col">Seed range</th><th scope="col">Correct / source steps</th><th scope="col">Updates</th><th scope="col">Refinements</th></tr></thead><tbody>__FUTUREROWS__</tbody></table></div></section>
<section aria-labelledby="trace"><h2 id="trace">One actual run, step by step</h2><p>This is the full success-paid arm, seed __TRACESEED__. Scenario phase and source descriptions are visible to this reviewer; they are not learner inputs. The stories below locate early interpretation and later paid changes without replacing the complete recorded trace.</p>
<p class="muted">Advice is captured before the current update; landmark count is captured after any refinement or reset. The stage description is approximate on edited steps. Stable common-source phase 0 provides the presence example; stable phase 2 provides the ordinal example.</p>
<div class="stories" id="stories"></div>
<details><summary>Inspect the recorded decisions</summary><div class="filters"><label>Source<select id="trace-source"><option value="all">All sources</option></select></label><label>Events<select id="trace-kind"><option value="key">Interpretation changes and paid edits</option><option value="all">Every world step</option></select></label></div>
<div class="pager"><button id="trace-back" type="button">Previous</button><span id="trace-count" aria-live="polite"></span><button id="trace-next" type="button">Next</button></div>
<div class="scroll" tabindex="0"><table><caption>Advice is the producer's public answer; a blank answer remains open</caption><thead><tr><th scope="col">Tick</th><th scope="col">Source</th><th scope="col">Interpretation</th><th scope="col">Advice</th><th scope="col">Action</th><th scope="col">Gross</th><th scope="col">Burned</th><th scope="col">Learn granted</th><th scope="col">Edit</th><th scope="col">Pressure</th><th scope="col">Supplier reserve</th></tr></thead><tbody id="trace-body"></tbody></table></div></details>
<noscript><p>The result tables above work without JavaScript. Enable local JavaScript to filter arms and inspect the embedded trace.</p></noscript></section>
<section aria-labelledby="controls"><h2 id="controls">What differs between the arms?</h2><details><summary>Open the exact recorded control settings</summary><div class="scroll" tabindex="0"><table><caption>Buyer probe period counts requests to that particular source</caption><thead><tr><th scope="col">Arm</th><th scope="col">Learning</th><th scope="col">Gate</th><th scope="col">Probe period</th><th scope="col">Initial trial allowance</th><th scope="col">Payment</th><th scope="col">Renewal</th><th scope="col">Idle knowledge</th><th scope="col">Advice delivery</th></tr></thead><tbody>__CONTROLROWS__</tbody></table></div></details>
<ul><li>The no-learning arm starts with empty models, while its alternating fallback can physically act.</li><li>Success-paid fees are conserved internal transfers. Successful fallback can pay a supplier with an open publication, so this is attributed task success, not causal contribution credit.</li><li>Opportunity traffic means a purchased request; funded demand means sufficient buyer reserve. The gate is authored and does not learn the value of a computation.</li><li>High error can keep requesting work on noise. Reserve exhaustion bounds spending but does not establish recognition of irreducible noise.</li></ul></section>
<section aria-labelledby="prices"><h2 id="prices">Prices and evidence boundaries</h2><p>Fee 2 transfers internally. Purchased execution 1, comparison 1, gate inspection 1, a granted learning attempt 1, construction 8 and an ordinal proposal/reset 16 enter retained burn sinks. Active rent is 1 per 32 world tasks; parked rent is zero. Abstract work excludes some reads, sampling, output and actual CPU time.</p>
<p>Currency is checked on every step: buyer, suppliers, attention, escrow and work sinks equal initial funding plus signed external physical outcomes. Group return subtracts burned work once; internal fees redistribute existing resources.</p>
<p>__ARCHIVES__. Saved replay covers retained ordinal anchor classes or fresh presence-only requests and paid gate continuation through a declared reconnected ordered fixture. It does not reconnect an arbitrary novel physical sample to the world's private metric dictionary.</p>
<p class="muted">Provider duplicate-feedback exclusion is not guaranteed. This synchronous trial admits each receipt once. A bounded supplied grammar, four initial scaffolds and authored action law remain necessary context for every result.</p></section>
<footer>Self-contained review. All values and this trace are embedded in the file; there are no third-party libraries or network requests. <a href="REPORT.md">Report</a> · <a href="README.md">Architecture and reproducibility</a></footer>
</main><script id="review-data" type="application/json">__DATA__</script><script>
"use strict";
const D=JSON.parse(document.getElementById("review-data").textContent), F="opportunity/success-paid";
const fmt=(x,d=2)=>x===null||x===undefined?"—":Number(x).toLocaleString(undefined,{minimumFractionDigits:d,maximumFractionDigits:d});
const pct=x=>x===null||x===undefined?"—":fmt(100*x,1)+"%";
function cell(row,value,header=false,cls=""){const e=document.createElement(header?"th":"td");if(header)e.scope="row";e.textContent=value;if(cls)e.className=cls;row.appendChild(e);return e;}
function cls(value){return value>0?"positive":value<0?"negative":"";}
const armSelect=document.getElementById("phase-arm"), seedSelect=document.getElementById("phase-seed");
D.names.forEach(name=>{const o=document.createElement("option");o.value=o.textContent=name;armSelect.appendChild(o);});armSelect.value=F;
[...new Set(D.runs.map(r=>r.seed))].sort((a,b)=>a-b).forEach(seed=>{const o=document.createElement("option");o.value=String(seed);o.textContent="Seed "+seed;seedSelect.appendChild(o);});
function renderPhases(){const tbody=document.getElementById("phase-body");tbody.replaceChildren();const name=armSelect.value, chosen=seedSelect.value;
document.getElementById("phase-caption").textContent=name+" · "+(chosen==="mean"?"means across seeds":"seed "+chosen);
const run=chosen==="mean"?null:D.runs.find(r=>r.arm===name&&String(r.seed)===chosen);
for(let phase=0;phase<4;phase++)for(let source=0;source<4;source++){const tr=document.createElement("tr");cell(tr,D.phases[phase],true);cell(tr,D.sources[source]);
if(run){const r=run.rows.find(r=>r.phase===phase&&r.source===source);if(!r){const c=cell(tr,"No requests");c.colSpan=6;c.className="muted";}else{const net=(r.gross-r.burned)/r.steps;cell(tr,fmt(net),false,cls(net));cell(tr,"—");cell(tr,pct(r.correct/r.steps));cell(tr,pct(r.answered/r.steps));cell(tr,pct(r.interpreted/r.steps));cell(tr,fmt(r.learningUpdates,0));}}
else{const v=D.summaries[name].phases[phase+":"+source];if(!v.net.n){const c=cell(tr,"No requests");c.colSpan=6;c.className="muted";}else{cell(tr,fmt(v.net.mean),false,cls(v.net.mean));cell(tr,fmt(v.net.low)+" to "+fmt(v.net.high));cell(tr,pct(v.correct.mean));cell(tr,pct(v.covered.mean));cell(tr,pct(v.interpreted.mean));cell(tr,fmt(v.updates.mean,1));}}tbody.appendChild(tr);}}
armSelect.addEventListener("change",renderPhases);seedSelect.addEventListener("change",renderPhases);
function stage(s){return !s?"no purchased event":s.refined||s.reset?(s.interpreted?"advice before edit":"open before edit"):s.interpreted?(s.landmarks>0?"ordinal · "+s.landmarks+" anchors":"presence"):(s.landmarks>0?"open · ordinal exists":"open");}
function story(step,title,body){if(!step)return;const article=document.createElement("article");article.className="story";const tick=document.createElement("div");tick.className="tick";tick.textContent="Tick "+step.tick+" · "+D.sources[step.source];const h=document.createElement("h3");h.textContent=title;const p=document.createElement("p");p.textContent=body+" Gross "+fmt(step.gross,0)+", burned "+fmt(step.burned,0)+", supplier reserve "+fmt(step.supplierReserve,0)+".";article.append(tick,h,p);document.getElementById("stories").appendChild(article);}
const firstOpen=D.trace.find(s=>s.bought&&!s.interpreted), firstPresence=D.trace.find(s=>s.source===0&&s.interpreted&&s.landmarks===0), firstRefine=D.trace.find(s=>s.source===0&&s.refined), rareReturn=D.trace.find(s=>s.source===1&&s.phase===3&&s.bought), futureEntry=D.trace.find(s=>s.source===3&&s.phase===3&&s.bought);
story(firstOpen,"An open answer can still be tested","The buyer purchases an untrained service; the native fallback acts. No magnitude or certainty estimate is supplied.");
story(firstPresence,"A simpler answer becomes supported","The presence-conditioned model publishes direction "+firstPresence?.advice+" without ordinal comparisons.");
story(firstRefine,"Pressure funds an ordinal proposal","The common law has changed. A paid construction creates a bounded ordinal model; immediate success is not guaranteed.");
story(rareReturn,"Demand returns after a long gap","Retained interpretation is "+stage(rareReturn)+". This is one actual return event, not the multi-seed retention estimate.");
story(futureEntry,"A previously noisy source is tested again","The reviewer knows the apparatus has become learnable. The provider receives no such announcement; its interpretation is "+stage(futureEntry)+".");
const traceSource=document.getElementById("trace-source"), traceKind=document.getElementById("trace-kind");D.sources.forEach((name,i)=>{const o=document.createElement("option");o.value=String(i);o.textContent=name;traceSource.appendChild(o);});
const lastStage=new Map(), lastPhase=new Map(), keyTicks=new Set();D.trace.forEach((s,i)=>{const value=stage(s);if(i<8||s.refined||s.reset||lastStage.get(s.source)!==value||lastPhase.get(s.source)!==s.phase)keyTicks.add(s.tick);lastStage.set(s.source,value);lastPhase.set(s.source,s.phase);});
let page=0;const pageSize=35;
function renderTrace(){const selected=D.trace.filter(s=>(traceSource.value==="all"||String(s.source)===traceSource.value)&&(traceKind.value==="all"||keyTicks.has(s.tick)));const pages=Math.max(1,Math.ceil(selected.length/pageSize));page=Math.min(page,pages-1);const tbody=document.getElementById("trace-body");tbody.replaceChildren();selected.slice(page*pageSize,(page+1)*pageSize).forEach(s=>{const tr=document.createElement("tr");cell(tr,s.tick,true);cell(tr,D.sources[s.source]);cell(tr,stage(s));cell(tr,s.advice===null?"open":s.advice);cell(tr,s.action);cell(tr,fmt(s.gross,0),false,cls(s.gross));cell(tr,fmt(s.burned,0));cell(tr,s.learningGranted?"yes":"no");cell(tr,s.reset?"reset":s.refined?"refined":"—");cell(tr,s.pressure);cell(tr,s.supplierReserve);tbody.appendChild(tr);});document.getElementById("trace-count").textContent="Page "+(page+1)+" of "+pages+" · "+selected.length+" matching steps";document.getElementById("trace-back").disabled=page===0;document.getElementById("trace-next").disabled=page>=pages-1;}
traceSource.addEventListener("change",()=>{page=0;renderTrace();});traceKind.addEventListener("change",()=>{page=0;renderTrace();});document.getElementById("trace-back").addEventListener("click",()=>{page--;renderTrace();});document.getElementById("trace-next").addEventListener("click",()=>{page++;renderTrace();});renderTrace();
</script></body></html>'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", nargs="?", type=Path, default=HERE / "results" / "confirmation-01")
    parser.add_argument("--output", type=Path, default=HERE / "review.html")
    args = parser.parse_args()
    build(args.directory.resolve(), args.output.resolve())


if __name__ == "__main__":
    main()
