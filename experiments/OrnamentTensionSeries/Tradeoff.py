"""Post-hoc exhibit of the first running collection meeting explicit trade-off thresholds."""
import csv
import json
from pathlib import Path
import subprocess
import sys
from PIL import Image,ImageDraw,ImageFont

def run(root,runtime):
    root=root.resolve()
    load=lambda f:json.loads(f.read_text(encoding="utf-8-sig"))
    with (root/"reserve/collections.csv").open(newline="") as f:
        index={(r["family"],r["rep"],r["setting"],r["mode"]):r for r in csv.DictReader(f)}
    matches=[]
    for (*_,mode),l in index.items():
        if mode!="Live":continue
        h=index[l["family"],l["rep"],l["setting"],"Hold"]
        if float(h["reference"])>=.7 and float(h["repeat"])>=.7 and float(l["novelty"])-float(h["novelty"])>=.02 and float(l["repeat"])<=float(h["repeat"])-.03:
            matches.append((l,h))
    if not matches:return
    l,h=matches[0];family=l["family"];rep=int(l["rep"])
    fam=list(dict.fromkeys(r["family"] for r in index.values())).index(family)
    winner=next(x for x in load(root/"finalists.json")["winners"] if x["tray"]["family"]==fam and x["tray"]["replicate"]==rep)
    setting=next(x for x in load(root.parent/"wave1/design/settings.json")["reserve"] if x["key"]==l["setting"])
    directory=root/"tradeoff";directory.mkdir(exist_ok=True)
    material=[]
    for mode in (3,0):
        path=directory/f"recipe-{mode}.json";out=directory/f"actual-{mode}.json"
        path.write_text(json.dumps(dict(tray=winner["tray"],rule=winner["rule"],setting=setting,mode=mode)),encoding="utf-8")
        subprocess.run(["dotnet",str(runtime.resolve()),"play",str(path),str(out)],check=True)
        material.append(load(out))
    (directory/"example.json").write_text(json.dumps(dict(eligible=len(matches),live=l,hold=h),indent=2),encoding="utf-8")
    def edges(trace):
        sites=trace["sites"]
        return [(sites[e["from"]],sites[e["to"]],e["owner"]) for e in trace["edges"]]
    panels=[[edges(step["trace"]) for step in mode] for mode in material]
    points=[p for mode in panels for member in mode for a,b,_ in member for p in (a,b)]
    x0=min(p["x"] for p in points);x1=max(p["x"] for p in points)
    y0=min(p["y"] for p in points);y1=max(p["y"] for p in points)
    img=Image.new("RGB",(1280,490),"#faf7ee");d=ImageDraw.Draw(img)
    def text(x,y,t,size=18,b=False):
        d.text((x,y),t,font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if b else "")+".ttf",size),fill="#25443b")
    text(25,17,"A regular key can be useful even when the series repeats",32,True)
    text(25,66,f"{family}  /  {l['setting']}  /  first 4 members of 8 at a shared scale",19)
    for j,(rows,record,title) in enumerate(zip(panels,(h,l),("Holding one program","Using series memory"))):
        base=110+j*180
        text(25,base,title,23,True)
        text(355,base+3,f"collection novelty {float(record['novelty']):.3f}     within-band repeat {float(record['repeat']):.3f}     historical profile {float(record['reference']):.3f}",17)
        scale=min(264/max(1,x1-x0),90/max(1,y1-y0))
        for k,lines in enumerate(rows[:4]):
            for a,b,o in lines:
                def xy(p):return (26+k*315+(264-scale*(x1-x0))/2+(p["x"]-x0)*scale,base+48+(90-(y1-y0)*scale)/2+(y1-p["y"])*scale)
                d.line([xy(a),xy(b)],fill=["#235348","#a84c31","#395e7c"][o],width=2)
            move=material[j][k]["move"]
            text(26+k*315,base+145,f"member {k+1} · tray {move['tool']+1} · pulse {move['pulse']:+d}",15)
    img.save(root/"Tradeoff.png")

if __name__=="__main__":run(Path(sys.argv[1]),Path(sys.argv[2]))
