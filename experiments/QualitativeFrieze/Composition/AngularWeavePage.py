"""Publish the captured angular weave experiment as an illustrated static study.

This adapter draws reported sections, crossings, local depth samples and
continuation candidates. It discovers no topology, crossing order or symmetry.
Ribbon thickness and the small depth displacement are presentation choices.
Requires Python-Markdown, matching the other ornament study publishers.
"""
from __future__ import annotations

import argparse
import html
import json
import math
import re
from pathlib import Path
import xml.etree.ElementTree as ET

import markdown

PAPER = "#fcfaf4"
INK = "#263b3a"
COLORS = ["#247b79", "#bc613d", "#70548a", "#4c6fa1", "#968126", "#a44c74"]


def esc(value):
    return html.escape(str(value), quote=True)


def slug(value):
    return re.sub(r"[^a-zA-Z0-9_-]", "-", str(value))


def fmt(value):
    if value is None:
        return "open"
    if isinstance(value, bool):
        return "yes" if value else "no"
    if isinstance(value, list):
        return " · ".join(fmt(v) for v in value) or "none"
    if isinstance(value, dict):
        return " · ".join(f"{words(k)} {fmt(v)}" for k, v in value.items())
    return str(value)


def words(value):
    return re.sub(r"([a-z])([A-Z])", r"\1 \2", str(value)).replace("_", " ").capitalize()


def number(value):
    if isinstance(value, str) and "/" in value:
        a, b = value.split("/")
        return float(a) / float(b)
    return float(value)


def pt(value):
    if isinstance(value, (list, tuple)):
        return number(value[0]), number(value[1])
    return number(value["x"]), number(value["y"])


def n(parent, tag, text=None, **attrs):
    element = ET.SubElement(parent, tag, {k.replace("_", "-"): str(v) for k, v in attrs.items()})
    element.text = text
    return element


def ln(parent, a, b, **attrs):
    return n(parent, "line", x1=a[0], y1=a[1], x2=b[0], y2=b[1], **attrs)


def svg(width, height, title, key):
    result = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": f"0 0 {width} {height}",
        "role": "img", "aria-labelledby": key + "-title", "class": "weave-svg"})
    n(result, "title", title, id=key + "-title")
    return result


def output(svg_element):
    return ET.tostring(svg_element, encoding="unicode")


def arrow(parent, a, b, color, size=4):
    length = math.dist(a, b)
    if length < 2:
        return
    u = ((b[0] - a[0]) / length, (b[1] - a[1]) / length)
    m = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)
    points = [(m[0] + u[0]*size, m[1] + u[1]*size),
              (m[0] - u[0]*size - u[1]*size*.65, m[1] - u[1]*size + u[0]*size*.65),
              (m[0] - u[0]*size + u[1]*size*.65, m[1] - u[1]*size - u[0]*size*.65)]
    n(parent, "polygon", points=" ".join(f"{x:g},{y:g}" for x,y in points), fill=color)


def project_for(points, width, height, margin=30, bounds=None):
    xs, ys = zip(*points)
    x0, x1, y0, y1 = bounds if bounds else (min(xs), max(xs), min(ys), max(ys))
    scale = min((width-2*margin)/max(.001,x1-x0), (height-2*margin)/max(.001,y1-y0))
    ox = (width-(x1-x0)*scale)/2
    oy = (height-(y1-y0)*scale)/2
    return lambda p: (ox+(p[0]-x0)*scale, oy+(y1-p[1])*scale), scale


def segment_map(case):
    return {str(s["id"]):s for s in case["segments"]}


def strand_color(case, strand):
    ids = [str(x["id"]) for x in case.get("strands", [])]
    if str(strand) not in ids:
        ids = list(dict.fromkeys(str(x["strand"]) for x in case["segments"]))
    return COLORS[ids.index(str(strand)) % len(COLORS)]


def unit(a, b):
    length = math.dist(a,b)
    return ((b[0]-a[0])/length, (b[1]-a[1])/length) if length else (0,0)


