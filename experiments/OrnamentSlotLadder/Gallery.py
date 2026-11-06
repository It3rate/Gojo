"""Render six actually executed bays. All widths of each engine share one scale."""
import html
import json
import sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

COLORS=["#234a3e","#a35839","#315f86"]
TITLES=["Key with attached limbs","Opposed inward spirals","Alternating mirrored crests"]

def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def render(root,mode):
    records=load(root/"exhibits.json")
    records=[r for r in records if r["case"]["mode"]==mode]
    width,height=1480,835; paper="#faf7eb";ink="#244239"
    img=Image.new("RGB",(width,height),paper);d=ImageDraw.Draw(img)
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" role="img"><title>Three width-aware engines: {mode}</title>',
         f'<rect width="{width}" height="{height}" fill="{paper}"/>']
    def label(x,y,s,sz,bold=False):
        d.text((x,y),s,font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",sz),fill=ink)
        svg.append(f'<text x="{x}" y="{y+sz}" font-family="system-ui,sans-serif" font-size="{sz}" font-weight="{700 if bold else 400}" fill="{ink}">{html.escape(s)}</text>')
    label(29,10,"What can extra vertical slots actually open?",35,True)
    label(29,68,f"{mode}    /    Fixed bytes and extent bindings across each row. Six executed bays; colors distinguish pens.",19)
    for i,c in enumerate(["2 slots","5 slots","9 slots"]):label(31+485*i,112,c,23,True)
    for engine in range(3):
        series=sorted([r for r in records if r["case"]["engine"]==engine],key=lambda r:r["case"]["slots"])
        sites=[p for r in series for p in r["draw"]["trace"]["sites"]]
        x0=min(0,min(p["x"] for p in sites));x1=max(series[0]["case"]["input"]["pitch"]*6,max(p["x"] for p in sites))
        y0=min(p["y"] for p in sites);y1=max(p["y"] for p in sites)
        scale=min(423/max(1,x1-x0),130/max(1,y1-y0))
        top=152+221*engine
        for col,r in enumerate(series):
            c=r["case"]; tr=r["draw"]["trace"]
            x=31+col*485
            label(x,top,TITLES[engine],18,True)
            label(x,top+31,f'{c["owners"]} active  ·  {c["edges"]} strokes  ·  {c["optionalEdges"]} optional',15)
            def xy(pt):
                return x+(423-(x1-x0)*scale)/2+(pt["x"]-x0)*scale,top+65+(135-(y1-y0)*scale)/2+(y1-pt["y"])*scale
            for edge in tr["edges"]:
                a=xy(tr["sites"][edge["from"]]);b=xy(tr["sites"][edge["to"]]);color=COLORS[edge["owner"]]
                d.line([a,b],fill=color,width=3)
                svg.append(f'<path d="M{a[0]:.2f} {a[1]:.2f}L{b[0]:.2f} {b[1]:.2f}" fill="none" stroke="{color}" stroke-width="2.7" stroke-linejoin="round"/>')
    label(31,807,"Constructed examples. The gates, rather than learning, specify when each optional stream can participate.",16)
    svg.append("</svg>")
    (root/f"{mode}.svg").write_text("".join(svg),encoding="utf-8")
    if mode=="Ladder":img.save(root/"Compare.png")

def build(root):
    for mode in ["Ladder","CapacityOnly","AlwaysOn","SpineOnly"]:render(root,mode)
    intro='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Drawing from width</title>
<style>body{max-width:1450px;margin:2em auto;padding:0 20px;color:#264238;background:#faf7eb;font:17px/1.55 system-ui}h1{font:42px Georgia,serif}p{max-width:78ch}img{width:100%;display:block}a{color:#28644d}select{font:inherit}</style>
<h1>Drawing from available width</h1><p>Compare the same program in three physical frames. Its spine reads slot one. The second pen can ask whether slot four exists, and the third whether slot eight exists. No external judge participates. The contact sheet uses the same physical scale across each row.</p>
<label>Connection <select id="mode"><option>Ladder</option><option>CapacityOnly</option><option>AlwaysOn</option><option>SpineOnly</option></select></label>
<p id="caption"></p><img id="sheet" alt="Actual drawings for the three widths and three engine families"><p><a href="Protocol.md">Registered protocol</a> · <a href="cases.json">All 216 outcomes</a> · <a href="exhibits.json">Edges, sites and history</a></p>
<script>const labels={Ladder:'The three owners read the actual first, fourth and eighth slots, and all must fit the physical frame.',CapacityOnly:'All slots publish present. Out-of-frame strokes can still be blocked. Actual height alone decides what fits.',AlwaysOn:'All slots publish present and the physical boundary is withdrawn. The three columns have equivalent drawing conditions.',SpineOnly:'Only the spine receives permission. Physical room remains available to each column.'};
const select=document.getElementById('mode'),image=document.getElementById('sheet'),caption=document.getElementById('caption');function refresh(){image.src=select.value+'.svg';caption.textContent=labels[select.value]}select.onchange=refresh;refresh()</script></html>'''
    (root/"Gallery.html").write_text(intro,encoding="utf-8")

if __name__=="__main__":build(Path(sys.argv[1]))
