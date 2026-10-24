"""Independent support/identity audit for non-generative annotation reads."""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def finite_tree(value):
    if isinstance(value, float):
        require(math.isfinite(value), "Annotation contains a nonfinite measured value")
    elif isinstance(value, dict):
        for child in value.values():
            finite_tree(child)
    elif isinstance(value, list):
        for child in value:
            finite_tree(child)


def derivative(controls, parameter):
    values = [[(b[j]-a[j])*(len(controls)-1) for j in range(2)]
              for a, b in zip(controls, controls[1:])]
    while len(values) > 1:
        values = [[a[j]*(1-parameter)+b[j]*parameter for j in range(2)]
                  for a, b in zip(values, values[1:])]
    return values[0]


def audit_annotations(annotation, observation):
    finite_tree(annotation)
    spans = {span["id"]: span for span in observation["spans"]}
    require(annotation["sourceId"] == observation["sourceId"], "Annotations changed acquisition source identity")
    ids, checked, ray_count = set(), 0, 0
    require(len(annotation["lines"]) == len(spans), "Every acquired interval needs one trace annotation")
    require(sorted(key for line in annotation["lines"] for key in line["spans"]) == sorted(spans),
            "Line annotations omitted or duplicated source intervals")
    for family in ("lines", "joints", "symmetries", "relations"):
        for entry in annotation[family]:
            require(entry["id"] not in ids, "Annotation IDs must be unique within a source/mode read")
            ids.add(entry["id"])
            require(entry["spans"] and len(set(entry["spans"])) == len(entry["spans"]), "Support must be a nonempty set of actual intervals")
            require(set(entry["spans"]) <= set(spans), "An annotation borrowed an absent interval")
            require(entry["qualification"], "An interpretive annotation lost its qualification")
            checked += 1
            if family == "joints":
                require(entry.get("observedSharedSite") in (True, False), "Shared acquired identity must be an explicit separate read")
                source_sites = []
                for participant in entry["participants"]:
                    require(participant["spanId"] in entry["spans"] and 0 <= participant["t"] <= 1,
                            "A joint participant is outside its actual interval")
                    site = participant.get("sourceSite")
                    if site is not None:
                        span = spans[participant["spanId"]]
                        require(site["strokeIndex"] == span["strokeIndex"] and
                                site["sampleIndex"] in (span["startIndex"], span["endIndex"]),
                                "A source-site claim is not an actual acquired endpoint")
                        require((participant["t"] == 0 and site["sampleIndex"] == span["startIndex"]) or
                                (participant["t"] == 1 and site["sampleIndex"] == span["endIndex"]),
                                "Acquired endpoint identity was attached to a different curve location")
                    source_sites.append(None if site is None else (site["strokeIndex"], site["sampleIndex"]))
                if entry["observedSharedSite"]:
                    require(source_sites and None not in source_sites and len(set(source_sites)) == 1,
                            "Coordinate coincidence was promoted to shared acquisition identity")
                for ray in entry["rays"]:
                    require(ray["spanId"] in entry["spans"] and 0 <= ray["t"] <= 1,
                            "Ray must select one actual supported curve location")
                    require(ray["toward"] in ("start", "end"), "Ordered ray direction was lost")
                    expected = derivative(spans[ray["spanId"]]["controls"], ray["t"])
                    if ray["toward"] == "start":
                        expected = [-value for value in expected]
                    actual = ray["vector"]
                    a_length, b_length = math.hypot(*expected), math.hypot(*actual)
                    if a_length > 1e-8 and b_length > 1e-8:
                        cross = expected[0]*actual[1]-expected[1]*actual[0]
                        dot = expected[0]*actual[0]+expected[1]*actual[1]
                        require(abs(cross)/(a_length*b_length) < 1e-6 and dot > 0,
                                "A joint ray no longer follows its selected source curve tangent")
                    ray_count += 1
                for pair in entry["rayPairs"]:
                    require(0 <= pair["first"] < pair["second"] < len(entry["rays"]),
                            "Angular pair refers to an absent or reordered ray")
                    a, b = (entry["rays"][pair[key]]["vector"] for key in ("first", "second"))
                    norm = math.hypot(*a)*math.hypot(*b)
                    require(norm > 0, "Degenerate rays cannot establish angular agreement")
                    angle = math.degrees(math.acos(max(-1, min(1, (a[0]*b[0]+a[1]*b[1])/norm))))
                    for key, value in (("opposedResidualDegrees", 180-angle),
                                       ("perpendicularResidualDegrees", abs(angle-90)),
                                       ("sameDirectionResidualDegrees", angle)):
                        require(abs(pair[key]-value) < 1e-5, "A joint's angular residual no longer follows its actual ray pair")
            if family == "symmetries":
                axis = entry["axis"]
                require(math.dist(axis["from"], axis["to"]) > 0, "A reflection needs a nonzero explicit axis")
                for pair in entry["correspondences"]:
                    require(pair["source"] in entry["spans"] and pair["target"] in entry["spans"],
                            "A mirror pair must select actual participant intervals")
                require(set(entry["unmatchedSpanIds"]) <= set(spans), "Unmatched symmetry residue lost its source")
    return {"annotations": checked, "rays": ray_count,
            "counts": {family: len(annotation[family]) for family in ("lines", "joints", "symmetries", "relations")}}


