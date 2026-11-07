"""Present captured native symmetry actions without performing those actions.

The report owns route selection, transforms, correspondence, and validation.
This adapter draws its points and supplies navigation among recorded results.
Usage: python SymmetryActionPage.py REPORT OUTPUT
"""
from __future__ import annotations

import argparse
import html
import json
import re
from pathlib import Path
import xml.etree.ElementTree as ET

import markdown

PAPER = "#fcfaf4"
GREEN = "#247b79"
RUST = "#bb603f"
GOLD = "#a38328"
BLUE = "#486fa0"


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
        return " · ".join(fmt(v) for v in value) or "none"
    if isinstance(value, dict):
        return "; ".join(f"{words(k)}: {fmt(v)}" for k, v in value.items())
    return str(value)


def node(parent, tag, text=None, **attrs):
    result = ET.SubElement(parent, tag, {k.rstrip("_").replace("_", "-"): str(v) for k, v in attrs.items()})
    result.text = text
    return result


def svg(width, height, key, title):
    root = ET.Element("svg", {"xmlns": "http://www.w3.org/2000/svg", "viewBox": f"0 0 {width} {height}",
        "class": "action-svg", "role": "img", "aria-labelledby": key + "-title"})
    node(root, "title", title, id=key + "-title")
    node(root, "rect", x=0, y=0, width=width, height=height, fill=PAPER)
    return root


def project(points, width, height, margin=30):
    points = points or [[0, 0], [1, 1]]
    xs, ys = zip(*points)
    x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
    scale = min((width - 2 * margin) / max(1, x1 - x0), (height - 2 * margin) / max(1, y1 - y0))
    ox, oy = (width - (x1 - x0) * scale) / 2, (height - (y1 - y0) * scale) / 2
    return lambda p: (ox + (p[0] - x0) * scale, oy + (y1 - p[1]) * scale)


def arrow(parent, a, b, color, size=4):
    dx, dy = b[0] - a[0], b[1] - a[1]
    distance = (dx * dx + dy * dy) ** .5
    if distance < size * 4:
        return
    ux, uy = dx / distance, dy / distance
    cx, cy = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
    pts = [(cx + ux * size, cy + uy * size), (cx - ux * size - uy * size * .65, cy - uy * size + ux * size * .65),
        (cx - ux * size + uy * size * .65, cy - uy * size - ux * size * .65)]
    node(parent, "polygon", points=" ".join(f"{x:g},{y:g}" for x, y in pts), fill=color, class_="arrow")


def path(root, points, projection, color, width=3, arrows=True, dashed=False):
    pts = [projection(p) for p in points]
    if not pts:
        return
    if len(pts) == 1:
        node(root, "circle", cx=pts[0][0], cy=pts[0][1], r=3, fill=color)
        return
    attrs = {"d": "M " + " L ".join(f"{x:g} {y:g}" for x, y in pts), "fill": "none", "stroke": color,
        "stroke_width": width, "stroke_linejoin": "miter", "stroke_linecap": "round"}
    if dashed:
        attrs["stroke_dasharray"] = "6 4"
    node(root, "path", **attrs)
    if arrows:
        for a, b in zip(pts, pts[1:]):
            arrow(root, a, b, color)
    node(root, "circle", cx=pts[0][0], cy=pts[0][1], r=4, fill=PAPER, stroke=color, stroke_width=2, class_="route-start")


def export(root):
    return ET.tostring(root, encoding="unicode")


def route_svg(points, key, title, color=GREEN, bounds=None, width=350, height=230):
    root = svg(width, height, key, title)
    projection = project(bounds if bounds is not None else points, width, height)
    path(root, points, projection, color)
    return export(root)


def compare_views(row, key):
    points = row.get("source", []) + row.get("output", []) + row.get("target", [])
    views = [("source", "1 · Selected source", GREEN), ("output", "2 · Native action output", BLUE), ("target", "3 · Existing target", RUST)]
    return '<div class="comparison-views">' + ''.join('<figure><figcaption>' + label + '</figcaption>' + route_svg(row.get(field, []), key + '-' + field, label, color, points) + '</figure>' for field, label, color in views) + '</div>'


def table(headers, rows, css=""):
    return '<div class="table-scroll"><table class="' + css + '"><thead><tr>' + ''.join('<th>' + esc(h) + '</th>' for h in headers) + '</tr></thead><tbody>' + ''.join('<tr>' + ''.join('<td>' + str(cell) + '</td>' for cell in row) + '</tr>' for row in rows) + '</tbody></table></div>'


