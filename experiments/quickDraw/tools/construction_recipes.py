"""Author numeric construction programs; C# executes and archives their structure.

No finished-coordinate snapshots are supplied. Locators name earlier sites and
parameter bindings. Names and prose below are presentation, not native meaning.
"""
from __future__ import annotations

import argparse
import copy
import json
import math
import re
from pathlib import Path


def loc(first, second=None, numerator=0, denominator=1000, origin=None):
    value = {"from": first, "to": second or first,
             "numerator": numerator, "denominator": denominator}
    if origin is not None:
        value["origin"] = origin
    return value


class Recipe:
    def __init__(self, key, title, parameters=None):
        self.data = {"schema": "gojo.drawing-construction-source.v1", "id": key,
                     "family": key, "title": title, "variantLabel": "Reference",
                     "viewBox": [0, 0, 512, 512], "parameters": [], "sites": [],
                     "edges": [], "rungs": [], "constraints": [], "repeats": [], "circular": []}
        for key, value in (parameters or {}).items():
            label = re.sub(r"([A-Z])", r" \1", key).capitalize()
            self.data["parameters"].append({"id": key, "value": value, "label": label})
        self.at = -1

    def begin(self, title, note):
        self.at += 1
        self.data["rungs"].append({"id": f"r{self.at}", "title": title, "note": note,
                                   "visible": [], "guides": [], "selected": [], "introduced": []})

    def point(self, key, x, y):
        self.data["sites"].append({"id": key, "at": self.at, "x": x, "y": y})
        return key

    def frame(self):
        self.point("page-origin", 0, 0)
        self.point("page-extent", 512, 512)

    def page(self, key, x, y):
        return self.point(key, loc("page-origin", "page-extent", x, 512),
                          loc("page-origin", "page-extent", y, 512))

    def inside(self, key, a, b, x, y):
        return self.point(key, loc(a, b, x), loc(a, b, y))

    def edge(self, key, controls, label=None):
        self.data["edges"].append({"id": key, "at": self.at, "controls": controls,
                                   "label": label or key.replace("-", " ")})
        self.data["rungs"][-1]["introduced"].append(key)
        return key

    def poly(self, key, sites, closed=True):
        pairs = list(zip(sites, sites[1:] + ([sites[0]] if closed else [])))
        return [self.edge(f"{key}-{i}", [a, b]) for i, (a, b) in enumerate(pairs)]

    def rectangle(self, key, a, b):
        tr = self.point(key + "-tr", loc(b), loc(a))
        bl = self.point(key + "-bl", loc(a), loc(b))
        corners = [a, tr, b, bl]
        return corners, self.poly(key, corners)

    def finish(self, visible, guides=()):
        rung = self.data["rungs"][-1]
        previous = self.data["rungs"][-2]["visible"] if self.at else []
        rung.update(visible=list(visible), guides=[edge for edge in dict.fromkeys(guides) if edge not in visible],
                    selected=[edge for edge in previous if edge not in visible])

    def contain(self, key, container, edges, label):
        self.data["constraints"].append({"id": key, "at": self.at, "container": container,
                                          "edges": edges, "label": label})

    def ellipse(self, key, a, b):
        # Four finite cubic quarter profiles, not a claim of an analytic circle.
        corners = [(500, 0), (1000, 500), (500, 1000), (0, 500)]
        tips = [self.inside(f"{key}-tip-{i}", a, b, x, y) for i, (x, y) in enumerate(corners)]
        handles = [((776, 0), (1000, 224)), ((1000, 776), (776, 1000)),
                   ((224, 1000), (0, 776)), ((0, 224), (224, 0))]
        result = []
        for i, pair in enumerate(handles):
            h = [self.inside(f"{key}-handle-{i}-{j}", a, b, x, y)
                 for j, (x, y) in enumerate(pair)]
            result.append(self.edge(f"{key}-curve-{i}", [tips[i], *h, tips[(i + 1) % 4]]))
        return tips, result

    def variant(self, suffix, label, overrides=None):
        result = copy.deepcopy(self.data)
        result["id"] += "-" + suffix
        result["variantLabel"] = label
        for parameter in result["parameters"]:
            if parameter["id"] in (overrides or {}):
                parameter["value"] = overrides[parameter["id"]]
        return result


