"""Independent preservation and causal-construction checks for observed drawings.

The audit deliberately does not invoke the observation detector or the recipe
builder. Success establishes the stated transport/causality contracts; it does
not certify church recognition, intended topology, or an ideal reconstruction.
"""
from __future__ import annotations

from collections import Counter
from fractions import Fraction
import argparse
import hashlib
import json
import math
from pathlib import Path


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def audit_observations(record, observation):
    """Account for original acquisition events, including repeated coordinates."""
    drawing = record["drawing"]
    strokes = observation["strokes"]
    spans = observation["spans"]
    require(len(strokes) == len(drawing), "Acquired stroke count changed")
    require(len({span["id"] for span in spans}) == len(spans), "Duplicate source span identity")
    require([span["strokeIndex"] for span in spans] == sorted(span["strokeIndex"] for span in spans),
            "Span sequence changed acquired stroke order")
    require(all(0 <= span["strokeIndex"] < len(drawing) for span in spans), "Unknown acquired stroke")
    acquired = 0
    backwards_times = 0
    duplicate_samples = 0
    for index, (source, stroke) in enumerate(zip(drawing, strokes)):
        require(stroke["index"] == index, "Acquired stroke ordinal changed")
        points = [list(point) for point in zip(source[0], source[1], strict=True)]
        times = source[2] if len(source) > 2 else None
        require(stroke["sourcePoints"] == points, "Original sample events or their order changed")
        require(stroke["times"] == times, "Acquired times changed or were sorted")
        selected = [span for span in spans if span["strokeIndex"] == index]
        require(stroke["spans"] == [span["id"] for span in selected], "Stroke span membership/order changed")
        require(bool(points) == bool(selected), "Nonempty strokes must be accounted for; empty strokes stay empty")
        acquired += len(points)
        duplicate_samples += sum(a == b for a, b in zip(points, points[1:]))
        if times:
            backwards_times += sum(a > b for a, b in zip(times, times[1:]))
        if not points:
            continue
        require(selected[0]["startIndex"] == 0 and selected[-1]["endIndex"] == len(points) - 1,
                "Source span coverage omits stroke endpoints")
        ownership = Counter()
        prior_end = None
        for span in selected:
            a, b = span["startIndex"], span["endIndex"]
            require(type(a) is int and type(b) is int and 0 <= a <= b < len(points), "Invalid source span")
            require(a < b or len(points) == 1, "A non-singleton stroke cannot supply zero-progress spans")
            require(prior_end is None or a == prior_end, "Source spans have a gap, overlap, or reversed order")
            for sample in range(a, b + 1):
                # A shared partition boundary is one acquired event, not two.
                if sample == a and prior_end is not None:
                    continue
                ownership[sample] += 1
            prior_end = b
        require(ownership == Counter(range(len(points))), "Acquired sample accounting is incomplete or duplicated")
    return {"sourceStrokeCount": len(drawing), "sourcePointCount": acquired,
            "sourceSpanCount": len(spans), "consecutiveDuplicateSamples": duplicate_samples,
            "backwardsTimeSteps": backwards_times,
            "scope": "Preserved acquired stroke/sample/time order and complete source-span partition; no intent claim."}


def close(actual, expected, message):
    require(math.isclose(actual, expected, rel_tol=1e-9, abs_tol=1e-7), message)