def full_route(row, key, width=1100, height=230):
    points = row["sourcePath"]
    projection = project(points, width, height, 35)
    root = svg(width, height, key, row["name"] + " — selected sections of the original route")
    path(root, points, projection, "#d2d4c9", width=3, arrows=False)
    shared = set(row.get("sharedIndices", []))
    first, second = set(row.get("sourceIndices", [])), set(row.get("targetIndices", []))
    for i, (a, b) in enumerate(zip(points, points[1:])):
        if i not in first | second:
            continue
        color = GOLD if i in shared else GREEN if i in first else RUST
        start, end = projection(a), projection(b)
        node(root, "line", x1=start[0], y1=start[1], x2=end[0], y2=end[1], stroke=color, stroke_width=5 if i in shared else 4)
        arrow(root, start, end, color)
        node(root, "text", str(i), x=(start[0] + end[0]) / 2 + 7, y=(start[1] + end[1]) / 2 - 8,
            font_size=11, font_family="system-ui,sans-serif", fill="#52635e", class_="section-label")
    return export(root)


def result_line(row):
    exact = row.get("exact")
    return '<p class="result-line"><span class="result ' + ('good' if exact else 'open') + '">' + ('Ordered geometry matches' if exact else 'Ordered geometry does not match') + '</span><span>Site-sharing pattern: <strong>' + esc(fmt(row.get("sitePatternExact"))) + '</strong></span></p>'


def t_panel(row, index):
    key = 't-' + str(row['id'])
    steps = [[str(r["index"]), esc(fmt(r["sourceStep"])), esc(fmt(r["outputStep"]))] for r in row.get("stepRows", [])]
    return f'''<article class="reading-panel" id="{esc(key)}" data-reading="{esc(row['id'])}"><p class="eyebrow">{esc(row['pattern'])} · overlapping reads</p><h3>{esc(row['name'])}</h3><p>{esc(row.get('note', ''))}</p>
<div class="whole-route">{full_route(row, key + '-whole')}</div><div class="legend"><span><i style="--swatch:{GREEN}"></i>First window</span><span><i style="--swatch:{RUST}"></i>Second window</span><span><i style="--swatch:{GOLD}"></i>Shared native sections</span></div>
<p class="caption">The gold sections are referenced by both readings. The highlighted windows do not add another physical stroke to the source.</p>
{compare_views(row, key)}{result_line(row)}<div class="read-facts"><span>Action <strong>{esc(row['bits'])}</strong> · code {row['code']}</span><span>Displacement <strong>{esc(fmt(row['offset']))}</strong></span><span>Shared sections <strong>{row['sharedCount']}</strong></span></div>
<details><summary>Which sections and steps are used?</summary>{table(['Read', 'Native section indices'], [['First window', esc(fmt(row['sourceIndices']))], ['Second window', esc(fmt(row['targetIndices']))], ['Shared sections', esc(fmt(row['sharedIndices']))]])}{table(['Step', 'Source difference', 'Output difference'], steps)}</details></article>'''


def action_panel(row, index, template, offset):
    key = f'action-{index}'
    bounds = row["source"] + row["output"]
    source = route_svg(row["source"], key + '-source', "Source route", GREEN, bounds, width=530, height=270)
    output = route_svg(row["output"], key + '-output', "Native action output", BLUE, bounds, width=530, height=270)
    return f'''<article class="action-panel" data-template="{esc(row['templateId'])}" data-code="{row['code']}" data-offset="{esc(row['offsetId'])}" data-bits="{esc(row['bits'])}" {'hidden' if index else ''}>
<div class="action-heading"><h3>{esc(template['name'])}</h3><div class="code-read">{esc(row['bits'])}<span>code {row['code']}</span></div></div><div class="action-views"><figure><figcaption>Source route</figcaption>{source}</figure><figure><figcaption>Native action output</figcaption>{output}</figure></div><p class="caption">Both panels use the same display frame. The hollow marker identifies each route’s start; arrows show its recorded traversal.</p>
<div class="read-facts"><span>Displacement word <strong>{esc(fmt(offset['word']))}</strong></span><span>Frame read <strong>{esc(fmt(offset['xy']))}</strong></span><span>Complete <strong>{esc(fmt(row['complete']))}</strong></span><span>Validated application <strong>{esc(fmt(row['validated']))}</strong></span></div></article>'''


