"""Compare registered source geometry and saved pen histories at one scale per figure."""
import argparse
import hashlib
import html
import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

TOOLS=Path(__file__).resolve().parent
ARMS=["authored-1","authored-2","printed-start","library-history","library-generic"]
LABELS={"authored-1":"Figure 1 construction","authored-2":"Figure 2 construction",
        "printed-start":"Figure 1 prior + figure 1 teacher","library-history":"Catalogue + figure 1 teacher",
        "library-generic":"Catalogue + general teacher"}
INK=["#254941","#8d4a33","#446180"]

def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))

def support(trace):
    sites=trace["sites"]
    return [(sites[e["from"]],sites[e["to"]],e["owner"]) for e in trace["edges"]]

def write_figure(rows,figure,rep,mode,output):
    selected=[r for r in rows if r["figure"]==figure and r["mode"]==mode and r["rep"] in (-1,rep)]
    control=f"authored-{figure}"
    names=[control, "printed-start","library-history","library-generic"]
    panels=[next(r for r in rows if r["figure"]==figure and r["mode"]=="Quiet" and r["arm"]==control)]
    panels.extend(next(r for r in selected if r["arm"]==n) for n in names)
    allsites=[site for r in panels for a,b,_ in support(r["trace"]) for site in (a,b)]
    left=min(0,min(s["x"] for s in allsites)); right=max(panels[0]["trace"]["pitch"]*4,max(s["x"] for s in allsites))
    low=min(0,min(s["y"] for s in allsites)); high=max(s["y"] for s in allsites)
    scale=min(740/max(1,right-left),100/max(1,high-low))
    width,height=1130,826
    paper="#faf7eb"
    img=Image.new("RGB",(width,height),paper)
    draw=ImageDraw.Draw(img)
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" role="img"><title>Meyer plate 81, figure {figure}, replicate {rep+1}</title>',
         f'<rect width="{width}" height="{height}" fill="{paper}"/>']
    def text(x,y,msg,size=16,bold=False):
        face="C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf"
        draw.text((x,y),msg,font=ImageFont.truetype(face,size),fill="#203e36")
        svg.append(f'<text x="{x}" y="{y+size}" font-family="system-ui,sans-serif" font-size="{size}" font-weight="{700 if bold else 400}" fill="#203e36">{html.escape(msg)}</text>')
    text(28,16,f"Meyer, plate 81 · figure {figure}",31,True)
    text(28,61,f"{'Training figure' if figure==1 else 'Transfer figure'}   /   new physical basis   /   replicate {rep+1}   /   {mode.lower()}   /   wide frame   /   four executed bays")
    for j,r in enumerate(panels):
        top=111+138*j
        # The geometry and pen width remain fixed across all four rows in a plate.
        label=(f"Printed centerline · quiet reference" if j==0 else
               f"Supplied program · {mode.lower()}" if j==1 else LABELS[r["arm"]])
        text(29,top,label,17,True)
        s=r["scores"]
        text(29,top+28,f"Line match {s['ink']:.2f}    Order {s['order']:.2f}",15)
        text(29,top+55,f"Seams {s['handoffs']} / 3    Overlaps {s['overlaps']}",15)
        def xy(point):
            return 355+(point["x"]-left)*scale,top+10+(.5*(105-(high-low)*scale))+(high-point["y"])*scale
        for a,b,owner in support(r["trace"]):
            x,y=xy(a);xx,yy=xy(b)
            draw.line((x,y,xx,yy),fill=INK[owner],width=3)
            svg.append(f'<path d="M{x:.2f} {y:.2f}L{xx:.2f} {yy:.2f}" fill="none" stroke="{INK[owner]}" stroke-width="2.4"/>')
    text(28,793,"Same scale and origin for all rows. Colors distinguish pens. A match measures line support, not historical pen order.",15)
    svg.append("</svg>")
    stem=output/f"Figure{figure}-{mode}-r{rep+1}"
    stem.with_suffix(".svg").write_text("".join(svg),encoding="utf-8")
    if rep==0 and mode=="Live": img.save(stem.with_suffix(".png"))

