"""Late, explicitly authored church interpretation over an existing candidate.

This finite adapter proposes construction imports; it is not kernel settlement
or a learned model. The caller materializes its weighted references and shared
sites in native construction structure. Earlier candidates are never rewritten.
"""
from __future__ import annotations

from copy import deepcopy
import math

from quickdraw_gestalt_geometry import _Builder, box, sample, distance, length, axis_read


POLICY = {
    "learned": False,
    "selectionPolicy": "authored ordered synthesis; structural admissibility, not raw tracing improvement",
    "nativeControlTolerance": 6,
    "jointRadius": 36.0,
    "maximumClusterDiameter": 58.0,
    "schedule": [
        {"originalInk": .015, "previousInterpretation": .25, "geometricPrior": 4.0},
        {"originalInk": .004, "previousInterpretation": .12, "geometricPrior": 6.0},
        {"originalInk": .001, "previousInterpretation": .04, "geometricPrior": 10.0},
    ],
    "qualification": "Exploratory hand-authored preferences. More distant original ink is expected, not treated as failure. View hypotheses and inferred joins are revisable; no absent cross, door or window is supplied.",
}


def ref(edge, index, weight=1.0):
    return {"edgeId": edge["id"], "controlIndex": index, "weight": weight}


def combine(*terms):
    """Combine already affine coefficients without normalizing them."""
    result = {}
    for scale, refs in terms:
        for row in refs:
            key = (row["edgeId"], row["controlIndex"])
            result[key] = result.get(key, 0.0)+scale*row["weight"]
    return [{"edgeId": eid, "controlIndex": index, "weight": weight}
            for (eid, index), weight in result.items() if abs(weight) > 1e-12]


def evaluate(refs, edges, axis):
    return sum(row["weight"]*edges[row["edgeId"]]["controls"][row["controlIndex"]][axis] for row in refs)


def elevated(edge, degree):
    points = deepcopy(edge["controls"])
    refs = [[ref(edge, i)] for i in range(len(points))]
    while len(points)-1 < degree:
        n = len(points)-1
        points = [points[0]]+[[i/(n+1)*points[i-1][j]+(1-i/(n+1))*points[i][j] for j in range(2)] for i in range(1, n+1)]+[points[-1]]
        refs = [refs[0]]+[combine((i/(n+1), refs[i-1]), (1-i/(n+1), refs[i])) for i in range(1, n+1)]+[refs[-1]]
    return points, refs


def _profile_delta(before, after):
    """Finite ordered displacement, distinct from nearest original-ink fit."""
    old = {e["id"]: e for e in before}
    values = []
    for edge in after:
        parents = [old[p] for p in edge.get("previousEdgeIds", edge.get("parents", [])) if p in old]
        if not parents:
            continue
        candidates = [sample(p["controls"]) for p in parents]
        target = sample(edge["controls"])
        values.extend(min(distance(p, q) for row in candidates for q in row) for p in target)
    return {"rms": math.sqrt(sum(v*v for v in values)/len(values)) if values else 0.0,
            "max": max(values, default=0.0), "sampleCount": len(values)}


def _front_read(edges):
    """Read a supported facade and exterior depth marks before regularization."""
    wall = [e for e in edges if e.get("role") == "wall-region"]
    shell = [e for e in edges if e.get("role") not in ("door-region", "cross-region", "detail-complement")]
    stroke_lengths = {}
    for edge in shell:
        for row in edge.get("sourceRanges", []):
            stroke_lengths[row["strokeIndex"]] = stroke_lengths.get(row["strokeIndex"], 0)+length(sample(edge["controls"]))/max(1, len(edge["sourceRanges"]))
    stroke = max(stroke_lengths, key=stroke_lengths.get) if stroke_lengths else None
    main = [e for e in shell if any(s["strokeIndex"] == stroke for s in e.get("sourceRanges", []))]
    if wall:
        bounds = box([p for e in wall for p in e["controls"]])
        front = wall
    elif main:
        verticals = [e for e in main if len(e["controls"]) == 2 and axis_read(e["controls"])[0] == "vertical" and distance(*e["controls"]) > 100]
        if len(verticals) >= 2:
            left = min(verticals, key=lambda e: e["controls"][0][0])
            right = max(verticals, key=lambda e: e["controls"][0][0])
            bounds = box([p for e in (left, right) for p in e["controls"]])
        else:
            bounds = box([p for e in main for p in sample(e["controls"])])
        front = main
    else:
        return {"kind": "unresolved", "reason": "No substantial connected facade support.", "supportEdgeIds": [], "candidates": [], "frontBounds": None, "mainStrokeIndex": stroke}
    left, top, right, bottom = bounds
    width, height = right-left, bottom-top
    exterior = [e for e in shell if e not in front and length(sample(e["controls"])) > 65
                and (max(p[0] for p in e["controls"]) < left+.22*width or min(p[0] for p in e["controls"]) > right-.12*width)]
    side_candidates = []
    for sign in (-1, 1):
        selected = [e for e in exterior if (min(p[0] for p in sample(e["controls"])) < left-35 if sign < 0 else max(p[0] for p in sample(e["controls"])) > right+35)]
        if not selected:
            continue
        b = box([p for e in selected for p in sample(e["controls"])])
        depth = left-b[0] if sign < 0 else b[2]-right
        vertical = any(axis_read(e["controls"])[0] == "vertical" and abs(e["controls"][-1][1]-e["controls"][0][1]) > .45*height for e in selected)
        rise = [e for e in shell if e not in front and axis_read(e["controls"])[1] > 15 and length(sample(e["controls"])) > 65
                and any((p[0] < left+30 if sign < 0 else p[0] > right-30) for p in e["controls"])]
        score = min(1.0, depth/max(80, width))*.5+.3*bool(vertical)+.2*bool(rise)
        if score > .30:
            side_candidates.append({"depthSign": sign, "bounds": b, "depth": depth, "supportEdgeIds": list(dict.fromkeys(e["id"] for e in selected+rise)), "score": score})
    side = max(side_candidates, key=lambda s: s["score"], default=None)
    oblique = side["score"] if side else 0.0
    kind = "oblique" if oblique >= .43 else "front"
    return {"kind": kind, "reason": "Supported exterior wall/depth marks select a local facade and a separate side plane." if kind == "oblique" else "No sufficiently supported exterior depth plane; frontal regularity is the current hypothesis.",
            "supportEdgeIds": list(dict.fromkeys([e["id"] for e in front]+(side["supportEdgeIds"] if side else []))),
            "frontEdgeIds": [e["id"] for e in front], "frontBounds": bounds,
            "mainStrokeIndex": stroke, "side": side if kind == "oblique" else None,
            "candidates": [{"kind": "front", "support": 1-oblique, "reason": "Upright envelope and local facade support.", "supportEdgeIds": [e["id"] for e in front]},
                           {"kind": "oblique", "support": oblique, "reason": "Exterior extent, long wall and angular roof/depth cues.", "supportEdgeIds": side["supportEdgeIds"] if side else []}],
            "qualification": "Authored view read, not learned perspective classification or a camera reconstruction."}