def church():
    r = Recipe("church-front", "A church from its construction frames", {
        "wallLeft": 92, "wallRight": 420, "wallTop": 216, "wallBottom": 438,
        "roofRise": 280, "towerHalfWidth": 95, "steepleTop": 48,
        "windowLeft": 130, "windowRight": 250, "windowTop": 170,
        "windowBottom": 520, "windowSpacing": 310, "archRise": 660})
    r.data["description"] = "Build from a wall, roof and tower frame; refine one window and repeat its actual construction. Compare valid proportions with an excessive-spacing trial."
    r.begin("Block in the main masses", "A wall rectangle, roof triangle and tower box establish the working frame. These are authored scaffolds.")
    r.frame()
    a = r.page("wall-tl", "wallLeft", "wallTop")
    b = r.page("wall-br", "wallRight", "wallBottom")
    wall, wall_edges = r.rectangle("wall", a, b)
    center = r.inside("wall-top-center", a, wall[1], 500, 0)
    peak = r.point("roof-peak", loc(a, wall[1], 500), loc(a, b, "roofRise", origin=a))
    # Reverse the vertical frame to make the roof rise above the wall.
    r.data["sites"][-1]["y"] = loc(b, a, "roofRise", origin=a)
    roof = r.poly("roof-guide", [a, peak, wall[1]])
    tower_l = r.point("tower-left-top", loc(wall[1], a, "towerHalfWidth", origin=center), loc(peak))
    tower_r = r.point("tower-right-bottom", loc(a, wall[1], "towerHalfWidth", origin=center), loc(a))
    tower, tower_edges = r.rectangle("tower-guide", tower_l, tower_r)
    mass = wall_edges + roof + tower_edges
    r.finish(mass)

    r.begin("Use the masses to place the silhouette", "Wall and tower sites guide the outline. Roof and tower scaffolds remain available after their guide strokes fade.")
    apex = r.point("steeple-tip", loc(center), loc("page-origin", "page-extent", "steepleTop", 512))
    cross_tip = r.point("cross-tip", loc(apex), loc(apex, "page-origin", 260))
    cross_join = r.inside("cross-joint", cross_tip, apex, 0, 420)
    cross_l = r.point("cross-left", loc(wall[1], a, 35, origin=cross_join), loc(cross_join))
    cross_r = r.point("cross-right", loc(a, wall[1], 35, origin=cross_join), loc(cross_join))
    roof_l = r.point("roof-at-left-tower", loc(tower_l), loc(a, peak, 760))
    roof_r = r.point("roof-at-right-tower", loc(tower_r), loc(a, peak, 760))
    outline_sites = [wall[3], a, roof_l, tower_l, apex, tower[1], roof_r, wall[1], b]
    shell = r.poly("silhouette", outline_sites)
    cross = r.poly("cross-stem", [apex, cross_join, cross_tip], False)
    cross += r.poly("cross-arm", [cross_l, cross_join, cross_r], False)
    silhouette = shell + cross
    r.finish(silhouette, mass)

    r.begin("Reserve one window area", "The window frame is positioned within the earlier wall frame. This rectangle is the source for the later arched opening.")
    wa = r.inside("window-tl", a, b, "windowLeft", "windowTop")
    wb = r.inside("window-br", a, b, "windowRight", "windowBottom")
    wc, window_rectangle = r.rectangle("window-box", wa, wb)
    r.finish(silhouette + window_rectangle, mass)
    r.contain("window-box-inside-wall", wall, window_rectangle, "Window frame inside the wall")

    r.begin("Round the window's top", "The old rectangle supplies the shoulders, lower corners and arch control. The straight top is replaced, with the old frame retained as a guide.")
    shoulder_l = r.inside("window-shoulder-left", wa, wb, 0, 340)
    shoulder_r = r.inside("window-shoulder-right", wa, wb, 1000, 340)
    control = r.point("window-arch-control", loc(wa, wb, 500), loc(wb, wa, "archRise", origin=shoulder_l))
    arch = [r.edge("window-left-side", [wc[3], shoulder_l]),
            r.edge("window-arch", [shoulder_l, control, shoulder_r]),
            r.edge("window-right-side", [shoulder_r, wb]),
            window_rectangle[2]]
    r.finish(silhouette + arch, mass + window_rectangle)
    r.contain("arched-window-inside-wall", wall, arch, "Complete window control hull inside the wall")

    r.begin("Repeat that construction three times", "Two new occurrences consume the first window's actual control sites and the wall's horizontal span. The shared spacing parameter moves each occurrence.")
    all_windows = list(arch)
    instances = [arch]
    source_edges = {edge["id"]: edge for edge in r.data["edges"]}
    previous_sites = {}
    for occurrence in (1, 2):
        mapping = {}
        for edge_id in arch:
            for source in source_edges[edge_id]["controls"]:
                if source in mapping:
                    continue
                prior = previous_sites.get(source, source)
                target = f"window-{occurrence + 1}-{source}"
                mapping[source] = r.point(target, loc(a, wall[1], "windowSpacing", origin=prior), loc(prior))
        copied = [r.edge(f"window-{occurrence + 1}-{edge_id}", [mapping[s] for s in source_edges[edge_id]["controls"]])
                  for edge_id in arch]
        all_windows += copied
        instances.append(copied)
        previous_sites = mapping
    r.data["repeats"].append({"id": "window-row", "at": r.at, "source": arch,
                              "instances": instances, "label": "Three occurrences of one window construction"})
    r.finish(silhouette + all_windows, mass + window_rectangle)
    r.contain("window-row-inside-wall", wall, all_windows, "All three window control hulls inside the wall")

    r.begin("Add subordinate details", "The door, belfry and rose opening are placed relative to the wall and tower, not independent page coordinates. The previous windows are carried unchanged.")
    door_l = r.inside("door-lower-left", a, b, 420, 1000)
    door_r = r.inside("door-lower-right", a, b, 580, 1000)
    door_sl = r.inside("door-shoulder-left", a, b, 420, 740)
    door_sr = r.inside("door-shoulder-right", a, b, 580, 740)
    door_control = r.inside("door-arch-control", a, b, 500, 570)
    door = [r.edge("door-left", [door_l, door_sl]), r.edge("door-arch", [door_sl, door_control, door_sr]),
            r.edge("door-right", [door_sr, door_r]), r.edge("door-base", [door_r, door_l])]
    door_middle_bottom = r.inside("door-bottom-middle", door_l, door_r, 500, 0)
    door_middle_top = r.inside("door-arch-middle", door_sl, door_control, 1000, 500)
    door.append(r.edge("door-seam", [door_middle_bottom, door_middle_top]))
    rose_a = r.inside("rose-frame-tl", tower_l, tower_r, 220, 220)
    rose_b = r.inside("rose-frame-br", tower_l, tower_r, 780, 620)
    _, rose = r.ellipse("tower-opening", rose_a, rose_b)
    r.finish(silhouette + all_windows + door + rose, mass + window_rectangle)
    r.contain("finished-window-row-inside-wall", wall, all_windows, "Three windows remain within the wall")
    return [r.variant("balanced", "Balanced facade"),
            r.variant("tall", "Tall steeple", {"steepleTop": 20, "wallLeft": 112, "wallRight": 400, "towerHalfWidth": 80}),
            r.variant("wide", "Broad wall and wider windows", {"wallLeft": 56, "wallRight": 456, "windowLeft": 100, "windowRight": 260, "windowSpacing": 320}),
            r.variant("outside-trial", "Rejected spacing trial", {"windowSpacing": 440})]


