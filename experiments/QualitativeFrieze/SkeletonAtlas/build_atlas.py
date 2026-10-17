"""Build authored visual references, not executable Gojo pattern models.

Coordinates here belong only to illustration. The source catalogue records
structural targets without claiming that the kernel has generated or proved
them. Run with Python 3.10+; only the standard library is required.
"""

from html import escape
import json
import math
from pathlib import Path
import textwrap

from extra_patterns import EXTRA_PATTERNS


HERE = Path(__file__).resolve().parent
OUT = HERE.parent / "results" / "skeleton-atlas"
PAPER = "#fffdf7"
INK = "#203e42"
RUST = "#aa603f"
SVG_STYLE = """.ink,.second,.guide,.scaffold,.frame{fill:none;stroke-linecap:round;stroke-linejoin:round}
.ink{stroke:#203e42}.second{stroke:#aa603f}
.scaffold{stroke:#c7d3c4;stroke-width:1}
.guide{stroke:#adbbb0;stroke-dasharray:4 5;stroke-width:1}
.frame{stroke:#c5cfc1;stroke-dasharray:3 6;stroke-width:1}
.node{fill:#203e42;stroke:#fffdf7;stroke-width:1}
.wash{fill:#e0eadd;stroke:none}.field{fill:#f0dec9;stroke:none}
.solid{fill:#203e42;stroke:none}.underpass{stroke:#fffdf7;fill:none}
.diagram-label{font:12px 'Segoe UI',sans-serif;fill:#708276}
"""


def num(value):
    return f"{value:.3f}".rstrip("0").rstrip(".")


class Canvas:
    def __init__(self):
        self.items = []

    def poly(self, points, role="ink", closed=False, width=2, fill="none"):
        coords = " ".join(f"{num(x)},{num(y)}" for x, y in points)
        tag = "polygon" if closed else "polyline"
        self.items.append(f'<{tag} class="{role}" points="{coords}" stroke-width="{num(width)}" fill="{fill}"/>')

    def line(self, a, b, role="ink", width=2):
        self.poly([a, b], role=role, width=width)

    def dot(self, point, r=2.7, role="node"):
        x, y = point
        self.items.append(f'<circle class="{role}" cx="{num(x)}" cy="{num(y)}" r="{num(r)}"/>')

    def region(self, points, role="wash"):
        self.poly(points, role=role, closed=True, width=0)

    def text(self, point, label, role="diagram-label"):
        self.items.append(f'<text x="{num(point[0])}" y="{num(point[1])}" class="{role}">{escape(label)}</text>')

    def raw(self, value):
        self.items.append(value)

    def content(self):
        return "".join(self.items)


def regular(cx, cy, radius, count, phase=-math.pi / 2):
    return [(cx + radius * math.cos(phase + i * math.tau / count),
             cy + radius * math.sin(phase + i * math.tau / count)) for i in range(count)]


def grid(c, step=40, role="guide"):
    for x in range(40, 441, step):
        c.line((x, 30), (x, 230), role)
    for y in range(30, 231, step):
        c.line((40, y), (440, y), role)


def diamond(c, x, y, r=20, role="ink"):
    c.poly([(x-r, y), (x, y-r), (x+r, y), (x, y+r)], role, closed=True)


def key_points(start=20, end=460, low=186, high=74, period=88):
    """A continuous stepped-return word with no immediate retraced edge."""
    points = [(start, low)]
    x = start
    while x + period <= end + .1:
        ux,uy=period/5,(low-high)/4
        # The established nested-key word: U4 R4 D3 L2 U R U L2 D3 R4.
        # Its last horizontal return meets the next rise without retracing.
        offsets=[(0,-4),(4,-4),(4,-1),(2,-1),(2,-2),
                 (3,-2),(3,-3),(1,-3),(1,0),(5,0)]
        points.extend((x+dx*ux,low+dy*uy) for dx,dy in offsets)
        x += period
    return points


def draw_key(c):
    c.poly(key_points(), width=2.6)
    c.line((20, 54), (460, 54), "guide")
    c.line((20, 206), (460, 206), "guide")


def draw_facing(c):
    top = key_points(start=20, end=460, low=111, high=31)
    bottom = [(x, 260-y) for x, y in top]
    c.poly(top, width=2.3)
    c.poly(bottom, "second", width=2.3)
    c.line((15, 130), (465, 130), "guide")


def draw_glide(c):
    def motif(x, flip=False, role="ink"):
        points = [(x, 182), (x+22, 182), (x+22, 147.333),
                  (x+44, 147.333), (x+44, 112.667), (x+66, 112.667), (x+66, 78), (x+88, 78)]
        if flip:
            points = [(px, 260-py) for px, py in points]
        c.poly(points, role, width=2.5)
    for i in range(5):
        motif(20+i*88, i % 2 == 1, "second" if i % 2 else "ink")
    c.line((20, 130), (460, 130), "guide")
    c.text((22, 224), "A → reflected B → A")


