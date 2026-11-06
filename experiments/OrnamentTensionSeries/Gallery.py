"""Render the registered eight-member series from saved native vector traces."""
import csv
import html
import json
import sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

MODES=["Live","TrayOnly","PulseOnly","Hold","Clock"]
COLORS=["#1b5549","#a45031","#385d80"]
PAPER="#faf7ef"

def load(p):return json.loads(p.read_text(encoding="utf-8-sig"))
def geom(step):
    t=step["trace"];s=t["sites"]
    return [(s[e["from"]],s[e["to"]],e["owner"]) for e in t["edges"]]

def build(root):
    table=load(root/"reserve/examples.json")
    families=sorted({t["sheet"]["tray"]["family"] for t in table})
    mode_data={ (t["sheet"]["tray"]["family"],MODES[t["sheet"]["mode"]]):t for t in table}
    with (root/"reserve/collections.csv").open(encoding="utf-8-sig",newline="") as f:
        names=list(dict.fromkeys(r["family"] for r in csv.DictReader(f)))
    footer=lambda s: f"tray {s['move']['tool']+1} · byte {s['publication']:02X} · pulse {s['move']['pulse']:+d}"
    renders={}
    for family in families:
        all_edges=[p for mode in MODES for step in mode_data[family,mode]["steps"]
                   for a,b,_ in geom(step) for p in (a,b)]
        if not all_edges: all_edges=[{"x":0,"y":0},{"x":1,"y":1}]
        xmin=min(p["x"] for p in all_edges);xmax=max(p["x"] for p in all_edges)
        ymin=min(p["y"] for p in all_edges);ymax=max(p["y"] for p in all_edges)
        scale=min(285/max(1,xmax-xmin),82/max(1,ymax-ymin))
        for mode in MODES:
            steps=mode_data[family,mode]["steps"];cards=[]
            for i,s in enumerate(steps):
                def xy(p):return (10+(285-(xmax-xmin)*scale)/2+(p["x"]-xmin)*scale,
                                  9+(82-(ymax-ymin)*scale)/2+(ymax-p["y"])*scale)
                segs=[(*xy(a),*xy(b),COLORS[o]) for a,b,o in geom(s)]
                cards.append((s,segs))
            renders[family,mode]=cards
    body=[]
    for family in families:
        for mode in MODES:
            tiles=[]
            for i,(s,segments) in enumerate(renders[family,mode]):
                paths="".join(f'<path d="M{x:.3f} {y:.3f} L{u:.3f} {v:.3f}" stroke="{color}" stroke-width="1.85"/>' for x,y,u,v,color in segments)
                tiles.append(f'<figure><svg viewBox="0 0 305 102"><title>Drawing {i+1}</title>{paths}</svg><figcaption>{i+1} · {footer(s)}</figcaption></figure>')
            body.append(f'<section data-family="{names[family]}" data-mode="{mode}"><h2>{names[family]} · {mode}</h2><div class="tiles">{"".join(tiles)}</div></section>')
    family_opts="".join(f'<option>{name}</option>' for name in names)
    mode_opts="".join(f'<option>{name}</option>' for name in MODES)
    result=f'''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Series memory</title>
<style>body{{max-width:1300px;margin:3em auto;padding:0 18px;background:{PAPER};color:#263b35;font:16px/1.45 system-ui}}h1,h2{{font-family:Georgia}}p{{max-width:82ch}}.tiles{{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px}}figure{{margin:0;padding:10px;border:1px solid #d4d5c7}}svg{{width:100%}}figcaption{{font-size:13px;color:#415849}}select{{font:inherit;padding:5px}}@media(max-width:800px){{.tiles{{grid-template-columns:repeat(2,minmax(0,1fr))}}}}</style>
<h1>Eight drawings, one remembered surface</h1><p>Each drawing is a fresh native execution. The series owner can remember two masked frieze bytes, choose between two stored machines, and send a signed pulse to the next frieze. Each pen color denotes an owner within one machine. All eight drawings and all controls for a family use the same display scale.</p>
<p>The publication below a drawing is what the next decision can read. The displayed pulse and tray are the inputs to the current drawing. These are fixed examples from the first reserved parent at five vertical slots. Select a mode to compare the same inputs.</p>
<label>Engine <select id="family">{family_opts}</select></label> <label>Mode <select id="mode">{mode_opts}</select></label>
{"".join(body)}<script>const f=document.getElementById('family'),m=document.getElementById('mode');function update(){{document.querySelectorAll('section').forEach(s=>s.hidden=s.dataset.family!==f.value||s.dataset.mode!==m.value)}}f.onchange=m.onchange=update;update()</script></html>'''
    (root/"Gallery.html").write_text(result,encoding="utf-8")
    # Eight registered families, first four live members of eight; no repetition introduced.
    width,height=1320,145+len(families)*119
    img=Image.new("RGB",(width,height),PAPER);d=ImageDraw.Draw(img)
    def text(x,y,s,size,bold=False):d.text((x,y),s,font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",size),fill="#233d34")
    text(24,14,"A series owner choosing a machine and sending pressure",31,True)
    text(24,63,"Members 1–4 of eight. Each family row uses a shared drawing scale. Colors distinguish pens.",19)
    for j,family in enumerate(families):
        y=112+j*119
        text(20,y,names[family],15,True)
        for col,(s,segs) in enumerate(renders[family,"Live"][:4]):
            left=20+col*326
            for x,v,u,w,color in segs:d.line((left+x,y+17+v*.74,left+u,y+17+w*.74),fill=color,width=2)
            text(left,y+94,footer(s),12)
    img.save(root/"ContactSheet.png")


if __name__=="__main__":build(Path(sys.argv[1]))
