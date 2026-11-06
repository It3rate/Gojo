"""Read the sealed observations and show the physical timing of a routing credit."""
import csv
import hashlib
import json
import statistics
import sys
from collections import defaultdict
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

MODES=["Live","Quiet","Fixed","CutUp","CutDown","Misrouted"]

def summarize(root):
    load=lambda p:json.loads(p.read_bytes())
    rows=load(root/"cases.json")
    complete=load(root/"complete.json")
    assert hashlib.sha256((root/"cases.json").read_bytes()).hexdigest().upper()==complete["cases"]
    assert complete["draws"]==len(rows)==1728
    compare={(r["sample"]["basis"],r["sample"]["fixture"],r["sample"]["wrongPrior"],r["sample"]["useful"],
             r["sample"]["bays"],r["mode"]):r for r in rows}
    groups=defaultdict(list)
    for r in rows:
        s=r["sample"];groups[s["fixture"],s["wrongPrior"],r["mode"]].append(r)
    result=[]
    for (fixture,biased,mode),batch in groups.items():
        total=lambda f:sum(r[f] for r in batch)
        mean=lambda f:statistics.mean(r[f] for r in batch)
        def share(phase):
            return statistics.mean(r["useful"+phase]/max(1,r["useful"+phase]+r["fixed"+phase]) for r in batch)
        matched=[compare[r["sample"]["basis"],fixture,biased,r["sample"]["useful"],r["sample"]["bays"],2] for r in batch]
        result.append(dict(fixture=fixture,biased=biased,mode=MODES[mode],drawings=len(batch),early_share=share("Early"),
            late_share=share("Late"),late_missing=sum(r["usefulLate"]+r["fixedLate"]==0 for r in batch),
            credited_drawings=sum(r["creditAfter"]>=0 for r in batch),post_credit_useful=total("usefulAfter"),
            post_credit_other=total("fixedAfter"),relief=total("relief"),misses=total("misses"),
            mean_strength=mean("strength"),settled_fraction=mean("settled"),pen_debt=mean("penDebt"),
            machine_debt=mean("machineDebt"),frieze_debt=mean("friezeDebt"),acts=mean("actuations"),
            changed_vs_fixed=sum(r["ink"]!=f["ink"] for r,f in zip(batch,matched))))
    with (root/"summary.csv").open("w",newline="",encoding="utf-8") as out:
        w=csv.DictWriter(out,fieldnames=list(result[0]));w.writeheader();w.writerows(result)
    cases={f:load(root/"examples"/(f+".json"))["result"] for f in ["early-Live","early-Fixed","synchronous-Live","synchronous-Fixed"]}
    for case in cases.values():
        route=next(r for r in case["stats"]["routes"] if r["source"]==2 and r["target"]==0 and r["lane"]==2)
        credits=[p for p in case["timeline"] if p["kind"] in ("relief","miss") and p["source"]==2 and p["target"]==0 and p["lane"]==2]
        assert sum(e["kind"]=="relief" for e in credits)==route["relief"]
        assert sum(e["kind"]=="miss" for e in credits)==route["misses"]
        deposits=[p for p in case["timeline"] if p["kind"]=="delivery" and p["source"]==2 and p["target"]==0 and p["lane"]==2 and p["accepted"]]
        assert len(deposits)==route["accepted"]
    return cases

def plate(root,cases):
    image=Image.new("RGB",(1190,755),"#fbf8ef");d=ImageDraw.Draw(image)
    def text(x,y,s,size=16,bold=False):
        d.text((x,y),s,font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",size),fill="#213b32")
    text(28,15,"When does seam relief reach the right pen?",36,True)
    text(28,73,"One useful outlet and one bound terminal, 32 native bays. Each column shows one bay in time order.",20)
    text(28,107,"Green = unit admitted at useful pen. Ochre = admitted at bound pen. A filled lower mark means the seam joined.",17)
    for j,(fixture,label) in enumerate((("early-Live","Early action · live"),("early-Fixed","Same timing · fixed strength"),
                                ("synchronous-Live","Same-beat action · live"),("synchronous-Fixed","Same-beat action · fixed strength"))):
        y=151+j*143
        case=cases[fixture];hist=case["timeline"];trace=case["trace"]
        text(28,y,label,21,True)
        credits=[e for e in hist if e["source"]==2 and e["target"]==0 and e["lane"]==2 and e["kind"] in ("relief","miss")]
        text(530,y+3,f"Useful route: {sum(e['kind']=='relief' for e in credits)} relief, {sum(e['kind']=='miss' for e in credits)} missed",16)
        for c in range(32):
            x=28+c*35
            received=[e for e in hist if e["source"]==2 and e["kind"]=="delivery" and e["lane"]==2 and e["accepted"] and e["cell"]==c]
            for idx,e in enumerate(received):
                d.ellipse((x+8*(idx%4),y+51-9*(idx//4),x+8*(idx%4)+7,y+58-9*(idx//4)),fill="#288167" if e["target"]==0 else "#b8792e")
            joined=any(e["owner"]==0 and e["cell"]==c and e["to"]==c+1 for e in trace["edges"])
            d.rectangle((x,y+68,x+27,y+75),fill="#203b33" if joined else "#d9d7ca")
            if c%4==0:text(x,y+84,str(c+1),13)
    text(28,722,"Inspection fixture at basis 0, uniform priors, useful pen at position 0. These marks summarize the actual drawing history.",15)
    image.save(root/"Timeline.png")

if __name__=="__main__":
    root=Path(sys.argv[1]);plate(root,summarize(root))
