"""Source-supported geometric annotation reads; never edits drawing structure.

The fitted finite source profiles and acquired interval identities remain the
basis. Names are display summaries of rays, profile correspondences and measured
residuals. The native annotation adapter transports those participants and reads;
this module does not establish contact, primitive intent or learned probability.
"""
from __future__ import annotations

import itertools
import math


POLICIES = {
    "observed-fit": {"angleDegrees": 10.0, "proximity": 4.0, "linearity": 4.0,
                     "reflectionDistance": 6.0, "repetitionDistance": 5.0},
    "gentle-priors": {"angleDegrees": 20.0, "proximity": 10.0, "linearity": 7.0,
                      "reflectionDistance": 14.0, "repetitionDistance": 12.0},
    "stronger-priors": {"angleDegrees": 32.0, "proximity": 20.0, "linearity": 12.0,
                        "reflectionDistance": 28.0, "repetitionDistance": 24.0},
}
MIN_SPAN = 12.0


def point(controls, t):
    values = [list(p) for p in controls]
    while len(values) > 1:
        values = [[(1-t)*a[j]+t*b[j] for j in range(2)] for a, b in zip(values, values[1:])]
    return values[0]


def tangent(controls, t):
    degree = len(controls)-1
    return point([[degree*(b[j]-a[j]) for j in range(2)] for a, b in zip(controls, controls[1:])], t)


def length(vector):
    return math.hypot(*vector)


def angle(vector):
    return math.degrees(math.atan2(vector[1], vector[0])) % 360


def separation(a, b):
    return abs((a-b+180) % 360-180)


def axis_separation(a, b):
    difference = separation(a, b)
    return min(difference, 180-difference)


def mean(points):
    return [sum(p[j] for p in points)/len(points) for j in range(2)]


def diameter(points):
    return max((math.dist(a, b) for a, b in itertools.combinations(points, 2)), default=0.0)


def profile(controls, count=16):
    return [point(controls, i/count) for i in range(count+1)]


def profile_length(controls):
    ps = profile(controls)
    return sum(math.dist(a, b) for a, b in zip(ps, ps[1:]))


def _entry(key, kind, label, spans, xy, measured, thresholds, qualification):
    return {"id": key, "kind": kind, "label": label, "spans": list(dict.fromkeys(spans)),
            "point": xy, "measured": measured, "thresholds": thresholds,
            "qualification": qualification}


def _line_read(span, policy):
    controls = span["controls"]
    vector = [controls[-1][j]-controls[0][j] for j in range(2)]
    heading = angle(vector) if length(vector) > 1e-9 else None
    linearity = span["lineError"]["max"]
    accepted_line = heading is not None and linearity <= policy["linearity"]
    candidates = []
    if accepted_line:
        candidates.append({"label": "straight", "residual": linearity, "threshold": policy["linearity"]})
    if heading is not None:
        for label, axis in (("horizontal", 0), ("vertical", 90), ("rising diagonal", 135), ("falling diagonal", 45)):
            residual = axis_separation(heading, axis)
            if accepted_line and residual <= policy["angleDegrees"]:
                candidates.append({"label": label, "residual": residual, "threshold": policy["angleDegrees"]})
    start, end = tangent(controls, 0), tangent(controls, 1)
    turn = (angle(end)-angle(start)+180) % 360-180 if length(start)*length(end) > 1e-12 else None
    midpoint = point(controls, .5)
    sagitta = ((vector[0]*(midpoint[1]-controls[0][1])-vector[1]*(midpoint[0]-controls[0][0]))/length(vector)
               if length(vector) > 1e-9 else None)
    bend = ("unresolved" if sagitta is None else "near-straight" if abs(sagitta) <= policy["linearity"]
            else "right" if sagitta > 0 else "left")
    if len(controls) > 2:
        candidates.append({"label": "smooth finite arc", "residual": span["fitError"]["max"],
                           "threshold": None})
    directional = [c["label"] for c in candidates if c["label"] not in ("straight", "smooth finite arc")]
    label = " / ".join(directional) if directional else "curve" if len(controls) > 2 else "line" if heading is not None else "point / unresolved direction"
    row = _entry("line-"+span["id"], "line", label, [span["id"]], point(controls, .5),
                 {"headingDegrees": heading, "linearityMax": linearity,
                  "fitRms": span["fitError"]["rms"], "length": profile_length(controls),
                  "tangentTurnDegrees": turn, "signedSagitta": sagitta, "finiteDegree": len(controls)-1},
                 {"angleDegrees": policy["angleDegrees"], "linearity": policy["linearity"]},
                 "Measured source interval. Direction and straightness are tolerance reads; a finite curve is not an exact circular arc.")
    row.update(sourceRange={key: span[key] for key in ("strokeIndex", "startIndex", "endIndex")},
               controls=[list(p) for p in controls], tangents={"start": start, "end": end}, candidates=candidates,
               bend={"sense": bend, "signedSagitta": sagitta,
                     "qualification": "Left/right relative to acquired direction in the page frame (y increases downwards); outward/inward is not claimed without a region basis."})
    return row


