"""Render the recorded drawing exhibits as an offline gallery and a contact sheet."""
import argparse
import csv
import hashlib
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def read_csv(path):
    if not path.exists():
        return []
    with path.open(encoding="utf-8-sig", newline="") as handle:
        return list(csv.DictReader(handle))


def read_exhibits(run):
    raw = (run / "exhibits.json").read_bytes()
    data = json.loads(raw.decode("utf-8-sig"))
    examples = data["examples"]
    if not examples or len({row["id"] for row in examples}) != len(examples):
        raise ValueError("Exhibits must have unique IDs and at least one example")
    for row in examples:
        sites = row["sites"]
        for edge in row["strokes"]:
            a, b = edge["a"], edge["b"]
            if not (0 <= a < len(sites) and 0 <= b < len(sites)):
                raise ValueError(f"Unbound segment in {row['id']}")
            xy = [edge[k] for k in ("x1", "y1", "x2", "y2")]
            if (not all(map(math.isfinite, xy))
                    or xy != [sites[a]["x"], sites[a]["y"], sites[b]["x"], sites[b]["y"]]):
                raise ValueError(f"Site/segment projection mismatch in {row['id']}")
    data["record"] = hashlib.sha256(raw).hexdigest()
    return data


def features_for_sheet(example):
    # Deduplicate translations and uniform changes of scale for the picture
    # selection only. The gallery retains every original candidate and scenario.
    edges = example["strokes"]
    if not edges:
        return ()
    start_x = min(min(edge["x1"], edge["x2"]) for edge in edges)
    start_y = min(min(edge["y1"], edge["y2"]) for edge in edges)
    extent = max(max(max(e["x1"], e["x2"]) - start_x, max(e["y1"], e["y2"]) - start_y)
                 for e in edges) or 1
    return tuple(sorted({tuple(sorted(((round((e["x1"]-start_x)/extent, 4), round((e["y1"]-start_y)/extent, 4)),
                                       (round((e["x2"]-start_x)/extent, 4), round((e["y2"]-start_y)/extent, 4)))))
                         for e in edges}))


def sheet_examples(examples, count=6):
    """One distinct high-utility design per brief, then distinct contrasting examples."""
    briefs = list(dict.fromkeys(e["brief"] for e in examples))
    seen = set()
    selected = []

    def add(e):
        signature = features_for_sheet(e)
        if not signature or signature in seen or len(selected) >= count:
            return False
        seen.add(signature)
        selected.append(e)
        return True

    for brief in briefs:
        group = (e for e in examples if e["brief"] == brief)
        for row in sorted(group, key=lambda e: -e["utility"]):
            if add(row):
                break
    for row in sorted(examples, key=lambda e: e["utility"]):
        add(row)
    return selected


def construction_examples(examples):
    """Examples of deposited geometry and witnessed mechanisms, regardless of utility."""
    candidates = [e for e in examples if e["strokes"]]
    seen = set()
    selected = []
    evidence = {}

    def count(e, kind):
        return sum(ev["kind"] == kind for ev in e["events"])

    reads = [
        ("drawing directions", lambda e: len({s["direction"] for s in e["strokes"]})),
        ("frame switches", lambda e: count(e, "switch")),
        ("branch events", lambda e: count(e, "branch")),
        ("merge events", lambda e: count(e, "merge")),
        ("extent slots", lambda e: len({s["extent"] for s in e["strokes"]})),
        ("drawing owners", lambda e: len({s["owner"] for s in e["strokes"]})),
    ]
    for name, measure in reads:
        for e in sorted(candidates, key=lambda e: (-measure(e), e["id"])):
            shape = features_for_sheet(e)
            if measure(e) > 0 and shape not in seen:
                seen.add(shape)
                selected.append(e)
                evidence[e["id"]] = f"{measure(e)} {name}"
                break
    return selected, evidence


def font(size, serif=False, bold=False):
    names = (["georgiab.ttf", "DejaVuSerif-Bold.ttf"] if serif and bold else
             ["georgia.ttf", "DejaVuSerif.ttf"] if serif else
             ["segoeuib.ttf", "DejaVuSans-Bold.ttf"] if bold else ["segoeui.ttf", "DejaVuSans.ttf"])
    for name in names:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


