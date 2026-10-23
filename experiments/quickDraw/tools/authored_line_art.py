"""Known-construction vector fixtures for native evidence inspection.

These are authored fixtures, not recovered Quick Draw intentions or trained
category templates. JSON is an adapter into native relation structure. SVG is
a plain black, unfilled render of the same edges; semantic labels stay outside
the drawing. Equal coordinates are never automatically merged into one site.
"""
from __future__ import annotations

import argparse
import html
import json
from pathlib import Path


class Scene:
    def __init__(self, key, title, note=""):
        self.data = {"id": key, "title": title, "viewBox": [0, 0, 512, 512], "coordinateQuantum": 1,
                     "status": "authored construction fixture", "notes": note,
                     "drawOrder": "ordered authored primitive construction; not recorded human pen order",
                     "source": {"kind": "new analytic vector artwork", "generator": "authored_line_art.py",
                                "trainingUse": "none; diagnostic fixture, not a Quick Draw ideal target"},
                     "sites": [], "edges": [], "strokes": [], "objects": [], "groups": [],
                     "regions": [], "correspondences": [], "crossings": [], "contacts": [],
                     "expectedJointForms": [],
                     "qualifications": ["Object, template/detail and region membership are authored premises.",
                                        "Unfilled line art does not establish occupied material or figure/ground by itself.",
                                        "Contact preserves distinct site/object identity; no load-bearing law is claimed."]}
        self.sites, self.edges, self.objects, self.groups = {}, {}, {}, {}

    def site(self, key, x, y):
        value = (x, y)
        if key in self.sites:
            if self.sites[key] != value:
                raise ValueError(f"Site {key} cannot silently move")
            return key
        if not all(isinstance(v, int) and not isinstance(v, bool) for v in value):
            raise ValueError("Authored fixture coordinates must be integers")
        self.sites[key] = value
        self.data["sites"].append({"id": key, "x": x, "y": y})
        return key

    def edge(self, key, start, end, interior=(), obj="church", group="outline", role="template"):
        if key in self.edges:
            raise ValueError(f"Duplicate edge {key}")
        controls = [list(self.sites[start]), *[list(p) for p in interior], list(self.sites[end])]
        if not 2 <= len(controls) <= 4:
            raise ValueError("Only line, quadratic and cubic primitives are admitted")
        row = {"id": key, "from": start, "to": end, "controls": controls}
        self.edges[key] = row
        self.data["edges"].append(row)
        self.data["strokes"].append({"id": f"step-{len(self.data['strokes'])}", "edges": [key]})
        if obj not in self.objects:
            self.objects[obj] = {"id": obj, "label": obj.replace("-", " "), "edges": []}
            self.data["objects"].append(self.objects[obj])
        self.objects[obj]["edges"].append(key)
        if group not in self.groups:
            self.groups[group] = {"id": group, "label": group.replace("-", " "), "role": role, "edges": []}
            self.data["groups"].append(self.groups[group])
        if self.groups[group]["role"] != role:
            raise ValueError("A group cannot silently change template/detail context")
        self.groups[group]["edges"].append(key)
        return key

    def poly(self, key, points, closed=True, obj="church", role="template", group=None):
        ids = [self.site(f"{key}-s{i}", *p) for i, p in enumerate(points)]
        pairs = list(zip(ids, ids[1:])) + ([(ids[-1], ids[0])] if closed else [])
        edges = [self.edge(f"{key}-e{i}", a, b, obj=obj, group=group or key, role=role)
                 for i, (a, b) in enumerate(pairs)]
        return ids, edges

    def region(self, key, edges, figure=True, parent=None):
        boundary = [{"edge": e, "forward": True} if isinstance(e, str) else e for e in edges]
        row = {"id": key, "boundary": boundary, "figure": figure, "status": "authored region participation"}
        if parent:
            row["parent"] = parent
        self.data["regions"].append(row)
        return key

    def circle(self, key, cx, cy, r, role="detail", obj="church", figure=False, parent="wall-region"):
        # Four cubic arcs with integer controls. This is the exact authored
        # curve, not a claim of an exact analytic Euclidean circle.
        k = round(r * .5522847498)
        points = [(cx, cy-r), (cx+r, cy), (cx, cy+r), (cx-r, cy)]
        ids = [self.site(f"{key}-s{i}", *p) for i, p in enumerate(points)]
        controls = [[(cx+k, cy-r), (cx+r, cy-k)], [(cx+r, cy+k), (cx+k, cy+r)],
                    [(cx-k, cy+r), (cx-r, cy+k)], [(cx-r, cy-k), (cx-k, cy-r)]]
        edges = [self.edge(f"{key}-e{i}", ids[i], ids[(i+1) % 4], controls[i], obj=obj, group=key, role=role)
                 for i in range(4)]
        self.region(f"{key}-region", edges, figure, parent)
        return edges

    def arch(self, key, cx, bottom, width, height, role="detail", obj="church", parent="wall-region", pointed=False):
        left, right, shoulder = cx-width//2, cx+width//2, bottom-height//2
        ids = [self.site(f"{key}-s{i}", *p) for i, p in enumerate(
               [(left, bottom), (left, shoulder), (right, shoulder), (right, bottom)])]
        edges = [self.edge(f"{key}-e0", ids[0], ids[1], obj=obj, group=key, role=role)]
        if pointed:
            tip = self.site(f"{key}-tip", cx, bottom-height)
            edges.extend((self.edge(f"{key}-e1a", ids[1], tip, [(left, bottom-height+8)], obj=obj, group=key, role=role),
                          self.edge(f"{key}-e1b", tip, ids[2], [(right, bottom-height+8)], obj=obj, group=key, role=role)))
        else:
            edges.append(self.edge(f"{key}-e1", ids[1], ids[2], [(cx, bottom-height)], obj=obj, group=key, role=role))
        edges.extend((self.edge(f"{key}-e2", ids[2], ids[3], obj=obj, group=key, role=role),
                      self.edge(f"{key}-e3", ids[3], ids[0], obj=obj, group=key, role=role)))
        self.region(f"{key}-region", edges, False, parent)
        return edges

    def repeat(self, key, source, target, dx, dy):
        self.data["correspondences"].append({"id": key, "kind": "repeat", "source": source,
                    "target": target, "translation": [dx, dy], "reversed": [False]*len(source),
                    "status": "authored repeated construction with exact control correspondence"})

    def mirror(self, key, center=256, selection=None):
        selected = list(selection or self.edges)
        used, source, target, reversed_flags = set(), [], [], []
        for edge_id in selected:
            if edge_id in used:
                continue
            controls = [[2*center-x, y] for x, y in self.edges[edge_id]["controls"]]
            for other_id in selected:
                other = self.edges[other_id]["controls"]
                if other_id not in used and (controls == other or controls == other[::-1]):
                    source.append(edge_id)
                    target.append(other_id)
                    reversed_flags.append(controls != other)
                    used.update((edge_id, other_id))
                    break
        if source:
            self.data["correspondences"].append({"id": key, "kind": "mirror", "source": source, "target": target,
                "axis": [[center, 0], [center, 512]], "reversed": reversed_flags,
                "status": "authored axis; exact correspondence calculated from authored controls",
                "scope": "paired edge participations only; unmatched edges remain outside this claim",
                "unmatched": [e for e in selected if e not in used]})

    def finish(self):
        validate(self.data)
        return self.data


