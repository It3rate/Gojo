"""Build an offline visual index for the recorded local path studies."""
import html
import os
import re
import sys
from pathlib import Path
from markdown import markdown


def build(root):
    root=root.resolve()
    experiment=Path(__file__).resolve().parents[1]/"OrnamentPathCampaign"
    documents={experiment/f"{name}.md":root/f"{name}.html" for name in
               ("Results","MeasurementResults","Criteria","HistoryAudit","Encoding","Protocol")}
    for document,page_path in documents.items():
        content=markdown(document.read_text(encoding="utf-8-sig"),extensions=["tables","fenced_code"])
        def local_link(match):
            link=match[1]
            if "://" in link or link.startswith("#"):
                return match[0]
            target=(document.parent/link).resolve()
            target=documents.get(target,target)
            return 'href="'+html.escape(os.path.relpath(target,root).replace(os.sep,"/"),quote=True)+'"'
        rendered=re.sub(r'href="([^"]+)"',local_link,content)
        style=("body{max-width:950px;margin:3em auto;padding:0 20px;font:17px/1.6 system-ui;"
               "background:#faf8f0;color:#203a31}a{color:#256347}h1,h2{font-family:Georgia,serif;line-height:1.15}"
               "table{border-collapse:collapse;display:block;overflow:auto}td,th{padding:.45em;border-bottom:1px solid #bbc4b6}"
               "code{overflow-wrap:anywhere}pre{overflow:auto}")
        page_path.write_text('<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+
                             html.escape(document.stem)+'</title><style>'+style+'</style><a href="Index.html">Experiment overview</a>'+rendered+'</html>',encoding="utf-8")
    cards=[
        ("sampler/Explore.html","sampler/Sampler.png","Twenty machines, two settings",
         "Compare how the same strokes behave when A, B and repeat pitch change. Each of the five bays was drawn by its machine."),
        ("wave1/Explore.html","wave1/ConstructionSheet.png","Inspect the construction",
         "Scrub a drawing in time. Select a stroke to see its owner, the gate that let it draw and the sites it connects."),
        ("shape-refinement/Gallery.html","shape-refinement/Compare.png","Change the judging rule",
         "Compare what the pen draws when returning through a course or branching from a stem has to be witnessed."),
        ("preference/PreferencePair.png","preference/PreferencePair.png","Where the visual readings disagree",
         "Two blinded observers favored the opposing diagonal figures. The registered countermarch score favored the row of narrow slots.")]
    open_run=root/"open-meander"
    if open_run.is_dir():
        page=open_run/"Gallery.html"
        thumb=open_run/"Compare.png"
        if page.exists() and thumb.exists():
            cards.insert(0,("open-meander/Gallery.html","open-meander/Compare.png","Can the return stay open?",
                            "Follow the final test of open passages, clearance and continuity on new calibrations."))
    body=[]
    for link,image,title,caption in cards:
        if not all((root/p).is_file() for p in (link,image)):
            raise ValueError(f"Missing gallery artwork: {link}")
        body.append(f'<article><a href="{link}"><img src="{image}" alt=""><h2>{html.escape(title)}</h2></a>'
                    f'<p>{html.escape(caption)}</p></article>')
    page='''<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Path ornaments · experiment book</title>
<style>
*{box-sizing:border-box}body{background:#faf8f0;color:#1e3930;max-width:1260px;margin:auto;padding:48px clamp(16px,5vw,68px);font:17px/1.55 system-ui}
h1,h2{font-family:Georgia,serif;font-weight:normal;line-height:1.07}h1{font-size:clamp(40px,6vw,74px);margin:0 0 16px}
h2{font-size:29px;margin:16px 0 8px}header{max-width:820px}nav{display:flex;flex-wrap:wrap;gap:18px;margin:20px 0 40px}
a{color:#205a46;text-underline-offset:4px}article a{text-decoration:none}article a:hover h2{text-decoration:underline}
main{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:35px}article{border-top:1px solid #7d9180;padding-top:14px}
img{width:100%;height:240px;object-fit:contain;background:#f6f4eb}p{margin:6px 0}article p{max-width:52ch}
@media(max-width:720px){main{grid-template-columns:1fr}body{padding-top:30px}img{height:auto;max-height:340px}}
</style><header><h1>Ornaments made by paths</h1><p>Each drawing starts with a blank band and a small program. Follow what the pens built, which judging rules selected it, and where a visually plausible answer fooled those rules.</p></header>'''
    nav='<nav><a href="Results.html">Results and limits</a><a href="MeasurementResults.html">All 28 criteria and their utility</a><a href="Criteria.html">Historical and modern evidence</a></nav>'
    (root/"Index.html").write_text(page+nav+"<main>"+"".join(body)+"</main></html>",encoding="utf-8")


if __name__=="__main__":
    build(Path(sys.argv[1]))