def _projection(xy, controls):
    samples = profile(controls, 24)
    best = None
    for i, (a, b) in enumerate(zip(samples, samples[1:])):
        vector = [b[j]-a[j] for j in range(2)]
        square = sum(v*v for v in vector)
        fraction = max(0, min(1, sum((xy[j]-a[j])*vector[j] for j in range(2))/square)) if square else 0
        projected = [a[j]+fraction*vector[j] for j in range(2)]
        candidate = (math.dist(xy, projected), (i+fraction)/24, projected)
        if best is None or candidate[0] < best[0]:
            best = candidate
    return best


def _intersections(first, second):
    a_points = profile(first, 24) if len(first) > 2 else first
    b_points = profile(second, 24) if len(second) > 2 else second
    result = []
    for i, (a, b) in enumerate(zip(a_points, a_points[1:])):
        u = [b[j]-a[j] for j in range(2)]
        for k, (c, d) in enumerate(zip(b_points, b_points[1:])):
            v = [d[j]-c[j] for j in range(2)]
            denominator = u[0]*v[1]-u[1]*v[0]
            if abs(denominator) < 1e-10:
                continue
            w = [c[j]-a[j] for j in range(2)]
            t, s = (w[0]*v[1]-w[1]*v[0])/denominator, (w[0]*u[1]-w[1]*u[0])/denominator
            if 0 <= t <= 1 and 0 <= s <= 1:
                xy = [a[j]+t*u[j] for j in range(2)]
                if not any(math.dist(xy, previous[2]) < 1 for previous in result):
                    result.append(((i+t)/(len(a_points)-1), (k+s)/(len(b_points)-1), xy))
    return result


def _ray_pairs(rays):
    result = []
    for first, second in itertools.combinations(range(len(rays)), 2):
        difference = separation(rays[first]["angleDegrees"], rays[second]["angleDegrees"])
        result.append({"first": first, "second": second, "opposedResidualDegrees": abs(180-difference),
                       "perpendicularResidualDegrees": abs(90-difference), "sameDirectionResidualDegrees": difference})
    return result


def classify_rays(rays, tolerance):
    pairs = _ray_pairs(rays)
    opposed = [pair for pair in pairs if pair["opposedResidualDegrees"] <= tolerance]
    disjoint_opposed = any({a["first"], a["second"]}.isdisjoint({b["first"], b["second"]})
                           for a, b in itertools.combinations(opposed, 2))
    count = len(rays)
    coincident = [pair for pair in pairs if pair["sameDirectionResidualDegrees"] <= 1.0]
    if coincident:
        label, residue = "coincident rays", min(p["sameDirectionResidualDegrees"] for p in coincident)
    elif count == 1:
        label, residue = "endpoint", 0.0
    elif count == 2:
        label = "I" if opposed else "L"
        residue = pairs[0]["opposedResidualDegrees"] if opposed else pairs[0]["perpendicularResidualDegrees"]
    elif count == 3:
        label = "T" if opposed else "Y"
        residue = min((p["opposedResidualDegrees"] for p in pairs), default=0)
    elif count == 4:
        label = "X" if disjoint_opposed else "K" if opposed else "four-way"
        residue = max((p["opposedResidualDegrees"] for p in opposed), default=0)
    else:
        label, residue = "complex" if count else "unresolved", 0.0
    geometric_fit = label in ("I", "T", "X", "K", "coincident rays")
    candidates = [{"label": label, "residualDegrees": residue if geometric_fit else None,
                   "thresholdDegrees": (1.0 if label == "coincident rays" else tolerance) if geometric_fit else None,
                   "qualification": "Angular fit" if geometric_fit else "Directional topology; no ideal angle is asserted"}]
    if count == 2 and label == "L":
        candidates.append({"label": "right-angle L" if residue <= tolerance else "angled L",
                           "residualDegrees": residue, "thresholdDegrees": tolerance if residue <= tolerance else None,
                           "qualification": "Right-angle fit" if residue <= tolerance else "Measured departure from a right angle; no right-angle claim"})
    if count == 2 and label == "I":
        candidates.append({"label": "smooth continuation", "residualDegrees": residue, "thresholdDegrees": tolerance})
    return label, candidates, pairs


