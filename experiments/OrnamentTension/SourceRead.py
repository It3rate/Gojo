"""Render the preselected anonymous drawings and named reference packets for shape retrieval."""
import hashlib
import json
import sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def strokes(trace):
    return [[(trace["sites"][e[key]]["x"],trace["sites"][e[key]]["y"]) for key in ("from","to")] for e in trace["edges"]]


def fit(draw,segments,box,color,width=3):
    points=[p for seg in segments for p in seg]
    if not points: return
    x0,y0,x1,y1=box
    left=min(x for x,y in points);right=max(x for x,y in points)
    lo=min(y for x,y in points);hi=max(y for x,y in points)
    scale=min((x1-x0)/max(right-left,1),(y1-y0)/max(hi-lo,1))
    def project(p):
        return (x0+(x1-x0-scale*(right-left))/2+(p[0]-left)*scale,
                y0+(y1-y0-scale*(hi-lo))/2+(hi-p[1])*scale)
    for seg in segments: draw.line([project(p) for p in seg],fill=color,width=width)


def prepare(root):
    exhibits=read(root/"wave1/exhibits.json")
    public=root/"source-read/panels"
    public.mkdir(parents=True,exist_ok=True)
    key=[]
    records=sorted((e for e in exhibits if e["arm"]=="coupled"),key=lambda e:
                   hashlib.sha256(("native-source-2606"+e["id"]).encode()).hexdigest())
    f=ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf",19)
    for page in range(4):
        img=Image.new("RGB",(1100,660),"#faf8ef");draw=ImageDraw.Draw(img)
        for j,record in enumerate(records[page*4:page*4+4]):
            code=f"D{page*4+j+1:02}";key.append({"code":code,"id":record["id"],"family":record["engine"]})
            y=j*165;draw.text((15,y+12),code,font=f,fill="#1c3c3b")
            fit(draw,strokes(record["trace"]),(85,y+16,1060,y+148),"#1d3e3b")
            draw.line((10,y+163,1090,y+163),fill="#d3d5c9")
        img.save(public/f"drawings-{page+1}.png")
    (root/"source-read/key.json").write_text(json.dumps(key,indent=2),encoding="utf-8")
    examples=[r for r in read(root/"history-reference/packets.json")["examples"] if r["holdout"]]
    img=Image.new("RGB",(1250,760),"#faf8ef");draw=ImageDraw.Draw(img)
    for i,r in enumerate(examples):
        x=(i%2)*625;y=(i//2)*190
        draw.text((x+18,y+10),r["family"],font=f,fill="#1d3e3b")
        paths=[[(int(px),int(py)) for px,py in (p.split(",") for p in s.split())] for s in r["paths"]]
        rendered=[[(px+n*r["pitch"],py) for px,py in path] for n in range(3) for path in paths]
        fit(draw,rendered,(x+22,y+48,x+596,y+171),"#405b52")
    img.save(public/"reference-families.png")


if __name__=="__main__":
    prepare(Path(sys.argv[1]))
