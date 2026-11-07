"""Publish recorded ornament signatures as a static illustrated study.

This is a presentation adapter: all geometry, match witnesses, regions and
scorecard observations come from the report. It does not run Gojo or discover
new symmetries. Requires Python-Markdown (python -m pip install markdown).
"""

import argparse
import html
import json
import math
import re
import shutil
import textwrap
import xml.etree.ElementTree as ET
from pathlib import Path

import markdown


INK = "#263b3a"
WHITE_COLORS = {"top": "#bde8de", "bottom": "#f4d1b3", "both": "#d5c9e6", "closed": "#d8bdde"}


def esc(value):
    return html.escape(str(value), quote=True)


def node(parent, tag, text=None, **attrs):
    item = ET.SubElement(parent, tag, {k.replace("_", "-"): str(v) for k, v in attrs.items()})
    item.text = text
    return item


def fmt(value):
    if value is None:
        return "unavailable"
    if isinstance(value, bool):
        return "yes" if value else "no"
    if isinstance(value, float):
        return f"{value:g}"
    if isinstance(value, (list, tuple)):
        return " · ".join(fmt(v) for v in value) or "none"
    if isinstance(value, dict):
        return "; ".join(f"{k}: {fmt(v)}" for k, v in value.items())
    return str(value)


def vertices(pattern):
    out = [(0, 0)]
    for v in pattern["segments"]:
        x, y = out[-1]
        out.append((x + v["x"], y + v["y"]))
    return out


def svg_text(svg):
    return ET.tostring(svg, encoding="unicode")


def arrow(parent, a, b, color, size=4):
    dx, dy = b[0]-a[0], b[1]-a[1]
    length = math.hypot(dx, dy)
    if length < 1:
        return
    ux, uy = dx/length, dy/length
    x, y = (a[0]+b[0])/2, (a[1]+b[1])/2
    points = [(x+ux*size, y+uy*size), (x-ux*size+uy*size*.7, y-uy*size-ux*size*.7),
              (x-ux*size-uy*size*.7, y-uy*size+ux*size*.7)]
    node(parent, "polygon", points=" ".join(f"{x:g},{y:g}" for x, y in points), fill=color)


def line(parent, a, b, **attrs):
    return node(parent, "line", x1=f"{a[0]:g}", y1=f"{a[1]:g}", x2=f"{b[0]:g}", y2=f"{b[1]:g}", **attrs)


def pattern_svg(pattern, key, small=False):
    points = vertices(pattern)
    gap = pattern["gaps"]
    pitch, low, high = gap["pitch"], gap["bottom"], gap["top"]
    cycles = 3 if not small else 2
    width, height = (1020, 270) if not small else (540, 170)
    margin_x, margin_y = (30, 35) if not small else (14, 18)
    scale = min((width-2*margin_x)/(cycles*pitch), (height-2*margin_y)/(max(1, high-low)))
    left = (width-cycles*pitch*scale)/2
    top = (height-(high-low)*scale)/2
    project = lambda p: (left+p[0]*scale, top+(high-p[1])*scale)
    svg = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": f"0 0 {width} {height}",
                             "role": "img", "aria-labelledby": f"{key}-title {key}-desc", "class": "pattern-svg"})
    node(svg, "title", f"{pattern['name']}: {cycles} repeats", id=f"{key}-title")
    node(svg, "desc", "Recorded centerline geometry. Optional layers expose connected whitespace, pen direction, "
         "and recorded rotation centers. Repeated crop edges continue into neighboring copies.", id=f"{key}-desc")
    defs = node(svg, "defs")
    clip = node(defs, "clipPath", id=f"{key}-clip")
    node(clip, "rect", x=left, y=0, width=cycles*pitch*scale, height=height)
    clipped = node(svg, "g", clip_path=f"url(#{key}-clip)")
    whites = node(clipped, "g", **{"class": "layer-whitespace"})
    for cycle in range(-1, cycles+1):
        for region in gap["regions"]:
            state = "both" if region["top"] and region["bottom"] else "top" if region["top"] else "bottom" if region["bottom"] else "closed"
            for cell in region["cells"]:
                x, y = project((cell["x0"]+cycle*pitch, cell["y1"]))
                node(whites, "rect", x=x, y=y, width=(cell["x1"]-cell["x0"])*scale,
                     height=(cell["y1"]-cell["y0"])*scale, fill=WHITE_COLORS[state])
    seams = node(clipped, "g", **{"class": "layer-seams", "stroke": "#939d93", "stroke-dasharray": "3 5", "stroke-width": 1})
    for k in range(1, cycles):
        line(seams, project((k*pitch, high+.3)), project((k*pitch, low-.3)))
    ink = node(clipped, "g", fill="none", stroke=INK, stroke_width=3.6 if not small else 2.8,
               stroke_linecap="butt", stroke_linejoin="miter")
    arrows = node(clipped, "g", **{"class": "layer-flow"})
    for cycle in range(-2, cycles+2):
        rendered = [project((x+cycle*pitch, y)) for x, y in points]
        node(ink, "polyline", points=" ".join(f"{x:g},{y:g}" for x, y in rendered))
        for a, b in zip(rendered, rendered[1:]):
            arrow(arrows, a, b, "#b04732", size=5 if not small else 3)
    numbers = node(clipped, "g", **{"class": "layer-labels", "font-size": 11, "font-family": "system-ui, sans-serif", "fill": "#875134", "text-anchor": "middle"})
    for i, (a, b) in enumerate(zip(points, points[1:])):
        x, y = project(((a[0]+b[0])/2+pitch, (a[1]+b[1])/2))
        dx, dy = b[0]-a[0], b[1]-a[1]
        node(numbers, "text", str(i), x=x+(9 if dy else 0), y=y+(-8 if dx else 3))
    for field, cls, color in (("sharedEndQuarterRows", "layer-quarter", "#71508a"), ("wholeBandHalfTurnRows", "layer-half", "#225da0")):
        group = node(clipped, "g", **{"class": cls})
        for row in pattern["joints"][field]:
            for cycle in range(cycles):
                for x2 in row["x2"]:
                    x, y = project((x2/2+cycle*pitch, row["y2"]/2))
                    node(group, "circle", cx=x, cy=y, r=5, fill="#fffdf7" if field == "wholeBandHalfTurnRows" else color,
                         stroke=color, stroke_width=2)
    mouths = node(clipped, "g", **{"class": "layer-whitespace"})
    for cycle in range(-1, cycles+1):
        for mouth in gap["mouths"]:
            is_top = mouth["side"] == "top"
            y = high if is_top else low
            a = project((mouth["start"]+cycle*pitch, y))
            b = project((mouth["end"]+cycle*pitch, y))
            d = -10 if is_top else 10
            color = "#276d68" if is_top else "#975021"
            line(mouths, (a[0], a[1]+d), (b[0], b[1]+d), stroke=color, stroke_width=3)
    return svg_text(svg)