def draw_drop(c, stage=2):
    for row in range(4):
        y = 40 + row*60
        offset = 40 if row % 2 and stage else 0
        for col in range(6 if offset else 5):
            x = 40 + col*80 - offset
            if x < 18 or x > 462:
                continue
            diamond(c, x, y, 19, "second" if row % 2 else "ink")
            if stage == 2:
                for xx in (x-40, x+40):
                    if row < 3 and 18 <= xx <= 462:
                        c.line((x, y), (xx, y+60), "scaffold")


def octagon_points(x, y, s=64):
    a = s/(2+math.sqrt(2))
    return [(x+a,y), (x+s-a,y), (x+s,y+a), (x+s,y+s-a),
            (x+s-a,y+s), (x+a,y+s), (x,y+s-a), (x,y+a)]


def draw_octagons(c, stage=2):
    s = 68
    if stage == 0:
        c.poly(octagon_points(196, 96, s), closed=True, width=2.5)
        return
    rows, cols = (2, 3) if stage == 1 else (3, 6)
    x0 = (480-cols*s)/2
    y0 = (260-rows*s)/2
    edges = set()
    for r in range(rows):
        for col in range(cols):
            points = octagon_points(x0+col*s, y0+r*s, s)
            for a, b in zip(points, points[1:]+points[:1]):
                key = tuple(sorted((tuple(round(v, 5) for v in a), tuple(round(v, 5) for v in b))))
                if key not in edges:
                    c.line(a, b, width=2)
                    edges.add(key)
    inset = s/(2+math.sqrt(2))
    for r in range(1, rows):
        for col in range(1, cols):
            x, y = x0+col*s, y0+r*s
            c.region([(x-inset,y), (x,y-inset), (x+inset,y), (x,y+inset)])
            diamond(c, x, y, inset, "second")


def stepped_boundary(x0, y0, width=80, height=44):
    return [(x0,y0), (x0+width*.25,y0), (x0+width*.25,y0-height),
            (x0+width*.75,y0-height), (x0+width*.75,y0), (x0+width,y0)]


def draw_counterchange(c, stage=2):
    # Two roles share one stepped boundary. Shaded membership is structural,
    # rather than a decorated stroke or a claim about Euclidean equal area.
    rows = [150] if stage==0 else [90,150,210]
    boundaries=[]
    for y in rows:
        points = []
        for i in range(5):
            segment = stepped_boundary(40+i*80, y, 80, 30)
            points.extend(segment if not points else segment[1:])
        boundaries.append(points)
    levels=[[(40,30),(440,30)],*boundaries,[(40,230),(440,230)]]
    for row,(upper,lower) in enumerate(zip(levels,levels[1:])):
        first=(row+int(stage==2))%2==0
        c.region(upper+list(reversed(lower)),"wash" if first else "field")
    for points in boundaries:
        c.poly(points, width=2.2)
    c.poly([(40,30),(440,30),(440,230),(40,230)],closed=True,width=1)
    c.text((48,49),"A" if stage!=2 else "B")
    c.text((48,108),"B" if stage!=2 else "A")
    if stage == 2:
        c.text((40, 25), "same partition · A ↔ B")


def draw_weave(c, stage=2):
    if stage == 0:
        c.line((100, 58), (380, 202), width=2.6)
        c.line((100, 202), (380, 58), "underpass", width=8)
        c.line((100, 202), (380, 58), "second", width=2.6)
        return
    xs = [132+i*54 for i in range(5)] if stage==1 else [24+i*54 for i in range(9)]
    p = [(x, 74 if i % 2 == 0 else 186) for i, x in enumerate(xs)]
    q = [(x, 186 if i % 2 == 0 else 74) for i, x in enumerate(xs)]
    c.poly(p, width=2.5)
    c.poly(q, "second", width=2.5)
    # Redraw a short overpass at each crossing to expose two thread identities.
    for i in range(len(xs)-1):
        cx = (xs[i]+xs[i+1])/2
        a, b = (p[i], p[i+1]) if i % 2 == 0 else (q[i], q[i+1])
        role = "ink" if i % 2 == 0 else "second"
        dx, dy = (b[0]-a[0])*.08, (b[1]-a[1])*.08
        c.line((cx-dx,130-dy), (cx+dx,130+dy), "underpass", width=8)
        c.line((cx-dx,130-dy), (cx+dx,130+dy), role, width=2.5)
    if stage == 2:
        c.line((24, 52), (456, 52), "guide")
        c.line((24, 208), (456, 208), "guide")