def ribbon_polygon(case, segment, project, width):
    """Display a miter only at an already shared native endpoint in this route."""
    a, b = project(pt(segment["from"])), project(pt(segment["to"]))
    direction = unit(a,b)
    normal = (-direction[1], direction[0])
    neighbors = sorted([s for s in case["segments"] if str(s["strand"])==str(segment["strand"])], key=lambda s:s.get("slot",0))
    index = next(i for i,s in enumerate(neighbors) if s["id"]==segment["id"])
    prev, nxt = neighbors[index-1], neighbors[(index+1)%len(neighbors)]
    prev_ok = prev.get("present",True) and segment.get("fromSite") is not None and prev.get("toSite")==segment["fromSite"]
    next_ok = nxt.get("present",True) and segment.get("toSite") is not None and nxt.get("fromSite")==segment["toSite"]
    def offset(p, other, valid):
        if not valid:
            return normal[0]*width/2, normal[1]*width/2
        other_dir = unit(project(pt(other["from"])), project(pt(other["to"])))
        other_n = (-other_dir[1],other_dir[0])
        denominator = 1 + normal[0]*other_n[0] + normal[1]*other_n[1]
        if denominator < .15:
            return normal[0]*width/2, normal[1]*width/2
        return (normal[0]+other_n[0])*width/2/denominator, (normal[1]+other_n[1])*width/2/denominator
    oa, ob = offset(a,prev,prev_ok), offset(b,nxt,next_ok)
    return [(a[0]+oa[0],a[1]+oa[1]),(b[0]+ob[0],b[1]+ob[1]),(b[0]-ob[0],b[1]-ob[1]),(a[0]-oa[0],a[1]-oa[1])]


def draw_case(case, key, view="ribbon", width=1080, height=290, selected=None, zoom=None, interactive=True):
    segments = case["segments"]
    points = [pt(s[end]) for s in segments for end in ["from","to"]]
    projection, scale = project_for(points,width,height,35,zoom)
    root = svg(width,height,f"{case['name']} — {view}",key)
    n(root,"rect",x=0,y=0,width=width,height=height,fill=PAPER)
    defs = n(root,"defs")
    clip = n(defs,"clipPath",id=key+"-clip")
    n(clip,"rect",x=0,y=0,width=width,height=height)
    drawing = n(root,"g",clip_path=f"url(#{key}-clip)")
    sm = segment_map(case)
    stroke_width = max(5,min(18,scale*.95)) if zoom is None else min(24,scale*.6)
    clearance = stroke_width*.22
    if view=="ribbon":
        for s in segments:
            if not s.get("present",True):
                continue
            hidden = [c for c in case.get("crossings",[]) if c.get("material",True) and str(c.get("backSegment"))==str(s["id"]) and str(c.get("frontSegment")) in sm and sm[str(c["frontSegment"])].get("present",True)]
            attrs = {"fill":"#171e1d","stroke":"#171e1d","stroke-width":"0.5","stroke-linejoin":"round"}
            if hidden:
                mask_id = key+"-mask-"+slug(s["id"])
                mask = n(defs,"mask",id=mask_id,maskUnits="userSpaceOnUse",x=0,y=0,width=width,height=height)
                n(mask,"rect",x=0,y=0,width=width,height=height,fill="white")
                for crossing in hidden:
                    front = sm[str(crossing["frontSegment"])]
                    at = projection(pt(crossing["at"]))
                    d = unit(projection(pt(front["from"])),projection(pt(front["to"])))
                    before = min(stroke_width*3, math.dist(at,projection(pt(front["from"]))))
                    after = min(stroke_width*3, math.dist(at,projection(pt(front["to"]))))
                    ln(mask,(at[0]-d[0]*before,at[1]-d[1]*before),(at[0]+d[0]*after,at[1]+d[1]*after),stroke="black",stroke_width=stroke_width+2*clearance,stroke_linecap="butt")
                attrs["mask"] = f"url(#{mask_id})"
            poly = ribbon_polygon(case,s,projection,stroke_width)
            n(drawing,"polygon",points=" ".join(f"{x:g},{y:g}" for x,y in poly),**attrs)
    else:
        for s in segments:
            a,b = projection(pt(s["from"])),projection(pt(s["to"]))
            if not s.get("present",True):
                ln(drawing,a,b,stroke="#b5aaa0",stroke_width=1.5,stroke_dasharray="3 6")
                continue
            color = strand_color(case,s["strand"])
            ln(drawing,a,b,stroke=color,stroke_width=3 if view=="strands" else 1.5,opacity=1 if view=="strands" else .24)
            if view=="strands":
                arrow(drawing,a,b,color)
        if view=="depth":
            for c in case.get("crossings",[]):
                if c.get("material") is False:
                    continue
                at = projection(pt(c["at"]))
                for role in ["second","first"]:
                    sid = c.get(role+"Segment")
                    sample = c.get(role+"Z")
                    if str(sid) not in sm or sample is None or not sm[str(sid)].get("present",True):
                        continue
                    s=sm[str(sid)]
                    d=unit(projection(pt(s["from"])),projection(pt(s["to"])))
                    up=(at[0],at[1]-sample*16)
                    color=strand_color(case,s["strand"])
                    ln(drawing,at,up,stroke=color,stroke_width=1,stroke_dasharray="2 3")
                    ln(drawing,(up[0]-d[0]*9,up[1]-d[1]*9),(up[0]+d[0]*9,up[1]+d[1]*9),stroke=color,stroke_width=4)
                    n(drawing,"circle",cx=up[0],cy=up[1],r=2.8,fill=color)
    for c in case.get("crossings",[]):
        at=projection(pt(c["at"]))
        if c.get("frontSegment") is None:
            n(drawing,"circle",cx=at[0],cy=at[1],r=8,fill="none",stroke="#a85d37",stroke_dasharray="3 3")
        if interactive:
            n(drawing,"circle",cx=at[0],cy=at[1],r=max(10,stroke_width),role="button",tabindex="0",
                **{"class":"cross-pick"+(" selected" if str(c["id"])==str(selected) else ""),"data-cross":c["id"],"aria-label":f"Inspect crossing {c['id']}","aria-pressed":str(str(c["id"])==str(selected)).lower()})
            n(drawing,"text",str(c["id"]),x=at[0]+7,y=at[1]-12,**{"class":"cross-label"})
        elif str(c["id"])==str(selected):
            n(drawing,"circle",cx=at[0],cy=at[1],r=stroke_width+5,fill="none",stroke="#a85d37",stroke_width=1.4)
    return root