def engine_panel(row, index):
    key = 'engine-' + str(index)
    fields = ["seedSegments", "occurrences", "expandedSegments", "retainedSeedSegments", "actionRows", "validatedApplications"]
    return f'''<article class="engine-panel" data-engine="{esc(row['id'])}" data-pattern="{esc(row['pattern'])}" {'hidden' if index else ''}><p class="eyebrow">{esc(row['pattern'])} · path {esc(row['pathIndex'])}</p><h3>{esc(row['id'])}</h3><p>{esc(row.get('note', ''))}</p>{compare_views(row, key)}{result_line(row)}<div class="read-facts"><span>Action <strong>{esc(row['bits'])}</strong> · code {row['code']}</span><span>Displacement <strong>{esc(fmt(row['offset']))}</strong></span></div>{table(['Captured quantity', 'Count'], [[words(k), esc(fmt(row.get(k)))] for k in fields])}</article>'''


def control_panel(row, index):
    return f'''<article class="control-panel"><h3>{esc(row['name'])}</h3><p>{esc(row.get('note', ''))}</p>{compare_views(row, 'control-' + str(index))}{result_line(row)}</article>'''


def overview(report):
    readings = report.get("tReadings", [])
    root = svg(1100, max(1, len(readings)) * 310, 'overview', 'Overlapping route reads and their native symmetry actions')
    for i, row in enumerate(readings):
        group = node(root, "g", transform=f"translate(0 {i * 310})")
        node(group, "text", row["pattern"] + " · " + row["name"], x=25, y=32, font_family="Georgia,serif", font_size=24, fill="#263b3a")
        child = ET.fromstring(full_route(row, 'overview-' + str(i), 1100, 230))
        child.set("y", "52"); child.set("width", "1100"); child.set("height", "230")
        group.append(child)
        node(group, "text", "Green: first window   Rust: second window   Gold: sections referenced by both", x=25, y=299, font_family="system-ui,sans-serif", font_size=13, fill="#62716b")
    return export(root)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source = Path(__file__).resolve().parent
    report = json.loads(args.report.read_text(encoding="utf-8-sig"))
    templates = {row["id"]: row for row in report["templates"]}
    offsets = {row["id"]: row for row in report["offsets"]}
    actions = report["actions"]
    expected = {(template, code, offset) for template in templates for code in range(16) for offset in offsets}
    observed = {(row["templateId"], row["code"], row["offsetId"]) for row in actions}
    if observed != expected or len(observed) != len(actions):
        raise ValueError("Action laboratory needs exactly one native read for each template/code/displacement.")
    out = args.output.resolve(); out.mkdir(parents=True, exist_ok=True)
    notes = (source.parent / "SymmetryActionStudy.md").read_text(encoding="utf-8")
    notes_html = markdown.markdown(notes, extensions=['tables', 'fenced_code'])
    notes_html = notes_html.replace('href="CentrelinePatternStudy.md"', 'href="/ornaments/centreline/#notes"')
    (out / "notes.md").write_text(notes, encoding="utf-8", newline="\n")
    (out / "study-data.json").write_text(json.dumps(report, separators=(",", ":"), ensure_ascii=False), encoding="utf-8", newline="\n")
    for ext in ["js", "css"]:
        (out / ('symmetry-actions.' + ext)).write_text((source / ('SymmetryActionPage.' + ext)).read_text(encoding="utf-8"), encoding="utf-8", newline="\n")
    overview_svg = overview(report)
    (out / "SymmetryActionOverview.svg").write_text(overview_svg, encoding="utf-8", newline="\n")
    (source.parent / "SymmetryActionOverview.svg").write_text(overview_svg, encoding="utf-8", newline="\n")
    t_readings = ''.join(t_panel(row, i) for i, row in enumerate(report.get("tReadings", [])))
    action_panels = ''.join(action_panel(row, i, templates[row["templateId"]], offsets[row["offsetId"]]) for i, row in enumerate(actions))
    bit_buttons = ''.join(f'<button type="button" class="bit-toggle" data-bit="{row["bit"]}" aria-pressed="false"><span class="bit-position">b{row["bit"]}</span><strong>0</strong><span>{esc(row["name"])}</span></button>' for row in sorted(report["bitGrammar"], key=lambda r: r["bit"], reverse=True))
    grammar_rows = [[f'b{row["bit"]}', esc(row["name"]), esc(row["meaning"])] for row in sorted(report["bitGrammar"], key=lambda r: r["bit"], reverse=True)]
    template_options = ''.join(f'<option value="{esc(row["id"])}">{esc(row["name"])}</option>' for row in templates.values())
    offset_options = ''.join(f'<option value="{esc(row["id"])}">{esc(row["name"])}</option>' for row in offsets.values())
    code_bits = {row["code"]: row["bits"] for row in actions}
    code_options = ''.join(f'<option value="{code}">{esc(code_bits[code])} · code {code}</option>' for code in range(16))
    engines = report.get("engines", [])
    engine_options = ''.join(f'<option value="{esc(row["id"])}">{esc(row["pattern"])} · path {row["pathIndex"]} · {row["seedSegments"]}-section seed · {esc(row["bits"])}</option>' for row in engines)
    engine_panels = ''.join(engine_panel(row, i) for i, row in enumerate(engines))
    corpus_rows = []
    for row in report.get("corpus", []):
        first_engine = next((e for e in engines if e["pattern"] == row["id"]), None)
        link = f'<button type="button" class="inline-button" data-select-engine="{esc(first_engine["id"])}">{row["engineCount"]} candidates</button>' if first_engine else str(row["engineCount"])
        corpus_rows.append([f'<a href="/ornaments/centreline/#pattern-{esc(row["id"])}">{esc(row["id"])}</a><small>{esc(row["name"])}</small>', str(row['paths']), str(row['segments']), str(row['spots']), str(row['closedPaths']), link, str(row['pairMatches']), esc(fmt(row.get('shortestSeed'))), esc(row.get('note', ''))])
    controls = ''.join(control_panel(row, i) for i, row in enumerate(report.get("controls", [])))
    pairs = report.get("pairs", [])
    pair_table = ('<details><summary>Whole-route action witnesses checked by the native reader</summary><p>Each row is a captured action witness, not an independent vote for a pattern. Several actions may explain the same unordered route pair; the corpus table counts that pair once.</p>' + table(['Pattern', 'First path', 'Second path', 'Action bits', 'Displacement', 'Read'], [[esc(row['pattern']), str(row['first']), str(row['second']), esc(row['bits']), esc(fmt(row['offset'])), esc(row.get('note', ''))] for row in pairs]) + '</details>') if pairs else ''
    findings = ''.join('<li>' + esc(row) + '</li>' for row in report.get("findings", []))
    count_labels = {"wholePathPairs": "Distinct unordered route pairs", "pairActionMatches": "Whole-route action witnesses"}
    counts = ''.join('<span><strong>' + esc(fmt(value)) + '</strong>' + esc(count_labels.get(key, words(key))) + '</span>' for key, value in report.get("counts", {}).items())
    page = f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><base href="/ornaments/symmetry-actions/"><title>From a matching shape to an action · Gojo ornaments</title><meta name="description" content="Inspect overlapping route readings, four-bit planar symmetry actions, native generated paths and bounded candidate engines across eighteen ornaments."><link rel="stylesheet" href="/ornaments/ornaments.css"><link rel="stylesheet" href="symmetry-actions.css"><script src="symmetry-actions.js" defer></script></head><body><a class="skip" href="#main">Skip to the study</a><main id="main"><nav aria-label="Site"><a href="/ornaments/">← Ornament studies</a><a href="/ornaments/centreline/">Centre-line breakdown</a><a href="/ornaments/weave/">Following a weave</a></nav>
