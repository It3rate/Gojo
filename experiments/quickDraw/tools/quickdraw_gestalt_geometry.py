"""Exploratory local simplification proposals over retained QuickDraw evidence.

This is a finite construction adapter. Native connected replacement witnesses,
primitive engines and source correspondences, built by the caller, own meaning.
The coefficients are authored experimental priors, not learned probabilities.
"""
from __future__ import annotations

from copy import deepcopy
import math

from quickdraw_structure_observations import distance, segment_distance, _quadratic, _turn


POLICY = {
    "basis": "512-unit normalized source page; y increases downwards",
    "learned": False, "profileSubdivisions": 24, "maximumPasses": 3,
    "maximumSourceDistance": 36.0, "maximumRelationDistance": 84.0,
    "passSchedule": [{"id": "continuity", "errorWeight": .45, "maximumSymmetricDistance": 36.0},
                     {"id": "primitives", "errorWeight": .12, "maximumSymmetricDistance": 50.0},
                     {"id": "relations", "errorWeight": .04, "maximumSymmetricDistance": 84.0}],
    "maximumClosureGap": 20.0, "axisAllowanceDegrees": 27.0,
    "mergeTurnAllowanceDegrees": 62.0, "errorWeight": 0.45,
    "coherentCurveBend": 18.0,
    "edgeCost": 7.0, "controlCost": 3.0,
    "fitBasis": "Symmetric finite nearest-distance: actual selected source samples to proposal segments, and 25 samples per proposal edge to actual source polyline segments. Source and proposal RMS contribute equally.",
    "qualification": "Authored church/upright/regularity expectations are revisable hypotheses. Complete original intervals remain retained read residue; no exact intent or learned convergence is claimed.",
}


def bezier(controls, t):
    points = [list(p) for p in controls]
    while len(points) > 1:
        points = [[(1-t)*a[j]+t*b[j] for j in range(2)] for a, b in zip(points, points[1:])]
    return points[0]


def sample(controls, n=24):
    return [bezier(controls, i/n) for i in range(n+1)]


def box(points):
    return [min(p[0] for p in points), min(p[1] for p in points), max(p[0] for p in points), max(p[1] for p in points)]


def length(points):
    return sum(distance(a, b) for a, b in zip(points, points[1:]))


def angle(a, b):
    return math.degrees(math.atan2(b[1]-a[1], b[0]-a[0]))


def axis_read(controls):
    a, b = controls[0], controls[-1]
    heading = math.degrees(math.atan2(abs(b[1]-a[1]), abs(b[0]-a[0])))
    return ("horizontal", heading) if heading <= 45 else ("vertical", 90-heading)


def _parameters(points):
    travel = [0.0]
    for a, b in zip(points, points[1:]):
        travel.append(travel[-1]+distance(a, b))
    return [v/travel[-1] if travel[-1] else 0.0 for v in travel]


def _cubic(points):
    ts = _parameters(points)
    aa = ab = bb = 0.0
    rhs = [[0.0, 0.0], [0.0, 0.0]]
    for p, t in zip(points, ts):
        u, v = 3*t*(1-t)**2, 3*t*t*(1-t)
        aa += u*u; ab += u*v; bb += v*v
        for j in range(2):
            rem = p[j]-(1-t)**3*points[0][j]-t**3*points[-1][j]
            rhs[0][j] += u*rem; rhs[1][j] += v*rem
    determinant = aa*bb-ab*ab
    if determinant < 1e-12:
        return [points[0], points[-1]]
    return [points[0], [(bb*rhs[0][j]-ab*rhs[1][j])/determinant for j in range(2)],
            [(aa*rhs[1][j]-ab*rhs[0][j])/determinant for j in range(2)], points[-1]]


def _ordered_error(points, controls):
    errors = [distance(p, bezier(controls, t)) for p, t in zip(points, _parameters(points))]
    return {"rms": math.sqrt(sum(e*e for e in errors)/len(errors)), "max": max(errors)}


def _rms(values):
    return math.sqrt(sum(v*v for v in values)/len(values)) if values else 0.0