def _joint(participants, by_id, policy, key, support_kind):
    unique = {(p["spanId"], round(p["t"], 7)): p for p in participants}
    participants = list(unique.values())
    points = [point(by_id[p["spanId"]]["controls"], p["t"]) for p in participants]
    rays = []
    for participant in participants:
        span_id, t = participant["spanId"], participant["t"]
        vector = tangent(by_id[span_id]["controls"], t)
        if length(vector) <= 1e-9:
            continue
        for toward, sign in (("start", -1), ("end", 1)):
            if toward == "start" and t <= 1e-8 or toward == "end" and t >= 1-1e-8:
                continue
            directed = [sign*x for x in vector]
            rays.append({"spanId": span_id, "t": t, "toward": toward,
                         "angleDegrees": angle(directed), "vector": directed})
    rays.sort(key=lambda ray: (ray["angleDegrees"], ray["spanId"], ray["toward"]))
    label, candidates, pairs = classify_rays(rays, policy["angleDegrees"])
    shared = [p["sourceSite"] for p in participants]
    actual_shared = bool(shared) and all(site is not None and site == shared[0] for site in shared)
    gap = diameter(points)
    row = _entry(key, "joint", label, [p["spanId"] for p in participants], mean(points),
                 {"rayCount": len(rays), "gap": gap,
                  "angleGapsDegrees": [(rays[(i+1)%len(rays)]["angleDegrees"]-ray["angleDegrees"]) % 360 for i, ray in enumerate(rays)]},
                 {"angleDegrees": policy["angleDegrees"], "proximity": policy["proximity"], "coincidentDegrees": 1.0},
                 "Shared acquired sample" if actual_shared else "Geometric proximity/intersection candidate; separate acquired sites remain separate and no contact is inserted.")
    row.update(participants=participants, rays=rays, rayPairs=pairs, rayCount=len(rays), candidates=candidates,
               observedSharedSite=actual_shared, gap=gap, supportKind=support_kind)
    return row