def audit_geometric_candidates(record, observation):
    """Check source correspondence and selected candidates without rediscovery."""
    payload = json.dumps(record["drawing"], separators=(",", ":"), ensure_ascii=False).encode()
    require(hashlib.sha256(payload).hexdigest() == observation["sourceDrawingSha256"], "Source drawing hash changed")
    normalization = observation["normalization"]
    scale, offset = normalization["scale"], normalization["offset"]
    require(scale > 0 and len(offset) == 2, "Normalization must preserve frame orientation")
    spans = {span["id"]: span for span in observation["spans"]}
    for stroke in observation["strokes"]:
        require(len(stroke["points"]) == len(stroke["sourcePoints"]), "Normalized samples changed acquisition count")
        for point, original in zip(stroke["points"], stroke["sourcePoints"]):
            for axis in range(2):
                close(point[axis], original[axis] * scale + offset[axis], "Normalization altered orientation/aspect/order")
        for key in stroke["spans"]:
            span = spans[key]
            require(span["controls"][0] == stroke["points"][span["startIndex"]], "Fitted start lost source correspondence")
            require(span["controls"][-1] == stroke["points"][span["endIndex"]], "Fitted end lost source correspondence")
    config = observation["config"]
    for candidate in observation["crosses"]:
        require(len(candidate["spans"]) == 2 and len(set(candidate["spans"])) == 2,
                "A cross candidate needs two distinct observed participating spans")
        selected = [spans[key] for key in candidate["spans"]]
        a, b = selected[0]["controls"][0], selected[0]["controls"][-1]
        c, d = selected[1]["controls"][0], selected[1]["controls"][-1]
        u, v = [b[i] - a[i] for i in range(2)], [d[i] - c[i] for i in range(2)]
        determinant = u[0] * v[1] - u[1] * v[0]
        require(determinant != 0, "Parallel observed spans cannot establish a crossing")
        q = [c[i] - a[i] for i in range(2)]
        fractions = [(q[0] * v[1] - q[1] * v[0]) / determinant,
                     (q[0] * u[1] - q[1] * u[0]) / determinant]
        arm = config["crossMinimumArmFraction"]
        require(all(arm <= value <= 1 - arm for value in fractions), "Crossing lacks observed arms on both sides")
        for observed, expected in zip(candidate["segmentFractions"], fractions):
            close(observed, expected, "Cross intersection parameter changed")
        for axis in range(2):
            close(candidate["point"][axis], a[axis] + fractions[0] * u[axis], "Cross intersection is unsupported")
        cosine = sum(x * y for x, y in zip(u, v)) / math.hypot(*u) / math.hypot(*v)
        residual = math.degrees(math.acos(max(-1, min(1, cosine)))) - 90
        close(candidate["rightAngleResidualDegrees"], residual, "Cross right-angle residual changed")
        require(abs(residual) <= config["crossRightAngleToleranceDegrees"], "Cross angle exceeds selected tolerance")
    for candidate in observation["nearJoins"]:
        require(candidate["strokeIndices"][0] != candidate["strokeIndices"][1], "Pen-lift proximity requires distinct strokes")
        points = [observation["strokes"][stroke]["points"][sample]
                  for stroke, sample in zip(candidate["strokeIndices"], candidate["sampleIndices"])]
        close(candidate["distance"], math.dist(*points), "Pen-lift gap was repaired instead of observed")
    for candidate in observation["circles"]:
        stroke = observation["strokes"][candidate["strokeIndex"]]
        close(candidate["endpointGap"], math.dist(stroke["points"][0], stroke["points"][-1]), "Oval endpoint gap was hidden")
    for candidate in observation["unresolved"]:
        selected = [spans[key] for key in candidate["spans"]]
        require(len(selected) == 3, "Two-corner disagreement requires its shared connector")
        directions = [math.degrees(math.atan2(span["controls"][-1][1] - span["controls"][0][1],
                                             span["controls"][-1][0] - span["controls"][0][0]))
                      for span in (selected[0], selected[2])]
        residual = abs((directions[1] - directions[0] + 90) % 180 - 90)
        close(candidate["outerParallelResidualDegrees"], residual, "Corner disagreement residual changed")
        require(residual > 0, "Exact parallel agreement cannot be published as a contradiction")
    return {"crossCandidates": len(observation["crosses"]), "ovalCandidates": len(observation["circles"]),
            "nearJoinCandidates": len(observation["nearJoins"]), "axisDisagreements": len(observation["unresolved"])}


def evaluate_program(program):
    """Replay only published affine instructions, independently of the builder."""
    parameters = {row["id"]: row["value"] for row in program["parameters"]}
    values, dependencies, introduced = {}, {}, {}
    for site in program["sites"]:
        key, at = site["id"], site["at"]
        require(key not in values, "Repeated construction site identity")
        point, refs = [], set()
        for axis, name in enumerate(("x", "y")):
            expression = site[name]
            if type(expression) is int:
                point.append(expression)
                continue
            first, second = expression["from"], expression["to"]
            origin = expression.get("origin", first)
            require(all(ref in values for ref in (origin, first, second)), "Missing or forward construction reference")
            require(all(introduced[ref] <= at for ref in (origin, first, second)), "Future-rung construction reference")
            require(expression["denominator"] > 0, "Invalid affine grade")
            numerator = expression["numerator"]
            numerator = parameters[numerator] if isinstance(numerator, str) else numerator
            displacement = Fraction((values[second][axis] - values[first][axis]) * numerator,
                                    expression["denominator"])
            point.append(values[origin][axis] + int(displacement))
            refs.update((origin, first, second))
        values[key], dependencies[key], introduced[key] = point, refs, at
    return values, dependencies