def depth_svg(case,strand,key):
    visits=strand.get("visits",[])
    root=svg(650,105,f"Strand {strand['id']} local depth at successive encounters",key)
    color=strand_color(case,strand["id"])
    for z,y in [(1,22),(0,48),(-1,74)]:
        ln(root,(30,y),(635,y),stroke="#dedbd0",stroke_width=1)
        n(root,"text",f"{z:+d}" if z else "?",x=4,y=y+4,fill="#62716b",font_size=12,font_family="system-ui")
    span=590/max(1,len(visits)-1)
    previous=None
    for i,v in enumerate(visits):
        x=35+i*span if len(visits)>1 else 320
        z=v.get("z")
        y=48-z*26 if z is not None else 48
        if previous and z is not None:
            ln(root,previous,(x,y),stroke=color,stroke_width=1,stroke_dasharray="3 5",opacity=.5)
        previous=(x,y) if z is not None else None
        n(root,"circle",cx=x,cy=y,r=5,fill=color if z is not None else PAPER,stroke=color,stroke_width=1.6,
            tabindex="0",role="button",**{"class":"depth-visit","data-cross":v["crossing"],"aria-label":f"Inspect crossing {v['crossing']} on strand {strand['id']}"})
        if z is None:
            n(root,"text","?",x=x,y=y+3.5,text_anchor="middle",font_size=9,font_family="system-ui",fill=color,pointer_events="none")
        n(root,"text",str(v["crossing"]),x=x,y=99,text_anchor="middle",font_size=10,font_family="system-ui",fill="#62716b")
    return root


def table(headers,rows,css=""):
    return '<div class="table-scroll"><table class="'+css+'"><thead><tr>'+''.join('<th scope="col">'+str(h)+'</th>' for h in headers)+'</tr></thead><tbody>'+''.join('<tr>'+''.join('<td>'+str(c)+'</td>' for c in row)+'</tr>' for row in rows)+'</tbody></table></div>'


