"""Observed Quick Draw -> budgeted hints -> learned local cubic proposal.

This numerical adapter is not canonical Gojo meaning. Native reconstruction
imports only the detached packets and model; private samples, fitted targets,
source correspondence and category strata remain in separate evaluator files.
No ideal shape, category-specific model, closure repair or axis snapping is used.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
import math
from pathlib import Path
import sys

from fit_pilot import bezier, distance, normalize, parameters, polynomial_fit
from select_samples import CATEGORIES, validate_record

COORDINATE_QUANTUM = 1024
PROFILE_STEP = 16
COEFFICIENT_QUANTUM = 1_000_000
BUDGETS = (3, 6, 12, 20)
SEED = "gojo-faithful-20261023-v1"


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")


def numpy():
    """Optional local dependency; no install or environment mutation on import."""
    try:
        import numpy as np
    except ImportError:
        local = Path("D:/_quickDraw/gojo-python")
        if not local.exists():
            raise RuntimeError("Install numpy, or use --python-packages to select a local installation")
        sys.path.insert(0, str(local))
        import numpy as np
    return np


def truncate_ratio(numerator, denominator):
    return (1 if numerator >= 0 else -1) * (abs(numerator) // denominator)


def sample_at(points, ts, t):
    for i in range(1, len(points)):
        if ts[i] >= t:
            width = ts[i] - ts[i - 1]
            f = (t - ts[i - 1]) / width if width else 0.0
            return tuple(a + f * (b - a) for a, b in zip(points[i - 1], points[i]))
    return points[-1]


def cubic_fit(points):
    """Endpoint-fixed fit to the observed trace, including dot/line fallbacks."""
    fit = polynomial_fit(points, 3) if len(points) >= 4 else None
    if fit is not None:
        return fit
    fit = polynomial_fit(points, 2) if len(points) >= 3 else None
    a, b = points[0], points[-1]
    if fit is not None:
        q = fit["controls"][1]
        controls = [a, tuple((x + 2*y)/3 for x, y in zip(a, q)),
                    tuple((x + 2*y)/3 for x, y in zip(b, q)), b]
    else:
        controls = [a, tuple((2*x+y)/3 for x, y in zip(a, b)),
                    tuple((x+2*y)/3 for x, y in zip(a, b)), b]
    ts = parameters(points)
    errors = [distance(p, bezier(controls, t)) for p, t in zip(points, ts)]
    return {"controls": controls, "sample_parameter": ts, "sample_errors": errors,
            "max_error": max(errors), "rms_error": math.sqrt(sum(x*x for x in errors)/len(errors))}


def hint_for(points):
    q = COORDINATE_QUANTUM
    start, end = [[round(v*q) for v in p] for p in (points[0], points[-1])]
    ts = parameters(points)
    profile = [sample_at(points, ts, t) for t in (.25, .5, .75)]
    features = [q, end[0]-start[0], end[1]-start[1]]
    for p in profile:
        features.extend(round((p[d]*q-start[d])/PROFILE_STEP)*PROFILE_STEP for d in (0, 1))
    return {"start": start, "end": end, "features": features}


def extract(drawing, budgets=BUDGETS):
    """Nested, order-preserving source partitions; empty strokes stay empty.

    Segmentation may inspect the entire acquired trace. The detached receiver
    receives no future reference: each stroke is reconstructed in original order.
    These are offline trace fits, not a causal prediction of a human's next move.
    """
    if not budgets or any(not isinstance(b, int) or b < 1 for b in budgets):
        raise ValueError("Budgets must be positive integers")
    strokes, basis = normalize(drawing)
    nodes, active = {}, []

    def create(si, a, b, parent=None):
        points = strokes[si][a:b+1]
        fit = cubic_fit(points)
        nid = len(nodes)
        node = {"node": nid, "parent": parent, "stroke": si, "sourceStart": a,
                "sourceEnd": b, "points": points, "fit": fit, "hint": hint_for(points)}
        if len(drawing[si]) == 3:
            node["sourceTime"] = [drawing[si][2][a], drawing[si][2][b]]
        nodes[nid] = node
        return nid

    for si, points in enumerate(strokes):
        if points:
            active.append(create(si, 0, len(points)-1))
    stroke_floor = len(active)
    snapshots = []
    for budget in sorted(set(budgets)):
        target = max(budget, stroke_floor)
        while len(active) < target:
            splittable = [n for n in active if nodes[n]["sourceEnd"]-nodes[n]["sourceStart"] > 1]
            if not splittable:
                break
            # Largest sampled deviation first; length breaks zero-error ties.
            chosen = max(splittable, key=lambda n: (nodes[n]["fit"]["max_error"],
                         sum(distance(a, b) for a, b in zip(nodes[n]["points"], nodes[n]["points"][1:])), -n))
            node = nodes[chosen]
            errors = node["fit"]["sample_errors"]
            candidates = range(1, len(errors)-1)
            k = max(candidates, key=lambda i: (errors[i], -abs(i-(len(errors)-1)/2)))
            split = node["sourceStart"] + k
            active.remove(chosen)
            active.extend((create(node["stroke"], node["sourceStart"], split, chosen),
                           create(node["stroke"], split, node["sourceEnd"], chosen)))
        selected = [nodes[n] for n in sorted(active, key=lambda n: (nodes[n]["stroke"], nodes[n]["sourceStart"]))]
        packet = {"coordinateQuantum": COORDINATE_QUANTUM,
                  "strokes": [{"sections": []} for _ in strokes]}
        for node in selected:
            packet["strokes"][node["stroke"]]["sections"].append(node["hint"])
        snapshots.append({"requestedBudget": budget, "effectiveBudget": len(selected),
                          "strokeFloor": stroke_floor, "sampleCapacityReached": len(selected) < target,
                          "nodes": selected, "packet": packet})
    return {"basis": basis, "strokes": strokes, "snapshots": snapshots, "nodes": nodes}


def geometry_signature(drawing):
    """Declared coarse near-duplicate grouping, not a perceptual identity claim.

    Eight equal-arclength samples per stroke at 1/32 extent; original stroke
    order/direction retained. Exact IDs are additionally never allowed to split.
    This catches some near duplicates; different orders and traces can evade it.
    """
    strokes, _ = normalize(drawing)
    value = []
    for points in strokes:
        ts = parameters(points) if points else []
        value.append([[round(v*32) for v in sample_at(points, ts, i/7)] for i in range(8)] if points else [])
    return hashlib.sha256(json.dumps(value, separators=(",", ":")).encode()).hexdigest()


def sample_corpus(source, output, per_category, skip, development):
    output.mkdir(parents=True, exist_ok=True)
    reserved_ids = {str(r["key_id"]) for r in development}
    reserved_geometry = {geometry_signature(r["drawing"]) for r in development}
    admitted, seen_ids, manifest = [], set(), []
    rejected = Counter()
    for category in CATEGORIES:
        path = source / f"{category}.ndjson"
        count = 0
        with path.open("rb") as stream:
            for line_number in range(skip):
                if not stream.readline():
                    raise ValueError(f"{path} ended before requested prefix skip")
            line_number = skip
            while count < per_category:
                offset = stream.tell()
                raw = stream.readline()
                if not raw:
                    break
                line_number += 1
                try:
                    record = json.loads(raw)
                    errors = validate_record(record)
                    if errors:
                        rejected["encoding"] += 1
                        continue
                    key = str(record["key_id"])
                    if key in reserved_ids or key in seen_ids:
                        rejected["reserved_or_duplicate_id"] += 1
                        continue
                    signature = geometry_signature(record["drawing"])
                    if signature in reserved_geometry:
                        rejected["development_geometry_group"] += 1
                        continue
                    # Resource bound is explicit, not a shape-quality filter.
                    points = sum(len(s[0]) for s in record["drawing"])
                    if points > 2500 or len(record["drawing"]) > 80:
                        rejected["resource_capacity"] += 1
                        continue
                except (ValueError, TypeError, KeyError):
                    rejected["malformed"] += 1
                    continue
                group = int(hashlib.sha256(f"{SEED}:{signature}".encode()).hexdigest()[:8], 16) % 10
                split = "train" if group < 7 else "validation" if group < 8 else "test"
                row = {"key_id": key, "category": category, "split": split, "geometryGroup": signature,
                       "sourcePath": str(path.resolve()), "sourceLine": line_number, "byteOffset": offset,
                       "byteLength": len(raw), "sha256": hashlib.sha256(raw).hexdigest()}
                admitted.append(record)
                manifest.append(row)
                seen_ids.add(key)
                count += 1
        if count != per_category:
            raise ValueError(f"Only {count} valid records found for {category}")
    with (output / "corpus.ndjson").open("w", encoding="utf-8", newline="\n") as stream:
        for record in admitted:
            stream.write(json.dumps(record, separators=(",", ":")) + "\n")
    report = {"schema": "gojo.quickdraw.faithful-corpus.v1", "seed": SEED,
              "sourcePrefixSkippedPerCategory": skip, "perCategory": per_category,
              "sampling": "next encoding-valid bounded prefix; no recognized/geometry quality filter",
              "categoryRole": "sampling/review stratum only; absent from features and model",
              "grouping": "8 arclength samples/stroke on 1/32 extent, original order; near duplicates may escape",
              "reservedDevelopmentCount": len(development), "rejected": dict(rejected),
              "splitCounts": dict(Counter(r["split"] for r in manifest)),
              "geometryGroupCounts": {split: len({r["geometryGroup"] for r in manifest if r["split"] == split})
                                      for split in ("train", "validation", "test")}, "records": manifest}
    write_json(output / "corpus-manifest.json", report)
    return admitted, manifest, report


def expand_scalar_weights(matrix):
    weights = [[0]*9 for _ in range(4)]
    for control in (0, 1):
        for axis in (0, 1):
            for j in range(4):
                weights[control*2+axis][1+2*j+axis] = round(float(matrix[control][j])*COEFFICIENT_QUANTUM)
    return weights


def fixed_profile_weights():
    np = numpy()
    ts = np.array([.25, .5, .75])
    design = np.stack((3*ts*(1-ts)**2, 3*ts*ts*(1-ts)), axis=1)
    target = np.concatenate((-(ts**3)[:, None], np.eye(3)), axis=1)
    return expand_scalar_weights(np.linalg.lstsq(design, target, rcond=None)[0])


def decode(hint, weights):
    start, end = hint["start"], hint["end"]
    offsets = [truncate_ratio(sum(w*f for w, f in zip(row, hint["features"])), COEFFICIENT_QUANTUM)
               for row in weights]
    return [start, [start[0]+offsets[0], start[1]+offsets[1]],
            [start[0]+offsets[2], start[1]+offsets[3]], end]


def normalized_controls(hint, weights):
    return [[v/COORDINATE_QUANTUM for v in p] for p in decode(hint, weights)]


def train(examples):
    np = numpy()
    xx, xy = np.zeros((4, 4)), np.zeros((4, 2))
    count = 0
    for data, meta in examples:
        if meta["split"] != "train":
            continue
        for snapshot in data["snapshots"]:
            weight = 1 / max(1, len(snapshot["nodes"]) * len(data["snapshots"]))
            for node in snapshot["nodes"]:
                hint = node["hint"]
                for axis in (0, 1):
                    x = np.array([hint["features"][1+axis+2*j]/COORDINATE_QUANTUM for j in range(4)])
                    y = np.array([node["fit"]["controls"][j][axis]-hint["start"][axis]/COORDINATE_QUANTUM for j in (1, 2)])
                    xx += weight*np.outer(x, x)
                    xy += weight*np.outer(x, y)
                count += 1
    if not count:
        raise ValueError("Training split is empty")
    candidates = []
    for ridge in (0.0, .00001, .001, .01, .1, 1.0):
        fitted = np.linalg.solve(xx+ridge*np.eye(4), xy).T
        weights = expand_scalar_weights(fitted)
        errors = []
        for data, meta in examples:
            if meta["split"] == "validation":
                errors.extend(measure_snapshot(s, weights)["rms"] for s in data["snapshots"])
        if not errors:
            raise ValueError("Validation split is empty")
        candidates.append({"ridge": ridge, "validationMeanRms": sum(errors)/len(errors), "weights": weights})
    best = min(candidates, key=lambda row: row["validationMeanRms"])
    model = {"schema": "gojo.quickdraw.local-profile-prior.v1", "coordinateQuantum": COORDINATE_QUANTUM,
             "coefficientQuantum": COEFFICIENT_QUANTUM, "weights": best["weights"], "trainingCount": count,
             "trainingDrawingCount": sum(meta["split"] == "train" for _, meta in examples),
             "ridge": best["ridge"], "profileStep": PROFILE_STEP,
             "features": ["one", "endDx", "endDy", "quarterDx", "quarterDy", "halfDx", "halfDy", "threeQuarterDx", "threeQuarterDy"],
             "outputs": ["control1Dx", "control1Dy", "control2Dx", "control2Dy"],
             "target": "endpoint-fixed cubic least-squares fit to observed source; no ideal template",
             "weighting": "each training drawing and budget equal total weight; shared x/y scalar coefficients",
             "equivariance": "shared scalar coefficients/no intercept/no cross-axis terms is algebraically equivalent to all axis-swap/reflection augmentations",
             "prediction": "start + integer truncation toward zero of dot(weights, features)/coefficientQuantum",
             "qualification": "exploratory local cubic proposal; no likelihood, intention, skill, or handedness inference"}
    return model, [{k: v for k, v in c.items() if k != "weights"} for c in candidates]


def measure_snapshot(snapshot, weights=None, mode="learned"):
    sums, count, worst = 0.0, 0, 0.0
    for node in snapshot["nodes"]:
        if mode == "fit":
            controls = node["fit"]["controls"]
        elif mode == "line":
            a, b = [[v/COORDINATE_QUANTUM for v in p] for p in (node["hint"]["start"], node["hint"]["end"])]
            controls = [a, b]
        else:
            controls = normalized_controls(node["hint"], weights)
        # One contribution per actual acquisition event, independent of budget.
        # Adjacent spans share a boundary event, so the later span omits it.
        # Equal coordinates at other source indices remain separate samples.
        pairs = list(zip(node["points"], node["fit"]["sample_parameter"]))
        if node["sourceStart"] > 0:
            pairs = pairs[1:]
        for point, t in pairs:
            error = distance(point, bezier(controls, t))
            sums += error*error
            worst = max(worst, error)
            count += 1
    return {"rms": math.sqrt(sums/count) if count else 0, "max": worst, "samples": count}


def evaluate(examples, model, fixed_weights):
    rows = []
    for data, meta in examples:
        for snapshot in data["snapshots"]:
            row = {"key_id": meta["key_id"], "category": meta["category"], "split": meta["split"],
                   "budget": snapshot["requestedBudget"], "sections": snapshot["effectiveBudget"],
                   "strokeFloor": snapshot["strokeFloor"], "sampleCapacityReached": snapshot["sampleCapacityReached"],
                   "learned": measure_snapshot(snapshot, model["weights"]),
                   "fixed": measure_snapshot(snapshot, fixed_weights), "line": measure_snapshot(snapshot, mode="line"),
                   "privateFit": measure_snapshot(snapshot, mode="fit")}
            packet = snapshot["packet"]
            row["hintJsonBytes"] = len(json.dumps(packet, separators=(",", ":")).encode())
            # Nine features include repeated one/end differences; five distinct
            # 2D positions are transmitted per section before shared endpoints.
            row["uniqueScalarFieldsPerSection"] = 10
            rows.append(row)
    return rows


def aggregate(rows):
    result = []
    for split in sorted({r["split"] for r in rows}):
        for budget in BUDGETS:
            selected = [r for r in rows if r["split"] == split and r["budget"] == budget]
            if not selected:
                continue
            entry = {"split": split, "budget": budget, "drawings": len(selected),
                     "meanSections": sum(r["sections"] for r in selected)/len(selected),
                     "strokeFloorOverBudget": sum(r["strokeFloor"] > budget for r in selected),
                     "sampleCapacityReached": sum(r["sampleCapacityReached"] for r in selected),
                     "meanHintJsonBytes": sum(r["hintJsonBytes"] for r in selected)/len(selected),
                     "learnedImprovesFixed": sum(r["learned"]["rms"] < r["fixed"]["rms"] for r in selected)}
            for method in ("learned", "fixed", "line", "privateFit"):
                values = sorted(r[method]["rms"] for r in selected)
                entry[method] = {"meanRms": sum(values)/len(values), "medianRms": values[len(values)//2],
                                 "p90Rms": values[min(len(values)-1, math.ceil(.9*len(values))-1)],
                                 "maximumSampleError": max(r[method]["max"] for r in selected)}
            result.append(entry)
    return result


def emit_packets(examples, output):
    """Outer IDs associate native output with the private evaluator only."""
    audits = []
    with (output / "packets.ndjson").open("w", encoding="utf-8", newline="\n") as stream:
        for data, meta in examples:
            for snapshot in data["snapshots"]:
                packet_id = f'{meta["key_id"]}-b{snapshot["requestedBudget"]}'
                packet = {"id": packet_id, **snapshot["packet"]}
                stream.write(json.dumps(packet, separators=(",", ":")) + "\n")
                nodes = []
                for node in snapshot["nodes"]:
                    nodes.append({k: v for k, v in node.items() if k not in ("hint", "points")})
                audits.append({"id": packet_id, "key_id": meta["key_id"], "category": meta["category"],
                               "split": meta["split"], "basis": data["basis"], "sourceStrokes": data["strokes"],
                               "requestedBudget": snapshot["requestedBudget"], "effectiveBudget": snapshot["effectiveBudget"],
                               "strokeFloor": snapshot["strokeFloor"], "sampleCapacityReached": snapshot["sampleCapacityReached"],
                               "nodes": nodes,
                               "partitionHistory": [{k: n[k] for k in ("node", "parent", "stroke", "sourceStart", "sourceEnd")}
                                                    for n in data["nodes"].values()]})
    with (output / "private-audit.ndjson").open("w", encoding="utf-8", newline="\n") as stream:
        for row in audits:
            stream.write(json.dumps(row, separators=(",", ":")) + "\n")


def encode_input(input_path, model_path, output):
    """Encode new sources with frozen weights; never calls the training code."""
    model = json.loads(model_path.read_text(encoding="utf-8"))
    if model.get("coordinateQuantum") != COORDINATE_QUANTUM or model.get("coefficientQuantum") != COEFFICIENT_QUANTUM:
        raise ValueError("Frozen model uses an incompatible coordinate/coefficient quantum")
    if model.get("profileStep") != PROFILE_STEP:
        raise ValueError("Frozen model uses an incompatible profile resolution")
    weights = model.get("weights")
    if not isinstance(weights, list) or len(weights) != 4 or any(
            not isinstance(row, list) or len(row) != 9 or any(isinstance(v, bool) or not isinstance(v, int) for v in row)
            for row in weights):
        raise ValueError("Frozen model must supply four rows of nine integer coefficients")
    output.mkdir(parents=True, exist_ok=True)
    examples, sources, seen = [], [], set()
    with input_path.open("rb") as stream:
        line_number = 0
        while True:
            offset = stream.tell()
            raw = stream.readline()
            if not raw:
                break
            line_number += 1
            if not raw.strip():
                continue
            record = json.loads(raw)
            errors = validate_record(record)
            if errors:
                raise ValueError(f"Invalid input at line {line_number}: {errors}")
            key = str(record["key_id"])
            if key in seen:
                raise ValueError(f"Duplicate source key {key}")
            seen.add(key)
            examples.append((extract(record["drawing"]), {"key_id": key,
                             "category": record.get("word", "unlabelled"), "split": "frozen-input"}))
            sources.append({"key_id": key, "sourcePath": str(input_path.resolve()), "sourceLine": line_number,
                            "byteOffset": offset, "byteLength": len(raw), "sha256": hashlib.sha256(raw).hexdigest()})
    if not examples:
        raise ValueError("No drawings in frozen input")
    fixed = fixed_profile_weights()
    rows = evaluate(examples, model, fixed)
    write_json(output / "model.json", model)
    write_json(output / "fixed-model.json", {"coordinateQuantum": COORDINATE_QUANTUM,
               "coefficientQuantum": COEFFICIENT_QUANTUM, "weights": fixed, "trainingCount": 0})
    write_json(output / "metrics.json", rows)
    write_json(output / "summary.json", {"schema": "gojo.quickdraw.frozen-input.v1", "mode": "frozen weights, no training",
               "modelSource": str(model_path.resolve()), "modelSha256": hashlib.sha256(model_path.read_bytes()).hexdigest(),
               "summary": aggregate(rows), "sources": sources})
    emit_packets(examples, output)
    print(json.dumps({"stage": "encoded", "drawings": len(examples), "packets": len(examples)*len(BUDGETS),
                      "output": str(output), "model": str(model_path)}), flush=True)


def run(args):
    if args.python_packages:
        sys.path.insert(0, str(args.python_packages))
    if args.encode_input:
        if not args.model:
            raise ValueError("--encode-input requires --model; it cannot silently train")
        return encode_input(args.encode_input, args.model, args.output)
    if args.model:
        raise ValueError("--model requires --encode-input")
    args.output.mkdir(parents=True, exist_ok=True)
    development = [json.loads(line) for line in args.development.read_text(encoding="utf-8").splitlines() if line.strip()]
    reserved = development[:]
    if args.challenges and args.challenges.exists():
        reserved.extend(json.loads(line) for line in args.challenges.read_text(encoding="utf-8").splitlines() if line.strip())
    corpus, manifest, corpus_report = sample_corpus(args.source, args.output, args.per_category, args.skip, reserved)
    print(json.dumps({"stage": "sampled", "records": len(corpus), "splits": corpus_report["splitCounts"]}), flush=True)
    examples = []
    for i, (record, meta) in enumerate(zip(corpus, manifest)):
        examples.append((extract(record["drawing"]), meta))
        if (i+1) % 250 == 0:
            print(json.dumps({"stage": "extracted", "records": i+1}), flush=True)
    development_examples = [(extract(r["drawing"]), {"key_id": str(r["key_id"]), "category": r["word"], "split": "development"})
                            for r in development]
    model, candidates = train(examples)
    write_json(args.output / "model.json", model)
    fixed = fixed_profile_weights()
    write_json(args.output / "fixed-model.json", {"coordinateQuantum": COORDINATE_QUANTUM,
               "coefficientQuantum": COEFFICIENT_QUANTUM, "weights": fixed, "trainingCount": 0})
    rows = evaluate(examples + development_examples, model, fixed)
    write_json(args.output / "metrics.json", rows)
    report = {"schema": "gojo.quickdraw.faithful-learning-summary.v1", "corpus": {k: v for k, v in corpus_report.items() if k != "records"},
              "model": model, "validationCandidates": candidates, "summary": aggregate(rows),
              "metric": "drawing-mean RMS distance to source at per-section source arclength parameters, normalized by max source extent",
              "scope": "bounded five-stratum prefix; uncurated training includes decoration, no intended-category truth",
              "limitations": ["offline segmentation uses full observed trace", "coarse metric profile hints, not semantic concepts",
                              "sampled error, not global Hausdorff/topology bound", "near-duplicate grouping is incomplete",
                              "no learned uncertainty calibration, drawing skill, handedness, idealization or gap-completion model",
                              "section count is not evidence fact count or compressed byte cost"]}
    write_json(args.output / "summary.json", report)
    emit_packets(development_examples, args.output)
    heldout_output = args.output / "heldout-review"
    heldout_output.mkdir(exist_ok=True)
    # First two hash-ranked untouched sources per sampling stratum. Selection
    # uses IDs only, never reconstruction error or recognition flags.
    review_examples = []
    for category in CATEGORIES:
        choices = [e for e in examples if e[1]["split"] == "test" and e[1]["category"] == category]
        choices.sort(key=lambda e: hashlib.sha256(f'{SEED}:review:{e[1]["key_id"]}'.encode()).hexdigest())
        review_examples.extend(choices[:2])
    emit_packets(review_examples, heldout_output)
    print(json.dumps({"stage": "complete", "output": str(args.output), "trainingSections": model["trainingCount"],
                      "ridge": model["ridge"], "developmentPackets": len(development_examples)*len(BUDGETS)}), flush=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=Path("D:/_quickDraw/quickdraw"))
    parser.add_argument("--development", type=Path, default=Path("D:/_quickDraw/gojo-pilot/selected.ndjson"))
    parser.add_argument("--challenges", type=Path, default=Path("D:/_quickDraw/gojo-pilot/challenges.ndjson"))
    parser.add_argument("--output", type=Path, default=Path("D:/_quickDraw/gojo-faithful"))
    parser.add_argument("--per-category", type=int, default=400)
    parser.add_argument("--skip", type=int, default=2000)
    parser.add_argument("--python-packages", type=Path)
    parser.add_argument("--encode-input", type=Path, help="Encode new NDJSON sources with frozen model instead of training")
    parser.add_argument("--model", type=Path, help="Existing learned model for --encode-input")
    run(parser.parse_args())