def score_svg(score, key, label, common_extent=None):
    sites = [(s["x"], s["y"]) for s in score["sites"]]
    strokes = score["strokes"]
    low_x, high_x = min(x for x, y in sites), max(x for x, y in sites)
    low_y, high_y = min(y for x, y in sites), max(y for x, y in sites)
    extent = common_extent or (high_x-low_x, high_y-low_y)
    scale = min(570/max(1, extent[0]), 155/max(1, extent[1]))
    left, top = (610-(high_x-low_x)*scale)/2, (205-(high_y-low_y)*scale)/2
    project = lambda p: (left+(p[0]-low_x)*scale, top+(high_y-p[1])*scale)
    svg = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": "0 0 610 205", "role": "img", "aria-labelledby": f"{key}-title"})
    node(svg, "title", label, id=f"{key}-title")
    palette = [INK, "#2d7b88", "#a85d37", "#74518c"]
    for stroke in strokes:
        a, b = project(sites[stroke["from"]]), project(sites[stroke["to"]])
        active = stroke["participates"]
        line(svg, a, b, stroke=palette[int(stroke.get("owner", 0)) % len(palette)] if active else "#a85d37",
             stroke_width=3.8 if active else 2, stroke_dasharray="none" if active else "4 5", stroke_linecap="round")
    # Identity and coincidence are intentionally kept separate. Marker dots
    # denote recorded native sites, not invented junctions at intersections.
    for x, y in sites:
        sx, sy = project((x, y))
        node(svg, "circle", cx=sx, cy=sy, r=2.4, fill="#fffdf7", stroke=INK, stroke_width=1)
    return svg_text(svg)


def table(headers, rows, cls=""):
    head = "".join(f'<th scope="col">{esc(h)}</th>' for h in headers)
    body = "".join("<tr>"+"".join(f"<td>{v}</td>" for v in row)+"</tr>" for row in rows)
    return f'<div class="table-scroll"><table class="{cls}"><thead><tr>{head}</tr></thead><tbody>{body}</tbody></table></div>'


def render_scorecard(patterns):
    keys = []
    for p in patterns:
        for f in p.get("features", []):
            if f["key"] not in [k["key"] for k in keys]:
                keys.append(f)
    if not keys:
        return ""
    rows = []
    for f in keys:
        row = [f'<strong>{esc(f["label"])}</strong><small>{esc(f.get("family", ""))}</small>']
        for p in patterns:
            match = next((v for v in p["features"] if v["key"] == f["key"]), None)
            row.append(esc(fmt(match["value"])) if match else "—")
        rows.append(row)
    return table(["Read"]+[p["name"] for p in patterns], rows, "scorecard")


def pattern_panel(p, i):
    key = p["id"]
    windows, joints, gaps, course = p["windows"], p["joints"], p["gaps"], p["course"]
    exact = sum(m["equalRun"] >= 3 for m in windows["matches"])
    dominance = "".join(f"<li>{esc(d)}</li>" for d in p.get("dominant", []))
    bands = [[esc(m["kind"]), esc(m["frame"].replace("-", " ")), f'({m["offsetX"]}, {m["offsetY"]})',
              "backward" if m["reversed"] else "forward"] for m in windows["band"]]
    regions = [[str(r["id"]), "top and bottom" if r["top"] and r["bottom"] else "top" if r["top"] else "bottom" if r["bottom"] else "enclosed",
                esc(fmt(r["area"])), str(len(r["cells"]))] for r in gaps["regions"]]
    pivots = []
    for field, title in (("sharedEndQuarterRows", "shared-tip quarter-turn"), ("wholeBandHalfTurnRows", "whole-band half-turn")):
        for row in joints[field]:
            pivots.append([title, fmt(row["y2"]/2), esc(fmt([x/2 for x in row["x2"]])), esc(fmt([x/2 for x in row["gaps2"]]))])
    lengths = [abs(s["x"])+abs(s["y"]) for s in p["segments"]]
    headings = ["E" if s["x"] > 0 else "W" if s["x"] < 0 else "N" if s["y"] > 0 else "S" for s in p["segments"]]
    sequence = " ".join(f"{h}{n}" for h, n in zip(headings, lengths))
    note = p.get("scope", "A chosen cardinal centerline reconstruction; the source image does not determine the generator's original history.")
    return f'''
<article class="pattern-panel" id="pattern-{esc(key)}" data-pattern="{esc(key)}" aria-labelledby="title-{esc(key)}">
  <div class="panel-heading"><div><h3 id="title-{esc(key)}">{esc(p['name'])}</h3></div>
  <p class="pattern-source">{esc(p['source'])}</p></div>
  <p>{esc(note)}</p>
  <div class="plot">{pattern_svg(p, key)}</div>
  <p class="plot-caption">Three periods of the recorded centerline. Dashed vertical marks are repeat cuts. Arrows show the authored pen direction; dots mark recorded symmetry centers.</p>
  <ul class="dominant">{dominance}</ul>
  <div class="summary-grid"><div><strong>{len(p['segments'])}</strong><span>reaches / period</span></div><div><strong>{windows['candidatesChecked']:,}</strong><span>window candidates checked</span></div><div><strong>{exact:,}</strong><span>equal-reach window witnesses</span></div><div><strong>{len(gaps['regions'])}</strong><span>white regions / period</span></div></div>
  <div class="split"><div><h4>Sequence read</h4><p>Left/right turns, including the seam:</p><p class="sequence">{esc(course['turnWord'])}</p><p>Reach order:</p><p class="sequence">{esc(course['reachOrder'])}</p><p>Longest shrinking, same-hand run: <strong>{course['longestContraction']} turns</strong>.</p></div>
  <div><h4>Whole-strip correspondences</h4><p>Offsets are local tick coordinates, modulo the repeat pitch. A matching shape can require backward traversal.</p>{table(['Kind','Frame','Offset','Traversal'], bands)}</div></div>
  <details><summary>White regions, spacing and pivots</summary><p>Colored regions use the same zero-width walls as the ink read. Areas and gaps are calibrated ticks of this reconstruction. "Nearest" means a neighboring parallel segment somewhere along a reach; it is not a promise of uniform clearance everywhere.</p>
  {table(['White region','Opens to','Area (square ticks)','Partition cells'], regions)}
  {table(['Gap direction','Width (ticks)','Overlap coverage'], [[esc(w['axis']),esc(fmt(w['width'])),esc(fmt(w['coverage']))] for w in gaps['widths']])}
  {table(['Pivot family','Row y','Phases x','Successive gaps'], pivots)}
  <p>Quarter-turn centers are read from the bounded common tips. Half-turn centers are qualified by the entire repeating band. Several local witnesses can share one pivot.</p></details>
  <details><summary>Two-arm corner correspondences</summary><p>The separate corner read checked {joints['candidatesChecked']:,} candidates and retained {len(joints['matches']):,} correspondences. It compares the incoming and outgoing arm at each corner, including partial arm extents, frame changes and traversal reversal.</p><p>The window explorer below begins at three reaches. The complete two-arm list is retained under <code>joints.matches</code> in the <a href="study-data.json">recorded JSON</a>; the study notes illustrate one shared-tip case.</p></details>
  <details><summary>Concrete reach encoding and read boundaries</summary><p class="sequence">{esc(sequence)}</p>
  <p>Each token is a direction and a repeated tick extent, read from the executed Gojo strip. It is a compact presentation of the trace. The published JSON retains the recorded source and comparison witnesses.</p>
  <p>Direction words do not alone determine stored orientation/direction bits: the active frame and traversal matter. This study claims only the supplied native evidence and its explicit geometric read.</p>
  <a href="study-data.json">Read the complete data</a></details>
</article>'''


