"""Draw the captured centre-line corpus; this adapter discovers no geometry or symmetry.

Usage: python CentrelinePatternPage.py REPORT OUTPUT
Native calculations live in CentrelinePatternStudy. Pixel scaling, line width,
color and the gallery's selection controls are presentation only.
"""
from __future__ import annotations

import argparse
import html
import json
import re
from pathlib import Path
import xml.etree.ElementTree as ET

import markdown

COLORS = ["#247b79", "#bb603f", "#70548a", "#486fa0", "#937a24", "#a54d76"]
PAPER = "#fcfaf4"


def esc(value):
    return html.escape(str(value), quote=True)


def words(value):
    return re.sub(r"([a-z])([A-Z])", r"\1 \2", str(value)).replace("_", " ").capitalize()


def fmt(value):
    if value is None:
        return "unresolved"
    if isinstance(value, bool):
        return "yes" if value else "no"
    if isinstance(value, list):
        return " · ".join(fmt(item) for item in value) or "none"
    if isinstance(value, dict):
        return "; ".join(f"{words(k)}: {fmt(v)}" for k, v in value.items())
    return str(value)


def number(value):
    if isinstance(value, str) and "/" in value:
        a, b = value.split("/")
        return float(a) / float(b)
    return float(value)


def point(value):
    if isinstance(value, dict):
        return number(value["x"]), number(value["y"])
    return number(value[0]), number(value[1])


def path_color(path, index):
    return "#9c9a8c" if "rail" in path.get("role", "").lower() else COLORS[index % len(COLORS)]


def node(parent, tag, text=None, **attrs):
    result = ET.SubElement(parent, tag, {k.replace("_", "-"): str(v) for k, v in attrs.items()})
    result.text = text
    return result


def picture(pattern, key, width=1100, height=270, selected=None, interactive=False):
    paths = pattern["paths"]
    points = [point(p) for path in paths for p in path["points"]]
    if not points:
        points = [(0, 0), (1, 1)]
    xs, ys = zip(*points)
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    margin = 28 if width > 600 else 18
    scale = min((width - margin * 2) / max(1, x1 - x0), (height - margin * 2) / max(1, y1 - y0))
    ox, oy = (width - (x1 - x0) * scale) / 2, (height - (y1 - y0) * scale) / 2
    project = lambda p: (ox + (p[0] - x0) * scale, oy + (y1 - p[1]) * scale)
    result = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": f"0 0 {width} {height}",
        "role": "img", "aria-labelledby": key + "-title", "class": "pattern-svg"})
    node(result, "title", pattern["name"], id=key + "-title")
    node(result, "rect", x=0, y=0, width=width, height=height, fill=PAPER)
    for index, path in enumerate(paths):
        active = selected is None or path["id"] in selected
        points_here = [project(point(p)) for p in path["points"]]
        if not points_here:
            continue
        group = node(result, "g", data_path=str(path["id"]), class_="path-group" if active else "path-group dimmed")
        group.attrib["class"] = group.attrib.pop("class-")
        color = path_color(path, index)
        node(group, "title", f"Path {path['id']}: {path.get('role', '')}")
        d = "M " + " L ".join(f"{x:g} {y:g}" for x, y in points_here)
        # Never append Z: every drawn handoff is already in the recorded route.
        if len(points_here) == 1:
            line = node(group, "circle", cx=points_here[0][0], cy=points_here[0][1], r=2.4,
                fill=color, stroke=color, stroke_width=1, class_="ink-line")
        else:
            line = node(group, "path", d=d, fill="none", stroke=color, stroke_width=2.8 if width > 600 else 2,
                stroke_linecap="square", stroke_linejoin="miter", class_="ink-line")
        line.attrib["class"] = line.attrib.pop("class-")
        if interactive:
            attrs = {"data_pick_path": str(path["id"]), "tabindex": "0", "role": "button", "aria_label": f"Isolate path {path['id']}: {path.get('role', '')}", "class_": "path-pick"}
            if len(points_here) == 1:
                pick = node(group, "circle", cx=points_here[0][0], cy=points_here[0][1], r=9, fill="transparent", **attrs)
            else:
                pick = node(group, "path", d=d, fill="none", stroke="transparent", stroke_width=15, **attrs)
            pick.attrib["class"] = pick.attrib.pop("class-")
        for a, b in zip(points_here, points_here[1:]):
            dx, dy = b[0] - a[0], b[1] - a[1]
            length = (dx * dx + dy * dy) ** .5
            if length < 18:
                continue
            ux, uy = dx / length, dy / length
            cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
            size = 3.8
            pts = [(cx + ux * size, cy + uy * size),
                (cx - ux * size - uy * size * .7, cy - uy * size + ux * size * .7),
                (cx - ux * size + uy * size * .7, cy - uy * size - ux * size * .7)]
            arrow = node(group, "polygon", points=" ".join(f"{x:g},{y:g}" for x, y in pts), fill=color, class_="direction-arrow")
            arrow.attrib["class"] = arrow.attrib.pop("class-")
    return ET.tostring(result, encoding="unicode")