def draw_selections(c):
    for panel in range(2):
        x0 = 20 + panel*242
        for r in range(3):
            for col in range(3):
                x, y, s = x0+col*65, 28+r*65, 65
                square=[(x,y),(x+s,y),(x+s,y+s),(x,y+s)]
                lozenge=[(x,y+s/2),(x+s/2,y),(x+s,y+s/2),(x+s/2,y+s)]
                c.poly(square,"scaffold",closed=True)
                c.poly(lozenge,"scaffold",closed=True)
                if panel == 0:
                    c.poly(lozenge,closed=True)
                elif (r+col)%2==0:
                    c.poly(square,closed=True)
                else:
                    c.poly(lozenge,"second",closed=True)
        c.text((x0,245), "diamond chains" if panel == 0 else "courts + linking diamonds")


def glossary(c, family, stage):
    if family == "S01":
        if stage == 0:
            c.line((110,130),(370,130),width=2.5)
            c.dot((110,130)); c.dot((370,130))
            c.poly([(356,123),(370,130),(356,137)])
        else:
            rows = [130] if stage == 1 else [64,130,196]
            for y in rows:
                c.line((40,y),(440,y),width=2.1)
                for x in range(40,441,80):
                    c.dot((x,y))
            if stage == 2:
                for x in (120,280,440):
                    c.line((x,44),(x,216),"second",width=1.5)
    elif family == "S02":
        if stage == 1:
            for r in range(5):
                c.line((45+r*22,36+r*43),(345+r*22,36+r*43))
            for col in range(7):
                c.line((45+col*50,36),(133+col*50,208))
            for col,row in ((2,1),(3,2),(4,3)):
                c.dot((45+col*50+row*22,36+row*43))
        else:
            for r in range(4):
                for col in range(6):
                    x,y=60+col*60,40+r*45
                    if stage == 2 and (r+col) % 2 == 0:
                        c.region([(x,y),(x+60,y),(x+60,y+45),(x,y+45)])
            for x in range(60,421,60):
                c.line((x,40),(x,220))
            for y in range(40,221,45):
                c.line((60,y),(420,y))
            if stage == 0:
                for x,y in ((180,85),(240,130),(300,175)):
                    c.dot((x,y))
    elif family == "S03":
        grid(c)
        if stage == 0:
            c.poly([(140,190),(140,70),(340,70)],width=2.8)
            c.dot((140,70))
        elif stage == 1:
            draw_key(c)
        else:
            draw_facing(c)
    elif family == "S04":
        draw_drop(c, stage)
    elif family == "S05":
        if stage == 0:
            points=regular(240,140,90,3)
            c.poly(points,closed=True,width=2.5)
            for i in range(3):
                a,b=points[i],points[(i+1)%3]
                c.dot(((a[0]+b[0])/2,(a[1]+b[1])/2))
        elif stage == 1:
            points=regular(240,130,100,6,0)
            c.poly(points,closed=True)
            for p in points:
                c.line((240,130),p,"second")
            c.dot((240,130))
        else:
            radius=43
            for col in range(6):
                for row in range(3):
                    x=65+col*radius*1.5
                    y=44+row*radius*math.sqrt(3)+(col%2)*radius*math.sqrt(3)/2
                    if y<=218:
                        vertices=regular(x,y,radius,6,0)
                        for p in vertices:c.line((x,y),p,"scaffold")
                        c.poly(vertices,closed=True)
                        if (col,row) in ((2,1),(3,0)):
                            c.poly([(x,y),*vertices[:3]],"second",closed=True)
    elif family == "S06":
        draw_octagons(c,stage)
    elif family == "S07":
        points=regular(240,130,102,6,0)
        if stage == 0:
            for i in (0,2,4):
                c.line((240,130),points[i])
            c.dot((240,130))
        else:
            for p in points:
                c.line((240,130),p)
            c.poly(points,closed=True,width=2.3)
            if stage == 2:
                for radius in (36,68):
                    c.poly(regular(240,130,radius,6,0),"second",closed=True)
            c.dot((240,130))
    elif family == "S08":
        draw_weave(c,stage)
    elif family == "S09":
        levels = [92] if stage == 0 else [92,64] if stage == 1 else [104,76,48]
        for i,r in enumerate(levels):
            c.poly([(240-r*1.8,130-r),(240+r*1.8,130-r),
                    (240+r*1.8,130+r),(240-r*1.8,130+r)],
                   "ink" if i%2==0 else "second",closed=True)
        if stage == 2:
            for a,b in [((240,26),(240,54)),((240,206),(240,234)),
                        ((52.8,130),(103.2,130)),((376.8,130),(427.2,130)),
                        ((103.2,82),(153.6,82)),((326.4,178),(376.8,178))]:
                c.line(a,b,"second")
    elif family == "S10":
        draw_counterchange(c,stage)


PATTERNS = {
    "key": draw_key, "facing": draw_facing, "glide": draw_glide,
    "drop": draw_drop, "octagons": draw_octagons,
    "counterchange": draw_counterchange, "weave": draw_weave,
    "selections": draw_selections, **EXTRA_PATTERNS,
}