def cross_detail(case,c,key):
    at=pt(c["at"])
    allpoints=[pt(s[end]) for s in case["segments"] for end in ["from","to"]]
    spread=max(max(x for x,y in allpoints)-min(x for x,y in allpoints),max(y for x,y in allpoints)-min(y for x,y in allpoints))
    radius=max(.7,min(3,spread/15))
    picture=output(draw_case(case,key,"strands",420,245,c["id"],(at[0]-radius,at[0]+radius,at[1]-radius*.65,at[1]+radius*.65),False))
    sm=segment_map(case)
    rows=[]
    for role in ["first","second"]:
        sid=c.get(role+"Segment")
        s=sm.get(str(sid))
        if not s:
            continue
        rows.append([esc(sid),esc(s["strand"]),f'{esc(s.get("fromSite","?"))} → {esc(s.get("toSite","?"))}',
            "over" if str(sid)==str(c.get("frontSegment")) else "under" if str(sid)==str(c.get("backSegment")) else "open",esc(fmt(c.get(role+"Z"))),fmt(s.get("present",True))])
    order=f'Section {esc(c["frontSegment"])} passes over section {esc(c["backSegment"])} at this encounter.' if c.get("frontSegment") is not None else 'This encounter has no settled front/back order.'
    if c.get("material") is False:
        order = 'The retained route records this crossing order, but at least one section is absent from the selected material.'
    return f'<div class="cross-witness" data-cross="{esc(c["id"])}">{picture}<p class="cross-caption">{order} Each section retains its own endpoints and route.</p>'+table(["Section","Strand","Native sites","Read","Depth","Present"],rows)+'</div>'


def basic_metrics(case):
    segments=case["segments"]
    strands=case.get("strands",[])
    crossings=case.get("crossings",[])
    return [(len(strands),"recorded strands"),(sum(s.get("present",True) for s in segments),"present sections"),
        (len(crossings),"recorded encounters"),(sum(c.get("frontSegment") is not None for c in crossings),"known local orders")]


def case_panel(case,index):
    cid=slug(case["id"])
    crossings=case.get("crossings",[])
    first=crossings[0]["id"] if crossings else None
    view_names={"ribbon":"Visible black ribbon","strands":"Continuous colored strands","depth":"Local depth samples"}
    figures=''.join(f'<div class="view-svg" data-view="{view}"><p class="nojs-label">{view_names[view]}</p>{output(draw_case(case,cid+"-"+view,view,selected=first))}</div>' for view in ["ribbon","strands","depth"])
    legend=''.join(f'<span><i class="swatch" style="--swatch:{strand_color(case,s["id"])}"></i>Strand {esc(s["id"])}</span>' for s in case.get("strands",[]))
    metrics=''.join(f'<div><strong>{esc(a)}</strong><span>{esc(b)}</span></div>' for a,b in basic_metrics(case))
    rhythm=''
    for s in case.get("strands",[]):
        word=' '.join('O' if v.get("over") is True else 'U' if v.get("over") is False else '?' for v in s.get("visits",[])) or 'No recorded crossing visits'
        rhythm+=f'<div class="depth-row"><strong style="color:{strand_color(case,s["id"])}">Strand {esc(s["id"])}</strong>{output(depth_svg(case,s,cid+"-depth-"+slug(s["id"])))}<p class="depth-word">{esc(word)}</p><p class="caption">Continuous material {fmt(s.get("continuous"))} · native route continuous {fmt(s.get("routeContinuous"))} · closed by identity {fmt(s.get("closed"))}<br>Alternations {fmt(s.get("alternations"))} / {fmt(s.get("adjacentVisits"))} · phase predictions {fmt(s.get("predictionMatches"))} / {fmt(s.get("predictionChecks"))}</p></div>'
    witness=''.join(cross_detail(case,c,cid+"-cross-"+slug(c["id"])) for c in crossings)
    detail=f'<div class="detail-grid"><div class="cross-detail"><h4>Inspect an encounter</h4><p class="caption">Click a crossing or a depth sample, or use the arrows below. Labels identify captured sections and sites.</p><div class="cross-nav js-only"><button class="previous-cross" type="button" aria-label="Previous crossing">←</button><output class="cross-count"></output><button class="next-cross" type="button" aria-label="Next crossing">→</button></div>{witness}</div><div class="depth-rows"><h4>Follow the crossing order</h4><p class="caption">O means over and U means under. The horizontal positions are successive visits, not distances. Dashed connectors only guide the eye. The center row is reserved for unknown depth. Depth between samples stays open.</p>{rhythm}</div></div>'
    extra=''
    for name in ["checks","summary"]:
        item=case.get(name)
        if isinstance(item,dict):
            extra+=table([words(name),"Recorded result"],[[esc(words(k)),esc(fmt(v))] for k,v in item.items()])
        elif isinstance(item,list) and item:
            extra+=generic_table(item)
    segments_table=table(["Section","Strand","Route slot","Native start → end","From → to","Present"],[[esc(s["id"]),esc(s["strand"]),esc(s.get("slot","?")),f'{esc(s.get("fromSite","?"))} → {esc(s.get("toSite","?"))}',f'{esc(fmt(s["from"]))} → {esc(fmt(s["to"]))}',fmt(s.get("present",True))] for s in case["segments"]])
    return f'<article class="case-panel" id="case-{cid}" data-case="{esc(case["id"])}"><div class="case-heading"><div><p class="eyebrow">Captured construction</p><h3>{esc(case["name"])}</h3><p>{esc(case.get("note",case.get("description",case.get("explanation",""))))}</p></div></div><div class="diagram">{figures}</div><div class="legend">{legend}</div><div class="metrics">{metrics}</div>{detail}<details><summary>Recorded checks and exact section references</summary>{extra}{segments_table}</details></article>'


