"""Render deposited pen marks. Usage: FlowGallery.py trace.json plate.svg"""

import argparse
import json
from pathlib import Path
import xml.etree.ElementTree as ET


ROWS = (
    ("Original reaches", (("fixed", "Two fixed descents"), ("feedback", "Return to baseline"))),
    ("Increased rise", (("stretched-fixed", "Two fixed descents"), ("stretched-feedback", "Return to baseline"))),
    ("Changed frame", (("mirrored", "Mirrored"), ("diagonal", "Diagonal reflection"))),
    ("Mirrored at each repeat", (("alternating", "Frame switches at the bay seam"),)),
    ("Independent pens", (("accent", "Main pen and spot pen"),)),
    ("Alternative continuations", (("trial-fixed", "Fixed trial"), ("trial-feedback", "Feedback trial"))),
)
PAPER, INK, MUTED, GUIDE, ACCENT = "#fbf9f1", "#253f38", "#55635a", "#bebfb1", "#ad5433"
WIDTH, PANEL, ROW = 920, 432, 165


def node(parent, tag, **attributes):
    return ET.SubElement(parent, tag, {key.replace("_", "-"): str(v) for key, v in attributes.items()})


def caption(parent, x, y, value, size, weight="normal", color=INK):
    node(parent, "text", x=x, y=y, font_size=size, font_weight=weight, fill=color).text = value


def visible(sample):
    return [mark for mark in sample["marks"] if mark["kind"] in ("line", "spot")]


def measured_frame(samples, plot_height):
    xs, ys = [0], [0]
    for sample in samples:
        for mark in visible(sample):
            xs.extend((mark["x1"], mark["x2"]))
            ys.extend((mark["y1"], mark["y2"]))
    left, right, low, high = min(xs), max(xs), min(ys), max(ys)
    scale = min((PANEL - 36) / max(1, right - left), plot_height / max(1, high - low))
    return ((left + right) / 2, (low + high) / 2, scale)


def draw_panel(panel, sample, label, frame, plot_height):
    mid_x, mid_y, scale = frame

    def project(x, y):
        y_center = 35 + plot_height / 2
        return round(PANEL / 2 + (x - mid_x) * scale, 3), round(y_center - (y - mid_y) * scale, 3)

    caption(panel, 0, 17, label, 15, color=MUTED)
    base_x, base_y = project(0, 0)
    if sample["name"] == "diagonal":
        rule = dict(x1=base_x, y1=28, x2=base_x, y2=42+plot_height)
    else:
        rule = dict(x1=8, y1=base_y, x2=PANEL-8, y2=base_y)
    node(panel, "line", **rule, stroke=GUIDE, stroke_dasharray="3 5", stroke_width=1)
    marks = visible(sample)
    if not marks:
        caption(panel, PANEL / 2 - 48, 68, "No deposited ink", 13, color=MUTED)
    # Spots are rendered on the upper display layer so the second pen is
    # visible when its site coincides with the primary pen's stroke.
    for mark in sorted(marks, key=lambda item: item["kind"] == "spot"):
        x, y = project(mark["x1"], mark["y1"])
        end_x, end_y = project(mark["x2"], mark["y2"])
        color = INK if mark["pen"] == 0 else ACCENT
        if mark["kind"] == "spot":
            ink = node(panel, "circle", cx=end_x, cy=end_y, r=4, fill=color, stroke=PAPER, stroke_width=1)
        else:
            ink = node(panel, "line", x1=x, y1=y, x2=end_x, y2=end_y,
                       stroke=color, stroke_width=2.8, stroke_linecap="round")
        node(ink, "title").text = f'Pen {mark["pen"]}, beat {mark["beat"]} · {mark["kind"]}'


def render(data):
    samples = {sample["name"]: sample for sample in data["samples"]}
    groups = [(title, [(name, label) for name, label in pairs if name in samples]) for title, pairs in ROWS]
    groups = [(title, pairs) for title, pairs in groups if pairs]
    if not groups:
        raise ValueError("The trace has no recognized pilot samples.")
    row_heights = [ROW + 55 if title == "Changed frame" else ROW for title, _ in groups]
    height = 94 + sum(row_heights)
    svg = ET.Element("svg", xmlns="http://www.w3.org/2000/svg", viewBox=f"0 0 {WIDTH} {height}",
                     width=str(WIDTH), height=str(height), role="img")
    node(svg, "title").text = "Fret constructions under matched inputs"
    node(svg, "desc").text = "Paired panels share a scale. The original and increased reaches also share their scale. The dashed rule locates the starting baseline."
    node(svg, "rect", width=WIDTH, height=height, fill=PAPER)
    page = node(svg, "g", font_family="Segoe UI, Arial, sans-serif")
    caption(page, 24, 42, "Fret constructions under matched inputs", 29, "bold")
    caption(page, 24, 66, "Dashed rule = starting baseline.   Green = main pen.   Copper = second pen.", 14, color=MUTED)
    # Align all primary comparisons. Use a separate frame for transformed
    # pairs so a vertical copy can fully occupy its plotting area.
    primary = [samples[key] for key in ("fixed", "feedback", "stretched-fixed", "stretched-feedback") if key in samples]
    top = 92
    for (title, pairs), row_height in zip(groups, row_heights):
        plot_height = row_height - 79
        node(page, "line", x1=24, y1=top, x2=WIDTH-24, y2=top, stroke=GUIDE, stroke_width=1)
        primary_row = title in ("Original reaches", "Increased rise")
        first = samples[pairs[0][0]]
        title_label = title
        if primary_row and "a" in first and "b" in first:
            title_label += f"  ·  A = {first['a']}, B = {first['b']}"
        caption(page, 24, top+24, title_label, 19, "bold")
        frames = [measured_frame(primary if primary_row else [samples[name]], plot_height) for name, _ in pairs]
        common_scale = min(frame[2] for frame in frames)
        for column, ((name, label), (cx, cy, _)) in enumerate(zip(pairs, frames)):
            panel = node(page, "g", transform=f"translate({24 + column * (PANEL + 12)} {top + 34})")
            draw_panel(panel, samples[name], label, (cx, cy, common_scale), plot_height)
        top += row_height
    return ET.tostring(svg, encoding="unicode") + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("trace", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    data = json.loads(args.trace.read_text(encoding="utf-8-sig"))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(render(data), encoding="utf-8")


if __name__ == "__main__":
    main()
