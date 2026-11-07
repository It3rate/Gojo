"""Render the recorded corner correspondence and complementary channels."""

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from SymmetryGallery import node, label, stroke


PAPER, INK, MUTED, RULE = "#fcfaf4", "#223a38", "#4a605c", "#ccd4c9"
RED, BLUE, FAINT = "#b63236", "#215ca8", "#b9c5bc"
TOP, BOTTOM, BOTH, ENCLOSED = "#bde8de", "#f4d1b3", "#d5c9e6", "#dedad0"
TOP_EDGE, BOTTOM_EDGE = "#277769", "#975021"


def vectors(pattern):
    return [(v["x"], v["y"]) for v in pattern["segments"]]


def vertices(pattern):
    points = [(0, 0)]
    for dx, dy in vectors(pattern):
        x, y = points[-1]
        points.append((x + dx, y + dy))
    return points


def polygon(g, points, color):
    node(g, "polygon", points=" ".join(f"{x:.2f},{y:.2f}" for x, y in points), fill=color)


def arrow(g, a, b, color, size):
    """Interior arrowhead so the shared pivot and cut endpoints stay legible."""
    x, y = ((a[0] + 2 * b[0]) / 3, (a[1] + 2 * b[1]) / 3)
    dx, dy = (b[0] - a[0], b[1] - a[1])
    length = (dx*dx + dy*dy)**0.5
    ux, uy = dx/length, dy/length
    polygon(g, [(x + size*ux, y + size*uy),
                (x - size*ux + .65*size*uy, y - size*uy - .65*size*ux),
                (x - size*ux - .65*size*uy, y - size*uy + .65*size*ux)], color)


def region_color(region, enclosed):
    return (BOTH if region["bottom"] else TOP) if region["top"] else (BOTTOM if region["bottom"] else enclosed)