def generic_table(rows):
    if not rows:
        return ''
    if not isinstance(rows[0],dict):
        return '<p>'+esc(fmt(rows))+'</p>'
    keys=list(dict.fromkeys(k for row in rows for k in row if k not in ["surface","nativeSurface"]))
    return table([esc(words(k)) for k in keys],[[esc(fmt(row.get(k))) for k in keys] for row in rows])


def continuation_svg(case,key):
    read=case["read"]
    fragments=read.get("fragments",[])
    candidates=read.get("candidates",[])
    occ=[pt(p) for p in read.get("occluder",[])]
    points=[pt(p) for f in fragments for p in f["controls"]]+occ
    projection,_=project_for(points or [(0,0),(1,1)],560,230,35)
    root=svg(560,230,case["name"],key)
    n(root,"rect",x=0,y=0,width=560,height=230,fill=PAPER)
    if occ:
        n(root,"polygon",points=" ".join(f"{x:g},{y:g}" for x,y in map(projection,occ)),fill="#dcd8ca",stroke="#b6af9f",stroke_width=1.5,stroke_dasharray="5 4")
    site_labels={}
    for f in fragments:
        coords=[projection(pt(p)) for p in f["controls"]]
        color=COLORS[f["index"]%len(COLORS)]
        if len(coords)==2:
            ln(root,coords[0],coords[1],stroke=color,stroke_width=4)
        else:
            # Controls are explicitly exposed. A cubic draw is only their
            # declared Bezier realization, never a newly completed bridge.
            if len(coords)==4:
                d='M '+','.join(map(str,coords[0]))+' C '+' '.join(','.join(map(str,p)) for p in coords[1:])
                n(root,"path",d=d,fill="none",stroke=color,stroke_width=4)
            else:
                n(root,"polyline",points=" ".join(f"{x:g},{y:g}" for x,y in coords),fill="none",stroke=color,stroke_width=4)
        for p,site in [(coords[0],f["startSite"]),(coords[-1],f["endSite"])]:
            n(root,"circle",cx=p[0],cy=p[1],r=4,fill=PAPER,stroke=color,stroke_width=2)
            existing=site_labels.setdefault(p,[])
            if site not in existing:
                n(root,"text",str(site),x=p[0]+5,y=p[1]+(-8 if not existing else 17),font_family="system-ui",font_size=11,fill=color)
                existing.append(site)
        if len(coords)==2:
            arrow(root,coords[0],coords[1],color)
    drawn_candidates={}
    for i,c in enumerate(candidates):
        if c.get("eligible"):
            a,b=projection(pt(c["from"])),projection(pt(c["to"]))
            drawn_candidates.setdefault((a,b),[]).append(i+1)
    for (a,b),numbers in drawn_candidates.items():
        ln(root,a,b,stroke="#a85d37",stroke_width=2,stroke_dasharray="4 5")
        label="candidate "+str(numbers[0]) if len(numbers)==1 else "candidates "+", ".join(map(str,numbers))
        n(root,"text",label,x=(a[0]+b[0])/2,y=(a[1]+b[1])/2-12,text_anchor="middle",font_family="system-ui",font_size=11,fill="#8d4e2e")
    return root


