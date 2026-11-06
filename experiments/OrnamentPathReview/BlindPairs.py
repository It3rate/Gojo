"""Render preregistered drawing pairs. Keep the key separate from reviewers' images."""
import argparse
import csv
import gzip
import hashlib
import json
import subprocess
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


def main(root, runtime):
    review=root/"visual-review"
    private=review/"key"
    public=review/"panels"
    private.mkdir(parents=True,exist_ok=True)
    public.mkdir(exist_ok=True)
    design=json.loads((root/"design/design.json").read_text(encoding="utf-8-sig"))
    with gzip.open(root/"blind-cases.csv.gz","rt",encoding="utf-8-sig",newline="") as f:
        cases={(r["brief"],r["arm"],int(r["replicate"]),r["scenario"]):r for r in csv.DictReader(f)}
    requests=[]
    key=[]
    pairings=[]
    for ri in range(4):
        for bi,brief in enumerate(design["briefs"]):
            s=design["blind"][2*((3*bi+ri)%12)]
            pair=f"P{len(pairings)+1:02d}"
            arms=["full","field" if ri%2==0 else "random"]
            if hashlib.sha256(("path-blind-720411/"+pair).encode()).digest()[0]&1:
                arms.reverse()
            pairings.append((pair,brief))
            for side,arm in zip("LR",arms):
                c=cases[brief["name"],arm,ri,s["name"]]
                requests.append(dict(id=pair+side,hex=c["genome"],input=s["input"],mode=0))
                key.append(dict(pair=pair,side=side,arm=arm,brief=brief["name"],utility=c["utility"],
                                genome=c["genome"],scenario=s["name"],trace=c["trace"]))
    (private/"key.json").write_text(json.dumps(key),encoding="utf-8")
    inputfile=private/"requests.json"
    output=private/"traces.json"
    inputfile.write_text(json.dumps(requests),encoding="utf-8")
    if not output.exists():
        subprocess.run(["dotnet",str(runtime),str(inputfile),str(output)],check=True)
    traces={r["id"]:r["trace"] for r in json.loads(output.read_text())}
    for r in key:
        # The full trace was serialized with the same convention used at assay.
        encoded=json.dumps(traces[r["pair"]+r["side"]],separators=(",",":")).encode()
        if hashlib.sha256(encoded).hexdigest().upper()!=r["trace"]: raise ValueError(r["pair"])
    render(public,pairings,traces)


def render(public,pairings,traces):
    font_file="C:/Windows/Fonts/segoeui.ttf"
    for page in range(5):
        img=Image.new("RGB",(1300,900),"#faf8f1")
        d=ImageDraw.Draw(img)
        def label(x,y,msg,size):
            d.text((x,y),msg,font=ImageFont.truetype(font_file,size),fill="#253C37")
        for j,(pair,b) in enumerate(pairings[page*4:page*4+4]):
            y=j*225
            label(22,y+5,f"{pair}    {b['name']}     {b['aim']}",18)
            paths=[traces[pair+side] for side in "LR"]
            ink=[[(trace["sites"][e["from"]],trace["sites"][e["to"]]) for e in trace["edges"]] for trace in paths]
            points=[p for side in ink for edge in side for p in edge]
            if not points: continue
            xmin=min(0,min(p["x"] for p in points))
            xmax=max(paths[0]["pitch"]*paths[0]["cells"],max(p["x"] for p in points))
            ymin=min(p["y"] for p in points)
            ymax=max(p["y"] for p in points)
            scale=min(570/max(1,xmax-xmin),140/max(1,ymax-ymin))
            for col,edges in enumerate(ink):
                x=43+650*col
                label(x,y+33,"Left" if col==0 else "Right",15)
                for a,bp in edges:
                    def xy(p):
                        return (x+(570-scale*(xmax-xmin))/2+(p["x"]-xmin)*scale,
                                y+59+(146-scale*(ymax-ymin))/2+(ymax-p["y"])*scale)
                    d.line([xy(a),xy(bp)],fill="#203C38",width=3)
            d.line((20,y+221,1280,y+221),fill="#d7d7ca")
        img.save(public/f"panel-{page+1}.png")


if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("run",type=Path)
    p.add_argument("runtime",type=Path)
    a=p.parse_args()
    main(a.run,a.runtime)
