"""Finite geometric observations over ordered Quick, Draw! samples.

This is an acquisition adapter, not a learned model or the canonical Gojo
representation. The native construction importer owns connected geometry.
Candidate names below are review reads backed by source intervals, measured
errors and explicit thresholds; they do not join sites or repair the drawing.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path


DEFAULTS = {
    "targetExtent": 512, "margin": 32,
    "polylineTolerance": 6.0, "curveTolerance": 7.0,
    "curveMinimumImprovement": 0.30, "curveMaxPolygonTurnDegrees": 72.0,
    "axisToleranceDegrees": 18.0, "cornerMinimumDegrees": 28.0,
    "rightAngleToleranceDegrees": 20.0, "nearJoinTolerance": 10.0,
    "crossMaxSpanFraction": 0.40, "crossMinSpanFraction": 0.025,
    "crossUpperFraction": 0.56, "crossRightAngleToleranceDegrees": 30.0,
    "crossMaximumLineError": 8.0,
    "crossMinimumArmFraction": 0.10,
    "closedGapFraction": 0.25, "ellipseRadialRmsTolerance": 0.18,
}


def distance(a, b):
    return math.hypot(b[0] - a[0], b[1] - a[1])


def bounds(points):
    return [min(p[0] for p in points), min(p[1] for p in points),
            max(p[0] for p in points), max(p[1] for p in points)]


def segment_distance(p, a, b):
    dx, dy = b[0] - a[0], b[1] - a[1]
    square = dx * dx + dy * dy
    t = max(0.0, min(1.0, ((p[0]-a[0])*dx + (p[1]-a[1])*dy) / square)) if square else 0.0
    return math.hypot(p[0]-a[0]-t*dx, p[1]-a[1]-t*dy)


def _error(errors):
    return {"rms": math.sqrt(sum(x*x for x in errors)/len(errors)), "max": max(errors)}


def polyline_indices(points, tolerance):
    """RDP in acquired order, with explicit turnaround support and raw indices."""
    if len(points) < 2:
        return [0]
    keep = {0, len(points)-1}
    pending = [(0, len(points)-1)]
    while pending:
        first, last = pending.pop()
        if last <= first + 1:
            continue
        errors = [(segment_distance(points[i], points[first], points[last]), i)
                  for i in range(first+1, last)]
        error, split = max(errors)
        # A collinear retrace can have zero perpendicular error. Preserve a
        # measured reversal larger than this read's tolerance as an interval
        # boundary, instead of treating sample order as disposable geometry.
        chord = distance(points[first], points[last])
        if chord > 1e-12:
            ux, uy = ((points[last][j]-points[first][j])/chord for j in range(2))
            peak, peak_index = 0.0, first
            for i in range(first+1, last+1):
                progress = (points[i][0]-points[first][0])*ux+(points[i][1]-points[first][1])*uy
                if peak-progress > tolerance and peak_index > first:
                    error, split = max(error, peak-progress), peak_index
                    break
                if progress > peak:
                    peak, peak_index = progress, i
        if error > tolerance:
            keep.add(split)
            pending.extend(((first, split), (split, last)))
    return sorted(keep)


def _quadratic(points):
    lengths = [0.0]
    for a, b in zip(points, points[1:]):
        lengths.append(lengths[-1]+distance(a, b))
    if lengths[-1] <= 1e-12:
        return [points[0], points[0], points[-1]], {"rms": 0.0, "max": 0.0}
    ts = [s/lengths[-1] for s in lengths]
    weights = [2*t*(1-t) for t in ts]
    denominator = sum(w*w for w in weights)
    if denominator <= 1e-12:
        control = [(points[0][j]+points[-1][j])/2 for j in range(2)]
    else:
        control = [sum(w*(p[j]-(1-t)**2*points[0][j]-t*t*points[-1][j])
                       for p, t, w in zip(points, ts, weights))/denominator for j in range(2)]
    fitted = [[(1-t)**2*points[0][j]+2*t*(1-t)*control[j]+t*t*points[-1][j]
               for j in range(2)] for t in ts]
    return [points[0], control, points[-1]], _error([distance(a, b) for a, b in zip(points, fitted)])


def _turn(a, b, c):
    u, v = (b[0]-a[0], b[1]-a[1]), (c[0]-b[0], c[1]-b[1])
    if math.hypot(*u) < 1e-12 or math.hypot(*v) < 1e-12:
        return 0.0
    return math.degrees(math.atan2(u[0]*v[1]-u[1]*v[0], u[0]*v[0]+u[1]*v[1]))


def _orientation(a, b, tolerance):
    dx, dy = b[0]-a[0], b[1]-a[1]
    if math.hypot(dx, dy) < 1e-9:
        return {"orientation": "unresolved", "axisDeviation": None, "axisDelta": None}
    angle = math.degrees(math.atan2(abs(dy), abs(dx)))
    horizontal, vertical = angle, 90-angle
    orientation = "horizontal" if horizontal <= tolerance else "vertical" if vertical <= tolerance else "angular"
    return {"orientation": orientation, "axisDeviation": min(horizontal, vertical),
            "axisDelta": dy if orientation == "horizontal" else dx if orientation == "vertical" else None}


def fit_stroke(points, stroke_index, config=None):
    config = DEFAULTS | (config or {})
    if not points:
        return []
    anchors = polyline_indices(points, config["polylineTolerance"])
    pairs = []
    k = 0
    while k < len(anchors)-1:
        chosen = k+1
        chosen_controls = [points[anchors[k]], points[anchors[chosen]]]
        chosen_error = None
        # Several shallow turns of one sign can support an arc. A sharp corner
        # or reversed turn keeps its own acquired boundary.
        for end in range(k+2, min(len(anchors), k+6)):
            turns = [_turn(points[anchors[j-1]], points[anchors[j]], points[anchors[j+1]])
                     for j in range(k+1, end)]
            if any(abs(t) > config["curveMaxPolygonTurnDegrees"] for t in turns):
                break
            if any(t < -3 for t in turns) and any(t > 3 for t in turns):
                break
            subset = points[anchors[k]:anchors[end]+1]
            controls, error = _quadratic(subset)
            line = _error([segment_distance(p, subset[0], subset[-1]) for p in subset])
            if (error["max"] <= config["curveTolerance"] and line["rms"] > 1e-9
                    and error["rms"] <= line["rms"]*(1-config["curveMinimumImprovement"])):
                chosen, chosen_controls, chosen_error = end, controls, error
        pairs.append((anchors[k], anchors[chosen], chosen_controls, chosen_error))
        k = chosen
    if not pairs:
        pairs = [(0, 0, [points[0], points[0]], {"rms": 0.0, "max": 0.0})]
    spans = []
    for i, (first, last, controls, curve_error) in enumerate(pairs):
        subset = points[first:last+1]
        line_error = _error([segment_distance(p, subset[0], subset[-1]) for p in subset])
        orientation = _orientation(controls[0], controls[-1], config["axisToleranceDegrees"])
        spans.append({"id": f"stroke-{stroke_index:03}-span-{i:03}", "strokeIndex": stroke_index,
                      "startIndex": first, "endIndex": last, "controls": controls,
                      "kind": "quadratic" if len(controls) == 3 else "point" if first == last else "line",
                      "lineError": line_error, "fitError": curve_error or line_error,
                      "chordLength": distance(controls[0], controls[-1]),
                      "pathLength": sum(distance(a, b) for a, b in zip(subset, subset[1:])),
                      **orientation})
    return spans


def _ellipse_read(stroke, config):
    points = stroke["points"]
    if len(points) < 8:
        return None
    left, top, right, bottom = stroke["bounds"]
    rx, ry = (right-left)/2, (bottom-top)/2
    if min(rx, ry) < config["polylineTolerance"]:
        return None
    cx, cy = (right+left)/2, (bottom+top)/2
    gap = distance(points[0], points[-1])
    if gap > config["closedGapFraction"]*max(2*rx, 2*ry):
        return None
    radii = [math.hypot((p[0]-cx)/rx, (p[1]-cy)/ry) for p in points]
    radial = _error([abs(r-1) for r in radii])
    angles = [math.atan2((p[1]-cy)/ry, (p[0]-cx)/rx) for p in points]
    travel = sum(math.atan2(math.sin(b-a), math.cos(b-a)) for a, b in zip(angles, angles[1:]))
    if radial["rms"] > config["ellipseRadialRmsTolerance"] or abs(travel) < math.pi*1.6:
        return None
    return {"id": f"ellipse-{stroke['index']:03}", "strokeIndex": stroke["index"],
            "spans": stroke["spans"], "center": [cx, cy], "radii": [rx, ry],
            "endpointGap": gap, "radialError": radial, "signedTraversalRadians": travel,
            "qualification": "Near-closed oval candidate; measured gap remains open and source endpoints remain distinct."}


def _cross_reads(spans, drawing_bounds, config):
    _, top, _, bottom = drawing_bounds
    size = max(drawing_bounds[2]-drawing_bounds[0], bottom-top)
    straight = [s for s in spans if s["lineError"]["max"] <= config["crossMaximumLineError"]
                and s["chordLength"] >= size*config["crossMinSpanFraction"]]
    horizontal = [s for s in straight if abs(s["controls"][-1][0]-s["controls"][0][0])
                  >= abs(s["controls"][-1][1]-s["controls"][0][1])]
    vertical = [s for s in straight if s not in horizontal]
    result = []
    for h in horizontal:
        for v in vertical:
            if max(h["chordLength"], v["chordLength"]) > size*config["crossMaxSpanFraction"]:
                continue
            a, b = h["controls"][0], h["controls"][-1]
            c, d = v["controls"][0], v["controls"][-1]
            u, w = (b[0]-a[0], b[1]-a[1]), (d[0]-c[0], d[1]-c[1])
            determinant = u[0]*w[1]-u[1]*w[0]
            if abs(determinant) < 1e-9:
                continue
            crossing_angle = math.degrees(math.acos(max(-1.0, min(1.0,
                (u[0]*w[0]+u[1]*w[1])/(h["chordLength"]*v["chordLength"])))))
            if abs(crossing_angle-90) > config["crossRightAngleToleranceDegrees"]:
                continue
            q = (c[0]-a[0], c[1]-a[1])
            ht = (q[0]*w[1]-q[1]*w[0])/determinant
            vt = (q[0]*u[1]-q[1]*u[0])/determinant
            arm = config["crossMinimumArmFraction"]
            if not (arm <= ht <= 1-arm and arm <= vt <= 1-arm):
                continue
            point = [a[0]+ht*u[0], a[1]+ht*u[1]]
            relative_height = (point[1]-top)/(bottom-top) if bottom > top else 1
            if relative_height > config["crossUpperFraction"]:
                continue
            result.append({"id": f"cross-{len(result):03}", "spans": [h["id"], v["id"]],
                           "strokeIndices": [h["strokeIndex"], v["strokeIndex"]], "point": point,
                           "segmentFractions": [ht, vt], "relativeHeight": relative_height,
                           "rightAngleResidualDegrees": crossing_angle-90,
                           "armLineErrors": [h["lineError"], v["lineError"]],
                           "qualification": "Two observed arms support an upper crossing candidate; no shared site or church meaning is asserted."})
    return result


def observe_record(record, config=None):
    config = DEFAULTS | (config or {})
    source = record["drawing"]
    if any(len(s) < 2 or len(s[0]) != len(s[1]) for s in source):
        raise ValueError("Drawing needs equally sized x/y arrays in each acquired stroke")
    for stroke in source:
        if len(stroke) > 2 and len(stroke[2]) != len(stroke[0]):
            raise ValueError("Acquired time and coordinate sample counts differ")
        if any(not math.isfinite(float(v)) for axis in stroke for v in axis):
            raise ValueError("Acquired samples must be finite")
    source_points = [list(zip(s[0], s[1])) for s in source]
    all_source_points = [p for stroke in source_points for p in stroke]
    source_bounds = bounds(all_source_points) if all_source_points else None
    width, height = ((source_bounds[2]-source_bounds[0], source_bounds[3]-source_bounds[1])
                     if source_bounds is not None else (0, 0))
    available = config["targetExtent"]-2*config["margin"]
    if available <= 0 or config["polylineTolerance"] <= 0 or config["curveTolerance"] <= 0:
        raise ValueError("Target interior and fitting tolerances must be positive")
    scale = (available/max(width, height) if max(width, height) > 0 else 1.0) if all_source_points else None
    offset = ([(config["targetExtent"]-width*scale)/2-source_bounds[0]*scale,
               (config["targetExtent"]-height*scale)/2-source_bounds[1]*scale]
              if all_source_points else None)
    strokes, spans = [], []
    for i, points in enumerate(source_points):
        normalized = [[p[j]*scale+offset[j] for j in range(2)] for p in points]
        fitted = fit_stroke(normalized, i, config)
        stroke = {"id": f"stroke-{i:03}", "index": i, "sourcePoints": [list(p) for p in points],
                  "points": normalized, "times": list(source[i][2]) if len(source[i]) > 2 else None,
                  "spans": [s["id"] for s in fitted], "bounds": bounds(normalized) if normalized else None}
        strokes.append(stroke)
        spans.extend(fitted)
    all_points = [p for s in strokes for p in s["points"]]
    all_bounds = bounds(all_points) if all_points else None
    extent = max(all_bounds[2]-all_bounds[0], all_bounds[3]-all_bounds[1]) if all_bounds else 0
    outline = [s["id"] for s in strokes
               if s["bounds"] is not None and max(s["bounds"][2]-s["bounds"][0], s["bounds"][3]-s["bounds"][1]) >= extent*0.55]
    if not outline and all_points:
        outline = [max((s for s in strokes if s["points"]), key=lambda s: sum(distance(a,b) for a,b in zip(s["points"],s["points"][1:])))['id']]
    corners, disagreements, near_joins = [], [], []
    by_id = {s["id"]: s for s in spans}
    for stroke in strokes:
        local = [by_id[key] for key in stroke["spans"]]
        for left, right in zip(local, local[1:]):
            angle = _turn(left["controls"][-2], left["controls"][-1], right["controls"][1])
            if abs(angle) < config["cornerMinimumDegrees"]:
                continue
            corners.append({"id": f"corner-{len(corners):03}", "strokeIndex": stroke["index"],
                            "sampleIndex": left["endIndex"], "spans": [left["id"], right["id"]],
                            "point": left["controls"][-1], "signedTurnDegrees": angle,
                            "rightAngleResidualDegrees": abs(angle)-90,
                            "rightAngleCandidate": abs(abs(angle)-90) <= config["rightAngleToleranceDegrees"]})
        for a, b, c in zip(local, local[1:], local[2:]):
            first = _turn(a["controls"][-2], a["controls"][-1], b["controls"][1])
            second = _turn(b["controls"][-2], b["controls"][-1], c["controls"][1])
            if all(abs(abs(t)-90) <= config["rightAngleToleranceDegrees"] for t in (first, second)):
                direction_a = math.degrees(math.atan2(a["controls"][-1][1]-a["controls"][0][1], a["controls"][-1][0]-a["controls"][0][0]))
                direction_c = math.degrees(math.atan2(c["controls"][-1][1]-c["controls"][0][1], c["controls"][-1][0]-c["controls"][0][0]))
                parallel_residue = abs((direction_c-direction_a+90)%180-90)
                if parallel_residue > 1e-6:
                    disagreements.append({"id": f"axis-disagreement-{len(disagreements):03}",
                                          "spans": [a["id"], b["id"], c["id"]],
                                          "rightAngleResidualsDegrees": [abs(first)-90, abs(second)-90],
                                          "outerParallelResidualDegrees": parallel_residue,
                                          "qualification": "Both local bends support near-right-angle reads, but their outer directions are not exactly parallel. No global repair applied."})
    for i, first in enumerate(strokes):
        for second in strokes[i+1:]:
            if not first["points"] or not second["points"]:
                continue
            for ai in (0, len(first["points"])-1):
                for bi in (0, len(second["points"])-1):
                    gap = distance(first["points"][ai], second["points"][bi])
                    if gap <= config["nearJoinTolerance"]:
                        near_joins.append({"id": f"near-join-{len(near_joins):03}",
                                           "strokeIndices": [first["index"], second["index"]],
                                           "sampleIndices": [ai, bi], "distance": gap,
                                           "points": [first["points"][ai], second["points"][bi]],
                                           "qualification": "Endpoint proximity across a pen lift; identities remain distinct, even at distance zero."})
    ellipses = [candidate for s in strokes if (candidate := _ellipse_read(s, config)) is not None]
    crosses = _cross_reads(spans, all_bounds, config) if all_bounds else []
    payload = json.dumps(source, separators=(",", ":"), ensure_ascii=False).encode()
    return {"schema": "gojo.quickdraw-geometric-observations.v1", "sourceId": str(record.get("key_id", "anonymous")),
            "sourceDrawingSha256": hashlib.sha256(payload).hexdigest(),
            "qualification": "Exploratory measured adapter reads. No trained classifier, inferred identity, symmetry, repaired closure or exact primitive intent.",
            "normalization": {"sourceBounds": source_bounds, "scale": scale, "offset": offset,
                              "targetBox": [0, 0, config["targetExtent"], config["targetExtent"]]},
            "config": config, "bounds": all_bounds, "strokes": strokes, "spans": spans,
            "outlineStrokeIds": outline, "outlineQualification": "Acquired strokes spanning at least 55% of the drawing extent; a heuristic coarse selection, not a proven outside boundary.",
            "emptyRead": not bool(all_points),
            "corners": corners, "circles": ellipses, "crosses": crosses, "nearJoins": near_joins,
            "unresolved": disagreements,
            "counts": {"strokes": len(strokes), "samples": sum(len(s["points"]) for s in strokes),
                       "spans": len(spans), "lines": sum(s["kind"] == "line" for s in spans),
                       "quadratics": sum(s["kind"] == "quadratic" for s in spans),
                       "corners": len(corners), "crosses": len(crosses), "ovals": len(ellipses),
                       "nearJoins": len(near_joins), "axisDisagreements": len(disagreements)}}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    records = [json.loads(line) for line in args.input.read_text(encoding="utf-8").splitlines() if line.strip()]
    result = [observe_record(row) for row in records]
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("".join(json.dumps(row, separators=(",", ":"))+"\n" for row in result), encoding="utf-8")
    print(json.dumps({"output": str(args.output), "drawings": [{"id": r["sourceId"], **r["counts"]} for r in result]}))


if __name__ == "__main__":
    main()
