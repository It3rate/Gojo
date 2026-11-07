"""Draw three symmetry witnesses from the executed fret segments."""

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


PAPER, INK, PALE = "#fcfaf3", "#263b37", "#bac4bc"
GREEN, RUST, GRAY = "#186c61", "#ae502e", "#58665d"


def node(root, tag, **attrs):
    return ET.SubElement(root, tag, {key.replace("_", "-"): str(value) for key, value in attrs.items()})


def label(root, x, y, text, size=16, color=INK, weight="normal"):
    node(root, "text", x=x, y=y, font_size=size, fill=color, font_weight=weight).text = text


def stroke(root, points, color, width=4):
    coords = " ".join(f"{x:.2f},{y:.2f}" for x, y in points)
    node(root, "polyline", points=coords, fill="none", stroke=color, stroke_width=width,
         stroke_linejoin="round", stroke_linecap="butt")


def guide(root, x1, y1, x2, y2):
    span = max(x2 - x1, y2 - y1)
    ticks = []
    for offset in range(0, int(span), 8):
        end = min(offset + 4, span)
        dx, dy = (x2 - x1) / span, (y2 - y1) / span
        ticks.append(f"M{x1 + dx*offset:g},{y1 + dy*offset:g} L{x1 + dx*end:g},{y1 + dy*end:g}")
    node(root, "path", d=" ".join(ticks), stroke=GRAY, stroke_width=1)


def cardinal(vector):
    x, y = vector
    if x and not y:
        return "E" if x > 0 else "W"
    if y and not x:
        return "N" if y > 0 else "S"
    raise ValueError("Expected a nonzero cardinal stroke")


def render(data):
    moves = [(s["x"], s["y"]) for s in data["segments"]]
    if len(moves) != 16:
        raise ValueError("This annotated witness expects one sixteen-stroke bay")
    points = [(0, 4)]  # Upper-left corner of the first long top bar, y positive upward.
    for dx, dy in moves:
        x, y = points[-1]
        points.append((x + dx, y + dy))
    pitch = points[-1][0] - points[0][0]

    def at(i):
        bay, slot = divmod(i, len(moves))
        x, y = points[slot]
        return (x + pitch * bay, y)

    svg = ET.Element("svg", xmlns="http://www.w3.org/2000/svg", viewBox="0 0 900 720",
                     width="900", height="720", role="img")
    node(svg, "title").text = "Three reads of the interlocking fret"
    node(svg, "desc").text = "Colored paths distinguish whole-band glide symmetry, diagonal correspondence of directions, and a scaled cap requiring cuts."
    node(svg, "rect", width=900, height=720, fill=PAPER)
    page = node(svg, "g", font_family="Segoe UI, Arial, sans-serif")
    label(page, 26, 38, "Three reads of the same fret", 29, weight="bold")
    label(page, 26, 65, "One tracing, viewed through the whole band, ordered directions and partial spans.", color=GRAY)

    def section(top, title):
        node(page, "line", x1=26, y1=top, x2=874, y2=top, stroke=PALE)
        label(page, 26, top + 28, title, 21, weight="bold")

    section(83, "The band repeats and mirrors")
    label(page, 26, 135, "The copper half repeats the green half four ticks right and on the other side of the midline.")
    project = lambda p: (45 + (p[0] + 0.5) * 20, 239 - p[1] * 20)
    # The visible path starts and ends partway along a bottom stroke.
    strip = node(page, "g")
    finite = [(-0.5, 0)] + [at(i) for i in range(-7, 73)] + [(39.5, 0)]
    stroke(strip, [project(p) for p in finite], PALE, 4)
    stroke(strip, [project(at(i)) for i in range(16, 25)], GREEN)
    stroke(strip, [project(at(i)) for i in range(24, 33)], RUST)
    axis, middle = project((19.5, 2))
    guide(page, axis, 149, axis, 250)
    guide(page, 45, middle, 845, middle)
    label(page, 26, 276, "Vertical dash: central mirror.     Horizontal dash: glide midline.     One full repeat: 8 ticks.", 15, GRAY)

    section(297, "A diagonal correspondence in the turn order")
    label(page, 26, 348, "Exchange horizontal and vertical directions. The eight arrows correspond; their reaches differ.")
    for index, start in enumerate((5, 12)):
        left, color = 28 + index * 435, (GREEN, RUST)[index]
        line = [at(start + j) for j in range(9)]
        min_x, max_x = min(x for x, y in line), max(x for x, y in line)
        min_y, max_y = min(y for x, y in line), max(y for x, y in line)
        mid_x, mid_y = (min_x + max_x) / 2, (min_y + max_y) / 2
        plot = [(left + 110 + (x - mid_x) * 17, 402 - (y - mid_y) * 17) for x, y in line]
        stroke(page, plot, color)
        node(page, "circle", cx=plot[0][0], cy=plot[0][1], r=4.5, fill=PAPER, stroke=color, stroke_width=2)
        vectors = [moves[(start + j) % len(moves)] for j in range(8)]
        label(page, left + 214, 395, f"Start at stroke {start + 1}", 16, color, "bold")
        label(page, left + 214, 421, " ".join(cardinal(v) for v in vectors), 14)
    label(page, 26, 468, "Open dot: start of the run. The two patches are drawn at the same scale.", 15, GRAY)

    section(491, "A scaled cap inside the ink")
    label(page, 26, 543, "Two cuts on the inner arms isolate a cap. A half-turn and a 7/2 enlargement match the outer cap.")
    cap = data["cap"]
    inner = [at(cap["inner"][0] + j) for j in range(4)]  # S3 W2 N1
    outer = [at(cap["outer"][0] + j) for j in range(4)]  # N3 E7 S3
    a, b = inner[1], inner[2]  # keep the whole horizontal; trim its neighboring arms
    first, _, last = cap["requiredInnerNumerators"]
    den = cap["scaleNumerator"]
    cut = [(a[0] - first["x"] / den, a[1] - first["y"] / den), a, b,
           (b[0] + last["x"] / den, b[1] + last["y"] / den)]
    scale = 24
    def inner_point(p):
        return (207 + (p[0] - 6) * scale, 644 - (p[1] - 1) * scale)
    stroke(page, [inner_point(p) for p in inner], PALE)
    stroke(page, [inner_point(p) for p in cut], GREEN)
    for p in (cut[0], cut[-1]):
        x, y = inner_point(p)
        node(page, "circle", cx=x, cy=y, r=4, fill=PAPER, stroke=GREEN, stroke_width=2)
    x0, y0 = outer[0]
    stroke(page, [(500 + (x - x0) * scale, 644 - (y - y0) * scale) for x, y in outer], RUST)
    label(page, 75, 678, "Inner cap with two new endpoints", 16, GREEN)
    label(page, 498, 678, "Outer cap, three whole strokes", 16, RUST)
    label(page, 26, 706, "Open dots: cuts. Gray: the unused parts of the inner arms.", 15, GRAY)
    return ET.tostring(svg, encoding="unicode") + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render(json.loads(args.report.read_text(encoding="utf-8-sig"))), encoding="utf-8")


if __name__ == "__main__":
    main()