def continuation_panel(case,index):
    read=case["read"]
    picture=output(continuation_svg(case,"inference-"+slug(case["id"])))
    rows=[[f'{esc(c["target"])}',fmt(c.get("straightAligned")),fmt(c.get("tangentCompatible")),fmt(c.get("occluderSupported")),fmt(c.get("eligible")),esc(c.get("status",""))] for c in read.get("candidates",[])]
    checks=table(["Target","Aligned","Tangents","Covered gap","Candidate","Result"],rows) if rows else ''
    return f'<article class="inference-card"><h3>{esc(case["name"])}</h3><p>{esc(case.get("explanation",""))}</p>{picture}<p><span class="result-chip">{esc(read.get("status",""))}</span></p><p>Known closure <strong>{fmt(read.get("actualClosure"))}</strong> · conditional closure <strong>{fmt(read.get("conditionalClosure"))}</strong> · compatible candidates <strong>{fmt(read.get("compatibleCandidates"))}</strong></p><p class="caption">{esc(read.get("limit",""))}</p><details><summary>Inspect candidate evidence</summary>{checks}</details></article>'


def junction_panel(case,index):
    points=[pt(s[end]) for s in case["segments"] for end in ["from","to"]]
    project,_=project_for(points,560,240,35)
    root=svg(560,240,case["name"],"junction-"+slug(case["id"]))
    for s in case["segments"]:
        ln(root,project(pt(s["from"])),project(pt(s["to"])),stroke=INK,stroke_width=5)
    for site in case.get("sites",[]):
        at=project(pt(site["at"]))
        n(root,"circle",cx=at[0],cy=at[1],r=6 if site["degree"]>2 else 3,fill="#a85d37" if site["degree"]>2 else PAPER,stroke=INK,stroke_width=1.5)
        n(root,"text",str(site["id"]),x=at[0]+8,y=at[1]-7,font_size=12,font_family="system-ui",fill=INK)
    return f'<article class="inference-card junction-card"><h3>{esc(case["name"])}</h3><p>{esc(case["explanation"])}</p>{output(root)}<p>Components <strong>{fmt(case["materialComponents"])}</strong> · ends <strong>{fmt(case["ends"])}</strong> · branch sites <strong>{fmt(case["branchSites"])}</strong></p><p class="caption">{esc(case["routing"])}</p></article>'


def scorecard(cases):
    rows=[]
    rows.append(["Native strands"]+[str(len(c.get("strands",[]))) for c in cases])
    rows.append(["Present sections"]+[str(sum(s.get("present",True) for s in c["segments"])) for c in cases])
    rows.append(["Known local orders"]+[str(sum(x.get("frontSegment") is not None for x in c.get("crossings",[]))) for c in cases])
    rows.append(["Continuous strands"]+[str(sum(s.get("continuous") is True for s in c.get("strands",[]))) for c in cases])
    rows.append(["Closed strands by identity"]+[str(sum(s.get("closed") is True for s in c.get("strands",[]))) for c in cases])
    keys=list(dict.fromkeys(k for c in cases for k in (c.get("summary",{}) if isinstance(c.get("summary"),dict) else {})))
    rows += [[esc(words(k))]+[esc(fmt(c.get("summary",{}).get(k))) for c in cases] for k in keys]
    return table(["Read"]+[f'<a href="#inspect" data-pick-case="{esc(c["id"])}">{esc(c["name"])}</a>' for c in cases],rows,"scorecard")


