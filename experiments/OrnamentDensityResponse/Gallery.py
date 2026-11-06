"""Render the same frozen law drawing against signed positive, zero and negative requests."""
import argparse
import html
import json
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def draw(run,name,rows):
    scale=2
    width,height=1300,126+200*len(rows)
    image=Image.new("RGB",(width*scale,height*scale),"#faf7ee")
    brush=ImageDraw.Draw(image)
    svg=[f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" role="img">'
         f'<title>{html.escape(name)}</title><rect width="100%" height="100%" fill="#faf7ee"/>']
    def label(x,y,text,size=17,bold=False,color="#23483e"):
        font=ImageFont.truetype("C:/Windows/Fonts/segoeui"+("b" if bold else "")+".ttf",size*scale)
        brush.text((x*scale,y*scale),text,font=font,fill=color,anchor="ls")
        svg.append(f'<text font-family="system-ui" font-size="{size}" font-weight="{700 if bold else 400}" '
                   f'x="{x}" y="{y}" fill="{color}">{html.escape(text)}</text>')
    label(25,39,name,29,True)
    label(25,72,"Each row uses one scale. Only the sign of the two reach-pressure injections changes.",17)
    for i,title in enumerate(("Ask for less","No request","Ask for more")):label(40+i*420,111,title,19,True)
    for row_index,row in enumerate(rows):
        y=127+200*row_index
        label(25,y+18,f"{row['family']}   /   {row['selection']}   /   {row['s']['key']}",16,True)
        views=sorted(row["traces"],key=lambda v:v["sign"])
        strokes=[]
        for v in views:
            t=v["trace"]
            strokes.append([(t["sites"][e["from"]],t["sites"][e["to"]],e["owner"]) for e in t["edges"]])
        points=[p for panel in strokes for a,b,o in panel for p in (a,b)]
        xmin=min([0]+[p["x"] for p in points]);xmax=max([row["s"]["input"]["pitch"]*row["s"]["input"]["cells"]]+[p["x"] for p in points])
        ymin=min([0]+[p["y"] for p in points]);ymax=max([0]+[p["y"] for p in points])
        scalar=min(378/max(1,xmax-xmin),98/max(1,ymax-ymin))
        for panel,edges in enumerate(strokes):
            def project(p):
                return (35+420*panel+(379-(xmax-xmin)*scalar)/2+(p["x"]-xmin)*scalar,
                        y+34+(101-(ymax-ymin)*scalar)/2+(ymax-p["y"])*scalar)
            for a,b,owner in edges:
                color=("#204d42","#9b5031","#405f95")[owner%3]
                x1,y1=project(a);x2,y2=project(b)
                brush.line((x1*scale,y1*scale,x2*scale,y2*scale),fill=color,width=4)
                svg.append(f'<path d="M{x1:.2f} {y1:.2f}L{x2:.2f} {y2:.2f}" fill="none" stroke="{color}" stroke-width="2"/>')
            m=views[panel]["metrics"]
            label(35+420*panel,y+162,f"Length {m['length']:.1f}    Broad {m['broad']:.2f}    Actions {m['actions']}",15)
        label(26,y+193,f"Difference in lengths / pitch: {row['gain']:+.2f}     Response read: {row['response']:.2f}",14)
    svg.append("</svg>")
    (run/(name+".svg")).write_text(''.join(svg),encoding="utf-8")
    image.resize((width,height),Image.Resampling.LANCZOS).save(run/(name+".png"))


def build(run):
    rows=load(run/"reserve/exhibits.json")
    registered=[r for r in rows if r["selection"]=="registered"]
    best=max(rows,key=lambda r:r["gain"]);worst=min(rows,key=lambda r:r["gain"])
    draw(run,"Density requests",registered)
    draw(run,"Largest and smallest changes",[best,worst] if best is not worst else [best])
    page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Does the pen answer a density request?</title><style>body{margin:2em auto;padding:0 1em;max-width:1260px;background:#faf7ee;color:#23483e;font:17px/1.55 system-ui}h1,h2{font-family:Georgia,serif}img{width:100%}a{color:#176246}</style><h1>Does the pen answer a density request?</h1>
<p>Each set uses the same engine, same policy, same physical inputs and the same shared clock. The frieze receives a request twice for less or more reach. A divergence must be caused by the changed input; utility of that divergence is a separate question.</p>
<img src="Density requests.svg" alt="Four preregistered reserve examples with negative, zero and positive requests.">
<h2>Range of outcomes</h2><p>These are the largest and smallest signed changes among the four validation-selected machines. Labels identify the post-hoc diagnostic selection. One can change its geometry with no corresponding change in total unique length.</p>
<img src="Largest and smallest changes.svg" alt="Strongest and weakest realized response.">
<p><a href="reserve/verdicts.json">Registered outcomes</a> · <a href="reserve/groups.csv">Parent comparisons</a> · <a href="reserve/exhibits.json">Complete native strokes, inputs and pressure traces</a></p></html>'''
    (run/"Gallery.html").write_text(page,encoding="utf-8")


if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("run",type=Path);build(p.parse_args().run)
