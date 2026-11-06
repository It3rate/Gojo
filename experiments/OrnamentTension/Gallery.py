"""Local plates of native paths, pressure comparisons and material-joint controls."""
import csv
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

PAPER="#f9f6ed"; INK="#183e37"; MID="#54675e"; RUST="#aa5437"; LINE="#d2d4c5"
PENS=[INK,"#995136","#3f6383"]

def load(p):
    return json.loads(p.read_text(encoding="utf-8-sig"))

def font(size,bold=False):
    return ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",size)

def bounds(traces):
    points=[tr["sites"][e[k]] for tr in traces for e in tr["edges"] for k in ("from","to")]
    return (min([0]+[p["x"] for p in points]),max([tr["pitch"]*tr["cells"] for tr in traces]+[p["x"] for p in points]),
            min([0]+[p["y"] for p in points]),max([0]+[p["y"] for p in points]))

def draw_trace(draw,tr,frame,box,color=True):
    l,r,b,t=frame; x,y,w,h=box;scale=min(w/max(1,r-l),h/max(1,t-b))
    def xy(p):return x+(w-(r-l)*scale)/2+(p["x"]-l)*scale,y+(h-(t-b)*scale)/2+(t-p["y"])*scale
    for e in tr["edges"]:
        draw.line([xy(tr["sites"][e["from"]]),xy(tr["sites"][e["to"]])],fill=PENS[e["owner"]%3] if color else INK,width=3)

def pictures(folder,foundations,pairs,joints):
    atlas=Image.new("RGB",(1340,870),PAPER);d=ImageDraw.Draw(atlas)
    d.text((32,18),"Eight named construction engines",font=font(38,True),fill=INK)
    d.text((34,72),"Authored starts. Every four-bay drawing ran its own rows. Colors follow pens.",font=font(20),fill=MID)
    for i,f in enumerate(foundations):
        x=30+(i%2)*652;y=125+(i//2)*181;t=f["trace"]
        d.line((x,y,x+616,y),fill=LINE);d.text((x,y+7),f["engine"].replace("-"," "),font=font(20,True),fill=INK)
        draw_trace(d,t,bounds([t]),(x+12,y+49,584,109))
    atlas.save(folder/"Engines.png")
    pic=Image.new("RGB",(1270,620),PAPER);d=ImageDraw.Draw(pic)
    d.text((32,16),"When feedback changes the ink",font=font(36,True),fill=INK)
    d.text((34,71),"Same program and input on each row. These are the largest paired gain and loss on the broad read.",font=font(19),fill=MID)
    for i,p in enumerate(pairs[:2]):
        y=123+i*239; items=p["items"];frame=bounds([v["draw"]["trace"] for v in items])
        d.line((30,y,1240,y),fill=LINE)
        d.text((34,y+9),p["title"]+"  /  "+items[0]["row"]["family"].replace("-"," "),font=font(22,True),fill=INK)
        for j,it in enumerate(items):
            x=36+621*j
            d.text((x,y+48),f"{it['mode']}  ·  {float(it['row']['broad']):.3f}  ·  {it['row']['edges']} strokes",font=font(18),fill=MID)
            draw_trace(d,it["draw"]["trace"],frame,(x+6,y+86,576,123))
    pic.save(folder/"Pressure.png")
    fig=Image.new("RGB",(1240,335),PAPER);d=ImageDraw.Draw(fig)
    d.text((30,15),"One visible X, different construction histories",font=font(32,True),fill=INK)
    d.text((32,61),"Same projected rays in the first four panels. Identities and incidence distinguish the joints.",font=font(18),fill=MID)
    for i,j in enumerate(joints[:4]):
        x=30+305*i; t=j["trace"];p=[t["sites"][e[k]] for e in t["edges"] for k in ("from","to")]
        frame=(min(q["x"] for q in p),max(q["x"] for q in p),min(q["y"] for q in p),max(q["y"] for q in p))
        draw_trace(d,t,frame,(x+24,102,230,143))
        d.text((x+20,260),["Two opposed Vs","Straight crossing","One shared site","Four arrivals"][i],font=font(18,True),fill=INK)
    fig.save(folder/"Joints.png")

def main(root):
    w=root/"wave1"
    data={k:load(w/name) for k,name in (("foundations","foundations.json"),("exhibits","exhibits.json"),
        ("joints","joints.json"),("analysis","analysis.json"),("pairs","diagnostics/pairs.json"))}
    data["panels"]=list(csv.DictReader((w/"panel-use.csv").open(encoding="utf-8")))
    pictures(w,data["foundations"],data["pairs"],data["joints"])
    html=Path(__file__).with_name("Gallery.html").read_text(encoding="utf-8")
    html=html.replace("/*__NATIVE__*/",json.dumps(data,separators=(",",":"),ensure_ascii=False).replace("<","\\u003c"))
    (w/"Explore.html").write_text(html,encoding="utf-8")

if __name__=="__main__":
    main(Path(sys.argv[1]))