def _joints(observation, by_id, policy):
    sites = {}
    for span in by_id.values():
        if profile_length(span["controls"]) < 1e-8:
            continue
        for t, index in ((0.0, span["startIndex"]), (1.0, span["endIndex"])):
            key = (span["strokeIndex"], index)
            site = sites.setdefault(key, {"key": key, "point": point(span["controls"], t), "participants": []})
            site["participants"].append({"spanId": span["id"], "t": t,
                                         "sourceSite": {"strokeIndex": key[0], "sampleIndex": key[1]}})
    sites = list(sites.values())
    clusters = set()
    # Complete-link neighborhoods cannot turn a chain of small gaps into an
    # arbitrarily large inferred junction. Source identities are never merged.
    for i, site in enumerate(sites):
        selected = [i]
        neighbors = sorted((math.dist(site["point"], other["point"]), j) for j, other in enumerate(sites) if j != i)
        for distance, j in neighbors:
            if distance > policy["proximity"]:
                break
            if all(math.dist(sites[j]["point"], sites[k]["point"]) <= policy["proximity"] for k in selected):
                selected.append(j)
        clusters.add(tuple(sorted(selected)))
    maximal = [cluster for cluster in sorted(clusters) if not any(set(cluster) < set(other) for other in clusters)]
    rows = []
    for cluster in maximal:
        participants = [p for i in cluster for p in sites[i]["participants"]]
        rows.append(_joint(participants, by_id, policy, f"joint-endpoints-{len(rows):03}", "endpoint neighborhood"))
    # Endpoint -> interior reads use finite profile samples and local tangents.
    for site in sites:
        own = {p["spanId"] for p in site["participants"]}
        for target in by_id.values():
            if target["id"] in own or profile_length(target["controls"]) < MIN_SPAN:
                continue
            gap, t, _ = _projection(site["point"], target["controls"])
            if gap <= policy["proximity"] and min(t, 1-t)*profile_length(target["controls"]) >= 4:
                participants = site["participants"] + [{"spanId": target["id"], "t": t, "sourceSite": None}]
                rows.append(_joint(participants, by_id, policy, f"joint-interior-{len(rows):03}", "endpoint near interior"))
    for first, second in itertools.combinations(by_id.values(), 2):
        if min(profile_length(first["controls"]), profile_length(second["controls"])) < MIN_SPAN:
            continue
        for t, s, xy in _intersections(first["controls"], second["controls"]):
            if min(t, 1-t)*profile_length(first["controls"]) < 4:
                continue
            if min(s, 1-s)*profile_length(second["controls"]) < 4:
                continue
            participants = [{"spanId": first["id"], "t": t, "sourceSite": None},
                            {"spanId": second["id"], "t": s, "sourceSite": None}]
            rows.append(_joint(participants, by_id, policy, f"joint-crossing-{len(rows):03}", "finite profile crossing"))
    for stroke in observation["strokes"]:
        if not stroke["spans"] or len(stroke["points"]) < 3:
            continue
        gap = math.dist(stroke["points"][0], stroke["points"][-1])
        travel = sum(profile_length(by_id[key]["controls"]) for key in stroke["spans"])
        if gap <= policy["proximity"] and travel >= max(40, 6*policy["proximity"]):
            row = _entry(f"joint-cycle-{stroke['index']:03}", "closed-cycle", "O", stroke["spans"],
                         mean(stroke["points"]), {"gap": gap, "traversalLength": travel},
                         {"proximity": policy["proximity"]},
                         "Near-closed acquired traversal. O is a cycle candidate, not a two-ray junction; the endpoint gap and separate indices remain visible.")
            row.update(participants=[{"spanId": stroke["spans"][0], "t": 0.0, "sourceSite": {"strokeIndex": stroke["index"], "sampleIndex": 0}},
                                     {"spanId": stroke["spans"][-1], "t": 1.0, "sourceSite": {"strokeIndex": stroke["index"], "sampleIndex": len(stroke["points"])-1}}],
                       rays=[], rayPairs=[], rayCount=0, candidates=[{"label": "O", "residualDegrees": 0, "thresholdDegrees": None}],
                       observedSharedSite=False, gap=gap, supportKind="near-closed acquired traversal")
            rows.append(row)
    # Multiple local reads at one place are retained as competing supported
    # scopes; exact duplicate participant reads are not duplicated.
    unique = {}
    for row in rows:
        key = (row["kind"], tuple(sorted((p["spanId"], round(p["t"], 3)) for p in row["participants"])))
        unique.setdefault(key, row)
    return list(unique.values())


def _mirror_error(first, second, axis_index, axis_value):
    a, b = profile(first, 8), profile(second, 8)
    reflected = [[2*axis_value-p[j] if j == axis_index else p[j] for j in range(2)] for p in a]
    possibilities = []
    for reversed_order in (False, True):
        target = b[::-1] if reversed_order else b
        errors = [math.dist(p, q) for p, q in zip(reflected, target)]
        possibilities.append((math.sqrt(sum(e*e for e in errors)/len(errors)), max(errors), reversed_order))
    return min(possibilities)


def _axis(index, value, scene_bounds, observer_box=None):
    observer_box = observer_box or [0, 0, 512, 512]
    direction = 1-index
    start, end = scene_bounds[direction], scene_bounds[direction+2]
    if abs(end-start) <= 1e-9:
        # An observer-frame axis remains usable for a flat drawing. These are
        # axis witnesses, not invented source geometry or an extra drawn line.
        start, end = observer_box[direction], observer_box[direction]+observer_box[direction+2]
    if index == 0:
        return {"from": [value, start], "to": [value, end]}
    return {"from": [start, value], "to": [end, value]}