CSS = r'''
html{scroll-behavior:smooth}header{max-width:940px;padding-bottom:30px}h1{font-size:clamp(40px,5.5vw,66px)}h2{font-size:34px;margin:0 0 18px}h3{font:400 30px/1.15 Georgia,serif;margin:0 0 12px}h4{font-size:17px;margin:0 0 12px}section{margin:42px 0}p{max-width:86ch}button,input,select{font:inherit}button,select{background:#fffdf7;border:1px solid var(--line);color:var(--ink);border-radius:3px;padding:9px 12px}button{cursor:pointer}button:hover{border-color:var(--accent)}button:focus-visible,select:focus-visible,input:focus-visible,summary:focus-visible{outline:3px solid var(--warm);outline-offset:3px}button:disabled{cursor:default;opacity:.45}a,button{touch-action:manipulation}.subnav{justify-content:flex-start;flex-wrap:wrap;gap:12px 24px;margin-bottom:28px;padding:0 0 16px}.subnav a{text-decoration:underline}.overview{font-size:19px;max-width:86ch}.overview p{margin:0 0 14px}.scope{padding:18px 22px;border-left:3px solid var(--accent);background:#e8eee4;font-size:15px}.method-grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:26px;margin-top:24px}.method-grid p{font-size:15px;margin:8px 0 0}.method-grid strong{font-size:17px}.pattern-menu{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}.pattern-card{display:block;text-decoration:none;color:inherit;border:1px solid var(--line);background:#fffdf7;min-width:0}.pattern-card:hover,.pattern-card[aria-current=true]{border-color:var(--accent);box-shadow:inset 0 -3px var(--accent)}.pattern-card svg{width:100%;height:auto;display:block}.pattern-card h3{font-size:22px;margin:0 16px 7px}.pattern-card p{font-size:12px;line-height:1.5;margin:0 16px 16px;color:var(--muted)}.pattern-card .layer-whitespace,.pattern-card .layer-flow,.pattern-card .layer-quarter,.pattern-card .layer-half,.pattern-card .layer-labels{display:none}.section-lead{color:var(--muted);max-width:84ch}.table-scroll{overflow:auto;border:1px solid var(--line);max-width:100%;margin:16px 0;background:#fffdf7}table{border-collapse:collapse;text-align:left;width:100%;font-size:14px;line-height:1.5}th,td{padding:11px 13px;border-bottom:1px solid #e0dfd3;vertical-align:top}th{font-weight:600;background:#eaece1;white-space:nowrap}tbody tr:last-child td{border-bottom:0}.scorecard td{min-width:115px}.scorecard th:first-child,.scorecard td:first-child{min-width:190px;position:sticky;left:0;background:#fffdf7;border-right:1px solid var(--line)}.scorecard th:first-child{background:#eaece1}.scorecard small{display:block;color:var(--muted);font-size:12px;font-weight:400}details{border-top:1px solid var(--line);padding:14px 0}summary{cursor:pointer;font-weight:600;line-height:1.5}details>p:first-of-type{margin-top:16px}.control-bar{display:flex;flex-wrap:wrap;gap:15px 24px;padding:16px 20px;background:#eaece1;border:1px solid var(--line);border-bottom:0;align-items:center}.control-bar label{display:inline-flex;align-items:center;gap:7px;font-size:14px}.control-bar input{width:17px;height:17px;accent-color:var(--accent);margin:0}.selection-controls{display:flex;gap:10px;flex-wrap:wrap;align-items:center;margin:18px 0}.selection-controls select{max-width:100%;min-width:230px}.selection-controls label{font-weight:600}.pattern-panel{background:#fffdf7;padding:26px;border:1px solid var(--line)}.pattern-panel+.pattern-panel{margin-top:24px}.panel-heading{display:flex;justify-content:space-between;gap:25px;align-items:start}.pattern-source{font-size:13px;color:var(--muted);max-width:40ch;margin-top:0}.panel-heading .eyebrow{margin:0 0 6px}.plot{margin:25px -26px 0;background:#fcfaf4;border-top:1px solid #e4e4d7;border-bottom:1px solid #e4e4d7}.plot svg{display:block;width:100%;height:auto}.plot-caption{font-size:13px;color:var(--muted);margin:10px 0 20px}.dominant{padding-left:20px;max-width:90ch}.dominant li{margin:8px 0}.summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px;border-top:1px solid var(--line);border-bottom:1px solid var(--line);padding:20px 0;margin:24px 0}.summary-grid strong{display:block;font:30px/1.2 Georgia,serif}.summary-grid span{font-size:12px;color:var(--muted)}.split{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1.5fr);gap:30px;margin:26px 0}.split p{font-size:14px}.sequence{font:15px/1.8 ui-monospace,Consolas,monospace;overflow-wrap:anywhere;letter-spacing:.08em}.legend{display:flex;flex-wrap:wrap;gap:12px 25px;font-size:13px;margin:12px 0}.legend span{display:flex;align-items:center;gap:7px}.swatch{display:inline-block;width:15px;height:15px;border:1px solid #727d73}.dot{border-radius:50%;background:#71508a}.ring{border-radius:50%;background:#fffdf7;border:2px solid #225da0}.top{background:#bde8de}.bottom{background:#f4d1b3}.both{background:#d5c9e6}.closed{background:#d8bdde}.js .pattern-panel[hidden]{display:none}.js .js-only{display:block}.js-only{display:none}.no-flow .layer-flow,.no-white .layer-whitespace,.no-quarter .layer-quarter,.no-half .layer-half,.no-labels .layer-labels{display:none}.witness{border:1px solid var(--line);padding:24px;background:#fffdf7;margin-top:24px}.filters{display:flex;flex-wrap:wrap;gap:14px 20px}.filters label{display:flex;flex-direction:column;gap:5px;font-size:13px}.filters select{font-size:14px;max-width:100%}.witness-nav{display:flex;gap:12px;align-items:center;flex-wrap:wrap;margin:20px 0}.witness-nav output{font-size:14px;min-width:165px}.witness-title{font-size:16px;font-weight:600;margin:0 0 14px}.window-pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}.window-box{border:1px solid var(--line);min-width:0;padding:14px}.window-box h5{font-size:14px;margin:0}.window-box svg{width:100%;display:block;margin-top:10px}.window-box p{font-size:13px;color:var(--muted);margin:8px 0 0}.source-ink{color:#b04732}.target-ink{color:#215ca8}.witness-limit{font-size:14px;color:var(--muted)}.witness-list{max-height:330px;overflow:auto}.witness-list button{font-size:12px;padding:4px 8px}.witness-list td{padding:7px 10px}.overlap-pairs{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}.overlap-pair{padding:20px;border:1px solid var(--line);background:#fffdf7}.overlap-pair h3{font-size:23px}.overlap-pair p{font-size:14px;margin-bottom:0}.contrasts{display:grid;grid-template-columns:1fr;gap:24px}.contrast{background:#fffdf7;border:1px solid var(--line);padding:24px}.contrast h3{font-size:27px}.contrast-pair{display:grid;grid-template-columns:1fr 1fr;gap:20px}.contrast-pair figure{margin:0;min-width:0}.contrast-pair svg{width:100%;display:block;background:#fcfaf4;border:1px solid var(--line)}.contrast-pair figcaption{font-size:13px;margin:8px 0 14px}.checks{margin:12px 0;padding-left:20px;font-size:14px}.checks li{margin:7px 0}.notes{border-top:1px solid var(--line);padding-top:36px}.notes h2{font-size:29px;margin-top:42px}.notes h3{font-size:23px;margin-top:28px}.notes>p,.notes>ul,.notes>ol{max-width:90ch}.notes pre{padding:18px;background:#eaece1;overflow:auto}.notes code{font-size:14px}.notes img{max-width:100%;height:auto}.notes table{display:block;overflow:auto}.download-row{display:flex;flex-wrap:wrap;gap:15px 25px}.notice{padding:14px 18px;background:#f0e5d8;border-left:3px solid var(--warm)}.empty{padding:20px;background:#f5f1e7}.visually-hidden{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}footer{margin-top:45px}
.contrasts{grid-template-columns:minmax(0,1fr)}.contrast{min-width:0}.js .selection-controls,.js .control-bar{display:flex}
@media(max-width:760px){.pattern-menu{grid-template-columns:repeat(2,minmax(0,1fr))}.method-grid{grid-template-columns:1fr;gap:18px}.split{grid-template-columns:minmax(0,1fr);gap:20px}.panel-heading{display:block}.pattern-source{max-width:none}.pattern-panel,.witness,.contrast{padding:18px}.plot{margin-left:-18px;margin-right:-18px}.pattern-panel h3{font-size:28px}.control-bar{padding:14px;gap:14px}.summary-grid{grid-template-columns:repeat(2,minmax(0,1fr))}.contrast-pair,.overlap-pairs,.window-pair{grid-template-columns:minmax(0,1fr)}.window-pair{gap:10px}.window-box{padding:10px}.window-box h5{font-size:13px}h2{font-size:29px}.scorecard th:first-child,.scorecard td:first-child{min-width:145px;max-width:145px}.selection-controls select{flex:1;min-width:180px}.selection-controls label{flex-basis:100%}.filters label{width:100%}.filters select{width:100%}.notes{font-size:15px}}
@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}}
'''


