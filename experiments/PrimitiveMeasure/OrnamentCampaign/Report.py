"""Read locked CSV observations and make local review artifacts; no candidate execution."""
import collections
import csv
import hashlib
import html
import json
import statistics
import sys
from pathlib import Path


def table(path):
    with path.open(encoding="utf-8-sig", newline="") as file:
        return list(csv.DictReader(file))


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def select(rows, **fields):
    return [r for r in rows if all(r[k] == v for k, v in fields.items())]


def avg(rows, field):
    return statistics.mean(float(r[field]) for r in rows)


def hashes(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def fabric_summary(rows):
    def key(r):
        return tuple(r[k] for k in ("method","rep","seed","law","phase","stock"))
    index = {(key(r), r["condition"]): r for r in rows}
    fam = sorted({r["seed"] for r in rows})
    live = select(rows, method="evolve", condition="live")
    out = []
    def entry(name, status, detail):
        out.append({"id": name, "status": status, "detail": detail})
    exact = all((int(r["final"],16) & int(r["initial"],16)) == int(r["initial"],16) and
                (int(r["final"],16) ^ int(r["initial"],16)).bit_count() + int(r["stock_left"]) == int(r["stock"]) for r in rows)
    entry("F1", "supported" if exact else "contrary", "protected ink and stock conservation over all rows")
    cut = select(rows, condition="cut-h")
    entry("F2", "supported" if all(r["final"] == r["initial"] for r in cut) else "contrary", f"{len(cut)} disconnected rows")
    number = sum(any(r["final"] != r["initial"] for r in select(rows, method="evolve", condition="unknown-v", law="14", seed=f)) for f in fam)
    entry("F3", "supported" if number >= 4 else "not met", f"{number}/6 families admit an OR mark despite an unknown vertical port")
    for name, condition in (("F4","hold-h"),("F5","hold-v")):
        rates = {f: statistics.mean(r["final"] != index[key(r),condition]["final"] for r in select(live, seed=f)) for f in fam}
        passed = sum(x >= .2 for x in rates.values())
        entry(name,"supported" if passed >= 4 else "not met",f"{passed}/6 families exceed 20% changed outputs: " + str({k:round(v,2) for k,v in rates.items()}))
    gains = {f: statistics.mean(int(index[key(r),"seed-profiles"]["mirror_mismatches"]) - int(r["mirror_mismatches"])
                               for r in select(live, seed=f)) for f in fam}
    entry("F6","supported" if sum(x >= 1 for x in gains.values()) >= 4 else "contrary" if sum(x <= -1 for x in gains.values()) >= 4 else "inconclusive",
          "mean reduction in mirror mismatches by seed: " + str({k:round(v,2) for k,v in gains.items()}))
    rates = {f: statistics.mean(r["final"] != index[key(dict(r,law="6")),"live"]["final"] for r in select(live, seed=f, law="14")) for f in fam}
    passed = sum(x >= .2 for x in rates.values())
    entry("F7","supported" if passed >= 4 else "not met", f"{passed}/6 families exceed 20% changed outputs on exchanging OR and XOR")
    small = select(rows,stock="6")
    nested = all(int(r["final"],16) & ~int(index[key(dict(r,stock="24")), r["condition"]]["final"],16) == 0 for r in small)
    entry("F8","supported" if nested else "contrary", f"{len(small)} matched allocations")
    return out


def gallery(folder, cases, corpus, fabric):
    selected = []
    for goal in ("balance","infill","mirror","seam"):
        natural = select(cases, training_goal=goal, goal=goal, method="evolve", wire="Live", treatment="native")
        natural.sort(key=lambda a: (int(a["delta_vs_idle"]),a["group"],a["variant"],a["replicate"]))
        for name, i in (("Largest regression",0),("Middle outcome",len(natural)//2),("Largest improvement",-1)):
            r = natural[i]
            seed = corpus[(r["goal"],r["group"],r["variant"])]
            selected.append({"kind":name,"goal":goal,"group":r["group"],"rep":r["replicate"],"variant":r["variant"],
                "width":int(seed["width"]),"initial":seed["initial_hex"],"final":r["final_ink_hex"],"pins":seed["protected_hex"],
                "intervention":seed["intervention_xor_hex"],"gain":int(r["delta_vs_idle"]),"edits":int(r["edits"]),"utility":int(r["utility"])})
    fabrics = [r for r in fabric if r["method"] == "evolve" and r["rep"] == "0" and r["phase"] == "0" and r["condition"]=="live"]
    page = """<!doctype html><html lang="en"><meta charset="utf-8"><title>Local ornament review</title>
<style>body{max-width:960px;margin:3rem auto;background:#f7f5ef;color:#193c35;font:16px/1.5 system-ui;padding:0 1rem}h1,h2{line-height:1.2}select{font:inherit;padding:.4rem;max-width:100%}svg{display:block;background:white;width:100%;max-width:850px;margin:1rem 0}p{max-width:80ch}a{color:#11634f}</style>
<h1>Local ornament review</h1><p>These are stored blind observations. Choosing a row reads the record. The examples select the lowest, middle and highest changes in offline utility within each brief over four evolved replicates.</p>
<label>Rail example <select id="rail"></select></label><div id="railview"></div>
<h2>Two private strips and a writer</h2><p>One signal comes from each axis. The writer reads their bound crossing through a Boolean law and spends its own ink. These examples use the first matched evolved replicate. Dark sites belong to the protected glyph seed. Green sites are new marks.</p>
<label>Fabric example <select id="fabric"></select></label><div id="fabricview"></div>
<script>
const rails=RAIL_JSON, fabrics=FABRIC_JSON;
const bit=(h,i)=>(BigInt('0x'+h)>>BigInt(i)&1n)===1n;
const law={'8':'AND','14':'OR','6':'XOR'};
const rail=document.getElementById('rail'),fabric=document.getElementById('fabric');
function option(s,t){const o=document.createElement('option');o.textContent=t;s.appendChild(o)}
rails.forEach(r=>option(rail,r.goal+' · '+r.kind+' · '+r.group+' · rep '+r.rep));
fabrics.forEach(r=>option(fabric,r.seed+' · '+law[r.law]+' · stock '+r.stock));
rail.onchange=()=>{const r=rails[rail.selectedIndex];let svg='<svg viewBox="0 0 920 140">';
['initial','final'].forEach((k,n)=>{let y=35+58*n;svg+='<text x="8" y="'+(y+4)+'">'+k+'</text>';
for(let i=0;i<r.width;i++){const x=95+i*800/r.width;svg+='<rect x="'+x+'" y="'+(y-10)+'" width="12" height="22" fill="'+(bit(r[k],i)?'#24735e':'#e3e5dc')+'"/>';
if(bit(r.pins,i))svg+='<circle cx="'+(x+6)+'" cy="'+(y+18)+'" r="3" fill="#884a25"/>';if(n===0&&bit(r.intervention,i))svg+='<path d="M'+x+' '+(y-15)+'h12" stroke="#bf4225" stroke-width="3"/>';}});
document.getElementById('railview').innerHTML=svg+'</svg><p>Change versus no edits on the same disturbed rail: '+r.gain+' / 1000. Final utility '+r.utility+'. Accepted edits '+r.edits+'. A dot marks a protected site. A red dash marks the externally toggled site.</p>'};
fabric.onchange=()=>{const r=fabrics[fabric.selectedIndex];let svg='<svg style="max-width:260px" viewBox="0 0 140 140">';for(let i=0;i<49;i++){const color=bit(r.initial,i)?'#203a38':bit(r.final,i)?'#359d78':'#ebeadf';svg+='<rect x="'+(i%7*20+1)+'" y="'+(Math.floor(i/7)*20+1)+'" width="18" height="18" fill="'+color+'"/>'}document.getElementById('fabricview').innerHTML=svg+'</svg><p>Stock remaining '+r.stock_left+'. Requests refused for lack of stock '+r.refused+'.</p>'};
rail.onchange();fabric.onchange();
</script></html>"""
    (folder/"Gallery.html").write_text(page.replace("RAIL_JSON",json.dumps(selected)).replace("FABRIC_JSON",json.dumps(fabrics)), encoding="utf-8")


def main(folder):
    f = folder
    # This is the first-wave analysis, including commentary about specific
    # winners. A different experiment requires its own interpretation.
    locked = read_json(f/"test/complete.json")
    if hashes(f/"frozen.json") != "BFA2E5AEF551148B0B468914373C85740042C52A0D3CF2C1B165EEE3399E47D8":
        raise ValueError("This report describes only the recorded primary finalists.")
    for file, digest in locked["FileHashes"].items():
        if hashes(f/"test"/file) != digest:
            raise ValueError(f"Blind results differ from their seal: {file}")
    hypotheses = table(f/"test/hypotheses.csv")
    champions = table(f/"test/champions.csv")
    cases = table(f/"test/cases.csv")
    fabric = table(f/"fabric/fabric.csv")
    corpus = {(r["goal"],r["parent"],r["variant"]):r for r in table(f/"manifest/episodes.csv") if r["split"]=="test"}
    freeze = read_json(f/"protocol.lock.json")
    replay = read_json(f/"replay/verification.json")
    if hashes(f/"replay/observed.json") != replay["ReferenceSha256"]: raise ValueError("Cold replay mismatch")
    details = fabric_summary(fabric)
    gallery(f,cases,corpus,fabric)
    goals=("balance","infill","mirror","seam"); methods=("parameter","open","random","evolve")
    active = [r for r in hypotheses if r["mechanism"]!="zero-budget"]
    counts=collections.Counter(r["status"] for r in active)
    def changes(goal,training):
        return {m:round(avg(select(champions,goal=goal,training_goal=training,method=m),"mean_change"),2) for m in methods}
    data={"substantive":dict(counts),"control":dict(collections.Counter(r["status"] for r in hypotheses if r["mechanism"]=="zero-budget")),
          "specialist_change":{g:changes(g,g) for g in goals},"mixed_change":{g:changes(g,"mixed") for g in goals},"fabric":details}
    (f/"primary-summary.json").write_text(json.dumps(data,indent=2),encoding="utf-8")
    histories=[read_json(p) for p in (f/"islands").rglob("winner.json")]
    text=["# First local ornament tournament", "",
          "The frozen programs improved infill and mirror utility on unfamiliar rails. For balance, full-grammar random search beat evolution on average. Seam repair was unreliable. A useful wire and a useful method for *finding* that wire are separate experimental findings.",
          "", f"Registration: `{freeze['ProtocolHash']}`. Winner lock: `{read_json(f/'test/complete.json')['LockHash']}`. The reserve outcomes were unopened for this report.",
          "", f"The primary competition charged {sum(h['Charged'] for h in histories):,} proposals across {len(histories)} populations. It executed 10,485,760 training episodes on 32-example banks, then used validation to lock one finalist per population. The paired blind table contains {len(cases):,} machine/case/condition rows plus their idle comparisons. Replicates and reflected siblings share evidence. They do not represent that many independent motifs.",
          "",f"There were 240 preregistered comparisons. All 24 zero-budget controls passed. Among 216 substantive predictions, {counts['supported']} were supported, {counts['contrary']} were contrary, and {counts['inconclusive']} were inconclusive under the frozen margins and grouped rules.",
          "", "## Blind utility above the same seed without edits", "",
          "Means are differences on each brief's own 0–1000 score. The scores of different briefs are not interchangeable physical quantities.", "",
          "| Specialist brief | Fixed wiring | Open loop | Full random | Evolved | Mixed evolved |","| --- | ---: | ---: | ---: | ---: | ---: |"]
    for g in goals:
        a=data["specialist_change"][g]
        text.append(f"| {g} | "+" | ".join(f"{a[m]:+.1f}" for m in methods)+f" | {data['mixed_change'][g]['evolve']:+.1f} |")
    text += ["","| Evolved specialist | Improved | Tied | Regressed | Exact final |","| --- | ---: | ---: | ---: | ---: |"]
    for g in goals:
        group=select(champions,goal=g,training_goal=g,method="evolve")
        text.append("| "+g+" | "+" | ".join(str(sum(int(x[k]) for x in group)) for k in ("improved","tied","regressed","exact"))+" |")
    text += ["", "## Predictions and search history", "",
        "| Mechanism | Supported | Inconclusive | Contrary | Mean left minus right |","| --- | ---: | ---: | ---: | ---: |"]
    for m in dict.fromkeys(r["mechanism"] for r in hypotheses):
        group=select(hypotheses,mechanism=m); c=collections.Counter(x["status"] for x in group)
        text.append(f"| {m} | {c['supported']} | {c['inconclusive']} | {c['contrary']} | {avg(group,'mean_delta'):+.1f} |")
    text += ["","The relation predicted differs by cell. A supported equivalence is not an advantage for the left arm. A paired cut or inversion tests a *particular* frozen machine. A comparison with open-loop/random tests different machines from different searches. The live-versus-frozen contrast was much weaker than the live-versus-inverted contrast. Using a wire need not imply adaptation to a newly disturbed rail.",
        "","| Search method | Mean distinct genomes per population | Mean distinct observed behaviors per population |","| --- | ---: | ---: |"]
    for m in methods:
        pop=[h for h in histories if h["Winner"]["Method"]==m]
        text.append(f"| {m} | {statistics.mean(h['DistinctGenomes'] for h in pop):.1f} | {statistics.mean(h['DistinctBehaviors'] for h in pop):.1f} |")
    text += ["","Behavior here is a training-bank fingerprint of final patterns and edit counts. Different bytes can yield the same behavior, and more diversity alone need not mean better performance.",
        "", "## A small machine and its failure", "",
        "Mirror replicate 3, program `0104C2004900244374`, retained one sensor row meaning “the reflected site is occupied and this site is empty.” Its writer passes that pulse into a stock-to-site passage. On `test-window-10`, ordinary view, it added the five missing reflections and reached 1,000 from 500. Disconnecting the signal left the score at 500. A sensor frozen on the initial pattern also reached 1,000, needing ten attempts rather than five. Thus this example establishes a useful correspondence, but updated sensing was not necessary for its final picture.",
        "", "On `test-reflected-05`, perturbed view, external ink appeared at site 3 of a 34-site ring. The very same live rule mirrored the disturbance to site 30. Its score was 857 against 929 with frozen sensing. Both repaired every original missing mirror. The learned rule mirrors visible ink and has no evidence distinguishing a valuable new mark from an accident in this case.",
        "", "For seam replicate 0 on `test-comb-02` ordinary view, the seed was already at 1,000. One live edit lowered utility to 800; a cut preserved 1,000. Aggregate results include these regressions.",
        "", "## Two-axis transfer", "", f"There are {len(fabric):,} fabric observations. The six small orthogonal glyph-like seeds and their projected strip inputs were supplied. The horizontal and vertical machines were trained on one-dimensional balance and infill. The crossing laws were supplied separately. The predicted checks below test that specific composition.",
        "","| Prediction | Verdict | Observation |","| --- | --- | --- |"]
    text += [f"| {v['id']} | {v['status']} | {v['detail']} |" for v in details]
    text += ["","## Verification and limits","", f"All {replay['Replays']:,} cold program/input replays in a fresh directory produced exactly the same outcomes, including trace hashes, as the independent reference execution. Wall times on this machine are in [the timing log](timing.log). They are not portable performance claims.",
        "", "This study searches a bounded local grammar with one to four Boolean rows per owner. The grammar supplies local gap, direction, goal and mirror observations and the allowed edit passages. The search chooses how to connect and use them. It does not learn those input senses, a general aesthetic critic, or arbitrary two-dimensional drawing.",
        "", "[Browse before/after examples and fabric outputs](Gallery.html). [Full prediction verdicts](test/hypotheses.csv) · [paired parent comparisons](test/groups.csv) · [all blind outcomes](test/cases.csv) · [packed winning programs](frozen.json) · [scientific protocol](manifest/ScienceProtocol.md)."]
    report = ("\r\n".join(text)+"\r\n").encode("utf-8")
    sealed_digest = "1320D2322AB10060B67E879F975C51A3390CCB1DCB4D1DB97E1423018EA1FBAC"
    if hashlib.sha256(report).hexdigest().upper() != sealed_digest:
        raise ValueError("Generated analysis differs from the report used to design the follow-ups.")
    path = f/"Report.md"
    if path.exists():
        if hashes(path) != sealed_digest:
            raise ValueError("An existing first-wave report differs from the recorded one.")
    else:
        path.write_bytes(report)


if __name__=="__main__":
    main(Path(sys.argv[1]).resolve())