def diagram(content, title, width=480, height=260):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" '
            f'role="img" aria-label="{escape(title,quote=True)}"><title>{escape(title)}</title>'
            f'<style>{SVG_STYLE}</style>{content}</svg>')


def pattern_content(pattern):
    c=Canvas(); PATTERNS[pattern](c); return c.content()


def source_link(source):
    if source["url"]:
        return f'<a href="{escape(source["url"],quote=True)}">{escape(source["label"])}</a>'
    return escape(source["label"])


CSS = """:root{--paper:#f3f0e7;--card:#fffdf7;--ink:#203e42;--muted:#66756a;--line:#d9dfd1;--rust:#aa603f}
*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.6 'Segoe UI',Arial,sans-serif}
a{color:inherit;text-underline-offset:3px}a:hover{color:var(--rust)}button,input,select,textarea{font:inherit}
header,main,footer{max-width:1320px;margin:auto;padding:0 32px}header{padding-top:36px}.eyebrow{font-size:12px;letter-spacing:.15em;text-transform:uppercase;color:var(--muted);font-weight:700}
h1,h2,h3,p{margin-top:0}h1{font:normal clamp(38px,5vw,64px)/1.12 Georgia,serif;margin:12px 0 20px;letter-spacing:-.025em}h2{font:normal 30px/1.25 Georgia,serif}h3{font-size:19px;line-height:1.35}.lede{max-width:880px;font-size:19px;line-height:1.6;color:#53665f}
nav{display:flex;gap:8px;flex-wrap:wrap;margin:24px 0 22px;border-bottom:1px solid var(--line);padding-bottom:17px}nav a{text-decoration:none;padding:7px 13px;border-radius:5px;font-size:14px}nav a[aria-current=page]{background:var(--ink);color:var(--card)}
.toolbar{display:flex;align-items:center;gap:10px;flex-wrap:wrap;padding:14px 0 20px}button,.button{padding:8px 13px;border:1px solid #bccabd;border-radius:5px;background:var(--card);cursor:pointer;color:var(--ink);text-decoration:none;display:inline-block;font-size:14px}button[aria-pressed=true]{background:#dce9df;border-color:#8cab9b}
input[type=search],select,textarea{background:var(--card);border:1px solid #bccabd;border-radius:5px;padding:8px 12px;color:var(--ink)}input[type=search]{min-width:260px}.toolbar .count{margin-left:auto;color:var(--muted);font-size:13px}
.legend{display:flex;gap:20px;flex-wrap:wrap;font-size:13px;color:var(--muted);margin:0 0 28px}.swatch{display:inline-block;width:24px;height:2px;background:var(--ink);vertical-align:middle;margin-right:7px}.swatch.second{background:var(--rust)}.swatch.guide{height:0;border-top:1px dashed #91a894;background:transparent}.swatch.region{height:12px;background:#e0eadd;border:1px solid #c5d5c0}
.family,.goal,.panel{border:1px solid var(--line);background:var(--card);border-radius:10px;margin-bottom:24px;overflow:hidden}.card-heading{padding:23px 25px 0}.id{font:700 12px 'Segoe UI',sans-serif;letter-spacing:.1em;color:var(--rust);margin-right:8px}.family .concept{max-width:900px;margin-bottom:20px;color:#53665f}.stages{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:1px;background:var(--line);border-block:1px solid var(--line)}.stage{padding:16px 12px 20px;background:var(--card);min-width:0}.stage h3{font-size:14px;margin:0 8px 8px}.stage p{font-size:13px;color:var(--muted);line-height:1.55;margin:10px 8px 0}.stage svg{display:block;width:100%;height:auto}.footnote{padding:17px 25px;font-size:14px;color:#53665f}.footnote p:last-child{margin-bottom:0}.source{font-size:12px;color:var(--muted)}
.goals{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:24px;align-items:start}.goal{margin:0;scroll-margin-top:20px}.goal .drawing{padding:0 15px}.goal svg{display:block;width:100%;height:auto}.goal .why{padding:0 25px;color:#53665f;font-size:14px;min-height:45px}.tags{display:flex;gap:7px;flex-wrap:wrap;padding:0 25px 16px}.tags a{font-size:12px;background:#edf1e6;border-radius:4px;padding:3px 8px;text-decoration:none}
details{padding:0 25px 20px;font-size:14px}summary{cursor:pointer;font-weight:600}details ul{padding-left:20px;margin:12px 0}details p{margin:12px 0}.actions{display:flex;gap:12px;align-items:center;flex-wrap:wrap;margin-top:16px}.actions a{font-size:13px}.goal .source{padding:0 25px 20px}
.guide{display:none}.show-guides .guide{display:initial}.mono svg .second{stroke:var(--ink)}.mono svg .field{fill:#e0eadd}.feature-grid{display:grid;grid-template-columns:1fr 1fr;gap:24px;margin:26px 0}.feature{padding:26px}.feature p{color:#53665f;max-width:500px}.feature .mini{width:100%;height:180px;object-fit:contain}.jump-list{display:flex;gap:8px;flex-wrap:wrap;margin:0 0 25px}.jump-list a{font-size:12px;text-decoration:none;border:1px solid var(--line);border-radius:4px;padding:5px 9px}
.sheet{display:block;width:100%;height:auto;border:1px solid var(--line);background:var(--card);border-radius:10px}.note{font-size:14px;color:var(--muted);max-width:1000px;margin:24px 0}.control-grid{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}.control{padding:20px}.control svg{width:100%;height:120px}.control h3{font-size:16px}.control p{font-size:13px;color:var(--muted)}
.brief{padding:28px}.brief-grid{display:grid;grid-template-columns:1fr 1fr;gap:28px}.brief svg{width:100%;height:auto}.brief label{display:block;margin:16px 0 8px;font-weight:600}.brief textarea{display:block;width:100%;min-height:110px;resize:vertical}.brief pre{font:14px/1.6 'Segoe UI',sans-serif;white-space:pre-wrap}.brief .note{margin:14px 0}.brief hr{border:0;border-top:1px solid var(--line);margin:24px 0}footer{margin-top:36px;padding-bottom:32px;color:var(--muted);font-size:12px;border-top:1px solid var(--line);padding-top:20px}
[hidden]{display:none!important}#empty{padding:30px;color:var(--muted)}.sr-only{position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0}
@media(max-width:850px){header,main,footer{padding-inline:18px}.stages{grid-template-columns:1fr}.stage{padding:15px 20px}.stage svg{max-height:220px}.goals,.feature-grid,.brief-grid{grid-template-columns:1fr}.control-grid{grid-template-columns:1fr}.goal .why{min-height:0}.toolbar .count{margin-left:0}h1{font-size:40px}}
@media print{body{background:white;font-size:12px}header,main,footer{max-width:none;padding-inline:0}nav,.toolbar,.actions,.jump-list,.no-print{display:none!important}.family,.goal,.panel{break-inside:avoid;border-radius:0}.goals{display:block}.goal{margin-bottom:20px}.stages{grid-template-columns:repeat(3,1fr)}.brief-grid{grid-template-columns:1fr 1fr}.guide{display:none}h1{font-size:32px}details{display:block}footer{margin-top:20px}.brief textarea{border:0;padding:0}}
"""