def make_overview(cases):
    root=svg(1080,360*min(3,len(cases)),"Angular weave — recorded controlled changes","overview")
    n(root,"rect",x=0,y=0,width=1080,height=360*min(3,len(cases)),fill=PAPER)
    for i,case in enumerate(cases[:3]):
        group=n(root,"g",transform=f"translate(0 {i*360})")
        n(group,"text",case["name"],x=30,y=35,font_size=26,font_family="Georgia,serif",fill=INK)
        picture=draw_case(case,f"overview-{i}","ribbon",1080,270,interactive=False)
        picture.set("y","60")
        group.append(picture)
    return output(root)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report",type=Path)
    parser.add_argument("output",type=Path)
    args=parser.parse_args()
    source=Path(__file__).resolve().parent
    report=json.loads(args.report.read_text(encoding="utf-8-sig"))
    weave=report.get("weave",report)
    cases=weave["cases"] if isinstance(weave,dict) else weave
    notes_path=source.parent/"AngularWeaveStudy.md"
    notes=notes_path.read_text(encoding="utf-8")
    notes_html=markdown.markdown(notes,extensions=["tables","fenced_code"])
    out=args.output.resolve()
    out.mkdir(parents=True,exist_ok=True)
    (out/"notes.md").write_text(notes,encoding="utf-8",newline="\n")
    (out/"study-data.json").write_text(json.dumps(report,separators=(",",":"),ensure_ascii=False),encoding="utf-8",newline="\n")
    for ext in ["css","js"]:
        (out/f"weave.{ext}").write_text((source/f"AngularWeavePage.{ext}").read_text(encoding="utf-8"),encoding="utf-8",newline="\n")
    overview=make_overview(cases)
    (out/"WeaveOverview.svg").write_text(overview,encoding="utf-8",newline="\n")
    # A repo-local figure supports the Markdown equivalent without website paths.
    (source.parent/"WeaveOverview.svg").write_text(overview,encoding="utf-8",newline="\n")
    options=''.join(f'<option value="{esc(c["id"])}">{esc(c["name"])}</option>' for c in cases)
    panels=''.join(case_panel(c,i) for i,c in enumerate(cases))
    continuation=''.join(continuation_panel(c,i) for i,c in enumerate(report.get("continuations",[])))
    junctions=''.join(junction_panel(c,i) for i,c in enumerate(report.get("junctions",[])))
    comparisons=''
    if isinstance(weave,dict):
        if weave.get("comparisons"):
            rows=[]
            for c in weave["comparisons"]:
                transform='Reflect the planar frame' if c.get("planarReflection") else 'Keep planar positions'
                transform+=' + invert local depth' if c.get("depthInverted") else ' + retain local depth'
                rows.append([esc(c["label"]),esc(transform),fmt(c["geometryMatches"]),fmt(c["topologyMatches"]),fmt(c["materialMatches"]),f'{c["ordersMatch"]} / {c["ordersChecked"]}'])
            comparisons+='<h3>Compare with the baseline</h3><p class="section-lead">Each row applies its stated transform before comparing corresponding source sections. A match after depth inversion means the orders exchange consistently. It does not mean the front strand stayed the same. Native route checks retain the known route even when its material is absent.</p>'+table(["Change","Selected transform","Planar geometry","Native route","Material","Order after transform"],rows)
        if weave.get("periodReads"):
            period_rows=[]
            for read in weave["periodReads"]:
                mapping=' · '.join(f'{i} → {target}' for i,target in enumerate(read["strandMap"]))
                period_rows.append([f'{read["bays"]} bay'+('s' if read["bays"]!=1 else ''),esc(mapping),
                    f'{read["segmentsMatched"]} / {read["segmentsChecked"]}',f'{read["crossingsMatched"]} / {read["crossingsChecked"]}',
                    str(read["ordersMatched"]),fmt(read["closesStrands"])])
            comparisons+='<h3>A repeating picture can exchange strands</h3><p class="section-lead">Move the view by one bay and compare the actual section correspondences. A strand may map onto its partner. A longer translation can return each named strand to itself. This is a correspondence inside the captured strip, and supplies no connection between its open ends.</p>'+table(["Translate by","Strand correspondence","Sections","Crossings","Matching orders","Closes strands"],period_rows)
        for key in ["symmetries","transforms"]:
            if weave.get(key):
                comparisons+=f'<h3>{esc(words(key))}</h3>'+generic_table(weave[key])
        if weave.get("encoding"):
            comparisons+='<details><summary>What connects to what in the construction</summary><ul>'+''.join('<li>'+esc(line)+'</li>' for line in weave["encoding"])+'</ul></details>'
    page=f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<base href="/ornaments/weave/"><title>Following a strand through a weave · Gojo ornaments</title>