def audit_native_program(program, run):
    values, dependencies = evaluate_program(program)
    require(run["nativeRestoreVerified"] and run["nativeReplayVerified"], "Native archive replay was not verified")
    require(len(program["rungs"]) == len(run["rungs"]), "Native rung count changed")
    edges = {edge["id"]: edge for edge in program["edges"]}
    require(len(edges) == len(program["edges"]), "Repeated edge identity")
    previous = set()
    checks = 0
    for index, (rung, actual) in enumerate(zip(program["rungs"], run["rungs"])):
        expected_sites = {site["id"] for site in program["sites"] if site["at"] <= index}
        require({site["id"] for site in actual["sites"]} == expected_sites, "Native site availability changed")
        for site in actual["sites"]:
            require([site["x"], site["y"]] == values[site["id"]], "Native numeric replay disagrees with affine instructions")
            require(set(site["dependencies"]) == dependencies[site["id"]], "Native dependency correspondence changed")
            checks += 2
        visible, guides = set(rung["visible"]), set(rung["guides"])
        require(not visible & guides, "Visible and construction-guide scopes overlap")
        require(set(rung["selected"]) == previous - visible, "Retired-edge accounting changed")
        require({edge["id"] for edge in actual["edges"] if not edge["guide"]} == visible,
                "Native visible scope changed")
        require({edge["id"] for edge in actual["edges"] if edge["guide"]} == guides,
                "Native guide scope changed")
        for edge in actual["edges"]:
            expected = edges[edge["id"]]
            require(edge["controlSiteIds"] == expected["controls"], "Native control identity changed")
            require(edge["controls"] == [values[key] for key in expected["controls"]], "Native controls changed")
            checks += 2
        previous = visible
        checks += 5
    return {"program": program["id"], "rungs": len(program["rungs"]), "checks": checks,
            "sites": len(values), "edges": len(edges), "nativeReplayVerified": True}


def curve_point(controls, parameter):
    current = [list(point) for point in controls]
    while len(current) > 1:
        current = [[(1 - parameter) * a[axis] + parameter * b[axis] for axis in range(2)]
                   for a, b in zip(current, current[1:])]
    return current[0]