COMMON_JS = """
document.querySelectorAll('[data-toggle]').forEach(button=>button.addEventListener('click',()=>{
 const active=document.body.classList.toggle(button.dataset.toggle);button.setAttribute('aria-pressed',String(active));
}));
const search=document.querySelector('#search'),family=document.querySelector('#family-filter');
function filterCards(){const q=(search?.value||'').trim().toLowerCase(),f=family?.value||'';let visible=0;
 document.querySelectorAll('[data-search]').forEach(card=>{const show=(!q||card.dataset.search.includes(q))&&(!f||card.dataset.families.split(' ').includes(f));card.hidden=!show;if(show)visible++;});
 const count=document.querySelector('#visible-count');if(count)count.textContent=visible+' shown';
 const empty=document.querySelector('#empty');if(empty)empty.hidden=visible!==0;
}
search?.addEventListener('input',filterCards);family?.addEventListener('change',filterCards);
"""


def shell(title, lede, body, active="index", script=""):
    pages=[("index","Start here"),("glossary","Skeleton glossary"),("goals","Goal catalogue"),("brief","Experiment brief")]
    nav="".join(f'<a href="{page}.html"'+(' aria-current="page"' if page==active else '')+f'>{label}</a>' for page,label in pages)
    return (f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<title>{escape(title)} · Gojo skeleton atlas</title><style>{CSS}</style></head><body>'
            f'<header><div class="eyebrow">Gojo · structural design references · edition 1</div><h1>{escape(title)}</h1>'
            f'<p class="lede">{lede}</p><nav aria-label="Atlas">{nav}</nav></header><main>{body}</main>'
            '<footer>Authored reference diagrams, not outputs or proofs from a live Gojo machine. '
            'Illustrated angles and spacing are representative; targets state which relations must survive variation. '
            '<a href="../../../../Gojo/docs/GojoHistoricalOrnamentRules.md">Historical research and capability boundaries</a>.</footer>'
            f'<script>{COMMON_JS}{script}</script></body></html>')