<header><p class="eyebrow">Recorded experiment · select, transform, compare</p><h1>From a matching shape<br>to an action</h1><p class="intro">A symmetry can describe how one part relates to another. Here we also use that relationship as an instruction: select a route, apply a small native action, and compare the generated route with an existing target.</p><p class="intro">Start with two overlapping T readings, then change the action bits yourself. The source stays available, and every result shows exactly which comparison was checked.</p></header>
<nav class="subnav" aria-label="Study"><a href="#overlap">Two readings, shared strokes</a><a href="#laboratory">Action laboratory</a><a href="#corpus">The eighteen-pattern corpus</a><a href="#engines">Candidate engines</a><a href="#controls">Limits and counterexamples</a><a href="#notes">Full study</a></nav>
<div class="scope">{esc(report['scope'])}</div><div class="counts">{counts}</div>
<section id="overlap"><h2>Two readings can use the same stroke</h2><p class="section-lead">The whole route is shown first. Green and rust identify the two selected windows; gold marks their shared section references. The three smaller drawings then separate the selected source, the native action output, and the target already present in the ornament.</p>{t_readings}</section>
<section id="laboratory"><h2>Try the action bits</h2><p class="section-lead">Choose a route, change one bit, and inspect the captured result. Three bits select a planar basis action. The fourth changes traversal. Displacement is a separate native word, so moving a copy is not hidden inside its orientation.</p><div class="lab-controls js-only"><label for="template-select">Source route</label><select id="template-select">{template_options}</select><label for="offset-select">Displacement</label><select id="offset-select">{offset_options}</select></div><div class="bit-controls js-only">{bit_buttons}</div><div class="code-controls js-only"><label for="action-code">Complete action word</label><button type="button" id="previous-action" aria-label="Previous action">←</button><select id="action-code">{code_options}</select><button type="button" id="next-action" aria-label="Next action">→</button><label class="arrow-control"><input type="checkbox" id="show-arrows" checked>Traversal arrows</label></div><p class="caption">These controls select precomputed native Gojo applications. The browser draws their captured points; it does not invent or execute the transformation.</p><noscript><p class="notice">The first recorded application is shown below. All applications are available in the recorded JSON; enable JavaScript to select them interactively.</p></noscript>{action_panels}<details><summary>What each bit means</summary><p>The displayed word is b3 b2 b1 b0. This is the small action grammar used by this experiment; it does not replace the ODV encoding or merge its distinct meanings.</p>{table(['Position', 'Action', 'Exact interpretation'], grammar_rows)}</details></section>
<section id="corpus"><h2>Read the same possibilities across eighteen patterns</h2><p class="section-lead">The search is bounded by its supplied paths, windows and action grammar. Candidate counts are overlapping explanations. They are not independent quality votes, an exhaustive set of engines, or probabilities of historical construction.</p><p class="caption">Each unordered route pair is counted once. The expandable action-witness table keeps the alternative transformations that match those pairs. The smallest seed is taken from any single route, including small inserts; it does not rank the complexity of the complete pattern.</p>{table(['Pattern', 'Paths', 'Sections', 'Spots', 'Closed paths', 'Candidate engines', 'Distinct route pairs', 'Smallest seed on any route', 'Scope'], corpus_rows, 'corpus-table')}{pair_table}</section>
<section id="engines"><h2>Inspect a candidate engine</h2><p class="section-lead">A retained seed and an action can account for several occurrences. These quantities expose the proposed reuse; they are not a calibrated measure of beauty or proof of the original maker’s method.</p><p class="caption">Recurrence predicts the selected chunks. The source retains its existing handoffs; newly generated copies are not automatically joined into one whole ornament.</p><div class="engine-controls js-only"><label for="engine-select">Candidate</label><button type="button" id="previous-engine" aria-label="Previous candidate">←</button><select id="engine-select">{engine_options}</select><button type="button" id="next-engine" aria-label="Next candidate">→</button></div>{engine_panels or '<p>No candidate engine was captured under this bounded search.</p>'}</section>
<section id="controls"><h2>What a match does—and does not—establish</h2><p class="section-lead">These controls keep positional agreement, traversal and native site-sharing available as separate questions.</p>{controls}<ul class="findings">{findings}</ul></section>
<section id="notes" class="notes"><div class="downloads"><a href="notes.md" download>Markdown study</a><a href="study-data.json" download>Complete recorded applications</a><a href="SymmetryActionOverview.svg" download>Overlapping-read SVG</a></div>{notes_html}</section><footer><p>Gojo ornament studies. Recognition, native action, observed correspondence and future settlement remain separate stages.</p><p><a href="/ornaments/centreline/">Return to the eighteen centre-line patterns</a></p></footer></main></body></html>'''
    (out / "index.html").write_text(page, encoding="utf-8", newline="\n")
    print(json.dumps({"output": str(out), "readings": len(report.get('tReadings', [])), "actions": len(actions), "engines": len(engines), "controls": len(report.get('controls', []))}))


if __name__ == '__main__':
    main()