def _symmetries(observation, by_id, policy):
    if observation["bounds"] is None:
        return [], {"localCandidates": 0, "localReported": 0}
    eligible = [span for span in by_id.values() if profile_length(span["controls"]) >= MIN_SPAN]
    local = []
    for first, second in itertools.combinations(eligible, 2):
        mid_a, mid_b = point(first["controls"], .5), point(second["controls"], .5)
        for index in (0, 1):
            if abs(mid_a[index]-mid_b[index]) < MIN_SPAN or abs(mid_a[1-index]-mid_b[1-index]) > policy["reflectionDistance"]:
                continue
            value = (mid_a[index]+mid_b[index])/2
            rms, maximum, reverse = _mirror_error(first["controls"], second["controls"], index, value)
            if maximum <= policy["reflectionDistance"]:
                local.append({"first": first["id"], "second": second["id"], "index": index,
                              "value": value, "rms": rms, "max": maximum, "reverse": reverse,
                              "extent": profile_length(first["controls"])+profile_length(second["controls"])})
    rows = []
    selected = sorted(local, key=lambda row: (row["max"], -row["extent"], row["first"], row["second"]))
    for item in selected:
        axis = _axis(item["index"], item["value"], observation["bounds"], observation.get("normalization", {}).get("targetBox"))
        row = _entry(f"mirror-local-{len(rows):03}", "reflection", "Local mirror pair", [item["first"], item["second"]],
                     mean([point(by_id[item["first"]]["controls"], .5), point(by_id[item["second"]]["controls"], .5)]),
                     {"rms": item["rms"], "max": item["max"], "coverage": 1.0, "scope": "selected pair only"},
                     {"reflectionDistance": policy["reflectionDistance"]},
                     "A local reflected pair under an explicit page-frame axis; it does not establish symmetry of the whole drawing.")
        row.update(axis=axis, correspondences=[{"source": item["first"], "target": item["second"],
                                               "reversed": item["reverse"], "error": item["max"]}], unmatchedSpanIds=[])
        rows.append(row)
    scene_bounds = observation["bounds"]
    for index in (0, 1):
        value = (scene_bounds[index]+scene_bounds[index+2])/2
        options = []
        for i, first in enumerate(eligible):
            for second in eligible[i:]:
                rms, maximum, reverse = _mirror_error(first["controls"], second["controls"], index, value)
                if maximum <= policy["reflectionDistance"]:
                    options.append((maximum, rms, first["id"], second["id"], reverse))
        used, correspondences = set(), []
        for maximum, rms, first, second, reverse in sorted(options):
            if first not in used and second not in used:
                used.update((first, second))
                correspondences.append({"source": first, "target": second, "reversed": reverse, "error": maximum})
        total_length = sum(profile_length(span["controls"]) for span in by_id.values())
        coverage = sum(profile_length(by_id[key]["controls"]) for key in sorted(used))/total_length if total_length else 0
        # Retain an explicitly incomplete whole-scope read even when only a
        # minority matches, so unmatched participation cannot disappear.
        if correspondences:
            all_ids = list(by_id)
            unmatched = [key for key in all_ids if key not in used]
            axis = _axis(index, value, scene_bounds, observation.get("normalization", {}).get("targetBox"))
            label = ("Whole-scope" if not unmatched else "Partial")+(" vertical reflection" if index == 0 else " horizontal reflection")
            row = _entry(f"mirror-whole-{index}", "reflection", label, sorted(used), mean([axis["from"], axis["to"]]),
                         {"coverage": coverage, "unmatchedFraction": 1-coverage,
                          "max": max(pair["error"] for pair in correspondences), "scope": "whole drawing"},
                         {"reflectionDistance": policy["reflectionDistance"]},
                         "Whole drawing tested against its bounding-frame middle axis. Unmatched intervals remain explicit; partial support is not a whole-drawing symmetry claim.")
            row.update(axis=axis, correspondences=correspondences, unmatchedSpanIds=unmatched)
            rows.append(row)
    return rows, {"localCandidates": len(local), "localReported": len(selected),
                  "selectionRule": "All computed local pairs retained, ordered by maximum reflection residue then supported length; display density is a viewer choice."}