JS = r'''
(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const html = s => String(s).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const num = n => Number.isInteger(n) ? String(n) : Number(n.toFixed(3)).toString();
  let report, current, matches=[], index=0;
  const frames = {'identity':'identity', 'quarter-left':'quarter-turn left', 'quarter-right':'quarter-turn right', 'half-turn':'half-turn', 'vertical-mirror':'vertical mirror', 'horizontal-mirror':'horizontal mirror', 'rising-diagonal':'rising diagonal', 'falling-diagonal':'falling diagonal'};
  const describe = m => `${m.source} → ${m.target} · ${frames[m.frame]} · ${m.reversed ? 'backward' : 'forward'}`;
  const phase = (n,len) => ((n%len)+len)%len;
  function picked(m) {
    const kind=$('read-kind').value;
    if(kind==='equal') return {n:m.equalRun,p:1,q:1};
    if(kind==='proportion') return {n:m.similarRun,p:m.scaleNumerator,q:m.scaleDenominator};
    if(kind==='direction') return {n:m.directionRun,p:null,q:null};
    return {n:m.cut.run,p:m.cut.scaleNumerator,q:m.cut.scaleDenominator};
  }
  function pathVectors(m,target) {
    const read=picked(m), n=current.segments.length, list=[];
    for(let k=0;k<read.n;k++) {
      const slot=phase((target?m.target:m.source)+(target&&m.reversed?-k:k),n);
      const item=current.segments[slot], sense=target&&m.reversed?-1:1;
      let x=item.x*sense,y=item.y*sense,length=Math.abs(x)+Math.abs(y),extent=length;
      if($('read-kind').value==='direction') extent=1;
      if($('read-kind').value==='cut'&&(k===0||k===read.n-1)) {
        const lim=k===0?m.cut.firstLimitNumerator:m.cut.lastLimitNumerator;
        extent=lim/(target?read.q:read.p);
      }
      list.push({x:x*extent/length,y:y*extent/length,slot,extent});
    }
    return list;
  }
  function miniSvg(vectors,color,name) {
    const points=[[0,0]];
    for(const v of vectors) {const p=points.at(-1);points.push([p[0]+v.x,p[1]+v.y]);}
    const xs=points.map(p=>p[0]),ys=points.map(p=>p[1]);
    const minX=Math.min(...xs),maxX=Math.max(...xs),minY=Math.min(...ys),maxY=Math.max(...ys);
    const scale=Math.min(370/Math.max(1,maxX-minX),155/Math.max(1,maxY-minY));
    const px=(420-(maxX-minX)*scale)/2,py=(195-(maxY-minY)*scale)/2;
    const project=p=>[px+(p[0]-minX)*scale,py+(maxY-p[1])*scale];
    const screen=points.map(project);
    let result=`<svg viewBox="0 0 420 195" role="img" aria-label="${html(name)}"><polyline points="${screen.map(p=>p.join(',')).join(' ')}" fill="none" stroke="${color}" stroke-width="4"/>`;
    for(let i=0;i<vectors.length;i++) {
      const a=screen[i],b=screen[i+1],dx=b[0]-a[0],dy=b[1]-a[1],l=Math.hypot(dx,dy),ux=dx/l,uy=dy/l;
      const x=(a[0]+b[0])/2,y=(a[1]+b[1])/2,s=5;
      const triangle=[[x+ux*s,y+uy*s],[x-ux*s+uy*s*.7,y-uy*s-ux*s*.7],[x-ux*s-uy*s*.7,y-uy*s+ux*s*.7]];
      result+=`<polygon points="${triangle.map(p=>p.join(',')).join(' ')}" fill="${color}"/><text x="${x+(dy?12:0)}" y="${y+(dx?-10:4)}" text-anchor="middle" font-family="system-ui" font-size="15" fill="${color}">${vectors[i].slot}</text>`;
    }
    result+=`<circle cx="${screen[0][0]}" cy="${screen[0][1]}" r="5" fill="#fffdf7" stroke="${color}" stroke-width="2"/></svg>`;
    return result;
  }
  function showMatch() {
    $('witness-count').textContent=matches.length?`${index+1} of ${matches.length} witnesses`:'No witnesses in this read';
    $('previous-witness').disabled=index===0||!matches.length;
    $('next-witness').disabled=index>=matches.length-1;
    if(!matches.length){$('witness-content').innerHTML='<p class="empty">This combination has no recorded match. Try another transform or a weaker read.</p>';return;}
    const m=matches[index],r=picked(m),kind=$('read-kind').value;
    const source=pathVectors(m,false),target=pathVectors(m,true);
    const scale=r.p==null?'Extents omitted':`Target/source scale ${r.p}:${r.q}`;
    const lengthMessage=kind==='direction'?'Every reach is drawn as one display step here. This comparison preserves heading order while discarding extent.':kind==='cut'?'Outer arms end at the largest jointly supported cuts; smaller positive cuts also match. Interior reaches are complete.': 'Every reach in this comparison is complete. Each panel fits its own bounds; read the scale ratio instead of comparing panel widths.';
    $('witness-content').innerHTML=`<p class="witness-title">${html(describe(m))}</p><p>${r.n} reaches · ${scale}. Source is transformed, then compared to the target in the stated traversal.</p><div class="window-pair"><div class="window-box"><h5 class="source-ink">Source · authored direction</h5>${miniSvg(source,'#b04732','Source window in authored direction')}<p>Slots ${source.map(v=>v.slot).join(' · ')}</p></div><div class="window-box"><h5 class="target-ink">Target · ${m.reversed?'backward comparison':'authored direction'}</h5>${miniSvg(target,'#215ca8',m.reversed?'Target window read opposite to the pen':'Target window in authored direction')}<p>Slots ${target.map(v=>v.slot).join(' · ')}</p></div></div><p class="witness-limit">${lengthMessage} Open circles mark comparison starts. ${m.reversed?'Blue arrows oppose the target pen direction; they describe this read.':'Both sets of arrows follow their source pen.'}</p>`;
  }
  function filterMatches() {
    const transform=$('frame-filter').value,traversal=$('traversal-filter').value;
    matches=current.windows.matches.filter(m=>picked(m).n>=3 && (transform==='all'||m.frame===transform) && (traversal==='all'||m.reversed===(traversal==='backward')) && !(!m.reversed&&m.source===m.target&&m.frame==='identity'));
    matches.sort((a,b)=>picked(b).n-picked(a).n||a.source-b.source||a.target-b.target||a.frame.localeCompare(b.frame));
    index=0;
    const limit=matches.length;
    $('witness-list').innerHTML=matches.length?`<div class="table-scroll witness-list"><table><thead><tr><th>Inspect</th><th>Source → target</th><th>Transform</th><th>Read</th><th>Reaches</th><th>Scale</th></tr></thead><tbody>${matches.slice(0,limit).map((m,i)=>{const r=picked(m);return `<tr><td><button type="button" data-witness="${i}" aria-label="Inspect witness ${i+1}">View</button></td><td>${m.source} → ${m.target}</td><td>${html(frames[m.frame])}</td><td>${m.reversed?'backward':'forward'}</td><td>${r.n}</td><td>${r.p==null?'omitted':r.p+':'+r.q}</td></tr>`;}).join('')}</tbody></table></div>`:'<p>No rows for these filters.</p>';
    showMatch();
  }
  function selectPattern(id,updateHash=true) {
    current=report.patterns.find(p=>p.id===id)||report.patterns[0];
    $('pattern-select').value=current.id;
    document.querySelectorAll('.pattern-panel').forEach(panel=>panel.hidden=panel.dataset.pattern!==current.id);
    document.querySelectorAll('[data-pick]').forEach(link=>link.setAttribute('aria-current',String(link.dataset.pick===current.id)));
    $('witness-pattern').textContent=current.name;
    if(updateHash)history.replaceState(null,'',`#pattern-${current.id}`);
    filterMatches();
  }
  function stepPattern(dir) {const n=report.patterns.length;selectPattern(report.patterns[phase(report.patterns.indexOf(current)+dir,n)].id);}
  async function start() {
    const response=await fetch('study-data.json');if(!response.ok)throw new Error('Recorded data unavailable');report=await response.json();
    document.documentElement.classList.add('js');
    $('pattern-select').addEventListener('change',e=>selectPattern(e.target.value));
    $('pattern-select').addEventListener('keydown',e=>{if(['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)){e.preventDefault();stepPattern(['ArrowLeft','ArrowUp'].includes(e.key)?-1:1);}});
    $('previous-pattern').addEventListener('click',()=>stepPattern(-1));$('next-pattern').addEventListener('click',()=>stepPattern(1));
    document.querySelectorAll('[data-pick]').forEach(link=>link.addEventListener('click',e=>{e.preventDefault();selectPattern(link.dataset.pick);$('inspector').scrollIntoView({block:'start'});}));
    document.querySelectorAll('[data-layer]').forEach(input=>input.addEventListener('change',()=>document.body.classList.toggle('no-'+input.dataset.layer,!input.checked)));
    ['read-kind','frame-filter','traversal-filter'].forEach(id=>$(id).addEventListener('change',filterMatches));
    $('previous-witness').addEventListener('click',()=>{index=Math.max(0,index-1);showMatch();});$('next-witness').addEventListener('click',()=>{index=Math.min(matches.length-1,index+1);showMatch();});
    $('witness-list').addEventListener('click',e=>{const button=e.target.closest('[data-witness]');if(button){index=Number(button.dataset.witness);showMatch();$('witness-content').scrollIntoView({block:'nearest'});}});
    const id=location.hash.replace('#pattern-','');selectPattern(id,false);
  }
  start().catch(error=>{console.error(error);const note=document.createElement('p');note.className='notice';note.textContent='Interactive controls could not load. All recorded diagrams and notes remain below; the JSON download contains the complete witnesses.';$('inspector').prepend(note);});
})();
'''


