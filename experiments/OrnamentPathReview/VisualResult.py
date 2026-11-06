"""Render the largest registered-score disagreement among unanimous reserve choices."""
import json
import sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont


def run(directory):
    load=lambda f:json.loads((directory/f).read_text(encoding="utf-8-sig"))
    results=load("reserve-result.json")
    disagreed=[r for r in results["rows"] if abs(r["target"])==1 and r["target"]*r["full"]<0]
    if not disagreed:
        return
    case=max(disagreed,key=lambda r:abs(r["full"]))
    pair=case["pair"]
    key={r["side"]:r for r in load("visual-review/key/key.json") if r["pair"]==pair}
    traces={r["id"][-1]:r["trace"] for r in load("visual-review/key/traces.json") if r["id"][:3]==pair}
    paths={k:[(v["sites"][e["from"]],v["sites"][e["to"]]) for e in v["edges"]] for k,v in traces.items()}
    points=[p for edges in paths.values() for edge in edges for p in edge]
    x0=min(0,min(p["x"] for p in points)); x1=max(traces["L"]["pitch"]*traces["L"]["cells"],max(p["x"] for p in points))
    y0=min(p["y"] for p in points);y1=max(p["y"] for p in points)
    scale=min(525/max(x1-x0,1),120/max(y1-y0,1))
    img=Image.new("RGB",(1200,315),"#f8f5ea"); d=ImageDraw.Draw(img)
    def text(x,y,s,size,bold=False):
        d.text((x,y),s,fill="#253c36",font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",size))
    text(32,13,"Where the judges and the visual observers disagreed",29,True)
    text(32,59,f"{pair}  /  {case['brief']}  /  Two equal-scale drawings on an unseen basis",19)
    for idx,side in enumerate("LR"):
        offset=35+idx*592
        preferred=side==("L" if case["target"]==1 else "R")
        text(offset,105,"Both observers preferred this" if preferred else "The full judge preferred this",21,True)
        text(offset,137,key[side]["arm"].replace("+"," + ")+" selection",17)
        for edge in paths[side]:
            def project(p):
                return offset+(525-(x1-x0)*scale)/2+(p["x"]-x0)*scale,176+(120-(y1-y0)*scale)/2+(y1-p["y"])*scale
            d.line([project(p) for p in edge],fill="#204c43",width=3)
    img.save(directory/"PreferencePair.png")


if __name__=="__main__":
    run(Path(sys.argv[1]))