def _relations(observation, by_id, lines, joints, policy):
    line_map = {row["spans"][0]: row for row in lines}
    eligible = [span for span in by_id.values() if profile_length(span["controls"]) >= MIN_SPAN]
    candidates = []
    for first, second in itertools.combinations(eligible, 2):
        a, b = line_map[first["id"]], line_map[second["id"]]
        separation_distance = math.dist(a["point"], b["point"])
        max_distance = min(200, (a["measured"]["length"]+b["measured"]["length"])/2+40)
        if separation_distance > max_distance:
            continue
        lengths = [a["measured"]["length"], b["measured"]["length"]]
        if min(lengths)/max(lengths) < .25:
            continue
        if all(row["measured"]["linearityMax"] <= policy["linearity"] and row["measured"]["headingDegrees"] is not None for row in (a, b)):
            difference = axis_separation(a["measured"]["headingDegrees"], b["measured"]["headingDegrees"])
            for kind, residue in (("parallel", difference), ("perpendicular", 90-difference)):
                if residue <= policy["angleDegrees"]:
                    candidates.append(_entry(f"relation-{kind}-{first['id']}-{second['id']}", kind, kind.capitalize()+" candidate",
                        [first["id"], second["id"]], mean([a["point"], b["point"]]),
                        {"angleResidualDegrees": residue, "centerDistance": separation_distance},
                        {"angleDegrees": policy["angleDegrees"], "maximumCenterDistance": max_distance},
                        "Relation between measured source profiles; parallel/perpendicular here are page geometry reads, not shared topology or Gojo's general orthogonal concept."))
        # Translation correspondence preserves traversal/reversal explicitly.
        first_profile, second_profile = profile(first["controls"], 8), profile(second["controls"], 8)
        possibilities = []
        for reverse in (False, True):
            target = second_profile[::-1] if reverse else second_profile
            delta = mean([[q[j]-p[j] for j in range(2)] for p, q in zip(first_profile, target)])
            errors = [math.dist([p[j]+delta[j] for j in range(2)], q) for p, q in zip(first_profile, target)]
            possibilities.append((max(errors), math.sqrt(sum(e*e for e in errors)/len(errors)), reverse, delta))
        maximum, rms, reverse, delta = min(possibilities)
        if maximum <= policy["repetitionDistance"] and length(delta) >= MIN_SPAN:
            row = _entry(f"relation-repeat-{first['id']}-{second['id']}", "repeated", "Translated profile pair",
                         [first["id"], second["id"]], mean([a["point"], b["point"]]),
                         {"max": maximum, "rms": rms, "translation": delta, "centerDistance": separation_distance},
                         {"repetitionDistance": policy["repetitionDistance"]},
                         "Two finite profiles admit an approximate translation correspondence; this is not proof of repeated object identity or a learned repeat rule.")
            row["correspondences"] = [{"source": first["id"], "target": second["id"], "reversed": reverse, "error": maximum}]
            candidates.append(row)
    for joint in joints:
        if joint["kind"] != "joint" or joint["rayCount"] != 2:
            continue
        first, second = joint["rays"]
        residue = joint["rayPairs"][0]["opposedResidualDegrees"]
        if residue <= policy["angleDegrees"]:
            row = _entry("relation-tangent-"+joint["id"], "tangent", "Tangent continuation", joint["spans"], joint["point"],
                         {"angleResidualDegrees": residue, "gap": joint["gap"]},
                         {"angleDegrees": policy["angleDegrees"], "proximity": policy["proximity"]},
                         "Local endpoint derivative rays, not whole-curve chords, support a smooth-continuation candidate.")
            row["jointId"] = joint["id"]
            candidates.append(row)
        # Two downward-diverging rays near the upper portion support a roof-like
        # slope read. This condition never consults a source category or ID.
        if observation["bounds"] and all(ray["vector"][1] > 0 for ray in (first, second)) and first["vector"][0]*second["vector"][0] < 0:
            top, bottom = observation["bounds"][1], observation["bounds"][3]
            relative_y = (joint["point"][1]-top)/(bottom-top) if bottom > top else 1
            if relative_y <= .6:
                candidates.append(_entry("relation-slopes-"+joint["id"], "roof-slopes", "Upper converging slopes", joint["spans"], joint["point"],
                                         {"relativeHeight": relative_y, "openingDegrees": joint["rayPairs"][0]["sameDirectionResidualDegrees"]},
                                         {"maximumRelativeHeight": .6},
                                         "Two observed slopes descend away from an upper common/proximate apex. Roof is an expectation label; no building category or roof primitive is imposed."))
    selected = []
    counts = {}
    for kind in ("parallel", "perpendicular", "repeated", "tangent", "roof-slopes"):
        group = [row for row in candidates if row["kind"] == kind]
        group.sort(key=lambda row: (row["measured"].get("angleResidualDegrees", row["measured"].get("max", 0)),
                                   row["measured"].get("centerDistance", 0), row["id"]))
        counts[kind] = {"candidates": len(group), "reported": len(group)}
        selected.extend(group)
    return selected, counts