def render_contrasts(report):
    patterns = {p["id"]: p for p in report["patterns"]}
    output = []
    for c in report.get("contrasts", []):
        parent = patterns[c["parent"]]
        score = c["score"]
        base_picture = parent.get("repeatScore", parent["score"])
        changed_picture = c.get("repeatScore", score)
        extents = [(max(p["x"] for p in s["sites"])-min(p["x"] for p in s["sites"]),
                    max(p["y"] for p in s["sites"])-min(p["y"] for p in s["sites"]))
                   for s in [base_picture, changed_picture]]
        common = tuple(max(e[i] for e in extents) for i in range(2))
        before = score_svg(base_picture, c["id"]+"-before", "Reference: "+parent["name"], common)
        after = score_svg(changed_picture, c["id"]+"-after", c["name"], common)
        checks = []
        for check in c.get("checks", []):
            state = "unavailable" if check["passed"] is None else "holds" if check["passed"] else "disturbed"
            count = f'{check["misses"]} miss(es) / {check["checked"]} checks'
            checks.append(f'<li><strong>{esc(check["claim"])}</strong> {state}: {count}.{(" "+esc(check["limit"])) if check.get("limit") else ""}</li>')
        checks = "".join(checks)
        material = table(["Read over the displayed two periods", "Reference", "Changed"],
                         [[label, str(base_picture[field]), str(changed_picture[field])] for label, field in [
                             ("Participating strokes", "depositedStrokes"), ("Native material components", "nativeComponents"),
                             ("Native ends", "nativeEnds"), ("Collinear overlapping pairs", "collinearOverlaps"),
                             ("Interior crossings", "interiorCrossings"), ("Contacts without native joins", "unjoinedContacts")]])
        limit = score.get("descriptorLimit")
        output.append(f'''<article class="contrast"><h3>{esc(c['name'])}</h3><p>{esc(c['change'])}</p><div class="contrast-pair"><figure>{before}<figcaption>Reference: {esc(parent['name'])}, two periods.</figcaption></figure><figure>{after}<figcaption>Changed material at the same display scale. Dashed strokes retain motion with deposition off. Dots mark native sites; coincident identities can lie on top of each other.</figcaption></figure></div><p>{esc(c.get('interpretation',''))}</p>{material}<ul class="checks">{checks}</ul>{f'<p class="notice">{esc(limit)}</p>' if limit else ''}</article>''')
    return "".join(output)