class Completion(_Builder):
    def __init__(self, observation, proposal):
        super().__init__(observation)
        last = proposal["steps"][-1]
        self.edges = deepcopy(last["edges"])
        self.residue = deepcopy(last["residueEdges"])
        self.groups = deepcopy(last["groups"])
        self.relations = deepcopy(last["relations"])
        self.steps = []
        self.counter = 0
        self.operation_counter = 0
        self.phase = 0
        self.view = _front_read(self.edges)
        self.bindings = {}
        self.pass_before = deepcopy(self.edges)

    def begin_late(self, phase):
        self.phase = phase
        self.operations = []
        self.pass_before = deepcopy(self.edges)
        self.bindings = {}
        for edge in self.edges+self.residue:
            edge["_rungParents"] = [edge["id"]]

    def new_edge(self, controls, sources, primitive=None, role=None, endpoints=None):
        edge = super().new_edge(controls, sources, primitive, role, endpoints)
        edge["id"] = f"completion-{self.counter:04}"
        edge["previousEdgeIds"] = [e["id"] for e in sources]
        edge.pop("transform", None)
        edge["siteKeys"] = [edge["endpointKeys"][0]]+[f"{edge['id']}-control-{i}" for i in range(1, len(controls)-1)]+[edge["endpointKeys"][-1]]
        return edge

    def patch_late(self, kind, before, after, bindings, *, constraint_before=0.0, constraint_after=0.0, details=None, reason=None):
        self.operation_counter += 1
        before, after = list(before), list(after)
        old_fit, new_fit = self.measure(before, before), self.measure(before, after)
        delta = _profile_delta(before, after)
        weights = POLICY["schedule"][self.phase]
        sb = weights["originalInk"]*old_fit["rms"]**2+weights["geometricPrior"]*constraint_before
        sa = weights["originalInk"]*new_fit["rms"]**2+weights["previousInterpretation"]*delta["rms"]**2+weights["geometricPrior"]*constraint_after
        invalid = any(not all(math.isfinite(v) for v in p) for e in after for p in e["controls"])
        accepted = not invalid and not reason
        binding_rows = list({b["targetSiteKey"]: b for b in bindings}.values())
        operation = {"id": f"completion-patch-{self.operation_counter:04}", "kind": kind, "accepted": accepted,
            "sourceEdgeIds": [e["id"] for e in before], "targetEdgeIds": [e["id"] for e in after],
            "sourceSpans": list(dict.fromkeys(s for e in before for s in e["spans"])),
            "beforeEdges": deepcopy(before), "afterEdges": deepcopy(after),
            "measurements": {"before": old_fit, "after": new_fit, "originalBefore": old_fit, "originalAfter": new_fit,
                             "previousPass": delta, "constraintBefore": constraint_before, "constraintAfter": constraint_after,
                             "complexityBefore": self.complexity(before), "complexityAfter": self.complexity(after)},
            "scoreBefore": sb, "scoreAfter": sa,
            "thresholds": {"rawDistanceIsAcceptanceGate": False, "policyWeights": deepcopy(weights)},
            "selectionPolicy": POLICY["selectionPolicy"],
            "reason": reason or "Explicit synthesis pressure selected a coherent later construction; original-ink disagreement remains diagnostic.",
            "construction": {"endpointBindings": binding_rows, "nativeControlTolerance": 6, **(details or {})},
            "qualification": POLICY["qualification"]}
        self.operations.append(operation)
        if accepted:
            selected = {e["id"] for e in before}
            first = min((i for i, e in enumerate(self.edges) if e["id"] in selected), default=len(self.edges))
            self.edges = [e for e in self.edges if e["id"] not in selected]
            self.edges[first:first] = after
            self.edges.sort(key=lambda edge: min(((row["strokeIndex"], row["startIndex"]) for row in edge.get("sourceRanges", [])), default=(100000, 0)))
            for edge in after:
                edge["_rungParents"] = list(dict.fromkeys(p for old in before for p in old.get("_rungParents", [old["id"]])))
        return accepted

    def bind(self, key, xrefs, yrefs=None, mode="weighted-merge"):
        yrefs = xrefs if yrefs is None else yrefs
        return {"targetSiteKey": key, "mode": mode, "inputs": xrefs, "xInputs": xrefs, "yInputs": yrefs}

    def lookup(self):
        return {e["id"]: e for e in self.pass_before+self.edges+[e for o in self.operations for e in o["beforeEdges"]+o["afterEdges"]]}

    def propagate(self, before, after, bindings, endpoint_moves=None):
        """Retarget actual incident profiles when a shared proposed site moves."""
        selected = {e["id"] for e in before}
        rows = {b["targetSiteKey"]: b for b in bindings}
        moves = {}
        for old, new in zip(before, after):
            for oi, ni in ((0, 0), (len(old["controls"])-1, len(new["controls"])-1)):
                target = new["siteKeys"][ni]
                if target in rows:
                    if target == old["siteKeys"][oi] and distance(old["controls"][oi], new["controls"][ni]) < 1e-9:
                        continue
                    moves[old["siteKeys"][oi]] = (target, new["controls"][ni], rows[target])
        if endpoint_moves is not None:
            moves = {oldkey: (newkey, point, rows[newkey]) for oldkey, newkey, point in endpoint_moves}
        for old in self.edges:
            if old["id"] in selected or not any(key in moves for key in old["endpointKeys"]):
                continue
            controls = deepcopy(old["controls"])
            keys = list(old["endpointKeys"])
            changes = []
            for j, index in enumerate((0, len(controls)-1)):
                move = moves.get(keys[j])
                changes.append(move)
                if move:
                    keys[j] = move[0]
            edge = self.new_edge(controls, [old], {"name": {2: "line", 3: "quadratic", 4: "cubic"}[len(controls)]}, old["role"], keys)
            for i, point in enumerate(controls):
                t = i/(len(controls)-1)
                xrefs, yrefs = [ref(old, i)], [ref(old, i)]
                for move, coefficient, oldindex in ((changes[0], 1-t, 0), (changes[1], t, len(controls)-1)):
                    if move:
                        xrefs = combine((1, xrefs), (coefficient, move[2]["xInputs"]), (-coefficient, [ref(old, oldindex)]))
                        yrefs = combine((1, yrefs), (coefficient, move[2]["yInputs"]), (-coefficient, [ref(old, oldindex)]))
                edge["controls"][i] = [evaluate(xrefs, self.lookup(), 0), evaluate(yrefs, self.lookup(), 1)]
                bindings.append(self.bind(edge["siteKeys"][i], xrefs, yrefs))
            before.append(old); after.append(edge)
        return before, after, bindings

    def capture_late(self, ident, title):
        self.capture(ident, title)
        step = self.steps[-1]
        for relation in step["relations"]:
            if relation.get("proposedOnly"):
                relation["active"] = False
        step["viewHypothesis"] = deepcopy(self.view)
        step["policyWeights"] = deepcopy(POLICY["schedule"][self.phase])
        step["selectionPolicy"] = POLICY["selectionPolicy"]
        step["metrics"]["previousPassRms"] = _profile_delta(self.pass_before, self.edges)["rms"]
        step["metrics"]["jointMergeCount"] = sum(len(o["construction"].get("jointClusters", [])) for o in self.operations if o["accepted"])

    def converge_joints(self):
        self.begin_late(0)
        for iteration in range(2):
            endpoints = {}
            for e in self.edges:
                if e.get("role") in ("cross-region", "door-region") or length(sample(e["controls"])) < 40:
                    continue
                for index in (0, len(e["controls"])-1):
                    key = e["siteKeys"][index]
                    endpoints.setdefault(key, {"key": key, "point": e["controls"][index], "refs": []})["refs"].append(ref(e, index))
            clusters = [[v] for v in endpoints.values()]
            changed = True
            while changed:
                changed = False
                for i, a in enumerate(clusters):
                    for j in range(i+1, len(clusters)):
                        b = clusters[j]
                        if min(distance(x["point"], y["point"]) for x in a for y in b) > POLICY["jointRadius"]:
                            continue
                        if max(distance(x["point"], y["point"]) for x in a+b for y in a+b) > POLICY["maximumClusterDiameter"]:
                            continue
                        edgeidsa = {r["edgeId"] for x in a for r in x["refs"]}
                        edgeidsb = {r["edgeId"] for x in b for r in x["refs"]}
                        # Two ends of one substantial edge do not collapse.
                        if edgeidsa.intersection(edgeidsb):
                            continue
                        clusters[i] = a+b; clusters.pop(j); changed = True; break
                    if changed:
                        break
            clusters = [c for c in clusters if len(c) > 1]
            if not clusters:
                break
            all_edges = {e["id"]: e for e in self.edges}
            moves, bindings, records = {}, [], []
            for ci, cluster in enumerate(clusters):
                key = f"completion-joint-{iteration}-{ci}"
                refs = combine(*[(1/len(cluster), [dict(row["refs"][0])]) for row in cluster])
                point = [evaluate(refs, all_edges, axis) for axis in range(2)]
                for row in cluster:
                    moves[row["key"]] = (key, point)
                bindings.append(self.bind(key, refs))
                records.append({"targetSiteKey": key, "sourceSiteKeys": [row["key"] for row in cluster], "inputs": refs,
                                "point": point, "spreadBefore": sum(distance(row["point"], point)**2 for row in cluster)/len(cluster), "spreadAfter": 0.0})
            before, after = [], []
            for old in self.edges:
                if not any(key in moves for key in old["endpointKeys"]):
                    continue
                before.append(old)
                controls, keys = deepcopy(old["controls"]), list(old["endpointKeys"])
                deltas = []
                for i, index in enumerate((0, len(controls)-1)):
                    key, target = moves.get(keys[i], (keys[i], controls[index]))
                    deltas.append([target[j]-controls[index][j] for j in range(2)])
                    keys[i] = key
                for i in range(len(controls)):
                    t = i/(len(controls)-1)
                    controls[i] = [controls[i][j]+(1-t)*deltas[0][j]+t*deltas[1][j] for j in range(2)]
                edge = self.new_edge(controls, [old], {"name": {2: "line", 3: "quadratic", 4: "cubic"}[len(controls)], "replacesPrimitive": old["primitive"].get("name")}, old["role"], keys)
                for i in range(len(controls)):
                    if i in (0, len(controls)-1) and old["siteKeys"][i] in moves:
                        continue
                    t = i/(len(controls)-1)
                    expr = [ref(old, i)]
                    for end, alpha, oldindex in [(0, 1-t, 0), (1, t, len(controls)-1)]:
                        move = moves.get(old["endpointKeys"][end])
                        if move:
                            moved = next(b["inputs"] for b in bindings if b["targetSiteKey"] == move[0])
                            expr = combine((1, expr), (alpha, moved), (-alpha, [ref(old, oldindex)]))
                    bindings.append(self.bind(edge["siteKeys"][i], expr))
                after.append(edge)
            self.patch_late("merge-nearby-proposal-joints", before, after, bindings,
                            constraint_before=sum(c["spreadBefore"] for c in records), details={"jointClusters": records, "iteration": iteration})
        # Zero-length overshoot intervals are retained, not discarded.
        for edge in list(self.edges):
            if length(sample(edge["controls"])) < 2 and edge["role"] != "cross-region":
                self.operation_counter += 1
                self.operations.append({"id": f"completion-patch-{self.operation_counter:04}", "kind": "carry-collapsed-joint-detail", "accepted": True,
                    "sourceEdgeIds": [edge["id"]], "targetEdgeIds": [edge["id"]], "sourceSpans": edge["spans"],
                    "beforeEdges": [deepcopy(edge)], "afterEdges": [deepcopy(edge)], "measurements": {"retainedExactly": True},
                    "scoreBefore": 1.0, "scoreAfter": 0.0, "thresholds": {}, "reason": "A joint merge consumed a tiny overshoot; its supported interval remains faint complement.",
                    "construction": {"complement": "faint retained detail"}, "selectionPolicy": POLICY["selectionPolicy"]})
                self.edges.remove(edge); self.residue.append(edge)
        self.capture_late("joint-convergence", "Merge nearby joints and trim overshoots")

    def _axis_refs(self):
        wall = [e for e in self.edges if e["role"] == "wall-region"]
        if wall:
            corners = [(e, i) for e in wall for i in (0, len(e["controls"])-1)]
            bottom = max(e["controls"][i][1] for e, i in corners)
            corners = [(e, i) for e, i in corners if abs(e["controls"][i][1]-bottom) < 5]
            left = min(corners, key=lambda p: p[0]["controls"][p[1]][0])
            right = max(corners, key=lambda p: p[0]["controls"][p[1]][0])
        else:
            main = self.main_contour()
            if not main:
                return []
            left, right = (main[0], 0), (main[-1], len(main[-1]["controls"])-1)
        return [ref(*left, .5), ref(*right, .5)]

    def main_contour(self):
        stroke = self.view.get("mainStrokeIndex")
        return [e for e in self.edges if e["role"] not in ("door-region", "cross-region") and any(s["strokeIndex"] == stroke for s in e.get("sourceRanges", []))]

    def regularize(self):
        self.begin_late(1)
        # A rising curve followed by a returning upright can be one complete
        # arch, rather than an accidental curve plus a disconnected wall.
        main = self.main_contour()
        for first, second in zip(main, main[1:]):
            if first not in self.edges or second not in self.edges or len(first["controls"]) < 3 or len(second["controls"]) != 2:
                continue
            a, high, b = first["controls"][0], first["controls"][-1], second["controls"][-1]
            if first["endpointKeys"][-1] != second["endpointKeys"][0] or abs(a[1]-b[1]) > 35 or min(a[1], b[1])-high[1] < 65 or abs(b[0]-a[0]) < 60:
                continue
            baseline = [ref(first, 0, .5), ref(second, 1, .5)]
            topref = [ref(first, 1)]
            base_y = (a[1]+b[1])/2
            top_y = min(p[1] for p in first["controls"])
            rise = base_y-top_y
            # y handles come from old curve control extrema; all are imports
            # from the current interpretation, not the original sample cloud.
            topindex = min(range(len(first["controls"])), key=lambda i: first["controls"][i][1])
            topref = [ref(first, topindex)]
            h_y = combine((4/3, topref), (-1/3, baseline))
            controls = [[a[0], base_y], [a[0], base_y-4*rise/3], [b[0], base_y-4*rise/3], [b[0], base_y]]
            keys = [first["endpointKeys"][0], second["endpointKeys"][-1]]
            edge = self.new_edge(controls, [first, second], {"name": "cubic-arch", "group": f"completion-arch-{self.counter+1}"}, "upper-arch-region", keys)
            bindings = [self.bind(edge["siteKeys"][0], [ref(first, 0)], baseline),
                        self.bind(edge["siteKeys"][1], [ref(first, 0)], h_y),
                        self.bind(edge["siteKeys"][2], [ref(second, 1)], h_y),
                        self.bind(edge["siteKeys"][3], [ref(second, 1)], baseline)]
            before, after, bindings = self.propagate([first, second], [edge], bindings,
                [(first["endpointKeys"][0], edge["endpointKeys"][0], edge["controls"][0]),
                 (second["endpointKeys"][-1], edge["endpointKeys"][-1], edge["controls"][-1])])
            self.patch_late("complete-supported-upper-arch", before, after, bindings,
                constraint_before=abs(a[1]-b[1])**2, details={"engine": "symmetric-open-arch", "partFrames": [{"kind": "arch", "participantEdgeIds": [first["id"], second["id"]], "viewFrame": "localFront", "vertices": [edge["siteKeys"][0], edge["siteKeys"][-1]]}]})
        # Keep the return of an outline as part of a long complete curve.
        main = self.main_contour()
        if len(main) >= 4:
            tail, previous = main[-1], main[-2]
            if len(tail["controls"]) == 2 and length(sample(tail["controls"])) < 45 and len(previous["controls"]) >= 3:
                controls = deepcopy(previous["controls"])
                controls[-1] = deepcopy(tail["controls"][-1])
                edge = self.new_edge(controls, [previous, tail], {"name": "cubic" if len(controls) == 4 else "quadratic"}, previous["role"], [previous["endpointKeys"][0], tail["endpointKeys"][-1]])
                bindings = [self.bind(key, [ref(previous, i)] if i < len(controls)-1 else [ref(tail, len(tail["controls"])-1)], mode="reference") for i, key in enumerate(edge["siteKeys"])]
                self.patch_late("absorb-short-outline-return", [previous, tail], [edge], bindings, constraint_before=length(sample(tail["controls"]))**2,
                                details={"engine": "whole-curve", "retainedSupport": True})
        self._regularize_side()
        self._carry_redundant_cross_detail()
        self.capture_late("self-regularization", "Read complete arches and a supported side frame")

    def _carry_redundant_cross_detail(self):
        crosses = {}
        for edge in self.edges:
            if edge["role"] == "cross-region":
                crosses.setdefault(edge["primitive"].get("group"), []).append(edge)
        for arms in crosses.values():
            bounds = box([p for e in arms for p in e["controls"]])
            width = bounds[2]-bounds[0]
            for edge in list(self.edges):
                if edge["role"] != "observed" or len(edge["controls"]) != 2 or length(edge["controls"]) > 1.5*width or axis_read(edge["controls"])[0] != "horizontal":
                    continue
                if not all(bounds[0]-20 <= p[0] <= bounds[2]+20 and bounds[1]-15 <= p[1] <= bounds[3]+15 for p in edge["controls"]):
                    continue
                self.operation_counter += 1
                self.operations.append({"id": f"completion-patch-{self.operation_counter:04}", "kind": "carry-redundant-cross-detail", "accepted": True,
                    "sourceEdgeIds": [edge["id"]], "targetEdgeIds": [edge["id"]], "sourceSpans": edge["spans"],
                    "beforeEdges": [deepcopy(edge)], "afterEdges": [deepcopy(edge)], "measurements": {"retainedExactly": True},
                    "scoreBefore": 1.0, "scoreAfter": 0.0, "thresholds": {"crossEnvelopeAllowance": 20},
                    "reason": "Short repeated marks inside an already supported cross envelope remain faint detail rather than extra structural bars.",
                    "construction": {"complement": "faint retained detail", "supportedCrossEdgeIds": [e["id"] for e in arms]}, "selectionPolicy": POLICY["selectionPolicy"]})
                self.edges.remove(edge); self.residue.append(edge)

    def _regularize_side(self):
        side = self.view.get("side")
        if not side:
            return
        selected = [e for e in self.edges if e["id"] in side["supportEdgeIds"]]
        if not selected:
            return
        # Side-frame rails may be skewed but retain their depth sign. Preserve
        # angular roof evidence separately rather than making all edges level.
        candidates = [e for e in selected if len(e["controls"]) == 2 and axis_read(e["controls"])[0] == "horizontal"]
        if len(candidates) < 2:
            return
        front = self.view["frontBounds"]
        sign = side["depthSign"]
        high = min(candidates, key=lambda e: sum(p[1] for p in e["controls"])/2)
        low = max(candidates, key=lambda e: sum(p[1] for p in e["controls"])/2)
        if high is low or abs(high["controls"][0][1]-low["controls"][0][1]) < 80:
            return
        # Native completion is a genuine parallelogram, rooted in the local
        # facade height plus an observed depth vector, not a frontal mirror.
        near_x = front[0] if sign < 0 else front[2]
        depth = side["depth"]
        dy = min(35, max(12, depth*.16))
        outer_x = near_x+sign*depth
        ytop, ybottom = front[1], front[3]
        vertices = [[near_x, ytop], [outer_x, ytop+dy], [outer_x, ybottom+dy], [near_x, ybottom]]
        # These finite frames are declared imported parameters. Their actual
        # participant controls are supplied below so the author preserves the
        # correspondences and source/side separation.
        side["parallelogramProposal"] = {"vertices": vertices, "depthSign": sign, "participantEdgeIds": [e["id"] for e in selected], "qualification": "Selected side-plane frame. Angular roof marks remain separately supported."}
        self.relations.append({"kind": "side-parallelogram-frame", **deepcopy(side["parallelogramProposal"]), "active": False, "proposedOnly": True})

    def synthesize(self):
        self.begin_late(2)
        axis = self._axis_refs()
        if not axis:
            self.capture_late("symmetric-synthesis", "No supported facade to synthesize")
            return
        source = self.lookup()
        axisx = evaluate(axis, source, 0)
        self.view["axisInputs"] = deepcopy(axis)
        self.view["axisX"] = axisx
        wall = [e for e in self.edges if e["role"] == "wall-region"]
        if wall:
            self._synthesize_box(wall, axis)
            roofs = [e for e in self.edges if e["role"] == "roof-region"]
            if roofs:
                # Roof edge order forms a supported open/closed triangle. A
                # closed three-edge roof keeps its base rather than adding ink.
                self._synthesize_roof(roofs, axis)
        else:
            contour = self.main_contour()
            if contour:
                self._mirror_chain(contour, axis, "synthesize-reflected-upright-outline")
        self._synthesize_side()
        self._align_parts(axis)
        self.capture_late("symmetric-synthesis", "Synthesize the facade while preserving side-view depth")
        self.steps[-1]["metrics"]["facadeAxisX"] = axisx

    def _mirror_chain(self, chain, axis, kind):
        # An open chain traversed left-to-right has a reversed counterpart.
        source = self.lookup()
        after, bindings, symmetry_pairs = [], [], []
        for i, old in enumerate(chain):
            other = chain[-i-1]
            degree = max(len(old["controls"]), len(other["controls"]))-1
            points, refs = elevated(old, degree)
            opoints, orefs = elevated(other, degree)
            opoints.reverse(); orefs.reverse()
            controls, rows = [], []
            for p, q, r, s in zip(points, opoints, refs, orefs):
                xr = combine((.5, r), (-.5, s), (1, axis))
                yr = combine((.5, r), (.5, s))
                controls.append([evaluate(xr, source, 0), evaluate(yr, source, 1)])
                rows.append((xr, yr))
            keys = [f"completion-symmetric-{old['endpointKeys'][0]}", f"completion-symmetric-{old['endpointKeys'][-1]}"]
            edge = self.new_edge(controls, [old], {"name": {2: "line", 3: "quadratic", 4: "cubic"}[len(controls)], "engine": "reflected-profile", "templateEdgeId": other["id"]}, old["role"], keys)
            edge["parents"] = list(dict.fromkeys(edge["parents"]+other.get("_rungParents", [other["id"]])+[r["edgeId"] for r in axis]))
            for key, (xr, yr) in zip(edge["siteKeys"], rows):
                bindings.append(self.bind(key, xr, yr, "mirror"))
            after.append(edge)
        for i, first in enumerate(after):
            second = after[-i-1]
            for j, key in enumerate(first["siteKeys"]):
                otherkey = second["siteKeys"][-j-1]
                if key < otherkey:
                    symmetry_pairs.append({"firstSiteKey": key, "secondSiteKey": otherkey, "axisInputs": deepcopy(axis)})
        before_points = [p for e in chain for p in sample(e["controls"])]
        residual = sum(min((p[0]+q[0]-2*evaluate(axis, source, 0))**2+(p[1]-q[1])**2 for q in before_points) for p in before_points)/max(1, len(before_points))
        constraint_ids = [e["id"] for e in chain]
        chain, after, bindings = self.propagate(list(chain), after, bindings)
        self.patch_late(kind, chain, after, bindings, constraint_before=residual,
                        details={"engine": "local-reflection", "axisInputs": deepcopy(axis), "symmetryPairs": symmetry_pairs,
                                 "constraintEdgeIds": constraint_ids,
                                 "viewFrame": "localFront" if self.view["kind"] == "oblique" else "front",
                                 "partFrames": [{"kind": "upright-reflected-outline", "participantEdgeIds": [e["id"] for e in chain], "vertices": [after[0]["siteKeys"][0], after[-1]["siteKeys"][-1]]}]})

    def _synthesize_box(self, walls, axis):
        source = self.lookup()
        corners = [(e, i) for e in walls for i in (0, len(e["controls"])-1)]
        top = min(e["controls"][i][1] for e, i in corners)
        bottom = max(e["controls"][i][1] for e, i in corners)
        tops = [(e, i) for e, i in corners if abs(e["controls"][i][1]-top) < 5]
        bottoms = [(e, i) for e, i in corners if abs(e["controls"][i][1]-bottom) < 5]
        left = min(bottoms, key=lambda row: row[0]["controls"][row[1]][0])
        right = max(bottoms, key=lambda row: row[0]["controls"][row[1]][0])
        tl = min(tops, key=lambda row: row[0]["controls"][row[1]][0])
        tr = max(tops, key=lambda row: row[0]["controls"][row[1]][0])
        toprefs = [ref(*tl, .5), ref(*tr, .5)]
        bottomrefs = [ref(*left, .5), ref(*right, .5)]
        vertices = [("completion-box-tl", [ref(*left)], toprefs), ("completion-box-tr", [ref(*right)], toprefs),
                    ("completion-box-bl", [ref(*left)], bottomrefs), ("completion-box-br", [ref(*right)], bottomrefs)]
        points = [[evaluate(x, source, 0), evaluate(y, source, 1)] for _, x, y in vertices]
        after, bindings = [], [self.bind(key, x, y) for key, x, y in vertices]
        for old in walls:
            indices = [min(range(4), key=lambda i: distance(point, points[i])) for point in (old["controls"][0], old["controls"][-1])]
            edge = self.new_edge([points[i] for i in indices], [old], {"name": "rectangle-side", "group": "completion-box", "side": len(after)}, "wall-region", [vertices[i][0] for i in indices])
            after.append(edge)
        before, after, bindings = self.propagate(list(walls), after, bindings)
        self.patch_late("synthesize-local-rectangular-facade", before, after, bindings,
                        details={"engine": "rectangle", "axisInputs": axis,
                                 "axisRectangles": [{"topLeftSiteKey": vertices[0][0], "topRightSiteKey": vertices[1][0], "bottomLeftSiteKey": vertices[2][0], "bottomRightSiteKey": vertices[3][0]}],
                                 "partFrames": [{"kind": "rectangle", "participantEdgeIds": [e["id"] for e in walls], "vertices": [v[0] for v in vertices], "viewFrame": "localFront"}],
                                 "symmetryPairs": [{"firstSiteKey": vertices[a][0], "secondSiteKey": vertices[b][0], "axisInputs": axis} for a, b in [(0, 1), (2, 3)]]})

    def _synthesize_side(self):
        side = self.view.get("side")
        if not side:
            return
        source = self.lookup()
        front = [e for e in self.edges if e["role"] == "wall-region"] or self.main_contour()
        if not front:
            return
        verticals = [e for e in front if len(e["controls"]) == 2 and axis_read(e["controls"])[0] == "vertical" and distance(*e["controls"]) > 100]
        if not verticals:
            return
        sign = side["depthSign"]
        boundary = min(verticals, key=lambda e: e["controls"][0][0]) if sign < 0 else max(verticals, key=lambda e: e["controls"][0][0])
        topindex = min(range(len(boundary["controls"])), key=lambda i: boundary["controls"][i][1])
        botindex = max(range(len(boundary["controls"])), key=lambda i: boundary["controls"][i][1])
        near_top, near_bottom = boundary["controls"][topindex], boundary["controls"][botindex]
        excluded = {e["id"] for e in front}
        candidates = [e for e in self.edges if e["id"] not in excluded and e["role"] not in ("door-region", "cross-region", "roof-region")
                      and (min(p[0] for p in sample(e["controls"])) < near_top[0]-35 if sign < 0 else max(p[0] for p in sample(e["controls"])) > near_top[0]+35)]
        side_strokes = {row["strokeIndex"] for e in candidates for row in e.get("sourceRanges", []) if len(e["controls"]) >= 3 and abs(e["controls"][-1][1]-e["controls"][0][1]) > 80}
        candidates += [e for e in self.edges if e not in candidates and e["id"] not in excluded and e["role"] not in ("door-region", "cross-region", "roof-region")
                       and any(row["strokeIndex"] in side_strokes for row in e.get("sourceRanges", []))]
        rails = [e for e in candidates if len(e["controls"]) == 2 and axis_read(e["controls"])[0] == "horizontal"]
        if len(rails) < 2:
            return
        top = min(rails, key=lambda e: abs(sum(p[1] for p in e["controls"])/2-near_top[1]))
        bottom = min((e for e in rails if e is not top), key=lambda e: abs(sum(p[1] for p in e["controls"])/2-near_bottom[1]))
        # The far wall must reach the facade's lower region. A descending
        # rear roof slope can be farther out than that wall but is a different
        # participant; selecting it would leave the real wall duplicated.
        sides = [e for e in candidates if e not in (top, bottom)
                 and abs(e["controls"][-1][1]-e["controls"][0][1]) > 80
                 and max(e["controls"][0][1], e["controls"][-1][1]) >= near_bottom[1]-50]
        if not sides:
            return
        outer = min(sides, key=lambda e: min(p[0] for p in e["controls"])) if sign < 0 else max(sides, key=lambda e: max(p[0] for p in e["controls"]))
        outerindex = min(range(len(outer["controls"])), key=lambda i: outer["controls"][i][0]) if sign < 0 else max(range(len(outer["controls"])), key=lambda i: outer["controls"][i][0])
        topfar = min(range(2), key=lambda i: top["controls"][i][0]) if sign < 0 else max(range(2), key=lambda i: top["controls"][i][0])
        botfar = min(range(2), key=lambda i: bottom["controls"][i][0]) if sign < 0 else max(range(2), key=lambda i: bottom["controls"][i][0])
        a = [ref(boundary, topindex)]
        c = [ref(boundary, botindex)]
        bx = [ref(outer, outerindex)]
        by = combine((.5, a), (.5, [ref(top, topfar)]), (.5, [ref(bottom, botfar)]), (-.5, c))
        dx, dy = combine((1, bx), (1, c), (-1, a)), combine((1, by), (1, c), (-1, a))
        vertices = [(boundary["siteKeys"][topindex], a, a), ("completion-side-far-top", bx, by),
                    (boundary["siteKeys"][botindex], c, c), ("completion-side-far-bottom", dx, dy)]
        points = [[evaluate(x, source, 0), evaluate(y, source, 1)] for _, x, y in vertices]
        after = []
        for old, pair in [(top, (0, 1)), (outer, (1, 3)), (bottom, (3, 2))]:
            if distance(old["controls"][0], points[pair[0]]) > distance(old["controls"][0], points[pair[1]]):
                pair = pair[::-1]
            after.append(self.new_edge([points[i] for i in pair], [old], {"name": "parallelogram-side", "group": "completion-side"}, "side-plane-region", [vertices[i][0] for i in pair]))
        bindings = [self.bind(key, x, y) for key, x, y in vertices]
        self.patch_late("synthesize-supported-side-parallelogram", [top, outer, bottom], after, bindings,
                        details={"engine": "parallelogram", "depthSign": sign, "viewFrame": "side",
                                 "parallelograms": [{"aSiteKey": vertices[0][0], "bSiteKey": vertices[1][0], "cSiteKey": vertices[2][0], "dSiteKey": vertices[3][0]}],
                                 "partFrames": [{"kind": "parallelogram", "participantEdgeIds": [e["id"] for e in (boundary, top, outer, bottom)], "vertices": [v[0] for v in vertices], "viewFrame": "side"}]})

    def _synthesize_roof(self, roofs, axis):
        # Match the base to the actual current facade top corners. Triangle
        # support, not a category label, licenses the peak construction.
        if len(roofs) not in (2, 3):
            return
        source = self.lookup()
        walls = [e for e in self.edges if e["role"] == "wall-region"]
        corners = [(e, i) for e in walls for i in (0, len(e["controls"])-1)]
        if not corners:
            return
        top = min(e["controls"][i][1] for e, i in corners)
        topcorners = [(e, i) for e, i in corners if e["controls"][i][1] < top+5]
        l = min(topcorners, key=lambda p: p[0]["controls"][p[1]][0])
        r = max(topcorners, key=lambda p: p[0]["controls"][p[1]][0])
        peak_edge, peak_index = min(((e, i) for e in roofs for i in (0, len(e["controls"])-1)), key=lambda v: v[0]["controls"][v[1]][1])
        vertices = [(l[0]["siteKeys"][l[1]], [ref(*l)], [ref(*l)]),
                    ("completion-synth-roof-peak", axis, [ref(peak_edge, peak_index)]),
                    (r[0]["siteKeys"][r[1]], [ref(*r)], [ref(*r)])]
        points = [[evaluate(x, source, 0), evaluate(y, source, 1)] for _, x, y in vertices]
        after, bindings = [], [self.bind(key, x, y) for key, x, y in vertices]
        for old in roofs:
            indices = [min(range(3), key=lambda i: distance(p, points[i])) for p in (old["controls"][0], old["controls"][-1])]
            if indices[0] == indices[1]:
                indices = [0, 2]
            edge = self.new_edge([points[i] for i in indices], [old], {"name": "triangle-side", "group": "completion-synth-roof", "side": len(after)}, "roof-region", [vertices[i][0] for i in indices])
            edge["parents"] = list(dict.fromkeys(edge["parents"]+[row["edgeId"] for _, x, y in vertices for row in x+y]))
            after.append(edge)
        before, after, bindings = self.propagate(list(roofs), after, bindings)
        self.patch_late("attach-centered-roof-to-facade-corners", before, after, bindings,
                        details={"engine": "triangle", "vertices": points, "vertexSiteKeys": [v[0] for v in vertices], "closed": len(roofs) == 3,
                                 "axisInputs": axis, "partFrames": [{"kind": "triangle", "participantEdgeIds": [e["id"] for e in roofs+walls], "vertices": [v[0] for v in vertices], "viewFrame": "localFront"}]})

    def _align_parts(self, axis):
        # All placement uses current construction parts. Cross count and door
        # count stay source-supported; no category default creates a feature.
        groups = {}
        for e in self.edges:
            if e["role"] in ("cross-region", "door-region"):
                groups.setdefault((e["role"], e["primitive"].get("group", e["id"])), []).append(e)
        crosses = [v for (role, _), v in groups.items() if role == "cross-region"]
        crosses.sort(key=lambda rows: box([p for e in rows for p in e["controls"]])[0])
        contour = self.main_contour()
        peak_sites = []
        if contour:
            for i in range(len(contour)-1):
                p = contour[i]["controls"][-1]
                if p[1] < contour[i]["controls"][0][1]-30 and p[1] < contour[i+1]["controls"][-1][1]-30:
                    peak_sites.append((contour[i], len(contour[i]["controls"])-1))
        for (role, _), chosen in list(groups.items()):
            source = self.lookup()
            b = box([p for e in chosen for p in e["controls"]])
            left = min(((e, i) for e in chosen for i in range(len(e["controls"]))), key=lambda row: row[0]["controls"][row[1]][0])
            right = max(((e, i) for e in chosen for i in range(len(e["controls"]))), key=lambda row: row[0]["controls"][row[1]][0])
            oldcenter = [ref(*left, .5), ref(*right, .5)]
            targetaxis = axis
            if role == "cross-region" and len(crosses) > 1 and len(peak_sites) == len(crosses):
                position = next(i for i, value in enumerate(crosses) if value is chosen)
                peak_sites.sort(key=lambda row: row[0]["controls"][row[1]][0])
                targetaxis = [ref(*peak_sites[position])]
            controls_rows, bindings = [], []
            for old in chosen:
                controls = []
                rows = []
                for i, p in enumerate(old["controls"]):
                    xr = combine((1, [ref(old, i)]), (1, targetaxis), (-1, oldcenter))
                    yr = [ref(old, i)]
                    controls.append([evaluate(xr, source, 0), p[1]])
                    rows.append((xr, yr))
                edge = self.new_edge(controls, [old], old["primitive"], old["role"], [f"completion-aligned-{k}" for k in old["endpointKeys"]])
                edge["parents"] = list(dict.fromkeys(edge["parents"]+[r["edgeId"] for r in targetaxis+oldcenter]))
                for key, (xr, yr) in zip(edge["siteKeys"], rows):
                    bindings.append(self.bind(key, xr, yr))
                controls_rows.append(edge)
            self.patch_late("align-supported-cross-to-upper-part" if role == "cross-region" else "align-supported-lower-door", chosen, controls_rows, bindings,
                            constraint_before=(evaluate(oldcenter, source, 0)-evaluate(targetaxis, source, 0))**2,
                            details={"axisInputs": targetaxis, "partKind": role, "existingPartOnly": True, "viewFrame": "localFront"})


def extend(observation, proposal):
    """Append three later construction states, preserving the original four."""
    result = deepcopy(proposal)
    completion = Completion(observation, proposal)
    completion.converge_joints()
    completion.regularize()
    completion.synthesize()
    result["steps"].extend(completion.steps)
    result["completionPolicy"] = deepcopy(POLICY)
    result["convergence"] = {"passes": len(result["steps"])-1, "stop": "Declared interpretation budget completed; original discrepancy, alternatives and complement retained.",
                             "accepted": sum(s["convergence"]["accepted"] for s in result["steps"]), "globalOptimumClaimed": False}
    return result