def star(points, inset=460):
    r = Recipe("partition-star", "A star from a partitioned circular construction", {"inset": inset, "radius": 180})
    r.data["description"] = "A circular whole, equal partitions, alternating reaches and ordered joins produce the star. Its measured marker positions retain a finite calibration."
    r.begin("Choose the center and common reach", "The rectangular bounds display a circular construction's measured frame. Cubic guide curves are finite approximations.")
    r.frame()
    center = r.page("center", 256, 256)
    right = r.point("radius-reference", loc("page-origin", "page-extent", "radius", 512, origin=center), loc(center))
    bottom = r.point("radius-down", loc(center), loc("page-origin", "page-extent", "radius", 512, origin=center))
    a = r.point("circle-tl", loc(right, center, 1000, origin=center), loc(bottom, center, 1000, origin=center))
    b = r.point("circle-br", loc(right), loc(bottom))
    _, circle = r.ellipse("circle-guide", a, b)
    r.finish(circle)
    r.begin("Partition the circular traversal", f"The whole traversal has {points * 2} equal partitions. XY marker placement uses an explicit finite sampled calibration; arbitrary-angle exact geometry is not claimed.")
    ring = []
    spokes = []
    for i in range(points * 2):
        angle = -math.pi / 2 + i * math.pi / points
        marker = r.point(f"outer-marker-{i}", loc(center, right, round(math.cos(angle) * 1000000), 1000000),
                         loc(center, bottom, round(math.sin(angle) * 1000000), 1000000))
        ring.append(marker)
        spokes.append(r.edge(f"radial-guide-{i}", [center, marker]))
    r.data["circular"].append({"id": "whole-partition", "at": r.at, "center": center, "ring": ring,
                                "label": f"Closed circular traversal with {points * 2} equal partitions; sampled XY calibration"})
    r.finish(circle + spokes)
    r.begin("Alternate insets and outsets", "Every inset is a shared fraction of its earlier radial reach. Outer markers retain their actual identity.")
    tips, inset_guides = [], []
    for i, marker in enumerate(ring):
        tip = marker if i % 2 == 0 else r.point(f"inset-{i}", loc(center, marker, "inset"), loc(center, marker, "inset"))
        tips.append(tip)
        inset_guides.append(r.edge(f"selected-radius-{i}", [center, tip]))
    r.finish(inset_guides, circle + spokes)
    r.begin("Join the selected sites", "Ordered joins return to the same first tip. Changing the inset changes the construction; changing partition count creates a different program variant.")
    outline = r.poly("star-outline", tips)
    r.finish(outline, circle + spokes + inset_guides)
    return r.variant(f"{points}-{inset}", f"{points} points · inset {inset / 1000:g}")