def toolbar(search_label, families=None):
    options=''
    if families:
        options='<label class="sr-only" for="family-filter">Skeleton family</label><select id="family-filter"><option value="">All skeleton families</option>'+''.join(f'<option value="{f["id"]}">{f["id"]} · {escape(f["name"])}</option>' for f in families)+'</select>'
    return ('<div class="toolbar"><label class="sr-only" for="search">'+search_label+'</label>'
            '<input type="search" id="search" placeholder="Find a name or ID…">'+options+
            '<button data-toggle="show-guides" aria-pressed="false">Construction guides</button>'
            '<button data-toggle="mono" aria-pressed="false">One ink</button><span class="count" id="visible-count"></span></div>'
            '<div class="legend"><span><i class="swatch"></i>selected path / boundary</span><span><i class="swatch second"></i>second thread or role</span>'
            '<span><i class="swatch guide"></i>optional construction guide</span><span><i class="swatch region"></i>region membership</span>'
            '<span>Selected paths meet at unbroken contacts; ● highlights a site; gaps mark separate crossings</span></div>')


def build_glossary(data):
    cards=[]
    for family in data["families"]:
        ident=family["id"]
        stages=[]
        for i,stage in enumerate(family["stages"]):
            c=Canvas();glossary(c,ident,i)
            sid=f'{ident}.{chr(65+i)}'
            svg=diagram(c.content(),f'{sid} {stage["label"]}')
            (OUT/"plates"/f"{sid}.svg").write_text(svg,encoding="utf-8")
            stages.append(f'<div class="stage" id="{sid}"><h3><a class="id" href="plates/{sid}.svg">{sid}</a>{escape(stage["label"])}</h3>{svg}<p>{escape(stage["caption"])}</p></div>')
        haystack=escape(f'{ident} {family["name"]} {family["idea"]}'.lower(),quote=True)
        cards.append(f'<section class="family" id="{ident}" data-search="{haystack}" data-families="{ident}">'
                     f'<div class="card-heading"><h2><span class="id">{ident}</span>{escape(family["name"])}</h2><p class="concept">{escape(family["idea"])}</p></div>'
                     '<div class="stages">'+''.join(stages)+'</div>'
                     f'<div class="footnote"><p><strong>Keep readable:</strong> {escape(family["invariant"])}</p>'
                     f'<p class="source">{source_link(family["source"])} · <a href="goals.html?family={ident}">Goals using {ident}</a></p></div></section>')
    body=toolbar("Find a skeleton")+'<div class="jump-list">'+''.join(f'<a href="#{f["id"]}">{f["id"]} {escape(f["name"])}</a>' for f in data["families"])+'</div>'+''.join(cards)+'<p id="empty" hidden>No matching skeletons.</p>'
    return shell("Skeleton glossary","Ten construction families, each read from a simple concept through two structural elaborations. Follow the relations; leave decoration aside.",body,"glossary",'filterCards();')


def brief_text(goal):
    return (f'TARGET: {goal["id"]} — {goal["name"]}\n'
            f'CHARACTER: {goal["why"]}\n\nREQUIRED\n'+''.join(f'- {x}\n' for x in goal["must"])+
            '\nALLOWED VARIATION\n'+''.join(f'- {x}\n' for x in goal["flexible"])+
            f'\nMISS TO WATCH FOR\n- {goal["miss"]}\n\n'
            'Before running: choose observation scope, seam/crossing policy, and any measured interpretation.\n'
            'After running: record each requirement as met / missed / unresolved, with evidence. '
            'Record visual character separately; correctness alone does not establish an interesting result.')


def build_goals(data):
    cards=[]
    for goal in data["goals"]:
        ident=goal["id"]
        svg=diagram(pattern_content(goal["pattern"]),f'{ident} {goal["name"]}')
        (OUT/"plates"/f"{ident}.svg").write_text(svg,encoding="utf-8")
        haystack=escape(f'{ident} {goal["name"]} {goal["why"]} {" ".join(goal["families"])}'.lower(),quote=True)
        tags=''.join(f'<a href="glossary.html#{sid}">{sid}</a>' for sid in goal["families"])
        requirements=''.join(f'<li>{escape(item)}</li>' for item in goal["must"])
        flexible=''.join(f'<li>{escape(item)}</li>' for item in goal["flexible"])
        cards.append(f'<article class="goal" id="{ident}" data-search="{haystack}" data-families="{" ".join(goal["families"])}">'
                     f'<div class="card-heading"><h2><span class="id">{ident}</span>{escape(goal["name"])}</h2></div><div class="drawing">{svg}</div>'
                     f'<p class="why">{escape(goal["why"])}</p><div class="tags">{tags}</div>'
                     f'<details><summary>Target and allowed variation</summary><p><strong>Required</strong></p><ul>{requirements}</ul>'
                     f'<p><strong>Can vary</strong></p><ul>{flexible}</ul><p><strong>A miss:</strong> {escape(goal["miss"])}</p>'
                     f'<div class="actions"><a class="button" href="brief.html?goal={ident}">Use this target</a><a href="plates/{ident}.svg">SVG plate</a></div></details>'
                     f'<div class="source">{source_link(goal["source"])}</div></article>')
    body=toolbar("Find a goal",data["families"])+('<p class="note">These are candidate design targets, not a ranking of beauty. '
          'Look for the named visual character as well as valid construction. Open a card to see requirements and permitted variation.</p>')+'<div class="goals">'+''.join(cards)+'</div><p id="empty" hidden>No matching goals.</p>'
    script="const initialFamily=new URLSearchParams(location.search).get('family');if(initialFamily&&family)family.value=initialFamily;filterCards();"
    return shell("Goal catalogue","Twelve named structural targets: enough variety to distinguish interesting design from an empty, crowded or merely correct repeat.",body,"goals",script)