def figure_root(width, height, title, desc):
    svg = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": f"0 0 {width} {height}",
                             "width": str(width), "height": str(height), "role": "img"})
    node(svg, "title", title)
    node(svg, "desc", desc)
    node(svg, "rect", width=width, height=height, fill="#fcfaf4")
    return svg


def figure_label(svg, x, y, text, size=16, color=INK, bold=False):
    return node(svg, "text", text, x=x, y=y, fill=color, font_size=size, font_family="Arial, sans-serif",
                font_weight="bold" if bold else "normal")


def paste_svg(parent, source, x, y, width, height, layers=False):
    child = ET.fromstring(source)
    child.set("x", str(x))
    child.set("y", str(y))
    child.set("width", str(width))
    child.set("height", str(height))
    if layers:
        for element in child.iter():
            if element.get("class") in ["layer-flow", "layer-quarter", "layer-half", "layer-labels"]:
                element.set("display", "none")
    parent.append(child)


def render_overview_figure(report):
    rows = math.ceil(len(report["patterns"])/2)
    svg = figure_root(1120, 100+rows*275, "Ornament signatures: six recorded references",
                      "Six cardinal centerline constructions and their connected white regions. Green regions open at the top; orange regions at the bottom.")
    figure_label(svg, 24, 38, "Different constructions, overlapping signatures", 28, bold=True)
    figure_label(svg, 24, 67, "Recorded centerlines. Green: opens at top. Orange: opens at bottom. Two periods per panel.", 17, "#62716b")
    for i, p in enumerate(report["patterns"]):
        x, y = 18+(i%2)*560, 105+(i//2)*275
        figure_label(svg, x+6, y+15, p["name"], 23, bold=True)
        figure_label(svg, x+6, y+41, p["source"], 13, "#62716b")
        paste_svg(svg, pattern_svg(p, "overview-"+p["id"], True), x, y+49, 540, 170, True)
        run = max((r["turns"] for r in p["course"]["turnRuns"]), default=0)
        figure_label(svg, x+6, y+240, f'{len(p["segments"])} reaches · same-hand run {run} · shrinking turns {p["course"]["longestContraction"]}', 15)
        line(svg, (x+6, y+258), (x+534, y+258), stroke="#cec9ba")
    return svg_text(svg)+"\n"


def render_contrast_figure(report):
    patterns = {p["id"]:p for p in report["patterns"]}
    svg = figure_root(1120, 88+len(report.get("contrasts", []))*280, "Controlled changes in ornament structure",
                      "Each row compares the reference on the left and a changed structure on the right at the same tick scale. Dashed strokes mean motion with no material deposited.")
    figure_label(svg, 24, 38, "The drawing can survive while a promise fails", 28, bold=True)
    figure_label(svg, 24, 66, "Reference at left, change at right. Shared scale per pair. Two periods. Dashed: motion without a deposit.", 17, "#62716b")
    for i, c in enumerate(report.get("contrasts", [])):
        y = 98+i*280
        parent = patterns[c["parent"]]
        base, changed = parent.get("repeatScore", parent["score"]), c.get("repeatScore", c["score"])
        extent = tuple(max(max(p[axis] for p in s["sites"])-min(p[axis] for p in s["sites"]) for s in [base, changed]) for axis in ["x", "y"])
        figure_label(svg, 24, y+10, c["name"], 22, bold=True)
        for row, part in enumerate(textwrap.wrap(c["change"], width=115)):
            figure_label(svg, 24, y+35+row*19, part, 15, "#62716b")
        paste_svg(svg, score_svg(base, c["id"]+"-fig-base", parent["name"], extent), 24, y+60, 520, 175)
        paste_svg(svg, score_svg(changed, c["id"]+"-fig-change", c["name"], extent), 574, y+60, 520, 175)
        checks = c.get("checks", [])
        failed = [ch["key"] for ch in checks if ch["passed"] is False]
        outcome = "Disturbed: "+", ".join(failed) if failed else "All selected checks hold."
        figure_label(svg, 24, y+253, outcome, 14, "#a15435" if failed else "#276d68")
        line(svg, (24, y+265), (1096, y+265), stroke="#cec9ba")
    return svg_text(svg)+"\n"


def render_page(report, notes):
    patterns = report["patterns"]
    title_line = re.search(r"^# (.+)$", notes, re.M)
    title = title_line.group(1) if title_line else "The signatures inside a pattern"
    text = re.sub(r"^# .+\n", "", notes, count=1)
    chunks = re.split(r"(?=^## )", text, maxsplit=1, flags=re.M)
    overview = markdown.markdown(chunks[0].strip(), extensions=["tables", "fenced_code"])
    rest = markdown.markdown(chunks[1] if len(chunks)>1 else "", extensions=["tables", "fenced_code", "toc"])
    cards = "".join(f'<a class="pattern-card" data-pick="{esc(p["id"])}" href="#pattern-{esc(p["id"])}">{pattern_svg(p,"thumb-"+p["id"],True)}<h3>{esc(p["name"])}</h3><p>{esc(p["source"])}</p></a>' for p in patterns)
    select = "".join(f'<option value="{esc(p["id"])}">{esc(p["name"])}</option>' for p in patterns)
    panels = "".join(pattern_panel(p, i) for i, p in enumerate(patterns))
    selection = table(["Source selection", "Status", "Reason"], [[esc(s["source"]), esc(s["status"]), esc(s["reason"])] for s in report.get("selection", [])])
    names = {p["id"]: p["name"] for p in patterns}
    labels = {f["key"]: f["label"] for p in patterns for f in p.get("features", [])}
    overlaps = "".join(f'<article class="overlap-pair"><h3>{esc(names[o["first"]])} + {esc(names[o["second"]])}</h3><p><strong>Shared readings:</strong> {esc("; ".join(labels.get(k,k) for k in o["shared"])) or "none in this signature"}.</p><p><strong>Different readings:</strong> {esc("; ".join(labels.get(k,k) for k in o["different"])) or "none in this signature"}.</p></article>' for o in report.get("overlaps", []))
    frame_options = "".join(f'<option value="{name}">{label}</option>' for name, label in [
        ("identity", "Identity / translation"), ("quarter-left", "Quarter-turn left"), ("half-turn", "Half-turn"),
        ("quarter-right", "Quarter-turn right"), ("vertical-mirror", "Vertical mirror"), ("horizontal-mirror", "Horizontal mirror"),
        ("rising-diagonal", "Rising diagonal"), ("falling-diagonal", "Falling diagonal")])
    return f'''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<base href="/ornaments/signatures/"><title>{esc(title)} · Gojo ornaments</title>
<meta name="description" content="An illustrated Gojo study of overlapping ornament signatures: path order, transformations, directed corners, symmetry pivots and connected whitespace.">
<link rel="stylesheet" href="/ornaments/ornaments.css"><link rel="stylesheet" href="study.css"><script src="study.js" defer></script></head>
<body class="no-quarter no-half no-labels no-flow"><a class="skip" href="#main">Skip to the study</a><main id="main">
<nav aria-label="Site"><a href="/ornaments/">← Ornament studies</a><a href="/">Gojo</a></nav>
<header><p class="eyebrow">Recorded study · ink, flow and empty space</p><h1>{esc(title)}</h1><div class="overview">{overview}</div></header>
<nav class="subnav" aria-label="Study"><a href="#collection">Patterns</a><a href="#scorecard">Scorecard</a><a href="#inspector">Inspect a pattern</a><a href="#contrasts">Controlled changes</a><a href="#notes">Study notes</a></nav>
<div class="scope">These are explicit centerline reconstructions of selected examples, read through a small declared grammar. The measurements describe these structures. A printed image leaves its original pen order, hidden crossings and construction history open.</div>
<div class="method-grid"><div><strong>1. Preserve the construction</strong><p>Keep ordered reaches, frame changes, pen participation and repeat seams available.</p></div><div><strong>2. Ask several small questions</strong><p>Compare whole bands, short windows, corners, pivot spacing and the white channels between lines.</p></div><div><strong>3. Change one thing</strong><p>Observe which readings survive and which fail. Retain their overlap instead of summing them as independent evidence.</p></div></div>
<section id="collection"><h2>Patterns in the study</h2><p class="section-lead">Select a reconstruction to examine its layers. Source rows count from the top of each supplied sheet. The drawings here are new centerline renderings of the recorded Gojo traces.</p><div class="pattern-menu">{cards}</div><details><summary>What was selected, and what remains open</summary>{selection}</details></section>
<section id="scorecard"><h2>A signature with overlapping readings</h2><p class="section-lead">Each row answers one question. A single corner can support several rows and many matches. These are descriptive features, not independent votes or a universal aesthetic score.</p>{render_scorecard(patterns)}<details><summary>Compare the shared and distinguishing features</summary><div class="overlap-pairs">{overlaps}</div><p>A matching feature means only that the two recorded readings agree at this resolution. It does not establish identical shapes, machines or histories.</p></details></section>
<section id="inspector"><h2>Read the same pattern several ways</h2><p class="section-lead">Toggle layers to see which parts participate in each reading.</p>
<div class="selection-controls js-only"><label for="pattern-select">Pattern</label><button id="previous-pattern" type="button" aria-label="Previous pattern">←</button><select id="pattern-select">{select}</select><button id="next-pattern" type="button" aria-label="Next pattern">→</button></div>
<div class="control-bar js-only"><label><input type="checkbox" data-layer="white" checked>White regions</label><label><input type="checkbox" data-layer="flow">Pen direction</label><label><input type="checkbox" data-layer="quarter">Quarter-turn pivots</label><label><input type="checkbox" data-layer="half">Half-turn pivots</label><label><input type="checkbox" data-layer="labels">Reach slots</label></div>
<div class="legend"><span><i class="swatch top"></i>Opens at top</span><span><i class="swatch bottom"></i>Opens at bottom</span><span><i class="swatch both"></i>Opens at both</span><span><i class="swatch closed"></i>Enclosed</span><span><i class="swatch dot"></i>Shared-tip quarter-turn</span><span><i class="swatch ring"></i>Whole-band half-turn</span></div>
{panels}
<div class="witness js-only"><p class="eyebrow">Bounded witness explorer</p><h3 id="witness-pattern">Window comparisons</h3><p>A window is an ordered run of at least three reaches. Each witness uses one frame, one traversal and one scale across the whole run. The list covers the recorded search, including windows crossing a repeat seam; unchanged self-comparisons are hidden here.</p>
<div class="filters"><label for="read-kind">What must agree<select id="read-kind"><option value="equal">Whole reaches, equal lengths</option><option value="proportion">Whole reaches, one scale ratio</option><option value="cut">Partial outer arms, one scale ratio</option><option value="direction">Directions only</option></select></label><label for="frame-filter">Transform<select id="frame-filter"><option value="all">All eight frames</option>{frame_options}</select></label><label for="traversal-filter">Target traversal<select id="traversal-filter"><option value="all">Forward and backward</option><option value="forward">Forward only</option><option value="backward">Backward only</option></select></label></div>
<div class="witness-nav"><button id="previous-witness" type="button">← Previous</button><output id="witness-count" aria-live="polite"></output><button id="next-witness" type="button">Next →</button></div><div id="witness-content"></div><details><summary>All rows in the current selection</summary><div id="witness-list"></div></details></div></section>
<section id="contrasts"><h2>What changes when a rule is disturbed?</h2><p class="section-lead">These cases deliberately disturb a specific promise. Their failures identify what the readings notice. A broken promise may be useful in another design; these examples do not define universal bad taste.</p><div class="contrasts">{render_contrasts(report)}</div></section>
<section class="notes" id="notes"><p class="eyebrow">Methods, interpretation and limits</p><div class="download-row"><a href="notes.md">Markdown equivalent</a><a href="study-data.json">Complete recorded JSON</a></div>{rest}</section>
<footer><p>This page presents recorded experimental reads. It neither learns a pattern nor runs a live settlement process. Geometry and witnesses come from the same report; explanatory labels are presentation.</p><a href="/ornaments/">Return to ornament studies</a></footer></main></body></html>'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--notes", type=Path, default=Path(__file__).with_name("PatternStudy.md"))
    args = parser.parse_args()
    report = json.loads(args.report.read_text(encoding="utf-8-sig"))
    notes = args.notes.read_text(encoding="utf-8-sig")
    args.output.mkdir(parents=True, exist_ok=True)
    for p in report["patterns"]:
        if "signature" in p:
            signature = p.pop("signature")
            p.update({**signature, **p})
    (args.notes.parent/"PatternOverview.svg").write_text(render_overview_figure(report), encoding="utf-8")
    (args.notes.parent/"PatternContrasts.svg").write_text(render_contrast_figure(report), encoding="utf-8")
    (args.output/"index.html").write_text(render_page(report, notes), encoding="utf-8")
    (args.output/"study.css").write_text(CSS.strip()+"\n", encoding="utf-8")
    (args.output/"study.js").write_text(JS.strip()+"\n", encoding="utf-8")
    (args.output/"study-data.json").write_text(json.dumps(report, ensure_ascii=False, separators=(",", ":"))+"\n", encoding="utf-8")
    (args.output/"notes.md").write_text(notes, encoding="utf-8")
    # Only copy figures explicitly linked by the canonical notes. They remain
    # presentation evidence, and we never publish the supplied source sheets.
    for target in re.findall(r"!?\[[^\]]*\]\(([^ )]+)(?:[^)]*)\)", notes):
        if not re.match(r"^[a-z]+:|^/|^#", target) and target.endswith(".svg"):
            source = args.notes.parent/target
            if source.is_file():
                destination = args.output/target
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(source, destination)
    print(f"Rendered {len(report['patterns'])} patterns and {len(report.get('contrasts', []))} controlled changes to {args.output}")


if __name__ == "__main__":
    main()