def bank(trace,unit):
    # Draw both outlines of an idealized broad pen. Resume only across actual
    # identical material endpoints, never a coordinate guess.
    out=[];sites=trace["sites"];ends={};paths={}
    for e in trace["edges"]:
        owner=e["owner"];a,b=sites[e["from"]],sites[e["to"]]
        if ends.get(owner)!=e["from"]:
            paths.setdefault(owner,[]).append([a])
        paths[owner][-1].append(b);ends[owner]=e["to"]
    for runs in paths.values():
        for run in runs:
            points=" ".join(f'{v["x"]},{-v["y"]}' for v in run)
            for color,thick in (("#22473e",unit*1.08),("#faf7eb",unit*.86)):
                out.append(f'<polyline points="{points}" stroke="{color}" stroke-width="{thick:.3f}" fill="none" stroke-linejoin="miter"/>')
    width=trace["cells"]*trace["pitch"]; extent=max(p["y"] for p in sites)
    return f'<svg viewBox="{-unit} {-extent-unit} {width+2*unit} {extent+2*unit}" role="img">{"".join(out)}</svg>'

def printed_comparison(rows,images):
    """A compact source/result view; the two vector examples share a scale per row."""
    img=Image.new("RGB",(1260,514),"#faf7eb");draw=ImageDraw.Draw(img)
    def label(x,y,msg,size,bold=False):
        draw.text((x,y),msg,font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",size),fill="#223d36")
    label(29,15,"A printed repeat and the program it leaves behind",32,True)
    label(29,65,"Meyer’s plate 81  /  The first fret was used for training; the second was held out.",19)
    for col,title in enumerate(["Printed banks","Supplied construction","Figure 1 trained machine"]):
        label([30,451,853][col],113,title,22,True)
    for f in (1,2):
        y=153+(f-1)*165
        label(30,y,f"Figure {f}  ·  {'training' if f==1 else 'transfer'}",16)
        crop=Image.open(images/f"Printed{f}.png")
        crop.thumbnail((375,95),Image.Resampling.LANCZOS);img.paste(crop,(30,y+40))
        native=next(r for r in rows if r["figure"]==f and r["arm"]==f"authored-{f}" and r["mode"]=="Quiet")
        trained=next(r for r in rows if r["figure"]==f and r["arm"]=="printed-start" and r["rep"]==0 and r["mode"]=="Live")
        paths=[support(r["trace"]) for r in (native,trained)]
        pts=[p for path in paths for edge in path for p in edge[:2]]
        x0=min(0,min(p["x"] for p in pts));x1=max(native["trace"]["pitch"]*4,max(p["x"] for p in pts))
        y0=min(p["y"] for p in pts);y1=max(p["y"] for p in pts)
        k=min(350/max(1,x1-x0),87/max(1,y1-y0))
        for j,(r,path) in enumerate(zip((native,trained),paths)):
            x=(451,853)[j]
            label(x,y,f"Line match {r['scores']['ink']:.2f}   /   Seams {r['scores']['handoffs']} of 3",16)
            def project(p):
                return x+(350-(x1-x0)*k)/2+(p["x"]-x0)*k,y+32+(95-(y1-y0)*k)/2+(y1-p["y"])*k
            for a,b,o in path:draw.line([project(a),project(b)],fill=INK[o],width=3)
    label(30,475,"Printed fragments are cropped independently. Native and trained drawings share one scale in each row. Replicate 1, first nominal basis.",16)
    img.save(images/"PrintedComparison.png")

def gallery(folder,sources,source_render=None):
    rows=load(folder/"final/drawings.json")
    images=folder/"images";images.mkdir(exist_ok=True)
    for fig in (1,2):
        for rep in range(4):
            for mode in ("Live","Quiet"):write_figure(rows,fig,rep,mode,images)
    source_render=source_render or sources/"plate-05.png"
    source_page=Image.open(source_render)
    # Render from original-resolution page coordinates to keep historical evidence
    # independent from the registered analytical reconstruction.
    scale=lambda rect:tuple(round(a*(source_page.width/814 if i%2==0 else source_page.height/1150)) for i,a in enumerate(rect))
    rects={1:(473,210,696,272),2:(473,331,696,411)}
    for fig,rect in rects.items():source_page.crop(scale(rect)).resize((669,3*(rect[3]-rect[1])),Image.Resampling.LANCZOS).save(images/f"Printed{fig}.png")
    citation={"work":"Meyer, Systematisch geordnetes Handbuch der Ornamentik, 1895, Tafel 81, figures 1 and 2",
              "source":"https://digital.ub.uni-paderborn.de/download/pdf/7739354.pdf",
              "local_pdf_page":5,"source_pdf_sha256":hashlib.sha256((sources/"meyer-bands.pdf").read_bytes()).hexdigest(),
              "render_sha256":hashlib.sha256(source_render.read_bytes()).hexdigest(),
              "render_size":list(source_page.size),"crop_boxes":{str(f):scale(rect) for f,rect in rects.items()}}
    (images/"source.json").write_text(json.dumps(citation,indent=2),encoding="utf-8")
    printed_comparison(rows,images)
    blocks=[]
    for fig in (1,2):
        r=next(r for r in rows if r["figure"]==fig and r["arm"]==f"authored-{fig}" and r["mode"]=="Quiet")
        blocks.append(f'<section><h2>Figure {fig} · {"training reference" if fig==1 else "held-out reference"}</h2><div class="compare"><figure><img src="images/Printed{fig}.png"><figcaption>Printed banks, cropped from the completed part of the plate.</figcaption></figure><figure>{bank(r["trace"],r["trace"]["pitch"]/(6 if fig==1 else 8))}<figcaption>Both banks of a broad pen around the executed centerline. Width is a rendering convention.</figcaption></figure></div></section>')
    markup='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Two printed frets</title>
<style>body{max-width:1150px;margin:2.5em auto;padding:0 22px;background:#faf7eb;color:#284239;font:17px/1.55 system-ui}h1,h2{font-family:Georgia,serif}h1{font-size:48px}a{color:#245b4a}figure{margin:0}.compare{display:grid;grid-template-columns:1fr 1fr;gap:24px;align-items:center}img,svg{display:block;width:100%}figcaption{font-size:15px}select{font:inherit;padding:4px}section{margin:28px 0}label{display:inline-block;margin-right:16px}@media(max-width:700px){.compare{grid-template-columns:1fr}}</style>
<h1>Two printed frets</h1><p>These are two angular courses reconstructed from the surviving banks of Meyer’s plate 81. The first figure was available during training. The second was scored only after the programs were locked. Both come from the same plate. A selected chronological interpretation is required to train on order.</p>'''
    markup+="".join(blocks)
    markup+='''<h2>Compare executed machines</h2><label>Figure <select id="fig"><option>1</option><option selected>2</option></select></label><label>Replicate <select id="rep"><option>1</option><option>2</option><option>3</option><option>4</option></select></label><label>Feedback <select id="mode"><option>Live</option><option>Quiet</option></select></label><img id="sheet" alt="Four recorded drawings from one basis with score and seam counts"><script>const el=id=>document.getElementById(id);function show(){el('sheet').src='images/Figure'+el('fig').value+'-'+el('mode').value+'-r'+el('rep').value+'.svg'};for(const s of document.querySelectorAll('select'))s.onchange=show;show()</script><p><a href="final/summary.json">Read all scores</a> · <a href="final/verdicts.json">Registered predictions</a> · <a href="https://digital.ub.uni-paderborn.de/ihd/content/structure/7730456">Meyer’s source</a></p></html>'''
    (folder/"Gallery.html").write_text(markup,encoding="utf-8")

if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("run",type=Path);p.add_argument("source_folder",type=Path)
    p.add_argument("--source-render",type=Path,help="Optional higher-resolution rendering of PDF page 5.")
    args=p.parse_args();gallery(args.run,args.source_folder,args.source_render)