def read_rows(path):
    return [json.loads(line) for line in path.read_text(encoding="utf-8-sig").splitlines() if line.strip()]


def verify(root):
    root = Path(root)
    manifest = json.loads((root/"annotation-manifest.json").read_text(encoding="utf-8"))
    for name, field in (("programs.ndjson", "programSha256"), ("observations.ndjson", "observationSha256"),
                        ("selected.ndjson", "sourceSha256"), ("annotations.ndjson", "annotationsSha256")):
        require(hashlib.sha256((root/name).read_bytes()).hexdigest() == manifest[field], "Annotation input/output fingerprints changed")
    observations = {o["sourceId"]: o for o in read_rows(root/"observations.ndjson")}
    programs = {p["id"]: p for p in read_rows(root/"programs.ndjson")}
    annotations = read_rows(root/"annotations.ndjson")
    require({a["programId"] for a in annotations} == set(programs) and len(annotations) == len(programs),
            "Annotations must correspond one-to-one with frozen native programs")
    results = []
    for annotation in annotations:
        program = programs[annotation["programId"]]
        require(program["sourceMapping"]["sourceId"] == annotation["sourceId"] and
                program["observationMapping"]["mode"] == annotation["mode"], "Annotation was assigned to the wrong source/mode")
        results.append({"programId": annotation["programId"], "mode": annotation["mode"],
                        **audit_annotations(annotation, observations[annotation["sourceId"]])})
    mode_order = ("observed-fit", "gentle-priors", "stronger-priors")
    for source_id in observations:
        modes = {a["mode"]: a for a in annotations if a["sourceId"] == source_id}
        require(set(modes) == set(mode_order), "Each acquired drawing needs all three tolerance reads")
        for key in ("angleDegrees", "proximity", "linearity", "reflectionDistance", "repetitionDistance"):
            values = [modes[mode]["policy"][key] for mode in mode_order]
            require(0 < values[0] <= values[1] <= values[2], "Interpretive tolerance narrowed or lost its declared basis")
    result = {"schema": "gojo.quickdraw-annotation-verification.v1", "passed": True,
              "sourceDrawings": len(observations), "programs": len(programs),
              "constructionUnchanged": True, "nativeGeometryRegenerated": False,
              "scope": "Independent support references, source-site claims, ordered curve tangents, explicit reflection participants and monotonically widening tolerance; qualitative intent remains a candidate.",
              "runs": results}
    (root/"annotation-verification.json").write_text(json.dumps(result, indent=2)+"\n", encoding="utf-8")
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    args = parser.parse_args()
    result = verify(args.root)
    print(json.dumps({key: result[key] for key in ("passed", "sourceDrawings", "programs", "constructionUnchanged")}))