def table(headers, rows, extra=""):
    return '<div class="table-scroll"><table class="' + extra + '"><thead><tr>' + ''.join('<th>' + esc(h) + '</th>' for h in headers) + '</tr></thead><tbody>' + ''.join('<tr>' + ''.join('<td>' + str(c) + '</td>' for c in row) + '</tr>' for row in rows) + '</tbody></table></div>'


def pair_table(pattern):
    rows = []
    for read in pattern.get("pairReads", []):
        rows.append([f'{read["first"]} → {read["second"]}', esc(read.get("transform", "")),
            f'{read.get("matched", 0)} / {read.get("checked", 0)}',
            '<span class="result ' + ('good' if read.get("exact") else 'open') + '">' + ('exact' if read.get("exact") else 'does not match exactly') + '</span>',
            esc(fmt(read.get("translation"))), esc(read.get("note", ""))])
    if not rows:
        return '<p class="caption">No pair correspondence is asserted for this construction.</p>'
    return table(["Paths", "Selected read", "Matched / checked", "Result", "Translation", "Qualification"], rows)


def pattern_panel(pattern, note, variant=False):
    pid = pattern["id"]
    paths = pattern["paths"]
    groups = pattern.get("groups", [])
    group_buttons = '<button type="button" class="group-button" data-paths="*" aria-pressed="true">All paths</button>'
    for group in groups:
        if group.get("id") == "all":
            continue
        group_buttons += f'<button type="button" class="group-button" data-group="{esc(group["id"])}" data-paths="{esc(json.dumps(group["pathIds"]))}" aria-pressed="false">{esc(group["name"])}</button>'
    legend = ''.join(f'<button type="button" data-pick-path="{p["id"]}" class="path-key"><i style="--swatch:{path_color(p, i)}"></i><span>{p["id"]} · {esc(p.get("role", "path"))}</span></button>' for i, p in enumerate(paths))
    metric_rows = [[esc(words(k)), esc(fmt(v))] for k, v in pattern.get("metrics", {}).items()]
    alternatives = ''.join('<li>' + esc(text) + '</li>' for text in note.get("alternatives", []))
    families = ''.join('<span class="family-tag">' + esc(text) + '</span>' for text in note.get("candidateFamilies", []))
    related = ' · '.join(f'<a href="#inspect" data-select-pattern="{esc(item)}">{esc(item)}</a>' for item in note.get("related", []))
    word_rows = [[str(p["id"]), esc(p.get("role", "")), '<code>' + esc(p.get("stepWord", "")) + '</code>',
        '<code>' + esc(p.get("turnWord", "")) + '</code>', esc(fmt(p.get("closed")))] for p in paths]
    pair_views = ''
    if pid == "CR4":
        for group in groups:
            if group.get("id") in ["outer", "inner"]:
                pair_views += '<div><h4>' + esc(group["name"]) + '</h4>' + picture(pattern, pid + '-' + group['id'], 540, 180, group['pathIds']) + '</div>'
        pair_views = '<div class="pair-views">' + pair_views + '</div>' if pair_views else ''
    limits = note.get("limits", pattern.get("scope", ""))
    source_link = f'<a href="#source-{pid[0]}" data-open-source="{pid[0]}">View reference sheet {pid[0]}</a>' if not variant else ''
    return f'''<article class="pattern-panel{' variant-panel' if variant else ''}" data-pattern="{esc(pid)}" id="pattern-{esc(pid)}">
<div class="panel-heading"><div><p class="eyebrow">{esc(pattern.get('source', 'Controlled construction'))} · {esc(pid)}</p><h3>{esc(note.get('name', pattern['name']))}</h3></div><span class="status">Authored interpretation</span></div>
<p class="observed">{esc(note.get('observed', pattern.get('scope', '')))}</p><p class="caption">{source_link}</p>
<div class="group-controls js-only" aria-label="Select a component group">{group_buttons}</div>
<div class="diagram">{picture(pattern, 'inspect-' + pid, interactive=True)}</div>
<p class="selection-caption caption" aria-live="polite">All recorded paths. Select a group or a colored line to isolate its contribution.</p>
<div class="path-legend js-only">{legend}</div>{pair_views}
<div class="explanation-grid"><div><h4>One way to construct it</h4><p>{esc(note.get('construction', pattern.get('scope', '')))}</p><h4>Other possible engines</h4><ul>{alternatives}</ul><div class="families">{families}</div></div><div><h4>What the diagram leaves open</h4><p>{esc(limits)}</p><p class="caption">{esc(pattern.get('scope', ''))}</p>{'<p class="related">Compare: ' + related + '</p>' if related else ''}</div></div>
<details><summary>Calculated reads and path words</summary><p class="caption">These counts and words are read from the authored native construction. They do not measure the source photograph or prove its original construction. Straight-step counts retain the chosen segmentation.</p><div class="read-grid"><div>{table(['Read', 'Result'], metric_rows)}</div><div><h4>Pair correspondences</h4>{pair_table(pattern)}</div></div><h4>Ordered path words</h4><p class="caption">Step words use the common reach of this realization. The arrows show traversal in its chosen frame; they do not infer physical flow from the reference image.</p>{table(['Path', 'Role', 'Step word', 'Turn word', 'Closed by identity'], word_rows)}</details>
</article>'''