def front(scene, upper, left=116, right=396, door_width=64, door_height=108, door_center=256, pointed=False):
    """Shared base sites make door contacts actual authored T junctions."""
    bottom, dl, dr = 440, door_center-door_width//2, door_center+door_width//2
    points = [(left, bottom), (dl, bottom), (dr, bottom), (right, bottom), *upper]
    sites, edges = scene.poly("outline", points)
    scene.region("wall-region", edges, True)
    shoulder = bottom-door_height//2
    a = scene.site("door-left-shoulder", dl, shoulder)
    b = scene.site("door-right-shoulder", dr, shoulder)
    door = [scene.edge("door-left", sites[1], a, group="door", role="detail")]
    if pointed:
        tip = scene.site("door-tip", door_center, bottom-door_height)
        door += [scene.edge("door-arch-left", a, tip, [(dl, bottom-door_height+10)], group="door", role="detail"),
                 scene.edge("door-arch-right", tip, b, [(dr, bottom-door_height+10)], group="door", role="detail")]
    else:
        door.append(scene.edge("door-arch", a, b, [(door_center, bottom-door_height)], group="door", role="detail"))
    door.append(scene.edge("door-right", b, sites[2], group="door", role="detail"))
    scene.region("door-opening", door + [{"edge": edges[1], "forward": False}], False, "wall-region")
    return sites, edges