def _native_reads(result, native_variant, by_id, policy):
    if native_variant is None:
        return
    edges = {edge["id"]: edge for edge in native_variant["analysis"]["edges"]}
    mapping = native_variant["observationMapping"]["spanEdges"]
    native = {key: edges[ids[0]]["controls"] for key, ids in mapping.items() if len(ids) == 1 and ids[0] in edges}
    for row in itertools.chain(result["lines"], result["joints"], result["symmetries"], result["relations"]):
        read = {"status": "unavailable", "point": None, "supportCenter": None,
                "basis": "same source participants projected onto native replay controls",
                "qualification": "A separate measured read of the mapped proposal; no contact or shared site is introduced."}
        row["read"] = read
        if not all(key in native for key in row["spans"]):
            continue
        read["supportCenter"] = mean([point(native[key], .5) for key in row["spans"]])
        if row["kind"] in ("joint", "closed-cycle"):
            locations = [point(native[p["spanId"]], p["t"]) for p in row["participants"]]
            gap = diameter(locations)
            rays = [{**ray, "vector": [(-1 if ray["toward"] == "start" else 1)*v for v in tangent(native[ray["spanId"]], ray["t"])]}
                    for ray in row["rays"]]
            for ray in rays:
                ray["angleDegrees"] = angle(ray["vector"])
            label = classify_rays(rays, policy["angleDegrees"])[0] if row["kind"] == "joint" else "O"
            supported = gap <= policy["proximity"] and label == row["label"]
            read.update(measured={"gap": gap, "rayLabel": label}, supportCenter=mean(locations))
        elif row["kind"] == "reflection":
            axis = row["axis"]
            index = 0 if axis["from"][0] == axis["to"][0] else 1
            value = axis["from"][index]
            maximum = max((_mirror_error(native[p["source"]], native[p["target"]], index, value)[1]
                           for p in row["correspondences"]), default=0)
            supported = maximum <= policy["reflectionDistance"]
            read["measured"] = {"maxReflectionError": maximum}
            if supported:
                read["axis"] = axis
        elif row["kind"] == "line":
            span_id = row["spans"][0]
            controls = native[span_id]
            vector = [controls[-1][j]-controls[0][j] for j in range(2)]
            heading = angle(vector) if length(vector) else None
            observed = row["measured"]["headingDegrees"]
            residual = axis_separation(heading, observed) if heading is not None and observed is not None else None
            supported = residual is not None and residual <= policy["angleDegrees"]
            read["measured"] = {"headingDegrees": heading, "directionChangeDegrees": residual}
        else:
            # Support highlighting is safe without pretending a source relation
            # was re-established on the deformed construction.
            read["qualification"] = "Mapped participant support only; this relation has not been re-established on the native proposal."
            continue
        read["status"] = "within-tolerance" if supported else "outside-tolerance"
        read["point"] = read["supportCenter"] if supported else None


def annotate(observation, mode, native_variant=None):
    if mode not in POLICIES:
        raise ValueError("Unknown interpretation mode")
    policy = {"mode": mode, **POLICIES[mode], "minSpanLength": MIN_SPAN, "coincidentDegrees": 1.0,
              "profileSamples": 24, "basis": "512-unit normalized page; fitted finite source curves"}
    by_id = {span["id"]: span for span in observation["spans"]}
    if len(by_id) != len(observation["spans"]):
        raise ValueError("Source interval identities must be unique")
    lines = [_line_read(span, policy) for span in by_id.values()]
    joints = _joints(observation, by_id, policy)
    symmetries, mirror_selection = _symmetries(observation, by_id, policy)
    relations, relation_selection = _relations(observation, by_id, lines, joints, policy)
    result = {"schema": "gojo.quickdraw-annotations.v1", "sourceId": observation["sourceId"], "mode": mode,
              "policy": policy, "lines": lines, "joints": joints, "symmetries": symmetries, "relations": relations,
              "counts": {"lines": len(lines), "joints": len(joints), "symmetries": len(symmetries), "relations": len(relations),
                         "jointLabels": {label: sum(row["label"] == label for row in joints) for label in sorted({row["label"] for row in joints})}},
              "selection": {"mirrors": mirror_selection, "relations": relation_selection},
              "qualification": "Exploratory annotation of measured source-fitted finite profiles. Thresholds widen with interpretation mode; no drawing coordinate, acquisition identity or construction program is changed. Candidates and residue are not learned probabilities."}
    _native_reads(result, native_variant, by_id, policy)
    return result