def overview(patterns, notes):
    width, row_height, columns = 1200, 210, 2
    rows = (len(patterns) + columns - 1) // columns
    root = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": f"0 0 {width} {rows * row_height}", "role": "img"})
    node(root, "title", "Eighteen authored centre-line interpretations")
    node(root, "rect", x=0, y=0, width=width, height=rows * row_height, fill=PAPER)
    for i, pattern in enumerate(patterns):
        x, y = (i % columns) * 600, (i // columns) * row_height
        group = node(root, "g", transform=f"translate({x} {y})")
        node(group, "text", pattern["id"] + " · " + notes.get(pattern["id"], {}).get("name", pattern["name"]), x=22, y=30, font_family="Georgia,serif", font_size=19, fill="#263b3a")
        child = ET.fromstring(picture(pattern, "overview-" + pattern["id"], 600, 160))
        child.set("y", "42")
        child.set("width", "600")
        child.set("height", "160")
        group.append(child)
        node(group, "line", x1=20, y1=205, x2=580, y2=205, stroke="#d9dacd")
    return ET.tostring(root, encoding="unicode")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source = Path(__file__).resolve().parent
    report = json.loads(args.report.read_text(encoding="utf-8-sig"))
    editorial = json.loads((source / "CentrelinePatternNotes.json").read_text(encoding="utf-8-sig"))
    patterns, variants = report["patterns"], report.get("variants", [])
    notes = {p["id"]: p for p in editorial["patterns"]}
    if set(notes) != {p["id"] for p in patterns}:
        raise ValueError("Editorial rows and native corpus must match exactly.")
    out = args.output.resolve()
    out.mkdir(parents=True, exist_ok=True)
    canonical = source.parent / "CentrelinePatternStudy.md"
    md = canonical.read_text(encoding="utf-8")
    (out / "notes.md").write_text(md, encoding="utf-8", newline="\n")
    (out / "study-data.json").write_text(json.dumps(report, separators=(",", ":"), ensure_ascii=False), encoding="utf-8", newline="\n")
    (out / "editorial-notes.json").write_text(json.dumps(editorial, separators=(",", ":"), ensure_ascii=False), encoding="utf-8", newline="\n")
    for ext in ["js", "css"]:
        (out / ("centreline." + ext)).write_text((source / ("CentrelinePatternPage." + ext)).read_text(encoding="utf-8"), encoding="utf-8", newline="\n")
    overview_svg = overview(patterns, notes)
    (out / "CentrelineOverview.svg").write_text(overview_svg, encoding="utf-8", newline="\n")
    (source.parent / "CentrelineOverview.svg").write_text(overview_svg, encoding="utf-8", newline="\n")
    cards = ''
    for p in patterns:
        cards += f'<a class="pattern-card" href="#pattern-{esc(p["id"])}" data-select-pattern="{esc(p["id"])}"><div class="card-heading"><strong>{esc(p["id"])}</strong><span>{esc(notes[p["id"]].get("name", p["name"]))}</span></div>{picture(p, "card-" + p["id"], 540, 140)}<span class="card-source">{esc(p.get("source", ""))}</span></a>'
    options = ''.join(f'<option value="{esc(p["id"])}">{esc(p["id"] + " · " + notes[p["id"]].get("name", p["name"]))}</option>' for p in patterns)
    panels = ''.join(pattern_panel(p, notes[p["id"]]) for p in patterns)
    variant_panels = ''.join(pattern_panel(p, {}, True) for p in variants)
    exclusions = [[esc(p.get("id", "")), esc(p.get("source", "")), esc(p.get("reason", ""))] for p in editorial.get("excluded", [])]
    family_map = {}
    for p in patterns:
        for family in notes[p["id"]].get("candidateFamilies", []):
            family_map.setdefault(family, []).append(p["id"])
    family_rows = [[esc(family), ' · '.join(f'<a href="#inspect" data-select-pattern="{esc(pid)}">{esc(pid)}</a>' for pid in ids)] for family, ids in sorted(family_map.items())]
    source_images = ''.join(f'<details class="source-sheet" id="source-{label}"><summary>Reference sheet {label}</summary><p class="caption">{description} The source is retained as supplied, including its watermarks. The colored diagrams above are separately authored interpretations.</p><img src="source-{label.lower()}.png" alt="Supplied ornament reference sheet {label}" loading="lazy"></details>' for label, description in [('A', 'Six horizontal rows, numbered from the top.'), ('B', 'Eight horizontal rows, numbered from the top. The small gold examples are motif keys, not additional rows.'), ('C', 'Number each column from its top: L1–L8 at left and R1–R6 at right.')])
    html_notes = markdown.markdown(md, extensions=["tables", "fenced_code"])
    page = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><base href="/ornaments/centreline/">
<title>Reading the strands · Gojo ornament studies</title><meta name="description" content="Eighteen centre-line interpretations of angular friezes: paths, mirrored pairs, compound motifs and alternative generating structures."><link rel="stylesheet" href="/ornaments/ornaments.css"><link rel="stylesheet" href="centreline.css"><script src="centreline.js" defer></script></head>
<body class="hide-directions"><a class="skip" href="#main">Skip to the study</a><main id="main"><nav aria-label="Site"><a href="/ornaments/">← Ornament studies</a><a href="/ornaments/weave/">Following a weave</a><a href="/ornaments/signatures/">Pattern signatures</a></nav>
<header><p class="eyebrow">Centre-line study · eighteen angular patterns</p><h1>Reading the strands</h1><p class="intro">A complicated border can become easier to understand when we separate its paths. A pair may reveal a simple mirror, a repeated hook, or a small shape that disappears into the combined design.</p><p class="intro">This study breaks down every wholly straight-edged row in the three supplied sheets. Select a pattern, isolate its components, and compare possible engines that could produce the same visible arrangement.</p></header>
<nav class="subnav" aria-label="Study"><a href="#gallery">All eighteen patterns</a><a href="#inspect">Inspect components</a><a href="#reflection">Change a pairing</a><a href="#engines">Shared engine ideas</a><a href="#sources">Reference sheets</a><a href="#notes">Full notes</a></nav>
<div class="scope"><strong>What these pictures claim.</strong> The centre lines are authored structural interpretations, not automatic image traces. Their chosen lengths and connections make the proposed construction inspectable. Alternative routes, joins, thickness and source history remain open where the image does not settle them.</div>
<section id="gallery"><h2>One source row, one inspectable construction</h2><p class="section-lead">A, B and C identify the supplied sheets. Each row keeps its own entry even when a related construction appears elsewhere. Click any drawing to explore it.</p><div class="gallery">{cards}</div></section>
<section id="inspect"><h2>Separate the components</h2><p class="section-lead">Start with CR4 for the angular weave. Compare the outer pair, inner pair, and combined view. Other rows expose their repeated paths, inserts, or border rails.</p><div class="controls js-only"><label for="pattern-select">Pattern</label><button type="button" id="previous-pattern" aria-label="Previous pattern">←</button><select id="pattern-select">{options}</select><button type="button" id="next-pattern" aria-label="Next pattern">→</button></div><div class="view-controls js-only"><label><input type="checkbox" id="show-context" checked>Keep other paths faintly visible</label><label><input type="checkbox" id="show-directions">Show traversal arrows</label><label><input type="checkbox" id="show-ribbon">Broaden the lines</label></div><p class="caption">Broadening is a display preview. It supplies no over/under order, clearance guarantee, or material join. The <a href="/ornaments/weave/">weave study</a> retains those crossing distinctions.</p><noscript><p class="notice">All eighteen constructions are shown below. Enable JavaScript to isolate paths and move between patterns with the arrow keys.</p></noscript>{panels}</section>
<section id="reflection"><h2>Same recipe, different relationship</h2><p class="section-lead">These controlled constructions distinguish a vertical reflection from a shifted partner, an expanded partner, or reversed traversal. The reported comparisons keep their chosen transformation explicit.</p>{variant_panels or '<p>The source constructions are shown above; no additional controlled variants were captured in this report.</p>'}<div class="notice"><strong>The middle is not forbidden.</strong> A horizontal segment on the reflection axis maps onto itself. Two independent copies would overlap there. A generator may deliberately avoid that overlap, share the segment, or preserve two coincident routes; reflection alone does not choose.</div></section>
<section id="engines"><h2>Related engine ideas across the sheets</h2><p class="section-lead">These are proposed construction families from the visual breakdown. They are a vocabulary for comparing hypotheses, not learned labels or a ranking of the original makers.</p>{table(['Candidate engine family', 'Source rows to compare'], family_rows)}<p>A future ranking can account for a shared recipe, its placements, and its exceptions. A short explanation is useful evidence; it does not establish the original construction history.</p></section>
<section id="sources"><h2>What was included and left out</h2><p>All eighteen wholly straight-edged rows have a centre-line interpretation. Mixed and curved rows remain listed below so a curved carrier or a small floral insert is not silently flattened into a different pattern.</p>{table(['Row', 'Source', 'Reason for leaving the full row out'], exclusions)}{source_images}</section>
<section id="notes" class="notes"><div class="downloads"><a href="notes.md" download>Markdown study</a><a href="study-data.json" download>Recorded native reads</a><a href="editorial-notes.json" download>Visual breakdown</a><a href="CentrelineOverview.svg" download>All-pattern SVG sheet</a></div>{html_notes}</section>
<footer><p>Gojo ornament studies · Connected paths and their selected reads remain the source of the diagrams. The alternative engines are hypotheses to test.</p><p><a href="/ornaments/">All ornament studies</a></p></footer></main></body></html>'''
    (out / "index.html").write_text(page, encoding="utf-8", newline="\n")
    print(json.dumps({"output": str(out), "patterns": len(patterns), "variants": len(variants), "excluded": len(exclusions)}))


if __name__ == "__main__":
    main()
