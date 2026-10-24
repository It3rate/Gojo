"""Compose source artwork through earlier primitive construction frames.

These authoring helpers emit the same native affine site/curve program as the
pilot. Source mappings are external correspondence evidence for verification;
native execution consumes only its connected coordinate and topology rows.
Rough frames carry no implicit containment or occupied-material claim.
"""
from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass

from construction_recipes import Recipe, loc


@dataclass
class Frame:
    a: str
    b: str
    bounds: tuple[int, int, int, int]
    edges: list[str]
    corners: list[str]


def source_digest(scene):
    return hashlib.sha256(json.dumps(scene, sort_keys=True, separators=(",", ":"),
                                     ensure_ascii=False).encode("utf-8")).hexdigest()


class SourceRecipe(Recipe):
    def __init__(self, scene, key, title=None, edge_ids=None, family="source-series", view_box=None):
        super().__init__(key, title or scene["title"])
        self.scene = scene
        self.data["family"] = family
        self.data["viewBox"] = list(view_box or scene["viewBox"])
        self.data["variantLabel"] = scene["title"]
        self.source_sites = {s["id"]: s for s in scene["sites"]}
        self.source_edges = {e["id"]: e for e in scene["edges"]}
        self.selected_source_edges = list(edge_ids if edge_ids is not None else self.source_edges)
        if len(set(self.selected_source_edges)) != len(self.selected_source_edges):
            raise ValueError("A source selection repeats an edge identity")
        if not set(self.selected_source_edges) <= self.source_edges.keys():
            raise ValueError("A source selection contains an unknown edge")
        self.site_map, self.edge_map, self.control_map = {}, {}, {}
        self._frames = []

    def frame(self):
        x, y, width, height = self.data["viewBox"]
        self.point("page-origin", x, y)
        self.point("page-extent", x + width, y + height)

    def source_point(self, key, point, frame=None):
        x, y = point
        if frame is None:
            left, top, width, height = self.data["viewBox"]
            a, b = "page-origin", "page-extent"
        else:
            left, top, right, bottom = frame.bounds
            width, height = right-left, bottom-top
            a, b = frame.a, frame.b
        if width <= 0 or height <= 0:
            raise ValueError("A source frame needs two positive measured spans")
        if any(type(v) is not int for v in (x, y, left, top, width, height)):
            raise ValueError("Source placement uses integer authored ticks")
        return self.point(key, loc(a, b, x-left, width), loc(a, b, y-top, height))

    def source_frame(self, key, bounds, parent=None, kind="rectangle", emit=True):
        bounds = tuple(bounds)
        if len(bounds) != 4 or bounds[2] <= bounds[0] or bounds[3] <= bounds[1]:
            raise ValueError("A construction frame needs nondegenerate source bounds")
        a = self.source_point(key + "-frame-a", bounds[:2], parent)
        b = self.source_point(key + "-frame-b", bounds[2:], parent)
        corners, edges = [], []
        if emit:
            if kind == "rectangle":
                corners, edges = self.rectangle(key, a, b)
            elif kind in ("triangle", "inverted-triangle"):
                corners, edges = self.triangle(key, a, b, apex_at_top=kind == "triangle")
            elif kind in ("ellipse", "oval", "circle"):
                corners, edges = self.ellipse(key, a, b)
            elif kind == "cross":
                corners, edges = self.cross(key, a, b)
            else:
                raise ValueError("Unknown authoring helper; select an implemented connected construction")
            # Publish a diagonal read from actual primitive output sites.
            # Later consumers therefore use its produced geometry rather than
            # merely sharing the original frame inputs with the primitive.
            if kind in ("ellipse", "oval", "circle"):
                a = self.point(key + "-exposed-a", loc(corners[3]), loc(corners[0]))
                b = self.point(key + "-exposed-b", loc(corners[1]), loc(corners[2]))
            elif kind == "cross":
                a = self.point(key + "-exposed-a", loc(corners[4]), loc(corners[1]))
                b = self.point(key + "-exposed-b", loc(corners[2]), loc(corners[3]))
            elif kind == "triangle":
                a = self.point(key + "-exposed-a", loc(corners[0]), loc(corners[1]))
                b = corners[2]
            elif kind == "inverted-triangle":
                a = corners[0]
                b = self.point(key + "-exposed-b", loc(corners[2]), loc(corners[1]))
        result = Frame(a, b, bounds, edges, corners)
        self._frames.append({"id": key, "at": self.at, "a": a, "b": b,
                             "parent": None if parent is None else [parent.a, parent.b],
                             "bounds": list(bounds), "kind": kind, "edges": list(edges),
                             "qualification": "rough positioning guide; no containment premise"})
        return result

    def source_site(self, source_id, frame):
        if source_id not in self.site_map:
            source = self.source_sites[source_id]
            self.site_map[source_id] = self.source_point("source-site-" + source_id,
                                                        (source["x"], source["y"]), frame)
        return self.site_map[source_id]

    def source_edge(self, source_edge_id, frame, chord=False):
        if source_edge_id not in self.selected_source_edges:
            raise ValueError("An edge lies outside this source selection")
        if not chord and source_edge_id in self.edge_map:
            return self.edge_map[source_edge_id]
        source = self.source_edges[source_edge_id]
        start = self.source_site(source["from"], frame)
        end = self.source_site(source["to"], frame)
        edge_id = ("chord-" if chord else "source-edge-") + source_edge_id
        if any(edge["id"] == edge_id for edge in self.data["edges"]):
            return edge_id
        controls = [start]
        if not chord:
            controls.extend(self.source_point(f"source-control-{source_edge_id}-{i}", tuple(point), frame)
                            for i, point in enumerate(source["controls"][1:-1]))
        controls.append(end)
        self.edge(edge_id, controls, source_edge_id.replace("-", " "))
        if not chord:
            self.edge_map[source_edge_id] = edge_id
            self.control_map[source_edge_id] = controls
        return edge_id

    def source_scope(self, edge_ids, frame, chords=False):
        return [self.source_edge(edge_id, frame, chord=chords) for edge_id in edge_ids]

    def translate_scope(self, key, source_edge_ids, target_edge_ids, dx, dy, frame):
        """Translate earlier actual controls, preserving source endpoint aliases.

        This deliberately accepts only a declared uniform translation whose
        target endpoints are still unbound (or already bound by this same copy).
        Incompatible existing geometry is not silently welded by coordinates.
        """
        if len(source_edge_ids) != len(target_edge_ids) or not source_edge_ids:
            raise ValueError("A repeat needs paired nonempty edge scopes")
        if len(set(source_edge_ids)) != len(source_edge_ids) or len(set(target_edge_ids)) != len(target_edge_ids):
            raise ValueError("Repeated edge scopes cannot duplicate an identity")
        width, height = frame.bounds[2]-frame.bounds[0], frame.bounds[3]-frame.bounds[1]
        source_aliases, target_aliases = {}, {}
        # Check the complete selection before emitting anything: a failed copy
        # must not leave a partially edited recipe or weld independent sites.
        for source_id, target_id in zip(source_edge_ids, target_edge_ids):
            if target_id not in self.selected_source_edges or source_id not in self.control_map or target_id in self.edge_map:
                raise ValueError("Repeated source must exist before a selected unbound target")
            source, target = self.source_edges[source_id], self.source_edges[target_id]
            if len(source["controls"]) != len(target["controls"]) or any(
                [p[0]+dx, p[1]+dy] != list(q) for p, q in zip(source["controls"], target["controls"])
            ):
                raise ValueError("The selected source controls do not establish the declared translation")
            for end in ("from", "to"):
                prior, copied = source[end], target[end]
                if copied in self.site_map:
                    raise ValueError("A repeated target endpoint was independently constructed earlier")
                if (prior in source_aliases and source_aliases[prior] != copied or
                    copied in target_aliases and target_aliases[copied] != prior):
                    raise ValueError("Repetition must preserve the source endpoint identity correspondence")
                source_aliases[prior], target_aliases[copied] = copied, prior
        translated = {}
        targets = []
        for source_id, target_id in zip(source_edge_ids, target_edge_ids):
            source, target = self.source_edges[source_id], self.source_edges[target_id]
            controls = []
            for index, prior in enumerate(self.control_map[source_id]):
                endpoint = target["from"] if index == 0 else target["to"] if index == len(target["controls"])-1 else None
                if prior not in translated:
                    if endpoint is not None and endpoint in self.site_map:
                        raise ValueError("A repeated target endpoint was independently constructed earlier")
                    site_id = "source-site-" + endpoint if endpoint is not None else f"source-control-{target_id}-{index-1}"
                    translated[prior] = self.point(site_id,
                        loc(frame.a, frame.b, dx, width, origin=prior),
                        loc(frame.a, frame.b, dy, height, origin=prior))
                result = translated[prior]
                if endpoint is not None:
                    if endpoint in self.site_map and self.site_map[endpoint] != result:
                        raise ValueError("A source endpoint cannot acquire a second construction identity")
                    self.site_map[endpoint] = result
                controls.append(result)
            edge_id = "source-edge-" + target_id
            self.edge(edge_id, controls, target_id.replace("-", " "))
            self.edge_map[target_id], self.control_map[target_id] = edge_id, controls
            targets.append(edge_id)
        source_ids = [self.edge_map[e] for e in source_edge_ids]
        self.data["repeats"].append({"id": key, "at": self.at, "source": source_ids,
                                    "instances": [source_ids, targets], "label": key.replace("-", " ")})
        return targets

    def finalize_source_mapping(self):
        if set(self.edge_map) != set(self.selected_source_edges):
            raise ValueError("The final construction has not reproduced every selected source edge")
        final = self.data["rungs"][-1]["visible"]
        if set(final) != set(self.edge_map.values()):
            raise ValueError("The final visible result must be exactly the selected source edges")
        used = {self.source_edges[e][end] for e in self.selected_source_edges for end in ("from", "to")}
        if set(self.site_map) != used:
            raise ValueError("The source endpoint correspondence is incomplete")
        self.data["sourceMapping"] = {
            "sceneId": self.scene["id"], "sourceSha256": source_digest(self.scene),
            "sourceEdgeOrder": list(self.selected_source_edges),
            "sourceStrokeOrder": [[e for e in stroke["edges"] if e in self.edge_map]
                                  for stroke in self.scene["strokes"] if any(e in self.edge_map for e in stroke["edges"])],
            "siteMap": dict(self.site_map), "edgeMap": dict(self.edge_map),
            "expectedSites": {s: [self.source_sites[s]["x"], self.source_sites[s]["y"]] for s in self.site_map},
            "expectedControls": {e: self.source_edges[e]["controls"] for e in self.selected_source_edges},
            "status": "authored-source-correspondence",
            "orderQualification": "Source path order retained separately; ladder order is an authored construction proposal."
        }
        self.data["constructionFrames"] = self._frames
        return self.data
