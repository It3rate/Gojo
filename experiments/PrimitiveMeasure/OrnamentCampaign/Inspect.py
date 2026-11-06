"""Build a local results viewer from frozen programs and observations; no search."""
import csv
import json
import sys
from pathlib import Path


def rows(path):
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def main(folder):
    planned = {r["id"]:r for r in rows(folder/"manifest/hypotheses.csv")}
    verdicts = [{**planned[r["id"]],**r} for r in rows(folder/"test/hypotheses.csv")]
    parents = rows(folder/"test/groups.csv")
    episodes = {(r["goal"],r["parent"],r["variant"]):r for r in rows(folder/"manifest/episodes.csv") if r["split"]=="test"}
    candidates = { (r["Goal"],str(r["Replicate"])):r["GenomeHex"] for r in
        json.loads((folder/"frozen.json").read_text(encoding="utf-8-sig"))["Content"]["Winners"] if r["Method"]=="evolve" }
    samples = {}
    for r in rows(folder/"test/cases.csv"):
        if r["method"] != "evolve" or r["training_goal"] != "mirror" or r["replicate"] != "3" or r["treatment"]!="native":
            continue
        key=(r["group"],r["variant"])
        if key not in {("test-window-10","0"),("test-reflected-05","1")}: continue
        if r["wire"] not in ("Live","Frozen","Cut","Inverted"): continue
        e=episodes[(r["goal"],) + key]
        samples.setdefault("/".join(key),{"input":e,"out":{}})["out"][r["wire"]]=r
    html = """<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Local ornament tournament</title><style>
body{max-width:1040px;margin:35px auto;padding:0 24px;background:#f7f5ef;color:#1b352f;font:17px/1.6 system-ui}
h1,h2{line-height:1.1}a{color:#126851}nav,fieldset{display:flex;gap:12px;flex-wrap:wrap;align-items:center}fieldset{border:0;padding:0}
select{font:inherit;padding:6px;max-width:100%}svg{display:block;background:white;width:100%;margin:14px 0}table{border-collapse:collapse;font-size:14px}td,th{padding:4px 10px;border-bottom:1px solid #bfc9c2;text-align:left}
.box{padding:18px 0;border-bottom:1px solid #bfc9c2}.mono{font-family:monospace}</style>
<h1>Local ornament tournament</h1>
<nav><a href="Report.md">First-wave report</a><a href="Gallery.html">Fabric and rail gallery</a><a href="manifest/ScienceProtocol.md">Frozen design</a><a href="test/hypotheses.csv">All verdicts</a></nav>
<section class="box"><h2>A nine-byte reflector</h2>
<p>The sensor asks whether a reflected site is occupied while this one is empty. Its writer can spend ink here. Choose a seed and a channel to replay that stored trial.</p>
<fieldset><label>Blind seed <select id="seed"></select></label><label>Channel <select id="wire"></select></label></fieldset><div id="sketch"></div></section>
<section><h2>Explore the 240 preregistered questions</h2>
<fieldset><label>Goal <select id="goal"></select></label><label>Starting family <select id="family"></select></label><label>Comparison <select id="mechanism"></select></label></fieldset>
<p>Each parent contributes two related views and four training replicates. The table shows the eight paired comparisons for each parent as one average. A positive difference favors the left arm.</p><div id="answer"></div></section>
<script>
const data=VERDICTS, groups=PARENTS, samples=SAMPLES;
const el=id=>document.getElementById(id), opt=(s,t)=>{let o=document.createElement('option');o.textContent=t;s.append(o)};
for(const field of ['goal','family','mechanism'])[...new Set(data.map(r=>r[field]))].forEach(v=>opt(el(field),v));
Object.keys(samples).forEach(v=>opt(el('seed'),v));['Live','Frozen','Cut','Inverted'].forEach(v=>opt(el('wire'),v));
const bit=(h,i)=>(BigInt('0x'+h)>>BigInt(i)&1n)===1n;
function draw(){const s=samples[el('seed').value], e=s.input, r=s.out[el('wire').value], width=+e.width;
let svg='<svg viewBox="0 0 950 150">';[e.seed_hex,r.final_ink_hex].forEach((h,n)=>{const y=35+n*62;svg+='<text x="6" y="'+(y+4)+'">'+['Initial','Result'][n]+'</text>';for(let i=0;i<width;i++){
const x=77+i*855/width;svg+='<rect x="'+x+'" y="'+(y-11)+'" width="15" height="23" fill="'+(bit(h,i)?'#27785c':'#dedfd6')+'"/><text x="'+(x+1)+'" y="'+(y+25)+'" font-size="9">'+i+'</text>';
if(bit(e.intervention_xor_hex,i)&&n===0)svg+='<path d="M'+x+' '+(y-17)+'h15" stroke="#c34e28" stroke-width="3"/>';}});svg+='</svg>';
const channel={Live:'The sensor reads current ink at each visit.',Frozen:'The sensor keeps reading the starting ink as the strip changes.',Cut:'The writer receives no observation from the sensor.',Inverted:'The connection exchanges the sensor’s known off and on signals.'}[el('wire').value];
el('sketch').innerHTML=svg+'<p>'+channel+' Utility '+r.utility+' / 1000, compared with '+r.idle_utility+' with the same external event and no edits. Attempts '+r.attempts+', accepted edits '+r.edits+'. A red dash marks any site toggled after the first scan.</p><p class="mono">Program: GENOME</p>';}
function show(){const r=data.find(r=>['goal','family','mechanism'].every(k=>r[k]===el(k).value));const g=groups.filter(p=>p.id===r.id);
el('answer').innerHTML='<h3>'+r.id+' · '+r.status+'</h3><p>'+r.rationale+'</p><p>Compared '+r.left_method+' / '+r.left_wire+' / '+r.left_treatment+' with '+r.right_method+' / '+r.right_wire+' / '+r.right_treatment+'. Predicted '+r.relation+', margin '+r.margin+'; observed average difference '+Number(r.mean_delta).toFixed(1)+'.</p><table><tr><th>Parent group</th><th>Matched cases</th><th>Mean difference</th></tr>'+g.map(p=>'<tr><td>'+p.group+'</td><td>'+p.matched_pairs+'</td><td>'+Number(p.delta).toFixed(1)+'</td></tr>').join('')+'</table>'}
['goal','family','mechanism'].forEach(k=>el(k).onchange=show);el('wire').onchange=draw;el('seed').onchange=draw;show();draw();
</script></html>"""
    for tag, value in [("VERDICTS",verdicts),("PARENTS",parents),("SAMPLES",samples)]:
        html=html.replace(tag,json.dumps(value,separators=(",",":")).replace("<","\\u003c"))
    (folder/"Explore.html").write_text(html.replace("GENOME",candidates["mirror","3"]), encoding="utf-8")


if __name__ == "__main__":
    main(Path(sys.argv[1]).resolve())