def crescent():
    r = Recipe("crescent", "A crescent from an oval envelope", {"left": 116, "right": 396, "innerBend": 420})
    r.data["description"] = "Refine an oval and wedge into two curved boundaries with shared tips. Reverse the horizontal frame to preserve the opposite facing."
    r.begin("Place the oval envelope", "The two signed horizontal endpoints retain facing. Reversing the frame mirrors later dependent strokes.")
    r.frame()
    a = r.page("envelope-a", "left", 76)
    b = r.page("envelope-b", "right", 436)
    _, envelope = r.ellipse("oval-guide", a, b)
    r.finish(envelope)
    r.begin("Select shared tips and a coarse wedge", "A closed triangular approximation establishes the two tips and the side carrying the crescent's mass.")
    top = r.inside("top-tip", a, b, 640, 40)
    bottom = r.inside("bottom-tip", a, b, 640, 960)
    outer = r.inside("outer-middle", a, b, 0, 500)
    wedge = r.poly("coarse-wedge", [top, outer, bottom])
    r.finish(wedge, envelope)
    r.begin("Bow the outer boundary", "The coarse wedge is replaced by a cubic contour. Its endpoints retain shared identity; old chords remain as guides.")
    h1 = r.inside("outer-top-control", a, b, -210, 0)
    h2 = r.inside("outer-bottom-control", a, b, -210, 1000)
    outer_curve = r.edge("outer-boundary", [top, h1, h2, bottom])
    r.finish([outer_curve, wedge[2]], envelope + wedge)
    r.begin("Refine the inner curve", "The inner profile consumes the same tips and a shared bend parameter. This is an authored crescent family, not inferred moon intent.")
    i1 = r.inside("inner-bottom-control", a, b, "innerBend", 740)
    i2 = r.inside("inner-top-control", a, b, "innerBend", 260)
    inner = r.edge("inner-boundary", [bottom, i1, i2, top])
    r.finish([outer_curve, inner], envelope + wedge)
    return [r.variant("left", "Left-side mass / opening right"),
            r.variant("right", "Right-side mass / opening left", {"left": 396, "right": 116}),
            r.variant("slender", "Slender crescent", {"innerBend": 60})]