class _Builder:
    def __init__(self, observation):
        self.observation = observation
        self.spans = {s["id"]: s for s in observation["spans"]}
        self.strokes = {s["index"]: s for s in observation["strokes"]}
        self.counter = 0
        self.operation_counter = 0
        self.edges = []
        self.residue = []
        self.operations = []
        self.groups = []
        self.relations = []
        self.steps = []
        self.error_weight = POLICY["errorWeight"]
        self.distance_bound = POLICY["maximumSourceDistance"]
        self.roof_spans = set()
        # A supported upper converging pair is an angular construction cue.
        # Preserve that read when a steep slope is also numerically near vertical.
        drawing = observation.get("bounds")
        if drawing:
            for i, first in enumerate(observation["spans"]):
                fa, fb = first["controls"][0], first["controls"][-1]
                fp, fo = sorted((fa, fb), key=lambda p: p[1])
                if fo[1]-fp[1] < 60 or abs(fo[0]-fp[0]) < 25 or fp[1] > drawing[1]+.45*(drawing[3]-drawing[1]):
                    continue
                for second in observation["spans"][i+1:]:
                    sp, so = sorted((second["controls"][0], second["controls"][-1]), key=lambda p: p[1])
                    if distance(fp, sp) <= 24 and so[1]-sp[1] > 60 and (fo[0]-fp[0])*(so[0]-sp[0]) < -625:
                        self.roof_spans.update((first["id"], second["id"]))
        for span in observation["spans"]:
            self.edges.append({"id": "observed-"+span["id"], "controls": deepcopy(span["controls"]),
                "spans": [span["id"]], "sourceRanges": [self.source_range(span)], "parents": [],
                "endpointKeys": [self.site_key(span["strokeIndex"], span["startIndex"]), self.site_key(span["strokeIndex"], span["endIndex"])],
                "primitive": {"name": "line" if len(span["controls"]) == 2 else "quadratic"},
                "role": "observed", "inferred": False})

    @staticmethod
    def site_key(stroke, index):
        return f"acquired-{stroke}-{index}"

    @staticmethod
    def source_range(span):
        return {k: span[k] for k in ("id", "strokeIndex", "startIndex", "endIndex")} | {"spanId": span["id"]}

    def points(self, edge):
        result = []
        previous = None
        for sid in edge["spans"]:
            span = self.spans[sid]
            values = self.strokes[span["strokeIndex"]]["points"][span["startIndex"]:span["endIndex"]+1]
            if previous == (span["strokeIndex"], span["startIndex"]):
                values = values[1:]
            result.extend(values)
            previous = (span["strokeIndex"], span["endIndex"])
        return result

    def measure(self, before, proposed):
        source_points = []
        source_segments = []
        seen_points, seen_segments = set(), set()
        for edge in before:
            for sid in edge["spans"]:
                span = self.spans[sid]
                stroke = span["strokeIndex"]
                points = self.strokes[stroke]["points"]
                for index in range(span["startIndex"], span["endIndex"]+1):
                    key = (stroke, index)
                    if key not in seen_points:
                        source_points.append(points[index]); seen_points.add(key)
                    if index < span["endIndex"] and key not in seen_segments:
                        source_segments.append((points[index], points[index+1])); seen_segments.add(key)
                if span["startIndex"] == span["endIndex"]:
                    point = points[span["startIndex"]]
                    source_segments.append((point, point))
        proposal_points = []
        proposal_segments = []
        for edge in proposed:
            pts = sample(edge["controls"])
            proposal_points.extend(pts)
            proposal_segments.extend(zip(pts, pts[1:]))
        if not source_points or not proposal_points:
            return {"rms": 0.0, "max": 0.0, "sourceRms": 0.0, "proposalRms": 0.0, "sourceMax": 0.0, "proposalMax": 0.0, "sourceTravel": 0.0, "proposalTravel": 0.0,
                    "sourceSampleCount": len(source_points), "sourceSegmentCount": len(source_segments), "proposalSampleCount": len(proposal_points)}
        forward = [min(segment_distance(p, a, b) for a, b in proposal_segments) for p in source_points]
        reverse = [min(segment_distance(p, a, b) for a, b in source_segments) for p in proposal_points]
        sr, pr = _rms(forward), _rms(reverse)
        return {"rms": math.sqrt((sr*sr+pr*pr)/2), "max": max(max(forward), max(reverse)),
                "sourceRms": sr, "proposalRms": pr, "sourceMax": max(forward), "proposalMax": max(reverse),
                "sourceSampleCount": len(source_points), "sourceSegmentCount": len(source_segments), "proposalSampleCount": len(proposal_points),
                "sourceTravel": sum(distance(a, b) for a, b in source_segments),
                "proposalTravel": sum(distance(a, b) for a, b in proposal_segments)}

    @staticmethod
    def complexity(edges):
        return POLICY["edgeCost"]*len(edges)+POLICY["controlCost"]*sum(len(e["controls"]) for e in edges)

    def new_edge(self, controls, sources, primitive=None, role=None, endpoints=None):
        self.counter += 1
        spans = list(dict.fromkeys(sid for e in sources for sid in e["spans"]))
        return {"id": f"proposal-{self.counter:04}", "controls": [list(p) for p in controls],
                "spans": spans, "sourceRanges": [self.source_range(self.spans[sid]) for sid in spans],
                "parents": list(dict.fromkeys(p for e in sources for p in e.get("_rungParents", [e["id"]]))),
                "endpointKeys": endpoints or [sources[0]["endpointKeys"][0], sources[-1]["endpointKeys"][-1]],
                "primitive": primitive or {"name": {2: "line", 3: "quadratic", 4: "cubic"}[len(controls)]},
                "role": role or sources[0]["role"], "inferred": False}

    def patch(self, kind, before, after, *, prior_before=0.0, prior_after=0.0, limit=None, details=None, force_reason=None):
        before, after = list(before), list(after)
        # An already shared acquired boundary is one proposal site. Retarget
        # its other incident profile explicitly, then charge that support too.
        # Merely nearby or coordinate-equal pen lifts are not swept into this.
        selected_ids = {e["id"] for e in before}
        moves = {key: point for e in after for key, point in zip(e["endpointKeys"], (e["controls"][0], e["controls"][-1]))}
        for incident in self.edges:
            if incident["id"] in selected_ids:
                continue
            controls = deepcopy(incident["controls"])
            deltas = []
            for key, point in zip(incident["endpointKeys"], (controls[0], controls[-1])):
                target = moves.get(key, point)
                deltas.append([target[j]-point[j] for j in range(2)])
            shared_incident = any(key in moves for key in incident["endpointKeys"])
            if max(math.hypot(*delta) for delta in deltas) < 1e-9 and not shared_incident:
                continue
            for i, point in enumerate(controls):
                t = i/(len(controls)-1)
                controls[i] = [point[j]+(1-t)*deltas[0][j]+t*deltas[1][j] for j in range(2)]
            before.append(incident)
            primitive = deepcopy(incident["primitive"])
            if primitive["name"] == "affine-profile":
                primitive = {"name": {2: "line", 3: "quadratic", 4: "cubic"}[len(controls)],
                             "staleTransform": incident.get("transform"), "reason": "Later incident-site retargeting changed this profile."}
            after.append(self.new_edge(controls, [incident], primitive, incident["role"], incident["endpointKeys"]))
        self.operation_counter += 1
        mb, ma = self.measure(before, before), self.measure(before, after)
        cb, ca = self.complexity(before), self.complexity(after)
        sb = self.error_weight*mb["rms"]**2+cb+prior_before
        sa = self.error_weight*ma["rms"]**2+ca+prior_after
        threshold = limit or self.distance_bound
        accepted = ma["max"] <= threshold and sa < sb-1e-7 and not force_reason
        reason = force_reason or ("Local complexity/expectation gain exceeds measured fit cost." if accepted else
                 "Proposed ink exceeds the finite source-distance allowance." if ma["max"] > threshold else
                 "No reduction in the declared local objective.")
        operation = {"id": f"patch-{self.operation_counter:04}", "kind": kind, "accepted": accepted,
            "sourceEdgeIds": [e["id"] for e in before], "targetEdgeIds": [e["id"] for e in after],
            "sourceSpans": list(dict.fromkeys(s for e in before for s in e["spans"])),
            "beforeEdges": deepcopy(before), "afterEdges": deepcopy(after),
            "measurements": {"before": mb, "after": ma, "complexityBefore": cb, "complexityAfter": ca,
                             "expectationBefore": prior_before, "expectationAfter": prior_after},
            "scoreBefore": sb, "scoreAfter": sa, "thresholds": {"maximumSymmetricDistance": threshold, "errorWeight": self.error_weight},
            "reason": reason, "construction": details or {},
            "qualification": "Non-draining proposal. Full selected original intervals remain read residue; drawn order and source-site identity are not replaced by geometric equality."}
        self.operations.append(operation)
        if accepted:
            selected = {e["id"] for e in before}
            first = min(i for i, e in enumerate(self.edges) if e["id"] in selected)
            self.edges = [e for e in self.edges if e["id"] not in selected]
            self.edges[first:first] = after
            self.edges.sort(key=lambda e: min((self.spans[s]["strokeIndex"], self.spans[s]["startIndex"]) for s in e["spans"]))
            for edge in after:
                edge["_rungParents"] = edge["parents"]
        return accepted

    def begin(self, phase=0):
        self.operations = []
        selected = POLICY["passSchedule"][phase]
        self.error_weight = selected["errorWeight"]
        self.distance_bound = selected["maximumSymmetricDistance"]
        for edge in self.edges+self.residue:
            edge["_rungParents"] = [edge["id"]]

    def capture(self, step_id, title):
        active_ids = {e["id"] for e in self.edges}
        for relation in self.relations:
            dependencies = relation.get("edgeIds", [])
            if relation.get("sourceEdgeId"):
                dependencies = [relation["sourceEdgeId"], relation["targetEdgeId"]]
            relation["active"] = all(eid in active_ids for eid in dependencies)
            if not relation["active"]:
                relation["staleReason"] = "A later local patch replaced one of the participating profiles; historical correspondence remains retained."
        for group in self.groups:
            active = [e for e in self.edges if e["primitive"].get("group") == group["id"]]
            group["edgeIds"] = [e["id"] for e in active]
            if active:
                b = box([p for e in active for p in sample(e["controls"])])
                group["frame"] = [b[:2], b[2:]]
                if "vertices" in group:
                    group["vertices"] = [active[0]["controls"][0]]+[e["controls"][-1] for e in active]
        def clean(rows):
            result = []
            for edge in rows:
                e = {k: deepcopy(v) for k, v in edge.items() if not k.startswith("_")}
                e["primitive"]["kind"] = e["primitive"]["name"]
                group = next((g for g in self.groups if g["id"] == e["primitive"].get("group")), None)
                b = box(sample(e["controls"]))
                e["primitive"]["frame"] = group["frame"] if group and "frame" in group else [b[:2], b[2:]]
                e["siteKeys"] = [e["endpointKeys"][0]]+[f"{e['id']}-control-{i}" for i in range(1, len(e["controls"])-1)]+[e["endpointKeys"][-1]]
                result.append(e)
            return result
        metrics = self.measure(self.edges, self.edges)
        self.steps.append({"id": step_id, "title": title, "edges": clean(self.edges), "residueEdges": clean(self.residue),
            "operations": deepcopy(self.operations), "groups": deepcopy(self.groups), "relations": deepcopy(self.relations),
            "metrics": metrics | {"edgeCount": len(self.edges), "residueEdgeCount": len(self.residue),
                                  "controlCount": sum(len(e["controls"]) for e in self.edges)},
            "sourceCoverage": {"active": [s for e in self.edges for s in e["spans"]],
                               "complement": [s for e in self.residue for s in e["spans"]]},
            "residue": {"sourceSpanIds": list(self.spans), "kind": "complete-original-intervals",
                        "qualification": "Original sampled ink remains available; a better construction fit does not certify intended meaning."},
            "convergence": {"accepted": sum(o["accepted"] for o in self.operations), "rejected": sum(not o["accepted"] for o in self.operations)}})

    def continuity(self):
        self.begin()
        # A small repeatedly retraced mark is retained as a complement instead
        # of pretending sixteen acquired turns are one intentional wall edge.
        for stroke in self.observation["strokes"]:
            selected = [e for e in self.edges if all(self.spans[s]["strokeIndex"] == stroke["index"] for s in e["spans"])]
            pts = stroke["points"]
            if len(selected) < 5 or not pts:
                continue
            b = box(pts); diagonal = distance(b[:2], b[2:])
            travel = length(pts)
            if diagonal < 45 and travel > max(100, 4*diagonal):
                self.operation_counter += 1
                self.operations.append({"id": f"patch-{self.operation_counter:04}", "kind": "carry-detail-complement", "accepted": True,
                    "sourceEdgeIds": [e["id"] for e in selected], "targetEdgeIds": [e["id"] for e in selected],
                    "sourceSpans": [s for e in selected for s in e["spans"]], "beforeEdges": deepcopy(selected), "afterEdges": deepcopy(selected),
                    "measurements": {"diameter": diagonal, "travel": travel, "retainedExactly": True},
                    "scoreBefore": self.complexity(selected), "scoreAfter": 0.0,
                    "thresholds": {"maximumDetailDiameter": 45, "minimumTravelToDiameter": 4},
                    "reason": "Small repeated retracing retained as visible detail complement, with every source interval and ordered turn preserved.",
                    "construction": {"focus": "main form", "complement": "faint retained detail"}})
                for edge in selected:
                    self.edges.remove(edge); edge["role"] = "detail-complement"; self.residue.append(edge)
        for iteration in range(24):
            changed = False
            for a, b in zip(self.edges, self.edges[1:]):
                sa, sb = self.spans[a["spans"][-1]], self.spans[b["spans"][0]]
                if sa["strokeIndex"] != sb["strokeIndex"] or sa["endIndex"] != sb["startIndex"]:
                    continue
                pa, pb = sample(sa["controls"]), sample(sb["controls"])
                turn = abs(_turn(pa[-3], pa[-1], pb[2]))
                points = self.points(a)+self.points(b)[1:]
                if turn > POLICY["mergeTurnAllowanceDegrees"] and min(length(pa), length(pb)) > 17:
                    continue
                choices = [[points[0], points[-1]], _quadratic(points)[0], _cubic(points)]
                candidates = []
                for controls in choices:
                    ordered = _ordered_error(points, controls)
                    if ordered["max"] > 25:
                        continue
                    edge = self.new_edge(controls, [a, b], role="continuous-profile")
                    m = self.measure([a, b], [edge])
                    score = POLICY["errorWeight"]*m["rms"]**2+self.complexity([edge])
                    candidates.append((score, edge, ordered))
                if not candidates:
                    continue
                _, proposed, ordered = min(candidates, key=lambda row: row[0])
                if self.patch("merge-continuous-intervals", [a, b], [proposed], details={"orderedFit": ordered, "boundaryTurnDegrees": turn, "iteration": iteration}):
                    changed = True
                    break
            if not changed:
                break
        self.capture("continuity", "Carry long lines and whole curves")

    def _rectangle(self, chain):
        if len(chain) != 3:
            return False
        reads = [axis_read(e["controls"]) for e in chain]
        if reads[0][0] != reads[2][0] or reads[0][0] == reads[1][0] or max(r[1] for r in reads) > 27:
            return False
        # Connected local three-sided region, not three unrelated parallel marks.
        if any(a["endpointKeys"][-1] != b["endpointKeys"][0] for a, b in zip(chain, chain[1:])):
            return False
        points = [chain[0]["controls"][0]]+[e["controls"][-1] for e in chain]
        major = 1 if reads[0][0] == "vertical" else 0
        if (points[1][major]-points[0][major])*(points[3][major]-points[2][major]) >= 0:
            return False
        other = 1-major
        one = (points[0][other]+points[1][other])/2
        two = (points[2][other]+points[3][other])/2
        near = (points[0][major]+points[3][major])/2
        far = (points[1][major]+points[2][major])/2
        if min(abs(one-two), abs(near-far)) < 25:
            return False
        vertices = []
        for o, m in [(one, near), (one, far), (two, far), (two, near)]:
            p = [0.0, 0.0]; p[other] = o; p[major] = m; vertices.append(p)
        gid = f"rectangle-{self.operation_counter+1}"
        keys = [chain[0]["endpointKeys"][0]]+[e["endpointKeys"][-1] for e in chain]
        b = box(vertices)
        role = "door-region" if (b[2]-b[0] < 0.4*(self.observation["bounds"][2]-self.observation["bounds"][0]) and b[1] > 0.5*(self.observation["bounds"][1]+self.observation["bounds"][3])) else "wall-region"
        after = [self.new_edge([vertices[i], vertices[i+1]], [e], {"name": "rectangle-side", "group": gid, "side": i}, role, keys[i:i+2]) for i, e in enumerate(chain)]
        details = {"engine": "rectangle", "vertices": vertices, "bounds": b, "closed": False,
                   "openSide": [vertices[-1], vertices[0]], "missingSideIsGuideOnly": True,
                   "sourceEndpointGaps": [distance(a["controls"][-1], z["controls"][0]) for a, z in zip(chain, chain[1:])]}
        if self.patch("three-sided-rectangle", chain, after, prior_before=55, details=details):
            self.groups.append({"id": gid, "kind": "rectangle", "role": role, "edgeIds": [e["id"] for e in after], "spans": [s for e in after for s in e["spans"]], **details})
            return True
        return False

    def primitives(self):
        self.begin(1)
        if not self.edges:
            self.capture("primitives", "No source ink for primitive proposals")
            return
        for stroke in self.observation["strokes"]:
            chain = [e for e in self.edges if all(self.spans[s]["strokeIndex"] == stroke["index"] for s in e["spans"])]
            if len(chain) != 3:
                continue
            a, b = chain[0]["controls"][0], chain[-1]["controls"][-1]
            if distance(a, b) > POLICY["maximumClosureGap"]:
                continue
            vertex = [(a[j]+b[j])/2 for j in range(2)]
            vertices = [vertex, chain[0]["controls"][-1], chain[1]["controls"][-1]]
            if min(distance(vertices[i], vertices[(i+1)%3]) for i in range(3)) < 65:
                continue
            area = abs(sum(vertices[i][0]*vertices[(i+1)%3][1]-vertices[(i+1)%3][0]*vertices[i][1] for i in range(3)))/2
            if area < 3500 or min(p[1] for p in vertices) > (self.observation["bounds"][1]+self.observation["bounds"][3])/2:
                continue
            gid = f"triangle-{self.operation_counter+1}"
            keys = [f"{gid}-closed-vertex", chain[0]["endpointKeys"][-1], chain[1]["endpointKeys"][-1]]
            after = [self.new_edge([vertices[i], vertices[(i+1)%3]], [e], {"name": "triangle-side", "group": gid, "side": i}, "roof-region", [keys[i], keys[(i+1)%3]]) for i, e in enumerate(chain)]
            details = {"engine": "triangle", "vertices": vertices, "closed": True,
                "sourceEndpointGap": distance(a, b), "inferredClosure": True,
                "inferredJoin": {"siteKey": keys[0], "sourceSiteKeys": [chain[0]["endpointKeys"][0], chain[-1]["endpointKeys"][-1]], "qualification": "Explicit proposal closure; the two acquired events remain separate."}}
            if self.patch("near-closed-roof-triangle", chain, after, prior_before=55, details=details):
                self.groups.append({"id": gid, "kind": "triangle", "role": "roof-region", "edgeIds": [e["id"] for e in after], "spans": [s for e in after for s in e["spans"]], **details})
        used = set()
        for i in range(max(0, len(self.edges)-2)):
            chain = self.edges[i:i+3]
            if any(e["id"] in used for e in chain):
                continue
            if self._rectangle(chain):
                used.update(e["id"] for e in self.edges[i:i+3])
        # Whole open arches are a lower-middle church cue. No new door is added.
        drawing = self.observation["bounds"]; width = drawing[2]-drawing[0]; height = drawing[3]-drawing[1]
        for stroke in self.observation["strokes"]:
            chain = [e for e in self.edges if all(self.spans[s]["strokeIndex"] == stroke["index"] for s in e["spans"])]
            if not chain or len(chain) > 5 or any(e["primitive"]["name"] == "rectangle-side" for e in chain):
                continue
            pts = stroke["points"]
            if len(pts) < 8:
                continue
            b = box(pts); w, h = b[2]-b[0], b[3]-b[1]
            if not (25 < w < width*.60 and 35 < h < height*.60 and b[1] > drawing[1]+height*.45):
                continue
            if min(pts[0][1], pts[-1][1]) < b[1]+h*.70:
                continue
            # Preserve acquired endpoint order; use a complete cubic profile.
            left_to_right = pts[0][0] <= pts[-1][0]
            lo, hi = (pts[0], pts[-1]) if left_to_right else (pts[-1], pts[0])
            cx = (lo[0]+hi[0])/2; half = max(12, (hi[0]-lo[0])/2)
            base = (lo[1]+hi[1])/2; top = b[1]
            controls = [[cx-half, base], [cx-half, top-(base-top)/3], [cx+half, top-(base-top)/3], [cx+half, base]]
            if not left_to_right:
                controls.reverse()
            gid = f"arch-{self.operation_counter+1}"
            after = [self.new_edge(controls, chain, {"name": "cubic-arch", "group": gid}, "door-region")]
            if self.patch("whole-door-arch", chain, after, prior_before=90, details={"engine": "symmetric-open-arch", "centerX": cx, "baseY": base, "topY": top, "closed": False}):
                self.groups.append({"id": gid, "kind": "arch", "role": "door-region", "edgeIds": [after[0]["id"]], "spans": after[0]["spans"], "bounds": [cx-half, top, cx+half, base]})
        # A peak supported by two long converging roof edges remains open below.
        for a, b in list(zip(self.edges, self.edges[1:])):
            if a not in self.edges or b not in self.edges or a["endpointKeys"][-1] != b["endpointKeys"][0]:
                continue
            p, peak, q = a["controls"][0], a["controls"][-1], b["controls"][-1]
            if max(max(segment_distance(v, e["controls"][0], e["controls"][-1]) for v in sample(e["controls"])) for e in (a, b)) > POLICY["coherentCurveBend"]:
                continue
            if not (peak[1] < min(p[1], q[1])-45 and min(distance(p, peak), distance(q, peak)) > 80 and abs(p[1]-q[1]) < 40):
                continue
            base = (p[1]+q[1])/2; vertices = [[p[0], base], peak, [q[0], base]]
            gid = f"roof-{self.operation_counter+1}"; keys = [a["endpointKeys"][0], a["endpointKeys"][-1], b["endpointKeys"][-1]]
            after = [self.new_edge([vertices[i], vertices[i+1]], [e], {"name": "triangle-side", "group": gid, "side": i}, "roof-region", keys[i:i+2]) for i, e in enumerate([a, b])]
            details = {"engine": "triangle", "vertices": vertices, "closed": False, "missingBaseIsGuideOnly": True}
            if self.patch("pointed-roof-profile", [a, b], after, prior_before=35, details=details):
                self.groups.append({"id": gid, "kind": "triangle", "role": "roof-region", "edgeIds": [e["id"] for e in after], "spans": [s for e in after for s in e["spans"]], **details})
        self.axis_lines()
        self.capture("primitives", "Read supported walls, roofs and arches")

    def axis_lines(self):
        # Local straightness and axis expectations retain their explicit error.
        for edge in list(self.edges):
            if edge not in self.edges or "group" in edge["primitive"]:
                continue
            pts = self.points(edge); a, b = edge["controls"][0], edge["controls"][-1]
            if distance(a, b) < 25:
                continue
            direction, deviation = axis_read(edge["controls"])
            roof_support = sorted(self.roof_spans.intersection(edge["spans"]))
            if deviation <= POLICY["axisAllowanceDegrees"] and not roof_support:
                axis = 1 if direction == "horizontal" else 0
                center = sum(p[axis] for p in pts)/len(pts)
                aa, bb = list(a), list(b); aa[axis] = bb[axis] = center
                controls = [aa, bb]
                prior = .14*deviation*deviation+8
                kind = "upright-or-level-line"
            else:
                controls, prior, kind = [a, b], 12.0, "straight-roof-or-angular-line"
            if len(controls) == len(edge["controls"]) and max(distance(p, q) for p, q in zip(controls, edge["controls"])) < 1e-8:
                continue
            after = [self.new_edge(controls, [edge], {"name": "line", "direction": direction if deviation <= POLICY["axisAllowanceDegrees"] else "angular"}, edge["role"])]
            bend = max(segment_distance(p, a, b) for p in sample(edge["controls"]))
            self.patch(kind, [edge], after, prior_before=prior,
                details={"axis": direction, "deviationDegrees": deviation, "coherentBend": bend, "sourceRoofSpans": roof_support, "reference": "authored page-up/upright expectation, subordinate to measured upper converging slopes"},
                force_reason="A substantial coherent curve retains its complete profile instead of being treated as a noisy straight line." if bend > POLICY["coherentCurveBend"] else None)

    def relationships(self):
        self.begin(2)
        if not self.edges:
            self.capture("relations", "No source ink for relational proposals")
            return
        self.axis_lines()
        drawing = self.observation["bounds"]
        # Cross candidates must already have two measured acquired arms.
        for crossing in self.observation.get("crosses", []):
            chosen = []
            for sid in crossing["spans"]:
                edge = next((e for e in self.edges if sid in e["spans"]), None)
                if edge and edge not in chosen:
                    chosen.append(edge)
            if len(chosen) != 2:
                continue
            horizontal = max(chosen, key=lambda e: abs(e["controls"][-1][0]-e["controls"][0][0]))
            vertical = next(e for e in chosen if e is not horizontal)
            vx = sum(p[0] for p in vertical["controls"])/len(vertical["controls"])
            hy = sum(p[1] for p in horizontal["controls"])/len(horizontal["controls"])
            a, b = horizontal["controls"][0], horizontal["controls"][-1]
            half = distance(a, b)/2
            hcontrols = [[vx-half, hy], [vx+half, hy]]
            if a[0] > b[0]: hcontrols.reverse()
            vcontrols = [[vx, vertical["controls"][0][1]], [vx, vertical["controls"][-1][1]]]
            gid = f"cross-{self.operation_counter+1}"
            after = [self.new_edge(hcontrols, [horizontal], {"name": "cross-arm", "group": gid, "axis": "horizontal"}, "cross-region"),
                     self.new_edge(vcontrols, [vertical], {"name": "cross-arm", "group": gid, "axis": "vertical"}, "cross-region")]
            old_center = (a[0]+b[0])/2
            before_prior = 40+.12*(old_center-vx)**2
            if self.patch("center-observed-cross-arms", [horizontal, vertical], after, prior_before=before_prior,
                          details={"engine": "centered-cross", "center": [vx, hy], "existingCrossId": crossing["id"], "armLengths": [2*half, distance(*vcontrols)], "sourceCrossingDoesNotCreateAcquiredIdentity": True}):
                self.groups.append({"id": gid, "kind": "cross", "edgeIds": [e["id"] for e in after], "spans": crossing["spans"], "center": [vx, hy]})
        # Locally balance the already-supported two roof slopes. Oblique side
        # walls are not forced into a global front-facing church silhouette.
        for group in list(self.groups):
            if group["kind"] != "triangle":
                continue
            chain = [e for e in self.edges if e["primitive"].get("group") == group["id"]]
            if len(chain) != 2:
                continue
            p, peak, q = chain[0]["controls"][0], chain[0]["controls"][-1], chain[1]["controls"][-1]
            center = (p[0]+q[0])/2; mismatch = peak[0]-center
            new_peak = [center, peak[1]]
            after = [self.new_edge([p, new_peak], [chain[0]], chain[0]["primitive"], chain[0]["role"]),
                     self.new_edge([new_peak, q], [chain[1]], chain[1]["primitive"], chain[1]["role"])]
            if self.patch("balance-local-roof", chain, after, prior_before=.20*mismatch*mismatch, prior_after=.20*(new_peak[0]-center)**2,
                          limit=POLICY["maximumRelationDistance"], details={"axis": {"from": [center, peak[1]], "to": [center, p[1]]}, "relaxation": 1.0, "unresolvedSymmetry": new_peak[0]-center}):
                self.relations.append({"kind": "local-reflection", "edgeIds": [e["id"] for e in after], "axisX": center, "remainingOffset": new_peak[0]-center})
        # Same-shape translation/scale is proposed only for substantial curved
        # profiles. The native constructor can reuse the earlier template.
        curves = [e for e in self.edges if len(e["controls"]) >= 3 and length(sample(e["controls"])) > 65]
        reused = set()
        for i, first in enumerate(curves):
            for second in curves[i+1:]:
                if first not in self.edges or second not in self.edges or second["id"] in reused:
                    continue
                a, b = box(sample(first["controls"])), box(sample(second["controls"]))
                aw, ah, bw, bh = a[2]-a[0], a[3]-a[1], b[2]-b[0], b[3]-b[1]
                if min(aw, ah, bw, bh) < 15 or not (.45 < bw/aw < 2.2 and .45 < bh/ah < 2.2):
                    continue
                if distance([(a[0]+a[2])/2, (a[1]+a[3])/2], [(b[0]+b[2])/2, (b[1]+b[3])/2]) < 40:
                    continue
                choices = []
                for reverse in (False, True):
                    transformed = [[b[0]+(p[0]-a[0])*bw/aw, b[1]+(p[1]-a[1])*bh/ah] for p in first["controls"]]
                    if reverse: transformed.reverse()
                    error = max(distance(p, q) for p, q in zip(sample(transformed), sample(second["controls"])))
                    choices.append((error, reverse, transformed))
                error, reverse, transformed = min(choices)
                if error > 18:
                    continue
                matrix = [bw/aw, 0.0, b[0]-a[0]*bw/aw, 0.0, bh/ah, b[1]-a[1]*bh/ah]
                after = [self.new_edge(transformed, [second], {"name": "affine-profile", "template": first["id"]}, second["role"])]
                after[0]["parents"] = list(dict.fromkeys(after[0]["parents"]+first.get("_rungParents", [first["id"]])))
                after[0]["transform"] = {"sourceEdgeId": first["id"], "sourceEdgeIds": [first["id"]], "matrix": matrix, "reversed": reverse}
                if self.patch("reuse-affine-profile", [second], after, prior_before=28, details={"templateEdgeId": first["id"], "matrix": matrix, "reversed": reverse, "profileMismatchBefore": error}):
                    reused.add(second["id"])
                    self.relations.append({"kind": "affine-profile-repetition", "sourceEdgeId": first["id"], "targetEdgeId": after[0]["id"], "matrix": matrix, "reversed": reverse})
        # Near-containment is an explicit relation read, not a fabricated door.
        for group in self.groups:
            if group.get("role") != "door-region" or "bounds" not in group:
                continue
            b = group["bounds"]
            overflow = max(0, drawing[0]-b[0], drawing[1]-b[1], b[2]-drawing[2], b[3]-drawing[3])
            self.relations.append({"kind": "near-containment", "groupId": group["id"], "parent": "observed-building-envelope", "bounds": b, "parentBounds": drawing, "overflow": overflow, "allowance": 12.0, "accepted": overflow <= 12, "qualification": "Envelope containment is weaker than wall occupancy or a closed interior."})
        self.capture("relations", "Balance related parts while retaining the drawing")


def build(observation):
    """Return four complete finite candidate states without modifying input."""
    builder = _Builder(observation)
    builder.capture("observed", "Original fitted source intervals")
    builder.continuity()
    builder.primitives()
    builder.relationships()
    return {"schema": "gojo.quickdraw-gestalt-proposals.v1", "sourceId": observation["sourceId"],
            "sourceDrawingSha256": observation.get("sourceDrawingSha256"), "policy": deepcopy(POLICY),
            "steps": builder.steps, "convergence": {"passes": 3, "stop": "Declared local pass budget completed; rejected candidates and remaining mismatch retained.",
                "accepted": sum(s["convergence"]["accepted"] for s in builder.steps), "globalOptimumClaimed": False}}