def audit_source_construction(program, observation):
    """Final strokes consume earlier construction and retain acquired identity."""
    mapping = program["sourceMapping"]
    require(mapping["sourceId"] == observation["sourceId"], "Construction source correspondence changed")
    span_map = mapping["spanMap"]
    spans = {span["id"]: span for span in observation["spans"]}
    require(set(span_map) == set(spans), "Final construction lost or invented acquired spans")
    require(len(set(span_map.values())) == len(span_map), "Distinct spans cannot silently collapse into one edge")
    require(set(program["rungs"][-1]["visible"]) == set(span_map.values()),
            "Final construction contains unsupported strokes or omits observed spans")
    values, dependencies = evaluate_program(program)
    edges = {edge["id"]: edge for edge in program["edges"]}
    sites = {site["id"]: site for site in program["sites"]}
    ancestry = {}
    for key, refs in dependencies.items():
        ancestry[key] = set(refs)
        for ref in refs:
            ancestry[key].update(ancestry[ref])
    acquired_endpoint_owners = {}
    errors = []
    coordinate_changes = []
    for stroke in observation["strokes"]:
        prior = None
        for key in stroke["spans"]:
            span = spans[key]
            edge = edges[span_map[key]]
            require(edge["at"] == stroke["index"] + 2, "Authored detail rungs changed acquired stroke order")
            require(len(edge["controls"]) == len(span["controls"]), "Selected line/curve participation changed silently")
            first, last = edge["controls"][0], edge["controls"][-1]
            if prior is not None:
                require(first == prior, "Consecutive acquisition spans lost their actual shared boundary")
            for site in (first, last):
                require(site not in acquired_endpoint_owners or acquired_endpoint_owners[site] == stroke["index"],
                        "A pen lift was welded by coordinate coincidence")
                acquired_endpoint_owners[site] = stroke["index"]
            earlier_drawn = {key for rung in program["rungs"][:edge["at"]]
                             for key in rung["visible"] + rung.get("guides", [])}
            earlier_drawn_controls = {site for earlier in program["edges"] if earlier["id"] in earlier_drawn
                                      for site in earlier["controls"]}
            require(all((ancestry[site] | {site}) & earlier_drawn_controls for site in edge["controls"]),
                    "Final detail has no actual dependency on an earlier drawn construction")
            require(all(sites[site]["at"] <= edge["at"] for site in edge["controls"]), "Edge consumed future controls")
            controls = [values[site] for site in edge["controls"]]
            coordinate_changes.extend(math.dist(a, b) for a, b in zip(controls, span["controls"]))
            points = stroke["points"][span["startIndex"]:span["endIndex"] + 1]
            lengths = [0.0]
            for a, b in zip(points, points[1:]):
                lengths.append(lengths[-1] + math.dist(a, b))
            for index, point in enumerate(points):
                if index == 0 and prior is not None:
                    continue
                parameter = lengths[index] / lengths[-1] if lengths[-1] else 0
                errors.append(math.dist(point, curve_point(controls, parameter)))
            prior = last
    expected_samples = sum(len(stroke["points"]) for stroke in observation["strokes"])
    require(len(errors) == expected_samples, "Approximation metric duplicated or omitted acquisition events")
    return {"sourceSpanCount": len(spans), "sourceStrokeCount": len(observation["strokes"]),
            "orderedSampleError": {"samples": len(errors), "rms": math.sqrt(sum(e * e for e in errors) / len(errors)) if errors else 0,
                                   "max": max(errors, default=0), "basis": "512-unit normalized page; equal acquired arclength parameter within each selected span",
                                   "qualification": "Diagnostic only: geometric and acquired-progress mismatch, not inferred-intent accuracy."},
            "maximumControlDisplacement": max(coordinate_changes, default=0),
            "preservedPenLiftIdentity": True, "preservedSourceStrokeOrder": True,
            "earlierDrawnConstructionConsumed": True}


def read_rows(path):
    return [json.loads(line) for line in Path(path).read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def verify(root, source):
    records = read_rows(source)
    observations = read_rows(root / "observations.ndjson")
    programs = read_rows(root / "programs.ndjson")
    runs = {run["id"]: run for run in read_rows(root / "native/inspector.ndjson")}
    by_source = {str(record["key_id"]): record for record in records}
    by_observation = {observation["sourceId"]: observation for observation in observations}
    require(len(by_source) == len(records) and len(by_observation) == len(observations), "Repeated source identity")
    require(set(by_source) == set(by_observation), "Observation fixture source membership changed")
    require({program["sourceMapping"]["sourceId"] for program in programs} == set(by_source),
            "Construction fixture source membership changed")
    require({program["id"] for program in programs} == set(runs), "Native construction membership changed")
    observation_results = [{"sourceId": key, **audit_observations(by_source[key], observation),
                            **audit_geometric_candidates(by_source[key], observation)}
                           for key, observation in by_observation.items()]
    results = [{**audit_native_program(program, runs[program["id"]]),
                **audit_source_construction(program, by_observation[program["sourceMapping"]["sourceId"]])}
               for program in programs]
    summary = {"schema": "gojo.quickdraw-interpreted-construction-audit.v1", "passed": True,
               "sourceSha256": hashlib.sha256(Path(source).read_bytes()).hexdigest(),
               "programSha256": hashlib.sha256((root / "programs.ndjson").read_bytes()).hexdigest(),
               "observationSha256": hashlib.sha256((root / "observations.ndjson").read_bytes()).hexdigest(),
               "sourceDrawings": len(records), "programs": len(programs),
               "rungs": sum(len(program["rungs"]) for program in programs),
               "checks": sum(result["checks"] for result in results),
               "scope": "Independent source coverage, candidate support, affine replay, control identity, causal earlier-drawing use and diagnostic approximation error. No learned intent or recognition claim.",
               "sources": observation_results, "runs": results}
    (root / "verification.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    return summary


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--source", type=Path, required=True)
    arguments = parser.parse_args()
    result = verify(arguments.root, arguments.source)
    print(json.dumps({key: result[key] for key in ("passed", "sourceDrawings", "programs", "rungs", "checks")}))