def cross(scene, stem_site, prefix="cross", height=52, width=36, role="template"):
    x, y = scene.sites[stem_site]
    center = scene.site(f"{prefix}-center", x, y-height//2)
    top = scene.site(f"{prefix}-top", x, y-height)
    left = scene.site(f"{prefix}-left", x-width//2, y-height//2)
    right = scene.site(f"{prefix}-right", x+width//2, y-height//2)
    edges = [scene.edge(f"{prefix}-stem", stem_site, center, group=prefix, role=role),
             scene.edge(f"{prefix}-top-arm", center, top, group=prefix, role=role),
             scene.edge(f"{prefix}-left-arm", center, left, group=prefix, role=role),
             scene.edge(f"{prefix}-right-arm", center, right, group=prefix, role=role)]
    return edges


def paired_windows(scene, left=176, right=336, bottom=366, width=34, height=74, pointed=False):
    a = scene.arch("window-left", left, bottom, width, height, pointed=pointed)
    b = scene.arch("window-right", right, bottom, width, height, pointed=pointed)
    scene.repeat("window-translation", a, b, right-left, 0)
    return a+b


def church_scenes():
    scenes = []
    # Facades have materially different topologies, proportions and component
    # choices. This is a fixture repertoire; broad style/family labels are not
    # learning targets or a claim of historical architectural classification.
    s = Scene("church-01", "Small gabled chapel", "Shared doorway/base T joints; repeated arched windows.")
    sites, _ = front(s, [(396, 278), (256, 176), (116, 278)])
    cross(s, sites[5]); paired_windows(s); s.circle("round-window", 256, 272, 24)
    s.mirror("facade-axis"); scenes.append(s.finish())

    s = Scene("church-02", "Central bell tower", "Stepped tower and roof outline; paired windows remain repeated parts.")
    sites, _ = front(s, [(402, 294), (304, 224), (304, 142), (256, 94), (208, 142), (208, 224), (110, 294)], left=110, right=402)
    cross(s, sites[7], height=44, width=32)
    s.arch("belfry", 256, 216, 42, 62)
    paired_windows(s, 170, 342, 366, 32, 68)
    s.mirror("tower-axis"); scenes.append(s.finish())

    s = Scene("church-03", "Church with a side tower", "Asymmetric outline; local window repetition does not imply whole-scene symmetry.")
    sites, _ = front(s, [(412, 270), (292, 190), (190, 270), (190, 148), (142, 96), (94, 148)], left=94, right=412, door_center=292)
    cross(s, sites[8], height=42, width=30)
    s.arch("tower-window", 142, 252, 34, 76)
    a = s.arch("window-a", 228, 360, 30, 68)
    b = s.arch("window-b", 356, 360, 30, 68)
    s.repeat("nave-windows", a, b, 128, 0)
    s.mirror("local-door-axis", 292, s.groups["door"]["edges"])
    scenes.append(s.finish())

    s = Scene("church-04", "Twin towers", "Two matching towers and a central gable; mirrored shared outline edges.")
    sites, _ = front(s, [(428, 186), (386, 126), (344, 186), (344, 268), (256, 208),
                        (168, 268), (168, 186), (126, 126), (84, 186)], left=84, right=428)
    cross(s, sites[5], "cross-right", 44, 26); cross(s, sites[11], "cross-left", 44, 26)
    a = s.arch("tower-left-opening", 126, 286, 34, 68)
    b = s.arch("tower-right-opening", 386, 286, 34, 68)
    s.repeat("tower-openings", a, b, 260, 0)
    s.circle("rose-window", 256, 286, 27)
    s.mirror("twin-tower-axis"); scenes.append(s.finish())

    s = Scene("church-05", "Domed chapel", "Curved roof with explicit cubic controls; no exact-circle assertion.")
    sites, edges = front(s, [(390, 272), (330, 272), (330, 210), (256, 136), (182, 210), (182, 272), (122, 272)], left=122, right=390)
    # Replace two diagonal dome sides by authored cubic arcs; endpoint identity
    # and boundary membership stay unchanged.
    s.edges[edges[6]]["controls"] = [[330, 210], [330, 166], [292, 136], [256, 136]]
    s.edges[edges[7]]["controls"] = [[256, 136], [220, 136], [182, 166], [182, 210]]
    cross(s, sites[7], height=46, width=30)
    paired_windows(s, 170, 342, 374, 34, 70)
    s.circle("drum-window", 256, 238, 23)
    s.mirror("dome-axis"); scenes.append(s.finish())

    s = Scene("church-06", "Wide church with three upper windows", "An explicit repeated row uses separate identities and translations.")
    sites, _ = front(s, [(430, 294), (346, 252), (346, 204), (256, 146), (166, 204), (166, 252), (82, 294)], left=82, right=430)
    cross(s, sites[7], height=46, width=32)
    rows = [s.arch(f"clerestory-{i}", x, 274, 28, 48) for i, x in enumerate((212, 256, 300))]
    s.repeat("clerestory-step-1", rows[0], rows[1], 44, 0)
    s.repeat("clerestory-step-2", rows[1], rows[2], 44, 0)
    paired_windows(s, 142, 370, 382, 36, 66)
    s.mirror("wide-facade-axis"); scenes.append(s.finish())

    s = Scene("church-07", "Chapel with a low annex", "The annex deliberately breaks whole-building reflection; the doorway retains local reflection.")
    sites, _ = front(s, [(438, 338), (366, 300), (366, 256), (240, 164), (114, 256)], left=114, right=438, door_center=240)
    cross(s, sites[7], height=50, width=34)
    a = s.arch("front-window", 166, 368, 32, 72)
    b = s.arch("annex-window", 402, 400, 32, 48)
    s.circle("round-window", 240, 272, 23)
    s.mirror("door-local-axis", 240, s.groups["door"]["edges"])
    scenes.append(s.finish())

    s = Scene("church-08", "Tall pointed facade", "Pointed quadratic arches keep their cusp sites; a circular opening remains a closed cycle.")
    sites, _ = front(s, [(376, 286), (256, 100), (136, 286)], left=136, right=376, door_width=70, door_height=124, pointed=True)
    cross(s, sites[5], height=48, width=30)
    paired_windows(s, 180, 332, 382, 30, 92, pointed=True)
    s.circle("rose-window", 256, 238, 30)
    s.mirror("pointed-axis"); scenes.append(s.finish())

    s = Scene("church-09", "Chapel with a curved apse", "A side-view construction includes curved outline support and asymmetry.")
    sites, edges = front(s, [(430, 312), (356, 252), (176, 252), (176, 172), (138, 122), (100, 172)], left=100, right=430, door_center=202, door_width=52, door_height=94)
    s.edges[edges[3]]["controls"] = [[430, 440], [468, 404], [468, 348], [430, 312]]
    cross(s, sites[8], height=42, width=28)
    s.arch("belfry", 138, 246, 28, 56)
    a = s.arch("nave-window-a", 278, 374, 34, 72)
    b = s.arch("nave-window-b", 350, 374, 34, 72)
    s.repeat("side-windows", a, b, 72, 0)
    scenes.append(s.finish())

    s = Scene("church-10", "Simple timber chapel", "Roof braces meet at shared sites; structural template and surface details are separate authored scopes.")
    sites, edges = front(s, [(398, 288), (256, 182), (114, 288)], left=114, right=398)
    cross(s, sites[5], height=48, width=32)
    midpoint = s.site("roof-base-mid", 256, 288)
    # These aliases are actual reused silhouette sites, not coordinate merging.
    roof_left, roof_right = sites[6], sites[4]
    s.edge("roof-base-left-half", roof_left, midpoint, group="timber-bracing", role="detail")
    s.edge("roof-base-right-half", midpoint, roof_right, group="timber-bracing", role="detail")
    s.edge("roof-center-brace", sites[5], midpoint, group="timber-bracing", role="detail")
    paired_windows(s, 176, 336, 374, 34, 66)
    s.mirror("timber-axis"); scenes.append(s.finish())

    s = Scene("church-11", "Compact church with square windows", "Straight window frames contrast with the curved doorway; repeated frames share no site identity.")
    sites, _ = front(s, [(386, 282), (302, 220), (302, 164), (256, 116), (210, 164), (210, 220), (126, 282)], left=126, right=386)
    cross(s, sites[7], height=44, width=30)
    a_sites, a = s.poly("square-window-left", [(158, 324), (194, 324), (194, 366), (158, 366)], role="detail")
    b_sites, b = s.poly("square-window-right", [(318, 324), (354, 324), (354, 366), (318, 366)], role="detail")
    s.region("square-window-left-region", a, False, "wall-region")
    s.region("square-window-right-region", b, False, "wall-region")
    s.repeat("square-window-repeat", a, b, 160, 0)
    s.arch("tower-opening", 256, 220, 34, 58)
    s.mirror("compact-axis"); scenes.append(s.finish())

    s = Scene("church-12", "A-frame chapel", "A sparse triangular shell with a curved portal; fewer parts does not imply a learned canonical church.")
    sites, _ = front(s, [(256, 108)], left=84, right=428, door_width=76, door_height=122)
    cross(s, sites[4], height=48, width=32)
    a = s.poly("diamond-window-left", [(170, 324), (190, 304), (210, 324), (190, 344)], role="detail")[1]
    b = s.poly("diamond-window-right", [(302, 324), (322, 304), (342, 324), (322, 344)], role="detail")[1]
    s.region("diamond-left-region", a, False, "wall-region")
    s.region("diamond-right-region", b, False, "wall-region")
    s.repeat("diamond-window-repeat", a, b, 132, 0)
    s.mirror("a-frame-axis"); scenes.append(s.finish())
    return scenes


def diagnostics():
    scenes = []
    s = Scene("diagnostic-rope-branch", "A rope attached to a branch", "A T-shaped image combines distinct branch and rope objects. Their contact does not weld their sites or imply one object.")
    a = s.site("branch-left", 74, 140); b = s.site("branch-contact", 256, 140); c = s.site("branch-right", 438, 140)
    s.edge("branch-left-piece", a, b, obj="branch", group="branch", role="template")
    s.edge("branch-right-piece", b, c, obj="branch", group="branch", role="template")
    d = s.site("branch-fork", 400, 84)
    s.edge("branch-fork-piece", c, d, obj="branch", group="branch", role="template")
    r = s.site("rope-contact", 256, 140); bottom = s.site("rope-end", 270, 416)
    s.edge("hanging-rope", r, bottom, [(256, 228), (282, 328)], obj="rope", group="rope", role="template")
    s.data["contacts"].append({"sites": [b, r], "status": "authored attachment; no load-bearing mechanics"})
    s.data["expectedJointForms"].append({"site": b, "form": "straight-through", "qualification": "T is only a projected composite; the rope owns a distinct contact site"})
    scenes.append(s.finish())

    s = Scene("diagnostic-crossing-join", "Crossing and joined X", "Equal projected X shapes have different topology: two independent paths on the left, one shared four-ray site on the right.")
    _, a = s.poly("left-path-a", [(76, 188), (204, 316)], False, "wire-a")
    _, b = s.poly("left-path-b", [(76, 316), (204, 188)], False, "wire-b")
    s.data["crossings"].append({"edges": [a[0], b[0]], "site": [140, 252], "status": "authored projected crossing; over-under unresolved"})
    center = s.site("joined-center", 372, 252)
    for i, point in enumerate(((308, 188), (436, 316), (308, 316), (436, 188))):
        endpoint = s.site(f"joined-end-{i}", *point)
        s.edge(f"joined-ray-{i}", center, endpoint, obj="joined-frame", group="joined-frame")
    s.data["expectedJointForms"].append({"site": center, "form": "X", "qualification": "four actual incident rays; two opposite pairs"})
    scenes.append(s.finish())

    s = Scene("diagnostic-joint-atlas", "T Y L O X K construction atlas", "Letter names describe this declared visual convention. O denotes a closed cycle, not a guessed degree-two joint; K has four rays with exactly one opposite pair.")
    layouts = {"T": ((86, 132), [(-48, 0), (48, 0), (0, 64)]),
               "Y": ((256, 132), [(-42, -48), (42, -48), (0, 64)]),
               "L": ((426, 132), [(-54, 0), (0, 64)]),
               "X": ((256, 366), [(-48, -48), (48, 48), (-48, 48), (48, -48)]),
               "K": ((414, 366), [(0, -64), (0, 64), (52, -42), (52, 42)])}
    for form, (point, rays) in layouts.items():
        center = s.site(f"{form}-center", *point)
        for i, (dx, dy) in enumerate(rays):
            end = s.site(f"{form}-end-{i}", point[0]+dx, point[1]+dy)
            s.edge(f"{form}-ray-{i}", center, end, obj=f"glyph-{form}", group=f"glyph-{form}")
        s.data["expectedJointForms"].append({"site": center, "form": form, "status": "authored fixture expectation; native reader must calculate"})
    s.circle("O-loop", 86, 366, 46, "template", "glyph-O", True, None)
    s.data["expectedJointForms"].append({"region": "O-loop-region", "form": "O", "status": "explicit closed cycle"})
    scenes.append(s.finish())
    return scenes


def validate(scene):
    def indexed(rows, name):
        result = {r["id"]: r for r in rows}
        if len(result) != len(rows):
            raise ValueError(f"Duplicate {name} identity")
        return result
    sites, edges = indexed(scene["sites"], "site"), indexed(scene["edges"], "edge")
    for edge in edges.values():
        controls = edge["controls"]
        if not 2 <= len(controls) <= 4:
            raise ValueError("Invalid primitive degree")
        for key, point in (("from", controls[0]), ("to", controls[-1])):
            site = sites[edge[key]]
            if point != [site["x"], site["y"]]:
                raise ValueError("Control endpoint disagrees with declared site")
        if any(not isinstance(v, int) or isinstance(v, bool) or not 0 <= v <= 512 for p in controls for v in p):
            raise ValueError("Fixture control outside declared integer viewport")
    for scope in ("objects", "groups"):
        indexed(scene[scope], scope)
        for row in scene[scope]:
            if any(e not in edges for e in row["edges"]):
                raise ValueError("Scope references an absent edge")
    seen_regions = set()
    for region in scene["regions"]:
        if region.get("parent") and region["parent"] not in seen_regions:
            raise ValueError("Region parent must already exist")
        oriented = []
        for reference in region["boundary"]:
            edge = edges[reference["edge"]]
            ends = [edge["from"], edge["to"]]
            oriented.append(ends if reference["forward"] else ends[::-1])
        if not oriented or any(a[1] != b[0] for a, b in zip(oriented, oriented[1:]+oriented[:1])):
            raise ValueError("Region cycle is not closed through actual shared endpoint identities")
        seen_regions.add(region["id"])
    for relation in scene["correspondences"]:
        if len(relation["source"]) != len(relation["target"]):
            raise ValueError("Unequal correspondence inventories")
        reverse = relation.get("reversed", [False]*len(relation["source"]))
        if len(reverse) != len(relation["source"]):
            raise ValueError("Unequal reversal inventory")
        for a, b, reversed_ in zip(relation["source"], relation["target"], reverse):
            source = edges[a]["controls"]
            if relation["kind"] == "mirror":
                axis = relation["axis"]
                if axis[0][0] != axis[1][0]:
                    raise ValueError("This authored adapter supports vertical mirror axes only")
                transformed = [[2*axis[0][0]-x, y] for x, y in source]
            elif relation["kind"] == "repeat":
                dx, dy = relation["translation"]
                transformed = [[x+dx, y+dy] for x, y in source]
            else:
                raise ValueError("Unknown authored transform contract")
            target = edges[b]["controls"][::-1] if reversed_ else edges[b]["controls"]
            if transformed != target:
                raise ValueError("Authored correspondence fails exact control comparison")
    for contact in scene["contacts"]:
        a, b = contact["sites"]
        if a == b or a not in sites or b not in sites:
            raise ValueError("Contact needs two existing distinct sites")
    declared_order = [e for stroke in scene["strokes"] for e in stroke["edges"]]
    if declared_order != list(edges):
        raise ValueError("Construction steps must expose the authored primitive order exactly")
    return True


def svg_path(controls):
    text = f"M{controls[0][0]} {controls[0][1]}"
    command = {2: "L", 3: "Q", 4: "C"}[len(controls)]
    return text + command + " ".join(str(v) for p in controls[1:] for v in p)


def svg(scene):
    paths = "".join(f'<path id="{html.escape(e["id"], quote=True)}" d="{svg_path(e["controls"])}"/>' for e in scene["edges"])
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512" role="img">'
            f'<title>{html.escape(scene["title"])}</title>'
            '<g fill="none" stroke="#000" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">'
            + paths + '</g></svg>')


def render_overview(scenes, path, columns=3):
    from PIL import Image, ImageDraw, ImageFont
    from fit_pilot import bezier
    factor, width, height = 2, 360, 390
    rows = (len(scenes)+columns-1)//columns
    image = Image.new("RGB", (width*columns*factor, height*rows*factor), "white")
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16*factor)
    for i, scene in enumerate(scenes):
        left, top = (i % columns)*width, (i//columns)*height
        for edge in scene["edges"]:
            points = [bezier(edge["controls"], j/64) for j in range(65)]
            screen = [((left+20+x*.625)*factor, (top+20+y*.625)*factor) for x, y in points]
            draw.line(screen, fill="black", width=2*factor)
        draw.text(((left+16)*factor, (top+352)*factor), scene["title"], fill="#17212b", font=font)
        draw.line(((left+4)*factor, (top+height-1)*factor, (left+width-4)*factor, (top+height-1)*factor), fill="#dce0e5", width=factor)
    image.resize((width*columns, height*rows), Image.Resampling.LANCZOS).save(path)


def run(output):
    output.mkdir(parents=True, exist_ok=True)
    scenes = church_scenes()+diagnostics()
    with (output / "scenes.ndjson").open("w", encoding="utf-8", newline="\n") as stream:
        for scene in scenes:
            stream.write(json.dumps(scene, separators=(",", ":")) + "\n")
            (output / f'{scene["id"]}.svg').write_text(svg(scene), encoding="utf-8")
    manifest = {"schema": "gojo.authored-line-art.v1", "status": "authored fixtures, no model training",
                "churches": 12, "diagnostics": 3, "drawOrder": "authored construction order, not human acquisition",
                "counts": [{"id": s["id"], "sites": len(s["sites"]), "edges": len(s["edges"]),
                            "regions": len(s["regions"]), "correspondences": len(s["correspondences"])} for s in scenes]}
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2)+"\n", encoding="utf-8")
    # A static authoring review only. The separately built inspector consumes
    # native restored output and owns interactive evidence overlays.
    cards = "".join(f'<article><h2>{html.escape(s["title"])}</h2>{svg(s)}<p>{html.escape(s["notes"])}</p></article>' for s in scenes)
    page = '<!doctype html><meta charset="utf-8"><title>Authored line art source review</title><style>body{font:16px system-ui;margin:28px;color:#15212b;background:#f4f5f6}main{display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:18px}article{background:white;padding:18px;border:1px solid #ccd2d8;border-radius:8px}h2{font-size:17px}svg{width:100%;max-height:350px}p{line-height:1.45;font-size:14px}</style><h1>Known-construction line art</h1><p>Twelve authored churches and three diagnostics. Black lines are the source artwork. Object and construction annotations are authored premises; native readback supplies calculated evidence separately.</p><main>'+cards+'</main>'
    (output / "source-review.html").write_text(page, encoding="utf-8")
    render_overview(scenes[:12], output / "church-overview.png")
    render_overview(scenes[12:], output / "diagnostic-overview.png")
    print(json.dumps({"scenes": len(scenes), "edges": sum(len(s["edges"]) for s in scenes), "output": str(output)}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=Path("D:/_quickDraw/gojo-line-art"))
    run(parser.parse_args().output)