def plot(svg, page, key, pattern, left, top, start_x, end_x, scale, channels, show_mouths, enclosed=ENCLOSED):
    gap = pattern["gaps"]
    pitch, low, high = gap["pitch"], gap["bottom"], gap["top"]
    def screen(p):
        return left + (p[0] - start_x)*scale, top + (high - p[1])*scale

    area = node(svg, "clipPath", id=key)
    width = (end_x - start_x)*scale
    height = (high - low)*scale
    node(area, "rect", x=left, y=top-3, width=width, height=height+6)
    g = node(page, "g", clip_path=f"url(#{key})")
    # Include neighboring repeats where a stroke or mouth wraps across the cut.
    points = vertices(pattern)
    for cycle in range(int(start_x // pitch) - 2, int(end_x // pitch) + 3):
        offset = cycle*pitch
        if channels:
            for region in gap["regions"]:
                for c in region["cells"]:
                    x, y = screen((offset+c["x0"], c["y1"]))
                    node(g, "rect", x=x, y=y, width=(c["x1"]-c["x0"])*scale,
                         height=(c["y1"]-c["y0"])*scale, fill=region_color(region, enclosed))
    for cycle in range(int(start_x // pitch) - 2, int(end_x // pitch) + 3):
        stroke(g, [screen((x + cycle*pitch, y)) for x, y in points],
               INK if channels else FAINT, 2.7 if channels else 5)
    if show_mouths:
        for cycle in range(int(start_x // pitch) - 1, int(end_x // pitch) + 1):
            for mouth in gap["mouths"]:
                above = mouth["side"] == "top"
                start, end = mouth["start"] + cycle*pitch, mouth["end"] + cycle*pitch
                mid = (start+end)/2
                if not start_x < mid < end_x:
                    continue
                x, y = screen((mid, high if above else low))
                sign = -1 if above else 1
                use = TOP_EDGE if above else BOTTOM_EDGE
                # A bracket sits outside the open span; its tip points inward.
                lo, hi = max(start_x, start), min(end_x, end)
                stroke(page, [(screen((lo, 0))[0], y+6*sign), (screen((hi, 0))[0], y+6*sign)], use, 2)
                polygon(page, [(x-4, y+13*sign), (x+4, y+13*sign), (x, y+8*sign)], use)
    return screen


def cut_arm(points, index, size):
    corner = points[index]
    def tip(other):
        dx, dy = other[0] - corner[0], other[1] - corner[1]
        extent = abs(dx) + abs(dy)
        if extent < size:
            raise ValueError("Selected cut extends beyond the stroke")
        return corner[0] + size*dx/extent, corner[1] + size*dy/extent
    return [tip(points[index-1]), corner, tip(points[index+1])]


def coverage_label(pattern, axis):
    widths = sorted(w["width"] for w in pattern["gaps"]["widths"] if w["axis"] == axis)
    return ", ".join(str(w) for w in widths) or "none"


def render(report):
    patterns = {p["id"]:p for p in report["patterns"]}
    fret = patterns["interlock"]
    svg = ET.Element("svg", xmlns="http://www.w3.org/2000/svg", width="950", height="952",
                     viewBox="0 0 950 952", role="img")
    node(svg, "title").text = "Directed corners and white channels in three frets"
    node(svg, "desc").text = "The shared tip of a red and blue corner is a rotation center. Connected white regions are filled according to their exposed openings."
    node(svg, "rect", width=950, height=952, fill=PAPER)
    page = node(svg, "g", font_family="Arial, sans-serif")
    label(page, 26, 39, "Corner flow and white channels", 30, INK, "bold")
    label(page, 26, 66, "The same ink exposes different patterns when read as a path, a pivot or a boundary.", 16, MUTED)

    def section(y, title):
        node(page, "line", x1=26, y1=y, x2=924, y2=y, stroke=RULE)
        label(page, 26, y+28, title, 21, INK, "bold")

    section(84, "Two corners share a rotation center")
    project = plot(svg, page, "detail", fret, 34, 138, 0, 8, 39, False, False)
    points = vertices(fret)
    match, = [m for m in fret["joints"]["matches"] if m["source"] == 6 and m["target"] == 4
              and m["frame"] == "quarter-left" and m["reversed"]]
    size = min(match["firstCut"], match["lastCut"])
    red, blue = cut_arm(points, match["source"], size), cut_arm(points, match["target"], size)
    pivot = (match["pivot"]["x2"]/2, match["pivot"]["y2"]/2)
    if red[0] != blue[-1] or red[0] != pivot:
        raise ValueError("The expected shared tip is absent.")
    for arm, color in ((red, RED), (blue, BLUE)):
        xyz = [project(p) for p in arm]
        stroke(page, xyz, color, 6)
        for a, b in zip(xyz, xyz[1:]):
            arrow(page, a, b, color, 9)
    x, y = project(pivot)
    rx, ry = project(red[1])
    bx, by = project(blue[1])
    radius = abs(by - ry)
    node(page, "path", d=f"M{rx:g},{ry:g} A{radius:g},{radius:g} 0 0 0 {bx:g},{by:g}",
         stroke=MUTED, fill="none", stroke_width=1.5, stroke_dasharray="2 3")
    node(page, "circle", cx=x, cy=y, r=5.2, fill=PAPER, stroke=INK, stroke_width=2)
    label(page, 409, 152, "Red leaves the shared tip", 18, RED, "bold")
    label(page, 409, 177, "North, then west. Its corner turns left.", 16, INK)
    label(page, 409, 210, "Blue enters the same tip", 18, BLUE, "bold")
    label(page, 409, 235, "North, then east. Its corner turns right.", 16, INK)
    label(page, 409, 269, "Turn the red ink 90° counterclockwise about the open dot.", 16, INK)
    label(page, 409, 292, "It covers the blue arms in reverse traversal order.", 16, INK)
    label(page, 34, 319, "Arrows follow the pen. The highlighted extent is one tick on each arm.", 15, MUTED)

    section(338, "Spacing between pivots on the midline")
    for row, (field, name) in enumerate((("sharedEndQuarterRows", "Shared-tip quarter-turns"),
                                         ("wholeBandHalfTurnRows", "Whole-band half-turns"))):
        pivot_row, = [r for r in fret["joints"][field] if r["y2"] == match["pivot"]["y2"]]
        y, pitch, scale = 395 + row*36, fret["gaps"]["pitch"], 27
        stroke(page, [(34, y), (34 + 2*pitch*scale, y)], RULE, 2)
        for lap in range(2):
            for x2 in pivot_row["x2"]:
                node(page, "circle", cx=34+(lap*pitch+x2/2)*scale, cy=y, r=4.5,
                     fill=BLUE if row == 0 else PAPER, stroke=BLUE if row == 0 else INK, stroke_width=2)
        spacings = " · ".join(f"{gap/2:g}" for gap in pivot_row["gaps2"])
        label(page, 505, y+5, f"{name}     {spacings} ticks", 16, INK)

    section(466, "Where the white channels open")
    plot(svg, page, "mouths", fret, 34, 538, 0, 3*fret["gaps"]["pitch"], 19, True, True)
    label(page, 545, 536, "Opens toward the top", 17, TOP_EDGE, "bold")
    label(page, 545, 563, "Opens toward the bottom", 17, BOTTOM_EDGE, "bold")
    label(page, 545, 596, "Colored brackets mark the open spans.", 15, INK)
    label(page, 545, 619, "Black strokes bound the connected white regions.", 15, INK)
    label(page, 34, 654, "The strip shows three periods. Left and right edges continue into the next repeat.", 15, MUTED)

    section(673, "Three signatures at the same tick scale")
    for col, key in enumerate(("interlock", "meyer1", "meyer2")):
        pat = patterns[key]
        left = 26 + col*309
        label(page, left, 738, pat["name"], 17, INK, "bold")
        scale = 16
        plot(svg, page, f"comparison-{col}", pat, left, 757, 0, 2*pat["gaps"]["pitch"], scale, True, False)
        turn_run = max((r["turns"] for r in pat["course"]["turnRuns"]), default=0)
        label(page, left, 882, f'Longest same-hand turn run   {turn_run}', 15, INK)
        label(page, left, 905, f'Gap widths   x {coverage_label(pat,"x")}   ·   y {coverage_label(pat,"y")}', 15, MUTED)
    label(page, 26, 938, "Gap widths are distinct centerline separations in ticks, measured between neighboring parallel strokes.", 15, MUTED)
    return ET.tostring(svg, encoding="unicode") + "\n"


def render_variants(report):
    patterns = {p["id"]:p for p in report["patterns"]}
    pocket, pivot = "#d7b2d7", "#5d3d78"
    svg = ET.Element("svg", xmlns="http://www.w3.org/2000/svg", width="950", height="359",
                     viewBox="0 0 950 359", role="img")
    node(svg, "title").text = "Gaps and pivots under a changed reach"
    node(svg, "desc").text = "Enclosed regions in the stretched fret are purple. Two hook settings show the change from one-tick nearest neighbors to grouped rotation centers."
    node(svg, "rect", width=950, height=359, fill=PAPER)
    page = node(svg, "g", font_family="Arial, sans-serif")
    label(page, 26, 39, "Gaps and pivots under a changed reach", 29, INK, "bold")
    label(page, 26, 65, f'The two hook settings were selected after the {len(report["hunt"])}-setting comparison. Their final drop differs by one tick.', 16, MUTED)
    node(page, "line", x1=26, y1=82, x2=924, y2=82, stroke=RULE)
    for col, (key, title) in enumerate((
        ("changed", "One stretched inner reach"), ("hook-neighbor", "Hook with one-tick neighbors"),
        ("hook-pivots", "Hook with paired pivots")
    )):
        pat = patterns[key]
        gap = pat["gaps"]
        left = 26+309*col
        label(page, left, 112, title, 18, INK, "bold")
        screen = plot(svg, page, f"variant-{col}", pat, left, 149, 0, 2*gap["pitch"],
                      15.5, True, True, enclosed=pocket)
        regions = [r for r in gap["regions"] if not r["top"] and not r["bottom"]]
        if regions:
            areas = [r["area"] for r in regions]
            area_label = f'{areas[0]:g} square ticks each' if len(set(areas)) == 1 else +                "Areas in square ticks  " + ", ".join(f'{area:g}' for area in areas)
            label(page, left, 259, f'{len(regions)} enclosed pockets per repeat', 16, INK, "bold")
            label(page, left, 285, area_label, 15, MUTED)
        else:
            rows = pat["joints"]["sharedEndQuarterRows"]
            for row in rows:
                for lap in range(2):
                    for x2 in row["x2"]:
                        x, y = screen((x2/2+lap*gap["pitch"], row["y2"]/2))
                        node(page, "circle", cx=x, cy=y, r=3.7, fill=pivot, stroke=PAPER, stroke_width=1.2)
            nearest = sum(stroke["width"] == 1 for stroke in gap["nearest"])
            label(page, left, 259, f'{nearest}/{len(gap["nearest"])} strokes have a one-tick neighbor', 16, INK, "bold")
            if len(rows) == 1:
                gaps = " · ".join(f'{g/2:g}' for g in rows[0]["gaps2"])
                note = f'Pivot gaps   {gaps} ticks'
            else:
                note = f'Pivots occupy {len(rows)} different rows.'
            label(page, left, 285, note, 15, MUTED)
    node(page, "line", x1=26, y1=304, x2=924, y2=304, stroke=RULE)
    for x, color, name in ((26, TOP, "Opens at top"), (210, BOTTOM, "Opens at bottom"),
                           (421, pocket, "Enclosed pocket")):
        node(page, "rect", x=x, y=326, width=16, height=12, fill=color)
        label(page, x+23, 338, name, 15, MUTED)
    node(page, "circle", cx=657, cy=332, r=4, fill=pivot)
    label(page, 669, 338, "Shared-tip quarter-turn pivot", 15, MUTED)
    return ET.tostring(svg, encoding="unicode") + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("report", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("variants", type=Path, nargs="?")
    args = parser.parse_args()
    report = json.loads(args.report.read_text(encoding="utf-8-sig"))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render(report), encoding="utf-8")
    if args.variants:
        args.variants.parent.mkdir(parents=True, exist_ok=True)
        args.variants.write_text(render_variants(report), encoding="utf-8")


if __name__ == "__main__":
    main()