<meta name="description" content="An interactive Gojo study of continuous strands, local over/under order, shallow depth, symmetry and hidden continuation candidates.">
<link rel="stylesheet" href="/ornaments/ornaments.css"><link rel="stylesheet" href="weave.css"><script src="weave.js" defer></script></head>
<body class="hide-labels"><a class="skip" href="#main">Skip to the study</a><main id="main">
<nav aria-label="Site"><a href="/ornaments/">← Ornament studies</a><a href="/ornaments/signatures/">Pattern signatures</a><a href="/">Gojo</a></nav>
<header><p class="eyebrow">Recorded study · continuity, crossing and depth</p><h1>Following a strand through a weave</h1>
<div class="overview"><p>A strand can disappear under another and still remain connected. This experiment keeps its route, its local crossing order and its visible ribbon available as separate reads of the same structure.</p>
<p>Start with the black ribbon, then reveal the colored strands or the small depth samples. Compare controlled changes to see which facts survive. The continuation examples below ask a second question—what can visible fragments justify when the hidden route has not been supplied?</p></div></header>
<nav class="subnav" aria-label="Study"><a href="#inspect">Explore the weave</a><a href="#compare">Compare readings</a><a href="#continuation">Hidden continuations</a><a href="#notes">Study notes</a></nav>
<div class="scope">This is an authored reconstruction inspired by the supplied angular ornament. The selected connections and crossing orders are explicit premises. The image alone does not establish its original machine, strand count or hidden route. Ribbon thickness and the displayed height are illustrative.</div>
<div class="three-readings"><div><strong>1. Keep the route</strong><p>A hidden section retains its actual endpoints and place in the strand. An absent section leaves a different result.</p></div><div><strong>2. Read each encounter</strong><p>Front and back belong to a local crossing. A strand can be above here and below at its next encounter.</p></div><div><strong>3. Preserve alternatives</strong><p>Aligned fragments can support a continuation candidate. Shared identity and a witnessed return route provide stronger evidence.</p></div></div>
<section id="inspect"><h2>Explore the weave</h2><p class="section-lead">Change one structural condition at a time. Select a crossing to inspect the actual section references and its depth order.</p>
<div class="controls js-only"><label for="case-select">Construction</label><button id="previous-case" type="button" aria-label="Previous construction">←</button><select id="case-select">{options}</select><button id="next-case" type="button" aria-label="Next construction">→</button></div>
<div class="view-controls js-only"><span class="view-label">View</span><button type="button" data-select-view="ribbon" aria-pressed="true">Black ribbon</button><button type="button" data-select-view="strands" aria-pressed="false">Continuous strands</button><button type="button" data-select-view="depth" aria-pressed="false">Local depth</button><label><input type="checkbox" id="show-labels">Crossing labels</label></div>
<p id="view-description" class="caption">The visible black ribbon. Local undercuts hide a small part of the lower strand while its native route stays connected.</p>
<noscript><p class="notice">All constructions and views are shown below. The first encounter in each construction is illustrated. The complete recorded read contains the other encounters. Enable JavaScript to select them directly.</p></noscript>
{panels}</section>
<section id="compare"><h2>What survives a change?</h2><p class="section-lead">These observations overlap. They describe the captured constructions and the selected comparison, without forming a single beauty score.</p>{scorecard(cases)}{comparisons}</section>
<section id="junctions"><h2>Crossing or junction?</h2><p class="section-lead">The same projected X can describe two passing routes or four arms meeting at one actual site. The shared site changes what a routing law could do. These controls leave depth order open.</p><div class="inference-grid">{junctions}</div></section>
<section id="continuation"><h2>What can a hidden gap tell us?</h2><p class="section-lead">These small fixtures expose fragments, endpoint tangents and a cover. A dashed connector marks candidate endpoints. It has not been inserted into the original structure and supplies no particular hidden curve. Endpoint numbers show identity, so equal coordinates can still carry different sites.</p><div class="inference-grid">{continuation}</div></section>
<section id="notes" class="notes"><div class="downloads"><a href="notes.md" download>Markdown study</a><a href="study-data.json" download>Complete recorded reads</a><a href="WeaveOverview.svg" download>SVG comparison</a></div>{notes_html}</section>
<footer><p>Gojo ornament studies. The diagrams are presentation reads of the recorded experiment. Construction, observation, candidate completion and actual topology retain their separate evidence.</p><p><a href="/ornaments/">All ornament studies</a> · <a href="/ornaments/signatures/">Earlier pattern signatures</a></p></footer>
</main></body></html>'''
    (out/"index.html").write_text(page,encoding="utf-8",newline="\n")
    print(json.dumps({"output":str(out),"cases":len(cases),"continuations":len(report.get("continuations",[]))}))


if __name__=="__main__":
    main()