def organic():
    r = Recipe("organic-contour", "An organic contour from a coarse envelope", {"bulge": 170, "top": 100, "bottom": 410})
    r.data["description"] = "Start with an envelope and closed landmark polygon, round the spans, then change one bend while carrying the other curves forward."
    r.begin("Set the enclosing rectangle", "A rough rectangle establishes relative locations for a soft closed shape.")
    r.frame()
    a = r.page("envelope-tl", 102, "top")
    b = r.page("envelope-br", 410, "bottom")
    _, box = r.rectangle("envelope", a, b)
    r.finish(box)
    r.begin("Mark the main directional changes", "Five landmarks belong to the earlier envelope. Their closed polygon is already a valid coarse drawing.")
    fractions = [(450, 0), (940, 220), (1000, 700), (480, 1000), (0, 620)]
    sites = [r.inside(f"landmark-{i}", a, b, x, y) for i, (x, y) in enumerate(fractions)]
    chords = r.poly("coarse-contour", sites)
    r.finish(chords, box)
    r.begin("Round each span using its local frame", "Local handles consume the earlier landmarks. Endpoint identity and closed traversal are retained while the straight profiles are replaced.")
    curves = []
    for i, start in enumerate(sites):
        end = sites[(i + 1) % len(sites)]
        previous = sites[(i - 1) % len(sites)]
        following = sites[(i + 2) % len(sites)]
        h1 = r.point(f"handle-{i}-a", loc(previous, end, 170, origin=start), loc(previous, end, 170, origin=start))
        h2 = r.point(f"handle-{i}-b", loc(following, start, 170, origin=end), loc(following, start, 170, origin=end))
        curves.append(r.edge(f"soft-contour-{i}", [start, h1, h2, end]))
    r.finish(curves, box + chords)
    r.begin("Refine one local bend", "Only the upper-right span changes. Four other curve identities are carried forward; the replacement still consumes the original endpoints.")
    start, end = sites[1:3]
    h1 = r.point("refined-handle-a", loc(a, b, "bulge", origin=start), loc(start, end, 180))
    h2 = r.point("refined-handle-b", loc(a, b, "bulge", origin=end), loc(start, end, 720))
    refined = r.edge("refined-bend", [start, h1, h2, end])
    final = [curves[0], refined, *curves[2:]]
    r.finish(final, box + chords + [curves[1]])
    return [r.variant("soft", "Soft asymmetric contour"), r.variant("tall", "Tall contour · stronger bend", {"top": 56, "bottom": 454, "bulge": 230})]


def build():
    return [*church(), star(5), star(7), star(5, 700), *crescent(), *organic()]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    programs = build()
    (args.output / "programs.ndjson").write_text("".join(json.dumps(p, separators=(",", ":")) + "\n" for p in programs), encoding="utf-8")
    manifest = {"schema": "gojo.drawing-construction-manifest.v1", "families": 4,
                "runs": len(programs), "stages": sum(len(p["rungs"]) for p in programs),
                "description": "Authored executable numeric locators, topology and refinement; no final-coordinate oracle or learned inference.",
                "programs": [{"id": p["id"], "family": p["family"], "title": p["title"], "variant": p["variantLabel"],
                              "rungs": len(p["rungs"]), "sites": len(p["sites"]), "edges": len(p["edges"])} for p in programs]}
    (args.output / "source-manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"families": 4, "runs": len(programs), "stages": manifest["stages"]}))


if __name__ == "__main__":
    main()