def build_sheet(data, mode):
    c=Canvas()
    if mode=="goals":
        width,height=1260,1520
        c.raw(f'<rect width="{width}" height="{height}" fill="{PAPER}"/>')
        c.raw('<text x="32" y="49" style="font:32px Georgia,serif;fill:#203e42">Goal skeletons</text>')
        c.text((32,78),"G01–G12 · authored structural targets · spacing may vary")
        for i,goal in enumerate(data["goals"]):
            x=22+(i%3)*410;y=107+(i//3)*347
            c.raw(f'<rect x="{x}" y="{y}" width="398" height="330" rx="8" fill="none" stroke="#d9dfd1"/>')
            c.raw(f'<text x="{x+16}" y="{y+30}" style="font:600 16px Segoe UI,sans-serif;fill:{INK}">{goal["id"]} · {escape(goal["name"])}</text>')
            c.raw(f'<g transform="translate({x+10} {y+49}) scale(.785)">{pattern_content(goal["pattern"])}</g>')
            for line,text in enumerate(textwrap.wrap(goal["why"],48)[:3]):
                c.text((x+16,y+264+line*17),text)
    else:
        width,height=1260,2640
        c.raw(f'<rect width="{width}" height="{height}" fill="{PAPER}"/>')
        c.raw('<text x="32" y="49" style="font:32px Georgia,serif;fill:#203e42">Skeleton glossary</text>')
        c.text((32,78),"S01–S10 · concept → recurrence → structural elaboration")
        for i,family in enumerate(data["families"]):
            y=105+i*250
            c.raw(f'<text x="32" y="{y+20}" style="font:600 17px Segoe UI,sans-serif;fill:{INK}">{family["id"]} · {escape(family["name"])}</text>')
            for stage in range(3):
                x=26+stage*410
                drawing=Canvas();glossary(drawing,family["id"],stage)
                c.raw(f'<g transform="translate({x} {y+27}) scale(.8)">{drawing.content()}</g>')
                c.text((x+8,y+238),f'{family["id"]}.{chr(65+stage)} · {family["stages"][stage]["label"]}')
    return diagram(c.content(),f'{mode} reference sheet',width,height)


def build_index(data):
    controls=[]
    for ident,name,note in [
        ("C01","Vacant field","Maximal apparent symmetry; no useful structure to recognize."),
        ("C02","Isolated bars","A valid repeated arrangement, but no connected cross-cell passage."),
        ("C03","Repeated overdraw","More darkening at one site; no new coverage or structural variety.")]:
        c=Canvas()
        if ident=="C01":
            c.poly([(70,35),(410,35),(410,225),(70,225)],"frame",closed=True)
        elif ident=="C02":
            for x in range(60,421,60):c.line((x,70),(x,190),width=3)
        else:
            for i in range(6):c.poly([(180,65),(300,65),(300,195),(180,195)],closed=True,width=2+i*1.8)
        controls.append(f'<div class="panel control"><h3><span class="id">{ident}</span>{name}</h3>{diagram(c.content(),name)}<p>{note}</p></div>')
    body=('<div class="feature-grid"><div class="panel feature"><div class="eyebrow">01 · learn the vocabulary</div>'
          '<h2>Simple forms, then elaborations</h2><p>Ten families and thirty labelled diagrams. Compare an ordered span with a fret, a lattice with its selected paths, or an aligned field with a half-drop.</p>'
          f'{diagram(pattern_content("key"),"Continuous key example")}<a class="button" href="glossary.html">Open the glossary</a></div>'
          '<div class="panel feature"><div class="eyebrow">02 · choose a destination</div><h2>Named goals, flexible dimensions</h2>'
          '<p>Twelve targets combine the families into courts, keys, passages and shared fields. Each states what matters, what can vary, and how a result can miss.</p>'
          f'{diagram(pattern_content("nested_courts"),"Nested diamond courts example")}<a class="button" href="goals.html">Browse the goals</a></div></div>'
          '<h2>Goal contact sheet</h2><p class="note">Use IDs in conversation: “G05, but wider courts”, “combine S04 with S08”, or “preserve G01 continuity while adding G08 field roles”.</p>'
          '<a href="goals.html"><img class="sheet" src="goal-sheet.svg" alt="Twelve named skeleton goal patterns, G01 through G12"></a>'
          '<p class="note"><a href="goal-sheet.svg">Goal sheet SVG</a> · <a href="glossary-sheet.svg">Glossary sheet SVG</a> · <a href="brief.html">Prepare an experiment brief</a></p>'
          '<h2>Useful plain controls</h2><p class="note">These can satisfy selected mathematical properties while missing a particular design target. They help expose weak scoring rules; simplicity itself is not a defect.</p>'
          '<div class="control-grid">'+''.join(controls)+'</div>')
    return shell("Skeleton atlas","A visual vocabulary for construction before adornment. Start with basic relations, explore their elaborations, then choose explicit targets for Gojo experiments.",body)


def build_brief(data):
    safe_data=json.dumps(data["goals"],ensure_ascii=True).replace("<","\\u003c")
    body=('<section class="panel brief"><div class="brief-grid"><div><label for="goal">Reference goal</label><select id="goal">'+
          ''.join(f'<option value="{g["id"]}">{g["id"]} · {escape(g["name"])}</option>' for g in data["goals"])+
          '</select><div id="goal-drawing"></div><p id="character"></p><a id="goal-link" href="goals.html">Open goal card</a></div>'
          '<div><h2>Set the expectation</h2><pre id="contract"></pre></div></div><hr>'
          '<label for="choices">Experiment choices</label><textarea id="choices" placeholder="Scope, allowed edits, seam and crossing policy, optional measured interpretation…"></textarea>'
          '<label for="observations">Result and evidence</label><textarea id="observations" placeholder="For each requirement: met / missed / unresolved, with evidence. Then describe the visual character separately."></textarea>'
          '<p class="note">Inputs stay in this page while it is open. Copy the brief to retain it.</p>'
          '<div class="actions"><button id="copy">Copy brief</button><button id="print">Print / save PDF</button><span id="copy-status" role="status"></span></div></section>')
    drawings={g["id"]:diagram(pattern_content(g["pattern"]),g["name"]) for g in data["goals"]}
    briefs={g["id"]:brief_text(g) for g in data["goals"]}
    script=f'const goals={safe_data},drawings={json.dumps(drawings)},briefs={json.dumps(briefs)};'+"""
const choice=document.querySelector('#goal');const requested=new URLSearchParams(location.search).get('goal');
if(goals.some(g=>g.id===requested))choice.value=requested;
function setGoal(){const g=goals.find(g=>g.id===choice.value);document.querySelector('#goal-drawing').innerHTML=drawings[g.id];
 document.querySelector('#character').textContent=g.why;document.querySelector('#contract').textContent=briefs[g.id];
 document.querySelector('#goal-link').href='goals.html#'+g.id;document.querySelector('#copy-status').textContent='';
 const url=new URL(location.href);url.searchParams.set('goal',g.id);history.replaceState(null,'',url);}
choice.addEventListener('change',setGoal);setGoal();
document.querySelector('#copy').addEventListener('click',async()=>{
 const text=briefs[choice.value]+'\\n\\nEXPERIMENT CHOICES\\n'+document.querySelector('#choices').value+'\\n\\nRESULT AND EVIDENCE\\n'+document.querySelector('#observations').value;
 try{await navigator.clipboard.writeText(text);document.querySelector('#copy-status').textContent='Copied.';}
 catch{const box=document.createElement('textarea');box.value=text;document.body.append(box);box.select();const ok=document.execCommand('copy');box.remove();document.querySelector('#copy-status').textContent=ok?'Copied.':'Select and copy the visible brief.';}
});document.querySelector('#print').addEventListener('click',()=>window.print());
"""
    return shell("Experiment brief","Choose a reference, declare the permitted changes, and record structural success separately from visual interest.",body,"brief",script)


def main():
    data=json.loads((HERE/"atlas.json").read_text(encoding="utf-8"))
    (OUT/"plates").mkdir(parents=True,exist_ok=True)
    for name,builder in [("index",build_index),("glossary",build_glossary),("goals",build_goals),("brief",build_brief)]:
        (OUT/f"{name}.html").write_text(builder(data),encoding="utf-8")
    for mode in ("goals","glossary"):
        name="goal" if mode=="goals" else "glossary"
        (OUT/f"{name}-sheet.svg").write_text(build_sheet(data,mode),encoding="utf-8")
    # A stable alias matches the other experiment galleries.
    (OUT/"gallery.html").write_text((OUT/"index.html").read_text(encoding="utf-8"),encoding="utf-8")
    print(f'Built {len(data["families"])} families, {sum(len(f["stages"]) for f in data["families"])} glossary diagrams and {len(data["goals"])} goal plates in {OUT}')


if __name__=="__main__":
    main()