def human(value):
    return str(value).replace("_", " ").replace("-", " ").strip()


def contact_sheet(run, selection, filename, heading, note, captions=None):
    units = 2
    width = 1360
    cols = 2 if len(selection) > 1 else 1
    cell_w = (width - 100) // cols
    cell_h = 206
    rows = (len(selection) + cols - 1) // cols
    height = 142 + rows * cell_h + 42
    paper = "#f8f6ef"
    ink = "#192d29"
    light = "#dcd8cb"
    image = Image.new("RGB", (width * units, height * units), paper)
    draw = ImageDraw.Draw(image)

    def text(x, y, value, size, color=ink, serif=False, bold=False):
        draw.text((x * units, y * units), value, font=font(size * units, serif, bold), fill=color)

    def line(x1, y1, x2, y2, color, weight):
        draw.line((x1 * units, y1 * units, x2 * units, y2 * units), fill=color, width=weight * units)

    text(50, 21, heading, 44, serif=True)
    text(50, 86, note, 19)
    for i, e in enumerate(selection):
        left = 50 + (i % cols) * cell_w
        top = 142 + (i // cols) * cell_h
        line(left, top, left + cell_w - 21, top, light, 1)
        title = human(e["brief"]) + "  /  " + (captions[e["id"]] if captions else human(e["label"]))
        if len(title) > 68:
            title = title[:66] + "…"
        text(left, top + 10, title, 16, bold=True)
        anno = f"{human(e['arm'])}    ·    {len(e['strokes'])} strokes    ·    utility {e['utility']:.3g}"
        text(left, top + 38, anno, 14, "#59645b")
        edges = e["strokes"]
        xs = [e[k] for e in edges for k in ("x1", "x2")]
        ys = [e[k] for e in edges for k in ("y1", "y2")]
        ww, hh = cell_w-48, cell_h-90
        x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
        scale = min(ww / max(1, x1-x0), hh / max(1, y1-y0))
        cx, cy = left+(cell_w-21)/2, top+68+hh/2
        for edge in edges:
            xy = [(cx+(edge[x]-(x0+x1)/2)*scale, cy-(edge[y]-(y0+y1)/2)*scale)
                  for x,y in (("x1","y1"),("x2","y2"))]
            line(*xy[0], *xy[1], ink, 2)
    image.resize((width, height), Image.Resampling.LANCZOS).save(run / filename)


def build(run):
    data = read_exhibits(run)
    design = json.loads((run / "design/design.json").read_text(encoding="utf-8-sig"))
    data["briefAims"] = {b["name"]: b["aim"] for b in design["briefs"]}
    data["criterionReads"] = {c["key"]: c["read"] for c in design["criteria"]}
    data["criterionUtility"] = read_csv(run / "criteria-utility.csv")
    data["groupComparison"] = read_csv(run / "group-comparison.csv")
    chosen = sheet_examples(data["examples"])
    data["featured"] = [e["id"] for e in chosen]
    constructions, reasons = construction_examples(data["examples"])
    data["constructions"] = reasons
    payload = json.dumps(data, ensure_ascii=False, separators=(",", ":"), allow_nan=False)
    template = Path(__file__).with_name("Gallery.html").read_text(encoding="utf-8")
    html = template.replace("/*__RECORDS__*/", payload.replace("<", "\\u003c"))
    (run / "Explore.html").write_text(html, encoding="utf-8")
    contact_sheet(run, chosen, "ContactSheet.png", "Ornament paths",
                  "Recorded machine output. Each panel is one complete run, fitted independently to the page.")
    contact_sheet(run, constructions, "ConstructionSheet.png", "Six construction reads",
                  "Selected for what happened while drawing. Each panel is one complete run, fitted independently.", reasons)
    print(f"{len(data['examples'])} recorded examples. {run / 'Explore.html'}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("run", type=Path)
    build(parser.parse_args().run)
