"""Build the local experiment book from the recorded Markdown and actual galleries."""
import html
import json
import os
import re
import sys
from pathlib import Path
from markdown import markdown


def build(root):
    root=root.resolve()
    project=Path(__file__).resolve().parent
    docs={project/(name+".md"):root/(display+".html") for name,display in
          (("Results","Results"),("Measurements","Measurements"),("Criteria","Criteria"),("HistoricalResults","Sources"),
           ("Engines","Engines"),("Runtime","Runtime"),("Protocol","Protocol"))}
    docs[root/"wave1/JointAssociations.md"]=root/"wave1/joint-associations.html"
    docs.update({project.parent/name/"README.md":root/(slug+".html") for name,slug in
                 (("OrnamentCommitteeStudy","Committee"),("OrnamentDensityResponse","Density"),
                  ("OrnamentTensionSeries","Series"),("OrnamentPlateStudy","Printed"),
                  ("OrnamentSlotLadder","Width"),("OrnamentRouteDiagnosis","Route"))})
    article_style="body{max-width:980px;margin:42px auto;padding:0 20px;font:17px/1.6 system-ui;color:#1c3c36;background:#faf7ee;overflow-wrap:anywhere}h1,h2{font-family:Georgia,serif;line-height:1.15}a{color:#1e634d}table,pre{display:block;overflow:auto;max-width:100%}td,th{border-bottom:1px solid #c4cabc;padding:.4em;text-align:left}img,svg{max-width:100%;height:auto}code{overflow-wrap:anywhere}"
    def article(doc,page):
        def relative(match):
            ref=match[1]
            if "://" in ref or ref.startswith("#"):return match[0]
            target=(doc.parent/ref).resolve()
            plate=project.parents[1]/"artifacts/local-tension-plate/primary"
            if target.is_relative_to(plate):target=root/"plate"/target.relative_to(plate)
            target=docs.get(target,target)
            return 'href="'+html.escape(os.path.relpath(target,page.parent).replace(os.sep,"/"),quote=True)+'"'
        body=markdown(doc.read_text(encoding="utf-8-sig"),extensions=["tables","fenced_code"])
        page.write_text('<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+
            html.escape(doc.stem)+'</title><style>'+article_style+'</style><a href="'+os.path.relpath(root/"Index.html",page.parent).replace(os.sep,"/")+
            '">Experiment overview</a>'+re.sub(r'href="([^"]+)"',relative,body)+'</html>',encoding="utf-8")
    for doc,page in docs.items():
        if doc.exists():article(doc,page)
    article(project/"Measurements.md",root/"wave1/metrics.html")
    rubric=root/"source-read"
    choice=json.loads((rubric/"result.json").read_text(encoding="utf-8"))
    panel_page=['<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Anonymous source read</title>',
                '<style>'+article_style+'img{width:100%;height:auto}figure{margin:18px 0}</style><a href="../Index.html">Experiment overview</a>',
                '<h1>Which family does the drawn line suggest?</h1>',
                '<p>One AI observer saw these drawings without program names, scores or stroke histories. It compared them with the eight reference sketches. It identified '+str(choice["correct"])+' of '+str(choice["total"])+' source labels. Two realizations share each family. One drawing may reasonably admit several constructions.</p>',
                '<details><summary>Show the supplied family sketches</summary><img src="panels/reference-families.png" alt="Eight reference family sketches"><a href="../history-reference/Index.html">Sources and interpretations</a></details>']
    for i in range(4):
        panel_page+=['<figure><img src="panels/drawings-'+str(i+1)+'.png" alt="Four anonymous generated designs"></figure><table><tr><th>Drawing</th><th>Source brief</th><th>Observer chose</th><th>Reason</th></tr>']
        for r in choice["rows"][i*4:i*4+4]:
            panel_page.append('<tr>'+''.join('<td>'+html.escape(str(r[k]))+'</td>' for k in ("code","target","family","reason"))+'</tr>')
        panel_page.append('</table>')
    (rubric/"Index.html").write_text(''.join(panel_page)+'</html>',encoding="utf-8")
    cards=[
        ("wave1/Explore.html","wave1/Pressure.png","Inspect what a pen feels","Move through a drawing beat by beat. See who sent a request, what changed, and what stayed blocked."),
        ("slot-ladder/Gallery.html","slot-ladder/Compare.png","Give a motif more room","Three unchanged programs at narrow, medium and wide settings. Compare available space with an actual connection to a new register."),
        ("density/Gallery.html","density/Largest and smallest changes.png","Ask for more or less line","Trace a signed request down from the frieze and compare the two actual responses at the same scale."),
        ("series/Gallery.html","series/Tradeoff.png","Remember a series","A fourth owner knows only eight published bits. Compare switching machines, applying tension and simply alternating."),
        ("plate/Gallery.html","plate/images/PrintedComparison.png","Compare with a printed fret","See two figures from Meyer's plate 81, their reconstructed centerlines, and a machine trained on just the first."),
        ("wave1/Explore.html#joint","wave1/Joints.png","Read an X","Four rays can arise from two turns, an independent crossing, a shared site, or four directed arrivals.")]
    contents=[]
    for link,pic,title,caption in cards:
        if not (root/link.split("#")[0]).is_file() or not (root/pic).is_file():continue
        contents.append(f'<article><a href="{link}"><img alt="" src="{pic}"><h2>{title}</h2></a><p>{caption}</p></article>')
    body='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Ornaments under tension</title>
<style>*{box-sizing:border-box}body{background:#faf7ef;color:#183e37;font:17px/1.5 system-ui;max-width:1320px;margin:auto;padding:42px clamp(18px,5vw,70px)}h1,h2{font-family:Georgia,serif;font-weight:normal;line-height:1.08}h1{font-size:clamp(44px,6vw,76px);margin:0 0 15px}p{max-width:78ch}nav{display:flex;gap:15px;flex-wrap:wrap;margin:20px 0 30px}a{color:#246044}main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:28px}article{border-top:1px solid #aebcaa;padding-top:14px}article img{display:block;width:100%;height:260px;object-fit:contain;background:#f4f0e5}article a{text-decoration:none}a:hover h2{text-decoration:underline}h2{font-size:30px;margin:12px 0 6px}article p{margin:0}@media(max-width:740px){main{grid-template-columns:1fr}article img{height:auto}}</style>
<header><h1>Ornaments under tension</h1><p>Each pen has a local problem. A machine can ask for a seam; a frieze can ask for a change in density; a whole series can tire of a familiar drawing. Follow what each owner could see, what it asked for, and what the lines actually did.</p>
<nav><a href="Results.html">Findings and limits</a><a href="Measurements.html">Judge and joint counts</a><a href="Criteria.html">Criteria and evidence</a><a href="history-reference/Index.html">Historical sketches</a><a href="source-read/Index.html">Blind source reading</a><a href="Route.html">Route timing test</a><a href="wave1/Engines.png">Eight authored engines</a></nav></header><main>'''
    (root/"Index.html").write_text(body+"".join(contents)+"</main></html>",encoding="utf-8")


if __name__=="__main__":
    build(Path(sys.argv[1]))
